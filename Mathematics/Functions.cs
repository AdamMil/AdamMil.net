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

  #region IDifferentiableFunction
  /// <summary>Represents a one-dimensional function that can be continuously differentiated in a region of interest.</summary>
  public interface IDifferentiableFunction : IOneDimensionalFunction
  {
    /// <summary>Gets the number of derivatives that are supported by the function. This must be at least one. This is not necessarily
    /// equal to the number of distinct derivatives. Many functions may support a practically unlimited number of derivatives, but with
    /// almost all of them equal to zero. The purpose of this property is to allow a method to check that a function supports a given
    /// number of derivatives, not to allow all of the derivatives to be enumerated.
    /// </summary>
    int DerivativeCount { get; }

    /// <summary>Returns the value of the function's first derivative at the given point.</summary>
    double EvaluateDerivative(double x);
    
    /// <summary>Returns the nth derivative of the function at a the given point.</summary>
    /// <param name="x">The point at which the derivative is to be evaluated.</param>
    /// <param name="derivative">The derivative to evaluate. The first derivative is specify by passing 1, the second derivative by
    /// passing 2, etc.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="derivative"/> is [ess than 1 or greater than
    /// <see cref="DerivativeCount"/>.
    /// </exception>
    double EvaluateDerivative(double x, int derivative);
  }
  #endregion

  #region IFunctionallyDifferentiableFunction
  /// <summary>Represents a one-dimensional differentiable function that can return a derivative as another function.</summary>
  public interface IFunctionallyDifferentiableFunction : IDifferentiableFunction
  {
    /// <summary>Returns a function representing the </summary>
    IOneDimensionalFunction GetDerivative(int derivative);
  }
  #endregion

  #region OneDimensionalFunction
  /// <summary>Implements an <see cref="IOneDimensionalFunction"/> that uses a delegate to provide the function.</summary>
  public class OneDimensionalFunction : IOneDimensionalFunction
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

    readonly Func<double,double> function;
  }
  #endregion

  #region DifferentiableFunction
  /// <summary>Implements an <see cref="IDifferentiableFunction"/> that uses delegates to provide the function and derivative.</summary>
  public sealed class DifferentiableFunction : OneDimensionalFunction, IFunctionallyDifferentiableFunction
  {
    /// <summary>Initializes a new <see cref="DifferentiableFunction"/> given delegates for the function and its derivative.</summary>
    public DifferentiableFunction(Func<double, double> function, Func<double, double> derivative) : base(function)
    {
      if(derivative == null) throw new ArgumentNullException();
      this.derivative = derivative;
    }

    /// <summary>Returns the value of the function's derivative at the given point.</summary>
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

}
