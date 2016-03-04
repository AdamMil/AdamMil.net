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

// TODO: communicate results with an enum rather than throwing exceptions, here and in other functions

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using AdamMil.Mathematics.RootFinding;
using AdamMil.Utilities;

namespace AdamMil.Mathematics.Optimization
{

#region ConstrainedMinimizer
/// <summary>Solves a general constrained optimization problem by solving a series of related unconstrained optimization problems.</summary>
/// <remarks>
/// <para>This class works based on the fact that a constrained optimization problem can be represented as functions f(x), representing the
/// objective without constraints, and c(x), representing how near the parameters are to violating the constraints, and that minimizing the
/// new function h(x) = f(x) + r*p(c(x)) is equivalent to solving the original constrained problem in the limit as r goes to either
/// infinity or zero (depending on the <see cref="ConstraintEnforcement"/> method), and where p(e) is a function that converts the measure
/// of how near the parameters are to violating the constraints into a penalty value.
/// </para>
/// </remarks>
/// <include file="documentation.xml" path="/Math/Optimization/ConstrainedMinimizer/OOBRemarks/node()"/>
public class ConstrainedMinimizer
{
  /// <summary>Initializes the <see cref="ConstrainedMinimizer"/> with the objective function to minimize.</summary>
  /// <include file="documentation.xml" path="/Math/Optimization/ConstrainedMinimizer/OOBRemarks/node()"/>
  public ConstrainedMinimizer(IDifferentiableMDFunction objectiveFunction)
  {
    if(objectiveFunction == null) throw new ArgumentNullException();
    function = objectiveFunction;
    ConstraintEnforcement = ConstraintEnforcement.QuadraticPenalty;
  }

  /// <summary>Initializes the <see cref="ConstrainedMinimizer"/> with the objective function to minimize.</summary>
  /// <include file="documentation.xml" path="/Math/Optimization/ConstrainedMinimizer/OOBRemarks/node()"/>
  public ConstrainedMinimizer(IDifferentiableFunction objectiveFunction) : this(new DifferentiableMDFunction(objectiveFunction)) { }

  /// <summary>Gets or sets the base penalty multiplier. This is the multiplier applied to be penalty on the first iteration. On each later
  /// iteration, the penalty multiplier will be multiplied by a factor of <see cref="PenaltyChangeFactor"/>. The default is 1.
  /// </summary>
  public double BasePenaltyMultipier
  {
    get { return _basePenaltyMultiplier; }
    set
    {
      if(value <= 0) throw new ArgumentOutOfRangeException();
      _basePenaltyMultiplier = value;
    }
  }

  /// <summary>Gets or sets the factor by which the penalty changes on each iteration after the first. The default is 100, indicating
  /// that the penalty multiplier changes by a factor of 100 on each iteration. (For penalty methods (the default), the penalty
  /// multiplier increases by this factor each iteration, but for barrier methods, it decreases.) Increasing the value generally decreases
  /// the number of iterations required to solve the problem, but increases the chance of missing the answer entirely.
  /// </summary>
  public double PenaltyChangeFactor
  {
    get { return _penaltyChangeFactor; }
    set
    {
      if(value <= 1) throw new ArgumentOutOfRangeException();
      _penaltyChangeFactor = value;
    }
  }

  /// <summary>Gets or sets the method by which the constraints and bounds will be enforced. The default is
  /// <see cref="Optimization.ConstraintEnforcement.QuadraticPenalty"/>.
  /// </summary>
  public ConstraintEnforcement ConstraintEnforcement
  {
    get; set;
  }

  /// <summary>Gets or sets the constraint tolerance. The minimization process will be terminated if the fractional contribution of the
  /// penalty to the function value is no greater than the constraint tolerance. Larger values will allow the process to terminate with a
  /// greater degree of constraint violation. The default is <c>1e-9</c>.
  /// </summary>
  public double ConstraintTolerance
  {
    get { return _constraintTolerance; }
    set
    {
      if(value < 0) throw new ArgumentOutOfRangeException();
      _constraintTolerance = value;
    }
  }

