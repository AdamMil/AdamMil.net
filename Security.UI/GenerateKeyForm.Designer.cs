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
  partial class GenerateKeyForm
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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GenerateKeyForm));
      System.Windows.Forms.Label lblKeyId;
      System.Windows.Forms.Label lblType;
      System.Windows.Forms.Label lblPrimaryUser;
      System.Windows.Forms.Label lblPassword;
      System.Windows.Forms.Label lblRepeat;
      System.Windows.Forms.Label lblKeyType;
      System.Windows.Forms.Label lblKeyLength;
      System.Windows.Forms.Label lblExpiration;
      System.Windows.Forms.Label lblSubExpire;
      System.Windows.Forms.Label lblSubLength;
      System.Windows.Forms.Label lblSubType;
      System.Windows.Forms.Label lblGenHelp;
      this.grpUser = new System.Windows.Forms.GroupBox();
      this.lblUserId = new System.Windows.Forms.Label();
      this.txtComment = new System.Windows.Forms.TextBox();
      this.txtEmail = new System.Windows.Forms.TextBox();
      this.txtName = new System.Windows.Forms.TextBox();
      this.grpPassword = new System.Windows.Forms.GroupBox();
      this.lblStrength = new System.Windows.Forms.Label();
      this.txtPass2 = new AdamMil.Security.UI.SecureTextBox();
      this.txtPass1 = new AdamMil.Security.UI.SecureTextBox();
      this.grpPrimary = new System.Windows.Forms.GroupBox();
      this.chkKeyNoExpiration = new System.Windows.Forms.CheckBox();
      this.keyExpiration = new System.Windows.Forms.DateTimePicker();
      this.keyLength = new System.Windows.Forms.ComboBox();
      this.keyType = new System.Windows.Forms.ComboBox();
      this.grpSubkey = new System.Windows.Forms.GroupBox();
      this.chkSubkeyNoExpiration = new System.Windows.Forms.CheckBox();
      this.subkeyExpiration = new System.Windows.Forms.DateTimePicker();
      this.subkeyLength = new System.Windows.Forms.ComboBox();
      this.subkeyType = new System.Windows.Forms.ComboBox();
      this.progressBar = new System.Windows.Forms.ProgressBar();
      this.btnCancel = new System.Windows.Forms.Button();
      this.btnGenerate = new System.Windows.Forms.Button();
      lblHelp = new System.Windows.Forms.Label();
      lblKeyId = new System.Windows.Forms.Label();
      lblType = new System.Windows.Forms.Label();
      lblPrimaryUser = new System.Windows.Forms.Label();
      lblPassword = new System.Windows.Forms.Label();
      lblRepeat = new System.Windows.Forms.Label();
      lblKeyType = new System.Windows.Forms.Label();
      lblKeyLength = new System.Windows.Forms.Label();
      lblExpiration = new System.Windows.Forms.Label();
      lblSubExpire = new System.Windows.Forms.Label();
      lblSubLength = new System.Windows.Forms.Label();
      lblSubType = new System.Windows.Forms.Label();
      lblGenHelp = new System.Windows.Forms.Label();
      this.grpUser.SuspendLayout();
      this.grpPassword.SuspendLayout();
      this.grpPrimary.SuspendLayout();
      this.grpSubkey.SuspendLayout();
      this.SuspendLayout();
      //
      // lblHelp
      //
      lblHelp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      lblHelp.Location = new System.Drawing.Point(8, 7);
      lblHelp.Name = "lblHelp";
      lblHelp.Size = new System.Drawing.Size(697, 47);
      lblHelp.TabIndex = 12;
      lblHelp.Text = resources.GetString("lblHelp.Text");
      //
      // lblKeyId
      //
      lblKeyId.Location = new System.Drawing.Point(7, 46);
      lblKeyId.Name = "lblKeyId";
      lblKeyId.Size = new System.Drawing.Size(79, 20);
      lblKeyId.TabIndex = 8;
      lblKeyId.Text = "Email";
      lblKeyId.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblType
      //
      lblType.Location = new System.Drawing.Point(7, 72);
      lblType.Name = "lblType";
      lblType.Size = new System.Drawing.Size(79, 20);
      lblType.TabIndex = 10;
      lblType.Text = "Comment";
      lblType.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblPrimaryUser
      //
      lblPrimaryUser.Location = new System.Drawing.Point(7, 20);
      lblPrimaryUser.Name = "lblPrimaryUser";
      lblPrimaryUser.Size = new System.Drawing.Size(79, 20);
      lblPrimaryUser.TabIndex = 6;
      lblPrimaryUser.Text = "Real Name";
      lblPrimaryUser.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblPassword
      //
      lblPassword.Location = new System.Drawing.Point(6, 20);
      lblPassword.Name = "lblPassword";
      lblPassword.Size = new System.Drawing.Size(107, 20);
      lblPassword.TabIndex = 9;
      lblPassword.Text = "Password";
      lblPassword.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblRepeat
      //
      lblRepeat.Location = new System.Drawing.Point(6, 46);
      lblRepeat.Name = "lblRepeat";
      lblRepeat.Size = new System.Drawing.Size(107, 20);
      lblRepeat.TabIndex = 10;
      lblRepeat.Text = "Repeat Password";
      lblRepeat.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblKeyType
      //
      lblKeyType.Location = new System.Drawing.Point(7, 17);
      lblKeyType.Name = "lblKeyType";
      lblKeyType.Size = new System.Drawing.Size(79, 20);
      lblKeyType.TabIndex = 7;
      lblKeyType.Text = "Key Type";
      lblKeyType.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblKeyLength
      //
      lblKeyLength.Location = new System.Drawing.Point(7, 43);
      lblKeyLength.Name = "lblKeyLength";
      lblKeyLength.Size = new System.Drawing.Size(79, 20);
      lblKeyLength.TabIndex = 9;
      lblKeyLength.Text = "Key Size";
      lblKeyLength.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblExpiration
      //
      lblExpiration.Location = new System.Drawing.Point(7, 69);
      lblExpiration.Name = "lblExpiration";
      lblExpiration.Size = new System.Drawing.Size(79, 20);
      lblExpiration.TabIndex = 11;
      lblExpiration.Text = "Expiration";
      lblExpiration.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblSubExpire
      //
      lblSubExpire.Location = new System.Drawing.Point(7, 69);
      lblSubExpire.Name = "lblSubExpire";
      lblSubExpire.Size = new System.Drawing.Size(79, 20);
      lblSubExpire.TabIndex = 11;
      lblSubExpire.Text = "Expiration";
      lblSubExpire.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblSubLength
      //
      lblSubLength.Location = new System.Drawing.Point(7, 43);
      lblSubLength.Name = "lblSubLength";
      lblSubLength.Size = new System.Drawing.Size(79, 20);
      lblSubLength.TabIndex = 9;
      lblSubLength.Text = "Key Size";
      lblSubLength.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblSubType
      //
      lblSubType.Location = new System.Drawing.Point(7, 17);
      lblSubType.Name = "lblSubType";
      lblSubType.Size = new System.Drawing.Size(79, 20);
      lblSubType.TabIndex = 7;
      lblSubType.Text = "Key Type";
      lblSubType.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblGenHelp
      //
      lblGenHelp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      lblGenHelp.Location = new System.Drawing.Point(5, 345);
      lblGenHelp.Name = "lblGenHelp";
      lblGenHelp.Size = new System.Drawing.Size(538, 26);
      lblGenHelp.TabIndex = 20;
      lblGenHelp.Text = "Key generation can take several minutes. Actively typing and performing disk-inte" +
    "nsive operations can help speed up the process.";
      //
      // grpUser
      //
      this.grpUser.Controls.Add(this.lblUserId);
      this.grpUser.Controls.Add(this.txtComment);
      this.grpUser.Controls.Add(this.txtEmail);
      this.grpUser.Controls.Add(this.txtName);
      this.grpUser.Controls.Add(lblKeyId);
      this.grpUser.Controls.Add(lblType);
      this.grpUser.Controls.Add(lblPrimaryUser);
      this.grpUser.Location = new System.Drawing.Point(8, 58);
      this.grpUser.Name = "grpUser";
      this.grpUser.Size = new System.Drawing.Size(344, 144);
      this.grpUser.TabIndex = 13;
      this.grpUser.TabStop = false;
      this.grpUser.Text = "Enter the information you want others to see";
      //
      // lblUserId
      //
      this.lblUserId.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.lblUserId.Location = new System.Drawing.Point(7, 99);
      this.lblUserId.Name = "lblUserId";
      this.lblUserId.Size = new System.Drawing.Size(330, 42);
      this.lblUserId.TabIndex = 12;
      this.lblUserId.Text = "Please enter your new user ID above.";
      //
      // txtComment
      //
      this.txtComment.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtComment.Location = new System.Drawing.Point(88, 72);
      this.txtComment.Name = "txtComment";
      this.txtComment.Size = new System.Drawing.Size(244, 21);
      this.txtComment.TabIndex = 11;
      this.txtComment.TextChanged += new System.EventHandler(this.userId_TextChanged);
      //
      // txtEmail
      //
      this.txtEmail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtEmail.Location = new System.Drawing.Point(88, 46);
      this.txtEmail.Name = "txtEmail";
      this.txtEmail.Size = new System.Drawing.Size(244, 21);
      this.txtEmail.TabIndex = 9;
      this.txtEmail.TextChanged += new System.EventHandler(this.userId_TextChanged);
      //
      // txtName
      //
      this.txtName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtName.Location = new System.Drawing.Point(88, 20);
      this.txtName.Name = "txtName";
      this.txtName.Size = new System.Drawing.Size(244, 21);
      this.txtName.TabIndex = 7;
      this.txtName.TextChanged += new System.EventHandler(this.userId_TextChanged);
      //
      // grpPassword
      //
      this.grpPassword.Controls.Add(this.lblStrength);
      this.grpPassword.Controls.Add(this.txtPass2);
      this.grpPassword.Controls.Add(this.txtPass1);
      this.grpPassword.Controls.Add(lblRepeat);
      this.grpPassword.Controls.Add(lblPassword);
      this.grpPassword.Location = new System.Drawing.Point(361, 58);
      this.grpPassword.Name = "grpPassword";
      this.grpPassword.Size = new System.Drawing.Size(344, 144);
      this.grpPassword.TabIndex = 14;
      this.grpPassword.TabStop = false;
      this.grpPassword.Text = "Select a strong password to protect your secret key";
      //
      // lblStrength
      //
      this.lblStrength.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.lblStrength.Location = new System.Drawing.Point(6, 74);
      this.lblStrength.Name = "lblStrength";
      this.lblStrength.Size = new System.Drawing.Size(332, 34);
      this.lblStrength.TabIndex = 13;
      this.lblStrength.Text = "Estimated password strength:";
      //
      // txtPass2
      //
      this.txtPass2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtPass2.ImeMode = System.Windows.Forms.ImeMode.Disable;
      this.txtPass2.Location = new System.Drawing.Point(115, 46);
      this.txtPass2.Name = "txtPass2";
      this.txtPass2.Size = new System.Drawing.Size(218, 21);
      this.txtPass2.TabIndex = 12;
      this.txtPass2.UseSystemPasswordChar = true;
      //
      // txtPass1
      //
      this.txtPass1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtPass1.ImeMode = System.Windows.Forms.ImeMode.Disable;
      this.txtPass1.Location = new System.Drawing.Point(115, 20);
      this.txtPass1.Name = "txtPass1";
      this.txtPass1.Size = new System.Drawing.Size(218, 21);
      this.txtPass1.TabIndex = 11;
      this.txtPass1.UseSystemPasswordChar = true;
      this.txtPass1.TextChanged += new System.EventHandler(this.txtPass1_TextChanged);
      //
      // grpPrimary
      //
      this.grpPrimary.Controls.Add(this.chkKeyNoExpiration);
      this.grpPrimary.Controls.Add(this.keyExpiration);
      this.grpPrimary.Controls.Add(lblExpiration);
      this.grpPrimary.Controls.Add(this.keyLength);
      this.grpPrimary.Controls.Add(lblKeyLength);
      this.grpPrimary.Controls.Add(this.keyType);
      this.grpPrimary.Controls.Add(lblKeyType);
      this.grpPrimary.Location = new System.Drawing.Point(8, 208);
      this.grpPrimary.Name = "grpPrimary";
      this.grpPrimary.Size = new System.Drawing.Size(344, 100);
      this.grpPrimary.TabIndex = 15;
      this.grpPrimary.TabStop = false;
      this.grpPrimary.Text = "&Primary key type (typically signing-only)";
      //
      // chkKeyNoExpiration
      //
      this.chkKeyNoExpiration.Checked = true;
      this.chkKeyNoExpiration.CheckState = System.Windows.Forms.CheckState.Checked;
      this.chkKeyNoExpiration.Location = new System.Drawing.Point(220, 72);
      this.chkKeyNoExpiration.Name = "chkKeyNoExpiration";
      this.chkKeyNoExpiration.Size = new System.Drawing.Size(117, 17);
      this.chkKeyNoExpiration.TabIndex = 13;
      this.chkKeyNoExpiration.Text = "No expiration";
      this.chkKeyNoExpiration.UseVisualStyleBackColor = true;
      this.chkKeyNoExpiration.CheckedChanged += new System.EventHandler(this.chkKeyNoExpiration_CheckedChanged);
      //
      // keyExpiration
      //
      this.keyExpiration.Enabled = false;
      this.keyExpiration.Format = System.Windows.Forms.DateTimePickerFormat.Short;
      this.keyExpiration.Location = new System.Drawing.Point(92, 69);
      this.keyExpiration.Name = "keyExpiration";
      this.keyExpiration.Size = new System.Drawing.Size(121, 21);
      this.keyExpiration.TabIndex = 12;
      //
      // keyLength
      //
      this.keyLength.FormattingEnabled = true;
      this.keyLength.Location = new System.Drawing.Point(92, 43);
      this.keyLength.Name = "keyLength";
      this.keyLength.Size = new System.Drawing.Size(121, 21);
      this.keyLength.TabIndex = 10;
      //
      // keyType
      //
      this.keyType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.keyType.FormattingEnabled = true;
      this.keyType.Location = new System.Drawing.Point(92, 17);
      this.keyType.Name = "keyType";
      this.keyType.Size = new System.Drawing.Size(240, 21);
      this.keyType.TabIndex = 8;
      this.keyType.SelectedIndexChanged += new System.EventHandler(this.keyType_SelectedIndexChanged);
      //
      // grpSubkey
      //
      this.grpSubkey.Controls.Add(this.chkSubkeyNoExpiration);
      this.grpSubkey.Controls.Add(this.subkeyExpiration);
      this.grpSubkey.Controls.Add(lblSubExpire);
      this.grpSubkey.Controls.Add(this.subkeyLength);
      this.grpSubkey.Controls.Add(lblSubLength);
      this.grpSubkey.Controls.Add(this.subkeyType);
      this.grpSubkey.Controls.Add(lblSubType);
      this.grpSubkey.Location = new System.Drawing.Point(361, 208);
      this.grpSubkey.Name = "grpSubkey";
      this.grpSubkey.Size = new System.Drawing.Size(344, 100);
      this.grpSubkey.TabIndex = 16;
      this.grpSubkey.TabStop = false;
      this.grpSubkey.Text = "&Subkey type (typically encryption-only)";
      //
      // chkSubkeyNoExpiration
      //
      this.chkSubkeyNoExpiration.Location = new System.Drawing.Point(220, 72);
      this.chkSubkeyNoExpiration.Name = "chkSubkeyNoExpiration";
      this.chkSubkeyNoExpiration.Size = new System.Drawing.Size(117, 17);
      this.chkSubkeyNoExpiration.TabIndex = 13;
      this.chkSubkeyNoExpiration.Text = "No expiration";
      this.chkSubkeyNoExpiration.UseVisualStyleBackColor = true;
      this.chkSubkeyNoExpiration.CheckedChanged += new System.EventHandler(this.chkSubkeyNoExpiration_CheckedChanged);
      //
      // subkeyExpiration
      //
      this.subkeyExpiration.Format = System.Windows.Forms.DateTimePickerFormat.Short;
      this.subkeyExpiration.Location = new System.Drawing.Point(92, 69);
      this.subkeyExpiration.Name = "subkeyExpiration";
      this.subkeyExpiration.Size = new System.Drawing.Size(121, 21);
      this.subkeyExpiration.TabIndex = 12;
      //
      // subkeyLength
      //
      this.subkeyLength.FormattingEnabled = true;
      this.subkeyLength.Location = new System.Drawing.Point(92, 43);
      this.subkeyLength.Name = "subkeyLength";
      this.subkeyLength.Size = new System.Drawing.Size(121, 21);
      this.subkeyLength.TabIndex = 10;
      //
      // subkeyType
      //
      this.subkeyType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.subkeyType.FormattingEnabled = true;
      this.subkeyType.Location = new System.Drawing.Point(92, 17);
      this.subkeyType.Name = "subkeyType";
      this.subkeyType.Size = new System.Drawing.Size(241, 21);
      this.subkeyType.TabIndex = 8;
      this.subkeyType.SelectedIndexChanged += new System.EventHandler(this.subkeyType_SelectedIndexChanged);
      //
      // progressBar
      //
      this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.progressBar.Location = new System.Drawing.Point(8, 316);
      this.progressBar.Name = "progressBar";
      this.progressBar.Size = new System.Drawing.Size(697, 23);
      this.progressBar.TabIndex = 17;
      //
      // btnCancel
      //
      this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnCancel.Location = new System.Drawing.Point(630, 347);
      this.btnCancel.Name = "btnCancel";
      this.btnCancel.Size = new System.Drawing.Size(75, 23);
      this.btnCancel.TabIndex = 18;
      this.btnCancel.Text = "&Close";
      this.btnCancel.UseVisualStyleBackColor = true;
      this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
      //
      // btnGenerate
      //
      this.btnGenerate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnGenerate.Enabled = false;
      this.btnGenerate.Location = new System.Drawing.Point(549, 347);
      this.btnGenerate.Name = "btnGenerate";
      this.btnGenerate.Size = new System.Drawing.Size(75, 23);
      this.btnGenerate.TabIndex = 19;
      this.btnGenerate.Text = "&Generate";
      this.btnGenerate.UseVisualStyleBackColor = true;
      this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);
      //
      // GenerateKeyForm
      //
      this.AcceptButton = this.btnGenerate;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(713, 376);
      this.Controls.Add(lblGenHelp);
      this.Controls.Add(this.btnGenerate);
      this.Controls.Add(this.btnCancel);
      this.Controls.Add(this.progressBar);
      this.Controls.Add(this.grpSubkey);
      this.Controls.Add(this.grpPrimary);
      this.Controls.Add(this.grpPassword);
      this.Controls.Add(this.grpUser);
      this.Controls.Add(lblHelp);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "GenerateKeyForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Generate Key Pair";
      this.grpUser.ResumeLayout(false);
      this.grpUser.PerformLayout();
      this.grpPassword.ResumeLayout(false);
      this.grpPassword.PerformLayout();
      this.grpPrimary.ResumeLayout(false);
      this.grpSubkey.ResumeLayout(false);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.TextBox txtComment;
    private System.Windows.Forms.TextBox txtEmail;
    private System.Windows.Forms.TextBox txtName;
    private System.Windows.Forms.Label lblUserId;
    private System.Windows.Forms.Label lblStrength;
    private SecureTextBox txtPass2;
    private SecureTextBox txtPass1;
    private System.Windows.Forms.ComboBox keyLength;
    private System.Windows.Forms.ComboBox keyType;
    private System.Windows.Forms.CheckBox chkKeyNoExpiration;
    private System.Windows.Forms.DateTimePicker keyExpiration;
    private System.Windows.Forms.CheckBox chkSubkeyNoExpiration;
    private System.Windows.Forms.DateTimePicker subkeyExpiration;
    private System.Windows.Forms.ComboBox subkeyLength;
    private System.Windows.Forms.ComboBox subkeyType;
    private System.Windows.Forms.ProgressBar progressBar;
    private System.Windows.Forms.Button btnGenerate;
    private System.Windows.Forms.Button btnCancel;
    private System.Windows.Forms.GroupBox grpUser;
    private System.Windows.Forms.GroupBox grpPassword;
    private System.Windows.Forms.GroupBox grpPrimary;
    private System.Windows.Forms.GroupBox grpSubkey;

  }
}