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

namespace AdamMil.Utilities
{

#region ArrayBuffer
/// <summary>Implements a thin wrapper around an array to provide a set of methods useful for implementing buffers.</summary>
/// <remarks>The buffer stores its data within a region of the <see cref="Buffer"/> array. The region begins at
/// <see cref="Offset"/> and extends for <see cref="Count"/> elements. The data may be shifted within the array by methods that
/// modify the buffer. You can use <see cref="GetLogicalIndex"/> and <see cref="GetRawIndex"/> to convert between logical indices
/// (i.e. indices within the data) and raw indices (indices within the underlying array) if necessary.
/// </remarks>
public class ArrayBuffer<T> : ICollection<T>
{
  /// <summary>Initializes a new <see cref="ArrayBuffer{T}"/> with the default capacity.</summary>
  public ArrayBuffer() : this(0) { }

  /// <summary>Initializes a new <see cref="ArrayBuffer{T}"/> with the given capacity.</summary>
  /// <param name="capacity">The initial capacity of the buffer. If equal to zero, default capacity will be used.</param>
  public ArrayBuffer(int capacity)
  {
    if(capacity < 0) throw new ArgumentOutOfRangeException();
    if(capacity == 0) capacity = DefaultCapacity;
    _buffer = new T[capacity];
  }

  /// <summary>Gets or sets the element at the given index. The index should be a valid logical index
  /// (from 0 to <see cref="Count"/>-1), just as with other collections, although for performance this may not be verified.
  /// </summary>
  public T this[int logicalIndex]
  {
    get
    {
      System.Diagnostics.Debug.Assert(logicalIndex >= 0 && logicalIndex < Count);
      return Buffer[Offset + logicalIndex]; 
    }
    set
    {
      System.Diagnostics.Debug.Assert(logicalIndex >= 0 && logicalIndex < Count);
      Buffer[Offset + logicalIndex] = value;
    }
  }

  /// <summary>Gets the buffer containing the data. The data is stored in a range starting from <see cref="Offset"/> and extending
  /// <see cref="Count"/> elements.
  /// </summary>
  public T[] Buffer
  {
    get { return _buffer; }
  }

  /// <summary>Gets or sets the capacity of the underlying buffer. If set to zero, the default capacity will be used.</summary>
  public int Capacity
  {
    get { return Buffer.Length; }
    set
    {
      if(value < Count) throw new ArgumentOutOfRangeException();
      if(value == 0) value = DefaultCapacity;
      if(value != Capacity)
      {
        T[] newBuffer = new T[value];
        Array.Copy(Buffer, Offset, newBuffer, 0, Count);
        _buffer = newBuffer;
        Offset  = 0;
        PinData = false;
      }
    }
  }

  /// <summary>Gets the number of items in the buffer.</summary>
  public int Count
  {
    get; private set;
  }

  /// <summary>Gets the end of the value range of items within the underlying array. This is the index at which data should be written, and
  /// is equal to <see cref="Offset"/> + <see cref="Count"/>.
  /// </summary>
  public int End
  {
    get { return Offset + Count; }
  }

  /// <summary>Gets whether the buffer is full (i.e. whether <see cref="Count"/> equals <see cref="Capacity"/>).</summary>
  public bool IsFull
  {
    get { return Count == Capacity; }
  }

  /// <summary>Gets the offset into the underlying array where the data is stored. This is the index from which data should be read.</summary>
  public int Offset
  {
    get; private set;
  }

  /// <summary>If false, data within the buffer may be moved automatically by various methods of this class, for instance to make room for
  /// new data. If true, data within the buffer will not be moved, and a new buffer array will be allocated whenever data would otherwise
  /// be moved within the buffer. This property will be automatically reset to false whenever the buffer is reallocated, since the newly
  /// allocated array has not been exposed to other code yet. It may be useful to set this to true if you perform buffer operations while
  /// data is being read from it asynchronously, and reset it to false when the asynchronous read is complete, since asynchronous reads
  /// usually require the data to remain in place until the operation is complete.
  /// </summary>
  public bool PinData { get; set; }

  /// <summary>Gets the amount of free space between the end of the data and the end of the array. This is equal to <see cref="Capacity"/>
  /// - <see cref="End"/>. This is the amount of contiguous free space available for writing, which may be less than the total amount of
  /// free space.
  /// </summary>
  public int SpaceAtEnd
  {
    get { return Buffer.Length - End; }
  }

