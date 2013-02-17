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
// incorporates nested transactions. finally, the system integrates with the .NET System.Transactions framework, although while
// doing so, lock-freedom must be suspended at a certain point to match the System.Transactions model, which doesn't really
// support obstruction clearing.
//
// the basic ideas come from that papers, but unfortunately it doesn't give complete a implementation description. for instance,
// the paper handwaves away nested transactions and assumes single-phase commit. additionally, it's not very amenable to the
// inclusion of fancy features from the Haskell STM or to integration with System.Transactions, so i've modified it somewhat
//
// the system allows the possibility that a transaction can read inconsistent state. this is a common problem with STM
// implementations, and in a system like this one, where transactions can help each other commit, it seems difficult to fix
// without adverse effects on either performance or transaction commit rate. a transaction that has read inconsistent state would
// eventually abort itself, but it's possible for the inconsistent state to cause unpredictable behavior if the transaction is
// not written with that possibility in mind. to address this, i've done the following:
// * added a way to manually check consistency during a transaction, to establish that the transaction has been consistent so far
// * added an option to automatically check consistency after each variable is opened. this eliminates the possibility of viewing
//   inconsistent state, at the cost of a substantial performance penalty
// * made STM.Retry() use a consistency check to see if an exception thrown from the transaction may have been caused by
//   inconsistency. if so, the transaction is retried, and if not, the exception is propagated

// TODO: add spin waits to various loops when we upgrade to .NET 4 (e.g. TryUpdateStatus)?
// TODO: consider using GCHandles (i.e. weak references) in the transaction log so that we don't hold onto things that aren't used anymore
// (but check the performance first)
// TODO: consider using CallContext instead of (or in addition to) thread-local storage so that the transaction will flow across
// asynchronous calls, etc

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Transactions;
using AdamMil.Utilities;

namespace AdamMil.Transactions
{

#region ISTMTransactionalValue
/// <summary>An interface that can be implemented by a value type to provide some control over its use within the transaction.</summary>
public interface ISTMTransactionalValue
{
  /// <summary>Called just before a transaction commits this value to a variable opened for write. This method can perform final
  /// operations upon the value, such as making it read-only.
  /// </summary>
  void PrepareForWrite();
}
#endregion

#region STM
/// <summary>Provides convenience methods for working with software transactional memory.</summary>
public static class STM
{
  /// <summary>Allocates and returns a new <see cref="TransactionalVariable{T}"/> with a default value. This is equivalent to
  /// constructing a <see cref="TransactionalVariable{T}"/> using its constructor.
  /// </summary>
  /// <include file="documentation.xml" path="/TX/STM/Allocate/node()" />
  public static TransactionalVariable<T> Allocate<T>()
  {
    return new TransactionalVariable<T>();
  }

  /// <summary>Allocates and returns a new <see cref="TransactionalVariable{T}"/> with the given value. This is equivalent to
  /// constructing a <see cref="TransactionalVariable{T}"/> using its constructor.
  /// </summary>
  /// <include file="documentation.xml" path="/TX/STM/Allocate/node()" />
  public static TransactionalVariable<T> Allocate<T>(T initialValue)
  {
    return new TransactionalVariable<T>(initialValue);
  }

  /// <exception cref="InvalidOperationException">Thrown if there is no current transaction, or if it is no longer active.</exception>
  /// <include file="documentation.xml" path="/TX/STM/CheckConsistency/node()" />
  public static void CheckConsistency()
  {
    GetTransaction().CheckConsistency();
  }

  /// <exception cref="InvalidOperationException">Thrown if there is no current transaction, or if it is no longer active.</exception>
  /// <include file="documentation.xml" path="/TX/STM/IsConsistent/node()" />
  public static bool IsConsistent()
  {
    return GetTransaction().IsConsistent();
  }

  /// <summary>Executes an action until it successfully commits in a transaction.</summary>
  /// <include file="documentation.xml" path="/TX/STM/Retry/*[@name != 'options' and @name != 'postCommitAction']"/>
  public static void Retry(Action action)
  {
    Retry(Timeout.Infinite, STMOptions.Default, action, null);
  }

  /// <summary>Executes an action until it successfully commits in a transaction.</summary>
  /// <param name="action">The action to execute.</param>
  /// <include file="documentation.xml" path="/TX/STM/Retry/*[@name != 'options']"/>
  public static void Retry(Action action, Action postCommitAction)
  {
    Retry(Timeout.Infinite, STMOptions.Default, action, postCommitAction);
  }

  /// <summary>Executes an action until it successfully commits in a transaction.</summary>
  /// <param name="action">The action to execute.</param>
  /// <include file="documentation.xml" path="/TX/STM/Retry/*[@name != 'postCommitAction']"/>
  public static void Retry(STMOptions options, Action action)
  {
    Retry(Timeout.Infinite, options, action, null);
  }

  /// <summary>Executes an action until it successfully commits in a transaction.</summary>
  /// <param name="action">The action to execute.</param>
  /// <include file="documentation.xml" path="/TX/STM/Retry/node()"/>
  public static void Retry(STMOptions options, Action action, Action postCommitAction)
  {
    Retry(Timeout.Infinite, options, action, postCommitAction);
  }

