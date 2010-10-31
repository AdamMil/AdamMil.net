using System;
using System.IO;
using NUnit.Framework;
using AdamMil.IO;

namespace AdamMil.IO.Tests
{

// TODO: add tests for strings and chars

[TestFixture]
public class BinaryReaderWriterTest
{
  [Test]
  public void Test()
  {
    MemoryStream ms = new MemoryStream();
    for(int i=0; i<500; i++)
    {
      ms.WriteByte((byte)(i%256));
    }
    
    ms.Position = 50;
    using(BinaryReader br = new BinaryReader(ms))
    {
      Assert.AreEqual(50, br.ReadByte()); // make sure the reader starts reading from the right place
      Assert.AreEqual(51, br.ReadByte());
    }
    Assert.AreEqual(ms.Position, 52); // make sure that the reader puts the stream position back where it should be
  }

  [Test]
  public void TestEncoded()
  {
    MemoryStream ms = new MemoryStream();
    int[] testInts = { 1, -1, 100, -100, 10000, -10000, 1000000, -1000000, int.MaxValue, int.MinValue };
    uint[] testUints = { 1, 1000, 50000, 1000000, uint.MaxValue };

    using(BinaryWriter bw = new BinaryWriter(ms))
    {
      foreach(int i in testInts)
      {
        bw.WriteEncoded(i);
        bw.WriteEncoded((long)i);
      }
      foreach(uint i in testUints)
      {
        bw.WriteEncoded(i);
        bw.WriteEncoded((ulong)i);
      }
      bw.WriteEncoded(ulong.MaxValue);
      bw.WriteEncoded(long.MaxValue);
      bw.WriteEncoded(long.MinValue);
    }

    ms.Position = 0;
    using(BinaryReader br = new BinaryReader(ms))
    {
      foreach(int i in testInts)
      {
        Assert.AreEqual(i, br.ReadEncodedInt32());
        Assert.AreEqual((long)i, br.ReadEncodedInt64());
      }
      foreach(uint i in testUints)
      {
        Assert.AreEqual(i, br.ReadEncodedUInt32());
        Assert.AreEqual((ulong)i, br.ReadEncodedUInt64());
      }
      Assert.AreEqual(ulong.MaxValue, br.ReadEncodedUInt64());
      Assert.AreEqual(long.MaxValue, br.ReadEncodedInt64());
      Assert.AreEqual(long.MinValue, br.ReadEncodedInt64());
    }
  }

  [Test]
  public void TestArray()
  {
    byte[] array = new byte[100];
    for(int i=0; i<array.Length; i++) array[i] = (byte)i;

    using(BinaryWriter bw = new BinaryWriter(array, 20, 60)) // write to indices 20-79
    {
      for(int i=0; i<60/4; i++) bw.Write(i);
      Assert.AreEqual(60, bw.Position);
    }

    using(BinaryReader br = new BinaryReader(array, 20, 60))
    {
      for(int i=0; i<60/4; i++) Assert.AreEqual(i, br.ReadInt32());
      Assert.AreEqual(60, br.Position);
    }

    for(int i=0; i<20; i++) Assert.AreEqual(i, array[i]);
    for(int i=80; i<array.Length; i++) Assert.AreEqual(i, array[i]);
  }
}

} // namespace AdamMil.IO.Tests