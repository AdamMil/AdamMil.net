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
using System.Diagnostics;
using System.Globalization;
using System.Text;
using AdamMil.IO;
using AdamMil.Mathematics.Random;
using AdamMil.Utilities;

// TODO: switch to unsafe code (if it's faster)

namespace AdamMil.Mathematics
{
  /// <summary>This class implements an arbitrary-precision integer type capable of storing extremely large positive and negative numbers.</summary>
  /// <remarks>This implementation limits numbers to 2147483647 bits in length, allowing it to store values up to nearly 10^646456993.</remarks>
  // NOTE: we don't derive from IEquatable<int>, IEquatable<long>, etc. because that assumes our GetHashCode implementation matches that of
  // the runtime for those types, which may not be the case. IComparable<int>, etc. don't really seem useful either...
  [Serializable]
  public struct Integer : ICloneable, IComparable, IComparable<Integer>, IConvertible, IEquatable<Integer>, IFormattable
  {
    /// <summary>Initializes a new <see cref="Integer"/> value from the given <see cref="int"/> value.</summary>
    public Integer(int value)
    {
      if(value == 0)
      {
        data = null;
        info = 0;
      }
      else
      {
        uint uValue = value > 0 ? (uint)value : (uint)-value;
        if(uValue == 1) // literal 1 values are common, so avoid the allocation if we can
        {
          data = One.data;
          info = value > 0 ? 1 : SignBit|1;
        }
        else
        {
          data = new uint[] { uValue };
          info = (uint)ComputeBitLength(uValue);
          if(value < 0) info |= SignBit;
        }
      }
    }

    /// <summary>Initializes a new <see cref="Integer"/> value from the given <see cref="uint"/> value.</summary>
    [CLSCompliant(false)]
    public Integer(uint value)
    {
      if(value == 0)
      {
        data = null;
        info = 0;
      }
      else if(value == 1) // literal 1 values are common, so avoid the allocation if we can
      {
        data = One.data;
        info = 1;
      }
      else
      {
        data = new uint[] { value };
        info = (uint)ComputeBitLength(value);
      }
    }

    /// <summary>Initializes a new <see cref="Integer"/> value from the given <see cref="long"/> value.</summary>
    public Integer(long value)
    {
      ulong uValue;
      if(value > 0)
      {
        uValue = (ulong)value;
        info   = 0;
      }
      else if(value < 0)
      {
        uValue = (ulong)-value; // this is correct even if value == long.MinValue
        info   = SignBit;
      }
      else
      {
        data = null;
        info = 0;
        return;
      }

      if(uValue > uint.MaxValue) data = new uint[] { (uint)uValue, (uint)(uValue>>32) };
      else data = new uint[] { (uint)uValue };
      info |= (uint)ComputeBitLength(uValue);
    }

    /// <summary>Initializes a new <see cref="Integer"/> value from the given <see cref="ulong"/> value.</summary>
    [CLSCompliant(false)]
    public Integer(ulong value)
    {
      if(value > uint.MaxValue)
      {
        data = new uint[] { (uint)value, (uint)(value>>32) };
        info = (uint)ComputeBitLength(value);
      }
      else
      {
        uint shortValue = (uint)value;
        if(shortValue != 0)
        {
          data = new uint[] { shortValue };
          info = (uint)ComputeBitLength(shortValue);
        }
        else
        {
          data = null;
          info = 0;
        }
      }
    }

    /// <summary>Initializes a new <see cref="Integer"/> value from the given <see cref="decimal"/>. The number will be converted to an
    /// integer by truncating it rather than rounding it, so any fractional part will be lost.
    /// </summary>
    public Integer(decimal value)
    {
      // a Decimal is a base-10 floating-point value. extract the base-10 mantissa and exponent
      int[] bits = Decimal.GetBits(value);
      int negExponent = ((bits[3]>>16) & 0xFF);
      bool negative = (bits[3]>>31) != 0;
      if(bits[2] == 0) // if the mantissa fits within 64 bits...
      {
        ulong mantissa = (uint)bits[0] | ((ulong)bits[1] << 32); // use 64-bit math to compute the value
        for(; negExponent > 0; negExponent -= 19) mantissa /= PowersOf10[Math.Min(negExponent, 19)]; // 10^19 fits in a ulong
        this = new Integer(mantissa);
        if(negative) info |= SignBit;
      }
      else // otherwise, the mantissa requires 96 bits, so we'll use arbitrary-precision math
      {
        this = new Integer(new uint[] { (uint)bits[0], (uint)bits[1], (uint)bits[2] }, negative, false);
        for(; negExponent > 0; negExponent -= 9) UnsafeDivide((uint)PowersOf10[Math.Min(negExponent, 9)]); // 10^9 fits in a uint
      }
    }

    /// <summary>Initializes a new <see cref="Integer"/> value from the given floating-point number. The number will be converted to an
    /// integer by truncating it rather than rounding it, so any fractional part will be lost.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is <see cref="double.PositiveInfinity"/>,
    /// <see cref="double.NegativeInfinity"/>, or <see cref="double.NaN"/>.
    /// </exception>
    public Integer(double value)
    {
      ulong mantissa;
      int exponent;
      bool negative;
      if(!IEEE754.Decompose(value, out negative, out exponent, out mantissa)) throw new ArgumentOutOfRangeException();

      // now the value equals mantissa * 2^exponent. if the exponent is negative, we can shift mantissa to the right to obtain the
      // integer portion. if the exponent is positive, we can shift to the left. the mantissa has at most 53 bits set.
      if(exponent <= 0)
      {
        int shift = Math.Min(53, -exponent);
        mantissa >>= shift;
        exponent += shift;
        if(mantissa == 0)
        {
          data = null;
          info = 0;
          return;
        }
        else
        {
          this = new Integer(mantissa);
        }
      }
      else if(exponent <= 11) // if the exponent is small enough, we can do the shift within the ulong
      {
        this = new Integer(mantissa << exponent);
      }
      else // otherwise, we'll do the shift within the Integer
      {
        this = new Integer(mantissa);
        UnsafeLeftShift(exponent);
      }
      if(negative) info |= SignBit;
    }

    /// <summary>Initializes a new <see cref="Integer"/> value from the given floating-point number. The number will be converted to an
    /// integer by truncating it rather than rounding it, so any fractional part will be lost.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is <see cref="FP107.PositiveInfinity"/>,
    /// <see cref="FP107.NegativeInfinity"/>, or <see cref="FP107.NaN"/>.
    /// </exception>
    public Integer(FP107 value)
    {
      int exponent;
      bool negative;
      if(!value.Decompose(out negative, out exponent, out this)) throw new ArgumentOutOfRangeException();
      UnsafeLeftShift(exponent); // the exponent is likely negative, making this a right shift that can be done in-place
      if(negative && !IsZero) info |= SignBit;
    }

    /// <summary>Initializes a new <see cref="Integer"/> from the given value, using freshly allocated internal storage, so that this value
    /// can be safely used with in-place operations like <see cref="UnsafeIncrement"/>.
    /// </summary>
    public Integer(Integer value)
    {
      if(value.data == null)
      {
        data = null;
      }
      else
      {
        data = new uint[value.GetElementCount()];
        value.data.FastCopy(data, data.Length);
      }
      info = value.info;
    }

    /// <summary>Initializes a new <see cref="Integer"/> value from a <see cref="BinaryReader"/>. The value is expected to have been saved
    /// with the <see cref="Save"/> method.
    /// </summary>
    public Integer(BinaryReader reader)
    {
      if(reader == null) throw new ArgumentNullException();
      info = reader.ReadUInt32();
      data = info == 0 ? null : reader.ReadUInt32s(GetElementCount((int)(info & 0x7FFFFFFF)));
    }

    /// <summary>Initializes a new <see cref="Integer"/> value from an array containing the bits for the magnitude of the value, plus a
    /// boolean indicating whether the value is negative. The first element should contain the 32 least significant bits, and so on.
    /// </summary>
    [CLSCompliant(false)]
    public Integer(uint[] bits, bool negative) : this(bits, negative, true) { }

    internal Integer(uint[] array, bool negative, bool clone)
    {
      if(array != null)
      {
        for(int i = array.Length-1; i >= 0; i--)
        {
          uint value = array[i];
          if(value != 0)
          {
            if(clone)
            {
              uint[] newArray = new uint[i+1];
              array.FastCopy(newArray, newArray.Length);
              array = newArray;
            }

            data = array;
            info = (uint)(ComputeBitLength(value) + i*32);
            if(negative) info |= SignBit;
            return;
          }
        }
      }

      data = null;
      info = 0;
    }

    Integer(uint[] array, uint info)
    {
      this.data = array;
      this.info = info;
    }

    Integer(uint[] array, int bitLength, bool negative)
    {
      this.data = array;
      this.info = (uint)bitLength;
      if(negative && bitLength != 0) this.info |= SignBit;
    }

    /// <summary>Gets the number of bits needed to store the magnitude of this <see cref="Integer"/> value. As this does not count any
    /// leading zeros, if one <see cref="Integer"/> has a larger bit length than another, then it must be greater in magnitude (although
    /// it could be opposite in sign), and vice versa.
    /// </summary>
    public int BitLength
    {
      get { return (int)(info & 0x7FFFFFFF); }
      private set { info = value == 0 ? 0 : info & SignBit | (uint)value; }
    }

    /// <summary>Determines whether this <see cref="Integer"/> value is even. Zero is considered even.</summary>
    public bool IsEven
    {
      get { return data == null || (data[0] & 1) == 0; }
    }

    /// <summary>Determines whether this <see cref="Integer"/> value is negative.</summary>
    public bool IsNegative
    {
      get { return (int)info < 0; }
    }

    /// <summary>Determines whether this <see cref="Integer"/> value is positive.</summary>
    public bool IsPositive
    {
      get { return (int)info > 0; }
    }

    /// <summary>Determines whether this <see cref="Integer"/> value is equal to zero.</summary>
    public bool IsZero
    {
      get { return info == 0; }
    }

    /// <summary>Returns the sign of this <see cref="Integer"/>, 1 if it's positive, -1 if it's negative, and 0 if it's zero.</summary>
    public int Sign
    {
      get { return Math.Sign((int)info); }
    }

    /// <summary>Returns an <see cref="Integer"/> value with the same magnitude and a non-negative sign.</summary>
    public Integer Abs()
    {
      return new Integer(data, info & ~SignBit);
    }

    /// <summary>Returns a new <see cref="Integer"/> with the same value but whose internal storage is newly allocated, allowing it to be
    /// safely used with in-place operations like <see cref="UnsafeIncrement"/>.
    /// </summary>
    public Integer Clone()
    {
      return new Integer(this);
    }

    /// <summary>Compares this <see cref="Integer"/> value to an <see cref="int"/> value and returns a positive number if this value is
    /// greater, a negative number if this value is less, or zero if the values are equal.
    /// </summary>
    public int CompareTo(int other)
    {
      if(other > 0) return !IsPositive ? -1 : info >= 32 ? 1 : (int)data[0] - other;
      else if(other == 0) return Sign;
      else if(!IsNegative) return 1;
      else if(BitLength > 32) return -1; // if other is negative, it may need 32 bits to store its magnitude rather than 31
      uint value = data[0];
      other = -other; // convert 'other' to its magnitude (when considered as an unsigned number)
      return value > (uint)other ? -1 : value < (uint)other ? 1 : 0;
    }

    /// <summary>Compares this <see cref="Integer"/> value to a <see cref="uint"/> value and returns a positive number if this value is
    /// greater, a negative number if this value is less, or zero if the values are equal.
    /// </summary>
    [CLSCompliant(false)]
    public int CompareTo(uint other)
    {
      if(IsNegative) return -1;
      else if(info > 32) return 1;
      uint value = ToUInt32Fast();
      return value > other ? 1 : value < other ? -1 : 0;
    }

    /// <summary>Compares this <see cref="Integer"/> value to a <see cref="long"/> value and returns a positive number if this value is
    /// greater, a negative number if this value is less, or zero if the values are equal.
    /// </summary>
    public int CompareTo(long other)
    {
      if(other >= 0)
      {
        if(IsNegative) return -1;
        else if(info >= 64) return 1;
        ulong value = ToUInt64Fast();
        return value > (ulong)other ? 1 : value < (ulong)other ? -1 : 0;
      }
      else if(!IsNegative) return 1;
      else if(BitLength > 64) return -1; // if other is negative, it may need 64 bits to store its magnitude rather than 63
      else
      {
        ulong value = ToNonZeroUInt64Fast();
        other = -other; // convert 'other' to its magnitude (when considered as an unsigned number)
        return value > (ulong)other ? -1 : value < (ulong)other ? 1 : 0;
      }
    }

    /// <summary>Compares this <see cref="Integer"/> value to a <see cref="ulong"/> value and returns a positive number if this value is
    /// greater, a negative number if this value is less, or zero if the values are equal.
    /// </summary>
    [CLSCompliant(false)]
    public int CompareTo(ulong other)
    {
      if(IsNegative) return -1;
      else if(info > 64) return 1;
      ulong value = ToUInt64Fast();
      return value > other ? 1 : value < other ? -1 : 0;
    }

    /// <summary>Compares this <see cref="Integer"/> value to another and returns a positive number if this value is greater, a negative
    /// number if this value is less, or zero if the values are equal.
    /// </summary>
    public int CompareTo(Integer other)
    {
      if(info != other.info)
      {
        if(IsPositive) return other.IsPositive ? (int)(info - other.info) : 1; // if both positive, compare bit lengths
        else if(IsNegative) return other.IsNegative ? (int)(other.info - info) : -1; // if both negative, compare bit lengths (reversed)
        else return -(int)other.info; // zero. return positive if the other is negative and vice versa. (info can never be int.MinValue)
      }
      else if(!IsZero)
      {
        // the values have the same sign and bit length and neither are zero
        int i = GetElementCount()-1;
        do
        {
          if(data[i] != other.data[i]) return IsPositive ^ (data[i] < other.data[i]) ? 1 : -1;
        } while(--i >= 0);
      }
      return 0;
    }

    /// <summary>Counts the number of trailing zero bits in the binary representation of the integer.</summary>
    /// <remarks>This method returns zero if the value is zero.</remarks>
    public int CountTrailingZeros()
    {
      return CountTrailingZeros(this);
    }

    /// <summary>Divides this value by the given divisor, returns the quotient, and stores the remainder in <paramref name="remainder"/>.</summary>
    public Integer DivRem(Integer divisor, out Integer remainder)
    {
      return DivRem(this, divisor, out remainder);
    }

    /// <summary>Determines whether this <see cref="Integer"/> equals the given object. If the object is not an <see cref="Integer"/>,
    /// false will be returned.
    /// </summary>
    public override bool Equals(object obj)
    {
      return obj is Integer && Equals((Integer)obj);
    }

    /// <summary>Determines whether this <see cref="Integer"/> equals the given value.</summary>
    public bool Equals(Integer value)
    {
      if(info != value.info) return false;
      if(!IsZero)
      {
        int i = 0, count = GetElementCount();
        do
        {
          if(data[i] != value.data[i]) return false;
        } while(++i < count);
      }
      return true;
    }

    /// <summary>Determines whether this <see cref="Integer"/> equals the given <see cref="int"/> value.</summary>
    public bool Equals(int value)
    {
      if(IsPositive) return info < 32 && data[0] == (uint)value;
      else if(IsNegative) return value < 0 && data[0] == (uint)(-value) && BitLength <= 32;
      else return value == 0;
    }

    /// <summary>Determines whether this <see cref="Integer"/> equals the given <see cref="uint"/> value.</summary>
    [CLSCompliant(false)]
    public bool Equals(uint value)
    {
      if(!IsZero) return info <= 32 && data[0] == value;
      else return value == 0;
    }

    /// <summary>Determines whether this <see cref="Integer"/> equals the given <see cref="long"/> value.</summary>
    public bool Equals(long value)
    {
      if(IsPositive) return info < 64 && ToNonZeroUInt64Fast() == (ulong)value;
      else if(IsNegative) return BitLength <= 64 && value < 0 && ToNonZeroUInt64Fast() == (ulong)(-value);
      else return value == 0;
    }

