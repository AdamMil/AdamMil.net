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

public class KeyManagementList : KeyListBase
{
  public KeyManagementList()
  {
    InitializeControl();
  }

  [Category("Appearance")]
  [DefaultValue(false)]
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

  [Category("Appearance")]
  [DefaultValue(false)]
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

  [Category("Appearance")]
  [DefaultValue(true)]
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

  [Browsable(false)]
  public PGPSystem PGP
  {
    get { return pgp; }
    set { pgp = value; }
  }

  public void ShowKeyring(Keyring keyring)
  {
    AssertPGPSystem();

    Dictionary<string,PrimaryKey> secretKeys = new Dictionary<string,PrimaryKey>();
    foreach(PrimaryKey secretKey in PGP.GetSecretKeys(keyring))
    {
      secretKeys[secretKey.EffectiveId] = secretKey;
    }

    PrimaryKey[] publicKeys = PGP.GetPublicKeys(keyring, ListOptions.RetrieveAttributes);
    KeyPair[] pairs = new KeyPair[publicKeys.Length];
    for(int i=0; i<pairs.Length; i++)
    {
      PrimaryKey secretKey;
      secretKeys.TryGetValue(publicKeys[i].EffectiveId, out secretKey);
      pairs[i] = new KeyPair(publicKeys[i], secretKey);
    }

    Items.Clear();
    foreach(KeyPair pair in pairs) AddKeyPair(pair);
  }

  public void AddKeyPair(KeyPair pair)
  {
    AddKeyPair(pair, false);
  }

  public void AddKeyPair(KeyPair pair, bool expanded)
  {
    if(pair == null) throw new ArgumentNullException();

    List<ListViewItem> items = new List<ListViewItem>();

    PrimaryKeyItem primaryItem = CreatePrimaryKeyItem(pair.PublicKey, pair.SecretKey);
    if(primaryItem == null) throw new ApplicationException("CreatePrimaryKeyItem returned null.");
    SetFont(primaryItem, GetItemStatus(pair.PublicKey) | (pair.SecretKey == null ? 0 : ItemStatus.Owned));
    items.Add(primaryItem);

    if(DisplayUserIds)
    {
      List<UserAttribute> userIds = new List<UserAttribute>(pair.PublicKey.UserIds.Count +
                                                            pair.PublicKey.Attributes.Count);
      foreach(UserId id in pair.PublicKey.UserIds)
      {
        if(id != pair.PublicKey.PrimaryUserId) userIds.Add(id); // the public key item itself counts as the primary user ID
      }
      userIds.AddRange(pair.PublicKey.Attributes);

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
      foreach(Subkey subkey in pair.PublicKey.Subkeys)
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
      foreach(string revoker in pair.PublicKey.DesignatedRevokers)
      {
        ListViewItem item = CreateDesignatedRevokerItem(revoker, pair.PublicKey);
        if(item != null)
        {
          item.IndentCount = 1;
          SetFont(item, pair.PublicKey.Revoked ? ItemStatus.Revoked
                                               : pair.PublicKey.Expired ? ItemStatus.Expired : ItemStatus.Normal);
          items.Add(item);
        }
      }
    }

    Items.Add(items[0]);

    if(items.Count > 1)
    {
      primaryItem.ImageIndex = PlusImage;
      primaryItem.relatedItems = new ListViewItem[items.Count-1];
      items.CopyTo(1, primaryItem.relatedItems, 0, items.Count-1);

      if(expanded) ExpandItem(primaryItem);
    }
  }

  #region ItemComparerBase
  protected abstract class ItemComparerBase : System.Collections.IComparer, IComparer<PGPListViewItem>
  {
    protected ItemComparerBase(KeyManagementList list)
    {
      if(list == null) throw new ArgumentNullException();
      this.list = list;
    }

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

    protected KeyManagementList List
    {
      get { return list; }
    }

    protected abstract int Compare(PrimaryKeyItem a, PrimaryKeyItem b);

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
  protected class ItemCompareByName : ItemComparerBase
  {
    public ItemCompareByName(KeyManagementList list) : base(list) { }

    protected override int Compare(PrimaryKeyItem a, PrimaryKeyItem b)
    {
      return string.Compare(a.PublicKey.PrimaryUserId.Name, b.PublicKey.PrimaryUserId.Name,
                            StringComparison.CurrentCultureIgnoreCase);
    }
  }
  #endregion

