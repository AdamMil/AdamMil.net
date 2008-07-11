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
public abstract class PGPListBase : ListView
{
  public PGPListBase()
  {
    // add flags for less flicker
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

  protected const int MinusImage=0, PlusImage=1, IndentImage=2, CornerImage=3;

  protected ImageList TreeImageList
  {
    get { return treeImageList; }
  }

  protected virtual void ClearCachedFonts() { }

  protected virtual AttributeItem CreateAttributeItem(UserAttribute attr)
  {
    if(attr == null) throw new ArgumentNullException();
    return new AttributeItem(attr, PGPUI.GetAttributeName(attr));
  }

  protected override void OnFontChanged(EventArgs e)
  {
    base.OnFontChanged(e);
    ClearCachedFonts();
  }

  protected override void OnForeColorChanged(EventArgs e)
  {
    base.OnForeColorChanged(e);
    ClearCachedFonts();
  }

  protected override void OnNotifyMessage(Message m)
  {
    // filter out the WM_ERASEBKGND message to prevent flicker
    if(m.Msg != 0x14) base.OnNotifyMessage(m);
  }

  readonly ImageList treeImageList;
}
#endregion

#region KeyListBase
public abstract class KeyListBase : PGPListBase
{
  [Flags]
  protected enum ItemStatus
  {
    Normal=0, Revoked=1, Disabled=2, Expired=3, TypeMask=3,
    Owned=4,
  }

  protected override void ClearCachedFonts()
  {
    Array.Clear(fonts, 0, fonts.Length);
  }

  protected virtual void CreateFont(ItemStatus type, out Font font, out Color color)
  {
    ItemStatus basicType = type & ItemStatus.TypeMask;

    color = basicType == ItemStatus.Normal ? ForeColor : SystemColors.GrayText;

    FontStyle style = FontStyle.Regular;
    switch(type & ItemStatus.TypeMask)
    {
      case ItemStatus.Expired: style = FontStyle.Italic; break;
      case ItemStatus.Revoked: style = FontStyle.Italic | FontStyle.Strikeout; break;
    }

    if((type & ItemStatus.Owned) != 0) style |= FontStyle.Bold;

    font = new Font(Font, style);
  }

  protected ItemStatus GetItemStatus(Key key)
  {
    if(key == null) throw new ArgumentNullException();
    return key.Revoked || key.GetPrimaryKey().Revoked ? ItemStatus.Revoked :
           key.Expired || key.GetPrimaryKey().Expired ? ItemStatus.Expired :
           key.GetPrimaryKey().Disabled ? ItemStatus.Disabled :
           ItemStatus.Normal;
  }

  protected ItemStatus GetItemStatus(UserAttribute attr)
  {
    if(attr == null) throw new ArgumentNullException();
    return attr.Revoked || attr.PrimaryKey.Revoked ? ItemStatus.Revoked :
           attr.PrimaryKey.Expired ? ItemStatus.Expired :
           attr.PrimaryKey.Disabled ? ItemStatus.Disabled :
           ItemStatus.Normal;
  }

  protected override void OnKeyDown(KeyEventArgs e)
  {
    base.OnKeyDown(e);

    if(!e.Handled && e.Modifiers == Keys.Control && e.KeyCode == Keys.A&& MultiSelect) // ctrl-a selects all items
    {
      foreach(ListViewItem item in Items) item.Selected = true;
      e.Handled = true;
    }
  }

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

#region KeyPair
public sealed class KeyPair
{
  public KeyPair(PrimaryKey publicKey, PrimaryKey secretKey)
  {
    if(publicKey == null) throw new ArgumentNullException();
    if(publicKey.Secret) throw new ArgumentException("The public key is supposed to be public, but is secret.");
    if(secretKey != null && !secretKey.Secret)
    {
      throw new ArgumentException("The public key is supposed to be secret, but is public.");
    }
    if(secretKey != null && !string.Equals(publicKey.EffectiveId, secretKey.EffectiveId, StringComparison.Ordinal))
    {
      throw new ArgumentException("The effective IDs of the public and secret keys do not match.");
    }

    this.publicKey = publicKey;
    this.secretKey = secretKey;
  }

  public PrimaryKey PublicKey
  {
    get { return publicKey; }
  }

