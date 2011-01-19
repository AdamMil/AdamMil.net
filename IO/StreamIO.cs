/*
AdamMil.IO is a library that provides high performance and high level IO
tools for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2010 Adam Milazzo

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
using System.IO;
using AdamMil.Utilities;

namespace AdamMil.IO
{

/// <summary>This delegate is used by the <see cref="StreamExtensions.ProcessStream"/> method to process a stream in chunks.
/// It is given a chunk of data to process and returns true if processing should continue or false if it should stop.
/// </summary>
/// <param name="buffer">The buffer containing the chunk of data.</param>
/// <param name="dataLength">The number of bytes in the buffer.</param>
/// <returns>Returns true if processing should continue or false if it should stop.</returns>
public delegate bool StreamProcessor(byte[] buffer, int dataLength);

#region StreamExtensions
/// <summary>This class provides methods and extension methods for reading and writing numeric and string values
/// from/to streams with little or big endianness.
/// </summary>
public unsafe static partial class StreamExtensions
{
  /// <summary>Copies a source stream into a destination stream and returns the number of bytes copied. The streams
  /// are not rewound or disposed.
  /// </summary>
  public static int CopyTo(this Stream source, Stream dest) { return CopyTo(source, dest, false, 0); }

  /// <summary>Copies a source stream into a destination stream and returns the number of bytes copied.</summary>
  public static int CopyTo(this Stream source, Stream dest, bool rewindSource)
  {
    return CopyTo(source, dest, rewindSource, 0);
  }

  /// <summary>Copies a source stream into a destination stream and returns the number of bytes copied.</summary>
  /// <param name="source">The stream from which the source data will be copied.</param>
  /// <param name="dest">The stream into which the source data will be written.</param>
  /// <param name="rewindSource">If true, the source stream's <see cref="Stream.Position"/> property will be set
  /// to 0 first to ensure that the entire source stream is copied.
  /// </param>
  /// <param name="bufferSize">The size of the buffer to use. Passing zero will use a default value.</param>
  public static int CopyTo(Stream source, Stream dest, bool rewindSource, int bufferSize)
  {
    if(source == null || dest == null) throw new ArgumentNullException();
    if(bufferSize < 0) throw new ArgumentOutOfRangeException("bufferSize");
    if(bufferSize == 0) bufferSize = 4096;

    if(rewindSource) source.Position = 0;
    byte[] buf = new byte[bufferSize];
    int read, total = 0;
    while(true)
    {
      read = source.Read(buf, 0, bufferSize);
      if(read == 0) return total;
      total += read;
      dest.Write(buf, 0, read);
    }
  }

  /// <summary>Processes a stream in chunks of 4096 bytes, using the given <see cref="StreamProcessor"/>.</summary>
  public static void Process(this Stream stream, StreamProcessor processor)
  {
    stream.Process(processor, 4096);
  }

  /// <summary>Processes a stream in chunks of the given size, using the given <see cref="StreamProcessor"/>.</summary>
  public static void Process(this Stream stream, StreamProcessor processor, int chunkSize)
  {
    if(stream == null || processor == null) throw new ArgumentNullException();
    if(chunkSize <= 0) throw new ArgumentOutOfRangeException();

    byte[] buffer = new byte[chunkSize];
    int read;
    do read = stream.Read(buffer, 0, chunkSize);
    while(read != 0 && processor(buffer, read));
  }

  /// <summary>Reads the given number of bytes from a stream.</summary>
  /// <returns>A byte array containing <paramref name="length"/> bytes of data.</returns>
  public static byte[] Read(this Stream stream, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    byte[] buf = new byte[count];
    stream.ReadOrThrow(buf, 0, count);
    return buf;
  }

  /// <summary>Reads the given number of bytes from a stream into a buffer, and throws an exception if the given number of bytes
  /// cannot be read.
  /// </summary>
  /// <returns>The number of bytes read. This will always be equal to <paramref name="length"/>.</returns>
  public static int ReadOrThrow(this Stream stream, byte[] buf, int index, int count)
  {
    return Read(stream, buf, index, count, true);
  }

  /// <summary>Tries to read the given number of bytes from a stream into a buffer. This will always read the requested amount of
  /// data if it exists within the stream. If not all the requested bytes could be read, the method will either return the number
  /// of bytes actually read (if <paramref name="throwOnEOF"/> is false) or throw an <see cref="EndOfStreamException"/> (if
  /// <paramref name="throwOnEOF"/> is true).
  /// </summary>
  /// <returns>The number of bytes read.</returns>
  public static int Read(this Stream stream, byte[] buf, int index, int count, bool throwOnEOF)
  {
    if(stream == null) throw new ArgumentNullException();
    Utility.ValidateRange(buf, index, count);

    int total=0;
    while(count != 0)
    {
      int read = stream.Read(buf, index, count);
      total += read;

      if(read == 0)
      {
        if(throwOnEOF) throw new EndOfStreamException();
        else break;
      }

      index += read;
      count -= read;
    }

    return total;
  }

  /// <summary>Reads and returns all of the remaining bytes from the stream.</summary>
  public static byte[] ReadAllBytes(this Stream stream)
  {
    if(stream == null) throw new ArgumentNullException();
    byte[] buffer  = new byte[stream.CanSeek ? (int)(stream.Length-stream.Position) : 4096];
    int totalBytes = 0;

    while(true)
    {
      if(totalBytes == buffer.Length)
      {
        byte[] newBuffer = new byte[buffer.Length*2];
        Array.Copy(buffer, newBuffer, totalBytes);
        buffer = newBuffer;
      }

      int read = stream.Read(buffer, totalBytes, buffer.Length - totalBytes);
      if(read == 0 || stream.CanSeek && stream.Position == stream.Length) break;
      totalBytes += read;
    }

    if(totalBytes != buffer.Length)
    {
      byte[] finalBuffer = new byte[totalBytes];
      Array.Copy(buffer, finalBuffer, totalBytes);
      buffer = finalBuffer;
    }

    return buffer;
  }

  /// <summary>Reads the given number of bytes from the stream and converts them into a string using UTF-8 encoding.</summary>
  public static string ReadString(this Stream stream, int length)
  {
    return ReadString(stream, length, System.Text.Encoding.UTF8);
  }

  /// <summary>Reads the given number of bytes from the stream and converts them to a string.</summary>
  public static string ReadString(this Stream stream, int length, System.Text.Encoding encoding)
  {
    return encoding.GetString(Read(stream, length));
  }

  /// <summary>Reads the next byte from a stream.</summary>
  /// <returns>The byte value read from the stream.</returns>
  /// <exception cref="EndOfStreamException">Thrown if the end of the stream was reached before the byte could be read.</exception>
  public static byte ReadByteOrThrow(this Stream stream)
  {
    int i = stream.ReadByte();
    if(i == -1) throw new EndOfStreamException();
    return (byte)i;
  }

  /// <summary>Reads a little-endian short (2 bytes) from a stream.</summary>
  public static short ReadLE2(this Stream stream)
  {
    return (short)(ReadByteOrThrow(stream)|(ReadByteOrThrow(stream)<<8));
  }

  /// <summary>Reads a big-endian short (2 bytes) from a stream.</summary>
  public static short ReadBE2(this Stream stream)
  {
    return (short)((ReadByteOrThrow(stream)<<8)|ReadByteOrThrow(stream));
  }

  /// <summary>Reads a little-endian integer (4 bytes) from a stream.</summary>
  public static int ReadLE4(this Stream stream)
  {
    return (int)(ReadByteOrThrow(stream)|(ReadByteOrThrow(stream)<<8)|
                 (ReadByteOrThrow(stream)<<16)|(ReadByteOrThrow(stream)<<24));
  }

  /// <summary>Reads a big-endian integer (4 bytes) from a stream.</summary>
  public static int ReadBE4(this Stream stream)
  {
    return (int)((ReadByteOrThrow(stream)<<24)|(ReadByteOrThrow(stream)<<16)|
                 (ReadByteOrThrow(stream)<<8)|ReadByteOrThrow(stream));
  }

  /// <summary>Reads a little-endian long (8 bytes) from a stream.</summary>
  public static long ReadLE8(this Stream stream)
  {
    byte[] buf = Read(stream, 8);
    return IOH.ReadLE4U(buf, 0) | ((long)IOH.ReadLE4(buf, 4)<<32);
  }

  /// <summary>Reads a big-endian long (8 bytes) from a stream.</summary>
  public static long ReadBE8(this Stream stream)
  {
    byte[] buf = Read(stream, 8);
    return ((long)IOH.ReadBE4(buf, 0)<<32) | IOH.ReadBE4U(buf, 4);
  }

  /// <summary>Reads a little-endian unsigned short (2 bytes) from a stream.</summary>
  public static ushort ReadLE2U(this Stream stream)
  {
    return (ushort)(ReadByteOrThrow(stream)|(ReadByteOrThrow(stream)<<8));
  }

  /// <summary>Reads a big-endian unsigned short (2 bytes) from a stream.</summary>
  public static ushort ReadBE2U(this Stream stream)
  {
    return (ushort)((ReadByteOrThrow(stream)<<8)|ReadByteOrThrow(stream));
  }

  /// <summary>Reads a little-endian unsigned integer (4 bytes) from a stream.</summary>
  public static uint ReadLE4U(this Stream stream)
  {
    return (uint)(ReadByteOrThrow(stream)|(ReadByteOrThrow(stream)<<8)|
                  (ReadByteOrThrow(stream)<<16)|(ReadByteOrThrow(stream)<<24));
  }

  /// <summary>Reads a big-endian unsigned integer (4 bytes) from a stream.</summary>
  public static uint ReadBE4U(this Stream stream)
  {
    return (uint)((ReadByteOrThrow(stream)<<24)|(ReadByteOrThrow(stream)<<16)|
                  (ReadByteOrThrow(stream)<<8)|ReadByteOrThrow(stream));
  }

  /// <summary>Reads a little-endian unsigned long (8 bytes) from a stream.</summary>
  public static ulong ReadLE8U(this Stream stream)
  {
    byte[] buf = Read(stream, 8);
    return IOH.ReadLE4U(buf, 0) | ((ulong)IOH.ReadLE4U(buf, 4)<<32);
  }

  /// <summary>Reads a big-endian unsigned long (8 bytes) from a stream.</summary>
  public static ulong ReadBE8U(this Stream stream)
  {
    byte[] buf = Read(stream, 8);
    return ((ulong)IOH.ReadBE4U(buf, 0)<<32) | IOH.ReadBE4U(buf, 4);
  }

  /// <summary>Reads an IEEE754 float (4 bytes) from a stream.</summary>
  public unsafe static float ReadFloat(this Stream stream)
  {
    byte* buf = stackalloc byte[4];
    buf[0]=ReadByteOrThrow(stream);
    buf[1]=ReadByteOrThrow(stream);
    buf[2]=ReadByteOrThrow(stream);
    buf[3]=ReadByteOrThrow(stream);
    return *(float*)buf;
  }

  /// <summary>Reads an IEEE754 double (8 bytes) from a stream.</summary>
  public unsafe static double ReadDouble(this Stream stream)
  {
    byte[] buf = Read(stream, sizeof(double));
    fixed(byte* ptr=buf) return *(double*)ptr;
  }

  /// <summary>Skips forward a number of bytes in a stream.</summary>
  /// <remarks>This method works on both seekable and non-seekable streams, but is more efficient with seekable ones.</remarks>
  public static void Skip(this Stream stream, long bytes)
  {
    if(bytes < 0) throw new ArgumentException("cannot be negative", "bytes");

    if(stream.CanSeek)
    {
      stream.Position += bytes;
    }
    else if(bytes <= 4)
    {
      int b = (int)bytes;
      while(b-- > 0) stream.ReadByteOrThrow();
    }
    else
    {
      byte[] buf = new byte[512];
      while(bytes != 0)
      {
        int read = stream.Read(buf, 0, (int)Math.Min(bytes, 512));
        if(read == 0) throw new EndOfStreamException();
        bytes -= read;
      }
    }
  }

  /// <summary>Writes an array of data to a stream.</summary>
  public static int Write(this Stream stream, byte[] data)
  {
    if(data == null) throw new ArgumentNullException();
    stream.Write(data, 0, data.Length);
    return data.Length;
  }

  /// <summary>Encodes a string as UTF-8 and writes it to a stream.</summary>
  /// <returns>The number of bytes written to the stream.</returns>
  public static int WriteString(this Stream stream, string str)
  {
    return WriteString(stream, str, System.Text.Encoding.UTF8);
  }

  /// <summary>Encodes a string using the given encoding and writes it to a stream.</summary>
  /// <returns>The number of bytes written to the stream.</returns>
  public static int WriteString(this Stream stream, string str, System.Text.Encoding encoding)
  {
    return Write(stream, encoding.GetBytes(str));
  }

  /// <summary>Writes a little-endian short (2 bytes) to a stream.</summary>
  public static void WriteLE2(this Stream stream, short val)
  {
    stream.WriteByte((byte)val);
    stream.WriteByte((byte)(val>>8));
  }

  /// <summary>Writes a big-endian short (2 bytes) to a stream.</summary>
  public static void WriteBE2(this Stream stream, short val)
  {
    stream.WriteByte((byte)(val>>8));
    stream.WriteByte((byte)val);
  }

  /// <summary>Writes a little-endian integer (4 bytes) to a stream.</summary>
  public static void WriteLE4(this Stream stream, int val)
  {
    stream.WriteByte((byte)val);
    stream.WriteByte((byte)(val>>8));
    stream.WriteByte((byte)(val>>16));
    stream.WriteByte((byte)(val>>24));
  }

  /// <summary>Writes a big-endian integer (4 bytes) to a stream.</summary>
  public static void WriteBE4(this Stream stream, int val)
  {
    stream.WriteByte((byte)(val>>24));
    stream.WriteByte((byte)(val>>16));
    stream.WriteByte((byte)(val>>8));
    stream.WriteByte((byte)val);
  }

  /// <summary>Writes a little-endian long (8 bytes) to a stream.</summary>
  public static void WriteLE8(this Stream stream, long val)
  {
    WriteLE4(stream, (int)val);
    WriteLE4(stream, (int)(val>>32));
  }

  /// <summary>Writes a big-endian long (8 bytes) to a stream.</summary>
  public static void WriteBE8(this Stream stream, long val)
  {
    WriteBE4(stream, (int)(val>>32));
    WriteBE4(stream, (int)val);
  }

  /// <summary>Writes a little-endian unsigned short (2 bytes) to a stream.</summary>
  public static void WriteLE2U(this Stream stream, ushort val)
  {
    stream.WriteByte((byte)val);
    stream.WriteByte((byte)(val>>8));
  }

  /// <summary>Writes a big-endian unsigned short (2 bytes) to a stream.</summary>
  public static void WriteBE2U(this Stream stream, ushort val)
  {
    stream.WriteByte((byte)(val>>8));
    stream.WriteByte((byte)val);
  }

  /// <summary>Writes a little-endian unsigned integer (4 bytes) to a stream.</summary>
  public static void WriteLE4U(this Stream stream, uint val)
  {
    stream.WriteByte((byte)val);
    stream.WriteByte((byte)(val>>8));
    stream.WriteByte((byte)(val>>16));
    stream.WriteByte((byte)(val>>24));
  }

  /// <summary>Writes a big-endian unsigned integer (4 bytes) to a stream.</summary>
  public static void WriteBE4U(this Stream stream, uint val)
  {
    stream.WriteByte((byte)(val>>24));
    stream.WriteByte((byte)(val>>16));
    stream.WriteByte((byte)(val>>8));
    stream.WriteByte((byte)val);
  }

  /// <summary>Writes a little-endian unsigned long (8 bytes) to a stream.</summary>
  public static void WriteLE8U(this Stream stream, ulong val)
  {
    WriteLE4U(stream, (uint)val);
    WriteLE4U(stream, (uint)(val>>32));
  }

  /// <summary>Writes a big-endian unsigned long (8 bytes) to a stream.</summary>
  public static void WriteBE8U(this Stream stream, ulong val)
  {
    WriteBE4U(stream, (uint)(val>>32));
    WriteBE4U(stream, (uint)val);
  }

  /// <summary>Writes an IEEE754 float (4 bytes) to a stream.</summary>
  public unsafe static void WriteFloat(this Stream stream, float val)
  {
    byte* buf = (byte*)&val;
    stream.WriteByte(buf[0]);
    stream.WriteByte(buf[1]);
    stream.WriteByte(buf[2]);
    stream.WriteByte(buf[3]);
  }

  /// <summary>Writes an IEEE754 double (8 bytes) to a stream.</summary>
  public unsafe static void WriteDouble(this Stream stream, double val)
  {
    byte[] buf = new byte[sizeof(double)];
    fixed(byte* pbuf=buf) *(double*)pbuf = val;
    stream.Write(buf, 0, sizeof(double));
  }
}
#endregion

#region TextReaderExtensions
public static class TextReaderExtensions
{
  /// <summary>Processes each line in the given reader using the given method.</summary>
  public static void ProcessLines(this TextReader reader, Action<string> processor)
  {
    if(reader == null || processor == null) throw new ArgumentNullException();

    while(true)
    {
      string line = reader.ReadLine();
      if(line == null) break;
      processor(line);
    }
  }

  /// <summary>Processes each non-empty and non-whitespace line in the given reader using the given method.</summary>
  public static void ProcessNonEmptyLines(this TextReader reader, Action<string> processor)
  {
    ProcessNonEmptyLines(reader, true, processor);
  }

  /// <summary>Processes each non-empty line in the given reader using the given method. If <paramref name="trimLines"/> is true,
  /// lines that contain only whitespace will not be processed either.
  /// </summary>
  public static void ProcessNonEmptyLines(this TextReader reader, bool trimLines, Action<string> processor)
  {
    ProcessLines(reader, line =>
    {
      if(trimLines) line = line.Trim();
      if(line.Length != 0) processor(line);
    });
  }
}
#endregion

} // namespace AdamMil.IO