  protected virtual void ActivateItem(ListViewItem item)
  {
    AttributeItem attrItem = item as AttributeItem;
    if(attrItem != null)
    {
      if(attrItem.Attribute is UserImage) // activating a user image displays it
      {
        ShowPhotoId((UserImage)attrItem.Attribute);
      }
      else // activating another attribute goes to the management form
      {
        ManageUserIds(GetPrimaryItem(item));
      }
    }
    else // otherwise, show the key properties
    {
      PrimaryKeyItem primaryItem = GetPrimaryItem(item);
      if(primaryItem != null) ShowKeyProperties(primaryItem);
    }
  }

  protected void AssertPGPSystem()
  {
    if(PGP == null) throw new InvalidOperationException("No PGP system has been set.");
  }

  protected override AttributeItem CreateAttributeItem(UserAttribute attr)
  {
    AttributeItem item = base.CreateAttributeItem(attr);
    item.SubItems.Add(string.Empty);
    item.SubItems.Add(string.Empty);
    item.SubItems.Add(PGPUI.GetTrustDescription(attr.CalculatedTrust));
    return item;
  }

  protected virtual ContextMenuStrip CreateContextMenu()
  {
    KeyPair[] pairs = GetSelectedKeyPairs();
    KeyPair[] myPairs = GetSecretKeyPairs();
    int attributeCount = 0, keyCount = pairs.Length, photoCount = 0, secretCount = 0;
    bool haveOwnedKeys = myPairs.Length != 0, hasEnabled = false, hasDisabled = false, hasUnrevokedAndCurrent = false;
    bool canRevoke = keyCount == 1 && pairs[0].SecretKey != null && !pairs[0].PublicKey.Revoked;

    foreach(KeyPair pair in pairs)
    {
      foreach(UserAttribute attr in pair.PublicKey.Attributes)
      {
        if(attr is UserImage) photoCount++;
      }

      if(pair.PublicKey.Disabled) hasDisabled = true;
      else hasEnabled = true;

      if(!pair.PublicKey.Revoked && !pair.PublicKey.Expired) hasUnrevokedAndCurrent = true;

      if(pair.SecretKey != null) secretCount++;
    }

    foreach(ListViewItem item in SelectedItems)
    {
      if(item is AttributeItem) attributeCount++;
    }

    // if we can't revoke because we own the key, perhaps we can revoke because we own a designated revoker key
    if(keyCount != 0 && !canRevoke && keyCount == 1 && !pairs[0].PublicKey.Revoked)
    {
      foreach(string designateRevoker in pairs[0].PublicKey.DesignatedRevokers)
      {
        foreach(KeyPair pair in myPairs)
        {
          if(string.Equals(pair.PublicKey.Fingerprint, designateRevoker, StringComparison.Ordinal))
          {
            canRevoke = true;
            break;
          }
        }
        if(canRevoke) break;
      }
    }

    ContextMenuStrip menu = new ContextMenuStrip();

    if(keyCount == 0)
    {
      menu.Items.Add(new ToolStripMenuItem("Export Keys...", null,
                                             delegate(object sender, EventArgs e) { ExportKeys(); }));
      menu.Items.Add(new ToolStripMenuItem("Import Keys...", null,
                                             delegate(object sender, EventArgs e) { ImportKeys(); }));
    }
    else
    {
      if(PGP != null)
      {
        // importing and exporting keys
        menu.Items.Add(new ToolStripMenuItem("Copy Public Keys to Clipboard", null,
                                             delegate(object sender, EventArgs e) { CopyPublicKeysToClipboard(); }));
        menu.Items.Add(new ToolStripMenuItem("Export Keys to File...", null,
                                             delegate(object sender, EventArgs e) { ExportKeysToFile(); }));
        menu.Items.Add(new ToolStripMenuItem("Import Keys...", null,
                                             delegate(object sender, EventArgs e) { ImportKeys(); }));
        menu.Items.Add(new ToolStripSeparator());

        // key server operations
        menu.Items.Add(new ToolStripMenuItem("Send Public Keys to Key Server...", null,
                                             delegate(object sender, EventArgs e) { SendKeysToKeyServer(); }));
        menu.Items.Add(new ToolStripMenuItem("Refresh Public Keys from Key Server...", null,
                                             delegate(object sender, EventArgs e) { RefreshKeysFromKeyServer(); }));
        menu.Items.Add(new ToolStripSeparator());

        // key signing
        menu.Items.Add(new ToolStripMenuItem("Sign Keys...", null,
                                             delegate(object sender, EventArgs e) { SignKeys(); }));
        menu.Items[menu.Items.Count-1].Enabled = haveOwnedKeys && hasUnrevokedAndCurrent;
        menu.Items.Add(new ToolStripMenuItem("Set Owner Trust...", null,
                                             delegate(object sender, EventArgs e) { SetOwnerTrust(); }));
        menu.Items.Add(new ToolStripSeparator());

        // key management
        menu.Items.Add(new ToolStripMenuItem("Clean Keys", null,
                                             delegate(object sender, EventArgs e) { CleanKeys(); }));
        menu.Items.Add(new ToolStripMenuItem("Delete Keys", null,
                                             delegate(object sender, EventArgs e) { DeleteKeys(); }));
        menu.Items.Add(new ToolStripMenuItem("Generate Revocation Certificate...", null,
                                             delegate(object sender, EventArgs e) { GenerateRevocationCertificate(); }));
        menu.Items[menu.Items.Count-1].Enabled = canRevoke;
        menu.Items.Add(new ToolStripMenuItem("Revoke Key...", null,
                                             delegate(object sender, EventArgs e) { RevokeKey(); }));
        menu.Items[menu.Items.Count-1].Enabled = canRevoke;
        menu.Items.Add(new ToolStripSeparator());
      }

      // user management
      if(PGP != null)
      {
        menu.Items.Add(new ToolStripMenuItem("Change Passphrase...", null,
                                             delegate(object sender, EventArgs e) { ChangePassphrase(); }));
        menu.Items[menu.Items.Count-1].Enabled = secretCount == 1;
        menu.Items.Add(new ToolStripMenuItem("Manage User IDs...", null,
                                             delegate(object sender, EventArgs e) { ManageUserIds(); }));
        menu.Items[menu.Items.Count-1].Enabled = secretCount == 1;
        menu.Items.Add(new ToolStripMenuItem("View Signatures...", null,
                                             delegate(object sender, EventArgs e) { ShowSignatures(); }));
        menu.Items[menu.Items.Count-1].Enabled = keyCount == 1;
      }

      menu.Items.Add(new ToolStripMenuItem("View Photo ID...", null,
                                           delegate(object sender, EventArgs e) { ShowPhotoId(); }));
      menu.Items[menu.Items.Count-1].Enabled = photoCount == 1;
      menu.Items.Add(new ToolStripMenuItem("View Key Properties...", null,
                                           delegate(object sender, EventArgs e) { ShowKeyProperties(); }));
      menu.Items[menu.Items.Count-1].Enabled = keyCount == 1;

      if(PGP != null)
      {
        menu.Items.Add(new ToolStripSeparator());

        // advanced options
        ToolStripMenuItem advanced = new ToolStripMenuItem("Advanced");
        advanced.DropDownItems.Add(new ToolStripMenuItem("Delete Secret Portion of Keys", null,
                                                      delegate(object sender, EventArgs e) { DeleteSecretKeys(); }));
        advanced.DropDownItems[advanced.DropDownItems.Count-1].Enabled = secretCount != 0;
        advanced.DropDownItems.Add(new ToolStripMenuItem("Disable Keys", null,
                                                         delegate(object sender, EventArgs e) { DisableKeys(); }));
        advanced.DropDownItems[advanced.DropDownItems.Count-1].Enabled = hasEnabled;
        advanced.DropDownItems.Add(new ToolStripMenuItem("Enable Keys", null,
                                                         delegate(object sender, EventArgs e) { EnableKeys(); }));
        advanced.DropDownItems[advanced.DropDownItems.Count-1].Enabled = hasDisabled;
        advanced.DropDownItems.Add(new ToolStripMenuItem("Minimize Keys", null,
                                                         delegate(object sender, EventArgs e) { MinimizeKeys(); }));
        advanced.DropDownItems.Add(new ToolStripMenuItem("Make this Key a Designated Revoker...", null,
                                                     delegate(object sender, EventArgs e) { MakeDesignatedRevoker(); }));
        advanced.DropDownItems[advanced.DropDownItems.Count-1].Enabled = keyCount == 1 && haveOwnedKeys;
        advanced.DropDownItems.Add(new ToolStripMenuItem("Export Keys...", null,
                                                         delegate(object sender, EventArgs e) { ExportKeys(); }));
        advanced.DropDownItems[advanced.DropDownItems.Count-1].Enabled = attributeCount != 0;

        menu.Items.Add(advanced);
      }
    }

    return menu;
  }

