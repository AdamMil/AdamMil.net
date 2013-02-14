using AdamMil.Utilities;
using NUnit.Framework;

namespace AdamMil.Utilities.Tests
{

[TestFixture]
public class PrimitiveTests
{
  [Test]
  public void TestPrimitiveParsing()
  {
    TestParse(" 0 ", 0);
    TestParse(" -0 ", 0);
    TestParse("-2147483648", -2147483648);
    TestParse("2147483647", 2147483647);
    TestParse("1000  ", 1000);
    TestParse("  -1000", -1000);
    TestParse("0000000000000100", 100);
    TestFailureInt("- 1000");
    TestFailureInt("2147483648");
    TestFailureInt("4000000000");
    TestFailureInt("8000000000");
    TestFailureInt("16000000000");
    TestFailureInt("-2147483649");
    TestFailureInt("-4000000000");
    TestFailureInt("-8000000000");
    TestFailureInt("-16000000000");
    TestFailureInt("4294967270");
    TestFailureInt("4294967280");
    TestFailureInt("4294967290");
    TestFailureInt("4294967279");
    TestFailureInt("4294967289");
    TestFailureInt("4294967299");

    TestParse(" 0 ", 0u);
    TestParse("2147483647", 2147483647u);
    TestParse("2147483648", 2147483648u);
    TestParse("4294967295", 4294967295u);
    TestParse("000000000000000004294967295", 4294967295u);
    TestParse("  1000   ", 1000u);
    TestFailureUInt("-0");
    TestFailureUInt("-1");
    TestFailureUInt("4294967296");
    TestFailureUInt("4772185870");
    TestFailureUInt("4772185880");
    TestFailureUInt("4772185890");
    TestFailureUInt("4772185879");
    TestFailureUInt("4772185889");
    TestFailureUInt("4772185899");
    TestFailureUInt("8000000000");
    TestFailureUInt("16000000000");
    TestFailureUInt("32000000000");

    TestParse("0", 0L);
    TestParse("-1", -1L);
    TestParse("1844674407370955159", 1844674407370955159L);
    TestParse("1844674407370955160", 1844674407370955160L);
    TestParse("1844674407370955161", 1844674407370955161L);
    TestParse("9223372036854775807", long.MaxValue);
    TestParse("-9223372036854775808", long.MinValue);
    TestFailureLong("18446744073709551590");
    TestFailureLong("18446744073709551600");
    TestFailureLong("18446744073709551610");
    TestFailureLong("-18446744073709551590");
    TestFailureLong("-18446744073709551600");
    TestFailureLong("-18446744073709551610");
    TestFailureLong("18446744073709551599");
    TestFailureLong("18446744073709551609");
    TestFailureLong("18446744073709551619");
    TestFailureLong("-18446744073709551599");
    TestFailureLong("-18446744073709551609");
    TestFailureLong("-18446744073709551619");
    TestFailureLong("9223372036854775808");
    TestFailureLong("-9223372036854775809");
    TestFailureLong("10000000000000000000");
    TestFailureLong("20000000000000000000");
    TestFailureLong("40000000000000000000");
    TestFailureLong("80000000000000000000");
    TestFailureLong("160000000000000000000");
    TestFailureLong("-10000000000000000000");
    TestFailureLong("-20000000000000000000");
    TestFailureLong("-40000000000000000000");
    TestFailureLong("-80000000000000000000");
    TestFailureLong("-160000000000000000000");

    TestParse("0", 0uL);
    TestParse("18446744073709551615", ulong.MaxValue);
    TestFailureULong("-0");
    TestFailureULong("-1");
    TestFailureULong("20496382304121723990");
    TestFailureULong("20496382304121724000");
    TestFailureULong("20496382304121724010");
    TestFailureULong("20496382304121723999");
    TestFailureULong("20496382304121724009");
    TestFailureULong("20496382304121724019");
    TestFailureULong("18446744073709551616");
    TestFailureULong("20000000000000000000");
    TestFailureULong("40000000000000000000");
    TestFailureULong("80000000000000000000");
    TestFailureULong("160000000000000000000");
    TestFailureULong("320000000000000000000");
    TestFailureULong("640000000000000000000");
  }

  static void TestFailureInt(string str)
  {
    int value;
    Assert.IsFalse(InvariantCultureUtility.TryParse(str, out value));
  }

  static void TestFailureLong(string str)
  {
    long value;
    Assert.IsFalse(InvariantCultureUtility.TryParse(str, out value));
  }

  static void TestFailureUInt(string str)
  {
    uint value;
    Assert.IsFalse(InvariantCultureUtility.TryParse(str, out value));
  }

  static void TestFailureULong(string str)
  {
    ulong value;
    Assert.IsFalse(InvariantCultureUtility.TryParse(str, out value));
  }

  static void TestParse(string str, int expectedValue)
  {
    int value;
    Assert.IsTrue(InvariantCultureUtility.TryParse(str, out value));
    Assert.AreEqual(expectedValue, value);
  }

  static void TestParse(string str, uint expectedValue)
  {
    uint value;
    Assert.IsTrue(InvariantCultureUtility.TryParse(str, out value));
    Assert.AreEqual(expectedValue, value);
  }

  static void TestParse(string str, long expectedValue)
  {
    long value;
    Assert.IsTrue(InvariantCultureUtility.TryParse(str, out value));
    Assert.AreEqual(expectedValue, value);
  }

  static void TestParse(string str, ulong expectedValue)
  {
    ulong value;
    Assert.IsTrue(InvariantCultureUtility.TryParse(str, out value));
    Assert.AreEqual(expectedValue, value);
  }
}

} // namespace AdamMil.Utilities.Tests
