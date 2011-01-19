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

  /// <summary>Creates a new file in the system temporary file directory, with the given extension.</summary>
  public static string GetTempFileNameWithExtension(string extension)
  {
    return GetTempFileNameWithExtension(Path.GetTempPath(), extension);
  }

  /// <summary>Creates a new file in the given directory (which must exist), with the given extension.</summary>
  public static string GetTempFileNameWithExtension(string tempDirectory, string extension)
  {
    Random random = new Random();

    // make sure the extension starts with a period
    if(!string.IsNullOrEmpty(extension) && extension[0] != '.') extension = "." + extension;

    for(int tries=0; tries<10; tries++)
    {
      string fileName;
      do
      {
        fileName = Path.Combine(tempDirectory, GetRandomName(random, 8, false));
        if(!string.IsNullOrEmpty(extension)) fileName += extension;
      }
      while(File.Exists(fileName));

      try
      {
        using(FileStream file = new FileStream(fileName, FileMode.CreateNew, FileAccess.Write)) { }
        return fileName;
      }
      catch { }
    }

    throw new Exception("Unable to allocate a temporary file.");
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

  static string GetRandomName(Random random, int length, bool includeExtension)
  {
    if(length < (includeExtension ? 5 : 1)) throw new ArgumentOutOfRangeException();
    const string ValidChars = "abcdefghijklmnopqrstuvwxyz0123456789~@#%&()-_=+[]{};',";
    char[] chars = new char[length];
    for(int i = 0, extensionIndex = includeExtension ? length-4 : -1; i < chars.Length; i++)
    {
      chars[i] = i == extensionIndex ? '.' : ValidChars[random.Next(ValidChars.Length)];
    }
    return new string(chars);
  }

  static readonly char[] DirectorySeparatorChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
}

} // namespace AdamMil.Utilities
