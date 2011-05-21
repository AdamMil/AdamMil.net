using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using AdamMil.Mathematics.Combinatorics;
using AdamMil.Mathematics.Random;
using AdamMil.Tests;
using AdamMil.Utilities;
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
    // test basic functionality
    Dictionary<string,int> real = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    real["a"] = 0;
    real["b"] = 1;
    real["z"] = 25;

    // test initializing a dictionary with items and using a custom comparer
    TransactionalDictionary<string, int> dict = new TransactionalDictionary<string, int>(real, StringComparer.OrdinalIgnoreCase);

    Assert.AreEqual(3, dict.Count);
    Assert.AreEqual(0, dict["a"]);
    Assert.AreEqual(0, dict["A"]);
    Assert.AreEqual(1, dict["b"]);
    Assert.AreEqual(25, dict["Z"]);
    Assert.IsTrue(dict.ContainsKey("B"));
    Assert.IsFalse(dict.ContainsKey("c"));
    TestHelpers.AssertArrayEquals(dict.Keys.Order().ToArray(), "a", "b", "z");
    TestHelpers.AssertArrayEquals(dict.Values.Order().ToArray(), 0, 1, 25);
    TestDictContains(dict, "a", 0, "b", 1, "z", 25);
    TestHelpers.TestException<KeyNotFoundException>(delegate { dict["x"].ToString(); });

    KeyValuePair<string, int>[] pairs = new KeyValuePair<string, int>[dict.Count];
    dict.CopyTo(pairs, 0);
    TestHelpers.AssertArrayEquals(pairs.OrderBy(p => p.Key).ToArray(), new KeyValuePair<string, int>("a", 0),
                                  new KeyValuePair<string, int>("b", 1), new KeyValuePair<string, int>("z", 25));

    int value;
    Assert.IsTrue(dict.TryGetValue("A", out value) && value == 0);
    Assert.IsTrue(dict.TryGetValue("B", out value) && value == 1);
    Assert.IsTrue(dict.TryGetValue("Z", out value) && value == 25);
    Assert.IsFalse(dict.TryGetValue("x", out value));
    Assert.IsFalse(dict.Remove("not here"));

    // test various modifications
    Action changeDict = delegate
    {
      Assert.IsTrue(dict.Remove("A"));
      Assert.AreEqual(2, dict.Count);
      Assert.IsFalse(dict.ContainsKey("a"));
      Assert.IsTrue(dict.ContainsKey("b"));
      Assert.IsTrue(dict.ContainsKey("z"));

      dict["b"] *= 2;
      dict["z"] *= 2;
      Assert.AreEqual(2, dict["b"]);
      Assert.AreEqual(50, dict["z"]);

      // make sure the underlying array gets enlarged during the write, so we can test that reverting an operation that caused
      // an enlargement doesn't corrupt the dictionary
      dict["c"] = 2;
      dict["d"] = 3;
      dict["e"] = 4;
      dict["f"] = 5;
      TestDictContains(dict, "b", 2, "c", 2, "d", 3, "e", 4, "f", 5, "z", 50);

      dict.Clear();
      Assert.AreEqual(0, dict.Count);
      Assert.IsFalse(dict.ContainsKey("b"));
      Assert.IsFalse(dict.ContainsKey("z"));

      dict["apple"] = 3;
      dict["pear"]  = 2;
      dict["cucumber"] = 0;
      dict["lime"]  = -2;

      TestHelpers.TestException<ArgumentException>(delegate { dict.Add("ApplE", 4); });
      TestDictContains(dict, "apple", 3, "cucumber", 0, "lime", -2, "pear", 2);
    };

    // ensure changes are reverted if the transaction isn't committed
    using(STMTransaction.Create()) changeDict();
    TestDictContains(dict, "a", 0, "b", 1, "z", 25);

    // test that changes are committed if the transaction is
    STM.Retry(changeDict);
    TestDictContains(dict, "apple", 3, "cucumber", 0, "lime", -2, "pear", 2);

    // test that System.Transactions transactions also work
    using(TransactionScope scope = new TransactionScope())
    {
      dict.Clear();
      dict["a"] = 10;
      dict["b"] = 20;
      dict["c"] = 30;
      // make sure other threads can't see the changes
      TestHelpers.RunInAnotherThread(delegate { TestDictContains(dict, "apple", 3, "cucumber", 0, "lime", -2, "pear", 2); });
      scope.Complete();
    }
    TestDictContains(dict, "a", 10, "b", 20, "c", 30);

    // perform some fuzzing
    for(int hashType=0; hashType<2; hashType++)
    {
      IEqualityComparer<int> hash = hashType == 0 ? (IEqualityComparer<int>)new GoodHash() : new CrapHash();
      RandomNumberGenerator rand = RandomNumberGenerator.CreateDefault(new uint[] { 42 });
      for(int count=0; count<100; count++)
      {
        TransactionalDictionary<int, int> stressDict = new TransactionalDictionary<int, int>(hash);

        int[] nums = new int[count];
        for(int i=0; i<nums.Length; i++) nums[i] = i;
        nums.RandomlyPermute(rand);

        for(int i=0; i<nums.Length; i++)
        {
          stressDict.Add(nums[i], i);
          for(int j=0; j<=i; j++) Assert.IsTrue(stressDict.TryGetValue(nums[j], out value) && value == j);
        }

        for(int i=nums.Length-1; i >= 0; i--)
        {
          Assert.IsTrue(stressDict.Remove(nums[i]));
          for(int j=i-1; j >= 0; j--) Assert.IsTrue(stressDict.TryGetValue(nums[j], out value) && value == j);
        }

        for(int i=0; i<nums.Length; i++)
        {
          stressDict.Add(nums[i], i);
          if(i > nums.Length/2) stressDict.Remove(nums[i-nums.Length/2]);

          for(int j=i > nums.Length/2 ? i-nums.Length/2+1 : 0; j<=i; j++)
          {
            Assert.IsTrue(stressDict.TryGetValue(nums[j], out value) && value == j);
          }
        }

        Dictionary<int, int> stressReal = new Dictionary<int, int>(stressDict);
        for(int i=1; i<=count; i++)
        {
          int a = rand.Next(count), b = rand.Next(count);
          stressDict[a] = i;
          stressDict.Remove(b);
          stressReal[a] = i;
          stressReal.Remove(b);
          Assert.AreEqual(stressReal.Count, stressDict.Count);
          foreach(KeyValuePair<int, int> pair in stressReal) Assert.AreEqual(pair.Value, stressDict[pair.Key]);
        }
      }
    }
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
      TestHelpers.RunInAnotherThread(delegate // make sure other threads can't see the changes
      {
        Assert.AreEqual(5, list.Count);
        Assert.AreEqual("pear", list[0]);
      });
      scope.Complete();
    }
    Assert.AreEqual(2, list.Count);
    Assert.AreEqual("ABC", list[0]);
    Assert.AreEqual("OneTwoThree", list[1]);
  }

  static void TestDictContains(TransactionalDictionary<string, int> dict, params object[] pairs)
  {
    if(pairs.Length % 2 != 0) throw new ArgumentException();
    KeyValuePair<string,int>[] array = new KeyValuePair<string,int>[pairs.Length/2];
    for(int i=0; i<array.Length; i++) array[i] = new KeyValuePair<string,int>((string)pairs[i*2], (int)pairs[i*2+1]);
    Assert.AreEqual(array.Length, dict.Count);
    TestHelpers.AssertArrayEquals(dict.OrderBy(p => p.Key).ToArray(), array);
  }

  #region CrapHash
  sealed class CrapHash : IEqualityComparer<int>
  {
    public bool Equals(int x, int y)
    {
      return x == y;
    }

    public int GetHashCode(int obj)
    {
      return obj.GetHashCode() / 3;
    }
  }
  #endregion

  #region GoodHash
  sealed class GoodHash : IEqualityComparer<int>
  {
    public bool Equals(int x, int y)
    {
      return x == y;
    }

    public int GetHashCode(int obj)
    {
      return obj;
    }
  }
  #endregion
}

} // namespace AdamMil.Transactions.Tests
