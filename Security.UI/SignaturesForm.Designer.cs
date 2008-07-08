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
  partial class SignaturesForm
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
      this.components = new System.ComponentModel.Container();
      this.lblDescription = new System.Windows.Forms.Label();
      this.sigList = new AdamMil.Security.UI.SignatureList();
      this.SuspendLayout();
      // 
      // lblDescription
      // 
      this.lblDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.lblDescription.Location = new System.Drawing.Point(5, 3);
      this.lblDescription.Name = "lblDescription";
      this.lblDescription.Size = new System.Drawing.Size(741, 19);
      this.lblDescription.TabIndex = 1;
      this.lblDescription.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
      // 
      // sigList
      // 
      this.sigList.AllowColumnReorder = true;
      this.sigList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.sigList.Font = new System.Drawing.Font("Arial", 8F);
      this.sigList.FullRowSelect = true;
      this.sigList.Location = new System.Drawing.Point(8, 26);
      this.sigList.Name = "sigList";
      this.sigList.Size = new System.Drawing.Size(736, 239);
      this.sigList.TabIndex = 0;
      this.sigList.UseCompatibleStateImageBehavior = false;
      this.sigList.View = System.Windows.Forms.View.Details;
      // 
      // SignaturesForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(752, 273);
      this.Controls.Add(this.lblDescription);
      this.Controls.Add(this.sigList);
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.MinimumSize = new System.Drawing.Size(320, 180);
      this.Name = "SignaturesForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Signature List";
      this.ResumeLayout(false);

    }

    #endregion

    private SignatureList sigList;
    private System.Windows.Forms.Label lblDescription;
  }
}