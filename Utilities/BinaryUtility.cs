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
using System.IO;
using System.Security.Cryptography;

namespace AdamMil.Utilities
{

/// <summary>Provides utilities for working with bytes and arrays of bytes.</summary>
public static class BinaryUtility
{
  /// <summary>Returns a hash of the given stream. The stream is not rewound before or after computing the hash.</summary>
  public static byte[] Hash(Stream stream, HashAlgorithm hash)
  {
    if(stream == null || hash == null) throw new ArgumentNullException();
    hash.Initialize();
    byte[] buffer = new byte[4096];
    while(true)
    {
      int read = stream.Read(buffer, 0, buffer.Length);
      if(read != 0)
      {
        hash.TransformBlock(buffer, 0, read, null, 0);
      }
      else
      {
        hash.TransformFinalBlock(buffer, 0, 0);
        break;
      }
    }
    return hash.Hash;
  }

  /// <summary>Returns the 20-byte SHA1 hash of the given data.</summary>
  public static byte[] HashSHA1(byte[] data)
  {
    if(data == null) throw new ArgumentNullException();
    using(SHA1 sha1 = SHA1.Create()) return sha1.ComputeHash(data);
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
  public static byte[] ParseHex(string hexString, bool allowEmbeddedWhitespace)
  {
    if(hexString == null) throw new ArgumentNullException();
    byte[] bytes;
    if(allowEmbeddedWhitespace)
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

  /// <summary>Attempts to parse a hex string that may contain embedded whitespace. Returns true if binary data was parsed successfully
  /// and false if not (i.e. if there were an odd number of hex digits or any non-hex, non-whitespace characters).
  /// </summary>
  public static bool TryParseHex(string hexString, out byte[] bytes)
  {
    if(hexString == null)
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
        else if(char.IsWhiteSpace(c)) continue;
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

