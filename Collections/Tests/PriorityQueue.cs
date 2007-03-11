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
    PriorityQueue<int> queue = new PriorityQueue<int>();

    bool threw = false;
    try { queue.Dequeue(); }
    catch(InvalidOperationException) { threw = true; }
    Assert.IsTrue(threw);

    threw = false;
    try { queue.Peek(); }
    catch(InvalidOperationException) { threw = true; }
    Assert.IsTrue(threw);

    int[] randomNumbers = new int[] { 8, 4, 0, 2, 5, 7, 1, 8, 6, 3, 8, 9 };

    List<int> list = new List<int>(randomNumbers);
    AddItems(queue, randomNumbers);
    Assert.AreEqual(queue.Peek(), 9);
    SortedDequeue(queue, list);

    list = new List<int>(randomNumbers);
    TestRemove(queue, list, 7);

    list = new List<int>(randomNumbers);
    TestRemove(queue, list, 4, 1, 8, 8);

    list = new List<int>(randomNumbers);
    TestRemove(queue, list, 8, 8, 4, 6);

    Assert.IsFalse(queue.Remove(20));
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