using System;
using System.Linq;
using System.Text;
using AdamMil.Mathematics.ErrorCorrection;
using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Mathematics.Tests
{
  [TestFixture]
  public class ErrorCorrectionCodes
  {
    [Test]
    public void T01_ReedSolomon()
    {
      byte[] orig = Encoding.UTF8.GetBytes("Hello, world! Velit omnis consequatur nobis. Cum omnis ipsam rerum ut velit minus. Bye!");
      ReedSolomon rs = new ReedSolomon(8);

      // first test the basics
      byte[] encoded = rs.Encode(orig);
      TestHelpers.AssertArrayEquals(encoded.Skip(rs.EccLength).ToArray(), orig); // test that the encoded data ends with the original data
      Assert.IsTrue(rs.Check(encoded));
      TestHelpers.AssertArrayEquals(rs.Decode(encoded), orig);

      // we should be able to correct up to four errors in the encoded data
      encoded[7] = 77;
      encoded[22] = 44;
      encoded[62] = 11;
      encoded[81] = 0;
      Assert.IsFalse(rs.Check(encoded));
      TestHelpers.AssertArrayEquals(rs.Decode(encoded), orig);

      // but not five
      encoded[2] = 1;
      Assert.IsNull(rs.Decode(encoded));

      // however, if we specify the positions, we should be able to fix more
      TestHelpers.AssertArrayEquals(rs.Decode(encoded, new[] { 2, 7, 22, 62, 81 }, true), orig);
      encoded[12] = 2;
      TestHelpers.AssertArrayEquals(rs.Decode(encoded, new[] { 2, 7, 12, 22, 62 }, false), orig);

      // in fact should be able to fix at least eight errors
      encoded[23] = 3;
      encoded[40] = 4;
      TestHelpers.AssertArrayEquals(rs.Decode(encoded, new[] { 2, 7, 12, 22, 23, 40, 62, 81 }, true), orig);
      TestHelpers.AssertArrayEquals(rs.Decode(encoded, new[] { 2, 7, 12, 22, 23, 40, 62, 81 }, false), orig);

      // but not nine errors
      encoded[70] = 5;
      Assert.IsNull(rs.Decode(encoded, new[] { 2, 7, 12, 22, 23, 40, 62, 70, 81 }, true));

      // now try the methods that take array indexes
      byte[] orig2 = new byte[orig.Length/2];
      int encodedLength = orig2.Length + rs.EccLength;
      Array.Copy(orig, orig2, orig2.Length);
      rs.Encode(orig, 0, orig.Length/2, encoded, 10);

      // first, try the basic decoding
      Assert.IsTrue(rs.Check(encoded, 10, orig2.Length+rs.EccLength));
      TestHelpers.AssertArrayEquals(rs.Decode(encoded, 10, encodedLength), orig2);
      byte[] decoded = new byte[orig2.Length + 30];
      Assert.AreEqual(orig2.Length, rs.Decode(encoded, 10, encodedLength, decoded, 20));
      TestHelpers.AssertArrayEquals(decoded.Skip(20).Take(orig2.Length).ToArray(), orig2);

      // now fix four errors with positions unspecified
      encoded[11] = 77;
      encoded[17] = 44;
      encoded[23] = 11;
      encoded[34] = 0;
      Assert.AreEqual(orig2.Length, rs.Decode(encoded, 10, encodedLength, decoded, 20));
      TestHelpers.AssertArrayEquals(decoded.Skip(20).Take(orig2.Length).ToArray(), orig2);

      // now fix eight errors with positions specified
      encoded[13] = 1;
      encoded[19] = 2;
      encoded[27] = 3;
      encoded[31] = 4;
      Assert.AreEqual(-1, rs.Decode(encoded, 10, encodedLength, decoded, 20)); // check that it gives a failure result without positions
      Assert.AreEqual(orig2.Length, rs.Decode(encoded, 10, encodedLength, decoded, 20, new[] { 1, 3, 7, 9, 13, 17, 21, 24 }));
      TestHelpers.AssertArrayEquals(decoded.Skip(20).Take(orig2.Length).ToArray(), orig2);
    }
  }
}