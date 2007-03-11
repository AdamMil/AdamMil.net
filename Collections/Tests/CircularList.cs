using System;
using System.Collections.Generic;
using NUnit.Framework;
using AdamMil.Collections;

namespace AdamMil.Collections.Tests
{

[TestFixture]
public class CircularListTest
{
  #region BasicTests
  [Test]
  public void BasicTests()
  {
    int[] oneToTen = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    List<int> list = new List<int>(oneToTen);

    CircularList<int> circ = new CircularList<int>(40);
    Assert.IsTrue(circ.CanGrow);

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
    circ.AddRange((IEnumerable<int>)oneToTen);
    list.AddRange(oneToTen);
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

    // test RemoveFirst(T[])
    circ.Insert(0, oneToTen, 0, oneToTen.Length);
    int[] firstFive = new int[5];
    circ.RemoveFirst(firstFive, 0, 5);
    Compare(firstFive, 1, 2, 3, 4, 5);
    Compare(circ, 6, 7, 8, 9, 10);

    // test CopyTo
    circ.CopyTo(firstFive, 0);
    Compare(firstFive, 6, 7, 8, 9, 10);

    // test RemoveRange()
    circ.RemoveRange(0, 2);
    Compare(circ, 8, 9, 10);
    circ.RemoveRange(circ.Count-2, 2);
    Compare(circ, 8);

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
    circ.AddRange(1, 2, 3, 4, 5);
    circ.RemoveAt(2);
    Compare(circ, 1, 2, 4, 5);
    circ.RemoveAt(0);
    Compare(circ, 2, 4, 5);
    circ.RemoveAt(circ.Count-1);
    Compare(circ, 2, 4);

    // test Remove(T)
    circ.Clear();
    circ.AddRange(oneToTen);
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
    Compare(dest, 2, 3, 4, 5, 6, 7, 8, 9);

    // test the GetEnumerable
    list.Clear();
    foreach(int i in circ) list.Add(i);
    Compare(circ, list);

    // test IndexOf() for the non-contiguous code path
    circ.Clear();
    circ.AddRange(6, 7, 8, 9, 10);
    circ.Insert(0, new int[] { 1, 2, 3, 4, 5 }, 0, 5);
    Assert.AreEqual(0, circ.IndexOf(1));
    Assert.AreEqual(5, circ.IndexOf(6));

    // test edge case for MoveTail()
    circ = new CircularList<int>(10);
    circ.Insert(0, 2);
    circ.Insert(0, 1);
    Assert.AreEqual(1, circ.RemoveFirst());

    // test edge case for MoveHead(int) and RemoveFirst(int)
    circ.Clear();
    circ.AddRange(1, 2, 3, 4, 5);
    circ.RemoveFirst(5);
    Assert.AreEqual(0, circ.Count);
    circ.AddRange(oneToTen);
    Compare(circ, oneToTen);

    // test edge case for MoveTail(int)
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
  }
  #endregion

  #region ExceptionTests
  [Test]
  public void ExceptionTests()
  {
    int[] array = new int[10];

    CircularList<int> circ = new CircularList<int>(5, false);
    circ.AddRange(1, 2, 3, 4, 5);

    bool threw = false;
    try { circ.CopyTo(array, 7); }
    catch(ArgumentOutOfRangeException) { threw = true; }

    circ.CopyTo(4, array, 0, 1);

    threw = false;
    try { int x = circ[10]; }
    catch(ArgumentOutOfRangeException) { threw = true; }
    Assert.IsTrue(threw);

    threw = false;
    try { circ.CopyTo(4, array, 0, 2); }
    catch(ArgumentOutOfRangeException) { threw = true; }
    Assert.IsTrue(threw);

    threw = false;
    try { circ.Add(10); }
    catch(InvalidOperationException) { threw = true; }
    Assert.IsTrue(threw);

    threw = false;
    try { circ.Insert(0, array, 0, -1); }
    catch(ArgumentOutOfRangeException) { threw = true; }
    Assert.IsTrue(threw);

    threw = false;
    try { circ.Insert(2, array, 0, 2); }
    catch(ArgumentOutOfRangeException) { threw = true; }
    Assert.IsTrue(threw);

    circ.Clear();
    threw = false;
    try { circ.RemoveFirst(); }
    catch(InvalidOperationException) { threw = true; }
    Assert.IsTrue(threw);

    threw = false;
    try { circ.RemoveFirst(-1); }
    catch(ArgumentOutOfRangeException) { threw = true; }
    Assert.IsTrue(threw);

    circ.RemoveFirst(0);
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

  static void Compare<T>(T[] a, params T[] b)
  {
    Assert.AreEqual(a.Length, b.Length);
    for(int i=0; i<a.Length; i++)
    {
      Assert.AreEqual(b[i], a[i]);
    }
  }
}

} // namespace AdamMil.Collections.Tests