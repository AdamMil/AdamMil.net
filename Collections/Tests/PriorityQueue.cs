using System;
using System.Collections.Generic;
using NUnit.Framework;
using AdamMil.Tests;
using AdamMil.Collections;

namespace AdamMil.Collections.Tests
{

[TestFixture]
public class PriorityQueueTest
{
  [Test]
  public void Test()
  {
    // these numbers were tweaked to exercise all paths within HeapifyNode()
    int[] numbers = new int[] { 8, 4, 0, 2, 5, 7, 1, 8, 6, 3, 8, 9 };

    PriorityQueue<int> queue;

    TestHelpers.TestException<ArgumentNullException>(delegate() { queue = new PriorityQueue<int>(null, 10); });
    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { queue = new PriorityQueue<int>(-10); });

    queue = new PriorityQueue<int>(4); // set a small capacity to ensure that the queue will be resized
    Assert.IsFalse(queue.IsReadOnly); // test IsReadOnly

    TestHelpers.TestException<InvalidOperationException>(delegate() { queue.Dequeue(); }); // don't allow underflow
    TestHelpers.TestException<InvalidOperationException>(delegate() { queue.Peek(); });
    TestHelpers.TestEnumerator(queue);

    // test addition
    List<int> list = new List<int>(numbers);
    AddItems(queue, numbers);
    Assert.AreEqual(queue.Peek(), 9);
    SortedDequeue(queue, list);

    // test enumerator
    queue.Enqueue(1);
    IEnumerator<int> e = queue.GetEnumerator();
    Assert.IsTrue(e.MoveNext());
    Assert.AreEqual(1, e.Current);
    queue.Enqueue(2);
    TestHelpers.TestException<InvalidOperationException>(delegate() { e.MoveNext(); }); // ensure MoveNext throws if the queue was modified
    queue.Clear();

    // test Count
    AddItems(queue, numbers);
    Assert.AreEqual(queue.Count, numbers.Length);

    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { queue.Capacity = 1; }); // don't allow insufficient capacities

    list = new List<int>(numbers);
    list.Sort();
    list.Reverse();
    int[] array = new int[queue.Count+5];
    queue.CopyTo(array, 0);
    for(int i=0; i<list.Count; i++) Assert.AreEqual(array[i], list[i]);
    queue.CopyTo(array, 5);
    for(int i=0; i<list.Count; i++) Assert.AreEqual(array[i+5], list[i]);

    queue.Capacity = 20;
    Assert.AreEqual(20, queue.Capacity);

    queue.TrimExcess();
    Assert.AreEqual(numbers.Length, queue.Capacity);

    foreach(int i in numbers) Assert.IsTrue(queue.Contains(i));
    Assert.IsFalse(queue.Contains(20));

    list = new List<int>(numbers);
    TestRemove(queue, list, 7);

    list = new List<int>(numbers);
    TestRemove(queue, list, 4, 1, 8, 8);

    list = new List<int>(numbers);
    TestRemove(queue, list, 8, 8, 4, 6);

    Assert.IsFalse(((ICollection<int>)queue).Remove(20));

    queue.Clear();
    Assert.AreEqual(0, queue.Count);
  }

  static void AddItems(PriorityQueue<int> queue, IList<int> items)
  {
    int initialCount = queue.Count;
    foreach(int i in items)
    {
      if((i & 1) == 0) queue.Enqueue(i); // use Enqueue or Add interchangeably
      else ((ICollection<int>)queue).Add(i);
    }
    Assert.AreEqual(initialCount+items.Count, queue.Count);
  }

  static void SortedDequeue(PriorityQueue<int> queue, List<int> items)
  {
    Assert.AreEqual(items.Count, queue.Count);
    items.Sort();
    items.Reverse(); // the queue will return items in reverse order (highest to lowest)

    // first test using the enumerator
    int index = 0;
    foreach(int i in queue)
    {
      Assert.IsTrue(index < items.Count);
      Assert.AreEqual(items[index], i);
      index++;
    }

    // then actually dequeue the items
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
      ((ICollection<int>)queue).Remove(i);
    }
    SortedDequeue(queue, items);
  }
}

} // namespace AdamMil.Collections.Tests