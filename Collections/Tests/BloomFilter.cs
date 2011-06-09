using NUnit.Framework;

namespace AdamMil.Collections.Tests
{

[TestFixture]
public class BloomFilterTest
{
  [Test]
  public void Test()
  {
    foreach(int itemCount in new int[] { 100, 1000, 10000, 100000 })
    {
      for(int rate=1; rate <= 13; rate++) // try rates between 1/2 and 1/8192
      {
        // first make sure that there are no false negatives
        float desiredRate = 1f / (1<<rate);
        BloomFilter<int> f = new BloomFilter<int>(itemCount, desiredRate);
        for(int i=0; i<itemCount; i++)
        {
          f.Add(i);
          Assert.IsTrue(f.PossiblyContains(i));
        }

        // then make sure the false positive rate is acceptable
        int collisions = 0;
        for(int i=itemCount, end=itemCount*5; i<end; i++)
        {
          if(f.PossiblyContains(i)) collisions++;
        }
        float actualRate = (float)collisions / (itemCount*4);
        Assert.IsTrue(actualRate < desiredRate*3,
                      "The false positive rate of " + actualRate.ToString() + " for " + itemCount +
                      " items was unacceptable. We desired a rate of approximately " + desiredRate.ToString());
      }
    }
  }
}

} // namespace AdamMil.Collections.Tests