    /// <summary>Determines whether this <see cref="Integer"/> equals the given <see cref="ulong"/> value.</summary>
    [CLSCompliant(false)]
    public bool Equals(ulong value)
    {
      if(!IsZero) return info <= 64 && ToNonZeroUInt64Fast() == value;
      else return value == 0;
    }

    /// <summary>Gets the value of a bit, where zero is the least significant bit.</summary>
    /// <remarks>This method allows you to read bits beyond those actually stored. (I.e. <paramref name="index"/> can be greater than or
    /// equal to <see cref="BitLength"/>.)
    /// <include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/>
    /// </remarks>
    public bool GetBit(int index)
    {
      if((uint)index >= (uint)BitLength) // if they're attempting to read out of bounds...
      {
        if(index < 0) throw new ArgumentOutOfRangeException();
        return IsNegative; // then positive values are zero-extended and negative values are one-extended, emulating two's complement
      }
      else if(IsPositive) // if the value is positive (zero is handled above), we simply return the bit as-is
      {
        return (data[index>>5] & (1u<<(index&31))) != 0;
      }
      else // otherwise, if the value is negative, we have to emulate two's complement
      {
        int i, wordIndex = index>>5;
        // we have to pretend that negative values are stored in two's complement. we do this by effectively subtracting 1 from the value
        // and negating the bits. we first have to determine whether the subtraction is still borrowing when it gets to the desired word
        bool borrow = true;
        for(i=0; i<wordIndex; i++)
        {
          if(data[i] != 0) { borrow = false; break; }
        }
        uint v = data[wordIndex];
        if(borrow) v--;
        return (v & (1u<<(index&31))) == 0; // invert the comparison rather than negating the bits to save an operation
      }
    }

    /// <summary>Returns an array of <see cref="uint"/> that represents the magnitude of the <see cref="Integer"/>. This array can be
    /// passed to the <see cref="Integer(uint[],bool)"/> constructor to recreate the value.
    /// </summary>
    /// <remarks>The magnitude of the integer equals bits[0]*2^(32*0) + bits[1]*2^(32*1) + bits[2]*2^(32*2) + ... + bits[n]*2^(32*n).</remarks>
    [CLSCompliant(false)]
    public uint[] GetBits()
    {
      bool isNegative;
      return GetBits(out isNegative);
    }

    /// <summary>Returns an array of <see cref="uint"/> that represents the magnitude of the <see cref="Integer"/> as well as a boolean
    /// that indicates whether the value is negative. This array and boolean can be passed to the <see cref="Integer(uint[],bool)"/>
    /// constructor to recreate the value.
    /// </summary>
    /// <remarks>The magnitude of the integer equals bits[0]*2^(32*0) + bits[1]*2^(32*1) + bits[2]*2^(32*2) + ... + bits[n]*2^(32*n).</remarks>
    [CLSCompliant(false)]
    public uint[] GetBits(out bool isNegative)
    {
      isNegative = IsNegative;
      return data == null ? new uint[0] : (uint[])data.Clone();
    }

    /// <include file="documentation.xml" path="/Math/Common/GetHashCode/*"/>
    public override int GetHashCode()
    {
      int hash = 0;
      if(data != null)
      {
        for(int i=0; i<data.Length; i++) hash ^= (int)data[i];
        if(IsNegative) hash = -hash;
      }
      return hash;
    }

    /// <summary>Returns this value raised to the given power.</summary>
    public Integer Pow(int power)
    {
      return Pow(this, power);
    }

    /// <summary>Saves this value to a <see cref="BinaryWriter"/>. The value can be recreated using the
    /// <see cref="Integer(BinaryReader)"/> constructor.
    /// </summary>
    public void Save(BinaryWriter writer)
    {
      if(writer == null) throw new ArgumentNullException();
      writer.Write(info);
      if(!IsZero) writer.Write(data, 0, GetElementCount());
    }

    /// <summary>Returns the square of this value, which is equal to the value multiplied by itself.</summary>
    public Integer Square()
    {
      return this * this; // TODO: see if there's a more efficient implementation
    }

    /// <summary>Converts this <see cref="Integer"/> to an <see cref="int"/>. If the value cannot be represented, an exception will be
    /// thrown. To convert out-of-bounds values to <see cref="int"/>, use a cast instead, i.e. <c>(int)value</c>.
    /// </summary>
    /// <exception cref="OverflowException">Thrown if the value cannot be represented as an <see cref="int"/>.</exception>
    public int ToInt32()
    {
      if(BitLength <= 32)
      {
        uint abs = ToUInt32Fast();
        if(IsNegative)
        {
          if(abs <= (uint)int.MaxValue+1) return -(int)abs;
        }
        else
        {
          if(abs <= int.MaxValue) return (int)abs;
        }
      }

      throw new OverflowException();
    }

    /// <summary>Converts this <see cref="Integer"/> to a <see cref="long"/>. If the value cannot be represented, an exception will be
    /// thrown. To convert out-of-bounds values to <see cref="long"/>, use a cast instead, i.e. <c>(long)value</c>.
    /// </summary>
    /// <exception cref="OverflowException">Thrown if the value cannot be represented as a <see cref="long"/>.</exception>
    public long ToInt64()
    {
      if(BitLength <= 64)
      {
        ulong abs = ToUInt64Fast();
        if(IsNegative)
        {
          if(abs <= (ulong)long.MaxValue+1) return -(long)abs;
        }
        else
        {
          if(abs <= long.MaxValue) return (long)abs;
        }
      }

      throw new OverflowException();
    }

    /// <summary>Converts this <see cref="Integer"/> value to string using the conventions of the current culture.</summary>
    public override string ToString()
    {
      return ToString(null, null);
    }

    /// <summary>Converts this <see cref="Integer"/> value to string using the given format and the conventions of the current culture.</summary>
    public string ToString(string format)
    {
      return ToString(format, null);
    }

    /// <summary>Converts this <see cref="Integer"/> value to string using the conventions of the given format provider, or the current
    /// culture if the provider is null.
    /// </summary>
    public string ToString(IFormatProvider provider)
    {
      return ToString(null, provider);
    }

    /// <summary>Converts this <see cref="Integer"/> value to string using the given format and the conventions of the given format
    /// provider, or the current culture if the provider is null.
    /// </summary>
    public string ToString(string format, IFormatProvider provider)
    {
      int desiredPrecision;
      char formatType;
      bool capitalize;
      if(!NumberFormat.ParseFormatString(format, 'D', out formatType, out desiredPrecision, out capitalize))
      {
        throw new FormatException("Unsupported format string: " + format);
      }

      NumberFormatInfo nums = NumberFormatInfo.GetInstance(provider);
      if(formatType == 'X') // if hexadecimal format was requested...
      {
        if(IsZero)
        {
          return "0x0";
        }
        else
        {
          StringBuilder sb = new StringBuilder();
          if(IsNegative) sb.Append(nums.NegativeSign);
          sb.Append("0x");
          int i = GetElementCount()-1;
          ToHex(sb, data[i], capitalize, false);
          for(i--; i >= 0; i--) ToHex(sb, data[i], capitalize, true);
          return sb.ToString();
        }
      }
      else
      {
        byte[] digits = GetDigits(this);
        return NumberFormat.FormatNumber(digits, digits.Length, IsNegative, nums, formatType, desiredPrecision, -1, capitalize);
      }
    }

    /// <summary>Converts this <see cref="Integer"/> to a <see cref="uint"/>. If the value cannot be represented, an exception will be
    /// thrown. To convert out-of-bounds values to <see cref="uint"/>, use a cast instead, i.e. <c>(uint)value</c>.
    /// </summary>
    /// <exception cref="OverflowException">Thrown if the value cannot be represented as a <see cref="uint"/>.</exception>
    [CLSCompliant(false)]
    public uint ToUInt32()
    {
      if(info > 32) throw new OverflowException();
      return ToUInt32Fast();
    }

    /// <summary>Converts this <see cref="Integer"/> to a <see cref="ulong"/>. If the value cannot be represented, an exception will be
    /// thrown. To convert out-of-bounds values to <see cref="ulong"/>, use a cast instead, i.e. <c>(ulong)value</c>.
    /// </summary>
    /// <exception cref="OverflowException">Thrown if the value cannot be represented as a <see cref="ulong"/>.</exception>
    [CLSCompliant(false)]
    public ulong ToUInt64()
    {
      if(info > 64) throw new OverflowException();
      return ToUInt64Fast();
    }

    /// <summary>Adds a value to this <see cref="Integer"/> in place. If this integer shares storage with others, the others may be
    /// corrupted.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    public void UnsafeAdd(int value)
    {
      if(value != 0)
      {
        bool negativeValue = value < 0;
        if(negativeValue) value = -value;
        if(IsNegative == negativeValue) UnsafeAddMagnitude((uint)value);
        else UnsafeSubtractCore((uint)value);
      }
    }

    /// <summary>Adds a value to this <see cref="Integer"/> in place. If this integer shares storage with others, the others may be
    /// corrupted.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    [CLSCompliant(false)]
    public void UnsafeAdd(uint value)
    {
      if(value != 0)
      {
        if(!IsNegative) UnsafeAddMagnitude(value);
        else UnsafeSubtractCore(value);
      }
    }

    /// <summary>Bitwise-ANDs this value with the given integer, in place. If this integer shares storage with others, the others may be
    /// corrupted.
    /// </summary>
    /// <remarks>
    /// <include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/remarks/*"/>
    /// </remarks>
    public void UnsafeBitwiseAnd(int value)
    {
      UnsafeBitwiseAnd((uint)value, value < 0);
    }

    /// <summary>Bitwise-ANDs this value with the given integer, in place. If this integer shares storage with others, the others may be
    /// corrupted.
    /// </summary>
    /// <remarks>
    /// <include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/remarks/*"/>
    /// </remarks>
    [CLSCompliant(false)]
    public void UnsafeBitwiseAnd(uint value)
    {
      UnsafeBitwiseAnd(value, false);
    }

    /// <summary>Bitwise-ORs this value with the given integer, in place. If this integer shares storage with others, the others may be
    /// corrupted.
    /// </summary>
    /// <remarks>
    /// <include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/remarks/*"/>
    /// </remarks>
    public void UnsafeBitwiseOr(int value)
    {
      UnsafeBitwiseOr((uint)value, value < 0);
    }

    /// <summary>Bitwise-ORs this value with the given integer, in place. If this integer shares storage with others, the others may be
    /// corrupted.
    /// </summary>
    /// <remarks>
    /// <include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/remarks/*"/>
    /// </remarks>
    [CLSCompliant(false)]
    public void UnsafeBitwiseOr(uint value)
    {
      UnsafeBitwiseOr(value, false);
    }

    /// <summary>Negates this integer, bitwise and in place. If this integer shares storage with others, the others may be corrupted.</summary>
    /// <remarks>
    /// <include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/remarks/*"/>
    /// </remarks>
    public void UnsafeBitwiseNegate()
    {
      // ~x == -(x+1)
      if(IsPositive)
      {
        UnsafeIncrementMagnitude();
        info ^= SignBit;
      }
      else if(IsNegative)
      {
        UnsafeDecrementMagnitude();
        if(!IsZero) info ^= SignBit;
      }
      else
      {
        this = MinusOne;
      }
    }

    /// <summary>Decrements this <see cref="Integer"/> in place. If this integer shares storage with others, the others may be corrupted.</summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    public void UnsafeDecrement()
    {
      if(IsPositive)
      {
        if(BitLength == 1) MakeZero();
        else UnsafeDecrementMagnitude();
      }
      else if(IsNegative)
      {
        UnsafeIncrementMagnitude();
      }
      else
      {
        this = MinusOne;
      }
    }

    /// <summary>Divides this <see cref="Integer"/> in place by the given value and returns the remainder. If this integer shares storage
    /// with others, the others may be corrupted.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    public int UnsafeDivide(int value)
    {
      int remainder = (int)UnsafeDivide((uint)(value < 0 ? -value : value)); // divide by the magnitude
      if(value < 0 && !IsZero) info ^= SignBit;
      return remainder;
    }

    /// <summary>Divides this <see cref="Integer"/> in place by the given value and returns the remainder. If this integer shares storage
    /// with others, the others may be corrupted.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    [CLSCompliant(false)]
    public long UnsafeDivide(uint value) // this must return a long because the remainder can be from -(2^32-1) to 2^32-1
    {
      long remainder;
      int cmp = CompareMagnitudes(value);
      if(cmp > 0) // if our value has the greater magnitude, then we need to do a full divide
      {
        CloneIfNecessary();
        remainder = DivideMagnitudes(data, BitLength, value, data);
        if(IsNegative) remainder = -remainder;
        BitLength = ComputeBitLength();
      }
      else if(cmp == 0) // if the magnitudes are equal, the result is +/- 1 or an exception (if both are zero)
      {
        if(value == 0) throw new DivideByZeroException();
        UnsafeSetMagnitude(1); // our sign stays the same because value is positive
        remainder = 0;
      }
      else // if our value has the smaller magnitude, then we become zero
      {
        remainder = ToUInt32Fast();
        if(IsNegative) remainder = -remainder;
        MakeZero();
      }
      return remainder;
    }

    /// <summary>Increments this <see cref="Integer"/> in place. If this integer shares storage with others, the others may be corrupted.</summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    public void UnsafeIncrement()
    {
      if(IsPositive)
      {
        UnsafeIncrementMagnitude();
      }
      else if(IsNegative)
      {
        if(BitLength == 1) MakeZero();
        else UnsafeDecrementMagnitude();
      }
      else
      {
        this = One;
      }
    }

    /// <summary>Left-shifts this <see cref="Integer"/> in place by the given number of bits. (If the shift count is negative, the value
    /// will be effectively right-shifted.) If this integer shares storage with others, the others may be corrupted.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    public void UnsafeLeftShift(int shift)
    {
      if(shift > 0) UnsafeLeftShift((uint)shift);
      else if(shift < 0) UnsafeRightShift((uint)-shift);
    }

    /// <summary>Multiplies this <see cref="Integer"/> in place. If this integer shares storage with others, the others may be corrupted.</summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    public void UnsafeMultiply(int value)
    {
      UnsafeMultiply((uint)(value < 0 ? -value : value));
      if(value < 0 && !IsZero) info ^= SignBit;
    }

    /// <summary>Multiplies this <see cref="Integer"/> in place. If this integer shares storage with others, the others may be corrupted.</summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    [CLSCompliant(false)]
    public void UnsafeMultiply(uint value)
    {
      if(value <= 1)
      {
        if(value == 0) MakeZero();
      }
      else if(BitLength <= 1) // if our value is 0, 1, or -1...
      {
        if(!IsZero) UnsafeSetMagnitude(value);
      }
      else
      {
        int maxBitLength = BitLength + ComputeBitLength(value);
        if(maxBitLength < 0) throw new OverflowException();
        ExpandArrayTo(GetElementCount(maxBitLength));
        CloneIfNecessary(); // call this after ExpandArrayTo to avoid an extra allocation in one case
        Multiply(data, BitLength, value, data);
        BitLength = ComputeBitLength(data, GetElementCount(maxBitLength)-1);
      }
    }

    /// <summary>Right-shifts this <see cref="Integer"/> in place by the given number of bits. (If the shift amount is negative, the value
    /// will be effectively left-shifted.) If this integer shares storage with others, the others may be corrupted.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    public void UnsafeRightShift(int shift)
    {
      if(shift > 0) UnsafeRightShift((uint)shift);
      else if(shift < 0) UnsafeLeftShift((uint)-shift);
    }

    /// <summary>Replaces this <see cref="Integer"/> in place with the remainder after dividing by the given value. If this integer shares
    /// storage with others, the others may be corrupted.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    public void UnsafeRemainder(int value)
    {
      UnsafeSetMagnitude(RemainderMagnitude(data, BitLength, (uint)(value < 0 ? -value : value)));
    }

    /// <summary>Replaces this <see cref="Integer"/> in place with the remainder after dividing by the given value. If this integer shares
    /// storage with others, the others may be corrupted.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    [CLSCompliant(false)]
    public void UnsafeRemainder(uint value)
    {
      UnsafeSetMagnitude(RemainderMagnitude(data, BitLength, value));
    }

    /// <summary>Sets this <see cref="Integer"/> in place. If this integer shares storage with others, the others may be corrupted.</summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    public void UnsafeSet(int value)
    {
      UnsafeSetMagnitude((uint)(value < 0 ? -value : value));
      if(IsNegative != (value < 0)) info ^= SignBit;
    }

