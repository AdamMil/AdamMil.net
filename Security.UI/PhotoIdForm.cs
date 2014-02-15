/*
AdamMil.Security.UI is a .NET library providing common user interface widgets
for OpenPGP-based software.
Copyright (C) 2008-2013 Adam Milazzo (http://www.adammil.net/)

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
using System.Drawing;
using System.Windows.Forms;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

/// <summary>Displays a user's photo ID.</summary>
public partial class PhotoIdForm : Form
{
  /// <summary>Creates a new <see cref="PhotoIdForm"/>. You must call <see cref="Initialize"/> to initialize the form.</summary>
  public PhotoIdForm()
  {
    InitializeComponent();

    // save the layout made in the designer so we can recreate it on resize
    topSpace        = picture.Top;
    bottomSpace     = Height - picture.Bottom;
    horizontalSpace = picture.Left;
  }

  /// <summary>Initializes a new <see cref="PhotoIdForm"/> with the given <see cref="UserImage"/>.</summary>
  public PhotoIdForm(UserImage photoId) : this()
  {
    Initialize(photoId);
  }

  /// <summary>Initializes this form with the given <see cref="UserImage"/>.</summary>
  public void Initialize(UserImage photoId)
  {
    if(photoId == null) throw new ArgumentNullException();

    Text = "Photo ID for " + photoId.PrimaryKey.PrimaryUserId.Name;
    lblId.Text = Text + "\nKey ID: " + photoId.PrimaryKey.ShortKeyId;

    picture.Image = photoId.GetBitmap();

    // set the initial height of the form based on the picture
    Height = Math.Min(picture.Image.Height + 2, 384) + topSpace + bottomSpace;
  }

  /// <include file="documentation.xml" path="/UI/Common/OnKeyDown/node()"/>
  protected override void OnKeyDown(KeyEventArgs e)
  {
    base.OnKeyDown(e);

    if(!e.Handled && PGPUI.IsCloseKey(e))
    {
      Close();
      e.Handled = true;
    }
  }

  /// <include file="documentation.xml" path="/UI/Common/OnSizeChanged/node()"/>
  protected override void OnSizeChanged(EventArgs e)
  {
    base.OnSizeChanged(e);

    if(picture.Image != null) // if we have a picture to display, we should resize it
    {
      // calculate the maximum control size
      Size maxCtlSize = new Size(Width - horizontalSpace*2, Height - topSpace - bottomSpace);
      Size imageSize  = new Size(picture.Image.Width, picture.Image.Height);

      // if the image is larger than the maximum control size, it needs to be shrunk to fit.
      if(imageSize.Width > maxCtlSize.Width || imageSize.Height > maxCtlSize.Height)
      {
        picture.SizeMode = PictureBoxSizeMode.Zoom;
        picture.Size     = new Size(Math.Min(imageSize.Width, maxCtlSize.Width),
                                    Math.Min(imageSize.Height, maxCtlSize.Height));
      }
      else // the image is not bigger, so just center it in the picture box
      {
        picture.SizeMode = PictureBoxSizeMode.CenterImage;
        picture.Size     = imageSize;
      }

      // in any case, center the picture box
      picture.Location = new Point((maxCtlSize.Width - picture.Width) / 2 + horizontalSpace, topSpace);
    }
  }

  readonly int topSpace, bottomSpace, horizontalSpace;
}

} // namespace AdamMil.Security.UI