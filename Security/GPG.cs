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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using AdamMil.Collections;
using AdamMil.IO;
using AdamMil.Security.PGP.GPG.StatusMessages;
using AdamMil.Utilities;
using Microsoft.Win32.SafeHandles;
using SecureString=System.Security.SecureString;

namespace AdamMil.Security.PGP.GPG
{

/// <summary>Processes text output from GPG.</summary>
public delegate void TextLineHandler(string line);

#region GPG
/// <summary>A base class to aid in the implementation of interfaces to the GNU Privacy Guard (GPG).</summary>
public abstract class GPG : PGPSystem
{
  /// <summary>Parses an argument from a GPG status message into a cipher name, or null if the cipher type cannot be
  /// determined.
  /// </summary>
  public static string ParseCipher(string str)
  {
    switch((OpenPGPCipher)int.Parse(str, CultureInfo.InvariantCulture))
    {
      case OpenPGPCipher.AES: return SymmetricCipher.AES;
      case OpenPGPCipher.AES192: return SymmetricCipher.AES192;
      case OpenPGPCipher.AES256: return SymmetricCipher.AES256;
      case OpenPGPCipher.Blowfish: return SymmetricCipher.Blowfish;
      case OpenPGPCipher.CAST5: return SymmetricCipher.CAST5;
      case OpenPGPCipher.IDEA: return SymmetricCipher.IDEA;
      case OpenPGPCipher.TripleDES: return SymmetricCipher.TripleDES;
      case OpenPGPCipher.Twofish: return SymmetricCipher.Twofish;
      case OpenPGPCipher.DESSK: return "DESSK";
      case OpenPGPCipher.SAFER: return "SAFER";
      case OpenPGPCipher.Unencrypted: return "Unencrypted";
      default: return string.IsNullOrEmpty(str) ? null : str;
    }
  }

  /// <summary>Parses an argument from a GPG status message into a hash algorithm name, or null if the algorithm cannot
  /// be determined.
  /// </summary>
  public static string ParseHashAlgorithm(string str)
  {
    switch((OpenPGPHashAlgorithm)int.Parse(str, CultureInfo.InvariantCulture))
    {
      case OpenPGPHashAlgorithm.MD5: return HashAlgorithm.MD5;
      case OpenPGPHashAlgorithm.RIPEMD160: return HashAlgorithm.RIPEMD160;
      case OpenPGPHashAlgorithm.SHA1: return HashAlgorithm.SHA1;
      case OpenPGPHashAlgorithm.SHA224: return HashAlgorithm.SHA224;
      case OpenPGPHashAlgorithm.SHA256: return HashAlgorithm.SHA256;
      case OpenPGPHashAlgorithm.SHA384: return HashAlgorithm.SHA384;
      case OpenPGPHashAlgorithm.SHA512: return HashAlgorithm.SHA512;
      case OpenPGPHashAlgorithm.HAVAL: return "HAVAL-5-160";
      case OpenPGPHashAlgorithm.MD2: return "MD2";
      case OpenPGPHashAlgorithm.TIGER192: return "TIGER192";
      default: return string.IsNullOrEmpty(str) ? null : str;
    }
  }

  /// <summary>Parses an argument from a GPG status message into a key type name, or null if the key type cannot
  /// be determined.
  /// </summary>
  public static string ParseKeyType(string str)
  {
    switch((OpenPGPKeyType)int.Parse(str, CultureInfo.InvariantCulture))
    {
      case OpenPGPKeyType.DSA:
        return KeyType.DSA;
      case OpenPGPKeyType.ElGamal: case OpenPGPKeyType.ElGamalEncryptOnly:
        return KeyType.ElGamal;
      case OpenPGPKeyType.RSA: case OpenPGPKeyType.RSAEncryptOnly: case OpenPGPKeyType.RSASignOnly:
        return KeyType.RSA;
      default: return string.IsNullOrEmpty(str) ? null : str;
    }
  }

  /// <summary>Parses an argument from a GPG status message into a timestamp.</summary>
  public static DateTime ParseTimestamp(string str)
  {
    if(str.IndexOf('T') == -1) // the time is specified in seconds since Midnight, January 1, 1970
    {
      long seconds = long.Parse(str, CultureInfo.InvariantCulture);
      return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seconds);
    }
    else // the date is in ISO8601 format. DateTime.Parse() can handle it.
    {
      return DateTime.Parse(str, CultureInfo.InvariantCulture);
    }
  }

  /// <summary>Parses an argument from a GPG status message into a timestamp, or null if there is no timestamp.</summary>
  public static DateTime? ParseNullableTimestamp(string str)
  {
    return string.IsNullOrEmpty(str) || str.Equals("0", StringComparison.Ordinal) ?
      (DateTime?)null : ParseTimestamp(str);
  }
}
#endregion

#region ExeGPG
/// <summary>This class implements a connection to the GNU Privacy Guard via piping input to and from its command-line
/// executable.
/// </summary>
public class ExeGPG : GPG
{
  /// <summary>Initializes a new <see cref="ExeGPG"/> with no reference to the GPG executable.</summary>
  public ExeGPG() { }

  /// <summary>Initializes a new <see cref="ExeGPG"/> with a full path to the GPG executable.</summary>
  public ExeGPG(string exePath)
  {
    Initialize(exePath);
  }

  /// <summary>Raised when a line of text is to be logged.</summary>
  public event TextLineHandler LineLogged;

  /// <summary>Gets or sets whether the GPG agent will be used. If enabled, GPG may use its own user interface to query
  /// for passwords, bypassing the support provided by this library. The default is false. However, this property has
  /// no effect when using GPG2, because GPG2 doesn't allow the agent to be disabled.
  /// </summary>
  public bool EnableGPGAgent
  {
    get { return enableAgent; }
    set { enableAgent = value; }
  }

  /// <summary>Gets the path to the GPG executable, or null if <see cref="Initialize"/> has not been called.</summary>
  public string ExecutablePath
  {
    get { return exePath; }
  }

  /// <summary>Gets or sets whether the <see cref="SignatureBase.KeyFingerprint">KeySignature.KeyFingerprint</see>
  /// field will be retrieved. According to the GPG documentation, GPG won't return fingerprints on key signatures
  /// unless signature verification is enabled and signature caching is disabled, due to "various technical reasons".
  /// Checking the signatures and disabling the cache causes a significant performance hit, however, so by default it
  /// is not done. If this property is set to true, the cache will be disabled and signature verification will be
  /// enabled on all signature retrievals, allowing GPG to return the key signature fingerprint. Note that even with
  /// this property set to true, the fingerprint still won't be set if the key signature failed verification.
  /// </summary>
  public bool RetrieveKeySignatureFingerprints
  {
    get { return retrieveKeySignatureFingerprints; }
    set { retrieveKeySignatureFingerprints = value; }
  }