  /// <summary>Adds the given item to the buffer. This method may expand the array or shift the data within it.</summary>
  public void Add(T item)
  {
    GetArrayForWriting(1)[Count++] = item;
  }

  /// <summary>Updates the <see cref="Count"/> property to add the given number of items, assuming you've already added them
  /// to the array yourself.
  /// </summary>
  public void AddCount(int count)
  {
    if(count < 0 || End + count > Capacity) throw new ArgumentOutOfRangeException();
    Count += count;
  }

  /// <summary>Adds all the items from the given collection to the buffer.
  /// This method may expand the array or shift the data within it.
  /// </summary>
  public void AddRange(ICollection<T> data)
  {
    if(data == null) throw new ArgumentNullException();
    T[] destination = GetArrayForWriting(data.Count);
    data.CopyTo(destination, End);
    Count += data.Count;
  }

  /// <summary>Adds the items from the given region of the given array to the buffer.
  /// This method may expand the array or shift the data within it.
  /// </summary>
  public void AddRange(T[] data, int index, int count)
  {
    Utility.ValidateRange(data, index, count);
    T[] destination = GetArrayForWriting(count);
    Array.Copy(data, index, destination, End, count);
    Count += count;
  }

  /// <summary>Clears the buffer. Note that this does not actually erase the underlying array. It only sets the
  /// <see cref="Count"/> and <see cref="Offset"/> to zero.
  /// </summary>
  public void Clear()
  {
    Count = Offset = 0;
  }

  /// <summary>Gets whether the buffer contains the given item.</summary>
  public bool Contains(T item)
  {
    return Array.IndexOf(Buffer, item, Offset, Count) != -1;
  }

  /// <summary>Returns an enumerator that iterates over the items in the buffer. It is not valid to modify the buffer while the
  /// enumerator is in use.
  /// </summary>
  public ArraySegmentEnumerator<T> GetEnumerator()
  {
    return new ArraySegmentEnumerator<T>(Buffer, Offset, Count);
  }

  /// <summary>Copies all items from the buffer to the given array.</summary>
  public void CopyTo(T[] array, int arrayIndex)
  {
    CopyTo(array, arrayIndex, Count);
  }

  /// <summary>Copies the given number of items from the buffer to the given array.</summary>
  public void CopyTo(T[] array, int arrayIndex, int count)
  {
    Utility.ValidateRange(array, arrayIndex, count);
    if(count > Count) throw new ArgumentOutOfRangeException();
    Array.Copy(Buffer, Offset, array, arrayIndex, count);
  }

  /// <summary>Copies the given portion of the buffer (represented by the logical source index and count) to the given array.</summary>
  public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int count)
  {
    Utility.ValidateRange(Count, sourceIndex, count);
    Utility.ValidateRange(destination, destinationIndex, count);
    Array.Copy(Buffer, Offset+sourceIndex, destination, destinationIndex, count);
  }

  /// <summary>Ensures that the underlying array is large enough to hold the given number of items.</summary>
  public void EnsureCapacity(int capacity)
  {
    if(capacity < 0) throw new ArgumentOutOfRangeException();
    if(Capacity < capacity) Capacity = capacity;
  }

  /// <summary>Returns a reference to the buffer after ensuring that there is enough space at the end to hold the given number of
  /// additional items. This method may enlarge the buffer or shift data within it to ensure that there is sufficient space
  /// starting from <see cref="End"/> (which the method may also change) to hold the given number of new items. This method does
  /// not assume the items will be added and so does not update <see cref="Count"/>. You must call <see cref="AddCount"/> later if
  /// you add items to the array.
  /// </summary>
  /// <param name="elementsToAdd">The number of items that are expected to be added to the array.</param>
  public T[] GetArrayForWriting(int elementsToAdd)
  {
    int spaceAtEnd = SpaceAtEnd;
    if(elementsToAdd > spaceAtEnd)
    {
      if(elementsToAdd <= spaceAtEnd + Offset && !PinData) // if we can gain the needed space by shifting data over, do that
      {
        ShiftToFront();
      }
      else // otherwise, we need to enlarge the buffer
      {
        T[] newBuffer = new T[Math.Max(Capacity*2, Count + elementsToAdd)];
        if(Count != 0) Array.Copy(_buffer, Offset, newBuffer, 0, Count);
        _buffer = newBuffer;
        Offset  = 0;
        PinData = false;
      }
    }
    return Buffer;
  }

