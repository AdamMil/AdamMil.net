using System;
using System.Collections.Generic;

namespace AdamMil.Collections
{

/// <summary>An interface representing a queue of items.</summary>
public interface IQueue<T> : ICollection<T>
{
  /// <summary>Adds an item to the queue.</summary>
  void Enqueue(T item);
  /// <summary>Returns and removes the first item from the queue.</summary>
  T Dequeue();
  /// <summary>Returns the first item in the queue.</summary>
  T Peek();
}

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

} // namespace AdamMil.Collections