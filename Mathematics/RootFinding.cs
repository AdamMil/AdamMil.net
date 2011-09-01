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
using System.Collections.Generic;
using System.Runtime.Serialization;
using AdamMil.Mathematics.LinearEquations;
using AdamMil.Utilities;

// TODO: add polynomial-specific root finders

namespace AdamMil.Mathematics.RootFinding
{

// TODO: replace with a more general Interval structure someday?
#region RootInterval
/// <summary>Represents an interval in which a root of a one-dimensional function is assumed to exist.</summary>
public struct RootInterval
{
  /// <summary>Initializes a new <see cref="RootInterval"/> with the given lower and upper bounds for the function argument.</summary>
  public RootInterval(double min, double max)
  {
    if(min > max) throw new ArgumentException("The minimum must be less than or equal to the maximum.");
    Min = min;
    Max = max;
  }

  /// <summary>Converts the interval into a readable string.</summary>
  public override string ToString()
  {
    return Min.ToString() + ", " + Max.ToString();
  }

  /// <summary>Gets or sets the lower bound for the function argument.</summary>
  public double Min;
  /// <summary>Gets or sets the upper bound for the function argument.</summary>
  public double Max;
}
#endregion

#region RootNotFoundException
/// <summary>An exception thrown when a root of a function could not be found.</summary>
[Serializable]
public class RootNotFoundException : Exception
{
  /// <inheritdoc/>
  public RootNotFoundException(string message) : base(message) { }
  /// <inheritdoc/>
  public RootNotFoundException(string message, Exception innerException) : base(message, innerException) { }
  /// <inheritdoc/>
  public RootNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
#endregion

#region FindRoot
/// <summary>Provides methods for finding roots of one-dimensional functions.</summary>
public static class FindRoot
{
  /// <include file="documentation.xml" path="/Math/RootFinding/FindRoot1/*[@name != 'accuracy']"/>
  /// <include file="documentation.xml" path="/Math/RootFinding/BoundedNewtonRaphson/*"/>
  /// <returns>Returns a root of the function, to within a default accuracy. See the remarks for more details.</returns>
  public static double BoundedNewtonRaphson(IDifferentiableFunction function, RootInterval interval)
  {
    return BoundedNewtonRaphson(function, interval, GetDefaultAccuracy(interval));
  }

  /// <include file="documentation.xml" path="/Math/RootFinding/FindRoot1/*"/>
  /// <include file="documentation.xml" path="/Math/RootFinding/BoundedNewtonRaphson/*"/>
  /// <returns>Returns a root of the function, to within the specified accuracy. See the remarks for more details.</returns>
  public static double BoundedNewtonRaphson(IDifferentiableFunction function, RootInterval interval, double accuracy)
  {
    ValidateArguments(function, interval, accuracy);

    const int MaxIterations = 100;

    double vMin = function.Evaluate(interval.Min), vMax = function.Evaluate(interval.Max);
    if(vMin == 0) return interval.Min;
    else if(vMax == 0) return interval.Max;
    else if(vMin*vMax > 0) throw RootNotBracketedError();

    if(vMin > 0) Utility.Swap(ref interval.Min, ref interval.Max); // make the search go from low (negative) to high (positive)

    // see UnboundedNewtonRaphson() for an implementation of the basic Newton's method. in addition to the basic Newton's method, this
    // implementation adds bounds checking to prevent Newton's method from diverging wildly if it encounters a near-zero derivative, and
    // adds a convergence check to ensure that it isn't getting stuck in rare cases or slowing down too much near a root with a derivative
    // of zero. when that happens, it switches to the subdivision method

    double guess = (interval.Max + interval.Min) * 0.5, step = Math.Abs(interval.Max - interval.Min), prevStep = step;
    double value = function.Evaluate(guess), deriv = function.EvaluateDerivative(guess, 1);
    for(int i=0; i<MaxIterations; i++)
    {
      // in order to see decide whether to do Newton's method or simple subdivision in this iteration, we'll see whether the Newton step
      // 1) would keep the estimate in bounds, and 2) would probably decrease the magnitude of the function's value more than subdivision
      //
      // in order for the Newton step to keep the estimate within bounds (after doing x = x - f(x)/f'(x)), we would need to have:
      // min <= x - f(x)/f'(x) <= max
      // min-x <= -f(x)/f'(x) <= max-x
      // x-min >= f(x)/f'(x) >= x-max
      // (x-min)*f'(x) >= f(x) >= (x-max)*f'(x)
      // (x-min)*f'(x) - f(x) >= 0 >= (x-max)*f'(x) - f(x)
      // which is to say that (x-min)*f'(x) - f(x) and (x-max)*f'(x) - f(x) must not have the same sign. we can check that by multiplying
      // them together and seeing if the result is positive.
      bool giveUp;
      if((((guess-interval.Max)*deriv - value) * ((guess-interval.Min)*deriv - value)) > 0 || // if the Newton step would go out of bounds
         Math.Abs(2*value) > Math.Abs(prevStep*deriv)) // or the function value isn't decreasing fast enough (not sure why this works)...
      {
        prevStep = step; // then use bisection
        step     = 0.5 * (interval.Max - interval.Min); // just step to the middle of the current interval
        guess    = interval.Min + step;
        giveUp = guess == interval.Min; // if the step was so small as to make no difference in value, then we're as close as we can get
      }
      else // otherwise, the newton step would likely help, so use it instead
      {
        prevStep = step;
        step     = value / deriv; // compute the update amount f(x) / f'(x)
        double temp = guess;
        guess -= step;
        giveUp = guess == temp; // if the step was so small as to make no difference in the value, then we're as close as we can get
      }

      if(Math.Abs(step) <= accuracy) return guess; // if we're within the desired accuracy, then we're done
      else if(giveUp) break; // otherwise, if we can't go any further, give up

      value = function.Evaluate(guess);
      deriv = function.EvaluateDerivative(guess, 1);

      // shrink the interval around the current best guess
      if(value < 0) interval.Min = guess;
      else interval.Max = guess;
    }

    throw RootNotFoundError();
  }

