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

/// <summary>Displays a list of data signature verification results.</summary>
public class SignatureList : PGPListBase
{
  /// <summary>Creates a new <see cref="SignatureList"/>. You should call <see cref="Initialize"/> to initialize the
  /// list.
  /// </summary>
  public SignatureList()
  {
    InitializeControl();
  }

  /// <summary>Initializes a new <see cref="SignatureList"/> with the signatures to be displayed.</summary>
  public SignatureList(Signature[] sigs) : this()
  {
    Initialize(sigs);
  }

  /// <summary>Initializes this list given the key whose signatures will be displayed.</summary>
  public void Initialize(Signature[] sigs)
  {
    if(sigs == null) throw new ArgumentNullException();

    Items.Clear();

    foreach(Signature sig in sigs)
    {
      SignatureItem item = CreateSignatureItem(sig);
      if(item != null)
      {
        SetFont(item);
        Items.Add(item);
      }
    }
  }

  /// <include file="documentation.xml" path="/UI/ListBase/CreateSignatureItem/*"/>
  protected virtual SignatureItem CreateSignatureItem(Signature sig)
  {
    SignatureItem item = new SignatureItem(sig, sig.SignerName + " (0x" + sig.ShortKeyId + ")");
    item.SubItems.Add(sig.IsInvalid ? "invalid" : !sig.IsValid ? "unverified" : sig.Expired ? "expired" : "valid");
    item.SubItems.Add(sig.CreationTime.ToShortDateString());
    item.SubItems.Add(sig.Expiration.HasValue ? sig.Expiration.Value.ToShortDateString() : "n/a");
    return item;
  }

  /// <include file="documentation.xml" path="/UI/ListBase/SetSignatureItemFont/*"/>
  protected virtual void SetFont(SignatureItem item)
  {
    if(item.Signature.IsInvalid)
    {
      item.ForeColor = Color.Red;
    }
    else if(item.Signature.Expired)
    {
      item.ForeColor = SystemColors.GrayText;
    }
    else if(!item.Signature.IsValid)
    {
      item.ForeColor = Color.FromArgb(255, 96, 96);
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

  void InitializeControl()
  {
    ColumnHeader userIdHeader, validityHeader, createdHeader, expiresHeader;

    userIdHeader = new ColumnHeader();
    userIdHeader.Text = "User ID";
    userIdHeader.Width = 325;

    validityHeader = new ColumnHeader();
    validityHeader.Text = "Validity";
    validityHeader.Width = 75;

    createdHeader = new ColumnHeader();
    createdHeader.Text = "Created";
    createdHeader.Width = 75;

    expiresHeader = new ColumnHeader();
    expiresHeader.Text = "Expires";
    expiresHeader.Width = 75;

    Columns.AddRange(new ColumnHeader[] { userIdHeader, validityHeader, createdHeader, expiresHeader });
  }
}

} // namespace AdamMil.Security.UI