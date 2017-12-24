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
using System.Text;
using System.Text.RegularExpressions;
using AdamMil.Utilities;

// TODO: use unsafe code for arrays (esp. lookup tables) in polynomial after writing unit tests
// TODO: implement larger fields as extensions of smaller fields: see Efficient Software Implementations of Large Finite Fields GF(2^n) for Secure Storage Applications.
//       alternately, implement them as large Integer values. see Software Implementations of Elliptic Curve Cryptography (Shi and Yan)
// TODO: consider implementing polynomials that use Integer as the base storage, with values packed into words

namespace AdamMil.Mathematics.Fields
{
  #region GF2pField
  /// <summary>Represents a Galois field for some power of two from 1 to 31, i.e. GF(2^1) to GF(2^31). The values in the field are
  /// polynomials whose coefficients are in GF(2).
  /// </summary>
  /// <remarks>The values in the field are polynomials represented as integers. Each coefficient is either 0 or 1, so they are represented
  /// as bits within an integer, with the constant term in the lowest bit, the linear term in the second lowest bit, etc. So, x^3 + x + 1
  /// is represented in binary as 1011, corresponding to the integer 11.
  /// <para>Note that instantiating a GF2pField is a relatively expensive operations, so field objects should be reused if possible.</para>
  /// </remarks>
  // The field is constructed by taking a generator polynomial (default: x) raised to the power n modulo the prime polynomial for each of
  // the n values in the field except zero. So the values are g^0 mod p, g^1 mod p, g^2 mod p, through g^(2^power-2). So the values of a
  // GF(2^3) field with a generator of x (2) and a prime polynomial of x^3 + x + 1 [11] are:
  // x^0 mod (x^3+x+1) = 1 [1], x^1 mod (x^3+x+1) = x [2], x^2 mod (x^3+x+1) = x^2 [4], x^3 mod (x^3+x+1) = x+1 [3]
  // x^4 mod (x^3+x+1) = x^2+x [6], x^5 mod (x^3+x+1) = x^2+x+1 [7], x^6 mod (x^3+x+1) = x^2+1 [5], plus the zero value.
  // Note that the next value, x^7 mod (x^3+x+1), wraps around to equal 1 [1].
  public sealed class GF2pField
  {
    /// <summary>Initializes a new <see cref="GF2pField"/>.</summary>
    /// <param name="power">Determines the order of the field by the formula 2^<paramref name="power"/></param>
    public GF2pField(int power) : this(power, 0) { }

    /// <summary>Initializes a new <see cref="GF2pField"/>.</summary>
    /// <param name="power">Determines the order of the field by the formula 2^<paramref name="power"/>. This must be from 1 to 31.</param>
    /// <param name="prime">The value of the irreducible polynomial that helps define the field. If zero, a default polynomial for the
    /// power will be used.
    /// </param>
    /// <param name="generator">The generator polynomial that helps define the field. The values of the field are powers of the generator.
    /// The default is two, which is the minimum value for fields larger than GF(2^1). When <paramref name="power"/> is one, the generator
    /// must be one. Thus, the default value will not work for fields with a <paramref name="power"/> of one.
    /// </param>
    /// <remarks>The field will have 2^<paramref name="power"/> values equal to <paramref name="generator"/>^i mod <paramref name="prime"/>
    /// for i from 0 to 2^<paramref name="power"/>-2, as well as a zero value.
    /// </remarks>
    public GF2pField(int power, int prime, int generator = 2)
    {
      if(power < 1 || power > MaxPower) throw new ArgumentOutOfRangeException("power", "must be from 1 to 31");
      if(prime < 0 || prime != 0 && (uint)prime < (1u<<power)) throw new ArgumentOutOfRangeException("prime", "must be at least 2^power");
      if(power > 1 && generator <= 1) throw new ArgumentOutOfRangeException("generator", "must be at least 2");
      if((uint)generator > (uint)((1u<<power)-1)) throw new ArgumentOutOfRangeException("generator", "must be within the field");
      Order     = 1u << power;
      MaxValue  = (int)Order - 1;
      Power     = power;
      Prime     = prime == 0 ? PrimePolys[power-1] : prime;
      Generator = generator;
      // optimize calculations in small fields using a lookup table
      if(power <= MaxLUTPower) GenerateLogTables(power, Prime, generator, out logTable, out expTable);
    }

    /// <summary>Gets the generator polynomial that helps define the field.</summary>
    public int Generator { get; private set; }

    /// <summary>Gets the order of the field, which is the number of possible values in the field. The field's legal values are from 0
    /// (inclusive) to <see cref="Order"/> (exclusive).
    /// </summary>
    [CLSCompliant(false)]
    public uint Order { get; private set; }

    /// <summary>Gets the maximum value in the field. The field's legal values are from 0 to <see cref="MaxValue"/> (inclusive).</summary>
    public int MaxValue { get; private set; }

    /// <summary>Gets the power of two used to determine the <see cref="Order"/> of the field, which is computed as 2^<see cref="Power"/>.</summary>
    public int Power { get; private set; }

    /// <summary>Gets the prime polynomial that helps define the field.</summary>
    public int Prime { get; private set; }

    /// <summary>Adds two values, which are assumed to be within the field.</summary>
    public int Add(int a, int b)
    {
      return a ^ b;
    }

