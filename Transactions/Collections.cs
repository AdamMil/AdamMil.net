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
// TODO: use Release() to improve performance of the collections

namespace AdamMil.Transactions
{

#region TransactionalArray
/// <summary>Implements an array whose members are transactional variables. The array's length cannot be changed after it is
/// created, so the array does not support methods that add or remove members.
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
    return STM.Retry(delegate
    {
      for(int i=0; i<array.Length; i++)
      {
        if(comparer.Equals(item, array[i].Read())) return i;
      }
      return -1;
    });
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
    return STM.Retry(delegate
    {
      for(int i=0; i<array.Length; i++)
      {
        if(comparer.Equals(item, array[i].Read())) // if an item is found, changes to previous items won't affect the result,
        {                                          // so we can release them to avoid false conflicts
          while(i > 0) array[--i].Release();
          return true;
        }
      }
      return false;
    });
  }

  /// <inheritdoc/>
  public void CopyTo(T[] array, int arrayIndex)
  {
    if(array == null) throw new ArgumentNullException();
    if(arrayIndex < 0 || arrayIndex+Count > array.Length) throw new ArgumentOutOfRangeException();
    STM.Retry(delegate
    {
      for(int i=0; i<this.array.Length; i++) array[arrayIndex+i] = this.array[i].Read();
    });
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
    return new Enumerator(array);
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
    public Enumerator(TransactionalVariable<T>[] array)
    {
      this.array = array;
      index      = -1;
    }

    public T Current
    {
      get
      {
        if((uint)index >= (uint)array.Length) throw new InvalidOperationException();
        return current;
      }
    }

    public bool MoveNext()
    {
      if(index == array.Length) return false;
      if(++index == array.Length)
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
      index = -1;
    }

    object System.Collections.IEnumerator.Current
    {
      get { return Current; }
    }

    void IDisposable.Dispose() { }

    readonly TransactionalVariable<T>[] array;
    T current;
    int index;
  }
  #endregion

  readonly TransactionalVariable<T>[] array;
  readonly IEqualityComparer<T> comparer;
}
#endregion

#region TransactionalDictionary
public sealed class TransactionalDictionary<K, V> : IDictionary<K, V>
{
  public TransactionalDictionary() : this(0, null) { }
  public TransactionalDictionary(int initialCapacity) : this(initialCapacity, null) { }
  public TransactionalDictionary(IEqualityComparer<K> comparer) : this(0, comparer) { }
  public TransactionalDictionary(int initialCapacity, IEqualityComparer<K> comparer)
  {
    if(initialCapacity < 0) throw new ArgumentOutOfRangeException();
    this.comparer = comparer ?? EqualityComparer<K>.Default;
    //this.count    = STM.Allocate<int>();
    if(initialCapacity != 0) Allocate(initialCapacity);
  }

  public TransactionalDictionary(IDictionary<K, V> initialItems) : this(initialItems, null) { }
  public TransactionalDictionary(IDictionary<K, V> initialItems, IEqualityComparer<K> comparer) : this(0, comparer)
  {
    if(initialItems == null) throw new ArgumentNullException();
    Allocate(initialItems.Count);
    foreach(KeyValuePair<K, V> pair in initialItems) Add(pair.Key, pair.Value);
  }

  #region IDictionary<K,V> Members
  /// <inheritdoc/>
  public V this[K key]
  {
    get
    {
      int index = Find(key);
      if(index == -1) throw new KeyNotFoundException();
      return array[index].Value;
    }
    set
    {
      Add(key, value, true);
    }
  }

  /// <inheritdoc/>
  public ICollection<K> Keys
  {
    get { throw new NotImplementedException(); }
  }

  /// <inheritdoc/>
  public ICollection<V> Values
  {
    get { throw new NotImplementedException(); }
  }

  /// <inheritdoc/>
  public void Add(K key, V value)
  {
    Add(key, value, false);
  }

  /// <inheritdoc/>
  public bool ContainsKey(K key)
  {
    return Find(key) != -1;
  }

