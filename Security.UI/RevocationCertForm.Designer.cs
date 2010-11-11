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

namespace AdamMil.Security.UI
{
  partial class RevocationCertForm
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
      System.Windows.Forms.Label lblHelp;
      System.Windows.Forms.GroupBox howBox;
      System.Windows.Forms.Label lblRevokingKey;
      System.Windows.Forms.GroupBox whereBox;
      System.Windows.Forms.Button btnCancel;
      System.Windows.Forms.Label lblExplanation;
      System.Windows.Forms.GroupBox codeGroup;
      this.revokingKeys = new System.Windows.Forms.ComboBox();
      this.rbIndirect = new System.Windows.Forms.RadioButton();
      this.rbDirect = new System.Windows.Forms.RadioButton();
      this.btnBrowse = new System.Windows.Forms.Button();
      this.txtFile = new System.Windows.Forms.TextBox();
      this.rbFile = new System.Windows.Forms.RadioButton();
      this.rbClipboard = new System.Windows.Forms.RadioButton();
      this.rbRetired = new System.Windows.Forms.RadioButton();
      this.rbCompromised = new System.Windows.Forms.RadioButton();
      this.rbSuperceded = new System.Windows.Forms.RadioButton();
      this.rbNoReason = new System.Windows.Forms.RadioButton();
      this.btnOK = new System.Windows.Forms.Button();
      this.lblRevokedKey = new System.Windows.Forms.Label();
      this.txtExplanation = new System.Windows.Forms.TextBox();
      lblHelp = new System.Windows.Forms.Label();
      howBox = new System.Windows.Forms.GroupBox();
      lblRevokingKey = new System.Windows.Forms.Label();
      whereBox = new System.Windows.Forms.GroupBox();
      btnCancel = new System.Windows.Forms.Button();
      lblExplanation = new System.Windows.Forms.Label();
      codeGroup = new System.Windows.Forms.GroupBox();
      howBox.SuspendLayout();
      whereBox.SuspendLayout();
      codeGroup.SuspendLayout();
      this.SuspendLayout();
      //
      // lblHelp
      //
      lblHelp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      lblHelp.Location = new System.Drawing.Point(8, 7);
      lblHelp.Name = "lblHelp";
      lblHelp.Size = new System.Drawing.Size(442, 50);
      lblHelp.TabIndex = 0;
      lblHelp.Text = "A revocation certificate is a file that can be imported to revoke a key. You\'ll w" +
    "ant to keep the revocation certificate in a safe place, because anyone who gets " +
    "ahold of it can revoke your key!";
      //
      // howBox
      //
      howBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      howBox.Controls.Add(this.revokingKeys);
      howBox.Controls.Add(lblRevokingKey);
      howBox.Controls.Add(this.rbIndirect);
      howBox.Controls.Add(this.rbDirect);
      howBox.Location = new System.Drawing.Point(8, 101);
      howBox.Name = "howBox";
      howBox.Size = new System.Drawing.Size(442, 98);
      howBox.TabIndex = 2;
      howBox.TabStop = false;
      howBox.Text = "&How should the revocation certificate be generated?";
      //
      // revokingKeys
      //
      this.revokingKeys.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.revokingKeys.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.revokingKeys.Enabled = false;
      this.revokingKeys.FormattingEnabled = true;
      this.revokingKeys.Location = new System.Drawing.Point(100, 66);
      this.revokingKeys.Name = "revokingKeys";
      this.revokingKeys.Size = new System.Drawing.Size(332, 21);
      this.revokingKeys.TabIndex = 3;
      //
      // lblRevokingKey
      //
      lblRevokingKey.Location = new System.Drawing.Point(6, 67);
      lblRevokingKey.Name = "lblRevokingKey";
      lblRevokingKey.Size = new System.Drawing.Size(95, 18);
      lblRevokingKey.TabIndex = 2;
      lblRevokingKey.Text = "Revoking key:";
      lblRevokingKey.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // rbIndirect
      //
      this.rbIndirect.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbIndirect.Location = new System.Drawing.Point(9, 44);
      this.rbIndirect.Name = "rbIndirect";
      this.rbIndirect.Size = new System.Drawing.Size(416, 17);
      this.rbIndirect.TabIndex = 1;
      this.rbIndirect.TabStop = true;
      this.rbIndirect.Text = "Indirectly, because I am the key\'s designated revoker.";
      this.rbIndirect.UseVisualStyleBackColor = true;
      this.rbIndirect.CheckedChanged += new System.EventHandler(this.rbIndirect_CheckedChanged);
      //
      // rbDirect
      //
      this.rbDirect.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbDirect.Location = new System.Drawing.Point(9, 21);
      this.rbDirect.Name = "rbDirect";
      this.rbDirect.Size = new System.Drawing.Size(416, 17);
      this.rbDirect.TabIndex = 0;
      this.rbDirect.TabStop = true;
      this.rbDirect.Text = "Directly, because I own the key.";
      this.rbDirect.UseVisualStyleBackColor = true;
      //
      // whereBox
      //
      whereBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      whereBox.Controls.Add(this.btnBrowse);
      whereBox.Controls.Add(this.txtFile);
      whereBox.Controls.Add(this.rbFile);
      whereBox.Controls.Add(this.rbClipboard);
      whereBox.Location = new System.Drawing.Point(8, 410);
      whereBox.Name = "whereBox";
      whereBox.Size = new System.Drawing.Size(442, 100);
      whereBox.TabIndex = 6;
      whereBox.TabStop = false;
      whereBox.Text = "&Where should the revocation certificate be saved?";
      //
      // btnBrowse
      //
      this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnBrowse.Location = new System.Drawing.Point(365, 68);
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
      this.txtFile.Size = new System.Drawing.Size(350, 21);
      this.txtFile.TabIndex = 2;
      //
      // rbFile
      //
      this.rbFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbFile.Checked = true;
      this.rbFile.Location = new System.Drawing.Point(9, 44);
      this.rbFile.Name = "rbFile";
      this.rbFile.Size = new System.Drawing.Size(416, 17);
      this.rbFile.TabIndex = 1;
      this.rbFile.TabStop = true;
      this.rbFile.Text = "Save it to a file:";
      this.rbFile.UseVisualStyleBackColor = true;
      this.rbFile.CheckedChanged += new System.EventHandler(this.rbFile_CheckedChanged);
      //
      // rbClipboard
      //
      this.rbClipboard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbClipboard.Location = new System.Drawing.Point(9, 21);
      this.rbClipboard.Name = "rbClipboard";
      this.rbClipboard.Size = new System.Drawing.Size(416, 17);
      this.rbClipboard.TabIndex = 0;
      this.rbClipboard.Text = "Save it on the &clipboard.";
      this.rbClipboard.UseVisualStyleBackColor = true;
      //
      // btnCancel
      //
      btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      btnCancel.Location = new System.Drawing.Point(375, 516);
      btnCancel.Name = "btnCancel";
      btnCancel.Size = new System.Drawing.Size(75, 23);
      btnCancel.TabIndex = 8;
      btnCancel.Text = "&Cancel";
      btnCancel.UseVisualStyleBackColor = true;
      //
      // lblExplanation
      //
      lblExplanation.Location = new System.Drawing.Point(5, 323);
      lblExplanation.Name = "lblExplanation";
      lblExplanation.Size = new System.Drawing.Size(376, 13);
      lblExplanation.TabIndex = 4;
      lblExplanation.Text = "Short &explanation (optional):";
      lblExplanation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
      //
      // codeGroup
      //
      codeGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      codeGroup.Controls.Add(this.rbRetired);
      codeGroup.Controls.Add(this.rbCompromised);
      codeGroup.Controls.Add(this.rbSuperceded);
      codeGroup.Controls.Add(this.rbNoReason);
      codeGroup.Location = new System.Drawing.Point(8, 205);
      codeGroup.Name = "codeGroup";
      codeGroup.Size = new System.Drawing.Size(442, 112);
      codeGroup.TabIndex = 3;
      codeGroup.TabStop = false;
      codeGroup.Text = "What is the &reason for the revocation?";
      //
      // rbRetired
      //
      this.rbRetired.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbRetired.Location = new System.Drawing.Point(7, 88);
      this.rbRetired.Name = "rbRetired";
      this.rbRetired.Size = new System.Drawing.Size(426, 17);
      this.rbRetired.TabIndex = 3;
      this.rbRetired.Text = "The key is no longer used.";
      this.rbRetired.UseVisualStyleBackColor = true;
      //
      // rbCompromised
      //
      this.rbCompromised.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbCompromised.Location = new System.Drawing.Point(7, 65);
      this.rbCompromised.Name = "rbCompromised";
      this.rbCompromised.Size = new System.Drawing.Size(426, 17);
      this.rbCompromised.TabIndex = 2;
      this.rbCompromised.Text = "The key has been been compromised.";
      this.rbCompromised.UseVisualStyleBackColor = true;
      //
      // rbSuperceded
      //
      this.rbSuperceded.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbSuperceded.Location = new System.Drawing.Point(7, 42);
      this.rbSuperceded.Name = "rbSuperceded";
      this.rbSuperceded.Size = new System.Drawing.Size(426, 17);
      this.rbSuperceded.TabIndex = 1;
      this.rbSuperceded.Text = "The key has been superceded by a new key.";
      this.rbSuperceded.UseVisualStyleBackColor = true;
      //
      // rbNoReason
      //
      this.rbNoReason.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbNoReason.Checked = true;
      this.rbNoReason.Location = new System.Drawing.Point(7, 19);
      this.rbNoReason.Name = "rbNoReason";
      this.rbNoReason.Size = new System.Drawing.Size(426, 17);
      this.rbNoReason.TabIndex = 0;
      this.rbNoReason.TabStop = true;
      this.rbNoReason.Text = "No reason given.";
      this.rbNoReason.UseVisualStyleBackColor = true;
      //
      // btnOK
      //
      this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnOK.Location = new System.Drawing.Point(294, 516);
      this.btnOK.Name = "btnOK";
      this.btnOK.Size = new System.Drawing.Size(75, 23);
      this.btnOK.TabIndex = 7;
      this.btnOK.Text = "&OK";
      this.btnOK.UseVisualStyleBackColor = true;
      this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
      //
      // lblRevokedKey
      //
      this.lblRevokedKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.lblRevokedKey.Location = new System.Drawing.Point(8, 57);
      this.lblRevokedKey.Name = "lblRevokedKey";
      this.lblRevokedKey.Size = new System.Drawing.Size(442, 41);
      this.lblRevokedKey.TabIndex = 1;
      //
      // txtExplanation
      //
      this.txtExplanation.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtExplanation.Location = new System.Drawing.Point(8, 338);
      this.txtExplanation.MaxLength = 255;
      this.txtExplanation.Multiline = true;
      this.txtExplanation.Name = "txtExplanation";
      this.txtExplanation.Size = new System.Drawing.Size(442, 66);
      this.txtExplanation.TabIndex = 5;
      //
      // RevocationCertForm
      //
      this.AcceptButton = this.btnOK;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = btnCancel;
      this.ClientSize = new System.Drawing.Size(458, 545);
      this.Controls.Add(lblExplanation);
      this.Controls.Add(this.txtExplanation);
      this.Controls.Add(codeGroup);
      this.Controls.Add(this.btnOK);
      this.Controls.Add(btnCancel);
      this.Controls.Add(whereBox);
      this.Controls.Add(this.lblRevokedKey);
      this.Controls.Add(howBox);
      this.Controls.Add(lblHelp);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "RevocationCertForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Generate Revocation Certificate";
      howBox.ResumeLayout(false);
      whereBox.ResumeLayout(false);
      whereBox.PerformLayout();
      codeGroup.ResumeLayout(false);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.ComboBox revokingKeys;
    private System.Windows.Forms.RadioButton rbIndirect;
    private System.Windows.Forms.RadioButton rbDirect;
    private System.Windows.Forms.Label lblRevokedKey;
    private System.Windows.Forms.TextBox txtFile;
    private System.Windows.Forms.RadioButton rbFile;
    private System.Windows.Forms.RadioButton rbClipboard;
    private System.Windows.Forms.Button btnOK;
    private System.Windows.Forms.Button btnBrowse;
    private System.Windows.Forms.TextBox txtExplanation;
    private System.Windows.Forms.RadioButton rbSuperceded;
    private System.Windows.Forms.RadioButton rbNoReason;
    private System.Windows.Forms.RadioButton rbRetired;
    private System.Windows.Forms.RadioButton rbCompromised;

  }
}