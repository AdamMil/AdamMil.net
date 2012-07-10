using System;
using System.Linq;
using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Utilities.Tests
{

[TestFixture]
public class LinqTests
{
  [Test]
  public void T01_Selection()
  {
    int[] array = new int[1000];
    for(int i=0; i<array.Length; i++) array[i] = i/2;

    Random rand = new Random(); // shuffle the array
    for(int i=0; i<array.Length-1; i++) Utility.Swap(ref array[i], ref array[i+rand.Next(array.Length-i)]);

    TestHelpers.AssertArrayEquals(array.TakeLeast(10).Order().ToArray(), 0, 0, 1, 1, 2, 2, 3, 3, 4, 4);
    TestHelpers.AssertArrayEquals(array.TakeGreatest(10).OrderDescending().ToArray(), 499, 499, 498, 498, 497, 497, 496, 496, 495, 495);
  }
}

} // namespace AdamMil.Utilities.Tests
