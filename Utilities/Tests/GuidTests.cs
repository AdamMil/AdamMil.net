using System;
using AdamMil.Utilities;
using NUnit.Framework;

namespace AdamMil.Utilities.Tests
{

[TestFixture]
public class GuidTests
{
  [Test]
  public void TestGuidParse()
  {
    Guid guid = Guid.NewGuid();
    TestParse(guid.ToString("N"), guid);
    TestParse(guid.ToString("D"), guid);
    TestParse(guid.ToString("b"), guid);
    TestParse(guid.ToString("p"), guid);
    TestParse("{" + guid.ToString("n") + "}", guid);
    TestParse(" \n\t  " + guid.ToString() + "  \n\t", guid);

    Assert.IsFalse(GuidUtility.TryParse("ca761232ed4211cebacd00aa0057b22", out guid));
    Assert.IsFalse(GuidUtility.TryParse("ca761232ed4211cebacd00aa0057b-22", out guid));
    Assert.IsFalse(GuidUtility.TryParse("{ca761232ed4211cebacd00aa0057b223)", out guid));
    Assert.IsFalse(GuidUtility.TryParse(guid.ToString() + "X", false, out guid));
    Assert.IsFalse(GuidUtility.TryParse(guid.ToString() + " ", false, out guid));
    Assert.IsFalse(GuidUtility.TryParse(" " + guid.ToString(), false, out guid));
  }

  static void TestParse(string str, Guid expectedValue)
  {
    Guid value;
    Assert.IsTrue(GuidUtility.TryParse(str, out value));
    Assert.AreEqual(expectedValue, value);
  }
}

} // namespace AdamMil.Utilities.Tests
