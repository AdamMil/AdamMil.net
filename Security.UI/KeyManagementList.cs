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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

/// <summary>A complex list that displays and manages a user's keys.</summary>
public class KeyManagementList : KeyListBase
{
  /// <summary>Initializes a new <see cref="KeyManagementList"/>.</summary>
  public KeyManagementList()
  {
    InitializeControl();
  }

  /// <summary>Gets or sets whether the display the <see cref="PrimaryKey.DesignatedRevokers">designated revokers</see>
  /// of a key. The default is false.
  /// </summary>
  [Category("Appearance")]
  [DefaultValue(false)]
  [Description("Determines whether the designated revokers of a key will be displayed.")]
  public bool DisplayRevokers
  {
    get { return displayRevokers; }
    set
    {
      if(value != DisplayRevokers)
      {
        displayRevokers = value;
        RecreateItems();
      }
    }
  }

  /// <summary>Gets or sets whether the display the <see cref="PrimaryKey.Subkeys">subkeys</see> of a key. The default
  /// is false.
  /// </summary>
  [Category("Appearance")]
  [DefaultValue(false)]
  [Description("Determines whether the subkeys of a key will be displayed.")]
  public bool DisplaySubkeys
  {
    get { return displaySubkeys; }
    set
    {
      if(value != DisplaySubkeys)
      {
        displaySubkeys = value;
        RecreateItems();
      }
    }
  }

  /// <summary>Gets or sets whether the display the user IDs and user attributes of a key. The default is true.</summary>
  [Category("Appearance")]
  [DefaultValue(true)]
  [Description("Determines whether the user IDs and user attributes of a key will be displayed.")]
  public bool DisplayUserIds
  {
    get { return displayUserIds; }
    set 
    {
      if(value != DisplayUserIds)
      {
        displayUserIds = value;
        RecreateItems();
      }
    }
  }

  /// <summary>Gets or sets the <see cref="PGPSystem"/> that will be used to manage the keys displayed. This can be set
  /// to null if the list will only be used to display keys, and not to manage them.
  /// </summary>
  [Browsable(false)]
  public PGPSystem PGP
  {
    get { return pgp; }
    set { pgp = value; }
  }

  /// <summary>Adds a <see cref="PrimaryKey"/> to the list of items displayed.</summary>
  public void AddKey(PrimaryKey key)
  {
    AddKey(key, false);
  }

  /// <summary>Adds a <see cref="PrimaryKey"/> to the list of items displayed, optionally expanding it to show any
  /// subitems it may have.
  /// </summary>
  public void AddKey(PrimaryKey key, bool expanded)
  {
    if(key == null) throw new ArgumentNullException();

    PrimaryKeyItem primaryItem = CreatePrimaryKeyItem(key);
    if(primaryItem == null) return;

    List<ListViewItem> items = new List<ListViewItem>();
    SetFont(primaryItem, GetItemStatus(key));
    items.Add(primaryItem);

    if(DisplayUserIds)
    {
      List<UserAttribute> userIds = new List<UserAttribute>(key.UserIds.Count +
                                                            key.Attributes.Count);
      foreach(UserId id in key.UserIds)
      {
        if(id != key.PrimaryUserId) userIds.Add(id); // we don't show the primary user ID as a separate item
      }                                                         // it's represented by the primary key itself
      userIds.AddRange(key.Attributes);

      foreach(UserAttribute attr in userIds)
      {
        ListViewItem item = CreateAttributeItem(attr);
        if(item != null)
        {
          item.IndentCount = 1;
          SetFont(item, GetItemStatus(attr));
          items.Add(item);
        }
      }
    }

    if(DisplaySubkeys)
    {
      foreach(Subkey subkey in key.Subkeys)
      {
        ListViewItem item = CreateSubkeyItem(subkey);
        if(item != null)
        {
          item.IndentCount = 1;
          SetFont(item, GetItemStatus(subkey));
          items.Add(item);
        }
      }
    }

    if(DisplayRevokers)
    {
      foreach(string revoker in key.DesignatedRevokers)
      {
        ListViewItem item = CreateDesignatedRevokerItem(revoker, key);
        if(item != null)
        {
          item.IndentCount = 1;
          SetFont(item, key.Revoked ? ItemStatus.Revoked
                                               : key.Expired ? ItemStatus.Expired : ItemStatus.Normal);
          items.Add(item);
        }
      }
    }

    Items.Add(primaryItem);
    itemDict[key.EffectiveId] = primaryItem;

    if(items.Count > 1)
    {
      primaryItem.ImageIndex = PlusImage;
      primaryItem.relatedItems = new ListViewItem[items.Count-1];
      items.CopyTo(1, primaryItem.relatedItems, 0, items.Count-1);

      if(expanded) ExpandItem(primaryItem);
    }
  }

  /// <summary>Filters the list of keys by showing only those with a user ID or key fingerprint that contains at least
  /// one of the given keyword strings. If the keywords array is null or contains no keywords, all keys are shown.
  /// </summary>
  public void FilterItems(string[] keywords)
  {
    bool showAll = keywords == null || keywords.Length == 0 ||
                   keywords.Length == 1 && string.IsNullOrEmpty(keywords[0]);
    foreach(PrimaryKeyItem item in itemDict.Values)
    {
      bool shouldShow = showAll;
      if(!shouldShow)
      {
        foreach(string keyword in keywords)
        {
          if(PGPUI.KeyMatchesKeyword(item.PublicKey, keyword))
          {
            shouldShow = true;
            break;
          }
        }
      }

      if(shouldShow)
      {
        if(Items[item.PublicKey.EffectiveId] == null) // if the item is not currently displayed, show it
        {
          item.expanded   = false;
          item.ImageIndex = PlusImage;
          Items.Add(item);
        }
      }
      else
      {
        if(Items[item.PublicKey.EffectiveId] != null) // if the item is currently displayed, hide it
        {
          CollapseItem(item);
          Items.RemoveAt(item.Index);
        }
      }
    }
  }

  /// <summary>Clears the list and displays the keys from the given keyring, or the default keyring if it is null. The
  /// <see cref="PGP"/> property must have been set before this method can be called.
  /// </summary>
  public void ShowKeyring(Keyring keyring)
  {
    AssertPGPSystem();

    PrimaryKey[] keys = PGP.GetKeys(keyring, ListOptions.RetrieveAttributes | ListOptions.RetrieveSecretKeys);

    Items.Clear();
    itemDict.Clear();
    foreach(PrimaryKey key in keys) AddKey(key);

    this.keyring = keyring;
  }

  #region ItemComparerBase
  /// <summary>Provides a base class for comparers that sort the items in a <see cref="KeyManagementList"/>.</summary>
  protected abstract class ItemComparerBase : System.Collections.IComparer, IComparer<PGPListViewItem>
  {
    /// <summary>Initializes a new <see cref="ItemComparerBase"/> with the list whose items are being compared.</summary>
    protected ItemComparerBase(KeyManagementList list)
    {
      if(list == null) throw new ArgumentNullException();
      this.list = list;
    }

