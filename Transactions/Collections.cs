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
using AdamMil.Utilities;

// TODO: recheck the performance of MemberwiseClone() versus manual cloning to see if it's improved in .NET 4
// TODO: use Release() to improve performance of the collections, if possible

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

#region TransactionalDictionary
/// <summary>Provides a transactional hash table, meant to be used with <see cref="STMTransaction"/> or
/// <see cref="System.Transactions.Transaction"/>.
/// </summary>
/// <remarks>The <see cref="TransactionalDictionary{K,V}"/> is meant to provide a full-feature implementation of the standard
/// <see cref="IDictionary{K,V}"/> interface, and is not particularly scalable with regard to concurrent writes. In particular,
/// due to the need to keep track of the <see cref="ICollection{T}.Count"/> of items in the hash table, all additions and
/// removals will conflict with each other, even if they don't alter the same buckets within the hash table, because all must
/// update the item count.
/// </remarks>
public class TransactionalDictionary<K, V> : IDictionary<K, V>
{
  /// <summary>Initializes a new, empty <see cref="TransactionalDictionary{K,V}"/>.</summary>
  public TransactionalDictionary() : this(0, null) { }
  /// <summary>Initializes a new, empty <see cref="TransactionalDictionary{K,V}"/> with the given capacity.</summary>
  public TransactionalDictionary(int initialCapacity) : this(initialCapacity, null) { }
  /// <summary>Initializes a new, empty <see cref="TransactionalDictionary{K,V}"/> with the given
  /// <see cref="IEqualityComparer{T}"/>, which will be used to compare keys for equality.
  /// </summary>
  public TransactionalDictionary(IEqualityComparer<K> comparer) : this(0, comparer) { }
  /// <summary>Initializes a new, empty <see cref="TransactionalDictionary{K,V}"/> with the given capacity and the given
  /// <see cref="IEqualityComparer{T}"/>, which will be used to compare keys for equality.
  /// </summary>
  public TransactionalDictionary(int initialCapacity, IEqualityComparer<K> comparer)
  {
    if(initialCapacity < 0) throw new ArgumentOutOfRangeException();
    this.comparer  = comparer ?? EqualityComparer<K>.Default;
    this._count    = new TransactionalVariable<int>();
    this._freeSlot = new TransactionalVariable<int>();
    this._capacity = new TransactionalVariable<int>();

    if(initialCapacity != 0)
    {
      using(STMTransaction transaction = STMTransaction.Create(STMOptions.IgnoreSystemTransaction))
      {
        Allocate(initialCapacity);
        transaction.Commit();
      }
    }
  }

  /// <summary>Initializes a new <see cref="TransactionalDictionary{K,V}"/> with items taken from the given
  /// <see cref="IDictionary{K,V}"/>.
  /// </summary>
  public TransactionalDictionary(IDictionary<K, V> initialItems) : this(initialItems, null) { }
  /// <summary>Initializes a new <see cref="TransactionalDictionary{K,V}"/> with items taken from the given
  /// <see cref="IDictionary{K,V}"/>, using the given <see cref="IEqualityComparer{T}"/> to compare keys for equality.
  /// </summary>
  public TransactionalDictionary(IDictionary<K, V> initialItems, IEqualityComparer<K> comparer) : this(0, comparer)
  {
    if(initialItems == null) throw new ArgumentNullException();

    using(STMTransaction transaction = STMTransaction.Create(STMOptions.IgnoreSystemTransaction))
    {
      Allocate(initialItems.Count);
      foreach(KeyValuePair<K, V> pair in initialItems) Add(pair.Key, pair.Value, false);
      transaction.Commit();
    }
  }

  #region KeyCollection
  /// <summary>A read-only collection containing the keys within a <see cref="TransactionalDictionary{K,V}"/>.</summary>
  public sealed class KeyCollection : ICollection<K>
  {
    internal KeyCollection(TransactionalDictionary<K, V> dictionary)
    {
      this.dictionary = dictionary;
    }

