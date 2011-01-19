/*
AdamMil.Security.UI is a .NET library providing common user interface widgets
for OpenPGP-based software.
Copyright (C) 2008-2011 Adam Milazzo (http://www.adammil.net/)

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

/// <summary>This form helps the user select recipients for encryption. It is meant to be dislayed as a modal dialog.</summary>
public partial class RecipientSearchForm : Form
{
  /// <summary>Creates a new <see cref="RecipientSearchForm"/>. You must call <see cref="Initialize"/> to initialize
  /// the form.
  /// </summary>
  public RecipientSearchForm()
  {
    InitializeComponent();
  }

  /// <summary>Initializes a new <see cref="RecipientSearchForm"/> with the given <see cref="PGPSystem"/> and the
  /// default keyring.
  /// </summary>
  public RecipientSearchForm(PGPSystem pgp) : this(pgp, null) { }

  /// <summary>Initializes a new <see cref="RecipientSearchForm"/> with the given <see cref="PGPSystem"/> and keyring.</summary>
  public RecipientSearchForm(PGPSystem pgp, Keyring keyring) : this()
  {
    Initialize(pgp, keyring);
  }

  /// <summary>Gets the recipients selected by the user.</summary>
  public PrimaryKey[] GetSelectedRecipients()
  {
    return recipients.GetSelectedRecipients();
  }

  /// <summary>Initializes this form with the given <see cref="PGPSystem"/> and keyring.</summary>
  public void Initialize(PGPSystem pgp, Keyring keyring)
  {
    if(pgp == null) throw new ArgumentNullException();
    recipients.ShowKeyring(pgp, keyring);
  }

  void btnClear_Click(object sender, EventArgs e)
  {
    txtSearch.Text = string.Empty; // only enable the "Clear" button when there's something to clear
  }

  void txtSearch_TextChanged(object sender, EventArgs e)
  {
    string trimmed = txtSearch.Text.Trim(); // filter the list of recipients as the user types
    btnClear.Enabled = trimmed.Length != 0;
    recipients.FilterItems(trimmed.Length == 0 ?
                             null : trimmed.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
  }

  void recipients_SelectedIndexChanged(object sender, EventArgs e)
  {
    btnOK.Enabled = recipients.SelectedItems.Count != 0;
  }

  void recipients_MouseDoubleClick(object sender, MouseEventArgs e)
  {
    if(e.Button == MouseButtons.Left) // double-clicking a recipient closes the window
    {
      PrimaryKeyItem item = recipients.GetItemAt(e.X, e.Y) as PrimaryKeyItem;
      if(item != null) DialogResult = DialogResult.OK;
    }
  }
}

} // namespace AdamMil.Security.UI