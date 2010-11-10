/*
AdamMil.Collections is a library that provides useful collection classes for
the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2010 Adam Milazzo

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

namespace AdamMil.Collections
{

/// <summary>Implements useful extensions for .NET built-in collections.</summary>
public static partial class CollectionExtensions
{
  /// <summary>Returns the last item in the array.</summary>
  public static T Last<T>(this T[] array)
  {
    if(array == null) throw new ArgumentNullException();
    if(array.Length == 0) throw new ArgumentException("The collection is empty.");
    return array[array.Length-1];
  }

  /// <summary>Returns a random item from the list.</summary>
  public static T SelectRandom<T>(this IList<T> list, Random random)
  {
    if(list == null || random == null) throw new ArgumentNullException();
    if(list.Count == 0) throw new ArgumentException("The collection is empty.");
    return list[random.Next(list.Count)];
  }
}

} // namespace AdamMil.Collections