  /// <summary>Executes an action until it successfully commits in a transaction, or until the given time limit has elapsed.</summary>
  /// <param name="action">The action to execute.</param>
  /// <include file="documentation.xml" path="/TX/STM/RetryWithTimeout/*[@name != 'options' and @name != 'postCommitAction']"/>
  public static void Retry(int timeoutMs, Action action)
  {
    Retry(timeoutMs, STMOptions.Default, action, null);
  }

  /// <summary>Executes an action until it successfully commits in a transaction, or until the given time limit has elapsed.</summary>
  /// <param name="action">The action to execute.</param>
  /// <include file="documentation.xml" path="/TX/STM/RetryWithTimeout/*[@name != 'postCommitAction']"/>
  public static void Retry(int timeoutMs, STMOptions options, Action action)
  {
    Retry(timeoutMs, options, action, null);
  }

  /// <summary>Executes an action until it successfully commits in a transaction, or until the given time limit has elapsed.</summary>
  /// <param name="action">The action to execute.</param>
  /// <include file="documentation.xml" path="/TX/STM/RetryWithTimeout/node()"/>
  public static void Retry(int timeoutMs, STMOptions options, Action action, Action postCommitAction)
  {
    Retry(timeoutMs, options, (Func<object>)delegate { action(); return null; }, postCommitAction);
  }

  /// <summary>Executes a function until it successfully commits in a transaction. The value returned from the function in the
  /// first successful transaction will then be returned.
  /// </summary>
  /// <include file="documentation.xml" path="/TX/STM/Retry/*[@name != 'options' and @name != 'postCommitAction']"/>
  public static T Retry<T>(Func<T> function)
  {
    return Retry(Timeout.Infinite, STMOptions.Default, function, null);
  }

  /// <summary>Executes a function until it successfully commits in a transaction. The value returned from the function in the
  /// first successful transaction will then be returned.
  /// </summary>
  /// <param name="function">The function to execute.</param>
  /// <include file="documentation.xml" path="/TX/STM/Retry/*[@name != 'options']"/>
  public static T Retry<T>(Func<T> function, Action postCommitAction)
  {
    return Retry(Timeout.Infinite, STMOptions.Default, function, postCommitAction);
  }

  /// <summary>Executes a function until it successfully commits in a transaction. The value returned from the function in the
  /// first successful transaction will then be returned.
  /// </summary>
  /// <param name="function">The function to execute.</param>
  /// <include file="documentation.xml" path="/TX/STM/Retry/*[@name != 'postCommitAction']"/>
  public static T Retry<T>(STMOptions options, Func<T> function)
  {
    return Retry(Timeout.Infinite, options, function, null);
  }

  /// <summary>Executes a function until it successfully commits in a transaction. The value returned from the function in the
  /// first successful transaction will then be returned.
  /// </summary>
  /// <param name="function">The function to execute.</param>
  /// <include file="documentation.xml" path="/TX/STM/Retry/node()"/>
  public static T Retry<T>(STMOptions options, Func<T> function, Action postCommitAction)
  {
    return Retry(Timeout.Infinite, options, function, postCommitAction);
  }

  /// <summary>Executes a function until it successfully commits in a transaction, or until the given time limit has elapsed.
  /// The value returned from the function in the first successful transaction will then be returned.
  /// </summary>
  /// <param name="function">The function to execute.</param>
  /// <include file="documentation.xml" path="/TX/STM/RetryWithTimeout/*[@name != 'options' and @name != 'postCommitAction']"/>
  public static T Retry<T>(int timeoutMs, Func<T> function)
  {
    return Retry(timeoutMs, STMOptions.Default, function, null);
  }

  /// <summary>Executes a function until it successfully commits in a transaction, or until the given time limit has elapsed.
  /// The value returned from the function in the first successful transaction will then be returned.
  /// </summary>
  /// <param name="function">The function to execute.</param>
  /// <include file="documentation.xml" path="/TX/STM/RetryWithTimeout/*[@name != 'postCommitAction']"/>
  public static T Retry<T>(int timeoutMs, STMOptions options, Func<T> function)
  {
    return Retry(timeoutMs, options, function, null);
  }

  /// <summary>Executes a function until it successfully commits in a transaction, or until the given time limit has elapsed.
  /// The value returned from the function in the first successful transaction will then be returned.
  /// </summary>
  /// <param name="function">The function to execute.</param>
  /// <include file="documentation.xml" path="/TX/STM/RetryWithTimeout/node()"/>
  public static T Retry<T>(int timeoutMs, STMOptions options, Func<T> function, Action postCommitAction)
  {
    if(function == null) throw new ArgumentNullException();
    TimeoutTimer timer = new TimeoutTimer(timeoutMs);
    int delay = 1;
    do
    {
      STMTransaction tx = STMTransaction.Create(options);
      try
      {
        T value = function();
        tx.Commit(postCommitAction);
        return value;
      }
      catch(TransactionAbortedException) { }
      catch
      {
        // if the transaction has seen a consistent view of memory, then consider the exception to be legitimate and rethrow it
        if(tx.IsConsistent()) throw;
      }
      finally { tx.Dispose(); }

      if(timeoutMs != 0) // if it failed, wait a little bit before trying again
      {
        Thread.Sleep(Math.Min(delay, timer.RemainingTime));
        if(delay < 250) delay *= 2;
      }
    } while(!timer.HasExpired);

    throw new TransactionAbortedException();
  }

