/*
AdamMil.IO is a library that provides high performance and high level IO
tools for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2009 Adam Milazzo

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
using System.Text.RegularExpressions;
using System.Xml;

namespace AdamMil.IO
{

public static class Xml
{
  public static string Attr(XmlNode node, string attrName) 
  {
    return Attr(node, attrName, null); 
  }

  public static string Attr(XmlNode node, string attrName, string defaultValue)
  {
    XmlAttribute an = AttrNode(node, attrName);
    return an == null ? defaultValue : an.Value;
  }

  public static XmlAttribute AttrNode(XmlNode node, string attrName)
  { 
    return node == null ? null : node.Attributes[attrName]; 
  }

  public static bool Bool(XmlNode node, string attrName)
  {
    return Bool(node, attrName, false);
  }

  public static bool Bool(XmlNode node, string attrName, bool defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToBoolean(attrValue);
  }

  public static byte Byte(XmlNode node, string attrName)
  {
    return Byte(node, attrName, 0);
  }

  public static byte Byte(XmlNode node, string attrName, byte defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToByte(attrValue);
  }

  public static char Char(XmlNode node, string attrName)
  {
    return Char(node, attrName, '\0');
  }

  public static char Char(XmlNode node, string attrName, char defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToChar(attrValue);
  }

  public static string Child(XmlNode node, string xpath)
  {
    return Child(node, xpath, null);
  }

  public static string Child(XmlNode node, string xpath, string defaultValue)
  {
    if(node == null) return defaultValue;
    XmlNode child = node.SelectSingleNode(xpath);
    return child == null ? defaultValue : child.InnerText;
  }

  public static bool ChildBool(XmlNode node, string xpath)
  {
    return ChildBool(node, xpath, false);
  }

  public static bool ChildBool(XmlNode node, string xpath, bool defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ? defaultValue : XmlConvert.ToBoolean(childValue);
  }

  public static byte ChildByte(XmlNode node, string xpath)
  {
    return ChildByte(node, xpath, 0);
  }

  public static byte ChildByte(XmlNode node, string xpath, byte defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ? defaultValue : XmlConvert.ToByte(childValue);
  }

  public static char ChildChar(XmlNode node, string xpath)
  {
    return ChildChar(node, xpath, '\0');
  }

  public static char ChildChar(XmlNode node, string xpath, char defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ? defaultValue : XmlConvert.ToChar(childValue);
  }

  public static DateTime ChildDateTime(XmlNode node, string xpath)
  {
    return ChildDateTime(node, xpath, new DateTime());
  }

  public static DateTime ChildDateTime(XmlNode node, string xpath, DateTime defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ?
      defaultValue : XmlConvert.ToDateTime(childValue, XmlDateTimeSerializationMode.Unspecified);
  }

  public static decimal ChildDecimal(XmlNode node, string xpath)
  {
    return ChildDecimal(node, xpath, 0);
  }

  public static decimal ChildDecimal(XmlNode node, string xpath, decimal defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ? defaultValue : XmlConvert.ToDecimal(childValue);
  }

  public static double ChildDouble(XmlNode node, string xpath)
  {
    return ChildDouble(node, xpath, 0);
  }

  public static double ChildDouble(XmlNode node, string xpath, double defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ? defaultValue : XmlConvert.ToDouble(childValue);
  }

  public static Guid ChildGuid(XmlNode node, string xpath)
  {
    return ChildGuid(node, xpath, System.Guid.Empty);
  }

  public static Guid ChildGuid(XmlNode node, string xpath, Guid defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ? defaultValue : XmlConvert.ToGuid(childValue);
  }

  public static short ChildInt16(XmlNode node, string xpath)
  {
    return ChildInt16(node, xpath, 0);
  }

  public static short ChildInt16(XmlNode node, string xpath, short defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ? defaultValue : XmlConvert.ToInt16(childValue);
  }

  public static int ChildInt32(XmlNode node, string xpath)
  {
    return ChildInt32(node, xpath, 0);
  }

  public static int ChildInt32(XmlNode node, string xpath, int defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ? defaultValue : XmlConvert.ToInt32(childValue);
  }

  public static long ChildInt64(XmlNode node, string xpath)
  {
    return ChildInt64(node, xpath, 0);
  }

  public static long ChildInt64(XmlNode node, string xpath, long defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ? defaultValue : XmlConvert.ToInt64(childValue);
  }

  public static sbyte ChildSByte(XmlNode node, string xpath)
  {
    return ChildSByte(node, xpath, 0);
  }

  public static sbyte ChildSByte(XmlNode node, string xpath, sbyte defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ? defaultValue : XmlConvert.ToSByte(childValue);
  }

  public static float ChildSingle(XmlNode node, string xpath)
  {
    return ChildSingle(node, xpath, 0);
  }

  public static float ChildSingle(XmlNode node, string xpath, float defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ? defaultValue : XmlConvert.ToSingle(childValue);
  }

  public static string ChildString(XmlNode node, string xpath)
  {
    return ChildString(node, xpath, string.Empty);
  }

  public static string ChildString(XmlNode node, string xpath, string defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ? defaultValue : childValue;
  }

  public static TimeSpan ChildTimeSpan(XmlNode node, string xpath)
  {
    return ChildTimeSpan(node, xpath, new TimeSpan());
  }

  public static TimeSpan ChildTimeSpan(XmlNode node, string xpath, TimeSpan defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ? defaultValue : XmlConvert.ToTimeSpan(childValue);
  }

  public static ushort ChildUInt16(XmlNode node, string xpath)
  {
    return ChildUInt16(node, xpath, 0);
  }

  public static ushort ChildUInt16(XmlNode node, string xpath, ushort defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ? defaultValue : XmlConvert.ToUInt16(childValue);
  }

  public static uint ChildUInt32(XmlNode node, string xpath)
  {
    return ChildUInt32(node, xpath, 0);
  }

  public static uint ChildUInt32(XmlNode node, string xpath, uint defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ? defaultValue : XmlConvert.ToUInt32(childValue);
  }

  public static ulong ChildUInt64(XmlNode node, string xpath)
  {
    return ChildUInt64(node, xpath, 0);
  }

  public static ulong ChildUInt64(XmlNode node, string xpath, ulong defaultValue)
  {
    string childValue = Child(node, xpath);
    return string.IsNullOrEmpty(childValue) ? defaultValue : XmlConvert.ToUInt64(childValue);
  }

  public static DateTime? DateTime(XmlNode node, string attrName)
  {
    return DateTime(node, attrName, (DateTime?)null);
  }

  public static DateTime? DateTime(XmlNode node, string attrName, DateTime? defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ?
      defaultValue : XmlConvert.ToDateTime(attrValue, XmlDateTimeSerializationMode.Unspecified);
  }

  public static decimal Decimal(XmlNode node, string attrName)
  {
    return Decimal(node, attrName, 0);
  }

  public static decimal Decimal(XmlNode node, string attrName, decimal defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToDecimal(attrValue);
  }

  public static double Double(XmlNode node, string attrName)
  {
    return Double(node, attrName, 0);
  }

  public static double Double(XmlNode node, string attrName, double defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToDouble(attrValue);
  }

  public static Guid Guid(XmlNode node, string attrName)
  {
    return Guid(node, attrName, System.Guid.Empty);
  }

  public static Guid Guid(XmlNode node, string attrName, Guid defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToGuid(attrValue);
  }

  public static short Int16(XmlNode node, string attrName)
  {
    return Int16(node, attrName, 0);
  }

  public static short Int16(XmlNode node, string attrName, short defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToInt16(attrValue);
  }

  public static int Int32(XmlNode node, string attrName)
  {
    return Int32(node, attrName, 0);
  }

  public static int Int32(XmlNode node, string attrName, int defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToInt32(attrValue);
  }

  public static long Int64(XmlNode node, string attrName)
  {
    return Int64(node, attrName, 0);
  }

  public static long Int64(XmlNode node, string attrName, long defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToInt64(attrValue);
  }

  public static sbyte SByte(XmlNode node, string attrName)
  {
    return SByte(node, attrName, 0);
  }

  public static sbyte SByte(XmlNode node, string attrName, sbyte defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToSByte(attrValue);
  }

  public static float Single(XmlNode node, string attrName)
  {
    return Single(node, attrName, 0); 
  }

  public static float Single(XmlNode node, string attrName, float defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToSingle(attrValue);
  }

  public static string String(XmlNode node, string attrName)
  {
    return String(node, attrName, string.Empty);
  }

  public static string String(XmlNode node, string attrName, string defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : attrValue;
  }

  public static TimeSpan TimeSpan(XmlNode node, string attrName)
  {
    return TimeSpan(node, attrName, new TimeSpan());
  }

  public static TimeSpan TimeSpan(XmlNode node, string attrName, TimeSpan defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToTimeSpan(attrValue);
  }

  public static ushort UInt16(XmlNode node, string attrName)
  {
    return UInt16(node, attrName, 0);
  }

  public static ushort UInt16(XmlNode node, string attrName, ushort defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToUInt16(attrValue);
  }

  public static uint UInt32(XmlNode node, string attrName)
  {
    return UInt32(node, attrName, 0);
  }

  public static uint UInt32(XmlNode node, string attrName, uint defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToUInt32(attrValue);
  }

  public static ulong UInt64(XmlNode node, string attrName)
  {
    return UInt64(node, attrName, 0);
  }

  public static ulong UInt64(XmlNode node, string attrName, ulong defaultValue)
  {
    string attrValue = Attr(node, attrName);
    return string.IsNullOrEmpty(attrValue) ? defaultValue : XmlConvert.ToUInt64(attrValue);
  }

  public static bool IsEmpty(XmlAttribute attr)
  {
    return attr == null || string.IsNullOrEmpty(attr.Value); 
  }

  public static bool IsEmpty(XmlNode node, string attrName)
  {
    return IsEmpty(AttrNode(node, attrName));
  }

  public static string[] List(XmlNode node, string attrName)
  {
    XmlAttribute attr = AttrNode(node, attrName);
    return attr == null ? new string[0] : List(attr.Value);
  }

  public static string[] List(string data)
  {
    return string.IsNullOrEmpty(data) ? new string[0] : split.Split(data.Trim()); 
  }

  static readonly Regex split = new Regex(@"\s+", RegexOptions.Singleline);
}

} // namespace AdamMil.IO