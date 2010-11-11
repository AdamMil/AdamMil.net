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
using System.Globalization;

namespace AdamMil.Utilities
{

public static class TypeUtility
{
  /// <summary>Attempts to convert the given value into the given destination type using the invariant culture.</summary>
  public static object ChangeType(Type destinationType, object value)
  {
    return ChangeType(destinationType, value, CultureInfo.InvariantCulture);
  }

  /// <summary>Attempts to convert the given value into the given destination type using the given culture.</summary>
  public static object ChangeType(Type destinationType, object value, IFormatProvider culture)
  {
    if(destinationType == null) throw new ArgumentNullException();

    Type srcType = value == null ? null : value.GetType();
    // if the types match, or we're not simply dealing with a null value and a reference type...
    if(destinationType != srcType && (value != null || destinationType.IsValueType))
    {
      // we need special handling for Nullable<T> types. if the
      // destination type is a closed generic type, it might be Nullable<T>
      if(destinationType.IsGenericType && !destinationType.ContainsGenericParameters)
      {
        Type genericType = destinationType.GetGenericTypeDefinition(); // get the generic type
        if(genericType == typeof(Nullable<>)) // if the generic type is Nullable<T>
        {
          if(value == null) // if the value is null, we return null
          {
            return null;
          }
          else
          {
            // otherwise, get the type T in Nullable<T>
            destinationType = destinationType.GetGenericArguments()[0];
            // the below code will try to convert the value to type T
          }
        }
      }

      if(destinationType == typeof(Guid)) // Convert.ChangeType() will fail to convert a string to a Guid, so we'll do it ourselves
      {
        string stringValue = value as string;
        if(string.IsNullOrEmpty(stringValue)) throw new InvalidCastException();
        else value = new Guid(stringValue);
      }
      else if(destinationType.IsEnum) // Convert.ChangeType() also fails to handle enum types
      {
        string stringValue = value as string;
        if(string.IsNullOrEmpty(stringValue)) throw new InvalidCastException();
        try { value = Enum.Parse(destinationType, stringValue, true); }
        catch(ArgumentException) { throw new InvalidCastException(); }
      }
      else
      {
        value = Convert.ChangeType(value, destinationType, culture);
      }
    }

    return value;
  }
}

} // namespace AdamMil.Utilities
