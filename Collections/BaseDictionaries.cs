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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using AdamMil.Utilities;

namespace AdamMil.Collections
{

#region AccessLimitedDictionaryBase
/// <summary>Provides a base class that can be used to implement dictionaries that have restricted editing capabilities. For instance, you
/// may only want to allow items to be added in a certain way, or you may not want items to be able to be removed once they are added.
/// </summary>
[Serializable]
public abstract class AccessLimitedDictionaryBase<TKey,TValue> : IDictionary<TKey,TValue>
{
  /// <summary>Initializes an empty <see cref="AccessLimitedDictionaryBase{K,V}"/>.</summary>
  protected AccessLimitedDictionaryBase()
  {
    Items = new Dictionary<TKey, TValue>();
  }

  /// <summary>Initializes an empty <see cref="AccessLimitedDictionaryBase{K,V}"/>.</summary>
  protected AccessLimitedDictionaryBase(IEqualityComparer<TKey> comparer)
  {
    Items = new Dictionary<TKey, TValue>(comparer);
  }

  /// <summary>Initializes an <see cref="AccessLimitedDictionaryBase{K,V}"/> with the given items.</summary>
  protected AccessLimitedDictionaryBase(IDictionary<TKey, TValue> items)
  {
    Items = new Dictionary<TKey, TValue>(items);
  }

  /// <summary>Initializes an <see cref="AccessLimitedDictionaryBase{K,V}"/> with the given items.</summary>
  protected AccessLimitedDictionaryBase(IDictionary<TKey, TValue> items, IEqualityComparer<TKey> comparer)
  {
    Items = new Dictionary<TKey, TValue>(items, comparer);
  }

  /// <inheritdoc/>
  public TValue this[TKey key]
  {
    get { return Items[key]; }
    set { throw new NotSupportedException(); }
  }

  /// <inheritdoc/>
  public int Count
  {
    get { return Items.Count; }
  }

  /// <inheritdoc/>
  /// <remarks>The default implementation returns false. If your derived collection is completely read-only, you should override this
  /// property and return true.
  /// </remarks>
  public virtual bool IsReadOnly
  {
    get { return false; }
  }

  /// <inheritdoc/>
  public ICollection<TKey> Keys
  {
    get { return Items.Keys; }
  }

  /// <inheritdoc/>
  public ICollection<TValue> Values
  {
    get { return Items.Values; }
  }

  /// <inheritdoc/>
  public bool ContainsKey(TKey key)
  {
    return Items.ContainsKey(key);
  }

  /// <inheritdoc/>
  public bool TryGetValue(TKey key, out TValue value)
  {
    return Items.TryGetValue(key, out value);
  }

  /// <summary>Gets the underlying, writable dictionary that contains the items.</summary>
  protected Dictionary<TKey, TValue> Items { get; private set; }

  #region ICollection<KeyValuePair<TKey,TValue>> Members
  void ICollection<KeyValuePair<TKey,TValue>>.Add(KeyValuePair<TKey, TValue> item)
  {
    throw new NotSupportedException();
  }

  void ICollection<KeyValuePair<TKey, TValue>>.Clear()
  {
    throw new NotSupportedException();
  }

  bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
  {
    return ((ICollection<KeyValuePair<TKey, TValue>>)Items).Contains(item);
  }

  void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
  {
    ((ICollection<KeyValuePair<TKey, TValue>>)Items).CopyTo(array, arrayIndex);
  }

  bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
  {
    throw new NotSupportedException();
  }
  #endregion

  #region IDictionary<TKey,TValue> Members
  void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
  {
    throw new NotSupportedException();
  }

  bool IDictionary<TKey,TValue>.Remove(TKey key)
  {
    throw new NotSupportedException();
  }
  #endregion

  #region IEnumerable<KeyValuePair<TKey,TValue>> Members
  /// <inheritdoc/>
  public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
  {
    return Items.GetEnumerator();
  }
  #endregion

