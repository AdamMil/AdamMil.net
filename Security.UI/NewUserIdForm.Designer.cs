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
  partial class UserIdForm
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
      System.Windows.Forms.Label lblKeyId;
      System.Windows.Forms.Label lblType;
      System.Windows.Forms.Label lblPrimaryUser;
      System.Windows.Forms.Button btnCancel;
      System.Windows.Forms.Button btnOK;
      this.txtComment = new System.Windows.Forms.TextBox();
      this.txtEmail = new System.Windows.Forms.TextBox();
      this.txtName = new System.Windows.Forms.TextBox();
      this.lblHelp = new System.Windows.Forms.Label();
      this.chkPrimary = new System.Windows.Forms.CheckBox();
      lblKeyId = new System.Windows.Forms.Label();
      lblType = new System.Windows.Forms.Label();
      lblPrimaryUser = new System.Windows.Forms.Label();
      btnCancel = new System.Windows.Forms.Button();
      btnOK = new System.Windows.Forms.Button();
      this.SuspendLayout();
      //
      // txtComment
      //
      this.txtComment.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtComment.Location = new System.Drawing.Point(90, 60);
      this.txtComment.Name = "txtComment";
      this.txtComment.Size = new System.Drawing.Size(240, 21);
      this.txtComment.TabIndex = 5;
      this.txtComment.TextChanged += new System.EventHandler(this.txtUserId_TextChanged);
      //
      // txtEmail
      //
      this.txtEmail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtEmail.Location = new System.Drawing.Point(90, 34);
      this.txtEmail.Name = "txtEmail";
      this.txtEmail.Size = new System.Drawing.Size(240, 21);
      this.txtEmail.TabIndex = 3;
      this.txtEmail.TextChanged += new System.EventHandler(this.txtUserId_TextChanged);
      //
      // txtName
      //
      this.txtName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtName.Location = new System.Drawing.Point(90, 8);
      this.txtName.Name = "txtName";
      this.txtName.Size = new System.Drawing.Size(240, 21);
      this.txtName.TabIndex = 1;
      this.txtName.TextChanged += new System.EventHandler(this.txtUserId_TextChanged);
      //
      // lblKeyId
      //
      lblKeyId.Location = new System.Drawing.Point(3, 34);
      lblKeyId.Name = "lblKeyId";
      lblKeyId.Size = new System.Drawing.Size(81, 20);
      lblKeyId.TabIndex = 2;
      lblKeyId.Text = "Email";
      lblKeyId.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblType
      //
      lblType.Location = new System.Drawing.Point(3, 60);
      lblType.Name = "lblType";
      lblType.Size = new System.Drawing.Size(81, 20);
      lblType.TabIndex = 4;
      lblType.Text = "Comment";
      lblType.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblPrimaryUser
      //
      lblPrimaryUser.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      lblPrimaryUser.Location = new System.Drawing.Point(3, 8);
      lblPrimaryUser.Name = "lblPrimaryUser";
      lblPrimaryUser.Size = new System.Drawing.Size(81, 20);
      lblPrimaryUser.TabIndex = 0;
      lblPrimaryUser.Text = "Real Name";
      lblPrimaryUser.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblHelp
      //
      this.lblHelp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.lblHelp.Location = new System.Drawing.Point(6, 108);
      this.lblHelp.Name = "lblHelp";
      this.lblHelp.Size = new System.Drawing.Size(324, 43);
      this.lblHelp.TabIndex = 7;
      //
      // btnCancel
      //
      btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      btnCancel.Location = new System.Drawing.Point(255, 154);
      btnCancel.Name = "btnCancel";
      btnCancel.Size = new System.Drawing.Size(75, 23);
      btnCancel.TabIndex = 9;
      btnCancel.Text = "&Cancel";
      btnCancel.UseVisualStyleBackColor = true;
      //
      // btnOK
      //
      btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnOK.Location = new System.Drawing.Point(174, 154);
      btnOK.Name = "btnOK";
      btnOK.Size = new System.Drawing.Size(75, 23);
      btnOK.TabIndex = 8;
      btnOK.Text = "&OK";
      btnOK.UseVisualStyleBackColor = true;
      btnOK.Click += new System.EventHandler(this.btnOK_Click);
      //
      // chkPrimary
      //
      this.chkPrimary.Checked = true;
      this.chkPrimary.CheckState = System.Windows.Forms.CheckState.Checked;
      this.chkPrimary.Location = new System.Drawing.Point(90, 87);
      this.chkPrimary.Name = "chkPrimary";
      this.chkPrimary.Size = new System.Drawing.Size(240, 17);
      this.chkPrimary.TabIndex = 6;
      this.chkPrimary.Text = "Make this my primary user ID";
      this.chkPrimary.UseVisualStyleBackColor = true;
      //
      // UserIdForm
      //
      this.AcceptButton = btnOK;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = btnCancel;
      this.ClientSize = new System.Drawing.Size(338, 185);
      this.Controls.Add(this.chkPrimary);
      this.Controls.Add(btnOK);
      this.Controls.Add(btnCancel);
      this.Controls.Add(this.lblHelp);
      this.Controls.Add(this.txtComment);
      this.Controls.Add(this.txtEmail);
      this.Controls.Add(this.txtName);
      this.Controls.Add(lblKeyId);
      this.Controls.Add(lblType);
      this.Controls.Add(lblPrimaryUser);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "UserIdForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "New User Id";
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TextBox txtComment;
    private System.Windows.Forms.TextBox txtEmail;
    private System.Windows.Forms.TextBox txtName;
    private System.Windows.Forms.Label lblHelp;
    private System.Windows.Forms.CheckBox chkPrimary;
  }
}