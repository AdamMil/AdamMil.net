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
using AdamMil.Utilities;

namespace AdamMil.Collections
{

/// <summary>Represents a circular list, which can also be used as a FIFO queue.</summary>
[Serializable]
public class CircularList<T> : IList<T>, IQueue<T>, IReadOnlyList<T>
{
  /// <summary>Initializes a new <see cref="CircularList{T}"/> with the default capacity and the ability to expand.</summary>
  public CircularList() : this(0, true) { }

  /// <summary>Initializes a new <see cref="CircularList{T}"/> with the given capacity and the ability to expand.</summary>
  public CircularList(int capacity) : this(capacity, true) { }

  /// <param name="capacity">The initial capacity. If zero, a default capacity will be used.</param>
  /// <param name="canGrow">Determines whether the list is allowed to enlarge beyond its initial capacity. Even if this
  /// is set to false, you can still manually enlarge the list by setting the <see cref="Capacity"/> property.
  /// </param>
  public CircularList(int capacity, bool canGrow)
  {
    if(capacity < 0) throw new ArgumentOutOfRangeException("capacity", capacity, "Capacity must not be negative");
    if(capacity == 0) capacity = 16;
    this.array     = new T[capacity];
    this.canGrow   = canGrow;
    this.mustClear = !typeof(T).UnderlyingSystemType.IsPrimitive; // primitive types don't need to be cleared
  }

  #region Enumerator
  /// <summary>An enumerator capable of enumerating the items in a <see cref="CircularList{T}"/>.</summary>
  /// <remarks>This structure is not meant to be instantiated directly. Rather, you should call <see cref="GetEnumerator"/>.</remarks>
  public struct Enumerator : IEnumerator<T>
  {
    internal Enumerator(CircularList<T> list)
    {
      this.list      = list;
      this.myVersion = list.version;
      this.index     = -1;
      this.item      = default(T);
    }

    /// <inheritdoc/>
    public T Current
    {
      get
      {
        if(index == -1 || index == list.count) throw new InvalidOperationException();
        return item;
      }
    }

    /// <inheritdoc/>
    public void Dispose() { }

    /// <inheritdoc/>
    public bool MoveNext()
    {
      AssertNotModified();
      if(index >= list.count-1)
      {
        index = list.count; // ensure that Current will throw if used again
        return false;
      }

      item = list[++index];
      return true;
    }

    /// <inheritdoc/>
    public void Reset()
    {
      AssertNotModified();
      index = -1;
      item  = default(T);
    }

    void AssertNotModified()
    {
      if(list.version != myVersion) throw new InvalidOperationException("The collection has been modified.");
    }

    object System.Collections.IEnumerator.Current
    {
      get { return Current; }
    }

    readonly CircularList<T> list;
    readonly int myVersion;
    int index;
    T item;
  }
  #endregion

  /// <summary>Gets the amount of available space in the list.</summary>
  public int AvailableSpace
  {
    get { return array.Length - count; }
  }

  /// <summary>Gets whether this list can grow beyond its <see cref="Capacity"/>.</summary>
  public bool CanGrow
  {
    get { return canGrow; }
  }

  /// <summary>Gets or sets the number of items the list can hold without resizing of the list.</summary>
  public int Capacity
  {
    get { return array.Length; }
    set { ResizeArray(value); }
  }

  /// <summary>Gets whether the list is full to capacity. (Although if <see cref="CanGrow"/> is true, the list can
  /// resize itself to hold more items).
  /// </summary>
  public bool IsFull
  {
    get { return count == array.Length; }
  }

  /// <summary>Adds a list of items to the end of the circular list.</summary>
  public void AddRange(params T[] items)
  {
    if(items == null) throw new ArgumentNullException();
    Insert(count, items, 0, items.Length);
  }

  /// <summary>Adds a list of items to the end of the circular list.</summary>
  public void AddRange(T[] items, int index, int count)
  {
    Insert(this.count, items, index, count);
  }

