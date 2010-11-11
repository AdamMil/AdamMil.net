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
  partial class RecipientSearchForm
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
      System.Windows.Forms.Label lblSearch;
      System.Windows.Forms.Button btnCancel;
      this.recipients = new AdamMil.Security.UI.RecipientList();
      this.txtSearch = new System.Windows.Forms.TextBox();
      this.btnClear = new System.Windows.Forms.Button();
      this.btnOK = new System.Windows.Forms.Button();
      lblSearch = new System.Windows.Forms.Label();
      btnCancel = new System.Windows.Forms.Button();
      this.SuspendLayout();
      //
      // lblSearch
      //
      lblSearch.Location = new System.Drawing.Point(5, 7);
      lblSearch.Name = "lblSearch";
      lblSearch.Size = new System.Drawing.Size(72, 21);
      lblSearch.TabIndex = 0;
      lblSearch.Text = "&Search for:";
      lblSearch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
      //
      // btnCancel
      //
      btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      btnCancel.Location = new System.Drawing.Point(408, 318);
      btnCancel.Name = "btnCancel";
      btnCancel.Size = new System.Drawing.Size(75, 23);
      btnCancel.TabIndex = 5;
      btnCancel.Text = "Cancel";
      btnCancel.UseVisualStyleBackColor = true;
      //
      // recipients
      //
      this.recipients.AllowColumnReorder = true;
      this.recipients.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.recipients.Font = new System.Drawing.Font("Arial", 8F);
      this.recipients.FullRowSelect = true;
      this.recipients.HideSelection = false;
      this.recipients.Location = new System.Drawing.Point(8, 34);
      this.recipients.Name = "recipients";
      this.recipients.Size = new System.Drawing.Size(475, 276);
      this.recipients.TabIndex = 3;
      this.recipients.UseCompatibleStateImageBehavior = false;
      this.recipients.View = System.Windows.Forms.View.Details;
      this.recipients.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.recipients_MouseDoubleClick);
      this.recipients.SelectedIndexChanged += new System.EventHandler(this.recipients_SelectedIndexChanged);
      //
      // txtSearch
      //
      this.txtSearch.Location = new System.Drawing.Point(83, 7);
      this.txtSearch.Name = "txtSearch";
      this.txtSearch.Size = new System.Drawing.Size(218, 21);
      this.txtSearch.TabIndex = 1;
      this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
      //
      // btnClear
      //
      this.btnClear.Location = new System.Drawing.Point(307, 7);
      this.btnClear.Name = "btnClear";
      this.btnClear.Size = new System.Drawing.Size(56, 21);
      this.btnClear.TabIndex = 2;
      this.btnClear.Text = "&Clear";
      this.btnClear.UseVisualStyleBackColor = true;
      this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
      //
      // btnOK
      //
      this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.btnOK.Location = new System.Drawing.Point(327, 318);
      this.btnOK.Name = "btnOK";
      this.btnOK.Size = new System.Drawing.Size(75, 23);
      this.btnOK.TabIndex = 4;
      this.btnOK.Text = "&OK";
      this.btnOK.UseVisualStyleBackColor = true;
      //
      // RecipientSearchForm
      //
      this.AcceptButton = this.btnOK;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = btnCancel;
      this.ClientSize = new System.Drawing.Size(491, 346);
      this.Controls.Add(this.btnOK);
      this.Controls.Add(btnCancel);
      this.Controls.Add(this.btnClear);
      this.Controls.Add(this.txtSearch);
      this.Controls.Add(lblSearch);
      this.Controls.Add(this.recipients);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.MinimumSize = new System.Drawing.Size(379, 274);
      this.Name = "RecipientSearchForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Select Recipients";
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private RecipientList recipients;
    private System.Windows.Forms.TextBox txtSearch;
    private System.Windows.Forms.Button btnClear;
    private System.Windows.Forms.Button btnOK;
  }
}