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
using System.Runtime.Serialization;

namespace AdamMil.Collections
{

#region CycleException
/// <summary>An exception thrown when a cycle is detected during a topological sort.</summary>
[Serializable]
public class CycleException : ArgumentException
{
  /// <summary>Initializes a new <see cref="CycleException"/>.</summary>
  public CycleException() { }
  /// <summary>Initializes a new <see cref="CycleException"/>.</summary>
  public CycleException(object item)
    : this(item, "A cycle was detected involving item " + (item == null ? "NULL" : item.ToString())) { }
  /// <summary>Initializes a new <see cref="CycleException"/>.</summary>
  public CycleException(string message) : base(message) { }
  /// <summary>Initializes a new <see cref="CycleException"/>.</summary>
  public CycleException(object item, string message) : base(message)
  {
    ObjectInvolved = item;
  }
  /// <summary>Initializes a new <see cref="CycleException"/>.</summary>
  public CycleException(SerializationInfo info, StreamingContext context) : base(info, context) { }

  /// <summary>Gets an object involved in the cycle.</summary>
  public object ObjectInvolved { get; private set; }
}
#endregion

public static partial class CollectionExtensions
{
  /// <summary>Returns a new list containing the items in topologically sorted order, where each item comes after its
  /// dependencies. A <see cref="CycleException"/> will be thrown if a cycle is detected.
  /// </summary>
  public static List<T> GetTopologicalSort<T>(this IEnumerable<T> items, Converter<T, IEnumerable<T>> getDependencies)
  {
    if(items == null || getDependencies == null) throw new ArgumentNullException();

    ICollection<T> collection = items as ICollection<T>;
    int capacity = collection == null ? 16 : collection.Count;

    List<T> newList = new List<T>(capacity);
    Dictionary<T, bool> itemsProcessed = new Dictionary<T, bool>(capacity);
    foreach(T item in items) Visit(item, getDependencies, newList, itemsProcessed);
    return newList;
  }

  /// <summary>Enumerates the items in topologically sorted order, where each item comes after its dependencies. A
  /// <see cref="CycleException"/> will be thrown if a cycle is detected.
  /// </summary>
  /// <remarks>This method enumerates items lazily, but has reduced performance.</remarks>
  public static IEnumerable<T> TopologicalSort<T>(this IEnumerable<T> items, Converter<T,IEnumerable<T>> getDependencies)
  {
    if(items == null || getDependencies == null) throw new ArgumentNullException();

    ICollection<T> collection = items as ICollection<T>;
    int capacity = collection == null ? 16 : collection.Count;

    Dictionary<T, bool> itemsProcessed = new Dictionary<T, bool>(capacity);
    foreach(T item in items)
    {
      foreach(T orderedItem in Visit(item, getDependencies, itemsProcessed)) yield return orderedItem;
    }
  }

  /// <summary>Sorts the list items topologically, so that each item comes after its dependencies. A <see cref="CycleException"/>
  /// will be thrown if a cycle is detected.
  /// </summary>
  public static void TopologicalSort<T>(this IList<T> items, Converter<T, IEnumerable<T>> getDependencies)
  {
    // get the items in the right order
    List<T> newList = items.GetTopologicalSort(getDependencies);

    // now clear the list and add the ordered items
    items.Clear();
    List<T> list = items as List<T>;
    if(list != null)
    {
      list.AddRange(newList);
    }
    else
    {
      foreach(T item in newList) items.Add(item);
    }
  }

  /// <summary>Enumerates the items in topologically sorted sets, where no item in any set depends on any other item in the same
  /// set, and items come in a set after the set in which theihr dependencies are contained. A <see cref="CycleException"/> will
  /// be thrown if a cycle is detected.
  /// </summary>
  /// <remarks>The utility of this method is that all items in a set can be processed in parallel, as they have no dependencies
  /// on each other.
  /// </remarks>
  public static List<List<T>> TopologicalSortSets<T>(this IEnumerable<T> items, Converter<T, IEnumerable<T>> getDependencies)
  {
    if(items == null || getDependencies == null) throw new ArgumentNullException();

    ICollection<T> collection = items as ICollection<T>;
    int capacity = collection == null ? 16 : collection.Count;

    List<List<T>> sets = new List<List<T>>();
    Dictionary<T, int> itemHeights = new Dictionary<T, int>(capacity);
    foreach(T item in items) Visit(item, getDependencies, sets, itemHeights);
    return sets;
  }

  static IEnumerable<T> Visit<T>(T item, Converter<T, IEnumerable<T>> getDependencies, Dictionary<T, bool> itemsProcessed)
  {
    bool isProcessed;
    if(itemsProcessed.TryGetValue(item, out isProcessed))
    {
      if(isProcessed) yield break;
      else throw new CycleException(item);
    }

    itemsProcessed[item] = false;
    foreach(T dependency in getDependencies(item))
    {
      foreach(T orderedItem in Visit(dependency, getDependencies, itemsProcessed)) yield return orderedItem;
    }
    itemsProcessed[item] = true;

    yield return item;
  }

  static void Visit<T>(T item, Converter<T, IEnumerable<T>> getDependencies, List<T> orderedList,
                       Dictionary<T, bool> itemsProcessed)
  {
    bool isProcessed;
    if(itemsProcessed.TryGetValue(item, out isProcessed))
    {
      if(isProcessed) return;
      else throw new CycleException(item);
    }

    itemsProcessed[item] = false;
    foreach(T dependency in getDependencies(item)) Visit(dependency, getDependencies, orderedList, itemsProcessed);
    itemsProcessed[item] = true;

    orderedList.Add(item);
  }

  static int Visit<T>(T item, Converter<T, IEnumerable<T>> getDependencies, List<List<T>> sets,
                      Dictionary<T, int> itemHeights)
  {
    const int Processing = 0;

    int height;
    if(itemHeights.TryGetValue(item, out height))
    {
      if(height == Processing) throw new CycleException(item);
      else return height;
    }

    itemHeights[item] = Processing;
    // calculate the height of an item, which is the length of the longest chain of dependencies. items with no dependencies have
    // a height of one.
    height = 0;
    foreach(T dependency in getDependencies(item))
    {
      height = Math.Max(height, Visit(dependency, getDependencies, sets, itemHeights));
    }
    height++;
    itemHeights[item] = height;

    if(height > sets.Count) sets.Add(new List<T>());
    sets[height-1].Add(item);
    return height;
  }
}

} // namespace AdamMil.Collections