  /// <summary>Adds a list of items to the end of the circular list.</summary>
  public void AddRange(IEnumerable<T> items)
  {
    ICollection<T> collection = items as ICollection<T>;
    if(collection != null)
    {
      EnsureCapacity(count + collection.Count);

      int availableContiguousSpace = array.Length - (tail < head ? head : count);

      // if there's not enough contiguous space, but the collection is pretty big, we can make the space contiguous
      if(collection.Count > availableContiguousSpace && collection.Count >= 16)
      {
        MakeContiguous();
        if(tail != 0)
        {
          Array.Copy(array, tail, array, 0, count); // shift the data to the start of the array
          int unclearedItems = tail - collection.Count;
          if(mustClear && unclearedItems > 0) // if the new items wouldn't overwrite all the data left behind...
          {
            Array.Clear(array, head-unclearedItems, unclearedItems);
          }
          tail = 0;
          head = count;
        }
        availableContiguousSpace = array.Length - head;
      }

      if(collection.Count <= availableContiguousSpace)
      {
        collection.CopyTo(array, head);
        MoveHead(collection.Count);
        count += collection.Count;
        OnModified();
        return;
      }
    }

    if(items == null) throw new ArgumentNullException();
    foreach(T item in items) Add(item);
  }

  /// <summary>Inserts data into the list.</summary>
  /// <param name="destIndex">The index at which the data will be inserted.</param>
  /// <param name="items">An array containing the data to insert.</param>
  /// <param name="sourceIndex">The index into <paramref name="items"/> from which data will be read.</param>
  /// <param name="count">The number of items to copy.</param>
  public void Insert(int destIndex, T[] items, int sourceIndex, int count)
  {
    if(items == null) throw new ArgumentNullException();
    InsertSpace(destIndex, count);

    destIndex = GetRawIndex(destIndex);
    if(IsContiguousBlock(destIndex, count))
    {
      Array.Copy(items, sourceIndex, array, destIndex, count);
    }
    else
    {
      int toCopy = Math.Min(array.Length-destIndex, count);
      Array.Copy(items, sourceIndex, array, destIndex, toCopy);
      Array.Copy(items, sourceIndex+toCopy, array, 0, count-toCopy);
    }
  }

  /// <summary>Removes the first item from the circular list and returns it.</summary>
  public T RemoveFirst()
  {
    if(count == 0) throw new InvalidOperationException("The collection is empty.");
    T item = array[tail];
    array[MoveTail()] = default(T);
    count--;
    OnModified();
    return item;
  }

  /// <summary>Removes and discards the first <paramref name="count"/> items from the list.</summary>
  public void RemoveFirst(int count)
  {
    if((uint)count > (uint)this.count) throw new ArgumentOutOfRangeException();

    if(count == 1)
    {
      array[MoveTail()] = default(T);
    }
    else if(count == 0)
    {
      return; // avoid calling OnModified()
    }
    else
    {
      if(mustClear)
      {
        int toClear = Math.Min(count, array.Length-tail);
        Array.Clear(array, tail, toClear);
        Array.Clear(array, 0, count-toClear);
      }
      MoveTail(count);
    }

    this.count -= count;
    OnModified();
  }

  /// <summary>Removes the first <paramref name="count"/> items from the circular list and copies them to an array.</summary>
  /// <param name="array">The array to which the items should be copied.</param>
  /// <param name="index">The index into <paramref name="array"/> to which the items will be copied.</param>
  /// <param name="count">The number of items to copy.</param>
  public void RemoveFirst(T[] array, int index, int count)
  {
    CopyTo(0, array, index, count);
    RemoveFirst(count);
  }

  /// <summary>Removes the last item from the circular list and returns it.</summary>
  public T RemoveLast()
  {
    if(count == 0) throw new InvalidOperationException("The collection is empty.");
    if(--head < 0) head += array.Length;
    T item = array[head];
    array[head] = default(T);
    count--;
    OnModified();
    return item;
  }

