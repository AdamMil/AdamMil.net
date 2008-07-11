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

public partial class KeyRevocationForm : Form
{
  public KeyRevocationForm()
  {
    InitializeComponent();
  }

  [Browsable(false)]
  public KeyRevocationReason Reason
  {
    get
    {
      KeyRevocationCode code;
      if(rbSuperceded.Checked) code = KeyRevocationCode.KeySuperceded;
      else if(rbCompromised.Checked) code = KeyRevocationCode.KeyCompromised;
      else if(rbRetired.Checked) code = KeyRevocationCode.KeyRetired;
      else code = KeyRevocationCode.Unspecified;

      return new KeyRevocationReason(code, txtExplanation.Text.Trim());
    }
  }

  public KeyRevocationForm(PrimaryKey key) : this()
  {
    Initialize(key);
  }

  public void Initialize(PrimaryKey key)
  {
    if(key == null) throw new ArgumentNullException();
    lblKey.Text = "You are about to revoke this key:\n" + PGPUI.GetKeyName(key);
  }
}

} // namespace AdamMil.Security.UI