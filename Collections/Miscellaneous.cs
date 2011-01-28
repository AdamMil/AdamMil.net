/*
AdamMil.Collections is a library that provides useful collection classes for
the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2011 Adam Milazzo

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

namespace AdamMil.Collections
{

#region DelegateComparer
/// <summary>Provides an implementation of <see cref="IComparer{T}"/> that compares items using a <see cref="Comparison{T}"/>.</summary>
public sealed class DelegateComparer<T> : IComparer<T>
{
  /// <summary>Initializes a new <see cref="DelegateComparer{T}"/> with the given <see cref="Comparison{T}"/>.</summary>
  public DelegateComparer(Comparison<T> comparer)
  {
    if(comparer == null) throw new ArgumentNullException();
    this.comparer = comparer;
  }

  /// <summary>Compares two items.</summary>
  public int Compare(T x, T y)
  {
    return comparer(x, y);
  }

  readonly Comparison<T> comparer;
}
#endregion

#region ReadOnlyCollectionWrapperBase
/// <summary>Provides a base class for read-only collection wrappers.</summary>
public abstract class ReadOnlyCollectionWrapperBase
{
  /// <summary>Returns a <see cref="NotSupportedException"/> with a message stating that the collection is read-only.</summary>
  protected static NotSupportedException ReadOnlyException()
  {
    throw new NotSupportedException("This collection is read-only.");
  }
}
#endregion

#region ReadOnlyCollectionWrapper
/// <summary>Represents a read-only wrapper around a collection.</summary>
public sealed class ReadOnlyCollectionWrapper<T> : ReadOnlyCollectionWrapperBase, IReadOnlyCollection<T>, ICollection<T>
{
  /// <summary>Initializes a new <see cref="ReadOnlyCollectionWrapper{T}"/> around the given collection.</summary>
  public ReadOnlyCollectionWrapper(ICollection<T> collection)
  {
    if(collection == null) throw new ArgumentNullException();
    this.collection = collection;
  }

  /// <summary>Gets the number of items in the collection.</summary>
  public int Count
  {
    get { return collection.Count; }
  }

  /// <summary>Returns true if the collection contains the given item.</summary>
  public bool Contains(T item)
  {
    return collection.Contains(item);
  }

  /// <summary>Copies the items from the collection into the array starting at the given index.</summary>
  public void CopyTo(T[] array, int arrayIndex)
  {
    collection.CopyTo(array, arrayIndex);
  }

  /// <summary>Returns an enumerator that enumerates the items in the collection.</summary>
  public IEnumerator<T> GetEnumerator()
  {
    return collection.GetEnumerator();
  }

  /// <summary>Copies all of the items from the collection to a new array and returns it.</summary>
  public T[] ToArray()
  {
    T[] array = new T[collection.Count];
    collection.CopyTo(array, 0);
    return array;
  }

  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return ((System.Collections.IEnumerable)collection).GetEnumerator();
  }

  #region ICollection<T>
  int ICollection<T>.Count
  {
    get { return collection.Count; }
  }

  bool ICollection<T>.IsReadOnly
  {
    get { return true; }
  }

  void ICollection<T>.Add(T item)
  {
    throw ReadOnlyException();
  }

  void ICollection<T>.Clear()
  {
    throw ReadOnlyException();
  }

  bool ICollection<T>.Contains(T item)
  {
    return collection.Contains(item);
  }

  void ICollection<T>.CopyTo(T[] array, int arrayIndex)
  {
    collection.CopyTo(array, arrayIndex);
  }

  bool ICollection<T>.Remove(T item)
  {
    throw ReadOnlyException();
  }
  #endregion

  readonly ICollection<T> collection;
}
#endregion

#region ReadOnlyDictionaryWrapper
/// <summary>Represents a read-only wrapper around a dictionary.</summary>
public sealed class ReadOnlyDictionaryWrapper<K, V> : ReadOnlyCollectionWrapperBase, IReadOnlyDictionary<K, V>, IDictionary<K, V>
{
  /// <summary>Initializes this <see cref="ReadOnlyDictionaryWrapper{K,V}"/> with the given dictionary.</summary>
  public ReadOnlyDictionaryWrapper(IDictionary<K, V> dictionary)
  {
    if(dictionary == null) throw new ArgumentNullException();
    this.dictionary = dictionary;
    Keys   = new ReadOnlyCollectionWrapper<K>(dictionary.Keys);
    Values = new ReadOnlyCollectionWrapper<V>(dictionary.Values);
  }

  #region ICollection<KeyValuePair<K,V>> Members
  int ICollection<KeyValuePair<K, V>>.Count
  {
    get { return dictionary.Count; }
  }

  bool ICollection<KeyValuePair<K, V>>.IsReadOnly
  {
    get { return true; }
  }

  void ICollection<KeyValuePair<K, V>>.Add(KeyValuePair<K, V> item)
  {
    throw ReadOnlyException();
  }

  void ICollection<KeyValuePair<K, V>>.Clear()
  {
    throw ReadOnlyException();
  }

  bool ICollection<KeyValuePair<K, V>>.Contains(KeyValuePair<K, V> item)
  {
    return dictionary.Contains(item);
  }

  void ICollection<KeyValuePair<K, V>>.CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
  {
    dictionary.CopyTo(array, arrayIndex);
  }

  bool ICollection<KeyValuePair<K, V>>.Remove(KeyValuePair<K, V> item)
  {
    throw ReadOnlyException();
  }
  #endregion

  #region IDictionary<K,V> Members
  V IDictionary<K, V>.this[K key]
  {
    get { return this[key]; }
    set { throw ReadOnlyException(); }
  }

  ICollection<K> IDictionary<K, V>.Keys
  {
    get { return dictionary.Keys; }
  }

  ICollection<V> IDictionary<K, V>.Values
  {
    get { return dictionary.Values; }
  }

  void IDictionary<K, V>.Add(K key, V value)
  {
    throw ReadOnlyException();
  }

  bool IDictionary<K, V>.ContainsKey(K key)
  {
    return dictionary.ContainsKey(key);
  }

  bool IDictionary<K, V>.Remove(K key)
  {
    throw ReadOnlyException();
  }

  bool IDictionary<K, V>.TryGetValue(K key, out V value)
  {
    return dictionary.TryGetValue(key, out value);
  }
  #endregion

  #region IEnumerable Members
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return ((System.Collections.IEnumerable)dictionary).GetEnumerator();
  }
  #endregion

  #region IEnumerable<KeyValuePair<K,V>> Members
  /// <include file="documentation.xml" path="//Dictionary/GetEnumerator/*"/>
  public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
  {
    return dictionary.GetEnumerator();
  }
  #endregion

  #region IReadOnlyCollection<KeyValuePair<K,V>> Members
  /// <include file="documentation.xml" path="//Common/Count/*"/>
  public int Count
  {
    get { return dictionary.Count; }
  }

  /// <include file="documentation.xml" path="//Dictionary/Contains/*"/>
  public bool Contains(KeyValuePair<K, V> item)
  {
    return dictionary.Contains(item);
  }

  /// <include file="documentation.xml" path="//Dictionary/CopyTo/*"/>
  public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
  {
    dictionary.CopyTo(array, arrayIndex);
  }

  /// <include file="documentation.xml" path="//Dictionary/ToArray/*"/>
  public KeyValuePair<K, V>[] ToArray()
  {
    KeyValuePair<K, V>[] array = new KeyValuePair<K, V>[dictionary.Count];
    dictionary.CopyTo(array, 0);
    return array;
  }
  #endregion

  #region IReadOnlyDictionary<K,V> Members
  /// <include file="documentation.xml" path="//Dictionary/Indexer/*"/>
  public V this[K key]
  {
    get { return dictionary[key]; }
  }

  /// <include file="documentation.xml" path="//Dictionary/Keys/*"/>
  public IReadOnlyCollection<K> Keys
  {
    get; private set;
  }

  /// <include file="documentation.xml" path="//Dictionary/Values/*"/>
  public IReadOnlyCollection<V> Values
  {
    get; private set;
  }

  /// <include file="documentation.xml" path="//Dictionary/ContainsKey/*"/>
  public bool ContainsKey(K key)
  {
    return dictionary.ContainsKey(key);
  }

  /// <include file="documentation.xml" path="//Dictionary/TryGetValue/*"/>
  public bool TryGetValue(K key, out V value)
  {
    return dictionary.TryGetValue(key, out value);
  }
  #endregion

  readonly IDictionary<K, V> dictionary;
}
#endregion

#region ReadOnlyListWrapper
/// <summary>Represents a read-only wrapper around a list.</summary>
public sealed class ReadOnlyListWrapper<T> : ReadOnlyCollectionWrapperBase, IReadOnlyList<T>, IList<T>
{
  /// <summary>Initializes a new <see cref="ReadOnlyListWrapper{T}"/> around the given list.</summary>
  public ReadOnlyListWrapper(IList<T> list)
  {
    if(list == null) throw new ArgumentNullException();
    this.list = list;
  }

  /// <include file="documentation.xml" path="//Common/Indexer/*"/>
  public T this[int index]
  {
    get { return list[index]; }
  }

  /// <include file="documentation.xml" path="//Common/Count/*"/>
  public int Count
  {
    get { return list.Count; }
  }

  /// <include file="documentation.xml" path="//Common/Contains/*"/>
  public bool Contains(T item)
  {
    return list.Contains(item);
  }

  /// <include file="documentation.xml" path="//Common/CopyTo/*"/>
  public void CopyTo(T[] array, int arrayIndex)
  {
    list.CopyTo(array, arrayIndex);
  }

  /// <include file="documentation.xml" path="//Common/GetEnumerator/*"/>
  public IEnumerator<T> GetEnumerator()
  {
    return list.GetEnumerator();
  }

  /// <include file="documentation.xml" path="//Common/IndexOf/*"/>
  public int IndexOf(T item)
  {
    return list.IndexOf(item);
  }

  /// <include file="documentation.xml" path="//Common/ToArray/*"/>
  public T[] ToArray()
  {
    T[] array = new T[list.Count];
    list.CopyTo(array, 0);
    return array;
  }

  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return ((System.Collections.IEnumerable)list).GetEnumerator();
  }

  #region ICollection<T>
  int ICollection<T>.Count
  {
    get { return list.Count; }
  }

  bool ICollection<T>.IsReadOnly
  {
    get { return true; }
  }

  void ICollection<T>.Add(T item)
  {
    throw ReadOnlyException();
  }

  void ICollection<T>.Clear()
  {
    throw ReadOnlyException();
  }

  bool ICollection<T>.Contains(T item)
  {
    return list.Contains(item);
  }

  void ICollection<T>.CopyTo(T[] array, int arrayIndex)
  {
    list.CopyTo(array, arrayIndex);
  }

  bool ICollection<T>.Remove(T item)
  {
    throw ReadOnlyException();
  }
  #endregion

  #region IList<T>
  T IList<T>.this[int index]
  {
    get { return list[index]; }
    set { throw ReadOnlyException(); }
  }

  int IList<T>.IndexOf(T item)
  {
    return list.IndexOf(item);
  }

  void IList<T>.Insert(int index, T item)
  {
    throw ReadOnlyException();
  }

  void IList<T>.RemoveAt(int index)
  {
    throw ReadOnlyException();
  }
  #endregion

  readonly IList<T> list;
}
#endregion

#region ReversedComparer
/// <summary>Implements a comparer that wraps another comparer and returns the opposite comparison.</summary>
public sealed class ReversedComparer<T> : IComparer<T>
{
  /// <summary>Initializes a new <see cref="ReversedComparer{T}"/> wrapping the given comparer.</summary>
  public ReversedComparer(IComparer<T> comparer)
  {
    if(comparer == null) throw new ArgumentNullException();
    cmp = comparer;
  }

  /// <summary>Compares the two items, returning the opposite of the comparison given by the comparer with which this
  /// object was initialized.
  /// </summary>
  public int Compare(T a, T b)
  {
    // we can't use something like -cmp.Compare(a, b) because if it returned int.MinValue, then it could not be negated
    return cmp.Compare(b, a);
  }

  readonly IComparer<T> cmp;
}
#endregion

} // namespace AdamMil.Collections
