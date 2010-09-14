using System;

namespace AdamMil.Utilities.Encodings
{

#region BinaryEncoder
/// <summary>Provides a base class for encoders that transform one stream of bytes into another. Data can be encoded in chunks,
/// allowing efficient usage with large data streams.
/// </summary>
public abstract class BinaryEncoder
{
	/// <summary>Gets whether the encoder is capable of encoding in place. If true, it is valid for a user to pass the exact same
	/// buffer for both the source and destination. The default implementation returns false.
	/// </summary>
	public virtual bool CanEncodeInPlace
	{
		get { return false; }
	}

	/// <summary>Encodes data from the source buffer and writes it to the destination buffer.</summary>
	/// <param name="source">A pointer to the beginning of the source data.</param>
	/// <param name="sourceCount">The number of bytes to encode.</param>
	/// <param name="destination">A pointer to where the encoded bytes should be written.</param>
	/// <param name="destinationCount">The amount of space available in the destination buffer.</param>
	/// <param name="flush">If false, the encoder may not write all encoded bytes to the destination buffer, if it needs to see
	/// bytes from future calls before it can do so. If true, the encoder will assume that this is the end of the source data, and
	/// flush its internal state, writing all bytes to the destination.
	/// </param>
	/// <returns>Returns the number of bytes written to the destination buffer.</returns>
	/// <remarks>To encode data in chunks, pass false for <paramref name="flush"/> for all chunks except the last, where
	/// <paramref name="flush"/> should be true. The default implementation copies the data into arrays and calls
	/// <see cref="Encode(byte[],int,int,byte[],int,bool)"/>.
	/// </remarks>
	public unsafe virtual int Encode(byte* source, int sourceCount, byte* destination, int destinationCount, bool flush)
	{
		if(sourceCount < 0 || destinationCount < 0) throw new ArgumentOutOfRangeException();
		byte[] sourceArray = new byte[sourceCount], destinationArray = new byte[destinationCount];
		fixed(byte* srcPtr=sourceArray) Unsafe.Copy(source, srcPtr, sourceCount);
		return Encode(sourceArray, 0, sourceCount, destinationArray, 0, flush);
	}

	/// <summary>Encodes data from the source buffer and writes it to the destination buffer.</summary>
	/// <param name="source">An array that contains the source data.</param>
	/// <param name="sourceIndex">The index of the source data within the array.</param>
	/// <param name="sourceCount">The number of bytes to encode.</param>
	/// <param name="destination">An array where the encoded bytes should be written.</param>
	/// <param name="destinationIndex">The location in the destination array where the encoded bytes should be written.</param>
	/// <param name="flush">If false, the encoder may not write all encoded bytes to the destination buffer, if it needs to see
	/// bytes from future calls before it can do so. If true, the encoder will assume that this is the end of the source data, and
	/// flush its internal state, writing all bytes to the destination.
	/// </param>
	/// <returns>Returns the number of bytes written to the destination buffer.</returns>
	/// <remarks>To encode data in chunks, pass false for <paramref name="flush"/> for all chunks except the last, where
	/// <paramref name="flush"/> should be true.
	/// </remarks>
	public abstract int Encode(byte[] source, int sourceIndex, int sourceCount, byte[] destination, int destinationIndex,
	                           bool flush);

	/// <summary>Returns the number of bytes that would be needed to encode the given source data. This method does not change the
	/// state of the encoder.
	/// </summary>
	/// <param name="data">A pointer to the source data that would be encoded.</param>
	/// <param name="count">The number of bytes that would be encoded.</param>
	/// <param name="simulateFlush">Whether the encoder would be flushed.</param>
	/// <returns>Returns the number of bytes needed to encode the given chunk of data given the current encoder state.</returns>
	/// <remarks>The default implementation copies the data into arrays and calls <see cref="GetByteCount(byte[],int,int,bool)"/>.</remarks>
	public unsafe virtual int GetByteCount(byte* data, int count, bool simulateFlush)
	{
		if(count < 0) throw new ArgumentOutOfRangeException();
		byte[] array = new byte[count];
		fixed(byte* ptr=array) Unsafe.Copy(data, ptr, count);
		return GetByteCount(array, 0, count, simulateFlush);
	}

