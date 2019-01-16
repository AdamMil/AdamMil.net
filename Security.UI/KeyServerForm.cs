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
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace AdamMil.Security.UI
{

/// <summary>This form presents a dialog that allows the user to select a key server. The user can choose from a
/// predefined list or type in his own. It is meant to be used as a modal dialog.
/// </summary>
public partial class KeyServerForm : Form
{
  /// <summary>Initializes a new <see cref="KeyServerForm"/>.</summary>
  public KeyServerForm()
  {
    InitializeComponent();

    keyservers.Items.AddRange(PGPUI.GetDefaultKeyServers());
    keyservers.SelectedIndex = 0; // select the first keyserver by default

    // keep track of the margin around the help label so we can recreate it
    topSpace        = lblHelp.Top;
    bottomSpace     = Height - lblHelp.Bottom;
    horizontalSpace = lblHelp.Left;
  }

  /// <summary>Gets or sets the help text displayed in the form. The text should tell the user why he needs to choose
  /// a key server.
  /// </summary>
  public string HelpText
  {
    get { return lblHelp.Text; }
    set
    {
      if(value == null) throw new ArgumentNullException();

      if(!string.Equals(value, lblHelp.Text, StringComparison.Ordinal))
      {
        const int MaxWidth = 500;
        Size textSize;
        using(Graphics gdi = Graphics.FromHwnd(Handle))
        {
          textSize = Size.Ceiling(gdi.MeasureString(value, lblHelp.Font, new SizeF(MaxWidth, int.MaxValue)));
        }
        Size = new Size(Math.Max(MaxWidth, textSize.Width + horizontalSpace*2), textSize.Height + topSpace + bottomSpace);

        lblHelp.Text = value;
      }
    }
  }

  /// <summary>Gets a collection of strings containing key server URIs that the user can choose from.</summary>
  [Browsable(false)]
  public ComboBox.ObjectCollection KeyServers
  {
    get { return keyservers.Items; }
  }

  /// <summary>Gets or sets the key server URI selected by the user.</summary>
  [Browsable(false)]
  public Uri SelectedKeyServer
  {
    get { return keyServer; }
    set
    {
      keyServer = value;
      keyservers.Text = value == null ? string.Empty : value.AbsoluteUri;
    }
  }

  /// <include file="documentation.xml" path="/UI/Common/OnClosing/*"/>
  protected override void OnShown(EventArgs e)
  {
    base.OnShown(e);
    keyservers.Focus();
  }

  void btnOK_Click(object sender, EventArgs e)
  {
    try
    {
      keyServer = new Uri(keyservers.Text, UriKind.Absolute);
      DialogResult = DialogResult.OK;
    }
    catch
    {
      MessageBox.Show(keyservers.Text + " is not a valid key server name.", "Invalid URI",
                      MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
  }

  readonly int topSpace, bottomSpace, horizontalSpace;
  Uri keyServer;
}

} // namespace AdamMil.Security.UI