using System;
using System.ComponentModel;
using System.Windows.Forms;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

/// <summary>This form helps the user create a new subkey. The form does not actually create the subkey, but merely
/// gathers the information needed to do so. The form is meant to be used as a modal dialog.
/// </summary>
public partial class NewSubkeyForm : Form
{
  /// <summary>Creates a new <see cref="NewSubkeyForm"/>. You will need to call <see cref="Initialize"/> to initialize
  /// the form before displaying it.
  /// </summary>
  public NewSubkeyForm()
  {
    InitializeComponent();
  }

  /// <summary>Initializes a new <see cref="NewSubkeyForm"/> with the <see cref="PGPSystem"/> that will be used to
  /// create the subkey.
  /// </summary>
  public NewSubkeyForm(PGPSystem pgp) : this()
  {
    Initialize(pgp);
  }

  /// <summary>Gets the <see cref="KeyCapabilities"/> selected by the user.</summary>
  [Browsable(false)]
  public KeyCapabilities Capabilities
  {
    get { return ((KeyTypeItem)keyType.SelectedItem).Value.Capabilities; }
  }

  /// <summary>Gets the key expiration date selected by the user, or null if the key does not expire.</summary>
  [Browsable(false)]
  public DateTime? Expiration
  {
    get { return keyExpiration.Enabled ? keyExpiration.Value : (DateTime?)null; }
  }

  /// <summary>Gets the key length selected by the user.</summary>
  [Browsable(false)]
  public int KeyLength
  {
    get
    {
      return keyLength.SelectedIndex == -1 ?
        int.Parse(keyLength.Text) : ((ListItem<int>)keyLength.SelectedItem).Value;
    }
  }

  /// <summary>Gets the key type selected by the user.</summary>
  [Browsable(false)]
  public string KeyType
  {
    get { return ((KeyTypeItem)keyType.SelectedItem).Value.Type; }
  }

  /// <summary>Initializes a new <see cref="NewSubkeyForm"/> with the <see cref="PGPSystem"/> that will be used to
  /// create the subkey.
  /// </summary>
  public void Initialize(PGPSystem pgp)
  {
    if(pgp == null) throw new ArgumentNullException();
    this.pgp = pgp;

    KeyCapabilities signOnly = KeyCapabilities.Sign;
    KeyCapabilities encryptOnly = KeyCapabilities.Encrypt;
    KeyCapabilities signAndEncrypt = signOnly | encryptOnly;

    bool supportsDSA = false, supportsELG = false, supportsRSA = false;
    foreach(string type in pgp.GetSupportedKeyTypes())
    {
      if(!supportsDSA && string.Equals(type, PGP.KeyType.DSA, StringComparison.OrdinalIgnoreCase))
      {
        supportsDSA = true;
      }
      else if(!supportsELG && string.Equals(type, PGP.KeyType.ElGamal, StringComparison.OrdinalIgnoreCase))
      {
        supportsELG = true;
      }
      else if(!supportsRSA && string.Equals(type, PGP.KeyType.RSA, StringComparison.OrdinalIgnoreCase))
      {
        supportsRSA = true;
      }
    }

    keyType.Items.Clear();
    if(supportsELG) keyType.Items.Add(new KeyTypeItem(PGP.KeyType.ElGamal, encryptOnly, "El Gamal (encryption only)"));
    if(supportsRSA) keyType.Items.Add(new KeyTypeItem(PGP.KeyType.RSA, encryptOnly, "RSA (encryption only)"));
    if(supportsDSA) keyType.Items.Add(new KeyTypeItem(PGP.KeyType.DSA, signOnly, "DSA (signing only)"));
    if(supportsRSA)
    {
      keyType.Items.Add(new KeyTypeItem(PGP.KeyType.RSA, signOnly, "RSA (signing only)"));
      keyType.Items.Add(new KeyTypeItem(PGP.KeyType.RSA, signAndEncrypt, "RSA (sign and encrypt)"));
    }
    keyType.SelectedIndex = 0;

    // OpenPGP currently supports a maximum expiration date of February 25, 2174. we'll use the 24th to avoid
    // local <-> UTC conversion problems
    keyExpiration.MinDate = DateTime.Now.Date.AddDays(1);
    keyExpiration.MaxDate = new DateTime(2174, 2, 24, 0, 0, 0, DateTimeKind.Local);
    keyExpiration.Value = DateTime.UtcNow.Date.AddYears(5); // by default, the subkey expires in 5 years
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

  void btnOK_Click(object sender, EventArgs e)
  {
    if(keyLength.Enabled && keyLength.SelectedIndex == -1)
    {
      int customLength;
      if(!int.TryParse(keyLength.Text, out customLength) || customLength < 0)
      {
        MessageBox.Show(keyLength.Text + " is not a valid subkey size.", "Invalid key size",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }

      int maxLength = pgp.GetMaximumKeyLength(KeyType);
      if(customLength > maxLength)
      {
        MessageBox.Show("The subkey size is greater than the maximum supported size of " +
                        maxLength.ToString() + " bits.", "Key size too large", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
        return;
      }
    }

    DialogResult = DialogResult.OK;
  }

  void keyType_SelectedIndexChanged(object sender, EventArgs e)
  {
    // get the selected key length so we can restore it later
    int selectedKeyLength;
    if(keyLength.Items.Count == 0) selectedKeyLength = 0;
    else if(keyLength.SelectedIndex == -1) int.TryParse(keyLength.Text, out selectedKeyLength);
    else selectedKeyLength = ((ListItem<int>)keyLength.SelectedItem).Value;

    string keyType = KeyType;
    if(keyType == PGP.KeyType.Default) keyType = pgp.GetDefaultSubkeyType(); // if it's the default key type, get the real one

    keyLength.Items.Clear();

    // always add the default key length
    keyLength.Items.Add(new ListItem<int>(0, "Default"));
    keyLength.SelectedIndex = 0;

    // then add key lengths in increments of 1024 bits, from the minimum to the maximum length
    int maxKeyLength = pgp.GetMaximumKeyLength(keyType);
    for(int i=1, length=1024; length <= maxKeyLength; i++, length += 1024)
    {
      keyLength.Items.Add(new ListItem<int>(length, length.ToString()));
      if(length == selectedKeyLength) keyLength.SelectedIndex = i; // try to restore the selected key length
    }

    // if the selected key length is greater than the new maximum, select the new maximum
    if(selectedKeyLength > maxKeyLength) keyLength.SelectedIndex = keyLength.Items.Count-1;
  }

  void chkNoExpiration_CheckedChanged(object sender, EventArgs e)
  {
    keyExpiration.Enabled = !chkNoExpiration.Checked;
  }

  PGPSystem pgp;
}

} // namespace AdamMil.Security.UI