    #region ICollection<K> Members
    /// <inheritdoc/>
    public int Count
    {
      get { return dictionary.Count; }
    }

    /// <inheritdoc/>
    public bool IsReadOnly
    {
      get { return true; }
    }

    /// <inheritdoc/>
    public bool Contains(K item)
    {
      return dictionary.ContainsKey(item);
    }

    /// <inheritdoc/>
    public void CopyTo(K[] array, int arrayIndex)
    {
      Utility.ValidateRange(array, arrayIndex, dictionary.Count);
      foreach(KeyValuePair<K, V> pair in dictionary) array[arrayIndex++] = pair.Key;
    }

    void ICollection<K>.Add(K item)
    {
      throw new NotSupportedException();
    }

    void ICollection<K>.Clear()
    {
      throw new NotSupportedException();
    }

    bool ICollection<K>.Remove(K item)
    {
      throw new NotImplementedException();
    }
    #endregion

    #region IEnumerable<K> Members
    /// <inheritdoc/>
    public IEnumerator<K> GetEnumerator()
    {
      foreach(KeyValuePair<K, V> pair in dictionary) yield return pair.Key;
    }
    #endregion

    #region IEnumerable Members
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    #endregion

    readonly TransactionalDictionary<K, V> dictionary;
  }
  #endregion

  #region ValueCollection
  /// <summary>A read-only collection containing the values within a <see cref="TransactionalDictionary{K,V}"/>.</summary>
  public sealed class ValueCollection : ICollection<V>
  {
    internal ValueCollection(TransactionalDictionary<K, V> dictionary)
    {
      this.dictionary = dictionary;
    }

    #region ICollection<K> Members
    /// <inheritdoc/>
    public int Count
    {
      get { return dictionary.Count; }
    }

    /// <inheritdoc/>
    public bool IsReadOnly
    {
      get { return true; }
    }

    /// <inheritdoc/>
    public bool Contains(V item)
    {
      foreach(V testItem in this)
      {
        if(object.Equals(item, testItem)) return true;
      }
      return false;
    }

    /// <inheritdoc/>
    public void CopyTo(V[] array, int arrayIndex)
    {
      Utility.ValidateRange(array, arrayIndex, dictionary.Count);
      foreach(KeyValuePair<K, V> pair in dictionary) array[arrayIndex++] = pair.Value;
    }

    void ICollection<V>.Add(V item)
    {
      throw new NotSupportedException();
    }

    void ICollection<V>.Clear()
    {
      throw new NotSupportedException();
    }

    bool ICollection<V>.Remove(V item)
    {
      throw new NotImplementedException();
    }
    #endregion

    #region IEnumerable<V> Members
    /// <inheritdoc/>
    public IEnumerator<V> GetEnumerator()
    {
      foreach(KeyValuePair<K, V> pair in dictionary) yield return pair.Value;
    }
    #endregion

    #region IEnumerable Members
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    #endregion

    readonly TransactionalDictionary<K, V> dictionary;
  }
  #endregion

  #region IDictionary<K,V> Members
  /// <inheritdoc/>
  public V this[K key]
  {
    get
    {
      return STM.Retry(delegate
      {
        Bucket bucket = Find(key);
        if(bucket == null) throw new KeyNotFoundException();
        return bucket.Value;
      });
    }
    set
    {
      STM.Retry(delegate { Add(key, value, true); });
    }
  }

  /// <summary>Gets a read-only collection containing the keys within the dictionary.</summary>
  public KeyCollection Keys
  {
    get
    {
      if(_keys == null)
      {
        lock(this)
        {
          if(_keys == null) _keys = new KeyCollection(this);
        }
      }
      return _keys;
    }
  }

  /// <summary>Gets a read-only collection containing the values within the dictionary.</summary>
  public ValueCollection Values
  {
    get
    {
      if(_values == null)
      {
        lock(this)
        {
          if(_values == null) _values = new ValueCollection(this);
        }
      }
      return _values;
    }
  }

  /// <inheritdoc/>
  public void Add(K key, V value)
  {
    STM.Retry(delegate { Add(key, value, false); });
  }

