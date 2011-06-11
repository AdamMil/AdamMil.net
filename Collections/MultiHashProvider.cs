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

namespace AdamMil.Collections
{

// TODO: it would be good to implement hash providers optimized for 64-bit platforms (e.g. by using 64-bit block ciphers)

#region IMultiHashProvider
/// <summary>An interface for hashing an item in multiple ways.</summary>
/// <remarks>This interface is used by data structures that need to hash items using multiple hash functions. For instance, a
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

#region MultiHashProvider<T>
/// <summary>Provides implementations of <see cref="IMultiHashProvider{T}"/> suitable for most types. Use <see cref="Default"/>
/// to retrieve a suitable hash provider for a given type.
/// </summary>
/// <remarks>
/// This implementation has good-quality hashes for all integer types, <see cref="DateTime"/>, <see cref="string"/>,
/// <see cref="Decimal"/>, <see cref="Single"/>, <see cref="Double"/>, and <see cref="Char"/>, as well as nullable versions of
/// those. For other types, this implementation works by taking taking the standard .NET hash value (returned by
/// <see cref="object.GetHashCode"/>) and using it as an index into a number of pseudorandom permutations of the integers,
/// corresponding to the different hash functions. The problem with this approach -- and it is a rather severe problem in
/// general -- is that if two objects collide using one hash function, they will collide using every hash function. This may or
/// may not be a problem in your particular use case, but if it is, you'll need to create custom hash providers for those types.
/// </remarks>
public abstract class MultiHashProvider<T> : IMultiHashProvider<T>
{
  internal MultiHashProvider() { } // prevent subclassing outside this assembly

  /// <include file="documentation.xml" path="/Collections/MultiHashProvider/HashCount/*"/>
  public int HashCount
  {
    get { return int.MaxValue; }
  }

  /// <include file="documentation.xml" path="/Collections/MultiHashProvider/GetHashCode/*"/>
  public abstract int GetHashCode(int hashFunction, T item);

  /// <summary>Gets the default <see cref="MultiHashProvider{T}"/> instance.</summary>
  public static IMultiHashProvider<T> Default
  {
    get
    {
      if(instance == null)
      {
        object provider;
        switch(Type.GetTypeCode(typeof(T)))
        {
          case TypeCode.DateTime: provider = new DateTimeHashProvider(); break;
          case TypeCode.Decimal: provider = new DecimalHashProvider(); break;
          case TypeCode.Double: provider = new DoubleHashProvider(); break;
          case TypeCode.Int64: provider = new Int64HashProvider(); break;
          case TypeCode.String: provider = new StringHashProvider(); break;
          case TypeCode.UInt64: provider = new Uint64HashProvider(); break;
          case TypeCode.Object:
            if(typeof(T).IsGenericType && !typeof(T).ContainsGenericParameters && // if T is some kind of Nullable<U>...
               typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>))
            {
              Type hashType = typeof(NullableHashProvider<>).MakeGenericType(typeof(T).GetGenericArguments()[0]);
              provider = Activator.CreateInstance(hashType);
              break;
            }
            goto default;
          default:
          {
            // integers <= 32-bits can't collide don't need any special handling, since they can't collide without being the same
            Type hashType = typeof(T).IsValueType ? typeof(GenericValueTypeHashProvider<>)
                                                  : typeof(GenericReferenceTypeHashProvider<>);
            provider = Activator.CreateInstance(hashType.MakeGenericType(typeof(T)));
            break;
          }
        }
        instance = (MultiHashProvider<T>)provider;
      }
      return instance;
    }
  }

  // this is a weak block cipher with a 32-bit key and a 32-bit output. it should be clear from the small key size that it's
  // insecure, but if that's not enough, it's a weakened form of an already weak algorithm, xxtea. but it should serve its
  // purpose of giving us a pseudorandom permutation of the output space (i.e. of the 32-bit integers) with the key as the seed
  internal static uint tea32(uint key, uint data)
  {
    data += (((data>>5)^(data<<2)) + ((data>>3)^(data<<4))) ^ ((0x9e3779b9^data) + (key^data));
    key = (key>>8) | ((key&0xFF)<<24); // rotate the bytes in the key
    data += (((data>>5)^(data<<2)) + ((data>>3)^(data<<4))) ^ ((0x3c6ef372^data) + (key^data));
    key = (key>>8) | ((key&0xFF)<<24);
    data += (((data>>5)^(data<<2)) + ((data>>3)^(data<<4))) ^ ((0xdaa66d2b^data) + (key^data));
    key = (key>>8) | ((key&0xFF)<<24);
    data += (((data>>5)^(data<<2)) + ((data>>3)^(data<<4))) ^ ((0x78dde6e4^data) + (key^data));
    return data;
  }

  static MultiHashProvider<T> instance;
}
#endregion

#region DateTimeHashProvider
sealed class DateTimeHashProvider : MultiHashProvider<DateTime>
{
  public unsafe override int GetHashCode(int hashFunction, DateTime item)
  {
    if(hashFunction == 0)
    {
      return item.GetHashCode();
    }
    else
    {
      long ticks = item.Ticks; // we use two different hash functions to avoid the result always being zero when ticks == 0
      return (int)(tea32((uint)hashFunction, *(uint*)&ticks) ^ tea32((uint)hashFunction+1, ((uint*)&ticks)[1]));
    }
  }
}
#endregion