  /// <summary>Ensures that all data is shifted to the beginning of the underlying array, so that <see cref="End"/> equals
  /// <see cref="Count"/> and <see cref="Offset"/> equals zero, and then returns a reference to <see cref="Buffer"/>.
  /// <see cref="Buffer"/> may be reallocated if <see cref="PinData"/> is true.
  /// </summary>
  public T[] GetZeroOffsetArray()
  {
    if(!PinData)
    {
      ShiftToFront();
    }
    else if(Offset != 0 && Count != 0)
    {
      T[] newBuffer = new T[Buffer.Length];
      Array.Copy(Buffer, Offset, newBuffer, 0, Count);
      _buffer = newBuffer;
      Offset  = 0;
      PinData = false;
    }
    return Buffer;
  }

  /// <summary>Converts a raw index (an index within the underlying array) to a logical index (an index from 0 to
  /// <see cref="Count"/>). An exception will be thrown if the raw index does not point to an item within the buffer.
  /// </summary>
  public int GetLogicalIndex(int rawIndex)
  {
    if(rawIndex < Offset || rawIndex > End) throw new ArgumentOutOfRangeException();
    return rawIndex - Offset;
  }

  /// <summary>Converts a logical index (an index from 0 to <see cref="Count"/>) to a raw index (an index within the underlying
  /// array).
  /// </summary>
  public int GetRawIndex(int logicalIndex)
  {
    if((uint)logicalIndex > (uint)Count) throw new ArgumentOutOfRangeException();
    return logicalIndex + Offset;
  }

  /// <summary>Returns the logical index of the first instance of the given item within the buffer, or -1 if the item cannot be
  /// found.
  /// </summary>
  public int IndexOf(T item)
  {
    int index = Array.IndexOf(Buffer, item, Offset, Count);
    return index == -1 ? index : index - Offset;
  }

  /// <summary>Returns the logical index of the the given item within the buffer, or -1 if the item cannot be found. The search is
  /// begun at the given logical starting index.
  /// </summary>
  public int IndexOf(T item, int start)
  {
    int index = Array.IndexOf(Buffer, item, GetRawIndex(start), Count-start);
    return index == -1 ? index : index - Offset;
  }

  /// <summary>Returns the logical index of the the given item within the buffer, or -1 if the item cannot be found. The search is
  /// begun at the given logical starting index, and searches up to <paramref name="count"/> items at most.
  /// </summary>
  public int IndexOf(T item, int start, int count)
  {
    Utility.ValidateRange(Count, start, count);
    int index = Array.IndexOf(Buffer, item, GetRawIndex(start), count);
    return index == -1 ? index : index - Offset;
  }

  /// <summary>Removes the first item from the buffer and returns it.</summary>
  public T Remove()
  {
    if(Count == 0) throw new InvalidOperationException();
    T item = Buffer[Offset++];
    if(--Count == 0) Offset = 0;
    return item;
  }

  /// <summary>Removes the given number of items from the buffer and places them in the given array.</summary>
  public void Remove(T[] array, int index, int count)
  {
    CopyTo(array, index, count);
    Remove(count);
  }

  /// <summary>Removes the given number of items from the buffer. This does not alter the array, but only adjusts the
  /// <see cref="Count"/> and <see cref="Offset"/>.
  /// </summary>
  public void Remove(int count)
  {
    if((uint)count > (uint)Count) throw new ArgumentOutOfRangeException();
    Count -= count;
    Offset = Count == 0 ? 0 : Offset + count;
  }

  /// <summary>Sets the count to the given number of items. The items are still assumed to begin from <see cref="Offset"/>. This
  /// method is useful if you add or remove items yourself.
  /// </summary>
  public void SetCount(int count)
  {
    if(count < 0 || count+Offset > Capacity) throw new ArgumentOutOfRangeException();
    Count = count;
    if(count == 0) Offset = 0;
  }

  /// <summary>Sets the region of the underlying array that contains valid items. This method is useful if you add or remove
  /// items yourself.
  /// </summary>
  public void SetRange(int offset, int count)
  {
    Utility.ValidateRange(Buffer, offset, count);
    Offset = offset;
    Count  = count;
  }

  const int DefaultCapacity = 32;

  void ShiftToFront()
  {
    if(Offset != 0 && Count != 0)
    {
      Array.Copy(Buffer, Offset, Buffer, 0, Count);
      Offset = 0;
    }
  }

  bool ICollection<T>.IsReadOnly
  {
    get { return false; }
  }

  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

  IEnumerator<T> IEnumerable<T>.GetEnumerator()
  {
    return GetEnumerator();
  }

