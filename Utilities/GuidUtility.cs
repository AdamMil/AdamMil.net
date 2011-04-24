﻿/*
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
using System.Text;

namespace AdamMil.Utilities
{

/// <summary>Provides very general utilities to aid .NET development.</summary>
public static class GuidUtility
{
  /// <summary>Creates a GUID based on the given string. A given string will always produce the same GUID.</summary>
  public static Guid CreateGuid(string str)
  {
    return CreateGuid(Encoding.UTF8.GetBytes(str));
  }

  /// <summary>Creates a GUID based on the given data. A given set of bytes will always produce the same GUID.</summary>
  public static Guid CreateGuid(byte[] data)
  {
    byte[] hash;
    using(SHA1 sha1 = SHA1.Create()) hash = sha1.ComputeHash(data);

    // a GUID comprises four portions, one 32-bit integer, two 16 bit integers, and one block of 8 bytes. the top 1 to 3 bits of
    // the second byte of the fourth portion represent the type of GUID (0=NCS backwards compatibility, 10=standard,
    // 110=COM backwards compatibility, and 111=reserved). the top four bits of the third portion represent the version of the
    // GUID (1=MAC+Timestamp, 2=MAC+UserID, 3=MD5, 4=random, 5=SHA1). we'll create a standard version 5 GUID from a SHA1 hash.
    uint a = BitConverter.ToUInt32(hash, 0);
    ushort b = BitConverter.ToUInt16(hash, 4), c = BitConverter.ToUInt16(hash, 6);

    c = (ushort)(c & ~0xC000 | (5<<12)); // set the top four bits of the third portion to 5, indicating a version 5 GUID
    // set the top two bits of the second byte of the last portion to 10, to indicate that we're creating a standard GUID
    return new Guid(a, b, c, hash[8], (byte)(hash[9] & ~0xC0 | 0x80), hash[10], hash[11], hash[12], hash[13], hash[14], hash[15]);
  }
}

} // namespace AdamMil.Utilities
