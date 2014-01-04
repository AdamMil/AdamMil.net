using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using NUnit.Framework;
using AdamMil.Security.PGP;
using AdamMil.Security.PGP.GPG;
using AdamMil.Tests;

namespace AdamMil.Security.Tests
{

public abstract class GPGTestBase : IDisposable
{
  protected GPGTestBase(string gpgPath)
  {
    this.gpgPath = gpgPath;
  }

  ~GPGTestBase() { Dispose(false); }

  [TestFixtureTearDown]
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  void Dispose(bool manualDispose)
  {
    if(keyring != null)
    {
      File.Delete(keyring.PublicFile);
      File.Delete(keyring.SecretFile);
      File.Delete(keyring.TrustDbFile);
    }
  }

  const int Encrypter=0, Signer=1, Receiver=2; // keys that were imported

  [TestFixtureSetUp]
  public void Setup()
  {
    // we'll use "aoeu" as the password in places where passwords are needed
    // unfortunately, GPG2 insists on using the gpg-agent, which pops up and asks questions during the test
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

    gpg = new ExeGPG(gpgPath);
    gpg.LineLogged += delegate(string line) { System.Diagnostics.Debugger.Log(0, "GPG", line+"\n"); };
    gpg.KeyPasswordNeeded += delegate(string keyId, string userIdHint) { return password.Copy(); };
    gpg.DecryptionPasswordNeeded += delegate() { return password.Copy(); };
    gpg.RetrieveKeySignatureFingerprints = true;

    keyring = new Keyring(Path.GetTempFileName(), Path.GetTempFileName(), Path.GetTempFileName());
    keyring.Create(gpg, true);
  }

  [Test]
  public void T01_ImportTestKeys()
  {
    // import some keys for testing
    MemoryStream secretKeys = new MemoryStream(Encoding.ASCII.GetBytes(@"
-----BEGIN PGP PRIVATE KEY BLOCK-----

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
=UXCB
-----END PGP PRIVATE KEY BLOCK-----
"));

    MemoryStream publicKeys = new MemoryStream(Encoding.ASCII.GetBytes(@"
-----BEGIN PGP PUBLIC KEY BLOCK-----

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
=gHB+
-----END PGP PUBLIC KEY BLOCK-----
"));

    // import the secret keys
    ImportedKey[] results = gpg.ImportKeys(secretKeys, keyring, ImportOptions.ImportLocalSignatures);
    Assert.AreEqual(3, results.Length);
    foreach(ImportedKey key in results) Assert.IsTrue(key.Successful);

    // import the public keys, including the photograph(s)
    results = gpg.ImportKeys(publicKeys, keyring, ImportOptions.ImportLocalSignatures);
    Assert.AreEqual(3, results.Length);
    foreach(ImportedKey key in results) Assert.IsTrue(key.Successful);

    // read the keys (which also indicates that everything was imported)
    keys = gpg.GetKeys(keyring);

    // set the trust level of the encryption key to Ultimate
    gpg.SetOwnerTrust(TrustLevel.Ultimate, keys[Encrypter]);
  }

  [Test]
  public void T02_Hashing()
  {
    byte[] hash = gpg.Hash(new MemoryStream(Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog")),
                           HashAlgorithm.SHA1);
    CollectionAssert.AreEqual(new byte[] {
      0x2f, 0xd4, 0xe1, 0xc6, 0x7a, 0x2d, 0x28, 0xfc, 0xed, 0x84,
      0x9e, 0xe1, 0xbb, 0x76, 0xe7, 0x39, 0x1b, 0x93, 0xeb, 0x12 }, hash);
  }

  [Test]
  public void T03_RandomData()
  {
    byte[] random = new byte[100];
    gpg.GetRandomData(Randomness.Weak, random, 0, random.Length);
  }

  [Test]
  public void T04_Signing()
  {
    const string PlainString = "Hello, world!";

    EnsureImported();

    DecryptionOptions decryptOptions = new DecryptionOptions();
    decryptOptions.AdditionalKeyrings.Add(keyring);
    decryptOptions.IgnoreDefaultKeyring = true;

    MemoryStream plaintext = new MemoryStream(Encoding.UTF8.GetBytes(PlainString)), signature = new MemoryStream();

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

    // verify the embedded text
    MemoryStream output = new MemoryStream();
    signature.Position = 0;
    CheckSignatures(keys, gpg.Decrypt(signature, output, decryptOptions));
    output.Position = 0;
    Assert.IsTrue(new StreamReader(output).ReadToEnd().Contains(PlainString));

    // check that it contains the comment
    signature.Position = 0;
    Assert.IsTrue(new StreamReader(signature).ReadToEnd().Contains("Comment: Woot"));

    // test clearsigning
    plaintext.Position = 0;
    signature = new MemoryStream();
    gpg.Sign(plaintext, signature, new SigningOptions(SignatureType.ClearSignedText, keys[Signer], keys[Encrypter]));

    // verify that the original text is there in the clear
    signature.Position = 0;
    Assert.IsTrue(new StreamReader(signature).ReadToEnd().Contains(PlainString));

    // verify the signature
    output = new MemoryStream();
    signature.Position = 0;
    CheckSignatures(keys, gpg.Decrypt(signature, output, decryptOptions));
    output.Position = 0;
    Assert.IsTrue(new StreamReader(output).ReadToEnd().Contains(PlainString));

    // test binary detached signatures
    plaintext.Position = 0;
    signature = new MemoryStream();
    gpg.Sign(plaintext, signature, new SigningOptions(SignatureType.Detached, keys[Signer], keys[Encrypter]));

    // verify the signature
    plaintext.Position = signature.Position = 0;
    CheckSignatures(keys, gpg.Verify(plaintext, signature, options));
  }

  [Test]
  public void T05_Encryption()
  {
    EnsureImported();

    const string PlainTextString = "Hello, world!";
    MemoryStream plaintext  = new MemoryStream(Encoding.UTF8.GetBytes(PlainTextString));
    MemoryStream ciphertext = new MemoryStream(), decrypted = new MemoryStream();

    // use only the test keyring
    DecryptionOptions decryptOptions = new DecryptionOptions();
    decryptOptions.AdditionalKeyrings.Add(keyring);
    decryptOptions.IgnoreDefaultKeyring = true;

    // test ASCII key-based encryption
    EncryptionOptions encryptOptions = new EncryptionOptions(keys[Encrypter], keys[Receiver]);
    encryptOptions.AlwaysTrustRecipients = true;
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
    encryptOptions.AlwaysTrustRecipients = true;
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

    // test encryption of a large data source, to make sure GPG doesn't deadlock on the read
    byte[] buffer = new byte[70*1024];
    new Random().NextBytes(buffer);
    gpg.SignAndEncrypt(new MemoryStream(buffer), new MemoryStream(), null, new EncryptionOptions(password));
  }

  [Test]
  public void T06_Export()
  {
    EnsureImported();

    // test ascii armored public keys
    MemoryStream output = new MemoryStream();
    gpg.ExportKey(keys[Encrypter], output, ExportOptions.Default, new OutputOptions(OutputFormat.ASCII, "Woot"));

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
    gpg.ExportKey(keys[Encrypter], output, ExportOptions.ExportSecretKeys);

    // verify that it doesn't import (because the secret key already exists)
    output.Position = 0;
    TestHelpers.TestException<ImportFailedException>(delegate { gpg.ImportKeys(output, keyring); });

    // then delete the existing key and verify that it does import
    gpg.DeleteKey(keys[Encrypter], KeyDeletion.PublicAndSecret);
    Assert.AreEqual(2, gpg.GetKeys(keyring).Length);
    output.Position = 0;
    imported = gpg.ImportKeys(output, keyring);
    Assert.AreEqual(1, imported.Length);
    Assert.IsTrue(imported[0].Successful);
    Assert.AreEqual(imported[0].Fingerprint, keys[Encrypter].Fingerprint);

    // then delete all of the keys and reimport them to restore things to the way they were
    keys = gpg.GetKeys(keyring);
    Assert.AreEqual(3, keys.Length);
    gpg.DeleteKeys(keys, KeyDeletion.PublicAndSecret);
    Assert.AreEqual(0, gpg.GetKeys(keyring).Length);

    this.keys = null; // force the next test to import keys
  }

  [Test]
  public void T07_KeyCreation()
  {
    EnsureImported();
    NewKeyOptions options = new NewKeyOptions();
    options.KeyType          = KeyType.RSA;
    options.RealName         = "New Guy";
    options.Email            = "email@foo.com";
    options.Comment          = "Weird";
    options.KeyExpiration    = new DateTime(2090, 8, 1, 0, 0, 0, DateTimeKind.Utc);
    options.Password         = password;
    options.Keyring          = keyring;
    options.SubkeyType       = KeyType.RSA;
    options.SubkeyExpiration = new DateTime(2080, 4, 5, 0, 0, 0, DateTimeKind.Utc);

    // create and delete the key
    Assert.AreEqual(3, gpg.GetKeys(keyring).Length);
    PrimaryKey key = gpg.CreateKey(options);
    Assert.IsNotNull(key);
    Assert.AreEqual(KeyType.RSA, key.KeyType);
    Assert.AreEqual(1, key.UserIds.Count);
    Assert.AreEqual(1, key.Subkeys.Count);
    Assert.AreEqual("New Guy (Weird) <email@foo.com>", key.UserIds[0].Name);
    Assert.IsTrue(key.ExpirationTime.HasValue);
    Assert.AreEqual(options.KeyExpiration, key.ExpirationTime.Value.Date);
    Assert.AreEqual(options.SubkeyExpiration, key.Subkeys[0].ExpirationTime.Value.Date);
    Assert.AreEqual(KeyCapabilities.Authenticate | KeyCapabilities.Certify | KeyCapabilities.Sign, key.Capabilities);
    Assert.AreEqual(KeyCapabilities.Encrypt, key.Subkeys[0].Capabilities);
    Assert.AreEqual(4, gpg.GetKeys(keyring).Length);
    gpg.DeleteKey(key, KeyDeletion.PublicAndSecret);
  }

  [Test]
  public void T08_Attributes()
  {
    EnsureImported();

    PrimaryKey[] keys = gpg.GetKeys(keyring, ListOptions.RetrieveAttributes);

    Assert.AreEqual(1, keys[Encrypter].Attributes.Count);
    Assert.IsTrue(keys[Encrypter].Attributes[0] is UserImage);

    UserImage imageAttr = (UserImage)(keys[Encrypter].Attributes[0]);

    CollectionAssert.AreEqual(new byte[] { 0xed, 0xe6, 0x05, 0xf4, 0xff, 0x4b, 0x84, 0xca, 0xf7, 0x73,
                                           0xa2, 0x2b, 0x37, 0x19, 0xf1, 0x94, 0x71, 0x90, 0x7e, 0xd6 },
                              gpg.Hash(imageAttr.GetSubpacketStream(), HashAlgorithm.SHA1));

    using(System.Drawing.Bitmap bitmap = imageAttr.GetBitmap())
    {
      Assert.AreEqual(150, bitmap.Width);
      Assert.AreEqual(180, bitmap.Height);
    }
  }

  [Test]
  public void T09_EditUidAndSig()
  {
    EnsureImported();

    UserPreferences preferences = new UserPreferences();
    preferences.Keyserver = new Uri("hkp://keys.foo.com");
    preferences.PreferredCiphers.Add(OpenPGPCipher.AES256);
    preferences.PreferredCiphers.Add(OpenPGPCipher.AES);
    preferences.PreferredCiphers.Add(OpenPGPCipher.Twofish);
    preferences.PreferredCiphers.Add(OpenPGPCipher.CAST5);
    preferences.PreferredCompressions.Add(OpenPGPCompression.Bzip2);
    preferences.PreferredCompressions.Add(OpenPGPCompression.Zlib);
    preferences.PreferredCompressions.Add(OpenPGPCompression.Zip);
    preferences.PreferredHashes.Add(OpenPGPHashAlgorithm.SHA1);
    preferences.PreferredHashes.Add(OpenPGPHashAlgorithm.MD5);

    // test adding a user ID
    gpg.AddUserId(keys[Encrypter], "John", "john@gmail.com", "big man", preferences);

    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter]);
    Assert.AreEqual(2, keys[Encrypter].UserIds.Count);
    UserId newId = keys[Encrypter].UserIds[1];
    Assert.IsFalse(newId.Primary);
    Assert.AreEqual("John (big man) <john@gmail.com>", newId.Name);

    // test GetPreferences(), and ensure that AddUserId() added the preferences properly
    UserPreferences newPrefs = gpg.GetPreferences(newId);
    // TODO: Assert.AreEqual(preferences.Keyserver, newPrefs.Keyserver);
    Assert.AreEqual(preferences.Primary, newPrefs.Primary);
    CollectionAssert.AreEqual(preferences.PreferredCiphers, newPrefs.PreferredCiphers);
    CollectionAssert.AreEqual(preferences.PreferredCompressions, newPrefs.PreferredCompressions);
    CollectionAssert.AreEqual(preferences.PreferredHashes, newPrefs.PreferredHashes);

    // test SetPreferences()
    preferences.PreferredCiphers.Remove(OpenPGPCipher.Twofish);
    preferences.PreferredCompressions.Remove(OpenPGPCompression.Bzip2);
    gpg.SetPreferences(newId, preferences);
    newPrefs = gpg.GetPreferences(newId);
    Assert.AreEqual(preferences.Primary, newPrefs.Primary);
    CollectionAssert.AreEqual(preferences.PreferredCiphers, newPrefs.PreferredCiphers);
    CollectionAssert.AreEqual(preferences.PreferredCompressions, newPrefs.PreferredCompressions);
    CollectionAssert.AreEqual(preferences.PreferredHashes, newPrefs.PreferredHashes);

    // test key signing (sign Receiver's key with Encrypter's key)
    keys = gpg.GetKeys(keyring, ListOptions.RetrieveSignatures);
    Assert.AreEqual(keys[Receiver].UserIds[0].Signatures.Count, 1);
    gpg.SignKey(keys[Receiver], keys[Encrypter], null);
    keys[Receiver] = gpg.RefreshKey(keys[Receiver], ListOptions.RetrieveSignatures);
    Assert.AreEqual(2, keys[Receiver].UserIds[0].Signatures.Count);
    KeySignature keySig = keys[Receiver].UserIds[0].Signatures[1];
    Assert.IsFalse(keySig.Exportable);
    Assert.AreEqual(keys[Encrypter].KeyId, keySig.KeyId);
    Assert.AreEqual(keys[Encrypter].Fingerprint, keySig.KeyFingerprint);
    Assert.AreEqual(OpenPGPSignatureType.GenericCertification, keySig.Type);
    Assert.AreEqual(keys[Encrypter].PrimaryUserId.Name, keySig.SignerName);

    // test UID signing (sign Encrypter's new UID with Signer's key)
    KeySigningOptions ksOptions = new KeySigningOptions(CertificationLevel.Casual, true);
    ksOptions.Irrevocable = true;
    ksOptions.TrustDepth  = 2;
    ksOptions.TrustDomain = "Mu.*";
    ksOptions.TrustLevel  = TrustLevel.Marginal;
    gpg.SignAttribute(keys[Encrypter].UserIds[1], keys[Signer], ksOptions);
    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter], ListOptions.RetrieveSignatures);
    Assert.AreEqual(1, keys[Encrypter].UserIds[0].Signatures.Count);
    Assert.AreEqual(2, keys[Encrypter].UserIds[1].Signatures.Count);
    keySig = keys[Encrypter].UserIds[1].Signatures[1];
    Assert.IsTrue(keySig.Exportable);
    Assert.AreEqual(keys[Signer].KeyId, keySig.KeyId);
    Assert.AreEqual(keys[Signer].Fingerprint, keySig.KeyFingerprint);
    Assert.AreEqual(OpenPGPSignatureType.CasualCertification, keySig.Type);
    Assert.AreEqual(keys[Signer].PrimaryUserId.Name, keySig.SignerName);

    // test signature revocation
    gpg.RevokeSignatures(new UserRevocationReason(UserRevocationCode.Unspecified,
                                                   "i thought he was a nice guy...\n\nbut he's not\n"),
                         keys[Receiver].UserIds[0].Signatures[1]);
    keys[Receiver] = gpg.RefreshKey(keys[Receiver], ListOptions.RetrieveSignatures);
    Assert.AreEqual(3, keys[Receiver].UserIds[0].Signatures.Count);

    bool revoked = false;
    foreach(KeySignature sig in keys[Receiver].UserIds[0].Signatures)
    {
      if(sig.Revocation) revoked = true;
    }
    Assert.IsTrue(revoked);

    // test signature deletion
    gpg.DeleteSignatures(keys[Receiver].UserIds[0].Signatures[0], keys[Encrypter].UserIds[1].Signatures[1],
                         keys[Receiver].UserIds[0].Signatures[1]);
    keys = gpg.RefreshKeys(keys, ListOptions.RetrieveSignatures);
    Assert.AreEqual(1, keys[Receiver].UserIds[0].Signatures.Count);
    Assert.AreEqual(1, keys[Encrypter].UserIds[1].Signatures.Count);

    // test UID revocation
    gpg.RevokeAttributes(null, keys[Encrypter].UserIds[1]);
    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter]);
    Assert.IsTrue(keys[Encrypter].UserIds[1].Revoked);

