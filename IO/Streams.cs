/*
AdamMil.IO is a library that provides high performance and high level IO
tools for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2011 Adam Milazzo

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
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using AdamMil.Utilities;
using AdamMil.Utilities.Encodings;

namespace AdamMil.IO
{

#region AggregateStream
/// <summary>Implements a read-only stream that aggregates and concatenates the content of several other streams. If all
/// constituent streams are seekable, the aggregate stream will be seekable as well. An example usage is to prepend or append a
/// header or footer to a large stream without copying everything to temporary file or memory stream.
/// </summary>
/// <remarks>It is not supported for the length any of the constituent streams to be changed while the aggregate stream is in use.</remarks>
public class AggregateStream : Stream
{
  /// <summary>Initializes a new <see cref="AggregateStream"/> with the given set of constituent streams. The streams will be
  /// closed when the <see cref="AggregateStream"/> is closed.
  /// </summary>
  public AggregateStream(params Stream[] streams) : this(true, streams) { }

  /// <summary>Initializes a new <see cref="AggregateStream"/> with the given set of constituent streams. If
  /// <paramref name="ownStreams"/> is true, the streams will be closed when the <see cref="AggregateStream"/> is closed.
  /// </summary>
  public AggregateStream(bool ownStreams, params Stream[] streams)
  {
    if(streams == null) throw new ArgumentNullException();
    if(streams.Contains(null)) throw new ArgumentException("The array contained a null stream.");

    canSeek = streams.Length == 0 || streams.All(s => s.CanSeek);
    if(!streams.All(s => s.CanRead)) throw new ArgumentException("All streams must be readable.");
    if(canSeek) length = streams.Sum(s => s.Length);

    this.ownStreams = ownStreams;
    this.streams    = (Stream[])streams.Clone(); // copy the array to prevent its contents from being changed
    if(streams.Length == 0) currentStream = -1;
  }

  public override bool CanRead
  {
    get { return true; }
  }

  public override bool CanSeek
  {
    get { return canSeek; }
  }

  public override bool CanWrite
  {
    get { return false; }
  }

  public override long Length
  {
    get
    {
      AssertSeekable();
      return length;
    }
  }

  public override long Position
  {
    get { return position; }
    set { Seek(value, SeekOrigin.Begin); }
  }

  protected override void Dispose(bool disposing)
  {
    if(ownStreams)
    {
      foreach(Stream stream in streams) stream.Dispose();
    }
    base.Dispose(disposing);
  }

  public override void Flush()
  {
    foreach(Stream stream in streams) stream.Flush();
  }

  public override int ReadByte()
  {
    if(currentStream == -1) return -1;

    int value;
    while(true)
    {
      value = streams[currentStream].ReadByte();
      if(value != -1)
      {
        position++;
        break;
      }
      else if(currentStream == streams.Length-1)
      {
        break;
      }
      currentStream++;
    }
    return value;
  }

  public override int Read(byte[] buffer, int offset, int count)
  {
    Utility.ValidateRange(buffer, offset, count);
    if(currentStream == -1) return 0;

    int totalRead = 0;
    while(count != 0)
    {
      int read = streams[currentStream].Read(buffer, offset, count);
      if(read == 0)
      {
        if(currentStream == streams.Length-1) break;
        currentStream++;
      }
      else
      {
        position  += read;
        offset    += read;
        count     -= read;
        totalRead += read;
      }
    }
    return totalRead;
  }

  public override long Seek(long offset, SeekOrigin origin)
  {
    AssertSeekable();

    if(origin == SeekOrigin.Current) offset += position;
    else if(origin == SeekOrigin.End) offset += length;

    if((ulong)offset > (ulong)length) throw new ArgumentOutOfRangeException();
    position = offset;

    // find the stream containing the new position
    for(int i=0; i<streams.Length; i++)
    {
      if(offset <= streams[i].Length)
      {
        streams[i].Position = offset;
        currentStream = i;
        for(i++; i<streams.Length; i++) streams[i].Position = 0; // rewind all subsequent streams
        break;
      }

      offset -= streams[i].Length;
    }

    return position;
  }

  public override void SetLength(long value)
  {
    throw new NotSupportedException();
  }

  public override void Write(byte[] buffer, int offset, int count)
  {
    throw new NotSupportedException();
  }

  void AssertSeekable()
  {
    if(!canSeek) throw new NotSupportedException();
  }

  readonly Stream[] streams;
  readonly long length;
  readonly bool canSeek, ownStreams;
  int currentStream;
  long position;
}
#endregion

#region CopyReadStream
/// <summary>Implements as stream that wraps two other streams, a base stream and a copy stream. The stream delegates all
/// operations to the base stream, but data read from the base stream is also copied to the copy stream. An example usage is to
/// create a copy of an unseekable stream, for instance a network stream, for logging purposes.
/// </summary>
public class CopyReadStream : DelegateStream
{
  public CopyReadStream(Stream baseStream, Stream copyStream) : this(baseStream, copyStream, false, true) { }

  public CopyReadStream(Stream baseStream, Stream copyStream, bool autoFlush)
    : this(baseStream, copyStream, autoFlush, true) { }

  public CopyReadStream(Stream baseStream, Stream copyStream, bool autoFlush, bool ownStreams) : base(baseStream, ownStreams)
  {
    if(baseStream == null || copyStream == null) throw new ArgumentNullException();
    if(!baseStream.CanRead || !copyStream.CanWrite)
    {
      throw new ArgumentException("The input stream must be readable and the copy stream must be writable.");
    }

    this.copyStream = copyStream;
    this.ownStreams = ownStreams;
    AutoFlush = autoFlush;
  }

  /// <summary>Gets or sets whether the copy stream will be flushed after each write.</summary>
  public bool AutoFlush
  {
    get; set;
  }

  public override void Flush()
  {
    copyStream.Flush();
    base.Flush();
  }

  public override int Read(byte[] buffer, int offset, int count)
  {
    int bytes = base.Read(buffer, offset, count);
    copyStream.Write(buffer, offset, bytes);
    if(AutoFlush) copyStream.Flush();
    return bytes;
  }

  public override int ReadByte()
  {
    int value = base.ReadByte();
    if(value != -1)
    {
      copyStream.WriteByte((byte)value);
      if(AutoFlush) copyStream.Flush();
    }
    return value;
  }

  protected override void Dispose(bool disposing)
  {
    if(ownStreams) copyStream.Dispose();
    base.Dispose(disposing);
  }

  readonly Stream copyStream;
  readonly bool ownStreams;
}
#endregion

#region DataRecordStream
/// <summary>Implements a read-only stream that reads a value from a column in an <see cref="IDataRecord"/> object. (Note that
/// <see cref="IDataReader" /> objects also implement <see cref="IDataRecord"/>.) This is useful for reading large binary values
/// from databases. It is possible to seek within the stream if the data reader is not forward-only, but it is recommended to use
/// forward-only readers for large binary values, to avoid caching too much data in memory.
/// </summary>
public class DataRecordStream : Stream
{
  /// <summary>Initializes a new <see cref="DataRecordStream"/> with an <see cref="IDataRecord"/> and the index of the column to
  /// read from. The record will be disposed when the stream is closed.
  /// </summary>
  public DataRecordStream(IDataRecord record, int columnIndex) : this(record, columnIndex, true) { }

  /// <summary>Initializes a new <see cref="DataRecordStream"/> with an <see cref="IDataRecord"/> and the index of the column to
  /// read from. If <paramref name="ownRecord"/> is true, the record will be disposed when the stream is closed.
  /// </summary>
  public DataRecordStream(IDataRecord record, int columnIndex, bool ownRecord)
  {
    if(record == null) throw new ArgumentNullException();
    if((uint)columnIndex >= (uint)record.FieldCount) throw new ArgumentOutOfRangeException();
    this.record    = record;
    this.column    = columnIndex;
    this.length    = CalculateLength();
    this.ownRecord = ownRecord;
  }

  /// <summary>Initializes a new <see cref="DataRecordStream"/> with an <see cref="IDataRecord"/> and the name of the column to
  /// read from. The record will be disposed when the stream is closed.
  /// </summary>
  public DataRecordStream(IDataRecord record, string columnName) : this(record, columnName, true) { }

  /// <summary>Initializes a new <see cref="DataRecordStream"/> with an <see cref="IDataRecord"/> and the name of the column to
  /// read from. If <paramref name="ownRecord"/> is true, the record will be disposed when the stream is closed.
  /// </summary>
  public DataRecordStream(IDataRecord record, string columnName, bool ownRecord)
  {
    if(record == null) throw new ArgumentNullException();

    try { this.column = record.GetOrdinal(columnName); }
    catch(IndexOutOfRangeException) { throw new ArgumentException("No such column name: " + columnName); }

    this.record    = record;
    this.length    = CalculateLength();
    this.ownRecord = ownRecord;
  }

  public sealed override bool CanRead
  {
    get { return true; }
  }

  public override bool CanSeek
  {
    get { return true; }
  }

  public override bool CanWrite
  {
    get { return false; }
  }

  public override long Length
  {
    get { return length; }
  }

  public override long Position
  {
    get { return position; }
    set { Seek(value, SeekOrigin.Begin); }
  }

  public override void Flush() { }

  public override int Read(byte[] buffer, int offset, int count)
  {
    long bytesRead = record.GetBytes(column, position, buffer, offset, count);
    position += bytesRead;
    return (int)bytesRead;
  }

  public override long Seek(long offset, SeekOrigin origin)
  {
    if(origin == SeekOrigin.Current) offset += position;
    else if(origin == SeekOrigin.End) offset += length;

    if((ulong)offset > (ulong)length) throw new ArgumentOutOfRangeException();
    return position = offset;
  }

  public override void SetLength(long value)
  {
    throw new NotSupportedException();
  }

  public override void Write(byte[] buffer, int offset, int count)
  {
    throw new NotSupportedException();
  }

  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);
    if(ownRecord) Utility.Dispose(record);
  }

  long CalculateLength()
  {
    return record.GetBytes(column, 0, null, 0, 0);
  }

  readonly IDataRecord record;
  readonly long length;
  long position;
  readonly int column;
  readonly bool ownRecord;
}
#endregion

#region DelegateStream
/// <summary>Implements as stream that delegates all operations to another stream. This is meant to be used as a base class for
/// other streams, but can be used as-is, for instance to wrap a stream to prevent it from being closed by an API that always
/// closes streams given to it.
/// </summary>
public class DelegateStream : Stream
{
  public DelegateStream(Stream baseStream, bool ownStream)
  {
    if(baseStream == null) throw new ArgumentNullException();
    this.baseStream = baseStream;
    this.ownStream  = ownStream;
  }

  public override bool CanRead
  {
    get { return baseStream.CanRead; }
  }

  public override bool CanSeek
  {
    get { return baseStream.CanSeek; }
  }

  public override bool CanTimeout
  {
    get { return baseStream.CanTimeout; }
  }

  public override bool CanWrite
  {
    get { return baseStream.CanWrite; }
  }

  public override long Length
  {
    get { return baseStream.Length; }
  }

  public override long Position
  {
    get { return baseStream.Position; }
    set { baseStream.Position = value; }
  }

  public override int ReadTimeout
  {
    get { return baseStream.ReadTimeout; }
    set { baseStream.ReadTimeout = value; }
  }

  public override int WriteTimeout
  {
    get { return baseStream.WriteTimeout; }
    set { baseStream.WriteTimeout = value; }
  }

  public override void Flush()
  {
    baseStream.Flush();
  }

  public override int Read(byte[] buffer, int offset, int count)
  {
    return baseStream.Read(buffer, offset, count);
  }

  public override int ReadByte()
  {
    return baseStream.ReadByte();
  }

  public override long Seek(long offset, SeekOrigin origin)
  {
    return baseStream.Seek(offset, origin);
  }

  public override void SetLength(long length)
  {
    baseStream.SetLength(length);
  }

  public override void Write(byte[] buffer, int offset, int count)
  {
    baseStream.Write(buffer, offset, count);
  }

  public override void WriteByte(byte value)
  {
    baseStream.WriteByte(value);
  }

  protected Stream InnerStream
  {
    get { return baseStream; }
  }

  protected override void Dispose(bool disposing)
  {
    if(ownStream) baseStream.Dispose();
    base.Dispose(disposing);
  }

  readonly Stream baseStream;
  readonly bool ownStream;
}
#endregion

#region EncodedStream
/// <summary>The <see cref="EncodedStream"/> wraps an underlying stream. Data read from or written to the encoded stream can be
/// decoded or encoded using a <see cref="BinaryEncoding"/> or a pair of <see cref="BinaryEncoder"/> objects. An example usage is
/// to create a stream that performs base64 encoding or decoding by using <see cref="Base64Encoding"/>.
/// </summary>
public class EncodedStream : Stream
{
  /// <summary>Initializes a new <see cref="EncodedStream"/> with the given base stream and <see cref="BinaryEncoding"/>. The
  /// result is stream that will use <see cref="BinaryEncoding.GetEncoder"/> to write and <see cref="BinaryEncoding.GetDecoder"/>
  /// to read. The base stream will be closed when the encoded stream is closed.
  /// </summary>
  public EncodedStream(Stream baseStream, BinaryEncoding encoding) : this(baseStream, encoding, true) { }

  /// <summary>Initializes a new <see cref="EncodedStream"/> with the given base stream and <see cref="BinaryEncoding"/>. The
  /// result is stream that will use <see cref="BinaryEncoding.GetEncoder"/> to write and <see cref="BinaryEncoding.GetDecoder"/>
  /// to read. If <paramref name="ownStream"/> is true, the base stream will be closed when the encoded stream is closed.
  /// </summary>
  public EncodedStream(Stream baseStream, BinaryEncoding encoding, bool ownStream)
    : this(baseStream, GetDecoder(encoding), GetEncoder(encoding), true, ownStream) { }

  /// <summary>Initializes a new <see cref="EncodedStream"/> with the given base stream and a pair of <see cref="BinaryEncoder"/>
  /// objects. Either <see cref="BinaryEncoder"/> may be null. If the <paramref name="readEncoder"/> is given, the underlying
  /// stream data will be encoded using that encoder. Similarly, if the <paramref name="writeEncoder"/> is given, data written
  /// will be encoded using that encoder before being written to the base stream. A null encoder will prevent the stream from being read
  /// from or written to (depending on which encoder is null). The base stream will be closed when the encoded stream is closed.
  /// </summary>
  public EncodedStream(Stream baseStream, BinaryEncoder readEncoder, BinaryEncoder writeEncoder)
    : this(baseStream, readEncoder, writeEncoder, true, true) { }

  /// <summary>Initializes a new <see cref="EncodedStream"/> with the given base stream and a pair of <see cref="BinaryEncoder"/>
  /// objects. Either <see cref="BinaryEncoder"/> may be null. If the <paramref name="readEncoder"/> is given, the underlying
  /// stream data will be encoded using that encoder. Similarly, if the <paramref name="writeEncoder"/> is given, data written
  /// will be encoded using that encoder before being written to the base stream. If <paramref name="requireEncoding"/> is true, a null
  /// encoder will prevent the stream from being read from or written to (depending on which encoder is null). If false, a null encoder
  /// will allow the data to pass through unchanged. The base stream will be closed when the encoded stream is closed.
  /// </summary>
  public EncodedStream(Stream baseStream, BinaryEncoder readEncoder, BinaryEncoder writeEncoder, bool requireEncoding)
    : this(baseStream, readEncoder, writeEncoder, requireEncoding, true) { }

  /// <summary>Initializes a new <see cref="EncodedStream"/> with the given base stream and a pair of <see cref="BinaryEncoder"/>
  /// objects. Either <see cref="BinaryEncoder"/> may be null. If the <paramref name="readEncoder"/> is given, the underlying
  /// stream data will be encoded using that encoder. Similarly, if the <paramref name="writeEncoder"/> is given, data written
  /// will be encoded using that encoder before being written to the base stream. If <paramref name="requireEncoding"/> is true, a null
  /// encoder will prevent the stream from being read from or written to (depending on which encoder is null). If false, a null encoder
  /// will allow the data to pass through unchanged. If <paramref name="ownStream"/> is true, the base stream will be closed when the
  /// encoded stream is closed.
  /// </summary>
  public EncodedStream(Stream baseStream, BinaryEncoder readEncoder, BinaryEncoder writeEncoder, bool requireEncoding, bool ownStream)
  {
    if(baseStream == null) throw new ArgumentNullException();

    this.baseStream      = baseStream;
    this.readEncoder     = readEncoder;
    this.writeEncoder    = writeEncoder;
    this.requireEncoding = requireEncoding;
    this.ownStream       = ownStream;
  }

  public override bool CanRead
  {
    get { return (!requireEncoding || readEncoder != null) && baseStream.CanRead; }
  }

  public override bool CanSeek
  {
    get { return false; }
  }

  public override bool CanWrite
  {
    get { return (!requireEncoding || writeEncoder != null) && baseStream.CanWrite; }
  }

  public override long Length
  {
    get { throw new NotSupportedException(); }
  }

  public override long Position
  {
    get { throw new NotSupportedException(); }
    set { throw new NotSupportedException(); }
  }

  public override void Flush()
  {
    baseStream.Flush();
  }

  public void FlushEncoder()
  {
    EncodedWrite(new byte[0], 0, 0, true);
  }

  public override int Read(byte[] buffer, int offset, int count)
  {
    if(readEncoder == null)
    {
      if(requireEncoding) throw new NotSupportedException();
      return baseStream.Read(buffer, offset, count);
    }
    else
    {
      Utility.ValidateRange(buffer, offset, count);

      if(readBuffer == null) readBuffer = new ByteBuffer(4096);

      int totalRead = 0;
      while(count != 0)
      {
        if(readBuffer.Count == 0)
        {
          if(readEncoder.CanEncodeInPlace)
          {
            readBuffer.AddCount(baseStream.Read(readBuffer.GetContiguousArray(), 0, readBuffer.Capacity));
            bool flush = readBuffer.Count == 0;
            readBuffer.SetCount(readEncoder.Encode(readBuffer.Buffer, 0, readBuffer.Count, readBuffer.Buffer, 0, flush));
          }
          else
          {
            if(encodeBuffer == null) encodeBuffer = new ByteBuffer(readBuffer.Capacity);
            else encodeBuffer.Clear();
            encodeBuffer.AddCount(baseStream.Read(encodeBuffer.Buffer, 0, encodeBuffer.Capacity));
            bool flush = encodeBuffer.Count == 0;
            // use the value from GetMaxBytes(), which should have much higher performance, as long as the result is not too large
            int maxNeeded = readEncoder.GetMaxBytes(encodeBuffer.Count);
            readBuffer.EnsureCapacity(maxNeeded <= MaxEncodeBufferSize ?
                                        maxNeeded : readEncoder.GetByteCount(encodeBuffer.Buffer, 0, encodeBuffer.Count, flush));
            readBuffer.AddCount(readEncoder.Encode(encodeBuffer.Buffer, 0, encodeBuffer.Count, readBuffer.Buffer, 0, flush));
          }

          if(readBuffer.Count == 0) break;
        }

        int read = Math.Min(readBuffer.Count, count);
        readBuffer.Remove(buffer, offset, read);
        offset    += read;
        count     -= read;
        totalRead += read;
      }
      return totalRead;
    }
  }

  public override long Seek(long offset, SeekOrigin origin)
  {
    throw new NotSupportedException();
  }

  public override void SetLength(long value)
  {
    baseStream.SetLength(value);
  }

  public override void Write(byte[] buffer, int offset, int count)
  {
    if(writeEncoder == null)
    {
      if(requireEncoding) throw new NotSupportedException();
      baseStream.Write(buffer, offset, count);
    }
    else
    {
      Utility.ValidateRange(buffer, offset, count);
      if(writeBuffer == null) writeBuffer = new ByteBuffer();
      EncodedWrite(buffer, offset, count, false);
    }
  }

  protected override void Dispose(bool disposing)
  {
    if(writeBuffer != null)
    {
      FlushEncoder();
      writeBuffer = null;
    }

    if(ownStream) baseStream.Dispose();
    base.Dispose(disposing);
  }

  const int MaxEncodeBufferSize = 64*1024;

  void EncodedWrite(byte[] buffer, int offset, int count, bool flush)
  {
    // use the value from GetMaxBytes(), which should have much higher performance, as long as the result is not too large
    int maxNeeded = writeEncoder.GetMaxBytes(count);
    writeBuffer.EnsureCapacity(maxNeeded <= MaxEncodeBufferSize ? maxNeeded : writeEncoder.GetByteCount(buffer, offset, count, flush));
    baseStream.Write(writeBuffer.Buffer, 0, writeEncoder.Encode(buffer, offset, count, writeBuffer.Buffer, 0, flush));
  }

  readonly Stream baseStream;
  readonly BinaryEncoder readEncoder, writeEncoder;
  readonly bool ownStream, requireEncoding;
  ByteBuffer readBuffer, writeBuffer, encodeBuffer;

  static BinaryEncoder GetDecoder(BinaryEncoding encoding)
  {
    if(encoding == null) throw new ArgumentNullException();
    return encoding.GetDecoder();
  }

  static BinaryEncoder GetEncoder(BinaryEncoding encoding)
  {
    if(encoding == null) throw new ArgumentNullException();
    return encoding.GetEncoder();
  }
}
#endregion

#region ReferenceCountedStream
/// <summary>Wraps a stream using a reference count, so the wrapped stream won't be closed until the
/// <see cref="ReferenceCountedStream"/> has been closed enough times. This class is useful for passing to methods
/// that close the stream you give them, when you don't want the stream to be closed or you want to pass the same
/// stream to several such methods and only have it closed when all the methods have finished with it.
/// </summary>
public sealed class ReferenceCountedStream : DelegateStream
{
  public ReferenceCountedStream(Stream underlyingStream, int referenceCount) : base(underlyingStream, true)
  {
    if(underlyingStream == null) throw new ArgumentNullException();
    if(referenceCount < 1) throw new ArgumentOutOfRangeException();
    this.referenceCount = referenceCount;
  }

  protected override void Dispose(bool disposing)
  {
    if(referenceCount != 0) referenceCount--;
    else base.Dispose(disposing);
  }

  int referenceCount;
}
#endregion

#region StreamStream
/// <summary>This class provides a stream based on a portion of an existing stream.</summary>
/// <remarks>Many methods taking stream arguments expect the entire range of the stream to be devoted to the data
/// being read by that method. You may want to use a stream containing other data as well with one of those methods.
/// This class allows you to create a stream from a section of another stream. This class is not thread-safe and
/// should not be used by multiple threads simultaneously.
/// </remarks>
public class StreamStream : DelegateStream
{
  /// <param name="stream">The underlying stream. It is assumed that the underlying stream will not be used by other
  /// code while this stream is in use. The underlying stream will be closed automatically when this stream is closed.
  /// </param>
  /// <param name="length">The length of the <see cref="StreamStream"/>, beginning from the current position within the
  /// underlying stream.
  /// </param>
  /// <remarks>If you want to use the underlying stream in other code while this stream is in use, use one of the
  /// other constructors, such as <see cref="StreamStream(Stream,long,long,bool)"/>.
  /// </remarks>
  public StreamStream(Stream stream, long length) : this(stream, stream.CanSeek ? stream.Position : 0, length, false, true) { }

  /// <param name="stream">The underlying stream. It is assumed that the underlying stream will not be used by other
  /// code while this stream is in use. The underlying stream will be closed automatically when this stream is closed.
  /// </param>
  /// <param name="start">The starting position of this stream within the underlying stream. If the underlying stream is seekable,
  /// the position is taken to be relative to the beginning of the underlying stream. Otherwise, it is taken to be relative to the
  /// current position within the underlying stream.
  /// </param>
  /// <param name="length">The length of the <see cref="StreamStream"/>.</param>
  /// <remarks>If you want to use the underlying stream in other code while this stream is in use, use one of the
  /// other constructors, such as <see cref="StreamStream(Stream,long,long,bool)"/>.
  /// </remarks>
  public StreamStream(Stream stream, long start, long length) : this(stream, start, length, false, true) { }

  /// <param name="stream">The underlying stream. Unseekable streams cannot be shared (<paramref name="shared"/> must
  /// be false). If the stream is unseekable, then <paramref name="start" /> is assumed to be relative to the current position
  /// within the stream. Otherwise, it is assumed to be relative to the beginning of the stream. If <paramref name="shared"/> is
  /// false, the stream will be closed automatically when the <see cref="StreamStream"/> is closed. If <paramref name="shared"/>
  /// is true, the stream will not be closed automatically.
  /// </param>
  /// <param name="start">The starting position of this stream within the underlying stream. If the underlying stream is seekable,
  /// the position is taken to be relative to the beginning of the underlying stream. Otherwise, it is taken to be relative to the
  /// current position within the underlying stream.
  /// </param>
  /// <param name="length">The length of the <see cref="StreamStream"/>.</param>
  /// <param name="shared">If set to true, the underlying stream will be seeked to the expected position before
  /// each operation in case some other code has moved the file pointer. If set to false, it is assumed that other
  /// code will not touch the underlying stream while this stream is in use, so the additional seeking can be avoided.
  /// Note that setting this to true does not make the stream thread-safe.
  /// </param>
  public StreamStream(Stream stream, long start, long length, bool shared)
    : this(stream, start, length, shared, !shared) { }

  /// <param name="stream">The underlying stream. Unseekable streams cannot be shared (<paramref name="shared"/> must
  /// be false). If the stream is unseekable, then <paramref name="start" /> is assumed to be relative to the current position
  /// within the stream. Otherwise, it is assumed to be relative to the beginning of the stream.
  /// </param>
  /// <param name="start">The starting position of this stream within the underlying stream. If the underlying stream is seekable,
  /// the position is taken to be relative to the beginning of the underlying stream. Otherwise, it is taken to be relative to the
  /// current position within the underlying stream.
  /// </param>
  /// <param name="length">The length of the <see cref="StreamStream"/>.</param>
  /// <param name="shared">If set to true, the underlying stream will be seeked to the expected position before
  /// each operation in case some other code has moved the file pointer. If set to false, it is assumed that other
  /// code will not touch the underlying stream while this stream is in use, so the additional seeking can be avoided.
  /// Note that setting this to true does not make the stream thread-safe.
  /// </param>
  /// <param name="closeInner">If set to true, the underlying stream will be closed automatically when this stream
  /// is closed.
  /// </param>
  public StreamStream(Stream stream, long start, long length, bool shared, bool closeInner) : base(stream, closeInner)
  {
    if(stream == null) throw new ArgumentNullException();
    if(start < 0 || length < 0) throw new ArgumentOutOfRangeException("'start' or 'length'", "cannot be negative");

    if(stream.CanSeek)
    {
      if(!shared) stream.Position = start;
    }
    else
    {
      if(shared) throw new ArgumentException("If using an unseekable stream, 'shared' must be false");
      if(start != 0) stream.Skip(start);
    }

    this.start  = start;
    this.length = length;
    this.shared = shared;
  }

  /// <summary>Returns the length of this stream.</summary>
  public override long Length
  {
    get { return length; }
  }

  /// <summary>Gets/sets the current position within this stream.</summary>
  /// <remarks>You cannot seek past the end of a <see cref="StreamStream"/>. However, you can first use
  /// <see cref="SetLength"/> to increase the size of the stream and then seek past what was previously the end.
  /// <seealso cref="Seek"/>
  /// </remarks>
  public override long Position
  {
    get { return position; }
    set { Seek(value, SeekOrigin.Begin); }
  }

  /// <summary>Reads a sequence of bytes from the underlying stream and advances the position within this stream by
  /// the number of bytes read.
  /// </summary>
  /// <param name="buffer">An array of bytes into which data will be read.</param>
  /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data
  /// read from the underlying stream.
  /// </param>
  /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
  /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested
  /// if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
  /// </returns>
  public override int Read(byte[] buffer, int offset, int count)
  {
    if(count > length-position) count = (int)(length-position); // make sure we don't read past the end of the stream
    FixPosition();
    int read = InnerStream.Read(buffer, offset, count);
    position += read;
    return read;
  }

  /// <summary>Reads a byte from the stream and advances the position within this stream by one byte, or returns -1
  /// if at the end of the stream.
  /// </summary>
  /// <returns>The unsigned byte cast to an integer, or -1 if at the end of the stream.</returns>
  public override int ReadByte()
  {
    if(position >= length) return -1;
    FixPosition();
    int value = InnerStream.ReadByte();
    if(value != -1) position++;
    return value;
  }

  /// <summary>Sets the position within the current stream.</summary>
  /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
  /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain
  /// the new position.
  /// </param>
  /// <returns>The new position within the current stream.</returns>
  /// <remarks>You cannot seek past the end of a <see cref="StreamStream"/>. However, you can first use
  /// <see cref="SetLength"/> to increase the size of the stream and then seek past what was previously the end.
  /// <seealso cref="Position"/>
  /// </remarks>
  public override long Seek(long offset, SeekOrigin origin)
  {
    if(origin == SeekOrigin.Current) offset += position;
    else if(origin == SeekOrigin.End) offset += length;
    if((ulong)offset > (ulong)length) throw new ArgumentOutOfRangeException("Cannot seek outside the bounds of this stream.");
    position = InnerStream.Seek(start+offset, SeekOrigin.Begin) - start;
    return position;
  }

  /// <summary>Sets the length of the current and underlying streams.</summary>
  /// <param name="length">The desired length of the current stream in bytes.</param>
  /// <remarks>This method calls <see cref="Stream.SetLength"/> on the underlying stream, passing the given length plus the
  /// starting position of the <see cref="StreamStream"/>. If, after altering this stream's length, <see cref="Position"/> would
  /// be past the end of the range, it will be set to the end of the new range.
  /// </remarks>
  public override void SetLength(long length)
  {
    if(length < 0) throw new ArgumentOutOfRangeException();
    base.SetLength(length + start);
    this.length = length;
    if(position > length) Position = length;
  }

  /// <summary>Writes a sequence of bytes to the underlying stream and advances the current position by the number of
  /// bytes written.
  /// </summary>
  /// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes starting from
  /// <paramref name="offset"/> to the underlying stream.
  /// </param>
  /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to
  /// the underlying stream.
  /// </param>
  /// <param name="count">The number of bytes to be written to the underlying stream.</param>
  /// <remarks>You cannot write past the end of a <see cref="StreamStream"/>. However, you can first use
  /// <see cref="SetLength"/> to increase the size of the stream and then write data past what was previously the end.
  /// </remarks>
  public override void Write(byte[] buffer, int offset, int count)
  {
    if(count > length-position)
    {
      throw new ArgumentException("Cannot write past the end of a StreamStream (try resizing with SetLength first?)");
    }
    FixPosition();
    InnerStream.Write(buffer, offset, count);
    position = InnerStream.Position - start;
  }

  /// <summary>Writes a byte to the current position in the stream and advances the current position by one byte.</summary>
  /// <param name="value">The byte to write to the stream.</param>
  /// <remarks>You cannot write past the end of a <see cref="StreamStream"/>. However, you can first use
  /// <see cref="SetLength"/> to increase the size of the stream and then write data past what was previously the end.
  /// </remarks>
  public override void WriteByte(byte value)
  {
    if(position >= length)
    {
      throw new InvalidOperationException("Cannot write past the end of a StreamStream (try resizing with SetLength first?)");
    }
    FixPosition();
    InnerStream.WriteByte(value);
    position++;
  }

  /// <summary>Seeks the underlying stream to its expected place, if it is shared and thus may have been used by other code.</summary>
  void FixPosition()
  {
    if(shared) InnerStream.Position = position + start;
  }

  long start, length, position;
  readonly bool shared;
}
#endregion

#region TeeStream
/// <summary>Implements a stream that wraps several other streams. Everything written to the <see cref="TeeStream"/> is written
/// to all constituent streams. An example use is to write something to multiple places, for instance to a network as well as a
/// log file.
/// </summary>
public class TeeStream : Stream
{
  /// <summary>Initializes a new <see cref="TeeStream"/> with the given list of constituent streams. The constituent streams will
  /// be closed when the <see cref="TeeStream"/> is closed.
  /// </summary>
  public TeeStream(params Stream[] streams) : this(true, streams) { }

  /// <summary>Initializes a new <see cref="TeeStream"/> with the given list of constituent streams. If
  /// <paramref name="ownStreams"/> is true, the constituent streams will be closed when the <see cref="TeeStream"/> is closed.
  /// </summary>
  public TeeStream(bool ownStreams, params Stream[] streams)
  {
    if(streams == null) throw new ArgumentNullException();
    if(streams.Contains(null)) throw new ArgumentException("The array contained a null stream.");

    if(!streams.All(s => s.CanWrite)) throw new ArgumentException("All streams must be writable.");

    this.ownStreams = ownStreams;
    this.streams    = (Stream[])streams.Clone(); // copy the array to prevent its contents from being changed
  }

  public override bool CanRead
  {
    get { return false; }
  }

  public override bool CanSeek
  {
    get { return false; }
  }

  public override bool CanWrite
  {
    get { return true; }
  }

  public override long Length
  {
    get { throw new NotSupportedException(); }
  }

  public override long Position
  {
    get { throw new NotSupportedException(); }
    set { throw new NotSupportedException(); }
  }

  public override void Flush()
  {
    foreach(Stream stream in streams) stream.Flush();
  }

  public override int Read(byte[] buffer, int offset, int count)
  {
    throw new NotSupportedException();
  }

  public override long Seek(long offset, SeekOrigin origin)
  {
    throw new NotSupportedException();
  }

  public override void SetLength(long value)
  {
    foreach(Stream stream in streams) stream.SetLength(value);
  }

  public override void Write(byte[] buffer, int offset, int count)
  {
    foreach(Stream stream in streams) stream.Write(buffer, offset, count);
  }

  public override void WriteByte(byte value)
  {
    foreach(Stream stream in streams) stream.WriteByte(value);
  }

  protected override void Dispose(bool disposing)
  {
    if(ownStreams)
    {
      foreach(Stream stream in streams) stream.Dispose();
    }

    base.Dispose(disposing);
  }

  readonly Stream[] streams;
  readonly bool ownStreams;
}
#endregion

#region TemporaryFileStream
/// <summary>Represents a stream whose content is contained within a temporary file. The temporary file will be deleted when the
/// stream is disposed.
/// </summary>
public class TemporaryFileStream : FileStream
{
  /// <summary>Initializes a new, empty temporary file with a random name.</summary>
  public TemporaryFileStream() : this(Path.GetTempFileName(), null) { }

  /// <summary>Initializes a new temporary file with a random name, copies the content from <paramref name="initialContent"/> (if
  /// it's not null), and then rewinds the temporary file stream back to the beginning.
  /// </summary>
  public TemporaryFileStream(Stream initialContent) : this(Path.GetTempFileName(), initialContent, true) { }

  /// <summary>Opens or creates a new temporary file with the given name.</summary>
  public TemporaryFileStream(string tempFilePath)
    : base(tempFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
  {
    this.tempFilePath = tempFilePath;
  }

  /// <summary>Initializes a new temporary file with the given name, copies the content from
  /// <paramref name="initialContent"/> (if it's not null), and then rewinds the temporary file stream back to the beginning.
  /// </summary>
  public TemporaryFileStream(string tempFilePath, Stream initialContent) : this(tempFilePath, initialContent, true) { }

  /// <summary>Initializes a new, empty temporary file with the given name and copies the content from
  /// <paramref name="initialContent"/> (if it's not null). If <paramref name="rewindAfterCopy"/> is true, the stream will be
  /// rewound back to the beginning after copying the content from <paramref name="initialContent"/>. Otherwise, it will be left
  /// at the end.
  /// </summary>
  public TemporaryFileStream(string tempFilePath, Stream initialContent, bool rewindAfterCopy)
    : base(tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None)
  {
    this.tempFilePath = tempFilePath;

    if(initialContent != null)
    {
      initialContent.CopyTo(this);
      if(rewindAfterCopy) Position = 0;
    }
  }

  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);
    File.Delete(tempFilePath);
  }

  readonly string tempFilePath;
}
#endregion

#region TextStream
/// <summary>Performs the reverse operation of a <see cref="StreamReader"/>. That is, it converts a source of text back into a
/// stream of bytes. It is not supported for the text source to become shorter during the lifetime of the
/// <see cref="TextStream"/>, although it may become longer. A <see cref="TextStream"/> is similar to a
/// <see cref="StreamWriter"/>, but also supports the use of <see cref="Encoder"/> objects (in addition to <see cref="Encoding"/>
/// objects) and doesn't require a temporary stream for storage of the data. The <see cref="TextStream"/> is capable of reading
/// from string, <see cref="StringBuilder"/>, and <see cref="TextReader"/> objects.
/// </summary>
public class TextStream : Stream
{
  public TextStream(string text) : this(text, Encoding.UTF8) { }
  public TextStream(string text, Encoding encoding) : this(text, encoding.GetEncoder()) { }
  public TextStream(string text, Encoder encoder)
  {
    if(encoder == null) throw new ArgumentNullException("encoder");
    this.str     = text == null ? "" : text;
    this.encoder = encoder;
    encoder.Reset();
  }

  public TextStream(StringBuilder text) : this(text, Encoding.UTF8) { }
  public TextStream(StringBuilder text, Encoding encoding) : this(text, encoding.GetEncoder()) { }
  public TextStream(StringBuilder text, Encoder encoder)
  {
    if(text == null || encoder == null) throw new ArgumentNullException();
    this.builder = text;
    this.encoder = encoder;
    encoder.Reset();
  }

  public TextStream(TextReader reader) : this(reader, Encoding.UTF8) { }
  public TextStream(TextReader reader, Encoding encoding) : this(reader, encoding.GetEncoder()) { }
  public TextStream(TextReader reader, Encoder encoder)
  {
    if(reader == null || encoder == null) throw new ArgumentNullException();
    this.reader  = reader;
    this.encoder = encoder;
    encoder.Reset();
  }

  public override bool CanRead
  {
    get { return true; }
  }

  public override bool CanSeek
  {
    get { return false; }
  }

  public override bool CanWrite
  {
    get { return false; }
  }

  public override long Length
  {
    get { throw new NotSupportedException(); }
  }

  public override long Position
  {
    get { throw new NotSupportedException(); }
    set { throw new NotSupportedException(); }
  }

  public override void Flush()
  {
  }

  public override int Read(byte[] buffer, int offset, int count)
  {
    Utility.ValidateRange(buffer, offset, count);

    int totalRead = 0;
    while(count != 0)
    {
      // first try to service the request from the buffer
      if(byteBuffer != null && byteBuffer.Count != 0)
      {
        int read = Math.Min(count, byteBuffer.Count);
        byteBuffer.Remove(buffer, offset, read);
        offset    += read;
        count     -= read;
        totalRead += read;
      }

      // then try to get more characters from the character source
      int chars = Math.Min(1024, (count+1)/2); // assume a 2:1 ratio of bytes:characters
      if(reader == null) chars = Math.Min(chars, (builder != null ? builder.Length : str.Length) - charPosition);
      if(charBuffer == null || charBuffer.Length < chars) charBuffer = new char[Math.Max(64, chars)];

      if(reader != null)
      {
        chars = reader.Read(charBuffer, 0, chars);
      }
      else if(builder != null)
      {
        chars = Math.Min(chars, builder.Length-charPosition);
        builder.CopyTo(charPosition, charBuffer, 0, chars);
      }
      else
      {
        chars = Math.Min(chars, str.Length-charPosition);
        str.CopyTo(charPosition, charBuffer, 0, chars);
      }

      if(chars == 0) break;

      charPosition += chars;
      byte[] byteArray = byteBuffer.GetArrayForWriting(encoder.GetByteCount(charBuffer, 0, chars, chars == 0));
      byteBuffer.AddCount(encoder.GetBytes(charBuffer, 0, chars, byteArray, byteBuffer.Offset, chars == 0));
    }

    return totalRead;
  }

  public override long Seek(long offset, SeekOrigin origin)
  {
    throw new NotSupportedException();
  }

  public override void SetLength(long value)
  {
    throw new NotSupportedException();
  }

  public override void Write(byte[] buffer, int offset, int count)
  {
    throw new NotSupportedException();
  }

  readonly Encoder encoder;
  readonly ArrayBuffer<byte> byteBuffer = new ArrayBuffer<byte>(256);
  char[] charBuffer;
  TextReader reader;
  StringBuilder builder;
  string str;
  int charPosition;
}
#endregion

} // namespace AdamMil.IO
