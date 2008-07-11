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
  partial class KeyRevocationForm
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
      System.Windows.Forms.Label lblDescription;
      System.Windows.Forms.Label lblExplanation;
      System.Windows.Forms.GroupBox codeGroup;
      System.Windows.Forms.Button btnCancel;
      this.lblKey = new System.Windows.Forms.Label();
      this.txtExplanation = new System.Windows.Forms.TextBox();
      this.rbRetired = new System.Windows.Forms.RadioButton();
      this.rbCompromised = new System.Windows.Forms.RadioButton();
      this.rbSuperceded = new System.Windows.Forms.RadioButton();
      this.rbNoReason = new System.Windows.Forms.RadioButton();
      this.btnOK = new System.Windows.Forms.Button();
      lblDescription = new System.Windows.Forms.Label();
      lblExplanation = new System.Windows.Forms.Label();
      codeGroup = new System.Windows.Forms.GroupBox();
      btnCancel = new System.Windows.Forms.Button();
      codeGroup.SuspendLayout();
      this.SuspendLayout();
      // 
      // lblDescription
      // 
      lblDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      lblDescription.Location = new System.Drawing.Point(7, 5);
      lblDescription.Name = "lblDescription";
      lblDescription.Size = new System.Drawing.Size(302, 49);
      lblDescription.TabIndex = 0;
      lblDescription.Text = "Revoking a key makes it unusable. If the key was compromised, or no revocation re" +
    "ason is given, signatures made by the key will be considered untrustworthy.";
      // 
      // lblKey
      // 
      this.lblKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.lblKey.Location = new System.Drawing.Point(7, 54);
      this.lblKey.Name = "lblKey";
      this.lblKey.Size = new System.Drawing.Size(302, 44);
      this.lblKey.TabIndex = 1;
      this.lblKey.Text = "You are about to revoke the key:";
      // 
      // lblExplanation
      // 
      lblExplanation.Location = new System.Drawing.Point(5, 218);
      lblExplanation.Name = "lblExplanation";
      lblExplanation.Size = new System.Drawing.Size(286, 13);
      lblExplanation.TabIndex = 3;
      lblExplanation.Text = "Short explanation (optional):";
      lblExplanation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
      // 
      // txtExplanation
      // 
      this.txtExplanation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtExplanation.Location = new System.Drawing.Point(8, 234);
      this.txtExplanation.MaxLength = 255;
      this.txtExplanation.Multiline = true;
      this.txtExplanation.Name = "txtExplanation";
      this.txtExplanation.Size = new System.Drawing.Size(299, 66);
      this.txtExplanation.TabIndex = 4;
      // 
      // codeGroup
      // 
      codeGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      codeGroup.Controls.Add(this.rbRetired);
      codeGroup.Controls.Add(this.rbCompromised);
      codeGroup.Controls.Add(this.rbSuperceded);
      codeGroup.Controls.Add(this.rbNoReason);
      codeGroup.Location = new System.Drawing.Point(8, 101);
      codeGroup.Name = "codeGroup";
      codeGroup.Size = new System.Drawing.Size(299, 112);
      codeGroup.TabIndex = 2;
      codeGroup.TabStop = false;
      codeGroup.Text = "What is the reason for the revocation?";
      // 
      // rbRetired
      // 
      this.rbRetired.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbRetired.Location = new System.Drawing.Point(7, 88);
      this.rbRetired.Name = "rbRetired";
      this.rbRetired.Size = new System.Drawing.Size(283, 17);
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
      this.rbCompromised.Size = new System.Drawing.Size(283, 17);
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
      this.rbSuperceded.Size = new System.Drawing.Size(283, 17);
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
      this.rbNoReason.Size = new System.Drawing.Size(283, 17);
      this.rbNoReason.TabIndex = 0;
      this.rbNoReason.TabStop = true;
      this.rbNoReason.Text = "No reason given.";
      this.rbNoReason.UseVisualStyleBackColor = true;
      // 
      // btnOK
      // 
      this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.btnOK.Location = new System.Drawing.Point(151, 307);
      this.btnOK.Name = "btnOK";
      this.btnOK.Size = new System.Drawing.Size(75, 23);
      this.btnOK.TabIndex = 5;
      this.btnOK.Text = "&Revoke";
      this.btnOK.UseVisualStyleBackColor = true;
      // 
      // btnCancel
      // 
      btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      btnCancel.Location = new System.Drawing.Point(232, 307);
      btnCancel.Name = "btnCancel";
      btnCancel.Size = new System.Drawing.Size(75, 23);
      btnCancel.TabIndex = 6;
      btnCancel.Text = "&Cancel";
      btnCancel.UseVisualStyleBackColor = true;
      // 
      // KeyRevocationForm
      // 
      this.AcceptButton = this.btnOK;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = btnCancel;
      this.ClientSize = new System.Drawing.Size(316, 336);
      this.Controls.Add(this.btnOK);
      this.Controls.Add(btnCancel);
      this.Controls.Add(lblExplanation);
      this.Controls.Add(this.txtExplanation);
      this.Controls.Add(codeGroup);
      this.Controls.Add(this.lblKey);
      this.Controls.Add(lblDescription);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "KeyRevocationForm";
      this.ShowIcon = false;
      this.Text = "Key Revocation";
      codeGroup.ResumeLayout(false);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label lblKey;
    private System.Windows.Forms.TextBox txtExplanation;
    private System.Windows.Forms.RadioButton rbRetired;
    private System.Windows.Forms.RadioButton rbCompromised;
    private System.Windows.Forms.RadioButton rbSuperceded;
    private System.Windows.Forms.RadioButton rbNoReason;
    private System.Windows.Forms.Button btnOK;
  }
}