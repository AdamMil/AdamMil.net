using System;
using System.Collections.Generic;
using System.Linq;
using AdamMil.Mathematics.Random;
using NUnit.Framework;

// TODO: add tests for normal and exponential distributions

namespace AdamMil.Mathematics.Tests
{
  [TestFixture]
  public class RandomTests
  {
    [Test]
    public void Basics() // this test is expected to fail a bit less than 10% of the time
    {
      var rng = RandomNumberGenerator.CreateDefault();
      for(int i=0; i<500; i++) Assert.IsTrue((uint)rng.Next(17) < 17);
      for(int i=0; i<500; i++) Assert.IsTrue((uint)(rng.Next(-17, 17)+17) <= 34);
      for(int i=0; i<500; i++) Assert.AreEqual(17, rng.Next(17, 17));

      TestDistribution(2, () => rng.NextBoolean() ? 1 : 0);
      TestDistribution(6, () => rng.Next(10, 15)-10);
      TestDistribution(32, () => (int)rng.NextBits(5));
      TestDistribution(5000, () => rng.Next(5000));
      TestDistribution(1000, () => (int)(rng.NextDouble()*1000));
      TestDistribution(1000, () => (int)(rng.NextFloat()*1000));
      TestDistribution(5000, () => (int)(rng.Next(5000000000L) / 1000000));
      TestDistribution(5000, () => (int)(rng.Next(500000000000L) / 100000000));
      TestDistribution(2199, () => (int)(rng.NextBits64(41) * 9.9998942459933459758758544921877e-10));
    }

    static void TestDistribution(int bucketCount, Func<int> chooseBucket)
    {
      const int PerBucket = 200;
      int iterations = bucketCount * PerBucket;
      int[] buckets = new int[bucketCount];
      for(int i = 0; i < iterations; i++) buckets[chooseBucket()]++;
      if(bucketCount <= 100)
      {
        Bounds b = binomialMap[bucketCount];
        for(int i = 0; i < buckets.Length; i++) Assert.IsTrue(buckets[i] >= b.Lo && buckets[i] <= b.Hi);
      }
      else
      {
        double chiSq = 0;
        for(int i = 0; i < buckets.Length; i++)
        {
          int d = buckets[i] - PerBucket;
          chiSq += d*d * (1.0/PerBucket);
        }
        Assert.LessOrEqual(chiSq, chiSquareMap[bucketCount]);
      }
    }

    struct Bounds
    {
      public Bounds(int lo, int hi) { Lo = lo; Hi = hi; }
      public int Lo, Hi;
    }

    static Dictionary<int, Bounds> binomialMap = new Dictionary<int, Bounds>() { { 2, new Bounds(177, 223) }, { 6, new Bounds(166, 234) }, { 32, new Bounds(157, 247) } };
    static Dictionary<int, double> chiSquareMap = new Dictionary<int, double>() { { 1000, 1183.18 }, { 2199, 2479.48 }, { 5000, 5437.4 } };
  }
}
