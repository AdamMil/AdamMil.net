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

// TODO: complement this with an AccessLimitedSet<T>

#region SetBase
/// <summary>Provides a flexible base class for sets.</summary>
[Serializable]
public abstract class SetBase<T> : ICollection<T>
{
  /// <summary>Initializes a new <see cref="SetBase{T}"/>.</summary>
  protected SetBase() : this(null) { }

  /// <summary>Initializes a new <see cref="SetBase{T}"/> containing the given items (if <paramref name="items"/> is not null).</summary>
  protected SetBase(IEnumerable<T> items)
  {
    Items = items == null ? new HashSet<T>() : new HashSet<T>(items);
  }

  /// <summary>Gets the number of items in the set.</summary>
  public int Count
  {
    get { return Items.Count; }
  }

  /// <summary>Gets whether the set is read-only.</summary>
  public virtual bool IsReadOnly
  {
    get { return false; }
  }

  /// <summary>Adds an item to the set if it's not already a member.</summary>
  /// <returns>Returns true if the item was added or false if it already existed in the set.</returns>
  public bool Add(T item)
  {
    AssertNotReadOnly();
    return AddItem(item);
  }

  /// <summary>Removes all items from the set.</summary>
  public void Clear()
  {
    AssertNotReadOnly();
    if(Items.Count != 0) ClearItems();
  }

  /// <summary>Determines whether the set contains the given item.</summary>
  public bool Contains(T item)
  {
    return Items.Contains(item);
  }

  /// <summary>Copies the items from the set into an array.</summary>
  /// <param name="array">The array into which the items will be copied.</param>
  /// <param name="arrayIndex">The index at which the items will be written.</param>
  public void CopyTo(T[] array, int arrayIndex)
  {
    Items.CopyTo(array, arrayIndex);
  }

  /// <include file="documentation.xml" path="/Collections/Common/GetEnumerator/node()"/>
  public HashSet<T>.Enumerator GetEnumerator()
  {
    return Items.GetEnumerator();
  }

  /// <summary>Removes an item from the set.</summary>
  /// <returns>Returns true if the item was removed or false if it didn't exist in the set.</returns>
  public bool Remove(T item)
  {
    AssertNotReadOnly();
    return RemoveItem(item);
  }

  /// <summary>Converts the items in the set into an array.</summary>
  public T[] ToArray()
  {
    T[] array = new T[Count];
    CopyTo(array, 0);
    return array;
  }

  /// <summary>Removes the given items from the set.</summary>
  public void ExceptWith(IEnumerable<T> itemsToRemove)
  {
    if(itemsToRemove == null) throw new ArgumentNullException();
    AssertNotReadOnly();
    foreach(T item in itemsToRemove) RemoveItem(item);
  }

  /// <summary>Removes the given items from the set.</summary>
  public void ExceptWith(params T[] items)
  {
    ExceptWith((IEnumerable<T>)items);
  }

  /// <summary>Removes items from the set that are not among the given items.</summary>
  public void IntersectWith(IEnumerable<T> items)
  {
    if(items == null) throw new ArgumentNullException();
    AssertNotReadOnly();

    if(Items.Count != 0)
    {
      HashSet<T> set = items as HashSet<T>;
      if(set == null)
      {
        SetBase<T> setBase = items as SetBase<T>;
        set = setBase == null ? new HashSet<T>(items) : setBase.Items;
      }

      List<T> deadItems = null;
      foreach(T item in Items)
      {
        if(!set.Contains(item))
        {
          if(deadItems == null) deadItems = new List<T>();
          deadItems.Add(item);
        }
      }

      if(deadItems != null)
      {
        foreach(T item in deadItems) RemoveItem(item);
      }
    }
  }

  /// <summary>Removes items from the set that are not among the given items.</summary>
  public void IntersectWith(params T[] items)
  {
    IntersectWith((IEnumerable<T>)items);
  }

  /// <summary>Adds the given items to the set.</summary>
  public void UnionWith(IEnumerable<T> itemsToAdd)
  {
    if(itemsToAdd == null) throw new ArgumentNullException();
    AssertNotReadOnly();
    foreach(T item in itemsToAdd) AddItem(item);
  }

  /// <summary>Adds the given items to the set.</summary>
  public void UnionWith(params T[] items)
  {
    UnionWith((IEnumerable<T>)items);
  }

  /// <summary>Gets the underlying <see cref="HashSet{T}"/> containing the items in the collection.</summary>
  protected HashSet<T> Items
  {
    get; private set;
  }

  /// <summary>Called when the collection is being cleared. The base implementation actually performs the insertion.</summary>
  protected virtual void ClearItems()
  {
    Items.Clear();
    OnCollectionChanged();
  }

  /// <summary>Called when a new item is being inserted into the collection.
  /// The base implementation actually performs the insertion.
  /// </summary>
  protected virtual bool AddItem(T item)
  {
    bool added = Items.Add(item);
    if(added) OnCollectionChanged();
    return added;
  }

  /// <summary>Called when an item is being removed from the collection. The base implementation actually performs the removal.</summary>
  protected virtual bool RemoveItem(T item)
  {
    bool removed = Items.Remove(item);
    if(removed) OnCollectionChanged();
    return removed;
  }

  /// <summary>Throws an exception if the set is read-only.</summary>
  protected void AssertNotReadOnly()
  {
    if(IsReadOnly) throw new InvalidOperationException("The collection is read-only.");
  }

  /// <summary>Called when the collection has been changed by the user.</summary>
  protected virtual void OnCollectionChanged()
  {
  }

  void ICollection<T>.Add(T item)
  {
    Add(item);
  }

  IEnumerator<T> IEnumerable<T>.GetEnumerator()
  {
    return GetEnumerator();
  }

  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
}
#endregion

#region NonEmptyStringSet
/// <summary>Implements a set that contains non-empty strings.</summary>
[Serializable]
public class NonEmptyStringSet : SetBase<string>
{
  /// <summary>Initializes a new <see cref="NonEmptyStringSet"/>.</summary>
  public NonEmptyStringSet() { }

  /// <summary>Initializes a new <see cref="NonEmptyStringSet"/> containing the given items.</summary>
  public NonEmptyStringSet(IEnumerable<string> items) : base(items) { }

  /// <inheritdoc/>
  protected override bool AddItem(string item)
  {
    if(item == null) throw new ArgumentNullException();
    if(item.Length == 0) throw new ArgumentException("Members cannot be empty.");
    return base.AddItem(item);
  }
}
#endregion

} // namespace AdamMil.Collections
