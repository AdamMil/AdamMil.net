/*
AdamMil.Utilities is a library providing generally useful utilities for
.NET development.

http://www.adammil.net/
Copyright (C) 2010-2013 Adam Milazzo

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
using System.Security.Cryptography;

namespace AdamMil.Utilities
{

/// <summary>Provides utilities for working with bytes and arrays of bytes.</summary>
public static class BinaryUtility
{
  /// <summary>Determines whether the contents of the given arrays are equal. Either array can be null.</summary>
  public unsafe static bool AreEqual(byte[] a, byte[] b)
  {
    if(a == b) return true;
    else if(a == null || b == null || a.Length != b.Length) return false;
    fixed(byte* pA=a, pB=b) return Unsafe.AreEqual(pA, pB, a.Length);
  }

  /// <summary>Determines whether the contents of the given arrays are equal.</summary>
  public unsafe static bool AreEqual(byte[] a, int aIndex, byte[] b, int bIndex, int length)
  {
    Utility.ValidateRange(a, aIndex, length);
    Utility.ValidateRange(b, bIndex, length);
    fixed(byte* pA=a, pB=b) return Unsafe.AreEqual(pA+aIndex, pB+bIndex, length);
  }

  /// <summary>Returns the number of leading zero bits in the given value.</summary>
  [CLSCompliant(false)]
  public static int CountLeadingZeros(uint value)
  {
    if(value == 0) return 32;
    int count = 1;
    if((value >> 16) == 0) { count = 17; value <<= 16; }
    if((value >> 24) == 0) { count += 8; value <<= 8; }
    if((value >> 28) == 0) { count += 4; value <<= 4; }
    if((value >> 30) == 0) { count += 2; value <<= 2; }
    return count - (int)(value >> 31);
  }

  /// <summary>Returns the number of leading zero bits in the given value.</summary>
  [CLSCompliant(false)]
  public static int CountLeadingZeros(ulong value)
  {
    uint hi = (uint)(value >> 32);
    return hi == 0 ? 32 + CountLeadingZeros((uint)value) : CountLeadingZeros(hi);
  }

  /// <summary>Returns the number of trailing zero bits in the given value.</summary>
  [CLSCompliant(false)]
  public static int CountTrailingZeros(uint value)
  {
    if(value == 0) return 32;
    int count = 1;
    if((value & 0xFFFF) == 0) { count = 17; value >>= 16; }
    if((value & 0x00FF) == 0) { count += 8; value >>= 8; }
    if((value & 0x000F) == 0) { count += 4; value >>= 4; }
    if((value & 0x0003) == 0) { count += 2; value >>= 2; }
    return count - (int)(value & 1);
  }

  /// <summary>Returns the number of trailing zero bits in the given value.</summary>
  [CLSCompliant(false)]
  public static int CountTrailingZeros(ulong value)
  {
    uint lo = (uint)value;
    return lo == 0 ? 32 + CountTrailingZeros((uint)(value>>32)) : CountTrailingZeros(lo);
  }

  /// <summary>Returns a 32-bit hash of the given array, which can be null.</summary>
  public static int Hash(byte[] data)
  {
    return Hash(0, data);
  }

  /// <summary>Returns a 32-bit hash of the given array, which can be null.</summary>
  public unsafe static int Hash(int hashFunction, byte[] data)
  {
    if(data == null)
    {
      return unchecked((int)0x9e3eff0b) + hashFunction;
    }
    else
    {
      fixed(byte* pData=data) return Hash(hashFunction, pData, data.Length);
    }
  }

  /// <summary>Returns a 32-bit hash of the given data. <paramname name="hashFunction"/> can be specified to select the hash function.</summary>
  public unsafe static int Hash(int hashFunction, byte[] data, int index, int length)
  {
    Utility.ValidateRange(data, index, length);
    fixed(byte* pData=data) return Hash(hashFunction, pData+index, length);
  }

  /// <summary>Returns a 32-bit hash of the given data.</summary>
  public static int Hash(byte[] data, int index, int length)
  {
    return Hash(0, data, index, length);
  }

  /// <summary>Returns a 32-bit hash of the given data. <paramname name="hashFunction"/> can be specified to select the hash function.</summary>
  // this method is based off the hashing code in http://burtleburtle.net/bob/c/lookup3.c. the result is slightly less uniform than the
  // previous block cipher method i was using, but is substantially faster for large inputs
  // TODO: it would be good to implement a hash method optimized for 64-bit platforms
  [CLSCompliant(false)]
  public unsafe static int Hash(int hashFunction, void* data, int length)
  {
    if(data == null) throw new ArgumentNullException();
    if(length < 0) throw new ArgumentOutOfRangeException();

    uint a, b, c;
    a = b = c = 0x9e3eff0b + (uint)hashFunction;
    for(; length > 12; data = (byte*)data+12, length -= 12)
    {
      a += *(uint*)data;
      b += *((uint*)data+1);
      c += *((uint*)data+2);
      a -= c; a ^= (c<<4)  | (c>>28); c += b;
      b -= a; b ^= (a<<6)  | (a>>26); a += c;
      c -= b; c ^= (b<<8)  | (b>>24); b += a;
      a -= c; a ^= (c<<16) | (c>>16); c += b;
      b -= a; b ^= (a<<19) | (a>>13); a += c;
      c -= b; c ^= (b<<4)  | (b>>28); b += a;
    }

    if(length >= 8)
    {
      a += *(uint*)data;
      b += *((uint*)data+1);
      c += Read((byte*)data+8, (uint)length-8);
    }
    else if(length >= 4)
    {
      a += *(uint*)data;
      b += Read((byte*)data+4, (uint)length-4);
    }
    else
    {
      if(length == 0) return (int)c; // skip the final mixing step if there's no data left
      a += Read((byte*)data, (uint)length);
    }

    c = (c^b) - ((b<<14) | (b>>18));
    a = (a^c) - ((c<<11) | (c>>21));
    b = (b^a) - ((a<<25) | (a>>7));
    c = (c^b) - ((b<<16) | (b>>16));
    a = (a^c) - ((c<<4)  | (c>>28));
    b = (b^a) - ((a<<14) | (a>>18));
    return (int)((c^b) - ((b<<24) | (b>>8)));
  }

  /// <summary>Returns a hash of the given stream. The stream is not rewound before or after computing the hash.</summary>
  public static byte[] Hash(Stream stream, HashAlgorithm algorithm)
  {
    if(stream == null || algorithm == null) throw new ArgumentNullException();
    algorithm.Initialize();
    byte[] buffer = new byte[4096];
    while(true)
    {
      int read = stream.Read(buffer, 0, buffer.Length);
      if(read != 0)
      {
        algorithm.TransformBlock(buffer, 0, read, null, 0);
      }
      else
      {
        algorithm.TransformFinalBlock(buffer, 0, 0);
        break;
      }
    }
    return algorithm.Hash;
  }

  /// <summary>Returns the 20-byte SHA1 hash of the given data.</summary>
  public static byte[] HashSHA1(byte[] data)
  {
    if(data == null) throw new ArgumentNullException();
    return HashSHA1(data, 0, data.Length);
  }

  /// <summary>Returns the 20-byte SHA1 hash of the given data.</summary>
  public static byte[] HashSHA1(byte[] data, int index, int length)
  {
    Utility.ValidateRange(data, index, length);
    using(SHA1 sha1 = SHA1.Create()) return sha1.ComputeHash(data, index, length);
  }

  /// <summary>Returns the 20-byte SHA1 hash of the given stream. The stream is not rewound before or after computing the hash.</summary>
  public static byte[] HashSHA1(Stream stream)
  {
    using(SHA1 sha1 = SHA1.Create()) return Hash(stream, sha1);
  }

  /// <summary>Parse a hex string that does not allow embedded whitespace.</summary>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="hexString"/> is null.</exception>
  /// <exception cref="FormatException">Thrown if <paramref name="hexString"/> has an odd length or contains any non-hex-digit character.</exception>
  public static byte[] ParseHex(string hexString)
  {
    return ParseHex(hexString, false);
  }

  /// <summary>Parse a hex string.</summary>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="hexString"/> is null.</exception>
  /// <exception cref="FormatException">Thrown if <paramref name="hexString"/> contains an odd number of hex digits or contains any
  /// non-hex, non-whitespace character.
  /// </exception>
  public static byte[] ParseHex(string hexString, bool allowEmbeddedWhiteSpace)
  {
    if(hexString == null) throw new ArgumentNullException();
    byte[] bytes;
    if(allowEmbeddedWhiteSpace)
    {
      if(!TryParseHex(hexString, out bytes)) throw new FormatException();
    }
    else
    {
      if((hexString.Length & 1) != 0) throw new FormatException("The length of the hex string must be a multiple of two.");
      bytes = new byte[hexString.Length / 2];
      for(int i=0, o=0; i<hexString.Length; i += 2) bytes[o++] = (byte)((HexValue(hexString[i]) << 4) | HexValue(hexString[i+1]));
    }
    return bytes;
  }

  /// <summary>Converts the given value to a string of binary digits.</summary>
  public static string ToBinary(int value)
  {
    return ToBinary((uint)value);
  }

  /// <summary>Converts the given value to a string of binary digits.</summary>
  [CLSCompliant(false)]
  public static string ToBinary(uint value)
  {
    char[] chars = new char[Math.Max(1, 32-CountLeadingZeros(value))];
    int i = chars.Length - 1;
    do
    {
      chars[i--] = (char)('0' + (value & 1));
      value >>= 1;
    } while(value != 0);
    return new string(chars);
  }

  /// <summary>Converts the given value to a string of binary digits.</summary>
  public static string ToBinary(long value)
  {
    return ToBinary((ulong)value);
  }

  /// <summary>Converts the given value to a string of binary digits.</summary>
  [CLSCompliant(false)]
  public static string ToBinary(ulong value)
  {
    char[] chars = new char[Math.Max(1, 64-CountLeadingZeros(value))];
    int i = chars.Length - 1;
    do
    {
      chars[i--] = (char)('0' + ((uint)value & 1));
      value >>= 1;
    } while(value != 0);
    return new string(chars);
  }

  /// <summary>Converts the given byte value into a corresponding two-digit hex string.</summary>
  public static string ToHex(byte value)
  {
    return new string(new char[2] { HexChars[value >> 4], HexChars[value & 0xF] });
  }

  /// <summary>Converts the given binary data into a hex string.</summary>
  public static string ToHex(byte[] data)
  {
    if(data == null) throw new ArgumentNullException();
    char[] chars = new char[data.Length * 2];
    for(int i=0, o=0; i<data.Length; i++)
    {
      byte value = data[i];
      chars[o++] = HexChars[value >> 4];
      chars[o++] = HexChars[value & 0xF];
    }
    return new string(chars);
  }

  /// <summary>Attempts to parse a hex string that may contain embedded whitespace. Returns true if binary data was parsed successfully
  /// and false if not (i.e. if there were an odd number of hex digits or any non-hex, non-whitespace characters).
  /// </summary>
  public static bool TryParseHex(string hexString, out byte[] bytes)
  {
    return TryParseHex(hexString, true, out bytes);
  }

  /// <summary>Attempts to parse a hex string into bytes. Returns true if binary data was parsed successfully and false if not.</summary>
  public static bool TryParseHex(string hexString, bool allowEmbeddedWhiteSpace, out byte[] bytes)
  {
    if(hexString == null || !allowEmbeddedWhiteSpace && (hexString.Length & 1) != 0)
    {
      bytes = null;
      return false;
    }
    else if(hexString.Length == 0)
    {
      bytes = new byte[0];
      return true;
    }
    else
    {
      bytes = null;

      byte[] data = new byte[(hexString.Length+1)/2]; // assume the hex string does not contain more than one non-hex character
      int octet = -1, length = 0;
      for(int i=0; i<hexString.Length; i++)
      {
        char c = hexString[i];
        int value;
        if(c >= '0' && c <= '9') value = c - '0';
        else if(c >= 'A' && c <= 'F') value = c - ('A' - 10);
        else if(c >= 'a' && c <= 'f') value = c - ('a' - 10);
        else if(char.IsWhiteSpace(c) && allowEmbeddedWhiteSpace) continue;
        else return false;

        if(octet == -1)
        {
          octet = value;
        }
        else
        {
          if(length == data.Length)
          {
            byte[] newArray = new byte[length*2];
            Array.Copy(data, newArray, length);
            data = newArray;
          }
          data[length++] = (byte)((octet << 4) | value);
          octet = -1;
        }
      }

      if(octet != -1) return false;

      bytes = data.Trim(length);
      return true;
    }
  }

  /// <summary>Converts the given hex digit into its numeric value.</summary>
  static int HexValue(char c)
  {
    if(c >= '0' && c <= '9') return c - '0';
    else if(c >= 'A' && c <= 'F') return c - ('A' - 10);
    else if(c >= 'a' && c <= 'f') return c - ('a' - 10);
    else throw new FormatException("'" + c.ToString() + "' is not a valid hex digit.");
  }

  static unsafe uint Read(byte* data, uint length)
  {
    switch(length)
    {
      case 1: return *data;
      case 2: return *(ushort*)data;
      case 3: return *(ushort*)data | (uint)(data[2]<<16);
      default: return *(uint*)data;
    }
  }

  const string HexChars = "0123456789ABCDEF";
}

} // namespace AdamMil.Utilities

