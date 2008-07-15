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
using System.Windows.Forms;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

#region PGPListBase
// NOTE: this has to be the first class in the file in order for the resources to be compiled correctly
/// <summary>A base class for various lists of PGP items.</summary>
public abstract class PGPListBase : ListView
{
  /// <summary>Initializes a new <see cref="PGPListBase"/>.</summary>
  public PGPListBase()
  {
    // add flags to help reduce flicker
    this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
    this.SetStyle(ControlStyles.EnableNotifyMessage, true);

    ComponentResourceManager resources = new ComponentResourceManager(typeof(PGPListBase));

    treeImageList = new ImageList();
    treeImageList.ImageStream = ((ImageListStreamer)(resources.GetObject("listBase.ImageStream")));
    treeImageList.TransparentColor = System.Drawing.Color.Magenta;
    treeImageList.Images.SetKeyName(0, "Minus");
    treeImageList.Images.SetKeyName(1, "Plus");
    treeImageList.Images.SetKeyName(2, "Indent");
    treeImageList.Images.SetKeyName(3, "Corner");

    base.AllowColumnReorder = true;
    base.Font               = new Font("Arial", 8f);
    base.FullRowSelect      = true;
    base.SmallImageList     = treeImageList;
    base.View               = View.Details;
  }

  /// <summary>The index of the "minus" image, which represents an item that can be collapsed.</summary>
  protected const int MinusImage  = 0;
  /// <summary>The index of the "plus" image, which represents an item that can be expanded.</summary>
  protected const int PlusImage   = 1;
  /// <summary>The index of the "indent" image, which shows the indentation of a subitem, for subitems except the last.</summary>
  protected const int IndentImage = 2;
  /// <summary>The index of the "indent" image, which shows the indentation of the last subitem.</summary>
  protected const int CornerImage = 3;

  /// <summary>Gets the image list containing the tree images (<see cref="MinusImage"/>, <see cref="PlusImage"/>,
  /// <see cref="IndentImage"/>, and <see cref="CornerImage"/>).
  /// </summary>
  protected ImageList TreeImageList
  {
    get { return treeImageList; }
  }

  /// <include file="documentation.xml" path="/UI/ListBase/ClearCachedFonts/*"/>
  protected virtual void ClearCachedFonts() { }

  /// <include file="documentation.xml" path="/UI/ListBase/CreateAttributeItem/*"/>
  protected virtual AttributeItem CreateAttributeItem(UserAttribute attr)
  {
    if(attr == null) throw new ArgumentNullException();
    return new AttributeItem(attr, PGPUI.GetAttributeName(attr));
  }

  /// <include file="documentation.xml" path="/UI/Common/OnFontChanged/*"/>
  protected override void OnFontChanged(EventArgs e)
  {
    base.OnFontChanged(e);
    ClearCachedFonts();
    RecreateItems();
  }

  /// <include file="documentation.xml" path="/UI/Common/OnForeColorChanged/*"/>
  protected override void OnForeColorChanged(EventArgs e)
  {
    base.OnForeColorChanged(e);
    ClearCachedFonts();
    RecreateItems();
  }

  /// <include file="documentation.xml" path="/UI/Common/OnNotifyMessage/*"/>
  protected override void OnNotifyMessage(Message m)
  {
    // filter out the WM_ERASEBKGND message to prevent flicker
    if(m.Msg != 0x14) base.OnNotifyMessage(m);
  }

  /// <include file="documentation.xml" path="/UI/ListBase/RecreateItems/*"/>
  protected virtual void RecreateItems() { }

  readonly ImageList treeImageList;
}
#endregion

#region KeyListBase
/// <summary>A base class for lists that display PGP keys.</summary>
public abstract class KeyListBase : PGPListBase
{
  /// <summary>Represents the status of an item.</summary>
  [Flags]
  protected enum ItemStatus
  {
    /// <summary>The item is in a normal state (not expired, revoked, etc).</summary>
    Normal=0,
    /// <summary>The item has been revoked.</summary>
    Revoked=1,
    /// <summary>The item has been disabled.</summary>
    Disabled=2,
    /// <summary>The item has expired.</summary>
    Expired=3,
    /// <summary>A mask that can be applied to a <see cref="ItemStatus"/> value to determine its basic status
    /// (<see cref="Normal"/>, <see cref="Revoked"/>, <see cref="Disabled"/>, or <see cref="Expired"/>).
    /// </summary>
    BasicStatusMask=3,
    /// <summary>A flag that indicates that the item is owned by the current user.</summary>
    Owned=4,
  }

