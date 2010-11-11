using System;

namespace AdamMil.Utilities.Encodings
{

#region Base64Decoder
/// <summary>Implements a <see cref="BinaryEncoder"/> that transforms a stream of bytes containing base64-encoded data back into
/// the original binary data.
/// </summary>
public sealed class Base64Decoder : BinaryEncoder
{
  public override bool CanEncodeInPlace
  {
    get { return true; }
  }

  public unsafe override int Encode(byte* srcBytes, int srcCount, byte* destBytes, int destCount, bool flush)
  {
    if(srcCount < 0 || destCount < 0) throw new ArgumentOutOfRangeException();

    // go through the characters, collecting valid ones into a buffer, and processing them whenever we get 4 valid characters
    byte* originalBytePtr = destBytes;
    for(byte* end=srcBytes+srcCount; srcBytes != end; srcBytes++)
    {
      int value = GetBase64Value(*srcBytes);
      if(value != -1)
      {
        charBuffer = (charBuffer << 6) | (byte)value;
        if(++bufferChars == 4)
        {
          if(destCount < 3) throw Exceptions.InsufficientBufferSpace();
          destBytes[0] = (byte)(charBuffer >> 16);
          destBytes[1] = (byte)(charBuffer >> 8);
          destBytes[2] = (byte)charBuffer;
          destBytes  += 3;
          destCount  -= 3;
          bufferChars = 0;
        }
      }
    }

    // at this point there are at most 3 bytes in the buffer. if we're flushing the state, convert those remaining bytes too
    if(flush)
    {
      if(bufferChars == 2) // there is 1 data byte in 2 buffer bytes (012345 67xxxx -> xxxx0123 4567xxxx)
      {
        if(destCount == 0) throw Exceptions.InsufficientBufferSpace();
        *destBytes++ = (byte)(charBuffer >> 4);
      }
      else if(bufferChars == 3) // there are 2 data bytes in 3 buffer bytes (012345 670123 4567xx -> 01 23456701 234567xx)
      {
        if(destCount < 2) throw Exceptions.InsufficientBufferSpace();
        destBytes[0] = (byte)(charBuffer >> 10);
        destBytes[1] = (byte)(charBuffer >> 2);
        destBytes += 2;
      }

      bufferChars = 0;
    }

    return (int)(destBytes - originalBytePtr);
  }

  public unsafe override int Encode(byte[] srcBytes, int srcIndex, int srcCount, byte[] destBytes, int destIndex, bool flush)
  {
    Utility.ValidateRange(srcBytes, srcIndex, srcCount);
    if(destBytes == null) throw new ArgumentNullException();
    if(destIndex < 0 || destIndex > destBytes.Length) throw new ArgumentOutOfRangeException();

    fixed(byte* srcPtr=srcBytes)
    fixed(byte* destPtr=destBytes)
    {
      return Encode(srcPtr+srcIndex, srcCount, destPtr+destIndex, destBytes.Length-destIndex, flush);
    }
  }

  public unsafe override int GetByteCount(byte* bytes, int count, bool simulateFlush)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();

    int validCharCount = bufferChars + CountValidChars(bytes, count), byteCount = validCharCount / 4 * 3;
    if(simulateFlush)
    {
      validCharCount &= 3; // get the number of remainder characters
      if(validCharCount == 3) byteCount += 2;
      else if(validCharCount == 2) byteCount++;
    }
    return byteCount;
  }

  public unsafe override int GetByteCount(byte[] bytes, int index, int count, bool simulateFlush)
  {
    Utility.ValidateRange(bytes, index, count);
    fixed(byte* bytePtr=bytes) return GetByteCount(bytePtr+index, count, simulateFlush);
  }

  public override int GetMaxBytes(int unencodedByteCount)
  {
    int wholeChunks = unencodedByteCount/4, leftOver = unencodedByteCount&3;
    return wholeChunks*3 + (leftOver == 3 ? 2 : leftOver == 2 ? 1 : 0);
  }

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

