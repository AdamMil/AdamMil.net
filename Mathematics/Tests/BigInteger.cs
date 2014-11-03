using System;
using System.Globalization;
using System.IO;
using AdamMil.Tests;
using AdamMil.Utilities;
using NUnit.Framework;
using BinaryReader = AdamMil.IO.BinaryReader;
using BinaryWriter = AdamMil.IO.BinaryWriter;

// TODO: check bit length everywhere

namespace AdamMil.Mathematics.Tests
{
  [TestFixture]
  public class BigInteger
  {
    [Test]
    public void T01_Comparisons()
    {
      #pragma warning disable 1718 // comparison to the same value

      // test flags
      Integer med = 1000000, nmed = -med, big = 1000000000000, nbig = -big;
      Assert.AreEqual(0, Integer.Zero.BitLength);
      Assert.AreEqual(1, Integer.One.BitLength);
      Assert.AreEqual(1, Integer.MinusOne.BitLength);
      Assert.AreEqual(20, med.BitLength);
      Assert.AreEqual(20, nmed.BitLength);
      Assert.AreEqual(40, big.BitLength);
      Assert.AreEqual(40, nbig.BitLength);
      Assert.IsTrue(Integer.Zero.IsZero);
      Assert.IsFalse(Integer.One.IsZero);
      Assert.IsFalse(Integer.MinusOne.IsZero);
      Assert.IsFalse(big.IsZero);
      Assert.IsFalse(nbig.IsZero);
      Assert.IsFalse(Integer.Zero.IsPositive);
      Assert.IsTrue(Integer.One.IsPositive);
      Assert.IsFalse(Integer.MinusOne.IsPositive);
      Assert.IsTrue(big.IsPositive);
      Assert.IsFalse(nbig.IsPositive);
      Assert.IsFalse(Integer.Zero.IsNegative);
      Assert.IsFalse(Integer.One.IsNegative);
      Assert.IsTrue(Integer.MinusOne.IsNegative);
      Assert.IsFalse(big.IsNegative);
      Assert.IsTrue(nbig.IsNegative);
      Assert.IsTrue(Integer.Zero.IsEven);
      Assert.IsFalse(Integer.One.IsEven);
      Assert.IsFalse(Integer.MinusOne.IsEven);
      Assert.IsTrue(big.IsEven);
      Assert.IsTrue(nbig.IsEven);
      Assert.AreEqual(0, Integer.Zero.Sign);
      Assert.AreEqual(1, Integer.One.Sign);
      Assert.AreEqual(-1, Integer.MinusOne.Sign);
      Assert.AreEqual(1, big.Sign);
      Assert.AreEqual(-1, nbig.Sign);

      // test equality
      Assert.IsTrue(Integer.One == 1);
      Assert.IsTrue(Integer.MinusOne == -1);
      Assert.IsTrue(Integer.Zero == 0);
      Assert.IsTrue(med == 1000000);
      Assert.IsTrue(med == 1000000u);
      Assert.IsTrue(med == 1000000L);
      Assert.IsTrue(med == 1000000uL);
      Assert.IsTrue(nmed == -1000000);
      Assert.IsFalse(nmed == 1000000u);
      Assert.IsTrue(nmed == -1000000L);
      Assert.IsFalse(nmed == 1000000uL);
      Assert.IsTrue(big == 1000000000000);
      Assert.IsTrue(big == 1000000000000u);
      Assert.IsTrue(big == 1000000000000L);
      Assert.IsTrue(big == 1000000000000uL);
      Assert.IsTrue(nbig == -1000000000000);
      Assert.IsFalse(nbig == 1000000000000u);
      Assert.IsTrue(nbig == -1000000000000L);
      Assert.IsFalse(nbig == 1000000000000uL);
      Assert.IsTrue(1000000 == med);
      Assert.IsTrue(1000000u == med);
      Assert.IsTrue(1000000L == med);
      Assert.IsTrue(1000000uL == med);
      Assert.IsTrue(-1000000 == nmed);
      Assert.IsFalse(1000000u == nmed);
      Assert.IsTrue(-1000000L == nmed);
      Assert.IsFalse(1000000uL == nmed);
      Assert.IsTrue(1000000000000 == big);
      Assert.IsTrue(1000000000000u == big);
      Assert.IsTrue(1000000000000L == big);
      Assert.IsTrue(1000000000000uL == big);
      Assert.IsTrue(-1000000000000 == nbig);
      Assert.IsFalse(1000000000000u == nbig);
      Assert.IsTrue(-1000000000000L == nbig);
      Assert.IsFalse(1000000000000uL == nbig);
      Assert.IsTrue(med == med);
      Assert.IsTrue(nmed == nmed);
      Assert.IsFalse(med == nmed);
      Assert.IsTrue(big == big);
      Assert.IsTrue(nbig == nbig);
      Assert.IsFalse(big == nbig);
      Assert.IsFalse(med == big);
      Assert.IsFalse(nbig == med);
      Assert.AreEqual(med, med);
      Assert.AreEqual(nmed, nmed);
      Assert.AreEqual(big, big);
      Assert.AreEqual(nbig, nbig);

      // test inequality
      Assert.IsFalse(Integer.One != 1);
      Assert.IsFalse(Integer.MinusOne != -1);
      Assert.IsFalse(Integer.Zero != 0);
      Assert.IsFalse(med != 1000000);
      Assert.IsFalse(med != 1000000u);
      Assert.IsFalse(med != 1000000L);
      Assert.IsFalse(med != 1000000uL);
      Assert.IsFalse(nmed != -1000000);
      Assert.IsTrue(nmed != 1000000u);
      Assert.IsFalse(nmed != -1000000L);
      Assert.IsTrue(nmed != 1000000uL);
      Assert.IsFalse(big != 1000000000000);
      Assert.IsFalse(big != 1000000000000u);
      Assert.IsFalse(big != 1000000000000L);
      Assert.IsFalse(big != 1000000000000uL);
      Assert.IsFalse(nbig != -1000000000000);
      Assert.IsTrue(nbig != 1000000000000u);
      Assert.IsFalse(nbig != -1000000000000L);
      Assert.IsTrue(nbig != 1000000000000uL);
      Assert.IsFalse(1000000 != med);
      Assert.IsFalse(1000000u != med);
      Assert.IsFalse(1000000L != med);
      Assert.IsFalse(1000000uL != med);
      Assert.IsFalse(-1000000 != nmed);
      Assert.IsTrue(1000000u != nmed);
      Assert.IsFalse(-1000000L != nmed);
      Assert.IsTrue(1000000uL != nmed);
      Assert.IsFalse(1000000000000 != big);
      Assert.IsFalse(1000000000000u != big);
      Assert.IsFalse(1000000000000L != big);
      Assert.IsFalse(1000000000000uL != big);
      Assert.IsFalse(-1000000000000 != nbig);
      Assert.IsTrue(1000000000000u != nbig);
      Assert.IsFalse(-1000000000000L != nbig);
      Assert.IsTrue(1000000000000uL != nbig);
      Assert.IsFalse(med != med);
      Assert.IsFalse(nmed != nmed);
      Assert.IsTrue(med != nmed);
      Assert.IsFalse(big != big);
      Assert.IsFalse(nbig != nbig);
      Assert.IsTrue(big != nbig);
      Assert.IsTrue(med != big);
      Assert.IsTrue(nbig != med);
      Assert.AreNotEqual(med, nmed);
      Assert.AreNotEqual(big, nbig);
      Assert.AreNotEqual(med, big);
      Assert.AreNotEqual(nbig, med);

      // test less than, greater than, etc.
      Assert.IsFalse(Integer.One < 1);
      Assert.IsTrue(Integer.One < 2);
      Assert.IsTrue(Integer.Zero < 1);
      Assert.IsTrue(Integer.MinusOne < 0);
      Assert.IsFalse(Integer.MinusOne < -1);
      Assert.IsFalse(big < big);
      Assert.IsTrue(nmed < med);
      Assert.IsFalse(med < nmed);
      Assert.IsTrue(med < big);
      Assert.IsTrue(nmed < big);
      Assert.IsFalse(med < nbig);
      Assert.IsTrue(nbig < med);
      Assert.IsFalse(med < 100);
      Assert.IsFalse(med < 1000000);
      Assert.IsFalse(med < 1000000u);
      Assert.IsFalse(med < 1000000L);
      Assert.IsFalse(med < 1000000uL);
      Assert.IsTrue(med < 1000001);
      Assert.IsTrue(nmed < 0);
      Assert.IsFalse(nmed < -1000000);
      Assert.IsFalse(nmed < -1000001);
      Assert.IsTrue(nmed < -999999);
      Assert.IsFalse(big < 100);
      Assert.IsFalse(big < 1000000000000);
      Assert.IsFalse(big < 1000000000000u);
      Assert.IsFalse(big < 1000000000000L);
      Assert.IsFalse(big < 1000000000000uL);
      Assert.IsTrue(big < 1000000000001);
      Assert.IsTrue(nbig < 0);
      Assert.IsFalse(nbig < -1000000000000);
      Assert.IsFalse(nbig < -1000000000001);
      Assert.IsTrue(nbig < -999999999999);
      Assert.IsTrue(100 < med);
      Assert.IsFalse(1000000 < med);
      Assert.IsFalse(1000000u < med);
      Assert.IsFalse(1000000L < med);
      Assert.IsFalse(1000000uL < med);
      Assert.IsFalse(1000001 < med);
      Assert.IsFalse(0 < nmed);
      Assert.IsFalse(-1000000 < nmed);
      Assert.IsTrue(-1000001 < nmed);
      Assert.IsFalse(-999999 < nmed);
      Assert.IsTrue(100 < big);
      Assert.IsFalse(1000000000000 < big);
      Assert.IsFalse(1000000000000u < big);
      Assert.IsFalse(1000000000000L < big);
      Assert.IsFalse(1000000000000uL < big);
      Assert.IsFalse(1000000000001 < big);
      Assert.IsFalse(0 < nbig);
      Assert.IsFalse(-1000000000000 < nbig);
      Assert.IsTrue(-1000000000001 < nbig);
      Assert.IsFalse(-999999999999 < nbig);

      Assert.IsTrue(Integer.One <= 1);
      Assert.IsTrue(Integer.One <= 2);
      Assert.IsTrue(Integer.Zero <= 1);
      Assert.IsTrue(Integer.MinusOne <= 0);
      Assert.IsTrue(Integer.MinusOne <= -1);
      Assert.IsTrue(big <= big);
      Assert.IsTrue(nmed <= med);
      Assert.IsFalse(med <= nmed);
      Assert.IsTrue(med <= big);
      Assert.IsTrue(nmed <= big);
      Assert.IsFalse(med <= nbig);
      Assert.IsTrue(nbig <= med);
      Assert.IsFalse(med <= 100);
      Assert.IsTrue(med <= 1000000);
      Assert.IsTrue(med <= 1000000u);
      Assert.IsTrue(med <= 1000000L);
      Assert.IsTrue(med <= 1000000uL);
      Assert.IsTrue(med <= 1000001);
      Assert.IsTrue(nmed <= 0);
      Assert.IsTrue(nmed <= -1000000);
      Assert.IsFalse(nmed <= -1000001);
      Assert.IsTrue(nmed <= -999999);
      Assert.IsFalse(big <= 100);
      Assert.IsTrue(big <= 1000000000000);
      Assert.IsTrue(big <= 1000000000000u);
      Assert.IsTrue(big <= 1000000000000L);
      Assert.IsTrue(big <= 1000000000000uL);
      Assert.IsTrue(big <= 1000000000001);
      Assert.IsTrue(nbig <= 0);
      Assert.IsTrue(nbig <= -1000000000000);
      Assert.IsFalse(nbig <= -1000000000001);
      Assert.IsTrue(nbig <= -999999999999);
      Assert.IsTrue(100 <= med);
      Assert.IsTrue(1000000 <= med);
      Assert.IsTrue(1000000u <= med);
      Assert.IsTrue(1000000L <= med);
      Assert.IsTrue(1000000uL <= med);
      Assert.IsFalse(1000001 <= med);
      Assert.IsFalse(0 <= nmed);
      Assert.IsTrue(-1000000 <= nmed);
      Assert.IsTrue(-1000001 <= nmed);
      Assert.IsFalse(-999999 <= nmed);
      Assert.IsTrue(100 <= big);
      Assert.IsTrue(1000000000000 <= big);
      Assert.IsTrue(1000000000000u <= big);
      Assert.IsTrue(1000000000000L <= big);
      Assert.IsTrue(1000000000000uL <= big);
      Assert.IsFalse(1000000000001 <= big);
      Assert.IsFalse(0 <= nbig);
      Assert.IsTrue(-1000000000000 <= nbig);
      Assert.IsTrue(-1000000000001 <= nbig);
      Assert.IsFalse(-999999999999 <= nbig);

      Assert.IsFalse(Integer.One > 1);
      Assert.IsFalse(Integer.One > 2);
      Assert.IsFalse(Integer.Zero > 1);
      Assert.IsFalse(Integer.MinusOne > 0);
      Assert.IsFalse(Integer.MinusOne > -1);
      Assert.IsFalse(big > big);
      Assert.IsFalse(nmed > med);
      Assert.IsTrue(med > nmed);
      Assert.IsFalse(med > big);
      Assert.IsFalse(nmed > big);
      Assert.IsTrue(med > nbig);
      Assert.IsFalse(nbig > med);
      Assert.IsTrue(med > 100);
      Assert.IsFalse(med > 1000000);
      Assert.IsFalse(med > 1000000u);
      Assert.IsFalse(med > 1000000L);
      Assert.IsFalse(med > 1000000uL);
      Assert.IsFalse(med > 1000001);
      Assert.IsFalse(nmed > 0);
      Assert.IsFalse(nmed > -1000000);
      Assert.IsTrue(nmed > -1000001);
      Assert.IsFalse(nmed > -999999);
      Assert.IsTrue(big > 100);
      Assert.IsFalse(big > 1000000000000);
      Assert.IsFalse(big > 1000000000000u);
      Assert.IsFalse(big > 1000000000000L);
      Assert.IsFalse(big > 1000000000000uL);
      Assert.IsFalse(big > 1000000000001);
      Assert.IsFalse(nbig > 0);
      Assert.IsFalse(nbig > -1000000000000);
      Assert.IsTrue(nbig > -1000000000001);
      Assert.IsFalse(nbig > -999999999999);
      Assert.IsFalse(100 > med);
      Assert.IsFalse(1000000 > med);
      Assert.IsFalse(1000000u > med);
      Assert.IsFalse(1000000L > med);
      Assert.IsFalse(1000000uL > med);
      Assert.IsTrue(1000001 > med);
      Assert.IsTrue(0 > nmed);
      Assert.IsFalse(-1000000 > nmed);
      Assert.IsFalse(-1000001 > nmed);
      Assert.IsTrue(-999999 > nmed);
      Assert.IsFalse(100 > big);
      Assert.IsFalse(1000000000000 > big);
      Assert.IsFalse(1000000000000u > big);
      Assert.IsFalse(1000000000000L > big);
      Assert.IsFalse(1000000000000uL > big);
      Assert.IsTrue(1000000000001 > big);
      Assert.IsTrue(0 > nbig);
      Assert.IsFalse(-1000000000000 > nbig);
      Assert.IsFalse(-1000000000001 > nbig);
      Assert.IsTrue(-999999999999 > nbig);

      Assert.IsTrue(Integer.One >= 1);
      Assert.IsFalse(Integer.One >= 2);
      Assert.IsFalse(Integer.Zero >= 1);
      Assert.IsFalse(Integer.MinusOne >= 0);
      Assert.IsTrue(Integer.MinusOne >= -1);
      Assert.IsTrue(big >= big);
      Assert.IsFalse(nmed >= med);
      Assert.IsTrue(med >= nmed);
      Assert.IsFalse(med >= big);
      Assert.IsFalse(nmed >= big);
      Assert.IsTrue(med >= nbig);
      Assert.IsFalse(nbig >= med);
      Assert.IsTrue(med >= 100);
      Assert.IsTrue(med >= 1000000);
      Assert.IsTrue(med >= 1000000u);
      Assert.IsTrue(med >= 1000000L);
      Assert.IsTrue(med >= 1000000uL);
      Assert.IsFalse(med >= 1000001);
      Assert.IsFalse(nmed >= 0);
      Assert.IsTrue(nmed >= -1000000);
      Assert.IsTrue(nmed >= -1000001);
      Assert.IsFalse(nmed >= -999999);
      Assert.IsTrue(big >= 100);
      Assert.IsTrue(big >= 1000000000000);
      Assert.IsTrue(big >= 1000000000000u);
      Assert.IsTrue(big >= 1000000000000L);
      Assert.IsTrue(big >= 1000000000000uL);
      Assert.IsFalse(big >= 1000000000001);
      Assert.IsFalse(nbig >= 0);
      Assert.IsTrue(nbig >= -1000000000000);
      Assert.IsTrue(nbig >= -1000000000001);
      Assert.IsFalse(nbig >= -999999999999);
      Assert.IsFalse(100 >= med);
      Assert.IsTrue(1000000 >= med);
      Assert.IsTrue(1000000u >= med);
      Assert.IsTrue(1000000L >= med);
      Assert.IsTrue(1000000uL >= med);
      Assert.IsTrue(1000001 >= med);
      Assert.IsTrue(0 >= nmed);
      Assert.IsTrue(-1000000 >= nmed);
      Assert.IsFalse(-1000001 >= nmed);
      Assert.IsTrue(-999999 >= nmed);
      Assert.IsFalse(100 >= big);
      Assert.IsTrue(1000000000000 >= big);
      Assert.IsTrue(1000000000000u >= big);
      Assert.IsTrue(1000000000000L >= big);
      Assert.IsTrue(1000000000000uL >= big);
      Assert.IsTrue(1000000000001 >= big);
      Assert.IsTrue(0 >= nbig);
      Assert.IsTrue(-1000000000000 >= nbig);
      Assert.IsFalse(-1000000000001 >= nbig);
      Assert.IsTrue(-999999999999 >= nbig);

      #pragma warning restore 1718
    }