  /// <summary>Gets or sets the gradient tolerance. Each iteration of the minimization process will attempt to reduce the gradient to
  /// zero, but since it is unlikely to ever reach zero exactly, it will terminate when the gradient is fractionally less than the
  /// gradient tolerance. Larger values allow the process to terminate slightly further from the minimum. The default is <c>1e-8</c>.
  /// If you receive <see cref="MinimumNotFoundException"/> errors, increasing this value may help.
  /// </summary>
  public double GradientTolerance
  {
    get { return _gradientTolerance; }
    set
    {
      if(value < 0) throw new ArgumentOutOfRangeException();
      _gradientTolerance = value;
    }
  }

  /// <summary>Gets or sets the parameter convergence tolerance. The minimization process will be terminated if the approximate fractional
  /// change in all parameters is no greater than the parameter tolerance. Larger values allow the process to terminate when the
  /// parameters are changing by a larger amount between iterations. The default is <see cref="IEEE754.DoublePrecision"/> times 4, and
  /// should not be made smaller than <see cref="IEEE754.DoublePrecision"/>.
  /// </summary>
  public double ParameterTolerance
  {
    get { return _parameterTolerance; }
    set
    {
      if(value < 0) throw new ArgumentOutOfRangeException();
      _parameterTolerance = value;
    }
  }

  /// <summary>Gets or sets the value convergence tolerance. The minimization process will be terminated if the fractional change in value
  /// is no greater than the value tolerance. Larger values allow the process to terminate when the objective value is changing by a
  /// larger amount between iterations. The default is <c>1e-9</c>.
  /// </summary>
  public double ValueTolerance
  {
    get { return _valueTolerance; }
    set
    {
      if(value < 0) throw new ArgumentOutOfRangeException();
      _valueTolerance = value;
    }
  }

  /// <summary>Adds a constraint to the system.</summary>
  /// <param name="constraint">A function that returns the distance from the parameters to the edge of the constraints. A positive return
  /// value indicates that the parameters violate the constraints, and a non-positive return value indicates that the parameters do not
  /// violate the constraints. You should not merely return constant values indicating whether the constraint is violated or not, but
  /// actually measure the distance to the constraint. For example, to implement the constraint <c>x*y &gt;= 5</c>, return <c>5 - x*y</c>,
  /// and to implement the constraint <c>x*y = 5</c>, return <c>Math.Abs(5 - x*y)</c>.
  /// </param>
  /// <remarks>For simple bounds constraints, such as <c>y &gt;= 10</c> or <c>0 &lt;= x &lt;= 5</c>, it is usually more convenient to use
  /// the <see cref="SetBounds"/> method, which adds the appropriate constraint automatically.
  /// </remarks>
  public void AddConstraint(IDifferentiableMDFunction constraint)
  {
    if(constraint == null) throw new ArgumentNullException();
    if(constraint.Arity != function.Arity)
    {
      throw new ArgumentException("The arity of the constraint does not match that of the objective.");
    }
    if(constraints == null) constraints = new List<IDifferentiableMDFunction>();
    constraints.Add(constraint);
  }

  /// <summary>Locally minimizes the objective function passed to the constructor subject to the constraints that have been added.</summary>
  /// <param name="x">An initial guess for the location of the minimum. In general, a global minimum may not be found unless you can supply
  /// a nearby initial point. If you cannot, then only a local minimum may be found. The initial point is not required to satisfy any
  /// bounds or constraints unless <see cref="ConstraintEnforcement"/> is set to a barrier method.
  /// </param>
  /// <remarks>This method is safe to call from multiple threads simultaneously, as long as the properties of the object are not modified
  /// while any minimization is ongoing. If you receive <see cref="MinimumNotFoundException"/> errors, try increasing the value of
  /// <see cref="GradientTolerance"/>.
  /// </remarks>
  public double Minimize(double[] x)
  {
    if(x == null) throw new ArgumentNullException();
    if(x.Length != function.Arity) throw new ArgumentException("The initial point does not have the correct number of dimensions.");

    // if no constraints have been added, just minimize the function normally
    if(minBound == null && constraints == null) return Optimization.Minimize.BFGS(function, x, GradientTolerance);

    const int MaxIterations = 100;
    PenaltyFunction penaltyFunction = new PenaltyFunction(this);
    double[] oldX = new double[x.Length];
    double value = 0;

    penaltyFunction.PenaltyFactor = BasePenaltyMultipier;
    for(int iteration=0; iteration<MaxIterations; iteration++)
    {
      double newValue;
      try
      {
        newValue = Optimization.Minimize.BFGS(penaltyFunction, x, GradientTolerance);
      }
      catch(MinimumNotFoundException) // sometimes early on (e.g. just the first iteration or two) it will fail to find a minimum
      {                               // but will succeed after the penalty factor ramps up. so we'll ignore those errors at first
        if(iteration < 10) newValue = penaltyFunction.Evaluate(x);
        else throw;
      }

      if(Math.Abs(penaltyFunction.LastPenalty) <= Math.Abs(newValue)*ConstraintTolerance ||
         (iteration != 0 &&
          (FindRoot.GetParameterConvergence(x, oldX) <= ParameterTolerance ||
            Math.Abs(newValue-value) / Math.Max(1, Math.Abs(value)) <= ValueTolerance)))
      {
        return newValue;
      }

      value = newValue;
      ArrayUtility.FastCopy(x, oldX, x.Length);

      // if we're using a barrier method, we need to decrease the penalty factor on each iteration. otherwise, we need to increase it
      if(IsBarrierMethod) penaltyFunction.PenaltyFactor /= PenaltyChangeFactor;
      else penaltyFunction.PenaltyFactor *= PenaltyChangeFactor;
    }

    throw Optimization.Minimize.MinimumNotFoundError();
  }

