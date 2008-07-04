using System;
using System.Collections.Generic;

namespace AdamMil.Collections
{

#region ReadOnlyCollectionWrapper
/// <summary>Represents a read-only wrapper around a list.</summary>
public sealed class ReadOnlyCollectionWrapper<T> : IReadOnlyCollection<T>, ICollection<T>
{
  /// <summary>Initializes a new <see cref="ReadOnlyCollectionWrapper{T}"/> around the given collection.</summary>
  /// <param name="collection"></param>
  public ReadOnlyCollectionWrapper(ICollection<T> collection)
  {
    if(collection == null) throw new ArgumentNullException();
    this.collection = collection;
  }

  /// <summary>Gets the number of items in the collection.</summary>
  public int Count
  {
    get { return collection.Count; }
  }

  /// <summary>Returns true if the collection contains the given item.</summary>
  public bool Contains(T item)
  {
    return collection.Contains(item);
  }

  /// <summary>Copies the items from the collection into the array starting at the given index.</summary>
  public void CopyTo(T[] array, int arrayIndex)
  {
    collection.CopyTo(array, arrayIndex);
  }

  /// <summary>Returns an enumerator that enumerates the items in the collection.</summary>
  public IEnumerator<T> GetEnumerator()
  {
    return collection.GetEnumerator();
  }

  /// <summary>Copies all of the items from the collection to a new array and returns it.</summary>
  public T[] ToArray()
  {
    T[] array = new T[collection.Count];
    collection.CopyTo(array, 0);
    return array;
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
  /// <summary>Initializes a new <see cref="ReadOnlyListWrapper{T}"/> around the given list.</summary>
  public ReadOnlyListWrapper(IList<T> list)
  {
    if(list == null) throw new ArgumentNullException();
    this.list = list;
  }

  /// <summary>Gets the item from the list at the given index.</summary>
  public T this[int index]
  {
    get { return list[index]; }
  }

  /// <summary>Gets the number of items in the list.</summary>
  public int Count
  {
    get { return list.Count; }
  }

  /// <summary>Returns true if the list contains the given item.</summary>
  public bool Contains(T item)
  {
    return list.Contains(item);
  }

  /// <summary>Copies the items from the list to the given array, starting at the given index.</summary>
  public void CopyTo(T[] array, int arrayIndex)
  {
    list.CopyTo(array, arrayIndex);
  }

  /// <summary>Returns an enumerator that enumerates the items in the list.</summary>
  public IEnumerator<T> GetEnumerator()
  {
    return list.GetEnumerator();
  }

  /// <summary>Returns the index of the first item in the list equal to the given item, or -1 if the item could not
  /// be found.
  /// </summary>
  public int IndexOf(T item)
  {
    return list.IndexOf(item);
  }

  /// <summary>Copies all of the items from the collection to a new array and returns it.</summary>
  public T[] ToArray()
  {
    T[] array = new T[list.Count];
    list.CopyTo(array, 0);
    return array;
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
    return -cmp.Compare(a, b);
  }

  readonly IComparer<T> cmp;
}
#endregion

} // namespace AdamMil.Collections