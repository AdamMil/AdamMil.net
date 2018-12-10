/*
AdamMil.Mathematics is a library that provides some useful mathematics classes
for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2019 Adam Milazzo

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
using System.Globalization;
using AdamMil.IO;
using AdamMil.Utilities;

namespace AdamMil.Mathematics
{
  #region FP107
  /// <summary>A relatively efficient floating-point type that provides the same range as an IEEE 754 double-precision floating-point type
  /// but with approximately 107 bits of precision instead of 53, which is equivalent to 32 decimal digits instead of 15.
  /// </summary>
  /// <remarks>This data type does not implement a true 107-bit mantissa, but a variable-sized mantissa that in the great majority of cases
  /// is 107 bits or slightly more. In very rare cases, it can provide precision up to 2046 bits, and when the magnitude of the number is
  /// very small (around 1e-300 or less), the effective precision is reduced to less than 107 bits, gradually dropping to the point where
  /// it's equivalent to double precision (53 bits). When the magnitude is very large (around 1e300 or more), the data type continues to
  /// represent at least 107 bits of precision, but certain computations can only be performed with double precision.
  /// <note type="caution">Be careful when specifying literal or constant <see cref="FP107"/> values in source code. Simply assigning
  /// double-precision constants to <see cref="FP107"/> values will produce inaccurate results unless the decimal value can be represented
  /// exactly by the double-precision floating-point value. For instance, <c>FP107 x = 1.1;</c> produces an inaccurate value. Although
  /// <c>x == 1.1</c> will evaluate to true, x is actually approximately 1.1000000000000000888. This is because the double-precision value
  /// is only an approximation of 1.1, and actually equals
  /// 4953959590107546 * 2^-52 = 1.100000000000000088817841970012523233890533447265625. Despite being the best 53-bit approximation of 1.1,
  /// it is not the best 107-bit approximation.
  /// <para>To get the best <see cref="FP107"/> approximation to 1.1, any of the following can be used instead, in order from fastest to
  /// slowest: <c>FP107.FromComponents(1.1, -8.8817841970012528E-17)</c>, <c>x = FP107.Divide(11, 10)</c>, <c>x = (FP107)1.1m</c>,
  /// <c>x = FP107.Parse("1.1", CultureInfo.InvariantCulture)</c>, or <c>x = FP107.FromDecimalApproximation(1.1)</c>. The first is very
  /// fast. The second is reasonably fast, but for certain values may not provide the perfect approximation due to inaccuracies in the
  /// division (but for certain other values can provide even better than 107-bit precision). The last three are quite slow and should not
  /// be executed often, and the method that uses a <see cref="decimal"/> literal (1.1m in this case) is only suitable if the literal
  /// can be represented exactly by a <see cref="decimal"/> value. To discover the values needed by the <see cref="FromComponents"/>
  /// method, you can construct the approximation with any other method and pass the "S" format string to the <see cref="ToString(string)"/>
  /// method. (See the <see cref="ToString(string)"/> documentation for more details.) The <see cref="GetComponents"/> method can also be
  /// used to extract the components.
  /// </para>
  /// <para>
  /// Similar considerations apply when calling <see cref="FP107"/> methods that can take either a double or an <see cref="FP107"/> value.
  /// For instance, <c>FP107.Log(Math.E) != 1</c> because <c>Math.E</c> is not the best 107-bit approximation to Euler's constant.
  /// <c>FP107.E</c> works better in this case: <c>FP107.Log(FP107.E) == 1</c>.
  /// </para>
  /// <para>
  /// If a decimal value can be represented exactly as a double-precision floating-point number (e.g. 1.25), then it is safe to assign it
  /// directly to an <see cref="FP107"/> value or to pass it as a value to any <see cref="FP107"/> method, e.g. <c>FP107 x = 1.25;</c>
  /// or <c>FP107.Log(1.25)</c>, but care should be taken to document in your code that it is only safe for those particular values.
  /// </para>
  /// </note>
  /// </remarks>
  // The basic idea is that the value is represented as the sum of two floating-point values. Since the two values can have different
  // exponents, their mantissas represent different parts of the sum without overlap. For example, considering a hypothetical base-10
  // floating-point format with two digits of precision, 12.34 would normally be stored as 12e0, losing the 0.34. But stored as the sum
  // of two non-overlapping numbers, the value can be represented accurately: 12e0 + 34e-2. The combination effectively has a four-digit
  // mantissa. Usually these portions are adjacent or nearly so, as in the previous example, but they don't need to be. 12.00000034 can be
  // represented as well: 12e0 + 34e-8, for an effective ten-digit mantissa. With two IEEE754 doubles, we get a typical precision of 107
  // bits, 53 from each value plus an extra bit from the smaller value's sign bit.
  //
  // When an FP107 number gets down to near the minimum exponent, however, it can no longer maintain sufficient separation between the two
  // values' exponents (because that would require the smaller value's exponent to be below the minimum), so precision is gradually reduced
  // to the precision of a double. When FP107 numbers get near the extremes of the range, operations to compute the new component values
  // begin to overflow, in which case FP107 will fall back to using only a single component, again reducing the precision to that of a
  // double.
  //
  // The data type has the following invariants:
  // abs(hi) >= abs(lo)
  // if hi has a fractional part, then -0.5 < lo < 0.5
  // if lo is a non-zero integer, then hi is a non-zero integer
  //
  // The arithmetic algorithms are adapted from the following sources:
  // * http://www.mrob.com/pub/math/f161.html
  // * Extended-Precision Floating Point Numbers for GPU Computation (Thall, 2007)
  // with some error corrections and additional operations.
  //
  // The exponential, logarithmic, and trigonometric algorithms are adapted from
  // Algorithms for Quad-double Precision Floating Point Arithmetic (Hida, Li, and Bailey, 2000)
  // with corrected handling of NaNs and infinities, and some special casing for better precision.
  //
  // The algorithm to print floating-point numbers is adapted from
  // Printing Floating Point Numbers Quickly and Accurately (Burger & Dybvig, 1996).
  //
  // The algorithms to read floating-point numbers are adapted from
  // How to Read Floating Point Numbers Accurately (Clinger, 1990)
  // although the main result of the paper, the Bellerophon algorithm, is not used.
  [Serializable]
  public struct FP107 : IComparable, IComparable<FP107>, IConvertible, IEquatable<FP107>, IFormattable
  {
    /// <summary>Initializes a new <see cref="FP107"/> value from a double-precision floating-point value.</summary>
    public FP107(double value)
    {
      hi = value;
      if(double.IsNaN(value)) lo = value;
      else lo = 0;
    }

    /// <summary>Initializes a new <see cref="FP107"/> value from a <see cref="decimal"/> value.</summary>
    /// <remarks>The conversion is approximate.</remarks>
    public FP107(decimal value)
    {
      // a Decimal is a base-10 floating-point value. extract the base-10 mantissa and exponent
      int[] bits = Decimal.GetBits(value);
      Integer mantissa = new Integer(new uint[] { (uint)bits[0], (uint)bits[1], (uint)bits[2] }, false, false);
      int negExponent = ((bits[3]>>16) & 0xFF); // Decimal stores the negation of the actual exponent
      bool negative = (bits[3]>>31) != 0;

      // compute an approximation and refine it
      FP107 floatValue = (FP107)mantissa;
      if(negExponent != 0) floatValue /= FP107.Pow(10, negExponent);
      this = RefineParsedEstimate(floatValue, -negExponent, mantissa);
      if(negative) this = -this;
    }

    /// <summary>Initializes a new <see cref="FP107"/> value from a signed 32-bit integer.</summary>
    public FP107(int value)
    {
      hi = value;
      lo = 0;
    }

    /// <summary>Initializes a new <see cref="FP107"/> value from an unsigned 32-bit integer.</summary>
    [CLSCompliant(false)]
    public FP107(uint value)
    {
      hi = value;
      lo = 0;
    }

    /// <summary>Initializes a new <see cref="FP107"/> value from a signed 64-bit integer.</summary>
    public FP107(long value)
    {
      ulong magnitude;
      bool negative;
      if(value >= 0)
      {
        magnitude = (ulong)value;
        negative  = false;
      }
      else
      {
        magnitude = (ulong)-value;
        negative  = true;
      }

      if(magnitude <= IEEE754.MaxDoubleInt) // if the value is large enough to be represented by a double without loss of precision...
      {
        hi = value; // just convert the value directly to double
        lo = 0;
      }
      else // otherwise, we'll have to split it into two doubles
      {
        double a = IEEE754.ComposeDouble(negative, 32, magnitude >> 32), b = IEEE754.ComposeDouble(negative, 0, magnitude & 0xFFFFFFFF);
        hi = a + b; // and then normalize them
        lo = b - (hi - a);
      }
    }

    /// <summary>Initializes a new <see cref="FP107"/> value from an unsigned 64-bit integer.</summary>
    [CLSCompliant(false)]
    public FP107(ulong value)
    {
      if(value <= IEEE754.MaxDoubleInt) // if the value is large enough to be represented by a double without loss of precision...
      {
        hi = value; // just convert it directly to double
        lo = 0;
      }
      else // otherwise, we'll have to split it into two doubles
      {
        double a = IEEE754.ComposeDouble(false, 32, value >> 32), b = IEEE754.ComposeDouble(false, 0, value & 0xFFFFFFFF);
        hi = a + b; // and then normalize them
        lo = b - (hi - a);
      }
    }

    /// <summary>Initializes a new <see cref="FP107"/> value from an arbitrary-precision integer.</summary>
    /// <remarks>The conversion is approximate.</remarks>
    public FP107(Integer value)
    {
      this = (FP107)value;
    }

    /// <summary>Initializes a new <see cref="FP107"/> value from a <see cref="BinaryReader"/>. The value is expected to have been saved
    /// with the <see cref="Save"/> method.
    /// </summary>
    public FP107(BinaryReader reader)
    {
      if(reader == null) throw new ArgumentNullException();
      hi = reader.ReadDouble();
      lo = reader.ReadDouble();
    }

    /// <summary>Initializes a new <see cref="FP107"/> value from high and low components, with normalization.</summary>
    FP107(double a, double b)
    {
      if(double.IsInfinity(a))
      {
        hi = a;
        lo = 0;
      }
      else
      {
        hi = a + b;
        lo = b - (hi - a);
      }
    }

    /// <summary>Initializes a new <see cref="FP107"/> value from high and low components, without normalization.</summary>
    FP107(double hi, double lo, bool checkInfinities)
    {
      this.hi = hi;
      this.lo = checkInfinities && double.IsInfinity(hi) ? 0 : lo;
    }

    /// <summary>Determines whether the value represents positive or negative infinity.</summary>
    public bool IsInfinity
    {
      get { return double.IsInfinity(hi); }
    }

    /// <summary>Determines whether the value is not a number (NaN).</summary>
    public bool IsNaN
    {
      get { return double.IsNaN(hi); }
    }

    /// <summary>Determines whether the value is negative.</summary>
    public bool IsNegative
    {
      get { return hi < 0; }
    }

    /// <summary>Determines whether the value represents negative infinity.</summary>
    public bool IsNegativeInfinity
    {
      get { return double.IsNegativeInfinity(hi); }
    }

    /// <summary>Determines whether the value is a number (i.e. not NaN or infinity).</summary>
    public bool IsNumber
    {
      get { return hi.IsNumber(); }
    }

    /// <summary>Determines whether the value is positive.</summary>
    public bool IsPositive
    {
      get { return hi > 0; }
    }

    /// <summary>Determines whether the value represents positive infinity.</summary>
    public bool IsPositiveInfinity
    {
      get { return double.IsPositiveInfinity(hi); }
    }

    /// <summary>Determines whether the value is equal to zero.</summary>
    public bool IsZero
    {
      get { return hi == 0; }
    }

    /// <summary>Returns the magnitude (i.e. the absolute value) of this value.</summary>
    public FP107 Abs()
    {
      if(hi >= 0) return this; // it's positive or zero
      else return new FP107(-hi, -lo, false); // it's negative
    }

    /// <summary>Returns the arccosine of this value.</summary>
    public FP107 Acos()
    {
      return Acos(this);
    }

    /// <summary>Returns the hyperbolic arccosine of this value.</summary>
    /// <remarks>This method loses precision when the value is close to 1.</remarks>
    public FP107 Acosh()
    {
      return Acosh(this);
    }

    /// <summary>Returns the arcsine of this value.</summary>
    public FP107 Asin()
    {
      return Asin(this);
    }

    /// <summary>Returns the hyperbolic arcsine of this value.</summary>
    /// <remarks>This method loses precision when the value is close to 0.</remarks>
    public FP107 Asinh()
    {
      return Asinh(this);
    }

    /// <summary>Returns the arctangent of this value.</summary>
    public FP107 Atan()
    {
      return Atan(this);
    }

    /// <summary>Returns the hyperbolic arctangent of this value.</summary>
    public FP107 Atanh()
    {
      return Atanh(this);
    }

    /// <summary>Returns the smallest integer greater than or equal to this value.</summary>
    public FP107 Ceiling()
    {
      return Ceiling(this);
    }

    /// <summary>Compares this value to another, returning a negative number if it's less, a positive number if it's greater, and zero
    /// if the two values are equal. If either value is NaN, the result is undefined.
    /// </summary>
    public int CompareTo(FP107 value)
    {
      return hi > value.hi ? 1 : hi < value.hi ? -1 : lo > value.lo ? 1 : lo < value.lo ? -1 : 0;
    }

    /// <summary>Returns the cosine of this value.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/SinCosRemarks/node()"/>
    public FP107 Cos()
    {
      return Cos(this);
    }

    /// <summary>Returns the hyperbolic cosine of this value.</summary>
    public FP107 Cosh()
    {
      return Cosh(this);
    }

    /// <summary>Decomposes an <see cref="FP107"/> value into an exponent and mantissa, where the magnitude of the value equals
    /// <paramref name="mantissa"/> * 2^<paramref name="exponent"/>. If the value is <see cref="PositiveInfinity"/>,
    /// <see cref="NegativeInfinity"/>, or <see cref="NaN"/>, the method returns false.
    /// </summary>
    public bool Decompose(out bool negative, out int exponent, out Integer mantissa)
    {
      // decompose the high component, adjust the exponent if it's denormalized, add the hidden bit, and see if it matches special patterns
      ulong rawMantissa;
      IEEE754.RawDecompose(hi, out negative, out exponent, out rawMantissa);
      if(exponent == 0) exponent++; // if the number is denormalized (or zero) the actual exponent is one greater
      else if(exponent != (1<<IEEE754.DoubleExponentBits)-1) rawMantissa |= 1UL << IEEE754.DoubleMantissaBits; // add the hidden bit if normal
      else // the value is NaN or infinity
      {
        mantissa = default(Integer);
        return false;
      }

      // the number isn't infinity or NaN, so decompose the low component and construct the full 107-bit mantissa.
      mantissa = new Integer(rawMantissa);
      if(!mantissa.IsZero)
      {
        // decompose the low component
        bool loNegative;
        int loExponent;
        IEEE754.RawDecompose(lo, out loNegative, out loExponent, out rawMantissa);
        if(loExponent == 0) loExponent++;
        else if(loExponent != 0) rawMantissa |= 1UL << IEEE754.DoubleMantissaBits; // add the hidden one bit if the number is normalized

        // combine the low mantissa with the high one
        int bitLength = mantissa.BitLength; // keep track of the bit length separately since we use it to calculate the exponent below,
        if(rawMantissa != 0)                // and we don't want bit length changes due to the addition or subtraction to affect the
        {                                   // calculation of the exponent
          mantissa <<= exponent - loExponent;
          bitLength += exponent - loExponent;
          if(loNegative == negative) mantissa += rawMantissa; // add the low mantissa if the signs are equal
          else mantissa -= rawMantissa; // otherwise subtract it
        }

        exponent -= Math.Max(IEEE754.DoubleMantissaBits+1, bitLength) + (IEEE754.DoubleBias-1);
      }

      return true;
    }

    /// <summary>Divides this value by the given divisor, returns the quotient, and stores the remainder in <paramref name="remainder"/>.</summary>
    public FP107 DivRem(FP107 divisor, out FP107 remainder)
    {
      return DivRem(this, divisor, out remainder);
    }

    /// <summary>Determines whether this value is equal to the given object.</summary>
    public override bool Equals(object obj)
    {
      return obj is FP107 && Equals((FP107)obj);
    }

    /// <summary>Determines whether this value is equal to the given value.</summary>
    public bool Equals(FP107 value)
    {
      return hi == value.hi && lo == value.lo || IsNaN && value.IsNaN; // NaN != NaN, but NaN.Equals(NaN)
    }

    /// <summary>Raises e (Euler's constant) to a power equal to the current value, and returns the result.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/ExpRemarks/node()"/>
    public FP107 Exp()
    {
      return Exp(this);
    }

    /// <summary>Returns the largest integer less than or equal to this value.</summary>
    public FP107 Floor()
    {
      return Floor(this);
    }

    /// <summary>Returns the internal components of the <see cref="FP107"/> value. This method is intended to be used with the
    /// <see cref="FromComponents"/> method to reconstruct the value at a later time.
    /// </summary>
    public void GetComponents(out double first, out double second)
    {
      first  = hi;
      second = lo;
    }

    /// <summary>Computes a hash code for the value.</summary>
    public override int GetHashCode()
    {
      return hi.GetHashCode() ^ lo.GetHashCode();
    }

    /// <summary>Returns the natural logarithm of this value.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/LogRemarks/node()"/>
    public FP107 Log()
    {
      return Log(this);
    }

    /// <summary>Returns the base-10 logarithm of this value.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/Log10Remarks/node()"/>
    public FP107 Log10()
    {
      return Log10(this);
    }

    /// <summary>Returns this value raised to the given power.</summary>
    public FP107 Pow(int power)
    {
      return Pow(this, power);
    }

    /// <summary>Returns this value raised to the given power.</summary>
    public FP107 Pow(FP107 power)
    {
      return Pow(this, power);
    }

    /// <summary>Returns the given root (e.g. square root, cube root, etc) of the value.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/RootRemarks/node()"/>
    public FP107 Root(int root)
    {
      return Root(this, root);
    }

    /// <summary>Rounds this value to the nearest integer and returns the result.</summary>
    public FP107 Round()
    {
      return Round(this);
    }

    /// <summary>Saves this value to a <see cref="BinaryWriter"/>. The value can be recreated using the
    /// <see cref="FP107(BinaryReader)"/> constructor.
    /// </summary>
    public void Save(BinaryWriter writer)
    {
      if(writer == null) throw new ArgumentNullException();
      writer.Write(hi);
      writer.Write(lo);
    }

    /// <summary>Returns the sine of this value.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/SinCosRemarks/node()"/>
    public FP107 Sin()
    {
      return Sin(this);
    }

    /// <summary>Returns the hyperbolic sine of this value.</summary>
    public FP107 Sinh()
    {
      return Sinh(this);
    }

    /// <summary>Computes the sine and cosine of this value.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/SinCosRemarks/node()"/>
    public void SinCos(out FP107 sin, out FP107 cos)
    {
      SinCos(this, out sin, out cos);
    }

    /// <summary>Returns the sign of the value: -1 if the value is negative, 1 if the value is positive, and 0 if the value is zero.</summary>
    /// <exception cref="ArithmeticException">Thrown if the value is not a number (NaN).</exception>
    public int Sign()
    {
      return Math.Sign(hi);
    }

    /// <summary>Returns the square of the value. This is more efficient than multiplying it by itself.</summary>
    public FP107 Square()
    {
      double squarehi = hi*hi, slo, shi = Split(hi, out slo);
      double t = shi*shi - squarehi + 2.0*shi*slo + slo*slo;
      return new FP107(squarehi, 2*(hi*lo) + t);
    }

    /// <summary>Returns the square root of the value.</summary>
    public FP107 Sqrt()
    {
      return Sqrt(this);
    }

    /// <summary>Returns the tangent of this value.</summary>
    public FP107 Tan()
    {
      return Tan(this);
    }

    /// <summary>Returns the hyperbolic tangent of this value.</summary>
    public FP107 Tanh()
    {
      return Tanh(this);
    }

    /// <summary>Converts the value to string using the current culture.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/ToString/node()"/>
    public override string ToString()
    {
      return ToString(null, null);
    }

    /// <summary>Converts the value to string using the current culture.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/ToString/node()"/>
    public string ToString(string format)
    {
      return ToString(format, null);
    }

    /// <summary>Converts the value to string, using the given format provider.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/ToString/node()"/>
    public string ToString(IFormatProvider provider)
    {
      return ToString(null, provider);
    }

    /// <summary>Converts the value to string, using the given format provider.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/ToString/node()"/>
    public string ToString(string format, IFormatProvider provider)
    {
      // parse and validate the user's format string
      int desiredPrecision;
      char formatType;
      bool capitalize;
      formatType = ParseFormatString(format, out desiredPrecision, out capitalize);
      if(formatType == 'X') // if hex format was requested, return the raw component values converted to hex
      {
        format = capitalize ? "X" : "x";
        double localHi = hi, localLo = lo; // copy the values to the stack so we can take their addresses without pinning
        unsafe
        {
          return ((ulong*)&localHi)->ToString(format, CultureInfo.InvariantCulture).PadLeft(16, '0') + ":" +
                 ((ulong*)&localLo)->ToString(format, CultureInfo.InvariantCulture).PadLeft(16, '0');
        }
      }
      else if(formatType == 'S') // if component format was requested, return the raw components in decimal
      {
        if(lo == 0 || double.IsNaN(hi) || double.IsInfinity(hi)) return hi.ToString("R", CultureInfo.InvariantCulture);
        else return "[" + hi.ToString("R", CultureInfo.InvariantCulture) + ", " + lo.ToString("R", CultureInfo.InvariantCulture) + "]";
      }

      // get the number info from the format provider
      NumberFormatInfo nums = NumberFormatInfo.GetInstance(provider);

      // decompose the value into a sign, exponent, and mantissa
      Integer mantissa;
      int exponent;
      bool negative;
      if(!Decompose(out negative, out exponent, out mantissa))
      {
        if(IsNaN) return nums.NaNSymbol;
        else if(IsPositiveInfinity) return nums.PositiveInfinitySymbol;
        else return nums.NegativeInfinitySymbol;
      }

      // we want to increase the size of the mantissa to ensure it's at least 107 bits because the output precision depends on the mantissa
      // size, and if it's output with lower precision than the parser will use to read it back, it may not be read back accurately.
      if(mantissa.BitLength < 107 && !mantissa.IsZero)
      {
        exponent  -= 107 - mantissa.BitLength;
        mantissa <<= 107 - mantissa.BitLength; // the low component contributes a sign bit as well
      }

      // now convert the exponent and mantissa into decimal digits and then format the digits into a string
      int decimalPlace;
      byte[] digits = GetSignificantDigits(hi, exponent, -mantissa.BitLength - (IEEE754.DoubleBias-2), mantissa, out decimalPlace);
      // 32 is the typical number of full decimal digits of precision provided by FP107 (sometimes more or fewer)
      return NumberFormat.FormatNumber(digits, decimalPlace, negative, nums, formatType, desiredPrecision, 32, capitalize);
    }

    /// <summary>Returns the value, truncated towards zero.</summary>
    public FP107 Truncate()
    {
      return Truncate(this);
    }

    #region Arithmetic operators
    /// <summary>Negates an <see cref="FP107"/> value.</summary>
    public static FP107 operator-(FP107 value)
    {
      value.hi = -value.hi;
      value.lo = -value.lo;
      return value;
    }

    /// <summary>Increments an <see cref="FP107"/> value.</summary>
    public static FP107 operator++(FP107 value)
    {
      return value + 1d;
    }

    /// <summary>Decrements an <see cref="FP107"/> value.</summary>
    public static FP107 operator--(FP107 value)
    {
      return value - 1d;
    }

    /// <summary>Adds two <see cref="FP107"/> values.</summary>
    public static FP107 operator+(FP107 a, FP107 b)
    {
      double u;
      return new FP107(Add(a.hi, b.hi, out u), u + a.lo + b.lo);
    }

    /// <summary>Subtracts one <see cref="FP107"/> value from another.</summary>
    public static FP107 operator-(FP107 a, FP107 b)
    {
      double u;
      return new FP107(Subtract(a.hi, b.hi, out u), u + a.lo - b.lo);
    }

    /// <summary>Multiplies two <see cref="FP107"/> values.</summary>
    public static FP107 operator*(FP107 a, FP107 b)
    {
      double t;
      return new FP107(Multiply(a.hi, b.hi, out t), a.hi*b.lo + t + a.lo*b.hi);
    }

    /// <summary>Divides one <see cref="FP107"/> value by another.</summary>
    public static FP107 operator/(FP107 a, FP107 b)
    {
      if(a.IsInfinity)
      {
        if(b.IsInfinity || b.IsNaN || b.IsZero) return NaN; // infinity / {infinity,NaN,0} = NaN
        else if(a.Sign() == Math.Sign(b.hi)) return PositiveInfinity; // +infinity/+infinity = -infinity/-infinity = +infinity
        else return NegativeInfinity; // +infinity/-infinity = -infinity/+infinity = -infinity
      }
      else if(b.IsInfinity)
      {
        if(a.IsNaN) return NaN; // NaN/infinity = NaN
        else if(a.Sign() == b.Sign()) return Zero; // +finity/+infinity = -finity/-infinity = +0, 
        else return -Zero; // -finity/+infinity = +finity/-infinity = -0
      }

      double qhi = a.hi/b.hi; // NOTE: we can't optimize this by computing 1/b.hi because it reduces precision too much
      FP107 r = b * qhi;
      double slo, shi = Subtract(a.hi, r.hi, out slo);
      return new FP107(qhi, (slo-r.lo+a.lo+shi)/b.hi);
    }

    /// <summary>Divides one <see cref="FP107"/> value by another and returns the remainder.</summary>
    public static FP107 operator%(FP107 a, FP107 b)
    {
      if(a.IsInfinity) return NaN;
      else if(b.IsInfinity) return a;
      return a - (a/b).Truncate()*b;
    }

    /// <summary>Adds an <see cref="FP107"/> value and a double-precision floating-point value.</summary>
    public static FP107 operator+(FP107 a, double b)
    {
      double u;
      return new FP107(Add(a.hi, b, out u), u + a.lo);
    }

    /// <summary>Subtracts a double-precision floating-point value from an <see cref="FP107"/> value.</summary>
    public static FP107 operator-(FP107 a, double b)
    {
      double u;
      return new FP107(Subtract(a.hi, b, out u), u + a.lo);
    }

    /// <summary>Multiplies an <see cref="FP107"/> value and a double-precision floating-point value.</summary>
    public static FP107 operator*(FP107 a, double b)
    {
      double t;
      return new FP107(Multiply(a.hi, b, out t), t + a.lo*b);
    }

    /// <summary>Divides an <see cref="FP107"/> value by a double-precision floating-point value.</summary>
    public static FP107 operator/(FP107 a, double b)
    {
      return a / new FP107(b);
    }

    /// <summary>Divides an <see cref="FP107"/> value by a double-precision floating-point value and returns the remainder.</summary>
    public static FP107 operator%(FP107 a, double b)
    {
      return a % new FP107(b);
    }

    /// <summary>Adds an <see cref="FP107"/> value and a 32-bit signed integer value.</summary>
    public static FP107 operator+(FP107 a, int b)
    {
      return a + (double)b;
    }

    /// <summary>Subtracts a 32-bit signed integer value from an <see cref="FP107"/> value.</summary>
    public static FP107 operator-(FP107 a, int b)
    {
      return a - (double)b;
    }

    /// <summary>Multiplies an <see cref="FP107"/> value and a 32-bit signed integer value.</summary>
    public static FP107 operator*(FP107 a, int b)
    {
      return a * (double)b;
    }

    /// <summary>Divides an <see cref="FP107"/> value by a 32-bit signed integer value.</summary>
    public static FP107 operator/(FP107 a, int b)
    {
      return a / new FP107(b);
    }

    /// <summary>Divides an <see cref="FP107"/> value by a 32-bit signed integer value and returns the remainder.</summary>
    public static FP107 operator%(FP107 a, int b)
    {
      return a % new FP107(b);
    }

    /// <summary>Adds an <see cref="FP107"/> value and a 32-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static FP107 operator+(FP107 a, uint b)
    {
      return a + (double)b;
    }

    /// <summary>Subtracts a 32-bit unsigned integer value from an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static FP107 operator-(FP107 a, uint b)
    {
      return a - (double)b;
    }

    /// <summary>Multiplies an <see cref="FP107"/> value and a 32-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static FP107 operator*(FP107 a, uint b)
    {
      return a * (double)b;
    }

    /// <summary>Divides an <see cref="FP107"/> value by a 32-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static FP107 operator/(FP107 a, uint b)
    {
      return a / new FP107(b);
    }

    /// <summary>Divides an <see cref="FP107"/> value by a 32-bit unsigned integer value and returns the remainder.</summary>
    [CLSCompliant(false)]
    public static FP107 operator%(FP107 a, uint b)
    {
      return a % new FP107(b);
    }

    /// <summary>Adds an <see cref="FP107"/> value and a 64-bit signed integer value.</summary>
    public static FP107 operator+(FP107 a, long b)
    {
      return a + new FP107(b);
    }

    /// <summary>Subtracts a 64-bit signed integer value from an <see cref="FP107"/> value.</summary>
    public static FP107 operator-(FP107 a, long b)
    {
      return a - new FP107(b);
    }

    /// <summary>Multiplies an <see cref="FP107"/> value and a 64-bit signed integer value.</summary>
    public static FP107 operator*(FP107 a, long b)
    {
      return a * new FP107(b);
    }

    /// <summary>Divides an <see cref="FP107"/> value by a 64-bit signed integer value.</summary>
    public static FP107 operator/(FP107 a, long b)
    {
      return a / new FP107(b);
    }

    /// <summary>Divides an <see cref="FP107"/> value by a 64-bit signed integer value and returns the remainder.</summary>
    public static FP107 operator%(FP107 a, long b)
    {
      return a % new FP107(b);
    }

    /// <summary>Adds an <see cref="FP107"/> value and a 64-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static FP107 operator+(FP107 a, ulong b)
    {
      return a + new FP107(b);
    }

    /// <summary>Subtracts a 64-bit unsigned integer value from an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static FP107 operator-(FP107 a, ulong b)
    {
      return a - new FP107(b);
    }

    /// <summary>Multiplies an <see cref="FP107"/> value and a 64-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static FP107 operator*(FP107 a, ulong b)
    {
      return a * new FP107(b);
    }

    /// <summary>Divides an <see cref="FP107"/> value by a 64-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static FP107 operator/(FP107 a, ulong b)
    {
      return a / new FP107(b);
    }

    /// <summary>Divides an <see cref="FP107"/> value by a 64-bit unsigned integer value and returns the remainder.</summary>
    [CLSCompliant(false)]
    public static FP107 operator%(FP107 a, ulong b)
    {
      return a % new FP107(b);
    }

    /// <summary>Adds a double-precision floating-point value and an <see cref="FP107"/> value.</summary>
    public static FP107 operator+(double a, FP107 b)
    {
      double u;
      return new FP107(Add(a, b.hi, out u), u + b.lo);
    }

    /// <summary>Subtracts an <see cref="FP107"/> value from a double-precision floating-point value.</summary>
    public static FP107 operator-(double a, FP107 b)
    {
      double u;
      return new FP107(Subtract(a, b.hi, out u), u - b.lo);
    }

    /// <summary>Multiplies a double-precision floating-point value and an <see cref="FP107"/> value.</summary>
    public static FP107 operator*(double a, FP107 b)
    {
      double t;
      return new FP107(Multiply(a, b.hi, out t), t + a*b.lo);
    }

    /// <summary>Divides a double-precision floating-point value by an <see cref="FP107"/> value.</summary>
    public static FP107 operator/(double a, FP107 b)
    {
      return new FP107(a) / b;
    }

    /// <summary>Divides a double-precision floating-point value by an <see cref="FP107"/> value and returns the remainder.</summary>
    public static FP107 operator%(double a, FP107 b)
    {
      return new FP107(a) % b;
    }

    /// <summary>Adds a 32-bit signed integer value and an <see cref="FP107"/> value.</summary>
    public static FP107 operator+(int a, FP107 b)
    {
      return (double)a + b;
    }

    /// <summary>Subtracts an <see cref="FP107"/> value from a 32-bit signed integer value.</summary>
    public static FP107 operator-(int a, FP107 b)
    {
      return (double)a - b;
    }

    /// <summary>Multiplies a 32-bit signed integer value and an <see cref="FP107"/> value.</summary>
    public static FP107 operator*(int a, FP107 b)
    {
      return (double)a * b;
    }

    /// <summary>Divides a 32-bit signed integer value by an <see cref="FP107"/> value.</summary>
    public static FP107 operator/(int a, FP107 b)
    {
      return new FP107(a) / b;
    }

    /// <summary>Divides a 32-bit signed integer value by an <see cref="FP107"/> value and returns the remainder.</summary>
    public static FP107 operator%(int a, FP107 b)
    {
      return new FP107(a) % b;
    }

    /// <summary>Adds a 32-bit unsigned integer value and an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static FP107 operator+(uint a, FP107 b)
    {
      return (double)a + b;
    }

    /// <summary>Subtracts an <see cref="FP107"/> value from a 32-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static FP107 operator-(uint a, FP107 b)
    {
      return (double)a - b;
    }

    /// <summary>Multiplies a 32-bit unsigned integer value and an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static FP107 operator*(uint a, FP107 b)
    {
      return (double)a * b;
    }

    /// <summary>Divides a 32-bit unsigned integer value by an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static FP107 operator/(uint a, FP107 b)
    {
      return new FP107(a) / b;
    }

    /// <summary>Divides a 32-bit unsigned integer value by an <see cref="FP107"/> value and returns the remainder.</summary>
    [CLSCompliant(false)]
    public static FP107 operator%(uint a, FP107 b)
    {
      return new FP107(a) % b;
    }

    /// <summary>Adds a 64-bit signed integer value and an <see cref="FP107"/> value.</summary>
    public static FP107 operator+(long a, FP107 b)
    {
      return new FP107(a) + b;
    }

    /// <summary>Subtracts an <see cref="FP107"/> value from a 64-bit signed integer value.</summary>
    public static FP107 operator-(long a, FP107 b)
    {
      return new FP107(a) - b;
    }

    /// <summary>Multiplies a 64-bit signed integer value and an <see cref="FP107"/> value.</summary>
    public static FP107 operator*(long a, FP107 b)
    {
      return new FP107(a) * b;
    }

    /// <summary>Divides a 64-bit signed integer value by an <see cref="FP107"/> value.</summary>
    public static FP107 operator/(long a, FP107 b)
    {
      return new FP107(a) / b;
    }

    /// <summary>Divides a 64-bit signed integer value by an <see cref="FP107"/> value and returns the remainder.</summary>
    public static FP107 operator%(long a, FP107 b)
    {
      return new FP107(a) % b;
    }

    /// <summary>Adds a 64-bit unsigned integer value and an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static FP107 operator+(ulong a, FP107 b)
    {
      return new FP107(a) + b;
    }

    /// <summary>Subtracts an <see cref="FP107"/> value from a 64-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static FP107 operator-(ulong a, FP107 b)
    {
      return new FP107(a) - b;
    }

    /// <summary>Multiplies a 64-bit unsigned integer value and an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static FP107 operator*(ulong a, FP107 b)
    {
      return new FP107(a) * b;
    }

    /// <summary>Divides a 64-bit unsigned integer value by an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static FP107 operator/(ulong a, FP107 b)
    {
      return new FP107(a) / b;
    }

    /// <summary>Divides a 64-bit unsigned integer value by an <see cref="FP107"/> value and returns the remainder.</summary>
    [CLSCompliant(false)]
    public static FP107 operator%(ulong a, FP107 b)
    {
      return new FP107(a) % b;
    }
    #endregion

    #region Comparison operators
    /// <summary>Determines whether two <see cref="FP107"/> values are equal.</summary>
    public static bool operator==(FP107 a, FP107 b)
    {
      return a.hi == b.hi && a.lo == b.lo;
    }

    /// <summary>Determines whether two <see cref="FP107"/> values are unequal.</summary>
    public static bool operator!=(FP107 a, FP107 b)
    {
      return a.hi != b.hi || a.lo != b.lo;
    }

    /// <summary>Determines whether one <see cref="FP107"/> value is less than another.</summary>
    public static bool operator<(FP107 a, FP107 b)
    {
      return a.hi < b.hi || a.hi == b.hi && a.lo < b.lo;
    }

    /// <summary>Determines whether one <see cref="FP107"/> value is less than or equal to another.</summary>
    public static bool operator<=(FP107 a, FP107 b)
    {
      return a.hi < b.hi || a.hi == b.hi && a.lo <= b.lo;
    }

    /// <summary>Determines whether one <see cref="FP107"/> value is greater than another.</summary>
    public static bool operator>(FP107 a, FP107 b)
    {
      return a.hi > b.hi || a.hi == b.hi && a.lo > b.lo;
    }

    /// <summary>Determines whether one <see cref="FP107"/> value is greater than or equal to another.</summary>
    public static bool operator>=(FP107 a, FP107 b)
    {
      return a.hi > b.hi || a.hi == b.hi && a.lo >= b.lo;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is equal to a double-precision floating-point value.</summary>
    public static bool operator==(FP107 a, double b)
    {
      return a.hi == b && a.lo == 0;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is unequal to a double-precision floating-point value.</summary>
    public static bool operator!=(FP107 a, double b)
    {
      return a.hi != b || a.lo != 0;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is less than a double-precision floating-point value.</summary>
    public static bool operator<(FP107 a, double b)
    {
      return a.hi < b || a.hi == b && a.lo < 0;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is less than or equal to a double-precision floating-point value.</summary>
    public static bool operator<=(FP107 a, double b)
    {
      return a.hi < b || a.hi == b && a.lo <= 0;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is greater than a double-precision floating-point value.</summary>
    public static bool operator>(FP107 a, double b)
    {
      return a.hi > b || a.hi == b && a.lo > 0;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is greater than or equal to a double-precision floating-point value.</summary>
    public static bool operator>=(FP107 a, double b)
    {
      return a.hi > b || a.hi == b && a.lo >= 0;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is equal to a 32-bit signed integer value.</summary>
    public static bool operator==(FP107 a, int b)
    {
      return a.hi == b && a.lo == 0;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is unequal to a 32-bit signed integer value.</summary>
    public static bool operator!=(FP107 a, int b)
    {
      return a.hi != b || a.lo != 0;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is less than a 32-bit signed integer value.</summary>
    public static bool operator<(FP107 a, int b)
    {
      return a < (double)b;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is less than or equal to a 32-bit signed integer value.</summary>
    public static bool operator<=(FP107 a, int b)
    {
      return a <= (double)b;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is greater than a 32-bit signed integer value.</summary>
    public static bool operator>(FP107 a, int b)
    {
      return a > (double)b;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is greater than or equal to a 32-bit signed integer value.</summary>
    public static bool operator>=(FP107 a, int b)
    {
      return a >= (double)b;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is equal to a 32-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static bool operator==(FP107 a, uint b)
    {
      return a.hi == b && a.lo == 0;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is unequal to a 32-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static bool operator!=(FP107 a, uint b)
    {
      return a.hi != b || a.lo != 0;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is less than a 32-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static bool operator<(FP107 a, uint b)
    {
      return a < (double)b;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is less than or equal to a 32-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static bool operator<=(FP107 a, uint b)
    {
      return a <= (double)b;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is greater than a 32-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static bool operator>(FP107 a, uint b)
    {
      return a > (double)b;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is greater than or equal to a 32-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static bool operator>=(FP107 a, uint b)
    {
      return a >= (double)b;
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is equal to a 64-bit signed integer value.</summary>
    public static bool operator==(FP107 a, long b)
    {
      return a == new FP107(b);
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is unequal to a 64-bit signed integer value.</summary>
    public static bool operator!=(FP107 a, long b)
    {
      return a != new FP107(b);
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is less than a 64-bit signed integer value.</summary>
    public static bool operator<(FP107 a, long b)
    {
      return a < new FP107(b);
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is less than or equal to a 64-bit signed integer value.</summary>
    public static bool operator<=(FP107 a, long b)
    {
      return a <= new FP107(b);
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is greater than a 64-bit signed integer value.</summary>
    public static bool operator>(FP107 a, long b)
    {
      return a > new FP107(b);
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is greater than or equal to a 64-bit signed integer value.</summary>
    public static bool operator>=(FP107 a, long b)
    {
      return a >= new FP107(b);
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is equal to a 64-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static bool operator==(FP107 a, ulong b)
    {
      return a == new FP107(b);
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is unequal to a 64-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static bool operator!=(FP107 a, ulong b)
    {
      return a != new FP107(b);
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is less than a 64-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static bool operator<(FP107 a, ulong b)
    {
      return a < new FP107(b);
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is less than or equal to a 64-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static bool operator<=(FP107 a, ulong b)
    {
      return a <= new FP107(b);
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is greater than a 64-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static bool operator>(FP107 a, ulong b)
    {
      return a > new FP107(b);
    }

    /// <summary>Determines whether an <see cref="FP107"/> value is greater than or equal to a 64-bit unsigned integer value.</summary>
    [CLSCompliant(false)]
    public static bool operator>=(FP107 a, ulong b)
    {
      return a >= new FP107(b);
    }

    /// <summary>Determines whether a double-precision floating-point value is equal to an <see cref="FP107"/> value.</summary>
    public static bool operator==(double a, FP107 b)
    {
      return a == b.hi && b.lo == 0;
    }

    /// <summary>Determines whether a double-precision floating-point value is unequal to an <see cref="FP107"/> value.</summary>
    public static bool operator!=(double a, FP107 b)
    {
      return a != b.hi || b.lo != 0;
    }

    /// <summary>Determines whether a double-precision floating-point value is less than an <see cref="FP107"/> value.</summary>
    public static bool operator<(double a, FP107 b)
    {
      return a < b.hi || a == b.hi && b.lo > 0;
    }

    /// <summary>Determines whether a double-precision floating-point value is less than or equal to an <see cref="FP107"/> value.</summary>
    public static bool operator<=(double a, FP107 b)
    {
      return a < b.hi || a == b.hi && b.lo >= 0;
    }

    /// <summary>Determines whether a double-precision floating-point value is greater than an <see cref="FP107"/> value.</summary>
    public static bool operator>(double a, FP107 b)
    {
      return a > b.hi || a == b.hi && b.lo < 0;
    }

    /// <summary>Determines whether a double-precision floating-point value is greater than or equal to an <see cref="FP107"/> value.</summary>
    public static bool operator>=(double a, FP107 b)
    {
      return a > b.hi || a == b.hi && b.lo <= 0;
    }

    /// <summary>Determines whether a 32-bit signed integer value is equal to an <see cref="FP107"/> value.</summary>
    public static bool operator==(int a, FP107 b)
    {
      return a == b.hi && b.lo == 0;
    }

    /// <summary>Determines whether a 32-bit signed integer value is unequal to an <see cref="FP107"/> value.</summary>
    public static bool operator!=(int a, FP107 b)
    {
      return a != b.hi || b.lo != 0;
    }

    /// <summary>Determines whether a 32-bit signed integer value is less than an <see cref="FP107"/> value.</summary>
    public static bool operator<(int a, FP107 b)
    {
      return (double)a < b;
    }

    /// <summary>Determines whether a 32-bit signed integer value is less than or equal to an <see cref="FP107"/> value.</summary>
    public static bool operator<=(int a, FP107 b)
    {
      return (double)a <= b;
    }

    /// <summary>Determines whether a 32-bit signed integer value is greater than an <see cref="FP107"/> value.</summary>
    public static bool operator>(int a, FP107 b)
    {
      return (double)a > b;
    }

    /// <summary>Determines whether a 32-bit signed integer value is greater than or equal to an <see cref="FP107"/> value.</summary>
    public static bool operator>=(int a, FP107 b)
    {
      return (double)a >= b;
    }

    /// <summary>Determines whether a 32-bit unsigned integer value is equal to an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator==(uint a, FP107 b)
    {
      return a == b.hi && b.lo == 0;
    }

    /// <summary>Determines whether a 32-bit unsigned integer value is unequal to an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator!=(uint a, FP107 b)
    {
      return a != b.hi || b.lo != 0;
    }

    /// <summary>Determines whether a 32-bit unsigned integer value is less than an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator<(uint a, FP107 b)
    {
      return (double)a < b;
    }

    /// <summary>Determines whether a 32-bit unsigned integer value is less than or equal to an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator<=(uint a, FP107 b)
    {
      return (double)a <= b;
    }

    /// <summary>Determines whether a 32-bit unsigned integer value is greater than an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator>(uint a, FP107 b)
    {
      return (double)a > b;
    }

    /// <summary>Determines whether a 32-bit unsigned integer value is greater than or equal to an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator>=(uint a, FP107 b)
    {
      return (double)a >= b;
    }

    /// <summary>Determines whether a 64-bit signed integer value is equal to an <see cref="FP107"/> value.</summary>
    public static bool operator==(long a, FP107 b)
    {
      return (double)a == b;
    }

    /// <summary>Determines whether a 64-bit signed integer value is unequal to an <see cref="FP107"/> value.</summary>
    public static bool operator!=(long a, FP107 b)
    {
      return new FP107(a) != b;
    }

    /// <summary>Determines whether a 64-bit signed integer value is less than an <see cref="FP107"/> value.</summary>
    public static bool operator<(long a, FP107 b)
    {
      return new FP107(a) < b;
    }

    /// <summary>Determines whether a 64-bit signed integer value is less than or equal to an <see cref="FP107"/> value.</summary>
    public static bool operator<=(long a, FP107 b)
    {
      return new FP107(a) <= b;
    }

    /// <summary>Determines whether a 64-bit signed integer value is greater than an <see cref="FP107"/> value.</summary>
    public static bool operator>(long a, FP107 b)
    {
      return new FP107(a) > b;
    }

    /// <summary>Determines whether a 64-bit signed integer value is greater than or equal to an <see cref="FP107"/> value.</summary>
    public static bool operator>=(long a, FP107 b)
    {
      return new FP107(a) >= b;
    }

    /// <summary>Determines whether a 64-bit unsigned integer value is equal to an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator==(ulong a, FP107 b)
    {
      return new FP107(a) == b;
    }

    /// <summary>Determines whether a 64-bit unsigned integer value is unequal to an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator!=(ulong a, FP107 b)
    {
      return new FP107(a) != b;
    }

    /// <summary>Determines whether a 64-bit unsigned integer value is less than an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator<(ulong a, FP107 b)
    {
      return new FP107(a) < b;
    }

    /// <summary>Determines whether a 64-bit unsigned integer value is less than or equal to an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator<=(ulong a, FP107 b)
    {
      return new FP107(a) <= b;
    }

    /// <summary>Determines whether a 64-bit unsigned integer value is greater than an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator>(ulong a, FP107 b)
    {
      return new FP107(a) > b;
    }

    /// <summary>Determines whether a 64-bit unsigned integer value is greater than or equal to an <see cref="FP107"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator>=(ulong a, FP107 b)
    {
      return new FP107(a) >= b;
    }
    #endregion

    #region Conversion operators
    /// <summary>Provides an implicit conversion from double to <see cref="FP107"/>.</summary>
    public static implicit operator FP107(double value)
    {
      return new FP107(value);
    }

    /// <summary>Provides an implicit conversion from a signed 32-bit integer to <see cref="FP107"/>.</summary>
    public static implicit operator FP107(int value)
    {
      return new FP107(value);
    }

    /// <summary>Provides an implicit conversion from an unsigned 32-bit integer to <see cref="FP107"/>.</summary>
    [CLSCompliant(false)]
    public static implicit operator FP107(uint value)
    {
      return new FP107(value);
    }

    /// <summary>Provides an implicit conversion from a signed 64-bit integer to <see cref="FP107"/>.</summary>
    public static implicit operator FP107(long value)
    {
      return new FP107(value);
    }

    /// <summary>Provides an implicit conversion from an unsigned 64-bit integer to <see cref="FP107"/>.</summary>
    [CLSCompliant(false)]
    public static implicit operator FP107(ulong value)
    {
      return new FP107(value);
    }

    /// <summary>Provides an explicit conversion from <see cref="decimal"/> to <see cref="FP107"/>.</summary>
    public static explicit operator FP107(decimal value)
    {
      return new FP107(value);
    }

    /// <summary>Provides an explicit conversion from an <see cref="FP107"/> value to a double-precision floating-point value.</summary>
    public static explicit operator double(FP107 value)
    {
      return value.hi;
    }

    /// <summary>Provides an explicit conversion from an <see cref="FP107"/> value to a single-precision floating-point value.</summary>
    public static explicit operator float(FP107 value)
    {
      return (float)value.hi;
    }

    /// <summary>Provides an explicit conversion from an <see cref="FP107"/> value to <see cref="decimal"/>.</summary>
    public static explicit operator decimal(FP107 value)
    {
      // decompose the value into a sign, exponent, and mantissa
      Integer mantissa;
      int exponent;
      bool negative;
      if(!value.Decompose(out negative, out exponent, out mantissa))
      {
        throw new InvalidCastException("The FP107 value is NaN or Infinity.");
      }

      // now convert the exponent and mantissa into decimal digits
      int decimalPlace;
      byte[] digits = GetSignificantDigits(value.hi, exponent, -mantissa.BitLength - (IEEE754.DoubleBias-2), mantissa, out decimalPlace);
      return NumberFormat.DigitsToDecimal(digits, decimalPlace, negative);
    }

    /// <summary>Provides an explicit conversion from an <see cref="FP107"/> value to a 32-bit signed integer.</summary>
    public static explicit operator int(FP107 value)
    {
      return (int)(uint)value;
    }

    /// <summary>Provides an explicit conversion from an <see cref="FP107"/> value to a 32-bit unsigned integer.</summary>
    [CLSCompliant(false)]
    public static explicit operator uint(FP107 value)
    {
      uint intValue = (uint)value.hi;
      double remainder = value.hi % (intValue != 0 ? intValue : (double)uint.MaxValue+1);
      intValue += (uint)(int)(value.lo + remainder);
      return intValue;
    }

    /// <summary>Provides an explicit conversion from an <see cref="FP107"/> value to a 64-bit signed integer.</summary>
    public static explicit operator long(FP107 value)
    {
      return (long)(ulong)value;
    }

    /// <summary>Provides an explicit conversion from an <see cref="FP107"/> value to a 64-bit unsigned integer.</summary>
    [CLSCompliant(false)]
    public static explicit operator ulong(FP107 value)
    {
      ulong intValue = (ulong)value.hi;
      double remainder = value.hi % (intValue != 0 ? intValue : (double)ulong.MaxValue+1);
      intValue += (ulong)(long)(value.lo + remainder);
      return intValue;
    }

    /// <summary>Provides an explicit conversion from an <see cref="FP107"/> value to an arbitrary precision <see cref="Integer"/>.</summary>
    public static explicit operator Integer(FP107 value)
    {
      return new Integer(value);
    }
    #endregion

    /// <summary>Returns the magnitude (i.e. the absolute value) of a given value.</summary>
    public static FP107 Abs(FP107 value)
    {
      if(value.hi >= 0) return value; // it's positive or zero
      else return new FP107(-value.hi, -value.lo, false); // it's negative
    }

    /// <summary>Returns the arccosine of the given value.</summary>
    public static FP107 Acos(FP107 value)
    {
      FP107 abs = value.Abs();
      if(abs < 1d)
      {
        return Atan2(Sqrt(1d - value.Square()), value);
      }
      else if(abs == 1d)
      {
        if(value.IsPositive) return Zero;
        else return Pi;
      }
      else
      {
        return NaN;
      }
    }

    /// <summary>Returns the hyperbolic arccosine of the given value.</summary>
    /// <remarks>This method loses precision when the value is close to 1.</remarks>
    public static FP107 Acosh(FP107 value)
    {
      if(value < 1d) return NaN;
      return Log(value + Sqrt(value.Square() - 1d));
    }

    /// <summary>Adds two double-precision floating-point values with high precision and returns the result as an <see cref="FP107"/> value.</summary>
    public static FP107 Add(double a, double b)
    {
      double lo;
      return new FP107(Add(a, b, out lo), lo, true);
    }

    /// <summary>Adds three double-precision floating-point values with high precision and returns the result as an <see cref="FP107"/> value.</summary>
    public static FP107 Add(double a, double b, double c)
    {
      double ilo, ihi = Add(a, b, out ilo), ilo2;
      return new FP107(Add(ihi, c, out ilo2), ilo + ilo2, true);
    }

    /// <summary>Returns the arcsine of the given value.</summary>
    public static FP107 Asin(FP107 value)
    {
      FP107 abs = value.Abs();
      if(abs < 1d)
      {
        return Atan2(value, Sqrt(1d - value.Square()));
      }
      else if(abs == 1d)
      {
        if(value.IsNegative) return -PiOverTwo;
        else return PiOverTwo;
      }
      else
      {
        return NaN;
      }
    }

    /// <summary>Returns the hyperbolic arcsine of the given value.</summary>
    /// <remarks>This method loses precision when the value is close to 0.</remarks>
    public static FP107 Asinh(FP107 value)
    {
      if(value.IsInfinity) return value;
      return Log(value + Sqrt(value.Square() + 1d));
    }

    /// <summary>Returns the arctangent of the given value.</summary>
    public static FP107 Atan(FP107 value)
    {
      return Atan2(value, One);
    }

    /// <summary>Returns the angle whose tangent equals <paramref name="y"/>/<paramref name="x"/>.</summary>
    public static FP107 Atan2(FP107 y, FP107 x)
    {
      // first, get all the special cases out of the way
      if(x.IsNaN || y.IsNaN)
      {
        return NaN;
      }
      else if(x.IsZero)
      {
        if(y.IsZero) return NaN;
        else if(y.IsNegative) return -PiOverTwo;
        else return PiOverTwo;
      }
      else if(y.IsZero)
      {
        if(x.IsPositive) return Zero;
        else return Pi;
      }
      else if(x.IsInfinity)
      {
        if(y.IsInfinity) return NaN;
        else if(x.IsPositiveInfinity) return Zero;
        else if(y.IsPositive) return Pi;
        else return -Pi;
      }
      else if(y.IsInfinity)
      {
        if(y.IsPositiveInfinity) return PiOverTwo;
        else return -PiOverTwo;
      }
      else if(x == y)
      {
        if(y.IsPositive) return Pi.ScaleByPowerOfTwo(0.25); // Pi/4
        return (-3*Pi).ScaleByPowerOfTwo(0.25); // -3Pi/4
      }
      else if(x == -y)
      {
        if(y.IsPositive) return (3*Pi).ScaleByPowerOfTwo(0.25); // 3Pi/4
        else return -Pi.ScaleByPowerOfTwo(0.25); // -Pi/4
      }

      FP107 result = Math.Atan2(y.hi, x.hi); // get the initial estimate
      FP107 hypotenuse = Sqrt(x.Square() + y.Square()), sin, cos;

      x /= hypotenuse; // normalize x and y so that x^2 + y^2 = 1
      y /= hypotenuse;

      if(x.Abs() > y.Abs())
      {
        SinCos(result, out sin, out cos);
        result += (y - sin) / cos; // refine using Newton's method: a' = a + (y - sin(a)) / cos(a)
      }
      else
      {
        SinCos(result, out sin, out cos);
        result -= (x - cos) / sin; // refine using Newton's method: a' = a - (x - cos(a)) / sin(a)
      }
      return result;
    }

    /// <summary>Returns the hyperbolic arctangent of the given value.</summary>
    public static FP107 Atanh(FP107 value)
    {
      if(value.Abs() >= 1d) return NaN;
      else return Log((1d + value) / (1d - value)).ScaleByPowerOfTwo(0.5);
    }

    /// <summary>Returns the smallest integer greater than or equal to the given value.</summary>
    public static FP107 Ceiling(FP107 value)
    {
      // case:       hi:  lo:   val:   f(hi): f(lo):  f(lo)+f(hi):  f(val): adj:
      // zero+zero   0    0     0      0      0       0             0       0
      // int+int     10   2     12     10     2       12            12      0
      // int+zero    10   0     10     10     0       10            10      0
      // int-int     10  -2     8      10    -2       8             8       0
      // int+frac    10   2.3   12.3   10     3       13            13      0
      // int-frac    10  -2.3   7.7    10    -2       8             8       0
      // frac+frac   1.2  0.03  1.23   2      1       3             2      -1
      // frac+zero   1.2  0     1.2    2      0       2             2       0
      // frac-frac   1.2 -0.03  1.17   2      0       2             2       0
      // -int+int   -10   2    -8     -10     2      -8            -8       0
      // -int+zero  -10   0    -10    -10     0      -10           -10      0
      // -int-int   -10  -2    -12    -10    -2      -12           -12      0
      // -int+frac  -10   2.3  -7.7   -10     3      -7            -7       0
      // -int-frac  -10  -2.3  -12.3  -10    -2      -12           -12      0
      // -frac+frac -1.2  0.03 -1.17  -1      1       0            -1      -1
      // -frac+zero -1.2  0    -1.2   -1      0      -1            -1       0
      // -frac-frac -1.2 -0.03 -1.23  -1      0      -1            -1       0
      // synopsis: subtract 1 when hi and low have fractions and lo is positive
      double chi = Math.Ceiling(value.hi), clo = Math.Ceiling(value.lo);
      if(chi != value.hi && clo != value.lo && value.lo > 0) clo--;
      return new FP107(chi, clo); // renormalize because clo could have changed magnitude substantially relative to chi
    }

    /// <summary>Composes an <see cref="FP107"/> value from an exponent and mantissa, where the magnitude of the value equals
    /// <paramref name="mantissa"/> * 2^<paramref name="exponent"/>.
    /// </summary>
    public static FP107 Compose(bool negative, int exponent, Integer mantissa)
    {
      FP107 value = Compose(exponent, mantissa, false, true);
      if(negative) value = -value;
      return value;
    }

    /// <summary>Composes an <see cref="FP107"/> value from an exponent and mantissa, where the magnitude of the value equals
    /// <paramref name="mantissa"/> * 2^<paramref name="exponent"/>.
    /// </summary>
    /// <remarks>If the value is out of range and <paramref name="allowOutOfRange"/> is true, 0 or infinity will be returned, as
    /// appropriate. Otherwise, an exception will be thrown. An exception will be thrown in any case if the mantissa cannot be represented
    /// exactly.
    /// </remarks>
    public static FP107 Compose(bool negative, int exponent, Integer mantissa, bool allowOutOfRange)
    {
      FP107 value = Compose(exponent, mantissa, allowOutOfRange, true);
      if(negative) value = -value;
      return value;
    }

    /// <summary>Returns the cosine of the given value.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/SinCosRemarks/node()"/>
    public static FP107 Cos(FP107 value)
    {
      if(value.IsZero) return One;

      // see Sin(FP107) for a description of the strategy here.
      int j, k;
      if(!ReduceSinCosArgument(ref value, out j, out k)) return NaN;

      if(k == 0)
      {
        switch(j)
        {
          case 0: return CosReduced(value);
          case 1: return -SinReduced(value);
          case -1: return SinReduced(value);
          default: return -CosReduced(value);
        }
      }

      int absk = Math.Abs(k);
      FP107 u = CosTable[absk-1], v = SinTable[absk-1], sin, cos;
      SinCosReduced(value, out sin, out cos);
      if((j & 1) != 0)
      {
        u *= sin;
        v *= cos;
        if(j > 0)
        {
          if(k > 0) value = -u - v;
          else value = v - u;
        }
        else
        {
          if(k > 0) value = u + v;
          else value = u - v;
        }
      }
      else
      {
        u *= cos;
        v *= sin;
        if(j == 0)
        {
          if(k > 0) value = u - v;
          else value = u + v;
        }
        else
        {
          if(k > 0) value = v - u;
          else value = -u - v;
        }
      }
      return value;
    }

    /// <summary>Returns the hyperbolic cosine of the given value.</summary>
    public static FP107 Cosh(FP107 value)
    {
      if(value.IsZero) return One;
      // cosh(x) = (exp(x) + 1/exp(x)) / 2. we don't need to worry about cancelation like in Sinh() because there's no subtraction
      FP107 exp = Exp(value);
      return (exp + 1/exp).ScaleByPowerOfTwo(0.5);
    }

    /// <summary>Divides one double-precision floating-point value by another with high precision and returns the result as an
    /// <see cref="FP107"/> value.
    /// </summary>
    public static FP107 Divide(double a, double b)
    {
      if(double.IsInfinity(a))
      {
        if(b == 0 || double.IsInfinity(b) || double.IsNaN(b)) return NaN;
        else if(Math.Sign(a) == Math.Sign(b)) return PositiveInfinity;
        else return NegativeInfinity;
      }
      else if(double.IsInfinity(b))
      {
        if(double.IsNaN(a)) return NaN;
        else return Zero;
      }

      // NOTE: we can't optimize this multiplication by 1/b because it reduces precision too much
      double q0 = a/b, rlo, rhi = Multiply(b, q0, out rlo), s1, s0 = Subtract(a, rhi, out s1);
      return new FP107(q0, (s1-rlo+s0)/b);
    }

    /// <summary>Divides one value by another, returns the quotient, and stores the remainder in <paramref name="remainder"/>.</summary>
    public static FP107 DivRem(FP107 dividend, FP107 divisor, out FP107 remainder)
    {
      if(divisor.IsInfinity && !dividend.IsInfinity)
      {
        remainder = dividend;
        if(dividend.IsNaN) return NaN; // NaN % anything = NaN
        else if(divisor.Sign() == dividend.Sign()) return Zero; // +finity/+infinity = -finity/-infinity = +0
        else return -Zero; // +finity/-infinity = -finity/+infinity = -0
      }
      FP107 quotient = dividend / divisor;
      remainder = dividend - quotient.Truncate()*divisor;
      return quotient;
    }

    /// <summary>Raises Euler's number (e) to the given power and returns the value as an <see cref="FP107"/>.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/ExpRemarks/node()"/>
    public unsafe static FP107 Exp(FP107 power)
    {
      const double Bounds = 709.782712893384; // this method can only handle values between approximately -709.8 to 709.8
      if(power.hi < -Bounds) return Zero;
      else if(power.hi > Bounds) return PositiveInfinity;
      else if(power.lo == 0)
      {
        // special case integer powers because we can compute them faster and more exactly
        int intPower = (int)power.hi;
        if(intPower == power.hi) return Pow(E, intPower);
      }
      if(power.hi < 1 && power.hi > 0) // if we might be able to optimize this with a root computation...
      {
        FP107 root = 1/power;
        int intRoot = (int)root;
        if(intRoot == root) return Root(E, intRoot);
      }

      // the basic method used is to estimate exp(x) using its Taylor series:
      //   f(x) = e^x = x^0/0! + x^1/1! + x^2/2! + ... = 1 + x + x^2/2! + x^3/3! + ...
      // like all Taylor series, truncating the series provides an approximation to the function near a specific point. typically, this is
      // x = 0. so, the error grows rapidly as x gets further from zero. to avoid having to evaluate far too many terms, we'll perform
      // argument reduction to reduce the argument to a value near zero, and then scale it back up later.
      //
      // we do argument reduction by first noting that exp(kr + m*log(2)) = exp(r)^k * 2^m. this is because x^(a+b) = x^a * x^b and
      // x^(ab) = (x^a)^b, so exp(kr + m*log(2)) = exp(kr) * exp(m*log(2)) = exp(r)^k * exp(log(2))^m = exp(r)^k * 2^m. so, if we choose
      // an integer m so that m*log(2) is closest to x, then |x - m*log(2)| = |kr| <= log(2)/2. choosing 512 for k, we get |r| <= 0.000677,
      // which substantially speeds up the convergence of the Taylor series. (subtracting m*log(2) effectively removes exact powers of two)
      //
      // however, this argument reduction makes the result very small, and if we add one (from the first term in the taylor series), the
      // difference in magnitude between the 1 and the result will cause a loss of precision. so we'll actually compute exp(r)-1 (i.e.
      // dropping the first term in the Taylor series), and then add the 1 back later
      const double k = 512.0, inv_k = 1.0 / k;
      const int lgK = 9;
      double m = Math.Floor(power.hi/Ln2.hi + 0.5);
      FP107 r = (power - Ln2*m).ScaleByPowerOfTwo(inv_k);

      // now that we've scaled the argument, we'll evaluate the Taylor series until it converges to within 'Precision'.
      // InverseFactorials is an array storing 1/3!, 1/4!, etc., which we can multiply by successive powers of r to produce the terms
      FP107 powerOfR = r.Square(); // the numerator in the second term: x^2
      FP107 sum = r + powerOfR.ScaleByPowerOfTwo(0.5); // construct the first and second terms: x + x^2/2
      FP107 term;
      int i = 0;
      do
      {
        powerOfR *= r; // numerator of the next term
        term = powerOfR * InverseFactorials[i];
        sum += term;
      } while(++i < 6 && Math.Abs(term.hi) > Precision/k); // add up to six more terms (for a total of up to eight)

      // now we have exp(r)-1 and we want to compute exp(r)^k. we could do this by adding 1 and raising to the power of k, but if we add
      // one, we'll lose precision (which is why we computed exp(r)-1 in the first place). normally we would raise to the power of k by
      // repeated squaring: y^2^2^2^2^2^2^2^2^2 = y^(2^9) = y^512 = y^k. if we pretend that we add one, square, and then subtract one on
      // each iteration, that's like doing (y+1)^2-1 = y^2+2y+1-1 = y^2+2y. so instead of squaring on each iteration, we can do
      // s = s^2 + 2s. at the end, we'll have exp(r)^k-1
      i = lgK;
      do sum = sum.ScaleByPowerOfTwo(2) + sum.Square(); while(--i != 0);

      // now, we have exp(r)^k - 1, so add one
      sum += 1d;

      // now we want to compute exp(r)^k * 2^m. we can do this by just adjusting the exponent of the result (adding m to the exponent)
      int pow2 = (int)m;
      sum.hi = IEEE754.AdjustExponent(sum.hi, pow2); // this can be done component-wise since it maintains the same exponent separation
      sum.lo = IEEE754.AdjustExponent(sum.lo, pow2);
      return sum;
    }

    /// <summary>Returns the largest integer less than or equal to the given value.</summary>
    public static FP107 Floor(FP107 value)
    {
      // case:       hi:  lo:   val:   f(hi): f(lo):  f(lo)+f(hi):  f(val): adj:
      // zero+zero   0    0     0      0      0       0             0       0
      // int+int     10   2     12     10     2       12            12      0
      // int+zero    10   0     10     10     0       10            10      0
      // int-int     10  -2     8      10    -2       8             8       0
      // int+frac    10   2.3   12.3   10     2       12            12      0
      // int-frac    10  -2.3   7.7    10    -3       7             7       0
      // frac+frac   1.2  0.03  1.23   1      0       1             1       0
      // frac+zero   1.2  0     1.2    1      0       1             1       0
      // frac-frac   1.2 -0.03  1.17   1     -1       0             1       1
      // -int+int   -10   2    -8     -10     2      -8            -8       0
      // -int+zero  -10   0    -10    -10     0      -10           -10      0
      // -int-int   -10  -2    -12    -10    -2      -12           -12      0
      // -int+frac  -10   2.3  -7.7   -10     2      -8            -8       0
      // -int-frac  -10  -2.3  -12.3  -10    -3      -13           -13      0
      // -frac+frac -1.2  0.03 -1.17  -2      0      -2            -2       0
      // -frac+zero -1.2  0    -1.2   -2      0      -2            -2       0
      // -frac-frac -1.2 -0.03 -1.23  -2     -1      -3            -2       1
      // synopsis: add 1 when hi and low have fractions and lo is negative
      double fhi = Math.Floor(value.hi), flo = Math.Floor(value.lo);
      if(fhi != value.hi && flo != value.lo && value.lo < 0) flo++;
      return new FP107(fhi, flo); // renormalize because clo could have changed magnitude substantially relative to chi
    }

    /// <summary>This method is intended to be used with the <see cref="GetComponents"/> method or the "S" <see cref="ToString(string)"/>
    /// format to construct an <see cref="FP107"/> value from literals specified in source code. When used with the "S" format string, if
    /// the string value was "[foo, bar]", you can reconstruct the <see cref="FP107"/> value with source code
    /// <c>FP107.FromComponents(foo, bar)</c>. If the string value was simply "foo", then you can reconstruct the <see cref="FP107"/>
    /// with source code "foo" (except in the case of NaN and infinity, which should be coded as <c>double.NaN</c>, etc).
    /// </summary>
    public static FP107 FromComponents(double first, double second)
    {
      if(double.IsNaN(second) || double.IsInfinity(second)) first = second;
      else if(Math.Abs(second) > Math.Abs(first)) throw new ArgumentException(); // make sure they're in the right order
      return new FP107(first, second);
    }

    /// <summary>Given a double-precision floating-point value, returns an <see cref="FP107"/> value that is the closest approximation to
    /// one of the decimal values to which the original floating-point value was also a closest approximation. In effect, it returns an
    /// <see cref="FP107"/> value that has the same printed value.
    /// </summary>
    /// <remarks>This method is intended as a convenience when inputting constant values in source code, because while
    /// <c>(FP107)0.01 == 0.01</c>, <c>((FP107)0.01).ToString() != 0.01.ToString()</c>. On the other hand,
    /// <c>FP107.FromDecimalValue(0.01) != 0.01</c> but <c>FP107.FromDecimalValue(0.01).ToString() == 0.01.ToString()</c>. This is because
    /// while the double-precision value <c>0.01</c> is the closest 53-bit approximation to the real number 0.01, it is not the closest
    /// 107-bit approximation, so the resulting <see cref="FP107"/> value will not print nicely, and will not be the most accurate value
    /// for calculations.
    /// <note type="caution">This method is very slow - slower than parsing the same number from a string (assuming you have the string
    /// already) - so you should avoid calling it often. One alterative is to use the <see cref="FromComponents"/> method to construct a
    /// value from components. The first component will be equal or approximately equal to the decimal value, so source readability is
    /// somewhat maintained.
    /// </note>
    /// </remarks>
    public static FP107 FromDecimalApproximation(double value)
    {
      // get the binary value and exponent
      ulong mantissa;
      int exponent;
      bool negative;
      if(!IEEE754.Decompose(value, out negative, out exponent, out mantissa)) return new FP107(value); // if it's Infinity/NaN return it

      // get the decimal digits for a 53-bit approximation
      int decimalPlace;
      byte[] digits = GetSignificantDigits(value, exponent, 1-IEEE754.DoubleBiasToInt, new Integer(mantissa), out decimalPlace);

      // parse the digits into a decimal value and exponent, and compute an approximate value
      Integer b10mantissa = Integer.ParseDigits(digits, digits.Length);
      exponent = decimalPlace - digits.Length;
      FP107 approxValue = (FP107)b10mantissa;
      if(exponent > 0) approxValue *= Pow(10, exponent);
      else approxValue /= Pow(10, -exponent);

      // refine the approximate value into the best 107-bit approximation
      approxValue = RefineParsedEstimate(approxValue, exponent, b10mantissa);
      if(negative) approxValue = -approxValue;
      return approxValue;
    }

    /// <summary>Returns the natural logarithm of the given value.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/LogRemarks/node()"/>
    public static FP107 Log(FP107 value)
    {
      // the strategy here is simply to approximate a solution to e^x = a using Newton's method. if f(x) = e^x - a, then f'(x) = e^x and
      // the iteration is x1 = x - (e^x-a)/e^x = x + a/e^x - 1 = x + a*e^-x - 1
      double root = Math.Log(value.hi); // initial guess
      if(double.IsInfinity(root)) return root;
      return root + value*Exp(-root) - 1d; // refine using Newton iteration
    }

    /// <summary>Computes logarithm of the given value using the given base.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/LogRemarks/node()"/>
    public static FP107 Log(FP107 value, FP107 logBase)
    {
      // if value > 0 && logBase > 0 && logBase != 1, return the logarithm. otherwise, there's a mess of special cases:
      // (1) logBase == 1 => NaN (because Log(logBase) is NaN and Log(value) / NaN is NaN)
      // (1) value == +Infinity && logBase > 1 => +Infinity (because Infinity / Finity = Infinity)
      // (1) value == +Infinity && 0 < logBase < 1 => -Infinity
      // (2) value == 0 && logBase > 1 => -Infinity
      // (3) value == 0 && 0 < logBase < 1 => +Infinity
      // (4) value == 1 && (logBase == 0 || logBase == +Infinity) => 0
      // value < 0 => NaN
      // logBase < 0 => NaN
      // value is NaN || logBase is NaN => NaN
      // value != 1 && (logBase == 0 || logBase == +Infinity) => NaN
      if(value.IsPositive && logBase.IsPositive && logBase != 1d) // (1)
      {
        return Log(value) / Log(logBase);
      }
      else if(value.IsZero && !logBase.IsZero && logBase < PositiveInfinity)
      {
        if(logBase > 1d) return NegativeInfinity; // (2)
        else if(logBase < 1d) return PositiveInfinity; // (3)
        else return NaN;
      }
      else if(value == 1d && (logBase.IsZero || logBase.IsPositiveInfinity)) // (4)
      {
        return Zero;
      }
      else
      {
        return NaN;
      }
    }

    /// <summary>Returns the base-10 logarithm of the given value.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/Log10Remarks/node()"/>
    public static FP107 Log10(FP107 value)
    {
      FP107 logarithm = Log(value) / Ln10;
      // if value is an exact power of 10, then hi will be exactly correct and lo may be off by a small error. if these conditions are
      // met, we can zero lo to make the logarithm exact. this introduces some bias that causes some values very close to powers of 10
      // to have the wrong result, but Math.Log10 has the same behavior (e.g. Math.Log10(1e15+2) == 15). however, we don't want to make
      // the error threshold too loose or the bias will be too great. since the error increases with magnitude, we'll select a threshold
      // based on the magnitude. as a result, the method returns exact logarithms for powers of 10 from 10^-300 to 10^300 and 1e29 is the
      // first integer power of ten such that adding or subtracting 1 may return the wrong value
      int intLog = (int)logarithm.hi;
      if(logarithm.hi == intLog)
      {
        double absError = Math.Abs(logarithm.lo);
        if(absError <= 1.7877355160735823E-24) // if the error is within the loosest possible bound for an exact value...
        {
          intLog = Math.Abs(intLog);
          // select an appropriate threshold for the absolute error based on the magnitude so we can reduce the bias for smaller powers
          double threshold;
          if(intLog <= 15) threshold = 6.85195876253379E-31;
          else if(intLog <= 28) threshold = 2.5694845359501716E-30;
          else if(intLog <= 57) threshold = 1.2621774483536189E-29;
          else if(intLog <= 114) threshold = 4.4176210692376661E-29;
          else if(intLog <= 229) threshold = 1.9185484535094615E-28;
          else if(intLog <= 296) threshold = 7.0050848383625848E-28;
          else if(intLog == 297) threshold = 1.6777021030063986E-27;
          else if(intLog == 298) threshold = 1.8068070173182054E-26;
          else if(intLog == 299) threshold = 1.7674270809295725E-25;
          else threshold = 1.7877355160735823E-24;
          if(absError <= threshold) logarithm.lo = 0;
        }
      }
      return logarithm;
    }

    /// <summary>Returns the greater of two <see cref="FP107"/> values. If either value is NaN, the result is undefined.</summary>
    public static FP107 Max(FP107 a, FP107 b)
    {
      if(a >= b) return a;
      else return b;
    }

    /// <summary>Returns the lesser of two <see cref="FP107"/> values. If either value is NaN, the result is undefined.</summary>
    public static FP107 Min(FP107 a, FP107 b)
    {
      if(a <= b) return a;
      else return b;
    }

    /// <summary>Multiplies two double-precision floating-point values with high precision and returns the result as an <see cref="FP107"/>
    /// value.
    /// </summary>
    public static FP107 Multiply(double a, double b)
    {
      double lo;
      return new FP107(Multiply(a, b, out lo), lo);
    }

    /// <summary>Parses a high-precision floating-point value formatted according to the current culture.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/Parse/node()"/>
    /// <include file="documentation.xml" path="/Math/FP107/ParseRemarks/node()"/>
    public static FP107 Parse(string str)
    {
      return Parse(str, NumberStyles.Any, null);
    }

    /// <summary>Parses a high-precision floating-point value formatted using the given provider.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/Parse/node()"/>
    /// <include file="documentation.xml" path="/Math/FP107/ParseRemarks/node()"/>
    public static FP107 Parse(string str, IFormatProvider provider)
    {
      return Parse(str, NumberStyles.Any, provider);
    }

    /// <summary>Parses a high-precision floating-point value formatted using the given provider.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/Parse/node()"/>
    /// <include file="documentation.xml" path="/Math/FP107/ParseRemarks/node()"/>
    public static FP107 Parse(string str, NumberStyles style, IFormatProvider provider)
    {
      if(str == null) throw new ArgumentNullException();
      FP107 value;
      Exception ex;
      if(!TryParse(str, style, provider, out value, out ex)) throw ex;
      return value;
    }

    /// <summary>Returns the given value raised to the given power.</summary>
    /// <remarks>This method cannot always achieve full 107-bit precision, but it is close. The relative error is around 1e-31 or so
    /// through most of the range, but when computing very small values (such as 1e-305), the relative error may be as great as 1e-16
    /// or so. Powers of two are always computed exactly.
    /// </remarks>
    public static FP107 Pow(FP107 value, int power)
    {
      // special-case powers of two since we can compute them quickly
      if(value == 2) return new FP107(IEEE754.AdjustExponent(1, power));
      else if(value.IsNaN) return NaN;

      FP107 result = One, pow2 = value;
      bool recip = false;

      if(power < 0)
      {
        power = -power;
        recip = true;
        if(power < 0) // p == int.MinValue
        {
          power = -(power>>1);
          pow2  = pow2.Square();
        }
      }

      while(true)
      {
        if((power&1) != 0) result *= pow2;
        power >>= 1;
        if(power == 0) break;
        pow2 = pow2.Square();
      }

      if(recip) return 1d / result;
      else return result;
    }

    /// <summary>Returns the given value raised to the given power.</summary>
    /// <remarks>This method cannot always achieve full 107-bit precision, but it is close. The relative error is around 1e-32 or so
    /// through most of the range, but when computing very small values (such as 1e-305), the relative error may be as great as 1e-18
    /// or so. Integer powers of two are always computed exactly.
    /// </remarks>
    public static FP107 Pow(FP107 value, FP107 power)
    {
      // the basic implementation of pow(a,b) is exp(log(a) * b), but there are many special cases.
      // use a faster and more exact method for integral powers. (it can also handle negative values)
      int intPower = (int)power.hi;
      if(power == (int)power.hi) return Pow(value, intPower);

      if(power.hi < 1 && power.hi > 0) // if we might be able to use a root computation (which is faster and generally more accurate)...
      {
        FP107 root = 1/power;
        int intRoot = (int)root;
        if(intRoot == root) return Root(value, intRoot);
      }

      if(value.IsPositive)
      {
        if(value == 1 && !power.IsNaN)
        {
          return One;
        }
        else if(power.IsInfinity)
        {
          if(power.IsNegativeInfinity ^ (value < 1)) return Zero;
          else return PositiveInfinity;
        }
        else
        {
          if(value == E) return Exp(power);
          else return Exp(Log(value) * power);
        }
      }
      else if(value.IsZero)
      {
        if(power.IsPositive) return Zero;
        else if(power.IsNegative) return PositiveInfinity;
        else return NaN; // also, 0^0 = 1, but we know power != 0 because integral powers are handled above
      }
      else // value < 0 or value is NaN
      {
        if(value.IsNegativeInfinity && power.IsNegative) return Zero;
        if(power.IsInfinity && value != -1)
        {
          if(power.IsNegativeInfinity ^ (value > -1)) return Zero;
          else return PositiveInfinity;
        }
        return NaN;
      }
    }

    /// <summary>Generates a random <see cref="FP107"/> value greater than or equal to zero and less than one, using the given random
    /// number generator.
    /// </summary>
    public static FP107 Random(Random.RandomNumberGenerator rng)
    {
      if(rng == null) throw new ArgumentNullException();
      double hi = rng.NextDouble(), lo = rng.NextDouble()*(1.0/(1L<<54)); // scale the second value by 2^-54
      if(rng.NextBoolean()) lo = -lo;
      return new FP107(hi, lo); 
    }

    /// <summary>Returns a root of a value (e.g. square root, cube root, etc).</summary>
    /// <include file="documentation.xml" path="/Math/FP107/RootRemarks/node()"/>
    public static FP107 Root(FP107 value, int root)
    {
      if(root <= 0 || (root&1) == 0 && value.IsNegative) return NaN;
      else if(root == 2) return Sqrt(value);
      else if(root == 1 || value.IsZero) return value;

      // the simple strategy is to use Newton's method to solve x^n = a by taking f(x) = x^n - a and so f'(x) = nx^(n-1) and using the
      // iteration: x1 = x - (x^n-a)/nx^(n-1) = x - ((x^n-a)/x^(n-1))/n = x - (x - a/x^(n-1))/n = x - x(1 - a/x^n)/n = x - x(1 - ax^-n)/n.
      // this works, but Hida et al instead find an approximation to the inverse and then invert that. it's unclear what their reasoning
      // is, but for at least some values it converges slightly faster (resulting in higher accuracy after one iteration), but for other
      // values (particularly very large ones), the accuracy is slightly worse. anyway, i suppose they had good reasons, so we'll do it too
      FP107 abs = value.Abs(); // we have to use the absolute value and negate later to avoid problems with Log()
      double est = Math.Exp(-Math.Log(abs.hi) / root); // compute an initial estimate to the inverse
      FP107 inv = est + est * (1d - abs*Pow(est, root)) / (double)root; // refine it with Newton's method
      return (value.IsNegative ? -1 : 1) / inv; // invert the inverse to get the result
    }

    /// <summary>Rounds the given value to the nearest integer and returns it.</summary>
    public static FP107 Round(FP107 value)
    {
      // the only special case that concerns us is when hi has a fraction of +/-0.5 and
      // lo has a fraction and hi rounds to even when it should have rounded to odd:
      // hi =  1.5, lo < 0 (rounds to 2 rather than 1), hi =  2.5, lo > 0 (rounds to 2 rather than 3),
      // hi = -1.5, lo > 0 (rounds to -2 rather than -1), hi = -2.5, lo < 0 (rounds to -2 rather than -3)
      double rhi = Math.Round(value.hi), rlo = Math.Round(value.lo);
      if(rhi != value.hi) // if hi had a fraction...
      {
        double hi2 = value.hi*2; // check for 0.5 by doubling it and seeing if it's an integer
        if(hi2 == Math.Truncate(hi2)) // and it ended with .5...
        {
          int iv = (int)value.hi;
          if(value.lo > 0) // if lo was positive, we may need to add one to the result
          {
            // if the whole part of hi was even and positive, or odd and negative, add one
            if((iv & 1) == 0 ? value.hi > 0 : value.hi < 0) return new FP107(rhi, rlo+1);
          }
          else if(value.lo < 0) // if lo was negative, we may need to subtract one
          {
            // if the whole part of hi was even and negative, or odd and positive, add one
            if((iv & 1) == 0 ? value.hi < 0 : value.hi > 0) return new FP107(rhi, rlo-1);
          }
        }
      }
      return new FP107(rhi, rlo, false);
    }

    /// <summary>Returns the sine of the given value.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/SinCosRemarks/node()"/>
    public static FP107 Sin(FP107 value)
    {
      if(value.IsZero) return Zero;

      // the strategy here is to reduce the value to the form x = s + j*(pi/2) + k*(pi/16) where |s| <= pi/32, |j| <= 2, and |k| <= 4.
      // then we use a Taylor series to compute sin(s) and cos(s). (the argument reduction to within pi/32 of zero significantly speeds up
      // the convergence of the Taylor series, which is centered around zero.)
      //
      // we then combine the pieces using the formulas: sin(a + b) = sin(a)*cos(b) + cos(a)*sin(b) [1] and
      // cos(a + b) = cos(a)*cos(b) - sin(a)*sin(b) [2]. in this case, we have sin() and cos() of both s and k*pi/16 (the latter from a
      // precomputed table). the j*(pi/2) term is effectively a phase shift: sin(a + pi/2) = cos(a). putting it all together, we
      // use the value of j to select from formulas [1] and [2], which determine whether to multiply sin with cos or sin with sin, etc.
      // then we use the value of k to determine the signs of the terms as we add them
      int j, k;
      if(!ReduceSinCosArgument(ref value, out j, out k)) return NaN;

      // if k == 0, then the problem reduces to sin(s + 0) = sin(s)
      if(k == 0)
      {
        switch(j)
        {
          case 0: return SinReduced(value);
          case 1: return CosReduced(value);
          case -1: return -CosReduced(value);
          default: return -SinReduced(value);
        }
      }

      // otherwise, we have sin(s + t), which we solve as describe above
      int absk = Math.Abs(k);
      FP107 u = CosTable[absk-1], v = SinTable[absk-1], sin, cos; // u = cos(abs(k) * pi/16), v = sin(abs(k) * pi/16)
      SinCosReduced(value, out sin, out cos);
      if((j & 1) != 0)
      {
        u *= cos;
        v *= sin;
        if(j > 0)
        {
          if(k > 0) value = u - v;
          else value = u + v;
        }
        else
        {
          if(k > 0) value = v - u;
          else value = -u - v;
        }
      }
      else
      {
        u *= sin;
        v *= cos;
        if(j == 0)
        {
          if(k > 0) value = u + v;
          else value = u - v;
        }
        else
        {
          if(k > 0) value = -u - v;
          else value = v - u;
        }
      }
      return value;
    }

    /// <summary>Computes the sine and cosine of the given value.</summary>
    /// <include file="documentation.xml" path="/Math/FP107/SinCosRemarks/node()"/>
    public static void SinCos(FP107 value, out FP107 sin, out FP107 cos)
    {
      // see Sin(FP107) or Cos(FP107) for a description of the strategy here
      if(value.IsZero)
      {
        sin = Zero;
        cos = One;
      }
      else
      {
        int j, k;
        if(!ReduceSinCosArgument(ref value, out j, out k))
        {
          sin = NaN;
          cos = NaN;
        }
        else
        {
          FP107 s, c;
          SinCosReduced(value, out s, out c);
          if(k != 0)
          {
            int absk = Math.Abs(k);
            FP107 u = CosTable[absk-1], v = SinTable[absk-1], t = s;
            if(k > 0)
            {
              s = u*s + v*c;
              c = u*c - v*t;
            }
            else
            {
              s = u*s - v*c;
              c = u*c + v*t;
            }
          }

          switch(j)
          {
            case -1: sin = -c; cos = s; break;
            case 0: sin = s; cos = c; break;
            case 1: sin = c; cos = -s; break;
            default: sin = -s; cos = -c; break;
          }
        }
      }
    }

    /// <summary>Returns the hyperbolic sine of the given value.</summary>
    public static FP107 Sinh(FP107 value)
    {
      // if the value is sufficiently large, we can use the formula sinh(x) = (exp(x) - 1/exp(x)) / 2. but when the value is small, the
      // formula causes too much cancelation, reducing precision. so we'll use a Taylor series when the value is small. we can't use the
      // Taylor series all the time, though, because it would perform too poorly without argument reduction of some kind
      if(Math.Abs(value.hi) > 0.05)
      {
        FP107 exp = Exp(value);
        return (exp - 1/exp).ScaleByPowerOfTwo(0.5);
      }
      else if(value.IsZero)
      {
        return value;
      }
      else
      {
        // the Taylor series for sinh(x) is x^1/1! + x^3/3! + x^5/5! + ...
        FP107 sum = value, term = value, sqr = term.Square(); // set sum to the first term and sqr to x^2
        double factorial = 1.0, threshold = Math.Abs(value.hi) * Precision;

        // on each iteration, term equals the previous term and factorial equals the number whose factorial is taken in the denominator.
        // abbreviating those to t and n, we have t = x^n/n!. the next iteration is x^(n+2)/(n+2)!. we can get one from the other via
        // (t * x^2) / ((n+1)*(n+2)) = (x^n/n! * x^2) / ((n+1)*(n+2)) = (x^n * x^2) / (n!*(n+1)*(n+2)) = x^(n+2)/(n+2)!
        do
        {
          factorial += 2;
          term = term * sqr / ((factorial-1) * factorial); // compute the new term by the old one, as above
          sum += term;
        } while(Math.Abs(term.hi) > threshold);
        return sum;
      }
    }

    /// <summary>Returns the square root of the given value.</summary>
    public static FP107 Sqrt(FP107 value)
    {
      if(value.IsPositive)
      {
        // the usual method is to use Newton's method to solve x^2 = a. we represent it as a function with a zero at the answer:
        // f(x) = x^2 - a, which has a derivative f'(x) = 2x. and then Newton's method is x1 = x0 - f(x0)/f'(x0), yielding an iteration of
        // x1 ~= x0 - (x0^2-a)/2x0. thus if x ~= sqrt(a), then x - (x^2-a)/2x is an approximation that's twice as good
        //
        // but the full-precision division is expensive; Karp's trick avoids it by instead approximating 1/sqrt(a) and multiplying by a.
        // it uses f(x) = 1/x^2 - a. then f'(x) = -2/x^3 and the iteration is x1 = x - (1/x^2-a)/(-2/x^3). although this looks worse, we
        // can simplify: x + (1/x^2-a)*(x^3/2) = x + x^3(1/x^2-a)/2 = x + (x - ax^3)/2 = x + x(1 - ax^2)/2 = x + (1 - ax^2)(x/2). since we
        // need to multiply by a at the end (in our case, after one iteration), we have
        // sqrt(a) ~= a(x + (1 - ax^2)(x/2)) = ax + a(1 - ax^2)(x/2) = ax + (a - (ax)^2)(x/2).
        //
        // then the question with Karp's trick is how much precision we need with each operation. for this, i refer you to Karp's analysis
        // in High-Precision Division and Square Root (Karp & Markstein, 1997)
        double x = 1 / Math.Sqrt(value.hi), ax = value.hi * x;
        if(x == 0) return PositiveInfinity; // if the value was PositiveInfinity, x becomes 0. sqrt(+Infinity) = +Infinity
        return Add(ax, (value - new FP107(ax).Square()).hi * (x * 0.5));
      }
      else
      {
        if(value.IsZero) return Zero;
        else return NaN;
      }
    }

    /// <summary>Subtracts one double-precision floating-point value from another with high precision and returns the result as an
    /// <see cref="FP107"/> value.
    /// </summary>
    public static FP107 Subtract(double a, double b)
    {
      double lo;
      return new FP107(Subtract(a, b, out lo), lo, true);
    }

    /// <summary>Returns the tangent of the given value.</summary>
    public static FP107 Tan(FP107 value)
    {
      FP107 sin, cos;
      SinCos(value, out sin, out cos);
      return sin / cos;
    }

    /// <summary>Returns the hyperbolic tangent of the given value.</summary>
    public static FP107 Tanh(FP107 value)
    {
      if(value.IsZero) return value;
      if(value.IsInfinity)
      {
        if(value.IsPositiveInfinity) return One;
        else return MinusOne;
      }

      FP107 exp = Exp(value), inv = 1/exp;
      return (exp - inv) / (exp + inv);
    }

    /// <summary>Returns the value, truncated towards zero.</summary>
    public FP107 Truncate(FP107 value)
    {
      double thi = Math.Truncate(value.hi), tlo = Math.Truncate(value.lo);
      // if hi wasn't an integer or lo was, then any fraction in lo can't affect the overall result because it would
      // be dominated by the fraction in hi. but if hi was an integer and lo wasn't, it may change the result
      if(thi == value.hi && tlo != value.lo) // if hi was an integer and lo wasn't (and the value isn't NaN)...
      {
        if(value.hi > 0) // if hi was a positive integer and lo had a negative fraction, the overall result should be one less.
        {                // (e.g. hi = thi = 120, lo = -3.4, tlo = -3, hi + lo = 116.6, truncate(116.6) == 116, but thi + tlo = 117)
          if(value.lo < 0) return new FP107(thi, tlo-1); // subtract from tlo rather than thi because the fact the fraction guarantees it
        }                                                // will change. also, renormalize because thi may be able to incorporate the 1
        else // if hi was a negative integer and lo had a positive fraction, the overall result should be one greater.
        {    // (e.g. hi = thi = -120, lo = 3.4, tlo = 3, hi + lo = -116.6, truncate(-116.6) == -116, but thi + tlo = -117)
          // it's impossible for hi to be zero because it can only be zero if lo is zero in which case we wouldn't be here
          if(value.lo > 0) return new FP107(thi, tlo+1);
        }
      }
      return new FP107(thi, tlo, false);
    }

    /// <summary>Attempts to parse a high-precision floating-point value formatted according to the current culture and returns true if the
    /// parse was successful.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/FP107/ParseRemarks/node()"/>
    public static bool TryParse(string str, out FP107 value)
    {
      return TryParse(str, NumberStyles.Any, null, out value);
    }

    /// <summary>Attempts to parse a high-precision floating-point value formatted according to the given provider and returns true if the
    /// parse was successful.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/FP107/ParseRemarks/node()"/>
    public static bool TryParse(string str, IFormatProvider provider, out FP107 value)
    {
      return TryParse(str, NumberStyles.Any, provider, out value);
    }

    /// <summary>Attempts to parse a high-precision floating-point value formatted according to the given provider and returns true if the
    /// parse was successful.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/FP107/ParseRemarks/node()"/>
    public static bool TryParse(string str, NumberStyles style, IFormatProvider provider, out FP107 value)
    {
      Exception ex;
      return TryParse(str, style, provider, out value, out ex);
    }

    /// <summary>An <see cref="FP107"/> value equal to zero.</summary>
    public static readonly FP107 Zero = new FP107();
    /// <summary>An <see cref="FP107"/> value equal to one.</summary>
    public static readonly FP107 One = new FP107(1, 0, false);
    /// <summary>An <see cref="FP107"/> value equal to negative one.</summary>
    public static readonly FP107 MinusOne = new FP107(-1, 0, false);
    /// <summary>The largest, finite, positive <see cref="FP107"/> value that can be represented in practice.</summary>
    /// <remarks>While it's theoretically possible to represent a slightly larger value, it's difficult or impossible to construct with
    /// any arithmetic operation, and nearly any operation upon it, even ones that should be "safe", results in overflow to infinity.
    /// Therefore, this is the largest value that can be encountered in practice.
    /// </remarks>
    public static readonly FP107 MaxValue = new FP107(double.MaxValue, double.MaxValue/(1L<<54), false);
    /// <summary>The largest, finite, negative <see cref="FP107"/> value that can be represented in practice.</summary>
    /// <remarks>While it's theoretically possible to represent a slightly larger value, it's difficult or impossible to construct with
    /// any arithmetic operation, and nearly any operation upon it, even ones that should be "safe", results in overflow to infinity.
    /// Therefore, this is the largest value that can be encountered in practice.
    /// </remarks>
    public static readonly FP107 MinValue = new FP107(double.MinValue, double.MinValue/(1L<<54), false);
    /// <summary>An <see cref="FP107"/> value representing positive infinity.</summary>
    /// <remarks>To check if a value is infinite, use <see cref="IsInfinity"/> or <see cref="IsPositiveInfinity"/> rather than comparing
    /// against this value.
    /// </remarks>
    public static readonly FP107 PositiveInfinity = new FP107(double.PositiveInfinity, 0, false);
    /// <summary>An <see cref="FP107"/> value representing negative infinity.</summary>
    /// <remarks>To check if a value is infinite, use <see cref="IsInfinity"/> or <see cref="IsNegativeInfinity"/> rather than comparing
    /// against this value.
    /// </remarks>
    public static readonly FP107 NegativeInfinity = new FP107(double.NegativeInfinity, 0, false);
    /// <summary>An <see cref="FP107"/> that is not a number (i.e. NaN).</summary>
    /// <remarks>To check if a value is NaN, use <see cref="IsNaN"/> rather than comparing against this value.</remarks>
    public static readonly FP107 NaN = new FP107(double.NaN, double.NaN, false);
    /// <summary>An <see cref="FP107"/> approximating Euler's number (e).</summary>
    public static readonly FP107 E = new FP107(2.7182818284590451, 1.4456468917292502E-16, false);
    /// <summary>An <see cref="FP107"/> approximating the golden ratio, equal to (1+sqrt(5))/2.</summary>
    public static readonly FP107 GoldenRatio = new FP107(1.6180339887498949, -2.7160576018412528E-17, false);
    /// <summary>An <see cref="FP107"/> approximating the natural logarithm of 2.</summary>
    public static readonly FP107 Ln2 = new FP107(0.69314718055994529, 2.3190468138463E-17, false);
    /// <summary>An <see cref="FP107"/> approximating the natural logarithm of 10.</summary>
    public static readonly FP107 Ln10 = new FP107(2.3025850929940459, -2.1707562233822494E-16, false);
    /// <summary>An <see cref="FP107"/> approximating pi.</summary>
    public static readonly FP107 Pi = new FP107(3.1415926535897931, 1.2246467991473532E-16, false);
    /// <summary>An <see cref="FP107"/> approximating pi/2.</summary>
    public static readonly FP107 PiOverTwo = Pi/2;
    /// <summary>An <see cref="FP107"/> approximating 2*pi.</summary>
    public static readonly FP107 TwoPi = Pi*2;

    const double Precision = 4.9303806576313237838E-32; // Precision = 2^-104 (similar to IEEE754.DoublePrecision but not quite the same)

    #region IComparable Members
    int IComparable.CompareTo(object obj)
    {
      if(!(obj is FP107))
      {
        throw new ArgumentException("Expected a " + GetType().FullName + " value but received a " +
                                    (obj == null ? "null" : obj.GetType().FullName) + " value.");
      }
      return CompareTo((FP107)obj);
    }
    #endregion

    #region IConvertible Members
    TypeCode IConvertible.GetTypeCode()
    {
      return TypeCode.Object;
    }

    bool IConvertible.ToBoolean(IFormatProvider provider)
    {
      return !IsZero;
    }

    byte IConvertible.ToByte(IFormatProvider provider)
    {
      return checked((byte)hi);
    }

    char IConvertible.ToChar(IFormatProvider provider)
    {
      return checked((char)hi);
    }

    DateTime IConvertible.ToDateTime(IFormatProvider provider)
    {
      throw new InvalidCastException("Cannot convert from FP107 to DateTime.");
    }

    decimal IConvertible.ToDecimal(IFormatProvider provider)
    {
      return (decimal)this;
    }

    double IConvertible.ToDouble(IFormatProvider provider)
    {
      return hi;
    }

    short IConvertible.ToInt16(IFormatProvider provider)
    {
      return checked((short)hi);
    }

    int IConvertible.ToInt32(IFormatProvider provider)
    {
      return checked((int)hi);
    }

    long IConvertible.ToInt64(IFormatProvider provider)
    {
      return checked((long)hi + (long)lo);
    }

    sbyte IConvertible.ToSByte(IFormatProvider provider)
    {
      return checked((sbyte)hi);
    }

    float IConvertible.ToSingle(IFormatProvider provider)
    {
      return (float)hi;
    }

    object IConvertible.ToType(Type conversionType, IFormatProvider provider)
    {
      if(conversionType == typeof(Rational)) return new Rational(this);
      else if(conversionType == typeof(Integer)) return new Integer(this);
      else return MathHelpers.DefaultConvertToType(this, conversionType, provider);
    }

    ushort IConvertible.ToUInt16(IFormatProvider provider)
    {
      return checked((ushort)hi);
    }

    uint IConvertible.ToUInt32(IFormatProvider provider)
    {
      return checked((uint)hi);
    }

    ulong IConvertible.ToUInt64(IFormatProvider provider)
    {
      return checked((ulong)hi + (ulong)lo);
    }
    #endregion

    /// <summary>Returns the cosine of the value, assuming its magnitude is no greater than Pi/32.</summary>
    static FP107 CosReduced(FP107 value)
    {
      if(value.IsZero) return One;

      FP107 x = -value.Square(), r = x, sum = x.ScaleByPowerOfTwo(0.5) + 1, term;
      double threshold = Math.Abs((double)value) * (Precision/2);
      int i = 1;
      do
      {
        r *= x;
        term = r * InverseFactorials[i];
        sum += term;
        i += 2;
      } while(i < InverseFactorials.Length && Math.Abs((double)term) > threshold);
      return sum;
    }

    /// <summary>Returns this <see cref="FP107"/> value with each component scaled directly by the given factor. This is most useful to
    /// quickly scale by powers of two.
    /// </summary>
    FP107 ScaleByPowerOfTwo(double factor)
    {
      return new FP107(hi*factor, lo*factor, false);
    }

    double hi, lo;

    /// <summary>Adds two numbers with 107-bit precision, returning the high component and placing the low component in
    /// <paramref name="e"/>.
    /// </summary>
    static double Add(double a, double b, out double e)
    {
      double sum = a+b, v = sum-a;
      e = a - (sum-v) + (b-v);
      return sum;
    }

    /// <summary>Given a base-10 number represented as an exponent and mantissa, returns the closest 107-bit binary floating-point value.</summary>
    static FP107 FindNearestFloat(int exponent, Integer mantissa)
    {
      // this is AlgorithmM from "How to Read Floating Point Numbers Accurately" (Clinger, 1990), converted to an iterative form, and
      // limited by min and max exponents
      const int MantissaSize = 107;
      const int MinExponent = 1-IEEE754.DoubleBias-MantissaSize, MaxExponent = (1<<IEEE754.DoubleExponentBits)-2-IEEE754.DoubleBiasToInt;
      // the value is represented by the rational n/d, which we rescale until n/d is within the range for a valid IEEE floating-point
      // mantissa
      Integer n, d, nmax = Integer.Pow(2, MantissaSize), hnmax = Integer.Pow(2, MantissaSize-1);
      if(exponent < 0)
      {
        n = mantissa.Clone();
        d = Integer.Pow(10, -exponent);
      }
      else
      {
        n = mantissa * Integer.Pow(10, exponent);
        d = Integer.One;
        if(exponent == 0) n = n.Clone(); // if the exponent was 0, then n*10^0 may share storage with mantissa
      }

      int b2exponent = 0;
      while(true)
      {
        Integer r, b2mantissa = Integer.DivRem(n, d, out r);
        if(b2mantissa < nmax && (b2exponent == MinExponent || b2mantissa >= hnmax)) // if the base-2 mantissa is in the valid range...
        {
          Integer half = d - r; // round it to even
          if(r > half || !b2mantissa.IsEven && r == half) GetSuccessor(hnmax, nmax, ref b2exponent, ref b2mantissa);
          return Compose(b2exponent, b2mantissa, true, false); // and create an FP107 from it
        }
        else if(b2mantissa < hnmax) // otherwise, if the base-2 mantissa is too small (i.e. it's not normalized)...
        {
          n.UnsafeLeftShift(1); // double n/d (roughly doubling the mantissa) and decrement the base-2 exponent to keep the value the same
          if(--b2exponent < MinExponent) return FP107.Zero; // if the exponent went out of bounds, the value is too small to be represented
        }
        else // otherwise, if the base-2 mantissa is too large...
        {
          d.UnsafeLeftShift(1); // halve n/d (roughly halving the mantissa) and increment the base-2 exponent to keep the value the same
          if(++b2exponent > MaxExponent) return FP107.PositiveInfinity; // if the exponent went out of bounds, the value is too large
        }
      }
    }

    static byte[] GetSignificantDigits(double approxValue, int exponent, int minExponent, Integer mantissa, out int decimalPlace)
    {
      if(double.IsNaN(approxValue) || double.IsInfinity(approxValue)) throw new ArgumentException();

      // the Burger & Dybvig algorithm returns an unintuitive decimal place for zero values, so special case them
      if(mantissa.IsZero)
      {
        decimalPlace = 1;
        return new byte[1];
      }

      /*
       * float = u/v * 2^k : 2^52 <= u/v < 2^53
       * q = quotient(u/v)
       * r = remainder(u/v)
       * vmr = v - r
       */

      // the value of the number is v. given v, we need the previous and next floating-point numbers, v- and v+.
      // consider a 4-bit floating-point number, with two exponent bits (unbiased) and two mantissa bits (plus a hidden bit),
      // where a raw exponent of 0 represents denormalized numbers (as in IEEE) with an exponent of 1.
      // 00 00 = 0*2^1 = 0
      // 00 01 = 1*2^1 = 2
      // 00 10 = 2*2^1 = 4
      // 00 11 = 3*2^1 = 6
      // 01 00 = 4*2^1 = 8
      // 01 01 = 5*2^1 = 10
      // 01 10 = 6*2^1 = 12
      // 01 11 = 7*2^1 = 14
      // 10 00 = 4*2^2 = 16
      // 10 01 = 5*2^2 = 20
      // 10 10 = 6*2^2 = 24
      // 10 11 = 7*2^2 = 28
      // 11 00 = 4*2^3 = 32
      // 11 01 = 5*2^3 = 40
      // 11 10 = 6*2^3 = 48
      // 11 11 = 7*2^3 = 56
      //
      // for brevity: m = mantissa, p = mantissaSize, e = exponent, e_min = minimum representable exponent
      // v = f * 2^e
      // v- = { v - 2^e     if e = e_min or m != 2^(p-1),
      //        v - 2^(e-1) if e > e_min and m = 2^(p-1) }
      // v+ = v + 2^e
      //
      // the equation for v-, equivalent to v- = (m-1)*2^e, works for most numbers. but consider what happens when e > e_min and
      // m = 2^(p-1). in the example, e_min = 1 and p = 3 (including the hidden bit), so this occurs when the exponent bits are 10 or 11 and
      // the full mantissa equals 2^2 = 4, i.e. when the mantissa bits are 00 (with a hidden 1 bit). that is, it occurs on the lines
      // "10 00 = 4*2^2 = 16" and "11 00 = 4*2^3 = 32". consider the former. in that case, v = 16 and 2^e = 4, so v - 2^e = 12. but that's
      // not the predecessor, since there exists a value 14. in those cases, the formula for v- must be v- = v - 2^(e-1).
      //
      // the equation for v+ works for all numbers and is equivalent to v+ = (m+1) * 2^e.
      // 
      // there is an edge case for v- when v is zero and for v+ when v is the maximum representable value. in those cases, v- and v+ should
      // be -infinity and +infinity, respectively.
      //
      // the point of finding v- and v+ is that all real numbers between (v + v-)/2 and (v + v+)/2 exclusive round to v, so when we've
      // output enough digits that the number output falls between v- and v+, we can stop.
      //
      // then we can start by scaling v so that it's of the form 0.dddd... (i.e. scale v so it lies in [0.1,1)). if we have q = 0.dddd...
      // then the first digit equals floor(q*10). q can be multiplied by 10 and the process repeated to extract the rest of the digits.
      //
      // the basic algorithm is:
      // 1. determine v- and v+. set low = (v + v-)/2 and high = (v + v+)/2.
      // 2. scale the number to the form 0.dddd... and assign it to q.
      //    a. find the smallest integer k such that high <= 10^k. i.e. k = ceiling(log10 high)
      //    b. set q = v / 10^k
      // 3. extract digit, d = floor(q*10). set q = q*10 - d.
      // 4. increment a count of digits extracted, n = n+1.
      // 5. see if we're done:
      //    a. if 0.dd... * 10^k (considering only the first n digits) > low, i.e. if the number output so far would round up to v,
      //       then we're done.
      //    b. if 0.dd... * 10^k (considering only the first n digits, and with the last digit incremented) < high, i.e. if the number
      //       output so far would round down to v even if the next digit is rounded up, then we're done.
      //    c. if both termination conditions are true, see which comparand (of 0.dd... and 0.dd... incremented) is closer to v and
      //       use that as the final output. we can do this simply by incrementing the most recently extracted digit if necessary, since
      //       the increment cannot cause carry. (if it would carry, then the algorithm would have terminated at a previous step.)
      // 6. if we're not done, repeat from 3.
      //
      // the first digit produced by this method will be non-zero if the number is non-zero. (otherwise, k would not be the minimum integer
      // such that high <= 10^k.)
      //
      // the goal of the algorithm is to output digits that unambiguously represent the given floating-point value, and no more digits than
      // necessary. note that 'low' is equidistant between v and the floating-point number before 'low', and the same is true for 'high'
      // and the floating-point number after 'high'. if we don't know how the output will be used, we can't terminate when the output
      // exactly equals low or high. but if the tie-breaking rule used by any floating-point input routines that may read the output back
      // is known, we may be able to output fewer digits by using >= and <= rather than > and < in the termination conditions. if the input
      // routine uses IEEE unbiased rounding, it will break ties by rounding toward even mantissas. so if we assume that the any input
      // routine uses IEEE unbiased rounding - ours does - and if the mantissa is even, we can use >= and <= in our termination conditions.
      // otherwise, we must use > and <. this is tracked by evenMantissa below.
      //
      // in the actual implementation, since we don't have real numbers in the computer, but we can represent v as an arbitrary precision
      // rational. we'll also use the values m- and m+ where m- = (v - v-)/2 and m+ = (v+ - v)/2. i.e., m- and m+ represent the distances
      // from v to the previous and next numbers. to simplify things further, we can find a common denominator for v, m-, and m+,
      // representing v = n/d with an explicit numerator and denominator, and m- and m+ with just their numerators. i.e. m- = d(v - v-)/2
      // and m+ = d(v+ - v)/2. this converts some expensive arbitrary precision rational computations into somewhat less expensive arbitary
      // precision integer computations.
      Integer n, d, mMinus, mPlus;
      bool evenMantissa = mantissa.IsEven; // can we assume an input routine would round ties to the correct mantissa?
      if(exponent >= 0) // if the number is >= 1...
      {
        Integer power = Integer.Pow(2, exponent); // power = 2^e. e could be 0, so power could equal 1.
        if(mantissa != Integer.Pow(2, mantissa.BitLength-1)) // m != 2^(p-1), so v- = v - 2^e and v+ = v + 2^e...
        {
          n = (power*mantissa) << 1; // n = 2(m * 2^e)
          d = 2;                     // d = 2. thus n/d = m * 2^e = v
          mMinus = mPlus = power;    // m- = m+ = (v+ - v)/2 = (v + 2^e - v)/2 = 2^e/2 = power/d
        }
        else // m = 2^(p-1), so v- = v - 2^(e-1) and v+ = v + 2^e
        {
          n = (power*mantissa) << 2; // n = 4(m * 2^e)
          d = 4;                     // d = 4. this n/d = m * 2^e = v
          mMinus = power;            // m- = (v - v-)/2 = (v - (v - 2^(e-1)))/2 = 2^(e-1)/2 = 2^e/4 = power/d
          mPlus  = power<<1;         // m+ = (v+ - v)/2 = (v + 2^e - v)/2 = 2^e/2 = 2*2^e/4 = 2*power/d
        }
      }
      else // otherwise, if the number is between 0 and 1 exclusive...
      {
        if(mantissa != Integer.Pow(2, mantissa.BitLength-1) || exponent == minExponent) // if v- = v - 2^e and v+ = v + 2^e...
        {
          n = mantissa << 1;              // n = 2m
          d = Integer.Pow(2, 1-exponent); // d = 2^(1-e) = 1/2^(e-1). thus n/d = 2m * 2^(e-1) = m * 2^e = v
          mMinus = mPlus = Integer.One;   // m- = m+ = (v+ - v)/2 = (v + 2^e - v)/2 = 2^e/2 = 2^(e-1) = 1 / (1/2^(e-1)) = 1/d
        }
        else // m = 2^(p-1), so v- = v - 2^(e-1) and v+ = v + 2^e
        {
          n = mantissa << 2;              // n = 4m
          d = Integer.Pow(2, 2-exponent); // d = 2^(2-e) = 1/2^(e-2). this n/d = 4m * 2^(e-2) = m * 2^e = v
          mMinus = Integer.One;           // m- = (v - v-)/2 = (v - (v - 2^(e-1)))/2 = 2^(e-1)/2 = 2^(e-2) = 1 / (1/2^(e-2)) = 1/d
          mPlus  = 2;                     // m+ = (v+ - v)/2 = (v + 2^e - v)/2 = 2^e/2 = 2*2^(e-2) = 2 / (1/2^(e-2)) = 2/d
        }
      }

      // now that we have initial values for n, d, m-, and m+, we need to scale them so that n/d lies within [0.1,1)
      Scale(approxValue, ref n, ref d, ref mMinus, ref mPlus, evenMantissa, out decimalPlace);

      // finally, we have the number 0.dddd..., so we can go ahead and extract the digits
      return GetSignificantDigits(n, d, mMinus, mPlus, evenMantissa);
    }

    static byte[] GetSignificantDigits(Integer n, Integer d, Integer mMinus, Integer mPlus, bool boundaryOk)
    {
      // in this method, the value v is represented by the rational n/d, which has been scaled to lie within [0.1,1). this way, the first
      // significant digit can be extracted using truncate(10n/d). the remainder of the division contains the remaining digits.
      List<byte> digits = new List<byte>(34); // we have over 32 digits of precision, plus one just in case it's needed for rounding
      bool wouldRoundUp, wouldRoundDown; // whether the number output so far would round to the correct value (in the given direction)
      if(mMinus.BitLength == mPlus.BitLength) mPlus = mPlus.Clone(); // avoid sharing data between mMinus and mPlus
      do
      {
        // extract the first digit and assign the remainer back to n
        n.UnsafeMultiply(10u);
        mMinus.UnsafeMultiply(10u); // since we scaled n up by 10, we should also scale m- and m+
        mPlus.UnsafeMultiply(10u);
        byte digit = (byte)Integer.DivRem(n, d, out n);
        // now check the termination conditions
        Integer rNext = n + mPlus;
        wouldRoundUp   = boundaryOk ? n <= mMinus : n < mMinus; // if the value output would round up to the correct value
        wouldRoundDown = boundaryOk ? rNext >= d : rNext > d;   // if the value output would round down to the correct value
        // if we're not done, or if we're done because the number would round up to the correct value, output the digit.
        // if we're done because the number would round down to the correct digit even if the current digit is incremented (due, for
        // instance to the digits after that causing it to round up), then output the digit plus one. if both termination
        // conditions are met, choose whichever result is closest to the correct value. if they're equidistant, don't increment.
        if(!wouldRoundUp) digits.Add(!wouldRoundDown ? digit : (byte)(digit+1));
        else digits.Add(!wouldRoundDown || (n<<1) <= d ? digit : (byte)(digit+1));
      } while(!(wouldRoundUp | wouldRoundDown));
      return digits.ToArray();
    }

    /// <summary>Multiplies two numbers with 107-bit precision, returning the high component and placing the low component in
    /// <paramref name="e"/>.
    /// </summary>
    static double Multiply(double a, double b, out double e)
    {
      double product = a*b, alo, blo, ahi = Split(a, out alo), bhi = Split(b, out blo);
      e = ahi*bhi - product + ahi*blo + bhi*alo + alo*blo;
      // reduce precision if overflow occurs in ahi*bhi but not a*b because reduced precision with large numbers is better than NaNs
      if(double.IsInfinity(e) && !double.IsInfinity(product)) e = 0;
      return product;
    }

    static char ParseFormatString(string format, out int desiredPrecision, out bool capitalize)
    {
      char formatType;
      bool parsed = NumberFormat.ParseFormatString(format, 'G', out formatType, out desiredPrecision, out capitalize);
      if(!parsed && formatType != 'S') throw new FormatException("Unsupported format string: " + format);
      return formatType;
    }

    /// <summary>Returns an <see cref="FP107"/> value constructed from a base-2 mantissa and exponent.</summary>
    static FP107 Compose(int exponent, Integer mantissa, bool allowOutOfRange, bool exact)
    {
      const int MantissaSize = IEEE754.DoubleMantissaBits+1; // the size of the mantissas for hi and lo
      const int MinExponent = 1-IEEE754.DoubleBiasToInt, MaxExponent = (1<<IEEE754.DoubleExponentBits)-2-IEEE754.DoubleBiasToInt;
      if(mantissa.IsZero) exponent = 0; // if the mantissa is zero, make sure any exponent is allowed

      if(exponent > MaxExponent)
      {
        if(allowOutOfRange) return FP107.PositiveInfinity;
        throw new ArgumentOutOfRangeException("The exponent is out of range.");
      }

      int bitLength = mantissa.BitLength, maxLength = Math.Min(MantissaSize, exponent - (MinExponent-bitLength));
      if(maxLength <= 0)
      {
        if(allowOutOfRange) return FP107.Zero; // if the exponent would have to be reduced below the minimum, return zero
        throw new ArgumentOutOfRangeException("The exponent is out of range.");
      }

      // construct the mantissa for the high value
      Integer hiMantissa = mantissa; // start out with the whole mantissa
      double lo = 0;
      int hiExponent = exponent;
      bool loNegative = false;
      if(bitLength > maxLength) // if it has extra bits (it usually does)...
      {
        hiMantissa >>= bitLength - maxLength; // take only the first 'maxLength' bits
        hiExponent  += bitLength - maxLength;
        if(hiExponent > MaxExponent)
        {
          if(allowOutOfRange) return FP107.PositiveInfinity;
          throw new ArgumentOutOfRangeException("The exponent is out of range.");
        }

        // we want to round up if the matissa is odd and the next bit is set and the mantissa isn't all 1's
        if(!hiMantissa.IsEven && mantissa.GetBit(bitLength-(maxLength+1)) && hiMantissa != Integer.Pow(2, maxLength)-1)
        {
          // increment the hi mantissa to round up. after doing so it becomes greater than the original mantissa, so rather than
          // subtracting the high mantissa from the full mantissa, we need to subtract the full mantissa from the high mantissa.
          // because the remaining mantissa is effectively negative, the low value must become negative
          hiMantissa++;
          mantissa    = (hiMantissa << (bitLength - maxLength)) - mantissa;
          loNegative  = true;
        }
        else // we don't need to round...
        {
          mantissa -= hiMantissa << (bitLength - maxLength); // subtract the high mantissa from the full mantissa to get the lower bits
        }

        // if we had enough bits to fill the high mantissa and there are still some bits left, set the low mantissa
        if(maxLength >= MantissaSize && !mantissa.IsZero)
        {
          Integer loMantissa = mantissa; // start out with the whole remaining mantissa
          int loBitLength = mantissa.BitLength;
          maxLength = Math.Min(MantissaSize, exponent - (MinExponent-loBitLength));
          if(loBitLength > maxLength) // if the mantissa has more bits than we can fit, perhaps because the exponent hit the minimum...
          {
            if(exact) throw new ArgumentOutOfRangeException();
            if(maxLength > 0) // if we're allowed to put any bits in the low mantissa...
            {
              loMantissa >>= loBitLength - maxLength; // chop the low mantissa to the length we're allowed
              exponent    += loBitLength - maxLength;
              if(!loMantissa.IsEven && mantissa.GetBit(loBitLength-(maxLength+1)) && loMantissa != Integer.Pow(2, maxLength)-1)
              {
                loMantissa++; // round to even if needed and the mantissa isn't at the maximum value
              }
            }
            loBitLength = maxLength;
          }
          if(loBitLength > 0) lo = IEEE754.ComposeDouble(loNegative, exponent, loMantissa.ToUInt64());
        }
      }

      double hi = IEEE754.ComposeDouble(false, hiExponent, hiMantissa.ToUInt64(), allowOutOfRange);
      return new FP107(hi, lo);
    }

    /// <summary>Given a floating-point number expressed as an exponent and mantissa, along with values equal to 2^(n-1) and 2^n-1 where
    /// n is the maximum size of the mantissa (including any hidden bit), returns the floating-point number immediately less than it.
    /// </summary>
    static bool GetPredecessor(Integer hnmax, Integer nmaxm1, ref int exponent, ref Integer mantissa)
    {
      if(mantissa == hnmax)
      {
        exponent--;
        mantissa = nmaxm1;
        return true;
      }
      else
      {
        mantissa.UnsafeDecrement();
        return false;
      }
    }

    /// <summary>Given a floating-point number expressed as an exponent and mantissa, along with values equal to 2^(n-1) and 2^n where
    /// n is the maximum size of the mantissa (including any hidden bit), returns the floating-point number immediately greater than it.
    /// </summary>
    static bool GetSuccessor(Integer hnmax, Integer nmax, ref int exponent, ref Integer mantissa)
    {
      if(mantissa == nmax)
      {
        exponent++;
        mantissa = hnmax;
        return true;
      }
      else
      {
        mantissa.UnsafeIncrement();
        return false;
      }
    }

    /// <summary>Parses one half of an <see cref="FP107"/> value specified in hex format.</summary>
    static unsafe double ParseHex(string str, int i)
    {
      ulong value = 0;
      for(int e=i+16; i<e; i++) // parse the next 16 characters, which are all assumed to be hex digits
      {
        char c = char.ToUpperInvariant(str[i]);
        value = (value << 4) + (uint)(c - (c >= 'A' ? 'A'-10 : '0'));
      }
      return *(double*)&value; // return the parsed bytes as a double
    }

    /// <summary>Reduces the given value to have a magnitude no greater than Pi/32.</summary>
    static bool ReduceSinCosArgument(ref FP107 value, out int j, out int k)
    {
      if(!value.IsNaN && !value.IsInfinity)
      {
        value %= TwoPi;
        if(value > Pi) value -= TwoPi;
        else if(value < -Pi) value += TwoPi;

        double q = Math.Floor(value.hi/PiOverTwo.hi + 0.5);
        value -= PiOverTwo * q;
        j = (int)q;
        q = Math.Floor(value.hi/PiOver16.hi + 0.5);
        value -= PiOver16 * q;
        k = (int)q;
        if((uint)(j+2) <= 4u && (uint)(k+4) <= 8u) return true;
      }

      j = 0;
      k = 0;
      return false;
    }

    /// <summary>Attempts to refine a value that approximates the base-10 number represented by <paramref name="exponent"/> and
    /// <paramref name="mantissa"/> into the closest 107-bit floating-point number.
    /// </summary>
    static FP107 RefineParsedEstimate(FP107 value, int exponent, Integer mantissa)
    {
      Integer b2mantissa;
      int b2exponent;
      bool dummy;
      // if the estimate underflowed or overflowed or contains too few bits for the refinement to be likely to converge...
      if(value.IsZero || !value.Decompose(out dummy, out b2exponent, out b2mantissa) || b2mantissa.BitLength < 103)
      {
        return FindNearestFloat(exponent, mantissa); // switch to an algorithm that starts over from scratch
      }
      else // if we have an estimate that looks reasonable, try to refine it
      {
        const int MantissaSize = 107, MaxTries = 8;
        bool refined = false; // whether 'value' needs to be recalculated from b2exponent and b2mantissa

        // the algorithm expects a 107-bit mantissa, so fix it if it's too large or small
        if(b2mantissa.BitLength != MantissaSize)
        {
          refined      = b2mantissa.BitLength < MantissaSize; // try to keep the original value if it had more bits of precision
          b2exponent  -= MantissaSize - b2mantissa.BitLength;
          b2mantissa <<= MantissaSize - b2mantissa.BitLength;
        }

        // this is AlgorithmR from "How to Read Floating Point Numbers Accurately" (Clinger, 1990), limited to 8 iterations, after which
        // it'll fall back to using AlgorithmM (implemented in FindNearestFloat)
        Integer pow10 = Integer.Pow(10, Math.Abs(exponent)), pow2 = default(Integer);
        Integer nmax = Integer.Pow(2, MantissaSize), hnmax = Integer.Pow(2, MantissaSize-1), nmaxm1 = nmax-1;
        int tries; // how many iterations we've run so far
        bool changedExponent = true; // whether we need to recalculate pow2
        for(tries=0; tries<MaxTries; tries++)
        {
          if(changedExponent) // if b2exponent changed...
          {
            // if the exponent is out of range, fall back to AlgorithmM. (i don't trust this algorithm to handle denorms, etc.)
            const int MinExponent = 1-IEEE754.DoubleBias-MantissaSize;
            const int MaxExponent = (1<<IEEE754.DoubleExponentBits)-2-IEEE754.DoubleBiasToInt;
            if(b2exponent < MinExponent || b2exponent > MaxExponent) return FindNearestFloat(exponent, mantissa);
            else pow2 = Integer.Pow(2, Math.Abs(b2exponent)); // otherwise, recalculate pow2
          }

          // compute the ratio dec/bin, which is the ratio of the target decimal value to the binary approximation
          Integer dec, bin;
          if(exponent >= 0)
          {
            dec = mantissa * pow10;
            if(b2exponent >= 0)
            {
              bin = b2mantissa * pow2;
            }
            else
            {
              dec *= pow2;
              bin = b2mantissa;
            }
          }
          else
          {
            bin = b2mantissa * pow10;
            if(b2exponent >= 0)
            {
              dec = mantissa;
              bin *= pow2;
            }
            else
            {
              dec = mantissa * pow2;
            }
          }

          // this computes a measure of the error between the target and the approximation
          Integer error;
          bool dNegative = bin > dec;
          if(dNegative) error = bin - dec;
          else error = dec - bin;
          error = (error<<1) * b2mantissa;

          // then do a bunch of checks to determine whether the error is small enough
          if(error < bin)
          {
            if(!dNegative || b2mantissa != hnmax || (error<<1) <= bin) break;
            changedExponent = GetPredecessor(hnmax, nmaxm1, ref b2exponent, ref b2mantissa);
          }
          else if(error == bin)
          {
            if(b2mantissa.IsEven)
            {
              if(dNegative && b2mantissa == hnmax) changedExponent = GetPredecessor(hnmax, nmaxm1, ref b2exponent, ref b2mantissa);
              else break;
            }
            else
            {
              if(dNegative) GetPredecessor(hnmax, nmaxm1, ref b2exponent, ref b2mantissa);
              else GetSuccessor(hnmax, nmax, ref b2exponent, ref b2mantissa);
              refined = true;
              break;
            }
          }
          else
          {
            if(dNegative) changedExponent = GetPredecessor(hnmax, nmaxm1, ref b2exponent, ref b2mantissa);
            else changedExponent = GetSuccessor(hnmax, nmax, ref b2exponent, ref b2mantissa);
          }

          refined = true;
        }

        if(tries == MaxTries) value = FindNearestFloat(exponent, mantissa); // if too many iterations, give up and use another method
        else if(refined) value = Compose(b2exponent, b2mantissa, true, false); // otherwise, reconstruct 'value' if we changed it
        return value;
      }
    }

    static void Scale(double approximateValue, ref Integer n, ref Integer d, ref Integer mMinus, ref Integer mPlus, bool boundaryOk,
                      out int decimalPlace)
    {
      // we start with v = n/d and m- = d(v - v-)/2 and m+ = d(v+ - v)/2. we need to scale n and d so that n/d lies within [0.1,1). this is
      // equivalent to scaling v by 1/10^k where k is the smallest integer such that high <= 10^k. (low = (v + v-)/2 and high = (v + v+)/2.
      // see GetSignificantDigits for a detailed introduction.) we must also update m- and m+ to match because they're assumed to have the
      // same denominator as v.
      //
      // we don't want to perform the actual division needed to compare v directly, but we can note that if n >= d then n/d >= 1 and it
      // needs to be scaled down. we can do that by multiplying d by 10. so if n >= d, we can repeatedly multiply d by 10 until n < d.
      // on the other hand, if 10n < d, then n/d < 0.1. so if 10n < d, we can repeatedly multiply n by 10 until 10n >= d. this yields a
      // basic iterative algorithm, but it may be slow.
      //
      // we know that k = ceiling(log10 high) and high = (v + v+)/2. we can estimate k by taking k = ceiling(log10 v), in which case v
      // may be slightly less than high, resulting in a small chance that k is too small by 1. in our case, we only have the high component
      // of the f107 value, which may be either too high or too low. also, because the log function is not perfect, the result may be
      // slightly higher or lower than the true logarithm. to work around all this, we can subtract a small constant from the logarithm to
      // err on the side of it being too small, and then check whether the resulting value is correct or too small by one.

      // estimate k using the logarithmic method described above
      int k = (int)Math.Ceiling(Math.Log10(Math.Abs(approximateValue)) - 1e-10);

      // assume the estimate is correct and scale by it
      if(k >= 0) // if k >= 0, the value is too large and we need to divide by 10^k
      {
        d *= Integer.Pow(10, k); // we can divide n, m-, and m+ at the same time by increasing the denominator
      }
      else // otherwise, k < 0 and the value is too small, so we need to multiply by 10^-k
      {
        Integer scale = Integer.Pow(10, -k);
        n      *= scale; // scale up all three numerators
        mPlus  *= scale;
        mMinus *= scale;
      }

      // now check whether k was too small by one. we want high <= 10^k. if k is too small, then high > 10^k, meaning that the scaled high
      // value will be > 1. high = (v + v+)/2, v+ = v + 2^e, v = n/d, and m+ = d(v+ - v)/2. so 2m+/d = v+ - v, 2m+/d + 2v = v+ + v,
      // m+/d + v = (v + v+)/2 = high. and m+/d + v = m+/d + n/d = (n + m+)/d. and as noted above, we can check whether a/b > 1 by seeing
      // whether a > b. in this case, then, we need to compare n + m+ with d.
      int cmp = (n + mPlus).CompareTo(d);
      if(cmp >= (boundaryOk ? 0 : 1)) // if the scaled high > 1 (or >= 1 depending on what we can assume about the rounding mode)..
      {
        d *= 10; // scale everything down once more by increasing the denominator
        k++;     // and fix k
      }

      // k also happens to be the location of the decimal point. if k = 0 then that means v is in [0.1,1) and the decimal point goes
      // before the first digit (at index 0). if k = -1 then v is in [0.01,0.1) (i.e. v = 0.0dddd...), so the decimal point goes one
      // place before the first digit. (i.e. there's a leading zero.) and if k = 1 then v is in [1,10) and the decimal point goes after
      // the first digit, at index 1. etc.
      decimalPlace = k;
    }

    /// <summary>Computes the sine and cosine of the value, assuming its magnitude is no greater than Pi/32.</summary>
    static void SinCosReduced(FP107 value, out FP107 sin, out FP107 cos)
    {
      if(value.IsZero)
      {
        sin = Zero;
        cos = One;
      }
      else
      {
        sin = SinReduced(value);
        cos = Sqrt(1d - sin.Square()); // cos(x) = sqrt(1 - sin(x))
      }
    }

    /// <summary>Returns the sine of the value, assuming its magnitude is no greater than Pi/32.</summary>
    static FP107 SinReduced(FP107 value)
    {
      if(value.IsZero) return Zero;

      FP107 x = -value.Square(), r = value, sum = value, term;
      double threshold = Math.Abs((double)value) * (Precision/2);
      int i = 0;
      do
      {
        r *= x;
        term = r * InverseFactorials[i];
        sum += term;
        i += 2;
      } while(i < InverseFactorials.Length && Math.Abs((double)term) > threshold);
      return sum;
    }

    /// <summary>Splits a value with 53 bits of precision into two values, each having no more than 27 bits of precision.</summary>
    static double Split(double value, out double lo)
    {
      // the constant is 2^27+1. in general it should be 2^s+1 where s=ceil(m/2) and m is the number of bits in the mantissa plus one
      double t = ((1<<27)+1) * value;
      if(double.IsInfinity(t)) // if 'value' is so large that 't' overflows, then we can't accurately split it
      {
        lo = 0;
        return value;
      }

      double hi = t - (t-value);
      lo = value - hi;
      return hi;
    }

    /// <summary>Squares a number with 107-bit precision, returning the high component and placing the low component in
    /// <paramref name="lo"/>.
    /// </summary>
    static double Square(double value, out double lo)
    {
      double square = value*value, vl, vh = Split(value, out vl);
      lo = vh*vh - square + 2.0*vh*vl + vl*vl;
      return square;
    }

    /// <summary>Subtracts two numbers with 107-bit precision, returning the high component and placing the low component in
    /// <paramref name="lo"/>.
    /// </summary>
    static double Subtract(double a, double b, out double lo)
    {
      double sum = a-b, v = sum-a;
      lo = a - (sum-v) - (b+v);
      return sum;
    }

    static bool TryParse(string str, NumberStyles style, IFormatProvider provider, out FP107 value, out Exception ex)
    {
      ex = null;
      value = default(FP107);
      if(str == null)
      {
        ex = new ArgumentNullException();
        return false;
      }

      // skip leading and trailing whitespace if it's allowed
      int start = 0, end = str.Length;
      if((style & NumberStyles.AllowLeadingWhite) != 0)
      {
        while(start < str.Length && char.IsWhiteSpace(str[start])) start++;
      }
      if((style & NumberStyles.AllowTrailingWhite) != 0)
      {
        while(end != 0 && char.IsWhiteSpace(str[end-1])) end--;
      }

      // parse the number out of hexadecimal format (AAAAAAAAAAAAAAAA:BBBBBBBBBBBBBBBB) if we can
      if(end-start == 33 && str[start+16] == ':') // if the number might be in hexadecimal format...
      {
        bool hexadecimalFormat = true;
        for(int i=start; i<end; i++) // check if it really is hexadecimal format
        {
          char c = char.ToUpperInvariant(str[i]);
          if(i-start != 16 && (c < '0' || c > '9') && (c < 'A' || c > 'F')) { hexadecimalFormat = false; break; }
        }
        if(hexadecimalFormat)
        {
          value.hi = ParseHex(str, start);
          value.lo = ParseHex(str, start+17);
          return true;
        }
      }

      NumberFormatInfo nums = NumberFormatInfo.GetInstance(provider);

      // check for +/- infinity and NaN
      CultureInfo culture = provider as CultureInfo ?? CultureInfo.CurrentCulture;
      Func<string, bool> equals = s => end-start == s.Length && string.Compare(str, start, s, 0, s.Length, true, culture) == 0;
      if(equals(nums.NaNSymbol))
      {
        value = FP107.NaN;
        return true;
      }
      else if(equals(nums.PositiveInfinitySymbol))
      {
        value = FP107.PositiveInfinity;
        return true;
      }
      else if(equals(nums.NegativeInfinitySymbol))
      {
        value = FP107.NegativeInfinity;
        return true;
      }

      // at this point, it seems like we have a regular number, so parse the digits out of the string
      int digitCount, exponent;
      bool negative;
      byte[] digits = NumberFormat.ParseSignificantDigits(str, start, end, style, nums, out digitCount, out exponent, out negative);
      if(digits == null)
      {
        ex = new FormatException();
        return false;
      }

      // we've got a valid set of digits, so parse them into an FP107 value (which may be approximate)
      Integer mantissa = Integer.ParseDigits(digits, digitCount);

      // now we've parsed all the digits into a decimal mantissa. if the value is an integer, try to take a fast path with it
      if(exponent >= 0 && (digitCount < 20 || digitCount == 20 && digits[0] == 1 && digits[1] < 8))
      {
        // if the number given is an integer that might fit into a ulong, try to take a fast path
        ulong shortMantissa = mantissa.ToUInt64(); // scale the integer up and see if it overflows
        int i;
        for(i=0; i<exponent && shortMantissa <= 1844674407370955161UL; i++) shortMantissa *= 10;
        if(i == exponent) // if it didn't overflow...
        {
          value = shortMantissa; // then we're done
          goto done;
        }
      }

      // create an approximation of the value by scaling the mantissa by the exponent
      value = (FP107)mantissa;
      if(exponent < 0) value /= FP107.Pow(10, -exponent);
      else if(exponent > 0) value *= FP107.Pow(10, exponent);

      // refine our estimate to 107 bits of precision
      value = RefineParsedEstimate(value, exponent, mantissa);

      // signal an error if the value was too large
      if(value.IsPositiveInfinity)
      {
        ex = new OverflowException();
        return false;
      }

      // success
      done:
      if(negative) value = -value; // set the sign
      return true;
    }

    static readonly FP107 PiOver16 = Pi/16;

    static readonly FP107[] CosTable = new FP107[4] // values of Cos(k*pi/16) for k in 1..4
    {
      new FP107(0.98078528040323043, 1.8546939997825003E-17, false),
      new FP107(0.92387953251128674, 1.7645047084336677E-17, false),
      new FP107(0.83146961230254524, 1.4073856984728064E-18, false),
      new FP107(0.70710678118654757, -4.8336466567264567E-17, false),
    };

    static readonly FP107[] SinTable = new FP107[4] // values of Sin(k*pi/16) for k in 1..4
    {
      new FP107(0.19509032201612828, -7.9910790684617313E-18, false),
      new FP107(0.38268343236508978, -1.0050772696461588E-17, false),
      new FP107(0.55557023301960218, 4.7094109405616768E-17, false),
      new FP107(0.70710678118654757, -4.8336466567264567E-17, false),
    };

    static readonly FP107[] InverseFactorials = new FP107[] // 1/3! through 1/17!
    {
      new FP107(0.16666666666666666, 9.25185853854297E-18, false),
      new FP107(0.041666666666666664, 2.3129646346357427E-18, false),
      new FP107(0.0083333333333333332, 1.1564823173178714E-19, false),
      new FP107(0.0013888888888888889, -5.3005439543735771E-20, false),
      new FP107(0.00019841269841269841, 1.7209558293420705E-22, false),
      new FP107(2.48015873015873E-05, 2.1511947866775882E-23, false),
      new FP107(2.7557319223985893E-06, -1.8583932740464721E-22, false),
      new FP107(2.7557319223985888E-07, 2.3767714622250297E-23, false),
      new FP107(2.505210838544172E-08, -1.448814070935912E-24, false),
      new FP107(2.08767569878681E-09, -1.20734505911326E-25, false),
      new FP107(1.6059043836821613E-10, 1.2585294588752098E-26, false),
      new FP107(1.1470745597729725E-11, 2.0655512752830745E-28, false),
      new FP107(7.6471637318198164E-13, 7.03872877733453E-30, false),
      new FP107(4.7794773323873853E-14, 4.3992054858340813E-31, false),
      new FP107(2.8114572543455206E-15, 1.6508842730861433E-31, false),
    };
  }
  #endregion

  #region IEEE754
  /// <summary>Provides utilities related to the IEEE754 floating-point format.</summary>
  public static class IEEE754
  {
    /// <summary>The largest integer such that it and all integers of smaller magnitude can be represented exactly by a double-precision
    /// floating-point number.
    /// </summary>
    public const long MaxDoubleInt = 1L << (DoubleMantissaBits+1);
    /// <summary>The largest integer such that it and all integers of smaller magnitude can be represented exactly by a single-precision
    /// floating-point number.
    /// </summary>
    public const int MaxSingleInt = 1 << (SingleMantissaBits+1);
    /// <summary>The number of digits of precision that a double-precision floating-point number can provide (rounded down).</summary>
    public const int DoubleDigits = 15;
    /// <summary>The number of digits of precision that a single-precision floating-point number can provide (rounded down).</summary>
    public const int SingleDigits = 7;
    /// <summary>The exponent bias for double-precision floating-point values.</summary>
    public const int DoubleBias = 1023;
    /// <summary>The exponent bias for single-precision floating-point values.</summary>
    public const int SingleBias = 127;
    /// <summary>The exponent bias for double-precision floating-point values that converts the mantissa into an integer.</summary>
    public const int DoubleBiasToInt = DoubleBias + DoubleMantissaBits;
    /// <summary>The exponent bias for single-precision floating-point values that converts the mantissa into an integer.</summary>
    public const int SingleBiasToInt = SingleBias + SingleMantissaBits;
    /// <summary>The number of bits in the double-precision floating-point exponent.</summary>
    public const int DoubleExponentBits = 11;
    /// <summary>The number of bits in the single-precision floating-point exponent.</summary>
    public const int SingleExponentBits = 8;
    /// <summary>The number of bits in the double-precision floating-point mantissa, excluding the implicit leading bit.</summary>
    public const int DoubleMantissaBits = 52;
    /// <summary>The number of bits in the single-precision floating-point mantissa, excluding the implicit leading bit.</summary>
    public const int SingleMantissaBits = 23;
    /// <summary>The smallest double-precision floating-point number that, when added to 1.0, produces a result not equal to 1.0.</summary>
    public const double DoublePrecision = 2.2204460492503131e-16;
    /// <summary>The square root of <see cref="DoublePrecision"/>.</summary>
    public const double SqrtDoublePrecision = 0.00000001490116119;
    /// <summary>The smallest single-precision floating-point number that, when added to 1.0f, produces a result not equal to 1.0.</summary>
    public const float SinglePrecision = 1.192092896e-7f;
    /// <summary>The square root of <see cref="SinglePrecision"/>.</summary>
    public const float SqrtSinglePrecision = 0.0003452669830725f;

    /// <summary>Multiplies the given value by 2^<paramref name="offset"/> by adjusting the exponent.</summary>
    /// <remarks>This is equivalent to the <c>ldexp</c> C function. Like regular multiplication, the result will underflow to zero or
    /// overflow to infinity rather than an exception being thrown. This method is much slower than a multiplication if you already have
    /// 2^<paramref name="offset"/> available, but it is substantially faster than first computing Math.Pow(2, <paramref name="offset"/>)
    /// and then multiplying.
    /// </remarks>
    public static unsafe double AdjustExponent(double value, int offset)
    {
      // this method could be written more simply using Decompose and Compose, but since it's a replacement for Math.Pow() and
      // multiplication, we want it to be fast, so we'll inline the operations and structure the method around the most common case
      if(offset != 0)
      {
        // we'll assume that we can simply adjust the exponent, so grab the upper four bytes, where the exponent is
        uint hi = *(uint*)((byte*)&value+4);
        int exponent = (int)((hi>>20) & ((1u<<DoubleExponentBits)-1)); // extract the exponent
        if((uint)(exponent-1) < 2046u) // if the exponent is in the range 1-2046 (representing an normalized number)...
        {
          int newExponent = exponent + offset;
          if((uint)(newExponent-1) < 2046u) // if the new exponent is in the range 1-2046 (so the mantissa needn't change)...
          {
            *(uint*)((byte*)&value+4) = hi & ~(2047u<<20) | ((uint)newExponent<<20); // just stick the new exponent into the number
          }
          else if(newExponent <= 0) // otherwise, if the multiplication underflowed...
          {
            if(offset > 0) // if the multiplication actually overflowed but just seemed to underflow due to the integer addition
            {              // itself overflowing...
              goto overflow;
            }
            else if(newExponent < -DoubleMantissaBits) // if it underflowed to zero (we special case this due to the mod-64 shifts)...
            {
              value = 0;
            }
            else // it underflowed to a small (potentially zero) value...
            {
              // shift the mantissa (losing precision) because the exponent can't be reduced any further
              ulong mantissa = *(ulong*)&value & ((1L<<DoubleMantissaBits)-1) | (1UL<<DoubleMantissaBits); // the mantissa with hidden bit
              mantissa >>= 1-newExponent; // shift the mantissa to do gradual underflow. (we're not rounding to even. i guess it's okay.)
              *(ulong*)&value = mantissa | (value < 0 ? 1UL<<63 : 0); // the new exponent becomes zero
            }
          }
          else // otherwise, if the multiplication overflowed...
          {
            goto overflow;
          }
        }
        else if(exponent == 0) // if the number was denormalized (or zero) to begin with...
        {
          if(offset < 0) // if the value underflowed further...
          {
            if(offset <= -DoubleMantissaBits) // if it underflowed all the way to zero (we special case this due to the mod-64 shifts)...
            {
              value = 0;
            }
            else // if it might or might not have underflowed all the way to zero...
            {
              ulong mantissa = *(ulong*)&value & ((1L<<DoubleMantissaBits)-1); // get the full mantissa. (the hidden bit is zero)
              mantissa >>= -offset; // shift the mantissa to do gradual underflow. (we're not rounding to even. i guess it's okay.)
              *(ulong*)&value = mantissa | (value < 0 ? 1UL<<(DoubleExponentBits+DoubleMantissaBits) : 0); // the new exponent becomes zero
            }
          }
          else // the value was increased
          {
            ulong mantissa = *(ulong*)&value & ((1L<<DoubleMantissaBits)-1); // get the full mantissa. (the hidden bit is zero)
            int shift = Math.Min(offset, BinaryUtility.CountLeadingZeros(mantissa) - DoubleExponentBits);
            mantissa <<= shift; // shift the mantissa to the left as far as we can
            offset    -= shift;
            if((mantissa & (1UL<<DoubleMantissaBits)) != 0) // if the mantissa became normalized...
            {
              mantissa &= ~(1u<<DoubleMantissaBits); // remove the hidden bit
              offset++; // and increase the exponent because it's normalized now
            }
            if(offset > 2046) goto overflow;
            mantissa = mantissa | ((ulong)offset<<DoubleMantissaBits); // construct the final value in 'mantissa'
            if(value < 0) mantissa |= 1UL << (DoubleExponentBits+DoubleMantissaBits);
            *(ulong*)&value = mantissa;
          }
        }
      }
      return value;

      overflow: return value < 0 ? double.NegativeInfinity : double.PositiveInfinity;
    }

    /// <summary>Composes an IEEE 754 double-precision floating-point value from sign, exponent, and mantissa values. The value will be
    /// equal to <paramref name="mantissa"/> * 2^<paramref name="exponent"/>. This method cannot be used to construct values of
    /// <see cref="double.PositiveInfinity"/>, <see cref="double.NegativeInfinity"/>, or <see cref="double.NaN"/>.
    /// </summary>
    /// <remarks>The exponent and mantissa will be normalized if possible. To construct a denormalized value, use
    /// <see cref="RawComposeDouble(bool,int,ulong)"/>.
    /// </remarks>
    [CLSCompliant(false)]
    public static double ComposeDouble(bool negative, int exponent, ulong mantissa)
    {
      return ComposeDouble(negative, exponent, mantissa, false);
    }

    /// <summary>Composes an IEEE 754 double-precision floating-point value from sign, exponent, and mantissa values. The value will be
    /// equal to <paramref name="mantissa"/> * 2^<paramref name="exponent"/>. This method cannot be used to construct
    /// <see cref="double.NaN"/> values.
    /// </summary>
    /// <remarks>If the value is out of range and <paramref name="allowOutOfRange"/> is true, 0 or infinity will be returned, as
    /// appropriate. Otherwise, an exception will be thrown. The exponent and mantissa will be normalized if possible. To construct a
    /// denormalized value, use <see cref="RawComposeDouble(bool,int,ulong)"/>.
    /// </remarks>
    [CLSCompliant(false)]
    public static unsafe double ComposeDouble(bool negative, int exponent, ulong mantissa, bool allowOutOfRange)
    {
      if(mantissa >= MaxDoubleInt) // if the mantissa is too large to be inserted directly into the double...
      {
        // see if we can shift it into range without losing precision. don't bother shifting more than 11 bits because we'd just have to
        // shift back later when we normalize the value
        int shift = Math.Min(DoubleExponentBits, BinaryUtility.CountTrailingZeros(mantissa));
        mantissa >>= shift;
        if(mantissa >= MaxDoubleInt) throw new ArgumentOutOfRangeException("The mantissa is too large to be represented precisely.");
        exponent += shift; // increase to exponent to compensate for the decrease in the mantissa
      }

      exponent += DoubleBiasToInt; // bias the exponent
      if(exponent <= 0 && mantissa != 0) // if the value is too small to be represented...
      {
        if(allowOutOfRange) return 0;
        throw new ArgumentOutOfRangeException("The exponent is out of range for the mantissa.");
      }

      if((mantissa & (1UL<<DoubleMantissaBits)) != 0) // if the value is normalized...
      {
        mantissa &= ~(1UL<<DoubleMantissaBits); // remove the hidden bit
      }
      else if(mantissa == 0) // otherwise, if the value is zero, just set the correct exponent
      {
        exponent = 0;
      }
      else // otherwise, if the value is non-zero and denormalized, try to normalize it
      {
        int shift = Math.Min(exponent-1, BinaryUtility.CountLeadingZeros(mantissa) - DoubleExponentBits);
        if(shift > 0)
        {
          mantissa <<= shift;
          exponent -= shift;
        }
        // at this point, either the exponent was large enough to let us normalize it, or else the exponent has become equal to 1,
        // which is the correct biased exponent for a denormalized value, or the exponent was out of range to begin with
        if((mantissa & (1UL<<DoubleMantissaBits)) != 0) mantissa &= ~(1UL<<DoubleMantissaBits); // remove the hidden bit if normalized
        else if(exponent == 1) exponent = 0; // otherwise, fix up the exponent if it has the correct biased value
      }

      if((uint)exponent > (1u<<DoubleExponentBits)-2) // disallow exponents that are too large
      {
        if(allowOutOfRange) return negative ? double.NegativeInfinity : double.PositiveInfinity;
        throw new ArgumentOutOfRangeException("The exponent is out of range for the mantissa.");
      }

      // compose the value
      ulong value = mantissa | ((ulong)exponent<<DoubleMantissaBits);
      if(negative) value |= 1UL << (DoubleExponentBits+DoubleMantissaBits);
      return *(double*)&value;
    }

    /// <summary>Composes an IEEE 754 singe-precision floating-point value from sign, exponent, and mantissa values. The value will be
    /// equal to <paramref name="mantissa"/> * 2^<paramref name="exponent"/>. This method cannot be used to construct values of
    /// <see cref="float.PositiveInfinity"/>, <see cref="float.NegativeInfinity"/>, or <see cref="float.NaN"/>.
    /// </summary>
    /// <remarks>The exponent and mantissa will be normalized if possible. To construct a denormalized value, use
    /// <see cref="RawComposeSingle(bool,int,uint)"/>.
    /// </remarks>
    [CLSCompliant(false)]
    public static unsafe float ComposeSingle(bool negative, int exponent, uint mantissa)
    {
      return ComposeSingle(negative, exponent, mantissa, false);
    }

    /// <summary>Composes an IEEE 754 singe-precision floating-point value from sign, exponent, and mantissa values. The value will be
    /// equal to <paramref name="mantissa"/> * 2^<paramref name="exponent"/>. This method cannot be used to construct
    /// <see cref="double.NaN"/> values.
    /// </summary>
    /// <remarks>If the value is out of range and <paramref name="allowOutOfRange"/> is true, 0 or infinity will be returned, as
    /// appropriate. Otherwise, an exception will be thrown. The exponent and mantissa will be normalized if possible. To construct a
    /// denormalized value, use <see cref="RawComposeSingle(bool,int,uint)"/>.
    /// </remarks>
    [CLSCompliant(false)]
    public static unsafe float ComposeSingle(bool negative, int exponent, uint mantissa, bool allowOutOfRange)
    {
      if(mantissa >= MaxSingleInt) // if the mantissa is too large to be inserted directly into the float...
      {
        // see if we can shift it into range without losing precision. don't bother shifting more than 8 bits because we'd just have to
        // shift back later when we normalize the value
        int shift = Math.Min(SingleExponentBits, BinaryUtility.CountTrailingZeros(mantissa));
        mantissa >>= shift;
        if(mantissa >= MaxSingleInt) throw new ArgumentOutOfRangeException("The mantissa is too large to be represented precisely.");
        exponent += shift; // increase to exponent to compensate for the decrease in the mantissa
      }

      exponent += SingleBiasToInt; // bias the exponent
      if(exponent <= 0 && mantissa != 0) // if the value is too small to represent
      {
        if(allowOutOfRange) return 0;
        throw new ArgumentOutOfRangeException("The exponent is out of range for the mantissa.");
      }

      if((mantissa & (1u<<SingleMantissaBits)) != 0) // if the value is normalized...
      {
        mantissa &= ~(1u<<SingleMantissaBits); // remove the hidden bit
      }
      else if(mantissa == 0) // otherwise, if the value is zero, just set the correct exponent
      {
        exponent = 0;
      }
      else // otherwise, if the value is non-zero and denormalized, try to normalize it
      {
        int shift = Math.Min(exponent-1, BinaryUtility.CountLeadingZeros(mantissa) - SingleExponentBits);
        if(shift > 0)
        {
          mantissa <<= shift;
          exponent -= shift;
        }
        // at this point, either the exponent was large enough to let us normalize it, or else the exponent has become equal to 1,
        // which is the correct biased exponent for a denormalized value, or the exponent was out of range to begin with
        if((mantissa & (1u<<SingleMantissaBits)) != 0) mantissa &= ~(1u<<SingleMantissaBits); // remove the hidden bit if normalized
        else if(exponent == 1) exponent = 0; // otherwise, fix up the exponent if it has the correct biased value
      }

      if((uint)exponent > (1u<<SingleExponentBits)-2) // disallow exponents that are too large
      {
        if(allowOutOfRange) return negative ? float.NegativeInfinity : float.PositiveInfinity;
        throw new ArgumentOutOfRangeException("The exponent is out of range for the mantissa.");
      }

      // compose the value
      uint value = mantissa | ((uint)exponent<<SingleMantissaBits);
      if(negative) value |= 1u << (SingleExponentBits+SingleMantissaBits);
      return *(float*)&value;
    }

    /// <summary>Decomposes an IEEE 754 double-precision floating-point value into a sign, exponent, and mantissa.
    /// If <paramref name="value"/> is a special value such as <see cref="double.PositiveInfinity"/>,
    /// <see cref="double.NegativeInfinity"/>, or <see cref="double.NaN"/>, the method returns false and the exponent is meaningless.
    /// Otherwise, the value represents a number, in which case the exponent and mantissa will be normalized so that the floating-point
    /// magnitude equals <paramref name="mantissa"/> * 2^<paramref name="exponent"/> and the method returns true.
    /// </summary>
    [CLSCompliant(false)]
    public static bool Decompose(double value, out bool negative, out int exponent, out ulong mantissa)
    {
      int biasedExponent;
      RawDecompose(value, out negative, out biasedExponent, out mantissa);

      if(biasedExponent == 0) biasedExponent++; // if the number is denormalized (or zero) the actual exponent is one greater
      else if(biasedExponent != (1<<DoubleExponentBits)-1) mantissa |= 1UL<<DoubleMantissaBits; // otherwise, add the hidden one bit
      else // the number is NaN or infinity
      {
        exponent = 0;
        return false;
      }

      exponent = biasedExponent - DoubleBiasToInt; // unbias the exponent
      return true;
    }

    /// <summary>Decomposes an IEEE 754 single-precision floating-point value into a sign, exponent, and mantissa.
    /// If <paramref name="value"/> is a special value such as <see cref="float.PositiveInfinity"/>,
    /// <see cref="float.NegativeInfinity"/>, or <see cref="float.NaN"/>, the method returns false and the exponent is meaningless.
    /// Otherwise, the value represents a number, in which case the exponent and mantissa will be normalized so that the floating-point
    /// magnitude equals <paramref name="mantissa"/> * 2^<paramref name="exponent"/> and the method returns true.
    /// </summary>
    [CLSCompliant(false)]
    public static bool Decompose(float value, out bool negative, out int exponent, out uint mantissa)
    {
      int biasedExponent;
      RawDecompose(value, out negative, out biasedExponent, out mantissa);

      if(biasedExponent == 0) biasedExponent++; // if the number is denormalized (or zero) the actual exponent is one greater
      else if(biasedExponent != (1<<SingleExponentBits)-1) mantissa |= 1U<<SingleMantissaBits; // otherwise, add the hidden one bit
      else
      {
        exponent = 0;
        return false;
      }

      exponent = biasedExponent - SingleBiasToInt; // unbias the exponent
      return true;
    }

    /// <summary>Composes an IEEE 754 double-precision floating-point value from raw sign, exponent, and mantissa values.</summary>
    /// <remarks>The exponent should be in the raw (biased) form according to IEEE 754 rules.</remarks>
    [CLSCompliant(false)]
    public static unsafe double RawComposeDouble(bool negative, int exponent, ulong mantissa)
    {
      if((uint)exponent >= 1u<<DoubleExponentBits || mantissa >= 1UL<<DoubleMantissaBits) throw new ArgumentOutOfRangeException();
      ulong value = mantissa | ((ulong)exponent<<DoubleMantissaBits);
      if(negative) value |= 1UL << (DoubleExponentBits+DoubleMantissaBits);
      return *(double*)&value;
    }

    /// <summary>Composes an IEEE 754 single-precision floating-point value from raw sign, exponent, and mantissa values.</summary>
    /// <remarks>The exponent should be in the raw (biased) form according to IEEE 754 rules.</remarks>
    [CLSCompliant(false)]
    public static unsafe float RawComposeSingle(bool negative, int exponent, uint mantissa)
    {
      if((uint)exponent >= 1u<<SingleExponentBits || mantissa >= 1u<<SingleMantissaBits) throw new ArgumentOutOfRangeException();
      uint value = mantissa | ((uint)exponent<<SingleMantissaBits);
      if(negative) value |= 1u << (SingleExponentBits+SingleMantissaBits);
      return *(float*)&value;
    }

    /// <summary>Decomposes an IEEE 754 double-precision floating-point value into a sign, exponent, and mantissa.</summary>
    /// <remarks>The exponent will be returned in its raw (biased) form, and you must interpret the values according to IEEE 754 rules.</remarks>
    [CLSCompliant(false)]
    public static unsafe void RawDecompose(double value, out bool negative, out int exponent, out ulong mantissa)
    {
      ulong lv = *(ulong*)&value;
      mantissa = lv & ((1L<<DoubleMantissaBits)-1);
      uint iv = (uint)(lv>>DoubleMantissaBits);
      exponent = (int)(iv & ((1u<<DoubleExponentBits)-1));
      negative = (iv & (1u<<DoubleExponentBits)) != 0;
    }

    /// <summary>Decomposes an IEEE 754 single-precision floating-point value into a sign, exponent, and mantissa.</summary>
    /// <remarks>The exponent will be returned in its raw (biased) form, and you must interpret the values according to IEEE 754 rules.</remarks>
    [CLSCompliant(false)]
    public static unsafe void RawDecompose(float value, out bool negative, out int exponent, out uint mantissa)
    {
      uint uv = *(uint*)&value;
      mantissa = uv & ((1u<<SingleMantissaBits)-1);
      exponent = (int)((uv>>SingleMantissaBits) & ((1u<<SingleExponentBits)-1));
      negative = (uv & (1u << (SingleExponentBits+SingleMantissaBits))) != 0;
    }
  }
  #endregion
}
