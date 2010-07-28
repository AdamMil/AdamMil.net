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

/// <summary>This class represents a priority queue.</summary>
/// <remarks>
/// <para>A priority queue works like a standard <see cref="Queue{T}"/>, except that the items are ordered by a
/// predicate. The item with the highest priority will always be dequeued first. The object with the highest priority
/// is the object with the greatest value, as determined by the <see cref="IComparer{T}"/> used to initialize the
/// queue. Multiple objects with the same priority can be added to the queue.
/// </para>
/// <para>The priority queue is implemented using a heap, which is a very efficient array structure that makes finding
/// the highest priority item very fast (an O(1) operation), but makes finding the lowest priority item rather slow (an
/// O(n) operation).
/// </para>
/// </remarks>
[Serializable]
public sealed class PriorityQueue<T> : IQueue<T>, IReadOnlyCollection<T>
{
  /// <summary>Initializes a new, empty instance of the <see cref="PriorityQueue{T}"/> class, with a default capacity
  /// and using <see cref="Comparer{T}.Default"/> to compare elements.
  /// </summary>
  public PriorityQueue() : this(Comparer<T>.Default, 0) { }
  /// <summary>Initializes a new, empty instance of the <see cref="PriorityQueue{T}"/> class, with the specified
  /// capacity and using <see cref="Comparer{T}.Default"/> to compare elements.
  /// </summary>
  /// <param name="capacity">The initial capacity of the queue.</param>
  public PriorityQueue(int capacity) : this(Comparer<T>.Default, capacity) { }
  /// <summary>Initializes a new, empty instance of the <see cref="PriorityQueue{T}"/> class, with a default capacity
  /// and using the specified <see cref="IComparer{T}"/> to compare elements.
  /// </summary>
  /// <param name="comparer">The <see cref="IComparer{T}"/> that will be used to compare elements.</param>
  public PriorityQueue(IComparer<T> comparer) : this(comparer, 0) { }
  /// <summary>Initializes a new, empty instance of the <see cref="PriorityQueue{T}"/> class, with the specified
  /// capacity and using the given <see cref="IComparer{T}"/> to compare elements.
  /// </summary>
  /// <param name="comparer">The <see cref="IComparer{T}"/> that will be used to compare elements.</param>
  /// <param name="capacity">The initial capacity of the queue.</param>
  public PriorityQueue(IComparer<T> comparer, int capacity)
  {
    if(comparer == null) throw new ArgumentNullException("comparer");
    if(capacity < 0) throw new ArgumentOutOfRangeException("capacity", capacity, "capacity must not be negative");
    this.cmp       = comparer;
    this.array     = new T[capacity == 0 ? 16 : capacity];
  }

  /// <summary>Gets or sets the number of elements that the internal array can contain.</summary>
  public int Capacity
  {
    get { return array.Length; }
    set
    {
      if(value != array.Length)
      {
        if(value < count) throw new ArgumentOutOfRangeException("Capacity", "Capacity cannot be less than Count.");
        T[] newArray = new T[value];
        Array.Copy(array, newArray, count);
        array = newArray;
      }
    }
  }

  /// <summary>Removes and returns the element in the queue with the highest priority.</summary>
  /// <returns>The element in the queue with the highest priority.</returns>
  /// <exception cref="InvalidOperationException">Thrown if the collection is empty.</exception>
  public T Dequeue()
  {
    if(count == 0) throw new InvalidOperationException("The collection is empty.");
    T max = array[0];
    array[0] = array[--count];
    array[count] = default(T); // remove the reference to the old item
    HeapifySubtree(0);
    version++;
    return max;
  }

  /// <summary>Adds an item to the queue.</summary>
  /// <param name="item">The item to add to the queue.</param>
  public void Enqueue(T item)
  {
    if(count == Capacity) Capacity = count == 0 ? 16 : count*2;
    int i = count++, ip;

    while(i != 0) // heapify the array
    {
      ip = (i+1)/2-1;  // i=Parent(i)
      if(cmp.Compare(array[ip], item)>=0) break;
      array[i] = array[ip];
      i = ip;
    }
    array[i] = item;

    version++;
  }

  /// <summary>Returns the element in the queue with the highest priority.</summary>
  /// <remarks>This method does not modify the queue.</remarks>
  /// <exception cref="InvalidOperationException">Thrown if the collection is empty.</exception>
  public T Peek()
  {
    if(count == 0) throw new InvalidOperationException("The collection is empty.");
    return array[0];
  }

  /// <summary>Returns an array containing all of the items in the queue.</summary>
  public T[] ToArray()
  {
    T[] array = new T[Count];
    CopyTo(array, 0);
    return array;
  }

  /// <summary>Shrinks the capacity to the actual number of elements in the priority queue.</summary>
  public void TrimExcess() { Capacity = count; }