    // test adding a photo ID
    Random rnd = new Random();
    using(Bitmap bmp = new Bitmap(20, 30))
    {
      for(int y=0; y<bmp.Height; y++)
      {
        for(int x=0; x<bmp.Width; x++) bmp.SetPixel(x, y, Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256)));
      }
      gpg.AddPhoto(keys[Encrypter], bmp, preferences);
    }

    // verify that the photo was added correctly
    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter], ListOptions.RetrieveAttributes);
    Assert.AreEqual(2, keys[Encrypter].Attributes.Count);
    Assert.IsTrue(keys[Encrypter].Attributes[1] is UserImage);
    UserImage userImage = (UserImage)keys[Encrypter].Attributes[1];
    using(Bitmap bmp = userImage.GetBitmap())
    {
      Assert.AreEqual(bmp.Width, 20);
      Assert.AreEqual(bmp.Height, 30);
    }

    // test attribute deletion. ensure that you can't delete all the user IDs
    TestHelpers.TestException<PGPException>(delegate { gpg.DeleteAttributes(keys[Encrypter].UserIds.ToArray()); });
    // delete the new UID and photo
    gpg.DeleteAttributes(keys[Encrypter].UserIds[1], keys[Encrypter].Attributes[1]);
    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter], ListOptions.RetrieveAttributes);
    Assert.AreEqual(1, keys[Encrypter].UserIds.Count);
    Assert.AreEqual(1, keys[Encrypter].Attributes.Count);
    Assert.AreNotEqual("John (big man) <john@gmail.com>", keys[Encrypter].UserIds[0].Name);
    using(Bitmap bmp = ((UserImage)keys[Encrypter].Attributes[0]).GetBitmap()) Assert.AreNotEqual(bmp.Width, 20);

    // set the trust level the reciever key to Marginal
    Assert.AreNotEqual(TrustLevel.Marginal, keys[Receiver].OwnerTrust);
    gpg.SetOwnerTrust(TrustLevel.Marginal, keys[Receiver]);
    keys[Receiver] = gpg.RefreshKey(keys[Receiver]);
    Assert.AreEqual(TrustLevel.Marginal, keys[Receiver].OwnerTrust);

    // delete the keys and reimport them to put everything back how it was
    gpg.DeleteKeys(keys, KeyDeletion.PublicAndSecret);
    this.keys = null; // force the next test to reimport keys
  }

  [Test]
  public void T10_EditKeyAndSubkey()
  {
    EnsureImported();

    // add a new subkey to Encrypter's key
    DateTime expiration = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    gpg.AddSubkey(keys[Encrypter], KeyType.RSA, KeyCapabilities.Encrypt, 1500, expiration);
    gpg.AddSubkey(keys[Encrypter], KeyType.RSA, KeyCapabilities.Encrypt | KeyCapabilities.Authenticate, 0, null);

    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter]);
    Assert.AreEqual(3, keys[Encrypter].Subkeys.Count);
    Assert.AreEqual(KeyType.RSA, keys[Encrypter].Subkeys[1].KeyType);
    Assert.AreEqual(KeyCapabilities.Encrypt, keys[Encrypter].Subkeys[1].Capabilities);
    Assert.IsTrue(keys[Encrypter].Subkeys[1].Length >= 1500); // GPG may round the key size up
    Assert.IsTrue(keys[Encrypter].Subkeys[1].ExpirationTime.HasValue);
    Assert.AreEqual(expiration.Date, keys[Encrypter].Subkeys[1].ExpirationTime.Value.Date);
    Assert.AreEqual(KeyType.RSA, keys[Encrypter].Subkeys[2].KeyType);
    Assert.AreEqual(KeyCapabilities.Encrypt | KeyCapabilities.Authenticate, keys[Encrypter].Subkeys[2].Capabilities);
    Assert.IsFalse(keys[Encrypter].Subkeys[2].ExpirationTime.HasValue);

    // change the expiration of the primary key and first subkey
    expiration = new DateTime(2101, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    gpg.ChangeExpiration(keys[Encrypter], expiration);
    gpg.ChangeExpiration(keys[Encrypter].Subkeys[0], expiration);
    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter]);
    Assert.IsTrue(keys[Encrypter].ExpirationTime.HasValue);
    Assert.AreEqual(expiration.Date, keys[Encrypter].ExpirationTime.Value.Date);
    Assert.IsTrue(keys[Encrypter].Subkeys[0].ExpirationTime.HasValue);
    Assert.AreEqual(expiration.Date, keys[Encrypter].Subkeys[0].ExpirationTime.Value.Date);

    // test subkey revocation
    gpg.RevokeSubkeys(new KeyRevocationReason(KeyRevocationCode.KeyRetired, "byebye"), keys[Encrypter].Subkeys[1]);
    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter]);
    Assert.IsTrue(keys[Encrypter].Subkeys[1].Revoked);

    // remove a password and put it back
    gpg.ChangePassword(keys[Encrypter], null);
    gpg.ChangePassword(keys[Encrypter], password);

    // clean, minimize, disable, and reenable the keys
    gpg.CleanKeys(keys);
    gpg.MinimizeKeys(keys);
    gpg.DisableKeys(keys);
    gpg.EnableKeys(keys);

    // delete all subkeys from Encrypter
    gpg.DeleteSubkeys(keys[Encrypter].Subkeys.ToArray());

    // allow Signer to revoke Encrypter's key
    gpg.AddDesignatedRevoker(keys[Encrypter], keys[Signer]);
    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter]);
    Assert.AreEqual(1, keys[Encrypter].DesignatedRevokers.Count);
    Assert.AreEqual(keys[Signer].Fingerprint, keys[Encrypter].DesignatedRevokers[0]);

    // revoke Encrypter's key using the designated revoker
    gpg.RevokeKeys(keys[Signer], null, keys[Encrypter]);

    // revoke Signer's key directly
    gpg.RevokeKeys(null, keys[Signer]);
    keys = gpg.RefreshKeys(keys);
    Assert.IsTrue(keys[Encrypter].Revoked);
    Assert.IsTrue(keys[Signer].Revoked);

    // delete the keys and reimport them to put everything back how it was
    gpg.DeleteKeys(keys, KeyDeletion.PublicAndSecret);
    this.keys = null; // force the next test to reimport keys
  }

  [Test]
  public void T11_Keyring()
  {
    EnsureImported();

    // try searching by partial name
    PrimaryKey key = gpg.FindKey("encrypt", keyring);
    Assert.IsNotNull(key);
    Assert.IsTrue(key.PrimaryUserId.Name.StartsWith("Encrypter"));

    // try searching by email
    key = gpg.FindKey("e@x.com", keyring);
    Assert.IsNotNull(key);
    Assert.IsTrue(key.PrimaryUserId.Name.StartsWith("Encrypter"));

    // try searching by short key ID
    key = gpg.FindKey(key.ShortKeyId, keyring);
    Assert.IsNotNull(key);
    Assert.IsTrue(key.PrimaryUserId.Name.StartsWith("Encrypter"));

    // try searching for secret keys
    key = gpg.FindKey(key.ShortKeyId, keyring, ListOptions.RetrieveSecretKeys);
    Assert.IsNotNull(key);
    Assert.IsTrue(key.PrimaryUserId.Name.StartsWith("Encrypter"));
    Assert.IsTrue(key.HasSecretKey);

    // try refreshing a secret key
    key = gpg.RefreshKey(key, ListOptions.RetrieveOnlySecretKeys);
    Assert.IsNotNull(key);
    Assert.IsTrue(key.PrimaryUserId.Name.StartsWith("Encrypter"));
    Assert.IsTrue(key.HasSecretKey);

    // make sure we can retrieve both attributes and signatures (a bug caused it to never return when doing this)
    gpg.GetKeys(keyring, ListOptions.RetrieveAll);

    // import an expired key and make sure it doesn't mess things up when retrieving signatures (a bug related to
    // status messages appearing in the middle of a line caused this)
    MemoryStream stream = new MemoryStream(Encoding.ASCII.GetBytes(@"
-----BEGIN PGP PUBLIC KEY BLOCK-----

mQGiBEDYQeoRBACKwwPQSmvejhiLpuvp35HPe2lnseOj6S3DwcKdLrtC0aBGjuKjSr1nlqFaiR8ucxbIyN2nK31DB3UIZw1L+JFagmEjX8MGEYzm0NvCmiRuLc2R7Yt80Hi4JgGukRAd9hJ8U+NpVktUluW7TmmwYaJIImdj7ngUcT/yF/Emw4L08wCg4ucRo7KMmphLCKsqNPo6FzQVXCED/i/Zbr1eb9PJylFfkmBoKj3ERf5nsgnAUuBlytrArdZUuNWWdSMmKbOLib4w3OTj86IlOZYvQqgDuixZFp5pfBprMUKLDv1BYnrXahWbab1k/VE6m436O8I9Pq2bzGg6PDprEORdMqdMW1TF4M7q4s0JG/ysE3Cgw/54E8J9M5rEA/9rVCPj6n74chphe8ZZHMgvKJG2XXInSgEgtznL7kx51AKCMPFQWmH0g/L0u1Vp7U8uM8eLAvKbiy1i73exFAWvlwh0jbzEtAgsZ7XuKl1H8k0BIGResqrBnoT7ZTvIu+X1piYNUlxh1y1Ul6cmuhJp2bjqsBifO5eEZTv8CCXp0rQ4SmFtZXMgTWFydGluIChJIGhlYXJ0IFN0YWxsbWFuKSA8amFtaWVAamFtZXNjbWFydGluLm5ldD6IZAQTEQIAJAUCQNhB6gIbAwUJA8JnAAYLCQgHAwIDFQIDAxYCAQIeAQIXgAAKCRAI0ptdr7cw8iapAKDiy5Xb4mEnbmp3IrPVtyZPm6GL6wCg01khh2Mij9+l0QMwCClGzDAVSW65AQ0EQNhB6xAEANxtoA+ojD51o0aX17hfgY1cJUbQEdmhStAV3ca6GZ07wp8wTW8wwNyKqXSqAX3ugL7ewDlcBnCBNeFeCIvD7M2ayrEsGqQQ8bfTDKQ8Ohlp2vsdKc4NRqWY6/JBB+b6imIDPbJc6viXDWX5USER85mugdLieJZiI2woI20sQH67
AAMFBACLFWaqntBBdepV1a0BPGYpSIrkuRLRfmk82hRmzRny5JIhDBrdtIVE1EESsdDcMaq3MUwIgzZsmlvFo+CtAtRNqtsMz3kK40e62pbNf+tehLvmbcuhvKMyUzAJkL7yZ8uncS+HtDUX0C3VWP5npIPtJ0OAeQJuV4bKX/3s8kjMOohPBBgRAgAPBQJA2EHrAhsMBQkDwmcAAAoJEAjSm12vtzDynUsAoK5Lu0BcdvjkxDgAYSSme425qzsUAJ44px1dGoM6bjAJ6DGPU36c8dqGlg==
=Plx0
-----END PGP PUBLIC KEY BLOCK-----"));
    ImportedKey[] results = gpg.ImportKeys(stream, keyring);
    Assert.AreEqual(1, results.Length);
    Assert.IsTrue(results[0].Successful);

    PrimaryKey[] keys = gpg.GetKeys(keyring, ListOptions.RetrieveSignatures);
    Assert.AreEqual(4, keys.Length);
    Assert.IsTrue(keys[3].PrimaryUserId.Name.StartsWith("James Martin"));
    gpg.DeleteKey(keys[3], KeyDeletion.PublicAndSecret);

    // import a keyring with many signatures and some large attributes, to make sure we can read the attribute data
    // and the signatures without any deadlocks
    stream = new MemoryStream(Encoding.ASCII.GetBytes(@"
-----BEGIN PGP PUBLIC KEY BLOCK-----

mQGhBEhvXKgRBACDUvy0v9nv0ObZQkIbBeQgAm0KEjlAVFD01vRN9MldQRdIZjTlnfSpr2rlrVHxiBVhK9Qglg9Ksa0q6bWpYGOWukfVpX1wrNemP7X/8bBR3nMMWvhpkIiwqhabKGGV1q3Eud+Qn0VYV55gMwN2z3rfqozyXuXwqk/zBnWAIgPSowCgxULs5MvNkbUYUUGYtkrolmDO0pkD9iBdwUUegV4qvL326oioFeRscLhA//coS93K7y8BpShGGi2DHkeIqbhb5DrStRD0+6a4KPjzVPE5dTy5JOUkX4xgNjEm9XoJiGaTcKkfQcMnjgLHWR8IFLN/KAJ0G3aa4JfSzN+I0KVGGeXjml/KIguPJhh+fyoNUC1tfmfkHr4D/3rO7pYxxY7R1ddz02dA5Yc+MQ0MbJV3ZUIDznjRg4pOSxkLYsc2Pu/IHBTstoSZEc68uN1LvbRwZhxXUf2DofoPR8kdRyL37x5/xaC3gBtrwjsdudx4J7/E1vQxmRYWRXifAx8jps0QUUzpn35yJfhF9h9Kdem0eoQhU4ksBYRttB9BZGFtIE1pbGF6em8gPGFkYW1AYWRhbW1pbC5uZXQ+iHkEExECADkCGwMGCwkIBwMCBBUCCAMEFgIDAQIeAQIXgAUCSG9fJRgYaGtwOi8va2V5c2VydmVyLm1pbmUubnUACgkQtuWYLWJSQwpg0QCcDu3alyiFLVkrOo5KxfnXA3R0kSAAniPbMXm72iJmwZ8uDKfouQ+Ah7z30dvk2+IBEAABAQAAAAAAAAAAAAAAAP/Y/+AAEEpGSUYAAQEBAGAAYAAA/+EAyEV4aWYAAElJKgAIAAAABwA+AQUAAgAAAGIAAAA/AQUABgAAAHIAAAABAwUAAQAAAKIAAAACAwIAFgAAAKoAAAAQUQEAAQAAAAEAkgERUQQAAQAAABMLAAASUQQAAQAAABML
AAAAAAAAJXoAAKCGAQCDgAAAoIYBAP/5AACghgEA6YAAAKCGAQAwdQAAoIYBAGDqAACghgEAmDoAAKCGAQBvFwAAoIYBAKCGAQCOsQAAUGhvdG9zaG9wIElDQyBwcm9maWxlAP/iDFhJQ0NfUFJPRklMRQABAQAADEhMaW5vAhAAAG1udHJSR0IgWFlaIAfOAAIACQAGADEAAGFjc3BNU0ZUAAAAAElFQyBzUkdCAAAAAAAAAAAAAAAAAAD21gABAAAAANMtSFAgIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEWNwcnQAAAFQAAAAM2Rlc2MAAAGEAAAAbHd0cHQAAAHwAAAAFGJrcHQAAAIEAAAAFHJYWVoAAAIYAAAAFGdYWVoAAAIsAAAAFGJYWVoAAAJAAAAAFGRtbmQAAAJUAAAAcGRtZGQAAALEAAAAiHZ1ZWQAAANMAAAAhnZpZXcAAAPUAAAAJGx1bWkAAAP4AAAAFG1lYXMAAAQMAAAAJHRlY2gAAAQwAAAADHJUUkMAAAQ8AAAIDGdUUkMAAAQ8AAAIDGJUUkMAAAQ8AAAIDHRleHQAAAAAQ29weXJpZ2h0IChjKSAxOTk4IEhld2xldHQtUGFja2FyZCBDb21wYW55AABkZXNjAAAAAAAAABJzUkdCIElFQzYxOTY2LTIuMQAAAAAAAAAAAAAAEnNSR0IgSUVDNjE5NjYtMi4xAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABYWVogAAAAAAAA81EAAQAAAAEWzFhZWiAAAAAAAAAAAAAAAAAAAAAAWFlaIAAAAAAAAG+iAAA49QAAA5BYWVogAAAAAAAAYpkAALeFAAAY2lhZWiAAAAAAAAAkoAAAD4QAALbPZGVzYwAA
AAAAAAAWSUVDIGh0dHA6Ly93d3cuaWVjLmNoAAAAAAAAAAAAAAAWSUVDIGh0dHA6Ly93d3cuaWVjLmNoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAGRlc2MAAAAAAAAALklFQyA2MTk2Ni0yLjEgRGVmYXVsdCBSR0IgY29sb3VyIHNwYWNlIC0gc1JHQgAAAAAAAAAAAAAALklFQyA2MTk2Ni0yLjEgRGVmYXVsdCBSR0IgY29sb3VyIHNwYWNlIC0gc1JHQgAAAAAAAAAAAAAAAAAAAAAAAAAAAABkZXNjAAAAAAAAACxSZWZlcmVuY2UgVmlld2luZyBDb25kaXRpb24gaW4gSUVDNjE5NjYtMi4xAAAAAAAAAAAAAAAsUmVmZXJlbmNlIFZpZXdpbmcgQ29uZGl0aW9uIGluIElFQzYxOTY2LTIuMQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAdmlldwAAAAAAE6T+ABRfLgAQzxQAA+3MAAQTCwADXJ4AAAABWFlaIAAAAAAATAlWAFAAAABXH+dtZWFzAAAAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAACjwAAAAJzaWcgAAAAAENSVCBjdXJ2AAAAAAAABAAAAAAFAAoADwAUABkAHgAjACgALQAyADcAOwBAAEUASgBPAFQAWQBeAGMAaABtAHIAdwB8AIEAhgCLAJAAlQCaAJ8ApACpAK4AsgC3ALwAwQDGAMsA0ADVANsA4ADlAOsA8AD2APsBAQEHAQ0BEwEZAR8BJQErATIBOAE+AUUBTAFSAVkBYAFnAW4BdQF8AYMBiwGSAZoBoQGpAbEBuQHBAckB0QHZAeEB6QHyAfoCAwIMAhQCHQImAi8COAJBAksCVAJdAmcCcQJ6AoQCjgKYAqICrAK2AsECywLVAuAC6wL1
AwADCwMWAyEDLQM4A0MDTwNaA2YDcgN+A4oDlgOiA64DugPHA9MD4APsA/kEBgQTBCAELQQ7BEgEVQRjBHEEfgSMBJoEqAS2BMQE0wThBPAE/gUNBRwFKwU6BUkFWAVnBXcFhgWWBaYFtQXFBdUF5QX2BgYGFgYnBjcGSAZZBmoGewaMBp0GrwbABtEG4wb1BwcHGQcrBz0HTwdhB3QHhgeZB6wHvwfSB+UH+AgLCB8IMghGCFoIbgiCCJYIqgi+CNII5wj7CRAJJQk6CU8JZAl5CY8JpAm6Cc8J5Qn7ChEKJwo9ClQKagqBCpgKrgrFCtwK8wsLCyILOQtRC2kLgAuYC7ALyAvhC/kMEgwqDEMMXAx1DI4MpwzADNkM8w0NDSYNQA1aDXQNjg2pDcMN3g34DhMOLg5JDmQOfw6bDrYO0g7uDwkPJQ9BD14Peg+WD7MPzw/sEAkQJhBDEGEQfhCbELkQ1xD1ERMRMRFPEW0RjBGqEckR6BIHEiYSRRJkEoQSoxLDEuMTAxMjE0MTYxODE6QTxRPlFAYUJxRJFGoUixStFM4U8BUSFTQVVhV4FZsVvRXgFgMWJhZJFmwWjxayFtYW+hcdF0EXZReJF64X0hf3GBsYQBhlGIoYrxjVGPoZIBlFGWsZkRm3Gd0aBBoqGlEadxqeGsUa7BsUGzsbYxuKG7Ib2hwCHCocUhx7HKMczBz1HR4dRx1wHZkdwx3sHhYeQB5qHpQevh7pHxMfPh9pH5Qfvx/qIBUgQSBsIJggxCDwIRwhSCF1IaEhziH7IiciVSKCIq8i3SMKIzgjZiOUI8Ij8CQfJE0kfCSrJNolCSU4JWgllyXHJfcmJyZXJocmtyboJxgnSSd6J6sn3CgNKD8ocSiiKNQpBik4KWspnSnQKgIqNSpoKpsqzysCKzYraSudK9EsBSw5LG4soizXLQwtQS12Last4S4W
Lkwugi63Lu4vJC9aL5Evxy/+MDUwbDCkMNsxEjFKMYIxujHyMioyYzKbMtQzDTNGM38zuDPxNCs0ZTSeNNg1EzVNNYc1wjX9Njc2cjauNuk3JDdgN5w31zgUOFA4jDjIOQU5Qjl/Obw5+To2OnQ6sjrvOy07azuqO+g8JzxlPKQ84z0iPWE9oT3gPiA+YD6gPuA/IT9hP6I/4kAjQGRApkDnQSlBakGsQe5CMEJyQrVC90M6Q31DwEQDREdEikTORRJFVUWaRd5GIkZnRqtG8Ec1R3tHwEgFSEtIkUjXSR1JY0mpSfBKN0p9SsRLDEtTS5pL4kwqTHJMuk0CTUpNk03cTiVObk63TwBPSU+TT91QJ1BxULtRBlFQUZtR5lIxUnxSx1MTU19TqlP2VEJUj1TbVShVdVXCVg9WXFapVvdXRFeSV+BYL1h9WMtZGllpWbhaB1pWWqZa9VtFW5Vb5Vw1XIZc1l0nXXhdyV4aXmxevV8PX2Ffs2AFYFdgqmD8YU9homH1YklinGLwY0Njl2PrZEBklGTpZT1lkmXnZj1mkmboZz1nk2fpaD9olmjsaUNpmmnxakhqn2r3a09rp2v/bFdsr20IbWBtuW4SbmtuxG8eb3hv0XArcIZw4HE6cZVx8HJLcqZzAXNdc7h0FHRwdMx1KHWFdeF2Pnabdvh3VnezeBF4bnjMeSp5iXnnekZ6pXsEe2N7wnwhfIF84X1BfaF+AX5ifsJ/I3+Ef+WAR4CogQqBa4HNgjCCkoL0g1eDuoQdhICE44VHhauGDoZyhteHO4efiASIaYjOiTOJmYn+imSKyoswi5aL/IxjjMqNMY2Yjf+OZo7OjzaPnpAGkG6Q1pE/kaiSEZJ6kuOTTZO2lCCUipT0lV+VyZY0lp+XCpd1l+CYTJi4mSSZkJn8mmia1ZtCm6+cHJyJnPedZJ3SnkCerp8dn4uf+qBp
oNihR6G2oiailqMGo3aj5qRWpMelOKWpphqmi6b9p26n4KhSqMSpN6mpqhyqj6sCq3Wr6axcrNCtRK24ri2uoa8Wr4uwALB1sOqxYLHWskuywrM4s660JbSctRO1irYBtnm28Ldot+C4WbjRuUq5wro7urW7LrunvCG8m70VvY++Cr6Evv+/er/1wHDA7MFnwePCX8Lbw1jD1MRRxM7FS8XIxkbGw8dBx7/IPci8yTrJuco4yrfLNsu2zDXMtc01zbXONs62zzfPuNA50LrRPNG+0j/SwdNE08bUSdTL1U7V0dZV1tjXXNfg2GTY6Nls2fHadtr724DcBdyK3RDdlt4c3qLfKd+v4DbgveFE4cziU+Lb42Pj6+Rz5PzlhOYN5pbnH+ep6DLovOlG6dDqW+rl63Dr++yG7RHtnO4o7rTvQO/M8Fjw5fFy8f/yjPMZ86f0NPTC9VD13vZt9vv3ivgZ+Kj5OPnH+lf65/t3/Af8mP0p/br+S/7c/23////bAEMACAYGBwYFCAcHBwkJCAoMFA0MCwsMGRITDxQdGh8eHRocHCAkLicgIiwjHBwoNyksMDE0NDQfJzk9ODI8LjM0Mv/bAEMBCQkJDAsMGA0NGDIhHCEyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMv/AABEIAJgAeAMBIgACEQEDEQH/xAAfAAABBQEBAQEBAQAAAAAAAAAAAQIDBAUGBwgJCgv/xAC1EAACAQMDAgQDBQUEBAAAAX0BAgMABBEFEiExQQYTUWEHInEUMoGRoQgjQrHBFVLR8CQzYnKCCQoWFxgZGiUmJygpKjQ1Njc4OTpDREVGR0hJSlNUVVZXWFlaY2RlZmdoaWpzdHV2d3h5eoOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPE
xcbHyMnK0tPU1dbX2Nna4eLj5OXm5+jp6vHy8/T19vf4+fr/xAAfAQADAQEBAQEBAQEBAAAAAAAAAQIDBAUGBwgJCgv/xAC1EQACAQIEBAMEBwUEBAABAncAAQIDEQQFITEGEkFRB2FxEyIygQgUQpGhscEJIzNS8BVictEKFiQ04SXxFxgZGiYnKCkqNTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqCg4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2dri4+Tl5ufo6ery8/T19vf4+fr/2gAMAwEAAhEDEQA/APDM0dqbRWZY6gAnoCaaOSBjmrVrFIHDr98dBjr7UpOyHGLk7EAByCOQelOCNjdg7e59K2BDbYXcCmeRjkKfUUPKEf8AcxDLcE9j61l7W+yOj2CXxMyYreWYhY4yc96tTaXPAF3gFmA4Xt9avQiSNDHAmxccsRz9BVqMhB+9dNmcY6kn2pSlUvdLQcYUlo3qY1gBbapE0pAABGfciussiGu4SpBG7qDmsiSGCV3ZI0j7ZAyT/hWatxeWEu+3mK4Ocjp+VRJOp6lxtT21R6GRVW7/ANaP9wVmaR4lgv2jtpx5d03APVXP9DWndf63/gIrjnBwdmdUJKWqKknpTe9Occj1pvWgodH9+iiPhwfaimhM8+2kdqTpUppjNXq3PIsOj8okByRWnBboqh4LjLdhnOaxu9SpJtI9fXNTKNy4z5TdMkccYkkKuW5x6e1Qi/QMNsYGOvoafpeg3+qSBoiPLc4JPf3xXoOjfDSDyxJdSM5wBt6VjOrCnub06FSq7nm815NLGOCCQckCq8byKysdxK5Oa97t/h7pi8mEu2MAt2qtc/D/AE9WP7vr2rJYyDNXgpdGeK/bXjUIXIBPIUdKmS5gYEOrjPUy
N1r0y48CWafdh3H3PNclr/g+SBGuLdX8xRnaec/StVVhLQzlQnBXOTvItjCWM7RnIYdQa6LRtZk1NHiuSDcRKPmAxvHr9awbWITK6Srnrn1FJa3Een6nHOFIVTtkA7qe9VUpqUbdTOnUcZXOvk6im0rnJGOR1zTRXAegPj/1n4UU2M/vPwooCxwxFNYAin0h6V6h5RCRWnoGjPrGqRxAfukIMh/pWaRzXpHw6sgunNckfNJMefYcCs6s3GN0a0KanNKWx6B4f0K3sYF2oAcda6aGJVAwOBVKzYbQK0FPAry3Bt3Z7SmkrIuRE9McUk6bxyOKfbgY71M65471SpLcycrMwbm1Cgkce1c7eQrMJEdciuzvIgIeK5e7TYGb2ppWY5SujxvUbCOw1W5LD5QCR261y1wQJ2Ck7ckYNd74njQXchc4LcZ7V59OMStjp2r06TujxqytJna2N0LuwhmAAJXBA7EcVZUZ7Zrm9Le5TTlEDqqliT65p8qXcmS80zf8DrllQfM7bHSsQuVaam688MDbpJUQD+81Fcw9u45Ib6kUVSoLuS8Q+iKdNY4FOpr9K6jmIzXrngGEp4dtsjhstn8a8i5wa9M0TXnstLt7e2tJZVhiAJVSctWNbVJI6MO0pNs9UsgpUetaI7LXA6Z4vYOsV1YTwZHUjpXa2N2LoAqc1xyi0ehGaaNi3zjiptwzya5vU9cvbT91p9m08x4yThV+tZIv/Fl0yvLaQRDPRZh0qlHQhy1O1uFDxH6cVyuoHKFRSLeavZKTJEzox+7vDkVWmvBdSNmNkbuCKVn1HdW0PMfGitHIWkUhCdpI7ehrgZVI+919K9d8bWq3OjzHaA6DOfavMotPuNUvIreziaW4mO1EXqT7120Ze6efXi+c0rFGGl27lcKy9vrUhHFdVfeGv7E0+G1NwLh4Yws21flU+xrm7iAxNxyp6GojUUmF
SlKG5WNFKwoqzIwaY5p9RuK0ExnUV6l4X1h7HS7KK3jD3M4AG44VR6k15aehr1rSNFE2mWBYbTCg5HGcjms6trK5tQTbdi7d+Kb1dfbSJLa3uMSJEzZ2lmb+6DyQPWup8IIwuNRjIIWF9oHUKe4rMt7CJW847GYLhpZBuYAe9dH4U2ppMk+0L5rNITjrk8foBXLVatod1KDvqyG+u3hWZ4UDMpAAPcnpXPeIdR1nStKtb6G7UmWby5XSLfHbjHcDkntXRXMDSOSpwGJDD1FRWmlqjkkAr/tdD+FFNpDqQfexg6R4l1KS0tZ9Rt1kFwSpEakNHjuw9D610U9ms0TyRliwXINXksICFVUGemQOlWWiEUbJt28UTkTGOhwHiK383SZgByUzj3rB+Htklp596ylJW/dh8Z2r3Irq9YC/Zpoz1wazfheyy6fqIYoPszEfMcZBq4v3GZzS9omWtfiS10++baVE0a7cnqScVwjqGUgjg12fjW63RWtuOp+Zh7dq41qkKm5mTwmM8fdoq7IoZSD0orVT7nM4a6HH0x6fUb10GJHXvvhqMXWlwuOhQcfhXgZ5GK9r8AarHPosKhgSgCt65rGuvdOnCtc2p0OpW3lWEoH90mtTQlZPCtsOhKVj+IdUig0uV+GCkb+eduecVuadcWy+H491wm1EDhgchlPORXHZtHoxaTFVlXaG/PtWiUXamSCMVzs+sxiIkiGONuFMj4JHripItRsntlEd8olQDBJzn60lGS3HKUTq7dFYbgAOOaz9Ql2q2KgsNZE0JjyolUfMuevuPas3U7zzFIVx0zz29auxk2cp4tvPs8TMpxuWqPgCz/4k11dSOT5lw4KZGCR0J9qqeL5/NjkU4CqDgg5rM8Nz3SaVBb+cRbzb5DGOmc4+v4Vty+4c3N+8TNHXr77fqOUOY4V8tW/vY6n86yWFa0jbQcAEgZxVK7cu
kR6ZzwKiw5O7uUSMnGQM+tFOdTg0U9SdDiutMenfPuClQCRkZpq7pR2HFdhxDK6vwLqi2esR28rlYpWw3PrXKlCD1p8LNDMsgPKnPFKS5lYqEnGVz1HWrXUZbqeP59kJz8p42nuB3pukiaCNPLkL2zKCsYcquO+Ae1a/hvWRrGjRyPjzUjMbHvntV7Q9Tk0/UIUltY57aPeFXaNy5PYnjHXj3rlTtozuUObWJhT6Hd6hkzzRoAf3e1iSoPbAq1H4SvfkEckvmfKxdxsTae5z2rt11zfGkFrpWxk3BdzLhc55yKriG6upVe9mDou1Qi8KxHQ4ouy+TysYXhy2u/PaG4LqFG5N33sH+lWb2QGTyRJ3JbPYVtSSeTOxRASy4J7g/wCFc5NJ9n3TOikkEsp+tZp3dxNWVjmPFMjLDMFxtX5icfeB6Vy9hqsul6Zb3BhM6lmjjQHG0HnmtPxFqT32LGFczTOF2DsPWpJ9LMekfZo8GSBQQQPvEdf610qyjqYaOaV7GUfFlxlm/szBbHzBmz/Ko5PFW/b5liylc/x9f0qNXIXdu4HfNVJ715D5VsCxPG4DJP0FNKMtLHTWw6pRu6mvpua1vqMWpSbIFdZNudr4GR7Giqljp5tWE9w37zsin7vr+NFZyUb+6OOEr8qclqzAlmXzlZAThSOaLZ1UkMcZGKiJxz3pvauu2h5JZkZM5B/So/MGeM1EabRyhzHVeEdYax1IQMxEMxwfTNemuk6D7TZGN0PzFX6D1xXhkEzQTJKvVTnHrXqejaquoWCI7l48Y2r2rCrCzujpoVPss3dN1vU7y9EENrGCTgvIPlHvXZRQyJFunk3vjLEDgVw9rdR290vl/Iq9Fx/WtS/8RyJZsAw29CyHn/P+FYSTex1RlZasnv8AURDdNsbMZX5m9Frz3XvEqNJGo5eMlQqnOR2OfSqOq69dXcy2+nvuYE5Z
B3+tSab4da32veOokk5b1A9vQ1pGmo7nPOq3oibw5o9xPeC+vI8SMp8oscAY6k1tXtxbaeJJbqXy4R8qnHLH0A9atJf2HhvSjc3WXLErFEOZHb0Gew9a8/ubm98Sak88rBUB/h+7GP7q+/vVWvq9hQjKTUIK8mVrh31W/f7PD5aMc7M8KPU1rWdlFZKSDulPVz/Ie1TRW8VrEIoV2r39T7mnbST1rOdS6stj6LB5eqPv1Pen+XoMmjEi5HUfrRUvlNjg80VmpWO2VK7vY4TPFJ2o5pK9E+FHUw8U7NNJoAmtrdrh9qg8/pWpDHfaY2+0mYr6f/WpmjsEQhsDzDhTnrjqK02yc459qxnNp2PWweDpVKXO3q/wK66/qiBmJfd1zjpW3p1odatkknv28sr/AKtAQc+hNY7suw54HetLwpdFYptq74UkKjPYHkf1pbq6M8RQjSa1vc6fTtHgtVAgCBv939Kk1vVbPw/ACxEl4w/dw5G4+7H+EVh6r4xjtIzFpzK8x4aQrlU+nqa5iK0mvZmub13O9tx3H5pD6mly/akY04Sqy5KSu/yHE3evXz3NxIdpOGfsB/dUela8YjgiEcahVXgAVGpVECIAqjgAdqQnNZzk5eh9Hg8JDDR01k92TeZmnRksfQetRog6mpC4jGentWbR3J9WTs4RMk4A70Vaeyh+z288qsmYTIy+aGDnjb9Dk8jsPfNFZRkpbFJylqjzXHvRiiivVPgAxTSKKKAFXIOQTn2rYSe4gwJoy6+oPzD/ABoorObPQwMdJSTs1YrXVy1ySN22P06Ej3qS2e7aKS3ti6xyY3gHAOOmaKKp+7HQwop4iulUe5etrKO3O+QiSTtxwPoKt7mbmiiuZtvVn0lKnCkuSCshwY+lPQ85NFFSzpgOM+Pu9f5UsbHdu6t70UUmrIqMm3qTouSTjFFFFYnUj//ZiGAEExECACAF
Akhv9LICGwMGCwkIBwMCBBUCCAMEFgIDAQIeAQIXgAAKCRC25ZgtYlJDCpWfAJ9bDkoB9ETeriCtXsQ2i6O+0CNo0ACgwMUorOyLhiqVkJAB8myCioty2zm5Ag0ESG9eOBAIAM6cvQt7EL63IVFV+XPhUJb1gDpsYQdhkX5LJj0z7ivakDIWcR+Q3FNCIx/emi+LbgkI5w2Pvc0nEYQHSjjlwtIiCTkGVSde/XVOHYxJTHU6uI5KsoO/u0BUvXRqzTLNd4Z6AQ7PkvGbkGRBXJHe75ZMxWFS3Hb3mZ2bahoK2AdMftM8QgKpJY25IMn4TzAYjREFDAev4zJE03NOKyLFXmxTQLqKWVfw9QZXE1IjC9gV5ZQISKX+E5BJ8nvXgPBXynDIHjGCzQwiyD27wgm8JJ38kmnQbr6TmprG5xMw3aJD6GiQdlsXt4KdX2CzQG938z+m23s3CnoRPVPkhEl9lscABAsIAKC8xQDETN9lzDdmPfvKmMwEh7EpZXR2BB7ZbWYXXWyh5YKf9AcnVZNW7fDfoG0Xw1my/A643usD8FBnu/rsTcTz8qetYdkeFzYxHcFUex1K/h+xqdwOOgCyFALSZWVbml/R6a80KgEY+KWm/SepbTBcbdkFEZbqrmX9lLEp6Pz21QwGV7wnH5Z+HHnOzUlx/EVIihnOWyZG7Aqh9dgCa7YFjjXzZRj/wfL2etabXiGfHTadhKtkRz82cla+/p/eUV7FGIe83sbDveY+HJBIOAWYAr5yY0cZJgH05Xjw3Hgihjns4VRYyoGgtPSl2OFL0igIO94JOIAMWVGAudcnvKGITwQYEQIADwUCSG9eOAIbDAUJBaOagAAKCRC25ZgtYlJDCr+FAKCxpCMLw26rGf1dcVk2baGsfAYqFQCgs2YyE8bFl2UC2qWTRCgSdvwkwXyYjQRIcJ/UAQQAoVioyCcFDWgMTkTt
rXvey6ZAzo8pNL0Qw5KzHpx/2fZ0zmvjp3H2yIY8n3l2oxNrdZFgzOZwYl3IgOayjzzM+sq3YvDiIxkKY7GhYSQf96g/3endyjuVEzoW8ZtGugUjPl1PF8KRFFvfuxAk+wXXJwnlRnBJy42ynrpHgBI5sPsAEQEAAbQXVGVzdGVyIDx0ZXN0QGdtYWlsLmNvbT6IuQQTAQIAIwIbAwYLCQgHAwIEFQIIAwQWAgMBAh4BAheABQJIcKAWAhkBAAoJEIscSShCsQV4i3YD/0Avr4G0RvFSI8023rDT1tLBXU8rSz/yxpXzeGwQec3vlMzPW7usnjlhOL7obPOmh9pvIWATZgy9RTORe7gnwHq/tuHv772Q1TdlYW5E8mWFhxcnecHD4kSjvNPcB5wA3PRbywdaBWoXgVprO0G3uXEYPKilZt/2dKrVsCj3tqAY0d5w3m4BEAABAQAAAAAAAAAAAAAAAP/Y/+AAEEpGSUYAAQEBAGAAYAAA/9sAQwAIBgYHBgUIBwcHCQkICgwUDQwLCwwZEhMPFB0aHx4dGhwcICQuJyAiLCMcHCg3KSwwMTQ0NB8nOT04MjwuMzQy/9sAQwEJCQkMCwwYDQ0YMiEcITIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIy/8AAEQgAlgCQAwEiAAIRAQMRAf/EAB8AAAEFAQEBAQEBAAAAAAAAAAABAgMEBQYHCAkKC//EALUQAAIBAwMCBAMFBQQEAAABfQECAwAEEQUSITFBBhNRYQcicRQygZGhCCNCscEVUtHwJDNicoIJChYXGBkaJSYnKCkqNDU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6g4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS
09TV1tfY2drh4uPk5ebn6Onq8fLz9PX29/j5+v/EAB8BAAMBAQEBAQEBAQEAAAAAAAABAgMEBQYHCAkKC//EALURAAIBAgQEAwQHBQQEAAECdwABAgMRBAUhMQYSQVEHYXETIjKBCBRCkaGxwQkjM1LwFWJy0QoWJDThJfEXGBkaJicoKSo1Njc4OTpDREVGR0hJSlNUVVZXWFlaY2RlZmdoaWpzdHV2d3h5eoKDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uLj5OXm5+jp6vLz9PX29/j5+v/aAAwDAQACEQMRAD8A9U8V3P2vwzdwFltr2VDCscjdNxwc47YBP0rOfULC70uBDeSQZZbeXaSI3Hc5Ixjg81IyXh8fPc3NuDYW8WwuH+6WG4MR6YGPxPauoZFaR1jSOQMM7XHtjP8ASvCr/vXaTu0+m/kRZs5mES6tpgltL+NmMKq6ugbc2MfgCeKp+FlTRLnUdM1O9jN1FdGfzAvyvG6rtBJ6HIPHtWxdxpbJcwxeTZ3McpkSd1+UhySOnb7w/CvPNWv5YtamWHUJpIbq1ESSxRD/AEiRTgBT3wWIJHSuFU3Sk4PW/wDw4l7rslqeg3dze6zPHbaZMn9nMp+23R52r02J/tHnJ7fWqU1vb+FbG21C3kaWCxBic4zmB5OnuVwMH6+tar6atjoD2+nsbPKgbVG5Rzg8f4VleJi9p4Jv7e5jJR4l2SoCUA3Dqf4T9eK9PCtXjLe5va0bnY2yQlmuITuScB9wbKngAEfgBXEePdYuI9T0jTNP3/avPEokjOChAIwSeACG5z2+tVrHxHb+FpItHUzXlvHbh7Z1f5ehDpuP3uQCP97FamgeHL69WfU9amkhmvTv+yxHaEX0J6gnjIB7CuytKc4p0F+hlui/ZR2ccqajfHGp3Ufl
lS5YIvXy1HT8epOafrAF5YyyoF/0fbLEpGNxU5H4cU+HwloKXSyiyRpY23DMjNg+vJq5caFp9xEUNuoG1go5wpIxx6fhXM6FWslz2t2/4IWZHGLFZnjklgkdv3iKw4GAOffoK5DVtSk8P+K/7Qt7i3zfyxwXiyvyADhSgHcAnPtW9deGrQQWzxaTA8iZjZSOdp69/X+dXB4e0EGFTpFqxYkDfGCRgH1qqVKrJezj7iXbt/mTrezMS81/TrNbu5ubuG2uFKu8YOSw4+Ye+Kln8beGLO0LRXYkl2lUiiy5Zj2GOM1ymn2lnpNzqNrqei2/26xeHY4wwdG4Qrnq2BzXTW2gwW2ty3FxHA7yRGeCJcERk8O31ORz7158qk6KnGUOa27f5kpNPRnKouuz3kmrwwxWFtcCPzYrhPNdFHAKIPQHPJ5JNbWn+HdJ1FVur28GqTqf9ZL8qR+gWMcA9M96ux34eVrJY2fyhjeAcOc4I6Y4rGubi40bUhZWsFvO91hordyVIbPLsR0HGfqOOtePGq53jaz6W009dzOM9b2NG+mXRvJsrKAPeXGBbW6/KoAIJJ9FHXP4Vm6JI/hGXXYby5luV85bt024Zg4y0ij03gg+wrVsYIrWxjnvnmuJ3IM10RnLkYwPRRzgDtVbU73Sx4h024dm2yLJZXHmqRkEblz+II/4FW9F3i4x/wCH/r9Sotbo27LUZppL95raSWJrp45HVBnaqhcY7jORkVq/2mltuRyqbSAPMO04PHQ1jWM09zrl5b2kiKsMzsR/d3KpAb6kufwrauYDcWLLd22XxnJweelejL20nKrDp1/r5dDTmdtGYni54/scU8juxmP2crEcfIxG9s9toGfz9a57VPCsFv4UF1NmK9tJGkhKMcRqX+4OccDvVyGb7Xd63G/+psLV7eJT03Mu5yff7o+ma6mSxspdNKS5k3QkhCchgRg8
fj+tcy9pN80Uu718ri+Jcxyi6/cwzx298ZI77JcW0OCJM42jGepJIx2x6V0bXf2wtbaqsC24ba1v94PjBG71+nt3rzGdZYNQt7hWYXECBY3YEFwM/OT6Yxj8a1pdYVcy7nZmwxYtzXTQU1bkdjSmpOJr+Io31XVh9lgiS2tlUJMTgb48tgfi36Ump+OVskt8jMsI+YZ6muIu/Fk7sbK0V5C2dqICxz9B+NVU8C+L9ckNwbMwq5yPOcKcfSvUUXK7l1NFSb3OwPxElnYTZ8pjjlenX0rsvD3iuPWIDyryjGNpwG+leQj4a+MbSQP9kSZB1VJQf51XEureGbyKOWyuLFgcjemAx/lj6Vag43cHZilTfQ+hoTLvcTJjDEqwORiuT1K9ltviRZG8kENhFZsYsnPmSOwU4A5zyB+FV/CPjS0vIls7kfZ7raNu4ko+PftWpaGHUNT/AOEheNipAtrdZDgIoJLNj1LcfgK2eJhSV5ENNI5rx3c2+m6tZeJoPIv1tz5U1qoy7Ic8g9Plx+prVm1yObRDfxxTJaeR5i3Dbf3aYyQcHpiuZ8TwWyeLrNOY4r25VlZDgQ7AzMcHj5iai0DXINCuH0a5hmOkxKLqzkuVw0zEjdu9QCcj8PQV4dapDEXcdF+auZNrds1f+Exa601oNHs1Tb+7M7qRAMHqO7c89vesWZoJBcpPqBlPnIZGhbLSnAyQ3opzgDgVq3fj/QUtJYpUYhZnBjiXcCDyOncniuPh1Ma7q/2ho4LFMlC0uSwTHB498CuH6vLmstun5mc05aI6KDxNLbRvZfakXCEpuAKnP8Xt3P1FReJTPf6KswkEvlp5srAclhgr+GMCuai0uZNXt4NWWe6jljZAIFOMZ+UjHLYz07Vs6b4fsH07yrqWTyYty7vNIMh/hyvuOacowoyUm9RW5XqzvdMjtYlv5bZQJ7q6ct24ACqPyAP1
zXQLqsQt181QvAGT90155pplg0WZLYzMXZ4oBL8wP7xsNnrjBNathFqs0MrXMtrGDtDIg3FVUfeAzxSpYqpCUlGVl6fIpVHfQbLPZw+E9TltgqT3fmeWGPMm92A/9CrQv9QF1fGxiI3bUNyVbiNCOAvudp+gyfSuNj1S8fTYlt4YnW48qKLep+XYqHcfRMDOfVq37nQESNp7q6kjmiCPM6KFDvluuO3OB6DFOrJ2a216fIuTbWhzviPUop9ZMkgRI4EEaIvQgelY1rDdeIr8WVodkY5llxxGP8faue1rUGm1aTcxIU4/LivSfCyRaPoStMViyN8rHqWP+cV7lGkoxVzthGy1Oj8P6HpmgQCOxtwZSPnmfl3Pua6aGYlD2NeZ3/jqaB9mn6dNMv8Az0ZcCrGkeNru8uVhljClj0rpTsa25tD1CJ8ryeajvrC01K1a2vbeOeFhgq4zXB+KNS1azijS2dkaUfKw7Vi6Xca5DIJJ/ELDv5bYNWqiehm6LvdMyvHfh6XwbewXdo0j6bM2FZuTE390n09K6rS/FpHhqzSysDeXIOEgRuW9SfQDNX5NTtte06bR9YSOaKddvmL69j9R1ritEm/4RHXprCdvMwCIXPcGuLGQcqb5CJxdvMs+MrXUpG0m3v722RZr1kIhTbjkbfmPOeorB1vRgczR28+6Jx5StK0jvz8yj3NdHrWkza81vO08duyzLHHu5yu5fmPoMn681uW+oWMarI0u66t0OxnGd+OuB6AcDNeLGrOEE0tVfQ4pc2h54jXF1O11Dpfm2l237yTbgfLwMehC8Vv2U0KaxaR6fGggiDyLNcxZUgDocdSMnHNY2jaw+n6neWM0bTQtKJ7WGR/LG93yTx27+mK6KLw/4n1ZrmaaJLR1kLRBuAW7AY6D3+tVUjOMrE8sk9Bt7K1xq1td2XnzSiJ3j3YABYsSOB7Dn0rVtvC2
owsLowq7bi43HKseobB+pGDS6fZraWxSRRb30Kg3EZYsZ2wclR02kjtjFakGtSJby+WLkeSFRg5yC45Yew5AzUyTs1O/l/XYpLXU5yw0qCKzie5kl3hVGC5JiA5bd2wSeMVfM0ELXNz5jqmAY1iwDJ2Cn1LccVT1O+s7OO4MeoBpXBkdh/D1GMd+tUrBpA1zctaSNtQGGNgCqRgZb6E9SfwrnknJ3e/5swvct2cAs/D3m3aLMRChJXI2odvyqPYAknuRWjv1G/1G5sreLzkR497FsLtA469cgVg26NH4YvGKbDOXOBISERugx6YbHFWrOJhpz39xq1wHCBo1jl2YBGM47/T2qowvJuXf/Iu92ea38Bh8bz2so+7c5K/jn+tdrf6kkMxWQZjhXIX39a4Jbl9Q8aSXDtveW4+965NelyeGm1WWQniNjg+9fTxuoK56dO5mw689zaefJe21lbsSEaT+MgZwBSaVJLcais0kIBiZSJEHyyA8jFdNB4IgRIlCIqRjjcM4rH8YavD4YsQYcNKvCr6n1qm1axvGLvds7rWLb+3fDi+SVF3FkoM9Djp9K88fTLuwsShtYp71yQ7TSlQv0xXK2/xA1oHzIX2SleFPGa9G8MeO7W/0yN9YtAkqnbKxXOD6/SjmSdxqKkrIzdE0TXPtO5od9sQGVlbJU+lUfiRZzWt/bXIUgvHu59RwRXsFnqGmyQq1v5ew8gr0rn/Hujx614edo8Ge2zLGfUdx+X8qbXUxk+ljzTSfFU95oE+nNCs2UZFDjOOhH6qPyrcWwknsoUkcWxmRV8uMM7ynO4A9N2CcfhXG+Bbn+zvGCJgMxIKq6k9+frxmvdrXUbe9ijvZrWL5JikcrJt2EA8knkDqPxrz54aEql3Ll6/10Rx1Fdnj0dlePNLrF0zSAqsbXBj5hm3HbuXpznHp81dpYa/c69pKBLrN/CSzrMAE
JDc8j0wR+NS+K0tJX1azl1GFrm4+zvtLbVjTcVAwO+Oc1zGk+KNE0q+1G+vb+CW7K7Et4EY72A4yBwewrLEuSuqd20S0+h0dnBqmoaU01wRE1xcH50jw8YOMD1HT8iavQW+r2BV5GtZIZG3Y2FSuBxkdOo6/SshviHoV9Yi6NxJbPtBk+QjJ7AHvzWN/wsuxnme2gjvLjKKZZI0LFQDyD7dPzrkp+0lVblB2JSd9hmnaN9tlhurwo0cYARVXaT33H1GcgZ64zWxJCGsJLYFozPJteUAnCrk7foeBVL7bPNM9zdTR/Z9qxhi2wuVHQiuW1nxPcrOba6tXt0bIWVHymD/WsqcJTnzRZEIX16Drq91K0nksYG+zbz91x8pGex/Kr1lq9pLZzaRqUjw3MEDBQgyJGxwAew5rClne5s3aSTzXg+dSD95cY/A1yEF/PFqHlyNvWRlO/BHHFehRoKcWktvzN+WOyRt+GoxJ4jtHfvNmvabbUo7ckEgAGvD7GT7J4hidG+VXBPt7fhXW32rsGBRjjPOK9SWiOqDseg6t4rjsrB3QgvjCj1NeUeJrw3DoZ3Mk2NzHsCadqN7JIkTux2KC1YcGdUuH85zHGvzM/p6VC7s1lPSyK62ssuZCwABzyecV1Gl67FDbmA7ShGwgjr70608T+EtLtIohZPdzISWkKZzkdOayrq90TV0k+wRNaTDlS3c/4VT0WqJVlsdBo3iObTrw2olJgY5jyentXeaT4ja7LQytlCMEH3FeOeHUe+1ERMTnefwIHNdLBqJtbpog2CDnPrUveyCUm1qPhjisdRu2imZLyKRvJAHRe/ParlnBe3enGG98R3IMhyIo5MYzk4LHjGe9T+HbNdbnuo3QefvWVH7+hX8QTVWwN94JuhY65C0tlMxaNiDt74BI9xnHpXLiUnJK5hWjomjpLnSPDi+DreYWqT3T7ZLq5ffN
IQpw37zsvHrwK1dWGhW9oGtNGY4kEsYjt/L5XDZDEDHBqjo3iCyhs5dChmmvUWLIkROJFYfNn+7yT19a0hfJqGhSCWXZq5HkvEFJBXpgDvxzkc151SpOpJpvRP8AA53K7sYl94YlvNLjt9QsY7a1j2OsUWMsQT949e7ZAqlqclto3lwadZxw20Q82JkT5XyBuHuM4/Gut1MrqbKLpZYWVVWSMkYiH94/rx71h3dpY2jbZb1pI3OyOP7xWTI2n6DJOPesU/e5ZO66diG7uxwfiDVtP1i8YW1yEiB+SI5AH/16qaJo93qqS2l4jtaRuG4PLDOdoPb61X0PSo9cvpRKyxRBN7ADGSP4R716VZaBqdlof2yxuIYLOQbUGzc5UnGRnpmuup+7jyQ/4YcpWVkcpcWNtaWqDaUYFnGG4IBYAD16Y/CucuLO2uIIwkZWI8ggYb257f8A1q7LU7VNHiDXcqyag8zKsbr5gMW5lk3DsN2OaztH0qKytV3yzNDGD5jxBZWUHIPHcYP6U6bcItt6vYIX1uYuq6a1hax3Ma5d3ZifbtWcb5nZOcjFd3q9/odtbPaWck0tuse5HmUblVuxrzQMnOw7lDnafavVhJSidjkpWkjrEhTUvDcwUjzY2B98VyWpG601HgljZBLyrY4YfWuh8OXQEvlP/q3OGHtXb3VjazWiJLGsiAZ5XI+tNWTszaC5keIxpI4wqFs+grT0/QNYvLhVtrKXD8b2XCgepNenQahpmkNmK1iDDv5QrTs/EQ1SQBYSxBwD0ArbmRShG9rmVb6BBoFtZlDvkjB82Qj77EcmuVmlP25nPQ9K9H8SL5Omq78ZI4rzG9Y/bQF+6TWOrdyKuh6l8MrmGSE2qoDdPIx3bMkKMdT6f41a+IWgN4ivxY2ZlW6JQqAD5aYVjl+wzwB+PpVHwlqdt4f8CDWHg81rfzJXAzuO59uPyFdp
4elsNP0cXS3UVxC+6dp1bJcsSR16j5iBXm1q6nL/AAs5ZVObfocb4RjtL/w/pmvW2621BWazkiUALcHI4z7gdfX6V1019BcXVvLAgMkUZ3bl27QzYww6jp196818T69deH/FepXWnRW50q7Kh7dHAXcAvzgD7rkntW3H4ttLm289llfUlTJgz+9hAAcMSPvJ/k1k6XOnKPwsyfkdNqcsN35M8Eqh0VvNi4wo/wBv39DXM3fmSQ2otZlW9MjyTg4IRFPGM9/mX65FXrPSvEUqT3Re3SOZfMYLGApU5xu9wM4IqlpunRHxQ88OpyTKYVlDSuqMAudxbjg57fjXJTi23boQrvU5iCxgeKxjs4zLN5bOUQ4HAz8x9eG4+ldn4UtbK805WuXnRo3YsgmwWznCkdeOtcPa38cF/ZrbqVmtGBQBcb8Ek7iOuc4rsdJv7SDR/OVQuqI6sbhzv2jcT37gHHuBXTzqK1ZHMmYnjBLJ7C5jSEyYlkTq3mBQwIA9RjP6etc1ocV1ELlmikhjUNt3KQAuOMiuj8QXF1cfama4MsfmlklVdpOe57gkAVz8uoXf2eSK5AlTyyUZvvHHOKuNTmXLDVMcJXdkZ7gTy3aSAR284Kq6jOwjgKfbgmuVCS2k8kEgKsORn+ldRqV1IltZ2UFsouAqB1jBPmOTnr3znmr2r6X9ohMV2pOopGpZguAmRkKvsAcV0063svi2f9XLU+TfY5exvPszl+5/Su00jxTCLdIZxlVGOTXm8xaF3Rs5B5pqXZToevevR5bo6o1HHY9RvbvSZY5CFzwMHNLout2WmBiqLuzx715f/acrEgsetI2ovjAYihQ6F+26npXiTxYL+JF3jAGcVyLXQmZTnvkH0rnftMkjZLHrXQeHLNr3UIkZGZM5YDuKU/djczlO+p6nojRXXhHTdKaNghdhNtO3cu1mQse4LZ/759ql8Ahg
95o01wRDZSFyo6iPGV5xjrnj0qhYXLMtv5dzsMyCONj92ELnGR09ee2an1GG70DWX1mzjnNoEZLlS4/eLx1PfHX6CvnpXm3G2+vzOX4iv4w8J6e1/ZXRunNzqJLpGoAL9DuA7dqbNpEGhaJLrFj5qXdtGcSuhbbtHzCTsVcHA+lOvbo3kod5VmuYIYxEhiIZAS5JB/hAJT68DtXV6jo2ranZi2uL+ztwyeU1skG4FMDknP1pyquEIxb9f69CtTJsPFdpJocUqt5cM8O1bcZBik7qDn7o/TP0rDuL1r6e4tra32NOgS6mH8Sg8498ZGB1FVdQiufDVybqVrWe2lh8ibaOAeikD/dwPwBqlFfz3dj9htGkRF2tkKAzbeDnv6dKpRVuant/X5EtPdDvtP8AZkkyNaB9zfOzZBXHYGo4byO8bFndGNW5QNx+HvV/xFNDrkkbaKZZ7lUIkRAfLYKo+fJ75J4rn5ljtl2XRuIiv3WaI5jYEgrgd89+9XTpRau9yVDqal9qGpRM0L2yTIQN/l8kH1xVlhaag0T2xjV1GWQZBIx0x2rk7u7uXu5zb2csjggPMcqwPc8cV0NvaX+n2tvc+crpKI/O5Jwz9snnI6HtkU5UuS3JoxqLi/d3Es7S30/U4dVilu0uIXHl+aAypgEcA9RzWheXMM8nnSXLOx2hmfqegFLKLmO3jBBnKffLvuy3t+FY81s16rOzsrnnMY2j24rncvaP3nsYyqczs9jl9ctES/nKEgbjwRWBIhIyK7O6sZZbaSSRgzg4yB1rnjb9QU5HcV79GfuI7or3UZG1gcVNHA7n2rQW1DcbPxNW4rXGAF4rSVQdjNW32kV23gyNob1ZHAMeNpyM8Vl2+mpJnI5NdTZQx6bp0s0ibgADjOOM1hVlzQaKtodDFYh5xcRSJwilVxgkk4Le/IP4GrE+r2Gs2uoQ2Udy11Y25NxF
Gu0bjkHPbua4271WXxVqlhbxXYtYCxM0sQ2um3/63Std5hb29za6dPEMozK7vt858ANvOcnoMZrx/Y8kuZ7/AJepzuFncb4bkm1Jry7nkRJfMiGwnaCcEjbj05/Otm5vP7MkngkTzWcB0D5G1u+e5X+eay/Dc9tZWCQhXedR94DjfuyvHv049q1rueS2Mk0uyWVhmbcMn8/QdOKmvpN326D5G5a7HP6xm40jUJLohUSIuoI+9naAF9Mkj8BVW00bUrKwtrC4P71gBFMpBCseQjH04ODSz+JoZIZvtRiaz3lFiC4bHXJP1HStzTXX7GrDa0TsHLyfdK4IAK9h710U3JR1WheqVzirLUPEtxavpUcKwSM5eSRlEbfi39K1LPTJNOeO61HUpbuWJ/mtwPlxjrnuf8KuQ2Tx332dr9JIYgDILrDBsHJGR15wage1me482REWOJ1V1Eh5yx6A9eOPwp1aibcY2XcxlLXQtXEdvHoMkxnE927sSUUqAuBhcevJOfrVa6naJQhjaSMYOCSAtM1SQXjvbQrsQAYYnGTj/IpW2LozxF7hnJDAHGC3A6+1YQpuyclYygk2i9calZ20cEEKRbmTzJ5Pm3Zwcgk8HoCMetYs92Zw00UflgpiPnn8ag1S9jtF2zSrMxA4UcisZ9bYxFEjC56EV2U8BKUufb1LdO8rnUiF5NNzLjzGy34VhyWqrkheTW1ZazaalYRhHAnChXTuMVBLB82RnBrt5eT3TuSSSSMIw4cgCpI42LAdq0pLLB3gVXChXxipCxas4iSAB0rrLOxF4jWzjiWMx49zWHpUW9lyO9dtpsQS4AAHanYaPIIrPVNE1KS8jt3KxEg+o7HI9K6HTtbstV2yxCKC8UZCso2MR047Vp+Ib+3uvEtyto/yK+wkHgkj5v1zXISafa3+oIkhbT5I8iSZFyrj1x2NVWwinFS6mDerTOs0
G4msYne+OLqMESQ7eMk5B/TPFSXGqtPdbPOB2nkH+LI9/wCVZsN8kFuLOOR7kk/66QY46cVPc38CWwRUUHO5gFHJxjJ/KsFl0qj5p6C5tTP8S6FHrF+BpLIZ7aFHuskCPkn5qox6jqvkwRwysHtWwzw8gZzx9MA8Vdh1QxTOzY2uNpUDG4eh9qWK4tUF0BDsW6IMwDY3Yzj6dTW/1KSiop3t3EpMuHbDH5hXJ7DsKoXN5JdsCWZWQYGO1FFdlKjTgvdRlCKRQmd5onV2YlRkHNU4tYvLMPB5rGOQYIz2oorRxT0aLRWuFM58wNwOxqlIxC0UUmCK294ZBIjFWHIIPNbVr4qvYUCzKk2O54NFFRKKe5qmy4njEMcS2fH+y1OGt2srgiCQbue1FFYyhFMtNmxaa/a2yA+TKSPQD/Gpb/x1NLG8FhC0HmDBkY/MB7UUU6cU5BJtIxbAkSbyScc/jV/UIBPFHdr8rkYNFFdiOZvUhtxtLP3AqvczHOO2aKKRSIWAwCeoo3k4PNFFAmf/2Yi5BBMBAgAjAhsDBgsJCAcDAgQVAggDBBYCAwECHgECF4AFAkh1R/QCGQEACgkQixxJKEKxBXjqxQP/fHgv8fDBMNMXWGaWv1sooneOmuwJcedOvHPQHbi8TvjMLDS8bSI8f5wzKG+v2gIx4nr/Jn360+aQnPEzWeCZSuoyHH+hubSkpxJe4jaqfGJCxZgh/VDgWRf7fNA1ff4eFaucOTN2bnzKa5N1ZKb9Yw7KVJcRb1388Xu08IHMV1O0K01ycy4gVGVzdGVyICh0aGUgd2lmZSkgPHRlc3RiYWJlQGdtYWlsLmNvbT6ItgQTAQIAIAUCSHCgDwIbAwYLCQgHAwIEFQIIAwQWAgMBAh4BAheAAAoJEIscSShCsQV4pPUD/idOQxDRZmwlGfvueiVEH0jccQAzp+rXc0Y0DRwMySZX
FlRKdElOI3mYivbYP/8bfgrhae7qvTtpvbFiwJwZ58Kzim81qX+DA4H8wGAYa2d7bwVYFZKPxzCOEdFqIUm7htfgKQ0Xyj2Mq//a3MpBKsdWpCt/IIC6FIQABCWiVVlj0dom2iQBEAABAQAAAAAAAAAAAAAAAP/Y/+AAEEpGSUYAAQEBAGAAYAAA/9sAQwAIBgYHBgUIBwcHCQkICgwUDQwLCwwZEhMPFB0aHx4dGhwcICQuJyAiLCMcHCg3KSwwMTQ0NB8nOT04MjwuMzQy/9sAQwEJCQkMCwwYDQ0YMiEcITIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIy/8AAEQgAqgCQAwEiAAIRAQMRAf/EAB8AAAEFAQEBAQEBAAAAAAAAAAABAgMEBQYHCAkKC//EALUQAAIBAwMCBAMFBQQEAAABfQECAwAEEQUSITFBBhNRYQcicRQygZGhCCNCscEVUtHwJDNicoIJChYXGBkaJSYnKCkqNDU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6g4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2drh4uPk5ebn6Onq8fLz9PX29/j5+v/EAB8BAAMBAQEBAQEBAQEAAAAAAAABAgMEBQYHCAkKC//EALURAAIBAgQEAwQHBQQEAAECdwABAgMRBAUhMQYSQVEHYXETIjKBCBRCkaGxwQkjM1LwFWJy0QoWJDThJfEXGBkaJicoKSo1Njc4OTpDREVGR0hJSlNUVVZXWFlaY2RlZmdoaWpzdHV2d3h5eoKDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uLj5OXm5+jp6vLz
9PX29/j5+v/aAAwDAQACEQMRAD8AnkECS+SPI3quQfLGMdMg4/D2qfULq8GlRO2nWd35bAMyxgsPRgV6+/FUvDZm1SG7kmjSC0ikZFQAnLDjPufU1MkE3h+1OoRxxT7UMSyljsBzgbsDPFebB2VmjGa1uMg1Kz1ljZXmnxxSlggKgK2M4BU459xUFxotlDdRSRXiSEgKI2hUMcdPqahtpLlpINQmZJGjn82RldW4zzgjtTrLVEvLq5CIsbiQyKvqOzD0PqPxFZwjaT5WTY57xHo6xz4WEiNjuR1XKq3cEjpVdFjmQb4FVSVbeiDBPTB9O3511fm21upmW1uwhPzPA+5TjqCGz69DUV1Bos1qHhWWCXhgCm0Z+g9q2UvaKy3Gcu9rFAFa4hXyY87Nyj5iDkk1PGVM07+TtdtoREXHH+NWr8SmIB2jeKHEudvJ/wBn39alu+dAt72QBZ5rgkkDpjj/AArRqzDQgjIWFw9vEpjbDJsByp9/atW10bzraG90+e1kdl+aOReG9QRWTbTJG8jSscKVDjHX1H0wc/hWxFGsEAlguAkLcp8hYn8u9C5k/dYNHP6tYfZ7khgsYLH91syVP1OMj0qvDCVYuVQBeT8vUYrX1LUvlG26uG9UZcqfcE8/hWbFKWJkULgnHJwBXbTlPl94roPhkU7H+zgjoMgD+lDW8ZO/yUHqpA609YnaWPKsytxlRgCo7oMkgTBXA79T9atEDZPv5CJn/cHShCu4Dy0I6fMmRyKrmXkcnAHX+lKk2+TEhXGCVPU/hTsNAzIHAMSpk4IKgU8xxPcBFREUDPKjBqchJVVlG9u+84qvNJgZCAMD99T/AEoBnXJrjWFzptgMSCMItxsA+eQ4z+I6/Wreo6xPoniXzLf97BNH+9t2+4w5z+Nc1q9q0F1vhDMk7B4icHafTPqDSa9qzyXtrIgAzAAc9mBOa8ySaY2n
ex0sul2t7JBc6M4SxmcuYz/yybHzKfaqENlp4vJZIbhZ4du0OR8wPYg+mR+lZei6/Ha6FcQzJtmlkLuf4dvAAH15/KrOn2UV6gvRujg2gIqg73x0AHfP6USXUNjatZtLdQYoWlZf9YQxUE5/iB4JqxeaTDeRyXCrGqOchRLz9VB/lVMW2yKO2jGWjc+Yo5+Qj1HoacEiltg6MsUkfXHRqUIPcm12Zk+nvCuyaQGIcjjk+1U5Ht/OiWeRh28sc8E1r3YRW3Rqry7eNwHX61kfYSbh7pkEkknJ+fGB04+ldLpzZfs5FzXdGh06GO3BInJ2ux6EE5B/lUdkksFrPbyb8EYeNxkD0Ze4IqW/NxexwSSymcQxCNweHKgnr7jNWbiNVtoTIcOi4Vm7/wCyfqO9c7i1NITTjuY9xbldrPIH469/xqoYTuRiCy44I4GakdZTI3lozxjv/dHvTknAXIU89M9K74ppaibGrDNHIMyAK3cnp/hTZIEmjdo+CDycEgD60ozIHVmcnPU0YkVG2fICclie9aWEVPsKM+BL8o9QKaoe1I8lllwTkbRUhuCoOUDv03MM5FRx3IDuVt15yByetPUYt07NJtdU3D7xz09hUUYVwI88kkL0H50XCSqV8wYVhkYOf8miGKMcS9T0IoA7W1FhPcPaXMJuLLeTExPzxejDFQa74NuWge7s8XFoBujdG3OGJ5B9Qf0qPSWjWURSnaBgHPc11FrLNYiQxOqwkYYOcrj+VczgnudMopnmEmkvDpwnmkxM0nlR2zKQSPX6Zroo7dri0iCyCB4UADYIyfT2B5Ga6G6mW6voopBbygtmN5EwYc+4/hzVS6sZLe6aGWLyXYZO07l/D1rnlFxl3MJxaKVhqTWGpxXF1H8kZw2PvkYxnPcVWv8AWobvUJ7iGJIoi3yqoxn3Puai1a7Sxsru3DnzFiAB6jJPT8q5PSZX
vdXtrYnKyPz9OtaU7K8jSjFM7SVZre2truU4W5QyJjsM4qJrgRttz1G5DWvFYrd+FrGK/JRrYSRYHUqScY/Srln4PW+0SJbqSO3uEbMLlwGI9CKpVmzu9ikc356ZWaE4bGSvqKuRXhuLdlQjdIBzszgj1/Dis7xBoOpeHFV50YRFyEkH3T3FV9NuPNm2qcGTkZOADQ2nqYVYXjYuyRFMKqKiHuorPu3EG7AZvc9BVy6WcuQNrpn7275fwFU5LYXEUs/lMgjUZZz19K2icNiO2lXzMArkrlscgf8A16kZTvMUjHaTkdwKr29qqAszspx36H2o+0eSsmJcOB93OeK0AXaxB2wEop5cA5ApTbyyW/n20ZCKwDbyOfeo4tWukfhsqMYUnjn+dasdxM1okbZ8pugAGFY9D+GKGMo3cC22yaZSxYYAX19fpVS7WOOOKQS7nIBEYAwAfetC4VownnFWCklgTnGPf8qozR/aJmEg2Z+6VGT+lCA12upNmwRkMD97FWLPUrhY5bdwzo6bSu/7vHaqjStLGQnJ7A9RUF2Z4BFdwKQoBEqnuAa5zqsaSgz3Eql2BhhBBDYDEDkfjzWnp2sfaY7azuoZQApKzYyT3PPYYrCsLv7XqAmlAAbKnA6cdq6CGSO0snmEQdwNqqeTz2HoKX2dRStY5/xXZP5GoTR/vY2CkP1wox3rB8HWqvqL3klytuIB99hnk+g9a9Cmto9V0srGhR2Qh42OMZHr3rz/AMIiC28TyWN5wrZA9nU8f1rlpzclJDw1rpHq/hi3j1oNDJLI8TLuikaMqx98HpV/TfDUWla213O8c8qjbvblsZzjB6Vmy6k+kNFLpiB2Xggn16mnLqL6hE091evPfjIEdvF0GeASOKaa6Hpcre52mu2kHiLw9dadIikyxkIT/C3Y/nXz75U2nX/kygiSJyjj3Br2fTr17dVJdjxyG6iv
I/Ed3FeeMLx4gNjy9fU+taRlcwnCyLT6kCSo6dGG3P4Yq3fxRRRR2kQCFsSzbVyN2OB+H9a6jXLfSxDbPFaW6uUAchtu7GOT/jWbeRaO8vkx3VzaTFdxR03gnuN3XrWqmpSt2PLktTj5vMJZFU/N36Zqp9lMQJkXI7kHg+la12YRdmMOUYHHmypjPYd8Gqd1dJC4WMgkDBCNlfrg10KQiss0MSNDNENrchtvzKfrWlZxyvZvBbyKH2hkMgwXOen5GsJrpssoIKk9SOa09OuncnznDlV+Qdx+NVJANvw7RIN2fm+Zs9CO1Ei405GjVc52udvJ9K1XtAbMhWVi0gZV29T71E9u0MGJFCGRSVKqD+lSn0AdiNGV4lzuBJPH5VYtHnd3VsOiqSRjO5TwR+VE9m2nXK292siSEcKMHg8g/SpFtxG5cuAM8KFyKwszruVUsm027QsInhbMkLZIDA9j7itHTHuJ9sIYCIKNy4H3g3P+femIkMsItZZT5cjZQgAhW7H1FQ3WmSCHfESlxGzOBGpZZOnQ44Ixkg0pfDoTJaGpcwXyvy25YRjITIAPYgV5r4ki+x+IWe1LRnIdcnlW+teoyXs7La3SZUygAHbkHjnv+lcdr+kjU7u6nXaJA2FVemMf5NctNqE2uhjT0Zd0jXbfV7MI0pjnxtkQHBB9vauw0WSCxCs9xtiXpGorwyWNoJmBLJKOQVODmuv0C/ubrTwrXDO8eAx7kVVSHKuZHqUa7l7rPUdS1qO7haCzTk/efHSvO9Z0o2moRXPOxj83tXS6OxVGXHXnNT6tbC5sSAMkDNZweprJK1ijqt8n2aKaRhv+z7EQDI9yf049q569uJbi0s5Gy7eXsY98g12nhFrW9V7G9soLuI8bJV+Yeu1u1afiPwV4ftbWG4tbia0ZjhLdvm+uK7KaSl5nmVKLvdHkd9NNLPvmQqwGMdamaxke
yFzA3m4XLr3XA647it+78PSxRSbYzcgfxq2T+XWqc9wdOt1Gxo5nGx9vRvrXTd9jBqxzYjIIz949quW8/kzxs42lD2Hardxpvl2ou0ZDA5AXkbvy+tQfYma1M7MQBjC55I9fpTbTEaMcrx7UjkDK3z5PUg9KpyXlxFL5cAweec5BqaGIrbxeWwEjR8AkDjnJz+NZqs29gQxP96kkGh6brMLXumxXkSo0tudj8ZO09PyrDUGJNzuFB7Z5Fb0EskDNkI8Z+V+vIPWsfU7IWc6hd5tn+aOTHP8Aut7isTqsZ0oaOH7QQAGPHc+1SrqV/ZWTvFIQ7LkjFI48+NVQ87hnGeQO1XF0651ErAik+YQqovp7mpbHHc2dAuG16M6ffG1I8oSrEsGGwfcHjn0rm9Tu7fSZLq2uY9/kyERf3gf6g+hrqJJLfwnoEhgRVu3B3P3HtmvIrzUJ9SkluZ5d7s5JJ6msadJT3NsSovVGfq04u7wy7cEjpW94XtJYZy6ktvQblrnlTfcISPlLAV7VoXhxNOsUG0M7DcT606ukbIVFK9ylZwzgZReK6K0015LdpXxs2n+VX7WFVTa0aj6VDqrTQ6fJFE23zfkHsO5/LNYRjZnU5XMHwGsaas9w7ALvIXHesrxn4jfU/EVwYHIgi/dRjPYd/wATmr1rInhzw1LfsQLi4yluD6nv+A5rg3cHJZsuWruw6veRyV3b3TSttUktnDmU5Byea1V1G01oGK4RMvxnGD9c1w8kvJXP3m/Sr1jK0cqnJA7Edq6rnKatxmK1lsYgqpGSHUjLn8ay4mmSYlMk7du1sYx6Vr3sbXF1BKxBjkUFs9AR/Kp5rO0vYVJcwlW2l8fe+vp9aybs7GWl7GTPBcXCQxQRn91Gm5lGdoPXPf8A/VTmtbeCGT7WZmkQELgbQD/X/wCvWw+loLjzxJICgUAL0PHY96ivzaIjRugK
seVZiDU812DN2Wd9xRF2Rjn5Twas21zEsRimXzkkxvXPPs1Zl+WYnaDGrdCR1FMhYx7VI4GDtPeufmtI7baFu50l7RvNtf3kR5GB8wH4VNp2vWml3ls17cRxKdyh2PCnH/16mtdQEDnkGM8EZ6fSuc1u10/V9WtI9hKO5DKeApq3axMb3Dxxr+l3toy29/FKc9EbOa4GKMfZI/Vua6zX/CFlZWkjRRgEKTkCuVhfymKSKRgAjPpjrSpOLvYuqpJq5NDbbjvx04WvUPC3jO1jsorLVi8bxAKswGQw7Zx/OuCt1WRFKjg81et9OnvJlitY2eUnAC10SpxkrMyhOSl7p7Paz2V5F51rcxSpjqjA1zviPXbK1jaN5RJMw2iOM5YDv9K5G70K80eW3jvMRvMOXib7n1Aq7e+EktbA3lvqtvenPKR5BH51h7GF7NnVeq1dIw9T1O41W4WScBI412QxL0jX0+tY93+55I49avyMAcDms28vvLBQhWzwQwrrSSVkcTbbuzJM4adR1IGAK6TS9Kmuiu9SSeQM4rG8PWSXviJUncbcF+BjNet2WnRwgGMVxV67i+WJ1UaCkuaRzN9pdzo9pHO8a+S2cPDncp9DzyMUlnfkqG2sznkOExkfSut8SAR+H0kddwWYfrmuFYXck8flhmRWDEgYC89/TinSnKcbs58RBRqWRryyPJM7R5AUY4xuBHes66K3KCWYifL4ORhlH1qRoryaecw2UrkOdpWIn+lQh2hfF1bmMbd3zqRz061oc7ZpS8TgMCBjpj9ajQl5HCttQcDFOkV+oXcR3PUetNiiYRuxw3OevSuax3k0cbReZHHjC5OGOeKoz5FzaSSRMjxNnkcnJ6mtK2j+0EeXxz355+lZOrXEkWozhAkkCyKpOeUbAyM/5FVO/JoZSdmjodcj8616Z3LXOyaVDPoMUrRqXWPGceldZNH9p0tW
HXbmsm1AbSJ4cfNG7A/Q8/1rmW56Nrqxx9mdzpEpAPcnsK9H8LWdjc2yi21K2imJIVDzIxHfGRj2ryd5WhvZYQ23c2Af6GuiOlpaaH9seXE0h2wrk7mbPX2HvXoucWrNXOajCUbyg7HpDeGJ9RuJYptQtR5X32R97KPofu/jWPq2nDQ0Ekd4tzbM21mDKXGehwOoritP067kukt4ZWaeZguSThj7+o+tT6uk9nfGyjuTI6/KSOx7/lWEp02nyr5nfCNeMl7WevYS/EJilu4JV8vPzAcYPpWBcSRyckAmq9/fzO/2VW/dRk4UdCe5qp5jZxuwfeumEny6nl4jl9o+UsaVOLbxDaOrcF9p47Gva7FiYxjnivCI2kN/AeMrIDx9a940kq9tGfUCuLFLVM3wz0aL+oWkd9pTQytIqhg2Y13Nke1Z9opttNeCa1eNIjvUErEu0f3s8sfWt1VkMJESqz/wh+mfes2bQri7lZ5pQ5YlicfL+dRTnKMbRVzlxuk0c5qF3HehN15cBm4CQyfIPwxzTRcaPLbLBcXWo715D5UqG9NtaV7pNtZeYsrcouSFiOOTgYOOapvDavbNjT5CYukj5H4EY4q1KSd5o4tikU3K3zkZGAev4VWYukRTHyP75q2xLAK5IPXAHSpYhEZAZG+SMFz8uM1oegFrm1tS0zKueg7k+1ckhjuNSvLg3Ox2ySjkbJOePwp2tavMssrsDuHyoc8DPQVzi4mVbY5LkblbPf0pS2MajPUdGu/NsFRhhguMZziq8UYS/vI84DxhgPpxXL+HL+S3RoHJyDuA/pW5JqMMcj3DHnyigFczVmelTnzQTOOniVvEGCMoZQK6LWnWfX0WFv8AR7aIBV7Dj/8AXWFeRtDBb6iwHz3J4zzgCtaWaO8vJbqFMJIQQSADwO4ronK1MvBwU6tuxv8Ahp47VrvUZjhbeI7D0+Y1yd3f
sbm7uIoyQARvxjnqT+ZrrrqS107wiR5waafDtg9uvT8hXC3F6r2XkoOGJaVsdyc4FUqb5VFGtXELmlN/L5f8ExcMXLucseSaJBu6ihnDscU9AXGCDxXXY8W4y1jb7bEPVwP1r2bR5Tb5tnYlomKHIx0ryC1G29iZc/eGMeteqAznVd8gYSPsLjHU4xn9K5cUlZHRhpWnY7ZXkGnyyxY8xELLnpnFYsGo6pLA02owzRRjGfsowce/t+Fb0KmOwkJXJ2E4PfiuSl8SLPGqTQSRun3XTJGPT1rGk7JkY3dM0LbULC2Vp31WZmPSOQ5Q+2emao33iWCTfcpaSXSr8oBXKqfw6fjWVeSwyRs3m7XYZHOD+XSsyK1kt5RLG8katww3AZHvWl+iOGxqRAIGwwIxnr+n1pu8/Y555AEyQBjoRmlmTbEUDttzgkj9KdqEW7TNqIMLjHzdqcdzvkcD4hYxxK4U4kc449P/ANdZVpuM8XQAHG49a7HxLaQHw6JZFAkilXacdecH+f6VxMMxVy4Ucc4/Sqa0MZbnQQ3FvBdQyZ25QZB75PrVu+ubdukmBnFYVxcJcQQGPb8uUI9RwQams1N3asC2JoTuKnjevt7isXBblU60oLlRd1a6W40+2tgoDxM35HpVXStQjtZPJuX2xkYDkZAouXWSclAcY9c5NVnhWQFWHXpXTCkpU0mXRrzpT54mtfTwS4EMuYscnGKxJ3M7bEzsHpQmYG2Sq0kPoDyK1Et7XywY+hraEFGNkTVqyqSc5bmdHbqcALTmUN8idB1NWpWwPLiH40xYwgwO9VYzI7cGC5hlUco4YA98GvV9HuV1LUXnRcR7V2oTkqPQ15c6kD0xXoHgK+W+t2xw8ICNx3rlxUVa5tQX7xM9DRwUK+2K5F4baNpmuI2ZV65ZWBAPoORXTRk7ua4q/ugt5PKYZVKsyb4WCEnnHJ/l3rlp
vcrGLRMZcQ2jXDxSR2sUZwf3LjcCf7xJH5UkWlkN5JtxDE5x9oeXcp98jp+NUdst7py3Mcai4jbZuaIEnPIB45+tO1BEOlsZtQ5jG503lVbnlBjj8uBWkNdTgLqlbuJ1WFklAyCRwaL1VawBQEP/ABY54AqS5JDIQSDuH9KntiTaXak5Abgf8BrSK1O6TOL8ZXAmtbOzjwVIMuc9+n9TXHpHshVSclyAB6DNb/ign+1oeegAH/fArFh+/L7S8UNnPJ6lrT9N+3SNAsiIwPykjrg+1XZdPmht5/LVmkhIDjoVXPUGn2hKavEVOP3y9K1NaZlu70BiAUIwD7VK+JIi+pzr7lOTmkyCeualjOYhnn61HKAFyAAa7rFibNw6imrPsJXNVWY4PJp0XQUMDQR1K8D65qaNSwz/ACFUoydw571eB+ZR2poAkjO3BKius+GgjF3fxoDkBWPH1rC2qFXCgcdhXW+CEVdZvNqgZt0JwPc1z4h3gzeh8aO5A2gselcFBqNtPqEkl3vcyFkR0T5UUdvXNd7L/qJP90/yrye1JWWLHHPb61x0upWM2R01teWGnwyR26NMJQM7kJdj9WrGeP7a8YhiMVvyVUYKlwSNx9R/WtK4lkFhqJEjgrCSCGPHWtbUraBbe3xBGMRqBhBwNorRXszg6H//2Yi2BBMBAgAgBQJIdUFfAhsDBgsJCAcDAgQVAggDBBYCAwECHgECF4AACgkQixxJKEKxBXjb7gQAmRVDoD4V5ULie50WcU2UUHEdkZX4RVgxMJ0CaIFNUzPjtb4TrXyRV5PatT9l4XKGKq55a0Qd+UAmOZFVduhOH7QT+lcwznrCrZ3VJso7Fe9x4aBCAcCTEyuw0HLDiivuQ+cUj+y1QQkpS6qyyVTFusTp3WI0/2mwFsg9v7ev4umZAaIESD4qixEEAKZtpukRxasBH3jd46CBGmszXgwC
Ft5dD4iGJQdk/s7sEvRTgPNLyfvDelIK1WzF6ywabTtY0VrkAe8uZ/OZI/I0EqeCXvVUaPHA8ueJ/hep7Ooiyh95zmw2mMGyqmKPlqSy18HVMf87+4l/E9SaPFcFg8YEAyCURwUGUL02nqszAKDNenhZGnlySjGvgDLP08m8cm7J2QP/TV5QL599m2G4wZE1LspplwSVt3oryAAuXeIieOZnq5OUbaZwGAhl0qH1EyO3NiisdB/t/ZcvJgyWkJZTOCwj6qAbLSssqPNqNZeiOIeuYr7mnSYESfOhmWL3kAWfWeIFGLvnzznCaTKireDGt2w73hYbf5XoHOxECbTIsFwfbfED/RlOyXTKsyStqqquE+JlsBZ+FmYiAykBfDzqTptLyYb/KkxxflKWqCi6Qfpx8GqcvEKgpnzSYMjNvXLMAv7eTLZrOtVLVRkGGSPerBqb+vuLJQtVfI+KkbNReQWOGlhrRtDEqFbDGjTM4pHlph8IjPmYnMkI2okS+3wMzHLvAKZTtB5idS1jaGFuIDxtYXJpa29idUBob3RtYWlsLmNvbT6IYAQTEQIAIAUCSD4qiwIbAwYLCQgHAwIEFQIIAwQWAgMBAh4BAheAAAoJEOBw2ZmRjCm7sz0An17qh/mWpoZ+O2g++guS5HgO/5DuAKCliqdE/9h8c2MBLIwBMsg/SqZknLkCDQRIPiqLEAgA5VZFq0n74+sqGMvaWpT6fBZJzuNjWQg4HCcAVRHDrfvs6GOXjusOp86XZnFhO9LFciuK+Y43kL8FjDm6mAuBFwVcBzFCeHJ45P5I/mEgq9Z6/CMVYl7zKsAaVGu0E4LmLhrFFpr4HlszLvBod+m89MZNM//fGuR1T+gvn7Uzaipl7cmCJzF58F5prD1aHETgdeWkA0bpB7uv/n5NOix1h/UYJhTdLgRudsChvRNHbxTRKqPOieoWYo/9s6I+
ZpCwLDp6mlIsdrANtT6nxdk14n8Nr0N/F2UAaGcFbgeZI8miy5EGtaa0mfptQyOvdxOtM/euAQP1wdlNganCIa692wADBggAvCyeHiOlO398aWc2VKwc7VPGlHIToaUA5RKFrwUZwGltyiEjoidH8sDsdI+XrPZDRlSBQPzm2MPp2Hv5v0H1nDgSUbRIwJWDULfIjnaUTC/TYsgGkMjw2ub3B3Qf2qNTbRzAKE2PXbPmJvK5M1x1htx3RAPgZRC0nhgRE3eg5sj7Y9TQTIjRJTQ2K7+CxK1FHTp8i8CN6WxjKIj2EbjKHihl6CKHCPmcyIE9h/h6bSDDmOJvSA4ECoL6OJjwCg/oSCZ8/NSQD4GqEPJ+Go1Mr8M0UlM8lmI3+3v/Vkk/J0PFNp0Rjyqaccf25rGhmBK+LU3dIrZgiQr9QExWAtbMOYhJBBgRAgAJBQJIPiqLAhsMAAoJEOBw2ZmRjCm7coYAn0sVu5N2ZRVjkhwoFLyNnCQeD2JiAKC292ICT0O52ToDHsKNucakbPQuS5kBogQ6owWHEQQAwKS2J3fCgF+K4hCq3w+9jU8mA5RFh83VUGcxb2LpV4Hkp0rHlDA8kRy4zOxbfjf149ePr12rZZwdBZfCFN0ze3Xakxt6OfxNYVrBnU5ZeGUqXR0TdPHjNqDrWqxntQcZkIoNwTy1k15CCilY2duYqyCJYVtpKb05Rh5mY8I9pFsAoKNaYFts8yt7S5w3rKSeJuWa9yWJBAC6VIPp0Vl1sgrOT86lUwp2pu+YfHaUvzDGp9u+B/pw5zghl/wYV2xB57eaIAl60VHaAq93AHFMo48OV2GD2Ah63rIPzeI/51DVbMU290ijPMMERW+a/8ayCMt/9uwEuNhIN7aK0yaS5WRuSKLRgYJQR3S7hLT7H0amy0sTut3UbAP/QaGlFS5HDNalfq9/Ds/PfnqQeHnY+iq5
GuJILFm0VAwl6QOzSWVsA4GrT3nxIFYY39NcJZPr5ZhkKMEncmkQhS9pNoBeyg9AAAmL9aDZyN4SAB7uhdA7hXhPGV5vpxrkfHz9DBj2sNYhO3sWe3S9jrMJfSxxetBz4THgbNR42/20MFJpY2hhcmQgU3RhbGxtYW4gKENoaWVmIEdOVWlzYW5jZSkgPHJtc0BnbnUub3JnPohGBBARAgAGBQI61i2CAAoJEGhJbiEFsN/OnoQAn2ybXSOmE+cbCS5uSMGK3arXgL1PAKCMPcLy4g7jQUO5hg/whlCqIFwSTohGBBARAgAGBQI61i8zAAoJEOd14yTbQbOHTyoAnitmHZhYNyeNpAztYMDigqOUodkcAJ48GGDy63vk/sI2febaL34AqsgbjIhGBBARAgAGBQI7bqHlAAoJEBcp9YZqnzw4nOIAoIkXMOm3nHbkU8Nn4C/ZXUHz+9UJAJ4igyT0sJN9zMt+5rq4wC7jAnwzp4hGBBARAgAGBQI7kapAAAoJEFawMV8BZ8o4jXsAoJZ2hw8QmmO8sKVjHEPRvwUVxcapAJ9msY2dUe2I+L833zO9HbZiNAQWCIhGBBARAgAGBQI7+IVnAAoJEOTOYZnQmAqZIBIAnjSrQC6yR/lcONV4VEAG+kLr1Ua8AJ4p0QipNN8tMUZCDXzSkogFRI8XLohGBBARAgAGBQI7/EOxAAoJEDv2CcaLr829nqQAn06ROhnySoLbVGYTaQJH/kcrLp6CAKCvG5lzc2Kft72ZRMvlK997Hv9ZlohGBBARAgAGBQI7/Lf2AAoJENcNX1hgPNB4LJYAn0oxmUKadOm419WMs3hTCRDrItT8AJ9t+J8qpc/IXWHmGnKLZUFbkNfw0IhGBBARAgAGBQI7/iRgAAoJEPHSzMhJehdtSn8AoIvF/b9NMYzO+fAJIV08nwkFmtbIAKC+p8/azsNPK8Lv
wnzrqau1N2nRqYhGBBARAgAGBQI8ATgrAAoJEOdTjsAhk5FpKUQAnj+mpmY1pqpN5An3Lfb5/Fkl5R1ZAKCeUhzlIfSbimGrM0+tlVM5mhFcTohGBBARAgAGBQI8WZAwAAoJEPIZh0PGQoNCHrQAnikLTH3o889LdvGKTQpZ1S64N5cPAKCCgZPbqlX7Ho3BuZOBOKyD0GQ5x4hGBBARAgAGBQI9LHgjAAoJEDXFFDboSm5pKFQAnj93WykajCWA4VOXaejDnYR/D1N5AJ9HVujBHd7CujTJRQbXDOFraziHyYhGBBARAgAGBQI9LZNtAAoJEBjNJaUi84rztO4AnjXpwVURJ0xyIQEgaAj/T1MEjoMMAJ9Lcsn+uKq5efcVlM1DwmTI3KucW4hGBBARAgAGBQI9LZSYAAoJENGj7q+v0QrP/kEAmQGCAeaCKgcX/mej7f0hxNpFJ6ziAKCl2ay5iyslfXv1TKeTWxU83eGieYhGBBARAgAGBQI9LbefAAoJEHm3lmgEJh90lhAAniTvTWqPhk1wZBxRoqTIP3px/x7oAJ0RJI9ZH9QLZDEkNJOtrFvIwDP6WohGBBARAgAGBQI9MtEMAAoJEJUzdHX4v2Q6xWcAni5/ILP4acpSYj5Dn5Xcw8ijZHEhAJ93ez1ilWr9YPZ64q1OtOP2PgmfSIhGBBARAgAGBQI9Mtf2AAoJEAQiibOX/jz99r4AnjXHvXrUQtX+2LClJhBiry4NsDgIAKCDhU9E/qI/TeM+4R0oK0SvXyZ+J4hGBBARAgAGBQI9VTiQAAoJEOj6GxFCl1r3NxoAoL5vhuAxvia6ov1nFBjiriHHIBG7AKCrENFFCrX7hMC0zkYjGTRGaZaQDIhGBBARAgAGBQI9jNqpAAoJENBlk7NU+gyIMygAoIm5Vp/s7k2QzFcLprv931wILhUJAKCMzxOe50dZRyO/
CTb3irdhO3QK8IhGBBARAgAGBQI9uDY3AAoJENtaslySt7CDGOIAoMRUucC7e3WX+QP7DDlRjFJIdkEeAJ9qHN9GGqvvH0yc0uTiM1Kb1+3MSYhGBBARAgAGBQI+c9pNAAoJEK0k/pjoZFO4md8AnR4Xlh/4kkm1zal0Wcl4NE0FqwpdAKCfProDVN6XU7aNimZ5wk83jvIWSYhGBBARAgAGBQI+mJv1AAoJENIMKN29ZTz5llYAoMeOwmumK+oDYKZqaIPR6vDQi/w1AKDIN6cqaIX5MzQ+NWgb7jRj6wP4/YhGBBARAgAGBQI/icCWAAoJEPCpikxM1uPS7d4AoJUcsFxdm8soLjn6QbyxlqqlIIRZAJ9ZImf0Y/hqC3kftB7aZGVt9PjrO4hGBBARAgAGBQJAP3OoAAoJEJfZnjo7wHURFDcAn0XbgcNg4U407jk5UruVVinKBycXAJ45D+w2xlKNrPzZ4tyVkMfoaECGTohGBBARAgAGBQJBUnumAAoJEGbHVllRYY6IB/MAoN9lgz/T1SZNAwMkT+y+l7riF++vAJ9ebgMaZ8dvKR+hznc4DLk8YdlssYhGBBARAgAGBQJCAqEIAAoJENEk1xR8VLnM0qgAn00736fANe5ILTpJf639wbTmLvCsAJ9fQiZZBXMTZCh6ccc2nlBHPHO4XohGBBARAgAGBQJCHCUIAAoJEDOxg1+a8NUlCqYAn1/ZenOXNOAZKxBuv8+XDwXA6AfkAKDVRQxPbfeDAhnghWPQ6e3Ed+U8hIhGBBARAgAGBQJCKXINAAoJEJYXLWbruFlpbYoAoLvmh2sKBZ7WGo8lq2EsqDrNac3lAJ0WhBofYKdSXb5ahzHBgYhCZcLs9ohGBBARAgAGBQJCQrB0AAoJEMZT9h3b5Nnc2voAoMd7nYvAoAj3i+0BHJ8yEuouugVmAJ9GXUpgzSZAPe7m
MAfuLjDJJm7Y5ohGBBARAgAGBQJCQxmoAAoJEEiFWHjKgx1ln5oAn00AG2MLKjS8pJfZ0ls0brEfTRIZAJ49juMISZXPOSrqevw1pqmcwSA6T4hGBBARAgAGBQJDHq1cAAoJEFmgVrcf6Zdgg3QAnjTG7N5s2LDTFqH3taV9dIcuErR5AJ9rWhF1GAz9uA7l7KHZ5llAPwcwtohGBBARAgAGBQJFTm8nAAoJEGkAypseAo6lp8sAn2iXCaZ0Wr+OCUUlKKCFdikM33nMAJ0e6QKbE5iWJPH19FV5m1frKVsxOIhGBBARAgAGBQJF2FM0AAoJEJD7iwq77zktKngAoKVezYpfc83tXMBQgpzSP4hJVlzWAKDOCyipiNGkS97H4BEzEvHX5TgEjIhGBBARAgAGBQJGAdpwAAoJEJVG4JVSjrMmgxkAnRPzQXwkwHUDO4sIKMijIdgyO4tlAJ4oNd/JDWqiIY0NM3j+7dhZvxpX5IhGBBARAgAGBQJGoOjOAAoJENan4jkvAD8ERvEAn2XyajU6G5J31E01rRO/2rc7jR3fAKCkHgYnfAhWRJzVtw46zoRAXT2Cg4hGBBARAgAGBQJHEmoHAAoJEAxZ8TbxnWa07LAAn3isGDm+reTu7JDncNeauI8bBEkrAJ0bTvF7zT/tkIZN56KTNzb6R74Z5IhGBBARAgAGBQJHMY3HAAoJEOnk/bZXlwrVSpgAniBc/plY1YPcwcc1eRj5PMCmhj3sAJ9Y7QFI2wyYHf+tIn0DusTB1J6OQYhGBBARAgAGBQJHvo2PAAoJEOSiiqwxGCNC/nwAnA6UkaiJGLgUaRnGKJ9/u9JoEshvAJ0fpNhXvN86cZSVHHZrNIM96BbFnIhGBBARAgAGBQJHyChyAAoJEPG6TB7RyLqPyR0An11Zez6lJ0OVOHT68Z7fwFH19yfsAJ9kUXEgeF/nzil2
3Xurs3zuEH8P94hGBBARAgAGBQJHyCnGAAoJEBbSoYAwtGNTzB0AoNW1yxaGQjoQdiqBCXA/YxtBEcKLAKCC73TQ6lrG+8s6eFoxiz5bHiVt1YhGBBARAgAGBQJIF2wCAAoJEBZYiyhr7q+NcPYAoNMltdvOLO8gnk4dOQUnCVVeEDeiAKC49IuCXuMGy5wEVWCDNuYT9Y+bXYhGBBARAgAGBQJIJ5rSAAoJENRQSGzROTKm6McAoLzO08kSKQkUqmOAhDXujBI79MQMAJ4oYs9qSQRyYjuZMLu+X9Vq2VQLm4hGBBERAgAGBQJAGP26AAoJEGBFaju/pOzQXf8AnieFRAFWy5BYm3MiT6l1/fENarAFAJ9+42rbxd8A1yybSiKThPG1zN+1MohGBBERAgAGBQJB3GYAAAoJEBki3h3bFlY/2gMAn0EyB3uJaStPzzxODPNutQ2l9OGeAKCiwtoyxZsDQotioj6mIlPfH59dYYhGBBIRAgAGBQI+RQ5uAAoJEJh2iWGe0QG/MSIAn0kXD3Gd1PojICY/PZwtIqdOhWOmAKCB8c0aic4ea9tp87TayE/t1L4UaYhGBBIRAgAGBQI+d1MsAAoJENvD6/wz4/5WDeUAoMMYai9kFPdwoK5R2fGpaEEzkv3hAKCokB1ExekgnhWSkWrynKyhumJRxIhGBBIRAgAGBQI/hK4YAAoJEBEuzfLEo4I+RGIAn2zKTJVLj4nMklqy1/JtEU0dAy9RAKCPVIHzuwdI6XBhYU5P4rUt+oW4oohGBBIRAgAGBQJCI9BMAAoJEPQPuyj1pU7VXbQAniXEN5Nn0w4zKAFY+WMxFL7ytGCYAKCtknHAtrUEZjMWY2AYpqsyWRJzJYhGBBIRAgAGBQJCMIDlAAoJEJwR3YheOeRxsx4AoK0+J6wFWCQy9raQ/Cgs5HI3MaGtAKDePFSKIOO07Yg+
Xp+EFgBnvvR/BIhGBBIRAgAGBQJCQqVmAAoJENYT1fs1aki0WosAoOfR0828zup8cqq8W4ZtVrZF16gQAKDBIo/5OhEDAOIw/3zlS6jdCvRV5ohGBBIRAgAGBQJCoW3nAAoJEGFAN/jaWt18PZoAoNrVuPufIhDSdcN7kujJfNLEpkFaAKDFqy4oamnXMel38DuPK6fMh5HA54hGBBMRAgAGBQI9MwPhAAoJEIfWoN3aShEWk8wAmwVRwkfl9ET/qj31Tt3d5yQDc5rqAJ0Wp9xyuNP8BT4H/mekJG41qHvGsIhGBBMRAgAGBQI9NtE1AAoJEJDKh5AwTbyWmq0AmwQEV/ArGYH0s7n2mjDdu/HoOiAoAJ96so2p8/LUdBoqHim48O8+tn/xDIhGBBMRAgAGBQI9NtLVAAoJEDm2Yqiv44FpqVAAoOC8sx3HbfKE5hQev0ybl3A0BV0jAJ9v2GYZ3qp+46tRi8TFZMRvEJccb4hGBBMRAgAGBQI9O/3BAAoJECte2OeH8SO0tXQAnjtaJ3h7tP35jXA/nVTflU6w3BfZAKCHqrHDgAkguzfOV84YfpKGh1BgyIhGBBMRAgAGBQI9PIzNAAoJEF9fNO6guz3rMZEAoJhUsw5pywkA8Hk2a3lvrUcEX5gmAJsGuR7w0R89hsnSajAL7hw+/dUIM4hGBBMRAgAGBQI9n07JAAoJEHU392ZJUOqmC5gAn2N+9l2GQfgRMudnbW+lkxwZqu9gAJ9mL39maaJT533ivLBnCpBt8CTa6IhGBBMRAgAGBQI+IyQeAAoJEDHMMs73jz7kr5oAnj7pYBp9kCUbFaFqYTUKRMq3/6LxAJ42PcLjm7sLeQAowz1SpLKJKPUg5IhGBBMRAgAGBQI+RXavAAoJEG74r8KGV0rKy6YAnigR45t8lorjKlEUhmxqXi/AbYAOAKCvi5GYNK0QQ3nZ
BjtwIuwATKmqHYhGBBMRAgAGBQI+RZu+AAoJEAnizUlE5svN5iEAnjjG9jg8hljeik9MyQ5PSCcDQmGDAJ4qBWegit6G7WVB1Bk9h4p+EObEqIhGBBMRAgAGBQI+RZ2WAAoJECm+XSJo/VSf0nEAnj5n5SMfSbbX/fWesqBdPFBVqgLUAJ4zQVBUHWFTvtEDMOAOOFfsYzwQaIhGBBMRAgAGBQI+SZ+SAAoJEN56r26UwJx/twYAoNkrL5ykAVhOxtWEtWYDs3Cg9ZAoAKDrwGiFW529fYH9/SkN1lx7Ehvk1IhGBBMRAgAGBQI+d+AxAAoJEC4s9nt3lqYLuSMAoLzPWC6ZGVVTthU3Y4TF3pVeVhfHAKCvAc5x68Ti4JKLQ8gyvHFpmQjxuohGBBMRAgAGBQI/fyaWAAoJEC7nhs4bI/MfX/QAmgKhF0PnhTxJ9It6TGYAvIltCBYlAKCGFGN6r+Ny4ClP1GPKKDhrNnoS34hGBBMRAgAGBQJAJgEiAAoJEKOY4DdcC8/q2dAAniwioTFxdFt/AdvmQKQk9Z8THX/uAJ9xUEeskhxQoVkngXFEw9d+uQgi+IhGBBMRAgAGBQJAN/oJAAoJEBMwvm2kowYlLmsAnAiC6R1/N9Ikp0pYsSMsFXC0MePuAJ9fRguqRDCb0WkblXrkXlxb6BZUdIhGBBMRAgAGBQJAr0TmAAoJEHk8snsWRx8RFU0Anii1NalGde/492p8w6Jt7D/nzbSTAJ0eIU2AXl6ITc/MFR/iVv9CGVStNohGBBMRAgAGBQJBSgD5AAoJEJRPxqdqagu8yKsAn35sB6tsp3VdSKjAD3q5QFtXrwMMAJ99a9pQ4IG4MRow8aBiP89qmCmYk4hGBBMRAgAGBQJBTHKtAAoJEEDjVNn6JuLuhucAnjCKht7eLKcgMafip+V7x6wr8xysAJ9uXF5Ajv9WZ1Ce
r/juxazf1P5YtYhGBBMRAgAGBQJBUKcpAAoJEFZmf45SPfOpA2MAniDYzEG1gvh4RhfR4xPersc1fOd3AJ9xEHg2Qe/y7zoD+CmkzGNVZpYqRohGBBMRAgAGBQJCSNaXAAoJELZixPwqTRf+dFwAn0xLHbAvm66Wslt15Eh3QBfLwcdtAJ0ZBpi9tWWf84ujldzGJ+zumXp8tYhGBBMRAgAGBQJCoESaAAoJENknxzcUSQEmbuUAn0OWoYkK6DdL271t+AwcSyrlrKLNAJ4saMIoY75cCaaT6fFDa0mUjlXeF4hGBBMRAgAGBQJFDFtQAAoJEGkKdDpeA9cWT7oAoNFnUS9Fy+vexNlUF+Nvd9EKM7duAJ9SAfph4DSFON4t0DgOmoOrr0FqXIhKBBARAgAKBQJA8sMDAwUBPAAKCRDnkLK7wufXz6Z9AJ9f7szJ+PNg2G1IYf6s/jfd/aABhwCdH2OW0Ca2aEvT0SwIHts6OWexACyJAFUDBRA7kfeDdLNak/iIWAkBAVcaAgCYHF52n2Y6f6woTd4EzB8TkaDlWY6iAVdokBtRlv4gcgh3nFGWVZSdSircsSo2zlh2PM3iF91+2Nv5uy0OJHiIiFcEExECABcFAjqjBYcFCwcKAwQDFQMCAxYCAQIXgAAKCRBiTcVlE16maFSCAJ0ZFcINImDPampeQhtggDdoCYczFQCfZo8BLSWcSmgv4knv0swqCUK581mIXwQTEQIAFwUCOqMFhwULBwoDBAMVAwIDFgIBAheAABIJEGJNxWUTXqZoB2VHUEcAAQFUggCdGRXCDSJgz2pqXkIbYIA3aAmHMxUAn2aPAS0lnEpoL+JJ79LMKglCufNZiHwEEhECADwFAj51JIE1Gmh0dHA6Ly9hbml6ZS5vcmcvZGZjL2dwZy1wb2xpY3kvNjI0REM1NjUxMzVFQTY2OC5hc2MACgkQ
t5YHPclUH7Lv7ACgusqf7yD6qt+sq7MXT8YdkbAjKCAAni50SS67NQ1dGfElNCCsn08tdm+jiQCVAwUQPAE94bm/3a0nyBvJAQGHagQAjABp8tYWJYFSY4d2Re2B+7UxBURJh+BYnktAm7ZmSwKK3GDjzrN+ojLwh6ACKfNZ2rzKxJkgm7QDIjyTGB4yDIP6VB4PXNvNW1lpWxIDvfYfuUmb3pHzWJHU597C6I3whSPOjHY+fhrT+ZvgslyaRnDcfl+xzk3qU765GnaYMQKJARwEEAECAAYFAkPp15AACgkQhitJofOjivHY1wf/Tlb4uCCbx6mXnhe87jTOeA5ZDGxeDN11rP5ojqVW9sQytJDC3kZLDrsMHAUQkBTgxn1Cg/5Ia6lKdfKynO3z436hngZ3SgD1Ic2s3iliuhUaUHt+YbauJapsFUZIOIiTI23OJnVEcepfDXu6aPQd+17a/x6pMMq0MkF9993vEdGpXkp7KRsoc06Uem3wYMYtyjLSyGtmBn7yuDJDGyoYHxGiA4cNVkWL9tQ4TSCiBwLAhuNaDS+ZeJYnab7tHnKfycMMDjdoziXK6qS2Tgme82bXghnCizT3IH1Y+bxA9Kcb4WEE5ZAML7jkN4kP4r0nGg76/WQDKMuz8X0fxYPCI4kCHAQQAQIABgUCRrhoPwAKCRAJQDI7Z6q+kkLkD/4tjIwE6XGwgbG37c4TdOP+o8GPonb4Gri/zECZWVZI6wahmmjfu1c8NIVOXMgbsELtshSx/eUviQTzRrT23E9eU2/qea2Jn0XuRMdp/fnys45pAfqC69vSnGBXqBsDWCv3inrqiVexo3HS3C64bWSShQJKM3ESkgJKIsQBEGOlYMcTaOWewt8kpTG97JNRoDbcplK+UPgIw6uBTSuO7D5NddXK6bgqbZO2K1zmzT1PcQ1tMW8FiZwxdn5+7vfn9/56Vx+u
E84M3ytqjpN4iUVOgDqLo3G1vKnjHhIH2qsZFATlaG+d/A31IK+WDb+oxtxOBh9wAXJkxfTLPO0Sr0MKqXWaGf72QfeCwdnInmFrYqA4lXetF7ik+BM0ArBIgYx29y9Xydq+ujFAvfgSscNma8R2KlsvBM8iwAvdRynK64146r3j+SFqpgL6f+tZLY0JLpWjNsLnkuxu4uq5vjQaRI42RRsjvCTcWmvyDT6xLtpf6xPoueHxLgnfSFh6NfJkOEUOBslBHSunJdSYAOXfX6S5rillKqN5Ths80wAGb5r/NW5rgiElfxiH82zXovjlZ33zflTM3j8QzSW7odBJLZ8A9IE7oFCtLYHAk6MsDMUs4mwTgL04mdx4PIQoRpGGFJ10tC+SALUSS89G9RZ+r63S7gYpHPFXpi8wyBcNo4kCHAQSAQIABgUCSDL5pwAKCRAP0mHyDGoY6ujhEACp6rsY0iCh3K8MXsugdaG4Z9HBLpNFYkLroHkiauhMBY273i4/bHbNseGyi5ZLQDsvf3xiKOD6Fv9/fb4W9agypt9RUNerIJcUPFV4At+RDfQReF7MtYHYyhu6H6bzzyuUvQ/B+0StsbDa7eAvbOw2jLZ1cJRMN0N6zZ15BZEIqY1qmor6ZINzOXavRv7gkFqM1agSB2Utm2x4aZSykvd44hj/HJsMBl5cgYTQEiQLHDK3IQiDyc45qRF0dXQOOxwjt2h+HnkqcWXooiTNtjoQH3q+DvRpH/VENHdTr9tU6oQoJTXs0zwI0S5fFVtYayStQCaNVwc494tvzmBYaG2/EBRR6NSbAV+Zm9Zd9/UP5rcC1eOiYcUzO3nie1ywwkTKe6ve5ARGAxYrDf0MpGB8HZ1trXkG/js07To0Y/+r88OyCTdkkJIBlFKN/AKPPLpD+UgI2T38qux7Z/p3FZTvYjHkFPOkBw0s3bbrbU4f1xymwn/3
A2vQeoDMU0TXnBp+fHVF3auXU1grP/08nzekF470s8fPccga22t33zqdTRFKRKwK2yjONqPpfXIRrTsvxoXTEazY+pRDR9Wlz5jbTAI7NYiUHWwLe1eMzxjoJ/hF+HQ/3WxNsKsgNJUHKGhd/9dU50sqbH1uHqrqKoZkpJPaSfjQJd6vjmgnGiXoRrkBDQQ6owWQEAQA/mf8FPRjkxG2BBACJafXHI7Ml5EFaFYtHNFRAGf66lSMMmQVCNd8Mf9wCA2QUCQt0jodYe326264CuHit5hyd0+NucFI5rMu+QqWtDt/q4ibOOc/9t+EQG5Ulqdpm/OYBnwhR6rrgXFIcdDVtYZpRF7EJ4gqk784HAt8bBRKChsAAwYEAO0eLwwzglV0ikyHxRc11Lvm8rfL6lw7UZdobV9xoRRfh626VUKvpGfPFGMguqDfYLnsStQyV/W4KgBgK226QUbfH9MmrrW2LUPtmp8a/J7vVjib/d2ZD6/iM8Ck5ss0WJqruj0dylhDTXYjRJBucm9nsdmmPjzIAgICgXoWtL+iiE4EGBECAAYFAjqjBZAAEgkQYk3FZRNepmgHZUdQRwABARG+AJ9VvX3Pw9CCj8Iy1UUmm9wVufuexwCgleFl6vnBm/dFNSFIom8o2DEPC8M=
=EhZV
-----END PGP PUBLIC KEY BLOCK-----"));
    results = gpg.ImportKeys(stream, keyring);
    foreach(ImportedKey result in results) Assert.IsTrue(result.Successful);

    // read several times in quick succession to try to expose deadlocks
    for(int i=0; i<20; i++) keys = gpg.GetKeys(keyring);
    Assert.AreEqual(7, keys.Length);
    gpg.DeleteKeys(keys, KeyDeletion.PublicAndSecret);
    this.keys = null; // force the next test to reimport keys
  }

  [Test]
  public void T12_KeyServer()
  {
    KeyDownloadOptions downloadOptions = new KeyDownloadOptions(new Uri("hkp://pgp.mit.edu"));

    // try searching for keys
    List<PrimaryKey> keysFound = new List<PrimaryKey>();
    gpg.FindKeysOnServer(downloadOptions.KeyServer,
                         delegate(PrimaryKey[] keys) { keysFound.AddRange(keys); return true; },
                         "adam@adammil.net");
    Assert.AreEqual(2, keysFound.Count);
    Assert.IsTrue(keysFound[0].PrimaryUserId.Name.StartsWith("Adam Milazzo"));
    Assert.IsTrue(keysFound[1].Revoked);

    // import the key found
    ImportedKey[] result = gpg.ImportKeysFromServer(downloadOptions, keyring, keysFound[0].EffectiveId);
    Assert.AreEqual(1, result.Length);
    Assert.IsTrue(result[0].Successful);
    Assert.AreEqual("21A3235327E64C6256B7912BB6E5982D6252430A", result[0].Fingerprint);

    // make sure it was really imported
    PrimaryKey adamsKey = gpg.FindKey("6252430A", keyring);
    Assert.IsNotNull(adamsKey);
    Assert.IsTrue(adamsKey.PrimaryUserId.Name.StartsWith("Adam Milazzo"));
    // TODO: Assert.IsNotNull(gpg.GetPreferences(adamsKey.PrimaryUserId).Keyserver);

    // refresh the key
    result = gpg.RefreshKeysFromServer(downloadOptions, adamsKey);
    Assert.AreEqual(1, result.Length);
    Assert.IsTrue(result[0].Successful);

    // refresh the entire keyring
    result = gpg.RefreshKeysFromServer(downloadOptions, keyring);
    Assert.AreEqual(1, result.Length);
    Assert.IsTrue(result[0].Successful);

    // upload the key back to the key server
    gpg.UploadKeys(new KeyUploadOptions(downloadOptions.KeyServer), adamsKey);

    // delete the key from the keyring
    gpg.DeleteKey(adamsKey, KeyDeletion.PublicAndSecret);
  }

  void CheckSignatures(PrimaryKey[] keys, Signature[] sigs)
  {
    Assert.AreEqual(sigs.Length, 2);
    Assert.IsTrue(sigs[0].IsValid);
    Assert.IsTrue(sigs[1].IsValid);
    Assert.AreEqual(keys[Signer].Fingerprint, sigs[0].KeyFingerprint);
    Assert.AreEqual(keys[Encrypter].Fingerprint, sigs[1].KeyFingerprint);
  }

  void EnsureImported()
  {
    if(keys == null) T01_ImportTestKeys();
  }

  ExeGPG gpg;
  string gpgPath;
  System.Security.SecureString password;
  Keyring keyring;
  PrimaryKey[] keys;
}

[TestFixture]
public class GPG1Test : GPGTestBase
{
  public GPG1Test() : base("c:/program files/gnu/gnupg/gpg.exe") { }
}

[TestFixture]
public class GPG2Test : GPGTestBase
{
  public GPG2Test() : base("c:/program files/gnu/gnupg/gpg2.exe") { }
}

} // namespace AdamMil.Security.Tests