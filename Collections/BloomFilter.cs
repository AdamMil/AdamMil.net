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

using System;

// TODO: is it possible to use IntPtr or something to generalize this for 64-bit platforms?

namespace AdamMil.Collections
{

/// <summary>Implements a Bloom filter, which is a space-efficient probabilistic set that supports two operations. You can add
/// an item to the set using <see cref="Add"/>, and you can query whether an item might have been added to the set using
/// <see cref="PossiblyContains"/>. False positives are possible, so that <see cref="PossiblyContains"/> may return true for an
/// item that was never added, but false negatives are not possible, so if <see cref="PossiblyContains"/> returns false, then the
/// item definitely was not added. It is not possible to remove items from the set or to count or enumerate the items within the
/// set.
/// </summary>
/// <remarks>
/// <para>As more items are added to the set, the false positive rate increases. It is possible to tune the size of the Bloom
/// filter to achieve an expected false positive rate for a given number of items. The <see cref="BloomFilter{T}(int,float)"/>
/// constructor can assist with this.
/// </para>
/// <para>
/// The Bloom filter requires an <see cref="IMultiHashProvider{T}"/> to operate, although <see cref="MultiHashProvider{T}"/>
/// class, which is used by default, should be suitable.
/// </para>
/// </remarks>
public class BloomFilter<T>
{
  /// <include file="documentation.xml" path="/Collections/BloomFilter/TuningConstructor/*"/>
  public BloomFilter(int itemCount, float falsePositiveRate) : this(itemCount, falsePositiveRate, null) { }

  /// <include file="documentation.xml" path="/Collections/BloomFilter/TuningConstructor/*"/>
  /// <param name="hashProvider">The <see cref="IMultiHashProvider{T}"/> that provides the hash codes for the items added to the
  /// set, or null to use the default hash code provider.
  /// </param>
  public BloomFilter(int itemCount, float falsePositiveRate, IMultiHashProvider<T> hashProvider)
  {
    if(itemCount < 0 || falsePositiveRate <= 0 || falsePositiveRate >= 1) throw new ArgumentOutOfRangeException();
    if(itemCount == 0) itemCount = 1; // prevent a bit count of zero
    if(hashProvider == null) hashProvider = MultiHashProvider<T>.Default;
    if(hashProvider.HashCount == 0) throw new ArgumentException("The hash provider does not support any hash functions.");

    // assuming an optimal number of hash functions, the required bit count is -(itemCount * ln(falsePositiveRate) / ln(2)^2).
    // we'll convert the division into multiplication by the reciprocal (1 / ln(2)^2) ~= 2.08, and then roll the negation into it
    long bitCount = (long)Math.Round(Math.Log(falsePositiveRate) * itemCount * -2.0813689810056 + 0.5); // round up

    // given that number of bits, the optimal number of hash functions is bitCount * ln(2) / itemCount.
    int hashCount = (int)Math.Min(hashProvider.HashCount, (long)(bitCount * 0.6931471805599 / itemCount + 0.5));

    // since the hash count wasn't an exact integer, and may have been clipped by hashProvider.HashCount, we'll recalculate the
    // bit count to take into account the actual number of hash functions. the optimal number of bits for a given number of
    // hashes and false positive rate is can be computed using the following formula:
    // falsePositiveRate ~= (1 - e^(-hashCount * itemCount / bitCount)) ^ hashCount.
    // if we use p = falsePositiveRate, k = hashCount, n = itemCount, and m = bitCount, we get the following transformation:
    // p ~= (1 - e^(-kn/m))^k
    // p^(1/k) ~= 1 - e^(-kn/m)
    // 1 - p^(1/k) ~= e^(-kn/m)
    // ln(1 - p^(1/k)) ~= -kn / m
    // -kn / ln(1 - p^(1/k)) ~= m
    //
    // is there a way to calculate the bit count in a single step (by first calculating the number of hashes)?
    bitCount = (long)Math.Round((double)-hashCount * itemCount /
                                Math.Log(1 - Math.Pow(falsePositiveRate, 1.0 / hashCount)) + 0.5); // round up
    if(bitCount >= int.MaxValue) throw new ArgumentException("Too many bits (" + bitCount.ToString() + ") would be required.");

    this.bits         = new uint[(int)(bitCount/32 + ((bitCount&31) == 0 ? 0 : 1))]; // round up to the nearest 32 bits
    this.hashProvider = hashProvider;
    this.hashCount    = (int)hashCount;
  }

  /// <include file="documentation.xml" path="/Collections/BloomFilter/DirectConstructor/*"/>
  public BloomFilter(int bitCount, int maxHashCount) : this(bitCount, maxHashCount, null) { }

  /// <include file="documentation.xml" path="/Collections/BloomFilter/DirectConstructor/*"/>
  /// <param name="hashProvider">The <see cref="IMultiHashProvider{T}"/> that provides the hash codes for the items added to the
  /// set, or null to use the default hash code provider.
  /// </param>
  public BloomFilter(int bitCount, int maxHashCount, MultiHashProvider<T> hashProvider)
  {
    if(bitCount <= 0 || maxHashCount <= 0) throw new ArgumentOutOfRangeException();
    if(hashProvider == null) hashProvider = MultiHashProvider<T>.Default;
    if(hashProvider.HashCount == 0) throw new ArgumentException("The hash provider does not support any hash functions.");

    this.bits         = new uint[bitCount/32 + ((bitCount&31) == 0 ? 0 : 1)]; // round up to the nearest 32 bits
    this.hashProvider = hashProvider;
    this.hashCount    = Math.Min(hashProvider.HashCount, maxHashCount);
  }

  /// <summary>Adds an item to the set.</summary>
  public void Add(T item)
  {
    uint bitCount = (uint)bits.Length << 5; // the bit count is the array length times 32, since each uint has 32 bits
    for(int hashFunction=0; hashFunction < hashCount; hashFunction++)
    {
      // we'll get the value from each hash function and use it as an index into the bit array, setting the corresponding bits
      uint hash = (uint)hashProvider.GetHashCode(hashFunction, item) % bitCount;
      bits[hash >> 5] |= (uint)1 << (int)(hash & 31);
    }
  }

  /// <summary>Checks whether an item might have been added to the set before.</summary>
  /// <returns>True if the item might have been added to the set, or false if the item was definitely not added to the set.</returns>
  /// <remarks>Note that false positives are possible, so this method may return true for an item that was never added.</remarks>
  public bool PossiblyContains(T item)
  {
    uint bitCount = (uint)bits.Length << 5; // the bit count is the array length times 32, since each uint has 32 bits
    for(int hashFunction=0; hashFunction < hashCount; hashFunction++)
    {
      // we'll get the value from each hash function and use it as an index into the bit array. the item has not been added if
      // any bit is zero, since Add() would have set all of those bits to one
      uint hash = (uint)hashProvider.GetHashCode(hashFunction, item) % bitCount;
      if((bits[hash >> 5] & ((uint)1 << (int)(hash & 31))) == 0) return false;
    }
    return true; // all of the bits were one, so the item might have been added (or this may be a false positive)
  }

  readonly uint[] bits;
  readonly IMultiHashProvider<T> hashProvider;
  readonly int hashCount;
}

} // namespace AdamMil.Collections