  /// <summary>Executes an action in the context of a transaction, using the current transaction if it exists, or creating and committing
  /// a new one if not.
  /// </summary>
  public static void RunInTransaction(Action action)
  {
    RunInTransaction((Func<object>)delegate { action(); return null; });
  }

  /// <summary>Executes a function in the context of a transaction, using the current transaction if it exists, or creating and committing
  /// a new one if not.
  /// </summary>
  public static T RunInTransaction<T>(Func<T> function)
  {
    if(function == null) throw new ArgumentNullException();
    STMTransaction tx = STMTransaction.Current;
    bool ownTransaction = tx == null;
    try
    {
      if(tx == null) tx = STMTransaction.Create();
      T value = function();
      if(ownTransaction) tx.Commit();
      return value;
    }
    finally
    {
      if(ownTransaction && tx != null) tx.Dispose();
    }
  }

  /// <include file="documentation.xml" path="/TX/STM/WaitForDistributedTransaction/node()" />
  /// <remarks>The method will wait for up to 30 seconds before throwing an exception.</remarks>
  public static void WaitForDistributedTransaction()
  {
    STMTransaction.WaitForDistributedTransaction(30 * 1000);
  }

  /// <include file="documentation.xml" path="/TX/STM/WaitForDistributedTransaction/node()" />
  /// <param name="timeoutMs">The number of milliseconds that the method should wait for any pending .NET system transaction to complete,
  /// or <see cref="Timeout.Infinite"/> if the method should wait indefinitely.
  /// </param>
  public static void WaitForDistributedTransaction(int timeoutMs)
  {
    STMTransaction.WaitForDistributedTransaction(timeoutMs);
  }