  /// <summary>Adds or removes a bounds constraint on a parameter.</summary>
  /// <param name="parameter">The ordinal of the parameter to bound, from 0 to one less than the arity of the objective function.</param>
  /// <param name="minimum">
  /// The minimum value of the parameter, or <see cref="double.NegativeInfinity"/> if the parameter is unbounded below.
  /// </param>
  /// <param name="maximum">
  /// The maximum value of the parameter, or <see cref="double.PositiveInfinity"/> if the parameter is unbounded above.
  /// </param>
  /// <remarks>To remove a bounds constraint, use <see cref="double.NegativeInfinity"/> for the minimum and
  /// <see cref="double.PositiveInfinity"/> for the maximum.
  /// To set an equality constraint, pass the same value for <paramref name="minimum"/> and <paramref name="maximum"/>. Note that
  /// equality constraints are not suitable for use with interior-point <see cref="ConstraintEnforcement"/> methods. Setting a bound with
  /// this method merely adds a constraint as though <see cref="AddConstraint"/> were called with a suitable constraint function. It does
  /// not in itself prevent the parameter from going out of bounds (although some types of <see cref="ConstraintEnforcement"/> methods do).
  /// </remarks>
  public void SetBounds(int parameter, double minimum, double maximum)
  {
    if((uint)parameter >= (uint)function.Arity || minimum > maximum ||
       minimum == double.PositiveInfinity || maximum == double.NegativeInfinity)
    {
      throw new ArgumentOutOfRangeException();
    }

    // if the bound is infinite, then we're actually removing the bound
    if(minimum == double.NegativeInfinity && maximum == double.PositiveInfinity)
    {
      if(minBound != null) // if the arrays have been allocated (i.e. if there are any bounds to remove)
      {
        minBound[parameter] = minimum; // remove it
        maxBound[parameter] = maximum;

        // see if there are any bounds remaining
        bool boundRemaining = false;
        for(int i=0; i<minBound.Length; i++)
        {
          if(minBound[i] != double.NegativeInfinity || maxBound[i] != double.PositiveInfinity)
          {
            boundRemaining = true;
            break;
          }
        }

        // if there are no bounds remaining, free the arrays to indicate that we don't need to perform bounds checking at all
        if(!boundRemaining) minBound = maxBound = null;
      }
    }
    else // otherwise, we're adding the bound
    {
      if(minBound == null) // if the necessary arrays haven't been allocated, do it now
      {
        minBound = new double[function.Arity];
        maxBound = new double[function.Arity];
        for(int i=0; i<minBound.Length; i++) minBound[i] = double.NegativeInfinity;
        for(int i=0; i<maxBound.Length; i++) maxBound[i] = double.PositiveInfinity;
      }
      minBound[parameter] = minimum;
      maxBound[parameter] = maximum;
    }
  }

  #region PenaltyFunction
  sealed class PenaltyFunction : IDifferentiableMDFunction
  {
    public PenaltyFunction(ConstrainedMinimizer minimizer)
    {
      this.minimizer = minimizer;
      gradient = new double[Arity];
    }

    public int Arity
    {
      get { return minimizer.function.Arity; }
    }

    public double PenaltyFactor, LastPenalty;

