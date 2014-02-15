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

#region IQueue
/// <summary>An interface representing a queue of items.</summary>
public interface IQueue<T> : ICollection<T>
{
  /// <summary>Gets whether the collection is empty. This may be more efficient than comparing <see cref="ICollection{T}.Count"/>
  /// to zero.
  /// </summary>
  bool IsEmpty { get; }
  /// <summary>Adds an item to the queue.</summary>
  void Enqueue(T item);
  /// <summary>Returns and removes the first item from the queue.</summary>
  T Dequeue();
  /// <summary>Returns the first item in the queue.</summary>
  T Peek();
  /// <include file="documentation.xml" path="//Queue/TryDequeue/node()"/>
  bool TryDequeue(out T item);
}
#endregion

#region IReadOnlyCollection
/// <summary>An interface representing a collection that does not support being changed and does not necessarily have
/// a particular ordering.
/// </summary>
public interface IReadOnlyCollection<T> : IEnumerable<T>, System.Collections.IEnumerable
{
  /// <summary>Gets the number of items in the collection.</summary>
  int Count { get; }
  /// <summary>Determines whether the collection contains the given item.</summary>
  bool Contains(T item);
  /// <summary>Copies all of the items from the collection to the given array, starting from the given location.</summary>
  void CopyTo(T[] array, int arrayIndex);
  /// <summary>Copies all of the items from the collection to a new array and returns it.</summary>
  T[] ToArray();
}
#endregion

#region IReadOnlyList
/// <summary>An interface representing a collection that does not support being changed, but has a particular ordering
/// and allows random access to items.
/// </summary>
public interface IReadOnlyList<T> : IReadOnlyCollection<T>
{
  /// <summary>Retrieves the item at the given index.</summary>
  /// <param name="index">The index of the item, from 0 to <see cref="IReadOnlyCollection{T}.Count"/>-1.</param>
  T this[int index] { get; }
  /// <summary>Retrieves the index of the first item of the collection that is equal to the given value, or -1 if
  /// the item could not be found.
  /// </summary>
  int IndexOf(T item);
}
#endregion

#region IReadOnlyDictionary
/// <summary>An interface representing a dictionary that does not support being changed.</summary>
public interface IReadOnlyDictionary<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>>
{
  /// <summary>Gets the value corresponding to the given key.</summary>
  TValue this[TKey key] { get; }
  /// <summary>Gets a collection containing the keys in this dictionary.</summary>
  IReadOnlyCollection<TKey> Keys { get; }
  /// <summary>Gets a collection containing the values in this dictionary.</summary>
  IReadOnlyCollection<TValue> Values { get; }
  /// <summary>Determines whether the dictionary contains the given key.</summary>
  bool ContainsKey(TKey key);
  /// <summary>Attempts to retrieve the value for the given key. If the value could be retrieved, it is placed in
  /// <paramref name="value"/> and true is return. Otherwise, <paramref name="value"/> is set to the default value for
  /// its type and false is returned.
  /// </summary>
  bool TryGetValue(TKey key, out TValue value);
}
#endregion

} // namespace AdamMil.Collections