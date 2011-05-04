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

  /// <summary>Enlarges the given array if it's too small to accommodate its current size plus the new elements.</summary>
  public static T[] EnlargeArray<T>(T[] array, int currentSize, int newElements)
  {
    if(currentSize < 0 || newElements < 0 || array != null && currentSize > array.Length)
    {
      throw new ArgumentOutOfRangeException();
    }

    int currentCapacity = array == null ? 0 : array.Length, newSize = currentSize + newElements;
    if(currentCapacity < newSize)
    {
      if(newSize < 4) newSize = 4;
      T[] newArray = new T[Math.Max(currentCapacity*2, newSize)];
      if(currentSize != 0) Array.Copy(array, newArray, currentSize);
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
    if(index < 0 || count < 0 || index + count > listSize) throw new ArgumentOutOfRangeException();
  }

  /// <summary>Validates that the given list is not null, and the given range exists within the list.</summary>
  public static void ValidateRange(System.Collections.IList list, int index, int count)
  {
    if(list == null) throw new ArgumentNullException();
    if(index < 0 || count < 0 || index + count > list.Count) throw new ArgumentOutOfRangeException();
  }

  /// <summary>Validates that the given array is not null, and the given range exists within the array.</summary>
  public static void ValidateRange(Array array, int index, int count)
  {
    if(array == null) throw new ArgumentNullException();
    if(index < 0 || count < 0 || index + count > array.Length) throw new ArgumentOutOfRangeException();
  }

  /// <summary>Validates that the given string is not null, and the given range exists within the string.</summary>
  public static void ValidateRange(string str, int index, int count)
  {
    if(str == null) throw new ArgumentNullException();
    if(index < 0 || count < 0 || index + count > str.Length) throw new ArgumentOutOfRangeException();
  }
}

} // namespace AdamMil.Utilities
