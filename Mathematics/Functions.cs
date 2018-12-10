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
using AdamMil.Utilities;

// TODO: add a method to evaluate both the value and (first?) derivative, and update methods to use it, to help reduce redundant
// calculation?

namespace AdamMil.Mathematics
{
  #region IOneDimensionalFunction
  /// <summary>Represents a one-dimensional function.</summary>
  public interface IOneDimensionalFunction
  {
    /// <summary>Returns the value of the function at the given point.</summary>
    double Evaluate(double x);
  }
  #endregion

  #region IMultidimensionalFunction
  /// <summary>Represents a multidimensional function, which takes a vector of input values and returns a scalar.</summary>
  public interface IMultidimensionalFunction
  {
    /// <summary>Gets the arity of the function, which is the number of arguments that it takes. This should be at least one.</summary>
    int Arity { get; }
    /// <summary>Returns the value of the function at the point specified by the given array of values.</summary>
    double Evaluate(params double[] x);
  }
  #endregion

  #region IVectorValuedFunction
  /// <summary>Represents a vector-valued function, which returns a vector of values.</summary>
  public interface IVectorValuedFunction
  {
    /// <summary>Gets the input arity of the function, which is the number of input arguments that it takes. This should be at least one.</summary>
    int InputArity { get; }
    /// <summary>Gets the output arity of the function, which is the length of the vector returned by the function.
    /// This should be at least one.
    /// </summary>
    int OutputArity { get; }
    /// <summary>Evaluates the function at the given input point. The function's value should be written into <paramref name="output"/>.</summary>
    void Evaluate(double[] input, double[] output);
  }
  #endregion

  #region IDifferentiableFunction
  /// <summary>Represents a one-dimensional function that can be continuously differentiated in a region of interest.</summary>
  public interface IDifferentiableFunction : IOneDimensionalFunction
  {
    /// <include file="documentation.xml" path="/Math/Functions/DifferentiableFunction/DerivativeCount/node()"/>
    int DerivativeCount { get; }

    /// <include file="documentation.xml" path="/Math/Functions/DifferentiableFunction/EvaluateDerivative/node()"/>
    double EvaluateDerivative(double x, int derivative);
  }
  #endregion

  #region IDifferentiableMDFunction
  /// <summary>Represents a differentiable multidimensional function.</summary>
  public interface IDifferentiableMDFunction : IMultidimensionalFunction
  {
    /// <include file="documentation.xml" path="/Math/Functions/DifferentiableFunction/EvaluateGradient/node()"/>
    void EvaluateGradient(double[] x, double[] gradient);
  }
  #endregion

  #region IDifferentiableVVFunction
  /// <summary>Represents a vector-valued function that can be differentiated to produce a Jacobian matrix.</summary>
  public interface IDifferentiableVVFunction : IVectorValuedFunction
  {
    /// <summary>Computes the Jacobian matrix. For a vector-valued function with input arity n and output arity m, the Jacobian is an
    /// m-by-n matrix where each row i corresponds to the gradient of the input vector with respect to the output component i. That is,
    /// an element at row i and column j is the partial derivative of the j'th input component with respect to the i'th output component.
    /// </summary>
    /// <param name="input">The point at which the Jacobian is to be evaluated.</param>
    /// <param name="matrix">The matrix in which the Jacobian should be stored. It can be assumed that the matrix is of the correct size.</param>
    void EvaluateJacobian(double[] input, Matrix matrix);
  }
  #endregion

  #region IFunctionallyDifferentiableFunction
  /// <summary>Represents a one-dimensional differentiable function that can provide its derivative as another function.</summary>
  public interface IFunctionallyDifferentiableFunction : IDifferentiableFunction
  {
    /// <summary>Returns a <see cref="IOneDimensionalFunction"/> representing the given derivative.</summary>
    IOneDimensionalFunction GetDerivative(int derivative);
  }
  #endregion

  #region IFunctionallyDifferentiableMDFunction
  /// <summary>Represents a differentiable multi-dimensional function that can provide its gradient (i.e. derivative) as another function.</summary>
  public interface IFunctionallyDifferentiableMDFunction : IDifferentiableMDFunction
  {
    /// <summary>Returns a <see cref="IVectorValuedFunction"/> representing the gradient. The vector-valued function should have
    /// input and output arity equal to <see cref="IMultidimensionalFunction.Arity"/>.
    /// </summary>
    IVectorValuedFunction GetGradient();
  }
  #endregion

