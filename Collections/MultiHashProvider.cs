/*
AdamMil.Collections is a library that provides useful collection classes for
the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2016 Adam Milazzo

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
using AdamMil.Utilities;

namespace AdamMil.Collections
{

// TODO: it would be good to implement hash providers optimized for 64-bit platforms

#region IMultiHashable
/// <summary>An interface that allows a type to be hashed in multiple ways.</summary>
/// <remarks>This interface is used by data structures that need to hash items using multiple hash functions. For instance, a
/// data structure may desire to obtain two hashes for each item and combine the hashes somehow.
/// </remarks>
public interface IMultiHashable
{
  /// <summary>Gets a hash code for this value.</summary>
  /// <param name="hashFunction">The number of the hash function to use. Each hash function should hash the object in a different way.
  /// (Different hash functions may produce the same value sometimes, but that should be a rare coincidence.) This value may be any
  /// number from 0 to <see cref="int.MaxValue"/>.
  /// </param>
  /// <returns>A hash code. Negative hash codes are supported.</returns>
  int GetHashCode(int hashFunction);
}
#endregion

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
  // these methods are based off the hashing code in http://burtleburtle.net/bob/c/lookup3.c. the result is slightly less uniform than the
  // previous block cipher method i was using, but faster -- slightly faster for 4-byte inputs and much faster for large byte strings.
  // the block cipher also did poorly with certain input values, and hopefully this exhibits better worst-case behavior
  internal static int Hash4(int hashFunction, uint data)
  {
    uint a, b, c;
    a = c = 0x9e3eff0b + (uint)hashFunction;
    b = c + data;
    c = (c^b) - ((b<<14) | (b>>18));
    a = (a^c) - ((c<<11) | (c>>21));
    b = (b^a) - ((a<<25) | (a>>7));
    c = (c^b) - ((b<<16) | (b>>16));
    a = (a^c) - ((c<<4)  | (c>>28));
    b = (b^a) - ((a<<14) | (a>>18));
    return (int)((c^b) - ((b<<24) | (b>>8)));
  }

  internal static unsafe int Hash8(int hashFunction, void* data)
  {
    uint a = 0x9e3eff0b + (uint)hashFunction, b = a + *(uint*)data, c = a + *((uint*)data+1);
    a = (a^c) - ((c<<14) | (c>>18));
    b = (b^a) - ((a<<11) | (a>>21));
    c = (c^b) - ((b<<25) | (b>>7));
    a = (a^c) - ((c<<16) | (c>>16));
    b = (b^a) - ((a<<4)  | (a>>28));
    c = (c^b) - ((b<<14) | (b>>18));
    return (int)((a^c) - ((c<<24) | (c>>8)));
  }

  internal static unsafe int Hash16(int hashFunction, void* data)
  {
    uint a = 0x9e3eff0b + (uint)hashFunction, b = a + *(uint*)data, c = a + *((uint*)data+1);
    a += *((uint*)data+2);
    b -= a; b ^= (a<<4)  | (a>>28); a += c;
    c -= b; c ^= (b<<6)  | (b>>26); b += a;
    a -= c; a ^= (c<<8)  | (c>>24); c += b;
    b -= a; b ^= (a<<16) | (a>>16); a += c;
    c -= b; c ^= (b<<19) | (b>>13); b += a;
    a -= c; a ^= (c<<4)  | (c>>28); c += b;

    b += *((uint*)data+3);
    a = (a^c) - ((c<<14) | (c>>18));
    b = (b^a) - ((a<<11) | (a>>21));
    c = (c^b) - ((b<<25) | (b>>7));
    a = (a^c) - ((c<<16) | (c>>16));
    b = (b^a) - ((a<<4)  | (a>>28));
    c = (c^b) - ((b<<14) | (b>>18));
    return (int)((a^c) - ((c<<24) | (c>>8)));
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
/// <summary>Provides implementations of <see cref="IMultiHashProvider{T}"/> suitable for many types. Use <see cref="Default"/>
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
          case TypeCode.UInt64: provider = new UInt64HashProvider(); break;
          case TypeCode.Object:
            if(typeof(T) == typeof(Guid))
            {
              provider = new GuidHashProvider();
            }
            else if(typeof(IMultiHashable).IsAssignableFrom(typeof(T))) // if T implements IMultiHashable...
            {
              provider = Activator.CreateInstance(typeof(MultiHashableHashProvider<>).MakeGenericType(typeof(T)));
            }
            else if(typeof(T).IsGenericType && !typeof(T).ContainsGenericParameters && // if T is some kind of Nullable<U>...
                    typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>))
            {
              provider = Activator.CreateInstance(typeof(NullableHashProvider<>).MakeGenericType(typeof(T).GetGenericArguments()[0]));
              break;
            }
            goto default;
          default:
          {
            // this branch also works for primitive value types <= 32 bits since their hashes can't collide without them being equal
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

#region ByteArrayHashProvider
sealed class ByteArrayHashProvider : MultiHashProvider<byte[]>
{
  public unsafe override int GetHashCode(int hashFunction, byte[] array)
  {
    return BinaryUtility.Hash(hashFunction, array);
  }
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
           item == 0.0       ? HashHelper.Hash4(hashFunction, 0) : HashHelper.Hash8(hashFunction, &item);
  }
}
#endregion

#region GenericReferenceTypeHashProvider
sealed class GenericReferenceTypeHashProvider<T> : MultiHashProvider<T> where T : class
{
  public override int GetHashCode(int hashFunction, T item)
  {
    int hash = item == null ? 0 : item.GetHashCode();
    if(hashFunction != 0) hash = HashHelper.Hash4(hashFunction, (uint)hash);
    return hash;
  }
}
#endregion

#region GenericValueTypeHashProvider
sealed class GenericValueTypeHashProvider<T> : MultiHashProvider<T> where T : struct
{
  public override int GetHashCode(int hashFunction, T item)
  {
    int hash = item.GetHashCode();
    if(hashFunction != 0) hash = HashHelper.Hash4(hashFunction, (uint)hash);
    return hash;
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

#region MultiHashableHashProvider
sealed class MultiHashableHashProvider<T> : IMultiHashProvider<T> where T : IMultiHashable
{
  public int HashCount
  {
    get { return int.MaxValue; }
  }

  public int GetHashCode(int hashFunction, T item)
  {
    return item != null ? item.GetHashCode(hashFunction) : hashFunction == 0 ? 0 : HashHelper.Hash4(hashFunction, 0);
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
      hashFunction == 0 ? item.GetValueOrDefault().GetHashCode() :
                          MultiHashProvider<T>.Default.GetHashCode(hashFunction, item.GetValueOrDefault()) :
      hashFunction == 0 ? unchecked((int)0xe7a03d9a) : HashHelper.Hash4(hashFunction, 0xe7a03d9a);
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
      // use an arbitrary non-zero value for empty strings to differentiate them from null strings, as both are common cases
      return HashHelper.Hash4(hashFunction, item == null ? 0 : 0x8E5211CC);
    }
    else
    {
      fixed(char* chars = item) return BinaryUtility.Hash(hashFunction, chars, item.Length*sizeof(char));
    }
  }
}
#endregion

#region UInt64HashProvider
sealed class UInt64HashProvider : MultiHashProvider<ulong>
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
      hash = HashHelper.Hash4(hashFunction, 0);
    }
    else if(elementSize != 0) // if it's an array of blittable types...
    {
      GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned); // pin the array so we can get a pointer to the raw bytes
      try                                                           // NOTE: GCHandle is relatively slow...
      {
        hash = BinaryUtility.Hash(hashFunction, Marshal.UnsafeAddrOfPinnedArrayElement(array, 0).ToPointer(),
                                  array.Length * elementSize);
      }
      finally { handle.Free(); }
    }
    else
    {
      hash = 0;
      string[] strings = array as string[];
      if(strings != null) // we have to special-case the non-blittable types that have custom MultiHashProvider implementations, because
      {                   // the generic code below will only use the generic MultiHashProvider. this is string[] and IMultiHashable[]
        for(int i=0; i<strings.Length; i++) hash ^= MultiHashProvider<string>.Default.GetHashCode(hashFunction, strings[i]);
      }
      else
      {
        IMultiHashable[] hashables = array as IMultiHashable[];
        if(hashables != null)
        {
          for(int i=0; i<hashables.Length; i++) hash ^= hashables[i].GetHashCode(hashFunction);
        }
        else
        {
          for(int i=0; i<array.Length; i++) hash ^= MultiHashProvider<object>.Default.GetHashCode(hashFunction, array.GetValue(i));
        }
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
  public new static MultiHashProvider<T[]> Default
  {
    get
    {
      if(instance == null)
      {
        if(!typeof(T).IsArray) throw new InvalidOperationException(typeof(T).FullName + " is not an array type.");
        instance = typeof(T) == typeof(byte) ? (MultiHashProvider<T[]>)(object)new ByteArrayHashProvider() : new ArrayHashProvider<T>();
      }
      return instance;
    }
  }

  /// <summary>The size of each element for blittable types, or zero for non-blittable types.</summary>
  readonly int elementSize;

  static MultiHashProvider<T[]> instance;
}
#endregion

} // namespace AdamMil.Collections
