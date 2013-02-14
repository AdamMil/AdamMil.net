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

namespace AdamMil.Security.UI
{
  partial class UserRevocationForm
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
      System.Windows.Forms.GroupBox codeGroup;
      System.Windows.Forms.Label lblUserIds;
      System.Windows.Forms.Label label1;
      System.Windows.Forms.Button btnCancel;
      System.Windows.Forms.Button btnRevoke;
      this.rbInvalid = new System.Windows.Forms.RadioButton();
      this.rbNoReason = new System.Windows.Forms.RadioButton();
      this.ids = new System.Windows.Forms.ListBox();
      this.txtExplanation = new System.Windows.Forms.TextBox();
      lblDescription = new System.Windows.Forms.Label();
      codeGroup = new System.Windows.Forms.GroupBox();
      lblUserIds = new System.Windows.Forms.Label();
      label1 = new System.Windows.Forms.Label();
      btnCancel = new System.Windows.Forms.Button();
      btnRevoke = new System.Windows.Forms.Button();
      codeGroup.SuspendLayout();
      this.SuspendLayout();
      //
      // lblDescription
      //
      lblDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      lblDescription.Location = new System.Drawing.Point(5, 5);
      lblDescription.Name = "lblDescription";
      lblDescription.Size = new System.Drawing.Size(317, 52);
      lblDescription.TabIndex = 0;
      lblDescription.Text = "Revoking a user ID is a statement that the user ID no longer valid or no longer u" +
    "sed with this key. Once distributed, a revoked user ID cannot be restored.";
      //
      // codeGroup
      //
      codeGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      codeGroup.Controls.Add(this.rbInvalid);
      codeGroup.Controls.Add(this.rbNoReason);
      codeGroup.Location = new System.Drawing.Point(8, 138);
      codeGroup.Name = "codeGroup";
      codeGroup.Size = new System.Drawing.Size(315, 68);
      codeGroup.TabIndex = 3;
      codeGroup.TabStop = false;
      codeGroup.Text = "What is the reason for the revocation?";
      //
      // rbInvalid
      //
      this.rbInvalid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbInvalid.Location = new System.Drawing.Point(7, 42);
      this.rbInvalid.Name = "rbInvalid";
      this.rbInvalid.Size = new System.Drawing.Size(299, 17);
      this.rbInvalid.TabIndex = 5;
      this.rbInvalid.Text = "The user IDs are no longer valid.";
      this.rbInvalid.UseVisualStyleBackColor = true;
      //
      // rbNoReason
      //
      this.rbNoReason.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbNoReason.Checked = true;
      this.rbNoReason.Location = new System.Drawing.Point(7, 19);
      this.rbNoReason.Name = "rbNoReason";
      this.rbNoReason.Size = new System.Drawing.Size(299, 17);
      this.rbNoReason.TabIndex = 4;
      this.rbNoReason.TabStop = true;
      this.rbNoReason.Text = "No reason given.";
      this.rbNoReason.UseVisualStyleBackColor = true;
      //
      // lblUserIds
      //
      lblUserIds.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      lblUserIds.Location = new System.Drawing.Point(6, 57);
      lblUserIds.Name = "lblUserIds";
      lblUserIds.Size = new System.Drawing.Size(316, 13);
      lblUserIds.TabIndex = 1;
      lblUserIds.Text = "You are about to revoke these user IDs:";
      lblUserIds.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
      //
      // label1
      //
      label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      label1.Location = new System.Drawing.Point(5, 213);
      label1.Name = "label1";
      label1.Size = new System.Drawing.Size(316, 13);
      label1.TabIndex = 5;
      label1.Text = "Short explanation (optional):";
      label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
      //
      // btnCancel
      //
      btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      btnCancel.Location = new System.Drawing.Point(247, 300);
      btnCancel.Name = "btnCancel";
      btnCancel.Size = new System.Drawing.Size(75, 23);
      btnCancel.TabIndex = 8;
      btnCancel.Text = "&Cancel";
      btnCancel.UseVisualStyleBackColor = true;
      //
      // btnRevoke
      //
      btnRevoke.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnRevoke.DialogResult = System.Windows.Forms.DialogResult.OK;
      btnRevoke.Location = new System.Drawing.Point(166, 300);
      btnRevoke.Name = "btnRevoke";
      btnRevoke.Size = new System.Drawing.Size(75, 23);
      btnRevoke.TabIndex = 7;
      btnRevoke.Text = "&Revoke";
      btnRevoke.UseVisualStyleBackColor = true;
      //
      // ids
      //
      this.ids.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.ids.FormattingEnabled = true;
      this.ids.Location = new System.Drawing.Point(8, 76);
      this.ids.Name = "ids";
      this.ids.SelectionMode = System.Windows.Forms.SelectionMode.None;
      this.ids.Size = new System.Drawing.Size(315, 56);
      this.ids.TabIndex = 2;
      //
      // txtExplanation
      //
      this.txtExplanation.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtExplanation.Location = new System.Drawing.Point(8, 228);
      this.txtExplanation.MaxLength = 255;
      this.txtExplanation.Multiline = true;
      this.txtExplanation.Name = "txtExplanation";
      this.txtExplanation.Size = new System.Drawing.Size(314, 65);
      this.txtExplanation.TabIndex = 6;
      //
      // UserRevocationForm
      //
      this.AcceptButton = btnRevoke;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = btnCancel;
      this.ClientSize = new System.Drawing.Size(331, 329);
      this.Controls.Add(btnRevoke);
      this.Controls.Add(btnCancel);
      this.Controls.Add(label1);
      this.Controls.Add(this.txtExplanation);
      this.Controls.Add(lblUserIds);
      this.Controls.Add(this.ids);
      this.Controls.Add(codeGroup);
      this.Controls.Add(lblDescription);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "UserRevocationForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "User ID Revocation";
      codeGroup.ResumeLayout(false);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.RadioButton rbInvalid;
    private System.Windows.Forms.RadioButton rbNoReason;
    private System.Windows.Forms.ListBox ids;
    private System.Windows.Forms.TextBox txtExplanation;
  }
}