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

// TODO: create a SecureTextBox and use it

public partial class PasswordForm : Form
{
  public PasswordForm()
  {
    InitializeComponent();
  }

  public string DescriptionText
  {
    get { return lblDescription.Text; }
    set { lblDescription.Text = value; }
  }

  public bool EnableRememberPassword
  {
    get { return chkRemember.Enabled; }
    set { chkRemember.Enabled = value; }
  }

  public bool RememberPassword
  {
    get { return chkRemember.Enabled && chkRemember.Checked; }
  }

  public string RememberText
  {
    get { return chkRemember.Text; }
    set { chkRemember.Text = value; }
  }

  public unsafe SecureString GetPassword()
  {
    // TODO: obviously, this isn't secure at all. that'll be fixed later.
    fixed(char* chars = txtPassword.Text.ToCharArray())
    {
      return new SecureString(chars, txtPassword.Text.Length);
    }
  }
}

} // namespace AdamMil.Security.UI