using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace AdamMil.Tests
{

public static class CollectionHelpers
{
  public static void ArrayEquals<T>(T[] a, params T[] b)
  {
    if(a == null && b == null) return;
    if(a == null || b == null) throw new ArgumentNullException();
    Assert.AreEqual(a.Length, b.Length);
    for(int i=0; i<a.Length; i++)
    {
      Assert.AreEqual(b[i], a[i]);
    }
  }

  public static T[] ToArray<T>(ICollection<T> collection)
  {
    if(collection == null) throw new ArgumentNullException();
    T[] array = new T[collection.Count];
    collection.CopyTo(array, 0);
    return array;
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