  #region ICollection<>
  /// <summary>Gets the number of elements contained in the priority queue.</summary>
  public int Count { get { return count; } }
  /// <summary>Gets a value indicating whether access to the queue is read-only.</summary>
  /// <remarks>See the <see cref="ICollection{T}.IsReadOnly"/> property for more information.
  /// <seealso cref="ICollection{T}.IsReadOnly"/>
  /// </remarks>
  public bool IsReadOnly { get { return false; } }

  void ICollection<T>.Add(T item) { Enqueue(item); }
  /// <summary>Removes all elements from the priority queue.</summary>
  public void Clear()
  {
    Array.Clear(array, 0, count);
    count = 0;
    version++;
  }
  /// <summary>Returns whether an item exists in the queue.</summary>
  /// <param name="item">The item to search for.</param>
  /// <returns>True if the item exists in the queue and false otherwise.</returns>
  public bool Contains(T item) { return IndexOf(item) != -1; }
  /// <summary>Copies the queue elements to an existing one-dimensional Array, starting at the specified array index.</summary>
  /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied
  /// from the queue.
  /// </param>
  /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
  /// <remarks>The items in the array will be in the same order that they would be dequeued.</remarks>
  public void CopyTo(T[] array, int arrayIndex)
  {
    Array.Copy(this.array, 0, array, arrayIndex, count);
    Array.Sort(array, arrayIndex, count, new ReversedComparer<T>(cmp));
  }
  /// <summary>Removes an item from the queue.</summary>
  /// <param name="item">The item to remove.</param>
  /// <remarks>Removing an item in this fashion is not efficient.</remarks>
  bool ICollection<T>.Remove(T item)
  {
    int index = IndexOf(item);
    if(index == -1) return false;
    for(count--; index<count; index++) array[index] = array[index+1];
    array[count] = default(T); // remove the duplicated reference to the last item
    Heapify();
    version++;
    return true;
  }

  int IndexOf(T item)
  {
    // TODO: this could be optimized to exclude whole subtrees from the search based on the tree structure 
    for(int i=0; i<count; i++)
    {
      if(cmp.Compare(array[i], item) == 0) return i;
    }
    return -1;
  }
  #endregion

  #region IEnumerable
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
  #endregion

  #region IEnumerable<T>
  sealed class Enumerator : IEnumerator<T>
  {
    public Enumerator(PriorityQueue<T> queue)
    {
      this.queue   = queue;
      this.version = queue.version;
      array = new T[queue.count];
      queue.CopyTo(array, 0);
      Reset();
    }

    public T Current
    {
      get
      {
        if(index < 0 || index == array.Length) throw new InvalidOperationException();
        return array[index];
      }
    }

    public bool MoveNext()
    {
      AssertNotModified();
      return index == array.Length ? false : ++index < array.Length;
    }

    public void Reset()
    {
      AssertNotModified();
      index = -1;
    }

    void AssertNotModified()
    {
      if(version != queue.version) throw new InvalidOperationException();
    }

    object System.Collections.IEnumerator.Current
    {
      get { return Current; }
    }
    
    void IDisposable.Dispose() { }
    
    readonly PriorityQueue<T> queue;
    T[] array;
    int index, version;
  }

  /// <summary>Returns an <see cref="IEnumerator{T}"/> that can iterate through the queue in the same order as items
  /// would be dequeued.
  /// </summary>
  public IEnumerator<T> GetEnumerator() { return new Enumerator(this); }
  #endregion

  /// <summary>Heapify the subtree at index <paramref name="i"/>, assuming that Right(i) and Left(i) are both valid
  /// heaps already.
  /// </summary>
  void HeapifySubtree(int i)
  {
    T temp;
    int li, ri, largest;
    while(true)
    {
      ri=(i+1)*2; li=ri-1; // ri=Right(i), li=Left(i)
      // find the largest of i and its two children
      largest = li<count && cmp.Compare(array[li], array[i])>0 ? li : i;
      if(ri<count && cmp.Compare(array[ri], array[largest])>0) largest=ri;
      if(largest == i) break; // if the largest is i, the heap property is satisfied
      // otherwise, swap i with largest, restoring the heap property for this triplet.
      temp = array[i]; array[i] = array[largest]; array[largest] = temp;
      // but that may have broken the heap property for the subtree 'largest', so repeat for that child
      i = largest;
    }
  }

  /// <summary>Heapify the entire array, which may be in any order.</summary>
  void Heapify()
  {
    // start at the last node that could possibly have children (the rightmost non-leaf node, the last node that could
    // possibly violate the heap property) and work our way up the tree back to the root.
    for(int i=count/2-1; i >= 0; i--) HeapifySubtree(i);
  }

  T[] array;
  IComparer<T> cmp;
  int count, version;
}

} // namespace AdamMil.Collections