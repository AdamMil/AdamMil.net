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
    public void T01_BasicMinimization()
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
  }
}