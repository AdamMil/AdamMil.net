using System;
using System.Collections.Generic;

namespace AdamMil.Collections
{

/// <summary>This class represents a priority queue.</summary>
/// <remarks>
/// <para>A priority queue works like a standard <see cref="Queue"/>, except that the items are ordered by a
/// predicate. The value with the highest priority will always be dequeued first. The object with the highest priority
/// is the object with the greatest value, as determined by the <see cref="IComparer"/> used to initialize the queue.
/// Multiple objects with the same priority level can be added to the queue.
/// </para>
/// <para>The priority queue is implemented using a heap, which is a very efficient array structure that makes finding
/// the highest priority item very fast (an O(1) operation), but makes finding the lowest priority item rather
/// slow (an O(n) operation).
/// </para>
/// </remarks>
[Serializable]
public class PriorityQueue<T> : ICollection<T>
{
  /// <summary>Initializes a new, empty instance of the <see cref="PriorityQueue"/> class, with a default capacity and
  /// using <see cref="Comparer.Default"/> to compare elements.
  /// </summary>
  public PriorityQueue() : this(Comparer<T>.Default, 0) { }
  /// <summary>Initializes a new, empty instance of the <see cref="PriorityQueue"/> class, with the specified capacity
  /// and using <see cref="Comparer.Default"/> to compare elements.
  /// </summary>
  /// <param name="capacity">The initial capacity of the queue.</param>
  public PriorityQueue(int capacity) : this(Comparer<T>.Default, capacity) { }
  /// <summary>Initializes a new, empty instance of the <see cref="PriorityQueue"/> class, with a default capacity
  /// and using the specified <see cref="IComparer"/> to compare elements.
  /// </summary>
  /// <param name="comparer">The <see cref="IComparer"/> that will be used to compare elements.</param>
  public PriorityQueue(IComparer<T> comparer) : this(comparer, 0) { }
  /// <summary>Initializes a new, empty instance of the <see cref="PriorityQueue"/> class, with the specified capacity
  /// and using the given <see cref="IComparer"/> to compare elements.
  /// </summary>
  /// <param name="comparer">The <see cref="IComparer"/> that will be used to compare elements.</param>
  /// <param name="capacity">The initial capacity of the queue.</param>
  public PriorityQueue(IComparer<T> comparer, int capacity)
  {
    if(comparer == null) throw new ArgumentNullException("comparer");
    if(capacity < 0) throw new ArgumentOutOfRangeException("capacity", capacity, "capacity must not be negative");
    this.cmp       = comparer;
    this.array     = new List<T>(capacity);
  }

  /// <summary>Gets or sets the number of elements that the internal array can contain.</summary>
  public int Capacity
  {
    get { return array.Capacity; }
    set { array.Capacity = value; }
  }

  /// <summary>Removes and returns the element in the queue with the highest priority.</summary>
  /// <returns>The element in the queue with the highest priority.</returns>
  /// <exception cref="InvalidOperationException">Thrown if the collection is empty.</exception>
  public T Dequeue()
  {
    if(Count == 0) throw new InvalidOperationException("The collection is empty.");
    T max = array[0];
    array[0] = array[array.Count-1];
    array.RemoveAt(array.Count-1);
    HeapifyNode(0);
    return max;
  }

  /// <summary>Adds an item to the queue.</summary>
  /// <param name="value">The item to add to the queue.</param>
  public void Enqueue(T value)
  {
    int i = Count, ip;
    array.Add(value);

    // insert the item into the array
    while(i != 0) // heapify 'array'
    {
      ip = i/2;  // i=Parent(i)
      if(cmp.Compare(array[ip], value)>=0) break;
      array[i] = array[ip];
      i = ip;
    }
    array[i] = value;
  }

  /// <summary>Returns the element in the queue with the highest priority.</summary>
  /// <remarks>This method does not modify the queue.</remarks>
  /// <exception cref="InvalidOperationException">Thrown if the collection is empty.</exception>
  public T Peek()
  {
    if(Count == 0) throw new InvalidOperationException("The collection is empty.");
    return array[0];
  }

