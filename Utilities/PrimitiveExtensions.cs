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
using System.Globalization;

namespace AdamMil.Utilities
{

/// <summary>Provides extensions for primitive numeric types.</summary>
public static class PrimitiveExtensions
{
  /// <summary>Converts the value to a string using the invariant culture.</summary>
  public static string ToInvariantString(this int value)
  {
    return value.ToString(CultureInfo.InvariantCulture);
  }

  /// <summary>Converts the value to a string using the invariant culture.</summary>
  public static string ToInvariantString(this long value)
  {
    return value.ToString(CultureInfo.InvariantCulture);
  }

  /// <summary>Converts the value to a string using the invariant culture.</summary>
  [CLSCompliant(false)]
  public static string ToInvariantString(this uint value)
  {
    return value.ToString(CultureInfo.InvariantCulture);
  }

  /// <summary>Converts the value to a string using the invariant culture.</summary>
  [CLSCompliant(false)]
  public static string ToInvariantString(this ulong value)
  {
    return value.ToString(CultureInfo.InvariantCulture);
  }

  /// <summary>Converts the value to a string using the invariant culture.</summary>
  public static string ToInvariantString(this decimal value)
  {
    return value.ToString(CultureInfo.InvariantCulture);
  }

  /// <summary>Converts the value to a string using the invariant culture.</summary>
  public static string ToInvariantString(this double value)
  {
    return value.ToString(CultureInfo.InvariantCulture);
  }
}

} // namespace AdamMil.Utilities
