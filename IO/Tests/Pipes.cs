using System;
using System.IO;
using Microsoft.Win32.SafeHandles;
using NUnit.Framework;
using AdamMil.IO;
using AdamMil.Tests;

namespace AdamMil.IO.Tests
{

[TestFixture]
public class PipeTest
{
  [Test]
  public void Test()
  {
    using(InheritablePipe pipe = new InheritablePipe())
    {
      FileStream server = new FileStream(new SafeFileHandle(pipe.ServerHandle, false), FileAccess.ReadWrite);
      FileStream client = new FileStream(new SafeFileHandle(pipe.ClientHandle, false), FileAccess.ReadWrite);

      byte[] writeBuffer = System.Text.Encoding.UTF8.GetBytes("Hello, world!");
      byte[] readBuffer = new byte[writeBuffer.Length];

      client.Write(writeBuffer, 0, writeBuffer.Length);
      client.Flush();

      server.Write(writeBuffer, 0, writeBuffer.Length);
      server.Flush();

      int index = 0, count = readBuffer.Length;
      while(count != 0)
      {
        int read = server.Read(readBuffer, index, count);
        if(read == 0) break;
        index += read;
        count -= read;
      }

      Assert.AreEqual(index, readBuffer.Length);
      TestHelpers.AssertArrayEquals(writeBuffer, readBuffer);


      Array.Clear(readBuffer, 0, readBuffer.Length);
      index = 0;
      count = readBuffer.Length;
      while(count != 0)
      {
        int read = client.Read(readBuffer, index, count);
        if(read == 0) break;
        index += read;
        count -= read;
      }

      Assert.AreEqual(index, readBuffer.Length);
      TestHelpers.AssertArrayEquals(writeBuffer, readBuffer);
    }
  }
}

} // namespace AdamMil.IO.Tests