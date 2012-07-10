using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

namespace AdamMil.Tests
{

public static class TestHelpers
{
  public static void AssertArrayEquals<T>(T[] actual, params T[] expected)
  {
    if(actual == null && expected == null) return;
    if(actual == null || expected == null) throw new ArgumentNullException();
    Assert.AreEqual(actual.Length, expected.Length);
    for(int i=0; i<actual.Length; i++) Assert.AreEqual(expected[i], actual[i]);
  }

  public static void AssertEqual(object expected, object actual)
  {
    if(expected == null && actual == null) return;
    else if(expected is IList && actual is IList) AssertListEquals((IList)expected, (IList)actual);
    else if(expected is DateTime && actual is DateTime)
    {
      DateTime expectedTime = (DateTime)expected, actualTime = (DateTime)actual;
      Assert.AreEqual(expectedTime, actualTime);
      Assert.AreEqual(expectedTime.Kind, actualTime.Kind);
    }
    else
    {
      Assert.AreEqual(expected, actual);
    }
  }

  public static void AssertListEquals(IList expected, IList actual)
  {
    AssertListEquals(expected, actual, true);
  }

  public static void AssertListEquals(IList expected, IList actual, bool requireSameType)
  {
    if(expected == null && actual == null) return;
    Assert.IsFalse(expected == null || actual == null);
    if(requireSameType) Assert.AreSame(expected.GetType(), actual.GetType());
    Assert.AreEqual(expected.Count, actual.Count);
    for(int i=0; i<expected.Count; i++) Assert.AreEqual(expected[i], actual[i]);
   }

  public static void AssertPropertyEquals(string propertyName, object expected, object actual)
  {
    object expectedValue = GetPropertyValue(expected, propertyName), actualValue = GetPropertyValue(actual, propertyName);
    Assert.AreEqual(expectedValue, actualValue, "Expected property " + propertyName + " to be equal for " +
                    expected + " and " + actual);
  }

  public static void RunInAnotherThread(Action action)
  {
    if(action == null) throw new ArgumentNullException();
    Exception exception = null;
    Thread thread = new Thread((ThreadStart)delegate
    {
      try { action(); }
      catch(Exception ex) { exception = ex; }
    });
    thread.Start();
    thread.Join();
    if(exception != null) throw exception;
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
    TestException<T>(null, block);
  }

  public static void TestException<T>(string substring, Action block) where T : Exception
  {
    bool threw = false;
    try
    {
      block();
    }
    catch(Exception ex)
    {
      if(!typeof(T).IsAssignableFrom(ex.GetType()) || substring != null && !ex.Message.Contains(substring)) throw;
      threw = true;
    }
    Assert.IsTrue(threw, "A " + typeof(T).Name + " exception was expected" + (substring == null ? null : " (with substring \"" +
                         substring + "\")") + ", but did not occur.");
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
}

} // namespace AdamMil.Tests