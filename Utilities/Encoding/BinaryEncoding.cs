using System;

namespace AdamMil.Utilities.Encodings
{

#region BinaryEncoder
/// <summary>Provides a base class for encoders that transform one stream of bytes into another. Data can be encoded in chunks,
/// allowing efficient usage with large data streams.
/// </summary>
public abstract class BinaryEncoder
{
  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/CanEncodeInPlace/*"/>
  public virtual bool CanEncodeInPlace
  {
    get { return false; }
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/EncodePtr/*"/>
  /// <remarks>
  /// The default implementation copies the data into arrays and calls <see cref="Encode(byte[],int,int,byte[],int,bool)"/>.
  /// </remarks>
  [CLSCompliant(false)]
  public unsafe virtual int Encode(byte* source, int sourceCount, byte* destination, int destinationCapacity, bool flush)
  {
    if(sourceCount < 0 || destinationCapacity < 0) throw new ArgumentOutOfRangeException();
    byte[] sourceArray = new byte[sourceCount], destinationArray = new byte[destinationCapacity];
    fixed(byte* srcPtr=sourceArray) Unsafe.Copy(source, srcPtr, sourceCount);
    return Encode(sourceArray, 0, sourceCount, destinationArray, 0, flush);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/Encode/*"/>
  public abstract int Encode(byte[] source, int sourceIndex, int sourceCount, byte[] destination, int destinationIndex,
                             bool flush);

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/GetByteCountPtr/*"/>
  /// <remarks>
  /// The default implementation copies the data into arrays and calls <see cref="GetByteCount(byte[],int,int,bool)"/>.
  /// </remarks>
  [CLSCompliant(false)]
  public unsafe virtual int GetByteCount(byte* data, int count, bool simulateFlush)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    byte[] array = new byte[count];
    fixed(byte* ptr=array) Unsafe.Copy(data, ptr, count);
    return GetByteCount(array, 0, count, simulateFlush);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/GetByteCount/*"/>
  public abstract int GetByteCount(byte[] data, int index, int count, bool simulateFlush);

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/GetMaxBytes/*"/>
  public abstract int GetMaxBytes(int unencodedByteCount);

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/Reset/*"/>
  public abstract void Reset();
}
#endregion

#region UnsafeBinaryEncoder
/// <summary>Provides a base class for binary encoders that are primarily implemented using unsafe operations on byte pointers.</summary>
/// <remarks><see cref="BinaryEncoder"/> implements the unsafe encoding methods in terms of the safe encoding methods, requiring derived
/// classes to implement the safe encoding methods. <see cref="UnsafeBinaryEncoder"/> is derived from <see cref="BinaryEncoder"/> and
/// reverses this, implementing the safe encoding methods in terms of the unsafe ones, and requires derived classes to override the
/// unsafe methods.
/// </remarks>
public abstract class UnsafeBinaryEncoder : BinaryEncoder
{
  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/Encode/*"/>
  /// <remarks>The default implementation fixes the arrays and calls <see cref="Encode(byte*,int,byte*,int,bool)"/>.</remarks>
  public unsafe override int Encode(byte[] source, int sourceIndex, int sourceCount, byte[] destination, int destinationIndex, bool flush)
  {
    Utility.ValidateRange(source, sourceIndex, sourceCount);
    Utility.ValidateRange(destination, destinationIndex, 0);
    fixed(byte* srcBase=source, destBase=destination)
    {
      byte dummy;
      return Encode(srcBase == null ? &dummy : srcBase+sourceIndex, sourceCount, // pointers are when arrays are empty
                    destBase == null ? &dummy : destBase+destinationIndex, destination.Length-destinationIndex, flush);
    }
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/EncodePtr/*"/>
  /// <remarks>Derived classes must override this method.</remarks>
  [CLSCompliant(false)]
  public override unsafe int Encode(byte* source, int sourceCount, byte* destination, int destinationCapacity, bool flush)
  {
    throw new NotImplementedException();
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/GetByteCount/*"/>
  /// <remarks>The default implementation fixes the array and calls <see cref="GetByteCount(byte*,int,bool)"/>.</remarks>
  public unsafe override int GetByteCount(byte[] data, int index, int count, bool simulateFlush)
  {
    Utility.ValidateRange(data, index, count);
    fixed(byte* dataPtr=data)
    {
      byte dataByte; // dataPtr is null when source is zero bytes, so provide /some/ pointer
      return GetByteCount(dataPtr == null ? &dataByte : dataPtr+index, count, simulateFlush);
    }
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/GetByteCountPtr/*"/>
  /// <remarks>Derived classes must override this method.</remarks>
  [CLSCompliant(false)]
  public override unsafe int GetByteCount(byte* data, int count, bool simulateFlush)
  {
    throw new NotImplementedException();
  }
}
#endregion

#region BinaryEncoding
/// <summary>Provides a base class for bidirectional encodings that can transform streams from one representation into another,
/// and back again.
/// </summary>
public abstract class BinaryEncoding
{
  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/CanDecodeInPlace/*"/>
  /// <remarks>The default implementation returns false.</remarks>
  public virtual bool CanDecodeInPlace
  {
    get { return false; }
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/CanEncodeInPlace/*"/>
  /// <remarks>The default implementation returns false.</remarks>
  public virtual bool CanEncodeInPlace
  {
    get { return false; }
  }

  /// <summary>Decodes the given data, which is assumed to have been previously encoded with this encoding.</summary>
  public byte[] Decode(byte[] encodedBytes)
  {
    if(encodedBytes == null) throw new ArgumentNullException();
    return Decode(encodedBytes, 0, encodedBytes.Length);
  }

  /// <summary>Decodes the given data, which is assumed to have been previously encoded with this encoding.</summary>
  public byte[] Decode(byte[] encodedBytes, int index, int count)
  {
    Utility.ValidateRange(encodedBytes, index, count);

    int maxDecodedBytes = GetMaxDecodedBytes(count);
    // if we don't know how many bytes it could expand into, or it's large and much greater than the encoded data length,
    // get the exact count
    if(maxDecodedBytes == -1 || maxDecodedBytes > 65500 && maxDecodedBytes >= count*3)
    {
      maxDecodedBytes = GetDecodedByteCount(encodedBytes, index, count);
    }

    byte[] decodedBytes = new byte[maxDecodedBytes];
    return MakeReturnArray(decodedBytes, Decode(encodedBytes, index, count, decodedBytes, 0));
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/DecodePtr/*"/>
  /// <remarks>
  /// The default implementation copies the data into an array and calls <see cref="Decode(byte[],int,int,byte[],int)"/>.
  /// </remarks>
  [CLSCompliant(false)]
  public unsafe virtual int Decode(byte* encodedBytes, int encodedByteCount, byte* decodedBytes, int decodedByteCapacity)
  {
    if(encodedByteCount < 0 || decodedByteCapacity < 0) throw new ArgumentOutOfRangeException();
    byte[] encodedByteArray = new byte[encodedByteCount], decodedByteArray = new byte[decodedByteCapacity];
    fixed(byte* ebPtr=encodedByteArray) Unsafe.Copy(encodedBytes, ebPtr, encodedByteCount);
    return Decode(encodedByteArray, 0, encodedByteCount, decodedByteArray, 0);
  }

  /// <summary>Encodes the given data.</summary>
  public byte[] Encode(byte[] decodedBytes)
  {
    if(decodedBytes == null) throw new ArgumentNullException();
    return Encode(decodedBytes, 0, decodedBytes.Length);
  }

  /// <summary>Encodes the given data.</summary>
  public byte[] Encode(byte[] decodedBytes, int index, int count)
  {
    Utility.ValidateRange(decodedBytes, index, count);

    int maxEncodedBytes = GetMaxEncodedBytes(count);
    // if we don't know how many bytes it could expand into, or it's large and much greater than the encoded data length,
    // get the exact count
    if(maxEncodedBytes == -1 || maxEncodedBytes > 65500 && maxEncodedBytes >= count*3)
    {
      maxEncodedBytes = GetEncodedByteCount(decodedBytes, index, count);
    }

    byte[] encodedBytes = new byte[maxEncodedBytes];
    return MakeReturnArray(encodedBytes, Encode(decodedBytes, index, count, encodedBytes, 0));
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/EncodePtr/*"/>
  /// <remarks>
  /// The default implementation copies the data into an array and calls <see cref="Encode(byte[],int,int,byte[],int)"/>.
  /// </remarks>
  [CLSCompliant(false)]
  public unsafe virtual int Encode(byte* dataBytes, int dataByteCount, byte* encodedBytes, int encodedByteCapacity)
  {
    if(dataByteCount < 0 || encodedByteCapacity < 0) throw new ArgumentOutOfRangeException();
    byte[] decodedByteArray = new byte[dataByteCount], encodedByteArray = new byte[encodedByteCapacity];
    fixed(byte* dbPtr=decodedByteArray) Unsafe.Copy(dataBytes, dbPtr, dataByteCount);
    return Encode(decodedByteArray, 0, dataByteCount, encodedByteArray, 0);
  }

  /// <summary>Returns the number of bytes that the given encoded data would decode into.</summary>
  public int GetDecodedByteCount(byte[] encodedBytes)
  {
    if(encodedBytes == null) throw new ArgumentNullException();
    return GetDecodedByteCount(encodedBytes, 0, encodedBytes.Length);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetDecodedByteCountPtr/*"/>
  [CLSCompliant(false)]
  public unsafe virtual int GetDecodedByteCount(byte* encodedBytes, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    byte[] array = new byte[count];
    fixed(byte* ptr=array) Unsafe.Copy(encodedBytes, ptr, count);
    return GetDecodedByteCount(array, 0, count);
  }

  /// <summary>Returns the number of bytes that the given data would encode into.</summary>
  public int GetEncodedByteCount(byte[] decodedBytes)
  {
    if(decodedBytes == null) throw new ArgumentNullException();
    return GetEncodedByteCount(decodedBytes, 0, decodedBytes.Length);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetEncodedByteCountPtr/*"/>
  [CLSCompliant(false)]
  public unsafe virtual int GetEncodedByteCount(byte* decodedBytes, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    byte[] array = new byte[count];
    fixed(byte* ptr=array) Unsafe.Copy(decodedBytes, ptr, count);
    return GetEncodedByteCount(array, 0, count);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetDecoder/*"/>
  /// <remarks>Note that the default implementation creates and returns a <see cref="DefaultBinaryEncoder"/>, which may not be
  /// capable of properly decoding data in chunks. If the encoding requires that state be maintained while decoding chunks of
  /// data, you must override this method and return a more suitable encoder.
  /// </remarks>
  public virtual BinaryEncoder GetDecoder()
  {
    return new DefaultBinaryEncoder(this, false);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetEncoder/*"/>
  /// <remarks>Note that the default implementation creates and returns a <see cref="DefaultBinaryEncoder"/>, which may not be
  /// capable of properly encoding data in chunks. If the encoding requires that state be maintained while encoding chunks of
  /// data, you must override this method and return a more suitable encoder.
  /// </remarks>
  public virtual BinaryEncoder GetEncoder()
  {
    return new DefaultBinaryEncoder(this, true);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/Decode/*"/>
  public abstract int Decode(byte[] encodedBytes, int encodedByteIndex, int encodedByteCount,
                             byte[] decodedBytes, int decodedByteIndex);

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/Encode/*"/>
  public abstract int Encode(byte[] data, int dataIndex, int dataByteCount, byte[] encodedBytes, int encodedByteIndex);

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetDecodedByteCount/*"/>
  public abstract int GetDecodedByteCount(byte[] encodedBytes, int index, int count);

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetEncodedByteCount/*"/>
  public abstract int GetEncodedByteCount(byte[] decodedBytes, int index, int count);

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetMaxDecodedBytes/*"/>
  public abstract int GetMaxDecodedBytes(int encodedByteCount);

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetMaxEncodedBytes/*"/>
  public abstract int GetMaxEncodedBytes(int dataByteCount);

  static byte[] MakeReturnArray(byte[] array, int desiredLength)
  {
    if(array.Length != desiredLength)
    {
      byte[] newArray = new byte[desiredLength];
      Array.Copy(array, newArray, desiredLength);
      array = newArray;
    }
    return array;
  }
}
#endregion

#region UnsafeBinaryEncoding
/// <summary>Provides a base class for binary encodings that are primarily implemented using unsafe operations on byte pointers.</summary>
/// <remarks><see cref="BinaryEncoding"/> implements the unsafe encoding and decoding methods in terms of the safe methods, requiring
/// derived classes to implement the safe methods. <see cref="UnsafeBinaryEncoding"/> is derived from <see cref="BinaryEncoding"/> and
/// reverses this, implementing the safe encoding and decoding methods in terms of the unsafe ones, and requires derived classes to
/// override the unsafe methods.
/// </remarks>
public abstract class UnsafeBinaryEncoding : BinaryEncoding
{
  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/Decode/*"/>
  /// <remarks>The default implementation fixes the arrays and calls <see cref="Decode(byte*,int,byte*,int)"/>.</remarks>
  public unsafe override int Decode(byte[] encodedBytes, int encodedByteIndex, int encodedByteCount,
                                    byte[] decodedBytes, int decodedByteIndex)
  {
    Utility.ValidateRange(encodedBytes, encodedByteIndex, encodedByteCount);
    Utility.ValidateRange(decodedBytes, decodedByteIndex, 0);
    fixed(byte* encBase=encodedBytes, decBase=decodedBytes)
    {
      byte dummy;
      return Decode(encBase == null ? &dummy : encBase+encodedByteIndex, encodedByteCount,
                    decBase == null ? &dummy : decBase+decodedByteIndex, decodedBytes.Length - decodedByteIndex);
    }
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/DecodePtr/*"/>
  /// <remarks>Derived classes must override this method.</remarks>
  [CLSCompliant(false)]
  public override unsafe int Decode(byte* encodedBytes, int encodedByteCount, byte* decodedBytes, int decodedByteCapacity)
  {
    throw new NotImplementedException();
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/Encode/*"/>
  /// <remarks>The default implementation fixes the arrays and calls <see cref="Encode(byte*,int,byte*,int)"/>.</remarks>
  public unsafe override int Encode(byte[] data, int dataIndex, int dataByteCount, byte[] encodedBytes, int encodedByteIndex)
  {
    Utility.ValidateRange(data, dataIndex, dataByteCount);
    Utility.ValidateRange(encodedBytes, encodedByteIndex, 0);
    fixed(byte* decBase=data, encBase=encodedBytes)
    {
      byte dummy;
      return Encode(decBase == null ? &dummy : decBase+dataIndex, dataByteCount,
                    encBase == null ? &dummy : encBase+encodedByteIndex, encodedBytes.Length - encodedByteIndex);
    }
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/EncodePtr/*"/>
  /// <remarks>Derived classes must override this method.</remarks>
  [CLSCompliant(false)]
  public override unsafe int Encode(byte* dataBytes, int dataByteCount, byte* encodedBytes, int encodedByteCapacity)
  {
    throw new NotImplementedException();
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetDecodedByteCount/*"/>
  /// <remarks>The default implementation fixes the array and calls <see cref="GetDecodedByteCount(byte*,int)"/>.</remarks>
  public unsafe override int GetDecodedByteCount(byte[] encodedBytes, int index, int count)
  {
    Utility.ValidateRange(encodedBytes, index, count);
    fixed(byte* basePtr=encodedBytes)
    {
      byte dummy;
      return GetDecodedByteCount(basePtr == null ? &dummy : basePtr+index, count);
    }
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetDecodedByteCountPtr/*"/>
  /// <remarks>Derived classes must override this method.</remarks>
  [CLSCompliant(false)]
  public override unsafe int GetDecodedByteCount(byte* encodedBytes, int count)
  {
    throw new NotImplementedException();
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetEncodedByteCount/*"/>
  /// <remarks>The default implementation fixes the array and calls <see cref="GetEncodedByteCount(byte*,int)"/>.</remarks>
  public unsafe override int GetEncodedByteCount(byte[] decodedBytes, int index, int count)
  {
    Utility.ValidateRange(decodedBytes, index, count);
    fixed(byte* basePtr=decodedBytes)
    {
      byte dummy;
      return GetEncodedByteCount(basePtr == null ? &dummy : basePtr+index, count);
    }
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetEncodedByteCountPtr/*"/>
  /// <remarks>Derived classes must override this method.</remarks>
  [CLSCompliant(false)]
  public override unsafe int GetEncodedByteCount(byte* decodedBytes, int count)
  {
    throw new NotImplementedException();
  }
}
#endregion

#region DefaultBinaryEncoder
/// <summary>Implements a <see cref="BinaryEncoder"/> based on a <see cref="BinaryEncoding"/>. Since a
/// <see cref="BinaryEncoding"/> object does not provide any methods for encoding data in chunks, this encoder cannot either, and
/// will malfunction if chunks of data are encoded when used with an encoding that requires encoder state to be maintained between
/// chunks.
/// </summary>
public sealed class DefaultBinaryEncoder : BinaryEncoder
{
  /// <summary>Initializes a new <see cref="DefaultBinaryEncoder" /> based on the given <see cref="BinaryEncoding"/>. If
  /// <paramref name="encode"/> is true, the encoder will use the <see cref="BinaryEncoding"/> to encode data. Otherwise, it will
  /// use it to decode data.
  /// </summary>
  public DefaultBinaryEncoder(BinaryEncoding encoding, bool encode)
  {
    if(encoding == null) throw new ArgumentNullException();
    this.encoding = encoding;
    this.encode   = encode;
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/CanEncodeInPlace/*"/>
  public override bool CanEncodeInPlace
  {
    get { return encode ? encoding.CanEncodeInPlace : encoding.CanDecodeInPlace; }
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/EncodePtr/*"/>
  [CLSCompliant(false)]
  public override unsafe int Encode(byte* source, int sourceCount, byte* destination, int destinationCapacity, bool flush)
  {
    return encode ? encoding.Encode(source, sourceCount, destination, destinationCapacity) :
                    encoding.Decode(source, sourceCount, destination, destinationCapacity);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/Encode/*"/>
  public override int Encode(byte[] source, int sourceIndex, int sourceCount, byte[] destination, int destinationIndex, bool flush)
  {
    return encode ? encoding.Encode(source, sourceIndex, sourceCount, destination, destinationIndex) :
                    encoding.Decode(source, sourceIndex, sourceCount, destination, destinationIndex);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/GetByteCountPtr/*"/>
  [CLSCompliant(false)]
  public override unsafe int GetByteCount(byte* data, int count, bool simulateFlush)
  {
    return encode ? encoding.GetEncodedByteCount(data, count) : encoding.GetDecodedByteCount(data, count);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/GetByteCount/*"/>
  public override int GetByteCount(byte[] data, int index, int count, bool simulateFlush)
  {
    return encode ? encoding.GetEncodedByteCount(data, index, count) : encoding.GetDecodedByteCount(data, index, count);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/GetMaxBytes/*"/>
  public override int GetMaxBytes(int unencodedByteCount)
  {
    return encode ? encoding.GetMaxEncodedBytes(unencodedByteCount) : encoding.GetMaxDecodedBytes(unencodedByteCount);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoder/Reset/*"/>
  public override void Reset()
  {
  }

  readonly BinaryEncoding encoding;
  readonly bool encode;
}
#endregion

#region EncoderDecoderBinaryEncoding
/// <summary>Implements a <see cref="BinaryEncoding"/> that encodes and decodes data based on a pair of
/// <see cref="BinaryEncoder"/> objects.
/// </summary>
public class EncoderDecoderBinaryEncoding : BinaryEncoding
{
  /// <summary>Initializes a new <see cref="EncoderDecoderBinaryEncoding"/> based on two <see cref="BinaryEncoder"/> objects.</summary>
  public EncoderDecoderBinaryEncoding(BinaryEncoder encoder, BinaryEncoder decoder)
  {
    if(encoder == null || decoder == null) throw new ArgumentNullException();
    this.encoder = encoder;
    this.decoder = decoder;
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/CanDecodeInPlace/*"/>
  public override bool CanDecodeInPlace
  {
    get { return decoder.CanEncodeInPlace; }
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/CanEncodeInPlace/*"/>
  public override bool CanEncodeInPlace
  {
    get { return encoder.CanEncodeInPlace; }
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/DecodePtr/*"/>
  [CLSCompliant(false)]
  public override unsafe int Decode(byte* encodedBytes, int encodedByteCount, byte* decodedBytes, int decodedByteCapacity)
  {
    decoder.Reset();
    return decoder.Encode(encodedBytes, encodedByteCount, decodedBytes, decodedByteCapacity, true);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/Decode/*"/>
  public override int Decode(byte[] encodedBytes, int encodedByteIndex, int encodedByteCount,
                             byte[] decodedBytes, int decodedByteIndex)
  {
    decoder.Reset();
    return decoder.Encode(encodedBytes, encodedByteIndex, encodedByteCount, decodedBytes, decodedByteIndex, true);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/EncodePtr/*"/>
  [CLSCompliant(false)]
  public override unsafe int Encode(byte* dataBytes, int dataByteCount, byte* encodedBytes, int encodedByteCapacity)
  {
    encoder.Reset();
    return encoder.Encode(dataBytes, dataByteCount, encodedBytes, encodedByteCapacity, true);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/Encode/*"/>
  public override int Encode(byte[] data, int dataIndex, int dataByteCount, byte[] encodedBytes, int encodedByteIndex)
  {
    encoder.Reset();
    return encoder.Encode(data, dataIndex, dataByteCount, encodedBytes, encodedByteIndex, true);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetDecodedByteCountPtr/*"/>
  [CLSCompliant(false)]
  public override unsafe int GetDecodedByteCount(byte* encodedBytes, int count)
  {
    decoder.Reset();
    return decoder.GetByteCount(encodedBytes, count, true);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetDecodedByteCount/*"/>
  public override int GetDecodedByteCount(byte[] encodedBytes, int index, int count)
  {
    decoder.Reset();
    return decoder.GetByteCount(encodedBytes, index, count, true);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetEncodedByteCountPtr/*"/>
  [CLSCompliant(false)]
  public override unsafe int GetEncodedByteCount(byte* data, int count)
  {
    encoder.Reset();
    return encoder.GetByteCount(data, count, true);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetEncodedByteCount/*"/>
  public override int GetEncodedByteCount(byte[] data, int index, int count)
  {
    encoder.Reset();
    return encoder.GetByteCount(data, index, count, true);
  }

  /// <summary>Returns the decoder passed to the constructor.</summary>
  public override BinaryEncoder GetDecoder()
  {
    return decoder;
  }

  /// <summary>Returns the encoder passed to the constructor.</summary>
  public override BinaryEncoder GetEncoder()
  {
    return encoder;
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetMaxDecodedBytes/*"/>
  public override int GetMaxDecodedBytes(int encodedByteCount)
  {
    return decoder.GetMaxBytes(encodedByteCount);
  }

  /// <include file="documentation.xml" path="//Utilities/BinaryEncoding/GetMaxEncodedBytes/*"/>
  public override int GetMaxEncodedBytes(int dataByteCount)
  {
    return encoder.GetMaxBytes(dataByteCount);
  }

  readonly BinaryEncoder encoder, decoder;
}
#endregion

} // namespace AdamMil.Utilities.Encodings
