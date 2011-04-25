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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using AdamMil.Utilities;

namespace AdamMil.Collections
{

#region AccessLimitedCollectionBase
/// <summary>Provides a base class that can be used to implement collections that have restricted editing capabilities. For
/// instance, you may only want to allow items to be added in a certain way, or you may not want items to be able to be removed
/// once they are added.
/// </summary>
[Serializable]
public abstract class AccessLimitedCollectionBase<T> : IList<T>
{
  protected AccessLimitedCollectionBase()
  {
    Items = new List<T>();
  }

  protected AccessLimitedCollectionBase(IEnumerable<T> items) : this()
  {
    Items.AddRange(items);
  }

  public T this[int index]
  {
    get { return Items[index]; }
  }

  public int Count
  {
    get { return Items.Count; }
  }

  public virtual bool IsReadOnly
  {
    get { return false; }
  }

  public bool Contains(T item)
  {
    return IndexOf(item) != -1;
  }

  public void CopyTo(T[] array, int arrayIndex)
  {
    Items.CopyTo(array, arrayIndex);
  }

  public IEnumerator<T> GetEnumerator()
  {
    return Items.GetEnumerator();
  }

  public virtual int IndexOf(T item)
  {
    return Items.IndexOf(item);
  }

  public T[] ToArray()
  {
    T[] array = new T[Count];
    CopyTo(array, 0);
    return array;
  }

  protected List<T> Items
  {
    get; private set;
  }

  #region IEnumerable
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
  #endregion

  #region IList<T> Members
  T IList<T>.this[int index]
  {
    get { return Items[index]; }
    set { throw new NotSupportedException(); }
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
}
#endregion

#region CollectionBase
/// <summary>Provides a flexible base class for new collections.</summary>
[Serializable]
public abstract class CollectionBase<T> : IList<T>
{
  /// <summary>Initializes a new <see cref="CollectionBase{T}"/>.</summary>
  protected CollectionBase()
  {
    Items = new List<T>();
  }

  /// <summary>Initializes a new <see cref="CollectionBase{T}"/> with an existing list of items.</summary>
  protected CollectionBase(IEnumerable<T> items) : this()
  {
    AddRange(items);
  }

  /// <summary>Gets or sets the item at the given index, which must be from 0 to <see cref="Count"/>-1.</summary>
  public T this[int index]
  {
    get { return Items[index]; }
    set { SetItem(index, value); }
  }

  /// <summary>Gets the number of items in the collection.</summary>
  public int Count
  {
    get { return Items.Count; }
  }

  /// <summary>Gets whether the collection is read only.</summary>
  public virtual bool IsReadOnly
  {
    get { return false; }
  }

  /// <summary>Adds the given item to the collection.</summary>
  public void Add(T item)
  {
    AssertNotReadOnly();
    InsertItem(Count, item);
  }

  /// <summary>Adds a list of items to the collection.</summary>
  public void AddRange(IEnumerable<T> items)
  {
    if(items == null) throw new ArgumentNullException();
    AssertNotReadOnly();

    // if we know how many items there are, use that knowledge to preallocate space within the collection
    ICollection collection = items as ICollection;
    if(collection != null)
    {
      int newCount = Items.Count + collection.Count, capacity = Items.Capacity;
      if(capacity == 0) capacity = 4;
      if(capacity < newCount)
      {
        do capacity *= 2; while(capacity < newCount);
        Items.Capacity = capacity;
      }
    }

    foreach(T item in items) Add(item);
  }

  /// <summary>Adds a list of items to the collection.</summary>
  public void AddRange(params T[] items)
  {
    AddRange((IEnumerable<T>)items);
  }

  /// <include file="documentation.xml" path="//Common/Clear/*"/>
  public void Clear()
  {
    AssertNotReadOnly();
    if(Items.Count != 0)
    {
      ClearItems();
      OnCollectionChanged();
    }
  }

  /// <include file="documentation.xml" path="//Common/Contains/*"/>
  public bool Contains(T item)
  {
    return IndexOf(item) != -1;
  }

  /// <include file="documentation.xml" path="//Common/CopyTo/*"/>
  public void CopyTo(T[] array, int arrayIndex)
  {
    Items.CopyTo(array, arrayIndex);
  }

  /// <include file="documentation.xml" path="//Common/GetEnumerator/*"/>
  public IEnumerator<T> GetEnumerator()
  {
    return Items.GetEnumerator();
  }

