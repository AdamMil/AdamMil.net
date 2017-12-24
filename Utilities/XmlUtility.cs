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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace AdamMil.Utilities
{

#region XmlDuration
/// <summary>Represents an <c>xs:duration</c> value.</summary>
/// <remarks>An <see cref="XmlDuration"/> works like a <see cref="TimeSpan"/> value, except that it maintains a distinction between the
/// number of years and months in the duration, which may vary in actual length, and the days, hours, minutes, and seconds, which do not.
/// For example, January is longer than February and leap years are longer than regular years, so an <c>xs:duration</c> of <c>P1Y2M</c>
/// (1 year and 2 months) adds a variable amount of real time depending on the date to which it's added. The <see cref="TimeSpan"/>
/// structure does not capture this distinction, and is therefore inappropriate to represent an <c>xs:duration</c> value.
/// <para>The <c>xs:duration</c> format does have some limitations to be aware of, however. An <c>xs:duration</c> can represent a positive
/// or negative span of time, but it cannot represent a span of time where the variable portion is positive and the fixed portion is
/// negative, or vice versa. For instance, you can have a period of one month and one day, or negative one month and negative one day, but
/// you cannot have a period of one month and negative one day or vice versa. This also prevents durations from being added together when
/// the result would not be entirely positive or negative (or zero).
/// </para>
/// <para>The XML Schema specification says "Time durations are added by simply adding each of their fields, respectively, without
/// overflow", where "fields" refers to the components such as month, day, hour, etc.  Since adding without overflow is not really
/// possible in a fixed amount of space, and we desire to keep the structure as small as possible without placing tight restrictions on the
/// range of each component, and we want to relax the restrictions on having components with differing signs, adding two
/// <see cref="XmlDuration"/> values will allow overflow between fields. For instance, adding two durations of 40 seconds will yield a
/// duration of 1 minute and 20 seconds rather than a duration of 80 seconds. (The two are equivalent in all ways except for their string
/// representation.) Similarly, adding a duration of -10 seconds to a duration of 1 minute will not be an error but will instead yield a
/// duration of 50 seconds.
/// </para>
/// <para>The <see cref="XmlDuration"/> type is limited to maximums of 2147483647 total months (as any combination of years and months)
/// and approximately 10675199.1167 total days (as any combination of days, hours, minutes, etc). This is not a limitation inherent to the
/// <c>xs:duration</c> format, but one imposed by the fixed amount of space available in the <see cref="XmlDuration"/> structure.
/// </para>
/// </remarks>
[Serializable]
public struct XmlDuration
{
  /// <summary>Represents the number of ticks in one millisecond.</summary>
  public const long TicksPerMillisecond = 10*1000L; // one tick is 100 nanoseconds, the same as that used by DateTime, Timespan, etc.
  /// <summary>Represents the number of ticks in one second.</summary>
  public const long TicksPerSecond = TicksPerMillisecond * 1000;
  /// <summary>Represents the number of ticks in one minute.</summary>
  public const long TicksPerMinute = TicksPerSecond * 60;
  /// <summary>Represents the number of ticks in one hour.</summary>
  public const long TicksPerHour = TicksPerMinute * 60;
  /// <summary>Represents the number of ticks in one day.</summary>
  public const long TicksPerDay = TicksPerHour * 24;

  /// <summary>Initializes a new <see cref="XmlDuration"/> from the given <see cref="TimeSpan"/> value.</summary>
  /// <remarks>All <see cref="TimeSpan"/> values except <see cref="TimeSpan.MinValue"/> can be represented as an <see cref="XmlDuration"/>.</remarks>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="timeSpan"/> equals <see cref="TimeSpan.MinValue"/>.</exception>
  public XmlDuration(TimeSpan timeSpan)
  {
    if(timeSpan.Ticks < 0)
    {
      _ticks = -timeSpan.Ticks; // TODO: it would be nice to remove this limitation
      if(_ticks < 0) throw new ArgumentOutOfRangeException("TimeSpan.MinValue cannot be represented as an XmlDuration.");
      _months = 0x80000000;
    }
    else
    {
      _months = 0;
      _ticks  = timeSpan.Ticks;
    }
  }

  /// <summary>Initializes a new <see cref="XmlDuration"/> from the given number of years, months, and days.</summary>
  /// <include file="documentation.xml" path="/Utilities/XmlDuration/AddSubRemarks/node()"/>
  public XmlDuration(int years, int months, int days)
  {
    if(days < -10675199 || days > 10675199) throw OverflowError();
    _ticks  = days * TicksPerDay;
    _months = GetTotalMonths(years, months);
    FixSign();
  }

  /// <summary>Initializes a new <see cref="XmlDuration"/> from the given components.</summary>
  /// <include file="documentation.xml" path="/Utilities/XmlDuration/AddSubRemarks/node()"/>
  public XmlDuration(int years, int months, int days, int hours, int minutes, int seconds)
  {
    if(days < -10675199 || days > 10675199 || hours < -256204778 || hours > 256204778) throw OverflowError();
    _ticks  = Add(days * TicksPerDay, Add(hours * TicksPerHour, minutes*TicksPerMinute + seconds*TicksPerSecond));
    _months = GetTotalMonths(years, months);
    FixSign();
  }

  /// <summary>Initializes a new <see cref="XmlDuration"/> from the given components.</summary>
  /// <include file="documentation.xml" path="/Utilities/XmlDuration/AddSubRemarks/node()"/>
  public XmlDuration(int years, int months, int days, int hours, int minutes, int seconds, int milliseconds)
  {
    if(days < -10675199 || days > 10675199 || hours < -256204778 || hours > 256204778) throw OverflowError();
    _ticks = Add(days * TicksPerDay,
                 Add(hours * TicksPerHour, minutes*TicksPerMinute + seconds*TicksPerSecond + milliseconds*TicksPerMillisecond));
    _months = GetTotalMonths(years, months);
    FixSign();
  }

  /// <summary>Initializes a new <see cref="XmlDuration"/> from a number of years and months to add, and a number of 100-nanosecond ticks
  /// to add.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/XmlDuration/AddSubRemarks/node()"/>
  public XmlDuration(int years, int months, long ticks)
  {
    _ticks  = ticks;
    _months = GetTotalMonths(years, months);
    FixSign();
  }

  /// <summary>Initializes a new <see cref="XmlDuration"/> from a number months to add or subtract, and a number of 100-nanosecond ticks to
  /// add or subtract, and a boolean that indicates whether the duration should be negative (i.e. whether we should subtract instead of
  /// add). The numbers of months and ticks must be non-negative.
  /// </summary>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="totalMonths"/> or <paramref name="ticks"/> is negative.</exception>
  public XmlDuration(int totalMonths, long ticks, bool isNegative)
  {
    if(totalMonths < 0 || ticks < 0) throw new ArgumentOutOfRangeException("An argument was negative.");
    if(isNegative && totalMonths == 0 && ticks == 0) isNegative = false;
    _ticks  = ticks;
    _months = (uint)totalMonths | (isNegative ? 0x80000000 : 0u);
  }

  XmlDuration(uint encodedMonths, long ticks)
  {
    _ticks  = ticks;
    _months = encodedMonths;
  }

  /// <summary>Gets the non-negative days component of this duration value.</summary>
  public int Days
  {
    get { return (int)(_ticks / TicksPerDay); }
  }

  /// <summary>Gets the non-negative hours component of this duration value.</summary>
  public int Hours
  {
    get { return (int)(_ticks % TicksPerDay / TicksPerHour); }
  }

  /// <summary>Gets the non-negative milliseconds component of this duration value, excluding the fractional part.</summary>
  public int Milliseconds
  {
    get { return (int)(_ticks % TicksPerSecond) / (int)TicksPerMillisecond; }
  }

  /// <summary>Gets the non-negative minutes component of this duration value.</summary>
  public int Minutes
  {
    get { return (int)(_ticks % TicksPerHour / TicksPerMinute); }
  }

  /// <summary>Gets the non-negative months component of this duration value.</summary>
  public int Months
  {
    get { return TotalMonths % 12; }
  }

  /// <summary>Gets whether the </summary>
  public bool IsNegative
  {
    get { return (int)_months < 0; }
  }

  /// <summary>Gets the non-negative seconds component of this duration value, including the fractional part.</summary>
  public double Seconds
  {
    get { return (int)(_ticks % TicksPerMinute) / (double)TicksPerSecond; }
  }

  /// <summary>Gets the non-negative number of ticks encapsulating the day and time components of this duration value.</summary>
  public long Ticks
  {
    get { return _ticks; }
  }

  /// <summary>Gets the non-negative seconds component of this duration value, excluding the fractional part.</summary>
  public int WholeSeconds
  {
    get { return (int)(_ticks % TicksPerMinute) / (int)TicksPerSecond; }
  }

  /// <summary>Gets the non-negative years component of this duration value.</summary>
  public int Years
  {
    get { return TotalMonths / 12; }
  }

  /// <summary>Gets the non-negative total number of months in this duration value.</summary>
  public int TotalMonths
  {
    get { return (int)(_months & 0x7FFFFFFF); }
  }

  /// <summary>Returns the absolute value of this duration, which will be an <see cref="XmlDuration"/> of the same length but with
  /// <see cref="IsNegative"/> equal to false.
  /// </summary>
  public XmlDuration Abs()
  {
    return new XmlDuration((uint)TotalMonths, _ticks);
  }

  /// <summary>Adds the given duration to this duration and returns the result.</summary>
  /// <include file="documentation.xml" path="/Utilities/XmlDuration/AddSubRemarks/node()"/>
  public XmlDuration Add(XmlDuration duration)
  {
    if(((uint)(_months ^ duration._months) & 0x80000000) == 0) // if the two durations have the same sign...
    {
      return new XmlDuration(Add(TotalMonths, duration.TotalMonths), Add(_ticks, duration._ticks), IsNegative);
    }
    else // if the durations have opposite signs...
    {
      long ticks = _ticks - duration._ticks;
      int months = TotalMonths - duration.TotalMonths;

      // if the resulting components have opposite signs, the value cannot be represented as an xs:duration
      if(months < 0 ? ticks > 0 : months > 0 && ticks < 0) throw UnrepresentableError();

      return months < 0 ? new XmlDuration(-months, -ticks, !IsNegative) : new XmlDuration(months, ticks, IsNegative);
    }
  }

  /// <summary>Adds the given number of days (which can be negative) to this duration and returns the result.</summary>
  /// <include file="documentation.xml" path="/Utilities/XmlDuration/AddSubRemarks/node()"/>
  public XmlDuration AddDays(double days)
  {
    return AddTicks((long)(days * TicksPerDay + 0.5));
  }

  /// <summary>Adds the given number of hours (which can be negative) to this duration and returns the result.</summary>
  /// <include file="documentation.xml" path="/Utilities/XmlDuration/AddSubRemarks/node()"/>
  public XmlDuration AddHours(double hours)
  {
    return AddTicks((long)(hours * TicksPerHour + 0.5));
  }

  /// <summary>Adds the given number of milliseconds (which can be negative) to this duration and returns the result.</summary>
  /// <include file="documentation.xml" path="/Utilities/XmlDuration/AddSubRemarks/node()"/>
  public XmlDuration AddMilliseconds(double seconds)
  {
    return AddTicks((long)(seconds * TicksPerMillisecond + 0.5));
  }

  /// <summary>Adds the given number of minutes (which can be negative) to this duration and returns the result.</summary>
  /// <include file="documentation.xml" path="/Utilities/XmlDuration/AddSubRemarks/node()"/>
  public XmlDuration AddMinutes(double minutes)
  {
    return AddTicks((long)(minutes * TicksPerMinute + 0.5));
  }

  /// <summary>Adds the given number of months (which can be negative) to this duration and returns the result.</summary>
  /// <include file="documentation.xml" path="/Utilities/XmlDuration/AddSubRemarks/node()"/>
  public XmlDuration AddMonths(int months)
  {
    if(IsNegative == (months < 0))
    {
      months = Add(TotalMonths, months);
    }
    else
    {
      months = TotalMonths - months;
      if(months < 0) throw UnrepresentableError(); // if the result changed sign, it can't be represented as an xs:duration
    }
    return new XmlDuration(months, _ticks, IsNegative);
  }

  /// <summary>Adds the given number of seconds (which can be negative) to this duration and returns the result.</summary>
  /// <include file="documentation.xml" path="/Utilities/XmlDuration/AddSubRemarks/node()"/>
  public XmlDuration AddSeconds(double seconds)
  {
    return AddTicks((long)(seconds * TicksPerSecond + 0.5));
  }

  /// <summary>Adds the given number of ticks (which can be negative) to this duration and returns the result.</summary>
  /// <include file="documentation.xml" path="/Utilities/XmlDuration/AddSubRemarks/node()"/>
  public XmlDuration AddTicks(long ticks)
  {
    if(IsNegative == (ticks < 0))
    {
      ticks = Add(_ticks, ticks);
    }
    else
    {
      ticks = _ticks - ticks;
      if(ticks < 0) throw UnrepresentableError(); // if the result changed sign, it can't be represented as an xs:duration
    }
    return new XmlDuration(TotalMonths, ticks, IsNegative);
  }

  /// <summary>Adds the given number of years (which can be negative) to this duration and returns the result.</summary>
  /// <include file="documentation.xml" path="/Utilities/XmlDuration/AddSubRemarks/node()"/>
  public XmlDuration AddYears(int years)
  {
    if(years < -178956970 || years > 178956970) throw OverflowError();
    return AddMonths(years*12);
  }

  /// <inheritdoc/>
  public override bool Equals(object obj)
  {
    return obj is XmlDuration && Equals((XmlDuration)obj);
  }

  /// <summary>Determines whether the given duration equals this one.</summary>
  public bool Equals(XmlDuration other)
  {
    return _ticks == other._ticks && _months == other._months;
  }

  /// <inheritdoc/>
  public override int GetHashCode()
  {
    return (int)((uint)(ulong)_ticks ^ (uint)((ulong)_ticks >> 32) ^ (uint)_months);
  }

  /// <summary>Returns an <see cref="XmlDuration"/> with the same length as this one, but the opposite sign.</summary>
  public XmlDuration Negate()
  {
    return new XmlDuration(TotalMonths, _ticks, !IsNegative);
  }

  /// <summary>Subtractions the given duration from this one and returns the result.</summary>
  /// <include file="documentation.xml" path="/Utilities/XmlDuration/AddSubRemarks/node()"/>
  public XmlDuration Subtract(XmlDuration duration)
  {
    return Add(duration.Negate());
  }

  /// <summary>Returns the duration as an <c>xs:duration</c> value, which is an ISO 8601 duration as extended by the XML Schema
  /// specification.
  /// </summary>
  /// <remarks>For example, <c>P1Y2MT2H</c> represents a duration of one year, two months, and two hours.</remarks>
  public override string ToString()
  {
    StringBuilder sb = new StringBuilder(40);
    if(IsNegative) sb.Append('-');
    sb.Append('P');
    if(TotalMonths != 0)
    {
      RenderComponent(sb, Years, 'Y');
      RenderComponent(sb, Months, 'M');
    }
    if(_ticks != 0)
    {
      RenderComponent(sb, Days, 'D');
      int hours = Hours, minutes = Minutes, secondTicks = (int)(_ticks % TicksPerMinute);
      if((hours|minutes|secondTicks) != 0)
      {
        sb.Append('T');
        RenderComponent(sb, hours, 'H');
        RenderComponent(sb, minutes, 'M');
        if(secondTicks != 0)
        {
          int component = secondTicks / (int)TicksPerSecond; // whole seconds
          sb.Append(component.ToStringInvariant());
          component = secondTicks % (int)TicksPerSecond; // fractional seconds in 100 ns units
          if(component != 0) sb.Append('.').Append(component.ToStringInvariant().PadLeft(7, '0').TrimEnd('0'));
          sb.Append('S');
        }
      }
    }

    if(sb.Length <= 2) sb.Append("0D"); // there has to be at least one component, so add 0 days if we haven't added any components so far
    return sb.ToString();
  }

  /// <summary>Returns a <see cref="TimeSpan"/> that represents the same duration as this <see cref="XmlDuration"/>. Note that not all
  /// <see cref="XmlDuration"/> values can be represented as <see cref="TimeSpan"/> values.
  /// </summary>
  /// <remarks><see cref="TimeSpan"/> values can only represent fixed lengths of time. Month and years are not fixed lengths of time
  /// and so durations having non-zero months or years cannot be represented as time spans.
  /// </remarks>
  /// <exception cref="InvalidOperationException">Thrown if the <see cref="XmlDuration"/> cannot be represented as a
  /// <see cref="TimeSpan"/>.
  /// </exception>
  public TimeSpan ToTimeSpan()
  {
    if(TotalMonths != 0)
    {
      throw new InvalidOperationException("This duration cannot be represented by a TimeSpan because it has a variable-length component.");
    }
    return new TimeSpan(IsNegative ? -Ticks : Ticks);
  }

  /// <summary>Adds two durations together and returns the result.</summary>
  /// <include file="documentation.xml" path="/Utilities/XmlDuration/AddSubRemarks/node()"/>
  public static XmlDuration operator+(XmlDuration a, XmlDuration b)
  {
    return a.Add(b);
  }

  /// <summary>Subtracts <paramref name="b"/> from <paramref name="a"/> and returns the result.</summary>
  /// <include file="documentation.xml" path="/Utilities/XmlDuration/AddSubRemarks/node()"/>
  public static XmlDuration operator-(XmlDuration a, XmlDuration b)
  {
    return a.Add(b.Negate());
  }

  /// <summary>Negates an <see cref="XmlDuration"/> and returns the result.</summary>
  public static XmlDuration operator-(XmlDuration duration)
  {
    return duration.Negate();
  }

  /// <summary>Determines if two durations are equal.</summary>
  public static bool operator==(XmlDuration a, XmlDuration b)
  {
    return a._ticks == b._ticks && a._months == b._months;
  }

  /// <summary>Determines if two durations are unequal.</summary>
  public static bool operator!=(XmlDuration a, XmlDuration b)
  {
    return a._ticks != b._ticks || a._months != b._months;
  }

  /// <summary>Adds a duration to a <see cref="DateTime"/> and returns the resulting <see cref="DateTime"/>.</summary>
  public static DateTime Add(DateTime dateTime, XmlDuration duration)
  {
    if(duration._months != 0) dateTime = dateTime.AddMonths(duration.IsNegative ? -duration.TotalMonths : (int)duration._months);
    if(duration._ticks != 0) dateTime = dateTime.AddTicks(duration.IsNegative ? -duration._ticks : duration._ticks);
    return dateTime;
  }

  /// <summary>Adds a duration to a <see cref="DateTimeOffset"/> and returns the resulting <see cref="DateTimeOffset"/>.</summary>
  public static DateTimeOffset Add(DateTimeOffset dateTime, XmlDuration duration)
  {
    if(duration._months != 0) dateTime = dateTime.AddMonths(duration.IsNegative ? -duration.TotalMonths : (int)duration._months);
    if(duration._ticks != 0) dateTime = dateTime.AddTicks(duration.IsNegative ? -duration._ticks : duration._ticks);
    return dateTime;
  }

  /// <summary>Parses an <see cref="XmlDuration"/> from an ISO 8601 duration string as extended by the XML Schema specification.</summary>
  /// <remarks>For example, <c>P1Y2MT2H</c> represents a duration of one year, two months, and two hours.</remarks>
  public static XmlDuration Parse(string str)
  {
    if(str == null) throw new ArgumentNullException();
    XmlDuration duration;
    if(!TryParse(str, out duration)) throw new FormatException();
    return duration;
  }

  /// <summary>Subtracts a duration from a <see cref="DateTime"/> and returns the resulting <see cref="DateTime"/>.</summary>
  public static DateTime Subtract(DateTime dateTime, XmlDuration duration)
  {
    if(duration._months != 0) dateTime = dateTime.AddMonths(duration.IsNegative ? (int)duration._months : -duration.TotalMonths);
    if(duration._ticks != 0) dateTime = dateTime.AddTicks(duration.IsNegative ? duration._ticks : -duration._ticks);
    return dateTime;
  }

  /// <summary>Subtracts a duration from a <see cref="DateTimeOffset"/> and returns the resulting <see cref="DateTimeOffset"/>.</summary>
  public static DateTimeOffset Subtract(DateTimeOffset dateTime, XmlDuration duration)
  {
    if(duration._months != 0) dateTime = dateTime.AddMonths(duration.IsNegative ? (int)duration._months : -duration.TotalMonths);
    if(duration._ticks != 0) dateTime = dateTime.AddTicks(duration.IsNegative ? duration._ticks : -duration._ticks);
    return dateTime;
  }

  /// <summary>Attempts to parses an <see cref="XmlDuration"/> from an ISO 8601 duration string as extended by the XML Schema
  /// specification.
  /// </summary>
  /// <remarks>For example, <c>P1Y2MT2H</c> represents a duration of one year, two months, and two hours.</remarks>
  public static bool TryParse(string str, out XmlDuration duration)
  {
    if(!string.IsNullOrEmpty(str))
    {
      Match m = reDuration.Match(str);
      if(m.Success)
      {
        // parse all of the components
        int years, months, days, hours, totalMonths;
        long mins;
        double seconds;
        bool hadComponent = false;

        if(!ParseGroup(m.Groups["y"], 178956970, ref hadComponent, out years) ||
           !ParseGroup(m.Groups["mo"], int.MaxValue, ref hadComponent, out months) ||
           !ParseGroup(m.Groups["d"], 10675199, ref hadComponent, out days))
        {
          goto failed;
        }

        Group g = m.Groups["h"];
        bool hadTimeComponent = g.Success;
        if(!ParseGroup(g, 256204778, ref hadComponent, out hours)) goto failed;

        g = m.Groups["min"];
        hadTimeComponent |= g.Success;
        if(!g.Success) mins = 0;
        else if(!InvariantCultureUtility.TryParseExact(g.Value, out mins) || mins > 15372286728) goto failed;
        else hadComponent = true;

        g = m.Groups["s"];
        hadTimeComponent |= g.Success;
        if(!g.Success)
        {
          seconds = 0;
        }
        else if(!double.TryParse(g.Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out seconds) ||
                seconds > 922337203685.47747)
        {
          goto failed;
        }
        else
        {
          hadComponent = true;
        }

        long longMonths = years*12 + months;
        totalMonths = (int)longMonths;
        if(totalMonths != longMonths) goto failed; // fail if the total months overflow

        long ticks = days * TicksPerDay;
        if((ticks += hours*TicksPerHour) < 0 || (ticks += mins*TicksPerMinute) < 0 || (ticks += (long)(seconds*TicksPerSecond + 0.5)) < 0)
        {
          goto failed; // fail if the ticks overflow
        }

        // fail if no components were specified (at least one component is required) or if an empty time component was specified
        // (which the standard says is illegal)
        if(!hadComponent || !hadTimeComponent && m.Groups["time"].Success) goto failed;

        duration = new XmlDuration(totalMonths, ticks, m.Groups["n"].Success);
        return true;
      }
    }

    failed:
    duration = default(XmlDuration);
    return false;
  }

  /// <summary>The largest possible negative <see cref="XmlDuration"/>.</summary>
  public static readonly XmlDuration MinValue = new XmlDuration((uint)int.MaxValue | 0x80000000, long.MaxValue);

  /// <summary>The largest possible positive <see cref="XmlDuration"/>.</summary>
  public static readonly XmlDuration MaxValue = new XmlDuration((uint)int.MaxValue, long.MaxValue);

  /// <summary>A zero <see cref="XmlDuration"/>.</summary>
  public static readonly XmlDuration Zero = new XmlDuration();

  void FixSign()
  {
    // if the two parts have different signs, the value can't be represented as an xs:duration
    if((int)_months < 0 ? _ticks > 0 : (int)_months > 0 && _ticks < 0) throw UnrepresentableError();
    // otherwise, if a value was negative and it's not possible to negate both values (because a value is at the minimum)...
    if(((int)_months < 0 || _ticks < 0) && ((_months = (uint)-(int)_months ^ 0x80000000) == 0 || (_ticks = -_ticks) < 0))
    {
      throw OverflowError(); // then it's out of range
    }
  }

  long _ticks;
  uint _months;

  static int Add(int a, int b)
  {
    try { return checked(a + b); }
    catch(OverflowException) { throw OverflowError(); }
  }

  static long Add(long a, long b)
  {
    try { return checked(a + b); }
    catch(OverflowException) { throw OverflowError(); }
  }

  static uint GetTotalMonths(int years, int months)
  {
    long totalMonths = years*12L + months;
    int intValue = (int)totalMonths;
    if(totalMonths != intValue || intValue == int.MinValue) throw OverflowError();
    return (uint)intValue;
  }

  static ArgumentOutOfRangeException OverflowError()
  {
    return new ArgumentOutOfRangeException("The result would be outside the range of XmlDuration.");
  }

  static bool ParseGroup(Group group, int maxValue, ref bool hadValue, out int value)
  {
    if(!group.Success) value = 0; // missing components are implicitly equal to zero
    else if(!InvariantCultureUtility.TryParseExact(group.Value, out value) || value > maxValue) return false;
    else hadValue = true;
    return true;
  }

  static void RenderComponent(StringBuilder sb, int component, char c)
  {
    if(component != 0) sb.Append(component.ToStringInvariant()).Append(c);
  }

  static ArgumentException UnrepresentableError()
  {
    return new ArgumentException("The result is not representable as an xs:duration because the fixed and variable portions of the " +
                                 "duration have opposite signs.");
  }

  static readonly Regex reDuration =
    new Regex(@"^\s*(?<n>-)?P(?:(?<y>[0-9]+)Y)?(?:(?<mo>[0-9]+)M)?(?:(?<d>[0-9]+)D)?(?<time>T(?:(?<h>[0-9]+)H)?(?:(?<min>[0-9]+)M)?(?:(?<s>[0-9]+(?:\.[0-9]+))S)?)?\s*$",
              RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
}
#endregion

#region XmlDocumentExtensions
/// <summary>Provides useful extensinos to the <see cref="XmlDocument"/> class.</summary>
public static class XmlDocumentExtensions
{
  /// <summary>Creates a new <see cref="XmlElement"/> having the given text content.</summary>
  public static XmlElement CreateElementWithContent(this XmlDocument document, string qualifiedName, string textValue)
  {
    XmlElement element = document.CreateElement(qualifiedName);
    if(textValue != null) element.AppendChild(document.CreateTextNode(textValue));
    return element;
  }
}
#endregion

#region XmlElementExtensions
/// <summary>Provides useful extensions to the <see cref="XmlElement"/> class.</summary>
public static class XmlElementExtensions
{
  /// <summary>Appends the given <see cref="XmlElement"/> to the end of the list of child nodes, and returns the element.</summary>
  public static XmlElement AppendElement(this XmlElement element, XmlElement newChild)
  {
    if(element == null) throw new ArgumentNullException();
    element.AppendChild(newChild);
    return newChild;
  }

  /// <summary>Returns the named attribute value.</summary>
  public static string GetAttribute(this XmlElement element, XmlQualifiedName attributeName)
  {
    if(element == null || attributeName == null) throw new ArgumentNullException();
    return element.GetAttribute(attributeName.Name, attributeName.Namespace);
  }

  /// <summary>Sets the named attribute with a value based on a boolean.</summary>
  public static void SetAttribute(this XmlElement element, string attributeName, bool value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(value));
  }

  /// <summary>Sets the named attribute with a value based on a byte.</summary>
  public static void SetAttribute(this XmlElement element, string attributeName, byte value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(value));
  }

  /// <summary>Sets the named attribute with a value based on a character.</summary>
  public static void SetAttribute(this XmlElement element, string attributeName, char value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(value));
  }

  /// <summary>Sets the named attribute with a value based on a <see cref="DateTime"/>. The <see cref="DateTimeKind"/> of the
  /// <see cref="DateTime"/> will be preserved.
  /// </summary>
  public static void SetAttribute(this XmlElement element, string attributeName, DateTime dateTimeValue)
  {
    element.SetAttribute(attributeName, dateTimeValue, XmlDateTimeSerializationMode.RoundtripKind);
  }

  /// <summary>Sets the named attribute with a value based on a <see cref="DateTime"/>.</summary>
  public static void SetAttribute(this XmlElement element, string attributeName, DateTime dateTimeValue,
                                  XmlDateTimeSerializationMode dateTimeMode)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(dateTimeValue, dateTimeMode));
  }

  /// <summary>Sets the named attribute with a value based on a <see cref="Decimal"/>.</summary>
  public static void SetAttribute(this XmlElement element, string attributeName, decimal value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(value));
  }

  /// <summary>Sets the named attribute with a value based on a 64-bit floating point value.</summary>
  public static void SetAttribute(this XmlElement element, string attributeName, double value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(value));
  }

  /// <summary>Sets the named attribute with a value based on a <see cref="Guid"/>.</summary>
  public static void SetAttribute(this XmlElement element, string attributeName, Guid value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(value));
  }

  /// <summary>Sets the named attribute with a value based on a 16-bit integer.</summary>
  public static void SetAttribute(this XmlElement element, string attributeName, short value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(value));
  }

  /// <summary>Sets the named attribute with a value based on a 32-bit integer.</summary>
  public static void SetAttribute(this XmlElement element, string attributeName, int value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(value));
  }

  /// <summary>Sets the named attribute with a value based on a 64-bit integer.</summary>
  public static void SetAttribute(this XmlElement element, string attributeName, long value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(value));
  }

  /// <summary>Sets the named attribute with a value based on an 8-bit integer.</summary>
  [CLSCompliant(false)]
  public static void SetAttribute(this XmlElement element, string attributeName, sbyte value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(value));
  }

  /// <summary>Sets the named attribute with a value based on an 32-bit floating point value.</summary>
  public static void SetAttribute(this XmlElement element, string attributeName, float value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(value));
  }

  /// <summary>Sets the named attribute with a value based on an <see cref="TimeSpan"/>.</summary>
  public static void SetAttribute(this XmlElement element, string attributeName, TimeSpan value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(value));
  }

  /// <summary>Sets the named attribute with a value based on a 16-bit unsigned integer.</summary>
  [CLSCompliant(false)]
  public static void SetAttribute(this XmlElement element, string attributeName, ushort value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(value));
  }

  /// <summary>Sets the named attribute with a value based on a 32-bit unsigned integer.</summary>
  [CLSCompliant(false)]
  public static void SetAttribute(this XmlElement element, string attributeName, uint value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(value));
  }

  /// <summary>Sets the named attribute with a value based on a 64-bit unsigned integer.</summary>
  [CLSCompliant(false)]
  public static void SetAttribute(this XmlElement element, string attributeName, ulong value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(value));
  }

  /// <summary>Sets the named attribute with a value based on an <see cref="XmlDuration"/>.</summary>
  public static void SetAttribute(this XmlElement element, string attributeName, XmlDuration value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, value.ToString());
  }

  /// <summary>Sets the named attribute.</summary>
  public static void SetAttribute(this XmlElement element, XmlQualifiedName attributeName, string value)
  {
    if(element == null || attributeName == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName.Name, attributeName.Namespace, value);
  }

  /// <summary>Sets the named attribute, using the specified prefix.</summary>
  public static void SetAttribute(this XmlElement element, string prefix, string localName, string namespaceUri, string value)
  {
    if(element == null) throw new ArgumentNullException();
    XmlAttribute attr = element.GetAttributeNode(localName, namespaceUri);
    if(attr == null || !attr.Prefix.OrdinalEquals(prefix))
    {
      attr = element.OwnerDocument.CreateAttribute(prefix, localName, namespaceUri);
      element.SetAttributeNode(attr);
    }
    attr.Value = value;
  }

  /// <summary>Sets the named attribute with a value based on the date portion of a <see cref="DateTime"/>.</summary>
  public static void SetDateAttribute(this XmlElement element, string attributeName, DateTime value)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
  }

  /// <summary>Sets the named attribute with a value based on the time portion of a <see cref="DateTime"/>. The
  /// <see cref="DateTimeKind"/> of the <see cref="DateTime"/> will be preserved.
  /// </summary>
  public static void SetTimeAttribute(this XmlElement element, string attributeName, DateTime value)
  {
    element.SetTimeAttribute(attributeName, value, XmlDateTimeSerializationMode.RoundtripKind);
  }

  /// <summary>Sets the named attribute with a value based on a <see cref="DateTime"/>.</summary>
  public static void SetTimeAttribute(this XmlElement element, string attributeName, DateTime value,
                                      XmlDateTimeSerializationMode mode)
  {
    if(element == null) throw new ArgumentNullException();
    element.SetAttribute(attributeName, XmlConvert.ToString(value, mode).Substring(11)); // strip off the date portion
  }
}
#endregion

