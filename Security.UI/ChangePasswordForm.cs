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
using System.Security;
using System.Windows.Forms;

namespace AdamMil.Security.UI
{

/// <summary>A form that helps a user choose a new password for their secret key. The form is meant to be shown as a
/// modal dialog.
/// </summary>
public partial class ChangePasswordForm : Form
{
  /// <summary>Initializes a new <see cref="ChangePasswordForm"/>.</summary>
  public ChangePasswordForm()
  {
    InitializeComponent();
    UpdatePasswordStrength();
  }

  /// <summary>Retrieves the password entered by the user.</summary>
  public SecureString GetPassword()
  {
    return pass1.GetText();
  }

  void btnOK_Click(object sender, EventArgs e)
  {
    if(PGPUI.ValidateAndCheckPasswords(pass1, pass2)) DialogResult = DialogResult.OK;
  }

  void pass1_TextChanged(object sender, EventArgs e)
  {
    UpdatePasswordStrength();
  }

  void UpdatePasswordStrength()
  {
    lblStrength.Text = "Estimated password strength: " +
                       PGPUI.GetPasswordStrengthDescription(pass1.GetPasswordStrength());
  }
}

} // namespace AdamMil.Security.UI