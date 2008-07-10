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
  partial class NewPhotoIdForm
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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewPhotoIdForm));
      System.Windows.Forms.Label lblResize;
      System.Windows.Forms.Button btnDone;
      System.Windows.Forms.Button btnCancel;
      this.overlay = new AdamMil.Security.UI.NewPhotoIdForm.OverlayControl();
      this.btnCrop = new System.Windows.Forms.Button();
      this.btnUndo = new System.Windows.Forms.Button();
      this.cmbSize = new System.Windows.Forms.ComboBox();
      lblHelp = new System.Windows.Forms.Label();
      lblResize = new System.Windows.Forms.Label();
      btnDone = new System.Windows.Forms.Button();
      btnCancel = new System.Windows.Forms.Button();
      this.SuspendLayout();
      // 
      // lblHelp
      // 
      lblHelp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      lblHelp.Location = new System.Drawing.Point(8, 7);
      lblHelp.Name = "lblHelp";
      lblHelp.Size = new System.Drawing.Size(486, 46);
      lblHelp.TabIndex = 0;
      lblHelp.Text = resources.GetString("lblHelp.Text");
      // 
      // lblResize
      // 
      lblResize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      lblResize.Location = new System.Drawing.Point(136, 515);
      lblResize.Name = "lblResize";
      lblResize.Size = new System.Drawing.Size(66, 23);
      lblResize.TabIndex = 3;
      lblResize.Text = "Resize to:";
      lblResize.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      // 
      // btnDone
      // 
      btnDone.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      btnDone.Location = new System.Drawing.Point(358, 515);
      btnDone.Name = "btnDone";
      btnDone.Size = new System.Drawing.Size(60, 23);
      btnDone.TabIndex = 5;
      btnDone.Text = "&Done";
      btnDone.UseVisualStyleBackColor = true;
      btnDone.Click += new System.EventHandler(this.btnDone_Click);
      // 
      // btnCancel
      // 
      btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      btnCancel.Location = new System.Drawing.Point(424, 515);
      btnCancel.Name = "btnCancel";
      btnCancel.Size = new System.Drawing.Size(70, 23);
      btnCancel.TabIndex = 6;
      btnCancel.Text = "Cancel";
      btnCancel.UseVisualStyleBackColor = true;
      // 
      // overlay
      // 
      this.overlay.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.overlay.Bitmap = null;
      this.overlay.Location = new System.Drawing.Point(8, 56);
      this.overlay.Name = "overlay";
      this.overlay.Size = new System.Drawing.Size(486, 452);
      this.overlay.TabIndex = 0;
      this.overlay.TabStop = false;
      this.overlay.MouseMove += new System.Windows.Forms.MouseEventHandler(this.overlay_MouseMove);
      this.overlay.MouseDown += new System.Windows.Forms.MouseEventHandler(this.overlay_MouseDown);
      this.overlay.MouseUp += new System.Windows.Forms.MouseEventHandler(this.overlay_MouseUp);
      // 
      // btnCrop
      // 
      this.btnCrop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.btnCrop.Enabled = false;
      this.btnCrop.Location = new System.Drawing.Point(8, 515);
      this.btnCrop.Name = "btnCrop";
      this.btnCrop.Size = new System.Drawing.Size(60, 23);
      this.btnCrop.TabIndex = 1;
      this.btnCrop.Text = "&Crop";
      this.btnCrop.UseVisualStyleBackColor = true;
      this.btnCrop.Click += new System.EventHandler(this.btnCrop_Click);
      // 
      // btnUndo
      // 
      this.btnUndo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.btnUndo.Enabled = false;
      this.btnUndo.Location = new System.Drawing.Point(74, 515);
      this.btnUndo.Name = "btnUndo";
      this.btnUndo.Size = new System.Drawing.Size(60, 23);
      this.btnUndo.TabIndex = 2;
      this.btnUndo.Text = "&Undo";
      this.btnUndo.UseVisualStyleBackColor = true;
      this.btnUndo.Click += new System.EventHandler(this.btnUndo_Click);
      // 
      // cmbSize
      // 
      this.cmbSize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.cmbSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cmbSize.FormattingEnabled = true;
      this.cmbSize.Items.AddRange(new object[] {
            "Small (96x115)",
            "Medium (144x173)",
            "Large (240x288)",
            "Very large (360x432)",
            "Leave as-is"});
      this.cmbSize.Location = new System.Drawing.Point(204, 516);
      this.cmbSize.Name = "cmbSize";
      this.cmbSize.Size = new System.Drawing.Size(148, 21);
      this.cmbSize.TabIndex = 4;
      // 
      // NewPhotoIdForm
      // 
      this.AcceptButton = btnDone;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = btnCancel;
      this.ClientSize = new System.Drawing.Size(502, 543);
      this.Controls.Add(this.overlay);
      this.Controls.Add(btnCancel);
      this.Controls.Add(btnDone);
      this.Controls.Add(this.cmbSize);
      this.Controls.Add(lblResize);
      this.Controls.Add(this.btnUndo);
      this.Controls.Add(this.btnCrop);
      this.Controls.Add(lblHelp);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.MinimumSize = new System.Drawing.Size(510, 320);
      this.Name = "NewPhotoIdForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Create Photo Id";
      this.ResumeLayout(false);

    }

    #endregion

    private OverlayControl overlay;
    private System.Windows.Forms.Button btnCrop;
    private System.Windows.Forms.Button btnUndo;
    private System.Windows.Forms.ComboBox cmbSize;
  }
}