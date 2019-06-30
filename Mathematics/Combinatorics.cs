/*
AdamMil.Mathematics is a library that provides some useful mathematics classes
for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2019 Adam Milazzo

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
    for(int end=start+count-1; start <= end; start++) Swap(list, start, random.Next(start, end));
  }

  /// <summary>Randomly permutes the given portion of the list in-place, using the given random number generator.</summary>
  public static void RandomlyPermute<T>(this IList<T> list, int start, int count, RandomNumberGenerator random)
  {
    if(list == null) throw new ArgumentNullException();
    for(int end=start+count-1; start <= end; start++) Swap(list, start, random.Next(start, end));
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
  public static Integer CountCombinations(int nChosen, int nPossibilities)
  {
    if(nChosen < 1 || nPossibilities < 1 || nChosen > nPossibilities) throw new ArgumentOutOfRangeException();

    // this is the binomial coefficient, which is the number of ways to choose k items from n possibilities and is
    // equal to n! / ((n-k)!k!)
    Integer count = (uint)nPossibilities;
    for(int n = nChosen-1; n != 0; n--) count.UnsafeMultiply((uint)--nPossibilities);
    for(; nChosen > 1; nChosen--) count.UnsafeDivide((uint)nChosen);
    return count;
  }

  /// <summary>Calculates the number of ways that a number of items may be permuted, which is equal to its factorial.</summary>
  public static Integer CountPermutations(int n)
  {
    if(n < 1) throw new ArgumentOutOfRangeException();
    return Integer.Factorial(n);
  }
}

} // namespace AdamMil.Mathematics.Combinatorics
