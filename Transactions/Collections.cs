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

using System;
using System.Collections.Generic;
using System.Transactions;
using AdamMil.Collections;
using AdamMil.Utilities;

namespace AdamMil.Transactions
{

#region TransactionalArray
/// <summary>Implements an array whose members are transactional variables. The array does not support methods that add or remove
/// individual items, but the array can be enlarged by calling <see cref="Enlarge"/>.
/// </summary>
public sealed class TransactionalArray<T> : IList<T>
{
  /// <summary>Initializes a new <see cref="TransactionalArray{T}"/> with the given number of elements.</summary>
  public TransactionalArray(int length) : this(length, null) { }
  /// <summary>Initializes a new <see cref="TransactionalArray{T}"/> with the given number of elements, and using the given
  /// <see cref="IComparer{T}"/> to compare elements.
  /// </summary>
  public TransactionalArray(int length, IEqualityComparer<T> comparer)
  {
    if(length < 0) throw new ArgumentOutOfRangeException();
    array = new TransactionalVariable<T>[length];
    for(int i=0; i<array.Length; i++) array[i] = new TransactionalVariable<T>();

    this.comparer = comparer == null ? EqualityComparer<T>.Default :  comparer;
  }

  /// <summary>Initializes a new <see cref="TransactionalArray{T}"/> with the given set of items.</summary>
  public TransactionalArray(IEnumerable<T> initialItems) : this(initialItems, null) { }
  /// <summary>Initializes a new <see cref="TransactionalArray{T}"/> with the given set of items, and using the given
  /// <see cref="IComparer{T}"/> to compare elements.
  /// </summary>
  public TransactionalArray(IEnumerable<T> initialItems, IEqualityComparer<T> comparer)
  {
    if(initialItems == null) throw new ArgumentNullException();
    ICollection<T> collection = initialItems as ICollection<T>;
    if(collection == null) collection = new List<T>(initialItems);

    array = new TransactionalVariable<T>[collection.Count];
    int index = 0;
    foreach(T item in initialItems) array[index++] = new TransactionalVariable<T>(item);
    if(index != array.Length) throw new ArgumentException();

    this.comparer = comparer == null ? EqualityComparer<T>.Default :  comparer;
  }

  /// <summary>Determines whether the given item exists in a given range within the array.</summary>
  public bool Contains(T item, int index, int count)
  {
    Utility.ValidateRange(Count, index, count);
    return STM.Retry(delegate
    {
      for(int i=index, end=index+count; i<end; i++)
      {
        if(comparer.Equals(item, array[i].Read())) // if an item is found, changes to previous items won't affect the result,
        {                                          // so we can release them to avoid false conflicts
          while(i > index) array[--i].Release();
          return true;
        }
      }
      return false;
    });
  }

  /// <summary>Copies a specified number of items into an array.</summary>
  public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int count)
  {
    Utility.ValidateRange(Count, sourceIndex, count);
    Utility.ValidateRange(destination, destinationIndex, count);
    STM.Retry(delegate
    {
      for(int i=0; i<count; i++) destination[destinationIndex+i] = array[sourceIndex+i].Read();
    });
  }

  /// <summary>Enlarges the array to accomodate the given number of elements. Transactions accessing existing elements of the
  /// current array will not be disturbed (although the value of the <see cref="Count"/> property will be seen to increase). If
  /// the number is less than the current size, the array will not be shrunk.
  /// This method is not transactional, but is thread-safe.
  /// </summary>
  public void Enlarge(int newSize)
  {
    if(newSize < 0) throw new ArgumentOutOfRangeException();
    lock(this)
    {
      if(newSize > array.Length)
      {
        TransactionalVariable<T>[] newArray = new TransactionalVariable<T>[newSize];
        Array.Copy(array, newArray, array.Length);
        for(int i=array.Length; i < newArray.Length; i++) newArray[i] = new TransactionalVariable<T>();
        array = newArray;
      }
    }
  }

