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
using System.Threading;
using System.Windows.Forms;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

public partial class GenerateKeyForm : Form
{
  public GenerateKeyForm()
  {
    InitializeComponent();
  }

  public GenerateKeyForm(PGPSystem pgp) : this(pgp, null) { }

  public GenerateKeyForm(PGPSystem pgp, Keyring keyring) : this()
  {
    Initialize(pgp, keyring);
  }

  public void Initialize(PGPSystem pgp, Keyring keyring)
  {
    if(pgp == null) throw new ArgumentNullException();
    this.pgp = pgp;
    this.keyring = keyring;

    UpdatePasswordStrength();

    KeyCapabilities signOnly = KeyCapabilities.Sign;
    KeyCapabilities encryptOnly = KeyCapabilities.Encrypt;
    KeyCapabilities signAndEncrypt = signOnly | encryptOnly;
    keyType.Items.Clear();
    keyType.Items.Add(new KeyTypeItem(KeyType.DSA, signOnly | KeyCapabilities.Certify, "DSA (signing only)"));
    keyType.Items.Add(new KeyTypeItem(KeyType.RSA, signOnly | KeyCapabilities.Certify, "RSA (signing only)"));
    keyType.Items.Add(new KeyTypeItem(KeyType.RSA, signAndEncrypt | KeyCapabilities.Certify, "RSA (sign and encrypt)"));
    keyType.SelectedIndex = 0;

    subkeyType.Items.Clear();
    subkeyType.Items.Add(new KeyTypeItem(null, 0, "None"));
    subkeyType.Items.Add(new KeyTypeItem(KeyType.ElGamal, encryptOnly, "El Gamal (encryption only)"));
    subkeyType.Items.Add(new KeyTypeItem(KeyType.RSA, encryptOnly, "RSA (encryption only)"));
    subkeyType.Items.Add(new KeyTypeItem(KeyType.DSA, signOnly, "DSA (signing only)"));
    subkeyType.Items.Add(new KeyTypeItem(KeyType.RSA, signOnly, "RSA (signing only)"));
    subkeyType.Items.Add(new KeyTypeItem(KeyType.RSA, signAndEncrypt, "RSA (sign and encrypt)"));
    subkeyType.SelectedIndex = 1;

    // OpenPGP currently supports a maximum expiration date of February 25, 2174. we'll use the 24th to avoid
    // local <-> UTC conversion problems
    keyExpiration.MinDate = subkeyExpiration.MinDate = DateTime.Now.Date.AddDays(1);
    keyExpiration.MaxDate = subkeyExpiration.MaxDate = new DateTime(2174, 2, 24, 0, 0, 0, DateTimeKind.Local);
  }

  protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
  {
    base.OnClosing(e);
    if(!btnCancel.Enabled) e.Cancel = true;
  }

  sealed class KeyTypeAndCapability
  {
    public KeyTypeAndCapability(string type, KeyCapabilities caps)
    {
      Type         = type;
      Capabilities = caps;
    }

    public string Type;
    public KeyCapabilities Capabilities;
  }

  sealed class KeyTypeItem : ListItem<KeyTypeAndCapability>
  {
    public KeyTypeItem(string type, KeyCapabilities caps, string text)
      : base(new KeyTypeAndCapability(type, caps), text) { }
  }

