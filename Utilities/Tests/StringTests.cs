using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Utilities.Tests
{

[TestFixture]
public class StringTests
{
  [Test]
  public void T01_TestRemoveAndReplace()
  {
    const string TestString = "Hello, world!";
    Assert.AreEqual(TestString, TestString.Replace((char[])null, "foo"));
    Assert.AreEqual(TestString, TestString.Replace(new char[] { 'a', 'b', 'c' }, "foo"));
    Assert.AreEqual("Hellfoo, wfoofoold!", TestString.Replace(new char[] { 'a', 'o', 'c', 'r' }, "foo"));
    Assert.AreEqual("Hellfoo, wfoorld!", TestString.Replace(new char[] { 'o' }, "foo"));
    Assert.AreEqual("Hell, wrld", TestString.Remove('a', 'o', 'b', '!'));
  }

}

} // namespace AdamMil.Utilities.Tests