  /// <inheritdoc/>
  public bool Remove(K key)
  {
    if(array != null)
    {
      int addressableSize = GetAddressableSize(), hash = comparer.GetHashCode(key) % addressableSize, index = array[hash].First;
      if(index != Empty)
      {
        int prevIndex = -1;
        while(true)
        {
          if(comparer.Equals(key, array[index].Key)) // if we found the item to remove...
          {
            int nextIndex = array[index].Next;
            // if we can move an item out of the cellar, do so, since it can greatly improve the efficiency of GetFreeSlot()
            if(index < addressableSize && nextIndex >= addressableSize)
            {
              array[index].Key   = array[nextIndex].Key; // move the next item in the chain into this slot
              array[index].Value = array[nextIndex].Value;
              array[index].Next  = array[nextIndex].Next;
              prevIndex = index;
              index     = nextIndex;
              nextIndex = array[index].Next;
            }

            if(prevIndex != -1) array[prevIndex].Next = nextIndex; // if there's a previous item, unlink this item from it
            else if(nextIndex == Null) array[hash].First = Empty; // if this is the only item in the chain, remove the chain
            else if(array[hash].First == index) array[hash].First = nextIndex; // if this is the first item in the chain, make
            array[index].Key   = default(K);                                   // the chain's head point to the next item
            array[index].Value = default(V);
            array[index].Next  = Empty;

            // adjust the free slot if the new empty space is closer to the end of the array or if both are in the cellar
            if(freeSlot < index || index >= addressableSize)
            {
              // if both are within the cellar, make a link from it to the previous free space so we can easily find it again.
              // we encode it as a negative number, and subtract three because 0, -1, and -2 already have other meanings
              if(index >= addressableSize && freeSlot >= addressableSize) array[index].Next = -freeSlot - 3;
              freeSlot = index;
            }
            _count--;
            return true;
          }

          prevIndex = index;
          index = array[index].Next;
          if(index == Null) break;
        }
      }
    }

    return false;
  }

  /// <inheritdoc/>
  public bool TryGetValue(K key, out V value)
  {
    int index = Find(key);
    if(index == -1)
    {
      value = default(V);
      return false;
    }
    else
    {
      value = array[index].Value;
      return true;
    }
  }
  #endregion

