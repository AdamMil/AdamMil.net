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
using System.Collections;
using System.Collections.Generic;

namespace AdamMil.Collections
{

#region CollectionBase
/// <summary>Provides a flexible base class for new collections.</summary>
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
  public virtual void Clear()
  {
    AssertNotReadOnly();
    if(Items.Count != 0) ClearItems();
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
    if(index < 0 || index > Count) throw new ArgumentOutOfRangeException();
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
public class NonNullCollection<T> : ValidatedCollection<T> where T : class
{
  /// <summary>Initializes a new <see cref="NonNullCollection{T}"/>.</summary>
  protected NonNullCollection() { }
  /// <summary>Initializes a new <see cref="NonNullCollection{T}"/> with the given list of items.</summary>
  protected NonNullCollection(IEnumerable<T> items) : base(items) { }

  /// <include file="documentation.xml" path="//ValidatedCollection/ValidateItem/*"/>
  protected override void ValidateItem(T item, int index)
  {
    if(item == null) throw new ArgumentNullException();
  }
}
#endregion

#region NonEmptyStringCollection
/// <summary>Represents a collection of strings that validates the items being added to ensure that none are null or empty.</summary>
public class NonEmptyStringCollection : ValidatedCollection<string>
{
  /// <include file="documentation.xml" path="//ValidatedCollection/ValidateItem/*"/>
  protected override void ValidateItem(string item, int index)
  {
    if(string.IsNullOrEmpty(item)) throw new ArgumentException();
  }
}
#endregion

} // namespace AdamMil.Collections
