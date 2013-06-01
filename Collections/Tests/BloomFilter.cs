using System;
using System.Threading;
using AdamMil.Mathematics.Random;
using AdamMil.Utilities;
using NUnit.Framework;

namespace AdamMil.Collections.Tests
{

[TestFixture]
public class BloomFilterTest
{
  // NOTE: since the Bloom filter is a probabilistic data structure, this test will sometimes fail, but it has been tuned to fail
  // no more than about 20% of the time on average if the filter is working correctly. see the comments below for more details
  [Test]
  public void Test()
  {
    RandomNumberGenerator[] rands = new RandomNumberGenerator[SystemInformation.GetAvailableCpuThreads()];
    for(int i=0; i<rands.Length; i++) rands[i] = RandomNumberGenerator.CreateDefault();
    foreach(int itemCount in new int[] { 100, 1000, 10000, 100000, 1000000 })
    {
      // test false positive rates from 1/2 to 1/8196 (1/64 for 100 items, and 1/1024 for 1000 items)
      for(int rate=1; rate <= (itemCount <= 100 ? 6 : itemCount <= 1000 ? 10 : 13); rate++)
      {
        float desiredRate = 1f / (1<<rate);
        BloomFilter<ulong> f = new BloomFilter<ulong>(itemCount, desiredRate);
        for(int i=0; i<itemCount; i++)
        {
          ulong n = rands[0].NextUint64();
          f.Add(n); // we can't gainfully parallelize addition of items because the filter is not thread-safe
          Assert.IsTrue(f.PossiblyContains(n)); // make sure that there are no false negatives
        }

        // then make sure the false positive rate is acceptable
        int collisions = 0;
        Tasks.ParallelFor(0, itemCount*4, (i, end, info) =>
        {
          RandomNumberGenerator rand = rands[info.ThreadNumber];
          do
          {
            ulong n = rand.NextUint64();
            if(f.PossiblyContains(n)) Interlocked.Increment(ref collisions);
            i++;
          } while(i < end);
        }, itemCount <= 100000 ? 1 : SystemInformation.GetAvailableCpuThreads());
        double actualRate = (double)collisions / (itemCount*4);

        // now we'll apply a statistical significance test to see if the number of failures was more than we could expect
        // by chance. first we'll calculate some parameters about the Bloom filter using the same logic as the filter
        int hashCount = (int)(Math.Log(desiredRate) * -1.4426950408890 + 0.5);
        int bitCount = (int)Math.Round((double)-hashCount * itemCount / Math.Log(1 - Math.Pow(desiredRate, 1.0 / hashCount)) + 0.5);
        bitCount = bitCount + ((bitCount&31) == 0 ? 0 : 32-bitCount&31);

        // the number of distinct bits returned from the k different hash functions is k*(1-1/bitCount)^k
        double distinctBits = hashCount * Math.Pow(1-1.0/bitCount, hashCount);
        // the chance of a bit being set after n items was added is 1-(1-1/bitCount)^(kn)
        double p = 1 - Math.Pow(1-1.0/bitCount, hashCount*itemCount);
        // the chance of a collision is the chance that all of the bits tested are set, p^distinctBits
        p = Math.Pow(p, distinctBits);
        // then, the number of expected collisions is 4n*p (since we're doing 4n checks), and the standard deviation is given by
        // the binomial distribution: sqrt(4n*p*(1-p))
        double expected = 4*itemCount * p, stddev = Math.Sqrt(expected * (1-p));

        // if we want the unit test to fail only once per five runs on average if it's functioning normally, then the overall
        // probability of success should be 0.8. since we're doing 6+10+13*3 = 55 size/rate combinations, this means the chance
        // of an individual combination succeeding should be 1-(1-0.8)/55 = .9964. this corresponds to about 2.69 standard
        // deviations
        const double allowedDeviation = 2.69;
        System.Diagnostics.Debugger.Log(0, "",
          string.Format("{0} items: desired={1:f5}, actual={2} {3:f2}x ({4:f2} / {5:f2}[{6:f2}+{7:f2}] collisions)\n",
                        itemCount, desiredRate, actualRate, actualRate/desiredRate, collisions, expected+stddev*allowedDeviation,
                        expected, stddev*allowedDeviation));
        Assert.IsTrue(collisions-expected <= Math.Round(stddev*allowedDeviation),
                      "The false positive rate of " + actualRate.ToString() + " for " + itemCount +
                      " items was unacceptable. We desired a rate of approximately " + desiredRate.ToString() + ". Note that " +
                      "it is normal for this test to fail 20% of the time.");
      }
    }
  }
}

} // namespace AdamMil.Collections.Tests