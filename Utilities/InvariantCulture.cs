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
using System.Globalization;

namespace AdamMil.Utilities
{

#region InvariantCultureUtility
/// <summary>Provides convenient methods for quickly parsing values from strings rendered in the invariant culture.</summary>
public static class InvariantCultureUtility
{
  /// <summary>Tries to parse a double-precision floating point number out of a string using the <see cref="CultureInfo.InvariantCulture"/>.
  /// The method accepts null strings and leading and trailing whitespace.
  /// </summary>
  public static bool TryParse(string str, out double value)
  {
    return double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
  }

  /// <summary>Tries to parse a single-precision floating point number out of a string using the <see cref="CultureInfo.InvariantCulture"/>.
  /// The method accepts null strings and leading and trailing whitespace.
  /// </summary>
  public static bool TryParse(string str, out float value)
  {
    return float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
  }

  /// <summary>Tries to parse a 32-bit signed integer out of a string using the same format as the
  /// <see cref="CultureInfo.InvariantCulture"/>. The method accepts null strings and leading and trailing whitespace.
  /// </summary>
  public static bool TryParse(string str, out int value)
  {
    if(str == null)
    {
      value = 0;
      return false;
    }
    else
    {
      return TryParse(str, 0, str.Length, out value);
    }
  }

  /// <summary>Tries to parse a 32-bit unsigned integer out of a string using the same format as the
  /// <see cref="CultureInfo.InvariantCulture"/>. The method accepts null strings and leading and trailing whitespace.
  /// </summary>
  [CLSCompliant(false)]
  public static bool TryParse(string str, out uint value)
  {
    if(str == null)
    {
      value = 0;
      return false;
    }
    else
    {
      return TryParse(str, 0, str.Length, out value);
    }
  }

  /// <summary>Tries to parse a 64-bit signed integer out of a string using the same format as the
  /// <see cref="CultureInfo.InvariantCulture"/>. The method accepts null strings and leading and trailing whitespace.
  /// </summary>
  public static bool TryParse(string str, out long value)
  {
    if(str == null)
    {
      value = 0;
      return false;
    }
    else
    {
      return TryParse(str, 0, str.Length, out value);
    }
  }

  /// <summary>Tries to parse a 64-bit unsigned integer out of a string using the same format as the
  /// <see cref="CultureInfo.InvariantCulture"/>. The method accepts null strings and leading and trailing whitespace.
  /// </summary>
  [CLSCompliant(false)]
  public static bool TryParse(string str, out ulong value)
  {
    if(str == null)
    {
      value = 0;
      return false;
    }
    else
    {
      return TryParse(str, 0, str.Length, out value);
    }
  }

  /// <summary>Tries to parse a 32-bit signed integer out of a string using the same format as the
  /// <see cref="CultureInfo.InvariantCulture"/>. The method accepts leading and trailing whitespace.
  /// </summary>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> and <paramref name="length"/> do not specify a valid
  /// region within the string.
  /// </exception>
  public static bool TryParse(string str, int index, int length, out int value)
  {
    if(Trim(str, ref index, ref length))
    {
      return TryParseExact(str, index, length, out value);
    }
    else
    {
      value = 0;
      return false;
    }
  }

  /// <summary>Tries to parse a 32-bit unsigned integer out of a string using the same format as the
  /// <see cref="CultureInfo.InvariantCulture"/>. The method accepts leading and trailing whitespace.
  /// </summary>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> and <paramref name="length"/> do not specify a valid
  /// region within the string.
  /// </exception>
  [CLSCompliant(false)]
  public static bool TryParse(string str, int index, int length, out uint value)
  {
    if(Trim(str, ref index, ref length))
    {
      return TryParseExact(str, index, length, out value);
    }
    else
    {
      value = 0;
      return false;
    }
  }

  /// <summary>Tries to parse a 64-bit signed integer out of a string using the same format as the
  /// <see cref="CultureInfo.InvariantCulture"/>. The method accepts leading and trailing whitespace.
  /// </summary>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> and <paramref name="length"/> do not specify a valid
  /// region within the string.
  /// </exception>
  public static bool TryParse(string str, int index, int length, out long value)
  {
    if(Trim(str, ref index, ref length))
    {
      return TryParseExact(str, index, length, out value);
    }
    else
    {
      value = 0;
      return false;
    }
  }