  /// <include file="documentation.xml" path="//Common/IndexOf/*"/>
  public virtual int IndexOf(T item)
  {
    return Items.IndexOf(item);
  }

  /// <include file="documentation.xml" path="//Common/Insert/*"/>
  public void Insert(int index, T item)
  {
    if((uint)index > (uint)Count) throw new ArgumentOutOfRangeException();
    AssertNotReadOnly();
    InsertItem(index, item);
  }

  /// <include file="documentation.xml" path="//Common/Remove/*"/>
  public bool Remove(T item)
  {
    AssertNotReadOnly();
    int index = IndexOf(item);
    if(index == -1)
    {
      return false;
    }
    else
    {
      RemoveAt(index);
      return true;
    }
  }

  /// <include file="documentation.xml" path="//Common/RemoveAt/*"/>
  public void RemoveAt(int index)
  {
    AssertNotReadOnly();
    RemoveItem(index, this[index]);
  }

  /// <include file="documentation.xml" path="//Common/ToArray/*"/>
  public T[] ToArray()
  {
    T[] array = new T[Count];
    CopyTo(array, 0);
    return array;
  }

  /// <summary>Gets a reference to the underlying list of items. Modifying this list will not trigger any events (e.g.
  /// <see cref="ClearItems"/>, <see cref="InsertItem"/>, <see cref="RemoveItem"/>, <see cref="SetItem"/>, etc).
  /// </summary>
  protected List<T> Items
  {
    get; private set;
  }

  /// <summary>Throws an exception if the collection is read-only.</summary>
  protected void AssertNotReadOnly()
  {
    if(IsReadOnly) throw new InvalidOperationException("The collection is read-only.");
  }

  /// <include file="documentation.xml" path="//CollectionBase/ClearItems/*"/>
  protected virtual void ClearItems()
  {
    Items.Clear();
    OnCollectionChanged();
  }

  /// <include file="documentation.xml" path="//CollectionBase/InsertItem/*"/>
  protected virtual void InsertItem(int index, T item)
  {
    Items.Insert(index, item);
    OnCollectionChanged();
  }

  /// <include file="documentation.xml" path="//CollectionBase/RemoveItem/*"/>
  protected virtual void RemoveItem(int index, T item)
  {
    Items.RemoveAt(index);
    OnCollectionChanged();
  }

  /// <include file="documentation.xml" path="//CollectionBase/SetItem/*"/>
  protected virtual void SetItem(int index, T item)
  {
    Items[index] = item;
    OnCollectionChanged();
  }

  /// <include file="documentation.xml" path="//CollectionBase/OnCollectionChanged/*"/>
  protected virtual void OnCollectionChanged()
  {
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
}
#endregion

#region ValidatedCollection
/// <summary>Represents a collection that validates the items being added.</summary>
[Serializable]
public abstract class ValidatedCollection<T> : CollectionBase<T>
{
  /// <summary>Initializes a new <see cref="ValidatedCollection{T}"/>.</summary>
  protected ValidatedCollection() { }
  /// <summary>Initializes a new <see cref="ValidatedCollection{T}"/> with the given list of items.</summary>
  protected ValidatedCollection(IEnumerable<T> items) : base(items) { }

  /// <include file="documentation.xml" path="//CollectionBase/InsertItem/*"/>
  protected override void InsertItem(int index, T item)
  {
    ValidateItem(item, index);
    base.InsertItem(index, item);
  }

  /// <include file="documentation.xml" path="//CollectionBase/SetItem/*"/>
  protected override void SetItem(int index, T item)
  {
    ValidateItem(item, index);
    base.SetItem(index, item);
  }

  /// <include file="documentation.xml" path="//ValidatedCollection/ValidateItem/*"/>
  protected abstract void ValidateItem(T item, int index);
}
#endregion

#region NonNullCollection
/// <summary>Represents a collection that validates the items being added to ensure that none are null.</summary>
[Serializable]
public class NonNullCollection<T> : ValidatedCollection<T> where T : class
{
  /// <summary>Initializes a new <see cref="NonNullCollection{T}"/>.</summary>
  public NonNullCollection() { }
  /// <summary>Initializes a new <see cref="NonNullCollection{T}"/> with the given list of items.</summary>
  public NonNullCollection(IEnumerable<T> items) : base(items) { }

