using NUnit.Framework;

namespace AdamMil.Utilities.Tests
{

[TestFixture]
public class PathTests
{
  [Test]
  public void T01_AppendToFileName()
  {
    Assert.AreEqual("C:/foo-2.txt", PathUtility.AppendToFileName("C:/foo.txt", "-2"));
    Assert.AreEqual("foo-2.txt", PathUtility.AppendToFileName("foo.txt", "-2"));
    Assert.AreEqual("/foo.bar-2.txt", PathUtility.AppendToFileName("/foo.bar.txt", "-2"));
  }
}

} // namespace AdamMil.Utilities.Tests