  /// <summary>Tries to parse a 64-bit unsigned integer out of a string using the same format as the
  /// <see cref="CultureInfo.InvariantCulture"/>. The method accepts leading and trailing whitespace.
  /// </summary>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> and <paramref name="length"/> do not specify a valid
  /// region within the string.
  /// </exception>
  [CLSCompliant(false)]
  public static bool TryParse(string str, int index, int length, out ulong value)
  {
    if(Trim(str, ref index, ref length))
    {
      return TryParseExact(str, index, length, out value);
    }
    else
    {
      value = 0;
      return false;
    }
  }

  /// <summary>Tries to parse a 32-bit signed integer out of a string using the same format as the
  /// <see cref="CultureInfo.InvariantCulture"/>. The method accepts null strings but does not accept whitespace.
  /// </summary>
  public static bool TryParseExact(string str, out int value)
  {
    if(str == null)
    {
      value = 0;
      return false;
    }
    else
    {
      return TryParseExact(str, 0, str.Length, out value);
    }
  }

  /// <summary>Tries to parse a 32-bit unsigned integer out of a string using the same format as the
  /// <see cref="CultureInfo.InvariantCulture"/>. The method accepts null strings but does not accept whitespace.
  /// </summary>
  [CLSCompliant(false)]
  public static bool TryParseExact(string str, out uint value)
  {
    if(str == null)
    {
      value = 0;
      return false;
    }
    else
    {
      return TryParseExact(str, 0, str.Length, out value);
    }
  }

  /// <summary>Tries to parse a 64-bit signed integer out of a string using the same format as the
  /// <see cref="CultureInfo.InvariantCulture"/>. The method accepts null strings but does not accept whitespace.
  /// </summary>
  public static bool TryParseExact(string str, out long value)
  {
    if(str == null)
    {
      value = 0;
      return false;
    }
    else
    {
      return TryParseExact(str, 0, str.Length, out value);
    }
  }

  /// <summary>Tries to parse a 64-bit unsigned integer out of a string using the same format as the
  /// <see cref="CultureInfo.InvariantCulture"/>. The method accepts null strings but does not accept whitespace.
  /// </summary>
  [CLSCompliant(false)]
  public static bool TryParseExact(string str, out ulong value)
  {
    if(str == null)
    {
      value = 0;
      return false;
    }
    else
    {
      return TryParseExact(str, 0, str.Length, out value);
    }
  }

  /// <summary>Tries to parse a 32-bit signed integer out of a string using the same format as the
  /// <see cref="CultureInfo.InvariantCulture"/>. The method does not accept null strings or whitespace.
  /// </summary>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> and <paramref name="length"/> do not specify a valid
  /// region within the string.
  /// </exception>
  public static bool TryParseExact(string str, int index, int length, out int value)
  {
    Utility.ValidateRange(str, index, length);
    if(length != 0)
    {
      bool negative = str[index] == '-';
      if(negative)
      {
        if(--length == 0) goto failed;
        index++;
      }

      int v = 0;
      for(int end = index+length; index < end && v <= 0; index++)
      {
        char c = str[index];
        if(c > '9' || c < '0' || v < -429496728) goto failed; // below -429496728 it may underflow undetectably
        v = v*10 - (c-'0'); // accumulate the number as a negative value so we can reach a maximum magnitude of 2^31
      }

      if(v <= 0 && (negative || v != int.MinValue))
      {
        value = negative ? v : -v;
        return true;
      }
    }

    failed:
    value = 0;
    return false;
  }

  /// <summary>Tries to parse a 32-bit unsigned integer out of a string using the same format as the
  /// <see cref="CultureInfo.InvariantCulture"/>. The method does not accept null strings or whitespace.
  /// </summary>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> and <paramref name="length"/> do not specify a valid
  /// region within the string.
  /// </exception>
  [CLSCompliant(false)]
  public static bool TryParseExact(string str, int index, int length, out uint value)
  {
    Utility.ValidateRange(str, index, length);
    if(length != 0)
    {
      uint v = 0;
      for(int end = index+length; index < end; index++)
      {
        char c = str[index];
        if(c > '9' || c < '0' || v > 477218587u) goto failed; // above 477218587 it may overflow undetectably
        uint newV = v*10 + (uint)(c-'0');
        if(newV < v) goto failed;
        v = newV;
      }

      value = v;
      return true;
    }

    failed:
    value = 0;
    return false;
  }


