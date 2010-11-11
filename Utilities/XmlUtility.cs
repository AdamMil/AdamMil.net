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
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace AdamMil.Utilities
{

#region XmlNodeExtensions
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

  /// <summary>Returns the value of the named attribute as an 8-bit signed integer, or 0 if the attribute was unspecified or empty.</summary>
  public static sbyte GetSByteAttribute(this XmlNode node, string attrName)
  {
    return GetSByteAttribute(node, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as an 8-bit signed integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
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
  public static ushort GetUInt16Attribute(this XmlNode node, string attrName)
  {
    return GetUInt16Attribute(node, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 16-bit unsigned integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static ushort GetUInt16Attribute(this XmlNode node, string attrName, ushort defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToUInt16(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 32-bit unsigned integer, or 0 if the attribute was unspecified or empty.</summary>
  public static uint GetUInt32Attribute(this XmlNode node, string attrName)
  {
    return GetUInt32Attribute(node, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 32-bit unsigned integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static uint GetUInt32Attribute(this XmlNode node, string attrName, uint defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToUInt32(attrValue);
  }

  /// <summary>Returns the value of the named attribute as a 64-bit unsigned integer, or 0 if the attribute was unspecified or empty.</summary>
  public static ulong GetUInt64Attribute(this XmlNode node, string attrName)
  {
    return GetUInt64Attribute(node, attrName, 0);
  }

  /// <summary>Returns the value of the named attribute as a 64-bit unsigned integer, or the given default
  /// value if the attribute was unspecified or empty.
  /// </summary>
  public static ulong GetUInt64Attribute(this XmlNode node, string attrName, ulong defaultValue)
  {
    string attrValue = GetAttributeValue(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToUInt64(attrValue);
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

  /// <summary>Removes all the child nodes of the given node.</summary>
  public static void RemoveChildren(this XmlNode node)
  {
    if(node == null) throw new ArgumentNullException();
    while(node.FirstChild != null) node.RemoveChild(node.FirstChild);
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
  public static sbyte SelectSByte(this XmlNode node, string xpath)
  {
    return SelectSByte(node, xpath, 0);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as an 8-bit signed integer,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
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
  public static ushort SelectUInt16(this XmlNode node, string xpath)
  {
    return SelectUInt16(node, xpath, 0);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 16-bit unsigned integer,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  public static ushort SelectUInt16(this XmlNode node, string xpath, ushort defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToUInt16(stringValue);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 32-bit unsigned integer,
  /// or 0 if the node could not be found or was empty.
  /// </summary>
  public static uint SelectUInt32(this XmlNode node, string xpath)
  {
    return SelectUInt32(node, xpath, 0);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 32-bit unsigned integer,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
  public static uint SelectUInt32(this XmlNode node, string xpath, uint defaultValue)
  {
    string stringValue = SelectValue(node, xpath);
    return string.IsNullOrEmpty(stringValue) ? defaultValue : XmlConvert.ToUInt32(stringValue);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 64-bit unsigned integer,
  /// or 0 if the node could not be found or was empty.
  /// </summary>
  public static ulong SelectUInt64(this XmlNode node, string xpath)
  {
    return SelectUInt64(node, xpath, 0);
  }

  /// <summary>Returns the inner text of the node selected by the given XPath query as a 64-bit unsigned integer,
  /// or the given default value if the node could not be found or was empty.
  /// </summary>
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
}
#endregion

#region XmlUtility
public static class XmlUtility
{
  /// <summary>Parses a string containing a whitespace-separated list of items into an array of strings containing the substrings
  /// corresponding to the individual items.
  /// </summary>
  public static string[] ParseList(string listValue)
  {
    if(listValue != null) listValue = listValue.Trim();
    return string.IsNullOrEmpty(listValue) ? new string[0] : split.Split(listValue);
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
      string[] bits = split.Split(listValue);
      T[] values = new T[bits.Length];
      for(int i=0; i<values.Length; i++) values[i] = converter(bits[i]);
      return values;
    }
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
          if(sb == null) // then initialize the string builder if we haven't already
          {
            sb = new StringBuilder(text.Length + 100); // allocate enough room for the text, plus some entities
            sb.Append(text, 0, i); // add the text so far (which didn't need encoding)
          }
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

  /// <summary>Creates and returns an XML entity containing the character's hex code.</summary>
  static string MakeHexEntity(char c)
  {
    return "&#x" + ((int)c).ToString("X", CultureInfo.InvariantCulture) + ";";
  }

  static readonly Regex split = new Regex(@"\s+", RegexOptions.Singleline);
}
#endregion

} // namespace AdamMil.Utilities