    /// <summary>Compares the two list items given.</summary>
    public int Compare(PGPListViewItem a, PGPListViewItem b)
    {
      if(a == b) return 0;

      PrimaryKeyItem pa = List.GetPrimaryItem(a), pb = List.GetPrimaryItem(b);
      
      // if the two items don't belong to the same primary item, then sort them based on the primary items
      if(pa != pb) return Compare(pa, pb);

      // one may be primary and another not. in that case, the primary comes first
      if(pa == a) return -1; // a is primary
      else if(pb == b) return 1; // b is primary
      
      // the two items belong to the same primary, so sort them within the primary. put user IDs first, then user
      // attributes, subkeys, and finally designated revokers. sort user IDs by name, attributes by creation date, and
      // subkeys by key ID, and revokers by fingerprint
      AttributeItem aai = a as AttributeItem, bai = b as AttributeItem;
      if(aai != null)
      {
        if(bai == null) return -1;
        
        UserId aid = aai.Attribute as UserId, bid = bai.Attribute as UserId;
        if(aid != null)
        {
          return bid == null ? -1 : string.Compare(aid.Name, bid.Name, StringComparison.CurrentCultureIgnoreCase);
        }
        else if(bid != null) return 1;

        return DateTime.Compare(aai.Attribute.CreationTime, bai.Attribute.CreationTime);
      }
      else if(bai != null) return 1;

      SubkeyItem ask = a as SubkeyItem, bsk = b as SubkeyItem;
      if(ask != null)
      {
        return bsk == null ? -1 : string.Compare(ask.Subkey.ShortKeyId, bsk.Subkey.ShortKeyId,
                                                 StringComparison.OrdinalIgnoreCase);
      }
      else if(bsk != null) return 1;

      DesignatedRevokerItem adr = a as DesignatedRevokerItem, bdr = b as DesignatedRevokerItem;
      if(adr != null)
      {
        return bdr == null ? -1 : string.Compare(adr.Fingerprint, bdr.Fingerprint, StringComparison.OrdinalIgnoreCase);
      }
      else if(bdr != null) return 1;

      // we don't know what kinds of items these are, so just compare them by hash code
      return a.GetHashCode() - b.GetHashCode();
    }

    /// <summary>Gets the <see cref="KeyManagementList"/> passed to the constructor.</summary>
    protected KeyManagementList List
    {
      get { return list; }
    }

    /// <summary>Compares two <see cref="PrimaryKeyItem"/> objects.</summary>
    protected abstract int Compare(PrimaryKeyItem a, PrimaryKeyItem b);

    /// <summary>Compares two list items.</summary>
    int System.Collections.IComparer.Compare(object a, object b)
    {
      if(a == b) return 0;

      PGPListViewItem ai = a as PGPListViewItem, bi = b as PGPListViewItem;
      return ai == null ? 1 : bi == null ? -1 : Compare(ai, bi);
    }

    readonly KeyManagementList list;
  }
  #endregion

  #region ItemCompareByName
  /// <summary>An item comparer that sorts primary keys by name.</summary>
  protected class ItemComparerByName : ItemComparerBase
  {
    /// <summary>Initializes a new <see cref="ItemComparerByName"/> with the list whose items are being compared.</summary>
    public ItemComparerByName(KeyManagementList list) : base(list) { }

    /// <summary>Compares two <see cref="PrimaryKeyItem"/> objects by the names of their primary user IDs.</summary>
    protected override int Compare(PrimaryKeyItem a, PrimaryKeyItem b)
    {
      return string.Compare(a.PublicKey.PrimaryUserId.Name, b.PublicKey.PrimaryUserId.Name,
                            StringComparison.CurrentCultureIgnoreCase);
    }
  }
  #endregion

  /// <include file="documentation.xml" path="/UI/ListBase/ActivateItem/*"/>
  protected virtual void ActivateItem(ListViewItem item)
  {
    PrimaryKeyItem primaryItem = GetPrimaryItem(item);
    AttributeItem attrItem = item as AttributeItem;
    if(attrItem != null)
    {
      if(attrItem.Attribute is UserImage) // activating a user image displays it
      {
        ShowPhotoId((UserImage)attrItem.Attribute);
        return;
      }
      else if(primaryItem != null && primaryItem.PublicKey.HasSecretKey) // activating any other attribute goes to the
      {                                                                  // management form, if we have the secret key
        ManageUserIds(GetPrimaryItem(item));
        return;
      }
    }

    if(primaryItem != null)
    {
      if(item is SubkeyItem && primaryItem.PublicKey.HasSecretKey) // activating a subkey goes to the management form
      {                                                            // if the user owns the key
        ManageSubkeys(primaryItem);
      }
      else // otherwise, show the key properties
      {
        ShowKeyProperties(primaryItem);
      }
    }
  }

  /// <summary>Throws an exception if the <see cref="PGP"/> property hasn't been set.</summary>
  protected void AssertPGPSystem()
  {
    if(PGP == null) throw new InvalidOperationException("No PGP system has been set.");
  }

  /// <include file="documentation.xml" path="/UI/ListBase/CreateAttributeItem/*"/>
  protected override AttributeItem CreateAttributeItem(UserAttribute attr)
  {
    AttributeItem item = base.CreateAttributeItem(attr);
    item.SubItems.Add(string.Empty);
    item.SubItems.Add(string.Empty);
    item.SubItems.Add(PGPUI.GetTrustDescription(attr.CalculatedTrust));
    return item;
  }

