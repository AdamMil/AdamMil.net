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

namespace AdamMil.IO
{

/// <summary>This class provides methods for reading and writing integer and floating point numbers from/to arrays
/// with little or big endianness.
/// </summary>
public unsafe static partial class IOH
{
  #region Reading
  /// <summary>Reads a little-endian short (2 bytes) from a byte array.</summary>
  public static short ReadLE2(byte[] buf, int index) { return (short)(buf[index]|(buf[index+1]<<8)); }

  /// <summary>Reads a little-endian short (2 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static short ReadLE2(byte* buf) { return (short)(*buf|(buf[1]<<8)); }

  /// <summary>Reads a big-endian short (2 bytes) from a byte array.</summary>
  public static short ReadBE2(byte[] buf, int index) { return (short)((buf[index]<<8)|buf[index+1]); }

  /// <summary>Reads a big-endian short (2 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static short ReadBE2(byte* buf) { return (short)((*buf<<8)|buf[1]); }

  /// <summary>Reads a little-endian integer (4 bytes) from a byte array.</summary>
  public static int ReadLE4(byte[] buf, int index)
  {
    return (int)(buf[index]|(buf[index+1]<<8)|(buf[index+2]<<16)|(buf[index+3]<<24));
  }

  /// <summary>Reads a little-endian integer (4 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static int ReadLE4(byte* buf) { return (int)(*buf|(buf[1]<<8)|(buf[2]<<16)|(buf[3]<<24)); }

  /// <summary>Reads a big-endian integer (4 bytes) from a byte array.</summary>
  public static int ReadBE4(byte[] buf, int index)
  {
    return (int)((buf[index]<<24)|(buf[index+1]<<16)|(buf[index+2]<<8)|buf[index+3]);
  }

  /// <summary>Reads a big-endian integer (4 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static int ReadBE4(byte* buf) { return (int)((*buf<<24)|(buf[1]<<16)|(buf[2]<<8)|buf[3]); }

  /// <summary>Reads a little-endian long (8 bytes) from a byte array.</summary>
  public static long ReadLE8(byte[] buf, int index) { return ReadLE4U(buf, index)|((long)ReadLE4(buf, index+4)<<32); }

  /// <summary>Reads a little-endian long (8 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static long ReadLE8(byte* buf) { return ReadLE4U(buf)|((long)ReadLE4(buf+4)<<32); }

  /// <summary>Reads a big-endian long (8 bytes) from a byte array.</summary>
  public static long ReadBE8(byte[] buf, int index) { return ((long)ReadBE4(buf, index)<<32)|(uint)ReadBE4(buf, index+4); }

  /// <summary>Reads a big-endian long (8 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static long ReadBE8(byte* buf) { return ((long)ReadBE4(buf)<<32)|(uint)ReadBE4(buf+4); }

  /// <summary>Reads a little-endian unsigned short (2 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static ushort ReadLE2U(byte[] buf, int index) { return (ushort)(buf[index]|(buf[index+1]<<8)); }

  /// <summary>Reads a little-endian unsigned short (2 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static ushort ReadLE2U(byte* buf) { return (ushort)(*buf|(buf[1]<<8)); }

  /// <summary>Reads a big-endian unsigned short (2 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static ushort ReadBE2U(byte[] buf, int index) { return (ushort)((buf[index]<<8)|buf[index+1]); }

  /// <summary>Reads a big-endian unsigned short (2 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static ushort ReadBE2U(byte* buf) { return (ushort)((*buf<<8)|buf[1]); }

  /// <summary>Reads a little-endian unsigned integer (4 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static uint ReadLE4U(byte[] buf, int index)
  {
    return (uint)(buf[index]|(buf[index+1]<<8)|(buf[index+2]<<16)|(buf[index+3]<<24));
  }

  /// <summary>Reads a little-endian unsigned integer (4 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static uint ReadLE4U(byte* buf) { return (uint)(*buf|(buf[1]<<8)|(buf[2]<<16)|(buf[3]<<24)); }

  /// <summary>Reads a big-endian unsigned integer (4 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static uint ReadBE4U(byte[] buf, int index)
  {
    return (uint)((buf[index]<<24)|(buf[index+1]<<16)|(buf[index+2]<<8)|buf[index+3]);
  }

  /// <summary>Reads a big-endian unsigned integer (4 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static uint ReadBE4U(byte* buf) { return (uint)((*buf<<24)|(buf[1]<<16)|(buf[2]<<8)|buf[3]); }