    /// <summary>Divides one value by another, both of which are assumed to be within the field.</summary>
    /// <exception cref="DivideByZeroException">Thrown if <paramref name="denominator"/> is zero.</exception>
    public int Divide(int numerator, int denominator)
    {
      if(logTable != null)
      {
        if(denominator == 0) throw new DivideByZeroException();
        return numerator != 0 ? expTable[logTable[numerator] + MaxValue - logTable[denominator]] : 0;
      }
      else
      {
        return Multiply(numerator, Invert(denominator));
      }
    }

    /// <summary>Exponentiates a value which is assumed to be in the field.</summary>
    /// <returns>Returns <see cref="Generator"/>^<paramref name="value"/>.</returns>
    public int Exp(int value)
    {
      return expTable != null ? expTable[value] : Pow(Generator, value);
    }

    /// <summary>Inverts a value in the field, equivalent to computing 1/<paramref name="n"/>.</summary>
    /// <remarks>Multiplying a value by the inverse of x is the same as dividing the value by x. If many values need to be divided by
    /// the same divisor, it is more efficient to compute the inverse of the divisor and multiply the values by the inverse.
    /// </remarks>
    /// <exception cref="DivideByZeroException">Thrown if <paramref name="n"/> is zero.</exception>
    public int Invert(int n)
    {
      if(n == 0) throw new DivideByZeroException();
      if(logTable != null)
      {
        return expTable[MaxValue - logTable[n]];
      }
      else
      {
        // implement the Extended Euclidean Algorithm to generate the inverse. i expect it'd work better than fancier algorithms
        // in the case of machine integers (since shifts are fast), but the need to count leading zeroes is unfortunate
        int b = 1, c = 0, m = Prime;
        while(n > 1) // TODO: we may be able to avoid most of the zero-counting cost by implementing another algorithm, since some
        {            // algorithms don't need to know exact difference in degree, only if one value has a larger degree than the other
          int j = BinaryUtility.CountLeadingZeros((uint)m) - BinaryUtility.CountLeadingZeros((uint)n);
          if(j < 0)
          {
            j = -j;
            int t = n; n = m; m = t;
            t = b; b = c; c = t;
          }
          n ^= m<<j;
          b ^= c<<j;
        }
        return b;
      }
    }

    /// <summary>Computes the discrete logarithm of the given value, which is assumed to be in the field.</summary>
    /// <remarks>The base of the logarithm is the value of the <see cref="Generator"/> polynomial. Calculation of logarithms is not yet
    /// supported for fields larger than GF(2^8).
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="n"/> is less than 1.</exception>
    /// <exception cref="NotSupportedException">Thrown if <see cref="Power"/> is greater than 8.</exception>
    public int Log(int n)
    {
      if(n <= 0) throw new ArgumentOutOfRangeException();
      if(logTable != null) return logTable[n];
      else throw new NotSupportedException("Logarithms are only supported for fields up to GF(2^8).");
    }

    /// <summary>Multiplies two values, which are assumed to be within the field.</summary>
    public unsafe int Multiply(int a, int b)
    {
      if(a == 0 || b == 0) return 0;
      if(logTable != null)
      {
        return expTable[logTable[a] + logTable[b]]; // x*y = exp(log(x) + log(y))
      }
      else if(a == b)
      {
        return Square(a);
      }
      else
      {
        // the basic algorithm shifts and adds one operand for each 1 bit in the other operand. for instance, if a is 1011 (x^3 + x + 1)
        // and b is 110 (x^2 + x), the algorithm adds 0*a + 1*(a<<1) + 1*(a<<2), which is (x^4 + x^2 + x) + (x^5 + x^3 + x^2). this is how
        // multiplication can be done in general. for instance in decimal, 23 * 12 can be represented as 2*23 + 1*(23*10) = 46 + 230 = 276.
        // in decimal, we "shift" by multiplying by 10, but in a binary field, we multiply by 2. also, addition of GF(2) values is XOR
        int product = 0;
        while(b != 0)
        {
          if((b & 1) != 0) product ^= a;
          b >>= 1;
          a *= 2;
          // the final caveat is that we must keep the result inside the field. the invariant we maintain is that a <= MaxValue. since
          // MaxValue is one less than a power of 2, that means we maintain that a does not have a 1 in a certain bit (the Power'th bit).
          // so anytime it gains a 1 there, we subtract Prime (with XOR), which also has a 1 in that place, thus removing the 1
          if((uint)a > (uint)MaxValue) a ^= Prime; // use an unsigned comparison in case we're dealing with the high (sign) bit
        }
        return product;
      }
    }

    /// <summary>Inverts a value in the field, equivalent to computing 0 - <paramref name="n"/>.</summary>
    /// <remarks>In GF(2^p) fields, negation is a no-op, so this method just returns the value given.</remarks>
    public int Negate(int n)
    {
      return n; // negation is 0 - n, and since subtraction is XOR and anything XOR'd with zero remains unchanged, it's a no-op
    }

    /// <summary>Parses a polynomial string of the form returned by <see cref="ToString"/>.</summary>
    public int Parse(string str)
    {
      int value;
      if(!TryParse(str, out value)) throw new FormatException();
      return value;
    }

