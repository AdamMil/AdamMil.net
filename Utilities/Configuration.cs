/*
AdamMil.Collections is a library that provides useful collection classes for
the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2016 Adam Milazzo

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
using System.Configuration;

namespace AdamMil.Configuration
{

#region CustomElementCollection
/// <summary>Implements a base class for custom <see cref="ConfigurationElement"/> objects.</summary>
public abstract class CustomElementCollection<T> : ConfigurationElementCollection, IEnumerable<T> where T : ConfigurationElement
{
  /// <summary>Initializes a new <see cref="CustomElementCollection{T}"/> with the default key comparer.</summary>
  protected CustomElementCollection() : base() { }

  /// <summary>Initializes a new <see cref="CustomElementCollection{T}"/> with the given key comparer.</summary>
  protected CustomElementCollection(System.Collections.IComparer keyComparer) : base(keyComparer) { }

  /// <inheritdoc/>
  public override ConfigurationElementCollectionType CollectionType
  {
    get { return ConfigurationElementCollectionType.AddRemoveClearMap; }
  }

  /// <summary>Gets or sets the <see cref="ConfigurationElement"/> at the given index.</summary>
  public T this[int index]
  {
    get { return (T)BaseGet(index); }
    set
    {
      if(value == null) throw new ArgumentNullException();
      BaseRemoveAt(index);
      BaseAdd(index, value);
    }
  }

  /// <summary>Adds a <see cref="ConfigurationElement"/> to the collection.</summary>
  public void Add(T element)
  {
    if(element == null) throw new ArgumentNullException();
    BaseAdd(element);
  }

  /// <summary>Clears the collection.</summary>
  public void Clear()
  {
    BaseClear();
  }

  /// <summary>Inserts a <see cref="ConfigurationElement"/> into the collection at the given index.</summary>
  public void Insert(int index, T element)
  {
    if(element == null) throw new ArgumentNullException();
    BaseAdd(index, element);
  }

  /// <summary>Removes a <see cref="ConfigurationElement"/> from the collection.</summary>
  public void Remove(T element)
  {
    if(element == null) return;
    BaseRemove(GetElementKey(element));
  }

  /// <summary>Removes the configuration element at the given index.</summary>
  public void RemoveAt(int index)
  {
    BaseRemoveAt(index);
  }

  #region IEnumerable<T> Members
  /// <summary>Returns an enumerator that iterates through the configuration elements in the collection.</summary>
  public new IEnumerator<T> GetEnumerator()
  {
    foreach(T element in (ConfigurationElementCollection)this) yield return element;
  }
  #endregion

  /// <summary>Creates and returns a new instance of the configuration element.</summary>
  protected abstract T CreateElement();
  /// <summary>Returns a value that acts as the key for the given configuration element.</summary>
  protected abstract object GetElementKey(T element);

  /// <inheritdoc/>
  protected sealed override ConfigurationElement CreateNewElement()
  {
    return (T)CreateElement();
  }

  /// <inheritdoc/>
  protected sealed override object GetElementKey(ConfigurationElement element)
  {
    return GetElementKey((T)element);
  }
}
#endregion

} // namespace AdamMil.Configuration