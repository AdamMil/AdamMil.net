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

#region CollectionBase
/// <summary>Provides a flexible base class for new collections.</summary>
public abstract class CollectionBase<T> : IList<T>
{
	public CollectionBase()
	{
		Items = new List<T>();
	}

	public T this[int index]
	{
		get { return Items[index]; }
		set { SetItem(index, value); }
	}

	public int Count
	{
		get { return Items.Count; }
	}

	public virtual bool IsReadOnly
	{
		get { return false; }
	}

	public void Add(T item)
	{
		AssertNotReadOnly();
		InsertItem(Count, item);
	}

	public void AddRange(IEnumerable<T> items)
	{
		if(items == null) throw new ArgumentNullException();
		AssertNotReadOnly();
		
		// if we know how many items there are, use that knowledge to preallocate space within the collection
		ICollection<T> collection = items as ICollection<T>;
		if(collection != null)
		{
			int newCount = Items.Count + collection.Count, capacity = Items.Capacity;
			if(capacity < newCount)
			{
				do capacity *= 2; while(capacity < newCount);
				Items.Capacity = capacity;
			}
		}

		foreach(T item in items) Add(item);
	}

	public virtual void Clear()
	{
		AssertNotReadOnly();
		if(Items.Count != 0)
		{
			Items.Clear();
			OnCollectionChanged();
		}
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

	public void Insert(int index, T item)
	{
		if(index < 0 || index > Count) throw new ArgumentOutOfRangeException();
		AssertNotReadOnly();
		InsertItem(index, item);
	}

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

	public void RemoveAt(int index)
	{
		AssertNotReadOnly();
		RemoveItem(index, this[index]);
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

	/// <summary>Called when a new item is being inserted into the collection.
	/// The base implementation actually performs the insertion.
	/// </summary>
	protected virtual void InsertItem(int index, T item)
	{
		Items.Insert(index, item);
		OnCollectionChanged();
	}

	/// <summary>Called when an item is being removed from the collection. The base implementation actually performs the removal.</summary>
	protected virtual void RemoveItem(int index, T item)
	{
		Items.RemoveAt(index);
		OnCollectionChanged();
	}

	/// <summary>Called when an item in the collection is being assigned. The base implementation actually performs the assignment.</summary>
	protected virtual void SetItem(int index, T item)
	{
		Items[index] = item;
		OnCollectionChanged();
	}

	protected void AssertNotReadOnly()
	{
		if(IsReadOnly) throw new InvalidOperationException("The collection is read-only.");
	}

	/// <summary>Called when the collection may have been changed by the user.</summary>
	protected virtual void OnCollectionChanged()
	{
	}

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
#endregion

#region ValidatedCollection
/// <summary>Represents a collection that validates the items being added.</summary>
public abstract class ValidatedCollection<T> : CollectionBase<T>
{
	protected override void InsertItem(int index, T item)
	{
		ValidateItem(item, index);
		base.InsertItem(index, item);
	}

	protected override void SetItem(int index, T item)
	{
		ValidateItem(item, index);
		base.SetItem(index, item);
	}

	protected abstract void ValidateItem(T item, int index);
}
#endregion

#region NonNullCollection
/// <summary>Represents a collection that validates the items being added to ensure that none are null.</summary>
public class NonNullCollection<T> : ValidatedCollection<T> where T : class
{
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
	protected override void ValidateItem(string item, int index)
	{
		if(string.IsNullOrEmpty(item)) throw new ArgumentException();
	}
}
#endregion

} // namespace AdamMil.Collections
