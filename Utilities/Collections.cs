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

/// <summary>Implements useful extensions for .NET built-in collections.</summary>
public static partial class CollectionExtensions
{
  /// <summary>Adds a list of items to an <see cref="ICollection{T}"/>.</summary>
  public static void AddRange<T>(this ICollection<T> collection, params T[] items)
  {
    AddRange(collection, (IEnumerable<T>)items);
  }

  /// <summary>Adds a list of items to an <see cref="ICollection{T}"/>.</summary>
  public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
  {
    if(collection == null) throw new ArgumentNullException();

    if(items != null)
    {
      List<T> builtInList = collection as List<T>;
      if(builtInList != null)
      {
        builtInList.AddRange(items);
      }
      else
      {
        foreach(T item in items) collection.Add(item);
      }
    }
  }

  /// <summary>Adds a set of key/value pairs to a dictionary. An exception will be thrown if a key already exists in the
  /// dictionary.
  /// </summary>
  public static void AddRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<KeyValuePair<K, V>> pairs)
  {
    if(dictionary == null || pairs == null) throw new ArgumentNullException();
    foreach(KeyValuePair<K, V> pair in pairs) dictionary.Add(pair.Key, pair.Value);
  }

  /// <summary>Adds a list of items to a <see cref="List{T}"/>.</summary>
  public static void AddRange<T>(this List<T> list, params T[] items)
  {
    if(list == null) throw new ArgumentNullException();
    if(items != null) list.AddRange((IEnumerable<T>)items);
  }

  /// <summary>Adds a list of items to an <see cref="IList{T}"/>.</summary>
  public static void AddRange<T>(this IList<T> list, IList<T> items)
  {
    if(list == null) throw new ArgumentNullException();

    if(items != null)
    {
      List<T> builtInList = list as List<T>;
      if(builtInList != null)
      {
        builtInList.AddRange(items);
      }
      else
      {
        foreach(T item in items) list.Add(item);
      }
    }
  }

  /// <summary>Returns a new <see cref="HashSet{T}"/> containing the members of <paramref name="set"/> that don't exist in
  /// <paramref name="excluded"/>.
  /// </summary>
  public static HashSet<T> Except<T>(this HashSet<T> set, HashSet<T> excluded)
  {
    if(set == null || excluded == null) throw new ArgumentNullException();
    HashSet<T> result = new HashSet<T>();
    foreach(T item in set)
    {
      if(!excluded.Contains(item)) result.Add(item);
    }
    return result;
  }

  /// <summary>Returns a new <see cref="HashSet{T}"/> containing the members of <paramref name="set1"/> that also exist in
  /// <paramref name="set2"/>.
  /// </summary>
  public static HashSet<T> Intersection<T>(this HashSet<T> set1, HashSet<T> set2)
  {
    if(set1 == null || set2 == null) throw new ArgumentNullException();
    HashSet<T> result = new HashSet<T>();
    foreach(T item in set1)
    {
      if(set2.Contains(item)) result.Add(item);
    }
    return result;
  }

  /// <summary>Returns the last item in the array.</summary>
  public static T Last<T>(this T[] array)
  {
    if(array == null) throw new ArgumentNullException();
    if(array.Length == 0) throw new ArgumentException("The collection is empty.");
    return array[array.Length-1];
  }

  /// <summary>Removes duplicates from the list. Note that this method has O(dn+n) complexity (where d is the number of
  /// duplicates) and is unsuitable for lists containing large numbers of items and duplicates.
  /// </summary>
  public static void RemoveDuplicates<T>(this IList<T> list)
  {
    list.RemoveDuplicates(EqualityComparer<T>.Default);
  }

  /// <summary>Removes duplicates from the list. Note that this method has O(dn+n) complexity (where d is the number of
  /// duplicates) and is unsuitable for lists containing large numbers of items and duplicates.
  /// </summary>
  public static void RemoveDuplicates<T>(this IList<T> list, IEqualityComparer<T> comparer)
  {
    if(list == null) throw new ArgumentNullException();
    HashSet<T> itemsSeen = new HashSet<T>(comparer);
    for(int i=list.Count-1; i >= 0; i--)
    {
      T item = list[i];
      if(!itemsSeen.Add(item)) list.RemoveAt(i);
    }
  }

  /// <summary>Removes duplicates from the list. Note that this method has O(2n^2) complexity and is unsuitable for lists
  /// containing large numbers of items.
  /// </summary>
  public static void RemoveDuplicates<T>(this IList<T> list, Comparison<T> comparer)
  {
    if(comparer == null) throw new ArgumentNullException();
    list.RemoveDuplicates((a, b) => comparer(a, b) == 0);
  }

  /// <summary>Removes duplicates from the list. Note that this method has O(2n^2) complexity and is unsuitable for lists
  /// containing large numbers of items.
  /// </summary>
  public static void RemoveDuplicates<T>(this IList<T> list, Func<T, T, bool> equalityComparer)
  {
    if(list == null || equalityComparer == null) throw new ArgumentNullException();

    for(int i=list.Count-1; i >= 1; i--)
    {
      T item = list[i];
      for(int j=i-1; j >= 0; j--)
      {
        if(equalityComparer(item, list[j]))
        {
          list.RemoveAt(i);
          break;
        }
      }
    }
  }

  /// <summary>Removes a set of keys from a dictionary.</summary>
  public static void RemoveRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<K> keysToRemove)
  {
    if(dictionary == null || keysToRemove == null) throw new ArgumentNullException();
    foreach(K key in keysToRemove) dictionary.Remove(key);
  }

  /// <summary>Adds a set of key/value pairs to a dictionary. If a key already exists in the dictionary, the value will be
  /// overwritten.
  /// </summary>
  public static void SetRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<KeyValuePair<K, V>> pairs)
  {
    if(dictionary == null || pairs == null) throw new ArgumentNullException();
    foreach(KeyValuePair<K, V> pair in pairs) dictionary[pair.Key] = pair.Value;
  }

  /// <summary>Returns a random item from the list.</summary>
  public static T SelectRandom<T>(this IList<T> list, Random random)
  {
    if(list == null || random == null) throw new ArgumentNullException();
    if(list.Count == 0) throw new ArgumentException("The collection is empty.");
    return list[random.Next(list.Count)];
  }

  /// <summary>Returns the value associated with the given key, or null if the given value does not exist in the dictionary. Note that null
  /// will also be returned if the value does exist in the dictionary, but the value is null.
  /// </summary>
  public static V TryGetValue<K, V>(this IDictionary<K, V> dictionary, K key) where V : class
  {
    if(dictionary == null) throw new ArgumentNullException();
    V value;
    dictionary.TryGetValue(key, out value);
    return value;
  }

  /// <summary>Returns a new <see cref="HashSet{T}"/> containing the members that exist in <paramref name="set1"/> or
  /// <paramref name="set2"/>.
  /// </summary>
  public static HashSet<T> Union<T>(this HashSet<T> set1, HashSet<T> set2)
  {
    if(set1 == null || set2 == null) throw new ArgumentNullException();
    HashSet<T> result = new HashSet<T>(set1);
    result.UnionWith(set2);
    return result;
  }
}

} // namespace AdamMil.Collections
