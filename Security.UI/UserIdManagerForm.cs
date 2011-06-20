/*
AdamMil.Security.UI is a .NET library providing common user interface widgets
for OpenPGP-based software.
Copyright (C) 2008-2011 Adam Milazzo (http://www.adammil.net/)

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

/// <summary>This form helps the user manage his user IDs.</summary>
public partial class UserIdManagerForm : Form
{
  /// <summary>Creates a new <see cref="UserIdManagerForm"/>. You must call <see cref="Initialize"/> to initialize the
  /// form before displaying it.
  /// </summary>
  public UserIdManagerForm()
  {
    InitializeComponent();
  }

  /// <summary>Initializes a new <see cref="UserIdManagerForm"/> with the <see cref="PGPSystem"/> that will be used to
  /// edit the key, and the key to edit.
  /// </summary>
  public UserIdManagerForm(PGPSystem pgp, PrimaryKey key) : this()
  {
    Initialize(pgp, key);
  }

  /// <summary>Initializes this form with the <see cref="PGPSystem"/> that will be used to edit the key, and the key to
  /// edit.
  /// </summary>
  public void Initialize(PGPSystem pgp, PrimaryKey key)
  {
    if(pgp == null || key == null) throw new ArgumentNullException();
    this.pgp = pgp;
    this.key = key;

    btnAddPhotoId.Enabled = btnAddUserId.Enabled = key.HasSecretKey;

    if(Visible) ReloadKey();
  }

  /// <include file="documentation.xml" path="/UI/Common/OnKeyDown/*"/>
  protected override void OnKeyDown(KeyEventArgs e)
  {
    base.OnKeyDown(e);

    if(!e.Handled && PGPUI.IsCloseKey(e))
    {
      Close();
      e.Handled = true;
    }
  }

  /// <include file="documentation.xml" path="/UI/Common/OnShown/*"/>
  protected override void OnShown(EventArgs e)
  {
    base.OnShown(e);
    ReloadKey(); // for some reason, if we do this earlier, then the ListView won't render the fonts
  }              // correctly until the form is redrawn...

  /// <summary>The index of the icon within the image list.</summary>
  const int UserIdIcon=0, PhotoIdIcon=1, UnknownIcon=2;

  /// <summary>Called when the given list item is activated by the user, either with the keyboard or mouse.</summary>
  void ActivateUserId(AttributeItem item)
  {
    // activating a photo ID will display it
    UserImage image = item.Attribute as UserImage;
    if(image != null) new PhotoIdForm(image).ShowDialog();
  }

  /// <summary>Checks whether all of the <see cref="UserId"/> attributes are selected. If so, a message is displayed
  /// to the user informing him that he can't perform the operation (such as delete) on all of the user IDs, because
  /// he must leave at least one. Returns true if at least one user ID is not selected.
  /// </summary>
  /// <param name="op">The name of the operation performed on the user IDs, such as "delete" or "revoke".</param>
  bool EnsureNotAllUserIdsSelected(string op)
  {
    // check if all user IDs are selected. if so, give an error because you can't delete or revoke all user IDs.
    bool hasUnselectedUserId = false;
    foreach(AttributeItem item in userIds.Items)
    {
      if(!item.Selected && item.Attribute is UserId)
      {
        hasUnselectedUserId = true;
        break;
      }
    }

    // show an error if the user tries to delete or revoke all the user IDs
    if(!hasUnselectedUserId)
    {
      bool onlyOne = userIds.SelectedItems.Count == 1;
      MessageBox.Show("You must leave at least one non-photo ID. If you don't want " +
                      (onlyOne ? "this ID" : "these IDs") + ", you must create your new ID before you "+op+" them.",
                      "Can't " + op + " all user IDs", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    return hasUnselectedUserId;
  }

  /// <summary>Gets the <see cref="UserAttribute"/> objects selected by the user.</summary>
  UserAttribute[] GetSelectedAttributes()
  {
    UserAttribute[] attributes = new UserAttribute[userIds.SelectedItems.Count];
    for(int i=0; i<attributes.Length; i++) attributes[i] = ((AttributeItem)userIds.SelectedItems[i]).Attribute;
    return attributes;
  }

  /// <summary>Reloads the key and redisplays its attributes and user IDs.</summary>
  void ReloadKey()
  {
    if(key != null)
    {
      key = pgp.RefreshKey(key, ListOptions.RetrieveAttributes | ListOptions.RetrieveSecretKeys);

      if(key == null)
      {
        foreach(Control control in Controls)
        {
          if(control != lblDescription) control.Enabled = false;
        }
        lblDescription.Text = "The key you were editing no longer exists.";
      }
      else
      {
        userIds.Items.Clear();

        foreach(UserId id in key.UserIds)
        {
          AttributeItem item = new AttributeItem(id, PGPUI.GetAttributeName(id));
          if(id.Revoked) item.Text += " (revoked)";
          item.ImageIndex = UserIdIcon;
          SetFont(item);
          userIds.Items.Add(item);
        }

        foreach(UserAttribute attr in key.Attributes)
        {
          bool isPhoto = attr is UserImage;
          AttributeItem item = new AttributeItem(attr, PGPUI.GetAttributeName(attr));
          if(attr.Revoked) item.Text += " (revoked)";
          item.ImageIndex = isPhoto ? PhotoIdIcon : UnknownIcon;
          SetFont(item);
          userIds.Items.Add(item);
        }
      }
    }
  }

  /// <summary>Sets the font of an <see cref="AttributeItem"/>, based on the properties of the
  /// <see cref="UserAttribute"/> it represents.
  /// </summary>
  void SetFont(AttributeItem item)
  {
    if(item.Attribute.Revoked)
    {
      item.Font = new Font(Font, FontStyle.Italic);
      item.ForeColor = SystemColors.GrayText;
    }
    else if(item.Attribute.Primary) item.Font = new Font(Font, FontStyle.Bold);
  }

  void btnAddPhotoId_Click(object sender, EventArgs e)
  {
    OpenFileDialog ofd = new OpenFileDialog();

    ofd.Filter = "Image files (*.jpg;*.png;*.gif;*.bmp;*.tif;*.jpeg;*.tiff)|"+
                 "*.jpg;*.png;*.gif;*.bmp;*.tif;*.jpeg;*.tiff|All files (*.*)|*.*";
    ofd.Title  = "Select the image file for your photo ID";
    ofd.SupportMultiDottedExtensions = true;
    if(ofd.ShowDialog() == DialogResult.OK)
    {
      NewPhotoIdForm form = new NewPhotoIdForm();

      try { form.Initialize(ofd.FileName); }
      catch(Exception ex)
      {
        if(ex is System.IO.IOException || ex is System.ArgumentException)
        {
          MessageBox.Show("The selected file is not a valid image, or no longer exists.", "Not an image",
                          MessageBoxButtons.OK, MessageBoxIcon.Error);
          return;
        }
        else
        {
          throw ex;
        }
      }

      if(form.ShowDialog() == DialogResult.OK)
      {
        PGPUI.DoWithErrorHandling("adding the photo", delegate { pgp.AddPhoto(key, form.Bitmap, null); });
        ReloadKey();
      }
    }
  }

  void btnAddUserId_Click(object sender, EventArgs e)
  {
    UserIdForm form = new UserIdForm();
    if(form.ShowDialog() == DialogResult.OK)
    {
      PGPUI.DoWithErrorHandling("adding the user ID",
                                delegate { pgp.AddUserId(key, form.RealName, form.Email, form.Comment, form.Preferences); });
      ReloadKey();
    }
  }

  void btnDelete_Click(object sender, EventArgs e)
  {
    if(userIds.SelectedItems.Count != 0)
    {
      if(!EnsureNotAllUserIdsSelected("delete")) return;

      // inform the user that deleting published user IDs is pointless, and get confirmation
      bool onlyOne = userIds.SelectedItems.Count == 1;
      string deleting = onlyOne ? "\""+userIds.SelectedItems[0].Text+"\"" : "multiple user IDs";
      string userId = (onlyOne ? "this user ID" : "these user IDs");
      string s = (onlyOne ? null : "s"), them = (onlyOne ? "it" : "them"), they = (onlyOne ? "it" : "they");
      if(MessageBox.Show("You are about to delete " + deleting + "! Note that you cannot retract a user ID once it "+
                         "has been distributed, so if this key (with " + userId + ") has ever been given to another "+
                         "person or uploaded to a public key server, you should revoke the user ID" + s + " instead "+
                         "of deleting " + them + ", because " + they + " would only be deleted from your machine, "+
                         "and could reappear in the future.\n\nAre you sure you want to delete " + userId + "?",
                         "Delete user IDs?",
                         MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2)
          == DialogResult.Yes)
      {
        PGPUI.DoWithErrorHandling("deleting a user ID", delegate { pgp.DeleteAttributes(GetSelectedAttributes()); });
        ReloadKey();
      }
    }
  }

  void btnRevoke_Click(object sender, EventArgs e)
  {
    if(userIds.SelectedItems.Count != 0)
    {
      if(!EnsureNotAllUserIdsSelected("revoke")) return;

      UserAttribute[] attrs = GetSelectedAttributes();
      UserRevocationForm form = new UserRevocationForm(attrs);
      if(form.ShowDialog() == DialogResult.OK)
      {
        PGPUI.DoWithErrorHandling("revoking a user ID", delegate { pgp.RevokeAttributes(form.Reason, attrs); });
        ReloadKey();
      }
    }
  }

  void btnSetPrimary_Click(object sender, EventArgs e)
  {
    if(userIds.SelectedIndices.Count == 1)
    {
      try
      {
        pgp.SetPreferences(((AttributeItem)userIds.SelectedItems[0]).Attribute, new UserPreferences(true));
        ReloadKey();
      }
      catch(OperationCanceledException) { }
    }
  }

  void userIds_KeyDown(object sender, KeyEventArgs e)
  {
    if(!e.Handled)
    {
      // Enter activates the selected item
      if(e.Modifiers == Keys.None && e.KeyCode == Keys.Enter && userIds.SelectedIndices.Count == 1)
      {
        ActivateUserId((AttributeItem)userIds.SelectedItems[0]);
        e.Handled = true;
      }
      else if(e.Modifiers == Keys.Control && e.KeyCode == Keys.A) // Ctrl-A selects all items
      {
        foreach(ListViewItem item in userIds.Items) item.Selected = true;
        e.Handled = true;
      }
    }
  }

  void userIds_MouseDoubleClick(object sender, MouseEventArgs e)
  {
    if(e.Button == MouseButtons.Left) // double-clicking an item selects it
    {
      ListViewItem selectedItem = userIds.GetItemAt(e.X, e.Y);
      if(selectedItem != null) ActivateUserId((AttributeItem)selectedItem);
    }
  }

  void userIds_SelectedIndexChanged(object sender, EventArgs e)
  {
    btnDelete.Enabled = btnRevoke.Enabled = userIds.SelectedIndices.Count != 0 && key.HasSecretKey;
    btnSetPrimary.Enabled = key.HasSecretKey && userIds.SelectedItems.Count == 1 &&
                            !((AttributeItem)userIds.SelectedItems[0]).Attribute.Primary;
  }

  PGPSystem pgp;
  PrimaryKey key;
}

} // namespace AdamMil.Security.UI