  bool ICollection<T>.Remove(T item)
  {
    throw new NotSupportedException();
  }

  T[] _buffer;
}
#endregion

#region ArrayExtensions
/// <summary>Provides convenient extensions for working with arrays.</summary>
public static class ArrayExtensions
{
  /// <summary>Clears an array.</summary>
  public static void Clear(this Array array)
  {
    if(array == null) throw new ArgumentNullException();
    Array.Clear(array, 0, array.Length);
  }

  /// <summary>Concatenates two arrays and returns a new array containing the combined contents. If both arrays are null, the result
  /// will be null.
  /// </summary>
  /// <remarks>This method allocates a new array even if the combined content would equal one of the source arrays. If you want to avoid
  /// unnecessary allocation in this case, use <see cref="ArrayUtility.Concat{T}(bool,T[],T[])"/> and pass false for the
  /// <c>alwaysAllocate</c> parameter.
  /// </remarks>
  public static T[] Concat<T>(this T[] a, T[] b)
  {
    return ArrayUtility.Concat(true, a, b);
  }

  /// <summary>Determines whether a specified item exists within an array.</summary>
  public static bool Contains(this Array array, object item)
  {
    return Array.IndexOf(array, item) != -1;
  }

  /// <summary>Determines whether a specified item exists within an array, searching from a given index.</summary>
  public static bool Contains(this Array array, object item, int startIndex)
  {
    return Array.IndexOf(array, item, startIndex) != -1;
  }

  /// <summary>Determines whether a specified item exists within an array.</summary>
  public static bool Contains<T>(this T[] array, T item)
  {
    return Array.IndexOf<T>(array, item) != -1;
  }

  /// <summary>Determines whether a specified item exists within an array, searching from a given index.</summary>
  public static bool Contains<T>(this T[] array, T item, int startIndex)
  {
    return Array.IndexOf<T>(array, item, startIndex) != -1;
  }

  /// <summary>Returns the index of the first element of an arrey equal to the given item, or -1 if the item cannot be found.</summary>
  public static int IndexOf(this Array array, object item)
  {
    return Array.IndexOf(array, item);
  }

  /// <summary>Returns the index of the first element of an arrey equal to the given item, searching from a given index, or -1
  /// if the item cannot be found.
  /// </summary>
  public static int IndexOf(this Array array, object item, int startIndex)
  {
    return Array.IndexOf(array, item, startIndex);
  }

  /// <summary>Returns the index of the first element of an arrey equal to the given item, or -1 if the item cannot be found.</summary>
  public static int IndexOf<T>(this T[] array, T item)
  {
    return Array.IndexOf<T>(array, item);
  }

  /// <summary>Returns the index of the first element of an arrey equal to the given item, searching from a given index, or -1
  /// if the item cannot be found.
  /// </summary>
  public static int IndexOf<T>(this T[] array, T item, int startIndex)
  {
    return Array.IndexOf<T>(array, item, startIndex);
  }

  /// <summary>Returns an array segment containing the entire array.</summary>
  public static ArraySegment<T> Segment<T>(this T[] array)
  {
    return new ArraySegment<T>(array);
  }

  /// <summary>Returns an array segment containing the remainder of the given array, starting at the given index.</summary>
  public static ArraySegment<T> Segment<T>(this T[] array, int index)
  {
    if(array == null) throw new ArgumentNullException();
    return new ArraySegment<T>(array, index, array.Length-index);
  }

  /// <summary>Returns an array segment containing a subsection of the given array.</summary>
  public static ArraySegment<T> Segment<T>(this T[] array, int index, int length)
  {
    return new ArraySegment<T>(array, index, length);
  }

  /// <summary>Returns an array containing the remainder of the given array, starting at the given index. If <paramref name="index"/> is
  /// zero, the original array will be returned unchanged.
  /// </summary>
  public static T[] Subarray<T>(this T[] array, int index)
  {
    if(array == null) throw new ArgumentNullException();
    return Subarray(array, index, array.Length-index, false);
  }

  /// <summary>Returns an array containing a subsection of the given array. If the entire array is requested, the array will be returned
  /// unchanged.
  /// </summary>
  public static T[] Subarray<T>(this T[] array, int index, int length)
  {
    return Subarray(array, index, length, false);
  }

