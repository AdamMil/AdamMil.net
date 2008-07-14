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

namespace AdamMil.Security.UI
{
  partial class ImportForm
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if(disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      System.Windows.Forms.Button btnCancel;
      System.Windows.Forms.GroupBox whereBox;
      System.Windows.Forms.GroupBox grpHow;
      System.Windows.Forms.Button btnBackup;
      System.Windows.Forms.Button btnDefaults;
      System.Windows.Forms.Button btnUpdate;
      this.btnImport = new System.Windows.Forms.Button();
      this.btnBrowse = new System.Windows.Forms.Button();
      this.txtFile = new System.Windows.Forms.TextBox();
      this.rbFile = new System.Windows.Forms.RadioButton();
      this.rbClipboard = new System.Windows.Forms.RadioButton();
      this.options = new System.Windows.Forms.CheckedListBox();
      btnCancel = new System.Windows.Forms.Button();
      whereBox = new System.Windows.Forms.GroupBox();
      grpHow = new System.Windows.Forms.GroupBox();
      btnBackup = new System.Windows.Forms.Button();
      btnDefaults = new System.Windows.Forms.Button();
      btnUpdate = new System.Windows.Forms.Button();
      whereBox.SuspendLayout();
      grpHow.SuspendLayout();
      this.SuspendLayout();
      // 
      // btnImport
      // 
      this.btnImport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnImport.Location = new System.Drawing.Point(251, 245);
      this.btnImport.Name = "btnImport";
      this.btnImport.Size = new System.Drawing.Size(75, 23);
      this.btnImport.TabIndex = 2;
      this.btnImport.Text = "&Import";
      this.btnImport.UseVisualStyleBackColor = true;
      this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
      // 
      // btnCancel
      // 
      btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      btnCancel.Location = new System.Drawing.Point(332, 245);
      btnCancel.Name = "btnCancel";
      btnCancel.Size = new System.Drawing.Size(75, 23);
      btnCancel.TabIndex = 3;
      btnCancel.Text = "&Cancel";
      btnCancel.UseVisualStyleBackColor = true;
      // 
      // whereBox
      // 
      whereBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      whereBox.Controls.Add(this.btnBrowse);
      whereBox.Controls.Add(this.txtFile);
      whereBox.Controls.Add(this.rbFile);
      whereBox.Controls.Add(this.rbClipboard);
      whereBox.Location = new System.Drawing.Point(8, 139);
      whereBox.Name = "whereBox";
      whereBox.Size = new System.Drawing.Size(399, 100);
      whereBox.TabIndex = 1;
      whereBox.TabStop = false;
      whereBox.Text = "&Where are the keys located?";
      // 
      // btnBrowse
      // 
      this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnBrowse.Location = new System.Drawing.Point(322, 68);
      this.btnBrowse.Name = "btnBrowse";
      this.btnBrowse.Size = new System.Drawing.Size(67, 21);
      this.btnBrowse.TabIndex = 3;
      this.btnBrowse.Text = "&Browse";
      this.btnBrowse.UseVisualStyleBackColor = true;
      this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
      // 
      // txtFile
      // 
      this.txtFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtFile.Location = new System.Drawing.Point(9, 68);
      this.txtFile.Name = "txtFile";
      this.txtFile.Size = new System.Drawing.Size(307, 21);
      this.txtFile.TabIndex = 2;
      // 
      // rbFile
      // 
      this.rbFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbFile.Checked = true;
      this.rbFile.Location = new System.Drawing.Point(9, 44);
      this.rbFile.Name = "rbFile";
      this.rbFile.Size = new System.Drawing.Size(373, 17);
      this.rbFile.TabIndex = 1;
      this.rbFile.TabStop = true;
      this.rbFile.Text = "They are in a file:";
      this.rbFile.UseVisualStyleBackColor = true;
      this.rbFile.CheckedChanged += new System.EventHandler(this.rbFile_CheckedChanged);
      // 
      // rbClipboard
      // 
      this.rbClipboard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbClipboard.Location = new System.Drawing.Point(9, 21);
      this.rbClipboard.Name = "rbClipboard";
      this.rbClipboard.Size = new System.Drawing.Size(373, 17);
      this.rbClipboard.TabIndex = 0;
      this.rbClipboard.Text = "They are on the &clipboard.";
      this.rbClipboard.UseVisualStyleBackColor = true;
      // 
      // grpHow
      // 
      grpHow.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      grpHow.Controls.Add(btnUpdate);
      grpHow.Controls.Add(btnBackup);
      grpHow.Controls.Add(btnDefaults);
      grpHow.Controls.Add(this.options);
      grpHow.Location = new System.Drawing.Point(8, 6);
      grpHow.Name = "grpHow";
      grpHow.Size = new System.Drawing.Size(399, 127);
      grpHow.TabIndex = 0;
      grpHow.TabStop = false;
      grpHow.Text = "&How should the keys be imported?";
      // 
      // options
      // 
      this.options.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.options.CheckOnClick = true;
      this.options.FormattingEnabled = true;
      this.options.Location = new System.Drawing.Point(10, 20);
      this.options.Name = "options";
      this.options.Size = new System.Drawing.Size(379, 68);
      this.options.TabIndex = 0;
      // 
      // btnBackup
      // 
      btnBackup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      btnBackup.Location = new System.Drawing.Point(260, 94);
      btnBackup.Name = "btnBackup";
      btnBackup.Size = new System.Drawing.Size(129, 23);
      btnBackup.TabIndex = 3;
      btnBackup.Text = "&Restore a Backup";
      btnBackup.UseVisualStyleBackColor = true;
      btnBackup.Click += new System.EventHandler(this.btnBackup_Click);
      // 
      // btnDefaults
      // 
      btnDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      btnDefaults.Location = new System.Drawing.Point(10, 94);
      btnDefaults.Name = "btnDefaults";
      btnDefaults.Size = new System.Drawing.Size(134, 23);
      btnDefaults.TabIndex = 1;
      btnDefaults.Text = "Import &Public Keys";
      btnDefaults.UseVisualStyleBackColor = true;
      btnDefaults.Click += new System.EventHandler(this.btnDefaults_Click);
      // 
      // btnUpdate
      // 
      btnUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      btnUpdate.Location = new System.Drawing.Point(150, 94);
      btnUpdate.Name = "btnUpdate";
      btnUpdate.Size = new System.Drawing.Size(104, 23);
      btnUpdate.TabIndex = 2;
      btnUpdate.Text = "&Update Keys";
      btnUpdate.UseVisualStyleBackColor = true;
      btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
      // 
      // ImportForm
      // 
      this.AcceptButton = this.btnImport;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = btnCancel;
      this.ClientSize = new System.Drawing.Size(415, 273);
      this.Controls.Add(this.btnImport);
      this.Controls.Add(btnCancel);
      this.Controls.Add(whereBox);
      this.Controls.Add(grpHow);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "ImportForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Import Keys";
      whereBox.ResumeLayout(false);
      whereBox.PerformLayout();
      grpHow.ResumeLayout(false);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Button btnImport;
    private System.Windows.Forms.Button btnBrowse;
    private System.Windows.Forms.TextBox txtFile;
    private System.Windows.Forms.RadioButton rbFile;
    private System.Windows.Forms.RadioButton rbClipboard;
    private System.Windows.Forms.CheckedListBox options;
  }
}