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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AdamMil.IO;
using AdamMil.Mathematics.Random;
using AdamMil.Utilities;

// TODO: see if it's faster to produce larger numbers and do a single Simplify() at the end (as currently) or to perform several
// intermediate GCDs so we can reduce the size of the final numbers (as in my old Rational class)

namespace AdamMil.Mathematics
{
  /// <summary>This class implements an arbitrary-precision rational number type capable of both extreme range and extreme precision.</summary>
  /// <remarks>A rational number is a number represented as the ratio of two integers. In this implementation, the denominator is always
  /// positive and the numerator and denominator are kept as small as possible.
  /// <note type="caution">The default value of the Rational type is 0/0, which is not a valid number. You should initialize zero values
  /// with <see cref="Rational.Zero"/>.
  /// <para>
  /// Be careful when converting floating-point values to <see cref="Rational"/> values. Simply assigning
  /// floating-point values to <see cref="Rational"/>s will produce inaccurate results unless the decimal value can be represented exactly
  /// by the floating-point value. For instance, <c>Rational x = (Rational)1.1;</c> produces an inaccurate value. Although <c>x == 1.1</c>
  /// will evaluate to true, the floating-point value is only an approximation of 1.1, and actually equals 4953959590107546 * 2^-52 =
  /// 1.100000000000000088817841970012523233890533447265625.
  /// </para>
  /// <para>To get a <see cref="Rational"/> value exactly equal to 1.1, any of the following can be used instead, in order from fastest
  /// to slowest: <c>x = Rational.FromComponents(11, 10)</c>, <c>x = new Rational(11, 10)</c>, <c>x = (Rational)1.1m</c>,
  /// <c>x = Rational.Parse("1.1", CultureInfo.InvariantCulture)</c>, or <c>x = Rational.FromDecimalApproximation(1.1)</c>. The first is
  /// very fast. The last is especially slow and should not be executed often, and the method that uses a <see cref="decimal"/>
  /// literal (1.1m in this case) is only suitable if the value can be represented exactly by a <see cref="decimal"/> value.
  /// </para>
  /// <para>
  /// If a value can be represented exactly as a floating-point number (e.g. 1.25), then it is safe to assign it directly to a
  /// <see cref="Rational"/> value, e.g. <c>Rational x = (Rational)1.25;</c>, but care should be taken to document in your code that it is
  /// only safe for those particular values.
  /// </para>
  /// </note>
  /// </remarks>
  [Serializable]
  public struct Rational : IComparable, IComparable<Rational>, IConvertible, IEquatable<Rational>, IFormattable
  {
    /// <summary>Initializes a new <see cref="Rational"/> value from an <see cref="Integer"/>.</summary>
    public Rational(int value)
    {
      n = value;
      d = Integer.One;
    }

    /// <summary>Initializes a new <see cref="Rational"/> value from an <see cref="Integer"/>.</summary>
    [CLSCompliant(false)]
    public Rational(uint value)
    {
      n = value;
      d = Integer.One;
    }

    /// <summary>Initializes a new <see cref="Rational"/> value from an <see cref="Integer"/>.</summary>
    public Rational(long value)
    {
      n = value;
      d = Integer.One;
    }

    /// <summary>Initializes a new <see cref="Rational"/> value from an <see cref="Integer"/>.</summary>
    [CLSCompliant(false)]
    public Rational(ulong value)
    {
      n = value;
      d = Integer.One;
    }

    /// <summary>Initializes a new <see cref="Rational"/> value from an <see cref="Integer"/>.</summary>
    public Rational(Integer value)
    {
      n = value;
      d = Integer.One;
    }

    /// <summary>Initializes a new <see cref="Rational"/> value from a floating-point value.</summary>
    /// <remarks>The rational will be exactly equal to the floating-point value, but this may not be what you want since floating-point
    /// numbers are usually approximations of decimal numbers. You can use the <see cref="Approximate(int)"/> method to produce a nearby
    /// simpler number.
    /// </remarks>
    public Rational(double value)
    {
      ulong mantissa;
      int exponent;
      bool negative;
      if(!IEEE754.Decompose(value, out negative, out exponent, out mantissa)) throw new ArgumentOutOfRangeException();
      if(mantissa == 0) exponent = 0;
      if(exponent < 0) // if the exponent is negative, shift the mantissa right to avoid having to simplify the denominator later
      {
        int shift = Math.Min(-exponent, BinaryUtility.CountTrailingZeros(mantissa)); // don't shift so much as to make the exponent > 0,
        mantissa >>= shift;                                                          // or else we'd just shift it back later
        exponent  += shift;
      }

      n = mantissa;
      if(exponent >= 0) // if the exponent is nonnegative, the value is mantissa * 2^exponent...
      {
        n.UnsafeLeftShift(exponent);
        d = Integer.One;
      }
      else // if the exponent is negative, the value is mantissa / 2^-exponent...
      {
        d = Integer.Pow(2, -exponent);
      }
      if(negative) n = -n;
    }

    /// <summary>Initializes a new <see cref="Rational"/> value from a floating-point value.</summary>
    /// <remarks>The rational will be exactly equal to the floating-point value, but this may not be what you want since floating-point
    /// numbers are usually approximations of decimal numbers. You can use the <see cref="Approximate(int)"/> method to produce a nearby
    /// simpler number.
    /// </remarks>
    public Rational(FP107 value)
    {
      int exponent;
      bool negative;
      if(!value.Decompose(out negative, out exponent, out n)) throw new ArgumentOutOfRangeException();
      if(n.IsZero) exponent = 0;
      if(exponent < 0) // if the exponent is negative, shift the mantissa right to avoid having to simplify the denominator later
      {
        int shift = Math.Min(-exponent, Integer.CountTrailingZeros(n)); // don't shift so much as to make the exponent > 0,
        n.UnsafeRightShift(shift);                                      // or else we'd just shift it back later
        exponent += shift;
      }

      if(exponent >= 0) // if the exponent is nonnegative, the value is mantissa * 2^exponent...
      {
        n.UnsafeLeftShift(exponent);
        d = Integer.One;
      }
      else // if the exponent is negative, the value is mantissa / 2^-exponent...
      {
        d = Integer.Pow(2, -exponent);
      }
      if(negative) n = -n;
    }

    /// <summary>Initializes a new <see cref="Rational"/> value from a decimal value.</summary>
    public Rational(decimal value) : this(value, true) { }

    /// <summary>Initializes a new <see cref="Rational"/> value from a decimal value, with an option to skip simplification.</summary>
    /// <remarks>Normally, you should use the <see cref="Rational(decimal)"/> constructor instead, but this constructor may be useful with
    /// unsimplified operations.
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/remarks/node()"/>
    /// </remarks>
    public Rational(decimal value, bool simplify)
    {
      // a Decimal is a base-10 floating point value. extract the base-10 mantissa and exponent
      int[] bits = Decimal.GetBits(value);
      n = new Integer(new uint[] { (uint)bits[0], (uint)bits[1], (uint)bits[2] }, (bits[3]>>31) != 0, false);
      d = Integer.Pow(10, ((bits[3]>>16) & 0xFF));
      if(simplify && (n.BitLength | d.BitLength) > 1) Simplify(Integer.GreatestCommonFactor(n, d));
    }

    /// <summary>Initializes a new <see cref="Rational"/> value from a <see cref="BinaryReader"/>. The value is expected to have been saved
    /// with the <see cref="Save"/> method.
    /// </summary>
    public Rational(BinaryReader reader)
    {
      n = new Integer(reader);
      d = new Integer(reader);
    }

    /// <summary>Initializes a new <see cref="Rational"/> value from a numerator and denominator.</summary>
    /// <remarks>The numerator and denominator will be divided by their greatest common factor.
    /// To avoid this, use <see cref="FromComponents"/>.
    /// </remarks>
    public Rational(int numerator, int denominator)
    {
      if(denominator > 0)
      {
        n = numerator;
        d = denominator;
      }
      else if(denominator < 0)
      {
        n = -numerator;
        d = -denominator;
      }
      else
      {
        throw new DivideByZeroException();
      }

      if((n.BitLength | d.BitLength) > 1) Simplify((uint)NumberTheory.GreatestCommonFactor(numerator, denominator));
    }

    /// <summary>Initializes a new <see cref="Rational"/> value from a numerator and denominator.</summary>
    /// <remarks>The numerator and denominator will be divided by their greatest common factor.
    /// To avoid this, use <see cref="FromComponents"/>.
    /// </remarks>
    [CLSCompliant(false)]
    public Rational(uint numerator, uint denominator)
    {
      if(denominator == 0) throw new DivideByZeroException();
      n = numerator;
      d = denominator;
      if((n.BitLength | d.BitLength) > 1) Simplify(NumberTheory.GreatestCommonFactor(numerator, denominator));
    }

    /// <summary>Initializes a new <see cref="Rational"/> value from a numerator and denominator.</summary>
    /// <remarks>The numerator and denominator will be divided by their greatest common factor.
    /// To avoid this, use <see cref="FromComponents"/>.
    /// </remarks>
    public Rational(long numerator, long denominator)
    {
      if (denominator > 0)
      {
        n = numerator;
        d = denominator;
      }
      else if (denominator < 0)
      {
        n = -numerator;
        d = -denominator;
      }
      else
      {
        throw new DivideByZeroException();
      }
      if((n.BitLength | d.BitLength) > 1) Simplify((ulong)NumberTheory.GreatestCommonFactor(numerator, denominator));
    }

    /// <summary>Initializes a new <see cref="Rational"/> value from a numerator and denominator.</summary>
    /// <remarks>The numerator and denominator will be divided by their greatest common factor.
    /// To avoid this, use <see cref="FromComponents"/>.
    /// </remarks>
    [CLSCompliant(false)]
    public Rational(ulong numerator, ulong denominator)
    {
      if(denominator == 0) throw new DivideByZeroException();
      n = numerator;
      d = denominator;
      if((n.BitLength | d.BitLength) > 1) Simplify(NumberTheory.GreatestCommonFactor(numerator, denominator));
    }

    /// <summary>Initializes a new <see cref="Rational"/> value from a numerator and denominator.</summary>
    /// <remarks>The numerator and denominator will be divided by their greatest common factor.
    /// To avoid this, use <see cref="FromComponents"/>.
    /// </remarks>
    public Rational(Integer numerator, Integer denominator)
    {
      if(denominator.IsPositive)
      {
        n = numerator;
        d = denominator;
      }
      else if(denominator.IsNegative)
      {
        n = -numerator;
        d = -denominator;
      }
      else
      {
        throw new DivideByZeroException();
      }
      if((n.BitLength | d.BitLength) > 1) Simplify(Integer.GreatestCommonFactor(n, d));
    }

