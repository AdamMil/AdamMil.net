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
using System.Security.Cryptography;
using System.Text;

namespace AdamMil.Utilities
{

/// <summary>Provides very general utilities to aid .NET development.</summary>
public static class GuidUtility
{
  /// <summary>Creates a GUID based on the given string. A given string will always produce the same GUID, and the GUID should not be
  /// expected to be "globally unique", although a collision between two distinct strings is very unlikely.
  /// </summary>
  public static Guid CreateGuid(string str)
  {
    return CreateGuid(Encoding.UTF8.GetBytes(str));
  }

  /// <summary>Creates a GUID based on the given data. A given set of bytes will always produce the same GUID, and the GUID should not be
  /// expected to be "globally unique", although a collision between any two distinct strings is very unlikely.
  /// </summary>
  public static Guid CreateGuid(byte[] data)
  {
    byte[] hash;
    using(SHA1 sha1 = SHA1.Create()) hash = sha1.ComputeHash(data);

    // a GUID comprises four portions: one 32-bit integer, two 16 bit integers, and one block of 8 bytes. the top 1 to 3 bits of
    // the second byte of the fourth portion represent the type of GUID (0=NCS backwards compatibility, 10=standard,
    // 110=COM backwards compatibility, and 111=reserved). the top four bits of the third portion represent the version of the
    // GUID (1=MAC+Timestamp, 2=MAC+UserID, 3=MD5, 4=random, 5=SHA1). we'll create a standard version 5 GUID from a SHA1 hash.
    uint a = BitConverter.ToUInt32(hash, 0);
    ushort b = BitConverter.ToUInt16(hash, 4), c = BitConverter.ToUInt16(hash, 6);

    c = (ushort)(c & ~0xC000 | (5<<12)); // set the top four bits of the third portion to 5, indicating a version 5 GUID
    // set the top two bits of the second byte of the last portion to 10, to indicate that we're creating a standard GUID
    return new Guid(a, b, c, hash[8], (byte)(hash[9] & ~0xC0 | 0x80), hash[10], hash[11], hash[12], hash[13], hash[14], hash[15]);
  }

  /// <summary>Attempts to parse a <see cref="Guid"/> from a string. The string may contain leading and trailing whitespace.
  /// Returns true if successful and false if not.
  /// </summary>
  public static bool TryParse(string str, out Guid guid)
  {
    return TryParse(str, true, out guid);
  }

  /// <summary>Attempts to parse a <see cref="Guid"/> from a string. If <paramref name="allowWhitespace"/> is true, the string may
  /// contain leading and trailing whitespace. Returns true if successful and false if not.
  /// </summary>
  public static bool TryParse(string str, bool allowWhitespace, out Guid guid)
  {
    if(str != null)
    {
      int start = 0, length = str.Length;
      if(allowWhitespace) StringUtility.Trim(str, out start, out length);
      if(length >= 32)
      {
        int end = start + length;
        char s = str[start];
        if(s == '{' || s == '(')
        {
          char e = str[end-1];
          if(e == (s == '{' ? '}' : ')'))
          {
            start++;
            end--;
          }
          else
          {
            start = -1;
          }
        }

        if(start >= 0)
        {
          uint a, f, h;
          ushort b, c, d;

          if(TryParse(str, ref start, end, out a) && TryParse(str, ref start, end, out b) && TryParse(str, ref start, end, out c) &&
             TryParse(str, ref start, end, out d) && TryParse(str, start, end, 4, out f) && TryParse(str, start+4, end, 8, out h) &&
             start+12 == end)
          {
            guid = new Guid(a, b, c, (byte)(d>>8), (byte)d, (byte)(f>>8), (byte)f,
                            (byte)(h>>24), (byte)(h>>16), (byte)(h>>8), (byte)h);
            return true;
          }
        }
      }
    }

    guid = new Guid();
    return false;
  }

  static int HexValue(char c)
  {
    if(c >= '0' && c <= '9') return c - '0';
    else if(c >= 'A' && c <= 'F') return c - ('A' - 10);
    else if(c >= 'a' && c <= 'f') return c - ('a' - 10);
    else return -1;
  }

  static bool TryParse(string str, ref int start, int end, out uint value)
  {
    if(!TryParse(str, start, end, 8, out value)) return false;
    start += 8;
    if(start < end && str[start] == '-') start++;
    return true;
  }

  static bool TryParse(string str, ref int start, int end, out ushort value)
  {
    uint uintValue;
    bool success = TryParse(str, start, end, 4, out uintValue);
    value = (ushort)uintValue;
    if(success)
    {
      start += 4;
      if(start < end && str[start] == '-') start++;
    }
    return success;
  }

  static bool TryParse(string str, int start, int end, int length, out uint value)
  {
    value = 0;
    if(end - start < length) return false;

    uint v = 0;
    for(int e=start+length; start<e; start++)
    {
      int nibble = HexValue(str[start]);
      if(nibble == -1) return false;
      v = (v << 4) | (uint)nibble;
    }

    value = v;
    return true;
  }
}

} // namespace AdamMil.Utilities
