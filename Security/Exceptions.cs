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

namespace AdamMil.Security.PGP
{

#region FailureReason
/// <summary>Gives a set of possible reasons for a failure.</summary>
[Flags]
public enum FailureReason
{
  /// <summary>This value indicates that the reason for failure is completely unknown.</summary>
  None=0,
  /// <summary>The failure could have been caused by a missing secret key, or a secret key for which a password was
  /// not known.
  /// </summary>
  MissingSecretKey=0x1,
  /// <summary>The failure could have been caused by a missing public key.</summary>
  MissingPublicKey=0x2,
  /// <summary>The failure could have been caused by an incorrect password.</summary>
  BadPassword=0x4,
  /// <summary>The failure could have been caused by an unsupported algorithm.</summary>
  UnsupportedAlgorithm=0x8,
  /// <summary>The failure could have been caused by invalid data, for instance an attempt to decrypt plaintext.</summary>
  BadData=0x10,
  /// <summary>The failure could have been caused by the fact that one or more encryption recipients were not valid.</summary>
  InvalidRecipients=0x20,
  /// <summary>The failure could have been caused by the keyring being locked.</summary>
  KeyringLocked=0x40,
  /// <summary>The failure could have been caused by an untrusted recipient.</summary>
  UntrustedRecipient=0x80,
  /// <summary>The failure could have been caused by an attempt to import a secret key when that key already exists.</summary>
  SecretKeyAlreadyExists=0x100,
  /// <summary>The failure could have been caused by the given key not being found.</summary>
  KeyNotFound=0x200,
}
#endregion

#region PGPException
/// <summary>The base class of exceptions specific to PGP systems.</summary>
public class PGPException : ApplicationException
{
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons0/*"/>
  public PGPException() : base("A PGP-related error occurred.") { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons1/*"/>
  public PGPException(string message) : base(message) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons2/*"/>
  public PGPException(string message, Exception innerException) : base(message, innerException) { }

  /// <summary>Initializes a new operation failure exception with the given base message and failure reasons.</summary>
  protected PGPException(string baseMessage, FailureReason reasons)
    : base(baseMessage + GetFailureText(reasons))
  {
    this.reasons = reasons;
  }

  /// <summary>Initializes a new operation failure exception with the given base message, failure reasons, and
  /// additional text.
  /// </summary>
  protected PGPException(string baseMessage, FailureReason reasons, string extraText)
    : base(baseMessage + GetFailureText(reasons) + " " + extraText)
  {
    this.reasons = reasons;
  }

  /// <summary>Gets a list of potential causes for the failure.</summary>
  public FailureReason Reasons
  {
    get { return reasons; }
  }

  /// <summary>Converts a <see cref="FailureReason"/> value into a human-readable string containing the list of
  /// reasons, or null if no reasons were given.
  /// </summary>
  protected static string GetFailureText(FailureReason reasons)
  {
    string reasonString = null;
    if((reasons & FailureReason.MissingSecretKey) != 0) reasonString += " missing or inaccessible secret key.";
    if((reasons & FailureReason.MissingPublicKey) != 0) reasonString += " missing public key.";
    if((reasons & FailureReason.BadPassword) != 0) reasonString += " bad or missing passphrase.";
    if((reasons & FailureReason.UnsupportedAlgorithm) != 0) reasonString += " unsupported algorithm.";
    if((reasons & FailureReason.BadData) != 0) reasonString += " invalid source data.";
    if((reasons & FailureReason.InvalidRecipients) != 0) reasonString += " invalid recipient(s).";
    if((reasons & FailureReason.KeyringLocked) != 0) reasonString += " keyring locked by another process.";
    if((reasons & FailureReason.UntrustedRecipient) != 0) reasonString += " a recipient was not trusted.";
    if((reasons & FailureReason.SecretKeyAlreadyExists) != 0) reasonString += " the secret key already exists.";
    if((reasons & FailureReason.KeyNotFound) != 0) reasonString += " the key was not found.";
    return reasonString == null ? null : " Suspected reason(s):" + reasonString;
  }

  readonly FailureReason reasons;
}
#endregion

#region DecryptionFailedException
/// <summary>An exception thrown when decryption of a document has failed.</summary>
public class DecryptionFailedException : PGPException
{
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons0/*"/>
  public DecryptionFailedException() : base("Decryption failed.") { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons1/*"/>
  public DecryptionFailedException(string message) : base(message) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons2/*"/>
  public DecryptionFailedException(string message, Exception innerException) : base(message, innerException) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF/*"/>
  public DecryptionFailedException(FailureReason reasons) : base("Decryption failed.", reasons) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF2/*"/>
  public DecryptionFailedException(FailureReason reasons, string extraText)
    : base("Decryption failed.", reasons, extraText) { }
}
#endregion

#region EncryptionFailedException
/// <summary>An exception thrown when encryption of a document has failed.</summary>
public class EncryptionFailedException : PGPException
{
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons0/*"/>
  public EncryptionFailedException() : base("Encryption failed.") { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons1/*"/>
  public EncryptionFailedException(string message) : base(message) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons2/*"/>
  public EncryptionFailedException(string message, Exception innerException) : base(message, innerException) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF/*"/>
  public EncryptionFailedException(FailureReason reasons) : base("Encryption failed.", reasons) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF2/*"/>
  public EncryptionFailedException(FailureReason reasons, string extraText)
    : base("Encryption failed.", reasons, extraText) { }
}
#endregion

#region ExportFailedException
/// <summary>An exception thrown when a key export has failed.</summary>
public class ExportFailedException : PGPException
{
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons0/*"/>
  public ExportFailedException() : base("Export failed.") { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons1/*"/>
  public ExportFailedException(string message) : base(message) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons2/*"/>
  public ExportFailedException(string message, Exception innerException) : base(message, innerException) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF/*"/>
  public ExportFailedException(FailureReason reasons) : base("Export failed.", reasons) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF2/*"/>
  public ExportFailedException(FailureReason reasons, string extraText)
    : base("Export failed.", reasons, extraText) { }
}
#endregion

#region ImportFailedException
/// <summary>An exception thrown when a key import has failed.</summary>
public class ImportFailedException : PGPException
{
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons0/*"/>
  public ImportFailedException() : base("Import failed.") { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons1/*"/>
  public ImportFailedException(string message) : base(message) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons2/*"/>
  public ImportFailedException(string message, Exception innerException) : base(message, innerException) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF/*"/>
  public ImportFailedException(FailureReason reasons) : base("Import failed.", reasons) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF2/*"/>
  public ImportFailedException(FailureReason reasons, string extraText)
    : base("Import failed.", reasons, extraText) { }
}
#endregion

#region KeyEditFailedException
/// <summary>An exception thrown when a key edit operation has failed.</summary>
public class KeyEditFailedException : PGPException
{
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons0/*"/>
  public KeyEditFailedException() : base("Key edit failed.") { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons1/*"/>
  public KeyEditFailedException(string message) : base(message) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons2/*"/>
  public KeyEditFailedException(string message, Exception innerException) : base(message, innerException) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF/*"/>
  public KeyEditFailedException(FailureReason reasons) : base("Key edit failed.", reasons) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF2/*"/>
  public KeyEditFailedException(FailureReason reasons, string extraText)
    : base("Key edit failed.", reasons, extraText) { }
}
#endregion

#region SigningFailedException
/// <summary>An exception thrown when the signing of a document has failed.</summary>
public class SigningFailedException : PGPException
{
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons0/*"/>
  public SigningFailedException() : base("Signing failed.") { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons1/*"/>
  public SigningFailedException(string message) : base(message) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons2/*"/>
  public SigningFailedException(string message, Exception innerException) : base(message, innerException) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF/*"/>
  public SigningFailedException(FailureReason reasons) : base("Signing failed.", reasons) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF2/*"/>
  public SigningFailedException(FailureReason reasons, string extraText)
    : base("Signing failed.", reasons, extraText) { }
}
#endregion

} // namespace AdamMil.Security.PGP