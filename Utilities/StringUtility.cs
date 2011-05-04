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
using System.Collections.Generic;
using System.Text;

namespace AdamMil.Utilities
{

/// <summary>Provides useful string utilities and extensions.</summary>
public static class StringUtility
{
  /// <summary>Returns the first string that is not empty, or null if all strings are null or empty.</summary>
  public static string Coallesce(string str1, string str2)
  {
    return !string.IsNullOrEmpty(str1) ? str1 : !string.IsNullOrEmpty(str2) ? str2 : null;
  }

  /// <summary>Returns the first string that is not empty, or null if all strings are null or empty.</summary>
  public static string Coallesce(string str1, string str2, string str3)
  {
    return Coallesce(Coallesce(str1, str2), str3);
  }

  /// <summary>Returns the first string that is not empty, or null if all strings are null or empty.</summary>
  public static string Coallesce(params string[] strings)
  {
    if(strings != null)
    {
      foreach(string str in strings)
      {
        if(!string.IsNullOrEmpty(str)) return str;
      }
    }
    return null;
  }

  /// <summary>Determines whether a string contains a given character.</summary>
  public static bool Contains(this string strToSearch, char charToSearchFor)
  {
    if(strToSearch == null) throw new ArgumentNullException();
    return strToSearch.IndexOf(charToSearchFor) != -1;
  }

  /// <summary>Determines whether a string contains a given substring.</summary>
  public static bool Contains(this string strToSearch, string strToSearchFor, StringComparison comparisonType)
  {
    if(strToSearch == null) throw new ArgumentNullException();
    return strToSearch.IndexOf(strToSearchFor, comparisonType) != -1;
  }

  /// <summary>Determines whether two strings are identical, using a case-sensitive comparison with the current culture.</summary>
  public static bool CulturallyEquals(this string string1, string string2)
  {
    return string.Equals(string1, string2, StringComparison.CurrentCulture);
  }

