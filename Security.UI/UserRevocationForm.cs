/*
AdamMil.Security.UI is a .NET library providing common user interface widgets
for OpenPGP-based software.
Copyright (C) 2008-2013 Adam Milazzo (http://www.adammil.net/)

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

/// <summary>This form helps the user revoke one or more user attributes. The form does not actually perform the
/// revocation, but merely gathers the information needed to do so. The form is meant to be used as a modal dialog.
/// </summary>
public partial class UserRevocationForm : Form
{
  /// <summary>Creates a new <see cref="UserRevocationForm"/>. You must call <see cref="Initialize"/> to initialize
  /// the form.
  /// </summary>
  public UserRevocationForm()
  {
    InitializeComponent();
  }

  /// <summary>Initializes a new <see cref="UserRevocationForm"/> with the list of attributes to be revoked.</summary>
  public UserRevocationForm(UserAttribute[] attrs) : this()
  {
    Initialize(attrs);
  }

  /// <summary>Gets the <see cref="UserRevocationReason"/> entered by the user.</summary>
  [Browsable(false)]
  public UserRevocationReason Reason
  {
    get
    {
      return new UserRevocationReason(
        rbInvalid.Checked ? UserRevocationCode.IdNoLongerValid : UserRevocationCode.Unspecified, txtExplanation.Text);
    }
    set
    {
      RadioButton button;
      button = value != null && value.Reason == UserRevocationCode.IdNoLongerValid ? rbInvalid : rbNoReason;
      button.Checked = true;
      button.Focus();

      txtExplanation.Text = value == null ? string.Empty : value.Explanation;
    }
  }

  /// <summary>Initializes this form with the list of attributes to be revoked.</summary>
  public void Initialize(UserAttribute[] attrs)
  {
    if(attrs == null) throw new ArgumentNullException();
    if(attrs.Length == 0) throw new ArgumentException("No attributes were given.");

    ids.Items.Clear();
    foreach(UserAttribute attr in attrs) ids.Items.Add(PGPUI.GetAttributeName(attr));
  }
}

} // namespace AdamMil.Security.UI