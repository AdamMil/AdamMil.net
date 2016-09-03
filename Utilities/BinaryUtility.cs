/*
AdamMil.Utilities is a library providing generally useful utilities for
.NET development.

http://www.adammil.net/
Copyright (C) 2010-2016 Adam Milazzo

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

#region BinaryUtility
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

  /// <summary>Swaps the bytes in the value, converting big-endian values to little-endian and vice versa.</summary>
  public static int ByteSwap(int value)
  {
    return (int)ByteSwap((uint)value);
  }

  /// <summary>Swaps the bytes in the value, converting big-endian values to little-endian and vice versa.</summary>
  [CLSCompliant(false)]
  public static uint ByteSwap(uint value)
  {
    return (value>>24) | ((value>>8) & 0xFF00) | ((value<<8) & 0xFF0000) | (value<<24);
  }

  /// <summary>Swaps the bytes in the value, converting big-endian values to little-endian and vice versa.</summary>
  public static long ByteSwap(long value)
  {
    return (long)ByteSwap((ulong)value);
  }

  /// <summary>Swaps the bytes in the value, converting big-endian values to little-endian and vice versa.</summary>
  [CLSCompliant(false)]
  public static ulong ByteSwap(ulong value)
  {
    return ((ulong)ByteSwap((uint)value)<<32) | ByteSwap((uint)(value>>32));
  }

  /// <summary>Swaps the bytes in the value, converting big-endian values to little-endian and vice versa.</summary>
  public static short ByteSwap(short value)
  {
    return (short)ByteSwap((ushort)value);
  }

  /// <summary>Swaps the bytes in the value, converting big-endian values to little-endian and vice versa.</summary>
  [CLSCompliant(false)]
  public static ushort ByteSwap(ushort value)
  {
    return (ushort)((value>>8) | (value<<8));
  }

  /// <summary>Returns the number of one bits in the value.</summary>
  [CLSCompliant(false)]
  public static int CountBits(uint value)
  {
    // the basic idea is to take a binary value and count the number of bits in each pair, each nibble, each byte, etc. for example, with
    // the value 11100100, we count the bits in each pair 10|01|01|00. we do this by masking off the high and low bits in each pair,
    // shifting the high bit to overlap the low bit, and adding them: (x & 0x55) + ((x>>1) & 0x55) (assuming x is a byte). (e.g. 10100000
    // and 01000100 after masking, 01010000 and 01000100 after shifting, and 10010100 after adding.) the addition will never overflow,
    // since the maximum is two bits per pair. then we repeat with a shift of 2 and a mask of 0x33, and shift of 4 and a mask of 0x0F.
    // (this continues two more steps for the full 32-bit word.) now there's a way to optimize the first step, since
    // (x & 0x55) + ((x>>1) & 0x55) == x - ((x>>1) & 0x55). it's basically taking the pair minus the high bit. this works because the
    // possibilities are: 11-01 = 10, 10-01 = 01, 01-00 = 01, and 00-00 = 00. the full 32-bit example:
    // 10111100011000110111111011111111 -> 01|10|10|00|01|01|00|10|01|10|10|01|10|10|10|10
    value = value - ((value>>1) & 0x55555555); // 0x55 == 01010101
    // now we have the sums for pairs and want to compute the sums for nibbles: 0011|0010|0010|0010|0011|0011|0100|0100
    value = (value & 0x33333333) + ((value >> 2) & 0x33333333); // 0x33 == 00110011 (the only step that can't be optimized with 32 bits)
    // now we have the sums for nibbles and want to compute the sums for bytes: 00000101|00000100|00000110|00001000. note that each field
    // in the sum of nibbles has a leading zero. we can skip the masking before adding since this leading zero ensures that when we align
    // the fields and add, there will be no carry from one field into the next. we just have to do the mask at the end.
    // 0011|0010|0010|0010|0011|0011|0100|0100 + 0000|0011|0010|0010|0010|0011|0011|0100 = 00110101|01000100|01010110|01111000
    // the correct sums are in the lower nibbles after the addition, so we can mask off the higher nibbles with 0x0F0F0F0F
    value = (value + (value >> 4)) & 0x0F0F0F0F; // 0x0F == 00001111
    // now we have the sums for bytes and want to compute the sums for 16-bit half-words: 0000000000001001|0000000000001110. at this point
    // there's so much zero padding (4 bits per byte) that we don't need any more masking until the very end. so we just align and add as
    // before: 00000101|00000100|00000110|00001000 + 00000000|00000101|00000100|00000110 = 0000010100001001|0000101000001110. the correct
    // answers are in the lower 5 bits of each 16-bit half-word, and we still have at least 3 bits of padding before the garbage bits begin
    value = value + (value >> 8);
    // now we have the sums for 16-bit words and want to get the sum for the complete word: 00000000000000000000000000010111. as before,
    // we just align and add: 0000010100001001|0000101000001110 + 0000000000000000|0000010100001001 = 00000101000010010000111100010111.
    // the answer is in the bottom 6 bits
    return (int)((value + (value >> 16)) & 0x3F); // AND with 63, not 31, because there can be up to 32 bits
  }

  /// <summary>Returns the number of one bits in the value.</summary>
  [CLSCompliant(false)]
  public static unsafe int CountBits(ulong value)
  {
    if(sizeof(IntPtr) >= 8) // on a 64-bit architecture, operate on the 64-bit value directly
    {
      // this takes the same approach as the 32-bit version and extends it a step. we still don't need any extra masking. the 32-bit
      // version ended with at least 2 bits of padding per field. after adding a step we still have at least one bit of padding
      value = value - ((value>>1) & 0x5555555555555555);
      value = (value & 0x3333333333333333) + ((value>>2) & 0x3333333333333333);
      value = (value + (value>>4)) & 0x0F0F0F0F0F0F0F0F;
      value = value + (value>>8);
      value = value + (value>>16);
      return (int)(((uint)value + (uint)(value>>32)) & 0x7F);
    }
    else // otherwise, break it up into 32-bit words and operate on them
    {
      return CountBits((uint)value) + CountBits((uint)(value>>32));
    }
  }

  /// <summary>Returns the number of leading zero bits in the given value.</summary>
  [CLSCompliant(false)]
  public static int CountLeadingZeros(uint value)
  {
    if(value == 0) return 32;
    // this algorithm works by repeatedly subdividing the value. first it considers the top 16 bits. if they're zero, it adds 16 to the
    // leading zero count and left-shifts the value by 16. either way, the top 16 bits now contain at least one 1 bit since we ensured
    // that the value was non-zero above. then we check the top 8 bits and possibly left-shift by 8, after which the leading one bit is
    // in the top 8 bits. etc.
    int count = 1; // add 1 to the start since we know there's a leading bit somewhere. this saves one operation later
    if((value >> 16) == 0) { count = 17; value <<= 16; } // after this, the leading bit is in the top 16 bits
    if((value >> 24) == 0) { count += 8; value <<= 8; }  // after this, the leading bit is in the top 8 bits
    if((value >> 28) == 0) { count += 4; value <<= 4; }  // after this, the leading bit is in the top 4 bits
    if((value >> 30) == 0) { count += 2; value <<= 2; }  // after this, the leading bit is in the top 2 bits
    // now, either the leading bit is in the high bit or it's in the one below. if it's not in the high bit, we'd want to add one to the
    // count. however, we already added 1 to the count at the start, so instead we need to subtract 1 if the leading one bit /is/ in the
    // high bit. we can just shift right and subtract (or do a signed shift and add)
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
    // this algorithm works just like CountLeadingZeros above, except that at each step it ensures the trailing one bit is in the lower
    // bits rather than the upper bits
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
    fixed(byte* pData=data) return Hash(hashFunction, pData, data != null ? 0 : data.Length);
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
  // TODO: it would be good to implement a hash method optimized for 64-bit platforms. perhaps http://burtleburtle.net/bob/hash/spooky.html
  [CLSCompliant(false)]
  public unsafe static int Hash(int hashFunction, void* data, int length)
  {
    if(length < 0) throw new ArgumentOutOfRangeException();
    else if(length > 0 && data == null) throw new ArgumentNullException();

    byte* pData = (byte*)data;
    uint a, b, c;
    a = b = c = 0x9e3eff0b + (uint)hashFunction;
    for(; length > 12; pData = (byte*)pData+12, length -= 12)
    {
      a += *(uint*)pData;
      b += *((uint*)pData+1);
      c += *((uint*)pData+2);
      a -= c; a ^= (c<<4)  | (c>>28); c += b;
      b -= a; b ^= (a<<6)  | (a>>26); a += c;
      c -= b; c ^= (b<<8)  | (b>>24); b += a;
      a -= c; a ^= (c<<16) | (c>>16); c += b;
      b -= a; b ^= (a<<19) | (a>>13); a += c;
      c -= b; c ^= (b<<4)  | (b>>28); b += a;
    }

    if(length >= 4)
    {
      a += *(uint*)pData;
      if(length >= 8)
      {
        b += *((uint*)pData+1);
        c += Read((byte*)pData+8, (uint)length-8);
      }
      else
      {
        b += Read((byte*)pData+4, (uint)length-4);
      }
    }
    else
    {
      if(length != 0)
      {
        a += Read((byte*)pData, (uint)length);
      }
      else
      {
        return (int)c; // skip the final mixing step if there's no data left
      }
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

  /// <summary>Determines whether the given character is a hex digit.</summary>
  public static bool IsHex(char c)
  {
    return c <= '9' ? c >= '0' : c >= 'a' ? c <= 'f' : c >= 'A' && c <= 'F';
  }

  /// <summary>Parses the given hex digit into its numeric value.</summary>
  public static byte ParseHex(char c)
  {
    // '0' is 48, 'A' is 65, and 'a' is 97
    if(c <= '9') // if c <= '9' then it can't be a-f or A-F
    {
      if(c >= '0') return (byte)(c - '0');
    }
    else if(c >= 'a') // if c >= 'a' then it can't be A-F
    {
      if(c <= 'f') return (byte)(c - ('a'-10));
    }
    else if(c >= 'A' && c <= 'F')
    {
      return (byte)(c - ('A'-10));
    }
    throw new FormatException("'" + c.ToString() + "' is not a valid hex digit.");
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
      for(int i=0, o=0; i<hexString.Length; i += 2) bytes[o++] = (byte)((ParseHex(hexString[i]) << 4) | ParseHex(hexString[i+1]));
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

  /// <summary>Converts the given byte value into a corresponding two-digit uppercase hex string.</summary>
  public static string ToHex(byte value)
  {
    return ToHex(value, false);
  }

  /// <summary>Converts the given byte value into a corresponding two-digit hex string.</summary>
  public static string ToHex(byte value, bool lowercase)
  {
    string hexChars = lowercase ? LHexChars : UHexChars;
    return new string(new char[2] { hexChars[value >> 4], hexChars[value & 0xF] });
  }

  /// <summary>Converts the given binary data into an uppercase hex string.</summary>
  public static string ToHex(byte[] data)
  {
    return ToHex(data, false);
  }

  /// <summary>Converts the given binary data into a hex string.</summary>
  public static string ToHex(byte[] data, bool lowercase)
  {
    if(data == null) throw new ArgumentNullException();
    return ToHex(data, 0, data.Length, lowercase);
  }

  /// <summary>Converts the given binary data into an uppercase hex string.</summary>
  public static string ToHex(byte[] data, int index, int length)
  {
    return ToHex(data, index, length, false);
  }

  /// <summary>Converts the given binary data into a hex string.</summary>
  public static string ToHex(byte[] data, int index, int length, bool lowercase)
  {
    Utility.ValidateRange(data, index, length);
    char[] chars = new char[length * 2];
    string hexChars = lowercase ? LHexChars : UHexChars;
    for(int end=index+length, o=0; index<end; index++)
    {
      byte value = data[index];
      chars[o++] = hexChars[value >> 4];
      chars[o++] = hexChars[value & 0xF];
    }
    return new string(chars);
  }

  /// <summary>Converts a nibble value (0-15) into the corresponding uppercase hex digit.</summary>
  public static char ToHexChar(byte nibble)
  {
    if(nibble >= 16) throw new ArgumentOutOfRangeException();
    return UHexChars[nibble];
  }

  /// <summary>Converts a nibble value (0-15) into the corresponding hex digit.</summary>
  public static char ToHexChar(byte nibble, bool lowercase)
  {
    if(nibble >= 16) throw new ArgumentOutOfRangeException();
    return lowercase ? LHexChars[nibble] : UHexChars[nibble];
  }

  /// <summary>Attempts to parse a hex digit into its numeric value. Returns true if it was parsed successfully.</summary>
  public static bool TryParseHex(char c, out byte value)
  {
    // '0' is 48, 'A' is 65, and 'a' is 97
    if(c <= '9') // if c <= '9' then it can't be a-f or A-F
    {
      if(c >= '0') { value = (byte)(c - '0'); return true; }
    }
    else if(c >= 'a') // if c >= 'a' then it can't be A-F
    {
      if(c <= 'f') { value = (byte)(c - ('a'-10)); return true; }
    }
    else if(c >= 'A' && c <= 'F')
    {
      value = (byte)(c - ('A'-10));
      return true;
    }
    value = 0;
    return false;
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

      // assume the hex string does not contain more than one non-hex character. this gives us the correct length if the string contains
      // no non-hex characters, which should be a common case
      byte[] data = new byte[(hexString.Length+1)/2];
      int octet = -1, length = 0;
      for(int i=0; i<hexString.Length; i++)
      {
        char c = hexString[i];
        int value;
        if(c <= '9' && c >= '0') value = c - '0';
        else if(c >= 'a' && c <= 'f') value = c - ('a' - 10);
        else if(c >= 'A' && c <= 'F') value = c - ('A' - 10);
        else if(char.IsWhiteSpace(c) && allowEmbeddedWhiteSpace) continue;
        else return false;

        if(octet < 0)
        {
          octet = value;
        }
        else
        {
          if(length == data.Length) data = Utility.EnlargeArray(data, length, 1);
          data[length++] = (byte)((octet << 4) | value);
          octet = -1;
        }
      }

      if(octet >= 0) return false;

      bytes = data.Trim(length);
      return true;
    }
  }

  static unsafe uint Read(byte* data, uint length)
  {
    switch(length)
    {
      case 0: return 0;
      case 1: return *data;
      case 2: return *(ushort*)data;
      case 3: return *(ushort*)data | (uint)(data[2]<<16);
      default: return *(uint*)data;
    }
  }

  const string LHexChars = "0123456789abcdef", UHexChars = "0123456789ABCDEF";
}
#endregion

#region CRC32
/// <summary>Computes a 32-bit cyclic redundancy check using a configurable polynomial.</summary>
/// <remarks>For hashing, <see cref="BinaryUtility.Hash(byte[])"/> and its overrides are preferable to this class, because they're about 4x
/// as fast, support multiple hash functions, and are designed for hashing. This class is recommended when you must generate checksum
/// values to match a particular standard, such as the PNG standard. This class uses and generates little-endian (reversed) CRC values,
/// regardless of machine architecture, and inverts the bits of the CRC, as this is the most common form. As a result, it cannot currently
/// support all known 32-bit CRCs, but it supports the most common ones.
/// </remarks>
// TODO: support big-endian (non-reversed) CRCs and different initialization and final XOR values, so we can support other well-known
// 32-bit CRCs such as CRC-32Q, CRC-32/BZIP2, and CRC-32/MPEG-2
public sealed class CRC32
{
  /// <summary>The CRC-32C polynomial (0x82F63B78) used by iSCSI, Ext4, etc. A <see cref="CRC32"/> object initialized with this polynomial
  /// is accessible from the <see cref="CRC32C"/> member.
  /// </summary>
  public const int CRC32CPolynomial = unchecked((int)0x82F63B78);
  /// <summary>The standard polynomial (0xEDB88320) used by Ethernet, PNG, gzip, etc. A <see cref="CRC32"/> object initialized with this
  /// polynomial is accessible from the <see cref="Default"/> member.
  /// </summary>
  public const int StandardPolynomial = unchecked((int)0xEDB88320);

  /// <summary>Initializes a CRC-32 calculator with the standard polynomial (0xEDB88320) used by Ethernet, PNG, etc.</summary>
  /// <remarks>This class initializes a table when constructed, which can take significant time. Therefore, the object should be
  /// reused if multiple CRC values need to be computed, and you should use the built-in CRC object <see cref="Default"/> instead if
  /// possible.
  /// </remarks>
  public CRC32() : this(StandardPolynomial) { }

  /// <summary>Initializes a CRC-32 calculator with the given polynomial. The polynomial should be specified in reverse form, so that the
  /// standard polynomial used by Ethernet, PNG, etc. is 0xEDB88320 and the CRC-32C polynomial used by iSCSI, etc. is 0x82F63B78. (These
  /// two constants are available from <see cref="StandardPolynomial"/> and <see cref="CRC32CPolynomial"/> respectively.)
  /// </summary>
  /// <remarks>This class initializes a table when constructed, which can take significant time. Therefore, the object should be
  /// reused if multiple CRC values need to be computed, and you should use the built-in CRC objects <see cref="Default"/> and
  /// <see cref="CRC32C"/> if possible.
  /// </remarks>
  public CRC32(int polynomial)
  {
    // compute a table that allows us to read data 4 bytes a time
    for(uint i=0; i<256; i++) // the first 256 entries are the standard Sarwate table
    {
      uint crc = i;
      for(int j=0; j<8; j++) crc = (crc >> 1) ^ (uint)(-(int)(crc & 1) & polynomial); // -(crc&1) & p == (crc&1) != 0 ? p : 0
      table[i] = crc;
    }
    for(uint i=0; i<256; i++) // the other 768 entries use the "slicing-by-4" technique apparently invented at Intel
    {
      uint v = table[i];
      table[i+256] = v = (v>>8) ^ table[v & 0xFF];
      table[i+512] = v = (v>>8) ^ table[v & 0xFF];
      table[i+768] = v = (v>>8) ^ table[v & 0xFF];
    }
  }

  /// <summary>Computes the CRC of the given array.</summary>
  public int Compute(byte[] data)
  {
    if(data == null) throw new ArgumentNullException();
    return Compute(data, 0, data.Length, 0);
  }

  /// <summary>Computes the CRC of the given array segment.</summary>
  public int Compute(byte[] data, int index, int length)
  {
    return Compute(data, index, length, 0);
  }

  /// <summary>Computes the CRC of the given array segment, continuing the CRC of a previous chunk of data.</summary>
  public unsafe int Compute(byte[] data, int index, int length, int previousCRC)
  {
    Utility.ValidateRange(data, index, length);
    // zero-length arrays yield a null-pointer when fixed, so check for a length of zero to avoid an ArgumentNullException
    if(length == 0) return previousCRC;
    else fixed(byte* pData=data) return Compute(pData+index, length, previousCRC);
  }

  /// <summary>Computes the CRC of the given data.</summary>
  [CLSCompliant(false)]
  public unsafe int Compute(void* data, int length)
  {
    return Compute(data, length, 0);
  }

  /// <summary>Computes the CRC of the given chunk of data, continuing the CRC of a previous chunk of data.</summary>
  [CLSCompliant(false)]
  public unsafe int Compute(void* data, int length, int previousCRC)
  {
    if(data == null) throw new ArgumentNullException();
    if(length < 0) throw new ArgumentOutOfRangeException();
    uint crc = (uint)~previousCRC;
    if(length != 0)
    {
      fixed(uint* pTable=this.table)
      {
        byte* pData = (byte*)data;
        // first align the read pointer to a 4-byte boundary
        while(((uint)pData & 3) != 0 && --length >= 0) crc = (crc>>8) ^ pTable[(crc&0xFF) ^ *pData++];
        // now read data in 4-byte chunks
        for(byte* pEnd = pData + (length & ~3); pData < pEnd; pData += 4)
        {
          if(BitConverter.IsLittleEndian) crc ^= *(uint*)pData;
          else crc ^= BinaryUtility.ByteSwap(*(uint*)pData); // byte-swap the data on big-endian architectures
          crc  = pTable[crc>>24] ^ pTable[((crc>>16) & 0xFF) + 256] ^ pTable[((crc>>8) & 0xFF) + 512] ^ pTable[(crc & 0xFF) + 768];
        }
        length &= 3;
        while(--length >= 0) crc = (crc>>8) ^ pTable[(crc&0xFF) ^ *pData++];
      }
    }
    return (int)~crc;
  }

  readonly uint[] table = new uint[1024];

  /// <summary>Gets a <see cref="CRC32"/> object initialized with the <see cref="CRC32CPolynomial"/>.</summary>
  public static CRC32 CRC32C
  {
    get
    {
      if(_crc32c == null) _crc32c = new CRC32(CRC32CPolynomial);
      return _crc32c;
    }
  }

  /// <summary>Gets a <see cref="CRC32"/> object initialized with the <see cref="StandardPolynomial"/>.</summary>
  public static CRC32 Default
  {
    get
    {
      if(_default == null) _default = new CRC32();
      return _default;
    }
  }

  static CRC32 _default, _crc32c;
}
#endregion

} // namespace AdamMil.Utilities
