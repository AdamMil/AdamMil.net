/*
AdamMil.IO is a library that provides high performance and high level IO
tools for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2008 Adam Milazzo

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

namespace AdamMil.IO
{

/// <summary>This class provides methods for reading and writing numeric and string values from/to streams
/// with little or big endianness.
/// </summary>
public unsafe static partial class IOH
{
  #region CopyStream
  /// <summary>Copies a source stream into a destination stream and returns the number of bytes copied. The streams
  /// are not rewound or disposed.
  /// </summary>
  public static int CopyStream(Stream source, Stream dest) { return CopyStream(source, dest, false, false, 0); }

  /// <summary>Copies a source stream into a destination stream and returns the number of bytes copied. The streams
  /// are not rewound.
  /// </summary>
  public static int CopyStream(Stream source, Stream dest, bool disposeStreams)
  {
    return CopyStream(source, dest, disposeStreams, false, 0);
  }

  /// <summary>Copies a source stream into a destination stream and returns the number of bytes copied.</summary>
  public static int CopyStream(Stream source, Stream dest, bool disposeStreams, bool rewindSource)
  {
    return CopyStream(source, dest, disposeStreams, rewindSource, 0);
  }

  /// <summary>Copies a source stream into a destination stream and returns the number of bytes copied.</summary>
  /// <param name="source">The stream from which the source data will be copied.</param>
  /// <param name="dest">The stream into which the source data will be written.</param>
  /// <param name="disposeStreams">If true, the source and destination streams will be disposed after the copy is made.</param>
  /// <param name="rewindSource">If true, the source stream's <see cref="Stream.Position"/> property will be set
  /// to 0 first to ensure that the entire source stream is copied.
  /// </param>
  /// <param name="bufferSize">The size of the buffer to use. Passing zero will use a default value.</param>
  public static int CopyStream(Stream source, Stream dest, bool disposeStreams, bool rewindSource, int bufferSize)
  {
    if(source == null || dest == null) throw new ArgumentNullException();
    if(bufferSize < 0) throw new ArgumentOutOfRangeException("bufferSize");
    if(bufferSize == 0) bufferSize = 4096;

    try
    {
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
    finally
    {
      if(disposeStreams)
      {
        source.Dispose();
        dest.Dispose();
      }
    }
  }
  #endregion

  #region Reading
  /// <summary>Reads the given number of bytes from a stream.</summary>
  /// <returns>A byte array containing <paramref name="length"/> bytes of data.</returns>
  public static byte[] Read(Stream stream, int length)
  {
    if(length < 0) throw new ArgumentOutOfRangeException();
    byte[] buf = new byte[length];
    Read(stream, buf, 0, length, true);
    return buf;
  }

  /// <summary>Reads the given number of bytes from a stream into a buffer.</summary>
  /// <returns>The number of bytes read. This will always be equal to <paramref name="length"/>.</returns>
  public static int Read(Stream stream, byte[] buf, int index, int length)
  {
    return Read(stream, buf, index, length, true);
  }

  /// <summary>Tries to read the given number of bytes from a stream into a buffer.</summary>
  /// <returns>The number of bytes read.</returns>
  public static int Read(Stream stream, byte[] buf, int index, int length, bool throwOnEOF)
  {
    if(stream == null || buf == null) throw new ArgumentNullException();
    if(index < 0 || length < 0 || index+length > buf.Length) throw new ArgumentOutOfRangeException();

    int read, total=0;
    while(length != 0)
    {
      read = stream.Read(buf, index, length);
      total += read;

      if(read == 0)
      {
        if(throwOnEOF) throw new EndOfStreamException();
        else break;
      }

      index  += read;
      length -= read;
    }

    return total;
  }

  /// <summary>Reads the given number of bytes from the stream and converts them into a string using ASCII encoding.</summary>
  public static string ReadAscii(Stream stream, int length)
  {
    return ReadString(stream, length, System.Text.Encoding.ASCII);
  }

  /// <summary>Reads the given number of bytes from the stream and converts them into a string using UTF-8 encoding.</summary>
  public static string ReadString(Stream stream, int length)
  {
    return ReadString(stream, length, System.Text.Encoding.UTF8);
  }

  /// <summary>Reads the given number of bytes from the stream and converts them to a string.</summary>
  public static string ReadString(Stream stream, int length, System.Text.Encoding encoding)
  {
    return encoding.GetString(Read(stream, length));
  }

  /// <summary>Reads the next byte from a stream.</summary>
  /// <returns>The byte value read from the stream.</returns>
  /// <exception cref="EndOfStreamException">Thrown if the end of the stream was reached before the byte could be read.
  /// </exception>
  public static byte Read1(Stream stream)
  {
    int i = stream.ReadByte();
    if(i == -1) throw new EndOfStreamException();
    return (byte)i;
  }

  /// <summary>Reads a little-endian short (2 bytes) from a stream.</summary>
  public static short ReadLE2(Stream stream) { return (short)(Read1(stream)|(Read1(stream)<<8)); }

  /// <summary>Reads a big-endian short (2 bytes) from a stream.</summary>
  public static short ReadBE2(Stream stream) { return (short)((Read1(stream)<<8)|Read1(stream)); }

  /// <summary>Reads a little-endian integer (4 bytes) from a stream.</summary>
  public static int ReadLE4(Stream stream)
  {
    return (int)(Read1(stream)|(Read1(stream)<<8)|(Read1(stream)<<16)|(Read1(stream)<<24));
  }

  /// <summary>Reads a big-endian integer (4 bytes) from a stream.</summary>
  public static int ReadBE4(Stream stream)
  {
    return (int)((Read1(stream)<<24)|(Read1(stream)<<16)|(Read1(stream)<<8)|Read1(stream));
  }

  /// <summary>Reads a little-endian long (8 bytes) from a stream.</summary>
  public static long ReadLE8(Stream stream)
  {
    byte[] buf = Read(stream, 8);
    return ReadLE4U(buf, 0) | ((long)ReadLE4(buf, 4)<<32);
  }

  /// <summary>Reads a big-endian long (8 bytes) from a stream.</summary>
  public static long ReadBE8(Stream stream)
  {
    byte[] buf = Read(stream, 8);
    return ((long)ReadBE4(buf, 0)<<32)|ReadBE4U(buf, 4);
  }

  /// <summary>Reads a little-endian unsigned short (2 bytes) from a stream.</summary>
  public static ushort ReadLE2U(Stream stream) { return (ushort)(Read1(stream)|(Read1(stream)<<8)); }

  /// <summary>Reads a big-endian unsigned short (2 bytes) from a stream.</summary>
  public static ushort ReadBE2U(Stream stream) { return (ushort)((Read1(stream)<<8)|Read1(stream)); }

  /// <summary>Reads a little-endian unsigned integer (4 bytes) from a stream.</summary>
  public static uint ReadLE4U(Stream stream)
  {
    return (uint)(Read1(stream)|(Read1(stream)<<8)|(Read1(stream)<<16)|(Read1(stream)<<24));
  }

  /// <summary>Reads a big-endian unsigned integer (4 bytes) from a stream.</summary>
  public static uint ReadBE4U(Stream stream)
  {
    return (uint)((Read1(stream)<<24)|(Read1(stream)<<16)|(Read1(stream)<<8)|Read1(stream));
  }

  /// <summary>Reads a little-endian unsigned long (8 bytes) from a stream.</summary>
  public static ulong ReadLE8U(Stream stream)
  {
    byte[] buf = Read(stream, 8);
    return ReadLE4U(buf, 0)|((ulong)ReadLE4U(buf, 4)<<32);
  }

  /// <summary>Reads a big-endian unsigned long (8 bytes) from a stream.</summary>
  public static ulong ReadBE8U(Stream stream)
  {
    byte[] buf = Read(stream, 8);
    return ((ulong)ReadBE4U(buf, 0)<<32)|ReadBE4U(buf, 4);
  }

  /// <summary>Reads an IEEE754 float (4 bytes) from a stream.</summary>
  public unsafe static float ReadFloat(Stream stream)
  {
    byte* buf = stackalloc byte[4];
    buf[0]=Read1(stream); buf[1]=Read1(stream); buf[2]=Read1(stream); buf[3]=Read1(stream);
    return *(float*)buf;
  }

  /// <summary>Reads an IEEE754 double (8 bytes) from a stream.</summary>
  public unsafe static double ReadDouble(Stream stream)
  {
    byte[] buf = Read(stream, sizeof(double));
    fixed(byte* ptr=buf) return *(double*)ptr;
  }
  #endregion

  #region Skip
  /// <summary>Skips forward a number of bytes in a stream.</summary>
  /// <remarks>This method works on both seekable and non-seekable streams, but is more efficient with seekable ones.</remarks>
  public static void Skip(Stream stream, long bytes)
  {
    if(bytes < 0) throw new ArgumentException("cannot be negative", "bytes");

    if(stream.CanSeek) 
    {
      stream.Position += bytes;
    }
    else if(bytes <= 4)
    { 
      int b = (int)bytes; 
      while(b-- > 0) Read1(stream); 
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
  #endregion

  #region Writing
  /// <summary>Writes an array of data to a stream.</summary>
  public static int Write(Stream stream, byte[] data)
  {
    if(data == null) throw new ArgumentNullException();
    stream.Write(data, 0, data.Length);
    return data.Length;
  }

  /// <summary>Encodes a string as ASCII and writes it to a stream.</summary>
  /// <returns>The number of bytes written to the stream.</returns>
  public static int WriteAscii(Stream stream, string str)
  {
    return WriteString(stream, str, System.Text.Encoding.ASCII);
  }

  /// <summary>Encodes a string as UTF-8 and writes it to a stream.</summary>
  /// <returns>The number of bytes written to the stream.</returns>
  public static int WriteString(Stream stream, string str)
  {
    return WriteString(stream, str, System.Text.Encoding.UTF8);
  }

  /// <summary>Encodes a string using the given encoding and writes it to a stream.</summary>
  /// <returns>The number of bytes written to the stream.</returns>
  public static int WriteString(Stream stream, string str, System.Text.Encoding encoding)
  {
    return Write(stream, encoding.GetBytes(str));
  }

  /// <summary>Writes a little-endian short (2 bytes) to a stream.</summary>
  public static void WriteLE2(Stream stream, short val)
  {
    stream.WriteByte((byte)val);
    stream.WriteByte((byte)(val>>8));
  }

  /// <summary>Writes a big-endian short (2 bytes) to a stream.</summary>
  public static void WriteBE2(Stream stream, short val)
  {
    stream.WriteByte((byte)(val>>8));
    stream.WriteByte((byte)val);
  }

  /// <summary>Writes a little-endian integer (4 bytes) to a stream.</summary>
  public static void WriteLE4(Stream stream, int val)
  {
    stream.WriteByte((byte)val);
    stream.WriteByte((byte)(val>>8));
    stream.WriteByte((byte)(val>>16));
    stream.WriteByte((byte)(val>>24));
  }

  /// <summary>Writes a big-endian integer (4 bytes) to a stream.</summary>
  public static void WriteBE4(Stream stream, int val)
  {
    stream.WriteByte((byte)(val>>24));
    stream.WriteByte((byte)(val>>16));
    stream.WriteByte((byte)(val>>8));
    stream.WriteByte((byte)val);
  }

  /// <summary>Writes a little-endian long (8 bytes) to a stream.</summary>
  public static void WriteLE8(Stream stream, long val)
  {
    WriteLE4(stream, (int)val);
    WriteLE4(stream, (int)(val>>32));
  }

  /// <summary>Writes a big-endian long (8 bytes) to a stream.</summary>
  public static void WriteBE8(Stream stream, long val)
  {
    WriteBE4(stream, (int)(val>>32));
    WriteBE4(stream, (int)val);
  }

  /// <summary>Writes a little-endian unsigned short (2 bytes) to a stream.</summary>
  public static void WriteLE2U(Stream stream, ushort val)
  {
    stream.WriteByte((byte)val);
    stream.WriteByte((byte)(val>>8));
  }

  /// <summary>Writes a big-endian unsigned short (2 bytes) to a stream.</summary>
  public static void WriteBE2U(Stream stream, ushort val)
  {
    stream.WriteByte((byte)(val>>8));
    stream.WriteByte((byte)val);
  }

  /// <summary>Writes a little-endian unsigned integer (4 bytes) to a stream.</summary>
  public static void WriteLE4U(Stream stream, uint val)
  {
    stream.WriteByte((byte)val);
    stream.WriteByte((byte)(val>>8));
    stream.WriteByte((byte)(val>>16));
    stream.WriteByte((byte)(val>>24));
  }

  /// <summary>Writes a big-endian unsigned integer (4 bytes) to a stream.</summary>
  public static void WriteBE4U(Stream stream, uint val)
  {
    stream.WriteByte((byte)(val>>24));
    stream.WriteByte((byte)(val>>16));
    stream.WriteByte((byte)(val>>8));
    stream.WriteByte((byte)val);
  }

  /// <summary>Writes a little-endian unsigned long (8 bytes) to a stream.</summary>
  public static void WriteLE8U(Stream stream, ulong val)
  {
    WriteLE4U(stream, (uint)val);
    WriteLE4U(stream, (uint)(val>>32));
  }

  /// <summary>Writes a big-endian unsigned long (8 bytes) to a stream.</summary>
  public static void WriteBE8U(Stream stream, ulong val)
  {
    WriteBE4U(stream, (uint)(val>>32));
    WriteBE4U(stream, (uint)val);
  }

  /// <summary>Writes an IEEE754 float (4 bytes) to a stream.</summary>
  public unsafe static void WriteFloat(Stream stream, float val)
  {
    byte* buf = (byte*)&val;
    stream.WriteByte(buf[0]);
    stream.WriteByte(buf[1]);
    stream.WriteByte(buf[2]);
    stream.WriteByte(buf[3]);
  }

  /// <summary>Writes an IEEE754 double (8 bytes) to a stream.</summary>
  public unsafe static void WriteDouble(Stream stream, double val)
  {
    byte[] buf = new byte[sizeof(double)];
    fixed(byte* pbuf=buf) *(double*)pbuf = val;
    stream.Write(buf, 0, sizeof(double));
  }
  #endregion
}

} // namespace AdamMil.IO