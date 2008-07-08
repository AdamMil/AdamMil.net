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
using System.Drawing;
using System.Windows.Forms;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

public partial class UserIdManagerForm : Form
{
  UserIdManagerForm()
  {
    InitializeComponent();
  }

  public UserIdManagerForm(PGPSystem pgp, PrimaryKey key) : this()
  {
    if(pgp == null || key == null) throw new ArgumentNullException();
    this.pgp = pgp;
    this.key = key;
  }

  protected override void OnShown(EventArgs e)
  {
    base.OnShown(e);
    ReloadKey(); // for some reason, if we do this earlier, then the ListView won't render the fonts
  }              // correctly until the form is redrawn...

  const int UserIdImage=0, PhotoIdImage=1, UnknownImage=2;

  void ActivateUserId(ListViewItem item)
  {
    // activating a photo ID will display it
    UserImage image = item.Tag as UserImage;
    if(image != null) new PhotoIdForm(image).ShowDialog();
  }

  void AddUserIds()
  {
    userIds.Items.Clear();

    foreach(UserId id in key.UserIds)
    {
      ListViewItem item = new ListViewItem(id.Name);
      item.ImageIndex = UserIdImage;
      item.Tag        = id;
      SetFont(item, id);
      userIds.Items.Add(item);
    }

    foreach(UserAttribute attr in key.Attributes)
    {
      bool isPhoto = attr is UserImage;
      ListViewItem item = new ListViewItem(isPhoto ? "Photo Id" : "Unknown user attribute");
      item.ImageIndex = isPhoto ? PhotoIdImage : UnknownImage;
      item.Tag        = attr;
      SetFont(item, attr);
      userIds.Items.Add(item);
    }
  }

  bool EnsureNotAllUserIdsSelected(string op)
  {
    // first check if all user IDs are selected. if so, give an error because you can't delete all user IDs.
    bool hasUnselectedUserId = false;
    foreach(ListViewItem item in userIds.Items)
    {
      if(!item.Selected && item.Tag is UserId)
      {
        hasUnselectedUserId = true;
        break;
      }
    }

    // show an error if the user tries to delete all the user IDs
    if(!hasUnselectedUserId)
    {
      bool onlyOne = userIds.SelectedItems.Count == 1;
      MessageBox.Show("You must leave at least one non-photo ID. If you don't want " +
                      (onlyOne ? "this ID" : "these IDs") + ", you must create your new ID before you "+op+" them.",
                      "Can't " + op + " all user IDs", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    return hasUnselectedUserId;
  }

  UserAttribute[] GetSelectedAttributes()
  {
    UserAttribute[] attributes = new UserAttribute[userIds.SelectedItems.Count];
    for(int i=0; i<attributes.Length; i++) attributes[i] = (UserAttribute)userIds.SelectedItems[i].Tag;
    return attributes;
  }

  void ReloadKey()
  {
    if(key != null)
    {
      key = pgp.RefreshKey(key, ListOptions.RetrieveAttributes);

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
        AddUserIds();
      }
    }
  }

  void SetFont(ListViewItem item, UserAttribute attr)
  {
    if(attr.Revoked)
    {
      item.Font = new Font(Font, FontStyle.Italic);
      item.ForeColor = SystemColors.GrayText;
    }
    else if(attr.Primary) item.Font = new Font(Font, FontStyle.Bold);
  }

  void btnAddPhotoId_Click(object sender, EventArgs e)
  {
    NewPhotoIdForm form = new NewPhotoIdForm();
    form.ShowDialog();
    throw new NotImplementedException();
  }

  void btnAddUserId_Click(object sender, EventArgs e)
  {
    UserIdForm form = new UserIdForm();

    if(form.ShowDialog() == DialogResult.OK)
    {
      pgp.AddUserId(key, form.RealName, form.Email, form.Comment, form.Preferences);
      ReloadKey();
    }
  }

  void btnDelete_Click(object sender, EventArgs e)
  {
    if(userIds.SelectedItems.Count != 0)
    {
      if(!EnsureNotAllUserIdsSelected("delete")) return;

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
        pgp.DeleteAttributes(GetSelectedAttributes());
        ReloadKey();
      }
    }
  }

  void btnRevoke_Click(object sender, EventArgs e)
  {
    if(userIds.SelectedItems.Count != 0)
    {
      if(!EnsureNotAllUserIdsSelected("revoke")) return;

      UserRevocationForm form = new UserRevocationForm();
      foreach(ListViewItem item in userIds.SelectedItems) form.UserIdList.Add(item.Text);
      if(form.ShowDialog() == DialogResult.OK)
      {
        pgp.RevokeAttributes(form.Reason, GetSelectedAttributes());
        ReloadKey();
      }
    }
  }

  void btnSetPrimary_Click(object sender, EventArgs e)
  {
    if(userIds.SelectedIndices.Count == 1)
    {
      pgp.SetPreferences((UserAttribute)userIds.SelectedItems[0].Tag, new UserPreferences(true));
      ReloadKey();
    }
  }

  void userIds_KeyDown(object sender, KeyEventArgs e)
  {
    if(!e.Handled)
    {
      if(e.Modifiers == Keys.None && e.KeyCode == Keys.Enter && userIds.SelectedIndices.Count == 1)
      {
        ActivateUserId(userIds.SelectedItems[0]);
        e.Handled = true;
      }
      else if(e.Modifiers == Keys.Control && e.KeyCode == Keys.A)
      {
        foreach(ListViewItem item in userIds.Items) item.Selected = true;
        e.Handled = true;
      }
    }
  }

  void userIds_MouseDoubleClick(object sender, MouseEventArgs e)
  {
    if(e.Button == MouseButtons.Left)
    {
      ListViewItem selectedItem = userIds.GetItemAt(e.X, e.Y);
      if(selectedItem != null) ActivateUserId(selectedItem);
    }
  }

  void userIds_SelectedIndexChanged(object sender, EventArgs e)
  {
    btnDelete.Enabled = btnRevoke.Enabled = userIds.SelectedIndices.Count != 0;
    btnSetPrimary.Enabled = userIds.SelectedItems.Count == 1 && !((UserAttribute)userIds.SelectedItems[0].Tag).Primary;
  }

  readonly PGPSystem pgp;
  PrimaryKey key;
}

} // namespace AdamMil.Security.UI