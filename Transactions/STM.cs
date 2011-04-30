/*
AdamMil.Transactions is a library for the .NET framework that simplifies the
creation of transactional software.

http://www.adammil.net/
Copyright (C) 2011 Adam Milazzo

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

// this is a software transactional memory (STM) system based on the ideas (particularly OSTM) laid out in the paper "Concurrent
// programming without locks" [Fraser and Harris, 2007]. the system should be lock-free, meaning that it is free from deadlock
// and livelock, and a transaction should be guaranteed to eventually commit if it is retried enough times. the system also
// incorporates features based on the Haskell STM system originally described in "Composable Memory Transactions" [Harris,
// Marlow, Jones, and Herlihy, 2006], such as nested transactions, automatic retry, and orElse composition. finally, the system
// integrates with the .NET System.Transactions framework.
//
// the basic ideas come from those papers, but unfortunately they don't give complete implementation descriptions. (for instance
// the Fraser paper handwaves away nested transactions and assumes single-phase commit, while the Haskell paper contains only a
// brief textual description of the implementation, which is tied to Concurrent Haskell runtime implementation details.)
// additionally, Fraser's system is not very amenable to the inclusion of fancy features from the Haskell STM or to integration
// with System.Transactions, so i've modified it substantially. hopefully i haven't broken the design too much.
//
// the system allows the possibility that a transaction can read inconsistent state. this is a common problem with STM
// implementations, and in a system like this one, where transactions can help each other commit, it seems difficult to fix
// without adverse effects on either performance or transaction commit rate. a transaction that has read inconsistent state would
// eventually abort itself, but it's possible for the inconsistent state to cause unpredictable behavior if the transaction is
// not written with that possibility in mind. to address this, i've done the following:
// * added a way to manually check consistency during a transaction, and an option to automatically check it after each variable
//   is opened. the automatic check is not enabled by default due to the fact that it incurs a substantial performance penalty
// * made STM.Retry() use a consistency check to see if an exception thrown from the transaction may have been caused by
//   inconsistency. if so, the transaction is retried, and if not, the exception is propagated

// TODO: add spin waits to various loops when we upgrade to .NET 4

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Transactions;
using AdamMil.Utilities;

namespace AdamMil.Transactions
{

#region STM
/// <summary>Provides convenience methods for working with software transactional memory.</summary>
public static class STM
{
  /// <summary>Allocates and returns a new <see cref="TransactionalVariable{T}"/> with a default value. This is equivalent to
  /// constructing a <see cref="TransactionalVariable{T}"/> using its constructor.
  /// </summary>
  /// <include file="documentation.xml" path="TX/STM/Allocate/*" />
  public static TransactionalVariable<T> Allocate<T>()
  {
    return new TransactionalVariable<T>();
  }

  /// <summary>Allocates and returns a new <see cref="TransactionalVariable{T}"/> with the given value. This is equivalent to
  /// constructing a <see cref="TransactionalVariable{T}"/> using its constructor.
  /// </summary>
  /// <include file="documentation.xml" path="TX/STM/Allocate/*" />
  public static TransactionalVariable<T> Allocate<T>(T initialValue)
  {
    return new TransactionalVariable<T>(initialValue);
  }

  /// <exception cref="InvalidOperationException">Thrown if there is no current transaction, or if it is no longer active.</exception>
  /// <include file="documentation.xml" path="TX/STM/CheckConsistency/*" />
  public static void CheckConsistency()
  {
    GetTransaction().CheckConsistency();
  }

  /// <exception cref="InvalidOperationException">Thrown if there is no current transaction, or if it is no longer active.</exception>
  /// <include file="documentation.xml" path="TX/STM/IsConsistent/*" />
  public static bool IsConsistent()
  {
    return GetTransaction().IsConsistent();
  }

  /// <summary>Executes an action until it successfully commits in a transaction.</summary>
  /// <include file="documentation.xml" path="TX/STM/Retry/*"/>
  public static void Retry(Action action)
  {
    Retry(action, Timeout.Infinite);
  }

  /// <summary>Executes an action until it successfully commits in a transaction, or until the given time limit has elapsed.</summary>
  /// <param name="action">The action to execute.</param>
  /// <include file="documentation.xml" path="TX/STM/RetryWithTimeout/*"/>
  public static void Retry(Action action, int timeoutMs)
  {
    Retry((Func<object>)delegate { action(); return null; }, timeoutMs);
  }

  /// <summary>Executes a function until it successfully commits in a transaction. The value returned from the function in the
  /// first successful transaction will then be returned.
  /// </summary>
  /// <include file="documentation.xml" path="TX/STM/Retry/*"/>
  public static T Retry<T>(Func<T> function)
  {
    return Retry(function, Timeout.Infinite);
  }

  /// <summary>Executes a function until it successfully commits in a transaction, or until the given time limit has elapsed.
  /// The value returned from the function in the first successful transaction will then be returned.
  /// </summary>
  /// <param name="function">The function to execute.</param>
  /// <include file="documentation.xml" path="TX/STM/RetryWithTimeout/*"/>
  public static T Retry<T>(Func<T> function, int timeoutMs)
  {
    if(function == null) throw new ArgumentNullException();
    Stopwatch timer = timeoutMs == Timeout.Infinite ? null : Stopwatch.StartNew();
    int delay = 1;
    do
    {
      STMTransaction tx = STMTransaction.Create();
      try
      {
        T value = function();
        tx.Commit();
        return value;
      }
      catch(TransactionAbortedException) { }
      catch
      {
        // if the transaction has seen a consistent view of memory, then consider the exception to be legitimate
        if(tx.IsConsistent()) throw;
      }
      finally { tx.Dispose(); }
      Thread.Sleep(delay); // if it failed, wait a little bit before trying again
      if(delay < 250) delay *= 2;
    } while(timer == null || timer.ElapsedMilliseconds < timeoutMs);

    throw new TransactionAbortedException();
  }

  static STMTransaction GetTransaction()
  {
    STMTransaction transaction = STMTransaction.Current;
    if(transaction == null) throw new InvalidOperationException();
    return transaction;
  }
}
#endregion

#region STMImmutableAttribute
/// <summary>An attribute that can be applied to a type to designate that it is immutable as far as the STM system is concerned,
/// so it need not be copied when a variable of that type is opened in write mode, and need not implement
/// <see cref="ICloneable"/>. (And even if <see cref="ICloneable"/> is implemented, it will not be used by the STM system.) This
/// attribute can also be applied to immutable types used for fields within structures, allowing the STM system to copy the
/// structure directly rather than requiring an <see cref="ICloneable"/> implementation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class STMImmutableAttribute : Attribute
{
}
#endregion

#region STMOptions
/// <summary>Options used during the creation of a transaction to control how the transaction should behave.</summary>
[Flags]
public enum STMOptions
{
  /// <summary>Checks consistency only when <see cref="STMTransaction.Commit"/> is called, and attempts to integrate with
  /// System.Transactions.
  /// </summary>
  Default=0,
  /// <summary>Checks consistency after each variable is opened, as well as during <see cref="STMTransaction.Commit"/>, in order
  /// to ensure that the transaction always sees a consistent view of memory. This option incurs a substantial performance
  /// penalty, and it's recommended to avoid it and call <see cref="STMTransaction.CheckConsistency"/> manually where
  /// consistency is required, or better yet, write write your transactions so that they can tolerate inconsistency. See
  /// <see cref="STMTransaction.CheckConsistency"/> for details.
  /// </summary>
  EnsureConsistency=1,
  /// <summary>No attempt will be made to integrate the STM transaction with System.Transactions, but if an enclosing transaction
  /// was already integrated, then the newly created one will be implicitly integrated as well.
  /// </summary>
  IgnoreSystemTransaction=2
}
#endregion

#region STMTransaction
/// <summary>Represents a transaction that controls access to memory resources managed by <see cref="TransactionalVariable{T}"/>
/// objects. To commit the transaction, call <see cref="Commit"/>. In any case, <see cref="Dispose"/> must be called to clean up
/// the transaction. Typically, a transaction should be created and used in a <c>using</c> statement to ensure that it gets
/// disposed.
/// </summary>
/// <remarks>
/// <para>Transactions can be composed by nesting them, in which case the changes are not really committed until the outermost
/// transaction is committed. If a nested <see cref="STMTransaction"/> aborts, it can be retried and does not necessarily force
/// the parent transaction to abort.
/// </para>
/// <para>By default, <see cref="STMTransaction"/> objects integrate with the System.Transactions <see cref="Transaction"/>
/// class. When <see cref="Create"/> is called to create a new <see cref="STMTransaction"/>, it will be enlisted in the current
/// <see cref="Transaction"/> if there is one, and the changes will not be committed until the <see cref="STMTransaction"/> and
/// the <see cref="Transaction"/> are both committed. Integration with System.Transactions can be disabled using
/// <see cref="STMOptions.IgnoreSystemTransaction"/>.
/// </para>
/// </remarks>
/// <include file="documentation.xml" path="TX/STM/ConsistencyRemarks/*" />
public sealed class STMTransaction : IDisposable, IEnlistmentNotification
{
  internal STMTransaction(STMTransaction parent, STMOptions options)
  {
    this.parent  = parent;
    this.id      = (ulong)Interlocked.Increment(ref idCounter);
    this.options = options;
  }

  internal STMTransaction(STMTransaction parent, Transaction systemTransaction) : this(parent, STMOptions.Default)
  {
    this.systemTransaction = systemTransaction;
    systemTransaction.EnlistVolatile(this, EnlistmentOptions.None);
  }

  /// <exception cref="InvalidOperationException">Thrown if the transaction is no longer active.</exception>
  /// <remarks>If you are using <see cref="STM.Retry"/>, you can check the consistency of the current transaction using
  /// <see cref="STM.CheckConsistency"/>.
  /// </remarks>
  /// <seealso cref="STM.CheckConsistency"/>
  /// <include file="documentation.xml" path="TX/STM/CheckConsistency/*" />
  public void CheckConsistency()
  {
    if(!IsConsistent()) throw new TransactionAbortedException();
  }

  /// <include file="documentation.xml" path="TX/STM/Commit/*"/>
  public void Commit()
  {
    Commit(null);
  }

  /// <param name="postCommitAction">An action that will be executed after the transaction is successfully committed, or null if
  /// there is nothing additional to execute. This is intended to be used for non-transactional side effects that should not
  /// execute if a transaction is restarted. Using this parameter differs from simply running code after <see cref="Commit"/>
  /// returns because in the case of a nested transaction, it will be queued and executed only if all enclosing transactions also
  /// commit successfully. If the action throws an exception, it will not be caught by the <see cref="Commit"/> method, and will
  /// prevent post-commit actions from enclosing transactions from running.
  /// </param>
  /// <include file="documentation.xml" path="TX/STM/Commit/*"/>
  public void Commit(Action postCommitAction)
  {
    if(this != Current) throw new InvalidOperationException();
    else if(!TryCommit()) throw new TransactionAbortedException();

    if(postCommitAction != null)
    {
      if(postCommitActions == null) postCommitActions = new Queue<Action>();
      postCommitActions.Enqueue(postCommitAction);
    }

    if(parent == null) ExecutePostCommitActions();
    else MergePostCommitActions();
  }

  /// <summary>Disposes the transaction, removing it from the transaction stack and decoupling it from any
  /// <see cref="Transaction"/> in scope. This must be called when you have finished with the transaction.
  /// </summary>
  public void Dispose()
  {
    // decouple the transaction from System.Transactions to prevent it from holding up other transactions
    systemTransaction = null;
    RemoveFromStack();
  }

  /// <exception cref="InvalidOperationException">Thrown if the transaction is no longer active.</exception>
  /// <remarks>If you are using <see cref="STM.Retry"/>, you can check the consistency of the current transaction using
  /// <see cref="STM.IsConsistent"/>.
  /// </remarks>
  /// <seealso cref="STM.IsConsistent"/>
  /// <include file="documentation.xml" path="TX/STM/IsConsistent/*" />
  public bool IsConsistent()
  {
    AssertActive();
    return IsConsistent(true);
  }

  /// <inheritdoc/>
  public override string ToString()
  {
    return "STM Transaction #" + id.ToInvariantString();
  }

  /// <summary>Gets the current <see cref="STMTransaction"/> for this thread, or null if no transaction has been created.</summary>
  public static STMTransaction Current
  {
    get { return _current; }
    private set { _current = value; }
  }

  /// <summary>Creates a new <see cref="STMTransaction"/> using the default <see cref="STMOptions"/>, makes it the current
  /// transaction for the thread, and returns it.
  /// </summary>
  public static STMTransaction Create()
  {
    return Create(STMOptions.Default);
  }

  /// <summary>Creates a new <see cref="STMTransaction"/> using the given <see cref="STMOptions"/>, makes it the current
  /// transaction for the thread, and returns it.
  /// </summary>
  public static STMTransaction Create(STMOptions options)
  {
    STMTransaction transaction = Current;

    // if there's a current system transaction, enlist in it if we haven't already done so and aren't ignoring it
    Transaction systemTransaction = (options & STMOptions.IgnoreSystemTransaction) != 0 ? null : Transaction.Current;
    if(systemTransaction != null)
    {
      // search for an enclosing transaction that is bound to the system transaction
      bool found = false;
      for(STMTransaction t = transaction; t != null; t = t.parent)
      {
        if(t.systemTransaction == systemTransaction)
        {
          found = true;
          break;
        }
      }

      // if there was none found, push a new transaction onto the stack that is bound to and under the control of the
      // system transaction. the user's transaction will be nested within that one
      if(!found) transaction = new STMTransaction(transaction, systemTransaction);
    }

    Current = transaction = new STMTransaction(transaction, options);
    return transaction;
  }

  internal object OpenForRead(TransactionalVariable variable)
  {
    AssertActive();

    // search this transaction and enclosing transactions to try to find a log entry (read or write) for the variable
    object value;
    STMTransaction transaction = this;
    do
    {
      if(transaction.TryRead(variable, out value)) return value;
      transaction = transaction.parent;
    } while(transaction != null);

    // if no log entry exists, get the committed value of the variable, add it to the read log, and return it
    value = GetCommittedValue(variable);
    // if we're supposed to check consistency, do it before adding the log entry because the new entry needn't be checked
    if((options & STMOptions.EnsureConsistency) != 0) CheckConsistency();
    if(readLog == null) readLog = new Dictionary<TransactionalVariable, object>();
    readLog.Add(variable, value);
    return value;
  }

  internal object OpenForWrite(TransactionalVariable variable)
  {
    AssertActive();
    // if the variable has been opened for writing by this transaction, return the current value from the write log. otherwise,
    // create a new entry using the most recently committed value
    WriteEntry entry;
    return (writeLog != null && writeLog.TryGetValue(variable, out entry) ? entry : CreateWriteEntry(variable, null, false))
           .NewValue;
  }

  internal object ReadWithoutOpening(TransactionalVariable variable, bool useNewValue)
  {
    // search the this transaction and enclosing transactions to try to find a log entry for the variable
    object value;
    STMTransaction transaction = this;
    do
    {
      // if the variable has already been opened for reading, and has not subsequently been opened for writing, then just use
      // its current value from the read log
      if(transaction.readLog != null && transaction.readLog.TryGetValue(variable, out value)) return value;

      // if the variable has been opened for writing, return its value from the write log
      WriteEntry entry;
      if(transaction.writeLog != null && transaction.writeLog.TryGetValue(variable, out entry))
      {
        return !useNewValue || transaction.status == Status.Aborted ? entry.OldValue : entry.NewValue;
      }
      transaction = transaction.parent;
    } while(transaction != null);

    // otherwise, get the current value. if it's locked by another transaction, read it from that transaction's log
    value = variable.value;
    STMTransaction owningTransaction = value as STMTransaction;
    return owningTransaction == null ? value : owningTransaction.ReadWithoutOpening(variable, false);
  }

  internal void Release(TransactionalVariable variable)
  {
    // try removing it from the read log first, since a released variable is almost certain to have been opened in read-only
    // mode, and if it's not found there, then try removing it from the write log
    if((readLog == null || !readLog.Remove(variable)) && writeLog != null) writeLog.Remove(variable);
  }

  internal void Set(TransactionalVariable variable, object value)
  {
    AssertActive();
    // if it already exists in the write log, overwrite the new value. otherwise, create a new entry for it in the log
    WriteEntry entry;
    if(writeLog != null && writeLog.TryGetValue(variable, out entry)) entry.NewValue = value;
    else CreateWriteEntry(variable, value, true);
  }

  #region Status
  /// <summary>Contains constants representing the state of a transaction.</summary>
  static class Status
  {
    /// <summary>The transaction is still active and its status has not been determined.</summary>
    public const int Undetermined=0;
    /// <summary>The transaction is in the process of committing, and is checking opened variables for conflicts.</summary>
    public const int ReadCheck=1;
    /// <summary>The transaction is prepared to commit successfully, and will do so as soon as <see cref="FinishCommit"/> is
    /// called. This is used to establishing a point after the read check where a transaction tied to System.Transactions can no
    /// longer be aborted, since the abortion of a prepared System.Transactions transaction would violate the contract that a
    /// resource manager is supposed to implement.
    /// </summary>
    public const int Prepared=2;
    /// <summary>The transaction has been successfully committed.</summary>
    public const int Committed=3;
    /// <summary>The transaction has been aborted.</summary>
    public const int Aborted=4;
  }
  #endregion

  #region VariableComparer
  /// <summary>Compares <see cref="TransactionalVariable"/> objects by their IDs.</summary>
  sealed class VariableComparer : IComparer<TransactionalVariable>
  {
    VariableComparer() { }

    public int Compare(TransactionalVariable a, TransactionalVariable b)
    {
      return a.id < b.id ? -1 : a == b ? 0 : 1; // only identical variables have identical IDs
    }

    public static readonly VariableComparer Instance = new VariableComparer();
  }
  #endregion

  #region WriteEntry
  /// <summary>Represents an entry in the write log.</summary>
  sealed class WriteEntry
  {
    public WriteEntry() { }
    public WriteEntry(object oldValue, object newValue)
    {
      OldValue = oldValue;
      NewValue = newValue;
    }

    public object OldValue, NewValue;
  }
  #endregion

  void AssertActive()
  {
    if(status != Status.Undetermined)
    {
      throw new InvalidOperationException("The transaction is no longer active. Either Dispose() or Commit() has been called.");
    }
  }

  WriteEntry CreateWriteEntry(TransactionalVariable variable, object newValue, bool useNewValue)
  {
    WriteEntry entry = new WriteEntry();

    // if it was previously opened in any mode (possibly by an enclosing transaction), get the current value from the log.
    // otherwise, call GetCommittedValue() to get the current committed value
    STMTransaction transaction = this;
    do
    {
      if(transaction.TryRead(variable, out entry.OldValue))
      {
        // if it was found in our own read log, remove it, because it's being moved to our write log
        if(transaction == this) readLog.Remove(variable);
        break;
      }
      transaction = transaction.parent;
    } while(transaction != null);

    if(transaction == null) entry.OldValue = GetCommittedValue(variable);

    // if we're supposed to check consistency, do it before adding the log entry because the new entry needn't be checked
    if((options & STMOptions.EnsureConsistency) != 0) CheckConsistency();

    // set the new value. if we don't have one, clone the old value into a private copy to create the new value
    entry.NewValue = useNewValue ? newValue : variable.Clone(entry.OldValue);
    // and add the entry to the write log
    if(writeLog == null) writeLog = new SortedDictionary<TransactionalVariable, WriteEntry>(VariableComparer.Instance);
    writeLog.Add(variable, entry);
    return entry;
  }

  /// <summary>Executes any registered post-commit actions.</summary>
  void ExecutePostCommitActions()
  {
    if(postCommitActions != null)
    {
      try
      {
        foreach(Action action in postCommitActions) action();
      }
      finally
      {
        postCommitActions = null;
      }
    }
  }

  /// <summary>Takes the status from <see cref="preparedStatus"/> (usually set by calling <see cref="PrepareToCommit"/>) and
  /// attempts to commit or roll back the transaction based on that status.
  /// </summary>
  // NOTE: this method needs to be reentrant from multiple threads, because transactions can help each other commit by calling
  // each other's TryCommit() methods, and the user can also call Commit()
  void FinishCommit()
  {
    if(preparedStatus == Status.Undetermined) throw new InvalidOperationException();
    int currentStatus = TryUpdateStatus(ref status, preparedStatus);

    if(parent == null) // if this is a top-level transaction...
    {
      if(writeLog != null) // if we have variables to unlock...
      {
        // unlock everything by either committing our changes or rolling them back
        foreach(KeyValuePair<TransactionalVariable, WriteEntry> pair in writeLog)
        {
          Interlocked.CompareExchange(ref pair.Key.value,
                                      currentStatus == Status.Committed ? pair.Value.NewValue : pair.Value.OldValue, this);
        }
      }
    }
    else if(status == Status.Committed) // this is a successful nested transaction, so commit into the parent transaction
    {
      parent.AssertActive(); // make sure the parent transaction is still active
      // despite the fact that FinishCommit() must be thread-safe in general, we don't have to protect access to the log data
      // structures here because this is a nested transaction, and FinishCommit() should only be called concurrently for
      // top-level transactions, since nested transactions never take ownership of any variables, and the parent transaction,
      // which might be top-level, can't have started committing yet
      if(writeLog != null)
      {
        if(parent.writeLog == null)
        {
          parent.writeLog = new SortedDictionary<TransactionalVariable, WriteEntry>(VariableComparer.Instance);
        }
        foreach(KeyValuePair<TransactionalVariable, WriteEntry> pair in writeLog)
        {
          WriteEntry entry;
          if(parent.writeLog.TryGetValue(pair.Key, out entry))
          {
            entry.NewValue = pair.Value.NewValue;
          }
          else
          {
            parent.writeLog.Add(pair.Key, new WriteEntry(pair.Value.OldValue, pair.Value.NewValue));
            // remove it from the parent's read log if we added it to the write log
            if(parent.readLog != null) parent.readLog.Remove(pair.Key);
          }
        }
      }
      if(readLog != null)
      {
        if(parent.readLog == null) parent.readLog = new Dictionary<TransactionalVariable, object>();
        foreach(KeyValuePair<TransactionalVariable, object> pair in readLog)
        {
          // only add to the parent's read log if the variable doesn't already exist in its write log
          if(parent.writeLog == null || !parent.writeLog.ContainsKey(pair.Key)) parent.readLog.Add(pair.Key, pair.Value);
        }
      }
    }

    RemoveFromStack(); // remove the transaction from the stack after the commit process has finished
  }

  /// <summary>Retrieves the current committed value from a <see cref="TransactionalVariable"/>.</summary>
  object GetCommittedValue(TransactionalVariable variable)
  {
    object value = variable.value; // try to get the value. it might be an STMTransaction reference if it's locked

    STMTransaction owningTransaction = value as STMTransaction;
    if(owningTransaction != null) // if the variable is currently locked for commit by a top-level transaction...
    {
      WriteEntry entry = owningTransaction.writeLog[variable]; // then grab the entry from the owning transaction's write log
      if(owningTransaction.status == Status.ReadCheck) // if the other transaction is performing its read check...
      {
        // in general we want to help the other transaction commit, but if the other transaction is performing its read check and
        // so is ours, then there is the possibility of a cycle whereby in our read check, we've locked a location read by the
        // other transaction, and the other transaction has locked a location that we've read. this would result in an infinite
        // recursion whereby each tries to help the other to commit. the solution is to abort one of them, but the logic must be
        // careful not to abort both, as that would downgrade the guarantee of lock-freedom to mere obstruction-freedom. so if
        // we're both performing our read check, then the transaction with the greater ID will be aborted. also, we can't help
        // transactions tied to System.Transactions to commit because they would only be in the preparation phase, and we don't
        // know whether the transaction manager will actually decide to commit them. so we'll play it safe and abort them
        if(owningTransaction.systemTransaction == null && (status != Status.ReadCheck || id > owningTransaction.id))
        {
          owningTransaction.TryCommit(); // help it commit
        }
        else
        {
          Interlocked.CompareExchange(ref owningTransaction.status, Status.Aborted, Status.ReadCheck); // abort it
        }
      }
      else if(owningTransaction.status == Status.Prepared && status == Status.Undetermined)
      {
        // the other transaction is prepared to commit, so it will probably do so very shortly. if our transaction is still
        // active, then if we take the old value, we will almost certainly abort later, so it may be worth waiting a short time
        // to get the new value and reduce our chance of having to abort
        Thread.Sleep(0);
      }
      // if the owning transaction committed directly to the variable, return the new value. otherwise, return the old value
      value = owningTransaction.parent == null && owningTransaction.status == Status.Committed ? entry.NewValue : entry.OldValue;
    }
    return value;
  }

  bool IsConsistent(bool checkWriteLog)
  {
    // we only check variables that were opened by this transaction and were not opened by an enclosing transaction, since those
    // are the only variables whose values might change if this transaction was restarted
    if(readLog != null)
    {
      // go through all the variables opened for reading and check if they've been changed by another transaction. unlike the
      // checks for the write log, we don't have to worry about variables changed by an enclosing transaction because in that
      // case they won't have been added to our read log. rather, they'd have been read directly from the enclosing transaction's
      // write log
      foreach(KeyValuePair<TransactionalVariable, object> pair in readLog)
      {
        // variables in the read log cannot have been opened by an enclosing transaction
        if(GetCommittedValue(pair.Key) != pair.Value) return false;
      }
    }

    // if this is a nested transaction, we have to check the variables in the write log for changes, too, since we didn't
    // do it above in the lock code
    if(checkWriteLog && writeLog != null)
    {
      foreach(KeyValuePair<TransactionalVariable, WriteEntry> pair in writeLog)
      {
        // if another transaction committed a change and we didn't inherit the original value from an enclosing transaction,
        // we fail. we don't fail if we inherited the value, because retrying the transaction would never succeed. instead, we'll
        // let the enclosing transaction deal with the failure, since that's the one that needs to be retried
        if((parent == null || !IsOpenInEnclosure(pair.Key)) && GetCommittedValue(pair.Key) != pair.Value.OldValue) return false;
      }
    }
    return true;
  }

  /// <summary>Determines whether the variable has been opened in an enclosing transaction.</summary>
  bool IsOpenInEnclosure(TransactionalVariable variable)
  {
    for(STMTransaction trans = parent; trans != null; trans = trans.parent)
    {
      if(trans.readLog != null && trans.readLog.ContainsKey(variable) ||
         trans.writeLog != null && trans.writeLog.ContainsKey(variable))
      {
        return true;
      }
    }
    return false;
  }

  /// <summary>Moves our post-commit actions into the parent so that the parent can execute them upon commit.</summary>
  void MergePostCommitActions()
  {
    // the parent shouldn't have any yet because it hasn't been committed
    parent.postCommitActions = postCommitActions;
    postCommitActions        = null;
  }

  /// <summary>Prepares the transaction for committing by taking ownership of written variables, and places the transaction
  /// status into <see cref="preparedStatus"/>.
  /// </summary>
  // NOTE: this method needs to be reentrant from multiple threads, because transactions can help each other commit by calling
  // each other's TryCommit() methods, and the user can also call Commit()
  void PrepareToCommit()
  {
    int newStatus = Status.Aborted; // assume that the transaction will abort

    // acquire "locks" on all of the changed variables by replacing their values with a pointer to the current transaction. the
    // variables are locked in order by ID. but we only need to do it if this isn't a nested transaction. in fact, locking may
    // fail in a nested transaction if it and an enclosing transaction both change the same variable, because it will think the
    // variable has been committed by another transaction, due to the old value (taken from the enclosing transaction) not
    // matching the currently committed value
    if(parent == null && writeLog != null) // if we need to commit changes directly to the variables...
    {
      foreach(KeyValuePair<TransactionalVariable, WriteEntry> pair in writeLog)
      {
        while(true)
        {
          // replace the variable's value with a pointer to the transaction if another transaction hasn't committed a change
          object value = Interlocked.CompareExchange(ref pair.Key.value, this, pair.Value.OldValue);
          // if we just locked it, or it was already locked, then we're done with it. otherwise, it couldn't be locked
          if(value == pair.Value.OldValue || value == this) break;
          STMTransaction owningTransaction = value as STMTransaction;
          if(owningTransaction == null) goto decide; // another transaction committed a change already, so we fail
          // another transaction already has it locked for committing, but we can help that transaction commit and then check
          // again. if the transaction aborts, we can lock it. however, we can't simply commit a transaction tied to
          // System.Transactions because we don't know if the transaction manager will actually want to commit it. in that case,
          // if its ultimate fate hasn't been decided yet, we may have to abort ourselves instead. (we can't simply abort it
          // because we could get stuck in an infinite loop if it never gets around to undoing its changes)
          int otherStatus = owningTransaction.status;
          if(owningTransaction.systemTransaction == null ||
             (otherStatus == Status.Aborted || otherStatus == Status.Committed))
          {
            owningTransaction.TryCommit();
          }
          else if(otherStatus != Status.Prepared && id < owningTransaction.id)
          {
            // if a transaction tied to System.Transactions hasn't yet finished preparation, then we can try to abort it, but
            // we'll only do so if we started running first
            Interlocked.CompareExchange(ref owningTransaction.status, Status.Aborted, otherStatus);
            if(owningTransaction.status == Status.Aborted) owningTransaction.TryCommit();
          }
          else
          {
            // otherwise, it has finished preparation, so we can't abort it, as that may lead to inconsistencies, since after
            // preparing, a System.Transactions transaction is supposed to be guaranteed to commit successfully when Commit() is
            // eventually called. if we aborted it now, it would violate that expectation. (or, it just started first and we want
            // to be courteous)
            goto decide;
          }
        }
      }
    }

    // move from the Undetermined phase to the ReadCheck phase. this is done with CAS because of the reentrancy requirement
    Interlocked.CompareExchange(ref status, Status.ReadCheck, Status.Undetermined);

    if(IsConsistent(parent != null) && status != Status.Aborted)
    {
      // at this point, it looks like we're going to succeed, but it's still possible that another transaction has already
      // aborted us, or will do so shortly
      newStatus = Status.Committed;
    }

    decide:
    int currentStatus = TryUpdateStatus(ref preparedStatus, newStatus);

    // now update the real status based on the prepared status. this is to get us out of the read check phase so that we can't
    // be aborted anymore
    TryUpdateStatus(ref status, currentStatus == Status.Aborted ? Status.Aborted : Status.Prepared);
  }

  /// <summary>Removes the transaction from the transaction stack.</summary>
  void RemoveFromStack()
  {
    if(!removedFromStack)
    {
      // find the current transaction in the stack
      bool found = false;
      STMTransaction top = STMTransaction.Current;
      for(STMTransaction transaction = top; transaction != null; transaction = transaction.parent)
      {
        if(transaction == this)
        {
          found = true;
          break;
        }
      }

      if(found)
      {
        // abort transactions that haven't been committed
        while(true) // for this transaction and nested transactions...
        {
          TryUpdateStatus(ref top.status, Status.Aborted);
          top.removedFromStack = true; // mark transactions as having been removed from the stack
          if(top == this) break;
          top = top.parent;
        }

        STMTransaction.Current = parent; // remove this transaction and all nested transactions
      }
    }
  }

  void RollBack()
  {
    if(status != Status.Committed && status != Status.Aborted) // if the transaction hasn't already finished...
    {
      preparedStatus = Status.Aborted; // then abort it
      FinishCommit();
    }
  }

  bool TryCommit()
  {
    if(preparedStatus == Status.Undetermined) PrepareToCommit();
    FinishCommit();
    return status == Status.Committed;
  }

  /// <summary>Attempts to read the current value of the given variable from the read and write logs, and returns true if the
  /// value was found. No new log entries will be created.
  /// </summary>
  bool TryRead(TransactionalVariable variable, out object value)
  {
    // if the variable has already been opened for reading, and has not subsequently been opened for writing, then just use
    // its current value from the read log
    if(readLog != null && readLog.TryGetValue(variable, out value)) return true;

    // if the variable has been opened for writing, return its current value from the write log
    WriteEntry entry;
    if(writeLog != null && writeLog.TryGetValue(variable, out entry))
    {
      value = entry.NewValue;
      return true;
    }

    value = null;
    return false;
  }

  #region IEnlistmentNotification Members
  void IEnlistmentNotification.Commit(Enlistment enlistment)
  {
    try
    {
      if(TryCommit() && postCommitActions != null)
      {
        if(parent == null) ExecutePostCommitActions();
        else MergePostCommitActions();
      }
    }
    finally
    {
      Dispose(); // dispose the transaction here, since it's not exposed to the user
      enlistment.Done();
    }
  }

  void IEnlistmentNotification.InDoubt(Enlistment enlistment)
  {
    // roll our changes back if the transaction is in doubt, to at least put the variables in a consistent state. although the
    // rest of the transaction might have completed successfully, we'll never find out. if we take no action, our changes won't
    // get committed -- the variables may be left temporarily in a locked state -- so we might as well roll them back to return
    // them to an unlocked state sooner
    try
    {
      RollBack();
    }
    finally
    {
      Dispose(); // dispose the transaction here, since it's not exposed to the user
      enlistment.Done();
    }
  }

  void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
  {
    try
    {
      PrepareToCommit();
    }
    finally
    {
      if(preparedStatus == Status.Committed && status == Status.Prepared) preparingEnlistment.Prepared();
      else preparingEnlistment.ForceRollback(); // if it doesn't seem we can commit, then rollback the transaction
    }
  }

  void IEnlistmentNotification.Rollback(Enlistment enlistment)
  {
    try
    {
      RollBack();
    }
    finally
    {
      Dispose(); // dispose the transaction here, since it's not exposed to the user
      enlistment.Done();
    }
  }
  #endregion

  /// <summary>The transaction's unique ID.</summary>
  readonly ulong id;
  /// <summary>The enclosing transaction on the transaction stack, or null if this is a top-level transaction.</summary>
  readonly STMTransaction parent;
  /// <summary>The .NET <see cref="Transaction"/> in which this STM transaction is enlisted, or null if none.</summary>
  Transaction systemTransaction;
  /// <summary>The read log, containing variables opened in read-only mode by this transaction, or null if none have been.</summary>
  Dictionary<TransactionalVariable, object> readLog;
  /// <summary>The write log, containing variables open in write mode by this transaction, or null if none have been.</summary>
  SortedDictionary<TransactionalVariable, WriteEntry> writeLog;
  /// <summary>A queue holding actions to be executed after a successful top-level commit, or null if none have been enqueued.</summary>
  Queue<Action> postCommitActions;
  int preparedStatus, status;
  /// <summary>The <see cref="STMOptions"/> used to create the transaction.</summary>
  readonly STMOptions options;
  bool removedFromStack;

  static int TryUpdateStatus(ref int status, int newStatus)
  {
    int currentStatus;
    while(true)
    {
      currentStatus = status;
      // if the status has already been set (i.e. it's equal to Committed or Aborted), then we can't change it.
      if(currentStatus == newStatus || currentStatus == Status.Committed || currentStatus == Status.Aborted) break;
      Interlocked.CompareExchange(ref status, newStatus, currentStatus);
    }
    return currentStatus;
  }

  [ThreadStatic] static STMTransaction _current;
  // the counter can provide 10 billion IDs/second for 58 years before overflowing, which should be enough to ensure uniqueness
  static long idCounter;
}
#endregion

#region TransactionalVariable
/// <summary>Represents a slot within transactional memory. To create a new transactional variable, either construct an instance
/// of <see cref="TransactionalVariable{T}"/> or call <see cref="STM.Allocate"/>.
/// </summary>
public abstract class TransactionalVariable
{
  internal TransactionalVariable(object initialValue) // prevent any other assembly from subclassing it
  {
    id    = (ulong)Interlocked.Increment(ref idCounter);
    value = initialValue;
  }

  /// <include file="documentation.xml" path="TX/STM/OpenForWrite/*" />
  public object OpenForWrite()
  {
    return GetTransaction().OpenForWrite(this);
  }

  /// <include file="documentation.xml" path="TX/STM/Read/*" />
  public object Read()
  {
    STMTransaction transaction = STMTransaction.Current;
    return transaction == null ? ReadCommitted() : transaction.OpenForRead(this);
  }

  /// <include file="documentation.xml" path="TX/STM/ReadWithoutOpening/*" />
  public object ReadWithoutOpening()
  {
    STMTransaction transaction = STMTransaction.Current;
    return transaction == null ? ReadCommitted() : transaction.ReadWithoutOpening(this, true);
  }

  /// <summary>Causes the transaction to forget that this variable has been opened. A variable opened in write mode will have any
  /// changes discarded, and in any case, the variable will be excluded from later consistency checks unless it is opened again.
  /// This method must be used with great care, as it can destroy the consistency of a system by allowing transactions to make
  /// decisions based on inconsistent data. As such, you should not use this method unless you know exactly what you're doing.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This method can be used to implement certain optimizations, and is useful in the construction of efficient transactional
  /// data structures. To use this method successfully, you must ensure that when the transaction commits, the variables that
  /// remain open suffice to detect any inconsistency that could meaningfully affect the result. Be aware that calling this
  /// method releases the variable regardless of how many times it has been opened in the same transaction. That is to say, the
  /// variables are not reference counted.
  /// </para>
  /// <para>As an example, imagine a sorted linked list A -> B -> D -> E. If C is inserted between B and D, then B will need to
  /// be updated. This change may cause a transaction that is searching for E to abort because it opened the preceding items
  /// (including B) for read access. However, the insertion and the search are not actually in conflict. Releasing variables to
  /// avoid this kind of false conflict can greatly increase scalability. The solution is the following:
  /// <list type="bullet">
  ///   <item><description>A transaction searching for an item need only keep the node currently being checked and the previous
  ///   node open for reading. Nodes before that can be released.
  ///   </description></item>
  ///   <item><description>A transaction inserting an item needs to keep the node being modified open for writing and the
  ///   previous node open for reading. (While searching for the place to insert the item, it can follow the searching behavior
  ///   described above.)
  ///   </description></item>
  ///   <item><description>A transaction removing an item needs to have both the node being removed and the previous node open
  ///   for writing, but can release items before that.
  ///   </description></item>
  /// </list>
  /// To see that this solution is correct, first consider what is needed for a search. If a search finds an item, it should
  /// still exist within the list at the point that the search transaction commits. That is, it must not have been removed. When
  /// a node is removed from a linked list, it and/or the previous node must be modified. By keeping the found node and the
  /// previous node open for read access, a search ensures that neither have been modified, and therefore the node could not have
  /// been removed. Similarly, if a search doesn't find an item, it should not exist in the list at the time the search commits.
  /// Since the list is sorted, the search would stop as soon as it finds an item greater than the search key. Since the previous
  /// node must have been less than the search key, the key must be inserted between those two nodes in order to add it to the
  /// list. To do this, the lesser node must be modified, but since it's held open by the search, it cannot be.
  /// </para>
  /// <para>Next, consider what is needed for an insertion to succeed. The node before the one being inserted must not be removed
  /// or modified. Ensuring this works the same way as for the search. Finally, consider what is necessary for a removal to
  /// succeed. The node being removed and/or the previous node must be modified, and the subsequent node must not be removed, and
  /// nothing can be inserted immediately after the previous node or the node to be removed. Opening both the node to remove and
  /// the previous node for write access ensures both of these conditions. If the next node was removed or something was inserted
  /// after the node being removed, it would require the modification of the node being removed, which is prevented by it being
  /// kept open. If something was to be inserted after the previous node, the previous node would need to be modified, and
  /// keeping it open prevents that. Therefore, all three operations can be supported by keeping only two nodes open at a time.
  /// This type of analysis may quickly grow complex, but is necessary to ensure correct usage of this method. If in doubt, don't
  /// release your variables.
  /// </para>
  /// </remarks>
  public void Release()
  {
    GetTransaction().Release(this);
  }

  /// <summary>Gets the string representation of the <see cref="TransactionalVariable"/>'s value for the current thread. This is
  /// done by converting the value returned by <see cref="ReadWithoutOpening"/> to a string.
  /// </summary>
  public override string ToString()
  {
    object value = ReadWithoutOpening();
    return value == null ? "" : value.ToString(); // now finally call .ToString() on the value
  }

  /// <summary>Indicates how a value will be cloned when a variable is opened in write mode.</summary>
  protected enum CloneType : byte
  {
    /// <summary>The value will not be cloned at all. This is only suitable for immutable types.</summary>
    NoClone,
    /// <summary>The value can be cloned by unboxing and reboxing it. This is suitable for mutable value types.</summary>
    Rebox,
    /// <summary>The value should be cloned by using the value's <see cref="ICloneable"/> implementation.</summary>
    ICloneable
  }

  /// <include file="documentation.xml" path="TX/STM/Set/*" />
  // NOTE: this is not public because it would allow type safety to be broken (i.e. non-T stored in TransactionalVariable<T>)
  protected void Set(object newValue)
  {
    GetTransaction().Set(this, newValue);
  }

  /// <summary>Returns a <see cref="CloneType"/> value indicating how the type should be cloned. It is assumed that the type is
  /// cloneable (i.e. that <see cref="ValidateCloneType"/> has been successfully called on it).
  /// </summary>
  protected static CloneType GetCloneType(Type type)
  {
    if(Type.GetTypeCode(type) != TypeCode.Object) return CloneType.NoClone;
    try
    {
      typeLock.EnterReadLock();
      return cloneTypes[type];
    }
    finally { typeLock.ExitReadLock(); }
  }

  /// <summary>Ensures that the type is cloneable, and stores information about how to clone.</summary>
  protected static void ValidateCloneType(Type type)
  {
    // primitives, strings, and DBNull values are immutable and don't need to be cloned
    if(Type.GetTypeCode(type) != TypeCode.Object) return;

    typeLock.ReadWrite(
      delegate { return cloneTypes.ContainsKey(type); },
      delegate
      {
        CloneType cloneType;
        if(type.GetCustomAttributes(typeof(STMImmutableAttribute), false).Length != 0) cloneType = CloneType.NoClone;
        else if(typeof(ICloneable).IsAssignableFrom(type)) cloneType = CloneType.ICloneable;
        else if(type.IsValueType && IsCopyable(type)) cloneType = CloneType.Rebox;
        else throw new NotSupportedException(type.FullName + " is not cloneable.");
        cloneTypes[type] = cloneType;
      });
  }

  /// <summary>Called to clone a value from this variable. The clone should be as deep as necessary to ensure that the given
  /// value cannot be changed using the reference returned, but need not be any deeper. If the value is immutable, it may simply
  /// be returned as-is.
  /// </summary>
  internal abstract object Clone(object value);

  /// <summary>The unique ID of this transactional variable, used to achieve a total order on all transactional variables.</summary>
  internal readonly ulong id;
  /// <summary>The most recently committed value, or a reference to the transaction currently committing this .</summary>
  internal object value;

  /// <summary>Reads the value most recently committed to this variable, without regard for whether it has been opened by the
  /// current transaction.
  /// </summary>
  object ReadCommitted()
  {
    object value = this.value; // get the most recently committed value
    // if the variable is currently locked by some other transaction, get the old value from its log
    STMTransaction transaction = value as STMTransaction;
    if(transaction != null) value = transaction.ReadWithoutOpening(this, false);
    return value;
  }

  /// <summary>Gets the current <see cref="STMTransaction"/> for this thread, or throws an exception if there is no current
  /// transaction.
  /// </summary>
  static STMTransaction GetTransaction()
  {
    STMTransaction transaction = STMTransaction.Current;
    if(transaction == null) throw new InvalidOperationException("There is no current transaction.");
    return transaction;
  }

  /// <summary>Determines whether the given type, which is assumed to be a struct type, can be cloned by simply copying its
  /// field values.
  /// </summary>
  static bool IsCopyable(Type type)
  {
    return IsCopyable(type, null);
  }

  static bool IsCopyable(Type type, HashSet<Type> typesSeen)
  {
    // 'type' refers to a structure. for each field in the structure...
    foreach(FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
    {
      Type fieldType = field.FieldType;
      if(typesSeen != null && typesSeen.Contains(fieldType)) continue;

      if(Type.GetTypeCode(fieldType) == TypeCode.Object) // if the field may not be copyable...
      {
        // if it's a reference type, then it can be simply copied only if it's marked with [STMImmutable]
        if(!fieldType.IsValueType)
        {
          if(fieldType.GetCustomAttributes(typeof(STMImmutableAttribute), false).Length == 0) return false;
          else continue;
        }

        // we haven't processed the field type already, but it's a value type, so it might be copyable. check it recursively
        if(typesSeen == null) typesSeen = new HashSet<Type>();
        typesSeen.Add(fieldType);
        if(!IsCopyable(fieldType, typesSeen)) return false;
      }
    }

    return true;
  }

  static readonly Dictionary<Type, CloneType> cloneTypes = new Dictionary<Type, CloneType>();
  static readonly ReaderWriterLockSlim typeLock = new ReaderWriterLockSlim();
  // the counter can provide 10 billion IDs/second for 58 years before overflowing, which should be enough to ensure uniqueness
  static long idCounter;
}
#endregion

#region TransactionalVariable<T>
/// <summary>Represents a slot within transactional memory. To create a new transactional variable, construct an instance of this
/// type or call <see cref="STM.Allocate"/>.
/// </summary>
/// <include file="documentation.xml" path="TX/STM/TVarRemarks/*" />
public sealed class TransactionalVariable<T> : TransactionalVariable
{
  /// <summary>Allocates a new <see cref="TransactionalVariable{T}"/> with a default value.</summary>
  /// <include file="documentation.xml" path="TX/STM/NewTVar/*" />
  public TransactionalVariable() : this(default(T)) { }
  /// <summary>Allocates a new <see cref="TransactionalVariable{T}"/> with the given value.</summary>
  /// <include file="documentation.xml" path="TX/STM/NewTVar/*" />
  public TransactionalVariable(T initialValue) : base(initialValue)
  {
    ValidateCloneType(typeof(T)); // verify that the object can be cloned
  }

  /// <include file="documentation.xml" path="TX/STM/OpenForWrite/*" />
  public new T OpenForWrite()
  {
    return (T)base.OpenForWrite();
  }

  /// <include file="documentation.xml" path="TX/STM/Read/*" />
  public new T Read()
  {
    return (T)base.Read();
  }

  /// <include file="documentation.xml" path="TX/STM/ReadWithoutOpening/*" />
  public new T ReadWithoutOpening()
  {
    return (T)base.ReadWithoutOpening();
  }

  /// <include file="documentation.xml" path="TX/STM/Set/*" />
  public void Set(T newValue)
  {
    base.Set(newValue);
  }

  internal override object Clone(object value)
  {
    switch(GetCloneType(typeof(T)))
    {
      case CloneType.ICloneable:
        if(value != null)
        {
          value = ((ICloneable)value).Clone();
          if(!(value is T)) // ensure that Clone() returns a value of the right type
          {
            throw new InvalidOperationException("A call to Clone() on a value of type " + typeof(T).FullName + " returned " +
                                                (value == null ? "null" : "a value of type " + value.GetType().FullName) + ".");
          }
        }
        break;
      case CloneType.Rebox: value = (T)value; break;
    }
    return value;
  }
}
#endregion

} // namespace AdamMil.Transactions
