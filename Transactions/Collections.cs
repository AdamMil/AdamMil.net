/*
AdamMil.Transactions is a library for the .NET framework that simplifies the
creation of transactional software.

http://www.adammil.net/
Copyright (C) 2011 Adam Milazzo

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

namespace AdamMil.Transactions
{

#region TransactionalArray
/// <summary>Implements an array whose members are transactional variables. The array's length cannot be changed after it is
/// created, so the array does not support methods that add or remove members.
/// </summary>
/// <remarks>When iterating over the array using an enumerator, be sure to dispose the enumerator when you're done with it, as
/// the enumerator holds open a transaction that is disposed when the enumerator is.
/// </remarks>
public sealed class TransactionalArray<T> : IList<T>
{
  /// <summary>Initializes a new <see cref="TransactionalArray{T}"/> with the given number of elements.</summary>
  public TransactionalArray(int length) : this(length, null) { }
  /// <summary>Initializes a new <see cref="TransactionalArray{T}"/> with the given number of elements, and using the given
  /// <see cref="IComparer{T}"/> to compare elements.
  /// </summary>
  public TransactionalArray(int length, IComparer<T> comparer)
  {
    if(length < 0) throw new ArgumentOutOfRangeException();
    array = new TransactionalVariable<T>[length];
    for(int i=0; i<array.Length; i++) array[i] = new TransactionalVariable<T>();

    this.comparer = comparer == null ? Comparer<T>.Default :  comparer;
  }

  /// <summary>Initializes a new <see cref="TransactionalArray{T}"/> with the given set of items.</summary>
  public TransactionalArray(IEnumerable<T> initialItems) : this(initialItems, null) { }
  /// <summary>Initializes a new <see cref="TransactionalArray{T}"/> with the given set of items, and using the given
  /// <see cref="IComparer{T}"/> to compare elements.
  /// </summary>
  public TransactionalArray(IEnumerable<T> initialItems, IComparer<T> comparer)
  {
    if(initialItems == null) throw new ArgumentNullException();
    ICollection<T> collection = initialItems as ICollection<T>;
    if(collection == null) collection = new List<T>(initialItems);

    array = new TransactionalVariable<T>[collection.Count];
    int index = 0;
    foreach(T item in initialItems) array[index] = new TransactionalVariable<T>(item);
    if(index != array.Length) throw new ArgumentException();

    this.comparer = comparer == null ? Comparer<T>.Default :  comparer;
  }

  #region IList<T> Members
  /// <inheritdoc/>
  public T this[int index]
  {
    get
    {
      using(STMTransaction transaction = STMTransaction.Create(true))
      {
        T value = array[index].Read();
        transaction.Commit();
        return value;
      }
    }
    set
    {
      using(STMTransaction transaction = STMTransaction.Create(true))
      {
        array[index].Set(value);
        transaction.Commit();
      }
    }
  }

  /// <inheritdoc/>
  public int IndexOf(T item)
  {
    using(STMTransaction transaction = STMTransaction.Create())
    {
      for(int i=0; i<array.Length; i++)
      {
        if(comparer.Compare(item, array[i].Read()) == 0) return i;
      }
      transaction.Commit();
    }
    return -1;
  }

  void IList<T>.Insert(int index, T item)
  {
    throw new NotSupportedException();
  }

  void IList<T>.RemoveAt(int index)
  {
    throw new NotSupportedException();
  }
  #endregion

  #region ICollection<T> Members
  /// <inheritdoc/>
  public int Count
  {
    get { return array.Length; }
  }

  /// <inheritdoc/>
  public bool IsReadOnly
  {
    get { return false; }
  }

  /// <inheritdoc/>
  public bool Contains(T item)
  {
    return IndexOf(item) != -1;
  }

  /// <inheritdoc/>
  public void CopyTo(T[] array, int arrayIndex)
  {
    using(STMTransaction transaction = STMTransaction.Create())
    {
      for(int i=0; i<this.array.Length; i++) array[arrayIndex+i] = this.array[i].Read();
      transaction.Commit();
    }
  }

  void ICollection<T>.Add(T item)
  {
    throw new NotSupportedException();
  }

  void ICollection<T>.Clear()
  {
    throw new NotSupportedException();
  }

  bool ICollection<T>.Remove(T item)
  {
    throw new NotSupportedException();
  }
  #endregion

  #region IEnumerable<T> Members
  /// <inheritdoc/>
  public IEnumerator<T> GetEnumerator()
  {
    return new Enumerator(array);
  }
  #endregion

  #region IEnumerable Members
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
  #endregion

  #region Enumerator
  sealed class Enumerator : IEnumerator<T>
  {
    public Enumerator(TransactionalVariable<T>[] array)
    {
      this.array  = array;
      transaction = STMTransaction.Create();
      index       = -1;
    }

    public T Current
    {
      get
      {
        if((uint)index >= (uint)array.Length) throw new InvalidOperationException();
        return current;
      }
    }

    public void Dispose()
    {
      if(transaction != null)
      {
        transaction.Commit();
        transaction.Dispose();
        transaction = null;
      }
    }

    public bool MoveNext()
    {
      if(index == array.Length) return false;
      if(++index == array.Length)
      {
        current = default(T);
        return false;
      }
      else
      {
        current = array[index].Read();
        return true;
      }
    }

    public void Reset()
    {
      if(transaction == null) throw new InvalidOperationException();
      index = -1;
    }

    object System.Collections.IEnumerator.Current
    {
      get { return Current; }
    }

    readonly TransactionalVariable<T>[] array;
    STMTransaction transaction;
    T current;
    int index;
  }
  #endregion

  readonly TransactionalVariable<T>[] array;
  readonly IComparer<T> comparer;
}
#endregion

} // namespace AdamMil.Transactions
