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

public partial class UserRevocationForm : Form
{
  public UserRevocationForm()
  {
    InitializeComponent();
  }

  [Browsable(false)]
  public ListBox.ObjectCollection UserIdList
  {
    get { return ids.Items; }
  }

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

  protected override void OnShown(EventArgs e)
  {
    base.OnShown(e);
    rbNoReason.Focus();
  }
}

} // namespace AdamMil.Security.UI