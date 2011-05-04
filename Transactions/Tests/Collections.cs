using System;
using System.Collections.Generic;
using System.Transactions;
using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Transactions.Tests
{

[TestFixture]
public class CollectionTests
{
  // TODO: test contention
  [Test]
  public void T01_TestArray()
  {
    // test the constructor that takes a count
    Assert.AreEqual(2, new TransactionalArray<int>(2).Count);

    // test the constructor that takes a list of items
    TransactionalArray<int> array = new TransactionalArray<int>(new int[] { 0, 1, 2, 3, 4 });
    Assert.AreEqual(5, array.Count);
    for(int i=0; i<5; i++) Assert.AreEqual(i, array[i]);

    // ensure that the array can be changed outside a transaction
    array[0] = 5;
    Assert.AreEqual(5, array[0]);
    array[0] = 0;

    using(STMTransaction tx = STMTransaction.Create())
    {
      // test indexing
      array[2] = 42;
      Assert.AreEqual(42, array[2]);

      // test IndexOf() and Contains()
      Assert.AreEqual(2, array.IndexOf(42));
      Assert.IsTrue(array.Contains(42));
      Assert.AreEqual(-1, array.IndexOf(55));
      Assert.IsFalse(array.Contains(55));

      // test CopyTo()
      int[] extArray = new int[5];
      array.CopyTo(extArray, 0);
      for(int i=0; i<5; i++) Assert.AreEqual(i == 2 ? 42 : i, extArray[i]);

      // test the enumerator
      List<int> list = new List<int>();
      foreach(int i in array) list.Add(i);
      Assert.AreEqual(5, list.Count);
      for(int i=0; i<5; i++) Assert.AreEqual(i == 2 ? 42 : i, list[i]);

      // test isolation
      TestHelpers.RunInAnotherThread(delegate { Assert.AreEqual(2, array[2]); });
      tx.Commit();
    }
    Assert.AreEqual(42, array[2]);

    // test that operations compose transactionally
    using(STMTransaction otx = STMTransaction.Create())
    {
      STM.Retry(delegate { array[2] = 2; });
      STM.Retry(delegate { array[0] = -1; });
      Assert.AreEqual(-1, array[0]);
      Assert.AreEqual(2, array[2]);
      TestHelpers.RunInAnotherThread(delegate
      {
        Assert.AreEqual(0, array[0]);
        Assert.AreEqual(42, array[2]);
      });
      otx.Commit();
    }
    Assert.AreEqual(-1, array[0]);
    Assert.AreEqual(2, array[2]);

    // test array enlarging
    array.Enlarge(0); // test that the array can't be shrunk
    Assert.AreEqual(5, array.Count);
    array.Enlarge(6); // test that it can be enlarged
    Assert.AreEqual(6, array.Count);
    Assert.AreEqual(-1, array[0]); // test that enlarging it doesn't change any values
    Assert.AreEqual(2, array[2]);
  }

  [Test]
  public void T02_TestDictionary()
  {
    throw new NotImplementedException();
  }

  [Test]
  public void T03_TestList()
  {
    // test initializing a list with items and using a custom comparer
    TransactionalList<string> list = new TransactionalList<string>(new string[] { "APPLE", "pear", "apple" },
                                                                   StringComparer.OrdinalIgnoreCase);
    Assert.AreEqual(3, list.Count);
    Assert.AreEqual(0, list.IndexOf("APPLE"));
    Assert.AreEqual(0, list.IndexOf("apple"));
    Assert.AreEqual(0, list.IndexOf("ApPlE"));
    Assert.IsTrue(list.Contains("Pear"));

    // test that it can be changed outside a transaction
    list[0] = "foo";
    list.Insert(0, "x");
    Assert.AreEqual("x", list[0]);
    Assert.AreEqual("foo", list[1]);
    Assert.AreEqual(4, list.Count);
    list.Remove("x");
    Assert.AreEqual(3, list.Count);

    // test various modifications
    Action changeList = delegate
    {
      list[0] = null;
      Assert.AreEqual(null, list[0]);
      Assert.AreEqual(2, list.IndexOf("APPLE"));
      Assert.AreEqual(3, list.Count);
      list.RemoveAt(0);
      Assert.AreEqual(2, list.Count);
      Assert.AreEqual("pear", list[0]);
      Assert.AreEqual("apple", list[1]);
      Assert.IsTrue(list.Remove("APPLE"));
      Assert.AreEqual(1, list.Count);
      Assert.AreEqual("pear", list[0]);
      list.Insert(1, "pineapple");
      Assert.AreEqual(2, list.Count);
      Assert.AreEqual("pineapple", list[1]);
      list.AddRange(new string[] { "y", "z" });
      Assert.AreEqual(4, list.Count);
      list.Insert(list.Count-2, "x");
      Assert.AreEqual(5, list.Count);
      TestHelpers.AssertArrayEquals(list.ToArray(), "pear", "pineapple", "x", "y", "z");
    };

    // test that changes are reverted if the transaction isn't committed
    using(STMTransaction.Create()) changeList();
    TestHelpers.AssertArrayEquals(list.ToArray(), "foo", "pear", "apple");

    // test that changes are committed if the transaction is
    STM.Retry(changeList);
    TestHelpers.AssertArrayEquals(list.ToArray(), "pear", "pineapple", "x", "y", "z");

    // test that System.Transactions transactions also work
    using(TransactionScope scope = new TransactionScope())
    {
      list.Clear();
      list.Add("ABC");
      list.Add("OneTwoThree");
      TestHelpers.RunInAnotherThread(delegate { Assert.AreEqual(5, list.Count); }); // make sure another thread can't see changes
      scope.Complete();
    }
    Assert.AreEqual(2, list.Count);
    Assert.AreEqual("ABC", list[0]);
    Assert.AreEqual("OneTwoThree", list[1]);
  }
}

} // namespace AdamMil.Transactions.Tests
