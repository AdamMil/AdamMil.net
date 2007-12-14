using System;
using System.Collections.Generic;

namespace AdamMil.Collections
{

/// <summary>An abstract interface for a queue of items.</summary>
public interface IQueue<T> : ICollection<T>
{
  /// <summary>Adds an item to the queue.</summary>
  void Enqueue(T item);
  /// <summary>Returns and removes the first item from the queue.</summary>
  T Dequeue();
  /// <summary>Returns the first item in the queue.</summary>
  T Peek();
}

} // namespace AdamMil.Collections