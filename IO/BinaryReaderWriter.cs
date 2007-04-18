using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace AdamMil.IO
{

#region PinnedBuffer
/// <summary>This class supports the <see cref="BinaryReader"/> and <see cref="BinaryWriter"/> classes and is not
/// intended to be used directly. This class manages a constantly-pinned buffer. This class in not safe for use by
/// multiple threads concurrently.
/// </summary>
public unsafe abstract class PinnedBuffer : IDisposable
{
  /// <summary>Initializes an empty IOBuffer with the given buffer size.</summary>
  /// <param name="bufferSize">The initial buffer size. If zero, a default size of 4096 bytes will be used.</param>
  public PinnedBuffer(int bufferSize)
  {
    if(bufferSize < 0) throw new ArgumentOutOfRangeException();
    if(bufferSize == 0) bufferSize = 4096;

    buffer = new byte[bufferSize];
    PinBuffer();
  }

  ~PinnedBuffer()
  {
    Dispose(true);
  }

  /// <summary>Unpins and frees the buffer.</summary>
  public void Dispose()
  {
    GC.SuppressFinalize(this);
    Dispose(false);
  }

  /// <summary>Returns a safe reference to the underlying buffer.</summary>
  protected byte[] Buffer
  {
    get { return buffer; }
  }

  /// <summary>Returns an unsafe pointer to the underlying buffer.</summary>
  protected byte* BufferPtr
  {
    get { return bufferPtr; }
  }

  /// <summary>Creates the new buffer to be used when enlarging the IO buffer.</summary>
  /// <param name="newSize">The new size of the buffer.</param>
  /// <returns>A newly-allocated buffer of the given size.</returns>
  /// <remarks>The default implementation simply allocates a new buffer and copies the old data into it.</remarks>
  protected virtual byte[] CreateResizeBuffer(int newSize)
  {
    byte[] newBuffer = new byte[newSize];
    Array.Copy(buffer, newBuffer, buffer.Length);
    return newBuffer;
  }

  /// <summary>Unpins and frees the buffer.</summary>
  protected virtual void Dispose(bool finalizing)
  {
    FreeBuffer();
  }
  
  /// <summary>Ensures that the buffer has at least the given capacity. This method will resize the buffer if
  /// necessary, which will invalidate any pointers to the buffer.
  /// </summary>
  /// <param name="capacity">The required capacity of the buffer.</param>
  protected void EnsureCapacity(int capacity)
  {
    if(capacity > buffer.Length)
    {
      int newSize = buffer.Length, add = 4096;

      if((newSize & 0xFFF) != 0) // if the buffer size is not a multiple of 4096, grow the buffer in doubles
      {
        add = buffer.Length;
      }

      do newSize += add; while(newSize < capacity);

      byte[] newBuffer = CreateResizeBuffer(newSize);

      FreeBuffer();
      buffer = newBuffer;
      PinBuffer();
    }
  }

  /// <summary>Copies data from one region of memory to another. The method does not handle overlapping regions.</summary>
  /// <param name="src">A pointer to the region from where data will be read.</param>
  /// <param name="dest">A pointer to the region where data will be written.</param>
  /// <param name="nbytes">The number of bytes to copy.</param>
  protected static void Copy(byte* src, byte* dest, int nbytes)
  {
    // TODO: we should copy /aligned/ dwords.
    if(nbytes < 0) throw new ArgumentOutOfRangeException();

    if(nbytes >= 16)
    {
      do
      {
        *(uint*)dest = *(uint*)src;
        *(uint*)(dest+4)  = *(uint*)(src+4);
        *(uint*)(dest+8)  = *(uint*)(src+8);
        *(uint*)(dest+12) = *(uint*)(src+12);
        src    += 16;
        dest   += 16;
        nbytes -= 16;
      } while(nbytes >= 16);
    }

    if(nbytes != 0)
    {
      if((nbytes & 8) != 0)
      {
        *(uint*)dest = *(uint*)src;
        *(uint*)(dest+4) = *(uint*)(src+4);
        dest += 8;
        src  += 8;
      }
      if((nbytes & 4) != 0)
      {
        *(uint*)dest = *(uint*)src;
        dest += 4;
        src  += 4;
      }
      if((nbytes & 2) != 0)
      {
        *(ushort*)dest = *(ushort*)src;
        dest += 2;
        src  += 2;
      }
      if((nbytes & 1) != 0)
      {
        *dest = *src;
      }
    }
  }

  /// <summary>Swaps the byte order of each word in the given data.</summary>
  /// <param name="data">A pointer to the data.</param>
  /// <param name="words">The number of two-byte words to swap.</param>
  protected static void SwapEndian2(byte* data, int words)
  {
    byte* end = data + words*sizeof(ushort);
    for(; data != end; data += sizeof(ushort))
    {
      byte t  = data[0];
      data[0] = data[1];
      data[1] = t;
    }
  }

  /// <summary>Swaps the byte order of each doubleword in the given data.</summary>
  /// <param name="data">A pointer to the data.</param>
  /// <param name="dwords">The number of four-byte doublewords to swap.</param>
  protected static void SwapEndian4(byte* data, int dwords)
  {
    byte* end = data + dwords*sizeof(uint);
    for(; data != end; data += sizeof(uint))
    {
      byte t  = data[0];
      data[0] = data[3];
      data[3] = t;

      t       = data[1];
      data[1] = data[2];
      data[2] = t;
    }
  }

  /// <summary>Swaps the byte order of each quadword in the given data.</summary>
  /// <param name="data">A pointer to the data.</param>
  /// <param name="qwords">The number of eight-byte quadwords to swap.</param>
  protected static void SwapEndian8(byte* data, int qwords)
  {
    byte* end = data + qwords*sizeof(ulong);
    for(; data != end; data += sizeof(ulong))
    {
      byte t  = data[0];
      data[0] = data[7];
      data[7] = t;

      t       = data[1];
      data[1] = data[6];
      data[6] = t;

      t       = data[2];
      data[2] = data[5];
      data[5] = t;

      t       = data[3];
      data[3] = data[4];
      data[4] = t;
    }
  }

  void FreeBuffer()
  {
    if(handle.IsAllocated) handle.Free();
    bufferPtr = null;
  }
  
  void PinBuffer()
  {
    handle    = GCHandle.Alloc(buffer, GCHandleType.Pinned);
    bufferPtr = (byte*)handle.AddrOfPinnedObject().ToPointer();
  }

  byte[] buffer;
  byte*  bufferPtr;
  GCHandle handle;
}
#endregion

#region BinaryReaderWriterBase
/// <summary>This class supports the <see cref="BinaryReader"/> and <see cref="BinaryWriter"/> classes and is not meant
/// to be used directly.
/// </summary>
public abstract class BinaryReaderWriterBase : PinnedBuffer
{
  internal BinaryReaderWriterBase(Stream stream, bool littleEndian, int bufferSize, bool shared) : base(bufferSize)
  {
    if(stream == null) throw new ArgumentNullException();
    this.stream       = stream;
    this.littleEndian = littleEndian;
    this.shared       = shared;
    StoreStreamPosition();
  }

  /// <summary>Returns a reference to the underlying stream.</summary>
  public Stream BaseStream
  {
    get { return this.stream; }
  }

  /// <summary>Gets or sets whether the data being read from or written to the stream is little endian.</summary>
  /// <remarks>If the endianness of the data does not match the endianness of the system, the bytes will be swapped as
  /// necessary.
  /// </remarks>
  public bool LittleEndian
  {
    get { return littleEndian; }
    set { littleEndian = value; }
  }

  /// <summary>
  /// Gets whether the underlying stream can be safely used by another class while this reader or writer is in use.
  /// </summary>
  public bool Shared
  {
    get { return shared; }
  }

  /// <summary>Seeks the underlying stream to the expected position if necessary.</summary>
  internal void EnsureStreamPositioned()
  {
    if(shared && stream.Position != lastStreamPosition)
    {
      stream.Position = lastStreamPosition;
    }
  }

  /// <summary>Stores the stream position if <see cref="Shared"/> is true.</summary>
  internal void StoreStreamPosition()
  {
    if(shared) lastStreamPosition = stream.Position;
  }

  long lastStreamPosition;
  readonly Stream stream;
  readonly bool shared;
  bool littleEndian;
}
#endregion

#region BinaryReader
/// <summary>This class makes it easy to efficiently deserialize values from a stream.</summary>
/// <remarks>The class buffers input, and so may read more bytes from the stream than you explicitly request. However,
/// when the class is disposed, it will seek the stream to the end of the data read from the reader.
/// This class in not safe for use by multiple threads concurrently.
/// </remarks>
public unsafe class BinaryReader : BinaryReaderWriterBase
{
  /// <summary>Initializes this <see cref="BinaryReader"/> with the default buffer size, little-endianness, and the
  /// assumption that the stream will not be accessed by any other classes while this reader is in use.
  /// </summary>
  /// <param name="stream">The stream from which data will be read.</param>
  /// <remarks>If the underlying stream will be accessed by any other classes while this reader is in use, you must use
  /// an override that takes a 'shared' parameter, and pass <c>true</c>.
  /// </remarks>
  public BinaryReader(Stream stream) : this(stream, true) { }

  /// <summary>Initializes this <see cref="BinaryReader"/> with the default buffer size and the
  /// assumption that the stream will not be accessed by any other classes while this reader is in use.
  /// </summary>
  /// <param name="stream">The stream from which data will be read.</param>
  /// <param name="littleEndian">Whether the data being read is little endian. This can be changed at any time using
  /// the <see cref="LittleEndian"/> property.
  /// </param>
  /// <remarks>If the underlying stream will be accessed by any other classes while this reader is in use, you must use
  /// an override that takes a 'shared' parameter, and pass <c>true</c>.
  /// </remarks>
  public BinaryReader(Stream stream, bool littleEndian) : this(stream, littleEndian, 0, false) { }

  /// <summary>Initializes this <see cref="BinaryReader"/>.</summary>
  /// <param name="stream">The stream from which data will be read. If the stream does its own buffering, it may be
  /// more efficient to eliminate the buffer from the stream, so that the data is not buffered multiple times.
  /// </param>
  /// <param name="littleEndian">Whether the data being read is little endian. This can be changed at any time using
  /// the <see cref="LittleEndian"/> property.
  /// </param>
  /// <param name="bufferSize">The buffer size. If zero, a default buffer size of 4096 bytes will be used. The default
  /// buffer size is usually sufficient, but if you know approximately how much data you'll be reading from the stream,
  /// you can tune the buffer size so that the <see cref="BinaryReader"/> does not read more data than is necessary.
  /// </param>
  /// <param name="shared">Whether or not the stream will be accessed by other any other class while this reader is in
  /// use. If true, the reader will ensure that the underlying stream is seeked to the correct position before each
  /// read. You cannot pass a value of true for unseekable streams. Passing true also does not make this class safe for
  /// simultaneous use by multiple threads. You'll have to synchronize that yourself.
  /// </param>
  public BinaryReader(Stream stream, bool littleEndian, int bufferSize, bool shared)
    : base(stream, littleEndian, bufferSize, shared) { }

  /// <summary>Gets or sets the current position of the reader within the underlying stream. This equal to the
  /// underlying stream's position, minus the amount of data available in the reader's buffer.
  /// </summary>
  /// <remarks>Note that setting the position may cause data to be discarded from the buffer. This inefficiency can be
  /// mitigated by reducing the size of the buffer so that less data is thrown away.
  /// </remarks>
  public long Position
  {
    get
    {
      EnsureStreamPositioned();
      return BaseStream.Position - AvailableData;
    }
    set
    {
      EnsureStreamPositioned();
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
        StoreStreamPosition();
      }
    }
  }

  /// <summary>Reads a one-byte boolean value from the stream.</summary>
  public bool ReadBool()
  {
    return ReadByte() != 0;
  }

  /// <summary>Reads an unsigned byte from the stream.</summary>
  public byte ReadByte()
  {
    return *ReadContiguousData(1);
  }

  /// <summary>Reads a signed byte from the stream.</summary>
  public sbyte ReadSByte()
  {
    return (sbyte)ReadByte();
  }

  /// <summary>Reads a two-byte character from the stream.</summary>
  public char ReadChar()
  {
    return (char)ReadUInt16();
  }

  /// <summary>Reads a signed two-byte integer from the stream.</summary>
  public short ReadInt16()
  {
    return (short)ReadUInt16();
  }

  /// <summary>Reads an unsigned two-byte integer from the stream.</summary>
  public ushort ReadUInt16()
  {
    byte* data = ReadContiguousData(sizeof(ushort));
    return LittleEndian ? IOH.ReadLE2U(data, 0) : IOH.ReadBE2U(data, 0);
  }

  /// <summary>Reads a signed four-byte integer from the stream.</summary>
  public int ReadInt32()
  {
    return (int)ReadUInt32();
  }

  /// <summary>Reads an unsigned four-byte integer from the stream.</summary>
  public uint ReadUInt32()
  {
    byte* data = ReadContiguousData(sizeof(uint));
    return LittleEndian ? IOH.ReadLE4U(data, 0) : IOH.ReadBE4U(data, 0);
  }

  /// <summary>Reads a signed eight-byte integer from the stream.</summary>
  public long ReadInt64()
  {
    return (long)ReadUInt64();
  }

  /// <summary>Reads an unsigned eight-byte integer from the stream.</summary>
  public ulong ReadUInt64()
  {
    byte* data = ReadContiguousData(sizeof(ulong));
    return LittleEndian ? IOH.ReadLE8U(data, 0) : IOH.ReadBE8U(data, 0);
  }

  /// <summary>Reads a four-byte float from the stream.</summary>
  public float ReadSingle()
  {
    return *(float*)ReadContiguousData(sizeof(float));
  }

  /// <summary>Reads an eight-byte float from the stream.</summary>
  public double ReadDouble()
  {
    return *(double*)ReadContiguousData(sizeof(double));
  }

  /// <summary>Reads an array of bytes from the stream.</summary>
  public byte[] ReadByte(int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    byte[] data = new byte[count];
    fixed(byte* ptr=data) ReadData(ptr, count);
    return data;
  }

  /// <summary>Reads a number of two-byte characters from the stream.</summary>
  public void ReadByte(byte[] array, int index, int count)
  {
    if(array == null) throw new ArgumentNullException();
    if(index < 0 || index+count > array.Length) throw new ArgumentOutOfRangeException();
    fixed(byte* ptr=array) ReadData(ptr, count);
  }

  /// <summary>Reads an array of two-byte characters from the stream.</summary>
  public char[] ReadChar(int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    char[] data = new char[count];
    fixed(char* ptr=data) ReadChar(ptr, count);
    return data;
  }

  /// <summary>Reads a number of two-byte characters from the stream.</summary>
  public void ReadChar(char[] array, int index, int count)
  {
    if(array == null) throw new ArgumentNullException();
    if(index < 0 || index+count > array.Length) throw new ArgumentOutOfRangeException();
    fixed(char* ptr=array) ReadChar(ptr+index, count);
  }

  /// <summary>Reads a number of two-byte characters from the stream.</summary>
  public void ReadChar(char* array, int count)
  {
    ReadUInt16((ushort*)array, count);
  }

  /// <summary>Reads an array of signed two-byte integers from the stream.</summary>
  public short[] ReadInt16(int count)
  {
    short[] data = new short[count];
    fixed(short* ptr=data) ReadInt16(ptr, count);
    return data;
  }

  /// <summary>Reads an array of signed two-byte integers from the stream.</summary>
  public void ReadInt16(short[] array, int index, int count)
  {
    if(array == null) throw new ArgumentNullException();
    if(index < 0 || index+count > array.Length) throw new ArgumentOutOfRangeException();
    fixed(short* ptr=array) ReadInt16(ptr+index, count);
  }

  /// <summary>Reads an array of signed two-byte integers from the stream.</summary>
  public void ReadInt16(short* array, int count)
  {
    ReadUInt16((ushort*)array, count);
  }

  /// <summary>Reads an array of unsigned two-byte integers from the stream.</summary>
  public ushort[] ReadUInt16(int count)
  {
    ushort[] data = new ushort[count];
    fixed(ushort* ptr=data) ReadUInt16(ptr, count);
    return data;
  }

  /// <summary>Reads an array of unsigned two-byte integers from the stream.</summary>
  public void ReadUInt16(ushort[] array, int index, int count)
  {
    if(array == null) throw new ArgumentNullException();
    if(index < 0 || index+count > array.Length) throw new ArgumentOutOfRangeException();
    fixed(ushort* ptr=array) ReadUInt16(ptr+index, count);
  }

  /// <summary>Reads an array of unsigned two-byte integers from the stream.</summary>
  public void ReadUInt16(ushort* array, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    ReadData(array, count*sizeof(ushort));
    MakeSystemEndian2(array, count);
  }

  /// <summary>Reads an array of signed four-byte integers from the stream.</summary>
  public int[] ReadInt32(int count)
  {
    int[] data = new int[count];
    fixed(int* ptr=data) ReadInt32(ptr, count);
    return data;
  }

  /// <summary>Reads an array of signed four-byte integers from the stream.</summary>
  public void ReadInt32(int[] array, int index, int count)
  {
    if(array == null) throw new ArgumentNullException();
    if(index < 0 || index+count > array.Length) throw new ArgumentOutOfRangeException();
    fixed(int* ptr=array) ReadInt32(ptr+index, count);
  }

  /// <summary>Reads an array of signed four-byte integers from the stream.</summary>
  public void ReadInt32(int* array, int count)
  {
    ReadUInt32((uint*)array, count);
  }

  /// <summary>Reads an array of unsigned four-byte integers from the stream.</summary>
  public uint[] ReadUInt32(int count)
  {
    uint[] data = new uint[count];
    fixed(uint* ptr=data) ReadUInt32(ptr, count);
    return data;
  }

  /// <summary>Reads an array of unsigned four-byte integers from the stream.</summary>
  public void ReadUInt32(uint[] array, int index, int count)
  {
    if(array == null) throw new ArgumentNullException();
    if(index < 0 || index+count > array.Length) throw new ArgumentOutOfRangeException();
    fixed(uint* ptr=array) ReadUInt32(ptr+index, count);
  }

  /// <summary>Reads an array of unsigned four-byte integers from the stream.</summary>
  public void ReadUInt32(uint* array, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    ReadData(array, count*sizeof(uint));
    MakeSystemEndian4(array, count);
  }

  /// <summary>Reads an array of signed eight-byte integers from the stream.</summary>
  public long[] ReadInt64(int count)
  {
    long[] data = new long[count];
    fixed(long* ptr=data) ReadInt64(ptr, count);
    return data;
  }

  /// <summary>Reads an array of signed eight-byte integers from the stream.</summary>
  public void ReadInt64(long[] array, int index, int count)
  {
    if(array == null) throw new ArgumentNullException();
    if(index < 0 || index+count > array.Length) throw new ArgumentOutOfRangeException();
    fixed(long* ptr=array) ReadInt64(ptr+index, count);
  }

  /// <summary>Reads an array of signed eight-byte integers from the stream.</summary>
  public void ReadInt64(long* array, int count)
  {
    ReadUInt64((ulong*)array, count);
  }

  /// <summary>Reads an array of unsigned eight-byte integers from the stream.</summary>
  public ulong[] ReadUInt64(int count)
  {
    ulong[] data = new ulong[count];
    fixed(ulong* ptr=data) ReadUInt64(ptr, count);
    return data;
  }

  /// <summary>Reads an array of unsigned eight-byte integers from the stream.</summary>
  public void ReadUInt64(ulong[] array, int index, int count)
  {
    if(array == null) throw new ArgumentNullException();
    if(index < 0 || index+count > array.Length) throw new ArgumentOutOfRangeException();
    fixed(ulong* ptr=array) ReadUInt64(ptr+index, count);
  }

  /// <summary>Reads an array of unsigned eight-byte integers from the stream.</summary>
  public void ReadUInt64(ulong* array, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    ReadData(array, count*sizeof(ulong));
    MakeSystemEndian8(array, count);
  }

  /// <summary>Reads an array of four-byte floats from the stream.</summary>
  public float[] ReadSingle(int count)
  {
    float[] data = new float[count];
    fixed(float* ptr=data) ReadSingle(ptr, count);
    return data;
  }

  /// <summary>Reads an array of four-byte floats from the stream.</summary>
  public void ReadSingle(float[] array, int index, int count)
  {
    if(array == null) throw new ArgumentNullException();
    if(index < 0 || index+count > array.Length) throw new ArgumentOutOfRangeException();
    fixed(float* ptr=array) ReadSingle(ptr+index, count);
  }

  /// <summary>Reads an array of unsigned four-byte integers from the stream.</summary>
  public void ReadSingle(float* array, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    ReadData(array, count*sizeof(float));
  }

  /// <summary>Reads an array of eight-byte floats from the stream.</summary>
  public double[] ReadDouble(int count)
  {
    double[] data = new double[count];
    fixed(double* ptr=data) ReadDouble(ptr, count);
    return data;
  }

  /// <summary>Reads an array of eight-byte floats from the stream.</summary>
  public void ReadDouble(double[] array, int index, int count)
  {
    if(array == null) throw new ArgumentNullException();
    if(index < 0 || index+count > array.Length) throw new ArgumentOutOfRangeException();
    fixed(double* ptr=array) ReadDouble(ptr+index, count);
  }

  /// <summary>Reads an array of unsigned eight-byte integers from the stream.</summary>
  public void ReadDouble(double* array, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    ReadData(array, count*sizeof(double));
  }

  /// <summary>Reads a string stored as an array of two-byte characters from the stream.</summary>
  /// <param name="nchars">The number of two-byte characters to read.</param>
  /// <returns>A string of length <paramref name="nchars"/> containing the characters read from the stream.</returns>
  public string ReadString(int nchars)
  {
    if(nchars < 0) throw new ArgumentOutOfRangeException();
    if(nchars == 0) return string.Empty;
    char* data = (char*)ReadContiguousData(nchars * sizeof(char));
    MakeSystemEndian2(data, nchars);
    return new string(data, 0, nchars);
  }

  /// <summary>Reads a string that was written by <see cref="BinaryWriter.AddStringWithLength"/>.</summary>
  /// <remarks>Essentially, the string is stored as a 4-byte integer holding the length, followed by that many two-byte
  /// characters. A null string is represented with a length of -1.
  /// </remarks>
  public string ReadStringWithLength()
  {
    int nchars = ReadInt32();
    return nchars == -1 ? null : ReadString(nchars);
  }

  /// <summary>Advances the reader by the given number of bytes.</summary>
  public void Skip(int nbytes)
  {
    if(nbytes < 0) throw new ArgumentOutOfRangeException();
    int advance = Math.Min(AvailableData, nbytes);
    AdvanceTail(advance);
    nbytes -= advance;
    if(nbytes != 0) Position += nbytes;
  }

  /// <summary>Skips a string that was written by <see cref="BinaryWriter.AddStringWithLength"/>.</summary>
  public void SkipStringWithLength()
  {
    int length = ReadInt32();
    if(length > 0) Skip(length * sizeof(char));
  }

  /// <summary>
  /// Overrides <see cref="PinnedBuffer.CreateResizeBuffer"/> to properly resize the circular array that we're using.
  /// </summary>
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

  protected override void Dispose(bool finalizing)
  {
    BaseStream.Position = Position; // set the stream position to the end of the data read from the reader.
    base.Dispose(finalizing);       // this way, the stream is not positioned at some seemingly random place.
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

  /// <summary>Ensures that the data, which must consist of two-byte integers, has system endianness.</summary>
  /// <param name="data">A pointer to the data.</param>
  /// <param name="nwords">The number of words to potentially swap.</param>
  protected void MakeSystemEndian2(void* data, int nwords)
  {
    if(LittleEndian != BitConverter.IsLittleEndian) SwapEndian2((byte*)data, nwords);
  }

  /// <summary>Ensures that the data, which must consist of four-byte integers, has system endianness.</summary>
  /// <param name="data">A pointer to the data.</param>
  /// <param name="dwords">The number of doublewords to potentially swap.</param>
  protected void MakeSystemEndian4(void* data, int dwords)
  {
    if(LittleEndian != BitConverter.IsLittleEndian) SwapEndian4((byte*)data, dwords);
  }

  /// <summary>Ensures that the data, which must consist of eight-byte integers, has system endianness.</summary>
  /// <param name="data">A pointer to the data.</param>
  /// <param name="qwords">The number of quadwords to potentially swap.</param>
  protected void MakeSystemEndian8(void* data, int qwords)
  {
    if(LittleEndian != BitConverter.IsLittleEndian) SwapEndian8((byte*)data, qwords);
  }

  /// <summary>Ensures that the given number of bytes are available in a contiguous form in the buffer.</summary>
  /// <param name="nbytes">The number of contiguous bytes to read.</param>
  /// <returns>A pointer to the bytes requested.</returns>
  /// <remarks>The buffer size will be enlarged if necessary, but this method is better suited for small reads. For
  /// larger reads, consider using <see cref="ReadData"/>, which will not enlarge the buffer.
  /// </remarks>
  protected byte* ReadContiguousData(int nbytes)
  {
    if(ContiguousData < nbytes) // if there's not enough contiguous data, read more data and/or shift existing data
    {
      if(Buffer.Length < nbytes) // if the buffer simply isn't big enough, we'll first enlarge it
      {
        EnsureCapacity(nbytes);
        if(ContiguousData >= nbytes) goto done; // enlarging the buffer compacts the data, so there may be enough now
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
      
      EnsureStreamPositioned();

      int toRead = availableContiguousSpace - headIndex; // fill the entire contiguous region
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
      
      StoreStreamPosition();
    }

    done:
    byte* data = BufferPtr + tailIndex;
    AdvanceTail(nbytes);
    return data;
  }

  /// <summary>Reads a number of bytes from the stream into the given memory region.</summary>
  /// <param name="dest">A pointer to the location in memory where the data will be written.</param>
  /// <param name="nbytes">The number of bytes to read from the stream.</param>
  protected void ReadData(void* dest, int nbytes)
  {
    byte* ptr = (byte*)dest;

    // attempt to satisfy the request with the contiguous data starting from the tail
    ReadDataInternal(ref ptr, ref nbytes, Math.Min(ContiguousData, nbytes));

    if(nbytes != 0)
    {
      if(headIndex != 0) // attempt to satisfy the remaining request with contiguous data starting from index 0
      {
        ReadDataInternal(ref ptr, ref nbytes, Math.Min(headIndex, nbytes));
      }

      // if that wasn't enough either, we've exhausted the buffer and will read more in the loop below
      if(nbytes != 0)
      {
        EnsureStreamPositioned();
        do
        {
          headIndex = BaseStream.Read(Buffer, 0, Buffer.Length);
          if(headIndex == 0) throw new EndOfStreamException();
          ReadDataInternal(ref ptr, ref nbytes, Math.Min(headIndex, nbytes));
        } while(nbytes != 0);
        StoreStreamPosition();
      }
    }
  }

  /// <summary>Advances the tail by the given number of bytes. It's assumed that this is not greater than
  /// <see cref="ContiguousData"/>. 
  /// </summary>
  void AdvanceTail(int nbytes)
  {
    tailIndex += nbytes;

    Debug.Assert(tailIndex <= Buffer.Length);

    // if the buffer becomes empty due to this move, put the pointers back at the front of the buffer to ensure that
    // we have as much contiguous space as possible
    if(tailIndex == headIndex) headIndex = tailIndex = 0;
  }

  void ReadDataInternal(ref byte* ptr, ref int bytesNeeded, int bytesAvailable)
  {
    Copy(BufferPtr+tailIndex, ptr, bytesAvailable);
    AdvanceTail(bytesAvailable);
    ptr += bytesAvailable;
    bytesNeeded -= bytesAvailable;
  }

  int headIndex, tailIndex;
}
#endregion

#region BinaryWriter
/// <summary>This class makes it easy to efficiently serialize values into a stream.</summary>
/// <remarks>This class in not safe for use by multiple threads concurrently.</remarks>
public unsafe class BinaryWriter : BinaryReaderWriterBase
{
  /// <summary>Initializes this <see cref="BinaryWriter"/> with the default buffer size, little-endianness, and the
  /// assumption that the stream will not be accessed by any other classes while this writer is in use.
  /// </summary>
  /// <param name="stream">The stream to which data will be written.</param>
  /// <remarks>If the underlying stream will be accessed by any other classes while this writer is in use, you must use
  /// an override that takes a 'shared' parameter, and pass <c>true</c>.
  /// </remarks>
  public BinaryWriter(Stream stream) : this(stream, true) { }
  
  /// <summary>Initializes this <see cref="BinaryWriter"/> with the default buffer size and the
  /// assumption that the stream will not be accessed by any other classes while this writer is in use.
  /// </summary>
  /// <param name="stream">The stream to which data will be written.</param>
  /// <param name="littleEndian">Whether the data being written is little endian. This can be changed at any time using
  /// the <see cref="LittleEndian"/> property.
  /// </param>
  /// <remarks>If the underlying stream will be accessed by any other classes while this writer is in use, you must use
  /// an override that takes a 'shared' parameter, and pass <c>true</c>.
  /// </remarks>
  public BinaryWriter(Stream stream, bool littleEndian) : this(stream, littleEndian, 0, false) { }

  /// <summary>Initializes this <see cref="BinaryWriter"/>.</summary>
  /// <param name="stream">The stream to which data will be written. If the stream does its own buffering, it may be
  /// more efficient to eliminate the buffer from the stream, so that the data is not buffered multiple times.
  /// </param>
  /// <param name="littleEndian">Whether the data should be written with little endianness. This can be changed at any
  /// time using the <see cref="LittleEndian"/> property.
  /// </param>
  /// <param name="bufferSize">The buffer size. If zero, a default buffer size of 4096 bytes will be used. The default
  /// buffer size is usually sufficient, but if you know approximately how much data you'll be writing to the stream,
  /// you can tune the buffer size so that the <see cref="BinaryWriter"/> does not allocate more memory than is
  /// necessary.
  /// </param>
  /// <param name="shared">Whether or not the stream will be accessed by other any other class while this writer is in
  /// use. If true, the writer will ensure that the underlying stream is seeked to the correct position before each
  /// write. You cannot pass a value of true for unseekable streams. Passing true also does not make this class safe
  /// for simultaneous use by multiple threads. You'll have to synchronize that yourself.
  /// </param>
  public BinaryWriter(Stream stream, bool littleEndian, int bufferSize, bool shared)
    : base(stream, littleEndian, bufferSize, shared) { }
  
  /// <summary>Gets or sets the position of the writer. This is equal to the position of the underlying stream, plus
  /// the amount of data in the buffer.
  /// </summary>
  public long Position
  {
    get { return BaseStream.Position + writeIndex; }
    set
    {
      if(value != Position)
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

  public void Write(sbyte value)
  {
    Write((byte)value);
  }

  public void Write(byte value)
  {
    if(Buffer.Length == writeIndex) FlushBuffer(); // make sure there's room
    BufferPtr[writeIndex++] = value;
  }

  /// <summary>Writes a two-byte character to the stream.</summary>
  public void Write(char c)
  {
    Write((ushort)c);
  }

  /// <summary>Writes a signed two-byte integer to the stream.</summary>
  public void Write(short value)
  {
    Write((ushort)value);
  }

  /// <summary>Writes an unsigned two-byte integer to the stream.</summary>
  public void Write(ushort value)
  {
    EnsureSpace(sizeof(ushort));
    if(LittleEndian) IOH.WriteLE2U(BufferPtr, writeIndex, value);
    else IOH.WriteBE2U(BufferPtr, writeIndex, value);
    writeIndex += sizeof(ushort);
  }

  /// <summary>Writes a signed four-byte integer to the stream.</summary>
  public void Write(int value)
  {
    Write((uint)value);
  }

  /// <summary>Writes an unsigned four-byte integer to the stream.</summary>
  public void Write(uint value)
  {
    EnsureSpace(sizeof(uint));
    if(LittleEndian) IOH.WriteLE4U(BufferPtr, writeIndex, value);
    else IOH.WriteBE4U(BufferPtr, writeIndex, value);
    writeIndex += sizeof(uint);
  }

  /// <summary>Writes a signed eight-byte integer to the stream.</summary>
  public void Write(long value)
  {
    Write((ulong)value);
  }

  /// <summary>Writes an unsigned eight-byte integer to the stream.</summary>
  public void Write(ulong value)
  {
    EnsureSpace(sizeof(ulong));
    if(LittleEndian) IOH.WriteLE8U(BufferPtr, writeIndex, value);
    else IOH.WriteBE8U(BufferPtr, writeIndex, value);
    writeIndex += sizeof(ulong);
  }

  /// <summary>Writes a four-byte float to the stream.</summary>
  public void Write(float value)
  {
    EnsureSpace(sizeof(float));
    IOH.WriteFloat(BufferPtr, writeIndex, value);
    writeIndex += sizeof(float);
  }

  /// <summary>Writes an eight-byte float to the stream.</summary>
  public void Write(double value)
  {
    EnsureSpace(sizeof(double));
    IOH.WriteDouble(BufferPtr, writeIndex, value);
    writeIndex += sizeof(double);
  }

  /// <summary>Writes a string to the stream as an array of two-byte characters.</summary>
  public void Write(string str)
  {
    if(str == null) throw new ArgumentNullException();
    fixed(char* data=str) Write(data, str.Length);
  }
  
  /// <summary>Writes a string to the stream with its length prefixed. Null strings are also supported by this method.</summary>
  public void WriteStringWithLength(string str)
  {
    if(str == null)
    {
      Write(-1);
    }
    else
    {
      Write(str.Length);
      Write(str);
    }
  }

  /// <summary>Writes an array of bytes to the stream.</summary>
  public void Write(byte[] data)
  {
    if(data == null) throw new ArgumentNullException();
    fixed(byte* ptr=data) Write(ptr, data.Length);
  }

  /// <summary>Writes an array of bytes to the stream.</summary>
  public void Write(byte[] data, int index, int count)
  {
    if(data == null) throw new ArgumentNullException();
    if(index < 0 || index+count > data.Length) throw new ArgumentOutOfRangeException();
    fixed(byte* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of bytes to the stream.</summary>
  public void Write(byte* data, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    Write((void*)data, count, 1);
  }

  /// <summary>Writes an array of two-byte characters to the stream.</summary>
  public void Write(char[] data)
  {
    if(data == null) throw new ArgumentNullException();
    fixed(char* ptr=data) Write(ptr, data.Length);
  }

  /// <summary>Writes an array of two-byte characters to the stream.</summary>
  public void Write(char[] data, int index, int count)
  {
    if(data == null) throw new ArgumentNullException();
    if(index < 0 || index+count > data.Length) throw new ArgumentOutOfRangeException();
    fixed(char* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of two-byte characters to the stream.</summary>
  public void Write(char* data, int count)
  {
    Write((ushort*)data, count);
  }

  /// <summary>Writes an array of signed two-byte integers to the stream.</summary>
  public void Write(short[] data)
  {
    if(data == null) throw new ArgumentNullException();
    fixed(short* ptr=data) Write(ptr, data.Length);
  }

  /// <summary>Writes an array of signed two-byte integers to the stream.</summary>
  public void Write(short[] data, int index, int count)
  {
    if(data == null) throw new ArgumentNullException();
    if(index < 0 || index+count > data.Length) throw new ArgumentOutOfRangeException();
    fixed(short* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of signed two-byte integers to the stream.</summary>
  public void Write(short* data, int count)
  {
    Write((ushort*)data, count);
  }

  /// <summary>Writes an array of unsigned two-byte integers to the stream.</summary>
  public void Write(ushort[] data)
  {
    if(data == null) throw new ArgumentNullException();
    fixed(ushort* ptr=data) Write(ptr, data.Length);
  }

  /// <summary>Writes an array of unsigned two-byte integers to the stream.</summary>
  public void Write(ushort[] data, int index, int count)
  {
    if(data == null) throw new ArgumentNullException();
    if(index < 0 || index+count > data.Length) throw new ArgumentOutOfRangeException();
    fixed(ushort* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of unsigned two-byte integers to the stream.</summary>
  public void Write(ushort* data, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    Write((void*)data, count, sizeof(ushort));
  }

  /// <summary>Writes an array of signed four-byte integers to the stream.</summary>
  public void Write(int[] data)
  {
    if(data == null) throw new ArgumentNullException();
    fixed(int* ptr=data) Write(ptr, data.Length);
  }

  /// <summary>Writes an array of signed four-byte integers to the stream.</summary>
  public void Write(int[] data, int index, int count)
  {
    if(data == null) throw new ArgumentNullException();
    if(index < 0 || index+count > data.Length) throw new ArgumentOutOfRangeException();
    fixed(int* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of signed four-byte integers to the stream.</summary>
  public void Write(int* data, int count)
  {
    Write((uint*)data, count);
  }

  /// <summary>Writes an array of unsigned four-byte integers to the stream.</summary>
  public void Write(uint[] data)
  {
    if(data == null) throw new ArgumentNullException();
    fixed(uint* ptr=data) Write(ptr, data.Length);
  }

  /// <summary>Writes an array of unsigned four-byte integers to the stream.</summary>
  public void Write(uint[] data, int index, int count)
  {
    if(data == null) throw new ArgumentNullException();
    if(index < 0 || index+count > data.Length) throw new ArgumentOutOfRangeException();
    fixed(uint* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of unsigned four-byte integers to the stream.</summary>
  public void Write(uint* data, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    Write((void*)data, count, sizeof(uint));
  }

  /// <summary>Writes an array of signed eight-byte integers to the stream.</summary>
  public void Write(long[] data)
  {
    if(data == null) throw new ArgumentNullException();
    fixed(long* ptr=data) Write(ptr, data.Length);
  }

  /// <summary>Writes an array of signed eight-byte integers to the stream.</summary>
  public void Write(long[] data, int index, int count)
  {
    if(data == null) throw new ArgumentNullException();
    if(index < 0 || index+count > data.Length) throw new ArgumentOutOfRangeException();
    fixed(long* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of signed eight-byte integers to the stream.</summary>
  public void Write(long* data, int count)
  {
    Write((ulong*)data, count);
  }

  /// <summary>Writes an array of unsigned eight-byte integers to the stream.</summary>
  public void Write(ulong[] data)
  {
    if(data == null) throw new ArgumentNullException();
    fixed(ulong* ptr=data) Write(ptr, data.Length);
  }

  /// <summary>Writes an array of unsigned eight-byte integers to the stream.</summary>
  public void Write(ulong[] data, int index, int count)
  {
    if(data == null) throw new ArgumentNullException();
    if(index < 0 || index+count > data.Length) throw new ArgumentOutOfRangeException();
    fixed(ulong* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of unsigned eight-byte integers to the stream.</summary>
  public void Write(ulong* data, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    Write((void*)data, count, sizeof(ulong));
  }

  /// <summary>Writes an array of four-byte floats to the stream.</summary>
  public void Write(float[] data)
  {
    if(data == null) throw new ArgumentNullException();
    fixed(float* ptr=data) Write(ptr, data.Length);
  }

  /// <summary>Writes an array of four-byte floats to the stream.</summary>
  public void Write(float[] data, int index, int count)
  {
    if(data == null) throw new ArgumentNullException();
    if(index < 0 || index+count > data.Length) throw new ArgumentOutOfRangeException();
    fixed(float* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of four-byte floats to the stream.</summary>
  public void Write(float* data, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    Write((void*)data, count*sizeof(float), 1);
  }

  /// <summary>Writes an array of eight-byte floats to the stream.</summary>
  public void Write(double[] data)
  {
    if(data == null) throw new ArgumentNullException();
    fixed(double* ptr=data) Write(ptr, data.Length);
  }

  /// <summary>Writes an array of eight-byte floats to the stream.</summary>
  public void Write(double[] data, int index, int count)
  {
    if(data == null) throw new ArgumentNullException();
    if(index < 0 || index+count > data.Length) throw new ArgumentOutOfRangeException();
    fixed(double* ptr=data) Write(ptr+index, count);
  }

  /// <summary>Writes an array of eight-byte floats to the stream.</summary>
  public void Write(double* data, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    Write((void*)data, count*sizeof(double), 1);
  }

  /// <summary>Writes the data from the buffer to the underlying stream and then flushes the underlying stream.</summary>
  public void Flush()
  {
    FlushBuffer();
    BaseStream.Flush();
  }

  /// <summary>Writes the data from the buffer to the underlying stream, but does not flush the underlying stream. (Use
  /// <see cref="Flush"/> for that.)</summary>
  public void FlushBuffer()
  {
    BaseStream.Write(Buffer, 0, writeIndex);
    writeIndex = 0;
  }

  /// <summary>Gets the position in the buffer where the next write should occur.</summary>
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
  protected override void Dispose(bool finalizing)
  {
    FlushBuffer();
    base.Dispose(finalizing);
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

  /// <summary>Writes data from the given region of memory to the stream.</summary>
  /// <param name="data">The location in memory of the data to write.</param>
  /// <param name="count">The number of items to write. Each item has a size of <paramref name="wordSize"/> bytes.</param>
  /// <param name="wordSize">The size of each item to copy. The bytes in each item will be swapped to ensure the
  /// correct endianness. If you don't want any swapping to occur, use a value of one for the word size.
  /// </param>
  /// <remarks>This method will not enlarge the buffer unless it is smaller than <paramref name="wordSize"/>. Rather,
  /// it will fill and flush the buffer as many times as is necessary to write the data.
  /// </remarks>
  protected void Write(void* data, int count, int wordSize)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();

    if(wordSize != 1 && wordSize != 2 && wordSize != 4 && wordSize != 8)
    {
      throw new ArgumentOutOfRangeException("Word size must be 1, 2, 4, or 8.");
    }

    EnsureSpace(wordSize); // make sure the buffer is big enough to hold at least one word
    
    int shift = wordSize == 4 ? 2 : wordSize == 8 ? 3 : wordSize-1; // the shift to divide or mulitply by the word size
    byte* src = (byte*)data;
    while(count != 0)
    {
      int toCopy = Math.Min(AvailableSpace>>shift, count), bytesWritten = toCopy<<shift;
      Copy(src, WritePtr, bytesWritten);

      if(wordSize != 1) // make sure the data in the buffer has the correct endianness
      {
        if(wordSize == 4) MakeDesiredEndian4(toCopy);
        else if(wordSize == 2) MakeDesiredEndian2(toCopy);
        else MakeDesiredEndian8(toCopy);
      }

      count      -= toCopy;
      src        += bytesWritten;
      writeIndex += bytesWritten;
      if(writeIndex == Buffer.Length) Flush();
    }
  }

  /// <summary>The amount of empty space in the buffer.</summary>
  int AvailableSpace
  {
    get { return Buffer.Length - writeIndex; }
  }

  int writeIndex;
}
#endregion

} // namespace AdamMil.IO