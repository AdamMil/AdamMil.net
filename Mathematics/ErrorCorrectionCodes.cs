/*
AdamMil.Mathematics is a library that provides some useful mathematics classes
for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2016 Adam Milazzo

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
using AdamMil.Mathematics.Fields;
using AdamMil.Utilities;

// TODO: consider allowing field powers other than GF(2^8) with values packed tightly into bytes

namespace AdamMil.Mathematics.ErrorCorrection
{
  /// <summary>Represents a Reed-Solomon error-correction code.</summary>
  /// <remarks>A Reed-Solomon code with k error-correction symbols can correct up to k errors per data block if the error locations are
  /// known, up to k/2 errors per data block if the error locations are unknown, or some number of errors between them if some of the error
  /// locations are known. This implementation currently only supports 8-bit symbols, suitable for encoding bytes of data.
  /// </remarks>
  public sealed class ReedSolomon
  {
    /// <summary>Initializes a new <see cref="ReedSolomon"/> code.</summary>
    /// <param name="eccLength">The number of error-correction symbols to use per block of data. (Each data block can be up to 255 bytes.)</param>
    public ReedSolomon(int eccLength) : this(eccLength, new GF2pField(8)) { }

    /// <summary>Initializes a new <see cref="ReedSolomon"/> code.</summary>
    /// <param name="eccLength">The number of error-correction symbols to use per block of data. (Each data block can be up to 255 bytes.)</param>
    /// <param name="field">The Galois field to use for the Reed-Solomon code. The field must currently be a GF(2^8) field (i.e. have a
    /// <see cref="GF2pField.Power"/> of 8).
    /// </param>
    public ReedSolomon(int eccLength, GF2pField field)
    {
      if((uint)eccLength > 254) throw new ArgumentOutOfRangeException("eccLength", "must be from 0-254");
      if(field == null) throw new ArgumentNullException();
      if(field.Power != 8) throw new ArgumentException("field.Power must equal 8.");
      EccLength = eccLength;
      Field     = field;
      Prime     = CreatePrimePolynomial(field, eccLength);
    }

    /// <summary>Gets the number of error-correction symbols per block of data.</summary>
    public int EccLength { get; private set; }

    /// <summary>Gets the Galois field used to compute the error-correction code.</summary>
    public GF2pField Field { get; private set; }

    /// <summary>Gets the primitive polynomial for the code.</summary>
    public GF2pPolynomial Prime { get; private set; }

    /// <include file="documentation.xml" path="/Math/ReedSolomon/Check/*[@name != 'srcIndex' and @name != 'length']"/>
    public bool Check(byte[] source)
    {
      if(source == null) throw new ArgumentNullException();
      return Check(source, 0, source.Length);
    }

    /// <include file="documentation.xml" path="/Math/ReedSolomon/Check/*"/>
    public bool Check(byte[] source, int srcIndex, int length)
    {
      if(length < EccLength) throw new ArgumentOutOfRangeException("length", "must be at least EccLength");
      Utility.ValidateRange(source, srcIndex, length);
      return CalculateSyndromes(ToPolynomial(source, srcIndex, length), EccLength).IsZero;
    }

    /// <include file="documentation.xml" path="/Math/ReedSolomon/DecodeBytes/*[@name != 'srcIndex' and @name != 'length' and @name != 'errorPositions' and @name != 'allErrorsKnown']"/>
    public byte[] Decode(byte[] source)
    {
      return Decode(source, null, false);
    }

    /// <include file="documentation.xml" path="/Math/ReedSolomon/DecodeBytes/*[@name != 'errorPositions' and @name != 'allErrorsKnown']"/>
    public byte[] Decode(byte[] source, int srcIndex, int length)
    {
      return Decode(source, srcIndex, length, null, false);
    }

    /// <include file="documentation.xml" path="/Math/ReedSolomon/DecodeBool/*[@name != 'errorPositions' and @name != 'allErrorsKnown']"/>
    public int Decode(byte[] source, int srcIndex, int length, byte[] destination, int destIndex)
    {
      return Decode(source, srcIndex, length, destination, destIndex, null, false);
    }

    /// <include file="documentation.xml" path="/Math/ReedSolomon/DecodeBytes/*[@name != 'srcIndex' and @name != 'length' and @name != 'errorPositions']"/>
    /// <param name="errorPositions">An array containing the indexes within <paramref name="source"/> containing errors, or null if the
    /// locations of errors are not known.
    /// </param>
    public byte[] Decode(byte[] source, int[] errorPositions, bool allErrorsKnown = false)
    {
      if(source == null) throw new ArgumentNullException("source");
      return Decode(source, 0, source.Length, errorPositions, allErrorsKnown);
    }

    /// <include file="documentation.xml" path="/Math/ReedSolomon/DecodeBytes/*"/>
    public byte[] Decode(byte[] source, int srcIndex, int length, int[] errorPositions, bool allErrorsKnown = false)
    {
      Utility.ValidateRange(source, srcIndex, length);
      if(length < EccLength) throw new ArgumentOutOfRangeException("length", "must be at least EccLength");
      byte[] destination = new byte[length - EccLength];
      return Decode(source, srcIndex, length, destination, 0, errorPositions, allErrorsKnown) >= 0 ? destination : null;
    }

    /// <include file="documentation.xml" path="/Math/ReedSolomon/DecodeBool/*"/>
    public int Decode(byte[] source, int srcIndex, int length, byte[] destination, int destIndex,
                      int[] errorPositions, bool allErrorsKnown = false)
    {
      if(length < EccLength) throw new ArgumentOutOfRangeException("length", "must be at least EccLength");
      Utility.ValidateRange(source, srcIndex, length);
      Utility.ValidateRange(destination, destIndex, length - EccLength);

      GF2pPolynomial message = ToPolynomial(source, srcIndex, length), syndromes = CalculateSyndromes(message, EccLength);
      if(syndromes.IsZero)
      {
        Array.Copy(source, srcIndex + EccLength, destination, destIndex, length - EccLength);
        return length - EccLength;
      }

      GF2pPolynomial locator = default(GF2pPolynomial), evaluator = default(GF2pPolynomial);
      if(errorPositions != null && errorPositions.Length != 0)
      {
        locator   = CalculateErasureLocator(errorPositions, Field);
        evaluator = CalculateErrorEvaluator(syndromes, locator);
      }
      else if(allErrorsKnown)
      {
        return -1;
      }

      if(!allErrorsKnown)
      {
        BerlekampMassey(syndromes, locator, evaluator, errorPositions == null ? 0 : errorPositions.Length, out locator, out evaluator);
      }

      int[] allErrorPositions = FindErrors(locator, length);
      if(allErrorPositions == null || allErrorPositions.Length > EccLength) return -1;

      // now return the corrected data. copy the source data to the destination
      Array.Copy(source, srcIndex + EccLength, destination, destIndex, length - EccLength);
      int correctionStart = 0; // skip connections to ECC codes, since we don't return them
      while(correctionStart < allErrorPositions.Length && allErrorPositions[correctionStart] < EccLength) correctionStart++;
      if(correctionStart < allErrorPositions.Length) // if there are corrections to the data...
      {
        int[] errorMagnitudes = Forney(evaluator, allErrorPositions, correctionStart); // apply them
        for(int i=0; i<errorMagnitudes.Length; i++)
        {
          destination[destIndex + allErrorPositions[i+correctionStart] - EccLength] ^= (byte)errorMagnitudes[i];
        }
      }

      return length - EccLength;
    }

    /// <summary>Encodes a data block by adding <see cref="EccLength"/> error-correction symbols to it.</summary>
    /// <param name="data">The array containing the data block to encode. This can be at most 255-<see cref="EccLength"/> bytes in length.</param>
    /// <returns>Returns the encoded data block.</returns>
    public byte[] Encode(byte[] data)
    {
      if(data == null) throw new ArgumentNullException();
      return Encode(data, 0, data.Length);
    }

    /// <summary>Encodes a data block by adding <see cref="EccLength"/> error-correction symbols to it.</summary>
    /// <param name="data">The array containing the data block to encode</param>
    /// <param name="index">The index within <paramref name="data"/> at which the data block begins</param>
    /// <param name="length">The length of the data block. This can be at most 255-<see cref="EccLength"/></param>
    /// <returns>Returns the encoded data block.</returns>
    public byte[] Encode(byte[] data, int index, int length)
    {
      if((uint)length > (uint)(255 - EccLength)) throw new ArgumentException("The data length must be from 0 to 255 - EccLength.");
      byte[] destination = new byte[length + EccLength];
      Encode(data, index, length, destination, 0);
      return destination;
    }

    /// <summary>Encodes a data block by adding <see cref="EccLength"/> error-correction symbols to it.</summary>
    /// <param name="source">The array containing the data block to encode</param>
    /// <param name="srcIndex">The index within <paramref name="source"/> at which the data block begins</param>
    /// <param name="length">The length of the data block. This can be at most 255-<see cref="EccLength"/></param>
    /// <param name="destination">The array into which the encoded block will be written</param>
    /// <param name="destIndex">The index at which the encoded block will be written. The array must have at least
    /// <paramref name="length"/>+<see cref="EccLength"/> bytes available at the index.
    /// </param>
    /// <returns>Returns the number of bytes written to the <paramref name="destination"/> array, which will equal
    /// <paramref name="length"/>+<see cref="EccLength"/>.
    /// </returns>
    public int Encode(byte[] source, int srcIndex, int length, byte[] destination, int destIndex)
    {
      Utility.ValidateRange(source, srcIndex, length);
      Utility.ValidateRange(destination, destIndex, length + EccLength);
      if(source.Length > 255 - EccLength) throw new ArgumentException("The source length can be at most 255 - EccLength.");

      if(EccLength != 0)
      {
        int[] coef = new int[length + EccLength];
        for(int i=0; i<length; i++) coef[i+EccLength] = source[i+srcIndex];
        GF2pPolynomial code = new GF2pPolynomial(Field, coef, false) % Prime;
        for(int i=0; i<code.data.Length; i++) destination[i + destIndex] = (byte)code.data[i];
      }
      Array.Copy(source, srcIndex, destination, destIndex + EccLength, length);
      return length + EccLength;
    }

    // Converts a data block into a polynomial
    GF2pPolynomial ToPolynomial(byte[] source, int srcIndex, int length)
    {
      int[] coef = new int[length];
      for(int i=0; i<coef.Length; i++) coef[i] = source[i + srcIndex];
      return new GF2pPolynomial(Field, coef, false);
    }

    static void BerlekampMassey(GF2pPolynomial syndromes, GF2pPolynomial erasureLocator, GF2pPolynomial erasureEvaluator, int erasureCount,
                                out GF2pPolynomial errataLocator, out GF2pPolynomial errataEvaluator)
    {
      GF2pPolynomial locator, prevLocator, evaluator, prevEvaluator, a, b;
      if(erasureCount != 0)
      {
        locator = erasureLocator;
        a = evaluator = erasureEvaluator;
      }
      else
      {
        evaluator = locator = new GF2pPolynomial(syndromes.Field, 1);
        a = new GF2pPolynomial(syndromes.Field, 0);
      }
      b = locator;

      int L = 0;
      for(int i = 0, count = syndromes.Length-erasureCount; i < count; i++)
      {
        int k = i + erasureCount, delta = GF2pPolynomial.MultiplyAt(syndromes, locator, k);
        prevLocator   = locator;
        prevEvaluator = evaluator;
        GF2pPolynomial shiftA = a<<1, shiftB = b<<1;
        locator   += shiftB*delta;
        evaluator += shiftA*delta;
        if(delta == 0 || 2*L > k+erasureCount+1)
        {
          b = shiftB;
          a = shiftA;
        }
        else
        {
          b = prevLocator / delta;
          a = prevEvaluator / delta;
          L = k + 1 - L;
        }
      }

      if(evaluator.Length > locator.Length) evaluator = evaluator.Truncate(locator.Length);
      evaluator = CalculateErrorEvaluator(syndromes, locator);

      errataLocator   = locator;
      errataEvaluator = evaluator;
    }

    static GF2pPolynomial CalculateErasureLocator(int[] positions, GF2pField field)
    {
      if(positions == null) throw new ArgumentNullException();
      GF2pPolynomial locator = new GF2pPolynomial(field, 1);
      foreach(int position in positions) locator *= new GF2pPolynomial(field, new int[] { 1, field.Exp(position) }, false);
      return locator;
    }

    static GF2pPolynomial CalculateErrorEvaluator(GF2pPolynomial syndromes, GF2pPolynomial locator)
    {
      return (syndromes*locator).Truncate(syndromes.Length) << 1;
    }

    static GF2pPolynomial CalculateSyndromes(GF2pPolynomial message, int eccLength)
    {
      int[] syndromes = new int[eccLength]; // add a zero at the front to simply later calculations
      for(int i=0; i<syndromes.Length; i++) syndromes[i] = message.Evaluate(message.Field.Exp(i+1)); // add 1 because we assume fcr == 1
      return new GF2pPolynomial(message.Field, syndromes, false);
    }

    static GF2pPolynomial CreatePrimePolynomial(GF2pField field, int symbolCount)
    {
      GF2pPolynomial poly = new GF2pPolynomial(field, 1);
      for(int i=1; i<=symbolCount; i++) poly *= new GF2pPolynomial(field, new int[] { field.Exp(i), 1 }, false); // add 1 because fcr == 1
      return poly;
    }

    static int[] FindErrors(GF2pPolynomial locator, int dataLength)
    { // TODO: ideally we'd use the more efficient Chien's search here rather than this brute-force approach
      int[] errorPositions = new int[locator.Degree];
      int ei = 0;
      for(int maxValue = (int)(locator.Field.Order-1), di = 0; di < dataLength; di++)
      {
        if(locator.Evaluate(locator.Field.Exp(maxValue-di)) == 0)
        {
          if(ei != errorPositions.Length) errorPositions[ei++] = di;
          else return null;
        }
      }

      return ei == errorPositions.Length ? errorPositions : null;
    }

    static int[] Forney(GF2pPolynomial evaluator, int[] errorPositions, int startIndex)
    {
      int[] powers = new int[errorPositions.Length], magnitudes = new int[powers.Length - startIndex];
      for(int i=0; i<powers.Length; i++) powers[i] = evaluator.Field.Exp(errorPositions[i]);
      for(int i=0; i<magnitudes.Length; i++)
      {
        int invPower = evaluator.Field.Invert(powers[i+startIndex]), divisor = 1;
        for(int ei = 0; ei < powers.Length; ei++)
        {
          if(ei != i+startIndex) divisor = evaluator.Field.Multiply(divisor, evaluator.Field.Multiply(powers[ei], invPower) ^ 1);
        }
        magnitudes[i] = evaluator.Field.Divide(evaluator.Evaluate(invPower), divisor);
      }
      return magnitudes;
    }
  }
}
