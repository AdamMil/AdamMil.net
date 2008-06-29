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

/// <summary>The base class of exceptions specific to PGP systems.</summary>
public class PGPException : ApplicationException
{
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons0/*"/>
  public PGPException() { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons1/*"/>
  public PGPException(string message) : base(message) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons2/*"/>
  public PGPException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>An exception thrown when an encryption recipient is invalid.</summary>
public class InvalidRecipientException : PGPException
{
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons0/*"/>
  public InvalidRecipientException() { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons1/*"/>
  public InvalidRecipientException(string message) : base(message) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons2/*"/>
  public InvalidRecipientException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>An exception thrown when a passphrase provided for signing or decryption is invalid.</summary>
public class WrongPassphraseException : PGPException
{
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons0/*"/>
  public WrongPassphraseException() { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons1/*"/>
  public WrongPassphraseException(string message) : base(message) { }
  /// <include file="documentation.xml" path="/Security/Common/Exception/Cons2/*"/>
  public WrongPassphraseException(string message, Exception innerException) : base(message, innerException) { }
}

} // namespace AdamMil.Security.PGP