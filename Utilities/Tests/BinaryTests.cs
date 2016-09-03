using System.Text;
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

  [Test]
  public void T03_TestCRC32()
  {
    const string dogString = "The quick brown fox jumps over the lazy dog";
    byte[] checkBytes = Encoding.ASCII.GetBytes("123456789"), dogBytes = Encoding.ASCII.GetBytes(dogString);
    Assert.AreEqual(0, CRC32.Default.Compute(new byte[0]));
    Assert.AreEqual(unchecked((int)0xcbf43926), CRC32.Default.Compute(checkBytes));
    Assert.AreEqual(0x414fa339, CRC32.Default.Compute(dogBytes));
    Assert.AreEqual(0x414fa339, CRC32.Default.Compute(Encoding.ASCII.GetBytes(" " + dogString), 1, dogString.Length)); // test a misaligned pointer
    Assert.AreEqual(0x414fa339, CRC32.Default.Compute(dogBytes, 21, dogBytes.Length-21, CRC32.Default.Compute(dogBytes, 0, 21))); // test continuing a CRC
    Assert.AreEqual(unchecked((int)0xe3069283), CRC32.CRC32C.Compute(checkBytes));
    Assert.AreEqual(0x22620404, CRC32.CRC32C.Compute(dogBytes));
  }

  [Test]
  public void T04_TestByteSwaps()
  {
    Assert.AreEqual(unchecked((short)0x8967), BinaryUtility.ByteSwap((short)0x6789));
    Assert.AreEqual((ushort)0x3412, BinaryUtility.ByteSwap((ushort)0x1234));
    Assert.AreEqual(unchecked((int)0x89674523), BinaryUtility.ByteSwap((int)0x23456789));
    Assert.AreEqual((uint)0x78563412, BinaryUtility.ByteSwap((uint)0x12345678));
    Assert.AreEqual(unchecked((int)0x89674523), BinaryUtility.ByteSwap((int)0x23456789));
    Assert.AreEqual(unchecked((long)0xEFCDAB9078563412), BinaryUtility.ByteSwap((long)0x1234567890ABCDEF));
    Assert.AreEqual((ulong)0xEFCDAB9078563412, BinaryUtility.ByteSwap((ulong)0x1234567890ABCDEF));
  }

  static int CountBits(uint v)
  {
    int count = 0;
    for(; v != 0; v &= v-1) count++;
    return count;
  }
}

} // namespace AdamMil.Utilities.Tests