  /// <include file="documentation.xml" path="/Math/RootFinding/BracketInward/*"/>
  public static List<RootInterval> BracketInward(IOneDimensionalFunction function, RootInterval interval, int segments)
  {
    if(function == null) throw new ArgumentNullException();
    return BracketInward(function.Evaluate, interval, segments);
  }

  /// <include file="documentation.xml" path="/Math/RootFinding/BracketInward/*"/>
  public static List<RootInterval> BracketInward(Func<double, double> function, RootInterval interval, int segments)
  {
    ValidateArguments(function, interval);
    if(segments <= 0) throw new ArgumentOutOfRangeException();

    List<RootInterval> intervals = new List<RootInterval>();
    double segmentSize = (interval.Max - interval.Min) / segments, max = interval.Min, vMin = function(interval.Min);
    for(int i=0; i<segments; i++)
    {
      max = i == segments-1 ? interval.Max : max + segmentSize; // make sure the end of the last segment exactly matches the the interval
      double vMax = function(max);
      if(vMin*vMax <= 0) intervals.Add(new RootInterval(interval.Min, max)); // if a zero crossing (or zero touching) was found, add it
      vMin = vMax;
      interval.Min = max;
    }
    return intervals;
  }

  /// <include file="documentation.xml" path="/Math/RootFinding/BracketOutward/*"/>
  public static bool BracketOutward(IOneDimensionalFunction function, ref RootInterval initialGuess)
  {
    if(function == null) throw new ArgumentNullException();
    return BracketOutward(function.Evaluate, ref initialGuess);
  }

  /// <include file="documentation.xml" path="/Math/RootFinding/BracketOutward/*"/>
  public static bool BracketOutward(Func<double, double> function, ref RootInterval initialGuess)
  {
    ValidateArguments(function, initialGuess);

    const int MaxTries = 50;
    const double Expansion = 1.6;

    double vMin = function(initialGuess.Min), vMax = function(initialGuess.Max);
    for(int i=0; i<MaxTries; i++)
    {
      if(vMin*vMax < 0) return true; // if the two values have opposite signs, assume there's a zero crossing
      if(Math.Abs(vMin) < Math.Abs(vMax)) // otherwise, if the minimum side of the interval is closer to zero, expand in that direction
      {
        initialGuess.Min += (initialGuess.Min - initialGuess.Max) * Expansion;
        vMin = function(initialGuess.Min);
      }
      else // otherwise, the maximum side is closer (or they're the same distance), so expand in that direction
      {
        initialGuess.Max += (initialGuess.Max - initialGuess.Min) * Expansion;
        vMax = function(initialGuess.Max);
      }
    }

    // if one of the values was zero on the last iteration, then we would have expanded the iteration to contain a zero crossing or
    // zero touching, but would have fallen out of the loop before we could detect that and return true. so give it one last chance
    return vMin * vMin <= 0;
  }