    Rational(Integer numerator, Integer denominator, bool checkSign)
    {
      if(!checkSign || denominator.IsPositive)
      {
        n = numerator;
        d = denominator;
      }
      else
      {
        n = -numerator;
        d = -denominator;
      }
    }

    /// <summary>Determines whether the value is an integer.</summary>
    public bool IsInteger
    {
      get { return d.BitLength == 1; } // d == 1
    }

    /// <summary>Determines whether the value is negative.</summary>
    public bool IsNegative
    {
      get { return n.IsNegative; }
    }

    /// <summary>Determines whether the value is positive.</summary>
    public bool IsPositive
    {
      get { return n.IsPositive; }
    }

    /// <summary>Determines whether the value is zero.</summary>
    public bool IsZero
    {
      get { return n.IsZero; }
    }

    /// <summary>Gets the numerator of the ratio.</summary>
    public Integer Numerator
    {
      get { return n; }
    }

    /// <summary>Gets the denominator of the ratio. This value is always positive.</summary>
    public Integer Denominator
    {
      get { return d; }
    }

    /// <summary>Gets the sign of the number, -1 if negative, 1 if positive, and 0 if zero.</summary>
    public int Sign
    {
      get { return n.Sign; }
    }

    /// <summary>Returns the magnitude (i.e. the absolute value) of this value.</summary>
    public Rational Abs()
    {
      if(IsNegative) return new Rational(-n, d, false);
      else return this;
    }

    /// <summary>Returns the rational number having the smallest denominator within plus or minus 10^-<paramref name="digits"/> of
    /// this value. Ties are broken by choosing the number with the smallest numerator.
    /// </summary>
    public Rational Approximate(int digits)
    {
      return Approximate(this, digits);
    }

    /// <summary>Returns the rational number having the smallest denominator within plus or minus <paramref name="tolerance"/> of
    /// this value. Ties are broken by choosing the number with the smallest numerator.
    /// </summary>
    public Rational Approximate(double tolerance)
    {
      return Approximate(this, new Rational(tolerance));
    }

    /// <summary>Returns the rational number having the smallest denominator within plus or minus <paramref name="tolerance"/> of
    /// this value. Ties are broken by choosing the number with the smallest numerator.
    /// </summary>
    public Rational Approximate(Rational tolerance)
    {
      return Approximate(this, tolerance);
    }

    /// <summary>Returns the smallest integer greater than or equal to this value.</summary>
    public Integer Ceiling()
    {
      return Ceiling(this);
    }

    /// <summary>Compares this value to the given value, and returns a positive number if this value is greater, a negative number if this
    /// value is less, and zero if the two values are equal.
    /// </summary>
    public int CompareTo(int value)
    {
      int cmp = Sign - Math.Sign(value); // if the signs are different, we can infer the relationship from that
      if(cmp == 0 && (d.BitLength != 1 || n != value)) cmp = n.CompareTo(d*value);
      return cmp;
    }

    /// <summary>Compares this value to the given value, and returns a positive number if this value is greater, a negative number if this
    /// value is less, and zero if the two values are equal.
    /// </summary>
    public int CompareTo(Integer value)
    {
      int cmp = Sign - value.Sign; // if the signs are different, we can infer the relationship from that
      if(cmp == 0 && (d.BitLength != 1 || n != value)) cmp = n.CompareTo(d*value);
      return cmp;
    }

    /// <summary>Compares this value to the given value, and returns a positive number if this value is greater, a negative number if this
    /// value is less, and zero if the two values are equal.
    /// </summary>
    public int CompareTo(Rational value)
    {
      int cmp = Sign - value.Sign; // if the signs are different, we can infer the relationship from that
      if(cmp == 0 && (n != value.n || d != value.d)) cmp = (n*value.d).CompareTo(d*value.n);
      return cmp;
    }

    /// <summary>Divides this value by the given divisor, returns the quotient, and stores the remainder in <paramref name="remainder"/>.</summary>
    public Rational DivRem(Rational divisor, out Rational remainder)
    {
      return DivRem(this, divisor, out remainder);
    }

    /// <summary>Divides this value by the given divisor, returns the quotient, and stores the remainder in <paramref name="remainder"/>.</summary>
    public Rational DivRem(int divisor, out Rational remainder)
    {
      return DivRem(this, divisor, out remainder);
    }

    /// <summary>Returns true if the given object is a <see cref="Rational"/> value equal to this one.</summary>
    public override bool Equals(object obj)
    {
      return obj is Rational && Equals((Rational)obj);
    }

    /// <summary>Returns true if the given integer equals this value.</summary>
    public bool Equals(int value)
    {
      return d.BitLength == 1 && n == value;
    }

    /// <summary>Returns true if the given integer equals this value.</summary>
    public bool Equals(Integer value)
    {
      return d.BitLength == 1 && n == value;
    }

    /// <summary>Returns true if the given value equals this value.</summary>
    public bool Equals(Rational value)
    {
      return n == value.n && d == value.d;
    }

    /// <summary>Converts a rational into a continued fraction.</summary>
    /// <returns>Returns an enumerator that produces the coefficients of the continued fraction.</returns>
    public IEnumerable<Integer> EnumerateContinuedFraction()
    {
      return EnumerateContinuedFraction(this);
    }

    /// <summary>Returns the largest integer less than or equal to this value.</summary>
    public Integer Floor()
    {
      return Floor(this);
    }

    /// <summary>Computes a hash code for the value.</summary>
    public override int GetHashCode()
    {
      return n.GetHashCode() ^ d.GetHashCode();
    }

    /// <summary>Returns the inverse of this value, which equals 1 divided by this value but calculated much more efficiently.</summary>
    public Rational Inverse()
    {
      if(IsZero) throw new DivideByZeroException();
      return new Rational(d, n, true);
    }

    /// <summary>Returns this value raised to the given power.</summary>
    public Rational Pow(int power)
    {
      return Pow(this, power);
    }

    /// <summary>Returns the given root (e.g. square root, cube root, etc) of this value.</summary>
    /// <param name="root">The root to compute. Two is the square root, three is the cube root, etc.</param>
    /// <param name="digits">The number of decimal digits of precision to which the root should be calculated</param>
    public Rational Root(int root, int digits)
    {
      return Root(this, root, digits);
    }

    /// <summary>Rounds this value to the nearest integer and returns the result.</summary>
    public Integer Round()
    {
      return Round(this);
    }

    /// <summary>Rounds this value to the given number of fractional digits and returns the result.</summary>
    public Rational Round(int digits)
    {
      return Round(this, digits);
    }

    /// <summary>Saves this value to a <see cref="BinaryWriter"/>. The value can be recreated using the
    /// <see cref="Rational(BinaryReader)"/> constructor.
    /// </summary>
    public void Save(BinaryWriter writer)
    {
      n.Save(writer);
      d.Save(writer);
    }

    /// <summary>Returns a simplified version of this rational if it was produced by unsimplified arithmetic
    /// (e.g. <see cref="UnsimplifiedAdd"/>).
    /// </summary>
    /// <remarks>In most cases, this method does not need to be called, since all normal operations simplify the result automatically.</remarks>
    public Rational Simplify()
    {
      return Simplify(this);
    }

    /// <summary>Returns the square of the value. This is more efficient than multiplying it by itself.</summary>
    public Rational Square()
    {
      return new Rational(n.Square(), d.Square(), false);
    }

    /// <summary>Returns the square root of this value.</summary>
    public Rational Sqrt(int digits)
    {
      return Sqrt(this, digits);
    }

    /// <summary>Converts a rational into a continued fraction.</summary>
    /// <returns>Returns a list containing the coefficients of the continued fraction.</returns>
    public List<Integer> ToContinuedFraction()
    {
      return ToContinuedFraction(this);
    }

    /// <summary>Converts the value to string using the current culture.</summary>
    /// <remarks>This method is equivalent to calling <see cref="ToString(string)"/> with the "R" format.</remarks>
    public override string ToString()
    {
      return ToString(null, null);
    }

    /// <summary>Converts the value to string using the current culture.</summary>
    public string ToString(string format)
    {
      return ToString(format, null);
    }

    /// <summary>Converts the value to string using the current culture.</summary>
    /// <param name="fractionalDigits">The maximum number of fractional digits to return.</param>
    public string ToString(int fractionalDigits)
    {
      return ToString(fractionalDigits, null);
    }

    /// <summary>Converts the value to string using the given format provider.</summary>
    /// <remarks>This method is equivalent to calling <see cref="ToString(string,IFormatProvider)"/> with the "R" format.</remarks>
    public string ToString(IFormatProvider provider)
    {
      return ToString(null, provider);
    }

    /// <summary>Converts the value to string using the current culture.</summary>
    /// <param name="fractionalDigits">The maximum number of fractional digits to return.</param>
    /// <param name="provider">The <see cref="IFormatProvider"/> that controls how the number is formatted, or null to use the current
    /// culture's format provider.
    /// </param>
    public string ToString(int fractionalDigits, IFormatProvider provider)
    {
      if(fractionalDigits < 0) throw new ArgumentOutOfRangeException();
      return ToString(provider, 'F', fractionalDigits, false);
    }

    /// <summary>Converts the value to string using the current culture.</summary>
    public string ToString(string format, IFormatProvider provider)
    {
      // parse and validate the user's format string
      int desiredPrecision;
      char formatType;
      bool capitalize;
      if(!NumberFormat.ParseFormatString(format, 'G', out formatType, out desiredPrecision, out capitalize))
      {
        throw new FormatException("Unsupported format string: " + format);
      }
      return ToString(provider, formatType, desiredPrecision, capitalize);
    }

    /// <summary>Returns the value, truncated towards zero.</summary>
    public Integer Truncate()
    {
      if(IsInteger) return n;
      else return n / d;
    }

    /// <summary>Returns this rational added to another without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public Rational UnsimplifiedAdd(Rational value)
    {
      return UnsimplifiedAdd(this, value);
    }

    /// <summary>Returns this rational added to an integer without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public Rational UnsimplifiedAdd(int value)
    {
      return UnsimplifiedAdd(this, value);
    }

    /// <summary>Compares this rational to another. Neither needs to be simplified.</summary>
    public int UnsimplifiedCompareTo(Rational other)
    {
      return UnsimplifiedCompare(this, other);
    }

