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
}

} // namespace AdamMil.Utilities.Tests