  /// <inheritdoc/>
  public bool ContainsKey(K key)
  {
    return STM.Retry(delegate { return Find(key) != null; });
  }

  /// <inheritdoc/>
  public bool Remove(K key)
  {
    if(array != null)
    {
      return STM.Retry(delegate
      {
        int addressableSize = GetAddressableSize(), hash = comparer.GetHashCode(key) % addressableSize, index;
        Bucket bucket = array[hash].Read();
        index = bucket.First;
        if(index != Empty)
        {
          if(index != hash) bucket = array[index].Read();
          int prevIndex = -1;
          while(true)
          {
            if(comparer.Equals(key, bucket.Key)) // if we found the item to remove...
            {
              int nextIndex = bucket.Next;
              bucket = array[index].OpenForWrite();
              // if we can move an item out of the cellar, do so, since it can greatly improve the efficiency of GetFreeSlot()
              if(index < addressableSize && nextIndex >= addressableSize)
              {
                Bucket nextBucket = array[nextIndex].OpenForWrite();
                bucket.Key   = nextBucket.Key; // move the next item in the chain into this slot
                bucket.Value = nextBucket.Value;
                bucket.Next  = nextBucket.Next;
                prevIndex = index;
                index     = nextIndex;
                nextIndex = nextBucket.Next;
                bucket    = nextBucket;
              }

              // if there's a previous item, unlink this item from it. otherwise, if this is the only item in the chain, remove
              // the chain. otherwise, if this is the first item in the chain, make the chain's head point to the next item
              if(prevIndex != -1) array[prevIndex].OpenForWrite().Next = nextIndex;
              else if(nextIndex == Null) array[hash].OpenForWrite().First = Empty;
              else if(array[hash].Read().First == index) array[hash].OpenForWrite().First = nextIndex;
              bucket.Key   = default(K);
              bucket.Value = default(V);
              bucket.Next  = Empty;

              // adjust the free slot if the new empty space is closer to the end of the array or if both are in the cellar
              int freeSlot = _freeSlot.Read();
              if(freeSlot < index || index >= addressableSize)
              {
                // if both are within the cellar, make a link from it to the previous free space so we can easily find it again.
                // we encode it as a negative number, and subtract three because 0, -1, and -2 already have other meanings
                if(index >= addressableSize && freeSlot >= addressableSize) bucket.Next = -freeSlot - 3;
                _freeSlot.Set(index);
              }
              _count.Set(_count.OpenForWrite() - 1);
              return true;
            }

            prevIndex = index;
            index = bucket.Next;
            if(index == Null) break;
            bucket = array[index].Read();
          }
        }

        return false;
      });
    }

    return false;
  }

  /// <inheritdoc/>
  public bool TryGetValue(K key, out V value)
  {
    Bucket bucket = STM.Retry(delegate { return Find(key); });
    if(bucket == null)
    {
      value = default(V);
      return false;
    }
    else
    {
      value = bucket.Value;
      return true;
    }
  }

  ICollection<K> IDictionary<K, V>.Keys
  {
    get { return Keys; }
  }

  ICollection<V> IDictionary<K, V>.Values
  {
    get { return Values; }
  }
  #endregion

  #region ICollection<KeyValuePair<K,V>> Members
  /// <inheritdoc/>
  public int Count
  {
    get { return _count.Read(); }
  }

  /// <inheritdoc/>
  public bool IsReadOnly
  {
    get { return false; }
  }

  /// <inheritdoc/>
  public void Clear()
  {
    if(array != null)
    {
      STM.Retry(delegate
      {
        for(int i=0; i<array.Length; i++) array[i].OpenForWrite().Clear();
        _freeSlot.Set(array.Length - 1);
        _capacity.Set(array.Length);
        _count.Set(0);
      });
    }
  }

  /// <inheritdoc/>
  public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
  {
    STM.Retry(delegate
    {
      Utility.ValidateRange(array, arrayIndex, Count);
      int index = arrayIndex; // don't alter the parameter, in case the transaction restarts
      foreach(KeyValuePair<K, V> pair in this) array[index++] = pair;
    });
  }

