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

namespace AdamMil.Mathematics
{
  /// <summary>Implements various number-theoretical functions.</summary>
  public static class NumberTheory
  {
    /// <summary>Returns the greatest common factor of two integers, as a nonnegative value.</summary>
    public static int GreatestCommonFactor(int a, int b)
    {
      return (int)GreatestCommonFactor(Abs(a), Abs(b));
    }

    /// <summary>Returns the greatest common factor of two integers.</summary>
    [CLSCompliant(false)]
    public static uint GreatestCommonFactor(uint a, uint b)
    {
      while(true)
      {
        if(b == 0) return a; // use the Euclidean GCD algorithm, which is much faster than the binary GCD algorithm for native integers
        a %= b;
        if(a == 0) return b;
        b %= a;
      }
    }

    /// <summary>Returns the greatest common factor of two integers, as a nonnegative value.</summary>
    public static long GreatestCommonFactor(long a, long b)
    {
      return (long)GreatestCommonFactor(Abs(a), Abs(b));
    }

    /// <summary>Returns the greatest common factor of two integers.</summary>
    [CLSCompliant(false)]
    public static unsafe ulong GreatestCommonFactor(ulong a, ulong b)
    {
      // if we're on a 32-bit architecture and both numbers fit in 32 bits, switch to the 32-bit function
      if(sizeof(IntPtr) <= 4 && (a|b) <= uint.MaxValue) return GreatestCommonFactor((uint)a, (uint)b);
      bool aSmall = false, bSmall = sizeof(IntPtr) <= 4 && b <= uint.MaxValue;
      while(true)
      {
        if(b == 0) return a; // use the Euclidean GCD algorithm, which is much faster than the binary GCD algorithm for native integers
        a %= b;
        if(sizeof(IntPtr) <= 4) // if we're on a 32-bit architecture, switch to 32-bit operations as soon as we can
        {
          aSmall = a <= uint.MaxValue;
          if(aSmall & bSmall) return GreatestCommonFactor((uint)b, (uint)a);
        }

        if(a == 0) return b;
        b %= a;
        if(sizeof(IntPtr) <= 4)
        {
          bSmall = b <= uint.MaxValue;
          if(aSmall & bSmall) return GreatestCommonFactor((uint)a, (uint)b);
        }
      }
    }

    /// <summary>Returns the greatest common factor of two integers, as a nonnegative value.</summary>
    public static Integer GreatestCommonFactor(Integer a, Integer b)
    {
      return Integer.GreatestCommonFactor(a, b);
    }

    /// <summary>Returns the least common multiple of two integers, as a nonnegative value.</summary>
    public static long LeastCommonMultiple(int a, int b)
    {
      return (long)LeastCommonMultiple(Abs(a), Abs(b));
    }

    /// <summary>Returns the least common multiple of two integers.</summary>
    [CLSCompliant(false)]
    public static ulong LeastCommonMultiple(uint a, uint b)
    {
      if((a|b) == 0) return 0;
      return a / GreatestCommonFactor(a, b) * (ulong)b;
    }

    /// <summary>Returns the least common multiple of two integers, as a nonnegative value.</summary>
    public static Integer LeastCommonMultiple(long a, long b)
    {
      return LeastCommonMultiple(Abs(a), Abs(b));
    }

    /// <summary>Returns the least common multiple of two integers.</summary>
    [CLSCompliant(false)]
    public static Integer LeastCommonMultiple(ulong a, ulong b)
    {
      if((a|b) == 0) return 0;
      return a / GreatestCommonFactor(a, b) * new Integer(b);
    }

    /// <summary>Returns the least common multiple of two integers, as a nonnegative value.</summary>
    public static Integer LeastCommonMultiple(Integer a, Integer b)
    {
      return Integer.LeastCommonMultiple(a, b);
    }

    static uint Abs(int value)
    {
      return (uint)(value >= 0 ? value : -value);
    }

    static ulong Abs(long value)
    {
      return (ulong)(value >= 0 ? value : -value);
    }
  }
}
