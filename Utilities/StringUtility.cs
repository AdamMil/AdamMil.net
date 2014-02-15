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
using System.Collections.Generic;
using System.Text;

namespace AdamMil.Utilities
{

/// <summary>Provides useful string utilities and extensions.</summary>
public static class StringUtility
{
  /// <summary>Returns the first string that is not null or empty, or null if all strings are null or empty.</summary>
  public static string Coalesce(string str1, string str2)
  {
    return !string.IsNullOrEmpty(str1) ? str1 : !string.IsNullOrEmpty(str2) ? str2 : null;
  }

  /// <summary>Returns the first string that is not empty, or null if all strings are null or empty.</summary>
  public static string Coalesce(string str1, string str2, string str3)
  {
    return Coalesce(Coalesce(str1, str2), str3);
  }

  /// <summary>Returns the first string that is not empty, or null if all strings are null or empty.</summary>
  public static string Coalesce(params string[] strings)
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

  /// <summary>Returns the concatenation of strings <paramref name="a"/> and <paramref name="b"/>, separated by
  /// <paramref name="separator"/>, if both strings are not null or empty. Otherwise, returns the first string that is not null or empty.
  /// </summary>
  public static string Combine(string separator, string a, string b)
  {
    return string.IsNullOrEmpty(a) ? b : string.IsNullOrEmpty(b) ? a : a + separator + b;
  }

  /// <summary>Returns the concatenation of strings <paramref name="a"/>, <paramref name="b"/>, and <paramref name="c"/>, separated by
  /// <paramref name="separator"/>, and ignoring strings that are null or empty.
  /// </summary>
  public static string Combine(string separator, string a, string b, string c)
  {
    return Combine(separator, Combine(separator, a, b), c);
  }

  /// <summary>Returns the concatenation of the given strings, separated by <paramref name="separator"/>, and ignoring strings that are
  /// null or empty.
  /// </summary>
  public static string Combine(string separator, params string[] strings)
  {
    return Combine(separator, (IEnumerable<string>)strings);
  }

  /// <summary>Returns the concatenation of the given strings, separated by <paramref name="separator"/>, and ignoring strings that are
  /// null or empty.
  /// </summary>
  public static string Combine(string separator, IEnumerable<string> strings)
  {
    if(strings == null) throw new ArgumentNullException();
    StringBuilder sb = new StringBuilder();
    foreach(string str in strings)
    {
      if(!string.IsNullOrEmpty(str))
      {
        if(sb.Length != 0) sb.Append(separator);
        sb.Append(str);
      }
    }
    return sb.ToString();
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

  /// <summary>Determines whether a substring ends at the given index within a string.</summary>
  public static bool EndsAt(this string str, int index, string substring)
  {
    return EndsAt(str, index, substring, StringComparison.Ordinal);
  }

  /// <summary>Determines whether a substring ends at the given index within a string.</summary>
  public static bool EndsAt(this string str, int index, string substring, StringComparison comparisonType)
  {
    if(str == null || substring == null) throw new ArgumentNullException();
    if((uint)index >= (uint)str.Length) throw new ArgumentOutOfRangeException();
    index++;
    return index >= substring.Length && string.Compare(str, index-substring.Length, substring, 0, substring.Length, comparisonType) == 0;
  }

  /// <summary>Returns true if the given string is not null but has a length of zero.</summary>
  public static bool IsEmpty(this string str)
  {
    return str != null && str.Length == 0;
  }

  /// <summary>Returns true if the given string is null, empty, or contains only whitespace characters.</summary>
  public static bool IsNullOrSpace(string str)
  {
    if(str != null)
    {
      for(int i=0; i<str.Length; i++)
      {
        if(!char.IsWhiteSpace(str[i])) return false;
      }
    }
    return true;
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

  /// <summary>Removes all instances of the given character from the string.</summary>
  public static string Remove(string str, char charToRemove) // it would be nice if this was an extension, but it conflicts with the
  {                                                          // string.Remove(int) method, annoyingly enough
    if(str == null) throw new ArgumentNullException();
    return str.Replace(new string(charToRemove, 1), null); // TODO: this could be optimized
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

  /// <summary>Reverses a string.</summary>
  public static string Reverse(this string str)
  {
    if(str == null) throw new ArgumentNullException();
    char[] chars = str.ToCharArray();
    for(int i=0, len=chars.Length/2; i<len; i++) Utility.Swap(ref chars[i], ref chars[chars.Length-i-1]);
    return new string(chars);
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

  /// <summary>Determines whether a substring starts at the given index within a string.</summary>
  public static bool StartsAt(this string str, int index, string substring)
  {
    return StartsAt(str, index, substring, StringComparison.Ordinal);
  }

  /// <summary>Determines whether a substring starts at the given index within a string.</summary>
  public static bool StartsAt(this string str, int index, string substring, StringComparison comparisonType)
  {
    if(str == null || substring == null) throw new ArgumentNullException();
    if((uint)index > (uint)str.Length) throw new ArgumentOutOfRangeException();
    return str.Length - index >= substring.Length && string.Compare(str, index, substring, 0, substring.Length, comparisonType) == 0;
  }

  /// <summary>Finds the region of the string within leading and trailing whitespace.</summary>
  /// <param name="str">The string to trim.</param>
  /// <param name="start">A variable that receives the start of the trimmed region.</param>
  /// <param name="length">A variable that receives the length of the trimmed region.</param>
  /// <returns>True if any whitespace was skipped, and false if <paramref name="start"/> equals 0 and <paramref name="length"/> equals
  /// the length of the input.
  /// </returns>
  public static bool Trim(this string str, out int start, out int length)
  {
    if(str == null) throw new ArgumentNullException();
    int i = 0, j = str.Length - 1;
    while(i < str.Length && char.IsWhiteSpace(str[i])) i++;
    while(j > i && char.IsWhiteSpace(str[j])) j--;
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
}

} // namespace AdamMil.Utilities