    /// <summary>Computes a value within the field raised to a power.</summary>
    /// <param name="value">The value within the field to raise to a power</param>
    /// <param name="power">The power to raise the value to. This can be any integer value, including negative values.</param>
    public int Pow(int value, int power)
    {
      if(power < 0) power = MaxValue + power % MaxValue; // 2^-1 == 2^(MaxValue-1)

      if(value == Generator && expTable != null && power < expTable.Length) // special case powers of the generator polynomial
      {
        return expTable[power];
      }
      else // handle the general case of x^power
      {
        if(logTable != null && power <= 16843009) // avoid overflow
        {
          return expTable[(uint)(logTable[value]*power) % (uint)MaxValue];
        }
        else if(power <= 1) // handle value^0 and value^1
        {
          return power == 0 ? 1 : value;
        }
        else if(value == 1) // handle 1^power
        {
          return value;
        }
        else // handle other arguments
        {
          int result = 1;
          while(true)
          {
            if((power & 1) != 0) result = Multiply(result, value);
            power >>= 1;
            if(power == 0) break;
            value = Square(value);
          }
          return result;
        }
      }
    }

    /// <summary>Squares a value in the field. This is more efficient than multiplying it by itself.</summary>
    public int Square(int value)
    {
      if(logTable != null)
      {
        return value != 0 ? expTable[logTable[value]*2] : 0;
      }
      else if(value <= 65535)
      {
        return Reduce(SquareTable[value&0xFF] | (SquareTable[(value>>8)&0xFF]<<16));
      }
      else
      {
        uint low  = (uint)(SquareTable[value&0xFF] | (SquareTable[(value>>8)&0xFF]<<16));
        uint high = (uint)(SquareTable[(value>>16)&0xFF] | (SquareTable[value>>24]<<16));
        return Reduce((long)(((ulong)high<<32) | low));
      }
    }

    /// <summary>Subtracts one value from another, both of which are assumed to be within the field.</summary>
    /// <remarks>In GF(2^p) fields, subtraction and addition are the same operation.</remarks>
    public int Subtract(int a, int b)
    {
      return a ^ b;
    }

    /// <summary>Converts a value in the field to a polynomial string.</summary>
    /// <remarks>This method produces a string of the form "1 + x + x^2 + x^4".</remarks>
    public string ToString(int value)
    {
      if(value == 0) return "0";

      StringBuilder sb = new StringBuilder();
      for(int i=0, mask=1; value != 0; mask *= 2, i++)
      {
        if((value & mask) != 0)
        {
          if(sb.Length != 0) sb.Append(" + ");
          if(i != 0)
          {
            sb.Append('x');
            if(i > 1) sb.Append('^').Append(i.ToStringInvariant());
          }
          else
          {
            sb.Append('1');
          }

          value &= ~mask;
        }
      }
      return sb.ToString();
    }

    /// <summary>Attempts to parse a polynomial string of the form returned by <see cref="ToString"/>.</summary>
    public bool TryParse(string str, out int value)
    {
      if(str == null) throw new ArgumentNullException();

      value = 0;
      bool success = false;
      foreach(Term term in ParseTerms(str))
      {
        if(term.Power >= Power) return false;
        success = true;
        if((term.Coefficient & 1) != 0) value ^= 1 << term.Power;
      }

      return success;
    }

    #region Term
    internal struct Term
    {
      public Term(int coefficient, int power)
      {
        Coefficient = coefficient;
        Power       = power;
      }

      public readonly int Coefficient, Power;
    }
    #endregion

    /// <summary>Enumerates terms in a </summary>
    internal static IEnumerable<Term> ParseTerms(string str)
    {
      Match m = PolyRegex.Match(str);
      if(m.Success)
      {
        CaptureCollection captures = m.Groups["term"].Captures;
        for(int i=0; i<captures.Count; i++)
        {
          string termStr = captures[i].Value;
          int x = termStr.IndexOf('x'), coefficient, power;
          if(x < 0)
          {
            InvariantCultureUtility.TryParseExact(termStr, out coefficient);
            power = 0;
          }
          else
          {
            if(x == 0) coefficient = 1;
            else InvariantCultureUtility.TryParse(termStr, 0, x, out coefficient);

            if(x < termStr.Length - 2 && termStr[x+1] == '^')
            {
              InvariantCultureUtility.TryParse(termStr, x+2, termStr.Length - (x+2), out power);
            }
            else
            {
              power = 1;
            }
          }

          yield return new Term(coefficient, power);
        }
      }
    }

    internal readonly byte[] logTable, expTable;

    const int MaxLUTPower = 8, MaxPower = 31; // maximum powers the class can support

    /// <summary>Reduces a polynomial with a degree less than 2*<see name="Power"/>-1 to a degree less than <see cref="Power"/>, placing
    /// it in the field.
    /// </summary>
    int Reduce(int value)
    {
      if((uint)value > (uint)MaxValue)
      {
        for(int shift = Power-2, mask = 1<<(shift+Power); ; shift--, mask>>=1)
        {
          if((value & mask) != 0)
          {
            value ^= Prime << shift;
            if((uint)value <= (uint)MaxValue) break;
          }
        }
      }
      return value;
    }

    /// <summary>Reduces a polynomial with a degree less than 2*<see name="Power"/>-1 to a degree less than <see cref="Power"/>, placing
    /// it in the field.
    /// </summary>
    int Reduce(long value)
    {
      if(value > MaxValue)
      {
        for(long mask = 1L<<(Power*2-2), prime = (long)Prime << (Power-2); ; prime>>=1, mask>>=1)
        {
          if((value & mask) != 0)
          {
            value ^= prime;
            if(value <= MaxValue) break; ;
          }
        }
      }
      return (int)value;
    }

