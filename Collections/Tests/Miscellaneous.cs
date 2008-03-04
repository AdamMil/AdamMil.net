using System;
using System.Collections.Generic;
using NUnit.Framework;
using AdamMil.Tests;
using AdamMil.Collections;

namespace AdamMil.Collections.Tests
{

[TestFixture]
public class MiscellaneousTests
{
  [Test]
  public void Test()
  {
    TestHelpers.TestException<ArgumentNullException>(delegate() { new ReversedComparer<int>(null); });
  }
}

} // namespace AdamMil.Collections.Tests