  /// <summary>Removes and discards the last <paramref name="count"/> items from the list.</summary>
  public void RemoveLast(int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();

    if(count != 0)
    {
      int newHead = head - count;
      if(newHead < 0 && head != 0) // if the data to remove was split...
      {
        head = newHead + array.Length;
        if(mustClear)
        {
          Array.Clear(array, head, -newHead);
          Array.Clear(array, 0, count+newHead);
        }
      }
      else if(mustClear)
      {
        head = newHead;
        if(head < 0) head += array.Length;
        Array.Clear(array, head, count);
      }

      this.count -= count;
      OnModified();
    }
  }

  /// <summary>Removes the last <paramref name="count"/> items from the circular list and copies them to an array.</summary>
  /// <param name="array">The array to which the items should be copied.</param>
  /// <param name="index">The index into <paramref name="array"/> to which the items will be copied.</param>
  /// <param name="count">The number of items to copy.</param>
  public void RemoveLast(T[] array, int index, int count)
  {
    CopyTo(this.count-count, array, index, count);
    RemoveLast(count);
  }

  /// <summary>Removes a range of items from the list.</summary>
  /// <param name="index">The index of the first item to remove.</param>
  /// <param name="count">The number of items to remove.</param>
  /// <remarks>With the current implementation, unless <paramref name="count"/> equals 1, you can only remove data
  /// from the start or end of the list, so <paramref name="index"/> must be equal to zero or
  /// <see cref="Count"/>-<paramref name="count"/>.
  /// </remarks>
  public void RemoveRange(int index, int count)
  {
    if(index == 0) // if removing from the beginning
    {
      RemoveFirst(count);
    }
    else if(index == this.count-count) // if removing from the end
    {
      if(index < 0) throw new ArgumentOutOfRangeException();
      RemoveLast(count);
    }
    else if(count == 1) // we'll implement the special case of count == 1 so that RemoveAt(int) and Remove(T) work
    {
      int rawIndex = GetRawIndex(index);
      this.count--;

      if(head <= tail && rawIndex >= tail && head != 0) // this is the tricky case. the data is split and we're
      {                               // removing from the right block and we have to shift data from the left into it
        Array.Copy(array, rawIndex+1, array, rawIndex, array.Length-rawIndex-1);
        array[array.Length-1] = array[0];
        Array.Copy(array, 1, array, 0, --head);
      }
      else // the data's contiguous or the index is within the left block, so we can just shift the head data left.
      {    // TODO: this can be optimized by choosing the smallest chunk to shift.
        Array.Copy(array, rawIndex+1, array, rawIndex, this.count-index);
        if(--head < 0) head += array.Length;
      }
      array[head] = default(T);
      OnModified();
    }
    else
    {
      throw new ArgumentOutOfRangeException("Data can only be removed from the beginning or end of the list.");
    }
  }

  /// <summary>Returns an array containing all of the items in the list.</summary>
  public T[] ToArray()
  {
    T[] array = new T[Count];
    CopyTo(array, 0);
    return array;
  }

  /// <summary>Sets the capacity of the list to the number of items it currently contains.</summary>
  public void TrimExcess()
  {
    Capacity = count;
  }

  #region IList<T>
  /// <summary>Gets or sets the item at the given index.</summary>
  public T this[int index]
  {
    get { return array[GetRawIndex(index)]; }
    set
    {
      array[GetRawIndex(index)] = value;
      OnModified();
    }
  }

  /// <summary>Returns the index of the given item within the list.</summary>
  /// <param name="item">The item to search for.</param>
  /// <returns>The index at which the item is located, or -1 if the item is not in the list.</returns>
  public int IndexOf(T item)
  {
    return IndexOf(item, 0, count);
  }

  /// <summary>Returns the index of the given item within the list.</summary>
  /// <param name="item">The item to search for.</param>
  /// <param name="startIndex">The index into the list at which the search will begin.</param>
  /// <param name="count">The number of items to search.</param>
  /// <returns>The index at which the item is located, or -1 if the item is not in the list.</returns>
  public int IndexOf(T item, int startIndex, int count)
  {
    Utility.ValidateRange(this.count, startIndex, count);
    startIndex = GetRawIndex(startIndex);

    int index;
    if(IsContiguousBlock(startIndex, count))
    {
      index = Array.IndexOf(array, item, startIndex, count);
    }
    else
    {
      int toSearch = array.Length - startIndex;
      index = Array.IndexOf(array, item, startIndex, toSearch);
      if(index == -1) index = Array.IndexOf(array, item, 0, count-toSearch);
    }

    if(index != -1) index = GetLogicalIndex(index);
    return index;
  }