  /// <summary>Shrinks the capacity to the actual number of elements in the priority queue.</summary>
  public void TrimExcess() { array.TrimExcess(); }

  #region ICollection<>
  /// <summary>Gets the number of elements contained in the priority queue.</summary>
  public int Count { get { return array.Count; } }
  /// <summary>Gets a value indicating whether access to the queue is read-only.</summary>
  /// <remarks>See the <see cref="ICollection.IsReadOnly"/> property for more information.
  /// <seealso cref="ICollection.IsReadOnly"/>
  /// </remarks>
  public bool IsReadOnly { get { return false; } }

  void ICollection<T>.Add(T item) { Enqueue(item); }
  /// <summary>Removes all elements from the priority queue.</summary>
  public void Clear() { array.Clear(); }
  /// <summary>Returns whether an item exists in the queue.</summary>
  /// <param name="item">The item to search for.</param>
  /// <returns>True if the item exists in the queue and false otherwise.</returns>
  public bool Contains(T item) { return array.Contains(item); }
  /// <summary>Copies the queue elements to an existing one-dimensional Array, starting at the specified array index.</summary>
  /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied
  /// from the queue.
  /// </param>
  /// <param name="startIndex">The zero-based index in array at which copying begins.</param>
  /// <remarks>See <see cref="ICollection.CopyTo"/> for more information. <seealso cref="ICollection.CopyTo"/></remarks>
  public void CopyTo(T[] array, int startIndex) { this.array.CopyTo(array, startIndex); }
  /// <summary>Removes an item from the queue.</summary>
  /// <param name="item">The item to remove.</param>
  /// <remarks>Removing an item in this fashion is not efficient - O(ln n).</remarks>
  public bool Remove(T item)
  {
    int index = array.IndexOf(item);
    if(index == -1) return false;
    array.RemoveAt(index);
    Heapify();
    return true;
  }
  #endregion

  #region IEnumerable
  /// <summary>Returns an enumerator that can iterate through the queue.</summary>
  /// <returns>An <see cref="IEnumerator"/> that can be used to iterate through the queue.</returns>
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
  #endregion

  #region IEnumerable<T>
  /// <summary>Returns an enumerator that can iterate through the queue.</summary>
  /// <returns>An <see cref="IEnumerator"/> that can be used to iterate through the queue.</returns>
  public IEnumerator<T> GetEnumerator() { return array.GetEnumerator(); }
  #endregion

  /// <summary>Heapify the array from node <paramref name="i"/>, assuming that Right(i) and Left(i) are both valid
  /// heaps already.
  /// </summary>
  /// <param name="i"></param>
  void HeapifyNode(int i)
  {
    T tmp;
    int li, ri, largest, count=Count;
    while(true)
    {
      ri=(i+1)*2; li=ri-1; // ri=Right(i), li=Left(i)
      // find the largest of i and its two children
      largest = li<count && cmp.Compare(array[li], array[i])>0 ? li : i;
      if(ri<count && cmp.Compare(array[ri], array[largest])>0) largest=ri;
      if(largest == i) break; // if the largest is i, the heap property is satisfied
      // otherwise, swap i with largest, restoring the heap property for this triplet.
      tmp=array[i]; array[i]=array[largest]; array[largest]=tmp;
      // but that may have broken the heap property for the subtree 'largest', so repeat for that child
      i = largest;
    }
  }

  /// <summary>Heapify the entire array, which may be in any order.</summary>
  void Heapify()
  {
    // start at the last node that could possibly have children (the rightmost non-leaf node, the last node that could
    // possibly violate the heap property) and work our way up the tree back to the root.
    for(int i=Count/2-1; i >= 0; i--) HeapifyNode(i);
  }

  List<T> array; // slightly less efficient than managing an array ourselves, but makes for much simpler code
  IComparer<T> cmp;
}

} // namespace AdamMil.Collections