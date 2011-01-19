/*
AdamMil.Security.UI is a .NET library providing common user interface widgets
for OpenPGP-based software.
Copyright (C) 2008-2011 Adam Milazzo (http://www.adammil.net/)

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

/// <summary>This form helps the user to choose an owner trust level for a set of keys. It is intended to be used as
/// a modal dialog.
/// </summary>
public partial class OwnerTrustForm : Form
{
  /// <summary>Creates a new <see cref="OwnerTrustForm"/>. You must call <see cref="Initialize"/> to initialize the
  /// form.
  /// </summary>
  public OwnerTrustForm()
  {
    InitializeComponent();
  }

  /// <summary>Initializes a new <see cref="OwnerTrustForm"/>, with the list of keys that will have their trust value
  /// edited.
  /// </summary>
  public OwnerTrustForm(PrimaryKey[] keysToTrust) : this()
  {
    Initialize(keysToTrust);
  }

  /// <summary>Gets the <see cref="PGP.TrustLevel"/> selected by the user.</summary>
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
  }

  /// <summary>Initializes this form with the list of keys that will have their trust value edited.</summary>
  public void Initialize(PrimaryKey[] keysToTrust)
  {
    if(keysToTrust == null) throw new ArgumentNullException();
    if(keysToTrust.Length == 0) throw new ArgumentException("No keys were given.");

    trustedKeys.Items.Clear();

    // set the initial trust level to what all the keys agree on, or Unknown if they don't agree, and add the keys
    TrustLevel initialTrustLevel = keysToTrust[0].OwnerTrust;
    foreach(PrimaryKey key in keysToTrust)
    {
      if(key.OwnerTrust != initialTrustLevel) initialTrustLevel = TrustLevel.Unknown;
      trustedKeys.Items.Add(new KeyItem(key));
    }

    RadioButton button;
    switch(initialTrustLevel)
    {
      case TrustLevel.Never: button = rbDontTrust; break;
      case TrustLevel.Marginal: button = rbCasual; break;
      case TrustLevel.Full: button = rbFull; break;
      case TrustLevel.Ultimate: button = rbUltimate; break;
      default: button = rbDontKnow; break;
    }
    button.Checked = true;
  }

  /// <include file="documentation.xml" path="/UI/Common/OnShown/*"/>
  protected override void OnShown(EventArgs e)
  {
    base.OnShown(e);

    // focus the selected trust level button when the form is shown, so it can be changed easily
    RadioButton button;
    if(rbDontTrust.Checked) button = rbDontTrust;
    else if(rbCasual.Checked) button = rbCasual;
    else if(rbFull.Checked) button = rbFull;
    else if(rbUltimate.Checked) button = rbUltimate;
    else button = rbDontKnow;
    button.Focus();
  }
}

} // namespace AdamMil.Security.UI