using System;

namespace AdamMil.Utilities.Encodings
{

#region Base64Decoder
/// <summary>Implements a <see cref="BinaryEncoder"/> that transforms a stream of bytes containing base64-encoded data back into
/// the original binary data.
/// </summary>
public sealed class Base64Decoder : BinaryEncoder
{
  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/CanEncodeInPlace/node()"/>
  public override bool CanEncodeInPlace
  {
    get { return true; }
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/EncodePtr/node()"/>
  [CLSCompliant(false)]
  public unsafe override int Encode(byte* source, int sourceCount, byte* destination, int destinationCapacity, bool flush)
  {
    if(sourceCount < 0 || destinationCapacity < 0) throw new ArgumentOutOfRangeException();

    // go through the characters, collecting valid ones into a buffer, and processing them whenever we get 4 valid characters
    byte* originalBytePtr = destination;
    for(byte* end=source+sourceCount; source != end; source++)
    {
      int value = GetBase64Value(*source);
      if(value != -1)
      {
        charBuffer = (charBuffer << 6) | (byte)value;
        if(++bufferChars == 4)
        {
          if(destinationCapacity < 3) throw Exceptions.InsufficientBufferSpace();
          destination[0] = (byte)(charBuffer >> 16);
          destination[1] = (byte)(charBuffer >> 8);
          destination[2] = (byte)charBuffer;
          destination      += 3;
          destinationCapacity -= 3;
          bufferChars = 0;
        }
      }
    }

    // at this point there are at most 3 bytes in the buffer. if we're flushing the state, convert those remaining bytes too
    if(flush)
    {
      if(bufferChars == 2) // there is 1 data byte in 2 buffer bytes (012345 67xxxx -> xxxx0123 4567xxxx)
      {
        if(destinationCapacity == 0) throw Exceptions.InsufficientBufferSpace();
        *destination++ = (byte)(charBuffer >> 4);
      }
      else if(bufferChars == 3) // there are 2 data bytes in 3 buffer bytes (012345 670123 4567xx -> 01 23456701 234567xx)
      {
        if(destinationCapacity < 2) throw Exceptions.InsufficientBufferSpace();
        destination[0] = (byte)(charBuffer >> 10);
        destination[1] = (byte)(charBuffer >> 2);
        destination += 2;
      }

      bufferChars = 0;
    }

    return (int)(destination - originalBytePtr);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/Encode/node()"/>
  public unsafe override int Encode(byte[] source, int sourceIndex, int sourceCount, byte[] destination, int destinationIndex,
                                    bool flush)
  {
    Utility.ValidateRange(source, sourceIndex, sourceCount);
    if(destination == null) throw new ArgumentNullException();
    if((uint)destinationIndex > (uint)destination.Length) throw new ArgumentOutOfRangeException();

    fixed(byte* srcPtr=source, destPtr=destination)
    {
      return Encode(srcPtr+sourceIndex, sourceCount, destPtr+destinationIndex, destination.Length-destinationIndex, flush);
    }
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/GetByteCountPtr/node()"/>
  [CLSCompliant(false)]
  public unsafe override int GetByteCount(byte* data, int count, bool simulateFlush)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();

    int validCharCount = bufferChars + CountValidChars(data, count), byteCount = validCharCount / 4 * 3;
    if(simulateFlush)
    {
      validCharCount &= 3; // get the number of remainder characters
      if(validCharCount == 3) byteCount += 2;
      else if(validCharCount == 2) byteCount++;
    }
    return byteCount;
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/GetByteCount/node()"/>
  public unsafe override int GetByteCount(byte[] data, int index, int count, bool simulateFlush)
  {
    Utility.ValidateRange(data, index, count);
    fixed(byte* bytePtr=data) return GetByteCount(bytePtr+index, count, simulateFlush);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/GetMaxBytes/node()"/>
  public override int GetMaxBytes(int unencodedByteCount)
  {
    int wholeChunks = unencodedByteCount/4, leftOver = unencodedByteCount&3;
    return wholeChunks*3 + (leftOver == 3 ? 2 : leftOver == 2 ? 1 : 0) + bufferChars;
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/Reset/node()"/>
  public override void Reset()
  {
    bufferChars = 0;
  }

  uint charBuffer;
  int bufferChars;

  static unsafe int CountValidChars(byte* bytes, int count)
  {
    int validCharCount = 0;
    for(byte* end=bytes+count; bytes != end; bytes++)
    {
      if(GetBase64Value(*bytes) != -1) validCharCount++;
    }
    return validCharCount;
  }

  static int GetBase64Value(byte c)
  {
    if(c >= 'A' && c <= 'Z') return c - 'A';
    else if(c >= 'a' && c <= 'z') return c - ('a'-26);
    else if(c >= '0' && c <= '9') return c - ('0'-52);
    else if(c == '+') return 62;
    else if(c == '/') return 63;
    else return -1;
  }
}
#endregion

#region Base64Encoder
/// <summary>Implements a <see cref="BinaryEncoder"/> that transforms a stream of bytes into a base64-encoded representation.
/// The encoder can optionally insert line breaks into the output to wrap lines at an arbitrary length.
/// </summary>
public sealed class Base64Encoder : BinaryEncoder
{
  /// <summary>Initializes a new <see cref="Base64Encoder"/> that does not perform line wrapping.</summary>
  public Base64Encoder() { }

  /// <summary>Initializes a new <see cref="Base64Encoder"/> that wraps lines at the given number of characters. If
  /// <paramref name="charactersPerLine"/> is zero, line wrapping will be disabled.
  /// </summary>
  public Base64Encoder(int charactersPerLine)
  {
    if(charactersPerLine < 0) throw new ArgumentOutOfRangeException();
    this.wrapLinesAt = charactersPerLine;
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/EncodePtr/node()"/>
  [CLSCompliant(false)]
  public unsafe override int Encode(byte* source, int sourceCount, byte* destination, int destinationCapacity, bool flush)
  {
    if(destinationCapacity < GetByteCount(sourceCount, flush)) throw new ArgumentOutOfRangeException();

    // if there's something in the buffer already, try to pad it out to 3 bytes and encode that
    byte* originalDestPtr = destination;
    int read;
    if(byteBuffer.Count != 0)
    {
      read = Math.Min(3-byteBuffer.Count, sourceCount);
      byteBuffer.AddRange(source, read);
      source      += read;
      sourceCount -= read;
      if(byteBuffer.Count == 3)
      {
        fixed(byte* bytePtr=byteBuffer.Buffer) Encode(bytePtr+byteBuffer.Offset, byteBuffer.Count, ref destination);
        byteBuffer.Clear();
      }
    }

    // then encode the bulk of the array
    read = Encode(source, sourceCount, ref destination);
    source      += read;
    sourceCount -= read;

    // put the remaining data into the buffer
    byteBuffer.AddRange(source, sourceCount);

    if(flush) // if we're flushing the last bytes
    {
      if(byteBuffer.Count != 0) // if we have one or two bytes remaining...
      {
        if(destinationCapacity < 4) throw Exceptions.InsufficientBufferSpace();
        if(byteBuffer.Count == 1)
        {
          int value = byteBuffer[0]; // 00000011
          AddChar(ref destination, base64[value >> 2]);
          AddChar(ref destination, base64[(value & 3) << 4]);
          AddChar(ref destination, (byte)'=');
        }
        else
        {
          int value = (byteBuffer[0] << 8) | byteBuffer[1]; // 00000011 11110000
          AddChar(ref destination, base64[value >> 10]);
          AddChar(ref destination, base64[(value >> 4) & 0x3F]);
          AddChar(ref destination, base64[(value & 0xF) << 2]);
        }
        AddChar(ref destination, (byte)'=');
        byteBuffer.Clear();
      }

      if(wrapLinesAt != 0 && linePosition != 0) // if we're supposed to wrap lines and we've started one, add a final newline
      {
        destination[0] = (byte)'\r';
        destination[1] = (byte)'\n';
        destination += 2;
        linePosition = 0;
      }
    }

    return (int)(destination - originalDestPtr);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/Encode/node()"/>
  public unsafe override int Encode(byte[] source, int sourceIndex, int sourceCount, byte[] destination, int destinationIndex,
                                    bool flush)
  {
    Utility.ValidateRange(source, sourceIndex, sourceCount);
    if(destination == null) throw new ArgumentNullException();
    if((uint)destinationIndex > (uint)destination.Length) throw new ArgumentOutOfRangeException();
    fixed(byte* srcPtr=source, destPtr=destination)
    {
      return Encode(srcPtr+sourceIndex, sourceCount, destPtr+destinationIndex, destination.Length-destinationIndex, flush);
    }
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/GetByteCountPtr/node()"/>
  [CLSCompliant(false)]
  public unsafe override int GetByteCount(byte* data, int count, bool simulateFlush)
  {
    return GetByteCount(count, simulateFlush);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/GetByteCount/node()"/>
  public unsafe override int GetByteCount(byte[] data, int index, int count, bool simulateFlush)
  {
    Utility.ValidateRange(data, index, count);
    return GetByteCount(count, simulateFlush);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/GetMaxBytes/node()"/>
  public override int GetMaxBytes(int unencodedByteCount)
  {
    return GetByteCount(unencodedByteCount, true);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/Reset/node()"/>
  public override void Reset()
  {
    byteBuffer.Clear();
    linePosition = 0;
  }

  unsafe void AddChar(ref byte* bytes, byte c)
  {
    *bytes++ = c;

    if(wrapLinesAt != 0 && ++linePosition == wrapLinesAt)
    {
      bytes[0] = (byte)'\r';
      bytes[1] = (byte)'\n';
      bytes += 2;
      linePosition = 0;
    }
  }

  unsafe int Encode(byte* srcBytes, int count, ref byte* destBytes)
  {
    byte* destPtr = destBytes, originalBytePtr = srcBytes;
    for(; count-3 >= 0; srcBytes += 3, count -= 3) // encode 3-byte chunks until we run out of chunks
    {
      // combine the 3 bytes into a single, 24-bit value, and then break the 24-bit value into four 6-bit chunks
      int value = (srcBytes[0] << 16) | (srcBytes[1] << 8) | srcBytes[2]; // 00000011 11110000 00111111
      if(wrapLinesAt == 0)
      {
        destPtr[0] = base64[value >> 18];
        destPtr[1] = base64[(value >> 12) & 0x3F];
        destPtr[2] = base64[(value >> 6) & 0x3F];
        destPtr[3] = base64[value & 0x3F];
        destPtr += 4;
      }
      else
      {
        AddChar(ref destPtr, base64[value >> 18]);
        AddChar(ref destPtr, base64[(value >> 12) & 0x3F]);
        AddChar(ref destPtr, base64[(value >> 6) & 0x3F]);
        AddChar(ref destPtr, base64[value & 0x3F]);
      }
    }

    destBytes = destPtr;
    return (int)(srcBytes - originalBytePtr);
  }

  int GetByteCount(int count, bool simulateFlush)
  {
    if((uint)count > (uint)1610612733) throw new ArgumentOutOfRangeException(); // the max chars we can decode into 2^31-1 bytes
    if(byteBuffer != null) count += byteBuffer.Count;
    int byteCount = (count + (simulateFlush ? 2 : 0)) / 3 * 4;
    if(wrapLinesAt != 0) byteCount += (byteCount + linePosition + (simulateFlush ? wrapLinesAt-1 : 0)) / wrapLinesAt * 2;
    return byteCount;
  }

  readonly ByteBuffer byteBuffer = new ByteBuffer(4);
  int wrapLinesAt, linePosition;

  static readonly byte[] base64 =
    SimpleEightBitEncoding.Instance.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/");
}
#endregion

#region Base64Encoding
/// <summary>Implements a <see cref="BinaryEncoding"/> that performs base64 encoding and decoding.</summary>
public sealed class Base64Encoding : EncoderDecoderBinaryEncoding
{
  /// <summary>Initializes a new <see cref="Base64Encoding"/> that does not perform line wrapping.</summary>
  public Base64Encoding() : base(new Base64Encoder(), new Base64Decoder()) { }

  /// <summary>Initializes a new <see cref="Base64Encoding"/> that wraps lines at the given number of characters. If
  /// <paramref name="charactersPerLine"/> is zero, line wrapping is disabled.
  /// </summary>
  public Base64Encoding(int charactersPerLine) : base(new Base64Encoder(charactersPerLine), new Base64Decoder()) { }
}
#endregion

#region Base64TextEncoding
/// <summary>Implements an <see cref="System.Text.Encoding"/> that will "encode" (actually decode) base64 text into its original
/// binary data, and "decode" (actually encode) binary data into a base64 representation. The terms are reversed because the
/// purpose of an <see cref="System.Text.Encoder"/> is to encode text into binary, but in this case, the textual representation
/// is the encoded one. This class may be used with a <see cref="System.IO.StreamReader"/> or
/// <see cref="System.IO.StreamWriter"/>, for instance, to provide efficient base64 encoding or decoding, respectively.
/// </summary>
public sealed class Base64TextEncoding : EightBitEncoding
{
  /// <summary>Initializes a new <see cref="Base64TextEncoding"/> that does not perform line wrapping.</summary>
  public Base64TextEncoding() : base(new Base64Decoder(), new Base64Encoder()) { }

  /// <summary>Gets the name of the encoding. This implementation returns "base64".</summary>
  public override string EncodingName
  {
    get { return "base64"; }
  }
}
#endregion

} // namespace AdamMil.Utilities.Encodings
