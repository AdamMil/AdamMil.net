using System;
using NUnit.Framework;

namespace AdamMil.Collections.Tests
{

static class Helpers
{
  public delegate void CodeBlock();

  public static void AssertEqual<T>(T[] a, params T[] b)
  {
    Assert.AreEqual(a.Length, b.Length);
    for(int i=0; i<a.Length; i++)
    {
      Assert.AreEqual(b[i], a[i]);
    }
  }

  public static void TestException<T>(CodeBlock block) where T : Exception
  {
    bool threw = false;
    try { block(); }
    catch(Exception ex)
    {
      if(!typeof(T).IsAssignableFrom(ex.GetType())) throw;
      threw = true;
    }
    Assert.IsTrue(threw);
  }
}

} // namespace AdamMil.Collections.Tests