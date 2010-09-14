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
using System.Collections.Generic;
using System.Text;

namespace AdamMil.Utilities
{

public static class StringUtility
{
	/// <summary>Determines whether the string contains another given string.</summary>
	public static bool Contains(this string strToSearch, string strToSearchFor, StringComparison comparisonType)
	{
		return strToSearch.IndexOf(strToSearchFor, comparisonType) != -1;
	}

	/// <summary>Converts the given string, which is assumed to contain only hex digits, into the corresponding byte array.</summary>
	public static byte[] FromHex(string hex)
	{
		if(hex == null) throw new ArgumentNullException();
		hex = hex.Trim();
		if((hex.Length & 1) != 0) throw new ArgumentException("The length of the hex data must be a multiple of two.");

		byte[] data = new byte[hex.Length / 2];
		for(int i=0,o=0; i<hex.Length; i += 2)
		{
			data[o++] = (byte)((HexValue(hex[i]) << 4) | HexValue(hex[i+1]));
		}
		return data;
	}

	/// <summary>Converts the given byte value into a corresponding two-digit hex string.</summary>
	public static string ToHex(byte value)
	{
		return new string(new char[2] { HexChars[value >> 4], HexChars[value & 0xF] });
	}

	/// <summary>Converts the given binary data into a hex string.</summary>
	public static string ToHex(byte[] data)
	{
		if(data == null) throw new ArgumentNullException();
		char[] chars = new char[data.Length * 2];
		for(int i=0,o=0; i<data.Length; i++)
		{
			byte value = data[i];
			chars[o++] = HexChars[value >> 4];
			chars[o++] = HexChars[value & 0xF];
		}
		return new string(chars);
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

	/// <summary>Splits the given string, passing each item through the conversion function before returning it.</summary>
	public static string[] Split(this string str, char separator, Converter<string, string> converter)
	{
		return str.Split(separator, converter, StringSplitOptions.None);
	}

	/// <summary>Splits the given string, passing each item through the conversion function before returning it.</summary>
	public static string[] Split(this string str, char separator, Converter<string,string> converter, StringSplitOptions options)
	{
		if(converter == null) throw new ArgumentNullException();
		if(string.IsNullOrEmpty(str))
		{
			return options == StringSplitOptions.RemoveEmptyEntries ? new string[0] : new string[] { converter(str) };
		}

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
		if(converter == null) throw new ArgumentNullException();
		if(string.IsNullOrEmpty(str)) return options == StringSplitOptions.RemoveEmptyEntries ? new T[0] : new T[] { converter(str) };

		string[] bits = str.Split(new char[] { separator }, options);
		T[] items = new T[bits.Length];
		for(int i=0; i<items.Length; i++) items[i] = converter(bits[i]);
		return items;
	}

	/// <summary>Converts the given hex digit into its numeric value.</summary>
	static int HexValue(char c)
	{
		if(c >= '0' && c <= '9') return c - '0';
		c = char.ToUpperInvariant(c);
		if(c >= 'A' && c <= 'F') return c - ('A' - 10);
		throw new ArgumentException("'" + c.ToString() + "' is not a valid hex digit.");
	}

	const string HexChars = "0123456789ABCDEF";
}

} // namespace AdamMil.Utilities
