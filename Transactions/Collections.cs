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

// TODO: recheck the performance of MemberwiseClone() versus manual cloning to see if it's improved in .NET 4

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
  public TransactionalArray(int length, IComparer<T> comparer)
  {
    if(length < 0) throw new ArgumentOutOfRangeException();
    array = new TransactionalVariable<T>[length];
    for(int i=0; i<array.Length; i++) array[i] = new TransactionalVariable<T>();

    this.comparer = comparer == null ? Comparer<T>.Default :  comparer;
  }

  /// <summary>Initializes a new <see cref="TransactionalArray{T}"/> with the given set of items.</summary>
  public TransactionalArray(IEnumerable<T> initialItems) : this(initialItems, null) { }
  /// <summary>Initializes a new <see cref="TransactionalArray{T}"/> with the given set of items, and using the given
  /// <see cref="IComparer{T}"/> to compare elements.
  /// </summary>
  public TransactionalArray(IEnumerable<T> initialItems, IComparer<T> comparer)
  {
    if(initialItems == null) throw new ArgumentNullException();
    ICollection<T> collection = initialItems as ICollection<T>;
    if(collection == null) collection = new List<T>(initialItems);

    array = new TransactionalVariable<T>[collection.Count];
    int index = 0;
    foreach(T item in initialItems) array[index] = new TransactionalVariable<T>(item);
    if(index != array.Length) throw new ArgumentException();

    this.comparer = comparer == null ? Comparer<T>.Default :  comparer;
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
        if(comparer.Compare(item, array[i].Read()) == 0) return i;
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
    return IndexOf(item) != -1;
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
  readonly IComparer<T> comparer;
}
#endregion

// TODO: ensure that we check for null keys
#region TransactionalDictionary
public sealed class TransactionalDictionary<K, V> : IDictionary<K, V>
{
  public TransactionalDictionary() : this((IComparer<K>)null) { }
  public TransactionalDictionary(IComparer<K> comparer)
  {
    this.comparer = comparer ?? Comparer<K>.Default;
    this.root     = new TransactionalVariable<Node>();
    this.count    = new TransactionalVariable<int>();
  }

  public TransactionalDictionary(IEnumerable<KeyValuePair<K, V>> initialItems) : this(initialItems, null) { }
  public TransactionalDictionary(IEnumerable<KeyValuePair<K, V>> initialItems, IComparer<K> comparer) : this(comparer)
  {
    if(initialItems == null) throw new ArgumentNullException();
    using(STMTransaction tx = STMTransaction.Create())
    {
      foreach(KeyValuePair<K, V> pair in initialItems) Add(pair.Key, pair.Value);
      tx.Commit();
    }
  }

  #region IDictionary<K,V> Members
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
      STM.Retry(delegate
      {
        TransactionalVariable<Node> variable;
        Node node = FindNode(key, out variable);
        if(node == null) throw new KeyNotFoundException();
        variable.OpenForWrite().Value = value;
      });
    }
  }

  public ICollection<K> Keys
  {
    get { throw new NotImplementedException(); }
  }

  public ICollection<V> Values
  {
    get { throw new NotImplementedException(); }
  }

  public void Add(K key, V value)
  {
    throw new NotImplementedException();
  }

  public bool ContainsKey(K key)
  {
    return FindNode(key) != null;
  }

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
  public int Count
  {
    get { return count.Read(); }
  }

  public bool IsReadOnly
  {
    get { return false; }
  }

  public void Clear()
  {
    STM.Retry(delegate
    {
      root.Set(null);
      count.Set(0);
    });
  }

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
  public enum Color : byte
  {
    Black=0, Red=1
  }
  #endregion

  #region Node
  sealed class Node : ICloneable
  {
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
    Node node;
    do
    {
      node = nodeVar.Read();
      int cmp = comparer.Compare(key, node.Key);
      if(cmp == 0) break;
      else nodeVar = cmp < 0 ? node.Left : node.Right;
    } while(nodeVar != Leaf);
    variable = nodeVar;
    return nodeVar == Leaf ? null : node;
  }

  void Remove(TransactionalVariable<Node> variable)
  {
    throw new NotImplementedException();
  }

  readonly IComparer<K> comparer;
  readonly TransactionalVariable<Node> root;
  readonly TransactionalVariable<int> count;

  static readonly TransactionalVariable<Node> Leaf = new TransactionalVariable<Node>();
}
#endregion

} // namespace AdamMil.Transactions
