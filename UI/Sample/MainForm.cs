using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using AdamMil.UI.RichDocument;

namespace Sample
{
  public partial class MainForm : Form
  {
    public MainForm()
    {
      InitializeComponent();

      this.documentEditor1.Clear();

      /*DocumentNode block1 = CreateBlock(0), block2 = CreateBlock(1);
      block1.Style.BackColor = block2.Style.BackColor = Color.LightGray;
      block1.Style.Margin = block2.Style.Margin = new FourSide(new Measurement(4, Unit.Pixels));
      block1.Style.Padding = new FourSide(new Measurement(3, Unit.Pixels));
      block2.Style.Padding = new FourSide(new Measurement(6, Unit.Pixels));
      block2.Style.BorderWidth = new Measurement(2, Unit.Pixels);
      block2.Style.BorderStyle = AdamMil.UI.RichDocument.BorderStyle.Dashed;

      //this.documentEditor1.Document.Root.Children.Add(block1);
      //this.documentEditor1.Document.Root.Children.Add(block2);
      this.documentEditor1.Text = "This is a long block of text. It needs to split onto at least three lines. Well, I think this should do it.";

      DocumentNode textBlock = this.documentEditor1.Document.Root.Children[0];
      textBlock.Parent.Children.Remove(textBlock);
      textBlock.Style.BackColor = Color.FromArgb(230, 255, 230);

      DocumentNode center = new DocumentNode();
      center.Layout = AdamMil.UI.RichDocument.Layout.Block;
      center.Style.Width = new Measurement(80, Unit.Percent);
      center.Style.HorizontalAlignment = HorizontalAlignment.Center;
      center.Style.BorderWidth = new Measurement(1, Unit.Pixels);
      center.Children.Add(textBlock);
      this.documentEditor1.Document.Root.Children.Add(center);*/

      DocumentNode inline = new DocumentNode();
      inline.Layout = AdamMil.UI.RichDocument.Layout.Inline;
      inline.Children.Add(new TextNode("Here is another block of text. "));
      DocumentNode textBlock = new TextNode("Bold.");
      textBlock.Style.FontStyle = FontStyle.Bold;
      inline.Children.Add(textBlock);
      inline.Children.Add(new TextNode(" It's shorter than the other one."));
      this.documentEditor1.Document.Root.Children.Add(inline);

      /*DocumentNode center = new DocumentNode();
      center.Layout = AdamMil.UI.RichDocument.Layout.Block;
      center.Style.BackColor = Color.FromArgb(240, 240, 255);
      center.Style.Width = new Measurement(80, Unit.Percent);
      center.Style.HorizontalAlignment = HorizontalAlignment.Center;
      center.Style.BorderWidth = new Measurement(1, Unit.Pixels);
      center.Style.Margin = new FourSide(new Measurement(), new Measurement(5, Unit.Pixels), new Measurement(), new Measurement());
      center.Children.Add(inline);
      this.documentEditor1.Document.Root.Children.Add(center);*/
      
      this.documentEditor1.Document.Root.Style.Width = new Measurement(100, Unit.Percent);
      this.documentEditor1.Document.Root.Style.HorizontalAlignment = HorizontalAlignment.Center;
    }

    static DocumentNode CreateBlock(int n)
    {
      DocumentNode block = new DocumentNode();
      block.Layout = AdamMil.UI.RichDocument.Layout.Block;

      TextNode node = new TextNode("Hello, ");
      node.Style.BackColor = Color.FromArgb(255, 224, 224);
      node.Style.FontSize = new Measurement(18, Unit.Points);
      block.Children.Add(node);

      node = new TextNode("world! ");
      node.Style.BackColor = Color.FromArgb(224, 255, 224);
      node.Style.FontNames = new string[] { "Times New Roman" };
      node.Style.FontSize = new Measurement(14, Unit.Points);
      block.Children.Add(node);

      node = new TextNode("How are you?");
      node.Style.BackColor = Color.FromArgb(224, 224, 255);
      node.Style.FontSize = new Measurement(12, Unit.Points);
      if(n != 0)
      {
        FourSide fourSide = new FourSide();
        fourSide.SetHorizontal(new Measurement(2, Unit.Pixels));
        node.Style.Margin = node.Style.Padding = fourSide;
        node.Style.BorderWidth = new Measurement(1, Unit.Pixels);
      }
      block.Children.Add(node);
      /*
      node = new TextNode("are ");
      node.Style.BackColor = Color.FromArgb(255, 224, 224);
      node.Style.FontSize = new Measurement(20, Unit.Points);
      node.Style.FontNames = new string[] { "mono" };
      block.Children.Add(node);

      node = new TextNode("you?");
      node.Style.BackColor = Color.FromArgb(224, 255, 224);
      node.Style.FontSize = new Measurement(8, Unit.Points);
      block.Children.Add(node);*/

      return block;
    }
  }
}