#region XmlNamespaceResolverExtensions
/// <summary>Provides useful extensions to the <see cref="IXmlNamespaceResolver"/> class.</summary>
public static class XmlNamespaceResolverExtensions
{
  /// <summary>Parses a qualified name (i.e. a name of the form <c>prefix:localName</c> or <c>namespaceUri:localName</c>) into an
  /// <see cref="XmlQualifiedName"/> in the context of the current namespace resolver. This method also accepts local names.
  /// </summary>
  public static XmlQualifiedName ParseQualifiedName(this IXmlNamespaceResolver resolver, string qualifiedName)
  {
    if(resolver == null) throw new ArgumentNullException();
    return string.IsNullOrEmpty(qualifiedName) ?
      XmlQualifiedName.Empty : XmlUtility.ParseQualifiedName(qualifiedName, resolver.LookupNamespace);
  }
}
#endregion

#region XmlNodeExtensions
/// <summary>Provides useful extensions to the <see cref="XmlNode"/> class.</summary>
public static class XmlNodeExtensions
{
  /// <summary>Returns the value of the named attribute, or <c>default(T)</c> if the attribute was unspecified.</summary>
  public static T GetAttribute<T>(this XmlNode node, string attrName, Converter<string, T> converter)
  {
    return GetAttribute<T>(node, attrName, converter, default(T));
  }