  /// <include file="documentation.xml" path="/Math/RootFinding/FindRoot1/*[@name != 'accuracy']"/>
  /// <include file="documentation.xml" path="/Math/RootFinding/Brent/*"/>
  /// <returns>Returns a root of the function, to within a default level of accuracy. See the remarks for more details.</returns>
  public static double Brent(IOneDimensionalFunction function, RootInterval interval)
  {
    if(function == null) throw new ArgumentNullException();
    return Brent(function.Evaluate, interval);
  }

  /// <include file="documentation.xml" path="/Math/RootFinding/FindRoot1/*[@name != 'accuracy']"/>
  /// <include file="documentation.xml" path="/Math/RootFinding/Brent/*"/>
  /// <returns>Returns a root of the function, to within a default level of accuracy. See the remarks for more details.</returns>
  public static double Brent(Func<double, double> function, RootInterval interval)
  {
    return Brent(function, interval, GetDefaultAccuracy(interval));
  }

  /// <include file="documentation.xml" path="/Math/RootFinding/FindRoot1/*"/>
  /// <include file="documentation.xml" path="/Math/RootFinding/Brent/*"/>
  /// <returns>Returns a root of the function, to within the specified accuracy. See the remarks for more details.</returns>
  public static double Brent(IOneDimensionalFunction function, RootInterval interval, double accuracy)
  {
    return Brent(function.Evaluate, interval, accuracy);
  }

  /// <include file="documentation.xml" path="/Math/RootFinding/FindRoot1/*"/>
  /// <include file="documentation.xml" path="/Math/RootFinding/Brent/*"/>
  /// <returns>Returns a root of the function, to within the specified accuracy. See the remarks for more details.</returns>
  public static double Brent(Func<double, double> function, RootInterval interval, double accuracy)
  {
    ValidateArguments(function, interval, accuracy);

    const int MaxIterations = 100;
    double a = interval.Min, b = interval.Max, va = function(a), vb = function(b);

    if(va == 0) return interval.Min;
    else if(vb == 0) return interval.Max;
    else if(va*vb > 0) throw RootNotBracketedError();

    // the secant method for root finding takes two points (initially the edges of the interval) and uses a linear interpolation between
    // them (intersecting it with the x axis) to get the next estimate of where the root is. it converges quite quickly, but it is possible
    // that the secant method will diverge from the solution given a point where the secant line takes it way off course. the false
    // position method is similar except that it updates the points defining the line in such a way that they always remain bracketed (by
    // keeping track of an older point to allow sometimes updating only one of the endpoints). this is slower, but surer. ridder's method
    // evaluates the point in the midpoint of the line as well, and uses some magic to factor out an exponential factor to turn the three
    // points into a straight line, and applies the false position method to the modified points, producing a method that converges
    // quadratically while remaining bracketed. ridder's method generally works very well.
    //
    // however, all of those methods assume approximately linear behavior between the root estimates, and can get bogged down with
    // pathological functions, taking take many iterations to converge -- many more than the simple subdivision method, which is at least
    // guaranteed linear convergence. the van Wijngaarden-Dekker-Brent method (or Brent's method for short) works by using inverse
    // quadratic interpolation to fit an inverse quadratic function to the points, but if the result would take the next step out of
    // bounds, or if it wouldn't shrink the bounds quickly enough, then uses subdivision instead. in this way, it achieves generally
    // quadratic convergence while guaranteeing at least linear convergence. unlike the simpler methods, i can't claim to actually
    // understand all the math behind it, but here's the implementation

    accuracy *= 0.5; // we actually use half the accuracy in the math below, so do the division only once

    // a, b, and c are the three points defining the current estimate of the root's position. va, vb, and vc are the values of the function
    // corresponding those points. b corresponds to the current best estimate
    double c = b, vc = vb, range = 0, e = 0;
    for(int i=0; i<MaxIterations; i++)
    {
      if(vb*vc > 0) // if f(b) and f(c) have the same sign (which won't be zero because we've already handled that case)...
      {
        // then we have vb and vc on one side of zero and va on the other side. discard c and replace it with a copy of a. this gives
        // a and c on one side of the root, and b on the other
        c  = a;
        vc = va;
        e  = range = b-a;
      }

      if(Math.Abs(vc) < Math.Abs(vb)) // if f(c) is closer to zero than f(b)...
      {
        // make the one closer to zero b, which is supposed to be our best estimate so far, and discard a
        a=b; b=c; c=a;
        va=vb; vb=vc; vc=va;
      }

      // check to see how well we're converging on the root
      double tolerance = 2*IEEE754.DoublePrecision * Math.Abs(b) + accuracy, xm = 0.5 * (c-b);
      
      if(Math.Abs(xm) <= tolerance || vb == 0) return b; // if the current best estimate is close enough, return it

      if(Math.Abs(e) >= tolerance && Math.Abs(va) > Math.Abs(vb)) // if the bounds are increasing quickly enough...
      {
        // attempt inverse quadratic interpolation. the next root estimate basically equals b + P/Q where:
        // P = S * (T*(R-T)*(c-b) - (1-R)*(b-a)) and
        // Q = (T-1)*(R-1)*(S-1) given
        // R = f(b) / f(c)
        // S = f(b) / f(a)
        // T = f(a) / f(c)
        double p, q, s = vb / va;
        if(a == c) // if a = c (a common case), then T = 1 and (a-b) = (c-b) = 2*xm, and the expression simplifies...
        {
          // P = S * ((R-1)*2xm + (1-R)*2xm) = S * 2xm * ((R-1) + (1-R)) = S * 2xm = S * 2 * 0.5 * (c-b) = S * (c-b)
          p = s * (c-b);
          // this seems like it should simplify Q = (T-1)*(R-1)*(S-1) = Q = (1-1)*(R-1)*(S-1) = 0. but that would lead to division by zero
          // later when we divide P by Q, so Brent's method uses this instead. i'm not sure why.
          q = 1 - s;
        }
        else // otherwise, we use the equations above (with some changes that i don't exactly understand)
        {
          double r = vb / vc;
          q = va / vc; // use q to hold T
          p = s * (q*(q-r)*(c-b) - (r-1)*(b-a)); // this seems to be negated from the expected formula. i'm not sure why.
          q = (q-1) * (r-1) * (s-1);
        }

        if(p > 0) q = -q;
        else p = -p;

        if(2*p < Math.Min(3*xm*q - Math.Abs(tolerance*q), Math.Abs(e*q))) // if the interpolation puts us within bounds, then use it
        {
          e = range;
          range = p / q;
        }
        else // otherwise, use bisection
        {
          range = xm;
          e = range;
        }
      }
      else
      {
        range = xm;
        e = range;
      }

      a  = b;
      va = vb;
      b += Math.Abs(range) > tolerance ? range : WithSign(tolerance, xm);
      vb = function(b);
    }

    throw RootNotFoundError();
  }

