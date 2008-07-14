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

/// <summary>This form displays basic properties about a key pair.</summary>
public partial class KeyPropertiesForm : Form
{
  /// <summary>Creates a new <see cref="KeyPropertiesForm"/>. You should call <see cref="Initialize"/> to initialize
  /// the form.
  /// </summary>
  public KeyPropertiesForm()
  {
    InitializeComponent();
  }

  /// <summary>Initializes a new <see cref="KeyPropertiesForm"/> with the given key pair.</summary>
  public KeyPropertiesForm(KeyPair pair) : this()
  {
    Initialize(pair);
  }

  /// <summary>Initializes this form with the given key pair.</summary>
  public void Initialize(KeyPair pair)
  {
    if(pair == null) throw new ArgumentNullException();

    txtPrimaryId.Text   = pair.PublicKey.PrimaryUserId.Name;
    txtKeyId.Text       = pair.PublicKey.ShortKeyId +
                          (pair.PublicKey.KeyId.Length > 8 ? " (long ID: " + pair.PublicKey.KeyId + ")" : null);
    txtKeyType.Text     = (pair.SecretKey == null ? "public key" : "public and secret key pair");
    txtKeyValidity.Text = PGPUI.GetKeyValidityDescription(pair.PublicKey);
    txtOwnerTrust.Text  = PGPUI.GetTrustDescription(pair.PublicKey.OwnerTrust);
    txtFingerprint.Text = pair.PublicKey.Fingerprint;

    List<string> capabilities = new List<string>();
    if(pair.PublicKey.HasCapability(KeyCapabilities.Encrypt)) capabilities.Add("encrypt");
    if(pair.PublicKey.HasCapability(KeyCapabilities.Sign)) capabilities.Add("sign");
    if(pair.PublicKey.HasCapability(KeyCapabilities.Certify)) capabilities.Add("certify");
    if(pair.PublicKey.HasCapability(KeyCapabilities.Authenticate)) capabilities.Add("authenticate");
    txtCapabilities.Text = capabilities.Count == 0 ? "none" : string.Join(", ", capabilities.ToArray());

    keyList.Items.Clear();
    keyList.AddKey(pair.PublicKey);
    foreach(Subkey subkey in pair.PublicKey.Subkeys) keyList.AddKey(subkey);
  }

  /// <include file="documentation.xml" path="/UI/Common/OnKeyDown/*"/>
  protected override void OnKeyDown(KeyEventArgs e)
  {
    base.OnKeyDown(e);

    if(!e.Handled && PGPUI.IsCloseKey(e)) // let the form be closed by pressing escape
    {
      Close();
      e.Handled = true;
    }
  }

  /// <include file="documentation.xml" path="/UI/Common/OnShown/*"/>
  protected override void OnShown(EventArgs e)
  {
    base.OnShown(e);
    txtPrimaryId.Focus();       // put the cursor in the primary user ID box
    txtPrimaryId.DeselectAll(); // but focusing a text box selects the text. we don't want that
  }
}

} // namespace AdamMil.Security.UI