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
using System.ComponentModel;
using System.Windows.Forms;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

/// <summary>This form helps the user enter a new user ID.</summary>
public partial class UserIdForm : Form
{
  /// <summary>Initializes a new <see cref="UserIdForm"/>.</summary>
  public UserIdForm()
  {
    InitializeComponent();
    UpdateHelpLabel();
  }

  /// <summary>Gets the optional user ID comment entered by the user.</summary>
  [Browsable(false)]
  public string Comment
  {
    get { return txtComment.Text.Trim(); }
  }

  /// <summary>Gets the optional email address entered by the user.</summary>
  [Browsable(false)]
  public string Email
  {
    get { return txtEmail.Text.Trim(); }
  }

  /// <summary>Gets whether the user ID should be made the key's primary user ID.</summary>
  [Browsable(false)]
  public bool MakePrimary
  {
    get { return chkPrimary.Checked; }
  }

  /// <summary>Gets the name entered by the user.</summary>
  [Browsable(false)]
  public string RealName
  {
    get { return txtName.Text.Trim(); }
  }

  /// <summary>Gets the <see cref="UserPreferences"/> entered by the user, or null to use the default preferences.</summary>
  [Browsable(false)]
  public UserPreferences Preferences
  {
    get { return null; } // TODO: implement user preferences
  }

  /// <summary>Updates the label that shows the user what his user ID will look like.</summary>
  void UpdateHelpLabel()
  {
    string realName = RealName;

    if(string.IsNullOrEmpty(realName))
    {
      lblHelp.Text = "Please enter your new user ID above.";
    }
    else
    {
      string email = Email, comment = Comment;
      lblHelp.Text = "Your user ID will be displayed as:\n" + realName +
                     (!string.IsNullOrEmpty(comment) ? " ("+comment+")" : null) +
                     (!string.IsNullOrEmpty(email) ? " <" + email + ">" : null);
    }
  }

  void btnOK_Click(object sender, EventArgs e)
  {
    if(PGPUI.ValidateUserId(RealName, Email, Comment)) DialogResult = DialogResult.OK;
  }

  void txtUserId_TextChanged(object sender, EventArgs e)
  {
    UpdateHelpLabel();
  }
}

} // namespace AdamMil.Security.UI