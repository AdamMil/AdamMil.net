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
using AdamMil.Utilities;

namespace AdamMil.Mathematics.Optimization
{

#region MinimumBracket
/// <summary>Represents an interval in which a local minimum of a one-dimensional function is assumed to exist.</summary>
/// <remarks>A local minimum is assumed to be bracketed between points x and z when there exists a point y between x and z where
/// f(x) &gt;= f(y) and f(z) &gt;= f(y).
/// </remarks>
[Serializable]
public struct MinimumBracket
{
  /// <summary>Initializes a new <see cref="MinimumBracket"/> given values for <see cref="High1"/>, <see cref="Low"/>, and
  /// <see cref="High2"/>.
  /// </summary>
  public MinimumBracket(double high1, double low, double high2)
  {
    High1 = high1;
    Low   = low;
    High2 = high2;
  }

  /// <summary>Represents a value of the function parameter for which the function value is greater than (or equal to) the value obtained
  /// using <see cref="Low"/>.
  /// </summary>
  public double High1, High2;
  /// <summary>Represents a value of the function parameter for which the function value is less than (or equal to) the value obtained
  /// using <see cref="High1"/> or <see cref="High2"/>.
  /// </summary>
  public double Low;
}
#endregion

#region MinimumNotFoundException
/// <summary>An exception thrown when a minimum of a function could not be found.</summary>
[Serializable]
public class MinimumNotFoundException : Exception
{
  /// <summary>Initializes a new <see cref="MinimumNotFoundException"/>.</summary>
  public MinimumNotFoundException() { }
  /// <summary>Initializes a new <see cref="MinimumNotFoundException"/>.</summary>
  public MinimumNotFoundException(string message) : base(message) { }
  /// <summary>Initializes a new <see cref="MinimumNotFoundException"/>.</summary>
  public MinimumNotFoundException(string message, Exception innerException) : base(message, innerException) { }
  /// <summary>Initializes a new <see cref="MinimumNotFoundException"/>.</summary>
  public MinimumNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
#endregion

#region Minimize
/// <summary>Provides methods for minimizing functions, that is for finding parameters that minimize the values of functions.</summary>
/// <remarks>For one-dimensional function minimization, <see cref="Brent"/> is the recommended method. There is a version that takes a
/// differentiable function and uses the derivative, as well as a version that takes a general function and does not use the derivative.
/// <see cref="GoldenSection"/> is a simple, robust method that is not particularly fast, and is generally not recommended, since Brent's
/// method is substantially faster in most cases. Before minimizing a one-dimensional function, you must first bracket the minimum.
/// <see cref="BracketInward"/> and <see cref="BracketOutward"/> exist to help with this.
/// </remarks>
public static class Minimize
{
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/BracketInward/*"/>
  public static IEnumerable<MinimumBracket> BracketInward(IOneDimensionalFunction function, double x1, double x2, int segments)
  {
    if(function == null) throw new ArgumentNullException();
    return BracketInward(function.Evaluate, x1, x2, segments);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/BracketInward/*"/>
  public static IEnumerable<MinimumBracket> BracketInward(Func<double, double> function, double x1, double x2, int segments)
  {
    if(function == null) throw new ArgumentNullException();
    if(segments <= 0) throw new ArgumentOutOfRangeException();

    double segmentSize = (x2 - x1) / segments, v1 = function(x1), origX2 = x2;
    x2 = x1;
    for(int i=0; i<segments; i++)
    {
      x2 = i == segments-1 ? origX2 : x2 + segmentSize; // make sure the end of the last segment exactly matches the original end
      // get the value of the function at the midpoint of the segment
      double xm = x1 + (x2-x1)*0.5, v2 = function(x2), vm = function(xm);

      // if the function doesn't appear to bracket a minimum based on the midpoint, fit a parabola to the three points and minimize that
      if(vm >= v1 || vm >= v2)
      {
        // to fit a parabola to three points we first take the formula for a quadratic: A*x^2 + B*x + C = y. then we substitute for the
        // three (x,y) pairs and get three linear equations that we can solve for the coefficients A, B, and C. then we find the minimum or
        // maximum by solving for the point where the derivative (2A*x + B) equals zero, and we get x = -B/2A. note that this doesn't
        // depend on the value of C, so we needn't compute it. the straightforward solution gives:
        // d = (x1-x2) * (x1-x3) * (x2-x3)
        // A = (x1*(y3-y2) + x2*(y1-y3) + x3*(y2-y1)) / d
        // B = (x1^2*(y2-y3) + x2^2*(y3-y1) + x3^2*(y1-y2)) / d
        //
        // then -B / 2A = -((x1^2*(y2-y3) + x2^2*(y3-y1) + x3^2*(y1-y2)) / d) * (d / 2*(x1*(y3-y2) + x2*(y1-y3) + x3*(y2-y1)))
        // (multiplying by the reciprocal) and d cancels out. distributing the negation in the numerator leaves:
        // (x1^2*(y3-y2) + x2^2*(y1-y3) + x3^2*(y2-y1)) / (2*(x1*(y3-y2) + x2*(y1-y3) + x3*(y2-y1)))
        //
        // this equals (x1*g + x2*h + x3*j) / 2(g+h+j) if we take g=x1(y3-y2), h=x2(y1-y3), and j=x3(y2-y1). two final wrinkles: if A = 0,
        // then this would involve division by zero. in that case, the quadratic reduces to a linear form (B*x + C = y). in that
        // case, the line may have a minimum inside the subinterval (at an edge), but it likely continues beyond the subinterval, in
        // which case it's not really a minimum of the function. so we'll ignore the case where A = 0. also, it's possible that the minimum
        // of the parabola is outside the subinterval. in that case also, we'll ignore it.
        // note that in the following code (x1,xm,x2,v1,vm,v2) represent (x1,x2,x3,y1,y2,y3) in the math
        double g = x1*(v2-vm), h = xm*(v1-v2), j = x2*(vm-v1), d = g + h + j;
        if(d != 0) // if we can fit a proper parabola to it...
        {
          double x = (x1*g + xm*h + x2*j) / (2*d); // take the minimum or maximum of the parabola
          if((x1-x)*(x-x2) > 0) // if the minimum or maximum of the parabola is within the subinterval...
          {
            xm = x; // use that
            vm = function(xm); // and get the function value there
          }
        }
      }

      // if we found a minimum either way, return it
      if(v1 > vm && v2 > vm) yield return new MinimumBracket(x1, xm, x2);

      x1 = x2;
      v1 = v2;
    }
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/BracketOutward/*"/>
  public static bool BracketOutward(IOneDimensionalFunction function, double x1, double x2, out MinimumBracket bracket)
  {
    if(function == null) throw new ArgumentNullException();
    return BracketOutward(function.Evaluate, x1, x2, out bracket);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/BracketOutward/*"/>
  public static bool BracketOutward(Func<double, double> function, double x1, double x2, out MinimumBracket bracket)
  {
    if(function == null) throw new ArgumentNullException();

    // we'll use the golden ratio for the expansion factor. this is related to the fact that the optimal shape of a bracket for
    // minimization via subdivision shrinks it by a factor of the golden ratio on each iteration (see GoldenSection). so we'll strive to
    // output a bracket of this optimal shape to increase the efficiency of routines that make use of subdivision
    const double Expansion = MathHelpers.GoldenRatio, MaxStep = 100;

    // to bracket outward, we'll maintain three points x1, xm, and x2 where f(x1) >= f(xm), and we'll attempt to find a point x2 where
    // f(x2) >= f(xm). (the x1 and x2 parameters become the initial values of x1 and xm, in whichever order is needed to maintain the
    // invariant.) if haven't found a bracket yet, then we have f(x1) >= f(xm) > f(x2), so the three points x1, xm, x2 are heading
    // downhill. we can then take xm = x2 and expand x2 by some amount. if f(x2) has not decreased after doing that, then f(x2) >= f(xm)
    // and we're done. rather than increasing x2 blindly in many steps, we can attempt to fit a parabola to the three points and find its
    // vertex. that should take us close to the turning point if the function can be locally well-approximated by an upward-opening
    // parabola. if the quadratic fit doesn't help (because the function value increased at the vertex point), then we'll expand by a
    // constant factor -- the golden ratio
    double xm, v1 = function(x1), vm = function(x2), v2;
    if(vm > v1)
    {
      Utility.Swap(ref x1, ref x2);
      Utility.Swap(ref v1, ref vm);
    }

    // get the initial guess for x2 by merely expanding the bracket by a constant factor
    xm = x2;
    x2 = xm + (xm-x1)*Expansion;
    v2 = function(x2);

    while(vm > v2) // while we haven't found a suitable value for x2...
    {
      // see BracketInward for a description of how the parabolic fit works
      double g = x1*(v2-vm), h = xm*(v1-v2), j = x2*(vm-v1), d = g + h + j, x, v, xLimit = xm + (x2-xm)*MaxStep;
      if(d != 0) // if we can fit a proper parabola to it...
      {
        x = (x1*g + xm*h + x2*j) / (2*d); // find the vertex of the parabola
        if((xm-x)*(x-x2) > 0) // if the vertex is between xm and x2...
        {
          v = function(x);
          if(v < v2) // if f(x) < f(x2), then we have a minimum with the points xm,x,x2. we need f(x) <= f(xm) and f(x) <= f(x2). the first
          {          // is guaranteed by the shape of the parabola. since the vertex is between xm and x2, which the parabola passes
                     // through, it must either be above or below both. so since f(x) < f(x2) it must also be the case that f(x) < f(xm)
            x1 = xm; v1 = vm;
            xm = x;  vm = v;
            break;
          }
          else if(v > vm) // if f(x) > f(xm), then we have a minimum with the points x1,xm,x. we need f(xm) <= f(x1) and f(xm) <= f(x). the
          {               // first condition is guaranteed by the invariant f(x1) >= f(xm)
            x2 = x;
            v2 = v;
            break;
          }
          // otherwise, the function value was between f(xm) and f(x2). this doesn't give us a minimum, and with the vertex inside the
          // interval, it doesn't expand the interval either. so we'll expand the interval by a fixed factor
          goto expand;
        }
      }
      else // otherwise, the parabola was degenerate because the points are colinear
      {
        x = xLimit; // move x as far along the line as we'll allow
      }

      if((x2-x)*(x-xLimit) > 0) // if x is between x2 and xLimit...
      {
        v = function(x);
        if(v < v2) // if f(x) < f(x2) then we have f(x1) >= f(xm) > f(x2) > f(x), so we can discard xm to get x1,x2,x. this allows the
        {          // interval to keep expanding by a fixed factor (since the expansion is based on xm and x2). then we expand the result
          xm = x2; vm = v2;
          x2 = x;  v2 = v;
          goto expand;
        }
        else
        {
          // otherwise, we have f(x1) >= f(xm) > f(x2) <= f(x). this is a minimum with xm,x2,x if f(xm) <= f(x). we'll do the shift below
          // and the check at the start of the next iteration. in any case we want to shift x -> x2 -> xm to keep the expansion geometric
          goto shift;
        }
      }
      else if((x-xLimit)*(xLimit-x2) >= 0) // if x is beyond than the limit...
      {
        x = xLimit; // clip it to the limit and then shift it into place
        v = function(x);
        goto shift;
      }

      expand:
      x = x2 + (x2-xm)*Expansion;
      v = function(x);

      shift:
      x1 = xm; v1 = vm;
      xm = x2; vm = v2;
      x2 = x;  v2 = v;
    }

    bracket = new MinimumBracket(x1, xm, x2);
    // if the size of the interval is too large to be represented (e.g. we didn't find a minimum), then we failed
    return !double.IsInfinity(x2-x1);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[@name != 'accuracy' and @name != 'value']"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/NDBrentRemarks/*"/>
  public static double Brent(IOneDimensionalFunction function, MinimumBracket bracket)
  {
    if(function == null) throw new ArgumentNullException();
    return Brent(function.Evaluate, bracket);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[@name != 'accuracy']"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/NDBrentRemarks/*"/>
  public static double Brent(IOneDimensionalFunction function, MinimumBracket bracket, out double value)
  {
    if(function == null) throw new ArgumentNullException();
    return Brent(function.Evaluate, bracket, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[@name != 'value']"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/NDBrentRemarks/*"/>
  [CLSCompliant(false)]
  public static double Brent(IOneDimensionalFunction function, MinimumBracket bracket, double accuracy)
  {
    if(function == null) throw new ArgumentNullException();
    return Brent(function.Evaluate, bracket, accuracy);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/NDBrentRemarks/*"/>
  public static double Brent(IOneDimensionalFunction function, MinimumBracket bracket, double accuracy, out double value)
  {
    if(function == null) throw new ArgumentNullException();
    return Brent(function.Evaluate, bracket, accuracy, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[@name != 'accuracy' and @name != 'value']"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/NDBrentRemarks/*"/>
  public static double Brent(Func<double, double> function, MinimumBracket bracket)
  {
    double value;
    return Brent(function, bracket, IEEE754.SqrtDoublePrecision, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[@name != 'accuracy']"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/NDBrentRemarks/*"/>
  public static double Brent(Func<double, double> function, MinimumBracket bracket, out double value)
  {
    return Brent(function, bracket, IEEE754.SqrtDoublePrecision, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[@name != 'value']"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/NDBrentRemarks/*"/>
  [CLSCompliant(false)]
  public static double Brent(Func<double, double> function, MinimumBracket bracket, double accuracy)
  {
    double value;
    return Brent(function, bracket, accuracy, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/NDBrentRemarks/*"/>
  public static double Brent(Func<double, double> function, MinimumBracket bracket, double accuracy, out double value)
  {
    if(function == null) throw new ArgumentNullException();
    if(accuracy < 0) throw new ArgumentOutOfRangeException();

    // Brent's method combines the sureness of the golden section search with the parabolic interpolation described in BracketInside().
    // this allows it to converge quickly to the minimum if the local behavior of the function can be roughly approximated by a
    // parabola and to get there eventually if it can't. the difficulty is knowing when to switch between the two approaches. Brent's
    // method keeps track of six points: the two edges of the bracket, the points giving the least and second-least known values,
    // the previous least point from the last iteration (which was evaluated two iterations prior), and the most recently evaluated point.
    // in short, Brent's method uses parabolic interpolation when the interpolated point is in the bracket and the movement is less than
    // half the distance from the previous least point (evaluated two iterations prior). requiring it to be less ensures that the parabolic
    // interpolation is working (as the function should be getting smoother and the jumps smaller) and not cycling. using the point from
    // two iterations prior rather than one is a heuristic -- requiring two bad steps in a row before switching to golden section search

    const double InvGoldenRatioComp = 0.38196601125010515; // one minus the inverse of the golden ratio, used by golden section search
    const int MaxIterations = 100;

    double left = Math.Min(bracket.High1, bracket.High2), right = Math.Max(bracket.High1, bracket.High2);
    double minPt = bracket.Low, secondMinPt = minPt, prev2ndMinPt = minPt;
    double minVal = function(minPt), secondMinVal = minVal, prev2ndMinVal = minVal, step = 0, prevStep = 0;

    for(int iteration=0; iteration < MaxIterations; iteration++)
    {
      // we're done when the distance between the brackets is less than or equal to minPt*accuracy*2 and minPt is centered in the bracket
      double mid = 0.5*(left+right);
      double tol1 = accuracy * Math.Abs(minPt) + (IEEE754.DoublePrecision*0.001); // prevent tol2 from being zero when minPt is zero
      double tol2 = tol1 * 2;
      if(Math.Abs(minPt-mid) <= tol2 - 0.5*(right-left))
      {
        value = minVal;
        return minPt;
      }

      double x;
      if(Math.Abs(prevStep) <= tol1) // if the step we'd compare the parabolic interpolation against is too small (near the roundoff error)
      {
        // we can't meaningfully compare the interpolation step against it, and the interpolation step is unlikely to be smaller than it,
        // so just do golden section search
        prevStep = (minPt >= mid ? left : right) - minPt;
        step     = prevStep * InvGoldenRatioComp;
      }
      else // otherwise, the previous step was substantial, so attempt parabolic interpolation
      {
        // see BracketInward() for a general description of how the parabolic interpolation works. one difference is that BracketInward()
        // computes the position of the vertex, but actually the step size to get from the old minimum point to the vertex
        double g = secondMinPt*(prev2ndMinVal-minVal), h = minPt*(secondMinVal-prev2ndMinVal), j = prev2ndMinPt*(minVal-secondMinVal);
        double d = g + h + j; // calculate the denominator
        // if the denominator is zero, use a point that will force a subdivision step
        x = d == 0 ? double.PositiveInfinity : (secondMinPt*g + minPt*h + prev2ndMinPt*j)/(2*d);
        double newStep = x - minPt; // subtract minPt from the vertex to get the step size

        // if the step size isn't less than than half the previous step size, or would take us out of bounds...
        if(Math.Abs(newStep) >= Math.Abs(0.5*prevStep) || x <= left || x >= right)
        {
          prevStep = (minPt >= mid ? left : right) - minPt; // then use golden section search
          step     = prevStep * InvGoldenRatioComp;
        }
        else // otherwise, the interpolation is valid
        {
          prevStep = step;
          step     = newStep;
          if(x-left < tol2 || right-x < tol2) step = MathHelpers.WithSign(tol1, mid-minPt);
        }
      }

      // if the step size is greater than the roundoff error, use it. otherwise, use a minimum step size to ensure we're actually getting
      // somewhere
      x = minPt + (Math.Abs(step) >= tol1 ? step : MathHelpers.WithSign(tol1, step));
      double v = function(x);

      if(v <= minVal) // if the new value is less than or equal to the smallest known value...
      {
        // update the bracket, making the old best point an edge. we have f(left) >= f(minPt) and f(right) >= f(minPt) and f(minPt) >= f(x)
        if(x >= minPt) left = minPt; // if the new point is to the right of the old minimum, a new bracket is minPt, x, right
        else right = minPt; // otherwise, it's to the left, and a new bracket is left, x, minPt

        // make the minimum point the previous minimum point, the new point the minimum point, etc.
        prev2ndMinPt = secondMinPt; prev2ndMinVal = secondMinVal;
        secondMinPt  = minPt;       secondMinVal  = minVal;
        minPt        = x;           minVal        = v;
      }
      else // the new value is greater than the smallest known value...
      {
        // update the bracket, making the new point an edge. we have f(left) >= f(minPt) and f(right) >= f(minPt) and f(x) >= f(minPt)
        if(x < minPt) left = x; // if the new point is to the left of the old minimum, a new bracket is x, minPt, right
        else right = x; // otherwise, it's to the right, and a new bracket is left, minPt, x

        if(v <= secondMinVal || secondMinPt == minPt) // if the new value is between the minimum value and the second minimum value...
        {
          prev2ndMinPt = secondMinPt; prev2ndMinVal = secondMinVal; // then make the new point the second minimum value
          secondMinPt  = x;           secondMinVal  = v;
        }
        // otherwise, if it's between the second minimum value and the previous second minimum value...
        else if(v <= prev2ndMinVal || prev2ndMinPt == minPt || prev2ndMinPt == secondMinPt)
        {
          prev2ndMinPt  = x; // make it the previous second minimum value
          prev2ndMinVal = v;
        }
      }
    }

    throw MinimumNotFoundError();
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[@name != 'accuracy' and @name != 'value']"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/DBrentRemarks/*"/>
  public static double Brent(IDifferentiableFunction function, MinimumBracket bracket)
  {
    double value;
    return Brent(function, bracket, IEEE754.SqrtDoublePrecision, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[@name != 'accuracy']"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/DBrentRemarks/*"/>
  public static double Brent(IDifferentiableFunction function, MinimumBracket bracket, out double value)
  {
    return Brent(function, bracket, IEEE754.SqrtDoublePrecision, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[@name != 'value']"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/DBrentRemarks/*"/>
  [CLSCompliant(false)]
  public static double Brent(IDifferentiableFunction function, MinimumBracket bracket, double accuracy)
  {
    double value;
    return Brent(function, bracket, accuracy, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/DBrentRemarks/*"/>
  public static double Brent(IDifferentiableFunction function, MinimumBracket bracket, double accuracy, out double value)
  {
    if(function == null) throw new ArgumentNullException();
    if(accuracy < 0) throw new ArgumentOutOfRangeException();

    // Brent's method with derivative information is somewhat different from Brent's method without. the basic idea is that, given a
    // bracketed minimum, the sign of the derivative at the low point indicates which direction is downhill (to the left when positive and
    // to the right when negative). then, the secant method can be used to extrapolate and find the point where the derivative would be
    // near zero (assuming linear behavior around the point). as in Brent's method, we also maintain a point from two iterations prior and
    // use the secant method with that point as well. if both secant lines provide valid extrapolations, we choose the one that implies
    // a smaller step size
    const int MaxIterations = 100;

    double left = Math.Min(bracket.High1, bracket.High2), right = Math.Max(bracket.High1, bracket.High2);
    double minPt = bracket.Low, secondMinPt = minPt, prev2ndMinPt = minPt;
    double minVal = function.Evaluate(minPt), secondMinVal = minVal, prev2ndMinVal = minVal, step = 0, prevStep = 0;
    double minDeriv = function.EvaluateDerivative(minPt, 1), secondMinDeriv = minDeriv, prev2ndMinDeriv = minDeriv;

    for(int iteration=0; iteration < MaxIterations; iteration++)
    {
      // we're done when the distance between the brackets is less than or equal to minPt*accuracy*2 and minPt is centered in the bracket
      double mid = 0.5*(left+right);
      double tol1 = accuracy * Math.Abs(minPt) + (IEEE754.DoublePrecision*0.001); // prevent tol2 from being zero when minPt is zero
      double tol2 = tol1 * 2;
      if(Math.Abs(minPt-mid) <= tol2 - 0.5*(right-left))
      {
        value = minVal;
        return minPt;
      }

      double x;
      if(Math.Abs(prevStep) <= tol1) // if the previous step was very small (near the level of roundoff error), then the secant method is
      {                              // unlikely to provide a useful extrapolation, so just bisect the interval
        // we can't meaningfully compare the interpolation step against it, and the interpolation step is unlikely to be smaller than it,
        // so just use bisection
        goto bisect;
      }
      else
      {
        // we'll compute two step sizes, based on the two secant lines using the best point with the two previous best points
        double step1 = double.PositiveInfinity, step2 = step1; // initialize the step sizes to invalid values
        if(secondMinDeriv  != minDeriv) step1 = (secondMinDeriv  - minDeriv) * minDeriv / (minDeriv - secondMinDeriv);
        if(prev2ndMinDeriv != minDeriv) step2 = (prev2ndMinDeriv - minDeriv) * minDeriv / (minDeriv - prev2ndMinDeriv);

        // check which of the steps are valid. they must remain in bounds and have the opposite sign of the derivative (i.e. stepping in
        // the downhill direction)
        x = minPt + step1;
        bool ok1 = (left-x)*(x-right) > 0 && minDeriv*step1 <= 0;
        x = minPt + step2;
        bool ok2 = (left-x)*(x-right) > 0 && minDeriv*step2 <= 0;

        double prevPrevStep = prevStep;
        prevStep = step;

        if(!ok1 && !ok2) // if neither are okay, then just bisect the interval
        {
          goto bisect;
        }
        else // otherwise, one of the secant steps is good
        {
          if(ok1 && ok2) step = Math.Abs(step1) < Math.Abs(step2) ? step1 : step2; // if both are good, choose the smaller step size
          else step = ok1 ? step1 : step2; // otherwise, choose whichever is valid

          // if the step is more than half the previous step size (i.e. it's not converging quickly enough, is cycling, etc.), then bisect
          if(Math.Abs(step2) > Math.Abs(0.5*prevPrevStep))
          {
            goto bisect;
          }
          else
          {
            x = minPt + step;
            if(x-left < tol2 || right-x < tol2) step = MathHelpers.WithSign(tol1, mid-minPt);
            goto done;
          }
        }
      }

      bisect:
      prevStep = (minPt >= mid ? left : right) - minPt;
      step     = prevStep * 0.5;

      done:
      double v;
      if(Math.Abs(step) >= tol1) // if the step size is greater than the roundoff error, use it
      {
        x = minPt + step;
        v = function.Evaluate(x);
      }
      else // otherwise, use a minimum step size to ensure we're actually getting somewhere
      {
        x = minPt + MathHelpers.WithSign(tol1, step);
        v = function.Evaluate(x);
        if(v > minVal) // if the minimum step size caused an increase in function value, then we were already as close as we can get
        {
          value = minVal; // so return the best point
          return minPt;
        }
      }

      double deriv = function.EvaluateDerivative(x, 1);
      if(v <= minVal) // if the new value is less than or equal to the smallest known value...
      {
        // update the bracket, making the old best point an edge. we have f(left) >= f(minPt) and f(right) >= f(minPt) and f(minPt) >= f(x)
        if(x >= minPt) left = minPt; // if the new point is to the right of the old minimum, a new bracket is minPt, x, right
        else right = minPt; // otherwise, it's to the left, and a new bracket is left, x, minPt

        // make the minimum point the previous minimum point, the new point the minimum point, etc.
        prev2ndMinPt = secondMinPt; prev2ndMinVal = secondMinVal; prev2ndMinDeriv = secondMinDeriv;
        secondMinPt  = minPt;       secondMinVal  = minVal;       secondMinDeriv  = minDeriv;
        minPt        = x;           minVal        = v;            minDeriv        = deriv;
      }
      else // the new value is greater than the smallest known value...
      {
        // update the bracket, making the new point an edge. we have f(left) >= f(minPt) and f(right) >= f(minPt) and f(x) >= f(minPt)
        if(x < minPt) left = x; // if the new point is to the left of the old minimum, a new bracket is x, minPt, right
        else right = x; // otherwise, it's to the right, and a new bracket is left, minPt, x

        if(v <= secondMinVal || secondMinPt == minPt) // if the new value is between the minimum value and the second minimum value...
        {                                             // then make the new point the second minimum value
          prev2ndMinPt = secondMinPt; prev2ndMinVal = secondMinVal; prev2ndMinDeriv = secondMinDeriv;
          secondMinPt  = x;           secondMinVal  = v;            secondMinDeriv  = deriv;
        }
        // otherwise, if it's between the second minimum value and the previous second minimum value...
        else if(v <= prev2ndMinVal || prev2ndMinPt == minPt || prev2ndMinPt == secondMinPt)
        {
          prev2ndMinPt    = x; // make it the previous second minimum value
          prev2ndMinVal   = v;
          prev2ndMinDeriv = deriv;
        }
      }
    }

    throw MinimumNotFoundError();
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[@name != 'accuracy' and @name != 'value']"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/GoldenSectionRemarks/*"/>
  public static double GoldenSection(IOneDimensionalFunction function, MinimumBracket bracket)
  {
    if(function == null) throw new ArgumentNullException();
    return GoldenSection(function.Evaluate, bracket);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[@name != 'accuracy']"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/GoldenSectionRemarks/*"/>
  public static double GoldenSection(IOneDimensionalFunction function, MinimumBracket bracket, out double value)
  {
    if(function == null) throw new ArgumentNullException();
    return GoldenSection(function.Evaluate, bracket, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[@name != 'value']"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/GoldenSectionRemarks/*"/>
  [CLSCompliant(false)]
  public static double GoldenSection(IOneDimensionalFunction function, MinimumBracket bracket, double accuracy)
  {
    if(function == null) throw new ArgumentNullException();
    return GoldenSection(function.Evaluate, bracket, accuracy);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/GoldenSectionRemarks/*"/>
  public static double GoldenSection(IOneDimensionalFunction function, MinimumBracket bracket, double accuracy, out double value)
  {
    if(function == null) throw new ArgumentNullException();
    return GoldenSection(function.Evaluate, bracket, accuracy, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[@name != 'accuracy' and @name != 'value']"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/GoldenSectionRemarks/*"/>
  public static double GoldenSection(Func<double, double> function, MinimumBracket bracket)
  {
    double value;
    return GoldenSection(function, bracket, IEEE754.SqrtDoublePrecision, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[@name != 'accuracy']"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/GoldenSectionRemarks/*"/>
  public static double GoldenSection(Func<double, double> function, MinimumBracket bracket, out double value)
  {
    return GoldenSection(function, bracket, IEEE754.SqrtDoublePrecision, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[@name != 'value']"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/GoldenSectionRemarks/*"/>
  [CLSCompliant(false)]
  public static double GoldenSection(Func<double, double> function, MinimumBracket bracket, double accuracy)
  {
    double value;
    return GoldenSection(function, bracket, accuracy, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/GoldenSectionRemarks/*"/>
  public static double GoldenSection(Func<double, double> function, MinimumBracket bracket, double accuracy, out double value)
  {
    if(function == null) throw new ArgumentNullException();
    if(accuracy < 0) throw new ArgumentOutOfRangeException();

    // the golden section search works is analogous to the bisection search for finding roots. it works by repeatedly shrinking the bracket
    // around the minimum until the bracket is very small. we maintain four points x0, x1, x2, and x3 where f(x0) >= f(x1) and
    // f(x3) >= f(x1) or f(x0) >= f(x2) and f(x3) >= f(x2). that is, x0 and x3 represent the edges of the bracket, x1 or x2 is the low
    // point in the bracket, and the other point is the new point we're evaluating at each step. so at each iteration, we have two high
    // points on the edges, and one low point and one point of unknown value in between: (H  L  U  H) or (H  U  L  H), stored in
    // (x0 x1 x2 x3). depending on the relationship between the low and unknown point, we can shrink the bracket on one side or another.
    // if f(x1) <= f(x2) then x0,x1,x2 form a new bracket. otherwise, f(x2) < f(x1) and x1,x2,x3 form a new bracket.
    // note that either x0 <= x1 <= x2 <= x3 or x0 >= x1 >= x2 >= x3.
    //
    // the only complexity is the selection of the new point to test. if the points are initially evenly spaced (|  |  |  |), then when the
    // bracket is shrunk by discarding one of the edges, one point will be in the center (|  |  |), and it's not possible to select a new
    // inner point that results in even spacing. the two inner points would end up on one side or another (| | |   |). the lopsided points
    // make the logic more complex and in the worst case, the bracket only shrinks by 25% each iteration. we can do better and simplify the
    // logic using the golden ratio (actually, its inverse). the inverse of the golden ratio is about 0.62, and its complement is about
    // 1 - 0.62 = 0.38. then, we choose new inner points such that x1 is 38% of the way between x0 and x3, and x2 is about 62% of the
    // way. (equivalently, x1 is 38% of the way between x0 and x3, and x2 is 38% of the way between x1 and x3, due to the special
    // properties of the golden ratio.) this results in a shape like (|  | |  |). when shrunk we get (|  | |), say, and the new point can
    // be chosen thus (| || |). this results in consistent performance, as the shrinkage is always the same 38%. any other arrangement
    // gives worse performance in the worst case. note that there's no guarantee that the initial bracket will conform to this shape, but
    // we can choose points so that it converges on the right shape

    const double InvGoldenRatio = 0.61803398874989485, InvGRComplement = 1 - InvGoldenRatio;
    double x0 = bracket.High1, x1, x2, x3 = bracket.High2;

    // if the initial middle point is closer to the left side (e.g. |  |    |), choose the new point to closer to the right side
    if(Math.Abs(x3 - bracket.Low) > Math.Abs(bracket.Low - x0))
    {
      x1 = bracket.Low;                  // place x2 38% of the way from x3-x1 to put it in the right place. if x1 is not in the right
      x2 = x1 + (x3-x1)*InvGRComplement; // place, the placement will tend to counteract x1's mispositioning
    }
    else // otherwise, the initial point is closer to the right side (or centered), so choose the new point closer to the right
    {
      x2 = bracket.Low;
      x1 = x2 - (x2-x0)*InvGRComplement; // place x1 62% of the way from x0 to x2 (38% back from x2)
    }

    double v1 = function(x1), v2 = function(x2);
    // in general, we can only expect to get the answer to within a fraction of the center value
    while(Math.Abs(x3-x0) > (Math.Abs(x1)+Math.Abs(x2))*accuracy) // while the bracket is still too large compared to the center values...
    {
      if(v2 < v1) // if f(x2) < f(x1) then we have f(x1) > f(x2) and f(x3) >= f(x2), so we can take x1,x2,x3 as the new bracket
      {
        double x = x2*InvGoldenRatio + x3*InvGRComplement;
        x0 = x1;
        x1 = x2; v1 = v2;
        x2 = x;  v2 = function(x);
      }
      else // otherwise, f(x1) <= f(x2), so we have f(x2) >= f(x1) and f(x0) >= f(x1), so we can take x0,x1,x2 as the new bracket
      {
        double x = x1*InvGoldenRatio + x0*InvGRComplement;
        x3 = x2;
        x2 = x1; v2 = v1;
        x1 = x;  v1 = function(x);
      }
    }

    // finally, when the bracket has shrunk to be very small, take the lower of v1 and v2 as the minimum
    if(v1 < v2)
    {
      value = v1;
      return x1;
    }
    else
    {
      value = v2;
      return x2;
    }
  }

  static MinimumNotFoundException MinimumNotFoundError()
  {
    throw new MinimumNotFoundException("No minimum found within the given interval and tolerance.");
  }
}
#endregion

} // namespace AdamMil.Mathematics.Optimization