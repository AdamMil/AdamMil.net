/*
AdamMil.Mathematics is a library that provides some useful mathematics classes
for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2010 Adam Milazzo

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

using System;
using System.Collections.Generic;
using AdamMil.Mathematics.Random;

namespace AdamMil.Mathematics.Combinatorics
{

/// <summary>A class that performs permutations of lists.</summary>
public static class Permutations
{
  /// <summary>Randomly permutes the given list in-place, using a new random number generator.</summary>
  public static void RandomlyPermute<T>(this IList<T> list)
  {
    if(list == null) throw new ArgumentNullException();
    RandomlyPermute(list, 0, list.Count, RandomNumberGenerator.CreateFastest());
  }

  /// <summary>Randomly permutes the given list in-place, using the given random number generator.</summary>
  [CLSCompliant(false)]
  public static void RandomlyPermute<T>(this IList<T> list, RandomNumberGenerator random)
  {
    if(list == null) throw new ArgumentNullException();
    RandomlyPermute(list, 0, list.Count, random);
  }

  /// <summary>Randomly permutes the given list in-place, using the given random number generator.</summary>
  public static void RandomlyPermute<T>(this IList<T> list, System.Random random)
  {
    if(list == null) throw new ArgumentNullException();
    RandomlyPermute(list, 0, list.Count, random);
  }

  /// <summary>Randomly permutes the given portion of the list in-place, using a new random number generator.</summary>
  public static void RandomlyPermute<T>(this IList<T> list, int start, int count)
  {
    RandomlyPermute(list, start, count, RandomNumberGenerator.CreateFastest());
  }

  /// <summary>Randomly permutes the given portion of the list in-place, using the given random number generator.</summary>
  public static void RandomlyPermute<T>(this IList<T> list, int start, int count, System.Random random)
  {
    if(list == null) throw new ArgumentNullException();
    for(int end=start+count; start < end; start++) Swap(list, start, random.Next(start, end));
  }

  /// <summary>Randomly permutes the given portion of the list in-place, using the given random number generator.</summary>
  [CLSCompliant(false)]
  public static void RandomlyPermute<T>(this IList<T> list, int start, int count, RandomNumberGenerator random)
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