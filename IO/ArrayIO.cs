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
using AdamMil.Utilities;

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
  public static short ReadLE2(byte* buf)
  {
    if(BitConverter.IsLittleEndian) return *(short*)buf;
    else return (short)(*buf|(buf[1]<<8));
  }

  /// <summary>Reads a big-endian short (2 bytes) from a byte array.</summary>
  public static short ReadBE2(byte[] buf, int index) { return (short)((buf[index]<<8)|buf[index+1]); }

  /// <summary>Reads a big-endian short (2 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static short ReadBE2(byte* buf)
  {
    if(BitConverter.IsLittleEndian) return (short)((*buf<<8)|buf[1]);
    else return *(short*)buf;
  }

  /// <summary>Reads a little-endian integer (4 bytes) from a byte array.</summary>
  public static int ReadLE4(byte[] buf, int index)
  {
    return (int)(buf[index]|(buf[index+1]<<8)|(buf[index+2]<<16)|(buf[index+3]<<24));
  }

  /// <summary>Reads a little-endian integer (4 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static int ReadLE4(byte* buf)
  {
    if(BitConverter.IsLittleEndian) return *(int*)buf;
    else return (int)BinaryUtility.ByteSwap(*(uint*)buf);
  }

  /// <summary>Reads a big-endian integer (4 bytes) from a byte array.</summary>
  public static int ReadBE4(byte[] buf, int index)
  {
    return (int)((buf[index]<<24)|(buf[index+1]<<16)|(buf[index+2]<<8)|buf[index+3]);
  }

  /// <summary>Reads a big-endian integer (4 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static int ReadBE4(byte* buf)
  {
    if(BitConverter.IsLittleEndian) return (int)BinaryUtility.ByteSwap(*(uint*)buf);
    else return *(int*)buf;
  }

  /// <summary>Reads a little-endian long (8 bytes) from a byte array.</summary>
  public static long ReadLE8(byte[] buf, int index)
  {
    return ReadLE4U(buf, index) | ((long)ReadLE4(buf, index+4)<<32);
  }

  /// <summary>Reads a little-endian long (8 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static long ReadLE8(byte* buf)
  {
    if(BitConverter.IsLittleEndian) return *(long*)buf;
    else return (long)(BinaryUtility.ByteSwap(*(uint*)buf) | ((ulong)BinaryUtility.ByteSwap(*(uint*)(buf+4)) << 32));
  }

  /// <summary>Reads a big-endian long (8 bytes) from a byte array.</summary>
  public static long ReadBE8(byte[] buf, int index)
  {
    return ((long)ReadBE4(buf, index)<<32)|ReadBE4U(buf, index+4);
  }

  /// <summary>Reads a big-endian long (8 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static long ReadBE8(byte* buf)
  {
    if(BitConverter.IsLittleEndian) return (long)(((ulong)BinaryUtility.ByteSwap(*(uint*)buf) << 32) | BinaryUtility.ByteSwap(*(uint*)(buf+4)));
    else return *(long*)buf;
  }

  /// <summary>Reads a little-endian unsigned short (2 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static ushort ReadLE2U(byte[] buf, int index) { return (ushort)(buf[index]|(buf[index+1]<<8)); }

  /// <summary>Reads a little-endian unsigned short (2 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static ushort ReadLE2U(byte* buf)
  {
    if(BitConverter.IsLittleEndian) return *(ushort*)buf;
    else return BinaryUtility.ByteSwap(*(ushort*)buf);
  }

  /// <summary>Reads a big-endian unsigned short (2 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static ushort ReadBE2U(byte[] buf, int index) { return (ushort)((buf[index]<<8)|buf[index+1]); }

  /// <summary>Reads a big-endian unsigned short (2 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static ushort ReadBE2U(byte* buf)
  {
    if(BitConverter.IsLittleEndian) return BinaryUtility.ByteSwap(*(ushort*)buf);
    else return *(ushort*)buf;
  }

  /// <summary>Reads a little-endian unsigned integer (4 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static uint ReadLE4U(byte[] buf, int index)
  {
    return (uint)(buf[index]|(buf[index+1]<<8)|(buf[index+2]<<16)|(buf[index+3]<<24));
  }

  /// <summary>Reads a little-endian unsigned integer (4 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static uint ReadLE4U(byte* buf)
  {
    if(BitConverter.IsLittleEndian) return *(uint*)buf;
    else return BinaryUtility.ByteSwap(*(uint*)buf);
  }

  /// <summary>Reads a big-endian unsigned integer (4 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static uint ReadBE4U(byte[] buf, int index)
  {
    return (uint)((buf[index]<<24)|(buf[index+1]<<16)|(buf[index+2]<<8)|buf[index+3]);
  }

  /// <summary>Reads a big-endian unsigned integer (4 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static uint ReadBE4U(byte* buf)
  {
    if(BitConverter.IsLittleEndian) return BinaryUtility.ByteSwap(*(uint*)buf);
    else return *(uint*)buf;
  }

  /// <summary>Reads a little-endian unsigned long (8 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static unsafe ulong ReadLE8U(byte[] buf, int index)
  {
    return ReadLE4U(buf, index)|((ulong)ReadLE4U(buf, index+4)<<32);
  }

  /// <summary>Reads a little-endian unsigned long (8 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static ulong ReadLE8U(byte* buf)
  {
    if(BitConverter.IsLittleEndian) return *(ulong*)buf;
    else return BinaryUtility.ByteSwap(*(uint*)buf) | ((ulong)BinaryUtility.ByteSwap(*(uint*)(buf+4)) << 32);
  }

  /// <summary>Reads a big-endian unsigned long (8 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static ulong ReadBE8U(byte[] buf, int index)
  {
    return ((ulong)ReadBE4U(buf, index)<<32)|ReadBE4U(buf, index+4);
  }

  /// <summary>Reads a big-endian unsigned long (8 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public static ulong ReadBE8U(byte* buf)
  {
    if(BitConverter.IsLittleEndian) return ((ulong)BinaryUtility.ByteSwap(*(uint*)buf) << 32) | BinaryUtility.ByteSwap(*(uint*)(buf+4));
    else return *(ulong*)buf;
  }

  /// <summary>Reads a little-endian IEEE754 float (4 bytes) from a byte array.</summary>
  public unsafe static float ReadLESingle(byte[] buf, int index)
  {
    int v = buf[index]|(buf[index+1]<<8)|(buf[index+2]<<16)|(buf[index+3]<<24);
    return *(float*)&v;
  }

  /// <summary>Reads a little-endian IEEE754 float (4 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public unsafe static float ReadLESingle(byte* buf)
  {
    if(BitConverter.IsLittleEndian)
    {
      return *(float*)buf;
    }
    else
    {
      uint v = BinaryUtility.ByteSwap(*(uint*)buf);
      return *(float*)&v;
    }
  }

  /// <summary>Reads a little-endian IEEE754 double (8 bytes) from a byte array.</summary>
  public unsafe static double ReadLEDouble(byte[] buf, int index)
  {
    ulong v = ReadLE8U(buf, index);
    return *(double*)&v;
  }

  /// <summary>Reads a little-endian IEEE754 double (8 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public unsafe static double ReadLEDouble(byte* buf)
  {
    if(BitConverter.IsLittleEndian)
    {
      return *(double*)buf;
    }
    else
    {
      ulong v = ReadLE8U(buf);
      return *(double*)&v;
    }
  }

  /// <summary>Reads a big-endian IEEE754 float (4 bytes) from a byte array.</summary>
  public unsafe static float ReadBESingle(byte[] buf, int index)
  {
    int v = (buf[index]<<24)|(buf[index+1]<<16)|(buf[index+2]<<8)|buf[index+3];
    return *(float*)&v;
  }

  /// <summary>Reads a big-endian IEEE754 float (4 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public unsafe static float ReadBESingle(byte* buf)
  {
    if(BitConverter.IsLittleEndian)
    {
      uint v = BinaryUtility.ByteSwap(*(uint*)buf);
      return *(float*)&v;
    }
    else
    {
      return *(float*)buf;
    }
  }

  /// <summary>Reads a big-endian IEEE754 double (8 bytes) from a byte array.</summary>
  public unsafe static double ReadBEDouble(byte[] buf, int index)
  {
    ulong v = ReadBE8U(buf, index);
    return *(double*)&v;
  }

  /// <summary>Reads a big-endian IEEE754 double (8 bytes) from a byte array.</summary>
  [CLSCompliant(false)]
  public unsafe static double ReadBEDouble(byte* buf)
  {
    if(BitConverter.IsLittleEndian)
    {
      ulong v = ReadBE8U(buf);
      return *(double*)&v;
    }
    else
    {
      return *(double*)buf;
    }
  }
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
    if(BitConverter.IsLittleEndian) *(short*)buf = val;
    else *(ushort*)buf = BinaryUtility.ByteSwap((ushort)val);
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
    if(BitConverter.IsLittleEndian) *(ushort*)buf = BinaryUtility.ByteSwap((ushort)val);
    else *(short*)buf = val;
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
    if(BitConverter.IsLittleEndian) *(int*)buf = val;
    else *(uint*)buf = BinaryUtility.ByteSwap((uint)val);
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
    if(BitConverter.IsLittleEndian) *(uint*)buf = BinaryUtility.ByteSwap((uint)val);
    else *(int*)buf = val;
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
    if(BitConverter.IsLittleEndian)
    {
      *(long*)buf = val;
    }
    else
    {
      *(uint*)buf     = BinaryUtility.ByteSwap(*((uint*)&val+1));
      *(uint*)(buf+4) = BinaryUtility.ByteSwap(*(uint*)&val);
    }
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
    if(BitConverter.IsLittleEndian)
    {
      *(uint*)buf     = BinaryUtility.ByteSwap(*((uint*)&val+1));
      *(uint*)(buf+4) = BinaryUtility.ByteSwap(*(uint*)&val);
    }
    else
    {
      *(long*)buf = val;
    }
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
    if(BitConverter.IsLittleEndian) *(ushort*)buf = val;
    else *(ushort*)buf = BinaryUtility.ByteSwap(val);
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
    if(BitConverter.IsLittleEndian) *(ushort*)buf = BinaryUtility.ByteSwap(val);
    else *(ushort*)buf = val;
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
    if(BitConverter.IsLittleEndian) *(uint*)buf = val;
    else *(uint*)buf = BinaryUtility.ByteSwap(val);
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
    if(BitConverter.IsLittleEndian) *(uint*)buf = BinaryUtility.ByteSwap(val);
    else *(uint*)buf = val;
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
    if(BitConverter.IsLittleEndian)
    {
      *(ulong*)buf = val;
    }
    else
    {
      *(uint*)buf     = BinaryUtility.ByteSwap(*((uint*)&val+1));
      *(uint*)(buf+4) = BinaryUtility.ByteSwap(*(uint*)&val);
    }
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
    if(BitConverter.IsLittleEndian)
    {
      *(uint*)buf     = BinaryUtility.ByteSwap(*((uint*)&val+1));
      *(uint*)(buf+4) = BinaryUtility.ByteSwap(*(uint*)&val);
    }
    else
    {
      *(ulong*)buf = val;
    }
  }

  /// <summary>Writes a little-endian IEEE754 float (4 bytes) to a byte array.</summary>
  public unsafe static void WriteLESingle(byte[] buf, int index, float val)
  {
    uint v = *(uint*)&val;
    buf[index]   = (byte)v;
    buf[index+1] = (byte)(v>>8);
    buf[index+2] = (byte)(v>>16);
    buf[index+3] = (byte)(v>>24);
  }

  /// <summary>Writes a little-endian IEEE754 float (4 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public unsafe static void WriteLESingle(byte* buf, float val)
  {
    if(BitConverter.IsLittleEndian) *(float*)buf = val;
    else *(uint*)buf = BinaryUtility.ByteSwap(*(uint*)&val);
  }

  /// <summary>Writes a big-endian IEEE754 float (4 bytes) to a byte array.</summary>
  public unsafe static void WriteBESingle(byte[] buf, int index, float val)
  {
    uint v = *(uint*)&val;
    buf[index]   = (byte)(v>>24);
    buf[index+1] = (byte)(v>>16);
    buf[index+2] = (byte)(v>>8);
    buf[index+3] = (byte)v;
  }

  /// <summary>Writes a big-endian IEEE754 float (4 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public unsafe static void WriteBESingle(byte* buf, float val)
  {
    if(BitConverter.IsLittleEndian) *(uint*)buf = BinaryUtility.ByteSwap(*(uint*)&val);
    else *(float*)buf = val;
  }

  /// <summary>Writes a little-endian IEEE754 double (8 bytes) to a byte array.</summary>
  public unsafe static void WriteLEDouble(byte[] buf, int index, double val)
  {
    ulong v = *(ulong*)&val;
    WriteLE4U(buf, index, (uint)v);
    WriteLE4U(buf, index+4, (uint)(v>>32));
  }

  /// <summary>Writes a little-endian IEEE754 double (8 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public unsafe static void WriteLEDouble(byte* buf, double val)
  {
    if(BitConverter.IsLittleEndian)
    {
      *(double*)buf = val;
    }
    else
    {
      *(uint*)buf     = BinaryUtility.ByteSwap(*((uint*)&val+1));
      *(uint*)(buf+4) = BinaryUtility.ByteSwap(*(uint*)&val);
    }
  }

  /// <summary>Writes a big-endian IEEE754 double (8 bytes) to a byte array.</summary>
  public unsafe static void WriteBEDouble(byte[] buf, int index, double val)
  {
    ulong v = *(ulong*)&val;
    WriteBE4U(buf, index, (uint)(v>>32));
    WriteBE4U(buf, index+4, (uint)v);
  }

  /// <summary>Writes a big-endian IEEE754 double (8 bytes) to a byte array.</summary>
  [CLSCompliant(false)]
  public unsafe static void WriteBEDouble(byte* buf, double val)
  {
    if(BitConverter.IsLittleEndian)
    {
      *(uint*)buf     = BinaryUtility.ByteSwap(*((uint*)&val+1));
      *(uint*)(buf+4) = BinaryUtility.ByteSwap(*(uint*)&val);
    }
    else
    {
      *(double*)buf = val;
    }
  }
  #endregion
}

} // namespace AdamMil.IO
