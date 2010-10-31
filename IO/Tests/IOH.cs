using System;
using System.Reflection;
using NUnit.Framework;
using System.IO;
using AdamMil.IO;

namespace AdamMil.IO.Tests
{

[TestFixture]
public class IOHTest
{
  [Test]
  public unsafe void Test()
  {
    const short Short = 0x1122;
    const int Int = 0x33445566;
    const long Long = 0x778899aabbccddee;
    double Double = 123.456;
    float Float = 789.012f;

    Test(Short, sizeof(short));
    Test(Int,   sizeof(int));
    Test(Long,  sizeof(long));

    Test("Double", Double, *(long*)&Double, sizeof(double), false);
    Test("Float", Float, *(uint*)&Float, sizeof(float), false);
  }

  static void Test(long value, int size)
  {
    Test(value, size, false, false);
    Test(value, size, false, true);
    Test(value, size, true, false);
    Test(value, size, true, true);
  }

  static void Test(long value, int size, bool bigEndian, bool unsigned)
  {
    string testName = (bigEndian ? "B" : "L") + "E" + size + (unsigned ? "U" : "");
    object valueObj = unsigned ? size == 2 ? (object)(ushort)value : size == 4 ? (object)(uint)value : (ulong)value
                               : size == 2 ? (object)(short)value  : size == 4 ? (object)(int)value  : value;
    Test(testName, valueObj, value, size, bigEndian);
  }

  static unsafe void Test(string testName, object valueObj, long value, int size, bool bigEndian)
  {
    byte[] array = new byte[8];

    // test reading and writing using the byte[] interface
    MethodInfo read = typeof(IOH).GetMethod("Read"+testName, new Type[] { typeof(byte[]), typeof(int) }),
              write = typeof(IOH).GetMethod("Write"+testName,
                                            new Type[] { typeof(byte[]), typeof(int), valueObj.GetType() });
    write.Invoke(null, new object[] { array, 0, valueObj });
    Assert.AreEqual(read.Invoke(null, new object[] { array, 0 }), valueObj);
    Verify(array, value, size, bigEndian);

    // and test using the byte* interface
    read  = typeof(IOH).GetMethod("Read"+testName, new Type[] { typeof(byte*), typeof(int) });
    write = typeof(IOH).GetMethod("Write"+testName, new Type[] { typeof(byte*), typeof(int), valueObj.GetType() });
    fixed(byte* ptr = array)
    {
      write.Invoke(null, new object[] { new IntPtr(ptr), 0, valueObj });
      Assert.AreEqual(read.Invoke(null, new object[] { new IntPtr(ptr), 0 }), valueObj);
    }
    Verify(array, value, size, bigEndian);

    // and test using the Stream interface
    read  = typeof(IOH).GetMethod("Read"+testName, new Type[] { typeof(Stream) });
    write = typeof(IOH).GetMethod("Write"+testName, new Type[] { typeof(Stream), valueObj.GetType() });
    MemoryStream ms = new MemoryStream(array, 0, array.Length, true, true);
    write.Invoke(null, new object[] { ms, valueObj });
    ms.Position = 0;
    Assert.AreEqual(read.Invoke(null, new object[] { ms }), valueObj);
    Verify(ms.GetBuffer(), value, size, bigEndian);
  }

  static void Verify(byte[] array, long value, int size, bool bigEndian)
  {
    for(int i=0, j=bigEndian ? size-1 : 0; i<size; i++)
    {
      Assert.AreEqual((byte)(value >> (j*8)), array[i]);
      if(bigEndian) j--;
      else j++;
    }
    for(int i=size; i<array.Length; i++) Assert.AreEqual(0, array[i]);
  }
}

} // namespace AdamMil.IO.Tests