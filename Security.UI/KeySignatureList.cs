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

/// <summary>Displays a list of signatures on a key's user IDs.</summary>
public class KeySignatureList : PGPListBase
{
  /// <summary>Creates a new <see cref="KeySignatureList"/>. You should call <see cref="Initialize"/> to initialize the
  /// list.
  /// </summary>
  public KeySignatureList()
  {
    InitializeControl();
  }

  /// <summary>Initializes a new <see cref="KeySignatureList"/> given the key whose signatures will be displayed.</summary>
  public KeySignatureList(PrimaryKey publicKey) : this()
  {
    Initialize(publicKey);
  }

  /// <summary>Initializes this list given the key whose signatures will be displayed.</summary>
  public void Initialize(PrimaryKey publicKey)
  {
    if(publicKey == null) throw new ArgumentNullException();

    Items.Clear();

    foreach(UserId userId in publicKey.UserIds)
    {
      AttributeItem item = CreateAttributeItem(userId);
      if(item != null)
      {
        SetFont(item);
        Items.Add(item);
        AddSignatures(userId.Signatures);
      }
    }

    foreach(UserAttribute attr in publicKey.Attributes)
    {
      AttributeItem item = CreateAttributeItem(attr);
      if(item != null)
      {
        SetFont(item);
        Items.Add(item);
        AddSignatures(attr.Signatures);
      }
    }
  }

  /// <include file="documentation.xml" path="/UI/ListBase/ClearCachedFonts/*"/>
  protected override void ClearCachedFonts()
  {
    userIdFont = revokedIdFont = null;
  }

  /// <include file="documentation.xml" path="/UI/ListBase/CreateAttributeItem/*"/>
  protected override AttributeItem CreateAttributeItem(UserAttribute attr)
  {
    AttributeItem item = base.CreateAttributeItem(attr);
    if(attr.Revoked) item.Text += " (revoked)";
    return item;
  }

  /// <include file="documentation.xml" path="/UI/ListBase/CreateSignatureItem/*"/>
  protected virtual KeySignatureItem CreateSignatureItem(KeySignature sig)
  {
    if(sig == null) throw new ArgumentNullException();

    string name = string.IsNullOrEmpty(sig.SignerName) ? "(User ID not found)" : sig.SignerName;

    if(sig.Revocation && sig.SelfSignature) name += " (self revocation)";
    else if(sig.SelfSignature) name += " (self signature)";
    else if(sig.Revocation) name += " (revocation)";

    KeySignatureItem item = new KeySignatureItem(sig, name);

    item.SubItems.Add(sig.ShortKeyId);
    item.SubItems.Add((sig.Exportable ? "Exportable " : "Local ") + PGPUI.GetSignatureDescription(sig.Type));
    item.SubItems.Add(sig.Expired ? "Expired" : sig.IsValid ? "Valid" : sig.IsInvalid ? "Invalid" :
                      sig.ErrorOccurred ? "Error" : "Unverified");
    item.SubItems.Add(sig.CreationTime.ToShortDateString());

    return item;
  }

  /// <include file="documentation.xml" path="/UI/ListBase/RecreateItems/*"/>
  protected override void RecreateItems()
  {
    if(Items.Count != 0) Initialize(((AttributeItem)Items[0]).PublicKey);
  }

  /// <include file="documentation.xml" path="/UI/ListBase/SetAttributeItemFont/*"/>
  protected virtual void SetFont(AttributeItem item)
  {
    if(item.Attribute.Revoked)
    {
      if(revokedIdFont == null) revokedIdFont = new Font(Font, FontStyle.Italic | FontStyle.Bold);
      item.Font      = revokedIdFont;
      item.ForeColor = SystemColors.GrayText;
    }
    else
    {
      if(userIdFont == null) userIdFont = new Font(Font, FontStyle.Bold);
      item.Font = userIdFont;
    }
  }

  /// <include file="documentation.xml" path="/UI/ListBase/SetKeySignatureItemFont/*"/>
  protected virtual void SetFont(KeySignatureItem item)
  {
    if(item.Signature.IsInvalid) // if the signature failed verification, it may indicate a security problem
    {
      item.ForeColor = Color.FromArgb(255, 96, 0); // so make it colorful
    }
    else if(item.Signature.Revocation && !item.Signature.SelfSignature) // if the signature says this key is NOT owned
    {                                                                   // by the real user
      // we want to make it striking, but if the signature is of unknown validity, it shouldn't be too striking
      item.ForeColor = item.Signature.IsValid ? Color.Red : Color.FromArgb(255, 96, 96);
    }
    else if(!item.Signature.IsValid) // if the signature could not be verified, then gray it out
    {
      item.ForeColor = SystemColors.GrayText;
    }
  }

  /// <summary>Adds the given signatures to the list.</summary>
  void AddSignatures(IEnumerable<KeySignature> sigs)
  {
    bool hadItem = false;
    foreach(KeySignature sig in sigs)
    {
      KeySignatureItem item = CreateSignatureItem(sig);
      if(item != null)
      {
        SetFont(item);
        item.ImageIndex  = IndentImage;
        item.IndentCount = 1;
        Items.Add(item);
        hadItem = true;
      }
    }

    if(hadItem) // change the last item's image to the corner, if we added an item
    {
      Items[Items.Count-1].ImageIndex = CornerImage;
    }
    else // otherwise, add an item that says there are no signatures
    {
      ListViewItem item = new ListViewItem("No signatures");
      item.ImageIndex  = CornerImage;
      item.IndentCount = 1;
      Items.Add(item);
    }
  }

  void InitializeControl()
  {
    ColumnHeader userIdHeader, keyIdHeader, sigTypeHeader, validityHeader, createdHeader;

    userIdHeader = new ColumnHeader();
    userIdHeader.Text = "User ID";
    userIdHeader.Width = 305;

    keyIdHeader = new ColumnHeader();
    keyIdHeader.Text = "Key ID";
    keyIdHeader.Width = 75;

    sigTypeHeader = new ColumnHeader();
    sigTypeHeader.Text = "Signature Type";
    sigTypeHeader.Width = 185;

    validityHeader = new ColumnHeader();
    validityHeader.Text = "Validity";
    validityHeader.Width = 75;

    createdHeader = new ColumnHeader();
    createdHeader.Text = "Created";
    createdHeader.Width = 75;

    Columns.AddRange(new ColumnHeader[] { userIdHeader, keyIdHeader, sigTypeHeader, validityHeader, createdHeader });
  }

  Font userIdFont, revokedIdFont;
}

} // namespace AdamMil.Security.UI