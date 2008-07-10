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
using System.Threading;
using System.Windows.Forms;

namespace AdamMil.Security.UI
{

public partial class ProgressForm : Form
{
  ProgressForm()
  {
    InitializeComponent();
  }

  public ProgressForm(string caption, string description, ThreadStart operation)
  {
    if(operation == null) throw new ArgumentNullException();
    InitializeComponent();

    Text = caption;
    lblDescription.Text = description;

    thread = new Thread(delegate()
      {
        try
        {
          operation();
          DialogResult = DialogResult.OK;
        }
        catch(Exception ex)
        { 
          exception = ex;
          DialogResult = DialogResult.Abort;
        }
      });
  }

  [Browsable(false)]
  public Exception Exception
  {
    get { return exception; }
  }

  public void ThrowException()
  {
    if(exception != null) throw exception;
  }

  protected override void OnLoad(EventArgs e)
  {
    base.OnLoad(e);
    thread.Start();
  }

  protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
  {
    base.OnClosing(e);
    if(!e.Cancel && !thread.Join(1000)) thread.Abort();
  }

  void btnCancel_Click(object sender, EventArgs e)
  {
    btnCancel.Enabled = false;
  }

  Exception exception;
  Thread thread;
}

} // namespace AdamMil.Security.UI