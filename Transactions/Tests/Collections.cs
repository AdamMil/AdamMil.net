using System;
using System.Collections.Generic;
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
  }

  [Test]
  public void T02_TestDictionary()
  {
    throw new NotImplementedException();
  }
}

} // namespace AdamMil.Transactions.Tests