  void ICollection<KeyValuePair<K, V>>.Add(KeyValuePair<K, V> item)
  {
    Add(item.Key, item.Value);
  }

  bool ICollection<KeyValuePair<K, V>>.Contains(KeyValuePair<K, V> item)
  {
    V value;
    return TryGetValue(item.Key, out value) && object.Equals(value, item.Value);
  }

  bool ICollection<KeyValuePair<K, V>>.Remove(KeyValuePair<K, V> item)
  {
    // the TryGetValue() / Remove() pair don't need to be wrapped in a transaction because the result should be linearizable even
    // if another transaction removes the value between TryGetValue() and Remove()
    V value;
    return TryGetValue(item.Key, out value) && object.Equals(value, item.Value) && Remove(item.Key);
  }
  #endregion

  #region IEnumerable<KeyValuePair<K,V>> Members
  /// <inheritdoc/>
  public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
  {
    return new Enumerator(array);
  }
  #endregion

  #region IEnumerable Members
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
  #endregion

  #region Bucket
  const int Null = -1, Empty = -2;

  sealed class Bucket : ICloneable
  {
    public Bucket()
    {
      Next = First = Empty;
    }

    public void Clear()
    {
      Key   = default(K);
      Value = default(V);
      Next  = First = Empty;
    }

    public object Clone()
    {
      Bucket newBucket = new Bucket();
      newBucket.Key   = Key;
      newBucket.Value = Value;
      newBucket.Next  = Next;
      newBucket.First = First;
      return newBucket;
    }

    #if DEBUG
    public override string ToString()
    {
      string str;
      if(Next == Empty) str = "Empty";
      else if(Next < Empty) str = "Empty -> " + (-(Next+3)).ToInvariantString();
      else
      {
        str = Convert.ToString(Key);
        if(Next != Null) str += " -> " + Next.ToInvariantString();
      }

      if(First != Empty) str += " (" + First.ToInvariantString() + ")";
      return str;
    }
    #endif

    public K Key;
    public V Value;
    public int Next, First;
  }
  #endregion

  #region Enumerator
  sealed class Enumerator : IEnumerator<KeyValuePair<K,V>>
  {
    public Enumerator(TransactionalVariable<Bucket>[] array)
    {
      this.array = array;
      index      = -1;
    }

    public KeyValuePair<K,V> Current
    {
      get
      {
        if(array == null || (uint)index >= (uint)array.Length) throw new InvalidOperationException();
        return _current;
      }
    }

    public bool MoveNext()
    {
      if(array == null || index == array.Length) return false;

      Bucket bucket = null;
      do
      {
        index++;
        if(index == array.Length) break;
        bucket = array[index].Read();
      } while(bucket.Next <= Empty);

      if(bucket == null || index == array.Length)
      {
        _current = default(KeyValuePair<K,V>);
        return false;
      }
      else
      {
        _current = new KeyValuePair<K, V>(bucket.Key, bucket.Value);
        return true;
      }
    }

    public void Reset()
    {
      index = -1;
    }

    object System.Collections.IEnumerator.Current
    {
      get { return Current; }
    }

    void IDisposable.Dispose() { }

    readonly TransactionalVariable<Bucket>[] array;
    KeyValuePair<K,V> _current;
    int index;
  }
  #endregion

