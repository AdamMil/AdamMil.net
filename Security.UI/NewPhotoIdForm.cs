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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace AdamMil.Security.UI
{

public partial class NewPhotoIdForm : Form
{
  public NewPhotoIdForm()
  {
    InitializeComponent();
    cmbSize.SelectedIndex = 1;
  }

  public NewPhotoIdForm(string initialImage) : this()
  {
    LoadImage(initialImage);
  }

  public Bitmap Bitmap
  {
    get { return newBitmap; }
  }

  public void LoadImage(string initialImage)
  {
    if(initialImage == null) throw new ArgumentNullException();

    using(FileStream file = new FileStream(initialImage, FileMode.Open, FileAccess.Read))
    {
      LoadImage(file);
    }
  }

  public void LoadImage(Stream stream)
  {
    if(stream == null) throw new ArgumentNullException();
    overlay.Bitmap = new Bitmap(stream);
  }

  protected override void OnMouseDown(MouseEventArgs e)
  {
    base.OnMouseDown(e);
    OnMouseDown(e.Button, e.Location);
  }

  protected override void OnMouseMove(MouseEventArgs e)
  {
    base.OnMouseMove(e);
    OnMouseMove(e.Location);
  }

  protected override void OnMouseUp(MouseEventArgs e)
  {
    base.OnMouseUp(e);
  }

  sealed class OverlayControl : Control
  {
    public OverlayControl()
    {
      SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer,
               true);
    }

    public Bitmap Bitmap
    {
      get { return bitmap; }
      set
      {
        bitmap = value;
        if(value != null) bitmapRect = new RectangleF(0, 0, value.Width, value.Height);
      }
    }

    public RectangleF BitmapRect
    {
      get { return bitmapRect; }
      set { bitmapRect = value; }
    }

    public bool HasOverlay
    {
      get
      {
        RectangleF overlay = RectangleF.Intersect(OverlayRect, GetBitmapDestinationRect());
        return overlay.Width >= 1 && overlay.Height >= 1;
      }
    }

    public RectangleF GetBitmapOverlay()
    {
      RectangleF bitmapDest = GetBitmapDestinationRect();
      RectangleF overlay = RectangleF.Intersect(OverlayRect, GetBitmapDestinationRect());
      float xScale = bitmapRect.Width / bitmapDest.Width, yScale = bitmapRect.Height / bitmapDest.Height;
      return new RectangleF((overlay.X - bitmapDest.X) * xScale + bitmapRect.X,
                            (overlay.Y - bitmapDest.Y) * yScale + bitmapRect.Y,
                            overlay.Width * xScale, overlay.Height * yScale);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);

      // draw the background image
      if(bitmap != null)
      {
        RectangleF destRect = GetBitmapDestinationRect();
        e.Graphics.DrawImage(bitmap, destRect, bitmapRect, GraphicsUnit.Pixel);

        // draw the overlay rectangle
        if(OverlayRect.Width != 0 && OverlayRect.Height != 0)
        {
          RectangleF rect = RectangleF.Intersect(OverlayRect, destRect);

          using(Brush brush = new SolidBrush(Color.FromArgb(112, 0, 0, 255)))
          {
            e.Graphics.FillRectangle(brush, rect);
          }
          e.Graphics.DrawRectangle(Pens.Blue, rect.X, rect.Y, rect.Width-1, rect.Height-1);
        }
      }
    }

    RectangleF GetBitmapDestinationRect()
    {
      RectangleF destRect = ClientRectangle;
      SizeF origSize = destRect.Size;

      float srcAspect = (float)bitmapRect.Width/bitmapRect.Height, destAspect = (float)destRect.Width/destRect.Height;
      if(srcAspect > destAspect) // the image has a wider aspect
      {
        if(bitmapRect.Width > destRect.Width)
        {
          destRect.Height = destRect.Width / srcAspect;
        }
        else if(bitmapRect.Height > destRect.Height)
        {
          destRect.Width = destRect.Height / srcAspect;
        }
        else
        {
          destRect.Size = bitmapRect.Size;
        }
      }
      else // the image has a taller (or equal) aspect
      {
        if(bitmapRect.Height > destRect.Height)
        {
          destRect.Width = destRect.Height * srcAspect;
        }
        else if(bitmapRect.Width > destRect.Width)
        {
          destRect.Height = destRect.Width * srcAspect;
        }
        else
        {
          destRect.Size = bitmapRect.Size;
        }
      }

      destRect.Offset((origSize.Width - destRect.Width) * 0.5f, (origSize.Height - destRect.Height) * 0.5f);

      return destRect;
    }

    public Rectangle OverlayRect;

    Bitmap bitmap;
    RectangleF bitmapRect;
  }

  void CancelDrag()
  {
    ResetOverlayRect();
    FinishDrag();
  }

  void FinishDrag()
  {
    dragging = mouseDown = false;
  }

  void OnMouseDown(MouseButtons button, Point location)
  {
    if(button == MouseButtons.Left)
    {
      mouseDown = true;
      dragging  = false;
      dragRect  = new Rectangle(PointToOverlay(location), new Size());
      SetOverlayRect(dragRect);
    }
  }

  void OnMouseMove(Point location)
  {
    if((Control.MouseButtons & MouseButtons.Left) == 0)
    {
      if(dragging) FinishDrag();
    }
    else if(mouseDown)
    {
      location = PointToOverlay(location);
      int xd = location.X - dragRect.X, yd = location.Y - dragRect.Y;

      if(!dragging && xd*xd + yd*yd >= 4) dragging = true;

      if(dragging)
      {
        dragRect.Width  = xd;
        dragRect.Height = yd;

        Rectangle overlayRect = dragRect;
        if(overlayRect.Width < 0)
        {
          overlayRect.X    += overlayRect.Width;
          overlayRect.Width = -overlayRect.Width;
        }

        if(overlayRect.Height < 0)
        {
          overlayRect.Y     += overlayRect.Height;
          overlayRect.Height = -overlayRect.Height;
        }
        SetOverlayRect(overlayRect);
      }
    }
  }

  void OnMouseUp(MouseButtons button, Point location)
  {
    if(dragging)
    {
      if((Control.MouseButtons & MouseButtons.Left) == 0)
      {
        OnMouseMove(location);
        FinishDrag();
      }
    }
    else
    {
      CancelDrag();
    }
  }

  Point PointToOverlay(Point pt)
  {
    return new Point(pt.X - overlay.Left, pt.Y - overlay.Top);
  }

  void ResetOverlayRect()
  {
    SetOverlayRect(new Rectangle());
  }

  SizeF ResizeBitmap(SizeF bitmapSize, int maxWidth, int maxHeight)
  {
    float aspectRatio = bitmapSize.Width / bitmapSize.Height, boxAspect = (float)maxWidth / maxHeight;
    SizeF newSize = bitmapSize;

    if(aspectRatio > boxAspect) // the image aspect is wider than the box aspect
    {
      if(newSize.Width > maxWidth)
      {
        newSize.Width  = maxWidth;
        newSize.Height = (float)Math.Round(maxWidth / aspectRatio);
      }
    }
    else // the image aspect is taller than or equal to the box aspect
    {
      if(newSize.Height > maxHeight)
      {
        newSize.Width  = (float)Math.Round(maxHeight * aspectRatio);
        newSize.Height = maxHeight;
      }
    }

    return newSize;
  }

  void SetOverlayRect(Rectangle rect)
  {
    overlay.OverlayRect = rect;
    overlay.Invalidate();
    btnCrop.Enabled = rect.Width != 0;
  }

  void btnCrop_Click(object sender, EventArgs e)
  {
    bitmapRects.Add(overlay.BitmapRect);
    btnUndo.Enabled = true;
    overlay.BitmapRect = overlay.GetBitmapOverlay();
    ResetOverlayRect();
  }

  void btnDone_Click(object sender, EventArgs e)
  {
    RectangleF bitmapRect = overlay.BitmapRect;
    SizeF newSize;

    switch(cmbSize.SelectedIndex)
    {
      case 0: newSize = ResizeBitmap(bitmapRect.Size, 96, 115); break;
      case 1: newSize = ResizeBitmap(bitmapRect.Size, 144, 173); break;
      case 2: newSize = ResizeBitmap(bitmapRect.Size, 240, 288); break;
      case 3: newSize = ResizeBitmap(bitmapRect.Size, 360, 432); break;
      default: newSize = bitmapRect.Size; break;
    }

    Bitmap bitmap = new Bitmap((int)Math.Round(newSize.Width), (int)Math.Round(newSize.Height),
                               PixelFormat.Format24bppRgb);
    bitmap.Palette = overlay.Bitmap.Palette; // copy the palette, if there is one

    using(Graphics g = Graphics.FromImage(bitmap))
    {
      g.CompositingMode    = CompositingMode.SourceCopy;
      g.CompositingQuality = CompositingQuality.HighQuality;
      g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
      g.DrawImage(overlay.Bitmap, new RectangleF(0, 0, bitmap.Width, bitmap.Height), bitmapRect,
                  GraphicsUnit.Pixel);
    }

    string tmpFile = Path.GetTempFileName();
    bool imageIsDone = true;
    try
    {
      using(FileStream file = new FileStream(tmpFile, FileMode.Truncate, FileAccess.Write))
      {
        bitmap.Save(file, ImageFormat.Jpeg);

        if(file.Length > 8*1024) // warn about large images
        {
          string sizeDesc = file.Length >= 32*1024 ? "extremely large" : file.Length >= 16*1024 ? "very large" :
                            "large";
          bool veryLarge = file.Length >= 16*1024;

          if(MessageBox.Show("This image is " + sizeDesc + " (" + file.Length.ToString() + " bytes), and adding it "+
                             "as a photo ID will cause your public key to become " + sizeDesc + " as well. " +
                             (veryLarge ? "It is recommended that you further reduce the size of the image. " : null) +
                             "Are you sure you want to use this image?", "Image is large",
                             MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) ==
             DialogResult.No)
          {
            imageIsDone = false;
          }
        }
      }
    }
    finally
    {
      File.Delete(tmpFile);
    }

    if(imageIsDone)
    {
      newBitmap = bitmap;
      DialogResult = DialogResult.OK;
    }
  }

  void btnUndo_Click(object sender, EventArgs e)
  {
    overlay.BitmapRect  = bitmapRects[bitmapRects.Count-1];
    overlay.OverlayRect = new Rectangle();
    overlay.Invalidate();
    bitmapRects.RemoveAt(bitmapRects.Count-1);
    btnCrop.Enabled = false;
    btnUndo.Enabled = bitmapRects.Count != 0;
  }

  void overlay_MouseDown(object sender, MouseEventArgs e)
  {
    OnMouseDown(e.Button, PointToClient(overlay.PointToScreen(e.Location)));
  }

  void overlay_MouseMove(object sender, MouseEventArgs e)
  {
    OnMouseMove(PointToClient(overlay.PointToScreen(e.Location)));
  }

  void overlay_MouseUp(object sender, MouseEventArgs e)
  {
    OnMouseUp(e.Button, PointToClient(overlay.PointToScreen(e.Location)));
  }

  List<RectangleF> bitmapRects = new List<RectangleF>();
  Bitmap newBitmap;
  Rectangle dragRect;
  bool dragging, mouseDown;
}

} // namespace AdamMil.Security.UI