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
    if(list == null) throw new ArgumentNullException();
    RandomlyPermute(list, 0, list.Count, new System.Random());
  }

  /// <summary>Randomly permutes the given list in-place, using the given random number generator.</summary>
  public static void RandomlyPermute<T>(IList<T> list, System.Random random)
  {
    if(list == null) throw new ArgumentNullException();
    RandomlyPermute(list, 0, list.Count, random);
  }

  /// <summary>Randomly permutes the given portion of the list in-place, using a new random number generator.</summary>
  public static void RandomlyPermute<T>(IList<T> list, int start, int count)
  {
    RandomlyPermute(list, start, count, new System.Random());
  }

  /// <summary>Randomly permutes the given portion of the list in-place, using the given random number generator.</summary>
  public static void RandomlyPermute<T>(IList<T> list, int start, int count, System.Random random)
  {
    if(list == null) throw new ArgumentNullException();
    for(int end=start+count; start < end; start++) Swap(list, start, random.Next(start, end));
  }

  static void Swap<T>(IList<T> list, int i, int j)
  {
    T t = list[i];
    list[i] = list[j];
    list[j] = t;
  }
}

/// <summary>Provides methods to calculate combinations and factorials.</summary>
public static class Combinations
{
  /// <summary>Calculates the binomial coefficient, which is the number of ways to choose k items from n possibilities.</summary>
  public static long CountCombinations(int nChosen, int nPossibilities)
  {
    if(nChosen < 1 || nPossibilities < 1 || nChosen > nPossibilities) throw new ArgumentOutOfRangeException();

    // this is the binomial coefficient, which is the number of ways to choose k items from n possibilities and is
    // equal to n! / ((n-k)!k!)

    long count = nPossibilities;
    while(--nChosen != 0) count *= --nPossibilities;
    return count;
  }

  /// <summary>Calculates the factorial of the given number, which is also the number of ways that a number of items
  /// may be permuted.
  /// </summary>
  public static long Factorial(int n)
  {
    if(n < 0) throw new ArgumentOutOfRangeException();
    if(n == 0) return 1;

    long factorial = n;
    if(n > 1)
    {
      while(--n != 1) checked { factorial *= n; }
    }
    return factorial;
  }
}

} // namespace AdamMil.Mathematics.Combinatorics