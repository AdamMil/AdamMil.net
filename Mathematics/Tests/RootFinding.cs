using System;
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
      Assert.AreEqual(1, FindRoot.UnboundedNewtonRaphson(function, new RootInterval(-10, 10)), Accuracy);
      // the others need the root to be at one of the boundaries, although this is a trivial case for any method. make sure it works from
      // both edges for all methods
      Assert.AreEqual(1, FindRoot.BoundedNewtonRaphson(function, new RootInterval(1, 10)), Accuracy);
      Assert.AreEqual(1, FindRoot.BoundedNewtonRaphson(function, new RootInterval(-10, 1)), Accuracy);
      Assert.AreEqual(1, FindRoot.Brent(function, new RootInterval(1, 10)), Accuracy);
      Assert.AreEqual(1, FindRoot.Brent(function, new RootInterval(-10, 1)), Accuracy);
      Assert.AreEqual(1, FindRoot.Subdivide(function, new RootInterval(1, 10)), Accuracy);
      Assert.AreEqual(1, FindRoot.Subdivide(function, new RootInterval(-10, 1)), Accuracy);

      // this is a parabola with roots at x=0 and x=2. since it crosses zero, it should be amenable to many different methods
      function = new DifferentiableFunction(x => (x-1)*(x-1) - 1, x => 2*x-2); // f(x) = (x-1)^2 - 1

      // first, let's try some root bracketing
      RootInterval interval = new RootInterval(0.5, 1.5);
      // bracket outwards
      Assert.IsTrue(FindRoot.BracketOutward(function, ref interval));
      Assert.IsTrue(interval.Min <= 0 && interval.Max >= 0 || interval.Min <= 2 && interval.Max >= 2); // make sure it brackets a root
      // bracket inwards. since interval, when divided into 20 pieces, will have the roots exactly on the boundaries, the sub intervals
      // should also (although that's not something we need to verify)
      interval = new RootInterval(-10, 10);
      bool foundZero = false, foundTwo = false;
      foreach(RootInterval sub in FindRoot.BracketInward(function, interval, 20))
      {
        if(sub.Min <= 0 && sub.Max >= 0) foundZero = true;
        if(sub.Min <= 2 && sub.Max >= 2) foundTwo  = true;
        Assert.IsTrue(sub.Min <= 0 && sub.Max >= 0 || sub.Min <= 2 && sub.Max >= 2);
      }
      Assert.IsTrue(foundZero && foundTwo);

      // try again, using an interval that doesn't divide evenly (and therefore won't provide cases that are trivial to solve)
      interval = new RootInterval(-8, 9);
      foundZero = foundTwo = false;
      foreach(RootInterval sub in FindRoot.BracketInward(function, interval, 20))
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
      interval = new RootInterval(-1, 1);
      TestHelpers.TestException<RootNotFoundException>(delegate { FindRoot.UnboundedNewtonRaphson(function, interval); });
      // ensure that the others complain about the root not being bracketed
      TestHelpers.TestException<ArgumentException>(delegate { FindRoot.BoundedNewtonRaphson(function, interval); });
      TestHelpers.TestException<ArgumentException>(delegate { FindRoot.Brent(function, interval); });
      TestHelpers.TestException<ArgumentException>(delegate { FindRoot.Subdivide(function, interval); });
      // ensure that bracketing fails as it should
      Assert.IsFalse(FindRoot.BracketOutward(function, ref interval));
      Assert.AreEqual(0, FindRoot.BracketInward(function, new RootInterval(-10, 10), 20).Count);
    }

    [Test]
    public void T02_Multidimensional()
    {
      // TODO: after we implement singular value decomposition, test with functions that would fail due to having an infinite number of
      // roots, like x^2 + y^2 = 10 and x+y = 0
      TwoDimensionalFunction function = new TwoDimensionalFunction();
      double[] point = new double[2] { 1, 1 }; // using 0,0 tends to cause a SingularMatrixException
      // check that the first call finds the root with moderate accuracy
      Assert.IsTrue(FindRoot.GlobalNewton(function, point));
      double quality = point[0]*point[0] + (point[1]-4)*(point[1]-4);
      Assert.AreEqual(0, quality, 1e-8);
      // check that subsequent calls can improve the root
      while(true)
      {
        double x=point[0], y=point[1];
        Assert.IsTrue(FindRoot.GlobalNewton(function, point));
        if(point[0] == x && point[1] == y) break; // if it stops changing, we're done
      }
      quality = point[0]*point[0] + (point[1]-4)*(point[1]-4);
      Assert.AreEqual(0, quality, 1e-15);

      // now try computing the Jacobian via finite differences rather than directly
      point = new double[2] { 1, 1 };
      Assert.IsTrue(FindRoot.GlobalNewton(new ApproximatelyDifferentiableVVFunction(function), point));
      quality = point[0]*point[0] + (point[1]-4)*(point[1]-4);
      Assert.AreEqual(0, quality, 1.5e-8);
      // unfortunately, we can't call it again to improve it due to errors caused by excessive roundoff error
    }

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
  }
}