  /// <summary>Reads a little-endian unsigned long (8 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static ulong ReadLE8U(byte[] buf, int index)
  {
    return ReadLE4U(buf, index)|((ulong)ReadLE4U(buf, index+4)<<32);
  }

  /// <summary>Reads a little-endian unsigned long (8 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static ulong ReadLE8U(byte* buf) { return ReadLE4U(buf)|((ulong)ReadLE4U(buf+4)<<32); }

  /// <summary>Reads a big-endian unsigned long (8 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static ulong ReadBE8U(byte[] buf, int index)
  {
    return ((ulong)ReadBE4U(buf, index)<<32)|ReadBE4U(buf, index+4);
  }

  /// <summary>Reads a big-endian unsigned long (8 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static ulong ReadBE8U(byte* buf) { return ((ulong)ReadBE4U(buf)<<32)|ReadBE4U(buf+4); }

  /// <summary>Reads an IEEE754 float (4 bytes) from a byte array.</summary>
  public unsafe static float ReadFloat(byte[] buf, int index)
  {
    fixed(byte* ptr=buf) return *(float*)(ptr+index);
  }

  /// <summary>Reads an IEEE754 float (4 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public unsafe static float ReadFloat(byte* buf) { return *(float*)buf; }

  /// <summary>Reads an IEEE754 double (8 bytes) from a byte array.</summary>
  public unsafe static double ReadDouble(byte[] buf, int index)
  {
    fixed(byte* ptr=buf) return *(double*)(ptr+index);
  }

  /// <summary>Reads an IEEE754 double (8 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public unsafe static double ReadDouble(byte* buf) { return *(double*)buf; }
  #endregion

  #region Writing
  /// <summary>Writes a little-endian short (2 bytes) to a byte array.</summary>
  public static void WriteLE2(byte[] buf, int index, short val)
  {
    buf[index]   = (byte)val;
    buf[index+1] = (byte)(val>>8);
  }

  /// <summary>Writes a little-endian short (2 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteLE2(byte* buf, short val)
  {
    *buf   = (byte)val;
    buf[1] = (byte)(val>>8);
  }

  /// <summary>Writes a big-endian short (2 bytes) to a byte array.</summary>
  public static void WriteBE2(byte[] buf, int index, short val)
  {
    buf[index]   = (byte)(val>>8);
    buf[index+1] = (byte)val;
  }

  /// <summary>Writes a big-endian short (2 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteBE2(byte* buf, short val)
  {
    *buf   = (byte)(val>>8);
    buf[1] = (byte)val;
  }

  /// <summary>Writes a little-endian integer (4 bytes) to a byte array.</summary>
  public static void WriteLE4(byte[] buf, int index, int val)
  {
    buf[index]   = (byte)val;
    buf[index+1] = (byte)(val>>8);
    buf[index+2] = (byte)(val>>16);
    buf[index+3] = (byte)(val>>24);
  }

  /// <summary>Writes a little-endian integer (4 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteLE4(byte* buf, int val)
  {
    *buf   = (byte)val;
    buf[1] = (byte)(val>>8);
    buf[2] = (byte)(val>>16);
    buf[3] = (byte)(val>>24);
  }

  /// <summary>Writes a big-endian integer (4 bytes) to a byte array.</summary>
  public static void WriteBE4(byte[] buf, int index, int val)
  {
    buf[index]   = (byte)(val>>24);
    buf[index+1] = (byte)(val>>16);
    buf[index+2] = (byte)(val>>8);
    buf[index+3] = (byte)val;
  }

  /// <summary>Writes a big-endian integer (4 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteBE4(byte* buf, int val)
  {
    *buf   = (byte)(val>>24);
    buf[1] = (byte)(val>>16);
    buf[2] = (byte)(val>>8);
    buf[3] = (byte)val;
  }

  /// <summary>Writes a little-endian long (8 bytes) to a byte array.</summary>
  public static void WriteLE8(byte[] buf, int index, long val)
  {
    WriteLE4(buf, index, (int)val);
    WriteLE4(buf, index+4, (int)(val>>32));
  }

  /// <summary>Writes a little-endian long (8 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteLE8(byte* buf, long val)
  {
    WriteLE4(buf, (int)val);
    WriteLE4(buf+4, (int)(val>>32));
  }