  #region OneDimensionalFunction
  /// <summary>Implements an <see cref="IOneDimensionalFunction"/> that uses a delegate to provide the function.</summary>
  public class OneDimensionalFunction : IOneDimensionalFunction, IMultidimensionalFunction
  {
    /// <summary>Initializes a new <see cref="OneDimensionalFunction"/> using the given delegate for the function</summary>
    public OneDimensionalFunction(Func<double,double> function)
    {
      if(function == null) throw new ArgumentNullException();
      this.function = function;
    }

    /// <summary>Returns the value of the function at the given point.</summary>
    public double Evaluate(double x)
    {
      return function(x);
    }

    #region IMultidimensionalFunction Members
    int IMultidimensionalFunction.Arity
    {
      get { return 1; }
    }

    double IMultidimensionalFunction.Evaluate(params double[] x)
    {
      return function(x[0]);
    }
    #endregion

    readonly Func<double,double> function;
  }
  #endregion

  #region DifferentiableFunction
  /// <summary>Implements an <see cref="IDifferentiableFunction"/> that uses delegates to provide the function and derivative.</summary>
  public sealed class DifferentiableFunction : OneDimensionalFunction, IFunctionallyDifferentiableFunction
  {
    /// <summary>Initializes a new <see cref="DifferentiableFunction"/> given delegates for the function and its first derivative.</summary>
    public DifferentiableFunction(Func<double, double> function, Func<double, double> derivative) : base(function)
    {
      if(derivative == null) throw new ArgumentNullException();
      this.derivative = derivative;
    }

    /// <summary>Returns the value of the function's first derivative at the given point.</summary>
    public double EvaluateDerivative(double x)
    {
      return derivative(x);
    }

    int IDifferentiableFunction.DerivativeCount
    {
      get { return 1; }
    }

    double IDifferentiableFunction.EvaluateDerivative(double x, int derivative)
    {
      if(derivative != 1) throw new ArgumentOutOfRangeException();
      return this.derivative(x);
    }

    IOneDimensionalFunction IFunctionallyDifferentiableFunction.GetDerivative(int derivative)
    {
      if(derivative != 1) throw new ArgumentOutOfRangeException();
      if(derivativeFunction == null) derivativeFunction = new OneDimensionalFunction(this.derivative);
      return derivativeFunction;
    }

    readonly Func<double, double> derivative;
    OneDimensionalFunction derivativeFunction;
  }
  #endregion

  #region MultidimensionalFunction
  /// <summary>Implements an <see cref="IMultidimensionalFunction"/> that uses a delegate to provide the function.</summary>
  public class MultidimensionalFunction : IMultidimensionalFunction
  {
    /// <summary>Initializes a new <see cref="MultidimensionalFunction"/> using the given delegate for the function.</summary>
    public MultidimensionalFunction(Func<double[], double> function, int arity)
    {
      if(function == null) throw new ArgumentNullException();
      if(arity <= 0) throw new ArgumentOutOfRangeException();
      this.function = function;
      this.arity    = arity;
    }

    /// <summary>Initializes a new <see cref="MultidimensionalFunction"/> using an <see cref="IOneDimensionalFunction"/> for the function.</summary>
    public MultidimensionalFunction(IOneDimensionalFunction function)
    {
      if(function == null) throw new ArgumentNullException();
      this.function = x => function.Evaluate(x[0]);
      this.arity    = 1;
    }

    /// <summary>Initializes a new <see cref="MultidimensionalFunction"/> using the given delegate for the function.</summary>
    public MultidimensionalFunction(Func<double, double> function)
    {
      if(function == null) throw new ArgumentNullException();
      this.function = x => function(x[0]);
      this.arity    = 1;
    }

    /// <summary>Initializes a new <see cref="MultidimensionalFunction"/> using the given delegate for the function.</summary>
    public MultidimensionalFunction(Func<double, double, double> function)
    {
      if(function == null) throw new ArgumentNullException();
      this.function = x => function(x[0], x[1]);
      this.arity    = 2;
    }

    /// <summary>Initializes a new <see cref="MultidimensionalFunction"/> using the given delegate for the function.</summary>
    public MultidimensionalFunction(Func<double, double, double, double> function)
    {
      if(function == null) throw new ArgumentNullException();
      this.function = x => function(x[0], x[1], x[2]);
      this.arity    = 3;
    }

    /// <summary>Initializes a new <see cref="MultidimensionalFunction"/> using the given delegate for the function.</summary>
    public MultidimensionalFunction(Func<double, double, double, double, double> function)
    {
      if(function == null) throw new ArgumentNullException();
      this.function = x => function(x[0], x[1], x[2], x[3]);
      this.arity    = 4;
    }

    /// <inheritdoc/>
    public int Arity
    {
      get { return arity; }
    }

    /// <inheritdoc/>
    public double Evaluate(params double[] x)
    {
      return function(x);
    }