  /// <include file="documentation.xml" path="/UI/ListBase/ClearCachedFonts/*"/>
  protected override void ClearCachedFonts()
  {
    Array.Clear(fonts, 0, fonts.Length);
  }

  /// <include file="documentation.xml" path="/UI/ListBase/CreateFont/*"/>
  protected virtual void CreateFont(ItemStatus type, out Font font, out Color color)
  {
    ItemStatus basicStatus = type & ItemStatus.BasicStatusMask;

    FontStyle style = FontStyle.Regular;
    switch(basicStatus)
    {
      case ItemStatus.Expired: style = FontStyle.Italic; break;
      case ItemStatus.Revoked: style = FontStyle.Italic | FontStyle.Strikeout; break;
    }
    if((type & ItemStatus.Owned) != 0) style |= FontStyle.Bold;

    font  = style == FontStyle.Regular ? Font : new Font(Font, style);
    color = basicStatus == ItemStatus.Normal ? ForeColor : SystemColors.GrayText;
  }

  /// <summary>Gets the <see cref="ItemStatus"/> value representing the status of a <see cref="Key"/>.</summary>
  protected ItemStatus GetItemStatus(Key key)
  {
    if(key == null) throw new ArgumentNullException();
    return key.Revoked || key.GetPrimaryKey().Revoked ? ItemStatus.Revoked :
           key.Expired || key.GetPrimaryKey().Expired ? ItemStatus.Expired :
           key.GetPrimaryKey().Disabled ? ItemStatus.Disabled :
           ItemStatus.Normal;
  }

  /// <summary>Gets the <see cref="ItemStatus"/> value representing the status of a <see cref="UserAttribute"/>.</summary>
  protected ItemStatus GetItemStatus(UserAttribute attr)
  {
    if(attr == null) throw new ArgumentNullException();
    return attr.Revoked || attr.PrimaryKey.Revoked ? ItemStatus.Revoked :
           attr.PrimaryKey.Expired ? ItemStatus.Expired :
           attr.PrimaryKey.Disabled ? ItemStatus.Disabled :
           ItemStatus.Normal;
  }

  /// <include file="documentation.xml" path="/UI/Common/OnKeyDown/*"/>
  protected override void OnKeyDown(KeyEventArgs e)
  {
    base.OnKeyDown(e);

    if(!e.Handled && e.Modifiers == Keys.Control && e.KeyCode == Keys.A&& MultiSelect) // ctrl-a selects all items
    {
      foreach(ListViewItem item in Items) item.Selected = true;
      e.Handled = true;
    }
  }

  /// <summary>Given a <see cref="ListViewItem"/>, sets its font based on the given <see cref="ItemStatus"/>.</summary>
  protected void SetFont(ListViewItem item, ItemStatus type)
  {
    Font font = fonts[(int)type];
    if(font == null)
    {
      CreateFont(type, out font, out fontColors[(int)type]);
      if(font == null) font = Font;
      fonts[(int)type] = font;
    }

    item.Font      = font;
    item.ForeColor = fontColors[(int)type];
  }

  Font[] fonts = new Font[8];
  Color[] fontColors = new Color[8];
}
#endregion

#region PGPListViewItem
/// <summary>Provides a base class for all list view items that represent PGP objects.</summary>
public abstract class PGPListViewItem : ListViewItem
{
  /// <summary>Initializes a new <see cref="PGPListViewItem"/>.</summary>
  protected PGPListViewItem() { }
  /// <summary>Initializes a new <see cref="PGPListViewItem"/> with the given text.</summary>
  protected PGPListViewItem(string text) : base(text) { }