  /// <include file="documentation.xml" path="/UI/ListBase/CreateContextMenu/*"/>
  protected virtual ContextMenuStrip CreateContextMenu()
  {
    PrimaryKey[] keys = GetSelectedKeys();
    PrimaryKey[] secretKeys = GetSecretKeys(false);
    int attributeCount = 0, keyCount = keys.Length, photoCount = 0, secretCount = 0;
    bool hasEnabled = false, hasDisabled = false, hasUsableSelectedKey = false, hasUsableSecretKey = false;
    bool canRevoke = keyCount == 1 && keys[0].HasSecretKey && !keys[0].Revoked;

    // count the number of photo IDs and secret keys, and determine whether any keys are disabled, enabled, or usable
    foreach(PrimaryKey key in keys)
    {
      foreach(UserAttribute attr in key.Attributes)
      {
        if(attr is UserImage) photoCount++;
      }

      if(key.Disabled) hasDisabled = true;
      else hasEnabled = true;

      if(key.Usable) hasUsableSelectedKey = true;
      if(key.HasSecretKey) secretCount++;
    }

    if(keyCount == 0 && PGP == null) return null;

    foreach(PrimaryKey secretKey in secretKeys)
    {
      if(secretKey.Usable)
      {
        hasUsableSecretKey = true;
        break;
      }
    }

    // if we can't revoke because we own the key, perhaps we can revoke because we own a designated revoker key
    if(keyCount == 1 && !canRevoke && !keys[0].Revoked)
    {
      foreach(string designatedRevoker in keys[0].DesignatedRevokers)
      {
        foreach(PrimaryKey secretKey in secretKeys)
        {
          if(string.Equals(secretKey.Fingerprint, designatedRevoker, StringComparison.Ordinal))
          {
            canRevoke = true;
            goto done;
          }
        }
      }
      done:;
    }

    // count the number of selected user attributes
    foreach(ListViewItem item in SelectedItems)
    {
      if(item is AttributeItem) attributeCount++;
    }

    ContextMenuStrip menu = new ContextMenuStrip();

    if(keyCount == 0) // if nothing is selected, display commands that operate on the entire list
    {
      menu.Items.Add(new ToolStripMenuItem("E&xport Keys...", null,
                                           delegate(object sender, EventArgs e) { ExportKeys(); }));
      menu.Items.Add(new ToolStripMenuItem("&Import Keys...", null,
                                           delegate(object sender, EventArgs e) { ImportKeys(); }));
      menu.Items.Add(new ToolStripSeparator());
      menu.Items.Add(new ToolStripMenuItem("I&mport Keys from Key Server...", null,
                                           delegate(object sender, EventArgs e) { ImportKeysFromKeyServer(); }));
    }
    else
    {
      if(PGP != null)
      {
        // importing and exporting keys
        menu.Items.Add(new ToolStripMenuItem("&Copy Public Keys to Clipboard", null,
                                             delegate(object sender, EventArgs e) { ExportPublicKeysToClipboard(); }));
        menu.Items.Add(new ToolStripMenuItem("E&xport Keys to File...", null,
                                             delegate(object sender, EventArgs e) { ExportKeysToFile(); }));
        menu.Items.Add(new ToolStripMenuItem("&Import Keys...", null,
                                             delegate(object sender, EventArgs e) { ImportKeys(); }));
        menu.Items.Add(new ToolStripSeparator());

        // key server operations
        menu.Items.Add(new ToolStripMenuItem("I&mport Keys from Key Server...", null,
                                             delegate(object sender, EventArgs e) { ImportKeysFromKeyServer(); }));
        menu.Items.Add(new ToolStripMenuItem("S&end Public Keys to Key Server...", null,
                                             delegate(object sender, EventArgs e) { SendKeysToKeyServer(); }));
        menu.Items.Add(new ToolStripMenuItem("Re&fresh Public Keys from Key Server...", null,
                                             delegate(object sender, EventArgs e) { RefreshKeysFromKeyServer(); }));
        menu.Items.Add(new ToolStripSeparator());

        // key signing
        menu.Items.Add(new ToolStripMenuItem("&Sign Keys...", null,
                                             delegate(object sender, EventArgs e) { SignKeys(); }));
        menu.Items[menu.Items.Count-1].Enabled = hasUsableSecretKey && hasUsableSelectedKey;
        menu.Items.Add(new ToolStripMenuItem("Set Owner &Trust...", null,
                                             delegate(object sender, EventArgs e) { SetOwnerTrust(); }));
        menu.Items.Add(new ToolStripSeparator());

        // key management
        menu.Items.Add(new ToolStripMenuItem("C&lean Keys", null,
                                             delegate(object sender, EventArgs e) { CleanKeys(); }));
        menu.Items.Add(new ToolStripMenuItem("&Delete Keys", null,
                                             delegate(object sender, EventArgs e) { DeleteKeys(); }));
        menu.Items.Add(new ToolStripMenuItem("&Generate Revocation Certificate...", null,
                                             delegate(object sender, EventArgs e) { GenerateRevocationCertificate(); }));
        menu.Items[menu.Items.Count-1].Enabled = canRevoke;
        menu.Items.Add(new ToolStripMenuItem("&Revoke Key...", null,
                                             delegate(object sender, EventArgs e) { RevokeKey(); }));
        menu.Items[menu.Items.Count-1].Enabled = canRevoke;
        menu.Items.Add(new ToolStripSeparator());
      }

      if(PGP != null)
      {
        menu.Items.Add(new ToolStripMenuItem("Change &Passphrase...", null,
                                             delegate(object sender, EventArgs e) { ChangePassphrase(); }));
        menu.Items[menu.Items.Count-1].Enabled = secretCount == 1;
        menu.Items.Add(new ToolStripMenuItem("&View Signatures...", null,
                                             delegate(object sender, EventArgs e) { ShowSignatures(); }));
        menu.Items[menu.Items.Count-1].Enabled = keyCount == 1;
      }

      menu.Items.Add(new ToolStripMenuItem("View P&hoto ID...", null,
                                           delegate(object sender, EventArgs e) { ShowPhotoId(); }));
      menu.Items[menu.Items.Count-1].Enabled = photoCount == 1;
      menu.Items.Add(new ToolStripMenuItem("View &Key Properties...", null,
                                           delegate(object sender, EventArgs e) { ShowKeyProperties(); }));
      menu.Items[menu.Items.Count-1].Enabled = keyCount == 1;

      if(PGP != null)
      {
        menu.Items.Add(new ToolStripSeparator());

        // advanced options
        ToolStripMenuItem advanced = new ToolStripMenuItem("&Advanced");
        advanced.DropDownItems.Add(new ToolStripMenuItem("Delete Secret &Portion of Keys", null,
                                                        delegate(object sender, EventArgs e) { DeleteSecretKeys(); }));
        advanced.DropDownItems[advanced.DropDownItems.Count-1].Enabled = secretCount != 0;
        advanced.DropDownItems.Add(new ToolStripMenuItem("E&xport Keys...", null,
                                                         delegate(object sender, EventArgs e) { ExportKeys(); }));
        advanced.DropDownItems.Add(new ToolStripMenuItem("&Disable Keys", null,
                                                         delegate(object sender, EventArgs e) { DisableKeys(); }));
        advanced.DropDownItems[advanced.DropDownItems.Count-1].Enabled = hasEnabled;
        advanced.DropDownItems.Add(new ToolStripMenuItem("&Enable Keys", null,
                                                         delegate(object sender, EventArgs e) { EnableKeys(); }));
        advanced.DropDownItems[advanced.DropDownItems.Count-1].Enabled = hasDisabled;
        advanced.DropDownItems.Add(new ToolStripMenuItem("&Minimize Keys", null,
                                                         delegate(object sender, EventArgs e) { MinimizeKeys(); }));
        advanced.DropDownItems.Add(new ToolStripMenuItem("Make this Key a Designated &Revoker...", null,
                                                   delegate(object sender, EventArgs e) { MakeDesignatedRevoker(); }));
        advanced.DropDownItems[advanced.DropDownItems.Count-1].Enabled = keyCount == 1 && hasUsableSecretKey;
        advanced.DropDownItems.Add(new ToolStripMenuItem("Manage &Subkeys...", null,
                                                         delegate(object sender, EventArgs e) { ManageSubkeys(); }));
        advanced.DropDownItems[advanced.DropDownItems.Count-1].Enabled = secretCount == 1;
        advanced.DropDownItems.Add(new ToolStripMenuItem("Manage &User IDs...", null,
                                                         delegate(object sender, EventArgs e) { ManageUserIds(); }));
        advanced.DropDownItems[advanced.DropDownItems.Count-1].Enabled = secretCount == 1;
        menu.Items.Add(advanced);
      }
    }

    return menu;
  }

  /// <include file="documentation.xml" path="/UI/ListBase/CreateDesignatedRevokerItem/*"/>
  protected virtual DesignatedRevokerItem CreateDesignatedRevokerItem(string fingerprint, PrimaryKey key)
  {
    DesignatedRevokerItem item = new DesignatedRevokerItem(fingerprint, key, "Designated revoker");
    item.SubItems.Add(fingerprint.Length > 8 ? fingerprint.Substring(fingerprint.Length - 8) : fingerprint);
    return item;
  }

