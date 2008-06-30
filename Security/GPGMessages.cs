using System;
using System.Globalization;

namespace AdamMil.Security.PGP.GPG.StatusMessages
{

#region StatusMessageType
/// <summary>Indicates the type of GPG status message that was received.</summary>
public enum StatusMessageType
{
  /// <summary>The <c>NEWSIG</c> message, implemented by the <see cref="StatusMessages.GenericMessage"/> class.</summary>
  NewSig, 
  /// <summary>The <c>GOODSIG</c> message, implemented by the <see cref="StatusMessages.GoodSigMessage"/> class.</summary>
  GoodSig, 
  /// <summary>The <c>EXPSIG</c> message, implemented by the <see cref="StatusMessages.ExpiredSigMessage"/> class.</summary>
  ExpiredSig, 
  /// <summary>The <c>EXPKEYSIG</c> message, implemented by the <see cref="StatusMessages.ExpiredKeySigMessage"/> class.</summary>
  ExpiredKeySig, 
  /// <summary>The <c>REVKEYSIG</c> message, implemented by the <see cref="StatusMessages.RevokedKeySigMessage"/> class.</summary>
  RevokedKeySig, 
  /// <summary>The <c>BADSIG</c> message, implemented by the <see cref="StatusMessages.BadSigMessage"/> class.</summary>
  BadSig, 
  /// <summary>The <c>ERRSIG</c> message, implemented by the <see cref="StatusMessages.ErrorSigMessage"/> class.</summary>
  ErrorSig, 
  /// <summary>The <c>VALIDSIG</c> message, implemented by the <see cref="StatusMessages.ValidSigMessage"/> class.</summary>
  ValidSig, 
  /// <summary>The <c>ENC_TO</c> message, implemented by the <see cref="StatusMessages.GenericKeyIdMessage"/> class.</summary>
  EncTo,
#pragma warning disable 1574 // TODO: remove this!
  /// <summary>The <c>NODATA</c> message, implemented by the <see cref="StatusMessages.NoDataMessage"/> class.</summary>
  NoData,
#pragma warning restore 1574 // TODO: remove this!
  /// <summary>The <c>UNEXPECTED</c> message, implemented by the <see cref="StatusMessages.GenericMessage"/> class.</summary>
  UnexpectedData,
  /// <summary>The <c>TRUST_UNDEFINED</c> message, implemented by the <see cref="StatusMessages.TrustLevelMessage"/> class.</summary>
  TrustUndefined,
  /// <summary>The <c>TRUST_NEVER</c> message, implemented by the <see cref="StatusMessages.TrustLevelMessage"/> class.</summary>
  TrustNever,
  /// <summary>The <c>TRUST_MARGINAL</c> message, implemented by the <see cref="StatusMessages.TrustLevelMessage"/> class.</summary>
  TrustMarginal,
  /// <summary>The <c>TRUST_FULLY</c> message, implemented by the <see cref="StatusMessages.TrustLevelMessage"/> class.</summary>
  TrustFully,
  /// <summary>The <c>TRUST_ULTIMATE</c> message, implemented by the <see cref="StatusMessages.TrustLevelMessage"/> class.</summary>
  TrustUltimate,
#pragma warning disable 1574 // TODO: remove this!
  /// <summary>The <c>PKA_TRUST_GOOD</c> message, implemented by the <see cref="StatusMessages.PKATrustGoodMessage"/> class.</summary>
  PKATrustGood, 
  /// <summary>The <c>PKA_TRUST_BAD</c> message, implemented by the <see cref="StatusMessages.PKATrustBadMessage"/> class.</summary>
  PKATrustBad, 
  /// <summary>The <c>KEYEXPIRED</c> message, implemented by the <see cref="StatusMessages.KeyExpiredMessage"/> class.</summary>
  KeyExpired, 
  /// <summary>The <c>KEYREVOKED</c> message, implemented by the <see cref="StatusMessages.KeyRevokedMessage"/> class.</summary>
  KeyRevoked, 
  /// <summary>The <c>BADARMOR</c> message, implemented by the <see cref="StatusMessages.BadArmorMessage"/> class.</summary>
  BadArmor, 
  /// <summary>The <c>NEED_PASSPHRASE</c> message, implemented by the <see cref="StatusMessages.NeedKeyPassphraseMessage"/> class.</summary>
  NeedKeyPassphrase, 
  /// <summary>The <c>NEED_PASSPHRASE_SYM</c> message, implemented by the <see cref="StatusMessages.NeedCipherPassphraseMessage"/> class.</summary>
  NeedCipherPassphrase,
  /// <summary>The <c>NEED_PASSPHRASE_PIN</c> message, implemented by the <see cref="StatusMessages.NeedPinMessage"/> class.</summary>
  NeedPin, 
  /// <summary>The <c>MISSING_PASSPHRASE</c> message, implemented by the <see cref="StatusMessages.MissingPassphraseMessage"/> class.</summary>
  MissingPassphrase, 
  /// <summary>The <c>BAD_PASSPHRASE</c> message, implemented by the <see cref="StatusMessages.BadPassphraseMessage"/> class.</summary>
  BadPassphrase, 
  /// <summary>The <c>GOOD_PASSPHRASE</c> message, implemented by the <see cref="StatusMessages.GoodPassphraseMessage"/> class.</summary>
  GoodPassphrase, 
  /// <summary>The <c>DECRYPTION_FAILED</c> message, implemented by the <see cref="StatusMessages.DecryptionFailedMessage"/> class.</summary>
  DecryptionFailed, 
  /// <summary>The <c>DECRYPTION_OKAY</c> message, implemented by the <see cref="StatusMessages.DecryptionOkayMessage"/> class.</summary>
  DecryptionOkay, 
  /// <summary>The <c>NO_PUBKEY</c> message, implemented by the <see cref="StatusMessages.MissingPublicKeyMessage"/> class.</summary>
  NoPublicKey,
  /// <summary>The <c>NO_SECKEY</c> message, implemented by the <see cref="StatusMessages.MissingSecretKeyMessage"/> class.</summary>
  NoSecretKey, 
  /// <summary>The <c>IMPORTED</c> message, implemented by the <see cref="StatusMessages.KeySigImportedMessage"/> class.</summary>
  Imported, 
  /// <summary>The <c>IMPORT_OK</c> message, implemented by the <see cref="StatusMessages.KeyImportOkayMessage"/> class.</summary>
  ImportOkay, 
  /// <summary>The <c>IMPORT_PROBLEM</c> message, implemented by the <see cref="StatusMessages.KeyImportProblemMessage"/> class.</summary>
  ImportProblem, 
  /// <summary>The <c>IMPORT_RES</c> message, implemented by the <see cref="StatusMessages.KeyImportResultsMessage"/> class.</summary>
  ImportResult, 
  /// <summary>The <c>FILE_START</c> message, implemented by the <see cref="StatusMessages.FileStartMessage"/> class.</summary>
  FileStart, 
  /// <summary>The <c>FILE_DONE</c> message, implemented by the <see cref="StatusMessages.FileDoneMessage"/> class.</summary>
  FileDone, 
  /// <summary>The <c>BEGIN_DECRYPTION</c> message, implemented by the <see cref="StatusMessages.BeginDecryptionMessage"/> class.</summary>
  BeginDecryption, 
  /// <summary>The <c>END_DECRYPTION</c> message, implemented by the <see cref="StatusMessages.EndDecryptionMessage"/> class.</summary>
  EndDecryption, 
  /// <summary>The <c>BEGIN_ENCRYPTION</c> message, implemented by the <see cref="StatusMessages.BeginEncryptionMessage"/> class.</summary>
  BeginEncryption, 
  /// <summary>The <c>END_ENCRYPTION</c> message, implemented by the <see cref="StatusMessages.EndEncryptionMessage"/> class.</summary>
  EndEncryption, 
  /// <summary>The <c>BEGIN_SIGNING</c> message, implemented by the <see cref="StatusMessages.BeginSigningMessage"/> class.</summary>
  BeginSigning, 
  /// <summary>The <c>DELETE_PROBLEM</c> message, implemented by the <see cref="StatusMessages.DeleteFailedMessage"/> class.</summary>
  DeleteFailed, 
  /// <summary>The <c>PROGRESS</c> message, implemented by the <see cref="StatusMessages.ProgressMessage"/> class.</summary>
  Progress, 
  /// <summary>The <c>SIG_CREATED</c> message, implemented by the <see cref="StatusMessages.SigCreatedMessage"/> class.</summary>
  SigCreated, 
  /// <summary>The <c>KEY_CREATED</c> message, implemented by the <see cref="StatusMessages.KeyCreatedMessage"/> class.</summary>
  KeyCreated, 
  /// <summary>The <c>KEY_NOT_CREATED</c> message, implemented by the <see cref="StatusMessages.KeyNotCreatedMessage"/> class.</summary>
  KeyNotCreated, 
  /// <summary>The <c>SESSION_KEY</c> message, implemented by the <see cref="StatusMessages.SessionKeyMessage"/> class.</summary>
  SessionKey, 
  /// <summary>The <c>USERID_HINT</c> message, implemented by the <see cref="StatusMessages.UserIdHintMessage"/> class.</summary>
  UserIdHint, 
  /// <summary>The <c>INV_RECP</c> message, implemented by the <see cref="StatusMessages.InvalidRecipientMessage"/> class.</summary>
  InvalidRecipient, 
  /// <summary>The <c>NO_RECP</c> message, implemented by the <see cref="StatusMessages.NoRecipientsMessage"/> class.</summary>
  NoRecipients, 
  /// <summary>The <c>ERROR</c> message, implemented by the <see cref="StatusMessages.ErrorMessage"/> class.</summary>
  Error, 
  /// <summary>The <c>CARDCTRL</c> message, implemented by the <see cref="StatusMessages.CardControlMessage"/> class.</summary>
  CardControl, 
  /// <summary>The <c>BACKUP_KEY_CREATED</c> message, implemented by the <see cref="StatusMessages.BackupKeyCreatedMessage"/> class.</summary>
  BackupKeyCreated,
#pragma warning restore 1574 // TODO: remove this!
  /// <summary>The <c>GOODMDC</c> message, implemented by the <see cref="StatusMessages.GenericMessage"/> class.</summary>
  GoodMDC,
}
#endregion

#region StatusMessage
/// <summary>The base class for GPG status messages.</summary>
public abstract class StatusMessage
{
  /// <summary>Initializes a new <see cref="StatusMessage"/> with the given <see cref="StatusMessageType"/>.</summary>
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
public enum KeyImportReason
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
public enum ImportFailureReason
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
public sealed class KeySigImportedMessage : StatusMessage
{
  /// <summary>Initializes a new <see cref="KeySigImportedMessage"/> with the given arguments.</summary>
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
public sealed class KeyImportOkayMessage : StatusMessage
{
  /// <summary>Initializes a new <see cref="KeyImportOkayMessage"/> with the given arguments.</summary>
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
public sealed class KeyImportFailedMessage : StatusMessage
{
  /// <summary>Initializes a new <see cref="KeyImportFailedMessage"/> with the given arguments.</summary>
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
#endregion

#region Signature verification messages
#region KeyIdAndNameSigVerifyMessage
/// <summary>A base class for signature verification messages with a key ID and user name as the only arguments. There
/// are several such messages.
/// </summary>
public abstract class KeyIdAndNameSigVerifyMessage : StatusMessage
{
  /// <summary>Initializes a new <see cref="KeyIdAndNameSigVerifyMessage"/> with the given
  /// <see cref="StatusMessageType"/> and arguments.
  /// </summary>
  protected KeyIdAndNameSigVerifyMessage(StatusMessageType type, string[] arguments) : base(type)
  {
    keyId    = arguments[0].ToUpperInvariant();
    userName = arguments[1];
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
public sealed class GoodSigMessage : KeyIdAndNameSigVerifyMessage
{
  /// <summary>Initializes a new <see cref="GoodSigMessage"/> with the given arguments.</summary>
  public GoodSigMessage(string[] arguments) : base(StatusMessageType.GoodSig, arguments) { }
}
#endregion

#region ExpiredSigMessage
/// <summary>Issued along with <see cref="GoodSigMessage"/> to indicate that although the signature is good, it has
/// expired.
/// </summary>
public sealed class ExpiredSigMessage : KeyIdAndNameSigVerifyMessage
{
  /// <summary>Initializes a new <see cref="ExpiredSigMessage"/> with the given arguments.</summary>
  public ExpiredSigMessage(string[] arguments) : base(StatusMessageType.ExpiredSig, arguments) { }
}
#endregion

#region ExpiredKeySigMessage
/// <summary>Issued along with <see cref="GoodSigMessage"/> to indicate that although the signature is good, it was
/// made with a key that has expired.
/// </summary>
public sealed class ExpiredKeySigMessage : KeyIdAndNameSigVerifyMessage
{
  /// <summary>Initializes a new <see cref="ExpiredKeySigMessage"/> with the given arguments.</summary>
  public ExpiredKeySigMessage(string[] arguments) : base(StatusMessageType.ExpiredKeySig, arguments) { }
}
#endregion

#region RevokedKeySigMessage
/// <summary>Issued along with <see cref="GoodSigMessage"/> to indicate that although the signature is good, it was
/// made with a key that has been revoked.
/// </summary>
public sealed class RevokedKeySigMessage : KeyIdAndNameSigVerifyMessage
{
  /// <summary>Initializes a new <see cref="RevokedKeySigMessage"/> with the given arguments.</summary>
  public RevokedKeySigMessage(string[] arguments) : base(StatusMessageType.RevokedKeySig, arguments) { }
}
#endregion

#region BadSigMessage
/// <summary>Issued when a bad signature is detected. For each signature, only one of <see cref="GoodSigMessage"/>,
/// <see cref="BadSigMessage"/>, or <see cref="ErrorSigMessage"/> will be issued.
/// </summary>
public sealed class BadSigMessage : KeyIdAndNameSigVerifyMessage
{
  /// <summary>Initializes a new <see cref="BadSigMessage"/> with the given arguments.</summary>
  public BadSigMessage(string[] arguments) : base(StatusMessageType.BadSig, arguments) { }
}
#endregion

#region ErrorSigMessage
/// <summary>Issued when an error prevented the signature from being verified. For each signature, only one of
/// <see cref="GoodSigMessage"/>, <see cref="BadSigMessage"/>, or <see cref="ErrorSigMessage"/> will be issued.
/// </summary>
public sealed class ErrorSigMessage : StatusMessage
{
  /// <summary>Initializes a new <see cref="ErrorSigMessage"/> with the given arguments.</summary>
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
public sealed class ValidSigMessage : StatusMessage
{
  /// <summary>Initializes a new <see cref="ValidSigMessage"/> with the given arguments.</summary>
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
public sealed class GenericMessage : StatusMessage
{
  /// <summary>Initializes a new <see cref="GenericMessage"/> with the given message type.</summary>
  public GenericMessage(StatusMessageType type) : base(type) { }
}
#endregion

#region GenericKeyIdMessage
/// <summary>A base class for messages that contain only a key ID parameter.</summary>
public sealed class GenericKeyIdMessage : StatusMessage
{
  /// <summary>Initializes a new <see cref="GenericKeyIdMessage"/> with the given type and arguments.</summary>
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

#region BadPassphraseMessage
/// <summary>Indicates that the previously-requested password was wrong.</summary>
public sealed class BadPassphraseMessage : StatusMessage
{
  /// <summary>Initializes a new <see cref="BadPassphraseMessage"/> object with the given arguments.</summary>
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

#region InvalidRecipientReason
/// <summary>Indicates the reason why an encryption recipient was rejected.</summary>
public enum InvalidRecipientReason
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
public sealed class InvalidRecipientMessage : StatusMessage
{
  /// <summary>Initializes a new <see cref="InvalidRecipientMessage"/> with the given arguments.</summary>
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

#region NeedPassphraseMessage
/// <summary>Issued when a password is needed to unlock a secret key.</summary>
public sealed class NeedKeyPassphraseMessage : StatusMessage
{
  /// <summary>Initializes a new <see cref="NeedKeyPassphraseMessage"/> with the given arguments.</summary>
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
public sealed class TrustLevelMessage : StatusMessage
{
  /// <summary>Initializes a new <see cref="TrustLevelMessage"/> with the given <see cref="StatusMessageType"/>
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
public sealed class UserIdHintMessage : StatusMessage
{
  /// <summary>Initializes a new <see cref="UserIdHintMessage"/> object with the given arguments.</summary>
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
