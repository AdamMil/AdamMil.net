using System;
using System.Collections.Generic;
using System.Linq;
using AdamMil.Mathematics.Optimization;
using NUnit.Framework;

namespace AdamMil.Mathematics.Tests
{
  [TestFixture]
  public class Optimization
  {
    [Test]
    public void T01_OneDimensional()
    {
      const double Accuracy = 1.5e-7;

      // sin(x+1) + x/2 has local minima at -2/3PI-1, 4/3PI-1, 10/3PI-1, etc.
      DifferentiableFunction function = new DifferentiableFunction(x => Math.Sin(x+1) + x/2, x => Math.Cos(x+1) + 0.5);
      Func<double, double> ndFunction = function.Evaluate; // create a version without derivative information
      // the three minima above are located between -5 and 13, so we should be able to find them with BracketInward()
      List<MinimumBracket> brackets = Minimize.BracketInward(function, -5, 13, 3).ToList();
      Assert.AreEqual(3, brackets.Count); // ensure we found them all
      List<double> minima = new List<double>();
      // for each bracket, try to find it using all available methods
      foreach(MinimumBracket bracket in brackets)
      {
        double x = Minimize.GoldenSection(function, bracket); // first use golden section search, which is the most reliable
        Assert.AreEqual(x, Minimize.Brent(function, bracket), Accuracy); // then make sure Brent's method gives a similar answer, both with
        Assert.AreEqual(x, Minimize.Brent(ndFunction, bracket), Accuracy); // and without the derivative
        minima.Add(x);
      }
      minima.Sort(); // then sort the results to put them in a known order and make sure they're equal to the expected values
      Assert.AreEqual(3, minima.Count);
      Assert.AreEqual(Math.PI*-2/3-1, minima[0], Accuracy);
      Assert.AreEqual(Math.PI*4/3-1,  minima[1], Accuracy);
      Assert.AreEqual(Math.PI*10/3-1, minima[2], Accuracy);

      // now test BracketOutward
      MinimumBracket b;
      Assert.IsFalse(Minimize.BracketOutward(x => x, 0, 1, out b)); // make sure it fails with functions that have no minimum
      Assert.IsTrue(Minimize.BracketOutward(x => 5, 0, 1, out b)); // but succeeds with constant functions
      Assert.IsTrue(Minimize.BracketOutward(function, 0, 1, out b)); // and with our sample function
      // make sure it searches in a downhill direction, as designed
      Assert.AreEqual(Math.PI*-2/3-1, Minimize.GoldenSection(function, b), Accuracy);
      Assert.IsTrue(Minimize.BracketOutward(function, 1, 2, out b));
      Assert.AreEqual(Math.PI*4/3-1, Minimize.GoldenSection(function, b), Accuracy);

      // try a function with a singularity, for kicks
      ndFunction = x => Math.Cos(x)/(x-1);
      Assert.AreEqual(1, Minimize.GoldenSection(ndFunction, new MinimumBracket(-1, -0.1, 1)), Accuracy);
      Assert.AreEqual(1, Minimize.Brent(ndFunction, new MinimumBracket(-1, -0.1, 1)), Accuracy);
    }

    [Test]
    public void T02_Multidimensional()
    {
      // test using the rosenbrock banana function, which has a long, curved, narrow valley
      RosenbrockBanana function = new RosenbrockBanana();
      double[] point = new double[2] { -1.2, 2 };
      double value = Minimize.BFGS(function, point);
      Assert.AreEqual(1, point[0], 2.4e-10);
      Assert.AreEqual(1, point[0], 2.4e-10);
      Assert.AreEqual(0, value, 6.4e-20);
    }

