using System;
using System.Collections.Generic;
using System.Security;
using System.Windows.Forms;
using AdamMil.Security.PGP;
using AdamMil.Security.PGP.GPG;
using AdamMil.Security.UI;

namespace Sample
{
  public partial class MainForm : Form
  {
    public MainForm()
    {
      InitializeComponent();

      ExeGPG gpg = new ExeGPG("d:/adammil/programs/gnupg/gpg.exe");
      gpg.LineLogged += delegate(string line) { System.Diagnostics.Debugger.Log(0, "GPG", line+"\n"); };
      gpg.DecryptionPasswordNeeded += GetDecryptionPassword;
      gpg.KeyPasswordNeeded += GetKeyPassword;
      gpg.KeyPasswordInvalid += PasswordInvalid;

      primaryKeyList1.PGP = gpg;
      this.primaryKeyList1.ShowKeyring(null);
    }

    static SecureString GetDecryptionPassword()
    {
      PasswordForm form = new PasswordForm();
      form.DescriptionText = "This data is encrypted with a password. Enter the password to decrypt the data.";
      return form.ShowDialog() == DialogResult.OK ? form.GetPassword() : null;
    }

    static SecureString GetKeyPassword(string keyId, string userId)
    {
      PasswordForm form = new PasswordForm();
      form.DescriptionText = "A password is needed to unlock the secret key for " + userId;
      return form.ShowDialog() == DialogResult.OK ? form.GetPassword() : null;
    }

    static void PasswordInvalid(string keyId)
    {
      MessageBox.Show("Incorrect password.", "Close sesame!", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    void btnDownload_Click(object sender, EventArgs e)
    {
      KeyServerSearchForm form = new KeyServerSearchForm(primaryKeyList1.PGP);
      form.Show();
    }

    private void button1_Click(object sender, EventArgs e)
    {
      GenerateKeyForm form = new GenerateKeyForm(primaryKeyList1.PGP);
      form.ShowDialog();
    }
  }
}
