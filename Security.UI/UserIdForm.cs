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
using System.Text.RegularExpressions;
using System.Windows.Forms;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

public partial class UserIdForm : Form
{
  public UserIdForm()
  {
    InitializeComponent();
    UpdateHelpLabel();
  }

  public string Comment
  {
    get { return txtComment.Text.Trim(); }
    set { txtComment.Text = value; }
  }

  public string Email
  {
    get { return txtEmail.Text.Trim(); }
    set { txtEmail.Text = value; }
  }

  public bool MakePrimary
  {
    get { return chkPrimary.Checked; }
    set { chkPrimary.Checked = value; }
  }

  public string RealName
  {
    get { return txtName.Text.Trim(); }
    set { txtName.Text = value; }
  }

  public UserPreferences Preferences
  {
    get { return null; } // TODO: implement user preferences
  }

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
    if(string.IsNullOrEmpty(RealName))
    {
      MessageBox.Show("You must enter your name.", "Name required", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    else if(!string.IsNullOrEmpty(Email) && !IsValidEmail(Email))
    {
      MessageBox.Show(Email + " is not a valid email address.", "Invalid email",
                      MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    else
    {
      DialogResult = DialogResult.OK;
    }
  }

  void txtEmail_TextChanged(object sender, EventArgs e)
  {
    UpdateHelpLabel();
  }

  static bool IsValidEmail(string email)
  {
    string[] parts = email.Split('@');
    if(parts.Length != 2) return false;

    string local = parts[0], domain = parts[1];
    
    // if the local portion is quoted, strip off the quotes
    if(local.Length > 2 && local[0] == '"' && local[local.Length-1] == '"') local = local.Substring(1, local.Length-2);

    return emailLocalRe.IsMatch(local) && domainRe.IsMatch(domain);
  }

  static readonly Regex emailLocalRe = new Regex(@"^[\w\d!#$%/?|^{}`~&'+=-]+(?:\.[\w\d!#$%/?|^{}`~&'+=-])*$",
                                                 RegexOptions.ECMAScript);
  // matches domain name or IP address in brackets
  static readonly Regex domainRe = new Regex(@"^(?:[a-zA-Z\d]+(?:[\.\-][a-zA-Z\d]+)*|\[\d{1,3}(?:\.\d{1,3}){3}])$",
                                             RegexOptions.ECMAScript);
}

} // namespace AdamMil.Security.UI