  protected virtual DesignatedRevokerItem CreateDesignatedRevokerItem(string fingerprint, PrimaryKey key)
  {
    DesignatedRevokerItem item = new DesignatedRevokerItem(fingerprint, key, "Designated revoker");
    item.SubItems.Add(fingerprint.Length > 8 ? fingerprint.Substring(fingerprint.Length - 8) : fingerprint);
    return item;
  }

  protected virtual PrimaryKeyItem CreatePrimaryKeyItem(PrimaryKey publicKey, PrimaryKey secretKey)
  {
    PrimaryKeyItem item = new PrimaryKeyItem(new KeyPair(publicKey, secretKey), publicKey.PrimaryUserId.Name);
    item.SubItems.Add(publicKey.ShortKeyId);
    item.SubItems.Add(secretKey == null ? "pub" : "pub/sec");
    item.SubItems.Add(PGPUI.GetKeyValidityDescription(publicKey));
    item.SubItems.Add(PGPUI.GetTrustDescription(publicKey.OwnerTrust));
    item.SubItems.Add(publicKey.ExpirationTime.HasValue ? publicKey.ExpirationTime.Value.ToShortDateString() : "n/a");
    return item;
  }

  protected virtual SubkeyItem CreateSubkeyItem(Subkey key)
  {
    if(key == null) throw new ArgumentNullException();

    bool signing = (key.Capabilities & KeyCapabilities.Sign) != 0;
    bool encryption = (key.Capabilities & KeyCapabilities.Encrypt) != 0;
    
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

  protected PrimaryKeyItem GetPrimaryItem(ListViewItem item)
  {
    PGPListViewItem pgpItem = item as PGPListViewItem;
    return pgpItem == null ? null : GetPrimaryItem(pgpItem);
  }

  protected PrimaryKeyItem GetPrimaryItem(PGPListViewItem pgpItem)
  {
    return (PrimaryKeyItem)Items[pgpItem.PublicKey.EffectiveId];
  }

  protected KeyPair[] GetSelectedKeyPairs()
  {
    PrimaryKeyItem[] items = GetSelectedPrimaryKeyItems();
    KeyPair[] pairs = new KeyPair[items.Length];
    for(int i=0; i<pairs.Length; i++) pairs[i] = items[i].KeyPair;
    return pairs;
  }

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

  protected PrimaryKey[] GetSelectedPublicKeys()
  {
    return GetPublicKeys(GetSelectedKeyPairs());
  }

  protected PrimaryKey[] GetSelectedSecretKeys()
  {
    return GetSecretKeys(GetSelectedKeyPairs());
  }

  protected override void OnKeyDown(KeyEventArgs e)
  {
    base.OnKeyDown(e);

    if(!e.Handled && e.Modifiers == Keys.None && SelectedItems.Count == 1)
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

  protected override void OnMouseClick(MouseEventArgs e)
  {
    base.OnMouseClick(e);

    if(e.Button == MouseButtons.Left)
    {
      PrimaryKeyItem item = GetItemAt(e.X, e.Y) as PrimaryKeyItem;
      // ugly code to see if they clicked on the +/- of a primary key item
      if(item != null && item.HasRelatedItems && e.X < TreeImageList.ImageSize.Width+4) ToggleItemExpansion(item);
    }
    else if(e.Button == MouseButtons.Right)
    {
      ContextMenuStrip menu = CreateContextMenu();
      if(menu != null) menu.Show(PointToScreen(e.Location));
    }
  }

  protected override void OnMouseDoubleClick(MouseEventArgs e)
  {
    base.OnMouseDoubleClick(e);

    if(e.Button == MouseButtons.Left) // double-clicking on a key opens its key properties
    {
      ListViewItem item = GetItemAt(e.X, e.Y);
      if(item != null) ActivateItem(item);
    }
  }

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

  protected KeyPair[] GetSecretKeyPairs()
  {
    List<KeyPair> pairs = new List<KeyPair>();
    foreach(ListViewItem item in Items)
    {
      PrimaryKeyItem primaryItem = item as PrimaryKeyItem;
      if(primaryItem != null && primaryItem.KeyPair.SecretKey != null) pairs.Add(primaryItem.KeyPair);
    }
    return pairs.ToArray();
  }

  protected virtual void RecreateItems()
  {
    List<PrimaryKeyItem> primaryKeyItems = new List<PrimaryKeyItem>();

    foreach(ListViewItem item in Items)
    {
      PrimaryKeyItem primaryKeyItem = item as PrimaryKeyItem;
      if(primaryKeyItem != null) primaryKeyItems.Add(primaryKeyItem);
    }

    Items.Clear();
    foreach(PrimaryKeyItem item in primaryKeyItems) AddKeyPair(item.KeyPair, item.Expanded);
  }

  protected void ReloadItem(PrimaryKeyItem item)
  {
    ReloadItems(new PrimaryKeyItem[] { item });
  }

  protected void ReloadItems(PrimaryKeyItem[] items)
  {
    AssertPGPSystem();

    KeyPair[] pairs = GetKeyPairs(items);

    PrimaryKey[] newPublicKeys = PGP.RefreshKeys(GetPublicKeys(pairs), ListOptions.RetrieveAttributes);
    
    List<PrimaryKey> oldSecretKeys = new List<PrimaryKey>();
    List<int> secretKeyIndices = new List<int>();

    for(int i=0; i<pairs.Length; i++)
    {
      if(pairs[i].SecretKey != null)
      {
        oldSecretKeys.Add(pairs[i].SecretKey);
        secretKeyIndices.Add(i);
      }
    }

    PrimaryKey[] refreshedSecretKeys = PGP.RefreshKeys(oldSecretKeys.ToArray());
    PrimaryKey[] newSecretKeys = new PrimaryKey[newPublicKeys.Length];
    for(int i=0; i<refreshedSecretKeys.Length; i++) newSecretKeys[secretKeyIndices[i]] = refreshedSecretKeys[i];

    RemoveItems(items);
    for(int i=0; i<newPublicKeys.Length; i++)
    {
      if(newPublicKeys[i] != null) AddKeyPair(new KeyPair(newPublicKeys[i], newSecretKeys[i]), items[i].Expanded);
    }
  }

  protected void ReloadKey(PrimaryKey key)
  {
    foreach(ListViewItem item in Items)
    {
      PrimaryKeyItem primaryItem = item as PrimaryKeyItem;
      if(primaryItem != null && primaryItem.PublicKey == key)
      {
        ReloadItem(primaryItem);
        return;
      }
    }

    throw new ArgumentException("The key was not found in the list.");
  }

  protected void RemoveItems(PrimaryKeyItem[] items)
  {
    foreach(PrimaryKeyItem item in items)
    {
      if(item.Expanded)
      {
        for(int i=0; i<item.relatedItems.Length; i++) Items.RemoveAt(item.Index+1);
      }
      Items.RemoveAt(item.Index);
    }
  }

  protected void Select(ListViewItem item, bool deselectOthers)
  {
    if(item == null) throw new ArgumentNullException();

    if(deselectOthers) SelectedIndices.Clear();
    else if(SelectedIndices.Contains(item.Index)) return;
    SelectedIndices.Add(item.Index);
    item.Focused = true;
  }

  protected void ToggleItemExpansion(PrimaryKeyItem item)
  {
    if(item == null) throw new ArgumentNullException();
    if(item.Expanded) CollapseItem(item);
    else ExpandItem(item);
  }

  #region Commands
  protected void ChangePassphrase()
  {
    AssertPGPSystem();
    PrimaryKey[] keys = GetSelectedPublicKeys();
    if(keys.Length == 0) return;

    ChangePasswordForm form = new ChangePasswordForm();
    if(form.ShowDialog() == DialogResult.OK)
    {
      try { PGP.ChangePassword(keys[0], form.GetPassword()); }
      catch(OperationCanceledException) { }
    }
  }

  protected void CopyPublicKeysToClipboard()
  {
    AssertPGPSystem();

    PrimaryKey[] keys = GetSelectedPublicKeys();
    if(keys.Length == 0) return;

    MemoryStream output = new MemoryStream();
    PGP.ExportPublicKeys(keys, output, ExportOptions.Default, new OutputOptions(OutputFormat.ASCII));

    if(output.Length == 0) return;

    Clipboard.SetText(Encoding.ASCII.GetString(output.ToArray()));
  }

  protected void CleanKeys()
  {
    AssertPGPSystem();
    PGP.CleanKeys(GetPublicKeys(GetSelectedPrimaryKeyItems()));
  }

  protected void DeleteKeys()
  {
    DeleteKeys(KeyDeletion.PublicAndSecret);
  }

  protected void DeleteKeys(KeyDeletion deletion)
  {
    AssertPGPSystem();

    KeyPair[] pairs = GetSelectedKeyPairs();
    if(pairs.Length == 0) return;

    bool hasSecretKey = false;
    foreach(KeyPair pair in pairs)
    {
      if(pair.SecretKey != null)
      {
        hasSecretKey = true;
        break;
      }
    }

    // if we're only deleting the secret portion, but there aren't any secret keys, then there's nothing to do
    if(!hasSecretKey && deletion == KeyDeletion.Secret) return;

    string message, caption;
    if(hasSecretKey)
    {
      caption = "Delete secret key?";
      message = "WARNING: You are about to delete " + (pairs.Length == 1 ? "a secret key" : "secret keys") + "!\n"+
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
    message += (pairs.Length == 1 ? " '"+PGPUI.GetKeyName(pairs[0].PublicKey)+"'" : "s") + "?";

    if(MessageBox.Show(message, caption, MessageBoxButtons.YesNo,
                       hasSecretKey ? MessageBoxIcon.Exclamation : MessageBoxIcon.Warning,
                       MessageBoxDefaultButton.Button2) == DialogResult.Yes)
    {
      PGP.DeleteKeys(GetPublicKeys(pairs), deletion);
      ReloadItems(GetSelectedPrimaryKeyItems());
    }
  }

  protected void DeleteSecretKeys()
  {
    DeleteKeys(KeyDeletion.Secret);
  }

  protected void DisableKeys()
  {
    AssertPGPSystem();
    PrimaryKeyItem[] items = Array.FindAll(GetSelectedPrimaryKeyItems(),
                                           delegate(PrimaryKeyItem item) { return !item.PublicKey.Disabled; });
    PGP.DisableKeys(GetPublicKeys(items));
    ReloadItems(items);
  }

  protected void EnableKeys()
  {
    AssertPGPSystem();
    PrimaryKeyItem[] items = Array.FindAll(GetSelectedPrimaryKeyItems(),
                                           delegate(PrimaryKeyItem item) { return item.PublicKey.Disabled; });
    PGP.EnableKeys(GetPublicKeys(items));
    ReloadItems(items);
  }

  protected void ExportKeys()
  {
    AssertPGPSystem();

    PrimaryKey[] keys = GetSelectedPublicKeys();
    ExportForm form = new ExportForm(keys);
    if(form.ShowDialog() == DialogResult.OK)
    {
      Stream output = OpenOutputFile(form.Filename);
      if(output == null) return;

      try
      {
        if(form.ExportPublicKeys) PGP.ExportPublicKeys(keys, output, form.ExportOptions, form.OutputOptions);
        if(form.ExportSecretKeys) PGP.ExportSecretKeys(keys, output, form.ExportOptions, form.OutputOptions);
        FinishOutputFile(output);
      }
      finally { output.Close(); }
    }
  }

  protected void ExportKeysToFile()
  {
    AssertPGPSystem();

    DialogResult result =
      MessageBox.Show("Do you want to include the secret keys in the saved file?", "Include secret keys?",
                      MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
    if(result == DialogResult.Cancel) return;
    ExportKeysToFile(result == DialogResult.Yes);
  }

  protected void ExportKeysToFile(bool includeSecretKeys)
  {
    AssertPGPSystem();

    KeyPair[] pairs = GetSelectedKeyPairs();

    string defaultFilename, defaultSuffix = includeSecretKeys ? " pub-sec.txt" : " pub.txt";
    if(pairs.Length == 1)
    {
      defaultFilename = PGPUI.MakeSafeFilename(PGPUI.GetKeyName(pairs[0].PublicKey)) + defaultSuffix;
    }
    else
    {
      defaultFilename = "Exported keys" + defaultSuffix;
    }

    SaveFileDialog sfd = new SaveFileDialog();
    sfd.DefaultExt      = ".txt";
    sfd.FileName        = defaultFilename;
    sfd.Filter          = "Text Files (*.txt)|*.txt|ASCII Files (*.asc)|*.asc|PGP Files (*.pgp)|*.pgp|All Files (*.*)|*.*";
    sfd.OverwritePrompt = true;
    sfd.Title           = "Export " + (includeSecretKeys ? "Secret and " : null) + "Public Keys";
    sfd.SupportMultiDottedExtensions = true;

    if(sfd.ShowDialog() == DialogResult.OK)
    {
      using(FileStream file = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write))
      {
        if(pairs.Length != 0)
        {
          OutputOptions output = new OutputOptions(OutputFormat.ASCII);
          PGP.ExportPublicKeys(GetPublicKeys(pairs), file, ExportOptions.Default, output);
          if(includeSecretKeys) PGP.ExportSecretKeys(GetSecretKeys(pairs), file, ExportOptions.Default, output);
        }
      }
    }
  }

  void GenerateRevocationCertificate()
  {
    AssertPGPSystem();
    PrimaryKey[] keys = GetSelectedPublicKeys();
    if(keys.Length == 0) return;

    RevocationCertForm form = new RevocationCertForm(keys[0], GetPublicKeys(GetSecretKeyPairs()));
    if(form.ShowDialog() == DialogResult.OK)
    {
      Stream output = OpenOutputFile(form.Filename);
      if(output == null) return;

      try
      {
        OutputOptions outputOptions = new OutputOptions(OutputFormat.ASCII);
        if(form.RevokeDirectly)
        {
          PGP.GenerateRevocationCertificate(keys[0], output, form.Reason, outputOptions);
        }
        else
        {
          PGP.GenerateRevocationCertificate(keys[0], form.SelectedRevokingKey, output, form.Reason,
                                                  outputOptions);
        }

        FinishOutputFile(output);
      }
      catch(OperationCanceledException) { }
      finally { output.Close(); }
    }
  }

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
      try { results = PGP.ImportKeys(input, form.ImportOptions); }
      catch(Exception ex)
      {
        MessageBox.Show("The import failed. (The error was: " + ex.Message + ")", "Import failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }

      int failCount = 0;
      foreach(ImportedKey key in results)
      {
        if(!key.Successful) failCount++;
      }

      MessageBox.Show((results.Length - failCount).ToString() + " key(s) imported successfully." +
                      (failCount == 0 ? null : "\n" + failCount.ToString() + " key(s) failed."), "Import results",
                      MessageBoxButtons.OK, failCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }
  }

  protected void MakeDesignatedRevoker()
  {
    AssertPGPSystem();

    PrimaryKey[] keys = GetSelectedPublicKeys(), myKeys = GetPublicKeys(GetSecretKeyPairs());
    if(keys.Length == 0 || myKeys.Length == 0) return;

    MakeDesignatedRevokerForm form = new MakeDesignatedRevokerForm(keys[0], myKeys);
    if(form.ShowDialog() == DialogResult.OK)
    {
      try { PGP.AddDesignatedRevoker(form.SelectedKey, keys[0]); }
      catch(OperationCanceledException) { }
      ReloadKey(form.SelectedKey);
    }
  }

  protected void ManageUserIds()
  {
    AssertPGPSystem();

    PrimaryKeyItem[] items = GetSelectedPrimaryKeyItems();
    if(items.Length == 0) return;

    ManageUserIds(items[0]);
  }

  protected void ManageUserIds(PrimaryKeyItem item)
  {
    if(item == null) throw new ArgumentNullException();
    new UserIdManagerForm(PGP, item.PublicKey).ShowDialog();
    ReloadItem(item);
  }

  protected void MinimizeKeys()
  {
    AssertPGPSystem();

    if(MessageBox.Show("Minimizing a key removes all signatures (except the self-signature) on each user ID in the "+
                       "key. You may have to sign the key again. Do you want to minimize the keys?", "Minimize keys?",
                       MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) ==
       DialogResult.Yes)
    {
      PGP.MinimizeKeys(GetPublicKeys(GetSelectedPrimaryKeyItems()));
    }
  }

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
      progress.ShowDialog();

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

      ReloadItems(items);
    }
  }

  protected void RevokeKey()
  {
    AssertPGPSystem();

    PrimaryKeyItem[] items = GetSelectedPrimaryKeyItems();
    if(items.Length == 0) return;

    KeyRevocationForm form = new KeyRevocationForm(items[0].PublicKey, GetPublicKeys(GetSecretKeyPairs()));
    if(form.ShowDialog() == DialogResult.OK)
    {
      try
      {
        if(form.RevokeDirectly)
        {
          PGP.RevokeKeys(form.Reason, items[0].PublicKey);
        }
        else
        {
          PGP.RevokeKeys(form.SelectedRevokingKey, form.Reason, items[0].PublicKey);
        }
        ReloadItems(items);
      }
      catch(OperationCanceledException) { }
    }
  }

  protected void SendKeysToKeyServer()
  {
    AssertPGPSystem();

    PrimaryKey[] keys = GetSelectedPublicKeys();
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

  protected void SetOwnerTrust()
  {
    AssertPGPSystem();

    PrimaryKeyItem[] items = GetSelectedPrimaryKeyItems();
    if(items.Length == 0) return;

    PrimaryKey[] keys = GetPublicKeys(items);

    OwnerTrustForm form = new OwnerTrustForm();

    // set the initial trust level to what all the keys agree on, or Unknown if they don't agree
    TrustLevel initialTrustLevel = keys[0].OwnerTrust;
    foreach(PrimaryKey key in keys)
    {
      if(key.OwnerTrust != initialTrustLevel) initialTrustLevel = TrustLevel.Unknown;
    }

    form.Initialize(initialTrustLevel, keys);
    if(form.ShowDialog() == DialogResult.OK)
    {
      PGP.SetOwnerTrust(form.TrustLevel, keys);
      ReloadItems(items);
    }
  }

  protected void ShowKeyProperties()
  {
    PrimaryKeyItem[] items = GetSelectedPrimaryKeyItems();
    if(items.Length == 0) return;
    ShowKeyProperties(items[0]);
  }

  protected void ShowKeyProperties(PrimaryKeyItem item)
  {
    if(item == null) throw new ArgumentNullException();
    new KeyPropertiesForm(item.KeyPair).ShowDialog();
  }

  protected void ShowPhotoId()
  {
    foreach(PrimaryKey key in GetSelectedPublicKeys())
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

  protected void ShowPhotoId(UserImage image)
  {
    new PhotoIdForm(image).ShowDialog();
  }

  protected void ShowSignatures()
  {
    AssertPGPSystem();
    PrimaryKey[] keys = GetSelectedPublicKeys();
    if(keys.Length == 0) return;

    PrimaryKey key = PGP.RefreshKey(keys[0], ListOptions.VerifyAll);
    if(key != null) new SignaturesForm(key).ShowDialog();
  }

  protected void SignKeys()
  {
    AssertPGPSystem();

    PrimaryKeyItem[] items = Array.FindAll(GetSelectedPrimaryKeyItems(),
      delegate(PrimaryKeyItem item) { return item.PublicKey.HasCapability(KeyCapabilities.Certify) &&
                                      !item.PublicKey.Expired && !item.PublicKey.Revoked; });
    if(items.Length == 0) return;

    PrimaryKey[] keys = GetPublicKeys(items), myKeys = GetPublicKeys(GetSecretKeyPairs());

    KeySigningForm form = new KeySigningForm(keys, myKeys);

    if(form.ShowDialog() == DialogResult.OK)
    {
      try { PGP.SignKeys(keys, form.SelectedSigningKey, form.KeySigningOptions); }
      catch(OperationCanceledException) { }
      ReloadItems(items);
    }
  }
  #endregion

  protected static KeyPair[] GetKeyPairs(PrimaryKeyItem[] items)
  {
    KeyPair[] pairs = new KeyPair[items.Length];
    for(int i=0; i<pairs.Length; i++) pairs[i] = items[i].KeyPair;
    return pairs;
  }

  protected static PrimaryKey[] GetPublicKeys(KeyPair[] pairs)
  {
    PrimaryKey[] keys = new PrimaryKey[pairs.Length];
    for(int i=0; i<keys.Length; i++) keys[i] = pairs[i].PublicKey;
    return keys;
  }

  protected static PrimaryKey[] GetPublicKeys(PrimaryKeyItem[] items)
  {
    PrimaryKey[] keys = new PrimaryKey[items.Length];
    for(int i=0; i<keys.Length; i++) keys[i] = items[i].PublicKey;
    return keys;
  }

  protected static PrimaryKey[] GetSecretKeys(KeyPair[] pairs)
  {
    List<PrimaryKey> keys = new List<PrimaryKey>();
    foreach(KeyPair pair in pairs)
    {
      if(pair.SecretKey != null) keys.Add(pair.SecretKey);
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

    base.ListViewItemSorter = new ItemCompareByName(this);
  }

  void FinishOutputFile(Stream stream)
  {
    if(stream is MemoryStream)
    {
      Clipboard.SetText(Encoding.ASCII.GetString(((MemoryStream)stream).ToArray()));
    }
  }

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

  PGPSystem pgp;
  bool displayUserIds=true, displaySubkeys, displayRevokers;
}

} // namespace AdamMil.Security.UI