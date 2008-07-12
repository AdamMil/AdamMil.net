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

public partial class KeyRevocationForm : Form
{
  public KeyRevocationForm()
  {
    InitializeComponent();
  }

  public KeyRevocationForm(PrimaryKey key, PrimaryKey[] ownedKeys) : this()
  {
    Initialize(key, ownedKeys);
  }

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

  [Browsable(false)]
  public bool RevokeDirectly
  {
    get { return rbDirect.Checked; }
  }

  [Browsable(false)]
  public PrimaryKey SelectedRevokingKey
  {
    get { return ((KeyItem)revokingKeys.SelectedItem).Value; }
  }

  public void Initialize(PrimaryKey keyToRevoke, PrimaryKey[] ownedKeys)
  {
    if(keyToRevoke == null || ownedKeys == null) throw new ArgumentNullException();

    rbDirect.Enabled = Array.IndexOf(ownedKeys, keyToRevoke) != -1;

    revokingKeys.Items.Clear();
    foreach(PrimaryKey key in ownedKeys)
    {
      if(key == null) throw new ArgumentException("A key was null.");
      if(keyToRevoke.DesignatedRevokers.Contains(key.Fingerprint))
      {
        revokingKeys.Items.Add(new KeyItem(key));
      }
    }

    rbIndirect.Enabled = revokingKeys.Items.Count != 0;
    if(revokingKeys.Items.Count != 0) revokingKeys.SelectedIndex = 0;

    if(rbDirect.Enabled) rbDirect.Checked = true;
    else if(rbIndirect.Enabled) rbIndirect.Checked = true;
    else rbDirect.Checked = rbIndirect.Checked = false;

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