  #region ICollection<KeyValuePair<K,V>> Members
  /// <inheritdoc/>
  public int Count
  {
    get { return _count; }
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
      Array.Clear(array, 0, array.Length);
      for(int i=0; i<array.Length; i++) array[i].Next = Empty;
      freeSlot = array.Length - 1;
      _count        = 0;
    }
  }

  /// <inheritdoc/>
  public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
  {
    if(array == null) throw new ArgumentNullException();
    if(arrayIndex < 0 || arrayIndex + Count > array.Length) throw new ArgumentOutOfRangeException();
    foreach(KeyValuePair<K,V> pair in this) array[arrayIndex++] = pair;
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
    throw new NotImplementedException();
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

  struct Bucket
  {
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
    public Enumerator(Bucket[] array)
    {
      this.array = array;
      index      = -1;
    }

    public KeyValuePair<K,V> Current
    {
      get
      {
        if(index < 0 || index >= array.Length) throw new InvalidOperationException();
        return _current;
      }
    }

    public bool MoveNext()
    {
      if(array == null || index == array.Length) return false;
      do index++; while(index < array.Length && array[index].Next == Empty);

      if(index == array.Length)
      {
        _current = default(KeyValuePair<K,V>);
        return false;
      }
      else
      {
        _current = new KeyValuePair<K, V>(array[index].Key, array[index].Value);
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

    readonly Bucket[] array;
    KeyValuePair<K,V> _current;
    int index;
  }
  #endregion

  void Add(K key, V value, bool canOverwrite)
  {
    if(array == null || Count == array.Length)
    {
      Bucket[] oldArray = array;

      // the addressable area is about 86% of the total area
      Allocate((array == null ? 0 : GetAddressableSize()*2) + 1);
      _count = 0;

      // rehash the data from the old array, if it exists
      if(oldArray != null)
      {
        for(int i=0; i<oldArray.Length; i++)
        {
          if(oldArray[i].Next != Empty) Add(oldArray[i].Key, oldArray[i].Value, false);
        }
      }
    }

    int addressSize = GetAddressableSize(), hash = comparer.GetHashCode(key) % addressSize, index = hash;
    if(array[hash].First != Empty) // if data for this hash code already exists...
    {
      index = array[hash].First; // jump to the first bucket servicing this hash code
      while(true) // search that hash code's chain for the key
      {
        if(comparer.Equals(key, array[index].Key)) // if we found the key...
        {
          if(!canOverwrite) throw new ArgumentException(); // if we can't overwrite, then throw an exception
          array[index].Value = value; // otherwise, overwrite the value and return
          return;
        }

        int nextIndex = array[index].Next;
        if(nextIndex == Null) break;
        index = nextIndex;
      }

      int slot = GetFreeSlot(addressSize);
      array[index].Next = slot; // link the new slot into the end of the chain
      index = slot;
    }
    else // no data exists for this hash code...
    {
      // find a place for the first item if we can't use the slot at the hash index
      if(array[hash].Next > Empty) index = GetFreeSlot(addressSize);
      array[hash].First = index; // and establish the new chain at that location
    }

    array[index].Key   = key;
    array[index].Value = value;
    array[index].Next  = Null;
    _count++;
  }

  void Allocate(int capacity)
  {
    // check the capacity against the largest prime size that will fit in an int after adding the cellar and the offset (3)
    // for free links
    if(capacity > 1846835911) throw new OutOfMemoryException();
    array = new Bucket[(GetPrimeSize(capacity)*50 + 22) / 43];
    freeSlot = array.Length - 1;
    for(int i=0; i<array.Length; i++) array[i].First = array[i].Next = Empty; // mark the buckets as empty
  }

  int Find(K key)
  {
    if(array != null)
    {
      int index = array[comparer.GetHashCode(key) % GetAddressableSize()].First;
      if(index != Empty)
      {
        do
        {
          if(comparer.Equals(key, array[index].Key)) return index;
          index = array[index].Next;
        } while(index != Null);
      }
    }
    return -1;
  }

  int GetAddressableSize()
  {
    return (int)((array.Length*43L + 25)/50); // the addressable area is 86% of the total area
  }

  int GetFreeSlot(int addressSize)
  {
    // if the collision spot is in the addressable area, then it may not be free any longer
    if(freeSlot < addressSize) // if it's in the addressable area, make sure it's still available...
    {
      while(freeSlot >= 0 && array[freeSlot].Next != Empty) freeSlot--;
    }

    int slot = freeSlot, nextFreeSlot = array[freeSlot].Next;
    if(nextFreeSlot != Empty)
    {
      freeSlot = -(nextFreeSlot+3); // if we have a pointer to the next free slot, use it
    }
    else
    {
      // otherwise, search for the next free slot in the cellar
      do freeSlot--; while(freeSlot >= addressSize && array[freeSlot].Next != Empty);
    }
    return slot;
  }

  readonly IEqualityComparer<K> comparer;
  Bucket[] array;
  //readonly TransactionalVariable<int> count;
  int _count, freeSlot;

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
    {                                                    // the +1 inside the loop would check 'end'
      if(n % (i-1) == 0 || n % (i+1) == 0) return false;
    }

    return true;
  }

  static readonly int[] primes =
  {
    5, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919, 1103, 1327, 1597, 1931,
    2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851,
    75431, 90523, 108631, 130363, 156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897,
    1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
  };
}
#endregion

#region TransactionalSortedDictionary
public sealed class TransactionalSortedDictionary<K, V> : IDictionary<K, V>
{
  public TransactionalSortedDictionary() : this((IComparer<K>)null) { }
  public TransactionalSortedDictionary(IComparer<K> comparer)
  {
    this.comparer = comparer ?? Comparer<K>.Default;
    this.root     = STM.Allocate(Leaf);
    this.count    = STM.Allocate<int>();
  }

  public TransactionalSortedDictionary(IDictionary<K, V> initialItems) : this(initialItems, null) { }
  public TransactionalSortedDictionary(IDictionary<K, V> initialItems, IComparer<K> comparer) : this(comparer)
  {
    if(initialItems == null) throw new ArgumentNullException();
    using(STMTransaction tx = STMTransaction.Create())
    {
      foreach(KeyValuePair<K, V> pair in initialItems) Add(pair.Key, pair.Value);
      tx.Commit();
    }
  }

  #region IDictionary<K,V> Members
  /// <inheritdoc/>
  public V this[K key]
  {
    get
    {
      Node node = FindNode(key);
      if(node == null) throw new KeyNotFoundException();
      return node.Value;
    }
    set
    {
      throw new NotImplementedException();
    }
  }

  /// <inheritdoc/>
  public ICollection<K> Keys
  {
    get { throw new NotImplementedException(); }
  }

  /// <inheritdoc/>
  public ICollection<V> Values
  {
    get { throw new NotImplementedException(); }
  }

  /// <inheritdoc/>
  public void Add(K key, V value)
  {
    STM.Retry(delegate
    {
      // first, find the place where the node needs to be inserted
      TransactionalVariable<Node> nodeVar = root, newVar;
      Node node = root.Read(), newNode;
      if(node == Leaf) // if the tree is empty, then it becomes the new root and we're done
      {
        newVar  = root;
        newNode = new Node(null, key, value);
        root.Set(newNode);
      }
      else // otherwise, we need to find the location in the tree where it should be inserted
      {
        while(true)
        {
          int cmp = comparer.Compare(key, node.Key);
          if(cmp == 0) throw new ArgumentException(); // if the key already exists, that's an error

          TransactionalVariable<Node> nextVar = cmp < 0 ? node.Left : node.Right;
          if(nextVar == LeafVariable) // this is where it needs to be inserted
          {
            newNode = new Node(nodeVar, key, value);
            newNode.Color = Color.Red;
            newVar = STM.Allocate(Leaf);
            newVar.Set(newNode);

            node = nodeVar.OpenForWrite();
            if(cmp < 0) node.Left = newVar;
            else node.Right = newVar;
            break;
          }
          node = nodeVar.Read();
        }

        // at this point, the node has been inserted into the right place, but we may need to rebalance the tree. we just
        // replaced a black leaf with a red node having 2 black children. the tree has the following invariants:
        // 1. the root is black
        // 2. both children of every red node are black
        // 3. every simple path from from a node to a leaf has the same number of black nodes
        // we haven't changed the root, so #1 holds. we've replaced ? -> black with ? -> red -> black, leaving the number of
        // black nodes equal along all paths to leaves, so #3 holds. if the parent is black, then #2 is also not violated, so
        // we're done. but if the parent is red, then #2 has been violated
        restart:
        if(node.Color == Color.Red) // if the parent is red...
        {
          // at this point, we can assume the grandparent is black because of #2. if the uncle (i.e. parent's sibling) is red as
          // well, then we can repaint both the parent and uncle from red to black, and the grandparent from black to red. this
          // transformation may violate #1 or #2.
          TransactionalVariable<Node> uncleVar = newVar == node.Left ? node.Right : node.Left;
          Node uncle = uncleVar.Read();
          if(uncle.Color == Color.Red)
          {
            node = nodeVar.OpenForWrite();
            node.Color = Color.Black;
            uncle = uncleVar.OpenForWrite();
            uncle.Color = Color.Black;

            newVar = node.Parent; // newVar = grandparent
            // if the grandparent was the root, repainting it red would violate #1, so we won't do that. in that case, we're done
            if(newVar != root) // if it's not the root...
            {
              // then we'll color the grandparent red. this may violate #2
              newNode = newVar.OpenForWrite();
              newNode.Color = Color.Red;
              nodeVar = newNode.Parent;
              node    = nodeVar.Read();
              goto restart; // this is analogous to our original situation, so we'll restart
            }
          }
          else // the new node is red but the uncle is black
          {
            // if the node is the right child of the parent and the parent node is the left child of the grandparent, or vice
            // versa, then we'll perform a rotation so they're on the same side
            TransactionalVariable<Node> gpVar = node.Parent;
            Node gp = gpVar.OpenForWrite();
            node = nodeVar.OpenForWrite();
            bool rotated = false;
            if(newVar == node.Left)
            {
              if(nodeVar == gp.Right)
              {
                RotateLeft(newNode, node, gp);
                rotated = true;
              }
            }
            else
            {
              if(nodeVar == gp.Left)
              {
                RotateRight(newNode, node, gp);
                rotated = true;
              }
            }

            if(rotated) // if the nodes were rotated, nodeVar/node and newVar/newNode got swapped
            {
              Utility.Swap(ref nodeVar, ref newVar);
              Utility.Swap(ref node, ref newNode);
            }

            node.Color = Color.Black;
            gp.Color   = Color.Red;

            if(nodeVar == gp.Left) RotateRight(node, gp, gp.Parent.OpenForWrite());
            else RotateLeft(node, gp, gp.Parent.OpenForWrite());
          }
        }
      }

      // finally, increment the item count
      count.Set(count.OpenForWrite() + 1);
    });
  }

  /// <inheritdoc/>
  public bool ContainsKey(K key)
  {
    return FindNode(key) != null;
  }

  /// <inheritdoc/>
  public bool Remove(K key)
  {
    return STM.Retry(delegate
    {
      TransactionalVariable<Node> variable;
      Node node = FindNode(key, out variable);
      if(node == null)
      {
        return false;
      }
      else
      {
        Remove(variable);
        return true;
      }
    });
  }

  /// <inheritdoc/>
  public bool TryGetValue(K key, out V value)
  {
    Node node = FindNode(key);
    if(node == null)
    {
      value = default(V);
      return false;
    }
    else
    {
      value = node.Value;
      return true;
    }
  }
  #endregion

  #region ICollection<KeyValuePair<K,V>> Members
  /// <inheritdoc/>
  public int Count
  {
    get { return count.Read(); }
  }

  /// <inheritdoc/>
  public bool IsReadOnly
  {
    get { return false; }
  }

  /// <inheritdoc/>
  public void Clear()
  {
    STM.Retry(delegate
    {
      root.Set(Leaf);
      count.Set(0);
    });
  }

  /// <inheritdoc/>
  public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
  {
    if(array == null) throw new ArgumentNullException();
    if(arrayIndex < 0) throw new ArgumentOutOfRangeException();
    STM.Retry(delegate
    {
      if(arrayIndex + Count > array.Length) throw new ArgumentOutOfRangeException();
      int index = arrayIndex;
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
    return STM.Retry(delegate
    {
      TransactionalVariable<Node> variable;
      Node node = FindNode(item.Key, out variable);
      if(node == null || !object.Equals(node.Value, item.Value))
      {
        return false;
      }
      else
      {
        Remove(variable);
        return true;
      }
    });
  }
  #endregion

  #region IEnumerable<KeyValuePair<K,V>> Members
  /// <inheritdoc/>
  public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
  {
    throw new NotImplementedException();
  }
  #endregion

  #region IEnumerable Members
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
  #endregion

  #region Color
  enum Color : byte
  {
    Black=0, Red=1
  }
  #endregion

  #region Node
  sealed class Node : ICloneable
  {
    public Node() { }

    public Node(TransactionalVariable<Node> parent, K key, V value)
    {
      Parent = parent;
      Key    = key;
      Value  = value;
    }

    public object Clone()
    {
      Node node = new Node(); // return MemberwiseClone(); could also be used, but unfortunately it's much slower :-(
      node.Parent = Parent;
      node.Left   = Left;
      node.Right  = Right;
      node.Key    = Key;
      node.Value  = Value;
      node.Color  = Color;
      return node;
    }

    public TransactionalVariable<Node> Parent, Left, Right;
    public K Key;
    public V Value;
    public Color Color;
  }
  #endregion

  Node FindNode(K key)
  {
    TransactionalVariable<Node> variable;
    return STM.Retry(delegate { return FindNode(key, out variable); });
  }

  Node FindNode(K key, out TransactionalVariable<Node> variable)
  {
    TransactionalVariable<Node> nodeVar = root;
    Node node = root.Read();
    while(node != Leaf)
    {
      int cmp = comparer.Compare(key, node.Key);
      if(cmp == 0) break;
      nodeVar = cmp < 0 ? node.Left : node.Right;
      node    = nodeVar.Read();
    }
    variable = nodeVar;
    return node == Leaf ? null : node;
  }

  void Remove(TransactionalVariable<Node> variable)
  {
    throw new NotImplementedException();
  }

  /// <summary>Performs a left rotation, assuming all three nodes are open for writing.</summary>
  void RotateLeft(Node n, Node p, Node parent)
  {
    //    /           /
    //   P           N
    // A   N  -->  P   C
    //    B C     A B

    TransactionalVariable<Node> pVar = n.Parent, nVar = p.Right;

    // make N the new parent
    parent.Left = p.Right;
    n.Parent    = p.Parent;

    // put B under P
    p.Right = n.Left;
    if(p.Right != LeafVariable) p.Right.OpenForWrite().Parent = pVar;

    // put P under N
    n.Left   = pVar;
    p.Parent = nVar;
  }

  /// <summary>Performs a right rotation, assuming the all variables are open for writing.</summary>
  void RotateRight(Node n, Node p, Node parent)
  {
    //   \         \
    //    P         N
    //  N   C --> A   P
    // A B           B C

    TransactionalVariable<Node> pVar = n.Parent, nVar = p.Right;

    // make N the new parent
    parent.Right = p.Left;
    n.Parent     = p.Parent;

    // put B under P
    p.Left = n.Right;
    if(p.Left != LeafVariable) p.Left.OpenForWrite().Parent = pVar;

    // put P under N
    n.Right  = pVar;
    p.Parent = nVar;
  }

  readonly IComparer<K> comparer;
  readonly TransactionalVariable<Node> root;
  readonly TransactionalVariable<int> count;

  static readonly Node Leaf = new Node();
  static readonly TransactionalVariable<Node> LeafVariable = STM.Allocate(Leaf);
}
#endregion

} // namespace AdamMil.Transactions