    /// <summary>Generates logarithm and exponent tables for the field.</summary>
    static void GenerateLogTables(int power, int prime, int generator, out byte[] logTable, out byte[] expTable)
    {
      int maxValue = (1<<power) - 1;
      byte[] logs = new byte[maxValue+1], exps = new byte[maxValue*2];
      for(int x=1, i=0; i < maxValue; i++)
      {
        logs[x] = (byte)i; // the log table holds the base-g logarithm of each value in the field. logs[0] is not used.
        exps[i] = (byte)x; // the exponent table holds g^i for each value i in the field
        x = Multiply(x, generator, prime, maxValue);
      }

      // the exponent table is doubled in length so that we can add two logarithms together and index without going off the end
      for(int i = maxValue; i < maxValue*2; i++) exps[i] = exps[i - maxValue];

      logTable = logs;
      expTable = exps;
    }

    static int[] MakeSquareTable()
    {
      // squaring a polynomial whose coefficients are in GF(2) is equivalent to inserting a 0 between each bit, because, for example:
      // (a0 + a1*x + a2*x^2)^2 = a0^2 + a0*a1*x + a0*a2*x^2 + a1*a0*x + a1*a1*x^2 + a1*a2*x^3 + a2*a0*x^2 + a2*a1*x^3 + a2*a2*x^4 =
      // a0*a0 + (a0*a1+a1*a0)*x + (a0*a2+a2*a0+a1*a1)*x^2 + (a1*a2+a2*a1)*x^3 + a2*a2*x^4 =
      // a0 + a1*x^2 + a2*x^4 (because * is AND and + is XOR in GF(2)). so, we'll compute a table containing for each byte the squared
      // value of that byte. field values larger than a byte can be processed as multiple bytes, since it's a purely bitwise operation
      int[] table = new int[256];
      int i;
      for(i=1; i<16; i++) // compute the first 16 values by looping over each bit
      {
        int value = 0;
        for(int mask=1, j=0; j<4; mask *= 2, j++) value |= (i & mask) << j;
        table[i] = value;
      }
      for(; i<table.Length; i++) table[i] = table[i&15] | (table[i>>4]<<8); // compute the rest of the values using the first 16
      return table;
    }

    /// <summary>Multiplies two numbers the slow way. This method is used before the lookup tables have been built.</summary>
    static int Multiply(int a, int b, int prime, int maxValue)
    {
      int product = 0;
      while(b != 0)
      {
        if((b & 1) != 0) product ^= a;
        b >>= 1;
        a *= 2;
        if((uint)a > (uint)maxValue) a ^= prime;
      }
      return product;
    }

    static readonly Regex PolyRegex = new Regex(@"^\s*[+-]?\s*(?<term>\d*\s*x(?:\^\d+)?|\d+)(?:\s*[+-]\s*(?<term>\d*\s*x(?:\^\d+)?|\d+))*\s*$",
                                                RegexOptions.Compiled | RegexOptions.ECMAScript);

    /// <summary>Default irreducible polynomials for each of the field sizes we support.</summary>
    static readonly int[] PrimePolys = new int[MaxPower]
    {
      3, 7, 11, 19, 37, 67, 131, 285, 529, 1033, 2053, 4179, 8219, 16427, 32771, 65581, 131081, 262183, 524327, 1048585, 2097157,
      4194307, 8388641, 16777243, 33554441, 67108935, 134217767, 268435465, 536870917, 1432791463, unchecked((int)2415919105)
    };

    static readonly int[] SquareTable = MakeSquareTable();
  }
  #endregion

  #region GF2pPolynomial
  /// <summary>Represents a polynomial whose coefficients are polynomials in the Galois field (of type <see cref="GF2pField"/>) for some
  /// power of two from 1 to 31, i.e. are in GF(2^1) to GF(2^31).
  /// </summary>
  /// <remarks>To create a polynomial whose coefficients are in GF(2), consider using <see cref="GF2pField"/>, which is much more efficient.</remarks>
  public struct GF2pPolynomial : IEquatable<GF2pPolynomial>
  {
    /// <summary>Initializes a new <see cref="GF2pPolynomial"/>.</summary>
    /// <param name="field">The field in which the polynomial's coefficients lie</param>
    /// <param name="value">The value of the polynomial's constant term, which must lie in the <paramref name="field"/></param>
    public GF2pPolynomial(GF2pField field, int value)
    {
      if(field == null) throw new ArgumentNullException();
      if(value != 0)
      {
        if((uint)value > field.MaxValue) throw new ArgumentOutOfRangeException("value", "must be within the field");
        data = new int[] { value };
      }
      else
      {
        data = null;
      }
      _field = field;
    }

    /// <summary>Initializes a new <see cref="GF2pPolynomial"/>.</summary>
    /// <param name="field">The field in which the polynomial's coefficients lie</param>
    /// <param name="coefficients">The coefficients of the polynomial, in order from the smallest to largest power. For example, to specify
    /// the polynomial 1 + 2x + 5x^3, you would pass {1, 2, 0, 5}. The coefficients must lie in the <paramref name="field"/>.
    /// </param>
    public GF2pPolynomial(GF2pField field, params int[] coefficients) : this(field, coefficients, true) { }