    [Test]
    public void T02_Conversions()
    {
      // test conversions and constructors with int, uint, long, and ulong
      Assert.AreEqual(int.MaxValue, (int)new Integer(int.MaxValue));
      Assert.AreEqual(int.MaxValue, new Integer(int.MaxValue).ToInt32());
      Assert.AreEqual(long.MaxValue, (long)new Integer(long.MaxValue));
      Assert.AreEqual(long.MaxValue, new Integer(long.MaxValue).ToInt64());
      Assert.AreEqual(uint.MaxValue, (uint)new Integer(uint.MaxValue));
      Assert.AreEqual(uint.MaxValue, new Integer(uint.MaxValue).ToUInt32());
      Assert.AreEqual(ulong.MaxValue, (ulong)new Integer(ulong.MaxValue));
      Assert.AreEqual(ulong.MaxValue, new Integer(ulong.MaxValue).ToUInt64());

      Assert.AreEqual(int.MinValue, (int)new Integer(int.MinValue));
      Assert.AreEqual(int.MinValue, new Integer(int.MinValue).ToInt32());
      Assert.AreEqual(long.MinValue, (long)new Integer(long.MinValue));
      Assert.AreEqual(long.MinValue, new Integer(long.MinValue).ToInt64());
      Assert.AreEqual(uint.MinValue, (uint)new Integer(uint.MinValue));
      Assert.AreEqual(uint.MinValue, new Integer(uint.MinValue).ToUInt32());
      Assert.AreEqual(ulong.MinValue, (ulong)new Integer(ulong.MinValue));
      Assert.AreEqual(ulong.MinValue, new Integer(ulong.MinValue).ToUInt64());

      Assert.AreEqual(uint.MaxValue, (uint)new Integer((long)uint.MaxValue));
      Assert.AreEqual(uint.MaxValue, new Integer((long)uint.MaxValue).ToUInt32());
      Assert.AreEqual(uint.MaxValue, (uint)new Integer((ulong)uint.MaxValue));
      Assert.AreEqual(uint.MaxValue, new Integer((ulong)uint.MaxValue).ToUInt32());

      Assert.AreEqual(1000000, (int)new Integer(1000000));
      Assert.AreEqual(1000000, new Integer(1000000).ToInt32());
      Assert.AreEqual(1000000, (int)new Integer(1000000u));
      Assert.AreEqual(1000000, new Integer(1000000u).ToInt32());
      Assert.AreEqual(1000000, (int)new Integer(1000000L));
      Assert.AreEqual(1000000, new Integer(1000000L).ToInt32());
      Assert.AreEqual(1000000, (int)new Integer(1000000uL));
      Assert.AreEqual(1000000, new Integer(1000000uL).ToInt32());
      Assert.AreEqual(-1000000, (int)new Integer(-1000000));
      Assert.AreEqual(-1000000, new Integer(-1000000).ToInt32());
      Assert.AreEqual(-1000000, (int)new Integer(-1000000L));
      Assert.AreEqual(-1000000, new Integer(-1000000L).ToInt32());

      Assert.AreEqual(1000000000000, (long)new Integer(1000000000000L));
      Assert.AreEqual(1000000000000, new Integer(1000000000000L).ToInt64());
      Assert.AreEqual(1000000000000, (long)new Integer(1000000000000uL));
      Assert.AreEqual(1000000000000, new Integer(1000000000000uL).ToInt64());
      Assert.AreEqual(-1000000000000, (long)new Integer(-1000000000000L));
      Assert.AreEqual(-1000000000000, new Integer(-1000000000000L).ToInt64());

      Assert.AreEqual(new Integer(-1000000000000), new Integer(new Integer(-1000000000000)));
      Assert.AreEqual(new Integer(1000000000000), new Integer(new Integer(1000000000000)));

      // test conversions to/from floating point and decimal
      AssertEqual(3, new Integer(Math.PI));
      AssertEqual(-2, new Integer(-FP107.E));
      AssertEqual(new Integer(float.MinValue), Integer.Parse("-340282346638528859811704183484516925440"));
      AssertEqual(new Integer(double.MaxValue), Integer.Parse("179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858368"));
      AssertEqual(new Integer(FP107.MaxValue), Integer.Parse("179769313486231580793728971405302307166001572487395108634089161737810574079057259642326644280530350389102191776040391417849536235805032710433848589497582645208959795824728567633954093335158118954813353848759795231931608806559135682943768914026291156873243967921161782282609668471618765104228873324865488158720"));
      AssertEqual(-12345, new Integer(-12345.6789m));
      AssertEqual(new Integer(0.12345678901234567890123456789m), Integer.Zero);
      Assert.AreEqual(float.MinValue, (float)new Integer(float.MinValue));
      Assert.AreEqual(double.MaxValue, (double)new Integer(double.MaxValue));
      Assert.AreEqual(FP107.MaxValue, (FP107)new Integer(FP107.MaxValue));
      Assert.AreEqual(decimal.MaxValue, (decimal)new Integer(decimal.MaxValue));

      // test conversions to various other integer types
      Assert.AreEqual((byte)100, (byte)new Integer(100));
      Assert.AreEqual((sbyte)100, (sbyte)new Integer(100));
      Assert.AreEqual((sbyte)-100, (sbyte)new Integer(-100));
      Assert.AreEqual((ushort)10000, (ushort)new Integer(10000));
      Assert.AreEqual((short)10000, (short)new Integer(10000));
      Assert.AreEqual((short)-10000, (short)new Integer(-10000));
      Assert.AreEqual(1000000u, (uint)new Integer(1000000));
      Assert.AreEqual(1000000, (uint)new Integer(1000000));
      Assert.AreEqual(-1000000, (int)new Integer(-1000000));
      Assert.AreEqual(1000000000000uL, (ulong)new Integer(1000000000000));
      Assert.AreEqual(1000000000000L, (ulong)new Integer(1000000000000));
      Assert.AreEqual(-1000000000000L, (long)new Integer(-1000000000000));

      // test truncation
      Assert.AreEqual(unchecked((byte)1000), (byte)new Integer(1000));
      Assert.AreEqual(unchecked((byte)-1000), (byte)new Integer(-1000));
      Assert.AreEqual(unchecked((sbyte)1000), (sbyte)new Integer(1000));
      Assert.AreEqual(unchecked((sbyte)-1000), (sbyte)new Integer(-1000));
      Assert.AreEqual(unchecked((ushort)1000000), (ushort)new Integer(1000000));
      Assert.AreEqual(unchecked((ushort)-1000000), (ushort)new Integer(-1000000));
      Assert.AreEqual(unchecked((short)1000000), (short)new Integer(1000000));
      Assert.AreEqual(unchecked((short)-1000000), (short)new Integer(-1000000));
      Assert.AreEqual(unchecked((uint)1000000000000L), (uint)new Integer(1000000000000L));
      Assert.AreEqual(unchecked((uint)-1000000000000L), (uint)new Integer(-1000000000000L));
      Assert.AreEqual(unchecked((int)1000000000000L), (int)new Integer(1000000000000L));
      Assert.AreEqual(unchecked((int)-1000000000000L), (int)new Integer(-1000000000000L));
      Assert.AreEqual(unchecked((long)7766279631452241920UL), (long)Integer.Pow(10, 20));
      Assert.AreEqual(7766279631452241920UL, (ulong)Integer.Pow(10, 20));

      // test IConvertible extras
      Assert.IsFalse(((IConvertible)Integer.Zero).ToBoolean(null));
      Assert.IsTrue(((IConvertible)Integer.One).ToBoolean(null));
      Assert.AreEqual((char)50000, ((IConvertible)new Integer(50000)).ToChar(null));

      // test conversions from string
      CultureInfo inv = CultureInfo.InvariantCulture;
      AssertEqual(50, Integer.Parse("+5e+1"));
      AssertEqual(333, Integer.Parse("333.999"));
      AssertEqual(123, Integer.Parse("12395.6789E-2"));
      AssertEqual(0, Integer.Parse("-.142"));
      AssertEqual(-77, Integer.Parse("(77)"));
      AssertEqual(123, Integer.Parse("  % 0, 1 ,234 .50E+1+  ", inv)); // percent
      AssertEqual(-123, Integer.Parse(" ( 0 0, 1 ,234500 E-1‰- ) ", inv)); // negative, permille
      AssertEqual(-123, Integer.Parse("¤123.45-", inv)); // negative, currency
      AssertEqual(-5000000000, Integer.Parse("- 0x12a05F200")); // hexadecimal
      AssertEqual((ulong)uint.MaxValue+1, Integer.Parse("0x100000000")); // hexadecimal

      // test string formatting
      Integer tv = -12345;
      Assert.AreEqual("-12345", tv.ToString());
      Assert.AreEqual("(¤12,345)", tv.ToString("C", inv));
      Assert.AreEqual("¤1,000,000", new Integer(1e6).ToString("C", inv));
      Assert.AreEqual("-1.2345E+4", tv.ToString("E", inv));
      Assert.AreEqual("-1.2345e+4", tv.ToString("e", inv));
      Assert.AreEqual("-1e+4", tv.ToString("e0", inv));
      Assert.AreEqual("-1.2e+4", tv.ToString("e1", inv));
      Assert.AreEqual("-1.23e+4", tv.ToString("e2", inv));
      Assert.AreEqual("-1.234e+4", tv.ToString("e3", inv));
      Assert.AreEqual("-1.235e+4", new Integer(-12346).ToString("e3", inv));
      Assert.AreEqual("-1.2345e+4", tv.ToString("e4", inv));
      Assert.AreEqual("-12345", tv.ToString("F", inv));
      Assert.AreEqual("100000", new Integer(100000).ToString("F", inv));
      Assert.AreEqual("-12345", tv.ToString("G", inv));
      Assert.AreEqual("1E+5", new Integer(1e5).ToString("G", inv));
      Assert.AreEqual("12345678901234", new Integer(12345678901234).ToString("G", inv));
      Assert.AreEqual("1.2346E+9", new Integer(1234560000).ToString("G4", inv));
      Assert.AreEqual("-1,234,500 %", tv.ToString("P", inv));
      Assert.AreEqual("-12345", tv.ToString("R", inv));
      Assert.AreEqual("-0x3039", tv.ToString("x", inv));
      Assert.AreEqual("-0x12A05F200", new Integer(-5000000000).ToString("X"));
      Assert.AreEqual("0x100000000", new Integer((ulong)uint.MaxValue+1).ToString("x"));
      Assert.AreEqual("0x10000000a", new Integer((ulong)uint.MaxValue+11).ToString("x"));
      
      // test long symbols
      NumberFormatInfo nfi = (NumberFormatInfo)inv.NumberFormat.Clone();
      nfi.CurrencySymbol = "$/=";
      nfi.PercentSymbol  = "pct";
      nfi.PerMilleSymbol = "pml";
      nfi.NegativeSign   = "minus";
      nfi.PositiveSign   = "plus";
      AssertEqual(-5, Integer.Parse("minus500 pct", nfi));
      AssertEqual(5, Integer.Parse("5 plus", nfi));
      AssertEqual(-5, Integer.Parse("5 minus", nfi));
      AssertEqual(-5, Integer.Parse("5000 pmlminus", nfi));

      // test grouping
      AssertEqual(1234567890, Integer.Parse("1,2345,678,90"));
      nfi.NumberGroupSizes = new int[] { 2, 3, 4 };
      Assert.AreEqual("1,2345,678,90", new Integer(1234567890).ToString("N", nfi));
      Assert.AreEqual("1,2345,6789,000,00", new Integer(12345678900000).ToString("N", nfi));
      nfi.NumberGroupSizes = new int[] { 2, 3, 0 };
      Assert.AreEqual("12345,678,90", new Integer(1234567890).ToString("N", nfi));

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

      // test saving and loading
      MemoryStream ms = new MemoryStream();
      using(BinaryWriter writer = new BinaryWriter(ms, false))
      {
        new Integer(5000000000).Save(writer);
        new Integer(-5000000000).Save(writer);
        Integer.Zero.Save(writer);
      }
      ms.Position = 0;
      using(BinaryReader reader = new BinaryReader(ms))
      {
        AssertEqual(5000000000, new Integer(reader));
        AssertEqual(-5000000000, new Integer(reader));
        Assert.IsTrue(new Integer(reader) == Integer.Zero);
      }

      // test conversion exceptions
      TestHelpers.TestException<ArgumentOutOfRangeException>(delegate { new Integer(double.NaN); });
      TestHelpers.TestException<ArgumentOutOfRangeException>(delegate { new Integer(double.PositiveInfinity); });
      TestHelpers.TestException<ArgumentOutOfRangeException>(delegate { new Integer(double.NegativeInfinity); });
      TestHelpers.TestException<ArgumentOutOfRangeException>(delegate { new Integer(FP107.NaN); });
      TestHelpers.TestException<ArgumentOutOfRangeException>(delegate { new Integer(FP107.PositiveInfinity); });
      TestHelpers.TestException<ArgumentOutOfRangeException>(delegate { new Integer(FP107.NegativeInfinity); });
      TestHelpers.TestException<OverflowException>(delegate { ((IConvertible)new Integer(1000)).ToByte(null); });
      TestHelpers.TestException<OverflowException>(delegate { ((IConvertible)new Integer(200)).ToSByte(null); });
      TestHelpers.TestException<OverflowException>(delegate { ((IConvertible)new Integer(100000)).ToUInt16(null); });
      TestHelpers.TestException<OverflowException>(delegate { ((IConvertible)new Integer(100000)).ToChar(null); });
      TestHelpers.TestException<OverflowException>(delegate { ((IConvertible)new Integer(50000)).ToInt16(null); });
      TestHelpers.TestException<OverflowException>(delegate { ((IConvertible)new Integer(5000000000)).ToUInt32(null); });
      TestHelpers.TestException<OverflowException>(delegate { ((IConvertible)new Integer(2500000000)).ToInt32(null); });
      TestHelpers.TestException<OverflowException>(delegate { ((IConvertible)Integer.Pow(10, 20)).ToUInt64(null); });
      TestHelpers.TestException<OverflowException>(delegate { ((IConvertible)Integer.Pow(10, 19)).ToInt64(null); });
      TestHelpers.TestException<OverflowException>(delegate { ((IConvertible)Integer.MinusOne).ToByte(null); });
      TestHelpers.TestException<OverflowException>(delegate { ((IConvertible)Integer.MinusOne).ToUInt16(null); });
      TestHelpers.TestException<OverflowException>(delegate { ((IConvertible)Integer.MinusOne).ToChar(null); });
      TestHelpers.TestException<OverflowException>(delegate { ((IConvertible)Integer.MinusOne).ToUInt32(null); });
      TestHelpers.TestException<OverflowException>(delegate { ((IConvertible)Integer.MinusOne).ToUInt64(null); });
      TestHelpers.TestException<InvalidCastException>(delegate { ((IConvertible)Integer.One).ToDateTime(null); });
    }

