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

public partial class OwnerTrustForm : Form
{
  public OwnerTrustForm()
  {
    InitializeComponent();

    rbDontKnow.Tag  = TrustLevel.Unknown;
    rbDontTrust.Tag = TrustLevel.Never;
    rbCasual.Tag    = TrustLevel.Marginal;
    rbFull.Tag      = TrustLevel.Full;
    rbUltimate.Tag  = TrustLevel.Ultimate;
  }

  [Browsable(false)]
  public TrustLevel TrustLevel
  {
    get
    {
      if(rbDontTrust.Checked) return TrustLevel.Never;
      else if(rbCasual.Checked) return TrustLevel.Marginal;
      else if(rbFull.Checked) return TrustLevel.Full;
      else if(rbUltimate.Checked) return TrustLevel.Ultimate;
      else return TrustLevel.Unknown;
    }
    set
    {
      RadioButton button;
      switch(value)
      {
        case TrustLevel.Never: button = rbDontTrust; break;
        case TrustLevel.Marginal: button = rbCasual; break;
        case TrustLevel.Full: button = rbFull; break;
        case TrustLevel.Ultimate: button = rbUltimate; break;
        default: button = rbDontKnow; break;
      }
      button.Checked = true;
      button.Focus();
    }
  }

  [Browsable(false)]
  public ListBox.ObjectCollection KeyList
  {
    get { return trustedKeys.Items; }
  }

  protected override void OnShown(EventArgs e)
  {
    base.OnShown(e);
    TrustLevel = TrustLevel; // invoke the setter to focus the radio button
  }
}

} // namespace AdamMil.Security.UI