    internal GF2pPolynomial(GF2pField field, int[] coefficients, bool copyAndValidate)
    {
      if(field == null) throw new ArgumentNullException();
      int length = coefficients != null ? coefficients.Length : 0;
      while(length != 0 && coefficients[length - 1] == 0) length--; // trim trailing zeros
      if(length != 0)
      {
        if(copyAndValidate || length != coefficients.Length)
        {
          if(copyAndValidate)
          {
            for(int i=0; i<coefficients.Length; i++)
            {
              if((uint)coefficients[i] > field.MaxValue)
              {
                throw new ArgumentOutOfRangeException("coefficients", "The coefficients must be within the field.");
              }
            }
          }

          data = new int[length];
          Array.Copy(coefficients, this.data, length);
        }
        else
        {
          data = coefficients;
        }
      }
      else
      {
        data = null;
      }

      _field = field;
    }

    /// <summary>Gets a coefficient of the polynomial.</summary>
    /// <param name="index">The index of the coefficient. 0 refers to the coefficient of the constant term, 1 refers to the coefficient of
    /// the linear term, etc. This must be from 0 to <see cref="Length"/>-1.
    /// </param>
    public int this[int index]
    {
      get { return data[index]; }
    }

    /// <summary>Gets the degree of the polynomial.</summary>
    /// <remarks>This method returns <see cref="Length"/>-1, which is -1 if the polynomial is zero.</remarks>
    public int Degree
    {
      get { return data != null ? data.Length-1 : -1; }
    }

    /// <summary>Gets the <see cref="GF2pField"/> passed to the constructor, or null if the value was initialized to the default
    /// <see cref="GF2pPolynomial"/>.
    /// </summary>
    public GF2pField Field
    {
      get { return _field; }
    }

    /// <summary>Gets a value that indicates whether the polynomial is zero.</summary>
    public bool IsZero
    {
      get { return data == null; }
    }

    /// <summary>Gets the number of coefficients in the polynomial.</summary>
    public int Length
    {
      get { return data != null ? data.Length : 0; }
    }

    /// <summary>Returns the quotient and remainder after dividing this polynomial by another.</summary>
    /// <returns>Returns the quotient of the division. The remainder is placed in <paramref name="remainder"/>.</returns>
    public GF2pPolynomial DivRem(GF2pPolynomial divisor, out GF2pPolynomial remainder)
    {
      return DivRem(this, divisor, out remainder);
    }

    /// <summary>Determines whether the given object is a <see cref="GF2pPolynomial"/> equal to this one.</summary>
    public override bool Equals(object obj)
    {
      return obj is GF2pPolynomial && Equals((GF2pPolynomial)obj);
    }

    /// <summary>Determines whether the given polynomial equals this one.</summary>
    public bool Equals(GF2pPolynomial other)
    {
      if(data == null) return other.data == null;
      else if(other.data == null || data.Length != other.data.Length || Field.Prime != other.Field.Prime) return false;

      for(int i = 0; i < data.Length; i++)
      {
        if(data[i] != other.data[i]) return false;
      }
      return true;
    }

    /// <summary>Evaluates the polynomial for some value of its variable.</summary>
    /// <param name="x">The value of the variable in the polynomial, which must lie in the polynomial's <see cref="Field"/></param>
    public int Evaluate(int x)
    {
      if((uint)x > (uint)Field.MaxValue) throw new ArgumentOutOfRangeException("x", "must lie in the polynomial's field");
      if(x == 0 || IsZero) return 0;

      int i = data.Length-1, y = data[i--]; // use Horner's method to evaluate it in linear time
      byte[] expTable = Field.expTable, logTable = Field.logTable;
      if(logTable != null)
      {
        for(; i >= 0; i--)
        {
          int value = data[i];
          y = y != 0 ? value ^ expTable[logTable[x] + logTable[y]] : value;
        }
      }
      else
      {
        for(; i >= 0; i--) y = data[i] ^ Field.Multiply(x, y);
      }
      return y;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
      int hash = (Field != null ? Field.Prime : 0) ^ (Length << 16);
      if(data != null)
      {
        for(int shift = 0, maxShift = 32-Field.Power, shiftInc = (Field.Power+1)/2, i = 0; i < data.Length; i++)
        {
          hash ^= (data[i] ^ i) << shift; // shift the coefficients to ensure they cover more of the word,
          shift += shiftInc;              // so we can have more than 2^power hash codes
          if(shift >= maxShift) shift = 0;
        }
      }
      return hash;
    }

    /// <summary>Multiplies two polynomials and returns the value of a particular coefficient in the result, but does so much more quickly
    /// than the full multiplication operator.
    /// </summary>
    /// <param name="other">The polynomial to multiply against</param>
    /// <param name="index">The index of the coefficient to return. 0 refers to the coefficient of the constant term, 1 refers to the
    /// coefficient of the linear term, etc.
    /// </param>
    public int MultiplyAt(GF2pPolynomial other, int index)
    {
      return MultiplyAt(this, other, index);
    }

    /// <summary>Returns an array containing the coefficients of the polynomial from the lowest degree to the highest (i.e. in the same
    /// order as expected by the constructor).
    /// </summary>
    /// <remarks>The array will be empty if the polynomial is zero.</remarks>
    public int[] ToArray()
    {
      int[] array = new int[Length];
      if(data != null) Array.Copy(data, array, data.Length);
      return array;
    }

