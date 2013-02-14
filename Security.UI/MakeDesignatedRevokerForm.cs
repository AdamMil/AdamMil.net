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

/// <summary>This form helps the user make a key a designated revoker for another key. The form does not actually make
/// the designated revoker, but merely gathers the information needed to do so. The form is meant to be used as a modal
/// dialog.
/// </summary>
public partial class MakeDesignatedRevokerForm : Form
{
  /// <summary>Creates a new <see cref="MakeDesignatedRevokerForm"/>. <see cref="Initialize"/> should be called to
  /// initialize the form.
  /// </summary>
  public MakeDesignatedRevokerForm()
  {
    InitializeComponent();
  }

  /// <summary>Initializes a new <see cref="MakeDesignatedRevokerForm"/> with the given designated revoker key, and a
  /// list of keys owned by the user that can be selected to be revocable by the revoker key.
  /// </summary>
  public MakeDesignatedRevokerForm(PrimaryKey revokerKey, PrimaryKey[] ownedKeys) : this()
  {
    Initialize(revokerKey, ownedKeys);
  }

  /// <summary>Gets the selected key that will have a designated revoker added to it.</summary>
  [Browsable(false)]
  public PrimaryKey SelectedKey
  {
    get { return ((KeyItem)this.ownedKeys.SelectedItem).Value; }
  }

  /// <summary>Initializes this form with the given designated revoker key, and a list of keys owned by the user that
  /// can be selected to be revocable by the revoker key.
  /// </summary>
  public void Initialize(PrimaryKey revokerKey, PrimaryKey[] ownedKeys)
  {
    if(revokerKey == null || ownedKeys == null) throw new ArgumentNullException();

    lblRevokingKey.Text = "You are making this key a designated revoker:\n" + PGPUI.GetKeyName(revokerKey);

    this.ownedKeys.Items.Clear();
    foreach(PrimaryKey key in ownedKeys)
    {
      if(key == null) throw new ArgumentException("A key was null.");
      this.ownedKeys.Items.Add(new KeyItem(key));
    }

    btnOK.Enabled = this.ownedKeys.Items.Count != 0;
    if(this.ownedKeys.Items.Count != 0) this.ownedKeys.SelectedIndex = 0;
  }
}

} // namespace AdamMil.Security.UI