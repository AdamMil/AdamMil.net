namespace Sample
{
  partial class MainForm
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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
      this.imageList1 = new System.Windows.Forms.ImageList(this.components);
      this.btnDownload = new System.Windows.Forms.Button();
      this.button1 = new System.Windows.Forms.Button();
      this.primaryKeyList1 = new AdamMil.Security.UI.KeyManagementList();
      this.SuspendLayout();
      // 
      // imageList1
      // 
      this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
      this.imageList1.TransparentColor = System.Drawing.Color.Magenta;
      this.imageList1.Images.SetKeyName(0, "minus.gif");
      this.imageList1.Images.SetKeyName(1, "plus.gif");
      this.imageList1.Images.SetKeyName(2, "indent.gif");
      this.imageList1.Images.SetKeyName(3, "corner.gif");
      // 
      // btnDownload
      // 
      this.btnDownload.Location = new System.Drawing.Point(15, 346);
      this.btnDownload.Name = "btnDownload";
      this.btnDownload.Size = new System.Drawing.Size(136, 23);
      this.btnDownload.TabIndex = 1;
      this.btnDownload.Text = "Download key";
      this.btnDownload.UseVisualStyleBackColor = true;
      this.btnDownload.Click += new System.EventHandler(this.btnDownload_Click);
      // 
      // button1
      // 
      this.button1.Location = new System.Drawing.Point(170, 346);
      this.button1.Name = "button1";
      this.button1.Size = new System.Drawing.Size(87, 23);
      this.button1.TabIndex = 2;
      this.button1.Text = "button1";
      this.button1.UseVisualStyleBackColor = true;
      this.button1.Click += new System.EventHandler(this.button1_Click);
      // 
      // primaryKeyList1
      // 
      this.primaryKeyList1.AllowColumnReorder = true;
      this.primaryKeyList1.DisplayRevokers = true;
      this.primaryKeyList1.DisplaySubkeys = true;
      this.primaryKeyList1.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.primaryKeyList1.FullRowSelect = true;
      this.primaryKeyList1.Location = new System.Drawing.Point(15, 13);
      this.primaryKeyList1.Name = "primaryKeyList1";
      this.primaryKeyList1.PGP = null;
      this.primaryKeyList1.Size = new System.Drawing.Size(835, 326);
      this.primaryKeyList1.TabIndex = 0;
      this.primaryKeyList1.UseCompatibleStateImageBehavior = false;
      this.primaryKeyList1.View = System.Windows.Forms.View.Details;
      // 
      // MainForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(880, 435);
      this.Controls.Add(this.button1);
      this.Controls.Add(this.btnDownload);
      this.Controls.Add(this.primaryKeyList1);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.Name = "MainForm";
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
      this.Text = "MainForm";
      this.ResumeLayout(false);

    }

    #endregion

    private AdamMil.Security.UI.KeyManagementList primaryKeyList1;
    private System.Windows.Forms.ImageList imageList1;
    private System.Windows.Forms.Button btnDownload;
    private System.Windows.Forms.Button button1;
  }
}
