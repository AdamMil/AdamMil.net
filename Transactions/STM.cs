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

// this is a software transactional memory (STM) system based on the ideas (particularly OSTM) laid out in the paper
// "Concurrent programming without locks" [Fraser and Harris, 2007]. the system should be lock-free, meaning that it is free from
// deadlock and livelock, and a transaction should be guaranteed to eventually commit if it is retried enough times. the system
// also incorporates features based on the Haskell STM system originally described in "Composable Memory Transactions" [Harris,
// Marlow, Jones, and Herlihy, 2006], such as nested transactions, automatic retry, and orElse composition. finally, the system
// integrates with the .NET System.Transactions framework.
//
// the basic ideas come from those papers, but unfortunately they don't give complete implementation descriptions. (for instance
// the Fraser paper handwaves away nested transactions and assumes single-phase commit, while the Haskell paper contains only a
// brief textual description of the implementation, which is tied to Concurrent Haskell runtime implementation details.)
// additionally, Fraser's system is not very amenable to the inclusion of fancy features from the Haskell STM or to integration
// with System.Transactions, so i've modified it substantially. hopefully i haven't broken the design too much.

// TODO: add spin waits to the CAS (CompareAndExchange) loops when we upgrade to .NET 4

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
  /// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> is not cloneable. See
  /// <see cref="TransactionalVariable{T}"/> for more details.
  /// </exception>
  public static TransactionalVariable<T> Allocate<T>()
  {
    return new TransactionalVariable<T>();
  }

  /// <summary>Allocates and returns a new <see cref="TransactionalVariable{T}"/> with the given value. This is equivalent to
  /// constructing a <see cref="TransactionalVariable{T}"/> using its constructor.
  /// </summary>
  /// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> is not cloneable. See
  /// <see cref="TransactionalVariable{T}"/> for more details.
  /// </exception>
  public static TransactionalVariable<T> Allocate<T>(T initialValue)
  {
    return new TransactionalVariable<T>(initialValue);
  }

  /// <summary>Executes an action until it successfully commits in a transaction.</summary>
  public static void Retry(Action action)
  {
    Retry(action, Timeout.Infinite);
  }

  /// <summary>Executes an action until it successfully commits in a transaction, or until the given time limit has ellapsed.</summary>
  /// <param name="action">The action to execute.</param>
  /// <param name="timeoutMs">The amount of time, in milliseconds, before the method will stop retrying the code. If
  /// <see cref="Timeout.Infinite"/> is given, the method will only stop when the action succeeds.
  /// </param>
  /// <remarks>If the method times out, the last exception thrown by the action will be rethrown.</remarks>
  public static void Retry(Action action, int timeoutMs)
  {
    if(action == null) throw new ArgumentNullException();
    Stopwatch timer = timeoutMs == Timeout.Infinite ? null : Stopwatch.StartNew();
    Exception exception = null;
    do
    {
      STMTransaction tx = STMTransaction.Create();
      try
      {
        action();
        tx.Commit();
        return;
      }
      catch(Exception ex) { exception = ex; }
      finally { tx.Dispose(); }
    } while(timer == null || timer.ElapsedMilliseconds < timeoutMs);

    throw exception;
  }

  /// <summary>Executes a function until it successfully commits in a transaction. The value returned from the function in the
  /// first successful transaction will then be returned.
  /// </summary>
  public static T Retry<T>(Func<T> function)
  {
    return Retry(function, Timeout.Infinite);
  }

  /// <summary>Executes a function until it successfully commits in a transaction, or until the given time limit has ellapsed.
  /// The value returned from the function in the first successful transaction will then be returned.
  /// </summary>
  /// <param name="function">The function to execute.</param>
  /// <param name="timeoutMs">The amount of time, in milliseconds, before the method will stop retrying the function. If
  /// <see cref="Timeout.Infinite"/> is given, the method will only stop when the action succeeds.
  /// </param>
  /// <remarks>If the method times out, the last exception thrown by the action will be rethrown.</remarks>
  public static T Retry<T>(Func<T> function, int timeoutMs)
  {
    if(function == null) throw new ArgumentNullException();
    Stopwatch timer = timeoutMs == Timeout.Infinite ? null : Stopwatch.StartNew();
    Exception exception = null;
    do
    {
      STMTransaction tx = STMTransaction.Create();
      try
      {
        T value = function();
        tx.Commit();
        return value;
      }
      catch(Exception ex) { exception = ex; }
      finally { tx.Dispose(); }
    } while(timer == null || timer.ElapsedMilliseconds < timeoutMs);

    throw exception;
  }
}
#endregion