  /// <summary>Gets an <see cref="IEnumerator{T}"/> that enumerates items within the given range.</summary>
  public IEnumerator<T> GetEnumerator(int index, int count)
  {
    Utility.ValidateRange(Count, index, count);
    return new Enumerator(array, index, count);
  }

  /// <summary>Returns the index of the first item in the given region of the array equal to the given item, or -1 if no such
  /// item could be found.
  /// </summary>
  public int IndexOf(T item, int index, int count)
  {
    Utility.ValidateRange(Count, index, count);
    return STM.Retry(delegate
    {
      for(int end=index+count; index<end; index++)
      {
        if(comparer.Equals(item, array[index].Read())) return index;
      }
      return -1;
    });
  }

  #region IList<T> Members
  /// <inheritdoc/>
  public T this[int index]
  {
    get { return array[index].Read(); }
    set { STM.Retry(delegate { array[index].Set(value); }); }
  }

  /// <inheritdoc/>
  public int IndexOf(T item)
  {
    return IndexOf(item, 0, Count);
  }

  void IList<T>.Insert(int index, T item)
  {
    throw new NotSupportedException();
  }

  void IList<T>.RemoveAt(int index)
  {
    throw new NotSupportedException();
  }
  #endregion

  #region ICollection<T> Members
  /// <inheritdoc/>
  public int Count
  {
    get { return array.Length; }
  }

  /// <inheritdoc/>
  public bool IsReadOnly
  {
    get { return false; }
  }

  /// <inheritdoc/>
  public bool Contains(T item)
  {
    return Contains(item, 0, Count);
  }

  /// <inheritdoc/>
  public void CopyTo(T[] array, int arrayIndex)
  {
    CopyTo(0, array, arrayIndex, Count);
  }

  void ICollection<T>.Add(T item)
  {
    throw new NotSupportedException();
  }

  void ICollection<T>.Clear()
  {
    throw new NotSupportedException();
  }

  bool ICollection<T>.Remove(T item)
  {
    throw new NotSupportedException();
  }
  #endregion

  #region IEnumerable<T> Members
  /// <inheritdoc/>
  public IEnumerator<T> GetEnumerator()
  {
    return new Enumerator(array, 0, array.Length);
  }
  #endregion

  #region IEnumerable Members
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
  #endregion

  #region Enumerator
  sealed class Enumerator : IEnumerator<T>
  {
    public Enumerator(TransactionalVariable<T>[] array, int start, int count)
    {
      this.array = array;
      this.start = start;
      this.end   = start + count;
      index      = start-1;
    }

    public T Current
    {
      get
      {
        if(index < start || index >= end) throw new InvalidOperationException();
        return current;
      }
    }

    public bool MoveNext()
    {
      if(index == end) return false;
      if(++index == end)
      {
        current = default(T);
        return false;
      }
      else
      {
        current = array[index].Read();
        return true;
      }
    }

    public void Reset()
    {
      index = start-1;
    }

    object System.Collections.IEnumerator.Current
    {
      get { return Current; }
    }

    void IDisposable.Dispose() { }

    readonly TransactionalVariable<T>[] array;
    T current;
    readonly int start, end;
    int index;
  }
  #endregion

  TransactionalVariable<T>[] array;
  readonly IEqualityComparer<T> comparer;
}
#endregion

#region TransactionalCache
/// <summary>Implements a transactional cache. The cache can be read and written both inside and outside of a
/// <see cref="System.Transactions.Transaction"/>. If the cache is modified within a transaction, the changes will not be visible to other
/// threads until the transaction is committed. If modified outside a transaction, changes will be visible immediately. If multiple
/// transactions modify the cache, the changes will not conflict and will not cause a rollback, but will be applied in the order that the
/// transactions commit.
/// </summary>
/// <threadsafety static="true" instance="true" />
public sealed class TransactionalCache<TKey,TValue>
{
  /// <summary>Initializes a new <see cref="TransactionalCache{TKey,TValue}"/>. New items will always be merged into the cache.</summary>
  public TransactionalCache()
  {
    cache = new DictionaryCache<TKey, TValue>();
  }

