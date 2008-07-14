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
using System.ComponentModel;
using System.Windows.Forms;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

/// <summary>This form helps the user revoke a key. The form does not actually revoke the key, but merely gathers the
/// information needed to do so. The form is meant to be displayed as a modal dialog.
/// </summary>
public partial class KeyRevocationForm : Form
{
  /// <summary>Creates a new <see cref="KeyRevocationForm"/>. You should call <see cref="Initialize"/> to initialize
  /// the form.
  /// </summary>
  public KeyRevocationForm()
  {
    InitializeComponent();
  }

  /// <summary>Initializes a new <see cref="KeyRevocationForm"/> with the given key to revoke and a list of keys for
  /// which the user has the secret portion.
  /// </summary>
  public KeyRevocationForm(PrimaryKey keyToRevoke, PrimaryKey[] ownedKeys) : this()
  {
    Initialize(keyToRevoke, ownedKeys);
  }

  /// <summary>Gets the <see cref="KeyRevocationReason"/> entered by the user.</summary>
  [Browsable(false)]
  public KeyRevocationReason Reason
  {
    get
    {
      KeyRevocationCode code;
      if(rbSuperceded.Checked) code = KeyRevocationCode.KeySuperceded;
      else if(rbCompromised.Checked) code = KeyRevocationCode.KeyCompromised;
      else if(rbRetired.Checked) code = KeyRevocationCode.KeyRetired;
      else code = KeyRevocationCode.Unspecified;

      return new KeyRevocationReason(code, txtExplanation.Text.Trim());
    }
  }

  /// <summary>Gets whether the key should be revoked directly. If true, the user is assumed to own the key and wants
  /// to revoke it directly. If false, the user will be revoking the key as a designated revoker, using the
  /// <see cref="SelectedRevokingKey"/>.
  /// </summary>
  [Browsable(false)]
  public bool RevokeDirectly
  {
    get { return rbDirect.Checked; }
  }

  /// <summary>Gets the designated revoking key that the user will use to perform the revocation. This value will be
  /// null if the key is to be revoked directly.
  /// </summary>
  [Browsable(false)]
  public PrimaryKey SelectedRevokingKey
  {
    get { return rbDirect.Checked ? null : ((KeyItem)revokingKeys.SelectedItem).Value; }
  }

  /// <summary>Initializes this form with the given key to revoke and a list of keys for which the user has the secret
  /// portion.
  /// </summary>
  public void Initialize(PrimaryKey keyToRevoke, PrimaryKey[] ownedKeys)
  {
    if(keyToRevoke == null || ownedKeys == null) throw new ArgumentNullException();

    // the user can revoke the key directly if the key to revoke is one that he owns
    rbDirect.Enabled = Array.IndexOf(ownedKeys, keyToRevoke) != -1;

    // of the keys the user owns, find the ones that are designated revokers of the key to revoke
    revokingKeys.Items.Clear();
    foreach(PrimaryKey key in ownedKeys)
    {
      if(key == null) throw new ArgumentException("A key was null.");
      if(keyToRevoke.DesignatedRevokers.Contains(key.Fingerprint))
      {
        revokingKeys.Items.Add(new KeyItem(key));
      }
    }

    // the user can revoke the key indirectly if he owns any designated revokers
    rbIndirect.Enabled = revokingKeys.Items.Count != 0;
    if(revokingKeys.Items.Count != 0) revokingKeys.SelectedIndex = 0; // and if he does, select the first one

    // check the first revocation method that's available
    if(rbDirect.Enabled) rbDirect.Checked = true;
    else if(rbIndirect.Enabled) rbIndirect.Checked = true;
    else rbDirect.Checked = rbIndirect.Checked = false;

    // the user can only revoke the key if he has some way to do so
    btnOK.Enabled = rbDirect.Enabled || rbIndirect.Enabled;

    lblKey.Text = btnOK.Enabled
      ? "You are about to revoke this key:\n" + PGPUI.GetKeyName(keyToRevoke)
      : "You cannot revoke the key because you do not have its secret key or the secret key of a designated revoker.";
  }

  void rbIndirect_CheckedChanged(object sender, EventArgs e)
  {
    revokingKeys.Enabled = ((RadioButton)sender).Checked;
  }
}

} // namespace AdamMil.Security.UI