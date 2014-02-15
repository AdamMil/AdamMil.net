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
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

/// <summary>This form helps the user generate a new key pair. It can be used as a modal dialog or a free-standing
/// window.
/// </summary>
public partial class GenerateKeyForm : Form
{
  /// <summary>Creates a new <see cref="GenerateKeyForm"/>. You should later call <see cref="Initialize"/> to
  /// initialize the form.
  /// </summary>
  public GenerateKeyForm()
  {
    InitializeComponent();
  }

  /// <summary>Initializes a new <see cref="GenerateKeyForm"/> with the <see cref="PGPSystem"/> that will be used to
  /// generate the keys. The keys will be created in the default keyring.
  /// </summary>
  public GenerateKeyForm(PGPSystem pgp) : this(pgp, null) { }

  /// <summary>Initializes a new <see cref="GenerateKeyForm"/> with the <see cref="PGPSystem"/> that will be used to
  /// generate the keys. The keys will be created in the given keyring, or the default keyring if it is null.
  /// </summary>
  public GenerateKeyForm(PGPSystem pgp, Keyring keyring) : this()
  {
    Initialize(pgp, keyring);
  }

  /// <summary>Raised when a key has been successfully generated.</summary>
  public event KeyEventHandler KeyGenerated;

  /// <summary>Gets whether key generation is currently in progress.</summary>
  [Browsable(false)]
  public bool InProgress
  {
    get { return thread != null; }
  }

  /// <summary>Cancels the key generation if it's currently in progress.</summary>
  public void CancelKeyGeneration()
  {
    Thread thread = this.thread; // grab a local copy so it doesn't get pulled out from under us
    if(thread != null)
    {
      try { thread.Abort(); }
      catch { }
      GenerationFinished();
    }
  }

  /// <summary>Initializes the form with the <see cref="PGPSystem"/> that will be used to generate the keys. The keys
  /// will be created in the given keyring, or the default keyring if it is null.
  /// </summary>
  public void Initialize(PGPSystem pgp, Keyring keyring)
  {
    if(pgp == null) throw new ArgumentNullException();
    this.pgp = pgp;
    this.keyring = keyring;

    UpdatePasswordStrength();

    KeyCapabilities signOnly = KeyCapabilities.Sign;
    KeyCapabilities encryptOnly = KeyCapabilities.Encrypt;
    KeyCapabilities signAndEncrypt = signOnly | encryptOnly;

    bool supportsDSA = false, supportsELG = false, supportsRSA = false;
    foreach(string type in pgp.GetSupportedKeyTypes())
    {
      if(!supportsDSA && string.Equals(type, KeyType.DSA, StringComparison.OrdinalIgnoreCase))
      {
        supportsDSA = true;
      }
      else if(!supportsELG && string.Equals(type, KeyType.ElGamal, StringComparison.OrdinalIgnoreCase))
      {
        supportsELG = true;
      }
      else if(!supportsRSA && string.Equals(type, KeyType.RSA, StringComparison.OrdinalIgnoreCase))
      {
        supportsRSA = true;
      }
    }

    keyType.Items.Clear();
    keyType.Items.Add(new KeyTypeItem(KeyType.Default, signOnly | KeyCapabilities.Certify, "Default (signing only)"));
    if(supportsDSA)
    {
      keyType.Items.Add(new KeyTypeItem(KeyType.DSA, signOnly | KeyCapabilities.Certify, "DSA (signing only)"));
    }
    if(supportsRSA)
    {
      keyType.Items.Add(new KeyTypeItem(KeyType.RSA, signOnly | KeyCapabilities.Certify, "RSA (signing only)"));
      keyType.Items.Add(new KeyTypeItem(KeyType.RSA, signAndEncrypt | KeyCapabilities.Certify, "RSA (sign and encrypt)"));
    }
    keyType.SelectedIndex = 0;

    subkeyType.Items.Clear();
    subkeyType.Items.Add(new KeyTypeItem(KeyType.None, 0, "None"));
    subkeyType.Items.Add(new KeyTypeItem(KeyType.Default, encryptOnly, "Default (encryption only)"));
    if(supportsELG) subkeyType.Items.Add(new KeyTypeItem(KeyType.ElGamal, encryptOnly, "El Gamal (encryption only)"));
    if(supportsRSA) subkeyType.Items.Add(new KeyTypeItem(KeyType.RSA, encryptOnly, "RSA (encryption only)"));
    if(supportsDSA) subkeyType.Items.Add(new KeyTypeItem(KeyType.DSA, signOnly, "DSA (signing only)"));
    if(supportsRSA)
    {
      subkeyType.Items.Add(new KeyTypeItem(KeyType.RSA, signOnly, "RSA (signing only)"));
      subkeyType.Items.Add(new KeyTypeItem(KeyType.RSA, signAndEncrypt, "RSA (sign and encrypt)"));
    }
    subkeyType.SelectedIndex = 1;

    // OpenPGP currently supports a maximum expiration date of February 25, 2174. we'll use the 24th to avoid
    // local <-> UTC conversion problems
    keyExpiration.MinDate  = subkeyExpiration.MinDate = DateTime.Now.Date.AddDays(1);
    keyExpiration.MaxDate  = subkeyExpiration.MaxDate = new DateTime(2174, 2, 24, 0, 0, 0, DateTimeKind.Local);
    subkeyExpiration.Value = DateTime.UtcNow.Date.AddYears(5); // by default, the subkey expires in 5 years
  }