  /// <summary>Tries to parse a 64-bit signed integer out of a string using the same format as the
  /// <see cref="CultureInfo.InvariantCulture"/>. The method does not accept null strings or whitespace.
  /// </summary>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> and <paramref name="length"/> do not specify a valid
  /// region within the string.
  /// </exception>
  public static bool TryParseExact(string str, int index, int length, out long value)
  {
    Utility.ValidateRange(str, index, length);
    if(length != 0)
    {
      bool negative = str[index] == '-';
      if(negative)
      {
        if(--length == 0) goto failed;
        index++;
      }

      long v = 0;
      for(int end = index+length; index < end && v <= 0; index++)
      {
        char c = str[index];
        if(c > '9' || c < '0' || v < -1844674407370955160) goto failed; // below -1844674407370955160 it may underflow undetectably
        v = v*10 - (c-'0'); // accumulate the number as a negative value so we can reach a maximum magnitude of 2^63
      }

      if(v <= 0 && (negative || v != long.MinValue))
      {
        value = negative ? v : -v;
        return true;
      }
    }

    failed:
    value = 0;
    return false;
  }

  /// <summary>Tries to parse a 64-bit unsigned integer out of a string using the same format as the
  /// <see cref="CultureInfo.InvariantCulture"/>. The method does not accept null strings or whitespace.
  /// </summary>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> and <paramref name="length"/> do not specify a valid
  /// region within the string.
  /// </exception>
  [CLSCompliant(false)]
  public static bool TryParseExact(string str, int index, int length, out ulong value)
  {
    Utility.ValidateRange(str, index, length);
    if(length != 0)
    {
      ulong v = 0;
      for(int end = index+length; index < end; index++)
      {
        char c = str[index];
        if(c > '9' || c < '0' || v > 2049638230412172400u) goto failed; // above 2049638230412172400 it may overflow undetectably
        ulong newV = v*10 + (uint)(c-'0');
        if(newV < v) goto failed;
        v = newV;
      }

      value = v;
      return true;
    }

    failed:
    value = 0;
    return false;
  }

  static bool Trim(string str, ref int index, ref int length)
  {
    int i = index, len = length;
    Utility.ValidateRange(str, i, len);
    int last = i+len-1;
    while(i <= last && char.IsWhiteSpace(str[i])) i++;
    while(i <= last && char.IsWhiteSpace(str[last])) last--;
    if(i <= last)
    {
      index  = i;
      length = last-i+1;
      return true;
    }
    else
    {
      return false;
    }
  }
}
#endregion

#region PrimitiveExtensions
/// <summary>Provides extensions for primitive .NET types.</summary>
public static class PrimitiveExtensions
{
  /// <summary>Returns the value of the integer rendered into a string using the invariant culture.</summary>
  public static string ToStringInvariant(this int value)
  {
    return value.ToString(CultureInfo.InvariantCulture);
  }

  /// <summary>Returns the value of the integer rendered into a string using the invariant culture.</summary>
  public static string ToStringInvariant(this long value)
  {
    return value.ToString(CultureInfo.InvariantCulture);
  }

  /// <summary>Returns the value of the integer rendered into a string using the invariant culture.</summary>
  [CLSCompliant(false)]
  public static string ToStringInvariant(this uint value)
  {
    return value.ToString(CultureInfo.InvariantCulture);
  }

  /// <summary>Returns the value of the integer rendered into a string using the invariant culture.</summary>
  [CLSCompliant(false)]
  public static string ToStringInvariant(this ulong value)
  {
    return value.ToString(CultureInfo.InvariantCulture);
  }

  /// <summary>Returns the value of the decimal rendered into a string using the invariant culture.</summary>
  public static string ToStringInvariant(this decimal value)
  {
    return value.ToString(CultureInfo.InvariantCulture);
  }

  /// <summary>Returns the value of the number rendered into a string using the invariant culture.</summary>
  public static string ToStringInvariant(this double value)
  {
    return value.ToString(CultureInfo.InvariantCulture);
  }

  /// <summary>Returns the value of the number rendered into a string using the invariant culture.</summary>
  public static string ToStringInvariant(this float value)
  {
    return value.ToString(CultureInfo.InvariantCulture);
  }
}
#endregion

} // namespace AdamMil.Utilities
