using System;
using System.IO;
using NUnit.Framework;
using AdamMil.IO;

namespace AdamMil.IO.Tests
{

[TestFixture]
public class BinaryReaderTest
{
  [Test]
  public unsafe void Test()
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
}

[TestFixture]
public class BinaryWriterTest
{
  [Test]
  public unsafe void Test()
  {
  }
}

} // namespace AdamMil.IO.Tests