  /// <summary>Initializes a new <see cref="TransactionalCache{TKey,TValue}"/>, using the given <see cref="IEqualityComparer{TKey}"/> to compare keys.
  /// New items will always be merged into the cache.
  /// </summary>
  public TransactionalCache(IEqualityComparer<TKey> keyComparer)
  {
    comparer = keyComparer;
    cache    = new DictionaryCache<TKey, TValue>(keyComparer);
  }

  /// <summary>Initializes a new <see cref="TransactionalCache{TKey,TValue}"/>. New items will be merged into the cache if they are greater than or
  /// equal to the existing items, according to <paramref name="valueMerger"/>.
  /// </summary>
  public TransactionalCache(Comparison<TValue> valueMerger) : this()
  {
    this.valueMerger = valueMerger;
  }

  /// <summary>Initializes a new <see cref="TransactionalCache{TKey,TValue}"/>, using the given <see cref="IEqualityComparer{TKey}"/> to
  /// compare keys. New items will be merged into the cache if they are greater than or equal to the existing items, according to
  /// <paramref name="valueMerger"/>.
  /// </summary>
  public TransactionalCache(IEqualityComparer<TKey> keyComparer, Comparison<TValue> valueMerger) : this(keyComparer)
  {
    this.valueMerger = valueMerger;
  }

  /// <summary>Sets the value of the given key within the cache.</summary>
  public TValue this[TKey key]
  {
    set { Set(key, value); }
  }

  /// <summary>Gets a reference to the internal cache, which may be used to configure its timeouts, etc.</summary>
  public DictionaryCache<TKey, TValue> Cache
  {
    get { return cache; }
  }

  /// <summary>Checks whether the cache contains the given key.</summary>
  public bool ContainsKey(TKey key)
  {
    TransactionState state = GetReadState();
    return state == null ? cache.ContainsKey(key) : state.ContainsKey(key);
  }

  /// <summary>Removes the value with the given key from the cache. This method does not need to be called within the context of a
  /// <see cref="System.Transactions.Transaction"/>, but if there is a current transaction, it will be used.
  /// </summary>
  public bool Remove(TKey key)
  {
    TransactionState state = GetWriteState();
    return state == null ? cache.Remove(key) : state.Remove(key);
  }

  /// <summary>Sets the value of the given key within the cache. If an entry with the given key already exists, its expiration time will
  /// be reset. This method does not need to be called within the context of a <see cref="System.Transactions.Transaction"/>, but if there
  /// is a current transaction, it will be used.
  /// </summary>
  public void Set(TKey key, TValue value)
  {
    TransactionState state = GetWriteState();
    if(state == null) Merge(key, value);
    else state.Set(key, value);
  }

  /// <summary>Attempts to retrieve the value with the given key from the cache.</summary>
  public bool TryGetValue(TKey key, out TValue value)
  {
    TransactionState state = GetReadState();
    return state == null ? cache.TryGetValue(key, out value) : state.TryGetValue(key, out value);
  }

  #region TransactionState
  sealed class TransactionState : ISinglePhaseNotification
  {
    public TransactionState(TransactionalCache<TKey,TValue> owner, Transaction transaction)
    {
      this.owner       = owner;
      this.transaction = transaction;
      transaction.EnlistVolatile(this, EnlistmentOptions.None);
    }

    public bool ContainsKey(TKey key)
    {
      if(removed != null && removed.Contains(key)) return false;
      else if(addedOrEdited != null && addedOrEdited.ContainsKey(key)) return true;
      else return owner.cache.ContainsKey(key);
    }

