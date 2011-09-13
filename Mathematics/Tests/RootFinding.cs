using System;
using System.Linq;
using AdamMil.Mathematics.RootFinding;
using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Mathematics.Tests
{
  [TestFixture]
  public class RootFinding
  {
    [Test]
    public void T01_OneDimensional()
    {
      // this is pretty close to the minimum that i can make it while still passing, given the original implementation. if an
      // implementation degrades substantially (with these functions, anyway), this should catch it
      const double Accuracy = 1.77636e-15;

      // this is a simple parabola with a double root at x=1. because of the double root, which means the function never crosses zero, only
      // touches it, many methods have more trouble with it. in particular, only unbounded newton raphson is able to find it without having
      // the root at one of the interval boundaries
      DifferentiableFunction function = new DifferentiableFunction(x => (x-1)*(x-1), x => 2*x-2); // f(x) = (x-1)^2

      // test unbounded newton raphson with a wide interval
      Assert.AreEqual(1, FindRoot.UnboundedNewtonRaphson(function, new RootBracket(-10, 10)), Accuracy);
      // the others need the root to be at one of the boundaries, although this is a trivial case for any method. make sure it works from
      // both edges for all methods
      Assert.AreEqual(1, FindRoot.BoundedNewtonRaphson(function, new RootBracket(1, 10)), Accuracy);
      Assert.AreEqual(1, FindRoot.BoundedNewtonRaphson(function, new RootBracket(-10, 1)), Accuracy);
      Assert.AreEqual(1, FindRoot.Brent(function, new RootBracket(1, 10)), Accuracy);
      Assert.AreEqual(1, FindRoot.Brent(function, new RootBracket(-10, 1)), Accuracy);
      Assert.AreEqual(1, FindRoot.Subdivide(function, new RootBracket(1, 10)), Accuracy);
      Assert.AreEqual(1, FindRoot.Subdivide(function, new RootBracket(-10, 1)), Accuracy);

      // this is a parabola with roots at x=0 and x=2. since it crosses zero, it should be amenable to many different methods
      function = new DifferentiableFunction(x => (x-1)*(x-1) - 1, x => 2*x-2); // f(x) = (x-1)^2 - 1

      // first, let's try some root bracketing
      RootBracket interval = new RootBracket(0.5, 1.5);
      // bracket outwards
      Assert.IsTrue(FindRoot.BracketOutward(function, ref interval));
      Assert.IsTrue(interval.Min <= 0 && interval.Max >= 0 || interval.Min <= 2 && interval.Max >= 2); // make sure it brackets a root
      // bracket inwards. since interval, when divided into 20 pieces, will have the roots exactly on the boundaries, the sub intervals
      // should also (although that's not something we need to verify)
      interval = new RootBracket(-10, 10);
      bool foundZero = false, foundTwo = false;
      foreach(RootBracket sub in FindRoot.BracketInward(function, interval, 20))
      {
        if(sub.Min <= 0 && sub.Max >= 0) foundZero = true;
        if(sub.Min <= 2 && sub.Max >= 2) foundTwo  = true;
        Assert.IsTrue(sub.Min <= 0 && sub.Max >= 0 || sub.Min <= 2 && sub.Max >= 2);
      }
      Assert.IsTrue(foundZero && foundTwo);

      // try again, using an interval that doesn't divide evenly (and therefore won't provide cases that are trivial to solve)
      interval = new RootBracket(-8, 9);
      foundZero = foundTwo = false;
      foreach(RootBracket sub in FindRoot.BracketInward(function, interval, 20))
      {
        double root = -1;
        if(sub.Min <= 0 && sub.Max >= 0)
        {
          foundZero = true;
          root = 0;
        }
        else if(sub.Min <= 2 && sub.Max >= 2)
        {
          foundTwo = true;
          root = 2;
        }
        else Assert.Fail();

        // ensure that all methods find the root
        Assert.AreEqual(root, FindRoot.BoundedNewtonRaphson(function, sub), Accuracy);
        Assert.AreEqual(root, FindRoot.Brent(function, sub), Accuracy);
        Assert.AreEqual(root, FindRoot.Subdivide(function, sub), Accuracy);
        Assert.AreEqual(root, FindRoot.UnboundedNewtonRaphson(function, sub), Accuracy);
      }
      Assert.IsTrue(foundZero && foundTwo);

      // ensure that unbounded newton-raphson fails properly when there's no root
      function = new DifferentiableFunction(x => x*x+1, x => 2*x); // f(x) = x^2+1, a parabola with no root
      interval = new RootBracket(-1, 1);
      TestHelpers.TestException<RootNotFoundException>(delegate { FindRoot.UnboundedNewtonRaphson(function, interval); });
      // ensure that the others complain about the root not being bracketed
      TestHelpers.TestException<ArgumentException>(delegate { FindRoot.BoundedNewtonRaphson(function, interval); });
      TestHelpers.TestException<ArgumentException>(delegate { FindRoot.Brent(function, interval); });
      TestHelpers.TestException<ArgumentException>(delegate { FindRoot.Subdivide(function, interval); });
      // ensure that bracketing fails as it should
      Assert.IsFalse(FindRoot.BracketOutward(function, ref interval));
      Assert.AreEqual(0, FindRoot.BracketInward(function, new RootBracket(-10, 10), 20).Count());
    }

    [Test]
    public void T02_Multidimensional()
    {
      IDifferentiableVVFunction function = new TwoDimensionalFunction();
      Test(FindRoot.GlobalNewton, function, 1, 1, 0, 4, 1e-15);
      Test(FindRoot.Broyden, function, 1, 1, 0, 4, 1e-15);

      // now try computing the Jacobian via finite differences rather than directly
      Test(FindRoot.GlobalNewton, (IVectorValuedFunction)function, 1, 1, 0, 4, 1.5e-8);
      Test(FindRoot.Broyden, (IVectorValuedFunction)function, 1, 1, 0, 4, 4e-9);

      // now try a function that has multiple roots, x^2 + y^2 = 10 and x+y = 0
      function = new MultiRootTwoDimensionalFunction();
      // the initial point has to be in a correct quadrant (x positive and y negative, or vice versa) or it will fail to find a root
      double[] point = new double[2] { 0.1, -1 };
      Assert.IsTrue(FindRoot.GlobalNewton(function, point));
      Assert.AreEqual(Math.Sqrt(5), point[0], 1e-15);
      Assert.AreEqual(-Math.Sqrt(5), point[1], 1e-15);

      point = new double[2] { 0.1, -1 };
      Assert.IsTrue(FindRoot.Broyden(function, point));
      Assert.AreEqual(Math.Sqrt(5), point[0], 1.5e-11);
      Assert.AreEqual(-Math.Sqrt(5), point[1], 1.5e-11);
    }

    static void Test(Func<IDifferentiableVVFunction,double[],bool> finder, IDifferentiableVVFunction function,
                     double initialX, double initialY, double desiredX, double desiredY, double finalAccuracy)
    {
      double[] point = new double[2] { initialX, initialY };
      Assert.IsTrue(finder(function, point));
      double quality = (point[0]-desiredX)*(point[0]-desiredX) + (point[1]-desiredY)*(point[1]-desiredY);
      Assert.AreEqual(0, quality, 1e-8);
      // check that subsequent calls can improve the root
      while(true)
      {
        double x=point[0], y=point[1];
        Assert.IsTrue(finder(function, point));
        if(point[0] == x && point[1] == y) break; // if it stops changing, we're done
      }
      quality = (point[0]-desiredX)*(point[0]-desiredX) + (point[1]-desiredY)*(point[1]-desiredY);
      Assert.AreEqual(0, quality, finalAccuracy);
    }

    static void Test(Func<IDifferentiableVVFunction, double[], bool> finder, IVectorValuedFunction function,
                     double initialX, double initialY, double desiredX, double desiredY, double accuracy)
    {
      double[] point = new double[2] { initialX, initialY };
      Assert.IsTrue(finder(new ApproximatelyDifferentiableVVFunction(function), point));
      double quality = (point[0]-desiredX)*(point[0]-desiredX) + (point[1]-desiredY)*(point[1]-desiredY);
      Assert.AreEqual(0, quality, accuracy);
    }

    #region TwoDimensionalFunction
    sealed class TwoDimensionalFunction : IDifferentiableVVFunction
    {
      public int InputArity
      {
        get { return 2; }
      }

      public int OutputArity
      {
        get { return 2; }
      }

      public void Evaluate(double[] input, double[] output)
      {
        double x = input[0], y = input[1];
        output[0] = x*x + y*y - 16;
        output[1] = x*x + (y-2)*(y-2) - 4;
      }

      public void EvaluateJacobian(double[] input, Matrix matrix)
      {
        double x = input[0], y = input[1];
        matrix[0, 0] = 2*x;
        matrix[0, 1] = 2*y;
        matrix[1, 0] = 2*x;
        matrix[1, 1] = 2*y-4;
      }
    }
    #endregion

    #region MultiRootTwoDimensionalFunction
    sealed class MultiRootTwoDimensionalFunction : IDifferentiableVVFunction
    {
      public int InputArity
      {
        get { return 2; }
      }

      public int OutputArity
      {
        get { return 2; }
      }

      public void Evaluate(double[] input, double[] output)
      {
        double x = input[0], y = input[1];
        output[0] = x*x + y*y - 10;
        output[1] = x + y;
      }

      public void EvaluateJacobian(double[] input, Matrix matrix)
      {
        double x = input[0], y = input[1];
        matrix[0, 0] = 2*x;
        matrix[0, 1] = 2*y;
        matrix[1, 0] = 1;
        matrix[1, 1] = 1;
      }
    }
    #endregion
  }
}