  /// <include file="documentation.xml" path="/UI/ListBase/CreatePrimaryKeyItem/*"/>
  protected virtual PrimaryKeyItem CreatePrimaryKeyItem(PrimaryKey key)
  {
    PrimaryKeyItem item = new PrimaryKeyItem(key, key.PrimaryUserId.Name);
    item.SubItems.Add(key.ShortKeyId);
    item.SubItems.Add(key.HasSecretKey ? "pub/sec" : "pub");
    item.SubItems.Add(PGPUI.GetKeyValidityDescription(key));
    item.SubItems.Add(PGPUI.GetTrustDescription(key.OwnerTrust));
    item.SubItems.Add(key.ExpirationTime.HasValue ? key.ExpirationTime.Value.ToShortDateString() : "n/a");
    return item;
  }

  /// <include file="documentation.xml" path="/UI/ListBase/CreateSubkeyItem/*"/>
  protected virtual SubkeyItem CreateSubkeyItem(Subkey key)
  {
    if(key == null) throw new ArgumentNullException();

    bool signing = key.HasCapabilities(KeyCapabilities.Sign);
    bool encryption = key.HasCapabilities(KeyCapabilities.Encrypt);
    
    string text = signing && encryption ? "Signing/encryption" :
                    signing ? "Signing" : encryption ? "Encryption" : "Other";

    SubkeyItem item = new SubkeyItem(key, text + " subkey");
    item.SubItems.Add(key.ShortKeyId);
    item.SubItems.Add(key.KeyType);
    item.SubItems.Add(PGPUI.GetTrustDescription(key.CalculatedTrust));
    item.SubItems.Add(string.Empty);
    item.SubItems.Add(key.ExpirationTime.HasValue ? key.ExpirationTime.Value.ToShortDateString() : "n/a");
    return item;
  }

  /// <summary>Given a the effective ID of a primary key, returns the <see cref="PrimaryKeyItem"/> associated with it,
  /// or null if there is no associated item.
  /// </summary>
  protected PrimaryKeyItem GetPrimaryItem(string effectiveId)
  {
    PrimaryKeyItem item;
    itemDict.TryGetValue(effectiveId, out item);
    return item;
  }

  /// <summary>Given a <see cref="ListViewItem"/>, returns the <see cref="PrimaryKeyItem"/> associated with it, or null
  /// if there is no associated item.
  /// </summary>
  protected PrimaryKeyItem GetPrimaryItem(ListViewItem item)
  {
    PGPListViewItem pgpItem = item as PGPListViewItem;
    return pgpItem == null ? null : GetPrimaryItem(pgpItem);
  }

  /// <summary>Given a <see cref="PGPListViewItem"/>, returns the <see cref="PrimaryKeyItem"/> associated with it.</summary>
  protected PrimaryKeyItem GetPrimaryItem(PGPListViewItem pgpItem)
  {
    PrimaryKeyItem primaryItem = pgpItem as PrimaryKeyItem;
    return primaryItem != null ? primaryItem : GetPrimaryItem(pgpItem.PublicKey.EffectiveId);
  }

  /// <summary>Gets the primary key items associated with the selected items.</summary>
  protected PrimaryKeyItem[] GetSelectedPrimaryKeyItems()
  {
    List<PrimaryKeyItem> selectedItems = new List<PrimaryKeyItem>();
    foreach(ListViewItem item in SelectedItems)
    {
      PrimaryKeyItem primaryItem = GetPrimaryItem(item);
      if(primaryItem != null && !selectedItems.Contains(primaryItem)) selectedItems.Add(primaryItem);
    }
    return selectedItems.ToArray();
  }

  /// <summary>Gets the primary key items associated with the selected items, if they have secret keys.</summary>
  protected PrimaryKeyItem[] GetSelectedSecretKeyItems(bool ignoreUnusable)
  {
    List<PrimaryKeyItem> selectedItems = new List<PrimaryKeyItem>();
    foreach(ListViewItem item in SelectedItems)
    {
      PrimaryKeyItem primaryItem = GetPrimaryItem(item);
      if(primaryItem != null && primaryItem.PublicKey.HasSecretKey &&
         (!ignoreUnusable || primaryItem.PublicKey.Usable) && !selectedItems.Contains(primaryItem))
      {
        selectedItems.Add(primaryItem);
      }
    }
    return selectedItems.ToArray();
  }

  /// <summary>Gets the public keys associated with the selected items.</summary>
  protected PrimaryKey[] GetSelectedKeys()
  {
    PrimaryKeyItem[] items = GetSelectedPrimaryKeyItems();
    PrimaryKey[] keys = new PrimaryKey[items.Length];
    for(int i=0; i<keys.Length; i++) keys[i] = items[i].PublicKey;
    return keys;
  }

  /// <include file="documentation.xml" path="/UI/Common/OnKeyDown/*"/>
  protected override void OnKeyDown(KeyEventArgs e)
  {
    base.OnKeyDown(e);

    if(!e.Handled && e.Modifiers == Keys.None)
    {
      if(e.KeyCode == Keys.Apps)
      {
        ContextMenuStrip menu = CreateContextMenu();
        if(menu != null)
        {
          menu.Show(PointToScreen(SelectedIndices.Count == 0 ? new Point()
                                                             : GetItemRect(SelectedIndices[0]).Location));
          e.Handled = true;
        }
      }
      else if(SelectedItems.Count == 1)
      {
        ListViewItem item = SelectedItems[0];
        PrimaryKeyItem itemAsPrimary = item as PrimaryKeyItem;

        if(e.KeyCode == Keys.Left)
        {
          if(itemAsPrimary != null) CollapseItem(itemAsPrimary); // collapse primary items
          else Select(GetPrimaryItem(item), true); // and move from sub items to the primary item
          e.Handled = true;
        }
        else if(e.KeyCode == Keys.Right)
        {
          if(itemAsPrimary != null)
          {
            if(itemAsPrimary.Expanded) Select(Items[item.Index+1], true); // move from primary items to sub items
            else ExpandItem(itemAsPrimary); // or expand primary items if they're not expanded
          }
          e.Handled = true;
        }
        else if(e.KeyCode == Keys.Enter) // enter shows key properties
        {
          ActivateItem(item);
          e.Handled = true;
        }
      }
    }
  }

  /// <include file="documentation.xml" path="/UI/Common/OnMouseClick/*"/>
  protected override void OnMouseClick(MouseEventArgs e)
  {
    base.OnMouseClick(e);

    // this only seems to get called if an item was clicked on, but we'll check anyway

    if(e.Button == MouseButtons.Left)
    {
      PrimaryKeyItem item = GetItemAt(e.X, e.Y) as PrimaryKeyItem;
      // ugly code to see if they clicked on the +/- of a primary key item
      if(item != null && item.HasRelatedItems && e.X < TreeImageList.ImageSize.Width+4) ToggleItemExpansion(item);
    }
    else if(e.Button == MouseButtons.Right && GetItemAt(e.X, e.Y) != null) // the case where the user doesn't click on
    {                                                                      // an item is handled in OnMouseUp
      ContextMenuStrip menu = CreateContextMenu();
      if(menu != null) menu.Show(PointToScreen(e.Location));
    }
  }

