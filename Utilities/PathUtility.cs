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
using System.IO;

namespace AdamMil.Utilities
{

/// <summary>Provides utilities to manipulate file paths.</summary>
public static class PathUtility
{
  /// <summary>Appends a suffix to the portion of a filename before the extension. For instance, if the
  /// filename is "C:\foo.txt" and the suffix is "-2", the return value will be "C:\foo-2.txt".
  /// </summary>
  public static string AppendToFileName(string fileName, string suffix)
  {
    if(string.IsNullOrEmpty(fileName)) throw new ArgumentException("File name cannot be empty.");
    // we use this code rather than Path.GetDirectoryName() and Path.Combine() to avoid changing the path separator character
    string directory = null;
    int dirSlash = fileName.LastIndexOfAny(DirectorySeparatorChars);
    if(dirSlash != -1)
    {
      directory = fileName.Substring(0, dirSlash+1); // include the slash in the directory name
      fileName  = fileName.Substring(dirSlash+1);
    }
    return directory + Path.GetFileNameWithoutExtension(fileName) + suffix + Path.GetExtension(fileName);
  }

  /// <summary>Determines whether two path strings reference the same file or directory.</summary>
  public static bool ArePathsEqual(string a, string b)
  {
    return string.Equals(NormalizePath(a), NormalizePath(b), StringComparison.Ordinal);
  }

  /// <summary>Works like <see cref="Directory.GetFiles(string,string)"/>, but without the unintuitive behavior regarding
  /// wildcards with 3-character extensions.
  /// </summary>
  public static string[] GetFiles(string directory, string pattern)
  {
    return GetFiles(directory, pattern, SearchOption.TopDirectoryOnly);
  }

  /// <summary>Works like <see cref="Directory.GetFiles(string,string,SearchOption)" />, but without the unintuitive behavior
  /// regarding wildcards with 3-character extensions.
  /// </summary>
  public static string[] GetFiles(string directory, string pattern, SearchOption options)
  {
    return FilterFiles(Directory.GetFiles(directory, pattern, options), pattern);
  }

  /// <summary>Works like <see cref="Directory.GetFileSystemEntries(string,string)" />, but without the unintuitive behavior
  /// regarding wildcards with 3-character extensions.
  /// </summary>
  public static string[] GetFileSystemEntries(string directory, string pattern)
  {
    return FilterFiles(Directory.GetFileSystemEntries(directory, pattern), pattern);
  }

  /// <summary>Creates a new file in the system temporary file directory, with the given extension.</summary>
  public static string GetTempFileNameWithExtension(string extension)
  {
    return GetTempFileNameWithExtension(Path.GetTempPath(), extension);
  }

  /// <summary>Creates a new file in the given directory (which must exist), with the given extension.</summary>
  public static string GetTempFileNameWithExtension(string tempDirectory, string extension)
  {
    if(!Directory.Exists(tempDirectory)) throw new ArgumentException();
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
        using(new FileStream(fileName, FileMode.CreateNew, FileAccess.Write)) { }
        return fileName;
      }
      catch(IOException) { }
    }

    throw new IOException("Unable to allocate a temporary file.");
  }


  /// <summary>Creates a new file in the given directory (which must exist), with a name similar to the given file name.</summary>
  public static string GetUniqueFileName(string directory, string fileName)
  {
    if(!Directory.Exists(directory)) throw new ArgumentException();

    fileName = Path.GetFileName(fileName);
    string testPath = Path.Combine(directory, fileName);
    int suffix = 1;
    do
    {
      if(!File.Exists(testPath))
      {
        try
        {
          using(new FileStream(testPath, FileMode.CreateNew, FileAccess.Write)) { }
          return testPath;
        }
        catch(IOException) { }
      }

      ++suffix;
      testPath = Path.Combine(directory, AppendToFileName(fileName, suffix.ToStringInvariant()));
    } while(suffix <= int.MaxValue);

    return GetTempFileNameWithExtension(directory, Path.GetExtension(fileName));
  }

  /// <summary>Normalizes a path by converting it to upper case, normalizing directory separator
  /// characters, and removing a trailing slash if there is one.
  /// </summary>
  public static string NormalizePath(string path)
  {
    if(!string.IsNullOrEmpty(path)) // let empty strings and nulls pass through
    {
      // some weird paths start with this, but .NET thinks it's illegal, so strip it off.
      // it doesn't change the way Windows handles it.
      if(path.StartsWith(@"\??\", StringComparison.Ordinal)) path = path.Substring(4);

      path = Path.GetFullPath(path).ToUpperInvariant()
                 .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

      // strip off a trailing slash if there is one
      if(path.Length != 0 && path[path.Length-1] == Path.DirectorySeparatorChar)
      {
        path = path.Substring(0, path.Length-1);
      }
    }

    return path;
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

  /// <remarks>If you give a wildcard pattern with a 3-character extension and an asterisk to Directory.GetFiles(), for instance
  /// *.xml, it may return files with extensions longer than 3 characters. For instance, it may return foo.xml~ or foo.xmlabc. It
  /// doesn't exhibit this behavior with extensions of more or less than 3 characters. This version of GetFiles() will check if
  /// the pattern ends with a 3-character extension, and if so, will strip out any filenames with extensions longer than 3
  /// characters.
  /// </remarks>
  static string[] FilterFiles(string[] files, string pattern)
  {
    if(pattern != null && pattern.Length >= 4 && pattern[pattern.Length-4] == '.') // if it may end in a 3-char extension
    {
      for(int i=pattern.Length-3; i<pattern.Length; i++) // if the extension is shorter or contains wildcards, simply return
      {
        char c = pattern[i];
        if(c == '*' || c == '?' || c == '.') return files;
      }

      bool foundAsterisk = false;
      for(int i=0; i<pattern.Length-4; i++) // if none of the non-extension pattern characters are '*', then simply return
      {
        if(pattern[i] == '*')
        {
          foundAsterisk = true;
          break;
        }
      }

      if(foundAsterisk) // if the pattern contains an asterisk and a 3-character extension...
      {
        for(int i=0; i<files.Length; i++) // see if any files have an extension that doesn't match
        {
          string file = files[i];
          int lastPeriod = file.LastIndexOf('.');
          // if any file has an extension that doesn't match, create a new array with only the ones that do match
          if(lastPeriod == -1 || lastPeriod != file.Length-4)
          {
            List<string> newFiles = new List<string>(files.Length-1);
            for(int j=0; j<i; j++) newFiles.Add(files[j]); // add the files up to 'i'
            for(i++; i<files.Length; i++) // add the files that have the correct extensions
            {
              file = files[i];
              lastPeriod = file.LastIndexOf('.');
              if(lastPeriod != -1 && lastPeriod == file.Length-4) newFiles.Add(file);
            }
            files = newFiles.ToArray();
            break;
          }
        }
      }
    }

    return files;
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