    /// <summary>Compares this rational, which need not be simplified, to an integer.</summary>
    public int UnsimplifiedCompareTo(int other)
    {
      return UnsimplifiedCompare(this, other);
    }

    /// <summary>Returns this rational minus one without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public Rational UnsimplifiedDecrement()
    {
      return new Rational(n - d, d, false);
    }

    /// <summary>Returns this rational divided by another without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public Rational UnsimplifiedDivide(Rational divisor)
    {
      return UnsimplifiedDivide(this, divisor);
    }

    /// <summary>Returns this rational divided by an integer without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public Rational UnsimplifiedDivide(int divisor)
    {
      return UnsimplifiedDivide(this, divisor);
    }

    /// <summary>Returns this rational divided by another and places the remainder in <paramref name="remainder"/>,
    /// without simplifying either.
    /// </summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public Rational UnsimplifiedDivRem(Rational divisor, out Rational remainder)
    {
      return UnsimplifiedDivRem(this, divisor, out remainder);
    }

    /// <summary>Returns this rational divided by an integer and places the remainder in <paramref name="remainder"/>,
    /// without simplifying either.
    /// </summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public Rational UnsimplifiedDivRem(int divisor, out Rational remainder)
    {
      return UnsimplifiedDivRem(this, divisor, out remainder);
    }

    /// <summary>Returns this rational plus one without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public Rational UnsimplifiedIncrement()
    {
      return new Rational(n + d, d, false);
    }

    /// <summary>Returns this rational multiplied by another without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public Rational UnsimplifiedMultiply(Rational value)
    {
      return UnsimplifiedMultiply(this, value);
    }

    /// <summary>Returns this rational multiplied by an integer without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public Rational UnsimplifiedMultiply(int value)
    {
      return UnsimplifiedMultiply(this, value);
    }

    /// <summary>Returns the remainder of this rational divided by another, without simplifying it.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public Rational UnsimplifiedRemainder(Rational divisor)
    {
      return UnsimplifiedRemainder(this, divisor);
    }

    /// <summary>Returns the remainder of this rational divided by an integer, without simplifying it.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public Rational UnsimplifiedRemainder(int divisor)
    {
      return UnsimplifiedRemainder(this, divisor);
    }

    /// <summary>Returns this rational minus another without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public Rational UnsimplifiedSubtract(Rational value)
    {
      return UnsimplifiedSubtract(this, value);
    }

    /// <summary>Returns this rational minus an integer without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public Rational UnsimplifiedSubtract(int value)
    {
      return UnsimplifiedSubtract(this, value);
    }

    #region Arithmetic operators
    /// <summary>Negates a <see cref="Rational"/> value.</summary>
    public static Rational operator-(Rational value)
    {
      return new Rational(-value.n, value.d, false);
    }

    /// <summary>Increments a <see cref="Rational"/> value.</summary>
    public static Rational operator++(Rational value)
    {
      return new Rational(value.n + value.d, value.d, false);
    }

    /// <summary>Decrements a <see cref="Rational"/> value.</summary>
    public static Rational operator--(Rational value)
    {
      return new Rational(value.n - value.d, value.d, false);
    }

    /// <summary>Adds two <see cref="Rational"/> values.</summary>
    public static Rational operator+(Rational a, Rational b)
    {
      return new Rational(a.n*b.d + b.n*a.d, a.d*b.d);
    }

    /// <summary>Subtracts one <see cref="Rational"/> value from another.</summary>
    public static Rational operator-(Rational a, Rational b)
    {
      return new Rational(a.n*b.d - b.n*a.d, a.d*b.d);
    }

    /// <summary>Multiplies two <see cref="Rational"/> values.</summary>
    public static Rational operator*(Rational a, Rational b)
    {
      return new Rational(a.n*b.n, a.d*b.d);
    }

    /// <summary>Divides one <see cref="Rational"/> value by another.</summary>
    public static Rational operator/(Rational a, Rational b)
    {
      return new Rational(a.n*b.d, a.d*b.n);
    }

    /// <summary>Divides one <see cref="Rational"/> value by another and returns the remainder.</summary>
    public static Rational operator%(Rational a, Rational b)
    {
      return a - new Rational((a.n*b.d) / (a.d*b.n) * b.n, b.d, false);
    }

    /// <summary>Adds a <see cref="Rational"/> value and an <see cref="Integer"/>.</summary>
    public static Rational operator+(Rational a, int b)
    {
      if(b == 1) return new Rational(a.n + a.d, a.d, false); // special case +1
      else return new Rational(b*a.d + a.n, a.d, false);
    }

    /// <summary>Subtracts an <see cref="Integer"/> from a <see cref="Rational"/> value.</summary>
    public static Rational operator-(Rational a, int b)
    {
      if(b == 1) return new Rational(a.n - a.d, a.d, false); // special case -1
      else return new Rational(a.n - b*a.d, a.d, false);
    }

    /// <summary>Multiplies a <see cref="Rational"/> value by an <see cref="Integer"/>.</summary>
    public static Rational operator*(Rational a, int b)
    {
      return new Rational(a.n*b, a.d);
    }

    /// <summary>Divides a <see cref="Rational"/> value by an <see cref="Integer"/>.</summary>
    public static Rational operator/(Rational a, int b)
    {
      return new Rational(a.n, a.d*b);
    }

    /// <summary>Divides a <see cref="Rational"/> value by an <see cref="Integer"/> and returns the remainder.</summary>
    public static Rational operator%(Rational a, int b)
    {
      Integer w = a.n / (a.d*b) * b;
      return new Rational(a.n - a.d*w, a.d);
    }

    /// <summary>Adds a <see cref="Rational"/> value and an <see cref="Integer"/>.</summary>
    public static Rational operator+(int a, Rational b)
    {
      if(a == 1) return new Rational(b.n + b.d, b.d, false); // special case +1
      else return new Rational(a*b.d + b.n, b.d, false);
    }

    /// <summary>Subtracts a <see cref="Rational"/> value from an <see cref="Integer"/>.</summary>
    public static Rational operator-(int a, Rational b)
    {
      if(a == 1) return new Rational(b.d - b.n, b.d, false); // special case -1
      else return new Rational(a*b.d - b.n, b.d, false);
    }

    /// <summary>Multiplies a <see cref="Rational"/> value by an <see cref="Integer"/>.</summary>
    public static Rational operator*(int a, Rational b)
    {
      return new Rational(a*b.n, b.d);
    }

    /// <summary>Divides a <see cref="Rational"/> value by an <see cref="Integer"/>.</summary>
    public static Rational operator/(int a, Rational b)
    {
      return new Rational(a*b.d, b.n);
    }

    /// <summary>Divides an <see cref="Integer"/> by a <see cref="Rational"/> value and returns the remainder.</summary>
    public static Rational operator%(int a, Rational b)
    {
      Integer p = (a*b.d) / b.n * b.n;
      return new Rational(a*b.d - p, p*b.d);
    }

    /// <summary>Adds a <see cref="Rational"/> value and an <see cref="Integer"/>.</summary>
    public static Rational operator+(Rational a, Integer b)
    {
      return new Rational(b*a.d + a.n, a.d, false);
    }

    /// <summary>Subtracts an <see cref="Integer"/> from a <see cref="Rational"/> value.</summary>
    public static Rational operator-(Rational a, Integer b)
    {
      return new Rational(a.n - b*a.d, a.d, false);
    }

    /// <summary>Multiplies a <see cref="Rational"/> value by an <see cref="Integer"/>.</summary>
    public static Rational operator*(Rational a, Integer b)
    {
      return new Rational(a.n*b, a.d);
    }

    /// <summary>Divides a <see cref="Rational"/> value by an <see cref="Integer"/>.</summary>
    public static Rational operator/(Rational a, Integer b)
    {
      return new Rational(a.n, a.d*b);
    }

    /// <summary>Divides a <see cref="Rational"/> value by an <see cref="Integer"/> and returns the remainder.</summary>
    public static Rational operator%(Rational a, Integer b)
    {
      Integer w = a.n / (a.d*b) * b;
      return new Rational(a.n - a.d*w, a.d);
    }

    /// <summary>Adds a <see cref="Rational"/> value and an <see cref="Integer"/>.</summary>
    public static Rational operator+(Integer a, Rational b)
    {
      return new Rational(a*b.d + b.n, b.d, false);
    }

    /// <summary>Subtracts a <see cref="Rational"/> value from an <see cref="Integer"/>.</summary>
    public static Rational operator-(Integer a, Rational b)
    {
      return new Rational(a*b.d - b.n, b.d, false);
    }

    /// <summary>Multiplies a <see cref="Rational"/> value by an <see cref="Integer"/>.</summary>
    public static Rational operator*(Integer a, Rational b)
    {
      return new Rational(a*b.n, b.d);
    }

    /// <summary>Divides a <see cref="Rational"/> value by an <see cref="Integer"/>.</summary>
    public static Rational operator/(Integer a, Rational b)
    {
      return new Rational(a*b.d, b.n);
    }

    /// <summary>Divides an <see cref="Integer"/> by a <see cref="Rational"/> value and returns the remainder.</summary>
    public static Rational operator%(Integer a, Rational b)
    {
      Integer p = (a*b.d) / b.n * b.n;
      return new Rational(a*b.d - p, p*b.d);
    }
    #endregion

    #region Comparison operators
    /// <summary>Determines whether one <see cref="Rational"/> value equals to another.</summary>
    public static bool operator==(Rational a, Rational b) { return a.Equals(b); }
    /// <summary>Determines whether one <see cref="Rational"/> value is not equal to another.</summary>
    public static bool operator!=(Rational a, Rational b) { return !a.Equals(b); }
    /// <summary>Determines whether one <see cref="Rational"/> value is less than another.</summary>
    public static bool operator<(Rational a, Rational b) { return a.CompareTo(b) < 0; }
    /// <summary>Determines whether one <see cref="Rational"/> value is greater than another.</summary>
    public static bool operator>(Rational a, Rational b) { return a.CompareTo(b) > 0; }
    /// <summary>Determines whether one <see cref="Rational"/> value is less than or equal another.</summary>
    public static bool operator<=(Rational a, Rational b) { return a.CompareTo(b) <= 0; }
    /// <summary>Determines whether one <see cref="Rational"/> value is greater than or equal another.</summary>
    public static bool operator>=(Rational a, Rational b) { return a.CompareTo(b) >= 0; }

