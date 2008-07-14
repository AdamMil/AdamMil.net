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

public partial class ImportForm : Form
{
  public ImportForm()
  {
    InitializeComponent();

    options.Items.Add(new ListItem<ImportOptions>(ImportOptions.ImportLocalSignatures, "Import Local Signatures"));
    options.Items.Add(new ListItem<ImportOptions>(ImportOptions.MergeOnly, "Don't Create New Keys (merge only)"));
    options.Items.Add(new ListItem<ImportOptions>(ImportOptions.CleanKeys, "Clean Imported Keys"), true);
    options.Items.Add(new ListItem<ImportOptions>(ImportOptions.MinimizeKeys, "Minimize Imported Keys"));
  }

  [Browsable(false)]
  public string Filename
  {
    get { return rbFile.Checked ? txtFile.Text : null; }
  }

  [Browsable(false)]
  public ImportOptions ImportOptions
  {
    get
    {
      ImportOptions importOptions = 0;
      for(int i=0; i<options.Items.Count; i++)
      {
        if(options.GetItemChecked(i)) importOptions |= ((ListItem<ImportOptions>)options.Items[i]).Value;
      }
      return importOptions;
    }
  }

  void rbFile_CheckedChanged(object sender, EventArgs e)
  {
    txtFile.Enabled = btnBrowse.Enabled = rbFile.Checked;
  }

  void btnDefaults_Click(object sender, EventArgs e)
  {
    options.SetItemChecked(0, false); // don't import local signatures
    options.SetItemChecked(1, false); // do import new keys
    options.SetItemChecked(2, true);  // clean keys
    options.SetItemChecked(3, false); // don't minimize keys
  }

  void btnUpdate_Click(object sender, EventArgs e)
  {
    options.SetItemChecked(0, false); // don't import local signatures
    options.SetItemChecked(1, true);  // don't import new keys
    options.SetItemChecked(2, true);  // clean keys
    options.SetItemChecked(3, false); // don't minimize keys
  }

  void btnBackup_Click(object sender, EventArgs e)
  {
    options.SetItemChecked(0, true);  // do import local signatures
    options.SetItemChecked(1, false); // do import new keys
    options.SetItemChecked(2, false); // don't clean keys
    options.SetItemChecked(3, false); // don't minimize keys
  }

  void btnBrowse_Click(object sender, EventArgs e)
  {
    OpenFileDialog ofd = new OpenFileDialog();
    ofd.DefaultExt      = ".txt";
    ofd.Filter          = "Text Files (*.txt)|*.txt|ASCII Files (*.asc)|*.asc|PGP Files (*.pgp)|*.pgp|All Files (*.*)|*.*";
    ofd.Title           = "Import Keys";
    ofd.SupportMultiDottedExtensions = true;
    if(ofd.ShowDialog() == DialogResult.OK) txtFile.Text = ofd.FileName;
  }

  void btnImport_Click(object sender, EventArgs e)
  {
    bool badFilename = false;

    if(rbFile.Checked)
    {
      badFilename = txtFile.Text.Trim().Length == 0;

      if(!badFilename)
      {
        try { if(!new System.IO.FileInfo(txtFile.Text).Exists) badFilename = true; }
        catch { badFilename = true; }
      }
    }

    if(badFilename)
    {
      MessageBox.Show("You have not specified a valid file from where the imported keys should be loaded.",
                      "Missing or invalid filename", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    else
    {
      DialogResult = DialogResult.OK;
    }
  }
}

} // namespace AdamMil.Security.UI