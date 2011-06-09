using System;
using System.Collections.Generic;
using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Collections.Tests
{

[TestFixture]
public class CircularListTest
{
  #region MyCircularList
  class MyCircularList<T> : CircularList<T>
  {
    public MyCircularList() { }
    public MyCircularList(int capacity) : base(capacity) { }
    public MyCircularList(int capacity, bool canGrow) : base(capacity, canGrow) { }

    public new void MakeContiguous()
    {
      base.MakeContiguous();
      Assert.IsTrue(IsContiguous);
    }

    public void MakeNonContiguous(T[] items)
    {
      Clear();
      Capacity = items.Length;
      int added = items.Length/2;
      AddRange(items, items.Length-added, added);
      Insert(0, items, 0, items.Length-added);
      Assert.IsFalse(IsContiguous);
    }

    public void TestLogicalIndex()
    {
      TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { GetLogicalIndex(-1); });
      TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { GetLogicalIndex(Count); });
      Assert.AreEqual(0, GetLogicalIndex(Tail));
      Assert.AreEqual(Count-1, GetLogicalIndex(Head == 0 ? List.Length-1 : Head-1));
    }

    public void VerifyCleared()
    {
      if(IsContiguous) // if the data is contiguous, the free space is not
      {
        for(int i=0; i<Tail; i++) Assert.AreEqual(default(T), List[i]);
        for(int i=Head; i<List.Length; i++) Assert.AreEqual(default(T), List[i]);
      }
      else
      {
        for(int i=Head; i<Tail; i++) Assert.AreEqual(default(T), List[i]);
      }
    }
  }
  #endregion

  #region BasicTest
  [Test]
  public void BasicTest()
  {
    int[] oneToTen = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    List<int> list = new List<int>(oneToTen);

    MyCircularList<int> circ = new MyCircularList<int>(40);
    Assert.IsTrue(circ.CanGrow);
    Assert.IsFalse(circ.IsReadOnly);

    // test AddRange
    circ.AddRange(oneToTen);
    Compare(circ, oneToTen);

    // test IndexOf
    foreach(int i in oneToTen)
    {
      Assert.AreEqual(i-1, circ.IndexOf(i));
    }
    Assert.AreEqual(-1, circ.IndexOf(11));

    // test TrimExcess() -- resizing downward
    circ.TrimExcess();
    Compare(circ, list);

    // test resizing upwards (and AddRange(IEnumerable<T>))
    circ.AddRange((IEnumerable<int>)oneToTen); // small resize code path
    list.AddRange(oneToTen);
    Compare(circ, list);
    list.AddRange(oneToTen);
    circ.Clear();
    circ.Capacity = 4;
    circ.AddRange(list.ToArray()); // test the big resize code path (that goes through the while loop)
    Compare(circ, list);

    // test Insert(0, T[])
    circ.Insert(0, oneToTen, 0, oneToTen.Length);
    list.AddRange(oneToTen);
    Compare(circ, list);

    // test Insert(Count, T[])
    circ.Insert(circ.Count, oneToTen, 0, oneToTen.Length);
    list.AddRange(oneToTen);
    Compare(circ, list);

    // test RemoveFirst()
    foreach(int i in list)
    {
      Assert.AreEqual(i, circ.RemoveFirst());
    }
    Assert.AreEqual(0, circ.Count);
    list.Clear();

    // test RemoveLast()
    circ.MakeNonContiguous(oneToTen);
    for(int i=oneToTen.Length-1; i >= 0; i--)
    {
      Assert.AreEqual(oneToTen[i], circ.RemoveLast());
    }
    Assert.AreEqual(0, circ.Count);

    // test RemoveFirst(T[])
    circ.Insert(0, oneToTen, 0, oneToTen.Length);
    int[] firstFive = new int[5];
    circ.RemoveFirst(firstFive, 0, 5);
    TestHelpers.AssertArrayEquals(firstFive, 1, 2, 3, 4, 5);
    Compare(circ, 6, 7, 8, 9, 10);

    // test CopyTo
    Array.Clear(firstFive, 0, firstFive.Length);
    circ.CopyTo(firstFive, 0);
    TestHelpers.AssertArrayEquals(firstFive, 6, 7, 8, 9, 10);

    // test RemoveLast(T[])
    circ.Clear();
    circ.Insert(0, oneToTen, 0, oneToTen.Length);
    int[] lastFive = new int[5];
    circ.RemoveLast(lastFive, 0, 5);
    TestHelpers.AssertArrayEquals(lastFive, 6, 7, 8, 9, 10);
    Compare(circ, 1, 2, 3, 4, 5);

    // test RemoveRange()
    circ.RemoveRange(0, 2);
    Compare(circ, 3, 4, 5);
    circ.RemoveRange(circ.Count-2, 2);
    Compare(circ, 3);

    // test setter
    circ.Clear();
    circ.AddRange(oneToTen);
    for(int i=0; i<circ.Count; i++)
    {
      circ[i]++;
      Assert.AreEqual(oneToTen[i]+1, circ[i]);
    }

    // test Insert(int, T)
    circ.Clear();
    foreach(int i in oneToTen) circ.Insert(0, i);
    list = new List<int>(oneToTen);
    list.Reverse();
    Compare(circ, list);

    // test RemoveAt(int)
    circ.Clear();
    circ.AddRange(oneToTen);
    circ.RemoveAt(2);
    Compare(circ, 1, 2, 4, 5, 6, 7, 8, 9, 10);
    circ.RemoveAt(0);
    Compare(circ, 2, 4, 5, 6, 7, 8, 9, 10);
    circ.RemoveAt(circ.Count-1);
    Compare(circ, 2, 4, 5, 6, 7, 8, 9);

    // test Remove(T) (and thus RemoveAt(int)) with a non-contiguous block
    circ.MakeNonContiguous(oneToTen);
    Assert.IsTrue(circ.Remove(2));
    Assert.IsTrue(circ.Remove(4));
    Assert.IsTrue(circ.Remove(9));
    Assert.IsTrue(circ.Remove(7));
    Assert.IsFalse(circ.Remove(50));

    // test Add(T)
    circ.Clear();
    foreach(int i in oneToTen) circ.Add(i);
    Compare(circ, oneToTen);

    // test Contains()
    foreach(int i in oneToTen) Assert.IsTrue(circ.Contains(i));
    Assert.IsFalse(circ.Contains(11));

    // test CopyTo(int, T[])
    int[] dest = new int[8];
    circ.CopyTo(1, dest, 0, 8);
    TestHelpers.AssertArrayEquals(dest, 2, 3, 4, 5, 6, 7, 8, 9);

    // test the GetEnumerable
    list.Clear();
    foreach(int i in circ) list.Add(i);
    Compare(circ, list);

    // test IndexOf() for the non-contiguous code path
    circ.Clear();
    circ.AddRange(oneToTen, 5, 5);
    circ.Insert(0, oneToTen, 0, 5);
    Assert.AreEqual(0, circ.IndexOf(1));
    Assert.AreEqual(5, circ.IndexOf(6));
    Assert.AreEqual(9, circ.IndexOf(10));
    Assert.AreEqual(-1, circ.IndexOf(1, 1, 9));
    Assert.AreEqual(1, circ.IndexOf(2, 1, 9));
    Assert.AreEqual(-1, circ.IndexOf(10, 0, 9));

    // test wraparound case for MoveTail()
    circ = new MyCircularList<int>(10);
    circ.Insert(0, 2);
    circ.Insert(0, 1);
    Assert.AreEqual(1, circ.RemoveFirst());

    // test RemoveFirst(int) and wraparound case for MoveHead(int)
    circ.Clear();
    circ.AddRange(1, 2, 3, 4, 5);
    circ.RemoveFirst(5);
    Assert.AreEqual(0, circ.Count);
    circ.AddRange(oneToTen);
    Compare(circ, oneToTen);

    // test wraparound case for MoveTail(int)
    circ.RemoveFirst(10);
    Assert.AreEqual(0, circ.Count);

    // test AddRange(T[], int, int)
    circ.AddRange(oneToTen, 1, 9);
    Compare(circ, 2, 3, 4, 5, 6, 7, 8, 9, 10);

    // test edge case for RemoveRange()
    circ.AddRange(6, 7, 8, 9, 10);
    circ.RemoveFirst(5);
    Compare(circ, 7, 8, 9, 10, 6, 7, 8, 9, 10);
    circ.RemoveRange(2, 7);
    Compare(circ, 7, 8);

    // test CopyTo() when the list is empty
    circ.Clear();
    circ.CopyTo(firstFive, 0);

    // test IsFull and get_Capacity
    circ.Clear();
    circ.Capacity = oneToTen.Length;
    Assert.AreEqual(oneToTen.Length, circ.Capacity);
    circ.AddRange(oneToTen);
    Assert.AreEqual(0, circ.AvailableSpace);
    Assert.IsTrue(circ.IsFull);
    circ.RemoveFirst();
    Assert.AreEqual(1, circ.AvailableSpace);
    Assert.IsFalse(circ.IsFull);

    // test wraparound case for MoveHead()
    circ.Clear();
    circ.Capacity = oneToTen.Length;
    circ.AddRange(oneToTen, 0, 9);
    circ.Add(10);
    Compare(circ, oneToTen);

    // test AddRange(IEnumerable<T>) in the case where Add(T) is called
    circ.Clear();
    circ.Capacity = 15;
    circ.AddRange(oneToTen);
    circ.RemoveFirst(9);
    circ.AddRange((IEnumerable<int>)oneToTen);
    Compare(circ, 10, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

    // test AddRange(IEnumerable<T>) in the case where MakeContiguous() is called
    list = new List<int>(oneToTen);
    list.AddRange(oneToTen);
    circ.Clear();
    circ.Capacity = 25;
    circ.AddRange(oneToTen);
    circ.RemoveFirst(9);
    circ.AddRange(list);
    Compare(circ, 10, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

    // test Insert(T[]) in the case where the free space is fragmented
    circ.Clear();
    circ.Capacity = 10;
    circ.AddRange(oneToTen, 0, 5);
    circ.RemoveFirst(4);
    circ.Insert(1, oneToTen, 0, 9);
    Compare(circ, 5, 1, 2, 3, 4, 5, 6, 7, 8, 9);

    // test CopyTo(T[]) in the case where the data is fragmented
    circ.Clear();
    circ.AddRange(oneToTen);
    circ.RemoveFirst(8);
    circ.AddRange(oneToTen, 0, 5);
    circ.CopyTo(0, firstFive, 0, 5);
    TestHelpers.AssertArrayEquals(firstFive, 9, 10, 1, 2, 3);

    // test IQueue<T> interface
    circ.Clear();
    IQueue<int> queue = circ;
    // ... test addition
    foreach(int i in oneToTen) queue.Add(i);
    Assert.AreEqual(oneToTen.Length, queue.Count);
    // ... test peeking and removal
    foreach(int i in oneToTen)
    {
      Assert.AreEqual(i, queue.Peek());
      Assert.AreEqual(i, queue.Dequeue());
    }
    Assert.AreEqual(0, queue.Count);

    TestHelpers.TestEnumerator(queue);
  }
  #endregion

  #region ExceptionTest
  [Test]
  public void ExceptionTest()
  {
    int[] array = new int[10];
    int[] items = { 1, 2, 3, 4, 5 };

    MyCircularList<int> circ = new MyCircularList<int>(5, false);
    Assert.IsFalse(circ.CanGrow);
    Assert.IsFalse(circ.IsReadOnly);

    circ.AddRange(items);

    circ.TestLogicalIndex(); // test bounds checks in GetLogicalIndex()
    circ.MakeNonContiguous(items);
    circ.TestLogicalIndex();

    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { circ.Capacity = 1; }); // test that Capacity cannot be set less than Count
    TestHelpers.TestException<ArgumentNullException>(delegate() { circ.AddRange((int[])null); }); // test null checks in AddRange
    TestHelpers.TestException<ArgumentNullException>(delegate() { circ.AddRange((IEnumerable<int>)null); }); // test null check in AddRange
    TestHelpers.TestException<ArgumentNullException>(delegate() { circ.Insert(0, null, 0, 0); });
    TestHelpers.TestException<ArgumentNullException>(delegate() { circ.CopyTo(0, null, 0, 0); });
    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { circ.CopyTo(array, 7); }); // test bounds check in CopyTo()
    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { circ.IndexOf(1, 2, 5); }); // test bounds check in IndexOf()
    circ.CopyTo(4, array, 0, 1); // test bounds check in CopyTo()

    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { int x = circ[10]; }); // test bounds check in GetRawIndex()
    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { circ.CopyTo(4, array, 0, 2); }); // test bounds check in CopyTo()
    TestHelpers.TestException<InvalidOperationException>(delegate() { circ.Add(10); }); // test that the list can't be overflowed
    Assert.AreEqual(5, circ.Count);

    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { circ.Insert(0, array, 0, -1); }); // test bounds check inside Insert()
    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { circ.Insert(2, array, 0, 2); }); // test that insertion except from the beginning or end is disallowed
    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { circ.RemoveFirst(-1); }); // test simple bounds check inside RemoveFirst(int)
    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { circ.RemoveLast(-1); }); // test simple bounds check inside RemoveLast(int)
    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { circ.RemoveRange(-1, 6); }); // test the bounds check inside RemoveRange() for the "remove from end" case
    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { circ.RemoveRange(2, 2); }); // test that removal except from the beginning or end is disallowed

    circ.Clear();
    circ.RemoveFirst(0); // test count == 0 case for removefirst

    TestHelpers.TestException<InvalidOperationException>(delegate() { circ.RemoveFirst(); }); // test that the list can't be underflowed
    TestHelpers.TestException<InvalidOperationException>(delegate() { circ.RemoveLast(); }); // test that the list can't be underflowed
    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate() { circ = new MyCircularList<int>(-5); }); // test the negative capacity check
  }
  #endregion

  #region ClearTest
  [Test]
  public void ClearTest()
  {
    MyCircularList<object> circ = new MyCircularList<object>();
    object[] oneToTen = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    circ.AddRange(oneToTen);
    circ.Clear();
    circ.VerifyCleared();

    circ.AddRange(oneToTen, 0, 5);
    circ.RemoveFirst(5);
    circ.VerifyCleared();

    circ.AddRange(oneToTen);
    circ.AddRange(oneToTen);
    circ.RemoveFirst();
    circ.VerifyCleared();
    circ.RemoveFirst();
    circ.RemoveFirst();
    circ.VerifyCleared();
    Compare<object>(circ, 4, 5, 6, 7, 8, 9, 10, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

    circ.RemoveFirst(16);
    circ.VerifyCleared();
    Compare<object>(circ, 10);

    circ.MakeNonContiguous(oneToTen);
    circ.MakeContiguous(); // this tests the LeftCount < RightCount path
    circ.VerifyCleared();
    Compare(circ, oneToTen);

    circ.MakeNonContiguous(oneToTen);
    circ.RemoveRange(circ.Count-1, 1);
    circ.MakeContiguous(); // this tests the RightCount >= LeftCount path
    circ.VerifyCleared();
    Compare<object>(circ, 1, 2, 3, 4, 5, 6, 7, 8, 9);

    circ.MakeNonContiguous(oneToTen);
    circ.RemoveFirst(2);
    circ.RemoveRange(circ.Count-2, 2);
    circ.MakeContiguous(); // this test the AvailableSpace >= Math.Max(LeftCount, RightCount) path
    Compare<object>(circ, 3, 4, 5, 6, 7, 8);

    circ.MakeNonContiguous(oneToTen);
    circ.RemoveRange(1, 9); // this tests the RemoveRange() case where the data is split
    circ.VerifyCleared();
    Compare<object>(circ, 1);

    circ.Clear();
    circ.AddRange(oneToTen);
    circ.RemoveRange(1, 9); // this tests the RemoveRange() case where the data is not split
    circ.VerifyCleared();
    Compare<object>(circ, 1);

    circ.MakeNonContiguous(oneToTen); // test RemoveAt()
    circ.RemoveAt(2);
    circ.VerifyCleared();
    circ.RemoveAt(circ.Count-2);
    circ.VerifyCleared();
    Compare<object>(circ, 1, 2, 4, 5, 6, 7, 8, 10);

    // test AddRange(IEnumerable<T>) in the case where MakeContiguous() is called
    List<object> list = new List<object>(oneToTen);
    list.AddRange(oneToTen);
    circ.Clear();
    circ.Capacity = 25;
    circ.AddRange(list);
    circ.AddRange(list.ToArray(), 0, 5);
    Assert.IsTrue(circ.IsFull);
    circ.RemoveFirst(20);
    circ.RemoveRange(circ.Count-1, 1);
    list.RemoveRange(0, 4);
    circ.AddRange(list);
    circ.VerifyCleared();
    Compare<object>(circ, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
  }
  #endregion

  static void Compare<T>(CircularList<T> circ, params T[] items)
  {
    Compare(circ, (IList<T>)items);
  }

  static void Compare<T>(CircularList<T> circ, IList<T> items)
  {
    Assert.AreEqual(items.Count, circ.Count);
    for(int i=0; i<items.Count; i++)
    {
      Assert.AreEqual(items[i], circ[i]);
    }
  }
}

} // namespace AdamMil.Collections.Tests