#region DecimalHashProvider
sealed class DecimalHashProvider : MultiHashProvider<decimal>
{
  public unsafe override int GetHashCode(int hashFunction, decimal item)
  {
    if(hashFunction == 0)
    {
      return item.GetHashCode();
    }
    else
    {
      // decimals are 16 byte structures, so we can just hash the four uints. we use different hash functions to avoid the result
      // always being zero when the value is zero
      uint* pitem = (uint*)&item;
      return (int)(tea32((uint)hashFunction, *pitem)   ^ tea32((uint)hashFunction, pitem[1]) ^
                   tea32((uint)hashFunction, pitem[2]) ^ tea32((uint)hashFunction+1, pitem[3]));
    }
  }
}
#endregion

#region DoubleHashProvider
sealed class DoubleHashProvider : MultiHashProvider<double>
{
  public unsafe override int GetHashCode(int hashFunction, double item)
  {
    // we want +0 and -0 (IEEE floating point supports both) to hash to the same value, so we'll compare for equality with zero.
    // this also prevents the result from always being zero when the item has all bits zero
    return hashFunction == 0 ? item.GetHashCode() :
           item == 0.0       ? (int)tea32((uint)hashFunction, 0) :
             (int)(tea32((uint)hashFunction, *(uint*)&item) ^ tea32((uint)hashFunction, ((uint*)&item)[1]));
  }
}
#endregion

#region GenericReferenceTypeHashProvider
sealed class GenericReferenceTypeHashProvider<T> : MultiHashProvider<T> where T : class
{
  public override int GetHashCode(int hashFunction, T item)
  {
    uint hash = item == null ? 0 : (uint)item.GetHashCode();
    if(hashFunction != 0) hash = tea32((uint)hashFunction, hash);
    return (int)hash;
  }
}
#endregion

#region GenericValueTypeHashProvider
sealed class GenericValueTypeHashProvider<T> : MultiHashProvider<T> where T : struct
{
  public override int GetHashCode(int hashFunction, T item)
  {
    uint hash = (uint)item.GetHashCode();
    if(hashFunction != 0) hash = tea32((uint)hashFunction, hash);
    return (int)hash;
  }
}
#endregion

#region Int64HashProvider
sealed class Int64HashProvider : MultiHashProvider<long>
{
  public unsafe override int GetHashCode(int hashFunction, long item)
  {
    // we use two different hash functions to avoid the result always being zero when the value is zero
    return hashFunction == 0 ? item.GetHashCode()
                             : (int)(tea32((uint)hashFunction, *(uint*)&item) ^ tea32((uint)hashFunction+1, ((uint*)&item)[1]));
  }
}
#endregion

#region NullableHashProvider
sealed class NullableHashProvider<T> : MultiHashProvider<Nullable<T>> where T : struct
{
  public override int GetHashCode(int hashFunction, Nullable<T> item)
  {
    // since nulls and zeros are both common, we'll hash them to different values. (in particular, for null we'll use 0xe7a03d9a
    // or a hash of that, where 0xe7a03d9a is a randomly-chosen negative int)
    return item.HasValue ?
      hashFunction == 0 ? item.Value.GetHashCode() : MultiHashProvider<T>.Default.GetHashCode(hashFunction, item.Value) :
      (int)(hashFunction == 0 ? 0xe7a03d9a : tea32((uint)hashFunction, 0xe7a03d9a));
  }
}
#endregion

#region StringHashProvider
sealed class StringHashProvider : MultiHashProvider<string>
{
  public unsafe override int GetHashCode(int hashFunction, string item)
  {
    if(hashFunction == 0)
    {
      return item == null ? 0 : item.GetHashCode();
    }
    else if(item == null || item.Length == 0)
    {
      return (int)tea32((uint)hashFunction, 0);
    }
    else
    {
      uint hash = (uint)item.Length; // use the length as the initialization vector
      fixed(char* chars = item)
      {
        // this method essentially encrypts the string using the block cipher in CBC mode and returns the last block
        for(int i=0, chunks=item.Length/2; i<chunks; i++) hash = tea32((uint)hashFunction, ((uint*)chars)[i] ^ hash);
        if((item.Length & 1) != 0) hash = tea32((uint)hashFunction, (uint)chars[item.Length-1] ^ hash);
      }
      return (int)hash;
    }
  }
}
#endregion

#region Uint64HashProvider
sealed class Uint64HashProvider : MultiHashProvider<ulong>
{
  public unsafe override int GetHashCode(int hashFunction, ulong item)
  {
    // we use two different hash functions to avoid the result always being zero when the value is zero
    return hashFunction == 0 ? item.GetHashCode()
                             : (int)(tea32((uint)hashFunction, *(uint*)&item) ^ tea32((uint)hashFunction+1, ((uint*)&item)[1]));
  }
}
#endregion

} // namespace AdamMil.Collections
