using System;
using System.Collections.Generic;

namespace AdamMil.Mathematics.Combinatorics
{

/// <summary>A class that performs permutations of lists.</summary>
public static class Permutations
{
  /// <summary>Randomly permutes the given list in-place, using a new random number generator.</summary>
  public static void RandomlyPermute<T>(IList<T> list)
  {
    RandomlyPermute(list, new Random());
  }

  /// <summary>Randomly permutes the given list in-place, using the given random number generator.</summary>
  public static void RandomlyPermute<T>(IList<T> list, Random random)
  {
    for(int i=0; i<list.Count; i++) Swap(list, i, random.Next(i, list.Count));
  }

  static void Swap<T>(IList<T> list, int i, int j)
  {
    T t = list[i];
    list[i] = list[j];
    list[j] = t;
  }
}

} // namespace AdamMil.Mathematics.Combinatorics