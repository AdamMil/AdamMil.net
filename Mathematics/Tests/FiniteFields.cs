using System;
using System.Collections.Generic;
using AdamMil.Mathematics.Fields;
using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Mathematics.Tests
{
  [TestFixture]
  public class FiniteFields
  {
    [Test]
    public void T01_GaloisField()
    {
      TestHelpers.TestException<ArgumentOutOfRangeException>("must be from 1 to 31", () => new GF2pField(0));
      TestHelpers.TestException<ArgumentOutOfRangeException>("must be from 1 to 31", () => new GF2pField(32));
      TestHelpers.TestException<ArgumentOutOfRangeException>("at least 2^power", () => new GF2pField(5, -1));
      TestHelpers.TestException<ArgumentOutOfRangeException>("at least 2^power", () => new GF2pField(5, 31));
      TestHelpers.TestException<ArgumentOutOfRangeException>("within the field", () => new GF2pField(5, 37, 32));
      new GF2pField(5, 32, 31);
      TestHelpers.TestException<ArgumentOutOfRangeException>("at least 2", () => new GF2pField(2, 7, 1));
      new GF2pField(1, 3, 1);
      for(int power = 1; power < 10; power++) TestFieldBasics(power);

      // test squaring with a larger field
      GF2pField big = new GF2pField(24);
      Assert.AreEqual(big.Multiply(234567, 234567), big.Square(234567));
    }

    [Test]
    public void T02_PolynomialBasics()
    {
      GF2pField field = new GF2pField(8);
      TestHelpers.TestException<ArgumentNullException>(() => new GF2pPolynomial(null, 0));
      TestHelpers.TestException<ArgumentNullException>(() => new GF2pPolynomial(null, new int[0]));
      TestHelpers.TestException<ArgumentOutOfRangeException>("within the field", () => new GF2pPolynomial(field, -1));
      TestHelpers.TestException<ArgumentOutOfRangeException>("within the field", () => new GF2pPolynomial(field, 256));
      TestHelpers.TestException<ArgumentOutOfRangeException>("within the field", () => new GF2pPolynomial(field, new[] { -1 }));
      TestHelpers.TestException<ArgumentOutOfRangeException>("within the field", () => new GF2pPolynomial(field, new[] { 256 }));

      GF2pPolynomial zero = new GF2pPolynomial(field, 0);
      GF2pPolynomial p = new GF2pPolynomial(field, new[] { 0, 1, 254, 255, 0, 0 });
      Assert.AreEqual(0, p[0]); // test the indexer
      Assert.AreEqual(1, p[1]);
      Assert.AreEqual(254, p[2]);
      Assert.AreEqual(255, p[3]);

      Assert.AreEqual(3, p.Degree); // test various properties
      Assert.AreEqual(-1, zero.Degree);
      Assert.AreEqual(field, zero.Field);
      Assert.AreEqual(field, p.Field);
      Assert.IsTrue(zero.IsZero);
      Assert.IsFalse(p.IsZero);
      Assert.AreEqual(0, zero.Length);
      Assert.AreEqual(4, p.Length);

      Assert.IsTrue(p.Equals((object)(new GF2pPolynomial(field, 0, 1) + new GF2pPolynomial(field, 0, 0, 254, 255)))); // test Equals
      Assert.IsTrue(p.Equals(new GF2pPolynomial(new GF2pField(8), 0, 1, 254, 255))); // check that the field instance can be different
      Assert.IsFalse(p.Equals(new GF2pPolynomial(new GF2pField(8, 333), 0, 1, 254, 255))); // but it must be compatible
      Assert.IsFalse(p.Equals(new GF2pPolynomial(field, 0, 1, 3, 4)));
      Assert.IsTrue(p.Equals(p));
      Assert.IsTrue(zero.Equals(zero));
      Assert.IsTrue(zero.Equals(default(GF2pPolynomial)));
      Assert.IsFalse(p.Equals(zero));
      Assert.IsFalse(zero.Equals(p));
      Assert.AreNotEqual(zero.GetHashCode(), p.GetHashCode()); // test GetHashCode. mostly just make sure it doesn't throw
      Assert.IsTrue(p == new GF2pPolynomial(field, 0, 1, 254, 255));
      Assert.IsFalse(p != new GF2pPolynomial(field, 0, 1, 254, 255));
      Assert.IsFalse(p == zero);
      Assert.IsTrue(p != zero);

      TestHelpers.AssertArrayEquals(zero.ToArray(), new int[0]); // test ToArray
      TestHelpers.AssertArrayEquals(p.ToArray(), 0, 1, 254, 255);

      Assert.AreEqual("0", zero.ToString()); // test ToString and Parse
      Assert.AreEqual("17 + 2x", new GF2pPolynomial(field, 17, 2).ToString());
      Assert.AreEqual("x + 254x^2 + 255x^3", p.ToString());
      Assert.AreEqual(zero, GF2pPolynomial.Parse(field, "-0"));
      Assert.AreEqual(p, GF2pPolynomial.Parse(field, "+165x^3- 90 x^3 + x-254x^2"));

      Assert.AreEqual(p, p.Truncate(10)); // test Truncate
      Assert.AreEqual(p, p.Truncate(p.Length));
      Assert.AreEqual(new GF2pPolynomial(field, 0, 1, 254), p.Truncate(3));
      Assert.AreEqual(new GF2pPolynomial(field, 0, 1), p.Truncate(2));
      Assert.AreEqual(new GF2pPolynomial(field, 0), p.Truncate(1));
      Assert.AreEqual(new GF2pPolynomial(field, 0), new GF2pPolynomial(field, 1, 2).Truncate(0));
    }

    [Test]
    public void T03_PolynomialAddition()
    {
      GF2pField field = new GF2pField(8);
      GF2pPolynomial zero = new GF2pPolynomial(field, 0);
      GF2pPolynomial a = new GF2pPolynomial(field, 0, 1, 2, 3), b = new GF2pPolynomial(field, 5, 6, 7, 8, 9, 10);
      Assert.AreEqual(new GF2pPolynomial(field, 5^0, 6^1, 7^2, 8^3, 9, 10), a + b);
      Assert.AreEqual(new GF2pPolynomial(field, 5^0, 6^1, 7^2, 8^3, 9, 10), b + a);
      Assert.AreEqual(new GF2pPolynomial(field, 5^0, 6^1, 7^2, 8^3, 9, 10), a - b);
      Assert.AreEqual(new GF2pPolynomial(field, 5^0, 6^1, 7^2, 8^3, 9, 10), b - a);
      Assert.AreEqual(a, (a + default(GF2pPolynomial)));
      Assert.AreEqual(b, (default(GF2pPolynomial) + b));
      Assert.AreEqual(a.Field, (a+b).Field);
    }

    [Test]
    public void T04_PolynomialShifting()
    {
      GF2pField field = new GF2pField(8);
      GF2pPolynomial p = new GF2pPolynomial(field, 0, 1, 0, 2, 0, 3);

      Assert.AreEqual(new GF2pPolynomial(field, 1, 0, 2, 0, 3), p >> 1);
      Assert.AreEqual(new GF2pPolynomial(field, 0, 2, 0, 3), p >> 2);
      Assert.AreEqual(new GF2pPolynomial(field, 2, 0, 3), p >> 3);
      Assert.AreEqual(new GF2pPolynomial(field, 0), p >> 6);
      Assert.AreEqual(new GF2pPolynomial(field, 0), p >> 8);

      Assert.AreEqual(new GF2pPolynomial(field, 1, 0, 2, 0, 3), p << -1);
      Assert.AreEqual(new GF2pPolynomial(field, 0, 2, 0, 3), p << -2);
      Assert.AreEqual(new GF2pPolynomial(field, 2, 0, 3), p << -3);
      Assert.AreEqual(new GF2pPolynomial(field, 0), p << -6);
      Assert.AreEqual(new GF2pPolynomial(field, 0), p << -8);

      Assert.AreEqual(new GF2pPolynomial(field, 0, 0, 1, 0, 2, 0, 3), p << 1);
      Assert.AreEqual(new GF2pPolynomial(field, 0, 0, 0, 1, 0, 2, 0, 3), p << 2);
      Assert.AreEqual(new GF2pPolynomial(field, 0, 0, 1, 0, 2, 0, 3), p >> -1);
      Assert.AreEqual(new GF2pPolynomial(field, 0, 0, 0, 1, 0, 2, 0, 3), p >> -2);
    }

    [Test]
    public void T05_PolynomialMultiplication()
    {
      GF2pField field = new GF2pField(8);
      GF2pPolynomial a = new GF2pPolynomial(field, 0, 4, 17, 0, 9), b = new GF2pPolynomial(field, 19, 11, 0, 100);
      Assert.AreEqual(new GF2pPolynomial(field, 0, 76, 18, 187, 6, 57, 0, 99), a * b);
      Assert.AreEqual(a * b, b * a);
      Assert.AreEqual(new GF2pPolynomial(field, 0), new GF2pPolynomial(field, 0) * a);
      Assert.AreEqual(new GF2pPolynomial(field, 0), default(GF2pPolynomial) * b);

      Assert.AreEqual(new GF2pPolynomial(field, 121, 49, 0, 33), b * 7);

      Assert.AreEqual(0, GF2pPolynomial.MultiplyAt(a, b, 0));
      Assert.AreEqual(76, GF2pPolynomial.MultiplyAt(a, b, 1));
      Assert.AreEqual(18, GF2pPolynomial.MultiplyAt(a, b, 2));
      Assert.AreEqual(187, GF2pPolynomial.MultiplyAt(a, b, 3));
      Assert.AreEqual(6, GF2pPolynomial.MultiplyAt(a, b, 4));
      Assert.AreEqual(57, GF2pPolynomial.MultiplyAt(a, b, 5));
      Assert.AreEqual(0, GF2pPolynomial.MultiplyAt(a, b, 6));
      Assert.AreEqual(99, GF2pPolynomial.MultiplyAt(a, b, 7));
      Assert.AreEqual(0, GF2pPolynomial.MultiplyAt(a, b, 8));
      Assert.AreEqual(0, GF2pPolynomial.MultiplyAt(a, b, 9));

      Assert.AreEqual(0, a.MultiplyAt(b, 0));
      Assert.AreEqual(76, a.MultiplyAt(b, 1));
      Assert.AreEqual(18, a.MultiplyAt(b, 2));
      Assert.AreEqual(187, a.MultiplyAt(b, 3));
    }

    [Test]
    public void T06_PolynomialDivision()
    {
      Action<GF2pPolynomial, GF2pPolynomial, GF2pPolynomial, GF2pPolynomial> testDivision = (a, b, q, r) =>
      {
        Assert.AreEqual(q, a / b);
        Assert.AreEqual(r, a % b);
        GF2pPolynomial remainder;
        Assert.AreEqual(q, a.DivRem(b, out remainder));
        Assert.AreEqual(r, remainder);
        Assert.AreEqual(q, GF2pPolynomial.DivRem(a, b, out remainder));
        Assert.AreEqual(r, remainder);
      };

      GF2pField field = new GF2pField(8);
      GF2pPolynomial x = new GF2pPolynomial(field, 0, 4, 17, 0, 9, 1), y = new GF2pPolynomial(field, 19, 11, 0, 100);
      testDivision(x, y, new GF2pPolynomial(field, 136, 24, 185), new GF2pPolynomial(field, 237, 0, 112));
      testDivision(y, x, new GF2pPolynomial(field, 0), y);
      x = new GF2pPolynomial(field, 1, 2, 3, 4);
      testDivision(x, y, new GF2pPolynomial(field, 222), new GF2pPolynomial(field, 31, 195, 3));
      testDivision(y, x, new GF2pPolynomial(field, 25), new GF2pPolynomial(field, 10, 57, 43));
    }

    private static void AssertInField(GF2pField field, int value, bool allowZero = false)
    {
      Assert.GreaterOrEqual(value, 0);
      Assert.LessOrEqual(value, field.MaxValue);
      Assert.Less(value, field.Order);
      if(!allowZero) Assert.AreNotEqual(0, value);
    }

    private static void TestFieldBasics(int power)
    {
      GF2pField field = power != 1 ? new GF2pField(power) : new GF2pField(power, 0, 1);
      HashSet<int> set = new HashSet<int>();

      // ensure all the multiplicative values are within the field
      for(int x=1, i=1; i<field.Order; x = field.Multiply(x, field.Generator), i++)
      {
        AssertInField(field, x);
        Assert.IsTrue(set.Add(x));
      }
      Assert.AreEqual(set.Count, field.Order-1);

      for(int x=0; x<field.Order; x++)
      {
        Assert.AreEqual(x, field.Negate(x));

        int square = field.Square(x);
        Assert.AreEqual(square, field.Multiply(x, x));

        if(x != 0)
        {
          if (field.Power <= 8) // Log is only implemented up to GF(2^8)
          {
            int log = field.Log(x);
            AssertInField(field, log, true);
            Assert.AreEqual(x, field.Exp(log));
          }

          Assert.AreEqual(field.Pow(field.Generator, x), field.Exp(x));
          int inverse = field.Invert(x);
          Assert.AreEqual(field.Divide(1, x), inverse);
          Assert.AreEqual(x, field.Invert(inverse));
        }

        for(int y=0; y<field.Order; y++)
        {
          int sum = field.Add(x, y), product = field.Multiply(x, y), pow = field.Pow(x, y);
          AssertInField(field, sum, (x ^ y) == 0);
          AssertInField(field, product, x == 0 || y == 0);
          AssertInField(field, pow, x == 0);
          if(y != 0)
          {
            int quotient = field.Divide(x, y);
            AssertInField(field, quotient, x == 0);
          }
        }
      }
    }
 }
}