  /// <summary>Returns an array containing a subsection of the given array. If <paramref name="alwaysAllocate"/> is false, the original
  /// array will be returned if the entire array is requested. If <paramref name="alwaysAllocate"/> is false, this method will always
  /// return a freshly allocated array.
  /// </summary>
  public static T[] Subarray<T>(this T[] array, int index, int length, bool alwaysAllocate)
  {
    Utility.ValidateRange(array, index, length);
    if(!alwaysAllocate && index == 0 && length == array.Length) return array;
    T[] newArray = new T[length];
    Array.Copy(array, index, newArray, 0, length);
    return newArray;
  }

  /// <summary>Shrinks an array to a given length if it's not already of that length, and returns the array.</summary>
  public static T[] Trim<T>(this T[] array, int length)
  {
    return Subarray(array, 0, length, false);
  }

  /// <summary>Shrinks an array to a given length if it's not already of that length, and returns the array.</summary>
  public static T[] Trim<T>(this T[] array, long length)
  {
    if(array == null) throw new ArgumentNullException();
    if((ulong)length > (ulong)array.LongLength) throw new ArgumentOutOfRangeException();
    if(length != array.LongLength)
    {
      T[] trimmed = new T[length];
      Array.Copy(array, trimmed, length);
      array = trimmed;
    }
    return array;
  }
}
#endregion

#region ArrayUtility
public static partial class ArrayUtility
{
  /// <summary>Concatenates two arrays and returns the result. Source arrays that are null will be treated as empty. If all source arrays
  /// are null, null will be returned.
  /// </summary>
  /// <param name="alwaysAllocate">If false, a source array may be returned directly if all other source arrays are empty (i.e. if that
  /// source array's content would be equal to the concatenated content of the source arrays). If true, a new array will always be
  /// allocated to hold the concatenated content, assuming at least one source array is not empty.
  /// </param>
  /// <param name="a">The first source array.</param>
  /// <param name="b">The second source array.</param>
  public static T[] Concat<T>(bool alwaysAllocate, T[] a, T[] b)
  {
    return Concat(alwaysAllocate, a, b, null);
  }

  /// <summary>Concatenates three arrays and returns the result. Source arrays that are null will be treated as empty. If all source arrays
  /// are null, null will be returned.
  /// </summary>
  /// <param name="alwaysAllocate">If false, a source array may be returned directly if all other source arrays are empty (i.e. if that
  /// source array's content would be equal to the concatenated content of the source arrays). If true, a new array will always be
  /// allocated to hold the concatenated content, assuming at least one source array is not empty.
  /// </param>
  /// <param name="a">The first source array.</param>
  /// <param name="b">The second source array.</param>
  /// <param name="c">The third source array.</param>
  public static T[] Concat<T>(bool alwaysAllocate, T[] a, T[] b, T[] c)
  {
    T[] singleArray = a; // the single source array that equals the concatenated result
    long newLength = a == null ? 0 : a.LongLength;
    if(!IsNullOrEmpty(b))
    {
      newLength += b.LongLength;
      if(newLength < 0) throw new OutOfMemoryException();
      singleArray = IsNullOrEmpty(a) ? b : null;
    }
    if(!IsNullOrEmpty(c))
    {
      newLength += c.LongLength;
      if(newLength < 0) throw new OutOfMemoryException();
      singleArray = singleArray == b && IsNullOrEmpty(b) ? c : null;
    }

    if(newLength == 0 || !alwaysAllocate && singleArray != null) return singleArray;

    T[] newArray = new T[newLength];
    long index = 0;
    if(a != null) { Array.Copy(a, newArray, a.LongLength); index += a.LongLength; }
    if(b != null) { Array.Copy(b, 0, newArray, index, b.LongLength); index += b.LongLength; }
    if(c != null) Array.Copy(c, 0, newArray, index, c.LongLength);
    return newArray;
  }

  /// <summary>Concatenates two array segments and returns the result. Source arrays that are null will be treated as empty. If all
  /// source segments are null, null will be returned.
  /// </summary>
  public static T[] Concat<T>(ArraySegment<T> a, ArraySegment<T> b)
  {
    return Concat(a, b, default(ArraySegment<T>));
  }

  /// <summary>Concatenates three array segments and returns the result. Source arrays that are null will be treated as empty. If all
  /// source segments are null, null will be returned.
  /// </summary>
  public static T[] Concat<T>(ArraySegment<T> a, ArraySegment<T> b, ArraySegment<T> c)
  {
    long newLength = (long)a.Count + b.Count + c.Count;
    if(newLength == 0) return a.Array ?? b.Array ?? c.Array;
    T[] newArray = new T[newLength];
    long index = 0;
    if(a.Count != 0) { Array.Copy(a.Array, a.Offset, newArray, 0, a.Count); index += a.Count; }
    if(b.Count != 0) { Array.Copy(b.Array, b.Offset, newArray, index, b.Count); index += b.Count; }
    if(c.Count != 0) Array.Copy(c.Array, c.Offset, newArray, index, c.Count);
    return newArray;
  }

