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
using System.Security;
using System.Windows.Forms;

namespace AdamMil.Security.UI
{

public partial class ChangePasswordForm : Form
{
  public ChangePasswordForm()
  {
    InitializeComponent();
    UpdatePasswordStrength();
  }

  public SecureString GetPassword()
  {
    return pass1.GetText();
  }

  void btnOK_Click(object sender, EventArgs e)
  {
    if(!PGPUI.ArePasswordsEqual(pass1, pass2))
    {
      MessageBox.Show("The passwords you have entered do not match.", "Password mismatch", MessageBoxButtons.OK,
                      MessageBoxIcon.Error);
      return;
    }
    else if(pass1.TextLength == 0)
    {
      if(MessageBox.Show("You didn't enter a password! This is extremely insecure, as anybody can use your key. Are "+
                         "you sure you don't want a password?", "Password is blank!", MessageBoxButtons.YesNo,
                         MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
      {
        return;
      }
    }
    else if(pass1.GetPasswordStrength() < PasswordStrength.Moderate)
    {
      if(MessageBox.Show("You entered a weak password! This is not secure, as your password can be cracked in a "+
                         "relatively short period of time, allowing somebody access to your key. Are you sure you "+
                         "want a to use a weak password?", "Password is weak!", MessageBoxButtons.YesNo,
                         MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
      {
        return;
      }
    }

    DialogResult = DialogResult.OK;
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