	/// <summary>Returns the number of bytes that would be needed to encode the given source data. This method does not change the
	/// state of the encoder.
	/// </summary>
	/// <param name="data">An array containing the source data that would be encoded.</param>
	/// <param name="sourceIndex">The index of the source data within the array.</param>
	/// <param name="count">The number of bytes that would be encoded.</param>
	/// <param name="simulateFlush">Whether the encoder would be flushed.</param>
	/// <returns>Returns the number of bytes needed to encode the given chunk of data given the current encoder state.</returns>
	public abstract int GetByteCount(byte[] data, int index, int count, bool simulateFlush);

	/// <summary>Gets the maximum number of bytes that would be needed to fully encode source data of the given size.</summary>
	public abstract int GetMaxBytes(int unencodedByteCount);

	/// <summary>Resets the internal state of the encoder, so that new source data can be encoded. It is not necessary to call this
	/// method if the encoder was flushed on the most recent call to <see cref="Encode"/>.
	/// </summary>
	public abstract void Reset();
}
#endregion

#region BinaryEncoding
/// <summary>Provides a base class for bidirectional encodings that can transform streams from one representation into another,
/// and back again.
/// </summary>
public abstract class BinaryEncoding
{
	/// <summary>Gets whether the encoder is capable of decoding in place. If true, it is valid for a user to pass the exact same
	/// buffer for both the source and destination. The default implementation returns false.
	/// </summary>
	public virtual bool CanDecodeInPlace
	{
		get { return false; }
	}

	/// <summary>Gets whether the encoder is capable of encoding in place. If true, it is valid for a user to pass the exact same
	/// buffer for both the source and destination. The default implementation returns false.
	/// </summary>
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

