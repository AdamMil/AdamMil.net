/*
AdamMil.Collections is a library that provides useful collection classes for
the .NET framework.

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

using System.Collections.Generic;

namespace AdamMil.Collections
{

#region IMultiHashProvider
/// <summary>An interface for hashing an item in multiple ways.</summary>
/// <remarks>This interface is used by data structures that want to hash an item using multiple hash functions. For instance, a
/// data structure may desire to obtain two hashes for each item and combine the hashes somehow.
/// </remarks>
public interface IMultiHashProvider<T>
{
  /// <include file="documentation.xml" path="/Collections/MultiHashProvider/HashCount/*"/>
  int HashCount { get; }
  /// <include file="documentation.xml" path="/Collections/MultiHashProvider/GetHashCode/*"/>
  int GetHashCode(int hashFunction, T item);
}
#endregion

#region MultiHashProvider
/// <summary>Provides a default implementation for <see cref="IMultiHashProvider{T}"/>.</summary>
public sealed class MultiHashProvider<T> : IMultiHashProvider<T>
{
  /// <include file="documentation.xml" path="/Collections/MultiHash/HashCount/*"/>
  public int HashCount
  {
    get { return int.MaxValue; }
  }

  /// <include file="documentation.xml" path="/Collections/MultiHash/GetHashCode/*"/>
  public int GetHashCode(int hashFunction, T item)
  {
    int hash = EqualityComparer<T>.Default.GetHashCode(item);
    // hash function 0 will be the built-in .NET hash function. other hash functions will be constructed using a weak 32-bit
    // block cipher with the hash function as the key and the .NET hash as the data to be encrypted. this effectively uses the
    // .NET hash value as an index into a random permutation of the 32-bit integers
    if(hashFunction != 0) hash = (int)slip32((uint)hashFunction, (uint)hash);
    return hash;
  }

  /// <summary>Gets the default <see cref="MultiHashProvider{T}"/> instance.</summary>
  public static MultiHashProvider<T> Default
  {
    get
    {
      if(instance == null) instance = new MultiHashProvider<T>();
      return instance;
    }
  }

  // this is a weak block cipher with a 32-bit key and a 32-bit output. it should be clear from the small key size that it's
  // insecure, but if that's not enough, it's a greatly weakened form of skipjack, which is a block cipher designed by the NSA
  // to include a government backdoor and which has already been broken. but it should serve its purpose of giving us a
  // pseudorandom permutation of the output space  (i.e. of the integers) with the key as a seed
  static uint slip32(uint key, uint data)
  {
    uint low = data & 0xFFFF, high = data >> 16; // split the data into two parts
    // perform four Feistel rounds. the Luby-Rackoff analysis proves that if the round function is a cryptographically secure
    // pseudorandom function, then 3 rounds suffice to create a pseudorandom permutation and 4 rounds suffice to make a "strong"
    // pseudorandom permutation. i have no idea if the round function is cryptographically secure, but the result will
    // nonetheless be some kind of permutation, and it looks pretty random in basic tests, which is good enough for my purposes
    high ^= round(key, low);
    key = (key>>8) | ((key&0xFF)<<24); // rotate the key bytes rather than it as a circular array inside round(), for efficiency
    low ^= round(key, high) ^ 1;
    key = (key>>8) | ((key&0xFF)<<24);
    high ^= round(key, low) ^ 2;
    key = (key>>8) | ((key&0xFF)<<24);
    low ^= round(key, high) ^ 3;
    return (low<<16) | high; // swap the halves and recombine them
  }

  static uint round(uint key, uint word)
  {
    uint g0, g1, g2, g3;
    g0 = permutation[(byte)(word ^ key)] ^ (word>>8);
    g1 = permutation[(byte)(g0 ^ (key>>8))] ^ word;
    g2 = permutation[(byte)(g1 ^ (key>>16))] ^ g0;
    g3 = permutation[(byte)(g2 ^ (key>>24))] ^ g1;
    return (uint)((byte)g2<<8) | (byte)g3;
  }

  static readonly byte[] permutation = new byte[256] // this is a permutation of all possible bytes (the same used in skipjack)
  {
    0xa3,0xd7,0x09,0x83,0xf8,0x48,0xf6,0xf4,0xb3,0x21,0x15,0x78,0x99,0xb1,0xaf,0xf9,
    0xe7,0x2d,0x4d,0x8a,0xce,0x4c,0xca,0x2e,0x52,0x95,0xd9,0x1e,0x4e,0x38,0x44,0x28,
    0x0a,0xdf,0x02,0xa0,0x17,0xf1,0x60,0x68,0x12,0xb7,0x7a,0xc3,0xe9,0xfa,0x3d,0x53,
    0x96,0x84,0x6b,0xba,0xf2,0x63,0x9a,0x19,0x7c,0xae,0xe5,0xf5,0xf7,0x16,0x6a,0xa2,
    0x39,0xb6,0x7b,0x0f,0xc1,0x93,0x81,0x1b,0xee,0xb4,0x1a,0xea,0xd0,0x91,0x2f,0xb8,
    0x55,0xb9,0xda,0x85,0x3f,0x41,0xbf,0xe0,0x5a,0x58,0x80,0x5f,0x66,0x0b,0xd8,0x90,
    0x35,0xd5,0xc0,0xa7,0x33,0x06,0x65,0x69,0x45,0x00,0x94,0x56,0x6d,0x98,0x9b,0x76,
    0x97,0xfc,0xb2,0xc2,0xb0,0xfe,0xdb,0x20,0xe1,0xeb,0xd6,0xe4,0xdd,0x47,0x4a,0x1d,
    0x42,0xed,0x9e,0x6e,0x49,0x3c,0xcd,0x43,0x27,0xd2,0x07,0xd4,0xde,0xc7,0x67,0x18,
    0x89,0xcb,0x30,0x1f,0x8d,0xc6,0x8f,0xaa,0xc8,0x74,0xdc,0xc9,0x5d,0x5c,0x31,0xa4,
    0x70,0x88,0x61,0x2c,0x9f,0x0d,0x2b,0x87,0x50,0x82,0x54,0x64,0x26,0x7d,0x03,0x40,
    0x34,0x4b,0x1c,0x73,0xd1,0xc4,0xfd,0x3b,0xcc,0xfb,0x7f,0xab,0xe6,0x3e,0x5b,0xa5,
    0xad,0x04,0x23,0x9c,0x14,0x51,0x22,0xf0,0x29,0x79,0x71,0x7e,0xff,0x8c,0x0e,0xe2,
    0x0c,0xef,0xbc,0x72,0x75,0x6f,0x37,0xa1,0xec,0xd3,0x8e,0x62,0x8b,0x86,0x10,0xe8,
    0x08,0x77,0x11,0xbe,0x92,0x4f,0x24,0xc5,0x32,0x36,0x9d,0xcf,0xf3,0xa6,0xbb,0xac,
    0x5e,0x6c,0xa9,0x13,0x57,0x25,0xb5,0xe3,0xbd,0xa8,0x3a,0x01,0x05,0x59,0x2a,0x46
  };

  static MultiHashProvider<T> instance;
}
#endregion

} // namespace AdamMil.Collections
