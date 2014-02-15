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
using System.Collections.Generic;
using System.Windows.Forms;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

/// <summary>This form displays a list of data signatures to the user.</summary>
public partial class SignaturesForm : Form
{
  /// <summary>Creates a new <see cref="SignaturesForm"/>. You must call <c>Initialize</c> to initialize the
  /// form.
  /// </summary>
  public SignaturesForm()
  {
    InitializeComponent();
  }

  /// <summary>Initializes a new <see cref="SignaturesForm"/> with the signatures to display.</summary>
  public SignaturesForm(Signature[] sigs) : this()
  {
    Initialize(sigs);
  }

  /// <summary>Initializes a new <see cref="SignaturesForm"/> with the signatures to display.</summary>
  public SignaturesForm(Dictionary<string,Signature[]> sigs) : this()
  {
    Initialize(sigs);
  }

  /// <summary>Initializes this form with the signatures to display.</summary>
  public void Initialize(Signature[] sigs)
  {
    if(sigs == null) throw new ArgumentNullException();
    signatureList.Initialize(sigs);
  }

  /// <summary>Initializes this form with the signatures to display.</summary>
  public void Initialize(Dictionary<string,Signature[]> sigs)
  {
    if(sigs == null) throw new ArgumentNullException();
    signatureList.Initialize(sigs);
  }

  /// <include file="documentation.xml" path="/UI/Common/OnKeyDown/node()"/>
  protected override void OnKeyDown(KeyEventArgs e)
  {
    base.OnKeyDown(e);

    if(!e.Handled && PGPUI.IsCloseKey(e))
    {
      Close();
      e.Handled = true;
    }
  }
}

} // namespace AdamMil.Security.UI