    public double Evaluate(params double[] x)
    {
      double totalPenalty = 0;

      // compute the penalty for out-of-bound parameters
      if(minimizer.minBound != null)
      {
        if(minimizer.IsBarrierMethod) // barrier methods apply penalties everywhere
        {
          for(int i=0; i<x.Length; i++)
          {
            if(minimizer.minBound[i] != double.NegativeInfinity) totalPenalty += GetPenaltyValue(minimizer.minBound[i] - x[i]);
            if(minimizer.maxBound[i] != double.PositiveInfinity) totalPenalty += GetPenaltyValue(x[i] - minimizer.maxBound[i]);
          }
          if(double.IsNaN(totalPenalty)) goto done; // quit early if a barrier constraint is violated
        }
        else // penalty methods have penalties only when constraints are violated
        {
          for(int i=0; i<x.Length; i++)
          {
            double param = x[i], penalty;
            if(param < minimizer.minBound[i]) penalty = minimizer.minBound[i] - param;
            else if(param > minimizer.maxBound[i]) penalty = param - minimizer.maxBound[i];
            else continue;
            totalPenalty += GetPenaltyValue(penalty);
          }
        }
      }

      if(minimizer.constraints != null)
      {
        for(int i=0; i<minimizer.constraints.Count; i++)
        {
          IDifferentiableMDFunction constraint = minimizer.constraints[i];
          double penalty = constraint.Evaluate(x);
          if(penalty > 0)
          {
            totalPenalty += GetPenaltyValue(penalty);
            if(double.IsNaN(totalPenalty)) goto done; // quit early if a barrier constraint is violated
          }
        }
      }

      totalPenalty *= PenaltyFactor;
      done:
      LastPenalty = totalPenalty;

      return double.IsNaN(totalPenalty) ? double.NaN : minimizer.function.Evaluate(x) + totalPenalty;
    }

    public void EvaluateGradient(double[] x, double[] output)
    {
      minimizer.function.EvaluateGradient(x, output);

      // compute the penalty for out-of-bound parameters
      if(minimizer.minBound != null)
      {
        if(minimizer.IsBarrierMethod) // barrier methods apply penalties everywhere
        {
          for(int i=0; i<x.Length; i++)
          {
            if(minimizer.minBound[i] != double.NegativeInfinity)
            {
              output[i] -= PenaltyFactor * GetPenaltyGradient(minimizer.minBound[i] - x[i]);
            }
            if(minimizer.maxBound[i] != double.PositiveInfinity)
            {
              output[i] += PenaltyFactor * GetPenaltyGradient(x[i] - minimizer.maxBound[i]);
            }
          }
        }
        else // penalty methods have penalties only when constraints are violated
        {
          for(int i=0; i<x.Length; i++)
          {
            double param = x[i];
            if(param < minimizer.minBound[i]) output[i] -= PenaltyFactor * GetPenaltyGradient(minimizer.minBound[i] - param);
            else if(param > minimizer.maxBound[i]) output[i] += PenaltyFactor * GetPenaltyGradient(param - minimizer.maxBound[i]);
          }
        }
      }

      if(minimizer.constraints != null)
      {
        for(int i=0; i<minimizer.constraints.Count; i++)
        {
          IDifferentiableMDFunction constraint = minimizer.constraints[i];
          double penalty = constraint.Evaluate(x);
          if(penalty > 0)
          {
            penalty = PenaltyFactor * GetPenaltyGradient(penalty);
            constraint.EvaluateGradient(x, gradient);
            for(int j=0; j<output.Length; j++) output[j] += penalty*gradient[j];
          }
        }
      }
    }

    double GetPenaltyGradient(double penalty)
    {
      switch(minimizer.ConstraintEnforcement)
      {
        case ConstraintEnforcement.LinearPenalty: return 1;
        case ConstraintEnforcement.QuadraticPenalty: return 2*penalty;
        case ConstraintEnforcement.InverseBarrier: return penalty >= 0 ? double.NaN : 1/(penalty*penalty);
        case ConstraintEnforcement.LogBarrier: return penalty >= 0 ? double.NaN : -1/penalty;
        default: throw new NotImplementedException();
      }
    }