  /// <include file="documentation.xml" path="/UI/Common/OnClosing/node()"/>
  protected override void OnClosing(CancelEventArgs e)
  {
    base.OnClosing(e);

    if(!e.Cancel && InProgress)
    {
      if(MessageBox.Show("Key generation is currently in progress. Cancel it?", "Cancel key generation?",
                       MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) ==
           DialogResult.Yes)
      {
        CancelKeyGeneration();
      }
      else // don't close the form if the user wants to cancel the key generation
      {
        e.Cancel = true;
      }
    }
  }

  /// <summary>Stores the key type and key capability flags associated with a list box item.</summary>
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
    PGPUI.ShowErrorDialog("generating the key", ex);
  }

  void GenerationFinished()
  {
    thread = null;

    progressBar.Style = ProgressBarStyle.Blocks;

    btnGenerate.Enabled = grpPrimary.Enabled = grpPassword.Enabled = grpSubkey.Enabled =
      grpUser.Enabled = true;

    btnCancel.Text = "&Close";
  }

  void GenerationSucceeded(PrimaryKey newKey)
  {
    GenerationFinished();
    if(KeyGenerated != null) KeyGenerated(this, newKey);
    MessageBox.Show("Key generation successful.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
  }

  void UpdateKeyLengths(ComboBox keyTypes, ComboBox keyLengths)
  {
    // get the selected key length so we can restore it later
    int selectedKeyLength;
    if(keyLengths.Items.Count == 0) selectedKeyLength = 0;
    else if(keyLengths.SelectedIndex == -1) int.TryParse(keyLengths.Text, out selectedKeyLength);
    else selectedKeyLength = ((ListItem<int>)keyLengths.SelectedItem).Value;

    keyLengths.Items.Clear();

    string keyType = ((KeyTypeItem)keyTypes.SelectedItem).Value.Type;
    if(string.Equals(keyType, KeyType.None, StringComparison.OrdinalIgnoreCase)) // if the "None" key type is selected,
    {                                                                            // then disable the key lengths box
      keyLengths.Enabled = false;
    }
    else // otherwise, enable it and add suggested key lengths
    {
      if(keyType == KeyType.Default) // if it's the default key type, get the real one
      {
        keyType = keyTypes == this.keyType ? pgp.GetDefaultPrimaryKeyType() : pgp.GetDefaultSubkeyType();
      }

      keyLengths.Enabled = true;

      // always add the default key length
      keyLengths.Items.Add(new ListItem<int>(0, "Default"));
      keyLengths.SelectedIndex = 0;

      // then add key lengths in increments of 1024 bits, from the minimum to the maximum length
      int maxKeyLength = pgp.GetMaximumKeyLength(keyType);
      for(int i=1,length=1024; length <= maxKeyLength; i++,length += 1024)
      {
        keyLengths.Items.Add(new ListItem<int>(length, length.ToString()));
        if(length == selectedKeyLength) keyLengths.SelectedIndex = i; // try to restore the selected key length
      }

      // if the selected key length is greater than the new maximum, select the new maximum
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
    if(keyLength.Enabled && keyLength.SelectedIndex == -1)
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
      btnGenerate.Enabled = false; // disallow key generation if no user ID is entered
    }
    else
    {
      lblUserId.Text = "Your user ID will be displayed as:\n" + realName +
                       (!string.IsNullOrEmpty(comment) ? " ("+comment+")" : null) +
                       (!string.IsNullOrEmpty(email) ? " <"+email+">" : null);
      btnGenerate.Enabled = true; // allow key generation only if a user ID is entered
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
    // if "None" was selected, disable the other subkey controls
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
        CancelKeyGeneration();
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

    if(!PGPUI.ValidateUserId(realName, email, comment) ||
       !PGPUI.ValidateAndCheckPasswords(txtPass1, txtPass2) ||
       !ValidateKeyLength(keyType, keyLength, "primary key") ||
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

    // disable the UI controls while key generation is ongoing
    btnGenerate.Enabled = grpPrimary.Enabled = grpPassword.Enabled = grpSubkey.Enabled =
      grpUser.Enabled = false;
    btnCancel.Text = "&Cancel"; // turn the "Close" button into a "Cancel" button

    progressBar.Style = ProgressBarStyle.Marquee; // start up the "progress" bar

    // and start generating the key
    thread = new Thread(delegate()
    {
      try
      {
        PrimaryKey newKey = pgp.CreateKey(options);
        Invoke((ThreadStart)delegate { GenerationSucceeded(newKey); });
      }
      catch(ThreadAbortException) { }
      catch(Exception ex) { Invoke((ThreadStart)delegate { GenerationFailed(ex); }); }
    });

    thread.Start();
  }

  PGPSystem pgp;
  Keyring keyring;
  Thread thread;

  /// <summary>Gets the selected key length, assuming it has already been validated.</summary>
  static int GetSelectedKeyLength(ComboBox keyLength)
  {
    if(!keyLength.Enabled || keyLength.Items.Count == 0) return 0;
    else if(keyLength.SelectedIndex == -1) return int.Parse(keyLength.Text);
    else return ((ListItem<int>)keyLength.SelectedItem).Value;
  }
}

} // namespace AdamMil.Security.UI