  /// <summary>Gets the <see cref="PrimaryKey"/> that is associated with the PGP item represented by this list item.</summary>
  public abstract PrimaryKey PublicKey
  {
    get;
  }
}
#endregion

#region AttributeItem
/// <summary>A <see cref="PGPListViewItem"/> that represents a <see cref="UserAttribute"/>.</summary>
public class AttributeItem : PGPListViewItem
{
  /// <summary>Initializes a new <see cref="AttributeItem"/>.</summary>
  public AttributeItem(UserAttribute attr) : this(attr, string.Empty) { }

  /// <summary>Initializes a new <see cref="AttributeItem"/> with the given text.</summary>
  public AttributeItem(UserAttribute attr, string text) : base(text)
  {
    if(attr == null) throw new ArgumentNullException();
    this.attr = attr;
    this.Name = attr.Id == null ? null : attr.Id;
  }

  /// <summary>Gets the <see cref="UserAttribute"/> that this list item represents.</summary>
  public UserAttribute Attribute
  {
    get { return attr; }
  }

  /// <summary>Gets the <see cref="PrimaryKey"/> that owns the <see cref="Attribute"/>.</summary>
  public override PrimaryKey PublicKey
  {
    get { return attr.PrimaryKey; }
  }

  readonly UserAttribute attr;
}
#endregion

#region DesignatedRevokerItem
/// <summary>A <see cref="PGPListViewItem"/> that represents a designated revoker of a key.</summary>
public class DesignatedRevokerItem : PGPListViewItem
{
  /// <summary>Initializes a new <see cref="DesignatedRevokerItem"/> with the fingerprint of the designated revoker key
  /// and the <see cref="PrimaryKey"/> that it is allowed to revoke.
  /// </summary>
  public DesignatedRevokerItem(string fingerprint, PrimaryKey publicKey)
    : this(fingerprint, publicKey, string.Empty) { }

  /// <summary>Initializes a new <see cref="DesignatedRevokerItem"/> with the fingerprint of the designated revoker
  /// key, and the <see cref="PrimaryKey"/> that it is allowed to revoke, and the text of the item.
  /// </summary>
  public DesignatedRevokerItem(string fingerprint, PrimaryKey publicKey, string text) : base(text)
  {
    if(fingerprint == null || publicKey == null) throw new ArgumentNullException();
    if(string.IsNullOrEmpty(fingerprint)) throw new ArgumentException("The fingerprint is empty.");
    this.fingerprint = fingerprint;
    this.publicKey   = publicKey;
  }

  /// <summary>Gets the fingerprint of the designated revoker key.</summary>
  public string Fingerprint
  {
    get { return fingerprint; }
  }

  /// <summary>Gets the <see cref="PrimaryKey"/> that the designated revoker is allowed to revoke.</summary>
  public override PrimaryKey PublicKey
  {
    get { return publicKey; }
  }

  readonly PrimaryKey publicKey;
  readonly string fingerprint;
}
#endregion

#region KeySignatureItem
/// <summary>A <see cref="PGPListViewItem"/> that represents a <see cref="KeySignature"/>.</summary>
public class KeySignatureItem : PGPListViewItem
{
  /// <summary>Initializes a new <see cref="KeySignatureItem"/> with the <see cref="KeySignature"/> that it represents.</summary>
  public KeySignatureItem(KeySignature sig) : this(sig, null) { }

  /// <summary>Initializes a new <see cref="KeySignatureItem"/> with the <see cref="KeySignature"/> that it represents,
  /// and the text of the item.
  /// </summary>
  public KeySignatureItem(KeySignature sig, string text) : base(text)
  {
    if(sig == null) throw new ArgumentNullException();
    this.sig = sig;
  }

  /// <summary>Gets the <see cref="PrimaryKey"/> that is signed by the signature.</summary>
  public override PrimaryKey PublicKey
  {
    get { return sig.Object.PrimaryKey; }
  }

  /// <summary>Gets the <see cref="KeySignature"/> that this list item represents.</summary>
  public KeySignature Signature
  {
    get { return sig; }
  }

  readonly KeySignature sig;
}
#endregion

