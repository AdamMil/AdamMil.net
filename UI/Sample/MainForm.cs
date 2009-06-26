using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using AdamMil.UI.RichDocument;
using HorizontalAlignment = AdamMil.UI.RichDocument.HorizontalAlignment;

namespace Sample
{
  public partial class MainForm : Form
  {
    public MainForm()
    {
      InitializeComponent();

      this.documentEditor1.Clear();

      this.documentEditor1.Text = "Before about 18 months ago, I recommended Breyer's ice cream to everyone as \"the best\", at least as far as what was available in the average US market. It was made with simple ingredients, and had the best texture of any ice cream available. Unlike some \"ice creams\" that are so far from the real thing that they won't even melt, Breyer's melted in the mouth most delightfully. And it tasted great, too. That's because they didn't add gum, or any of that other crap.\n\nThen one day, I noticed an abrupt change in the taste and texture of Breyer's ice cream. I looked at the ingredients and noticed that they had added gum. I went to their website and noticed that they had also sold their soul to Unilever Corporation, a huge conglomerate with a corporate mission to buy up all the unique brands with good reputations and followings, and turn them into mass-produced shit while wringing out as many bucks as they can until people notice. Now I recommend that people don't buy Breyer's ice cream.\n\nI quickly emailed Breyer's about it, and got a response from Unilever saying they had added gum to improve quality! Cutting through the corp-jargon, I noticed that what they were really saying is that they were trying to boost profits by reducing quality control in their distribution network, and after the ice cream started to melt and refreeze, turning icy, they decided to add gum to cover up the issue. Saying as much in my response, I got back a rather haughty reply saying \"At Breyers were proud of our all natural heritage!!\" [sic]\n\nI wrote back, noting that it was fitting that she had used the word \"heritage\" — something they (Unilever) had inherited from the previously-great Breyer's Ice Cream company, and which they were proceeding to thoroughly destroy. Do you remember the old Breyer's Ice Cream commercials? They usually featured a young child, trying to read the ingredients in a competitor's ice cream. Confronted with phrases like \"soy lecithin\", \"potassium sorbate\", and \"sodium nitrate\", the child struggled. When handed a box of Breyer's ice cream, he was able to quickly and happily read off the ingredients: milk, cream, sugar, and strawberries.";

      //this.documentEditor1.Text = "This is a long block of text.\nIt needs to split onto at least three lines. Well, I think this should do it.";
      //this.documentEditor1.Text = "A\n\nB\nC";
      //this.documentEditor1.Document.Root.Children.Add(CreateBlock(0));
      //this.documentEditor1.Document.Root.Children.Add(new TextNode("A\n\nB\nC\n"));

      /*DocumentNode block1 = CreateBlock(0), block2 = CreateBlock(1);
      block1.Style.BackColor = block2.Style.BackColor = Color.LightGray;
      block1.Style.Margin = block2.Style.Margin = new FourSide(new Measurement(4, Unit.Pixels));
      block1.Style.Padding = new FourSide(new Measurement(3, Unit.Pixels));
      block2.Style.Padding = new FourSide(new Measurement(6, Unit.Pixels));
      block2.Style.BorderWidth = new Measurement(2, Unit.Pixels);
      block2.Style.BorderStyle = AdamMil.UI.RichDocument.BorderStyle.Dashed;

      //this.documentEditor1.Document.Root.Children.Add(block1);
      //this.documentEditor1.Document.Root.Children.Add(block2);
      this.documentEditor1.Text = "This is a long block of text.\nIt needs to split onto at least three lines. Well, I think this should do it.";

      DocumentNode textBlock = this.documentEditor1.Document.Root.Children[0];
      textBlock.Parent.Children.Remove(textBlock);
      textBlock.Style.BackColor = Color.FromArgb(230, 255, 230);
      textBlock.Style.Margin = new FourSide(new Measurement(1, Unit.Millimeters));
      textBlock.Style.Padding = new FourSide(new Measurement(1, Unit.Millimeters));
      textBlock.Style.BorderWidth = new Measurement(1, Unit.Pixels);

      DocumentNode center = new DocumentNode();
      center.Layout = AdamMil.UI.RichDocument.Layout.Block;
      center.Style.Width = new Measurement(80, Unit.Percent);
      center.Style.HorizontalAlignment = HorizontalAlignment.Center;
      center.Style.BorderWidth = new Measurement(1, Unit.Pixels);
      center.Children.Add(textBlock);
      center.Children.Add(new TextNode(" More text."));
      this.documentEditor1.Document.Root.Children.Add(center);

      center = new DocumentNode();
      center.Layout = AdamMil.UI.RichDocument.Layout.Block;
      center.Style.BackColor = Color.FromArgb(240, 240, 255);
      center.Style.Width = new Measurement(80, Unit.Percent);
      center.Style.HorizontalAlignment = HorizontalAlignment.Center;
      center.Style.BorderWidth = new Measurement(1, Unit.Pixels);
      center.Style.Margin = new FourSide(new Measurement(), new Measurement(5, Unit.Pixels), new Measurement(), new Measurement());
      center.Children.Add(new TextNode("Here is another block of text. It's shorter than the other one."));
      this.documentEditor1.Document.Root.Children.Add(center);

      /*DocumentNode inline = new DocumentNode();
      inline.Layout = AdamMil.UI.RichDocument.Layout.Inline;
      inline.Children.Add(new TextNode("This is a bit of text. "));
      DocumentNode textBlock = new TextNode("Bold.");
      textBlock.Style.FontStyle = FontStyle.Bold;
      textBlock.Style.BorderWidth = new Measurement(1, Unit.Pixels);
      textBlock.Style.Margin = new FourSide(new Measurement(), new Measurement(1, Unit.Pixels));
      textBlock.Style.Padding = new FourSide(new Measurement(1, Unit.Pixels), new Measurement(2, Unit.Pixels));
      textBlock.Style.BackColor = Color.FromArgb(255, 220, 220);
      inline.Children.Add(textBlock);
      inline.Children.Add(new TextNode(" This is some more text."));
      inline.Style.BorderWidth = new Measurement(1, Unit.Pixels);
      inline.Style.Padding = new FourSide(new Measurement(3, Unit.Pixels));
      inline.Style.Margin = new FourSide(new Measurement(3, Unit.Pixels));
      inline.Style.BackColor = Color.FromArgb(236, 255, 236);
      this.documentEditor1.Document.Root.Children.Add(inline);*/

      this.documentEditor1.Document.Root.Style.Width = new Measurement(100, Unit.Percent);
      this.documentEditor1.Document.Root.Style.Padding = new FourSide(new Measurement(3, Unit.Pixels));
      this.documentEditor1.Document.Root.Style.HorizontalAlignment = HorizontalAlignment.Justify;
    }

    static DocumentNode CreateBlock(int n)
    {
      DocumentNode block = new DocumentNode();
      block.Layout = AdamMil.UI.RichDocument.Layout.Block;
      block.Style.BorderWidth = new Measurement(1, Unit.Pixels);
      block.Style.Margin = new FourSide(new Measurement(1, Unit.Pixels));

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

      DocumentNode block2 = new DocumentNode();
      block2.Layout = AdamMil.UI.RichDocument.Layout.Block;
      block2.Style.BorderWidth = new Measurement(1, Unit.Pixels);
      block2.Style.Width = block2.Style.Height = new Measurement(2, Unit.Millimeters);
      block2.Style.Margin = new FourSide(new Measurement(2, Unit.Pixels));
      block.Children.Add(block2);

      block2 = new DocumentNode();
      block2.Layout = AdamMil.UI.RichDocument.Layout.Block;
      block2.Style.BorderWidth = new Measurement(1, Unit.Pixels);
      block2.Style.Width = block2.Style.Height = new Measurement(2, Unit.Millimeters);
      block2.Style.Margin = new FourSide(new Measurement(2, Unit.Pixels));
      block.Children.Add(block2);
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