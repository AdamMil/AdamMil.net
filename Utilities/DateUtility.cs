using System;

namespace AdamMil.Utilities
{

/// <summary>Provides extensions to the <see cref="DateTime"/> structure.</summary>
public static class DateUtility
{
  /// <summary>
  /// Returns a <see cref="DateTime"/> representing the latest representable moment in the day -- one tick before the next day.
  /// </summary>
  public static DateTime GetEndOfDay(this DateTime dateTime)
  {
    return dateTime.Date.AddDays(1).AddTicks(-1);
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
    return dateTime.ToString(timeOfDay.Ticks == 0 ? "d" : timeOfDay.Seconds == 0 ? "g" : "G", provider);
  }
}

} // namespace AdamMil.Utilities
