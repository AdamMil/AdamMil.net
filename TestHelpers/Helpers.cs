using System;
using NUnit.Framework;

namespace AdamMil.Tests
{

public static class CollectionHelpers
{
  public static void ArrayEquals<T>(T[] a, params T[] b)
  {
    Assert.AreEqual(a.Length, b.Length);
    for(int i=0; i<a.Length; i++)
    {
      Assert.AreEqual(b[i], a[i]);
    }
  }
}

public static class TestHelpers
{
  public delegate void CodeBlock();

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

} // namespace AdamMil.Tests