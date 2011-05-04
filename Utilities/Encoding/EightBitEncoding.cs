using System;
using System.Text;

namespace AdamMil.Utilities.Encodings
{

#region EightBitDecoder
/// <summary>Implements a <see cref="Decoder"/> that encodes binary data using a <see cref="BinaryEncoder"/> and then
/// simply expands each byte from 8 to 16 bits to form the characters.
/// </summary>
public sealed class EightBitDecoder : Decoder
{
  /// <summary>Initializes a new <see cref="EightBitDecoder"/> with the given <see cref="BinaryEncoder"/>. If the encoder is null,
  /// characters will simply be expanded without being encoded first.
  /// </summary>
  public EightBitDecoder(BinaryEncoder decoder)
  {
    this.decoder = decoder;
  }

  /// <inheritdoc/>
  public override int GetCharCount(byte[] bytes, int index, int count)
  {
    if(decoder != null) decoder.Reset();
    return GetCharCount(bytes, index, count, true);
  }

  /// <inheritdoc/>
  [CLSCompliant(false)]
  public unsafe override int GetCharCount(byte* bytes, int count, bool simulateFlush)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    return decoder == null ? count : decoder.GetByteCount(bytes, count, simulateFlush);
  }

  /// <inheritdoc/>
  public unsafe override int GetCharCount(byte[] bytes, int index, int count, bool simulateFlush)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    return decoder == null ? count : decoder.GetByteCount(bytes, index, count, simulateFlush);
  }

  /// <inheritdoc/>
  public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
  {
    if(decoder != null) decoder.Reset();
    return GetChars(bytes, byteIndex, byteCount, chars, charIndex, true);
  }

  /// <inheritdoc/>
  [CLSCompliant(false)]
  public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount, bool flush)
  {
    if(byteCount < 0) throw new ArgumentOutOfRangeException();

    if(decoder == null)
    {
      if(charCount < byteCount) throw Exceptions.InsufficientBufferSpace(byteCount);
      Decode(bytes, byteCount, chars);
    }
    else
    {
      if(decoder.CanEncodeInPlace)
      {
        byteCount = decoder.Encode(bytes, byteCount, bytes, byteCount, flush);
        Decode(bytes, byteCount, chars);
      }
      else
      {
        int bytesNeeded = decoder.GetByteCount(bytes, byteCount, flush);
        byte[] buffer = new byte[bytesNeeded];
        fixed(byte* bufferPtr=buffer)
        {
          byteCount = decoder.Encode(bytes, byteCount, bufferPtr, bytesNeeded, flush);
          if(charCount < byteCount) throw Exceptions.InsufficientBufferSpace(byteCount);
          Decode(bufferPtr, byteCount, chars);
        }
      }
    }

    return byteCount;
  }

  /// <inheritdoc/>
  public unsafe override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush)
  {
    Utility.ValidateRange(bytes, byteIndex, byteCount);
    Utility.ValidateRange(chars, charIndex, decoder == null ? byteCount : 0); // we can't validate the count if there's a decoder
    fixed(byte* bytePtr=bytes)
    fixed(char* charPtr=chars)
    {
      return GetChars(bytePtr+byteIndex, byteCount, charPtr+charIndex, chars.Length-charIndex, flush);
    }
  }

  readonly BinaryEncoder decoder;

  unsafe static void Decode(byte* bytes, int byteCount, char* chars)
  {
    for(byte* end=bytes+byteCount; bytes != end; chars++, bytes++) *chars = (char)*bytes;
  }
}
#endregion

#region EightBitEncoder
/// <summary>Implements a <see cref="Encoder"/> that simply truncates characters to 8 bits, and then encodes them using a
/// <see cref="BinaryEncoder"/>.
/// </summary>
public sealed class EightBitEncoder : Encoder
{
  /// <summary>Initializes a new <see cref="EightBitEncoder"/> with the given <see cref="BinaryEncoder"/>. If the encoder is null,
  /// characters will simply be truncated without being encoded.
  /// </summary>
  public EightBitEncoder(BinaryEncoder encoder)
  {
    this.encoder = encoder;
  }