    double GetPenaltyValue(double penalty)
    {
      switch(minimizer.ConstraintEnforcement)
      {
        case ConstraintEnforcement.LinearPenalty: return penalty;
        case ConstraintEnforcement.QuadraticPenalty: return penalty*penalty;
        case ConstraintEnforcement.InverseBarrier: return penalty >= 0 ? double.NaN : -1/penalty;
        case ConstraintEnforcement.LogBarrier: return penalty >= 0 ? double.NaN : -Math.Log(-penalty);
        default: throw new NotImplementedException();
      }
    }

    readonly ConstrainedMinimizer minimizer;
    readonly double[] gradient;
  }
  #endregion

  bool IsBarrierMethod
  {
    get
    {
      return ConstraintEnforcement == ConstraintEnforcement.LogBarrier || ConstraintEnforcement == ConstraintEnforcement.InverseBarrier;
    }
  }

  readonly IDifferentiableMDFunction function;
  List<IDifferentiableMDFunction> constraints;
  double[] minBound, maxBound;
  double _basePenaltyMultiplier=1, _penaltyChangeFactor=100;
  double _parameterTolerance=IEEE754.DoublePrecision*4, _constraintTolerance=1e-9, _valueTolerance=1e-9, _gradientTolerance=1e-8;
}
#endregion

#region ConstraintEnforcement
/// <summary>Determines the method of constraint enforcement used by the <see cref="ConstrainedMinimizer"/> class.</summary>
/// <remarks>
/// <para>There are two general types of penalty leading to two names for the constrained minimization process, called the penalty
/// method and the barrier method. In the penalty method, the penalty in zero in the feasible region and smoothly increases as the
/// constraints is violated. Violations of the constraint are merely penalized but not prevented. As such, it is also called an
/// exterior-point method, since the search point can move outside the feasible region. The penalty starts out weak and gradually
/// increases, enforcing the constraints with greater effectiveness. In the barrier method, a varying amount of penalty applies to
/// (and distorts) the entire feasible region, but the penalty climbs rapidly near the edge of the region, and reaches infinity at the edge
/// (beyond which a constraint would be violated). This singularity prevents the constraint from being violated at all, and so the method
/// is also known as an interior-point method. The penalty starts out strong (pushing the solution away from the edges of the feasible
/// area) and weakens gradually, allowing the solution to approach the edge if necessary, but making the transition into the singularity
/// more abrupt.
/// </para>
/// <para>There are pros and cons to both approaches. With the penalty method, the process can potentially produce results that
/// violate the constraints, but is capable of navigating areas of the search space outside the feasible region, and the process can be
/// started from any point. With the barrier method, the process can never leave the feasible region. This restricts
/// its possible movement, and requires that the initial point satisfy the constraints. If the barrier divides the space into separate
/// subspaces, the process is unlikely to escape the subspace containing the initial point, so if the minimum resides in another subspace,
/// it won't be found. In rare cases, the early distortion of the feasible region may force solutions toward the center of the space from
/// which the process may not escape. The barrier method is also generally unsuitable for equality constraints, as the penalty would always
/// be infinite, and even if it was not, the infinitesimally narrow feasible region would likely prevent all movement, given floating point
/// inaccuracy. Despite the barrier method's apparent limitations, when it works it works quite well, usually producing good results with
/// much less computational effort than penalty methods.
/// </para>
/// </remarks>
public enum ConstraintEnforcement
{
  /// <summary>A barrier (internal-point) method where the penalty is the negative logarithm of the negative distance from the feasible
  /// region (i.e. <c>-log -c(x)</c>). This results in a negative penalty over the entire feasible region up to a distance of 1 from the
  /// edge, and a positive penalty closer than that which increases exponentially. Generally, the distortion from a log barrier is greater
  /// than the distortion from an inverse barrier, but nonetheless the log barrier is somewhat superior to <see cref="InverseBarrier"/> in
  /// both accuracy and speed for many problems.
  /// </summary>
  LogBarrier,
  /// <summary>A barrier (internal-point) method where the penalty is equal to the negative inverse of the distance from the feasible
  /// region (i.e. <c>-1/c(x)</c>, but defined to be infinite when c(x) &gt;= 0). This results in a positive penalty applied to the entire
  /// feasible region which gets exponentially large near the barrier. Generally, the distortion from an inverse barrier is less than the
  /// distortion from a log barrier, but nonetheless the inverse barrier is somewhat inferior to <see cref="LogBarrier"/> in both accuracy
  /// and speed for many problems.
  /// </summary>
  InverseBarrier,
  /// <summary>A penalty (exterior-point) method where the penalty equals the square of the distance from the feasible region (e.g.
  /// <c>max(0, c(x)^2 * sgn(c(x)))</c>. This causes rapid increase in penalty the futher a point is outside the feasible region. Although
  /// it distorts the topography to a greater degree, for many problems, this gives faster and more accurate results than
  /// <see cref="LinearPenalty"/>.
  /// </summary>
  QuadraticPenalty,
  /// <summary>A penalty (exterior-point) method where the penalty equals the distance from the feasible region (e.g. <c>max(0, c(x))</c>).
  /// This causes only a steady increase in penalty outside the feasible region. This is usually inferior to
  /// <see cref="QuadraticPenalty"/>.
  /// </summary>
  LinearPenalty,
}
#endregion

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
  /// <summary>Finds a local minimum of multi-dimensional function near the given initial point, using a default error tolerance.</summary>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/BGFS/*[not(@name='tolerance')]"/>
  public static double BFGS(IDifferentiableMDFunction function, double[] x)
  {
    return BFGS(function, x, 1e-8); // NOTE: this constant is duplicated in ConstrainedMinimizer, so update it there if changed
  }

