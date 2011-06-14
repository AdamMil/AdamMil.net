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
using System.Threading;
using System.Windows.Forms;

namespace AdamMil.Security.UI
{

/// <summary>This form executes a long-running operation in a background thread, and provides the user a way to cancel
/// it. The form automatically closes itself when the operation is complete. The form is meant to be used as a modal
/// dialog, and the <see cref="DialogResult"/> will be <see cref="DialogResult.OK"/> if the operation completed
/// successfully, <see cref="DialogResult.Cancel"/> if the user canceled it, or <see cref="DialogResult.Abort"/> if the
/// operation failed.
/// </summary>
public partial class ProgressForm : Form
{
  /// <summary>Creates a new <see cref="ProgressForm"/>. You must call <see cref="Initialize"/> to initialize the form.</summary>
  public ProgressForm()
  {
    InitializeComponent();
  }

  /// <summary>Initializes a new <see cref="ProgressForm"/> with the form caption, a description of the operation, and
  /// a <see cref="ThreadStart"/> delegate that represents the operation to perform.
  /// </summary>
  public ProgressForm(string caption, string description, ThreadStart operation)
  {
    Initialize(caption, description, operation);
  }

  /// <summary>Gets the exception that occurred during the operation, if any.</summary>
  [Browsable(false)]
  public Exception Exception
  {
    get { return exception; }
  }

  /// <summary>Gets whether the operation is still in progress.</summary>
  [Browsable(false)]
  public bool InProgress
  {
    get { return thread != null; }
  }

  /// <summary>Cancels the operation, if it's still in progress.</summary>
  public void Cancel()
  {
    if(InProgress)
    {
      thread.Abort();
      thread = null;
    }
  }

  /// <summary>Initializes this form with the form caption, a description of the operation, and a
  /// <see cref="ThreadStart"/> delegate that represents the operation to perform.
  /// </summary>
  public void Initialize(string caption, string description, ThreadStart operation)
  {
    if(operation == null) throw new ArgumentNullException();
    InitializeComponent();

    if(!string.IsNullOrEmpty(caption)) Text = caption;
    if(!string.IsNullOrEmpty(description)) lblDescription.Text = description;

    thread = new Thread(delegate()
      {
        try
        {
          operation();
          DialogResult = DialogResult.OK;
        }
        catch(Exception ex)
        {
          if(ex is ThreadAbortException || ex is OperationCanceledException)
          {
            if(!(ex is ThreadAbortException)) exception = ex;
            DialogResult = DialogResult.Cancel;
          }
          else
          {
            exception = ex;
            DialogResult = DialogResult.Abort;
          }
        }
      });
  }

  /// <summary>Throws the exception that occurred during the operation, if any.</summary>
  public void ThrowExceptionIfAny()
  {
    if(exception != null) throw exception;
  }

  /// <include file="documentation.xml" path="/UI/Common/OnClosing/*"/>
  protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
  {
    base.OnClosing(e);

    if(!e.Cancel && InProgress && DialogResult == DialogResult.None)
    {
      e.Cancel = true; // we'll always cancel, and let the background thread set the DialogResult property to close
                       // the form
      if(MessageBox.Show("The operation has not yet completed. Cancel it?", "Cancel operation?",
                         MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) ==
           DialogResult.Yes)
      {
        CancelFromUI();
      }
    }
  }

  /// <include file="documentation.xml" path="/UI/Common/OnClosed/*"/>
  protected override void OnClosed(EventArgs e)
  {
    base.OnClosed(e);
    thread = null;
  }

  /// <include file="documentation.xml" path="/UI/Common/OnLoad/*"/>
  protected override void OnLoad(EventArgs e)
  {
    base.OnLoad(e);
    thread.Start();
  }

  /// <summary>Cancels from the user interface, disabling the cancel button so it can't be clicked again.</summary>
  void CancelFromUI()
  {
    btnCancel.Enabled = false;

    if(InProgress && !thread.Join(1000)) // give the operation an extra second, because we're bastards...
    {
      thread.Abort();
      thread = null;
    }
  }

  void btnCancel_Click(object sender, EventArgs e)
  {
    CancelFromUI();
  }

  Exception exception;
  Thread thread;
}

} // namespace AdamMil.Security.UI