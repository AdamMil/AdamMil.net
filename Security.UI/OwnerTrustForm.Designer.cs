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
  partial class OwnerTrustForm
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
      System.Windows.Forms.Label lblDescription;
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OwnerTrustForm));
      System.Windows.Forms.GroupBox groupBox;
      System.Windows.Forms.Button btnCancel;
      System.Windows.Forms.Button btnOK;
      System.Windows.Forms.Label lblKeys;
      this.rbDontTrust = new System.Windows.Forms.RadioButton();
      this.rbCasual = new System.Windows.Forms.RadioButton();
      this.rbUltimate = new System.Windows.Forms.RadioButton();
      this.rbFull = new System.Windows.Forms.RadioButton();
      this.rbDontKnow = new System.Windows.Forms.RadioButton();
      this.trustedKeys = new System.Windows.Forms.ListBox();
      lblDescription = new System.Windows.Forms.Label();
      groupBox = new System.Windows.Forms.GroupBox();
      btnCancel = new System.Windows.Forms.Button();
      btnOK = new System.Windows.Forms.Button();
      lblKeys = new System.Windows.Forms.Label();
      groupBox.SuspendLayout();
      this.SuspendLayout();
      //
      // lblDescription
      //
      lblDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      lblDescription.Location = new System.Drawing.Point(12, 9);
      lblDescription.Name = "lblDescription";
      lblDescription.Size = new System.Drawing.Size(423, 70);
      lblDescription.TabIndex = 0;
      lblDescription.Text = resources.GetString("lblDescription.Text");
      //
      // groupBox
      //
      groupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      groupBox.Controls.Add(this.rbDontTrust);
      groupBox.Controls.Add(this.rbCasual);
      groupBox.Controls.Add(this.rbUltimate);
      groupBox.Controls.Add(this.rbFull);
      groupBox.Controls.Add(this.rbDontKnow);
      groupBox.Location = new System.Drawing.Point(15, 172);
      groupBox.Name = "groupBox";
      groupBox.Size = new System.Drawing.Size(420, 143);
      groupBox.TabIndex = 3;
      groupBox.TabStop = false;
      groupBox.Text = "How rigorously do the owners validate others\' keys?";
      //
      // rbDontTrust
      //
      this.rbDontTrust.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbDontTrust.Location = new System.Drawing.Point(7, 44);
      this.rbDontTrust.Name = "rbDontTrust";
      this.rbDontTrust.Size = new System.Drawing.Size(407, 17);
      this.rbDontTrust.TabIndex = 5;
      this.rbDontTrust.Text = "I do NOT trust them to properly and reliably validate others\' keys.";
      this.rbDontTrust.UseVisualStyleBackColor = true;
      //
      // rbCasual
      //
      this.rbCasual.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbCasual.Location = new System.Drawing.Point(7, 68);
      this.rbCasual.Name = "rbCasual";
      this.rbCasual.Size = new System.Drawing.Size(407, 17);
      this.rbCasual.TabIndex = 6;
      this.rbCasual.Text = "I trust them to do casual verification of every key they sign.";
      this.rbCasual.UseVisualStyleBackColor = true;
      //
      // rbUltimate
      //
      this.rbUltimate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbUltimate.Location = new System.Drawing.Point(7, 114);
      this.rbUltimate.Name = "rbUltimate";
      this.rbUltimate.Size = new System.Drawing.Size(407, 17);
      this.rbUltimate.TabIndex = 8;
      this.rbUltimate.Text = "I own these keys, or I trust their owners blindly and completely.";
      this.rbUltimate.UseVisualStyleBackColor = true;
      //
      // rbFull
      //
      this.rbFull.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbFull.Location = new System.Drawing.Point(7, 91);
      this.rbFull.Name = "rbFull";
      this.rbFull.Size = new System.Drawing.Size(407, 17);
      this.rbFull.TabIndex = 7;
      this.rbFull.Text = "I trust them to do rigorous, full verification of every key they sign.";
      this.rbFull.UseVisualStyleBackColor = true;
      //
      // rbDontKnow
      //
      this.rbDontKnow.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbDontKnow.Checked = true;
      this.rbDontKnow.Location = new System.Drawing.Point(7, 21);
      this.rbDontKnow.Name = "rbDontKnow";
      this.rbDontKnow.Size = new System.Drawing.Size(407, 17);
      this.rbDontKnow.TabIndex = 4;
      this.rbDontKnow.TabStop = true;
      this.rbDontKnow.Text = "I don\'t know or won\'t say.";
      this.rbDontKnow.UseVisualStyleBackColor = true;
      //
      // btnCancel
      //
      btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      btnCancel.Location = new System.Drawing.Point(360, 323);
      btnCancel.Name = "btnCancel";
      btnCancel.Size = new System.Drawing.Size(75, 23);
      btnCancel.TabIndex = 10;
      btnCancel.Text = "&Cancel";
      btnCancel.UseVisualStyleBackColor = true;
      //
      // btnOK
      //
      btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
      btnOK.Location = new System.Drawing.Point(279, 323);
      btnOK.Name = "btnOK";
      btnOK.Size = new System.Drawing.Size(75, 23);
      btnOK.TabIndex = 9;
      btnOK.Text = "&OK";
      btnOK.UseVisualStyleBackColor = true;
      //
      // lblKeys
      //
      lblKeys.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      lblKeys.Location = new System.Drawing.Point(12, 79);
      lblKeys.Name = "lblKeys";
      lblKeys.Size = new System.Drawing.Size(420, 15);
      lblKeys.TabIndex = 1;
      lblKeys.Text = "You are setting the owner trust of these keys:";
      lblKeys.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
      //
      // trustedKeys
      //
      this.trustedKeys.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.trustedKeys.FormattingEnabled = true;
      this.trustedKeys.Location = new System.Drawing.Point(15, 97);
      this.trustedKeys.Name = "trustedKeys";
      this.trustedKeys.SelectionMode = System.Windows.Forms.SelectionMode.None;
      this.trustedKeys.Size = new System.Drawing.Size(420, 69);
      this.trustedKeys.TabIndex = 2;
      //
      // OwnerTrustForm
      //
      this.AcceptButton = btnOK;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = btnCancel;
      this.ClientSize = new System.Drawing.Size(447, 354);
      this.Controls.Add(lblKeys);
      this.Controls.Add(this.trustedKeys);
      this.Controls.Add(btnOK);
      this.Controls.Add(btnCancel);
      this.Controls.Add(groupBox);
      this.Controls.Add(lblDescription);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "OwnerTrustForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Set Owner Trust";
      groupBox.ResumeLayout(false);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.RadioButton rbDontKnow;
    private System.Windows.Forms.RadioButton rbDontTrust;
    private System.Windows.Forms.RadioButton rbCasual;
    private System.Windows.Forms.RadioButton rbUltimate;
    private System.Windows.Forms.RadioButton rbFull;
    private System.Windows.Forms.ListBox trustedKeys;
  }
}