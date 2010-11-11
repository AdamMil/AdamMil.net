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
  partial class KeySigningForm
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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(KeySigningForm));
      System.Windows.Forms.Label lblKeysSigned;
      System.Windows.Forms.Label lblSigningKey;
      System.Windows.Forms.GroupBox groupBox;
      System.Windows.Forms.Button btnOK;
      System.Windows.Forms.Button btnCancel;
      this.rbNone = new System.Windows.Forms.RadioButton();
      this.rbRigorous = new System.Windows.Forms.RadioButton();
      this.rbCasual = new System.Windows.Forms.RadioButton();
      this.rbNoAnswer = new System.Windows.Forms.RadioButton();
      this.signedKeys = new System.Windows.Forms.ListBox();
      this.signingKeys = new System.Windows.Forms.ComboBox();
      this.chkLocal = new System.Windows.Forms.CheckBox();
      lblHelp = new System.Windows.Forms.Label();
      lblKeysSigned = new System.Windows.Forms.Label();
      lblSigningKey = new System.Windows.Forms.Label();
      groupBox = new System.Windows.Forms.GroupBox();
      btnOK = new System.Windows.Forms.Button();
      btnCancel = new System.Windows.Forms.Button();
      groupBox.SuspendLayout();
      this.SuspendLayout();
      //
      // lblHelp
      //
      lblHelp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      lblHelp.Location = new System.Drawing.Point(5, 7);
      lblHelp.Name = "lblHelp";
      lblHelp.Size = new System.Drawing.Size(560, 60);
      lblHelp.TabIndex = 0;
      lblHelp.Text = resources.GetString("lblHelp.Text");
      //
      // lblKeysSigned
      //
      lblKeysSigned.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      lblKeysSigned.Location = new System.Drawing.Point(5, 67);
      lblKeysSigned.Name = "lblKeysSigned";
      lblKeysSigned.Size = new System.Drawing.Size(557, 17);
      lblKeysSigned.TabIndex = 1;
      lblKeysSigned.Text = "Keys being signed:";
      lblKeysSigned.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
      //
      // lblSigningKey
      //
      lblSigningKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      lblSigningKey.Location = new System.Drawing.Point(5, 160);
      lblSigningKey.Name = "lblSigningKey";
      lblSigningKey.Size = new System.Drawing.Size(557, 19);
      lblSigningKey.TabIndex = 3;
      lblSigningKey.Text = "Which of your keys will you use to sign the keys above?";
      lblSigningKey.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
      //
      // groupBox
      //
      groupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      groupBox.Controls.Add(this.rbNone);
      groupBox.Controls.Add(this.rbRigorous);
      groupBox.Controls.Add(this.rbCasual);
      groupBox.Controls.Add(this.rbNoAnswer);
      groupBox.Location = new System.Drawing.Point(9, 210);
      groupBox.Name = "groupBox";
      groupBox.Size = new System.Drawing.Size(557, 115);
      groupBox.TabIndex = 5;
      groupBox.TabStop = false;
      groupBox.Text = "How carefully have you verified that the keys actually belong to the people named" +
    " above?";
      //
      // rbNone
      //
      this.rbNone.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbNone.Location = new System.Drawing.Point(10, 43);
      this.rbNone.Name = "rbNone";
      this.rbNone.Size = new System.Drawing.Size(534, 17);
      this.rbNone.TabIndex = 7;
      this.rbNone.Text = "I have not checked at all.";
      this.rbNone.UseVisualStyleBackColor = true;
      this.rbNone.CheckedChanged += new System.EventHandler(this.rbPoor_CheckedChanged);
      //
      // rbRigorous
      //
      this.rbRigorous.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbRigorous.Location = new System.Drawing.Point(10, 89);
      this.rbRigorous.Name = "rbRigorous";
      this.rbRigorous.Size = new System.Drawing.Size(534, 17);
      this.rbRigorous.TabIndex = 9;
      this.rbRigorous.Text = "I have rigorously verified the owners\' identities.";
      this.rbRigorous.UseVisualStyleBackColor = true;
      //
      // rbCasual
      //
      this.rbCasual.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbCasual.Location = new System.Drawing.Point(10, 66);
      this.rbCasual.Name = "rbCasual";
      this.rbCasual.Size = new System.Drawing.Size(534, 17);
      this.rbCasual.TabIndex = 8;
      this.rbCasual.Text = "I have done casual checking.";
      this.rbCasual.UseVisualStyleBackColor = true;
      //
      // rbNoAnswer
      //
      this.rbNoAnswer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.rbNoAnswer.Checked = true;
      this.rbNoAnswer.Location = new System.Drawing.Point(10, 20);
      this.rbNoAnswer.Name = "rbNoAnswer";
      this.rbNoAnswer.Size = new System.Drawing.Size(534, 17);
      this.rbNoAnswer.TabIndex = 6;
      this.rbNoAnswer.TabStop = true;
      this.rbNoAnswer.Text = "I will not answer.";
      this.rbNoAnswer.UseVisualStyleBackColor = true;
      this.rbNoAnswer.CheckedChanged += new System.EventHandler(this.rbPoor_CheckedChanged);
      //
      // btnOK
      //
      btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
      btnOK.Location = new System.Drawing.Point(409, 331);
      btnOK.Name = "btnOK";
      btnOK.Size = new System.Drawing.Size(75, 23);
      btnOK.TabIndex = 11;
      btnOK.Text = "&OK";
      btnOK.UseVisualStyleBackColor = true;
      //
      // btnCancel
      //
      btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      btnCancel.Location = new System.Drawing.Point(490, 331);
      btnCancel.Name = "btnCancel";
      btnCancel.Size = new System.Drawing.Size(75, 23);
      btnCancel.TabIndex = 12;
      btnCancel.Text = "&Cancel";
      btnCancel.UseVisualStyleBackColor = true;
      //
      // signedKeys
      //
      this.signedKeys.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.signedKeys.FormattingEnabled = true;
      this.signedKeys.Location = new System.Drawing.Point(9, 87);
      this.signedKeys.Name = "signedKeys";
      this.signedKeys.SelectionMode = System.Windows.Forms.SelectionMode.None;
      this.signedKeys.Size = new System.Drawing.Size(557, 69);
      this.signedKeys.TabIndex = 2;
      //
      // signingKeys
      //
      this.signingKeys.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.signingKeys.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.signingKeys.FormattingEnabled = true;
      this.signingKeys.Location = new System.Drawing.Point(9, 182);
      this.signingKeys.Name = "signingKeys";
      this.signingKeys.Size = new System.Drawing.Size(557, 21);
      this.signingKeys.TabIndex = 4;
      //
      // chkLocal
      //
      this.chkLocal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.chkLocal.Checked = true;
      this.chkLocal.CheckState = System.Windows.Forms.CheckState.Checked;
      this.chkLocal.Location = new System.Drawing.Point(9, 332);
      this.chkLocal.Name = "chkLocal";
      this.chkLocal.Size = new System.Drawing.Size(380, 17);
      this.chkLocal.TabIndex = 10;
      this.chkLocal.Text = "Local signature (will not be exported)";
      this.chkLocal.UseVisualStyleBackColor = true;
      //
      // KeySigningForm
      //
      this.AcceptButton = btnOK;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = btnCancel;
      this.ClientSize = new System.Drawing.Size(575, 359);
      this.Controls.Add(btnCancel);
      this.Controls.Add(btnOK);
      this.Controls.Add(this.chkLocal);
      this.Controls.Add(groupBox);
      this.Controls.Add(this.signingKeys);
      this.Controls.Add(lblSigningKey);
      this.Controls.Add(lblKeysSigned);
      this.Controls.Add(this.signedKeys);
      this.Controls.Add(lblHelp);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "KeySigningForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Sign Keys";
      groupBox.ResumeLayout(false);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.ListBox signedKeys;
    private System.Windows.Forms.ComboBox signingKeys;
    private System.Windows.Forms.RadioButton rbNoAnswer;
    private System.Windows.Forms.RadioButton rbNone;
    private System.Windows.Forms.RadioButton rbRigorous;
    private System.Windows.Forms.RadioButton rbCasual;
    private System.Windows.Forms.CheckBox chkLocal;
  }
}