    /// <summary>Determines whether a <see cref="Rational"/> value equals an <see cref="Integer"/>.</summary>
    public static bool operator==(Rational a, int b) { return a.d.BitLength == 1 && a.n == b; }
    /// <summary>Determines whether a <see cref="Rational"/> value is not equal to an <see cref="Integer"/>.</summary>
    public static bool operator!=(Rational a, int b) { return a.d.BitLength != 1 || a.n != b; }
    /// <summary>Determines whether a <see cref="Rational"/> value is less than an <see cref="Integer"/>.</summary>
    public static bool operator<(Rational a, int b) { return a.CompareTo(b) < 0; }
    /// <summary>Determines whether a <see cref="Rational"/> value is greater than an <see cref="Integer"/>.</summary>
    public static bool operator>(Rational a, int b) { return a.CompareTo(b) > 0; }
    /// <summary>Determines whether a <see cref="Rational"/> value is less than or equal to an <see cref="Integer"/>.</summary>
    public static bool operator<=(Rational a, int b) { return a.CompareTo(b) <= 0; }
    /// <summary>Determines whether a <see cref="Rational"/> value is greater than or equal to an <see cref="Integer"/>.</summary>
    public static bool operator>=(Rational a, int b) { return a.CompareTo(b) >= 0; }

