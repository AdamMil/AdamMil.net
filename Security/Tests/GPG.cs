using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using AdamMil.Security.PGP;
using AdamMil.Security.PGP.GPG;
using AdamMil.Tests;

namespace AdamMil.Security.Tests
{

[TestFixture]
public class GPGTest : IDisposable
{
  ~GPGTest() { Dispose(true); }

  [TestFixtureTearDown]
  public void Dispose()
  {
    GC.SuppressFinalize(this);
    Dispose(false);
  }

  void Dispose(bool finalizing)
  {
    if(keyring != null)
    {
      File.Delete(keyring.PublicFile);
      File.Delete(keyring.SecretFile);
    }
  }

  const int Encrypter=0, Signer=1, Receiver=2; // keys that were imported

  [TestFixtureSetUp]
  public void Setup()
  {
    // we'll use "aoeu" as the password in places where passwords are needed
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

    gpg = new ExeGPG("d:/adammil/programs/gnupg/gpg.exe");
    gpg.KeyPasswordNeeded += delegate(string keyId, string userIdHint) { return password.Copy(); };
    gpg.CipherPasswordNeeded += delegate() { return password.Copy(); };

    keyring = new Keyring(Path.GetTempFileName(), Path.GetTempFileName());
  }

  [Test]
  public void T01_ImportTestKeys()
  {
    // import some keys for testing
    MemoryStream keyData = new MemoryStream(Encoding.ASCII.GetBytes(@"
-----BEGIN PGP PRIVATE KEY BLOCK-----
Version: GnuPG v1.4.9 (MingW32)

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
H7EsOJ/JYySUpqz5AsaDd4LWqA==
=UXCB
-----END PGP PRIVATE KEY BLOCK-----
"));
      
    ImportedKey[] results = gpg.ImportKeys(keyData, keyring, ImportOptions.ImportLocalSignatures);
    Assert.AreEqual(3, results.Length);
    foreach(ImportedKey key in results) Assert.IsTrue(key.Successful);
  }

  [Test]
  public void T02_TestHashing()
  {
    byte[] hash = gpg.Hash(new MemoryStream(Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog")),
                           HashAlgorithm.SHA1);
    CollectionAssert.AreEqual(new byte[] { 
      0x2f, 0xd4, 0xe1, 0xc6, 0x7a, 0x2d, 0x28, 0xfc, 0xed, 0x84,
      0x9e, 0xe1, 0xbb, 0x76, 0xe7, 0x39, 0x1b, 0x93, 0xeb, 0x12 }, hash);
  }

  [Test]
  public void T03_TestRandomData()
  {
    byte[] random = new byte[100];
    gpg.GetRandomData(Randomness.Strong, random, 0, random.Length);
  }

  [Test]
  public void T04_TestSigning()
  {
    MemoryStream plaintext = new MemoryStream(Encoding.UTF8.GetBytes("Hello, world!")), signature = new MemoryStream();
    PrimaryKey[] keys = gpg.GetPublicKeys(keyring);

    // we'll use only the test keyring
    VerificationOptions options = new VerificationOptions();
    options.AdditionalKeyrings.Add(keyring);
    options.IgnoreDefaultKeyring = true;

    // test ASCII embedded signatures
    gpg.Sign(plaintext, signature, new SigningOptions(keys[Signer], keys[Encrypter]),
             new OutputOptions(OutputFormat.ASCII, "Woot"));

    // verify the signature
    signature.Position = 0;
    CheckSignatures(keys, gpg.Verify(signature, options));

    // check that it contains the comment
    signature.Position = 0;
    string asciiSig = new StreamReader(signature).ReadToEnd();
    Assert.IsTrue(asciiSig.Contains("Comment: Woot"));

    // test binary detached signatures
    plaintext.Position = 0;
    signature = new MemoryStream();
    gpg.Sign(plaintext, signature, new SigningOptions(true, keys[Signer], keys[Encrypter]), null);

    // verify the signature
    plaintext.Position = signature.Position = 0;
    CheckSignatures(keys, gpg.Verify(signature, plaintext, options));
  }

  [Test]
  public void T05_TestEncryption()
  {
    PrimaryKey[] keys = gpg.GetPublicKeys(keyring);

    const string PlainTextString = "Hello, world!";
    MemoryStream plaintext  = new MemoryStream(Encoding.UTF8.GetBytes(PlainTextString));
    MemoryStream ciphertext = new MemoryStream(), decrypted = new MemoryStream();

    // use only the test keyring
    DecryptionOptions decryptOptions = new DecryptionOptions();
    decryptOptions.AdditionalKeyrings.Add(keyring);
    decryptOptions.IgnoreDefaultKeyring = true;

    // test ASCII key-based encryption
    EncryptionOptions encryptOptions = new EncryptionOptions(keys[Encrypter], keys[Receiver]);
    encryptOptions.AlwaysTrustRecipients = true; // TODO: remove this when we implement key editing
    gpg.Encrypt(plaintext, ciphertext, encryptOptions, new OutputOptions(OutputFormat.ASCII, "Woot"));

    // verify that it decrypts properly
    ciphertext.Position = 0;
    gpg.Decrypt(ciphertext, decrypted, decryptOptions);
    decrypted.Position = 0;
    Assert.AreEqual(PlainTextString, new StreamReader(decrypted).ReadToEnd());

    // verify that it contains the comment
    ciphertext.Position = 0;
    string asciiCiphertext = new StreamReader(ciphertext).ReadToEnd();
    Assert.IsTrue(asciiCiphertext.Contains("Comment: Woot"));

    // test binary password-based encryption
    ciphertext = new MemoryStream();
    plaintext.Position = 0;
    gpg.Encrypt(plaintext, ciphertext, new EncryptionOptions(password));

    // verify that it decrypts properly
    decrypted = new MemoryStream();
    ciphertext.Position = 0;
    gpg.Decrypt(ciphertext, decrypted, new DecryptionOptions(password));
    decrypted.Position = 0;
    Assert.AreEqual(PlainTextString, new StreamReader(decrypted).ReadToEnd());

    // test signed, encrypted data
    encryptOptions = new EncryptionOptions(keys[Encrypter]);
    encryptOptions.AlwaysTrustRecipients = true; // TODO: remove this when we implement key editing
    ciphertext = new MemoryStream();
    plaintext.Position = 0;
    gpg.SignAndEncrypt(plaintext, ciphertext, new SigningOptions(keys[Signer], keys[Encrypter]), encryptOptions);

    // verify the signatures
    decrypted = new MemoryStream();
    ciphertext.Position = 0;
    CheckSignatures(keys, gpg.Decrypt(ciphertext, decrypted, decryptOptions));

    // verify that it decrypted properly
    decrypted.Position = 0;
    Assert.AreEqual(PlainTextString, new StreamReader(decrypted).ReadToEnd());
  }

  [Test]
  public void T06_TestExport()
  {
    PrimaryKey[] keys = gpg.GetPublicKeys(keyring);
    
    // test ascii armored public keys
    MemoryStream output = new MemoryStream();
    gpg.ExportPublicKey(keys[Encrypter], output, ExportOptions.Default,
                        new OutputOptions(OutputFormat.ASCII, "Woot"));

    // verify that it contains the comment
    output.Position = 0;
    Assert.IsTrue(new StreamReader(output).ReadToEnd().Contains("Comment: Woot"));

    // verify that it imports
    output.Position = 0;
    ImportedKey[] imported = gpg.ImportKeys(output, keyring);
    Assert.AreEqual(1, imported.Length);
    Assert.IsTrue(imported[0].Successful);
    Assert.AreEqual(imported[0].Fingerprint, keys[Encrypter].Fingerprint);

    // test binary secret keys
    output = new MemoryStream();
    gpg.ExportSecretKey(keys[Encrypter], output);

    // verify that it doesn't import (because the secret key already exists)
    output.Position = 0;
    TestHelpers.TestException<ImportFailedException>(delegate { gpg.ImportKeys(output, keyring); });

    // then delete the existing key and verify that it does import
    gpg.DeleteKey(keys[Encrypter], KeyDeletion.PublicAndSecret);
    Assert.AreEqual(2, gpg.GetPublicKeys(keyring).Length);
    output.Position = 0;
    imported = gpg.ImportKeys(output, keyring);
    Assert.AreEqual(1, imported.Length);
    Assert.IsTrue(imported[0].Successful);
    Assert.AreEqual(imported[0].Fingerprint, keys[Encrypter].Fingerprint);

    // then delete all of the keys and reimport them to restore things to the way they were
    keys = gpg.GetPublicKeys(keyring);
    Assert.AreEqual(3, keys.Length);
    gpg.DeleteKeys(keys, KeyDeletion.PublicAndSecret);
    Assert.AreEqual(0, gpg.GetPublicKeys(keyring).Length);
    T01_ImportTestKeys();
  }

  void CheckSignatures(PrimaryKey[] keys, Signature[] sigs)
  {
    Assert.AreEqual(sigs.Length, 2);
    Assert.IsTrue(sigs[0].IsValid);
    Assert.IsTrue(sigs[1].IsValid);
    Assert.AreEqual(keys[Signer].Fingerprint, sigs[0].KeyFingerprint);
    Assert.AreEqual(keys[Encrypter].Fingerprint, sigs[1].KeyFingerprint);
  }

  ExeGPG gpg;
  System.Security.SecureString password;
  Keyring keyring;
}

} // namespace AdamMil.Security.Tests