    /// <summary>Sets this <see cref="Integer"/> in place. If this integer shares storage with others, the others may be corrupted.</summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    [CLSCompliant(false)]
    public void UnsafeSet(uint value)
    {
      UnsafeSetMagnitude(value);
      if(IsNegative) info ^= SignBit;
    }

    /// <summary>Sets this <see cref="Integer"/> in place. If this integer shares storage with others, the others may be corrupted.</summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    public void UnsafeSet(long value)
    {
      UnsafeSetMagnitude((ulong)(value < 0 ? -value : value));
      if(IsNegative != (value < 0)) info ^= SignBit;
    }

    /// <summary>Sets this <see cref="Integer"/> in place. If this integer shares storage with others, the others may be corrupted.</summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    [CLSCompliant(false)]
    public void UnsafeSet(ulong value)
    {
      UnsafeSetMagnitude(value);
      if(IsNegative) info ^= SignBit;
    }

    /// <summary>Sets a bit of this <see cref="Integer"/> in place. If this integer shares storage with others, the others may be
    /// corrupted.
    /// </summary>
    /// <remarks>This method allows you to set bits beyond those actually stored. (I.e. <paramref name="index"/> can be greater than or
    /// equal to <see cref="BitLength"/>.) In that case, the value will be expanded as necessary. It is not possible to make a positive
    /// value negative or vice versa with this method.
    /// <include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/remarks/*"/>
    /// </remarks>
    public void UnsafeSetBit(int index, bool value)
    {
      bool changingHighBit = index+1 == BitLength;
      if((uint)index >= (uint)BitLength) // if possibly setting a bit beyond the current maximum...
      {
        if((uint)index >= (uint)int.MaxValue) throw new ArgumentOutOfRangeException(); // ensure 0 <= index < int.MaxValue
        if(value == IsNegative) return; // don't expand the array if we're setting the bit to the value it'd effectively be anyway
        ExpandArrayTo(GetElementCount(index+1)); // if we're toggling a bit beyond the current end of the data, expand the array
        BitLength = index+1; // if the integer is negative this may not be the final bit length
      }

      CloneIfNecessary();
      uint mask = 1u << (index&31);
      if(!IsNegative)
      {
        if(value)
        {
          data[index>>5] |= mask;
        }
        else
        {
          data[index>>5] &= ~mask;
          if(changingHighBit) // if we cleared the high bit, then the bit length was reduced
          {
            int newLength = ComputeBitLength(); // so we need to recalculate it
            if(newLength == 0) MakeZero();
            else BitLength = newLength;
          }
        }
      }
      else
      {
        int i, wordIndex = index>>5;
        // we have to pretend that negative values are stored in two's complement. we do this by effectively subtracting 1 from the value
        // and negating the bits. we first have to determine whether the subtraction is still borrowing when it gets to the desired word
        bool sBorrow = true;
        for(i=0; i<wordIndex; i++)
        {
          if(data[i] != 0) { sBorrow = false; break; }
        }
        bool dBorrow = sBorrow;

        // get the word in two's complement
        uint v = data[wordIndex];
        if(sBorrow && v-- != 0) sBorrow = false;
        v = ~v;

        // modify it
        if(value) v |= mask;
        else v &= ~mask;

        // convert it back
        if(dBorrow && v-- != 0) dBorrow = false;
        v = ~v;
        data[wordIndex] = v;

        if(changingHighBit)
        {
          BitLength = ComputeBitLength(data, wordIndex);
          if(IsZero) data = null;
        }
        else if(sBorrow ^ dBorrow)
        {
          i++;
          if(dBorrow) // if we weren't borrowing from this word but now we are, then continue borrowing
          {
            for(int count = GetElementCount(); i<count; )
            {
              if(--data[i++] != 0) break;
            }
          }
          BitLength = ComputeBitLength(data, i-1);
        }
      }
    }

    /// <summary>Subtracts a value from this <see cref="Integer"/> in place. If this integer shares storage with others, the others may be
    /// corrupted.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    public void UnsafeSubtract(int value)
    {
      if(value != 0)
      {
        bool negativeValue = value < 0;
        if(negativeValue) value = -value;
        if(IsNegative == negativeValue) UnsafeSubtractCore((uint)value);
        else UnsafeAddMagnitude((uint)value);
      }
    }

    /// <summary>Subtracts a value from this <see cref="Integer"/> in place. If this integer shares storage with others, the others may be
    /// corrupted.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/Integer/UnsafeOps/*"/>
    [CLSCompliant(false)]
    public void UnsafeSubtract(uint value)
    {
      if(value != 0)
      {
        if(IsNegative) UnsafeAddMagnitude(value);
        else UnsafeSubtractCore(value);
      }
    }

    #region Arithmetic operators
    /// <summary>Adds one <see cref="Integer"/> value to another and returns the result.</summary>
    public static Integer operator+(Integer a, Integer b)
    {
      if(a.IsZero) return b;
      else if(b.IsZero) return a;
      else if(a.IsNegative == b.IsNegative) return new Integer(AddMagnitudes(a, b), a.IsNegative, false);
      else if(a.CompareMagnitudes(b) >= 0) return new Integer(SubtractMagnitudes(a, b), a.IsNegative, false);
      else return new Integer(SubtractMagnitudes(b, a), b.IsNegative, false);
    }

    /// <summary>Adds an <see cref="Integer"/> to an <see cref="int"/> and returns the result.</summary>
    public static Integer operator+(Integer a, int b)
    {
      bool negative = b < 0;
      return new Integer(Add(a, (uint)(negative ? -b : b), ref negative), negative, false);
    }

    /// <summary>Adds an <see cref="Integer"/> to a <see cref="uint"/> and returns the result.</summary>
    [CLSCompliant(false)]
    public static Integer operator+(Integer a, uint b)
    {
      bool negative = false;
      return new Integer(Add(a, b, ref negative), negative, false);
    }

    /// <summary>Adds an <see cref="int"/> to an <see cref="Integer"/> and returns the result.</summary>
    public static Integer operator+(int a, Integer b)
    {
      bool negative = a < 0;
      return new Integer(Add(b, (uint)(negative ? -a : a), ref negative), negative, false);
    }

    /// <summary>Adds a <see cref="uint"/> to an <see cref="Integer"/> and returns the result.</summary>
    [CLSCompliant(false)]
    public static Integer operator+(uint a, Integer b)
    {
      bool negative = false;
      return new Integer(Add(b, a, ref negative), negative, false);
    }

    /// <summary>Subtracts one <see cref="Integer"/> value from another and returns the result.</summary>
    public static Integer operator-(Integer a, Integer b)
    {
      if(b.IsZero) return a;
      else if(a.IsZero) return -b;
      else if(a.CompareMagnitudes(b) >= 0)
      {
        if(a.IsNegative == b.IsNegative) return new Integer(SubtractMagnitudes(a, b), a.IsNegative, false); // 10 - 1 = 9, -10 - -1 = -9
        else return new Integer(AddMagnitudes(a, b), a.IsNegative, false); // -10 - 1 = -11, 10 - -1 = 11
      }
      else
      {
        if(a.IsNegative == b.IsNegative) return new Integer(SubtractMagnitudes(b, a), !a.IsNegative, false); // 1 - 10 = -9, -1 - -10 = 9, 0 - 5 = -5
        else return new Integer(AddMagnitudes(b, a), a.IsNegative, false); // 1 - -10 = 11, -1 - 10 = -11
      }
    }

    /// <summary>Subtracts an <see cref="int"/> from an <see cref="Integer"/> and returns the result.</summary>
    public static Integer operator-(Integer a, int b)
    {
      bool negative = b < 0;
      return new Integer(Subtract(a, (uint)(negative ? -b : b), ref negative), negative, false);
    }

    /// <summary>Subtracts a <see cref="uint"/> from an <see cref="Integer"/> and returns the result.</summary>
    [CLSCompliant(false)]
    public static Integer operator-(Integer a, uint b)
    {
      bool negative = false;
      return new Integer(Subtract(a, b, ref negative), negative, false);
    }

    /// <summary>Subtracts an <see cref="Integer"/> from an <see cref="int"/> and returns the result.</summary>
    public static Integer operator-(int a, Integer b)
    {
      bool negative = a < 0;
      return new Integer(Subtract((uint)(negative ? -a : a), b, ref negative), negative, false);
    }

    /// <summary>Subtracts an <see cref="Integer"/> from a <see cref="uint"/> and returns the result.</summary>
    [CLSCompliant(false)]
    public static Integer operator-(uint a, Integer b)
    {
      bool negative = false;
      return new Integer(Subtract(a, b, ref negative), negative, false);
    }

    /// <summary>Multiplies one <see cref="Integer"/> value by another and returns the result.</summary>
    public static Integer operator*(Integer a, Integer b)
    {
      return new Integer(MultiplyMagnitudes(a, b), a.IsNegative ^ b.IsNegative, false);
    }

    /// <summary>Muliplies an <see cref="Integer"/> by an <see cref="int"/> and returns the result.</summary>
    public static Integer operator*(Integer a, int b)
    {
      bool negative = b < 0;
      return new Integer(MultiplyMagnitudes(a, (uint)(negative ? -b : b)), a.IsNegative ^ negative, false);
    }

    /// <summary>Muliplies an <see cref="Integer"/> by a <see cref="uint"/> and returns the result.</summary>
    [CLSCompliant(false)]
    public static Integer operator*(Integer a, uint b)
    {
      return new Integer(MultiplyMagnitudes(a, b), a.IsNegative, false);
    }

    /// <summary>Muliplies an <see cref="int"/> by an <see cref="Integer"/> and returns the result.</summary>
    public static Integer operator*(int a, Integer b)
    {
      bool negative = a < 0;
      return new Integer(MultiplyMagnitudes(b, (uint)(negative ? -a : a)), b.IsNegative ^ negative, false);
    }

    /// <summary>Muliplies a <see cref="uint"/> by an <see cref="Integer"/> and returns the result.</summary>
    [CLSCompliant(false)]
    public static Integer operator*(uint a, Integer b)
    {
      return new Integer(MultiplyMagnitudes(b, a), b.IsNegative, false);
    }

    /// <summary>Divides one <see cref="Integer"/> value by another and returns the quotient.</summary>
    public static Integer operator/(Integer a, Integer b)
    {
      uint[] remainder;
      return new Integer(DivideMagnitudes(a, b, out remainder), a.IsNegative ^ b.IsNegative, false);
    }

    /// <summary>Divides an <see cref="Integer"/> by an <see cref="int"/> and returns the quotient.</summary>
    public static Integer operator/(Integer a, int b)
    {
      bool negative = b < 0;
      return new Integer(DivideMagnitudes(a, (uint)(negative ? -b : b)), a.IsNegative ^ negative, false);
    }

    /// <summary>Divides an <see cref="Integer"/> by a <see cref="uint"/> and returns the quotient.</summary>
    [CLSCompliant(false)]
    public static Integer operator/(Integer a, uint b)
    {
      return new Integer(DivideMagnitudes(a, b), a.IsNegative, false);
    }

    /// <summary>Divides one <see cref="Integer"/> value by another and returns the remainder.</summary>
    public static Integer operator%(Integer a, Integer b)
    {
      uint[] remainder;
      DivideMagnitudes(a, b, out remainder);
      return new Integer(remainder, a.IsNegative, false);
    }

    /// <summary>Divides an <see cref="Integer"/> by an <see cref="int"/> and returns the remainder.</summary>
    public static int operator%(Integer a, int b)
    {
      int remainder = (int)RemainderMagnitude(a.data, a.BitLength, (uint)(b < 0 ? -b : b));
      return a.IsNegative ? -remainder : remainder;
    }

    /// <summary>Divides an <see cref="Integer"/> by a <see cref="uint"/> and returns the remainder.</summary>
    [CLSCompliant(false)]
    public static long operator%(Integer a, uint b)
    {
      long remainder = RemainderMagnitude(a.data, a.BitLength, b);
      if(a.IsNegative) remainder = -remainder;
      return remainder;
    }

    /// <summary>Divides an <see cref="int"/> by an <see cref="Integer"/> and returns the remainder.</summary>
    public static int operator%(int a, Integer b)
    {
      uint aMagnitude = (uint)(a < 0 ? -a : a);
      int remainder = (int)(b.CompareMagnitudes(aMagnitude) <= 0 ? aMagnitude % b.ToUInt32Fast() : aMagnitude);
      return a < 0 ? -remainder : remainder;
    }

    /// <summary>Divides a <see cref="uint"/> by an <see cref="Integer"/> and returns the remainder.</summary>
    [CLSCompliant(false)]
    public static uint operator%(uint a, Integer b)
    {
      return b.CompareMagnitudes(a) <= 0 ? a % b.ToUInt32Fast() : a;
    }

    /// <summary>Negates an <see cref="Integer"/> value and returns the result.</summary>
    public static Integer operator-(Integer value)
    {
      if(value.IsZero) return value;
      else return new Integer(value.data, value.info ^ SignBit); // we can reuse the same array since the magnitude doesn't change
    }

    /// <summary>Increments an <see cref="Integer"/> value and returns the result.</summary>
    public static Integer operator++(Integer value)
    {
      return new Integer(value.IsNegative ? DecrementMagnitude(value) : IncrementMagnitude(value), value.IsNegative, false);
    }

    /// <summary>Decrements an <see cref="Integer"/> value and returns the result.</summary>
    public static Integer operator--(Integer value)
    {
      return new Integer(value.IsPositive ? DecrementMagnitude(value) : IncrementMagnitude(value), !value.IsPositive, false);
    }
    #endregion

    #region Bitwise operators
    /// <summary>Shifts an <see cref="Integer"/> value left, bitwise, and returns the result. (If <paramref name="shift"/> is negative,
    /// the value is shifted right instead.)
    /// </summary>
    public static Integer operator<<(Integer value, int shift)
    {
      if(shift >= 0) return LeftShift(value, (uint)shift);
      else return RightShift(value, (uint)-shift);
    }

    /// <summary>Shifts an <see cref="Integer"/> value right, bitwise, and returns the result. (If <paramref name="shift"/> is negative,
    /// the value is shifted left instead.)
    /// </summary>
    public static Integer operator>>(Integer value, int shift)
    {
      if(shift >= 0) return RightShift(value, (uint)shift);
      else return LeftShift(value, (uint)-shift);
    }

    /// <summary>Bitwise-ANDs two <see cref="Integer"/> values and returns the result.</summary>
    /// <remarks>The result will be negative only if both parameters are negative.
    /// <para><include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/></para>
    /// </remarks>
    public static Integer operator&(Integer a, Integer b)
    {
      return new Integer(BitwiseAnd(a, b), a.IsNegative & b.IsNegative, false);
    }

    /// <summary>Bitwise-ANDs an <see cref="Integer"/> and an <see cref="int"/> and returns the result.</summary>
    /// <remarks>The result will be negative only if both parameters are negative.
    /// <para><include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/></para>
    /// </remarks>
    public static Integer operator&(Integer a, int b)
    {
      if(b >= 0) return new Integer((a.IsNegative ? (uint)-(int)a.data[0] : a.ToUInt32Fast()) & (uint)b);
      a = a.Clone();
      a.UnsafeBitwiseAnd(b);
      return a;
    }

    /// <summary>Bitwise-ANDs an <see cref="Integer"/> and a <see cref="uint"/> and returns the result.</summary>
    /// <remarks>The result will be non-negative.
    /// <para><include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/></para>
    /// </remarks>
    [CLSCompliant(false)]
    public static uint operator&(Integer a, uint b)
    {
      return (a.IsNegative ? (uint)-(int)a.data[0] : a.ToUInt32Fast()) & b;
    }

    /// <summary>Bitwise-ANDs an <see cref="int"/> and an <see cref="Integer"/> and returns the result.</summary>
    /// <remarks>The result will be negative only if both parameters are negative.
    /// <para><include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/></para>
    /// </remarks>
    public static Integer operator&(int a, Integer b)
    {
      if(a >= 0) return new Integer((uint)a & (b.IsNegative ? (uint)-(int)b.data[0] : b.ToUInt32Fast()));
      b = b.Clone();
      b.UnsafeBitwiseAnd(a);
      return b;
    }