  /// <summary>This method attempts to find the root of a vector-valued function, given an initial guess in <paramref name="x" />. That is,
  /// it attempts to find a value of the function that causes it to return a zero vector. The found root is stored back
  /// into <paramref name="x"/>. The method has a limitation that the vector-valued function must have the same input and output size.
  /// The method returns true if a root (or near root) was found, and false if it converged on a local minimum in the search space. If that
  /// happens, try again with a different initial guess.
  /// </summary>
  /// <remarks>If a root was found (i.e. the method returned true), you may be able to improve the root further by calling this method
  /// repeatedly until the root stops changing.
  /// </remarks>
  public static bool GlobalNewton(IDifferentiableVVFunction function, double[] x)
  {
    if(function == null || x == null) throw new ArgumentNullException();
    if(function.InputArity != function.OutputArity) throw new ArgumentException("The function must have the same input and output arity.");
    if(function.InputArity != x.Length) throw new ArgumentException("The point array does not match the function's input arity.");

    const int MaxIterations = 200;
    const double ScaledMaxStep = 100, InitialValueTolerance = 1e-15, ValueTolerance = 1e-8, ConvergeTolerance = 1e-12;

    double[] values = new double[function.OutputArity];
    function.Evaluate(x, values);

    // check to see if the initial value is sufficiently near a root. use a more strict test than ComponentTolerance to give the algorithm
    // a chance to improve the point if it starts out being quite close
    if(GetMaximumMagnitude(values) < InitialValueTolerance) return true;

    // calculate the maximum step that we'll allow the line search to take. it'll be some multiple of the original point's magnitude, but
    // not too small
    double maxStep = 0;
    for(int i=0; i<x.Length; i++) maxStep += x[i]*x[i];
    maxStep = Math.Max(Math.Sqrt(maxStep), x.Length) * ScaledMaxStep;

    SquaredFunction sqFunction = new SquaredFunction(function, values);
    Vector gradient = new Vector(function.InputArity), step = new Vector(function.InputArity);
    Matrix jacobian = new Matrix(function.InputArity, function.OutputArity), stepMatrix = step.ToColumnMatrix();
    LUDecomposition lud = new LUDecomposition(); // TODO: replace LU decomposition with SV decomposition to help with singular matrices
    double[] oldX = new double[x.Length];
    double sqValue = SquaredFunction.GetSquaredValue(values);
    for(int iteration=0; iteration < MaxIterations; iteration++)
    {
      function.EvaluateJacobian(x, jacobian);

      // compute the gradient for the line search
      for(int i=0; i<gradient.Size; i++)
      {
        double sum = 0;
        for(int j=0; j<values.Length; j++) sum += jacobian[j, i] * values[j];
        gradient[i] = sum;
      }

      ArrayUtility.SmallCopy(x, oldX, x.Length);

      lud.Initialize(jacobian);
      for(int i=0; i<step.Size; i++) step[i] = -values[i];
      stepMatrix.SetColumn(0, step);
      lud.Solve(stepMatrix, true).GetColumn(0, step.Array);
      bool converged = LineSearch(sqFunction, oldX, sqValue, gradient, step, x, out sqValue, maxStep);

      // check to see whether the function values have converged to nearly zero. the values array would have been updated by LineSearch()
      // via SquaredFunction.Evaluate(), which stores new values into the array
      if(GetMaximumMagnitude(values) < ValueTolerance) return true;

      if(converged) // if the line search reported convergence at a minimum value...
      {
        // see if the convergence is spurious (i.e. if the gradient is zero)
        double maxComponent = 0, divisor = Math.Max(sqValue, 0.5*x.Length);
        for(int i=0; i<x.Length; i++)
        {
          double component = Math.Abs(gradient[i]) * Math.Max(Math.Abs(x[i]), 1) / divisor;
          if(component > maxComponent) maxComponent = component;
        }
        return maxComponent >= ConvergeTolerance;
      }
      else // the line search didn't report convergence at a minimum value, so check to see whether the function parameters have converged
      {
        double maxArgument = 0;
        for(int i=0; i<x.Length; i++)
        {
          double argument = Math.Abs(x[i]-oldX[i]) / Math.Max(1, Math.Abs(x[i]));
          if(argument > maxArgument) maxArgument = argument;
        }
        if(maxArgument < IEEE754.DoublePrecision) return true;
      }
    }

    throw RootNotFoundError();
  }