    [Test]
    public void T03_Arithmetic()
    {
      // test basic addition, subtraction, multiplication, division, and remainder
      TestArithmetic(0, 1);
      TestArithmetic(10, 1);
      TestArithmetic(0, 40);
      TestArithmetic(2, 40);
      TestArithmetic(17, 12345);
      TestArithmetic(65536, 12345); // power of two that's also a power of 256 (we have fast paths for powers of two)
      TestArithmetic(1024, 12345);  // power of two that's not a power of 256
      TestArithmetic(1000000000L, 10);
      TestArithmetic((long)int.MinValue, int.MaxValue);
      TestArithmetic((long)-int.MaxValue, int.MaxValue);
      TestArithmetic((long)int.MaxValue+1, (long)int.MaxValue+1);
      TestArithmetic(uint.MaxValue, 1);

      // more addition
      TestAdd(int.MinValue, int.MaxValue, false);
      TestAdd(-int.MaxValue, int.MaxValue, false);
      TestAdd(long.MinValue, long.MaxValue, false);
      TestAdd(-long.MaxValue, long.MaxValue, false);
      TestAdd(5000000000, 4000000000, true);
      Assert.AreEqual(Integer.Parse("61392422837528727192"), Integer.Parse("19482599269902521726") + Integer.Parse("41909823567626205466"));
      Integer a, b;
      for(a=0, b=0; b <= 1000000000; b += 500000) a += b;
      AssertEqual(1000500000000, a);

      // more subtraction
      TestSubtract(5000000000, 4000000000);
      AssertEqual(Integer.Parse("-22427224297723683740"), Integer.Parse("19482599269902521726") - Integer.Parse("41909823567626205466"));

      // more multiplication
      a = Integer.Parse("-19482599269902521727");
      AssertEqual(a*a, a.Square());
      AssertEqual(Integer.Parse("816512298040377808979744885034004954316"), Integer.Parse("19482599269902521726") * Integer.Parse("41909823567626205466"));
      a = 1;
      for(int i=0; i<10; i++) a.UnsafeMultiply(10u);
      AssertEqual(10000000000, a);

      // division & remainder
      a = Integer.Parse("816512298040377808998191629177617027658");
      b = Integer.Parse("17909823567626205466");
      AssertEqual(Integer.Parse("45590192162267043368"), a / b);
      AssertEqual(Integer.Parse("10572194012916378170"), a % b);
      a = Integer.DivRem(a, b, out b);
      AssertEqual(Integer.Parse("45590192162267043368"), a);
      AssertEqual(Integer.Parse("10572194012916378170"), b);

      // increment/decrement
      a = -8;
      b = 8;
      for(int i=-8; i<=8; a++, b--, i++)
      {
        AssertEqual(i, a);
        AssertEqual(-i, b);
      }
      a = new Integer(uint.MaxValue);
      AssertEqual((ulong)uint.MaxValue+1, ++a);
      AssertEqual(uint.MaxValue, --a);

      // unsafe increment/decrement
      a = Integer.MinusOne; // as a special case, MinusOne, Zero, and One are safe for unsafe methods
      a.UnsafeIncrement();
      AssertEqual(Integer.Zero, a);
      Assert.IsTrue(Integer.MinusOne == -1);
      a.UnsafeIncrement();
      AssertEqual(Integer.One, a);
      a.UnsafeIncrement();
      AssertEqual(2, a);

      a = Integer.One;
      a.UnsafeDecrement();
      AssertEqual(a, Integer.Zero);
      AssertEqual(1, Integer.One);
      a.UnsafeDecrement();
      AssertEqual(a, Integer.MinusOne);
      a.UnsafeDecrement();
      AssertEqual(-2, a);

      a = uint.MaxValue;
      a.UnsafeIncrement();
      AssertEqual((ulong)uint.MaxValue+1, a);

      a = -uint.MaxValue;
      a.UnsafeDecrement();
      AssertEqual((long)-uint.MaxValue-1, a);

      // simple functions
      AssertEqual(Integer.One, Integer.MinusOne.Abs());
      AssertEqual(Integer.One, Integer.One.Abs());
      AssertEqual(Integer.Zero, Integer.Zero.Abs());

      // pow
      AssertEqual(Integer.Zero, Integer.Pow(0, 1));
      AssertEqual(Integer.One, Integer.Pow(0, 0));
      AssertEqual(Integer.One, Integer.Pow(-1, 0));
      AssertEqual(Integer.One, Integer.Pow(1, 0));
      AssertEqual(Integer.One, Integer.Pow(1, 5));
      AssertEqual(Integer.One, Integer.Pow(1, -5));
      AssertEqual(Integer.One, Integer.Pow(-1, -4));
      AssertEqual(Integer.One, Integer.Pow(-1, 0));
      AssertEqual(Integer.One, Integer.Pow(-1, 2));
      AssertEqual(Integer.MinusOne, Integer.Pow(-1, -3));
      AssertEqual(Integer.MinusOne, Integer.Pow(-1, 3));
      AssertEqual(-5, Integer.Pow(-5, 1));
      ulong p = 1;
      for(int i=0; i<64; p *= 2, i++) AssertEqual(p, Integer.Pow(2, i));
      p = 1;
      for(int i=0; i<20; p *= 10, i++) AssertEqual(p, Integer.Pow(10, i));
      p = 1;
      for(int i=0; i<15; p *= 17, i++) AssertEqual(p, Integer.Pow(17, i));
      AssertEqual(Integer.Pow(10, 24), Integer.Pow(10, 12).Square());
      AssertEqual(Integer.Pow(10, 50), Integer.Pow(10, 25).Square());
      AssertEqual(9, Integer.Pow(-3, 2));
      AssertEqual(-27, Integer.Pow(-3, 3));
      TestHelpers.TestException<ArgumentOutOfRangeException>(delegate { Integer.Pow(0, -1); });

      // unsafe set
      a = Integer.Zero;
      a.UnsafeSet(3);
      AssertEqual(3, a);
      a = Integer.One;
      a.UnsafeSet(4);
      AssertEqual(4, a);
      a = Integer.MinusOne;
      a.UnsafeSet(7);
      AssertEqual(7, a);
      a.UnsafeSet(-3);
      AssertEqual(-3, a);
      a.UnsafeSet(20u);
      AssertEqual(20, a);

      a.UnsafeSet(5000000000);
      AssertEqual(5000000000, a);
      a = Integer.Zero;
      a.UnsafeSet(-5000000000);
      AssertEqual(-5000000000, a);

      b = Integer.Parse("816512298040377808998191629177617027658");
      a = b.Clone();
      a.UnsafeSet(5);
      AssertEqual(5, a);
      a = b.Clone();
      a.UnsafeSet(-5000000000);
      AssertEqual(-5000000000, a);

      AssertEqual(0, Integer.Zero);
      AssertEqual(1, Integer.One);
      AssertEqual(-1, Integer.MinusOne);
    }

