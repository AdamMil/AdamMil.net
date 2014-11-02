using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Utilities.Tests
{

[TestFixture]
public class BinaryTests
{
  [Test]
  public void T01_ToAndFromHex()
  {
    byte[] data = new byte[] { 8, 10, 250, 0, 255, 20, 48, 58, 64, 128, 170, 30, 180 };
    Assert.AreEqual("080AFA00FF14303A4080AA1EB4", BinaryUtility.ToHex(data).ToUpperInvariant());
    TestHelpers.AssertArrayEquals(data, BinaryUtility.ParseHex(BinaryUtility.ToHex(data)));
  }

  [Test]
  public void T02_TestBitCounts()
  {
    // test methods to count leading and trailing zeros
    foreach(uint seed in new uint[] { 0xFFFFFFFF, 0x8a23b439, 0x80000001, 0xA3829f27 }) // each seed has the high and low bit set
    {
      uint f = seed, b = seed;
      for(int i=0; i<=32; f >>= 1, b <<= 1, i++)
      {
        Assert.AreEqual(i, BinaryUtility.CountLeadingZeros(f));
        Assert.AreEqual(i, BinaryUtility.CountTrailingZeros(b));
        Assert.AreEqual(CountBits(f), BinaryUtility.CountBits(f));
        Assert.AreEqual(CountBits(b), BinaryUtility.CountBits(b));
        Assert.AreEqual(CountBits(f)+CountBits(b), BinaryUtility.CountBits(((ulong)f<<32)|b));
      }
    }
  }

  static int CountBits(uint v)
  {
    int count = 0;
    for(; v != 0; v &= v-1) count++;
    return count;
  }
}

} // namespace AdamMil.Utilities.Tests