  void GenerationFailed(Exception ex)
  {
    GenerationFinished();

    MessageBox.Show("The key generation failed. The error was: " + ex.Message, "Key generation failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
  }

  void GenerationFinished()
  {
    thread = null;

    progressBar.Style = ProgressBarStyle.Blocks;

    btnGenerate.Enabled = grpPrimary.Enabled = grpPassword.Enabled = grpSubkey.Enabled =
      grpUser.Enabled = true;

    btnCancel.Text = "&Close";
  }

  void GenerationSucceeded()
  {
    GenerationFinished();

    MessageBox.Show("Key generation successful.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
  }

  void UpdateKeyLengths(ComboBox keyTypes, ComboBox keyLengths)
  {
    int selectedKeyLength;
    if(keyLengths.Items.Count == 0) selectedKeyLength = 0;
    else if(keyLengths.SelectedIndex == -1) int.TryParse(keyLengths.Text, out selectedKeyLength);
    else selectedKeyLength = ((ListItem<int>)keyLengths.SelectedItem).Value;

    keyLengths.Items.Clear();

    string keyType = keyTypes.Items.Count == 0 ? null : ((KeyTypeItem)keyTypes.SelectedItem).Value.Type;
    if(keyType == null)
    {
      keyLengths.Enabled = false;
    }
    else
    {
      keyLengths.Enabled = true;

      keyLengths.Items.Add(new ListItem<int>(0, "Default"));
      keyLengths.SelectedIndex = 0;

      int maxKeyLength = pgp.GetMaximumKeyLength(keyType);
      for(int i=1,length=1024; length <= maxKeyLength; i++,length += 1024)
      {
        keyLengths.Items.Add(new ListItem<int>(length, length.ToString()));
        if(length == selectedKeyLength) keyLengths.SelectedIndex = i;
      }

      if(selectedKeyLength > maxKeyLength) keyLengths.SelectedIndex = keyLengths.Items.Count-1;
    }
  }

  void UpdatePasswordStrength()
  {
    lblStrength.Text = "Estimated password strength: " +
                       PGPUI.GetPasswordStrengthDescription(txtPass1.GetPasswordStrength());
  }

  bool ValidateKeyLength(ComboBox keyType, ComboBox keyLength, string keyName)
  {
    if(keyLength.SelectedIndex == -1)
    {
      int customLength;
      if(!int.TryParse(keyLength.Text, out customLength) || customLength < 0)
      {
        MessageBox.Show(keyLength.Text + " is not a valid " + keyName + " size.", "Invalid key size",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
      }

      int maxLength = pgp.GetMaximumKeyLength(((KeyTypeItem)keyType.SelectedItem).Value.Type);
      if(customLength > maxLength)
      {
        MessageBox.Show("The " + keyName + " size is greater than the maximum supported size of " +
                        maxLength.ToString() + " bits.", "Key size too large", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
        return false;
      }
    }

    return true;
  }

  void userId_TextChanged(object sender, EventArgs e)
  {
    string realName = txtName.Text.Trim(), email = txtEmail.Text.Trim(), comment = txtComment.Text.Trim();

    if(string.IsNullOrEmpty(realName))
    {
      lblUserId.Text = "Please enter your new user ID above.";
      btnGenerate.Enabled = false;
    }
    else
    {
      lblUserId.Text = "Your user ID will be displayed as:\n" + realName +
                       (!string.IsNullOrEmpty(comment) ? " ("+comment+")" : null) +
                       (!string.IsNullOrEmpty(email) ? " <"+email+">" : null);
      btnGenerate.Enabled = true;
    }
  }

  void txtPass1_TextChanged(object sender, EventArgs e)
  {
    UpdatePasswordStrength();
  }

  void keyType_SelectedIndexChanged(object sender, EventArgs e)
  {
    UpdateKeyLengths(keyType, keyLength);
  }

  void subkeyType_SelectedIndexChanged(object sender, EventArgs e)
  {
    UpdateKeyLengths(subkeyType, subkeyLength);
    chkSubkeyNoExpiration.Enabled = subkeyLength.Enabled;
    subkeyExpiration.Enabled = chkSubkeyNoExpiration.Enabled && !chkSubkeyNoExpiration.Checked;
  }

  void chkKeyNoExpiration_CheckedChanged(object sender, EventArgs e)
  {
    keyExpiration.Enabled = !chkKeyNoExpiration.Checked;
  }

  void chkSubkeyNoExpiration_CheckedChanged(object sender, EventArgs e)
  {
    subkeyExpiration.Enabled = !chkSubkeyNoExpiration.Checked;
  }

  void btnCancel_Click(object sender, EventArgs e)
  {
    if(thread != null) // it's currently a "Cancel" button
    {
      if(MessageBox.Show("Key generation is currently in progress. Cancel it?", "Cancel key generation?",
                         MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) ==
         DialogResult.Yes)
      {
        // grab a local copy so it doesn't get pulled out from under us
        Thread localThread = this.thread;
        if(localThread != null)
        {
          try { localThread.Abort(); }
          catch { }
          GenerationFinished();
        }
      }
    }
    else // it's a "Close" button
    {
      DialogResult = DialogResult.OK;
    }
  }

  void btnGenerate_Click(object sender, EventArgs e)
  {
    string realName = txtName.Text.Trim(), email = txtEmail.Text.Trim(), comment = txtComment.Text.Trim();

    if(string.IsNullOrEmpty(realName))
    {
      MessageBox.Show("You must enter your name.", "Name required", MessageBoxButtons.OK, MessageBoxIcon.Error);
      return;
    }
    else if(!string.IsNullOrEmpty(email) && !PGPUI.IsValidEmail(email))
    {
      MessageBox.Show(email + " is not a valid email address.", "Invalid email",
                      MessageBoxButtons.OK, MessageBoxIcon.Error);
      return;
    }

    if(!PGPUI.ArePasswordsEqual(txtPass1, txtPass2))
    {
      MessageBox.Show("The passwords you have entered do not match.", "Password mismatch", MessageBoxButtons.OK,
                      MessageBoxIcon.Error);
      return;
    }
    else if(txtPass1.TextLength == 0)
    {
      if(MessageBox.Show("You didn't enter a password! This is extremely insecure, as anybody can use your key. Are "+
                         "you sure you don't want a password?", "Password is blank!", MessageBoxButtons.YesNo,
                         MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
      {
        return;
      }
    }
    else if(txtPass1.GetPasswordStrength() < PasswordStrength.Moderate)
    {
      if(MessageBox.Show("You entered a weak password! This is not secure, as your password can be cracked in a "+
                         "relatively short period of time, allowing somebody access to your key. Are you sure you "+
                         "want a to use a weak password?", "Password is weak!", MessageBoxButtons.YesNo,
                         MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
      {
        return;
      }
    }

    if(!ValidateKeyLength(keyType, keyLength, "primary key") ||
       !ValidateKeyLength(subkeyType, subkeyLength, "subkey"))
    {
      return;
    }

    KeyTypeAndCapability primaryType = ((KeyTypeItem)keyType.SelectedItem).Value;
    NewKeyOptions options = new NewKeyOptions();
    options.Comment         = comment;
    options.Email           = email;
    options.KeyCapabilities = primaryType.Capabilities;
    options.KeyExpiration   = keyExpiration.Enabled ? keyExpiration.Value : (DateTime?)null;
    options.KeyLength       = GetSelectedKeyLength(keyLength);
    options.Keyring         = keyring;
    options.KeyType         = primaryType.Type;
    options.Password        = txtPass1.GetText();
    options.RealName        = realName;

    if(subkeyType.SelectedIndex == 0)
    {
      options.SubkeyType = KeyType.None;
    }
    else
    {
      KeyTypeAndCapability subType = ((KeyTypeItem)subkeyType.SelectedItem).Value;
      options.SubkeyCapabilities = subType.Capabilities;
      options.SubkeyExpiration   = subkeyExpiration.Enabled ? subkeyExpiration.Value : (DateTime?)null;
      options.SubkeyLength       = GetSelectedKeyLength(subkeyLength);
      options.SubkeyType         = subType.Type;
    }

    btnGenerate.Enabled = grpPrimary.Enabled = grpPassword.Enabled = grpSubkey.Enabled =
      grpUser.Enabled = false;
    btnCancel.Text = "&Cancel";

    progressBar.Style = ProgressBarStyle.Marquee;

    thread = new Thread(delegate()
    {
      try
      {
        pgp.CreateKey(options);
        Invoke((ThreadStart)delegate { GenerationFinished(); });
      }
      catch(ThreadAbortException) { }
      catch(Exception ex) { Invoke((ThreadStart)delegate { GenerationFailed(ex); }); }
    });

    thread.Start();
  }

  PGPSystem pgp;
  Keyring keyring;
  Thread thread;

  static int GetSelectedKeyLength(ComboBox keyLength)
  {
    if(keyLength.Items.Count == 0) return 0;
    else if(keyLength.SelectedIndex == -1) return int.Parse(keyLength.Text);
    else return ((ListItem<int>)keyLength.SelectedItem).Value;
  }
}

} // namespace AdamMil.Security.UI