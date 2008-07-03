/*
GPG.net is a .NET interface to the GNU Privacy Guard (www.gnupg.org).
http://www.adammil.net/
Copyright (C) 2008 Adam Milazzo

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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security;
using AdamMil.Collections;

namespace AdamMil.Security.PGP
{

#region Algorithms and key types
#region Compression
/// <summary>A static class containing commonly-supported compression types. Note that not all of these types may be
/// supported, so it's recommended that <see cref="Default"/> be used whenever possible. If a specific algorithm is
/// desired, use <see cref="PGPSystem.GetSupportedCompressions"/> to verify that it is supported.
/// </summary>
public static class Compression
{
  /// <summary>The default compression type is used. Typically, this is the <see cref="Zip"/> algorithm.</summary>
  public static readonly string Default = null;
  /// <summary>The data will not be compressed. This is not recommended, for reasons of both both security and data
  /// size.
  /// </summary>
  public static readonly string Uncompressed = "Uncompressed";
  /// <summary>The RFC-1951 ZIP algorithm will be used. This is the only compression algorithm currently supported by
  /// the OpenPGP standard, and using it gives maximum compatibility, but it is also the least effective algorithm.
  /// </summary>
  public static readonly string Zip = "ZIP";
  /// <summary>The RFC-1950 ZLIB algorithm will be used. This algorithm typically achieves better compression than
  /// <see cref="Zip"/>, but may not be supported by all clients.
  /// </summary>
  public static readonly string Zlib = "ZLIB";
  /// <summary>The BZIP2 algorithm will be used. This modern algorithm provides a high compression ratio, but uses a
  /// significant amount of memory and CPU time, and may not be supported by all clients.
  /// </summary>
  public static readonly string Bzip2 = "BZIP2";
}
#endregion

#region HashAlgorithm
/// <summary>A static class containing commonly-supported hash algorithms. Note that not all of these types may be
/// supported, so it's recommended that <see cref="Default"/> be used whenever possible. If a specific algorithm is
/// desired, use <see cref="PGPSystem.GetSupportedHashes"/> to verify that it is supported.
/// </summary>
public static class HashAlgorithm
{
  /// <summary>The default hash algorithm is used. Typically, this is a the <see cref="SHA1"/> algorithm.</summary>
  public static readonly string Default = null;
  /// <summary>An older, less secure hash algorithm with a 128 bit output. This is widely supported, but less supported
  /// than <see cref="SHA1"/>.
  /// </summary>
  public static readonly string MD5 = "MD5";
  /// <summary>The RIPE-MD/160 algorithm, which has a 160 bit output. This algorithm was developed by the open academic
  /// community, as opposed to the NSA-designed SHA algorithms. It is less widely used and supported, and has been
  /// exposed to less scrutiny.
  /// </summary>
  public static readonly string RIPEMD160 = "RIPEMD160";
  /// <summary>The Secure Hash Algorithm, with a 160 bit output. This is the most compatible option, and a fairly
  /// secure choice.
  /// </summary>
  public static readonly string SHA1 = "SHA1";
  /// <summary>The Secure Hash Algorithm, with a 224 bit output. This is less compatible than <see cref="SHA1"/>.</summary>
  public static readonly string SHA224 = "SHA224";
  /// <summary>The Secure Hash Algorithm, with a 256 bit output. This is less compatible than <see cref="SHA1"/>.</summary>
  public static readonly string SHA256 = "SHA256";
  /// <summary>The Secure Hash Algorithm, with a 384 bit output. This is less compatible than <see cref="SHA1"/>.</summary>
  public static readonly string SHA384 = "SHA384";
  /// <summary>The Secure Hash Algorithm, with a 512 bit output. This is less compatible than <see cref="SHA1"/>.</summary>
  public static readonly string SHA512 = "SHA512";
}
#endregion

#region SymmetricCipher
/// <summary>A static class containing commonly-supported symmetric ciphers. Note that not all of these types may be
/// supported, so it's recommended that <see cref="Default"/> be used whenever possible. If a specific algorithm is
/// desired, use <see cref="PGPSystem.GetSupportedCiphers"/> to verify that it is supported.
/// </summary>
public static class SymmetricCipher
{
  /// <summary>The default cipher algorithm will be used.</summary>
  public static readonly string Default = null;
  /// <summary>The winning AES finalist, this is the Advanced Encryption Standard algorithm, also known as Rijndael,
  /// with a 128-bit key. This is very a widely-supported algorithm.
  /// </summary>
  public static readonly string AES = "AES";
  /// <summary>This is the Advanced Encryption Standard algorithm, also known as Rijndael, with a 192-bit key.</summary>
  public static readonly string AES192 = "AES192";
  /// <summary>This is the Advanced Encryption Standard algorithm, also known as Rijndael, with a 256-bit key.</summary>
  public static readonly string AES256 = "AES256";
  /// <summary>Blowfish is the predecessor of the <see cref="Twofish"/> cipher, and is somewhat more widely supported
  /// than it, but still less supported than <see cref="AES"/>.
  /// </summary>
  public static readonly string Blowfish = "BLOWFISH";
  /// <summary>The CAST algorithm with a 128-bit key.</summary>
  public static readonly string CAST5 = "CAST5";
  /// <summary>The International Data Encryption Algorithm. It is a fairly strong algorithm, but it is patented, and
  /// for that reason is not currently implemented in free software like the GNU Privacy Guard.
  /// </summary>
  public static readonly string IDEA = "IDEA";
  /// <summary>Serpent was an AES finalist, and was deemed to have the highest security. Its poor performance was its
  /// primary failing. Serpent is rarely supported.
  /// </summary>  
  public static readonly string Serpent = "SERPENT";
  /// <summary>The most widely-supported algorithm, but also the oldest and probably the weakest.</summary>
  public static readonly string TripleDES = "3DES";
  /// <summary>Twofish was an AES finalist, and was deemed to have higher security than the winning <see cref="AES"/>
  /// algorithm, Rijndael, (though lower than <see cref="Serpent"/>). Its poor performance was its primary failing.
  /// </summary>  
  public static readonly string Twofish = "TWOFISH";
}
#endregion

#region PrimaryKeyType
/// <summary>A static class containing commonly-supported primary key types. Note that not all of these types may be
/// supported, so it's recommended that <see cref="Default"/> be used whenever possible. If a specific algorithm is
/// desired, use <see cref="PGPSystem.GetSupportedPrimaryKeys"/> to verify that it is supported.
/// </summary>
public static class PrimaryKeyType
{
  /// <summary>The default master key type will be used.</summary>
  public static readonly string Default = null;
  /// <summary>The FIPS-186 Digital Signature Algorithm will be used, which is for signing only. Standard DSA keys can
  /// be up to 1024 bits in length. Larger keys are supported by only a few clients.
  /// </summary>
  public static readonly string DSA = "DSA";
  /// <summary>The RSA algorithm will be used, creating a key that can both sign and encrypt. (Although it's better to
  /// use a signature-only primary key and only use subkeys to encrypt.)
  /// </summary>
  public static readonly string RSA = "RSA";
}
#endregion

#region SubkeyType
/// <summary>A static class containing commonly-supported subkey types. Note that not all of these types may be
/// supported, so it's recommended that <see cref="Default"/> be used whenever possible. If a specific algorithm is
/// desired, use <see cref="PGPSystem.GetSupportedSubkeys"/> to verify that it is supported.
/// </summary>
public static class SubkeyType
{
  /// <summary>The default subkey type will be used.</summary>
  public static readonly string Default = null;
  /// <summary>No subkey will be created.</summary>
  public static readonly string None = "None";
  /// <summary>The FIPS-186 Digital Signature Algorithm will be used, which is for signing only. Standard DSA keys can
  /// be up to 1024 bits in length. Larger keys are supported by only a few clients.
  /// </summary>
  public static readonly string DSA = "DSA";
  /// <summary>An ElGamal encryption-only key will be created.</summary>
  public static readonly string ElGamalEncryptOnly = "ELG-E";
  /// <summary>An ElGamal encryption key will be created. Despite the contrast with <see cref="ElGamalEncryptOnly"/>,
  /// a subkey with this type can also be used only for encryption.
  /// </summary>
  public static readonly string ElGamal = "ELG";
  /// <summary>The RSA algorithm will be used, creating a key that can both sign and encrypt. (Although it's better to
  /// use a signature-only primary key to sign and an encryption-only subkey to encrypt.)
  /// </summary>
  public static readonly string RSA = "RSA";
  /// <summary>The RSA algorithm will be used, creating a signing-only key.</summary>
  public static readonly string RSASignOnly = "RSA-S";
  /// <summary>The RSA algorithm will be used, creating an encryption-only key.</summary>
  public static readonly string RSAEncryptOnly = "RSA-E";
}
#endregion
#endregion

#region Command options
#region DecryptionOptions
/// <summary>Specifies options that control the decryption of data.</summary>
public class DecryptionOptions : VerificationOptions
{
  /// <summary>Initializes a new <see cref="DecryptionOptions"/> object with the default options.</summary>
  public DecryptionOptions() { }

  /// <summary>Initializes a new <see cref="DecryptionOptions"/> object with the given cipher password.</summary>
  public DecryptionOptions(SecureString password)
  {
    Password = password;
  }

  /// <summary>Gets or sets the password used to decrypt the data. This is not related to the password used to access
  /// an encryption key on the keyring. The password used is decrypt ciphertext that was with a simple password (using
  /// <see cref="EncryptionOptions.Password"/>). The value null, which is the default, specifies that password-based
  /// decryption will not be attempted.
  /// </summary>
  /// <seealso cref="EncryptionOptions.Password"/>
  public SecureString Password
  {
    get { return password; }
    set { password = value; }
  }

  SecureString password;
  string cipher = SymmetricCipher.Default;
}
#endregion

#region EncryptionOptions
/// <summary>Specifies options that control the encryption of data.</summary>
public class EncryptionOptions
{
  /// <summary>Initializes a new <see cref="EncryptionOptions"/> object with no recipients.</summary>
  public EncryptionOptions() { }

  /// <summary>Initializes a new <see cref="EncryptionOptions"/> object with the given recipients.</summary>
  public EncryptionOptions(params Key[] recipients)
  {
    foreach(Key recipient in recipients) Recipients.Add(recipient);
  }

  /// <summary>Initializes a new <see cref="EncryptionOptions"/> object with the given symmetric cipher password.</summary>
  public EncryptionOptions(SecureString password)
  {
    Password = password;
  }

  /// <summary>Gets or sets whether recipients are always trusted. If false, trust issues with recipients can cause
  /// the encryption to fail. If true, trust issues with recipients will be ignored. The default is false.
  /// </summary>
  public bool AlwaysTrustRecipients
  {
    get { return alwaysTrust; }
    set { alwaysTrust = value; }
  }

  /// <summary>Gets or sets the name of the cipher algorithm used to encrypt the data. This can be one of the
  /// <see cref="SymmetricCipher"/> values, or another cipher name. However, specifying a cipher can cause the message
  /// to not be decryptable by some of the recipients, if their PGP clients do not support that algorithm, so it's
  /// usually best to leave it at the default value of <see cref="SymmetricCipher.Default"/>, and allow the software to
  /// determine the algorithm used.
  /// </summary>
  public string Cipher
  {
    get { return cipher; }
    set { cipher = value; }
  }

  /// <summary>Gets or sets the value of the "for your eyes only" flag in the message. If true, client programs will
  /// take extra precautions to prevent information from being disclosed, for instance by refusing to save the
  /// plaintext and only displaying it with a TEMPEST-resistant font.
  /// </summary>
  public bool EyesOnly
  {
    get { return eyesOnly; }
    set { eyesOnly = value; }
  }

  /// <summary>Gets or sets the password used to encrypt the data. This is not related to the password used to access
  /// an encryption key on the keyring. The password is used to create ciphertext that can be decrypted without a key,
  /// using only the password. This value can be combined with <see cref="Recipients"/> and/or
  /// <see cref="HiddenRecipients"/> to create ciphertext that can be decrypted with a password or a key. The value
  /// null, which is the default, specifies that password-based encryption will not be used, so the data can only be
  /// decrypted using the appropriate key. An empty password is treated as if the password was null.
  /// </summary>
  public SecureString Password
  {
    get { return password; }
    set { password = value; }
  }

  /// <summary>Specifies the public keys of the recipients of the message. The IDs of the recipients' keys will be
  /// included in the message, allowing easy decryption by the intended recipients, but also the disclosure of the
  /// recipients' key identities. <see cref="HiddenRecipients"/> can be used to include additional, unnamed recipients.
  /// In general, it is recommended that recipients be named and not hidden.
  /// </summary>
  public KeyCollection Recipients
  {
    get { return recipients; }
  }

  /// <summary>Specifies the public keys of the hidden recipients of the message. The IDs of the recipients keys will
  /// not be included in the message, and their client software will not know which key to use when decrypting the
  /// ciphertext. This can be cumbersome, requiring every key to be tried by the recipients, but it prevents the
  /// obvious association of their key IDs with the data.
  /// </summary>
  public KeyCollection HiddenRecipients
  {
    get { return hiddenRecipients; }
  }

  readonly KeyCollection recipients = new KeyCollection(KeyCapability.Encrypt);
  readonly KeyCollection hiddenRecipients = new KeyCollection(KeyCapability.Encrypt);
  SecureString password;
  string cipher = SymmetricCipher.Default;
  bool alwaysTrust, eyesOnly;
}
#endregion

#region ExportOptions
/// <summary>Options that control how keys are exported.</summary>
[Flags]
public enum ExportOptions
{
  /// <summary>The default export options will be used. This will cause only public keys to be exported.</summary>
  Default=0,
  /// <summary>Key signatures marked as "local only" will be exported. Normally, they are skipped.</summary>
  ExportLocalSignatures=1,
  /// <summary>Attribute user IDs (eg, photo IDs) will not be included in the output.</summary>
  ExcludeAttributes=2,
  /// <summary>Includes revoker information that was marked as sensitive.</summary>
  ExportSensitiveRevokerInfo=4,
  /// <summary>When exporting secret keys, this option causes the secret portion of the master key to not be exported.
  /// Only the secret subkeys are exported. This is not OpenPGP compliant and currently only GPG is known to
  /// implement this option or be capable of importing keys created by this option.
  /// </summary>
  ClobberMasterSecretKey=8,
  /// <summary>When exporting secret subkeys, resets their passwords to empty.</summary>
  ResetSubkeyPassword=16,
  /// <summary>Does not export unusable signatures, and does not export any signatures for unusable user IDs.</summary>
  CleanKeys=32,
  /// <summary>Exports only the most recent self-signature on each user ID.</summary>
  MinimizeKeys=64
}
#endregion

#region ImportOptions
/// <summary>Options that control how keys are imported.</summary>
[Flags]
public enum ImportOptions
{
  /// <summary>The default import options will be used.</summary>
  Default=0,
  /// <summary>Key signatures marked as "local only" will be imported. Normally, they are skipped.</summary>
  ImportLocalSignatures=1,
  /// <summary>Existing keys will be updated with the data from the import, but new keys will not be created.</summary>
  MergeOnly=2,
  /// <summary>Removes from imported keys all signatures that are unusable, and removes all signatures from user IDs
  /// that are unusable.
  /// </summary>
  CleanKeys=4,
  /// <summary>Removes from imported keys all signatures except the most recent self-signatures on each user ID.</summary>
  MinimizeKeys=8
}
#endregion

#region KeyDeletion
/// <summary>Determines which parts of a primary should be deleted.</summary>
public enum KeyDeletion
{
  /// <summary>The secret keys should be deleted.</summary>
  Secret,
  /// <summary>The entire primary key should be deleted.</summary>
  PublicAndSecret
}
#endregion

#region KeySigningOptions
/// <summary>Options to control the signing of others' keys and attributes.</summary>
public class KeySigningOptions
{
  /// <summary>Gets or sets whether the signature will be exportable. You create an exportable signature only if you've
  /// done proper validation of the owner's identity. The default is false.
  /// </summary>
  public bool Exportable
  {
    get { return exportable; }
    set { exportable = value; }
  }

  /// <summary>Gets or sets whether this signature is irrevocable. An irrevocable signature can never be revoked.
  /// The default is false.
  /// </summary>
  public bool Irrevocable
  {
    get { return irrevocable; }
    set { irrevocable = value; }
  }

  /// <summary>Gets or sets whether a trust signature will be created, and the trust level of the signature. This
  /// property is limited to three values: <see cref="TrustLevel.Unknown"/>, <see cref="TrustLevel.Marginal"/>, and
  /// <see cref="TrustLevel.Full"/>. If set to <see cref="TrustLevel.Unknown"/> (the default), a standard signature
  /// will be created. If set to <see cref="TrustLevel.Marginal"/> or <see cref="TrustLevel.Full"/>, a trust signature
  /// will be created, which signifies that the user is trusted to issue signatures with any lower trust level.
  /// </summary>
  public TrustLevel TrustLevel
  {
    get { return trustLevel; }
    set
    {
      if(trustLevel != TrustLevel.Unknown && trustLevel != TrustLevel.Marginal && trustLevel != TrustLevel.Full)
      {
        throw new ArgumentException("TrustLevel can only be Unknown, Marginal, or Full.");
      }
      trustLevel = value; 
    }
  }

  TrustLevel trustLevel;
  bool exportable, irrevocable;
}
#endregion

#region ListOptions
/// <summary>Options that control how keys will be retrieved.</summary>
[Flags]
public enum ListOptions
{
  /// <summary>The default options will be used.</summary>
  Default=0,

  /// <summary>Signatures on keys will be ignored.</summary>
  IgnoreSignatures=0,
  /// <summary>Signatures on keys will be retrieved, but not verified.</summary>
  RetrieveSignatures=1,
  /// <summary>Signatures on keys will be retrieved and verified.</summary>
  VerifySignatures=3,
  /// <summary>A mask that can be ANDed with a <see cref="ListOptions"/> to get the signature handling value, which is
  /// one of <see cref="IgnoreSignatures"/>, <see cref="RetrieveSignatures"/>, or <see cref="VerifySignatures"/>.
  /// </summary>
  SignatureMask=3,

  /// <summary>User attributes on keys will be ignored.</summary>
  IgnoreAttributes=0,
  /// <summary>User attributes will be retrieved, but unknown attributes will be ignored.</summary>
  RetrieveAttributes=4,
  /// <summary>A mask that can be ANDed with a <see cref="ListOptions"/> to get the attribute handling value, which is
  /// one of <see cref="IgnoreAttributes"/> or <see cref="RetrieveAttributes"/>.
  /// </summary>
  AttributeMask=4
}
#endregion

#region NewKeyOptions
/// <summary>Options that control how a new primary key should be created.</summary>
public class NewKeyOptions
{
  /// <summary>Gets or sets the name of the master key type. This can be a member of <see cref="PrimaryKeyType"/>, or
  /// another key type, but it's best to leave it at the default value of <see cref="PrimaryKeyType.Default"/>, which
  /// specifies that a default key type will be used.
  /// </summary>
  public string KeyType
  {
    get { return keyType; }
    set { keyType = value; }
  }

  /// <summary>Gets or sets the length of the master key, in bits. If set to zero, a default value will be used.</summary>
  public int KeyLength
  {
    get { return keyLength; }
    set { keyLength = value; }
  }

  /// <summary>Gets or sets the name of the subkey type. This can be a member of <see cref="PGP.SubkeyType"/>, or
  /// another key type, but it's best to leave it at the default value of <see cref="PGP.SubkeyType.Default"/>, which
  /// specifies that a default key type will be used. If set to <see cref="PGP.SubkeyType.None"/>, no subkey will be
  /// created.
  /// </summary>
  /// <remarks>Multiple subkeys can be associated with a master key. If set to a value other than 
  /// <see cref="PGP.SubkeyType.None"/>, this property causes a subkey to be created along with the master key. This
  /// is convenient, but it can be useful to create the subkey separately, for instance to set a different expiration
  /// date on the subkey.
  /// </remarks>
  public string SubkeyType
  {
    get { return subkeyType; }
    set { subkeyType = value; }
  }

  /// <summary>Gets or sets the length of the subkey, in bits. If set to zero, a default value will be used.</summary>
  public int SubkeyLength
  {
    get { return subkeyLength; }
    set { subkeyLength = value; }
  }

  /// <summary>Gets or sets the expiration of the master key and the subkey. This must be a time in the future.</summary>
  public DateTime? Expiration
  {
    get { return expiration; }
    set { expiration = value; }
  }

  /// <summary>Gets or sets the password used to encrypt the key. Because this password is often the weakest link in
  /// the entire encryption system, it should be very strong: at least 20 characters, containing upper and lowercase
  /// characters, numbers, and punctuation symbols. It should not contain words, even obfuscated using publically-known
  /// obfuscation schemes (such as "H31l0" instead of "Hello"), since these are vulnerable to dictionary attacks. If
  /// null or empty, no password will be used. This is very insecure.
  /// </summary>
  public SecureString Password
  {
    get { return password; }
    set { password = value; }
  }

  /// <summary>Gets or sets the the keyring in which the new key will be stored. The keyring must have both public and
  /// secret parts. If null, the default keyring will be used.
  /// </summary>
  public Keyring Keyring
  {
    get { return keyring; }
    set
    {
      if(value.SecretFile == null) throw new ArgumentException("The keyring must both public and secret parts.");
      keyring = value;
    }
  }

  /// <summary>Gets or sets the name of the person who owns the key. This value must be set to a non-empty string.</summary>
  public string RealName
  {
    get { return realName; }
    set { realName = value; }
  }

  /// <summary>Gets or sets the email address of the person who owns the key.</summary>
  public string Email
  {
    get { return email; }
    set { email = value; }
  }

  /// <summary>Gets or sets the comment associated with the person who owns the key, for instance "old email",
  /// "maiden name", or "<c>CompanyName</c>", used to help those who receive the key associate it with the right, and
  /// to help the key owner keep track of his own keys.
  /// </summary>
  public string Comment
  {
    get { return comment; }
    set { comment = value; }
  }

  SecureString password;
  string realName, email, comment;
  Keyring keyring;
  int keyLength, subkeyLength;
  DateTime? expiration;
  string keyType = PrimaryKeyType.Default, subkeyType = PGP.SubkeyType.Default;
}
#endregion

#region OutputFormat
/// <summary>Specifies the output format of an encryption or signing operation.</summary>
public enum OutputFormat
{
  /// <summary>The output will be in the OpenPGP binary format.</summary>
  Binary,
  /// <summary>The output will be in the ASCII-armored OpenPGP format. This format is suitable for sending through
  /// email and other text-based systems.
  /// </summary>
  ASCII
}
#endregion

#region OutputOptions
/// <summary>Specifies options that control the output of data.</summary>
public class OutputOptions
{
  /// <summary>Initializes a new <see cref="OutputOptions"/> object with a binary output format and no comments.</summary>
  public OutputOptions() { }

  /// <summary>Initializes a new <see cref="OutputOptions"/> object with the given format and comments.</summary>
  public OutputOptions(OutputFormat format, params string[] comments)
  {
    Format = format;

    foreach(string comment in comments)
    {
      if(comment != null) Comments.Add(comment);
    }
  }

  /// <summary>Gets a collection of comments to be added to the message. They are intended to be used with
  /// <see cref="OutputFormat.ASCII"/> output, and for maximum compatibility with mail systems should be limited to
  /// less than 60 characters per comment.
  /// </summary>
  public List<string> Comments
  {
    get { return comments; }
  }

  /// <summary>Gets or sets the format of the output. The default is <see cref="OutputFormat.Binary"/>.</summary>
  public OutputFormat Format
  {
    get { return format; }
    set { format = value; }
  }

  readonly List<string> comments = new List<string>();
  OutputFormat format = OutputFormat.Binary;
}
#endregion

#region Randomness
/// <summary>Determines the security level of random data generated by a <see cref="PGPSystem">PGP system</see>.</summary>
public enum Randomness
{
  /// <summary>The randomness produced is not very strong, but this is the fastest generator.</summary>
  Weak,
  /// <summary>The randomness produced is fairly strong, and the generator is quite fast. This is the recommended
  /// value for most uses.
  /// </summary>
  Strong,
  /// <summary>The randomness is the strongest available. This quality level is typically used only for key generation,
  /// and it can be very slow -- it might not complete for several minutes or longer for even a modest amount of random
  /// data. Since key generation should be handled by the PGP system itself, there is usually no reason for this option
  /// to be used.
  /// </summary>
  TooStrong
}
#endregion

#region SigningOptions
/// <summary>Specifies options that control the signing of data.</summary>
public class SigningOptions
{
  /// <summary>Initializes a new <see cref="SigningOptions"/> object with the default options and no signing keys.</summary>
  public SigningOptions() { }

  /// <summary>Initializes a new <see cref="SigningOptions"/> object with the given list of signing keys.</summary>
  public SigningOptions(params Key[] signers)
  {
    foreach(Key key in signers) Signers.Add(key);
  }

  /// <summary>Initializes a new <see cref="SigningOptions"/> object with the given detached flag and list of signing
  /// keys.
  /// </summary>
  public SigningOptions(bool detached, params Key[] signers)
  {
    Detached = detached;
    foreach(Key key in signers) Signers.Add(key);
  }

  /// <summary>Gets or sets a value that determines whether the signature will be embedded in or detached from the
  /// data. If true, the output of the signature operation will be only the signature itself. If false, the output will
  /// be a copy of the data with the signature embedded. The default is false.
  /// </summary>
  public bool Detached
  {
    get { return detached; }
    set { detached = value; }
  }

  /// <summary>Gets or sets the name of the algorithm used to hash the data. This can be one of the
  /// <see cref="HashAlgorithm"/> values, or another algorithm name, but it's usually best to leave it at the default
  /// value of <see cref="HashAlgorithm.Default"/>, and allow the software to determine the algorithm used.
  /// </summary>
  public string Hash
  {
    get { return hash; }
    set { hash = value; }
  }

  /// <summary>Gets a collection that should be filled with the keys used to sign the message. There must be at
  /// least one key added to the collection. The keys must have both public and private portions.
  /// </summary>
  public KeyCollection Signers
  {
    get { return signers; }
  }

  readonly KeyCollection signers = new KeyCollection(KeyCapability.Sign);
  string hash = HashAlgorithm.Default;
  bool detached;
}
#endregion

#region UserPreferences
/// <summary>Stores the preferences of a user, as associated with a user ID or attribute.</summary>
public class UserPreferences
{
  /// <summary>Gets or sets the user's preferred keyserver, or null if no keyserver is preferred.</summary>
  public Uri Keyserver
  {
    get { return keyServer; }
    set { keyServer = value; }
  }

  /// <summary>Gets or sets a list containing the user's preferred ciphers, in order from most to least preferred.
  /// Algorithms not listed are assumed to be unsupported by the user.
  /// </summary>
  public PreferenceList<OpenPGPCipher> PreferredCiphers
  {
    get { return preferredCiphers; }
  }

  /// <summary>Gets or sets a list containing the user's preferred compression algorithms, in order from most to least
  /// preferred. Algorithms not listed are assumed to be unsupported by the user.
  /// </summary>
  public PreferenceList<OpenPGPCompression> PreferredCompressions
  {
    get { return preferredCompressions; }
  }

  /// <summary>Gets or sets a list containing the user's preferred hash algorithms, in order from most to least
  /// preferred. Algorithms not listed are assumed to be unsupported by the user.
  /// </summary>
  public PreferenceList<OpenPGPHashAlgorithm> PreferredHashes
  {
    get { return preferredHashes; }
  }

  /// <summary>Gets or sets whether this user ID or attribute is the primary one.</summary>
  public bool Primary
  {
    get { return primary; }
    set { primary = value; }
  }

  readonly PreferenceList<OpenPGPCipher> preferredCiphers = new PreferenceList<OpenPGPCipher>();
  readonly PreferenceList<OpenPGPCompression> preferredCompressions = new PreferenceList<OpenPGPCompression>();
  readonly PreferenceList<OpenPGPHashAlgorithm> preferredHashes = new PreferenceList<OpenPGPHashAlgorithm>();
  Uri keyServer;
  bool primary;
}
#endregion

#region VerificationOptions
/// <summary>Specifies options that control the verification of signatures.</summary>
public class VerificationOptions
{
  /// <summary>Gets a collection of additional keyrings that will be searched to find the appropriate public key.</summary>
  public List<Keyring> AdditionalKeyrings
  {
    get { return keyrings; }
  }

  /// <summary>Gets or sets a value that determines whether the input will be assumed to be binary. If true, the input
  /// will be assumed to be binary. If false, the default, the input will be checked for ASCII armoring, allowing both
  /// text and binary input to be decrypted. The default is false.
  /// </summary>
  public bool AssumeBinaryInput
  {
    get { return binaryOnly; }
    set { binaryOnly = value; }
  }

  /// <summary>Gets or sets a value that determines whether the system should attempt to find keys not on the local
  /// keyrings by contacting public key servers, etc. If <see cref="KeyServer"/> is set, a limited form of key fetching
  /// will be enabled even if this is set to false. Note that downloaded keys may be added to a local keyring.
  /// The default is false.
  /// </summary>
  public bool AutoFetchKeys
  {
    get { return autoFetch; }
    set { autoFetch = value; }
  }

  /// <summary>Gets or sets a value that determines whether the default keyring should be ignored -- that is, not
  /// searched to find the secret key.
  /// </summary>
  public bool IgnoreDefaultKeyring
  {
    get { return ignoreDefaultKeyring; }
    set { ignoreDefaultKeyring = value; }
  }

  /// <summary>Gets or sets the URI of the public key to search for keys that are not found on the local keyrings. If
  /// set, a limited form of auto fetching (sufficient to contact the public key server) will be enabled even if
  /// <see cref="AutoFetchKeys"/> is false. Note that downloaded keys may be added to a local keyring.
  /// </summary>
  public Uri KeyServer
  {
    get { return keyServer; }
    set { keyServer = value; }
  }

  List<Keyring> keyrings = new List<Keyring>();
  Uri keyServer;
  bool autoFetch, ignoreDefaultKeyring, binaryOnly;
}
#endregion
#endregion

#region Command return types
#region ImportedKey
/// <summary>Represents a key that was processed during a key import. The key was not necessarily imported 
/// successfully.
/// </summary>
public class ImportedKey : ReadOnlyClass
{
  /// <summary>Gets the fingerprint of the primary key, or null if the fingerprint is not known.</summary>
  public string Fingerprint
  {
    get { return fingerprint; }
    set 
    {
      AssertNotReadOnly();
      fingerprint = value; 
    }
  }

  /// <summary>Gets the ID of the primary key, or null if the ID is not known.</summary>
  public string KeyId
  {
    get { return keyId; }
    set 
    {
      AssertNotReadOnly();
      keyId = value; 
    }
  }

  /// <summary>Gets the type of the primary key, or null if the type is not known.</summary>
  public string KeyType
  {
    get { return keyType; }
    set 
    {
      AssertNotReadOnly();
      keyType = value; 
    }
  }

  /// <summary>Gets whether the key was or contained a secret key.</summary>
  public bool Secret
  {
    get { return secret; }
    set
    {
      AssertNotReadOnly();
      secret = value;
    }
  }

  /// <summary>Gets whether the import of this key was successful.</summary>
  public bool Successful
  {
    get { return successful; }
    set 
    {
      AssertNotReadOnly();
      successful = value; 
    }
  }

  /// <summary>Gets the primary user ID associated with this key, or null if the user ID is not known.</summary>
  public string UserId
  {
    get { return userId; }
    set 
    {
      AssertNotReadOnly();
      userId = value; 
    }
  }

  string keyId, fingerprint, userId, keyType;
  bool secret, successful;
}
#endregion

#region SignatureStatus
/// <summary>Represents the status of a signature.</summary>
[Flags]
public enum SignatureStatus
{
  /// <summary>An error prevented the signature from being verified.</summary>
  Error=0,
  /// <summary>The signature is valid.</summary>
  Valid=0x1,
  /// <summary>The signature is invalid.</summary>
  Invalid=0x2,
  /// <summary>A mask that can be ANDed with a <see cref="SignatureStatus"/> to retrieve the overall success value:
  /// <c>Valid</c>, <c>Invalid</c>, or <c>Error</c>.
  /// </summary>
  SuccessMask=0x3,

  /// <summary>The signature has expired.</summary>
  ExpiredSignature=0x4,
  /// <summary>The signature was made with a key which has expired.</summary>
  ExpiredKey=0x8,
  /// <summary>The signature was made with a key which has been revoked.</summary>
  RevokedKey=0x10,
  /// <summary>A mask that can be ANDed with a <see cref="SignatureStatus"/> to retrieve additional information
  /// about a valid signature: <c>ExpiredSignature</c>, <c>ExpiredKey</c>, and/or <c>RevokedKey</c>.
  /// </summary>
  ValidFlagMask=ExpiredSignature | ExpiredKey | RevokedKey,

  /// <summary>The signer's public key could not be located.</summary>
  MissingKey=0x1000,
  /// <summary>The signature used an unsupported key type or hash algorithm.</summary>
  UnsupportedAlgorithm=0x2000,
  /// <summary>A mask that can be ANDed with a <see cref="SignatureStatus"/> to retrieve additional information
  /// about a verification error: <c>MissingKey</c> and/or <c>UnsupportedAlgorithm</c>.
  /// </summary>
  ErrorFlagMask=MissingKey | UnsupportedAlgorithm,
}
#endregion

#region Signature
/// <summary>Contains information about a digital signature.</summary>
/// <remarks>This class derives from <see cref="ReadOnlyClass"/>, and it is expected that a <see cref="PGPSystem"/>
/// implementation will call <see cref="ReadOnlyClass.MakeReadOnly"/> before returning a <see cref="Signature"/>
/// object.
/// </remarks>
public class Signature : ReadOnlyClass
{
  /// <summary>Gets the time the signature expires, or null if the signature does not expire.</summary>
  public DateTime? Expiration
  {
    get { return expiration; }
    set 
    {
      AssertNotReadOnly();
      expiration = value; 
    }
  }

  /// <summary>Gets whether an error occurred during verification of this signature.</summary>
  public bool ErrorOccurred
  {
    get { return (Status & SignatureStatus.SuccessMask) == SignatureStatus.Error; }
  }

  /// <summary>Gets whether the signature is known to be valid. This being false does not necessarily indicate that
  /// the signature is invalid, because it's possible that the validity could not be determined.
  /// </summary>
  public bool IsValid
  {
    get { return (Status & SignatureStatus.SuccessMask) == SignatureStatus.Valid; }
  }

  /// <summary>Gets whether the signature is known to be invalid. This being false does not necessarily indicate that
  /// the signature is valid, because it's possible that the validity could not be determined.
  /// </summary>
  public bool IsInvalid
  {
    get { return (Status & SignatureStatus.SuccessMask) == SignatureStatus.Invalid; }
  }

  /// <summary>Gets or sets the name of the hash algorithm used for the signature, or null if the algorithm could not
  /// be determined. Note that the algorithm is not necessarily supported by the PGP system.
  /// </summary>
  public string HashAlgorithm
  {
    get { return hashAlgorithm; }
    set 
    {
      AssertNotReadOnly();
      hashAlgorithm = value; 
    }
  }

  /// <summary>Gets or sets the fingerprint of the signing key.</summary>
  public string KeyFingerprint
  {
    get { return keyFingerprint; }
    set 
    {
      AssertNotReadOnly();
      keyFingerprint = value; 
    }
  }

  /// <summary>Gets or sets the ID of the signing key. Note that key IDs are not unique. For a unique identifier, use
  /// the key fingerprint (if set) instead.
  /// </summary>
  public string KeyId
  {
    get { return keyId; }
    set 
    {
      AssertNotReadOnly();
      keyId = value; 
    }
  }

  /// <summary>Gets or sets the type of the signing key, or null if the type could not be determined. Note that the
  /// key type is not necessarily supported by the PGP system.
  /// </summary>
  public string KeyType
  {
    get { return keyType; }
    set 
    {
      AssertNotReadOnly();
      keyType = value; 
    }
  }

  /// <summary>Gets or sets the fingerprint of the signing key's primary key.</summary>
  public string PrimaryKeyFingerprint
  {
    get { return primaryKeyFingerprint; }
    set 
    {
      AssertNotReadOnly();
      primaryKeyFingerprint = value; 
    }
  }

  /// <summary>Gets or sets the status of the signature.</summary>
  public SignatureStatus Status
  {
    get { return status; }
    set 
    {
      AssertNotReadOnly();
      status = value; 
    }
  }

  /// <summary>Gets or sets the time when the signature was made, or null if the time is not known.</summary>
  public DateTime? Timestamp
  {
    get { return timestamp; }
    set 
    {
      AssertNotReadOnly();
      timestamp = value; 
    }
  }

  /// <summary>Gets or sets the trust level of the signature, indicating how strongly the signature is believed to be
  /// valid.
  /// </summary>
  public TrustLevel TrustLevel
  {
    get { return trustLevel; }
    set 
    {
      AssertNotReadOnly();
      trustLevel = value; 
    }
  }

  /// <summary>Gets a human-readable description of the identity of the signer.</summary>
  public string UserName
  {
    get { return userName; }
    set 
    {
      AssertNotReadOnly();
      userName = value; 
    }
  }

  /// <include file="documentation.xml" path="/Security/Common/ToString/*"/>
  public override string ToString()
  {
    string from = null;
    if(!string.IsNullOrEmpty(UserName) || !string.IsNullOrEmpty(KeyId))
    {
      if(!string.IsNullOrEmpty(UserName)) from = UserName;
      if(!string.IsNullOrEmpty(KeyId)) from += (from == null ? "0x"+KeyId : " [0x"+KeyId+"]");
      from = " from " + from;
    }

    string str;
    if(IsValid)
    {
      SignatureStatus flags = Status & SignatureStatus.ValidFlagMask;

      str = "Valid ";
      if(flags != 0)
      {
        str += "(but";
        if((flags & SignatureStatus.ExpiredSignature) != 0) str += " expired";
        if((flags & SignatureStatus.ExpiredKey) != 0) str += " keyExpired";
        if((flags & SignatureStatus.RevokedKey) != 0) str += " keyRevoked";
        str += ") ";
      }
      str += "signature"+from;
    }
    else if(IsInvalid)
    {
      str = "Invalid signature"+from;
    }
    else
    {
      SignatureStatus flags = Status & SignatureStatus.ErrorFlagMask;

      str = "Error occurred ";
      if(flags != 0)
      {
        str += "(";
        if((flags & SignatureStatus.UnsupportedAlgorithm) != 0) str += "unsupported algorithm";
        if((flags & SignatureStatus.MissingKey) != 0) str += "missing key";
        str += ") ";
      }
      str += "while verifying signature"+from;
    }

    return str;
  }

  string hashAlgorithm, keyFingerprint, keyId, keyType, primaryKeyFingerprint, userName;
  DateTime? timestamp, expiration;
  SignatureStatus status = SignatureStatus.Error;
  TrustLevel trustLevel = TrustLevel.Unknown;
}
#endregion
#endregion

#region Delegates
#region CardPinHandler
/// <include file="documentation.xml" path="/Security/PGPSystem/GetCardPin/*"/>
public delegate SecureString CardPinHandler(string cardType, string chvNumber, string serialNumber);
#endregion

#region CipherPasswordHandler
/// <include file="documentation.xml" path="/Security/PGPSystem/GetPlainPassword/*"/>
public delegate SecureString CipherPasswordHandler();
#endregion

#region KeyPasswordHandler
/// <include file="documentation.xml" path="/Security/PGPSystem/GetKeyPassword/*"/>
public delegate SecureString KeyPasswordHandler(string keyId, string userIdHint);
#endregion

#region PasswordInvalidHandler
/// <include file="documentation.xml" path="/Security/PGPSystem/OnPasswordInvalid/*"/>
public delegate void PasswordInvalidHandler(string keyId);
#endregion
#endregion

#region Miscellaneous types
#region PreferenceList
/// <summary>A collection for managing preference lists. It behaves like a normal list, except that it does not allow
/// duplicate entries.
/// </summary>
public class PreferenceList<T> : System.Collections.ObjectModel.Collection<T> where T : struct
{
  /// <summary>Called when an item is about to be inserted into the collection.</summary>
  protected override void InsertItem(int index, T item)
  {
    if(Contains(item)) throw new InvalidOperationException("The collection already contains " + item.ToString());

    base.InsertItem(index, item);
  }

  /// <summary>Called when an item is about to be set in the collection.</summary>
  protected override void SetItem(int index, T item)
  {
    for(int i=0; i<Count; i++)
    {
      if(i != index && item.Equals(this[i]))
      {
        throw new InvalidOperationException("The collection already contains " + item.ToString());
      }
    }

    base.SetItem(index, item);
  }
}
#endregion

#region ReadOnlyClass
/// <summary>Represents a class that allows its properties to be set until <see cref="MakeReadOnly"/> is called, at
/// which point the object becomes read-only.
/// </summary>
public abstract class ReadOnlyClass
{
  /// <include file="documentation.xml" path="/Security/ReadOnlyClass/MakeReadOnly/*"/>
  public virtual void MakeReadOnly()
  {
    readOnly = true;
  }

  /// <summary>Throws an exception if <see cref="MakeReadOnly"/> has been called.</summary>
  protected void AssertNotReadOnly()
  {
    if(readOnly) throw new InvalidOperationException("This object has been finished, and the property is read only.");
  }

  bool readOnly;
}
#endregion

#region TrustLevel
/// <summary>Key trust indicates the extent to which the owner(s) of a key are trusted to validate the ownership
/// of other people's keys.
/// </summary>
public enum TrustLevel
{
  /// <summary>You don't know how thoroughly the owner of this key validates others' keys.</summary>
  Unknown,
  /// <summary>You do not trust the owner of this key to do proper validation of others' keys.</summary>
  Never,
  /// <summary>You trust the owner of this key to do only marginal validation of others' keys.</summary>
  Marginal,
  /// <summary>You trust the owner of this key to do full validation of others' keys.</summary>
  Full,
  /// <summary>You ultimately trust the owner of this key, making them a new root in the web of trust. This should
  /// normally be set only for keys you personally own.
  /// </summary>
  Ultimate
}
#endregion

#region VerificationLevel
/// <summary>Indicates how thoroughly you have verified the ownership of a given key -- that is, what steps you have
/// taken to prove that the key actually belongs to the person named on it.
/// </summary>
public enum VerificationLevel
{
  /// <summary>You do not wish to provide an answer as to how thoroughly you've verified the ownership of the key.</summary>
  Nondisclosed,
  /// <summary>You have not verified the ownership of the key.</summary>
  None,
  /// <summary>You have performed casual verification of the key ownership.</summary>
  Casual,
  /// <summary>You have performed rigorous verification of the key ownership.</summary>
  Rigorous
}
#endregion
#endregion

#region OpenPGP enumeration values
#region OpenPGPAttributeType
/// <summary>Represents the user attribute types defined in the OpenPGP standard (RFC-4880). This is to help with
/// parsing OpenPGP packets.
/// </summary>
public enum OpenPGPAttributeType
{
  /// <summary>An user ID containing an image of the user.</summary>
  Image=1
}
#endregion

#region OpenPGPCipher
/// <summary>Represents the symmetric ciphers defined in the OpenPGP standard (RFC-4880). This is to help with parsing
/// OpenPGP packets.
/// </summary>
public enum OpenPGPCipher
{
  /// <summary>This value indicates that the message is not encrypted.</summary>
  Unencrypted=0,
  /// <summary>The IDEA cipher, corresponding to <see cref="SymmetricCipher.IDEA"/>.</summary>
  IDEA=1,
  /// <summary>The 3DES cipher, corresponding to <see cref="SymmetricCipher.TripleDES"/>.</summary>
  TripleDES=2,
  /// <summary>The CAST5 cipher, corresponding to <see cref="SymmetricCipher.CAST5"/>.</summary>
  CAST5=3,
  /// <summary>The Blowfish cipher, corresponding to <see cref="SymmetricCipher.Blowfish"/>.</summary>
  Blowfish=4,
  /// <summary>The sAFER-SK128 cipher.</summary>
  SAFER=5,
  /// <summary>The DES/SK cipher.</summary>
  DESSK=6,
  /// <summary>The AES cipher with a 128-bit key, corresponding to <see cref="SymmetricCipher.AES"/>.</summary>
  AES=7,
  /// <summary>The AES cipher with a 192-bit key, corresponding to <see cref="SymmetricCipher.AES192"/>.</summary>
  AES192=8,
  /// <summary>The AES cipher with a 256-bit key, corresponding to <see cref="SymmetricCipher.AES256"/>.</summary>
  AES256=9,
  /// <summary>The Twofish cipher, corresponding to <see cref="SymmetricCipher.Twofish"/>.</summary>
  Twofish=10,
}
#endregion

#region OpenPGPCompression
/// <summary>Represents the compression types defined in the OpenPGP standard (RFC-4880). This is to help with parsing
/// OpenPGP packets.
/// </summary>
public enum OpenPGPCompression
{
  /// <summary>A value indicating that the message is uncompressed.</summary>
  Uncompressed=0,
  /// <summary>The Zip algorith, corresponding to <see cref="Compression.Zip"/>.</summary>
  Zip=1,
  /// <summary>The Zlib algorith, corresponding to <see cref="Compression.Zlib"/>.</summary>
  Zlib=2,
  /// <summary>The Bzip2 algorith, corresponding to <see cref="Compression.Bzip2"/>.</summary>
  Bzip2=3
}
#endregion

#region OpenPGPHashAlgorithm
/// <summary>Represents the hash algorithms defined in the OpenPGP standard (RFC-4880). This is to help with parsing
/// OpenPGP packets.
/// </summary>
public enum OpenPGPHashAlgorithm
{
  /// <summary>The MD5 algorithm, corresponding to <see cref="HashAlgorithm.MD5"/>.</summary>
  MD5=1,
  /// <summary>The SHA1 algorithm, corresponding to <see cref="HashAlgorithm.SHA1"/>.</summary>
  SHA1=2,
  /// <summary>The RIPE-MD/160 algorithm, corresponding to <see cref="HashAlgorithm.RIPEMD160"/>.</summary>
  RIPEMD160=3,
  /// <summary>The MD2 algorithm.</summary>
  MD2=5,
  /// <summary>The TIGER/192 algorithm.</summary>
  TIGER192=6,
  /// <summary>The HAVAL algorithm.</summary>
  HAVAL=7,
  /// <summary>The SHA256 algorithm, corresponding to <see cref="HashAlgorithm.SHA256"/>.</summary>
  SHA256=8,
  /// <summary>The SHA384 algorithm, corresponding to <see cref="HashAlgorithm.SHA384"/>.</summary>
  SHA384=9,
  /// <summary>The SHA512 algorithm, corresponding to <see cref="HashAlgorithm.SHA512"/>.</summary>
  SHA512=10,
  /// <summary>The SHA224 algorithm, corresponding to <see cref="HashAlgorithm.SHA224"/>.</summary>
  SHA224=11
}
#endregion

#region OpenPGPImageType
/// <summary>Represents the image types defined in the OpenPGP standard (RFC-4880). This is to help with parsing
/// OpenPGP packets.
/// </summary>
public enum OpenPGPImageType
{
  /// <summary>The ISO 10918-1 JPEG format.</summary>
  Jpeg=1,
}
#endregion

#region OpenPGPKeyType
/// <summary>Represents the key types defined in the OpenPGP standard (RFC-4880). This is to help with parsing OpenPGP
/// packets.
/// </summary>
public enum OpenPGPKeyType
{
  /// <summary>An RSA key type, equivalent to <see cref="SubkeyType.RSA"/>.</summary>
  RSA=1,
  /// <summary>An RSA encryption-only key type, corresponding to <see cref="SubkeyType.RSAEncryptOnly"/>.</summary>
  RSAEncryptOnly=2,
  /// <summary>An RSA signing-only key type, corresponding to <see cref="SubkeyType.RSASignOnly"/>.</summary>
  RSASignOnly=3,
  /// <summary>An ElGamal encryption-only key type, corresponding to <see cref="SubkeyType.ElGamalEncryptOnly"/>.</summary>
  ElGamalEncryptOnly=16,
  /// <summary>A DSA signing key, corresponding to <see cref="SubkeyType.DSA"/>.</summary>
  DSA=17,
  /// <summary>An elliptic curve key.</summary>
  EllipticCurve=18,
  /// <summary>An elliptic curve DSA key.</summary>
  ECDSA=19,
  /// <summary>An ElGamal key that can be used for both signing and encryption. Because of security weaknesses, this
  /// should not be used.
  /// </summary>
  ElGamal=20,
  /// <summary>The Diffie Hellmen X9.42 key type, as defined for IETF-S/MIME.</summary>
  DiffieHellman=21
}
#endregion

#region OpenPGPSignatureType
/// <summary>Represents the signature types defined in the OpenPGP standard (RFC-4880). This is to help with parsing
/// OpenPGP packets.
/// </summary>
public enum OpenPGPSignatureType
{
  /// <summary>A signature of a canonical binary document. The signer owns the document, created it, or certifies that
  /// it has not been modified.
  /// </summary>
  CanonicalBinary=0,
  /// <summary>A signature of a canonical text document. The signer owns the document, created it, or certifies that
  /// it has not been modified. The signature is computed over the text document with line endings normalized to CRLF.
  /// </summary>
  CanonicalText=1,
  /// <summary>A signature of its own subpacket contents.</summary>
  Standalone=2,
  /// <summary>A signature on a key's user ID that makes no statement about how well the key's ownership has been
  /// verified.
  /// </summary>
  GenericCertification=0x10,
  /// <summary>A signature on a key's user ID which states that no verification of the key's ownership has been
  /// performed.
  /// </summary>
  PersonaCertification=0x11,
  /// <summary>A signature on a key's user ID which states that casual verification of the key's ownership has been
  /// performed.
  /// </summary>
  CasualCertification=0x12,
  /// <summary>A signature on a key's user ID which states that rigorous verification of the key's ownership has been
  /// performed.
  /// </summary>
  PositiveCertification=0x13,
  /// <summary>A statement by a primary key that it owns a given subkey.</summary>
  SubkeyBinding=0x18,
  /// <summary>A statement by a subkey that it is owned by the primary key.</summary>
  PrimaryKeyBinding=0x19,
  /// <summary>A signature on a key, usually not made by the key itself, that binds additional information to the key.</summary>
  DirectKeySignature=0x1f,
  /// <summary>A signature on a key, usually made by the key itself, that indicates that the key has been revoked.</summary>
  PrimaryKeyRevocation=0x20,
  /// <summary>A signature on a subkey, usually made by the primary key, thath indicates that the subkey has been
  /// revoked.
  /// </summary>
  SubkeyRevocation=0x28,
  /// <summary>A signature that revokes a certification signature (<see cref="GenericCertification"/>,
  /// <see cref="PersonaCertification"/>, <see cref="CasualCertification"/>, or <see cref="PositiveCertification"/>) or
  /// a <see cref="DirectKeySignature"/>.
  /// </summary>
  CertificateRevocation=0x30,
  /// <summary>A signature that is only useful for its embedded timestamp.</summary>
  TimestampSignature=0x40,
  /// <summary>A signature over some arbitrary OpenPGP packets, certifying that the packets have not been altered.</summary>
  ConfirmationSignature=0x50
}
#endregion
#endregion

#region PGPSystem
/// <summary>This class represents a connection to a PGP encryption and key-management system, such as PGP or GPG.</summary>
public abstract class PGPSystem
{
  /// <summary>An event that is raised when a key password is invalid, to allow client applications to flush their
  /// password cache, if they're using one. Applications should not request a new password in response to this event.
  /// </summary>
  public event PasswordInvalidHandler KeyPasswordInvalid;

  /// <summary>An event that is raised when a secret key password needs to be obtained from the user.</summary>
  public event CardPinHandler CardPinNeeded;

  /// <summary>An event that is raised when a plain password needs to be obtained from the user.</summary>
  public event CipherPasswordHandler PlainPasswordNeeded;

  /// <summary>An event that is raised when a secret key password needs to be obtained from the user.</summary>
  public event KeyPasswordHandler KeyPasswordNeeded;

  #region Configuration
  /// <include file="documentation.xml" path="/Security/PGPSystem/GetSupportedCiphers/*"/>
  public abstract string[] GetSupportedCiphers();
  
  /// <include file="documentation.xml" path="/Security/PGPSystem/GetSupportedCompressions/*"/>
  public abstract string[] GetSupportedCompressions();
  
  /// <include file="documentation.xml" path="/Security/PGPSystem/GetSupportedHashes/*"/>
  public abstract string[] GetSupportedHashes();
  
  /// <include file="documentation.xml" path="/Security/PGPSystem/GetSupportedKeyTypes/*"/>
  public abstract string[] GetSupportedKeyTypes();
  #endregion

  #region Encryption and signing
  /// <include file="documentation.xml" path="/Security/PGPSystem/Encrypt/*"/>
  public void Encrypt(Stream sourceData, Stream destination, EncryptionOptions encryptionOptions)
  {
    Encrypt(sourceData, destination, encryptionOptions, null);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/Encrypt/*"/>
  public void Encrypt(Stream sourceData, Stream destination, EncryptionOptions encryptionOptions,
                      OutputOptions outputOptions)
  {
    SignAndEncrypt(sourceData, destination, null, encryptionOptions, outputOptions);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/Sign/*"/>
  public void Sign(Stream sourceData, Stream destination, SigningOptions signingOptions)
  {
    Sign(sourceData, destination, signingOptions, null);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/Sign/*"/>
  public void Sign(Stream sourceData, Stream destination, SigningOptions signingOptions, OutputOptions outputOptions)
  {
    SignAndEncrypt(sourceData, destination, signingOptions, null, outputOptions);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/SignAndEncrypt/*[@name != 'outputOptions']"/>
  public void SignAndEncrypt(Stream sourceData, Stream destination, SigningOptions signingOptions,
                                      EncryptionOptions encryptionOptions)
  {
    SignAndEncrypt(sourceData, destination, signingOptions, encryptionOptions, null);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/SignAndEncrypt/*"/>
  public abstract void SignAndEncrypt(Stream sourceData, Stream destination, SigningOptions signingOptions,
                                      EncryptionOptions encryptionOptions, OutputOptions outputOptions);

  /// <summary>Decrypts the given ciphertext and writes the result to the given destination, using the default
  /// decryption options. Signatures embedded in the ciphertext are also verified and returned.
  /// </summary>
  public Signature[] Decrypt(Stream ciphertext, Stream destination)
  {
    return Decrypt(ciphertext, destination, null);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/Decrypt/*"/>
  public abstract Signature[] Decrypt(Stream ciphertext, Stream destination, DecryptionOptions options);

  /// <summary>Verifies embedded signatures in the given signed data, using the default verification options. The
  /// signed data should not have been simultaneously encrypted. To verify signatures in encrypted, signed data, call
  /// <see cref="Decrypt(Stream,Stream)"/> with a destination stream of <see cref="Stream.Null"/>.
  /// </summary>
  public Signature[] Verify(Stream signedData)
  {
    return Verify(signedData, (VerificationOptions)null);
  }

  /// <summary>Verifies detached signatures for the given signed data, using the default verification options.</summary>
  public Signature[] Verify(Stream signature, Stream signedData)
  {
    return Verify(signature, signedData, null);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/Verify2/*"/>
  public abstract Signature[] Verify(Stream signedData, VerificationOptions options);
  
  /// <include file="documentation.xml" path="/Security/PGPSystem/Verify3/*"/>
  public abstract Signature[] Verify(Stream signature, Stream signedData, VerificationOptions options);
  #endregion

  #region Primary key management
  /// <include file="documentation.xml" path="/Security/PGPSystem/AddDesignatedRevoker/*" />
  public abstract void AddDesignatedRevoker(PrimaryKey key, PrimaryKey revokerKey);

  /// <include file="documentation.xml" path="/Security/PGPSystem/AddPhoto2/*" />
  public virtual void AddPhoto(PrimaryKey key, Image image, UserPreferences preferences)
  {
    if(key == null || image == null) throw new ArgumentNullException();

    string jpegFilename = Path.GetTempFileName();
    try
    {
      using(FileStream stream = new FileStream(jpegFilename, FileMode.Open, FileAccess.ReadWrite))
      {
        image.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
        stream.Position = 0;
        AddPhoto(key, stream, OpenPGPImageType.Jpeg, preferences);
      }
    }
    finally { File.Delete(jpegFilename); }
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/AddPhoto4/*" />
  public abstract void AddPhoto(PrimaryKey key, Stream image, OpenPGPImageType imageFormat,
                                UserPreferences preferences);

  /// <include file="documentation.xml" path="/Security/PGPSystem/AddUserId/*" />
  public abstract void AddUserId(PrimaryKey key, string realName, string email, string comment,
                                 UserPreferences preferences);

  /// <include file="documentation.xml" path="/Security/PGPSystem/AddSubkey/*" />
  public abstract void AddSubkey(PrimaryKey key, string keyType, int keyLength, DateTime? expiration);

  /// <include file="documentation.xml" path="/Security/PGPSystem/ChangeExpiration/*" />
  public abstract void ChangeExpiration(Key key, DateTime? expiration);

  /// <include file="documentation.xml" path="/Security/PGPSystem/ChangePassword/*" />
  public abstract void ChangePassword(PrimaryKey key, SecureString password);

  /// <summary>Cleans the given key by removing unusable signatures and user IDs from them.</summary>
  /// <include file="documentation.xml" path="/Security/PGPSystem/KeyNotUpdatedImmediately/*"/>
  public void CleanKey(PrimaryKey key)
  {
    if(key == null) throw new ArgumentNullException();
    CleanKeys(new PrimaryKey[] { key });
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/CleanKeys/*" />
  public abstract void CleanKeys(PrimaryKey[] keys);

  /// <summary>Minimizes the given key by removing all signatures except the most recent self-signature, and removes
  /// unusable user IDs.
  /// </summary>
  /// <include file="documentation.xml" path="/Security/PGPSystem/KeyNotUpdatedImmediately/*"/>
  public void MinimizeKey(PrimaryKey key)
  {
    if(key == null) throw new ArgumentNullException();
    MinimizeKeys(new PrimaryKey[] { key });
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/MinimizeKeys/*" />
  public abstract void MinimizeKeys(PrimaryKey[] keys);

  /// <include file="documentation.xml" path="/Security/PGPSystem/CreateKey/*"/>
  public abstract PrimaryKey CreateKey(NewKeyOptions options);

  /// <include file="documentation.xml" path="/Security/PGPSystem/DisableKeys/*" />
  public abstract void DisableKeys(PrimaryKey[] keys);

  /// <include file="documentation.xml" path="/Security/PGPSystem/EnableKeys/*" />
  public abstract void EnableKeys(PrimaryKey[] keys);

  /// <include file="documentation.xml" path="/Security/PGPSystem/DeleteAttributes/*" />
  public abstract void DeleteAttributes(UserAttribute[] attributes);

  /// <summary>Deletes the given primary key, or a part of it, from its keyring.</summary>
  /// <param name="key">The primary key to delete.</param>
  /// <param name="deletion">The portion of the key to delete.</param>
  /// <include file="documentation.xml" path="/Security/PGPSystem/KeyNotUpdatedImmediately/*"/>
  public void DeleteKey(PrimaryKey key, KeyDeletion deletion)
  {
    DeleteKeys(new PrimaryKey[] { key }, deletion);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/DeleteKeys/*"/>
  public abstract void DeleteKeys(PrimaryKey[] keys, KeyDeletion deletion);

  /// <include file="documentation.xml" path="/Security/PGPSystem/DeleteSignatures/*" />
  public abstract void DeleteSignatures(KeySignature[] signatures);

  /// <include file="documentation.xml" path="/Security/PGPSystem/DeleteSubkeys/*" />
  public abstract void DeleteSubkeys(Subkey[] subkeys);

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetPreferences/*" />
  public abstract UserPreferences GetPreferences(UserAttribute user);

  /// <include file="documentation.xml" path="/Security/PGPSystem/RevokeAttributes/*" />
  public abstract void RevokeAttributes(UserAttribute[] attributes);

  /// <include file="documentation.xml" path="/Security/PGPSystem/RevokeKeys/*" />
  public abstract void RevokeKeys(PrimaryKey[] keys);

  /// <include file="documentation.xml" path="/Security/PGPSystem/RevokeSignatures/*" />
  public abstract void RevokeSignatures(KeySignature[] signatures);

  /// <include file="documentation.xml" path="/Security/PGPSystem/RevokeSubkeys/*" />
  public abstract void RevokeSubkeys(Subkey[] subkeys);

  /// <include file="documentation.xml" path="/Security/PGPSystem/SetPreferences/*" />
  public abstract void SetPreferences(UserAttribute user, UserPreferences preferences);

  /// <include file="documentation.xml" path="/Security/PGPSystem/SetTrustLevel/*" />
  public abstract void SetTrustLevel(PrimaryKey key, TrustLevel trust);

  /// <include file="documentation.xml" path="/Security/PGPSystem/SignKey/*" />
  public abstract void SignKey(PrimaryKey keyToSign, PrimaryKey signingKey, KeySigningOptions options);

  /// <include file="documentation.xml" path="/Security/PGPSystem/SignUser/*"/>
  public abstract void SignKey(UserId userId, PrimaryKey signingKey, KeySigningOptions options);

  /// <summary>Searches for the public keys with the given fingerprint in the given keyring.</summary>
  /// <param name="fingerprint">The fingerprints of the key to search for.</param>
  /// <param name="keyring">The keyring to search, or null to search the default keyring.</param>
  /// <param name="options">Options controlling how keys should be returned.</param>
  /// <returns>Returns the key if it was found, or null if it was not.</returns>
  public PrimaryKey FindPublicKey(string fingerprint, Keyring keyring, ListOptions options)
  {
    PrimaryKey[] keys = FindPublicKeys(new string[] { fingerprint },
                                       keyring == null ? null : new Keyring[] { keyring }, keyring == null, options);
    return keys[0];
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/FindPublicKeys/*[@name != 'keyrings' and @name != 'includeDefaultKeyring']"/>
  /// <param name="keyring">The keyring to search, or null to search the default keyring.</param>
  public PrimaryKey[] FindPublicKeys(string[] fingerprints, Keyring keyring, ListOptions options)
  {
    return FindPublicKeys(fingerprints, keyring == null ? null : new Keyring[] { keyring }, keyring == null, options);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/FindPublicKeys/*"/>
  public abstract PrimaryKey[] FindPublicKeys(string[] fingerprints, Keyring[] keyrings, bool includeDefaultKeyring,
                                              ListOptions options);

  /// <summary>Searches for the secret keys with the given fingerprint in the given keyring.</summary>
  /// <param name="fingerprint">The fingerprints of the key to search for.</param>
  /// <param name="keyring">The keyring to search, or null to search the default keyring.</param>
  /// <param name="options">Options controlling how keys should be returned.</param>
  /// <returns>Returns the key if it was found, or null if it was not.</returns>
  public PrimaryKey FindSecretKey(string fingerprint, Keyring keyring, ListOptions options)
  {
    PrimaryKey[] keys = FindSecretKeys(new string[] { fingerprint },
                                       keyring == null ? null : new Keyring[] { keyring }, keyring == null, options);
    return keys[0];
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/FindSecretKeys/*[@name != 'keyrings' and @name != 'includeDefaultKeyring']"/>
  /// <param name="keyring">The keyring to search, or null to search the default keyring.</param>
  public PrimaryKey[] FindSecretKeys(string[] fingerprints, Keyring keyring, ListOptions options)
  {
    return FindSecretKeys(fingerprints, keyring == null ? null : new Keyring[] { keyring }, keyring == null, options);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/FindSecretKeys/*"/>
  public abstract PrimaryKey[] FindSecretKeys(string[] fingerprints, Keyring[] keyrings, bool includeDefaultKeyring,
                                              ListOptions options);

  /// <summary>Gets all public keys in the default keyring, without retrieving key signatures.</summary>
  public PrimaryKey[] GetPublicKeys()
  {
    return GetPublicKeys(ListOptions.Default);
  }

  /// <summary>Gets all public keys in the default keyring.</summary>
  public PrimaryKey[] GetPublicKeys(ListOptions options)
  {
    return GetPublicKeys(null, true, options);
  }

  /// <summary>Gets all public keys in the given keyring, without retrieving key signatures.</summary>
  public PrimaryKey[] GetPublicKeys(Keyring keyring)
  {
    return GetPublicKeys(keyring, ListOptions.Default);
  }

  /// <summary>Gets all public keys in the given keyring.</summary>
  public PrimaryKey[] GetPublicKeys(Keyring keyring, ListOptions options)
  {
    return GetPublicKeys(keyring == null ? null : new Keyring[] { keyring }, keyring == null, options);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetPublicKeys2/*"/>
  public abstract PrimaryKey[] GetPublicKeys(Keyring[] keyrings, bool includeDefaultKeyring, ListOptions options);

  /// <summary>Gets all secret keys in the default keyring.</summary>
  public PrimaryKey[] GetSecretKeys()
  {
    return GetSecretKeys(null, true);
  }

  /// <summary>Gets all secret keys in the given keyring.</summary>
  public PrimaryKey[] GetSecretKeys(Keyring keyring)
  {
    return keyring == null ? GetSecretKeys(null, true) : GetSecretKeys(new Keyring[] { keyring }, false);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetSecretKeys2/*"/>
  public abstract PrimaryKey[] GetSecretKeys(Keyring[] keyrings, bool includeDefaultKeyring);

  /// <summary>Refreshes the given key by reloading it from its keyring database and returning the updated key, or
  /// null if the key no longer exists on its keyring. The key will be retrieved with the default
  /// <see cref="ListOptions"/>.
  /// </summary>
  public PrimaryKey RefreshKey(PrimaryKey key)
  {
    return RefreshKey(key, ListOptions.Default);
  }

  /// <summary>Refreshes the given key by reloading it from its keyring database and returning the updated key, or
  /// null if the key no longer exists on its keyring.
  /// </summary>
  public PrimaryKey RefreshKey(PrimaryKey key, ListOptions options)
  {
    if(key == null) throw new ArgumentNullException();
    return key.Secret ?
      FindSecretKey(key.Fingerprint, key.Keyring, options) : FindPublicKey(key.Fingerprint, key.Keyring, options);
  }

  /// <summary>Exports the given public key to the given stream.</summary>
  public void ExportPublicKey(PrimaryKey key, Stream destination)
  {
    ExportPublicKey(key, destination, ExportOptions.Default, null);
  }

  /// <summary>Exports the given public key to the given stream.</summary>
  public void ExportPublicKey(PrimaryKey key, Stream destination, ExportOptions exportOptions)
  {
    ExportPublicKey(key, destination, exportOptions, null);
  }

  /// <summary>Exports the given public key to the given stream.</summary>
  public void ExportPublicKey(PrimaryKey key, Stream destination, ExportOptions exportOptions,
                              OutputOptions outputOptions)
  {
    ExportPublicKeys(new PrimaryKey[] { key }, destination, exportOptions, outputOptions);
  }

  /// <summary>Exports the given public keys to the given stream.</summary>
  public void ExportPublicKeys(PrimaryKey[] keys, Stream destination)
  {
    ExportPublicKeys(keys, destination, ExportOptions.Default, null);
  }

  /// <summary>Exports the given public keys to the given stream.</summary>
  public void ExportPublicKeys(PrimaryKey[] keys, Stream destination, ExportOptions exportOptions)
  {
    ExportPublicKeys(keys, destination, exportOptions, null);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ExportPublicKeys/*"/>
  public abstract void ExportPublicKeys(PrimaryKey[] keys, Stream destination, ExportOptions exportOptions,
                                        OutputOptions outputOptions);

  /// <summary>Exports all public keys in the given keyring files and/or the default keyring to the given stream.</summary>
  public void ExportPublicKeys(Keyring[] keyrings, bool includeDefaultKeyring, Stream destination)
  {
    ExportPublicKeys(keyrings, includeDefaultKeyring, destination, ExportOptions.Default, null);
  }

  /// <summary>Exports all public keys in the given keyring files and/or the default keyring to the given stream.</summary>
  public void ExportPublicKeys(Keyring[] keyrings, bool includeDefaultKeyring, Stream destination,
                               ExportOptions options)
  {
    ExportPublicKeys(keyrings, includeDefaultKeyring, destination, options, null);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ExportPublicKeys/*"/>
  public abstract void ExportPublicKeys(Keyring[] keyrings, bool includeDefaultKeyring, Stream destination,
                                        ExportOptions exportOptions, OutputOptions outputOptions);

  /// <summary>Exports the secret and public portion of the given key to the given stream.</summary>
  public void ExportSecretKey(PrimaryKey key, Stream destination)
  {
    ExportSecretKey(key, destination, ExportOptions.Default, null);
  }

  /// <summary>Exports the secret and public portion of the given key to the given stream.</summary>
  public void ExportSecretKey(PrimaryKey key, Stream destination, ExportOptions exportOptions)
  {
    ExportSecretKey(key, destination, exportOptions, null);
  }

  /// <summary>Exports the secret and public portion of the given key to the given stream.</summary>
  public void ExportSecretKey(PrimaryKey key, Stream destination, ExportOptions exportOptions,
                              OutputOptions outputOptions)
  {
    ExportSecretKeys(new PrimaryKey[] { key }, destination, exportOptions, outputOptions);
  }

  /// <summary>Exports the given secret and public keys to the given stream.</summary>
  public void ExportSecretKeys(PrimaryKey[] keys, Stream destination)
  {
    ExportSecretKeys(keys, destination, ExportOptions.Default, null);
  }

  /// <summary>Exports the given secret and public keys to the given stream.</summary>
  public void ExportSecretKeys(PrimaryKey[] keys, Stream destination, ExportOptions exportOptions)
  {
    ExportSecretKeys(keys, destination, exportOptions, null);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ExportSecretKeys/*"/>
  public abstract void ExportSecretKeys(PrimaryKey[] keys, Stream destination, ExportOptions exportOptions,
                                        OutputOptions outputOptions);

  /// <summary>Exports all secret keys in the given keyring files and/or the default keyring to the given stream.</summary>
  public void ExportSecretKeys(Keyring[] keyrings, bool includeDefaultKeyring, Stream destination)
  {
    ExportSecretKeys(keyrings, includeDefaultKeyring, destination, ExportOptions.Default, null);
  }

  /// <summary>Exports all secret keys in the given keyring files and/or the default keyring to the given stream.</summary>
  public void ExportSecretKeys(Keyring[] keyrings, bool includeDefaultKeyring, Stream destination,
                               ExportOptions exportOptions)
  {
    ExportSecretKeys(keyrings, includeDefaultKeyring, destination, exportOptions, null);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ExportSecretKeys/*"/>
  public abstract void ExportSecretKeys(Keyring[] keyrings, bool includeDefaultKeyring, Stream destination,
                                        ExportOptions exportOptions, OutputOptions outputOptions);

  /// <summary>Imports keys from the given source into the default keyring.</summary>
  public ImportedKey[] ImportKeys(Stream source)
  {
    return ImportKeys(source, null, ImportOptions.Default);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ImportKeys2/*"/>
  public ImportedKey[] ImportKeys(Stream source, ImportOptions options)
  {
    return ImportKeys(source, null, options);
  }

  /// <summary>Imports keys from the given source into the given keyring.</summary>
  public ImportedKey[] ImportKeys(Stream source, Keyring keyring)
  {
    return ImportKeys(source, keyring, ImportOptions.Default);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ImportKeys3/*"/>
  public abstract ImportedKey[] ImportKeys(Stream source, Keyring keyring, ImportOptions options);
  #endregion

  #region Miscellaneous
  /// <include file="documentation.xml" path="/Security/PGPSystem/CreatePublicKeyring/*"/>
  public virtual void CreatePublicKeyring(string path)
  {
    new FileStream(path, FileMode.Create, FileAccess.Write).Dispose();
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/CreateSecretKeyring/*"/>
  public virtual void CreateSecretKeyring(string path)
  {
    new FileStream(path, FileMode.Create, FileAccess.Write).Dispose();
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/CreateTrustDatabase/*"/>
  public virtual void CreateTrustDatabase(string path)
  {
    new FileStream(path, FileMode.Create, FileAccess.Write).Dispose();
  }

  /// <summary>Returns the given number of random bytes, with a degree of randomness suitable for session key
  /// generation and other secure tasks, but not key pair generation.
  /// </summary>
  public byte[] GetRandomData(int byteCount)
  {
    return GetRandomData(Randomness.Strong, byteCount);
  }

  /// <summary>Returns the given number of random bytes, with the given degree of randomness.</summary>
  public byte[] GetRandomData(Randomness quality, int byteCount)
  {
    byte[] buffer = new byte[byteCount];
    GetRandomData(quality, buffer, 0, byteCount);
    return buffer;
  }

  /// <summary>Generates random data and writes it to the given buffer, with a degree of randomness suitable for
  /// session key generation and other secure tasks, but not key pair generation.
  /// </summary>
  public void GetRandomData(byte[] buffer, int index, int count)
  {
    GetRandomData(Randomness.Strong, buffer, index, count);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GenerateRandomData/*"/>
  public abstract void GetRandomData(Randomness quality, byte[] buffer, int index, int count);

  /// <include file="documentation.xml" path="/Security/PGPSystem/Hash/*"/>
  public abstract byte[] Hash(Stream data, string hashAlgorithm);
  #endregion

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetCardPin/*"/>
  protected virtual SecureString GetCardPin(string cardType, string chvNumber, string serialNumber)
  {
    return CardPinNeeded != null ? CardPinNeeded(cardType, chvNumber, serialNumber) : null;
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetPlainPassword/*"/>
  protected virtual SecureString GetPlainPassword()
  {
    return PlainPasswordNeeded != null ? PlainPasswordNeeded() : null;
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetKeyPassword/*"/>
  protected virtual SecureString GetKeyPassword(string keyId, string userIdHint)
  {
    return KeyPasswordNeeded != null ? KeyPasswordNeeded(keyId, userIdHint) : null;
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/OnPasswordInvalid/*"/>
  protected virtual void OnInvalidPassword(string keyId)
  {
    if(KeyPasswordInvalid != null) KeyPasswordInvalid(keyId);
  }
}
#endregion

} // namespace AdamMil.GPG