  #region Configuration
  /// <include file="documentation.xml" path="/Security/PGPSystem/GetDefaultPrimaryKeyType/*"/>
  public override string GetDefaultPrimaryKeyType()
  {
    return KeyType.DSA;
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetDefaultSubkeyType/*"/>
  public override string GetDefaultSubkeyType()
  {
    return KeyType.ElGamal;
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetMaximumKeyLength/*"/>
  public override int GetMaximumKeyLength(string keyType)
  {
    if(!string.Equals(keyType, "RSA-E", StringComparison.OrdinalIgnoreCase) &&
       !string.Equals(keyType, "RSA-S", StringComparison.OrdinalIgnoreCase) &&
       !string.Equals(keyType, "ELG-E", StringComparison.OrdinalIgnoreCase) &&
       !string.Equals(keyType, "ELG", StringComparison.OrdinalIgnoreCase))
    {
      AssertSupported(keyType, keyTypes, "key type");
    }

    return string.Equals(keyType, "DSA", StringComparison.OrdinalIgnoreCase) ? 3072 : 4096;
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetSupportedCiphers/*"/>
  public override string[] GetSupportedCiphers()
  {
    AssertInitialized();
    return ciphers == null ? new string[0] : (string[])ciphers.Clone();
  }

  ///  <include file="documentation.xml" path="/Security/PGPSystem/GetSupportedCompressions/*"/>
  public override string[] GetSupportedCompressions()
  {
    AssertInitialized();
    return compressions == null ? new string[0] : (string[])compressions.Clone();
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetSupportedHashes/*"/>
  public override string[] GetSupportedHashes()
  {
    AssertInitialized();
    return hashes == null ? new string[0] : (string[])hashes.Clone();
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetSupportedKeyTypes/*"/>
  public override string[] GetSupportedKeyTypes()
  {
    AssertInitialized();
    return keyTypes == null ? new string[0] : (string[])keyTypes.Clone();
  }
  #endregion

  #region Encryption and signing
  /// <include file="documentation.xml" path="/Security/PGPSystem/SignAndEncrypt/*"/>
  public override void SignAndEncrypt(Stream sourceData, Stream destination, SigningOptions signingOptions,
                                      EncryptionOptions encryptionOptions, OutputOptions outputOptions)
  {
    if(sourceData == null || destination == null || encryptionOptions == null && signingOptions == null)
    {
      throw new ArgumentNullException();
    }

    string args = GetOutputArgs(outputOptions);
    bool symmetric = false; // whether we're doing password-based encryption (possibly in addition to key-based)
    bool customAlgo = false; // whether a custom algorithm was specified

    // add the keyrings of all the recipient and signer keys to the command line
    List<PrimaryKey> keyringKeys = new List<PrimaryKey>();

    if(encryptionOptions != null) // if we'll be doing any encryption
    {
      // we can't do signing with detached signatures because GPG doesn't have a way to specify the two output files.
      // and encryption with
      if(signingOptions != null && signingOptions.Type != SignatureType.Embedded)
      {
        if(signingOptions.Type == SignatureType.ClearSignedText)
        {
          throw new ArgumentException("Combining encryption with clear-signing does not make sense, because the data "+
                                      "cannot be both encrypted and in the clear.");
        }
        else
        {
          throw new NotSupportedException("Simultaneous encryption and detached signing is not supported by GPG. "+
                                          "Perform the encryption and detached signing as two separate steps.");
        }
      }

      symmetric = encryptionOptions.Password != null && encryptionOptions.Password.Length != 0;

      // we need recipients if we're not doing password-based encryption
      if(!symmetric && encryptionOptions.Recipients.Count == 0 && encryptionOptions.HiddenRecipients.Count == 0)
      {
        throw new ArgumentException("No recipients were specified.");
      }

      keyringKeys.AddRange(encryptionOptions.Recipients);
      keyringKeys.AddRange(encryptionOptions.HiddenRecipients);

      // if there are recipients for key-based encryption, add them to the command line
      if(encryptionOptions.Recipients.Count != 0 || encryptionOptions.HiddenRecipients.Count != 0)
      {
        args += GetFingerprintArgs(encryptionOptions.Recipients, "-r") +
                GetFingerprintArgs(encryptionOptions.HiddenRecipients, "-R") + "-e "; // plus the encrypt command
      }

      if(!string.IsNullOrEmpty(encryptionOptions.Cipher))
      {
        AssertSupported(encryptionOptions.Cipher, ciphers, "cipher");
        args += "--cipher-algo " + EscapeArg(encryptionOptions.Cipher) + " ";
        customAlgo = true;
      }

      if(symmetric) args += "-c "; // add the password-based encryption command if necessary

      if(encryptionOptions.AlwaysTrustRecipients) args += "--trust-model always ";
    }

    if(signingOptions != null) // if we'll be doing any signing
    {
      if(signingOptions.Signers.Count == 0) throw new ArgumentException("No signers were specified.");

      // add the keyrings of the signers to the command prompt
      keyringKeys.AddRange(signingOptions.Signers);

      if(!string.IsNullOrEmpty(signingOptions.Hash))
      {
        AssertSupported(encryptionOptions.Cipher, hashes, "hash");
        args += "--digest-algo "+EscapeArg(signingOptions.Hash)+" ";
        customAlgo = true;
      }

      // add all of the signers to the command line, and the signing command
      args += GetFingerprintArgs(signingOptions.Signers, "-u") +
        (signingOptions.Type == SignatureType.Detached ? "-b " :
         signingOptions.Type == SignatureType.ClearSignedText ? "--clearsign " : "-s ");
    }

    args += GetKeyringArgs(keyringKeys, true); // add all the keyrings to the command line

    Command cmd = Execute(args, StatusMessages.ReadInBackground, false);
    CommandState state = new CommandState(cmd);
    if(customAlgo) state.FailureReasons |= FailureReason.UnsupportedAlgorithm; // using a custom algo can cause failure

    using(ManualResetEvent ready = new ManualResetEvent(false)) // create an event to signal when the data
    using(cmd)                                                  // should be sent
    {
      cmd.InputNeeded += delegate(string promptId)
      {
        if(string.Equals(promptId, "untrusted_key.override", StringComparison.Ordinal))
        { // this question indicates that a recipient key is not trusted
          bool alwaysTrust = encryptionOptions != null && encryptionOptions.AlwaysTrustRecipients;
          if(!alwaysTrust) state.FailureReasons |= FailureReason.UntrustedRecipient;
          cmd.SendLine(alwaysTrust ? "Y" : "N");
        }
        else if(string.Equals(promptId, "passphrase.enter", StringComparison.Ordinal) &&
                state.PasswordMessage != null && state.PasswordMessage.Type == StatusMessageType.NeedCipherPassphrase)
        {
          cmd.SendPassword(encryptionOptions.Password, false);
        }
        else if(!state.Canceled)
        {
          DefaultPromptHandler(promptId, state);
          if(state.Canceled) cmd.Kill(); // kill GPG if the user doesn't give the password, so it doesn't keep asking
        }
      };

      cmd.StatusMessageReceived += delegate(StatusMessage msg)
      {
        switch(msg.Type)
        {
          case StatusMessageType.BeginEncryption: case StatusMessageType.BeginSigning:
            ready.Set(); // all set. send the data!
            break;

          default: DefaultStatusMessageHandler(msg, state); break;
        }
      };

      cmd.Start();

      // wait until it's time to write the data or the process aborted
      while(!ready.WaitOne(50, false) && !cmd.Process.HasExited) { }

      // if the process is still running and it didn't exit before we could copy the input data...
      if(!cmd.Process.HasExited) ReadAndWriteStreams(destination, sourceData, cmd.Process);

      cmd.WaitForExit(); // and wait for the command to finish
    }

    if(!cmd.SuccessfulExit) // if the process wasn't successful, throw an exception
    {
      if(state.Canceled) throw new OperationCanceledException();
      else if(encryptionOptions != null) throw new EncryptionFailedException(state.FailureReasons);
      else throw new SigningFailedException(state.FailureReasons);
    }
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/Decrypt/*"/>
  public override Signature[] Decrypt(Stream ciphertext, Stream destination, DecryptionOptions options)
  {
    if(ciphertext == null || destination == null) throw new ArgumentNullException();

    Command cmd = Execute(GetVerificationArgs(options, true) + "-d", StatusMessages.ReadInBackground, false);
    return DecryptVerifyCore(cmd, ciphertext, destination, options);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/Verify2/*"/>
  public override Signature[] Verify(Stream signedData, VerificationOptions options)
  {
    if(signedData == null) throw new ArgumentNullException();
    return VerifyCore(null, signedData, options);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/Verify3/*"/>
  /// <remarks>The signature data (from <paramref name="signature"/>) will be written into a temporary file for the
  /// duration of this method call.
  /// </remarks>
  public override Signature[] Verify(Stream signedData, Stream signature, VerificationOptions options)
  {
    if(signedData == null || signature == null) throw new ArgumentNullException();

    // copy the signature into a temporary file, because we can't pass both streams on standard input
    string sigFileName = Path.GetTempFileName();
    try
    {
      using(FileStream file = new FileStream(sigFileName, FileMode.Truncate, FileAccess.Write))
      {
        signature.CopyTo(file);
      }

      return VerifyCore(sigFileName, signedData, options);
    }
    finally { File.Delete(sigFileName); }
  }
  #endregion

  #region Key import and export
  /// <include file="documentation.xml" path="/Security/PGPSystem/ExportKeys/*"/>
  public override void ExportKeys(PrimaryKey[] keys, Stream destination, ExportOptions exportOptions,
                                  OutputOptions outputOptions)
  {
    if(keys == null || destination == null) throw new ArgumentNullException();

    if((exportOptions & (ExportOptions.ExportPublicKeys|ExportOptions.ExportSecretKeys)) == 0)
    {
      throw new ArgumentException("At least one of ExportOptions.ExportPublicKeys or ExportOptions.ExportSecretKeys "+
                                  "must be specified.");
    }

    if(keys.Length == 0) return;

    if((exportOptions & ExportOptions.ExportPublicKeys) != 0)
    {
      string args = GetKeyringArgs(keys, false) + GetExportArgs(exportOptions, false, true) +
                    GetOutputArgs(outputOptions) + GetFingerprintArgs(keys);
      ExportCore(args, destination);
    }

    if((exportOptions & ExportOptions.ExportSecretKeys) != 0)
    {
      string args = GetKeyringArgs(keys, true) + GetExportArgs(exportOptions, true, true) +
                    GetOutputArgs(outputOptions) + GetFingerprintArgs(keys);
      ExportCore(args, destination);
    }
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ExportKeys2/*"/>
  public override void ExportKeys(Keyring[] keyrings, bool includeDefaultKeyring, Stream destination,
                                  ExportOptions exportOptions, OutputOptions outputOptions)
  {
    if(destination == null) throw new ArgumentNullException();

    if((exportOptions & (ExportOptions.ExportPublicKeys|ExportOptions.ExportSecretKeys)) == 0)
    {
      throw new ArgumentException("At least one of ExportOptions.ExportPublicKeys or ExportOptions.ExportSecretKeys "+
                                  "must be specified.");
    }

    if((exportOptions & ExportOptions.ExportPublicKeys) != 0)
    {
      string args = GetKeyringArgs(keyrings, !includeDefaultKeyring, false) +
                    GetExportArgs(exportOptions, false, true) + GetOutputArgs(outputOptions);
      ExportCore(args, destination);
    }

    if((exportOptions & ExportOptions.ExportSecretKeys) != 0)
    {
      string args = GetKeyringArgs(keyrings, !includeDefaultKeyring, true) +
                    GetExportArgs(exportOptions, true, true) + GetOutputArgs(outputOptions);
      ExportCore(args, destination);
    }
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ImportKeys3/*"/>
  public override ImportedKey[] ImportKeys(Stream source, Keyring keyring, ImportOptions options)
  {
    if(source == null) throw new ArgumentNullException();

    CommandState state;
    Command cmd = Execute(GetImportArgs(keyring, options) + "--import", StatusMessages.ReadInBackground, false);
    ImportedKey[] keys = ImportCore(cmd, source, out state);
    if(!cmd.SuccessfulExit) throw new ImportFailedException(state.FailureReasons);
    return keys;
  }
  #endregion

  #region Key revocation
  /// <include file="documentation.xml" path="/Security/PGPSystem/AddDesignatedRevoker/*" />
  public override void AddDesignatedRevoker(PrimaryKey key, PrimaryKey revokerKey)
  {
    if(key == null || revokerKey == null) throw new ArgumentNullException();

    if(string.IsNullOrEmpty(revokerKey.Fingerprint))
    {
      throw new ArgumentException("The revoker key has no fingerprint.");
    }

    if(string.Equals(key.Fingerprint, revokerKey.Fingerprint, StringComparison.Ordinal))
    {
      throw new ArgumentException("You can't add a key as its own designated revoker.");
    }

    DoEdit(key, GetKeyringArgs(new PrimaryKey[] { key, revokerKey }, true), false,
           new AddRevokerCommand(revokerKey.Fingerprint));
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GenerateRevocationCertificate/*" />
  public override void GenerateRevocationCertificate(PrimaryKey key, Stream destination, KeyRevocationReason reason,
                                                     OutputOptions outputOptions)
  {
    GenerateRevocationCertificateCore(key, null, destination, reason, outputOptions);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GenerateRevocationCertificateD/*" />
  public override void GenerateRevocationCertificate(PrimaryKey keyToRevoke, PrimaryKey designatedRevoker,
                                                     Stream destination, KeyRevocationReason reason,
                                                     OutputOptions outputOptions)
  {
    if(designatedRevoker == null) throw new ArgumentNullException();
    GenerateRevocationCertificateCore(keyToRevoke, designatedRevoker, destination, reason, outputOptions);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/RevokeKeys/*" />
  public override void RevokeKeys(KeyRevocationReason reason, params PrimaryKey[] keys)
  {
    RevokeKeysCore(null, reason, keys);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/RevokeKeysD/*" />
  public override void RevokeKeys(PrimaryKey designatedRevoker, KeyRevocationReason reason, params PrimaryKey[] keys)
  {
    if(designatedRevoker == null) throw new ArgumentNullException();
    RevokeKeysCore(designatedRevoker, reason, keys);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/RevokeSubkeys/*" />
  public override void RevokeSubkeys(KeyRevocationReason reason, params Subkey[] subkeys)
  {
    EditSubkeys(subkeys, delegate { return new RevokeSubkeysCommand(reason); });
  }
  #endregion

  #region Key server operations
  /// <include file="documentation.xml" path="/Security/PGPSystem/FindKeysOnServer/*"/>
  public override void FindKeysOnServer(Uri keyServer, KeySearchHandler handler, params string[] searchKeywords)
  {
    if(keyServer == null || handler == null || searchKeywords == null) throw new ArgumentNullException();
    if(searchKeywords.Length == 0) throw new ArgumentException("No keywords were given.");

    string args = "--keyserver " + EscapeArg(keyServer.AbsoluteUri) + " --with-colons --fixed-list-mode --search-keys";
    foreach(string keyword in searchKeywords) args += " " + EscapeArg(keyword);

    Command cmd = Execute(args, StatusMessages.MixIntoStdout, true);
    CommandState state = new CommandState(cmd);
    using(cmd)
    {
      List<PrimaryKey> keysFound = new List<PrimaryKey>();
      List<UserId> userIds = new List<UserId>();

      cmd.StandardErrorLine += delegate(string line) { DefaultStandardErrorHandler(line, state); };

      cmd.Start();

      while(true)
      {
        string line;
        cmd.ReadLine(out line);
        if(line != null) LogLine(line);

        gotLine:
        if(line == null && cmd.StatusMessage == null) break;

        if(line == null)
        {
          switch(cmd.StatusMessage.Type)
          {
            case StatusMessageType.GetLine:
              GetInputMessage m = (GetInputMessage)cmd.StatusMessage;
              if(string.Equals(m.PromptId, "keysearch.prompt", StringComparison.Ordinal))
              {
                // we're done with this chunk of the search, so we'll give the keys to the search handler.
                // we won't continue if we didn't find anything, even if the handler returns true
                bool shouldContinue = keysFound.Count != 0 && handler(keysFound.ToArray());
                cmd.SendLine(shouldContinue ? "N" : "Q");
                keysFound.Clear();
                break;
              }
              else goto default;

            default: DefaultStatusMessageHandler(cmd.StatusMessage, state); break;
          }
        }
        else if(line.StartsWith("pub:", StringComparison.Ordinal)) // a key description follows
        {
          string[] fields = line.Split(':');

          PrimaryKey key = new PrimaryKey();

          if(IsValidKeyId(fields[1])) key.KeyId = fields[1].ToUpperInvariant();
          else if(IsValidFingerprint(fields[1])) key.Fingerprint = fields[1].ToUpperInvariant();
          else // there's no valid ID, so skip any related records that follow
          {
            do
            {
              cmd.ReadLine(out line);
              if(line != null) LogLine(line);
            }
            while(line != null && !line.StartsWith("pub:", StringComparison.Ordinal));
            goto gotLine;
          }

          if(fields.Length > 2 && !string.IsNullOrEmpty(fields[2])) key.KeyType = ParseKeyType(fields[2]);
          if(fields.Length > 3 && !string.IsNullOrEmpty(fields[3])) key.Length = int.Parse(fields[3]);
          if(fields.Length > 4 && !string.IsNullOrEmpty(fields[4])) key.CreationTime = ParseTimestamp(fields[4]);
          if(fields.Length > 5 && !string.IsNullOrEmpty(fields[5])) key.ExpirationTime = ParseNullableTimestamp(fields[5]);

          if(fields.Length > 6 && !string.IsNullOrEmpty(fields[6]))
          {
            foreach(char c in fields[6])
            {
              switch(char.ToLowerInvariant(c))
              {
                case 'd': key.Disabled = true; break;
                case 'e': key.Expired = true; break;
                case 'r': key.Revoked = true; break;
              }
            }
          }

          // now parse the user IDs
          while(true)
          {
            cmd.ReadLine(out line);
            if(line == null) break; // if we hit a status message or EOF, break

            LogLine(line);
            if(line.StartsWith("pub:", StringComparison.Ordinal)) break;
            else if(!line.StartsWith("uid", StringComparison.Ordinal)) continue;

            fields = line.Split(':');
            if(string.IsNullOrEmpty(fields[1])) continue;

            UserId id = new UserId();
            id.PrimaryKey = key;
            id.Name       = CUnescape(fields[1]);
            id.Signatures = NoSignatures;
            if(fields.Length > 2 && !string.IsNullOrEmpty(fields[2])) id.CreationTime = ParseTimestamp(fields[2]);
            id.MakeReadOnly();
            userIds.Add(id);
          }

          if(userIds.Count != 0)
          {
            key.Attributes         = NoAttributes;
            key.DesignatedRevokers = NoRevokers;
            key.Signatures         = NoSignatures;
            key.Subkeys            = NoSubkeys;
            key.UserIds            = new ReadOnlyListWrapper<UserId>(userIds.ToArray());
            key.MakeReadOnly();
            keysFound.Add(key);

            userIds.Clear();
          }

          goto gotLine;
        }
      }

      cmd.WaitForExit();
    }

    if(!cmd.SuccessfulExit) throw new KeyServerFailedException("Key search failed.", state.FailureReasons);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ImportKeysFromServer/*"/>
  public override ImportedKey[] ImportKeysFromServer(KeyDownloadOptions options, Keyring keyring,
                                                     params string[] keyFingerprintsOrIds)
  {
    if(keyFingerprintsOrIds == null) throw new ArgumentNullException();
    if(keyFingerprintsOrIds.Length == 0) return new ImportedKey[0];

    string args = GetKeyServerArgs(options, true) + GetImportArgs(keyring, options.ImportOptions) + "--recv-keys";
    foreach(string id in keyFingerprintsOrIds)
    {
      if(string.IsNullOrEmpty(id)) throw new ArgumentException("A key ID was null or empty.");
      args += " " + id;
    }
    return KeyServerCore(args, "Key import", true);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/RefreshKeyringFromServer/*"/>
  public override ImportedKey[] RefreshKeysFromServer(KeyDownloadOptions options, Keyring keyring)
  {
    string args = GetImportArgs(keyring, options == null ? ImportOptions.Default : options.ImportOptions) +
                  GetKeyServerArgs(options, false) + "--refresh-keys";
    return KeyServerCore(args, "Keyring refresh", true);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/RefreshKeysFromServer/*"/>
  public override ImportedKey[] RefreshKeysFromServer(KeyDownloadOptions options, params PrimaryKey[] keys)
  {
    if(keys == null) throw new ArgumentNullException();
    if(keys.Length == 0) return new ImportedKey[0];

    string args = GetKeyringArgs(keys, true) + GetKeyServerArgs(options, false) +
                  GetImportArgs(null, options == null ? ImportOptions.Default : options.ImportOptions) +
                  "--refresh-keys " + GetFingerprintArgs(keys);
    return KeyServerCore(args, "Key refresh", true);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/UploadKeys/*"/>
  public override void UploadKeys(KeyUploadOptions options, params PrimaryKey[] keys)
  {
    if(keys == null) throw new ArgumentNullException();
    if(keys.Length == 0) return;

    string args = GetKeyringArgs(keys, false) + GetKeyServerArgs(options, true) +
                  GetExportArgs(options.ExportOptions, false, false) + "--send-keys " + GetFingerprintArgs(keys);
    KeyServerCore(args, "Key upload", false);
  }
  #endregion

  #region Key signing
  /// <include file="documentation.xml" path="/Security/PGPSystem/DeleteSignatures/*" />
  public override void DeleteSignatures(params KeySignature[] signatures)
  {
    EditSignatures(signatures, delegate(KeySignature[] sigs) { return new DeleteSigsCommand(sigs); });
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/RevokeSignatures/*" />
  public override void RevokeSignatures(UserRevocationReason reason, params KeySignature[] signatures)
  {
    EditSignatures(signatures, delegate(KeySignature[] sigs) { return new RevokeSigsCommand(reason, sigs); });
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/SignAttributes/*"/>
  public override void SignAttributes(UserAttribute[] attributes, PrimaryKey signingKey, KeySigningOptions options)
  {
    if(attributes == null || signingKey == null) throw new ArgumentNullException();

    foreach(List<UserAttribute> attrList in GroupAttributesByKey(attributes))
    {
      EditCommand[] commands = new EditCommand[attrList.Count+1];
      for(int i=0; i<attrList.Count; i++) commands[i] = new RawCommand("uid " + attrList[i].Id);
      commands[attrList.Count] = new SignKeyCommand(options, false);

      PrimaryKey keyToEdit = attrList[0].PrimaryKey;
      DoEdit(keyToEdit, "--ask-cert-level " + GetKeyringArgs(new PrimaryKey[] { keyToEdit, signingKey }, true) +
             "-u " + signingKey.Fingerprint, false, commands);
    }
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/SignKeys/*" />
  public override void SignKeys(PrimaryKey[] keysToSign, PrimaryKey signingKey, KeySigningOptions options)
  {
    if(keysToSign == null || signingKey == null) throw new ArgumentNullException();

    foreach(PrimaryKey key in keysToSign)
    {
      if(key == null) throw new ArgumentException("A key was null.");
      DoEdit(key, "--ask-cert-level " + GetKeyringArgs(new PrimaryKey[] { key, signingKey }, true) +
             "-u " + signingKey.Fingerprint, false, new SignKeyCommand(options, true));
    }
  }
  #endregion

  #region Keyring queries
  /// <include file="documentation.xml" path="/Security/PGPSystem/FindKey/*"/>
  public override PrimaryKey FindKey(string keywordOrId, Keyring keyring, ListOptions options)
  {
    PrimaryKey[] keys = FindKeys(new string[] { keywordOrId },
                                 keyring == null ? null : new Keyring[] { keyring }, keyring == null, options);
    return keys[0];
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/FindKeys/*"/>
  public override PrimaryKey[] FindKeys(string[] fingerprintsOrIds, Keyring[] keyrings,
                                        bool includeDefaultKeyring, ListOptions options)
  {
    if(fingerprintsOrIds == null) throw new ArgumentNullException();
    if(fingerprintsOrIds.Length == 0) return new PrimaryKey[0];

    // create search arguments containing all the key IDs
    string searchArgs = null;

    if(fingerprintsOrIds.Length > 1) // if there's more than one ID, we can't allow fancy matches like email addresses,
    {                                // so validate and normalize all IDs
      // clone the array so we don't modify the parameters
      fingerprintsOrIds = (string[])fingerprintsOrIds.Clone();
      for(int i=0; i<fingerprintsOrIds.Length; i++)
      {
        if(string.IsNullOrEmpty(fingerprintsOrIds[i]))
        {
          throw new ArgumentException("A fingerprint/ID was null or empty.");
        }
        fingerprintsOrIds[i] = NormalizeKeyId(fingerprintsOrIds[i]);
      }
    }

    // add all IDs to the command line
    foreach(string id in fingerprintsOrIds) searchArgs += EscapeArg(id) + " ";
    PrimaryKey[] keys = GetKeys(keyrings, includeDefaultKeyring, options, searchArgs);

    if(fingerprintsOrIds.Length == 1) // if there was only a single key returned, then that's the one
    {
      return keys.Length == 1 ? keys : new PrimaryKey[1];
    }
    else
    {
      // add each key found to a dictionary
      Dictionary<string, PrimaryKey> keyDict = new Dictionary<string, PrimaryKey>();
      foreach(PrimaryKey key in keys)
      {
        keyDict[key.Fingerprint] = key;
        keyDict[key.KeyId]       = key;
        keyDict[key.ShortKeyId]  = key;
      }

      // then create the return array and return the keys found
      if(keys.Length != fingerprintsOrIds.Length) keys = new PrimaryKey[fingerprintsOrIds.Length];
      for(int i=0; i<keys.Length; i++) keyDict.TryGetValue(fingerprintsOrIds[i], out keys[i]);
      return keys;
    }
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetKeys/*"/>
  public override PrimaryKey[] GetKeys(Keyring[] keyrings, bool includeDefaultKeyring, ListOptions options)
  {
    return GetKeys(keyrings, includeDefaultKeyring, options, null);
  }
  #endregion

  #region Miscellaneous
  /// <include file="documentation.xml" path="/Security/PGPSystem/CreateTrustDatabase/*"/>
  public override void CreateTrustDatabase(string path)
  {
    // the following creates a valid, empty version 3 trust database. (see gpg-src\doc\DETAILS)
    using(FileStream dbFile = File.Open(path, FileMode.Create, FileAccess.Write))
    {
      dbFile.SetLength(40); // the database is 40 bytes long, but only the first 16 bytes are non-zero

      byte[] headerStart = new byte[] { 1, 0x67, 0x70, 0x67, 3, 3, 1, 5, 1, 0, 0, 0 };
      dbFile.Write(headerStart, 0, headerStart.Length);

      // the next four bytes are the big-endian creation timestamp in seconds since epoch
      dbFile.WriteBE4((int)((DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds));
    }
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GenerateRandomData/*"/>
  public override void GetRandomData(Randomness quality, byte[] buffer, int index, int count)
  {
    Utility.ValidateRange(buffer, index, count);
    if(count == 0) return;

    // "gpg --gen-random QUALITY COUNT" writes random COUNT bytes to standard output. QUALITY is a value from 0 to 2
    // representing the quality of the random number generator to use
    string qualityArg;
    if(quality == Randomness.Weak) qualityArg = "0";
    else if(quality == Randomness.TooStrong) qualityArg = "2";
    else qualityArg = "1"; // we'll default to the Strong level

    Command cmd = Execute("--gen-random " + qualityArg + " " + count.ToInvariantString(),
                          StatusMessages.Ignore, true);
    using(cmd)
    {
      cmd.Start();
      count -= cmd.Process.StandardOutput.BaseStream.Read(buffer, 0, count, false);
      cmd.WaitForExit();
    }

    if(count != 0) throw new PGPException("GPG didn't write enough random bytes.");
    cmd.CheckExitCode();
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/Hash/*"/>
  public override byte[] Hash(Stream data, string hashAlgorithm)
  {
    if(data == null) throw new ArgumentNullException();

    bool customAlgorithm = false;
    if(hashAlgorithm == null || hashAlgorithm == HashAlgorithm.Default)
    {
      hashAlgorithm = HashAlgorithm.SHA1;
    }
    else if(hashAlgorithm.Length == 0)
    {
      throw new ArgumentException("Unspecified hash algorithm.");
    }
    else
    {
      AssertSupported(hashAlgorithm, hashes, "hash");
      customAlgorithm = true;
    }

    // "gpg --print-md ALGO" hashes data presented on standard input. if the algorithm is not supported, gpg exits
    // immediately with error code 2. otherwise, it consumes all available input, and then prints the hash in a
    // human-readable form, with hex digits nicely formatted into blocks and lines. we'll feed it all the input and
    // then read the output.
    List<byte> hash = new List<byte>();
    Command cmd = Execute("--print-md " + EscapeArg(hashAlgorithm), StatusMessages.Ignore, false);
    using(cmd)
    {
      cmd.Start();

      if(WriteStreamToProcess(data, cmd.Process))
      {
        while(true)
        {
          string line = cmd.Process.StandardOutput.ReadLine();
          if(line == null) break;

          // on each line, there are some hex digits separated with whitespace. we'll read each character, but only
          // use characters that are valid hex digits
          int value = 0, chars = 0;
          foreach(char c in line.ToLowerInvariant())
          {
            if(IsHexDigit(c))
            {
              value = (value<<4) + GetHexValue(c);
              if(++chars == 2) // when two hex digits have accumulated, a byte is complete, so write it to the output
              {
                hash.Add((byte)value);
                chars = 0;
              }
            }
          }
        }
      }

      cmd.WaitForExit();
    }

    if(!cmd.SuccessfulExit || hash.Count == 0)
    {
      throw new PGPException("Hash failed.",
                             customAlgorithm ? FailureReason.UnsupportedAlgorithm : FailureReason.None);
    }

    return hash.ToArray();
  }
  #endregion

  #region Primary key management
  /// <include file="documentation.xml" path="/Security/PGPSystem/AddSubkey/*" />
  public override void AddSubkey(PrimaryKey key, string keyType, KeyCapabilities capabilities, int keyLength,
                                 DateTime? expiration)
  {
    // if a custom length is specified, it might be long enough to require a DSA2 key, so add the option just in case
    DoEdit(key, (keyLength == 0 ? null : "--enable-dsa2 ") + "--expert", true,
           new AddSubkeyCommand(keyType, capabilities, keyLength, expiration));
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ChangeExpiration/*" />
  public override void ChangeExpiration(Key key, DateTime? expiration)
  {
    if(key == null) throw new ArgumentNullException();

    Subkey subkey = key as Subkey;
    if(subkey == null) // if it's not a subkey, we'll assume it's a primary key, and change that
    {
      DoEdit(key.GetPrimaryKey(), new ChangeExpirationCommand(expiration));
    }
    else // otherwise, first select the subkey
    {
      DoEdit(key.GetPrimaryKey(), new SelectSubkeyCommand(subkey.Fingerprint, true), new ChangeExpirationCommand(expiration));
    }
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ChangePassword/*" />
  public override void ChangePassword(PrimaryKey key, SecureString password)
  {
    DoEdit(key, new ChangePasswordCommand(password));
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/CleanKeys/*" />
  public override void CleanKeys(params PrimaryKey[] keys)
  {
    RepeatedRawEditCommand(keys, "clean");
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/CreateKey/*"/>
  /// <remarks>If <see cref="NewKeyOptions.Keyring"/> is set, the key will not be automatically trusted in the default
  /// trust database.
  /// </remarks>
  public override PrimaryKey CreateKey(NewKeyOptions options)
  {
    if(options == null) throw new ArgumentNullException();

    string email = Trim(options.Email), realName = Trim(options.RealName), comment = Trim(options.Comment);
    if(string.IsNullOrEmpty(email) && string.IsNullOrEmpty(realName))
    {
      throw new ArgumentException("At least one of NewKeyOptions.Email or NewKeyOptions.RealName must be set.");
    }

    if(ContainsControlCharacters(options.Comment + options.Email + options.RealName + options.Password))
    {
      throw new ArgumentException("The comment, email, real name, and/or password contains control characters. "+
                                  "Remove them.");
    }

    bool primaryIsDSA = string.IsNullOrEmpty(options.KeyType) || // DSA is the default primary key type
                        string.Equals(options.KeyType, KeyType.DSA, StringComparison.OrdinalIgnoreCase);
    bool primaryIsRSA = string.Equals(options.KeyType, KeyType.RSA, StringComparison.OrdinalIgnoreCase);

    if(!primaryIsDSA && !primaryIsRSA)
    {
      throw new KeyCreationFailedException(FailureReason.UnsupportedAlgorithm,
                                           "Primary key type "+options.KeyType+" is not supported.");
    }

    // GPG supports key sizes from 1024 to 3072 (for DSA keys) or 4096 (for other keys)
    int maxKeyLength = primaryIsDSA ? 3072 : 4096;
    if(options.KeyLength != 0 && (options.KeyLength < 1024 || options.KeyLength > maxKeyLength))
    {
      throw new KeyCreationFailedException(FailureReason.None, "Key length " +
        options.KeyLength.ToInvariantString() + " is not supported.");
    }

    bool subIsDSA = string.Equals(options.SubkeyType, KeyType.DSA, StringComparison.OrdinalIgnoreCase);
    bool subIsELG = string.IsNullOrEmpty(options.SubkeyType) || // ElGamal is the default subkey type
                    string.Equals(options.SubkeyType, KeyType.ElGamal, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(options.SubkeyType, "ELG-E", StringComparison.OrdinalIgnoreCase);
    bool subIsRSA  = string.Equals(options.SubkeyType, KeyType.RSA, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(options.SubkeyType, "RSA-E", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(options.SubkeyType, "RSA-S", StringComparison.OrdinalIgnoreCase);
    bool subIsNone = string.Equals(options.SubkeyType, KeyType.None, StringComparison.OrdinalIgnoreCase);

    if(!subIsNone && !subIsDSA && !subIsELG && !subIsRSA)
    {
      throw new KeyCreationFailedException(FailureReason.UnsupportedAlgorithm,
                                           "Subkey type "+options.SubkeyType+" is not supported.");
    }

    KeyCapabilities primaryCapabilities = options.KeyCapabilities, subCapabilities = options.SubkeyCapabilities;

    if(primaryCapabilities == KeyCapabilities.Default)
    {
      primaryCapabilities = KeyCapabilities.Certify | KeyCapabilities.Sign | KeyCapabilities.Authenticate;
    }

    if(!subIsNone) // if a subkey will be created
    {
      // GPG supports key sizes from 1024 to 3072 (for DSA keys) or 4096 (for other keys)
      maxKeyLength = subIsDSA ? 3072 : 4096;
      if(options.SubkeyLength != 0 && (options.SubkeyLength < 1024 || options.SubkeyLength > maxKeyLength))
      {
        throw new KeyCreationFailedException(FailureReason.None, "Key length "+
          options.SubkeyLength.ToInvariantString() + " is not supported.");
      }

      if(subCapabilities == KeyCapabilities.Default)
      {
        subCapabilities = subIsDSA ? KeyCapabilities.Sign : KeyCapabilities.Encrypt;
      }
    }

    int keyExpirationDays = GetExpirationDays(options.KeyExpiration);
    int subkeyExpirationDays = GetExpirationDays(options.SubkeyExpiration);

    // the options look good, so lets make the key
    string keyFingerprint = null, args = GetKeyringArgs(options.Keyring, true);

    // if we're using DSA keys greater than 1024 bits, we need to enable DSA2 support
    if(primaryIsDSA && options.KeyLength > 1024 || subIsDSA && options.SubkeyLength > 1024) args += "--enable-dsa2 ";

    Command cmd = Execute(args + "--batch --gen-key", StatusMessages.ReadInBackground, false);
    CommandState state = new CommandState(cmd);
    using(cmd)
    {
      cmd.StandardErrorLine += delegate(string line) { DefaultStandardErrorHandler(line, state); };

      cmd.StatusMessageReceived += delegate(StatusMessage msg)
      {
        if(msg.Type == StatusMessageType.KeyCreated) // when the key is created, grab its fingerprint
        {
          KeyCreatedMessage m = (KeyCreatedMessage)msg;
          if(m.PrimaryKeyCreated) keyFingerprint = m.Fingerprint;
        }
        else DefaultStatusMessageHandler(msg, state);
      };

      cmd.Start();

      cmd.Process.StandardInput.WriteLine("Key-Type: " + (primaryIsDSA ? "DSA" : "RSA"));

      int keyLength = options.KeyLength != 0 ? options.KeyLength : primaryIsDSA ? 1024 : 2048;
      cmd.Process.StandardInput.WriteLine("Key-Length: " + keyLength.ToInvariantString());

      cmd.Process.StandardInput.WriteLine("Key-Usage: " + GetKeyUsageString(primaryCapabilities));

      if(!subIsNone)
      {
        cmd.Process.StandardInput.WriteLine("Subkey-Type: " + (subIsDSA ? "DSA" : subIsELG ? "ELG-E" : "RSA"));
        cmd.Process.StandardInput.WriteLine("Subkey-Usage: " + GetKeyUsageString(subCapabilities));

        keyLength = options.SubkeyLength != 0 ? options.SubkeyLength : subIsDSA ? 1024 : 2048;
        cmd.Process.StandardInput.WriteLine("Subkey-Length: " + keyLength.ToInvariantString());
      }

      if(!string.IsNullOrEmpty(realName)) cmd.Process.StandardInput.WriteLine("Name-Real: " + realName);
      if(!string.IsNullOrEmpty(email)) cmd.Process.StandardInput.WriteLine("Name-Email: " + email);
      if(!string.IsNullOrEmpty(comment)) cmd.Process.StandardInput.WriteLine("Name-Comment: " + comment);

      if(options.Password != null && options.Password.Length != 0)
      {
        cmd.Process.StandardInput.Write("Passphrase: ");
        SecurityUtility.ProcessSecureString(options.Password,
                                            delegate(char[] chars) { cmd.Process.StandardInput.WriteLine(chars); });
      }

      // GPG doesn't allow separate expiration dates for the primary key and subkey during key creation, so we'll
      // just use the primary key's expiration date and set the subkey's date later
      if(options.KeyExpiration.HasValue)
      {
        cmd.Process.StandardInput.WriteLine("Expire-Date: " +
                                            keyExpirationDays.ToInvariantString() + "d");
      }

      cmd.Process.StandardInput.Close(); // close STDIN so GPG can start generating the key
      cmd.WaitForExit(); // wait for it to finish
    }

    PrimaryKey newKey = !cmd.SuccessfulExit || keyFingerprint == null ?
                          null : FindKey(keyFingerprint, options.Keyring, ListOptions.Default);
    if(newKey == null) throw new KeyCreationFailedException(state.FailureReasons);

    if(!subIsNone && keyExpirationDays != subkeyExpirationDays)
    {
      try
      {
        DoEdit(newKey, new SetDefaultPasswordCommand(options.Password),
               new SelectSubkeyCommand(newKey.Subkeys[0].Fingerprint, true),
               new ChangeExpirationCommand(options.SubkeyExpiration));
        newKey = RefreshKey(newKey);
        if(newKey == null) throw new KeyCreationFailedException("The key was created, but then disappeared suddenly.");
      }
      catch(Exception ex)
      {
        throw new KeyCreationFailedException("The key was created, but the subkey expiration date could not be set.",
                                             ex);
      }
    }

    return newKey;
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/DeleteKeys/*"/>
  public override void DeleteKeys(PrimaryKey[] keys, KeyDeletion deletion)
  {
    if(keys == null) throw new ArgumentNullException();

    string args = GetKeyringArgs(keys, deletion == KeyDeletion.PublicAndSecret);
    args += (deletion == KeyDeletion.Secret ? "--delete-secret-key " : "--delete-secret-and-public-key ") +
            GetFingerprintArgs(keys);

    Command cmd = Execute(args, StatusMessages.ReadInBackground, true);
    CommandState state = new CommandState(cmd);
    using(cmd)
    {
      cmd.StandardErrorLine += delegate(string line) { DefaultStandardErrorHandler(line, state); };
      cmd.StatusMessageReceived += delegate(StatusMessage msg) { DefaultStatusMessageHandler(msg, state); };
      cmd.InputNeeded += delegate(string promptId)
      {
        if(string.Equals(promptId, "delete_key.okay", StringComparison.Ordinal) ||
           string.Equals(promptId, "delete_key.secret.okay", StringComparison.Ordinal))
        {
          cmd.SendLine("Y");
        }
        else DefaultPromptHandler(promptId, state);
      };

      cmd.Start();
      cmd.WaitForExit();
    }

    if(!cmd.SuccessfulExit) throw new KeyEditFailedException("Deleting keys failed.", state.FailureReasons);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/DeleteSubkeys/*" />
  public override void DeleteSubkeys(params Subkey[] subkeys)
  {
    EditSubkeys(subkeys, delegate { return new DeleteSubkeysCommand(); });
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/DisableKeys/*" />
  public override void DisableKeys(params PrimaryKey[] keys)
  {
    RepeatedRawEditCommand(keys, "disable");
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/EnableKeys/*" />
  public override void EnableKeys(params PrimaryKey[] keys)
  {
    RepeatedRawEditCommand(keys, "enable");
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/MinimizeKeys/*" />
  public override void MinimizeKeys(params PrimaryKey[] keys)
  {
    RepeatedRawEditCommand(keys, "minimize");
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/SetOwnerTrust/*" />
  public override void SetOwnerTrust(TrustLevel trust, params PrimaryKey[] keys)
  {
    EditKeys(keys, delegate { return new SetTrustCommand(trust); });
  }
  #endregion

  #region User ID management
  /// <include file="documentation.xml" path="/Security/PGPSystem/AddPhoto4/*" />
  public override void AddPhoto(PrimaryKey key, Stream image, OpenPGPImageType imageFormat,
                                UserPreferences preferences)
  {
    if(key == null || image == null) throw new ArgumentNullException();

    if(imageFormat != OpenPGPImageType.Jpeg)
    {
      throw new NotImplementedException("Only JPEG photos are currently supported.");
    }

    // GPG requires an image filename, so save the image to a temporary file first
    string filename = Path.GetTempFileName();
    try
    {
      using(FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Write))
      {
        image.CopyTo(file);
      }
      DoEdit(key, new AddPhotoCommand(filename, preferences));
    }
    finally { File.Delete(filename); }
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/AddUserId/*" />
  public override void AddUserId(PrimaryKey key, string realName, string email, string comment,
                                 UserPreferences preferences)
  {
    realName = Trim(realName);
    email    = Trim(email);
    comment  = Trim(comment);

    if(string.IsNullOrEmpty(realName) && string.IsNullOrEmpty(email))
    {
      throw new ArgumentException("At least one of the real name or email must be set.");
    }

    // GPG normally imposes strict requirements for the user ID, but we want to be free! freeform, that is.
    DoEdit(key, "--allow-freeform-uid", true, new AddUidCommand(realName, email, comment, preferences));
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/DeleteAttributes/*" />
  public override void DeleteAttributes(params UserAttribute[] attributes)
  {
    EditAttributes(attributes, delegate { return new DeleteUidCommand(); });
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetPreferences/*" />
  public override UserPreferences GetPreferences(UserAttribute user)
  {
    if(user == null) throw new ArgumentNullException();
    if(user.PrimaryKey == null) throw new ArgumentException("The user attribute must be associated with a key.");

    // TODO: currently, this fails to retrieve the user's preferred keyserver, because GPG writes it to the TTY where
    // it can't be captured...

    UserPreferences preferences = new UserPreferences(); // this will be filled out by the GetPrefs class
    DoEdit(user.PrimaryKey, new RawCommand("uid " + user.Id), new GetPrefsCommand(preferences));
    return preferences;
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/RevokeAttributes/*" />
  public override void RevokeAttributes(UserRevocationReason reason, params UserAttribute[] attributes)
  {
    EditAttributes(attributes, delegate { return new RevokeUidCommand(reason); });
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/SetPreferences/*" />
  public override void SetPreferences(UserAttribute user, UserPreferences preferences)
  {
    if(user == null || preferences == null) throw new ArgumentNullException();
    if(user.PrimaryKey == null) throw new ArgumentException("The user attribute must be associated with a key.");
    DoEdit(user.PrimaryKey, new RawCommand("uid " + user.Id), new SetPrefsCommand(preferences));
  }
  #endregion

  /// <summary>Initializes a new <see cref="ExeGPG"/> object with path the path to the GPG executable. It is assumed
  /// that the executable file will not be altered during the lifetime of this object.
  /// </summary>
  public void Initialize(string exePath)
  {
    // do some basic checks on the executable path
    FileInfo info;
    try { info = new FileInfo(exePath); }
    catch(Exception ex) { throw new ArgumentException("The executable path is not valid.", ex); }

    if(!info.Exists) throw new FileNotFoundException();

    // it exists, so try to execute it and check the version information
    Process process;
    try { process = Execute(exePath, "--version"); }
    catch(Exception ex) { throw new ArgumentException("The file could not be executed.", ex); }

    process.StandardInput.Close(); // GPG should not expect any input

    // reset exePath here so we don't end up in a state where the supported algorithms have changed but not the exePath
    this.exePath = null;
    ciphers = hashes = keyTypes = compressions = null;

    while(true) // read the lists of supported algorithms
    {
      string line = process.StandardOutput.ReadLine();
      if(line == null) break;

      Match match = supportLineRe.Match(line);
      if(match.Success)
      {
        string key = match.Groups[1].Value.ToLowerInvariant();
        string[] list = commaSepRe.Split(match.Groups[2].Value.ToUpperInvariant());
        if(string.Equals(key, "pubkey", StringComparison.Ordinal))
        {
          // trim the key types, changing RSA-S and RSA-E into RSA, and ELG-E into ELG
          List<string> trimmedKeyTypes = new List<string>();
          for(int i=0; i<list.Length; i++)
          {
            if(string.Equals(list[i], "ELG-E", StringComparison.OrdinalIgnoreCase))
            {
              if(!trimmedKeyTypes.Contains("ELG")) trimmedKeyTypes.Add("ELG");
            }
            else if(string.Equals(list[i], "RSA-E", StringComparison.OrdinalIgnoreCase))
            {
              if(!trimmedKeyTypes.Contains("RSA")) trimmedKeyTypes.Add("RSA");
            }
            else if(string.Equals(list[i], "RSA-S", StringComparison.OrdinalIgnoreCase))
            {
              if(!trimmedKeyTypes.Contains("RSA")) trimmedKeyTypes.Add("RSA");
            }
            else trimmedKeyTypes.Add(list[i]);
          }
          keyTypes = trimmedKeyTypes.ToArray();
        }
        else if(string.Equals(key, "cipher", StringComparison.Ordinal)) ciphers = list;
        else if(string.Equals(key, "hash", StringComparison.Ordinal)) hashes = list;
        else if(string.Equals(key, "compression", StringComparison.Ordinal)) compressions = list;
      }
      else
      {
        match = versionLineRe.Match(line);
        if(match.Success)
        {
          string[] bits = match.Groups[1].Value.Split('.');
          gpgVersion = int.Parse(bits[0]) * 10000; // 1.4.9 becomes 10409, and 2.0.21 becomes 20021
          if(bits.Length > 1) gpgVersion += int.Parse(bits[1]) * 100;
          if(bits.Length > 2) gpgVersion += int.Parse(bits[2]);
        }
      }
    }

    if(Exit(process) != 0) throw new PGPException("GPG returned an error while running --version.");

    this.exePath = info.FullName; // everything seems okay, so set the full exePath
  }

  /// <summary>Processes status messages from GPG.</summary>
  delegate void StatusMessageHandler(StatusMessage message);

  /// <summary>Creates an edit command on demand.</summary>
  delegate EditCommand EditCommandCreator();

  /// <summary>Creates an edit command on demand to operate on the given key signatures.</summary>
  delegate EditCommand KeySignatureEditCommandCreator(KeySignature[] sigs);

  enum StatusMessages
  {
    Ignore, MixIntoStdout, ReadInBackground
  }

  #region CommandState
  /// <summary>Holds variables set by the default STDERR and status message handlers.</summary>
  sealed class CommandState
  {
    public CommandState(Command command)
    {
      if(command == null) throw new ArgumentNullException();
      this.Command = command;
    }

    /// <summary>Gets whether the operation may have been canceled by the user. (The user clicked a cancel button or
    /// something, but that's not necessarily the cause for failure, and the command may still have succeeded.)
    /// </summary>
    public bool Canceled
    {
      get { return (FailureReasons & FailureReason.OperationCanceled) != 0; }
    }

    /// <summary>The command being executed.</summary>
    public readonly Command Command;
    /// <summary>If not null, this password will be sent when a key password is requested.</summary>
    public SecureString DefaultPassword;
    /// <summary>The status message that informed us of the most recent password request.</summary>
    public StatusMessage PasswordMessage;
    /// <summary>The hint for the next password to be requested.</summary>
    public string PasswordHint;
    /// <summary>Some potential causes of a failure.</summary>
    public FailureReason FailureReasons;
  }
  #endregion

  #region Command
  /// <summary>Represents a GPG command.</summary>
  sealed class Command : System.Runtime.ConstrainedExecution.CriticalFinalizerObject, IDisposable
  {
    public Command(ExeGPG gpg, ProcessStartInfo psi, InheritablePipe commandPipe,
                   StatusMessages statusHandling, bool closeStdInput)
    {
      if(gpg == null || psi == null) throw new ArgumentNullException();
      this.gpg            = gpg;
      this.psi            = psi;
      this.commandPipe    = commandPipe;
      this.statusHandling = statusHandling;
      this.closeStdInput  = closeStdInput;
    }

    ~Command() { Dispose(true); }

    /// <summary>Called for each line of text from STDERR when using <see cref="StreamHandling.ProcessText"/>.</summary>
    public event TextLineHandler StandardErrorLine;
    /// <summary>Called for each input prompt, with the prompt ID.</summary>
    public event TextLineHandler InputNeeded;
    /// <summary>Called for each status message sent on the status pipe.</summary>
    public event StatusMessageHandler StatusMessageReceived;

    /// <summary>Gets the exit code of the process, or throws an exception if the process has not yet exited.</summary>
    public int ExitCode
    {
      get
      {
        if(process == null || !process.HasExited)
        {
          throw new InvalidOperationException("The process has not yet exited.");
        }
        return process.ExitCode;
      }
    }

    /// <summary>Gets the <see cref="ExeGPG"/> object that created this command.</summary>
    public ExeGPG GPG
    {
      get { return gpg; }
    }

    /// <summary>Returns true if the process has exited and the remaining data has been read from all streams.</summary>
    public bool IsDone
    {
      get { return statusDone && errorDone && process.HasExited; }
    }

    /// <summary>Gets the GPG process, or throws an exception if it has not been started yet.</summary>
    public Process Process
    {
      get
      {
        if(process == null) throw new InvalidOperationException("The process has not started yet.");
        return process;
      }
    }

    /// <summary>Gets the status message from the most recent line, or null if the most recent line didn't contain a
    /// status message.
    /// </summary>
    public StatusMessage StatusMessage
    {
      get { return statusMessage; }
    }

    /// <summary>Returns true if GPG exited successfully (with a return code of 0 [success] or 1 [warning]).</summary>
    public bool SuccessfulExit
    {
      get { return ExitCode == 0 || ExitCode == 1; }
    }

    /// <summary>Throws an exception if <see cref="SuccessfulExit"/> is false.</summary>
    public void CheckExitCode()
    {
      if(!SuccessfulExit)
      {
        throw new PGPException("GPG returned failure code "+ExitCode.ToInvariantString());
      }
    }

    /// <summary>Exits the process if it's running and frees system resources used by the <see cref="Command"/> object.</summary>
    public void Dispose()
    {
      GC.SuppressFinalize(this);
      Dispose(false);
    }

    /// <summary>Kills the process if it's running.</summary>
    public void Kill()
    {
      if(process != null && !process.HasExited)
      {
        try { process.Kill(); }
        catch(InvalidOperationException) { } // if it exited before the Kill(), don't worry about it
      }
    }

    /// <summary>Reads a line from STDOUT. The method will return true if a line was processed, but the line will be
    /// null if it was a status message. (The status message can be handled using <see cref="StatusMessageReceived"/>
    /// or by retrieving it from the <see cref="StatusMessage"/> property.) False will be returned if the end of input
    /// is reached.
    /// </summary>
    public bool ReadLine(out string line)
    {
      // if we don't have status messages mixed in, we can just let the StreamReader handle it
      if(statusHandling != StatusMessages.MixIntoStdout)
      {
        line = process.StandardOutput.ReadLine();
        return line != null;
      }

      bool tryAgain;

      do
      {
        statusMessage = null;  // assume this line isn't a status message
        tryAgain      = false; // and that it'll be something we want to return

        // if we don't know where the next EOL is, we need to read some more data
        while(nextOutEOL == -1)
        {
          if(outBytes == outBuffer.Length) // if we need to make more room in the buffer...
          {
            if(outStart != 0) // first try shifting the data over if we can
            {
              Array.Copy(outBuffer, outStart, outBuffer, 0, outBytes - outStart);
              outBytes -= outStart;
              outStart  = 0;
            }
            else // otherwise, enlarge the buffer
            {
              byte[] newBuffer = new byte[outBytes*2];
              Array.Copy(outBuffer, newBuffer, outBytes);
              outBuffer = newBuffer;
            }
          }

          // now try reading some more data
          int bytesRead = process.StandardOutput.BaseStream.Read(outBuffer, outBytes, outBuffer.Length - outBytes);

          if(bytesRead == 0) // we hit the EOF
          {
            if(outBytes - outStart != 0) // there's a bit of data left in the buffer, so use it as the last line
            {
              nextOutEOL = outBytes;
            }
            else if(partialLineLength != 0) // the buffer is empty, but there's a partial line left over. use that.
            {
              line = Encoding.UTF8.GetString(partialLine, 0, partialLineLength);
              partialLineLength = 0;
              return true;
            }
            else // there's no data left anywhere, so return null
            {
              line = null;
              return false;
            }
          }
          else // we got new data in the buffer, so see if there's an EOL character in it
          {
            nextOutEOL = Array.IndexOf(outBuffer, (byte)'\n', outBytes, bytesRead);
            outBytes += bytesRead;
          }
        }

        // at this point, we've found the EOL. we'll peek behind it and see if it's a CRLF combination.
        // we'll actually use a loop because sometimes GPG sends CR CR LF...
        int lineEnd = nextOutEOL;
        while(lineEnd != 0 && outBuffer[lineEnd-1] == '\r') lineEnd--;

        int lineLength = lineEnd - outStart;

        // now we know what data is in the next line. we need to search it for a "[GNUPG:] " token
        int statusMsgIndex = lineLength < 9 ? -1 : Array.IndexOf(outBuffer, (byte)'[', outStart, lineLength - 8);

        if(statusMsgIndex != -1)
        {
          if(outBuffer[statusMsgIndex+1] != (byte)'G' || outBuffer[statusMsgIndex+2] != (byte)'N' ||
             outBuffer[statusMsgIndex+3] != (byte)'U' || outBuffer[statusMsgIndex+4] != (byte)'P' ||
             outBuffer[statusMsgIndex+5] != (byte)'G' || outBuffer[statusMsgIndex+6] != (byte)':' ||
             outBuffer[statusMsgIndex+7] != (byte)']' || outBuffer[statusMsgIndex+8] != (byte)' ')
          {
            statusMsgIndex = -1;
          }
        }

        // if there's no status message in this line, then return the line as-is
        if(statusMsgIndex == -1)
        {
          if(partialLineLength == 0) // there's no partial line from a previous call
          {
            line = Encoding.UTF8.GetString(outBuffer, outStart, lineLength);
          }
          else // if there's a partial line from a previous call, prepend it to the new line
          {
            byte[] newLine = new byte[lineLength + partialLineLength];
            if(partialLineLength != 0) Array.Copy(partialLine, newLine, partialLineLength);
            Array.Copy(outBuffer, outStart, newLine, partialLineLength, lineLength);
            line = Encoding.UTF8.GetString(newLine);
            partialLineLength = 0;
          }
        }
        else // there's a status message in the line somewhere
        {
          if(statusMsgIndex != outStart) // it's embedded in the line
          {
            // append the non-status portion to the partial line
            if(partialLine == null || partialLine.Length < partialLineLength + lineLength)
            {
              byte[] newPartialLine = new byte[partialLineLength + lineLength + 64];
              if(partialLineLength != 0) Array.Copy(partialLine, newPartialLine, partialLineLength);
              partialLine = newPartialLine;
            }
            Array.Copy(outBuffer, outStart, partialLine, partialLineLength, statusMsgIndex - outStart);
            partialLineLength += statusMsgIndex - outStart;
          }

          lineLength = Decode(outBuffer, statusMsgIndex, lineEnd - statusMsgIndex);
          // GPG sends status lines in UTF-8 which has been further encoded so that certain characters become %XX.
          // decode the line and split it into arguments
          string type;
          string[] arguments;
          SplitDecodedLine(outBuffer, statusMsgIndex, lineLength, out type, out arguments);

          if(type != null) // if the line decoded properly, parse and handle the message
          {
            statusMessage = ParseStatusMessage(type, arguments);
            if(statusMessage != null) OnStatusMessage(statusMessage);
          }

          tryAgain = statusMessage == null; // if the status message was not used, go to the next line
          line = null; // no line is returned if a status message was handled
        }

        // search for the next EOL so we're ready for the next call
        outStart   = nextOutEOL+1;
        nextOutEOL = Array.IndexOf(outBuffer, (byte)'\n', outStart, outBytes - outStart);
      } while(tryAgain);

      return true;
    }

    /// <summary>Sends a blank line on the command stream.</summary>
    public void SendLine()
    {
      SendLine(null);
    }

    /// <summary>Sends the given line on the command stream. The line should not include any end-of-line characters.</summary>
    public void SendLine(string line)
    {
      if(commandStream == null) throw new InvalidOperationException("The command stream is not open.");

      if(gpg.LoggingEnabled) gpg.LogLine(">> " + line);

      if(!string.IsNullOrEmpty(line))
      {
        byte[] bytes = Encoding.UTF8.GetBytes(line);
        commandStream.Write(bytes, 0, bytes.Length);
      }
      commandStream.WriteByte((byte)'\n');
      commandStream.Flush();
    }

    /// <summary>Sends the given password on the command stream. If <paramref name="ownsPassword"/> is true, the
    /// password will be disposed.
    /// </summary>
    public void SendPassword(SecureString password, bool ownsPassword)
    {
      if(password == null)
      {
        SendLine();
      }
      else
      {
        SecurityUtility.ProcessSecureString(password,
          delegate(byte[] bytes) { commandStream.Write(bytes, 0, bytes.Length); });
        commandStream.WriteByte((byte)'\n'); // the password must be EOL-terminated for GPG to accept it
        commandStream.Flush();
      }
    }

    /// <summary>Starts executing the command.</summary>
    public void Start()
    {
      if(process != null) throw new InvalidOperationException("The process has already been started.");

      if(gpg.LoggingEnabled) gpg.LogLine(EscapeArg(psi.FileName) + " " + psi.Arguments);

      process = Process.Start(psi);

      if(closeStdInput) process.StandardInput.Close();

      // if we have a command pipe, set up a stream for it
      if(commandPipe != null)
      {
        commandStream = new FileStream(new SafeFileHandle(commandPipe.ServerHandle, false),
                          statusHandling == StatusMessages.ReadInBackground ? FileAccess.ReadWrite : FileAccess.Write);
      }

      outBuffer = new byte[4096];
      if(statusHandling == StatusMessages.ReadInBackground) OnStatusRead(null);
      else statusDone = true;

      errorBuffer = new byte[4096];
      OnStdErrorRead(null); // start reading STDERR on a background thread
    }

    /// <summary>Waits for the process to exit and all data to be read.</summary>
    public void WaitForExit()
    {
      Process.WaitForExit(); // first wait for the process to finish

      bool closedPipe = commandPipe == null;
      while(!IsDone) // then wait for all of the streams to finish being read
      {
        if(!closedPipe) // if GPG didn't close its end of the pipe, we may have to do it
        {
          commandPipe.CloseClient();
          closedPipe = true; // but don't close it on every iteration
        }

        System.Threading.Thread.Sleep(0); // give other threads (ie, the stream reading threads) a chance to finish
      }
    }

    delegate void LineProcessor(string line);

    enum StreamHandling
    {
      ProcessText, ProcessStatus, DumpBinary
    }

    void Dispose(bool finalizing)
    {
      if(!disposed)
      {
        Utility.Dispose(ref commandStream);
        Utility.Dispose(ref commandPipe);

        if(process != null) Exit(process);

        // wipe the read buffers. it's unlikely that they contains sensitive data, but just in case...
        SecurityUtility.ZeroBuffer(outBuffer);
        SecurityUtility.ZeroBuffer(errorBuffer);
        SecurityUtility.ZeroBuffer(partialLine);
        statusMessage = null;

        statusDone = errorDone = disposed = true; // mark reads complete so IsDone will return true
      }
    }

    /// <summary>Handles an asynchronous read completion on a stream.</summary>
    void HandleStream(StreamHandling handling, Stream stream, IAsyncResult result, ref byte[] buffer,
                      ref int bufferBytes, ref bool bufferDone, LineProcessor processor, AsyncCallback callback)
    {
      if(stream == null) // if the stream was destroyed already, then just mark that the stream is done
      {
        bufferDone = true;
      }
      else // otherwise, the stream is still going, so we can look at the data that was read
      {
        if(result != null) // if there was any data read, process it according to the stream handling
        {
          if(handling == StreamHandling.ProcessText)
          {
            foreach(string line in ProcessUnicodeStream(result, stream, ref buffer, ref bufferBytes, ref bufferDone))
            {
              processor(line);
            }
          }
          else if(handling == StreamHandling.ProcessStatus)
          {
            ProcessStatusStream(result, stream, handling, ref buffer, ref bufferBytes, ref bufferDone);
          }
          else
          {
            DumpBinaryStream(result, stream, ref bufferDone);
          }
        }

        // a possible race condition with WaitForExit() (or Dispose()) may have disposed the stream, so check CanRead
        // to prevent an ObjectDisposedException if possible
        // TODO: eliminate this race condition if possible
        if(!stream.CanRead)
        {
          bufferDone = true;
        }
        else // if we can still read from the stream, start another asynchronous read
        {
          try { stream.BeginRead(buffer, bufferBytes, buffer.Length - bufferBytes, callback, null); }
          catch(ObjectDisposedException) { bufferDone = true; } // if the stream was disposed, mark it as done
        }
      }
    }

    /// <summary>Handles a line of text from STDOUT.</summary>
    void OnStdOutLine(string line)
    {
      if(gpg.LoggingEnabled) gpg.LogLine("OUT: " + line);
    }

    /// <summary>Handles a line of text from STDERR.</summary>
    void OnStdErrorLine(string line)
    {
      if(gpg.LoggingEnabled) gpg.LogLine("ERR: " + line);
      if(StandardErrorLine != null) StandardErrorLine(line);
    }

    /// <summary>Handles a status message.</summary>
    void OnStatusMessage(StatusMessage message)
    {
      GetInputMessage inputMsg = message as GetInputMessage;
      if(inputMsg != null && InputNeeded != null)
      {
        InputNeeded(inputMsg.PromptId);
        return; // input messages are not given to the status message handler unless there's no prompt handler
      }

      if(StatusMessageReceived != null) StatusMessageReceived(message);
    }

    // warning 0420 is "reference to a volatile field will not be treated as volatile". we aren't worried about this
    // because the field is only written to by the callee, not read.
    #pragma warning disable 420
    void OnStdErrorRead(IAsyncResult result)
    {
      HandleStream(StreamHandling.ProcessText, process.StandardError.BaseStream, result, ref errorBuffer, ref errorBytes,
                   ref errorDone, OnStdErrorLine, OnStdErrorRead);
    }

    void OnStatusRead(IAsyncResult result)
    {
      HandleStream(StreamHandling.ProcessStatus, commandStream, result, ref outBuffer, ref outBytes,
                   ref statusDone, null, OnStatusRead);
    }
    #pragma warning restore 420

    /// <summary>Parses a status message with the given type and arguments, and returns the corresponding
    /// <see cref="StatusMessage"/>, or null if the message could not be parsed or was ignored.
    /// </summary>
    StatusMessage ParseStatusMessage(string type, string[] arguments)
    {
      StatusMessage message;
      switch(type)
      {
        case "NEWSIG": message = new GenericMessage(StatusMessageType.NewSig); break;
        case "GOODSIG": message = new GoodSigMessage(arguments); break;
        case "EXPSIG": message = new ExpiredSigMessage(arguments); break;
        case "EXPKEYSIG": message = new ExpiredKeySigMessage(arguments); break;
        case "REVKEYSIG": message = new RevokedKeySigMessage(arguments); break;
        case "BADSIG": message = new BadSigMessage(arguments); break;
        case "ERRSIG": message = new ErrorSigMessage(arguments); break;
        case "VALIDSIG": message = new ValidSigMessage(arguments); break;

        case "IMPORTED": message = new KeySigImportedMessage(arguments); break;
        case "IMPORT_OK": message = new KeyImportOkayMessage(arguments); break;
        case "IMPORT_PROBLEM": message = new KeyImportFailedMessage(arguments); break;
        case "IMPORT_RES": message = new KeyImportResultsMessage(arguments); break;

        case "USERID_HINT": message = new UserIdHintMessage(arguments); break;
        case "NEED_PASSPHRASE": message = new NeedKeyPassphraseMessage(arguments); break;
        case "GOOD_PASSPHRASE": message = new GenericMessage(StatusMessageType.GoodPassphrase); break;
        case "MISSING_PASSPHRASE": message = new GenericMessage(StatusMessageType.MissingPassphrase); break;
        case "BAD_PASSPHRASE": message = new BadPassphraseMessage(arguments); break;
        case "NEED_PASSPHRASE_SYM": message = new GenericMessage(StatusMessageType.NeedCipherPassphrase); break;

        case "BEGIN_SIGNING": message = new GenericMessage(StatusMessageType.BeginSigning); break;
        case "SIG_CREATED": message = new GenericMessage(StatusMessageType.SigCreated); break;

        case "BEGIN_DECRYPTION": message = new GenericMessage(StatusMessageType.BeginDecryption); break;
        case "END_DECRYPTION": message = new GenericMessage(StatusMessageType.EndDecryption); break;
        case "ENC_TO": message = new GenericKeyIdMessage(StatusMessageType.EncTo, arguments); break;
        case "DECRYPTION_OKAY": message = new GenericMessage(StatusMessageType.DecryptionOkay); break;
        case "DECRYPTION_FAILED": message = new GenericMessage(StatusMessageType.DecryptionFailed); break;

        case "BEGIN_ENCRYPTION": message = new GenericMessage(StatusMessageType.BeginEncryption); break;
        case "END_ENCRYPTION": message = new GenericMessage(StatusMessageType.EndEncryption); break;

        case "INV_RECP": message = new InvalidRecipientMessage(arguments); break;
        case "NODATA": message = new GenericMessage(StatusMessageType.NoData); break;
        case "NO_PUBKEY": message = new GenericKeyIdMessage(StatusMessageType.NoPublicKey, arguments); break;
        case "NO_SECKEY": message = new GenericKeyIdMessage(StatusMessageType.NoSecretKey, arguments); break;
        case "UNEXPECTED": message = new GenericMessage(StatusMessageType.UnexpectedData); break;

        case "TRUST_UNDEFINED": message = new TrustLevelMessage(StatusMessageType.TrustUndefined); break;
        case "TRUST_NEVER": message = new TrustLevelMessage(StatusMessageType.TrustNever); break;
        case "TRUST_MARGINAL": message = new TrustLevelMessage(StatusMessageType.TrustMarginal); break;
        case "TRUST_FULLY": message = new TrustLevelMessage(StatusMessageType.TrustFully); break;
        case "TRUST_ULTIMATE": message = new TrustLevelMessage(StatusMessageType.TrustUltimate); break;

        case "ATTRIBUTE": message = new AttributeMessage(arguments); break;

        case "GET_HIDDEN": message = new GetInputMessage(StatusMessageType.GetHidden, arguments); break;
        case "GET_BOOL": message = new GetInputMessage(StatusMessageType.GetBool, arguments); break;
        case "GET_LINE": message = new GetInputMessage(StatusMessageType.GetLine, arguments); break;

        case "DELETE_PROBLEM": message = new DeleteFailedMessage(arguments); break;

        case "KEY_CREATED": message = new KeyCreatedMessage(arguments); break;
        case "KEY_NOT_CREATED": message = new GenericMessage(StatusMessageType.KeyNotCreated); break;

        // ignore these messages
        case "PROGRESS": case "PLAINTEXT": case "PLAINTEXT_LENGTH": case "SIG_ID": case "GOT_IT": case "GOODMDC":
        case "KEYEXPIRED": case "SIGEXPIRED":
          message = null;
          break;

        default:
          if(gpg.LoggingEnabled) gpg.LogLine("Unprocessed status message: "+type);
          message = null;
          break;
      }
      return message;
    }

    void ProcessStatusStream(IAsyncResult result, Stream stream, StreamHandling handling,
                             ref byte[] buffer, ref int bufferBytes, ref bool bufferDone)
    {
      foreach(byte[] binaryLine in ProcessAsciiStream(result, stream, ref buffer, ref bufferBytes, ref bufferDone))
      {
        // GPG sends lines in UTF-8, which has been further encoded, so that certain characters become %XX. decode the
        // line and split it into arguments
        string type;
        string[] arguments;
        SplitDecodedLine(binaryLine, 0, Decode(binaryLine, 0, binaryLine.Length), out type, out arguments);

        if(type != null) // if the line decoded properly and has a message type, parse and handle the message
        {
          StatusMessage message = ParseStatusMessage(type, arguments);
          if(message != null) OnStatusMessage(message);
        }
      }
    }

    /// <summary>Splits a decoded ASCII line representing a status message into a message type and message arguments.</summary>
    void SplitDecodedLine(byte[] line, int index, int count, out string type, out string[] arguments)
    {
      if(gpg.LoggingEnabled) gpg.LogLine(Encoding.ASCII.GetString(line, index, count));

      List<string> chunks = new List<string>();
      type      = null;
      arguments = null;

      // the chunks are whitespace-separated
      for(int end=index+count; ;)
      {
        while(index < end && line[index] == (byte)' ') index++; // find the next non-whitespace character
        int start = index;
        while(index < end && line[index] != (byte)' ') index++; // find the next whitespace character after that

        if(start == end) break; // if we're at the end of the line, we're done

        chunks.Add(Encoding.UTF8.GetString(line, start, index-start)); // grab the text between the two
      }

      if(chunks.Count >= 2) // if there are enough chunks to make up a status line
      {
        type      = chunks[1]; // skip the first chunk, which is assumed to be "[GNUPG:]". the second becomes the type
        arguments = new string[chunks.Count-2]; // grab the rest as the arguments
        chunks.CopyTo(2, arguments, 0, arguments.Length);
      }
    }

    Process process;
    InheritablePipe commandPipe;
    FileStream commandStream;
    byte[] errorBuffer, outBuffer, partialLine;
    ProcessStartInfo psi;
    ExeGPG gpg;
    StatusMessage statusMessage;
    int errorBytes, outStart, outBytes, partialLineLength, nextOutEOL = -1;
    StatusMessages statusHandling;
    volatile bool errorDone, statusDone;
    bool closeStdInput, disposed;

    /// <summary>Decodes %XX-encoded values in ASCII text (represented as a byte array).</summary>
    /// <returns>Returns the new length of the text (the text is decoded in place, and can get shorter).</returns>
    static int Decode(byte[] encoded, int index, int count)
    {
      index = Array.IndexOf(encoded, (byte)'%', index, count);

      if(index != -1)
      {
        for(int offset=0; index < count; index++)
        {
          byte c = encoded[index + offset];
          if(c == (byte)'%' && index < count-2)
          {
            char high = (char)encoded[index + offset+1], low = (char)encoded[index + offset+2];
            if(IsHexDigit(high) && IsHexDigit(low))
            {
              encoded[index] = (byte)GetHexValue(high, low); // convert the hex value to the new byte value
              offset += 2;
              count  -= 2;
            }
          }
          else encoded[index] = encoded[index + offset];
        }
      }

      return count;
    }

    /// <summary>Handles an asynchronous read completion by throwing away the data that was read.</summary>
    static void DumpBinaryStream(IAsyncResult result, Stream stream, ref bool bufferDone)
    {
      int bytesRead = 0;
      if(stream != null)
      {
        try { bytesRead = stream.EndRead(result); }
        catch(ObjectDisposedException) { }
      }
      // if the stream was null, EndRead() returned zero, or ObjectDisposedException was thrown, the stream is done
      if(bytesRead == 0) bufferDone = true;
    }

    /// <summary>Processes data read in an ASCII stream and returns completed lines as arrays of bytes.</summary>
    static IEnumerable<byte[]> ProcessAsciiStream(IAsyncResult result, Stream stream,
                                                  ref byte[] buffer, ref int bufferBytes, ref bool bufferDone)
    {
      List<byte[]> lines = new List<byte[]>();
      if(result != null)
      {
        int bytesRead = 0;
        if(stream != null)
        {
          try { bytesRead = stream.EndRead(result); }
          catch(ObjectDisposedException) { }
        }

        if(bytesRead == 0) // if the stream was null, or EndRead() returned zero, or ObjectDisposedException was
        {                  // thrown, the stream is done
          bufferDone = true;

          if(bufferBytes != 0) // if data is still in the buffer, return it as the final line
          {
            byte[] line = new byte[bufferBytes];
            Array.Copy(buffer, line, bufferBytes);
            lines.Add(line);
            bufferBytes = 0;
          }
        }
        else // otherwise, data was read, so scan the new data for line endings
        {
          int index, searchStart = bufferBytes, newBufferStart = 0;
          bufferBytes += bytesRead;

          do
          {
            index = Array.IndexOf(buffer, (byte)'\n', searchStart, bufferBytes-searchStart);
            if(index == -1) break;

            // we found a line ending in the new data. we won't return the line ending, so we'll skip either 1 or 2
            // bytes depending on whether the ending is LF or CRLF
            int eolLength = 1;
            if(index != 0 && buffer[index-1] == (byte)'\r')
            {
              index--;
              eolLength++;
            }

            // grab the portion of the buffer corresponding to the line
            byte[] line = new byte[index-newBufferStart];
            Array.Copy(buffer, newBufferStart, line, 0, line.Length);
            lines.Add(line);

            // mark the returned portion of the buffer as unused
            newBufferStart = searchStart = index+eolLength;
          } while(bufferBytes != searchStart);

          if(newBufferStart != 0) // if any portion of the buffer became unused, shift the remaining data to the front
          {
            bufferBytes -= newBufferStart;
            if(bufferBytes != 0) Array.Copy(buffer, newBufferStart, buffer, 0, bufferBytes);
          }
        }
      }

      if(bufferBytes == buffer.Length) // if the buffer is full, enlarge it so we can read more data
      {
        byte[] newBuffer = new byte[buffer.Length*2];
        Array.Copy(buffer, newBuffer, bufferBytes);
        buffer = newBuffer;
      }

      return lines;
    }

    /// <summary>Processes data read in a UTF-8 stream.</summary>
    static IEnumerable<string> ProcessUnicodeStream(IAsyncResult result, Stream stream,
                                                    ref byte[] buffer, ref int bufferBytes, ref bool bufferDone)
    {
      List<string> lines = new List<string>();
      foreach(byte[] binaryLine in ProcessAsciiStream(result, stream, ref buffer, ref bufferBytes, ref bufferDone))
      {
        lines.Add(Encoding.UTF8.GetString(binaryLine));
      }
      return lines;
    }
  }
  #endregion

  #region DummyAttribute
  /// <summary>A placeholder for a user attribute that has not been fully retrieved in the key listing.</summary>
  sealed class DummyAttribute : UserAttribute
  {
    public DummyAttribute(AttributeMessage msg)
    {
      Message = msg;
    }

    public readonly AttributeMessage Message;
  }
  #endregion

  #region Edit commands and objects
  #region EditCommandResult
  /// <summary>Determines the result of processing during edit mode.</summary>
  enum EditCommandResult
  {
    /// <summary>The event was processed, and this command is finished.</summary>
    Done,
    /// <summary>The event was processed, but this command has more work to do.</summary>
    Continue,
    /// <summary>The event was not processed, and this command is finished.</summary>
    Next
  }
  #endregion

  #region EditUserId
  /// <summary>Represents a user ID or attribute parsed from an edit key listing.</summary>
  sealed class EditUserId
  {
    /// <summary>Determines whether this object is identical to the given <see cref="EditUserId"/>. This doesn't
    /// guarantee that they reference the same user ID, but it's the best we've got.
    /// </summary>
    public bool Matches(EditUserId id)
    {
      return IsAttribute == id.IsAttribute && string.Equals(Name, id.Name, StringComparison.Ordinal) &&
             string.Equals(Prefs, id.Prefs, StringComparison.Ordinal);
    }

    public override string ToString()
    {
      return (IsAttribute ? "Attribute " : null) + Name + " - " + Prefs;
    }

    public string Name, Prefs;
    public bool IsAttribute, Primary, Selected;
  }
  #endregion

  #region EditKey
  /// <summary>Represents the current state of a key being edited.</summary>
  sealed class EditKey
  {
    /// <summary>Returns the first <see cref="EditUserId"/> that is an attribute and is primary, or null if there is
    /// none.
    /// </summary>
    public EditUserId PrimaryAttribute
    {
      get
      {
        foreach(EditUserId userId in UserIds)
        {
          if(userId.IsAttribute && userId.Primary) return userId;
        }
        return null;
      }
    }

    /// <summary>Returns the first <see cref="EditUserId"/> that is a user ID and is primary, or null if there is none.</summary>
    public EditUserId PrimaryUserId
    {
      get
      {
        foreach(EditUserId userId in UserIds)
        {
          if(!userId.IsAttribute && userId.Primary) return userId;
        }
        return null;
      }
    }

    /// <summary>Returns the first <see cref="EditUserId"/> that is selected, or null if there is none.</summary>
    public EditUserId SelectedUserId
    {
      get
      {
        foreach(EditUserId userId in UserIds)
        {
          if(userId.Selected) return userId;
        }
        return null;
      }
    }

    /// <summary>A list of <see cref="EditUserId"/>, in the order in which they were listed.</summary>
    public readonly List<EditUserId> UserIds = new List<EditUserId>();
    /// <summary>A list of the fingerprints of subkeys, in the order in which they were listed.</summary>
    public readonly List<string> Subkeys = new List<string>();
  }
  #endregion

  #region EditCommand
  /// <summary>Represents a command that operates in edit mode.</summary>
  abstract class EditCommand
  {
    /// <summary>Responds to a request for input.</summary>
    public virtual EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                             CommandState state, string promptId)
    {
      if(string.Equals(promptId, "passphrase.enter", StringComparison.Ordinal) && state.PasswordMessage != null &&
         state.PasswordMessage.Type == StatusMessageType.NeedKeyPassphrase)
      {
        if(state.DefaultPassword != null)
        {
          state.Command.SendPassword(state.DefaultPassword, false);
        }
        else if(!state.Command.GPG.SendKeyPassword(state.Command, state.PasswordHint,
                                                   (NeedKeyPassphraseMessage)state.PasswordMessage))
        {
          throw new OperationCanceledException(); // abort if the password was not provided
        }
        return EditCommandResult.Continue;
      }
      else throw new NotImplementedException("Unhandled input request: " + promptId);
    }

    /// <summary>Processes a line of text received from GPG.</summary>
    public virtual EditCommandResult Process(string line)
    {
      return EditCommandResult.Continue;
    }

    /// <summary>Gets or sets whether this command expects a relist before the next prompt. If true, and GPG doesn't
    /// issue a relist, one will be manually requested.
    /// </summary>
    public bool ExpectRelist;

    /// <summary>Returns an exception that represents an unexpected condition.</summary>
    protected static KeyEditFailedException UnexpectedError(string problem)
    {
      return new KeyEditFailedException("Key edit failed: " + problem + ".");
    }
  }
  #endregion

  #region AddUidBase
  /// <summary>A base class for edit commands that add user IDs.</summary>
  abstract class AddUidBase : EditCommand
  {
    /// <param name="preferences">The <see cref="UserPreferences"/> to use, or null to use the defaults.</param>
    /// <param name="addAttribute">True if an attribute is being added, and false if a user ID is being added.</param>
    public AddUidBase(UserPreferences preferences, bool addAttribute)
    {
      this.preferences  = preferences;
      this.addAttribute = addAttribute;
    }

    /// <summary>Enqueues additional commands to set the preferences of the new user ID, which is assumed to be the
    /// last ID in the key.
    /// </summary>
    protected void AddPreferenceCommands(Queue<EditCommand> commands, EditKey originalKey)
    {
      if(preferences != null)
      {
        if(!preferences.Primary)
        {
          EditUserId id = addAttribute ? originalKey.PrimaryAttribute : originalKey.PrimaryUserId;
          if(id != null)
          {
            commands.Enqueue(new SelectUidCommand(id));
            commands.Enqueue(new SetPrimaryCommand());
          }
        }

        commands.Enqueue(new SelectLastUidCommand());
        commands.Enqueue(new SetPrefsCommand(preferences));
      }
    }

    readonly UserPreferences preferences;
    readonly bool addAttribute;
  }
  #endregion

  #region AddPhotoCommand
  /// <summary>An edit command that adds a photo id to a key.</summary>
  sealed class AddPhotoCommand : AddUidBase
  {
    public AddPhotoCommand(string filename, UserPreferences preferences) : base(preferences, true)
    {
      if(string.IsNullOrEmpty(filename)) throw new ArgumentException();
      this.filename = filename;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          state.Command.SendLine("addphoto " + filename);
          sentCommand = true;
        }
        else
        {
          AddPreferenceCommands(commands, originalKey);
          return EditCommandResult.Next;
        }
      }
      else if(string.Equals(promptId, "photoid.jpeg.size", StringComparison.Ordinal))
      {
        state.Command.SendLine("Y"); // yes, it's okay if the photo is large
      }
      else if(string.Equals(promptId, "photoid.jpeg.add", StringComparison.Ordinal))
      {
        // if GPG asks us for the filename, that means it rejected the file we gave originally
        throw UnexpectedError("The image was rejected. Perhaps it's not a valid JPEG?");
      }
      else return base.Process(commands, originalKey, key, state, promptId);

      return EditCommandResult.Continue;
    }

    readonly string filename;
    bool sentCommand;
  }
  #endregion

  #region AddRevokerCommand
  /// <summary>An edit command that adds a designated revoker to a key.</summary>
  sealed class AddRevokerCommand : EditCommand
  {
    /// <param name="fingerprint">The fingerprint of the designated revoker.</param>
    public AddRevokerCommand(string fingerprint)
    {
      this.fingerprint = fingerprint;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          state.Command.SendLine("addrevoker");
          sentCommand = true;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "keyedit.add_revoker", StringComparison.Ordinal))
      {
        if(!sentFingerprint)
        {
          state.Command.SendLine(fingerprint);
          sentFingerprint = true;
        }
        else // if it asks us again, that means it rejected the first fingerprint
        {
          throw UnexpectedError("Adding the designated revoker failed.");
        }
      }
      else if(string.Equals(promptId, "keyedit.add_revoker.okay", StringComparison.Ordinal))
      {
        state.Command.SendLine("Y");
      }
      else return base.Process(commands, originalKey, key, state, promptId);

      return EditCommandResult.Continue;
    }

    readonly string fingerprint;
    bool sentCommand, sentFingerprint;
  }
  #endregion

  #region AddSubkeyCommand
  /// <summary>An edit command that adds a subkey to a primary key.</summary>
  sealed class AddSubkeyCommand : EditCommand
  {
    /// <param name="type">The subkey type.</param>
    /// <param name="capabilities">The desired capabilities of the subkey.</param>
    /// <param name="length">The subkey length.</param>
    /// <param name="expiration">The subkey expiration, or null if it does not expire.</param>
    public AddSubkeyCommand(string type, KeyCapabilities capabilities, int length, DateTime? expiration)
    {
      this.type           = type;
      this.expiration     = expiration;
      this.expirationDays = GetExpirationDays(expiration);

      if((capabilities & KeyCapabilities.Certify) != 0)
      {
        throw new ArgumentException("The Certify capability is only allowed on the primary key.");
      }

      this.isDSA = string.Equals(type, KeyType.DSA, StringComparison.OrdinalIgnoreCase);
      this.isELG = type == null || string.Equals(type, KeyType.ElGamal, StringComparison.OrdinalIgnoreCase);
      this.isRSA = string.Equals(type, KeyType.RSA, StringComparison.OrdinalIgnoreCase);

      if(!isDSA && !isELG && !isRSA)
      {
        throw new KeyCreationFailedException(FailureReason.UnsupportedAlgorithm, "Unsupported subkey type: " + type);
      }

      if(capabilities == KeyCapabilities.Default)
      {
        capabilities = isDSA ? KeyCapabilities.Sign
                             : isELG ? KeyCapabilities.Encrypt : KeyCapabilities.Sign | KeyCapabilities.Encrypt;
      }
      else if(isDSA && (capabilities & KeyCapabilities.Encrypt) != 0)
      {
        throw new KeyCreationFailedException(FailureReason.None, "DSA keys cannot be used for encryption.");
      }
      else if(isELG && capabilities != KeyCapabilities.Encrypt)
      {
        throw new KeyCreationFailedException(FailureReason.None, "GPG only supports encryption-only ElGamal keys.");
      }

      if(length == 0)
      {
        length = isDSA ? 1024 : 2048;
      }
      else
      {
        int maxLength = isDSA ? 3072 : 4096;
        if(length < 0 || length > maxLength)
        {
          throw new KeyCreationFailedException(FailureReason.None, "Key length " +
                                               length.ToInvariantString() + " is not supported.");
        }
      }

      this.length        = length;
      this.capabilities  = capabilities;
      this.flagsToToggle = isELG ? 0 : (isDSA ? DefaultDSACaps : DefaultRSACaps) ^ capabilities;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          state.Command.SendLine("addkey");
          sentCommand = true;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "keygen.algo", StringComparison.Ordinal))
      {
        if(!sentAlgo)
        {
          if(isDSA) state.Command.SendLine(capabilities == KeyCapabilities.Sign ? "2" : "3");
          else if(isELG) state.Command.SendLine("4");
          else
          {
            state.Command.SendLine(capabilities == KeyCapabilities.Sign ?
                                     "5" : capabilities == KeyCapabilities.Encrypt ? "6" : "7");
          }
          sentAlgo = true;
        }
        else // if GPG asks a second time, then it rejected the algorithm choice
        {
          throw new KeyCreationFailedException(FailureReason.UnsupportedAlgorithm, "Unsupported subkey type: " + type);
        }
      }
      else if(string.Equals(promptId, "keygen.size", StringComparison.Ordinal))
      {
        if(!sentLength)
        {
          state.Command.SendLine(length.ToInvariantString());
          sentLength = true;
        }
        else // if GPG asks a second time, then it rejected the key length
        {
          throw new KeyCreationFailedException(FailureReason.None, "Key length " +
                                               length.ToInvariantString() + " is not supported.");
        }
      }
      else if(string.Equals(promptId, "keygen.valid", StringComparison.Ordinal))
      {
        if(!sentExpiration)
        {
          state.Command.SendLine(expirationDays.ToInvariantString());
          sentExpiration = true;
        }
        else // if GPG asks a second time, then it rejected the expiration date
        {
          throw new KeyCreationFailedException(FailureReason.None, "Expiration date " + Convert.ToString(expiration) +
                                               " is not supported.");
        }
      }
      else if(string.Equals(promptId, "keygen.flags", StringComparison.Ordinal))
      {
        if((flagsToToggle & KeyCapabilities.Authenticate) != 0)
        {
          state.Command.SendLine("A");
          flagsToToggle &= ~KeyCapabilities.Authenticate;
        }
        else if((flagsToToggle & KeyCapabilities.Encrypt) != 0)
        {
          state.Command.SendLine("E");
          flagsToToggle &= ~KeyCapabilities.Encrypt;
        }
        else if((flagsToToggle & KeyCapabilities.Sign) != 0)
        {
          state.Command.SendLine("S");
          flagsToToggle &= ~KeyCapabilities.Sign;
        }
        else
        {
          state.Command.SendLine("Q");
        }
      }
      else return base.Process(commands, originalKey, key, state, promptId);

      return EditCommandResult.Continue;
    }

    const KeyCapabilities DefaultRSACaps = KeyCapabilities.Sign | KeyCapabilities.Encrypt;
    const KeyCapabilities DefaultDSACaps = KeyCapabilities.Sign;

    readonly string type;
    readonly DateTime? expiration;
    readonly int length, expirationDays;
    readonly bool isDSA, isELG, isRSA;
    KeyCapabilities capabilities, flagsToToggle;
    bool sentCommand, sentAlgo, sentLength, sentExpiration;
  }
  #endregion

  #region AddUidCommand
  /// <summary>An edit command that adds a new user ID to a key.</summary>
  sealed class AddUidCommand : AddUidBase
  {
    public AddUidCommand(string realName, string email, string comment, UserPreferences preferences)
      : base(preferences, false)
    {
      if(ContainsControlCharacters(realName + email + comment))
      {
        throw new ArgumentException("The name, email, and/or comment contains control characters. Remove them.");
      }

      this.realName    = realName;
      this.email       = email;
      this.comment     = comment;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!startedUid)
        {
          state.Command.SendLine("adduid");
          startedUid = true;
        }
        else // if we didn't get to the "comment" prompt, then it probably failed
        {
          throw UnexpectedError("Adding a new user ID seemed to fail.");
        }
      }
      else if(string.Equals(promptId, "keygen.name", StringComparison.Ordinal)) state.Command.SendLine(realName);
      else if(string.Equals(promptId, "keygen.email", StringComparison.Ordinal)) state.Command.SendLine(email);
      else if(string.Equals(promptId, "keygen.comment", StringComparison.Ordinal))
      {
        state.Command.SendLine(comment);
        AddPreferenceCommands(commands, originalKey);
        return EditCommandResult.Done;
      }
      else return base.Process(commands, originalKey, key, state, promptId);

      return EditCommandResult.Continue;
    }

    readonly string realName, email, comment;
    bool startedUid;
  }
  #endregion

  #region ChangeExpirationCommand
  /// <summary>An edit command that changes the expiration date of the primary key or selected subkey.</summary>
  sealed class ChangeExpirationCommand : EditCommand
  {
    public ChangeExpirationCommand(DateTime? expiration)
    {
      this.expiration     = expiration;
      this.expirationDays = GetExpirationDays(expiration);
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          state.Command.SendLine("expire");
          sentCommand = true;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "keygen.valid", StringComparison.Ordinal))
      {
        if(!sentExpiration)
        {
          state.Command.SendLine(expirationDays.ToInvariantString());
          sentExpiration = true;
        }
        else // if GPG asked us twice, that means it rejected the expiration date
        {
          throw UnexpectedError("Changing expiration date to " + Convert.ToString(expiration) + " failed.");
        }
      }
      else return base.Process(commands, originalKey, key, state, promptId);

      return EditCommandResult.Continue;
    }

    readonly DateTime? expiration;
    readonly int expirationDays;
    bool sentCommand, sentExpiration;
  }
  #endregion

  #region ChangePasswordCommand
  /// <summary>An edit command that changes the password on a secret key.</summary>
  sealed class ChangePasswordCommand : EditCommand
  {
    public ChangePasswordCommand(SecureString password)
    {
      this.password = password;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          state.Command.SendLine("passwd");
          sentCommand = true;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "change_passwd.empty.okay", StringComparison.Ordinal))
      {
        state.Command.SendLine("Y"); // yes, an empty password is okay
      }
      else if(string.Equals(promptId, "passphrase.enter", StringComparison.Ordinal) && state.PasswordMessage != null &&
              state.PasswordMessage.Type == StatusMessageType.NeedCipherPassphrase)
      {
        state.Command.SendPassword(password, false);
      }
      else return base.Process(commands, originalKey, key, state, promptId);

      return EditCommandResult.Continue;
    }

    public override EditCommandResult Process(string line)
    {
      if(string.Equals(line, "Need the secret key to do this.", StringComparison.Ordinal))
      {
        throw new KeyEditFailedException("Changing password failed.", FailureReason.MissingSecretKey);
      }
      else return EditCommandResult.Continue;
    }

    readonly SecureString password;
    bool sentCommand;
  }
  #endregion

  #region DeleteSigsCommand
  /// <summary>An edit command that deletes key signatures on user IDs.</summary>
  sealed class DeleteSigsCommand : EditSigsBase
  {
    public DeleteSigsCommand(KeySignature[] sigs) : base(sigs) { }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          if(key.SelectedUserId == null)
          {
            throw UnexpectedError("Can't delete signatures because no user ID is selected. "+
                                  "Perhaps it no longer exists?");
          }
          state.Command.SendLine("delsig");
          sentCommand = true;
          ExpectRelist = false;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "keyedit.delsig.valid", StringComparison.Ordinal))
      {
        // the previous line should have contained a sig: line that was parsed into the various sig* member variables.
        // we'll answer yes if the parsed signature appears to match any of the KeySignature objects we have
        state.Command.SendLine(CurrentSigMatches ? "Y" : "N"); // do we want to delete this particular signature?
      }
      else if(string.Equals(promptId, "keyedit.delsig.selfsig", StringComparison.Ordinal))
      {
        state.Command.SendLine("Y"); // yes, it's okay to delete a self-signature
      }
      else return base.Process(commands, originalKey, key, state, promptId);

      return EditCommandResult.Continue;
    }

    bool sentCommand;
  }
  #endregion

  #region DeleteSubkeysCommand
  /// <summary>An edit command that deletes subkeys.</summary>
  sealed class DeleteSubkeysCommand : EditCommand
  {
    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          state.Command.SendLine("delkey");
          sentCommand = true;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "keyedit.remove.subkey.okay", StringComparison.Ordinal))
      {
        state.Command.SendLine("Y"); // yes, it's okay to delete a subkey
      }
      else return base.Process(commands, originalKey, key, state, promptId);

      return EditCommandResult.Continue;
    }

    bool sentCommand;
  }
  #endregion

  #region DeleteUidCommand
  /// <summary>An edit command that deletes a user ID or attribute.</summary>
  sealed class DeleteUidCommand : EditCommand
  {
    public DeleteUidCommand()
    {
      ExpectRelist = true;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          // find the first user ID (ignoring attributes) that is NOT selected
          int i;
          for(i=0; i<key.UserIds.Count; i++)
          {
            if(!key.UserIds[i].IsAttribute && !key.UserIds[i].Selected) break;
          }
          // if they're all selected, then that's a problem
          if(i == key.UserIds.Count) throw UnexpectedError("Can't delete the last user ID!");

          state.Command.SendLine("deluid");
          sentCommand = true;
          ExpectRelist = false;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "keyedit.remove.uid.okay", StringComparison.Ordinal))
      {
        state.Command.SendLine("Y"); // yes, it's okay to delete a user id
      }
      else return base.Process(commands, originalKey, key, state, promptId);

      return EditCommandResult.Continue;
    }

    bool sentCommand;
  }
  #endregion

  #region EditSigsBase
  /// <summary>A base class for commands that edit signatures.</summary>
  abstract class EditSigsBase : RevokeBase
  {
    protected EditSigsBase(KeySignature[] sigs) : this(null, sigs)
    {
      ExpectRelist = true;
    }

    protected EditSigsBase(UserRevocationReason reason, KeySignature[] sigs) : base(reason)
    {
      if(sigs == null || sigs.Length == 0) throw new ArgumentException();
      this.sigs = sigs;
    }

    /// <summary>Determines whether the current signature matches any of the signatures given to the constructor.</summary>
    protected bool CurrentSigMatches
    {
      get
      {
        bool matches = false;
        if(sigs != null)
        {
          foreach(KeySignature sig in sigs)
          {
            if(sig.Exportable == sigExportable && sig.Type == sigType &&
               string.Equals(sig.KeyId, sigKeyId, StringComparison.Ordinal) && sig.CreationTime == sigCreation)
            {
              matches = true;
              break;
            }
          }
        }
        return matches;
      }
    }

    public override EditCommandResult Process(string line)
    {
      // GPG spits out sig: lines and then asks us questions about them. we parse the sig: line so we know what
      // signature GPG is talking about
      if(line.StartsWith("sig:", StringComparison.OrdinalIgnoreCase))
      {
        string[] fields = line.Split(':');
        sigKeyId = fields[4].ToUpperInvariant();
        sigCreation = GPG.ParseTimestamp(fields[5]);
        string sigTypeStr = fields[10];
        sigType = (OpenPGPSignatureType)GetHexValue(sigTypeStr[0], sigTypeStr[1]);
        sigExportable = sigTypeStr[2] == 'x';
        return EditCommandResult.Continue;
      }
      else return base.Process(line);
    }

    readonly KeySignature[] sigs;
    string sigKeyId;
    DateTime sigCreation;
    OpenPGPSignatureType sigType;
    bool sigExportable;
  }
  #endregion

  #region GetPrefsCommand
  /// <summary>An edit command that retrieves user preferences.</summary>
  sealed class GetPrefsCommand : EditCommand
  {
    public GetPrefsCommand()
    {
      ExpectRelist = true;
    }

    /// <param name="preferences">A <see cref="UserPreferences"/> object that will be filled with the user preferences.</param>
    public GetPrefsCommand(UserPreferences preferences)
    {
      if(preferences == null) throw new ArgumentNullException();
      preferences.PreferredCiphers.Clear();
      preferences.PreferredCompressions.Clear();
      preferences.PreferredHashes.Clear();
      this.preferences = preferences;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          EditUserId selectedId = key.SelectedUserId;
          if(selectedId == null) throw UnexpectedError("No user ID is selected. Perhaps the user ID doesn't exist?");

          // parse the user preferences from the string given in the key listing. this doesn't include the key server,
          // but we can retrieve it using the "showpref" command
          foreach(string pref in selectedId.Prefs.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
          {
            int id = int.Parse(pref.Substring(1));
            if(pref[0] == 'S') preferences.PreferredCiphers.Add((OpenPGPCipher)id);
            else if(pref[0] == 'H') preferences.PreferredHashes.Add((OpenPGPHashAlgorithm)id);
            else if(pref[0] == 'Z') preferences.PreferredCompressions.Add((OpenPGPCompression)id);
          }
          preferences.Primary = selectedId.Primary;

          state.Command.SendLine("showpref"); // this will cause GPG to print the preferences in a
          sentCommand = true;       // text format that we can parse below
          ExpectRelist = false;
          return EditCommandResult.Continue;
        }
        else return EditCommandResult.Next;
      }
      else return base.Process(commands, originalKey, key, state, promptId);
    }

    public override EditCommandResult Process(string line)
    {
      if(sentCommand) // if we sent the showpref command, then we can look for the preferred keyserver line
      {
        line = line.Trim();
        if(line.StartsWith("Preferred keyserver: ", StringComparison.Ordinal))
        {
          preferences.Keyserver = new Uri(line.Substring(21)); // 21 is the length of "Preferred keyserver: "
          return EditCommandResult.Done;
        }
      }
      return EditCommandResult.Continue;
    }

    readonly UserPreferences preferences;
    bool sentCommand;
  }
  #endregion

  #region QuitCommand
  /// <summary>A command that quits the edit session, optionally saving first.</summary>
  sealed class QuitCommand : EditCommand
  {
    public QuitCommand(bool save)
    {
      this.save = save;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          state.Command.SendLine(save ? "save" : "quit");
          sentCommand = true;
        }
        else // if GPG didn't quit, then something's wrong...
        {
          throw new KeyEditFailedException("An error occurred while " + (save ? "saving" : "quitting") +
                                           ". Changes may not have been applied.");
        }
      }
      else if(string.Equals(promptId, "keyedit.save.okay", StringComparison.Ordinal))
      {
        state.Command.SendLine(save ? "Y" : "N");
      }
      else return base.Process(commands, originalKey, key, state, promptId);

      return EditCommandResult.Continue;
    }

    readonly bool save;
    bool sentCommand;
  }
  #endregion

  #region RawCommand
  /// <summary>An edit command that sends a single command to GPG.</summary>
  sealed class RawCommand : EditCommand
  {
    public RawCommand(string command)
    {
      this.command = command;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
       state.Command.SendLine(command);
      return EditCommandResult.Done;
    }

    readonly string command;
  }
  #endregion

  #region RevokeBase
  /// <summary>A base class for edit commands that revoke stuff.</summary>
  abstract class RevokeBase : EditCommand
  {
    public RevokeBase(UserRevocationReason reason)
    {
      userReason = reason;
    }

    public RevokeBase(KeyRevocationReason reason)
    {
      keyReason = reason;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(HandleRevokePrompt(state.Command, promptId, keyReason, userReason, ref lines, ref lineIndex))
      {
        return EditCommandResult.Continue;
      }
      else return base.Process(commands, originalKey, key, state, promptId);
    }

    readonly UserRevocationReason userReason;
    readonly KeyRevocationReason keyReason;
    string[] lines;
    int lineIndex;
  }
  #endregion

  #region RevokeSigsCommand
  /// <summary>An edit command that revokes key signatures.</summary>
  sealed class RevokeSigsCommand : EditSigsBase
  {
    public RevokeSigsCommand(UserRevocationReason reason, KeySignature[] sigs) : base(reason, sigs) { }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          if(key.SelectedUserId == null)
          {
            throw UnexpectedError("Can't revoke signatures because no user ID is selected. "+
                                  "Perhaps the user ID no longer exists?");
          }
          state.Command.SendLine("revsig");
          sentCommand = true;
          ExpectRelist = false;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "ask_revoke_sig.one", StringComparison.Ordinal))
      {
        // the previous line should have contained a sig: line that was parsed into the various sig* member variables.
        // we'll answer yes if the parsed signature appears to match any of the KeySignature objects we have
        state.Command.SendLine(CurrentSigMatches ? "Y" : "N"); // do we want to revoke this particular signature?
      }
      else if(string.Equals(promptId, "ask_revoke_sig.okay", StringComparison.Ordinal))
      {
        state.Command.SendLine("Y");
      }
      else return base.Process(commands, originalKey, key, state, promptId);

      return EditCommandResult.Continue;
    }

    bool sentCommand;
  }
  #endregion

  #region RevokeSubkeysCommand
  /// <summary>An edit command that revokes subkeys.</summary>
  sealed class RevokeSubkeysCommand : RevokeBase
  {
    public RevokeSubkeysCommand(KeyRevocationReason reason) : base(reason) { }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          state.Command.SendLine("revkey");
          sentCommand = true;
        }
        else if(!sentConfirmation) // if GPG never asked us if we were sure, then that means it failed
        {
          throw UnexpectedError("Unable to delete subkeys. Perhaps the subkey no longer exists?");
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "keyedit.revoke.subkey.okay", StringComparison.Ordinal))
      {
        state.Command.SendLine("Y");
        sentConfirmation = true;
      }
      else return base.Process(commands, originalKey, key, state, promptId);

      return EditCommandResult.Continue;
    }

    bool sentCommand, sentConfirmation;
  }
  #endregion

  #region RevokeUidCommand
  /// <summary>An edit command that revokes user IDs and attributes.</summary>
  sealed class RevokeUidCommand : RevokeBase
  {
    public RevokeUidCommand(UserRevocationReason reason) : base(reason)
    {
      ExpectRelist = true;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          if(!sentCommand)
          {
            if(key.SelectedUserId == null)
            {
              throw UnexpectedError("Can't revoke user IDs because none are selected. Perhaps they no longer exist?");
            }

            state.Command.SendLine("revuid");
            sentCommand = true;
            ExpectRelist = false;
          }
          else if(!sentConfirmation) // if GPG never asked us if we were sure, then that means it failed
          {
            throw UnexpectedError("Unable to revoke user IDs.");
          }
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "keyedit.revoke.uid.okay", StringComparison.Ordinal))
      {
        state.Command.SendLine("Y");
        sentConfirmation = true;
      }
      else return base.Process(commands, originalKey, key, state, promptId);

      return EditCommandResult.Continue;
    }

    bool sentCommand, sentConfirmation;
  }
  #endregion

  #region SelectLastUidCommand
  /// <summary>An edit command that selects the last user ID or attribute in the list.</summary>
  sealed class SelectLastUidCommand : EditCommand
  {
    public SelectLastUidCommand()
    {
      ExpectRelist = true;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(key.SelectedUserId != null) // clear the existing selection first
        {
          state.Command.SendLine("uid -");
          return EditCommandResult.Continue;
        }

        if(!key.UserIds[key.UserIds.Count-1].Selected) // then, if the UID is not already selected, select it
        {
          state.Command.SendLine("uid " + key.UserIds.Count.ToInvariantString());
          return EditCommandResult.Done;
        }

        return EditCommandResult.Next;
      }
      else return base.Process(commands, originalKey, key, state, promptId);
    }
  }
  #endregion

  #region SelectSubkeyCommand
  /// <summary>An edit command that selects a subkey by fingerprint.</summary>
  sealed class SelectSubkeyCommand : EditCommand
  {
    /// <param name="fingerprint">The fingerprint of the subkey to select.</param>
    /// <param name="deselectFirst">True to deselect other subkeys first, and false to not.</param>
    public SelectSubkeyCommand(string fingerprint, bool deselectFirst)
    {
      if(string.IsNullOrEmpty(fingerprint)) throw new ArgumentException("Fingerprint was null or empty.");
      this.fingerprint   = fingerprint;
      this.deselectFirst = deselectFirst;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(deselectFirst && !clearedSelection)
        {
          state.Command.SendLine("key -");   // GPG doesn't let us know which keys are currently selected, so we'll assume the
          clearedSelection = true; // worst and deselect all keys
          return EditCommandResult.Continue;
        }
        else
        {
          // find the subkey with the given fingerprint
          int index;
          for(index=0; index < key.Subkeys.Count; index++)
          {
            if(string.Equals(fingerprint, key.Subkeys[index], StringComparison.Ordinal)) break;
          }

          if(index == key.Subkeys.Count) throw UnexpectedError("No subkey found with fingerprint " + fingerprint);

          // then select it
          state.Command.SendLine("key " + (index+1).ToInvariantString());
          return EditCommandResult.Done;
        }
      }
      else return base.Process(commands, originalKey, key, state, promptId);
    }

    readonly string fingerprint;
    readonly bool deselectFirst;
    bool clearedSelection;
  }
  #endregion

  #region SelectUidCommand
  /// <summary>An edit command that selects a given user ID.</summary>
  sealed class SelectUidCommand : EditCommand
  {
    public SelectUidCommand(EditUserId id)
    {
      if(id == null) throw new ArgumentNullException();
      this.id = id;
      ExpectRelist = true;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        // find the UID that matches the given ID
        int index;
        for(index=0; index < key.UserIds.Count; index++)
        {
          if(id.Matches(key.UserIds[index])) // we found it
          {
            // make sure no other UIDs match it
            for(int i=index+1; i < key.UserIds.Count; i++)
            {
              if(id.Matches(key.UserIds[i])) throw UnexpectedError("Multiple user IDs matched " + id.ToString());
            }
            break;
          }
        }

        if(index == key.UserIds.Count) throw UnexpectedError("No user ID matched " + id.ToString());

        // if any UIDs besides the one we want are selected, deselect them
        for(int i=0; i < key.UserIds.Count; i++)
        {
          if(i != index && key.UserIds[i].Selected)
          {
            state.Command.SendLine("uid -");
            return EditCommandResult.Continue;
          }
        }

        // if the one we want is not currently selected, select it
        if(!key.UserIds[index].Selected)
        {
          state.Command.SendLine("uid " + (index+1).ToInvariantString());
          return EditCommandResult.Done;
        }

        return EditCommandResult.Next;
      }
      else return base.Process(commands, originalKey, key, state, promptId);
    }

    readonly EditUserId id;
  }
  #endregion

  #region SetAlgoPrefsCommand
  /// <summary>An edit command that sets user algorithm preferences.</summary>
  sealed class SetAlgoPrefsCommand : EditCommand
  {
    public SetAlgoPrefsCommand(UserPreferences preferences)
    {
      // create the preference string from the given preferences object
      StringBuilder prefString = new StringBuilder();

      foreach(OpenPGPCipher cipher in preferences.PreferredCiphers)
      {
        prefString.Append(" S").Append(((int)cipher).ToInvariantString());
      }
      foreach(OpenPGPHashAlgorithm hash in preferences.PreferredHashes)
      {
        prefString.Append(" H").Append(((int)hash).ToInvariantString());
      }
      foreach(OpenPGPCompression compression in preferences.PreferredCompressions)
      {
        prefString.Append(" Z").Append(((int)compression).ToInvariantString());
      }

      this.prefString = prefString.ToString();
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentPrefs)
        {
          state.Command.SendLine("setpref " + prefString);
          sentPrefs = true;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "keyedit.setpref.okay", StringComparison.Ordinal))
      {
        state.Command.SendLine("Y");
      }
      else return base.Process(commands, originalKey, key, state, promptId);

      return EditCommandResult.Continue;
    }

    string prefString;
    bool sentPrefs;
  }
  #endregion

  #region SetDefaultPasswordCommand
  sealed class SetDefaultPasswordCommand : EditCommand
  {
    public SetDefaultPasswordCommand(SecureString password)
    {
      this.password = password;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      state.DefaultPassword = password;
      return EditCommandResult.Next;
    }

    readonly SecureString password;
  }
  #endregion

  #region SetPrefsCommand
  /// <summary>An edit command that enqueues other commands to set the selected user's preferences.</summary>
  sealed class SetPrefsCommand : EditCommand
  {
    public SetPrefsCommand(UserPreferences preferences)
    {
      this.preferences = preferences;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(preferences.Primary) commands.Enqueue(new SetPrimaryCommand());

        if(preferences.Keyserver != null)
        {
          commands.Enqueue(new RawCommand("keyserver " + preferences.Keyserver.AbsoluteUri));
        }

        if(preferences.PreferredCiphers.Count != 0 || preferences.PreferredCompressions.Count != 0 ||
           preferences.PreferredHashes.Count != 0)
        {
          commands.Enqueue(new SetAlgoPrefsCommand(preferences));
        }

        return EditCommandResult.Next;
      }
      else return base.Process(commands, originalKey, key, state, promptId);
    }

    readonly UserPreferences preferences;
  }
  #endregion

  #region SetPrimaryCommand
  /// <summary>An edit command that sets the currently-selected user ID or attribute as primary.</summary>
  sealed class SetPrimaryCommand : EditCommand
  {
    public SetPrimaryCommand()
    {
      ExpectRelist = true;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(key.SelectedUserId == null)
        {
          throw UnexpectedError("Can't set primary user ID because no user ID is selected.");
        }

        if(!key.SelectedUserId.Primary) // if it's not already primary, make it so
        {
          state.Command.SendLine("primary");
          return EditCommandResult.Done;
        }
        else return EditCommandResult.Next; // otherwise, just go to the next command
      }
      else return base.Process(commands, originalKey, key, state, promptId);
    }
  }
  #endregion

  #region SetTrustCommand
  /// <summary>An edit command that sets the owner trust of the primary key.</summary>
  sealed class SetTrustCommand : EditCommand
  {
    public SetTrustCommand(TrustLevel level)
    {
      this.level = level;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          state.Command.SendLine("trust");
          sentCommand = true;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "edit_ownertrust.value", StringComparison.Ordinal))
      {
        switch(level)
        {
          case TrustLevel.Never: state.Command.SendLine("2"); break;
          case TrustLevel.Marginal: state.Command.SendLine("3"); break;
          case TrustLevel.Full: state.Command.SendLine("4"); break;
          case TrustLevel.Ultimate: state.Command.SendLine("5"); break;
          default: state.Command.SendLine("1"); break;
        }
      }
      else if(string.Equals(promptId, "edit_ownertrust.set_ultimate.okay", StringComparison.Ordinal))
      {
        state.Command.SendLine("Y"); // yes, it's okay to set ultimate trust
      }
      else return base.Process(commands, originalKey, key, state, promptId);

      return EditCommandResult.Continue;
    }

    readonly TrustLevel level;
    bool sentCommand;
  }
  #endregion

  #region SignKeyCommand
  /// <summary>An edit command that signs a primary key or the currently-selected user IDs.</summary>
  sealed class SignKeyCommand : EditCommand
  {
    /// <param name="options">Options that control the signing.</param>
    /// <param name="signWholeKey">If true, the entire key should be signed. If false, only the selected user IDs
    /// should be signed.
    /// </param>
    public SignKeyCommand(KeySigningOptions options, bool signWholeKey)
    {
      this.options = options;
      this.signWholeKey = signWholeKey;

      if(options != null && ContainsControlCharacters(options.TrustDomain))
      {
        throw new ArgumentException("The trust domain contains control characters. Remove them.");
      }
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              CommandState state, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          // build the command name based on the options
          string prefix = null;
          if(options == null || !options.Exportable) prefix += "l";
          if(options != null)
          {
            if(options.TrustLevel != TrustLevel.Unknown) prefix += "t";
            if(options.Irrevocable) prefix += "nr";
          }
          state.Command.SendLine(prefix + "sign");
          sentCommand = true;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "sign_uid.class", StringComparison.Ordinal))
      {
        if(options == null || options.CertificationLevel == CertificationLevel.Undisclosed)
        {
          state.Command.SendLine("0");
        }
        else if(options.CertificationLevel == CertificationLevel.None) state.Command.SendLine("1");
        else if(options.CertificationLevel == CertificationLevel.Casual) state.Command.SendLine("2");
        else if(options.CertificationLevel == CertificationLevel.Rigorous) state.Command.SendLine("3");
        else throw new NotSupportedException("Certification level " + options.CertificationLevel.ToString() +
                                             " is not supported.");
      }
      else if(string.Equals(promptId, "sign_uid.okay", StringComparison.Ordinal))
      {
        state.Command.SendLine("Y"); // yes, it's okay to sign a UID
      }
      else if(string.Equals(promptId, "keyedit.sign_all.okay", StringComparison.Ordinal))
      {
        // GPG is saying that no UID is selected, and asking if we want to sign the whole key
        if(signWholeKey)
        {
          state.Command.SendLine("Y");
        }
        else // if that wasn't what the user asked for, then bail out
        {
          throw UnexpectedError("No user ID was selected, and you didn't request to sign the entire key. "+
                                "Perhaps the user ID to sign no longer exists?");
        }
      }
      else if(string.Equals(promptId, "trustsig_prompt.trust_value", StringComparison.Ordinal))
      {
        if(options == null || options.TrustLevel == TrustLevel.Unknown)
        {
          throw UnexpectedError("GPG asked about trust levels for a non-trust signature.");
        }
        else if(options.TrustLevel == TrustLevel.Marginal) state.Command.SendLine("1");
        else if(options.TrustLevel == TrustLevel.Full) state.Command.SendLine("2");
        else throw new NotSupportedException("Trust level " + options.TrustLevel.ToString() + " is not supported.");
      }
      else if(string.Equals(promptId, "trustsig_prompt.trust_depth", StringComparison.Ordinal))
      {
        state.Command.SendLine(options.TrustDepth.ToInvariantString());
      }
      else if(string.Equals(promptId, "trustsig_prompt.trust_regexp", StringComparison.Ordinal))
      {
        state.Command.SendLine(options.TrustDomain);
      }
      else return base.Process(commands, originalKey, key, state, promptId);

      return EditCommandResult.Continue;
    }

    readonly KeySigningOptions options;
    readonly bool signWholeKey;
    bool sentCommand;
  }
  #endregion
  #endregion

  /// <summary>Gets whether logging is enabled.</summary>
  bool LoggingEnabled
  {
    get { return LineLogged != null; }
  }

  /// <summary>Throws an exception if <see cref="Initialize"/> has not yet been called.</summary>
  void AssertInitialized()
  {
    if(ExecutablePath == null) throw new InvalidOperationException("Initialize() has not been called.");
  }

  /// <summary>Throws an exception if the given type is not within the given array of supported types, with
  /// case-insensitive matching.
  /// </summary>
  void AssertSupported(string type, string[] supportedTypes, string name)
  {
    foreach(string supportedType in supportedTypes)
    {
      if(string.Equals(type, supportedType, StringComparison.OrdinalIgnoreCase)) return;
    }
    throw new ArgumentException(type + " is not a supported " + name + ".");
  }

  /// <summary>Performs the main work of both decryption and verification.</summary>
  Signature[] DecryptVerifyCore(Command cmd, Stream signedData, Stream destination, DecryptionOptions options)
  {
    CommandState state = new CommandState(cmd);
    using(cmd)
    {
      List<Signature> signatures = new List<Signature>(); // this holds the completed signatures
      Signature sig = new Signature(); // keep track of the current signature
      bool sigFilled = false, triedPasswordInOptions = false;

      cmd.StandardErrorLine += delegate(string line) { DefaultStandardErrorHandler(line, state); };
      cmd.InputNeeded += delegate(string promptId)
      {
        if(string.Equals(promptId, "passphrase.enter", StringComparison.Ordinal) && state.PasswordMessage != null &&
           state.PasswordMessage.Type == StatusMessageType.NeedCipherPassphrase)
        {
          // we'll first try sending the password from the options if we have it, but only once.
          if(!triedPasswordInOptions &&
             options != null && options.Password != null && options.Password.Length != 0)
          {
            triedPasswordInOptions = true;
            cmd.SendPassword(options.Password, false);
          }
          else // we either don't have a password in the options, or we already sent it (and it probably failed),
          {    // so ask the user
            SecureString password = GetDecryptionPassword();
            if(password != null) cmd.SendPassword(password, true);
            else cmd.SendLine();
          }
        }
        else DefaultPromptHandler(promptId, state);
      };

      cmd.StatusMessageReceived += delegate(StatusMessage msg)
      {
        if(msg is TrustLevelMessage)
        {
          sig.TrustLevel = ((TrustLevelMessage)msg).Level;
        }
        else
        {
          // if the message begins a new signature, add the previous one (if it's complete enough to add)
          if(msg.Type == StatusMessageType.NewSig  || msg.Type == StatusMessageType.BadSig ||
             msg.Type == StatusMessageType.GoodSig || msg.Type == StatusMessageType.ErrorSig)
          {
            if(sigFilled) signatures.Add(sig);
            sig = new Signature();
            sigFilled = false; // the new signature is not complete enough to add
          }

          switch(msg.Type)
          {
            case StatusMessageType.BadSig:
            {
              BadSigMessage bad = (BadSigMessage)msg;
              sig.KeyId      = bad.KeyId;
              sig.SignerName = bad.UserName;
              sig.Status     = SignatureStatus.Invalid;
              sigFilled      = true;
              break;
            }

            case StatusMessageType.ErrorSig:
            {
              ErrorSigMessage error = (ErrorSigMessage)msg;
              sig.HashAlgorithm = error.HashAlgorithm;
              sig.KeyId        = error.KeyId;
              sig.KeyType      = error.KeyType;
              sig.CreationTime = error.Timestamp;
              sig.Status       = SignatureStatus.Error | (error.MissingKey ? SignatureStatus.MissingKey : 0) |
                                 (error.UnsupportedAlgorithm ? SignatureStatus.UnsupportedAlgorithm : 0);
              sigFilled        = true;
              break;
            }

            case StatusMessageType.ExpiredKeySig:
            {
              ExpiredKeySigMessage em = (ExpiredKeySigMessage)msg;
              sig.KeyId      = em.KeyId;
              sig.SignerName = em.UserName;
              sig.Status    |= SignatureStatus.ExpiredKey;
              break;
            }

            case StatusMessageType.ExpiredSig:
            {
              ExpiredSigMessage em = (ExpiredSigMessage)msg;
              sig.KeyId      = em.KeyId;
              sig.SignerName = em.UserName;
              sig.Status    |= SignatureStatus.ExpiredSignature;
              break;
            }

            case StatusMessageType.GoodSig:
            {
              GoodSigMessage good = (GoodSigMessage)msg;
              sig.KeyId      = good.KeyId;
              sig.SignerName = good.UserName;
              sig.Status     = SignatureStatus.Valid | (sig.Status & SignatureStatus.ValidFlagMask);
              sigFilled      = true;
              break;
            }

            case StatusMessageType.RevokedKeySig:
            {
              RevokedKeySigMessage em = (RevokedKeySigMessage)msg;
              sig.KeyId      = em.KeyId;
              sig.SignerName = em.UserName;
              sig.Status    |= SignatureStatus.RevokedKey;
              break;
            }

            case StatusMessageType.ValidSig:
            {
              ValidSigMessage valid = (ValidSigMessage)msg;
              sig.HashAlgorithm         = valid.HashAlgorithm;
              sig.KeyType               = valid.KeyType;
              sig.PrimaryKeyFingerprint = valid.PrimaryKeyFingerprint;
              sig.Expiration            = valid.SignatureExpiration;
              sig.KeyFingerprint        = valid.SignatureKeyFingerprint;
              sig.CreationTime          = valid.SignatureTime;
              break;
            }

            default: DefaultStatusMessageHandler(msg, state); break;
          }
        }
      };

      cmd.Start();

      // write the signed and/or encrypted data to STDIN and read the plaintext from STDOUT
      if(destination == null) WriteStreamToProcess(signedData, cmd.Process);
      else ReadAndWriteStreams(destination, signedData, cmd.Process);

      cmd.WaitForExit();
      if(!cmd.SuccessfulExit) throw new DecryptionFailedException(state.FailureReasons);

      if(sigFilled) signatures.Add(sig); // add the final signature if it's filled out
      // make all the signature objects read only and return them
      foreach(Signature signature in signatures) signature.MakeReadOnly();
      return signatures.ToArray();
    }
  }

  /// <summary>Provides default handling for input prompts.</summary>
  void DefaultPromptHandler(string promptId, CommandState state)
  {
    if(string.Equals(promptId, "passphrase.enter", StringComparison.Ordinal) && state.PasswordMessage != null &&
       state.PasswordMessage.Type == StatusMessageType.NeedKeyPassphrase)
    {
      if(state.DefaultPassword != null)
      {
        state.Command.SendPassword(state.DefaultPassword, false);
      }
      else if(!SendKeyPassword(state.Command, state.PasswordHint, (NeedKeyPassphraseMessage)state.PasswordMessage))
      {
        state.FailureReasons |= FailureReason.OperationCanceled;
      }
    }
    else throw new NotImplementedException("Unhandled input request: " + promptId);
  }

  /// <summary>Provides default handling for status messages.</summary>
  void DefaultStatusMessageHandler(StatusMessage msg, CommandState state)
  {
    switch(msg.Type)
    {
      case StatusMessageType.UserIdHint: state.PasswordHint = ((UserIdHintMessage)msg).Hint; break;

      case StatusMessageType.InvalidRecipient:
      {
        state.FailureReasons |= FailureReason.InvalidRecipients;
        InvalidRecipientMessage m = (InvalidRecipientMessage)msg;
        throw new EncryptionFailedException(state.FailureReasons, "Invalid recipient "+m.Recipient+". "+m.ReasonText);
      }

      case StatusMessageType.BadPassphrase:
        if(state.DefaultPassword != null)
        {
          state.DefaultPassword = null;
        }
        else if(!state.Canceled) // don't send the OnInvalidPassword message if a user has simply hit Cancel
        {
          OnInvalidPassword(((BadPassphraseMessage)msg).KeyId);
          state.FailureReasons |= FailureReason.BadPassword;
        }
        break;

      case StatusMessageType.NoPublicKey: state.FailureReasons |= FailureReason.MissingPublicKey; break;
      case StatusMessageType.NoSecretKey: state.FailureReasons |= FailureReason.MissingSecretKey; break;

      case StatusMessageType.NeedKeyPassphrase:
      case StatusMessageType.NeedCipherPassphrase:
      case StatusMessageType.NeedPin:
        state.PasswordMessage = msg; // keep track of the password request message so it can be handled if we get a
        break;                       // password request prompt

      case StatusMessageType.UnexpectedData: case StatusMessageType.NoData:
        state.FailureReasons |= FailureReason.BadData;
        break;

      case StatusMessageType.DeleteFailed:
      {
        DeleteFailedMessage m = (DeleteFailedMessage)msg;
        if(m.Reason == DeleteFailureReason.NoSuchKey) state.FailureReasons |= FailureReason.KeyNotFound;
        break;
      }

      case StatusMessageType.GetBool: case StatusMessageType.GetHidden: case StatusMessageType.GetLine:
        throw new NotImplementedException("Unhandled input request: " + ((GetInputMessage)msg).PromptId);
    }
  }

  void DoEdit(PrimaryKey key, params EditCommand[] initialCommands)
  {
    DoEdit(key, null, true, initialCommands);
  }

  void DoEdit(PrimaryKey key, string args, bool addKeyring, params EditCommand[] initialCommands)
  {
    if(key == null) throw new ArgumentNullException();
    if(string.IsNullOrEmpty(key.Fingerprint)) throw new ArgumentException("The key to edit has no fingerprint.");

    EditKey originalKey = null, editKey = null;
    Queue<EditCommand> commands = new Queue<EditCommand>(initialCommands);

    args += " --with-colons --fixed-list-mode --edit-key " + key.EffectiveId;
    if(addKeyring) args = GetKeyringArgs(key.Keyring, true) + args;

    Command cmd = Execute(args, StatusMessages.MixIntoStdout, true);
    CommandState state = new CommandState(cmd);
    try
    {
      bool gotFreshList = false;

      cmd.StandardErrorLine += delegate(string line) { DefaultStandardErrorHandler(line, state); };

      cmd.Start();

      // the ExecuteForEdit() command coallesced the status lines into STDOUT, so we need to parse out the status
      // messages ourselves
      while(true)
      {
        string line;
        cmd.ReadLine(out line);
        if(line != null) LogLine(line);

        gotLine:
        if(line == null && cmd.StatusMessage == null) break;

        if(line == null) // it's a status message
        {
          switch(cmd.StatusMessage.Type)
          {
            case StatusMessageType.GetLine: case StatusMessageType.GetHidden: case StatusMessageType.GetBool:
            {
              string promptId = ((GetInputMessage)cmd.StatusMessage).PromptId;
              while(true) // input is needed, so process it
              {
                // if the queue is empty, add a quit command
                if(commands.Count == 0) commands.Enqueue(new QuitCommand(true));

                if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal) &&
                   !gotFreshList && commands.Peek().ExpectRelist)
                {
                  cmd.SendLine("list");
                  break;
                }

                EditCommandResult result = commands.Peek().Process(commands, originalKey, editKey, state, promptId);
                gotFreshList = false;
                if(result == EditCommandResult.Next || result == EditCommandResult.Done) commands.Dequeue();
                if(result == EditCommandResult.Continue || result == EditCommandResult.Done) break;
              }
              break;
            }

            default:
              DefaultStatusMessageHandler(cmd.StatusMessage, state);
              if(state.Canceled) throw new OperationCanceledException();
              break;
          }
        }
        else if(line.StartsWith("pub:", StringComparison.Ordinal)) // a key listing is beginning
        {
          // GPG outputs a key listing when edit mode is first started, and after commands that might change the key
          editKey = new EditKey();
          bool gotSubkey = false;

          do // parse the key listing
          {
            string[] fields = line.Split(':');

            switch(fields[0])
            {
              case "sub": gotSubkey = true; break;

              case "fpr":
                if(gotSubkey) editKey.Subkeys.Add(fields[9].ToUpperInvariant());
                break;

              case "uid": case "uat":
              {
                EditUserId uid = new EditUserId();
                uid.IsAttribute = fields[0][1] == 'a'; // it's an attribute if fields[0] == "uat"
                uid.Name        = fields[9];
                uid.Prefs       = fields[12].Split(',')[0];

                string[] bits = fields[13].Split(',');
                if(bits.Length > 1)
                {
                  foreach(char c in bits[1])
                  {
                    if(c == 'p') uid.Primary = true;
                    else if(c == 's') uid.Selected = true;
                  }
                }

                editKey.UserIds.Add(uid);
                break;
              }
            }

            cmd.ReadLine(out line);
            if(line != null) LogLine(line);
          } while(!string.IsNullOrEmpty(line)); // break out if the line is empty or a status message

          gotFreshList = true;

          // keep a copy of the original key state. this is useful to tell which user ID was initially primary, etc.
          if(originalKey == null) originalKey = editKey;

          // at this point, we've got a valid line, so jump to the part where we inspect it
          goto gotLine;
        }
        else // a line other than a key listing or a status line was received
        {
          while(true) // let the edit commands handle it
          {
            if(commands.Count == 0) break;

            EditCommandResult result = commands.Peek().Process(line);
            if(result == EditCommandResult.Next || result == EditCommandResult.Done) commands.Dequeue();
            if(result == EditCommandResult.Continue || result == EditCommandResult.Done) break;
          }
        }
      }

      cmd.WaitForExit();
    }
    catch // if an exception is thrown, GPG will probably be stuck at the menu waiting for input,
    {     // so we'll just kill it to prevent it from taking a long time to Dispose
      cmd.Kill();
      throw;
    }
    finally { cmd.Dispose(); }

    if(!cmd.SuccessfulExit) throw new KeyEditFailedException("Key edit failed.", state.FailureReasons);
  }

  /// <summary>Performs an edit command on groups of user attributes.</summary>
  void EditAttributes(UserAttribute[] attributes, EditCommandCreator cmdCreator)
  {
    foreach(List<UserAttribute> list in GroupAttributesByKey(attributes))
    {
      EditCommand[] commands = new EditCommand[list.Count+1];
      for(int i=0; i<list.Count; i++) commands[i] = new RawCommand("uid " + list[i].Id);
      commands[commands.Length-1] = cmdCreator();
      DoEdit(list[0].PrimaryKey, commands);
    }
  }

  /// <summary>Performs an edit command on a list of primary keys.</summary>
  void EditKeys(PrimaryKey[] keys, EditCommandCreator cmdCreator)
  {
    if(keys == null) throw new ArgumentNullException();

    foreach(PrimaryKey key in keys)
    {
      if(key == null) throw new ArgumentNullException("A key was null.");
      DoEdit(key, cmdCreator());
    }
  }

  /// <summary>Performs an edit command on groups of subkeys.</summary>
  void EditSubkeys(Subkey[] subkeys, EditCommandCreator cmdCreator)
  {
    foreach(List<Subkey> keyList in GroupSubkeysByKey(subkeys))
    {
      EditCommand[] commands = new EditCommand[keyList.Count+1];
      for(int i=0; i<keyList.Count; i++) commands[i] = new SelectSubkeyCommand(keyList[i].Fingerprint, false);
      commands[keyList.Count] = cmdCreator();
      DoEdit(keyList[0].PrimaryKey, commands);
    }
  }

  /// <summary>Performs an edit command on groups of key signatures.</summary>
  void EditSignatures(KeySignature[] signatures, KeySignatureEditCommandCreator cmdCreator)
  {
    // first group the signatures by the owning key and the signed object
    Dictionary<string, List<UserAttribute>> uidMap;
    Dictionary<string, List<KeySignature>> sigMap;
    GroupSignaturesByKeyAndObject(signatures, out uidMap, out sigMap);

    List<EditCommand> commands = new List<EditCommand>();
    foreach(KeyValuePair<string, List<UserAttribute>> pair in uidMap) // then, for each key to be edited...
    {
      bool firstUid = true;
      foreach(UserAttribute uid in pair.Value) // for each affected UID in the key
      {
        // select the UID
        if(!firstUid) commands.Add(new RawCommand("uid -"));
        commands.Add(new RawCommand("uid " + uid.Id));
        // then perform the command
        commands.Add(cmdCreator(sigMap[uid.Id].ToArray()));
        firstUid = false;
      }

      DoEdit(pair.Value[0].PrimaryKey, commands.ToArray());
      commands.Clear();
    }
  }

  /// <summary>Creates a new <see cref="Command"/> object and returns it.</summary>
  /// <param name="args">Command-line arguments to pass to GPG.</param>
  /// <param name="statusMessageHandling">How status messages will be handled.</param>
  /// <param name="closeStdInput">If true, STDIN will be closed immediately after starting the process so that
  /// GPG will not block waiting for input from it.
  /// </param>
  Command Execute(string args, StatusMessages statusMessageHandling, bool closeStdInput)
  {
    InheritablePipe commandPipe = null;
    if(statusMessageHandling != StatusMessages.Ignore) // if the status stream is requested...
    {
      commandPipe = new InheritablePipe(); // create a pipe for the command-fd
      string cmdFd = commandPipe.ClientHandle.ToInt64().ToInvariantString();
      string statusFd = statusMessageHandling == StatusMessages.MixIntoStdout ? "1" : cmdFd;
      args = "--exit-on-status-write-error --status-fd " + statusFd + " --command-fd " + cmdFd + " " + args;
    }
    return new Command(this, GetProcessStartInfo(ExecutablePath, args), commandPipe,
                       statusMessageHandling, closeStdInput);
  }

  /// <summary>Executes the given GPG executable with the given arguments.</summary>
  Process Execute(string exePath, string args)
  {
    return Process.Start(GetProcessStartInfo(exePath, args));
  }

  /// <summary>Performs the main work of exporting keys.</summary>
  void ExportCore(string args, Stream destination)
  {
    Command cmd = Execute(args, StatusMessages.ReadInBackground, true);
    CommandState state = new CommandState(cmd);
    using(cmd)
    {
      cmd.StatusMessageReceived += delegate(StatusMessage msg) { DefaultStatusMessageHandler(msg, state); };
      cmd.Start(); // simply start GPG and copy the output to the destination stream
      cmd.Process.StandardOutput.BaseStream.CopyTo(destination);
      cmd.WaitForExit();
    }

    if(!cmd.SuccessfulExit) throw new ExportFailedException(state.FailureReasons);
  }

  /// <summary>Generates a revocation certificate, either directly or via a designated revoker.</summary>
  void GenerateRevocationCertificateCore(PrimaryKey key, PrimaryKey designatedRevoker, Stream destination,
                                         KeyRevocationReason reason, OutputOptions outputOptions)
  {
    if(key == null || destination == null) throw new ArgumentNullException();

    string args = GetOutputArgs(outputOptions);
    if(designatedRevoker == null) // direct revocation certificate
    {
      args += GetKeyringArgs(key.Keyring, true) + "--gen-revoke ";
    }
    else // delegated revocation certificate
    {
      args += GetKeyringArgs(new PrimaryKey[] { key, designatedRevoker }, true) +
              "-u " + designatedRevoker.Fingerprint + " --desig-revoke ";
    }
    args += key.Fingerprint;

    Command cmd = Execute(args, StatusMessages.ReadInBackground, true);
    CommandState state = new CommandState(cmd);
    using(cmd)
    {
      string[] lines = null; // needed for the revocation prompt handler
      int lineIndex  = 0;

      cmd.StandardErrorLine += delegate(string line) { DefaultStandardErrorHandler(line, state); };
      cmd.StatusMessageReceived += delegate(StatusMessage msg) { DefaultStatusMessageHandler(msg, state); };

      cmd.InputNeeded += delegate(string promptId)
      {
        if(string.Equals(promptId, "gen_revoke.okay", StringComparison.Ordinal) ||
           string.Equals(promptId, "gen_desig_revoke.okay", StringComparison.Ordinal))
        {
          cmd.SendLine("Y");
        }
        else if(!HandleRevokePrompt(cmd, promptId, reason, null, ref lines, ref lineIndex))
        {
          if(!state.Canceled)
          {
            DefaultPromptHandler(promptId, state);
            if(state.Canceled) cmd.Kill(); // kill GPG if the user doesn't give the password, so it doesn't keep asking
          }
        }
      };

      cmd.Start();
      cmd.Process.StandardOutput.BaseStream.CopyTo(destination);
      cmd.WaitForExit();
    }

    if(!cmd.SuccessfulExit)
    {
      // if the user canceled
      if(state.Canceled) throw new OperationCanceledException();
      else
      {
        throw new PGPException("Unable to generate revocation certificate for key " + key.ToString(),
                               state.FailureReasons);
      }
    }
  }

  /// <summary>Does the work of retrieving and searching for keys.</summary>
  PrimaryKey[] GetKeys(Keyring[] keyrings, bool includeDefaultKeyring, ListOptions options, string searchArgs)
  {
    string args = "--with-fingerprint --with-fingerprint --with-colons --fixed-list-mode "; // machine-readable output

    // although GPG has a "show-keyring" option, it doesn't work with --with-colons, so we need to query each keyring
    // individually, so we can tell which keyring a key came from. this may cause problems with signature verification
    // if a key on one ring signs a key on another ring...
    List<PrimaryKey> keys = new List<PrimaryKey>(64);
    if(includeDefaultKeyring) GetKeys(keys, options, args, null, searchArgs);
    if(keyrings != null)
    {
      foreach(Keyring keyring in keyrings)
      {
        if(keyring == null) throw new ArgumentException("A keyring was null.");
        GetKeys(keys, options, args, keyring, searchArgs);
      }
    }

    // make the keys read only, which we couldn't do when they were first added to the list
    foreach(PrimaryKey key in keys) key.MakeReadOnly();

    return keys.ToArray();
  }

  /// <summary>Does the work of retrieving and searching for keys on a single keyring.</summary>
  void GetKeys(List<PrimaryKey> keys, ListOptions options, string args, Keyring keyring, string searchArgs)
  {
    ListOptions secretKeyHandling = options & ListOptions.SecretKeyMask;
    if(secretKeyHandling == ListOptions.IgnoreSecretKeys)
    {
      GetKeys(keys, options, args, keyring, searchArgs, false);
    }
    else
    {
      Dictionary<string,object> secretKeys = new Dictionary<string,object>(StringComparer.Ordinal);
      List<PrimaryKey> tempKeys = new List<PrimaryKey>(64);

      // get the list of secret keys and store their IDs in a dictionary
      GetKeys(tempKeys, options & ~(ListOptions.SignatureMask|ListOptions.RetrieveAttributes),
              args, keyring, searchArgs, true);
      foreach(PrimaryKey key in tempKeys) secretKeys[key.EffectiveId] = null;
      tempKeys.Clear();

      // then get the public keys and match them up to the secret ones
      GetKeys(tempKeys, options & ~ListOptions.SecretKeyMask, args, keyring, searchArgs, false);

      if(secretKeyHandling == ListOptions.RetrieveSecretKeys)
      {
        foreach(PrimaryKey key in tempKeys) key.HasSecretKey = secretKeys.ContainsKey(key.EffectiveId);
        keys.AddRange(tempKeys);
      }
      else
      {
        foreach(PrimaryKey key in tempKeys)
        {
          if(secretKeys.ContainsKey(key.EffectiveId))
          {
            key.HasSecretKey = true;
            keys.Add(key);
          }
        }
      }
    }
  }

  /// <summary>Does the work of retrieving and searching for keys on a single keyring.</summary>
  void GetKeys(List<PrimaryKey> keys, ListOptions options, string args, Keyring keyring,
               string searchArgs, bool secretKeys)
  {
    ListOptions signatures = options & ListOptions.SignatureMask;

    if(secretKeys) args += "--list-secret-keys ";
    else if(signatures != 0 && RetrieveKeySignatureFingerprints) args += "--check-sigs --no-sig-cache ";
    else if(signatures == ListOptions.RetrieveSignatures) args += "--list-sigs ";
    else if(signatures == ListOptions.VerifySignatures) args += "--check-sigs ";
    else args += "--list-keys ";

    args += GetKeyringArgs(keyring, secretKeys);

    // keep track of the initial key count so we know which ones were added by this call
    int initialKeyCount = keys.Count;

    // if we're searching, but GPG finds no keys, it will give an error. (it doesn't give an error if it found at least
    // one item searched for.) we'll keep track of this case and ignore the error if we happen to be searching.
    bool retrieveAttributes = (options & ListOptions.RetrieveAttributes) != 0;

    // annoyingly, GPG doesn't flush the attribute stream after writing an attribute, so we can't reliably read
    // attribute data in response to an ATTRIBUTE status message, because it may block waiting for data that's stuck in
    // GPG's output buffer. so we'll write the attribute data to a temp file, and create dummy attributes. then at the
    // end, we'll replace the dummy attributes with the real thing.

    // if attributes are being retrieved, create a new pipe and some syncronization primitives to help with the task
    InheritablePipe attrPipe;
    FileStream attrStream, attrTempStream;
    string attrTempFile;
    AttributeMessage attrMsg = null;
    AsyncCallback attrCallback = null;
    ManualResetEvent attrDone; // signaled when all attribute data has been read
    byte[] attrBuffer;
    if(retrieveAttributes)
    {
      attrPipe       = new InheritablePipe();
      attrStream     = new FileStream(new SafeFileHandle(attrPipe.ServerHandle, false), FileAccess.Read);
      attrDone       = new ManualResetEvent(false);
      attrTempFile   = Path.GetTempFileName();
      attrTempStream = new FileStream(attrTempFile, FileMode.Open, FileAccess.ReadWrite);
      attrBuffer     = new byte[4096];

      args += "--attribute-fd " + attrPipe.ClientHandle.ToInt64().ToInvariantString() + " ";
    }
    else // otherwise, attributes are not being retrieved, so we don't need them
    {
      attrPipe      = null;
      attrStream    = attrTempStream = null;
      attrDone      = null;
      attrTempFile  = null;
      attrBuffer    = null;
    }

    Command cmd = Execute(args + searchArgs, retrieveAttributes ? StatusMessages.MixIntoStdout : StatusMessages.Ignore,
                          true);
    CommandState state = new CommandState(cmd);
    try
    {
      cmd.StandardErrorLine += delegate(string line) { DefaultStandardErrorHandler(line, state); };

      if(retrieveAttributes)
      {
        cmd.StatusMessageReceived += delegate(StatusMessage msg)
        {
          if(msg.Type == StatusMessageType.Attribute) attrMsg = (AttributeMessage)msg;
        };

        // create the callback for reading the attribute stream. we have to do this in the background because it could
        // block if we try to read the attribute stream in the main loop -- even if we use asynchronous IO
        attrCallback = delegate(IAsyncResult result)
        {
          int bytesRead = attrStream.EndRead(result);
          if(bytesRead == 0) // if we're at EOF, then we're done
          {
            attrDone.Set();
          }
          else // otherwise, some data was read
          {
            attrTempStream.Write(attrBuffer, 0, bytesRead); // so write it to the temporary output stream
            attrStream.BeginRead(attrBuffer, 0, attrBuffer.Length, attrCallback, null);
          }
        };
      }

      cmd.Start();

      List<Subkey> subkeys = new List<Subkey>(); // holds the subkeys in the current primary key
      List<KeySignature> sigs = new List<KeySignature>(32); // holds the signatures on the last key or user id
      List<UserAttribute> attributes = new List<UserAttribute>(); // holds user attributes on the key
      List<string> revokers = new List<string>(); // holds designated revokers on the key

      PrimaryKey currentPrimary = null;
      Subkey currentSubkey = null;
      UserAttribute currentAttribute = null;

      // if we're retrieving attributes, start reading the data in the background
      if(retrieveAttributes) attrStream.BeginRead(attrBuffer, 0, attrBuffer.Length, attrCallback, null);

      while(true)
      {
        string line;
        cmd.ReadLine(out line);
        if(line == null)
        {
          if(cmd.StatusMessage == null) break;
          else continue;
        }

        // each line is a bunch of stuff separated by colons. this is documented in gpg-src\doc\DETAILS
        string[] fields = line.Split(':');
        switch(fields[0])
        {
          case "sig": case "rev": // a signature or revocation signature
            KeySignature sig = new KeySignature();
            if(!string.IsNullOrEmpty(fields[1]))
            {
              switch(fields[1][0])
              {
                case '!': sig.Status = SignatureStatus.Valid; break;
                case '-': sig.Status = SignatureStatus.Invalid; break;
                case '%': sig.Status = SignatureStatus.Error; break;
                case '?': sig.Status = SignatureStatus.Unverified; break;
              }
            }
            if(!string.IsNullOrEmpty(fields[4])) sig.KeyId        = fields[4].ToUpperInvariant();
            if(!string.IsNullOrEmpty(fields[5])) sig.CreationTime = GPG.ParseTimestamp(fields[5]);
            if(!string.IsNullOrEmpty(fields[9])) sig.SignerName   = CUnescape(fields[9]);
            if(fields[10] != null && fields[10].Length >= 2)
            {
              string type = fields[10];
              sig.Type = (OpenPGPSignatureType)GetHexValue(type[0], type[1]);
              sig.Exportable = type.Length >= 3 && type[2] == 'x';
            }
            if(fields.Length > 12 && !string.IsNullOrEmpty(fields[12]))
            {
              sig.KeyFingerprint = fields[12].ToUpperInvariant();
            }
            sigs.Add(sig);
            break;

          case "uid": // user id
          {
            FinishAttribute(attributes, sigs, currentPrimary, currentSubkey, ref currentAttribute);
            UserId userId = new UserId();
            if(!string.IsNullOrEmpty(fields[1]))
            {
              char c = fields[1][0];
              userId.CalculatedTrust = ParseTrustLevel(c);
              userId.Revoked         = c == 'r';
            }
            if(!string.IsNullOrEmpty(fields[5])) userId.CreationTime    = ParseTimestamp(fields[5]);
            if(!string.IsNullOrEmpty(fields[7])) userId.Id              = fields[7].ToUpperInvariant();
            if(!string.IsNullOrEmpty(fields[9])) userId.Name            = CUnescape(fields[9]);
            currentAttribute = userId;
            break;
          }

          case "pub": case "sec": // public and secret primary keys
            FinishPrimaryKey(keys, subkeys, attributes, sigs, revokers,
                             ref currentPrimary, ref currentSubkey, ref currentAttribute, options);
            currentPrimary = new PrimaryKey();
            currentPrimary.Keyring = keyring;
            ReadKeyData(currentPrimary, fields);
            currentPrimary.HasSecretKey = fields[0][0] == 's'; // it's secret if the field was "sec"
            break;

          case "sub": case "ssb": // public and secret subkeys
            FinishSubkey(subkeys, sigs, currentPrimary, ref currentSubkey, currentAttribute);
            currentSubkey = new Subkey();
            ReadKeyData(currentSubkey, fields);
            break;

          case "fpr": // key fingerprint
            if(currentSubkey != null) currentSubkey.Fingerprint = fields[9].ToUpperInvariant();
            else if(currentPrimary != null) currentPrimary.Fingerprint = fields[9].ToUpperInvariant();
            break;

          case "uat": // user attribute
          {
            FinishAttribute(attributes, sigs, currentPrimary, currentSubkey, ref currentAttribute);

            if(retrieveAttributes && attrMsg != null)
            {
              currentAttribute = new DummyAttribute(attrMsg);
              currentAttribute.Primary = attrMsg.IsPrimary;
              currentAttribute.Revoked = attrMsg.IsRevoked;
              if(!string.IsNullOrEmpty(fields[1])) currentAttribute.CalculatedTrust = ParseTrustLevel(fields[1][0]);
              if(!string.IsNullOrEmpty(fields[5])) currentAttribute.CreationTime    = ParseTimestamp(fields[5]);
              if(!string.IsNullOrEmpty(fields[7])) currentAttribute.Id              = fields[7].ToUpperInvariant();
              attrMsg = null;
            }
            break;
          }

          case "rvk": // a designated revoker
            revokers.Add(fields[9].ToUpperInvariant());
            break;

          case "crt": case "crs": // X.509 certificates (we just treat them as an end to the current key)
            FinishPrimaryKey(keys, subkeys, attributes, sigs, revokers,
                             ref currentPrimary, ref currentSubkey, ref currentAttribute, options);
            break;
        }
      }

      FinishPrimaryKey(keys, subkeys, attributes, sigs, revokers,
                       ref currentPrimary, ref currentSubkey, ref currentAttribute, options);
      cmd.WaitForExit();

      if(retrieveAttributes)
      {
        attrPipe.CloseClient(); // GPG should be done writing data now, so close its end of the pipe
        attrDone.WaitOne();     // wait for us to read all the data

        // the attribute data is done being read, so now we can go back and replace all the dummy attributes
        attrTempStream.Position = 0;
        for(int i=initialKeyCount; i<keys.Count; i++)
        {
          PrimaryKey key = keys[i];
          if(key.Attributes.Count != 0)
          {
            for(int j=0; j<key.Attributes.Count; j++)
            {
              DummyAttribute dummy = (DummyAttribute)key.Attributes[j];
              byte[] attrData = new byte[dummy.Message.Length];

              if(attrTempStream.Read(attrData, 0, attrData.Length, false) != attrData.Length)
              {
                LogLine("Ignoring truncated attribute.");
              }
              else
              {
                UserAttribute real = UserAttribute.Create(dummy.Message.AttributeType, attrData);
                real.CalculatedTrust = dummy.CalculatedTrust;
                real.CreationTime    = dummy.CreationTime;
                real.Id              = dummy.Id;
                real.PrimaryKey      = dummy.PrimaryKey;
                real.Primary         = dummy.Primary;
                real.Revoked         = dummy.Revoked;
                real.Signatures      = dummy.Signatures;
                real.MakeReadOnly();
                attributes.Add(real);
              }
            }

            key.Attributes = attributes.Count == 0 ?
              NoAttributes : new ReadOnlyListWrapper<UserAttribute>(attributes.ToArray());
            attributes.Clear();
          }
        }
      }
    }
    finally
    {
      cmd.Dispose();
      if(retrieveAttributes)
      {
        attrDone.Close();
        attrStream.Dispose();
        attrPipe.Dispose();
        attrTempStream.Close();
        File.Delete(attrTempFile);
      }
    }

    if(!cmd.SuccessfulExit)
    {
      // if we're searching, we don't want to throw an exception just because GPG didn't find what we were searching
      // for. so only throw if we're not searching or there's a failure reason besides a missing key
      if(string.IsNullOrEmpty(searchArgs) ||
         (state.FailureReasons & ~(FailureReason.KeyNotFound | FailureReason.MissingSecretKey |
                                   FailureReason.MissingPublicKey)) != 0)
      {
        throw new KeyRetrievalFailedException(state.FailureReasons);
      }
    }
  }

  /// <summary>Creates and returns a new <see cref="ProcessStartInfo"/> for the given GPG executable and arguments.</summary>
  ProcessStartInfo GetProcessStartInfo(string exePath, string args)
  {
    ProcessStartInfo psi = new ProcessStartInfo();
    // enable or disable the GPG agent on GPG 1.x, but on GPG 2.x, the agent is always enabled...
    psi.Arguments              = (gpgVersion >= 20000 ? null : EnableGPGAgent ? "--use-agent " : "--no-use-agent ") +
                                 "--no-tty --no-options --display-charset utf-8 " + args;
    psi.CreateNoWindow         = true;
    psi.ErrorDialog            = false;
    psi.FileName               = exePath;
    psi.RedirectStandardError  = true;
    psi.RedirectStandardInput  = true;
    psi.RedirectStandardOutput = true;
    psi.StandardErrorEncoding  = Encoding.UTF8;
    psi.StandardOutputEncoding = Encoding.UTF8;
    psi.UseShellExecute        = false;

    return psi;
  }

  /// <summary>Executes a command and collects import-related information.</summary>
  ImportedKey[] ImportCore(Command cmd, Stream source, out CommandState state)
  {
    // GPG sometimes sends multiple messages for a single key, for instance when the key has several subkeys or a
    // secret portion. so we'll keep track of how fingerprints map to ImportedKey objects, so we'll know whether to
    // modify the existing object or create a new one
    Dictionary<string, ImportedKey> keysByFingerprint = new Dictionary<string, ImportedKey>();
    // we want to return keys in the order they were processed, so we'll keep this ordered list of fingerprints
    List<string> fingerprintsSeen = new List<string>();

    CommandState localState = state = new CommandState(cmd);
    using(cmd)
    {
      cmd.StandardErrorLine += delegate(string line) { DefaultStandardErrorHandler(line, localState); };

      cmd.StatusMessageReceived += delegate(StatusMessage msg)
      {
        if(msg.Type == StatusMessageType.ImportOkay)
        {
          KeyImportOkayMessage m = (KeyImportOkayMessage)msg;

          ImportedKey key;
          if(!keysByFingerprint.TryGetValue(m.Fingerprint, out key))
          {
            key = new ImportedKey();
            key.Fingerprint = m.Fingerprint;
            key.Successful  = true;
            keysByFingerprint[key.Fingerprint] = key;
            fingerprintsSeen.Add(key.Fingerprint);
          }

          if((m.Reason & KeyImportReason.ContainsSecretKey) != 0) key.Secret = true;
        }
        else if(msg.Type == StatusMessageType.ImportProblem)
        {
          KeyImportFailedMessage m = (KeyImportFailedMessage)msg;

          ImportedKey key;
          if(!keysByFingerprint.TryGetValue(m.Fingerprint, out key))
          {
            key = new ImportedKey();
            key.Fingerprint = m.Fingerprint;
            keysByFingerprint[key.Fingerprint] = key;
            fingerprintsSeen.Add(key.Fingerprint);
          }

          key.Successful = false;
        }
        else { DefaultStatusMessageHandler(msg, localState); }
      };

      cmd.Start();
      if(source != null) WriteStreamToProcess(source, cmd.Process);
      cmd.WaitForExit();
    }

    ImportedKey[] keysProcessed = new ImportedKey[fingerprintsSeen.Count];
    for(int i=0; i<keysProcessed.Length; i++)
    {
      keysProcessed[i] = keysByFingerprint[fingerprintsSeen[i]];
      keysProcessed[i].MakeReadOnly();
    }
    return keysProcessed;
  }

  /// <summary>Performs the main work for key server operations.</summary>
  ImportedKey[] KeyServerCore(string args, string name, bool isImport)
  {
    CommandState state;
    Command cmd = Execute(args, StatusMessages.ReadInBackground, true);
    ImportedKey[] keys = ImportCore(cmd, null, out state);

    // during a keyring refresh, it's very likely that one of the keys won't be found on a keyserver, but we don't want
    // to throw an exception unless no keys were refreshed or we got a failure reason other than BadData or KeyNotFound
    if(!cmd.SuccessfulExit &&
       (!string.Equals(name, "Keyring refresh", StringComparison.Ordinal) || keys.Length == 0 ||
        (state.FailureReasons & ~(FailureReason.KeyNotFound | FailureReason.BadData)) != 0))
    {
      throw new KeyServerFailedException(name + " failed.", state.FailureReasons);
    }

    return keys;
  }

  /// <summary>Sends a line to the log if logging is enabled.</summary>
  void LogLine(string line)
  {
    if(LineLogged != null) LineLogged(line);
  }

  /// <summary>Edits each key given with a single edit command.</summary>
  void RepeatedRawEditCommand(PrimaryKey[] keys, string command)
  {
    EditKeys(keys, delegate { return new RawCommand(command); });
  }

  /// <summary>Does the work of revoking keys, either directly or via a designated revoker.</summary>
  void RevokeKeysCore(PrimaryKey designatedRevoker, KeyRevocationReason reason, PrimaryKey[] keysToRevoke)
  {
    if(keysToRevoke == null) throw new ArgumentNullException();

    foreach(PrimaryKey key in keysToRevoke)
    {
      if(key == null || string.IsNullOrEmpty(key.Fingerprint))
      {
        throw new ArgumentException("A key was null or had no fingerprint.");
      }
    }

    MemoryStream ms = new MemoryStream();
    foreach(PrimaryKey key in keysToRevoke)
    {
      if(!key.Revoked)
      {
        if(designatedRevoker == null) GenerateRevocationCertificate(key, ms, reason, null);
        else GenerateRevocationCertificate(key, designatedRevoker, ms, reason, null);

        ms.Position = 0;
        ImportKeys(ms, key.Keyring);

        ms.Position = 0;
        ms.SetLength(0);
      }
    }
  }

  /// <summary>Gets a key password from the user and sends it to the command stream. Returns true if a password was
  /// given and false if not, although if 'passwordRequired' is true, an exception will be throw if a password is not
  /// given.
  /// </summary>
  bool SendKeyPassword(Command command, string passwordHint, NeedKeyPassphraseMessage msg)
  {
    string userIdHint = passwordHint + " (0x" + msg.KeyId;
    if(!string.Equals(msg.KeyId, msg.PrimaryKeyId, StringComparison.Ordinal))
    {
      userIdHint += " on primary key 0x" + msg.PrimaryKeyId;
    }
    userIdHint += ")";

    SecureString password = GetKeyPassword(msg.KeyId, userIdHint);
    if(password == null)
    {
      command.SendLine();
      return false;
    }
    else
    {
      command.SendPassword(password, true);
      return true;
    }
  }

  /// <summary>Performs the work of verifying either a detached or embedded signature.</summary>
  Signature[] VerifyCore(string signatureFile, Stream signedData, VerificationOptions options)
  {
    string args = GetVerificationArgs(options, false);
    // --verify takes either one or two arguments. we want the signed data to be sent on STDIN
    args += "--verify " + (signatureFile == null ? "-" : EscapeArg(signatureFile) + " -");
    Command cmd = Execute(args, StatusMessages.ReadInBackground, false);
    return DecryptVerifyCore(cmd, signedData, FileStream.Null, null);
  }

  /// <summary>Determines whether the string contains control characters.</summary>
  static bool ContainsControlCharacters(string str)
  {
    if(str != null)
    {
      foreach(char c in str)
      {
        if(c < ' ' || char.IsControl(c)) return true;
      }
    }

    return false;
  }

  /// <summary>Performs C-unescaping on the given string, which has special characters encoded as <c>\xHH</c>, where
  /// <c>HH</c> are the hex digits of the character.
  /// </summary>
  static string CUnescape(string str)
  {
    return cEscapeRe.Replace(str, delegate(Match m)
    {
      return new string((char)GetHexValue(m.Value[2], m.Value[3]), 1);
    });
  }

  /// <summary>Performs default handling for lines of text read from STDERR.</summary>
  static void DefaultStandardErrorHandler(string line, CommandState state)
  {
    // this is such a messy way to detect errors, but what else can we do?
    if((state.FailureReasons & FailureReason.KeyringLocked) == 0 &&
       (line.IndexOf(" file write error", StringComparison.Ordinal) != -1 ||
        line.IndexOf(" file rename error", StringComparison.Ordinal) != -1))
    {
      state.FailureReasons |= FailureReason.KeyringLocked;
    }
    else if((state.FailureReasons & FailureReason.KeyNotFound) == 0 &&
            line.IndexOf(" not found on keyserver", StringComparison.Ordinal) != -1)
    {
      state.FailureReasons |= FailureReason.KeyNotFound;
    }
    else if((state.FailureReasons & (FailureReason.KeyNotFound | FailureReason.MissingPublicKey)) !=
            (FailureReason.KeyNotFound | FailureReason.MissingPublicKey) &&
            line.IndexOf(" public key not found", StringComparison.Ordinal) != -1)
    {
      state.FailureReasons |= FailureReason.KeyNotFound | FailureReason.MissingPublicKey;
    }
    else if((state.FailureReasons & FailureReason.SecretKeyAlreadyExists) == 0 &&
            line.IndexOf(" already in secret keyring", StringComparison.Ordinal) != -1)
    {
      state.FailureReasons |= FailureReason.SecretKeyAlreadyExists;
    }
    else if((state.FailureReasons & FailureReason.MissingSecretKey) == 0 &&
            line.Equals("Need the secret key to do this.", StringComparison.Ordinal))
    {
      state.FailureReasons |= FailureReason.MissingSecretKey;
    }
    else if((state.FailureReasons & FailureReason.NoKeyServer) == 0 &&
            line.IndexOf(" no keyserver known", StringComparison.Ordinal) != -1)
    {
      state.FailureReasons |= FailureReason.NoKeyServer;
    }
    else if((state.FailureReasons & FailureReason.BadKeyServerUri) == 0 &&
            line.IndexOf(" bad URI", StringComparison.Ordinal) != -1)
    {
      state.FailureReasons |= FailureReason.BadKeyServerUri;
    }
    else if((state.FailureReasons & FailureReason.MissingSecretKey) == 0) // handle: secret key "Foo" not found
    {
      int index = line.IndexOf("secret key \"", StringComparison.Ordinal);
      if(index != -1 && index < line.IndexOf("\" not found")) state.FailureReasons |= FailureReason.MissingSecretKey;
    }
  }

  /// <summary>Exits a process by closing STDIN, STDOUT, and STDERR, and waiting for it to exit. If it doesn't exit
  /// within a short period, it will be killed. Returns the process' exit code.
  /// </summary>
  static int Exit(Process process)
  {
    process.StandardInput.Close();
    process.StandardOutput.Close();
    process.StandardError.Close();
    if(!process.WaitForExit(500))
    {
      process.Kill();
      process.WaitForExit(100);
    }
    return process.ExitCode;
  }

  /// <summary>Escapes a command-line argument or throws an exception if it cannot be escaped.</summary>
  static string EscapeArg(string arg)
  {
    if(arg == null) throw new ArgumentNullException();

    if(ContainsControlCharacters(arg))
    {
      throw new ArgumentException("Argument '"+arg+"' contains illegal control characters.");
    }

    // this is almost the ugliest escaping algorithm ever, beaten only by the algorithm needed to escape cmd.exe
    // commands. and it's not even guaranteed to work because it's actually IMPOSSIBLE to properly escape command-line
    // arguments on Windows.
    //
    // first we need to escape double quotes with backslashes. but if those quotes have backslashes before them, then
    // we need to escape all of those backslashes too. (ie, if a run of backslashes is followed by a quote, all the
    // backslashes and the quote must be escaped, but if a run of backslashes isn't followed by a quote, the
    // backslashes must NOT be escaped. this means we need an arbitrary amount of look-ahead.) finally, if it contains
    // a space or tab, or is zero-length, it must be wrapped with double quotes. and after all that, it's not
    // guaranteed to work because it relies on the assumption that the target program will use CommandLineToArgvW to
    // parse its command line, which not all programs do. but GPG does, at least for the moment, so we should be okay.
    //
    // thank goodness we're not invoking cmd.exe. if we were, the task would be an order of magnitude more arcane.
    //
    // this all stems from the original, stupid decision in Windows to make programs parse their own command line. the
    // command line is from the USER INTERFACE level (ie, from the shell). it's what the user types in. but what
    // programs want to have is a list of arguments, not the raw text that the user typed! the shell receives the
    // command line, so the shell should parse it! the fact that the .NET standard library copied this insanity is what
    // really annoys me. ProcessStartInfo.Arguments should be a string array of unescaped arguments. c'mon!!

    // we have to wrap the argument with double quotes if it contains a space or a tab or is zero-length
    bool mustWrapWithQuotes = arg.IndexOf(' ') != -1 || arg.IndexOf('\t') != -1 || arg.Length == 0;

    // if we don't have to wrap it with quotes, and it also doesn't contain any quotes, then we don't need to escape it
    if(!mustWrapWithQuotes && arg.IndexOf('"') == -1) return arg;

    StringBuilder escaped = new StringBuilder(arg.Length + 10);
    if(mustWrapWithQuotes) escaped.Append('"');

    int start = 0;
    while(start < arg.Length)
    {
      // if there's a run of backslashes starting here, we need to find the end of it
      int bsEnd = start;
      while(bsEnd < arg.Length && arg[bsEnd] == '\\') bsEnd++;

      if(bsEnd == start) // there was no run of backslashes
      {
        if(arg[start] == '"') escaped.Append("\\\""); // but there was a double quote, so we need to escape it
        else escaped.Append(arg[start]);
        start++;
      }
      else // there was one or more backslashes. we need to see if it ends (or will end) with a quote
      {
        // it ends with a quote, or will end with one after we wrap the argument with quotes
        int bsCount = bsEnd - start;
        if(bsEnd == arg.Length && mustWrapWithQuotes || arg[bsEnd] == '"')
        {
          bsCount *= 2; // double the number of backslashes
        }
        escaped.Append('\\', bsCount);
        start = bsEnd; // continue with the character after the backslashes
      }
    }

    if(mustWrapWithQuotes) escaped.Append('"');
    return escaped.ToString();
  }

  /// <summary>A helper for reading key listings, that finishes the current primary key.</summary>
  static void FinishPrimaryKey(List<PrimaryKey> keys, List<Subkey> subkeys, List<UserAttribute> attributes,
                               List<KeySignature> sigs, List<string> revokers,ref PrimaryKey currentPrimary,
                               ref Subkey currentSubkey, ref UserAttribute currentAttribute, ListOptions options)
  {
    // finishing a primary key finishes all signatures, subkeys, and user IDs on it
    FinishSignatures(sigs, currentPrimary, currentSubkey, currentAttribute);
    FinishSubkey(subkeys, sigs, currentPrimary, ref currentSubkey, currentAttribute);
    FinishAttribute(attributes, sigs, currentPrimary, currentSubkey, ref currentAttribute);

    if(currentPrimary != null)
    {
      currentPrimary.Subkeys = new ReadOnlyListWrapper<Subkey>(subkeys.ToArray());

      // the attributes will be split into UserIds and other attributes
      List<UserId> userIds = new List<UserId>(attributes.Count);
      List<UserAttribute> userAttributes = new List<UserAttribute>();
      foreach(UserAttribute attr in attributes)
      {
        UserId userId = attr as UserId;
        if(userId != null) userIds.Add(userId);
        else userAttributes.Add(attr);
      }

      currentPrimary.UserIds    = new ReadOnlyListWrapper<UserId>(userIds.ToArray());

      currentPrimary.Attributes = userAttributes.Count == 0 ?
        NoAttributes : new ReadOnlyListWrapper<UserAttribute>(userAttributes.ToArray());

      currentPrimary.DesignatedRevokers = revokers.Count == 0 ?
        NoRevokers : new ReadOnlyListWrapper<string>(revokers.ToArray());

      if(currentPrimary.Signatures == null) currentPrimary.Signatures = NoSignatures;

      // we don't make the primary key read only here because a bug in GPG causes us to not know the real attributes
      // until the process exits. so we'll make the key read only later. also, we may need to set .HasSecretKey later.
      // GetKeys() will do it for us.

      // filter out unusable keys if that was asked of us. we can't use PrimaryKey.Usable because whether we care about
      // TotalCapabilities depends on the options
      if((options & ListOptions.IgnoreUnusableKeys) == 0 ||
         !currentPrimary.Disabled && !currentPrimary.Expired && !currentPrimary.Revoked &&
         (currentPrimary.TotalCapabilities != KeyCapabilities.None || (options & ListOptions.SecretKeyMask) != 0))
      {
        keys.Add(currentPrimary);
      }

      currentPrimary = null;
    }

    subkeys.Clear();
    attributes.Clear();
    revokers.Clear();
  }

  /// <summary>A helper for reading key listings, that finishes the current key signatures.</summary>
  static void FinishSignatures(List<KeySignature> sigs, PrimaryKey currentPrimary, Subkey currentSubkey,
                               UserAttribute currentAttribute)
  {
    ReadOnlyListWrapper<KeySignature> list = new ReadOnlyListWrapper<KeySignature>(sigs.ToArray());

    // add the signatures to the most recent object in the key listing
    ISignableObject signedObject = null;

    if(currentAttribute != null)
    {
      if(currentAttribute.Signatures == null) // only set the signatures if they're not set already
      {
        currentAttribute.Signatures = list;
        signedObject = currentAttribute;
      }
    }
    else if(currentSubkey != null)
    {
      if(currentSubkey.Signatures == null) // only set the signatures if they're not set already
      {
        currentSubkey.Signatures = list;
        signedObject = currentSubkey;
      }
    }
    else if(currentPrimary != null)
    {
      if(currentPrimary.Signatures == null) // only set the signatures if they're not set already
      {
        currentPrimary.Signatures = list;
        signedObject = currentPrimary;
      }
    }

    if(signedObject != null)
    {
      foreach(KeySignature sig in list)
      {
        sig.Object = signedObject;
        sig.MakeReadOnly();
      }
    }

    sigs.Clear();
  }

  /// <summary>A helper for reading key listings, that finishes the current subkey.</summary>
  static void FinishSubkey(List<Subkey> subkeys, List<KeySignature> sigs,
                           PrimaryKey currentPrimary, ref Subkey currentSubkey, UserAttribute currentAttribute)
  {
    FinishSignatures(sigs, currentPrimary, currentSubkey, currentAttribute);

    if(currentSubkey != null && currentPrimary != null)
    {
      currentSubkey.PrimaryKey = currentPrimary;
      if(currentSubkey.Signatures == null) currentSubkey.Signatures = NoSignatures;

      currentSubkey.MakeReadOnly();
      subkeys.Add(currentSubkey);
      currentSubkey = null;
    }
  }

  /// <summary>A helper for reading key listings, that finishes the current user attribute.</summary>
  static void FinishAttribute(List<UserAttribute> attributes, List<KeySignature> sigs,
                              PrimaryKey currentPrimary, Subkey currentSubkey, ref UserAttribute currentAttribute)
  {
    FinishSignatures(sigs, currentPrimary, currentSubkey, currentAttribute);

    if(currentAttribute != null && currentPrimary != null)
    {
      currentAttribute.PrimaryKey = currentPrimary;

      if(currentAttribute is UserId) // the primary user ID is the first one listed
      {
        foreach(UserAttribute attr in attributes)
        {
          if(attr is UserId) goto notPrimary;
        }
        currentAttribute.Primary = true;
        notPrimary:;
      }

      if(currentAttribute.Signatures == null) currentAttribute.Signatures = NoSignatures;

      currentAttribute.MakeReadOnly();
      attributes.Add(currentAttribute);
      currentAttribute = null;
    }
  }

  /// <summary>Given a list of fingerprints, returns a string containing the fingerprints of each, separated by spaces.</summary>
  static string GetFingerprintArgs(IEnumerable<PrimaryKey> keys)
  {
    return GetFingerprintArgs(keys, null);
  }

  static string GetFingerprintArgs(IEnumerable<PrimaryKey> keys, string prefix)
  {
    if(!string.IsNullOrEmpty(prefix)) prefix += " ";

    string args = null;
    foreach(PrimaryKey key in keys)
    {
      if(key == null) throw new ArgumentException("A key was null.");
      if(string.IsNullOrEmpty(key.Fingerprint))
      {
        throw new ArgumentException("The key " + key.ToString() + " had no fingerprint.");
      }
      args += prefix + key.Fingerprint + " ";
    }
    return args;
  }

  /// <summary>Converts a hex digit into its integer value.</summary>
  static int GetHexValue(char c)
  {
    if(c >= '0' && c <= '9') return c-'0';
    else
    {
      c = char.ToLowerInvariant(c);
      if(c >= 'a' && c <= 'f') return c-'a'+10;
    }

    throw new ArgumentException("'"+c.ToString()+"' is not a hex digit.");
  }

  /// <summary>Converts two hex digits into their combined integer value.</summary>
  static int GetHexValue(char high, char low)
  {
    return (GetHexValue(high)<<4) + GetHexValue(low);
  }

  /// <summary>Gets an expiration date in days from now, or zero if the key should not expire.</summary>
  static int GetExpirationDays(DateTime? expiration)
  {
    int expirationDays = 0;

    if(expiration.HasValue)
    {
      DateTime utcExpiration = expiration.Value.ToUniversalTime(); // the date should be in UTC

      // give us 30 seconds of fudge time so the key doesn't expire between now and when we run GPG
      if(utcExpiration <= DateTime.UtcNow.AddSeconds(30))
      {
        throw new ArgumentException("The key expiration date must be in the future.");
      }

      // GPG supports expiration dates in two formats: absolute dates and times relative to the current time.
      // but it only supports absolute dates up to 2038, so we have to use a relative time format (days from now)
      expirationDays = (int)Math.Ceiling((utcExpiration - DateTime.UtcNow.Date).TotalDays);
    }

    return expirationDays;
  }

  /// <summary>Creates GPG arguments to represent the given <see cref="ExportOptions"/>.</summary>
  static string GetExportArgs(ExportOptions options, bool exportSecretKeys, bool addExportCommand)
  {
    string args = null;

    if(options != ExportOptions.Default)
    {
      args += "--export-options \"";
      if((options & ExportOptions.CleanKeys) != 0) args += "export-clean ";
      if((options & ExportOptions.ExcludeAttributes) != 0) args += "no-export-attributes ";
      if((options & ExportOptions.ExportLocalSignatures) != 0) args += "export-local-sigs ";
      if((options & ExportOptions.ExportSensitiveRevokerInfo) != 0) args += "export-sensitive-revkeys ";
      if((options & ExportOptions.MinimizeKeys) != 0) args += "export-minimize ";
      if((options & ExportOptions.ResetSubkeyPassword) != 0) args += "export-reset-subkey-passwd ";
      args += "\" ";
    }

    if(addExportCommand)
    {
      if(exportSecretKeys)
      {
        args += (options & ExportOptions.ClobberPrimarySecretKey) != 0 ?
                  "--export-secret-subkeys " : "--export-secret-keys ";
      }
      else args += "--export "; // exporting public keys
    }

    return args;
  }

  /// <summary>Creates GPG arguments to represent the given keyring.</summary>
  static string GetKeyringArgs(Keyring keyring, bool secretKeyringFile)
  {
    string args = null;

    if(keyring != null)
    {
      args += "--no-default-keyring --keyring " + EscapeArg(NormalizeKeyringFile(keyring.PublicFile)) + " ";

      if(secretKeyringFile && keyring.SecretFile != null)
      {
        args += "--secret-keyring " + EscapeArg(NormalizeKeyringFile(keyring.SecretFile)) + " ";
      }

      if(keyring.TrustDbFile != null)
      {
        args += "--trustdb-name " + EscapeArg(NormalizeKeyringFile(keyring.TrustDbFile)) + " ";
      }
    }

    return args;
  }

  /// <summary>Creates GPG arguments to represent the given <see cref="ExportOptions"/>.</summary>
  static string GetImportArgs(Keyring keyring, ImportOptions options)
  {
    string args = GetKeyringArgs(keyring, true);

    if(keyring != null)
    {
      // add the --primary-keyring option so that GPG will import into the keyrings we've given it
      args += "--primary-keyring " + EscapeArg(NormalizeKeyringFile(keyring.PublicFile)) + " ";
    }

    if(options != ImportOptions.Default)
    {
      args += "--import-options \"";
      if((options & ImportOptions.CleanKeys) != 0) args += "import-clean ";
      if((options & ImportOptions.ImportLocalSignatures) != 0) args += "import-local-sigs ";
      if((options & ImportOptions.MergeOnly) != 0) args += "merge-only ";
      if((options & ImportOptions.MinimizeKeys) != 0) args += "import-minimize ";
      args += "\" ";
    }

    return args;
  }

  /// <summary>Given a set of key capabilities, returns the key usage string expected by GPG during key creation.</summary>
  static string GetKeyUsageString(KeyCapabilities caps)
  {
    // don't check for the Certify flag because GPG doesn't allow us to use it anyway
    List<string> capList = new List<string>();
    if((caps & KeyCapabilities.Authenticate) != 0) capList.Add("auth");
    if((caps & KeyCapabilities.Encrypt) != 0) capList.Add("encrypt");
    if((caps & KeyCapabilities.Sign) != 0) capList.Add("sign");
    return string.Join(" ", capList.ToArray());
  }

  /// <summary>Creates GPG arguments to represent the given keyrings.</summary>
  static string GetKeyringArgs(IEnumerable<Keyring> keyrings, bool ignoreDefaultKeyring, bool wantSecretKeyrings)
  {
    string args = null, trustDb = null;
    bool trustDbSet = false;

    if(keyrings != null)
    {
      foreach(Keyring keyring in keyrings)
      {
        string thisTrustDb = keyring == null ? null : NormalizeKeyringFile(keyring.TrustDbFile);
        if(!trustDbSet)
        {
          trustDb    = thisTrustDb;
          trustDbSet = true;
        }
        else if(!string.Equals(trustDb, thisTrustDb, StringComparison.Ordinal))
        {
          throw new ArgumentException("Trust databases cannot be mixed in the same command. The two databases were "+
                                      trustDb + " and " + thisTrustDb);
        }

        if(keyring != null)
        {
          args += "--keyring " + EscapeArg(NormalizeKeyringFile(keyring.PublicFile)) + " ";
          if(wantSecretKeyrings && keyring.SecretFile != null)
          {
            args += "--secret-keyring " + EscapeArg(NormalizeKeyringFile(keyring.SecretFile)) + " ";
          }
        }
      }
    }

    if(ignoreDefaultKeyring)
    {
      if(args == null)
      {
        throw new ArgumentException("The default keyring is being ignored, but no valid keyrings were given.");
      }
      args += "--no-default-keyring ";
    }

    if(trustDb != null) args += "--trustdb-name " + EscapeArg(trustDb) + " ";

    return args;
  }

  /// <summary>Returns keyring arguments for all of the given keys.</summary>
  static string GetKeyringArgs(IEnumerable<PrimaryKey> keys, bool secretKeyrings)
  {
    string args = null, trustDb = null;
    bool trustDbSet = false, overrideDefaultKeyring = true;

    if(keys != null)
    {
      // keep track of which public and secret keyring files have been seen so we don't add them twice
      Dictionary<string, object> publicFiles = new Dictionary<string, object>(StringComparer.Ordinal);
      Dictionary<string, object> secretFiles = new Dictionary<string, object>(StringComparer.Ordinal);

      foreach(Key key in keys)
      {
        if(key == null) throw new ArgumentException("A key was null.");

        string thisTrustDb = key.Keyring == null ? null : NormalizeKeyringFile(key.Keyring.TrustDbFile);

        if(!trustDbSet)
        {
          trustDb    = thisTrustDb;
          trustDbSet = true;
        }
        else if(!string.Equals(trustDb, thisTrustDb, StringComparison.Ordinal))
        {
          throw new ArgumentException("Trust databases cannot be mixed in the same command. The two databases were "+
                                      trustDb + " and " + thisTrustDb);
        }

        if(key.Keyring == null)
        {
          overrideDefaultKeyring = false;
        }
        else if(secretKeyrings && key.Keyring.SecretFile == null)
        {
          throw new ArgumentException("Keyring " + key.Keyring.ToString() + " on key " + key.ToString() +
                                      " has no secret portion.");
        }
        else
        {
          string publicFile = NormalizeKeyringFile(key.Keyring.PublicFile);
          string secretFile = key.Keyring.SecretFile == null ? null : NormalizeKeyringFile(key.Keyring.SecretFile);

          if(!publicFiles.ContainsKey(publicFile))
          {
            publicFiles[publicFile] = null;
            args += "--keyring " + publicFile + " ";
          }

          if(secretKeyrings && !secretFiles.ContainsKey(secretFile))
          {
            secretFiles[secretFile] = null;
            args += "--secret-keyring " + secretFile + " ";
          }
        }
      }

      // if we added any keys, args will be non-null
      if(overrideDefaultKeyring && args != null) args += "--no-default-keyring ";
      // if we're using a non-default trust database, reference it
      if(trustDb != null) args += "--trustdb-name " + EscapeArg(trustDb) + " ";
    }

    return args;
  }

  /// <summary>Creates GPG arguments to represent the given <see cref="KeyServerOptions"/>.</summary>
  static string GetKeyServerArgs(KeyServerOptions options, bool requireKeyServer)
  {
    if(requireKeyServer)
    {
      if(options == null) throw new ArgumentNullException();
      if(options.KeyServer == null) throw new ArgumentException("No key server was specified.");
    }

    string args = null;

    if(options != null)
    {
      if(options.KeyServer != null) args += "--keyserver " + EscapeArg(options.KeyServer.AbsoluteUri) + " ";

      if(options.HttpProxy != null || options.Timeout != 0)
      {
        string optStr = null;
        if(options.HttpProxy != null) optStr += "http-proxy=" + options.HttpProxy.AbsoluteUri + " ";
        if(options.Timeout != 0) optStr += "timeout=" + options.Timeout.ToInvariantString() + " ";
        args += "--keyserver-options " + EscapeArg(optStr) + " ";
      }
    }

    return args;
  }

  /// <summary>Creates GPG arguments to represent the given <see cref="OutputOptions"/>.</summary>
  static string GetOutputArgs(OutputOptions options)
  {
    string args = null;
    if(options != null)
    {
      if(options.Format == OutputFormat.ASCII) args += "-a ";

      foreach(string comment in options.Comments)
      {
        if(!string.IsNullOrEmpty(comment)) args += "--comment " + EscapeArg(comment) + " ";
      }
    }
    return args;
  }

  /// <summary>Creates GPG arguments to represent the given <see cref="VerificationOptions"/>.</summary>
  static string GetVerificationArgs(VerificationOptions options, bool wantSecretKeyrings)
  {
    string args = null;
    if(options != null)
    {
      args += GetKeyringArgs(options.AdditionalKeyrings, options.IgnoreDefaultKeyring, wantSecretKeyrings);

      if(options.AutoFetchKeys)
      {
        args += "--auto-key-locate ";
        if(options.KeyServer != null) args += "keyserver ";
        args += "ldap pka cert ";
      }

      if(options.KeyServer != null)
      {
        args += "--keyserver " + EscapeArg(options.KeyServer.AbsoluteUri) + " --keyserver-options auto-key-retrieve ";
        if(!options.AutoFetchKeys) args += "--auto-key-locate keyserver ";
      }

      if(options.AssumeBinaryInput) args += "--no-armor ";
    }
    return args;
  }

  /// <summary>Given an array of user attributes, returns a collection of user attribute lists, where the attributes in
  /// each list are grouped by key.
  /// </summary>
  static IEnumerable<List<UserAttribute>> GroupAttributesByKey(UserAttribute[] attributes)
  {
    if(attributes == null) throw new ArgumentNullException();

    Dictionary<string, List<UserAttribute>> keyMap = new Dictionary<string, List<UserAttribute>>();
    foreach(UserAttribute attribute in attributes)
    {
      if(attribute == null) throw new ArgumentException("An attribute was null.");

      if(attribute.PrimaryKey == null || string.IsNullOrEmpty(attribute.PrimaryKey.Fingerprint))
      {
        throw new ArgumentException("An attribute did not have a key with a fingerprint.");
      }

      List<UserAttribute> list;
      if(!keyMap.TryGetValue(attribute.PrimaryKey.Fingerprint, out list))
      {
        keyMap[attribute.PrimaryKey.Fingerprint] = list = new List<UserAttribute>();
      }

      int i;
      for(i=0; i<list.Count; i++)
      {
        if(string.Equals(list[i].Id, attribute.Id, StringComparison.Ordinal)) break;
      }
      if(i == list.Count) list.Add(attribute);
    }
    return keyMap.Values;
  }

  /// <summary>Groups a list of signatures by their owning attributes, and groups the owning attributes by their
  /// owning keys.
  /// </summary>
  static void GroupSignaturesByKeyAndObject(KeySignature[] signatures,
                                            out Dictionary<string, List<UserAttribute>> uidMap,
                                            out Dictionary<string, List<KeySignature>> sigMap)
  {
    if(signatures == null) throw new ArgumentNullException();

    // we need to group the signed objects by their owning key and the signatures by the signed object
    uidMap = new Dictionary<string, List<UserAttribute>>();
    sigMap = new Dictionary<string, List<KeySignature>>();

    foreach(KeySignature sig in signatures)
    {
      if(sig == null) throw new ArgumentException("A signature was null.");

      UserAttribute signedObject = sig.Object as UserAttribute;
      if(signedObject == null) throw new NotSupportedException("Only editing signatures on attributes is supported.");

      if(signedObject.PrimaryKey == null || string.IsNullOrEmpty(signedObject.PrimaryKey.Fingerprint))
      {
        throw new ArgumentException("A signed object did not have a key with a fingerprint.");
      }

      List<UserAttribute> uidList;
      if(!uidMap.TryGetValue(signedObject.PrimaryKey.Fingerprint, out uidList))
      {
        uidMap[signedObject.PrimaryKey.Fingerprint] = uidList = new List<UserAttribute>();
      }

      int i;
      for(i=0; i<uidList.Count; i++)
      {
        if(string.Equals(signedObject.Id, uidList[i].Id, StringComparison.Ordinal)) break;
      }
      if(i == uidList.Count) uidList.Add(signedObject);

      List<KeySignature> sigList;
      if(!sigMap.TryGetValue(signedObject.Id, out sigList))
      {
        sigMap[signedObject.Id] = sigList = new List<KeySignature>();
      }
      sigList.Add(sig);
    }
  }

  /// <summary>Given an array of subkeys, returns a collection of subkey lists, where the subkeys in each list are
  /// grouped by key.
  /// </summary>
  static IEnumerable<List<Subkey>> GroupSubkeysByKey(Subkey[] subkeys)
  {
    if(subkeys == null) throw new ArgumentNullException();

    // the subkeys need to be grouped by primary key
    Dictionary<string, List<Subkey>> keyMap = new Dictionary<string, List<Subkey>>();
    foreach(Subkey subkey in subkeys)
    {
      if(subkey == null) throw new ArgumentException("A subkey was null.");
      if(subkey.PrimaryKey == null || string.IsNullOrEmpty(subkey.PrimaryKey.Fingerprint))
      {
        throw new ArgumentException("A subkey did not have a primary key with a fingerprint.");
      }

      List<Subkey> keyList;
      if(!keyMap.TryGetValue(subkey.PrimaryKey.Fingerprint, out keyList))
      {
        keyMap[subkey.PrimaryKey.Fingerprint] = keyList = new List<Subkey>();
      }
      keyList.Add(subkey);
    }

    return keyMap.Values;
  }

  /// <summary>Handles a revocation prompt, supplying the reason, explanation, and confirmation.</summary>
  static bool HandleRevokePrompt(Command cmd, string promptId, KeyRevocationReason keyReason,
                                 UserRevocationReason userReason, ref string[] lines, ref int lineIndex)
  {
    if(string.Equals(promptId, "ask_revocation_reason.text", StringComparison.Ordinal))
    {
      if(lines == null) // parse the explanation text into lines, where no line is blank
      {
        string text = userReason != null ? userReason.Explanation :
                      keyReason  != null ? keyReason.Explanation  : null;
        lines = text == null ? // remove empty lines of text
            new string[0] : text.Replace("\r", "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
      }

      if(lineIndex < lines.Length) // send the next line if there are lines left to send
      {
        cmd.SendLine(lines[lineIndex++]);
      }
      else // otherwise, send a blank line, which signifies an the end to the explanation
      {
        cmd.SendLine();
        lineIndex = 0;
      }
    }
    else if(string.Equals(promptId, "ask_revocation_reason.code", StringComparison.Ordinal))
    {
      if(userReason != null && userReason.Reason == UserRevocationCode.IdNoLongerValid)
      {
        cmd.SendLine("4");
      }
      else if(keyReason != null)
      {
        if(keyReason.Reason == KeyRevocationCode.KeyCompromised) cmd.SendLine("1");
        else if(keyReason.Reason == KeyRevocationCode.KeyRetired) cmd.SendLine("3");
        else if(keyReason.Reason == KeyRevocationCode.KeySuperceded) cmd.SendLine("2");
        else cmd.SendLine("0");
      }
      else cmd.SendLine("0");
    }
    else if(string.Equals(promptId, "ask_revocation_reason.okay", StringComparison.Ordinal))
    {
      cmd.SendLine("Y");
    }
    else return false;

    return true;
  }

  /// <summary>Returns true if the given character is a valid hex digit.</summary>
  static bool IsHexDigit(char c)
  {
    if(c >= '0' && c <= '9') return true;
    else
    {
      c = char.ToLowerInvariant(c);
      return c >= 'a' && c <= 'f';
    }
  }

  /// <summary>Determines whether the given string is a valid key fingerprint.</summary>
  static bool IsValidKeyId(string str)
  {
    if(!string.IsNullOrEmpty(str) && (str.Length == 8 || str.Length == 16))
    {
      foreach(char c in str)
      {
        if(!IsHexDigit(c)) return false;
      }
      return true;
    }
    return false;
  }

  /// <summary>Determines whether the given string is a valid key fingerprint.</summary>
  static bool IsValidFingerprint(string str)
  {
    if(!string.IsNullOrEmpty(str) && (str.Length == 32 || str.Length == 40))
    {
      foreach(char c in str)
      {
        if(!IsHexDigit(c)) return false;
      }
      return true;
    }
    return false;
  }

  /// <summary>A helper for reading key listings, that reads the data for a primary key or subkey.</summary>
  static void ReadKeyData(Key key, string[] data)
  {
    PrimaryKey primaryKey = key as PrimaryKey;

    if(!string.IsNullOrEmpty(data[1])) // read various key flags
    {
      char c = data[1][0];
      switch(c)
      {
        case 'i': key.Invalid = true; break;
        case 'd': if(key is PrimaryKey) ((PrimaryKey)key).Disabled = true; break;
        case 'r': key.Revoked = true; break;
        case 'e': key.Expired = true; break;
        case '-': case 'q': case 'n': case 'm': case 'f': case 'u':
          key.CalculatedTrust = ParseTrustLevel(c);
          break;
      }
    }

    if(!string.IsNullOrEmpty(data[2])) key.Length = int.Parse(data[2], CultureInfo.InvariantCulture);
    if(!string.IsNullOrEmpty(data[3])) key.KeyType = ParseKeyType(data[3]);
    if(!string.IsNullOrEmpty(data[4])) key.KeyId = data[4].ToUpperInvariant();
    if(!string.IsNullOrEmpty(data[5])) key.CreationTime = ParseTimestamp(data[5]);
    if(!string.IsNullOrEmpty(data[6])) key.ExpirationTime = ParseNullableTimestamp(data[6]);
    if(!string.IsNullOrEmpty(data[8]) && primaryKey != null) primaryKey.OwnerTrust = ParseTrustLevel(data[8][0]);

    if(!string.IsNullOrEmpty(data[11]))
    {
      KeyCapabilities totalCapabilities = 0;
      foreach(char c in data[11])
      {
        switch(c)
        {
          case 'e': key.Capabilities |= KeyCapabilities.Encrypt; break;
          case 's': key.Capabilities |= KeyCapabilities.Sign; break;
          case 'c': key.Capabilities |= KeyCapabilities.Certify; break;
          case 'a': key.Capabilities |= KeyCapabilities.Authenticate; break;
          case 'E': totalCapabilities |= KeyCapabilities.Encrypt; break;
          case 'S': totalCapabilities |= KeyCapabilities.Sign; break;
          case 'C': totalCapabilities |= KeyCapabilities.Certify; break;
          case 'A': totalCapabilities |= KeyCapabilities.Authenticate; break;
          case 'D': if(key is PrimaryKey) ((PrimaryKey)key).Disabled = true; break;
        }
      }

      if(primaryKey != null) primaryKey.TotalCapabilities = totalCapabilities;
    }
  }

  /// <summary>Validates and normalize a key ID.</summary>
  static string NormalizeKeyId(string id)
  {
    string newId = id;

    // strip off any 0x prefix
    if(newId != null)
    {
      newId = newId.ToUpperInvariant();
      if(newId.StartsWith("0X", StringComparison.Ordinal)) newId = newId.Substring(2);
      newId = newId.Replace(":", ""); // some fingerprints have the octets separated by colons
    }

    if(string.IsNullOrEmpty(newId)) throw new ArgumentException("The key ID was null or empty.");

    // some key ids have a leading zero for no obvious reason...
    if(newId[0] == '0' && (newId.Length == 9 || newId.Length == 17 || newId.Length == 33 || newId.Length == 41))
    {
      newId = newId.Substring(1);
    }

    bool invalid = newId.Length != 8 && newId.Length != 16 && newId.Length != 32 && newId.Length != 40;
    if(!invalid)
    {
      foreach(char c in newId)
      {
        if(!IsHexDigit(c))
        {
          invalid = true;
          break;
        }
      }
    }

    if(invalid) throw new ArithmeticException("Invalid key ID: " + id);

    return newId;
  }

  /// <summary>Normalizes a keyring filename to something that is acceptable to GPG, and that allows two normalized
  /// filenames to be compared with an ordinal comparison.
  /// </summary>
  static string NormalizeKeyringFile(string filename)
  {
    if(filename != null)
    {
      // GPG treats relative keyring and trustdb paths as being relative to the user's home directory, so we'll get the
      // full path. and it detects relative paths by searching for only one directory separator char (backslash on
      // windows), so we'll normalize those as well
      filename = Path.GetFullPath(filename).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
      // use case insensitive filenames on operating systems besides *nix
      if(Environment.OSVersion.Platform != PlatformID.Unix) filename = filename.ToLowerInvariant();
    }
    return filename;
  }

  /// <summary>Converts a character representing a trust level into the corresponding <see cref="TrustLevel"/> value.</summary>
  static TrustLevel ParseTrustLevel(char c)
  {
    switch(c)
    {
      case 'n': return TrustLevel.Never;
      case 'm': return TrustLevel.Marginal;
      case 'f': return TrustLevel.Full;
      case 'u': return TrustLevel.Ultimate;
      default:  return TrustLevel.Unknown;
    }
  }

  /// <summary>Trims a string, or returns null if the string is null.</summary>
  static string Trim(string str)
  {
    return str == null ? null : str.Trim();
  }

  /// <summary>Copys all data from the write stream to the standard input of the process, and copies the data from
  /// the standard output into the read stream.
  /// </summary>
  /// <returns>Returns true if all data was written to the standard input before the process terminated, and all the
  /// data was read from standard output.
  /// </returns>
  static bool ReadAndWriteStreams(Stream read, Stream write, Process process)
  {
    ManualResetEvent readComplete = null;
    byte[] writeBuffer = new byte[4096], readBuffer = read == null ? null : new byte[4096];
    bool allDataWritten = false, allDataRead = read == null;

    if(read != null)
    {
      readComplete = new ManualResetEvent(false);

      AsyncCallback callback = null;
      callback = delegate(IAsyncResult result)
      {
        Stream stream = process.StandardOutput.BaseStream;
        int bytes = stream == null ? 0 : stream.EndRead(result);
        if(bytes == 0)
        {
          if(stream != null) allDataRead = true;
          readComplete.Set();
        }
        else
        {
          read.Write(readBuffer, 0, bytes);
          if(stream.CanRead)
          {
            try { stream.BeginRead(readBuffer, 0, readBuffer.Length, callback, null); }
            catch(ObjectDisposedException) { }
          }
        }
      };

      Stream stdOut = process.StandardOutput.BaseStream;
      if(stdOut != null && stdOut.CanRead)
      {
        try { stdOut.BeginRead(readBuffer, 0, readBuffer.Length, callback, null); }
        catch(ObjectDisposedException) { }
      }
    }

    while(!process.HasExited)
    {
      int bytes;

      // copy a chunk of bytes from the write stream to STDIN
      bytes = write.Read(writeBuffer, 0, writeBuffer.Length);
      if(bytes == 0)
      {
        allDataWritten = true;
        break;
      }

      Stream stdIn = process.StandardInput.BaseStream;
      if(stdIn == null || !stdIn.CanWrite) break;

      try { stdIn.Write(writeBuffer, 0, bytes); }
      catch(ObjectDisposedException) { break; }
    }

    process.StandardInput.Close(); // close STDIN so that GPG will finish up

    if(read != null)
    {
      readComplete.WaitOne();
      readComplete.Close();
    }

    return allDataWritten && allDataRead;
  }

  /// <summary>Writes all data from the stream to the standard input of the process,
  /// and then closes the standard input.
  /// </summary>
  /// <returns>Returns true if all data was written to the stream before the process terminated.</returns>
  static bool WriteStreamToProcess(Stream data, Process process)
  {
    return ReadAndWriteStreams(null, data, process);
  }

  string[] ciphers, hashes, keyTypes, compressions;
  string exePath;
  /// <summary>The GPG version, encoded so that 1.4.9 becomes 10409 and 2.0.21 becomes 20021</summary>
  int gpgVersion;
  bool enableAgent, retrieveKeySignatureFingerprints;

  static readonly ReadOnlyListWrapper<UserAttribute> NoAttributes =
    new ReadOnlyListWrapper<UserAttribute>(new UserAttribute[0]);
  static readonly ReadOnlyListWrapper<string> NoRevokers = new ReadOnlyListWrapper<string>(new string[0]);
  static readonly ReadOnlyListWrapper<KeySignature> NoSignatures =
    new ReadOnlyListWrapper<KeySignature>(new KeySignature[0]);
  static readonly ReadOnlyListWrapper<Subkey> NoSubkeys = new ReadOnlyListWrapper<Subkey>(new Subkey[0]);
  static readonly Regex versionLineRe = new Regex(@"gpg .*?(\d+(\.\d+)+)", RegexOptions.Singleline);
  static readonly Regex supportLineRe = new Regex(@"^(\w+):\s*(.+)", RegexOptions.Singleline);
  static readonly Regex commaSepRe = new Regex(@",\s*", RegexOptions.Singleline);
  static readonly Regex cEscapeRe = new Regex(@"\\x[0-9a-f]{2}",
    RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);
}
#endregion

} // namespace AdamMil.Security.PGP