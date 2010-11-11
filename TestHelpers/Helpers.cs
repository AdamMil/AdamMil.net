using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace AdamMil.Tests
{

public static class TestHelpers
{
  public static void AssertArrayEquals<T>(T[] expected, params T[] actual)
  {
    if(expected == null && actual == null) return;
    if(expected == null || actual == null) throw new ArgumentNullException();
    Assert.AreEqual(expected.Length, actual.Length);
    for(int i=0; i<expected.Length; i++)
    {
      Assert.AreEqual(actual[i], expected[i]);
    }
  }

  public static void AssertPropertyEquals(string propertyName, object expected, object actual)
  {
    object expectedValue = GetPropertyValue(expected, propertyName), actualValue = GetPropertyValue(actual, propertyName);
    Assert.AreEqual(expectedValue, actualValue, "Expected property " + propertyName + " to be equal for " +
                    expected + " and " + actual);
  }

  static object GetPropertyValue(object obj, string propertyName)
  {
    Type type = obj.GetType();

    PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    if(property != null) return property.GetValue(obj, null);

    FieldInfo field = type.GetField(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    if(field != null) return field.GetValue(obj);

    throw new ArgumentException("Expected " + obj.ToString() + " to have a property or field called \"" + propertyName + "\".");
  }

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

  public static void TestException<T>(Action block) where T : Exception
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