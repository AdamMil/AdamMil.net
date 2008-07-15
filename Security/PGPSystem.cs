/*
AdamMil.Security is a .NET library providing OpenPGP-based security.
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

// TODO: get an OpenPGP-compatible smart card, and then add smart card functions!

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

#region KeyType
/// <summary>A static class containing common key types. Note that not all of these types may be supported by a given
/// implementation, so it's recommended that <see cref="Default"/> be used whenever possible. If a specific algorithm
/// is desired, use <see cref="PGPSystem.GetSupportedKeyTypes"/> to verify that it is supported.
/// </summary>
public static class KeyType
{
  /// <summary>The default key type will be used.</summary>
  public const string Default = null;
  /// <summary>No key will be created.</summary>
  public static readonly string None = "None";
  /// <summary>The FIPS-186 Digital Signature Algorithm will be used, which is for signing only. Standard DSA keys can
  /// be up to 1024 bits in length. Larger keys are supported by only a few clients.
  /// </summary>
  public static readonly string DSA = "DSA";
  /// <summary>An ElGamal key will be used.</summary>
  public static readonly string ElGamal = "ELG";
  /// <summary>The RSA algorithm will be used.</summary>
  public static readonly string RSA = "RSA";
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
  /// <summary>An RSA key type, equivalent to <see cref="KeyType.RSA"/>.</summary>
  RSA=1,
  /// <summary>An RSA encryption-only key type. This is deprecated.</summary>
  RSAEncryptOnly=2,
  /// <summary>An RSA signing-only key type. This is deprecated.</summary>
  RSASignOnly=3,
  /// <summary>An ElGamal encryption-only key type, corresponding to <see cref="KeyType.ElGamal"/>.</summary>
  ElGamalEncryptOnly=16,
  /// <summary>A DSA signing key, corresponding to <see cref="KeyType.DSA"/>.</summary>
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
  /// <summary>The signature was not verified.</summary>
  Unverified=0,
  /// <summary>An error prevented the signature from being verified.</summary>
  Error=0x1,
  /// <summary>The signature is valid.</summary>
  Valid=0x2,
  /// <summary>The signature is invalid.</summary>
  Invalid=0x3,
  /// <summary>A mask that can be ANDed with a <see cref="SignatureStatus"/> to retrieve the overall success value:
  /// <c>Unverified, </c><c>Valid</c>, <c>Invalid</c>, or <c>Error</c>.
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

#region SignatureBase
/// <summary>A base class for types that represent signatures.</summary>
public class SignatureBase : ReadOnlyClass
{
  /// <summary>Gets or sets the time when the signature was created.</summary>
  public DateTime CreationTime
  {
    get { return creationTime; }
    set
    {
      AssertNotReadOnly();
      creationTime = value;
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

  /// <summary>Gets whether the signature is expired. This has no bearing on the signature's validity. An expired
  /// signature can still be validate successfully -- it's simply outdated.
  /// </summary>
  public bool Expired
  {
    get { return (Status & SignatureStatus.ExpiredSignature) != 0; }
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

  /// <summary>Gets a human-readable description of the identity of the signer.</summary>
  public string SignerName
  {
    get { return signerName; }
    set
    {
      AssertNotReadOnly();
      signerName = value;
    }
  }

  /// <summary>Gets the shortened version of the <see cref="KeyId"/>.</summary>
  public string ShortKeyId
  {
    get { return keyId == null || keyId.Length <= 8 ? keyId : keyId.Substring(keyId.Length - 8); }
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

  string keyId, keyFingerprint, signerName;
  DateTime creationTime;
  SignatureStatus status;
}
#endregion

#region Signature
/// <summary>Contains information about a digital signature.</summary>
/// <remarks>This class derives from <see cref="ReadOnlyClass"/>, and it is expected that a <see cref="PGPSystem"/>
/// implementation will call <see cref="ReadOnlyClass.MakeReadOnly"/> before returning a <see cref="Signature"/>
/// object.
/// </remarks>
public class Signature : SignatureBase
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

  /// <include file="documentation.xml" path="/Security/Common/ToString/*"/>
  public override string ToString()
  {
    string from = null;
    if(!string.IsNullOrEmpty(SignerName) || !string.IsNullOrEmpty(KeyId))
    {
      if(!string.IsNullOrEmpty(SignerName)) from = SignerName;
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
    else if(ErrorOccurred)
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
    else
    {
      str = "Unverified signature"+from;
    }

    return str;
  }

  string hashAlgorithm, keyType, primaryKeyFingerprint;
  DateTime? expiration;
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
public delegate SecureString KeyPasswordHandler(string keyId, string passwordHint);
#endregion

#region PasswordInvalidHandler
/// <include file="documentation.xml" path="/Security/PGPSystem/OnPasswordInvalid/*"/>
public delegate void PasswordInvalidHandler(string keyId);
#endregion
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

  /// <summary>An event that is raised when a decryption password needs to be obtained from the user.</summary>
  public event CipherPasswordHandler DecryptionPasswordNeeded;

  /// <summary>An event that is raised when a secret key password needs to be obtained from the user.</summary>
  public event KeyPasswordHandler KeyPasswordNeeded;

  #region Configuration
  /// <include file="documentation.xml" path="/Security/PGPSystem/GetDefaultPrimaryKeyType/*"/>
  public abstract string GetDefaultPrimaryKeyType();

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetDefaultSubkeyType/*"/>
  public abstract string GetDefaultSubkeyType();

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetMaximumKeyLength/*"/>
  public abstract int GetMaximumKeyLength(string keyType);

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

  #region Key import and export
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

  #region Key revocation
  /// <include file="documentation.xml" path="/Security/PGPSystem/AddDesignatedRevoker/*" />
  public abstract void AddDesignatedRevoker(PrimaryKey key, PrimaryKey revokerKey);

  /// <include file="documentation.xml" path="/Security/PGPSystem/GenerateRevocationCertificate/*" />
  public abstract void GenerateRevocationCertificate(PrimaryKey key, Stream destination, KeyRevocationReason reason,
                                                     OutputOptions outputOptions);

  /// <include file="documentation.xml" path="/Security/PGPSystem/GenerateRevocationCertificateD/*" />
  public abstract void GenerateRevocationCertificate(PrimaryKey keyToRevoke, PrimaryKey designatedRevoker,
                                                     Stream destination, KeyRevocationReason reason,
                                                     OutputOptions outputOptions);

  /// <include file="documentation.xml" path="/Security/PGPSystem/RevokeKeys/*" />
  public abstract void RevokeKeys(KeyRevocationReason reason, params PrimaryKey[] keys);

  /// <include file="documentation.xml" path="/Security/PGPSystem/RevokeKeysD/*" />
  public abstract void RevokeKeys(PrimaryKey designatedRevoker, KeyRevocationReason reason, params PrimaryKey[] keys);

  /// <include file="documentation.xml" path="/Security/PGPSystem/RevokeSubkeys/*" />
  public abstract void RevokeSubkeys(KeyRevocationReason reason, params Subkey[] subkeys);
  #endregion

  #region Key server operations
  /// <include file="documentation.xml" path="/Security/PGPSystem/FindPublicKeysOnServer/*"/>
  public abstract void FindPublicKeysOnServer(Uri keyServer, KeySearchHandler handler, params string[] searchKeywords);

  /// <summary>Downloads the public keys specified with the given fingerprints (or key IDs) from the given key server,
  /// and imports them into the default keyring.
  /// </summary>
  public ImportedKey[] ImportKeysFromServer(KeyDownloadOptions options, params string[] keyFingerprintsOrIds)
  {
    return ImportKeysFromServer(options, null, keyFingerprintsOrIds);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ImportKeysFromServer/*"/>
  public abstract ImportedKey[] ImportKeysFromServer(KeyDownloadOptions options, Keyring keyring,
                                                     params string[] keyFingerprintsOrIds);

  /// <summary>Refreshes all of the keys on the default keyring from a key server.</summary>
  public ImportedKey[] RefreshKeysFromServer(KeyDownloadOptions options)
  {
    return RefreshKeysFromServer(options, (Keyring)null);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/RefreshKeyringFromServer/*"/>
  public abstract ImportedKey[] RefreshKeysFromServer(KeyDownloadOptions options, Keyring keyring);

  /// <include file="documentation.xml" path="/Security/PGPSystem/RefreshKeysFromServer/*"/>
  public abstract ImportedKey[] RefreshKeysFromServer(KeyDownloadOptions options, params PrimaryKey[] keys);

  /// <include file="documentation.xml" path="/Security/PGPSystem/UploadKeys/*"/>
  public abstract void UploadKeys(KeyUploadOptions options, params PrimaryKey[] keys);
  #endregion

  #region Key signing
  /// <include file="documentation.xml" path="/Security/PGPSystem/DeleteSignatures/*" />
  public abstract void DeleteSignatures(params KeySignature[] signatures);

  /// <include file="documentation.xml" path="/Security/PGPSystem/RevokeSignatures/*" />
  public abstract void RevokeSignatures(UserRevocationReason reason, params KeySignature[] signatures);

  /// <include file="documentation.xml" path="/Security/PGPSystem/SignAttribute/*"/>
  public void SignAttribute(UserAttribute attribute, PrimaryKey signingKey, KeySigningOptions options)
  {
    if(attribute == null) throw new ArgumentNullException();
    SignAttributes(new UserAttribute[] { attribute }, signingKey, options);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/SignKey/*" />
  public void SignKey(PrimaryKey keyToSign, PrimaryKey signingKey, KeySigningOptions options)
  {
    if(keyToSign == null) throw new ArgumentNullException();
    SignKeys(new PrimaryKey[] { keyToSign }, signingKey, options);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/SignAttributes/*"/>
  public abstract void SignAttributes(UserAttribute[] attributes, PrimaryKey signingKey, KeySigningOptions options);

  /// <include file="documentation.xml" path="/Security/PGPSystem/SignKeys/*" />
  public abstract void SignKeys(PrimaryKey[] keysToSign, PrimaryKey signingKey, KeySigningOptions options);
  #endregion

  #region Keyring queries
  /// <include file="documentation.xml" path="/Security/PGPSystem/FindPublicKey/*[@name != 'options']"/>
  public PrimaryKey FindPublicKey(string keywordOrId, Keyring keyring)
  {
    return FindPublicKey(keywordOrId, keyring, ListOptions.Default);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/FindPublicKey/*"/>
  public abstract PrimaryKey FindPublicKey(string keywordOrId, Keyring keyring, ListOptions options);

  /// <include file="documentation.xml" path="/Security/PGPSystem/FindPublicKeys/*[@name != 'keyrings' and @name != 'includeDefaultKeyring']"/>
  /// <param name="keyring">The keyring to search, or null to search the default keyring.</param>
  public PrimaryKey[] FindPublicKeys(string[] fingerprintsOrIds, Keyring keyring, ListOptions options)
  {
    return FindPublicKeys(fingerprintsOrIds,
                          keyring == null ? null : new Keyring[] { keyring }, keyring == null, options);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/FindPublicKeys/*"/>
  public abstract PrimaryKey[] FindPublicKeys(string[] fingerprintsOrIds, Keyring[] keyrings,
                                              bool includeDefaultKeyring, ListOptions options);

  /// <include file="documentation.xml" path="/Security/PGPSystem/FindSecretKey/*[@name != 'options']"/>
  public PrimaryKey FindSecretKey(string keywordOrId, Keyring keyring)
  {
    return FindSecretKey(keywordOrId, keyring, ListOptions.Default);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/FindSecretKey/*"/>
  public abstract PrimaryKey FindSecretKey(string keywordOrId, Keyring keyring, ListOptions options);

  /// <include file="documentation.xml" path="/Security/PGPSystem/FindSecretKeys/*[@name != 'keyrings' and @name != 'includeDefaultKeyring']"/>
  /// <param name="keyring">The keyring to search, or null to search the default keyring.</param>
  public PrimaryKey[] FindSecretKeys(string[] fingerprintsOrIds, Keyring keyring, ListOptions options)
  {
    return FindSecretKeys(fingerprintsOrIds,
                          keyring == null ? null : new Keyring[] { keyring }, keyring == null, options);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/FindSecretKeys/*"/>
  public abstract PrimaryKey[] FindSecretKeys(string[] fingerprintsOrIds, Keyring[] keyrings,
                                              bool includeDefaultKeyring, ListOptions options);

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

  /// <summary>Refreshes the given keys by reloading them from its keyring database. An array is returned containing
  /// the updated keys, with null values for keys that no longer exist on their keyrings. The keys will be retrieved
  /// with the default <see cref="ListOptions"/>.
  /// </summary>
  public PrimaryKey[] RefreshKeys(PrimaryKey[] keys)
  {
    return RefreshKeys(keys, ListOptions.Default);
  }

  /// <summary>Refreshes the given keys by reloading them from its keyring database. An array is returned containing
  /// the updated keys, with null values for keys that no longer exist on their keyrings.
  /// </summary>
  public PrimaryKey[] RefreshKeys(PrimaryKey[] keys, ListOptions options)
  {
    if(keys == null) throw new ArgumentNullException();

    foreach(PrimaryKey key in keys)
    {
      if(key == null) throw new ArgumentException("A key was null.");
      if(string.IsNullOrEmpty(key.Fingerprint)) throw new ArgumentException("A key had no fingerprint.");
    }

    PrimaryKey[] refreshedKeys = new PrimaryKey[keys.Length];
    List<string> fingerprints = new List<string>(keys.Length); // holds the fingerprints of the keys to find
    List<int> groupIndices = new List<int>(keys.Length); // holds the indices of the keys in the original array

    // first we need to group the keys by keyring
    keys = (PrimaryKey[])keys.Clone(); // don't modify the parameter passed to us
    int[] indices = new int[keys.Length]; // we'll keep a map of indices that allows us to reverse the sorting
    for(int i=0; i<indices.Length; i++) indices[i] = i;
    Array.Sort<Key,int>(keys, indices, new CompareKeysByKeyring());

    // now we'll find the start and end of each group of keys with the same keyring
    int start=0;
    while(start < keys.Length)
    {
      Keyring keyring = keys[start].Keyring;
      int end;
      for(end=start+1; end<keys.Length && Keyring.Equals(keyring, keys[end].Keyring); end++) { }
      if(start == end) break;

      // now we have a group of keys from 'start' to 'end'. for each group, we may have to issue up to two calls:
      // one for public keys and one for secret keys. first we'll do the public keys
      for(int i=start; i<end; i++)
      {
        if(!keys[i].Secret)
        {
          fingerprints.Add(keys[i].Fingerprint);
          groupIndices.Add(indices[i]);
        }
      }

      if(fingerprints.Count != 0)
      {
        PrimaryKey[] foundKeys = FindPublicKeys(fingerprints.ToArray(), keyring, options);
        for(int i=0; i<foundKeys.Length; i++) refreshedKeys[groupIndices[i]] = foundKeys[i];
        fingerprints.Clear();
        groupIndices.Clear();
      }

      // now we'll do the secret keys, as well as move 'start' to the beginning of the next group, if any
      for(; start<end; start++)
      {
        if(keys[start].Secret)
        {
          fingerprints.Add(keys[start].Fingerprint);
          groupIndices.Add(indices[start]);
        }
      }

      if(fingerprints.Count != 0)
      {
        PrimaryKey[] foundKeys = FindSecretKeys(fingerprints.ToArray(), keyring, options);
        for(int i=0; i<foundKeys.Length; i++) refreshedKeys[groupIndices[i]] = foundKeys[i];
        fingerprints.Clear();
        groupIndices.Clear();
      }
    }

    return refreshedKeys;
  }
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

  #region Primary key management
  /// <include file="documentation.xml" path="/Security/PGPSystem/AddSubkey/*" />
  public abstract void AddSubkey(PrimaryKey key, string keyType, KeyCapabilities capabilities, int keyLength,
                                 DateTime? expiration);

  /// <include file="documentation.xml" path="/Security/PGPSystem/ChangeExpiration/*" />
  public abstract void ChangeExpiration(Key key, DateTime? expiration);

  /// <include file="documentation.xml" path="/Security/PGPSystem/ChangePassword/*" />
  public abstract void ChangePassword(PrimaryKey key, SecureString password);

  /// <include file="documentation.xml" path="/Security/PGPSystem/CleanKeys/*" />
  public abstract void CleanKeys(params PrimaryKey[] keys);

  /// <include file="documentation.xml" path="/Security/PGPSystem/CreateKey/*"/>
  public abstract PrimaryKey CreateKey(NewKeyOptions options);

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

  /// <include file="documentation.xml" path="/Security/PGPSystem/DeleteSubkeys/*" />
  public abstract void DeleteSubkeys(params Subkey[] subkeys);

  /// <include file="documentation.xml" path="/Security/PGPSystem/DisableKeys/*" />
  public abstract void DisableKeys(params PrimaryKey[] keys);

  /// <include file="documentation.xml" path="/Security/PGPSystem/EnableKeys/*" />
  public abstract void EnableKeys(params PrimaryKey[] keys);

  /// <include file="documentation.xml" path="/Security/PGPSystem/MinimizeKeys/*" />
  public abstract void MinimizeKeys(params PrimaryKey[] keys);

  /// <include file="documentation.xml" path="/Security/PGPSystem/SetOwnerTrust/*" />
  public abstract void SetOwnerTrust(TrustLevel trust, params PrimaryKey[] keys);
  #endregion

  #region User ID management
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

  /// <include file="documentation.xml" path="/Security/PGPSystem/DeleteAttributes/*" />
  public abstract void DeleteAttributes(params UserAttribute[] attributes);

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetPreferences/*" />
  public abstract UserPreferences GetPreferences(UserAttribute user);

  /// <include file="documentation.xml" path="/Security/PGPSystem/SetPreferences/*" />
  public abstract void SetPreferences(UserAttribute user, UserPreferences preferences);

  /// <include file="documentation.xml" path="/Security/PGPSystem/RevokeAttributes/*" />
  public abstract void RevokeAttributes(UserRevocationReason reason, params UserAttribute[] attributes);
  #endregion

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetCardPin/*"/>
  protected virtual SecureString GetCardPin(string cardType, string chvNumber, string serialNumber)
  {
    if(CardPinNeeded != null) return CardPinNeeded(cardType, chvNumber, serialNumber);
    else throw new UnhandledPasswordException("A smart card PIN was required, but no PIN handler was set.");
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetDecryptionPassword/*"/>
  protected virtual SecureString GetDecryptionPassword()
  {
    if(DecryptionPasswordNeeded != null) return DecryptionPasswordNeeded();
    else throw new UnhandledPasswordException();
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetKeyPassword/*"/>
  protected virtual SecureString GetKeyPassword(string keyId, string passwordHint)
  {
    if(KeyPasswordNeeded != null) return KeyPasswordNeeded(keyId, passwordHint);
    else throw new UnhandledPasswordException();
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/OnPasswordInvalid/*"/>
  protected virtual void OnInvalidPassword(string keyId)
  {
    if(KeyPasswordInvalid != null) KeyPasswordInvalid(keyId);
  }

  /// <summary>Compares keys based on their keyring.</summary>
  sealed class CompareKeysByKeyring : IComparer<Key>
  {
    public int Compare(Key a, Key b)
    {
      Keyring ak = a.GetPrimaryKey().Keyring, bk = b.GetPrimaryKey().Keyring;

      if(ak == bk) return 0;
      else if(ak == null) return -1;
      else if(bk == null) return 1;
      else
      {
        int cmp = string.CompareOrdinal(ak.PublicFile, bk.PublicFile);
        if(cmp == 0)
        {
          cmp = string.CompareOrdinal(ak.SecretFile, bk.SecretFile);
          if(cmp == 0) cmp = string.CompareOrdinal(ak.TrustDbFile, bk.TrustDbFile);
        }
        return cmp;
      }
    }
  }
}
#endregion

} // namespace AdamMil.GPG