  /// <summary>Writes a big-endian long (8 bytes) to a byte array.</summary>
  public static void WriteBE8(byte[] buf, int index, long val)
  {
    WriteBE4(buf, index, (int)(val>>32));
    WriteBE4(buf, index+4, (int)val);
  }

  /// <summary>Writes a big-endian long (8 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteBE8(byte* buf, long val)
  {
    WriteBE4(buf, (int)(val>>32));
    WriteBE4(buf+4, (int)val);
  }

  /// <summary>Writes a little-endian unsigned short (2 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteLE2U(byte[] buf, int index, ushort val)
  {
    buf[index]   = (byte)val;
    buf[index+1] = (byte)(val>>8);
  }

  /// <summary>Writes a little-endian unsigned short (2 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteLE2U(byte* buf, ushort val)
  {
    *buf   = (byte)val;
    buf[1] = (byte)(val>>8);
  }

  /// <summary>Writes a big-endian unsigned short (2 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteBE2U(byte[] buf, int index, ushort val)
  {
    buf[index]   = (byte)(val>>8);
    buf[index+1] = (byte)val;
  }

  /// <summary>Writes a big-endian unsigned short (2 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteBE2U(byte* buf, ushort val)
  {
    *buf   = (byte)(val>>8);
    buf[1] = (byte)val;
  }

  /// <summary>Writes a little-endian unsigned integer (4 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteLE4U(byte[] buf, int index, uint val)
  {
    buf[index]   = (byte)val;
    buf[index+1] = (byte)(val>>8);
    buf[index+2] = (byte)(val>>16);
    buf[index+3] = (byte)(val>>24);
  }

  /// <summary>Writes a little-endian unsigned integer (4 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteLE4U(byte* buf, uint val)
  {
    *buf   = (byte)val;
    buf[1] = (byte)(val>>8);
    buf[2] = (byte)(val>>16);
    buf[3] = (byte)(val>>24);
  }

  /// <summary>Writes a big-endian unsigned integer (4 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteBE4U(byte[] buf, int index, uint val)
  {
    buf[index]   = (byte)(val>>24);
    buf[index+1] = (byte)(val>>16);
    buf[index+2] = (byte)(val>>8);
    buf[index+3] = (byte)val;
  }

  /// <summary>Writes a big-endian unsigned integer (4 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteBE4U(byte* buf, uint val)
  {
    *buf   = (byte)(val>>24);
    buf[1] = (byte)(val>>16);
    buf[2] = (byte)(val>>8);
    buf[3] = (byte)val;
  }

  /// <summary>Writes a little-endian unsigned long (8 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteLE8U(byte[] buf, int index, ulong val)
  {
    WriteLE4U(buf, index, (uint)val);
    WriteLE4U(buf, index+4, (uint)(val>>32));
  }

  /// <summary>Writes a little-endian unsigned long (8 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteLE8U(byte* buf, ulong val)
  {
    WriteLE4U(buf, (uint)val);
    WriteLE4U(buf+4, (uint)(val>>32));
  }

  /// <summary>Writes a big-endian unsigned long (8 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteBE8U(byte[] buf, int index, ulong val)
  {
    WriteBE4U(buf, index, (uint)(val>>32));
    WriteBE4U(buf, index+4, (uint)val);
  }

  /// <summary>Writes a big-endian unsigned long (8 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public static void WriteBE8U(byte* buf, ulong val)
  {
    WriteBE4U(buf, (uint)(val>>32));
    WriteBE4U(buf+4, (uint)val);
  }

  /// <summary>Writes an IEEE754 float (4 bytes) to a byte array.</summary>
  public unsafe static void WriteFloat(byte[] buf, int index, float val)
  {
    fixed(byte* ptr=buf) *(float*)(ptr+index) = val;
  }

  /// <summary>Writes an IEEE754 float (4 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public unsafe static void WriteFloat(byte* buf, float val)
  {
    *(float*)buf = val;
  }

  /// <summary>Writes an IEEE754 double (8 bytes) to a byte array.</summary>
  public unsafe static void WriteDouble(byte[] buf, int index, double val)
  {
    fixed(byte* ptr=buf) *(double*)(ptr+index) = val;
  }

  /// <summary>Writes an IEEE754 double (8 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public unsafe static void WriteDouble(byte* buf, double val)
  {
    *(double*)buf = val;
  }
  #endregion
}

} // namespace AdamMil.IO