  void Add(K key, V value, bool canOverwrite)
  {
    int count = _count.OpenForWrite();
    if(count == _capacity.Read()) Allocate(GetAddressableSize()*2+1);

    int addressSize = GetAddressableSize(), hash = comparer.GetHashCode(key) % addressSize, index = hash;
    Bucket bucket = array[hash].Read();
    if(bucket.First != Empty) // if data for this hash code already exists...
    {
      index = bucket.First; // jump to the first bucket servicing this hash code
      if(hash != index) bucket = array[index].Read();
      while(true) // search that hash code's chain for the key
      {
        if(comparer.Equals(key, bucket.Key)) // if we found the key...
        {
          if(!canOverwrite) throw new ArgumentException(); // if we can't overwrite, then throw an exception
          array[index].OpenForWrite().Value = value; // otherwise, overwrite the value and return
          return;
        }

        int nextIndex = bucket.Next;
        if(nextIndex == Null) break;
        index  = nextIndex;
        bucket = array[index].Read();
      }

      int slot = GetFreeSlot(addressSize);
      array[index].OpenForWrite().Next = slot; // link the new slot into the end of the chain
      index = slot;
    }
    else // no data exists for this hash code...
    {
      // find a place for the first item if we can't use the slot at the hash index
      if(bucket.Next > Empty) index = GetFreeSlot(addressSize);
      array[hash].OpenForWrite().First = index; // and establish the new chain at that location
    }

    bucket = array[index].OpenForWrite();
    bucket.Key   = key;
    bucket.Value = value;
    bucket.Next  = Null;
    _count.Set(count + 1);
  }

  void Allocate(int newCapacity)
  {
    // check the capacity against the largest prime size that will fit in an int after adding the cellar and the offset (3)
    // for free links
    if(newCapacity > 1846835911) throw new OutOfMemoryException();
    newCapacity = (GetPrimeSize(newCapacity)*50 + 22) / 43;
    int existingCapacity = _capacity.Read();
    if(newCapacity > existingCapacity)
    {
      lock(this)
      {
        _capacity.Release(); // reread the capacity to see if somebody else has resized the array before we could obtain the lock
        existingCapacity = _capacity.Read();
        if(newCapacity > existingCapacity)
        {
          TransactionalVariable<Bucket>[] newArray =
            array == null || newCapacity > array.Length ? new TransactionalVariable<Bucket>[newCapacity] : array;
          KeyValuePair<K, V>[] existingData = null;
          if(array == null)
          {
            for(int i=0; i<newArray.Length; i++) newArray[i] = STM.Allocate(new Bucket());
          }
          else
          {
            existingData = new KeyValuePair<K, V>[Count];
            CopyTo(existingData, 0);
            if(newArray != array)
            {
              Array.Copy(array, newArray, array.Length);
              for(int i=array.Length; i<newArray.Length; i++) newArray[i] = STM.Allocate(new Bucket());
            }
            for(int i=0; i<existingCapacity; i++) newArray[i].OpenForWrite().Clear();
          }

          array = newArray;
          _freeSlot.Set(newArray.Length-1);
          _capacity.Set(newArray.Length);

          // rehash the existing data
          if(existingData != null)
          {
            foreach(KeyValuePair<K, V> pair in existingData) Add(pair.Key, pair.Value, false);
          }
        }
      }
    }
  }

  Bucket Find(K key)
  {
    if(array != null)
    {
      int hash = comparer.GetHashCode(key) % GetAddressableSize();
      Bucket bucket = array[hash].Read();
      int index = bucket.First;
      if(index != Empty)
      {
        if(index != hash) bucket = array[index].Read();
        while(true)
        {
          if(comparer.Equals(key, bucket.Key)) return bucket;
          index = bucket.Next;
          if(index == Null) break;
          bucket = array[index].Read();
        }
      }
    }
    return null;
  }

  int GetAddressableSize()
  {
    return (int)((_capacity.Read()*43L + 25)/50); // the addressable area is 86% of the total area
  }

  int GetFreeSlot(int addressSize)
  {
    // if the collision spot is in the addressable area, then it may not be free any longer
    int slot = _freeSlot.OpenForWrite();
    if(slot < addressSize) // if it's in the addressable area, make sure it's still available...
    {
      while(slot >= 0 && array[slot].Read().Next != Empty) slot--;
    }

    int nextFreeSlot = array[slot].Read().Next;
    if(nextFreeSlot != Empty)
    {
      nextFreeSlot = -(nextFreeSlot+3); // if we have a pointer to the next free slot, use it
    }
    else
    {
      // otherwise, search for the next free slot in the cellar
      nextFreeSlot = slot;
      do nextFreeSlot--; while(nextFreeSlot >= addressSize && array[nextFreeSlot].Read().Next != Empty);
    }

    _freeSlot.Set(nextFreeSlot);
    return slot;
  }

