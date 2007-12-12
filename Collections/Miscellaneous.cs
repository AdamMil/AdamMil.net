using System;
using System.Collections.Generic;

namespace AdamMil.Collections
{

#region ReversedComparer
public sealed class ReversedComparer<T> : IComparer<T>
{
  public ReversedComparer(IComparer<T> comparer)
  {
    if(comparer == null) throw new ArgumentNullException();
    cmp = comparer;
  }

  public int Compare(T a, T b)
  {
    return -cmp.Compare(a, b);
  }

  readonly IComparer<T> cmp;
}
#endregion

} // namespace AdamMil.Collections