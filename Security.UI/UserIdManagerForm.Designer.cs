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
  partial class UserIdManagerForm
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
      System.Windows.Forms.ColumnHeader nameColumn;
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserIdManagerForm));
      this.btnAddUserId = new System.Windows.Forms.Button();
      this.btnAddPhotoId = new System.Windows.Forms.Button();
      this.lblDescription = new System.Windows.Forms.Label();
      this.userIds = new System.Windows.Forms.ListView();
      this.imageList = new System.Windows.Forms.ImageList(this.components);
      this.btnSetPrimary = new System.Windows.Forms.Button();
      this.btnDelete = new System.Windows.Forms.Button();
      this.btnRevoke = new System.Windows.Forms.Button();
      nameColumn = new System.Windows.Forms.ColumnHeader();
      this.SuspendLayout();
      // 
      // btnAddUserId
      // 
      this.btnAddUserId.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.btnAddUserId.Location = new System.Drawing.Point(8, 233);
      this.btnAddUserId.Name = "btnAddUserId";
      this.btnAddUserId.Size = new System.Drawing.Size(93, 23);
      this.btnAddUserId.TabIndex = 2;
      this.btnAddUserId.Text = "&Add User ID";
      this.btnAddUserId.UseVisualStyleBackColor = true;
      this.btnAddUserId.Click += new System.EventHandler(this.btnAddUserId_Click);
      // 
      // btnAddPhotoId
      // 
      this.btnAddPhotoId.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.btnAddPhotoId.Location = new System.Drawing.Point(107, 233);
      this.btnAddPhotoId.Name = "btnAddPhotoId";
      this.btnAddPhotoId.Size = new System.Drawing.Size(93, 23);
      this.btnAddPhotoId.TabIndex = 3;
      this.btnAddPhotoId.Text = "Add &Photo ID";
      this.btnAddPhotoId.UseVisualStyleBackColor = true;
      this.btnAddPhotoId.Click += new System.EventHandler(this.btnAddPhotoId_Click);
      // 
      // nameColumn
      // 
      nameColumn.Width = 460;
      // 
      // lblDescription
      // 
      this.lblDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.lblDescription.Location = new System.Drawing.Point(5, 8);
      this.lblDescription.Name = "lblDescription";
      this.lblDescription.Size = new System.Drawing.Size(484, 120);
      this.lblDescription.TabIndex = 0;
      this.lblDescription.Text = resources.GetString("lblDescription.Text");
      // 
      // userIds
      // 
      this.userIds.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.userIds.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            nameColumn});
      this.userIds.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
      this.userIds.LabelWrap = false;
      this.userIds.Location = new System.Drawing.Point(8, 130);
      this.userIds.Name = "userIds";
      this.userIds.Size = new System.Drawing.Size(481, 95);
      this.userIds.SmallImageList = this.imageList;
      this.userIds.TabIndex = 1;
      this.userIds.UseCompatibleStateImageBehavior = false;
      this.userIds.View = System.Windows.Forms.View.Details;
      this.userIds.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.userIds_MouseDoubleClick);
      this.userIds.SelectedIndexChanged += new System.EventHandler(this.userIds_SelectedIndexChanged);
      this.userIds.KeyDown += new System.Windows.Forms.KeyEventHandler(this.userIds_KeyDown);
      // 
      // imageList
      // 
      this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
      this.imageList.TransparentColor = System.Drawing.Color.Magenta;
      this.imageList.Images.SetKeyName(0, "userid.gif");
      this.imageList.Images.SetKeyName(1, "photoId.gif");
      this.imageList.Images.SetKeyName(2, "unknown.gif");
      // 
      // btnSetPrimary
      // 
      this.btnSetPrimary.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.btnSetPrimary.Enabled = false;
      this.btnSetPrimary.Location = new System.Drawing.Point(206, 233);
      this.btnSetPrimary.Name = "btnSetPrimary";
      this.btnSetPrimary.Size = new System.Drawing.Size(121, 23);
      this.btnSetPrimary.TabIndex = 4;
      this.btnSetPrimary.Text = "&Set as Primary ID";
      this.btnSetPrimary.UseVisualStyleBackColor = true;
      this.btnSetPrimary.Click += new System.EventHandler(this.btnSetPrimary_Click);
      // 
      // btnDelete
      // 
      this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.btnDelete.Enabled = false;
      this.btnDelete.Location = new System.Drawing.Point(333, 233);
      this.btnDelete.Name = "btnDelete";
      this.btnDelete.Size = new System.Drawing.Size(75, 23);
      this.btnDelete.TabIndex = 5;
      this.btnDelete.Text = "&Delete";
      this.btnDelete.UseVisualStyleBackColor = true;
      this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
      // 
      // btnRevoke
      // 
      this.btnRevoke.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.btnRevoke.Enabled = false;
      this.btnRevoke.Location = new System.Drawing.Point(414, 233);
      this.btnRevoke.Name = "btnRevoke";
      this.btnRevoke.Size = new System.Drawing.Size(75, 23);
      this.btnRevoke.TabIndex = 6;
      this.btnRevoke.Text = "&Revoke";
      this.btnRevoke.UseVisualStyleBackColor = true;
      this.btnRevoke.Click += new System.EventHandler(this.btnRevoke_Click);
      // 
      // UserIdManagerForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(497, 262);
      this.Controls.Add(this.userIds);
      this.Controls.Add(this.btnRevoke);
      this.Controls.Add(this.btnDelete);
      this.Controls.Add(this.btnSetPrimary);
      this.Controls.Add(this.btnAddPhotoId);
      this.Controls.Add(this.btnAddUserId);
      this.Controls.Add(this.lblDescription);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.KeyPreview = true;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.MinimumSize = new System.Drawing.Size(505, 289);
      this.Name = "UserIdManagerForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Manage User IDs";
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.ListView userIds;
    private System.Windows.Forms.Button btnSetPrimary;
    private System.Windows.Forms.Button btnDelete;
    private System.Windows.Forms.Button btnRevoke;
    private System.Windows.Forms.Label lblDescription;
    private System.Windows.Forms.ImageList imageList;
    private System.Windows.Forms.Button btnAddUserId;
    private System.Windows.Forms.Button btnAddPhotoId;
  }
}