/*
AdamMil.IO is a library that provides high performance and high level IO
tools for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2016 Adam Milazzo

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using AdamMil.Utilities;

namespace AdamMil.IO
{

#region PinnedBuffer
/// <summary>This class supports the <see cref="BinaryReader"/> and <see cref="BinaryWriter"/> classes and is not
/// intended to be used directly. This class manages a constantly-pinned buffer. This class in not safe for use by
/// multiple threads concurrently.
/// </summary>
public unsafe abstract class PinnedBuffer : IDisposable
{
  /// <summary>Initializes an empty PinnedBuffer with the given buffer size.</summary>
  /// <param name="bufferSize">The initial buffer size. If zero, a default size of 4096 bytes will be used.</param>
  protected PinnedBuffer(int bufferSize)
  {
    if(bufferSize < 0) throw new ArgumentOutOfRangeException();
    if(bufferSize == 0) bufferSize = 4096;

    Buffer = new byte[bufferSize];
    PinBuffer();
  }

  /// <summary>Initializes a PinnedBuffer with the given array. The buffer cannot be further enlarged.</summary>
  protected PinnedBuffer(byte[] array)
  {
    if(array == null) throw new ArgumentNullException();
    Buffer = array;
    ExternalBuffer = true;
    PinBuffer();
  }

  ~PinnedBuffer()
  {
    Dispose(false);
  }

  /// <summary>Unpins and frees the buffer.</summary>
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  /// <summary>Returns a safe reference to the underlying buffer.</summary>
  protected byte[] Buffer
  {
    get; private set;
  }

  /// <summary>Returns an unsafe pointer to the underlying buffer.</summary>
  [CLSCompliant(false)]
  protected byte* BufferPtr
  {
    get; private set;
  }

  /// <summary>Gets whether the buffer was passed to the constructor and cannot be enlarged or reallocated.</summary>
  protected bool ExternalBuffer
  {
    get; private set;
  }

  /// <summary>Creates the new buffer to be used when enlarging the IO buffer.</summary>
  /// <param name="newSize">The new size of the buffer.</param>
  /// <returns>A newly-allocated buffer of the given size.</returns>
  /// <remarks>The default implementation simply allocates a new buffer and copies the old data into it.</remarks>
  protected virtual byte[] CreateResizeBuffer(int newSize)
  {
    byte[] newBuffer = new byte[newSize];
    Array.Copy(Buffer, newBuffer, Buffer.Length);
    return newBuffer;
  }

  /// <summary>Unpins and frees the buffer.</summary>
  protected virtual void Dispose(bool manualDispose)
  {
    FreeBuffer();
  }

  /// <summary>Ensures that the buffer has at least the given capacity. This method will resize the buffer if
  /// necessary, which will invalidate any pointers to the buffer.
  /// </summary>
  /// <param name="capacity">The required capacity of the buffer.</param>
  protected void EnsureCapacity(int capacity)
  {
    if(capacity > Buffer.Length)
    {
      if(ExternalBuffer) throw new InvalidOperationException("This buffer cannot be enlarged beyond its original size.");

      int newSize = Buffer.Length, add = 4096;
      if((newSize & 0xFFF) != 0) add = Buffer.Length; // if the buffer size is not a multiple of 4096, grow the buffer in doubles
      do newSize += add; while(newSize < capacity);

      byte[] newBuffer = CreateResizeBuffer(newSize);
      FreeBuffer();
      Buffer = newBuffer;
      PinBuffer();
    }
  }

  /// <summary>Swaps the byte order of each word in the given data.</summary>
  /// <param name="data">A pointer to the data.</param>
  /// <param name="words">The number of two-byte words to swap.</param>
  [CLSCompliant(false)]
  protected static void SwapEndian2(void* data, int words)
  {
    for(ushort* p = (ushort*)data, end = p + words; p < end; p++) *p = BinaryUtility.ByteSwap(*p);
  }

  /// <summary>Swaps the byte order of each doubleword in the given data.</summary>
  /// <param name="data">A pointer to the data.</param>
  /// <param name="dwords">The number of four-byte doublewords to swap.</param>
  [CLSCompliant(false)]
  protected static void SwapEndian4(void* data, int dwords)
  {
    for(uint* p = (uint*)data, end = p + dwords; p < end; p++) *p = BinaryUtility.ByteSwap(*p);
  }

  /// <summary>Swaps the byte order of each quadword in the given data.</summary>
  /// <param name="data">A pointer to the data.</param>
  /// <param name="qwords">The number of eight-byte quadwords to swap.</param>
  [CLSCompliant(false)]
  protected static void SwapEndian8(void* data, int qwords)
  {
    for(uint* p = (uint*)data, end = p + qwords*2; p < end; p += 2)
    {
      uint t = BinaryUtility.ByteSwap(p[0]);
      p[0]   = BinaryUtility.ByteSwap(p[1]);
      p[1]   = t;
    }
  }

  void FreeBuffer()
  {
    if(handle.IsAllocated) handle.Free();
    BufferPtr = null;
  }

  void PinBuffer()
  {
    handle    = GCHandle.Alloc(Buffer, GCHandleType.Pinned); // NOTE: GCHandle is slow. it'd be nice to do without it...
    BufferPtr = (byte*)handle.AddrOfPinnedObject().ToPointer();
  }

  GCHandle handle;
}
#endregion

#region BinaryReaderWriterBase
/// <summary>This class supports the <see cref="BinaryReader"/> and <see cref="BinaryWriter"/> classes and is not meant
/// to be used directly.
/// </summary>
public abstract class BinaryReaderWriterBase : PinnedBuffer
{
  internal BinaryReaderWriterBase(Stream stream, bool ownStream, bool littleEndian, int bufferSize) : base(bufferSize)
  {
    if(stream == null) throw new ArgumentNullException();
    this.stream    = stream;
    this.ownStream = ownStream;
    LittleEndian = littleEndian;
  }

  internal BinaryReaderWriterBase(byte[] array, int index, int length, bool littleEndian) : base(array)
  {
    Utility.ValidateRange(array, index, length);
    LittleEndian = littleEndian;
  }

  /// <summary>Returns a reference to the underlying stream.</summary>
  public Stream BaseStream
  {
    get { return this.stream; }
  }

  /// <summary>Gets or sets whether integer data being read from or written to the stream is little endian.</summary>
  /// <remarks>If the endianness of the data does not match the endianness of the system, the bytes will be swapped as
  /// necessary. The default is true.
  /// </remarks>
  public bool LittleEndian { get; set; }

  /// <inheritdoc/>
  protected override void Dispose(bool manualDispose)
  {
    if(ownStream) stream.Dispose();
    base.Dispose(manualDispose);
  }

  readonly Stream stream;
  readonly bool ownStream;
}
#endregion

#region BinaryReader
/// <summary>This class makes it easy to efficiently deserialize values from a stream or array.</summary>
/// <remarks>If initialized with a stream, the reader will buffer input, and so may read more bytes from the stream
/// than you explicitly request. However, when the class is disposed, it will seek the stream to the end of the data
/// read from the reader, if the stream supports seeking. This class is not safe for use by multiple threads concurrently.
/// </remarks>
public unsafe class BinaryReader : BinaryReaderWriterBase
{
  /// <summary>Initializes this <see cref="BinaryReader"/> with the default buffer size, little-endianness, and the assumption that the
  /// stream will not be accessed by any other classes while this reader is in use. The stream will be closed when the
  /// <see cref="BinaryReader"/> is disposed.
  /// </summary>
  /// <param name="stream">The stream from which data will be read.</param>
  public BinaryReader(Stream stream) : this(stream, true, true) { }

  /// <summary>Initializes this <see cref="BinaryReader"/> with the default buffer size, little-endianness, and the assumption that the
  /// stream will not be accessed by any other classes while this reader is in use.
  /// </summary>
  /// <param name="stream">The stream from which data will be read.</param>
  /// <param name="ownStream">If true, the stream will be closed when the <see cref="BinaryReader"/> is disposed.</param>
  public BinaryReader(Stream stream, bool ownStream) : this(stream, ownStream, true) { }

  /// <summary>Initializes this <see cref="BinaryReader"/> with the default buffer size and the
  /// assumption that the stream will not be accessed by any other classes while this reader is in use.
  /// </summary>
  /// <param name="stream">The stream from which data will be read.</param>
  /// <param name="ownStream">If true, the stream will be closed when the <see cref="BinaryReader"/> is disposed.</param>
  /// <param name="littleEndian">Whether the data being read is little endian. This can be changed at any time using
  /// the <see cref="LittleEndian"/> property.
  /// </param>
  public BinaryReader(Stream stream, bool ownStream, bool littleEndian) : this(stream, ownStream, littleEndian, 0) { }

  /// <summary>Initializes this <see cref="BinaryReader"/>.</summary>
  /// <param name="stream">The stream from which data will be read. If the stream does its own buffering, it may be
  /// more efficient to eliminate the buffer from the stream, so that the data is not buffered multiple times.
  /// </param>
  /// <param name="ownStream">If true, the stream will be closed when the <see cref="BinaryReader"/> is disposed.</param>
  /// <param name="littleEndian">Whether the data being read is little endian. This can be changed at any time using
  /// the <see cref="LittleEndian"/> property.
  /// </param>
  /// <param name="bufferSize">The buffer size. If zero, a default buffer size of 4096 bytes will be used. The default
  /// buffer size is usually sufficient, but if you know approximately how much data you'll be reading from the stream,
  /// you can tune the buffer size so that the <see cref="BinaryReader"/> does not read more data than is necessary.
  /// </param>
  public BinaryReader(Stream stream, bool ownStream, bool littleEndian, int bufferSize) : base(stream, ownStream, littleEndian, bufferSize)
  {
    DefaultEncoding = Encoding.UTF8;
  }

  /// <summary>Initializes this <see cref="BinaryReader"/> to read from the given array with little endianness.</summary>
  public BinaryReader(byte[] array) : this(array, 0, array.Length, true) { }

  /// <summary>Initializes this <see cref="BinaryReader"/> to read from the given array with little endianness.</summary>
  /// <param name="index">The starting index of the area within the buffer from which data will be read.</param>
  /// <param name="length">The length of the area within the buffer from which data will be read.</param>
  public BinaryReader(byte[] array, int index, int length) : this(array, index, length, true) { }

  /// <summary>Initializes this <see cref="BinaryReader"/> to read from the given array with the given endianness.</summary>
  /// <param name="index">The starting index of the area within the buffer from which data will be read.</param>
  /// <param name="length">The length of the area within the buffer from which data will be read.</param>
  public BinaryReader(byte[] array, int index, int length, bool littleEndian)
    : base(array, index, length, littleEndian)
  {
    this.tailIndex  = this.startIndex = index;
    this.headIndex  = index+length;
    DefaultEncoding = Encoding.UTF8;
  }

  /// <summary>Gets or sets the default encoding used to transform characters and strings into bytes and vice versa.</summary>
  public Encoding DefaultEncoding
  {
    get { return encoding; }
    set
    {
      if(value != DefaultEncoding)
      {
        if(value == null) throw new ArgumentNullException();
        encoding = value;
        decoder  = value.GetDecoder();
      }
    }
  }

  /// <summary>Gets or sets the current position of the reader within the underlying stream or external buffer. This equal to the
  /// underlying stream's position, minus the amount of data available in the reader's buffer.
  /// </summary>
  /// <remarks>Note that setting the position may cause data to be discarded from the buffer. This inefficiency can be
  /// mitigated by reducing the size of the buffer so that less data is thrown away, although seek performance must be balanced
  /// with read performance.
  /// </remarks>
  /// <exception cref="NotSupportedException">Thrown if the reader is based on a stream and the stream is not seekable.</exception>
  public long Position
  {
    get
    {
      if(ExternalBuffer) return tailIndex - startIndex;
      else return BaseStream.Position - AvailableData;
    }
    set
    {
      if(ExternalBuffer)
      {
        if(value < 0 || value > headIndex-startIndex) throw new ArgumentOutOfRangeException();
        tailIndex = (int)(value-startIndex);
      }
      else
      {
        long dataEnd = BaseStream.Position, dataStart = dataEnd - AvailableData;

        // if the new position is within the range of data in our buffer, we can simply tweak our tail
        if(value >= dataStart && value < dataEnd)
        {
          AdvanceTail((int)(value-dataStart));
        }
        else // otherwise, we have to discard the whole buffer
        {
          tailIndex = headIndex = 0;
          BaseStream.Position = value;
        }
      }
    }
  }

  /// <summary>Reads a number of bytes from the stream into the given memory region.</summary>
  /// <param name="dest">A pointer to the location in memory where the data will be written.</param>
  /// <param name="nbytes">The number of bytes to read from the stream.</param>
  [CLSCompliant(false)]
  public void Read(void* dest, int nbytes)
  {
    if(nbytes < 0) throw new ArgumentOutOfRangeException();

    byte* ptr = (byte*)dest;

    // attempt to satisfy the request with the contiguous data starting from the tail
    ReadDataInternal(ref ptr, ref nbytes, Math.Min(ContiguousData, nbytes));
    if(nbytes != 0) // if more data was requested...
    {
      if(ExternalBuffer) throw new EndOfStreamException(); // if the buffer is external, there's no more data

      if(headIndex != 0) // attempt to satisfy the remaining request with contiguous data starting from index 0
      {
        ReadDataInternal(ref ptr, ref nbytes, Math.Min(headIndex, nbytes));
      }

      // if that wasn't enough either, we've exhausted the buffer and will read more in the loop below
      if(nbytes != 0)
      {
        do
        {
          // avoid reading too much from a non-seekable stream
          headIndex = BaseStream.Read(Buffer, 0, BaseStream.CanSeek ? Buffer.Length : Math.Min(Buffer.Length, nbytes));
          if(headIndex == 0) throw new EndOfStreamException();
          ReadDataInternal(ref ptr, ref nbytes, Math.Min(headIndex, nbytes));
        } while(nbytes != 0);
      }
    }
  }

  /// <summary>Reads a number of bytes from the stream into the given memory region.</summary>
  /// <param name="dest">A pointer to the location in memory where the data will be written.</param>
  /// <param name="nbytes">The number of bytes to read from the stream.</param>
  [CLSCompliant(false)]
  public void Read(void* dest, long nbytes)
  {
    if(nbytes < 0) throw new ArgumentOutOfRangeException();
    const int BigChunkSize = int.MaxValue & ~7; // trim off the last few bits to avoid misaligning the reads
    while(nbytes >= BigChunkSize)
    {
      Read(dest, BigChunkSize);
      dest    = (byte*)dest + BigChunkSize;
      nbytes -= BigChunkSize;
    }
    Read(dest, (int)nbytes);
  }

  /// <summary>Reads a number of items from the stream into the given memory region, optionally swapping the bytes in
  /// each item.
  /// </summary>
  /// <param name="dest">The location in memory to which the data will be written.</param>
  /// <param name="count">The number of items to read. Each item has a size of <paramref name="wordSize"/> bytes.</param>
  /// <param name="wordSize">The size of each item to read. The bytes in each item will be swapped to ensure the
  /// correct endianness. If you don't want any swapping to occur, use a value of one for the word size.
  /// </param>
  [CLSCompliant(false)]
  public void Read(void* dest, int count, int wordSize)
  {
    if(wordSize != 1 && wordSize != 2 && wordSize != 4 && wordSize != 8)
    {
      throw new ArgumentOutOfRangeException("Word size must be 1, 2, 4, or 8.");
    }
    if(count < 0) throw new ArgumentOutOfRangeException();

    int shift = wordSize == 4 ? 2 : wordSize == 8 ? 3 : wordSize-1; // the shift to divide or multiply by the word size
    byte* destPtr = (byte*)dest;

    while(count != 0)
    {
      EnsureContiguousData(wordSize);
      int toRead = Math.Min(ContiguousData>>shift, count), bytesRead = toRead<<shift;
      Unsafe.Copy(BufferPtr+tailIndex, destPtr, bytesRead);

      if(wordSize != 1)
      {
        if(wordSize == 4) MakeSystemEndian4(destPtr, toRead);
        else if(wordSize == 2) MakeSystemEndian2(destPtr, toRead);
        else MakeSystemEndian8(destPtr, toRead);
      }

      destPtr += bytesRead;
      AdvanceTail(bytesRead);
      count -= toRead;
    }
  }

  /// <summary>Reads a <see cref="DateTime"/> object from the binary reader with automatic time zone adjustment.
  /// The value is assumed to have been written with <see cref="BinaryWriter.WriteAdjustableDateTime(DateTime)"/>.
  /// </summary>
  /// <remarks>If the value is based on the local time zone, it will be automatically adjusted to the new local time zone when
  /// deserialized. (For instance, 3PM serialized in PST may become 6PM when deserialized in EST.) Times based on UTC are unaffected.
  /// </remarks>
  public DateTime ReadAdjustableDateTime()
  {
    return DateTime.FromBinary(ReadInt64());
  }

  /// <summary>Reads a one-byte boolean value from the stream.</summary>
  public bool ReadBoolean()
  {
    return ReadByte() != 0;
  }

  /// <summary>Reads an unsigned byte from the stream.</summary>
  public byte ReadByte()
  {
    return *ReadContiguousData(1);
  }

  /// <summary>Reads a signed byte from the stream.</summary>
  [CLSCompliant(false)]
  public sbyte ReadSByte()
  {
    return (sbyte)ReadByte();
  }

  /// <summary>Reads a character from the stream using the default encoding.</summary>
  public char ReadChar()
  {
    return ReadChar(DefaultEncoding);
  }

  /// <summary>Reads a character from the stream using the given encoding.</summary>
  public char ReadChar(Encoding encoding)
  {
    if(encoding == null) throw new ArgumentOutOfRangeException();

    Decoder decoder = encoding == DefaultEncoding ? this.decoder : encoding.GetDecoder();
    char value;
    for(int i=0; i<16; i++) // assume that all characters fit within 16 bytes. (the .NET BinaryWriter makes this assumption)
    {
      byte byteValue = ReadByte();
      if(decoder.GetChars(&byteValue, 1, &value, 1, false) != 0) return value; // also assume that we needn't flush the decoder
    }

    decoder.Reset();
    throw new NotSupportedException(); // throw an exception if the character didn't fit within 16 bytes
  }

  /// <summary>Reads a <see cref="DateTime"/> object from the binary reader. The <see cref="DateTime"/> is assumed to have been
  /// written with <see cref="BinaryWriter.Write(DateTime)"/>.
  /// </summary>
  public DateTime ReadDateTime()
  {
    ulong value = ReadUInt64();
    return new DateTime((long)(value>>2), (DateTimeKind)(value&3));
  }

  /// <summary>Reads a variable-length signed integer from the stream.</summary>
  public int ReadEncodedInt32()
  {
    int byteValue = ReadByte(), value = byteValue & 0x3F, shift = 6;
    bool negative = (byteValue & 0x40) != 0;
    while((byteValue & 0x80) != 0)
    {
      byteValue = ReadByte();
      value |= (byteValue & 0x7F) << shift;
      shift += 7;
    }
    return negative && shift < 32 ? value | (-1 << shift) : value; // if it's negative, fill in the bits for the sign extension
  }

  /// <summary>Reads a variable-length unsigned integer from the stream.</summary>
  [CLSCompliant(false)]
  public uint ReadEncodedUInt32()
  {
    uint value = 0, byteValue;
    int shift = 0;
    do
    {
      byteValue = ReadByte();
      value |= (byteValue & 0x7F) << shift;
      shift += 7;
    } while((byteValue & 0x80) != 0);
    return value;
  }

  /// <summary>Reads a variable-length signed long integer from the stream.</summary>
  public long ReadEncodedInt64()
  {
    int byteValue = ReadByte(), shift = 6;
    bool negative = (byteValue & 0x40) != 0;
    long value = byteValue & 0x3F;
    while((byteValue & 0x80) != 0)
    {
      byteValue = ReadByte();
      value |= (long)(byteValue & 0x7F) << shift;
      shift += 7;
    }
    return negative && shift < 64 ? value | ((long)-1 << shift) : value; // if it's negative, fill in the bits for the sign-extension
  }

  /// <summary>Reads a variable-length unsigned long integer from the stream.</summary>
  [CLSCompliant(false)]
  public ulong ReadEncodedUInt64()
  {
    ulong value = 0;
    uint byteValue;
    int shift = 0;
    do
    {
      byteValue = ReadByte();
      value |= (ulong)(byteValue & 0x7F) << shift;
      shift += 7;
    } while((byteValue & 0x80) != 0);
    return value;
  }

  /// <summary>Reads a <see cref="Guid"/> from the stream.</summary>
  public unsafe Guid ReadGuid()
  {
    // this assumes that Guid is laid out the right way in memory. it is in Microsoft's implementation
    return *(Guid*)ReadContiguousData(16);
  }

  /// <summary>Reads a signed two-byte integer from the stream.</summary>
  public short ReadInt16()
  {
    return (short)ReadUInt16();
  }

  /// <summary>Reads an unsigned two-byte integer from the stream.</summary>
  [CLSCompliant(false)]
  public ushort ReadUInt16()
  {
    byte* data = ReadContiguousData(sizeof(ushort));
    return LittleEndian == BitConverter.IsLittleEndian ? *(ushort*)data : LittleEndian ? IOH.ReadLE2U(data) : IOH.ReadBE2U(data);
  }

  /// <summary>Reads a signed four-byte integer from the stream.</summary>
  public int ReadInt32()
  {
    return (int)ReadUInt32();
  }

  /// <summary>Reads an unsigned four-byte integer from the stream.</summary>
  [CLSCompliant(false)]
  public uint ReadUInt32()
  {
    byte* data = ReadContiguousData(sizeof(uint));
    return LittleEndian == BitConverter.IsLittleEndian ? *(uint*)data : LittleEndian ? IOH.ReadLE4U(data) : IOH.ReadBE4U(data);
  }

  /// <summary>Reads a signed eight-byte integer from the stream.</summary>
  public long ReadInt64()
  {
    return (long)ReadUInt64();
  }

  /// <summary>Reads an unsigned eight-byte integer from the stream.</summary>
  [CLSCompliant(false)]
  public ulong ReadUInt64()
  {
    byte* data = ReadContiguousData(sizeof(ulong));
    return LittleEndian == BitConverter.IsLittleEndian ? *(ulong*)data : LittleEndian ? IOH.ReadLE8U(data) : IOH.ReadBE8U(data);
  }

  /// <summary>Reads a four-byte float from the stream.</summary>
  public float ReadSingle()
  {
    byte* data = ReadContiguousData(sizeof(float));
    return LittleEndian == BitConverter.IsLittleEndian ? *(float*)data : LittleEndian ? IOH.ReadLESingle(data) : IOH.ReadBESingle(data);
  }

  /// <summary>Reads an eight-byte float from the stream.</summary>
  public double ReadDouble()
  {
    byte* data = ReadContiguousData(sizeof(double));
    return LittleEndian == BitConverter.IsLittleEndian ? *(double*)data : LittleEndian ? IOH.ReadLEDouble(data) : IOH.ReadBEDouble(data);
  }

  /// <summary>Reads a 13-byte <see cref="decimal"/> value from the stream.</summary>
  public decimal ReadDecimal()
  {
    int low = ReadInt32(), mid = ReadInt32(), high = ReadInt32(), scale = ReadByte();
    bool isNegative = (scale & 0x80) != 0;
    return new decimal(low, mid, high, isNegative, (byte)(scale & 0x7F));
  }

  /// <summary>Reads an array of one-byte boolean values from the stream.</summary>
  public bool[] ReadBooleans(int count)
  {
    bool[] data = new bool[count];
    if(count != 0) fixed(bool* ptr=data) Read(ptr, count);
    return data;
  }

  /// <summary>Reads an array of one-byte boolean values from the stream.</summary>
  public void ReadBooleans(bool[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    if(count != 0) fixed(bool* ptr=array) Read(ptr+index, count);
  }

  /// <summary>Reads an array of one-byte boolean values from the stream.</summary>
  [CLSCompliant(false)]
  public void ReadBooleans(bool* array, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    Read(array, count);
  }

  /// <summary>Reads an array of bytes from the stream.</summary>
  public byte[] ReadBytes(int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    byte[] data = new byte[count];
    if(count != 0) fixed(byte* ptr=data) Read(ptr, count);
    return data;
  }

  /// <summary>Reads a number of bytes from the stream.</summary>
  public void ReadBytes(byte[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    if(count != 0) fixed(byte* ptr=array) Read(ptr, count);
  }

  /// <summary>Reads an array of signed bytes from the stream.</summary>
  [CLSCompliant(false)]
  public sbyte[] ReadSBytes(int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    sbyte[] data = new sbyte[count];
    if(count != 0) fixed(sbyte* ptr=data) Read(ptr, count);
    return data;
  }

  /// <summary>Reads a number of signed bytes from the stream.</summary>
  [CLSCompliant(false)]
  public void ReadSBytes(sbyte[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    if(count != 0) fixed(sbyte* ptr=array) Read(ptr, count);
  }

  /// <summary>Reads an array of two-byte characters from the stream using the given encoding.</summary>
  public char[] ReadChars(int count)
  {
    return ReadChars(count, DefaultEncoding);
  }

  /// <summary>Reads an array of two-byte characters from the stream using the given encoding.</summary>
  public char[] ReadChars(int count, Encoding encoding)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    char[] chars = new char[count];
    if(count != 0) fixed(char* charPtr=chars) ReadChars(charPtr, count, encoding);
    return chars;
  }

  /// <summary>Reads a number of two-byte characters from the stream. Returns the number of bytes read from the stream.</summary>
  public int ReadChars(char[] array, int index, int count)
  {
    return ReadChars(array, index, count, DefaultEncoding);
  }

  /// <summary>Reads a number of two-byte characters from the stream. Returns the number of bytes read from the stream.</summary>
  public int ReadChars(char[] array, int index, int count, Encoding encoding)
  {
    Utility.ValidateRange(array, index, count);
    if(count == 0) return 0; // zero-length arrays yield a null-pointer when fixed, so avoid passing a null pointer
    else fixed(char* ptr=array) return ReadChars(ptr+index, count, encoding);
  }

  /// <summary>Reads a number of two-byte characters from the stream. Returns the number of bytes read from the stream.</summary>
  [CLSCompliant(false)]
  public int ReadChars(char* array, int count)
  {
    return ReadChars(array, count, DefaultEncoding);
  }

  /// <summary>Reads a number of two-byte characters from the stream. Returns the number of bytes read from the stream.</summary>
  [CLSCompliant(false)]
  public int ReadChars(char* array, int count, Encoding encoding)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    if(encoding == null) throw new ArgumentNullException();

    Decoder decoder = encoding == DefaultEncoding ? this.decoder : encoding.GetDecoder();
    int totalRead = 0;
    while(count != 0)
    {
      EnsureContiguousData(1);
      // NOTE: this assumes that each character is at least one byte (the built-in .NET BinaryReader makes the same assumption)
      // this also assumes that we never need to flush the decoder
      int bytesRead = Math.Min(count, ContiguousData);
      int charsRead = decoder.GetChars(BufferPtr+tailIndex, bytesRead, array, count, false);
      AdvanceTail(bytesRead);
      totalRead += bytesRead;
      array += charsRead;
      count -= charsRead;
    }
    return totalRead;
  }

  /// <summary>Reads an array of <see cref="DateTime"/> objects from the stream with automatic time zone adjustment.</summary>
  /// <remarks>If a value is based on the local time zone, it will be automatically adjusted to the new local time zone when
  /// deserialized. (For instance, 3PM serialized in PST may become 6PM when deserialized in EST.) Times based on UTC are unaffected.
  /// </remarks>
  public DateTime[] ReadAdjustableDateTimes(int count)
  {
    DateTime[] data = new DateTime[count];
    ReadAdjustableDateTimes(data, 0, count);
    return data;
  }

  /// <summary>Reads an array of <see cref="DateTime"/> objects from the stream with automatic time zone adjustment.</summary>
  /// <remarks>If a value is based on the local time zone, it will be automatically adjusted to the new local time zone when
  /// deserialized. (For instance, 3PM serialized in PST may become 6PM when deserialized in EST.) Times based on UTC are unaffected.
  /// </remarks>
  public void ReadAdjustableDateTimes(DateTime[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    for(int end=index+count; index < end; index++) array[index] = ReadAdjustableDateTime();
  }

  /// <summary>Reads an array of <see cref="DateTime"/> objects from the stream.</summary>
  public DateTime[] ReadDateTimes(int count)
  {
    DateTime[] data = new DateTime[count];
    ReadDateTimes(data, 0, count);
    return data;
  }

  /// <summary>Reads an array of <see cref="DateTime"/> objects from the stream.</summary>
  public void ReadDateTimes(DateTime[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    for(int end=index+count; index < end; index++) array[index] = ReadDateTime();
  }

  /// <summary>Reads a number of integers from the binary reader, each of which is assumed to have been written with
  /// <see cref="BinaryWriter.WriteEncoded(int)"/>.
  /// </summary>
  public int[] ReadEncodedInt32s(int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    int[] values = new int[count];
    for(int i=0; i<values.Length; i++) values[i] = ReadEncodedInt32();
    return values;
  }

  /// <summary>Reads a number of long integers from the binary reader, each of which is assumed to have been written with
  /// <see cref="BinaryWriter.WriteEncoded(long)"/>.
  /// </summary>
  public long[] ReadEncodedInt64s(int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    long[] values = new long[count];
    for(int i=0; i<values.Length; i++) values[i] = ReadEncodedInt64();
    return values;
  }

  /// <summary>Reads a number of unsigned integers from the binary reader, each of which is assumed to have been written with
  /// <see cref="BinaryWriter.WriteEncoded(uint)"/>.
  /// </summary>
  [CLSCompliant(false)]
  public uint[] ReadEncodedUInt32s(int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    uint[] values = new uint[count];
    for(int i=0; i<values.Length; i++) values[i] = ReadEncodedUInt32();
    return values;
  }

  /// <summary>Reads a number of unsigned long integers from the binary reader, each of which is assumed to have been written with
  /// <see cref="BinaryWriter.WriteEncoded(ulong)"/>.
  /// </summary>
  [CLSCompliant(false)]
  public ulong[] ReadEncodedUInt64s(int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    ulong[] values = new ulong[count];
    for(int i=0; i<values.Length; i++) values[i] = ReadEncodedUInt64();
    return values;
  }

  /// <summary>Reads an array of <see cref="Guid"/> objects from the stream.</summary>
  public Guid[] ReadGuids(int count)
  {
    Guid[] data = new Guid[count];
    ReadGuids(data, 0, count);
    return data;
  }

  /// <summary>Reads an array of <see cref="Guid"/> objects from the stream.</summary>
  public unsafe void ReadGuids(Guid[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    if(count != 0) fixed(Guid* ptr=array) Read((void*)(ptr+index), count*16L);
  }

  /// <summary>Reads an array of signed two-byte integers from the stream.</summary>
  public short[] ReadInt16s(int count)
  {
    short[] data = new short[count];
    if(count != 0) fixed(short* ptr=data) ReadInt16s(ptr, count);
    return data;
  }

  /// <summary>Reads an array of signed two-byte integers from the stream.</summary>
  public void ReadInt16s(short[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    if(count != 0) fixed(short* ptr=array) ReadInt16s(ptr+index, count);
  }

  /// <summary>Reads an array of signed two-byte integers from the stream.</summary>
  [CLSCompliant(false)]
  public void ReadInt16s(short* array, int count)
  {
    ReadUInt16s((ushort*)array, count);
  }

  /// <summary>Reads an array of unsigned two-byte integers from the stream.</summary>
  [CLSCompliant(false)]
  public ushort[] ReadUInt16s(int count)
  {
    ushort[] data = new ushort[count];
    if(count != 0) fixed(ushort* ptr=data) ReadUInt16s(ptr, count);
    return data;
  }

  /// <summary>Reads an array of unsigned two-byte integers from the stream.</summary>
  [CLSCompliant(false)]
  public void ReadUInt16s(ushort[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    if(count != 0) fixed(ushort* ptr=array) ReadUInt16s(ptr+index, count);
  }

  /// <summary>Reads an array of unsigned two-byte integers from the stream.</summary>
  [CLSCompliant(false)]
  public void ReadUInt16s(ushort* array, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    Read(array, count*sizeof(ushort));
    MakeSystemEndian2(array, count);
  }

  /// <summary>Reads an array of signed four-byte integers from the stream.</summary>
  public int[] ReadInt32s(int count)
  {
    int[] data = new int[count];
    if(count != 0) fixed(int* ptr=data) ReadInt32s(ptr, count);
    return data;
  }

  /// <summary>Reads an array of signed four-byte integers from the stream.</summary>
  public void ReadInt32s(int[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    if(count != 0) fixed(int* ptr=array) ReadInt32s(ptr+index, count);
  }

  /// <summary>Reads an array of signed four-byte integers from the stream.</summary>
  [CLSCompliant(false)]
  public void ReadInt32s(int* array, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    Read(array, count*sizeof(uint));
    MakeSystemEndian4(array, count);
  }

  /// <summary>Reads an array of unsigned four-byte integers from the stream.</summary>
  [CLSCompliant(false)]
  public uint[] ReadUInt32s(int count)
  {
    uint[] data = new uint[count];
    if(count != 0) fixed(uint* ptr=data) ReadUInt32s(ptr, count);
    return data;
  }

  /// <summary>Reads an array of unsigned four-byte integers from the stream.</summary>
  [CLSCompliant(false)]
  public void ReadUInt32s(uint[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    if(count != 0) fixed(uint* ptr=array) ReadUInt32s(ptr+index, count);
  }

  /// <summary>Reads an array of unsigned four-byte integers from the stream.</summary>
  [CLSCompliant(false)]
  public void ReadUInt32s(uint* array, int count)
  {
    ReadInt32s((int*)array, count);
  }

  /// <summary>Reads an array of signed eight-byte integers from the stream.</summary>
  public long[] ReadInt64s(int count)
  {
    long[] data = new long[count];
    if(count != 0) fixed(long* ptr=data) ReadInt64s(ptr, count);
    return data;
  }

  /// <summary>Reads an array of signed eight-byte integers from the stream.</summary>
  public void ReadInt64s(long[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    if(count != 0) fixed(long* ptr=array) ReadInt64s(ptr+index, count);
  }

  /// <summary>Reads an array of signed eight-byte integers from the stream.</summary>
  [CLSCompliant(false)]
  public void ReadInt64s(long* array, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    Read(array, count*sizeof(long));
    MakeSystemEndian8(array, count);
  }

  /// <summary>Reads an array of unsigned eight-byte integers from the stream.</summary>
  [CLSCompliant(false)]
  public ulong[] ReadUInt64s(int count)
  {
    ulong[] data = new ulong[count];
    if(count != 0) fixed(ulong* ptr=data) ReadUInt64s(ptr, count);
    return data;
  }

  /// <summary>Reads an array of unsigned eight-byte integers from the stream.</summary>
  [CLSCompliant(false)]
  public void ReadUInt64s(ulong[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    if(count != 0) fixed(ulong* ptr=array) ReadUInt64s(ptr+index, count);
  }

  /// <summary>Reads an array of unsigned eight-byte integers from the stream.</summary>
  [CLSCompliant(false)]
  public void ReadUInt64s(ulong* array, int count)
  {
    ReadInt64s((long*)array, count);
  }

  /// <summary>Reads a nullable <see cref="DateTime"/> object from the binary reader with automatic time zone adjustment.
  /// The value is assumed to have been written with <see cref="BinaryWriter.WriteAdjustableDateTime(DateTime?)"/>.
  /// </summary>
  /// <remarks>If the value is based on the local time zone, it will be automatically adjusted to the new local time zone when
  /// deserialized. (For instance, 3PM serialized in PST may become 6PM when deserialized in EST.) Times based on UTC are unaffected.
  /// </remarks>
  public DateTime? ReadNullableAdjustableDateTime()
  {
    if(!ReadBoolean()) return null;
    else return ReadAdjustableDateTime();
  }

  /// <summary>Reads a one-byte nullable boolean value from the stream in the format written by <see cref="BinaryWriter.Write(bool?)"/>.</summary>
  /// <remarks>The value is read as 0=false, 1=true, &gt;1=null.</remarks>
  public bool? ReadNullableBoolean()
  {
    byte v = ReadByte();
    if(v > 1) return null;
    else return v != 0;
  }

  /// <summary>Reads a nullable, signed byte value from the stream as a boolean potentially followed by a value.</summary>
  [CLSCompliant(false)]
  public sbyte? ReadNullableSByte()
  {
    if(!ReadBoolean()) return null;
    else return ReadSByte();
  }

  /// <summary>Reads a nullable, unsigned byte value from the stream as a boolean potentially followed by a value.</summary>
  public byte? ReadNullableByte()
  {
    if(!ReadBoolean()) return null;
    else return ReadByte();
  }

  /// <summary>Reads a nullable character from the stream as a boolean potentially followed by a value, using the default encoding.</summary>
  public char? ReadNullableChar()
  {
    if(!ReadBoolean()) return null;
    else return ReadChar();
  }

  /// <summary>Reads a nullable character from the stream as a boolean potentially followed by a value, using the default encoding.</summary>
  public char? ReadNullableChar(Encoding encoding)
  {
    if(!ReadBoolean()) return null;
    else return ReadChar(encoding);
  }

  /// <summary>Reads a nullable <see cref="DateTime"/> value from the stream as a boolean potentially followed by a value.</summary>
  public DateTime? ReadNullableDateTime()
  {
    if(!ReadBoolean()) return null;
    else return ReadDateTime();
  }

  /// <summary>Reads a nullable <see cref="Guid"/> from the stream as a boolean potentially followed by a value.</summary>
  public Guid? ReadNullableGuid()
  {
    if(!ReadBoolean()) return null;
    else return ReadGuid();
  }

  /// <summary>Reads a nullable, signed two-byte integer from the stream as a boolean potentially followed by a value.</summary>
  public short? ReadNullableInt16()
  {
    if(!ReadBoolean()) return null;
    else return ReadInt16();
  }

  /// <summary>Reads a nullable, unsigned two-byte integer from the stream as a boolean potentially followed by a value.</summary>
  [CLSCompliant(false)]
  public ushort? ReadNullableUInt16()
  {
    if(!ReadBoolean()) return null;
    else return ReadUInt16();
  }

  /// <summary>Reads a nullable, signed four-byte integer from the stream as a boolean potentially followed by a value.</summary>
  public int? ReadNullableInt32()
  {
    if(!ReadBoolean()) return null;
    else return ReadInt32();
  }

  /// <summary>Reads a nullable, unsigned four-byte integer from the stream as a boolean potentially followed by a value.</summary>
  [CLSCompliant(false)]
  public uint? ReadNullableUInt32()
  {
    if(!ReadBoolean()) return null;
    else return ReadUInt32();
  }

  /// <summary>Reads a nullable, signed eight-byte integer from the stream as a boolean potentially followed by a value.</summary>
  public long? ReadNullableInt64()
  {
    if(!ReadBoolean()) return null;
    else return ReadInt64();
  }

  /// <summary>Reads a nullable, unsigned eight-byte integer from the stream as a boolean potentially followed by a value.</summary>
  [CLSCompliant(false)]
  public ulong? ReadNullableUInt64()
  {
    if(!ReadBoolean()) return null;
    else return ReadUInt64();
  }

  /// <summary>Reads a nullable, four-byte float from the stream as a boolean potentially followed by a value.</summary>
  public float? ReadNullableSingle()
  {
    if(!ReadBoolean()) return null;
    else return ReadSingle();
  }

  /// <summary>Reads a nullable, eight-byte float from the stream as a boolean potentially followed by a value.</summary>
  public double? ReadNullableDouble()
  {
    if(!ReadBoolean()) return null;
    else return ReadDouble();
  }

  /// <summary>Reads a nullable <see cref="decimal"/> from the stream as a boolean potentially followed by a value.</summary>
  public decimal? ReadNullableDecimal()
  {
    if(!ReadBoolean()) return null;
    else return ReadDecimal();
  }

  /// <summary>Reads an array of four-byte floats from the stream.</summary>
  public float[] ReadSingles(int count)
  {
    float[] data = new float[count];
    if(count != 0) fixed(float* ptr=data) ReadSingles(ptr, count);
    return data;
  }

  /// <summary>Reads an array of four-byte floats from the stream.</summary>
  public void ReadSingles(float[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    if(count != 0) fixed(float* ptr=array) ReadSingles(ptr+index, count);
  }

  /// <summary>Reads an array of unsigned four-byte integers from the stream.</summary>
  [CLSCompliant(false)]
  public void ReadSingles(float* array, int count)
  {
    ReadInt32s((int*)array, count);
  }

  /// <summary>Reads an array of eight-byte floats from the stream.</summary>
  public double[] ReadDoubles(int count)
  {
    double[] data = new double[count];
    if(count != 0) fixed(double* ptr=data) ReadDoubles(ptr, count);
    return data;
  }

  /// <summary>Reads an array of eight-byte floats from the stream.</summary>
  public void ReadDoubles(double[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    if(count != 0) fixed(double* ptr=array) ReadDoubles(ptr+index, count);
  }

  /// <summary>Reads an array of unsigned eight-byte integers from the stream.</summary>
  [CLSCompliant(false)]
  public void ReadDoubles(double* array, int count)
  {
    ReadInt64s((long*)array, count);
  }

  /// <summary>Reads an array of 13-byte <see cref="decimal"/> values from the stream.</summary>
  public decimal[] ReadDecimals(int count)
  {
    decimal[] array = new decimal[count];
    ReadDecimals(array, 0, count);
    return array;
  }

  /// <summary>Reads an array of 13-byte <see cref="decimal"/> values from the stream.</summary>
  public void ReadDecimals(decimal[] array, int index, int count)
  {
    Utility.ValidateRange(array, index, count);
    for(int end=index+count; index < end; index++) array[index] = ReadDecimal();
  }

  /// <summary>Reads a string stored as an array of encoded bytes from the stream.</summary>
  /// <param name="nbytes">The number of bytes to read.</param>
  /// <returns>A string containing the characters decoded from the stream.</returns>
  public string ReadString(int nbytes)
  {
    return ReadString(nbytes, DefaultEncoding);
  }

  /// <summary>Reads a string stored as an array of encoded bytes from the stream.</summary>
  /// <param name="nbytes">The number of bytes to read.</param>
  /// <param name="encoding">The <see cref="Encoding"/> used to decode the bytes.</param>
  /// <returns>A string containing the characters decoded from the stream.</returns>
  public string ReadString(int nbytes, Encoding encoding)
  {
    if(encoding == null) throw new ArgumentNullException();
    if(nbytes < 0) throw new ArgumentOutOfRangeException();

    if(nbytes == 0) return string.Empty;
    // if it all fits into the buffer, then read it directly out of the buffer
    else if(nbytes <= Buffer.Length) return encoding.GetString(Buffer, (int)(ReadContiguousData(nbytes)-BufferPtr), nbytes);
    else return encoding.GetString(ReadBytes(nbytes)); // otherwise, read it into a temporary array
  }

  /// <summary>Reads a string that was written by <see cref="BinaryWriter.WriteNullableString"/> or
  /// <see cref="BinaryReader.WriteStringWithLength"/>.
  /// </summary>
  public string ReadNullableString()
  {
    return ReadNullableString(DefaultEncoding);
  }

  /// <summary>Reads a string that was written by <see cref="BinaryWriter.WriteNullableString"/> or
  /// <see cref="BinaryReader.WriteStringWithLength"/>.
  /// </summary>
  public string ReadNullableString(Encoding encoding)
  {
    uint length = ReadEncodedUInt32();
    return length == 0 ? null : ReadString((int)(length-1), encoding);
  }

  /// <summary>Reads a number of strings. The strings are assumed to have been written with <see cref="BinaryWriter.WriteNullableString(string)" />.</summary>
  public string[] ReadNullableStrings(int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    string[] values = new string[count];
    for(int i=0; i<values.Length; i++) values[i] = ReadNullableString();
    return values;
  }

  /// <summary>Advances the reader by the given number of bytes.</summary>
  public void Skip(int nbytes)
  {
    if(nbytes < 0) throw new ArgumentOutOfRangeException();
    int advance = Math.Min(AvailableData, nbytes);
    AdvanceTail(advance);
    nbytes -= advance;
    if(nbytes != 0)
    {
      if(ExternalBuffer) throw new EndOfStreamException();
      BaseStream.Skip(nbytes);
    }
  }

  /// <summary>Skips a string that was written by <see cref="BinaryWriter.WriteNullableString"/>.</summary>
  public void SkipNullableString()
  {
    int length = (int)ReadEncodedUInt32() - 1; // subtract one because null is represented by zero, empty as 1, etc.
    if(length > 0) Skip(length);
  }

  /// <summary>Overrides <see cref="PinnedBuffer.CreateResizeBuffer"/> to properly resize the circular array that we're using.</summary>
  protected override byte[] CreateResizeBuffer(int newSize)
  {
    byte[] newBuffer = new byte[newSize];

    // copy the available data so it starts at the beginning of the new buffer
    int availableData = AvailableData;
    if(tailIndex <= headIndex)
    {
      Array.Copy(Buffer, tailIndex, newBuffer, 0, availableData);
    }
    else
    {
      Array.Copy(Buffer, tailIndex, newBuffer, 0, Buffer.Length - tailIndex);
      Array.Copy(Buffer, 0, newBuffer, tailIndex, headIndex);
    }

    tailIndex = 0; // fixup the indices to reflect that the data has been moved to the beginning
    headIndex = availableData;

    return newBuffer;
  }

  /// <inheritdoc/>
  protected override void Dispose(bool manualDispose)
  {
    if(!ExternalBuffer && BaseStream.CanSeek)
    {
      BaseStream.Position = Position; // set the stream position to the end of the data read from the reader.
                                      // this way, the stream is not positioned at some seemingly random place.
    }
    base.Dispose(manualDispose);
  }

  /// <summary>Gets the amount of data available in the buffer.</summary>
  protected int AvailableData
  {
    get { return tailIndex <= headIndex ? headIndex - tailIndex : headIndex + Buffer.Length-tailIndex; }
  }

  /// <summary>Gets the amount of contiguous data available at the front of the circular array.</summary>
  protected int ContiguousData
  {
    get { return (tailIndex <= headIndex ? headIndex : Buffer.Length) - tailIndex; }
  }

  /// <summary>Ensures that the given number of bytes are available in a contiguous form in the buffer.</summary>
  protected void EnsureContiguousData(int nbytes)
  {
    if(ContiguousData < nbytes) // if there's not enough contiguous data, read more data and/or shift existing data
    {
      if(ExternalBuffer) throw new EndOfStreamException(); // with an external buffer, there's no more data to read

      if(Buffer.Length < nbytes) // if the buffer simply isn't big enough, we'll first enlarge it
      {
        EnsureCapacity(nbytes);
        if(ContiguousData >= nbytes) return; // enlarging the buffer compacts the data, and there is enough now
      }

      // find out how many contiguous bytes we can fit without shifting data around
      int availableContiguousSpace = Buffer.Length - tailIndex;
      if(availableContiguousSpace < nbytes) // if it's not enough, we'll need to shift the data
      {
        if(tailIndex < headIndex) // if the data is in one chunk, we can simply move the chunk
        {
          Array.Copy(Buffer, tailIndex, Buffer, 0, headIndex-tailIndex);
        }
        else // otherwise we'll need to move two chunks, using a temporary storage space to hold one of them
        {
          // this is an edge case, so we'll always choose the second chunk for placement in temporary storage.
          // there are potential optimizations that can be done.
          byte[] temp = new byte[headIndex];
          Array.Copy(Buffer, temp, headIndex);
          Array.Copy(Buffer, tailIndex, Buffer, 0, Buffer.Length - tailIndex);
          Array.Copy(temp, 0, Buffer, Buffer.Length - tailIndex, headIndex);
        }
        headIndex = AvailableData; // update the pointers to indicate that the data has been defragmented
        tailIndex = 0;

        availableContiguousSpace = Buffer.Length; // now we have a contiguous region the size of the whole buffer
      }

      int toRead = availableContiguousSpace - headIndex; // fill the entire contiguous region
      if(toRead > nbytes && !BaseStream.CanSeek) toRead = nbytes; // avoid reading too much from nonseekable streams
      do
      {
        int read = BaseStream.Read(Buffer, headIndex, toRead);
        if(read == 0)
        {
          if(AvailableData < nbytes) throw new EndOfStreamException();
          break;
        }
        headIndex += read;
        toRead -= read;
      } while(toRead != 0);
    }
  }

  /// <summary>Ensures that the data, which must consist of two-byte integers, has system endianness.</summary>
  /// <param name="data">A pointer to the data.</param>
  /// <param name="nwords">The number of words to potentially swap.</param>
  [CLSCompliant(false)]
  protected void MakeSystemEndian2(void* data, int nwords)
  {
    if(LittleEndian != BitConverter.IsLittleEndian) SwapEndian2(data, nwords);
  }

  /// <summary>Ensures that the data, which must consist of four-byte integers, has system endianness.</summary>
  /// <param name="data">A pointer to the data.</param>
  /// <param name="dwords">The number of doublewords to potentially swap.</param>
  [CLSCompliant(false)]
  protected void MakeSystemEndian4(void* data, int dwords)
  {
    if(LittleEndian != BitConverter.IsLittleEndian) SwapEndian4(data, dwords);
  }

  /// <summary>Ensures that the data, which must consist of eight-byte integers, has system endianness.</summary>
  /// <param name="data">A pointer to the data.</param>
  /// <param name="qwords">The number of quadwords to potentially swap.</param>
  [CLSCompliant(false)]
  protected void MakeSystemEndian8(void* data, int qwords)
  {
    if(LittleEndian != BitConverter.IsLittleEndian) SwapEndian8(data, qwords);
  }

  /// <summary>Ensures that the given number of bytes are available in a contiguous form in the buffer, returns a
  /// pointer to them, and advances past them.
  /// </summary>
  /// <param name="nbytes">The number of contiguous bytes to read.</param>
  /// <returns>A pointer to the bytes requested.</returns>
  /// <remarks>The buffer size will be enlarged if necessary, but this method is better suited for small reads. For
  /// larger reads, consider using <see cref="ReadData"/>, which will not enlarge the buffer.
  /// </remarks>
  [CLSCompliant(false)]
  protected byte* ReadContiguousData(int nbytes)
  {
    EnsureContiguousData(nbytes);
    byte* data = BufferPtr + tailIndex;
    AdvanceTail(nbytes);
    return data;
  }

  /// <summary>Advances the tail by the given number of bytes. It's assumed that this is not greater than
  /// <see cref="ContiguousData"/>.
  /// </summary>
  void AdvanceTail(int nbytes)
  {
    tailIndex += nbytes;

    Debug.Assert(tailIndex <= Buffer.Length);

    // if the buffer becomes empty due to this move, put the pointers back at the front of the buffer to ensure that
    // we have as much contiguous space as possible. we don't do this with external buffers, though, because we can't
    // read any more data into them.
    if(tailIndex == headIndex && !ExternalBuffer) headIndex = tailIndex = 0;
  }

  void ReadDataInternal(ref byte* ptr, ref int bytesNeeded, int bytesAvailable)
  {
    Unsafe.Copy(BufferPtr+tailIndex, ptr, bytesAvailable);
    AdvanceTail(bytesAvailable);
    ptr += bytesAvailable;
    bytesNeeded -= bytesAvailable;
  }

  int headIndex, tailIndex;
  readonly int startIndex;
  Encoding encoding;
  Decoder decoder;
}
#endregion

#region BinaryWriter
/// <summary>This class makes it easy to efficiently serialize values into a stream or array.</summary>
/// <remarks>This class is not safe for use by multiple threads concurrently.</remarks>
public unsafe class BinaryWriter : BinaryReaderWriterBase
{
  /// <summary>Initializes this <see cref="BinaryWriter"/> with the default buffer size, little-endianness, and the assumption that the
  /// stream will not be accessed by any other classes while this writer is in use. The stream will be closed when the
  /// <see cref="BinaryWriter"/> is disposed.
  /// </summary>
  /// <param name="stream">The stream to which data will be written.</param>
  public BinaryWriter(Stream stream) : this(stream, true, true) { }

  /// <summary>Initializes this <see cref="BinaryWriter"/> with the default buffer size, little-endianness, and the
  /// assumption that the stream will not be accessed by any other classes while this writer is in use.
  /// </summary>
  /// <param name="stream">The stream to which data will be written.</param>
  /// <param name="ownStream">If true, the stream will be closed when the <see cref="BinaryWriter"/> is disposed.</param>
  public BinaryWriter(Stream stream, bool ownStream) : this(stream, ownStream, true) { }

  /// <summary>Initializes this <see cref="BinaryWriter"/> with the default buffer size and the
  /// assumption that the stream will not be accessed by any other classes while this writer is in use.
  /// </summary>
  /// <param name="stream">The stream to which data will be written.</param>
  /// <param name="ownStream">If true, the stream will be closed when the <see cref="BinaryWriter"/> is disposed.</param>
  /// <param name="littleEndian">Whether the data being written is little endian. This can be changed at any time using
  /// the <see cref="LittleEndian"/> property.
  /// </param>
  public BinaryWriter(Stream stream, bool ownStream, bool littleEndian) : this(stream, ownStream, littleEndian, 0) { }

  /// <summary>Initializes this <see cref="BinaryWriter"/>.</summary>
  /// <param name="stream">The stream to which data will be written. If the stream does its own buffering, it may be
  /// more efficient to eliminate the buffer from the stream, so that the data is not buffered multiple times.
  /// </param>
  /// <param name="ownStream">If true, the stream will be closed when the <see cref="BinaryWriter"/> is disposed.</param>
  /// <param name="littleEndian">Whether the data should be written with little endianness. This can be changed at any
  /// time using the <see cref="LittleEndian"/> property.
  /// </param>
  /// <param name="bufferSize">The buffer size. If zero, a default buffer size of 4096 bytes will be used. The default
  /// buffer size is usually sufficient, but if you know approximately how much data you'll be writing to the stream,
  /// you can tune the buffer size so that the <see cref="BinaryWriter"/> does not allocate more memory than is
  /// necessary.
  /// </param>
  public BinaryWriter(Stream stream, bool ownStream, bool littleEndian, int bufferSize) : base(stream, ownStream, littleEndian, bufferSize)
  {
    DefaultEncoding = Encoding.UTF8;
  }

  /// <summary>Initializes this <see cref="BinaryWriter"/> to write into the given array with little endianness.</summary>
  public BinaryWriter(byte[] array) : this(array, 0, array.Length, true) { }

  /// <summary>Initializes this <see cref="BinaryWriter"/> to write into the given array with little endianness.</summary>
  /// <param name="index">The beginning of the area of the array to which the writer can write.</param>
  /// <param name="length">The length of the area to which the writer can write.</param>
  public BinaryWriter(byte[] array, int index, int length) : this(array, index, length, true) { }

  /// <summary>Initializes this <see cref="BinaryWriter"/> to write into the given array with the given endianness.</summary>
  /// <param name="index">The beginning of the area of the array to which the writer can write.</param>
  /// <param name="length">The length of the area to which the writer can write.</param>
  public BinaryWriter(byte[] array, int index, int length, bool littleEndian)
    : base(array, index, length, littleEndian)
  {
    this.startIndex   = this.writeIndex = index;
    this.bufferLength = length;
    DefaultEncoding   = Encoding.UTF8;
  }

  /// <summary>Gets or sets the default encoding used to transform characters and strings into bytes.</summary>
  public Encoding DefaultEncoding
  {
    get { return encoding; }
    set
    {
      if(value == null) throw new ArgumentNullException();
      encoding = value;
    }
  }

  /// <summary>Gets or sets the position of the writer. This is equal to the position of the underlying stream, plus
  /// the amount of data in the buffer.
  /// </summary>
  public long Position
  {
    get { return ExternalBuffer ? writeIndex-startIndex : BaseStream.Position+writeIndex; }
    set
    {
      if(ExternalBuffer)
      {
        if(value < 0 || value > bufferLength) throw new ArgumentOutOfRangeException();
        writeIndex = (int)value + startIndex;
      }
      else if(value != Position)
      {
        FlushBuffer();
        BaseStream.Position = value;
      }
    }
  }

  /// <summary>Writes a boolean value to the stream as a single byte.</summary>
  public void Write(bool value)
  {
    Write(value ? (byte)1 : (byte)0);
  }

  /// <summary>Writes a nullable boolean value to the stream as a single byte that can be read by
  /// <see cref="BinaryReader.ReadNullableBoolean"/>.
  /// </summary>
  /// <remarks>The value is written as 0=false, 1=true, 2=null.</remarks>
  public void Write(bool? value)
  {
    Write(!value.HasValue ? (byte)2 : value.GetValueOrDefault() ? (byte)1 : (byte)0);
  }

  /// <summary>Writes a single signed byte to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(sbyte value)
  {
    Write((byte)value);
  }

  /// <summary>Writes a nullable, signed byte to the stream as a boolean potentially followed by the value.</summary>
  [CLSCompliant(false)]
  public void Write(sbyte? value)
  {
    Write(value.HasValue);
    if(value.HasValue) Write(value.GetValueOrDefault());
  }

  /// <summary>Writes a single byte to the stream.</summary>
  public void Write(byte value)
  {
    if(ExternalBuffer)
    {
      if(writeIndex == bufferLength) RaiseFullError(); // if it's an external buffer, we can't enlarge it any more
    }
    else if(Buffer.Length == writeIndex) FlushBuffer();
    BufferPtr[writeIndex++] = value;
  }

  /// <summary>Writes a nullable byte to the stream as a boolean potentially followed by the value.</summary>
  public void Write(byte? value)
  {
    Write(value.HasValue);
    if(value.HasValue) Write(value.GetValueOrDefault());
  }

  /// <summary>Writes a character to the stream, using the default encoding. Returns the number of bytes written to the
  /// stream.
  /// </summary>
  public int Write(char value)
  {
    return Write(value, DefaultEncoding);
  }

  /// <summary>Writes a character to the stream, using the given encoding. Returns the number of bytes written to the stream.</summary>
  public int Write(char value, Encoding encoding)
  {
    if(encoding == null) throw new ArgumentNullException();
    if(char.IsSurrogate(value)) throw new ArgumentException();

    // we assume that all characters can fit in 16 bytes (the built-in BinaryWriter makes the same assumption)
    byte* buffer = stackalloc byte[16];
    int bytes = encoding.GetBytes(&value, 1, buffer, 16);
    WriteCore(buffer, bytes);
    return bytes;
  }

  /// <summary>Writes a nullable character to the stream as a boolean potentially followed by the value, using the default encoding.</summary>
  public void Write(char? value)
  {
    Write(value, DefaultEncoding);
  }

  /// <summary>Writes a nullable character to the stream as a boolean potentially followed by the value, using the given encoding.</summary>
  public void Write(char? value, Encoding encoding)
  {
    Write(value.HasValue);
    if(value.HasValue) Write(value.GetValueOrDefault(), encoding);
  }

  /// <summary>Writes a signed two-byte integer to the stream.</summary>
  public void Write(short value)
  {
    Write((ushort)value);
  }

  /// <summary>Writes a nullable, signed two-byte integer to the stream as a boolean potentially followed by the value.</summary>
  public void Write(short? value)
  {
    Write(value.HasValue);
    if(value.HasValue) Write(value.GetValueOrDefault());
  }

  /// <summary>Writes an unsigned two-byte integer to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(ushort value)
  {
    EnsureSpace(sizeof(ushort));
    if(LittleEndian == BitConverter.IsLittleEndian) *(ushort*)WritePtr = value;
    else if(LittleEndian) IOH.WriteLE2U(WritePtr, value);
    else IOH.WriteBE2U(WritePtr, value);
    writeIndex += sizeof(ushort);
  }

  /// <summary>Writes a nullable, unsigned two-byte integer to the stream as a boolean potentially followed by the value.</summary>
  [CLSCompliant(false)]
  public void Write(ushort? value)
  {
    Write(value.HasValue);
    if(value.HasValue) Write(value.GetValueOrDefault());
  }

  /// <summary>Writes a signed four-byte integer to the stream.</summary>
  public void Write(int value)
  {
    EnsureSpace(sizeof(int));
    if(LittleEndian == BitConverter.IsLittleEndian) *(int*)WritePtr = value;
    else if(LittleEndian) IOH.WriteLE4(WritePtr, value);
    else IOH.WriteBE4(WritePtr, value);
    writeIndex += sizeof(int);
  }

  /// <summary>Writes a nullable, signed four-byte integer to the stream as a boolean potentially followed by the value.</summary>
  public void Write(int? value)
  {
    Write(value.HasValue);
    if(value.HasValue) Write(value.GetValueOrDefault());
  }

  /// <summary>Writes an unsigned four-byte integer to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(uint value)
  {
    Write((int)value);
  }


  /// <summary>Writes a nullable, unsigned four-byte integer to the stream as a boolean potentially followed by the value.</summary>
  [CLSCompliant(false)]
  public void Write(uint? value)
  {
    Write(value.HasValue);
    if(value.HasValue) Write(value.GetValueOrDefault());
  }

  /// <summary>Writes a signed eight-byte integer to the stream.</summary>
  public void Write(long value)
  {
    EnsureSpace(sizeof(long));
    if(LittleEndian == BitConverter.IsLittleEndian) *(long*)WritePtr = value;
    else if(LittleEndian) IOH.WriteLE8(WritePtr, value);
    else IOH.WriteBE8(WritePtr, value);
    writeIndex += sizeof(long);
  }

  /// <summary>Writes a nullable, signed eight-byte integer to the stream as a boolean potentially followed by the value.</summary>
  public void Write(long? value)
  {
    Write(value.HasValue);
    if(value.HasValue) Write(value.GetValueOrDefault());
  }

  /// <summary>Writes an unsigned eight-byte integer to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(ulong value)
  {
    Write((long)value);
  }

  /// <summary>Writes a nullable, unsigned eight-byte integer to the stream as a boolean potentially followed by the value.</summary>
  [CLSCompliant(false)]
  public void Write(ulong? value)
  {
    Write(value.HasValue);
    if(value.HasValue) Write(value.GetValueOrDefault());
  }

  /// <summary>Writes a four-byte float to the stream.</summary>
  public void Write(float value)
  {
    EnsureSpace(sizeof(float));
    if(LittleEndian == BitConverter.IsLittleEndian) *(float*)WritePtr = value;
    else if(LittleEndian) IOH.WriteLESingle(WritePtr, value);
    else IOH.WriteBESingle(WritePtr, value);
    writeIndex += sizeof(float);
  }

  /// <summary>Writes a nullable four-byte float to the stream as a boolean potentially followed by the value.</summary>
  public void Write(float? value)
  {
    Write(value.HasValue);
    if(value.HasValue) Write(value.GetValueOrDefault());
  }

  /// <summary>Writes an eight-byte float to the stream.</summary>
  public void Write(double value)
  {
    EnsureSpace(sizeof(double));
    if(LittleEndian == BitConverter.IsLittleEndian) *(double*)WritePtr = value;
    else if(LittleEndian) IOH.WriteLEDouble(WritePtr, value);
    else IOH.WriteBEDouble(WritePtr, value);
    writeIndex += sizeof(double);
  }

  /// <summary>Writes a nullable eight-byte float to the stream as a boolean potentially followed by the value.</summary>
  public void Write(double? value)
  {
    Write(value.HasValue);
    if(value.HasValue) Write(value.GetValueOrDefault());
  }

  /// <summary>Writes a 13-byte <see cref="decimal"/> value to the stream.</summary>
  public void Write(decimal value)
  {
    int[] array = decimal.GetBits(value);
    Write(array[0]);
    Write(array[1]);
    Write(array[2]);
    int scale = array[3];
    Write((byte)((scale>>16) | (scale>>24)));
  }

  /// <summary>Writes a nullable <see cref="decimal"/> to the stream as a boolean potentially followed by the value.</summary>
  public void Write(decimal? value)
  {
    Write(value.HasValue);
    if(value.HasValue) Write(value.GetValueOrDefault());
  }

  /// <summary>Writes an array of signed bytes to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(sbyte[] data)
  {
    if(data == null) throw new ArgumentNullException();
    Write(data, 0, data.Length);
  }

  /// <summary>Writes an array of signed bytes to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(sbyte[] data, int index, int count)
  {
    Utility.ValidateRange(data, index, count);
    if(count != 0) fixed(sbyte* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of bytes to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(sbyte* data, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    WriteCore((byte*)data, count);
  }

  /// <summary>Writes an array of bytes to the stream.</summary>
  public void Write(byte[] data)
  {
    if(data == null) throw new ArgumentNullException();
    Write(data, 0, data.Length);
  }

  /// <summary>Writes an array of bytes to the stream.</summary>
  public void Write(byte[] data, int index, int count)
  {
    Utility.ValidateRange(data, index, count);
    if(count != 0) fixed(byte* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of bytes to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(byte* data, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    WriteCore(data, count);
  }

  /// <summary>Writes an array of characters to the stream, using the default encoding. Returns the number of bytes written to
  /// the stream.
  /// </summary>
  public int Write(char[] chars)
  {
    if(chars == null) throw new ArgumentNullException();
    return Write(chars, 0, chars.Length, DefaultEncoding);
  }

  /// <summary>Writes an array of characters to the stream, with the given encoding. Returns the number of bytes written to
  /// the stream.
  /// </summary>
  public int Write(char[] chars, Encoding encoding)
  {
    if(chars == null || encoding == null) throw new ArgumentNullException();
    return Write(chars, 0, chars.Length, encoding);
  }

  /// <summary>Writes an array of characters to the stream, using the default encoding. Returns the number of bytes written to
  /// the stream.
  /// </summary>
  public int Write(char[] chars, int index, int count)
  {
    return Write(chars, index, count, DefaultEncoding);
  }

  /// <summary>Writes an array of characters to the stream, using the given encoding. Returns the number of bytes written to
  /// the stream.
  /// </summary>
  public int Write(char[] chars, int index, int count, Encoding encoding)
  {
    Utility.ValidateRange(chars, index, count);
    if(count == 0) return 0; // zero-length arrays yield a null-pointer when fixed, so avoid passing a null pointer
    else fixed(char* charPtr=chars) return Write(charPtr+index, count, encoding);
  }

  /// <summary>Writes an array of characters to the stream, using the default encoding. Returns the number of bytes written to
  /// the stream.
  /// </summary>
  [CLSCompliant(false)]
  public int Write(char* data, int count)
  {
    return Write(data, count, DefaultEncoding);
  }

  /// <summary>Writes an array of characters to the stream, using the given encoding. Returns the number of bytes written to
  /// the stream.
  /// </summary>
  [CLSCompliant(false)]
  public int Write(char* data, int count, Encoding encoding)
  {
    if(encoding == null) throw new ArgumentNullException();
    if(count < 0) throw new ArgumentOutOfRangeException();
    if(count == 0) return 0;

    int spaceNeeded = encoding.GetMaxByteCount(count);
    if(spaceNeeded <= StackAllocThreshold)
    {
      byte* buffer = stackalloc byte[spaceNeeded];
      spaceNeeded = encoding.GetBytes(data, count, buffer, spaceNeeded);
      WriteCore(buffer, spaceNeeded);
    }
    else
    {
      int chunkSize = StackAllocThreshold;
      while(chunkSize != 0 && encoding.GetMaxByteCount(chunkSize) > StackAllocThreshold) chunkSize /= 2;
      if(chunkSize == 0) throw new NotSupportedException();

      Encoder encoder = encoding.GetEncoder();
      int bufferSize = encoding.GetMaxByteCount(chunkSize);
      byte* buffer = stackalloc byte[bufferSize];

      spaceNeeded = 0;
      while(true)
      {
        int charsToRead = Math.Min(count, chunkSize);
        int bytesWritten = encoder.GetBytes(data, charsToRead, buffer, bufferSize, count == 0);
        WriteCore(buffer, bytesWritten);
        spaceNeeded += bytesWritten;
        if(count == 0) break;

        data  += charsToRead;
        count -= charsToRead;
      }
    }
    return spaceNeeded;
  }

  /// <summary>Writes a <see cref="DateTime"/> to the stream as a sequence of 8 bytes. The <see cref="DateTime"/> may later be
  /// read with <see cref="BinaryReader.ReadDateTime"/>.
  /// </summary>
  public void Write(DateTime dateTime)
  {
    // Ticks requires 62 bits and Kind requires 2 bits, so the whole thing can be packed into 64 bits
    Write(((ulong)dateTime.Ticks << 2) | (uint)dateTime.Kind);
  }

  /// <summary>Writes a nullable <see cref="DateTime"/> to the stream as a boolean potentially followed by the value.</summary>
  public void Write(DateTime? value)
  {
    Write(value.HasValue);
    if(value.HasValue) Write(value.GetValueOrDefault());
  }

  /// <summary>Writes an array of <see cref="DateTime"/> objects to the stream.</summary>
  public void Write(DateTime[] dateTimes)
  {
    if(dateTimes == null) throw new ArgumentNullException();
    Write(dateTimes, 0, dateTimes.Length);
  }

  /// <summary>Writes an array of <see cref="DateTime"/> objects to the stream.</summary>
  public void Write(DateTime[] dateTimes, int index, int count)
  {
    Utility.ValidateRange(dateTimes, index, count);
    for(int end = index+count; index < end; index++) Write(dateTimes[index]);
  }

  /// <summary>Writes a <see cref="Guid"/> to the stream as a sequence of 16 bytes. The <see cref="Guid"/> may later be read
  /// with <see cref="BinaryReader.ReadGuid"/>.
  /// </summary>
  public unsafe void Write(Guid guid)
  {
    // this assumes that Guid is laid out the right way in memory. it is in Microsoft's implementation
    WriteCore((byte*)&guid, 16);
  }

  /// <summary>Writes a nullable <see cref="Guid"/> to the stream as a boolean potentially followed by the value.</summary>
  public void Write(Guid? value)
  {
    Write(value.HasValue);
    if(value.HasValue) Write(value.GetValueOrDefault());
  }

  /// <summary>Writes an array of <see cref="Guid"/> objects to the stream.</summary>
  public void Write(Guid[] guids)
  {
    if(guids == null) throw new ArgumentNullException();
    Write(guids, 0, guids.Length);
  }

  /// <summary>Writes an array of <see cref="Guid"/> objects to the stream.</summary>
  public unsafe void Write(Guid[] guids, int index, int count)
  {
    Utility.ValidateRange(guids, index, count);
    if(count != 0) fixed(Guid* ptr=guids) WriteCore((byte*)(ptr+index), count*16L);
  }

  /// <summary>Writes an array of signed two-byte integers to the stream.</summary>
  public void Write(short[] data)
  {
    if(data == null) throw new ArgumentNullException();
    Write(data, 0, data.Length);
  }

  /// <summary>Writes an array of signed two-byte integers to the stream.</summary>
  public void Write(short[] data, int index, int count)
  {
    Utility.ValidateRange(data, index, count);
    if(count != 0) fixed(short* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of signed two-byte integers to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(short* data, int count)
  {
    Write((ushort*)data, count);
  }

  /// <summary>Writes an array of unsigned two-byte integers to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(ushort[] data)
  {
    if(data == null) throw new ArgumentNullException();
    Write(data, 0, data.Length);
  }

  /// <summary>Writes an array of unsigned two-byte integers to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(ushort[] data, int index, int count)
  {
    Utility.ValidateRange(data, index, count);
    if(count != 0) fixed(ushort* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of unsigned two-byte integers to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(ushort* data, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    Write((byte*)data, count, sizeof(ushort));
  }

  /// <summary>Writes an array of signed four-byte integers to the stream.</summary>
  public void Write(int[] data)
  {
    if(data == null) throw new ArgumentNullException();
    Write(data, 0, data.Length);
  }

  /// <summary>Writes an array of signed four-byte integers to the stream.</summary>
  public void Write(int[] data, int index, int count)
  {
    Utility.ValidateRange(data, index, count);
    if(count != 0) fixed(int* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of signed four-byte integers to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(int* data, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    WriteCore((byte*)data, count, sizeof(int));
  }

  /// <summary>Writes an array of unsigned four-byte integers to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(uint[] data)
  {
    if(data == null) throw new ArgumentNullException();
    Write(data, 0, data.Length);
  }

  /// <summary>Writes an array of unsigned four-byte integers to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(uint[] data, int index, int count)
  {
    Utility.ValidateRange(data, index, count);
    if(count != 0) fixed(uint* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of unsigned four-byte integers to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(uint* data, int count)
  {
    Write((int*)data, count);
  }

  /// <summary>Writes an array of signed eight-byte integers to the stream.</summary>
  public void Write(long[] data)
  {
    if(data == null) throw new ArgumentNullException();
    Write(data, 0, data.Length);
  }

  /// <summary>Writes an array of signed eight-byte integers to the stream.</summary>
  public void Write(long[] data, int index, int count)
  {
    Utility.ValidateRange(data, index, count);
    if(count != 0) fixed(long* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of signed eight-byte integers to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(long* data, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    WriteCore((byte*)data, count, sizeof(long));
  }

  /// <summary>Writes an array of unsigned eight-byte integers to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(ulong[] data)
  {
    if(data == null) throw new ArgumentNullException();
    Write(data, 0, data.Length);
  }

  /// <summary>Writes an array of unsigned eight-byte integers to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(ulong[] data, int index, int count)
  {
    Utility.ValidateRange(data, index, count);
    if(count != 0) fixed(ulong* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of unsigned eight-byte integers to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(ulong* data, int count)
  {
    Write((long*)data, count);
  }

  /// <summary>Writes an array of four-byte floats to the stream.</summary>
  public void Write(float[] data)
  {
    if(data == null) throw new ArgumentNullException();
    Write(data, 0, data.Length);
  }

  /// <summary>Writes an array of four-byte floats to the stream.</summary>
  public void Write(float[] data, int index, int count)
  {
    Utility.ValidateRange(data, index, count);
    if(count != 0) fixed(float* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of four-byte floats to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(float* data, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    WriteCore((byte*)data, count, sizeof(float));
  }

  /// <summary>Writes an array of eight-byte floats to the stream.</summary>
  public void Write(double[] data)
  {
    if(data == null) throw new ArgumentNullException();
    Write(data, 0, data.Length);
  }

  /// <summary>Writes an array of eight-byte floats to the stream.</summary>
  public void Write(double[] data, int index, int count)
  {
    Utility.ValidateRange(data, index, count);
    if(count != 0) fixed(double* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of eight-byte floats to the stream.</summary>
  [CLSCompliant(false)]
  public void Write(double* data, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    WriteCore((byte*)data, count, sizeof(double));
  }

  /// <summary>Writes an array of 13-byte decimals to the stream.</summary>
  public void Write(decimal[] data)
  {
    if(data == null) throw new ArgumentNullException();
    Write(data, 0, data.Length);
  }

  /// <summary>Writes an array of 13-byte floats to the stream.</summary>
  public void Write(decimal[] data, int index, int count)
  {
    Utility.ValidateRange(data, index, count);
    for(int end=index+count; index<end; index++) Write(data[index]);
  }

  /// <summary>Writes a string to the stream, using the default encoding. Returns the number of bytes written to the stream.
  /// This method does not accept null values. To write a nullable string, use <see cref="WriteNullableString(string)"/>.
  /// </summary>
  public int Write(string str)
  {
    return Write(str, DefaultEncoding);
  }

  /// <summary>Writes a string to the stream, with the given encoding. Returns the number of bytes written to the stream.
  /// This method does not accept null values. To write a nullable string, use <see cref="WriteNullableString(string,Encoding)"/>.
  /// </summary>
  public int Write(string str, Encoding encoding)
  {
    if(str == null) throw new ArgumentNullException();
    if(str.Length == 0) return 0; // zero-length arrays yield a null-pointer when fixed, so avoid passing a null pointer
    else fixed(char* chars=str) return Write(chars, str.Length, encoding);
  }

  /// <summary>Writes a substring to the stream, using the default encoding. Returns the number of bytes written to the stream.</summary>
  public int Write(string str, int index, int count)
  {
    return Write(str, index, count, DefaultEncoding);
  }

  /// <summary>Writes a substring to the stream, using the given encoding. Returns the number of bytes written to the stream.</summary>
  public int Write(string str, int index, int count, Encoding encoding)
  {
    Utility.ValidateRange(str, index, count);
    if(count == 0) return 0; // zero-length arrays yield a null-pointer when fixed, so avoid passing a null pointer
    else fixed(char* chars=str) return Write(chars+index, count, encoding);
  }

  /// <summary>Writes data from the given region of memory to the stream.</summary>
  /// <param name="data">The location in memory of the data to write.</param>
  /// <param name="count">The number of items to write. Each item has a size of <paramref name="wordSize"/> bytes.</param>
  /// <param name="wordSize">The size of each item to copy. The bytes in each item will be swapped to ensure the
  /// correct endianness. If you don't want any swapping to occur, use a value of 1 for the word size.
  /// </param>
  /// <remarks>This method will not enlarge the buffer unless it is smaller than <paramref name="wordSize"/>. Rather,
  /// it will fill and flush the buffer as many times as is necessary to write the data.
  /// </remarks>
  [CLSCompliant(false)]
  public void Write(void* data, int count, int wordSize)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();

    if(wordSize != 1 && wordSize != 2 && wordSize != 4 && wordSize != 8)
    {
      throw new ArgumentOutOfRangeException("Word size must be 1, 2, 4, or 8.");
    }

    WriteCore((byte*)data, count, wordSize);
  }

  /// <summary>Writes a <see cref="DateTime"/> value with automatic time zone adjustment. This takes 8 bytes.</summary>
  /// <remarks>If the value is based on the local time zone, it will be automatically adjusted to the new local time zone when
  /// deserialized. (For instance, 3PM serialized in PST may become 6PM when deserialized in EST.) Times based on UTC are unaffected.
  /// The value can be deserialized with <see cref="BinaryReader.ReadAdjustableDateTime"/>.
  /// </remarks>
  public void WriteAdjustableDateTime(DateTime value)
  {
    Write(value.ToBinary());
  }

  /// <summary>Writes a nullable <see cref="DateTime"/> value as a boolean potentially followed by the value, with automatic time zone
  /// adjustment.
  /// </summary>
  /// <remarks>If the value is based on the local time zone, it will be automatically adjusted to the new local time zone when
  /// deserialized. (For instance, 3PM serialized in PST may become 6PM when deserialized in EST.) Times based on UTC are unaffected.
  /// The value can be deserialized with <see cref="BinaryReader.ReadNullableAdjustableDateTime"/>.
  /// </remarks>
  public void WriteAdjustableDateTime(DateTime? value)
  {
    Write(value.HasValue);
    if(value.HasValue) WriteAdjustableDateTime(value.GetValueOrDefault());
  }

  /// <summary>Writes an array of <see cref="DateTime"/> objects to the stream with automatic time zone adjustment.</summary>
  /// <remarks>If a value is based on the local time zone, it will be automatically adjusted to the new local time zone when
  /// deserialized. (For instance, 3PM serialized in PST may become 6PM when deserialized in EST.) Times based on UTC are unaffected.
  /// The values can be deserialized with <see cref="BinaryReader.ReadAdjustableDateTimes"/>.
  /// </remarks>
  public void WriteAdjustableDateTimes(DateTime[] dateTimes)
  {
    if(dateTimes == null) throw new ArgumentNullException();
    WriteAdjustableDateTimes(dateTimes, 0, dateTimes.Length);
  }

  /// <summary>Writes an array of <see cref="DateTime"/> objects to the stream with automatic time zone adjustment.</summary>
  /// <remarks>If a value is based on the local time zone, it will be automatically adjusted to the new local time zone when
  /// deserialized. (For instance, 3PM serialized in PST may become 6PM when deserialized in EST.) Times based on UTC are unaffected.
  /// The values can be deserialized with <see cref="BinaryReader.ReadAdjustableDateTimes"/>.
  /// </remarks>
  public void WriteAdjustableDateTimes(DateTime[] dateTimes, int index, int count)
  {
    Utility.ValidateRange(dateTimes, index, count);
    for(int end = index+count; index < end; index++) WriteAdjustableDateTime(dateTimes[index]);
  }

  /// <summary>Writes a signed integer with a variable-length format, taking from one to five bytes.</summary>
  public void WriteEncoded(int value)
  {
    // values from -64 to 63 will be encoded into a single byte, from -16384 to 16383 in 2 bytes, etc.
    // the first byte will contain a sign bit in the 7th bit (0x40)
    int byteValue = value & 0x3F | (value < 0 ? 0x40 : 0);

    if(value <= 63 && value >= -64)
    {
      Write((byte)byteValue);
    }
    else
    {
      Write((byte)(byteValue | 0x80));
      value >>= 6;

      while(value > 127 || value < -128)
      {
        Write((byte)(value | 0x80));
        value >>= 7;
      }
      Write((byte)(value & 0x7F));
    }
  }

  /// <summary>Writes a signed long integer with a variable-length format, taking from one to ten bytes.</summary>
  public void WriteEncoded(long value)
  {
    if(value <= int.MaxValue && value >= int.MinValue)
    {
      WriteEncoded((int)value);
    }
    else
    {
      // values from -64 to 63 will be encoded into a single byte, from -16384 to 16383 in 2 bytes, etc.
      // the first byte will contain a sign bit in the 7th bit (0x40)
      int byteValue = (byte)(value & 0x3F) | (value < 0 ? 0x40 : 0);

      Write((byte)(byteValue | 0x80));
      value >>= 6;

      while(value > 127 || value < -128)
      {
        Write((byte)((int)value | 0x80));
        value >>= 7;
      }
      Write((byte)((int)value & 0x7F));
    }
  }

  /// <summary>Writes an unsigned integer with a variable-length format, taking from one to five bytes.</summary>
  [CLSCompliant(false)]
  public void WriteEncoded(uint value)
  {
    // values from 0-127 will be encoded into a single byte, from 128 to 32767 in two bytes, etc
    while(value > 127)
    {
      Write((byte)(value | 0x80));
      value >>= 7;
    }
    Write((byte)value);
  }

  /// <summary>Writes an unsigned long integer with a variable-length format, taking from one to ten bytes.</summary>
  [CLSCompliant(false)]
  public void WriteEncoded(ulong value)
  {
    if(value <= uint.MaxValue)
    {
      WriteEncoded((uint)value);
    }
    else
    {
      // values from 0-127 will be encoded into a single byte, from 128 to 32767 in two bytes, etc
      while(value > 127)
      {
        Write((byte)((int)value | 0x80));
        value >>= 7;
      }
      Write((byte)value);
    }
  }

  /// <summary>Writes a string to the stream, using the default encoding. Null strings are supported.</summary>
  public void WriteNullableString(string str)
  {
    WriteNullableString(str, DefaultEncoding);
  }

  /// <summary>Writes a string to the stream, with the given encoding. Null strings are supported.</summary>
  public void WriteNullableString(string str, Encoding encoding)
  {
    if(str == null) WriteEncoded(0U);
    else WriteStringWithLength(str, 0, str.Length, encoding);
  }

  /// <summary>Writes a substring to the stream, using the default encoding.</summary>
  /// <remarks>The string may be read back with <see cref="BinaryReader.ReadNullableString"/>.</remarks>
  public void WriteStringWithLength(string str, int index, int length)
  {
    Write(str, index, length, DefaultEncoding);
  }

  /// <summary>Writes a substring to the stream, using the given encoding.</summary>
  /// <remarks>The string may be read back with <see cref="BinaryReader.ReadNullableString"/>.</remarks>
  public void WriteStringWithLength(string str, int index, int length, Encoding encoding)
  {
    Utility.ValidateRange(str, index, length);
    if(encoding == null) throw new ArgumentNullException("encoding");

    if(length != 0) // zero-length strings yield a null-pointer when fixed, so avoid passing a null pointer
    {
      int spaceNeeded = encoding.GetMaxByteCount(length);
      if(spaceNeeded <= StackAllocThreshold)
      {
        byte* buffer = stackalloc byte[spaceNeeded];
        fixed(char* chars=str) spaceNeeded = encoding.GetBytes(chars+index, length, buffer, spaceNeeded);
        WriteEncoded((uint)spaceNeeded + 1); // we write a length one greater than actual, so null strings can have a length of 0
        WriteCore(buffer, spaceNeeded);
      }
      else
      {
        fixed(char* chars=str)
        {
          WriteEncoded((uint)encoding.GetByteCount(chars+index, length) + 1);
          Write(chars+index, length, encoding);
        }
      }
    }
    else
    {
      WriteEncoded(1U); // zero is for null strings, so we have to add one to the length (producing 1)
    }
  }

  /// <summary>Writes a sequence of strings to the stream, with the default encoding. Null strings are supported.</summary>
  /// <remarks>Individual strings are written using <see cref="WriteNullableString(string)"/> and the sequence can be read back
  /// with <see cref="BinaryReader.ReadNullableStrings"/>.
  /// </remarks>
  public void WriteNullableStrings(params string[] array)
  {
    WriteNullableStrings(array, DefaultEncoding);
  }

  /// <summary>Writes a sequence of strings to the stream, with the given encoding. Null strings are supported.</summary>
  /// <remarks>Individual strings are written using <see cref="WriteNullableString(string,Encoding)"/> and the sequence can be read back
  /// with <see cref="BinaryReader.ReadNullableStrings"/>.
  /// </remarks>
  public void WriteNullableStrings(string[] array, Encoding encoding)
  {
    if(array == null) throw new ArgumentNullException();
    WriteNullableStrings(array, 0, array.Length, encoding);
  }

  /// <summary>Writes a sequence of strings to the stream, with the default encoding. Null strings are supported.</summary>
  /// <remarks>Individual strings are written using <see cref="WriteNullableString(string)"/> and the sequence can be read back
  /// with <see cref="BinaryReader.ReadNullableStrings"/>.
  /// </remarks>
  public void WriteNullableStrings(string[] array, int index, int length)
  {
    WriteNullableStrings(array, index, length, DefaultEncoding);
  }

  /// <summary>Writes a sequence of strings to the stream, with the given encoding. Null strings are supported.</summary>
  /// <remarks>Individual strings are written using <see cref="WriteNullableString(string,Encoding)"/> and the sequence can be read back
  /// with <see cref="BinaryReader.ReadNullableStrings"/>.
  /// </remarks>
  public void WriteNullableStrings(string[] array, int index, int length, Encoding encoding)
  {
    Utility.ValidateRange(array, index, length);
    foreach(string str in array) WriteNullableString(str);
  }

  /// <summary>Writes the given number of zero bytes.</summary>
  public void WriteZeros(int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    if(count != 0) // avoid throwing an exception if we're at the end of an external buffer
    {
      EnsureSpace(1); // the loop below assumes there's some free space in the buffer, so ensure that
      do
      {
        int toWrite = Math.Min(count, AvailableSpace);
        if(toWrite == 0) RaiseFullError();

        Array.Clear(Buffer, writeIndex, toWrite);
        writeIndex += toWrite;
        count      -= toWrite;

        if(AvailableSpace == 0) FlushBuffer();
      } while(count != 0);
    }
  }

  /// <summary>Writes the data from the buffer to the underlying stream and then flushes the underlying stream.</summary>
  public void Flush()
  {
    if(!ExternalBuffer)
    {
      FlushBuffer();
      BaseStream.Flush();
    }
  }

  /// <summary>Writes the data from the buffer to the underlying stream, but does not flush the underlying stream.
  /// (Use <see cref="Flush"/> for that.)
  /// <seealso cref="Flush"/>
  /// </summary>
  public void FlushBuffer()
  {
    if(!ExternalBuffer && writeIndex != 0)
    {
      BaseStream.Write(Buffer, 0, writeIndex);
      writeIndex = 0;
    }
  }

  /// <summary>Gets the position in the buffer where the next write should occur.</summary>
  [CLSCompliant(false)]
  protected byte* WritePtr
  {
    get { return BufferPtr+writeIndex; }
  }

  /// <summary>Overrides <see cref="PinnedBuffer.CreateResizeBuffer"/> to copy only the minimum necessary to the
  /// new buffer.
  /// </summary>
  protected override byte[] CreateResizeBuffer(int newSize)
  {
    byte[] newBuffer = new byte[newSize];
    Array.Copy(Buffer, newBuffer, writeIndex);
    return newBuffer;
  }

  /// <summary>Overrides <see cref="PinnedBuffer.Dispose"/> to flush the buffer before freeing it.</summary>
  protected override void Dispose(bool manualDispose)
  {
    FlushBuffer();
    base.Dispose(manualDispose);
  }

  /// <summary>Ensures that there is enough space in the buffer to copy the given number of bytes to it.</summary>
  /// <param name="nbytes">The number of bytes to reserve in the buffer.</param>
  /// <remarks>This method may alter the write index or enlarge the buffer. It is not recommended to use this
  /// method for large writes. Rather, use <see cref="Write(void*,int)"/> to write a large chunk of data to the buffer.
  /// </remarks>
  protected void EnsureSpace(int nbytes)
  {
    if(nbytes > AvailableSpace) // if there's not enough space...
    {
      if(ExternalBuffer) RaiseFullError(); // if it's an external buffer, we can't enlarge it
      FlushBuffer(); // flushing the buffer may free up space.
      EnsureCapacity(nbytes); // now the buffer is empty, so ensure it's big enough
    }
  }

  /// <summary>Ensures that the data most recently added to the buffer, which must consist of two-byte integers,
  /// has the desired endianness.
  /// </summary>
  /// <param name="nwords">The number of words to potentially swap.</param>
  protected void MakeDesiredEndian2(int words)
  {
    if(LittleEndian != BitConverter.IsLittleEndian) SwapEndian2(WritePtr-words*2, words);
  }

  /// <summary>Ensures that the data most recently added to the buffer, which must consist of four-byte integers,
  /// has the desired endianness.
  /// </summary>
  /// <param name="ndwords">The number of doublewords to potentially swap.</param>
  protected void MakeDesiredEndian4(int dwords)
  {
    if(LittleEndian != BitConverter.IsLittleEndian) SwapEndian4(WritePtr-dwords*4, dwords);
  }

  /// <summary>Ensures that the data most recently added to the buffer, which must consist of eight-byte integers,
  /// has the desired endianness.
  /// </summary>
  /// <param name="nqwords">The number of quadwords to potentially swap.</param>
  protected void MakeDesiredEndian8(int qwords)
  {
    if(LittleEndian != BitConverter.IsLittleEndian) SwapEndian8(WritePtr-qwords*8, qwords);
  }

  const int StackAllocThreshold = 1024;

  /// <summary>The amount of empty space in the buffer.</summary>
  int AvailableSpace
  {
    get { return ExternalBuffer ? bufferLength-writeIndex+startIndex : Buffer.Length-writeIndex; }
  }

  void WriteCore(byte* data, int count)
  {
    if(count != 0)
    {
      do
      {
        int bytes = AvailableSpace;
        if(bytes == 0)
        {
          FlushBuffer();
          bytes = AvailableSpace;
          if(bytes == 0) RaiseFullError();
        }

        if(count < bytes) bytes = count;
        Unsafe.Copy(data, WritePtr, bytes);
        data       += bytes;
        writeIndex += bytes;
        count      -= bytes;
      } while(count != 0);
    }
  }

  void WriteCore(byte* data, long count)
  {
    const int BigChunkSize = int.MaxValue & ~7; // trim off the last few bits to avoid misaligning the writes
    while(count >= BigChunkSize)
    {
      WriteCore(data, BigChunkSize);
      data  += BigChunkSize;
      count -= BigChunkSize;
    }
    WriteCore(data, (int)count);
  }

  void WriteCore(byte* data, int count, int wordSize)
  {
    if(wordSize == 1)
    {
      WriteCore(data, count);
    }
    else if(count != 0)
    {
      int shift = wordSize == 4 ? 2 : wordSize == 8 ? 3 : 1; // the shift to divide or multiply by the word size
      do
      {
        int chunks = AvailableSpace>>shift;
        if(chunks == 0)
        {
          FlushBuffer();
          chunks = AvailableSpace>>shift;
          if(chunks == 0) RaiseFullError();
        }

        if(count < chunks) chunks = count;
        int bytes = chunks<<shift;
        Unsafe.Copy(data, WritePtr, bytes);
        data       += bytes;
        writeIndex += bytes;

        if(wordSize == 4) MakeDesiredEndian4(chunks);
        else if(wordSize == 2) MakeDesiredEndian2(chunks);
        else MakeDesiredEndian8(chunks);

        count -= chunks;
      } while(count != 0);
    }
  }

  int writeIndex;
  readonly int startIndex, bufferLength;
  Encoding encoding;

  static void RaiseFullError()
  {
    throw new InvalidOperationException("The buffer is full.");
  }
}
#endregion

} // namespace AdamMil.IO