  /// <include file="documentation.xml" path="/Math/RootFinding/FindRoot1/*[@name != 'accuracy']"/>
  /// <include file="documentation.xml" path="/Math/RootFinding/Subdivide/*"/>
  /// <returns>Returns a root of the function, to within a default level of accuracy. See the remarks for more details.</returns>
  public static double Subdivide(IOneDimensionalFunction function, RootInterval interval)
  {
    if(function == null) throw new ArgumentNullException();
    return Subdivide(function.Evaluate, interval);
  }

  /// <include file="documentation.xml" path="/Math/RootFinding/FindRoot1/*[@name != 'accuracy']"/>
  /// <include file="documentation.xml" path="/Math/RootFinding/Subdivide/*"/>
  /// <returns>Returns a root of the function, to within a default level of accuracy. See the remarks for more details.</returns>
  public static double Subdivide(Func<double, double> function, RootInterval interval)
  {
    return Subdivide(function, interval, GetDefaultAccuracy(interval));
  }

  /// <include file="documentation.xml" path="/Math/RootFinding/FindRoot1/*"/>
  /// <include file="documentation.xml" path="/Math/RootFinding/Subdivide/*"/>
  /// <returns>Returns a root of the function, to within the specified accuracy. See the remarks for more details.</returns>
  public static double Subdivide(IOneDimensionalFunction function, RootInterval interval, double accuracy)
  {
    if(function == null) throw new ArgumentNullException();
    return Subdivide(function.Evaluate, interval, accuracy);
  }

  /// <include file="documentation.xml" path="/Math/RootFinding/FindRoot1/*"/>
  /// <include file="documentation.xml" path="/Math/RootFinding/Subdivide/*"/>
  /// <returns>Returns a root of the function, to within the specified accuracy. See the remarks for more details.</returns>
  public static double Subdivide(Func<double, double> function, RootInterval interval, double accuracy)
  {
    ValidateArguments(function, interval, accuracy);

    const int MaxIterations = 100;

    double vMin = function(interval.Min), vMax = function(interval.Max);

    if(vMin == 0) return interval.Min;
    else if(vMax == 0) return interval.Max;
    else if(vMin*vMax > 0) throw RootNotBracketedError();

    // do a simple binary search of the interval
    double difference, start;
    if(vMin < 0)
    {
      difference = interval.Max - interval.Min;
      start      = interval.Min;
    }
    else
    {
      difference = interval.Min - interval.Max;
      start      = interval.Max;
    }

    for(int i=0; i<MaxIterations; i++)
    {
      difference *= 0.5;
      double mid = start + difference, value = function(mid);
      if(value <= 0) start = mid;
      if(Math.Abs(difference) <= accuracy || value == 0) return mid;
    }

    throw RootNotFoundError();
  }

