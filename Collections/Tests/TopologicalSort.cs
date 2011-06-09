using System.Collections.Generic;
using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Collections.Tests
{

[TestFixture]
public class TopologicalSortTest
{
  [Test]
  public void Test()
  {
    Dictionary<string,List<string>> dependencies = new Dictionary<string,List<string>>()
    {
      { "tophat", new List<string>() },
      { "bowtie", new List<string>() {"shirt"} },
      { "socks", new List<string>() },
      { "pocketwatch", new List<string>() {"vest"} },
      { "vest", new List<string>() {"shirt"} },
      { "shirt", new List<string>() },
      { "shoes", new List<string>() {"trousers", "socks" } },
      { "cufflinks", new List<string>() {"shirt"} },
      { "gloves", new List<string>() },
      { "tailcoat", new List<string>() {"vest"} },
      { "underpants", new List<string>() },
      { "trousers", new List<string>() {"underpants"} }
    };

    // make sure the CheckResults method detects bad orders
    List<string> badOrder = new List<string>()
    {
      "trousers", "underpants", "tailcoat", "gloves", "cufflinks", "shoes", "shirt", "vest", "pocketwatch",
      "socks", "bowtie", "tophat"
    };
    TestHelpers.TestException<AssertionException>(delegate { CheckResults(badOrder, badOrder, dependencies); });

    // try make sure all the variants work
    List<string> items = new List<string>(dependencies.Keys);
    CheckResults(items, items.OrderTopologically(s => dependencies[s]), dependencies);
    CheckResults(items, items.GetTopologicalSort(s => dependencies[s]), dependencies);

    items.TopologicalSort(s => dependencies[s]);
    CheckResults(new List<string>(dependencies.Keys), items, dependencies);

    items = new List<string>(dependencies.Keys);
    CheckResults(items, items.GetTopologicalSortSets(s => dependencies[s]), dependencies);

    // make sure the cycle detection works
    Dictionary<string, List<string>> depsWithCycle = new Dictionary<string, List<string>>()
    {
      { "a", new List<string>() { "b" } },
      { "b", new List<string>() { "c" } },
      { "c", new List<string>() { "a" } },
    };
    TestHelpers.TestException<CycleException>(delegate
    {
      new List<string>(depsWithCycle.Keys).TopologicalSort(s => depsWithCycle[s]);
    });
    TestHelpers.TestException<CycleException>(delegate
    {
      new List<string>(depsWithCycle.Keys).GetTopologicalSortSets(s => depsWithCycle[s]);
    });
  }

  static void AssertSeen(string item, HashSet<string> seen, Dictionary<string, List<string>> dependencies)
  {
    Assert.IsTrue(seen.Contains(item));
    foreach(string dependency in dependencies[item]) AssertSeen(dependency, seen, dependencies);
  }

  static void CheckResults(List<string> originalItems, IEnumerable<string> items, Dictionary<string, List<string>> dependencies)
  {
    HashSet<string> seen = new HashSet<string>();

    foreach(string item in items)
    {
      foreach(string dependency in dependencies[item]) AssertSeen(dependency, seen, dependencies);
      Assert.IsTrue(seen.Add(item));
    }

    Assert.AreEqual(originalItems.Count, seen.Count);
  }

  static void CheckResults(IList<string> originalItems, List<List<string>> sets, Dictionary<string, List<string>> dependencies)
  {
    HashSet<string> seen = new HashSet<string>();

    foreach(List<string> set in sets)
    {
      // check the dependencies first to make sure no item in the set depends on any other in the set
      foreach(string item in set)
      {
        foreach(string dependency in dependencies[item]) AssertSeen(dependency, seen, dependencies);
      }
      // then mark the items seen
      foreach(string item in set) Assert.IsTrue(seen.Add(item));
    }

    Assert.AreEqual(originalItems.Count, seen.Count);
  }
}

} // namespace AdamMil.Collections.Tests