    /// <summary>Bitwise-ANDs a <see cref="uint"/> and an <see cref="Integer"/> and returns the result.</summary>
    /// <remarks>The result will be non-negative.
    /// <para><include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/></para>
    /// </remarks>
    [CLSCompliant(false)]
    public static uint operator&(uint a, Integer b)
    {
      return a & (b.IsNegative ? (uint)-(int)b.data[0] : b.ToUInt32Fast());
    }

    /// <summary>Bitwise-ORs two <see cref="Integer"/> values and returns the result.</summary>
    /// <remarks>The result will be negative if either parameter is negative.
    /// <para><include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/></para>
    /// </remarks>
    public static Integer operator|(Integer a, Integer b)
    {
      return new Integer(BitwiseOr(a, b), a.IsNegative | b.IsNegative, false);
    }

    /// <summary>Bitwise-ORs an <see cref="Integer"/> and an <see cref="int"/> and returns the result.</summary>
    /// <remarks>The result will be negative if either parameter is negative.
    /// <para><include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/></para>
    /// </remarks>
    public static Integer operator|(Integer a, int b)
    {
      a = a.Clone();
      a.UnsafeBitwiseOr(b);
      return a;
    }

    /// <summary>Bitwise-ORs an <see cref="Integer"/> and a <see cref="uint"/> and returns the result.</summary>
    /// <remarks>The result will be negative if the <see cref="Integer"/> is negative.
    /// <para><include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/></para>
    /// </remarks>
    [CLSCompliant(false)]
    public static Integer operator|(Integer a, uint b)
    {
      a = a.Clone();
      a.UnsafeBitwiseOr(b);
      return a;
    }

    /// <summary>Bitwise-ORs an <see cref="int"/> and an <see cref="Integer"/> and returns the result.</summary>
    /// <remarks>The result will be negative if either parameter is negative.
    /// <para><include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/></para>
    /// </remarks>
    public static Integer operator|(int a, Integer b)
    {
      b = b.Clone();
      b.UnsafeBitwiseOr(a);
      return b;
    }

    /// <summary>Bitwise-ORs a <see cref="uint"/> and an <see cref="Integer"/> and returns the result.</summary>
    /// <remarks>The result will be negative if the <see cref="Integer"/> is negative.
    /// <para><include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/></para>
    /// </remarks>
    [CLSCompliant(false)]
    public static Integer operator|(uint a, Integer b)
    {
      b = b.Clone();
      b.UnsafeBitwiseOr(a);
      return b;
    }

    /// <summary>Negates an <see cref="Integer"/> value, bitwise, and returns the result.</summary>
    /// <remarks>This method will make positive values negative and negative values non-positive.
    /// <para><include file="documentation.xml" path="/Math/Integer/BitwiseOps/remarks/*"/></para>
    /// </remarks>
    public static Integer operator~(Integer value)
    {
      return new Integer(value.IsNegative ? DecrementMagnitude(value) : IncrementMagnitude(value), !value.IsNegative, false); // ~x==-(x+1)
    }
    #endregion