    readonly Func<double[], double> function;
    readonly int arity;
  }
  #endregion

  #region DifferentiableMDFunction
  /// <summary>Implements an <see cref="IDifferentiableMDFunction"/> that uses delegates to provide the function and its gradient.</summary>
  public sealed class DifferentiableMDFunction : MultidimensionalFunction, IDifferentiableMDFunction
  {
    /// <summary>Initializes a new <see cref="DifferentiableMDFunction"/> from a <see cref="IDifferentiableFunction"/>.</summary>
    public DifferentiableMDFunction(IDifferentiableFunction function) : base(function)
    {
      gradient = (x, output) => output[0] = function.EvaluateDerivative(x[0], 1);
    }

    /// <summary>Initializes a new <see cref="DifferentiableMDFunction"/> from a one-dimensional function and its derivative.</summary>
    public DifferentiableMDFunction(Func<double, double> function, Func<double, double> derivative) : base(function)
    {
      if(derivative == null) throw new ArgumentNullException();
      gradient = (x, output) => output[0] = derivative(x[0]);
    }

    /// <summary>Initializes a new <see cref="DifferentiableMDFunction"/> from a two-dimensional function and its gradient.</summary>
    public DifferentiableMDFunction(Func<double, double, double> function, Action<double, double, double[]> gradient) : base(function)
    {
      if(gradient == null) throw new ArgumentNullException();
      this.gradient = (x, output) => gradient(x[0], x[1], output);
    }

    /// <summary>Initializes a new <see cref="DifferentiableMDFunction"/> from a three-dimensional function and its gradient.</summary>
    public DifferentiableMDFunction(Func<double, double, double, double> function, Action<double, double, double, double[]> gradient)
      : base(function)
    {
      if(gradient == null) throw new ArgumentNullException();
      this.gradient = (x, output) => gradient(x[0], x[1], x[2], output);
    }

    /// <summary>Initializes a new <see cref="DifferentiableMDFunction"/> from a multidimensional function and its gradient.</summary>
    public DifferentiableMDFunction(Func<double[], double> function, Action<double[], double[]> gradient, int arity)
      : base(function, arity)
    {
      if(gradient == null) throw new ArgumentNullException();
      this.gradient = gradient;
    }

    /// <include file="documentation.xml" path="/Math/Functions/DifferentiableFunction/EvaluateGradient/node()"/>
    public void EvaluateGradient(double[] x, double[] gradient)
    {
      this.gradient(x, gradient);
    }

    readonly Action<double[], double[]> gradient;
  }
  #endregion

  #region ApproximatelyDifferentiableMDFunction
  /// <summary>Provides an <see cref="IDifferentiableMDFunction"/> that estimates the gradient of an
  /// <see cref="IMultidimensionalFunction"/> via forward-difference approximation. This is based on the fact that
  /// <c>(f(x+h) - f(x)) / h -> f'(x)</c> as <c>h -> 0</c>.
  /// </summary>
  /// <remarks>In general, it is faster and more accurate to compute the gradient directly rather than approximating it, but in
  /// cases where that is difficult to achieve, this approximation may be helpful.
  /// </remarks>
  public sealed class ApproximatelyDifferentiableMDFunction : IDifferentiableMDFunction
  {
    /// <summary>Initializes a new <see cref="ApproximatelyDifferentiableMDFunction"/> given an <see cref="IMultidimensionalFunction"/>
    /// whose gradient should be approximated using a forward-difference method.
    /// </summary>
    public ApproximatelyDifferentiableMDFunction(IMultidimensionalFunction function) : this(function, 1e-8) { }

    /// <summary>Initializes a new <see cref="ApproximatelyDifferentiableMDFunction"/> given an <see cref="IMultidimensionalFunction"/>
    /// whose gradient should be approximated using a forward-difference method, and the difference factor.
    /// </summary>
    /// <param name="function">The function whose gradient will be approximated.</param>
    /// <param name="factor">A very small positive quantity representing fractional amount by which each argument will be increased when
    /// computing the gradient. The default (used by <see cref="ApproximatelyDifferentiableVVFunction(IVectorValuedFunction)"/>) is
    /// 0.00000001 (i.e. 1e-8). Smaller values might give a better approximation, but are more susceptible to roundoff error.
    /// </param>
    public ApproximatelyDifferentiableMDFunction(IMultidimensionalFunction function, double factor)
    {
      if(function == null) throw new ArgumentNullException();
      if(factor <= 0) throw new ArgumentOutOfRangeException();
      this.function = function;
      this.input2   = new double[function.Arity];  // the array used to hold x+h
      this.factor   = factor;
    }