    [Test]
    public void T04_Bitwise()
    {
      TestGetBit(11, "1011");
      TestGetBit(5000000000, "100101010000001011111001000000000");
      TestGetBit(-5000000000, "011010101111110100000111000000000");
      TestGetBit(-uint.MaxValue-1, "100000000000000000000000000000000");

      TestShift(0, 1);
      TestShift(500, 1);
      TestShift(500, 2);
      TestShift(500, 3);
      TestShift(32, 5);
      TestShift(32, 6);
      TestShift(500, 100);
      TestShift(500000000000, 32);
      TestShift(500000000000000000, 40);

      TestNot(0);
      TestNot(1);
      TestNot(2);
      TestNot(3);
      TestNot(int.MaxValue);
      TestNot((uint)int.MaxValue+1);
      TestNot(uint.MaxValue);
      TestNot(5000000000);
      TestNot(500000000000000000);
      TestNot((long)uint.MaxValue+1);
      TestNot(long.MaxValue);
      Integer a = (ulong)long.MaxValue+1, b;
      AssertEqual(Integer.Parse("-9223372036854775809"), ~a);
      AssertEqual(a, ~~a);
      AssertEqual(-a, ~(a-1));

      TestAndOr(0, 1);
      TestAndOr(0, 1024);
      TestAndOr(0x80000000, 0x7FFFFFFF);
      TestAndOr(0x00010000, 0x00010000);
      TestAndOr(0x00010000, 0x00020000);
      TestAndOr(0xFFFF0000, 0x0000FFFF);
      TestAndOr(0x0000000100000000, 0x00000000FFFFFFFF);
      TestAndOr(0x0000000000000001, 0x0000000100000000);
      TestAndOr(0xFFFFFFFF00000000, 0x00000000FFFFFFFF);
      a = Integer.Parse("197481328345907517401944277496");
      b = Integer.Parse("853584882143245824844097430346");
      AssertEqual(Integer.Parse("180140343486486871498037428552"), a & b);
      AssertEqual(Integer.Parse("673444538656758953346060001800"), -a & b);
      AssertEqual(Integer.Parse("17340984859420645903906848944"), a & -b);
      AssertEqual(Integer.Parse("-870925867002666470748004279296"), -a & -b);
      AssertEqual(Integer.Parse("870925867002666470748004279290"), a | b);
      AssertEqual(Integer.Parse("-17340984859420645903906848950"), -a | b);
      AssertEqual(Integer.Parse("-673444538656758953346060001794"), a | -b);
      AssertEqual(Integer.Parse("-180140343486486871498037428546"), -a | -b);

      TestSetBit(0, 0);
      TestSetBit(0, 1);
      TestSetBit(1, 0);
      TestSetBit(1, 1);
      TestSetBit(1, 2);
      TestSetBit(1, 4);
      TestSetBit(1, 31);
      TestSetBit(1, 32);
      TestSetBit(1, 33);
      for(int i=0; i<=16; i++) TestSetBit(50000, i);
      for(long i=0x100000000; i<=0x100000001; i++)
      {
        TestSetBit(i, 0);
        TestSetBit(i, 1);
        TestSetBit(i, 2);
        TestSetBit(i, 15);
        TestSetBit(i, 31);
        TestSetBit(i, 32);
        TestSetBit(i, 33);
      }
    }

