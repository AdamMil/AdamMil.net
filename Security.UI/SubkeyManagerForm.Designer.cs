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
  partial class SubkeyManagerForm
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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SubkeyManagerForm));
      this.btnAdd = new System.Windows.Forms.Button();
      this.btnRevoke = new System.Windows.Forms.Button();
      this.btnDelete = new System.Windows.Forms.Button();
      this.lblDescription = new System.Windows.Forms.Label();
      this.subkeys = new AdamMil.Security.UI.SimpleKeyList();
      this.SuspendLayout();
      // 
      // btnAdd
      // 
      this.btnAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.btnAdd.Location = new System.Drawing.Point(9, 233);
      this.btnAdd.Name = "btnAdd";
      this.btnAdd.Size = new System.Drawing.Size(93, 23);
      this.btnAdd.TabIndex = 2;
      this.btnAdd.Text = "&Add Subkey";
      this.btnAdd.UseVisualStyleBackColor = true;
      this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
      // 
      // btnRevoke
      // 
      this.btnRevoke.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.btnRevoke.Enabled = false;
      this.btnRevoke.Location = new System.Drawing.Point(189, 233);
      this.btnRevoke.Name = "btnRevoke";
      this.btnRevoke.Size = new System.Drawing.Size(75, 23);
      this.btnRevoke.TabIndex = 4;
      this.btnRevoke.Text = "&Revoke";
      this.btnRevoke.UseVisualStyleBackColor = true;
      this.btnRevoke.Click += new System.EventHandler(this.btnRevoke_Click);
      // 
      // btnDelete
      // 
      this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.btnDelete.Enabled = false;
      this.btnDelete.Location = new System.Drawing.Point(108, 233);
      this.btnDelete.Name = "btnDelete";
      this.btnDelete.Size = new System.Drawing.Size(75, 23);
      this.btnDelete.TabIndex = 3;
      this.btnDelete.Text = "&Delete";
      this.btnDelete.UseVisualStyleBackColor = true;
      this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
      // 
      // lblDescription
      // 
      this.lblDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.lblDescription.Location = new System.Drawing.Point(6, 6);
      this.lblDescription.Name = "lblDescription";
      this.lblDescription.Size = new System.Drawing.Size(504, 120);
      this.lblDescription.TabIndex = 0;
      this.lblDescription.Text = resources.GetString("lblDescription.Text");
      // 
      // subkeys
      // 
      this.subkeys.AllowColumnReorder = true;
      this.subkeys.Font = new System.Drawing.Font("Arial", 8F);
      this.subkeys.FullRowSelect = true;
      this.subkeys.HideSelection = false;
      this.subkeys.Location = new System.Drawing.Point(9, 130);
      this.subkeys.Name = "subkeys";
      this.subkeys.Size = new System.Drawing.Size(500, 97);
      this.subkeys.TabIndex = 5;
      this.subkeys.UseCompatibleStateImageBehavior = false;
      this.subkeys.View = System.Windows.Forms.View.Details;
      this.subkeys.SelectedIndexChanged += new System.EventHandler(this.subkeys_SelectedIndexChanged);
      // 
      // SubkeyManagerForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(517, 260);
      this.Controls.Add(this.subkeys);
      this.Controls.Add(this.btnRevoke);
      this.Controls.Add(this.btnDelete);
      this.Controls.Add(this.btnAdd);
      this.Controls.Add(this.lblDescription);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.KeyPreview = true;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.MinimumSize = new System.Drawing.Size(525, 287);
      this.Name = "SubkeyManagerForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Subkey Manager";
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Button btnRevoke;
    private System.Windows.Forms.Button btnDelete;
    private System.Windows.Forms.Label lblDescription;
    private SimpleKeyList subkeys;
    private System.Windows.Forms.Button btnAdd;
  }
}