  /// <summary>Finds a local minimum of multi-dimensional function near the given initial point.</summary>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/BGFS/node()"/>
  public static double BFGS(IDifferentiableMDFunction function, double[] x, double tolerance)
  {
    if(function == null || x == null) throw new ArgumentNullException();
    if(function.Arity != x.Length) throw new ArgumentException("The dimensions of the initial point must match the function's arity.");
    if(tolerance < 0) throw new ArgumentOutOfRangeException();

    const int MaxIterations = 100;
    const double ScaledMaxStep = 100, ParameterTolerance = IEEE754.DoublePrecision*4;

    Matrix invHessian = new Matrix(x.Length, x.Length);
    double[] gradient = new double[x.Length], step = new double[x.Length], tmp = new double[x.Length], gradDiff = new double[x.Length];
    // occasionally, it stalls. if that happens, we'll try restarting. (we do two 100 iteration tries rather than one 200 iteration try.)
    for(int tries=0; tries<2; tries++)
    {
      invHessian.SetIdentity();
      double value = function.Evaluate(x), maxStep = Math.Max(MathHelpers.GetMagnitude(x), x.Length)*ScaledMaxStep;
      function.EvaluateGradient(x, gradient);
      MathHelpers.NegateVector(gradient, step); // the initial step is the opposite of the gradient (i.e. directly downhill)

      for(int iteration=0; iteration < MaxIterations; iteration++)
      {
        // move in the step direction as far as we can go. the new point is output in 'tmp'
        FindRoot.LineSearch(function, x, value, gradient, step, tmp, out value, maxStep);
        MathHelpers.SubtractVectors(tmp, x, step); // store the actual distance moved into 'step'
        ArrayUtility.FastCopy(tmp, x, x.Length); // update the current point

        // if the parameters are barely changing, then we've converged
        if(GetParameterConvergence(x, step) <= ParameterTolerance) return value;

        // evaluate the gradient at the new point. if the gradient is about zero, we're done
        ArrayUtility.FastCopy(gradient, gradDiff, gradient.Length); // copy the old gradient
        function.EvaluateGradient(x, gradient); // get the new gradient
        if(GetGradientConvergence(x, gradient, value) <= tolerance) return value; // check the new gradient for convergence to zero

        // compute the difference between the new and old gradients, and multiply the difference by the inverse hessian
        MathHelpers.SubtractVectors(gradient, gradDiff, gradDiff);
        MathHelpers.Multiply(invHessian, gradDiff, tmp);

        double fac = MathHelpers.DotProduct(gradDiff, step);
        double gdSqr = MathHelpers.SumSquaredVector(tmp), stepSqr = MathHelpers.SumSquaredVector(step);
        if(fac > Math.Sqrt(gdSqr*stepSqr*IEEE754.DoublePrecision)) // skip the hessian update if the vectors are nearly orthogonal
        {
          fac = 1 / fac;
          double fae = MathHelpers.DotProduct(gradDiff, tmp), fad = 1 / fae;
          for(int i=0; i<gradDiff.Length; i++) gradDiff[i] = fac*step[i] - fad*tmp[i];
          for(int i=0; i<step.Length; i++)
          {
            for(int j=i; j<step.Length; j++)
            {
              double element = invHessian[i, j] + fac*step[i]*step[j] - fad*tmp[i]*tmp[j] + fae*gradDiff[i]*gradDiff[j];
              invHessian[i, j] = element;
              invHessian[j, i] = element;
            }
          }
        }

        for(int i=0; i<step.Length; i++) step[i] = -MathHelpers.SumRowTimesVector(invHessian, i, gradient);
      }
    }

    throw MinimumNotFoundError();
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/BracketInward/node()"/>
  public static IEnumerable<MinimumBracket> BracketInward(IOneDimensionalFunction function, double x1, double x2, int segments)
  {
    if(function == null) throw new ArgumentNullException();
    return BracketInward(function.Evaluate, x1, x2, segments);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/BracketInward/node()"/>
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

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/BracketOutward/node()"/>
  public static bool BracketOutward(IOneDimensionalFunction function, double x1, double x2, out MinimumBracket bracket)
  {
    if(function == null) throw new ArgumentNullException();
    return BracketOutward(function.Evaluate, x1, x2, out bracket);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/BracketOutward/node()"/>
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

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[not(@name='tolerance') and not(@name='value')]"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/NDBrentRemarks/node()"/>
  public static double Brent(IOneDimensionalFunction function, MinimumBracket bracket)
  {
    if(function == null) throw new ArgumentNullException();
    return Brent(function.Evaluate, bracket);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[not(@name='tolerance')]"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/NDBrentRemarks/node()"/>
  public static double Brent(IOneDimensionalFunction function, MinimumBracket bracket, out double value)
  {
    if(function == null) throw new ArgumentNullException();
    return Brent(function.Evaluate, bracket, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[not(@name='value')]"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/NDBrentRemarks/node()"/>
  [CLSCompliant(false)]
  public static double Brent(IOneDimensionalFunction function, MinimumBracket bracket, double tolerance)
  {
    if(function == null) throw new ArgumentNullException();
    return Brent(function.Evaluate, bracket, tolerance);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/node()"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/NDBrentRemarks/node()"/>
  public static double Brent(IOneDimensionalFunction function, MinimumBracket bracket, double tolerance, out double value)
  {
    if(function == null) throw new ArgumentNullException();
    return Brent(function.Evaluate, bracket, tolerance, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[not(@name='tolerance') and not(@name='value')]"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/NDBrentRemarks/node()"/>
  public static double Brent(Func<double, double> function, MinimumBracket bracket)
  {
    double value;
    return Brent(function, bracket, IEEE754.SqrtDoublePrecision, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[not(@name='tolerance')]"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/NDBrentRemarks/node()"/>
  public static double Brent(Func<double, double> function, MinimumBracket bracket, out double value)
  {
    return Brent(function, bracket, IEEE754.SqrtDoublePrecision, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[not(@name='value')]"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/NDBrentRemarks/node()"/>
  [CLSCompliant(false)]
  public static double Brent(Func<double, double> function, MinimumBracket bracket, double tolerance)
  {
    double value;
    return Brent(function, bracket, tolerance, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/node()"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/NDBrentRemarks/node()"/>
  public static double Brent(Func<double, double> function, MinimumBracket bracket, double tolerance, out double value)
  {
    if(function == null) throw new ArgumentNullException();
    if(tolerance < 0) throw new ArgumentOutOfRangeException();

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
      // we're done when the distance between the brackets is less than or equal to minPt*tolerance*2 and minPt is centered in the bracket
      double mid = 0.5*(left+right);
      double tol1 = tolerance * Math.Abs(minPt) + (IEEE754.DoublePrecision*0.001); // prevent tol2 from being zero when minPt is zero
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

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[not(@name='tolerance') and not(@name='value')]"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/DBrentRemarks/node()"/>
  public static double Brent(IDifferentiableFunction function, MinimumBracket bracket)
  {
    double value;
    return Brent(function, bracket, IEEE754.SqrtDoublePrecision, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[not(@name='tolerance')]"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/DBrentRemarks/node()"/>
  public static double Brent(IDifferentiableFunction function, MinimumBracket bracket, out double value)
  {
    return Brent(function, bracket, IEEE754.SqrtDoublePrecision, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[not(@name='value')]"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/DBrentRemarks/node()"/>
  [CLSCompliant(false)]
  public static double Brent(IDifferentiableFunction function, MinimumBracket bracket, double tolerance)
  {
    double value;
    return Brent(function, bracket, tolerance, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/node()"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/DBrentRemarks/node()"/>
  public static double Brent(IDifferentiableFunction function, MinimumBracket bracket, double tolerance, out double value)
  {
    if(function == null) throw new ArgumentNullException();
    if(tolerance < 0) throw new ArgumentOutOfRangeException();

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
      // we're done when the distance between the brackets is less than or equal to minPt*tolerance*2 and minPt is centered in the bracket
      double mid = 0.5*(left+right);
      double tol1 = tolerance * Math.Abs(minPt) + (IEEE754.DoublePrecision*0.001); // prevent tol2 from being zero when minPt is zero
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

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[not(@name='tolerance') and not(@name='value')]"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/GoldenSectionRemarks/node()"/>
  public static double GoldenSection(IOneDimensionalFunction function, MinimumBracket bracket)
  {
    if(function == null) throw new ArgumentNullException();
    return GoldenSection(function.Evaluate, bracket);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[not(@name='tolerance')]"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/GoldenSectionRemarks/node()"/>
  public static double GoldenSection(IOneDimensionalFunction function, MinimumBracket bracket, out double value)
  {
    if(function == null) throw new ArgumentNullException();
    return GoldenSection(function.Evaluate, bracket, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[not(@name='value')]"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/GoldenSectionRemarks/node()"/>
  [CLSCompliant(false)]
  public static double GoldenSection(IOneDimensionalFunction function, MinimumBracket bracket, double tolerance)
  {
    if(function == null) throw new ArgumentNullException();
    return GoldenSection(function.Evaluate, bracket, tolerance);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/node()"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/GoldenSectionRemarks/node()"/>
  public static double GoldenSection(IOneDimensionalFunction function, MinimumBracket bracket, double tolerance, out double value)
  {
    if(function == null) throw new ArgumentNullException();
    return GoldenSection(function.Evaluate, bracket, tolerance, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[not(@name='tolerance') and not(@name='value')]"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/GoldenSectionRemarks/node()"/>
  public static double GoldenSection(Func<double, double> function, MinimumBracket bracket)
  {
    double value;
    return GoldenSection(function, bracket, IEEE754.SqrtDoublePrecision, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[not(@name='tolerance')]"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/GoldenSectionRemarks/node()"/>
  public static double GoldenSection(Func<double, double> function, MinimumBracket bracket, out double value)
  {
    return GoldenSection(function, bracket, IEEE754.SqrtDoublePrecision, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/*[not(@name='value')]"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/GoldenSectionRemarks/node()"/>
  [CLSCompliant(false)]
  public static double GoldenSection(Func<double, double> function, MinimumBracket bracket, double tolerance)
  {
    double value;
    return GoldenSection(function, bracket, tolerance, out value);
  }

  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/Minimize1D/node()"/>
  /// <include file="documentation.xml" path="/Math/Optimization/Minimize/GoldenSectionRemarks/node()"/>
  public static double GoldenSection(Func<double, double> function, MinimumBracket bracket, double tolerance, out double value)
  {
    if(function == null) throw new ArgumentNullException();
    if(tolerance < 0) throw new ArgumentOutOfRangeException();

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
    while(Math.Abs(x3-x0) > (Math.Abs(x1)+Math.Abs(x2))*tolerance) // while the bracket is still too large compared to the center values...
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

  internal static double GetParameterConvergence(double[] x, double[] step)
  {
    double maxValue = 0;
    for(int i=0; i<x.Length; i++)
    {
      double value = Math.Abs(step[i]) / Math.Max(Math.Abs(x[i]), 1);
      if(value > maxValue) maxValue = value;
    }
    return maxValue;
  }

  internal static MinimumNotFoundException MinimumNotFoundError()
  {
    throw new MinimumNotFoundException("No minimum found within the given interval and tolerance.");
  }

  static double GetGradientConvergence(double[] x, double[] gradient, double value)
  {
    double maxValue = 0, divisor = Math.Max(Math.Abs(value), 1);
    for(int i=0; i<x.Length; i++)
    {
      double component = Math.Abs(gradient[i]) * Math.Max(Math.Abs(x[i]), 1) / divisor;
      if(component > maxValue) maxValue = component;
    }
    return maxValue;
  }
}
#endregion

} // namespace AdamMil.Mathematics.Optimization