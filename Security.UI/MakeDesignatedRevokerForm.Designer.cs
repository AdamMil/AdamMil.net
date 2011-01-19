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
  partial class MakeDesignatedRevokerForm
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
      System.Windows.Forms.Label lblRevokedKey;
      System.Windows.Forms.Button btnCancel;
      this.lblRevokingKey = new System.Windows.Forms.Label();
      this.ownedKeys = new System.Windows.Forms.ComboBox();
      this.btnOK = new System.Windows.Forms.Button();
      lblHelp = new System.Windows.Forms.Label();
      lblRevokedKey = new System.Windows.Forms.Label();
      btnCancel = new System.Windows.Forms.Button();
      this.SuspendLayout();
      //
      // lblHelp
      //
      lblHelp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      lblHelp.Location = new System.Drawing.Point(5, 7);
      lblHelp.Name = "lblHelp";
      lblHelp.Size = new System.Drawing.Size(448, 30);
      lblHelp.TabIndex = 0;
      lblHelp.Text = "A designated revoker is a key that has permission to revoke another key. Adding a" +
    " designated revoker cannot be undone!";
      //
      // lblRevokedKey
      //
      lblRevokedKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      lblRevokedKey.Location = new System.Drawing.Point(5, 88);
      lblRevokedKey.Name = "lblRevokedKey";
      lblRevokedKey.Size = new System.Drawing.Size(447, 13);
      lblRevokedKey.TabIndex = 2;
      lblRevokedKey.Text = "Which of your keys is the above key allowed to revoke?";
      //
      // btnCancel
      //
      btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      btnCancel.Location = new System.Drawing.Point(381, 131);
      btnCancel.Name = "btnCancel";
      btnCancel.Size = new System.Drawing.Size(75, 23);
      btnCancel.TabIndex = 4;
      btnCancel.Text = "&Cancel";
      btnCancel.UseVisualStyleBackColor = true;
      //
      // lblRevokingKey
      //
      this.lblRevokingKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.lblRevokingKey.Location = new System.Drawing.Point(5, 41);
      this.lblRevokingKey.Name = "lblRevokingKey";
      this.lblRevokingKey.Size = new System.Drawing.Size(451, 47);
      this.lblRevokingKey.TabIndex = 1;
      this.lblRevokingKey.Text = "You are making this key a designated revoker:";
      //
      // ownedKeys
      //
      this.ownedKeys.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.ownedKeys.FormattingEnabled = true;
      this.ownedKeys.Location = new System.Drawing.Point(8, 104);
      this.ownedKeys.Name = "ownedKeys";
      this.ownedKeys.Size = new System.Drawing.Size(448, 21);
      this.ownedKeys.TabIndex = 3;
      //
      // btnOK
      //
      this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.btnOK.Location = new System.Drawing.Point(300, 131);
      this.btnOK.Name = "btnOK";
      this.btnOK.Size = new System.Drawing.Size(75, 23);
      this.btnOK.TabIndex = 5;
      this.btnOK.Text = "&OK";
      this.btnOK.UseVisualStyleBackColor = true;
      //
      // MakeDesignatedRevokerForm
      //
      this.AcceptButton = this.btnOK;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = btnCancel;
      this.ClientSize = new System.Drawing.Size(464, 159);
      this.Controls.Add(this.btnOK);
      this.Controls.Add(btnCancel);
      this.Controls.Add(this.ownedKeys);
      this.Controls.Add(lblRevokedKey);
      this.Controls.Add(this.lblRevokingKey);
      this.Controls.Add(lblHelp);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "MakeDesignatedRevokerForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Make Designated Revoker";
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Label lblRevokingKey;
    private System.Windows.Forms.ComboBox ownedKeys;
    private System.Windows.Forms.Button btnOK;
  }
}