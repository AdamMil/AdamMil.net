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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace AdamMil.Security.UI
{

/// <summary>This form helps a user create a new photo ID by allowing them to easily crop and resize a photo.</summary>
public partial class NewPhotoIdForm : Form
{
  /// <summary>Creates a new <see cref="NewPhotoIdForm"/>. You must call <see cref="Initialize"/> to initialize the
  /// form.
  /// </summary>
  public NewPhotoIdForm()
  {
    InitializeComponent();
    cmbSize.SelectedIndex = 1;
  }

  /// <summary>Initializes a new <see cref="NewPhotoIdForm"/> with the name of a file containing the initial image.</summary>
  public NewPhotoIdForm(string imageFilename) : this()
  {
    Initialize(imageFilename);
  }

  /// <summary>Initializes a new <see cref="NewPhotoIdForm"/> with a stream containing the initial image data.</summary>
  public NewPhotoIdForm(Stream imageStream) : this()
  {
    Initialize(imageStream);
  }

  /// <summary>Gets the final bitmap created by the user.</summary>
  [Browsable(false)]
  public Bitmap Bitmap
  {
    get { return newBitmap; }
  }

  /// <summary>Gets or sets whether the user will be warned about images that are large and so will cause the primary
  /// key to become large. The default is true.
  /// </summary>
  public bool WarnAboutLargeImages
  {
    get { return warnAboutSize; }
    set { warnAboutSize = value; }
  }

  /// <summary>Initializes this form with the name of a file containing the initial image.</summary>
  public void Initialize(string imageFilename)
  {
    if(imageFilename == null) throw new ArgumentNullException();

    using(FileStream file = new FileStream(imageFilename, FileMode.Open, FileAccess.Read))
    {
      Initialize(file);
    }
  }

  /// <summary>Initializes this form with a stream containing the initial image data.</summary>
  public void Initialize(Stream imageStream)
  {
    if(imageStream == null) throw new ArgumentNullException();
    overlay.Bitmap = new Bitmap(imageStream);
  }

  /// <include file="documentation.xml" path="/UI/Common/OnMouseDown/*"/>
  protected override void OnMouseDown(MouseEventArgs e)
  {
    base.OnMouseDown(e);
    OnMouseDown(e.Button, e.Location);
  }

  /// <include file="documentation.xml" path="/UI/Common/OnMouseMove/*"/>
  protected override void OnMouseMove(MouseEventArgs e)
  {
    base.OnMouseMove(e);
    OnMouseMove(e.Location);
  }

  /// <include file="documentation.xml" path="/UI/Common/OnMouseUp/*"/>
  protected override void OnMouseUp(MouseEventArgs e)
  {
    base.OnMouseUp(e);
    OnMouseUp(e.Button, e.Location);
  }

  #region OverlayControl
  /// <summary>A control that displays a bitmap and an optional selection area overlayed on top of it.</summary>
  sealed class OverlayControl : Control
  {
    public OverlayControl()
    {
      SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer,
               true);
    }

    /// <summary>Gets or sets the bitmap shown in the overlay control. Setting this will reset the
    /// <see cref="BitmapRect"/> property.
    /// </summary>
    [Browsable(false)]
    public Bitmap Bitmap
    {
      get { return bitmap; }
      set
      {
        bitmap = value;
        if(value != null) bitmapRect = new RectangleF(0, 0, value.Width, value.Height);
      }
    }

    /// <summary>Gets or sets the region of the bitmap shown in the overlay control, in bitmap coordinates.</summary>
    [Browsable(false)]
    public RectangleF BitmapRect
    {
      get { return bitmapRect; }
      set
      {
        bitmapRect = value;
        Invalidate();
      }
    }

    /// <summary>Gets whether a region of the image is selected with an overlay rectangle.</summary>
    [Browsable(false)]
    public bool HasOverlay
    {
      get
      {
        RectangleF overlay = RectangleF.Intersect(OverlayRect, GetBitmapDestinationRect());
        return overlay.Width >= 1 && overlay.Height >= 1;
      }
    }

    /// <summary>Gets or sets the region of the control covered with the overlay rectangle, in client units.</summary>
    public RectangleF OverlayRect
    {
      get { return overlayRect; }
      set
      {
        overlayRect = value;
        Invalidate();
      }
    }

    /// <summary>Retrieves the region of the bitmap selected with an overlay rectangle, in bitmap coordinates.</summary>
    public RectangleF GetBitmapOverlay()
    {
      RectangleF bitmapDest = GetBitmapDestinationRect(); // get the region of the control with the bitmap on it
      // get the portion of the overlay within the bitmap
      RectangleF overlay = RectangleF.Intersect(OverlayRect, GetBitmapDestinationRect());
      // get a scale factor to convert from screen pixels to bitmap pixels
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
        RectangleF overlay = RectangleF.Intersect(OverlayRect, destRect);
        if(overlay.Width != 0 && overlay.Height != 0)
        {
          using(Brush brush = new SolidBrush(Color.FromArgb(112, 0, 0, 255)))
          {
            e.Graphics.FillRectangle(brush, overlay);
          }
          e.Graphics.DrawRectangle(Pens.Blue, overlay.X, overlay.Y, overlay.Width-1, overlay.Height-1);
        }
      }
    }

    /// <summary>Gets the region of the overlay control onto which the bitmap will be painted.</summary>
    RectangleF GetBitmapDestinationRect()
    {
      RectangleF destRect = ClientRectangle; // begin with the whole client area

      float srcAspect = (float)bitmapRect.Width/bitmapRect.Height, destAspect = (float)destRect.Width/destRect.Height;
      if(srcAspect > destAspect) // the image has a wider aspect than the control
      {
        if(bitmapRect.Width > destRect.Width) // and if the bitmap region is physically wider, then let the bitmap fill
        {                                     // the control horizontally, and we'll reduce the height
          destRect.Height = destRect.Width / srcAspect;
        }
        else // otherwise, the bitmap region fits within the control, so use the size as-is
        {
          destRect.Size = bitmapRect.Size;
        }
      }
      else // the image has a taller (or equal) aspect
      {
        if(bitmapRect.Height > destRect.Height) // since the aspect is taller, we have to test the height first
        {
          destRect.Width = destRect.Height * srcAspect;
        }
        else
        {
          destRect.Size = bitmapRect.Size;
        }
      }

      // now center the bitmap within the control
      destRect.Offset((Width - destRect.Width) * 0.5f, (Height - destRect.Height) * 0.5f);

      return destRect;
    }

    Bitmap bitmap;
    RectangleF bitmapRect, overlayRect;
  }
  #endregion

  /// <summary>Called when a drag of the overlay rectangle is canceled.</summary>
  void CancelDrag()
  {
    ResetOverlayRect();
    FinishDrag();
  }

  /// <summary>Called when a drag of the overlay rectangle is complete.</summary>
  void FinishDrag()
  {
    dragging = mouseDown = false;
  }

  /// <summary>Called when a mouse button is depressed within the form or any of its controls, and passed the mouse
  /// button and the location in the form's client units.
  /// </summary>
  void OnMouseDown(MouseButtons button, Point location)
  {
    if(button == MouseButtons.Left)
    {
      mouseDown = true;  // the mouse button is pressed
      dragging  = false; // but we're not dragging yet
      dragRect  = new Rectangle(PointToOverlay(location), new Size()); // start a new drag rectangle from this point
      SetOverlayRect(dragRect); // and reset the drag rectangle
    }
  }

  /// <summary>Called when a mouse is moved over the form or any of its controls, and passed the new cursor location
  /// in form client units.
  /// </summary>
  void OnMouseMove(Point location)
  {
    if((Control.MouseButtons & MouseButtons.Left) == 0) // if the left mouse button is not depressed
    {
      if(dragging) FinishDrag(); // if we're still dragging, then we missed the mouse up event somehow (because this
    }                            // method is not actually called for every control on the form...)
    else if(mouseDown) // otherwise, if the mouse button was pressed over the control
    {
      location = PointToOverlay(location); // calculate the distance from where it was first depressed
      int xd = location.X - dragRect.X, yd = location.Y - dragRect.Y;

      if(!dragging && xd*xd + yd*yd >= 4) dragging = true; // if we're not dragging, but the point has moved a bit,
                                                           // then start dragging
      if(dragging) // if we're dragging now (possibly just started)...
      {
        dragRect.Width  = xd; // then update the dimensions of the drag rectangle
        dragRect.Height = yd;

        // create a copy of the current drag rectangle, and fix up the coordinates if the width and/or height are
        // negative, caused by the user dragging the mouse above or to the left of the start position
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

        // now set the new overlay rectangle
        SetOverlayRect(overlayRect);
      }
    }
  }

  /// <summary>Called when the mouse is released over the form or any of its controls, and passed the button released
  /// and the location in the form's client units.
  /// </summary>
  void OnMouseUp(MouseButtons button, Point location)
  {
    if(dragging) // if we were dragging...
    {
      if((Control.MouseButtons & MouseButtons.Left) == 0) // but now the left mouse button is no longer depressed,
      {                                                   // either because it was released just now or earlier, then
        OnMouseMove(location);                            // finish the drag
        FinishDrag();
      }
    }
    else if(button == MouseButtons.Left) // otherwise, we weren't dragging, so if it was the left button, then this is
    {                                    // just a click, which will clear the drag rectangle
      CancelDrag();
    }
  }

  /// <summary>Coverts a point from the form's client units to the overlay control's client units.</summary>
  Point PointToOverlay(Point pt)
  {
    return new Point(pt.X - overlay.Left, pt.Y - overlay.Top);
  }

  /// <summary>Removes the overlay rectangle.</summary>
  void ResetOverlayRect()
  {
    SetOverlayRect(new Rectangle());
  }

  /// <summary>Given a bitmap size, and a maximum width and height, returns a size that fits within the maximums while
  /// preserving the original bitmap's aspect ratio.
  /// </summary>
  Size ResizeBitmap(SizeF bitmapSize, int maxWidth, int maxHeight)
  {
    float aspectRatio = bitmapSize.Width / bitmapSize.Height, boxAspect = (float)maxWidth / maxHeight;
    Size newSize = Size.Round(bitmapSize);

    if(aspectRatio > boxAspect) // if the image aspect is wider than the box aspect...
    {
      if(newSize.Width > maxWidth) // and the image is wider too, then shrink it.
      {
        newSize.Width  = maxWidth;
        newSize.Height = (int)Math.Round(maxWidth / aspectRatio);
      }
    }
    else // the image aspect is taller than or equal to the box aspect
    {
      if(newSize.Height > maxHeight)
      {
        newSize.Width  = (int)Math.Round(maxHeight * aspectRatio);
        newSize.Height = maxHeight;
      }
    }

    return newSize;
  }

  void SetOverlayRect(Rectangle rect)
  {
    overlay.OverlayRect = rect;
    btnCrop.Enabled = rect.Width != 0;
  }

  void btnCrop_Click(object sender, EventArgs e)
  {
    bitmapRects.Add(overlay.BitmapRect); // before the image is cropped, save the previous bitmap rectangle so the user
    btnUndo.Enabled = true;              // can undo the cropping
    overlay.BitmapRect = overlay.GetBitmapOverlay(); // then set the bitmap rect to the overlay rect
    ResetOverlayRect();
  }

  void btnDone_Click(object sender, EventArgs e)
  {
    RectangleF bitmapRect = overlay.BitmapRect;

    // figure out the final size of the bitmap
    Size newSize;
    switch(cmbSize.SelectedIndex)
    {
      case 0: newSize = ResizeBitmap(bitmapRect.Size, 96, 115); break;
      case 1: newSize = ResizeBitmap(bitmapRect.Size, 144, 173); break;
      case 2: newSize = ResizeBitmap(bitmapRect.Size, 240, 288); break;
      case 3: newSize = ResizeBitmap(bitmapRect.Size, 360, 432); break;
      default: newSize = Size.Round(bitmapRect.Size); break;
    }

    // then create the final bitmap
    Bitmap bitmap = new Bitmap(newSize.Width, newSize.Height, PixelFormat.Format24bppRgb);
    bitmap.Palette = overlay.Bitmap.Palette; // copy the palette, if there is one

    using(Graphics g = Graphics.FromImage(bitmap)) // and blit the original bitmap into the final bitmap
    {
      g.CompositingMode    = CompositingMode.SourceCopy;
      g.CompositingQuality = CompositingQuality.HighQuality;
      g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
      g.DrawImage(overlay.Bitmap, new RectangleF(0, 0, bitmap.Width, bitmap.Height), bitmapRect, GraphicsUnit.Pixel);
    }

    bool imageIsDone = true;
    if(WarnAboutLargeImages) // if we should warn the user about large images...
    {
      // then save the bitmap to a temp file so we can calculate how large it is
      string tmpFile = Path.GetTempFileName();
      try
      {
        using(FileStream file = new FileStream(tmpFile, FileMode.Truncate, FileAccess.Write))
        {
          bitmap.Save(file, ImageFormat.Jpeg);

          if(file.Length >= 8*1024)
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
    }

    if(imageIsDone)
    {
      newBitmap = bitmap;
      DialogResult = DialogResult.OK;
    }
  }

  void btnUndo_Click(object sender, EventArgs e)
  {
    overlay.BitmapRect = bitmapRects[bitmapRects.Count-1]; // restore the previous bitmap rectangle
    ResetOverlayRect();                                    // reset the overlay rectangle
    bitmapRects.RemoveAt(bitmapRects.Count-1);             // remove the previous bitmap rectangle from the undo list
    btnUndo.Enabled = bitmapRects.Count != 0;              // and update the Enabled state of the "Undo" button
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
  bool dragging, mouseDown, warnAboutSize = true;
}

} // namespace AdamMil.Security.UI