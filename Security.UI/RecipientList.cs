/*
AdamMil.Security.UI is a .NET library providing common user interface widgets
for OpenPGP-based software.
Copyright (C) 2008 Adam Milazzo (http://www.adammil.net/)

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

/// <summary>This list shows the usable encryption recipients on a keyring.</summary>
public class RecipientList : KeyListBase
{
  /// <summary>Initializes a new <see cref="RecipientList"/>.</summary>
  public RecipientList()
  {
    InitializeControl();
  }

  /// <summary>Filters the list of recipients by showing only those with a user ID or key fingerprint that contains at
  /// least one of the given keyword strings. If the keywords array is null or contains no keywords,
  /// all keys are shown.
  /// </summary>
  public void FilterItems(string[] keywords)
  {
    if(keys == null) throw new InvalidOperationException("ShowKeyring has not been called.");
    
    if(keywords == null || keywords.Length == 0 || keywords.Length == 1 && string.IsNullOrEmpty(keywords[0]))
    {
      AddKeyItems(keys);
      return;
    }

    List<PrimaryKey> keysToShow = new List<PrimaryKey>();
    foreach(PrimaryKey key in keys)
    {
      bool addKey = false;

      foreach(string keyword in keywords)
      {
        string trimmed = keyword.Trim();

        foreach(UserId id in key.UserIds)
        {
          if(id.Name.IndexOf(trimmed, StringComparison.CurrentCultureIgnoreCase) != -1)
          {
            addKey = true;
            goto done;
          }
        }

        if(key.EffectiveId.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) != -1)
        {
          addKey = true;
          goto done;
        }
      }

      done:
      if(addKey) keysToShow.Add(key);
    }

    AddKeyItems(keysToShow.ToArray());
  }

  /// <summary>Gets the recipients that are selected in the list.</summary>
  public PrimaryKey[] GetSelectedRecipients()
  {
    List<PrimaryKey> keys = new List<PrimaryKey>(SelectedItems.Count);
    foreach(ListViewItem item in SelectedItems)
    {
      PrimaryKeyItem primaryItem = item as PrimaryKeyItem;
      if(primaryItem != null) keys.Add(primaryItem.PublicKey);
    }
    return keys.ToArray();
  }

  /// <summary>Displays the given recipients.</summary>
  public void ShowKeys(PrimaryKey[] keys)
  {
    if(keys == null) throw new ArgumentNullException();
    AddKeyItems(keys);
    this.keys = keys;
  }

  /// <summary>Displays the recipients in the given keyring.</summary>
  public void ShowKeyring(PGPSystem pgp, Keyring keyring)
  {
    if(pgp == null) throw new ArgumentNullException();
    ShowKeys(pgp.GetKeys(keyring, ListOptions.RetrieveSecretKeys | ListOptions.IgnoreUnusableKeys));
  }

  /// <summary>Creates the <see cref="PrimaryKeyItem"/> used to display the given key.</summary>
  protected virtual PrimaryKeyItem CreatePrimaryKeyItem(PrimaryKey key)
  {
    if(key == null) throw new ArgumentNullException();
    return new PrimaryKeyItem(key, PGPUI.GetKeyName(key));
  }

  /// <include file="documentation.xml" path="/UI/Common/OnSizeChanged/*"/>
  protected override void OnSizeChanged(EventArgs e)
  {
    base.OnSizeChanged(e);
    Columns[0].Width = Width - 20; // make the column scale with the form, and allow 20 pixels for the scrollbar
  }

  /// <include file="documentation.xml" path="/UI/ListBase/RecreateItems/*"/>
  protected override void RecreateItems()
  {
    List<PrimaryKey> keys = new List<PrimaryKey>(Items.Count);
    foreach(ListViewItem item in Items)
    {
      PrimaryKeyItem primaryItem = item as PrimaryKeyItem;
      if(primaryItem != null) keys.Add(primaryItem.PublicKey);
    }

    AddKeyItems(keys.ToArray());
  }

  void InitializeControl()
  {
    base.SmallImageList = null;

    ColumnHeader header = new ColumnHeader();
    header.Text  = "Recipient";
    header.Width = 300;
    Columns.Add(header);
  }

  /// <summary>Clears the current list items, and displays the given keys in the list.</summary>
  void AddKeyItems(PrimaryKey[] keys)
  {
    Items.Clear();

    foreach(PrimaryKey key in keys)
    {
      if(key.HasCapabilities(KeyCapabilities.Encrypt))
      {
        PrimaryKeyItem item = CreatePrimaryKeyItem(key);
        if(item != null)
        {
          SetFont(item, GetItemStatus(key));
          Items.Add(item);
        }
      }
    }
  }

  PrimaryKey[] keys;
}

} // namespace AdamMil.Security.UI