#region STMImmutableAttribute
/// <summary>An attribute that can be applied to a type to designate that it is immutable as far as STM is concerned, so it need
/// not be copied when a variable of that type is opened in write mode, and need not implement <see cref="ICloneable"/>.
/// (And even if <see cref="ICloneable"/> is implemented, it will not be used by the STM system.) This attribute can also be
/// applied to immutable types used for fields within structures, allowing the STM system to copy the structure directly rather
/// than requiring an <see cref="ICloneable"/> implementation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class STMImmutableAttribute : Attribute
{
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
/// transaction is committed. Unlike the <see cref="Transaction"/> class, if a nested <see cref="STMTransaction"/> aborts, it can
/// be retried and does not force the parent <see cref="STMTransaction"/> to abort.
/// </para>
/// <para><see cref="STMTransaction"/> objects integrate with the .NET <see cref="Transaction"/> class. When <see cref="Create"/>
/// is called to create a new <see cref="STMTransaction"/>, it will be enlisted in the current <see cref="Transaction"/> if there
/// is one, and the changes will not be committed until the <see cref="STMTransaction"/> and the <see cref="Transaction"/> are
/// both committed.
/// </para>
/// </remarks>
public sealed class STMTransaction : IDisposable, IEnlistmentNotification
{
  internal STMTransaction(STMTransaction parent)
  {
    this.parent = parent;
    this.id     = (ulong)Interlocked.Increment(ref nextId);
  }

  internal STMTransaction(STMTransaction parent, Transaction systemTransaction) : this(parent)
  {
    this.systemTransaction = systemTransaction;
    systemTransaction.EnlistVolatile(this, EnlistmentOptions.None);
  }

  /// <summary>Attempts to commit the current transaction.</summary>
  /// <exception cref="InvalidOperationException">Thrown if the transaction has already finished (i.e. been committed, rolled
  /// back, etc), or if this is not the topmost transaction on the transaction stack.
  /// </exception>
  /// <exception cref="TransactionAbortedException">Thrown if the attempt to commit the transaction failed because of a conflict
  /// with another transaction.
  /// </exception>
  public void Commit()
  {
    if(this != Current) throw new InvalidOperationException();
    if(!TryCommit()) throw new TransactionAbortedException();
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

  /// <summary>Creates a new <see cref="STMTransaction"/> makes it the current transaction for the thread, and returns it.</summary>
  public static STMTransaction Create()
  {
    return Create(false);
  }

  /// <summary>Returns the current <see cref="STMTransaction"/> for the thread, potentially creating a new one first.</summary>
  /// <param name="ignoreSystemTransaction">If true, no attempt will be made to integrate the STM transaction with the current
  /// <see cref="Transaction"/>. This can improve performance of the <see cref="Create"/> call slightly if you know that
  /// <see cref="Transaction.Current"/> is null. If false is passed, the <see cref="STMTransaction"/> will be integrated with the
  /// current <see cref="Transaction"/> as normal.
  /// </param>
  public static STMTransaction Create(bool ignoreSystemTransaction)
  {
    STMTransaction transaction = Current;

    // if there's a current system transaction, enlist in it if we haven't already done so
    Transaction systemTransaction = ignoreSystemTransaction ? null : Transaction.Current;
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
      if(!found) Current = transaction = new STMTransaction(transaction, systemTransaction);
    }

    Current = transaction = new STMTransaction(transaction);
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
    if(readLog == null) readLog = new Dictionary<TransactionalVariable, object>();
    readLog.Add(variable, value);
    return value;
  }

  internal object OpenForWrite(TransactionalVariable variable)
  {
    AssertActive();
    // if the variable has been opened for writing by this transaction, return the current value from the write log
    WriteEntry entry;
    if(writeLog != null && writeLog.TryGetValue(variable, out entry)) return entry.NewValue;

    // otherwise, create a new entry in the write log
    return CreateWriteEntry(variable, null, false).NewValue;
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

    // otherwise, get the current value. if it's currently locked by another transaction, read it from that transaction's log
    value = variable.value;
    STMTransaction owningTransaction = value as STMTransaction;
    return owningTransaction == null ? value : owningTransaction.ReadWithoutOpening(variable, false);
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
    /// called. (This is useful for two-stage commit, to prevent a prepared transaction tied to System.Transactions from being
    /// aborted, which would violate the contract that a resource manager is supposed to implement.)
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
    public object OldValue, NewValue;
  }
  #endregion

  void AssertActive()
  {
    if(status != Status.Undetermined) throw new InvalidOperationException("The transaction is no longer active.");
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

    // set the new value. if we don't have one, clone the old value into a private copy to create the new value
    entry.NewValue = useNewValue ? newValue : variable.Clone(entry.OldValue);
    // and add the entry to the write log
    if(writeLog == null) writeLog = new SortedDictionary<TransactionalVariable, WriteEntry>(VariableComparer.Instance);
    writeLog.Add(variable, entry);
    return entry;
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

    if(parent == null || currentStatus != Status.Committed) // if this is a top-level or unsuccessful transaction...
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
    else // this is a successful nested transaction, so commit into the parent transaction by copying our log entries into it
    {
      parent.AssertActive(); // make sure the parent transaction is still active
      if(writeLog != null)
      {
        foreach(KeyValuePair<TransactionalVariable, WriteEntry> pair in writeLog) parent.Set(pair.Key, pair.Value.NewValue);
      }
      if(readLog != null)
      {
        foreach(KeyValuePair<TransactionalVariable,object> pair in readLog) parent.OpenForRead(pair.Key, pair.Value);
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
        // if we aren't performing our read check yet, then they started to commit first, so we should help them commit.
        // otherwise, we are both trying to commit, so help them commit only if they started executing first (i.e. have a lower
        // id). (it's not actually critical that they started executing first. it's only important that there exists some global
        // ordering of transactions that can be used to resolve conflicts). if we don't help them commit, then abort them. also,
        // we can't help transactions tied to System.Transactions to commit because they would only be in the preparation phase,
        // and we don't know whether the transaction manager will actually commit them. so we'll play it safe and abort them
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
        // if the other transaction is prepared to commit, so it will probably do so very shortly, and our transaction is still
        // active. if we take the old value, we will almost certainly abort later, so it may be worth waiting a short time to get
        // the new value and reduce our chance of having to abort
        Thread.Sleep(0);
      }
      // if the owning transaction committed directly to the variable, return the new value. otherwise, return the old value
      value = owningTransaction.parent == null && owningTransaction.status == Status.Committed ? entry.NewValue : entry.OldValue;
    }
    return value;
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

  /// <summary>Called from a nested transaction to commit read log entries into the enclosing transaction's (i.e. our) read log.</summary>
  void OpenForRead(TransactionalVariable variable, object value)
  {
    // we can assume that the variable is not opened in any enclosing transaction, because if it was then it wouldn't have
    // existed in a nested transaction's read log
    if(readLog == null) readLog = new Dictionary<TransactionalVariable, object>();
    readLog.Add(variable, value);
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
          // if its ultimate fate hasn't been decided yet, we may have to roll it back or abort ourselves instead. (we can't
          // simply abort it because we could get stuck in an infinite loop if it never gets around to undoing its changes)
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

    if(readLog != null)
    {
      // go through all the variables opened for reading and check if they've been changed by another transaction. unlike the
      // checks for the write log, above and below, we don't have to worry about variables changed by an enclosing transaction
      // because in that case they won't have been added to our read log. rather, they'd have been read directly from the
      // enclosing transaction's write log
      foreach(KeyValuePair<TransactionalVariable, object> pair in readLog)
      {
        if(GetCommittedValue(pair.Key) != pair.Value) goto decide; // if another transaction committed a change, we fail
      }
    }

    // if this is a nested transaction, we have to check the variables in the write log for changes, too, since we didn't
    // do it above in the lock code
    if(parent != null && writeLog != null)
    {
      foreach(KeyValuePair<TransactionalVariable, WriteEntry> pair in writeLog)
      {
        // if another transaction committed a change and we didn't inherit the original value from an enclosing transaction,
        // we fail. we don't fail if we inherited the value, because retrying the transaction would never succeed. instead, we'll
        // let the enclosing transaction deal with the failure, since that's the one that needs to be retried
        if(!IsOpenInEnclosure(pair.Key) && GetCommittedValue(pair.Key) != pair.Value.OldValue) goto decide;
      }
    }

    // at this point, it looks like we're going to succeed, but it's still possible that another transaction has already aborted
    // us, or will do so shortly
    if(status != Status.Aborted) newStatus = Status.Committed; // if another transaction aborted us, detect it sooner

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
          while(true) // while we're trying to abort them...
          {
            int status = top.status;
            if(status == Status.Committed || status == Status.Aborted) break;
            Interlocked.CompareExchange(ref top.status, Status.Aborted, status);
          }
          if(top == this) break;
          top.removedFromStack = true; // mark nested transactions as having been removed from the stack
          top = top.parent;
        }

        STMTransaction.Current = parent; // remove this transaction and all nested transactions
        removedFromStack = true;
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
      TryCommit();
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
    // get committed -- the variables may just be left in a locked state -- so we might as well roll them back to return them to
    // a consistent state sooner
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
  /// <summary>The read log, containing variables opened in read mode by this transaction, or null if none have been opened.</summary>
  Dictionary<TransactionalVariable, object> readLog;
  /// <summary>The write log, containing variables written by this transaction, or null if no variables have been written.</summary>
  SortedDictionary<TransactionalVariable, WriteEntry> writeLog;
  int preparedStatus, status;
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
  static long nextId;
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
    id    = (ulong)Interlocked.Increment(ref nextId);
    value = initialValue;
  }

  /// <summary>Opens the variable for read/write access and returns the current value. This method is meant to be used when the
  /// variable's value needs to be read before writing it, or when it is an object whose methods and properties will be used to
  /// mutate it. To replace the value completely, call <see cref="Set"/>.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown if there is no active <see cref="STMTransaction"/> on this thread, or
  /// if the current value's implementation of <see cref="ICloneable"/> is incorrect.
  /// </exception>
  public object OpenForWrite()
  {
    return GetTransaction().OpenForWrite(this);
  }

  /// <summary>Opens the variable for read access and returns the current value. You must not call any methods or properties on
  /// the returned object that would change it, without opening it in write mode (by calling <see cref="OpenForWrite"/>) first!
  /// If the variable will later be opened for write access, it is more efficient to just open it once, for read/write
  /// access, rather than opening it for read access and later reopening it for write access, although it is best to avoid
  /// opening a variable in write mode if possible.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown if there is no active <see cref="STMTransaction"/> on this thread.</exception>
  public object Read()
  {
    return GetTransaction().OpenForRead(this);
  }

  /// <summary>Reads the value of this variable without opening it. This should only be done if the logic will not be affected by
  /// the variable's value changing after it's read. If there is no current transaction, the most recently committed value will
  /// be returned.
  /// </summary>
  public object ReadWithoutOpening()
  {
    object value;
    STMTransaction transaction = STMTransaction.Current;
    if(transaction != null) // if there is a transaction on this thread...
    {
      value = transaction.ReadWithoutOpening(this, true); // get the current value from it
    }
    else // otherwise, there is no current transaction on this thread
    {
      value = this.value; // get the most recently committed value
      // if the variable is currently locked by some other transaction, get the old value from its log
      transaction = value as STMTransaction;
      if(transaction != null) value = transaction.ReadWithoutOpening(this, false);
    }
    return value;
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
    /// <summary>The value does will not be cloned or copied at all. This is only suitable for immutable types.</summary>
    NoClone,
    /// <summary>The value can be cloned by unboxing and reboxing it. This is suitable for mutable value types.</summary>
    Rebox,
    /// <summary>The value should be cloned by using the value's <see cref="ICloneable"/> implementation.</summary>
    ICloneable
  }

  /// <summary>Opens the variable for writing and sets it to the given value. This method is meant to be used when the
  /// variable's value will be replaced completely. To alter a mutable object through its methods and properties, use
  /// <see cref="OpenForWrite"/> to return a mutable instance of it.
  /// </summary>
  // NOTE: this is not public because it would allow type safety to be broken (e.g. non-T stored in TransactionalVariable<T>)
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
    typeLock.EnterReadLock();
    try { return cloneTypes[type]; }
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
  static long nextId;
}
#endregion

#region TransactionalVariable<T>
/// <summary>Represents a slot within transactional memory. To create a new transactional variable, construct an instance of this
/// type or call <see cref="STM.Allocate"/>.
/// </summary>
public sealed class TransactionalVariable<T> : TransactionalVariable
{
  /// <summary>Allocates a new <see cref="TransactionalVariable{T}"/> with a default value.</summary>
  /// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> is not cloneable. See
  /// <see cref="TransactionalVariable{T}"/> for more details.
  /// </exception>
  public TransactionalVariable() : this(default(T)) { }
  /// <summary>Allocates a new <see cref="TransactionalVariable{T}"/> with the given value.</summary>
  /// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> is not cloneable. See
  /// <see cref="TransactionalVariable{T}"/> for more details.
  /// </exception>
  public TransactionalVariable(T initialValue) : base(initialValue)
  {
    ValidateCloneType(typeof(T)); // verify that the object can be cloned
  }

  /// <summary>Opens the variable for read/write access and returns the current value. This method is meant to be used when the
  /// variable's value needs to be read before writing it, or when it is an object whose methods and properties will be used to
  /// mutate it. To replace the value completely, call <see cref="Set"/>.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown if there is no active <see cref="STMTransaction"/> on this thread, or
  /// if the current value's implementation of <see cref="ICloneable"/> is incorrect.
  /// </exception>
  public new T OpenForWrite()
  {
    return (T)base.OpenForWrite();
  }

  /// <summary>Opens the variable for read access and returns the current value. You must not call any methods or properties on
  /// the returned object that would change it, without opening it in write mode (by calling <see cref="OpenForWrite"/>) first!
  /// If the variable will later be opened for write access, it is more efficient to just open it once, for read/write
  /// access, rather than opening it for read access and later reopening it for write access, although it is best to avoid
  /// opening a variable in write mode if possible.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown if there is no active <see cref="STMTransaction"/> on this thread.</exception>
  public new T Read()
  {
    return (T)base.Read();
  }

  /// <summary>Reads the value of this variable without opening it. This should only be done if the logic will not be affected by
  /// the variable's value changing after it's read. If there is no current transaction, the most recently committed value will
  /// be returned.
  /// </summary>
  public new T ReadWithoutOpening()
  {
    return (T)base.ReadWithoutOpening();
  }

  /// <summary>Opens the variable for writing and sets its value to the given value. This method is meant to be used when the
  /// variable's value will be replaced completely. To alter a mutable object through its methods and properties, use
  /// <see cref="OpenForWrite"/> to return the current instance of it.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown if there is no active <see cref="STMTransaction"/> on this thread, or
  /// if the current value's implementation of <see cref="ICloneable"/> is incorrect.
  /// </exception>
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

} // namespace AdamMil.Transactions.STM