    [Test]
    public void T03_Constrained()
    {
      // a constrained minimization problem using a barrier function to minimize x^2 subject to x > 5
      MinimumBracket bracket = Minimize.BracketInward(new BarrierFunction(1), 5 + (5.0/2)*IEEE754.DoublePrecision, 100, 100).First();
      double value;
      Assert.AreEqual(5, Minimize.Brent(new BarrierFunction(1e-10), bracket, out value), 1.2e-7);
      Assert.AreEqual(25, value, 1.2e-6);

      // test a constrained minimization problem using various methods
      ConstrainedMinimizer minimizer;
      double[] point;
      foreach(ConstraintEnforcement method in (ConstraintEnforcement[])Enum.GetValues(typeof(ConstraintEnforcement)))
      {
        // test the same problem using the ConstrainedMinimizer
        point = new double[2] { 5, 8 };
        minimizer = new ConstrainedMinimizer(new ConstraintTestFunction()) { ConstraintEnforcement = method };
        minimizer.SetBounds(0, 1, double.PositiveInfinity);
        minimizer.SetBounds(1, 3, 9);
        value = minimizer.Minimize(point);
        switch(method)
        {
          case ConstraintEnforcement.InverseBarrier: // ~ 220 function calls and 180 gradient evaluations
            Assert.AreEqual(9, value, 1.4e-8);
            Assert.AreEqual(1, point[0], 2.4e-10);
            Assert.AreEqual(3, point[1], 4.1e-10);
            break;
          case ConstraintEnforcement.LinearPenalty: // ~ 870 function calls and 120 gradient evaluations
            Assert.AreEqual(9, value, 4.4e-11);
            Assert.AreEqual(1, point[0], 2.5e-12);
            Assert.AreEqual(3, point[1], 2.5e-12);
            break;
          case ConstraintEnforcement.LogBarrier: // ~ 140 function calls and 130 gradient evaluations
            Assert.AreEqual(9, value, 5.1e-9);
            Assert.AreEqual(1, point[0], 6e-12);
            Assert.AreEqual(3, point[1], 1.7e-11);
            break;
          case ConstraintEnforcement.QuadraticPenalty: // ~ 670 function calls and 170 gradient evaluations
            Assert.AreEqual(9, value, 9e-9);
            Assert.AreEqual(1, point[0], 9e-10);
            Assert.AreEqual(3, point[1], 4e-10);
            break;
          default: throw new NotImplementedException();
        }
      }

      // test the constrained minimizer with a tricky problem -- finding the minimum of the rosenbrock banana constrained to an
      // arbitrary line that doesn't pass through the minimum of the original problem
      minimizer = new ConstrainedMinimizer(new RosenbrockBanana());
      minimizer.AddConstraint(new LineConstraint(-1.5, -1.5, 0, 1.5, 0));
      point = new double[2] { -1.2, 2 };
      value = minimizer.Minimize(point);
      Assert.AreEqual(2.4975, value, 2e-9);
      Assert.AreEqual(-0.57955689989313142185, point[0], 8e-10);
      Assert.AreEqual(0.34088620021373715629, point[1], 1e-9);
    }

    #region BarrierFunction1D
    /// <summary>Implements the function <c>f(x) = x^2</c> subject to the constraint that x &gt; 5.</summary>
    sealed class BarrierFunction : IDifferentiableFunction
    {
      public BarrierFunction(double logF)
      {
        this.logF = logF;
      }

      public int Arity
      {
        get { return 2; }
      }

      public int DerivativeCount
      {
        get { return 1; }
      }

      public double EvaluateDerivative(double x, int derivative)
      {
        return 2*x - logF/(x-5);
      }

      public double Evaluate(double x)
      {
        double barrier = logF * Math.Log(x-5);
        return double.IsNaN(barrier) ? double.PositiveInfinity : x*x - barrier;
      }

      readonly double logF;
    }
    #endregion

    #region ConstraintTestFunction
    /// <summary>Implements the function f(x,y) = x^2 * y^2</summary>
    sealed class ConstraintTestFunction : IDifferentiableMDFunction
    {
      public int Arity
      {
        get { return 2; }
      }

      public double Evaluate(double[] x)
      {
        return x[0]*x[0]*x[1]*x[1];
      }

      public void EvaluateGradient(double[] x, double[] gradient)
      {
        double a = x[0], b = x[1], ab2 = 2*a*b;
        gradient[0] = ab2 * b;
        gradient[1] = ab2 * a;
      }
    }
    #endregion

    #region LineConstraint
    sealed class LineConstraint : IDifferentiableMDFunction
    {
      public LineConstraint(double x1, double y1, double x2, double y2, double distance)
      {
        xs = x1;
        ys = y1;
        xd = x2 - x1;
        yd = y2 - y1;
        double len = Math.Sqrt(xd*xd + yd*yd);
        xd /= len;
        yd /= len;
        this.distance = distance;
      }

      public int Arity
      {
        get { return 2; }
      }


      public double Evaluate(double[] input)
      {
        double x = input[0], y = input[1];
        return Math.Abs(xd*(input[1]-ys) - yd*(input[0]-xs)) - distance;
      }

      public void EvaluateGradient(double[] input, double[] gradient)
      {
        double sdist = xd*(input[1]-ys) - yd*(input[0]-xs), nerror = Math.Abs(sdist) - distance;
        if(nerror > 0)
        {
          if(sdist < 0)
          {
            gradient[0] = yd;
            gradient[1] = -xd;
          }
          else
          {
            gradient[0] = -yd;
            gradient[1] = xd;
          }
        }
        else
        {
          gradient[0] = 0;
          gradient[1] = 0;
        }
      }

      readonly double xs, ys, xd, yd, distance;
    }
    #endregion

    #region RosenbrockBanana
    /// <summary>Implements the Rosenbrock banana function <c>f(x,y) = 100*(y-x^2)^2 + (1-x^2)^2</c>.</summary>
    sealed class RosenbrockBanana : IDifferentiableMDFunction
    {
      public int Arity
      {
        get { return 2; }
      }

      public double Evaluate(params double[] x)
      {
        double a = x[0], b = x[1], aa = a*a;
        return 100*(b-aa)*(b-aa) + (1-a)*(1-a);
      }

      public void EvaluateGradient(double[] x, double[] gradient)
      {
        double a = x[0], b = x[1], aa = a*a, v = 200*(b-aa);
        gradient[0] = 2*((a-1) - v*a);
        gradient[1] = v;
      }
    }
    #endregion
  }
}