    static void AssertEqual(int expected, Integer value)
    {
      Assert.IsTrue(value == expected);
      Assert.AreEqual(ComputeBitLength((uint)(expected < 0 ? -expected : expected)), value.BitLength);
    }

    static void AssertEqual(uint expected, Integer value)
    {
      Assert.IsTrue(value == expected);
      Assert.AreEqual(ComputeBitLength(expected), value.BitLength);
    }

    static void AssertEqual(long expected, Integer value)
    {
      Assert.IsTrue(value == expected);
      Assert.AreEqual(ComputeBitLength((ulong)(expected < 0 ? -expected : expected)), value.BitLength);
    }

    static void AssertEqual(ulong expected, Integer value)
    {
      Assert.IsTrue(value == expected);
      Assert.AreEqual(ComputeBitLength(expected), value.BitLength);
    }

    static void AssertEqual(Integer expected, Integer value)
    {
      Assert.AreEqual(expected, value);
      Assert.AreEqual(expected.BitLength, value.BitLength);
      Assert.AreEqual(ComputeBitLength(value), value.BitLength);
    }

    static int ComputeBitLength(uint v)
    {
      return 32 - BinaryUtility.CountLeadingZeros(v);
    }

    static int ComputeBitLength(ulong v)
    {
      return 64 - BinaryUtility.CountLeadingZeros(v);
    }