  #region IEnumerable Members
  IEnumerator IEnumerable.GetEnumerator()
  {
    return Items.GetEnumerator();
  }
  #endregion
}
#endregion

#region DictionaryBase
/// <summary>Provides a flexible base class for new dictionary-like collections.</summary>
[Serializable]
public abstract class DictionaryBase<TKey, TValue> : IDictionary<TKey, TValue>
{
  /// <summary>Initializes a new, empty <see cref="DictionaryBase"/>.</summary>
  protected DictionaryBase() { }

  /// <summary>Initializes a new <see cref="DictionaryBase"/> containing the items from the given dictionary.</summary>
  protected DictionaryBase(IDictionary<TKey, TValue> initialItems)
  {
    items = new Dictionary<TKey, TValue>(initialItems);
  }

  /// <inheritdoc/>
  public TValue this[TKey key]
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

  /// <inheritdoc/>
  public int Count
  {
    get { return items == null ? 0 : items.Count; }
  }

  /// <inheritdoc/>
  public virtual bool IsReadOnly
  {
    get { return false; }
  }

  /// <inheritdoc/>
  public ICollection<TKey> Keys
  {
    get { return items == null ? (ICollection<TKey>)new TKey[0] : items.Keys; }
  }

  /// <inheritdoc/>
  public ICollection<TValue> Values
  {
    get { return items == null ? (ICollection<TValue>)new TValue[0] : items.Values; }
  }

  /// <inheritdoc/>
  public void Add(TKey key, TValue value)
  {
    AssertWritable();
    AddItem(key, value);
  }

  /// <inheritdoc/>
  public virtual void Clear()
  {
    AssertWritable();
    if(Count != 0) ClearItems();
  }

  /// <inheritdoc/>
  public bool ContainsKey(TKey key)
  {
    return items != null && items.ContainsKey(key);
  }

  /// <inheritdoc/>
  public bool Remove(TKey key)
  {
    AssertWritable();
    return items != null && RemoveItem(key);
  }

  /// <inheritdoc/>
  public bool TryGetValue(TKey key, out TValue value)
  {
    if(items == null)
    {
      value = default(TValue);
      return false;
    }
    else
    {
      return items.TryGetValue(key, out value);
    }
  }

  /// <summary>Gets the underlying, writeable dictionary containing the items.</summary>
  protected Dictionary<TKey, TValue> Items
  {
    get
    {
      if(items == null) items = new Dictionary<TKey, TValue>();
      return items;
    }
  }

  /// <summary>Called when an item is being added to the dictionary. The base implementation actually performs the addition.</summary>
  protected virtual void AddItem(TKey key, TValue value)
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
  protected virtual bool RemoveItem(TKey key)
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
  protected virtual void SetItem(TKey key, TValue value)
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

  Dictionary<TKey, TValue> items;

  #region ICollection<KeyValuePair<K,V>> Members
  void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
  {
    Add(item.Key, item.Value);
  }

  bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
  {
    if(item.Key == null) return false;
    TValue value;
    return TryGetValue(item.Key, out value) && object.Equals(value, item.Value);
  }

  void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
  {
    Utility.ValidateRange(array, arrayIndex, Count);
    if(items != null)
    {
      foreach(KeyValuePair<TKey, TValue> pair in items) array[arrayIndex++] = pair;
    }
  }

  bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
  {
    return ((ICollection<KeyValuePair<TKey, TValue>>)this).Contains(item) && Remove(item.Key);
  }
  #endregion

