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

public partial class KeySigningForm : Form
{
  public KeySigningForm()
  {
    InitializeComponent();
  }

  [Browsable(false)]
  public CertificationLevel CertificationLevel
  {
    get
    {
      if(rbNone.Checked) return CertificationLevel.None;
      else if(rbCasual.Checked) return CertificationLevel.Casual;
      else if(rbRigorous.Checked) return CertificationLevel.Rigorous;
      else return CertificationLevel.Undisclosed;
    }
    set
    {
      RadioButton button;
      switch(value)
      {
        case CertificationLevel.None: button = rbNone; break;
        case CertificationLevel.Casual: button = rbCasual; break;
        case CertificationLevel.Rigorous: button = rbRigorous; break;
        default: button = rbNoAnswer; break;
      }

      // if no verification was performed, mark the signature as local only
      if(button == rbNoAnswer || button == rbNone) LocalOnly = true;

      button.Checked = true;
      button.Focus();
    }
  }

  [Browsable(false)]
  public bool LocalOnly
  {
    get { return chkLocal.Checked; }
    set { chkLocal.Checked = value; }
  }

  [Browsable(false)]
  public KeySigningOptions KeySigningOptions
  {
    get { return new KeySigningOptions(CertificationLevel, !LocalOnly); }
  }

  [Browsable(false)]
  public ListBox.ObjectCollection SignedKeys
  {
    get { return signedKeys.Items; }
  }

  [Browsable(false)]
  public ComboBox.ObjectCollection SigningKeys
  {
    get { return signingKeys.Items; }
  }

  [Browsable(false)]
  public int SelectedSigningKey
  {
    get { return signingKeys.SelectedIndex; }
  }

  protected override void OnShown(EventArgs e)
  {
    base.OnShown(e);
    if(signingKeys.SelectedIndex == -1) signingKeys.SelectedIndex = 0;
  }

  void rbPoor_CheckedChanged(object sender, EventArgs e)
  {
    if(((RadioButton)sender).Checked) LocalOnly = true;
  }
}

} // namespace AdamMil.Security.UI