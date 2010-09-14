/*
AdamMil.Utilities is a library providing generally useful utilities for
.NET development.

http://www.adammil.net/
Copyright (C) 2010 Adam Milazzo

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

  /// <summary>Enlarges the given array if it's too small to accommodate its current size and the new elements.</summary>
  public static void EnlargeArray<T>(ref T[] array, int currentSize, int newElements)
  {
    if(array == null) throw new ArgumentNullException();
    if(currentSize < 0 || currentSize > array.Length || newElements < 0) throw new ArgumentOutOfRangeException();

    int newSize = currentSize + newElements;
    if(array.Length < newSize)
    {
      T[] newArray = new T[Math.Max(array.Length*2, newSize)];
      Array.Copy(array, newArray, currentSize);
      array = newArray;
    }
  }

  /// <summary>Validates that the given array is not null, and the given range exists within the array.</summary>
  public static void ValidateRange(Array array, int index, int count)
  {
    if(array == null) throw new ArgumentNullException();
    if(index < 0 || count < 0 || index + count > array.Length) throw new ArgumentOutOfRangeException();
  }
}

} // namespace AdamMil.Utilities
