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
using System.Collections.Generic;

namespace AdamMil.Utilities
{

/// <summary>Provides very general utilities to aid .NET development.</summary>
public static class Utility
{
  /// <summary>Disposes the given object if it's not null.</summary>
  public static void Dispose(IDisposable obj)
  {
    if(obj != null) obj.Dispose();
  }

  /// <summary>Disposes the given object if it's disposable and not null.</summary>
  public static void Dispose(object obj)
  {
    IDisposable disposable = obj as IDisposable;
    if(disposable != null) disposable.Dispose();
  }

  /// <summary>Disposes the given object if it's not null, and sets it to null.</summary>
  public static void Dispose<T>(ref T obj) where T : IDisposable
  {
    if(obj != null)
    {
      obj.Dispose();
      obj = default(T);
    }
  }

  /// <summary>Disposes each object in the given array of objects if the array is not null, and sets each element to null.</summary>
  public static void DisposeAll<T>(T[] array) where T : class, IDisposable
  {
    if(array != null)
    {
      for(int i=0; i<array.Length; i++)
      {
        T value = array[i];
        if(value != null)
        {
          value.Dispose();
          array[i] = null;
        }
      }
    }
  }

  /// <summary>Disposes each object in the given collection of objects, if the collection is not null.</summary>
  public static void DisposeAll<T>(IEnumerable<T> items) where T : class, IDisposable
  {
    if(items != null)
    {
      foreach(T value in items)
      {
        if(value != null) value.Dispose();
      }
    }
  }

  /// <summary>Enlarges the given array to precisely the given length if it's not already of the given length. The array cannot be shrunk
  /// by this method, and attempting to do so will cause an exception to be thrown.
  /// </summary>
  public static T[] EnlargeArray<T>(T[] array, int newLength)
  {
    int currentLength = array == null ? 0 : array.Length;
    if(newLength <= currentLength)
    {
      if(newLength < currentLength) throw new ArgumentOutOfRangeException();
    }
    else
    {
      T[] newArray = new T[newLength];
      if(array != null) Array.Copy(array, newArray, currentLength);
      array = newArray;
    }
    return array;
  }

  /// <summary>Enlarges the given array if it's too small to accommodate its current size plus the new elements. The new array is
  /// returned. It is acceptable if <paramref name="array"/> is null or <paramref name="newElements"/> is negative, indicating that
  /// elements are being removed from the array. If the array is enlarged, it may be made larger than strictly necessary to hold the
  /// new elements, in order to accomodate future growth.
  /// </summary>
  public static T[] EnlargeArray<T>(T[] array, int currentSize, int newElements)
  {
    int capacity = array == null ? 0 : array.Length;
    if((uint)currentSize > (uint)capacity) throw new ArgumentOutOfRangeException();

    int newSize = currentSize + newElements;
    if(capacity < newSize)
    {
      T[] newArray = new T[Math.Max(capacity*2, newSize)];
      if(array != null) Array.Copy(array, newArray, currentSize);
      array = newArray;
    }
    return array;
  }

  /// <summary>Gets the enum values of the given type.</summary>
  public static T[] GetEnumValues<T>()
  {
    return (T[])Enum.GetValues(typeof(T));
  }

  /// <summary>Parses the string representation of an enumeration name or value.</summary>
  public static T ParseEnum<T>(string value)
  {
    return (T)Enum.Parse(typeof(T), value);
  }

  /// <summary>Parses the string representation of an enumeration name or value.</summary>
  public static T ParseEnum<T>(string value, bool ignoreCase)
  {
    return (T)Enum.Parse(typeof(T), value, ignoreCase);
  }

  /// <summary>Rounds the given value up to a power of two. If it is already a power of two, it is returned unchanged. If it is negative
  /// or zero, zero is returned.
  /// </summary>
  public static int RoundUpToPowerOfTwo(int value)
  {
    return value <= 0 ? 0 : (int)RoundUpToPowerOfTwo((uint)value);
  }

  /// <summary>Rounds the given value up to a power of two. If it is already a power of two, or zero, it is returned unchanged.</summary>
  [CLSCompliant(false)]
  public static uint RoundUpToPowerOfTwo(uint value)
  {
    value--;
    value |= (value >> 1);
    value |= (value >> 2);
    value |= (value >> 4);
    value |= (value >> 8);
    value |= (value >> 16);
    return value+1;
  }

  /// <summary>Rounds the given value up to a power of two. If it is already a power of two, it is returned unchanged. If it is negative
  /// or zero, zero is returned.
  /// </summary>
  public static long RoundUpToPowerOfTwo(long value)
  {
    return value <= 0 ? 0 : (long)RoundUpToPowerOfTwo((ulong)value);
  }

  /// <summary>Rounds the given value up to a power of two. If it is already a power of two, or zero, it is returned unchanged.</summary>
  [CLSCompliant(false)]
  public static ulong RoundUpToPowerOfTwo(ulong value)
  {
    value--;
    value |= (value >> 1);
    value |= (value >> 2);
    value |= (value >> 4);
    value |= (value >> 8);
    value |= (value >> 16);
    value |= (value >> 32);
    return value+1;
  }

  /// <summary>Swaps two variables.</summary>
  public static void Swap<T>(ref T a, ref T b)
  {
    T temp = a;
    a = b;
    b = temp;
  }

  /// <summary>Validates that the given range exists within a list of the given size.</summary>
  public static void ValidateRange(int listSize, int index, int count)
  {
    if((index|count) < 0 || (uint)(index + count) > (uint)listSize) throw new ArgumentOutOfRangeException();
  }

  /// <summary>Validates that the given list is not null, and the given range exists within the list.</summary>
  public static void ValidateRange(System.Collections.IList list, int index, int count)
  {
    if(list == null) throw new ArgumentNullException();
    if((index|count) < 0 || (uint)(index + count) > (uint)list.Count) throw new ArgumentOutOfRangeException();
  }

  /// <summary>Validates that the given array is not null, and the given range exists within the array.</summary>
  public static void ValidateRange(Array array, int index, int count)
  {
    if(array == null) throw new ArgumentNullException();
    if((index|count) < 0 || (uint)(index + count) > (uint)array.Length) throw new ArgumentOutOfRangeException();
  }

  /// <summary>Validates that the given string is not null, and the given range exists within the string.</summary>
  public static void ValidateRange(string str, int index, int count)
  {
    if(str == null) throw new ArgumentNullException();
    if((index|count) < 0 || (uint)(index + count) > (uint)str.Length) throw new ArgumentOutOfRangeException();
  }
}

} // namespace AdamMil.Utilities