    /// <inheritdoc/>
    public int Arity
    {
      get { return function.Arity; }
    }

    /// <inheritdoc/>
    public double Evaluate(double[] input)
    {
      return function.Evaluate(input);
    }

    /// <inheritdoc/>
    public void EvaluateGradient(double[] input, double[] gradient)
    {
      if(input == null || gradient == null) throw new ArgumentNullException();

      double value = function.Evaluate(input);
      ArrayUtility.FastCopy(input, input2, input.Length);
      for(int x=0; x<input2.Length; x++)
      {
        double arg = input[x], h = arg*factor;
        if(h == 0) h = factor;
        input2[x] = arg + h; // this trick ensures that the value of h added to the input value is exactly the same as the value of h used
        h = input2[x] - arg; // in the divisor later. essentially, it eliminates a source of error caused by the difference of precision
        double value2 = function.Evaluate(input2);
        input2[x] = arg;
        gradient[x] = (value2 - value) / h;
      }
    }

    readonly IMultidimensionalFunction function;
    readonly double[] input2;
    readonly double factor;
  }
  #endregion

  #region ApproximatelyDifferentiableVVFunction
  /// <summary>Provides an <see cref="IDifferentiableVVFunction"/> that estimates the Jacobian matrix of an
  /// <see cref="IVectorValuedFunction"/> via forward-difference approximation. This is based on the fact that
  /// <c>(f(x+h) - f(x)) / h -> f'(x)</c> as <c>h -> 0</c>.
  /// </summary>
  /// <remarks>In general, it is faster and more accurate to compute the Jacobian matrix directly rather than approximating it, but in
  /// cases where that is difficult to achieve, this approximation may be helpful.
  /// </remarks>
  public sealed class ApproximatelyDifferentiableVVFunction : IDifferentiableVVFunction
  {
    /// <summary>Initializes a new <see cref="ApproximatelyDifferentiableVVFunction"/> given an <see cref="IVectorValuedFunction"/> whose
    /// Jacobian matrix should be approximated using a forward-difference method.
    /// </summary>
    public ApproximatelyDifferentiableVVFunction(IVectorValuedFunction function) : this(function, 1e-8) { }

    /// <summary>Initializes a new <see cref="ApproximatelyDifferentiableVVFunction"/> given an <see cref="IVectorValuedFunction"/> whose
    /// Jacobian matrix should be approximated using a forward-difference method, and the difference factor.
    /// </summary>
    /// <param name="function">The function whose Jacobian matrix will be approximated.</param>
    /// <param name="factor">A very small positive quantity representing fractional amount by which each argument will be increased when
    /// computing the Jacobian matrix. The default (used by <see cref="ApproximatelyDifferentiableVVFunction(IVectorValuedFunction)"/>) is
    /// 0.00000001 (i.e. 1e-8). Smaller values might give a better approximation, but are more susceptible to roundoff error.
    /// </param>
    public ApproximatelyDifferentiableVVFunction(IVectorValuedFunction function, double factor)
    {
      if(function == null) throw new ArgumentNullException();
      if(factor <= 0) throw new ArgumentOutOfRangeException();
      this.function  = function;
      this.input2    = new double[function.InputArity];  // the array used to hold x+h
      this.output    = new double[function.OutputArity]; // the array used to hold f(x)
      this.output2   = new double[function.OutputArity]; // the array used to hold f(x+h)
      this.factor    = factor;
    }

    /// <inheritdoc/>
    public int InputArity
    {
      get { return function.InputArity; }
    }

    /// <inheritdoc/>
    public int OutputArity
    {
      get { return function.OutputArity; }
    }

    /// <inheritdoc/>
    public void Evaluate(double[] input, double[] output)
    {
      function.Evaluate(input, output);
    }

    /// <inheritdoc/>
    public void EvaluateJacobian(double[] input, Matrix matrix)
    {
      if(input == null || matrix == null) throw new ArgumentNullException();

      function.Evaluate(input, output);
      ArrayUtility.FastCopy(input, input2, input.Length);
      for(int x=0; x<input2.Length; x++)
      {
        double arg = input[x], h = arg*factor;
        if(h == 0) h = factor;
        input2[x] = arg + h; // this trick ensures that the value of h added to the input value is exactly the same as the value of h used
        h = input2[x] - arg; // in the divisor later. essentially, it eliminates a source of error caused by the difference of precision
        function.Evaluate(input2, output2);
        input2[x] = arg;
        for(int y=0; y<output.Length; y++) matrix[y, x] = (output2[y] - output[y]) / h;
      }
    }

    readonly IVectorValuedFunction function;
    readonly double[] input2, output, output2;
    readonly double factor;
  }
  #endregion
}