  static STMTransaction GetTransaction()
  {
    STMTransaction transaction = STMTransaction.Current;
    if(transaction == null) throw new InvalidOperationException("There is no current transaction.");
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
public sealed class STMImmutableAttribute : Attribute
{
}
#endregion

#region STMOptions
/// <summary>Options used during the creation of a transaction to control how the transaction should behave.</summary>
[Flags]
public enum STMOptions
{
  /// <summary>Checks consistency only when <see cref="STMTransaction.Commit"/> is called, and attempts to integrate with
  /// System.Transactions (assuming no containing transaction has disabled it).
  /// </summary>
  Default=0,
  /// <summary>Checks consistency after each variable is opened, as well as during <see cref="STMTransaction.Commit"/>, in order
  /// to ensure that the transaction always sees a consistent view of memory. This option incurs a substantial performance
  /// penalty, and it's recommended to avoid it and call <see cref="STMTransaction.CheckConsistency"/> manually where
  /// consistency is required, or better yet, write write your transactions so that they can tolerate inconsistency. See
  /// <see cref="STMTransaction.CheckConsistency"/> for details.
  /// </summary>
  EnsureConsistency=1,
  /// <summary>Disables integration with System.Transactions for the current <see cref="STMTransaction"/> and all nested transactions.</summary>
  DisableSystemTransactions=2,
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
/// <para><see cref="STMTransaction"/> objects integrate with the System.Transactions <see cref="Transaction"/> class. When
/// <see cref="Create"/> is called to create a new <see cref="STMTransaction"/>, it will be enlisted in the current
/// <see cref="Transaction"/> if there is one, and the changes will not be committed until the <see cref="STMTransaction"/> and
/// the <see cref="Transaction"/> are both committed. Similarly, if <see cref="Current"/> will implicitly create an
/// <see cref="STMTransaction"/> that integrates with a <see cref="Transaction"/> if necessary.
/// </para>
/// </remarks>
/// <include file="documentation.xml" path="/TX/STM/ConsistencyRemarks/node()" />
public sealed class STMTransaction : IDisposable, ISinglePhaseNotification
{
  internal STMTransaction(STMTransaction parent, STMOptions options)
  {
    if(parent != null) options |= parent.options & STMOptions.DisableSystemTransactions; // inherit DisableSystemTransactions from parent
    this.parent  = parent;
    this.id      = (ulong)Interlocked.Increment(ref idCounter);
    this.options = options;
  }

  internal STMTransaction(STMTransaction parent, Transaction systemTransaction) : this(parent, STMOptions.Default)
  {
    Debug.Assert((options & STMOptions.DisableSystemTransactions) == 0); // make sure the parent isn't disabling integration
    this.systemTransaction = systemTransaction;
    systemTransaction.EnlistVolatile(this, EnlistmentOptions.None);
  }

  /// <summary>Gets or sets whether consistency will be checked after each variable is opened, in order to ensure that the
  /// transaction always sees a consistent view of memory. The default is false. This option incurs a substantial performance
  /// penalty, and it's recommended to avoid it and call <see cref="STMTransaction.CheckConsistency"/> manually where consistency
  /// is required, or better yet, write write your transactions so that they can tolerate inconsistency. See <see
  /// cref="STMTransaction.CheckConsistency"/> for details.
  /// </summary>
  /// <include file="documentation.xml" path="/TX/STM/ConsistencyRemarks/node()" />
  public bool EnsureConsistency
  {
    get { return (options & STMOptions.EnsureConsistency) != 0; }
    set
    {
      if(value) options |= STMOptions.EnsureConsistency;
      else options &= ~STMOptions.EnsureConsistency;
    }
  }

  /// <exception cref="InvalidOperationException">Thrown if the transaction is no longer active.</exception>
  /// <remarks>If you are using <see cref="STM.Retry"/>, you can check the consistency of the current transaction using
  /// <see cref="STM.CheckConsistency"/>.
  /// </remarks>
  /// <seealso cref="STM.CheckConsistency"/>
  /// <include file="documentation.xml" path="/TX/STM/CheckConsistency/node()" />
  public void CheckConsistency()
  {
    if(!IsConsistent()) throw new TransactionAbortedException();
  }

  /// <include file="documentation.xml" path="/TX/STM/Commit/node()"/>
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
  /// <include file="documentation.xml" path="/TX/STM/Commit/node()"/>
  public void Commit(Action postCommitAction)
  {
    if(this != CurrentUnmanaged) throw new InvalidOperationException();
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
    RemoveFromStack();
    // decouple the transaction from System.Transactions to prevent it from holding up other transactions, but only do it if the
    // transaction has been removed from the stack. that should be the case most of the time, but sometimes a system transaction will be
    // committed on a separate thread and we won't be able to remove it. (i've only seen this happen with distributed transactions.) in
    // that case, we have little choice but to leave it on the stack until it gets removed by CurrentUnmanaged
    if(removedFromStack) systemTransaction = null;
  }

  /// <exception cref="InvalidOperationException">Thrown if the transaction is no longer active.</exception>
  /// <remarks>If you are using <see cref="STM.Retry"/>, you can check the consistency of the current transaction using
  /// <see cref="STM.IsConsistent"/>.
  /// </remarks>
  /// <seealso cref="STM.IsConsistent"/>
  /// <include file="documentation.xml" path="/TX/STM/IsConsistent/node()" />
  public bool IsConsistent()
  {
    AssertActive();
    return IsConsistent(true);
  }

  /// <inheritdoc/>
  public override string ToString()
  {
    return "STM Transaction #" + id.ToStringInvariant();
  }

  /// <summary>Gets the current <see cref="STMTransaction"/> for this thread, or null if no transaction has been created and there is no
  /// current <see cref="Transaction"/>.
  /// </summary>
  /// <remarks>If no <see cref="STMTransaction"/> has been created but a current <see cref="Transaction"/> exists, a new
  /// <see cref="STMTransaction"/> will be created and enlisted in the <see cref="Transaction"/> by this property.
  /// </remarks>
  public static STMTransaction Current
  {
    get
    {
      Transaction systemTransaction = null;
      bool bindSystemTransaction = false;
      STMTransaction transaction = CurrentUnmanaged;
      if(transaction == null) // if there's no current STM transaction...
      {
        // push a new transaction onto the stack that is bound to and under the control of the system transaction if there is one
        systemTransaction     = Transaction.Current;
        bindSystemTransaction = systemTransaction != null;
      }
      else if((transaction.options & STMOptions.DisableSystemTransactions) == 0) // otherwise, if System.Transaction integration is enabled
      {
        systemTransaction = Transaction.Current;
        if(transaction.systemTransaction != systemTransaction) // if there is an STM transaction, but it's not bound to the current system
        {                                                      // transaction (which could be null)...
          if(systemTransaction != null) // if there is a system transaction...
          {
            STMTransaction boundTransaction = transaction; // look for an STM transaction on the stack bound to it...
            do boundTransaction = boundTransaction.parent;
            while(boundTransaction != null && boundTransaction.systemTransaction != systemTransaction);
            // if there is none, push a new transaction onto the stack that's bound to the system transaction
            bindSystemTransaction = boundTransaction == null;
          }
          else // otherwise, the current STM transaction is bound to a system transaction, but there is no current system transaction.
          {    // that probably means that the system transaction is being suppressed
            return GetUnsuppressedTransaction(transaction);
          }
        }
      }

      if(bindSystemTransaction) _current = transaction = new STMTransaction(transaction, systemTransaction);
      return transaction;
    }
  }

  /// <summary>Gets whether there is an <see cref="STMTransaction"/> currently associated with this thread. This is not quite the same as
  /// checking whether <see cref="Current"/> is not null, since the <see cref="Current"/> property will create a new
  /// <see cref="STMTransaction"/> if there is a system <see cref="Transaction"/> to bind to, while this property will not.
  /// </summary>
  public static bool HasCurrent
  {
    get { return CurrentUnsuppressed != null; }
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
    STMTransaction tx = new STMTransaction((options & STMOptions.DisableSystemTransactions) == 0 ? Current : CurrentUnmanaged, options);
    _current = tx;
    return tx;
  }

  /// <summary>If this <see cref="STMTransaction"/> is bound to an system transaction that has been suppressed, this method returns
  /// the nearest unsuppressed transaction or null if there is no unsuppressed transaction. Otherwise, the method returns this
  /// <see cref="STMTransaction"/>.
  /// </summary>
  internal STMTransaction GetUnsuppressed()
  {
    return systemTransaction != null && Transaction.Current == null ? GetUnsuppressedTransaction(this) : this;
  }

  internal bool IsConsistent(TransactionalVariable variable)
  {
    // see IsConsistent(bool) for details
    object value;
    if(readLog != null && readLog.TryGetValue(variable, out value)) return GetCommittedValue(variable) == value;

    WriteEntry entry;
    if(writeLog != null && writeLog.TryGetValue(variable, out entry))
    {
      return !IsOpenInEnclosure(variable) && GetCommittedValue(variable) == entry.OldValue;
    }

    return true;
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
    if(EnsureConsistency) CheckConsistency();
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
    return (writeLog != null && writeLog.TryGetValue(variable, out entry) ? entry : CreateWriteEntry(variable, null, false)).NewValue;
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
    AssertActive();
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

  /// <summary>Gets the current <see cref="STMTransaction"/> for this thread, or null if no transaction has been created. Transactions will
  /// not be created automatically and enlisted with System.Transactions, and the property makes no attempt to handle system transactions
  /// that have been suppressed.
  /// </summary>
  internal static STMTransaction CurrentUnmanaged
  {
    get
    {
      STMTransaction transaction = _current;
      // sometimes system transactions get committed or rolled back on other threads, so they can't be successfully removed from the stack.
      // (this usually happens with distributed transactions.) in that case, they can get left on the stack. so we'll detect that case and
      // remove those transactions now
      while(transaction != null && transaction.systemTransaction != null &&
            (transaction.status == Status.Committed || transaction.status == Status.Aborted))
      {
        transaction.Dispose(); // this should remove the transaction from the stack
        transaction = _current; // get the new transaction on top of the stack
      }
      return transaction;
    }
  }

  /// <summary>Gets the nearest unsuppressed <see cref="STMTransaction"/> for this thread, or null if no transaction has been created or
  /// the current transaction has been suppressed and there is no unsuppressed ancestor. The property will not create a new transaction in
  /// any case.
  /// </summary>
  internal static STMTransaction CurrentUnsuppressed
  {
    get
    {
      STMTransaction transaction = CurrentUnmanaged;
      if(transaction != null && transaction.systemTransaction != null && Transaction.Current == null)
      {
        transaction = GetUnsuppressedTransaction(transaction);
      }
      return transaction;
    }
  }

  /// <summary>Waits for the transaction to become usable after the close of a transaction scope that may have involved a distributed
  /// transaction.
  /// </summary>
  internal static void WaitForDistributedTransaction(int timeoutMs)
  {
    TimeoutTimer timer = new TimeoutTimer(timeoutMs);
    int delay = 0;
    do
    {
      STMTransaction transaction = STMTransaction.CurrentUnmanaged;
      if(transaction == null || transaction.systemTransaction == null || transaction.status != Status.Prepared) return;

      if(timeoutMs != 0)
      {
        Thread.Sleep(Math.Min(delay, timer.RemainingTime));
        if(delay == 0) delay = 1;
        else if(delay < 250) delay *= 2;
      }
    } while(!timer.HasExpired);

    throw new TransactionException("Transaction is still not ready.");
  }

  [ThreadStatic] static STMTransaction _current;

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
    if(EnsureConsistency) CheckConsistency();

    // set the new value. if we don't have one, clone the old value into a private copy to create the new value
    entry.NewValue = useNewValue ? newValue : variable.Clone(entry.OldValue);
    // and add the entry to the write log
    if(writeLog == null) writeLog = new Dictionary<TransactionalVariable, WriteEntry>();
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
        if(parent.writeLog == null) parent.writeLog = new Dictionary<TransactionalVariable, WriteEntry>();
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

    // acquire "locks" on all of the changed variables by replacing their values with a pointer to the current transaction. the variables
    // are locked in order by ID to ensure global progress. we only need to do this if this isn't a nested transaction. in fact, locking
    // may fail in a nested transaction if it and an enclosing transaction both change the same variable, because it will think the
    // variable has been committed by another transaction, due to the old value (taken from the enclosing transaction) not matching the
    // currently committed value
    if(parent == null && writeLog != null) // if we need to commit changes directly to the variables...
    {
      TransactionalVariable[] sortedVars = new TransactionalVariable[writeLog.Count];
      WriteEntry[] writeEntries = new WriteEntry[writeLog.Count];
      int index = 0;
      foreach(KeyValuePair<TransactionalVariable, WriteEntry> pair in writeLog)
      {
        sortedVars[index]   = pair.Key;
        writeEntries[index] = pair.Value;
        index++;
      }
      Array.Sort(sortedVars, writeEntries, VariableComparer.Instance);

      for(index=0; index<sortedVars.Length; index++)
      {
        WriteEntry writeEntry = writeEntries[index];
        while(true)
        {
          // replace the variable's value with a pointer to the transaction if another transaction hasn't committed a change
          object value = Interlocked.CompareExchange(ref sortedVars[index].value, this, writeEntry.OldValue);
          // if we just locked it, or it was already locked, then we're done with it. otherwise, it couldn't be locked
          if(value == writeEntry.OldValue) break; // if we just locked it, then finish processing it
          else if(value == this) goto alreadyLocked; // otherwise, if it was already locked, skip to the next entry

          // at this point, we couldn't lock it because it was locked or changed by another transaction
          STMTransaction owningTransaction = value as STMTransaction;
          if(owningTransaction == null) goto decide; // another transaction committed a change already, so we fail
          // another transaction already has it locked for committing, but we can help that transaction commit and then check
          // again. if the transaction aborts, we can lock it. however, we can't simply commit a transaction tied to
          // System.Transactions because we don't know if the transaction manager will actually want to commit it. in that case,
          // if its ultimate fate hasn't been decided yet, we may have to abort ourselves instead. (we can't simply abort it
          // because we could get stuck in an infinite loop if it never gets around to undoing its changes)
          int otherStatus = owningTransaction.status;
          if(owningTransaction.systemTransaction == null ||
             (otherStatus == Status.Committed || otherStatus == Status.Aborted))
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

        ISTMTransactionalValue transactional = writeEntry.NewValue as ISTMTransactionalValue;
        if(transactional != null) transactional.PrepareForWrite();

        alreadyLocked:;
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
      STMTransaction top = STMTransaction._current;
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

        _current = parent; // remove this transaction and all nested transactions
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

  #region ISinglePhaseNotification Members
  void IEnlistmentNotification.Commit(Enlistment enlistment)
  {
    if(enlistment == null) throw new ArgumentNullException();
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
      Dispose(); // dispose the transaction here, since it's managed automatically
      enlistment.Done();
    }
  }

  void IEnlistmentNotification.InDoubt(Enlistment enlistment)
  {
    // roll our changes back if the transaction is in doubt, to at least put the variables in a consistent state. although the
    // rest of the transaction might have completed successfully, we'll never find out. if we take no action, our changes won't
    // get committed -- the variables may be left temporarily in a locked state -- so we might as well roll them back to return
    // them to an unlocked state sooner
    if(enlistment == null) throw new ArgumentNullException();
    try
    {
      RollBack();
    }
    finally
    {
      Dispose(); // dispose the transaction here, since it's managed automatically
      enlistment.Done();
    }
  }

  void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
  {
    if(preparingEnlistment == null) throw new ArgumentNullException();
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
    if(enlistment == null) throw new ArgumentNullException();
    try
    {
      RollBack();
    }
    finally
    {
      Dispose(); // dispose the transaction here, since it's managed automatically
      enlistment.Done();
    }
  }

  void ISinglePhaseNotification.SinglePhaseCommit(SinglePhaseEnlistment enlistment)
  {
    if(enlistment == null) throw new ArgumentNullException();
    bool committed = false;
    try
    {
      committed = TryCommit();
      if(committed && postCommitActions != null)
      {
        if(parent == null) ExecutePostCommitActions();
        else MergePostCommitActions();
      }
    }
    finally
    {
      Dispose(); // dispose the transaction here, since it's managed automatically
      if(committed)
      {
        // if we have no changes to commit, call Done() to tell the transaction coordinator that we have read-only semantics.
        // otherwise, call Committed() to tell it that we committed some changes
        if(writeLog == null) enlistment.Done();
        else enlistment.Committed();
      }
      else
      {
        enlistment.Aborted();
      }
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
  Dictionary<TransactionalVariable, WriteEntry> writeLog;
  /// <summary>A queue holding actions to be executed after a successful top-level commit, or null if none have been enqueued.</summary>
  Queue<Action> postCommitActions;
  int preparedStatus, status;
  STMOptions options;
  bool removedFromStack;

  static STMTransaction GetUnsuppressedTransaction(STMTransaction transaction)
  {
    // if there happens to be an STM transaction above the suppressed system transaction, we can return that. otherwise, we'll return null
    STMTransaction unboundTransaction = null;
    while(true)
    {
      STMTransaction parent = transaction.parent;
      if(parent == null) break;
      if(parent.systemTransaction == null && transaction.systemTransaction != null) unboundTransaction = parent;
      transaction = parent;
    }
    return unboundTransaction;
  }

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

  // the counter can provide 10 billion IDs per second for 58 years before overflowing, which should be enough to ensure uniqueness
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

  /// <summary>Checks that this variable hasn't been changed by another transaction since it was opened. If it has, a
  /// <see cref="TransactionAbortedException"/> will be thrown.
  /// </summary>
  /// <include file="documentation.xml" path="/TX/STM/ConsistencyCheckRemarks/node()" />
  /// <exception cref="InvalidOperationException">Thrown if there is no current, active transaction.</exception>
  public void CheckConsistency()
  {
    if(!IsConsistent()) throw new TransactionAbortedException();
  }

  /// <summary>Checks that this variable hasn't been changed by another transaction since it was opened.</summary>
  /// <include file="documentation.xml" path="/TX/STM/ConsistencyCheckRemarks/node()" />
  /// <exception cref="InvalidOperationException">Thrown if there is no current, active transaction.</exception>
  public bool IsConsistent()
  {
    return GetTransaction().IsConsistent(this);
  }

  /// <include file="documentation.xml" path="/TX/STM/OpenForWrite/node()" />
  public object OpenForWrite()
  {
    return GetTransaction().OpenForWrite(this);
  }

  /// <include file="documentation.xml" path="/TX/STM/Read/node()" />
  public object Read()
  {
    STMTransaction transaction = STMTransaction.CurrentUnsuppressed;
    return transaction == null ? ReadCommitted() : transaction.OpenForRead(this);
  }

  /// <include file="documentation.xml" path="/TX/STM/ReadCommitted/node()" />
  public object ReadCommitted()
  {
    object value = this.value; // get the most recently committed value
    // if the variable is currently locked by some other transaction, get the old value from its log
    STMTransaction transaction = value as STMTransaction;
    if(transaction != null)
    {
      transaction = transaction.GetUnsuppressed();
      if(transaction != null) value = transaction.ReadWithoutOpening(this, false);
    }
    return value;
  }

  /// <include file="documentation.xml" path="/TX/STM/ReadWithoutOpening/node()" />
  public object ReadWithoutOpening()
  {
    STMTransaction transaction = STMTransaction.CurrentUnsuppressed;
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
  /// <para>As an example, imagine a sorted linked list <c>A -> B -> D -> E</c>. If C is inserted, then B will need to be
  /// updated. This change will cause a transaction that is searching for E to abort if it opened the preceding items (including
  /// B) for read access. However, the insertion and the search are not actually in conflict. Releasing variables to avoid this
  /// kind of false conflict can greatly increase scalability. The solution is the following:
  /// <list type="bullet">
  ///   <item><description>A transaction searching for an item need only keep the node currently being checked open for reading.</description></item>
  ///   <item><description>A transaction inserting an item needs to keep the node being modified open for writing and the
  ///   previous node open for reading. (While searching for the place to insert the item, it should keep the current and
  ///   previous nodes open for reading.)
  ///   </description></item>
  ///   <item><description>A transaction removing an item needs to have both the node being removed and the previous node open
  ///   for writing, but can release items before that.
  ///   </description></item>
  /// </list>
  /// This key concept in determining whether two operations really conflict is linearizability. Note that under this scheme, a
  /// search may begin for C, and concurrently, C may be inserted into the list. If the insert method returns before the search
  /// method, the search may report that nothing was found, despite C existing when the search returned! Graphically, the
  /// overlapping operations may look like this:
  /// <code>
  /// [Insert(C).............]
  /// .....[Search(C)..............]:false
  /// </code>
  /// However, this behavior is not incorrect. An operation is atomic if, over the course of its execution, there is a point when
  /// it appears to complete instantaneously, and crucially, that point does not have to be at the end of the operation. Consider
  /// the call sequence again, with possible linearization points (i.e. points when the operations appear to complete atomically)
  /// represented using pipe characters:
  /// <code>
  /// [Insert(C)...........|.]
  /// .....[Search(C).|............]:false
  /// </code>
  /// Now you can see that indeed the search could have linearized (i.e. completed) before the insertion. Even if the search did
  /// not in fact complete first, that is irrelevant. All that matters is that it is possible to place linearization points such
  /// that the results would be valid. More formally, a history -- a series of operation invocations and responses -- is
  /// sequential if the corresponding response for each invocation comes immediately after it in the history, and a history is
  /// linearizable if it can be reordered to form a sequential history, and that history is valid according to the definitions of
  /// the operations, and any response which preceded an invocation in the original history still precedes it in the reordered
  /// history. The history for the above sequence is: <c>Insert(C), Search(C), Insert returns, Search returns false</c>. It is
  /// possible to reorder this history into a linearization: <c>Search(C), Search returns false, Insert(C), Insert returns</c>,
  /// and therefore it is a valid result. (Note that <c>Insert(C), Insert returns, Search(C), Search returns false</c> is another
  /// possible serialization, but is not valid according to the definition of the operations, so it is not a linearization.)
  /// Because there exists at least one linearization of the history, the history is transactionally valid. This process of
  /// reordering histories is equivalent to the process of choosing linearization points in a graphical representation of the
  /// overlapping operations.
  /// </para>
  /// <para>
  /// The linearizability of the operations is what must be maintained when releasing variables as an optimization, not the exact
  /// linearization points. A data structure remains linearizable as long as all valid histories can be linearized. For a given
  /// structure, this is difficult to prove, but for our sorted linked list example, we can take an informal approach. First
  /// consider what is needed for a search. For a search to not find an item, all that matters is that there was a point during
  /// the search at which the item didn't exist. (It can be considered to have linearized at that point.) Only if the item
  /// existed for the entire time must the search return it, so what we have to show is that if the item exists the entire time,
  /// then the search will find it. The search algorithm would only fail to find it if it's prevented from reaching the item, and
  /// as normally written that would only happen if the current node is changed, preventing the search from getting to the next
  /// item. To prevent this, it suffices to keep the current node open, because changing a node requires modifying it. Note that
  /// search that occurs during insertion and removal (to find the insertion point or node to remove) must keep the previous node
  /// open as well, because insertion cannot have the insertion point disappear after it's found, and removal must be able to
  /// modify the previous node.
  /// </para>
  /// <para>Next, consider what is needed for an insertion to succeed. The nodes before one being inserted (i.e. B
  /// in A -> B -> D when inserting C) must not be removed or modified, and the node after (i.e. D) must not be deleted.
  /// Protecting B from deletion is accomplished by keeping A open. Protecting B from modification and D from removal are
  /// accomplished by keeping B open, so again only two nodes need to be kept open. Finally, consider what is necessary for
  /// removal to succeed. The node being removed and/or the previous node must be modified, and the subsequent node must not be
  /// removed, and nothing can be inserted immediately after the previous node or the node to be removed. Opening both the node
  /// to remove and the previous node ensures both of these conditions. If the next node was removed or something was inserted
  /// after the node being removed, it would require the modification of the node being removed, which is prevented by it being
  /// kept open. If something was to be inserted after the previous node, the previous node would need to be modified, and
  /// keeping it open prevents that. Therefore, all three operations can be supported by keeping only two nodes open at a time.
  /// </para>
  /// <para>Note that this analysis is only valid for a sorted structure. In a sorted linked list, seeing B -> D is enough to
  /// conclude that C does not exist, but in a regular linked list, or an array, C could be inserted anywhere. Consider a search
  /// for C in the following array: <c>J, B, F, C</c>, and the following history:
  /// <code>
  /// [Search(C).0..............1............2..............3.........]:false
  /// ..............[Set(0,C)]..................[Set(3,N)]
  /// </code>
  /// The item C exists the entire time, but a search that only keeps a small window open can miss it and report that it doesn't
  /// exist. More formally, this history is not linearizable: <c>Search(C), Set(0,C), Set(0,C) returns, Set(3,N), Set(3,N)
  /// returns, Search(C) returns false</c>. It can be serialized: <c>Search(C), Search(C) returns false, Set(0,C), Set(0,C)
  /// returns, Set(3,N), Set(3,N) returns</c>, however this is not valid because Search(C) should have returned true in this
  /// history. Therefore, a search over an unordered structure must be capable of detecting changes that insert the searched-for
  /// item prior to current item that the search is examining. In other words, a search that doesn't find an item in an
  /// unordered structure must check the consistency of every item when it commits. But if the search finds the item, it can then
  /// release all other items, because changes to them couldn't affect the conclusion that the item existed somewhere.
  /// </para>
  /// <para>
  /// This type of analysis may quickly grow complex, but is necessary to ensure correct usage of this method. If in doubt, don't
  /// release your variables.
  /// </para>
  /// </remarks>
  public void Release()
  {
    STMTransaction transaction = STMTransaction.CurrentUnsuppressed;
    if(transaction != null) transaction.Release(this);
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
  protected enum CloneType
  {
    /// <summary>The value will not be cloned at all. This is only suitable for immutable types.</summary>
    NoClone,
    /// <summary>The value can be cloned by unboxing and reboxing it. This is suitable for mutable value types.</summary>
    Rebox,
    /// <summary>The value should be cloned by using the value's <see cref="ICloneable"/> implementation.</summary>
    ICloneable
  }

  /// <include file="documentation.xml" path="/TX/STM/Set/node()" />
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
    lock(cloneTypes) return cloneTypes[type];
  }

  /// <summary>Ensures that the type is cloneable, and stores information about how to clone.</summary>
  protected static void ValidateCloneType(Type type)
  {
    // primitives, strings, and DBNull values are immutable and don't need to be cloned
    if(Type.GetTypeCode(type) != TypeCode.Object) return;

    lock(cloneTypes)
    {
      if(!cloneTypes.ContainsKey(type))
      {
        CloneType cloneType;
        if(type.GetCustomAttributes(typeof(STMImmutableAttribute), false).Length != 0) cloneType = CloneType.NoClone;
        else if(typeof(ICloneable).IsAssignableFrom(type)) cloneType = CloneType.ICloneable;
        else if(type.IsValueType && IsCopyable(type)) cloneType = CloneType.Rebox;
        else throw new NotSupportedException(type.FullName + " is not cloneable.");
        cloneTypes[type] = cloneType;
      }
    }
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

  /// <summary>Determines whether the given type, which is assumed to be a value type (i.e. struct), can be cloned by simply copying its
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
  // the counter can provide one billion IDs/second for 580 years before overflowing, which should be enough to ensure uniqueness
  static long idCounter;
}
#endregion

#region TransactionalVariable<T>
/// <summary>Represents a slot within transactional memory. To create a new transactional variable, construct an instance of this
/// type or call <see cref="STM.Allocate"/>.
/// </summary>
/// <include file="documentation.xml" path="/TX/STM/TVarRemarks/node()" />
public sealed class TransactionalVariable<T> : TransactionalVariable
{
  /// <summary>Allocates a new <see cref="TransactionalVariable{T}"/> with a default value.</summary>
  /// <include file="documentation.xml" path="/TX/STM/NewTVar/node()" />
  public TransactionalVariable() : this(default(T)) { }
  /// <summary>Allocates a new <see cref="TransactionalVariable{T}"/> with the given value.</summary>
  /// <include file="documentation.xml" path="/TX/STM/NewTVar/node()" />
  public TransactionalVariable(T initialValue) : base(initialValue)
  {
    ValidateCloneType(typeof(T)); // verify that the object can be cloned
  }

  /// <include file="documentation.xml" path="/TX/STM/OpenForWrite/node()" />
  public new T OpenForWrite()
  {
    return (T)base.OpenForWrite();
  }

  /// <include file="documentation.xml" path="/TX/STM/Read/node()" />
  public new T Read()
  {
    return (T)base.Read();
  }

  /// <include file="documentation.xml" path="/TX/STM/ReadCommitted/node()" />
  public new T ReadCommitted()
  {
    return (T)base.ReadCommitted();
  }

  /// <include file="documentation.xml" path="/TX/STM/ReadWithoutOpening/node()" />
  public new T ReadWithoutOpening()
  {
    return (T)base.ReadWithoutOpening();
  }

  /// <include file="documentation.xml" path="/TX/STM/Set/node()" />
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
