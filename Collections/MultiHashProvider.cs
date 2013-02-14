/*
AdamMil.Collections is a library that provides useful collection classes for
the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2013 Adam Milazzo

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
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AdamMil.Collections
{

// TODO: it would be good to implement hash providers optimized for 64-bit platforms (e.g. by using 64-bit block ciphers). XXTEA would work
// TODO: run HashHelper.Cipher through the NIST suite to make sure it's producing sufficiently random output

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

#region HashHelper
// sadly, .NET duplicates the implementation of static methods inside generic types when the type parameter changes, even when
// the static methods don't depend on the type parameter at all. so we'll put all of these static methods in a separate class
// rather than inside MultiHashProvider, where they really belong
static class HashHelper
{
  // this is a weak block cipher with a 32-bit key and a 32-bit output based on a 16:16 balanced Feistel network. it should be clear from
  // the small key size and poor design that it's insecure, but it should serve its purpose of giving us a pseudorandom permutation of the
  // output space (i.e. of the 32-bit integers) with the key as the seed. the function is near impossible to read, but it works!
  internal static uint Cipher(uint key, uint data)
  {
    uint R = (data^key) & 0xFFFF, L = (data>>16) ^ (((((R>>5)^(R<<2)) + ((R>>3)^(R<<4))) ^ ((R^0x79b9) + R)) & 0xFFFF);
    key = (key>>3) | (key<<29);
    R ^= ((((L>>5)^(L<<2)) + ((L>>3)^(L<<4))) ^ ((L^0xf372) + (L^key))) & 0xFFFF;
    return ((L ^ ((((R>>5)^(R<<2)) + ((R>>3)^(R<<4))) ^ ((R^0x6d2b) + (R^((key>>3)|(key<<29)))))) << 16) | R;
  }

  internal static unsafe int Hash8(int hashFunction, void* data)
  {
    // we use two different hash functions to prevent the output from always being zero when the input bytes are all zero
    return (int)(Cipher((uint)hashFunction, *(uint*)data) ^ Cipher((uint)hashFunction+1, ((uint*)data)[1]));
  }

  internal static unsafe int Hash16(int hashFunction, void* data)
  {
    // we use multiple hash functions to prevent the output from always being zero when the input bytes are all zero (two would suffice,
    // but we might as well use more to increase the quality of the hash)
    return (int)(Cipher((uint)hashFunction,   *(uint*)data)     ^ Cipher((uint)hashFunction+1, ((uint*)data)[1]) ^
                 Cipher((uint)hashFunction+2, ((uint*)data)[2]) ^ Cipher((uint)hashFunction+3, ((uint*)data)[3]));
  }

  internal static unsafe int HashBytes(int hashFunction, void* data, int length)
  {
    System.Diagnostics.Debug.Assert(length > 0);
    // incorporate the length, which is assumed to be non-zero, into the initialization vector
    uint hash = (uint)length ^ 0x9e3eff0b ^ (uint)hashFunction;
    // this method essentially encrypts the bytes using the block cipher in CBC mode and returns the last block
    for(int i=0, chunks=length/4; i<chunks; i++) hash = Cipher((uint)hashFunction, ((uint*)data)[i] ^ hash);
    if((length&3) != 0)
    {
      data = (byte*)data + length/4; // advance to the remaining bytes
      uint lastChunk = 0;
      if((length & 2) != 0)
      {
        lastChunk = *(ushort*)data;
        data = (byte*)data + 2;
      }
      if((length & 1) != 0) lastChunk = (lastChunk<<8) | *(byte*)data;
      hash = Cipher((uint)hashFunction, lastChunk ^ hash);
    }
    return (int)hash;
  }


  /// <summary>Gets whether the type is a simple value type with no reference types anywhere in its object graph.</summary>
  internal static bool IsBlittable(Type type)
  {
    return IsBlittable(type, null);
  }

  static bool IsBlittable(Type type, HashSet<Type> typesSeen)
  {
    if(!type.IsValueType) return false;
    else if(type.IsPrimitive || type.IsEnum) return true;

    foreach(FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
    {
      Type fieldType = field.FieldType;
      // reference types and MarshalAs attributes make a type unblittable, the latter because it may cause Marshal.SizeOf() to
      // give us the wrong value
      if(!fieldType.IsValueType || Attribute.GetCustomAttribute(field, typeof(MarshalAsAttribute), false) != null)
      {
        return false;
      }
      else if(!fieldType.IsPrimitive && !fieldType.IsEnum && (typesSeen == null || !typesSeen.Contains(fieldType)))
      {
        if(typesSeen == null) typesSeen = new HashSet<Type>();
        typesSeen.Add(fieldType);
        if(!IsBlittable(fieldType, typesSeen)) return false;
      }
    }

    return true;
  }
}
#endregion

#region MultiHashProvider
/// <summary>Provides implementations of <see cref="IMultiHashProvider{T}"/> suitable for most types. Use <see cref="Default"/>
/// to retrieve a suitable hash provider for a given type.
/// </summary>
/// <remarks>
/// This implementation has good-quality hashes for all integer types, <see cref="string"/>, <see cref="Decimal"/>,
/// <see cref="Single"/>, <see cref="Double"/>, <see cref="Char"/>, <see cref="DateTime"/>, and <see cref="Guid"/> as well as
/// nullable versions of those.
/// For other types, this implementation works by taking taking the standard .NET hash value (returned by
/// <see cref="object.GetHashCode"/>) and using it as an index into a number of pseudorandom permutations of the integers,
/// corresponding to the different hash functions. The problem with this approach -- and it is a rather severe problem in
/// general -- is that if two objects collide using one hash function, they will collide using every hash function. This may or
/// may not be a problem in your particular use case, but if it is, you'll need to create custom hash providers for those types.
/// <para>
/// Note that arrays are not treated specially. They are hashed like any generic object, meaning that the contents of the array
/// are not considered by the hash. If you want the contents to be hashed, you can use <see cref="ArrayHashProvider{T}"/>.
/// </para>
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
  public static MultiHashProvider<T> Default
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
            if(typeof(T) == typeof(Guid))
            {
              provider = new GuidHashProvider();
            }
            else if(typeof(T).IsGenericType && !typeof(T).ContainsGenericParameters && // if T is some kind of Nullable<U>...
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
      long ticks = item.Ticks;
      return HashHelper.Hash8(hashFunction, &ticks);
    }
  }
}
#endregion

#region DecimalHashProvider
sealed class DecimalHashProvider : MultiHashProvider<decimal>
{
  public DecimalHashProvider()
  {
    // decimals are 16 byte structures, but we'll check this because it may be implementation-specific
    System.Diagnostics.Debug.Assert(sizeof(decimal) == 16);
  }

  public unsafe override int GetHashCode(int hashFunction, decimal item)
  {
    return hashFunction == 0 ? item.GetHashCode() : HashHelper.Hash16(hashFunction, &item);
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
           item == 0.0       ? (int)HashHelper.Cipher((uint)hashFunction, 0) :
             (int)(HashHelper.Cipher((uint)hashFunction, *(uint*)&item) ^
                   HashHelper.Cipher((uint)hashFunction, ((uint*)&item)[1]));
  }
}
#endregion

#region GenericReferenceTypeHashProvider
sealed class GenericReferenceTypeHashProvider<T> : MultiHashProvider<T> where T : class
{
  public override int GetHashCode(int hashFunction, T item)
  {
    uint hash = item == null ? 0 : (uint)item.GetHashCode();
    if(hashFunction != 0) hash = HashHelper.Cipher((uint)hashFunction, hash);
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
    if(hashFunction != 0) hash = HashHelper.Cipher((uint)hashFunction, hash);
    return (int)hash;
  }
}
#endregion

#region GuidHashProvider
sealed class GuidHashProvider : MultiHashProvider<Guid>
{
  public GuidHashProvider()
  {
    // guids are 16 byte structures, but we'll check this because it may be implementation-specific. we use IsBlittable() to
    // ensure that Marshal.SizeOf() has given us the right answer (i.e. that the managed size equals the marshalled size)
    System.Diagnostics.Debug.Assert(Marshal.SizeOf(typeof(Guid)) == 16 && HashHelper.IsBlittable(typeof(Guid)));
  }

  public unsafe override int GetHashCode(int hashFunction, Guid item)
  {
    return hashFunction == 0 ? item.GetHashCode() : HashHelper.Hash16(hashFunction, &item);
  }
}
#endregion

#region Int64HashProvider
sealed class Int64HashProvider : MultiHashProvider<long>
{
  public unsafe override int GetHashCode(int hashFunction, long item)
  {
    return hashFunction == 0 ? item.GetHashCode() : HashHelper.Hash8(hashFunction, &item);
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
    // TODO: why negative?
    return item.HasValue ?
      hashFunction == 0 ? item.Value.GetHashCode() : MultiHashProvider<T>.Default.GetHashCode(hashFunction, item.Value) :
      (int)(hashFunction == 0 ? 0xe7a03d9a : HashHelper.Cipher((uint)hashFunction, 0xe7a03d9a));
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
    else if(string.IsNullOrEmpty(item))
    {
      // use an arbitrary number for empty strings to differentiate them from null strings, as both are common cases
      return (int)HashHelper.Cipher((uint)hashFunction, item == null ? 0 : 0x8E5211CC);
    }
    else
    {
      fixed(char* chars = item) return HashHelper.HashBytes(hashFunction, chars, item.Length*sizeof(char));
    }
  }
}
#endregion

#region Uint64HashProvider
sealed class Uint64HashProvider : MultiHashProvider<ulong>
{
  public unsafe override int GetHashCode(int hashFunction, ulong item)
  {
    return hashFunction == 0 ? item.GetHashCode() : HashHelper.Hash8(hashFunction, &item);
  }
}
#endregion

#region ArrayHashProvider
/// <summary>Provides an implementation of <see cref="IMultiHashProvider{T}"/> specifically designed to hash arrays. In most
/// cases, you should use the generic version, <see cref="ArrayHashProvider{T}"/>, which provides static type safety, instead.
/// This non-generic version doesn't provide static type safety, but has the benefit of supporting multidimensional arrays.
/// Arrays of arrays still have no special handling -- the contents of inner arrays will not be hashed.
/// </summary>
/// <remarks>
/// This implementation has a specialized hash for arrays of blittable value types (value types containing no pointers to
/// reference types in their object graphs and having no special marshalling requirements). For arrays of other types,
/// <see cref="MultiHashProvider{E}"/> is used to hash each element. <see cref="MultiHashProvider{E}"/> only works well for
/// certain types; see its documentation for details. Note that large arrays may be quite slow to hash, so you may wish to
/// memoize the hash values.
/// </remarks>
public sealed class ArrayHashProvider : MultiHashProvider<Array>
{
  /// <summary>Initializes a new <see cref="ArrayHashProvider"/> suitable for hashing arrays of the given type.</summary>
  public ArrayHashProvider(Type arrayType)
  {
    if(arrayType == null) throw new ArgumentNullException();
    if(!arrayType.IsArray) throw new ArgumentException(arrayType.FullName + " is not an array type.");
    Type elementType = arrayType.GetElementType();
    this.arrayType   = arrayType;
    this.elementSize = HashHelper.IsBlittable(elementType) ? Marshal.SizeOf(elementType) : 0;
  }

  /// <include file="documentation.xml" path="/Collections/MultiHashProvider/GetHashCode/*"/>
  public override int GetHashCode(int hashFunction, Array item)
  {
    if(item != null && item.GetType() != arrayType) throw new ArgumentException();
    return HashCore(hashFunction, item, elementSize);
  }

  readonly Type arrayType;
  /// <summary>The size of each element for blittable types, or zero for non-blittable types.</summary>
  readonly int elementSize;

  internal static unsafe int HashCore(int hashFunction, Array array, int elementSize)
  {
    int hash;
    if(array == null || array.Length == 0)
    {
      hash = (int)HashHelper.Cipher((uint)hashFunction, 0);
    }
    else if(elementSize != 0) // if it's an array of blittable types...
    {
      GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned); // pin the array so we can get a pointer to the raw bytes
      try                                                           // NOTE: GCHandle is relatively slow...
      {
        hash = HashHelper.HashBytes(hashFunction, Marshal.UnsafeAddrOfPinnedArrayElement(array, 0).ToPointer(),
                                    array.Length * elementSize);
      }
      finally { handle.Free(); }
    }
    else
    {
      hash = 0;
      string[] strings = array as string[];
      if(strings != null) // we have to special-case the non-blittable types that have custom MultiHashProvider implementations,
      {                   // because the generic code below will only use the generic MultiHashProvider
        for(int i=0; i<strings.Length; i++) hash ^= MultiHashProvider<string>.Default.GetHashCode(hashFunction, strings[i]);
      }
      else
      {
        for(int i=0; i<array.Length; i++) hash ^= MultiHashProvider<object>.Default.GetHashCode(hashFunction, array.GetValue(i));
      }
    }
    return hash;
  }
}
#endregion

#region ArrayHashProvider<T>
/// <summary>Provides an implementation of <see cref="IMultiHashProvider{T}"/> specifically designed to hash arrays. Use
/// <see cref="Default"/> to retrieve a suitable hash provider for a given type. Note that you must specify the element type when
/// doing so. For example, use <c>ArrayHashProvider&lt;byte&gt;.Default</c> rather than
/// <c>ArrayHashProvider&lt;byte[]&gt;.Default</c>. The latter would represent arrays of arrays of bytes. This class has no
/// special handling for arrays of arrays -- the content of inner arrays will not be hashed. Multidimensional arrays are also not
/// supported, although the non-generic <see cref="ArrayHashProvider"/> class supports them.
/// </summary>
/// <remarks>
/// This implementation has a specialized hash for arrays of blittable value types (value types containing no pointers to
/// reference types in their object graphs and having no special marshalling requirements). For arrays of other types,
/// <see cref="MultiHashProvider{E}"/> is used to hash each element. <see cref="MultiHashProvider{E}"/> only works well for
/// certain types; see its documentation for details. Note that large arrays may be quite slow to hash, so you may wish to
/// memoize the hash values.
/// </remarks>
public sealed class ArrayHashProvider<T> : MultiHashProvider<T[]>
{
  ArrayHashProvider()
  {
    Type elementType = typeof(T).GetElementType();
    elementSize = HashHelper.IsBlittable(elementType) ? Marshal.SizeOf(elementType) : 0;
  }

  /// <include file="documentation.xml" path="/Collections/MultiHashProvider/GetHashCode/*"/>
  public unsafe override int GetHashCode(int hashFunction, T[] item)
  {
    return ArrayHashProvider.HashCore(hashFunction, item, elementSize);
  }

  /// <summary>Gets the default <see cref="MultiHashProvider{T}"/> instance.</summary>
  public new static ArrayHashProvider<T> Default
  {
    get
    {
      if(instance == null)
      {
        if(!typeof(T).IsArray) throw new InvalidOperationException(typeof(T).FullName + " is not an array type.");
        instance = new ArrayHashProvider<T>();
      }
      return instance;
    }
  }

  /// <summary>The size of each element for blittable types, or zero for non-blittable types.</summary>
  readonly int elementSize;

  static ArrayHashProvider<T> instance;
}
#endregion

} // namespace AdamMil.Collections
