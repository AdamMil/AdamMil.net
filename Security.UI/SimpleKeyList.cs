/*
AdamMil.Security.UI is a .NET library providing common user interface widgets
for OpenPGP-based software.
Copyright (C) 2008-2011 Adam Milazzo (http://www.adammil.net/)

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
using System.Windows.Forms;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

/// <summary>A simple list of keys. You can derive a new class from this one to change the way the items are displayed.</summary>
public class SimpleKeyList : KeyListBase
{
  /// <summary>Initializes a new <see cref="SimpleKeyList"/>.</summary>
  public SimpleKeyList()
  {
    InitializeControl();
  }

  /// <summary>Adds a key to the list.</summary>
  public void AddKey(Key key)
  {
    if(key == null) throw new ArgumentNullException();

    PGPListViewItem item = CreateKeyItem(key);
    if(item != null)
    {
      SetFont(item, GetItemStatus(key));
      Items.Add(item);
    }
  }

  /// <include file="documentation.xml" path="/UI/ListBase/CreateKeyItem/*"/>
  protected virtual PGPListViewItem CreateKeyItem(Key key)
  {
    PrimaryKey primaryKey = key as PrimaryKey;
    PGPListViewItem item = primaryKey != null ?
      (PGPListViewItem)new PrimaryKeyItem(primaryKey) : new SubkeyItem((Subkey)key);

    item.Text = primaryKey != null ? "primary" : "subkey";
    item.SubItems.Add(key.ShortKeyId);
    item.SubItems.Add(key.KeyType);
    item.SubItems.Add(key.Length.ToString());
    item.SubItems.Add(key.CreationTime.ToShortDateString());
    item.SubItems.Add(key.ExpirationTime.HasValue ? key.ExpirationTime.Value.ToShortDateString() : "n/a");

    char[] capChars = new char[4];
    int caps = 0;
    if((key.Capabilities & KeyCapabilities.Authenticate) != 0) capChars[caps++] = 'A';
    if((key.Capabilities & KeyCapabilities.Certify) != 0) capChars[caps++] = 'C';
    if((key.Capabilities & KeyCapabilities.Encrypt) != 0) capChars[caps++] = 'E';
    if((key.Capabilities & KeyCapabilities.Sign) != 0) capChars[caps++] = 'S';
    item.SubItems.Add(caps == 0 ? "none" : new string(capChars, 0, caps));

    return item;
  }

  /// <include file="documentation.xml" path="/UI/ListBase/RecreateItems/*"/>
  protected override void RecreateItems()
  {
    List<Key> keys = new List<Key>(Items.Count);
    foreach(ListViewItem item in Items)
    {
      PrimaryKeyItem primaryItem = item as PrimaryKeyItem;
      keys.Add(primaryItem != null ? (Key)primaryItem.PublicKey : ((SubkeyItem)item).Subkey);
    }

    Items.Clear();
    foreach(Key key in keys) AddKey(key);
  }

  void InitializeControl()
  {
    ColumnHeader keyHeader, keyIdHeader, algorithmHeader, sizeHeader, createdHeader, expireHeader, capsHeader;

    keyHeader = new ColumnHeader();
    keyHeader.Text = "Key";
    keyHeader.Width = 60;

    keyIdHeader = new ColumnHeader();
    keyIdHeader.Text = "Key ID";
    keyIdHeader.Width = 80;

    algorithmHeader = new ColumnHeader();
    algorithmHeader.Text = "Algorithm";
    algorithmHeader.Width = 65;

    sizeHeader = new ColumnHeader();
    sizeHeader.Text = "Size";
    sizeHeader.Width = 45;

    createdHeader = new ColumnHeader();
    createdHeader.Text = "Created";
    createdHeader.Width = 75;

    expireHeader = new ColumnHeader();
    expireHeader.Text = "Expiration";
    expireHeader.Width = 75;

    capsHeader = new ColumnHeader();
    capsHeader.Text = "Capabilities";
    capsHeader.Width = 70;

    Columns.AddRange(new ColumnHeader[] { keyHeader, keyIdHeader, algorithmHeader, sizeHeader, createdHeader,
                                          expireHeader, capsHeader });
  }
}

} // namespace AdamMil.Security.UI