  #region IEnumerable<KeyValuePair<K,V>> Members
  /// <inheritdoc/>
  public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
  {
    return EnumerableExtensions.EmptyIfNull(items).GetEnumerator();
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
public abstract class ValidatedDictionary<TKey, TValue> : DictionaryBase<TKey, TValue>
{
  /// <summary>Initializes a new, empty <see cref="ValidatedDictionary{TKey,TValue}"/>.</summary>
  protected ValidatedDictionary() { }

  /// <summary>Initializes a new <see cref="ValidatedDictionary{TKey,TValue}"/> containing the given items. The items will be validated by
  /// calling <see cref="ValidateItem"/> as usual.
  /// </summary>
  protected ValidatedDictionary(IDictionary<TKey, TValue> initialItems)
  {
    if(initialItems == null) throw new ArgumentNullException();
    foreach(KeyValuePair<TKey, TValue> pair in initialItems)
    {
      ValidateItem(pair.Key, pair.Value);
      Items[pair.Key] = pair.Value;
    }
  }

  /// <summary>Calls <see cref="ValidateItem"/> to validate the item, and then adds it if it passes validation.</summary>
  protected override void AddItem(TKey key, TValue value)
  {
    ValidateItem(key, value);
    base.AddItem(key, value);
  }

  /// <summary>Calls <see cref="ValidateItem"/> to validate the item, and then adds it if it passes validation.</summary>
  protected override void SetItem(TKey key, TValue value)
  {
    ValidateItem(key, value);
    base.SetItem(key, value);
  }

  /// <summary>Called to validate items being added to the dictionary.</summary>
  protected abstract void ValidateItem(TKey key, TValue value);
}
#endregion

#region NonEmptyStringDictionary
/// <summary>Represents a dictionary of strings that validates the items being added to ensure that none are null or empty.</summary>
[Serializable]
public class NonEmptyStringDictionary : ValidatedDictionary<string, string>
{
  /// <summary>Initializes a new, empty <see cref="NonEmptyStringDictionary"/>.</summary>
  public NonEmptyStringDictionary() { }

  /// <inheritdoc/>
  protected override void ValidateItem(string key, string value)
  {
    if(string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)) throw new ArgumentException();
  }
}
#endregion

#region MultiValuedDictionaryBase
/// <summary>Provides a base class for implementing dictionaries that map a single key onto multiple items.</summary>
[Serializable]
public abstract class MultiValuedDictionaryBase<TKey, TValue, TValueCollection>
  : Dictionary<TKey, TValueCollection> where TValueCollection : ICollection<TValue>
{
  /// <summary>Initializes a new, empty <see cref="MultiValuedDictionaryBase{K,V,C}"/> that uses the default key comparer.</summary>
  protected MultiValuedDictionaryBase() { }
  /// <summary>Initializes a new, empty <see cref="MultiValuedDictionaryBase{K,V,C}"/> that uses the given key comparer.</summary>
  protected MultiValuedDictionaryBase(IEqualityComparer<TKey> keyComparer) : base(keyComparer) { }
  /// <summary>Deserializes a new <see cref="MultiValuedDictionaryBase{K,V,C}"/>.</summary>
  protected MultiValuedDictionaryBase(SerializationInfo info, StreamingContext context) : base(info, context) { }

  /// <summary>Adds a new item to the collection associated with the given key.</summary>
  public void Add(TKey key, TValue value)
  {
    TValueCollection list;
    if(!TryGetValue(key, out list)) this[key] = list = CreateCollection();
    list.Add(value);
  }

  /// <summary>Adds a series of values to the collection associated with the given key.</summary>
  public void AddRange(TKey key, IEnumerable<TValue> values)
  {
    if(values == null) throw new ArgumentNullException();
    TValueCollection list;
    if(!TryGetValue(key, out list)) this[key] = list = CreateCollection();
    AddRange(list, values);
  }

  /// <summary>Adds a value to a set of keys.</summary>
  public void AddRange(IEnumerable<TKey> keys, TValue value)
  {
    if(keys == null) throw new ArgumentNullException();
    foreach(TKey key in keys) Add(key, value);
  }

  /// <summary>Determines whether the collection associated with the given key contains the given item.</summary>
  public bool Contains(TKey key, TValue value)
  {
    TValueCollection list;
    return TryGetValue(key, out list) && list.Contains(value);
  }

  /// <summary>Returns an enumerable object that can be used to enumerate the items associated with the given key.</summary>
  public IEnumerable<TValue> Enumerate(TKey key)
  {
    TValueCollection list;
    return TryGetValue(key, out list) ? list : Enumerable.Empty<TValue>();
  }

  /// <summary>Attempts to remove an item from the collection associated with the given key. Returns true if the item was found
  /// and removed, and false if it was not found.
  /// </summary>
  public bool Remove(TKey key, TValue value)
  {
    TValueCollection list;
    return TryGetValue(key, out list) && list.Remove(value);
  }

  /// <summary>Adds a series of values to the given collection. The default collection iterates through the items one by one,
  /// but this method can be overriden if the collection supports a more efficient implementation.
  /// </summary>
  protected virtual void AddRange(TValueCollection collection, IEnumerable<TValue> items)
  {
    if(collection == null || items == null) throw new ArgumentNullException();
    foreach(TValue item in items) collection.Add(item);
  }

  /// <summary>Called to create a collection to store values associated with a key.</summary>
  protected abstract TValueCollection CreateCollection();
}
#endregion

#region MultiValuedDictionary
/// <summary>Represents a dictionary that associates a <see cref="List{T}"/> of items with each key. The list associated with a
/// key is capable of storing the same value multiple times.
/// </summary>
[Serializable]
public class MultiValuedDictionary<TKey, TValue> : MultiValuedDictionaryBase<TKey, TValue, List<TValue>>
{
  /// <summary>Initializes a new, empty <see cref="MultiValuedDictionary{K,V}"/> that uses the default key comparer.</summary>
  public MultiValuedDictionary() { }
  /// <summary>Initializes a new, empty <see cref="MultiValuedDictionary{K,V}"/> that uses the given key comparer.</summary>
  public MultiValuedDictionary(IEqualityComparer<TKey> keyComparer) : base(keyComparer) { }
  /// <summary>Deserializes a new <see cref="MultiValuedDictionary{K,V}"/>.</summary>
  protected MultiValuedDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