  /// <include file="documentation.xml" path="/UI/Common/OnMouseDoubleClick/*"/>
  protected override void OnMouseDoubleClick(MouseEventArgs e)
  {
    base.OnMouseDoubleClick(e);

    if(e.Button == MouseButtons.Left) // double-clicking on a key opens its key properties
    {
      ListViewItem item = GetItemAt(e.X, e.Y);
      if(item != null) ActivateItem(item);
    }
  }

  /// <include file="documentation.xml" path="/UI/Common/OnMouseUp/*"/>
  protected override void OnMouseUp(MouseEventArgs e)
  {
    base.OnMouseUp(e);

    // since OnMouseClick only seems to get called if an item was clicked on, we'll check for the case where the user
    // clicks on empty space
    if(e.Button == MouseButtons.Right && GetItemAt(e.X, e.Y) == null)
    {
      ContextMenuStrip menu = CreateContextMenu();
      if(menu != null) menu.Show(PointToScreen(e.Location));
    }
  }

  /// <summary>Collapses the given <see cref="PrimaryKeyItem"/>, hiding its related items.</summary>
  protected void CollapseItem(PrimaryKeyItem item)
  {
    if(item == null) throw new ArgumentNullException();
    if(item.Expanded && item.HasRelatedItems)
    {
      for(int i=0; i<item.relatedItems.Length; i++) Items.RemoveAt(item.Index+1);
      item.ImageIndex = PlusImage;
      item.expanded   = false;
    }
  }

  /// <summary>Expands the given <see cref="PrimaryKeyItem"/>, showing its related items.</summary>
  protected void ExpandItem(PrimaryKeyItem item)
  {
    if(item == null) throw new ArgumentNullException();

    if(!item.Expanded && item.HasRelatedItems)
    {
      // add the items. the sorting may change the order
      for(int i=item.relatedItems.Length-1; i >= 0; i--) Items.Insert(item.Index+1, item.relatedItems[i]);
      
      // now that they've been added and are in order, set the icons
      for(int i=0; i < item.relatedItems.Length; i++)
      {
        Items[item.Index + i + 1].ImageIndex = (i == item.relatedItems.Length-1 ? CornerImage : IndentImage);
      }

      item.ImageIndex = MinusImage;
      item.expanded   = true;
    }
  }

  /// <summary>Gets all of the keys in the list that have a secret key.</summary>
  protected PrimaryKey[] GetSecretKeys(bool ignoreUnusable)
  {
    List<PrimaryKey> keys = new List<PrimaryKey>();
    foreach(PrimaryKeyItem item in itemDict.Values)
    {
      if(item.PublicKey.HasSecretKey && (!ignoreUnusable || item.PublicKey.Usable)) keys.Add(item.PublicKey);
    }
    return keys.ToArray();
  }

  /// <include file="documentation.xml" path="/UI/ListBase/RecreateItems/*"/>
  protected override void RecreateItems()
  {
    Dictionary<string, object> visible = new Dictionary<string, object>();

    foreach(ListViewItem item in Items)
    {
      PrimaryKeyItem primaryKeyItem = item as PrimaryKeyItem;
      if(primaryKeyItem != null) visible[primaryKeyItem.PublicKey.EffectiveId] = null;
    }

    Items.Clear();
    foreach(PrimaryKeyItem item in itemDict.Values)
    {
      bool isVisible = visible.ContainsKey(item.PublicKey.EffectiveId);
      AddKey(item.PublicKey, isVisible && item.Expanded);
      if(!isVisible) Items.Remove(GetPrimaryItem(item.PublicKey.EffectiveId));
    }
  }

  /// <summary>Reloads the given list of <see cref="PrimaryKeyItem"/> from the PGP system.</summary>
  protected void ReloadItems(params PrimaryKeyItem[] items)
  {
    AssertPGPSystem();

    PrimaryKey[] newKeys = PGP.RefreshKeys(GetPublicKeys(items),
                                           ListOptions.RetrieveAttributes | ListOptions.RetrieveSecretKeys);

    // remove the items, and recreate the ones that still exist
    for(int i=0; i<newKeys.Length; i++)
    {
      bool expanded = items[i].Expanded;
      RemoveItems(items[i]);
      if(newKeys[i] != null) AddKey(newKeys[i], expanded);
    }
  }

  /// <summary>Reloads the <see cref="PrimaryKeyItem"/> that represents the given public key.</summary>
  protected void ReloadKey(PrimaryKey publicKey)
  {
    PrimaryKeyItem item = GetPrimaryItem(publicKey.EffectiveId);
    if(item != null) ReloadItems(item);
    throw new ArgumentException("The key was not found in the list.");
  }

  /// <summary>Reloads the <see cref="PrimaryKeyItem"/> that represents the public key with the given id, or adds it if
  /// it does not exist in the list.
  /// </summary>
  protected void ReloadOrAddKey(string effectiveId)
  {
    AssertPGPSystem();

    PrimaryKeyItem item = GetPrimaryItem(effectiveId);
    if(item != null)
    {
      ReloadItems(item);
    }
    else
    {
      PrimaryKey key = PGP.FindKey(effectiveId, keyring,
                                   ListOptions.RetrieveAttributes | ListOptions.RetrieveSecretKeys);
      if(key != null) AddKey(key);
    }
  }

  /// <summary>Removes the given items and their subitems from the list.</summary>
  protected void RemoveItems(params PrimaryKeyItem[] items)
  {
    foreach(PrimaryKeyItem item in items)
    {
      CollapseItem(item);
      Items.RemoveAt(item.Index);
      itemDict.Remove(item.PublicKey.EffectiveId);
    }
  }

  /// <summary>Selects the given list view item, optionally deselecting all others first.</summary>
  protected void Select(ListViewItem item, bool deselectOthers)
  {
    if(item == null) throw new ArgumentNullException();

    if(deselectOthers && (SelectedIndices.Count > 1 || SelectedIndices.Count == 1 && !item.Selected))
    {
      SelectedIndices.Clear();
    }

    if(!item.Selected) item.Selected = true;

    item.Focused = true; // move the keyboard focus to the item, too
  }

  /// <summary>Expands the given item if it's collapsed, or collapses it if it's expanded.</summary>
  protected void ToggleItemExpansion(PrimaryKeyItem item)
  {
    if(item == null) throw new ArgumentNullException();
    if(item.Expanded) CollapseItem(item);
    else ExpandItem(item);
  }

  #region Commands
  /// <summary>Changes the passphrase for the first selected key, if a key is selected.</summary>
  protected void ChangePassphrase()
  {
    AssertPGPSystem();
    PrimaryKey[] keys = GetSelectedKeys();
    if(keys.Length == 0) return;

    ChangePasswordForm form = new ChangePasswordForm();
    if(form.ShowDialog() == DialogResult.OK)
    {
      try { PGP.ChangePassword(keys[0], form.GetPassword()); }
      catch(OperationCanceledException) { }
    }
  }

  /// <summary>Cleans the selected public keys.</summary>
  protected void CleanKeys()
  {
    AssertPGPSystem();
    PGP.CleanKeys(GetPublicKeys(GetSelectedPrimaryKeyItems()));
  }

  /// <summary>Deletes the public and secret portions of the selected keys.</summary>
  protected void DeleteKeys()
  {
    DeleteKeys(KeyDeletion.PublicAndSecret);
  }

