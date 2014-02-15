/*
AdamMil.Security is a .NET library providing OpenPGP-based security.
http://www.adammil.net/
Copyright (C) 2008-2013 Adam Milazzo

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
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using AdamMil.Collections;
using AdamMil.IO;
using AdamMil.Utilities;

namespace AdamMil.Security.PGP
{

#region ISignableObject
/// <summary>Represents an object that can be signed with a <see cref="KeySignature"/>.</summary>
public interface ISignableObject
{
  /// <summary>Gets the <see cref="PrimaryKey"/> that owns the signed object.</summary>
  PrimaryKey PrimaryKey { get; }
}
#endregion

#region KeySignature
/// <summary>Represents a signature on a key.</summary>
public class KeySignature : SignatureBase
{
  /// <summary>Gets or sets whether this signature is exportable.</summary>
  public bool Exportable
  {
    get { return exportable; }
    set
    {
      AssertNotReadOnly();
      exportable = value;
    }
  }

  /// <summary>Gets or sets the object that is signed by this signature.</summary>
  public ISignableObject Object
  {
    get { return signedObject; }
    set
    {
      AssertNotReadOnly();
      signedObject = value;
    }
  }

  /// <summary>Gets whether this is a revocation signature.</summary>
  public bool Revocation
  {
    get
    {
      return Type == OpenPGPSignatureType.PrimaryKeyRevocation || Type == OpenPGPSignatureType.SubkeyRevocation ||
             Type == OpenPGPSignatureType.CertificateRevocation;
    }
  }

  /// <summary>Gets whether this is a self-signature, which is a signature by the object's own primary key.</summary>
  public bool SelfSignature
  {
    get
    {
      PrimaryKey key = Object != null ? Object.PrimaryKey : null;
      return key != null &&
             (string.Equals(KeyFingerprint, key.Fingerprint, StringComparison.Ordinal) ||
              string.Equals(KeyId, key.KeyId, StringComparison.Ordinal));
    }
  }

  /// <summary>Gets the trust level of the signature if the signature is a certification signature.</summary>
  public TrustLevel TrustLevel
  {
    get { return trustLevel; }
  }

  /// <summary>Gets or sets the signature type.</summary>
  public OpenPGPSignatureType Type
  {
    get { return type; }
    set
    {
      AssertNotReadOnly();
      type = value;
    }
  }

  /// <include file="documentation.xml" path="/Security/ReadOnlyClass/Finish/node()"/>
  public override void MakeReadOnly()
  {
    if(signedObject == null) throw new InvalidOperationException("The Object property is not set.");

    switch(type)
    {
      case OpenPGPSignatureType.PersonaCertification: trustLevel = TrustLevel.Never; break;
      case OpenPGPSignatureType.CasualCertification: trustLevel = TrustLevel.Marginal; break;
      case OpenPGPSignatureType.PositiveCertification: trustLevel = TrustLevel.Full; break;
      default: trustLevel = TrustLevel.Unknown; break;
    }

    base.MakeReadOnly();
  }

  /// <include file="documentation.xml" path="/Security/Common/ToString/node()"/>
  public override string ToString()
  {
    string str;
    if(IsValid) str = "Valid ";
    else if(ErrorOccurred) str = "Error in ";
    else if(IsInvalid) str = "Invalid ";
    else str = "Unverified ";

    str += type.ToString() + " signature";

    if(!string.IsNullOrEmpty(SignerName) || !string.IsNullOrEmpty(KeyId))
    {
      str += " by "+SignerName;
      if(!string.IsNullOrEmpty(KeyId)) str += string.IsNullOrEmpty(SignerName) ? "0x"+KeyId : " [0x"+KeyId+"]";
    }

    return str;
  }

  ISignableObject signedObject;
  TrustLevel trustLevel;
  OpenPGPSignatureType type;
  bool exportable;
}
#endregion

#region User attributes
#region UserAttribute
/// <summary>Represents a user attribute, which associates data about a key owner with the key.</summary>
/// <remarks>After the PGP system creates a <see cref="UserAttribute"/> object and sets its properties, it should call
/// <see cref="MakeReadOnly"/> to lock the property values, creating a read-only object.
/// </remarks>
public abstract class UserAttribute : ReadOnlyClass, ISignableObject
{
  /// <summary>Gets or sets the calculated trust level of this user attribute, which represents how much this user
  /// attribute is trusted to be truly associated with the the person named on the key.
  /// </summary>
  public TrustLevel CalculatedTrust
  {
    get { return trustLevel; }
    set
    {
      AssertNotReadOnly();
      trustLevel = value;
    }
  }

  /// <summary>Gets or sets the date when this attribute was created.</summary>
  public DateTime CreationTime
  {
    get { return creationTime; }
    set
    {
      AssertNotReadOnly();
      creationTime = value;
    }
  }

  /// <summary>Gets or sets an identifier for this attribute that identifies it within its key, or null if it
  /// has no such identifier. Attribute IDs are almost certainly unique within a key, but identical attributes may have
  /// identical IDs.
  /// </summary>
  public string Id
  {
    get { return id; }
    set
    {
      AssertNotReadOnly();
      id = value;
    }
  }

  /// <summary>Gets or sets the <see cref="PrimaryKey"/> to which this attribute belongs.</summary>
  public PrimaryKey PrimaryKey
  {
    get { return key; }
    set
    {
      AssertNotReadOnly();
      key = value;
    }
  }

  /// <summary>Gets or sets whether this is the primary user ID of the key.</summary>
  public bool Primary
  {
    get { return primary; }
    set
    {
      AssertNotReadOnly();
      primary = value;
    }
  }

  /// <summary>Gets or sets whether this user ID has been revoked.</summary>
  public bool Revoked
  {
    get { return revoked; }
    set
    {
      AssertNotReadOnly();
      revoked = value;
    }
  }

  /// <summary>Gets or sets a read-only list of signatures on this user ID.</summary>
  public IReadOnlyList<KeySignature> Signatures
  {
    get { return sigs; }
    set
    {
      AssertNotReadOnly();
      sigs = value;
    }
  }

  /// <include file="documentation.xml" path="/Security/ReadOnlyClass/Finish/node()"/>
  public override void MakeReadOnly()
  {
    if(key == null) throw new InvalidOperationException("The Key property is not set.");
    if(sigs == null) throw new InvalidOperationException("The Signatures property is not set.");

    foreach(KeySignature sig in sigs)
    {
      if(sig == null) throw new InvalidOperationException("A key signature was null.");
    }

    base.MakeReadOnly();
  }

  /// <summary>Given an OpenPGP user attribute type, and its subpacket data, returns a <see cref="UserAttribute"/>
  /// object representing it.
  /// </summary>
  public static UserAttribute Create(OpenPGPAttributeType type, byte[] subpacketData)
  {
    // currently, only Image attributes are supported
    return type == OpenPGPAttributeType.Image ?
      (UserAttribute)new UserImage(subpacketData) : new UnknownUserAttribute((int)type, subpacketData);
  }

  PrimaryKey ISignableObject.PrimaryKey
  {
    get { return key; }
  }

  PrimaryKey key;
  IReadOnlyList<KeySignature> sigs;
  string id;
  DateTime creationTime;
  TrustLevel trustLevel;
  bool primary, revoked;
}
#endregion

#region UserAttributeWithData
/// <summary>Represents a user attribute with the raw subpacket data available.</summary>
public abstract class UserAttributeWithData : UserAttribute
{
  /// <summary>Initializes a new <see cref="UserAttributeWithData"/> object with the given attribute subpacket data.
  /// The array will be owned by the attribute.
  /// </summary>
  protected UserAttributeWithData(byte[] subpacketData)
  {
    if(subpacketData == null) throw new ArgumentNullException();
    this.subpacketData = subpacketData;
  }

  /// <summary>Returns an array containing the OpenPGP attribute subpacket data.</summary>
  public byte[] GetSubpacketData()
  {
    return (byte[])subpacketData.Clone();
  }

  /// <summary>Returns a stream that reads the OpenPGP attribute subpacket data.</summary>
  public Stream GetSubpacketStream()
  {
    return new MemoryStream(subpacketData, 0, subpacketData.Length, false, false);
  }

  /// <summary>Returns a reference to the internal subpacket data array.</summary>
  protected byte[] Data
  {
    get { return subpacketData; }
  }

  readonly byte[] subpacketData;
}
#endregion

#region UserId
/// <summary>Represents a user ID for a key. Some keys may be used by multiple people, or one person filling multiple
/// roles, or one person who changed his name or email address, and user IDs allow these multiple identity claims to be
/// associated with a key and individually trusted, revoked, etc. User IDs can be signed by people who testify to the
/// truthfulness and accuracy of the identity.
/// </summary>
public class UserId : UserAttribute
{
  /// <summary>Gets or sets the name of the user. The standard format is <c>NAME (COMMENT) &lt;EMAIL&gt;</c>, where
  /// the comment is optional. This format should be used with unless you have a compelling reason to do otherwise.
  /// </summary>
  public string Name
  {
    get { return name; }
    set
    {
      AssertNotReadOnly();
      name = value;
    }
  }

  /// <include file="documentation.xml" path="/Security/Common/ToString/node()"/>
  public override string ToString()
  {
    return string.IsNullOrEmpty(Name) ? "Unknown user" : Name;
  }

  string name;
}
#endregion

#region UserImage
/// <summary>Represents a user attribute containing an image of the user.</summary>
public class UserImage : UserAttributeWithData
{
  /// <summary>Initializes a new <see cref="UserImage"/> attribute with the given attribute subpacket data.</summary>
  public UserImage(byte[] data) : base(data) { }

  /// <summary>Returns a new <see cref="Bitmap"/> containing the image data.</summary>
  public Bitmap GetBitmap()
  {
    if(Data.Length < 3) throw new PGPException("Invalid image header.");

    try
    {
      int headerLength = IOH.ReadLE2(Data, 0), headerVersion = Data[2];
      if(headerVersion != 1)
      {
        throw new PGPException("Unsupported image header version " +
                               headerVersion.ToStringInvariant());
      }

      // the Bitmap(Stream) constructor wants to own the entire stream, so we can't pass it a stream that's seeked
      // to the end of the header. instead, we'll construct a memory stream for just the portion after the header
      return new Bitmap(new MemoryStream(Data, headerLength, Data.Length-headerLength, false, false));
    }
    catch(Exception ex)
    {
      if(ex is OutOfMemoryException || ex is PGPException) throw;
      else throw new PGPException("Invalid or unsupported user image attribute.", ex);
    }
  }
}
#endregion

#region UnknownUserAttribute
/// <summary>Represents a user attribute that is not understood by this library. It may be a custom attribute, or this
/// library may be out of date.
/// </summary>
public class UnknownUserAttribute : UserAttributeWithData
{
  /// <summary>Initializes a new <see cref="UnknownUserAttribute"/> with the given OpenPGP attribute type and an array
  /// containing the attribute subpacket data. The attribute will own the array.
  /// </summary>
  public UnknownUserAttribute(int type, byte[] data) : base(data)
  {
    this.type = type;
  }

  /// <summary>Gets the OpenPGP attribute type.</summary>
  public int Type
  {
    get { return type; }
  }

  readonly int type;
}
#endregion
#endregion

#region Key types
#region KeyCapabilities
/// <summary>Describes the capabilities of a key, but not necessarily the capabilities to which it can currently be put
/// to use by you. For instance, a key may be capable of encryption and signing, but if you don't have the private
/// portion, you cannot utilize that capability. Or, the key may have been disabled.
/// </summary>
[Flags]
public enum KeyCapabilities
{
  /// <summary>The key has no utility.</summary>
  None=0,
  /// <summary>When creating a key, this specifies that the default key flags should be used.</summary>
  Default=0,
  /// <summary>The key can be used to encrypt data.</summary>
  Encrypt=1,
  /// <summary>The key can be used to sign data.</summary>
  Sign=2,
  /// <summary>The key can be used to certify other keys.</summary>
  Certify=4,
  /// <summary>The key can be used to authenticate its owners.</summary>
  Authenticate=8,
}
#endregion

#region Key
/// <summary>A base class for OpenPGP keys.</summary>
public abstract class Key : ReadOnlyClass
{
  /// <summary>Gets or sets the calculated trust level of this key, which represents how strongly this key is believed
  /// to be owned by at least one of its user IDs.
  /// </summary>
  public TrustLevel CalculatedTrust
  {
    get { return calculatedTrust; }
    set
    {
      AssertNotReadOnly();
      calculatedTrust = value;
    }
  }

  /// <summary>Gets or sets the capabilities of this key. This value represents the original capabilities of the key,
  /// not necessarily what a particular person will be able to do with it.
  /// </summary>
  public KeyCapabilities Capabilities
  {
    get { return capabilities; }
    set
    {
      AssertNotReadOnly();
      capabilities = value;
    }
  }

  /// <summary>Gets or sets the time when the key was created.</summary>
  public DateTime CreationTime
  {
    get { return creationTime; }
    set
    {
      AssertNotReadOnly();
      creationTime = value;
    }
  }

  /// <summary>Gets the <see cref="Fingerprint"/> if it is valid, and the <see cref="KeyId"/> otherwise.</summary>
  public string EffectiveId
  {
    get
    {
      return !string.IsNullOrEmpty(Fingerprint) ? Fingerprint :
             !string.IsNullOrEmpty(keyId) ? KeyId : null; // use keyId rather than KeyId to get the raw value
    }
  }

  /// <summary>Gets or sets the time when the key will expire, or null if it has no expiration.</summary>
  public DateTime? ExpirationTime
  {
    get { return expirationTime; }
    set
    {
      AssertNotReadOnly();
      expirationTime = value;
    }
  }

  /// <summary>Gets or sets whether the key has expired.</summary>
  public bool Expired
  {
    get { return expired; }
    set
    {
      AssertNotReadOnly();
      expired = value;
    }
  }

  /// <summary>Gets or sets the fingerprint of the key. The fingerprint can be used as a unique key ID, but there is a
  /// miniscule chance that two different keys will have the same fingerprint.
  /// </summary>
  public string Fingerprint
  {
    get { return fingerprint; }
    set
    {
      AssertNotReadOnly();
      fingerprint = value;
    }
  }

  /// <summary>Gets or sets whether the key is invalid (for instance, due to a missing self-signature).</summary>
  public bool Invalid
  {
    get { return invalid; }
    set
    {
      AssertNotReadOnly();
      invalid = value;
    }
  }

  /// <summary>Gets or sets the ID of the key. Note that the key ID is not guaranteed to be unique. For a more unique
  /// ID, use the <see cref="Fingerprint"/>. If the key ID is not set, but the fingerprint is, the property will return
  /// a key ID value calculated from the fingerprint.
  /// </summary>
  public string KeyId
  {
    get
    {
      if(!string.IsNullOrEmpty(keyId))
      {
        return keyId;
      }
      else if(!string.IsNullOrEmpty(Fingerprint) && Fingerprint.Length >= 16)
      {
        return Fingerprint.Substring(Fingerprint.Length - 16);
      }
      else return null;
    }
    set
    {
      AssertNotReadOnly();
      keyId = value;
    }
  }

  /// <summary>Gets or sets the name of the key type, or null if the key type could not be determined.</summary>
  public string KeyType
  {
    get { return keyType; }
    set
    {
      AssertNotReadOnly();
      keyType = value;
    }
  }

  /// <include file="documentation.xml" path="/Security/Key/Keyring/node()"/>
  public abstract Keyring Keyring
  {
    get; set;
  }

  /// <summary>Gets or sets the length of the key, in bits.</summary>
  public int Length
  {
    get { return length; }
    set
    {
      AssertNotReadOnly();
      length = value;
    }
  }

  /// <summary>Gets or sets whether the key has been revoked.</summary>
  public bool Revoked
  {
    get { return revoked; }
    set
    {
      AssertNotReadOnly();
      revoked = value;
    }
  }

  /// <summary>Gets or sets a read-only list of signatures on this user ID.</summary>
  public IReadOnlyList<KeySignature> Signatures
  {
    get { return sigs; }
    set
    {
      AssertNotReadOnly();
      sigs = value;
    }
  }

  /// <summary>Gets or sets the short version of the <see cref="KeyId"/>. Note that the key ID is not guaranteed to be
  /// unique. For a more unique ID, use the <see cref="Fingerprint"/>. If the key ID is not set, but the fingerprint
  /// is, the property will return a key ID value calculated from the fingerprint.
  /// </summary>
  public string ShortKeyId
  {
    get
    {
      if(!string.IsNullOrEmpty(keyId) && keyId.Length >= 8)
      {
        return keyId.Substring(keyId.Length - 8);
      }
      else if(!string.IsNullOrEmpty(Fingerprint) && Fingerprint.Length >= 8)
      {
        return Fingerprint.Substring(Fingerprint.Length - 8);
      }
      else return null;
    }
    set
    {
      AssertNotReadOnly();
      keyId = value;
    }
  }

  /// <include file="documentation.xml" path="/Security/ReadOnlyClass/Finish/node()"/>
  public override void MakeReadOnly()
  {
    if(sigs == null) throw new InvalidOperationException("The Signatures property has not been set.");

    foreach(KeySignature sig in sigs)
    {
      if(sig == null) throw new InvalidOperationException("A key signature was null.");
    }

    base.MakeReadOnly();
  }

  /// <summary>Gets or sets the primary key associated with this key, or the current key if it is a primary key.</summary>
  public abstract PrimaryKey GetPrimaryKey();

  /// <include file="documentation.xml" path="/Security/Common/ToString/node()"/>
  public override string ToString()
  {
    return "0x" + KeyId;
  }

  string keyId, keyType, fingerprint;
  IReadOnlyList<KeySignature> sigs;
  DateTime? expirationTime;
  DateTime creationTime;
  int length;
  KeyCapabilities capabilities;
  TrustLevel calculatedTrust;
  bool invalid, revoked, expired;
}
#endregion

#region PrimaryKey
/// <summary>An OpenPGP primary key. In OpenPGP, the keys on a keyring are primary keys. Each primary key can have an
/// arbitrary number of <see cref="UserId">user IDs</see> and <see cref="Subkey">subkeys</see> associated with it. Both
/// primary keys and subkeys can be used to sign and encrypt, depending on their individual capabilities, but typically
/// the roles of the keys are divided so that the primary key is only used for signing while the subkeys are only used
/// for encryption.
/// </summary>
public class PrimaryKey : Key, ISignableObject
{
  /// <summary>Gets or sets a read-only list of user attributes (excluding <see cref="UserId"/> attributes) associated
  /// with this primary key.
  /// </summary>
  public IReadOnlyList<UserAttribute> Attributes
  {
    get { return userAttributes; }
    set
    {
      AssertNotReadOnly();
      userAttributes = value;
    }
  }

  /// <summary>Gets or sets a read-only list of fingerprints of keys that are allowed to revoke this primary key. In
  /// addition, a key is always allowed to be used to revoke itself.
  /// </summary>
  public IReadOnlyList<string> DesignatedRevokers
  {
    get { return revokers; }
    set
    {
      AssertNotReadOnly();
      revokers = value;
    }
  }

  /// <summary>Gets or sets whether the key has been disabled, indicating that it should not be used. Because the
  /// enabled status is not stored within the key, a key can be disabled by anyone, but it will only be disabled for
  /// that person. It can be reenabled at any time.
  /// </summary>
  public bool Disabled
  {
    get { return disabled; }
    set
    {
      AssertNotReadOnly();
      disabled = value;
    }
  }

  /// <summary>Gets or sets whether this primary key has a secret portion. This property will not be valid unless
  /// <see cref="ListOptions.RetrieveSecretKeys"/> was passed during the key retrieval.
  /// </summary>
  public bool HasSecretKey
  {
    get { return hasSecret; }
    set
    {
      AssertNotReadOnly();
      hasSecret = value;
    }
  }

  /// <include file="documentation.xml" path="/Security/Key/Keyring/node()"/>
  public override Keyring Keyring
  {
    get { return keyring; }
    set
    {
      AssertNotReadOnly();
      keyring = value;
    }
  }

  /// <summary>Gets or sets the extent to which the owner(s) of a key are trusted to validate the ownership
  /// of other people's keys.
  /// </summary>
  public TrustLevel OwnerTrust
  {
    get { return ownerTrust; }
    set
    {
      AssertNotReadOnly();
      ownerTrust = value;
    }
  }

  /// <summary>Gets the primary user ID for this key, or the first user ID if no user ID has been marked as primary.</summary>
  public UserId PrimaryUserId
  {
    get { return primaryUserId; }
  }

  /// <summary>Gets or sets a read-only list of subkeys of this primary key.</summary>
  public IReadOnlyList<Subkey> Subkeys
  {
    get { return subkeys; }
    set
    {
      AssertNotReadOnly();
      subkeys = value;
    }
  }

  /// <summary>Gets or sets the combined capabilities of this primary key and its subkeys.</summary>
  public KeyCapabilities TotalCapabilities
  {
    get { return totalCapabilities; }
    set
    {
      AssertNotReadOnly();
      totalCapabilities = value;
    }
  }

  /// <summary>Gets whether the key is not disabled, expired, or revoked, and whether the key has some capabilities.</summary>
  public bool Usable
  {
    get { return !Disabled && !Expired && !Revoked && TotalCapabilities != KeyCapabilities.None; }
  }

  /// <summary>Gets or sets a read-only list of user IDs associated with this primary key.</summary>
  public IReadOnlyList<UserId> UserIds
  {
    get { return userIds; }
    set
    {
      AssertNotReadOnly();
      userIds = value;
    }
  }

  /// <include file="documentation.xml" path="/Security/Key/Finish/node()"/>
  public override void MakeReadOnly()
  {
    if(string.IsNullOrEmpty(EffectiveId)) throw new InvalidOperationException("No key ID or fingerprint is set.");
    if(Attributes == null) throw new InvalidOperationException("The Attributes property is not set.");
    if(DesignatedRevokers == null) throw new InvalidOperationException("The DesignatedRevokers property is not set.");
    if(Subkeys == null) throw new InvalidOperationException("The Subkeys property is not set.");
    if(userIds == null || userIds.Count == 0)
    {
      throw new InvalidOperationException("The UserIds property is not set, or is empty.");
    }

    primaryUserId = null;
    foreach(UserId user in UserIds)
    {
      if(user == null) throw new InvalidOperationException("A user ID was null.");
      if(user.Primary)
      {
        if(primaryUserId != null) throw new InvalidOperationException("There are multiple primary user ids.");
        primaryUserId = user;
      }
    }

    if(primaryUserId == null) primaryUserId = UserIds[0]; // if no user ID is marked as primary, just use the first one

    foreach(UserAttribute attr in Attributes)
    {
      if(attr == null) throw new InvalidOperationException("A user attribute was null.");
    }

    foreach(string revoker in DesignatedRevokers)
    {
      if(string.IsNullOrEmpty(revoker)) throw new InvalidOperationException("A designated revoker was empty or null.");
    }

    foreach(Subkey subkey in Subkeys)
    {
      if(subkey == null) throw new InvalidOperationException("A subkey was null.");
    }

    base.MakeReadOnly();
  }

  /// <summary>Returns this key.</summary>
  public override PrimaryKey GetPrimaryKey()
  {
    return this;
  }

  /// <summary>Returns true if the <see cref="TotalCapabilities"/> property contains the given capability flags.</summary>
  public bool HasCapabilities(KeyCapabilities capability)
  {
    return (TotalCapabilities & capability) == capability;
  }

  /// <include file="documentation.xml" path="/Security/Common/ToString/node()"/>
  public override string ToString()
  {
    string str = base.ToString();
    if(PrimaryUserId != null) str += " " + PrimaryUserId.ToString();
    return str;
  }

  PrimaryKey ISignableObject.PrimaryKey
  {
    get { return this; }
  }

  IReadOnlyList<Subkey> subkeys;
  IReadOnlyList<UserId> userIds;
  IReadOnlyList<UserAttribute> userAttributes;
  IReadOnlyList<string> revokers;

  UserId primaryUserId;
  Keyring keyring;
  KeyCapabilities totalCapabilities;
  TrustLevel ownerTrust;
  bool disabled, hasSecret;
}
#endregion

#region Subkey
/// <summary>Represents a subkey of a primary key. See <see cref="PrimaryKey"/> for a more thorough description.</summary>
public class Subkey : Key, ISignableObject
{
  /// <include file="documentation.xml" path="/Security/Key/Keyring/node()"/>
  public override Keyring Keyring
  {
    get { return PrimaryKey != null ? PrimaryKey.Keyring : null; }
    set { throw new NotSupportedException("To change a subkey's keyring, set the keyring of its primary key."); }
  }

  /// <summary>Gets or sets the primary key that owns this subkey.</summary>
  public PrimaryKey PrimaryKey
  {
    get { return primaryKey; }
    set
    {
      AssertNotReadOnly();
      primaryKey = value;
    }
  }

  /// <include file="documentation.xml" path="/Security/Key/Finish/node()"/>
  public override void MakeReadOnly()
  {
    if(primaryKey == null) throw new InvalidOperationException("The PrimaryKey property has not been set.");
    base.MakeReadOnly();
  }

  /// <summary>Gets the primary key that owns this subkey.</summary>
  public override PrimaryKey GetPrimaryKey()
  {
    return primaryKey;
  }

  /// <summary>Returns true if the <see cref="Key.Capabilities"/> property contains the given capability flags.</summary>
  public bool HasCapabilities(KeyCapabilities capability)
  {
    return (Capabilities & capability) == capability;
  }

  /// <include file="documentation.xml" path="/Security/Common/ToString/node()"/>
  public override string ToString()
  {
    string str = base.ToString();
    if(PrimaryKey != null && PrimaryKey.PrimaryUserId != null) str += " " + PrimaryKey.PrimaryUserId.ToString();
    return str;
  }

  PrimaryKey primaryKey;
}
#endregion
#endregion

#region KeyCollection
/// <summary>A collection of <see cref="Key"/> objects. The key objects cannot be null.</summary>
public class KeyCollection<KeyType> : Collection<KeyType> where KeyType : Key
{
  /// <summary>Initializes a new <see cref="KeyCollection{T}"/> with no required key capabilities.</summary>
  public KeyCollection() { }

  /// <summary>Initializes a new <see cref="KeyCollection{T}"/> with the given set of required key capabilities.</summary>
  public KeyCollection(KeyCapabilities requiredCapabilities)
  {
    this.requiredCapabilities = requiredCapabilities;
  }

  /// <summary>Adds the given keys to the collection.</summary>
  public void AddRange(IEnumerable<KeyType> keys)
  {
    if(keys == null) throw new ArgumentNullException();

    foreach(KeyType key in keys)
    {
      if(key == null) throw new ArgumentException("A key was null.");
    }

    foreach(KeyType key in keys) Add(key);
  }

  /// <summary>Called when a new item is about to be inserted.</summary>
  protected override void InsertItem(int index, KeyType item)
  {
    ValidateKey(item);
    base.InsertItem(index, item);
  }

  /// <summary>Called when an item is about to be changed.</summary>
  protected override void SetItem(int index, KeyType item)
  {
    ValidateKey(item);
    base.SetItem(index, item);
  }

  /// <summary>Called to verify that the key matches is allowed in the collection.</summary>
  protected void ValidateKey(KeyType key)
  {
    if(key == null) throw new ArgumentNullException();

    PrimaryKey primaryKey = key as PrimaryKey;
    KeyCapabilities capabilities = primaryKey != null ? primaryKey.TotalCapabilities : key.Capabilities;

    if((capabilities & requiredCapabilities) != requiredCapabilities)
    {
      throw new ArgumentException("The key does not have all of the required capabilities: " +
                                  requiredCapabilities.ToString());
    }
  }

  KeyCapabilities requiredCapabilities;
}
#endregion

#region Keyring
/// <summary>Represents a keyring, which is composed of a public keyring file and a secret keyring file. The public
/// file stores public keys and their signatures and attributes, while the secret keyring file stores secret keys.
/// It is possible to have a keyring with only a public keyring file (indicating that the secret keys are missing), but
/// not to have a secret file without a public file. Keyrings are specific to an individual <see cref="PGPSystem"/>,
/// so it can't be expected that a keyring (or its keys) from one system can be manipulated by a different system.
/// </summary>
public class Keyring
{
  /// <summary>Initializes a new <see cref="Keyring"/> with the given public and secret filenames, and trust database.</summary>
  /// <param name="publicFile">The name of the public keyring file, which is required.</param>
  /// <param name="secretFile">The name of the secret keyring file, which is optional.</param>
  /// <param name="trustDbFile">The name of the trust database. If null, the default trust database will be used.</param>
  public Keyring(string publicFile, string secretFile, string trustDbFile)
  {
    if(string.IsNullOrEmpty(publicFile)) throw new ArgumentException("The public portion of a keyring is required.");
    this.publicFile  = publicFile;
    this.secretFile  = string.IsNullOrEmpty(secretFile)  ? null : secretFile;
    this.trustDbFile = string.IsNullOrEmpty(trustDbFile) ? null : trustDbFile;
  }

  /// <summary>Gets the name of the public keyring file.</summary>
  public string PublicFile
  {
    get { return publicFile; }
  }

  /// <summary>Gets the name of the secret keyring file.</summary>
  public string SecretFile
  {
    get { return secretFile; }
  }

  /// <summary>Gets the name of the trust database.</summary>
  public string TrustDbFile
  {
    get { return trustDbFile; }
  }

  /// <summary>Creates the keyring files using the given <see cref="PGPSystem"/>, if they do not already exist or if
  /// <paramref name="overwrite"/> is true.
  /// </summary>
  public void Create(PGPSystem system, bool overwrite)
  {
    if(system == null) throw new ArgumentNullException();
    if(overwrite || !File.Exists(PublicFile)) system.CreatePublicKeyring(PublicFile);
    if(SecretFile  != null && (overwrite || !File.Exists(SecretFile)))  system.CreateSecretKeyring(SecretFile);
    if(TrustDbFile != null && (overwrite || !File.Exists(TrustDbFile))) system.CreateTrustDatabase(TrustDbFile);
  }

  /// <summary>Determines whether this keyring equals the given object.</summary>
  public override bool Equals(object obj)
  {
    return Equals(obj as Keyring);
  }

  /// <summary>Determines whether this keyring equals the given keyring.</summary>
  public bool Equals(Keyring other)
  {
    return this == other ||
           other != null && string.Equals(PublicFile, other.PublicFile, StringComparison.Ordinal) &&
           string.Equals(SecretFile, other.SecretFile, StringComparison.Ordinal) &&
           string.Equals(TrustDbFile, other.TrustDbFile, StringComparison.Ordinal);
  }

  /// <summary>Returns a hash code for this keyring.</summary>
  public override int GetHashCode()
  {
    return PublicFile.GetHashCode();
  }

  /// <include file="documentation.xml" path="/Security/Common/ToString/node()"/>
  public override string ToString()
  {
    return "public:" + PublicFile + (SecretFile == null ? null : ", secret:" + SecretFile) +
           (TrustDbFile == null ? null : ", trustdb:" + TrustDbFile);
  }

  /// <summary>Determines whether the two keyrings given are equal.</summary>
  public static bool Equals(Keyring a, Keyring b)
  {
    return a == null ? b == null : a.Equals(b);
  }

  string publicFile, secretFile, trustDbFile;
}
#endregion

} // namespace AdamMil.Security.PGP