    static int ComputeBitLength(Integer v)
    {
      uint[] data = v.GetBits();
      for(int i=data.Length-1; i >= 0; i--)
      {
        if(data[i] != 0) return ComputeBitLength(data[i]) + i*32;
      }
      return 0;
    }

    static void TestAdd(int a, int b, bool negate)
    {
      for(int i=0; i<4; i++)
      {
        int expected = checked(a + b);
        for(int j=0; j<2; j++)
        {
          Integer iv = a;
          AssertEqual(expected, iv+b);
          AssertEqual(expected, b+iv);
          AssertEqual(expected, iv+(long)b);
          AssertEqual(expected, (long)b+iv);
          iv.UnsafeAdd(b);
          AssertEqual(expected, iv);
          if(b >= 0)
          {
            iv = a;
            AssertEqual(expected, iv+(uint)b);
            AssertEqual(expected, (uint)b+iv);
            AssertEqual(expected, iv+(ulong)b);
            AssertEqual(expected, (ulong)b+iv);
            iv.UnsafeAdd((uint)b);
            AssertEqual(expected, iv);
          }
          AssertEqual(new Integer(expected), new Integer(a) + new Integer(b));
          Utility.Swap(ref a, ref b);
        }

        if(!negate) break;
        else if((i&1) == 0) a = -a;
        else b = -b;
      }
    }