  /// <summary>Inserts an item into the list.</summary>
  /// <param name="index">The index at which the item should be inserted.</param>
  /// <param name="item">The item to insert.</param>
  public void Insert(int index, T item)
  {
    InsertSpace(index, 1);
    array[GetRawIndex(index)] = item;
  }

  /// <summary>Removes the item at the given index.</summary>
  /// <param name="index">The index of the item to remove.</param>
  public void RemoveAt(int index)
  {
    RemoveRange(index, 1);
  }
  #endregion

  #region ICollection<T>
  /// <summary>Gets the number of items in the list.</summary>
  public int Count
  {
    get { return count; }
  }

  /// <summary>Gets whether the list items cannot be changed.</summary>
  public bool IsReadOnly
  {
    get { return false; }
  }

  /// <summary>Adds an item to the end of the list.</summary>
  /// <param name="item">The item to add.</param>
  public void Add(T item)
  {
    EnsureCapacity(count+1);
    count++; // don't combine this with the above, because if EnsureCapacity() fails, we don't want 'count' to change
    array[MoveHead()] = item;
    OnModified();
  }

  /// <summary>Removes all items from the list.</summary>
  public void Clear()
  {
    if(count != 0)
    {
      if(mustClear)
      {
        Array.Clear(array, RightIndex, RightCount);
        Array.Clear(array, LeftIndex, LeftCount);
      }
      head = tail = count = 0;
      OnModified();
    }
  }

  /// <summary>Determines whether the list contains the given item.</summary>
  /// <param name="item">The item to search for.</param>
  /// <returns>True if the list contains the item and false if not.</returns>
  public bool Contains(T item)
  {
    return IndexOf(item) != -1;
  }

  /// <summary>Copies the entire list to an array.</summary>
  /// <param name="array">The array to which the items will be copied.</param>
  /// <param name="arrayIndex">The index into <paramref name="array"/> where the items will be copied.</param>
  public void CopyTo(T[] array, int arrayIndex)
  {
    CopyTo(0, array, arrayIndex, count);
  }

  /// <summary>Copies a portion of the list to an array.</summary>
  /// <param name="sourceIndex">The index within the list at which copying will begin.</param>
  /// <param name="array">The array to which the items will be copied.</param>
  /// <param name="destIndex">The index within <paramref name="array"/> where the items will be copied.</param>
  /// <param name="count">The number of items to copy.</param>
  public void CopyTo(int sourceIndex, T[] array, int destIndex, int count)
  {
    Utility.ValidateRange(array, destIndex, count);
    Utility.ValidateRange(this.count, sourceIndex, count);

    if(this.count != 0)
    {
      sourceIndex = GetRawIndex(sourceIndex);
      if(!IsContiguousBlock(sourceIndex, count)) // the source data is split, so copying starts from the right
      {
        int toCopy = this.array.Length - sourceIndex; // the number of bytes to copy from the first (tail) chunk
        // copy the right (tail) chunk and make sourceIndex point at the left
        Array.Copy(this.array, sourceIndex, array, destIndex, toCopy);
        sourceIndex = 0;
        count -= toCopy;
        destIndex += toCopy;
      }

      Array.Copy(this.array, sourceIndex, array, destIndex, count);
    }
  }

  /// <summary>Removes an item from the list.</summary>
  /// <param name="item">The item to remove.</param>
  /// <returns>True if the item was removed and false if was not found.</returns>
  public bool Remove(T item)
  {
    int index = IndexOf(item);
    if(index != -1)
    {
      RemoveAt(index);
      return true;
    }
    else
    {
      return false;
    }
  }
  #endregion

  /// <summary>Returns an object that will iterate over the items in the list.</summary>
  public Enumerator GetEnumerator()
  {
    return new Enumerator(this);
  }

