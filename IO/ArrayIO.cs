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
  public static short ReadLE2(byte* buf, int index) { return (short)(buf[index]|(buf[index+1]<<8)); }

  /// <summary>Reads a big-endian short (2 bytes) from a byte array.</summary>
  public static short ReadBE2(byte[] buf, int index) { return (short)((buf[index]<<8)|buf[index+1]); }

  /// <summary>Reads a big-endian short (2 bytes) from a byte array.</summary>
  public static short ReadBE2(byte* buf, int index) { return (short)((buf[index]<<8)|buf[index+1]); }

  /// <summary>Reads a little-endian integer (4 bytes) from a byte array.</summary>
  public static int ReadLE4(byte[] buf, int index)
  {
    return (int)(buf[index]|(buf[index+1]<<8)|(buf[index+2]<<16)|(buf[index+3]<<24));
  }

  /// <summary>Reads a little-endian integer (4 bytes) from a byte array.</summary>
  public static int ReadLE4(byte* buf, int index)
  {
    return (int)(buf[index]|(buf[index+1]<<8)|(buf[index+2]<<16)|(buf[index+3]<<24));
  }

  /// <summary>Reads a big-endian integer (4 bytes) from a byte array.</summary>
  public static int ReadBE4(byte[] buf, int index)
  {
    return (int)((buf[index]<<24)|(buf[index+1]<<16)|(buf[index+2]<<8)|buf[index+3]);
  }

  /// <summary>Reads a big-endian integer (4 bytes) from a byte array.</summary>
  public static int ReadBE4(byte* buf, int index)
  {
    return (int)((buf[index]<<24)|(buf[index+1]<<16)|(buf[index+2]<<8)|buf[index+3]);
  }

  /// <summary>Reads a little-endian long (8 bytes) from a byte array.</summary>
  public static long ReadLE8(byte[] buf, int index) { return ReadLE4U(buf, index)|((long)ReadLE4(buf, index+4)<<32); }

  /// <summary>Reads a little-endian long (8 bytes) from a byte array.</summary>
  public static long ReadLE8(byte* buf, int index) { return ReadLE4U(buf, index)|((long)ReadLE4(buf, index+4)<<32); }

  /// <summary>Reads a big-endian long (8 bytes) from a byte array.</summary>
  public static long ReadBE8(byte[] buf, int index)
  {
    return ((long)ReadBE4(buf, index)<<32)|(uint)ReadBE4(buf, index+4);
  }

  /// <summary>Reads a big-endian long (8 bytes) from a byte array.</summary>
  public static long ReadBE8(byte* buf, int index)
  {
    return ((long)ReadBE4(buf, index)<<32)|(uint)ReadBE4(buf, index+4);
  }

  /// <summary>Reads a little-endian unsigned short (2 bytes) from a byte array.</summary>
  public static ushort ReadLE2U(byte[] buf, int index) { return (ushort)(buf[index]|(buf[index+1]<<8)); }

  /// <summary>Reads a little-endian unsigned short (2 bytes) from a byte array.</summary>
  public static ushort ReadLE2U(byte* buf, int index) { return (ushort)(buf[index]|(buf[index+1]<<8)); }

  /// <summary>Reads a big-endian unsigned short (2 bytes) from a byte array.</summary>
  public static ushort ReadBE2U(byte[] buf, int index) { return (ushort)((buf[index]<<8)|buf[index+1]); }

  /// <summary>Reads a big-endian unsigned short (2 bytes) from a byte array.</summary>
  public static ushort ReadBE2U(byte* buf, int index) { return (ushort)((buf[index]<<8)|buf[index+1]); }

  /// <summary>Reads a little-endian unsigned integer (4 bytes) from a byte array.</summary>
  public static uint ReadLE4U(byte[] buf, int index)
  {
    return (uint)(buf[index]|(buf[index+1]<<8)|(buf[index+2]<<16)|(buf[index+3]<<24));
  }

  /// <summary>Reads a little-endian unsigned integer (4 bytes) from a byte array.</summary>
  public static uint ReadLE4U(byte* buf, int index)
  {
    return (uint)(buf[index]|(buf[index+1]<<8)|(buf[index+2]<<16)|(buf[index+3]<<24));
  }

  /// <summary>Reads a big-endian unsigned integer (4 bytes) from a byte array.</summary>
  public static uint ReadBE4U(byte[] buf, int index)
  {
    return (uint)((buf[index]<<24)|(buf[index+1]<<16)|(buf[index+2]<<8)|buf[index+3]);
  }

  /// <summary>Reads a big-endian unsigned integer (4 bytes) from a byte array.</summary>
  public static uint ReadBE4U(byte* buf, int index)
  {
    return (uint)((buf[index]<<24)|(buf[index+1]<<16)|(buf[index+2]<<8)|buf[index+3]);
  }

  /// <summary>Reads a little-endian unsigned long (8 bytes) from a byte array.</summary>
  public static ulong ReadLE8U(byte[] buf, int index)
  {
    return ReadLE4U(buf, index)|((ulong)ReadLE4U(buf, index+4)<<32);
  }

  /// <summary>Reads a little-endian unsigned long (8 bytes) from a byte array.</summary>
  public static ulong ReadLE8U(byte* buf, int index)
  {
    return ReadLE4U(buf, index)|((ulong)ReadLE4U(buf, index+4)<<32);
  }

  /// <summary>Reads a big-endian unsigned long (8 bytes) from a byte array.</summary>
  public static ulong ReadBE8U(byte[] buf, int index)
  {
    return ((ulong)ReadBE4U(buf, index)<<32)|ReadBE4U(buf, index+4);
  }

  /// <summary>Reads a big-endian unsigned long (8 bytes) from a byte array.</summary>
  public static ulong ReadBE8U(byte* buf, int index)
  {
    return ((ulong)ReadBE4U(buf, index)<<32)|ReadBE4U(buf, index+4);
  }

  /// <summary>Reads an IEEE754 float (4 bytes) from a byte array.</summary>
  public unsafe static float ReadFloat(byte[] buf, int index)
  {
    fixed(byte* ptr=buf) return *(float*)(ptr+index);
  }

  /// <summary>Reads an IEEE754 float (4 bytes) from a byte array.</summary>
  public unsafe static float ReadFloat(byte* buf, int index)
  {
    return *(float*)(buf+index);
  }

  /// <summary>Reads an IEEE754 double (8 bytes) from a byte array.</summary>
  public unsafe static double ReadDouble(byte[] buf, int index)
  {
    fixed(byte* ptr=buf) return *(double*)(ptr+index);
  }

  /// <summary>Reads an IEEE754 double (8 bytes) from a byte array.</summary>
  public unsafe static double ReadDouble(byte* buf, int index)
  {
    return *(double*)(buf+index);
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
  public static void WriteLE2(byte* buf, int index, short val)
  {
    buf[index]   = (byte)val;
    buf[index+1] = (byte)(val>>8);
  }

  /// <summary>Writes a big-endian short (2 bytes) to a byte array.</summary>
  public static void WriteBE2(byte[] buf, int index, short val)
  {
    buf[index]   = (byte)(val>>8);
    buf[index+1] = (byte)val;
  }

  /// <summary>Writes a big-endian short (2 bytes) to a byte array.</summary>
  public static void WriteBE2(byte* buf, int index, short val)
  {
    buf[index]   = (byte)(val>>8);
    buf[index+1] = (byte)val;
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
  public static void WriteLE4(byte* buf, int index, int val)
  {
    buf[index]   = (byte)val;
    buf[index+1] = (byte)(val>>8);
    buf[index+2] = (byte)(val>>16);
    buf[index+3] = (byte)(val>>24);
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
  public static void WriteBE4(byte* buf, int index, int val)
  {
    buf[index]   = (byte)(val>>24);
    buf[index+1] = (byte)(val>>16);
    buf[index+2] = (byte)(val>>8);
    buf[index+3] = (byte)val;
  }

  /// <summary>Writes a little-endian long (8 bytes) to a byte array.</summary>
  public static void WriteLE8(byte[] buf, int index, long val)
  {
    WriteLE4(buf, index, (int)val);
    WriteLE4(buf, index+4, (int)(val>>32));
  }

  /// <summary>Writes a little-endian long (8 bytes) to a byte array.</summary>
  public static void WriteLE8(byte* buf, int index, long val)
  {
    WriteLE4(buf, index, (int)val);
    WriteLE4(buf, index+4, (int)(val>>32));
  }

  /// <summary>Writes a big-endian long (8 bytes) to a byte array.</summary>
  public static void WriteBE8(byte[] buf, int index, long val)
  {
    WriteBE4(buf, index, (int)(val>>32));
    WriteBE4(buf, index+4, (int)val);
  }

  /// <summary>Writes a big-endian long (8 bytes) to a byte array.</summary>
  public static void WriteBE8(byte* buf, int index, long val)
  {
    WriteBE4(buf, index, (int)(val>>32));
    WriteBE4(buf, index+4, (int)val);
  }

  /// <summary>Writes a little-endian unsigned short (2 bytes) to a byte array.</summary>
  public static void WriteLE2U(byte[] buf, int index, ushort val)
  {
    buf[index]   = (byte)val;
    buf[index+1] = (byte)(val>>8);
  }

  /// <summary>Writes a little-endian unsigned short (2 bytes) to a byte array.</summary>
  public static void WriteLE2U(byte* buf, int index, ushort val)
  {
    buf[index]   = (byte)val;
    buf[index+1] = (byte)(val>>8);
  }

  /// <summary>Writes a big-endian unsigned short (2 bytes) to a byte array.</summary>
  public static void WriteBE2U(byte[] buf, int index, ushort val)
  {
    buf[index]   = (byte)(val>>8);
    buf[index+1] = (byte)val;
  }

  /// <summary>Writes a big-endian unsigned short (2 bytes) to a byte array.</summary>
  public static void WriteBE2U(byte* buf, int index, ushort val)
  {
    buf[index]   = (byte)(val>>8);
    buf[index+1] = (byte)val;
  }

  /// <summary>Writes a little-endian unsigned integer (4 bytes) to a byte array.</summary>
  public static void WriteLE4U(byte[] buf, int index, uint val)
  {
    buf[index]   = (byte)val;
    buf[index+1] = (byte)(val>>8);
    buf[index+2] = (byte)(val>>16);
    buf[index+3] = (byte)(val>>24);
  }

  /// <summary>Writes a little-endian unsigned integer (4 bytes) to a byte array.</summary>
  public static void WriteLE4U(byte* buf, int index, uint val)
  {
    buf[index]   = (byte)val;
    buf[index+1] = (byte)(val>>8);
    buf[index+2] = (byte)(val>>16);
    buf[index+3] = (byte)(val>>24);
  }

  /// <summary>Writes a big-endian unsigned integer (4 bytes) to a byte array.</summary>
  public static void WriteBE4U(byte[] buf, int index, uint val)
  {
    buf[index]   = (byte)(val>>24);
    buf[index+1] = (byte)(val>>16);
    buf[index+2] = (byte)(val>>8);
    buf[index+3] = (byte)val;
  }

  /// <summary>Writes a big-endian unsigned integer (4 bytes) to a byte array.</summary>
  public static void WriteBE4U(byte* buf, int index, uint val)
  {
    buf[index]   = (byte)(val>>24);
    buf[index+1] = (byte)(val>>16);
    buf[index+2] = (byte)(val>>8);
    buf[index+3] = (byte)val;
  }

  /// <summary>Writes a little-endian unsigned long (8 bytes) to a byte array.</summary>
  public static void WriteLE8U(byte[] buf, int index, ulong val)
  {
    WriteLE4U(buf, index, (uint)val);
    WriteLE4U(buf, index+4, (uint)(val>>32));
  }

  /// <summary>Writes a little-endian unsigned long (8 bytes) to a byte array.</summary>
  public static void WriteLE8U(byte* buf, int index, ulong val)
  {
    WriteLE4U(buf, index, (uint)val);
    WriteLE4U(buf, index+4, (uint)(val>>32));
  }

  /// <summary>Writes a big-endian unsigned long (8 bytes) to a byte array.</summary>
  public static void WriteBE8U(byte[] buf, int index, ulong val)
  {
    WriteBE4U(buf, index, (uint)(val>>32));
    WriteBE4U(buf, index+4, (uint)val);
  }

  /// <summary>Writes a big-endian unsigned long (8 bytes) to a byte array.</summary>
  public static void WriteBE8U(byte* buf, int index, ulong val)
  {
    WriteBE4U(buf, index, (uint)(val>>32));
    WriteBE4U(buf, index+4, (uint)val);
  }

  /// <summary>Writes an IEEE754 float (4 bytes) to a byte array.</summary>
  public unsafe static void WriteFloat(byte[] buf, int index, float val)
  {
    fixed(byte* ptr=buf) *(float*)(ptr+index) = val;
  }

  /// <summary>Writes an IEEE754 float (4 bytes) to a byte array.</summary>
  public unsafe static void WriteFloat(byte* buf, int index, float val)
  {
    *(float*)(buf+index) = val;
  }

  /// <summary>Writes an IEEE754 double (8 bytes) to a byte array.</summary>
  public unsafe static void WriteDouble(byte[] buf, int index, double val)
  {
    fixed(byte* ptr=buf) *(double*)(ptr+index) = val;
  }

  /// <summary>Writes an IEEE754 double (8 bytes) to a byte array.</summary>
  public unsafe static void WriteDouble(byte* buf, int index, double val)
  {
    *(double*)(buf+index) = val;
  }
  #endregion
}

} // namespace AdamMil.IO