  /// <inheritdoc/>
  [CLSCompliant(false)]
  public unsafe override int GetByteCount(char* chars, int count, bool simulateFlush)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    return encoder == null ? count : encoder.GetByteCount(Encode(chars, count), 0, count, simulateFlush);
  }

  /// <inheritdoc/>
  public unsafe override int GetByteCount(char[] chars, int index, int count, bool simulateFlush)
  {
    Utility.ValidateRange(chars, index, count);
    if(encoder == null) return count;
    fixed(char* charPtr=chars) return encoder.GetByteCount(Encode(charPtr+index, count), 0, count, simulateFlush);
  }

  /// <inheritdoc/>
  [CLSCompliant(false)]
  public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, bool flush)
  {
    if(charCount < 0) throw new ArgumentOutOfRangeException();

    if(byteCount < charCount) // if there's not enough space in the byte buffer to do the initial eight bit encoding...
    {
      if(encoder == null) throw Exceptions.InsufficientBufferSpace(charCount);

      // use a temporary buffer, and then copy the bytes from that into the destination buffer
      byte[] temp = new byte[charCount];
      fixed(byte* tempPtr=temp)
      {
        int bytesNeeded = GetBytes(chars, charCount, tempPtr, charCount, flush);
        if(byteCount < bytesNeeded) throw Exceptions.InsufficientBufferSpace(bytesNeeded);
        Unsafe.Copy(tempPtr, bytes, bytesNeeded);
        return bytesNeeded;
      }
    }

    Encode(chars, charCount, bytes);

    if(encoder == null)
    {
      return charCount;
    }
    else
    {
      encoder.Reset();
      if(encoder.CanEncodeInPlace)
      {
        return encoder.Encode(bytes, charCount, bytes, byteCount, flush);
      }
      else
      {
        int bytesNeeded = encoder.GetByteCount(bytes, charCount, flush);
        byte[] buffer = new byte[bytesNeeded];
        fixed(byte* bufferPtr=buffer)
        {
          bytesNeeded = encoder.Encode(bytes, charCount, bufferPtr, bytesNeeded, flush);
          if(byteCount < bytesNeeded) throw Exceptions.InsufficientBufferSpace(bytesNeeded);
          Unsafe.Copy(bufferPtr, bytes, bytesNeeded);
        }
        return bytesNeeded;
      }
    }
  }

  /// <inheritdoc/>
  public unsafe override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
  {
    Utility.ValidateRange(chars, charIndex, charCount);
    Utility.ValidateRange(bytes, byteIndex, encoder == null ? charCount : 0); // we can't validate the count if there's an encoder
    fixed(char* charPtr=chars)
    fixed(byte* bytePtr=bytes)
    {
      return GetBytes(charPtr+charIndex, charCount, bytePtr+byteIndex, bytes.Length-byteIndex, flush);
    }
  }

  readonly BinaryEncoder encoder;

  unsafe static byte[] Encode(char* chars, int charCount)
  {
    byte[] bytes = new byte[charCount];
    fixed(byte* bytePtr=bytes) Encode(chars, charCount, bytePtr);
    return bytes;
  }

  unsafe static void Encode(char* chars, int charCount, byte* bytes)
  {
    for(char* end=chars+charCount; chars != end; bytes++, chars++)
    {
      char c = *chars;
      *bytes = c > 0xFF ? (byte)'?' : (byte)c;
    }
  }
}
#endregion

#region EightBitEncoding
/// <summary>Implements an <see cref="Encoding"/> that will encode or decode data using a pair of <see cref="BinaryEncoder"/>
/// objects. The text characters are truncated to 8 bits before being passed to the encoder. Note that if you need to encode or
/// decode data in chunks, you should use the <see cref="Decoder"/> or <see cref="Encoder"/> returned from
/// <see cref="Encoding.GetDecoder()"/> or <see cref="Encoding.GetEncoder"/>, because <see cref="Encoding"/> is not capable of
/// handling data in chunks.
/// </summary>
public class EightBitEncoding : EncoderDecoderEncoding
{
  /// <summary>Initializes a new <see cref="EightBitEncoding"/> that simply truncates characters to 8 bits.</summary>
  public EightBitEncoding() : this(null, null) { }

  /// <summary>Initializes a new <see cref="EightBitEncoding"/> that truncates characters to 8 bits and encodes them with the
  /// given encoding.
  /// </summary>
  public EightBitEncoding(BinaryEncoding encoding) : this(encoding.GetEncoder(), encoding.GetDecoder()) { }

  /// <summary>Initializes a new <see cref="EightBitEncoding"/> that truncates characters to 8 bits and encodes and decodes them
  /// with the given encoder and decoder.
  /// </summary>
  public EightBitEncoding(BinaryEncoder encoder, BinaryEncoder decoder)
    : base(new EightBitEncoder(encoder), new EightBitDecoder(decoder))
  {
    this.encoder = encoder;
    this.decoder = decoder;
  }

  /// <inheritdoc/>
  public override int GetMaxByteCount(int charCount)
  {
    if(charCount < 0) throw new ArgumentOutOfRangeException();
    return encoder == null ? charCount : encoder.GetMaxBytes(charCount);
  }

  /// <inheritdoc/>
  public override int GetMaxCharCount(int byteCount)
  {
    if(byteCount < 0) throw new ArgumentOutOfRangeException();
    return decoder == null ? byteCount : decoder.GetMaxBytes(byteCount);
  }

  readonly BinaryEncoder encoder, decoder;
}
#endregion

#region SimpleEightBitEncoding
/// <summary>Implements an <see cref="Encoding"/> that simply truncates characters to bytes and expands bytes to characters.</summary>
public sealed class SimpleEightBitEncoding : EightBitEncoding
{
  /// <summary>Gets a default instance of <see cref="SimpleEightBitEncoding"/>.</summary>
  public static readonly SimpleEightBitEncoding Instance = new SimpleEightBitEncoding();
}
#endregion

} // namespace AdamMil.Utilities.Encodings
