using System;
using System.Text;

namespace AdamMil.Utilities.Encodings
{

#region EncoderDecoderEncoding
/// <summary>Provides a base for <see cref="System.Text.Encoding"/> classes constructed from the combination of an
/// <see cref="Encoder"/> and <see cref="Decoder"/>.
/// </summary>
public abstract class EncoderDecoderEncoding : Encoding
{
  /// <summary>Initializes an <see cref="EncoderDecoderEncoding"/> encoding from the combination of an <see cref="Encoder"/>
  /// and <see cref="Decoder"/>.
  /// </summary>
  protected EncoderDecoderEncoding(Encoder encoder, Decoder decoder)
  {
    if(encoder == null || decoder == null) throw new ArgumentNullException();
    this.encoder = encoder;
    this.decoder = decoder;
  }

  /// <inheritdoc/>
  [CLSCompliant(false)]
  public unsafe override int GetByteCount(char* chars, int count)
  {
    encoder.Reset();
    return encoder.GetByteCount(chars, count, true);
  }

  /// <inheritdoc/>
  public override int GetByteCount(char[] chars, int index, int count)
  {
    encoder.Reset();
    return encoder.GetByteCount(chars, index, count, true);
  }

  /// <inheritdoc/>
  [CLSCompliant(false)]
  public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
  {
    encoder.Reset();
    return encoder.GetBytes(chars, charCount, bytes, byteCount, true);
  }

  /// <inheritdoc/>
  public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
  {
    encoder.Reset();
    return encoder.GetBytes(chars, charIndex, charCount, bytes, byteIndex, true);
  }

  /// <inheritdoc/>
  [CLSCompliant(false)]
  public unsafe override int GetCharCount(byte* bytes, int count)
  {
    decoder.Reset();
    return decoder.GetCharCount(bytes, count, true);
  }

  /// <inheritdoc/>
  public override int GetCharCount(byte[] bytes, int index, int count)
  {
    decoder.Reset();
    return decoder.GetCharCount(bytes, index, count, true);
  }

  /// <inheritdoc/>
  [CLSCompliant(false)]
  public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
  {
    decoder.Reset();
    return decoder.GetChars(bytes, byteCount, chars, charCount, true);
  }

  /// <inheritdoc/>
  public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
  {
    decoder.Reset();
    return decoder.GetChars(bytes, byteIndex, byteCount, chars, charIndex, true);
  }

  /// <inheritdoc/>
  public override Decoder GetDecoder()
  {
    return decoder;
  }

  /// <inheritdoc/>
  public override Encoder GetEncoder()
  {
    return encoder;
  }

  readonly Encoder encoder;
  readonly Decoder decoder;
}
#endregion

} // namespace AdamMil.Utilities.Encodings