  #region IEnumerable
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
  #endregion

  #region IEnumerable<T>
  IEnumerator<T> IEnumerable<T>.GetEnumerator()
  {
    return GetEnumerator();
  }
  #endregion

  #region IQueue<T> Members
  bool IQueue<T>.IsEmpty
  {
    get { return Count == 0; }
  }

  void IQueue<T>.Enqueue(T item)
  {
    Add(item);
  }

  T IQueue<T>.Dequeue()
  {
    return RemoveFirst();
  }

  T IQueue<T>.Peek()
  {
    return this[0];
  }

  bool IQueue<T>.TryDequeue(out T item)
  {
    if(Count == 0)
    {
      item = default(T);
      return false;
    }
    else
    {
      item = RemoveFirst();
      return true;
    }
  }
  #endregion

  /// <summary>Gets a reference to the internal array.</summary>
  protected T[] List
  {
    get { return array; }
  }

  /// <summary>Gets whether the data within the array is stored contiguously.</summary>
  protected bool IsContiguous
  {
    get { return tail < head || count == 0; }
  }

  /// <summary>Gets the index just past the end of the list, where the next item will be inserted.</summary>
  protected int Head
  {
    get { return head; }
  }

  /// <summary>Gets the index of the first item in the list.</summary>
  protected int Tail
  {
    get { return tail; }
  }

  /// <summary>Gets the number of items in the left chunk of data.</summary>
  /// <remarks>If the data is not contiguous, it will be split into two chunks. The chunk on the left side stores the
  /// latter portion of the list. If the data is contiguous, this property gets the total number of items in the list.
  /// </remarks>
  protected int LeftCount
  {
    get { return IsContiguous ? count : head; }
  }

  /// <summary>Gets the starting index of the left chunk of data.</summary>
  /// <remarks>If the data is not contiguous, it will be split into two chunks. The chunk on the left side stores the
  /// latter portion of the list and always starts from index 0. If the data is contiguous, this property gets the
  /// index where all of the data begins.
  /// </remarks>
  protected int LeftIndex
  {
    get { return IsContiguous ? tail : 0; }
  }

  /// <summary>Gets the number of items in the right chunk of data.</summary>
  /// <remarks>If the data is not contiguous, it will be split into two chunks. The chunk on the right side stores the
  /// first portion of the list. If the data is contiguous, this property returns 0.
  /// </remarks>
  protected int RightCount
  {
    get { return IsContiguous ? 0 : array.Length-tail; }
  }

  /// <summary>Gets the starting index of the right chunk of data.</summary>
  /// <remarks>If the data is not contiguous, it will be split into two chunks. The chunk on the right side stores the
  /// first portion of the list, and its index points to the first item. If the data is contiguous, this property
  /// returns 0.
  /// </remarks>
  protected int RightIndex
  {
    get { return IsContiguous ? 0 : tail; }
  }

  /// <summary>Ensures that the array is large enough to hold <paramref name="newCount"/> items.</summary>
  protected void EnsureCapacity(int newCount)
  {
    if(newCount > array.Length)
    {
      if(!canGrow) throw new InvalidOperationException("This operation would cause the list to exceed its capacity.");

      // make sure the new array is at least double the old, can hold at least 8 elements, and is a multiple of 8
      int newArraySize = (array.Length*2+7) & ~7;
      while(newArraySize < newCount) newArraySize *= 2;
      ResizeArray(newArraySize);
    }
    else if(count == 0) head = tail = 0; // whenever we're about to add items, write from index 0 if possible
  }

  /// <summary>Converts a raw array index into a logical index.</summary>
  /// <returns>The logical index is the index into the data -- into list of items.</returns>
  protected int GetLogicalIndex(int rawIndex)
  {
    if((uint)rawIndex >= (uint)array.Length) throw new ArgumentOutOfRangeException();

    if(rawIndex >= head || IsContiguous) rawIndex -= tail;
    else if(rawIndex < head) rawIndex += array.Length-tail;
    return rawIndex;
  }

