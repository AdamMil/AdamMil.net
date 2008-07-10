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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace AdamMil.Security.UI
{

public partial class KeyServerForm : Form
{
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

  [Browsable(false)]
  public ComboBox.ObjectCollection KeyServers
  {
    get { return keyservers.Items; }
  }

  [Browsable(false)]
  public Uri SelectedKeyServer
  {
    get { return keyServer; }
  }

  protected override void OnClosing(CancelEventArgs e)
  {
    base.OnClosing(e);

    if(!e.Cancel && !cancelled)
    {
      try { keyServer = new Uri(keyservers.Text, UriKind.Absolute); }
      catch
      {
        MessageBox.Show(keyservers.Text + " is not a valid key server name.", "Invalid URI",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
        e.Cancel = true;
      }
    }
  }

  protected override void OnShown(EventArgs e)
  {
    base.OnShown(e);
    keyservers.Focus();
  }

  void btnCancel_Click(object sender, EventArgs e)
  {
    cancelled = true;
  }

  readonly int topSpace, bottomSpace, horizontalSpace;
  Uri keyServer;
  bool cancelled;
}

} // namespace AdamMil.Security.UI