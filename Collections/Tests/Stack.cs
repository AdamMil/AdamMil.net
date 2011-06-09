using System;
using System.Collections.Generic;
using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Collections.Tests
{

[TestFixture]
public class StackTest
{
  [Test]
  public void Test()
  {
    int[] numbers = new int[] { 8, 4, 0, 2, 5, 7, 1, 8, 6, 3, 8, 9 };

    Stack<int> queue;

    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { queue = new Stack<int>(-10); });

    queue = new Stack<int>(4); // use a small capacity to force a resize
    Assert.IsFalse(queue.IsReadOnly);

    TestHelpers.TestException<InvalidOperationException>(delegate() { queue.Pop(); });
    TestHelpers.TestException<InvalidOperationException>(delegate() { queue.Peek(); });
    TestHelpers.TestEnumerator(queue);

    // test basic addition and removal
    List<int> list = new List<int>(numbers);
    AddItems(queue, numbers);
    Assert.AreEqual(queue.Peek(), 9);
    ReversedDequeue(queue, list);

    // test Count
    AddItems(queue, numbers);
    Assert.AreEqual(queue.Count, numbers.Length);

    // test CopyTo
    list = new List<int>(numbers);
    list.Reverse();
    int[] array = new int[queue.Count+5];
    queue.CopyTo(array, 0);
    for(int i=0; i<list.Count; i++) Assert.AreEqual(array[i], list[i]);
    queue.CopyTo(array, 5);
    for(int i=0; i<list.Count; i++) Assert.AreEqual(array[i+5], list[i]);

    // test indexer
    for(int i=0; i<numbers.Length; i++) Assert.AreEqual(numbers[i], queue[i]);
    int original = queue.Peek();
    queue[queue.Count-1] = ~original;
    Assert.AreEqual(~original, queue.Peek());
    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { int x = queue[-1]; });
    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { int x = queue[queue.Count]; });
    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { queue[-1] = 0; });
    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { queue[queue.Count] = 0; });

    // test Capacity
    queue.Capacity = 20;
    Assert.AreEqual(20, queue.Capacity);
    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { queue.Capacity = 1; });

    // test TrimExcess
    queue.TrimExcess();
    Assert.AreEqual(numbers.Length, queue.Capacity);

    // test Contains
    queue.Clear();
    AddItems(queue, numbers);
    foreach(int i in numbers) Assert.IsTrue(queue.Contains(i));
    Assert.IsFalse(queue.Contains(20));

    // test Remove
    list = new List<int>(numbers);
    TestRemove(queue, list, 7, 8, 9);
    Assert.IsFalse(((ICollection<int>)queue).Remove(20));

    // test Clear
    queue.Clear();
    Assert.AreEqual(0, queue.Count);
  }

  static void AddItems(Stack<int> queue, IList<int> items)
  {
    int initialCount = queue.Count;
    foreach(int i in items) queue.Push(i);
    Assert.AreEqual(initialCount+items.Count, queue.Count);
  }

  static void ReversedDequeue(Stack<int> queue, List<int> items)
  {
    Assert.AreEqual(items.Count, queue.Count);
    items.Reverse(); // the queue will return items in reverse order

    // first test using the enumerator
    int index = 0;
    foreach(int i in queue)
    {
      Assert.IsTrue(index < items.Count);
      Assert.AreEqual(items[index], i);
      index++;
    }

    // then actually dequeue the items
    foreach(int i in items) Assert.AreEqual(i, queue.Pop());
    Assert.AreEqual(queue.Count, 0);
  }

  static void TestRemove(Stack<int> queue, List<int> items, params int[] toRemove)
  {
    queue.Clear();
    AddItems(queue, items);
    foreach(int i in toRemove)
    {
      items.Remove(i);
      ((ICollection<int>)queue).Remove(i);
    }
    ReversedDequeue(queue, items);
  }
}

} // namespace AdamMil.Collections.Tests