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

    System.Security.SecureString password;
    unsafe
    {
      char* pass = stackalloc char[4];
      pass[0] = 'a';
      pass[1] = 'o';
      pass[2] = 'e';
      pass[3] = 'u';
      password = new System.Security.SecureString(pass, 4);
      password.MakeReadOnly();
    }

    gpg.KeyPasswordNeeded += delegate(string keyId, string userIdHint) { return password.Copy(); };
    gpg.CipherPasswordNeeded += delegate() { return password.Copy(); };

    // test getting random data
    byte[] random = new byte[100];
    gpg.GetRandomData(Randomness.Strong, random, 0, random.Length);

    // test hashing
    byte[] hash = gpg.Hash(new MemoryStream(Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog")),
                           HashAlgorithm.SHA1);
    CollectionAssert.AreEqual(hash, new byte[] { 
      0x2f, 0xd4, 0xe1, 0xc6, 0x7a, 0x2d, 0x28, 0xfc, 0xed, 0x84,
      0x9e, 0xe1, 0xbb, 0x76, 0xe7, 0x39, 0x1b, 0x93, 0xeb, 0x12 });

    MemoryStream plaintext, ciphertext, signature, decrypted;
    Signature[] sigs;

    PrimaryKey[] keys = gpg.GetPublicKeys();
    plaintext = new MemoryStream(Encoding.UTF8.GetBytes("Hello, world!"));

    signature = new MemoryStream();
    gpg.Sign(plaintext, signature, new SigningOptions(true, keys[4]), new OutputOptions(OutputFormat.ASCII, "Woot"));

    plaintext.Position = 0;
    signature.Position = 0;
    sigs = gpg.Verify(signature, plaintext, null);

    ciphertext = new MemoryStream();
    plaintext.Position = 0;
    gpg.Encrypt(plaintext, ciphertext, new EncryptionOptions(keys[1]), null);

    ciphertext = new MemoryStream();
    plaintext.Position = 0;
    gpg.Encrypt(plaintext, ciphertext, new EncryptionOptions(password), null);

    decrypted = new MemoryStream();
    ciphertext.Position = 0;
    gpg.Decrypt(ciphertext, decrypted, new DecryptionOptions(password));

    ciphertext = new MemoryStream();
    plaintext.Position = 0;
    gpg.SignAndEncrypt(plaintext, ciphertext, new SigningOptions(keys[4]), new EncryptionOptions(keys[5]), null);

    decrypted = new MemoryStream();
    ciphertext.Position = 0;
    sigs = gpg.Decrypt(ciphertext, decrypted, null);
  }
}

} // namespace AdamMil.Security.Tests