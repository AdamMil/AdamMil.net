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
  protected EncoderDecoderEncoding(Encoder encoder, Decoder decoder)
  {
    if(encoder == null || decoder == null) throw new ArgumentNullException();
    this.encoder = encoder;
    this.decoder = decoder;
  }

  public unsafe override int GetByteCount(char* chars, int count)
  {
    encoder.Reset();
    return encoder.GetByteCount(chars, count, true);
  }

  public override int GetByteCount(char[] chars, int index, int count)
  {
    encoder.Reset();
    return encoder.GetByteCount(chars, index, count, true);
  }

  public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
  {
    encoder.Reset();
    return encoder.GetBytes(chars, charCount, bytes, byteCount, true);
  }

  public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
  {
    encoder.Reset();
    return encoder.GetBytes(chars, charIndex, charCount, bytes, byteIndex, true);
  }

  public unsafe override int GetCharCount(byte* bytes, int count)
  {
    decoder.Reset();
    return decoder.GetCharCount(bytes, count, true);
  }

  public override int GetCharCount(byte[] bytes, int index, int count)
  {
    decoder.Reset();
    return decoder.GetCharCount(bytes, index, count, true);
  }

  public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
  {
    decoder.Reset();
    return decoder.GetChars(bytes, byteCount, chars, charCount, true);
  }

  public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
  {
    decoder.Reset();
    return decoder.GetChars(bytes, byteIndex, byteCount, chars, charIndex, true);
  }

  public override Decoder GetDecoder()
  {
    return decoder;
  }

  public override Encoder GetEncoder()
  {
    return encoder;
  }

  readonly Encoder encoder;
  readonly Decoder decoder;
}
#endregion

} // namespace AdamMil.Utilities.Encodings
