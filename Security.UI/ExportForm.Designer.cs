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

namespace AdamMil.Security.UI
{
  partial class ExportForm
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
      System.Windows.Forms.Label lblKeys;
      System.Windows.Forms.GroupBox grpHow;
      System.Windows.Forms.Button btnBackup;
      System.Windows.Forms.Button btnDefaults;
      System.Windows.Forms.GroupBox whereBox;
      System.Windows.Forms.Button btnCancel;
      this.options = new System.Windows.Forms.CheckedListBox();
      this.btnBrowse = new System.Windows.Forms.Button();
      this.txtFile = new System.Windows.Forms.TextBox();
      this.rbFile = new System.Windows.Forms.RadioButton();
      this.rbClipboard = new System.Windows.Forms.RadioButton();
      this.keyList = new System.Windows.Forms.ListBox();
      this.btnExport = new System.Windows.Forms.Button();
      lblKeys = new System.Windows.Forms.Label();
      grpHow = new System.Windows.Forms.GroupBox();
      btnBackup = new System.Windows.Forms.Button();
      btnDefaults = new System.Windows.Forms.Button();
      whereBox = new System.Windows.Forms.GroupBox();
      btnCancel = new System.Windows.Forms.Button();
      grpHow.SuspendLayout();
      whereBox.SuspendLayout();
      this.SuspendLayout();
      //
      // lblKeys
      //
      lblKeys.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      lblKeys.Location = new System.Drawing.Point(5, 3);
      lblKeys.Name = "lblKeys";
      lblKeys.Size = new System.Drawing.Size(396, 13);
      lblKeys.TabIndex = 0;
      lblKeys.Text = "Keys to be exported:";
      lblKeys.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
      //
      // grpHow
      //
      grpHow.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      grpHow.Controls.Add(btnBackup);
      grpHow.Controls.Add(btnDefaults);
      grpHow.Controls.Add(this.options);
      grpHow.Location = new System.Drawing.Point(8, 94);
      grpHow.Name = "grpHow";
      grpHow.Size = new System.Drawing.Size(396, 225);
      grpHow.TabIndex = 2;
      grpHow.TabStop = false;
      grpHow.Text = "&How should the keys be exported?";
      //
      // btnBackup
      //
      btnBackup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      btnBackup.Location = new System.Drawing.Point(201, 192);
      btnBackup.Name = "btnBackup";
      btnBackup.Size = new System.Drawing.Size(131, 23);
      btnBackup.TabIndex = 2;
      btnBackup.Text = "&Make a Backup";
      btnBackup.UseVisualStyleBackColor = true;
      btnBackup.Click += new System.EventHandler(this.btnBackup_Click);
      //
      // btnDefaults
      //
      btnDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      btnDefaults.Location = new System.Drawing.Point(10, 192);
      btnDefaults.Name = "btnDefaults";
      btnDefaults.Size = new System.Drawing.Size(185, 23);
      btnDefaults.TabIndex = 1;
      btnDefaults.Text = "For &Public Distribution";
      btnDefaults.UseVisualStyleBackColor = true;
      btnDefaults.Click += new System.EventHandler(this.btnDefaults_Click);
      //
      // options
      //
      this.options.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.options.CheckOnClick = true;
      this.options.FormattingEnabled = true;
      this.options.Location = new System.Drawing.Point(10, 20);
      this.options.Name = "options";
      this.options.Size = new System.Drawing.Size(376, 164);
      this.options.TabIndex = 0;
      this.options.SelectedIndexChanged += new System.EventHandler(this.options_SelectedIndexChanged);
      //
      // whereBox
      //
      whereBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      whereBox.Controls.Add(this.btnBrowse);
      whereBox.Controls.Add(this.txtFile);
      whereBox.Controls.Add(this.rbFile);
      whereBox.Controls.Add(this.rbClipboard);
      whereBox.Location = new System.Drawing.Point(8, 325);
      whereBox.Name = "whereBox";
      whereBox.Size = new System.Drawing.Size(396, 100);
      whereBox.TabIndex = 3;
      whereBox.TabStop = false;
      whereBox.Text = "&Where should the keys be saved?";
      //
      // btnBrowse
      //
      this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnBrowse.Location = new System.Drawing.Point(319, 68);
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
      this.txtFile.Size = new System.Drawing.Size(304, 21);
      this.txtFile.TabIndex = 2;
      //
      // rbFile
      //
      this.rbFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbFile.Checked = true;
      this.rbFile.Location = new System.Drawing.Point(9, 44);
      this.rbFile.Name = "rbFile";
      this.rbFile.Size = new System.Drawing.Size(370, 17);
      this.rbFile.TabIndex = 1;
      this.rbFile.TabStop = true;
      this.rbFile.Text = "Save them to a file:";
      this.rbFile.UseVisualStyleBackColor = true;
      this.rbFile.CheckedChanged += new System.EventHandler(this.rbFile_CheckedChanged);
      //
      // rbClipboard
      //
      this.rbClipboard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbClipboard.Location = new System.Drawing.Point(9, 21);
      this.rbClipboard.Name = "rbClipboard";
      this.rbClipboard.Size = new System.Drawing.Size(370, 17);
      this.rbClipboard.TabIndex = 0;
      this.rbClipboard.Text = "Save them on the &clipboard. (ASCII export only)";
      this.rbClipboard.UseVisualStyleBackColor = true;
      this.rbClipboard.CheckedChanged += new System.EventHandler(this.rbClipboard_CheckedChanged);
      //
      // btnCancel
      //
      btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      btnCancel.Location = new System.Drawing.Point(329, 434);
      btnCancel.Name = "btnCancel";
      btnCancel.Size = new System.Drawing.Size(75, 23);
      btnCancel.TabIndex = 5;
      btnCancel.Text = "&Cancel";
      btnCancel.UseVisualStyleBackColor = true;
      //
      // keyList
      //
      this.keyList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.keyList.FormattingEnabled = true;
      this.keyList.Location = new System.Drawing.Point(8, 19);
      this.keyList.Name = "keyList";
      this.keyList.SelectionMode = System.Windows.Forms.SelectionMode.None;
      this.keyList.Size = new System.Drawing.Size(396, 69);
      this.keyList.TabIndex = 1;
      //
      // btnExport
      //
      this.btnExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnExport.Location = new System.Drawing.Point(248, 434);
      this.btnExport.Name = "btnExport";
      this.btnExport.Size = new System.Drawing.Size(75, 23);
      this.btnExport.TabIndex = 4;
      this.btnExport.Text = "&Export";
      this.btnExport.UseVisualStyleBackColor = true;
      this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
      //
      // ExportForm
      //
      this.AcceptButton = this.btnExport;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = btnCancel;
      this.ClientSize = new System.Drawing.Size(412, 464);
      this.Controls.Add(this.btnExport);
      this.Controls.Add(btnCancel);
      this.Controls.Add(whereBox);
      this.Controls.Add(grpHow);
      this.Controls.Add(lblKeys);
      this.Controls.Add(this.keyList);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MaximumSize = new System.Drawing.Size(2000, 489);
      this.MinimizeBox = false;
      this.MinimumSize = new System.Drawing.Size(366, 489);
      this.Name = "ExportForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Export Keys";
      grpHow.ResumeLayout(false);
      whereBox.ResumeLayout(false);
      whereBox.PerformLayout();
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.ListBox keyList;
    private System.Windows.Forms.CheckedListBox options;
    private System.Windows.Forms.Button btnBrowse;
    private System.Windows.Forms.TextBox txtFile;
    private System.Windows.Forms.RadioButton rbFile;
    private System.Windows.Forms.RadioButton rbClipboard;
    private System.Windows.Forms.Button btnExport;
  }
}