/*
AdamMil.Collections is a library that provides useful collection classes for
the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2016 Adam Milazzo

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

/// <summary>This class represents a stack, also known as a LIFO queue.</summary>
/// <remarks>Unlike the built-in <see cref="System.Collections.Generic.Stack{T}"/> class, this collection is indexable.
/// It also implements <see cref="IQueue{T}"/>.
/// </remarks>
[Serializable]
public sealed class IndexableStack<T> : IQueue<T>, IReadOnlyList<T>
{
  /// <summary>Initializes a new, empty instance of the <see cref="IndexableStack{T}"/> class, with a default capacity.</summary>
  public IndexableStack() : this(0) { }
  /// <summary>Initializes a new, empty instance of the <see cref="IndexableStack{T}"/> class, with the specified capacity.</summary>
  /// <param name="capacity">The initial capacity of the stack.</param>
  public IndexableStack(int capacity)
  {
    if(capacity < 0) throw new ArgumentOutOfRangeException("capacity", capacity, "capacity must not be negative");
    this.array = new T[capacity == 0 ? 16 : capacity];
    this.mustClear = !typeof(T).UnderlyingSystemType.IsPrimitive; // primitive types don't need to be cleared
  }
  /// <summary>Initializes a new instance of the <see cref="IndexableStack{T}"/> class, filled with items from the given object.</summary>
  /// <param name="items">An <see cref="IEnumerable{T}"/> containing objects to add to the stack.</param>
  public IndexableStack(IEnumerable<T> items)
  {
    if(items == null) throw new ArgumentNullException();

    ICollection<T> collection = items as ICollection<T>;
    if(collection != null)
    {
      array = new T[collection.Count == 0 ? 16 : collection.Count];
      collection.CopyTo(array, 0);
    }
    else
    {
      array = new T[16];
      foreach(T item in items) Push(item);
    }

    this.mustClear = !typeof(T).UnderlyingSystemType.IsPrimitive; // primitive types don't need to be cleared
  }

  #region Enumerator
  /// <summary>An enumerator capable of enumerating the items in a <see cref="IndexableStack{T}"/>.</summary>
  /// <remarks>This structure is not meant to be instantiated directly. Rather, you should call <see cref="GetEnumerator"/>.</remarks>
  public struct Enumerator : IEnumerator<T>
  {
    internal Enumerator(IndexableStack<T> stack)
    {
      this.stack = stack;
      version    = stack.version;
      index      = 0;
      count      = 0;
      current    = default(T);
      Reset();
    }

    /// <inheritdoc/>
    public T Current
    {
      get
      {
        if(index < 0 || index == count) throw new InvalidOperationException();
        return current;
      }
    }

    /// <inheritdoc/>
    public void Dispose() { }

    /// <inheritdoc/>
    public bool MoveNext()
    {
      AssertNotModified();
      if(index == -1) return false;
      if(--index == -1)
      {
        current = default(T);
        return false;
      }
      else
      {
        current = stack.array[index];
        return true;
      }
    }

    /// <inheritdoc/>
    public void Reset()
    {
      AssertNotModified();
      count   = stack.count;
      index   = count;
    }

    void AssertNotModified()
    {
      if(version != stack.version) throw new InvalidOperationException("The collection has been modified.");
    }

    object System.Collections.IEnumerator.Current
    {
      get { return Current; }
    }

    readonly IndexableStack<T> stack;
    int index, count, version;
    T current;
  }
  #endregion

  /// <summary>Gets or sets an item in the stack.</summary>
  /// <param name="index">The index of the item on the stack. The bottom of the stack has an index of zero and the top
  /// of the stack has an index of <see cref="Count"/>-1.
  /// </param>
  public T this[int index]
  {
    get
    {
      if((uint)index >= (uint)count) throw new ArgumentOutOfRangeException();
      return array[index];
    }
    set
    {
      if((uint)index >= (uint)count) throw new ArgumentOutOfRangeException();
      array[index] = value;
      version++;
    }
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

  /// <summary></summary>
  /// <returns></returns>
  public Enumerator GetEnumerator()
  {
    return new Enumerator(this);
  }

  /// <summary>Returns the index of the first item in the stack (starting from the bottom) that is equal to the given
  /// value, or -1 if the item cannot be found.
  /// </summary>
  public int IndexOf(T item)
  {
    return Array.IndexOf(array, item, 0, count);
  }

  /// <summary>Returns the element on top of the stack.</summary>
  /// <exception cref="InvalidOperationException">Thrown if the collection is empty.</exception>
  public T Peek()
  {
    if(count == 0) throw new InvalidOperationException("The collection is empty.");
    return array[count-1];
  }

  /// <summary>Removes and returns the element on top of the stack.</summary>
  /// <exception cref="InvalidOperationException">Thrown if the collection is empty.</exception>
  public T Pop()
  {
    if(count == 0) throw new InvalidOperationException("The collection is empty.");
    T item = array[--count];
    array[count] = default(T);
    version++;
    return item;
  }

  /// <summary>Removes the given number of items from the top of the stack.</summary>
  public void Pop(int count)
  {
    if((uint)count > (uint)Count) throw new ArgumentOutOfRangeException();
    this.count -= count;
    if(mustClear) Array.Clear(array, Count, count);
    version++;
  }

  /// <summary>Adds an item to the stack.</summary>
  /// <param name="item">The item to add to the stack.</param>
  public void Push(T item)
  {
    if(count == Capacity) Capacity = count == 0 ? 16 : count*2;
    array[count++] = item;
    version++;
  }

  /// <summary>Returns an array containing all of the items in the stack.</summary>
  public T[] ToArray()
  {
    T[] array = new T[Count];
    CopyTo(array, 0);
    return array;
  }

  /// <summary>Shrinks the capacity to the actual number of elements in the priority queue.</summary>
  public void TrimExcess() { Capacity = count; }

  /// <summary>Attempts to remove an item from the stack. True is returned if an item was successfully removed (and stored in
  /// <paramref name="item"/>) and false if an item was not removed because the stack was empty.
  /// </summary>
  public bool TryPop(out T item)
  {
    if(Count == 0)
    {
      item = default(T);
      return false;
    }
    else
    {
      item = Pop();
      return true;
    }
  }

  bool IQueue<T>.IsEmpty
  {
    get { return Count == 0; }
  }

  T IQueue<T>.Dequeue()
  {
    return Pop();
  }

  void IQueue<T>.Enqueue(T item)
  {
    Push(item);
  }

  bool IQueue<T>.TryDequeue(out T item)
  {
    return TryPop(out item);
  }

  #region ICollection<T>
  /// <summary>Gets the number of elements contained in the stack.</summary>
  public int Count { get { return count; } }
  /// <summary>Gets a value indicating whether access to the stack is read-only.</summary>
  /// <remarks>See the <see cref="ICollection{T}.IsReadOnly"/> property for more information.
  /// <seealso cref="ICollection{T}.IsReadOnly"/>
  /// </remarks>
  public bool IsReadOnly { get { return false; } }

  void ICollection<T>.Add(T item) { Push(item); }
  /// <summary>Removes all elements from the stack.</summary>
  public void Clear()
  {
    if(mustClear) Array.Clear(array, 0, count);
    count = 0;
    version++;
  }
  /// <summary>Returns whether an item exists in the stack.</summary>
  /// <param name="item">The item to search for.</param>
  /// <returns>True if the item exists in the stack and false otherwise.</returns>
  public bool Contains(T item) { return IndexOf(item) != -1; }
  /// <summary>Copies the stack to an existing one-dimensional Array, starting at the specified array index.</summary>
  /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied
  /// from the queue.
  /// </param>
  /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
  /// <remarks>The items in the array will be in the same order that they would be dequeued.</remarks>
  public void CopyTo(T[] array, int arrayIndex)
  {
    Array.Copy(this.array, 0, array, arrayIndex, count);
    Array.Reverse(array, arrayIndex, count);
  }
  /// <summary>Removes an item from the stack.</summary>
  /// <param name="item">The item to remove.</param>
  bool ICollection<T>.Remove(T item)
  {
    int index = IndexOf(item);
    if(index == -1) return false;
    for(count--; index<count; index++) array[index] = array[index+1];
    array[count] = default(T); // remove the duplicated reference to the last item
    version++;
    return true;
  }
  #endregion

  #region IEnumerable
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
  #endregion

  #region IEnumerable<T>
  /// <summary>Returns an <see cref="IEnumerator{T}"/> that can iterate through the queue in the same order as items
  /// would be dequeued.
  /// </summary>
  IEnumerator<T> IEnumerable<T>.GetEnumerator()
  {
    return GetEnumerator();
  }
  #endregion

  T[] array;
  int count, version;
  bool mustClear;
}

} // namespace AdamMil.Collections