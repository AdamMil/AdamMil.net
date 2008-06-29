using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using AdamMil.Security.PGP;
using AdamMil.Security.PGP.GPG;

namespace AdamMil.Security.Tests
{
  [TestFixture]
  public class GPGTest
  {
    [Test]
    public void Test()
    {
      ExeGPG gpg = new ExeGPG("d:/adammil/programs/gnupg/gpg.exe");

      gpg.PasswordNeeded += delegate(string keyId, string hint)
      {
        unsafe
        {
          char* pass = stackalloc char[4];
          pass[0] = 'a';
          pass[1] = 'o';
          pass[2] = 'e';
          pass[3] = 'u';
          return new System.Security.SecureString(pass, 4);
        }
      };

      PrimaryKey[] keys = gpg.GetPublicKeys(KeySignatures.Ignore);

      MemoryStream plaintext = new MemoryStream(Encoding.UTF8.GetBytes("Hello, world!"));

      MemoryStream sig = new MemoryStream();
      SigningOptions options = new SigningOptions(keys[4]);
      options.Detached = true;
      gpg.Sign(plaintext, sig, options, new OutputOptions(OutputFormat.ASCII, "Woot"));

      plaintext.Position = 0;
      sig.Position = 0;
      Signature[] sigs = gpg.Verify(sig, plaintext, null);

      MemoryStream ciphertext = new MemoryStream();
      plaintext.Position = 0;
      gpg.Encrypt(plaintext, ciphertext, new EncryptionOptions(keys[1]), null);

      ciphertext = new MemoryStream();
      plaintext.Position = 0;
      gpg.SignAndEncrypt(plaintext, ciphertext, new SigningOptions(keys[4]), new EncryptionOptions(keys[1]), null);

      ciphertext.Position = 0;
      sigs = gpg.Verify(ciphertext, null);
    }
  }

} // namespace AdamMil.IO.Tests