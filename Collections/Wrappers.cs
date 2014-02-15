/*
AdamMil.Collections is a library that provides useful collection classes for
the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2013 Adam Milazzo

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
public sealed class ReadOnlyDictionaryWrapper<TKey, TValue>
  : ReadOnlyCollectionWrapperBase, IReadOnlyDictionary<TKey, TValue>, IDictionary<TKey, TValue>
{
  /// <summary>Initializes this <see cref="ReadOnlyDictionaryWrapper{TKey,TValue}"/> with the given dictionary.</summary>
  public ReadOnlyDictionaryWrapper(IDictionary<TKey, TValue> dictionary)
  {
    if(dictionary == null) throw new ArgumentNullException();
    this.dictionary = dictionary;
    Keys   = new ReadOnlyCollectionWrapper<TKey>(dictionary.Keys);
    Values = new ReadOnlyCollectionWrapper<TValue>(dictionary.Values);
  }

  #region ICollection<KeyValuePair<K,V>> Members
  int ICollection<KeyValuePair<TKey, TValue>>.Count
  {
    get { return dictionary.Count; }
  }

  bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
  {
    get { return true; }
  }

  void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
  {
    throw ReadOnlyException();
  }

  void ICollection<KeyValuePair<TKey, TValue>>.Clear()
  {
    throw ReadOnlyException();
  }

  bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
  {
    return dictionary.Contains(item);
  }

  void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
  {
    dictionary.CopyTo(array, arrayIndex);
  }

  bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
  {
    throw ReadOnlyException();
  }
  #endregion

  #region IDictionary<K,V> Members
  TValue IDictionary<TKey, TValue>.this[TKey key]
  {
    get { return dictionary[key]; }
    set { throw ReadOnlyException(); }
  }

  ICollection<TKey> IDictionary<TKey, TValue>.Keys
  {
    get { return dictionary.Keys; }
  }

  ICollection<TValue> IDictionary<TKey, TValue>.Values
  {
    get { return dictionary.Values; }
  }

  void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
  {
    throw ReadOnlyException();
  }

  bool IDictionary<TKey, TValue>.ContainsKey(TKey key)
  {
    return dictionary.ContainsKey(key);
  }

  bool IDictionary<TKey, TValue>.Remove(TKey key)
  {
    throw ReadOnlyException();
  }

  bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
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
  /// <include file="documentation.xml" path="//Dictionary/GetEnumerator/node()"/>
  public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
  {
    return dictionary.GetEnumerator();
  }
  #endregion

  #region IReadOnlyCollection<KeyValuePair<K,V>> Members
  /// <include file="documentation.xml" path="//Common/Count/node()"/>
  public int Count
  {
    get { return dictionary.Count; }
  }

  /// <include file="documentation.xml" path="//Dictionary/Contains/node()"/>
  public bool Contains(KeyValuePair<TKey, TValue> item)
  {
    return dictionary.Contains(item);
  }

  /// <include file="documentation.xml" path="//Dictionary/CopyTo/node()"/>
  public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
  {
    dictionary.CopyTo(array, arrayIndex);
  }

  /// <include file="documentation.xml" path="//Dictionary/ToArray/node()"/>
  public KeyValuePair<TKey, TValue>[] ToArray()
  {
    KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[dictionary.Count];
    dictionary.CopyTo(array, 0);
    return array;
  }
  #endregion

  #region IReadOnlyDictionary<K,V> Members
  /// <include file="documentation.xml" path="//Dictionary/Indexer/node()"/>
  public TValue this[TKey key]
  {
    get { return dictionary[key]; }
  }

  /// <include file="documentation.xml" path="//Dictionary/Keys/node()"/>
  public IReadOnlyCollection<TKey> Keys
  {
    get; private set;
  }

  /// <include file="documentation.xml" path="//Dictionary/Values/node()"/>
  public IReadOnlyCollection<TValue> Values
  {
    get; private set;
  }

  /// <include file="documentation.xml" path="//Dictionary/ContainsKey/node()"/>
  public bool ContainsKey(TKey key)
  {
    return dictionary.ContainsKey(key);
  }

  /// <include file="documentation.xml" path="//Dictionary/TryGetValue/node()"/>
  public bool TryGetValue(TKey key, out TValue value)
  {
    return dictionary.TryGetValue(key, out value);
  }
  #endregion

  readonly IDictionary<TKey, TValue> dictionary;
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

  /// <include file="documentation.xml" path="//Common/Indexer/node()"/>
  public T this[int index]
  {
    get { return list[index]; }
  }

  /// <include file="documentation.xml" path="//Common/Count/node()"/>
  public int Count
  {
    get { return list.Count; }
  }

  /// <include file="documentation.xml" path="//Common/Contains/node()"/>
  public bool Contains(T item)
  {
    return list.Contains(item);
  }

  /// <include file="documentation.xml" path="//Common/CopyTo/node()"/>
  public void CopyTo(T[] array, int arrayIndex)
  {
    list.CopyTo(array, arrayIndex);
  }

  /// <include file="documentation.xml" path="//Common/GetEnumerator/node()"/>
  public IEnumerator<T> GetEnumerator()
  {
    return list.GetEnumerator();
  }

  /// <include file="documentation.xml" path="//Common/IndexOf/node()"/>
  public int IndexOf(T item)
  {
    return list.IndexOf(item);
  }

  /// <include file="documentation.xml" path="//Common/ToArray/node()"/>
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

} // namespace AdamMil.Collections
