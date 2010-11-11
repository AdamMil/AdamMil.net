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
using System.IO;

namespace AdamMil.Utilities
{

/// <summary>Provides utilities to manipulate file paths.</summary>
public static class PathUtility
{
  /// <summary>Appends the to the portion of the filename before the extension. For instance, if the
  /// filename is "C:\foo.txt" and the suffix is "-2", the return value will be "C:\foo-2.txt".
  /// </summary>
  public static string AppendToFileName(string filename, string suffix)
  {
    // we use this code rather than Path.GetDirectoryName() and Path.Combine() to avoid changing the path separator character
    string directory = null;
    int dirSlash = filename.LastIndexOfAny(DirectorySeparatorChars);
    if(dirSlash != -1)
    {
      directory = filename.Substring(0, dirSlash+1);
      filename  = filename.Substring(dirSlash+1);
    }
    return directory + Path.GetFileNameWithoutExtension(filename) + suffix + Path.GetExtension(filename);
  }

  /// <summary>Removes invalid filename characters from the given string, and returns it.</summary>
  public static string StripInvalidFileNameChars(string name)
  {
    return StringUtility.Remove(name, Path.GetInvalidFileNameChars());
  }

  /// <summary>Removes invalid path characters from the given string, and returns it.</summary>
  public static string StripInvalidPathChars(string name)
  {
    return StringUtility.Remove(name, Path.GetInvalidPathChars());
  }

  static readonly char[] DirectorySeparatorChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
}

} // namespace AdamMil.Utilities
