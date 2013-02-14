using System;
using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Utilities.Tests
{

  [TestFixture]
  public class XmlTests
  {
    #region TestXmlBoolean
    [Test]
    public void TestXmlBoolean()
    {
      bool value;
      Assert.IsTrue(XmlUtility.TryParse("true", out value) && value);
      Assert.IsTrue(XmlUtility.TryParse("false", out value) && !value);
      Assert.IsTrue(XmlUtility.TryParse(" true", out value) && value);
      Assert.IsTrue(XmlUtility.TryParse("  false ", out value) && !value);
      Assert.IsTrue(XmlUtility.TryParse("1", out value) && value);
      Assert.IsTrue(XmlUtility.TryParse("0", out value) && !value);
      Assert.IsFalse(XmlUtility.TryParse("True", out value));
      Assert.IsFalse(XmlUtility.TryParse("False", out value));
      Assert.IsFalse(XmlUtility.TryParse("", out value));
      Assert.IsFalse(XmlUtility.TryParse("2", out value));
    }
    #endregion

    #region TestXmlDateTime
    [Test]
    public void TestXmlDateTime()
    {
      TestHelpers.AssertEqual(new DateTime(2000, 1, 2, 0, 0, 0, DateTimeKind.Unspecified),
                              XmlUtility.ParseDateTime("2000-01-02"));
      TestHelpers.AssertEqual(new DateTime(2000, 1, 2, 3, 4, 5, 6, DateTimeKind.Unspecified),
                              XmlUtility.ParseDateTime("2000-01-02T03:04:05.006"));
      TestHelpers.AssertEqual(new DateTime(2000, 1, 3, 0, 0, 0, DateTimeKind.Unspecified),
                              XmlUtility.ParseDateTime("2000-01-02T24:00:00"));
      TestHelpers.AssertEqual(new DateTime(2000, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc),
                              XmlUtility.ParseDateTime("2000-01-02T03:04:05.006Z"));
      TestHelpers.AssertEqual(new DateTimeOffset(2000, 1, 2, 3, 4, 5, 6, new TimeSpan(5, 30, 0)),
                              XmlUtility.ParseDateTime("2000-01-02T03:04:05.006+05:30"));
      TestHelpers.AssertEqual(new DateTimeOffset(2000, 1, 2, 3, 4, 5, 6, new TimeSpan(5, 30, 0).Negate()),
                              XmlUtility.ParseDateTime("2000-01-02T03:04:05.006-05:30"));
      TestHelpers.AssertEqual(new DateTimeOffset(2000, 1, 2, 3, 4, 5, 6, TimeSpan.Zero),
                              XmlUtility.ParseDateTime("2000-01-02T03:04:05.006+00:00"));
      TestHelpers.TestException<FormatException>(delegate { XmlUtility.ParseDateTime("2000-01-02T24:00:01"); });
      TestHelpers.TestException<FormatException>(delegate { XmlUtility.ParseDateTime("2000-01-02T25:00:00"); });
      TestHelpers.TestException<FormatException>(delegate { XmlUtility.ParseDateTime("2000-1-2"); });
      TestHelpers.TestException<FormatException>(delegate { XmlUtility.ParseDateTime("2000-01-02T1:00:00"); });
      TestHelpers.TestException<FormatException>(delegate { XmlUtility.ParseDateTime("2000-01-02T05:00"); });
    }
    #endregion

    #region TestXmlDuration
    [Test]
    public void TestXmlDuration()
    {
      XmlDuration d1 = new XmlDuration(new TimeSpan(100, 10, 1, 2, 3));
      Assert.AreEqual(100, d1.Days);
      Assert.AreEqual(10, d1.Hours);
      Assert.AreEqual(1, d1.Minutes);
      Assert.AreEqual(2, d1.WholeSeconds);
      Assert.AreEqual(3, d1.Milliseconds);
      Assert.AreEqual(2.003, d1.Seconds);
      Assert.IsFalse(d1.IsNegative);
      Assert.AreEqual("P100DT10H1M2.003S", d1.ToString());

      XmlDuration d2 = new XmlDuration(new TimeSpan(100, 10, 1, 2, 3).Negate());
      Assert.AreEqual(0, d2.TotalMonths);
      Assert.AreEqual(100, d2.Days);
      Assert.AreEqual(10, d2.Hours);
      Assert.AreEqual(1, d2.Minutes);
      Assert.AreEqual(2, d2.WholeSeconds);
      Assert.AreEqual(3, d2.Milliseconds);
      Assert.AreEqual(2.003, d2.Seconds);
      Assert.IsTrue(d2.IsNegative);
      Assert.AreEqual("-P100DT10H1M2.003S", d2.ToString());
      Assert.AreEqual(d2, XmlDuration.Parse("-P100DT10H1M2.003S"));

      Assert.IsTrue(d1.Negate() == d2);
      Assert.IsTrue(d1 == d2.Negate());
      Assert.IsTrue(d1.Negate().Negate() == d1);
      Assert.IsTrue(d2.Abs() == d1);

      d1 = new XmlDuration(1, 2, 3);
      Assert.AreEqual(1, d1.Years);
      Assert.AreEqual(2, d1.Months);
      Assert.AreEqual(14, d1.TotalMonths);
      Assert.AreEqual("P1Y2M3D", d1.ToString());

      d2 = d1 + new XmlDuration(2, 3, 4);
      Assert.AreEqual(3, d2.Years);
      Assert.AreEqual(5, d2.Months);
      Assert.AreEqual(41, d2.TotalMonths);
      Assert.AreEqual(7, d2.Days);

      DateTime dt = new DateTime(2000, 1, 1).Add(d2);
      Assert.AreEqual(2003, dt.Year);
      Assert.AreEqual(6, dt.Month);
      Assert.AreEqual(8, dt.Day);

      d1 = new XmlDuration(1, 2, 3, 4, 5, 6, 7);
      Assert.AreEqual(1, d1.Years);
      Assert.AreEqual(2, d1.Months);
      Assert.AreEqual(3, d1.Days);
      Assert.AreEqual(4, d1.Hours);
      Assert.AreEqual(5, d1.Minutes);
      Assert.AreEqual(6, d1.WholeSeconds);
      Assert.AreEqual(7, d1.Milliseconds);
      Assert.AreEqual("P1Y2M3DT4H5M6.007S", d1.ToString());
      Assert.AreEqual(d1, XmlDuration.Parse("P1Y2M3DT4H5M6.007S"));

      Assert.AreEqual("P0D", XmlDuration.Zero.ToString());

      d2 = d1.Negate();
      Assert.AreEqual(XmlDuration.Zero, d1 + d2);
      Assert.AreEqual(XmlDuration.Zero, d2 + d1);
      Assert.AreEqual(XmlDuration.Zero, XmlDuration.Zero.Negate());
      Assert.AreEqual(XmlDuration.Zero, XmlDuration.MaxValue + XmlDuration.MinValue);
      Assert.AreEqual(XmlDuration.Zero, XmlDuration.MinValue + XmlDuration.MaxValue);

      d1 = new XmlDuration(1, 2, 3, 4, 5, 6) + new XmlDuration(0, 1, 2, 3, 4, 5).Negate();
      Assert.AreEqual(1, d1.Years);
      Assert.AreEqual(1, d1.Months);
      Assert.AreEqual(1, d1.Days);
      Assert.AreEqual(1, d1.Hours);
      Assert.AreEqual(1, d1.Minutes);
      Assert.AreEqual(1, d1.Seconds);

      d1 = new XmlDuration(1, 2, 3).Negate() + new XmlDuration(2, 3, 4).Negate();
      Assert.AreEqual(3, d1.Years);
      Assert.AreEqual(5, d1.Months);
      Assert.AreEqual(7, d1.Days);

      d1 = new XmlDuration(1, 2, 3) - new XmlDuration(2, 0, 3);
      Assert.AreEqual(0, d1.Years);
      Assert.AreEqual(10, d1.Months);
      Assert.AreEqual(0, d1.Days);
      Assert.IsTrue(d1.IsNegative);

      TestHelpers.TestException<ArgumentException>(delegate { new XmlDuration(1, 2, 3).Subtract(new XmlDuration(0, 0, 5)); });
      TestHelpers.TestException<ArgumentException>(delegate { new XmlDuration(1, 2, 3).Subtract(new XmlDuration(2, 0, 1)); });

      d1 = new XmlDuration(1, 2, 3, 4, 5, 6);
      Assert.AreEqual(new XmlDuration(1, 2, 103, 4, 5, 6), d1.AddDays(100));
      Assert.AreEqual(new XmlDuration(1, 2, 7, 8, 5, 6), d1.AddHours(100));
      Assert.AreEqual(new XmlDuration(1, 2, 3, 4, 5, 6, 100), d1.AddMilliseconds(100));
      Assert.AreEqual(new XmlDuration(1, 2, 3, 5, 45, 6), d1.AddMinutes(100));
      Assert.AreEqual(new XmlDuration(9, 6, 3, 4, 5, 6), d1.AddMonths(100));
      Assert.AreEqual(new XmlDuration(1, 2, 3, 4, 6, 46), d1.AddSeconds(100));
      Assert.AreEqual(new XmlDuration(101, 2, 3, 4, 5, 6), d1.AddYears(100));

      Assert.AreEqual(new XmlDuration(1, 2, 3).Negate(), new XmlDuration(-1, -2, -3));
      Assert.AreEqual(new XmlDuration(1, 2, 3, 4, 5, 6).Negate(), new XmlDuration(-1, -2, -3, -4, -5, -6));
      Assert.AreEqual(new XmlDuration(0, 10, 3), new XmlDuration(1, -2, 3));
      TestHelpers.TestException<ArgumentException>(delegate { new XmlDuration(1, 2, -3); });
      TestHelpers.TestException<ArgumentException>(delegate { new XmlDuration(-1, -2, 3); });

      Assert.AreEqual(new TimeSpan(0, 0, 3), new XmlDuration(new TimeSpan(0, 0, 3)).ToTimeSpan());
      Assert.AreEqual(new TimeSpan(0, 0, 3).Negate(), new XmlDuration(new TimeSpan(0, 0, 3).Negate()).ToTimeSpan());
      Assert.AreEqual(new TimeSpan(0, 0, 3).Negate(), new XmlDuration(new TimeSpan(0, 0, 3)).Negate().ToTimeSpan());
      TestHelpers.TestException<InvalidOperationException>(delegate { new XmlDuration(1, 2, 3).ToTimeSpan(); });
    }
    #endregion
  }

} // namespace AdamMil.Utilities.Tests