    /// <summary>Determines whether an <see cref="Integer"/> equals a <see cref="Rational"/> value.</summary>
    public static bool operator==(int a, Rational b) { return b.d.BitLength == 1 && b.n == a; }
    /// <summary>Determines whether an <see cref="Integer"/> is not equal to a <see cref="Rational"/> value.</summary>
    public static bool operator!=(int a, Rational b) { return b.d.BitLength != 1 || b.n != a; }
    /// <summary>Determines whether an <see cref="Integer"/> is less than a <see cref="Rational"/> value.</summary>
    public static bool operator<(int a, Rational b) { return b.CompareTo(a) > 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is greater than a <see cref="Rational"/> value.</summary>
    public static bool operator>(int a, Rational b) { return b.CompareTo(a) < 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is less than or equal to a <see cref="Rational"/> value.</summary>
    public static bool operator<=(int a, Rational b) { return b.CompareTo(a) >= 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is greater than or equal to a <see cref="Rational"/> value.</summary>
    public static bool operator>=(int a, Rational b) { return b.CompareTo(a) <= 0; }

    /// <summary>Determines whether a <see cref="Rational"/> value equals an <see cref="Integer"/>.</summary>
    public static bool operator==(Rational a, Integer b) { return a.d.BitLength == 1 && a.n == b; }
    /// <summary>Determines whether a <see cref="Rational"/> value is not equal to an <see cref="Integer"/>.</summary>
    public static bool operator!=(Rational a, Integer b) { return a.d.BitLength != 1 || a.n != b; }
    /// <summary>Determines whether a <see cref="Rational"/> value is less than an <see cref="Integer"/>.</summary>
    public static bool operator<(Rational a, Integer b) { return a.CompareTo(b) < 0; }
    /// <summary>Determines whether a <see cref="Rational"/> value is greater than an <see cref="Integer"/>.</summary>
    public static bool operator>(Rational a, Integer b) { return a.CompareTo(b) > 0; }
    /// <summary>Determines whether a <see cref="Rational"/> value is less than or equal to an <see cref="Integer"/>.</summary>
    public static bool operator<=(Rational a, Integer b) { return a.CompareTo(b) <= 0; }
    /// <summary>Determines whether a <see cref="Rational"/> value is greater than or equal to an <see cref="Integer"/>.</summary>
    public static bool operator>=(Rational a, Integer b) { return a.CompareTo(b) >= 0; }

    /// <summary>Determines whether an <see cref="Integer"/> equals a <see cref="Rational"/> value.</summary>
    public static bool operator==(Integer a, Rational b) { return b.d.BitLength == 1 && b.n == a; }
    /// <summary>Determines whether an <see cref="Integer"/> is not equal to a <see cref="Rational"/> value.</summary>
    public static bool operator!=(Integer a, Rational b) { return b.d.BitLength != 1 || b.n != a; }
    /// <summary>Determines whether an <see cref="Integer"/> is less than a <see cref="Rational"/> value.</summary>
    public static bool operator<(Integer a, Rational b) { return b.CompareTo(a) > 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is greater than a <see cref="Rational"/> value.</summary>
    public static bool operator>(Integer a, Rational b) { return b.CompareTo(a) < 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is less than or equal to a <see cref="Rational"/> value.</summary>
    public static bool operator<=(Integer a, Rational b) { return b.CompareTo(a) >= 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is greater than or equal to a <see cref="Rational"/> value.</summary>
    public static bool operator>=(Integer a, Rational b) { return b.CompareTo(a) <= 0; }
    #endregion

    #region Conversion operators
    /// <summary>Provides an implicit conversion from a signed 32-bit integer to a <see cref="Rational"/>.</summary>
    public static implicit operator Rational(int value)
    {
      return new Rational(value);
    }

    /// <summary>Provides an implicit conversion from an unsigned 32-bit integer to a <see cref="Rational"/>.</summary>
    [CLSCompliant(false)]
    public static implicit operator Rational(uint value)
    {
      return new Rational(value);
    }

    /// <summary>Provides an implicit conversion from a signed 64-bit integer to a <see cref="Rational"/>.</summary>
    public static implicit operator Rational(long value)
    {
      return new Rational(value);
    }

    /// <summary>Provides an implicit conversion from an unsigned 64-bit integer to a <see cref="Rational"/>.</summary>
    [CLSCompliant(false)]
    public static implicit operator Rational(ulong value)
    {
      return new Rational(value);
    }

    /// <summary>Provides an implicit conversion from an <see cref="Integer"/> to a <see cref="Rational"/>.</summary>
    public static implicit operator Rational(Integer value)
    {
      return new Rational(value);
    }

    /// <summary>Provides an implicit conversion from a <see cref="decimal"/> to a <see cref="Rational"/>.</summary>
    public static implicit operator Rational(decimal value)
    {
      return new Rational(value);
    }

    /// <summary>Provides an explicit conversion from a double-precision floating-point number to a <see cref="Rational"/>.</summary>
    public static explicit operator Rational(double value)
    {
      return new Rational(value);
    }

    /// <summary>Provides an explicit conversion from a single-precision floating-point number to a <see cref="Rational"/>.</summary>
    public static explicit operator Rational(float value)
    {
      return new Rational(value);
    }

    /// <summary>Provides an explicit conversion from an <see cref="FP107"/> value to a <see cref="Rational"/>.</summary>
    public static explicit operator Rational(FP107 value)
    {
      return new Rational(value);
    }

    /// <summary>Provides an explicit conversion from a <see cref="Rational"/> value to a double-precision floating-point value.</summary>
    public static explicit operator double(Rational value)
    {
      if(value.IsInteger) return (double)value.n;
      else if(value.n.BitLength == 1) return value.Sign/(double)value.d;
      int exponent = value.n.BitLength - value.d.BitLength - IEEE754.DoubleMantissaBits;
      return IEEE754.ComposeDouble(value.IsNegative, exponent, (ulong)Round(new Rational(value.n >> exponent, value.d, false)), true);
    }

    /// <summary>Provides an explicit conversion from a <see cref="Rational"/> value to a single-precision floating-point value.</summary>
    public static explicit operator float(Rational value)
    {
      if(value.IsInteger) return (float)value.n;
      else if(value.n.BitLength == 1) return value.Sign/(float)value.d;
      int exponent = value.n.BitLength - value.d.BitLength - IEEE754.SingleMantissaBits;
      return IEEE754.ComposeSingle(value.IsNegative, exponent, (uint)Round(new Rational(value.n >> exponent, value.d, false)), true);
    }

    /// <summary>Provides an explicit conversion from a <see cref="Rational"/> value to an <see cref="FP107"/>.</summary>
    public static explicit operator FP107(Rational value)
    {
      if(value.IsInteger) return (FP107)value.n;
      else if(value.n.BitLength == 1) return value.Sign / (FP107)value.d;
      int exponent = value.n.BitLength - value.d.BitLength - 106; // only 106 because we exclude the implicit leading 1 bit
      return FP107.Compose(value.IsNegative, exponent, Round(new Rational(value.n >> exponent, value.d, false)), true);
    }

    /// <summary>Provides an explicit conversion from a <see cref="Rational"/> value to a <see cref="decimal"/> value.</summary>
    public static explicit operator decimal(Rational value)
    {
      int decimalPlace; // TODO: see if we can make this more efficient, etc., e.g. by scaling n/d to the mantissa
      return NumberFormat.DigitsToDecimal(value.GetDigits(29, 'F', out decimalPlace), decimalPlace, value.IsNegative);
    }

    /// <summary>Provides an explicit conversion from a <see cref="Rational"/> value to a signed 32-bit integer.</summary>
    public static explicit operator int(Rational value)
    {
      return (int)(Integer)value;
    }

    /// <summary>Provides an explicit conversion from a <see cref="Rational"/> value to an unsigned 32-bit integer.</summary>
    [CLSCompliant(false)]
    public static explicit operator uint(Rational value)
    {
      return (uint)(Integer)value;
    }

    /// <summary>Provides an explicit conversion from a <see cref="Rational"/> value to a signed 64-bit integer.</summary>
    public static explicit operator long(Rational value)
    {
      return (long)(Integer)value;
    }

    /// <summary>Provides an explicit conversion from a <see cref="Rational"/> value to an unsigned 64-bit integer.</summary>
    [CLSCompliant(false)]
    public static explicit operator ulong(Rational value)
    {
      return (ulong)(Integer)value;
    }

    /// <summary>Provides an explicit conversion from a <see cref="Rational"/> value to an <see cref="Integer"/>.</summary>
    public static explicit operator Integer(Rational value)
    {
      return Truncate(value);
    }
    #endregion

    /// <summary>Returns the magnitude (i.e. the absolute value) of a given value.</summary>
    public static Rational Abs(Rational value)
    {
      if(value.IsNegative) return new Rational(-value.n, value.d, false);
      else return value;
    }

    /// <summary>Returns the rational number having the smallest denominator within plus or minus 10^-<paramref name="digits"/> of
    /// <paramref name="value"/>. Ties are broken by choosing the number with the smallest numerator.
    /// </summary>
    public static Rational Approximate(double value, int digits)
    {
      return Approximate(new Rational(value), Rational.Pow(10, -digits));
    }

    /// <summary>Returns the rational number having the smallest denominator within plus or minus <paramref name="tolerance"/> of
    /// <paramref name="value"/>. Ties are broken by choosing the number with the smallest numerator.
    /// </summary>
    public static Rational Approximate(double value, double tolerance)
    {
      return Approximate(new Rational(value), new Rational(tolerance));
    }

    /// <summary>Returns the rational number having the smallest denominator within plus or minus 10^-<paramref name="digits"/> of
    /// <paramref name="value"/>. Ties are broken by choosing the number with the smallest numerator.
    /// </summary>
    public static Rational Approximate(Rational value, int digits)
    {
      return Approximate(value, Rational.Pow(10, -digits));
    }

    /// <summary>Returns the rational number having the smallest denominator within plus or minus <paramref name="tolerance"/> of
    /// <paramref name="value"/>. Ties are broken by choosing the number with the smallest numerator.
    /// </summary>
    public static Rational Approximate(Rational value, Rational tolerance)
    {
      if(tolerance.IsZero) return value;

      // this is the algorithm PDQ2 by Joe Horn, with some optimizations as well as bug fixes for negative values. it's one of the uglier
      // algorithms i've seen, but it's faster than the straightforward approach of generating continued fractions for the bounds and
      // seeing where they differ
      Integer n0 = value.n.Abs(), ad0 = tolerance.n.Abs() * value.d, bn0 = tolerance.d * n0, bd = tolerance.d * value.d;
      Integer n = n0, d = value.d, x, cn = Integer.One, pn = default(Integer), y, cd = default(Integer), pd = Integer.One, q, r;
      do
      {
        q = n.DivRem(d, out r); n = d; d = r;
        x = q*cn + pn; pn = cn; cn = x; // x/y is the new fraction for testing. pn/pd is the previous one.
        y = q*cd + pd; pd = cd; cd = y; // cn/cd is a copy of x/y whose only purpose is to avoid some copies
      } while((bn0*y - bd*x).Abs() > ad0*y); // value.n - value.d * x/y > tolerance

      if(q.BitLength > 1)
      {
        Integer lo = default(Integer), hi = q;
        bool withinTolerance;
        do
        {
          Integer mid = (lo + hi) >> 1; // (lo + hi) / 2
          x = cn - pn*mid;
          y = cd - pd*mid;
          withinTolerance = (bn0*y - bd*x).Abs() <= ad0*y; // value.n - value.d * x/y <= tolerance
          if(withinTolerance) lo = mid;
          else hi = mid;
        } while(hi > lo + 1);

        if(!withinTolerance)
        {
          x = cn - pn*lo;
          y = cd - pd*lo;
        }
      }

      Rational result = new Rational(x, y);
      if(value.IsNegative) result = -result;
      return result;
    }

    /// <summary>Returns the smallest integer greater than or equal to the given value.</summary>
    public static Integer Ceiling(Rational value)
    {
      if(value.IsInteger) return value.n;
      Integer i = value.Truncate();
      if(value.IsPositive) i.UnsafeIncrement(); // e.g. 1.5 -> 1 (truncate) -> 2 (ceiling)
      return i;
    }

    /// <summary>Divides one value by another, returns the quotient, and stores the remainder in <paramref name="remainder"/>.</summary>
    public static Rational DivRem(Rational dividend, int divisor, out Rational remainder)
    {
      Rational quotient = dividend / divisor;
      remainder = dividend - quotient.Truncate()*divisor;
      return quotient;
    }

    /// <summary>Divides one value by another, returns the quotient, and stores the remainder in <paramref name="remainder"/>.</summary>
    public static Rational DivRem(Rational dividend, Rational divisor, out Rational remainder)
    {
      Rational quotient = dividend / divisor;
      remainder = UnsimplifiedSubtract(dividend, new Rational(divisor.n*quotient.Truncate(), divisor.d, false)).Simplify();
      return quotient;
    }

    /// <summary>Converts a rational into a continued fraction.</summary>
    /// <returns>Returns an enumerator that produces the coefficients of the continued fraction.</returns>
    public static IEnumerable<Integer> EnumerateContinuedFraction(Rational value)
    {
      while(true) // to convert a number to its continued fraction, you take the whole part, add that to the continued fraction, invert the
      {           // fractional part, and repeat. you stop when there's no fractional part left to invert
        Integer w = value.Truncate();
        yield return w;
        value = UnsimplifiedSubtract(value, w);
        if(value.IsZero) break;
        value = value.Inverse();
      }
    }

    /// <summary>Returns the largest integer less than or equal to the given value.</summary>
    public static Integer Floor(Rational value)
    {
      if(value.IsInteger) return value.n;
      Integer i = value.Truncate();
      if(value.IsNegative) i.UnsafeDecrement(); // e.g. -1.5 -> -1 (truncate) -> -2 (floor)
      return i;
    }

    /// <summary>This method is used to construct a <see cref="Rational"/> from the <see cref="Numerator"/> and <see cref="Denominator"/>
    /// obtained from an existing <see cref="Rational"/>.
    /// </summary>
    /// <param name="numerator">The numerator of the ratio, which must not share any factors with <paramref name="denominator"/>.</param>
    /// <param name="denominator">The denominator of the ratio, which must be positive and must not share any factors with
    /// <paramref name="numerator"/>.
    /// </param>
    /// <remarks>This method differs from the <see cref="Rational(Integer,Integer)"/> constructor in that the values are not checked for
    /// sign or range, and are not divided by their greatest common factor. You must do those yourself if necessary.
    /// </remarks>
    public static Rational FromComponents(Integer numerator, Integer denominator)
    {
      return new Rational(numerator, denominator, false);
    }

    /// <summary>Converts a continued fraction to a <see cref="Rational"/>.</summary>
    public static Rational FromContinuedFraction(params int[] continuedFraction)
    {
      if(continuedFraction == null) throw new ArgumentNullException();
      return FromContinuedFraction(continuedFraction, continuedFraction.Length);
    }

    /// <summary>Converts a continued fraction to a <see cref="Rational"/>.</summary>
    public static Rational FromContinuedFraction(IList<int> continuedFraction)
    {
      if(continuedFraction == null) throw new ArgumentNullException();
      return FromContinuedFraction(continuedFraction, continuedFraction.Count);
    }

    /// <summary>Converts a continued fraction to a <see cref="Rational"/>.</summary>
    /// <param name="continuedFraction">The list of coefficients in the continued fraction. There must be at least one coefficient.</param>
    /// <param name="maxTerms">The maximum number of terms to use from the continued fraction. This must be at least one. If less than the
    /// number of available coefficients, the result will be an approximation of the exact value. If greater than or equal to the number of
    /// available coefficients, the exact value will be returned.
    /// </param>
    public static Rational FromContinuedFraction(IList<int> continuedFraction, int maxTerms)
    {
      if(continuedFraction == null) throw new ArgumentNullException();
      if(maxTerms <= 0) throw new ArgumentOutOfRangeException("There must be at least one coefficient.");
      if(maxTerms > continuedFraction.Count) maxTerms = continuedFraction.Count;

      // to convert a continued fraction to a number, you start at the end (x = c[N-1]) and for each preceding term, you set x = c[i] + 1/x
      Integer rn = continuedFraction[--maxTerms], rd = Integer.One;
      while(--maxTerms >= 0)
      {
        Integer t = rn; // v' = 1/v + cf[i] => n'/d' = d/n + cf[i] = (d + n*cf[i]) / n
        rn = rd + rn*continuedFraction[maxTerms];
        rd = t;
      }
      return new Rational(rn, rd, true);
    }

    /// <summary>Converts a continued fraction to a <see cref="Rational"/>.</summary>
    public static Rational FromContinuedFraction(IList<Integer> continuedFraction)
    {
      if(continuedFraction == null) throw new ArgumentNullException();
      if(continuedFraction.Count == 0) throw new ArgumentException("The continued fraction must have at least one coefficient.");
      return FromContinuedFraction(continuedFraction, continuedFraction.Count);
    }

    /// <summary>Converts a continued fraction to a <see cref="Rational"/>.</summary>
    /// <param name="continuedFraction">The list of coefficients in the continued fraction. There must be at least one coefficient.</param>
    /// <param name="maxTerms">The maximum number of terms to use from the continued fraction. This must be at least one. If less than the
    /// number of available coefficients, the result will be an approximation of the exact value. If greater than or equal to the number of
    /// available coefficients, the exact value will be returned.
    /// </param>
    public static Rational FromContinuedFraction(IList<Integer> continuedFraction, int maxTerms)
    {
      if(continuedFraction == null) throw new ArgumentNullException();
      if(maxTerms <= 0) throw new ArgumentOutOfRangeException();
      if(maxTerms > continuedFraction.Count) maxTerms = continuedFraction.Count;

      // to convert a continued fraction to a number, you start at the end (x = c[N-1]) and for each preceding term, you set x = c[i] + 1/x
      Integer rn = continuedFraction[--maxTerms], rd = Integer.One;
      while(--maxTerms >= 0)
      {
        Integer t = rn; // v' = 1/v + cf[i] => n'/d' = d/n + cf[i] = (d + n*cf[i]) / n
        rn = rd + rn*continuedFraction[maxTerms];
        rd = t;
      }
      return new Rational(rn, rd, true);
    }

    /// <summary>Given a double-precision floating-point value, returns a <see cref="Rational"/> value that has the same printed value.</summary>
    /// <remarks><note type="caution">This method is slow - slower than parsing the same number from a string (assuming you have the string
    /// already) - so you should avoid calling it often.
    /// </note></remarks>
    public static Rational FromDecimalApproximation(double value)
    {
      return Parse(value.ToStringInvariant(), CultureInfo.InvariantCulture);
    }

    /// <summary>Returns the inverse of a <see cref="Rational"/>, which is 1 divided by the value but calculated much more efficiently.</summary>
    public static Rational Inverse(Rational value)
    {
      if(value.IsZero) throw new DivideByZeroException();
      return new Rational(value.d, value.n, true);
    }

    /// <summary>Returns the greater of two <see cref="Rational"/> values.</summary>
    public static Rational Max(Rational a, Rational b)
    {
      if(a >= b) return a;
      else return b;
    }

    /// <summary>Returns the lesser of two <see cref="Rational"/> values.</summary>
    public static Rational Min(Rational a, Rational b)
    {
      if(a <= b) return a;
      else return b;
    }

    /// <summary>Parses a <see cref="Rational"/> value formatted according to the current culture.</summary>
    public static Rational Parse(string str)
    {
      return Parse(str, NumberStyles.Any, null);
    }

    /// <summary>Parses a <see cref="Rational"/> value formatted using the given format provider.</summary>
    public static Rational Parse(string str, IFormatProvider provider)
    {
      return Parse(str, NumberStyles.Any, provider);
    }

    /// <summary>Parses a <see cref="Rational"/> value formatted using the given format provider and style.</summary>
    public static Rational Parse(string str, NumberStyles style, IFormatProvider provider)
    {
      Rational value;
      Exception ex;
      if(!TryParse(str, style, provider, out value, out ex)) throw ex;
      return value;
    }

    /// <summary>Returns an integer raised to a given power.</summary>
    public static Rational Pow(int value, int power)
    {
      bool negative = value < 0, invert = power < 0;
      if(negative)
      {
        value = -value; // this is okay even if value == int.MinValue because Integer.Pow treats it as unsigned
        if((power & 1) == 0) negative = false; // only odd values with odd powers cause odd results
      }
      power = Math.Abs(power);

      Integer result = Integer.Pow(value, power);
      if(negative) result = -result;
      if(!invert) return new Rational(result, Integer.One, false);
      else return new Rational(Integer.One, result, negative);
    }

    /// <summary>Returns a <see cref="Rational"/> value raised to a given power.</summary>
    public static Rational Pow(Rational value, int power)
    {
      bool invert = power < 0;
      power = Math.Abs(power);

      Integer numerator = Integer.Pow(value.n, power), denominator = Integer.Pow(value.d, power);
      if(!invert) return new Rational(numerator, denominator, false);
      else return new Rational(denominator, numerator, true);
    }

    /// <summary>Returns a random <see cref="Rational"/> greater than or equal to zero and less than one.</summary>
    /// <param name="rng">The <see cref="RandomNumberGenerator"/> used to generate the rational.</param>
    /// <param name="bits">The number of bits of precision used in generating the rational. If N bits are specified, the function will
    /// choose from 2^N possible results.
    /// </param>
    public static Rational Random(RandomNumberGenerator rng, int bits)
    {
      if(rng == null) throw new ArgumentNullException();
      Integer n = Integer.Random(rng, bits);
      int shift = Integer.CountTrailingZeros(n); // it's unlikely to help much, but remove as many shared powers of 2 as we can
      n.UnsafeRightShift(shift);
      return new Rational(n, Integer.Pow(2, bits - shift));
    }

    /// <summary>Returns a root (e.g. square root, cube root, etc) of an integer, computed as a <see cref="Rational"/>.</summary>
    public static Rational Root(int value, int root, int digits)
    {
      return Root(new Integer(value), root, digits);
    }

    /// <summary>Returns a root (e.g. square root, cube root, etc) of an integer, computed as a <see cref="Rational"/>.</summary>
    public static Rational Root(Integer value, int root, int digits)
    {
      // can't compute even roots of negative values, or zero'th roots (since the r'th root of x is x^(1/r))
      if(value.IsZero) return value;
      if(value.IsNegative && (root & 1) == 0 || root == 0) throw new ArgumentOutOfRangeException();

      bool negate = value.IsNegative, invert = root < 0;
      if(negate) value = -value;
      if(invert) root = -root;

      Rational result;
      if(root <= 2)
      {
        if(root == 2) result = Sqrt(value, digits);
        else result = value; // root == 1
      }
      else
      {
        Rational initialGuess;
        if(digits <= 15)
        {
          double dblValue = (double)value;
          if(double.IsPositiveInfinity(dblValue)) initialGuess = value >> GetRoughMaxDoubleShift(root);
          else initialGuess = new Rational(Math.Pow(dblValue, 1d/root));
        }
        else
        {
          FP107 fpValue = (FP107)value;
          if(fpValue.IsPositiveInfinity) initialGuess = value >> GetRoughMaxDoubleShift(root);
          else initialGuess = new Rational(FP107.Root(fpValue, root));
        }

        if(initialGuess.IsZero) initialGuess = new Rational(double.Epsilon); // an initial estimate of 0 can't converge
        result = NewtonsMethod(initialGuess, digits, x =>
        {
          // use Newton's method: x' = x - f(x)/f'(x) where f(x) = x^root - value, and thus f'(x) = root * x^(root-1). when x = n/d, this
          // is x' = n/d - (n^r/d^r - v) / (r*n^(r-1)/d^(r-1)) = n/d - ((n^r - v*d^r) / d^r) * (d^(r-1)/(r*n^(r-1))) =
          // n/d - (n^r - v*d^r) / (d*r*n^(r-1)) = (r*n^r - n^r + v*d^r) / (d*r*n^(r-1)) = ((r-1)*n^r + v*d^r) / (d*r*n^(r-1)). if we let
          // m = n^(r-1), then x' = ((r-1)*mn + v*d^r) / drm
          if(x.IsZero) return x; // if x becomes zero (due to rounding in NewtonsMethod), we're done (and anyway can't continue)
          Integer m = Integer.Pow(x.n, root-1);
          return new Rational(m*x.n*(root-1) + Integer.Pow(x.d, root)*value, m*x.d*root, true); // (r-1)*m == r*m-m = nPow-m
        });
      }

      if(negate) result = -result;
      if(invert) result = result.Inverse();
      return result;
    }

    /// <summary>Returns a root (e.g. square root, cube root, etc) of a <see cref="Rational"/> value.</summary>
    public static Rational Root(Rational value, int root, int digits)
    {
      // can't compute even roots of negative values, or zero'th roots (since the r'th root of x is x^(1/r))
      if(value.IsInteger) return Root(value.n, root, digits);
      if(value.IsNegative && (root & 1) == 0 || root == 0) throw new ArgumentOutOfRangeException();

      bool negate = value < 0, invert = root < 0;
      if(negate) value = -value;
      if(invert) root = -root;

      Rational result;
      if(root <= 2)
      {
        if(root == 2) result = Sqrt(value, digits);
        else result = value; // root == 1
      }
      else
      {
        Rational initialGuess = digits <= 15 ?
          new Rational(Math.Pow((double)value, 1d/root)) : new Rational(FP107.Root((FP107)value, root));
        if(initialGuess.IsZero) initialGuess = new Rational(double.Epsilon); // an initial estimate of 0 can't converge
        result = NewtonsMethod(initialGuess, digits, x =>
        {
          // use Newton's method: x' = x - f(x)/f'(x) where f(x) = x^root - value, and thus f'(x) = root * x^(root-1)
          if(x.IsZero) return x; // if x becomes zero (due to rounding in NewtonsMethod), we're done (and anyway can't continue)
          Rational m = Pow(x, root-1); // we'll let m = x^(root-1) so x^root = m*x
          return x.UnsimplifiedSubtract(
            m.UnsimplifiedMultiply(x).UnsimplifiedSubtract(value).UnsimplifiedDivide(m.UnsimplifiedMultiply(root)));
        });
      }

      if(negate) result = -result;
      if(invert) result = result.Inverse();
      return result;
    }

    /// <summary>Rounds the value to the nearest integer and returns the result.</summary>
    public static Integer Round(Rational value)
    {
      if(value.IsInteger) return value.n;

      Integer remainder, whole = Integer.DivRem(value.n, value.d, out remainder);
      // to see whether we should increase the magnitude, we'll compare 2*remainder to the denominator. if it's greater, we round up.
      // if it's equal, we round if 'whole' is odd (i.e. we round to even numbers)
      remainder <<= 1; // remainder *= 2
      int cmp = remainder.Abs().CompareTo(value.d);
      if(cmp > 0 || cmp == 0 && !whole.IsEven)
      {
        if(value.IsNegative) whole--;
        else whole++;
      }
      return whole;
    }

    /// <summary>Rounds the value to the given number of fractional digits and returns the result.</summary>
    public static Rational Round(Rational value, int digits)
    {
      if(digits == 0)
      {
        if(value.IsInteger) return value;
        else return Round(value);
      }
      else if(digits > 0)
      {
        Integer factor = Integer.Pow(10, digits);
        return new Rational(Round(new Rational(value.n * factor, value.d, false)), factor);
      }
      else
      {
        Integer factor = Integer.Pow(10, -digits);
        return Round(new Rational(value.n, value.d*factor, false)) * factor;
      }
    }

    /// <summary>Simplifies a rational that was produced by unsimplified arithmetic (e.g. <see cref="UnsimplifiedAdd"/>).</summary>
    /// <remarks>In most cases, this method does not need to be called, since all normal operations simplify the result automatically.</remarks>
    public static Rational Simplify(Rational rational)
    {
      if(rational.IsZero)
      {
        rational.d = Integer.One;
      }
      else
      {
        Integer gcf = Integer.GreatestCommonFactor(rational.n, rational.d);
        if(gcf.BitLength > 1)
        {
          rational.n /= gcf;
          rational.d /= gcf;
        }
      }
      return rational;
    }

    /// <summary>Returns the square root of the given value.</summary>
    public static Rational Sqrt(int value, int digits)
    {
      return Sqrt(new Integer(value), digits);
    }

    /// <summary>Returns the square root of the given value.</summary>
    public static Rational Sqrt(Integer value, int digits)
    {
      if(value.IsNegative) throw new ArgumentOutOfRangeException();
      if(value.IsZero) return value;

      Rational initialGuess;
      if(digits <= 15)
      {
        double dblValue = (double)value;
        if(double.IsPositiveInfinity(dblValue)) initialGuess = value >> 480; // sqrt(double.MaxValue) is roughly MaxValue / 2^480
        else initialGuess = new Rational(Math.Sqrt(dblValue));
      }
      else
      {
        FP107 fpValue = (FP107)value;
        if(fpValue.IsPositiveInfinity) initialGuess = value >> 480;
        else initialGuess = new Rational(FP107.Sqrt(fpValue));
      }

      if(initialGuess.IsZero) initialGuess = new Rational(double.Epsilon); // an initial estimate of 0 can't converge
      return NewtonsMethod(initialGuess, digits, x =>
      {
        // the basic idea is to use Newton's method: x' = x - f(x)/f'(x) where f(x) = x^2 - value. when x = n/d, this becomes
        // x' = n/d - (n^2/d^2 - value) / (2n/d) = n/d - ((n^2 - d^2*value) / d^2) * (d/2n) = n/d - (n^2 - d^2*value) / 2nd =
        // (2n^2 - (n^2 - d^2*value)) / 2nd = (n^2 + d^2*value) / 2nd
        if(x.IsZero) return x; // if x becomes zero (due to rounding in NewtonsMethod), we're done (and anyway can't continue)
        else return new Rational(x.n.Square() + x.d.Square()*value, (x.n*x.d) << 1, false);
      });
    }

    /// <summary>Returns the square root of the given value.</summary>
    public static Rational Sqrt(Rational value, int digits)
    {
      if(value.IsInteger) return Sqrt(value.n, digits);
      if(value.IsNegative) throw new ArgumentOutOfRangeException();
      Rational initialGuess = digits <= 15 ? new Rational(Math.Sqrt((double)value)) : new Rational(FP107.Sqrt((FP107)value));
      if(initialGuess.IsZero) initialGuess = new Rational(double.Epsilon); // an initial estimate of 0 can't converge
      return NewtonsMethod(initialGuess, digits, x => // Newton's method: x' = f(x)/f'(x) where f(x) = x^2 - value
      {
        if(x.IsZero) return x; // if x becomes zero (due to rounding in NewtonsMethod), we're done (and anyway can't continue)
        else return x.UnsimplifiedSubtract(x.Square().UnsimplifiedSubtract(value).UnsimplifiedDivide(x.UnsimplifiedMultiply(2)));
      });
    }

    /// <summary>Converts a rational into a continued fraction.</summary>
    /// <returns>Returns a list containing the coefficients of the continued fraction.</returns>
    public static List<Integer> ToContinuedFraction(Rational value)
    {
      return EnumerateContinuedFraction(value).ToList();
    }

    /// <summary>Returns the value, truncated towards zero as an <see cref="Integer"/>.</summary>
    public static Integer Truncate(Rational value)
    {
      if(value.IsInteger) return value.n;
      else return value.n / value.d;
    }

    /// <summary>Attempts to parse a <see cref="Rational"/> value formatted according to the current culture and returns true if the
    /// parse was successful.
    /// </summary>
    public static bool TryParse(string str, out Rational value)
    {
      return TryParse(str, NumberStyles.Any, null, out value);
    }

    /// <summary>Attempts to parse a <see cref="Rational"/> value formatted according to the given format provider and returns true if the
    /// parse was successful.
    /// </summary>
    public static bool TryParse(string str, IFormatProvider provider, out Rational value)
    {
      return TryParse(str, NumberStyles.Any, provider, out value);
    }

    /// <summary>Attempts to parse a <see cref="Rational"/> value formatted according to the given format provider and style, and returns
    /// true if the parse was successful.
    /// </summary>
    public static bool TryParse(string str, NumberStyles style, IFormatProvider provider, out Rational value)
    {
      Exception ex;
      return TryParse(str, style, provider, out value, out ex);
    }

    /// <summary>Adds two rationals without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public static Rational UnsimplifiedAdd(Rational a, Rational b)
    {
      return new Rational(a.n*b.d + b.n*a.d, a.d*b.d, false);
    }

    /// <summary>Adds a rationals and an integer without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public static Rational UnsimplifiedAdd(Rational a, int b)
    {
      return new Rational(a.n + a.d*b, a.d, false);
    }

    /// <summary>Compares two rationals, which need not be simplified.</summary>
    /// <returns>Returns a positive number if a &gt; b, a negative number if a &lt; b, and zero if a = b.</returns>
    public static int UnsimplifiedCompare(Rational a, Rational b)
    {
      int cmp = a.Sign - b.Sign; // if the signs are different, we can infer the relationship from that
      if(cmp == 0) cmp = (a.n*b.d).CompareTo(a.d*b.n);
      return cmp;
    }

    /// <summary>Compares a rational, which need not be simplified, to an integer.</summary>
    /// <returns>Returns a positive number if a &gt; b, a negative number if a &lt; b, and zero if a = b.</returns>
    public static int UnsimplifiedCompare(Rational a, int b)
    {
      int cmp = a.Sign - Math.Sign(b); // if the signs are different, we can infer the relationship from that
      if(cmp == 0) cmp = a.n.CompareTo(a.d*b);
      return cmp;
    }

    /// <summary>Subtracts one from a rational without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public static Rational UnsimplifiedDecrement(Rational value)
    {
      return new Rational(value.n - value.d, value.d, false);
    }

    /// <summary>Divides one rational by another without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public static Rational UnsimplifiedDivide(Rational a, Rational b)
    {
      if(b.IsZero) throw new DivideByZeroException();
      return new Rational(a.n*b.d, a.d*b.n, true);
    }

    /// <summary>Divides a rational by an integer without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public static Rational UnsimplifiedDivide(Rational a, int b)
    {
      if(b == 0) throw new DivideByZeroException();
      return new Rational(a.n, a.d*b, true);
    }

    /// <summary>Divides an integer by a rational without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public static Rational UnsimplifiedDivide(int a, Rational b)
    {
      if(b.IsZero) throw new DivideByZeroException();
      return new Rational(b.d*a, b.n, true);
    }

    /// <summary>Divides one rational by another, returning the quotient and placing the remainder in <paramref name="remainder"/>,
    /// without simplifying either.
    /// </summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public static Rational UnsimplifiedDivRem(Rational a, Rational b, out Rational remainder)
    {
      Rational quotient = UnsimplifiedDivide(a, b);
      remainder = UnsimplifiedSubtract(a, new Rational(b.n*quotient.Truncate(), b.d, false));
      return quotient;
    }

    /// <summary>Divides a rational by an integer, returning the quotient and placing the remainder in <paramref name="remainder"/>,
    /// without simplifying either.
    /// </summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public static Rational UnsimplifiedDivRem(Rational a, int b, out Rational remainder)
    {
      Rational quotient = UnsimplifiedDivide(a, b);
      remainder = new Rational(a.n - quotient.Truncate()*a.d*b, a.d, false);
      return quotient;
    }

    /// <summary>Adds one to a rational without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public static Rational UnsimplifiedIncrement(Rational value)
    {
      return new Rational(value.n + value.d, value.d, false);
    }

    /// <summary>Multiplies two rationals without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public static Rational UnsimplifiedMultiply(Rational a, Rational b)
    {
      return new Rational(a.n*b.n, a.d*b.d, false);
    }

    /// <summary>Multiplies a rational by an integer without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public static Rational UnsimplifiedMultiply(Rational a, int b)
    {
      return new Rational(a.n*b, a.d, false);
    }

    /// <summary>Divides one rational by another and returns the remainder without simplifying it.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public static Rational UnsimplifiedRemainder(Rational a, Rational b)
    {
      return UnsimplifiedSubtract(a, new Rational((a.n*b.d) / (a.d*b.n) * b.n, b.d, false));
    }

    /// <summary>Divides a rational by an integer and returns the remainder without simplifying it.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public static Rational UnsimplifiedRemainder(Rational a, int b)
    {
      Integer w = a.n / (a.d*b) * b;
      return new Rational(a.n - w*a.d, a.d, false);
    }

    /// <summary>Subtracts one rational from another without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public static Rational UnsimplifiedSubtract(Rational a, Rational b)
    {
      return new Rational(a.n*b.d - b.n*a.d, a.d*b.d, false);
    }

    /// <summary>Subtracts an integer from a rational without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public static Rational UnsimplifiedSubtract(Rational a, int b)
    {
      return new Rational(a.n - a.d*b, a.d, false);
    }

    /// <summary>Subtracts a rational from an integer without simplifying the result.</summary>
    /// <include file="documentation.xml" path="/Rational/UnsimplifiedRemarks/node()"/>
    public static Rational UnsimplifiedSubtract(int a, Rational b)
    {
      return new Rational(b.d*a - b.n, b.d, false);
    }

    /// <summary>As <see cref="Rational"/> value equal to zero.</summary>
    public static readonly Rational Zero = new Rational(Integer.Zero, Integer.One, false);
    /// <summary>As <see cref="Rational"/> value equal to one.</summary>
    public static readonly Rational One = new Rational(Integer.One, Integer.One, false);
    /// <summary>As <see cref="Rational"/> value equal to negative one.</summary>
    public static readonly Rational MinusOne = new Rational(Integer.MinusOne, Integer.One, false);

    #region IComparable Members
    int IComparable.CompareTo(object obj)
    {
      if(!(obj is Rational))
      {
        throw new ArgumentException("Expected a " + GetType().FullName + " value but received a " +
                                    (obj == null ? "null" : obj.GetType().FullName) + " value.");
      }
      return CompareTo((Rational)obj);
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
      return ((IConvertible)(Integer)this).ToByte(provider);
    }

    char IConvertible.ToChar(IFormatProvider provider)
    {
      return ((IConvertible)(Integer)this).ToChar(provider);
    }

    DateTime IConvertible.ToDateTime(IFormatProvider provider)
    {
      throw new InvalidCastException("Cannot convert from Rational to DateTime.");
    }

    decimal IConvertible.ToDecimal(IFormatProvider provider)
    {
      return (decimal)this;
    }

    double IConvertible.ToDouble(IFormatProvider provider)
    {
      return (double)this;
    }

    short IConvertible.ToInt16(IFormatProvider provider)
    {
      return ((IConvertible)(Integer)this).ToInt16(provider);
    }

    int IConvertible.ToInt32(IFormatProvider provider)
    {
      return ((Integer)this).ToInt32();
    }

    long IConvertible.ToInt64(IFormatProvider provider)
    {
      return ((Integer)this).ToInt64();
    }

    sbyte IConvertible.ToSByte(IFormatProvider provider)
    {
      return ((IConvertible)(Integer)this).ToSByte(provider);
    }

    float IConvertible.ToSingle(IFormatProvider provider)
    {
      return (float)this;
    }

    object IConvertible.ToType(Type conversionType, IFormatProvider provider)
    {
      if(conversionType == typeof(FP107)) return (FP107)this;
      else if(conversionType == typeof(Integer)) return (Integer)this;
      else return MathHelpers.DefaultConvertToType(this, conversionType, provider);
    }

    ushort IConvertible.ToUInt16(IFormatProvider provider)
    {
      return ((IConvertible)(Integer)this).ToUInt16(provider);
    }

    uint IConvertible.ToUInt32(IFormatProvider provider)
    {
      return ((Integer)this).ToUInt32();
    }

    ulong IConvertible.ToUInt64(IFormatProvider provider)
    {
      return ((Integer)this).ToUInt64();
    }
    #endregion

    byte[] GetDigits(int fractionalDigits, char formatType, out int decimalPlace)
    {
      if(IsZero) // if the value is zero, return at least one digit
      {
        decimalPlace = 1;
        return new byte[1];
      }

      if(formatType == 'P') fractionalDigits += 2; // percent format has an implicit x100 scale, so add two digits

      // if we don't need any fractional digits or scaling, just return the digits for the whole part
      if(fractionalDigits == 0 && formatType != 'G' && formatType != 'E')
      {
        byte[] wholeDigits = Integer.GetDigits(Round());
        decimalPlace = wholeDigits.Length;
        return wholeDigits;
      }

      // we need fractional digits or scaling
      fractionalDigits += 2; // add a couple extra digits so we have a >99% chance of being able to round correctly without further scaling

      // first get the digits for the whole part of the number
      Rational abs = Abs();
      Integer whole = abs.Truncate();
      decimalPlace = 0;
      if(whole.IsZero && (formatType == 'G' || formatType == 'E')) // in exponential format, we have to scale the number until we have a
      {                                                            // whole part (e.g. 2.2e-10). ('G' is potentially exponential.)
        for(int scale = 4; whole.IsZero; scale *= 2) // scale the value up until we have a whole part
        {
          abs = abs.UnsimplifiedMultiply(Integer.Pow(10, scale));
          decimalPlace -= scale;
          whole = abs.Truncate();
        }
      }

      var digits = new List<byte>((whole.BitLength+31)/32*10 + fractionalDigits + 1); // elementCount * 10 + fractionalDigits + 1
      fractionalDigits -= -decimalPlace; // if we shifted some fractional digits into the whole part, account for them
      decimalPlace += AddDigits(digits, whole);

      if(fractionalDigits > 0 && !abs.IsInteger) // if we need to add (more) fractional digits as well...
      {
        // shift the fractional part into the whole part and render it
        if(!whole.IsZero) abs = abs.UnsimplifiedSubtract(whole);
        abs = abs.UnsimplifiedMultiply(Integer.Pow(10, fractionalDigits));
        whole = abs.Truncate();
        AddFractionalDigits(digits, whole, fractionalDigits);

        // check that we have enough digits for correct rounding. we only need more if the last digits are a 5 followed by at least two
        // zeros (because we added two to fractionalDigits above), and if the 5 is not preceded by an odd digit. if the digit before the 5
        // is odd, it will round up anyway (e.g. 1.5 already rounds to 2, so we needn't find 1.5000001 to make that determination)
        int i = digits.Count-1, scaleSize = 4;
        while(i >= 0 && digits[i] == 0) i--; // count trailing zeros
        if(i < digits.Count-2 && i >= 0 && digits[i] == 5 && (i == 0 || (digits[i-1] & 1) == 0)) // if we need more digits for rounding...
        {
          abs = abs.UnsimplifiedSubtract(whole).Simplify();
          if(!abs.IsInteger) // if there are more digits available...
          {
            do
            {
              abs   = abs.UnsimplifiedMultiply(Integer.Pow(10, scaleSize));
              whole = abs.Truncate();
              AddFractionalDigits(digits, whole, scaleSize);
              scaleSize *= 2;
            } while(whole.IsZero);
          }
        }
      }

      return digits.ToArray();
    }

    void Simplify(Integer gcf)
    {
      if (n.IsZero)
      {
        d = Integer.One;
      }
      else if (gcf.BitLength > 1) // gcd != 1
      {
        n /= gcf;
        d /= gcf;
      }
    }

    void Simplify(uint gcf)
    {
      if(gcf > 1)
      {
        n.UnsafeDivide(gcf);
        d.UnsafeDivide(gcf);
      }
    }

    void Simplify(ulong gcf)
    {
      if(gcf <= uint.MaxValue)
      {
        uint gcf32 = (uint)gcf;
        if(gcf > 1)
        {
          n.UnsafeDivide(gcf32);
          d.UnsafeDivide(gcf32);
        }
      }
      else
      {
        n /= gcf;
        d /= gcf;
      }
    }

    string ToString(IFormatProvider provider, char formatType, int desiredPrecision, bool capitalize)
    {
      NumberFormatInfo nums = NumberFormatInfo.GetInstance(provider);
      if(d.IsZero) return nums.NaNSymbol; // if the value is invalid, return NaN

      if(formatType == 'R') // if round-trip format was requested, display the ratio as a fraction
      {
        return n.ToString("R") + "/" + d.ToString("R");
      }
      else if(formatType == 'X') // if hex format was requested, return the component values converted to hex
      {
        string format = capitalize ? "X" : "x";
        return n.ToString(format) + "/" + d.ToString(format);
      }

      // since a rational may have an infinite decimal representation, we'll need to choose the number of digits up front
      int defaultPrecision = NumberFormat.GetDefaultPrecision(nums, formatType);
      if(defaultPrecision == -1) defaultPrecision = 20;
      int fractionalDigits = desiredPrecision == -1 ? defaultPrecision : desiredPrecision, decimalPlace;
      byte[] digits = GetDigits(formatType != 'D' ? fractionalDigits : 0, formatType, out decimalPlace);
      return NumberFormat.FormatNumber(digits, decimalPlace, IsNegative, nums, formatType, desiredPrecision, defaultPrecision, capitalize);
    }

    Integer n, d; // these aren't marked readonly in order to prevent the compiler from making copies all the time

    static int AddDigits(List<byte> digits, Integer value)
    {
      value = value.Clone(); // clone the value so we can use unsafe methods on it
      int start = digits.Count;
      do digits.Add((byte)value.UnsafeDivide(10u)); while(!value.IsZero);
      digits.Reverse(start, digits.Count - start); // the above loop added them in reverse order
      return digits.Count - start;
    }

    static void AddFractionalDigits(List<byte> digits, Integer value, int expectedDigits)
    {
      int insertPoint = digits.Count, addedDigits = AddDigits(digits, value);
      if(addedDigits < expectedDigits) digits.InsertRange(insertPoint, new byte[expectedDigits - addedDigits]); // add leading zeros
    }

    static Rational GetEpsilon(int digits)
    {
      digits++; // add one digit so the value will be correct even after rounding
      if(digits >= 0) return new Rational(Integer.One, Integer.Pow(10, digits));
      else return Integer.Pow(10, -digits);
    }

    /// <summary>Returns the approximate number of bits needed to right-shift <see cref="double.MaxValue"/> to produce its Nth root.</summary>
    static int GetRoughMaxDoubleShift(int root)
    {
      // the maximum double value equals 2^1024. the Nth root of it equals (2^1024)^(1/n) = 2^(1024/n), and the divisor needed to scale
      // 2^1024 down to its Nth root is 2^1024 / 2^(1024/n) = 2^(1024 - 1024/n). since we're using integer math, and this will be used to
      // scale down a number in such a way that the result shouldn't equal zero, we'll make sure to round down 
      // (by maxing 1024/n less than it should be and thus 1024 - 1024/n greater than it should be), but that's okay. that corresponds to a
      // right shift of 1024 - 1024/n bits. we also want to shift by whole words because it's faster, so we'll ensure it's a multiple of 32
      return (1024 - (1023+root)/root) & ~31;
    }

    static Rational NewtonsMethod(Rational x, int digits, Func<Rational, Rational> update)
    {
      Rational epsilon = GetEpsilon(digits);
      if(digits < -1) digits = -1;
      Rational xo = x;
      while(true)
      {
        x = update(xo);
        if(x.UnsimplifiedSubtract(xo).Abs().UnsimplifiedCompareTo(epsilon) <= 0) return x.Simplify();
        // round and simplify the number to prevent an exponential increase in bit length. we need one more digit to guarantee convergence
        xo = Round(x, digits + 1);
      }
    }

    static bool TryParse(string str, NumberStyles style, IFormatProvider provider, out Rational value, out Exception ex)
    {
      value = default(Rational);
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

      NumberFormatInfo nums = NumberFormatInfo.GetInstance(provider);
      CultureInfo culture = provider as CultureInfo ?? CultureInfo.CurrentCulture;
      Integer n, d;
      bool negative = false;

      // see if the number is in ratio format
      int slashIndex = str.IndexOf('/', start, end-start);
      if(slashIndex >= 0)
      {
        if(str.StartsAt(start, nums.NegativeSign))
        {
          negative = true;
          start += nums.NegativeSign.Length;
          while(start < str.Length && char.IsWhiteSpace(str[start])) start++;
        }

        string nStr = str.Substring(start, slashIndex-start), dStr = str.Substring(slashIndex+1, end-(slashIndex+1));
        if(nStr.Length >= 3 && nStr[0] == '0' && char.ToUpperInvariant(nStr[1]) == 'X') // if it appears to be in hexadecimal format...
        { // require both parts to be in hexadecimal
          if((style & NumberStyles.HexNumber) != 0 && dStr.Length >= 3 && dStr[0] == '0' && char.ToUpperInvariant(dStr[1]) == 'X')
          {
            if(Integer.TryParse(nStr, style, provider, out n, out ex) && Integer.TryParse(dStr, style, provider, out d, out ex))
            {
              if(d.IsPositive) goto done;
              ex = new FormatException();
            }
            return false;
          }
          ex = new FormatException();
          return false;
        }
        else
        {
          NumberStyles noHex = style & ~NumberStyles.HexNumber; // don't allow only one part to be in hex
          if(Integer.TryParse(nStr, noHex, provider, out n, out ex) && Integer.TryParse(dStr, noHex, provider, out d, out ex))
          {
            if(d.IsPositive) goto done;
            ex = new FormatException();
          }
          return false;
        }
      }
      else // the number is not in ratio format...
      {
        // check for NaN
        if(end-start == nums.NaNSymbol.Length && string.Compare(str, start, nums.NaNSymbol, 0, end-start, true, culture) == 0)
        {
          ex = null;
          return true;
        }

        // it's not NaN, so try to parse the digits out of the string
        int digitCount, exponent;
        byte[] digits = NumberFormat.ParseSignificantDigits(str, start, end, style, nums, out digitCount, out exponent, out negative);
        if(digits == null)
        {
          ex = new FormatException();
          return false;
        }

        // we've got a valid set of digits, so parse them. trim trailing zeros if it would help us reduce scaling
        while(exponent < 0 && digitCount-1 > 0 && digits[digitCount-1] == 0) { digitCount--; exponent++; }
        n = Integer.ParseDigits(digits, digitCount);
        if(exponent > 0) n *= Integer.Pow(10, exponent);
        d = exponent < 0 ? Integer.Pow(10, -exponent) : Integer.One;
      }

      done:
      if(negative) n = -n;
      value = new Rational(n, d);
      ex    = null;
      return true;
    }
  }
}
