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
  partial class KeyServerSearchForm
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
      System.Windows.Forms.Label lblKeyServer;
      System.Windows.Forms.Label lblTerms;
      System.Windows.Forms.Label lblHelp;
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(KeyServerSearchForm));
      this.keyservers = new System.Windows.Forms.ComboBox();
      this.progressBar = new System.Windows.Forms.ProgressBar();
      this.btnImport = new System.Windows.Forms.Button();
      this.terms = new System.Windows.Forms.TextBox();
      this.btnSearch = new System.Windows.Forms.Button();
      this.results = new AdamMil.Security.UI.SearchResultsList();
      lblKeyServer = new System.Windows.Forms.Label();
      lblTerms = new System.Windows.Forms.Label();
      lblHelp = new System.Windows.Forms.Label();
      this.SuspendLayout();
      //
      // lblKeyServer
      //
      lblKeyServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      lblKeyServer.Location = new System.Drawing.Point(8, 464);
      lblKeyServer.Name = "lblKeyServer";
      lblKeyServer.Size = new System.Drawing.Size(92, 21);
      lblKeyServer.TabIndex = 1;
      lblKeyServer.Text = "Key Server:";
      lblKeyServer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblTerms
      //
      lblTerms.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      lblTerms.Location = new System.Drawing.Point(8, 491);
      lblTerms.Name = "lblTerms";
      lblTerms.Size = new System.Drawing.Size(92, 21);
      lblTerms.TabIndex = 2;
      lblTerms.Text = "Search Terms:";
      lblTerms.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      //
      // lblHelp
      //
      lblHelp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      lblHelp.Location = new System.Drawing.Point(8, 4);
      lblHelp.Name = "lblHelp";
      lblHelp.Size = new System.Drawing.Size(674, 50);
      lblHelp.TabIndex = 8;
      lblHelp.Text = resources.GetString("lblHelp.Text");
      //
      // keyservers
      //
      this.keyservers.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.keyservers.FormattingEnabled = true;
      this.keyservers.Location = new System.Drawing.Point(102, 464);
      this.keyservers.Name = "keyservers";
      this.keyservers.Size = new System.Drawing.Size(268, 21);
      this.keyservers.TabIndex = 1;
      //
      // progressBar
      //
      this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.progressBar.Location = new System.Drawing.Point(8, 519);
      this.progressBar.Name = "progressBar";
      this.progressBar.Size = new System.Drawing.Size(674, 23);
      this.progressBar.TabIndex = 3;
      //
      // btnImport
      //
      this.btnImport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.btnImport.Enabled = false;
      this.btnImport.Location = new System.Drawing.Point(457, 463);
      this.btnImport.Name = "btnImport";
      this.btnImport.Size = new System.Drawing.Size(75, 23);
      this.btnImport.TabIndex = 4;
      this.btnImport.Text = "&Import";
      this.btnImport.UseVisualStyleBackColor = true;
      this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
      //
      // terms
      //
      this.terms.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.terms.Location = new System.Drawing.Point(102, 491);
      this.terms.Name = "terms";
      this.terms.Size = new System.Drawing.Size(266, 21);
      this.terms.TabIndex = 2;
      this.terms.TextChanged += new System.EventHandler(this.terms_TextChanged);
      this.terms.KeyDown += new System.Windows.Forms.KeyEventHandler(this.terms_KeyDown);
      //
      // btnSearch
      //
      this.btnSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.btnSearch.Enabled = false;
      this.btnSearch.Location = new System.Drawing.Point(376, 463);
      this.btnSearch.Name = "btnSearch";
      this.btnSearch.Size = new System.Drawing.Size(75, 23);
      this.btnSearch.TabIndex = 3;
      this.btnSearch.Text = "&Search";
      this.btnSearch.UseVisualStyleBackColor = true;
      this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
      //
      // results
      //
      this.results.AllowColumnReorder = true;
      this.results.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.results.CheckBoxes = true;
      this.results.Font = new System.Drawing.Font("Arial", 8F);
      this.results.FullRowSelect = true;
      this.results.HideSelection = false;
      this.results.Location = new System.Drawing.Point(8, 55);
      this.results.Name = "results";
      this.results.Size = new System.Drawing.Size(674, 402);
      this.results.TabIndex = 0;
      this.results.UseCompatibleStateImageBehavior = false;
      this.results.View = System.Windows.Forms.View.Details;
      this.results.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.results_ItemChecked);
      //
      // KeyServerSearchForm
      //
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(690, 549);
      this.Controls.Add(this.btnSearch);
      this.Controls.Add(this.terms);
      this.Controls.Add(this.keyservers);
      this.Controls.Add(lblTerms);
      this.Controls.Add(this.btnImport);
      this.Controls.Add(this.progressBar);
      this.Controls.Add(lblKeyServer);
      this.Controls.Add(this.results);
      this.Controls.Add(lblHelp);
      this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.MinimumSize = new System.Drawing.Size(548, 315);
      this.Name = "KeyServerSearchForm";
      this.ShowIcon = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Download OpenPGP Keys";
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private SearchResultsList results;
    private System.Windows.Forms.ComboBox keyservers;
    private System.Windows.Forms.ProgressBar progressBar;
    private System.Windows.Forms.Button btnImport;
    private System.Windows.Forms.TextBox terms;
    private System.Windows.Forms.Button btnSearch;
  }
}