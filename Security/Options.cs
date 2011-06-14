/*
AdamMil.Security is a .NET library providing OpenPGP-based security.
http://www.adammil.net/
Copyright (C) 2008-2011 Adam Milazzo

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
using System.Security;

namespace AdamMil.Security.PGP
{

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

#region CertificationLevel
/// <summary>Indicates how thoroughly you have verified the ownership of a given key -- that is, what steps you have
/// taken to prove that the key actually belongs to the person named on it.
/// </summary>
public enum CertificationLevel
{
  /// <summary>You do not wish to provide an answer as to how thoroughly you've verified the ownership of the key.</summary>
  Undisclosed,
  /// <summary>You have not verified the ownership of the key.</summary>
  None,
  /// <summary>You have performed casual verification of the key ownership. If you know the owner personally, you may
  /// have received the key from him in person on a CD or USB key, or verified the key ID or fingerprint over the
  /// phone. If you don't know the owner personally, then you may have verified the key fingerprint with the owner and
  /// checked the key's user and photo IDs against the owner's government-issued photo ID.
  /// </summary>
  Casual,
  /// <summary>You have performed rigorous verification of the key ownership. For somebody you know personally, this
  /// may be exchanging the keys in person, on a CD or USB key, and verifying the fingerprints in person. If you don't
  /// know the owner personally, then you may have checked the key's user and photo IDs against a hard-to-forge
  /// document such as a passport and verified, by exchange of email, that the email address on the key is controlled
  /// by the same person.
  /// </summary>
  Rigorous
}
#endregion

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
  public EncryptionOptions(params PrimaryKey[] recipients)
  {
    foreach(PrimaryKey recipient in recipients) Recipients.Add(recipient);
  }

  /// <summary>Initializes a new <see cref="EncryptionOptions"/> object with the given symmetric cipher password.</summary>
  public EncryptionOptions(SecureString password)
  {
    Password = password;
  }

  /// <summary>Gets or sets whether recipients are always trusted. If false, trust issues with recipients can cause
  /// the encryption to fail. If true, trust issues with recipients will be ignored. The default is false. The normal
  /// way to resolve a trust issue is to verify that the recipient actually owns the key, and then sign it using
  /// <see cref="PGPSystem.SignKey(PrimaryKey,PrimaryKey,KeySigningOptions)"/>.
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
  public KeyCollection<PrimaryKey> Recipients
  {
    get { return recipients; }
  }

  /// <summary>Specifies the public keys of the hidden recipients of the message. The IDs of the recipients keys will
  /// not be included in the message, and their client software will not know which key to use when decrypting the
  /// ciphertext. This can be cumbersome, requiring every key to be tried by the recipients, but it prevents the
  /// obvious association of their key IDs with the data.
  /// </summary>
  public KeyCollection<PrimaryKey> HiddenRecipients
  {
    get { return hiddenRecipients; }
  }

  readonly KeyCollection<PrimaryKey> recipients = new KeyCollection<PrimaryKey>(KeyCapabilities.Encrypt);
  readonly KeyCollection<PrimaryKey> hiddenRecipients = new KeyCollection<PrimaryKey>(KeyCapabilities.Encrypt);
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
  Default=ExportPublicKeys,
  /// <summary>The public portions of keys will be exported.</summary>
  ExportPublicKeys=0x1,
  /// <summary>The secret portions of keys will be exported.</summary>
  ExportSecretKeys=0x2,
  /// <summary>The public and secret portions of keys will be exported.</summary>
  ExportPublicAndSecretKeys=ExportPublicKeys | ExportSecretKeys,
  /// <summary>Key signatures marked as "local only" will be exported. Normally, they are skipped.</summary>
  ExportLocalSignatures=0x4,
  /// <summary>Attribute user IDs (eg, photo IDs) will not be included in the output.</summary>
  ExcludeAttributes=0x8,
  /// <summary>Includes revoker information that was marked as sensitive.</summary>
  ExportSensitiveRevokerInfo=0x10,
  /// <summary>When exporting secret keys, this option causes the secret portion of the primary key to not be exported.
  /// Only the secret subkeys are exported. This is not OpenPGP compliant and currently only GPG is known to
  /// implement this option or be capable of importing keys created by this option.
  /// </summary>
  ClobberPrimarySecretKey=0x20,
  /// <summary>When exporting secret subkeys, resets their passwords to empty.</summary>
  ResetSubkeyPassword=0x40,
  /// <summary>Does not export unusable signatures, and does not export any signatures for unusable user IDs.</summary>
  CleanKeys=0x80,
  /// <summary>Exports only the most recent self-signature on each user ID.</summary>
  MinimizeKeys=0x100,
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

#region KeyServerOptions
/// <summary>Options that control access to a keyserver.</summary>
public abstract class KeyServerOptions
{
  /// <summary>Gets or sets the key server that will be used.</summary>
  public Uri KeyServer
  {
    get { return keyServer; }
    set { keyServer = value; }
  }

  /// <summary>Gets or sets the proxy to use for HTTP-based key servers, or null to use the default proxy.</summary>
  public Uri HttpProxy
  {
    get { return httpProxy; }
    set { httpProxy = value; }
  }

  /// <summary>Gets or sets the number of seconds to attempt a key server action before giving up, or zero to use the
  /// default timeout.
  /// </summary>
  public int Timeout
  {
    get { return timeout; }
    set
    {
      if(value < 0) throw new ArgumentOutOfRangeException();
      timeout = value;
    }
  }

  Uri keyServer, httpProxy;
  int timeout;
}
#endregion

#region KeyDownloadOptions
/// <summary>Options that control how keys are downloaded from key servers.</summary>
public class KeyDownloadOptions : KeyServerOptions
{
  /// <summary>Initializes a new <see cref="KeyDownloadOptions"/> with the default options.</summary>
  public KeyDownloadOptions() { }

  /// <summary>Initializes a new <see cref="KeyDownloadOptions"/> with the given key server.</summary>
  public KeyDownloadOptions(Uri keyServer)
  {
    KeyServer = keyServer;
  }

  /// <summary>Initializes a new <see cref="KeyDownloadOptions"/> with the given options.</summary>
  public KeyDownloadOptions(Uri keyServer, ImportOptions importOptions)
  {
    KeyServer     = keyServer;
    ImportOptions = importOptions;
  }

  /// <summary>Initializes a new <see cref="KeyDownloadOptions"/> with the given options.</summary>
  public KeyDownloadOptions(Uri keyServer, ImportOptions importOptions, bool ignorePreferredKeyServer)
  {
    KeyServer     = keyServer;
    ImportOptions = importOptions;
    IgnorePreferredKeyServer = ignorePreferredKeyServer;
  }

  /// <summary>Gets or sets whether the preferred key server of the keys being downloaded will be ignored. If true, all
  /// keys will be retrieved from the key server named in <see cref="KeyServerOptions.KeyServer"/>. If false, the
  /// preferred key server for a key being downloaded will be used if it's known. THe default is false.
  /// </summary>
  public bool IgnorePreferredKeyServer
  {
    get { return ignorePreferred; }
    set { ignorePreferred = value; }
  }

  /// <summary>Gets or sets the import options that control how the downloaded keys will be imported.</summary>
  public ImportOptions ImportOptions
  {
    get { return importOptions; }
    set { importOptions = value; }
  }

  ImportOptions importOptions;
  bool ignorePreferred;
}
#endregion

#region KeyUploadOptions
/// <summary>Options that control how keys are uploaded to key servers.</summary>
public class KeyUploadOptions : KeyServerOptions
{
  /// <summary>Initializes a new <see cref="KeyUploadOptions"/> with the default options.</summary>
  public KeyUploadOptions() { }

  /// <summary>Initializes a new <see cref="KeyUploadOptions"/> with the given key server.</summary>
  public KeyUploadOptions(Uri keyServer)
  {
    KeyServer = keyServer;
  }

  /// <summary>Initializes a new <see cref="KeyUploadOptions"/> with the given options.</summary>
  public KeyUploadOptions(Uri keyServer, ExportOptions exportOptions)
  {
    KeyServer     = keyServer;
    ExportOptions = exportOptions;
  }

  /// <summary>Gets or sets the import options that control how the downloaded keys will be imported.</summary>
  public ExportOptions ExportOptions
  {
    get { return exportOptions; }
    set { exportOptions = value; }
  }

  ExportOptions exportOptions;
}
#endregion

#region KeyRevocationCode
/// <summary>Gives the reason for the revocation of a key.</summary>
public enum KeyRevocationCode
{
  /// <summary>No reason was given for the revocation, although there might be a textual explanation.</summary>
  Unspecified=0,
  /// <summary>The key is being replaced by a new key.</summary>
  KeySuperceded=1,
  /// <summary>The key may have been compromised.</summary>
  KeyCompromised=2,
  /// <summary>The key is no longer used.</summary>
  KeyRetired=3,
}
#endregion

#region KeyRevocationReason
/// <summary>Options that control how keys are revoked.</summary>
public class KeyRevocationReason
{
  /// <summary>Initializes a new <see cref="KeyRevocationReason"/> object with the default options.</summary>
  public KeyRevocationReason() { }

  /// <summary>Initializes a new <see cref="KeyRevocationReason"/> object with the given reason and explanation.</summary>
  public KeyRevocationReason(KeyRevocationCode reason, string explanation)
  {
    this.reason      = reason;
    this.explanation = explanation;
  }

  /// <summary>Gets or sets a human-readable string explaining the reason for the revocation.</summary>
  public string Explanation
  {
    get { return explanation; }
    set { explanation = value; }
  }

  /// <summary>Gets or sets the machine-readable reason for the revocation.</summary>
  public KeyRevocationCode Reason
  {
    get { return reason; }
    set { reason = value; }
  }

  string explanation;
  KeyRevocationCode reason;
}
#endregion

#region KeySearchHandler
/// <summary>A callback for the key search that is passed a chunk of matching keys.</summary>
/// <param name="keys">The keys found in this chunk. The keys will not be complete keys, but will only have minimal
/// fields filled out. To use the keys, they should first be downloaded from the keyserver with
/// <see cref="PGPSystem.ImportKeysFromServer(KeyDownloadOptions,Keyring,string[])"/>.
/// </param>
/// <returns>Returns true to continue the search and false to stop it.</returns>
public delegate bool KeySearchHandler(PrimaryKey[] keys);
#endregion

#region KeySigningOptions
/// <summary>Options to control the signing of others' keys and attributes.</summary>
public class KeySigningOptions
{
  /// <summary>Initializes a new <see cref="KeySigningOptions"/> with the default values.</summary>
  public KeySigningOptions() { }

  /// <summary>Initializes a new <see cref="KeySigningOptions"/> with the given values.</summary>
  public KeySigningOptions(CertificationLevel certLevel, bool exportable)
  {
    CertificationLevel = certLevel;
    Exportable         = exportable;
  }

  /// <summary>Gets or sets how thoroughly you have verified the person named on the key is its actual owner.</summary>
  public CertificationLevel CertificationLevel
  {
    get { return certLevel; }
    set { certLevel = value; }
  }

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
  /// property is limited to three values: <see cref="PGP.TrustLevel.Unknown"/>, <see cref="PGP.TrustLevel.Marginal"/>,
  /// and <see cref="PGP.TrustLevel.Full"/>. If set to <see cref="PGP.TrustLevel.Unknown"/> (the default), a standard
  /// signature will be created. If set to <see cref="PGP.TrustLevel.Marginal"/> or <see cref="PGP.TrustLevel.Full"/>,
  /// a trust signature will be created, which signifies that the user is trusted to issue signatures with any lower
  /// trust level.
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

  /// <summary>Gets or sets the depth of the trust, as a positive integer. This property only takes effect if
  /// <see cref="TrustLevel"/> is not <see cref="PGP.TrustLevel.Unknown"/>. A value greater than one allows the signed
  /// key to make trust signatures on your behalf. In general, a key signed with a depth of trust D can make trust
  /// signatures with a trust depth of at most D-1.
  /// </summary>
  public int TrustDepth
  {
    get { return trustDepth; }
    set
    {
      if(value < 1) throw new ArgumentOutOfRangeException();
      trustDepth = value;
    }
  }

  /// <summary>Gets or sets the trust domain, as a regular expression in the same format as Henry Spencer's "almost
  /// public domain" regular expression package (see RFC-4880 section 5.2.3.14). Only signatures by the signed key on
  /// user IDs that match the regular expression have trust extended to them. If empty or null, there will be no
  /// restriction on trusted user IDs.
  /// </summary>
  public string TrustDomain
  {
    get { return trustDomain; }
    set { trustDomain = value; }
  }

  string trustDomain;
  int trustDepth = 1;
  CertificationLevel certLevel;
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
  /// <summary>Signatures on keys will be retrieved, causing <see cref="UserAttribute.Signatures"/> to be filled but
  /// not verified.
  /// </summary>
  RetrieveSignatures=1,
  /// <summary>Signatures on keys will be retrieved, causing <see cref="UserAttribute.Signatures"/> to be filled and
  /// verified.
  /// </summary>
  VerifySignatures=3,
  /// <summary>A mask that can be ANDed with a <see cref="ListOptions"/> to get the signature handling value, which is
  /// one of <see cref="IgnoreSignatures"/>, <see cref="RetrieveSignatures"/>, or <see cref="VerifySignatures"/>.
  /// </summary>
  SignatureMask=3,

  /// <summary>User attributes on keys will be ignored.</summary>
  IgnoreAttributes=0,
  /// <summary>User attributes will be retrieved, causing the <see cref="PrimaryKey.Attributes"/> property to be
  /// filled.
  /// </summary>
  RetrieveAttributes=4,

  /// <summary>The secret portions of keys will be ignored.</summary>
  IgnoreSecretKeys=0,
  /// <summary>The secret portions of keys will be retrieved, causing the <see cref="PrimaryKey.HasSecretKey"/>
  /// property to be valid.
  /// </summary>
  RetrieveSecretKeys=8,
  /// <summary>Only keys that have secret portions will be retrieved.</summary>
  RetrieveOnlySecretKeys=24,
  /// <summary>A mask that can be ANDed with a <see cref="ListOptions"/> to get the secret key handling value, which is
  /// one of <see cref="IgnoreSecretKeys"/>, <see cref="RetrieveSecretKeys"/>, or <see cref="RetrieveOnlySecretKeys"/>.
  /// </summary>
  SecretKeyMask=24,

  /// <summary>Unusable keys (expired, revoked, disabled, etc) will not be returned.</summary>
  IgnoreUnusableKeys=32,
  /// <summary>Even keys that are unusable (expired, revoked, disabled, etc) will be returned.</summary>
  RetrieveUnusableKeys=0,

  /// <summary>Retrieves attributes, signatures, and secret keys, ignoring unusable keys.</summary>
  RetrieveAllUsable=RetrieveAttributes | RetrieveSignatures | RetrieveSecretKeys | IgnoreUnusableKeys,
  /// <summary>Retrieves and verifies attributes, signatures, and secret keys, ignoring unusable keys.</summary>
  VerifyAllUsable=RetrieveAttributes | VerifySignatures | RetrieveSecretKeys | IgnoreUnusableKeys,

  /// <summary>Retrieves attributes, signatures, and secret keys, including unusable keys.</summary>
  RetrieveAll=RetrieveAttributes | RetrieveSignatures | RetrieveSecretKeys | RetrieveUnusableKeys,
  /// <summary>Retrieves and verifies attributes, signatures, and secret keys, including unusable keys.</summary>
  VerifyAll=RetrieveAttributes | VerifySignatures | RetrieveSecretKeys | RetrieveUnusableKeys,
}
#endregion

#region NewKeyOptions
/// <summary>Options that control how a new primary key should be created.</summary>
public class NewKeyOptions
{
  /// <summary>Gets or sets the name of the primary key type. This can be a member of <see cref="PGP.KeyType"/>, or
  /// another key type, but it's best to leave it at the default value of <see cref="PGP.KeyType.Default"/>, which
  /// specifies that a default key type will be used.
  /// </summary>
  public string KeyType
  {
    get { return keyType; }
    set { keyType = value; }
  }

  /// <summary>Gets or sets the length of the primary key, in bits. If set to zero, a default value will be used.</summary>
  public int KeyLength
  {
    get { return keyLength; }
    set { keyLength = value; }
  }

  /// <summary>Gets or sets the capabilities of the primary key. OpenPGP requires that all primary keys be capable of
  /// certification, so attempting to set this property to a value that is not equal to <see
  /// cref="PGP.KeyCapabilities.Default"/> and does not include the <see cref="PGP.KeyCapabilities.Certify"/> and <see
  /// cref="PGP.KeyCapabilities.Sign"/> flags (because certification requires the ability to sign) will cause an
  /// exception to be thrown. Not all capabilites are supported by all key types.
  /// </summary>
  public KeyCapabilities KeyCapabilities
  {
    get { return keyCapabilities; }
    set
    {
      if(value != 0 && (value & KeyCapabilities.Certify) == 0)
      {
        throw new ArgumentException("The OpenPGP standard requires that all primary keys support certification, so "+
                                    "Certify and Sign flags must be passed.");
      }
      else if((value & (KeyCapabilities.Certify|KeyCapabilities.Sign)) == KeyCapabilities.Certify)
      {
        throw new ArgumentException("The Certify capability requires the Sign capability.");
      }

      keyCapabilities = value;
    }
  }

  /// <summary>Gets or sets the expiration of the primary key. This must be a time in the future.</summary>
  public DateTime? KeyExpiration
  {
    get { return keyExpiration; }
    set { keyExpiration = value; }
  }

  /// <summary>Gets or sets the name of the subkey type. This can be a member of <see cref="PGP.KeyType"/>, or
  /// another key type, but it's best to leave it at the default value of <see cref="PGP.KeyType.Default"/>, which
  /// specifies that a default key type will be used. If set to <see cref="PGP.KeyType.None"/>, no subkey will be
  /// created.
  /// </summary>
  /// <remarks>Multiple subkeys can be associated with a primary key. If set to a value other than
  /// <see cref="PGP.KeyType.None"/>, this property causes a subkey to be created along with the primary key.
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

  /// <summary>Gets or sets the expiration of the subkey. This must be a time in the future.</summary>
  public DateTime? SubkeyExpiration
  {
    get { return subkeyExpiration; }
    set { subkeyExpiration = value; }
  }

  /// <summary>Gets or sets the capabilities of the subkey. Passing the <see cref="PGP.KeyCapabilities.Certify"/> flag
  /// will cause an exception to be thrown, because only the primary key can be used for certification. Not all
  /// capabilites are supported by all subkey types.
  /// </summary>
  public KeyCapabilities SubkeyCapabilities
  {
    get { return subkeyCapabilities; }
    set
    {
      if((value & KeyCapabilities.Certify) != 0)
      {
        throw new ArgumentException("The Certify capability is only allowed on the primary key.");
      }

      subkeyCapabilities = value;
    }
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
      if(value != null && value.SecretFile == null)
      {
        throw new ArgumentException("The keyring must have both public and secret parts.");
      }
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
  KeyCapabilities keyCapabilities = KeyCapabilities.Default, subkeyCapabilities = KeyCapabilities.Default;
  DateTime? keyExpiration, subkeyExpiration;
  string keyType = PGP.KeyType.Default, subkeyType = PGP.KeyType.Default;
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

  /// <summary>Initializes a new <see cref="OutputOptions"/> object with the given format.</summary>
  public OutputOptions(OutputFormat format)
  {
    Format = format;
  }

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
  /// <summary>This is the fastest generator. Despite the name, the generator is still cryptographically strong, but it
  /// is only pseudo-random.
  /// </summary>
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
  public SigningOptions(params PrimaryKey[] signers)
  {
    foreach(PrimaryKey key in signers) Signers.Add(key);
  }

  /// <summary>Initializes a new <see cref="SigningOptions"/> object with the given <see cref="SignatureType"/> and
  /// list of signing keys.
  /// </summary>
  public SigningOptions(SignatureType type, params PrimaryKey[] signers)
  {
    Type = type;
    foreach(PrimaryKey key in signers) Signers.Add(key);
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
  public KeyCollection<PrimaryKey> Signers
  {
    get { return signers; }
  }

  /// <summary>Gets or sets the type of signature to create. The default is <see cref="SignatureType.Embedded"/>.</summary>
  public SignatureType Type
  {
    get { return type; }
    set { type = value; }
  }

  readonly KeyCollection<PrimaryKey> signers = new KeyCollection<PrimaryKey>(KeyCapabilities.Sign);
  string hash = HashAlgorithm.Default;
  SignatureType type = SignatureType.Embedded;
}
#endregion

#region SignatureType
/// <summary>Determines the type of a data signature to be made.</summary>
public enum SignatureType
{
  /// <summary>Signs arbitrary data, and outputs an OpenPGP message containing both the original data and the
  /// signature.
  /// </summary>
  Embedded,
  /// <summary>Signs arbitrary data, and outputs an OpenPGP message containing only the signature. To verify the
  /// signature, the original data will need to be presented again.
  /// </summary>
  Detached,
  /// <summary>Signs text data, and outputs a text document containing the original text and an embedded OpenPGP
  /// message that holds the signature. The benefit of a clear-signed text is that the original text can be read by
  /// humans without any special software. OpenPGP software is only needed to verify the signature. The text may be
  /// modified to normalize end-of-line whitespace and line endings for platform independence.
  /// </summary>
  ClearSignedText
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

#region UserPreferences
/// <summary>Stores the preferences of a user, as associated with a user ID or attribute.</summary>
public class UserPreferences
{
  /// <summary>Initializes a new <see cref="UserPreferences"/> object with the default preferences.</summary>
  public UserPreferences() { }

  /// <summary>Initializes a new <see cref="UserPreferences"/> object with the given value for <see cref="Primary"/>.</summary>
  public UserPreferences(bool primary)
  {
    Primary = primary;
  }

  /// <summary>Gets or sets the user's preferred key server, or null if no key server is preferred.</summary>
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

#region UserRevocationCode
/// <summary>Gives the reason for the revocation of a user ID.</summary>
public enum UserRevocationCode
{
  /// <summary>No reason was given for the revocation, although there might be a textual explanation.</summary>
  Unspecified=0,
  /// <summary>The user ID is no longer valid (eg, email address, job, or name changed, etc).</summary>
  IdNoLongerValid=32
}
#endregion

#region UserRevocationReason
/// <summary>Options that control how user IDs are revoked.</summary>
public class UserRevocationReason
{
  /// <summary>Initializes a new <see cref="UserRevocationReason"/> object with the default options.</summary>
  public UserRevocationReason() { }

  /// <summary>Initializes a new <see cref="UserRevocationReason"/> object with the given reason and explanation.</summary>
  public UserRevocationReason(UserRevocationCode reason, string explanation)
  {
    this.reason      = reason;
    this.explanation = explanation;
  }

  /// <summary>Gets or sets a human-readable string explaining the reason for the revocation.</summary>
  public string Explanation
  {
    get { return explanation; }
    set { explanation = value; }
  }

  /// <summary>Gets or sets the machine-readable reason for the revocation.</summary>
  public UserRevocationCode Reason
  {
    get { return reason; }
    set { reason = value; }
  }

  string explanation;
  UserRevocationCode reason;
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

} // namespace AdamMil.Security.PGP