/*
AdamMil.Utilities is a library providing generally useful utilities for
.NET development.

http://www.adammil.net/
Copyright (C) 2010-2011 Adam Milazzo

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

namespace AdamMil.Utilities
{

/// <summary>Provides utilities for working with bytes and arrays of bytes.</summary>
public static class BinaryUtility
{
  /// <summary>Converts the given string, which is assumed to contain only hex digits, into the corresponding byte array.</summary>
  /// <exception cref="FormatException">Thrown if the string contains non-hex digits or if the string's length is odd.</exception>
  public static byte[] FromHex(string hex)
  {
    if(hex == null) throw new ArgumentNullException();
    hex = hex.Trim();
    if((hex.Length & 1) != 0) throw new FormatException("The length of the hex data must be a multiple of two.");

    byte[] data = new byte[hex.Length / 2];
    for(int i=0, o=0; i<hex.Length; i += 2)
    {
      data[o++] = (byte)((HexValue(hex[i]) << 4) | HexValue(hex[i+1]));
    }
    return data;
  }

  /// <summary>Returns the 20-byte SHA1 hash of the given data.</summary>
  public static byte[] HashSHA1(byte[] data)
  {
    if(data == null) throw new ArgumentNullException();
    using(SHA1 sha1=SHA1.Create()) return sha1.ComputeHash(data);
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

  /// <summary>Converts the given hex digit into its numeric value.</summary>
  static int HexValue(char c)
  {
    if(c >= '0' && c <= '9') return c - '0';
    else if(c >= 'A' && c <= 'F') return c - ('A' - 10);
    else if(c >= 'a' && c <= 'f') return c - ('a' - 10);
    else throw new FormatException("'" + c.ToString() + "' is not a valid hex digit.");
  }

  const string HexChars = "0123456789ABCDEF";
}

} // namespace AdamMil.Utilities

