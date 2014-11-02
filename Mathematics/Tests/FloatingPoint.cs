using System;
using System.Globalization;
using System.IO;
using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Mathematics.Tests
{
  [TestFixture]
  public class FloatingPoint
  {
    [Test]
    public void TestIEEE754()
    {
      // make sure RawDecompose/RawCompose and Compose/Decompose can round trip various kinds of values
      TestRawDecomposition(double.Epsilon, false, 0, 1);
      TestRawDecomposition(double.MaxValue, false, 2046, (1UL<<52)-1);
      TestRawDecomposition(double.MinValue, true, 2046, (1UL<<52)-1);
      TestRawDecomposition(double.NegativeInfinity, true, 2047, 0);
      TestRawDecomposition(double.PositiveInfinity, false, 2047, 0);
      TestRawDecomposition(0d, false, 0, 0);
      TestRawDecomposition(float.Epsilon, false, 0, 1);
      TestRawDecomposition(float.MaxValue, false, 254, (1u<<23)-1);
      TestRawDecomposition(float.MinValue, true, 254, (1u<<23)-1);
      TestRawDecomposition(float.NegativeInfinity, true, 255, 0);
      TestRawDecomposition(float.PositiveInfinity, false, 255, 0);
      TestRawDecomposition(0f, false, 0, 0);
      TestDecomposition(double.Epsilon, false, -1074, 1);
      TestDecomposition(double.MaxValue, false, 971, (1UL<<53)-1);
      TestDecomposition(double.MinValue, true, 971, (1UL<<53)-1);
      TestDecomposition(float.Epsilon, false, -149, 1);
      TestDecomposition(float.MaxValue, false, 104, (1u<<24)-1);
      TestDecomposition(float.MinValue, true, 104, (1u<<24)-1);

      // make sure Compose can normalize values, fix exponents, etc.
      Assert.AreEqual(0, IEEE754.ComposeDouble(false, 100, 0));
      Assert.AreEqual(1, IEEE754.ComposeDouble(false, 0, 1));
      Assert.AreEqual(2, IEEE754.ComposeDouble(false, 0, 2));
      Assert.AreEqual(2, IEEE754.ComposeDouble(false, 1, 1));
      Assert.AreEqual(0, IEEE754.ComposeDouble(false, -5000, 0));
      Assert.AreEqual(0, IEEE754.ComposeDouble(false, 5000, 0));
      Assert.AreEqual(IEEE754.RawComposeDouble(false, 2046, 0), IEEE754.ComposeDouble(false, 1023, 1));
      Assert.AreEqual(IEEE754.MaxDoubleInt+2, IEEE754.ComposeDouble(false, 0, IEEE754.MaxDoubleInt+2));
      Assert.AreEqual(0, IEEE754.ComposeSingle(false, 100, 0));
      Assert.AreEqual(1, IEEE754.ComposeSingle(false, 0, 1));
      Assert.AreEqual(2, IEEE754.ComposeSingle(false, 0, 2));
      Assert.AreEqual(2, IEEE754.ComposeSingle(false, 1, 1));
      Assert.AreEqual(0, IEEE754.ComposeSingle(false, -5000, 0));
      Assert.AreEqual(0, IEEE754.ComposeSingle(false, 5000, 0));
      Assert.AreEqual(IEEE754.RawComposeSingle(false, 254, 0), IEEE754.ComposeSingle(false, 127, 1));
      Assert.AreEqual(IEEE754.MaxSingleInt+2, IEEE754.ComposeSingle(false, 0, IEEE754.MaxSingleInt+2));

      // make sure Compose rejects invalid values
      TestHelpers.TestException<ArgumentOutOfRangeException>(delegate { IEEE754.ComposeDouble(false, 0, IEEE754.MaxDoubleInt+1); });
      TestHelpers.TestException<ArgumentOutOfRangeException>(delegate { IEEE754.ComposeDouble(false, -1075, 1); });
      TestHelpers.TestException<ArgumentOutOfRangeException>(delegate { IEEE754.ComposeDouble(false, 1024, 1); });
      TestHelpers.TestException<ArgumentOutOfRangeException>(delegate { IEEE754.ComposeSingle(false, 0, IEEE754.MaxSingleInt+1); });
      TestHelpers.TestException<ArgumentOutOfRangeException>(delegate { IEEE754.ComposeSingle(false, -150, 1); });
      TestHelpers.TestException<ArgumentOutOfRangeException>(delegate { IEEE754.ComposeSingle(false, 128, 1); });

      // test AdjustExponent
      Assert.AreEqual(2, IEEE754.AdjustExponent(1, 1));
      Assert.AreEqual(0.5, IEEE754.AdjustExponent(1, -1));
      Assert.AreEqual(double.MaxValue/2, IEEE754.AdjustExponent(double.MaxValue, -1));
      Assert.AreEqual(double.MaxValue, IEEE754.AdjustExponent(double.MaxValue, 0));
      Assert.AreEqual(double.PositiveInfinity, IEEE754.AdjustExponent(double.MaxValue, 1));
      Assert.AreEqual(double.PositiveInfinity, IEEE754.AdjustExponent(double.MaxValue, int.MaxValue));
      Assert.AreEqual(double.NegativeInfinity, IEEE754.AdjustExponent(double.MinValue, 1));
      Assert.AreEqual(0, IEEE754.AdjustExponent(double.Epsilon, -2000));
      Assert.AreEqual(double.PositiveInfinity, IEEE754.AdjustExponent(double.Epsilon, 3000));
      Assert.AreEqual(double.NegativeInfinity, IEEE754.AdjustExponent(-double.Epsilon, 3000));
      Assert.AreEqual(0, IEEE754.AdjustExponent(double.Epsilon, -1));
      Assert.AreEqual(double.Epsilon*16, IEEE754.AdjustExponent(double.Epsilon, 4));
      Assert.AreEqual(-double.Epsilon*16, IEEE754.AdjustExponent(-double.Epsilon, 4));
      Assert.AreEqual(double.Epsilon, IEEE754.AdjustExponent(double.Epsilon*16, -4));
      Assert.AreEqual(-double.Epsilon, IEEE754.AdjustExponent(-double.Epsilon*16, -4));
      Assert.AreEqual(double.Epsilon*(1UL<<52), IEEE754.AdjustExponent(double.Epsilon, 52));
      Assert.AreEqual(-double.Epsilon*(1UL<<52), IEEE754.AdjustExponent(-double.Epsilon, 52));
      Assert.AreEqual(double.Epsilon*(1UL<<53), IEEE754.AdjustExponent(double.Epsilon, 53));
      Assert.AreEqual(double.Epsilon, IEEE754.AdjustExponent(double.Epsilon*(1UL<<52), -52));
      Assert.AreEqual(-double.Epsilon, IEEE754.AdjustExponent(-double.Epsilon*(1UL<<52), -52));
    }

    [Test]
    public void FP107_Comparisons()
    {
      #pragma warning disable 1718

      // test flags
      Assert.IsTrue(FP107.NaN.IsNaN);
      Assert.IsFalse(FP107.NaN.IsInfinity);
      Assert.IsFalse(FP107.NaN.IsPositiveInfinity);
      Assert.IsFalse(FP107.NaN.IsNegativeInfinity);
      Assert.IsTrue(FP107.PositiveInfinity.IsInfinity);
      Assert.IsTrue(FP107.PositiveInfinity.IsPositiveInfinity);
      Assert.IsFalse(FP107.PositiveInfinity.IsNegativeInfinity);
      Assert.IsFalse(FP107.PositiveInfinity.IsNaN);
      Assert.IsTrue(FP107.NegativeInfinity.IsInfinity);
      Assert.IsFalse(FP107.NegativeInfinity.IsPositiveInfinity);
      Assert.IsTrue(FP107.NegativeInfinity.IsNegativeInfinity);
      Assert.IsFalse(FP107.NegativeInfinity.IsNaN);

      // test equality
      Assert.AreEqual(double.Epsilon, (double)new FP107(double.Epsilon));
      Assert.AreEqual(double.MaxValue, (double)new FP107(double.MaxValue));
      Assert.AreEqual(double.MinValue, (double)new FP107(double.MinValue));
      Assert.AreEqual(double.PositiveInfinity, (double)new FP107(double.PositiveInfinity));
      Assert.AreEqual(double.NegativeInfinity, (double)new FP107(double.NegativeInfinity));
      Assert.AreEqual(double.NaN, (double)new FP107(double.NaN));
      Assert.AreEqual(Math.E, (double)FP107.E);
      Assert.AreEqual(Math.PI, (double)FP107.Pi);
      Assert.AreEqual(0, (double)FP107.Zero);
      Assert.AreEqual(1, (double)FP107.One);
      Assert.AreEqual(-1, (double)FP107.MinusOne);
      Assert.AreEqual(FP107.Pi, FP107.Pi);
      Assert.IsTrue(double.Epsilon == new FP107(double.Epsilon));
      Assert.IsTrue(double.MaxValue == new FP107(double.MaxValue));
      Assert.IsTrue(double.MinValue == new FP107(double.MinValue));
      Assert.IsTrue(double.PositiveInfinity == new FP107(double.PositiveInfinity));
      Assert.IsTrue(double.NegativeInfinity == new FP107(double.NegativeInfinity));
      Assert.IsTrue(new FP107(double.Epsilon) == new FP107(double.Epsilon));
      Assert.IsTrue(new FP107(double.MaxValue) == new FP107(double.MaxValue));
      Assert.IsTrue(new FP107(double.MinValue) == new FP107(double.MinValue));
      Assert.IsTrue(new FP107(double.PositiveInfinity) == new FP107(double.PositiveInfinity));
      Assert.IsTrue(new FP107(double.NegativeInfinity) == new FP107(double.NegativeInfinity));
      Assert.IsTrue(FP107.PositiveInfinity == FP107.PositiveInfinity);
      Assert.IsTrue(FP107.NegativeInfinity == FP107.NegativeInfinity);
      Assert.IsTrue(FP107.Pi == FP107.Pi);
      Assert.IsFalse(FP107.NaN == FP107.NaN);
      Assert.IsFalse(Math.E == FP107.E);
      Assert.IsFalse(Math.PI == FP107.Pi);

      // test inequality
      Assert.IsTrue(FP107.One != FP107.Zero);
      Assert.IsTrue(FP107.NaN != FP107.NaN);
      Assert.IsTrue(new FP107((double)(IEEE754.MaxDoubleInt+1)) != new FP107(IEEE754.MaxDoubleInt+1));
      Assert.IsFalse(FP107.One != FP107.One);
      Assert.IsFalse(FP107.Pi != FP107.Pi);
      Assert.IsTrue(FP107.Pi != Math.PI);

      // test less than, greater than, etc.
      Assert.IsTrue(FP107.Zero < FP107.One);
      Assert.IsTrue(FP107.MinusOne < FP107.Zero);
      Assert.IsTrue(FP107.NegativeInfinity < FP107.MinValue);
      Assert.IsTrue(3 < FP107.Pi);
      Assert.IsFalse(4 < FP107.Pi);
      Assert.IsTrue(Math.PI < FP107.Pi);
      Assert.IsTrue((FP107)Math.PI < FP107.Pi);
      Assert.IsFalse(FP107.One < FP107.One);
      Assert.IsFalse(FP107.Pi < FP107.Pi);
      Assert.IsTrue((FP107)double.MaxValue < FP107.Add(double.MaxValue, double.Epsilon));
      Assert.IsTrue((FP107)double.MinValue < FP107.Subtract(double.Epsilon, double.MaxValue));

      Assert.IsTrue(FP107.Zero <= FP107.One);
      Assert.IsTrue(FP107.MinusOne <= FP107.Zero);
      Assert.IsTrue(FP107.NegativeInfinity <= FP107.MinValue);
      Assert.IsTrue(3 <= FP107.Pi);
      Assert.IsFalse(4 <= FP107.Pi);
      Assert.IsTrue(Math.PI <= FP107.Pi);
      Assert.IsTrue((FP107)Math.PI <= FP107.Pi);
      Assert.IsTrue(FP107.One <= FP107.One);
      Assert.IsTrue(FP107.Pi <= FP107.Pi);

      Assert.IsFalse(FP107.Zero > FP107.One);
      Assert.IsFalse(FP107.MinusOne > FP107.Zero);
      Assert.IsFalse(FP107.NegativeInfinity > FP107.MinValue);
      Assert.IsFalse(3 > FP107.Pi);
      Assert.IsTrue(4 > FP107.Pi);
      Assert.IsFalse(Math.PI > FP107.Pi);
      Assert.IsFalse((FP107)Math.PI > FP107.Pi);
      Assert.IsFalse(FP107.One > FP107.One);
      Assert.IsFalse(FP107.Pi > FP107.Pi);
      Assert.IsTrue((FP107)double.MaxValue > FP107.Subtract(double.MaxValue, double.Epsilon));
      Assert.IsTrue((FP107)double.MinValue > FP107.Subtract(double.MinValue, double.Epsilon));

      Assert.IsFalse(FP107.Zero >= FP107.One);
      Assert.IsFalse(FP107.MinusOne >= FP107.Zero);
      Assert.IsFalse(FP107.NegativeInfinity >= FP107.MinValue);
      Assert.IsFalse(3 >= FP107.Pi);
      Assert.IsTrue(4 >= FP107.Pi);
      Assert.IsFalse(Math.PI >= FP107.Pi);
      Assert.IsFalse((FP107)Math.PI >= FP107.Pi);
      Assert.IsTrue(FP107.One >= FP107.One);
      Assert.IsTrue(FP107.Pi >= FP107.Pi);

      #pragma warning restore 1718
    }

    [Test]
    public void FP107_Conversions()
    {
      // test conversions to/from integer, double, and float
      Assert.AreEqual((double)int.MaxValue, (double)new FP107(int.MaxValue));
      Assert.AreEqual(int.MaxValue, (int)new FP107(int.MaxValue));
      Assert.AreEqual((double)uint.MaxValue, (double)new FP107(uint.MaxValue));
      Assert.AreEqual(uint.MaxValue, (uint)new FP107(uint.MaxValue));
      Assert.AreEqual((double)long.MaxValue, (double)new FP107(long.MaxValue));
      Assert.AreEqual(long.MaxValue, (long)new FP107(long.MaxValue));
      Assert.AreEqual((double)ulong.MaxValue, (double)new FP107(ulong.MaxValue));

      Assert.AreEqual((double)int.MinValue, (double)new FP107(int.MinValue));
      Assert.AreEqual(int.MinValue, (int)new FP107(int.MinValue));
      Assert.AreEqual((double)uint.MinValue, (double)new FP107(uint.MinValue));
      Assert.AreEqual(uint.MinValue, (uint)new FP107(uint.MinValue));
      Assert.AreEqual((double)long.MinValue, (double)new FP107(long.MinValue));
      Assert.AreEqual(long.MinValue, (long)new FP107(long.MinValue));
      Assert.AreEqual((double)ulong.MinValue, (double)new FP107(ulong.MinValue));

      Assert.AreEqual(ulong.MaxValue, (ulong)new FP107(ulong.MaxValue));
      Assert.AreEqual(uint.MaxValue, (uint)new FP107(ulong.MaxValue));
      Assert.AreEqual(-1L, (long)new FP107(ulong.MaxValue));
      Assert.AreEqual(-1, (int)new FP107(ulong.MaxValue));

      Assert.AreEqual(Math.PI, (double)FP107.Pi);
      Assert.AreEqual((float)Math.PI, (float)FP107.Pi);

      // test conversions to/from decimal
      Assert.AreEqual(-3.141592653589793238462643383m, ((IConvertible)(-FP107.Pi)).ToDecimal(null));
      Assert.AreEqual(FP107.FromDecimalApproximation(12345.6789), (FP107)12345.6789m);
      Assert.AreEqual(FP107.FromDecimalApproximation(-12345.6789), new FP107(-12345.6789m));

      // test conversions to/from string
      CultureInfo inv = CultureInfo.InvariantCulture;
      // these should round-trip exactly
      int[] dens = new int[] { 2, 3, 5, 7, 11, -23 };
      string[] strs = new string[]
      {
        "+5e-1", ".3333333333333333333333333333333333333333", "+.02E1", ".1428571428571428571428571428571428571429",
        ".0909090909090909090909090909090909090909", "(.0434782608695652173913043478260869565217)",
      };
      for(int i=0; i<dens.Length; i++)
      {
        Assert.AreEqual(FP107.Divide(1, dens[i]), FP107.Parse(strs[i], inv));
      }

      // these should round-trip within about 3 bits of precision (FP107 only guarantees round-tripping 107 bits, but these have more)
      dens = new int[] { -15, 17 };
      strs = new string[] { "-.0666666666666666666666666666666666666667", ".0588235294117647058823529411764705882352", };
      for(int i=0; i<dens.Length; i++)
      {
        FP107 diff = (FP107.Divide(1, dens[i]) - FP107.Parse(strs[i], inv)).Abs();
        Assert.IsTrue(diff < FP107.Pow(2, -110));
      }

      // make sure various values round-trip
      FP107[] values = new FP107[]
      {
        double.Epsilon, FP107.E, FP107.GoldenRatio, FP107.Pi, -0d, 1e20, 3/FP107.Pow(10, 307),
        FP107.MaxValue, FP107.MinValue, FP107.PositiveInfinity, FP107.NegativeInfinity, FP107.NaN, FP107.Pow(10, 49),
      };
      foreach(FP107 value in values) Assert.AreEqual(value, FP107.Parse(value.ToString("R", inv), inv));
      Assert.AreEqual("1.05", FP107.FromDecimalApproximation(1.05).ToString("R", inv));

      // bug repros
      Assert.AreEqual("1E+49", FP107.Pow(10, 49).ToString("E0"));
      Assert.AreEqual("2.2E-16", FP107.FromComponents(2.2204460492503131E-16, -3.2000000000000004E-48).ToString("E1"));

      // overflow and underflow
      Assert.AreEqual(FP107.MaxValue, FP107.Parse("1.79769313486231580793728971405302e+308", inv));
      TestHelpers.TestException<OverflowException>(delegate { FP107.Parse("1.79769313486231580793728971405303e+308", inv); });
      TestHelpers.TestException<OverflowException>(delegate { FP107.Parse("10e+308", inv); }); // "10e" isn't a typo
      Assert.AreEqual((FP107)double.Epsilon, FP107.Parse("5e-324", inv));
      Assert.AreEqual(FP107.Zero, FP107.Parse("2e-324", inv));

      // test string formatting
      FP107 npi = -FP107.Pi;
      // first test default formattings in the invariant culture
      Assert.AreEqual("-3.1415926535897932384626433832795", npi.ToString(inv));
      Assert.AreEqual("(¤3.14)", npi.ToString("C", inv));
      Assert.AreEqual("-3.1415926535897932384626433832795E+0", npi.ToString("E", inv));
      Assert.AreEqual("-3.14", npi.ToString("F", inv));
      Assert.AreEqual("-3.1415926535897932384626433832795", npi.ToString("G", inv));
      Assert.AreEqual("-3.14", npi.ToString("N", inv));
      Assert.AreEqual("-314.16 %", npi.ToString("P", inv));
      Assert.AreEqual("-3.1415926535897932384626433832795", npi.ToString("R", inv));
      Assert.AreEqual("C00921FB54442D18:BCA1A62633145C07", npi.ToString("X", inv));
      Assert.AreEqual("c00921fb54442d18:bca1a62633145c07", npi.ToString("x", inv));
      Assert.AreEqual("1E+5", new FP107(1e5).ToString(inv));
      Assert.AreEqual("1E+5", new FP107(1e5).ToString("G", inv));
      Assert.AreEqual("100000", new FP107(1e5).ToString("F", inv));
      Assert.AreEqual("1,000,000", new FP107(1e6).ToString("N", inv));
      Assert.AreEqual("¤1,000,000", new FP107(1e6).ToString("C", inv));
      Assert.AreEqual("-3.14159E+2", (npi*100).ToString("E5", inv));
      Assert.AreEqual("0", new FP107(-0d).ToString(inv));
      Assert.AreEqual("-0", new FP107(-0d).ToString("R", inv));
      Assert.AreEqual("[3.1415926535897931, 1.2246467991473532E-16]", FP107.Pi.ToString("S"));
      Assert.AreEqual(Math.PI.ToString("R", inv), ((FP107)Math.PI).ToString("S"));

      // test rounding
      Assert.AreEqual("1", new FP107(1.5 - IEEE754.DoublePrecision).ToString("F0", inv));
      Assert.AreEqual("2", new FP107(1.5).ToString("F0", inv));
      Assert.AreEqual("2", new FP107(1.5 + IEEE754.DoublePrecision).ToString("F0", inv));
      Assert.AreEqual("2", new FP107(2.5).ToString("F0", inv));
      Assert.AreEqual("3", new FP107(2.5 + IEEE754.DoublePrecision*2).ToString("F0", inv));
      Assert.AreEqual("4", new FP107(3.5).ToString("F0", inv));
      Assert.AreEqual("-3", npi.ToString("F0", inv));
      Assert.AreEqual("-3.1", npi.ToString("F1", inv));
      Assert.AreEqual("-3.142", npi.ToString("F3", inv));
      Assert.AreEqual("0.001", new FP107(0.0012356).ToString("F3", inv));
      Assert.AreEqual("54321.001", new FP107(54321.0012356).ToString("F3", inv));
      Assert.AreEqual("1", new FP107(0.99999).ToString("F4", inv));
      Assert.AreEqual("0.99999", new FP107(0.99999).ToString("F5", inv));
      Assert.AreEqual("0.99999", new FP107(0.99999).ToString("F9", inv));
      Assert.AreEqual("0.1", new FP107(0.099999).ToString("F5", inv));
      Assert.AreEqual("10", new FP107(9.99999).ToString("F4", inv));

      // test parsing weird strings
      Assert.AreEqual(FP107.FromDecimalApproximation(123.45), FP107.Parse("  % 0, 1 ,234 .50E+1+  ", inv)); // percent
      Assert.AreEqual(FP107.FromDecimalApproximation(-123.45), FP107.Parse(" ( 0 0, 1 ,234500 E-1‰- ) ", inv)); // negative, permille
      Assert.AreEqual(FP107.FromDecimalApproximation(-123.45), FP107.Parse("¤123.45-", inv)); // negative, currency

      // test long symbols
      NumberFormatInfo nfi = (NumberFormatInfo)inv.NumberFormat.Clone();
      nfi.CurrencySymbol = "$/=";
      nfi.PercentSymbol  = "pct";
      nfi.PerMilleSymbol = "pml";
      nfi.NegativeSign   = "minus";
      nfi.PositiveSign   = "plus";
      TestFormatRoundTrip(5, "$/=5", "C", nfi);
      TestFormatRoundTrip(FP107.FromDecimalApproximation(-0.05), "minus5 pct", "P", nfi);
      Assert.AreEqual(new FP107(5), FP107.Parse("5 plus", nfi));
      Assert.AreEqual(new FP107(-5), FP107.Parse("5 minus", nfi));
      Assert.AreEqual(FP107.FromDecimalApproximation(-.005), FP107.Parse("5 pmlminus", nfi));

      // test weird grouping
      nfi.NumberGroupSizes = new int[] { 2, 3, 4 };
      Assert.AreEqual("1,2345,678,90", new FP107(1234567890).ToString("N", nfi));
      Assert.AreEqual("1,2345,6789,000,00", new FP107(12345678900000).ToString("N", nfi));
      nfi.NumberGroupSizes = new int[] { 2, 3, 0 };
      Assert.AreEqual("12345,678,90", new FP107(1234567890).ToString("N", nfi));

      // test currency formats
      TestCurrencyFormat(false, "$n",  0);
      TestCurrencyFormat(false, "n$",  1);
      TestCurrencyFormat(false, "$ n", 2);
      TestCurrencyFormat(false, "n $", 3);
      TestCurrencyFormat(true, "($n)", 0);
      TestCurrencyFormat(true, "-$n",  1);
      TestCurrencyFormat(true, "$-n",  2);
      TestCurrencyFormat(true, "$n-",  3);
      TestCurrencyFormat(true, "(n$)", 4);
      TestCurrencyFormat(true, "-n$",  5);
      TestCurrencyFormat(true, "n-$",  6);
      TestCurrencyFormat(true, "n$-",  7);
      TestCurrencyFormat(true, "-n $", 8);
      TestCurrencyFormat(true, "-$ n", 9);
      TestCurrencyFormat(true, "n $-", 10);
      TestCurrencyFormat(true, "$ n-", 11);
      TestCurrencyFormat(true, "$ -n", 12);
      TestCurrencyFormat(true, "n- $", 13);
      TestCurrencyFormat(true, "($ n)", 14);
      TestCurrencyFormat(true, "(n $)", 15);

      // test percent formats
      TestPercentFormat(false, "n %", 0);
      TestPercentFormat(false, "n%",  1);
      TestPercentFormat(false, "%n",  2);
      TestPercentFormat(false, "% n", 3);
      TestPercentFormat(true, "-n %", 0);
      TestPercentFormat(true, "-n%",  1);
      TestPercentFormat(true, "-%n",  2);
      TestPercentFormat(true, "%-n",  3);
      TestPercentFormat(true, "%n-",  4);
      TestPercentFormat(true, "n-%",  5);
      TestPercentFormat(true, "n%-",  6);
      TestPercentFormat(true, "-% n", 7);
      TestPercentFormat(true, "n %-", 8);
      TestPercentFormat(true, "% n-", 9);
      TestPercentFormat(true, "% -n", 10);
      TestPercentFormat(true, "n- %", 11);

      // test number formats
      TestNumberFormat("(n)", 0);
      TestNumberFormat("-n",  1);
      TestNumberFormat("- n", 2);
      TestNumberFormat("n-",  3);
      TestNumberFormat("n -", 4);

      // test saving and loading in various forms
      double hi, lo;
      FP107.Pi.GetComponents(out hi, out lo);
      Assert.AreEqual(FP107.Pi, FP107.FromComponents(hi, lo));
      FP107.NegativeInfinity.GetComponents(out hi, out lo);
      Assert.AreEqual(FP107.NegativeInfinity, FP107.FromComponents(hi, lo));
      FP107.NaN.GetComponents(out hi, out lo);
      Assert.AreEqual(FP107.NaN, FP107.FromComponents(hi, lo));
      
      MemoryStream ms = new MemoryStream();
      using(var writer = new AdamMil.IO.BinaryWriter(ms, false)) FP107.Pi.Save(writer);
      ms.Position = 0;
      using(var reader = new AdamMil.IO.BinaryReader(ms)) Assert.AreEqual(FP107.Pi, new FP107(reader));
    }

    [Test]
    public void FP107_Arithmetic()
    {
      CultureInfo inv = CultureInfo.InvariantCulture;

      // test addition
      TestAdd(42, 40, 2); // add normal numbers
      TestAdd(-38, -40, 2);
      TestAdd(0, double.MinValue, double.MaxValue); // test cancelation of big numbers
      TestAdd(FP107.Add(double.MaxValue, double.Epsilon), double.MaxValue, double.Epsilon); // add numbers of hugely differing magnitudes
      TestAdd(FP107.Add(double.MinValue, -double.Epsilon), double.MinValue, -double.Epsilon);
      TestAdd(FP107.Add(double.MaxValue, double.MaxValue/(1L<<54)), double.MaxValue, double.MaxValue/(1L<<54)); // add near overflow
      Assert.AreNotEqual(FP107.PositiveInfinity, FP107.Add(double.MaxValue, double.MaxValue/(1L<<54)));
      TestAdd(double.PositiveInfinity, double.MaxValue, double.MaxValue/(1L<<53));
      TestAdd(FP107.Add(double.MinValue, double.MinValue/(1L<<54)), double.MinValue, double.MinValue/(1L<<54));
      Assert.AreNotEqual(FP107.NegativeInfinity, FP107.Add(double.MinValue, double.MinValue/(1L<<54)));
      TestAdd(double.NegativeInfinity, double.MinValue, double.MinValue/(1L<<53));
      TestAdd(double.NaN, 50, double.NaN); // test NaN propagation
      Assert.AreEqual(FP107.Parse("5.859874482048838473822930854632145852538", inv), FP107.Pi + FP107.E); // high precision

      // test subtraction
      TestSubtract(2, 42, 40); // subtract normal numbers
      TestSubtract(-42, -40, 2);
      TestSubtract(-38, -40, -2);
      TestSubtract(double.MaxValue, 0, double.MinValue); // subtract big from small
      TestSubtract(double.MinValue, 0, double.MaxValue);
      TestSubtract(FP107.Subtract(double.MaxValue, double.Epsilon), double.MaxValue, double.Epsilon); // widely differing magnitudes
      TestSubtract(FP107.Subtract(double.MinValue, double.Epsilon), double.MinValue, double.Epsilon);
      TestSubtract(FP107.Subtract(double.MinValue, double.MinValue/(1L<<54)), double.MinValue, double.MinValue/(1L<<54)); // near overflow
      TestSubtract(double.NegativeInfinity, double.MinValue, double.MaxValue/(1L<<53));
      TestSubtract(FP107.Add(double.MaxValue, double.MaxValue/(1L<<54)), double.MaxValue, double.MinValue/(1L<<54));
      TestSubtract(double.PositiveInfinity, double.MaxValue, double.MinValue/(1L<<53));
      TestSubtract(double.NaN, 50, double.NaN); // test NaN propagation
      Assert.AreEqual(FP107.Parse(".4233108251307480031023559119268412534926", inv), FP107.Pi - FP107.E); // high precision

      // test multiplication
      TestMultiply(1680, 42, 40); // multiply normal numbers
      TestMultiply(-80, -40, 2);
      TestMultiply(80, -40, -2);
      TestMultiply(100, 100*Math.Pow(2, 20), 1/Math.Pow(2, 20));
      TestMultiply(1, 64, 1d/64);
      TestMultiply(double.MaxValue, 1, double.MaxValue);
      TestMultiply(0, double.MinValue, 0);
      TestMultiply(IEEE754.ComposeDouble(false, -103, 1), IEEE754.ComposeDouble(false, 971, 1), double.Epsilon); // differing magnitudes
      TestMultiply(double.MaxValue, double.MaxValue/2, 2); // near overflow
      TestMultiply(double.NaN, 50, double.NaN); // test NaN propagation
      TestMultiply(double.NaN, 0, double.PositiveInfinity); // 0 * infinity = NaN
      TestMultiply(double.NaN, 0, double.NegativeInfinity);
      TestMultiply(double.PositiveInfinity, 1, double.PositiveInfinity); // x * infinity = infinity
      TestMultiply(double.NegativeInfinity, 1, double.NegativeInfinity);
      TestMultiply(double.NegativeInfinity, -1, double.PositiveInfinity); // x * infinity = infinity
      TestMultiply(double.PositiveInfinity, -1, double.NegativeInfinity);
      Assert.AreEqual(FP107.Parse("8.5397342226735670654635508695467", inv), FP107.Pi * FP107.E); // high precision

      // test division
      TestDivide(FP107.FromDecimalApproximation(1.05), 42, 40);
      TestDivide(-20, -40, 2);
      TestDivide(20, -40, -2);
      TestDivide(1.0/(1<<20), 1, 1<<20);
      TestDivide(4096, 64, 1d/64);
      TestDivide(-1, double.MaxValue, double.MinValue);
      TestDivide(0, 0, double.MaxValue);
      TestDivide(Math.Pow(2, 1000), Math.Pow(2, 500), Math.Pow(2, -500));
      TestDivide(double.MinValue, double.MaxValue, -1);
      TestDivide(double.MaxValue, double.MaxValue/2, 0.5);
      TestDivide(double.NaN, double.NaN, 5);
      TestDivide(double.NaN, double.PositiveInfinity, double.PositiveInfinity); // infinity / infinity = NaN
      TestDivide(double.NaN, double.PositiveInfinity, 0); // infinity / 0 = NaN
      TestDivide(double.PositiveInfinity, 5, 0); // finity / 0 = infinity
      TestDivide(double.NegativeInfinity, -5, 0);
      TestDivide(double.PositiveInfinity, double.PositiveInfinity, 100); // infinity / finity = infinity
      TestDivide(double.NegativeInfinity, double.PositiveInfinity, -100);
      TestDivide(double.PositiveInfinity, double.NegativeInfinity, -100);
      TestDivide(double.NegativeInfinity, double.NegativeInfinity, 100);
      TestDivide(0, double.MaxValue, double.PositiveInfinity); // finity / infinity = 0
      Assert.AreEqual(FP107.Parse("1.15572734979092171791009318331269", inv), FP107.Pi / FP107.E); // high precision

      // test remainder
      TestRemainder(2, 42, 40);
      TestRemainder(-1, -9, 8);
      TestRemainder(1, 9, -8);
      TestRemainder(-1, -9, -8);
      TestRemainder(5, 5, 6);
      TestRemainder(0.125, 1.125, 0.5);
      TestRemainder(0, -40, 2);
      TestRemainder(0, -40, -2);
      TestRemainder(0, 1, 1d/(1<<20));
      TestRemainder(1.0715086061883472E+301, double.MaxValue/2, Math.Pow(2, 1000));
      TestRemainder(double.NaN, 5, 0);
      TestRemainder(double.NaN, 0, double.NaN);
      TestRemainder(double.NaN, double.PositiveInfinity, 1); // infinity % anything = NaN
      TestRemainder(double.NaN, double.PositiveInfinity, double.PositiveInfinity);
      TestRemainder(double.NaN, double.NaN, double.PositiveInfinity); // NaN % infinity = NaN
      TestRemainder(1000, 1000, double.PositiveInfinity); // a % infinity = a
      TestRemainder(1000, 1000, double.NegativeInfinity); // a % infinity = a
      TestRemainder(-1000, -1000, double.PositiveInfinity); // a % infinity = a
      TestRemainder(-1000, -1000, double.NegativeInfinity); // a % infinity = a
      Assert.AreEqual(FP107.Parse(".42331082513074800310235591192684", inv), FP107.Pi % FP107.E); // high precision
    }

    [Test]
    public void FP107_SimpleFunctions()
    {
      // test the Abs function
      Assert.AreEqual(FP107.Pi, FP107.Pi.Abs());
      Assert.AreEqual(FP107.Pi, (-FP107.Pi).Abs());
      Assert.AreEqual(FP107.Divide(1, 5), FP107.Divide(1, 5).Abs());
      Assert.AreEqual(FP107.Divide(1, 5), FP107.Divide(-1, 5).Abs());
      Assert.AreEqual(FP107.PositiveInfinity, FP107.PositiveInfinity.Abs());
      Assert.AreEqual(FP107.PositiveInfinity, FP107.NegativeInfinity.Abs());
      Assert.AreEqual(FP107.NaN, FP107.NaN.Abs());

      // test the Ceiling function
      Assert.AreEqual((FP107)1, new FP107(1).Ceiling());
      Assert.AreEqual((FP107)2, new FP107(1.25).Ceiling());
      Assert.AreEqual((FP107)2, new FP107(1.75).Ceiling());
      Assert.AreEqual((FP107)2, new FP107(2).Ceiling());
      Assert.AreEqual((FP107)(-1), new FP107(-1).Ceiling());
      Assert.AreEqual((FP107)(-1), new FP107(-1.25).Ceiling());
      Assert.AreEqual((FP107)(-1), new FP107(-1.75).Ceiling());
      Assert.AreEqual((FP107)(-2), new FP107(-2).Ceiling());
      Assert.AreEqual(new FP107(IEEE754.MaxDoubleInt+1), FP107.Add(IEEE754.MaxDoubleInt, 0.9).Ceiling());
      Assert.AreEqual(new FP107(IEEE754.MaxDoubleInt), FP107.Add(IEEE754.MaxDoubleInt, -0.5).Ceiling());
      Assert.AreEqual(new FP107(-IEEE754.MaxDoubleInt+1), FP107.Add(-IEEE754.MaxDoubleInt, 0.5).Ceiling());
      Assert.AreEqual(new FP107(-IEEE754.MaxDoubleInt), FP107.Add(-IEEE754.MaxDoubleInt, -0.5).Ceiling());
      Assert.AreEqual(FP107.PositiveInfinity, FP107.PositiveInfinity.Ceiling());
      Assert.AreEqual(FP107.NegativeInfinity, FP107.NegativeInfinity.Ceiling());
      Assert.AreEqual(FP107.NaN, FP107.NaN.Ceiling());

      // test the Floor function
      Assert.AreEqual((FP107)1, new FP107(1).Floor());
      Assert.AreEqual((FP107)1, new FP107(1.25).Floor());
      Assert.AreEqual((FP107)1, new FP107(1.75).Floor());
      Assert.AreEqual((FP107)2, new FP107(2).Floor());
      Assert.AreEqual((FP107)(-1), new FP107(-1).Floor());
      Assert.AreEqual((FP107)(-2), new FP107(-1.25).Floor());
      Assert.AreEqual((FP107)(-2), new FP107(-1.75).Floor());
      Assert.AreEqual((FP107)(-2), new FP107(-2).Floor());
      Assert.AreEqual(new FP107(IEEE754.MaxDoubleInt), FP107.Add(IEEE754.MaxDoubleInt, 0.9).Floor());
      Assert.AreEqual(new FP107(IEEE754.MaxDoubleInt-1), FP107.Add(IEEE754.MaxDoubleInt, -0.5).Floor());
      Assert.AreEqual(new FP107(-IEEE754.MaxDoubleInt), FP107.Add(-IEEE754.MaxDoubleInt, 0.5).Floor());
      Assert.AreEqual(new FP107(-IEEE754.MaxDoubleInt-1), FP107.Add(-IEEE754.MaxDoubleInt, -0.5).Floor());
      Assert.AreEqual(FP107.PositiveInfinity, FP107.PositiveInfinity.Floor());
      Assert.AreEqual(FP107.NegativeInfinity, FP107.NegativeInfinity.Floor());
      Assert.AreEqual(FP107.NaN, FP107.NaN.Floor());

      // test Min and Max
      Assert.AreEqual(FP107.NegativeInfinity, FP107.Min(FP107.PositiveInfinity, FP107.NegativeInfinity));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Max(FP107.PositiveInfinity, FP107.NegativeInfinity));
      Assert.AreEqual(FP107.E, FP107.Min(FP107.Pi, FP107.E));
      Assert.AreEqual(FP107.Pi, FP107.Max(FP107.Pi, FP107.E));

      // test the Round function
      Assert.AreEqual((FP107)1, new FP107(1).Round());
      Assert.AreEqual((FP107)1, new FP107(1.25).Round());
      Assert.AreEqual((FP107)2, new FP107(1.5).Round());
      Assert.AreEqual((FP107)2, new FP107(1.75).Round());
      Assert.AreEqual((FP107)2, new FP107(2).Round());
      Assert.AreEqual((FP107)2, new FP107(2.5).Round());
      Assert.AreEqual((FP107)(-1), new FP107(-1).Round());
      Assert.AreEqual((FP107)(-1), new FP107(-1.25).Round());
      Assert.AreEqual((FP107)(-2), new FP107(-1.5).Round());
      Assert.AreEqual((FP107)(-2), new FP107(-1.75).Round());
      Assert.AreEqual((FP107)(-2), new FP107(-2).Round());
      Assert.AreEqual((FP107)(-2), new FP107(-2.5).Round());
      Assert.AreEqual(new FP107(IEEE754.MaxDoubleInt), FP107.Add(IEEE754.MaxDoubleInt, 0.5).Round());
      Assert.AreEqual(new FP107(IEEE754.MaxDoubleInt+1), FP107.Add(IEEE754.MaxDoubleInt, 0.9).Round());
      Assert.AreEqual(new FP107(IEEE754.MaxDoubleInt), FP107.Add(IEEE754.MaxDoubleInt, -0.5).Round());
      Assert.AreEqual(new FP107(-IEEE754.MaxDoubleInt), FP107.Add(-IEEE754.MaxDoubleInt, 0.5).Round());
      Assert.AreEqual(new FP107(-IEEE754.MaxDoubleInt), FP107.Add(-IEEE754.MaxDoubleInt, -0.5).Round());
      Assert.AreEqual(new FP107(-IEEE754.MaxDoubleInt-1), FP107.Add(-IEEE754.MaxDoubleInt, -0.6).Round());
      Assert.AreEqual(FP107.PositiveInfinity, FP107.PositiveInfinity.Round());
      Assert.AreEqual(FP107.NegativeInfinity, FP107.NegativeInfinity.Round());
      Assert.AreEqual(FP107.NaN, FP107.NaN.Round());
      // create large values than end in 0.5, and that either have an even or odd whole part to test a special case where Math.Round()
      // would round the high component towards even when, due to the offset in the low component, the value should be rounded toward odd
      double hiEven = IEEE754.RawComposeDouble(false, 1074, (1UL<<52)-3), hiOdd = IEEE754.RawComposeDouble(false, 1074, (1UL<<52)-1);
      Assert.AreEqual(new FP107(hiEven+0.5), FP107.Add(hiEven, 0.25).Round());
      Assert.AreEqual(new FP107(hiEven-0.5), FP107.Add(hiEven, -0.25).Round());
      Assert.AreEqual(new FP107(hiOdd+0.5), FP107.Add(hiOdd, 0.25).Round());
      Assert.AreEqual(new FP107(hiOdd-0.5), FP107.Add(hiOdd, -0.25).Round());
      Assert.AreEqual(new FP107(-hiEven+0.5), FP107.Add(-hiEven, 0.25).Round());
      Assert.AreEqual(new FP107(-hiEven-0.5), FP107.Add(-hiEven, -0.25).Round());
      Assert.AreEqual(new FP107(-hiOdd+0.5), FP107.Add(-hiOdd, 0.25).Round());
      Assert.AreEqual(new FP107(-hiOdd-0.5), FP107.Add(-hiOdd, -0.25).Round());

      // test the Sign function
      Assert.AreEqual(1, new FP107(1).Sign());
      Assert.AreEqual(0, FP107.Zero.Sign());
      Assert.AreEqual(-1, new FP107(-1).Sign());
      Assert.AreEqual(1, FP107.PositiveInfinity.Sign());
      Assert.AreEqual(-1, FP107.NegativeInfinity.Sign());
      TestHelpers.TestException<ArithmeticException>(delegate { FP107.NaN.Sign(); });

      // test the Random function. it's hard to test a random function, but we can at least make sure it isn't obviously broken
      Random.RandomNumberGenerator rng = Random.RandomNumberGenerator.CreateDefault();
      for(int i=0; i<100; i++)
      {
        FP107 value = FP107.Random(rng);
        Assert.IsTrue(value >= 0 && value < 1);
      }

      // test the Truncate function
      Assert.AreEqual((FP107)1, new FP107(1).Truncate());
      Assert.AreEqual((FP107)1, new FP107(1.25).Truncate());
      Assert.AreEqual((FP107)1, new FP107(1.75).Truncate());
      Assert.AreEqual((FP107)2, new FP107(2).Truncate());
      Assert.AreEqual((FP107)(-1), new FP107(-1).Truncate());
      Assert.AreEqual((FP107)(-1), new FP107(-1.25).Truncate());
      Assert.AreEqual((FP107)(-1), new FP107(-1.75).Truncate());
      Assert.AreEqual((FP107)(-2), new FP107(-2).Truncate());
      Assert.AreEqual(new FP107(IEEE754.MaxDoubleInt), FP107.Add(IEEE754.MaxDoubleInt, 0.9).Truncate());
      Assert.AreEqual(new FP107(IEEE754.MaxDoubleInt-1), FP107.Add(IEEE754.MaxDoubleInt, -0.5).Truncate());
      Assert.AreEqual(new FP107(-IEEE754.MaxDoubleInt+1), FP107.Add(-IEEE754.MaxDoubleInt, 0.5).Truncate());
      Assert.AreEqual(new FP107(-IEEE754.MaxDoubleInt), FP107.Add(-IEEE754.MaxDoubleInt, -0.5).Truncate());
      Assert.AreEqual(FP107.NaN, FP107.NaN.Truncate());
      Assert.AreEqual(FP107.PositiveInfinity, FP107.PositiveInfinity.Truncate());
      Assert.AreEqual(FP107.NegativeInfinity, FP107.NegativeInfinity.Truncate());
    }

    [Test]
    public void FP107_PowerFunctions()
    {
      CultureInfo inv = CultureInfo.InvariantCulture;

      // test the Exp function
      Assert.AreEqual(FP107.NaN, FP107.Exp(FP107.NaN)); // exp(NaN) = NaN
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Exp(FP107.PositiveInfinity)); // exp(+Infinity) = +Infinity
      Assert.AreEqual(FP107.Zero, FP107.Exp(FP107.NegativeInfinity)); // exp(-Infinity) = 0
      // integer powers should take an optimized path
      Assert.AreEqual(1/FP107.E, FP107.Exp(-1)); // e^-1 = 1/e (exact)
      Assert.AreEqual(FP107.One, FP107.Exp(0)); // e^0 = 1 (exact)
      Assert.AreEqual(FP107.E, FP107.Exp(1)); // e^1 = e (exact)
      Assert.AreEqual(FP107.E*FP107.E, FP107.Exp(2));
      Assert.AreEqual(FP107.Parse("20.085536923187667740928529654581718", inv), FP107.Exp(3));
      // roots should also take an optimized path
      Assert.AreEqual(FP107.E.Sqrt(), FP107.Exp(0.5));
      Assert.AreEqual(FP107.E.Root(3), FP107.Exp(FP107.One/3));
      // now test powers that avoid the optimized paths
      Assert.Less(RelativeError(FP107.Parse("3.4903429574618413761305460296722655", inv), FP107.Exp(1.25)), (FP107)8.83e-33);
      Assert.Less(RelativeError(FP107.Parse("3.4516107331259239871361985995265747e+43", inv), FP107.Exp(100.25)), (FP107)1.72e-31);
      Assert.Less(RelativeError(FP107.Parse("1.0552644065332766916488973568515697e+308", inv), FP107.Exp(709.25)), (FP107)2.78e-30);
      Assert.AreEqual(FP107.Parse("0.28650479686019010032488542664783760", inv), FP107.Exp(-1.25));
      Assert.Less(RelativeError(FP107.Parse("0.000035357500850409982404587639763277425", inv), FP107.Exp(-10.25)), (FP107)3.73e-30);

      // test the Log function
      // test the Log(FP107) variant
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Log(FP107.PositiveInfinity));
      Assert.AreEqual(FP107.NegativeInfinity, FP107.Log(FP107.Zero));
      Assert.AreEqual(FP107.NaN, FP107.Log(FP107.MinusOne));
      Assert.AreEqual(FP107.NaN, FP107.Log(FP107.NaN));
      Assert.AreEqual(FP107.Zero, FP107.Log(FP107.One));
      Assert.AreEqual(FP107.One, FP107.Log(FP107.E));
      Assert.AreEqual(FP107.Parse("0.69314718055994530941723212145817657", inv), FP107.Log((FP107)2));
      Assert.AreEqual(FP107.Parse("-0.69314718055994530941723212145817657", inv), FP107.Log((FP107)0.5));
      Assert.AreEqual(FP107.Parse("1.14472988584940017414342735135305", inv), FP107.Log(FP107.Pi));
      Assert.Less(RelativeError(FP107.Parse("3.9120230054281460586187507879105518", inv), FP107.Log((FP107)50)), (FP107)6.31e-33);
      Assert.Less(RelativeError(460 + FP107.Parse(".517018598809136803598290936872840", inv), FP107.Log(FP107.Parse("1e+200", inv))), (FP107)5.42e-31);
      Assert.Less(RelativeError(688 + FP107.Parse(".4729428052196595213794449506249", inv), FP107.Log(FP107.Parse("1e+299", inv))), (FP107)5.92e-28);
      // test the Log(FP107,FP107) variant
      Assert.AreEqual(FP107.NaN, FP107.Log(-1, 2)); // test a mess of special cases
      Assert.AreEqual(FP107.NaN, FP107.Log(1, -1));
      Assert.AreEqual(FP107.NaN, FP107.Log(2, 0));
      Assert.AreEqual(FP107.NaN, FP107.Log(0, 0));
      Assert.AreEqual(FP107.NaN, FP107.Log(0, double.PositiveInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Log(double.NaN, 2));
      Assert.AreEqual(FP107.NaN, FP107.Log(2, double.NaN));
      Assert.AreEqual(FP107.NaN, FP107.Log(2, 1));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Log(0, 0.5));
      Assert.AreEqual(FP107.NegativeInfinity, FP107.Log(0, 1.5));
      Assert.AreEqual(FP107.NegativeInfinity, FP107.Log(double.PositiveInfinity, 0.5));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Log(double.PositiveInfinity, 1.5));
      Assert.AreEqual(FP107.Zero, FP107.Log(1, 0));
      Assert.AreEqual(FP107.Zero, FP107.Log(1, double.PositiveInfinity));
      Assert.AreEqual(FP107.One, FP107.Log(FP107.E, FP107.E));
      Assert.AreEqual(FP107.One, FP107.Log(2, 2));
      Assert.AreEqual(FP107.Log(100000), FP107.Log(100000, FP107.E));
      Assert.Less(RelativeError(FP107.Parse("6.6438561897747246957406388589787804", inv), FP107.Log(100, 2)), (FP107)2.23e-32);
      Assert.Less(RelativeError(2, FP107.Log(100, 10)), (FP107)1.08e-32);
      Assert.Less(RelativeError(FP107.FromComponents(299, 1.194350409917695660743110166992163e-33), FP107.Log(FP107.Parse("1e299"), 10)), (FP107)5.92e-28);
      Assert.Less(RelativeError(FP107.FromComponents(100, 9.9372793290423189596e-35), FP107.Log(FP107.Parse("1e300"), 1000)), (FP107)5.96e-27);

      // test the Log10 function
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Log10(FP107.PositiveInfinity));
      Assert.AreEqual(FP107.NegativeInfinity, FP107.Log10(FP107.Zero));
      Assert.AreEqual(FP107.NaN, FP107.Log10(FP107.MinusOne));
      Assert.AreEqual(FP107.NaN, FP107.Log10(FP107.NaN));
      Assert.AreEqual(FP107.Zero, FP107.Log10(FP107.One));
      Assert.AreEqual(FP107.Parse("9.6329598612473982468396446311837769", inv), FP107.Log10(FP107.Pow(2, 32)));
      Assert.Less(RelativeError(FP107.Parse("38.531839444989592987358578524735107", inv), FP107.Log10(FP107.Pow(2, 128))), (FP107)4.1e-32);
      Assert.Less(RelativeError(199 + FP107.Parse(".88391712088351362192262609706337", inv), FP107.Log10(FP107.Pow(2,664))), (FP107)1.66e-31);
      Assert.Less(RelativeError(299 + FP107.Parse("2.171472409461972328019043986726559e-11", inv), FP107.Log10(FP107.Parse("100000000005e288", inv))), (FP107)4.39e-28);
      for(int i=-300; i <= 300; i++) // Log10 is supposed to be exact from 10^-300 to 10^300 and relatively unambiguous from 10^-28 to 10^28
      {
        FP107 value = FP107.Pow(10, i);
        FP107 log = value.Log10();
        Assert.AreEqual((FP107)i, log); // make sure the logarithm is exact
        if(Math.Abs(i) <= 28)
        {
          // make sure it's sufficiently unambiguous
          if(i >= 0)
          {
            Assert.AreNotEqual((FP107)i, (value-1).Log10());
            Assert.AreNotEqual((FP107)i, (value+1).Log10());
          }
          else
          {
            value = FP107.Pow(10, -i);
            Assert.AreNotEqual((FP107)i, (1/(value-1)).Log10());
            Assert.AreNotEqual((FP107)i, (1/(value+1)).Log10());
          }
        }
      }

      // test the Pow function
      // test the Pow(FP107,int) variant
      Assert.AreEqual(FP107.NaN, FP107.Pow(FP107.NaN, 0));
      Assert.AreEqual(FP107.One, FP107.Pow(1, 0));
      Assert.AreEqual(FP107.Zero, FP107.Pow(FP107.NegativeInfinity, -1));
      Assert.AreEqual(FP107.NegativeInfinity, FP107.Pow(FP107.NegativeInfinity, 1));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Pow(FP107.NegativeInfinity, 2));
      Assert.AreEqual(FP107.Zero, FP107.Pow(FP107.PositiveInfinity, -1));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Pow(FP107.PositiveInfinity, 1));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Pow(0, -1));
      Assert.AreEqual(FP107.Zero, FP107.Pow(0, 1));
      Assert.AreEqual(FP107.One, FP107.Pow(1, -1));
      Assert.AreEqual((FP107)IEEE754.ComposeDouble(false, 1000, 1), FP107.Pow(2, 1000)); // test that powers of two are exact
      Assert.AreEqual((FP107)IEEE754.ComposeDouble(false, -1000, 1), FP107.Pow(2, -1000));
      Assert.AreEqual((FP107)4052555153018976267UL, FP107.Pow(3, 39));
      Assert.AreEqual((FP107)4052555153018976267UL, FP107.Pow(3, 39));
      Assert.AreEqual(FP107.Parse("5.169878828456422967946304325437268e+16", inv), FP107.Pow(2.5, 42));
      Assert.Less(RelativeError(FP107.Parse("1.934281311383406679529881600000000e-17", inv), FP107.Pow(2.5, -42)), (FP107)1.77e-32);
      Assert.Less(RelativeError(FP107.Parse("1e308", inv), FP107.Pow(10, 308)), (FP107)1.45e-31);
      Assert.Less(RelativeError(FP107.Parse("1e-290", inv), FP107.Pow(10, -290)), (FP107)1.51e-31);
      Assert.AreEqual(FP107.Parse("1e-300", inv), FP107.Pow(10, -300));
      // test the Pow(FP107,FP107) variant
      Assert.AreEqual(FP107.NaN, FP107.Pow(FP107.NaN, FP107.Zero)); // test a ton of special cases
      Assert.AreEqual(FP107.NaN, FP107.Pow(FP107.Zero, FP107.NaN));
      Assert.AreEqual(FP107.One, FP107.Pow(FP107.Zero, FP107.Zero));
      Assert.AreEqual(FP107.Zero, FP107.Pow(FP107.NegativeInfinity, -2.5));
      Assert.AreEqual(FP107.NegativeInfinity, FP107.Pow(FP107.NegativeInfinity, 1d));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Pow(FP107.NegativeInfinity, 2d));
      Assert.AreEqual(FP107.NaN, FP107.Pow(-1, 1.5));
      Assert.AreEqual(FP107.NaN, FP107.Pow(-1, FP107.PositiveInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Pow(-1, FP107.NegativeInfinity));
      Assert.AreEqual(FP107.Zero, FP107.Pow(0.5, FP107.PositiveInfinity));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Pow(0.5, FP107.NegativeInfinity));
      Assert.AreEqual(FP107.Zero, FP107.Pow(-0.5, FP107.PositiveInfinity));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Pow(-0.5, FP107.NegativeInfinity));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Pow(1.5, FP107.PositiveInfinity));
      Assert.AreEqual(FP107.Zero, FP107.Pow(1.5, FP107.NegativeInfinity));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Pow(-1.5, FP107.PositiveInfinity));
      Assert.AreEqual(FP107.Zero, FP107.Pow(-1.5, FP107.NegativeInfinity));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Pow(0, -0.5));
      Assert.AreEqual(FP107.Zero, FP107.Pow(FP107.Zero, 0.5));
      Assert.AreEqual(FP107.One, FP107.Pow(FP107.One, FP107.Zero));
      Assert.AreEqual(FP107.Zero, FP107.Pow(FP107.PositiveInfinity, -0.5));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Pow(FP107.PositiveInfinity, 0.5));
      Assert.AreEqual((FP107)IEEE754.ComposeDouble(false, 1000, 1), FP107.Pow(2d, 1000)); // test that powers of two are exact
      Assert.AreEqual((FP107)IEEE754.ComposeDouble(false, -1000, 1), FP107.Pow(2d, -1000));
      Assert.AreEqual(FP107.Pi.Sqrt(), FP107.Pi.Pow(0.5)); // test the root optimization path
      Assert.AreEqual(FP107.Pi.Root(3), FP107.Pi.Pow(FP107.One/3));
      Assert.AreEqual((FP107)(-3), FP107.Pow(-27, FP107.One/3));
      Assert.AreEqual(FP107.Exp(4.2), FP107.Pow(FP107.E, 4.2)); // test the Exp detection path
      Assert.Less(RelativeError(FP107.Parse("3162.2776601683793319988935444327185", inv), FP107.Pow(10, 3.5)), (FP107)1.6e-32);
      Assert.Less(RelativeError(FP107.Parse("0.00031622776601683793319988935444327185", inv), FP107.Pow(10, -3.5)), (FP107)4.76e-33);
      Assert.Less(RelativeError(FP107.Parse("3.1622776601683793319988935444327185e300", inv), FP107.Pow(10, 300.5)), (FP107)1.7e-30);
      Assert.AreEqual(FP107.Parse("3.1622776601683793319988935444327185e-301", inv), FP107.Pow(10, -300.5));

      // test the Root function
      Assert.AreEqual(FP107.NaN, FP107.Root(FP107.NaN, 3));
      Assert.AreEqual(FP107.NaN, FP107.Root(1, 0));
      Assert.AreEqual(FP107.NaN, FP107.Root(1, -1));
      Assert.AreEqual(FP107.NaN, FP107.Root(-1, 2));
      Assert.AreEqual(FP107.Zero, FP107.Root(0, 3));
      Assert.AreEqual(FP107.GoldenRatio.Sqrt(), FP107.Root(FP107.GoldenRatio, 2));
      Assert.Less(RelativeError(FP107.Parse("1.2599210498948731647672106072782284", inv), FP107.Root(2, 3)), (FP107)4.9e-33);
      Assert.Less(RelativeError(FP107.Parse("6.5810958916722235810416644749177087", inv), FP107.Root(12345, 5)), (FP107)6.93e-32);
      Assert.Less(RelativeError(FP107.Parse("1.873817422860384047760333287008379e27", inv), FP107.Root(FP107.Pow(10, 300), 11)), (FP107)1.71e-25);
      Assert.AreEqual((FP107)(-3), FP107.Root(-27, 3));
      Assert.Less(RelativeError(FP107.Divide(-1, 7), FP107.Root(FP107.Divide(-1, 823543), 7)), (FP107)6.48e-32);

      // test the Sqrt function
      Assert.AreEqual(FP107.NaN, FP107.Sqrt(FP107.MinusOne));
      Assert.AreEqual(FP107.NaN, FP107.Sqrt(FP107.NaN));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Sqrt(FP107.PositiveInfinity));
      Assert.AreEqual(FP107.Zero, FP107.Sqrt(FP107.Zero));
      Assert.AreEqual(FP107.One, FP107.Sqrt(FP107.One));
      Assert.AreEqual((FP107)8, FP107.Sqrt(64));
      Assert.AreEqual((FP107)256, FP107.Sqrt(65536));
      Assert.AreEqual((FP107)65536, FP107.Sqrt(1UL<<32));
      Assert.Less(RelativeError(FP107.Parse("1.4142135623730950488016887242096981", inv), FP107.Sqrt(2)), (FP107)1.75e-32);
      Assert.Less(RelativeError(FP107.Parse("111.10805551354051124500443874307524", inv), FP107.Sqrt(12345)), (FP107)7.1e-33);
      Assert.Less(RelativeError(FP107.Parse("1e150", inv), FP107.Sqrt(FP107.Pow(10, 300))), (FP107)8.58e-32);
    }

    [Test]
    public void FP107_Trigonometry()
    {
      CultureInfo inv = CultureInfo.InvariantCulture;

      // test the Cos, Sin, Tan, Cosh, Sinh, and Tanh functions
      Assert.AreEqual(FP107.NaN, FP107.Cos(FP107.NaN));
      Assert.AreEqual(FP107.NaN, FP107.Cos(FP107.PositiveInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Cos(FP107.NegativeInfinity));
      Assert.AreEqual(FP107.One, FP107.Cos(0));
      Assert.AreEqual(FP107.Zero, FP107.Cos(FP107.PiOverTwo));
      Assert.AreEqual(FP107.Zero, FP107.Cos(-FP107.PiOverTwo));

      Assert.AreEqual(FP107.NaN, FP107.Sin(FP107.NaN));
      Assert.AreEqual(FP107.NaN, FP107.Sin(FP107.PositiveInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Sin(FP107.NegativeInfinity));
      Assert.AreEqual(FP107.Zero, FP107.Sin(0));
      Assert.AreEqual(FP107.One, FP107.Sin(FP107.PiOverTwo));
      Assert.AreEqual(FP107.MinusOne, FP107.Sin(-FP107.PiOverTwo));

      Assert.AreEqual(FP107.NaN, FP107.Tan(FP107.NaN));
      Assert.AreEqual(FP107.NaN, FP107.Tan(FP107.PositiveInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Tan(FP107.NegativeInfinity));
      Assert.AreEqual(FP107.Zero, FP107.Tan(FP107.Pi));
      Assert.AreEqual(FP107.Zero, FP107.Tan(-FP107.Pi));
      Assert.IsTrue(FP107.Tan(FP107.PiOverTwo).IsInfinity);
      Assert.IsTrue(FP107.Tan(-FP107.PiOverTwo).IsInfinity);

      Assert.AreEqual(FP107.NaN, FP107.Cosh(FP107.NaN));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Cosh(FP107.PositiveInfinity));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Cosh(FP107.NegativeInfinity));
      Assert.AreEqual(FP107.One, FP107.Cosh(FP107.Zero));
      Assert.AreEqual(FP107.NaN, FP107.Sinh(FP107.NaN));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Sinh(FP107.PositiveInfinity));
      Assert.AreEqual(FP107.NegativeInfinity, FP107.Sinh(FP107.NegativeInfinity));
      Assert.AreEqual(FP107.Zero, FP107.Sinh(FP107.Zero));
      Assert.AreEqual(FP107.NaN, FP107.Tanh(FP107.NaN));
      Assert.AreEqual(FP107.One, FP107.Tanh(FP107.PositiveInfinity));
      Assert.AreEqual(FP107.MinusOne, FP107.Tanh(FP107.NegativeInfinity));
      Assert.AreEqual(FP107.Zero, FP107.Sinh(FP107.Zero));

      // test that the functions are roughly correct over a wide range (-4Pi to 4Pi)
      for(int i=-256; i<=256; i++)
      {
        FP107 value = FP107.Pi * (i/64.0);
        double dvalue = Math.PI * (i/64.0);

        FP107 sin = FP107.Sin(value), cos = FP107.Cos(value), tan = FP107.Tan(value), sin2, cos2;
        FP107.SinCos(value, out sin2, out cos2);
        Assert.AreEqual(sin, value.Sin());
        Assert.AreEqual(cos, value.Cos());
        Assert.AreEqual(tan, value.Tan());
        Assert.Less(AbsoluteError(sin, sin2), (FP107)5e-32); // these may differ slightly because different calculations are used
        Assert.Less(AbsoluteError(cos, cos2), (FP107)5e-32);
        Assert.Less(Math.Abs(Math.Sin(dvalue)-(double)sin), 2e-15); // Math.Sin/Cos() and aren't as accurate as one would like.
        Assert.Less(Math.Abs(Math.Cos(dvalue)-(double)cos), 2e-15); // but i haven't verified it's Math's fault for every value

        double lotan = Math.Tan(dvalue);
        if(tan.Abs() > 1e16) Assert.Greater(Math.Abs(lotan), 1e14); // check that when one is near infinity, the other is also
        else Assert.Less(Math.Abs(lotan-(double)tan), 3e-13); // Math.Tan() is even less accurate

        FP107 sinh = FP107.Sinh(value), cosh = FP107.Cosh(value), tanh = FP107.Tanh(value);
        Assert.AreEqual(sinh, value.Sinh());
        Assert.AreEqual(cosh, value.Cosh());
        Assert.AreEqual(tanh, value.Tanh());
        Assert.Less(Math.Abs(Math.Sinh(dvalue)-(double)sinh), 2e-10);
        Assert.Less(Math.Abs(1 - Math.Cosh(dvalue)/(double)cosh), 2e-15);
        Assert.Less(Math.Abs(1 - Math.Tanh(dvalue)/(double)tanh), 2e-15);
      }

      // test a couple values for high precision (Pi/64 and Pi/32) with all the functions
      Assert.Less(RelativeError(FP107.Parse("0.04906767432741801425495497694268266", inv), FP107.Sin(FP107.Pi/64)), (FP107)7.86e-33);
      Assert.Less(RelativeError(FP107.Parse("0.09801714032956060199419556388864185", inv), FP107.Sin(FP107.Pi/32)), (FP107)5.31e-32);
      Assert.Less(RelativeError(FP107.Parse("0.9987954562051723927147716047591007", inv), FP107.Cos(FP107.Pi/64)), (FP107)1.55e-33);
      Assert.Less(RelativeError(FP107.Parse("0.9951847266721968862448369531094799", inv), FP107.Cos(FP107.Pi/32)), (FP107)6.2e-33);
      Assert.Less(RelativeError(FP107.Parse("0.04912684976946725410534332127131362", inv), FP107.Tan(FP107.Pi/64)), (FP107)8.58e-33);
      Assert.Less(RelativeError(FP107.Parse("0.09849140335716425307719752129132743", inv), FP107.Tan(FP107.Pi/32)), (FP107)3.92e-32);
      Assert.Less(RelativeError(FP107.Parse("0.04910710084731371218778646310926753", inv), FP107.Sinh(FP107.Pi/64)), (FP107)5.89e-33);
      Assert.Less(RelativeError(FP107.Parse("0.09833255252142786072705283378432444", inv), FP107.Sinh(FP107.Pi/32)), (FP107)2.75e-32);
      Assert.Less(RelativeError(FP107.Parse("1.001205027631018360693523591634050", inv), FP107.Cosh(FP107.Pi/64)), (FP107)2.47e-32);
      Assert.AreEqual(FP107.Parse("1.004823014707256478218988448213107", inv), FP107.Cosh(FP107.Pi/32));
      Assert.Less(RelativeError(FP107.Parse("0.04904799665609701911989570473855705", inv), FP107.Tanh(FP107.Pi/64)), (FP107)3.92e-32);
      Assert.Less(RelativeError(FP107.Parse("0.09786056955520262292373795675302364", inv), FP107.Tanh(FP107.Pi/32)), (FP107)4.73e-32);
      // Sinh takes a different path if the magnitude is small
      Assert.Less(RelativeError(FP107.Parse("0.003906259934115041690372470979145619", inv), FP107.Sinh(FP107.Pow(2,-8))), (FP107)6.17e-32);

      // test the hyperbolic functions with values that give somewhat extreme results to make sure the quality doesn't degrade too much
      Assert.Less(RelativeError(FP107.Parse("3.317811999670491765769362530319015e6", inv), FP107.Sinh(FP107.Pi*5)), (FP107)1.56e-32);
      Assert.Less(RelativeError(FP107.Parse("3.317811999670642467496901536780090e6", inv), FP107.Cosh(FP107.Pi*5)), (FP107)2.15e-32);
      Assert.Less(RelativeError(FP107.Parse("0.9999999999999545779786335191548064", inv), FP107.Tanh(FP107.Pi*5)), (FP107)1.24e-32);

      // test the Acos, Asin, and ATan functions
      Assert.AreEqual(FP107.NaN, FP107.Acos(FP107.NaN));
      Assert.AreEqual(FP107.NaN, FP107.Acos(FP107.PositiveInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Acos(FP107.NegativeInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Acos(-1.01));
      Assert.AreEqual(FP107.NaN, FP107.Acos(1.01));
      Assert.AreEqual(FP107.Pi, FP107.Acos(-1));
      Assert.AreEqual(FP107.PiOverTwo, FP107.Acos(0));
      Assert.AreEqual(FP107.Zero, FP107.Acos(1));

      Assert.AreEqual(FP107.NaN, FP107.Asin(FP107.NaN));
      Assert.AreEqual(FP107.NaN, FP107.Asin(FP107.PositiveInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Asin(FP107.NegativeInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Asin(-1.01));
      Assert.AreEqual(FP107.NaN, FP107.Asin(1.01));
      Assert.AreEqual(-FP107.PiOverTwo, FP107.Asin(-1));
      Assert.AreEqual(FP107.Zero, FP107.Asin(0));
      Assert.AreEqual(FP107.PiOverTwo, FP107.Asin(1));

      Assert.AreEqual(FP107.NaN, FP107.Atan(FP107.NaN));
      Assert.AreEqual(-FP107.PiOverTwo, FP107.Atan(FP107.NegativeInfinity));
      Assert.AreEqual(FP107.PiOverTwo, FP107.Atan(FP107.PositiveInfinity));
      Assert.AreEqual(FP107.Zero, FP107.Atan(FP107.Zero));

      // test that they're roughly the same as the Math equivalents
      for(int i=-10; i<=10; i++)
      {
        FP107 value = FP107.Divide(i, 10);
        double dvalue = i/10.0;
        FP107 acos = FP107.Acos(value), asin = FP107.Asin(value), atan = FP107.Atan(value);
        Assert.AreEqual(acos, value.Acos());
        Assert.AreEqual(asin, value.Asin());
        Assert.AreEqual(atan, value.Atan());
        Assert.Less(Math.Abs(Math.Acos(dvalue)-(double)acos), 5e-16);
        Assert.Less(Math.Abs(Math.Asin(dvalue)-(double)asin), 5e-16);
        Assert.Less(Math.Abs(Math.Atan(dvalue)-(double)atan), 5e-16);
      }

      // test a couple values for high precision (2^-52 and 1-2^-52)
      Assert.AreEqual(FP107.Parse("1.570796326794896397186716766608443", inv), FP107.Acos(FP107.Pow(2,-52)));
      Assert.Less(RelativeError(FP107.Parse("2.107342425544701628342116428821212e-8", inv), FP107.Acos(1-FP107.Pow(2, -52))), (FP107)8.72e-33);
      Assert.Less(RelativeError(FP107.Parse("2.220446049250313080847263336181659e-16", inv), FP107.Asin(FP107.Pow(2, -52))), (FP107)6.2e-33);
      Assert.AreEqual(FP107.Parse("1.570796305721472363784305408218587", inv), FP107.Asin(1-FP107.Pow(2, -52)));
      Assert.Less(RelativeError(FP107.Parse("2.220446049250313080847263336181604e-16", inv), FP107.Atan(FP107.Pow(2, -52))), (FP107)4.11e-33);
      Assert.Less(RelativeError(FP107.Parse("0.7853981633974481985933583833042094", inv), FP107.Atan(1-FP107.Pow(2, -52))), (FP107)7.85e-33);
      Assert.AreEqual(FP107.Parse("-1.550798992821746086170568494738155", inv), FP107.Atan(-50));

      // test the Atan2 function
      Assert.AreEqual(FP107.NaN, FP107.Atan2(FP107.NaN, 1));
      Assert.AreEqual(FP107.NaN, FP107.Atan2(1, FP107.NaN));
      Assert.AreEqual(FP107.NaN, FP107.Atan2(FP107.NaN, FP107.PositiveInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Atan2(FP107.PositiveInfinity, FP107.NaN));
      Assert.AreEqual(FP107.NaN, FP107.Atan2(FP107.NaN, FP107.NegativeInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Atan2(FP107.NegativeInfinity, FP107.NaN));
      Assert.AreEqual(FP107.NaN, FP107.Atan2(FP107.PositiveInfinity, FP107.PositiveInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Atan2(FP107.PositiveInfinity, FP107.NegativeInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Atan2(FP107.NegativeInfinity, FP107.PositiveInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Atan2(FP107.NegativeInfinity, FP107.NegativeInfinity));
      Assert.AreEqual(FP107.PiOverTwo, FP107.Atan2(FP107.PositiveInfinity, 10));
      Assert.AreEqual(FP107.PiOverTwo, FP107.Atan2(FP107.PositiveInfinity, -10));
      Assert.AreEqual(FP107.Zero, FP107.Atan2(1, FP107.PositiveInfinity));
      Assert.AreEqual(FP107.Pi, FP107.Atan2(1, FP107.NegativeInfinity));
      Assert.AreEqual(-FP107.PiOverTwo, FP107.Atan2(FP107.NegativeInfinity, 10));
      Assert.AreEqual(-FP107.PiOverTwo, FP107.Atan2(FP107.NegativeInfinity, -10));
      Assert.AreEqual(FP107.Pi, FP107.Atan2(1, FP107.NegativeInfinity));
      Assert.AreEqual(-FP107.Pi, FP107.Atan2(-1, FP107.NegativeInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Atan2(0, 0));
      Assert.AreEqual(FP107.Zero, FP107.Atan2(0, 1));
      Assert.AreEqual(FP107.Zero, FP107.Atan2(0, FP107.PositiveInfinity));
      Assert.AreEqual(FP107.Pi, FP107.Atan2(0, -1));
      Assert.AreEqual(FP107.Pi, FP107.Atan2(0, FP107.NegativeInfinity));
      Assert.AreEqual(FP107.PiOverTwo, FP107.Atan2(1, 0));
      Assert.AreEqual(FP107.PiOverTwo, FP107.Atan2(FP107.PositiveInfinity, 0));
      Assert.AreEqual(-FP107.PiOverTwo, FP107.Atan2(-1, 0));
      Assert.AreEqual(-FP107.PiOverTwo, FP107.Atan2(FP107.NegativeInfinity, 0));
      for(int y=-10; y<=10; y++)
      {
        for(int x=-10; x<=10; x++) Assert.Less(Math.Abs(Math.Atan2(y, x) - (double)FP107.Atan2(y, x)), 5e-16);
      }
      Assert.AreEqual(FP107.Parse("0.4636476090008061162142562314612144", inv), FP107.Atan2(1, 2));
      Assert.Less(RelativeError(FP107.Parse("2.553590050042225687217032302654417", inv), FP107.Atan2(2, -3)), (FP107)2.42e-33);
      Assert.Less(RelativeError(FP107.Parse("-0.9827937232473290679857106110146660", inv), FP107.Atan2(-3, 2)), (FP107)1.57e-32);
      Assert.AreEqual(FP107.Parse("-2.214297435588181006034130920357074", inv), FP107.Atan2(-4, -3));

      // test the Acosh function
      Assert.AreEqual(FP107.NaN, FP107.Acosh(FP107.NaN));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Acosh(FP107.PositiveInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Acosh(FP107.NegativeInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Acosh(0.99));
      Assert.AreEqual(FP107.Zero, FP107.Acosh(1));
      Assert.Less(RelativeError(FP107.Parse("2.107342425544701550354780375182800e-8", inv), FP107.Acosh(1+FP107.Pow(2, -52))), (FP107)5.68e-25);
      Assert.Less(RelativeError(FP107.Parse("1.348699152348506797621894773260353e-6", inv), FP107.Acosh(1+FP107.Pow(2, -40))), (FP107)4.78e-28);
      Assert.Less(RelativeError(FP107.Parse("0.0001726334912862519978925314241990132", inv), FP107.Acosh(1+FP107.Pow(2, -26))), (FP107)4.72e-29);
      Assert.AreEqual(FP107.Parse("1.316957896924816708625046347307969", inv), FP107.Acosh(2));
      Assert.Less(RelativeError(FP107.Parse("12.20607264550517372950625189487995", inv), FP107.Acosh(1e5)), (FP107)2.03e-33);
      Assert.Less(RelativeError(FP107.Parse("230.9516564799645137112163775898946", inv), FP107.Acosh(FP107.Parse("1e100"))), (FP107)3.69e-31);
      Assert.AreEqual(FP107.Acosh(1.5), new FP107(1.5).Acosh());

      // test the Asinh function
      Assert.AreEqual(FP107.NaN, FP107.Asinh(FP107.NaN));
      Assert.AreEqual(FP107.PositiveInfinity, FP107.Asinh(FP107.PositiveInfinity));
      Assert.AreEqual(FP107.NegativeInfinity, FP107.Asinh(FP107.NegativeInfinity));
      Assert.AreEqual(FP107.Zero, FP107.Asinh(0));
      Assert.AreEqual(FP107.Asinh(0.5), new FP107(0.5).Asinh());
      Assert.Less(RelativeError(FP107.Parse("1.490116119384765569854625829798161e-8", inv), FP107.Asinh(FP107.Pow(2, -26))), (FP107)1.11e-24);
      Assert.Less(RelativeError(FP107.Parse("0.8813735870195430252326093249797923", inv), FP107.Asinh(1)), (FP107)1.4e-32);
      Assert.Less(RelativeError(FP107.Parse("-4.605270170991423826621239267208306", inv), FP107.Asinh(-50)), (FP107)3.85e-30);

      // test the Atanh function
      Assert.AreEqual(FP107.NaN, FP107.Atanh(FP107.NaN));
      Assert.AreEqual(FP107.NaN, FP107.Atanh(FP107.PositiveInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Atanh(FP107.NegativeInfinity));
      Assert.AreEqual(FP107.NaN, FP107.Atanh(-1.01));
      Assert.AreEqual(FP107.NaN, FP107.Atanh(1.01));
      Assert.AreEqual(FP107.NaN, FP107.Atanh(-1));
      Assert.AreEqual(FP107.Zero, FP107.Atanh(0));
      Assert.AreEqual(FP107.NaN, FP107.Atanh(1));
      Assert.Less(RelativeError(FP107.Parse("2.220446049250313080847263336181677e-16", inv), FP107.Atanh(FP107.Pow(2, -52))), (FP107)2.77e-32);
      Assert.Less(RelativeError(FP107.Parse("18.36840028483855064404549998738385", inv), FP107.Atanh(1-FP107.Pow(2, -52))), (FP107)1.08e-32);
      Assert.AreEqual(FP107.Parse("-31.53819671547751157848406152614509", inv), FP107.Atanh(-1+FP107.Pow(2, -90)));
      Assert.AreEqual(FP107.Atanh(0.5), new FP107(0.5).Atanh());
    }

    static FP107 AbsoluteError(FP107 a, FP107 b)
    {
      return FP107.Abs(a - b);
    }

    static FP107 RelativeError(FP107 a, FP107 b)
    {
      return FP107.Abs(1 - a/b);
    }

    static FP107 RelativeError2(FP107 a, FP107 b, double epsilon=0)
    {
      if(a.IsZero) a = epsilon;
      if(b.IsZero) b = epsilon;
      return FP107.Abs(1 - a/b);
    }

    static void TestAdd(FP107 expectedSum, double a, double b)
    {
      FP107 sum = FP107.Add(a, b), sum2 = (FP107)a + b, sum3 = a + (FP107)b, sum4 = (FP107)a + (FP107)b;
      FP107 sum5 = FP107.Add(b, a), sum6 = (FP107)b + a, sum7 = b + (FP107)a, sum8 = (FP107)b + (FP107)a;
      FP107 sum9 = FP107.Add(0, a, b), sum10 = FP107.Add(b, 0, a), sum11 = FP107.Add(a, b, 0);
      Assert.AreEqual(expectedSum, sum);
      Assert.AreEqual(sum, sum2);
      Assert.AreEqual(sum2, sum3);
      Assert.AreEqual(sum3, sum4);
      Assert.AreEqual(sum4, sum5);
      Assert.AreEqual(sum5, sum6);
      Assert.AreEqual(sum6, sum7);
      Assert.AreEqual(sum7, sum8);
      Assert.AreEqual(sum8, sum9);
      Assert.AreEqual(sum9, sum10);
      Assert.AreEqual(sum10, sum11);
    }

    static void TestCurrencyFormat(bool negative, string pattern, int patternNumber)
    {
      NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
      if(negative) nfi.CurrencyNegativePattern = patternNumber;
      else nfi.CurrencyPositivePattern = patternNumber;
      pattern = pattern.Replace("$", nfi.CurrencySymbol).Replace("-", nfi.NegativeSign).Replace("n", "1");
      Assert.AreEqual(pattern, new FP107(negative ? -1 : 1).ToString("C", nfi));
      Assert.AreEqual(new FP107(negative ? -1 : 1), FP107.Parse(pattern, nfi));
    }
 
    static void TestDecomposition(double value, bool expectedNegative, int expectedExponent, ulong expectedMantissa)
    {
      bool negative;
      int exponent;
      ulong mantissa;
      IEEE754.Decompose(value, out negative, out exponent, out mantissa);
      Assert.AreEqual(expectedNegative, negative);
      Assert.AreEqual(expectedExponent, exponent);
      Assert.AreEqual(expectedMantissa, mantissa);
      Assert.AreEqual(value, IEEE754.ComposeDouble(negative, exponent, mantissa));
    }

    static void TestDecomposition(float value, bool expectedNegative, int expectedExponent, uint expectedMantissa)
    {
      bool negative;
      int exponent;
      uint mantissa;
      IEEE754.Decompose(value, out negative, out exponent, out mantissa);
      Assert.AreEqual(expectedNegative, negative);
      Assert.AreEqual(expectedExponent, exponent);
      Assert.AreEqual(expectedMantissa, mantissa);
      Assert.AreEqual(value, IEEE754.ComposeSingle(negative, exponent, mantissa));
    }

    static void TestDivide(FP107 expectedQuotient, double a, double b)
    {
      FP107 quot = FP107.Divide(a, b), quot2 = (FP107)a / b, quot3 = a / (FP107)b, quot4 = (FP107)a / (FP107)b;
      FP107 rem, quot5 = FP107.DivRem(a, b, out rem);
      Assert.AreEqual(expectedQuotient, quot);
      Assert.AreEqual(quot, quot2);
      Assert.AreEqual(quot2, quot3);
      Assert.AreEqual(quot3, quot4);
      Assert.AreEqual(quot4, quot5);
    }

    static void TestFormatRoundTrip(FP107 value, string expectedString, string format, IFormatProvider provider)
    {
      Assert.AreEqual(expectedString, value.ToString(format, provider));
      Assert.AreEqual(value, FP107.Parse(expectedString, provider));
    }

    static void TestMultiply(FP107 expectedProduct, double a, double b)
    {
      FP107 prod = FP107.Multiply(a, b), prod2 = (FP107)a * b, prod3 = a * (FP107)b, prod4 = (FP107)a * (FP107)b;
      FP107 prod5 = FP107.Multiply(b, a), prod6 = (FP107)b * a, prod7 = b * (FP107)a, prod8 = (FP107)b * (FP107)a;
      Assert.AreEqual(expectedProduct, prod);
      Assert.AreEqual(prod, prod2);
      Assert.AreEqual(prod2, prod3);
      Assert.AreEqual(prod3, prod4);
      Assert.AreEqual(prod4, prod5);
      Assert.AreEqual(prod5, prod6);
      Assert.AreEqual(prod6, prod7);
      Assert.AreEqual(prod7, prod8);
    }

    static void TestRemainder(FP107 expectedRemainder, double a, double b)
    {
      FP107 rem, quot = FP107.DivRem(a, b, out rem), rem2 = (FP107)a % b, rem3 = a % (FP107)b, rem4 = (FP107)a % (FP107)b;
      Assert.AreEqual(expectedRemainder, rem);
      Assert.AreEqual(rem, rem2);
      Assert.AreEqual(rem2, rem3);
      Assert.AreEqual(rem3, rem4);
    }

    static void TestSubtract(FP107 expectedDifference, double a, double b)
    {
      FP107 diff = FP107.Subtract(a, b), diff2 = (FP107)a - b, diff3 = a - (FP107)b, diff4 = (FP107)a - (FP107)b;
      Assert.AreEqual(expectedDifference, diff);
      Assert.AreEqual(diff, diff2);
      Assert.AreEqual(diff2, diff3);
      Assert.AreEqual(diff3, diff4);
    }

    static void TestNumberFormat(string pattern, int patternNumber)
    {
      NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
      nfi.NumberNegativePattern = patternNumber;
      pattern = pattern.Replace("-", nfi.NegativeSign).Replace("n", "1");
      Assert.AreEqual(pattern, FP107.MinusOne.ToString("N", nfi));
      Assert.AreEqual(FP107.MinusOne, FP107.Parse(pattern, nfi));
    }

    static void TestPercentFormat(bool negative, string pattern, int patternNumber)
    {
      NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
      if(negative) nfi.PercentNegativePattern = patternNumber;
      else nfi.PercentPositivePattern = patternNumber;
      pattern = pattern.Replace("%", nfi.PercentSymbol).Replace("-", nfi.NegativeSign).Replace("n", "1");
      FP107 value = FP107.Parse(negative ? "-.01" : ".01", CultureInfo.InvariantCulture);
      Assert.AreEqual(pattern, value.ToString("P", nfi));
      Assert.AreEqual(value, FP107.Parse(pattern, nfi));
    }

    static void TestRawDecomposition(double value, bool expectedNegative, int expectedRawExponent, ulong expectedMantissa)
    {
      bool negative;
      int exponent;
      ulong mantissa;
      IEEE754.RawDecompose(value, out negative, out exponent, out mantissa);
      Assert.AreEqual(expectedNegative, negative);
      Assert.AreEqual(expectedRawExponent, exponent);
      Assert.AreEqual(expectedMantissa, mantissa);
      Assert.AreEqual(value, IEEE754.RawComposeDouble(negative, exponent, mantissa));
    }

    static void TestRawDecomposition(float value, bool expectedNegative, int expectedRawExponent, uint expectedMantissa)
    {
      bool negative;
      int exponent;
      uint mantissa;
      IEEE754.RawDecompose(value, out negative, out exponent, out mantissa);
      Assert.AreEqual(expectedNegative, negative);
      Assert.AreEqual(expectedRawExponent, exponent);
      Assert.AreEqual(expectedMantissa, mantissa);
      Assert.AreEqual(value, IEEE754.RawComposeSingle(negative, exponent, mantissa));
    }
  }
}