  /// <include file="documentation.xml" path="//ValidatedCollection/ValidateItem/*"/>
  protected override void ValidateItem(T item, int index)
  {
    if(item == null) throw new ArgumentNullException();
  }
}
#endregion

#region NonEmptyStringCollection
/// <summary>Represents a collection of strings that validates the items being added to ensure that none are null or empty.</summary>
[Serializable]
public class NonEmptyStringCollection : ValidatedCollection<string>
{
  public NonEmptyStringCollection() { }

  /// <include file="documentation.xml" path="//ValidatedCollection/ValidateItem/*"/>
  protected override void ValidateItem(string item, int index)
  {
    if(string.IsNullOrEmpty(item)) throw new ArgumentException();
  }
}
#endregion

#region DictionaryBase
[Serializable]
public abstract class DictionaryBase<K, V> : IDictionary<K, V>
{
  protected DictionaryBase() { }

  protected DictionaryBase(IDictionary<K, V> initialItems)
  {
    items = new Dictionary<K, V>(initialItems);
  }

  public V this[K key]
  {
    get
    {
      if(items == null) throw new KeyNotFoundException();
      return items[key];
    }
    set
    {
      AssertWritable();
      SetItem(key, value);
    }
  }

  public int Count
  {
    get { return items == null ? 0 : items.Count; }
  }

  public virtual bool IsReadOnly
  {
    get { return false; }
  }

  public ICollection<K> Keys
  {
    get { return items == null ? (ICollection<K>)new K[0] : items.Keys; }
  }

  public ICollection<V> Values
  {
    get { return items == null ? (ICollection<V>)new V[0] : items.Values; }
  }

  public void Add(K key, V value)
  {
    AssertWritable();
    AddItem(key, value);
  }

  public virtual void Clear()
  {
    AssertWritable();
    if(Count != 0) ClearItems();
  }

  public bool ContainsKey(K key)
  {
    return items != null && items.ContainsKey(key);
  }

  public bool Remove(K key)
  {
    AssertWritable();
    return items != null && RemoveItem(key);
  }

  public bool TryGetValue(K key, out V value)
  {
    if(items == null)
    {
      value = default(V);
      return false;
    }
    else
    {
      return items.TryGetValue(key, out value);
    }
  }

  protected Dictionary<K, V> Items
  {
    get
    {
      if(items == null) items = new Dictionary<K, V>();
      return items;
    }
  }

  /// <summary>Called when an item is being added to the dictionary. The base implementation actually performs the addition.</summary>
  protected virtual void AddItem(K key, V value)
  {
    Items.Add(key, value);
    OnCollectionChanged();
  }

  /// <summary>Called when the dictionary is being cleared. The base implementation actually clears the dictionary.</summary>
  protected virtual void ClearItems()
  {
    items = null;
    OnCollectionChanged();
  }

  /// <summary>Called when an item is being removed from the dictionary. The base implementation actually performs the removal.</summary>
  protected virtual bool RemoveItem(K key)
  {
    if(items.Remove(key))
    {
      OnCollectionChanged();
      return true;
    }
    else
    {
      return false;
    }
  }

  /// <summary>Called when an item in the dictionary is being assigned. The base implementation actually performs the assignment.</summary>
  protected virtual void SetItem(K key, V value)
  {
    Items[key] = value;
    OnCollectionChanged();
  }

  /// <summary>Called when the collection may have been changed by the user.</summary>
  protected virtual void OnCollectionChanged()
  {
  }

  void AssertWritable()
  {
    if(IsReadOnly) throw new InvalidOperationException("The collection is read-only.");
  }

  Dictionary<K, V> items;

  #region ICollection<KeyValuePair<K,V>> Members
  void ICollection<KeyValuePair<K, V>>.Add(KeyValuePair<K, V> item)
  {
    Add(item.Key, item.Value);
  }

  bool ICollection<KeyValuePair<K, V>>.Contains(KeyValuePair<K, V> item)
  {
    if(item.Key == null) return false;
    V value;
    return TryGetValue(item.Key, out value) && object.Equals(value, item.Value);
  }

  void ICollection<KeyValuePair<K, V>>.CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
  {
    Utility.ValidateRange(array, arrayIndex, Count);
    if(items != null)
    {
      foreach(KeyValuePair<K, V> pair in items) array[arrayIndex++] = pair;
    }
  }