  /// <summary>Converts a logical index into a raw array index.</summary>
  /// <remarks>
  /// The raw index is the physical array index corresponding to a given logical index -- an index into the data.
  /// </remarks>
  protected int GetRawIndex(int logicalIndex)
  {
    if((uint)logicalIndex >= (uint)count) throw new ArgumentOutOfRangeException();

    if(IsContiguous || logicalIndex < array.Length-tail) logicalIndex += tail;
    else logicalIndex -= array.Length-tail;
    return logicalIndex;
  }

  /// <summary>Inserts a chunk of free space. The space must be completely filled by the caller, as its contents are left undefined by this
  /// method.
  /// </summary>
  /// <param name="logicalIndex">The index at which to insert the space.</param>
  /// <param name="additionalItems">The number of items to reserve at that index.</param>
  protected void InsertSpace(int logicalIndex, int additionalItems)
  {
    if((uint)logicalIndex > (uint)count || additionalItems < 0) throw new ArgumentOutOfRangeException();

    if(additionalItems != 0)
    {
      EnsureCapacity(count + additionalItems);

      if(logicalIndex == count) // if the insertion is at the beginning or end, we can just move a pointer
      {
        MoveHead(additionalItems);
      }
      else if(logicalIndex == 0)
      {
        tail -= additionalItems;
        if(tail < 0) tail += array.Length;
      }
      else if(!IsContiguous) // otherwise, we have to shift data. if the data isn't contiguous, we have the data at the sides and a gap in
      {                      // the middle, so just find which side the index is on and move the data into the gap
        int tailLength = array.Length - tail, cmp = logicalIndex - tailLength;
        if(cmp > 0 || cmp == 0 && tailLength < head) // if we're shifting data from the left side into the gap...
        {
          Array.Copy(array, cmp, array, cmp+additionalItems, head-cmp);
          MoveHead(additionalItems);
        }
        else // otherwise, we're shifting data from the right side into the gap
        {
          Array.Copy(array, tail, array, tail-additionalItems, logicalIndex);
          tail -= additionalItems;
        }
      }
      else if(logicalIndex <= count/2) // the data is contiguous, so we need to shift either the data at/before or after the index.
      {                                // if it's in the left half...
        if(tail >= additionalItems) // we'll shift that data left. if we don't need to split the data...
        {
          Array.Copy(array, tail, array, tail-additionalItems, logicalIndex);
          tail -= additionalItems;
        }
        else
        {
          // _ |a b +2 c d e f| _ _ _ -> b _ _ c d e f| _ _ |a
          // _ |a b +3 c d e f| _ _ _ -> _ _ c d e f| _ _ |a b
          // _ |a b +4 c d e f| _ _ _ -> _ _ c d e f| _ |a b _
          int rightSize = additionalItems - tail, newTail = array.Length - rightSize;
          if(logicalIndex <= rightSize)
          {
            Array.Copy(array, tail, array, newTail, logicalIndex);
          }
          else
          {
            Array.Copy(array, tail, array, newTail, rightSize);
            Array.Copy(array, additionalItems, array, 0, logicalIndex-rightSize);
          }
          tail = newTail;
        }
      }
      else // shift the data after the index to the right
      {
        int rawIndex = tail + logicalIndex, rightSpace = array.Length - head;
        if(rightSpace >= additionalItems) // if we don't need to split the data...
        {
          Array.Copy(array, rawIndex, array, rawIndex+additionalItems, head-rawIndex);
          MoveHead(additionalItems);
        }
        else
        {
          // _ _ _ |a b c d +4 e f| _ -> _ e f| |a b c d _ _ _  (leftSize=3, leftBehind=1)
          // _ _ _ |a b c d +3 e f| _ -> e f| _ |a b c d _ _ _  (leftSize=2, leftBehind=0)
          // _ _ _ |a b c d +2 e f| _ -> f| _ _ |a b c d _ _ e  (leftSize=1, leftBehind=-1)
          int leftBehind = array.Length - rawIndex - additionalItems;
          if(leftBehind <= 0)
          {
            Array.Copy(array, rawIndex, array, -leftBehind, count-logicalIndex);
          }
          else
          {
            Array.Copy(array, rawIndex+leftBehind, array, 0, count-logicalIndex-leftBehind);
            Array.Copy(array, rawIndex, array, array.Length-leftBehind, leftBehind);
          }
          head = additionalItems - rightSpace;
        }
      }

      count += additionalItems;
      OnModified();
    }
  }