  /// <include file="documentation.xml" path="/Math/RootFinding/FindRoot1/*[@name != 'accuracy']"/>
  /// <include file="documentation.xml" path="/Math/RootFinding/UnboundedNewtonRaphson/*"/>
  /// <returns>Returns a root of the function, to within a default accuracy. See the remarks for more details.</returns>
  public static double UnboundedNewtonRaphson(IDifferentiableFunction function, RootInterval interval)
  {
    return UnboundedNewtonRaphson(function, interval, GetDefaultAccuracy(interval));
  }

  /// <include file="documentation.xml" path="/Math/RootFinding/FindRoot1/*"/>
  /// <include file="documentation.xml" path="/Math/RootFinding/UnboundedNewtonRaphson/*"/>
  /// <returns>Returns a root of the function, to within the specified accuracy. See the remarks for more details.</returns>
  public static double UnboundedNewtonRaphson(IDifferentiableFunction function, RootInterval interval, double accuracy)
  {
    ValidateArguments(function, interval, accuracy);

    // Newton's method (also called Newton-Raphson after Joseph Raphson who independently invented the method some time after Newton, but
    // who published it before Newton) uses a function's derivative to estimate the location of the root. it works by repeatedly taking a
    // point and effectively intersecting the tangent line with the x axis and using that as the location for the next point to try. among
    // other shortcomings, this can cause it to go to infinity if the tangent line is horizontal (or nearly horizontal), and in some rare
    // cases it can enter a loop where the tangent from point A goes to point B, and the tangent from point B goes to point A. but in
    // general, it is an effective method.
    //
    // given a guess for the root location x, Newton's method takes x - f(x)/f'(x) as the next guess. this is based on the Taylor series
    // expansion of a function around a point: f(x+d) ~= f(x) + f'(x)*d + f''(x)*d^2/2 + f'''(x)*d^3/6 + ... for a small value of d. it is
    // assumed that the function is smooth and the higher order terms don't contribute much and can be safely ignored. (this is often, but
    // not always, true.) if we take d to be quite small and assume the function is well-behaved, then it simplifies into
    // f(x+d) ~= f(x) + f'(x)*d. if the function is approximately linear around that point, then f(x+d) = 0 would imply that
    // d = -f(x) / f'(x). (this is the step where the tangent line is intersected with the x axis by solving the linear equation.) thus,
    // the update step is x+d, or x - f(x)/f'(x).

    const int MaxIterations = 100;
    double guess = (interval.Max + interval.Min) * 0.5;
    for(int i=0; i<MaxIterations; i++)
    {
      double step = function.Evaluate(guess) / function.EvaluateDerivative(guess, 1);
      guess -= step;
      if((interval.Min-guess) * (guess-interval.Max) < 0) break; // if it went outside the interval, abort
      if(Math.Abs(step) <= accuracy) return guess; // if we're within the desired accuracy, then we're done
    }

    throw RootNotFoundError();
  }
  #region SquaredFunction
  sealed class SquaredFunction : IMultidimensionalFunction
  {
    public SquaredFunction(IVectorValuedFunction function, double[] output)
    {
      this.function = function;
      this.output   = output;
    }

    public int Arity
    {
      get { return function.InputArity; }
    }

    public double Evaluate(params double[] x)
    {
      function.Evaluate(x, output);
      return GetSquaredValue(output);
    }

    public static double GetSquaredValue(double[] x)
    {
      double sum = 0;
      for(int i=0; i<x.Length; i++)
      {
        double value = x[i];
        sum += value*value;
      }
      return 0.5 * sum;
    }

    readonly IVectorValuedFunction function;
    readonly double[] output;
  }
  #endregion

  static double GetMaximumMagnitude(double[] values)
  {
    double maxMagnitude = 0;
    for(int i=0; i<values.Length; i++)
    {
      double magnitude = Math.Abs(values[i]);
      if(magnitude > maxMagnitude) maxMagnitude = magnitude;
    }
    return maxMagnitude;
  }

