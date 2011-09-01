/*
AdamMil.Mathematics is a library that provides some useful mathematics classes
for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2011 Adam Milazzo

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
using System.Text;
using AdamMil.Utilities;

namespace AdamMil.Mathematics
{
  /// <summary>Represents a polynomial function, which is a one-dimensional function of the form: <c>c0 + c1*x + c2*x^2 + c3*x^3 + ...</c>
  /// for coefficients c0, c1, c2, c3, etc.
  /// </summary>
  [Serializable]
  public class Polynomial : ICloneable, IEquatable<Polynomial>, IFunctionallyDifferentiableFunction
  {
    /// <summary>Initalizes a new <see cref="Polynomial"/> from the given array of coefficients. The array should be ordered so that the
    /// coefficient for <c>x^i</c> is stored at index <c>i</c>. This means that the constant term (i.e. <c>c0</c>) is at the first index,
    /// the linear term (i.e. <c>c1*x</c>) is at the second index, etc.
    /// </summary>
    public Polynomial(params double[] coefficients)
    {
      if(coefficients == null) throw new ArgumentNullException();

      if(coefficients.Length == 0)
      {
        coefficients = new double[1];
        Initialize(coefficients, false);
      }
      else
      {
        Initialize(coefficients, true);
      }
    }

    Polynomial(double[] coefficients, bool clone)
    {
      Initialize(coefficients, clone);
    }

    Polynomial(double[] coefficients, int length, int degree)
    {
      double[] array = new double[length];
      ArrayUtility.SmallCopy(coefficients, array, degree+1);

      this.coefficients = array;
      Degree = degree;
    }

    /// <summary>Gets or sets a coefficient of the polynomial function, where the coefficient for <c>x^i</c> is at index <c>i</c>.
    /// Coefficients greater than the current <see cref="Degree"/> can be retrieved or set with this method. If retrieved, they will be
    /// zero. If set to a non-zero value, the degree of the polynomial will be increased. Similarly, if the maximum coefficient is set to
    /// zero, the degree of the polynomial will be decreased.
    /// </summary>
    public double this[int index]
    {
      get { return index > Degree ? 0 : coefficients[index]; }
      set
      {
        if(index <= Degree) // if it references one of the existing coefficients...
        {
          coefficients[index] = value; // set it (this should catch coefficient < 0 errors)
          if(index == Degree && value == 0) // if it was the last coefficient and it was set to zero, then the degree must be reduced
          {
            do index--; while(index > 0 && coefficients[index] == 0);
            Degree = index;
          }
        }
        else if(value != 0) // otherwise, if it's not one of the existing coefficients, and it's not zero, the degree must be increased
        {
          if(index+1 > coefficients.Length) coefficients = Utility.EnlargeArray(coefficients, Length, index-Degree);
          coefficients[index] = value;
          Degree = index;
        }
      }
    }

    /// <summary>Gets the degree of the polynomial, which is the largest power of the independent variable included in the function. For
    /// instance, the polynomial <c>2x^3 + 5x - 7</c> has a degree of 3. A polynomial with only a constant term has a degree of zero.
    /// </summary>
    public int Degree { get; private set; }

    /// <inheritdoc/>
    public int DerivativeCount
    {
      // calculating derivatives may involve computing a factorial, and the largest factorial that fits in a double is 170!. that said,
      // we don't compute the full factorial in IMultiplyDifferentiableFunction.EvaluateDerivative(double,int), which is what this property
      // is intended to be used with. but we do compute the full factorial in EvaluateDerivatives(), and 0.170k (derivatives) is enough for
      // anyone
      get { return 170; }
    }

    /// <summary>Adds a constant value to the polynomial.</summary>
    public void Add(double value)
    {
      coefficients[0] += value;
    }

    /// <summary>Adds another polynomial to the polynomial.</summary>
    public void Add(Polynomial value)
    {
      if(value == null) throw new ArgumentNullException();
      if(value.Degree > Degree) coefficients = Utility.EnlargeArray(coefficients, Length, value.Degree-Degree);
      for(int i=0; i <= value.Degree; i++) coefficients[i] += value.coefficients[i];
      FixDegree(Math.Max(Degree, value.Degree));
    }

    /// <summary>Returns a copy of the polynomial.</summary>
    public Polynomial Clone()
    {
      return new Polynomial(coefficients, Length, Degree);
    }

    /// <summary>Copies the coefficients of the polynomial into the array at the given index. The coefficients will be written in the same
    /// format as that accepted by the <see cref="Polynomial(double[])"/> constructor. That is, the first value will be the constant term,
    /// the second value will be the linear term, etc.
    /// </summary>
    public void CopyTo(double[] array, int index)
    {
      Utility.ValidateRange(array, index, Length);
      ArrayUtility.SmallCopy(coefficients, 0, array, index, Length);
    }

    /// <summary>Divides the polynomial by a constant factor.</summary>
    public void Divide(double factor)
    {
      Multiply(1 / factor);
    }

    /// <summary>Divides the polynomial by another polynomial and discards the remainder.</summary>
    public unsafe void Divide(Polynomial value)
    {
      if(value == null) throw new ArgumentNullException();

      if(value.Degree == 0) // if it's actually division by a constant, use the method specialized for that case
      {
        Divide(value.coefficients[0]);
      }
      else
      {
        double mainFactor = 1 / value.coefficients[value.Degree]; // this should catch division by zero

        double* remain = stackalloc double[Length];
        fixed(double* data=coefficients) Unsafe.Copy(data, remain, Length*sizeof(double));

        int i = Degree;
        for(int end=Degree-value.Degree; i > end; i--) coefficients[i] = 0;
        for(; i >= 0; i--)
        {
          double quot = remain[value.Degree+i] * mainFactor;
          coefficients[i] = quot;
          if(i == 0) break;
          for(int j=value.Degree+i-1; j >= i; j--) remain[j] -= quot * value.coefficients[j-i];
        }

        Degree -= value.Degree;
      }
    }

    /// <summary>Divides the polynomial by the given polynomial and stores the remainder in <paramref name="remainder"/>.</summary>
    public void Divide(Polynomial value, out Polynomial remainder)
    {
      if(value == null) throw new ArgumentNullException();

      double mainFactor = 1 / value.coefficients[value.Degree]; // this should catch division by zero

      double[] remain = new double[Length];
      ArrayUtility.SmallCopy(coefficients, remain, Length);

      int i = Degree;
      for(int end=Degree-value.Degree; i > end; i--) coefficients[i] = 0;
      for(; i >= 0; i--)
      {
        double quot = remain[value.Degree+i] * mainFactor;
        coefficients[i] = quot;
        for(int j=value.Degree+i-1; j >= i; j--) remain[j] -= quot * value.coefficients[j-i];
      }
      for(i=value.Degree; i <= Degree; i++) remain[i] = 0;

      remainder = new Polynomial(remain, false);
      Degree -= value.Degree;
    }

    /// <summary>Determines whether the given object is a polynomial equal to this one.</summary>
    public override bool Equals(object obj)
    {
      return Equals(this, obj as Polynomial);
    }

    /// <summary>Determines whether the given polynomial is equal to this one.</summary>
    public bool Equals(Polynomial other)
    {
      return Equals(this, other);
    }

    /// <summary>Determines whether the given polynomial is equal to this one, to within the given tolerance, meaning that the difference
    /// between the coefficients can be no greater than the tolerance.
    /// </summary>
    public bool Equals(Polynomial other, double tolerance)
    {
      return Equals(this, other, tolerance);
    }

    /// <summary>Evaluates the polynomial using the given value of the independent variable.</summary>
    /// <remarks>If you want to evaluate the derivative as well, consider using a method that computes both at the same time, such as
    /// <see cref="Evaluate(double,out double)"/>.
    /// </remarks>
    public unsafe double Evaluate(double x)
    {
      // we'll evaluate the polynomial recursively as follows. given coefficients c0, c1, c2, c3, c4, and c5, we first add every pair:
      // c0 + c1*x, c2 + c3*x, c4 + c5*x
      // then we add those pairs (any missing coefficients are considered to be zero):
      // (c0 + c1*x) + (c2 + c3*x)*x^2, (c4 + c5*x) + 0*x^2
      // then we add those pairs:
      // ((c0 + c1*x) + (c2 + c3*x)*x^2) + ((c4 + c5*x) + 0*x^2)*x^4
      // until we have a single result. this method has better roundoff properties than adding sequentially as we do in other methods
      // instead of using explicit recursion, we'll use an array allocated on the stack
      int length = Degree/2 + 1;
      double* data = stackalloc double[length];

      // do the first round of additions and fill the array with data
      for(int i=0; i <= Degree; i += 2)
      {
        double value = coefficients[i];
        if(i < Degree) value += coefficients[i+1]*x;
        data[i/2] = value;
      }

      while(length > 1)
      {
        x *= x;
        for(int i=0; i < length; i += 2)
        {
          double value = data[i];
          if(i+1 != length) value += data[i+1]*x;
          data[i/2] = value;
        }
        length = (length+1) / 2;
      }

      return *data;
    }

    /// <summary>Evaluates the polynomial using the given value of the independent variable, and stores the derivative in
    /// <paramref name="derivative"/>.
    /// </summary>
    public double Evaluate(double x, out double derivative)
    {
      // given coefficients c0, c1, c2, and c3, the value can be expressed as c0 + x*(c1 + x*(c2 + x*c3)). this can be computed iteratively
      // with the recurrence v_n = c_n + x*v_(n+1). similarly, the derivative can be expressed as:
      // x*(x*(x*0 + c3) + (c2 + x*c3)) + (c1 + x*(c2 + x*c3)) =
      // x*(x*c3 + c2 + x*c3) + (c1 + x*c2 + x^2*c3) =
      // 3*x^2*c3 + 2*x*c2 + c1
      // this can be computed iteratively using the recurrance d_n = v_(n+1) + x*d_(n+1)
      int index = Degree;
      double value = coefficients[index], deriv = 0;
      while(index > 0)
      {
        index--;
        deriv = deriv*x + value;
        value = value*x + coefficients[index];
      }
      derivative = deriv;
      return value;
    }

    /// <summary>Returns the value of the polynomial's derivative using the given value of the independent variable.</summary>
    /// <remarks>If you want to get the polynomial's value as well, consider using a method that computes both at the same time, such as
    /// <see cref="Evaluate(double,out double)"/>.
    /// </remarks>
    public double EvaluateDerivative(double x)
    {
      return EvaluateDerivative(x, 1);
    }

    /// <summary>Returs the value of the polynomial's nth derivative using the given value of the independent variable.</summary>
    /// <param name="x">The value of the independent variable.</param>
    /// <param name="derivative">The derivative to compute. Specify 1 for the first derivative, 2 for the second derivative, etc.</param>
    /// <remarks>If you want to compute multiple derivatives, at a time, consider using <see cref="EvaluateDerivatives"/>.</remarks>
    public double EvaluateDerivative(double x, int derivative)
    {
      if(derivative < 1 || derivative > 170) throw new ArgumentOutOfRangeException(); // see DerivativeCount for an explanation

      double deriv;
      if(derivative > Degree)
      {
        deriv = 0;
      }
      else
      {
        // For a polynomial with coefficients c0, c1, c2, c3, and c4, the first four derivatives are:
        // 1: c1*1 + c2*2*x + c3*3*x^2 + c4*4*x^3
        // 2: c2*2*1 + c3*3*2*x + c4*4*3*x^2
        // 3: c3*3*2*1 + c4*4*3*2*x
        // 4: c4*4*3*2*1
        //
        // You can see that the nth derivative is the c_n*n! + (c_(n+1) * (n+1)!/1! * x^1) + (c_(n+2) * (n+2)!/2! * x^2) +
        // (c_(n+3) * (n+3)!/3! * x^3) + ...
        // For the 1st derivative, this simplifies to c1 + c2*2*x^1 + c3*3*x^2 + c4*4*x^3, so we'll use that as a special case.
        // For the other cases, we'll maintain a factor initially set to n!. Then for each iteration i (counting from 1), we'll remove a
        // factor of i and add a factor of n+i, which should maintain it equal to (n+1)! / 1!

        double xFactor = x;
        if(derivative == 1)
        {
          deriv = coefficients[1];
          for(int i=2; i <= Degree; xFactor *= x, i++) deriv += coefficients[i] * xFactor * i;
        }
        else
        {
          double constFactor = 1;
          for(int i=2; i <= derivative; i++) constFactor *= i;

          deriv = coefficients[derivative] * constFactor;
          derivative++;
          for(int i=1; derivative <= Degree; i++, xFactor *= x, derivative++)
          {
            constFactor = constFactor / i * derivative; // divide first to keep the factor smaller and avoid some precision loss
            deriv += coefficients[derivative] * xFactor * constFactor;
          }
        }
      }
      return deriv;
    }

    /// <summary>Returns an array containing all derivatives of the polynomial using the given value of the independent variable, except
    /// those beyond N, where N is the polynomial's degree, because those are always zero.
    /// </summary>
    public double[] EvaluateDerivatives(double x)
    {
      double[] derivatives = new double[Degree];
      EvaluateDerivatives(x, derivatives, 0, derivatives.Length);
      return derivatives;
    }

    /// <summary>Returns the value of the polynomial using the given value of the independent variable, and simultaneously computes several
    /// of the polynomial's derivatives.
    /// </summary>
    /// <param name="x">The value of the independent variable.</param>
    /// <param name="array">The array into which the derivatives will be stored.</param>
    /// <param name="index">The index into <paramref name="array"/> at which the derivatives should be written.</param>
    /// <param name="count">The number of derivatives to compute.</param>
    /// <returns>Returns the value of the polynomial.</returns>
    public double EvaluateDerivatives(double x, double[] array, int index, int count)
    {
      Utility.ValidateRange(array, index, count);
      Array.Clear(array, index, count);

      int i = Degree;
      double value = coefficients[i];
      for(count--,i--; i >= 0; i--)
      {
        for(int j=Math.Min(count, Degree-i)+index; j > index; j--) array[j] = array[j]*x + array[j-1];
        array[index] = array[index]*x + value;
        value = value*x + coefficients[i];
      }

      double factor = 1;
      for(i=2; i <= count; i++)
      {
        factor *= i;
        array[i+index-1] *= factor;
      }

      return value;
    }

    /// <summary>Returns a new polynomial representing this polynomial's derivative.</summary>
    public Polynomial GetDerivative()
    {
      return GetDerivative(1);
    }

    /// <summary>Returns a new polynomial representing this polynomial's nth derivative.</summary>
    public Polynomial GetDerivative(int derivative)
    {
      if(derivative < 1) throw new ArgumentOutOfRangeException();

      double[] deriv;
      if(derivative > Degree)
      {
        deriv = new double[1]; // the derivative is zero
      }
      else
      {
        // see EvaluateDerivative() for a description of how this works
        deriv = new double[Length - derivative];
        if(derivative == 1)
        {
          for(int i=1; i <= Degree; i++) deriv[i-1] = coefficients[i] * i;
        }
        else
        {
          double constFactor = 1;
          for(int i=2; i <= derivative; i++) constFactor *= i;

          for(int i=0; ; )
          {
            deriv[i] = coefficients[derivative] * constFactor;
            derivative++;
            if(derivative > Degree) break;
            i++;
            constFactor = constFactor / i * derivative;
          }
        }
      }

      return new Polynomial(deriv, false);
    }

    /// <summary>Computes a hash code for the polynomial.</summary>
    public unsafe override int GetHashCode()
    {
      int hash = 0;
      for(int i=0; i<coefficients.Length; i++)
      {
        double d = coefficients[i];
        if(d != 0) hash ^= *(int*)&d ^ ((int*)&d)[1] ^ i; // +0 and -0 compare equally, so they mustn't lead to different hash codes
      }
      return hash;
    }

    /// <summary>Multiplies the polynomial by a constant factor.</summary>
    public void Multiply(double factor)
    {
      for(int i=0; i<=Degree; i++) coefficients[i] *= factor;
      if(factor == 0) Degree = 0;
    }

    /// <summary>Multiplies the polynomial by another polynomial.</summary>
    public unsafe void Multiply(Polynomial value)
    {
      if(value == null) throw new ArgumentNullException();

      // (a0 + a1*x + a2*x^2) * (b0 + b1*x + b2^x^2 + b3*x^3) =
      //
      // a0 * (b0 + b1*x + b2^x^2 + b3*x^3) + a1*x * (b0 + b1*x + b2^x^2 + b3*x^3) + a2*x^2 * (b0 + b1*x + b2^x^2 + b3*x^3) =
      //
      // (a0*b0 + a0*b1*x + a0*b2*x^2 + a0*b3*x^3) + (a1*b0*x + a1*b1*x^2 + a1*b2*x^3 + a1*b3*x^4) +
      //   (a2*b0*x^2 + a2*b1*x^3 + a2*b2*x^4 + a2*b3*x^5) =
      //
      // a0*b0 + (a0*b1 + a1*b0)*x + (a0*b2 + a1*b1 * a2*b0)*x^2 + (a0*b3 + a1*b2 + a2*b1)*x^3 + (a1*b3 + a2*b2)*x^4 + a2*b3*x^5
      //
      // notice how in the result, the indices of the coefficients that are multiplied sum to the index of the result coefficient that they
      // contribute to. so for the 0th result coefficient, we have only a0*b0 (where 0+0 = 0). for the 1st result coefficient, we have
      // a0*b1 and a1*b0 (where 0+1 = 1+0 = 1). for the 2nd result coefficient, we have a0*b2, a1*b1, and a2*b0 (where 0+2 = 1+1 = 2+0 = 2)

      if(value.Degree == 0) // if it's actually a multiplication by a constant, we already have a method tuned for that
      {
        Multiply(value.coefficients[0]);
      }
      else
      {
        int newDegree = Degree + value.Degree;
        if(newDegree+1 > coefficients.Length) coefficients = Utility.EnlargeArray(coefficients, Length, value.Degree);

        // create a copy of the coefficients on the stack, and then clear the coefficients in preparation for writing the result
        double* copy = stackalloc double[Length];
        fixed(double* data=coefficients)
        {
          int length = Length*sizeof(double);
          Unsafe.Copy(data, copy, length);
          Unsafe.Clear(data, length);
        }

        for(int i=0; i <= Degree; i++)
        {
          double av = copy[i];
          for(int j=0; j <= value.Degree; j++) coefficients[i+j] += av * value.coefficients[j];
        }

        Degree = newDegree;
      }
    }

    /// <summary>Negates the polynomial.</summary>
    public void Negate()
    {
      for(int i=0; i<=Degree; i++) coefficients[i] = -coefficients[i];
    }

    /// <summary>Subtracts a constant value from the polynomial.</summary>
    public void Subtract(double value)
    {
      coefficients[0] -= value;
    }

    /// <summary>Subtracts another polynomial from the polynomial.</summary>
    public void Subtract(Polynomial value)
    {
      if(value == null) throw new ArgumentNullException();
      if(value.Degree > Degree) coefficients = Utility.EnlargeArray(coefficients, Length, value.Degree-Degree);
      for(int i=0; i <= value.Degree; i++) coefficients[i] -= value.coefficients[i];
      FixDegree(Math.Max(Degree, value.Degree));
    }

    /// <summary>Returns an array containing the coefficients of the polynomial. The coefficients will be returned in the same format as
    /// that accepted by the <see cref="Polynomial(double[])"/> constructor. That is, the first value will be the constant term, the second
    /// value will be the linear term, etc.
    /// </summary>
    public double[] ToArray()
    {
      double[] array = new double[Length];
      ArrayUtility.SmallCopy(coefficients, array, Length);
      return array;
    }

    /// <summary>Converts the polynomial to a human-readable string.</summary>
    public override string ToString()
    {
      return ToString(null);
    }

    /// <summary>Converts the polynomial to a human-readable string, using the given format for the coefficients.</summary>
    public string ToString(string format)
    {
      StringBuilder sb = new StringBuilder();
      for(int i=Degree; i >= 0; i--)
      {
        double coeff = coefficients[i];
        if(coeff != 0)
        {
          if(sb.Length != 0) sb.Append(coeff < 0 ? " - " : " + ");
          else if(coeff < 0) sb.Append('-');

          sb.Append(Math.Abs(coeff).ToString(format, System.Globalization.CultureInfo.InvariantCulture));
          if(i != 0)
          {
            sb.Append('x');
            if(i != 1) sb.Append('^').Append(i.ToInvariantString());
          }
        }
      }

      if(sb.Length == 0) sb.Append('0');
      return sb.ToString();
    }

    /// <summary>Removes excess storage used by the polynomial, so that it only stores a number of coefficients equal to its degree plus
    /// one.
    /// </summary>
    /// <remarks>Various operations may cause the internal storage of the polynomial to be greater than it's degree. For instance, if one
    /// high-degree polynomial is subtracted from another, resulting in a low-degree polynomial, the low-degree polynomial may still have
    /// an amount of storage allocated equal to its original, high degree. This is done for performance, to avoid reallocating the storage
    /// every time the polynomial's degree changes. However, if you want to eliminate any extra storage used by the polynomial, you can
    /// call this method.
    /// </remarks>
    public void TrimExcess()
    {
      if(coefficients.Length > Length) coefficients = ToArray();
    }

    /// <summary>Adds two polynomials and returns the result.</summary>
    public static Polynomial Add(Polynomial a, Polynomial b)
    {
      if(a == null || b == null) throw new ArgumentNullException();
      a = a.Clone(Math.Max(a.Degree, b.Degree));
      a.Add(b);
      return a;
    }

    /// <summary>Adds a constant value to a polynomial and returns the result.</summary>
    public static Polynomial Add(Polynomial a, double value)
    {
      if(a == null) throw new ArgumentNullException();
      a = a.Clone();
      a.Add(value);
      return a;
    }

    /// <summary>Divides a polynomial by a constant factor and returns the result.</summary>
    public static Polynomial Divide(Polynomial a, double factor)
    {
      return Multiply(a, 1/factor);
    }

    /// <summary>Divides one polynomial by another and returns the result, discarding the remainder.</summary>
    [CLSCompliant(false)] // this conflicts with Polynomial.Divide(Polynomial value, out Polynomial remainder)
    public static Polynomial Divide(Polynomial a, Polynomial b)
    {
      if(a == null) throw new ArgumentNullException();
      a = a.Clone();
      a.Divide(b);
      return a;
    }

    /// <summary>Divides one polynomial by another and returns the result, while storing remainder in <paramref name="remainder" />.</summary>
    public static Polynomial Divide(Polynomial a, Polynomial b, out Polynomial remainder)
    {
      if(a == null) throw new ArgumentNullException();
      a = a.Clone();
      a.Divide(b, out remainder);
      return a;
    }

    /// <summary>Determines whether two polynomials are equal. One or both polynomials may be null. Null polynomials are never equal to
    /// non-null polynomials.
    /// </summary>
    public static bool Equals(Polynomial a, Polynomial b)
    {
      if(a == null) return b == null;
      else if(b == null || a.Degree != b.Degree) return false;

      for(int i=0; i <= a.Degree; i++)
      {
        if(a.coefficients[i] != b.coefficients[i]) return false;
      }

      return true;
    }

    /// <summary>Determines whether two polynomials are equal to within the given tolerance, meaning that the difference between the
    /// coefficients can be no greater than the tolerance. One or both polynomials may be null. Null polynomials are never equal to
    /// non-null polynomials.
    /// </summary>
    public static bool Equals(Polynomial a, Polynomial b, double tolerance)
    {
      if(a == null) return b == null;
      else if(b == null || a.Degree != b.Degree) return false;

      for(int i=0; i <= a.Degree; i++)
      {
        if(Math.Abs(a.coefficients[i] - b.coefficients[i]) > tolerance) return false;
      }

      return true;
    }

    /// <summary>Multiplies a polynomial by a constant factor and returns the result.</summary>
    public static Polynomial Multiply(Polynomial a, double factor)
    {
      if(a == null) throw new ArgumentNullException();
      a = a.Clone();
      a.Multiply(factor);
      return a;
    }

    /// <summary>Multiplies one polynomial by another and returns the result.</summary>
    public static Polynomial Multiply(Polynomial a, Polynomial b)
    {
      if(a == null || b == null) throw new ArgumentNullException();
      a = a.Clone(a.Degree + b.Degree);
      a.Multiply(b);
      return a;
    }

    /// <summary>Returns the negation of a polynomial.</summary>
    public static Polynomial Negate(Polynomial a)
    {
      if(a == null) throw new ArgumentNullException();
      a = a.Clone();
      a.Negate();
      return a;
    }

    /// <summary>Subtracts a constant value from a polynomial and returns the result.</summary>
    public static Polynomial Subtract(Polynomial a, double b)
    {
      if(a == null) throw new ArgumentNullException();
      a = a.Clone();
      a.Subtract(b);
      return a;
    }

    /// <summary>Subtracts a polynomial from a constant value and returns the result.</summary>
    public static Polynomial Subtract(double a, Polynomial b)
    {
      Polynomial result = Negate(b);
      result.coefficients[0] += a;
      return result;
    }

    /// <summary>Subtracts one polynomial from another and returns the result.</summary>
    public static Polynomial Subtract(Polynomial a, Polynomial b)
    {
      if(a == null || b == null) throw new ArgumentNullException();
      a = a.Clone(Math.Max(a.Degree, b.Degree));
      a.Subtract(b);
      return a;
    }

    /// <summary>Returns the negation of a polynomial.</summary>
    public static Polynomial operator-(Polynomial a)
    {
      return Negate(a);
    }

    /// <summary>Adds two polynomials and returns the result.</summary>
    public static Polynomial operator+(Polynomial a, Polynomial b)
    {
      return Add(a, b);
    }

    /// <summary>Subtracts one polynomial from another and returns the result.</summary>
    public static Polynomial operator-(Polynomial a, Polynomial b)
    {
      return Subtract(a, b);
    }

    /// <summary>Multiplies one polynomial by another and returns the result.</summary>
    public static Polynomial operator*(Polynomial a, Polynomial b)
    {
      return Multiply(a, b);
    }

    /// <summary>Adds a constant value to a polynomial and returns the result.</summary>
    public static Polynomial operator+(Polynomial a, double b)
    {
      return Add(a, b);
    }

    /// <summary>Subtracts a constant value from a polynomial and returns the result.</summary>
    public static Polynomial operator-(Polynomial a, double b)
    {
      return Subtract(a, b);
    }

    /// <summary>Multiplies a polynomial by a constant factor and returns the result.</summary>
    public static Polynomial operator*(Polynomial a, double b)
    {
      return Multiply(a, b);
    }

    /// <summary>Divides a polynomial by a constant factor and returns the result.</summary>
    public static Polynomial operator/(Polynomial a, double b)
    {
      return Divide(a, b);
    }

    /// <summary>Adds a constant value to a polynomial and returns the result.</summary>
    public static Polynomial operator+(double a, Polynomial b)
    {
      return Add(b, a);
    }

    /// <summary>Subtracts a polynomial from a constant value and returns the result.</summary>
    public static Polynomial operator-(double a, Polynomial b)
    {
      return Subtract(a, b);
    }

    /// <summary>Multiplies a polynomial by a constant factor and returns the result.</summary>
    public static Polynomial operator*(double a, Polynomial b)
    {
      return Multiply(b, a);
    }

    /// <summary>Gets the number of coefficients stored in the array, which is one greater than the polynomial's degree.</summary>
    int Length
    {
      get { return Degree+1; }
    }

    /// <summary>Clones the polynomial, with the clone having capacity for the given degree.</summary>
    Polynomial Clone(int degreeCapacity)
    {
      return new Polynomial(coefficients, degreeCapacity+1, Degree);
    }

    void FixDegree()
    {
      FixDegree(Degree);
    }

    /// <summary>Fixes the degree of the polynomial if some of the higher coefficients may have changed.</summary>
    void FixDegree(int maxDegree)
    {
      while(maxDegree > 0 && coefficients[maxDegree] == 0) maxDegree--;
      Degree = maxDegree;
    }

    void Initialize(double[] coefficients, bool clone)
    {
      int degree = coefficients.Length; // find the degree of the polynomial
      while(degree > 0 && coefficients[--degree] == 0) { }

      // if the array is substantially larger than needed or we have to clone it, then reallocate it
      if(clone || coefficients.Length-degree > 5 && degree <= coefficients.Length/2)
      {
        double[] array = new double[degree+1];
        ArrayUtility.SmallCopy(coefficients, array, array.Length);
        coefficients = array;
      }

      this.coefficients = coefficients;
      Degree = degree;
    }

    #region ICloneable Members
    object ICloneable.Clone()
    {
      return Clone();
    }
    #endregion

    #region IFunctionallyDifferentiableFunction Members
    IOneDimensionalFunction IFunctionallyDifferentiableFunction.GetDerivative(int derivative)
    {
      return GetDerivative(derivative);
    }
    #endregion

    double[] coefficients;
  }
}