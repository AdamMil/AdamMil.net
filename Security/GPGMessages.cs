/*
AdamMil.Security is a .NET library providing OpenPGP-based security.
http://www.adammil.net/
Copyright (C) 2008-2010 Adam Milazzo

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
using System.Globalization;

namespace AdamMil.Security.PGP.GPG.StatusMessages
{

#region StatusMessageType
/// <summary>Indicates the type of GPG status message that was received.</summary>
enum StatusMessageType
{
  NewSig, GoodSig, ExpiredSig, ExpiredKeySig, RevokedKeySig, BadSig, ErrorSig, ValidSig, EncTo, NoData, UnexpectedData,
  TrustUndefined, TrustNever, TrustMarginal, TrustFully, TrustUltimate, PKATrustGood, PKATrustBad, KeyExpired,
  KeyRevoked, BadArmor, NeedKeyPassphrase, NeedCipherPassphrase, NeedPin, MissingPassphrase, BadPassphrase,
  GoodPassphrase, DecryptionFailed, DecryptionOkay, NoPublicKey, NoSecretKey, Imported, ImportOkay, ImportProblem, 
  ImportResult, FileStart, FileDone, BeginDecryption, EndDecryption, BeginEncryption, EndEncryption, BeginSigning, 
  DeleteFailed, Progress, SigCreated, KeyCreated, KeyNotCreated, SessionKey, UserIdHint, InvalidRecipient,
  NoRecipients, Error, CardControl, BackupKeyCreated, GoodMDC, GetBool, GetLine, GetHidden, Attribute,
}
#endregion

#region StatusMessage
/// <summary>The base class for GPG status messages.</summary>
abstract class StatusMessage
{
  protected StatusMessage(StatusMessageType type)
  {
    this.type = type;
  }

  /// <summary>Gets the type of the message.</summary>
  public StatusMessageType Type
  {
    get { return type; }
  }

  readonly StatusMessageType type;
}
#endregion

#region Key import messages
#region KeyImportReason
/// <summary>Gives the reasons that a key was created or updated during an import.</summary>
[Flags]
enum KeyImportReason
{
  /// <summary>This value means that the key was not actually changed (and there was no reason for the import).</summary>
  NotChanged=0,
  /// <summary>This flag indicates that the key was entirely new.</summary>
  NewKey=1,
  /// <summary>This flag indicates that the key contained new user IDs.</summary>
  NewUserId=2,
  /// <summary>This flag indicates that the key contained new signatures.</summary>
  NewSignature=4,
  /// <summary>This flag indicates that the key contained new subkeys.</summary>
  NewSubkey=8,
  /// <summary>This flag indicates that the key contained a secret key.</summary>
  ContainsSecretKey=16
}
#endregion

#region ImportFailureReason
/// <summary>Gives the reason that a key could not be imported.</summary>
enum ImportFailureReason
{
  /// <summary>The import failed for an unspecified reason</summary>
  Unknown,
  /// <summary>The certificate is invalid.</summary>
  InvalidCertificate,
  /// <summary>The issuer certificate is missing.</summary>
  IssuerCertificateMissing,
  /// <summary>The certificate chain is too long.</summary>
  CertificateChainTooLong,
  /// <summary>There was an error saving the certificate to disk.</summary>
  ErrorStoringCertificate
}
#endregion

#region KeySigImportedMessage
/// <summary>A message that gives key ID and the signer name of the key signature just imported.</summary>
sealed class KeySigImportedMessage : StatusMessage
{
  public KeySigImportedMessage(string[] arguments) : base(StatusMessageType.Imported) 
  {
    keyId    = arguments[0].ToUpperInvariant();
    userName = arguments[1];
  }

  /// <summary>Gets the key ID of the key signature just imported.</summary>
  public string KeyId
  {
    get { return keyId; }
  }

  /// <summary>Gets the name of the signer who signed the key.</summary>
  public string UserName
  {
    get { return userName; }
  }

  readonly string keyId, userName;
}
#endregion

#region KeyImportOkayMessage
/// <summary>This message indicates that a key was successfully created or updated during a key import.</summary>
sealed class KeyImportOkayMessage : StatusMessage
{
  public KeyImportOkayMessage(string[] arguments) : base(StatusMessageType.ImportOkay)
  {
    reason      = (KeyImportReason)int.Parse(arguments[0], CultureInfo.InvariantCulture);
    fingerprint = arguments.Length < 2 ? null : arguments[1].ToUpperInvariant();
  }

  /// <summary>Gets the fingerprint of the key that was created or updated, or null if it is not specified.</summary>
  public string Fingerprint
  {
    get { return fingerprint; }
  }

  /// <summary>Gets the reasons the key was created or updated.</summary>
  public KeyImportReason Reason
  {
    get { return reason; }
  }

  readonly string fingerprint;
  readonly KeyImportReason reason;
}
#endregion

#region KeyImportFailedMessage
/// <summary>Issued when a key failed to import.</summary>
sealed class KeyImportFailedMessage : StatusMessage
{
  public KeyImportFailedMessage(string[] arguments) : base(StatusMessageType.ImportProblem)
  {
    reason      = (ImportFailureReason)int.Parse(arguments[0], CultureInfo.InvariantCulture);
    fingerprint = arguments.Length < 2 ? null : arguments[1].ToUpperInvariant();
  }

  /// <summary>Gets the fingerprint of the key that could not be imported, or null if it is not known.</summary>
  public string Fingerprint
  {
    get { return fingerprint; }
  }

  /// <summary>Gets the reason the key could not be imported.</summary>
  public ImportFailureReason Reason
  {
    get { return reason; }
  }

  readonly string fingerprint;
  readonly ImportFailureReason reason;
}
#endregion

#region KeyImportResultsMessage
/// <summary>This message gives the end results of a key import process.</summary>
sealed class KeyImportResultsMessage : StatusMessage
{
  public KeyImportResultsMessage(string[] arguments) : base(StatusMessageType.ImportResult)
  {
    totalKeys          = int.Parse(arguments[0]);
    keysWithoutUserIds = int.Parse(arguments[1]);
    keysImported       = int.Parse(arguments[2]);
    keysUnchanged      = int.Parse(arguments[4]);
    newUserIds         = int.Parse(arguments[5]);
    newSubkeys         = int.Parse(arguments[6]);
    newSigs            = int.Parse(arguments[7]);
    newRevocations     = int.Parse(arguments[8]);
    secretsRead        = int.Parse(arguments[9]);
    secretsImported    = int.Parse(arguments[10]);
    secretsUnchanged   = int.Parse(arguments[11]);
    keysNotImported    = int.Parse(arguments[12]);
  }

  /// <summary>Gets the number of keys that were missing user IDs.</summary>
  public int KeysWithoutUserIds
  {
    get { return keysWithoutUserIds; }
  }

  /// <summary>Gets the number of new revocation certificates imported.</summary>
  public int NewRevocations
  {
    get { return newRevocations; }
  }

  /// <summary>Gets the number of new key signatures.</summary>
  public int NewSignatures
  {
    get { return newSigs; }
  }

  /// <summary>Gets the number of new subkeys.</summary>
  public int NewSubkeys
  {
    get { return newSubkeys; }
  }

  /// <summary>Gets the number of new user IDs.</summary>
  public int NewUserIds
  {
    get { return newUserIds; }
  }

  /// <summary>Gets the number of public keys imported.</summary>
  public int PublicKeysImported
  {
    get { return keysImported; }
  }

  /// <summary>Gets the number of secret keys imported.</summary>
  public int SecretKeysImported
  {
    get { return secretsImported; }
  }

  /// <summary>Gets the number of secret keys read.</summary>
  public int SecretKeysProcessed
  {
    get { return secretsRead; }
  }

  /// <summary>Gets the number of secret keys that were unchanged because they already existed in the keyring.</summary>
  public int SecretKeysUnchanged
  {
    get { return secretsUnchanged; }
  }

  /// <summary>Gets the total number of keys that were processed.</summary>
  public int TotalKeysProcessed
  {
    get { return totalKeys; }
  }

  /// <summary>Gets the number keys that were unchanged because they already existed in the keyring.</summary>
  public int UnchangedKeys
  {
    get { return keysUnchanged; }
  }

  /// <summary>Gets the number of keys that were not imported.</summary>
  public int UnimportedKeys
  {
    get { return keysNotImported; }
  }

  readonly int totalKeys, keysWithoutUserIds, keysImported, keysUnchanged, newUserIds, newSubkeys,
               newSigs, newRevocations, secretsRead, secretsImported, secretsUnchanged, keysNotImported;
}
#endregion
#endregion

#region Signature verification messages
#region KeyIdAndNameSigVerifyMessage
/// <summary>A base class for signature verification messages with a key ID and user name as the only arguments. There
/// are several such messages.
/// </summary>
abstract class KeyIdAndNameSigVerifyMessage : StatusMessage
{
  protected KeyIdAndNameSigVerifyMessage(StatusMessageType type, string[] arguments) : base(type)
  {
    keyId    = arguments[0].ToUpperInvariant();
    userName = string.Join(" ", arguments, 1, arguments.Length-1);
  }

  /// <summary>Gets the ID of the signing key used, as a hex string. Note that key IDs are not unique.</summary>
  public string KeyId
  {
    get { return keyId; }
  }

  /// <summary>Gets a human-readable description of the signer's identity.</summary>
  public string UserName
  {
    get { return userName; }
  }

  string keyId, userName;
}
#endregion

#region GoodSigMessage
/// <summary>Issued when a good signature is detected. For each signature, only one of <see cref="GoodSigMessage"/>,
/// <see cref="BadSigMessage"/>, or <see cref="ErrorSigMessage"/> will be issued. For good signatures, the
/// <see cref="ValidSigMessage"/> will also be emitted, and contains more useful information such as the fingerprints
/// of the keys, the timestamp of the signature, etc.
/// </summary>
sealed class GoodSigMessage : KeyIdAndNameSigVerifyMessage
{
  public GoodSigMessage(string[] arguments) : base(StatusMessageType.GoodSig, arguments) { }
}
#endregion

#region ExpiredSigMessage
/// <summary>Issued along with <see cref="GoodSigMessage"/> to indicate that although the signature is good, it has
/// expired.
/// </summary>
sealed class ExpiredSigMessage : KeyIdAndNameSigVerifyMessage
{
  public ExpiredSigMessage(string[] arguments) : base(StatusMessageType.ExpiredSig, arguments) { }
}
#endregion

#region ExpiredKeySigMessage
/// <summary>Issued along with <see cref="GoodSigMessage"/> to indicate that although the signature is good, it was
/// made with a key that has expired.
/// </summary>
sealed class ExpiredKeySigMessage : KeyIdAndNameSigVerifyMessage
{
  public ExpiredKeySigMessage(string[] arguments) : base(StatusMessageType.ExpiredKeySig, arguments) { }
}
#endregion

#region RevokedKeySigMessage
/// <summary>Issued along with <see cref="GoodSigMessage"/> to indicate that although the signature is good, it was
/// made with a key that has been revoked.
/// </summary>
sealed class RevokedKeySigMessage : KeyIdAndNameSigVerifyMessage
{
  public RevokedKeySigMessage(string[] arguments) : base(StatusMessageType.RevokedKeySig, arguments) { }
}
#endregion

#region BadSigMessage
/// <summary>Issued when a bad signature is detected. For each signature, only one of <see cref="GoodSigMessage"/>,
/// <see cref="BadSigMessage"/>, or <see cref="ErrorSigMessage"/> will be issued.
/// </summary>
sealed class BadSigMessage : KeyIdAndNameSigVerifyMessage
{
  public BadSigMessage(string[] arguments) : base(StatusMessageType.BadSig, arguments) { }
}
#endregion

#region ErrorSigMessage
/// <summary>Issued when an error prevented the signature from being verified. For each signature, only one of
/// <see cref="GoodSigMessage"/>, <see cref="BadSigMessage"/>, or <see cref="ErrorSigMessage"/> will be issued.
/// </summary>
sealed class ErrorSigMessage : StatusMessage
{
  public ErrorSigMessage(string[] arguments) : base(StatusMessageType.ErrorSig)
  {
    keyId     = arguments[0].ToUpperInvariant();
    keyType   = GPG.ParseKeyType(arguments[1]);
    hashAlgo  = GPG.ParseHashAlgorithm(arguments[2]);
    timestamp = GPG.ParseTimestamp(arguments[4]);

    int reason = int.Parse(arguments[5]);
    if(reason == 4) unsupportedAlgo = true;
    else if(reason == 9) missingKey = true;
  }

  /// <summary>Gets the ID of the signing key used, as a hex string. Note that key IDs are not unique.</summary>
  public string KeyId
  {
    get { return keyId; }
  }

  /// <summary>Gets the hash algorithm used to sign the message, or null if the hash algorithm is unknown. Note that
  /// the hash algorithm is not necessarily supported by GPG.
  /// </summary>
  public string HashAlgorithm
  {
    get { return hashAlgo; }
  }

  /// <summary>Gets the type of the key used to sign the message, or null if the key type is unknown. Note that the
  /// key type is not necessarily supported by GPG.
  /// </summary>
  public string KeyType
  {
    get { return keyType; }
  }

  /// <summary>Gets the time when the signature was created.</summary>
  public DateTime Timestamp
  {
    get { return timestamp; }
  }

  /// <summary>Gets whether the error was due to an inability to locate the public key of the signer.</summary>
  public bool MissingKey
  {
    get { return missingKey; }
  }

  /// <summary>Gets whether the error was due to the key type or hash algorithm being unsupported.</summary>
  public bool UnsupportedAlgorithm
  {
    get { return unsupportedAlgo; }
  }

  readonly string keyId, keyType, hashAlgo;
  readonly DateTime timestamp;
  readonly bool missingKey, unsupportedAlgo;
}
#endregion

#region ValidSigMessage
/// <summary>Issued along with a <see cref="GoodSigMessage"/> to provide more information about the signature.</summary>
sealed class ValidSigMessage : StatusMessage
{
  public ValidSigMessage(string[] arguments) : base(StatusMessageType.ValidSig)
  {
    sigFingerprint     = arguments[0].ToUpperInvariant();
    sigTime            = GPG.ParseTimestamp(arguments[2]);
    sigExpiration      = GPG.ParseNullableTimestamp(arguments[3]);
    keyType            = GPG.ParseKeyType(arguments[6]);
    hashAlgo           = GPG.ParseHashAlgorithm(arguments[7]);
    primaryFingerprint = arguments[9].ToUpperInvariant();
  }

  /// <summary>Gets the fingerprint of the signing key, as a hex string.</summary>
  public string SignatureKeyFingerprint
  {
    get { return sigFingerprint; }
  }

  /// <summary>Gets the fingerprint of the signing key's associated primary key, as a hex string. This may be equal to
  /// <see cref="SignatureKeyFingerprint"/> if the signing key was the primary key.
  /// </summary>
  public string PrimaryKeyFingerprint
  {
    get { return primaryFingerprint; }
  }

  /// <summary>Gets the hash algorithm used to sign the message, or null if the hash algorithm is unknown. Note that
  /// the hash algorithm is not necessarily supported by GPG.
  /// </summary>
  public string HashAlgorithm
  {
    get { return hashAlgo; }
  }

  /// <summary>Gets the type of the key used to sign the message, or null if the key type is unknown. Note that the
  /// key type is not necessarily supported by GPG.
  /// </summary>
  public string KeyType
  {
    get { return keyType; }
  }

  /// <summary>Gets the time when the signature was made.</summary>
  public DateTime SignatureTime
  {
    get { return sigTime; }
  }

  /// <summary>Gets the time when the signature will expire, or null if the signature does not expire.</summary>
  public DateTime? SignatureExpiration
  {
    get { return sigExpiration; }
  }

  readonly string sigFingerprint, primaryFingerprint, keyType, hashAlgo;
  readonly DateTime sigTime;
  readonly DateTime? sigExpiration;
}
#endregion
#endregion

#region Other messages
#region GenericMessage
/// <summary>A generic message with no properties.</summary>
sealed class GenericMessage : StatusMessage
{
  public GenericMessage(StatusMessageType type) : base(type) { }
}
#endregion

#region GenericKeyIdMessage
/// <summary>A class for messages that contain only a key ID parameter.</summary>
sealed class GenericKeyIdMessage : StatusMessage
{
  public GenericKeyIdMessage(StatusMessageType type, string[] arguments) : base(type)
  {
    keyId = arguments[0].ToUpperInvariant();
  }

  /// <summary>Gets the related key ID.</summary>
  public string KeyId
  {
    get { return keyId; }
  }

  readonly string keyId;
}
#endregion

#region GetInputMessage
/// <summary>A class for messages that request input from the user.</summary>
sealed class GetInputMessage : StatusMessage
{
  public GetInputMessage(StatusMessageType type, string[] arguments) : base(type)
  {
    promptId = arguments[0];
  }

  /// <summary>Gets a string representing the type of information requested.</summary>
  public string PromptId
  {
    get { return promptId; }
  }

  readonly string promptId;
}
#endregion

#region AttributeMessage
/// <summary>A message issued when an attribute is about to be written to the attribute file descriptor.</summary>
sealed class AttributeMessage : StatusMessage
{
  public AttributeMessage(string[] arguments) : base(StatusMessageType.Attribute)
  {
    length = int.Parse(arguments[1]);
    type   = (OpenPGPAttributeType)int.Parse(arguments[2]);
    creation = GPG.ParseNullableTimestamp(arguments[5]);
    expiration = GPG.ParseNullableTimestamp(arguments[6]);

    int flags = int.Parse(arguments[7]);
    primary = (flags & 1) != 0;
    revoked = (flags & 2) != 0;
    expired = (flags & 4) != 0;
  }

  public OpenPGPAttributeType AttributeType
  {
    get { return type; }
  }

  public DateTime? CreationTime
  {
    get { return creation; }
  }

  public DateTime? ExpirationTime
  {
    get { return expiration; }
  }

  public bool IsExpired
  {
    get { return expired; }
  }

  public bool IsPrimary
  {
    get { return primary; }
  }

  public bool IsRevoked
  {
    get { return revoked; }
  }

  /// <summary>Gets whether the attribute is valid (ie, has a valid self-signature).</summary>
  public bool IsValid
  {
    get { return creation.HasValue; }
  }

  /// <summary>Gets the length of the attribute data, in bytes.</summary>
  public int Length
  {
    get { return length; }
  }

  readonly DateTime? creation, expiration;
  readonly int length;
  readonly OpenPGPAttributeType type;
  readonly bool primary, revoked, expired;
}
#endregion

#region BadPassphraseMessage
/// <summary>Indicates that the previously-requested password was wrong.</summary>
sealed class BadPassphraseMessage : StatusMessage
{
  public BadPassphraseMessage(string[] arguments) : base(StatusMessageType.BadPassphrase) 
  {
    keyId = arguments[0].ToUpperInvariant();
  }

  /// <summary>Gets the key ID whose password was incorrectly given.</summary>
  public string KeyId
  {
    get { return keyId; }
  }

  readonly string keyId;
}
#endregion

#region DeleteFailureReason
/// <summary>Describes the reason that a key deletion failed.</summary>
enum DeleteFailureReason
{
  Unknown=0, NoSuchKey=1, MustDeleteSecretKeyFirst=2, AmbiguousKey=3
}
#endregion

#region DeleteFailedMessage
/// <summary>A message given when a key deletion fails.</summary>
sealed class DeleteFailedMessage : StatusMessage
{
  public DeleteFailedMessage(string[] arguments) : base(StatusMessageType.DeleteFailed)
  {
    reason = (DeleteFailureReason)int.Parse(arguments[0]);
  }

  /// <summary>Gets the reason the key deletion failed.</summary>
  public DeleteFailureReason Reason
  {
    get { return reason; }
  }

  readonly DeleteFailureReason reason;
}
#endregion

#region InvalidRecipientReason
/// <summary>Indicates the reason why an encryption recipient was rejected.</summary>
enum InvalidRecipientReason
{
  /// <summary>No reason was given for the failure.</summary>
  None,
  /// <summary>The recipient could not be found.</summary>
  NotFound,
  /// <summary>The recipient was specified ambiguously.</summary>
  AmbiguousSpecification,
  /// <summary>The key was used incorrectly.</summary>
  WrongUsage,
  /// <summary>The key was revoked.</summary>
  KeyRevoked,
  /// <summary>The key was expired.</summary>
  KeyExpired,
  /// <summary>The key has no known certificate revocation list.</summary>
  NoRevocationList,
  /// <summary>The key's certificate revocation list is too old.</summary>
  RevocationListTooOld,
  /// <summary>A policy mismatch occurred.</summary>
  PolicyMismatch,
  /// <summary>The key is not a secret key.</summary>
  NotSecret,
  /// <summary>The key is not trusted.</summary>
  NotTrusted
}
#endregion

#region InvalidRecipientMessage
/// <summary>Indicates that a recipient was invalid.</summary>
sealed class InvalidRecipientMessage : StatusMessage
{
  public InvalidRecipientMessage(string[] arguments) : base(StatusMessageType.InvalidRecipient)
  {
    reason    = (InvalidRecipientReason)int.Parse(arguments[0], CultureInfo.InvariantCulture);
    recipient = string.Join(" ", arguments, 1, arguments.Length-1);

    switch(reason)
    {
      case InvalidRecipientReason.AmbiguousSpecification:
        reasonText = "The recipient was ambiguous.";
        break;
      case InvalidRecipientReason.KeyExpired:
        reasonText = "The recipient's key expired.";
        break;
      case InvalidRecipientReason.KeyRevoked:
        reasonText = "The recipient's key was revoked.";
        break;
      case InvalidRecipientReason.NoRevocationList:
        reasonText = "The recipient's key has no known certificate revocation list.";
        break;
      case InvalidRecipientReason.NotFound:
        reasonText = "The recipient was not found.";
        break;
      case InvalidRecipientReason.NotSecret:
        reasonText = "The recipient's key is not a secret key.";
        break;
      case InvalidRecipientReason.NotTrusted:
        reasonText = "The recipient is not trusted.";
        break;
      case InvalidRecipientReason.PolicyMismatch:
        reasonText = "A policy mismatch occurred.";
        break;
      case InvalidRecipientReason.RevocationListTooOld:
        reasonText = "The recipient's key's certificate revocation list is too old.";
        break;
      case InvalidRecipientReason.WrongUsage:
        reasonText = "The recipient's key was not intended for this usage.";
        break;
      default:
        reasonText = "An unknown failure occurred.";
        break;
    }
  }

  /// <summary>Gets the reason why the recipient was invalid.</summary>
  public InvalidRecipientReason Reason
  {
    get { return reason; }
  }

  /// <summary>Gets a description of the reason why the recipient was invalid.</summary>
  public string ReasonText
  {
    get { return reasonText; }
  }

  /// <summary>Gets the invalid recipient, as specified on the command line (a key fingerprint in this case).</summary>
  public string Recipient
  {
    get { return recipient; }
  }

  readonly string recipient, reasonText;
  readonly InvalidRecipientReason reason;
}
#endregion

#region KeyCreatedMessage
/// <summary>A message issued when a key was successfully created.</summary>
sealed class KeyCreatedMessage : StatusMessage
{
  public KeyCreatedMessage(string[] arguments) : base(StatusMessageType.KeyCreated)
  {
    switch(arguments[0][0])
    {
      case 'B': primaryCreated = subkeyCreated = true; break;
      case 'P': primaryCreated = true; break;
      case 'S': subkeyCreated = true; break;
    }

    fingerprint = arguments.Length > 1 ? arguments[1].ToUpperInvariant() : null;
  }

  /// <summary>Gets the fingerprint of the key created. If <see cref="PrimaryKeyCreated"/> is true, this is the
  /// fingerprint of the primary key. If <see cref="SubkeyCreated"/> is true, this is the fingerprint of the subkey.
  /// </summary>
  public string Fingerprint
  {
    get { return fingerprint; }
  }

  /// <summary>Gets whether a primary key was created.</summary>
  public bool PrimaryKeyCreated
  {
    get { return primaryCreated; }
  }

  /// <summary>Gets whether a subkey was created.</summary>
  public bool SubkeyCreated
  {
    get { return subkeyCreated; }
  }

  readonly string fingerprint;
  readonly bool primaryCreated, subkeyCreated;
}
#endregion

#region NeedPassphraseMessage
/// <summary>Issued when a password is needed to unlock a secret key.</summary>
sealed class NeedKeyPassphraseMessage : StatusMessage
{
  public NeedKeyPassphraseMessage(string[] arguments) : base(StatusMessageType.NeedKeyPassphrase)
  {
    primaryKeyId = arguments[0].ToUpperInvariant();
    keyId        = arguments[1].ToUpperInvariant();
  }

  /// <summary>Gets the ID of the key for which the password is needed.</summary>
  public string KeyId
  {
    get { return keyId; }
  }
  
  /// <summary>Gets the ID of the primary key that owns the key for which the password is needed.</summary>
  public string PrimaryKeyId
  {
    get { return primaryKeyId; }
  }

  readonly string primaryKeyId, keyId;
}
#endregion

#region TrustLevelMessage
/// <summary>Indicates the trust level of something. For instance, as part of a signature verification, this represents
/// how trustworthy the signature is.
/// </summary>
sealed class TrustLevelMessage : StatusMessage
{
  /// <summary> a new <see cref="TrustLevelMessage"/> with the given <see cref="StatusMessageType"/>
  /// representing the trust level message to which it corresponds.
  /// </summary>
  public TrustLevelMessage(StatusMessageType type) : base(type)
  {
    switch(type)
    {
      case StatusMessageType.TrustFully: level = TrustLevel.Full; break;
      case StatusMessageType.TrustMarginal: level = TrustLevel.Marginal; break;
      case StatusMessageType.TrustNever: level = TrustLevel.Never; break;
      case StatusMessageType.TrustUltimate: level = TrustLevel.Ultimate; break;
      default: level = TrustLevel.Unknown; break;
    }
  }

  /// <summary>Gets the trust level.</summary>
  public TrustLevel Level
  {
    get { return level; }
  }

  readonly TrustLevel level;
}
#endregion

#region UserIdHintMessage
/// <summary>Gives a hint about the user ID for a given primary key ID. This usually precedes password requests; the
/// hint can be displayed to help the users remember which password to enter.
/// </summary>
sealed class UserIdHintMessage : StatusMessage
{
  public UserIdHintMessage(string[] arguments) : base(StatusMessageType.UserIdHint)
  {
    keyId = arguments[0].ToUpperInvariant();
    if(arguments.Length > 1) hint = string.Join(" ", arguments, 1, arguments.Length-1);
  }

  /// <summary>Gets the user ID hint.</summary>
  public string Hint
  {
    get { return hint; }
  }

  /// <summary>Gets the key ID to which the hint is related.</summary>
  public string PrimaryKeyId
  {
    get { return keyId; }
  }

  readonly string hint, keyId;
}
#endregion
#endregion

} // namespace AdamMil.Security.PGP.GPG.StatusMessages
