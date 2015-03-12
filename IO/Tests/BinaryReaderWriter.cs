using System;
using System.IO;
using NUnit.Framework;
using AdamMil.IO;
using AdamMil.Tests;
using AdamMil.Utilities;

namespace AdamMil.IO.Tests
{

[TestFixture]
public class BinaryReaderWriterTest
{
  [Test]
  public void TestBasics()
  {
    MemoryStream ms = new MemoryStream();
    for(int i=0; i<500; i++)
    {
      ms.WriteByte((byte)(i%256));
    }

    ms.Position = 50;
    using(BinaryReader br = new BinaryReader(ms, false))
    {
      Assert.AreEqual(50, br.ReadByte()); // make sure the reader starts reading from the right place
      Assert.AreEqual(51, br.ReadByte());
    }
    Assert.AreEqual(ms.Position, 52); // make sure that the reader puts the stream position back where it should be

    for(int i=0; i<2; i++)
    {
      DateTime dt1 = new DateTime(1999, 12, 31, 1, 2, 3, DateTimeKind.Utc), dt2 = DateTime.Now;
      Guid guid = Guid.NewGuid();
      ms.SetLength(0);
      using(BinaryWriter w = new BinaryWriter(ms, false))
      {
        w.LittleEndian = i == 0;
        w.Write(true);
        w.Write((byte)42);
        w.Write(new byte[] { 1, 2, 3 });
        w.Write(dt1);
        w.Write(new DateTime[] { dt2, dt1 });
        w.Write(3.14159m);
        w.Write(new decimal[] { 2000.77777m, -10000000000000.55m });
        w.Write(Math.PI);
        w.Write(new double[] { Math.E, Math.Sqrt(2) });
        w.Write((float)Math.PI);
        w.Write(new float[] { (float)Math.E, 140000.22f });
        w.Write(guid);
        w.Write(new Guid[] { Guid.Empty, guid });
        w.Write(-42);
        w.Write(new int[] { int.MaxValue, int.MinValue });
        w.Write(-500L);
        w.Write(new long[] { long.MaxValue, long.MinValue });
        w.Write((sbyte)-50);
        w.Write(new sbyte[] { -100, 100 });
        w.Write((short)-5000);
        w.Write(new short[] { -30000, 30000 });
        w.Write((ushort)50000);
        w.Write(new ushort[] { 70, ushort.MaxValue });
        w.Write(3000000000u);
        w.Write(new uint[] { 10000, uint.MaxValue });
        w.Write(9000000000ul);
        w.Write(new ulong[] { 20000, ulong.MaxValue });
      }

      ms.Position = 0;
      using(BinaryReader r = new BinaryReader(ms, false))
      {
        r.LittleEndian = i == 0;
        Assert.AreEqual(true, r.ReadBoolean());
        Assert.AreEqual((byte)42, r.ReadByte());
        TestHelpers.AssertArrayEquals(r.ReadBytes(3), new byte[] { 1, 2, 3 });
        Assert.AreEqual(dt1, r.ReadDateTime());
        TestHelpers.AssertArrayEquals(r.ReadDateTimes(2), new DateTime[] { dt2, dt1 });
        Assert.AreEqual(3.14159m, r.ReadDecimal());
        TestHelpers.AssertArrayEquals(r.ReadDecimals(2), new decimal[] { 2000.77777m, -10000000000000.55m });
        Assert.AreEqual(Math.PI, r.ReadDouble());
        TestHelpers.AssertArrayEquals(r.ReadDoubles(2), new double[] { Math.E, Math.Sqrt(2) });
        Assert.AreEqual((float)Math.PI, r.ReadSingle());
        TestHelpers.AssertArrayEquals(r.ReadSingles(2), new float[] { (float)Math.E, 140000.22f });
        Assert.AreEqual(guid, r.ReadGuid());
        TestHelpers.AssertArrayEquals(r.ReadGuids(2), new Guid[] { Guid.Empty, guid });
        Assert.AreEqual(-42, r.ReadInt32());
        TestHelpers.AssertArrayEquals(r.ReadInt32s(2), new int[] { int.MaxValue, int.MinValue });
        Assert.AreEqual(-500L, r.ReadInt64());
        TestHelpers.AssertArrayEquals(r.ReadInt64s(2), new long[] { long.MaxValue, long.MinValue });
        Assert.AreEqual((sbyte)-50, r.ReadSByte());
        TestHelpers.AssertArrayEquals(r.ReadSBytes(2), new sbyte[] { -100, 100 });
        Assert.AreEqual((short)-5000, r.ReadInt16());
        TestHelpers.AssertArrayEquals(r.ReadInt16s(2), new short[] { -30000, 30000 });
        Assert.AreEqual((ushort)50000, r.ReadUInt16());
        TestHelpers.AssertArrayEquals(r.ReadUInt16s(2), new ushort[] { 70, ushort.MaxValue });
        Assert.AreEqual(3000000000u, r.ReadUInt32());
        TestHelpers.AssertArrayEquals(r.ReadUInt32s(2), new uint[] { 10000, uint.MaxValue });
        Assert.AreEqual(9000000000ul, r.ReadUInt64());
        TestHelpers.AssertArrayEquals(r.ReadUInt64s(2), new ulong[] { 20000, ulong.MaxValue });
      }
    }
  }

