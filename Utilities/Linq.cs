/*
AdamMil.Utilities is a library providing generally useful utilities for
.NET development.

http://www.adammil.net/
Copyright (C) 2010-2013 Adam Milazzo

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
using System.Linq;
using AdamMil.Collections;

namespace AdamMil.Utilities
{

/// <summary>Provides additional LINQ extensions for <see cref="IEnumerable{T}"/>.</summary>
public static class EnumerableExtensions
{
  /// <summary>Concatenates two sequences. If either sequence is null, it is treated as though it was empty.</summary>
  public static IEnumerable<T> Coalesce<T>(this IEnumerable<T> first, IEnumerable<T> second)
  {
    return first == null ? (second != null ? second : Enumerable.Empty<T>()) :
           second == null ? first : first.Concat(second);
  }

  /// <summary>Concatenates three sequences. If any sequence is null, it is treated as though it was empty.</summary>
  public static IEnumerable<T> Coalesce<T>(IEnumerable<T> first, IEnumerable<T> second, IEnumerable<T> third)
  {
    return first.Coalesce(second).Coalesce(third);
  }

  /// <summary>Concatenates an arbitrary number of sequences. If any sequence is null, it is treated as though it was empty.</summary>
  public static IEnumerable<T> Coalesce<T>(params IEnumerable<T>[] sequences)
  {
    if(sequences == null) throw new ArgumentNullException();
    return CoalesceCore(sequences); // put the generator in its own function so we can do argument validation immediately
  }

  /// <summary>Appends a single item to a sequence.</summary>
  public static IEnumerable<T> Concat<T>(this IEnumerable<T> sequence, T value)
  {
    if(sequence == null) throw new ArgumentNullException();
    return ConcatCore(sequence, value);
  }

  /// <summary>Returns the given sequence if it is not null, and an empty sequence otherwise.</summary>
  public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> sequence)
  {
    return sequence == null ? Enumerable.Empty<T>() : sequence;
  }

  /// <summary>Returns the maximum <see cref="DateTime"/> value from a sequence.</summary>
  /// <exception cref="InvalidOperationException">Thrown if <paramref name="items"/> is empty.</exception>
  public static DateTime Max<T>(this IEnumerable<T> items, Func<T, DateTime> dateSelector)
  {
    if(items == null || dateSelector == null) throw new ArgumentNullException();
    DateTime max = DateTime.MinValue;
    bool hadElement = false;

    foreach(T item in items)
    {
      DateTime time = dateSelector(item);
      if(time > max) max = time;
      hadElement = true;
    }

    if(!hadElement) throw new InvalidOperationException();
    return max;
  }

  /// <summary>Returns the maximum <see cref="DateTime"/> value from a sequence, or null if the sequence is empty or contains only
  /// null values.
  /// </summary>
  public static DateTime? Max<T>(this IEnumerable<T> items, Func<T, DateTime?> dateSelector)
  {
    if(items == null || dateSelector == null) throw new ArgumentNullException();

    DateTime? max = null;
    foreach(T item in items)
    {
      DateTime? time = dateSelector(item);
      if(time.HasValue && (!max.HasValue || time.Value > max.Value)) max = time;
    }
    return max;
  }

  /// <summary>Walks through two sequences that are sorted in the same order and executes an action for each element that belongs to both
  /// sequences, using the default <see cref="IComparer{T}"/> to compare them.
  /// </summary>
  public static void MergeJoin<T>(this IEnumerable<T> sortedLeft, IEnumerable<T> sortedRight, Action<T> onInnerMatch)
  {
    MergeJoin(sortedLeft, sortedRight, onInnerMatch, null, null, null);
  }

  /// <summary>Walks through two sequences that are sorted in the same order and executes an action for each element that belongs to both
  /// sequences, using the given <see cref="IComparer{T}"/> to compare them.
  /// </summary>
  public static void MergeJoin<T>(this IEnumerable<T> sortedLeft, IEnumerable<T> sortedRight, Action<T> onInnerMatch, IComparer<T> comparer)
  {
    MergeJoin(sortedLeft, sortedRight, onInnerMatch, null, null, comparer);
  }

  /// <summary>Walks through two sequences that are sorted in the same order and executes one of three actions for each element, depending
  /// on whether it belongs to both sequences, or only the left or right sequence, using the default <see cref="IComparer{T}"/> to compare
  /// them.
  /// </summary>
  public static void MergeJoin<T>(this IEnumerable<T> sortedLeft, IEnumerable<T> sortedRight, Action<T> onInnerMatch,
                                  Action<T> onOuterLeft, Action<T> onOuterRight)
  {
    MergeJoin(sortedLeft, sortedRight, onInnerMatch, onOuterLeft, onOuterRight, null);
  }

  /// <summary>Walks through two sequences that are sorted in the same order and executes one of three actions for each element, depending
  /// on whether it belongs to both sequences, or only the left or right sequence, using the given <see cref="IComparer{T}"/> to compare
  /// them.
  /// </summary>
  public static void MergeJoin<T>(this IEnumerable<T> sortedLeft, IEnumerable<T> sortedRight, Action<T> onInnerMatch,
                                  Action<T> onOuterLeft, Action<T> onOuterRight, IComparer<T> comparer)
  {
    if(sortedLeft == null || sortedRight == null) throw new ArgumentNullException();
    if(onInnerMatch == null && onOuterLeft == null && onOuterRight == null) return;
    if(comparer == null) comparer = Comparer<T>.Default;

    IEnumerator<T> left = null, right = null;
    try
    {
      left = sortedLeft.GetEnumerator();
      right = sortedRight.GetEnumerator();
      bool leftHasValue = left.MoveNext(), rightHasValue = right.MoveNext();
      while(leftHasValue & rightHasValue) // if neither sequence is at the end...
      {
        int cmp = comparer.Compare(left.Current, right.Current);
        if(cmp < 0) // if the left value is less than the right value, the left is an outer value
        {
          if(onOuterLeft != null) onOuterLeft(left.Current);
          leftHasValue = left.MoveNext();
        }
        else if(cmp > 0) // if the right value is less, the right is an outer value
        {
          if(onOuterRight != null) onOuterRight(right.Current);
          rightHasValue = right.MoveNext();
        }
        else // otherwise, the values are equal, so it's an inner match
        {
          if(onInnerMatch != null) onInnerMatch(left.Current);
          leftHasValue  = left.MoveNext();
          rightHasValue = right.MoveNext();
        }
      }

      // now go through the remaining values from the remaining sequence, if any
      if(leftHasValue && onOuterLeft != null)
      {
        do onOuterLeft(left.Current); while(left.MoveNext());
      }
      else if(rightHasValue && onOuterRight != null)
      {
        do onOuterRight(right.Current); while(right.MoveNext());
      }
    }
    finally
    {
      Utility.Dispose(left);
      Utility.Dispose(right);
    }
  }

  /// <summary>Returns the minimum <see cref="DateTime"/> value from a sequence.</summary>
  /// <exception cref="InvalidOperationException">Thrown if <paramref name="items"/> is empty.</exception>
  public static DateTime Min<T>(this IEnumerable<T> items, Func<T,DateTime> dateSelector)
  {
    if(items == null || dateSelector == null) throw new ArgumentNullException();
    DateTime min = DateTime.MaxValue;
    bool hadElement = false;

    foreach(T item in items)
    {
      DateTime time = dateSelector(item);
      if(time < min) min = time;
      hadElement = true;
    }

    if(!hadElement) throw new InvalidOperationException();
    return min;
  }

  /// <summary>Returns the minimum <see cref="DateTime"/> value from a sequence, or null if the sequence is empty or contains only
  /// null values.
  /// </summary>
  public static DateTime? Min<T>(this IEnumerable<T> items, Func<T, DateTime?> dateSelector)
  {
    if(items == null || dateSelector == null) throw new ArgumentNullException();

    DateTime? min = null;
    foreach(T item in items)
    {
      DateTime? time = dateSelector(item);
      if(time.HasValue && (!min.HasValue || time.Value < min.Value)) min = time;
    }
    return min;
  }

  /// <summary>Returns the items in ascending order.</summary>
  public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> items)
  {
    return items.OrderBy(Identity);
  }

  /// <summary>Returns the items in ascending order, compared using the given <see cref="IComparer{T}"/>.</summary>
  public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> items, IComparer<T> comparer)
  {
    return items.OrderBy(Identity, comparer);
  }

  /// <summary>Returns the items in ascending order, compared using the given <see cref="Comparison{T}"/>.</summary>
  public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> items, Comparison<T> comparison)
  {
    return items.OrderBy(Identity, new DelegateComparer<T>(comparison));
  }

  /// <summary>Returns the items in ascending order, with their keys compared using the given <see cref="Comparison{T}"/>.</summary>
  public static IOrderedEnumerable<T> OrderBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> keySelector,
                                                       Comparison<TKey> comparison)
  {
    return items.OrderBy(keySelector, new DelegateComparer<TKey>(comparison));
  }

  /// <summary>Returns the items in descending order, with their keys compared using the given <see cref="Comparison{T}"/>.</summary>
  public static IOrderedEnumerable<T> OrderByDescending<T, TKey>(this IEnumerable<T> items, Func<T, TKey> keySelector,
                                                                 Comparison<TKey> comparison)
  {
    return items.OrderByDescending(keySelector, new DelegateComparer<TKey>(comparison));
  }

  /// <summary>Returns the items in descending order.</summary>
  public static IOrderedEnumerable<T> OrderDescending<T>(this IEnumerable<T> items)
  {
    return items.OrderByDescending(Identity);
  }

  /// <summary>Returns the items in descending order, compared using the given <see cref="IComparer{T}"/>.</summary>
  public static IOrderedEnumerable<T> OrderDescending<T>(this IEnumerable<T> items, IComparer<T> comparer)
  {
    return items.OrderByDescending(Identity, comparer);
  }

  /// <summary>Returns the items in descending order, compared using the given <see cref="Comparison{T}"/>.</summary>
  public static IOrderedEnumerable<T> OrderDescending<T>(this IEnumerable<T> items, Comparison<T> comparison)
  {
    return items.OrderByDescending(Identity, new DelegateComparer<T>(comparison));
  }

  /// <summary>Returns up to the specified number of items, all of which are greater than or equal to the rest of the items. The
  /// items may not be returned in sorted order.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Linq/TakeGreatest/*"/>
  public static IEnumerable<T> TakeGreatest<T>(this IEnumerable<T> items, int count)
  {
    return items.TakeGreatest(count, Comparer<T>.Default);
  }

  /// <summary>Returns up to the specified number of items, all of which are greater than or equal to the rest of the items (using
  /// the given <see cref="Comparison{T}"/> to compare them). The items may not be returned in sorted order.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Linq/TakeGreatest/*"/>
  public static IEnumerable<T> TakeGreatest<T>(this IEnumerable<T> items, int count, Comparison<T> comparison)
  {
    return items.TakeGreatest(count, new DelegateComparer<T>(comparison));
  }

  /// <summary>Returns up to the specified number of items, all of which are greater than or equal to the rest of the items (using
  /// the given <see cref="IComparer{T}"/> to compare them). The items may not be returned in sorted order.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Linq/TakeGreatest/*"/>
  public static IEnumerable<T> TakeGreatest<T>(this IEnumerable<T> items, int count, IComparer<T> comparer)
  {
    return items.TakeLeast(count, new ReversedComparer<T>(comparer));
  }

  /// <summary>Returns up to the specified number of items, all of which are greater than or equal to the rest of the items (using
  /// the given key selector function to compare them). The items may not be returned in sorted order.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Linq/TakeGreatest/node()"/>
  public static IEnumerable<T> TakeGreatest<T, TKey>(this IEnumerable<T> items, int count, Func<T, TKey> keySelector)
  {
    return items.TakeGreatest(count, MakeKeyComparer(keySelector));
  }

  /// <summary>Returns up to the specified number of items, all of which are less than or equal to the rest of the items. The
  /// items may not be returned in sorted order.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Linq/TakeLeast/*"/>
  public static IEnumerable<T> TakeLeast<T>(this IEnumerable<T> items, int count)
  {
    return items.TakeLeast(count, Comparer<T>.Default);
  }

  /// <summary>Returns up to the specified number of items, all of which are less than or equal to the rest of the items (using
  /// the given <see cref="Comparison{T}"/> to compare them). The items may not be returned in sorted order.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Linq/TakeLeast/*"/>
  public static IEnumerable<T> TakeLeast<T>(this IEnumerable<T> items, int count, Comparison<T> comparison)
  {
    return items.TakeLeast(count, new DelegateComparer<T>(comparison));
  }

  /// <summary>Returns up to the specified number of items, all of which are less than or equal to the rest of the items (using
  /// the given key selector function to compare them). The items may not be returned in sorted order.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Linq/TakeLeast/node()"/>
  public static IEnumerable<T> TakeLeast<T, TKey>(this IEnumerable<T> items, int count, Func<T, TKey> keySelector)
  {
    return items.TakeLeast(count, MakeKeyComparer(keySelector));
  }

  /// <summary>Returns up to the specified number of items, all of which are less than or equal to the rest of the items (using
  /// the given <see cref="IComparer{T}"/> to compare them). The items may not be returned in sorted order.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Linq/TakeLeast/*"/>
  public static IEnumerable<T> TakeLeast<T>(this IEnumerable<T> items, int count, IComparer<T> comparer)
  {
    if(items == null) throw new ArgumentNullException();
    if(count < 0) throw new ArgumentOutOfRangeException();
    else if(count == 0) return Enumerable.Empty<T>();

    // if all of the items are desired, just return them as-is
    ICollection<T> collection = items as ICollection<T>;
    if(collection != null && count >= collection.Count) return items;

    T[] array = items.ToArray();
    if(count >= array.Length) return items;

    // SelectLeast assumes that count < array.Length, which we've guaranteed above
    SelectLeast(array, comparer ?? Comparer<T>.Default, count);
    return new ArraySegmentEnumerable<T>(array, 0, count);
  }

  static IEnumerable<T> CoalesceCore<T>(IEnumerable<T>[] sequences)
  {
    foreach(IEnumerable<T> sequence in sequences)
    {
      if(sequence != null)
      {
        foreach(T value in sequence) yield return value;
      }
    }
  }

  static IEnumerable<T> ConcatCore<T>(IEnumerable<T> sequence, T value)
  {
    foreach(T item in sequence) yield return item;
    yield return value;
  }

  static T Identity<T>(T value)
  {
    return value;
  }

  static IComparer<T> MakeKeyComparer<T, TKey>(Func<T, TKey> keySelector)
  {
    if(keySelector == null) throw new ArgumentNullException();
    return new DelegateComparer<T>((a, b) => Comparer<TKey>.Default.Compare(keySelector(a), keySelector(b)));
  }

  /// <summary>Partially sorts the array such that the least <paramref name="desiredCount"/> items are moved to the front,
  /// but not necessarily in sorted order.
  /// </summary>
  static void SelectLeast<T>(T[] items, IComparer<T> comparer, int desiredCount)
  {
    int start = 0, count = items.Length;

    while(true)
    {
      // at this point, it is assumed that desiredCount < count

      // if the segment is very small, then use selection sort to find the smallest N items (where N = desiredCount)
      if(count < 7) // TODO: it would be wise to tune this value based on experimental evidence
      {
        for(int i=start, iend=start+desiredCount, jend=start+count; i<iend; i++) // for the first N elements...
        {
          int min = i;
          for(int j=i+1; j<jend; j++) // find the smallest remaining element
          {
            if(comparer.Compare(items[j], items[min]) < 0) min = j;
          }
          if(i != min) Utility.Swap(ref items[i], ref items[min]); // and move it towards the front
        }
        break; // after that, we're done
      }
      else // otherwise, use a partial quick-sort
      {
        // first, partition the items and find the pivot point
        int pivotPoint=start, endInc=start+count-1;
        Utility.Swap(ref items[start+count/2], ref items[endInc]); // move the pivot element to the end temporarily
        T pivot = items[endInc]; // grab a copy of the pivot element (we know it's at the end because we just put it there)
        for(int i=start; i<endInc; i++) // then, for each item except the pivot element...
        {
          if(comparer.Compare(items[i], pivot) < 0) // if it's less than the pivot element...
          {
            if(i != pivotPoint) Utility.Swap(ref items[i], ref items[pivotPoint]); // move it towards the beginning of the array
            pivotPoint++; // and keep track of where the pivot point would be
          }
        }
        Utility.Swap(ref items[pivotPoint], ref items[endInc]); // move the pivot element into its final position

        // now, we have all the items less than the pivot element to the left of the pivot point
        int leftCount = pivotPoint - start; // get the number of items less than the pivot element

        if(leftCount > desiredCount) // if there are more than the desired number of items on the left side, then the left side
        {                            // contains the ones we want, but we need to sort them further to find them.
          count = leftCount; // the items in the right partition can be ignored because we have enough on the left side
        }
        else // otherwise, the number of items on the left is less than or equal to the number of items we want, so we know that
        {    // they all belong to the set of items we want (i.e. the least N items), and we don't need to sort them further
          desiredCount -= leftCount;
          if(desiredCount <= 0) break; // if there are no more items to find, we're done
          start  = pivotPoint; // otherwise, look for the remaining items at and to the right of the pivot point
          count -= leftCount;
        }
      }
    }
  }

  /// <summary>Returns a <see cref="HashSet{T}"/> containing the items from the given sequence.</summary>
  public static HashSet<T> ToSet<T>(this IEnumerable<T> items)
  {
    if(items == null) throw new ArgumentNullException();
    return new HashSet<T>(items);
  }
 
  /// <summary>Filters the given sequence to remove null values.</summary>
  public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> items) where T : class
  {
    return items.Where(item => item != null);
  }
   
  /// <summary>Filters the given sequence to remove null values.</summary>
  public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> items) where T : struct
  {
    return items.Where(item => item.HasValue).Select(item => item.GetValueOrDefault()); // .GetValueOrDefault() is faster than .Value
  }
}

} // namespace AdamMil.Utilities
