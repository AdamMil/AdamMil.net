using System;
using System.Collections.Generic;

namespace AdamMil.Collections
{

/// <summary>This class represents a stack, also known as a LIFO queue.</summary>
[Serializable]
public sealed class Stack<T> : ICollection<T>, IQueue<T>
{
  /// <summary>Initializes a new, empty instance of the <see cref="Stack"/> class, with a default capacity.</summary>
  public Stack() : this(0) { }
  /// <summary>Initializes a new, empty instance of the <see cref="Stack"/> class, with the specified capacity.</summary>
  /// <param name="capacity">The initial capacity of the stack.</param>
  public Stack(int capacity)
  {
    if(capacity < 0) throw new ArgumentOutOfRangeException("capacity", capacity, "capacity must not be negative");
    this.array = new T[capacity == 0 ? 16 : capacity];
  }
  /// <summary>Initializes a new instance of the <see cref="Stack"/> class, filled with items from the given object.</summary>
  /// <param name="items">An <see cref="IEnumerable{T}"/> containing objects to add to the stack.</param>
  public Stack(IEnumerable<T> items)
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
      foreach(T item in items) Enqueue(item);
    }
  }

  /// <summary>Gets or sets an item in the stack.</summary>
  /// <param name="index">The index of the item on the stack. The bottom of the stack has an index of zero and the top
  /// of the stack has an index of <see cref="Count"/>-1.
  /// </param>
  public T this[int index]
  {
    get
    {
      if(index < 0 || index >= count) throw new ArgumentOutOfRangeException();
      return array[index];
    }
    set
    {
      if(index < 0 || index >= count) throw new ArgumentOutOfRangeException();
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

  /// <summary>Removes and returns the element on top of the stack.</summary>
  /// <exception cref="InvalidOperationException">Thrown if the collection is empty.</exception>
  public T Dequeue()
  {
    if(count == 0) throw new InvalidOperationException("The collection is empty.");
    T item = array[--count];
    array[count] = default(T);
    version++;
    return item;
  }

  /// <summary>Adds an item to the stack.</summary>
  /// <param name="value">The item to add to the stack.</param>
  public void Enqueue(T value)
  {
    if(count == Capacity) Capacity = count == 0 ? 16 : count*2;
    array[count++] = value;
    version++;
  }

  /// <summary>Returns the element on top of the stack.</summary>
  /// <exception cref="InvalidOperationException">Thrown if the collection is empty.</exception>
  public T Peek()
  {
    if(count == 0) throw new InvalidOperationException("The collection is empty.");
    return array[count-1];
  }

  /// <summary>Shrinks the capacity to the actual number of elements in the priority queue.</summary>
  public void TrimExcess() { Capacity = count; }

  #region ICollection<>
  /// <summary>Gets the number of elements contained in the stack.</summary>
  public int Count { get { return count; } }
  /// <summary>Gets a value indicating whether access to the stack is read-only.</summary>
  /// <remarks>See the <see cref="ICollection.IsReadOnly"/> property for more information.
  /// <seealso cref="ICollection.IsReadOnly"/>
  /// </remarks>
  public bool IsReadOnly { get { return false; } }

  void ICollection<T>.Add(T item) { Enqueue(item); }
  /// <summary>Removes all elements from the stack.</summary>
  public void Clear()
  {
    Array.Clear(array, 0, count);
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
  /// <param name="startIndex">The zero-based index in array at which copying begins.</param>
  /// <remarks>The items in the array will be in the same order that they would be dequeued.</remarks>
  public void CopyTo(T[] array, int startIndex)
  {
    Array.Copy(this.array, 0, array, startIndex, count);
    Array.Reverse(array, startIndex, count);
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

  int IndexOf(T item)
  {
    return Array.IndexOf(array, item, 0, count);
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
    public Enumerator(Stack<T> stack)
    {
      this.stack = stack;
      Reset();
    }

    public T Current
    {
      get
      {
        if(index < 0 || index == count) throw new InvalidOperationException();
        return current;
      }
    }

    public bool MoveNext()
    {
      if(version != stack.version) throw new InvalidOperationException();
      if(index == count) return false;
      if(++index == count)
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

    public void Reset()
    {
      version = stack.version;
      count   = -1;
      index   = count;
    }

    object System.Collections.IEnumerator.Current
    {
      get { return Current; }
    }
    
    void IDisposable.Dispose() { }
    
    readonly Stack<T> stack;
    T current;
    int index, count, version;
  }

  /// <summary>Returns an <see cref="IEnumerator{T}"/> that can iterate through the queue in the same order as items
  /// would be dequeued.
  /// </summary>
  public IEnumerator<T> GetEnumerator() { return new Enumerator(this); }
  #endregion

  T[] array;
  int count, version;
}

} // namespace AdamMil.Collections