using System;
using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Mathematics.Tests
{
  [TestFixture]
  public class Polynomials
  {
    [Test]
    public void T01_Polynomials()
    {
      Polynomial poly = new Polynomial(1, -2, 3, -5, 7);

      Assert.AreEqual(4, poly.Degree);
      Assert.AreEqual(1,  poly[0]);
      Assert.AreEqual(-2, poly[1]);
      Assert.AreEqual(3,  poly[2]);
      Assert.AreEqual(-5, poly[3]);
      Assert.AreEqual(7,  poly[4]);

      Assert.AreEqual("7x^4 - 5x^3 + 3x^2 - 2x + 1", poly.ToString());

      Assert.AreEqual(96174, poly.Evaluate(11));
      double derivative;
      Assert.AreEqual(96174, poly.Evaluate(11, out derivative));
      Assert.AreEqual(35517, derivative);
      Assert.AreEqual(35517, poly.EvaluateDerivative(11, 1)); // 28x^3 - 15x^2 + 6x - 2
      Assert.AreEqual(9840, poly.EvaluateDerivative(11, 2)); // 84x^2 - 30x + 6
      Assert.AreEqual(1818, poly.EvaluateDerivative(11, 3)); // 168x - 30
      Assert.AreEqual(168, poly.EvaluateDerivative(11, 4)); // 168
      Assert.AreEqual(0, poly.EvaluateDerivative(11, 5)); // 0

      Assert.AreEqual(new Polynomial(-2, 6, -15, 28), poly.GetDerivative());
      Assert.AreEqual(new Polynomial(6, -30, 84), poly.GetDerivative(2));
      Assert.AreEqual(new Polynomial(-30, 168), poly.GetDerivative(3));
      Assert.AreEqual(new Polynomial(168), poly.GetDerivative(4));
      Assert.AreEqual(new Polynomial(0), poly.GetDerivative(5));

      double[] derivs = new double[6];
      Assert.AreEqual(96174, poly.EvaluateDerivatives(11, derivs, 0, 0)); // ensure that we can safely use a length of zero
      Assert.AreEqual(96174, poly.EvaluateDerivatives(11, derivs, 1, derivs.Length-1));
      Assert.AreEqual(35517, derivs[1]);
      Assert.AreEqual(9840,  derivs[2]);
      Assert.AreEqual(1818,  derivs[3]);
      Assert.AreEqual(168,   derivs[4]);
      Assert.AreEqual(0,     derivs[5]);

      double[] coeffs = new double[6];
      poly.CopyTo(coeffs, 1);
      Assert.AreEqual(1,  coeffs[1]);
      Assert.AreEqual(-2, coeffs[2]);
      Assert.AreEqual(3,  coeffs[3]);
      Assert.AreEqual(-5, coeffs[4]);
      Assert.AreEqual(7,  coeffs[5]);

      Assert.AreEqual(new Polynomial(11, 18, 33, -5, 7), poly + new Polynomial(10, 20, 30));
      Assert.AreEqual(new Polynomial(-9, -22, -27, -5, 7), poly - new Polynomial(10, 20, 30));
      Assert.AreEqual(new Polynomial(-1, 2, -3, 5, -7), -poly);
      Assert.AreEqual(new Polynomial(2, -4, 6, -10, 14), poly*2);
      Assert.AreEqual(new Polynomial(2, -4, 6, -10, 14), poly/0.5);
      Assert.AreEqual(new Polynomial(11, -2, 3, -5, 7), poly+10);
      Assert.AreEqual(new Polynomial(11, -2, 3, -5, 7), 10+poly);
      Assert.AreEqual(new Polynomial(-9, -2, 3, -5, 7), poly-10);
      Assert.AreEqual(new Polynomial(9, 2, -3, 5, -7), 10-poly);

      // (7x^4 - 5x^3 + 3x^2 - 2x + 1) * (2x^2 + 3x - 5) = 14x^6 + 11x^5 - 44x^4 + 30x^3 - 19x^2 + 13x - 5
      Assert.AreEqual(new Polynomial(-5, 13, -19, 30, -44, 11, 14), poly * new Polynomial(-5, 3, 2));

      // (7x^4 - 5x^3 + 3x^2 - 2x + 1) / (2x^2 + 3x - 5) = 3.5x^2 - 7.75x + 21.875 with remainder -106.375x + 110.375
      Polynomial remainder;
      Assert.AreEqual(new Polynomial(21.875, -7.75, 3.5), Polynomial.Divide(poly, new Polynomial(-5, 3, 2), out remainder));
      Assert.AreEqual(new Polynomial(110.375, -106.375), remainder);
      Assert.AreEqual(new Polynomial(21.875, -7.75, 3.5), Polynomial.Divide(poly, new Polynomial(-5, 3, 2)));

      // check that we can set coefficients outside the current size of the polynomial
      poly[10] = 42;
      Assert.AreEqual(10, poly.Degree);
      Assert.AreEqual("42x^10 + 7x^4 - 5x^3 + 3x^2 - 2x + 1", poly.ToString());
      poly[10] = 0; // check that the degree decreases when we set the higher coefficients to zero
      Assert.AreEqual(4, poly.Degree);
      Assert.AreEqual(new Polynomial(1, -2, 3, -5, 7), poly);
    }
  }
}