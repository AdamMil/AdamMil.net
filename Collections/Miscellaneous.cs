/*
AdamMil.Collections is a library that provides useful collection classes for
the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2010 Adam Milazzo

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

#region ReadOnlyCollectionWrapper
/// <summary>Represents a read-only wrapper around a collection.</summary>
public sealed class ReadOnlyCollectionWrapper<T> : IReadOnlyCollection<T>, ICollection<T>
{
  /// <summary>Initializes a new <see cref="ReadOnlyCollectionWrapper{T}"/> around the given collection.</summary>
  /// <param name="collection"></param>
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

  readonly ICollection<T> collection;

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
    ThrowReadOnlyException();
  }

  void ICollection<T>.Clear()
  {
    ThrowReadOnlyException();
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
    ThrowReadOnlyException();
    return false;
  }

  static void ThrowReadOnlyException()
  {
    throw new NotSupportedException("This collection is read-only.");
  }
  #endregion
}
#endregion

#region ReadOnlyDictionaryWrapper
/// <summary>Represents a read-only wrapper around a dictionary.</summary>
public sealed class ReadOnlyDictionaryWrapper<K, V> : IReadOnlyDictionary<K, V>
{
  /// <summary>Initializes this <see cref="ReadOnlyDictionaryWrapper{K,V}"/> with the given dictionary.</summary>
  public ReadOnlyDictionaryWrapper(IDictionary<K, V> dictionary)
  {
    if(dictionary == null) throw new ArgumentNullException();
    this.dictionary = dictionary;
    Keys   = new ReadOnlyCollectionWrapper<K>(dictionary.Keys);
    Values = new ReadOnlyCollectionWrapper<V>(dictionary.Values);
  }

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

  #region IEnumerable<KeyValuePair<K,V>> Members
  /// <include file="documentation.xml" path="//Dictionary/GetEnumerator/*"/>
  public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
  {
    return dictionary.GetEnumerator();
  }
  #endregion

  #region IEnumerable Members
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return ((System.Collections.IEnumerable)dictionary).GetEnumerator();
  }
  #endregion

  readonly IDictionary<K, V> dictionary;
}
#endregion

#region ReadOnlyListWrapper
/// <summary>Represents a read-only wrapper around a list.</summary>
public sealed class ReadOnlyListWrapper<T> : IReadOnlyList<T>, IList<T>
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

  readonly IList<T> list;

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
    ThrowReadOnlyException();
  }

  void ICollection<T>.Clear()
  {
    ThrowReadOnlyException();
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
    ThrowReadOnlyException();
    return false;
  }

  static void ThrowReadOnlyException()
  {
    throw new NotSupportedException("This collection is read-only.");
  }
  #endregion

  #region IList<T>
  T IList<T>.this[int index]
  {
    get { return list[index]; }
    set { ThrowReadOnlyException(); }
  }

  int IList<T>.IndexOf(T item)
  {
    return list.IndexOf(item);
  }

  void IList<T>.Insert(int index, T item)
  {
    ThrowReadOnlyException();
  }

  void IList<T>.RemoveAt(int index)
  {
    ThrowReadOnlyException();
  }
  #endregion
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
    return -cmp.Compare(a, b);
  }

  readonly IComparer<T> cmp;
}
#endregion

} // namespace AdamMil.Collections