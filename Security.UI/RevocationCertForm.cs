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

public partial class RevocationCertForm : Form
{
  public RevocationCertForm()
  {
    InitializeComponent();
  }

  public RevocationCertForm(PrimaryKey keyToRevoke, PrimaryKey[] ownedKeys) : this()
  {
    Initialize(keyToRevoke, ownedKeys);
  }

  [Browsable(false)]
  public string Filename
  {
    get { return rbFile.Checked ? txtFile.Text : null; }
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

    lblRevokedKey.Text = btnOK.Enabled ? "The certificate will be generated for:\n" + PGPUI.GetKeyName(keyToRevoke)
      : "You cannot generate the certificate because you do not have its secret key or the secret key of a "+
        "designated revoker.";

    this.keyToRevoke = keyToRevoke;
  }

  void btnBrowse_Click(object sender, EventArgs e)
  {
    string defaultFilename = PGPUI.MakeSafeFilename("revoke " + PGPUI.GetKeyName(keyToRevoke) + ".txt");

    SaveFileDialog sfd = new SaveFileDialog();
    sfd.DefaultExt      = ".txt";
    sfd.FileName        = defaultFilename;
    sfd.Filter          = "Text Files (*.txt)|*.txt|ASCII Files (*.asc)|*.asc|All Files (*.*)|*.*";
    sfd.OverwritePrompt = true;
    sfd.Title           = "Save Revocation Certificate";
    sfd.SupportMultiDottedExtensions = true;
    if(sfd.ShowDialog() == DialogResult.OK) txtFile.Text = sfd.FileName;
  }

  void btnOK_Click(object sender, EventArgs e)
  {
    bool badFilename = false;

    if(rbFile.Checked)
    {
      badFilename = txtFile.Text.Trim().Length == 0;

      if(!badFilename)
      {
        try { new System.IO.FileInfo(txtFile.Text); }
        catch { badFilename = true; }
      }
    }

    if(badFilename)
    {
      MessageBox.Show("You have not specified a valid file where the revocation certificate should be saved.",
                      "Missing or invalid filename", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    else
    {
      DialogResult = DialogResult.OK;
    }
  }

  void rbFile_CheckedChanged(object sender, EventArgs e)
  {
    txtFile.Enabled = btnBrowse.Enabled = ((RadioButton)sender).Checked;
  }

  void rbIndirect_CheckedChanged(object sender, EventArgs e)
  {
    revokingKeys.Enabled = ((RadioButton)sender).Checked;
  }

  PrimaryKey keyToRevoke;
}

} // namespace AdamMil.Security.UI