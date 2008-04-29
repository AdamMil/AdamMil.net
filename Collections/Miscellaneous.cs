using System;
using System.Collections.Generic;

namespace AdamMil.Collections
{

#region ReadOnlyCollectionWrapper
/// <summary>Represents a read-only wrapper around a list.</summary>
public sealed class ReadOnlyCollectionWrapper<T> : IReadOnlyCollection<T>, ICollection<T>
{
  public ReadOnlyCollectionWrapper(ICollection<T> collection)
  {
    if(collection == null) throw new ArgumentNullException();
    this.collection = collection;
  }

  public int Count
  {
    get { return collection.Count; }
  }

  public bool Contains(T item)
  {
    return collection.Contains(item);
  }

  public void CopyTo(T[] array, int arrayIndex)
  {
    collection.CopyTo(array, arrayIndex);
  }

  public IEnumerator<T> GetEnumerator()
  {
    return collection.GetEnumerator();
  }

  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return ((System.Collections.IEnumerable)collection).GetEnumerator();
  }

  readonly ICollection<T> collection;

  #region ICollection<T>
  int ICollection<T>.Count
  {
    get { return collection.Count; }
  }

  bool ICollection<T>.IsReadOnly
  {
    get { return true; }
  }

  void ICollection<T>.Add(T item)
  {
    ThrowReadOnlyException();
  }

  void ICollection<T>.Clear()
  {
    ThrowReadOnlyException();
  }

  bool ICollection<T>.Contains(T item)
  {
    return collection.Contains(item);
  }

  void ICollection<T>.CopyTo(T[] array, int arrayIndex)
  {
    collection.CopyTo(array, arrayIndex);
  }

  bool ICollection<T>.Remove(T item)
  {
    ThrowReadOnlyException();
    return false;
  }

  static void ThrowReadOnlyException()
  {
    throw new NotSupportedException("This collection is read-only.");
  }
  #endregion
}
#endregion

#region ReadOnlyListWrapper
/// <summary>Represents a read-only wrapper around a list.</summary>
public sealed class ReadOnlyListWrapper<T> : IReadOnlyList<T>, IList<T>
{
  public ReadOnlyListWrapper(IList<T> list)
  {
    if(list == null) throw new ArgumentNullException();
    this.list = list;
  }

  public T this[int index]
  {
    get { return list[index]; }
  }

  public int Count
  {
    get { return list.Count; }
  }

  public bool Contains(T item)
  {
    return list.Contains(item);
  }

  public void CopyTo(T[] array, int arrayIndex)
  {
    list.CopyTo(array, arrayIndex);
  }

  public IEnumerator<T> GetEnumerator()
  {
    return list.GetEnumerator();
  }

  public int IndexOf(T item)
  {
    return list.IndexOf(item);
  }

  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return ((System.Collections.IEnumerable)list).GetEnumerator();
  }

  readonly IList<T> list;

  #region ICollection<T>
  int ICollection<T>.Count
  {
    get { return list.Count; }
  }

  bool ICollection<T>.IsReadOnly
  {
    get { return true; }
  }

  void ICollection<T>.Add(T item)
  {
    ThrowReadOnlyException();
  }

  void ICollection<T>.Clear()
  {
    ThrowReadOnlyException();
  }

  bool ICollection<T>.Contains(T item)
  {
    return list.Contains(item);
  }

  void ICollection<T>.CopyTo(T[] array, int arrayIndex)
  {
    list.CopyTo(array, arrayIndex);
  }

  bool ICollection<T>.Remove(T item)
  {
    ThrowReadOnlyException();
    return false;
  }

  static void ThrowReadOnlyException()
  {
    throw new NotSupportedException("This collection is read-only.");
  }
  #endregion

  #region IList<T>
  T IList<T>.this[int index]
  {
    get { return list[index]; }
    set { ThrowReadOnlyException(); }
  }

  int IList<T>.IndexOf(T item)
  {
    return list.IndexOf(item);
  }

  void IList<T>.Insert(int index, T item)
  {
    ThrowReadOnlyException();
  }

  void IList<T>.RemoveAt(int index)
  {
    ThrowReadOnlyException();
  }
  #endregion
}
#endregion

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