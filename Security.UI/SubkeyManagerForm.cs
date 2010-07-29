/*
AdamMil.Security.UI is a .NET library providing common user interface widgets
for OpenPGP-based software.
Copyright (C) 2008-2010 Adam Milazzo (http://www.adammil.net/)

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
using System.Windows.Forms;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

/// <summary>This form helps the user manage his subkeys.</summary>
public partial class SubkeyManagerForm : Form
{
  /// <summary>Creates a new <see cref="SubkeyManagerForm"/>. You must call <see cref="Initialize"/> to initialize the
  /// form before displaying it.
  /// </summary>
  public SubkeyManagerForm()
  {
    InitializeComponent();
  }

  /// <summary>Initializes a new <see cref="SubkeyManagerForm"/> with the <see cref="PGPSystem"/> that will be used to
  /// edit the key, and the key to edit.
  /// </summary>
  public SubkeyManagerForm(PGPSystem pgp, PrimaryKey publicKey) : this()
  {
    Initialize(pgp, publicKey);
  }

  /// <summary>Initializes this form with the <see cref="PGPSystem"/> that will be used to edit the key, and the key to
  /// edit.
  /// </summary>
  public void Initialize(PGPSystem pgp, PrimaryKey publicKey)
  {
    if(pgp == null || publicKey == null) throw new ArgumentNullException();
    this.pgp = pgp;
    this.key = publicKey;

    btnAdd.Enabled = key.HasSecretKey;

    if(Visible) ReloadKey();
  }

  /// <include file="documentation.xml" path="/UI/Common/OnKeyDown/*"/>
  protected override void OnKeyDown(KeyEventArgs e)
  {
    base.OnKeyDown(e);

    if(!e.Handled && PGPUI.IsCloseKey(e))
    {
      Close();
      e.Handled = true;
    }
  }

  /// <include file="documentation.xml" path="/UI/Common/OnShown/*"/>
  protected override void OnShown(EventArgs e)
  {
    base.OnShown(e);
    ReloadKey(); // for some reason, if we do this earlier, then the ListView won't render the fonts
  }              // correctly until the form is redrawn...

  /// <summary>Gets the <see cref="Subkey"/> objects selected by the user.</summary>
  Subkey[] GetSelectedKeys()
  {
    Subkey[] keys = new Subkey[subkeys.SelectedItems.Count];
    for(int i=0; i<keys.Length; i++) keys[i] = ((SubkeyItem)subkeys.SelectedItems[i]).Subkey;
    return keys;
  }

  /// <summary>Reloads the key and redisplays its subkeys.</summary>
  void ReloadKey()
  {
    if(key != null)
    {
      key = pgp.RefreshKey(key);

      if(key == null)
      {
        foreach(Control control in Controls)
        {
          if(control != lblDescription) control.Enabled = false;
        }
        lblDescription.Text = "The key you were editing no longer exists.";
      }
      else
      {
        subkeys.Items.Clear();
        foreach(Subkey subkey in key.Subkeys) subkeys.AddKey(subkey);
      }
    }
  }

  void btnAdd_Click(object sender, EventArgs e)
  {
    NewSubkeyForm form = new NewSubkeyForm(pgp);
    if(form.ShowDialog() == DialogResult.OK)
    {
      ProgressForm progress = new ProgressForm("Generating Subkey",
        "Please wait while the subkey is generated. This may take several minutes. Actively typing and performing "+
        "disk-intensive operations can help speed up the process.",
        delegate { pgp.AddSubkey(key, form.KeyType, form.Capabilities, form.KeyLength, form.Expiration); });
      if(progress.ShowDialog() == DialogResult.Abort) progress.ThrowException();
      ReloadKey();
    }
  }

  void btnDelete_Click(object sender, EventArgs e)
  {
    if(subkeys.SelectedItems.Count != 0)
    {
      Subkey[] selectedKeys = GetSelectedKeys();

      // inform the user that deleting published subkeys is pointless, and get confirmation
      bool onlyOne = selectedKeys.Length == 1;
      string deleting = onlyOne ? "a subkey" : "multiple subkeys";
      string subkey = (onlyOne ? "this subkey" : "these subkeys");
      string s = (onlyOne ? null : "s"), them = (onlyOne ? "it" : "them"), they = (onlyOne ? "it" : "they");
      if(MessageBox.Show("You are about to delete " + deleting + "! Note that you cannot retract a subkey once it "+
                         "has been distributed, so if this key (with " + subkey + ") has ever been given to another "+
                         "person or uploaded to a public key server, you should revoke the subkey" + s + " instead "+
                         "of deleting " + them + ", because " + they + " would only be deleted from your machine, "+
                         "and could reappear in the future.\n\nAre you sure you want to delete " + subkey + "?",
                         "Delete user IDs?",
                         MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2)
          == DialogResult.Yes)
      {
        try { pgp.DeleteSubkeys(selectedKeys); }
        catch(OperationCanceledException) { }
        ReloadKey();
      }
    }
  }

  void btnRevoke_Click(object sender, EventArgs e)
  {
    if(subkeys.SelectedItems.Count != 0)
    {
      Subkey[] selectedKeys = GetSelectedKeys();
      KeyRevocationForm form = new KeyRevocationForm(selectedKeys);
      if(form.ShowDialog() == DialogResult.OK)
      {
        try { pgp.RevokeSubkeys(form.Reason, selectedKeys); }
        catch(OperationCanceledException) { }
        ReloadKey();
      }
    }
  }

  void subkeys_SelectedIndexChanged(object sender, EventArgs e)
  {
    btnDelete.Enabled = btnRevoke.Enabled = key.HasSecretKey && subkeys.SelectedItems.Count != 0;
  }

  PGPSystem pgp;
  PrimaryKey key;
}

} // namespace AdamMil.Security.UI