    static void TestAdd(long a, long b, bool negate)
    {
      for(int i=0; i<4; i++)
      {
        long expected = checked(a + b);
        for(int j=0; j<2; Utility.Swap(ref a, ref b), j++)
        {
          Integer iv = a;
          AssertEqual(expected, iv+b);
          AssertEqual(expected, b+iv);
          if(b >= 0)
          {
            AssertEqual(expected, iv+(ulong)b);
            AssertEqual(expected, (ulong)b+iv);
          }
          AssertEqual(new Integer(expected), new Integer(a) + new Integer(b));
        }

        if(!negate) break;
        else if((i&1) == 0) a = -a;
        else b = -b;
      }
    }

    static void TestAnd(int a, int b)
    {
      for(int i=0; i<4; i++)
      {
        long expected = a & b;
        for(int j=0; j<2; Utility.Swap(ref a, ref b), j++)
        {
          Integer iv = a;
          AssertEqual(expected, iv&b);
          AssertEqual(expected, b&iv);
          AssertEqual(expected, iv&(long)b);
          AssertEqual(expected, (long)b&iv);
          iv.UnsafeBitwiseAnd(b);
          AssertEqual(expected, iv);
          if(b >= 0)
          {
            iv = a;
            AssertEqual(expected, iv&(uint)b);
            AssertEqual(expected, (uint)b&iv);
            AssertEqual(expected, iv&(ulong)b);
            AssertEqual(expected, (ulong)b&iv);
            iv.UnsafeBitwiseAnd((uint)b);
            AssertEqual(expected, iv);
          }
          AssertEqual(expected, new Integer(a) & new Integer(b));
        }

        if((i&1) == 0) a = -a;
        else b = -b;
      }
    }

    static void TestAnd(long a, long b)
    {
      for(int i=0; i<4; i++)
      {
        long expected = a & b;
        for(int j=0; j<2; Utility.Swap(ref a, ref b), j++)
        {
          Integer iv = a;
          AssertEqual(expected, iv&b);
          AssertEqual(expected, b&iv);
          if(b >= 0)
          {
            AssertEqual(expected, iv&(ulong)b);
            AssertEqual(expected, (ulong)b&iv);
          }
          AssertEqual(expected, new Integer(a) & new Integer(b));
        }

        if((i&1) == 0) a = -a;
        else b = -b;
      }
    }

    static void TestAnd(ulong a, ulong b)
    {
      ulong expected = a & b;
      for(int j=0; j<2; Utility.Swap(ref a, ref b), j++)
      {
        Integer iv = a;
        AssertEqual(expected, iv&b);
        AssertEqual(expected, b&iv);
        AssertEqual(expected, new Integer(a) & new Integer(b));
      }
    }

    static void TestAndOr(int a, int b)
    {
      TestAnd(a, b);
      TestOr(a, b);
    }

    static void TestAndOr(long a, long b)
    {
      TestAnd(a, b);
      TestOr(a, b);
    }

    static void TestAndOr(ulong a, ulong b)
    {
      TestAnd(a, b);
      TestOr(a, b);
    }

    static void TestArithmetic(int a, int b)
    {
      TestAdd(a, b, true);
      TestSubtract(a, b);
      TestMultiply(a, b);
      TestDivide(a, b);
    }

    static void TestArithmetic(long a, long b)
    {
      TestAdd(a, b, true);
      TestSubtract(a, b);
      TestMultiply(a, b);
      TestDivide(a, b);
    }

    static void TestCurrencyFormat(bool negative, string pattern, int patternNumber)
    {
      NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
      if(negative) nfi.CurrencyNegativePattern = patternNumber;
      else nfi.CurrencyPositivePattern = patternNumber;
      pattern = pattern.Replace("$", nfi.CurrencySymbol).Replace("-", nfi.NegativeSign).Replace("n", "1");
      Assert.AreEqual(pattern, new Integer(negative ? -1 : 1).ToString("C", nfi));
      Assert.AreEqual(new Integer(negative ? -1 : 1), Integer.Parse(pattern, nfi));
    }

    static void TestDivide(int a, int b)
    {
      for(int i=0; i<4; i++)
      {
        for(int j=0; j<2; Utility.Swap(ref a, ref b), j++)
        {
          if(b == 0) continue;

          int expected = a / b, remainder = a % b;
          Integer iv = a;
          AssertEqual(expected, iv/b);
          AssertEqual(expected, iv/(long)b);
          AssertEqual(remainder, iv%b);
          AssertEqual(remainder, iv%(long)b);
          AssertEqual(remainder, iv.UnsafeDivide(b));
          AssertEqual(expected, iv);

          if(b >= 0)
          {
            iv = a;
            AssertEqual(expected, iv/(uint)b);
            AssertEqual(expected, iv/(ulong)b);
            AssertEqual(remainder, iv%(uint)b);
            AssertEqual(remainder, iv%(ulong)b);
            AssertEqual(remainder, iv.UnsafeDivide((uint)b));
            AssertEqual(expected, iv);
          }

          iv = b;
          AssertEqual(expected, a/iv);
          AssertEqual(expected, (long)a/iv);
          AssertEqual(remainder, a%iv);
          AssertEqual(remainder, (long)a%iv);
          if(a >= 0)
          {
            AssertEqual(expected, (uint)a/iv);
            AssertEqual(expected, (ulong)a/iv);
            AssertEqual(remainder, (uint)a%iv);
            AssertEqual(remainder, (ulong)a%iv);
          }

          AssertEqual(new Integer(expected), new Integer(a) / new Integer(b));
          AssertEqual(new Integer(remainder), new Integer(a) % new Integer(b));
          Integer rem;
          AssertEqual(new Integer(expected), Integer.DivRem(a, b, out rem));
          AssertEqual(new Integer(remainder), rem);
        }

        if((i&1) == 0) a = -a;
        else b = -b;
      }
    }

    static void TestDivide(long a, long b)
    {
      for(int i=0; i<4; i++)
      {
        for(int j=0; j<2; Utility.Swap(ref a, ref b), j++)
        {
          if(b == 0) continue;

          long expected = a / b, remainder = a % b;
          Integer iv = a;
          AssertEqual(expected, iv/b);
          AssertEqual(remainder, iv%b);
          if(b >= 0)
          {
            AssertEqual(expected, iv/(ulong)b);
            AssertEqual(remainder, iv%(ulong)b);
          }

          iv = b;
          AssertEqual(expected, a/iv);
          AssertEqual(remainder, a%iv);
          if(a >= 0)
          {
            AssertEqual(expected, (ulong)a/iv);
            AssertEqual(remainder, (ulong)a%iv);
          }

          AssertEqual(new Integer(expected), new Integer(a) / new Integer(b));
          AssertEqual(new Integer(remainder), new Integer(a) % new Integer(b));
        }

        if((i&1) == 0) a = -a;
        else b = -b;
      }
    }

    static void TestGetBit(Integer a, string bits)
    {
      Assert.AreEqual(bits.Length, a.BitLength);
      for(int i=1; i <= bits.Length; i++) Assert.AreEqual(bits[bits.Length-i] != '0', a.GetBit(i-1));
      for(int i=0; i<3; i++) Assert.AreEqual(a.IsNegative, a.GetBit(a.BitLength+i)); // test a few bits past the end
    }

    static void TestMultiply(int a, int b)
    {
      for(int i=0; i<4; i++)
      {
        int expected = checked(a * b);
        for(int j=0; j<2; j++)
        {
          Integer iv = a;
          AssertEqual(expected, iv*b);
          AssertEqual(expected, b*iv);
          AssertEqual(expected, iv*(long)b);
          AssertEqual(expected, (long)b*iv);
          iv.UnsafeMultiply(b);
          AssertEqual(expected, iv);
          if(b >= 0)
          {
            iv = a;
            AssertEqual(expected, iv*(uint)b);
            AssertEqual(expected, (uint)b*iv);
            AssertEqual(expected, iv*(ulong)b);
            AssertEqual(expected, (ulong)b*iv);
            iv.UnsafeMultiply((uint)b);
            AssertEqual(expected, iv);
          }
          AssertEqual(new Integer(expected), new Integer(a) * new Integer(b));
          Utility.Swap(ref a, ref b);
        }

        if((i&1) == 0) a = -a;
        else b = -b;
      }
    }