	/// <summary>Decodes the given data, which is assumed to have been previously encoded with this encoding.</summary>
	/// <param name="encodedBytes">A pointer to the encoded data.</param>
	/// <param name="encodedByteCount">The number of encoded bytes to decode.</param>
	/// <param name="decodedBytes">A pointer to the destination buffer where the decoded bytes will be written.</param>
	/// <param name="decodedByteCount">The number of bytes available in the destination buffer.</param>
	/// <returns>Returns the number of bytes actually written to the destination buffer.</returns>
	/// <remarks>The default implementation copies the data into an array and calls <see cref="Decode(byte[],int,int,byte[],int)"/>.</remarks>
	public unsafe virtual int Decode(byte* encodedBytes, int encodedByteCount, byte* decodedBytes, int decodedByteCount)
	{
		if(encodedByteCount < 0 || decodedByteCount < 0) throw new ArgumentOutOfRangeException();
		byte[] encodedByteArray = new byte[encodedByteCount], decodedByteArray = new byte[decodedByteCount];
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

	/// <summary>Encodes the given data.</summary>
	/// <param name="encodedBytes">A pointer to the source data.</param>
	/// <param name="encodedByteCount">The number of bytes to encode.</param>
	/// <param name="decodedBytes">A pointer to the destination buffer where the encoded bytes will be written.</param>
	/// <param name="decodedByteCount">The number of bytes available in the destination buffer.</param>
	/// <returns>Returns the number of bytes actually written to the destination buffer.</returns>
	/// <remarks>The default implementation copies the data into an array and calls <see cref="Encode(byte[],int,int,byte[],int)"/>.</remarks>
	public unsafe virtual int Encode(byte* decodedBytes, int decodedByteCount, byte* encodedBytes, int encodedByteCount)
	{
		if(encodedByteCount < 0 || decodedByteCount < 0) throw new ArgumentOutOfRangeException();
		byte[] decodedByteArray = new byte[decodedByteCount], encodedByteArray = new byte[encodedByteCount];
		fixed(byte* dbPtr=decodedByteArray) Unsafe.Copy(decodedBytes, dbPtr, decodedByteCount);
		return Encode(decodedByteArray, 0, decodedByteCount, encodedByteArray, 0);
	}

	/// <summary>Returns the number of bytes that the given encoded data would decode into.</summary>
	public int GetDecodedByteCount(byte[] encodedBytes)
	{
		if(encodedBytes == null) throw new ArgumentNullException();
		return GetDecodedByteCount(encodedBytes, 0, encodedBytes.Length);
	}

	/// <summary>Returns the number of bytes that the given encoded data would decode into.</summary>
	/// <remarks>The default implementation copies the data into an array and calls
	/// <see cref="GetDecodedByteCount(byte[],int,int)"/>.
	/// </remarks>
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

	/// <summary>Returns the number of bytes that the given data would encode into.</summary>
	/// <remarks>The default implementation copies the data into an array and calls
	/// <see cref="GetEncodedByteCount(byte[],int,int)"/>.
	/// </remarks>
	public unsafe virtual int GetEncodedByteCount(byte* decodedBytes, int count)
	{
		if(count < 0) throw new ArgumentOutOfRangeException();
		byte[] array = new byte[count];
		fixed(byte* ptr=array) Unsafe.Copy(decodedBytes, ptr, count);
		return GetEncodedByteCount(array, 0, count);
	}

	/// <summary>Returns a <see cref="BinaryEncoder"/> based on this encoding. Note that the default implementation creates and
	/// returns a <see cref="DefaultBinaryEncoder"/>, which may not be capable of properly decoding data in chunks. If the encoding
	/// requires that state be maintained while decoding chunks of data, you must override this method and return a more suitable
	/// encoder.
	/// </summary>
	public virtual BinaryEncoder GetDecoder()
	{
		return new DefaultBinaryEncoder(this, false);
	}

	/// <summary>Returns a <see cref="BinaryEncoder"/> based on this encoding. Note that the default implementation creates and
	/// returns a <see cref="DefaultBinaryEncoder"/>, which may not be capable of properly encoding data in chunks. If the encoding
	/// requires that state be maintained while encoding chunks of data, you must override this method and return a more suitable
	/// encoder.
	/// </summary>
	public virtual BinaryEncoder GetEncoder()
	{
		return new DefaultBinaryEncoder(this, true);
	}

	/// <summary>Decodes the given data, which is assumed to have been previously encoded with this encoding.</summary>
	public abstract int Decode(byte[] encodedBytes, int ebIndex, int encodedByteCount, byte[] decodedBytes, int dbIndex);

	/// <summary>Encodes the given data.</summary>
	public abstract int Encode(byte[] decodedBytes, int dbIndex, int decodedByteCount, byte[] encodedBytes, int ebIndex);

	/// <summary>Returns the number of bytes that the given encoded data would decode into.</summary>
	public abstract int GetDecodedByteCount(byte[] encodedBytes, int index, int count);

	/// <summary>Returns the number of bytes that the given data would encode into.</summary>
	public abstract int GetEncodedByteCount(byte[] decodedBytes, int index, int count);

	/// <summary>Returns the maximum number of bytes that the given number of encoded bytes could decode into.</summary>
	public abstract int GetMaxDecodedBytes(int encodedByteCount);

	/// <summary>Returns the maximum number of bytes that the given amount of data could encode into.</summary>
	public abstract int GetMaxEncodedBytes(int decodedByteCount);

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

	public override bool CanEncodeInPlace
	{
		get { return encode ? encoding.CanEncodeInPlace : encoding.CanDecodeInPlace; }
	}

	public override unsafe int Encode(byte* source, int sourceCount, byte* destination, int destinationCount, bool flush)
	{
		return encode ? encoding.Encode(source, sourceCount, destination, destinationCount) :
		                encoding.Decode(source, sourceCount, destination, destinationCount);
	}

	public override int Encode(byte[] source, int sourceIndex, int sourceCount, byte[] destination, int destinationIndex, bool flush)
	{
		return encode ? encoding.Encode(source, sourceIndex, sourceCount, destination, destinationIndex) :
		                encoding.Decode(source, sourceIndex, sourceCount, destination, destinationIndex);
	}

	public override unsafe int GetByteCount(byte* data, int count, bool simulateFlush)
	{
		return encode ? encoding.GetEncodedByteCount(data, count) : encoding.GetDecodedByteCount(data, count);
	}

	public override int GetByteCount(byte[] data, int index, int count, bool simulateFlush)
	{
		return encode ? encoding.GetEncodedByteCount(data, index, count) : encoding.GetDecodedByteCount(data, index, count);
	}

	public override int GetMaxBytes(int unencodedByteCount)
	{
		return encode ? encoding.GetMaxEncodedBytes(unencodedByteCount) : encoding.GetMaxDecodedBytes(unencodedByteCount);
	}

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
	public EncoderDecoderBinaryEncoding(BinaryEncoder encoder, BinaryEncoder decoder)
	{
		if(encoder == null || decoder == null) throw new ArgumentNullException();
		this.encoder = encoder;
		this.decoder = decoder;
	}

	public override bool CanDecodeInPlace
	{
		get { return decoder.CanEncodeInPlace; }
	}

	public override bool CanEncodeInPlace
	{
		get { return encoder.CanEncodeInPlace; }
	}

	public override unsafe int Decode(byte* encodedBytes, int encodedByteCount, byte* decodedBytes, int decodedByteCount)
	{
		decoder.Reset();
		return decoder.Encode(encodedBytes, encodedByteCount, decodedBytes, decodedByteCount, true);
	}

	public override int Decode(byte[] encodedBytes, int ebIndex, int encodedByteCount, byte[] decodedBytes, int dbIndex)
	{
		decoder.Reset();
		return decoder.Encode(encodedBytes, ebIndex, encodedByteCount, decodedBytes, dbIndex, true);
	}

	public override unsafe int Encode(byte* decodedBytes, int decodedByteCount, byte* encodedBytes, int encodedByteCount)
	{
		encoder.Reset();
		return encoder.Encode(decodedBytes, decodedByteCount, encodedBytes, encodedByteCount, true);
	}

	public override int Encode(byte[] decodedBytes, int dbIndex, int decodedByteCount, byte[] encodedBytes, int ebIndex)
	{
		encoder.Reset();
		return encoder.Encode(decodedBytes, dbIndex, decodedByteCount, encodedBytes, ebIndex, true);
	}

	public override unsafe int GetDecodedByteCount(byte* encodedBytes, int count)
	{
		decoder.Reset();
		return decoder.GetByteCount(encodedBytes, count, true);
	}

	public override int GetDecodedByteCount(byte[] encodedBytes, int index, int count)
	{
		decoder.Reset();
		return decoder.GetByteCount(encodedBytes, index, count, true);
	}

	public override unsafe int GetEncodedByteCount(byte* decodedBytes, int count)
	{
		encoder.Reset();
		return encoder.GetByteCount(decodedBytes, count, true);
	}

	public override int GetEncodedByteCount(byte[] decodedBytes, int index, int count)
	{
		encoder.Reset();
		return encoder.GetByteCount(decodedBytes, index, count, true);
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

	public override int GetMaxDecodedBytes(int encodedByteCount)
	{
		return decoder.GetMaxBytes(encodedByteCount);
	}

	public override int GetMaxEncodedBytes(int decodedByteCount)
	{
		return encoder.GetMaxBytes(decodedByteCount);
	}

	readonly BinaryEncoder encoder, decoder;
}
#endregion

} // namespace AdamMil.Utilities.Encodings
