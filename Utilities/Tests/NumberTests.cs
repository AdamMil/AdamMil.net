using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Utilities.Tests
{

[TestFixture]
public class NumberTests
{
  [Test]
  public void T01_TestNumberExtensions()
  {
    Assert.IsTrue(5d.IsNumber());
    Assert.IsTrue((-5d).IsNumber());
    Assert.IsTrue(0d.IsNumber());
    Assert.IsFalse(double.NaN.IsNumber());
    Assert.IsFalse(double.PositiveInfinity.IsNumber());
    Assert.IsFalse(double.NegativeInfinity.IsNumber());
  }

}

} // namespace AdamMil.Utilities.Tests