  /// <summary>Deletes the given portions of the selected keys.</summary>
  protected void DeleteKeys(KeyDeletion deletion)
  {
    AssertPGPSystem();

    PrimaryKeyItem[] items = deletion == KeyDeletion.Secret ?
      GetSelectedSecretKeyItems(false) : GetSelectedPrimaryKeyItems();
    if(items.Length == 0) return;

    bool hasSecretKey = false;
    foreach(PrimaryKeyItem item in items)
    {
      if(item.PublicKey.HasSecretKey)
      {
        hasSecretKey = true;
        break;
      }
    }

    string message, caption;
    if(hasSecretKey)
    {
      caption = "Delete secret key?";
      message = "WARNING: You are about to delete " + (items.Length == 1 ? "a secret key" : "secret keys") + "!\n"+
                "If you delete your secret key (and you don't have a backup copy), you will no longer be able to "+
                "decrypt messages encrypted for that key, and you cannot revoke your key anymore.\n\n"+
                "Do you really want to delete " +
                (deletion == KeyDeletion.Secret ? "the" : "both the public and") + " secret key";
    }
    else
    {
      caption = "Delete public key?";
      message = "Are you sure you want to delete the public key";
    }
    message += (items.Length == 1 ? " '"+PGPUI.GetKeyName(items[0].PublicKey)+"'" : "s") + "?";

    if(MessageBox.Show(message, caption, MessageBoxButtons.YesNo,
                       hasSecretKey ? MessageBoxIcon.Exclamation : MessageBoxIcon.Warning,
                       MessageBoxDefaultButton.Button2) == DialogResult.Yes)
    {
      PGP.DeleteKeys(GetPublicKeys(items), deletion);
      ReloadItems(items);
    }
  }

  /// <summary>Deletes the secret portion of the selected keys.</summary>
  protected void DeleteSecretKeys()
  {
    DeleteKeys(KeyDeletion.Secret);
  }

  /// <summary>Disables the selected keys.</summary>
  protected void DisableKeys()
  {
    AssertPGPSystem();
    PrimaryKeyItem[] items = Array.FindAll(GetSelectedPrimaryKeyItems(),
                                           delegate(PrimaryKeyItem item) { return !item.PublicKey.Disabled; });
    PGP.DisableKeys(GetPublicKeys(items));
    ReloadItems(items);
  }

  /// <summary>Enables the selected keys.</summary>
  protected void EnableKeys()
  {
    AssertPGPSystem();
    PrimaryKeyItem[] items = Array.FindAll(GetSelectedPrimaryKeyItems(),
                                           delegate(PrimaryKeyItem item) { return item.PublicKey.Disabled; });
    PGP.EnableKeys(GetPublicKeys(items));
    ReloadItems(items);
  }

  /// <summary>Exports the selected keys to the destination of the user's choice.</summary>
  protected void ExportKeys()
  {
    AssertPGPSystem();

    PrimaryKey[] keys = GetSelectedKeys();
    ExportForm form = new ExportForm(keys);
    if(form.ShowDialog() == DialogResult.OK)
    {
      Stream output = OpenOutputFile(form.Filename);
      if(output == null) return;

      try
      {
        if(keys.Length == 0) PGP.ExportKeys(keyring, output, form.ExportOptions, form.OutputOptions);
        else PGP.ExportKeys(keys, output, form.ExportOptions, form.OutputOptions);
        FinishOutputFile(output);
      }
      finally { output.Close(); }
    }
  }

  /// <summary>Exports the selected keys to a file, using the default export options.</summary>
  protected void ExportKeysToFile()
  {
    AssertPGPSystem();

    DialogResult result =
      MessageBox.Show("Do you want to include the secret keys in the saved file?", "Include secret keys?",
                      MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
    if(result == DialogResult.Cancel) return;
    ExportKeysToFile(result == DialogResult.Yes);
  }

  /// <summary>Exports the selected keys to a file, optionally including the secret keys, using the default export
  /// options.
  /// </summary>
  protected void ExportKeysToFile(bool includeSecretKeys)
  {
    AssertPGPSystem();

    PrimaryKey[] keys = GetSelectedKeys();
    if(keys.Length == 0) return;

    string defaultFilename, defaultSuffix = includeSecretKeys ? " pub-sec.txt" : " pub.txt";
    if(keys.Length == 1) // if only one key is selected, include its name in the filename
    {
      defaultFilename = PGPUI.MakeSafeFilename(PGPUI.GetKeyName(keys[0])) + defaultSuffix;
    }
    else
    {
      defaultFilename = "Exported keys" + defaultSuffix;
    }

    SaveFileDialog sfd = new SaveFileDialog();
    sfd.DefaultExt = ".txt";
    sfd.FileName   = defaultFilename;
    sfd.Filter     = "Text Files (*.txt)|*.txt|ASCII Files (*.asc)|*.asc|PGP Files (*.pgp)|*.pgp|All Files (*.*)|*.*";
    sfd.Title      = "Export " + (includeSecretKeys ? "Secret and " : null) + "Public Keys";
    sfd.SupportMultiDottedExtensions = true;

    if(sfd.ShowDialog() == DialogResult.OK)
    {
      using(FileStream file = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write))
      {
        if(keys.Length != 0)
        {
          ExportOptions options = includeSecretKeys ? ExportOptions.ExportPublicAndSecretKeys : ExportOptions.Default;
          PGP.ExportKeys(keys, file, options, new OutputOptions(OutputFormat.ASCII));
        }
      }
    }
  }

  /// <summary>Exports the selected public keys to the clipboard, using the default export options.</summary>
  protected void ExportPublicKeysToClipboard()
  {
    AssertPGPSystem();

    PrimaryKey[] keys = GetSelectedKeys();
    if(keys.Length == 0) return;

    MemoryStream output = new MemoryStream();
    PGP.ExportKeys(keys, output, ExportOptions.Default, new OutputOptions(OutputFormat.ASCII));

    if(output.Length == 0) return;

    Clipboard.SetText(Encoding.ASCII.GetString(output.ToArray()));
  }

  /// <summary>Generates a revocation certificate for the first selected key, if any are selected.</summary>
  protected void GenerateRevocationCertificate()
  {
    AssertPGPSystem();

    PrimaryKey[] keys = GetSelectedKeys();
    if(keys.Length == 0) return;

    RevocationCertForm form = new RevocationCertForm(keys[0], GetSecretKeys(true));
    if(form.ShowDialog() == DialogResult.OK)
    {
      Stream output = OpenOutputFile(form.Filename);
      if(output == null) return;

      try
      {
        OutputOptions outputOptions = new OutputOptions(OutputFormat.ASCII);
        if(form.RevokeDirectly) PGP.GenerateRevocationCertificate(keys[0], output, form.Reason, outputOptions);
        else PGP.GenerateRevocationCertificate(keys[0], form.SelectedRevokingKey, output, form.Reason, outputOptions);
        FinishOutputFile(output);
      }
      catch(OperationCanceledException) { }
      finally { output.Close(); }
    }
  }