    /// <summary>Truncates the polynomial to a given length, returning the low order terms.</summary>
    public GF2pPolynomial Truncate(int length)
    {
      if(length >= Length) return this;
      if(length < 0) throw new ArgumentOutOfRangeException();
      while(length != 0 && data[length-1] == 0) length--; // trim trailing zeros from the result to avoid a copy in the constructor

      int[] truncated = null;
      if(length != 0)
      {
        truncated = new int[length];
        Array.Copy(data, truncated, length);
      }
      return new GF2pPolynomial(Field, truncated, false);
    }

    /// <summary>Converts the polynomial to a string.</summary>
    /// <remarks>This method produces a string of the form "7 + 2x + x^2 + 4x^4".</remarks>
    public override string ToString()
    {
      if(IsZero) return "0";

      StringBuilder sb = new StringBuilder();
      for(int i = 0; i < data.Length; i++)
      {
        if(data[i] != 0)
        {
          if(sb.Length != 0) sb.Append(" + ");
          if(i == 0 || data[i] > 1) sb.Append(data[i].ToStringInvariant());
          if(i != 0)
          {
            sb.Append('x');
            if(i > 1) sb.Append('^').Append(i.ToStringInvariant());
          }
        }
      }
      return sb.ToString();
    }

    /// <summary>Determines whether two polynomials are equal.</summary>
    public static bool operator==(GF2pPolynomial a, GF2pPolynomial b)
    {
      return a.Equals(b);
    }

    /// <summary>Determines whether two polynomials are equal.</summary>
    public static bool operator!=(GF2pPolynomial a, GF2pPolynomial b)
    {
      return !a.Equals(b);
    }

    /// <summary>Left-shifts the coefficients in the polynomial (so 1+3x might become 1x+3x^2), effectively multiplying it by
    /// x^<paramref name="shift"/>.
    /// </summary>
    public static GF2pPolynomial operator<<(GF2pPolynomial value, int shift)
    {
      if(!value.IsZero)
      {
        if(shift > 0)
        {
          int[] data = new int[value.data.Length + shift];
          Array.Copy(value.data, 0, data, shift, value.data.Length);
          value = new GF2pPolynomial(value.Field, data, false);
        }
        else if(shift < 0)
        {
          shift = -shift;
          if(shift > 0) value >>= shift;
          else value = new GF2pPolynomial(value.Field, 0); // handle underflow
        }
      }

      return value;
    }

    /// <summary>Right-shifts the coefficients in the polynomial (so 1x+3x^2 might become 1+3x), effectively dividing it by
    /// x^<paramref name="shift"/>. Lower order terms may be lost.
    /// </summary>
    public static GF2pPolynomial operator>>(GF2pPolynomial value, int shift)
    {
      if(!value.IsZero)
      {
        if(shift > 0)
        {
          if(shift < value.data.Length)
          {
            int[] data = new int[value.data.Length - shift];
            Array.Copy(value.data, shift, data, 0, data.Length);
            value = new GF2pPolynomial(value.Field, data, false);
          }
          else
          {
            value = new GF2pPolynomial(value.Field, 0);
          }
        }
        else if(shift < 0)
        {
          shift = -shift;
          if(shift < 0) throw new ArgumentOutOfRangeException(); // handle overflow
          value <<= shift;
        }
      }

      return value;
    }

    /// <summary>Negates a polynomial.</summary>
    /// <remarks>Negation in GF(2^p) fields is a no-op, so this method just returns the value given.</remarks>
    public static GF2pPolynomial operator-(GF2pPolynomial value)
    {
      return value;
    }

    /// <summary>Adds two polynomials.</summary>
    public static GF2pPolynomial operator+(GF2pPolynomial a, GF2pPolynomial b)
    {
      if(a.Length < b.Length) Utility.Swap(ref a, ref b);
      if(b.IsZero) return a;
      CheckPrimes(a.Field, b.Field);

      int[] result = new int[a.data.Length];
      for(int i = 0; i < b.data.Length; i++) result[i] = a.data[i] ^ b.data[i];
      if(a.data != null) Array.Copy(a.data, b.Length, result, b.Length, a.Length - b.Length);
      return new GF2pPolynomial(a.Field, result, false);
    }

    /// <summary>Subtracts one polynomial from another.</summary>
    /// <remarks>Subtraction in GF(2^p) fields is equivalent to addition.</remarks>
    public static GF2pPolynomial operator-(GF2pPolynomial a, GF2pPolynomial b)
    {
      return a + b;
    }

    /// <summary>Multiplies two polynomials.</summary>
    public static GF2pPolynomial operator*(GF2pPolynomial a, GF2pPolynomial b)
    {
      if(a.Length < b.Length) Utility.Swap(ref a, ref b);
      if(b.IsZero) return new GF2pPolynomial(a.Field, null, false); // can't use (a.Field, 0) because the field may be null if b is default
      CheckPrimes(a.Field, b.Field);
      if(b.data.Length == 1) return a * b.data[0];

      int[] result = new int[a.Length + b.Length - 1];
      byte[] expTable = a.Field.expTable, logTable = a.Field.logTable;
      for(int i = 0; i < a.data.Length; i++)
      {
        int av = a.data[i];
        if(av != 0)
        {
          if(logTable != null)
          {
            int aLog = logTable[av];
            for(int j = 0; j < b.data.Length; j++)
            {
              int bv = b.data[j];
              if(bv != 0) result[j+i] ^= expTable[logTable[bv] + aLog]; // r[j+i] = add(r[j+i], mul(a[i], b[j]))
            }
          }
          else
          {
            for(int j = 0; j < b.data.Length; j++)
            {
              int bv = b.data[j];
              if(bv != 0) result[j+i] ^= a.Field.Multiply(av, bv);
            }
          }
        }
      }

      return new GF2pPolynomial(a.Field, result, false);
    }

