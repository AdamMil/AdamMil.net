using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;
using AdamMil.Utilities;
using NUnit.Framework;

namespace AdamMil.Tests
{

public static class TestHelpers
{
  public static void AssertArrayEquals<T>(T[] actual, params T[] expected)
  {
    if(actual == null && expected == null) return;
    Assert.IsFalse(actual == null || expected == null);
    Assert.AreEqual(actual.Length, expected.Length);
    for(int i=0; i<actual.Length; i++) Assert.AreEqual(expected[i], actual[i]);
  }

  public static void AssertEqual(DateTime expected, DateTime actual)
  {
    DateTime expectedTime = (DateTime)expected, actualTime = (DateTime)actual;
    Assert.AreEqual(expectedTime, actualTime);
    Assert.AreEqual(expectedTime.Kind, actualTime.Kind);
  }

  public static void AssertEqual(IList expected, IList actual)
  {
    AssertListEquals(expected, actual);
  }

  public static void AssertEqual(object expected, object actual)
  {
    if(expected == null && actual == null) return;
    else if(expected is IList && actual is IList) AssertListEquals((IList)expected, (IList)actual);
    else if(expected is DateTime && actual is DateTime) AssertEqual((DateTime)expected, (DateTime)actual);
    else if(!AssertDictionaryObjectEquals(expected, actual)) Assert.AreEqual(expected, actual);
  }

