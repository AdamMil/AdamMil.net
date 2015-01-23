using System;
using System.IO;
using System.Text;
using AdamMil.Tests;
using AdamMil.Utilities.Encodings;
using NUnit.Framework;

namespace AdamMil.IO.Tests
{

  [TestFixture]
  public class StreamTests
  {
    [Test]
    public void TestAggregateStream()
    {
      const string string1 = "Hello, world!", string2 = " This is the second stream!", string3 = " And this is the third.";
      MemoryStream s1 = new MemoryStream(Encoding.UTF8.GetBytes(string1));
      MemoryStream s2 = new MemoryStream(Encoding.UTF8.GetBytes(string2));
      MemoryStream s3 = new MemoryStream(Encoding.UTF8.GetBytes(string3));
      byte[] allBytes = Encoding.UTF8.GetBytes(string1 + string2 + string3), allBytes2 = new byte[allBytes.Length];

      // we'll also test the delegate stream
      DelegateStream stream = new DelegateStream(new AggregateStream(s1, s2, s3), true);
      Assert.IsTrue(stream.CanRead);
      Assert.IsTrue(stream.CanSeek);
      Assert.AreEqual(s1.Length + s2.Length + s3.Length, stream.Length);
      Assert.AreEqual(0, stream.Position);

      for(int i=0; i<allBytes.Length; i++)
      {
        Assert.AreEqual(allBytes[i], (byte)stream.ReadByte());
        Assert.AreEqual(i+1, stream.Position);
      }
      Assert.AreEqual(-1, stream.ReadByte());

      stream.Position = 0;
      Assert.AreEqual(0, stream.Position);

      Assert.AreEqual(allBytes2.Length, stream.Read(allBytes2, 0, allBytes2.Length));
      TestHelpers.AssertArrayEquals(allBytes, allBytes2);
      Assert.AreEqual(stream.Length, stream.Position);

      stream.Seek(-stream.Length, SeekOrigin.Current);
      Assert.AreEqual(0, stream.Position);

      Assert.AreEqual(string1+string2+string3, new StreamReader(stream).ReadToEnd());
      Assert.AreEqual(stream.Length, stream.Position);
      stream.Seek(-stream.Length, SeekOrigin.End);
      Assert.AreEqual(0, stream.Position);
    }

    [Test]
    public void TestCopyReadStream()
    {
      const string str = "Hello, world!";
      MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(str)), destination = new MemoryStream();
      CopyReadStream stream = new CopyReadStream(source, destination);
      Assert.AreEqual(str, new StreamReader(new DelegateStream(stream, false)).ReadToEnd());
      stream.Position = 0;
      Assert.AreEqual(str, new StreamReader(stream).ReadToEnd());

      destination.Position = 0;
      Assert.AreEqual(str+str, new StreamReader(destination).ReadToEnd());
    }

    [Test]
    public void TestEncodedStream()
    {
      byte[] bytes = new byte[512];
      for(int i=0; i<bytes.Length; i++) bytes[i] = (byte)(i < 256 ? i : 511-i);

      MemoryStream ms = new MemoryStream();
      using(EncodedStream stream = new EncodedStream(ms, new Base64Encoding(), false)) stream.Write(bytes, 0, bytes.Length);
      Assert.AreEqual((bytes.Length+2)/3*4, ms.Length);
      Assert.AreEqual(Convert.ToBase64String(bytes), SimpleEightBitEncoding.Instance.GetString(ms.ToArray()));

      ms.Position = 0;
      using(EncodedStream stream = new EncodedStream(ms, new Base64Encoding(), false))
      {
        byte[] bytesRead = new byte[bytes.Length];
        Assert.AreEqual(bytesRead.Length, stream.Read(bytesRead, 0, bytesRead.Length));
        Assert.AreEqual(-1, stream.ReadByte());
        TestHelpers.AssertArrayEquals(bytes, bytesRead);
      }
    }

    [Test]
    public void TestTeeStream()
    {
      MemoryStream ms1 = new MemoryStream(), ms2 = new MemoryStream();
      TeeStream stream = new TeeStream(ms1, ms2);
      byte[] bytes = Encoding.UTF8.GetBytes("Hello, world!");
      stream.Write(bytes);
      Assert.AreEqual(ms1.Position, ms2.Position);
      TestHelpers.AssertArrayEquals(bytes, ms1.ToArray());
      TestHelpers.AssertArrayEquals(bytes, ms2.ToArray());
    }

    [Test]
    public void TestTextStream()
    {
      const string str = "Hello, world!";
      StringBuilder sb = new StringBuilder(str);
      StringReader sr = new StringReader(str);
      Assert.AreEqual(str, new StreamReader(new TextStream(str)).ReadToEnd());
      Assert.AreEqual(str, new StreamReader(new TextStream(sb)).ReadToEnd());
      Assert.AreEqual(str, new StreamReader(new TextStream(sr)).ReadToEnd());
    }
  }

} // namespace AdamMil.IO.Tests
