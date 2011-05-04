/*
AdamMil.Utilities is a library providing generally useful utilities for
.NET development.

http://www.adammil.net/
Copyright (C) 2010-2011 Adam Milazzo

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

namespace AdamMil.Utilities
{

#region DelegateComparer
/// <summary>Implements a comparer that uses a <see cref="Comparison{T}"/> delegate to compare objects.</summary>
public sealed class DelegateComparer<T> : IComparer<T>
{
  /// <summary>Initializes a new <see cref="ReversedComparer{T}"/> wrapping the given <see cref="Comparison{T}"/>.</summary>
  public DelegateComparer(Comparison<T> comparer)
  {
    if(comparer == null) throw new ArgumentNullException();
    cmp = comparer;
  }

  /// <summary>Compares two items using the <see cref="Comparison{T}"/> delegate passed to the constructor.</summary>
  public int Compare(T a, T b)
  {
    return cmp(a, b);
  }

  readonly Comparison<T> cmp;
}
#endregion

#region ReversedComparer
/// <summary>Implements a comparer that wraps another comparer and returns the opposite comparison.</summary>
public sealed class ReversedComparer<T> : IComparer<T>
{
  /// <summary>Initializes a new <see cref="ReversedComparer{T}"/> wrapping the given comparer.</summary>
  public ReversedComparer(IComparer<T> comparer)
  {
    if(comparer == null) throw new ArgumentNullException();
    cmp = comparer;
  }

  /// <summary>Compares the two items, returning the opposite of the comparison given by the comparer with which this
  /// object was initialized.
  /// </summary>
  public int Compare(T a, T b)
  {
    // NOTE: we can't use something like -cmp.Compare(a, b) because if it returned int.MinValue, then it could not be negated
    return cmp.Compare(b, a);
  }

  readonly IComparer<T> cmp;
}
#endregion


} // namespace AdamMil.Utilities