    /// <summary>Multiplies a polynomial by an integer, thereby scaling the coefficients.</summary>
    public static GF2pPolynomial operator*(GF2pPolynomial a, int b)
    {
      if(a.IsZero || b == 0) return new GF2pPolynomial(a.Field, null, false); // can't use (a.Field, 0) because the field may be null
      else if(b == 1) return a;

      int[] result = new int[a.Length];
      byte[] expTable = a.Field.expTable, logTable = a.Field.logTable;
      if(logTable != null)
      {
        int bLog = logTable[b];
        for(int i = 0; i < a.data.Length; i++)
        {
          int av = a.data[i];
          if(av != 0) result[i] ^= expTable[logTable[av] + bLog]; // r[i] = add(r[i], mul(a[i], b))
        }
      }
      else
      {
        for(int i = 0; i < a.data.Length; i++)
        {
          int av = a.data[i];
          if(av != 0) result[i] ^= a.Field.Multiply(av, b);
        }
      }

      return new GF2pPolynomial(a.Field, result, false);
    }

    /// <summary>Multiplies a polynomial by an integer, thereby scaling the coefficients.</summary>
    public static GF2pPolynomial operator*(int a, GF2pPolynomial b)
    {
      return b * a;
    }

    /// <summary>Divides one polynomial by another.</summary>
    public static GF2pPolynomial operator/(GF2pPolynomial numerator, GF2pPolynomial denominator)
    {
      if(denominator.IsZero) throw new DivideByZeroException();
      CheckPrimes(numerator.Field, denominator.Field);
      if(denominator.data.Length == 1) return numerator / denominator.data[0];

      int index = denominator.Degree;
      if(index >= numerator.Length) return new GF2pPolynomial(denominator.Field, 0);

      int[] result = DivRem(denominator.Field, numerator.data, denominator.data), quotient;
      if(index > 0)
      {
        quotient = new int[result.Length - index];
        Array.Copy(result, index, quotient, 0, quotient.Length);
      }
      else
      {
        quotient = result;
      }

      return new GF2pPolynomial(denominator.Field, quotient, false);
    }

    /// <summary>Divides a polynomial by an integer, thereby scaling the coefficients.</summary>
    public static GF2pPolynomial operator/(GF2pPolynomial numerator, int denominator)
    {
      if(denominator == 0) throw new DivideByZeroException();
      if(numerator.IsZero) return numerator;
      return new GF2pPolynomial(numerator.Field, Divide(numerator.Field, numerator.data, denominator), false);
    }

    /// <summary>Produces the remainder after dividing one polynomial by another.</summary>
    public static GF2pPolynomial operator%(GF2pPolynomial numerator, GF2pPolynomial denominator)
    {
      if(denominator.IsZero) throw new DivideByZeroException();
      CheckPrimes(numerator.Field, denominator.Field);

      int index = denominator.Degree;
      if(index >= numerator.Length) return numerator;
      else if(index == 0) return new GF2pPolynomial(denominator.Field, 0);
      int[] result = DivRem(denominator.Field, numerator.data, denominator.data), remainder = new int[index];
      Array.Copy(result, remainder, index);
      return new GF2pPolynomial(denominator.Field, remainder, false);
    }

    /// <summary>Returns the quotient and remainder after dividing one polynomial by another.</summary>
    /// <returns>Returns the quotient of the division. The remainder is placed in <paramref name="remainder"/>.</returns>
    public static GF2pPolynomial DivRem(GF2pPolynomial numerator, GF2pPolynomial denominator, out GF2pPolynomial remainder)
    {
      if(denominator.IsZero) throw new DivideByZeroException();
      CheckPrimes(numerator.Field, denominator.Field);

      int index = denominator.Degree;
      if(index >= numerator.Length)
      {
        remainder = numerator;
        return new GF2pPolynomial(denominator.Field, 0);
      }

      int[] result = DivRem(denominator.Field, numerator.data, denominator.data), quotient, rem;
      if(index > 0)
      {
        quotient = new int[result.Length - index];
        rem = new int[index];
        Array.Copy(result, rem, index);
        Array.Copy(result, index, quotient, 0, quotient.Length);
      }
      else
      {
        quotient = result;
        rem = null;
      }

      remainder = new GF2pPolynomial(denominator.Field, rem, false);
      return new GF2pPolynomial(denominator.Field, quotient, false);
    }

    /// <summary>Multiplies two polynomials and returns the value of a particular coefficient in the result, but does so much more quickly
    /// than the full multiplication operator.
    /// </summary>
    /// <param name="a">A polynomial to multiply</param>
    /// <param name="b">Another polynomial to multiply</param>
    /// <param name="index">The index of the coefficient to return. 0 refers to the coefficient of the constant term, 1 refers to the
    /// coefficient of the linear term, etc.
    /// </param>
    public static int MultiplyAt(GF2pPolynomial a, GF2pPolynomial b, int index)
    {
      if(index < 0) throw new ArgumentOutOfRangeException();
      if(index >= a.Length + b.Length - 1) return 0;
      if(a.Length < b.Length) Utility.Swap(ref a, ref b);
      if(b.IsZero) return 0;
      CheckPrimes(a.Field, b.Field);

      int result = 0, i = Math.Max(0, index+1-a.data.Length), length = Math.Min(index+1, b.data.Length);
      byte[] expTable = a.Field.expTable, logTable = a.Field.logTable;
      if(logTable != null)
      {
        for(; i<length; i++)
        {
          int bv = b.data[i];
          if(bv != 0)
          {
            int av = a.data[index-i];
            if(av != 0) result ^= expTable[logTable[av] + logTable[bv]];
          }
        }
      }
      else
      {
        for(; i<length; i++) result ^= a.Field.Multiply(b.data[i], a.data[index-i]);
      }
      return result;
    }

