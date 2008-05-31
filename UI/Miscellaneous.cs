/*
AdamMil.UI is a library that provides useful user interface controls for the
.NET framework.

http://www.adammil.net/
Copyright (C) 2008 Adam Milazzo

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
using System.Drawing;
using System.Windows.Forms;

namespace AdamMil.UI
{

#region ClipboardDataFormats
/// <summary>A class containing types of data that are commonly used with the clipboard, and which are supported by
/// the classes defined in this assembly.
/// </summary>
/// <remarks>These type strings should not be used directly with the <see cref="Clipboard"/> class. These exist as a
/// simpler alternative to the <see cref="DataFormats"/> class, which contains Windows-specific types and types that
/// are more specific than necessary. As such, one of these type strings may map onto several of the type strings
/// defined in <see cref="DataFormats"/>. They are strings rather than an enumeration to allow new types to be defined
/// by other assemblies.
/// </remarks>
public static class ClipboardDataFormats
{
  /// <summary>Audio data.</summary>
  public const string Audio = "Audio";
  /// <summary>Comma-separated value format, commonly used to exchange tabular data.</summary>
  public const string CSV = "CSV";
  /// <summary>A list of files.</summary>
  public const string Files = "Files";
  /// <summary>Hypertext markup language.</summary>
  public const string Html = "HTML";
  /// <summary>Image data.</summary>
  public const string Image = "Image";
  /// <summary>A hyperlink.</summary>
  public const string Link = "Link";
  /// <summary>Rich text format.</summary>
  public const string RTF = "RTF";
  /// <summary>Plain text.</summary>
  public const string Text = "Text";
}
#endregion

#region FontHelper
/// <summary>Provides helper classes to determine which fonts are installed and retrieve font families by name.</summary>
public static class FontHelpers
{
  static FontHelpers()
  {
    using(System.Drawing.Text.InstalledFontCollection fonts = new System.Drawing.Text.InstalledFontCollection())
    {
      installedFonts = new Dictionary<string, FontFamily>(fonts.Families.Length, StringComparer.OrdinalIgnoreCase);
      foreach(FontFamily family in fonts.Families) installedFonts[family.Name] = family;
    }
  }

  /// <summary>Returns the font family with the given name, or null if the named font is not installed.</summary>
  public static FontFamily GetFontFamily(string fontName)
  {
    if(fontName == null) throw new ArgumentNullException();
    FontFamily family;
    installedFonts.TryGetValue(fontName, out family);
    return family;
  }

  /// <summary>Returns true if the given font is installed.</summary>
  public static bool IsFontInstalled(string fontName)
  {
    if(fontName == null) throw new ArgumentNullException();
    return installedFonts.ContainsKey(fontName);
  }

  /// <summary>Returns an array containing the font families installed on the system.</summary>
  public static FontFamily[] GetInstalledFonts()
  {
    return new List<FontFamily>(installedFonts.Values).ToArray();
  }

  static readonly Dictionary<string, FontFamily> installedFonts;
}
#endregion

#region Span
/// <summary>Represents a span within a discrete range. The span is stored as a starting index and a length, both of
/// which must be non-negative.
/// </summary>
public struct Span
{
  /// <summary>Initializes this <see cref="Span"/> with the given start index and length.</summary>
  public Span(int start, int length)
  {
    if(start < 0 || length < 0) throw new ArgumentOutOfRangeException();
    this.start  = start;
    this.length = length;
  }

  /// <summary>Gets or sets the start index of the span.</summary>
  public int Start
  {
    get { return start; }
    set
    {
      if(value < 0) throw new ArgumentOutOfRangeException();
      start = value;
    }
  }

  /// <summary>Gets or sets the length of the span.</summary>
  public int Length
  {
    get { return length; }
    set
    {
      if(value < 0) throw new ArgumentOutOfRangeException();
      length = value;
    }
  }

  /// <summary>Gets the index immediately after the span.</summary>
  public int End
  {
    get { return Start + Length; }
  }

  /// <summary>Determines whether this span fully contains the given span.</summary>
  public bool Contains(Span span)
  {
    return Start <= span.Start && End >= span.End && Length != 0 && span.Length != 0;
  }

  /// <summary>Determines whether this span contains the given index.</summary>
  public bool Contains(int offset)
  {
    return Start <= offset && offset < End;
  }

  /// <summary>Determines whether this span intersects the given span.</summary>
  public bool Intersects(Span span)
  {
    return span.End > Start && span.Start < End && Length != 0 && span.Length != 0;
  }

  /// <summary>Returns true if the given object is a span that is equal to this span.</summary>
  public override bool Equals(object obj)
  {
    return obj is Span ? this == (Span)obj : false;
  }

  /// <summary>Returns true if the given span is equal to this span.</summary>
  public bool Equals(Span span)
  {
    return this == span;
  }

  /// <include file="documentation.xml" path="/UI/Common/GetHashCode/*"/>
  public override int GetHashCode()
  {
 	  return Start ^ (Length<<16);
  }

  /// <summary>Determines if the two spans are identical.</summary>
  public static bool operator==(Span a, Span b) { return a.Start == b.Start && a.Length == b.Length; }
  /// <summary>Determines if the two spans are not identical.</summary>
  public static bool operator!=(Span a, Span b) { return a.Start != b.Start || a.Length != b.Length; }

  int start, length;
}
#endregion

} // namespace AdamMil.UI