  static internal bool LineSearch(IMultidimensionalFunction function, double[] x, double value, Vector gradient, Vector step,
                                  double[] newX, out double newValue, double maxStep=double.PositiveInfinity)
  {
    // when performing Newton's method against a function, taking the full Newton step may not decrease the function's value. the
    // step points in the direction of decrease, but since it's based on the derivative at a single point, it's only guaranteed to
    // initially decrease. as we go farther away from the point, the function can do who knows what. so if the Newton step would do
    // newX = x + step, a line search will find a factor F such newX = x + F*step represents is sure to represent a decrease in the
    // function's value, where 0 < F <= 1. in general, we want to set F = 1, representing the full Newton step, but if this doesn't
    // decrease the function value sufficiently, then we try lower values of F. the function value is guaranteed to decrease if F is small
    // enough, although there's no guarantee that it will decrease sufficiently.
    //
    // we will consider a decrease in value to be sufficient if it is a certain fraction of the ideal decrease implied by the full Newton
    // step, that is, if f(newX) <= f(x) + c*gradient*step, where c is the fraction that we want and gradient is the function's gradient
    // at x. we can get away with a small value for c. Numerical Recipes suggests 0.0001. this prevents f() from decreasing too slowly
    // relative to the step length. the routine also needs to avoid taking steps that are too small.
    //
    // the way new step values are chosen (if the full Newton step is not good enough) is as follows:
    // if we take the function g(F) = f(x + F*step), then g'(F) = gradient*step. we can then attempt to minimize g(F), which will have the
    // effect of choosing a value for F that makes the new f() value as small as possible. to minimize g(F) we model it using the available
    // information and then minimize the model. at the start, we have g(0) and g'(0) available as the old f() value and gradient*step,
    // respectively. after trying the Newton step, we also have g(1). using this information, we can model g(F) as a quadratic that
    // passes through the two known points:
    //     g(F) ~= (g(1) - g(0) - g'(0))*F^2 + g'(0)*F + g(0)
    // if F is 0, then clearly the quadratic equals the constant term, so the constant term must be g(0). given that, if F is 1, then the
    // non-constant terms must equal g(1) - g(0), so that when the constant term is added, the result is g(1). i'm not exactly sure why
    // g'(0) is used the way it is. there's probably an important reason for choosing it as the linear term. anyway, this is the
    // quadratic model used in NR.
    //
    // the quadratic is a parabola that has its minimum when its derivative is zero. so we take the derivative (treating the coefficients
    // as constants) and set it equal to zero. if g(F) ~= a*F^2 + b*F + d, then the derivative is 2a*F + b, and solving for F at 0 we get
    // F = -b/2a, or:
    //     F = -g'(0) / 2(g(1) - g(0) - g'(0))
    // according to Numerical Recipes, it can be shown that usually F must be less than or equal to 1/2 when a (the constant we chose
    // earlier) is small. so we enforce that F is <= 1/2. we also want to prevent the step from being too small, so we enforce that
    // F >= 0.1.
    //
    // if the new step is not good enough, then we have more information. on future attempts, we can model g(F) as a cubic, using the
    // previous two values of F (which we'll call F' and F''). then we have:
    //     g(F) ~= a*F^3 + b*F^2 + g'(0)*F + g(0)
    // to find the values of the coefficients a and b, we have to constrain the equation so it passes through the known values g(F') and
    // g(F''). substituting those in, we get two equations that we can solve for the two unknowns:
    //     a*F'^3  + b*F'^2  + g'(0)*F'  + g(0) = g(F')
    //     a*F''^3 + b*F''^2 + g'(0)*F'' + g(0) = g(F'')
    // once we have the coefficients a and b, we can find the minimum of the cubic. if we make certain assumptions like F > 0, and others
    // that i don't quite understand (e.g. assumptions about the values of a and b?), we can use the tactic of solving for where the
    // derivative is 0. since the derivative is quadratic, there would normally be two solutions, but based on the assumptions, we can
    // discard one of them, and we end up with F = (-b + sqrt(b^2 - 3a*g'(0))) / 3a. we then enforce that the new F is between 0.1 and 0.5
    // times the previous F and try again.

    const double Alpha = 0.0001;

    // first limit the step length to maxStep
    if(!double.IsPositiveInfinity(maxStep))
    {
      double length = step.GetMagnitude();
      if(length > maxStep) step *= maxStep/length;
    }

    // then calculate the slope as the dot product between the step and the gradient -- this is g'(0) in the description above
    double slope = Vector.DotProduct(gradient, step);
    if(slope >= 0) throw new ArgumentException("Invalid step vector, or too much roundoff error occurred.");

    // find the minimum allowable factor. if the factor drops below this value, then we give up
    double minFactor = 0;
    for(int i=0; i<x.Length; i++)
    {
      double tempFactor = Math.Abs(step[i]) / Math.Max(1, Math.Abs(x[i]));
      if(tempFactor > minFactor) minFactor = tempFactor;
    }
    minFactor = IEEE754.DoublePrecision / minFactor;

    // start with a factor of 1, representing the full Newton step
    double factor = 1, prevFactor = 0, prevValue = 0;
    while(true) // while we're looking for a suitable step size...
    {
      // try the current step size
      for(int i=0; i<newX.Length; i++) newX[i] = x[i] + step[i]*factor;
      newValue = function.Evaluate(newX);

      if(factor < minFactor) // if it's too small, then give up and return true, indicating that it has converged on a minimum
      {
        ArrayUtility.SmallCopy(x, newX, x.Length);
        return true;
      }
      else if(newValue <= value + Alpha*factor*slope) // otherwise, if the function value has decreased significantly (see above)...
      {                                               // this is basically checking if f(newX) <= f(x) + c*F*(gradient*step)
        return false; // return false, indicating that it decreased and hasn't, so far as we know, hit a minimum
      }
      else // otherwise, the decrease wasn't sufficient and we have to find a smaller factor
      {
        double nextFactor;
        if(factor == 1) // if this is the first time we've had to decrease F, use the quadratic model (F = -g'(0) / 2(g(1) - g(0) - g'(0)))
        {
          nextFactor = -slope / (2*(newValue-value-slope));
        }
        else // otherwise, it's the second or later time, so use the cubic model
        {
          // compute the coefficients of the cubic, a and b (discussed above)
          double rhs1 = newValue - value - factor*slope, rhs2 = prevValue - value - prevValue*slope;
          double a = (rhs1/(factor*factor) - rhs2/(prevFactor*prevFactor)) / (factor - prevFactor);
          double b = (-prevFactor*rhs1/(factor*factor) + factor*rhs2/(prevFactor*prevFactor)) / (factor - prevFactor);

          // the new F is found by solving for a point where the derivative is zero: F = (-b + sqrt(b^2 - 3a*g'(0))) / 3a
          if(a == 0) // if that formula would cause division by zero, we can simplify the cubic by removing the cubic term (since a is 0),
          {          // getting b*F^2 + g'(0)*F + g(0), and solve for a point where its derivative is zero: F = -g'(0) / 2b
            nextFactor = -slope / (2*b);
          }
          else // otherwise, there wouldn't be a division by zero, so solve for F
          {
            double discriminant = b*b - 3*a*slope;
            if(discriminant < 0) nextFactor = 0.5 * factor; // if there's no real solution, just set F to be as big as possible
            else if(b <= 0) nextFactor = (-b+Math.Sqrt(discriminant)) / (3*a); // if the solution would come out normally, use it
            else nextFactor = -slope / (b + Math.Sqrt(discriminant)); // otherwise... (i don't understand this bit)
          }

          if(nextFactor > 0.5*factor) nextFactor = 0.5*factor; // clip the new factor to be no larger than half the old factor
        }

        prevFactor = factor;   // keep track of the previous factor (F'')
        prevValue  = newValue; // and the previous value g(F'')
        factor     = Math.Max(nextFactor, 0.1*factor); // clip the new factor to be no smaller than one tenth the old factor
      }
    }
  }

  static double GetDefaultAccuracy(RootInterval interval)
  {
    return (Math.Abs(interval.Min) + Math.Abs(interval.Max)) * (0.5 * IEEE754.DoublePrecision);
  }

  static ArgumentException RootNotBracketedError()
  {
    return new ArgumentException("The interval does not bracket a root.");
  }

  static RootNotFoundException RootNotFoundError()
  {
    return new RootNotFoundException("No root found within the given interval and tolerance.");
  }

  static void ValidateArguments(object function, RootInterval interval)
  {
    if(function == null) throw new ArgumentNullException();
    if(interval.Min > interval.Max) throw new ArgumentException("Invalid interval.");
  }

  static void ValidateArguments(object function, RootInterval interval, double accuracy)
  {
    ValidateArguments(function, interval);
    if(accuracy < 0) throw new ArgumentOutOfRangeException();
  }

  /// <summary>Returns a value having the magnitude of the first argument and the sign of the second argument.</summary>
  static double WithSign(double value, double sign)
  {
    return (sign < 0) ^ (value < 0) ? -value : value;
  }
}
#endregion

} // namespace AdamMil.Mathematics.RootFinding