    #region Comparison operators
    /// <summary>Determines whether two <see cref="Integer"/> values are equal.</summary>
    public static bool operator==(Integer a, Integer b) { return a.Equals(b); }
    /// <summary>Determines whether two <see cref="Integer"/> values are unequal.</summary>
    public static bool operator!=(Integer a, Integer b) { return !a.Equals(b); }
    /// <summary>Determines whether one <see cref="Integer"/> value is less than another.</summary>
    public static bool operator<(Integer a, Integer b) { return a.CompareTo(b) < 0; }
    /// <summary>Determines whether one <see cref="Integer"/> value is less than or equal to another.</summary>
    public static bool operator<=(Integer a, Integer b) { return a.CompareTo(b) <= 0; }
    /// <summary>Determines whether one <see cref="Integer"/> value is greater than another.</summary>
    public static bool operator>(Integer a, Integer b) { return a.CompareTo(b) > 0; }
    /// <summary>Determines whether one <see cref="Integer"/> value is greater than or equal to another.</summary>
    public static bool operator>=(Integer a, Integer b) { return a.CompareTo(b) >= 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is equal to the given 32-bit signed integer.</summary>
    public static bool operator==(Integer a, int b) { return a.Equals(b); }
    /// <summary>Determines whether an <see cref="Integer"/> is unequal to the given 32-bit signed integer.</summary>
    public static bool operator!=(Integer a, int b) { return !a.Equals(b); }
    /// <summary>Determines whether an <see cref="Integer"/> is less than the given 32-bit signed integer.</summary>
    public static bool operator<(Integer a, int b) { return a.CompareTo(b) < 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is less than or equal to the given 32-bit signed integer.</summary>
    public static bool operator<=(Integer a, int b) { return a.CompareTo(b) <= 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is greater than the given 32-bit signed integer.</summary>
    public static bool operator>(Integer a, int b) { return a.CompareTo(b) > 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is greater than or equal to the given 32-bit signed integer.</summary>
    public static bool operator>=(Integer a, int b) { return a.CompareTo(b) >= 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is equal to the given 32-bit unsigned integer.</summary>
    [CLSCompliant(false)]
    public static bool operator==(Integer a, uint b) { return a.Equals(b); }
    /// <summary>Determines whether an <see cref="Integer"/> is unequal to the given 32-bit unsigned integer.</summary>
    [CLSCompliant(false)]
    public static bool operator!=(Integer a, uint b) { return !a.Equals(b); }
    /// <summary>Determines whether an <see cref="Integer"/> is less than the given 32-bit unsigned integer.</summary>
    [CLSCompliant(false)]
    public static bool operator<(Integer a, uint b) { return a.CompareTo(b) < 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is less than or equal to the given 32-bit unsigned integer.</summary>
    [CLSCompliant(false)]
    public static bool operator<=(Integer a, uint b) { return a.CompareTo(b) <= 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is greater than the given 32-bit unsigned integer.</summary>
    [CLSCompliant(false)]
    public static bool operator>(Integer a, uint b) { return a.CompareTo(b) > 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is greater than or equal to the given 32-bit unsigned integer.</summary>
    [CLSCompliant(false)]
    public static bool operator>=(Integer a, uint b) { return a.CompareTo(b) >= 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is equal to the given 64-bit signed integer.</summary>
    public static bool operator==(Integer a, long b) { return a.Equals(b); }
    /// <summary>Determines whether an <see cref="Integer"/> is unequal to the given 64-bit signed integer.</summary>
    public static bool operator!=(Integer a, long b) { return !a.Equals(b); }
    /// <summary>Determines whether an <see cref="Integer"/> is less than the given 64-bit signed integer.</summary>
    public static bool operator<(Integer a, long b) { return a.CompareTo(b) < 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is less than or equal to the given 64-bit signed integer.</summary>
    public static bool operator<=(Integer a, long b) { return a.CompareTo(b) <= 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is greater than the given 64-bit signed integer.</summary>
    public static bool operator>(Integer a, long b) { return a.CompareTo(b) > 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is greater than or equal to the given 64-bit signed integer.</summary>
    public static bool operator>=(Integer a, long b) { return a.CompareTo(b) >= 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is equal to the given 64-bit unsigned integer.</summary>
    [CLSCompliant(false)]
    public static bool operator==(Integer a, ulong b) { return a.Equals(b); }
    /// <summary>Determines whether an <see cref="Integer"/> is unequal to the given 64-bit unsigned integer.</summary>
    [CLSCompliant(false)]
    public static bool operator!=(Integer a, ulong b) { return !a.Equals(b); }
    /// <summary>Determines whether an <see cref="Integer"/> is less than the given 64-bit unsigned integer.</summary>
    [CLSCompliant(false)]
    public static bool operator<(Integer a, ulong b) { return a.CompareTo(b) < 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is less than or equal to the given 64-bit unsigned integer.</summary>
    [CLSCompliant(false)]
    public static bool operator<=(Integer a, ulong b) { return a.CompareTo(b) <= 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is greater than the given 64-bit unsigned integer.</summary>
    [CLSCompliant(false)]
    public static bool operator>(Integer a, ulong b) { return a.CompareTo(b) > 0; }
    /// <summary>Determines whether an <see cref="Integer"/> is greater than or equal to the given 64-bit unsigned integer.</summary>
    [CLSCompliant(false)]
    public static bool operator>=(Integer a, ulong b) { return a.CompareTo(b) >= 0; }
    /// <summary>Determines whether a signed 32-bit integer is equal to the given <see cref="Integer"/> value.</summary>
    public static bool operator==(int a, Integer b) { return b.Equals(a); }
    /// <summary>Determines whether a signed 32-bit integer is unequal to the given <see cref="Integer"/> value.</summary>
    public static bool operator!=(int a, Integer b) { return !b.Equals(a); }
    /// <summary>Determines whether a signed 32-bit integer is less than the given <see cref="Integer"/> value.</summary>
    public static bool operator<(int a, Integer b) { return b.CompareTo(a) > 0; }
    /// <summary>Determines whether a signed 32-bit integer is less than or equal to the given <see cref="Integer"/> value.</summary>
    public static bool operator<=(int a, Integer b) { return b.CompareTo(a) >= 0; }
    /// <summary>Determines whether a signed 32-bit integer is greater than the given <see cref="Integer"/> value.</summary>
    public static bool operator>(int a, Integer b) { return b.CompareTo(a) < 0; }
    /// <summary>Determines whether a signed 32-bit integer is greater than or equal to the given <see cref="Integer"/> value.</summary>
    public static bool operator>=(int a, Integer b) { return b.CompareTo(a) <= 0; }
    /// <summary>Determines whether an unsigned 32-bit integer is equal to the given <see cref="Integer"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator==(uint a, Integer b) { return b.Equals(a); }
    /// <summary>Determines whether an unsigned 32-bit integer is unequal to the given <see cref="Integer"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator!=(uint a, Integer b) { return !b.Equals(a); }
    /// <summary>Determines whether an unsigned 32-bit integer is less than the given <see cref="Integer"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator<(uint a, Integer b) { return b.CompareTo(a) > 0; }
    /// <summary>Determines whether an unsigned 32-bit integer is less than or equal to the given <see cref="Integer"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator<=(uint a, Integer b) { return b.CompareTo(a) >= 0; }
    /// <summary>Determines whether an unsigned 32-bit integer is greater than the given <see cref="Integer"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator>(uint a, Integer b) { return b.CompareTo(a) < 0; }
    /// <summary>Determines whether an unsigned 32-bit integer is greater than or equal to the given <see cref="Integer"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator>=(uint a, Integer b) { return b.CompareTo(a) <= 0; }
    /// <summary>Determines whether a signed 64-bit integer is equal to the given <see cref="Integer"/> value.</summary>
    public static bool operator==(long a, Integer b) { return b.Equals(a); }
    /// <summary>Determines whether a signed 64-bit integer is unequal to the given <see cref="Integer"/> value.</summary>
    public static bool operator!=(long a, Integer b) { return !b.Equals(a); }
    /// <summary>Determines whether a signed 64-bit integer is less than the given <see cref="Integer"/> value.</summary>
    public static bool operator<(long a, Integer b) { return b.CompareTo(a) > 0; }
    /// <summary>Determines whether a signed 64-bit integer is less than or equal to the given <see cref="Integer"/> value.</summary>
    public static bool operator<=(long a, Integer b) { return b.CompareTo(a) >= 0; }
    /// <summary>Determines whether a signed 64-bit integer is greater than the given <see cref="Integer"/> value.</summary>
    public static bool operator>(long a, Integer b) { return b.CompareTo(a) < 0; }
    /// <summary>Determines whether a signed 64-bit integer is greater than or equal to the given <see cref="Integer"/> value.</summary>
    public static bool operator>=(long a, Integer b) { return b.CompareTo(a) <= 0; }
    /// <summary>Determines whether an unsigned 64-bit integer is equal to the given <see cref="Integer"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator==(ulong a, Integer b) { return b.Equals(a); }
    /// <summary>Determines whether an unsigned 64-bit integer is unequal to the given <see cref="Integer"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator!=(ulong a, Integer b) { return !b.Equals(a); }
    /// <summary>Determines whether an unsigned 64-bit integer is less than the given <see cref="Integer"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator<(ulong a, Integer b) { return b.CompareTo(a) > 0; }
    /// <summary>Determines whether an unsigned 64-bit integer is less than or equal to the given <see cref="Integer"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator<=(ulong a, Integer b) { return b.CompareTo(a) >= 0; }
    /// <summary>Determines whether an unsigned 64-bit integer is greater than the given <see cref="Integer"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator>(ulong a, Integer b) { return b.CompareTo(a) < 0; }
    /// <summary>Determines whether an unsigned 64-bit integer is greater than or equal to the given <see cref="Integer"/> value.</summary>
    [CLSCompliant(false)]
    public static bool operator>=(ulong a, Integer b) { return b.CompareTo(a) <= 0; }
    #endregion

    #region Conversion operators
    /// <summary>Provides an implicit conversion from <see cref="int"/> to <see cref="Integer"/>.</summary>
    public static implicit operator Integer(int value)
    {
      return new Integer(value);
    }

    /// <summary>Provides an implicit conversion from <see cref="uint"/> to <see cref="Integer"/>.</summary>
    [CLSCompliant(false)]
    public static implicit operator Integer(uint value)
    {
      return new Integer(value);
    }

    /// <summary>Provides an implicit conversion from <see cref="long"/> to <see cref="Integer"/>.</summary>
    public static implicit operator Integer(long value)
    {
      return new Integer(value);
    }

    /// <summary>Provides an implicit conversion from <see cref="ulong"/> to <see cref="Integer"/>.</summary>
    [CLSCompliant(false)]
    public static implicit operator Integer(ulong value)
    {
      return new Integer(value);
    }

    /// <summary>Provides an explicit conversion from <see cref="double"/> to <see cref="Integer"/>.</summary>
    public static explicit operator Integer(double value)
    {
      return new Integer(value);
    }

    /// <summary>Provides an explicit conversion from <see cref="float"/> to <see cref="Integer"/>.</summary>
    public static explicit operator Integer(float value)
    {
      return new Integer(value);
    }

    /// <summary>Provides an explicit conversion from <see cref="decimal"/> to <see cref="Integer"/>.</summary>
    public static explicit operator Integer(decimal value)
    {
      return new Integer(value);
    }

    /// <summary>Provides an explicit conversion from <see cref="Integer"/> to <see cref="int"/>. This method will not throw an exception;
    /// the <see cref="Integer"/> will simply be truncated to 32 bits.
    /// </summary>
    public static explicit operator int(Integer value)
    {
      int smallValue = (int)value.ToUInt32Fast();
      if(value.IsNegative) smallValue = -smallValue;
      return smallValue;
    }

    /// <summary>Provides an explicit conversion from <see cref="Integer"/> to <see cref="uint"/>. This method will not throw an exception;
    /// the <see cref="Integer"/> will simply be truncated to 32 bits.
    /// </summary>
    [CLSCompliant(false)]
    public static explicit operator uint(Integer value)
    {
      return (uint)(int)value;
    }

    /// <summary>Provides an explicit conversion from <see cref="Integer"/> to <see cref="long"/>. This method will not throw an exception;
    /// the <see cref="Integer"/> will simply be truncated to 64 bits.
    /// </summary>
    public static explicit operator long(Integer value)
    {
      long smallValue = (long)value.ToUInt64Fast();
      if(value.IsNegative) smallValue = -smallValue;
      return smallValue;
    }

    /// <summary>Provides an explicit conversion from <see cref="Integer"/> to <see cref="ulong"/>. This method will not throw an
    /// exception; the <see cref="Integer"/> will simply be truncated to 64 bits.
    /// </summary>
    [CLSCompliant(false)]
    public static explicit operator ulong(Integer value)
    {
      return (ulong)(long)value;
    }

    /// <summary>Provides an explicit conversion from <see cref="Integer"/> to <see cref="short"/>. This method will not throw an
    /// exception; the <see cref="Integer"/> will simply be truncated to 16 bits.
    /// </summary>
    public static explicit operator short(Integer value)
    {
      return (short)(int)value;
    }

    /// <summary>Provides an explicit conversion from <see cref="Integer"/> to <see cref="ushort"/>. This method will not throw an
    /// exception; the <see cref="Integer"/> will simply be truncated to 16 bits.
    /// </summary>
    [CLSCompliant(false)]
    public static explicit operator ushort(Integer value)
    {
      return (ushort)(int)value;
    }

    /// <summary>Provides an explicit conversion from <see cref="Integer"/> to <see cref="sbyte"/>. This method will not throw an
    /// exception; the <see cref="Integer"/> will simply be truncated to 8 bits.
    /// </summary>
    [CLSCompliant(false)]
    public static explicit operator sbyte(Integer value)
    {
      return (sbyte)(int)value;
    }

    /// <summary>Provides an explicit conversion from <see cref="Integer"/> to <see cref="byte"/>. This method will not throw an
    /// exception; the <see cref="Integer"/> will simply be truncated to 8 bits.
    /// </summary>
    public static explicit operator byte(Integer value)
    {
      return (byte)(int)value;
    }

    /// <summary>Provides an explicit conversion from <see cref="Integer"/> to <see cref="char"/>. This method will not throw an
    /// exception; the <see cref="Integer"/> will simply be truncated.
    /// </summary>
    public static explicit operator char(Integer value)
    {
      return (char)(int)value;
    }

    /// <summary>Provides an explicit conversion from <see cref="Integer"/> to <see cref="double"/>. This method will not throw an
    /// exception; if the <see cref="Integer"/> is too large to be represented, <see cref="double.PositiveInfinity"/> or
    /// <see cref="double.NegativeInfinity"/> will be returned.
    /// </summary>
    public static explicit operator double(Integer value)
    {
      if(value.IsZero)
      {
        return 0;
      }
      else if(value.BitLength > 1024) // (Integer)double.MaxValue requires 1024 bits. more than that, and it must be +/- Infinity
      {
        return value.IsPositive ? double.PositiveInfinity : double.NegativeInfinity;
      }
      else
      {
        int count = value.GetElementCount();
        double fp = value.data[count-1];
        for(int i=count-2; i >= 0; i--) fp = fp*4294967296.0 + value.data[i];
        if(value.IsNegative) fp = -fp;
        return fp;
      }
    }

    /// <summary>Provides an explicit conversion from <see cref="Integer"/> to <see cref="float"/>. This method will not throw an
    /// exception; if the <see cref="Integer"/> is too large to be represented, <see cref="float.PositiveInfinity"/> or
    /// <see cref="float.NegativeInfinity"/> will be returned.
    /// </summary>
    public static explicit operator float(Integer value)
    {
      // (Integer)float.MaxValue requires 128 bits. more than that, and it must be +/- Infinity
      if(value.BitLength > 128) return value.IsPositive ? float.PositiveInfinity : float.NegativeInfinity;
      else return (float)(double)value;
    }

    /// <summary>Provides an explicit conversion from <see cref="Integer"/> to <see cref="FP107"/>. This method will not throw an
    /// exception; if the <see cref="Integer"/> is too large to be represented, <see cref="FP107.PositiveInfinity"/> or
    /// <see cref="FP107.NegativeInfinity"/> will be returned.
    /// </summary>
    public static explicit operator FP107(Integer value)
    {
      if(value.IsZero)
      {
        return FP107.Zero;
      }
      else if(value.BitLength > 1024) // (Integer)FP107.MaxValue requires 1024 bits. more than that, and it must be +/- Infinity
      {
        return value.IsPositive ? FP107.PositiveInfinity : FP107.NegativeInfinity;
      }
      else
      {
        int count = value.GetElementCount();
        FP107 fp = new FP107(value.data[count-1]);
        for(int i=count-2; i >= 0; i--) fp = fp*4294967296.0 + value.data[i];
        if(value.IsNegative) fp = -fp;
        return fp;
      }
    }

    /// <summary>Provides an explicit conversion from <see cref="Integer"/> to <see cref="decimal"/>. If the <see cref="Integer"/> is too
    /// large to be represented, an exception will be thrown.
    /// </summary>
    /// <exception cref="OverflowException">Thrown if the <paramref name="value"/> is cannot be represented by a <see cref="decimal"/>.</exception>
    public static explicit operator decimal(Integer value)
    {
      int bitLength = value.BitLength;
      if(bitLength > 96) throw new OverflowException();

      int lo = 0, mid = 0, hi = 0;
      if(bitLength != 0)
      {
        lo = (int)value.data[0];
        if(bitLength > 32)
        {
          mid = (int)value.data[1];
          if(bitLength > 64) hi = (int)value.data[2];
        }
      }
      return new decimal(lo, mid, hi, value.IsNegative, 0);
    }
    #endregion

    /// <summary>Returns the absolute value of the given <see cref="Integer"/>.</summary>
    public static Integer Abs(Integer value)
    {
      return new Integer(value.data, value.info & ~SignBit);
    }

    /// <summary>Counts the number of trailing zero bits in the binary representation of the integer.</summary>
    /// <remarks>This method returns zero if the value is zero.</remarks>
    public static int CountTrailingZeros(Integer value)
    {
      int count = 0;
      if(value.data != null)
      {
        for(int i=0; i<value.data.Length; i++)
        {
          if(value.data[i] != 0)
          {
            count += BinaryUtility.CountTrailingZeros(value.data[i]);
            break;
          }
          count += 32;
        }
      }
      return count;
    }

    /// <summary>Divides one <see cref="Integer"/> value by another and returns its quotient and remainder.</summary>
    public static Integer DivRem(Integer dividend, Integer divisor, out Integer remainder)
    {
      uint[] rem;
      Integer quotient = new Integer(DivideMagnitudes(dividend, divisor, out rem), dividend.IsNegative ^ divisor.IsNegative, false);
      remainder = new Integer(rem, dividend.IsNegative, false);
      return quotient;
    }

    /// <summary>Computes the factorial of the given number.</summary>
    public static Integer Factorial(int n)
    {
      if(n < 0) throw new ArgumentOutOfRangeException();
      if(n == 0) return One;
      Integer i = n; // TODO: is there a better algorithm?
      while(--n > 1) i.UnsafeMultiply((uint)n);
      return i;
    }

    /// <summary>Returns the greatest common factor of two integers.</summary>
    [CLSCompliant(false)]
    public static unsafe Integer GreatestCommonFactor(Integer a, Integer b)
    {
      // take a fast path if we can
      if((a.info|b.info) <= 32) return NumberTheory.GreatestCommonFactor(a.ToUInt32Fast(), b.ToUInt32Fast());
      if((a.info|b.info) <= 64) return NumberTheory.GreatestCommonFactor(a.ToUInt64Fast(), b.ToUInt64Fast());

      // because division is especially slow for bigints, we'll use the binary GCD algorithm, which allows us to do in-place operations
      if(a.IsNegative) a = -a;
      if(b.IsNegative) b = -b;
      a = a.Clone(); // clone the values so we can perform unsafe operations on them
      b = b.Clone();
      int powers = 0;
      while(!a.IsZero && !b.IsZero)
      {
        if(!a.IsEven) // if a is odd...
        {
          if(b.IsEven) // if b is even, then GCF(a,b) is not divisible by two, so divide b by two
          {
            b.UnsafeRightShift(1);
          }
          else if(a.CompareTo(b) >= 0) // otherwise, both are odd. since gcf(a,b) divides a-b, we can replace the larger value with
          {                            // abs(a-b), but since that must be even (because a and b are odd), we can also divide it by two
            a -= b; // replace a with abs(a-b)
            a.UnsafeRightShift(1); // and divide it by 2
          }
          else
          {
            b -= a; // replace b with abs(a-b)
            b.UnsafeRightShift(1);
          }
        }
        else // a is even...
        {
          a.UnsafeRightShift(1); // there are two cases. 1) b is odd, so two is not in the GCF and we can divide a by two, or 2) b is even,
          if(b.IsEven)           // so both are divisible by two and thus two is in the GCF, in which case we divide both by two and keep
          {                      // track of the extra power of two. so either way we divide a by two
            b.UnsafeRightShift(1);
            powers++;
          }
        }

        if((a.info|b.info) <= 64) // switch to faster code ASAP
        {
          a = NumberTheory.GreatestCommonFactor(a.ToUInt64Fast(), b.ToUInt64Fast());
          a.UnsafeLeftShift(powers);
          return a;
        }
      }

      // at this point one (or both) is zero, and the GCD is the remaining non-zero value (if any) times the power of two extracted above
      if(a.IsZero)
      {
        b.UnsafeLeftShift(powers);
        return b;
      }
      else
      {
        a.UnsafeLeftShift(powers);
        return a;
      }
    }

    /// <summary>Returns the least common multiple of two integers, as a nonnegative value.</summary>
    public static Integer LeastCommonMultiple(Integer a, Integer b)
    {
      if((a.info | b.info) == 0) return Zero; // LCM(0,0) = 0
      return Abs(a / GreatestCommonFactor(a, b) * b);
    }

    /// <summary>Returns the greater of two <see cref="Integer"/> values.</summary>
    public static Integer Max(Integer a, Integer b)
    {
      if(a >= b) return a;
      else return b;
    }

    /// <summary>Returns the lesser of two <see cref="Integer"/> values.</summary>
    public static Integer Min(Integer a, Integer b)
    {
      if(a <= b) return a;
      else return b;
    }

    /// <summary>Parses an <see cref="Integer"/> value formatted according to the current culture.</summary>
    public static Integer Parse(string str)
    {
      return Parse(str, NumberStyles.Any, null);
    }

    /// <summary>Parses an <see cref="Integer"/> value formatted according to the given provider, or the current culture if the provider
    /// is null.
    /// </summary>
    public static Integer Parse(string str, IFormatProvider provider)
    {
      return Parse(str, NumberStyles.Any, provider);
    }

    /// <summary>Parses an <see cref="Integer"/> value formatted using the given provider, or the current culture if the provider is null.</summary>
    public static Integer Parse(string str, NumberStyles style, IFormatProvider provider)
    {
      if(str == null) throw new ArgumentNullException();
      Integer value;
      Exception ex;
      if(!TryParse(str, style, provider, out value, out ex)) throw ex;
      return value;
    }

    /// <summary>Returns the given value raised to the given power.</summary>
    public static Integer Pow(int value, int power)
    {
      if(power <= 1)
      {
        if(power == 0 || value == 1) return One; // x^0 == 1 and 1^y == 1 regardless of x or y
        else if(power == 1) return value;
        else if(value == -1) return (power&1) == 0 ? One : MinusOne; // -1^y is -1 when y is odd and 1 otherwise, regardless of y's sign
        else if(value != 0) return Zero; // x^y truncates to zero when abs(x) > 1 and y < 0
        else throw new ArgumentOutOfRangeException(); // exception when 0^y and y < 0
      }

      bool negative = value < 0;
      if(negative)
      {
        value = -value; // this is okay even if value == int.MinValue because we'll treat it as unsigned
        if((power & 1) == 0) negative = false; // only odd values with odd powers cause odd results
      }

      // special case powers of powers of two because they are common and can be calculated very efficiently
      if(((uint)value & (uint)(value-1)) == 0) // if value is a power of two or zero...
      {
        if((uint)value >= 2) // if value is a power of two greater than or equal to two (use uint to handle int.MinValue)...
        {
          // a power of two is represented as a one bit followed by some zeros. the number of zero bits added for each power we're raising
          // it to equals the base-2 logarithm of the the value. (so if value = 2 and power = 3, then the value is a one bit followed by
          // 3*log2(value) = 3*1 zero bits: 1000. if value = 8 instead, we have 3*log2(8) = 3*3 = 9 zero bits. and 8^3 = 2^9.)
          // since value is a one bit followed by some zeros, we can compute its base-2 logarithm by counting the trailing zero bits
          long longBitLength = (long)power * BinaryUtility.CountTrailingZeros((uint)value) + 1; // power * log2(value) + 1
          if(longBitLength > int.MaxValue) throw new OverflowException();

          int bitLength = (int)longBitLength;
          uint[] data = new uint[GetElementCount(bitLength)];
          data[data.Length-1] = 1u << ((bitLength-1) & 31);
          return new Integer(data, bitLength, negative);
        }
        else if(value == 1)
        {
          if(negative) return MinusOne;
          else return One;
        }
        else // value == 0
        {
          return Zero;
        }
      }

      Integer result;
      if(value == 10 && power < PowersOf10.Length) // special-case powers of 10 that fit in a ulong
      {
        result = new Integer(PowersOf10[power]);
      }
      else if((uint)value-3 < SmallExponents.Length && power <= SmallExponents[(uint)value-3]) // if it can be computed with Math.Pow...
      {
        result = new Integer((ulong)Math.Pow((uint)value, power));
      }
      else // powers of other values will be computed by repeated squaring
      {
        Integer pow = (uint)value;
        result = One;
        while(true)
        {
          if((power & 1) != 0) result *= pow;
          power >>= 1;
          if(power == 0) break;
          pow = pow.Square();
        }
      }

      if(negative) result = -result;
      return result;
    }

    /// <summary>Returns the given value raised to the given power.</summary>
    public static Integer Pow(Integer value, int power)
    {
      if(value.BitLength <= 31) return Pow((int)value, power); // if 'value' fits in an int, take a faster path

      if(power <= 1) // handle nonpositive powers (assuming 'value' is large)
      {
        if(power == 1) return value;
        else if(power == 0) return One; // x^0 == 1
        else return Zero; // x^y truncates to zero when abs(x) > 1 and y < 0
      }

      bool negative = value.IsNegative;
      if(negative)
      {
        value = -value;
        if((power & 1) == 0) negative = false; // only odd values with odd powers cause odd results
      }

      // compute the result by repeated squaring
      Integer result = One;
      while(true)
      {
        if((power & 1) != 0) result *= value;
        power >>= 1;
        if(power == 0) break;
        value = value.Square();
      }

      if(negative) result = -result;
      return result;
    }

    /// <summary>Generates a random nonnegative <see cref="Integer"/> up to the given bit length.</summary>
    public static Integer Random(RandomNumberGenerator rng, int maxBits)
    {
      if(rng == null) throw new ArgumentNullException();
      if((uint)maxBits > uint.MaxValue/8) throw new ArgumentOutOfRangeException();
      if(maxBits == 0) return Zero;
      uint[] data = new uint[((uint)maxBits+31)/32];
      for(int i = 0; i < data.Length-1; i++) data[i] = rng.NextUInt32();
      data[data.Length-1] = (maxBits & 31) == 0 ? rng.NextUInt32() : rng.NextBits(maxBits & 31);
      return new Integer(data, false);
    }

    /// <summary>Attempts to parse an <see cref="Integer"/> value formatted according to the current culture and returns true if the
    /// parse was successful.
    /// </summary>
    public static bool TryParse(string str, out Integer value)
    {
      return TryParse(str, NumberStyles.Any, null, out value);
    }

    /// <summary>Attempts to parse an <see cref="Integer"/> value formatted according to the given provider (or the current culture if the
    /// provider is null) and returns true if the parse was successful.
    /// </summary>
    public static bool TryParse(string str, IFormatProvider provider, out Integer value)
    {
      return TryParse(str, NumberStyles.Any, provider, out value);
    }

    /// <summary>Attempts to parse an <see cref="Integer"/> value formatted according to the given provider (or the current culture if the
    /// provider is null) and returns true if the parse was successful.
    /// </summary>
    public static bool TryParse(string str, NumberStyles style, IFormatProvider provider, out Integer value)
    {
      Exception ex;
      return TryParse(str, style, provider, out value, out ex);
    }

    /// <summary>An <see cref="Integer"/> value equal to negative one.</summary>
    public static readonly Integer MinusOne = new Integer(new uint[] { 1 }, SignBit|1);
    /// <summary>An <see cref="Integer"/> value equal to one.</summary>
    public static readonly Integer One = new Integer(MinusOne.data, 1); // use the same array for One and MinusOne (see CloneIfNecessary)
    /// <summary>An <see cref="Integer"/> value equal to zero.</summary>
    public static readonly Integer Zero = new Integer();

    const uint SignBit = 0x80000000;

    #region ICloneable Members
    object ICloneable.Clone()
    {
      return Clone();
    }
    #endregion

    #region IComparable Members
    int IComparable.CompareTo(object obj)
    {
      if(!(obj is Integer))
      {
        throw new ArgumentException("Expected a " + GetType().FullName + " value but received a " +
                                    (obj == null ? "null" : obj.GetType().FullName) + " value.");
      }
      return CompareTo((Integer)obj);
    }
    #endregion

    #region IConvertible Members
    TypeCode IConvertible.GetTypeCode()
    {
      return TypeCode.Object;
    }

    bool IConvertible.ToBoolean(IFormatProvider provider)
    {
      return !IsZero; // use the same behavior as the built-in integer types
    }

    byte IConvertible.ToByte(IFormatProvider provider)
    {
      uint uiValue = ToUInt32();
      if(uiValue > byte.MaxValue) throw new OverflowException();
      return (byte)uiValue;
    }

    char IConvertible.ToChar(IFormatProvider provider)
    {
      uint uiValue = ToUInt32();
      if(uiValue > (uint)char.MaxValue) throw new OverflowException();
      return (char)uiValue;
    }

    DateTime IConvertible.ToDateTime(IFormatProvider provider)
    {
      throw new InvalidCastException("Cannot convert " + GetType().FullName + " to " + typeof(DateTime).FullName + ".");
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
      int iValue = ToInt32();
      if(iValue > short.MaxValue || iValue < short.MinValue) throw new OverflowException();
      return (short)iValue;
    }

    int IConvertible.ToInt32(IFormatProvider provider)
    {
      return ToInt32();
    }

    long IConvertible.ToInt64(IFormatProvider provider)
    {
      return ToInt64();
    }

    sbyte IConvertible.ToSByte(IFormatProvider provider)
    {
      int iValue = ToInt32();
      if(iValue > sbyte.MaxValue || iValue < sbyte.MinValue) throw new OverflowException();
      return (sbyte)iValue;
    }

    float IConvertible.ToSingle(IFormatProvider provider)
    {
      return (float)this;
    }

    object IConvertible.ToType(Type conversionType, IFormatProvider provider)
    {
      if(conversionType == typeof(FP107)) return new FP107(this);
      else if(conversionType == typeof(Rational)) return new Rational(this);
      else return MathHelpers.DefaultConvertToType(this, conversionType, provider);
    }

    ushort IConvertible.ToUInt16(IFormatProvider provider)
    {
      uint uiValue = ToUInt32();
      if(uiValue > ushort.MaxValue) throw new OverflowException();
      return (ushort)uiValue;
    }

    uint IConvertible.ToUInt32(IFormatProvider provider)
    {
      return ToUInt32();
    }

    ulong IConvertible.ToUInt64(IFormatProvider provider)
    {
      return ToUInt64();
    }
    #endregion

    /// <summary>Returns the digits of the number's decimal representation.</summary>
    internal static byte[] GetDigits(Integer value)
    {
      // generate the digits in reverse order by repeatedly dividing by 10
      value = Abs(value.Clone()); // clone the value so we can divide in-place
      var digits = new System.Collections.Generic.List<byte>(value.GetElementCount()*10);
      do digits.Add((byte)value.UnsafeDivide(10u)); while(!value.IsZero);
      digits.Reverse(); // then reverse the digits to put them in the right order
      return digits.ToArray();
    }

    /// <summary>Converts an array of decimal digits into a <see cref="Integer"/> value.</summary>
    internal static Integer ParseDigits(byte[] digits, int digitCount)
    {
      Integer value = Integer.Zero;
      for(int i=0; i<digitCount; ) // while there are digits left to parse...
      {
        // parse up to 9 digits at a time, sticking them into a uint to avoid as much Integer math as possible. although we can parse more
        // than 9 digits into a uint, 10^9 is the largest power of 10 that fits in a uint below
        uint someDigits = 0;
        int numDigits = Math.Min(9, digitCount-i);
        for(int end=i+numDigits; i<end; i++) someDigits = someDigits*10 + digits[i];

        // then combine the digits with the Integer
        value.UnsafeMultiply((uint)PowersOf10[numDigits]); // value = value * 10^numDigits + someDigits
        value.UnsafeAdd(someDigits);
      }
      return value;
    }

    internal static bool TryParse(string str, NumberStyles style, IFormatProvider provider, out Integer value, out Exception ex)
    {
      ex    = null;
      value = default(Integer);
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
      CultureInfo culture = provider == null ? CultureInfo.CurrentCulture : provider as CultureInfo;
      bool negative = false;

      // parse the number out of hexadecimal format if we can
      if((style & NumberStyles.HexNumber) != 0 && end-start >= 3) // if the number is long enough to be in hexadecimal format...
      {
        int i = start;
        if(str.StartsAt(i, nums.NegativeSign))
        {
          negative = true;
          i += nums.NegativeSign.Length;
          while(i < str.Length && char.IsWhiteSpace(str[i])) i++;
        }
        if(end-start >= 3 && str[i] == '0' && char.ToUpperInvariant(str[i+1]) == 'X') // if it starts with 0x...
        {
          bool hexadecimalFormat = true;
          i += 2;
          for(int j=i; j<end; j++) // check if it really is hexadecimal format
          {
            if(!BinaryUtility.IsHex(str[j])) { hexadecimalFormat = false; break; }
          }
          if(hexadecimalFormat)
          {
            uint[] data = new uint[((end-i)+7)/8]; // 8 nibbles per uint, rounded up
            for(int di=0; di<data.Length; end -= 8, di++)
            {
              uint word = 0;
              for(int j=Math.Max(i, end-8); j < end; j++) word = (word<<4) | BinaryUtility.ParseHex(str[j]);
              data[di] = word;
            }
            value = new Integer(data, negative, false);
            return true;
          }
        }
      }

      // try to parse the digits out of the string
      int digitCount, exponent;
      byte[] digits = NumberFormat.ParseSignificantDigits(str, start, end, style, nums, out digitCount, out exponent, out negative);
      if(digits == null)
      {
        ex = new FormatException();
        return false;
      }

      // we've got a valid set of digits, so parse them and scale them by the exponent
      value = Integer.ParseDigits(digits, digitCount);
      if(exponent < 0) value /= Pow(10, -exponent);
      else if(exponent > 0) value *= Pow(10, exponent);
      if(negative) value = -value;
      return true;
    }

    void CloneIfNecessary() // this assumes One.data == MinusOne.data and we have no other built-in non-zero values
    {
      if(data == One.data) data = new uint[1] { 1 };
    }

    static uint[] CloneIfNecessary(uint[] data)
    {
      return data != One.data ? data : new uint[1] { 1 };
    }

    int CompareMagnitudes(uint other)
    {
      if(BitLength > 32) return 1;
      uint value = ToUInt32Fast();
      return value > other ? 1 : value < other ? -1 : 0;
    }

    int CompareMagnitudes(ulong other)
    {
      if(BitLength > 64) return 1;
      ulong value = ToUInt64Fast();
      return value > other ? 1 : value < other ? -1 : 0;
    }

    int CompareMagnitudes(Integer other)
    {
      int cmp = BitLength - other.BitLength;
      if(cmp == 0 && !IsZero) cmp = Compare(data, BitLength, other.data, other.BitLength);
      return cmp;
    }

    int ComputeBitLength()
    {
      return ComputeBitLength(data, GetElementCount()-1);
    }

    void ExpandArrayTo(int newLength)
    {
      if(newLength != 0 && (data == null || data.Length < newLength))
      {
        uint[] newData = new uint[newLength];
        if(data != null) data.FastCopy(newData, data.Length);
        data = newData;
      }
    }

    int GetElementCount()
    {
      return GetElementCount(BitLength);
    }

    void MakeZero()
    {
      data = null;
      info = 0;
    }

    /// <summary>Gets the magnitude of this <see cref="Integer"/>, assuming it fits within 32 bits.</summary>
    uint ToUInt32Fast()
    {
      return IsZero ? 0 : data[0];
    }

    /// <summary>Gets the magnitude of this <see cref="Integer"/>, assuming it fits within 64 bits.</summary>
    ulong ToUInt64Fast()
    {
      if(IsZero) return 0;
      ulong value = data[0];
      if(BitLength > 32) value |= (ulong)data[1] << 32;
      return value;
    }

    /// <summary>Gets the magnitude of this <see cref="Integer"/>, assuming it fits within 64 bits and isn't zero.</summary>
    ulong ToNonZeroUInt64Fast()
    {
      ulong value = data[0];
      if(BitLength > 32) value |= (ulong)data[1] << 32;
      return value;
    }

    void UnsafeAddMagnitude(uint value)
    {
      if(IsZero)
      {
        UnsafeSetMagnitude(value);
      }
      else
      {
        CloneIfNecessary();
        if(Add(data, value))
        {
          BitLength = ComputeBitLength();
        }
        else
        {
          uint[] newData = new uint[data.Length+1];
          data.FastCopy(newData, data.Length);
          newData[data.Length] = 1;
          BitLength = data.Length*32 + 1;
          data = newData;
        }
      }
    }

    void UnsafeBitwiseAnd(uint value, bool bNegative) // 'value' is already in two's complement
    {
      if(!IsZero)
      {
        CloneIfNecessary();
        if(IsPositive && !bNegative)
        {
          data[0] &= value;
          if(data[0] == 0)
          {
            MakeZero();
          }
          else
          {
            for(int i=1, count = GetElementCount(); i<count; i++) data[i] = 0;
            BitLength = ComputeBitLength(data[0]);
          }
        }
        else
        {
          bool aBorrow = IsNegative, rNegative = aBorrow & bNegative, rBorrow = rNegative;
          uint av = data[0];
          if(aBorrow)
          {
            if(av-- != 0) aBorrow = false;
            av = ~av;
          }
          value &= av;
          if(rBorrow)
          {
            if(value-- != 0) rBorrow = false;
            value = ~value;
          }
          data[0] = value;

          int i=1, count = GetElementCount();
          if(!bNegative)
          {
            if(data[0] == 0)
            {
              MakeZero();
            }
            else
            {
              for(; i<count; i++) data[i] = 0;
              BitLength = ComputeBitLength(data[0]);
            }
          }
          else
          {
            for(; (aBorrow | rBorrow) && i<count; i++)
            {
              av = data[i];
              if(aBorrow && av-- != 0) aBorrow = false;
              av = ~av;
              if(rNegative)
              {
                if(rBorrow && av-- != 0) rBorrow = false;
                av = ~av;
              }
              data[i] = av;
            }
            if(i == count) BitLength = ComputeBitLength();
          }

          if(rNegative != IsNegative) info ^= SignBit;
        }
      }
    }

    void UnsafeBitwiseOr(uint value, bool bNegative) // 'value' is already in two's complement
    {
      if(value != 0)
      {
        if(IsZero)
        {
          UnsafeSet(bNegative ? (uint)-(int)value : value);
          if(bNegative) info |= SignBit;
        }
        else
        {
          CloneIfNecessary();
          if(IsPositive && !bNegative) // if both numbers are non-negative, take the fast and easy route
          {
            data[0] |= value;
            if(BitLength < 32) BitLength = ComputeBitLength(data[0]);
          }
          else
          {
            bool aBorrow = IsNegative, rBorrow = true;
            uint av = data[0];
            if(aBorrow)
            {
              if(av-- != 0) aBorrow = false;
              av = ~av;
            }
            value |= av;
            if(value-- != 0) rBorrow = false;
            data[0] = ~value;

            int i = 1, count = GetElementCount();
            if(bNegative)
            {
              if(rBorrow) data[i++] = 1;
              BitLength = ComputeBitLength(data, i-1);
              for(; i<count; i++) data[i] = 0;
            }
            else
            {
              for(; (aBorrow | rBorrow) && i<count; i++)
              {
                av = data[i];
                if(aBorrow && av-- != 0) aBorrow = false;
                av = ~av;
                if(rBorrow && av-- != 0) rBorrow = false;
                data[i] = ~av;
              }
              if(i == count) BitLength = ComputeBitLength(data, count-1);
            }

            if(IsPositive) info ^= SignBit;
          }
        }
      }
    }

    void UnsafeDecrementMagnitude()
    {
      CloneIfNecessary();
      for(int i=0; i<data.Length; i++)
      {
        uint value = data[i]--;
        if(value != 0)
        {
          // if the high word was a power of two before being decremented, then the bit length decreased
          if(i == data.Length-1 && (value & (value-1)) == 0) BitLength--;
          break;
        }
      }
    }

    void UnsafeIncrementMagnitude()
    {
      CloneIfNecessary();
      for(int i=0; i<data.Length; i++)
      {
        uint value = ++data[i];
        if(value != 0)
        {
          // if the high word was a power of two after being incremented, then the bit length increased. otherwise, return now
          if(i != data.Length-1 || (value & (value-1)) != 0) return;
        }
      }

      int newBitLength = BitLength+1;
      if(newBitLength < 0) throw new OverflowException();
      BitLength = newBitLength;

      if(GetElementCount(newBitLength) != data.Length) // if the array became all zeros, then we need to add a 1 bit to the next element
      {
        uint[] newData = new uint[data.Length+1];
        data.FastCopy(newData, data.Length);
        newData[data.Length] = 1;
        data = newData;
      }
    }

    void UnsafeLeftShift(uint shift)
    {
      if(!IsZero)
      {
        Debug.Assert(shift <= (uint)int.MaxValue+1);
        int newBitLength = (int)((uint)BitLength + shift);
        if(newBitLength < 0) throw new OverflowException();
        int newLength = GetElementCount(newBitLength);
        uint[] result = data == null || data.Length < newLength ? new uint[newLength] : CloneIfNecessary(data);
        LeftShift(data, BitLength, shift, result);
        data      = result;
        BitLength = newBitLength;
      }
    }

    void UnsafeRightShift(uint shift)
    {
      Debug.Assert(shift <= (uint)int.MaxValue+1);
      int newBitLength = (int)((uint)BitLength - shift);
      if(newBitLength <= 0)
      {
        MakeZero();
      }
      else
      {
        CloneIfNecessary();
        RightShift(data, BitLength, shift, data);
        BitLength = newBitLength;
      }
    }

    void UnsafeSetMagnitude(uint value)
    {
      if(value == 0)
      {
        MakeZero();
      }
      else
      {
        if(data == null)
        {
          data = new uint[1] { value };
        }
        else
        {
          CloneIfNecessary();
          data[0] = value;
          for(int i=1, count=GetElementCount(); i<count; i++) data[i] = 0;
        }
        BitLength = ComputeBitLength(value);
      }
    }

    void UnsafeSetMagnitude(ulong value)
    {
      if(value == 0)
      {
        MakeZero();
      }
      else
      {
        if(data == null || data.Length < 2)
        {
          data = new uint[2] { (uint)value, (uint)(value>>32) };
        }
        else
        {
          CloneIfNecessary();
          data[0] = (uint)value;
          data[1] = (uint)(value>>32);
          for(int i=2, count=GetElementCount(); i<count; i++) data[i] = 0;
        }
        BitLength = ComputeBitLength(data, 1);
      }
    }

    void UnsafeSubtractCore(uint value)
    {
      int cmp = CompareMagnitudes(value);
      if(cmp == 0)
      {
        MakeZero();
      }
      else
      {
        CloneIfNecessary();
        if(cmp > 0)
        {
          Subtract(data, value);
          BitLength = ComputeBitLength();
        }
        else // if the value is greater than this...
        {
          if(data == null) data = new uint[1] { value }; // then we have at most one element, so do the subtraction directly
          else data[0] = value - data[0];
          BitLength = ComputeBitLength(data[0]);
          info     ^= SignBit;
        }
      }
    }

    uint[] data;
    uint info;

    static uint[] Add(Integer a, uint b, ref bool bNegative)
    {
      if(b == 0)
      {
        bNegative = a.IsNegative;
        return a.data;
      }
      else if(a.IsZero) return new uint[] { b };
      else if(a.IsNegative == bNegative) return b == 1 ? IncrementMagnitude(a) : AddMagnitudes(a, b);
      else if(a.CompareMagnitudes(b) >= 0)
      {
        bNegative = a.IsNegative;
        return b == 1 ? DecrementMagnitude(a) : SubtractMagnitudes(a, b);
      }
      else return new uint[] { b - a.data[0] };
    }

    static bool Add(uint[] a, uint value)
    {
      uint sum = a[0] + value;
      a[0] = sum;
      if(sum < value) // if there was overflow...
      {
        bool carry = true;
        for(int i=1; i<a.Length; i++) // carry
        {
          if(++a[i] != 0) { carry = false; break; }
        }
        return !carry; // return false if we need to add an extra one bit
      }
      return true;
    }

    static void Add(uint[] a, int aBitLength, uint[] b, int bBitLength, uint[] result)
    {
      if(aBitLength < bBitLength) // ensure that 'a' is the larger array (or that they're equal in size)
      {
        Utility.Swap(ref a, ref b);
        Utility.Swap(ref aBitLength, ref bBitLength);
      }

      // add the values from both arrays
      uint carry = 0;
      int i, count;
      for(i=0, count=GetElementCount(bBitLength); i<count; i++)
      {
        uint sum = a[i] + b[i] + carry;
        if(carry == 0)
        {
          if(sum < a[i]) carry = 1; // without carry, overflow occurs when a + b < a (or < b)
        }
        else
        {
          if(sum > a[i]) carry = 0; // with carry, overflow occurs when a + b + 1 <= a (or <= b)
        }
        result[i] = sum;
      }

      // now the elements from b have been added to a. there may still be additional elements in a
      count = GetElementCount(aBitLength);

      // if we were carrying, continue adding the carry to elements in a
      if(carry != 0)
      {
        while(i < count)
        {
          uint sum = a[i] + 1;
          result[i] = sum;
          i++;
          if(sum != 0) { carry = 0; break; }
        }
      }

      // now we've exhaused the elements in a that need carry applied
      if(carry != 0) // if there is carry, there are no more elements in a, so just set the last element to 1
      {
        result[i] = 1;
      }
      else if(result != a) // if there's no more carry and we're not adding in-place, just copy the remaining elements
      {
        for(; i < count; i++) result[i] = a[i];
      }
    }

    static uint[] AddMagnitudes(Integer a, uint b)
    {
      int maxBitLength = Math.Max(a.BitLength, ComputeBitLength(b)) + 1;
      if(maxBitLength < 0) throw new OverflowException(); // handle the addition overflowing
      uint[] result = new uint[GetElementCount(maxBitLength)];
      a.data.FastCopy(result, a.GetElementCount());
      Add(result, b);
      return result;
    }

    static uint[] AddMagnitudes(Integer a, Integer b)
    {
      int maxBitLength = Math.Max(a.BitLength, b.BitLength) + 1;
      if(maxBitLength < 0) throw new OverflowException(); // handle the addition overflowing
      uint[] result = new uint[GetElementCount(maxBitLength)];
      Add(a.data, a.BitLength, b.data, b.BitLength, result);
      return result;
    }

    /// <summary>Copies a region of bits from one array to another.</summary>
    static void BitCopy(uint[] src, int srcIndex, uint[] dest, int destIndex, int bitCount)
    {
      if(bitCount == 1) // if we're only copying a single bit...
      {
        uint smask = 1u << (srcIndex & 31), dmask = 1u << (destIndex & 31);
        destIndex >>= 5;
        if((src[srcIndex>>5] & smask) == 0) dest[destIndex] &= ~dmask;
        else dest[destIndex] |= dmask;
      }
      else if(((srcIndex | destIndex) & 31) == 0) // if the source and destination are aligned on word boundaries...
      {
        srcIndex  >>= 5;
        destIndex >>= 5;
        int whole = bitCount >> 5;
        for(int i=0; i<whole; i++) dest[destIndex+i] = src[srcIndex+i];
        int part = bitCount & 31;
        if(part != 0)
        {
          uint mask = (1u << part) - 1;
          dest[destIndex+whole] = dest[destIndex+whole] & ~mask | (src[srcIndex+whole] & mask);
        }
      }
      else // the source and/or destination is not aligned...
      {
        int sbi = srcIndex & 31, sbc = 32-sbi, dbi = destIndex & 31, dbc = 32-dbi;
        srcIndex  >>= 5;
        destIndex >>= 5;

        // first handle the case of copying 32-bit chunks, which allow us to write whole destination elements
        int whole = bitCount >> 5;
        if(whole != 0)
        {
          // we may need to handle the first and last elements specially, since they may have bits that need to be preserved
          uint srcBits = src[srcIndex] >> sbi;
          if(sbc < dbc) srcBits |= src[++srcIndex] << sbc; // if the first source element doesn't provide enough bits, add the second
          uint destValue = dbi == 0 ? 0 : dest[destIndex] & ~(((1u<<dbc)-1) << dbi);
          dest[destIndex] = destValue | (srcBits << dbi);
          destIndex++; // now we've finished the first destination element

          --whole;
          if(whole != 0) // if there are any destination elements we can write without preserving any of their bits...
          {
            if(sbc < dbc)
            {
              // xxxxxx|01 234567|AB CDEFGH|01 234567|xx
              // xxx|01234 567|ABCDE FGH|01234 567|xxxxx
              // or
              // xxx|01234 567|ABCDE FGH|01234 567|xxxxx
              // |01234567 |ABCDEFGH |01234567
              srcBits = src[srcIndex];
              int cdiff = dbc - sbc, cdc = 32 - cdiff; // note: cdiff != 32 and cdc != 32
              do
              {
                // at this point, srcBits has 32 bits in it, of which cdiff were used
                destValue = srcBits >> cdiff; // get the remaining bits
                srcBits = src[++srcIndex];
                dest[destIndex++] = destValue | (srcBits << cdc); // combine them with the 32-cdiff bits from the next source element
              } while(--whole != 0);
            }
            else
            {
              // xxx|01234 567|ABCDE FGH|01234 567|xxxxx
              // xxxxxx|01 234567|AB CDEFGH|01 234567|xx
              // or
              // xxxx|0123 4567|ABCD EFGH|0123 4567|xxxx
              // xxxx|0123 4567|ABCD EFGH|0123 4567|xxxx
              // or
              // |01234567 |ABCDEFGH |01234567
              // xxx|01234 567|ABCDE FGH|01234 567|xxxxx
              int cdiff = sbc - dbc; // note: cdiff != 32
              do
              {
                // at this point, srcBits has exactly sbc bits in it, of which dbc were used and cdiff were unused
                destValue = srcBits >> dbc; // grab the unused source bits (dbc != 32 here)
                srcBits = src[++srcIndex]; // get the next 32 source bits
                dest[destIndex++] = destValue | (srcBits << cdiff); // complete the next destination value, using all but cdiff bits
                srcBits >>= sbi;
              } while(--whole != 0);
            }
          }

          if(dbi != 0) // if we have to write a final destination element, preserving some of its bits...
          {
            uint dmask1 = ~((1u<<dbi)-1);
            if(sbc < dbc)
            {
              destValue = src[srcIndex] >> (dbc - sbc);
            }
            else
            {
              destValue = srcBits >> dbc;
              if(sbi != 0) destValue |= src[++srcIndex] << (sbc - dbc);
            }
            dest[destIndex] = dest[destIndex] & ~dmask1 | (destValue & dmask1);
          }
        }

        // now copy the remaining bits (up to 31)
        int part = bitCount & 31;
        if(part != 0)
        {
          // grab all the source bits
          uint partMask = (1u<<part)-1, srcBits = src[srcIndex] >> sbi;
          if(sbc < part) srcBits |= src[++srcIndex] << sbc;
          srcBits &= partMask;

          dest[destIndex] = dest[destIndex] & ~(partMask << dbi) | (srcBits << dbi);
          if(dbc < part)
          {
            destIndex++;
            dest[destIndex] = dest[destIndex] & ~((1u<<(part-dbc))-1) | (srcBits >> dbc);
          }
        }
      }
    }

    static uint[] BitwiseAnd(Integer a, Integer b)
    {
      if(a.IsZero || b.IsZero) return null;

      int newBitLength;
      if(a.IsPositive && b.IsPositive) newBitLength = Math.Min(a.BitLength, b.BitLength);
      else if(a.BitLength >= b.BitLength) newBitLength = b.IsNegative ? a.BitLength : b.BitLength;
      else newBitLength = a.IsNegative ? b.BitLength : a.BitLength;

      uint[] result = new uint[GetElementCount(newBitLength)];
      BitwiseAnd(a.data, a.BitLength, a.IsNegative, b.data, b.BitLength, b.IsNegative, result);
      return result;
    }

    static void BitwiseAnd(uint[] a, int aBitLength, bool aNegative, uint[] b, int bBitLength, bool bNegative, uint[] result)
    {
      Debug.Assert(aBitLength > 0 && bBitLength > 0 && result != a && result != b);

      if(!aNegative && !bNegative)
      {
        int count = GetElementCount(Math.Min(aBitLength, bBitLength));
        for(int i=0; i<count; i++) result[i] = a[i] & b[i];
      }
      else
      {
        // we have to treat the negative numbers as though they were stored in two's complement. this means that we must effectively
        // subtract 1, negate the bits, and tack on an additional 1 bit in the high position if the original value was a power of 2. any
        // bits beyond those directly stored are assumed to be 1s. we can do this more quickly by just negating the entire uint, in which
        // case we never need to add additional 1 bits beyond those already in the uint. finally, if the result is negative (i.e. if both
        // a and b are negative), then we need to apply two's complement to the result, subtracting 1 and negating again
        if(aBitLength < bBitLength)
        {
          Utility.Swap(ref a, ref b);
          Utility.Swap(ref aBitLength, ref bBitLength);
          Utility.Swap(ref aNegative, ref bNegative);
        }
        int i, count = GetElementCount(bBitLength);
        bool aBorrow = aNegative, bBorrow = bNegative, rNegative = aNegative & bNegative, rBorrow = rNegative;
        for(i=0; i<count; i++)
        {
          uint av = a[i];
          if(aNegative)
          {
            if(aBorrow && av-- != 0) aBorrow = false;
            av = ~av;
          }
          uint bv = b[i];
          if(bNegative)
          {
            if(bBorrow && bv-- != 0) bBorrow = false;
            bv = ~bv;
          }
          bv &= av; // reuse bv for the result
          if(rNegative)
          {
            if(rBorrow && bv-- != 0) rBorrow = false;
            bv = ~bv;
          }
          result[i] = bv;
        }

        if(bNegative)
        {
          count = GetElementCount(aBitLength);
          // if b was negative, then by negating it all the higher bits become 1s, which would leave the remaining elements from a
          // unchanged. so basically we want to copy the remaining elements from a
          for(; (aBorrow | rBorrow) && i<count; i++) // if we're still borrowing from either a or the result, do the more complex loop.
          {                                          // we can't be borrowing from b at this point because we assume b != 0
            uint av = a[i];
            if(aBorrow && av-- != 0) aBorrow = false;
            av = ~av;
            if(rNegative)
            {
              if(rBorrow && av-- != 0) rBorrow = false;
              av = ~av;
            }
            result[i] = av;
          }

          if(result != a) // at this point, we're no longer borrowing, so just copy the remainder
          {
            for(; i<count; i++) result[i] = a[i];
          }
        }
      }
    }

    static uint[] BitwiseOr(Integer a, Integer b)
    {
      if(a.IsZero) return b.data; // x | 0 = x
      else if(b.IsZero) return a.data;

      int newBitLength = Math.Max(a.BitLength, b.BitLength);
      uint[] result = new uint[GetElementCount(newBitLength)];
      BitwiseOr(a.data, a.BitLength, a.IsNegative, b.data, b.BitLength, b.IsNegative, result);
      return result;
    }

    static void BitwiseOr(uint[] a, int aBitLength, bool aNegative, uint[] b, int bBitLength, bool bNegative, uint[] result)
    {
      Debug.Assert(aBitLength > 0 && bBitLength > 0 && result != a && result != b);

      if(aBitLength < bBitLength)
      {
        Utility.Swap(ref a, ref b);
        Utility.Swap(ref aBitLength, ref bBitLength);
        Utility.Swap(ref aNegative, ref bNegative);
      }

      int i, count = GetElementCount(bBitLength);
      if(!aNegative && !bNegative) // if both numbers are non-negative, take the fast and easy route
      {
        for(i=0; i<count; i++) result[i] = a[i] | b[i];
        for(count=GetElementCount(aBitLength); i<count; i++) result[i] = a[i];
      }
      else
      {
        // we have to treat the negative numbers as though they were stored in two's complement. this means that we must effectively
        // subtract 1, negate the bits, and tack on an additional 1 bit in the high position if the original value was a power of 2. any
        // bits beyond those directly stored are assumed to be 1s. we can do this more quickly by just negating the entire uint, in which
        // case we never need to add additional 1 bits beyond those already in the uint. finally, we need to apply two's complement
        // to the result, subtracting 1 and negating again
        bool aBorrow = aNegative, bBorrow = bNegative, rBorrow = true;
        for(i=0; i<count; i++)
        {
          uint av = a[i];
          if(aNegative)
          {
            if(aBorrow && av-- != 0) aBorrow = false;
            av = ~av;
          }
          uint bv = b[i];
          if(bNegative)
          {
            if(bBorrow && bv-- != 0) bBorrow = false;
            bv = ~bv;
          }
          bv |= av; // reuse bv for the result
          if(rBorrow && bv-- != 0) rBorrow = false;
          result[i] = ~bv;
        }

        count = GetElementCount(aBitLength);
        if(i < count)
        {
          if(bNegative) // if b was negative, then by negating it all the higher bits become 1s, so we would set the remaining elements to
          {             // 0xFFFFFFFF. but then when we negated them, they'd be back to zero, unless we were still borrowing, in which case
            if(rBorrow) result[i] = 1; // the next element would be a 1
          }
          else // if only a was negative, the continue converting it into two's complement
          {
            for(; (aBorrow | rBorrow) && i<count; i++) // if we're still borrowing from either a or the result, do the more complex loop
            {
              uint av = a[i];
              if(aBorrow && av-- != 0) aBorrow = false;
              av = ~av;
              if(rBorrow && av-- != 0) rBorrow = false;
              result[i] = ~av;
            }

            if(result != a) // at this point, we're no longer borrowing, so just copy the remainder
            {
              for(; i<count; i++) result[i] = a[i];
            }
          }
        }
      }
    }

    static int Compare(uint[] a, int aBitLength, uint[] b, int bBitLength)
    {
      int cmp = aBitLength - bBitLength;
      if(cmp == 0 && bBitLength != 0) // if the values have the same bit length and neither are zero...
      {
        int i = GetElementCount(bBitLength)-1;
        do
        {
          if(a[i] != b[i]) return a[i] > b[i] ? 1 : -1;
        } while(--i >= 0);
      }
      return cmp;
    }

    static int ComputeBitLength(uint value)
    {
      return 32 - BinaryUtility.CountLeadingZeros(value);
    }

    static int ComputeBitLength(ulong value)
    {
      return 64 - BinaryUtility.CountLeadingZeros(value);
    }

    static int ComputeBitLength(uint[] data)
    {
      return ComputeBitLength(data, data.Length-1);
    }

    static int ComputeBitLength(uint[] data, int i)
    {
      for(; i >= 0; i--)
      {
        if(data[i] != 0) return ComputeBitLength(data[i]) + i*32;
      }
      return 0;
    }

    static void Decrement(uint[] data, int bitLength, uint[] result)
    {
      int i;
      for(i=0; i<data.Length; i++)
      {
        uint value = data[i];
        result[i] = value - 1;
        if(value != 0) break;
      }
      if(data != result)
      {
        int count = GetElementCount(bitLength);
        for(i++; i < count; i++) result[i] = data[i];
      }
    }

    static uint[] DecrementMagnitude(Integer value)
    {
      if(value.IsZero) return One.data;
      if(value.BitLength == 1) return null; // if magnitude == 1, decrementing it makes it zero
      uint[] result = new uint[value.GetElementCount()];
      Decrement(value.data, value.BitLength, result);
      return result;
    }

    static uint DivideMagnitudes(uint[] a, int aBitLength, uint b, uint[] result)
    {
      if(b == 0) throw new DivideByZeroException();

      if(aBitLength == 0)
      {
        return 0;
      }
      else if((b & (b-1)) == 0) // if dividing by a power of two, we can take a fast path...
      {
        // when b is a power of 2, a % b == a & (b-1) and a / b == (a >> log2(b)). the base-2 logarithm of a power of two equals the
        // number of trailing zero bits
        uint remainder = a[0] & (b-1);
        RightShift(a, aBitLength, (uint)BinaryUtility.CountTrailingZeros(b), result); 
        return remainder;
      }
      else
      {
        ulong remainder = 0;
        for(int i=GetElementCount(aBitLength)-1; i >= 0; i--)
        {
          remainder = (remainder<<32) | a[i];
          Debug.Assert(i < result.Length || remainder/b == 0);
          if(i < result.Length) result[i] = (uint)(remainder / b);
          remainder %= b;
        }
        return (uint)remainder;
      }
    }

    static uint[] DivideMagnitudes(Integer a, uint b)
    {
      if(b == 0) throw new DivideByZeroException();
      else if(a.IsZero || b == 1) return a.data;

      int maxBitLength = a.BitLength - ComputeBitLength(b) + 1;
      if(maxBitLength <= 0) return null;
      
      uint[] result = new uint[GetElementCount(maxBitLength)];
      DivideMagnitudes(a.data, a.BitLength, b, result);
      return result;
    }

    static uint[] DivideMagnitudes(Integer a, Integer b, out uint[] remainder)
    {
      if(b.BitLength <= 1) // if abs(b) is 0 or 1...
      {
        if(b.IsZero) throw new DivideByZeroException();
        remainder = null; // n / 1 == n
        return a.data;
      }

      int cmp = a.CompareMagnitudes(b);
      if(cmp < 0) // smaller / larger = 0 r smaller
      {
        remainder = a.data;
        return null;
      }
      else if(cmp == 0) // n/n == 1
      {
        remainder = null;
        return One.data;
      }

      // do binary long division, with some optimizations
      uint[] quotient = new uint[GetElementCount(a.BitLength - b.BitLength + 1)];
      if(b.BitLength <= 32) // if the divisor fits in 32-bits, use a simpler method
      {
        remainder = new uint[] { DivideMagnitudes(a.data, a.BitLength, b.data[0], quotient) };
      }
      else
      {
        uint[] rem = new uint[GetElementCount(b.BitLength+1)]; // an extra bit isn't necessary to store the remainder but simplifies the code
        for(int rembits=0, bit=a.BitLength-1; bit >= 0; bit--)
        {
          // left-shift the remainder by the amount needed to make rembits == b.BitLength (but at least shift by one)
          int shift = Math.Max(1, Math.Min(bit+1, b.BitLength-rembits));
          if(rembits != 0) LeftShift(rem, rembits, (uint)shift, rem);
          // then copy the numerator bits into the space we just created
          bit -= shift-1;
          BitCopy(a.data, bit, rem, 0, shift);
          if(rembits != 0) rembits += shift;
          else rembits = ComputeBitLength(rem);

          if(Compare(rem, rembits, b.data, b.BitLength) >= 0) // if remainder >= b...
          {
            Subtract(rem, rembits, b.data, b.BitLength, rem);
            rembits = ComputeBitLength(rem);
            quotient[bit>>5] |= 1u << (bit&31); // set the current bit within the quotient
          }
        }
        remainder = rem;
      }
      return quotient;
    }

    static int GetElementCount(int bitLength)
    {
      return (bitLength + 31) >> 5;
    }

    static bool Increment(uint[] data, int bitLength, uint[] result)
    {
      int i;
      for(i=0; i<data.Length; i++)
      {
        uint sum = data[i] + 1;
        result[i] = sum;
        if(sum != 0) break;
      }

      if(i == data.Length)
      {
        result[i] = 1;
      }
      else if(data != result)
      {
        int count = GetElementCount(bitLength);
        for(i++; i < count; i++) result[i] = data[i];
      }
      return true;
    }

    static uint[] IncrementMagnitude(Integer value)
    {
      if(value.IsZero) return One.data;

      int newBitLength = value.BitLength, count = GetElementCount(newBitLength);
      if((newBitLength&31) == 0 && value.data[count-1] == 0xFFFFFFFF) // if expansion might be necessary...
      {
        if(++newBitLength < 0) // add an extra element just in case. if that may cause overflow...
        {
          for(int i=0; i<count; i++) // do a more thorough check
          {
            if(value.data[i] != 0xFFFFFFFF) { count--; newBitLength--; break; } // if we don't need to add another element after all...
          }
          if(newBitLength < 0) throw new OverflowException();
        }
        count++;
      }

      uint[] result = new uint[count];
      Increment(value.data, value.BitLength, result);
      return result;
    }

    static Integer LeftShift(Integer value, uint shift)
    {
      Debug.Assert(shift <= (uint)int.MaxValue+1);
      if(shift == 0 || value.IsZero) return value;

      int bitLength = value.BitLength, newBitLength = (int)((uint)bitLength + shift);
      if(newBitLength < 0) throw new OverflowException();
      uint[] result = new uint[GetElementCount(newBitLength)];
      LeftShift(value.data, bitLength, shift, result);
      return new Integer(result, value.info+shift);
    }

    static void LeftShift(uint[] data, int bitLength, uint shift, uint[] result)
    {
      Debug.Assert(bitLength != 0 && shift <= (uint)int.MaxValue+1);
      int dcount = GetElementCount(bitLength);
      if(shift == 0)
      {
        if(data != result) data.FastCopy(result, dcount);
      }
      else
      {
        int whole = (int)(shift >> 5), part = (int)(shift & 31), rcount = (int)((uint)bitLength + shift), offset;
        if(rcount < 0) throw new OverflowException();
        rcount = GetElementCount(rcount);

        if(part == 0) // if we're just shifting entire elements...
        {
          for(int i=whole; i<rcount; i++) result[i] = data[i-whole];
          offset = whole;
        }
        else // otherwise, we have to combine adjacent elements
        {
          int comp = 32 - part, i = dcount-1;
          offset = rcount - dcount;
          if(offset == whole)
          {
            uint pvalue = i != 0 ? data[i-1] : 0, value = data[i];
            while(true)
            {
              result[i+offset] = (value << part) | (pvalue >> comp);
              value = pvalue;
              if(--i > 0) pvalue = data[i-1];
              else if(i == 0) pvalue = 0;
              else break;
            }
          }
          else
          {
            uint pvalue = data[i], value = 0;
            while(true)
            {
              result[i+offset] = (value << part) | (pvalue >> comp);
              value = pvalue;
              if(i > 0) pvalue = data[--i];
              else if(i == 0) { i--; pvalue = 0; }
              else break;
            }
          }
        }

        // then fill the empty space with zero bits
        if(data == result)
        {
          for(int i=0; i<whole; i++) result[i] = 0;
        }
      }
    }

    static void Multiply(uint[] a, int aBitLength, uint b, uint[] result)
    {
      Debug.Assert(b != 0);
      if((b & (b-1)) == 0) // if b is a power of two, we can take a fast path since multiplying by a power of two is the same as
      {                    // left-shifting by its base-2 logarithm, which for a power of two is the number of trailing zero bits
        LeftShift(a, aBitLength, (uint)BinaryUtility.CountTrailingZeros(b), result);
      }
      else
      {
        ulong carry = 0;
        int i, count;
        for(i=0, count=GetElementCount(aBitLength); i<count; i++)
        {
          carry += (ulong)a[i] * b;
          result[i] = (uint)carry;
          carry >>= 32;
        }
        if(carry != 0) result[i] = (uint)carry;
      }
    }

    static uint[] MultiplyMagnitudes(Integer a, uint b)
    {
      if(a.IsZero || b == 0) return null;
      else if(a.BitLength == 1) return new uint[] { b }; // if a.Abs() == 1
      else if(b == 1) return a.data;

      int maxBitLength = a.BitLength + ComputeBitLength(b);
      if(maxBitLength < 0) throw new OverflowException();
      uint[] result = new uint[GetElementCount(maxBitLength)];

      Multiply(a.data, a.BitLength, b, result);
      return result;
    }

    static uint[] MultiplyMagnitudes(Integer a, Integer b)
    {
      if(b.BitLength == 1) return a.data;
      else if(a.BitLength == 1) return b.data;
      else if(b.BitLength <= 32) return MultiplyMagnitudes(a, b.ToUInt32Fast());
      else if(a.BitLength <= 32) return MultiplyMagnitudes(b, a.ToUInt32Fast());

      int maxBitLength = a.BitLength + b.BitLength;
      if(maxBitLength < 0) throw new OverflowException();
      uint[] result = new uint[GetElementCount(maxBitLength)];

      for(int ae=a.GetElementCount(), be=b.GetElementCount(), ai=0; ai<ae; ai++)
      {
        if(a.data[ai] == 0) continue;

        ulong carry = 0;
        for(int ri=ai, bi=0; bi<be; ri++, bi++)
        {
          carry += (ulong)a.data[ai] * b.data[bi] + result[ri];
          result[ri] = (uint)carry;
          carry >>= 32;
        }
        if(carry != 0) result[ai+be] = (uint)carry;
      }
      return result;
    }

    static uint RemainderMagnitude(uint[] a, int aBitLength, uint b)
    {
      if(b == 0) throw new DivideByZeroException();

      if(aBitLength == 0)
      {
        return 0;
      }
      else if((b & (b-1)) == 0) // if b is a power of two, we can take a fast path...
      {
        return a[0] & (b-1);
      }
      else
      {
        ulong remainder = 0;
        for(int i=GetElementCount(aBitLength)-1; i >= 0; i--) remainder = ((remainder<<32) | a[i]) % b;
        return (uint)remainder;
      }
    }

    static Integer RightShift(Integer value, uint shift)
    {
      Debug.Assert(shift <= (uint)int.MaxValue+1);
      if(shift == 0) return value;
      int bitLength = value.BitLength;
      if(shift >= (uint)bitLength) return Zero;

      int newBitLength = (int)((uint)bitLength - shift);
      if(newBitLength < 0) throw new OverflowException();
      uint[] result = new uint[GetElementCount(newBitLength)];
      RightShift(value.data, bitLength, shift, result);
      return new Integer(result, value.info-shift);
    }

    static void RightShift(uint[] data, int bitLength, uint shift, uint[] result)
    {
      Debug.Assert(shift < (uint)bitLength);
      if(shift == 0)
      {
        if(data != result) data.FastCopy(result, GetElementCount(bitLength));
      }
      else
      {
        int whole = (int)(shift >> 5), part = (int)(shift & 31), rcount = GetElementCount((int)((uint)bitLength-shift));
        if(part == 0) // if we're just shifting entire elements...
        {
          for(int i=0; i<rcount; i++) result[i] = data[i+whole];
        }
        else
        {
          int comp = 32 - part, dcount = GetElementCount(bitLength), i = whole;
          uint value = data[i], nvalue = i+1 < dcount ? data[i+1] : 0;
          for(int end = i+rcount; i<end; )
          {
            result[i-whole] = (value >> part) | (nvalue << comp);
            value = nvalue;
            i++;
            nvalue = i+1 < dcount ? data[i+1] : 0;
          }

          if(data == result) // if we're shifting in-place, zero out the remaining elements
          {
            for(i -= whole; i<dcount; i++) result[i] = 0;
          }
        }
      }
    }

    static void Subtract(uint[] a, uint value)
    {
      uint av = a[0];
      a[0] = av - value;
      if(a[0] > av) // if av - value > av, then there was underflow
      {
        for(int i=1; i<a.Length; i++) // borrow
        {
          if(a[i]-- != 0) break;
        }
      }
    }

    static void Subtract(uint[] a, int aBitLength, uint[] b, int bBitLength, uint[] result)
    {
      Debug.Assert(Compare(a, aBitLength, b, bBitLength) >= 0);

      // subtract the values from both arrays
      uint borrow = 0;
      int i, count;
      for(i=0, count=GetElementCount(bBitLength); i<count; i++)
      {
        uint diff = a[i] - b[i] - borrow;
        if(borrow == 0)
        {
          if(diff > a[i]) borrow = 1; // without borrow, overflow occurs when a - b > a
        }
        else
        {
          if(diff < a[i]) borrow = 0; // with borrow, overflow occurs when a - b - 1 >= a
        }
        result[i] = diff;
      }

      // now the elements from b have been subtracted from a. there may still be additional elements in a
      count = GetElementCount(aBitLength);

      // if we were borrowing, continue borrowing from the remaining elements in a
      if(borrow != 0)
      {
        while(i < count)
        {
          uint diff = a[i];
          result[i] = diff - 1;
          i++;
          if(diff != 0) { borrow = 0; break; }
        }
      }

      // now we've exhaused the elements in a that need borrow applied, so just copy the remaining elements if we're not working in place
      if(result != a)
      {
        for(; i < count; i++) result[i] = a[i];
      }
    }

    static uint[] Subtract(Integer a, uint b, ref bool negative)
    {
      if(b == 0) // x - 0 = x
      {
        negative = a.IsNegative;
        return a.data;
      }
      else if(a.IsZero) // 0 - x = -x
      {
        negative = !negative;
        return new uint[] { b };
      }
      else if(a.CompareMagnitudes(b) >= 0)
      {
        if(a.IsNegative == negative) // big - small = medium, -big - -small = -medium
        {
          return b == 1 ? DecrementMagnitude(a) : SubtractMagnitudes(a, b);
        }
        else // big - -small = bigger, -big - small = -bigger
        {
          negative = !negative; // negative = a.IsNegative
          return b == 1 ? IncrementMagnitude(a) : AddMagnitudes(a, b);
        }
      }
      else // a < b and a fits in a uint
      {
        negative = !negative;
        if(a.IsPositive == negative) return new uint[] { b - a.data[0] }; // small - big = -(big - small)
        else return b == 1 ? IncrementMagnitude(a) : AddMagnitudes(a, b); // small - -big = bigger, -small - big = -bigger
      }
    }

    static uint[] Subtract(uint a, Integer b, ref bool negative)
    {
      if(a == 0) // 0 - x = -x
      {
        negative = b.IsPositive;
        return b.data;
      }
      else if(b.IsZero) // x - 0 = x
      {
        return new uint[] { a };
      }
      else if(b.CompareMagnitudes(a) >= 0) // if a <= b...
      {
        if(b.IsNegative == negative) // small - big = -medium, -small - -big = medium
        {
          negative = !negative;
          return a == 1 ? DecrementMagnitude(b) : SubtractMagnitudes(b, a);
        }
        else // small - -big = bigger, -small - big = -bigger
        {
          return a == 1 ? IncrementMagnitude(b) : AddMagnitudes(b, a);
        }
      }
      else // a > b and b fits in a uint
      {
        if(b.IsNegative == negative) return new uint[] { a - b.data[0] }; // big - small = medium, -big - -small = -medium
        else return a == 1 ? IncrementMagnitude(b) : AddMagnitudes(b, a); // big - -small = bigger, -big - small = -bigger
      }
    }

    static uint[] SubtractMagnitudes(Integer a, uint b)
    {
      uint[] result = new uint[a.GetElementCount()];
      a.data.FastCopy(result, result.Length);
      Subtract(result, b);
      return result;
    }

    static uint[] SubtractMagnitudes(Integer a, Integer b)
    {
      uint[] result = new uint[a.GetElementCount()];
      Subtract(a.data, a.BitLength, b.data, b.BitLength, result);
      return result;
    }

    static void ToHex(StringBuilder sb, uint value, bool capitalize, bool pad)
    {
      for(int shift=28; shift >= 0; shift -= 4)
      {
        byte nibble = (byte)((value>>shift) & 0xF);
        if(!pad)
        {
          if(nibble == 0) continue;
          else pad = true;
        }
        char c = BinaryUtility.ToHexChar(nibble);
        if(!capitalize) c = char.ToLowerInvariant(c);
        sb.Append(c);
      }
    }

    // SmallExponents[i] is the largest power that i+3 can be raised to while being represented exactly by a double
    static readonly byte[] SmallExponents = new byte[31 - 3 + 1]
    {
      33, 26, 22, 20, 18, 17, 16, 15, 15, 14, 14, 13, 13, 13, 12, 12, 12, 12, 12, 11, 11, 11, 11, 11, 11, 11, 10, 10, 10
    };
    static readonly ulong[] PowersOf10 = new ulong[20] // PowersOf10[19] (10^19) is the largest power of 10 that fits in a ulong
    {
      1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000, 10000000000, 100000000000, 1000000000000, 10000000000000,
      100000000000000, 1000000000000000, 10000000000000000, 100000000000000000, 1000000000000000000, 10000000000000000000,
    };
  }
}
