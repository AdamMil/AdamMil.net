using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace AdamMil.Utilities.Tests
{

[TestFixture]
public class ArrayTests
{
  [Test]
  public void TestArrayBuffers()
  {
    ArrayBuffer<byte> buffer = new ArrayBuffer<byte>(256);
    Assert.AreEqual(256, buffer.Capacity);

    for(int i=0; i<10; i++) buffer.Add((byte)i);
    for(int i=0; i<10; i++) buffer.Add((byte)i);
    Assert.AreEqual(20, buffer.Count);
    for(int i=0; i<10; i++) Assert.AreEqual(i, buffer.Remove());
    Assert.AreEqual(10, buffer.Count);
    for(int i=0; i<10; i++) Assert.AreEqual(i, buffer[i]);

    byte[] bytes = new byte[10];
    buffer.Remove(bytes, 0, bytes.Length);
    for(int i=0; i<10; i++) Assert.AreEqual(i, bytes[i]);
    Assert.AreEqual(0, buffer.Count);

    buffer.AddRange(bytes);
    buffer.AddRange(bytes, 0, bytes.Length);
    Assert.AreEqual(20, buffer.Count);
    for(int i=0; i<20; i++) Assert.AreEqual(i%10, buffer[i]);
    Assert.IsTrue(buffer.Contains(5));
    Assert.AreEqual(5, buffer.IndexOf(5));
    Assert.IsFalse(buffer.Contains(20));
    Assert.AreEqual(-1, buffer.IndexOf(20));

    buffer.CopyTo(5, bytes, 0, 5);
    for(int i=0; i<5; i++) Assert.AreEqual(i+5, bytes[i]);

    buffer.Remove(15);
    Assert.AreEqual(5, buffer.Count);
    for(int i=0; i<5; i++) Assert.AreEqual(i+5, buffer[i]);

    List<byte> list = new List<byte>(buffer);
    for(int i=0; i<5; i++) Assert.AreEqual(i+5, list[i]);

    Array.Clear(bytes, 0, bytes.Length);
    buffer.CopyTo(bytes, 0);
    for(int i=0; i<5; i++) Assert.AreEqual(i+5, bytes[i]);

    byte[] array = buffer.GetZeroOffsetArray();
    for(int i=0; i<5; i++) Assert.AreEqual(i+5, array[i]);

    buffer.Clear();
    Assert.AreEqual(0, buffer.Count);

    buffer.EnsureCapacity(200);
    Assert.AreEqual(256, buffer.Capacity);

    buffer.EnsureCapacity(300);
    Assert.AreEqual(300, buffer.Capacity);

    buffer.AddCount(250);
    Assert.AreEqual(250, buffer.Count);

    array = buffer.GetArrayForWriting(100);
    Assert.IsTrue(array.Length >= 350);

    buffer.Remove(100);
    Assert.AreEqual(150, buffer.Count);
    Assert.AreEqual(100, buffer.GetRawIndex(0));
    Assert.AreEqual(250, buffer.GetRawIndex(buffer.Count));
    Assert.AreEqual(0, buffer.GetLogicalIndex(100));
    Assert.AreEqual(buffer.Count, buffer.GetLogicalIndex(250));

    buffer.SetCount(1);
    Assert.AreEqual(1, buffer.Count);
    Assert.AreEqual(100, buffer.Offset);

    buffer.GetZeroOffsetArray();
    Assert.AreEqual(0, buffer.Offset);
  }
}

} // namespace AdamMil.Utilities.Tests
