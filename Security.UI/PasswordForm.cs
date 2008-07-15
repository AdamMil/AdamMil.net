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
using System.Security;
using System.Windows.Forms;

namespace AdamMil.Security.UI
{

/// <summary>This form prompts the suer to enter a password.</summary>
public partial class PasswordForm : Form
{
  /// <summary>Initializes a new <see cref="PasswordForm"/>.</summary>
  public PasswordForm()
  {
    InitializeComponent();
  }

  /// <summary>Gets or sets the description of the password that the user needs to enter.</summary>
  public string DescriptionText
  {
    get { return lblDescription.Text; }
    set { lblDescription.Text = value; }
  }

  /// <summary>Gets or sets whether the "Remember my password" checkbox is enabled. The default is true.</summary>
  public bool EnableRememberPassword
  {
    get { return chkRemember.Enabled; }
    set { chkRemember.Enabled = value; }
  }

  /// <summary>Gets or sets whether the "Remember by password" checkbox is checked.</summary>
  [Browsable(false)]
  public bool RememberPassword
  {
    get { return chkRemember.Enabled && chkRemember.Checked; }
    set
    {
      if(!chkRemember.Enabled) throw new InvalidOperationException("EnableRememberPassword is currently false.");
      chkRemember.Checked = value; 
    }
  }

  /// <summary>Gets or sets the text of the "Remember my password" checkbox, in case you want to change it to something
  /// like "Remember my password for 5 minutes". The default is "Remember my password for this session".
  /// </summary>
  public string RememberText
  {
    get { return chkRemember.Text; }
    set { chkRemember.Text = value; }
  }

  /// <summary>Gets the password entered by the user.</summary>
  public SecureString GetPassword()
  {
    return txtPassword.GetText();
  }
}

} // namespace AdamMil.Security.UI