  /// <summary>Imports keys into the keyring given to <see cref="ShowKeyring"/>, or the default keyring if that method
  /// was not called.
  /// </summary>
  protected void ImportKeys()
  {
    AssertPGPSystem();

    ImportForm form = new ImportForm();
    if(form.ShowDialog() == DialogResult.OK)
    {
      Stream input = null;
      if(form.Filename == null)
      {
        if(Clipboard.ContainsText(TextDataFormat.Text) || Clipboard.ContainsText(TextDataFormat.UnicodeText))
        {
          try { input = new MemoryStream(Encoding.ASCII.GetBytes(Clipboard.GetText()), false); }
          catch { }
        }

        if(input == null)
        {
          MessageBox.Show("The clipboard does not seem to contain any text data.", "Clipboard has no text",
                          MessageBoxButtons.OK, MessageBoxIcon.Error);
          return;
        }
      }
      else
      {
        try { input = new FileStream(form.Filename, FileMode.Open, FileAccess.Read); }
        catch(Exception ex)
        {
          MessageBox.Show("The input file could not be opened. (The error was: " + ex.Message + ")",
                          "Couldn't open the file", MessageBoxButtons.OK, MessageBoxIcon.Error);
          return;
        }
      }

      ImportedKey[] results;
      try { results = PGP.ImportKeys(input, keyring, form.ImportOptions); }
      catch(Exception ex)
      {
        MessageBox.Show("The import failed. (The error was: " + ex.Message + ")", "Import failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }

      PGPUI.ShowImportResults(results);

      foreach(ImportedKey key in results)
      {
        if(key.Successful) ReloadOrAddKey(key.EffectiveId);
      }
    }
  }

  /// <summary>Imports keys from a key server into the keyring given to <see cref="ShowKeyring"/>, or the default
  /// keyring if that method was not called.
  /// </summary>
  protected void ImportKeysFromKeyServer()
  {
    AssertPGPSystem();
    
    KeyServerSearchForm form = new KeyServerSearchForm(PGP, keyring);

    form.ImportCompleted += delegate(object sender, ImportedKey[] results)
    {
      foreach(ImportedKey key in results)
      {
        if(key.Successful) ReloadOrAddKey(key.EffectiveId);
      }
    };

    form.ShowDialog();
  }

  /// <summary>Makes the selected key a designated revoker for one of the keys owned by the user.</summary>
  protected void MakeDesignatedRevoker()
  {
    AssertPGPSystem();

    PrimaryKey[] keys = GetSelectedKeys(), myKeys = GetSecretKeys(true);
    if(keys.Length == 0 || myKeys.Length == 0) return;

    MakeDesignatedRevokerForm form = new MakeDesignatedRevokerForm(keys[0], myKeys);
    if(form.ShowDialog() == DialogResult.OK)
    {
      try { PGP.AddDesignatedRevoker(form.SelectedKey, keys[0]); }
      catch(OperationCanceledException) { }
      ReloadKey(form.SelectedKey);
    }
  }

  /// <summary>Opens the subkey manager for the first selected key, if any are selected.</summary>
  protected void ManageSubkeys()
  {
    PrimaryKeyItem[] items = GetSelectedPrimaryKeyItems();
    if(items.Length == 0) return;
    ManageSubkeys(items[0]);
  }

  /// <summary>Opens the subkey manager for the given item.</summary>
  protected void ManageSubkeys(PrimaryKeyItem item)
  {
    if(item == null) throw new ArgumentNullException();
    new SubkeyManagerForm(PGP, item.PublicKey).ShowDialog();
    ReloadItems(item);
  }

  /// <summary>Opens the user ID manager for the first selected key, if any are selected.</summary>
  protected void ManageUserIds()
  {
    PrimaryKeyItem[] items = GetSelectedPrimaryKeyItems();
    if(items.Length == 0) return;
    ManageUserIds(items[0]);
  }

  /// <summary>Opens the user ID manager for the given item.</summary>
  protected void ManageUserIds(PrimaryKeyItem item)
  {
    if(item == null) throw new ArgumentNullException();
    new UserIdManagerForm(PGP, item.PublicKey).ShowDialog();
    ReloadItems(item);
  }

  /// <summary>Minimizes the selected keys, if any are selected.</summary>
  protected void MinimizeKeys()
  {
    AssertPGPSystem();

    PrimaryKeyItem[] items = GetSelectedPrimaryKeyItems();
    if(items.Length == 0) return;

    if(MessageBox.Show("Minimizing a key removes all signatures (except the self-signature) on each user ID in the "+
                       "key. You may have to sign the key again. Do you want to minimize the keys?", "Minimize keys?",
                       MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) ==
       DialogResult.Yes)
    {
      PGP.MinimizeKeys(GetPublicKeys(items));
      ReloadItems(items);
    }
  }

  /// <summary>Refreshes the selected keys from a public key server, if any are selected.</summary>
  protected void RefreshKeysFromKeyServer()
  {
    AssertPGPSystem();

    PrimaryKeyItem[] items = GetSelectedPrimaryKeyItems();
    if(items.Length == 0) return;

    string selection = items.Length == 1 ? PGPUI.GetKeyName(items[0].PublicKey) : "the selected keys";

    KeyServerForm form = new KeyServerForm();
    form.HelpText = "Refresh " + selection + " from which keyserver?";
    if(form.ShowDialog() == DialogResult.OK)
    {
      PrimaryKey[] keys = GetPublicKeys(items);
      ProgressForm progress = new ProgressForm(
        "Refreshing Keys", "Refreshing " + selection + " from " + form.SelectedKeyServer.AbsoluteUri + "...",
        delegate { PGP.RefreshKeysFromServer(new KeyDownloadOptions(form.SelectedKeyServer), keys); });

      DialogResult result = progress.ShowDialog();
      ReloadItems(items);

      if(result == DialogResult.Abort)
      {
        ImportFailedException failure = progress.Exception as ImportFailedException;
        if(failure != null && (failure.Reasons & FailureReason.KeyNotFound) != 0)
        {
          MessageBox.Show(items.Length == 1 ? "Key not found." : "Not all keys were found.", "Key(s) not found",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else
        {
          progress.ThrowException();
        }
      }
    }
  }

  /// <summary>Refreshes the first of the selected keys, if any keys are selected.</summary>
  protected void RevokeKey()
  {
    AssertPGPSystem();

    PrimaryKeyItem[] items = GetSelectedPrimaryKeyItems();
    if(items.Length == 0) return;

    KeyRevocationForm form = new KeyRevocationForm(items[0].PublicKey, GetSecretKeys(true));
    if(form.ShowDialog() == DialogResult.OK)
    {
      try
      {
        if(form.RevokeDirectly) PGP.RevokeKeys(form.Reason, items[0].PublicKey);
        else PGP.RevokeKeys(form.SelectedRevokingKey, form.Reason, items[0].PublicKey);
        ReloadItems(items[0]);
      }
      catch(OperationCanceledException) { }
    }
  }

  /// <summary>Sends the selected public keys to a key server, if any keys are selected.</summary>
  protected void SendKeysToKeyServer()
  {
    AssertPGPSystem();

    PrimaryKey[] keys = GetSelectedKeys();
    if(keys.Length == 0) return;

    string selection = keys.Length == 1 ? PGPUI.GetKeyName(keys[0]) : "the selected keys";

    KeyServerForm form = new KeyServerForm();
    form.HelpText = "Send " + selection + " to which keyserver?";
    if(form.ShowDialog() == DialogResult.OK)
    {
      ProgressForm progress = new ProgressForm(
        "Uploading Keys", "Sending " + selection + " to " + form.SelectedKeyServer.AbsoluteUri + "...",
        delegate { PGP.UploadKeys(new KeyUploadOptions(form.SelectedKeyServer), keys); });
      progress.ShowDialog();
      progress.ThrowException();
    }
  }

  /// <summary>Sets the owner trust of the selected keys, if any keys are selected.</summary>
  protected void SetOwnerTrust()
  {
    AssertPGPSystem();

    PrimaryKeyItem[] items = GetSelectedPrimaryKeyItems();
    if(items.Length == 0) return;

    PrimaryKey[] keys = GetPublicKeys(items);
    OwnerTrustForm form = new OwnerTrustForm(keys);
    if(form.ShowDialog() == DialogResult.OK)
    {
      PGP.SetOwnerTrust(form.TrustLevel, keys);
      ReloadItems(items);
    }
  }

  /// <summary>Shows the key properties of the first selected key, if any keys are selected.</summary>
  protected void ShowKeyProperties()
  {
    PrimaryKeyItem[] items = GetSelectedPrimaryKeyItems();
    if(items.Length == 0) return;
    ShowKeyProperties(items[0]);
  }

  /// <summary>Shows the key properties of the given item.</summary>
  protected void ShowKeyProperties(PrimaryKeyItem item)
  {
    if(item == null) throw new ArgumentNullException();
    new KeyPropertiesForm(item.PublicKey).ShowDialog();
  }

  /// <summary>Shows the photo ID of the first selected key, if any keys are selected.</summary>
  protected void ShowPhotoId()
  {
    foreach(PrimaryKey key in GetSelectedKeys())
    {
      foreach(UserAttribute attr in key.Attributes)
      {
        UserImage image = attr as UserImage;
        if(image != null)
        {
          ShowPhotoId(image);
          return;
        }
      }
    }
  }

  /// <summary>Shows the given photo ID.</summary>
  protected void ShowPhotoId(UserImage image)
  {
    new PhotoIdForm(image).ShowDialog();
  }

  /// <summary>Shows the signatures of the first selected key, if any keys are selected.</summary>
  protected void ShowSignatures()
  {
    AssertPGPSystem();
    PrimaryKey[] keys = GetSelectedKeys();
    if(keys.Length == 0) return;

    PrimaryKey key = PGP.RefreshKey(keys[0], ListOptions.VerifyAll);
    if(key != null) new KeySignaturesForm(key).ShowDialog();
    else RemoveItems(GetSelectedPrimaryKeyItems()[0]); // if the key no longer exists, remove it from the list
  }

  /// <summary>Signs the selected keys, if any keys are selected.</summary>
  protected void SignKeys()
  {
    AssertPGPSystem();

    PrimaryKeyItem[] items = Array.FindAll(GetSelectedPrimaryKeyItems(),
      delegate(PrimaryKeyItem item) { return item.PublicKey.HasCapabilities(KeyCapabilities.Certify) &&
                                      item.PublicKey.Usable; });
    if(items.Length == 0) return;

    PrimaryKey[] keys = GetPublicKeys(items), myKeys = GetSecretKeys(true);
    KeySigningForm form = new KeySigningForm(keys, myKeys);
    if(form.ShowDialog() == DialogResult.OK)
    {
      try { PGP.SignKeys(keys, form.SelectedSigningKey, form.KeySigningOptions); }
      catch(OperationCanceledException) { }
      ReloadItems(items);
    }
  }
  #endregion

  /// <summary>Given an array of <see cref="PrimaryKeyItem"/>, returns the corresponding public keys.</summary>
  protected static PrimaryKey[] GetPublicKeys(PrimaryKeyItem[] items)
  {
    PrimaryKey[] keys = new PrimaryKey[items.Length];
    for(int i=0; i<keys.Length; i++) keys[i] = items[i].PublicKey;
    return keys;
  }

  /// <summary>Given an array of <see cref="PrimaryKeyItem"/>, returns the keys that have the secret component.</summary>
  protected static PrimaryKey[] GetSecretKeys(PrimaryKeyItem[] items, bool ignoreUnusable)
  {
    List<PrimaryKey> keys = new List<PrimaryKey>();
    foreach(PrimaryKeyItem item in items)
    {
      PrimaryKey key = item.PublicKey;
      if(key.HasSecretKey && (!ignoreUnusable || key.Usable)) keys.Add(key);
    }
    return keys.ToArray();
  }

  void InitializeControl()
  {
    ColumnHeader userIdHeader, keyIdHeader, keyTypeHeader, validityHeader, trustHeader, expireHeader;

    userIdHeader = new ColumnHeader();
    userIdHeader.Text = "User ID";
    userIdHeader.Width = 300;

    keyIdHeader = new ColumnHeader();
    keyIdHeader.Text = "Key ID";
    keyIdHeader.Width = 80;

    keyTypeHeader = new ColumnHeader();
    keyTypeHeader.Text = "Type";
    keyTypeHeader.Width = 70;

    validityHeader = new ColumnHeader();
    validityHeader.Text = "Key validity";
    validityHeader.Width = 80;

    trustHeader = new ColumnHeader();
    trustHeader.Text = "Owner trust";
    trustHeader.Width = 80;

    expireHeader = new ColumnHeader();
    expireHeader.Text = "Expiration";
    expireHeader.Width = 75;

    Columns.AddRange(new ColumnHeader[] { userIdHeader, keyIdHeader, keyTypeHeader, validityHeader, trustHeader,
                                          expireHeader });

    base.ListViewItemSorter = new ItemComparerByName(this);
    base.SmallImageList     = TreeImageList;
  }

  /// <summary>Finishes an output file opened with <see cref="OpenOutputFile"/>. This must be called before the file is
  /// closed or disposed.
  /// </summary>
  void FinishOutputFile(Stream stream)
  {
    if(stream is MemoryStream)
    {
      Clipboard.SetText(Encoding.ASCII.GetString(((MemoryStream)stream).ToArray()));
    }
  }

  /// <summary>Opens an output file that writes to the named file, or to the clipboard if the file name is null.
  /// <see cref="FinishOutputFile"/> must be called on the returned stream before it is closed. The returned stream
  /// will be null if the file could not be opened, in which case a message will have already been displayed to the
  /// user.
  /// </summary>
  Stream OpenOutputFile(string filename)
  {
    Stream output;

    if(filename == null) output = new MemoryStream();
    else
    {
      try { output = new FileStream(filename, FileMode.Create, FileAccess.Write); }
      catch(Exception ex)
      {
        MessageBox.Show("The output file could not be opened. (The error was: " + ex.Message + ")",
                        "Couldn't open the file", MessageBoxButtons.OK, MessageBoxIcon.Error);
        output = null;
      }
    }

    return output;
  }

  Dictionary<string, PrimaryKeyItem> itemDict = new Dictionary<string, PrimaryKeyItem>(StringComparer.Ordinal);
  PGPSystem pgp;
  Keyring keyring;
  bool displayUserIds=true, displaySubkeys, displayRevokers;
}

} // namespace AdamMil.Security.UI