    public bool Remove(TKey key)
    {
      bool wasRemoved = false;

      if(addedOrEdited != null && addedOrEdited.Remove(key))
      {
        if(addedOrEdited.Count == 0) addedOrEdited = null;
        wasRemoved = true;
      }

      if(removed == null) removed = new HashSet<TKey>(owner.comparer);
      wasRemoved |= removed.Add(key);

      return wasRemoved;
    }

    public void Set(TKey key, TValue value)
    {
      if(addedOrEdited == null) addedOrEdited = new Dictionary<TKey, TValue>(owner.comparer);
      addedOrEdited[key] = value;
      if(removed != null && removed.Remove(key) && removed.Count == 0) removed = null;
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
      if(removed != null && removed.Contains(key))
      {
        value = default(TValue);
        return false;
      }

      if(addedOrEdited != null && addedOrEdited.TryGetValue(key, out value))
      {
        return true;
      }
      else
      {
        return owner.cache.TryGetValue(key, out value);
      }
    }

    void Commit()
    {
      // merge the state into the cache
      if(addedOrEdited != null)
      {
        foreach(KeyValuePair<TKey, TValue> pair in addedOrEdited) owner.Merge(pair.Key, pair.Value);
      }
      if(removed != null)
      {
        foreach(TKey key in removed) owner.cache.Remove(key);
      }
      OnTransactionComplete();
    }

    void OnTransactionComplete()
    {
      if(transaction != null) // remove the transaction from the map of transactions to states
      {
        lock(owner.transactionStates) owner.transactionStates.Remove(transaction);
        transaction = null;
      }
    }

    #region ISinglePhaseNotification Members
    void IEnlistmentNotification.Commit(Enlistment enlistment)
    {
      if(enlistment == null) throw new ArgumentNullException();
      Commit();
      enlistment.Done();
    }

    void IEnlistmentNotification.InDoubt(Enlistment enlistment)
    {
      if(enlistment == null) throw new ArgumentNullException();
      OnTransactionComplete();
      enlistment.Done();
    }

    void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
    {
      if(preparingEnlistment == null) throw new ArgumentNullException();
      preparingEnlistment.Prepared();
    }

    void IEnlistmentNotification.Rollback(Enlistment enlistment)
    {
      if(enlistment == null) throw new ArgumentNullException();
      OnTransactionComplete();
      enlistment.Done();
    }

    void ISinglePhaseNotification.SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
    {
      if(singlePhaseEnlistment == null) throw new ArgumentNullException();
      Commit();
      singlePhaseEnlistment.Committed();
    }
    #endregion

    Dictionary<TKey,TValue> addedOrEdited;
    HashSet<TKey> removed;
    readonly TransactionalCache<TKey, TValue> owner;
    Transaction transaction;
  }
  #endregion

  TransactionState GetReadState()
  {
    Transaction transaction = Transaction.Current;
    return transaction == null ? null : (TransactionState)transactionStates[transaction];
  }

  TransactionState GetWriteState()
  {
    TransactionState state = null;
    Transaction transaction = Transaction.Current;
    if(transaction != null)
    {
      state = (TransactionState)transactionStates[transaction];
      if(state == null)
      {
        lock(transactionStates)
        {
          // if the cache must be thread-safe, check to see if another thread snuck in and created the transaction state already
          if(cache.ThreadSafe) state = (TransactionState)transactionStates[transaction];
          if(state == null) transactionStates[transaction] = state = new TransactionState(this, transaction);
        }
      }
    }
    return state;
  }

  void Merge(TKey key, TValue value)
  {
    TValue existingValue;
    if(valueMerger == null || !cache.TryGetValue(key, out existingValue) ||
       !object.Equals(key, value) && valueMerger(value, existingValue) >= 0)
    {
      cache.Set(key, value);
    }
  }

  readonly DictionaryCache<TKey, TValue> cache;
  readonly System.Collections.Hashtable transactionStates = new System.Collections.Hashtable();
  readonly Comparison<TValue> valueMerger;
  readonly IEqualityComparer<TKey> comparer;
}
#endregion

} // namespace AdamMil.Transactions