  public PrimaryKey SecretKey
  {
    get { return secretKey; }
  }

  public override bool Equals(object obj)
  {
    return Equals(obj as KeyPair);
  }

  public bool Equals(KeyPair other)
  {
    return this == other;
  }

  public override int GetHashCode()
  {
    string id = publicKey.EffectiveId;
    return id == null ? 0 : id.GetHashCode();
  }

  public static bool operator==(KeyPair a, KeyPair b)
  {
    if((object)a == (object)b) return true; // cast to object to prevent stack overflow with ==
    else if((object)a == null || (object)b == null) return false;
    else return a.PublicKey == b.PublicKey && a.SecretKey == b.SecretKey;
  }

  public static bool operator!=(KeyPair a, KeyPair b)
  {
    return !(a == b);
  }

  readonly PrimaryKey publicKey, secretKey;
}
#endregion

#region PGPListViewItem
public abstract class PGPListViewItem : ListViewItem
{
  protected PGPListViewItem() { }
  protected PGPListViewItem(string text) : base(text) { }

  public abstract PrimaryKey PublicKey
  {
    get;
  }
}
#endregion

#region AttributeItem
public class AttributeItem : PGPListViewItem
{
  public AttributeItem(UserAttribute attr) : this(attr, string.Empty) { }

  public AttributeItem(UserAttribute attr, string text)
    : base(text)
  {
    if(attr == null) throw new ArgumentNullException();
    this.attr = attr;
    this.Name = attr.Id == null ? null : attr.Id;
  }

  public UserAttribute Attribute
  {
    get { return attr; }
  }

  public override PrimaryKey PublicKey
  {
    get { return attr.PrimaryKey; }
  }

  readonly UserAttribute attr;
}
#endregion

#region KeySignatureItem
public class KeySignatureItem : PGPListViewItem
{
  public KeySignatureItem(KeySignature sig) : this(sig, string.Empty) { }

  public KeySignatureItem(KeySignature sig, string text) : base(text)
  {
    if(sig == null) throw new ArgumentNullException();
    this.sig = sig;
  }

  public override PrimaryKey PublicKey
  {
    get { return sig.Object.PrimaryKey; }
  }

  public KeySignature Signature
  {
    get { return sig; }
  }

  readonly KeySignature sig;
}
#endregion

#region PrimaryKeyItem
public class PrimaryKeyItem : PGPListViewItem
{
  public PrimaryKeyItem(PrimaryKey publicKey) : this(new KeyPair(publicKey, null), null) { }

  public PrimaryKeyItem(KeyPair keyPair) : this(keyPair, null) { }

  public PrimaryKeyItem(KeyPair keyPair, string text) : base(text)
  {
    if(keyPair == null) throw new ArgumentNullException();
    this.keyPair = keyPair;
    this.Name    = keyPair.PublicKey.EffectiveId;
  }

  public bool Expanded
  {
    get { return expanded; }
  }

  public bool HasRelatedItems
  {
    get { return relatedItems != null; }
  }

  public KeyPair KeyPair
  {
    get { return keyPair; }
  }

  public override PrimaryKey PublicKey
  {
    get { return KeyPair.PublicKey; }
  }

  public PrimaryKey SecretKey
  {
    get { return KeyPair.SecretKey; }
  }

  public ListViewItem[] GetRelatedItems()
  {
    return relatedItems == null ? new ListViewItem[0] : (ListViewItem[])relatedItems.Clone();
  }

  internal ListViewItem[] relatedItems;
  internal bool expanded;
  readonly KeyPair keyPair;
}
#endregion

#region SubkeyItem
public class SubkeyItem : PGPListViewItem
{
  public SubkeyItem(Subkey subkey) : this(subkey, string.Empty) { }

  public SubkeyItem(Subkey subkey, string text) : base(text)
  {
    if(subkey == null) throw new ArgumentNullException();
    this.subkey = subkey;
    this.Name   = subkey.EffectiveId;
  }

  public override PrimaryKey PublicKey
  {
    get { return subkey.PrimaryKey; }
  }

  public Subkey Subkey
  {
    get { return subkey; }
  }

  readonly Subkey subkey;
}
#endregion

} // namespace AdamMil.Security.UI