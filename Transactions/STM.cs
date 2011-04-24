﻿/*
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

// see the OSTM description within "Concurrent programming without locks" [Fraser and Harris, 2007], and
// also "Composable Memory Transactions" [Harris, Marlow, Jones, and Herlihy, 2006]

// TODO: add spin waits to the CAS (CompareAndExchange) loops when we upgrade to .NET 4

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Transactions;
using AdamMil.Utilities;

namespace AdamMil.Transactions
{

#region STM
/// <summary>Provides convenience methods for working with software transactional memory. This is equivalent to
/// constructing a <see cref="TransactionalVariable{T}"/> using its constructor.
/// </summary>
public static class STM
{
  /// <summary>Allocates and returns a new <see cref="TransactionalVariable{T}"/> with a default value.</summary>
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
}
#endregion

#region STMImmutable
/// <summary>An attribute that can be applied to a type to designate that it is immutable as far as STM is concerned, so it need
/// not be copied when a variable of that type is opened in read/write mode, and need not implement <see cref="ICloneable"/>.
/// (And even if it does implement <see cref="ICloneable"/>, it will not be called by the STM system.) This attribute can also be
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
/// objects. To commit the transaction, call <see cref="Commit"/>. The transaction will be rolled back if <see cref="Dispose"/>
/// is called, and the transaction has not already been committed. Typically, a transaction should be used in a <c>using</c>
/// statement to ensure that it's either committed or rolled back.
/// </summary>
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
  /// back, etc).
  /// </exception>
  /// <exception cref="TransactionAbortedException">Thrown if the attempt to commit the transaction failed because of a conflict
  /// with another transaction.
  /// </exception>
  public void Commit()
  {
    if(!TryCommit()) throw new TransactionAbortedException();
  }

  /// <summary>Disposes the transaction, removing it from the transaction stack.</summary>
  public void Dispose()
  {
    RemoveFromStack();
  }

  /// <summary>Gets the current <see cref="STMTransaction"/> for this thread.</summary>
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
  /// <param name="useExisting">If false, a new transaction will always be created. If true, a new transaction will only be
  /// created if there is no current transaction.
  /// </param>
  public static STMTransaction Create(bool useExisting)
  {
    STMTransaction transaction = Current;

    // if there's a current system transaction, enlist in it if we haven't already done so
    Transaction systemTransaction = Transaction.Current;
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
      if(!found)
      {
        transaction = new STMTransaction(transaction, systemTransaction);
        Current = transaction;
      }
    }

    if(!useExisting || transaction == null)
    {
      transaction = new STMTransaction(transaction);
      Current = transaction;
    }
    return transaction;
  }

  internal object OpenForRead(TransactionalVariable variable)
  {
    AssertActive();

    // search the this transaction and enclosing transactions to try to find a log entry for the variable
    object value;
    STMTransaction transaction = this;
    do
    {
      if(transaction.TryRead(variable, out value)) return value;
      transaction = transaction.parent;
    } while(transaction != null);

    // if no log entry exists, get the value of the variable, add it to the read log, and return it
    value = GetCommittedValue(variable);
    readLog.Add(variable, value);
    return value;
  }

  internal object OpenForWrite(TransactionalVariable variable)
  {
    AssertActive();
    // if the variable has been opened for writing by this transaction, return the current value from the write log
    WriteEntry entry;
    if(writeLog != null && writeLog.TryGetValue(variable, out entry)) return entry.NewValue;

    // otherwise, create a new write entry
    return CreateWriteEntry(variable, null, false).NewValue;
  }

  internal object ReadWithoutOpening(TransactionalVariable variable, bool useNewValue)
  {
    // search the this transaction and enclosing transactions to try to find a log entry for the variable
    object value;
    for(STMTransaction transaction = this; transaction != null; transaction = transaction.parent)
    {
      // if the variable has already been opened for reading, and has not subsequently been opened for writing, then just use
      // its current value from the read log
      if(transaction.readLog.TryGetValue(variable, out value)) return value;

      // if the variable has been opened for writing, return its current value from the write log
      WriteEntry entry;
      if(transaction.writeLog != null && transaction.writeLog.TryGetValue(variable, out entry))
      {
        return !useNewValue || transaction.status == Status.Aborted ? entry.OldValue : entry.NewValue;
      }
    }

    // otherwise, return the current value, or null if it's currently locked by another transaction
    value = variable.value;
    STMTransaction owningTransaction = value as STMTransaction;
    return owningTransaction == null ? value : owningTransaction.ReadWithoutOpening(variable, false);
  }

  internal void Set(TransactionalVariable variable, object value)
  {
    AssertActive();
    // if it already exists in the write log, overwrite the new value. otherwise, create a new entry
    WriteEntry entry;
    if(writeLog != null && writeLog.TryGetValue(variable, out entry)) entry.NewValue = value;
    else CreateWriteEntry(variable, value, true);
  }

  #region Status
  static class Status
  {
    /// <summary>The transaction is still active and its status has not been determined.</summary>
    public const int Undetermined=0;
    /// <summary>The transaction is in the process of committing, and is checking its read variables for conflicts.</summary>
    public const int ReadCheck=1;
    /// <summary>The transaction has been successfully committed.</summary>
    public const int Committed=2;
    /// <summary>The transaction has been aborted.</summary>
    public const int Aborted=3;
  }
  #endregion

  #region VariableComparer
  /// <summary>Compares <see cref="TransactionalVariable"/> objects by their IDs.</summary>
  sealed class VariableComparer : IComparer<TransactionalVariable>
  {
    VariableComparer() { }

    public int Compare(TransactionalVariable a, TransactionalVariable b)
    {
      return a.id < b.id ? -1 : a == b ? 0 : 1;
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
    WriteEntry entry;
    entry = new WriteEntry();

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

    // clone the old value into a private copy to create the new value, and add the entry to the write log
    entry.NewValue = useNewValue ? newValue : variable.Clone(entry.OldValue);
    if(writeLog == null) writeLog = new SortedDictionary<TransactionalVariable, WriteEntry>(VariableComparer.Instance);
    writeLog.Add(variable, entry);
    return entry;
  }

  void FinishCommit()
  {
    if(preparedStatus == Status.Undetermined) throw new InvalidOperationException();

    int currentStatus;
    while(true)
    {
      currentStatus = status;
      // if the final state has already been set (i.e. it's equal to Committed or Aborted), then we can't change it.
      if(currentStatus == Status.Committed || currentStatus == Status.Aborted) break;
      Interlocked.CompareExchange(ref status, preparedStatus, currentStatus); // try to set the final state
    }

    // now unlock everything we've locked, either committing our change or rolling it back. we don't need to do this in a finally
    // block because later transactions, if they see our dangling lock, will try to help us commit again, so we'll get a second
    // chance to unlock
    if(writeLog != null)
    {
      if(parent == null || currentStatus != Status.Committed) // if this is a top-level or aborted transaction...
      {
        foreach(KeyValuePair<TransactionalVariable, WriteEntry> pair in writeLog) // commit or roll back the changes directly
        {
          Interlocked.CompareExchange(ref pair.Key.value,
                                      currentStatus == Status.Committed ? pair.Value.NewValue : pair.Value.OldValue, this);
        }
      }
      else if(parent.status != Status.Undetermined) // make sure the parent transaction hasn't finished yet
      {
        throw new TransactionAbortedException("The parent transaction has already finished.");
      }
      else // otherwise, this is a successful nested transaction, so commit into the parent transaction
      {
        foreach(KeyValuePair<TransactionalVariable, WriteEntry> pair in writeLog)
        {
          parent.Set(pair.Key, pair.Value.NewValue); // add the new value to the parent's write log
        }
      }
    }
  }

  /// <summary>Retrieves the current committed value from a <see cref="TransactionalVariable"/>.</summary>
  object GetCommittedValue(TransactionalVariable variable)
  {
    object value = variable.value; // try to get the value

    STMTransaction owningTransaction = value as STMTransaction;
    if(owningTransaction != null) // if the variable is currently locked for commit by another transaction...
    {
      WriteEntry entry = owningTransaction.writeLog[variable]; // then grab the entry from the owning transaction's write log
      if(owningTransaction.status == Status.ReadCheck) // if the other transaction is performing its read check...
      {
        // if we aren't performing our read check yet, then they started to commit first, so we should help them commit.
        // otherwise, we are both trying to commit, so help them commit only if they started executing first (i.e. have a lower
        // id). (it's not actually critical that they started executing first. it's only important that there exists some global
        // ordering of transactions that can be used to resolve conflicts)
        if(status != Status.ReadCheck || id > owningTransaction.id) owningTransaction.TryCommit();
        else Interlocked.CompareExchange(ref owningTransaction.status, Status.Aborted, Status.ReadCheck);
      }
      // if the owning transaction committed directly to the variable, return the new value. otherwise, return the old value
      value = owningTransaction.status == Status.Committed && owningTransaction.parent == null ? entry.NewValue : entry.OldValue;
    }
    return value;
  }

  void PrepareCommit()
  {
    int newStatus = Status.Aborted; // assume that the transaction will abort

    // acquire "locks" on all of the variables opened for writing by replacing their values with a pointer to the current
    // transaction. the variables are locked in order by ID. but we only need to do it if this isn't a nested transaction. in
    // fact, locking may fail in a nested transaction if it and an enclosing transaction both set the same variable, because it
    // will think the variable has been committed by another transaction, due to the old value (taken from the enclosing
    // transaction) not matching the currently committed value
    if(parent == null && writeLog != null) // if we need to commit changes directly to the variables...
    {
      foreach(KeyValuePair<TransactionalVariable, WriteEntry> pair in writeLog)
      {
        while(true)
        {
          // replace the variable's value with a pointer to the transaction if another transaction hasn't committed a change
          object value = Interlocked.CompareExchange(ref pair.Key.value, this, pair.Value.OldValue);
          // if we just locked it, or it was already locked, then we're done with this variable. otherwise, it couldn't be locked
          if(value == pair.Value.OldValue || value == this) break;
          STMTransaction owningTransaction = value as STMTransaction;
          if(owningTransaction == null) goto decide; // another transaction committed a change already, so we fail
          // another transaction already has it locked for committing, so help that transaction commit and then check again.
          // if the transaction aborted, it may be available to us
          owningTransaction.TryCommit();
        }
      }
    }

    // move from the Undetermined phase to the ReadCheck phase. this is done with CAS because of the reentrancy requirement
    Interlocked.CompareExchange(ref status, Status.ReadCheck, Status.Undetermined);

    // go through all the variables opened for reading and check if they've been changed by another transaction. unlike the check
    // during locking, above, we don't have to worry about variables changed by an enclosing transaction because in that case
    // they won't have been added to our read log. rather, they'd have been read directly from the enclosing transaction's
    // write log
    foreach(KeyValuePair<TransactionalVariable, object> pair in readLog)
    {
      if(GetCommittedValue(pair.Key) != pair.Value) goto decide; // if a change has been committed by another transaction, we fail
    }

    // at this point, it looks like we're going to succeed, but it's still possible that another transaction will abort us
    newStatus = Status.Committed;

    decide:
    int currentStatus;
    while(true)
    {
      currentStatus = preparedStatus;
      // if the prepared state has already been set (i.e. it's equal to Committed or Aborted), then we can't change it.
      if(currentStatus == Status.Committed || currentStatus == Status.Aborted) break;
      Interlocked.CompareExchange(ref preparedStatus, newStatus, currentStatus); // try to set the prepared status
    }
  }

  /// <summary>Removes the transaction from the transaction stack.</summary>
  void RemoveFromStack()
  {
    if(!rolledBack)
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

      if(!found) throw new InvalidOperationException("This is not a current transaction for this thread.");

      // abort transactions that haven't been committed
      while(true) // for this transaction and nested transactions...
      {
        while(true) // while we haven't been able to set the status...
        {
          int status = top.status;
          if(status == Status.Committed || status == Status.Aborted) break;
          Interlocked.CompareExchange(ref top.status, Status.Aborted, status);
        }
        if(top == this) break;
        top = top.parent;
      }

      STMTransaction.Current = parent;
      rolledBack = true;
    }
  }

  // NOTE: this method needs to be reentrant from multiple threads, because transactions can help each other commit by calling
  // each other's CommitCore() methods, and the user can also call Commit()
  bool TryCommit()
  {
    if(preparedStatus == Status.Undetermined) PrepareCommit();
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
    if(readLog.TryGetValue(variable, out value)) return true;

    // if the variable has been opened for writing, return its current value from the write log
    WriteEntry entry;
    if(writeLog != null && writeLog.TryGetValue(variable, out entry))
    {
      value = entry.NewValue;
      return true;
    }

    return false;
  }

  readonly ulong id;
  readonly STMTransaction parent;
  readonly Transaction systemTransaction;
  readonly Dictionary<TransactionalVariable, object> readLog = new Dictionary<TransactionalVariable, object>();
  SortedDictionary<TransactionalVariable, WriteEntry> writeLog;
  int status, preparedStatus;
  bool rolledBack;

  [ThreadStatic] static STMTransaction _current;

  static long nextId;

  #region IEnlistmentNotification Members
  void IEnlistmentNotification.Commit(Enlistment enlistment)
  {
    TryCommit();
    enlistment.Done();
  }

  void IEnlistmentNotification.InDoubt(Enlistment enlistment)
  {
    enlistment.Done();
  }

  void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
  {
    PrepareCommit();
    if(preparedStatus == Status.Committed) preparingEnlistment.Prepared();
    else preparingEnlistment.ForceRollback();
  }

  void IEnlistmentNotification.Rollback(Enlistment enlistment)
  {
    if(status != Status.Committed && status != Status.Aborted)
    {
      preparedStatus = Status.Aborted;
      FinishCommit();
    }
  }
  #endregion
}
#endregion

#region TransactionalVariable
/// <summary>Represents a slot within transactional memory. To create a new transactional variable, either construct an instance
/// of <see cref="TransactionalVariable{T}"/> or call <see cref="STM.Allocate"/>.
/// </summary>
public abstract class TransactionalVariable
{
  internal TransactionalVariable(object initialValue)
  {
    id    = (ulong)Interlocked.Increment(ref nextId);
    value = initialValue;
  }

  /// <summary>Opens the variable for read/write access and returns the current value. This method is meant to be used when the
  /// variable's value needs to be read before writing it, or when it is an object whose methods and properties will be used to
  /// mutate it. To replace the value completely, call <see cref="Set"/>.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown if there is no current <see cref="STMTransaction"/> on this thread, or
  /// if the current value's implementation of <see cref="ICloneable"/> is incorrect.
  /// </exception>
  public object OpenForWrite()
  {
    return GetTransaction().OpenForWrite(this);
  }

  /// <summary>Opens the variable for read access and returns the current value. You must not call any methods or properties on
  /// the returned object that would change it, without opening it in read/write mode (by calling <see cref="OpenForWrite"/>)
  /// first! And if the variable will later be opened for read/write access, it is more efficient to just open it once, for
  /// read/write access, rather than opening it for read access and later reopening it for read/write access.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown if there is no current <see cref="STMTransaction"/> on this thread.</exception>
  public object Read()
  {
    return GetTransaction().OpenForRead(this);
  }

  /// <summary>Gets the string representation of the <see cref="TransactionalVariable"/>'s value for the current thread. If there
  /// is no current <see cref="STMTransaction"/> on the thread, the most recently committed value will be returned.
  /// </summary>
  public override string ToString()
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
    return value == null ? "" : value.ToString(); // now finally call .ToString() on the value
  }

  /// <summary>Indicates how a value will be cloned when a variable is opened in read/write mode.</summary>
  internal enum CloneType : byte
  {
    /// <summary>The value does not need to be cloned at all. This is only suitable for immutable types.</summary>
    NoClone,
    /// <summary>The value can be cloned by unboxing and reboxing it. This is suitable for mutable value types.</summary>
    Rebox,
    /// <summary>The value should be cloned by using the value's <see cref="ICloneable"/> implementation.</summary>
    ICloneable
  }

  /// <summary>Called to clone a value from this variable. The clone should be as deep as necessary to ensure that the given
  /// value cannot be changed using the referenced returned, but need not be any deeper. If the value is already immutable, it
  /// may simply be returned as-is.
  /// </summary>
  internal abstract object Clone(object value);

  /// <summary>Opens the variable for writing and sets its value to the given value. This method is meant to be used when the
  /// variable's value will be replaced completely. To alter a mutable object through its methods and properties, use
  /// <see cref="OpenForWrite"/> to return the current instance of it.
  /// </summary>
  // NOTE: this is not public because it would allow type safety to be broken (e.g. non-T stored in TransactionalVariable<T>)
  internal void Set(object newValue)
  {
    GetTransaction().Set(this, newValue);
  }

  /// <summary>The unique ID of this transactional variable, used to achieve a total order on all transactional variables.</summary>
  internal readonly ulong id;
  /// <summary>The most recently committed value, or a reference to the transaction currently committing this .</summary>
  internal object value;

  /// <summary>Returns a <see cref="CloneType"/> value indicating how the type should be cloned. It is assumed that the type is
  /// cloneable (i.e. that <see cref="ValidateCloneType"/> has already been called on it).
  /// </summary>
  internal static CloneType GetCloneType(Type type)
  {
    if(Type.GetTypeCode(type) != TypeCode.Object) return CloneType.NoClone;
    typeLock.EnterReadLock();
    try { return cloneTypes[type]; }
    finally { typeLock.ExitReadLock(); }
  }

  /// <summary>Ensures that the type is cloneable, and stores information about how to clone it in a cache.</summary>
  internal static void ValidateCloneType(Type type)
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
  /// <exception cref="InvalidOperationException">Thrown if there is no current <see cref="STMTransaction"/> on this thread, or
  /// if the current value's implementation of <see cref="ICloneable"/> is incorrect.
  /// </exception>
  public new T OpenForWrite()
  {
    return (T)base.OpenForWrite();
  }

  /// <summary>Opens the variable for read access and returns the current value. You must not call any methods or properties on
  /// the returned object that would change it, without opening it in read/write mode (by calling <see cref="OpenForWrite"/>)
  /// first! And if the variable will later be opened for read/write access, it is more efficient to just open it once, for
  /// read/write access, rather than opening it for read access and later reopening it for read/write access.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown if there is no current <see cref="STMTransaction"/> on this thread.</exception>
  public new T Read()
  {
    return (T)base.Read();
  }

  /// <summary>Opens the variable for writing and sets its value to the given value. This method is meant to be used when the
  /// variable's value will be replaced completely. To alter a mutable object through its methods and properties, use
  /// <see cref="OpenForWrite"/> to return the current instance of it.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown if there is no current <see cref="STMTransaction"/> on this thread, or
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