  [Test]
  public void TestCharsAndStrings()
  {
    MemoryStream ms = new MemoryStream();
    using(BinaryWriter bw = new BinaryWriter(ms, false))
    {
      bw.Write('H');
      bw.Write('吉');
      bw.WriteStringWithLength("Hello, world.吉吉");
      bw.WriteStringWithLength(new string('吉', 4096)); // write a really long string
      bw.Write(new char[] { 'H', 'o', 'p', 'e', '吉' });
    }

    ms.Position = 0;
    using(BinaryReader br = new BinaryReader(ms, false))
    {
      Assert.AreEqual('H', br.ReadChar());
      Assert.AreEqual('吉', br.ReadChar());
      Assert.AreEqual("Hello, world.吉吉", br.ReadStringWithLength());
      Assert.AreEqual(new string('吉', 4096), br.ReadStringWithLength());
      TestHelpers.AssertArrayEquals(new char[] { 'H', 'o', 'p', 'e', '吉' }, br.ReadChars(5));
    }
  }

  [Test]
  public void TestEncodedInts()
  {
    MemoryStream ms = new MemoryStream();
    int[] testInts = { 1, -1, 100, -100, 10000, -10000, 1000000, -1000000, int.MaxValue, int.MinValue };
    uint[] testUints = { 1, 1000, 50000, 1000000, uint.MaxValue };

    using(BinaryWriter bw = new BinaryWriter(ms, false))
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

  /*[Test]
  public void TestSerialization()
  {
    MemoryStream ms = new MemoryStream();
    for(int i=0; i<2; i++)
    {
      DateTime dt1 = new DateTime(1999, 12, 31, 1, 2, 3, DateTimeKind.Utc), dt2 = DateTime.Now;
      Guid guid = Guid.NewGuid();
      ms.SetLength(0);
      using(BinaryWriter w = new BinaryWriter(ms, false))
      {
        w.LittleEndian = i == 0;
        w.WriteValueWithType(null);
        w.WriteValueWithType(DBNull.Value);
        w.WriteValueWithType(false);
        w.WriteValueWithType(true);
        w.WriteValueWithType((byte)42);
        w.WriteValueWithType(new byte[] { 1, 2, 3 });
        w.WriteValueWithType(dt1);
        w.WriteValueWithType(new DateTime[] { dt2, dt1 });
        w.WriteValueWithType(3.14159m);
        w.WriteValueWithType(new decimal[] { 2000.77777m, -10000000000000.55m });
        w.WriteValueWithType(Math.PI);
        w.WriteValueWithType(new double[] { Math.E, Math.Sqrt(2) });
        w.WriteValueWithType((float)Math.PI);
        w.WriteValueWithType(new float[] { (float)Math.E, 140000.22f });
        w.WriteValueWithType(guid);
        w.WriteValueWithType(new Guid[] { Guid.Empty, guid });
        w.WriteValueWithType(-42);
        w.WriteValueWithType(new int[] { int.MaxValue, int.MinValue });
        w.WriteValueWithType(-500L);
        w.WriteValueWithType(new long[] { long.MaxValue, long.MinValue });
        w.WriteValueWithType((sbyte)-50);
        w.WriteValueWithType(new sbyte[] { -100, 100 });
        w.WriteValueWithType((short)-5000);
        w.WriteValueWithType(new short[] { -30000, 30000 });
        w.WriteValueWithType((ushort)50000);
        w.WriteValueWithType(new ushort[] { 70, ushort.MaxValue });
        w.WriteValueWithType(3000000000u);
        w.WriteValueWithType(new uint[] { 10000, uint.MaxValue });
        w.WriteValueWithType(9000000000ul);
        w.WriteValueWithType(new ulong[] { 20000, ulong.MaxValue });
        w.WriteValueWithType(new bool[] { true, false, true, true, false, true, true, true, true, false, false, true });
        w.WriteValueWithType(new TimeSpan(dt2.Ticks));
        w.WriteValueWithType(new TimeSpan[] { new TimeSpan(dt1.Ticks), TimeSpan.FromDays(Math.E) });
        w.WriteValueWithType(new DateTimeOffset(DateTime.SpecifyKind(dt2, DateTimeKind.Unspecified), TimeSpan.FromMinutes(190)));
        w.WriteValueWithType(new DateTimeOffset[] { new DateTimeOffset(DateTime.SpecifyKind(dt1, DateTimeKind.Unspecified), TimeSpan.FromHours(8)),
                                                    new DateTimeOffset(DateTime.SpecifyKind(dt2, DateTimeKind.Unspecified), TimeSpan.FromHours(-3)) });
        w.WriteValueWithType(new XmlDuration(1, 2, 3));
        w.WriteValueWithType(new XmlDuration[] { new XmlDuration(2, 3, 4), new XmlDuration(3, 4, 5, 6, 7, 8) });
        w.WriteValueWithType(new XmlQualifiedName("foo", "http://bar"));
        w.WriteValueWithType(new XmlQualifiedName[] { new XmlQualifiedName("foo2", null), new XmlQualifiedName("elem", "ns") });
      }

      ms.Position = 0;
      using(BinaryReader r = new BinaryReader(ms, false))
      {
        r.LittleEndian = i == 0;
        Assert.AreEqual(null, r.ReadValueWithType());
        Assert.AreEqual(DBNull.Value, r.ReadValueWithType());
        Assert.AreEqual(false, r.ReadValueWithType());
        Assert.AreEqual(true, r.ReadValueWithType());
        Assert.AreEqual((byte)42, r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((byte[])r.ReadValueWithType(), new byte[] { 1, 2, 3 });
        Assert.AreEqual(dt1, r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((DateTime[])r.ReadValueWithType(), new DateTime[] { dt2, dt1 });
        Assert.AreEqual(3.14159m, r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((decimal[])r.ReadValueWithType(), new decimal[] { 2000.77777m, -10000000000000.55m });
        Assert.AreEqual(Math.PI, r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((double[])r.ReadValueWithType(), new double[] { Math.E, Math.Sqrt(2) });
        Assert.AreEqual((float)Math.PI, r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((float[])r.ReadValueWithType(), new float[] { (float)Math.E, 140000.22f });
        Assert.AreEqual(guid, r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((Guid[])r.ReadValueWithType(), new Guid[] { Guid.Empty, guid });
        Assert.AreEqual(-42, r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((int[])r.ReadValueWithType(), new int[] { int.MaxValue, int.MinValue });
        Assert.AreEqual(-500L, r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((long[])r.ReadValueWithType(), new long[] { long.MaxValue, long.MinValue });
        Assert.AreEqual((sbyte)-50, r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((sbyte[])r.ReadValueWithType(), new sbyte[] { -100, 100 });
        Assert.AreEqual((short)-5000, r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((short[])r.ReadValueWithType(), new short[] { -30000, 30000 });
        Assert.AreEqual((ushort)50000, r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((ushort[])r.ReadValueWithType(), new ushort[] { 70, ushort.MaxValue });
        Assert.AreEqual(3000000000u, r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((uint[])r.ReadValueWithType(), new uint[] { 10000, uint.MaxValue });
        Assert.AreEqual(9000000000ul, r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((ulong[])r.ReadValueWithType(), new ulong[] { 20000, ulong.MaxValue });
        TestHelpers.AssertArrayEquals((bool[])r.ReadValueWithType(), new bool[] { true, false, true, true, false, true, true, true, true, false, false, true });
        Assert.AreEqual(new TimeSpan(dt2.Ticks), r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((TimeSpan[])r.ReadValueWithType(), new TimeSpan[] { new TimeSpan(dt1.Ticks), TimeSpan.FromDays(Math.E) });
        Assert.AreEqual(new DateTimeOffset(DateTime.SpecifyKind(dt2, DateTimeKind.Unspecified), TimeSpan.FromMinutes(190)), r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((DateTimeOffset[])r.ReadValueWithType(),
                                      new DateTimeOffset[] { new DateTimeOffset(DateTime.SpecifyKind(dt1, DateTimeKind.Unspecified), TimeSpan.FromHours(8)),
                                                             new DateTimeOffset(DateTime.SpecifyKind(dt2, DateTimeKind.Unspecified), TimeSpan.FromHours(-3)) });
        Assert.AreEqual(new XmlDuration(1, 2, 3), r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((XmlDuration[])r.ReadValueWithType(), new XmlDuration[] { new XmlDuration(2, 3, 4), new XmlDuration(3, 4, 5, 6, 7, 8) });
        Assert.AreEqual(new XmlQualifiedName("foo", "http://bar"), r.ReadValueWithType());
        TestHelpers.AssertArrayEquals((XmlQualifiedName[])r.ReadValueWithType(), new XmlQualifiedName[] { new XmlQualifiedName("foo2", null), new XmlQualifiedName("elem", "ns") });
      }
    }
  }*/
}

} // namespace AdamMil.IO.Tests