  /// <inheritdoc/>
  protected override void AddRange(List<TValue> collection, IEnumerable<TValue> items)
  {
    if(collection == null) throw new ArgumentNullException();
    collection.AddRange(items);
  }

  /// <inheritdoc/>
  protected override List<TValue> CreateCollection()
  {
    return new List<TValue>();
  }
}
#endregion

#region HashSetDictionary
/// <summary>Represents a dictionary that associates a <see cref="HashSet{T}"/> of items with each key. The set associated with
/// a key stores each item only once.
/// </summary>
[Serializable]
public class HashSetDictionary<TKey, TValue> : MultiValuedDictionaryBase<TKey, TValue, HashSet<TValue>>
{
  /// <summary>Initializes a new, empty <see cref="HashSetDictionary{K,V}"/> that uses the default key comparer.</summary>
  public HashSetDictionary() { }
  /// <summary>Initializes a new, empty <see cref="HashSetDictionary{K,V}"/> that uses the given key comparer.</summary>
  public HashSetDictionary(IEqualityComparer<TKey> keyComparer) : base(keyComparer) { }
  /// <summary>Initializes a new, empty <see cref="HashSetDictionary{K,V}"/> that uses the given key comparer and value comparer.</summary>
  public HashSetDictionary(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer) : base(keyComparer)
  {
    this.valueComparer = valueComparer;
  }

  /// <summary>Deserializes a new <see cref="HashSetDictionary{K,V}"/>.</summary>
  protected HashSetDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
  {
    if(info == null) throw new ArgumentNullException();
    valueComparer = (IEqualityComparer<TValue>)info.GetValue("valueComparer", typeof(IEqualityComparer<TValue>));
  }

  /// <inheritdoc/>
  [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
  public override void GetObjectData(SerializationInfo info, StreamingContext context)
  {
    if(info == null) throw new ArgumentNullException();
    base.GetObjectData(info, context);
    info.AddValue("valueComparer", valueComparer);
  }

  /// <inheritdoc/>
  protected override void AddRange(HashSet<TValue> collection, IEnumerable<TValue> items)
  {
    if(collection == null) throw new ArgumentNullException();
    collection.UnionWith(items);
  }

  /// <inheritdoc/>
  protected override HashSet<TValue> CreateCollection()
  {
    return new HashSet<TValue>(valueComparer);
  }

  readonly IEqualityComparer<TValue> valueComparer;
}
#endregion

} // namespace AdamMil.Collections
