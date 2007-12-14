using System;
using System.Collections.Generic;

namespace AdamMil.Collections
{

interface IQueue<T> : IEnumerable<T>
{
  void Enqueue(T item);
  T Dequeue();
  T Peek();
}

} // namespace AdamMil.Collections