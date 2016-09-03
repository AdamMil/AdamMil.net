using System;
using System.Globalization;
using System.IO;
using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Mathematics.Tests
{
  [TestFixture]
  public class Rationals
  {
    [Test]
    public void Comparisons()
    {
      #pragma warning disable 1718 // comparison to the same value

      // test flags
      Assert.IsTrue(Rational.One.IsPositive);
      Assert.IsFalse(Rational.One.IsNegative);
      Assert.IsFalse(Rational.One.IsZero);
      Assert.IsFalse(Rational.MinusOne.IsPositive);
      Assert.IsTrue(Rational.MinusOne.IsNegative);
      Assert.IsFalse(Rational.MinusOne.IsZero);
      Assert.IsFalse(Rational.Zero.IsPositive);
      Assert.IsFalse(Rational.Zero.IsNegative);
      Assert.IsTrue(Rational.Zero.IsZero);

      // test equality
      Assert.AreEqual(0, (double)Rational.Zero);
      Assert.AreEqual(1, (double)Rational.One);
      Assert.AreEqual(-1, (double)Rational.MinusOne);
      Assert.AreEqual(Rational.Zero, Rational.Zero);
      Assert.AreEqual(Rational.One, Rational.One);
      Assert.AreEqual(Rational.MinusOne, Rational.MinusOne);
      Assert.AreEqual((Rational)0, Rational.Zero);
      Assert.AreEqual((Rational)1, Rational.One);
      Assert.AreEqual((Rational)(-1), Rational.MinusOne);
      Assert.IsTrue(100 == new Rational(100));
      Assert.IsTrue(new Rational(100) == 100);
      Assert.IsTrue(int.MaxValue == new Rational(int.MaxValue));
      Assert.IsTrue(new Rational(int.MaxValue) == int.MaxValue);
      Assert.IsTrue(int.MinValue == new Rational(int.MinValue));
      Assert.IsTrue(new Rational(int.MinValue) == int.MinValue);
      Assert.IsTrue(new Integer(12308947234908) == new Rational(12308947234908));
      Assert.IsTrue(new Rational(12308947234908) == new Integer(12308947234908));
      Assert.IsTrue(new Rational(int.MaxValue) == new Rational(int.MaxValue));
      Assert.IsTrue(new Rational(int.MinValue) == new Rational(int.MinValue));
      Assert.IsTrue(Rational.One.Equals(Rational.One));
      Assert.IsTrue(Rational.MinusOne.Equals(-1));
      Assert.IsTrue(Rational.MinusOne.Equals(Integer.MinusOne));
      Assert.IsFalse(Rational.Zero == default(Rational));
      Assert.IsFalse(Rational.Zero.Equals(default(Rational)));

      // test inequality
      Assert.IsTrue(Rational.One != Rational.Zero);
      Assert.IsTrue(Rational.One != Rational.MinusOne);
      Assert.IsFalse(Rational.One != Rational.One);
      Assert.IsFalse(Rational.Zero != Rational.Zero);
      Assert.IsTrue(Rational.One != -1);
      Assert.IsTrue(-1 != Rational.One);
      Assert.IsTrue(0 != default(Rational));

      // test less than, greater than, etc.
      Assert.IsTrue(Rational.Zero < Rational.One);
      Assert.IsTrue(Rational.MinusOne < Rational.Zero);
      Assert.IsTrue(3 < new Rational(3.14));
      Assert.IsFalse(4 < new Rational(3.14));
      Assert.IsFalse(Rational.One < Rational.One);
      Assert.IsFalse(Rational.One < Rational.MinusOne);
      Assert.IsTrue(Rational.Zero < Integer.One);
      Assert.IsTrue(Rational.MinusOne < Integer.Zero);
      Assert.IsTrue(Rational.Zero < 23);
      Assert.IsFalse(Rational.Zero < -23);
      Assert.IsTrue(Rational.MinusOne < 0);

      Assert.IsTrue(Rational.Zero <= Rational.One);
      Assert.IsTrue(Rational.MinusOne <= Rational.Zero);
      Assert.IsTrue(3 <= new Rational(3.14));
      Assert.IsFalse(4 <= new Rational(3.14));
      Assert.IsTrue(Rational.One <= Rational.One);
      Assert.IsFalse(Rational.One <= Rational.MinusOne);
      Assert.IsTrue(Rational.Zero <= Integer.One);
      Assert.IsTrue(Rational.MinusOne <= Integer.Zero);
      Assert.IsTrue(Rational.Zero <= 23);
      Assert.IsFalse(Rational.Zero <= -23);
      Assert.IsTrue(Rational.MinusOne <= 0);

      Assert.IsFalse(Rational.Zero > Rational.One);
      Assert.IsFalse(Rational.MinusOne > Rational.Zero);
      Assert.IsFalse(3 > new Rational(3.14));
      Assert.IsTrue(4 > new Rational(3.14));
      Assert.IsFalse(Rational.One > Rational.One);
      Assert.IsTrue(Rational.One > Rational.MinusOne);
      Assert.IsFalse(Rational.Zero > Integer.One);
      Assert.IsFalse(Rational.MinusOne > Integer.Zero);
      Assert.IsFalse(Rational.Zero > 23);
      Assert.IsTrue(Rational.Zero > -23);
      Assert.IsFalse(Rational.MinusOne > 0);

      Assert.IsFalse(Rational.Zero >= Rational.One);
      Assert.IsFalse(Rational.MinusOne >= Rational.Zero);
      Assert.IsFalse(3 >= new Rational(3.14));
      Assert.IsTrue(4 >= new Rational(3.14));
      Assert.IsTrue(Rational.One >= Rational.One);
      Assert.IsTrue(Rational.One >= Rational.MinusOne);
      Assert.IsFalse(Rational.Zero >= Integer.One);
      Assert.IsFalse(Rational.MinusOne >= Integer.Zero);
      Assert.IsFalse(Rational.Zero >= 23);
      Assert.IsTrue(Rational.Zero >= -23);
      Assert.IsFalse(Rational.MinusOne >= 0);

      #pragma warning restore 1718
    }

    [Test]
    public void Conversions()
    {
      // test creation from numerator / denominator pairs
      Rational threeFourths = new Rational(3, 4);
      Assert.AreEqual((Integer)3, threeFourths.Numerator);
      Assert.AreEqual((Integer)4, threeFourths.Denominator);
      Assert.AreEqual(threeFourths, new Rational(6, 8));
      Assert.AreEqual(threeFourths, new Rational(-21, -28));
      Assert.AreEqual(-threeFourths, new Rational(-132, 176));
      Assert.AreEqual(-threeFourths, new Rational(297, -396));
      Assert.AreEqual(threeFourths, new Rational(6397036269, 8529381692));
      Assert.AreEqual(threeFourths, new Rational(-70481482776, -93975310368));
      Assert.AreEqual(-threeFourths, new Rational(-71677154217, 95569538956));
      Assert.AreEqual(-threeFourths, new Rational(10468612329, -13958149772));
      Assert.AreEqual(threeFourths, new Rational(Integer.Parse("55340232221128654848"), Integer.Parse("73786976294838206464")));
      Assert.AreEqual(threeFourths, new Rational(Integer.Parse("-3320413933267719290880"), Integer.Parse("-4427218577690292387840")));
      Assert.AreEqual(-threeFourths, new Rational(Integer.Parse("-15511210043330985984000000"), Integer.Parse("20681613391107981312000000")));
      Assert.AreEqual(-threeFourths, new Rational(Integer.Parse("15511210043330985984"), Integer.Parse("-20681613391107981312")));

      // test conversions to/from integer, double, and float
      Assert.AreEqual(int.MaxValue, (int)new Rational(int.MaxValue));
      Assert.AreEqual(uint.MaxValue, (uint)new Rational(uint.MaxValue));
      Assert.AreEqual(long.MaxValue, (long)new Rational(long.MaxValue));
      Assert.AreEqual(int.MinValue, (int)new Rational(int.MinValue));
      Assert.AreEqual(uint.MinValue, (uint)new Rational(uint.MinValue));
      Assert.AreEqual(long.MinValue, (long)new Rational(long.MinValue));
      Assert.AreEqual(ulong.MaxValue, (ulong)new Rational(ulong.MaxValue));
      Assert.AreEqual(uint.MaxValue, (uint)new Rational(ulong.MaxValue));
      Assert.AreEqual(-1L, (long)new Rational(ulong.MaxValue));
      Assert.AreEqual(-1, (int)new Rational(ulong.MaxValue));
      Assert.AreEqual(23890723480, (long)new Rational(23890723480));
      Assert.AreEqual(Math.PI, (double)new Rational(Math.PI));
      Assert.AreEqual((float)Math.PI, (float)new Rational(Math.PI));
      Assert.AreEqual(FP107.Pi, (FP107)new Rational(FP107.Pi));

      // test conversions to/from decimal
      Assert.AreEqual(-3.141592653589793238462643383m, (decimal)-new Rational(FP107.Pi));
      Assert.AreEqual(Rational.FromDecimalApproximation(12345.6789), (Rational)12345.6789m);
      Assert.AreEqual(new Rational(-123456789, 10000), new Rational(-12345.6789m));

      // test miscellaneous conversions
      Assert.AreEqual('A', ((IConvertible)(Rational)65.6).ToChar(null));
      Assert.AreEqual((Integer)65, ((IConvertible)(Rational)65.6).ToType(typeof(Integer), null));
      Assert.AreEqual((FP107)65.25, ((IConvertible)(Rational)65.25).ToType(typeof(FP107), null));

      // test basic conversions to/from string
      CultureInfo inv = CultureInfo.InvariantCulture;
      int[] dens = new int[] { 2, 3, 5, 7, 16000, 11, -23, -15, 17 };
      string[] strs = new string[]
      {
        "0.5", "0.33333333333333333333", "0.2", "0.14285714285714285714", "0.0000625",
        "0.09090909090909090909", "-0.04347826086956521739", "-0.06666666666666666667", "0.05882352941176470588"
      };
      for(int i=0; i<dens.Length; i++)
      {
        Rational exact = new Rational(1, dens[i]);
        Assert.AreEqual(strs[i], exact.ToString("F20", inv));
        if(strs[i].Length < 10) Assert.AreEqual(exact, Rational.Parse(strs[i], inv));
        else Assert.Less(Rational.Abs(exact - Rational.Parse(strs[i], inv)), new Rational(1, Integer.Pow(10, 19)), strs[i]);
      }

      Assert.AreEqual("6.25E-5", new Rational(1, 16000).ToString(inv));
      Assert.AreEqual("1E+49", Rational.Pow(10, 49).ToString("E0", inv));
      Assert.AreEqual("2.2E-16", new Rational(FP107.FromComponents(2.2204460492503131E-16, -3.2000000000000004E-48)).ToString("E1", inv));

      // test string formatting
      Rational npi = -(Rational)FP107.Pi;
      // first test default formattings in the invariant culture
      Assert.AreEqual("-3.14159265358979323846", npi.ToString(inv));
      Assert.AreEqual("(¤3.14)", npi.ToString("C", inv));
      Assert.AreEqual("-3.14159265358979323846E+0", npi.ToString("E", inv));
      Assert.AreEqual("-3.14", npi.ToString("F", inv));
      Assert.AreEqual("-3.14159265358979323846", npi.ToString("G", inv));
      Assert.AreEqual("-3.14", npi.ToString("N", inv));
      Assert.AreEqual("-314.16 %", npi.ToString("P", inv));
      TestFormatRoundTrip(npi, "-127438138015862315638027235646471/40564819207303340847894502572032", "R", inv);
      TestFormatRoundTrip(npi, "-0x6487ED5110B4611A62633145C07/0x200000000000000000000000000", "X", inv);
      TestFormatRoundTrip(-npi, "0x6487ed5110b4611a62633145c07/0x200000000000000000000000000", "x", inv);
      TestFormatRoundTrip(new Rational(1e5), "1E+5", null, inv);
      TestFormatRoundTrip(new Rational(1e5), "1E+5", "G", inv);
      TestFormatRoundTrip(new Rational(1e5), "100000", "F", inv);
      TestFormatRoundTrip(new Rational(1e6), "1,000,000", "N", inv);
      TestFormatRoundTrip(new Rational(1e6), "¤1,000,000", "C", inv);
      Assert.AreEqual("-3.14159E+2", (npi*100).ToString("E5", inv));
      Assert.AreEqual("0", new Rational(-0d).ToString(inv));
      Assert.AreEqual("0/1", new Rational(-0d).ToString("R", inv));
      Assert.AreEqual("NaN", new Rational().ToString(inv));
      Assert.AreEqual(new Rational(), Rational.Parse("NaN", inv));

      // test rounding
      Assert.AreEqual("1", new Rational(1.5 - IEEE754.DoublePrecision).ToString("F0", inv));
      Assert.AreEqual("2", new Rational(1.5).ToString("F0", inv));
      Assert.AreEqual("2", new Rational(1.5 + IEEE754.DoublePrecision).ToString("F0", inv));
      Assert.AreEqual("-2", new Rational(-1.5 - IEEE754.DoublePrecision).ToString("F0", inv));
      Assert.AreEqual("-2", new Rational(-1.5).ToString("F0", inv));
      Assert.AreEqual("-1", new Rational(-1.5 + IEEE754.DoublePrecision).ToString("F0", inv));
      Assert.AreEqual("2", new Rational(2.5).ToString("F0", inv));
      Assert.AreEqual("3", new Rational(2.5 + IEEE754.DoublePrecision*2).ToString("F0", inv));
      Assert.AreEqual("4", new Rational(3.5).ToString("F0", inv));
      Assert.AreEqual("-3", npi.ToString("F0", inv));
      Assert.AreEqual("-3.1", npi.ToString("F1", inv));
      Assert.AreEqual("-3.142", npi.ToString("F3", inv));
      Assert.AreEqual("0.001", new Rational(0.0012356).ToString("F3", inv));
      Assert.AreEqual("54321.001", new Rational(54321.0012356).ToString("F3", inv));
      Assert.AreEqual("1", new Rational(0.99999).ToString("F4", inv));
      Assert.AreEqual("0.99999", new Rational(0.99999).ToString("F5", inv));
      Assert.AreEqual("0.99999", new Rational(0.99999).ToString("F9", inv));
      Assert.AreEqual("0.1", new Rational(0.099999).ToString("F5", inv));
      Assert.AreEqual("10", new Rational(9.99999).ToString("F4", inv));

      // test parsing weird strings
      Assert.AreEqual(new Rational(123.45m), Rational.Parse("  % 0, 1 ,234 .50E+1+  ", inv)); // percent
      Assert.AreEqual(new Rational(-123.45m), Rational.Parse(" ( 0 0, 1 ,234500 E-1‰- ) ", inv)); // negative, permille
      Assert.AreEqual(new Rational(-123.45m), Rational.Parse("¤123.45-", inv)); // negative, currency

      // test long symbols
      NumberFormatInfo nfi = (NumberFormatInfo)inv.NumberFormat.Clone();
      nfi.CurrencySymbol = "$_=";
      nfi.PercentSymbol  = "pct";
      nfi.PerMilleSymbol = "pml";
      nfi.NegativeSign   = "minus";
      nfi.PositiveSign   = "plus";
      nfi.NaNSymbol      = "nope";
      TestFormatRoundTrip(5, "$_=5", "C", nfi);
      TestFormatRoundTrip(Rational.FromDecimalApproximation(-0.05), "minus5 pct", "P", nfi);
      TestFormatRoundTrip(new Rational(), "nope", null, nfi);
      Assert.AreEqual(new Rational(5), Rational.Parse("5 plus", nfi));
      Assert.AreEqual(new Rational(-5), Rational.Parse("5 minus", nfi));
      Assert.AreEqual(Rational.FromDecimalApproximation(-.005), Rational.Parse("5 pmlminus", nfi));

      // test weird grouping
      nfi.NumberGroupSizes = new int[] { 2, 3, 4 };
      Assert.AreEqual("1,2345,678,90", new Rational(1234567890).ToString("N", nfi));
      Assert.AreEqual("1,2345,6789,000,00", new Rational(12345678900000).ToString("N", nfi));
      nfi.NumberGroupSizes = new int[] { 2, 3, 0 };
      Assert.AreEqual("12345,678,90", new Rational(1234567890).ToString("N", nfi));

      // test currency formats
      TestCurrencyFormat(false, "$n", 0);
      TestCurrencyFormat(false, "n$", 1);
      TestCurrencyFormat(false, "$ n", 2);
      TestCurrencyFormat(false, "n $", 3);
      TestCurrencyFormat(true, "($n)", 0);
      TestCurrencyFormat(true, "-$n", 1);
      TestCurrencyFormat(true, "$-n", 2);
      TestCurrencyFormat(true, "$n-", 3);
      TestCurrencyFormat(true, "(n$)", 4);
      TestCurrencyFormat(true, "-n$", 5);
      TestCurrencyFormat(true, "n-$", 6);
      TestCurrencyFormat(true, "n$-", 7);
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
      TestPercentFormat(false, "n%", 1);
      TestPercentFormat(false, "%n", 2);
      TestPercentFormat(false, "% n", 3);
      TestPercentFormat(true, "-n %", 0);
      TestPercentFormat(true, "-n%", 1);
      TestPercentFormat(true, "-%n", 2);
      TestPercentFormat(true, "%-n", 3);
      TestPercentFormat(true, "%n-", 4);
      TestPercentFormat(true, "n-%", 5);
      TestPercentFormat(true, "n%-", 6);
      TestPercentFormat(true, "-% n", 7);
      TestPercentFormat(true, "n %-", 8);
      TestPercentFormat(true, "% n-", 9);
      TestPercentFormat(true, "% -n", 10);
      TestPercentFormat(true, "n- %", 11);

      // test number formats
      TestNumberFormat("(n)", 0);
      TestNumberFormat("-n", 1);
      TestNumberFormat("- n", 2);
      TestNumberFormat("n-", 3);
      TestNumberFormat("n -", 4);

      // test saving and loading in various forms
      npi = -(Rational)Math.PI;
      Assert.AreEqual(-(Integer)884279719003555, npi.Numerator);
      Assert.AreEqual((Integer)281474976710656, npi.Denominator);
      Assert.AreEqual(npi, Rational.FromComponents(-884279719003555, 281474976710656));

      MemoryStream ms = new MemoryStream();
      using(var writer = new AdamMil.IO.BinaryWriter(ms, false)) ((Rational)Math.PI).Save(writer);
      ms.Position = 0;
      using(var reader = new AdamMil.IO.BinaryReader(ms)) Assert.AreEqual((Rational)Math.PI, new Rational(reader));
    }

    [Test]
    public void Arithmetic()
    {
      CultureInfo inv = CultureInfo.InvariantCulture;

      // test addition
      Rational maxPlusEpsilon = Rational.Parse("36385714125121573300846800698456749842842774431060269030973563199251835202763131874220510446199752578146168959525535975504123660741259730559491535919078220069839241298744801305292878640835527930863994674357611588999020693594474762898847930291552594690170203187215045688094955660773922576137969830342611860225019935582199601121469249223149872466121371567155862303084330314602566069416432551333006194774477514260351201969859368060220131234488198148976536169638305696900504838830719760875514246216508976803882728582677352177004129288847854463084006372981390756344519549931097743963603971632334891836831978686870043355177324550146752513/202402253307310618352495346718917307049556649764142118356901358027430339567995346891960383701437124495187077864316811911389808737385793476867013399940738509921517424276566361364466907742093216341239767678472745068562007483424692698618103355649159556340810056512358769552333414615230502532186327508646006263307707741093494784");
      TestAdd(40, 2); // add normal numbers
      TestAdd(-40, 2);
      TestAdd(-2, 1);
      TestAdd(1.1m, 0.9m);
      TestAdd(1.1m, -0.9m);
      TestAdd(-1.1m, -0.9m);
      TestAdd(123.456m, 234.876m);
      TestAdd(Rational.Zero, double.MinValue, double.MaxValue); // test cancelation of big numbers
      TestAdd(maxPlusEpsilon, double.MaxValue, double.Epsilon); // add numbers of hugely differing magnitudes
      TestAdd(-maxPlusEpsilon, double.MinValue, -double.Epsilon);
      TestAdd(Rational.Parse("88365048971274477961791626215475154398189511610745/15079682891156167474997086031827199058210108594814", inv),
              Rational.Parse("26151465932107044561886949/8324270144388272579650158"), Rational.Parse("44318294029212074848496194/16303789241138206983236097"));

      // test subtraction
      TestSubtract(42, 40); // subtract normal numbers
      TestSubtract(-40, 2);
      TestSubtract(-40, -2);
      TestSubtract(1.1m, 0.9m);
      TestSubtract(1.1m, -0.9m);
      TestSubtract(-1.1m, -0.9m);
      TestSubtract(123.456m, 234.876m);
      TestSubtract(0, double.MinValue); // subtract big from small
      TestSubtract(0, double.MaxValue);
      TestSubtract((Rational)double.MaxValue, maxPlusEpsilon, (Rational)double.Epsilon); // widely differing magnitudes
      TestSubtract((Rational)double.MinValue, -maxPlusEpsilon, -(Rational)double.Epsilon);
      TestSubtract(Rational.Parse("19150179022096022649343839939330095410959925166467/45239048673468502424991258095481597174630325784442", inv),
                   Rational.Parse("26151465932107044561886949/8324270144388272579650158"), Rational.Parse("44318294029212074848496194/16303789241138206983236097"));

      // test multiplication
      TestMultiply(42, 40); // multiply normal numbers
      TestMultiply(-40, 2);
      TestMultiply(-40, -2);
      TestMultiply(100, 100*Math.Pow(2, 20), 1/Math.Pow(2, 20));
      TestMultiply(1, 64, 1d/64);
      TestSubtract(123.456m, 234.876m);
      TestMultiply((Rational)double.MaxValue, 1, double.MaxValue);
      TestMultiply((Rational)0, double.MinValue, 0);
      TestMultiply(Rational.Parse("579494178237021310285708136560817422585849842386053/67858573010202753637486887143222395761945488676663", inv),
                   Rational.Parse("26151465932107044561886949/8324270144388272579650158"), Rational.Parse("44318294029212074848496194/16303789241138206983236097"));

      // test division
      TestDivide(42, 40);
      TestDivide(-40, 2);
      TestDivide(-40, -2);
      TestDivide(5m, 4m);
      TestDivide(12345m, 100m);
      TestDivide((Rational)(1.0/(1<<20)), 1.0, 1<<20);
      TestDivide(4096, 64, 1d/64);
      TestDivide(-1, double.MaxValue, double.MinValue);
      TestDivide(0, 0, double.MaxValue);
      TestDivide((Rational)Math.Pow(2, 1000), Math.Pow(2, 500), Math.Pow(2, -500));
      TestDivide(Rational.Pow(2, 1000), Rational.Pow(2, 500), Rational.Pow(2, -500));
      TestDivide((Rational)double.MinValue, double.MaxValue, -1);
      TestDivide((Rational)double.MaxValue, double.MaxValue/2, 0.5);
      TestDivide(Rational.Parse("142122662967959728267359359292877779302764229999351/122972483945863705618015519353547683891804304832884", inv),
                 Rational.Parse("26151465932107044561886949/8324270144388272579650158"), Rational.Parse("44318294029212074848496194/16303789241138206983236097"));

      // test remainder
      TestRemainder(42, 40);
      TestRemainder(-9, 8);
      TestRemainder(9, -8);
      TestRemainder(-9, -8);
      TestRemainder(5, 6);
      TestRemainder(0.125m, 1.125m, 0.5m);
      TestRemainder(-40, 2);
      TestRemainder(-40, -2);
      TestRemainder(1, 1d/(1<<20));
      TestRemainder((Rational)1.0715086061883472E+301, double.MaxValue/2, Math.Pow(2, 1000));
      TestRemainder(Rational.Parse("19150179022096022649343839939330095410959925166467/45239048673468502424991258095481597174630325784442", inv),
                    Rational.Parse("26151465932107044561886949/8324270144388272579650158"), Rational.Parse("44318294029212074848496194/16303789241138206983236097"));

      // test increment and decrement
      TestIncDec(42, 40);
      TestIncDec(42, 1);
      TestIncDec(1, 42);
      TestIncDec(-42, 40);
      TestIncDec(-42, 1);
      TestIncDec(-1, 42);
    }

    [Test]
    public void SimpleFunctions()
    {
      // test the Abs function
      Rational pi = (Rational)Math.PI, npi = -pi;
      Assert.AreEqual(pi, pi.Abs());
      Assert.AreEqual(pi, (-pi).Abs());
      Assert.AreEqual(Rational.One, Rational.Abs(Rational.MinusOne));
      Assert.AreEqual(Rational.Zero, Rational.Zero.Abs());

      // test the Ceiling function
      Assert.AreEqual((Integer)1, new Rational(1).Ceiling());
      Assert.AreEqual((Integer)2, new Rational(1.25).Ceiling());
      Assert.AreEqual((Integer)2, new Rational(1.75).Ceiling());
      Assert.AreEqual((Integer)2, new Rational(2).Ceiling());
      Assert.AreEqual((Integer)0, Rational.Zero.Ceiling());
      Assert.AreEqual((Integer)(-1), new Rational(-1).Ceiling());
      Assert.AreEqual((Integer)(-1), new Rational(-1.25).Ceiling());
      Assert.AreEqual((Integer)(-1), new Rational(-1.75).Ceiling());
      Assert.AreEqual((Integer)(-2), new Rational(-2).Ceiling());

      // test the Floor function
      Assert.AreEqual((Integer)1, new Rational(1).Floor());
      Assert.AreEqual((Integer)1, new Rational(1.25).Floor());
      Assert.AreEqual((Integer)1, new Rational(1.75).Floor());
      Assert.AreEqual((Integer)2, new Rational(2).Floor());
      Assert.AreEqual((Integer)0, Rational.Zero.Floor());
      Assert.AreEqual((Integer)(-1), new Rational(-1).Floor());
      Assert.AreEqual((Integer)(-2), new Rational(-1.25).Floor());
      Assert.AreEqual((Integer)(-2), new Rational(-1.75).Floor());
      Assert.AreEqual((Integer)(-2), new Rational(-2).Floor());

      // test Min and Max
      Assert.AreEqual(Rational.One, Rational.Min(pi, Rational.One));
      Assert.AreEqual(pi, Rational.Max(pi, Rational.One));
      Assert.AreEqual(-pi, Rational.Min(-pi, Rational.One));
      Assert.AreEqual(Rational.One, Rational.Max(-pi, Rational.One));

      // test the Round function
      Assert.AreEqual((Integer)1, new Rational(1).Round());
      Assert.AreEqual((Integer)1, new Rational(1.25).Round());
      Assert.AreEqual((Integer)2, new Rational(1.5).Round());
      Assert.AreEqual((Integer)2, new Rational(1.75).Round());
      Assert.AreEqual((Integer)2, new Rational(2).Round());
      Assert.AreEqual((Integer)2, new Rational(2.5).Round());
      Assert.AreEqual((Integer)0, new Rational(0).Round());
      Assert.AreEqual((Integer)(-1), new Rational(-1).Round());
      Assert.AreEqual((Integer)(-1), new Rational(-1.25).Round());
      Assert.AreEqual((Integer)(-2), new Rational(-1.5).Round());
      Assert.AreEqual((Integer)(-2), new Rational(-1.75).Round());
      Assert.AreEqual((Integer)(-2), new Rational(-2).Round());
      Assert.AreEqual((Integer)(-2), new Rational(-2.5).Round());
      Assert.AreEqual(new Rational(12, 10), new Rational(1.25).Round(1));
      Assert.AreEqual(new Rational(18, 10), new Rational(1.75).Round(1));
      Assert.AreEqual(new Rational(-12, 10), new Rational(-1.25).Round(1));
      Assert.AreEqual(new Rational(-18, 10), new Rational(-1.75).Round(1));
      Assert.AreEqual(new Rational(1.25), new Rational(1.25).Round(2));
      Assert.AreEqual(new Rational(1.75), new Rational(1.75).Round(2));
      Assert.AreEqual(new Rational(-1.25), new Rational(-1.25).Round(2));
      Assert.AreEqual(new Rational(-1.75), new Rational(-1.75).Round(2));
      Assert.AreEqual(new Rational(1.25), new Rational(1.25).Round(3));
      Assert.AreEqual(new Rational(1.75), new Rational(1.75).Round(3));
      Assert.AreEqual(new Rational(-1.25), new Rational(-1.25).Round(3));
      Assert.AreEqual(new Rational(-1.75), new Rational(-1.75).Round(3));
      Assert.AreEqual(new Rational(120), new Rational(123).Round(-1));
      Assert.AreEqual(new Rational(100), new Rational(123).Round(-2));
      Assert.AreEqual(new Rational(0), new Rational(123).Round(-3));
      Assert.AreEqual(new Rational(0), new Rational(123).Round(-4));

      // test the Sign function
      Assert.AreEqual(1, new Rational(1).Sign);
      Assert.AreEqual(0, Rational.Zero.Sign);
      Assert.AreEqual(-1, new Rational(-1).Sign);
      Assert.AreEqual(1, new Rational(10, 3).Sign);
      Assert.AreEqual(-1, new Rational(-10, 3).Sign);

      // test the Random function. it's hard to test a random function, but we can at least make sure it isn't obviously broken
      Random.RandomNumberGenerator rng = Random.RandomNumberGenerator.CreateFastest();
      Rational sum = 0, min = 1, max = 0;
      for(int i=0; i<100; i++)
      {
        Rational value = Rational.Random(rng, 32);
        sum += value;
        min = Rational.Min(min, value);
        max = Rational.Max(max, value);
        Assert.IsTrue(value >= 0 && value < 1);
      }
      Assert.IsTrue(sum >= 41 && sum <= 59); // there's about a 99.13% chance that the average will lie between 0.41 and 0.59
      Assert.IsTrue(min <= (Rational)0.055 && max >= (Rational)(1-0.055)); // there's about a 99.3% chance that both min and max will lie within 0.055 of the edge

      // test the Square function
      Assert.AreEqual(npi*npi, npi.Square());
      Assert.AreEqual(npi*npi, (-npi).Square());

      // test the Truncate function
      Assert.AreEqual((Integer)1, new Rational(1).Truncate());
      Assert.AreEqual((Integer)1, new Rational(1.25).Truncate());
      Assert.AreEqual((Integer)1, new Rational(1.75).Truncate());
      Assert.AreEqual((Integer)2, new Rational(2).Truncate());
      Assert.AreEqual((Integer)0, Rational.Zero.Truncate());
      Assert.AreEqual((Integer)(-1), new Rational(-1).Truncate());
      Assert.AreEqual((Integer)(-1), new Rational(-1.25).Truncate());
      Assert.AreEqual((Integer)(-1), new Rational(-1.75).Truncate());
      Assert.AreEqual((Integer)(-2), new Rational(-2).Truncate());

      // test the Inverse function
      Assert.AreEqual(Rational.One, Rational.One.Inverse());
      Assert.AreEqual(Rational.MinusOne, Rational.MinusOne.Inverse());
      TestHelpers.TestException<DivideByZeroException>(() => Rational.Zero.Inverse());
      TestHelpers.TestException<DivideByZeroException>(() => Rational.Inverse(Rational.Zero));
      Assert.AreEqual(new Rational(7, 22), new Rational(22, 7).Inverse());
      Assert.AreEqual(new Rational(-7, 22), new Rational(-22, 7).Inverse());

      // test the Simplify function
      Assert.AreEqual((Integer)130, Rational.FromComponents(130, 30).Numerator);
      Assert.AreEqual((Integer)30, Rational.FromComponents(130, 30).Denominator);
      Assert.AreEqual((Integer)13, Rational.FromComponents(130, 30).Simplify().Numerator);
      Assert.AreEqual((Integer)3, Rational.FromComponents(-130, 30).Simplify().Denominator);
    }

    [Test]
    public void Approximate()
    {
      Rational pi = Rational.Parse("3.1415926535897932385");
      TestApproximate(pi, 3, new Rational(16, 5), new Rational(22, 7), new Rational(201, 64), new Rational(333, 106),
                      new Rational(355, 113), new Rational(355, 113), new Rational(75948, 24175),
                      new Rational(100798, 32085), new Rational(103993, 33102), new Rational(312689, 99532));
      TestApproximate(123, 122, 123, 123, 123, 123);
      Assert.AreEqual(new Rational(11345), Rational.Approximate(12345d, -3));
    }

    [Test]
    public void ContinuedFractions()
    {
      TestContinuedFraction(new Rational(3), 3);
      TestContinuedFraction(new Rational(31, 10), 3, 10);
      TestContinuedFraction(new Rational(1234, 100000), 0, 81, 26, 1, 4, 1, 3);
      TestContinuedFraction(Rational.Parse("3.1415926535897932385"), 3, 7, 15, 1, 292, 1, 1, 1, 2, 1, 3, 1, 14, 2, 1, 1,
                            2, 2, 2, 1, 1, 6, 1, 2, 5, 5, 24, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 2, 83);
      Assert.AreEqual(new Rational(103993, 33102), Rational.FromContinuedFraction(new int[] { 3, 7, 15, 1, 292, 1, 1, 1, 2 }, 5));
    }

    [Test]
    public void PowerFunctions()
    {
      CultureInfo inv = CultureInfo.InvariantCulture;

      // test the Pow function
      Assert.AreEqual(Rational.One, Rational.Pow(1, 0));
      Assert.AreEqual(Rational.Zero, Rational.Pow(0, 1));
      Assert.AreEqual(Rational.One, Rational.Pow(1, -1));
      Assert.AreEqual(Rational.Zero, Rational.Pow(Rational.Zero, 1));
      Assert.AreEqual(Rational.One, Rational.Pow(Rational.One, 0));
      Assert.AreEqual((Rational)4052555153018976267UL, Rational.Pow(3, 39));
      Assert.AreEqual(Rational.Parse("227373675443232059478759765625/4398046511104", inv), Rational.Pow((Rational)2.5, 42));
      Assert.AreEqual(Rational.Parse("4398046511104/227373675443232059478759765625", inv), Rational.Pow((Rational)2.5, -42));
      Assert.AreEqual(new Rational(9, 4), Rational.Pow(new Rational(-3, 2), 2));
      Assert.AreEqual(new Rational(-27, 8), Rational.Pow(new Rational(-3, 2), 3));
      Assert.AreEqual(new Rational(-8, 27), Rational.Pow(new Rational(-3, 2), -3));

      // test the Sqrt function
      Assert.AreEqual(Rational.Zero, Rational.Sqrt(Integer.Zero, 10));
      Assert.AreEqual(Rational.One, Rational.Sqrt(Integer.One, 10));
      Assert.AreEqual((Rational)8, Rational.Sqrt(64, 10));
      Assert.AreEqual(Rational.Zero, Rational.Sqrt(Rational.Zero, 10));
      Assert.AreEqual(Rational.One, Rational.Sqrt(Rational.One, 10));
      Assert.AreEqual((Rational)8, Rational.Sqrt((Rational)64, 10));
      Assert.Less(AbsoluteError(Rational.Parse("1.4142135623730950488016887242096981", inv), Rational.Sqrt(2, 34)), Rational.Pow(10, -34));
      Assert.Less(AbsoluteError(Rational.Parse("1.5811388300841896659994467722163593", inv), Rational.Sqrt(new Rational(5, 2), 34)), Rational.Pow(10, -34));
      Assert.Less(AbsoluteError(Rational.Parse("3703.7036868518518135138887144511564153", inv), Rational.Sqrt(13717421, 34)), Rational.Pow(10, -34));
      Assert.Less(AbsoluteError(Rational.Parse("1.53154263391720344439620071574455365417883258688457507530557627e28", inv),
                                Rational.Sqrt(Rational.Parse("703686851851813513888714451156415292803286711450357152857/3", inv), 34)), Rational.Pow(10, -34));
      Assert.Less(AbsoluteError(Rational.Parse("1.53154263391720344439620071574455365417883258688457507530557627e28", inv),
                                Rational.Sqrt(Rational.Parse("703686851851813513888714451156415292803286711450357152857/3", inv), -10)), Rational.Pow(10, 10));
      TestHelpers.TestException<ArgumentOutOfRangeException>(() => Rational.Sqrt(Integer.MinusOne, 1));
      TestHelpers.TestException<ArgumentOutOfRangeException>(() => Rational.Sqrt(new Rational(-2, 3), 1));

      // test the Root function
      Assert.AreEqual(Rational.Zero, Rational.Root(Integer.Zero, 3, 10));
      Assert.AreEqual(Rational.One, Rational.Root(Integer.One, 3, 10));
      Assert.AreEqual((Rational)8, Rational.Root(4096, 4, 10));
      Assert.AreEqual(Rational.Zero, Rational.Root(Rational.Zero, 3, 10));
      Assert.AreEqual(Rational.One, Rational.Root(Rational.One, 3, 10));
      Assert.AreEqual((Rational)8, Rational.Root((Rational)4096, 4, 10));
      Assert.Less(AbsoluteError(Rational.Parse("1.2599210498948731647672106072782284", inv), Rational.Root(2, 3, 34)), Rational.Pow(10, -34));
      Assert.Less(AbsoluteError(Rational.Parse("1.3572088082974532857590447348397446", inv), Rational.Root(new Rational(5, 2), 3, 34)), Rational.Pow(10, -34));
      Assert.Less(AbsoluteError(Rational.Parse("239.3816314996405713525244310018501695", inv), Rational.Root(13717421, 3, 34)), Rational.Pow(10, -34));
      Assert.Less(AbsoluteError(Rational.Parse("-6.1671719954912444438138829568552367811894356144692179e18", inv),
                                Rational.Root(Rational.Parse("-703686851851813513888714451156415292803286711450357152857/3", inv), 3, 34)), Rational.Pow(10, -34));
      Assert.Less(AbsoluteError(Rational.Parse("6.1671719954912444438138829568552367811894356144692179e18", inv),
                                Rational.Root(Rational.Parse("703686851851813513888714451156415292803286711450357152857/3", inv), 3, -10)), Rational.Pow(10, 10));
      Assert.Less(AbsoluteError(Rational.Parse("433536.6507434652029774633706120043322824", inv),
                                Rational.Root(Rational.Parse("703686851851813513888714451156415292803286711450357152857/3", inv), 10, 34)), Rational.Pow(10, -34));
      TestHelpers.TestException<ArgumentOutOfRangeException>(() => Rational.Root(Integer.MinusOne, 2, 1));
      TestHelpers.TestException<ArgumentOutOfRangeException>(() => Rational.Root(new Rational(-2, 3), 2, 1));
      Rational.Root(Integer.MinusOne, 3, 1);
      Rational.Root(new Rational(-2, 3), 3, 1);
    }

    static Rational AbsoluteError(Rational a, Rational b)
    {
      return Rational.Abs(a - b);
    }

    static void TestAdd(int a, int b)
    {
      Rational sum = (Rational)a + b, sum2 = a + (Rational)b, sum3 = (Rational)a + (Rational)b;
      Rational sum4 = (Rational)b + a, sum5 = b + (Rational)a, sum6 = (Rational)b + (Rational)a;
      Rational sum7 = (Rational)a + (Integer)b, sum8 = (Integer)a + (Rational)b, sum9 = (Rational)b + (Integer)a, sum10 = (Integer)b + (Rational)a;
      Rational sum11 = Rational.UnsimplifiedAdd(a, b).Simplify(), sum12 = Rational.UnsimplifiedAdd(b, a).Simplify();
      Rational sum13 = Rational.UnsimplifiedAdd(a, (Rational)b).Simplify(), sum14 = Rational.UnsimplifiedAdd(b, (Rational)a).Simplify();
      Assert.AreEqual((Rational)(a+b), sum);
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
      Assert.AreEqual(sum11, sum12);
      Assert.AreEqual(sum12, sum13);
      Assert.AreEqual(sum13, sum14);
      TestAdd((double)a, (double)b);
      TestAdd((decimal)a, (decimal)b);
    }

    static void TestAdd(decimal a, decimal b)
    {
      TestAdd((Rational)(a+b), (Rational)a, (Rational)b);
      TestAdd((double)a, (double)b);
    }

    static void TestAdd(double a, double b)
    {
      TestAdd(Rational.FromDecimalApproximation(a+b), Rational.FromDecimalApproximation(a), Rational.FromDecimalApproximation(b));
    }

    static void TestAdd(Rational expectedSum, double a, double b)
    {
      TestAdd(expectedSum, (Rational)a, (Rational)b);
    }

    static void TestAdd(Rational expectedSum, Rational a, Rational b)
    {
      Rational sum = a + b, sum2 = b + a, sum3 = a.UnsimplifiedAdd(b).Simplify(), sum4 = b.UnsimplifiedAdd(a).Simplify();
      Assert.AreEqual(expectedSum, sum);
      Assert.AreEqual(sum, sum2);
      Assert.AreEqual(sum2, sum3);
      Assert.AreEqual(sum3, sum4);
    }

    static void TestApproximate(Rational value, params Rational[] approx)
    {
      for (int i = 0; i < approx.Length; i++)
      {
        Assert.AreEqual(approx[i], value.Approximate(i));
        Assert.AreEqual(-approx[i], (-value).Approximate(i));
        Assert.AreEqual(approx[i], Rational.Approximate((double)value, i));
      }
    }

    static void TestContinuedFraction(Rational r, params int[] cf)
    {
      Integer[] bicf = new Integer[cf.Length], nbicf = new Integer[cf.Length];
      for (int i=0; i<bicf.Length; i++) bicf[i] = cf[i];
      for (int i=0; i<nbicf.Length; i++) nbicf[i] = -cf[i];

      CollectionAssert.AreEqual(bicf, r.ToContinuedFraction());
      CollectionAssert.AreEqual(nbicf, (-r).ToContinuedFraction());
      CollectionAssert.AreEqual(bicf, Rational.ToContinuedFraction(r));
      Assert.AreEqual(r, Rational.FromContinuedFraction(bicf));
      Assert.AreEqual(-r, Rational.FromContinuedFraction(nbicf));
      Assert.AreEqual(r, Rational.FromContinuedFraction(cf));
    }

    static void TestCurrencyFormat(bool negative, string pattern, int patternNumber)
    {
      NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
      if(negative) nfi.CurrencyNegativePattern = patternNumber;
      else nfi.CurrencyPositivePattern = patternNumber;
      pattern = pattern.Replace("$", nfi.CurrencySymbol).Replace("-", nfi.NegativeSign).Replace("n", "1");
      Assert.AreEqual(pattern, new Rational(negative ? -1 : 1).ToString("C", nfi));
      Assert.AreEqual(new Rational(negative ? -1 : 1), Rational.Parse(pattern, nfi));
    }

    static void TestDivide(int a, int b)
    {
      Rational expected = new Rational(a, b), quot = (Rational)a / b, quot2 = a / (Rational)b;
      Rational quot3 = Rational.UnsimplifiedDivide(a, (Rational)b).Simplify(), quot4 = Rational.UnsimplifiedDivide((Rational)a, b).Simplify();
      Rational rem, quot5 = Rational.DivRem((Rational)a, b, out rem), quot6 = Rational.DivRem(a, (Rational)b, out rem);
      Rational quot7 = Rational.UnsimplifiedDivRem((Rational)a, b, out rem).Simplify(), quot8 = Rational.UnsimplifiedDivRem(a, (Rational)b, out rem).Simplify();
      Rational quot9 = (Rational)(-a) / (-b), quot10 = (-a) / (Rational)(-b), quot11 = Rational.UnsimplifiedDivide((Rational)(-a), -b).Simplify();
      Rational quot12 = Rational.UnsimplifiedDivide(-a, (Rational)(-b)).Simplify(), quot13 = Rational.UnsimplifiedDivRem(-a, -b, out rem).Simplify();
      Assert.AreEqual(expected, quot);
      Assert.AreEqual(quot, quot2);
      Assert.AreEqual(quot2, quot3);
      Assert.AreEqual(quot3, quot4);
      Assert.AreEqual(quot4, quot5);
      Assert.AreEqual(quot5, quot6);
      Assert.AreEqual(quot6, quot7);
      Assert.AreEqual(quot7, quot8);
      Assert.AreEqual(quot8, quot9);
      Assert.AreEqual(quot9, quot10);
      Assert.AreEqual(quot10, quot11);
      Assert.AreEqual(quot11, quot12);
      Assert.AreEqual(quot12, quot13);
      TestDivide((decimal)a, (decimal)b);
    }

    static void TestDivide(decimal a, decimal b)
    {
      TestDivide((Rational)(a/b), (Rational)a, (Rational)b);
      TestDivide((double)a, (double)b);
    }

    static void TestDivide(Rational expected, double a, double b)
    {
      TestDivide(expected, (Rational)a, (Rational)b);
    }

    static void TestDivide(double a, double b)
    {
      TestDivide(Rational.FromDecimalApproximation(a/b), Rational.FromDecimalApproximation(a), Rational.FromDecimalApproximation(b));
    }

    static void TestDivide(Rational expectedQuotient, Rational a, Rational b)
    {
      Rational quot = a / b, quot2 = (Rational)a * b.Inverse(), quot3 = a.UnsimplifiedDivide(b).Simplify();
      Rational rem, quot4 = Rational.DivRem(a, b, out rem), quot5 = a.UnsimplifiedDivRem(b, out rem).Simplify();
      Rational quot6 = -a / -b, quot7 = (-a).UnsimplifiedDivide(-b).Simplify(), quot8 = Rational.DivRem(-a, -b, out rem);
      Rational quot9 = (-a).UnsimplifiedDivRem(-b, out rem).Simplify();
      Assert.AreEqual(expectedQuotient, quot);
      Assert.AreEqual(quot, quot2);
      Assert.AreEqual(quot2, quot3);
      Assert.AreEqual(quot3, quot4);
      Assert.AreEqual(quot4, quot5);
      Assert.AreEqual(quot5, quot6);
      Assert.AreEqual(quot6, quot7);
      Assert.AreEqual(quot7, quot8);
      Assert.AreEqual(quot8, quot9);
    }

    static void TestFormatRoundTrip(Rational value, string expectedString, string format, IFormatProvider provider)
    {
      Assert.AreEqual(expectedString, value.ToString(format, provider));
      Assert.AreEqual(value, Rational.Parse(expectedString, provider));
    }

    static void TestIncDec(int n, int d)
    {
      Rational inc = new Rational(n+d, d), dec = new Rational(n-d, d), i1 = new Rational(n, d), i2 = i1.UnsimplifiedIncrement().Simplify();
      Rational i3 = -i1, i4 = -i3.UnsimplifiedDecrement().Simplify();
      Rational d1 = i1, d2 = d1.UnsimplifiedDecrement().Simplify(), d3 = -d1, d4 = -d3.UnsimplifiedIncrement().Simplify();
      Assert.AreEqual(inc, ++i1);
      Assert.AreEqual(inc, i2);
      Assert.AreEqual(inc, - --i3);
      Assert.AreEqual(inc, i4);
      Assert.AreEqual(dec, --d1);
      Assert.AreEqual(dec, d2);
      Assert.AreEqual(dec, - ++d3);
      Assert.AreEqual(dec, d4);
      i2--; i2--;
      d2++; d2++;
      Assert.AreEqual(inc, d2);
      Assert.AreEqual(dec, i2);
    }

    static void TestMultiply(int a, int b)
    {
      Rational expected = a*b, prod = (Rational)a * b, prod2 = a * (Rational)b, prod3 = (Rational)b * a, prod4 = b * (Rational)a;
      Rational prod5 = (Rational)(-a) * -b, prod6 = -a * (Rational)(-b), prod7 = (Rational)(-b) * -a, prod8 = -b * (Rational)(-a);
      Rational prod9 = Rational.UnsimplifiedMultiply((Rational)a, b).Simplify(), prod10 = Rational.UnsimplifiedMultiply(a, (Rational)b).Simplify();
      Rational prod11 = Rational.UnsimplifiedMultiply((Rational)(-b), -a).Simplify(), prod12 = Rational.UnsimplifiedMultiply(-b, (Rational)(-a)).Simplify();
      Assert.AreEqual(expected, prod);
      Assert.AreEqual(prod, prod2);
      Assert.AreEqual(prod2, prod3);
      Assert.AreEqual(prod3, prod4);
      Assert.AreEqual(prod4, prod5);
      Assert.AreEqual(prod5, prod6);
      Assert.AreEqual(prod6, prod7);
      Assert.AreEqual(prod7, prod8);
      Assert.AreEqual(prod8, prod9);
      Assert.AreEqual(prod9, prod10);
      Assert.AreEqual(prod10, prod11);
      Assert.AreEqual(prod11, prod12);
      TestMultiply((decimal)a, (decimal)b);
    }

    static void TestMultiply(decimal a, decimal b)
    {
      TestMultiply((Rational)(a*b), (Rational)a, (Rational)b);
      TestMultiply((double)a, (double)b);
    }

    static void TestMultiply(double a, double b)
    {
      TestMultiply(Rational.FromDecimalApproximation(a*b), Rational.FromDecimalApproximation(a), Rational.FromDecimalApproximation(b));
    }

    static void TestMultiply(Rational expectedProduct, double a, double b)
    {
      TestMultiply(expectedProduct, (Rational)a, (Rational)b);
    }

    static void TestMultiply(Rational expectedProduct, Rational a, Rational b)
    {
      Rational prod = a * b, prod2 = b * a, prod3 = a.UnsimplifiedMultiply(b).Simplify(), prod4 = b.UnsimplifiedMultiply(a).Simplify();
      Rational prod5 = -a * -b, prod6 = -b * -a, prod7 = (-a).UnsimplifiedMultiply(-b).Simplify(), prod8 = (-b).UnsimplifiedMultiply(-a).Simplify();
      Assert.AreEqual(expectedProduct, prod);
      Assert.AreEqual(prod, prod2);
      Assert.AreEqual(prod2, prod3);
      Assert.AreEqual(prod3, prod4);
      Assert.AreEqual(prod4, prod5);
      Assert.AreEqual(prod5, prod6);
      Assert.AreEqual(prod6, prod7);
      Assert.AreEqual(prod7, prod8);
    }

    static void TestRemainder(int a, int b)
    {
      Rational expected = a % b;
      Rational rem = a % b, rem2 = -(-a % b), rem3 = a % -b, rem4 = -(-a % -b), rem5 = Rational.UnsimplifiedRemainder(a, b).Simplify();
      Rational rem6 = -Rational.UnsimplifiedRemainder(-a, b).Simplify(), rem7 = Rational.UnsimplifiedRemainder(a, -b).Simplify();
      Rational rem8 = -Rational.UnsimplifiedRemainder(-a, -b).Simplify(), rem9, rem10, rem11, rem12, rem13, rem14, rem15, rem16;
      Rational.DivRem(a, b, out rem9);
      Rational.DivRem(-a, b, out rem10); rem10 = -rem10;
      Rational.DivRem(a, -b, out rem11);
      Rational.DivRem(-a, -b, out rem12); rem12 = -rem12;
      Rational.UnsimplifiedDivRem(a, b, out rem13); rem13 = rem13.Simplify();
      Rational.UnsimplifiedDivRem(-a, b, out rem14); rem14 = -rem14.Simplify();
      Rational.UnsimplifiedDivRem(a, -b, out rem15); rem15 = rem15.Simplify();
      Rational.UnsimplifiedDivRem(-a, -b, out rem16); rem16 = -rem16.Simplify();
      Assert.AreEqual(expected, rem);
      Assert.AreEqual(rem, rem2);
      Assert.AreEqual(rem2, rem3);
      Assert.AreEqual(rem3, rem4);
      Assert.AreEqual(rem4, rem5);
      Assert.AreEqual(rem5, rem6);
      Assert.AreEqual(rem6, rem7);
      Assert.AreEqual(rem7, rem8);
      Assert.AreEqual(rem8, rem9);
      Assert.AreEqual(rem9, rem10);
      Assert.AreEqual(rem10, rem11);
      Assert.AreEqual(rem11, rem12);
      Assert.AreEqual(rem12, rem13);
      Assert.AreEqual(rem13, rem14);
      Assert.AreEqual(rem14, rem15);
      Assert.AreEqual(rem15, rem16);
      TestRemainder((decimal)a, (decimal)b);
    }

    static void TestRemainder(decimal a, decimal b)
    {
      TestRemainder(a % b, a, b);
      TestRemainder((double)a, (double)b);
    }

    static void TestRemainder(double a, double b)
    {
      TestRemainder(Rational.FromDecimalApproximation(a % b), Rational.FromDecimalApproximation(a), Rational.FromDecimalApproximation(b));
    }

    static void TestRemainder(Rational expected, double a, double b)
    {
      TestRemainder(expected, (Rational)a, (Rational)b);
    }

    static void TestRemainder(Rational expectedRemainder, Rational a, Rational b)
    {
      Rational rem = a % b, rem2 = -(-a % b), rem3 = a % -b, rem4 = -(-a % -b), rem5 = a.UnsimplifiedRemainder(b).Simplify();
      Rational rem6 = -Rational.UnsimplifiedRemainder(-a, b).Simplify(), rem7 = Rational.UnsimplifiedRemainder(a, -b).Simplify();
      Rational rem8 = -Rational.UnsimplifiedRemainder(-a, -b).Simplify(), rem9, rem10, rem11, rem12, rem13, rem14, rem15, rem16;
      Rational.DivRem(a, b, out rem9);
      Rational.DivRem(-a, b, out rem10); rem10 = -rem10;
      Rational.DivRem(a, -b, out rem11);
      Rational.DivRem(-a, -b, out rem12); rem12 = -rem12;
      Rational.UnsimplifiedDivRem(a, b, out rem13); rem13 = rem13.Simplify();
      Rational.UnsimplifiedDivRem(-a, b, out rem14); rem14 = -rem14.Simplify();
      Rational.UnsimplifiedDivRem(a, -b, out rem15); rem15 = rem15.Simplify();
      Rational.UnsimplifiedDivRem(-a, -b, out rem16); rem16 = -rem16.Simplify();
      Assert.AreEqual(expectedRemainder, rem);
      Assert.AreEqual(rem, rem2);
      Assert.AreEqual(rem2, rem3);
      Assert.AreEqual(rem3, rem4);
      Assert.AreEqual(rem4, rem5);
      Assert.AreEqual(rem5, rem6);
      Assert.AreEqual(rem6, rem7);
      Assert.AreEqual(rem7, rem8);
      Assert.AreEqual(rem8, rem9);
      Assert.AreEqual(rem9, rem10);
      Assert.AreEqual(rem10, rem11);
      Assert.AreEqual(rem11, rem12);
      Assert.AreEqual(rem12, rem13);
      Assert.AreEqual(rem13, rem14);
      Assert.AreEqual(rem14, rem15);
      Assert.AreEqual(rem15, rem16);
    }

    static void TestSubtract(int a, int b)
    {
      Rational expected = a - b, diff = (Rational)a - b, diff2 = a - (Rational)b;
      Rational diff3 = (Rational)a - (Integer)b, diff4 = (Integer)a - (Rational)b;
      Rational diff5 = (Rational)a - (Rational)b, diff6 = (Rational)(-b) - (-a), diff7 = -b - (Rational)(-a);
      Rational diff8 = (Rational)(-b) - (Integer)(-a), diff9 = (Integer)(-b) - (Rational)(-a), diff10 = (Rational)(-b) - (Rational)(-a);
      Rational diff11 = Rational.UnsimplifiedSubtract(a, (Rational)b).Simplify(), diff12 = Rational.UnsimplifiedSubtract((Rational)a, b).Simplify();
      Rational diff13 = Rational.UnsimplifiedSubtract((Rational)a, (Rational)b).Simplify();
      Rational diff14 = Rational.UnsimplifiedSubtract(-b, (Rational)(-a)).Simplify(), diff15 = Rational.UnsimplifiedSubtract((Rational)(-b), -a).Simplify();
      Rational diff16 = Rational.UnsimplifiedSubtract((Rational)(-b), (Rational)(-a)).Simplify();
      Assert.AreEqual(expected, diff);
      Assert.AreEqual(diff, diff2);
      Assert.AreEqual(diff2, diff3);
      Assert.AreEqual(diff3, diff4);
      Assert.AreEqual(diff4, diff5);
      Assert.AreEqual(diff5, diff6);
      Assert.AreEqual(diff6, diff7);
      Assert.AreEqual(diff7, diff8);
      Assert.AreEqual(diff8, diff9);
      Assert.AreEqual(diff9, diff10);
      Assert.AreEqual(diff10, diff11);
      Assert.AreEqual(diff11, diff12);
      Assert.AreEqual(diff12, diff13);
      Assert.AreEqual(diff13, diff14);
      Assert.AreEqual(diff14, diff15);
      Assert.AreEqual(diff15, diff16);
      TestSubtract((decimal)a, (decimal)b);
    }

    static void TestSubtract(decimal a, decimal b)
    {
      TestSubtract((Rational)(a-b), (Rational)a, (Rational)b);
      TestSubtract((double)a, (double)b);
    }

    static void TestSubtract(double a, double b)
    {
      TestSubtract(Rational.FromDecimalApproximation(a - b), Rational.FromDecimalApproximation(a), Rational.FromDecimalApproximation(b));
    }

    static void TestSubtract(Rational expected, double a, double b)
    {
      TestSubtract(expected, (Rational)a, (Rational)b);
    }

    static void TestSubtract(Rational expected, Rational a, Rational b)
    {
      Rational diff = a - b, diff2 = -b - (-a);
      Rational diff3 = Rational.UnsimplifiedSubtract(a, b).Simplify();
      Rational diff4 = Rational.UnsimplifiedSubtract(-b, -a).Simplify();
      Assert.AreEqual(expected, diff);
      Assert.AreEqual(diff, diff2);
      Assert.AreEqual(diff2, diff3);
      Assert.AreEqual(diff3, diff4);
    }

    static void TestNumberFormat(string pattern, int patternNumber)
    {
      NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
      nfi.NumberNegativePattern = patternNumber;
      pattern = pattern.Replace("-", nfi.NegativeSign).Replace("n", "1");
      Assert.AreEqual(pattern, Rational.MinusOne.ToString("N", nfi));
      Assert.AreEqual(Rational.MinusOne, Rational.Parse(pattern, nfi));
    }

    static void TestPercentFormat(bool negative, string pattern, int patternNumber)
    {
      NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
      if(negative) nfi.PercentNegativePattern = patternNumber;
      else nfi.PercentPositivePattern = patternNumber;
      pattern = pattern.Replace("%", nfi.PercentSymbol).Replace("-", nfi.NegativeSign).Replace("n", "1");
      Rational value = Rational.Parse(negative ? "-.01" : ".01", CultureInfo.InvariantCulture);
      Assert.AreEqual(pattern, value.ToString("P", nfi));
      Assert.AreEqual(value, Rational.Parse(pattern, nfi));
    }
  }
}