    /// <summary>Parses a polynomial string of the form returned by <see cref="ToString"/>.</summary>
    public static GF2pPolynomial Parse(GF2pField field, string str)
    {
      GF2pPolynomial value;
      if(!TryParse(field, str, out value)) throw new FormatException();
      return value;
    }

    /// <summary>Attempts to parse a polynomial string of the form returned by <see cref="ToString"/>.</summary>
    public static bool TryParse(GF2pField field, string str, out GF2pPolynomial value)
    {
      if(field == null || str == null) throw new ArgumentNullException();

      value = default(GF2pPolynomial);
      List<GF2pField.Term> terms = new List<GF2pField.Term>(GF2pField.ParseTerms(str));
      int length = 0;
      foreach(GF2pField.Term term in terms)
      {
        if(term.Coefficient > field.MaxValue) return false;
        length = Math.Max(length, term.Power+1);
      }

      if(terms.Count != 0)
      {
        if(length == 0)
        {
          value = new GF2pPolynomial(field, 0);
        }
        else
        {
          int[] coefficients = new int[length];
          foreach(GF2pField.Term term in terms) coefficients[term.Power] ^= term.Coefficient;
          value = new GF2pPolynomial(field, coefficients, false);
        }
      }

      return terms.Count != 0;
    }

    internal readonly int[] data;
    readonly GF2pField _field;

    static void CheckPrimes(GF2pField field1, GF2pField field2)
    {
      if(field1 != null && field2 != null && field1.Prime != field2.Prime) // TODO: should they have to use the same generator, too?
      {
        throw new ArgumentException("The polynomial fields must use the same prime.");
      }
    }

    static int[] Divide(GF2pField field, int[] numerator, int denominator)
    {
      int[] result = new int[numerator.Length];
      byte[] expTable = field.expTable, logTable = field.logTable;
      if(logTable != null)
      {
        int normOffset = (int)field.Order - 1 - logTable[denominator];
        for(int i = 0; i < result.Length; i++)
        {
          int nv = numerator[i];
          if(nv != 0) result[i] = expTable[logTable[nv] + normOffset]; // result[i] = Divide(numerator[i], denominator)
        }
      }
      else
      {
        for(int inverse = field.Invert(denominator), i = 0; i < result.Length; i++)
        {
          int nv = numerator[i];
          if(nv != 0) result[i] = field.Multiply(nv, inverse);
        }
      }

      return result;
    }

    // Divides numerator by denominator and returns a single array containing the remainder (with a length of denominator.Length-1)
    // followed by the quotient (with a length of numerator.Length - (denominator.Length-1)).
    static int[] DivRem(GF2pField field, int[] numerator, int[] denominator)
    {
      int[] result = new int[numerator.Length];
      Array.Copy(numerator, result, numerator.Length); // start with the numerator at the beginning of the array, followed by zeros
      // since we're doing synthetic division, we want to divide the numerator by the denominator's highest order coefficient (norm)
      int normi = denominator.Length - 1, norm = denominator[normi]; // normi is the index of that coefficient and the remainder length too
      byte[] expTable = field.expTable, logTable = field.logTable;
      // if we have lookup tables, we'll compute the offset into the exponent table needed to divide by norm. otherwise we'll compute
      // the multiplicative inverse of the norm so we can divide by it using a multiplication
      if(logTable != null) norm = field.MaxValue - logTable[norm]; // if norm is 1, norm becomes field.MaxValue
      else norm = field.Invert(norm); // if norm is 1, Invert(norm) is also 1
      for(int i = numerator.Length - 1 - normi; i >= 0; i--)
      {
        int nv = result[i + normi];
        if(nv != 0)
        {
          if(logTable != null)
          {
            int nLog = logTable[nv];
            if(norm != field.MaxValue) // if the normalizer isn't 1 (so there's a point to dividing by it)...
            {
              result[i + normi] = nv = expTable[nLog + norm]; // result[i + normi] = nv = Divide(nv, norm)
              nLog = logTable[nv]; // recompute the logarithm of nv, since it changed
            }
            for(int j = 0; j < denominator.Length - 1; j++)
            {
              int dv = denominator[j];
              if(dv != 0) result[j + i] ^= expTable[logTable[dv] + nLog]; // result[j+i] -= Multiply(nv, dv)
            }
          }
          else
          {
            if(norm != 1) result[i + normi] = nv = field.Multiply(nv, norm); // = Divide(nv, norm)
            for(int j = 0; j < denominator.Length - 1; j++)
            {
              int dv = denominator[j];
              if(dv != 0) result[j + i] ^= field.Multiply(nv, dv);
            }
          }
        }
      }

      return result;
    }
  }
  #endregion
}
