using System;
using System.Collections.Generic;
using NUnit.Framework;
using AdamMil.Collections;

namespace AdamMil.Collections.Tests
{

[TestFixture]
public class PriorityQueueTest
{
  [Test]
  public void Test()
  {
    // actually, these numbers aren't quite random. they were tweaked to exercise all paths within HeapifyNode()
    int[] randomNumbers = new int[] { 8, 4, 0, 2, 5, 7, 1, 8, 6, 3, 8, 9 };

    PriorityQueue<int> queue;

    Helpers.TestException<ArgumentNullException>(delegate() { queue = new PriorityQueue<int>(null, 10); });
    Helpers.TestException<ArgumentOutOfRangeException>(delegate() { queue = new PriorityQueue<int>(-10); });

    queue = new PriorityQueue<int>();
    Assert.IsFalse(queue.IsReadOnly);

    Helpers.TestException<InvalidOperationException>(delegate() { queue.Dequeue(); });
    Helpers.TestException<InvalidOperationException>(delegate() { queue.Peek(); });

    List<int> list = new List<int>(randomNumbers);
    AddItems(queue, randomNumbers);
    Assert.AreEqual(queue.Peek(), 9);
    SortedDequeue(queue, list);

    AddItems(queue, randomNumbers);

    queue.Capacity = 20;
    Assert.AreEqual(20, queue.Capacity);

    queue.TrimExcess();
    Assert.AreEqual(randomNumbers.Length, queue.Capacity);

    foreach(int i in randomNumbers) Assert.IsTrue(queue.Contains(i));
    Assert.IsFalse(queue.Contains(20));

    list = new List<int>(randomNumbers);
    TestRemove(queue, list, 7);

    list = new List<int>(randomNumbers);
    TestRemove(queue, list, 4, 1, 8, 8);

    list = new List<int>(randomNumbers);
    TestRemove(queue, list, 8, 8, 4, 6);

    Assert.IsFalse(queue.Remove(20));

    queue.Clear();
    Assert.AreEqual(0, queue.Count);
  }

  static void AddItems(PriorityQueue<int> queue, IList<int> items)
  {
    int initialCount = queue.Count;
    foreach(int i in items) queue.Enqueue(i);
    Assert.AreEqual(initialCount+items.Count, queue.Count);
  }

  static void SortedDequeue(PriorityQueue<int> queue, List<int> items)
  {
    Assert.AreEqual(items.Count, queue.Count);
    items.Sort();
    items.Reverse(); // the queue will return items in reverse order (highest to lowest)
    foreach(int i in items) Assert.AreEqual(i, queue.Dequeue());
    Assert.AreEqual(queue.Count, 0);
  }

  static void TestRemove(PriorityQueue<int> queue, List<int> items, params int[] toRemove)
  {
    queue.Clear();
    AddItems(queue, items);
    foreach(int i in toRemove)
    {
      items.Remove(i);
      queue.Remove(i);
    }
    SortedDequeue(queue, items);
  }
}

} // namespace AdamMil.Collections.Tests