  public static void AssertDictionaryEquals<K,V>(IDictionary<K,V> expected, IDictionary<K,V> actual)
  {
    if(expected == null && actual == null) return;
    Assert.IsFalse(expected == null || actual == null);
    Assert.AreEqual(expected.Count, actual.Count);
    foreach(KeyValuePair<K,V> pair in expected)
    {
      V value;
      Assert.IsTrue(actual.TryGetValue(pair.Key, out value));
      AssertEqual(pair.Value, value);
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

  public static void AssertXmlEquals(string expectedXml, XmlNode actualNode)
  {
    AssertXmlEquals(GetXmlReader(expectedXml), GetXmlReader(actualNode));
  }

  public static void AssertXmlEquals(string expectedXml, string actualXml)
  {
    AssertXmlEquals(GetXmlReader(expectedXml), GetXmlReader(actualXml));
  }

  public static void AssertXmlEquals(string expectedXml, Stream actualXml)
  {
    AssertXmlEquals(GetXmlReader(expectedXml), GetXmlReader(actualXml));
  }

  public static void AssertXmlEquals(string expectedXml, XmlReader actual)
  {
    AssertXmlEquals(GetXmlReader(expectedXml), actual);
  }

  public static void AssertXmlEquals(Stream expectedXml, XmlNode actualNode)
  {
    AssertXmlEquals(GetXmlReader(expectedXml), GetXmlReader(actualNode));
  }

  public static void AssertXmlEquals(Stream expectedXml, string actualXml)
  {
    AssertXmlEquals(GetXmlReader(expectedXml), GetXmlReader(actualXml));
  }

  public static void AssertXmlEquals(Stream expectedXml, Stream actualXml)
  {
    AssertXmlEquals(GetXmlReader(expectedXml), GetXmlReader(actualXml));
  }

  public static void AssertXmlEquals(Stream expectedXml, XmlReader actual)
  {
    AssertXmlEquals(GetXmlReader(expectedXml), actual);
  }

  public static void AssertXmlEquals(XmlNode expectedNode, XmlNode actualNode)
  {
    AssertXmlEquals(GetXmlReader(expectedNode), GetXmlReader(actualNode));
  }

  public static void AssertXmlEquals(XmlNode expectedNode, string actualXml)
  {
    AssertXmlEquals(GetXmlReader(expectedNode), GetXmlReader(actualXml));
  }

  public static void AssertXmlEquals(XmlNode expectedNode, Stream actualXml)
  {
    AssertXmlEquals(GetXmlReader(expectedNode), GetXmlReader(actualXml));
  }

  public static void AssertXmlEquals(XmlNode expectedNode, XmlReader actual)
  {
    AssertXmlEquals(GetXmlReader(expectedNode), actual);
  }

  public static void AssertXmlEquals(XmlReader expected, XmlNode actualNode)
  {
    AssertXmlEquals(expected, GetXmlReader(actualNode));
  }

  public static void AssertXmlEquals(XmlReader expected, string actualXml)
  {
    AssertXmlEquals(expected, GetXmlReader(actualXml));
  }

  public static void AssertXmlEquals(XmlReader expected, Stream actualXml)
  {
    AssertXmlEquals(expected, GetXmlReader(actualXml));
  }

  public static void AssertXmlEquals(XmlReader expected, XmlReader actual)
  {
    XmlQualifiedName expectedClosing = null, actualClosing = null;
    while(true)
    {
      bool expectMore;
      do expectMore = expected.Read(); while(expected.NodeType == XmlNodeType.XmlDeclaration);
      if(expectedClosing != null)
      {
        Assert.AreEqual(XmlNodeType.EndElement, expected.NodeType);
        Assert.AreEqual(expectedClosing.Name, expected.LocalName);
        Assert.AreEqual(expectedClosing.Namespace, expected.NamespaceURI);
        expectedClosing = null;
        expectMore = expected.Read();
      }

      bool more;
      do more = actual.Read(); while(actual.NodeType == XmlNodeType.XmlDeclaration);
      Assert.AreEqual(expectMore, more);
      if(actualClosing != null)
      {
        Assert.AreEqual(XmlNodeType.EndElement, actual.NodeType);
        Assert.AreEqual(actualClosing.Name, actual.LocalName);
        Assert.AreEqual(actualClosing.Namespace, actual.NamespaceURI);
        actualClosing = null;
        Assert.AreEqual(expectMore, actual.Read());
      }

      if(!expectMore) break;

      XmlNodeType nodeType = expected.NodeType;
      Assert.AreEqual(nodeType, actual.NodeType);
      if(nodeType == XmlNodeType.Element || nodeType == XmlNodeType.EndElement)
      {
        Assert.AreEqual(expected.LocalName, actual.LocalName);
        Assert.AreEqual(expected.NamespaceURI, actual.NamespaceURI);
        if(nodeType == XmlNodeType.Element)
        {
          AssertDictionaryEquals(ReadAttributes(expected), ReadAttributes(actual));
          if(expected.IsEmptyElement != actual.IsEmptyElement)
          {
            XmlQualifiedName qname = new XmlQualifiedName(expected.LocalName, expected.NamespaceURI);
            if(expected.IsEmptyElement) actualClosing = qname;
            else expectedClosing = qname;
          }
        }
      }
      Assert.AreEqual(expected.Value, actual.Value);
    }
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

  static bool AssertDictionaryObjectEquals(object expected, object actual)
  {
    if(expected == null && actual == null) return true;
    if(expected == null || actual == null) return false;
    Type eType = expected.GetType(), aType = actual.GetType();
    if(!eType.IsGenericType || !aType.IsGenericType) return false;
    Type eTypeDef = eType.GetGenericTypeDefinition(), aTypeDef = aType.GetGenericTypeDefinition();
    if(eTypeDef != typeof(Dictionary<,>) || aTypeDef != typeof(Dictionary<,>)) return false;
    Type[] eArgs = eType.GetGenericArguments(), aArgs = eType.GetGenericArguments();
    if(eArgs[0] != aArgs[0] || eArgs[1] != aArgs[1]) return false;
    MethodInfo method = typeof(TestHelpers).GetMethod("AssertDictionaryEquals");
    method.MakeGenericMethod(eArgs).Invoke(null, new object[] { expected, actual });
    return true;
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

  static XmlReader GetXmlReader(Stream stream)
  {
    return XmlReader.Create(stream, GetXmlReaderSettings());
  }

  static XmlReader GetXmlReader(string xml)
  {
    return XmlReader.Create(new StringReader(xml), GetXmlReaderSettings());
  }

  static XmlReader GetXmlReader(XmlNode node)
  {
    return new XmlNodeReader(node);
  }

  static XmlReaderSettings GetXmlReaderSettings()
  {
    return new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = true };
  }

  static Dictionary<XmlQualifiedName, string> ReadAttributes(XmlReader reader)
  {
    return ReadAttributes(reader, true);
  }

  static Dictionary<XmlQualifiedName, string> ReadAttributes(XmlReader reader, bool ignoreNamespaces)
  {
    Dictionary<XmlQualifiedName, string> dict = new Dictionary<XmlQualifiedName, string>(reader.AttributeCount);
    if(reader.HasAttributes)
    {
      reader.MoveToFirstAttribute();
      do
      {
        if(!ignoreNamespaces || (reader.LocalName != "xmlns" && !reader.Name.StartsWith("xmlns:")))
        {
          string value = reader.Value;
          // normalise xsi:type attributes
          if(reader.NamespaceURI == "http://www.w3.org/2001/XMLSchema-instance" && reader.LocalName == "type")
          {
            value = reader.ParseQualifiedName(value).ToString();
          }
          dict[new XmlQualifiedName(reader.LocalName, reader.NamespaceURI)] = value;
        }
      } while(reader.MoveToNextAttribute());
      reader.MoveToElement(); // move back to the element
    }
    return dict;
  }
}

} // namespace AdamMil.Tests