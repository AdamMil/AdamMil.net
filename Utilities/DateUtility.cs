/*
AdamMil.Utilities is a library providing generally useful utilities for
.NET development.

http://www.adammil.net/
Copyright (C) 2010-2016 Adam Milazzo

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
using System.Text;

namespace AdamMil.Utilities
{

/// <summary>Provides extensions to the <see cref="DateTime"/> structure.</summary>
public static class DateUtility
{
  /// <summary>Adds an <see cref="XmlDuration"/> to a <see cref="DateTime"/> and returns the result.</summary>
  public static DateTime Add(this DateTime dateTime, XmlDuration duration)
  {
    return XmlDuration.Add(dateTime, duration);
  }

  /// <summary>Adds an <see cref="XmlDuration"/> to a <see cref="DateTimeOffset"/> and returns the result.</summary>
  public static DateTimeOffset Add(this DateTimeOffset dateTime, XmlDuration duration)
  {
    return XmlDuration.Add(dateTime, duration);
  }

  /// <summary>Determines whether two dates have the same value and <see cref="DateTimeKind"/>.</summary>
  public static bool ExactlyEquals(this DateTime a, DateTime b)
  {
    return a == b && a.Kind == b.Kind;
  }

  /// <summary>
  /// Returns a <see cref="DateTime"/> representing the latest representable moment in the day -- one tick before the next day.
  /// </summary>
  public static DateTime GetEndOfDay(this DateTime dateTime)
  {
    return dateTime.Date.AddDays(1).AddTicks(-1);
  }
 
  /// <summary>Subtracts an <see cref="XmlDuration"/> from a <see cref="DateTime"/> and returns the result.</summary>
  public static DateTime Subtract(this DateTime dateTime, XmlDuration duration)
  {
    return XmlDuration.Subtract(dateTime, duration);
  }

  /// <summary>Subtracts an <see cref="XmlDuration"/> from a <see cref="DateTimeOffset"/> and returns the result.</summary>
  public static DateTimeOffset Subtract(this DateTimeOffset dateTime, XmlDuration duration)
  {
    return XmlDuration.Subtract(dateTime, duration);
  }

  /// <summary>Converts the date to the shortest string that contains all of the relevant information. If the date contains no
  /// time component, only a date string will be returned. Otherwise, if it contains a time component with no seconds component,
  /// a short date and short time will be returned. Otherwise, a short date and long time will be returned.
  /// </summary>
  public static string ToShortString(this DateTime dateTime)
  {
    return dateTime.ToShortString(null);
  }

  /// <summary>Converts the date to the shortest string that contains all of the relevant information. If the date contains no
  /// time component, only a date string will be returned. Otherwise, if it contains a time component with no seconds component,
  /// a short date and short time will be returned. Otherwise, a short date and long time will be returned.
  /// </summary>
  public static string ToShortString(this DateTime dateTime, IFormatProvider provider)
  {
    TimeSpan timeOfDay = dateTime.TimeOfDay;
    return dateTime.ToString(timeOfDay.Ticks == 0 ? "d" : timeOfDay.Ticks % TimeSpan.TicksPerMinute == 0 ? "g" : "G", provider);
  }

  /// <summary>Converts the date to a string that contains all the information needed to reconstruct the date (with the exception of the
  /// <see cref="DateTime.Kind"/> property) and that can be compared with other strings returned from this function to sort date values.
  /// The returned string can also be parsed back into a date by <see cref="DateTime.Parse(string)"/>.
  /// </summary>
  public static string ToSortableString(this DateTime dateTime)
  {
    StringBuilder sb = new StringBuilder(28);
    PadLeft(sb, dateTime.Year, 4);
    sb.Append('-');
    PadLeft(sb, dateTime.Month, 2);
    sb.Append('-');
    PadLeft(sb, dateTime.Day, 2);

    TimeSpan timeOfDay = dateTime.TimeOfDay;
    if(timeOfDay.Ticks != 0) sb.Append(' ').Append(timeOfDay.ToString()); // TimeSpan.ToString() is documented as being in the format
    return sb.ToString();                                                 // hh:mm:ss[.fffffff], which is what we want
  }

  /// <summary>Gets the number of days in a given Gregorian month and year.</summary>
  internal static int GetDaysInMonth(int month, int year)
  {
    return month == 2 && IsLeapYear(year) ? 29 : monthDays[month-1];
  }

  /// <summary>Determines whether the given year is a leap year in the Gregorian calendar.</summary>
  internal static bool IsLeapYear(int year)
  {
    // the normal rule for determining a leap year is that it must be divisible by 400 or else it must be divisible by 4 but not 100. we
    // can use the fact that if it's divisible by 16, then if it's divisible by 100 then it must also be divisible by 400, in order to
    // reduce the number of division operations
    return (year & 3) == 0 && ((year & 15) == 0 || year % 100 != 0);
  }

  static void PadLeft(StringBuilder sb, int value, int length)
  {
    string str = value.ToStringInvariant();
    if(str.Length < length) sb.Append('0', length - str.Length);
    sb.Append(str);
  }

  static readonly int[] monthDays = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
}

} // namespace AdamMil.Utilities
