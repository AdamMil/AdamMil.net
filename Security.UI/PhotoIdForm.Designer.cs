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
  partial class PhotoIdForm
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
      this.lblId = new System.Windows.Forms.Label();
      this.picture = new System.Windows.Forms.PictureBox();
      ((System.ComponentModel.ISupportInitialize)(this.picture)).BeginInit();
      this.SuspendLayout();
      // 
      // lblId
      // 
      this.lblId.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.lblId.Location = new System.Drawing.Point(5, 6);
      this.lblId.Name = "lblId";
      this.lblId.Size = new System.Drawing.Size(342, 34);
      this.lblId.TabIndex = 0;
      // 
      // picture
      // 
      this.picture.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      this.picture.Location = new System.Drawing.Point(12, 45);
      this.picture.Name = "picture";
      this.picture.Size = new System.Drawing.Size(328, 216);
      this.picture.TabIndex = 1;
      this.picture.TabStop = false;
      // 
      // PhotoIdForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(352, 273);
      this.Controls.Add(this.picture);
      this.Controls.Add(this.lblId);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.MinimumSize = new System.Drawing.Size(300, 160);
      this.Name = "PhotoIdForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "OpenPGP Photo Id";
      ((System.ComponentModel.ISupportInitialize)(this.picture)).EndInit();
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Label lblId;
    private System.Windows.Forms.PictureBox picture;
  }
}