  /// <summary>Returns the value of the named attribute, or the given default value if the attribute was unspecified.</summary>
  public static T GetAttribute<T>(this XmlNode node, string attrName, Converter<string, T> converter,
                                  T defaultValue)
  {
    if(converter == null) throw new ArgumentNullException("converter");
    XmlAttribute an = GetAttributeNode(node, attrName);
    return an == null ? defaultValue : converter(an.Value);
  }

  /// <summary>Returns the value of the named attribute, or null if the attribute was unspecified.</summary>
  public static string GetAttributeValue(this XmlNode node, string attrName)
  {
    return GetAttributeValue(node, attrName, null);
  }

  /// <summary>Returns the value of the named attribute, or the given default value if the attribute was unspecified.</summary>
  public static string GetAttributeValue(this XmlNode node, string attrName, string defaultValue)
  {
    XmlAttribute an = GetAttributeNode(node, attrName);
    return an == null ? defaultValue : an.Value;
  }

  /// <summary>Returns the value of the named attribute as a boolean, or false if the attribute was unspecified or empty.</summary>
  public static bool GetBoolAttribute(this XmlNode node, string attrName)
  {
    return GetBoolAttribute(node, attrName, false);
  }

  /// <summary>Returns the value of the named attribute as a boolean, or the given
  /// default value if the attribute was unspecified or empty.
  /// </summary>
  public static bool GetBoolAttribute(this XmlNode node, string attrName, bool defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToBoolean(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a byte, or 0 if the attribute was unspecified or empty.</summary>
  public static byte GetByteAttribute(this XmlNode node, string attrName)
  {
    return GetByteAttribute(node, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a byte, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static byte GetByteAttribute(this XmlNode node, string attrName, byte defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToByte(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a character, or the nul character if the attribute was unspecified or empty.</summary>
  public static char GetCharAttribute(this XmlNode node, string attrName)
  {
    return GetCharAttribute(node, attrName, '\0');
  }

  /// <summary>Returns the value of the named attribute as a character, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static char GetCharAttribute(this XmlNode node, string attrName, char defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToChar(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a nullable datetime, or null if the attribute was unspecified or empty.</summary>
  public static DateTime? GetDateTimeAttribute(this XmlNode node, string attrName)
  {
    return GetDateTimeAttribute(node, attrName, (DateTime?)null);
  }

  /// <summary>Returns the value of the named attribute as a nullable datetime, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static DateTime? GetDateTimeAttribute(this XmlNode node, string attrName, DateTime? defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ?
      defaultValue : XmlConvert.ToDateTime(attrValue, XmlDateTimeSerializationMode.Unspecified);
  }

  /// <summary>Returns the value of the named attribute as a decimal, or 0 if the attribute was unspecified or empty.</summary>
  public static decimal GetDecimalAttribute(this XmlNode node, string attrName)
  {
    return GetDecimalAttribute(node, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a decimal, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static decimal GetDecimalAttribute(this XmlNode node, string attrName, decimal defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToDecimal(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 64-bit floating point value, or 0 if the attribute was unspecified or empty.</summary>
  public static double GetDoubleAttribute(this XmlNode node, string attrName)
  {
    return GetDoubleAttribute(node, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 64-bit floating point value, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static double GetDoubleAttribute(this XmlNode node, string attrName, double defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToDouble(attrValue);
  }

  /// <summary>Returns the value of the named attribute as an <see cref="XmlDuration"/>, or
  /// an empty duration if the attribute was unspecified or empty.
  /// </summary>
  public static XmlDuration GetDurationAttribute(this XmlNode node, string attrName)
  {
    return GetDurationAttribute(node, attrName, XmlDuration.Zero);
  }

  /// <summary>Returns the value of the named attribute as a <see cref="XmlDuration"/>, or
  /// the given default value if the attribute was unspecified or empty.
  /// </summary>
  public static XmlDuration GetDurationAttribute(this XmlNode node, string attrName, XmlDuration defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlDuration.Parse(attrValue);
  }

  /// <summary>Returns the value of the named attribute as an enum value of type <typeparamref name="T"/>, or the default (zero) value
  /// if the attribute was unspecified or empty. The enum value will be matched case-insensitively.
  /// </summary>
  public static T GetEnumAttribute<T>(this XmlNode node, string attrName) where T : struct
  {
    return GetEnumAttribute<T>(node, attrName, default(T), true);
  }

  /// <summary>Returns the value of the named attribute as an enum value of type <typeparamref name="T"/>, or the given default value if
  /// the attribute was unspecified or empty. The enum value will be matched case-insensitively.
  /// </summary>
  public static T GetEnumAttribute<T>(this XmlNode node, string attrName, T defaultValue) where T : struct
  {
    return GetEnumAttribute<T>(node, attrName, defaultValue, true);
  }

  /// <summary>Returns the value of the named attribute as an enum value of type <typeparamref name="T"/>, or the default (zero) value if
  /// the attribute was unspecified or empty. The enum value will be matched case-insensitively if <paramref name="ignoreCase"/> is true.
  /// </summary>
  public static T GetEnumAttribute<T>(this XmlNode node, string attrName, bool ignoreCase) where T : struct
  {
    return GetEnumAttribute<T>(node, attrName, default(T), ignoreCase);
  }

  /// <summary>Returns the value of the named attribute as an enum value of type <typeparamref name="T"/>, or the given default value if
  /// the attribute was unspecified or empty. The enum value will be matched case-insensitively if <paramref name="ignoreCase"/> is true.
  /// </summary>
  public static T GetEnumAttribute<T>(this XmlNode node, string attrName, T defaultValue, bool ignoreCase) where T : struct
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : (T)Enum.Parse(typeof(T), attrValue, ignoreCase);
  }

  /// <summary>Returns the value of the named attribute as a <see cref="Guid"/>, or <see cref="Guid.Empty" />
  /// if the attribute was unspecified or empty.
  /// </summary>
  public static Guid GetGuidAttribute(this XmlNode node, string attrName)
  {
    return GetGuidAttribute(node, attrName, Guid.Empty);
  }

  /// <summary>Returns the value of the named attribute as a <see cref="Guid"/>, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static Guid GetGuidAttribute(this XmlNode node, string attrName, Guid defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToGuid(attrValue);
  }

  /// <summary>Searches the node and its ancestors for the given attribute and returns the first one found, or null if the attribute was
  /// not defined on the node or any ancestor.
  /// </summary>
  public static XmlAttribute GetInheritedAttributeNode(this XmlNode node, string qualifiedName)
  {
    if(node == null) throw new ArgumentNullException();
    while(node.NodeType != XmlNodeType.Document)
    {
      XmlAttribute attr = node.Attributes[qualifiedName];
      if(attr != null) return attr;
      node = node.ParentNode;
    }
    return null;
  }

  /// <summary>Searches the node and its ancestors for the given attribute and returns the first one found, or null if the attribute was
  /// not defined on the node or any ancestor.
  /// </summary>
  public static XmlAttribute GetInheritedAttributeNode(this XmlNode node, XmlQualifiedName qualifiedName)
  {
    if(node == null || qualifiedName == null) throw new ArgumentNullException();
    while(node.NodeType != XmlNodeType.Document)
    {
      XmlAttribute attr = node.Attributes[qualifiedName.Name, qualifiedName.Namespace];
      if(attr != null) return attr;
      node = node.ParentNode;
    }
    return null;
  }

  /// <summary>Searches the node and its ancestors for the given attribute and returns the value of the first one found, or null if the
  /// attribute was not defined on the node or any ancestor.
  /// </summary>
  public static string GetInheritedAttributeValue(this XmlNode node, string qualifiedName)
  {
    return GetInheritedAttributeValue(node, qualifiedName, null);
  }

  /// <summary>Searches the node and its ancestors for the given attribute and returns the value of the first one found, or the given
  /// default value if the attribute was not defined on the node or any ancestor.
  /// </summary>
  public static string GetInheritedAttributeValue(this XmlNode node, string qualifiedName, string defaultValue)
  {
    XmlAttribute attr = node.GetInheritedAttributeNode(qualifiedName);
    return attr == null ? defaultValue : attr.Value;
  }

  /// <summary>Searches the node and its ancestors for the given attribute and returns the value of the first one found, or null if the
  /// attribute was not defined on the node or any ancestor.
  /// </summary>
  public static string GetInheritedAttributeValue(this XmlNode node, XmlQualifiedName qualifiedName)
  {
    return GetInheritedAttributeValue(node, qualifiedName, null);
  }

  /// <summary>Searches the node and its ancestors for the given attribute and returns the value of the first one found, or the given
  /// default value if the attribute was not defined on the node or any ancestor.
  /// </summary>
  public static string GetInheritedAttributeValue(this XmlNode node, XmlQualifiedName qualifiedName, string defaultValue)
  {
    XmlAttribute attr = node.GetInheritedAttributeNode(qualifiedName);
    return attr == null ? defaultValue : attr.Value;
  }

  /// <summary>Returns the value of the named attribute as a 16-bit signed integer, or 0 if the attribute was unspecified or empty.</summary>
  public static short GetInt16Attribute(this XmlNode node, string attrName)
  {
    return GetInt16Attribute(node, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 16-bit signed integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static short GetInt16Attribute(this XmlNode node, string attrName, short defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToInt16(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 32-bit signed integer, or 0 if the attribute was unspecified or empty.</summary>
  public static int GetInt32Attribute(this XmlNode node, string attrName)
  {
    return GetInt32Attribute(node, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 32-bit signed integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static int GetInt32Attribute(this XmlNode node, string attrName, int defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToInt32(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 64-bit signed integer, or 0 if the attribute was unspecified or empty.</summary>
  public static long GetInt64Attribute(this XmlNode node, string attrName)
  {
    return GetInt64Attribute(node, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 64-bit signed integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static long GetInt64Attribute(this XmlNode node, string attrName, long defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToInt64(attrValue);
  }

  /// <summary>Returns the value of the named attribute, or null if the attribute was unspecified.</summary>
  public static string GetQualifiedAttributeValue(this XmlNode node, string localName, string namespaceUri)
  {
    XmlAttribute an = GetAttributeNode(node, localName, namespaceUri);
    return an == null ? null : an.Value;
  }

  /// <summary>Returns the value of the named attribute, or the given default value if the attribute was unspecified.</summary>
  public static string GetQualifiedAttributeValue(this XmlNode node, string localName, string namespaceUri, string defaultValue)
  {
    XmlAttribute an = GetAttributeNode(node, localName, namespaceUri);
    return an == null ? defaultValue : an.Value;
  }

  /// <summary>Returns the value of the named attribute as an 8-bit signed integer, or 0 if the attribute was unspecified or empty.</summary>
  [CLSCompliant(false)]
  public static sbyte GetSByteAttribute(this XmlNode node, string attrName)
  {
    return GetSByteAttribute(node, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as an 8-bit signed integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  [CLSCompliant(false)]
  public static sbyte GetSByteAttribute(this XmlNode node, string attrName, sbyte defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToSByte(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 32-bit floating point value, or 0 if the attribute was unspecified or empty.</summary>
  public static float GetSingleAttribute(this XmlNode node, string attrName)
  {
    return GetSingleAttribute(node, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 32-bit floating point value, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static float GetSingleAttribute(this XmlNode node, string attrName, float defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToSingle(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a string, or the empty string if the attribute was unspecified or empty.</summary>
  public static string GetStringAttribute(this XmlNode node, string attrName)
  {
    return GetStringAttribute(node, attrName, string.Empty);
  }

  /// <summary>Returns the value of the named attribute as a string, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static string GetStringAttribute(this XmlNode node, string attrName, string defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : attrValue;
  }

  /// <summary>Returns the value of the named attribute as a <see cref="TimeSpan"/>, or
  /// an empty timespan if the attribute was unspecified or empty.
  /// </summary>
  public static TimeSpan GetTimeSpanAttribute(this XmlNode node, string attrName)
  {
    return GetTimeSpanAttribute(node, attrName, new TimeSpan());
  }

  /// <summary>Returns the value of the named attribute as a <see cref="TimeSpan"/>, or
  /// the given default value if the attribute was unspecified or empty.
  /// </summary>
  public static TimeSpan GetTimeSpanAttribute(this XmlNode node, string attrName, TimeSpan defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToTimeSpan(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 16-bit unsigned integer, or 0 if the attribute was unspecified or empty.</summary>
  [CLSCompliant(false)]
  public static ushort GetUInt16Attribute(this XmlNode node, string attrName)
  {
    return GetUInt16Attribute(node, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 16-bit unsigned integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  [CLSCompliant(false)]
  public static ushort GetUInt16Attribute(this XmlNode node, string attrName, ushort defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToUInt16(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 32-bit unsigned integer, or 0 if the attribute was unspecified or empty.</summary>
  [CLSCompliant(false)]
  public static uint GetUInt32Attribute(this XmlNode node, string attrName)
  {
    return GetUInt32Attribute(node, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 32-bit unsigned integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  [CLSCompliant(false)]
  public static uint GetUInt32Attribute(this XmlNode node, string attrName, uint defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToUInt32(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 64-bit unsigned integer, or 0 if the attribute was unspecified or empty.</summary>
  [CLSCompliant(false)]
  public static ulong GetUInt64Attribute(this XmlNode node, string attrName)
  {
    return GetUInt64Attribute(node, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 64-bit unsigned integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  [CLSCompliant(false)]
  public static ulong GetUInt64Attribute(this XmlNode node, string attrName, ulong defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToUInt64(attrValue);
  }

  /// <summary>Enumerates the children of type <see cref="XmlNodeType.Element"/>.</summary>
  public static IEnumerable<XmlElement> EnumerateChildElements(this XmlNode node)
  {
    if(node == null) throw new ArgumentNullException();
    for(XmlElement elem = node.GetFirstChildElement(); elem != null; elem = elem.GetNextSiblingElement()) yield return elem;
  }

  /// <summary>Returns the first child node of type <see cref="XmlNodeType.Element"/>, or null if there is no such child node.</summary>
  public static XmlElement GetChildElement(this XmlNode node, XmlQualifiedName name)
  {
    if(node == null || name == null) throw new ArgumentNullException();
    XmlElement child = node.GetFirstChildElement();
    while(child != null && child.HasName(name)) child = child.GetNextSiblingElement();
    return child;
  }

  /// <summary>Returns the first child node of type <see cref="XmlNodeType.Element"/>, or null if there is no such child node.</summary>
  public static XmlElement GetChildElement(this XmlNode node, string localName)
  {
    if(node == null || string.IsNullOrEmpty(localName)) throw new ArgumentNullException();
    XmlElement child = node.GetFirstChildElement();
    while(child != null && child.LocalName != localName) child = child.GetNextSiblingElement();
    return child;
  }

  /// <summary>Returns the first child node of type <see cref="XmlNodeType.Element"/>, or null if there is no such child node.</summary>
  public static XmlElement GetFirstChildElement(this XmlNode node)
  {
    if(node == null) throw new ArgumentNullException();
    XmlNode child = node.FirstChild;
    while(child != null && child.NodeType != XmlNodeType.Element) child = child.NextSibling;
    return (XmlElement)child;
  }

  /// <summary>Returns the next sibling node of type <see cref="XmlNodeType.Element"/>, or null if there is no such node.</summary>
  public static XmlElement GetNextSiblingElement(this XmlNode node)
  {
    if(node == null) throw new ArgumentNullException();
    do node = node.NextSibling; while(node != null && node.NodeType != XmlNodeType.Element);
    return (XmlElement)node;
  }

  /// <summary>Returns the previous sibling node of type <see cref="XmlNodeType.Element"/>, or null if there is no such node.</summary>
  public static XmlElement GetPreviousSiblingElement(this XmlNode node)
  {
    if(node == null) throw new ArgumentNullException();
    do node = node.PreviousSibling; while(node != null && node.NodeType != XmlNodeType.Element);
    return (XmlElement)node;
  }

  /// <summary>Returns the trimmed value of the node's inner text, or the given default value if the value is empty.</summary>
  public static string GetTrimmedInnerText(this XmlNode node, string defaultValue)
  {
    string innerText = node.InnerText.Trim();
    return string.IsNullOrEmpty(innerText) ? defaultValue : innerText;
  }

  /// <summary>Returns the <see cref="XmlQualifiedName"/> for the node.</summary>
  public static XmlQualifiedName GetQualifiedName(this XmlNode node)
  {
    if(node == null) throw new ArgumentNullException();
    return new XmlQualifiedName(node.LocalName, node.NamespaceURI);
  }

  /// <summary>Returns true if the node contains any non-text children.</summary>
  public static bool HasComplexContent(this XmlNode node)
  {
    if(node == null) throw new ArgumentNullException();
    for(XmlNode child = node.FirstChild; child != null; child = child.NextSibling)
    {
      if(!child.IsTextNode()) return true;
    }
    return false;
  }

  /// <summary>Determines whether the qualified name of the node equals the given qualified name.</summary>
  public static bool HasName(this XmlNode node, XmlQualifiedName qname)
  {
    if(node == null || qname == null) throw new ArgumentNullException();
    return node.HasName(qname.Name, qname.Namespace);
  }

  /// <summary>Determines whether the qualified name of the node equals the given qualified name.</summary>
  public static bool HasName(this XmlNode node, string localName, string namespaceUri)
  {
    if(node == null) throw new ArgumentNullException();
    if(string.IsNullOrEmpty(localName) || namespaceUri == null)
    {
      throw new ArgumentException("Local name must not be empty and namespace URI must not be null.");
    }

    return localName.OrdinalEquals(node.LocalName) && namespaceUri.OrdinalEquals(node.NamespaceURI);
  }

  /// <summary>Returns true if the node contains text children and only text children. (This includes CDATA and whitespace.)
  /// This method returns false for empty elements. Although intended to be called on elements, this method also works for
  /// attributes, in which case it will return true if the attribute value is not empty.
  /// </summary>
  public static bool HasSimpleContent(this XmlNode node)
  {
    if(node == null) throw new ArgumentNullException();
    bool hasText = false;
    for(XmlNode child = node.FirstChild; child != null; child = child.NextSibling)
    {
      if(child.IsTextNode()) hasText = true;
      else return false;
    }
    return hasText || node.NodeType == XmlNodeType.Attribute && !string.IsNullOrEmpty(node.Value);
  }

  /// <summary>Returns true if the node contains text children and only text children - this includes CDATA and whitespace - and at
  /// least one of the children contains characters besides whitespace. This method returns false for empty elements.  Although intended
  /// to be called on elements, this method also works for attributes, in which case it will return true if the attribute value is not
  /// empty or whitespace.
  /// </summary>
  public static bool HasSimpleNonSpaceContent(this XmlNode node)
  {
    if(node == null) throw new ArgumentNullException();
    bool hasText = false;
    for(XmlNode child = node.FirstChild; child != null; child = child.NextSibling)
    {
      if(!child.IsTextNode()) return false;
      else if(!StringUtility.IsNullOrSpace(child.Value)) hasText = true;
    }
    return hasText || node.NodeType == XmlNodeType.Attribute && !StringUtility.IsNullOrSpace(node.Value);
  }

  /// <summary>Returns true if the attribute was unspecified or empty.</summary>
  public static bool IsAttributeEmpty(XmlAttribute attr)
  {
    return attr == null || string.IsNullOrEmpty(attr.Value);
  }

  /// <summary>Returns true if the attribute was unspecified or empty.</summary>
  public static bool IsAttributeEmpty(this XmlNode node, string attrName)
  {
    return IsAttributeEmpty(GetAttributeNode(node, attrName));
  }

  /// <summary>Returns true if the node represents some type of text content. (This returns true for <see cref="XmlNodeType.Text"/>,
  /// <see cref="XmlNodeType.Whitespace"/>, <see cref="XmlNodeType.SignificantWhitespace"/>, and <see cref="XmlNodeType.CDATA"/>.)
  /// </summary>
  public static bool IsTextNode(this XmlNode node)
  {
    if(node == null) throw new ArgumentNullException();
    XmlNodeType type = node.NodeType;
    return type == XmlNodeType.Text || type == XmlNodeType.CDATA || type == XmlNodeType.SignificantWhitespace ||
           type == XmlNodeType.Whitespace;
  }

  /// <summary>Parses an attribute whose value contains a whitespace-separated list of items into an array of strings containing
  /// the substrings corresponding to the individual items.
  /// </summary>
  public static string[] ParseListAttribute(this XmlNode node, string attrName)
  {
    return XmlUtility.ParseList(GetAttributeValue(node, attrName));
  }

  /// <summary>Parses an attribute whose value contains a whitespace-separated list of items into an array containing the
  /// corresponding items, using the given converter to convert an item's string representation into its value.
  /// </summary>
  public static T[] ParseListAttribute<T>(this XmlNode node, string attrName, Converter<string, T> converter)
  {
    return XmlUtility.ParseList(GetAttributeValue(node, attrName), converter);
  }

  /// <summary>Parses a qualified name (i.e. a name of the form <c>prefix:localName</c> or <c>namespaceUri:localName</c>) into an
  /// <see cref="XmlQualifiedName"/> in the context of the current node. This method also accepts local names.
  /// </summary>
  public static XmlQualifiedName ParseQualifiedName(this XmlNode node, string qualifiedName)
  {
    if(node == null) throw new ArgumentNullException();
    return string.IsNullOrEmpty(qualifiedName) ?
      XmlQualifiedName.Empty : XmlUtility.ParseQualifiedName(qualifiedName, node.GetNamespaceOfPrefix);
  }

  /// <summary>Removes all the child nodes of the given node.</summary>
  public static void RemoveChildren(this XmlNode node)
  {
    if(node == null) throw new ArgumentNullException();
    while(node.FirstChild != null) node.RemoveChild(node.FirstChild);
  }

  /// <summary>Removes the node from its parent and therefore from the document.</summary>
  public static void RemoveFromParent(this XmlNode node)
  {
    if(node == null) throw new ArgumentNullException();
    if(node.ParentNode != null) node.ParentNode.RemoveChild(node);
  }


  /// <summary>Returns the inner text of the node selected by the given XPath query as a boolean,
  /// or false if the node could not be found or was empty.
  /// </summary>
  public static bool SelectBool(this XmlNode node, string xpath)
  {
    return SelectBool(node, xpath, false);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a boolean,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  public static bool SelectBool(this XmlNode node, string xpath, bool defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToBoolean(stringValue);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a byte,
  /// or 0 if the node could not be found or was empty.
  /// </summary>
  public static byte SelectByte(this XmlNode node, string xpath)
  {
    return SelectByte(node, xpath, 0);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a byte,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  public static byte SelectByte(this XmlNode node, string xpath, byte defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToByte(stringValue);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a character,
  /// or the nul character if the node could not be found or was empty.
  /// </summary>
  public static char SelectChar(this XmlNode node, string xpath)
  {
    return SelectChar(node, xpath, '\0');
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a character,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  public static char SelectChar(this XmlNode node, string xpath, char defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToChar(stringValue);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a nullable <see cref="DateTime"/>,
  /// or null if the node could not be found or was empty.
  /// </summary>
  public static DateTime? SelectDateTime(this XmlNode node, string xpath)
  {
    return SelectDateTime(node, xpath, null);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a nullable <see cref="DateTime"/>,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  public static DateTime? SelectDateTime(this XmlNode node, string xpath, DateTime? defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ?
      defaultValue : XmlConvert.ToDateTime(stringValue, XmlDateTimeSerializationMode.Unspecified);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a decimal,
  /// or 0 if the node could not be found or was empty.
  /// </summary>
  public static decimal SelectDecimal(this XmlNode node, string xpath)
  {
    return SelectDecimal(node, xpath, 0);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a decimal,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  public static decimal SelectDecimal(this XmlNode node, string xpath, decimal defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToDecimal(stringValue);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 64-bit floating point value,
  /// or 0 if the node could not be found or was empty.
  /// </summary>
  public static double SelectDouble(this XmlNode node, string xpath)
  {
    return SelectDouble(node, xpath, 0);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 64-bit floating point value,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  public static double SelectDouble(this XmlNode node, string xpath, double defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToDouble(stringValue);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as an <see cref="XmlDuration"/>,
  /// or an empty duration if the node could not be found or was empty.
  /// </summary>
  public static XmlDuration SelectDuration(this XmlNode node, string xpath)
  {
    return SelectDuration(node, xpath, XmlDuration.Zero);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as an <see cref="XmlDuration"/>,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  public static XmlDuration SelectDuration(this XmlNode node, string xpath, XmlDuration defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlDuration.Parse(stringValue);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a <see cref="Guid"/>,
  /// or <see cref="Guid.Empty"/> if the node could not be found or was empty.
  /// </summary>
  public static Guid SelectGuid(this XmlNode node, string xpath)
  {
    return SelectGuid(node, xpath, Guid.Empty);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a <see cref="Guid"/>,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  public static Guid SelectGuid(this XmlNode node, string xpath, Guid defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToGuid(stringValue);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 16-bit signed integer,
  /// or 0 if the node could not be found or was empty.
  /// </summary>
  public static short SelectInt16(this XmlNode node, string xpath)
  {
    return SelectInt16(node, xpath, 0);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 16-bit signed integer,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  public static short SelectInt16(this XmlNode node, string xpath, short defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToInt16(stringValue);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 32-bit signed integer,
  /// or 0 if the node could not be found or was empty.
  /// </summary>
  public static int SelectInt32(this XmlNode node, string xpath)
  {
    return SelectInt32(node, xpath, 0);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 32-bit signed integer,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  public static int SelectInt32(this XmlNode node, string xpath, int defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToInt32(stringValue);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 64-bit signed integer,
  /// or 0 if the node could not be found or was empty.
  /// </summary>
  public static long SelectInt64(this XmlNode node, string xpath)
  {
    return SelectInt64(node, xpath, 0);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 64-bit signed integer,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  public static long SelectInt64(this XmlNode node, string xpath, long defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToInt64(stringValue);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as an 8-bit signed integer,
  /// or 0 if the node could not be found or was empty.
  /// </summary>
  [CLSCompliant(false)]
  public static sbyte SelectSByte(this XmlNode node, string xpath)
  {
    return SelectSByte(node, xpath, 0);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as an 8-bit signed integer,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  [CLSCompliant(false)]
  public static sbyte SelectSByte(this XmlNode node, string xpath, sbyte defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToSByte(stringValue);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 32-bit floating point value,
  /// or 0 if the node could not be found or was empty.
  /// </summary>
  public static float SelectSingle(this XmlNode node, string xpath)
  {
    return SelectSingle(node, xpath, 0);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 32-bit floating point value,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  public static float SelectSingle(this XmlNode node, string xpath, float defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToSingle(stringValue);
  }

  /// <summary>Returns the trimmed inner text of the node selected by the given XPath query,
  /// or an empty string if the node could not be found.
  /// </summary>
  public static string SelectString(this XmlNode node, string xpath)
  {
    return SelectString(node, xpath, string.Empty);
  }

  /// <summary>Returns the trimmed inner text of the node selected by the given XPath query,
  /// or the given default value if the node could not be found.
  /// </summary>
  public static string SelectString(this XmlNode node, string xpath, string defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : stringValue;
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a <see cref="TimeSpan"/>,
  /// or an empty timespan if the node could not be found or was empty.
  /// </summary>
  public static TimeSpan SelectTimeSpan(this XmlNode node, string xpath)
  {
    return SelectTimeSpan(node, xpath, new TimeSpan());
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a <see cref="TimeSpan"/>,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  public static TimeSpan SelectTimeSpan(this XmlNode node, string xpath, TimeSpan defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToTimeSpan(stringValue);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 16-bit unsigned integer,
  /// or 0 if the node could not be found or was empty.
  /// </summary>
  [CLSCompliant(false)]
  public static ushort SelectUInt16(this XmlNode node, string xpath)
  {
    return SelectUInt16(node, xpath, 0);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 16-bit unsigned integer,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  [CLSCompliant(false)]
  public static ushort SelectUInt16(this XmlNode node, string xpath, ushort defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToUInt16(stringValue);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 32-bit unsigned integer,
  /// or 0 if the node could not be found or was empty.
  /// </summary>
  [CLSCompliant(false)]
  public static uint SelectUInt32(this XmlNode node, string xpath)
  {
    return SelectUInt32(node, xpath, 0);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 32-bit unsigned integer,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  [CLSCompliant(false)]
  public static uint SelectUInt32(this XmlNode node, string xpath, uint defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToUInt32(stringValue);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 64-bit unsigned integer,
  /// or 0 if the node could not be found or was empty.
  /// </summary>
  [CLSCompliant(false)]
  public static ulong SelectUInt64(this XmlNode node, string xpath)
  {
    return SelectUInt64(node, xpath, 0);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 64-bit unsigned integer,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  [CLSCompliant(false)]
  public static ulong SelectUInt64(this XmlNode node, string xpath, ulong defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToUInt64(stringValue);
  }

  /// <summary>Returns the trimmed inner text of the node selected by the given XPath query,
  /// or null if the node could not be found.
  /// </summary>
  public static string SelectValue(this XmlNode node, string xpath)
  {
    return SelectValue(node, xpath, null);
  }

  /// <summary>Returns the trimmed inner text of the node selected by the given XPath query, or
  /// the given default value if the node could not be found.
  /// </summary>
  public static string SelectValue(this XmlNode node, string xpath, string defaultValue)
  {
    if(node == null) return defaultValue;
    XmlNode selectedNode = node.SelectSingleNode(xpath);
    return selectedNode == null ? defaultValue : selectedNode.InnerText.Trim();
  }

  /// <summary>Gets the named <see cref="XmlAttribute"/> from the given node, or null if the node is null.</summary>
  static XmlAttribute GetAttributeNode(this XmlNode node, string attrName)
  {
    return node == null || node.Attributes == null ? null : node.Attributes[attrName];
  }

  /// <summary>Gets the named <see cref="XmlAttribute"/> from the given node, or null if the node is null.</summary>
  static XmlAttribute GetAttributeNode(this XmlNode node, string localName, string namespaceUri)
  {
    return node == null || node.Attributes == null ? null : node.Attributes[localName, namespaceUri];
  }
}
#endregion

#region XmlQualifiedNameExtensions
/// <summary>Provides useful extensions to the <see cref="XmlQualifiedName"/> class.</summary>
public static class XmlQualifiedNameExtensions
{
  /// <summary>Converts an <see cref="XmlQualifiedName"/> into a <c>localName</c>, <c>prefix:localName</c> <c>namespaceUri:localName</c>
  /// form valid in the context of the given node.
  /// </summary>
  public static string ToString(this XmlQualifiedName qname, XmlNode context)
  {
    if(qname == null || context == null) throw new ArgumentNullException();

    // if qname is not actually a qualified name, we can't necessarily translate it
    if(string.IsNullOrEmpty(qname.Namespace))
    {
      if(!string.IsNullOrEmpty(context.GetNamespaceOfPrefix(""))) // if simply using the local name wouldn't result in the same thing...
      {
        throw new ArgumentException("The qname has no namespace, and a default namespace has been set in the given context.");
      }
      return qname.Name;
    }

    string prefix = context.GetPrefixOfNamespace(qname.Namespace);
    if(string.IsNullOrEmpty(prefix))
    {
      // unfortunately, the method returns an empty string for both the default namespace and an undeclared namespace. if it's actually
      // undeclared, then use the namespace URI
      if(!qname.Namespace.OrdinalEquals(context.GetNamespaceOfPrefix(""))) prefix = qname.Namespace;
    }
    return string.IsNullOrEmpty(prefix) ? qname.Name : prefix + ":" + qname.Name;
  }

  /// <summary>Converts an <see cref="XmlQualifiedName"/> into a <c>localName</c>, <c>prefix:localName</c> <c>namespaceUri:localName</c>
  /// form valid in the context of the given namespace resolver.
  /// </summary>
  public static string ToString(this XmlQualifiedName qname, IXmlNamespaceResolver resolver)
  {
    if(qname == null || resolver == null) throw new ArgumentNullException();

    // if qname is not actually a qualified name, we can't necessarily translate it
    if(string.IsNullOrEmpty(qname.Namespace))
    {
      if(!string.IsNullOrEmpty(resolver.LookupNamespace(""))) // if simply using the local name wouldn't result in the same thing...
      {
        throw new ArgumentException("The qname has no namespace, and a default namespace has been set in the given context.");
      }
      return qname.Name;
    }

    string prefix = resolver.LookupPrefix(qname.Namespace);
    if(prefix == null) prefix = qname.Namespace;
    return string.IsNullOrEmpty(prefix) ? qname.Name : prefix + ":" + qname.Name;
  }

  /// <summary>Ensures that the given qualified name has a valid namespace URI and local name. Empty names are allowed.</summary>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="qname"/> is null.</exception>
  /// <exception cref="FormatException">Thrown if <paramref name="qname"/> does not have a valid format.</exception>
  public static void Validate(this XmlQualifiedName qname)
  {
    if(qname == null) throw new ArgumentNullException();
    try
    {
      if(!string.IsNullOrEmpty(qname.Namespace)) new Uri(qname.Namespace, UriKind.Absolute);
      if(!string.IsNullOrEmpty(qname.Name)) XmlConvert.VerifyNCName(qname.Name);
    }
    catch(UriFormatException ex)
    {
      throw new FormatException("The QName " + qname.ToString() + " does not have a valid namespace URI. " + ex.Message);
    }
    catch(XmlException ex)
    {
      throw new FormatException("The QName " + qname.ToString() + " does not have a valid local name. " + ex.Message);
    }
  }
}
#endregion

#region XmlReaderExtensions
/// <summary>Provides extensions to the <see cref="XmlReader"/> class.</summary>
public static class XmlReaderExtensions
{
  /// <summary>Returns the value of the named attribute, or <c>default(T)</c> if the attribute was unspecified.</summary>
  public static T GetAttribute<T>(this XmlReader reader, string attrName, Converter<string, T> converter)
  {
    return GetAttribute<T>(reader, attrName, converter, default(T));
  }

  /// <summary>Returns the value of the named attribute, or the given default value if the attribute was unspecified.</summary>
  public static T GetAttribute<T>(this XmlReader reader, string attrName, Converter<string, T> converter,
                                  T defaultValue)
  {
    if(reader == null || converter == null) throw new ArgumentNullException();
    string value = reader.GetAttribute(attrName);
    return value == null ? defaultValue : converter(value);
  }

  /// <summary>Returns the value of the named attribute as a boolean, or false if the attribute was unspecified or empty.</summary>
  public static bool GetBoolAttribute(this XmlReader reader, string attrName)
  {
    return GetBoolAttribute(reader, attrName, false);
  }

  /// <summary>Returns the value of the named attribute as a boolean, or the given
  /// default value if the attribute was unspecified or empty.
  /// </summary>
  public static bool GetBoolAttribute(this XmlReader reader, string attrName, bool defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToBoolean(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a byte, or 0 if the attribute was unspecified or empty.</summary>
  public static byte GetByteAttribute(this XmlReader reader, string attrName)
  {
    return GetByteAttribute(reader, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a byte, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static byte GetByteAttribute(this XmlReader reader, string attrName, byte defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToByte(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a character, or the nul character if the attribute was unspecified or empty.</summary>
  public static char GetCharAttribute(this XmlReader reader, string attrName)
  {
    return GetCharAttribute(reader, attrName, '\0');
  }

  /// <summary>Returns the value of the named attribute as a character, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static char GetCharAttribute(this XmlReader reader, string attrName, char defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToChar(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a nullable datetime, or null if the attribute was unspecified or empty.</summary>
  public static DateTime? GetDateTimeAttribute(this XmlReader reader, string attrName)
  {
    return GetDateTimeAttribute(reader, attrName, (DateTime?)null);
  }

  /// <summary>Returns the value of the named attribute as a nullable datetime, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static DateTime? GetDateTimeAttribute(this XmlReader reader, string attrName, DateTime? defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ?
      defaultValue : XmlConvert.ToDateTime(attrValue, XmlDateTimeSerializationMode.Unspecified);
  }

  /// <summary>Returns the value of the named attribute as a decimal, or 0 if the attribute was unspecified or empty.</summary>
  public static decimal GetDecimalAttribute(this XmlReader reader, string attrName)
  {
    return GetDecimalAttribute(reader, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a decimal, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static decimal GetDecimalAttribute(this XmlReader reader, string attrName, decimal defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToDecimal(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 64-bit floating point value, or 0 if the attribute was unspecified or empty.</summary>
  public static double GetDoubleAttribute(this XmlReader reader, string attrName)
  {
    return GetDoubleAttribute(reader, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 64-bit floating point value, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static double GetDoubleAttribute(this XmlReader reader, string attrName, double defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToDouble(attrValue);
  }

  /// <summary>Returns the value of the named attribute as an enum value of type <typeparamref name="T"/>, or the default (zero) value
  /// if the attribute was unspecified or empty. The enum value will be matched case-insensitively.
  /// </summary>
  public static T GetEnumAttribute<T>(this XmlReader reader, string attrName) where T : struct
  {
    return GetEnumAttribute<T>(reader, attrName, default(T), true);
  }

  /// <summary>Returns the value of the named attribute as an enum value of type <typeparamref name="T"/>, or the given default value if
  /// the attribute was unspecified or empty. The enum value will be matched case-insensitively.
  /// </summary>
  public static T GetEnumAttribute<T>(this XmlReader reader, string attrName, T defaultValue) where T : struct
  {
    return GetEnumAttribute<T>(reader, attrName, defaultValue, true);
  }

  /// <summary>Returns the value of the named attribute as an enum value of type <typeparamref name="T"/>, or the default (zero) value if
  /// the attribute was unspecified or empty. The enum value will be matched case-insensitively if <paramref name="ignoreCase"/> is true.
  /// </summary>
  public static T GetEnumAttribute<T>(this XmlReader reader, string attrName, bool ignoreCase) where T : struct
  {
    return GetEnumAttribute<T>(reader, attrName, default(T), ignoreCase);
  }

  /// <summary>Returns the value of the named attribute as an enum value of type <typeparamref name="T"/>, or the given default value if
  /// the attribute was unspecified or empty. The enum value will be matched case-insensitively if <paramref name="ignoreCase"/> is true.
  /// </summary>
  public static T GetEnumAttribute<T>(this XmlReader reader, string attrName, T defaultValue, bool ignoreCase) where T : struct
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : (T)Enum.Parse(typeof(T), attrValue, ignoreCase);
  }

  /// <summary>Returns the value of the named attribute as a <see cref="Guid"/>, or <see cref="Guid.Empty" />
  /// if the attribute was unspecified or empty.
  /// </summary>
  public static Guid GetGuidAttribute(this XmlReader reader, string attrName)
  {
    return GetGuidAttribute(reader, attrName, Guid.Empty);
  }

  /// <summary>Returns the value of the named attribute as a <see cref="Guid"/>, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static Guid GetGuidAttribute(this XmlReader reader, string attrName, Guid defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToGuid(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 16-bit signed integer, or 0 if the attribute was unspecified or empty.</summary>
  public static short GetInt16Attribute(this XmlReader reader, string attrName)
  {
    return GetInt16Attribute(reader, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 16-bit signed integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static short GetInt16Attribute(this XmlReader reader, string attrName, short defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToInt16(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 32-bit signed integer, or 0 if the attribute was unspecified or empty.</summary>
  public static int GetInt32Attribute(this XmlReader reader, string attrName)
  {
    return GetInt32Attribute(reader, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 32-bit signed integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static int GetInt32Attribute(this XmlReader reader, string attrName, int defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToInt32(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 64-bit signed integer, or 0 if the attribute was unspecified or empty.</summary>
  public static long GetInt64Attribute(this XmlReader reader, string attrName)
  {
    return GetInt64Attribute(reader, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 64-bit signed integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static long GetInt64Attribute(this XmlReader reader, string attrName, long defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToInt64(attrValue);
  }

  /// <summary>Returns the <see cref="XmlQualifiedName"/> for reader's current element.</summary>
  public static XmlQualifiedName GetQualifiedName(this XmlReader reader)
  {
    if(reader == null) throw new ArgumentNullException();
    if(reader.NodeType != XmlNodeType.Element && reader.NodeType != XmlNodeType.EndElement)
    {
      throw new InvalidOperationException("The reader must be positioned on an element.");
    }
    return new XmlQualifiedName(reader.LocalName, reader.NamespaceURI);
  }

  /// <summary>Returns the value of the named attribute as an 8-bit signed integer, or 0 if the attribute was unspecified or empty.</summary>
  [CLSCompliant(false)]
  public static sbyte GetSByteAttribute(this XmlReader reader, string attrName)
  {
    return GetSByteAttribute(reader, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as an 8-bit signed integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  [CLSCompliant(false)]
  public static sbyte GetSByteAttribute(this XmlReader reader, string attrName, sbyte defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToSByte(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 32-bit floating point value, or 0 if the attribute was unspecified or empty.</summary>
  public static float GetSingleAttribute(this XmlReader reader, string attrName)
  {
    return GetSingleAttribute(reader, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 32-bit floating point value, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static float GetSingleAttribute(this XmlReader reader, string attrName, float defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToSingle(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a string, or the empty string if the attribute was unspecified or empty.</summary>
  public static string GetStringAttribute(this XmlReader reader, string attrName)
  {
    return GetStringAttribute(reader, attrName, string.Empty);
  }

  /// <summary>Returns the value of the named attribute as a string, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static string GetStringAttribute(this XmlReader reader, string attrName, string defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : attrValue;
  }

  /// <summary>Returns the value of the named attribute as a <see cref="TimeSpan"/>, or
  /// an empty timespan if the attribute was unspecified or empty.
  /// </summary>
  public static TimeSpan GetTimeSpanAttribute(this XmlReader reader, string attrName)
  {
    return GetTimeSpanAttribute(reader, attrName, new TimeSpan());
  }

  /// <summary>Returns the value of the named attribute as a <see cref="TimeSpan"/>, or
  /// the given default value if the attribute was unspecified or empty.
  /// </summary>
  public static TimeSpan GetTimeSpanAttribute(this XmlReader reader, string attrName, TimeSpan defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToTimeSpan(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 16-bit unsigned integer, or 0 if the attribute was unspecified or empty.</summary>
  [CLSCompliant(false)]
  public static ushort GetUInt16Attribute(this XmlReader reader, string attrName)
  {
    return GetUInt16Attribute(reader, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 16-bit unsigned integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  [CLSCompliant(false)]
  public static ushort GetUInt16Attribute(this XmlReader reader, string attrName, ushort defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToUInt16(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 32-bit unsigned integer, or 0 if the attribute was unspecified or empty.</summary>
  [CLSCompliant(false)]
  public static uint GetUInt32Attribute(this XmlReader reader, string attrName)
  {
    return GetUInt32Attribute(reader, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 32-bit unsigned integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  [CLSCompliant(false)]
  public static uint GetUInt32Attribute(this XmlReader reader, string attrName, uint defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToUInt32(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 64-bit unsigned integer, or 0 if the attribute was unspecified or empty.</summary>
  [CLSCompliant(false)]
  public static ulong GetUInt64Attribute(this XmlReader reader, string attrName)
  {
    return GetUInt64Attribute(reader, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 64-bit unsigned integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  [CLSCompliant(false)]
  public static ulong GetUInt64Attribute(this XmlReader reader, string attrName, ulong defaultValue)
  {
    string attrValue = GetAttributeValue(reader, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToUInt64(attrValue);
  }

  /// <summary>Determines whether the qualified name of the current element equals the given qualified name.</summary>
  public static bool HasName(this XmlReader reader, XmlQualifiedName qname)
  {
    if(qname == null) throw new ArgumentNullException();
    return reader.HasName(qname.Name, qname.Namespace);
  }

  /// <summary>Determines whether the qualified name of the current element equals the given qualified name.</summary>
  public static bool HasName(this XmlReader reader, string localName, string namespaceUri)
  {
    if(reader == null) throw new ArgumentNullException();
    if(string.IsNullOrEmpty(localName) || namespaceUri == null)
    {
      throw new ArgumentException("Local name must not be empty and namespace URI must not be null.");
    }

    return localName.OrdinalEquals(reader.LocalName) && namespaceUri.OrdinalEquals(reader.NamespaceURI);
  }

  /// <summary>Parses a qualified name (i.e. a name of the form <c>prefix:localName</c> or <c>namespaceUri:localName</c>) into an
  /// <see cref="XmlQualifiedName"/> in the context of the current reader. This method also accepts local names.
  /// </summary>
  public static XmlQualifiedName ParseQualifiedName(this XmlReader reader, string qualifiedName)
  {
    if(reader == null) throw new ArgumentNullException();
    return string.IsNullOrEmpty(qualifiedName) ?
      XmlQualifiedName.Empty : XmlUtility.ParseQualifiedName(qualifiedName, reader.LookupNamespace);
  }

  /// <summary>Calls <see cref="XmlReader.Read"/> until <see cref="XmlReader.NodeType"/> is no longer equal to
  /// <see cref="XmlNodeType.Whitespace"/>, and returns the value of the last call to <see cref="XmlReader.Read"/>.
  /// </summary>
  public static bool ReadPastWhitespace(this XmlReader reader)
  {
    if(reader == null) throw new ArgumentNullException();
    while(reader.Read())
    {
      if(reader.NodeType != XmlNodeType.Whitespace) return true;
    }
    return false;
  }

  /// <summary>Skips the children of the current element, without skipping the end element.</summary>
  public static void SkipChildren(this XmlReader reader)
  {
    if(reader == null) throw new ArgumentNullException();
    if(reader.NodeType != XmlNodeType.Element) throw new InvalidOperationException();
    if(!reader.IsEmptyElement)
    {
      reader.Read();
      while(reader.NodeType != XmlNodeType.EndElement) reader.Skip();
    }
  }

  /// <summary>Skips nodes that are not <see cref="XmlNodeType.EndElement"/> nodes or empty elements.</summary>
  public static void SkipToEnd(this XmlReader reader)
  {
    if(reader == null) throw new ArgumentNullException();
    while(reader.NodeType != XmlNodeType.EndElement && !reader.IsEmptyElement) reader.Skip();
  }

  /// <summary>Skips <see cref="XmlNodeType.Whitespace"/> nodes (but not <see cref="XmlNodeType.SignificantWhitespace"/> nodes).</summary>
  public static void SkipWhiteSpace(this XmlReader reader)
  {
    if(reader == null) throw new ArgumentNullException();
    while(reader.NodeType == XmlNodeType.Whitespace) reader.Read();
  }

  /// <summary>Returns the value of the named attribute, or null if the attribute was unspecified.</summary>
  static string GetAttributeValue(this XmlReader reader, string attrName)
  {
    if(reader == null) throw new ArgumentNullException();
    return reader.GetAttribute(attrName);
  }
}
#endregion

#region XmlWriterExtensions
/// <summary>Provides extensions to the <see cref="XmlWriter"/> class.</summary>
public static class XmlWriterExtensions
{
  /// <summary>Writes the named attribute with content based on a boolean.</summary>
  public static void WriteAttribute(this XmlWriter writer, string localName, bool value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named attribute with content based on a byte.</summary>
  public static void WriteAttribute(this XmlWriter writer, string localName, byte value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named attribute with content based on a character.</summary>
  public static void WriteAttribute(this XmlWriter writer, string localName, char value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named attribute with content based on a <see cref="DateTime"/>. The <see cref="DateTimeKind"/> of the
  /// <see cref="DateTime"/> will be preserved.
  /// </summary>
  public static void WriteAttribute(this XmlWriter writer, string localName, DateTime value)
  {
    writer.WriteAttribute(localName, value, XmlDateTimeSerializationMode.RoundtripKind);
  }

  /// <summary>Writes the named attribute with content based on a <see cref="DateTime"/>.</summary>
  public static void WriteAttribute(this XmlWriter writer, string localName, DateTime value, XmlDateTimeSerializationMode mode)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, XmlConvert.ToString(value, mode));
  }

  /// <summary>Writes the named attribute with content based on a <see cref="Decimal"/>.</summary>
  public static void WriteAttribute(this XmlWriter writer, string localName, decimal value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named attribute with content based on a 64-bit floating point value.</summary>
  public static void WriteAttribute(this XmlWriter writer, string localName, double value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named attribute with content based on a <see cref="Guid"/>.</summary>
  public static void WriteAttribute(this XmlWriter writer, string localName, Guid value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named attribute with content based on a 16-bit integer.</summary>
  public static void WriteAttribute(this XmlWriter writer, string localName, short value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named attribute with content based on a 32-bit integer.</summary>
  public static void WriteAttribute(this XmlWriter writer, string localName, int value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named attribute with content based on a 64-bit integer.</summary>
  public static void WriteAttribute(this XmlWriter writer, string localName, long value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named attribute with content based on an 8-bit integer.</summary>
  [CLSCompliant(false)]
  public static void WriteAttribute(this XmlWriter writer, string localName, sbyte value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named attribute with content based on an 32-bit floating point value.</summary>
  public static void WriteAttribute(this XmlWriter writer, string localName, float value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named attribute with content based on an <see cref="TimeSpan"/>.</summary>
  public static void WriteAttribute(this XmlWriter writer, string localName, TimeSpan value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named attribute with content based on a 16-bit unsigned integer.</summary>
  [CLSCompliant(false)]
  public static void WriteAttribute(this XmlWriter writer, string localName, ushort value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named attribute with content based on a 32-bit unsigned integer.</summary>
  [CLSCompliant(false)]
  public static void WriteAttribute(this XmlWriter writer, string localName, uint value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named attribute with content based on a 64-bit unsigned integer.</summary>
  [CLSCompliant(false)]
  public static void WriteAttribute(this XmlWriter writer, string localName, ulong value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes an element with content based on an <see cref="XmlDuration"/> value.</summary>
  public static void WriteAttribute(this XmlWriter writer, string localName, XmlDuration value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteAttributeString(localName, value.ToString());
  }

  /// <summary>Writes an attribute with the given qualified name and value.</summary>
  public static void WriteAttributeString(this XmlWriter writer, XmlQualifiedName qname, string value)
  {
    if(writer == null || qname == null) throw new ArgumentNullException();
    writer.WriteAttributeString(qname.Name, qname.Namespace, value);
  }

  /// <summary>Writes an element with content based on the date portion of a <see cref="DateTime"/> value.</summary>
  public static void WriteDate(this XmlWriter writer, DateTime date)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteString(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
  }

  /// <summary>Writes an element with content based on the date portion of a <see cref="DateTime"/> value.</summary>
  public static void WriteDateElement(this XmlWriter writer, string localName, DateTime date)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
  }

  /// <summary>Writes the named element with content based on a boolean.</summary>
  public static void WriteElement(this XmlWriter writer, string localName, bool value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named element with content based on a byte.</summary>
  public static void WriteElement(this XmlWriter writer, string localName, byte value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named element with content based on a character.</summary>
  public static void WriteElement(this XmlWriter writer, string localName, char value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named element with content based on a <see cref="DateTime"/>. The <see cref="DateTimeKind"/> of the
  /// <see cref="DateTime"/> will be preserved.
  /// </summary>
  public static void WriteElement(this XmlWriter writer, string localName, DateTime value)
  {
    writer.WriteElement(localName, value, XmlDateTimeSerializationMode.RoundtripKind);
  }

  /// <summary>Writes the named element with content based on a <see cref="DateTime"/>.</summary>
  public static void WriteElement(this XmlWriter writer, string localName, DateTime value, XmlDateTimeSerializationMode mode)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(value, mode));
  }

  /// <summary>Writes the named element with content based on a <see cref="Decimal"/>.</summary>
  public static void WriteElement(this XmlWriter writer, string localName, decimal value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named element with content based on a 64-bit floating point value.</summary>
  public static void WriteElement(this XmlWriter writer, string localName, double value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named element with content based on a <see cref="Guid"/>.</summary>
  public static void WriteElement(this XmlWriter writer, string localName, Guid value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named element with content based on a 16-bit integer.</summary>
  public static void WriteElement(this XmlWriter writer, string localName, short value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named element with content based on a 32-bit integer.</summary>
  public static void WriteElement(this XmlWriter writer, string localName, int value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named element with content based on a 64-bit integer.</summary>
  public static void WriteElement(this XmlWriter writer, string localName, long value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named element with content based on an 8-bit integer.</summary>
  [CLSCompliant(false)]
  public static void WriteElement(this XmlWriter writer, string localName, sbyte value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named element with content based on an 32-bit floating point value.</summary>
  public static void WriteElement(this XmlWriter writer, string localName, float value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named element with content based on an <see cref="TimeSpan"/>.</summary>
  public static void WriteElement(this XmlWriter writer, string localName, TimeSpan value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named element with content based on a 16-bit unsigned integer.</summary>
  [CLSCompliant(false)]
  public static void WriteElement(this XmlWriter writer, string localName, ushort value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named element with content based on a 32-bit unsigned integer.</summary>
  [CLSCompliant(false)]
  public static void WriteElement(this XmlWriter writer, string localName, uint value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes the named element with content based on a 64-bit unsigned integer.</summary>
  [CLSCompliant(false)]
  public static void WriteElement(this XmlWriter writer, string localName, ulong value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(value));
  }

  /// <summary>Writes an element with content based on an <see cref="XmlDuration"/> value.</summary>
  public static void WriteElement(this XmlWriter writer, string localName, XmlDuration value)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, value.ToString());
  }

  /// <summary>Writes an element with the given name and value.</summary>
  public static void WriteElementString(this XmlWriter writer, XmlQualifiedName qname, string value)
  {
    if(writer == null || qname == null) throw new ArgumentNullException();
    writer.WriteElementString(qname.Name, qname.Namespace, value);
  }

  /// <summary>Writes an empty element with the given qualified name. Attributes cannot be added to the element.</summary>
  public static void WriteEmptyElement(this XmlWriter writer, string localName)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteStartElement(localName);
    writer.WriteEndElement();
  }

  /// <summary>Writes an empty element with the given qualified name. Attributes cannot be added to the element.</summary>
  public static void WriteEmptyElement(this XmlWriter writer, string localName, string ns)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteStartElement(localName, ns);
    writer.WriteEndElement();
  }

  /// <summary>Writes an empty element with the given qualified name. Attributes cannot be added to the element.</summary>
  public static void WriteEmptyElement(this XmlWriter writer, string prefix, string localName, string ns)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteStartElement(prefix, localName, ns);
    writer.WriteEndElement();
  }

  /// <summary>Writes an empty element with the given qualified name. Attributes cannot be added to the element.</summary>
  public static void WriteEmptyElement(this XmlWriter writer, XmlQualifiedName qname)
  {
    if(writer == null || qname == null) throw new ArgumentNullException();
    writer.WriteStartElement(qname.Name, qname.Namespace);
    writer.WriteEndElement();
  }

  /// <summary>Writes the specified qualified name, using an appropriate prefix for its namespace.</summary>
  public static void WriteQualifiedName(this XmlWriter writer, XmlQualifiedName qname)
  {
    if(writer == null || qname == null) throw new ArgumentNullException();
    writer.WriteQualifiedName(qname.Name, qname.Namespace);
  }

  /// <summary>Writes the start of an attribute with the specified qualified name.</summary>
  public static void WriteStartAttribute(this XmlWriter writer, XmlQualifiedName qname)
  {
    if(writer == null || qname == null) throw new ArgumentNullException();
    writer.WriteStartAttribute(qname.Name, qname.Namespace);
  }

  /// <summary>Writes a start tag with the specified qualified name.</summary>
  public static void WriteStartElement(this XmlWriter writer, XmlQualifiedName qname)
  {
    if(writer == null || qname == null) throw new ArgumentNullException();
    writer.WriteStartElement(qname.Name, qname.Namespace);
  }

  /// <summary>Writes an element with content based on the time portion of a <see cref="DateTime"/> value. The
  /// <see cref="DateTimeKind"/> of the <see cref="DateTime"/> will be preserved.
  /// </summary>
  public static void WriteTimeElement(this XmlWriter writer, string localName, DateTime time)
  {
    writer.WriteTimeElement(localName, time, XmlDateTimeSerializationMode.RoundtripKind);
  }

  /// <summary>Writes an element with content based on the time portion of a <see cref="DateTime"/> value. The
  /// <see cref="DateTimeKind"/> of the <see cref="DateTime"/> will be preserved.
  /// </summary>
  public static void WriteTimeElement(this XmlWriter writer, string localName, DateTime time, XmlDateTimeSerializationMode mode)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.WriteElementString(localName, XmlConvert.ToString(time, mode).Substring(11)); // strip off the date portion
  }
}
#endregion

#region XmlUtility
/// <summary>Provides utilities for reading and writing XML.</summary>
public static class XmlUtility
{
  /// <summary>Converts a string into an <c>xs:normalizedString</c> value.</summary>
  public static string NormalizeString(string value)
  {
    return NormalizeString(value, false);
  }

  /// <summary>Converts a string into an <c>xs:TOKEN</c> value.</summary>
  public static string NormalizeToken(string value)
  {
    return NormalizeString(value, true);
  }

  /// <summary>Parses a string containing a whitespace-separated list of items into an array of strings containing the substrings
  /// corresponding to the individual items.
  /// </summary>
  public static string[] ParseList(string listValue)
  {
    if(listValue != null) listValue = listValue.Trim();
    return string.IsNullOrEmpty(listValue) ? new string[0] : reListSplit.Split(listValue);
  }

  /// <summary>Parses a string containing a whitespace-separated list of items into an array containing the corresponding items,
  /// using the given converter to convert an item's string representation into its value.
  /// </summary>
  public static T[] ParseList<T>(string listValue, Converter<string, T> converter)
  {
    if(converter == null) throw new ArgumentNullException("converter");
    if(listValue != null) listValue = listValue.Trim();
    if(string.IsNullOrEmpty(listValue))
    {
      return new T[0];
    }
    else
    {
      string[] bits = reListSplit.Split(listValue);
      T[] values = new T[bits.Length];
      for(int i=0; i<values.Length; i++) values[i] = converter(bits[i]);
      return values;
    }
  }

  /// <summary>Parses an <c>xs:date</c>, <c>xs:dateTime</c> value, preserving the time zone information it contains.</summary>
  /// <param name="dateStr">The value, in <c>xs:date</c> or <c>xs:dateTime</c> format.</param>
  /// <returns>Returns either a <see cref="DateTime"/> or <see cref="DateTimeOffset"/> value. If no time zone information is given, the
  /// value will be a <see cref="DateTime"/> with a <see cref="DateTime.Kind"/> of <see cref="DateTimeKind.Unspecified"/>. If the UTC time
  /// zone (<c>Z</c>) is given, the value will be a <see cref="DateTime"/> with a <see cref="DateTime.Kind"/> of
  /// <see cref="DateTimeKind.Utc"/>. If any other time zone is given (including if the time zone is <c>+00:00</c> or <c>-00:00</c>), the
  /// value will be a <see cref="DateTimeOffset"/> representing a local time in the given time zone.
  /// </returns>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="dateStr"/> is null.</exception>
  /// <exception cref="FormatException">Thrown if <paramref name="dateStr"/> is not in the form required by the XML Schema specification.</exception>
  public static object ParseDateTime(string dateStr)
  {
    if(dateStr == null) throw new ArgumentNullException();
    object value;
    if(!TryParseDateTime(dateStr, out value)) throw new FormatException();
    return value;
  }

  /// <summary>Parses a qualified name (i.e. a name of the form <c>prefix:localName</c> or <c>namespaceUri:localName</c>) into an
  /// <see cref="XmlQualifiedName"/>, using the given function to resolve prefixes. This method also accepts local names.
  /// </summary>
  public static XmlQualifiedName ParseQualifiedName(string qualifiedName, Func<string,string> prefixToNamespace)
  {
    if(prefixToNamespace == null) throw new ArgumentNullException();
    if(string.IsNullOrEmpty(qualifiedName)) return XmlQualifiedName.Empty;
    int start, length, colon = qualifiedName.LastIndexOf(':');
    qualifiedName.Trim(out start, out length);
    string prefix = colon == -1 ? "" : qualifiedName.Substring(start, colon-start), ns = prefixToNamespace(prefix);
    string localName = colon == -1 ? qualifiedName : qualifiedName.Substring(colon+1, start+length-(colon+1));
    return new XmlQualifiedName(localName, string.IsNullOrEmpty(ns) ? prefix : ns);
  }

  /// <summary>Tries to parse an <c>xs:boolean</c> value. Returns true if a boolean value was successfully parsed and false otherwise.</summary>
  public static bool TryParse(string boolStr, out bool value)
  {
    if(!string.IsNullOrEmpty(boolStr))
    {
      int start, length;
      boolStr.Trim(out start, out length);
      char c = boolStr[start];
      if(length == 1)
      {
        value = c == '1';
        return value || c == '0';
      }
      else if(length == 4 && string.Compare(boolStr, start, "true", 0, 4, StringComparison.Ordinal) == 0)
      {
        value = true;
        return true;
      }
      else if(length == 5 && string.Compare(boolStr, start, "false", 0, 5, StringComparison.Ordinal) == 0)
      {
        value = false;
        return true;
      }
    }

    value = false;
    return false;
  }

  /// <summary>Tries to parse an <c>xs:date</c>, <c>xs:dateTime</c>, or <c>xs:datetimeoffset</c> value. <c>xs:datetimeoffset</c> values
  /// will be returned in local time if the offset matches the local time offset, and will be converted into UTC otherwise.
  /// </summary>
  /// <returns>Returns true if the value was successfully parsed and false if not.</returns>
  public static bool TryParse(string dateStr, out DateTime dateTime)
  {
    object value;
    if(TryParseDateTime(dateStr, out value))
    {
      if(value is DateTime)
      {
        dateTime = (DateTime)value;
      }
      else
      {
        DateTimeOffset offset = (DateTimeOffset)value;
        dateTime = offset.DateTime == offset.LocalDateTime ? offset.LocalDateTime : offset.UtcDateTime;
      }
      return true;
    }
    else
    {
      dateTime = new DateTime();
      return false;
    }
  }

  /// <summary>Tries to parse an <c>xsi:double</c> value.</summary>
  public static bool TryParse(string floatStr, out double value)
  {
    if(!string.IsNullOrEmpty(floatStr))
    {
      if(InvariantCultureUtility.TryParse(floatStr, out value)) return true;

      int start, length;
      floatStr.Trim(out start, out length);
      if(length == 3)
      {
        if(string.Compare(floatStr, start, "NaN", 0, 3, StringComparison.Ordinal) == 0)
        {
          value = double.NaN;
          return true;
        }
        else if(string.Compare(floatStr, start, "INF", 0, 3, StringComparison.Ordinal) == 0)
        {
          value = double.PositiveInfinity;
          return true;
        }
      }
      else if(length == 4 && string.Compare(floatStr, start, "-INF", 0, 4, StringComparison.Ordinal) == 0)
      {
        value = double.NegativeInfinity;
        return true;
      }
    }

    value = 0;
    return false;
  }

  /// <summary>Tries to parse an <c>xsi:float</c> value.</summary>
  public static bool TryParse(string floatStr, out float value)
  {
    if(!string.IsNullOrEmpty(floatStr))
    {
      if(InvariantCultureUtility.TryParse(floatStr, out value)) return true;

      int start, length;
      floatStr.Trim(out start, out length);
      if(length == 3)
      {
        if(string.Compare(floatStr, start, "NaN", 0, 3, StringComparison.Ordinal) == 0)
        {
          value = float.NaN;
          return true;
        }
        else if(string.Compare(floatStr, start, "INF", 0, 3, StringComparison.Ordinal) == 0)
        {
          value = float.PositiveInfinity;
          return true;
        }
      }
      else if(length == 4 && string.Compare(floatStr, start, "-INF", 0, 4, StringComparison.Ordinal) == 0)
      {
        value = float.NegativeInfinity;
        return true;
      }
    }

    value = 0;
    return false;
  }

  /// <summary>Tries to parse an <c>xsi:byte</c> value.</summary>
  [CLSCompliant(false)]
  public static bool TryParse(string str, out sbyte value)
  {
    return sbyte.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out value);
  }

  /// <summary>Tries to parse an <c>xsi:decimal</c> value.</summary>
  public static bool TryParse(string str, out decimal value)
  {
    const NumberStyles style = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite |
                               NumberStyles.AllowTrailingWhite;
    return decimal.TryParse(str, style, NumberFormatInfo.InvariantInfo, out value);
  }

  /// <summary>Tries to parse an <c>xsi:int</c> value.</summary>
  public static bool TryParse(string str, out int value)
  {
    return int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out value);
  }

  /// <summary>Tries to parse an <c>xsi:long</c> value.</summary>
  public static bool TryParse(string str, out long value)
  {
    return long.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out value);
  }

  /// <summary>Tries to parse an <c>xsi:short</c> value.</summary>
  public static bool TryParse(string str, out short value)
  {
    return short.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out value);
  }

  /// <summary>Tries to parse an <c>xsi:unsignedByte</c> value.</summary>
  public static bool TryParse(string str, out byte value)
  {
    return byte.TryParse(str, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo, out value);
  }

  /// <summary>Tries to parse an <c>xsi:unsignedInt</c> value.</summary>
  [CLSCompliant(false)]
  public static bool TryParse(string str, out uint value)
  {
    return uint.TryParse(str, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo, out value);
  }

  /// <summary>Tries to parse an <c>xsi:unsignedLong</c> value.</summary>
  [CLSCompliant(false)]
  public static bool TryParse(string str, out ulong value)
  {
    return ulong.TryParse(str, NumberStyles.AllowLeadingWhite|NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo, out value);
  }

  /// <summary>Tries to parse an <c>xsi:unsignedShort</c> value.</summary>
  [CLSCompliant(false)]
  public static bool TryParse(string str, out ushort value)
  {
    return ushort.TryParse(str, NumberStyles.AllowLeadingWhite|NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo, out value);
  }

  /// <summary>Tries to parse an <c>xs:date</c>, <c>xs:dateTime</c>, or <c>xs:datetimeoffset</c> value, preserving the time zone
  /// information it contains.
  /// </summary>
  /// <param name="dateStr">The value, in <c>xs:date</c> or <c>xs:dateTime</c> format.</param>
  /// <param name="value">A variable that receives either a <see cref="DateTime"/> or <see cref="DateTimeOffset"/> value. If no time zone
  /// information is given, the value will be a <see cref="DateTime"/> with a <see cref="DateTime.Kind"/> of
  /// <see cref="DateTimeKind.Unspecified"/>. If the UTC time zone (<c>Z</c>) is given, the value will be a <see cref="DateTime"/> with a
  /// <see cref="DateTime.Kind"/> of <see cref="DateTimeKind.Utc"/>. If any other time zone is given (including if the time zone is
  /// <c>+00:00</c> or <c>-00:00</c>), the value will be a <see cref="DateTimeOffset"/> representing a local time in the given time zone.
  /// </param>
  /// <returns>Returns true if the value was successfully parsed and false if not.</returns>
  public static bool TryParseDateTime(string dateStr, out object value)
  {
    if(!string.IsNullOrEmpty(dateStr))
    {
      Match m = reDateTime.Match(dateStr);
      if(m.Success)
      {
        int year, month, day, hour, minute;
        double secs;
        month = int.Parse(m.Groups["mo"].Value, CultureInfo.InvariantCulture);
        day = int.Parse(m.Groups["d"].Value, CultureInfo.InvariantCulture);
        if(m.Groups["h"].Success)
        {
          hour = int.Parse(m.Groups["h"].Value, CultureInfo.InvariantCulture);
          minute = int.Parse(m.Groups["min"].Value, CultureInfo.InvariantCulture);
          secs = double.Parse(m.Groups["s"].Value, CultureInfo.InvariantCulture);
        }
        else
        {
          hour = minute = 0;
          secs = 0;
        }
        if(InvariantCultureUtility.TryParseExact(m.Groups["y"].Value, out year) && year > 0 && year <= 9999 &&
           month > 0 && month <= 12 && day > 0 && day <= DateUtility.GetDaysInMonth(month, year) &&
           (hour < 24 && minute < 60 && secs < 60 || hour == 24 && minute == 0 && secs == 0))
        {
          string tz = m.Groups["tz"].Value;
          DateTimeKind kind = string.IsNullOrEmpty(tz) ? DateTimeKind.Unspecified :
                              tz.OrdinalEquals("Z")    ? DateTimeKind.Utc : DateTimeKind.Local;
          DateTime dateTime = new DateTime(year, month, day, hour == 24 ? 0 : hour, minute, 0, kind);
          if(hour == 24) dateTime = dateTime.AddDays(1);
          if(secs != 0) dateTime = dateTime.AddTicks((long)Math.Round(secs * TimeSpan.TicksPerSecond)); // .AddSeconds() has low precision

          if(kind != DateTimeKind.Local)
          {
            value = dateTime;
          }
          else
          {
            TimeSpan offset = new TimeSpan(int.Parse(tz.Substring(1, 2), CultureInfo.InvariantCulture),
                                           int.Parse(tz.Substring(4, 2), CultureInfo.InvariantCulture), 0);
            value = new DateTimeOffset(dateTime.Ticks, tz[0] == '-' ? offset.Negate() : offset);
          }
          return true;
        }
      }
    }

    value = null;
    return false;
  }

  /// <summary>Encodes the given text for safe insertion into XML elements and attributes. This
  /// method is not suitable for encoding XML element and attribute names. (To encode names,
  /// you should use <see cref="XmlConvert.EncodeName"/> or <see cref="XmlConvert.EncodeLocalName"/>.)
  /// </summary>
  public static string XmlEncode(string text)
  {
    return XmlEncode(text, true);
  }

  /// <summary>Encodes the given text for safe insertion into XML elements and, if <paramref name="isAttributeText"/> is true,
  /// attributes. This method is not suitable for encoding XML element and attribute names, but only content. (To encode names,
  /// you should use <see cref="XmlConvert.EncodeName"/> or <see cref="XmlConvert.EncodeLocalName"/>.)
  /// </summary>
  /// <param name="text">The text to encode. If null, null will be returned.</param>
  /// <param name="isAttributeText">If true, additional characters (such as quotation marks, apostrophes, tabs, and newlines)
  /// are encoded as well, allowing safe insertion into XML attributes. If false, the returned text may only be suitable for
  /// insertion into elements.
  /// </param>
  public static string XmlEncode(string text, bool isAttributeText)
  {
    // if no characters need encoding, we'll just return the original string, so 'sb' will remain
    // null until the character needs encoding.
    StringBuilder sb = null;

    if(text != null) // a null input string will be returned as null
    {
      for(int i=0; i<text.Length; i++)
      {
        string entity = null;
        char c = text[i];
        switch(c)
        {
          case '\t': case '\n': case '\r':
            if(isAttributeText) entity = MakeHexEntity(c);
            break;

          case '"':
            if(isAttributeText) entity = "&quot;";
            break;

          case '\'':
            if(isAttributeText) entity = "&apos;";
            break;

          case '&':
            entity = "&amp;";
            break;

          case '<':
            entity = "&lt;";
            break;

          case '>':
            entity = "&gt;";
            break;

          default:
            // all non-printable or non-ASCII characters will be encoded, except for those above
            if(c < 32 || c > 126) entity = MakeHexEntity(c);
            break;
        }

        if(entity != null) // if the character needs to be encoded...
        {
          // initialize the string builder if we haven't already, with enough room for the text, plus some entities
          if(sb == null) sb = new StringBuilder(text, 0, i, text.Length + 100);
          sb.Append(entity); // then add the entity for the current character
        }
        else if(sb != null) // the character doesn't need encoding. only add it if a previous character has needed encoding...
        {
          sb.Append(c);
        }

        // TODO: we should perhaps try to deal with unicode surrogates, but i think we can ignore them for now
      }
    }

    return sb != null ? sb.ToString() : text;
  }

  static bool IsWhitespace(char c)
  {
    return c == ' ' || c == '\t' || c == '\n' || c == '\r';
  }

  /// <summary>Creates and returns an XML entity containing the character's hex code.</summary>
  static string MakeHexEntity(char c)
  {
    return "&#x" + ((int)c).ToString("X", CultureInfo.InvariantCulture) + ";";
  }

  /// <summary>Converts a string to a <c>xs:normalizedString</c> or <c>xs:TOKEN</c> value.</summary>
  static string NormalizeString(string value, bool trim)
  {
    if(value != null)
    {
      bool previouslySpace = false;
      int start = 0, end = value.Length;
      if(trim && Trim(value, out start, out end)) end += start;

      for(int i=start; i<end; i++)
      {
        char c = value[i];
        bool encode = false;
        if(c == ' ')
        {
          if(previouslySpace) encode = true;
          else previouslySpace = true;
        }
        else if(c == '\t' || c == '\n' || c == '\r')
        {
          encode = true;
        }
        else
        {
          previouslySpace = false;
        }

        if(encode)
        {
          StringBuilder sb = new StringBuilder(value.Length);
          sb.Append(value, start, i-start);
          while(true)
          {
            if(c == '\t' || c == '\n' || c == '\r') c = ' ';
            if(c == ' ')
            {
              if(!previouslySpace)
              {
                sb.Append(' ');
                previouslySpace = true;
              }
            }
            else
            {
              sb.Append(c);
              previouslySpace = false;
            }

            if(++i == end) break;
            c = value[i];
          }
          return sb.ToString();
        }
      }

      if(start != 0 || end != value.Length) value = value.Substring(start, end-start);
    }

    return value;
  }

  static bool Trim(string str, out int start, out int length)
  {
    int i = 0, j = str.Length - 1;
    while(i < str.Length && IsWhitespace(str[i])) i++;
    while(j > i && IsWhitespace(str[j])) j--;
    if(j < i)
    {
      start  = 0;
      length = 0;
      return str.Length != 0;
    }
    else
    {
      start  = i;
      length = j - i + 1;
      return i != 0 || j != str.Length - 1;
    }
  }

  static readonly Regex reDateTime =
    new Regex(@"^\s*(?<y>-?[0-9]{4,})-(?<mo>[0-9]{2})-(?<d>[0-9]{2})(?:T(?<h>[0-9]{2}):(?<min>[0-9]{2}):(?<s>[0-9]{2}(?:\.[0-9]+)?)(?<tz>Z|[+\-][0-9]{2}:[0-9]{2})?)?\s*$",
              RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
  static readonly Regex reListSplit = new Regex(@"\s+", RegexOptions.Singleline);
}
#endregion

} // namespace AdamMil.Utilities
