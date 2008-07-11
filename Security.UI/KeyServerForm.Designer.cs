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
  partial class KeyServerForm
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
      System.Windows.Forms.Button btnOK;
      System.Windows.Forms.Button btnCancel;
      System.Windows.Forms.Label lblKeyServer;
      this.keyservers = new System.Windows.Forms.ComboBox();
      this.lblHelp = new System.Windows.Forms.Label();
      btnOK = new System.Windows.Forms.Button();
      btnCancel = new System.Windows.Forms.Button();
      lblKeyServer = new System.Windows.Forms.Label();
      this.SuspendLayout();
      // 
      // btnOK
      // 
      btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnOK.Location = new System.Drawing.Point(152, 39);
      btnOK.Name = "btnOK";
      btnOK.Size = new System.Drawing.Size(87, 23);
      btnOK.TabIndex = 4;
      btnOK.Text = "OK";
      btnOK.UseVisualStyleBackColor = true;
      btnOK.Click += new System.EventHandler(this.btnOK_Click);
      // 
      // btnCancel
      // 
      btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      btnCancel.Location = new System.Drawing.Point(246, 39);
      btnCancel.Name = "btnCancel";
      btnCancel.Size = new System.Drawing.Size(87, 23);
      btnCancel.TabIndex = 5;
      btnCancel.Text = "Cancel";
      btnCancel.UseVisualStyleBackColor = true;
      // 
      // lblKeyServer
      // 
      lblKeyServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      lblKeyServer.Location = new System.Drawing.Point(2, 12);
      lblKeyServer.Name = "lblKeyServer";
      lblKeyServer.Size = new System.Drawing.Size(96, 21);
      lblKeyServer.TabIndex = 2;
      lblKeyServer.Text = "Key server:";
      lblKeyServer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      // 
      // keyservers
      // 
      this.keyservers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.keyservers.FormattingEnabled = true;
      this.keyservers.Location = new System.Drawing.Point(98, 12);
      this.keyservers.Name = "keyservers";
      this.keyservers.Size = new System.Drawing.Size(235, 21);
      this.keyservers.TabIndex = 3;
      // 
      // lblHelp
      // 
      this.lblHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.lblHelp.Location = new System.Drawing.Point(9, 6);
      this.lblHelp.Name = "lblHelp";
      this.lblHelp.Size = new System.Drawing.Size(324, 0);
      this.lblHelp.TabIndex = 1;
      // 
      // KeyServerForm
      // 
      this.AcceptButton = btnOK;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = btnCancel;
      this.ClientSize = new System.Drawing.Size(343, 70);
      this.Controls.Add(this.lblHelp);
      this.Controls.Add(this.keyservers);
      this.Controls.Add(lblKeyServer);
      this.Controls.Add(btnCancel);
      this.Controls.Add(btnOK);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "KeyServerForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Select Key Server";
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.ComboBox keyservers;
    private System.Windows.Forms.Label lblHelp;

  }
}