  /// <summary>Determines whether the given span of the array is contiguous, or whether it would wrap around.</summary>
  /// <param name="rawIndex">The starting index of the span.</param>
  /// <param name="count">The number of items in the span.</param>
  /// <returns>True if the span is contiguous within the array, and false if it would need to wrap around.</returns>
  protected bool IsContiguousBlock(int rawIndex, int count)
  {
    return rawIndex < head || rawIndex+count <= array.Length;
  }

  /// <summary>Rearranges the data in the array to make it contiguous.</summary>
  protected void MakeContiguous()
  {
    if(!IsContiguous)
    {
      if(AvailableSpace >= Math.Max(LeftCount, RightCount)) // if there's enough free space that the biggest chunk can
      {                                                     // fit in it, then we don't need a temporary array
        Array.Copy(array, 0, array, array.Length-tail, head); // copy the left (head) part to its final location
        Array.Copy(array, tail, array, 0, array.Length-tail); // copy the right (tail) part to its final location
      }
      else if(LeftCount < RightCount)
      {
        T[] temp = new T[LeftCount];
        Array.Copy(array, temp, temp.Length);
        Array.Copy(array, tail, array, 0, RightCount);
        Array.Copy(temp, 0, array, RightCount, temp.Length);
      }
      else
      {
        T[] temp = new T[RightCount];
        Array.Copy(array, tail, temp, 0, temp.Length);
        Array.Copy(array, 0, array, RightCount, LeftCount);
        Array.Copy(temp, array, temp.Length);
      }

      if(mustClear)
      {
        int clearIndex = Math.Max(count, tail);
        Array.Clear(array, clearIndex, array.Length-clearIndex);
      }

      tail = 0;
      head = count;
    }
  }

  /// <summary>Get the current value of <see cref="Head"/>, and then increases it by one, wrapping it around if necessary.</summary>
  int MoveHead()
  {
    int index = head++;
    if(head == array.Length) head = 0;
    return index;
  }

  /// <summary>Advances <see cref="Head"/> by the given number of items (assumed to be &lt;= array.Length), wrapping it around if
  /// necessary.
  /// </summary>
  void MoveHead(int count)
  {
    head += count;
    if(head >= array.Length) head -= array.Length;
  }

  /// <summary>Get the current value of <see cref="Tail"/>, and then increases it by one, wrapping it around if necessary.</summary>
  int MoveTail()
  {
    int index = tail++;
    if(tail == array.Length) tail = 0;
    return index;
  }

  /// <summary>Advances <see cref="Tail"/> by the given number of items (assumed to be &lt;= array.Length), wrapping it around if
  /// necessary.
  /// </summary>
  void MoveTail(int count)
  {
    tail += count;
    if(tail >= array.Length) tail -= array.Length;
  }

  /// <summary>This method must be called when a logical element of the list is added, removed, or changed.</summary>
  void OnModified()
  {
    version++;
  }

  /// <summary>Resizes the array to the given capacity.</summary>
  void ResizeArray(int capacity)
  {
    if(capacity != array.Length)
    {
      if(capacity < this.count) throw new ArgumentOutOfRangeException("Capacity cannot be less than Count.");
      T[] newArray = new T[capacity];
      CopyTo(newArray, 0);
      array = newArray;
      tail  = 0;
      head  = count;
    }
  }

  /// <summary>The array containing the list items.</summary>
  T[] array;
  int head, tail, count;
  /// <summary>The version number of the list, used by enumerators to see if the list has changed.</summary>
  int version;
  /// <summary>Whether the list contains a data type that must be cleared out from the array.</summary>
  readonly bool mustClear;
  /// <summary>Whether the list can automatically increase its capacity when necessary.</summary>
  readonly bool canGrow;
}

} // namespace AdamMil.Collections