  /// <summary>Determines whether two strings are identical, using a comparison with the current culture.</summary>
  public static bool CulturallyEquals(this string string1, string string2, bool ignoreCase)
  {
    return string.Equals(string1, string2,
                         ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
  }

  /// <summary>Returns null if the given string is null or empty. Otherwise, returns the given string.</summary>
  public static string MakeNullIfEmpty(string str)
  {
    return string.IsNullOrEmpty(str) ? null : str;
  }

  /// <summary>Determines whether two strings are identical, using a case-sensitive ordinal comparison.</summary>
  public static bool OrdinalEquals(this string string1, string string2)
  {
    return string.Equals(string1, string2, StringComparison.Ordinal);
  }

  /// <summary>Determines whether two strings are identical, using an ordinal comparison.</summary>
  public static bool OrdinalEquals(this string string1, string string2, bool ignoreCase)
  {
    return string.Equals(string1, string2, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
  }

  /// <summary>Creates a string from the given sequence by concatenating each element.</summary>
  public static string Join(IEnumerable<string> source)
  {
    return Join(null, source);
  }

  /// <summary>Creates a string from the given sequence by concatenating each element, separated with the given separator.</summary>
  public static string Join(string separator, IEnumerable<string> source)
  {
    return Join(separator, source, str => str);
  }

  /// <summary>Creates a string from the given sequence by concatenating the result
  /// of the specified conversion function for each element.
  /// </summary>
  public static string Join<T>(IEnumerable<T> source, Converter<T,string> converter)
  {
    return Join(null, source, converter);
  }

  /// <summary>Creates a string from the given sequence by concatenating the result of the
  /// specified conversion function for each element, separated with the given separator.
  /// </summary>
  public static string Join<T>(string separator, IEnumerable<T> source, Converter<T,string> converter)
  {
    if(source == null || converter == null) throw new ArgumentNullException();

    StringBuilder sb = new StringBuilder();
    bool needsSeparator = false;
    foreach(T item in source)
    {
      if(needsSeparator) sb.Append(separator);
      else needsSeparator = true;
      sb.Append(converter(item));
    }
    return sb.ToString();
  }

  /// <summary>Removes all instances of the given characters from the string.</summary>
  public static string Remove(this string str, params char[] charsToRemove)
  {
    return str.Replace(charsToRemove, null);
  }

  /// <summary>Replaces each instance of the given characters with the given replacement string.</summary>
  public static string Replace(this string str, char[] charsToReplace, string replacement)
  {
    if(str != null && charsToReplace != null && charsToReplace.Length != 0)
    {
      if(charsToReplace.Length == 1)
      {
        str = str.Replace(new string(charsToReplace[0], 1), replacement);
      }
      else
      {
        int index = str.IndexOfAny(charsToReplace);
        if(index != -1)
        {
          StringBuilder sb = new StringBuilder(str.Length + (replacement == null || replacement.Length < 2 ? 0 : 100));
          int position = 0;
          do
          {
            sb.Append(str, position, index-position).Append(replacement);
            position = index + 1;
            index = str.IndexOfAny(charsToReplace, position);
          } while(index != -1);
          sb.Append(str, position, str.Length-position);
          str = sb.ToString();
        }
      }
    }

    return str;
  }

  /// <summary>Splits a string by the given substring.</summary>
  public static string[] Split(this string str, string separator)
  {
    if(str == null) throw new ArgumentNullException();
    return str.Split(new string[] { separator }, StringSplitOptions.None);
  }

  /// <summary>Splits a string by the given substring.</summary>
  public static string[] Split(this string str, string separator, StringSplitOptions options)
  {
    if(str == null) throw new ArgumentNullException();
    return str.Split(new string[] { separator }, options);
  }

  /// <summary>Splits a string by the given character.</summary>
  public static string[] Split(this string str, char separator, int maxItems)
  {
    if(str == null) throw new ArgumentNullException();
    return str.Split(new char[] { separator }, maxItems);
  }

  /// <summary>Splits a string by the given character.</summary>
  public static string[] Split(this string str, char separator, StringSplitOptions options)
  {
    return str.Split(new char[] { separator }, options);
  }

  /// <summary>Splits the given string, passing each item through the conversion function before returning it.</summary>
  public static string[] Split(this string str, char separator, Converter<string, string> converter)
  {
    return str.Split(separator, converter, StringSplitOptions.None);
  }

  /// <summary>Splits the given string, passing each item through the conversion function before returning it.</summary>
  public static string[] Split(this string str, char separator, Converter<string,string> converter, StringSplitOptions options)
  {
    if(str == null || converter == null) throw new ArgumentNullException();
    if(str.Length == 0) return options == StringSplitOptions.RemoveEmptyEntries ? new string[0] : new string[] { converter(str) };

    string[] items = str.Split(new char[] { separator }, options);
    for(int i=0; i<items.Length; i++) items[i] = converter(items[i]);
    return items;
  }

  /// <summary>Splits the given string, passing each item through the conversion function before returning it.</summary>
  public static T[] Split<T>(this string str, char separator, Converter<string,T> converter)
  {
    return str.Split(separator, converter, StringSplitOptions.None);
  }

  /// <summary>Splits the given string, passing each item through the conversion function before returning it.</summary>
  public static T[] Split<T>(this string str, char separator, Converter<string,T> converter, StringSplitOptions options)
  {
    if(str == null || converter == null) throw new ArgumentNullException();
    if(str.Length == 0) return options == StringSplitOptions.RemoveEmptyEntries ? new T[0] : new T[] { converter(str) };

    string[] bits = str.Split(new char[] { separator }, options);
    T[] items = new T[bits.Length];
    for(int i=0; i<items.Length; i++) items[i] = converter(bits[i]);
    return items;
  }
}

} // namespace AdamMil.Utilities
