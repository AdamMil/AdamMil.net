/*
AdamMil.Security.UI is a .NET library providing common user interface widgets
for OpenPGP-based software.
Copyright (C) 2008-2010 Adam Milazzo (http://www.adammil.net/)

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

/// <summary>This form helps the user export keys. The form does not actually export the keys, but merely gathers the
/// information needed to do so. It is meant to be shown as a modal dialog.
/// </summary>
public partial class ExportForm : Form
{
  /// <summary>Creates a new <see cref="ExportForm"/>. You should later call <see cref="Initialize"/> to initialize
  /// the form.
  /// </summary>
  public ExportForm()
  {
    InitializeComponent();

    options.Items.Add(new ListItem<ExportOptions>(ExportOptions.ExportPublicKeys, "Export Public Keys"));
    options.Items.Add(new ListItem<ExportOptions>(ExportOptions.ExportSecretKeys, "Export Secret Keys"));
    options.Items.Add("Export in a Binary Format", false);
    options.Items.Add(new ListItem<ExportOptions>(ExportOptions.ExportLocalSignatures, "Include Local Signatures"));
    options.Items.Add(new ListItem<ExportOptions>(ExportOptions.ExportSensitiveRevokerInfo, "Include Sensitive Revokers"));
    options.Items.Add(new ListItem<ExportOptions>(ExportOptions.ExcludeAttributes, "Exclude Attributes"));
    options.Items.Add(new ListItem<ExportOptions>(ExportOptions.CleanKeys, "Clean Exported Keys"));
    options.Items.Add(new ListItem<ExportOptions>(ExportOptions.MinimizeKeys, "Minimize Exported Keys"));
    options.Items.Add(new ListItem<ExportOptions>(ExportOptions.ClobberPrimarySecretKey, "Clobber Exported Primary Secret Key"));
    options.Items.Add(new ListItem<ExportOptions>(ExportOptions.ResetSubkeyPassword, "Reset Secret Subkey Passwords"));
    
    options.SetItemChecked(0, true); // export public keys by default
  }

  /// <summary>Initializes a new <see cref="ExportForm"/> with the given list of keys to export.</summary>
  public ExportForm(PrimaryKey[] keys) : this()
  {
    Initialize(keys);
  }

  /// <summary>Gets the <see cref="PGP.ExportOptions"/> selected by the user.</summary>
  [Browsable(false)]
  public ExportOptions ExportOptions
  {
    get
    {
      ExportOptions exportOptions = 0;
      for(int i=0; i<options.Items.Count; i++)
      {
        if(options.GetItemChecked(i))
        {
          ListItem<ExportOptions> exportItem = options.Items[i] as ListItem<ExportOptions>;
          if(exportItem != null) exportOptions |= exportItem.Value;
        }
      }
      return exportOptions;
    }
  }

  /// <summary>Gets the name of the file into which the keys should be saved, or null if they should be saved to the
  /// clipboard.
  /// </summary>
  [Browsable(false)]
  public string Filename
  {
    get { return rbFile.Checked ? txtFile.Text : null; }
  }

  /// <summary>Gets the <see cref="PGP.OutputOptions"/> selected by the user.</summary>
  [Browsable(false)]
  public OutputOptions OutputOptions
  {
    get
    {
      return new OutputOptions(rbClipboard.Checked || !options.GetItemChecked(2) ?
                                 OutputFormat.ASCII : OutputFormat.Binary);
    }
  }
  
  /// <summary>Initializes the form with the given list of keys to export. If the list is null or empty, it is assumed
  /// that all relevant keys will be exported.
  /// </summary>
  public void Initialize(PrimaryKey[] keys)
  {
    keyList.Items.Clear();

    if(keys == null || keys.Length == 0)
    {
      keyList.Items.Add("All keys");
    }
    else
    {
      foreach(PrimaryKey key in keys)
      {
        if(key == null) throw new ArgumentNullException();
        keyList.Items.Add(new KeyItem(key));
      }
    }
  }

  void btnDefaults_Click(object sender, EventArgs e)
  {
    options.SetItemChecked(0, true);  // export public keys
    options.SetItemChecked(1, false); // don't export secret keys
    options.SetItemChecked(2, false); // don't use binary
    options.SetItemChecked(3, false); // don't include local signatures
    options.SetItemChecked(4, false); // don't include sensitive revokers
    options.SetItemChecked(5, false); // don't exclude attributes
    options.SetItemChecked(6, false); // don't clean keys
    options.SetItemChecked(7, false); // don't minimize keys
    options.SetItemChecked(8, false); // don't clobber primary secret key
    options.SetItemChecked(9, false); // don't reset subkey passwords
  }

  void btnBackup_Click(object sender, EventArgs e)
  {
    options.SetItemChecked(0, true);  // export public keys
    options.SetItemChecked(1, true);  // export secret keys
    options.SetItemChecked(2, false); // don't use binary
    options.SetItemChecked(3, true);  // include local signatures
    options.SetItemChecked(4, true);  // include sensitive revokers
    options.SetItemChecked(5, false); // don't exclude attributes
    options.SetItemChecked(6, false); // don't clean keys
    options.SetItemChecked(7, false); // don't minimize keys
    options.SetItemChecked(8, false); // don't clobber primary secret key
    options.SetItemChecked(9, false); // don't reset subkey passwords
  }

  void rbFile_CheckedChanged(object sender, EventArgs e)
  {
    txtFile.Enabled = btnBrowse.Enabled = rbFile.Checked;
  }

  void btnBrowse_Click(object sender, EventArgs e)
  {
    string suffix;
    if(options.GetItemChecked(0) && options.GetItemChecked(1)) suffix = "pub-sec";
    else if(options.GetItemChecked(1)) suffix = "sec";
    else suffix = "pub";

    string ext = options.GetItemChecked(2) ? ".pgp" : ".txt";

    string defaultFilename =
      PGPUI.MakeSafeFilename((keyList.Items.Count == 1 ? keyList.Items[0].ToString() : "Exported keys") + "." +
                             suffix + ext);

    SaveFileDialog sfd = new SaveFileDialog();
    sfd.DefaultExt      = ".txt";
    sfd.FileName        = defaultFilename;
    sfd.Filter          = "Text Files (*.txt)|*.txt|ASCII Files (*.asc)|*.asc|PGP Files (*.pgp)|*.pgp|All Files (*.*)|*.*";
    sfd.FilterIndex     = options.GetItemChecked(2) ? 3 : 1;
    sfd.OverwritePrompt = true;
    sfd.Title           = "Save Exported Keys";
    sfd.SupportMultiDottedExtensions = true;
    if(sfd.ShowDialog() == DialogResult.OK) txtFile.Text = sfd.FileName;
  }

  void options_SelectedIndexChanged(object sender, EventArgs e)
  {
    // don't allow the export if neither "Export Public Keys" nor "Export Secret Keys" are checked
    btnExport.Enabled = options.GetItemChecked(0) || options.GetItemChecked(1);
  }

  void btnExport_Click(object sender, EventArgs e)
  {
    // do some basic validation of the output filename
    bool badFilename = false;

    if(rbFile.Checked)
    {
      badFilename = txtFile.Text.Trim().Length == 0;

      if(!badFilename)
      {
        try { new System.IO.FileInfo(txtFile.Text); }
        catch { badFilename = true; }
      }
    }

    if(badFilename)
    {
      MessageBox.Show("You have not specified a valid file where the exported keys should be saved.",
                      "Missing or invalid filename", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    else
    {
      DialogResult = DialogResult.OK;
    }
  }

  void rbClipboard_CheckedChanged(object sender, EventArgs e)
  {
    if(rbClipboard.Checked) options.SetItemChecked(2, false); // don't use binary on the clipboard
  }
}

} // namespace AdamMil.Security.UI