  /// <summary>Returns true if the given array is null or has a length of zero.</summary>
  public static bool IsNullOrEmpty(this Array array)
  {
    return array == null || array.LongLength == 0;
  }
}
#endregion

#region ArraySegmentEnumerable
/// <summary>Enumerates the items within a segment of an array.</summary>
public struct ArraySegmentEnumerable<T> : IEnumerable<T>
{
  /// <summary>Initializes a new <see cref="ArraySegmentEnumerable{T}"/> from the given <see cref="ArraySegment{T}"/>.</summary>
  public ArraySegmentEnumerable(ArraySegment<T> segment) : this(segment.Array, segment.Offset, segment.Count) { }

  /// <summary>Initializes a new <see cref="ArraySegmentEnumerable{T}"/> from the given array and region.</summary>
  public ArraySegmentEnumerable(T[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    this.array = array;
    this.start = index;
    this.count = count;
  }

  /// <inheritdoc/>
  public IEnumerator<T> GetEnumerator()
  {
    return new ArraySegmentEnumerator<T>(array, start, count);
  }

  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

  readonly T[] array;
  readonly int start, count;
}
#endregion

#region ArraySegmentEnumerator
/// <summary>Enumerates the items within a segment of an array.</summary>
public struct ArraySegmentEnumerator<T> : IEnumerator<T>
{
  /// <summary>Initializes a new <see cref="ArraySegmentEnumerator{T}"/> from the given <see cref="ArraySegment{T}"/>.</summary>
  public ArraySegmentEnumerator(ArraySegment<T> segment) : this(segment.Array, segment.Offset, segment.Count) { }

  /// <summary>Initializes a new <see cref="ArraySegmentEnumerator{T}"/> from the given array and region.</summary>
  public ArraySegmentEnumerator(T[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    this.array = array;
    this.start = index;
    this.end   = index + count;
    this.currentIndex = 0;
    Reset();
  }

  /// <include file="documentation.xml" path="//Utilities/IEnumerator/Current/node()"/>
  public T Current
  {
    get
    {
      if(currentIndex < start || currentIndex == end) throw new InvalidOperationException();
      return array[currentIndex];
    }
  }

  /// <include file="documentation.xml" path="//Utilities/IEnumerator/MoveNext/node()"/>
  public bool MoveNext()
  {
    if(currentIndex != end) currentIndex++;
    return currentIndex != end;
  }

  /// <include file="documentation.xml" path="//Utilities/IEnumerator/Reset/node()"/>
  public void Reset()
  {
    currentIndex = start-1;
  }

  object System.Collections.IEnumerator.Current
  {
    get { return Current; }
  }

  void IDisposable.Dispose() { }

  readonly T[] array;
  readonly int start, end;
  int currentIndex;
}
#endregion

#region ByteBuffer
/// <summary>Implements an <see cref="ArrayBuffer{T}"/> with additional, unsafe methods for using the buffer with byte pointers.</summary>
public sealed class ByteBuffer : ArrayBuffer<byte>
{
  /// <summary>Initializes a new <see cref="ByteBuffer"/> with the default capacity.</summary>
  public ByteBuffer() { }

  /// <summary>Initializes a new <see cref="ByteBuffer"/> with the given capacity.</summary>
  /// <param name="capacity">The initial capacity of the buffer. If equal to zero, the default capacity will be used.</param>
  public ByteBuffer(int capacity) : base(capacity) { }

  /// <summary>Adds the given number of bytes from the byte pointer to the buffer.</summary>
  [CLSCompliant(false)]
  public unsafe void AddRange(byte* sourceBytes, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    fixed(byte* dptr=GetArrayForWriting(count)) Unsafe.Copy(sourceBytes, dptr+End, count);
    AddCount(count);
  }

  /// <summary>Removes the given number of bytes from the buffer and copies them to the given byte pointer.</summary>
  [CLSCompliant(false)]
  public unsafe void Remove(byte* destination, int count)
  {
    if((uint)count > (uint)Count) throw new ArgumentOutOfRangeException();
    fixed(byte* dptr=Buffer) Unsafe.Copy(dptr+Offset, destination, count);
    Remove(count);
  }
}
#endregion

} // namespace AdamMil.Utilities