  bool ICollection<KeyValuePair<K, V>>.Remove(KeyValuePair<K, V> item)
  {
    return ((ICollection<KeyValuePair<K, V>>)this).Contains(item) && Remove(item.Key);
  }
  #endregion

  #region IEnumerable<KeyValuePair<K,V>> Members
  public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
  {
    return items == null ? (IEnumerator<KeyValuePair<K, V>>)new EmptyEnumerable<KeyValuePair<K, V>>() : items.GetEnumerator();
  }
  #endregion

  #region IEnumerable Members
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
  #endregion
}
#endregion

#region ValidatedDictionary
/// <summary>Represents a dictionary that validates the items being added.</summary>
[Serializable]
public abstract class ValidatedDictionary<K, V> : DictionaryBase<K, V>
{
  protected ValidatedDictionary() { }

  protected ValidatedDictionary(IDictionary<K, V> initialItems)
  {
    if(initialItems == null) throw new ArgumentNullException();
    foreach(KeyValuePair<K, V> pair in initialItems)
    {
      ValidateItem(pair.Key, pair.Value);
      Items[pair.Key] = pair.Value;
    }
  }

  protected override void AddItem(K key, V value)
  {
    ValidateItem(key, value);
    base.AddItem(key, value);
  }

  protected override void SetItem(K key, V value)
  {
    ValidateItem(key, value);
    base.SetItem(key, value);
  }

  protected abstract void ValidateItem(K key, V value);
}
#endregion

#region NonEmptyStringDictionary
/// <summary>Represents a dictionary of strings that validates the items being added to ensure that none are null or empty.</summary>
[Serializable]
public class NonEmptyStringDictionary : ValidatedDictionary<string, string>
{
  public NonEmptyStringDictionary() { }

  protected override void ValidateItem(string key, string value)
  {
    if(string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)) throw new ArgumentException();
  }
}
#endregion

#region MultiValuedDictionaryBase
[Serializable]
public abstract class MultiValuedDictionaryBase<TKey, TValue, TValueCollection>
  : Dictionary<TKey, TValueCollection> where TValueCollection : ICollection<TValue>
{
  protected MultiValuedDictionaryBase() { }
  protected MultiValuedDictionaryBase(SerializationInfo info, StreamingContext context) : base(info, context) { }

  public void Add(TKey key, TValue value)
  {
    TValueCollection list;
    if(!TryGetValue(key, out list)) this[key] = list = CreateCollection();
    list.Add(value);
  }

  public void AddRange(TKey key, IEnumerable<TValue> values)
  {
    TValueCollection list;
    if(!TryGetValue(key, out list)) this[key] = list = CreateCollection();
    AddRange(list, values);
  }

  public bool Contains(TKey key, TValue value)
  {
    TValueCollection list;
    return TryGetValue(key, out list) && list.Contains(value);
  }

  public IEnumerable<TValue> Enumerate(TKey key)
  {
    TValueCollection list;
    return TryGetValue(key, out list) ? (IEnumerable<TValue>)list : new EmptyEnumerable<TValue>();
  }

  public bool Remove(TKey key, TValue value)
  {
    TValueCollection list;
    return TryGetValue(key, out list) && list.Remove(value);
  }

  protected virtual void AddRange(TValueCollection collection, IEnumerable<TValue> items)
  {
    if(collection == null || items == null) throw new ArgumentNullException();
    foreach(TValue item in items) collection.Add(item);
  }

  protected abstract TValueCollection CreateCollection();
}
#endregion

#region MultiValuedDictionary
[Serializable]
public class MultiValuedDictionary<TKey, TValue> : MultiValuedDictionaryBase<TKey, TValue, List<TValue>>
{
  public MultiValuedDictionary() { }
  protected MultiValuedDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

  protected override void AddRange(List<TValue> collection, IEnumerable<TValue> items)
  {
    if(collection == null) throw new ArgumentNullException();
    collection.AddRange(items);
  }

  protected override List<TValue> CreateCollection()
  {
    return new List<TValue>();
  }
}
#endregion

#region HashSetDictionary
[Serializable]
public class HashSetDictionary<TKey, TValue> : MultiValuedDictionaryBase<TKey, TValue, HashSet<TValue>>
{
  public HashSetDictionary() { }
  protected HashSetDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

  protected override HashSet<TValue> CreateCollection()
  {
    return new HashSet<TValue>();
  }
}
#endregion

} // namespace AdamMil.Collections
