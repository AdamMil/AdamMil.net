namespace AdamMil.Security.UI
{
  partial class KeyForm
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
      System.Windows.Forms.Button btnCancel;
      System.Windows.Forms.Button btnOK;
      System.Windows.Forms.Label lblKeyServer;
      this.keys = new System.Windows.Forms.ComboBox();
      this.lblDescription = new System.Windows.Forms.Label();
      btnCancel = new System.Windows.Forms.Button();
      btnOK = new System.Windows.Forms.Button();
      lblKeyServer = new System.Windows.Forms.Label();
      this.SuspendLayout();
      // 
      // keys
      // 
      this.keys.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.keys.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.keys.FormattingEnabled = true;
      this.keys.Location = new System.Drawing.Point(91, 34);
      this.keys.Name = "keys";
      this.keys.Size = new System.Drawing.Size(420, 21);
      this.keys.TabIndex = 2;
      // 
      // btnCancel
      // 
      btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      btnCancel.Location = new System.Drawing.Point(436, 63);
      btnCancel.Name = "btnCancel";
      btnCancel.Size = new System.Drawing.Size(75, 23);
      btnCancel.TabIndex = 4;
      btnCancel.Text = "Cancel";
      btnCancel.UseVisualStyleBackColor = true;
      // 
      // btnOK
      // 
      btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
      btnOK.Location = new System.Drawing.Point(355, 63);
      btnOK.Name = "btnOK";
      btnOK.Size = new System.Drawing.Size(75, 23);
      btnOK.TabIndex = 3;
      btnOK.Text = "OK";
      btnOK.UseVisualStyleBackColor = true;
      // 
      // lblKeyServer
      // 
      lblKeyServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      lblKeyServer.Location = new System.Drawing.Point(0, 33);
      lblKeyServer.Name = "lblKeyServer";
      lblKeyServer.Size = new System.Drawing.Size(88, 21);
      lblKeyServer.TabIndex = 1;
      lblKeyServer.Text = "Select key:";
      lblKeyServer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      // 
      // lblDescription
      // 
      this.lblDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.lblDescription.Location = new System.Drawing.Point(8, 3);
      this.lblDescription.Name = "lblDescription";
      this.lblDescription.Size = new System.Drawing.Size(503, 28);
      this.lblDescription.TabIndex = 0;
      // 
      // KeyForm
      // 
      this.AcceptButton = btnOK;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = btnCancel;
      this.ClientSize = new System.Drawing.Size(519, 92);
      this.Controls.Add(this.lblDescription);
      this.Controls.Add(lblKeyServer);
      this.Controls.Add(btnCancel);
      this.Controls.Add(btnOK);
      this.Controls.Add(this.keys);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "KeyForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Select Key";
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.ComboBox keys;
    private System.Windows.Forms.Label lblDescription;
  }
}