  readonly IEqualityComparer<K> comparer;
  TransactionalVariable<Bucket>[] array;
  readonly TransactionalVariable<int> _count, _freeSlot, _capacity;
  KeyCollection _keys;
  ValueCollection _values;

  static int GetPrimeSize(int minimum)
  {
    for(int i=0; i<primes.Length; i++)
    {
      if(primes[i] >= minimum) return primes[i];
    }
    for(int i=minimum|1; i<int.MaxValue; i += 2)
    {
      if(IsPrime(i)) return i;
    }
    throw new OutOfMemoryException();
  }

  static bool IsPrime(int n) // it is assumed that n is a large, odd number
  {
    // this makes use of the fact that all primes except 2 and 3 are divisible by 6k+1 or 6k-1 for some positive integer k. this
    // works because all integers can be represented as 6k+i for some integer k where -1 <= i <= 4. 2 divides 6k + {0,2,4}
    // and 3 divides 6k+3, leaving only 6k+1 and 6k-1 to check. we don't need to handle n == 3 because n is assumed to be large
    for(int i=6, end=(int)Math.Sqrt(n); i < end; i += 6) // we use i < end rather than i <= end because
    {                                                    // the i+1 inside the loop would check 'end'
      if(n % (i-1) == 0 || n % (i+1) == 0) return false;
    }

    return true;
  }

  static readonly int[] primes =
  {
    3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919, 1103, 1327, 1597, 1931,
    2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851,
    75431, 90523, 108631, 130363, 156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897,
    1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
  };
}
#endregion

#region TransactionalList
/// <summary>Provides a transactional list, meant to be used with <see cref="STMTransaction"/> or
/// <see cref="System.Transactions.Transaction"/>.
/// </summary>
/// <remarks>The <see cref="TransactionalList{T}"/> is meant to provide a full-feature implementation of the standard
/// <see cref="IList{T}"/> interface, and is not particularly scalable with regard to concurrent writes. In particular,
/// due to the need to keep track of the <see cref="ICollection{T}.Count"/> of items in the list, all additions and removals will
/// conflict with each other, even if they don't alter the same parts of the list, because all must update the item count.
/// </remarks>
public class TransactionalList<T> : IList<T>
{
  /// <summary>Initializes a new, empty <see cref="TransactionalList{T}"/>.</summary>
  public TransactionalList() : this(0, null) { }
  /// <summary>Initializes a new, empty <see cref="TransactionalList{T}"/> with the given capacity.</summary>
  public TransactionalList(int capacity) : this(capacity, null) { }
  /// <summary>Initializes a new, empty <see cref="TransactionalList{T}"/> with the given <see cref="IEqualityComparer{T}"/>,
  /// which will be used to compare items for equality.
  /// </summary>
  public TransactionalList(IEqualityComparer<T> comparer) : this(0, comparer) { }
  /// <summary>Initializes a new, empty <see cref="TransactionalList{T}"/> with the given capacity and the given
  /// <see cref="IEqualityComparer{T}"/>, which will be used to compare items for equality.
  /// </summary>
  public TransactionalList(int capacity, IEqualityComparer<T> comparer)
  {
    array  = new TransactionalArray<T>(capacity, comparer);
    _count = new TransactionalVariable<int>();
  }

  /// <summary>Initializes a new <see cref="TransactionalList{T}"/> with the given items.</summary>
  public TransactionalList(IEnumerable<T> initialItems) : this(initialItems, null) { }
  /// <summary>Initializes a new <see cref="TransactionalList{T}"/> with the given items, and an
  /// <see cref="IEqualityComparer{T}"/>, which will be used to compare items for equality.
  /// </summary>
  public TransactionalList(IEnumerable<T> initialItems, IEqualityComparer<T> comparer)
  {
    array  = new TransactionalArray<T>(initialItems, comparer);
    _count = new TransactionalVariable<int>(array.Count);
  }