    static void TestMultiply(long a, long b)
    {
      for(int i=0; i<4; i++)
      {
        long expected = checked(a * b);
        for(int j=0; j<2; j++)
        {
          Integer iv = a;
          AssertEqual(expected, iv*b);
          AssertEqual(expected, b*iv);
          if(b >= 0)
          {
            AssertEqual(expected, iv*(ulong)b);
            AssertEqual(expected, (ulong)b*iv);
          }
          AssertEqual(new Integer(expected), new Integer(a) * new Integer(b));
          Utility.Swap(ref a, ref b);
        }

        if((i&1) == 0) a = -a;
        else b = -b;
      }
    }

    static void TestNot(long v)
    {
      for(int i=0; i<2; v=-v, i++)
      {
        Integer iv = v;
        AssertEqual(~v, ~iv);
        AssertEqual(v, ~~iv);
        AssertEqual(-v, ~(iv-1));
        iv.UnsafeBitwiseNegate();
        AssertEqual(~v, iv);
      }
    }

    static void TestNumberFormat(string pattern, int patternNumber)
    {
      NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
      nfi.NumberNegativePattern = patternNumber;
      pattern = pattern.Replace("-", nfi.NegativeSign).Replace("n", "1");
      Assert.AreEqual(pattern, Integer.MinusOne.ToString("N", nfi));
      Assert.AreEqual(Integer.MinusOne, Integer.Parse(pattern, nfi));
    }

    static void TestOr(int a, int b)
    {
      for(int i=0; i<4; i++)
      {
        long expected = a | b;
        for(int j=0; j<2; Utility.Swap(ref a, ref b), j++)
        {
          Integer iv = a;
          AssertEqual(expected, iv|b);
          AssertEqual(expected, b|iv);
          AssertEqual(expected, iv|(long)b);
          AssertEqual(expected, (long)b|iv);
          iv.UnsafeBitwiseOr(b);
          AssertEqual(expected, iv);
          if(b >= 0)
          {
            iv = a;
            AssertEqual(expected, iv|(uint)b);
            AssertEqual(expected, (uint)b|iv);
            AssertEqual(expected, iv|(ulong)b);
            AssertEqual(expected, (ulong)b|iv);
            iv.UnsafeBitwiseOr((uint)b);
            AssertEqual(expected, iv);
          }
          AssertEqual(expected, new Integer(a) | new Integer(b));
        }

        if((i|1) == 0) a = -a;
        else b = -b;
      }
    }

    static void TestOr(long a, long b)
    {
      for(int i=0; i<4; i++)
      {
        long expected = a | b;
        for(int j=0; j<2; Utility.Swap(ref a, ref b), j++)
        {
          Integer iv = a;
          AssertEqual(expected, iv|b);
          AssertEqual(expected, b|iv);
          if(b >= 0)
          {
            AssertEqual(expected, iv|(ulong)b);
            AssertEqual(expected, (ulong)b|iv);
          }
          AssertEqual(expected, new Integer(a) | new Integer(b));
        }

        if((i|1) == 0) a = -a;
        else b = -b;
      }
    }

    static void TestOr(ulong a, ulong b)
    {
      ulong expected = a | b;
      for(int j=0; j<2; Utility.Swap(ref a, ref b), j++)
      {
        Integer iv = a;
        AssertEqual(expected, iv|b);
        AssertEqual(expected, b|iv);
        AssertEqual(expected, new Integer(a) | new Integer(b));
      }
    }

    static void TestPercentFormat(bool negative, string pattern, int patternNumber)
    {
      NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
      if(negative) nfi.PercentNegativePattern = patternNumber;
      else nfi.PercentPositivePattern = patternNumber;
      pattern = pattern.Replace("%", nfi.PercentSymbol).Replace("-", nfi.NegativeSign).Replace("n", "100");
      Integer value = negative ? -1 : 1;
      Assert.AreEqual(pattern, value.ToString("P", nfi));
      Assert.AreEqual(value, Integer.Parse(pattern, nfi));
    }

    static void TestSetBit(long a, int bit)
    {
      Integer i = a;
      i.UnsafeSetBit(bit, false);
      AssertEqual(a & ~(1L<<bit), i);
      i = a;
      i.UnsafeSetBit(bit, true);
      AssertEqual(a | (1L<<bit), i);

      a = -a;
      i = a;
      i.UnsafeSetBit(bit, false);
      AssertEqual(a & ~(1L<<bit), i);
      i = a;
      i.UnsafeSetBit(bit, true);
      AssertEqual(a | (1L<<bit), i);
    }

    static void TestShift(Integer a, int shift)
    {
      for(int i=0; i<2; a=-a, i++)
      {
        Integer left = a << shift, left2 = a >> -shift, right = a >> shift, right2 = a << -shift;
        Integer exleft = a * Integer.Pow(2, shift), exright = a / Integer.Pow(2, shift);
        Assert.AreEqual(exleft, left);
        Assert.AreEqual(exleft, left2);
        Assert.AreEqual(a.IsZero ? 0 : a.BitLength+shift, left.BitLength);
        Assert.AreEqual(a.IsZero ? 0 : a.BitLength+shift, left2.BitLength);
        Assert.AreEqual(exright, right);
        Assert.AreEqual(exright, right2);
        Assert.AreEqual(Math.Max(0, a.BitLength-shift), right.BitLength);
        Assert.AreEqual(Math.Max(0, a.BitLength-shift), right2.BitLength);

        Integer b = a.Clone();
        b.UnsafeLeftShift(shift);
        Assert.AreEqual(exleft, b);
        Assert.AreEqual(exleft.BitLength, b.BitLength);

        b = a.Clone();
        b.UnsafeRightShift(shift);
        Assert.AreEqual(exright, b);
        Assert.AreEqual(exright.BitLength, b.BitLength);
      }
    }

    static void TestSubtract(int a, int b)
    {
      for(int i=0; i<4; i++)
      {
        for(int j=0; j<2; j++)
        {
          int expected = checked(a - b);
          Integer iv = a;
          AssertEqual(expected, iv-b);
          AssertEqual(-expected, b-iv);
          AssertEqual(expected, iv-(long)b);
          AssertEqual(-expected, (long)b-iv);
          iv.UnsafeSubtract(b);
          AssertEqual(expected, iv);
          if(b >= 0)
          {
            iv = a;
            AssertEqual(expected, iv-(uint)b);
            AssertEqual(-expected, (uint)b-iv);
            AssertEqual(expected, iv-(ulong)b);
            AssertEqual(-expected, (ulong)b-iv);
            iv.UnsafeSubtract((uint)b);
            AssertEqual(expected, iv);
          }
          AssertEqual(new Integer(expected), new Integer(a) - new Integer(b));
          Utility.Swap(ref a, ref b);
        }

        if((i&1) == 0) a = -a;
        else b = -b;
      }
    }

    static void TestSubtract(long a, long b)
    {
      for(int i=0; i<4; i++)
      {
        for(int j=0; j<2; j++)
        {
          long expected = checked(a - b);
          Integer iv = a;
          AssertEqual(expected, iv-b);
          AssertEqual(-expected, b-iv);
          if(b >= 0)
          {
            AssertEqual(expected, iv-(ulong)b);
            AssertEqual(-expected, (ulong)b-iv);
          }
          AssertEqual(new Integer(expected), new Integer(a) - new Integer(b));
          Utility.Swap(ref a, ref b);
        }

        if((i&1) == 0) a = -a;
        else b = -b;
      }
    }

    public static long iv { get; set; }
  }
}