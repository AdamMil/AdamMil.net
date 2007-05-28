/*
AdamMil.IO is a library that provides high performance and high level IO
tools for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007 Adam Milazzo

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
using System.Text;
using System.Runtime.InteropServices;

namespace AdamMil.IO
{

public static unsafe partial class IOH
{
  #region CalculateSize
  /// <summary>This method returns how many bytes would be written by a formatted write if given the specified
  /// format string and variable-length arguments to match, assuming UTF-8 encoding for encoded strings.
  /// </summary>
  /// <param name="args">The variable-width arguments where a prefix was not given as expected by the formatter.
  /// Fixed-width parameters should not be included. If you want to include fixed-width parameters too, use
  /// <see cref="CalculateSize(bool,string,object[])"/>.
  /// </param>
  public static int CalculateSize(string format, params object[] args)
  {
    return CalculateSize(false, Encoding.UTF8, format, args);
  }

  /// <summary>This method returns how many bytes would be written by a formatted write if given the specified
  /// format string and variable-length arguments to match.
  /// </summary>
  /// <param name="allArgs">If true, this method will expect all arguments to be passed. Otherwise, it will only
  /// expect the variable-length arguments where a prefix was not given in the format string.
  /// </param>
  /// <param name="args">The arguments expected by the formatter.</param>
  public static int CalculateSize(bool allArgs, string format, params object[] args)
  {
    return CalculateSize(allArgs, Encoding.UTF8, format, args);
  }

  /// <summary>This method returns how many bytes would be written by a formatted write if given the specified
  /// format string and variable-length arguments to match.
  /// </summary>
  /// <param name="allArgs">If true, this method will expect all arguments to be passed. Otherwise, it will only
  /// expect the variable-length arguments where a prefix was not given in the format string.
  /// </param>
  /// <param name="args">The arguments expected by the formatter.</param>
  public static int CalculateSize(bool allArgs, Encoding encoding, string format, params object[] args)
  {
    if(encoding == null || format == null || args == null) throw new ArgumentNullException();

    int si=0, ai=0, prefix, size=0, skipArgs=0;
    TextMode textMode = TextMode.EightBit;
    char starType   = '\0';
    bool starPrefix = false;

    try
    {
      for(; si<format.Length; si++)
      {
        char c = format[si];
        
        // first, get the prefix, if any
        if(char.IsDigit(c)) // if we find a number, use that as the prefix
        {
          prefix = c-'0';
          while(si++<format.Length && char.IsDigit(c=format[si]) && prefix >= 0) prefix = prefix*10 + (c-'0');
          if(prefix < 0) throw new ArgumentException("Overflow in prefix.");
        }
        else if(c == '?')// if there's a question mark, the prefix is the length of the next argument
        {
          prefix = QuestionPrefix;
          c = ' '; // make it read the next character as the code below
        }
        else if(c == '*') // if there's an asterisk, the length of the next argument will be written to the stream
        {
          if(starPrefix) throw new ArgumentException("You can't use two '*' prefixes in a row.");
          prefix     = StarPrefix;
          starPrefix = true;
          c          = ' '; // make it read the next character as the code below
        }
        else if(char.IsWhiteSpace(c)) continue;
        else prefix = DefaultPrefix;

        // now read until we have what should be a format code
        while(char.IsWhiteSpace(c))
        {
          if(++si >= format.Length) throw new ArgumentException("Missing format code after prefix.");
          c = format[si];
        }

        if(prefix == StarPrefix)
        {
          starType = c;
          continue; // go and read the next item
        }

        if(prefix != DefaultPrefix && (c == 'A' || c == 'U' || c == 'E' || c == '<' || c == '>' || c == '='))
        {
          throw new ArgumentException(string.Format("No prefix allowed before '{0}'", c));
        }

        if(prefix == QuestionPrefix)
        {
          if(c == 's' || c == 'p') prefix = DefaultPrefix;
          else
          {
            // we don't consume the argument in case it's an array of variable-width integer
            prefix   = GetArg<Array>(args, ai).Length;
            skipArgs = 1; // but we'll skip it later
          }
        }

        if(prefix == DefaultPrefix)
        {
          if(c == 's' || c == 'p')
          {
            string str = GetArg<string>(args, ref ai);
            prefix = textMode == TextMode.Encoded ? encoding.GetByteCount(str) : str.Length;
          }
          else if(c != 'A' && c != 'U' && c != 'E' && c != '<' && c != '>' && c != '=')
          {
            prefix   = 1;
            if(c != 'v' && c != 'V') skipArgs = allArgs ? prefix : 0;
          }
        }
        // if we have a literal prefix and we're encoding a string, convert the prefix from characters to bytes
        else if(textMode == TextMode.Encoded && (c == 's' || c == 'p'))
        {
          string str = GetArg<string>(args, ref ai);
          if(prefix > str.Length) str += new string('\0', prefix-str.Length); // pad the string if necessary
          char[] chars = str.ToCharArray();
          prefix = encoding.GetByteCount(chars, 0, prefix);
        }
        else if(allArgs) // with a literal prefix, skip over non-variable arguments if there might be any
        {
          skipArgs = prefix;
        }
        
        // if there was a star-prefixed code on the previous iteration, add the size of the length code,
        // now that we know the prefix
        if(starPrefix)
        {
          switch(starType)
          {
            case 'b': case 'B': size += 1; break;
            case 'w': case 'W': size += 2; break;
            case 'd': case 'D': size += 4; break;
            case 'q': case 'Q': size += 8; break;
            case 'v': size += GetEncodedSize(prefix); break;
            case 'V': size += GetEncodedSize((uint)prefix); break;
            default:
              throw new ArgumentException(string.Format("The code '{0}' cannot be used with a '*' prefix.",
                                                        starType));
          }
          starPrefix = false;
        }
        
        // now add the length of the current iteration's code
        switch(c)
        {
          case 'b': case 'B': case 'x': size += prefix; break;
          case 'w': case 'W': size += prefix*2; break;
          case 'd': case 'D': case 'f': size += prefix*4; break;
          case 'q': case 'Q': case 'F': size += prefix*8; break;
          case 'c': size += textMode == TextMode.EightBit ? prefix : prefix*2; break;
          case 's': case 'p':
            size += textMode == TextMode.UCS2 ? prefix*2 : prefix;
            if(c == 'p') size += GetEncodedSize((uint)prefix);
            break;
          case 'v': case 'V':
          {
            Array array = GetArg<object>(args, ai) as Array;
            if(array != null)
            {
              ai++;
              skipArgs = 0;
              if(prefix > array.Length)
              {
                throw new ArgumentException("Prefix is greater than the number of items in the array.");
              }
            }

            for(int i=0; i<prefix; i++)
            {
              object obj = array == null ? GetArg<object>(args, ref ai) : array.GetValue(i);
              size += c == 'v' ? GetEncodedSize(Convert.ToInt64(obj)) : GetEncodedSize(Convert.ToUInt64(obj));
            }
            break;
          }
          case 'A': textMode = TextMode.EightBit; break;
          case 'E': textMode = TextMode.Encoded; break;
          case 'U': textMode = TextMode.UCS2; break;
          case '<': case '>': case '=': break;
          default: throw new ArgumentException(string.Format("Unexpected character '{0}'", c));
        }

        if(skipArgs != 0) // skip over arguments if necessary
        {
          if(c == 's' || c == 'p') GetArg<string>(args, ref ai);
          else if((GetArg<object>(args, ai) as Array) != null) ai++; // skip over a single array
          else // or N regular arguments
          {
            ai += skipArgs;
            if(ai > args.Length) throw new ArgumentException("Not enough arguments, or invalid prefix.");
          }
          skipArgs = 0;
        }
      }

      if(starPrefix) throw new ArgumentException("A '*' prefixed item was not followed by another code.");
    }
    catch(Exception e)
    {
      throw new ArgumentException(string.Format("Error near char {0}, near argument {1}: {2}", si, ai, e.Message), e);
    }

    return size;
  }
  #endregion

  #region Read
  /// <summary>Reads formatted binary data from the given array, using UTF-8 encoding for encoded strings.</summary>
  /// <param name="index">The starting index of the area in the array from which data should be read.</param>
  /// <returns>Returns the objects read.</returns>
  public static object[] Read(byte[] array, int index, string format)
  {
    return Read(array, index, Encoding.UTF8, format);
  }

  /// <summary>Reads formatted binary data from the given array, using the given encoding for encoded strings.</summary>
  /// <param name="index">The starting index of the area in the array from which data should be read.</param>
  /// <returns>Returns the objects read.</returns>
  public static object[] Read(byte[] array, int index, Encoding encoding, string format)
  {
    int bytesRead;
    return Read(array, index, Encoding.UTF8, format, out bytesRead);
  }

  /// <summary>Reads formatted binary data from the given array, using UTF-8 encoding for encoded strings.</summary>
  /// <param name="index">The starting index of the area in the array from which data should be read.</param>
  /// <returns>Returns the objects read.</returns>
  public static object[] Read(byte[] array, int index, string format, out int bytesRead)
  {
    return Read(array, index, Encoding.UTF8, format, out bytesRead);
  }

  /// <summary>Reads formatted binary data from the given array, using the given encoding for encoded strings.</summary>
  /// <param name="index">The starting index of the area in the array from which data should be read.</param>
  /// <returns>Returns the objects read.</returns>
  public static object[] Read(byte[] array, int index, Encoding encoding, string format, out int bytesRead)
  {
    if(array == null) throw new ArgumentNullException();
    using(BinaryReader br = new BinaryReader(array, index, array.Length-index, BitConverter.IsLittleEndian))
    {
      return Read(br, encoding, format, out bytesRead);
    }
  }

  /// <summary>Reads formatted binary data from the given stream, using UTF-8 encoding for encoded strings.</summary>
  /// <returns>Returns the objects read.</returns>
  public static object[] Read(System.IO.Stream stream, string format)
  {
    return Read(stream, Encoding.UTF8, format);
  }

  /// <summary>Reads formatted binary data from the given stream, using the given encoding for encoded strings.</summary>
  /// <returns>Returns the objects read.</returns>
  public static object[] Read(System.IO.Stream stream, Encoding encoding, string format)
  {
    int bytesRead;
    return Read(stream, encoding, format, out bytesRead);
  }

  /// <summary>Reads formatted binary data from the given stream, using UTF-8 encoding for encoded strings.</summary>
  /// <returns>Returns the objects read.</returns>
  public static object[] Read(System.IO.Stream stream, string format, out int bytesRead)
  {
    return Read(stream, Encoding.UTF8, format, out bytesRead);
  }

  /// <summary>Reads formatted binary data from the given stream, using the given encoding for encoded strings.</summary>
  /// <returns>Returns the objects read.</returns>
  public static object[] Read(System.IO.Stream stream, Encoding encoding, string format, out int bytesRead)
  {
    using(BinaryReader br = new BinaryReader(stream))
    {
      return Read(br, encoding, format, out bytesRead);
    }
  }

  /// <summary>Reads formatted binary data from the given <see cref="BinaryReader"/>, using UTF-8 encoding for
  /// encoded strings.
  /// </summary>
  /// <returns>Returns the objects read.</returns>
  public static object[] Read(BinaryReader reader, string format)
  {
    return Read(reader, Encoding.UTF8, format);
  }

  /// <summary>Reads formatted binary data from the given <see cref="BinaryReader"/>, using the given encoding for
  /// encoded strings.
  /// </summary>
  /// <returns>Returns the objects read.</returns>
  public static object[] Read(BinaryReader reader, Encoding encoding, string format)
  {
    int bytesRead;
    return Read(reader, encoding, format, out bytesRead);
  }

  /// <summary>Reads formatted binary data from the given <see cref="BinaryReader"/>, using UTF-8 encoding for
  /// encoded strings.
  /// </summary>
  /// <returns>Returns the objects read.</returns>
  public static object[] Read(BinaryReader reader, string format, out int bytesRead)
  {
    return Read(reader, Encoding.UTF8, format, out bytesRead);
  }

  /// <summary>Reads formatted binary data from the given <see cref="BinaryReader"/>, using the given encoding for
  /// encoded strings.
  /// </summary>
  /// <returns>Returns the objects read.</returns>
  public static object[] Read(BinaryReader reader, Encoding encoding, string format, out int bytesRead)
  {
    bytesRead = 0;
    if(reader == null || encoding == null || format == null) throw new ArgumentNullException();

    object[] ret = new object[CalculateOutputs(format)];
    int si=0, ri=0, prefix;
    TextMode textMode = TextMode.EightBit;
    char starType = '\0';
    bool starPrefix = false, originalEndianness = reader.LittleEndian;

    try
    {
      reader.LittleEndian = BitConverter.IsLittleEndian;

      for(; si<format.Length; si++)
      {
        char c = format[si];
        
        // first, get the prefix, if any
        if(char.IsDigit(c)) // if we find a number, use that as the prefix
        {
          prefix = c-'0';
          while(si++<format.Length && char.IsDigit(c=format[si]) && prefix >= 0) prefix = prefix*10 + (c-'0');
          if(prefix < 0) throw new ArgumentException("Overflow in prefix.");
        }
        else if(c == '?')// if there's a question mark, the prefix is the length of the next argument
        {
          prefix = QuestionPrefix;
          c = ' '; // make it read the next character as the code below
        }
        else if(c == '*') // if there's an asterisk, the length of the next argument will be written to the stream
        {
          if(starPrefix) throw new ArgumentException("You can't use two '*' prefixes in a row.");
          prefix     = StarPrefix;
          starPrefix = true;
          c          = ' '; // make it read the next character as the code below
        }
        else if(char.IsWhiteSpace(c)) continue;
        else if(c == 's') prefix = QuestionPrefix;
        else prefix = DefaultPrefix;

        // now read until we have what should be a format code
        while(char.IsWhiteSpace(c))
        {
          if(++si >= format.Length) throw new ArgumentException("Missing format code after prefix.");
          c = format[si];
        }

        if(prefix == StarPrefix)
        {
          starType = c;
          continue; // go and read the next item
        }

        // if we have a star prefix, the prefix of the next item is the value of the starred item
        if(starPrefix)
        {
          if(prefix != QuestionPrefix)
          {
            throw new ArgumentException("A '*' prefixed item must be followed by a '?' prefixed item.");
          }

          switch(starType)
          {
            case 'b': case 'B':
              prefix = reader.ReadByte();
              bytesRead++;
              break;
            case 'w': case 'W':
              prefix = reader.ReadUInt16();
              bytesRead += 2;
              break;
            case 'd': case 'D':
              prefix = reader.ReadInt32();
              bytesRead += 4;
              break;
            case 'q': case 'Q':
              prefix = (int)reader.ReadInt64();
              bytesRead += 8;
              break;
            case 'v':
              prefix = reader.ReadEncodedInt32();
              bytesRead += GetEncodedSize(prefix);
              break;
            case 'V':
              prefix = (int)reader.ReadEncodedUInt32();
              bytesRead += GetEncodedSize((uint)prefix);
              break;
            default:
              throw new ArgumentException(string.Format("The code '{0}' cannot be used with a '*' prefix.",
                                                        starType));
          }

          starPrefix = false;
        }
        else if(prefix != DefaultPrefix && (c == 'A' || c == 'U' || c == 'E' || c == '<' || c == '>' || c == '='))
        {
          throw new ArgumentException(string.Format("No prefix allowed before '{0}'", c));
        }
        else if(prefix == QuestionPrefix)
        {
          throw new ArgumentException("A '?' prefixed item must be preceded by a '*' prefixed item.");
        }

        switch(c)
        {
          case 'x':
            if(prefix == DefaultPrefix)
            {
              reader.Skip(1);
              prefix = 1;
            }
            else reader.Skip(prefix);
            bytesRead += prefix;
            break;

          case 'b':
            if(prefix == DefaultPrefix)
            {
              AddOutput(ret, ref ri, reader.ReadSByte());
              prefix = 1;
            }
            else AddOutput(ret, ref ri, ReadArray(reader, new sbyte[prefix], 1, true));
            bytesRead += prefix;
            break;

          case 'B':
            if(prefix == DefaultPrefix)
            {
              AddOutput(ret, ref ri, reader.ReadByte());
              prefix = 1;
            }
            else AddOutput(ret, ref ri, ReadArray(reader, new byte[prefix], 1, true));
            bytesRead += prefix;
            break;

          case 'w':
            if(prefix == DefaultPrefix)
            {
              AddOutput(ret, ref ri, reader.ReadInt16());
              prefix = 1;
            }
            else AddOutput(ret, ref ri, ReadArray(reader, new short[prefix], 2, true));
            bytesRead += prefix*2;
            break;

          case 'W':
            if(prefix == DefaultPrefix)
            {
              AddOutput(ret, ref ri, reader.ReadUInt16());
              prefix = 1;
            }
            else AddOutput(ret, ref ri, ReadArray(reader, new ushort[prefix], 2, true));
            bytesRead += prefix*2;
            break;

          case 'd':
            if(prefix == DefaultPrefix)
            {
              AddOutput(ret, ref ri, reader.ReadInt32());
              prefix = 1;
            }
            else AddOutput(ret, ref ri, ReadArray(reader, new int[prefix], 4, true));
            bytesRead += prefix*4;
            break;

          case 'D':
            if(prefix == DefaultPrefix)
            {
              AddOutput(ret, ref ri, reader.ReadUInt32());
              prefix = 1;
            }
            else AddOutput(ret, ref ri, ReadArray(reader, new uint[prefix], 4, true));
            bytesRead += prefix*4;
            break;

          case 'q':
            if(prefix == DefaultPrefix)
            {
              AddOutput(ret, ref ri, reader.ReadInt64());
              prefix = 1;
            }
            else AddOutput(ret, ref ri, ReadArray(reader, new long[prefix], 8, true));
            bytesRead += prefix*8;
            break;

          case 'Q':
            if(prefix == DefaultPrefix)
            {
              AddOutput(ret, ref ri, reader.ReadUInt64());
              prefix = 1;
            }
            else AddOutput(ret, ref ri, ReadArray(reader, new ulong[prefix], 8, true));
            bytesRead += prefix*8;
            break;

          case 'f':
            if(prefix == DefaultPrefix)
            {
              AddOutput(ret, ref ri, reader.ReadSingle());
              prefix = 1;
            }
            else AddOutput(ret, ref ri, ReadArray(reader, new float[prefix], 4, false));
            bytesRead += prefix*4;
            break;

          case 'F':
            if(prefix == DefaultPrefix)
            {
              AddOutput(ret, ref ri, reader.ReadDouble());
              prefix = 1;
            }
            else AddOutput(ret, ref ri, ReadArray(reader, new double[prefix], 8, false));
            bytesRead += prefix*8;
            break;

          case 'c':
            if(textMode == TextMode.EightBit)
            {
              if(prefix == DefaultPrefix)
              {
                AddOutput(ret, ref ri, (char)reader.ReadByte());
                prefix = 1;
              }
              else
              {
                byte[] bytes = new byte[prefix];
                ReadArray(reader, bytes, 1, true);
                char[] chars = new char[prefix];
                for(int i=0; i<chars.Length; i++) chars[i] = (char)bytes[i];
                AddOutput(ret, ref ri, chars);
              }
              bytesRead += prefix;
            }
            else
            {
              if(prefix == DefaultPrefix)
              {
                AddOutput(ret, ref ri, reader.ReadChar());
                prefix = 1;
              }
              else AddOutput(ret, ref ri, ReadArray(reader, new char[prefix], 2, true));
              bytesRead += prefix*2;
            }
            break;

          case 's': case 'p':
          {
            if(prefix == DefaultPrefix)
            {
              if(c == 's') throw new ArgumentException("Strings must have a known length.");
              prefix = (int)reader.ReadEncodedUInt32();
              bytesRead += GetEncodedSize((uint)prefix);
            }

            string str;
            if(textMode == TextMode.UCS2)
            {
              str = reader.ReadString(prefix);
              bytesRead += prefix*2;
            }
            else
            {
              byte[] bytes = reader.ReadByte(prefix);
              if(textMode == TextMode.Encoded) str = encoding.GetString(bytes);
              else
              {
                char[] chars = new char[bytes.Length];
                for(int i=0; i<chars.Length; i++) chars[i] = (char)bytes[i];
                str = new string(chars);
              }
              bytesRead += prefix;
            }
            AddOutput(ret, ref ri, str);
            break;
          }

          case 'v':
            if(prefix == DefaultPrefix)
            {
              long value = reader.ReadEncodedInt64();
              bytesRead += GetEncodedSize(value);
              AddOutput(ret, ref ri, value);
            }
            else
            {
              long[] values = new long[prefix];
              for(int i=0; i<values.Length; i++)
              {
                values[i] = reader.ReadEncodedInt64();
                bytesRead += GetEncodedSize(values[i]);
              }
              AddOutput(ret, ref ri, values);
            }
            break;

          case 'V':
            if(prefix == DefaultPrefix)
            {
              ulong value = reader.ReadEncodedUInt64();
              bytesRead += GetEncodedSize(value);
              AddOutput(ret, ref ri, value);
            }
            else
            {
              ulong[] values = new ulong[prefix];
              for(int i=0; i<values.Length; i++)
              {
                values[i] = reader.ReadEncodedUInt64();
                bytesRead += GetEncodedSize(values[i]);
              }
              AddOutput(ret, ref ri, values);
            }
            break;

          case 'A': textMode = TextMode.EightBit; break;
          case 'E': textMode = TextMode.Encoded; break;
          case 'U': textMode = TextMode.UCS2; break;
          case '<': reader.LittleEndian = true; break;
          case '>': reader.LittleEndian = false; break;
          case '=': reader.LittleEndian = BitConverter.IsLittleEndian; break;
          default: throw new ArgumentException(string.Format("Unexpected character '{0}'", c));
        }
      }

      if(starPrefix) throw new ArgumentException("A '*' prefixed item was not followed by another code.");
    }
    catch(Exception e)
    {
      throw new ArgumentException(string.Format("Error near char {0}: {1}", si, e.Message), e);
    }
    finally { reader.LittleEndian = originalEndianness; }

    System.Diagnostics.Debug.Assert(ri == ret.Length);
    return ret;
  }

  static void AddOutput(object[] array, ref int index, object value)
  {
    array[index++] = value;
  }

  /// <summary>Calculates the number of outputs that would be read from a format string.</summary>
  static int CalculateOutputs(string format)
  { 
    if(format == null) throw new ArgumentNullException();
    int count = 0;
    for(int i=0; i<format.Length; i++)
    { 
      char c = format[i];
      switch(c)
      { 
        case 'b': case 'B': case 'w': case 'W': case 'd': case 'D': case 'f': case 'F':
        case 'q': case 'Q': case 'c': case 'p': case 's': case 'v': case 'V':
          count++;
          break;
        case '*': count--; break;
        case 'A': case 'U': case 'E': case '<': case '>': case '=': case '?': break;
        default:
          if(char.IsDigit(c) || char.IsWhiteSpace(c)) break;
          throw new ArgumentException(string.Format("Unexpected character '{0}'", c));
      }
    }

    if(count < 0) throw new ArgumentException("Invalid format string. Too many '*' prefixes.");
    return count;
  }

  static Array ReadArray(BinaryReader reader, Array array, int wordSize, bool respectEndianness)
  {
    if(array.Length != 0)
    {
      GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
      try
      {
        void* arrayPtr = handle.AddrOfPinnedObject().ToPointer();
        if(respectEndianness) reader.Read(arrayPtr, array.Length, wordSize);
        else reader.Read(arrayPtr, array.Length*wordSize);
      }
      finally { handle.Free(); }
    }
    return array;
  }
  #endregion

  #region Write
  /// <summary>Writes formatted binary data to the given stream, using UTF-8 encoding for encoded strings.</summary>
  /// <returns>Returns the number of bytes written.</returns>
  public static int Write(System.IO.Stream stream, string format, params object[] args)
  {
    return Write(stream, Encoding.UTF8, format, args);
  }

  /// <summary>Writes formatted binary data to the given stream, using the given encoding for encoded strings.</summary>
  /// <returns>Returns the number of bytes written.</returns>
  public static int Write(System.IO.Stream stream, Encoding encoding, string format, params object[] args)
  {
    using(BinaryWriter bw = new BinaryWriter(stream))
    {
      return Write(bw, encoding, format, args);
    }
  }

  /// <summary>Writes formatted binary data to the given array, using UTF-8 encoding for encoded strings.</summary>
  /// <param name="index">The starting index within the array where data can be written.</param>
  /// <returns>Returns the number of bytes written.</returns>
  public static int Write(byte[] array, int index, string format, params object[] args)
  {
    return Write(array, index, Encoding.UTF8, format, args);
  }

  /// <summary>Writes formatted binary data to the given array, using the given encoding for encoded strings.</summary>
  /// <param name="index">The starting index within the array where data can be written.</param>
  /// <returns>Returns the number of bytes written.</returns>
  public static int Write(byte[] array, int index, Encoding encoding, string format, params object[] args)
  {
    if(array == null) throw new ArgumentNullException();
    using(BinaryWriter bw = new BinaryWriter(array, index, array.Length-index, BitConverter.IsLittleEndian))
    {
      return Write(bw, encoding, format, args);
    }
  }

  /// <summary>Writes formatted binary data to the given <see cref="BinaryWriter"/>, using UTF-8 encoding for encoded
  /// strings.
  /// </summary>
  /// <returns>Returns the number of bytes written.</returns>
  public static int Write(BinaryWriter writer, string format, params object[] args)
  {
    return Write(writer, Encoding.UTF8, format, args);
  }

  /// <summary>Writes formatted binary data to the given <see cref="BinaryWriter"/>, using the given encoding for
  /// encoded strings.
  /// </summary>
  /// <returns>Returns the number of bytes written.</returns>
  public static int Write(BinaryWriter writer, Encoding encoding, string format, params object[] args)
  {
    if(writer == null || encoding == null || format == null || args == null) throw new ArgumentNullException();

    int si=0, ai=0, prefix, size=0;
    TextMode textMode = TextMode.EightBit;
    char starType   = '\0';
    bool starPrefix = false, originalEndianness = writer.LittleEndian;

    try
    {
      writer.LittleEndian = BitConverter.IsLittleEndian;
      for(; si<format.Length; si++)
      {
        char c = format[si];
        object stringValue = null;
        
        // first, get the prefix, if any
        if(char.IsDigit(c)) // if we find a number, use that as the prefix
        {
          prefix = c-'0';
          while(si++<format.Length && char.IsDigit(c=format[si]) && prefix >= 0) prefix = prefix*10 + (c-'0');
          if(prefix < 0) throw new ArgumentException("Overflow in prefix.");
        }
        else if(c == '?')// if there's a question mark, the prefix is the length of the next argument
        {
          prefix = QuestionPrefix;
          c = ' '; // make it read the next character as the code below
        }
        else if(c == '*') // if there's an asterisk, the length of the next argument will be written to the stream
        {
          if(starPrefix) throw new ArgumentException("You can't use two '*' prefixes in a row.");
          prefix     = StarPrefix;
          starPrefix = true;
          c          = ' '; // make it read the next character as the code below
        }
        else if(char.IsWhiteSpace(c)) continue;
        else prefix = DefaultPrefix;

        // now read until we have what should be a format code
        while(char.IsWhiteSpace(c))
        {
          if(++si >= format.Length) throw new ArgumentException("Missing format code after prefix.");
          c = format[si];
        }

        if(prefix == StarPrefix)
        {
          starType = c;
          continue; // go and read the next item
        }

        if(prefix != DefaultPrefix && (c == 'A' || c == 'U' || c == 'E' || c == '<' || c == '>' || c == '='))
        {
          throw new ArgumentException(string.Format("No prefix allowed before '{0}'", c));
        }

        if(prefix == QuestionPrefix)
        {
          if(c == 's' || c == 'p') prefix = DefaultPrefix;
          else prefix = GetArg<Array>(args, ai).Length;
        }

        if(prefix == DefaultPrefix)
        {
          if(c == 's' || c == 'p')
          {
            string str = GetArg<string>(args, ref ai);
            if(textMode == TextMode.Encoded)
            {
              byte[] encoded = encoding.GetBytes(str);
              prefix = encoded.Length;
              stringValue = encoded;
            }
            else
            {
              prefix = str.Length;
              stringValue = str;
            }
          }
          else if(c != 's' && c != 'p' && c != 'A' && c != 'U' && c != 'E' && c != '<' && c != '>' && c != '=')
          {
            prefix = 1;
          }
        }
        
        // if there was a star-prefixed code on the previous iteration, add the size code, now that we know the prefix
        if(starPrefix)
        {
          switch(starType)
          {
            case 'b': size += WriteStarPrefix(writer, prefix, 1, sbyte.MaxValue); break;
            case 'B': size += WriteStarPrefix(writer, prefix, 1, byte.MaxValue); break;
            case 'w': size += WriteStarPrefix(writer, prefix, 2, short.MaxValue); break;
            case 'W': size += WriteStarPrefix(writer, prefix, 2, ushort.MaxValue); break;
            case 'd': case 'D': size += WriteStarPrefix(writer, prefix, 4, int.MaxValue);  break;
            case 'q': case 'Q': size += WriteStarPrefix(writer, prefix, 8, int.MaxValue); break;
            case 'v':
              writer.WriteEncoded(prefix);
              size += GetEncodedSize(prefix);
              break;
            case 'V':
              writer.WriteEncoded((uint)prefix);
              size += GetEncodedSize((uint)prefix);
              break;
            default:
              throw new ArgumentException(string.Format("The code '{0}' cannot be used with a '*' prefix.",
                                                        starType));
          }
          starPrefix = false;
        }
        
        // now add the length of the current iteration's code
        switch(c)
        {
          case 'x': writer.WriteZeros(prefix); size += prefix; break;
          case 'b':
          {
            size += prefix;
            sbyte[] arr;
            if(TryGetArg(args, ref ai, out arr)) WriteArray(writer, arr, prefix, 1, true);
            else do writer.Write(Convert.ToSByte(GetArg(args, ref ai))); while(--prefix != 0);
            break;
          }
          case 'B':
          {
            size += prefix;
            byte[] arr;
            if(TryGetArg(args, ref ai, out arr)) WriteArray(writer, arr, prefix, 1, true);
            else do writer.Write(Convert.ToByte(GetArg(args, ref ai))); while(--prefix != 0);
            break;
          }
          case 'w':
          {
            size += prefix*2;
            short[] arr;
            if(TryGetArg(args, ref ai, out arr)) WriteArray(writer, arr, prefix, 2, true);
            else do writer.Write(Convert.ToInt16(GetArg(args, ref ai))); while(--prefix != 0);
            break;
          }
          case 'W':
          {
            size += prefix*2;
            ushort[] arr;
            if(TryGetArg(args, ref ai, out arr)) WriteArray(writer, arr, prefix, 2, true);
            else do writer.Write(Convert.ToUInt16(GetArg(args, ref ai))); while(--prefix != 0);
            break;
          }
          case 'd':
          {
            size += prefix*4;
            int[] arr;
            if(TryGetArg(args, ref ai, out arr)) WriteArray(writer, arr, prefix, 4, true);
            else do writer.Write(Convert.ToInt32(GetArg(args, ref ai))); while(--prefix != 0);
            break;
          }
          case 'D':
          {
            size += prefix*4;
            uint[] arr;
            if(TryGetArg(args, ref ai, out arr)) WriteArray(writer, arr, prefix, 4, true);
            else do writer.Write(Convert.ToUInt32(GetArg(args, ref ai))); while(--prefix != 0);
            break;
          }
          case 'q':
          {
            size += prefix*8;
            long[] arr;
            if(TryGetArg(args, ref ai, out arr)) WriteArray(writer, arr, prefix, 8, true);
            else do writer.Write(Convert.ToInt64(GetArg(args, ref ai))); while(--prefix != 0);
            break;
          }
          case 'Q':
          {
            size += prefix*8;
            ulong[] arr;
            if(TryGetArg(args, ref ai, out arr)) WriteArray(writer, arr, prefix, 8, true);
            else do writer.Write(Convert.ToUInt64(GetArg(args, ref ai))); while(--prefix != 0);
            break;
          }
          case 'c':
          {
            char[] arr;
            if(textMode == TextMode.EightBit)
            {
              size += prefix;
              if(TryGetArg(args, ref ai, out arr))
              {
                byte[] bytes = new byte[arr.Length];
                for(int i=0; i<bytes.Length; i++) bytes[i] = (byte)arr[i];
                WriteArray(writer, bytes, prefix, 1, false);
              }
              else do writer.Write((byte)Convert.ToChar(GetArg(args, ref ai))); while(--prefix != 0);
            }
            else
            {
              size += prefix*2;
              if(TryGetArg(args, ref ai, out arr)) WriteArray(writer, arr, prefix, 2, true);
              else do writer.Write(Convert.ToChar(GetArg(args, ref ai))); while(--prefix != 0);
            }
            break;
          }
          case 'f':
          {
            size += prefix*4;
            float[] arr;
            if(TryGetArg(args, ref ai, out arr)) WriteArray(writer, arr, prefix, 4, false);
            else do writer.Write(Convert.ToSingle(GetArg(args, ref ai))); while(--prefix != 0);
            break;
          }
          case 'F':
          {
            size += prefix*8;
            double[] arr;
            if(TryGetArg(args, ref ai, out arr)) WriteArray(writer, arr, prefix, 8, false);
            else do writer.Write(Convert.ToDouble(GetArg(args, ref ai))); while(--prefix != 0);
            break;
          }

          case 's': case 'p':
            if(stringValue == null)
            {
              string str = GetArg<string>(args, ref ai);
              if(textMode == TextMode.Encoded)
              {
                if(prefix > str.Length) str += new string('\0', prefix-str.Length); // pad string if necessary
                char[] chars = str.ToCharArray();
                byte[] encoded = encoding.GetBytes(chars, 0, prefix);
                prefix = encoded.Length; // convert prefix from characters to bytes
                stringValue = encoded;
              }
              else stringValue = str;
            }

            if(c == 'p')
            {
              writer.WriteEncoded((uint)prefix);
              size += GetEncodedSize((uint)prefix);
            }

            if(textMode == TextMode.EightBit)
            {
              string str = (string)stringValue;
              byte[] bytes = new byte[prefix];
              for(int i=0, e=Math.Min(str.Length, prefix); i<e; i++) bytes[i] = (byte)str[i];
              writer.Write(bytes);
              size += prefix;
            }
            else if(textMode == TextMode.Encoded)
            {
              writer.Write((byte[])stringValue);
              size += prefix;
            }
            else // UCS-2
            {
              writer.Write((string)stringValue, 0, prefix);
              size += prefix*2;
            }
            break;

          case 'v': case 'V':
          {
            Array array;
            if(TryGetArg(args, ref ai, out array))
            {
              if(prefix > array.Length)
              {
                throw new ArgumentException("The prefix is greater than the number of items in the array.");
              }
            }

            for(int i=0; i<prefix; i++)
            {
              object obj = array == null ? GetArg(args, ref ai) : array.GetValue(i);
              if(c == 'v')
              {
                long value = Convert.ToInt64(obj);
                writer.WriteEncoded(value);
                size += GetEncodedSize(value);
              }
              else
              {
                ulong value = Convert.ToUInt64(obj);
                writer.WriteEncoded(value);
                size += GetEncodedSize(value);
              }
            }
            break;
          }

          case 'A': textMode = TextMode.EightBit; break;
          case 'E': textMode = TextMode.Encoded; break;
          case 'U': textMode = TextMode.UCS2; break;
          case '<': writer.LittleEndian = true; break;
          case '>': writer.LittleEndian = false; break;
          case '=': writer.LittleEndian = BitConverter.IsLittleEndian; break;
          default: throw new ArgumentException(string.Format("Unexpected character '{0}'", c));
        }
      }

      if(starPrefix) throw new ArgumentException("A '*' prefixed item was not followed by another code.");
    }
    catch(Exception e)
    {
      throw new ArgumentException(string.Format("Error near char {0}, near argument {1}: {2}", si, ai, e.Message), e);
    }
    finally { writer.LittleEndian = originalEndianness; }

    return size;
  }

  static void WriteArray(BinaryWriter writer, Array array, int count, int wordSize, bool respectEndianness)
  {
    if(count > array.Length)
    {
      throw new ArgumentException("The prefix is greater than the number of items in the array.");
    }
    if(count != 0)
    {
      GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
      try
      {
        void* srcPtr = handle.AddrOfPinnedObject().ToPointer();
        if(respectEndianness) writer.Write(srcPtr, count, wordSize);
        else writer.Write(srcPtr, count*wordSize, 1);
      }
      finally { handle.Free(); }
    }
  }

  static int WriteStarPrefix(BinaryWriter writer, int prefix, int size, int maxPrefix)
  {
    if(prefix > maxPrefix)
    {
      throw new ArgumentException("The length of the data cannot fit in the type of the star-prefixed code.");
    }

    if(size == 4) writer.Write(prefix);
    else if(size == 2) writer.Write((ushort)prefix);
    else if(size == 1) writer.Write((byte)prefix);
    else if(size == 8) writer.Write((long)prefix);

    return size;
  }
  #endregion

  #region Shared private
  enum TextMode : byte { EightBit, UCS2, Encoded };
  const int StarPrefix = -3, QuestionPrefix = -2, DefaultPrefix = -1;

  static T GetArg<T>(object[] args, ref int index) where T : class
  {
    T value = GetArg<T>(args, index);
    index++;
    return value;
  }

  static T GetArg<T>(object[] args, int index) where T : class
  {
    if(index >= args.Length) throw new ArgumentException("Not enough arguments.");
    T value = args[index] as T;
    if(value == null) throw new ArgumentException("Expected "+typeof(T).Name);
    return value;
  }

  static object GetArg(object[] args, ref int index)
  {
    return GetArg<object>(args, ref index);
  }

  static int GetEncodedSize(long value)
  {
    int size = 1;
    if(value < -64 || value > 63)
    {
      size++;
      value >>= 6;
      while(value < -128 || value > 127)
      {
        size++;
        value >>= 7;
      }
    }
    return size;
  }

  static int GetEncodedSize(ulong value)
  {
    int size = 1;
    while(value > 127)
    {
      size++;
      value >>= 7;
    }
    return size;
  }

  static bool TryGetArg<T>(object[] args, ref int index, out T value) where T : class
  {
    if(index >= args.Length) throw new ArgumentException("Not enough arguments.");
    value = args[index] as T;
    if(value != null)
    {
      index++;
      return true;
    }
    else return false;
  }
  #endregion
}

} // namespace AdamMil.IO