#region PrimaryKeyItem
/// <summary>A <see cref="PGPListViewItem"/> that represents a <see cref="KeyPair"/>.</summary>
public class PrimaryKeyItem : PGPListViewItem
{
  /// <summary>Initializes a new <see cref="PrimaryKeyItem"/> with the public portion of the key pair it represents.</summary>
  public PrimaryKeyItem(PrimaryKey publicKey) : this(new KeyPair(publicKey, null), null) { }

  /// <summary>Initializes a new <see cref="PrimaryKeyItem"/> with the public portion of the key pair it represents,
  /// and the text of the item.
  /// </summary>
  public PrimaryKeyItem(PrimaryKey publicKey, string text) : this(new KeyPair(publicKey, null), text) { }

  /// <summary>Initializes a new <see cref="PrimaryKeyItem"/> with the <see cref="KeyPair"/> it represents.</summary>
  public PrimaryKeyItem(KeyPair keyPair) : this(keyPair, null) { }

  /// <summary>Initializes a new <see cref="PrimaryKeyItem"/> with the <see cref="KeyPair"/> it represents, and the
  /// text of the item.
  /// </summary>
  public PrimaryKeyItem(KeyPair keyPair, string text) : base(text)
  {
    if(keyPair == null) throw new ArgumentNullException();
    this.keyPair = keyPair;
    this.Name    = keyPair.PublicKey.EffectiveId;
  }

  /// <summary>Gets whether the <see cref="PrimaryKeyItem"/> is expanded to show its related items.</summary>
  public bool Expanded
  {
    get { return expanded; }
  }

  /// <summary>Gets whether the <see cref="PrimaryKeyItem"/> has related items.</summary>
  public bool HasRelatedItems
  {
    get { return relatedItems != null; }
  }

  /// <summary>Gets the <see cref="KeyPair"/> represented by this list item.</summary>
  public KeyPair KeyPair
  {
    get { return keyPair; }
  }

  /// <summary>Gets the public key of the <see cref="KeyPair"/> represented by this list item.</summary>
  public override PrimaryKey PublicKey
  {
    get { return KeyPair.PublicKey; }
  }

  /// <summary>Gets the secret key of the <see cref="KeyPair"/> represented by this list item, or null if the secret
  /// key is not available.
  /// </summary>
  public PrimaryKey SecretKey
  {
    get { return KeyPair.SecretKey; }
  }

  /// <summary>Gets an array of list items that are related to this <see cref="PrimaryKeyItem"/>.</summary>
  public ListViewItem[] GetRelatedItems()
  {
    return relatedItems == null ? new ListViewItem[0] : (ListViewItem[])relatedItems.Clone();
  }

  readonly KeyPair keyPair;
  internal ListViewItem[] relatedItems;
  internal bool expanded;
}
#endregion

#region SubkeyItem
/// <summary>A <see cref="PGPListViewItem"/> that represents a <see cref="PGP.Subkey"/>.</summary>
public class SubkeyItem : PGPListViewItem
{
  /// <summary>Initializes a new <see cref="SubkeyItem"/> with the <see cref="PGP.Subkey"/> that it represents.</summary>
  public SubkeyItem(Subkey subkey) : this(subkey, string.Empty) { }

  /// <summary>Initializes a new <see cref="SubkeyItem"/> with the <see cref="PGP.Subkey"/> that it represents, and the
  /// text of the item.
  /// </summary>
  public SubkeyItem(Subkey subkey, string text) : base(text)
  {
    if(subkey == null) throw new ArgumentNullException();
    this.subkey = subkey;
    this.Name   = subkey.EffectiveId;
  }

  /// <summary>Gets the <see cref="PrimaryKey"/> that owns the associated subkey.</summary>
  public override PrimaryKey PublicKey
  {
    get { return subkey.PrimaryKey; }
  }

  /// <summary>Gets the <see cref="PGP.Subkey"/> represented by this list item.</summary>
  public Subkey Subkey
  {
    get { return subkey; }
  }

  readonly Subkey subkey;
}
#endregion

} // namespace AdamMil.Security.UI