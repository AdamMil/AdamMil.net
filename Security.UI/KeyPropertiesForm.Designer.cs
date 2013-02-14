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
  partial class KeyPropertiesForm
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
      System.Windows.Forms.Label lblPrimaryUser;
      System.Windows.Forms.Label lblFingerprint;
      System.Windows.Forms.Label lblTrust;
      System.Windows.Forms.Label lblValidity;
      System.Windows.Forms.Label lblType;
      System.Windows.Forms.Label lblKeyId;
      System.Windows.Forms.Label lblCapabilities;
      this.txtPrimaryId = new System.Windows.Forms.TextBox();
      this.txtKeyId = new System.Windows.Forms.TextBox();
      this.txtFingerprint = new System.Windows.Forms.TextBox();
      this.txtOwnerTrust = new System.Windows.Forms.TextBox();
      this.txtKeyValidity = new System.Windows.Forms.TextBox();
      this.txtKeyType = new System.Windows.Forms.TextBox();
      this.keyList = new AdamMil.Security.UI.SimpleKeyList();
      this.txtCapabilities = new System.Windows.Forms.TextBox();
      lblPrimaryUser = new System.Windows.Forms.Label();
      lblFingerprint = new System.Windows.Forms.Label();
      lblTrust = new System.Windows.Forms.Label();
      lblValidity = new System.Windows.Forms.Label();
      lblType = new System.Windows.Forms.Label();
      lblKeyId = new System.Windows.Forms.Label();
      lblCapabilities = new System.Windows.Forms.Label();
      this.SuspendLayout();
      //
      // lblPrimaryUser
      //
      lblPrimaryUser.Location = new System.Drawing.Point(9, 8);
      lblPrimaryUser.Name = "lblPrimaryUser";
      lblPrimaryUser.Size = new System.Drawing.Size(104, 20);
      lblPrimaryUser.TabIndex = 0;
      lblPrimaryUser.Text = "Primary User ID";
      lblPrimaryUser.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblFingerprint
      //
      lblFingerprint.Location = new System.Drawing.Point(9, 166);
      lblFingerprint.Name = "lblFingerprint";
      lblFingerprint.Size = new System.Drawing.Size(104, 20);
      lblFingerprint.TabIndex = 10;
      lblFingerprint.Text = "Fingerprint";
      lblFingerprint.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblTrust
      //
      lblTrust.Location = new System.Drawing.Point(9, 140);
      lblTrust.Name = "lblTrust";
      lblTrust.Size = new System.Drawing.Size(104, 20);
      lblTrust.TabIndex = 8;
      lblTrust.Text = "Owner Trust";
      lblTrust.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblValidity
      //
      lblValidity.Location = new System.Drawing.Point(9, 114);
      lblValidity.Name = "lblValidity";
      lblValidity.Size = new System.Drawing.Size(104, 20);
      lblValidity.TabIndex = 6;
      lblValidity.Text = "Key Validity";
      lblValidity.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblType
      //
      lblType.Location = new System.Drawing.Point(9, 60);
      lblType.Name = "lblType";
      lblType.Size = new System.Drawing.Size(104, 20);
      lblType.TabIndex = 4;
      lblType.Text = "Type";
      lblType.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblKeyId
      //
      lblKeyId.Location = new System.Drawing.Point(9, 34);
      lblKeyId.Name = "lblKeyId";
      lblKeyId.Size = new System.Drawing.Size(104, 20);
      lblKeyId.TabIndex = 2;
      lblKeyId.Text = "Key ID";
      lblKeyId.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // txtPrimaryId
      //
      this.txtPrimaryId.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtPrimaryId.Location = new System.Drawing.Point(117, 8);
      this.txtPrimaryId.Name = "txtPrimaryId";
      this.txtPrimaryId.ReadOnly = true;
      this.txtPrimaryId.Size = new System.Drawing.Size(396, 21);
      this.txtPrimaryId.TabIndex = 1;
      //
      // txtKeyId
      //
      this.txtKeyId.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtKeyId.Location = new System.Drawing.Point(117, 34);
      this.txtKeyId.Name = "txtKeyId";
      this.txtKeyId.ReadOnly = true;
      this.txtKeyId.Size = new System.Drawing.Size(396, 21);
      this.txtKeyId.TabIndex = 3;
      //
      // txtFingerprint
      //
      this.txtFingerprint.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtFingerprint.Location = new System.Drawing.Point(117, 166);
      this.txtFingerprint.Name = "txtFingerprint";
      this.txtFingerprint.ReadOnly = true;
      this.txtFingerprint.Size = new System.Drawing.Size(396, 21);
      this.txtFingerprint.TabIndex = 11;
      //
      // txtOwnerTrust
      //
      this.txtOwnerTrust.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtOwnerTrust.Location = new System.Drawing.Point(117, 140);
      this.txtOwnerTrust.Name = "txtOwnerTrust";
      this.txtOwnerTrust.ReadOnly = true;
      this.txtOwnerTrust.Size = new System.Drawing.Size(396, 21);
      this.txtOwnerTrust.TabIndex = 9;
      //
      // txtKeyValidity
      //
      this.txtKeyValidity.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtKeyValidity.Location = new System.Drawing.Point(117, 114);
      this.txtKeyValidity.Name = "txtKeyValidity";
      this.txtKeyValidity.ReadOnly = true;
      this.txtKeyValidity.Size = new System.Drawing.Size(396, 21);
      this.txtKeyValidity.TabIndex = 7;
      //
      // txtKeyType
      //
      this.txtKeyType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtKeyType.Location = new System.Drawing.Point(117, 60);
      this.txtKeyType.Name = "txtKeyType";
      this.txtKeyType.ReadOnly = true;
      this.txtKeyType.Size = new System.Drawing.Size(396, 21);
      this.txtKeyType.TabIndex = 5;
      //
      // keyList
      //
      this.keyList.AllowColumnReorder = true;
      this.keyList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.keyList.Font = new System.Drawing.Font("Arial", 8F);
      this.keyList.FullRowSelect = true;
      this.keyList.Location = new System.Drawing.Point(12, 193);
      this.keyList.MultiSelect = false;
      this.keyList.Name = "keyList";
      this.keyList.Size = new System.Drawing.Size(501, 130);
      this.keyList.TabIndex = 12;
      this.keyList.UseCompatibleStateImageBehavior = false;
      this.keyList.View = System.Windows.Forms.View.Details;
      //
      // txtCapabilities
      //
      this.txtCapabilities.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtCapabilities.Location = new System.Drawing.Point(117, 87);
      this.txtCapabilities.Name = "txtCapabilities";
      this.txtCapabilities.ReadOnly = true;
      this.txtCapabilities.Size = new System.Drawing.Size(396, 21);
      this.txtCapabilities.TabIndex = 14;
      //
      // lblCapabilities
      //
      lblCapabilities.Location = new System.Drawing.Point(9, 87);
      lblCapabilities.Name = "lblCapabilities";
      lblCapabilities.Size = new System.Drawing.Size(104, 20);
      lblCapabilities.TabIndex = 13;
      lblCapabilities.Text = "Capabilities";
      lblCapabilities.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // KeyPropertiesForm
      //
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(523, 335);
      this.Controls.Add(this.txtCapabilities);
      this.Controls.Add(lblCapabilities);
      this.Controls.Add(this.keyList);
      this.Controls.Add(this.txtKeyType);
      this.Controls.Add(this.txtKeyValidity);
      this.Controls.Add(this.txtOwnerTrust);
      this.Controls.Add(this.txtFingerprint);
      this.Controls.Add(this.txtKeyId);
      this.Controls.Add(this.txtPrimaryId);
      this.Controls.Add(lblKeyId);
      this.Controls.Add(lblType);
      this.Controls.Add(lblValidity);
      this.Controls.Add(lblTrust);
      this.Controls.Add(lblFingerprint);
      this.Controls.Add(lblPrimaryUser);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.KeyPreview = true;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "KeyPropertiesForm";
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Key Properties";
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TextBox txtPrimaryId;
    private System.Windows.Forms.TextBox txtKeyId;
    private System.Windows.Forms.TextBox txtFingerprint;
    private System.Windows.Forms.TextBox txtOwnerTrust;
    private System.Windows.Forms.TextBox txtKeyValidity;
    private System.Windows.Forms.TextBox txtKeyType;
    private SimpleKeyList keyList;
    private System.Windows.Forms.TextBox txtCapabilities;
  }
}