  /// <summary>Adds the given items to the end of the list.</summary>
  public void AddRange(IEnumerable<T> items)
  {
    if(items == null) throw new ArgumentNullException();
    STM.Retry(delegate
    {
      int count = _count.OpenForWrite();
      foreach(T item in items)
      {
        EnlargeForAdd(count);
        array[count++] = item;
      }
      _count.Set(count);
    });
  }

  /// <summary>Returns an array containing all of the items in the list.</summary>
  public T[] ToArray()
  {
    return STM.Retry(delegate
    {
      T[] array = new T[Count];
      for(int i=0; i<array.Length; i++) array[i] = this.array[i];
      return array;
    });
  }

  #region IList<T> Members
  /// <inheritdoc/>
  public T this[int index]
  {
    get
    {
      return STM.Retry(delegate
      {
        if((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException();
        return array[index];
      });
    }
    set
    {
      STM.Retry(delegate
      {
        if((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException();
        array[index] = value;
      });
    }
  }

  /// <inheritdoc/>
  public int IndexOf(T item)
  {
    return STM.Retry(delegate { return array.IndexOf(item, 0, Count); });
  }

  /// <inheritdoc/>
  public void Insert(int index, T item)
  {
    STM.Retry(delegate
    {
      int count = _count.OpenForWrite();
      if((uint)index > (uint)count) throw new ArgumentOutOfRangeException();
      EnlargeForAdd(count);
      for(int i=count; i > index; i--) array[i] = array[i-1];
      array[index] = item;
      _count.Set(count+1);
    });
  }

  /// <inheritdoc/>
  public void RemoveAt(int index)
  {
    STM.Retry(delegate
    {
      int count = _count.OpenForWrite();
      if((uint)index >= (uint)count) throw new ArgumentOutOfRangeException();
      count--;
      for(; index < count; index++) array[index] = array[index+1];
      array[count] = default(T);
      _count.Set(count);
    });
  }
  #endregion

  #region ICollection<T> Members
  /// <inheritdoc/>
  public int Count
  {
    get { return _count.Read(); }
  }

  /// <inheritdoc/>
  public bool IsReadOnly
  {
    get { return false; }
  }

  /// <inheritdoc/>
  public void Add(T item)
  {
    STM.Retry(delegate
    {
      int count = _count.OpenForWrite();
      EnlargeForAdd(count);
      array[count] = item;
      _count.Set(count+1);
    });
  }

  /// <inheritdoc/>
  public void Clear()
  {
    STM.Retry(delegate
    {
      int count = _count.OpenForWrite();
      if(!typeof(T).UnderlyingSystemType.IsPrimitive) // if the items may contain references that should be cleared...
      {
        for(int i=0; i<count; i++) array[i] = default(T); // then clear them
      }
      _count.Set(0);
    });
  }

  /// <inheritdoc/>
  public bool Contains(T item)
  {
    return STM.Retry(delegate { return array.Contains(item, 0, Count); });
  }

  /// <inheritdoc/>
  public void CopyTo(T[] array, int arrayIndex)
  {
    STM.Retry(delegate { this.array.CopyTo(0, array, arrayIndex, Count); });
  }

  /// <inheritdoc/>
  public bool Remove(T item)
  {
    return STM.Retry(delegate
    {
      int index = IndexOf(item);
      if(index != -1) RemoveAt(index);
      return index != -1;
    });
  }
  #endregion

  #region IEnumerable<T> Members
  /// <inheritdoc/>
  public IEnumerator<T> GetEnumerator()
  {
    return array.GetEnumerator(0, Count);
  }
  #endregion

  #region IEnumerable Members
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
  #endregion

  void EnlargeForAdd(int currentCount)
  {
    if(currentCount == array.Count) array.Enlarge(currentCount < 2 ? 4 : currentCount * 2);
  }

  readonly TransactionalArray<T> array;
  readonly TransactionalVariable<int> _count;
}
#endregion

} // namespace AdamMil.Transactions
