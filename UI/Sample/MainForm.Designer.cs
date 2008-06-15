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
      this.documentEditor1 = new AdamMil.UI.RichDocument.DocumentEditor();
      this.SuspendLayout();
      // 
      // documentEditor1
      // 
      this.documentEditor1.BackColor = System.Drawing.SystemColors.Window;
      this.documentEditor1.CursorIndex = 0;
      this.documentEditor1.Dock = System.Windows.Forms.DockStyle.Fill;
      this.documentEditor1.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.documentEditor1.ForeColor = System.Drawing.SystemColors.WindowText;
      this.documentEditor1.Location = new System.Drawing.Point(0, 0);
      this.documentEditor1.Name = "documentEditor1";
      this.documentEditor1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
      this.documentEditor1.ScrollPosition = new System.Drawing.Point(0, 0);
      this.documentEditor1.Size = new System.Drawing.Size(292, 273);
      this.documentEditor1.TabIndex = 0;
      this.documentEditor1.Text = "In grid-fitted rendering (the default), font hinting usually changes the width of glyphs. When a sequence of glyphs all increase significantly in width GDI+ may have to close up the text to remain resolution independent. In pathological cases (such as a long run of bold lower case 'l's in 8 pt Microsoft Sans Serif on a 96 dpi display), the space between some letters can disappear completely.\n\nthisisoneverylongwordthatshouldn'tbreaksoeasily";
      // 
      // MainForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(292, 273);
      this.Controls.Add(this.documentEditor1);
      this.Name = "MainForm";
      this.Text = "MainForm";
      this.ResumeLayout(false);

    }

    #endregion

    private AdamMil.UI.RichDocument.DocumentEditor documentEditor1;
  }
}