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

  public static void TestEnumerator<T>(ICollection<T> collection)
  {
    collection.Clear();
    collection.Add(default(T));

    IEnumerator<T> e = collection.GetEnumerator();
    TestHelpers.TestException<InvalidOperationException>(delegate() { T x = e.Current; }); // test that the enumerator will throw on BOF
    Assert.IsTrue(e.MoveNext()); // try moving the enumerator
    Assert.AreEqual(default(T), e.Current); // ensure that we can access Current now
    // test that the enumerator throws when the collection is modified
    collection.Add(default(T));
    TestHelpers.TestException<InvalidOperationException>(delegate() { e.MoveNext(); });
    // ensure that we can still access e.Current
    Assert.AreEqual(default(T), e.Current);
    // ensure that Reset() throws after the collection is modified
    TestHelpers.TestException<InvalidOperationException>(delegate() { e.Reset(); });

    collection.Clear();
    for(int i=0; i<10; i++) collection.Add(default(T));
    e = collection.GetEnumerator();
    for(int i=0; i<10; i++)
    {
      Assert.IsTrue(e.MoveNext()); // move the enumerator to the end
      Assert.AreEqual(default(T), e.Current); // check each element
    }
    Assert.IsFalse(e.MoveNext()); // test that the enumerator detects the end
    Assert.IsFalse(e.MoveNext()); // test that the enumerator detects the end again (possibly another code path)

    TestHelpers.TestException<InvalidOperationException>(delegate() { T x = e.Current; }); // test that the enumerator will throw on EOF
    e.Reset();
    TestHelpers.TestException<InvalidOperationException>(delegate() { T x = e.Current; }); // test that Current will throw after Reset()
    Assert.IsTrue(e.MoveNext()); // assert that we can move again after Reset()
    Assert.AreEqual(default(T), e.Current); // and that we can access e.Current

    collection.Clear();
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
    Assert.That(threw, "A "+typeof(T).Name+" exception was expected, but did not occur.");
  }
}

} // namespace AdamMil.Tests