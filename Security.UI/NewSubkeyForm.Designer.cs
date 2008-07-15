namespace AdamMil.Security.UI
{
  partial class NewSubkeyForm
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
      System.Windows.Forms.Label lblSubExpire;
      System.Windows.Forms.Label lblSubLength;
      System.Windows.Forms.Label lblSubType;
      System.Windows.Forms.Button btnOK;
      System.Windows.Forms.Button btnCancel;
      this.chkNoExpiration = new System.Windows.Forms.CheckBox();
      this.keyExpiration = new System.Windows.Forms.DateTimePicker();
      this.keyLength = new System.Windows.Forms.ComboBox();
      this.keyType = new System.Windows.Forms.ComboBox();
      lblSubExpire = new System.Windows.Forms.Label();
      lblSubLength = new System.Windows.Forms.Label();
      lblSubType = new System.Windows.Forms.Label();
      btnOK = new System.Windows.Forms.Button();
      btnCancel = new System.Windows.Forms.Button();
      this.SuspendLayout();
      // 
      // chkNoExpiration
      // 
      this.chkNoExpiration.Location = new System.Drawing.Point(201, 64);
      this.chkNoExpiration.Name = "chkNoExpiration";
      this.chkNoExpiration.Size = new System.Drawing.Size(117, 17);
      this.chkNoExpiration.TabIndex = 20;
      this.chkNoExpiration.Text = "No expiration";
      this.chkNoExpiration.UseVisualStyleBackColor = true;
      this.chkNoExpiration.CheckedChanged += new System.EventHandler(this.chkNoExpiration_CheckedChanged);
      // 
      // keyExpiration
      // 
      this.keyExpiration.Format = System.Windows.Forms.DateTimePickerFormat.Short;
      this.keyExpiration.Location = new System.Drawing.Point(73, 61);
      this.keyExpiration.Name = "keyExpiration";
      this.keyExpiration.Size = new System.Drawing.Size(121, 21);
      this.keyExpiration.TabIndex = 19;
      // 
      // lblSubExpire
      // 
      lblSubExpire.Location = new System.Drawing.Point(1, 61);
      lblSubExpire.Name = "lblSubExpire";
      lblSubExpire.Size = new System.Drawing.Size(66, 20);
      lblSubExpire.TabIndex = 18;
      lblSubExpire.Text = "Expiration";
      lblSubExpire.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      // 
      // keyLength
      // 
      this.keyLength.FormattingEnabled = true;
      this.keyLength.Location = new System.Drawing.Point(73, 35);
      this.keyLength.Name = "keyLength";
      this.keyLength.Size = new System.Drawing.Size(121, 21);
      this.keyLength.TabIndex = 17;
      // 
      // lblSubLength
      // 
      lblSubLength.Location = new System.Drawing.Point(1, 35);
      lblSubLength.Name = "lblSubLength";
      lblSubLength.Size = new System.Drawing.Size(66, 20);
      lblSubLength.TabIndex = 16;
      lblSubLength.Text = "Key Size";
      lblSubLength.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      // 
      // keyType
      // 
      this.keyType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.keyType.FormattingEnabled = true;
      this.keyType.Location = new System.Drawing.Point(73, 9);
      this.keyType.Name = "keyType";
      this.keyType.Size = new System.Drawing.Size(254, 21);
      this.keyType.TabIndex = 15;
      this.keyType.SelectedIndexChanged += new System.EventHandler(this.keyType_SelectedIndexChanged);
      // 
      // lblSubType
      // 
      lblSubType.Location = new System.Drawing.Point(1, 9);
      lblSubType.Name = "lblSubType";
      lblSubType.Size = new System.Drawing.Size(66, 20);
      lblSubType.TabIndex = 14;
      lblSubType.Text = "Key Type";
      lblSubType.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      // 
      // btnOK
      // 
      btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnOK.Location = new System.Drawing.Point(171, 91);
      btnOK.Name = "btnOK";
      btnOK.Size = new System.Drawing.Size(75, 23);
      btnOK.TabIndex = 21;
      btnOK.Text = "&OK";
      btnOK.UseVisualStyleBackColor = true;
      btnOK.Click += new System.EventHandler(this.btnOK_Click);
      // 
      // btnCancel
      // 
      btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      btnCancel.Location = new System.Drawing.Point(252, 91);
      btnCancel.Name = "btnCancel";
      btnCancel.Size = new System.Drawing.Size(75, 23);
      btnCancel.TabIndex = 22;
      btnCancel.Text = "&Cancel";
      btnCancel.UseVisualStyleBackColor = true;
      // 
      // NewSubkeyForm
      // 
      this.AcceptButton = btnOK;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = btnCancel;
      this.ClientSize = new System.Drawing.Size(333, 120);
      this.Controls.Add(btnOK);
      this.Controls.Add(btnCancel);
      this.Controls.Add(this.chkNoExpiration);
      this.Controls.Add(this.keyExpiration);
      this.Controls.Add(lblSubExpire);
      this.Controls.Add(this.keyLength);
      this.Controls.Add(lblSubLength);
      this.Controls.Add(this.keyType);
      this.Controls.Add(lblSubType);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "NewSubkeyForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "New Subkey";
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.CheckBox chkNoExpiration;
    private System.Windows.Forms.DateTimePicker keyExpiration;
    private System.Windows.Forms.ComboBox keyLength;
    private System.Windows.Forms.ComboBox keyType;

  }
}