  public unsafe override int Encode(byte* srcBytes, int srcCount, byte* destBytes, int destCount, bool flush)
  {
    if(destCount < GetByteCount(srcCount, flush)) throw new ArgumentOutOfRangeException();

    // if there's something in the buffer already, try to pad it out to 3 bytes and encode that
    byte* originalDestPtr = destBytes;
    int read;
    if(byteBuffer.Count != 0)
    {
      read = Math.Min(3-byteBuffer.Count, srcCount);
      byteBuffer.AddRange(srcBytes, read);
      srcBytes += read;
      srcCount -= read;
      if(byteBuffer.Count == 3)
      {
        fixed(byte* bytePtr=byteBuffer.Buffer) Encode(bytePtr+byteBuffer.Offset, byteBuffer.Count, ref destBytes);
        byteBuffer.Clear();
      }
    }

    // then encode the bulk of the array
    read = Encode(srcBytes, srcCount, ref destBytes);
    srcBytes += read;
    srcCount -= read;

    // put the remaining data into the buffer
    byteBuffer.AddRange(srcBytes, srcCount);

    if(flush) // if we're flushing the last bytes
    {
      if(byteBuffer.Count != 0) // if we have one or two bytes remaining...
      {
        if(destCount < 4) throw Exceptions.InsufficientBufferSpace();
        if(byteBuffer.Count == 1)
        {
          int value = byteBuffer[0]; // 00000011
          AddChar(ref destBytes, base64[value >> 2]);
          AddChar(ref destBytes, base64[(value & 3) << 4]);
          AddChar(ref destBytes, (byte)'=');
        }
        else
        {
          int value = (byteBuffer[0] << 8) | byteBuffer[1]; // 00000011 11110000
          AddChar(ref destBytes, base64[value >> 10]);
          AddChar(ref destBytes, base64[(value >> 4) & 0x3F]);
          AddChar(ref destBytes, base64[(value & 0xF) << 2]);
        }
        AddChar(ref destBytes, (byte)'=');
        byteBuffer.Clear();
      }

      if(wrapLinesAt != 0 && linePosition != 0) // if we're supposed to wrap lines and we've started one, add a final newline
      {
        destBytes[0] = (byte)'\r';
        destBytes[1] = (byte)'\n';
        destBytes += 2;
        linePosition = 0;
      }
    }

    return (int)(destBytes - originalDestPtr);
  }

  public unsafe override int Encode(byte[] srcBytes, int srcIndex, int srcCount, byte[] destBytes, int destIndex, bool flush)
  {
    Utility.ValidateRange(srcBytes, srcIndex, srcCount);
    if(destBytes == null) throw new ArgumentNullException();
    if(destIndex < 0 || destIndex > destBytes.Length) throw new ArgumentOutOfRangeException();
    fixed(byte* srcPtr=srcBytes)
    fixed(byte* destPtr=destBytes)
    {
      return Encode(srcPtr+srcIndex, srcCount, destPtr+destIndex, destBytes.Length-destIndex, flush);
    }
  }

  public unsafe override int GetByteCount(byte* bytes, int count, bool simulateFlush)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    return GetByteCount(count, simulateFlush);
  }

  public unsafe override int GetByteCount(byte[] bytes, int index, int count, bool simulateFlush)
  {
    Utility.ValidateRange(bytes, index, count);
    return GetByteCount(count, simulateFlush);
  }

  public override int GetMaxBytes(int unencodedByteCount)
  {
    return (unencodedByteCount+2)/3*4;
  }

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

  /// <summary>Gets the default instance of the <see cref="Base64Encoding"/>, which does not perform line wrapping.</summary>
  public static readonly Base64Encoding Instance = new Base64Encoding();
}
#endregion

#region Base64TextEncoding
/// <summary>Implements an <see cref="Encoding"/> that will "encode" (actually decode) base64 text into its original binary data,
/// and "decode" (actually encode) binary data into a base64 representation. The terms are reversed because the purpose of an
/// <see cref="Encoder"/> is to encode text into binary, but in this case, the textual representation is the encoded one. This
/// class may be used with a <see cref="StreamReader"/> or <see cref="StreamWriter"/>, for instance, to provide efficient base64
/// encoding or decoding, respectively.
/// <para>Note that if you need to encode or decode data in chunks, you should use the <see cref="Decoder"/> or
/// <see cref="Encoder"/> returned from <see cref="Encoding.GetDecoder()"/> or <see cref="Encoding.GetEncoder"/>, because
/// <see cref="Encoding"/> is not capable of handling data in chunks.
/// </para>
/// </summary>
public sealed class Base64TextEncoding : EightBitEncoding
{
  public Base64TextEncoding() : base(new Base64Decoder(), new Base64Encoder()) { }

  public override string EncodingName
  {
    get { return "base64"; }
  }

  public static readonly Base64TextEncoding Instance = new Base64TextEncoding();
}
#endregion

} // namespace AdamMil.Utilities.Encodings
