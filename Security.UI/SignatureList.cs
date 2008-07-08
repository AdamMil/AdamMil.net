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

public class SignatureList : PGPListBase
{
  public SignatureList()
  {
    InitializeControl();
  }

  public void ShowSignatures(PrimaryKey publicKey)
  {
    if(publicKey == null) throw new ArgumentNullException();

    Items.Clear();

    foreach(UserId userId in publicKey.UserIds)
    {
      AttributeItem item = CreateAttributeItem(userId);
      if(item != null)
      {
        item.Font = GetBoldFont();
        Items.Add(item);
        AddSignatures(userId.Signatures);
      }
    }

    foreach(UserAttribute attr in publicKey.Attributes)
    {
      AttributeItem item = CreateAttributeItem(attr);
      if(item != null)
      {
        item.Font = GetBoldFont();
        Items.Add(item);
        AddSignatures(attr.Signatures);
      }
    }
  }

  protected override void ClearCachedFonts()
  {
    boldFont = null;
  }

  protected KeySignatureItem CreateSignatureItem(KeySignature sig)
  {
    if(sig == null) throw new ArgumentNullException();

    string name = string.IsNullOrEmpty(sig.SignerName) ? "(User ID not found)" : sig.SignerName;
    KeySignatureItem item = new KeySignatureItem(sig, name);

    item.SubItems.Add(sig.ShortKeyId);
    item.SubItems.Add((sig.Exportable ? "Exportable " : "Local ") + PGPUI.GetSignatureDescription(sig.Type));
    item.SubItems.Add(sig.Expired ? "Expired" : sig.IsValid ? "Valid" : sig.IsInvalid ? "Invalid" :
                      sig.ErrorOccurred ? "Error" : "Unverified");
    item.SubItems.Add(sig.CreationTime.ToShortDateString());
    return item;
  }

  void AddSignatures(IEnumerable<KeySignature> sigs)
  {
    KeySignatureItem item = null;
    foreach(KeySignature sig in sigs)
    {
      item = CreateSignatureItem(sig);
      if(item != null)
      {
        item.Font        = Font;
        item.ImageIndex  = IndentImage;
        item.IndentCount = 1;
        Items.Add(item);
      }
    }

    // change the last item's image to the corner
    if(item != null) item.ImageIndex = CornerImage;
  }

  Font GetBoldFont()
  {
    if(boldFont == null) boldFont = new Font(Font, FontStyle.Bold);
    return boldFont;
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

  Font boldFont;
}

} // namespace AdamMil.Security.UI