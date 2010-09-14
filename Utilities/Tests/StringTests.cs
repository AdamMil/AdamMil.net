using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Utilities.Tests
{

[TestFixture]
public class StringTests
{
	[Test]
	public void T01_ToAndFromBinary()
	{
		byte[] data = new byte[] { 8, 10, 250, 0, 255, 20, 48, 58, 64, 128, 170, 30, 180 };
		Assert.AreEqual("080AFA00FF14303A4080AA1EB4", StringUtility.ToHex(data).ToUpperInvariant());
		TestHelpers.AssertArrayEquals(data, StringUtility.FromHex(StringUtility.ToHex(data)));
	}

  [Test]
  public void T02_TestRemoveAndReplace()
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
