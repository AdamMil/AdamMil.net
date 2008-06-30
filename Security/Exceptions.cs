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

/// <summary>Gives a set of possible reasons for a failure.</summary>
[Flags]
public enum FailureReasons
{
  /// <summary>This value indicates that the reason for failure is completely unknown.</summary>
  None=0,
  /// <summary>The failure could have been caused by a missing secret key, or a secret key for which a password was
  /// not known.
  /// </summary>
  MissingSecretKey,
  /// <summary>The failure could have been caused by a missing public key.</summary>
  MissingPublicKey,
  /// <summary>The failure could have been caused by an incorrect password.</summary>
  BadPassword,
  /// <summary>The failure could have been caused by an unsupported algorithm.</summary>
  UnsupportedAlgorithm,
  /// <summary>The failure could have been caused by invalid data, for instance an attempt to decrypt plaintext.</summary>
  BadData,
  /// <summary>One or more encryption recipients were not valid.</summary>
  InvalidRecipients
}

/// <summary>The base class of exceptions specific to PGP systems.</summary>
public class PGPException : ApplicationException
{
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons0/*"/>
  public PGPException() { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons1/*"/>
  public PGPException(string message) : base(message) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons2/*"/>
  public PGPException(string message, Exception innerException) : base(message, innerException) { }

  /// <summary>Converts a <see cref="FailureReasons"/> value into a human-readable string containing the list of
  /// reasons, or null if no reasons were given.
  /// </summary>
  protected static string GetFailureText(FailureReasons reasons)
  {
    string reasonString = null;
    if((reasons & FailureReasons.MissingSecretKey) != 0) reasonString += " missing or inaccessible secret key.";
    if((reasons & FailureReasons.MissingPublicKey) != 0) reasonString += " missing public key.";
    if((reasons & FailureReasons.BadPassword) != 0) reasonString += " bad or missing passphrase.";
    if((reasons & FailureReasons.UnsupportedAlgorithm) != 0) reasonString += " unsupported algorithm.";
    if((reasons & FailureReasons.BadData) != 0) reasonString += " invalid source data.";
    if((reasons & FailureReasons.InvalidRecipients) != 0) reasonString += " invalid recipient(s).";
    return reasonString == null ? null : " Suspected reasons: " + reasonString;
  }
}

/// <summary>A base class for exceptions that represent failures of the overall PGP operation and present a possible
/// set of reasons for the failure.
/// </summary>
public abstract class PGPOperationFailedException : PGPException
{
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons0/*"/>
  public PGPOperationFailedException() { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons1/*"/>
  public PGPOperationFailedException(string message) : base(message) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons2/*"/>
  public PGPOperationFailedException(string message, Exception innerException) : base(message, innerException) { }

  /// <summary>Initializes a new operation failure exception with the given base message and failure reasons.</summary>
  protected PGPOperationFailedException(string baseMessage, FailureReasons reasons)
    : base(baseMessage + GetFailureText(reasons))
  {
    this.reasons = reasons;
  }

  /// <summary>Initializes a new operation failure exception with the given base message, failure reasons, and
  /// additional text.
  /// </summary>
  protected PGPOperationFailedException(string baseMessage, FailureReasons reasons, string extraText)
    : base(baseMessage + GetFailureText(reasons) + " " + extraText)
  {
    this.reasons = reasons;
  }

  /// <summary>Gets a list of potential causes for the failure.</summary>
  public FailureReasons Reasons
  {
    get { return reasons; }
  }

  readonly FailureReasons reasons;
}

/// <summary>An exception thrown when decryption of a document has failed.</summary>
public class DecryptionFailedException : PGPOperationFailedException
{
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons0/*"/>
  public DecryptionFailedException() { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons1/*"/>
  public DecryptionFailedException(string message) : base(message) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons2/*"/>
  public DecryptionFailedException(string message, Exception innerException) : base(message, innerException) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF/*"/>
  public DecryptionFailedException(FailureReasons reasons) : base("Decryption failed.", reasons) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF2/*"/>
  public DecryptionFailedException(FailureReasons reasons, string extraText)
    : base("Decryption failed.", reasons, extraText) { }
}

/// <summary>An exception thrown when encryption of a document has failed.</summary>
public class EncryptionFailedException : PGPOperationFailedException
{
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons0/*"/>
  public EncryptionFailedException() { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons1/*"/>
  public EncryptionFailedException(string message) : base(message) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons2/*"/>
  public EncryptionFailedException(string message, Exception innerException) : base(message, innerException) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF/*"/>
  public EncryptionFailedException(FailureReasons reasons) : base("Encryption failed.", reasons) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF2/*"/>
  public EncryptionFailedException(FailureReasons reasons, string extraText)
    : base("Encryption failed.", reasons, extraText) { }
}

/// <summary>An exception thrown when the signing of a document has failed.</summary>
public class SigningFailedException : PGPOperationFailedException
{
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons0/*"/>
  public SigningFailedException() { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons1/*"/>
  public SigningFailedException(string message) : base(message) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons2/*"/>
  public SigningFailedException(string message, Exception innerException) : base(message, innerException) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF/*"/>
  public SigningFailedException(FailureReasons reasons) : base("Signing failed.", reasons) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/ConsF2/*"/>
  public SigningFailedException(FailureReasons reasons, string extraText)
    : base("Signing failed.", reasons, extraText) { }
}

} // namespace AdamMil.Security.PGP