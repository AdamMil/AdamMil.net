using System;
using AdamMil.Mathematics.Combinatorics;
using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Mathematics.Tests
{
  [TestFixture]
  public class Combinatorics
  {
    [Test]
    public void TestCombinatorics()
    {
      Assert.AreEqual((Integer)120, Combinations.CountCombinations(3, 10));
      Assert.AreEqual((Integer)462, Combinations.CountCombinations(6, 11));
      Assert.AreEqual((Integer)3628800, Combinations.CountPermutations(10));
      Assert.AreEqual(Integer.Parse("30414093201713378043612608166064768844377641568960512000000000000"), Combinations.CountPermutations(50));
      TestHelpers.TestException<ArgumentOutOfRangeException>(() => Combinations.CountPermutations(0));
    }
  }
}
