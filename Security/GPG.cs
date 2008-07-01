using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using AdamMil.Collections;
using AdamMil.IO;
using AdamMil.Security.PGP.GPG.StatusMessages;
using Marshal      = System.Runtime.InteropServices.Marshal;
using SecureString = System.Security.SecureString;

namespace AdamMil.Security.PGP.GPG
{

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
      case OpenPGPKeyType.DSA: return PrimaryKeyType.DSA;
      case OpenPGPKeyType.ElGamal: return SubkeyType.ElGamal;
      case OpenPGPKeyType.ElGamalEncryptOnly: return SubkeyType.ElGamalEncryptOnly;
      case OpenPGPKeyType.RSA: return PrimaryKeyType.RSA;
      case OpenPGPKeyType.RSAEncryptOnly: return SubkeyType.RSAEncryptOnly;
      case OpenPGPKeyType.RSASignOnly: return SubkeyType.RSASignOnly;
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

  /// <summary>Gets the path to the GPG executable, or null if <see cref="Initialize"/> has not been called.</summary>
  public string ExecutablePath
  {
    get { return exePath; }
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

  /// <include file="documentation.xml" path="/Security/PGPSystem/SignAndEncrypt/*"/>
  public override void SignAndEncrypt(Stream sourceData, Stream destination, SigningOptions signingOptions,
                                      EncryptionOptions encryptionOptions, OutputOptions outputOptions)
  {
    if(sourceData == null || destination == null || encryptionOptions == null && signingOptions == null)
    {
      throw new ArgumentNullException();
    }

    string args = GetOutputArgs(outputOptions);
    CommandState state = new CommandState();
    bool symmetric = false; // whether we're doing password-based encryption (possibly in addition to key-based)

    if(encryptionOptions != null) // if we'll be doing any encryption
    {
      // we can't do signing with detached signatures because GPG doesn't have a way to specify the two output files
      if(signingOptions != null && signingOptions.Detached)
      {
        throw new NotSupportedException("Simultaneous encryption and detached signing is not supported by GPG. Perform "+
                                        "the encryption and detached signing as two separate steps.");
      }

      symmetric = encryptionOptions.Password != null && encryptionOptions.Password.Length != 0;

      // we need recipients if we're not doing password-based encryption
      if(!symmetric && encryptionOptions.Recipients.Count == 0 && encryptionOptions.HiddenRecipients.Count == 0)
      {
        throw new ArgumentException("No recipients were specified.");
      }

      // add the keyrings of all the recipient keys to the command line
      List<Key> totalRecipients = new List<Key>();
      totalRecipients.AddRange(encryptionOptions.Recipients);
      totalRecipients.AddRange(encryptionOptions.HiddenRecipients);
      args += GetKeyringArgs(totalRecipients, true, false, false);

      // if there are recipients for key-based encryption, add them to the command line
      if(totalRecipients.Count != 0)
      {
        foreach(Key key in encryptionOptions.Recipients) args += "-r " + key.Fingerprint + " ";
        foreach(Key key in encryptionOptions.HiddenRecipients) args += "-R " + key.Fingerprint + " ";
        args += "-e "; // and add the key-based encryption command
      }

      if(!string.IsNullOrEmpty(encryptionOptions.Cipher))
      {
        AssertSupported(encryptionOptions.Cipher, ciphers, "cipher");
        args += "--cipher-algo " + EscapeArg(encryptionOptions.Cipher) + " ";
        state.FailureReasons |= FailureReason.UnsupportedAlgorithm; // an unsupported cipher may cause a failure
      }

      if(symmetric) args += "-c "; // add the password-based encryption command if necessary

      if(encryptionOptions.AlwaysTrustRecipients) args += "--trust-model always ";
    }

    if(signingOptions != null) // if we'll be doing any signing
    {
      if(signingOptions.Signers.Count == 0) throw new ArgumentException("No signers were specified.");

      // add the keyrings of the signers to the command prompt
      args += GetKeyringArgs(signingOptions.Signers, true, true, false);

      if(!string.IsNullOrEmpty(signingOptions.Hash))
      {
        AssertSupported(encryptionOptions.Cipher, hashes, "hash");
        args += "--digest-algo "+EscapeArg(signingOptions.Hash)+" ";
        state.FailureReasons |= FailureReason.UnsupportedAlgorithm; // an unsupported hash may cause a failure
      }

      // add all of the signers to the command line
      foreach(Key key in signingOptions.Signers) args += "-u " + key.Fingerprint + " ";

      // and add the signing command (either detached or not)
      args += signingOptions.Detached ? "-b " : "-s ";
    }

    Command cmd = Execute(args, true, false, StreamHandling.Unprocessed, StreamHandling.ProcessText);
    using(ManualResetEvent ready = new ManualResetEvent(false)) // create an event to signal when the data
    using(cmd)                                                  // should be sent
    {
      cmd.StatusMessageReceived += delegate(StatusMessage msg)
      {
        switch(msg.Type)
        {
          case StatusMessageType.NeedCipherPassphrase:
            cmd.SendPassword(encryptionOptions.Password, false);
            break;

          case StatusMessageType.NeedKeyPassphrase:
            SendKeyPassword(cmd, state.PasswordHint, (NeedKeyPassphraseMessage)msg, true);
            break;

          case StatusMessageType.BeginEncryption: case StatusMessageType.BeginSigning:
            ready.Set();
            break;

          case StatusMessageType.GetHidden: case StatusMessageType.GetBool: case StatusMessageType.GetLine:
          {
            GetInputMessage m = (GetInputMessage)msg;

            if(string.Equals(m.PromptId, "passphrase.enter", StringComparison.Ordinal))
            {
              // this is handled by the NEED_PASSPHRASE* messages above
            }
            else if(string.Equals(m.PromptId, "untrusted_key.override", StringComparison.Ordinal))
            { // this question indicates that a recipient key is not trusted
              bool alwaysTrust = encryptionOptions != null && encryptionOptions.AlwaysTrustRecipients;
              if(!alwaysTrust) state.FailureReasons |= FailureReason.UntrustedRecipient;
              cmd.SendLine(alwaysTrust ? "Y" : "N");
            }
            else goto default;
            break;
          }

          default: DefaultStatusMessageHandler(msg, ref state); break;
        }
      };

      cmd.Start();

      // wait until it's time to write the data or the process aborted
      while(!ready.WaitOne(50, false) && !cmd.Process.HasExited) { }

      // if the process is still running and it didn't exit before we could copy the input data...
      if(!cmd.Process.HasExited && WriteStreamToProcess(sourceData, cmd.Process))
      {
        IOH.CopyStream(cmd.Process.StandardOutput.BaseStream, destination); // then read the output data
      }

      cmd.WaitForExit(); // and wait for the command to finish
    }

    if(!cmd.SuccessfulExit) // if the process wasn't successful, throw an exception
    {
      if(encryptionOptions != null) throw new EncryptionFailedException(state.FailureReasons);
      else throw new SigningFailedException(state.FailureReasons);
    }
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/Decrypt/*"/>
  public override Signature[] Decrypt(Stream ciphertext, Stream destination, DecryptionOptions options)
  {
    if(ciphertext == null || destination == null) throw new ArgumentNullException();

    Command cmd = Execute(GetVerificationArgs(options, true) + "-d", true, false,
                          StreamHandling.Unprocessed, StreamHandling.ProcessText);
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
  public override Signature[] Verify(Stream signature, Stream signedData, VerificationOptions options)
  {
    if(signature == null || signedData == null) throw new ArgumentNullException();

    // copy the signature into a temporary file, because we can't pass both streams on standard input
    string sigFileName = Path.GetTempFileName();
    try
    {
      using(FileStream file = new FileStream(sigFileName, FileMode.Truncate, FileAccess.Write))
      {
        IOH.CopyStream(signature, file);
      }

      return VerifyCore(sigFileName, signedData, options);
    }
    finally { File.Delete(sigFileName); }
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/FindPublicKeys/*"/>
  public override PrimaryKey[] FindPublicKeys(string[] fingerprints, KeySignatures signatures,
                                              Keyring[] keyrings, bool includeDefaultKeyring)
  {
    return FindKeys(fingerprints, signatures, keyrings, includeDefaultKeyring, false);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/FindSecretKeys/*"/>
  public override PrimaryKey[] FindSecretKeys(string[] fingerprints, KeySignatures signatures,
                                              Keyring[] keyrings, bool includeDefaultKeyring)
  {
    return FindKeys(fingerprints, signatures, keyrings, includeDefaultKeyring, true);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetPublicKeys2/*"/>
  public override PrimaryKey[] GetPublicKeys(KeySignatures signatures, Keyring[] keyrings, bool includeDefaultKeyring)
  {
    return GetKeys(signatures, keyrings, includeDefaultKeyring, false, null);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetSecretKeys2/*"/>
  public override PrimaryKey[] GetSecretKeys(Keyring[] keyrings, bool includeDefaultKeyring)
  {
    return GetKeys(KeySignatures.Ignore, keyrings, includeDefaultKeyring, true, null);
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
                        string.Equals(options.KeyType, PrimaryKeyType.DSA, StringComparison.OrdinalIgnoreCase);
    bool primaryIsRSA = string.Equals(options.KeyType, PrimaryKeyType.RSA, StringComparison.OrdinalIgnoreCase);

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
        options.KeyLength.ToString(CultureInfo.InvariantCulture) + " is not supported.");
    }

    bool subIsDSA = string.Equals(options.SubkeyType, SubkeyType.DSA, StringComparison.OrdinalIgnoreCase);
    bool subIsELG = string.IsNullOrEmpty(options.SubkeyType) || // ElGamal is the default subkey type
                    string.Equals(options.SubkeyType, SubkeyType.ElGamal, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(options.SubkeyType, SubkeyType.ElGamalEncryptOnly, StringComparison.OrdinalIgnoreCase);
    bool subIsRSAS = string.Equals(options.SubkeyType, SubkeyType.RSAEncryptOnly, StringComparison.OrdinalIgnoreCase);
    bool subIsRSAE = string.Equals(options.SubkeyType, SubkeyType.RSASignOnly, StringComparison.OrdinalIgnoreCase);
    bool subIsNone = string.Equals(options.SubkeyType, SubkeyType.None, StringComparison.OrdinalIgnoreCase);

    if(!subIsNone && !subIsDSA && !subIsELG && !subIsRSAS && !subIsRSAE)
    {
      throw new KeyCreationFailedException(FailureReason.UnsupportedAlgorithm,
                                           "Subkey type "+options.SubkeyType+" is not supported.");
    }

    if(!subIsNone) // if a subkey will be created
    {
      // GPG supports key sizes from 1024 to 3072 (for DSA keys) or 4096 (for other keys)
      maxKeyLength = subIsDSA ? 3072 : 4096;
      if(options.SubkeyLength != 0 && (options.SubkeyLength < 1024 || options.SubkeyLength > maxKeyLength))
      {
        throw new KeyCreationFailedException(FailureReason.None, "Key length "+
          options.SubkeyLength.ToString(CultureInfo.InvariantCulture) + " is not supported.");
      }
    }

    int expirationDays = 0;
    if(options.Expiration.HasValue)
    {
      DateTime expiration = options.Expiration.Value.ToUniversalTime(); // the date should be in UTC
      
      // give us 30 seconds of fudge time so the key doesn't expire between now and when we run GPG
      if(expiration <= DateTime.UtcNow.AddSeconds(30))
      {
        throw new ArgumentException("The key expiration date must be in the future.");
      }

      // GPG supports expiration dates in two formats: absolute dates and times relative to the current time.
      // but it only supports absolute dates up to 2038, so we have to use a relative time format (days from now)
      expirationDays = (int)Math.Ceiling((expiration - DateTime.UtcNow.Date).TotalDays);
    }

    // the options look good, so lets make the key
    string keyFingerprint = null;
    CommandState state = new CommandState();

    string args = GetKeyringArgs(options.Keyring, true, true, true);

    // if we're using DSA keys greater than 1024 bits, we need to enable DSA2 support
    if(primaryIsDSA && options.KeyLength > 1024 || subIsDSA && options.SubkeyLength > 1024) args += "--enable-dsa2 ";
    
    // if there's a keyring and we use --keyring and --secret-keyring, GPG will add the key to the trust database, but
    // during later commands that use the default keyring, it will bug out because it won't be able to find the new key
    // (which is referenced in the trust database), because it's not in the default keyring. using %pubring and
    // %secring in the batch gen-key commands causes it to add the key to the trust database, but those commands
    // clobber the keyring before the keys are created. so what we'll do is create a new trust database in a temp file
    // and then delete it later. this way, the default trust database and the existing keyring keys are not affected.
    string trustDbName = null;
    if(options.Keyring != null)
    {
      trustDbName = Path.GetTempFileName(); // create an empty file for the database

      // unfortunately, GPG will barf if the trust database is an empty file, so we need to build a valid trust
      // database. the following creates a valid, empty version 3 trust database. (see gpg-src\doc\DETAILS)
      using(FileStream dbFile = File.Open(trustDbName, FileMode.Open, FileAccess.Write))
      {
        dbFile.SetLength(40); // the database is 40 bytes long, but only the first 16 bytes are non-zero

        byte[] headerStart = new byte[] { 1, 0x67, 0x70, 0x67, 3, 3, 1, 5, 1, 0, 0, 0 };
        dbFile.Write(headerStart, 0, headerStart.Length);

        // the next four bytes are the big-endian creation timestamp in seconds since epoch. we'll pretend it was
        // created 60 seconds ago
        IOH.WriteBE4(dbFile,
          (int)((DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds - 60));
      }

      // we won't waste time checking the temporary trust database
      args += "--no-auto-check-trustdb --trustdb-name " + EscapeArg(trustDbName) + " ";
    }

    Command cmd = Execute(args + "--batch --gen-key", true, false,
                          StreamHandling.ProcessText, StreamHandling.ProcessText);
    try
    {
      cmd.StandardErrorLine += delegate(string line) { DefaultStandardErrorHandler(line, ref state); };

      cmd.StatusMessageReceived += delegate(StatusMessage msg)
      {
        if(msg.Type == StatusMessageType.KeyCreated) // when the key is created, grab its fingerprint
        {
          KeyCreatedMessage m = (KeyCreatedMessage)msg;
          if(m.PrimaryKeyCreated) keyFingerprint = m.Fingerprint;
        }
        else DefaultStatusMessageHandler(msg, ref state);
      };

      cmd.Start();

      cmd.Process.StandardInput.WriteLine("Key-Type: " + (primaryIsDSA ? "DSA" : "RSA"));
      if(options.KeyLength != 0)
      {
        cmd.Process.StandardInput.WriteLine("Key-Length: " + options.KeyLength.ToString(CultureInfo.InvariantCulture));
      }

      if(!subIsNone)
      {
        cmd.Process.StandardInput.WriteLine("Subkey-Type: " +
          (subIsDSA ? "DSA" : subIsELG ? "ELG-E" : subIsRSAE ? "RSA-E" : "RSA-S"));
        if(options.SubkeyLength != 0)
        {
          cmd.Process.StandardInput.WriteLine("Subkey-Length: " +
                                              options.SubkeyLength.ToString(CultureInfo.InvariantCulture));
        }
      }

      if(!string.IsNullOrEmpty(realName)) cmd.Process.StandardInput.WriteLine("Name-Real: " + realName);
      if(!string.IsNullOrEmpty(email)) cmd.Process.StandardInput.WriteLine("Name-Email: " + email);
      if(!string.IsNullOrEmpty(comment)) cmd.Process.StandardInput.WriteLine("Name-Comment: " + comment);

      if(options.Password != null && options.Password.Length != 0)
      {
        cmd.Process.StandardInput.Write("Passphrase: ");

        // treat the password as securely as we can by ensuring that it doesn't stick around in memory any longer than
        // necessary
        IntPtr bstr  = IntPtr.Zero;
        char[] chars = new char[options.Password.Length];
        try
        {
          bstr = Marshal.SecureStringToBSTR(options.Password);
          Marshal.Copy(bstr, chars, 0, chars.Length);
          cmd.Process.StandardInput.WriteLine(chars);
        }
        finally
        {
          if(bstr != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr);
          ZeroBuffer(chars);
        }
      }

      if(options.Expiration.HasValue)
      {
        cmd.Process.StandardInput.WriteLine("Expire-Date: " + 
                                            expirationDays.ToString(CultureInfo.InvariantCulture) + "d");
      }

      cmd.Process.StandardInput.Close(); // close STDIN so GPG can start generating the key
      cmd.WaitForExit(); // wait for it to finish
    }
    finally
    {
      cmd.Dispose();
      if(trustDbName != null) File.Delete(trustDbName);
    }

    if(!cmd.SuccessfulExit || keyFingerprint == null) throw new KeyCreationFailedException(state.FailureReasons);

    // return the new PrimaryKey
    return FindPublicKey(keyFingerprint, KeySignatures.Ignore, options.Keyring);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/DeleteKeys/*"/>
  public override void DeleteKeys(Key[] keys, KeyDeletion deletion)
  {
    if(keys == null) throw new ArgumentNullException();

    // deleting subkeys is done via --edit-key, which we don't support yet.
    // TODO: implement subkey deletion
    foreach(Key key in keys)
    {
      if(!(key is PrimaryKey)) throw new NotImplementedException("Deleting subkeys is not yet supported.");
    }

    string args = GetKeyringArgs(keys, true, deletion == KeyDeletion.PublicAndSecret, true);
    args += (deletion == KeyDeletion.Secret ? "--delete-secret-key " : "--delete-secret-and-public-key ");

    foreach(Key key in keys) // add the fingerprints of the keys to delete
    {
      if(key is PrimaryKey) args += key.Fingerprint + " ";
    }

    CommandState state = new CommandState();
    Command cmd = Execute(args, true, true, StreamHandling.ProcessText, StreamHandling.ProcessText);
    using(cmd)
    {
      cmd.StandardErrorLine += delegate(string line) { DefaultStandardErrorHandler(line, ref state); };

      cmd.StatusMessageReceived += delegate(StatusMessage msg)
      {
        switch(msg.Type)
        {
          case StatusMessageType.GetBool: case StatusMessageType.GetHidden: case StatusMessageType.GetLine: 
          {
            GetInputMessage m = (GetInputMessage)msg;
            if(string.Equals(m.PromptId, "delete_key.okay", StringComparison.Ordinal) ||
               string.Equals(m.PromptId, "delete_key.secret.okay", StringComparison.Ordinal))
            {
              cmd.SendLine("Y");
            }
            else goto default;
            break;
          }

          default: DefaultStatusMessageHandler(msg, ref state); break;
        }
      };

      cmd.Start();
      cmd.WaitForExit();
    }

    if(!cmd.SuccessfulExit) throw new KeyEditFailedException(state.FailureReasons);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ExportPublicKeys/*"/>
  public override void ExportPublicKeys(PrimaryKey[] keys, Stream destination, ExportOptions exportOptions,
                                        OutputOptions outputOptions)
  {
    ExportKeys(keys, destination, exportOptions, outputOptions, false);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ExportPublicKeys2/*"/>
  public override void ExportPublicKeys(Keyring[] keyrings, bool includeDefaultKeyring, Stream destination,
                                        ExportOptions exportOptions, OutputOptions outputOptions)
  {
    ExportKeyrings(keyrings, includeDefaultKeyring, destination, exportOptions, outputOptions, false);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ExportSecretKeys/*"/>
  public override void ExportSecretKeys(PrimaryKey[] keys, Stream destination, ExportOptions exportOptions, OutputOptions outputOptions)
  {
    ExportKeys(keys, destination, exportOptions, outputOptions, true);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ExportSecretKeys2/*"/>
  public override void ExportSecretKeys(Keyring[] keyrings, bool includeDefaultKeyring, Stream destination,
                                        ExportOptions exportOptions, OutputOptions outputOptions)
  {
    ExportKeyrings(keyrings, includeDefaultKeyring, destination, exportOptions, outputOptions, true);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ImportKeys3/*"/>
  public override ImportedKey[] ImportKeys(Stream source, Keyring keyring, ImportOptions options)
  {
    if(source == null) throw new ArgumentNullException();

    string args = GetKeyringArgs(keyring, true, true, true);

    if(keyring != null)
    {
      // create the keyring files if they don't exist. GPG will refuse to use them otherwise.
      if(!File.Exists(keyring.PublicFile)) File.Create(keyring.PublicFile).Dispose();

      if(!string.IsNullOrEmpty(keyring.SecretFile) && !File.Exists(keyring.SecretFile))
      {
        File.Create(keyring.SecretFile).Dispose();
      }

      // add the --primary-keyring option so that GPG will import into the keyrings we've given it
      args += "--primary-keyring " + EscapeArg(NormalizeKeyringFile(keyring.PublicFile)) + " ";
    }

    if(options != ImportOptions.Default)
    {
      args += "--import-options ";
      if((options & ImportOptions.CleanKeys) != 0) args += "import-clean ";
      if((options & ImportOptions.ImportLocalSignatures) != 0) args += "import-local-sigs ";
      if((options & ImportOptions.MergeOnly) != 0) args += "merge-only ";
      if((options & ImportOptions.MinimizeKeys) != 0) args += "import-minimize ";
    }

    // GPG sometimes sends multiple messages for a single key, for instance when the key has several subkeys or a
    // secret portion. so we'll keep track of how fingerprints map to ImportedKey objects, so we'll know whether to
    // modify the existing object or create a new one
    Dictionary<string, ImportedKey> keysByFingerprint = new Dictionary<string, ImportedKey>();
    // we want to return keys in the order they were processed, so we'll keep this ordered list of fingerprints
    List<string> fingerprintsSeen = new List<string>();

    CommandState state = new CommandState();
    Command cmd = Execute(args + "--import", true, false, StreamHandling.ProcessText, StreamHandling.ProcessText);
    using(cmd)
    {
      cmd.StandardErrorLine += delegate(string line) { DefaultStandardErrorHandler(line, ref state); };

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
        else DefaultStatusMessageHandler(msg, ref state);
      };

      cmd.Start();
      WriteStreamToProcess(source, cmd.Process);
      cmd.WaitForExit();
    }

    if(!cmd.SuccessfulExit) throw new ImportFailedException(state.FailureReasons);

    // return the keys in the order that they were seen
    ImportedKey[] keysProcessed = new ImportedKey[fingerprintsSeen.Count];
    for(int i=0; i<keysProcessed.Length; i++)
    {
      keysProcessed[i] = keysByFingerprint[fingerprintsSeen[i]];
      keysProcessed[i].MakeReadOnly();
    }
    return keysProcessed;
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GenerateRandomData/*"/>
  public override void GetRandomData(Randomness quality, byte[] buffer, int index, int count)
  {
    if(buffer == null) throw new ArgumentNullException();
    if(index < 0 || count < 0 || index+count > buffer.Length) throw new ArgumentOutOfRangeException();
    if(count == 0) return;

    // "gpg --gen-random QUALITY COUNT" writes random COUNT bytes to standard output. QUALITY is a value from 0 to 2
    // representing the quality of the random number generator to use
    string qualityArg;
    if(quality == Randomness.Weak) qualityArg = "0";
    else if(quality == Randomness.TooStrong) qualityArg = "2";
    else qualityArg = "1"; // we'll default to the Strong level

    Command cmd = Execute("--gen-random " + qualityArg + " " + count.ToString(CultureInfo.InvariantCulture),
                          false, true, StreamHandling.Unprocessed, StreamHandling.ProcessText);
    using(cmd)
    {
      cmd.Start();
      do
      {
        int read = cmd.Process.StandardOutput.BaseStream.Read(buffer, index, count);
        if(read == 0) break;
        index += read;
        count -= read;
      } while(count != 0);

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
    if(hashAlgorithm == null || hashAlgorithm == HashAlgorithm.Default) hashAlgorithm = HashAlgorithm.SHA1;
    else if(hashAlgorithm.Length == 0) throw new ArgumentException("Unspecified hash algorithm.");
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
    Command cmd = Execute("--print-md " + EscapeArg(hashAlgorithm), false, false,
                          StreamHandling.Unprocessed, StreamHandling.ProcessText);
    using(cmd)
    {
      cmd.Start();

      if(WriteStreamToProcess(data, cmd.Process))
      {
        while(true)
        {
          string line = cmd.Process.StandardOutput.ReadLine();
          if(line == null) break;

          int value = 0, chars = 0;
          foreach(char c in line.ToLowerInvariant())
          {
            if(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')
            {
              value = (value<<4) + GetHexValue(c);
              if(++chars == 2)
              {
                hash.Add((byte)value);
                chars = value = 0;
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

    while(true)
    {
      string line = process.StandardOutput.ReadLine();
      if(line == null) break;

      Match match = versionLineRe.Match(line);
      if(match.Success)
      {
        string key = match.Groups[1].Value.ToLowerInvariant();
        string[] list = commaSepRe.Split(match.Groups[2].Value);
        if(string.Equals(key, "pubkey", StringComparison.Ordinal)) keyTypes = list;
        else if(string.Equals(key, "cipher", StringComparison.Ordinal)) ciphers = list;
        else if(string.Equals(key, "hash", StringComparison.Ordinal)) hashes = list;
        else if(string.Equals(key, "compression", StringComparison.Ordinal)) compressions = list;
      }
    }

    if(Exit(process) != 0) throw new PGPException("GPG returned an error while running --version.");

    this.exePath = info.FullName; // everything seems okay, so set the full exePath
  }

  /// <summary>Determines how a process' stream will be handled.</summary>
  enum StreamHandling
  {
    /// <summary>The stream will not be processed by the <see cref="Command"/> object.</summary>
    Unprocessed,
    /// <summary>The stream will be closed immediately.</summary>
    Close,
    /// <summary>The stream will be read as GPG status lines (%XX encoded UTF-8 text) and the lines will be processed
    /// by the <see cref="Command"/>.
    /// </summary>
    ProcessStatus,
    /// <summary>The stream will be read as UTF-8 text and the lines will be processed by the
    /// <see cref="Command"/>.
    /// </summary>
    ProcessText,
    /// <summary>The stream will be read as binary and the data will be thrown away. This simply prevents the client
    /// process from blocking due to a full buffer.
    /// </summary>
    DumpBinary
  }

  /// <summary>Processes status messages from GPG.</summary>
  delegate void StatusMessageHandler(StatusMessage message);
  /// <summary>Processes text output from GPG.</summary>
  delegate void TextLineHandler(string line);

  /// <summary>Holds variables set by the default STDERR and status message handlers.</summary>
  struct CommandState
  {
    /// <summary>The hint for the next password to be requested.</summary>
    public string PasswordHint;
    /// <summary>Some potential causes of a failure.</summary>
    public FailureReason FailureReasons;
  }

  #region Command
  /// <summary>Represents a GPG command.</summary>
  sealed class Command : IDisposable
  {
    public Command(ProcessStartInfo psi, InheritablePipe statusPipe,
                   bool closeStdInput, StreamHandling stdOut, StreamHandling stdError)
    {
      if(psi == null) throw new ArgumentNullException();
      this.psi           = psi;
      this.statusPipe    = statusPipe;
      this.closeStdInput = closeStdInput;
      this.outHandling   = stdOut;
      this.errorHandling = stdError;
    }

    ~Command() { Dispose(true); }

    /// <summary>Called for each line of text from STDERR when using <see cref="StreamHandling.ProcessText"/>.</summary>
    public event TextLineHandler StandardErrorLine;
    /// <summary>Called for each line of text from STDOUT when using <see cref="StreamHandling.ProcessText"/>.</summary>
    public event TextLineHandler StandardOutputLine;
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

    /// <summary>Returns true if the process has exited and the remaining data has been read from all streams.</summary>
    public bool IsDone
    {
      get { return outDone && errorDone && statusDone && process.HasExited; }
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
        throw new PGPException("GPG returned failure code "+ExitCode.ToString(CultureInfo.InvariantCulture));
      }
    }

    /// <summary>Exits the process if it's running and frees system resources used by the <see cref="Command"/> object.</summary>
    public void Dispose()
    {
      GC.SuppressFinalize(this);
      Dispose(false);
    }

    /// <summary>Sends a blank line on the command stream.</summary>
    public void SendLine()
    {
      SendLine(string.Empty);
    }

    /// <summary>Sends the given line on the command stream. The line should not include any end-of-line characters.</summary>
    public void SendLine(string line)
    {
      if(line == null) throw new ArgumentNullException();
      if(statusStream == null) throw new InvalidOperationException("The command stream is not open.");
      byte[] bytes = Encoding.UTF8.GetBytes(line);
      statusStream.Write(bytes, 0, bytes.Length);
      statusStream.WriteByte((byte)'\n');
    }

    /// <summary>Sends the given password on the command stream. If <paramref name="ownsPassword"/> is true, the
    /// password will be disposed.
    /// </summary>
    public void SendPassword(SecureString password, bool ownsPassword)
    {
      IntPtr bstr  = IntPtr.Zero;
      char[] chars = new char[password.Length+1];
      byte[] bytes = null;
      try
      {
        if(statusStream == null) throw new InvalidOperationException("The command stream is not open.");
        bstr = Marshal.SecureStringToBSTR(password);
        Marshal.Copy(bstr, chars, 0, chars.Length);
        chars[password.Length] = '\n'; // the password must be EOL-terminated for GPG to accept it
        bytes = Encoding.UTF8.GetBytes(chars);
        statusStream.Write(bytes, 0, bytes.Length);
      }
      finally
      {
        if(ownsPassword) password.Dispose();
        if(bstr != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr);
        ZeroBuffer(chars);
        ZeroBuffer(bytes);
      }
    }

    /// <summary>Starts executing the command.</summary>
    public void Start()
    {
      if(process != null) throw new InvalidOperationException("The process has already been started.");

      process = Process.Start(psi);

      if(closeStdInput) process.StandardInput.Close();

      // if we have a status pipe, set up a stream to read-write it
      if(statusPipe != null)
      {
        statusStream = new FileStream(new SafeFileHandle(statusPipe.ServerHandle, false), FileAccess.ReadWrite);
        statusBuffer = new byte[4096];
        OnStatusRead(null); // start reading on a background thread
      }
      else statusDone = true;

      if(outHandling == StreamHandling.Close || outHandling == StreamHandling.Unprocessed)
      {
        if(outHandling == StreamHandling.Close) process.StandardOutput.Close();
        outDone = true;
      }
      else
      {
        outBuffer = new byte[4096];
        OnStdOutRead(null); // start reading on a background thread
      }

      if(errorHandling == StreamHandling.Close || errorHandling == StreamHandling.Unprocessed)
      {
        if(errorHandling == StreamHandling.Close) process.StandardError.Close();
        errorDone = true;
      }
      else
      {
        errorBuffer = new byte[4096];
        OnStdErrorRead(null); // start reading on a background thread
      }
    }

    /// <summary>Waits for the process to exit and all data to be read.</summary>
    public void WaitForExit()
    {
      Process.WaitForExit(); // first wait for the process to finish

      bool closedPipe = statusPipe == null;
      while(!IsDone) // then wait for all of the streams to finish being read
      {
        if(!closedPipe) // if GPG didn't close its end of the pipe, we may have to do it
        {
          statusPipe.CloseClient();
          closedPipe = true; // but don't close it on every iteration
        }

        System.Threading.Thread.Sleep(0); // give other threads (ie, the stream reading threads) a chance to finish
      }
    }

    delegate void LineProcessor(string line);

    void Dispose(bool finalizing)
    {
      if(!disposed)
      {
        if(statusPipe != null) // if we have a status pipe, we want to close our end of it to tell GPG to exit ASAP.
        {                      // this gives GPG a chance to exit more gracefully than if we just terminated it.
          if(statusStream != null)
          {
            statusStream.Dispose();
            statusStream = null;
          }

          statusPipe.CloseServer(); // close the server side of the pipe
          if(process != null) Exit(process); // then exit the process
          statusPipe.Dispose(); // and destroy the pipe
          statusPipe = null;
        }
        else if(process != null) Exit(process); // we don't have a status pipe, so just exit the process

        // wipe the read buffers. it's unlikely that they contain sensitive data, but just in case...
        ZeroBuffer(outBuffer);
        ZeroBuffer(errorBuffer);
        ZeroBuffer(statusBuffer);

        statusDone = outDone = errorDone = disposed = true; // mark all streams read so IsDone will return true
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
            ProcessStatusStream(result, stream, ref buffer, ref bufferBytes, ref bufferDone);
          }
          else
          {
            DumpBinaryStream(result, stream, ref bufferDone);
          }
        }

        // a possible race condition with WaitForExit() (or Dispose()) may have disposed the stream, so check CanRead
        // to prevent an ObjectDisposedException if possible
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
Debugger.Log(0, "", "OUT: "+line+"\n");
      if(StandardOutputLine != null) StandardOutputLine(line);
    }

    /// <summary>Handles a line of text from STDERR.</summary>
    void OnStdErrorLine(string line)
    {
Debugger.Log(0, "", "ERR: "+line+"\n");
      if(StandardErrorLine != null) StandardErrorLine(line);
    }

    /// <summary>Handles a status message.</summary>
    void OnStatusMessage(StatusMessage message)
    {
      if(StatusMessageReceived != null) StatusMessageReceived(message);
    }

    // warning 0420 is "reference to a volatile field will not be treated as volatile". we aren't worried about this
    // because the field is only written to by the callee, not read.
    #pragma warning disable 420
    void OnStdOutRead(IAsyncResult result)
    {
      HandleStream(outHandling, process.StandardOutput.BaseStream, result, ref outBuffer, ref outBytes, ref outDone,
                   OnStdOutLine, OnStdOutRead);
    }

    void OnStdErrorRead(IAsyncResult result)
    {
      HandleStream(errorHandling, process.StandardError.BaseStream, result, ref errorBuffer, ref errorBytes,
                   ref errorDone, OnStdErrorLine, OnStdErrorRead);
    }

    void OnStatusRead(IAsyncResult result)
    {
      HandleStream(StreamHandling.ProcessStatus, statusStream, result, ref statusBuffer, ref statusBytes,
                   ref statusDone, null, OnStatusRead);
    }
    #pragma warning restore 420

    void ProcessStatusStream(IAsyncResult result, Stream stream, ref byte[] buffer, ref int bufferBytes,
                             ref bool bufferDone)
    {
      foreach(byte[] binaryLine in ProcessAsciiStream(result, stream, ref buffer, ref bufferBytes, ref bufferDone))
      {
        // GPG sends lines in UTF-8, which has been further encoded, so that certain characters become %XX. decode the
        // line and split it into arguments
        string type;
        string[] arguments;
        SplitDecodedLine(binaryLine, Decode(binaryLine), out type, out arguments);

        if(type != null) // if the line decoded properly and has a message type, parse and handle the message
        {
          StatusMessage message = ParseStatusMessage(type, arguments);
          if(message != null) OnStatusMessage(message);
        }
      }
    }

    Process process;
    InheritablePipe statusPipe;
    FileStream statusStream;
    byte[] statusBuffer, outBuffer, errorBuffer;
    ProcessStartInfo psi;
    int statusBytes, outBytes, errorBytes;
    StreamHandling outHandling, errorHandling;
    volatile bool statusDone, outDone, errorDone;
    bool closeStdInput, disposed;

    /// <summary>Decodes %XX-encoded values in ASCII text (represented as a byte array).</summary>
    /// <returns>Returns the new length of the text (the text is decoded in place, and can get shorter).</returns>
    static int Decode(byte[] encoded)
    {
      int newLength = encoded.Length, index = -1;

      while(true)
      {
        index = Array.IndexOf(encoded, (byte)'%', index+1); // find the next percent sign
        if(index == -1) break;

        if(index < encoded.Length-2) // if there's enough space for two hex digits after the percent sign
        {
          byte high = encoded[index+1], low = encoded[index+2];
          encoded[index] = (byte)GetHexValue((char)high, (char)low); // convert the hex value to the new byte value
          index     += 2; // skip over two of the three digits. the third will be skipped on the next iteration
          newLength -= 2;
        }
      }

      return newLength;
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

    /// <summary>Parses a status message with the given type and arguments, and returns the corresponding
    /// <see cref="StatusMessage"/>, or null if the message could not be parsed or was ignored.
    /// </summary>
    static StatusMessage ParseStatusMessage(string type, string[] arguments)
    {
Debugger.Log(0, "GPG", type+" "+string.Join(" ", arguments)+"\n"); // TODO: remove this

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
        case "GOODMDC": message = new GenericMessage(StatusMessageType.GoodMDC); break;

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

        case "GET_HIDDEN": message = new GetInputMessage(StatusMessageType.GetHidden, arguments); break;
        case "GET_BOOL": message = new GetInputMessage(StatusMessageType.GetBool, arguments); break;
        case "GET_LINE": message = new GetInputMessage(StatusMessageType.GetLine, arguments); break;

        case "DELETE_PROBLEM": message = new DeleteFailedMessage(arguments); break;

        case "KEY_CREATED": message = new KeyCreatedMessage(arguments); break;
        case "KEY_NOT_CREATED": message = new GenericMessage(StatusMessageType.KeyNotCreated); break;

        // ignore these messages
        case "PLAINTEXT": case "PLAINTEXT_LENGTH": case "SIG_ID": case "GOT_IT": case "PROGRESS":
          message = null;
          break;

        default: message = null; break; // TODO: remove later, or replace with logging?
      }
      return message;
    }

    /// <summary>Splits a decoded ASCII line representing a status message into a message type and message arguments.</summary>
    static void SplitDecodedLine(byte[] line, int length, out string type, out string[] arguments)
    {
      List<string> chunks = new List<string>();

      // the chunks are whitespace-separated
      for(int index=0; ; )
      {
        while(index < length && line[index] == (byte)' ') index++; // find the next non-whitespace character
        int start = index;
        while(index < length && line[index] != (byte)' ') index++; // find the next whitespace character after that

        if(start == length) break; // if we're at the end of the line, we're done

        chunks.Add(Encoding.UTF8.GetString(line, start, index-start)); // grab the text between the two
      }

      if(chunks.Count < 2) // if there are not enough chunks to parse a message out of it, return null
      {
        type      = null;
        arguments = null;
      }
      else // otherwise, there are enough chunks
      {
        type      = chunks[1]; // skip the first chunk, which is assumed to be "[GNUPG:]". the second becomes the type
        arguments = new string[chunks.Count-2]; // grab the rest as the arguments
        chunks.CopyTo(2, arguments, 0, arguments.Length);
      }
    }
  }
  #endregion

  /// <summary>Throws an exception if <see cref="Initialize"/> has not yet been called.</summary>
  void AssertInitialized()
  {
    if(ExecutablePath == null) throw new InvalidOperationException("ExecutablePath is not set.");
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
    CommandState state = new CommandState();
    using(cmd)
    {
      List<Signature> signatures = new List<Signature>(); // this holds the completed signatures
      Signature sig = new Signature(); // keep track of the current signature
      bool sigFilled = false, triedPasswordInOptions = false;

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
              sig.KeyId    = bad.KeyId;
              sig.UserName = bad.UserName;
              sig.Status   = SignatureStatus.Invalid;
              sigFilled    = true;
              break;
            }
            
            case StatusMessageType.ErrorSig:
            {
              ErrorSigMessage error = (ErrorSigMessage)msg;
              sig.HashAlgorithm = error.HashAlgorithm;
              sig.KeyId     = error.KeyId;
              sig.KeyType   = error.KeyType;
              sig.Timestamp = error.Timestamp;
              sig.Status    = SignatureStatus.Error | (error.MissingKey ? SignatureStatus.MissingKey : 0) |
                              (error.UnsupportedAlgorithm ? SignatureStatus.UnsupportedAlgorithm : 0);
              sigFilled     = true;
              break;
            }
            
            case StatusMessageType.ExpiredKeySig:
            {
              ExpiredKeySigMessage em = (ExpiredKeySigMessage)msg;
              sig.KeyId    = em.KeyId;
              sig.UserName = em.UserName;
              sig.Status  |= SignatureStatus.ExpiredKey;
              break;
            }
            
            case StatusMessageType.ExpiredSig:
            {
              ExpiredSigMessage em = (ExpiredSigMessage)msg;
              sig.KeyId    = em.KeyId;
              sig.UserName = em.UserName;
              sig.Status  |= SignatureStatus.ExpiredSignature;
              break;
            }
            
            case StatusMessageType.GoodSig:
            {
              GoodSigMessage good = (GoodSigMessage)msg;
              sig.KeyId    = good.KeyId;
              sig.UserName = good.UserName;
              sig.Status   = SignatureStatus.Valid | (sig.Status & SignatureStatus.ValidFlagMask);
              sigFilled    = true;
              break;
            }
            
            case StatusMessageType.RevokedKeySig:
            {
              RevokedKeySigMessage em = (RevokedKeySigMessage)msg;
              sig.KeyId    = em.KeyId;
              sig.UserName = em.UserName;
              sig.Status  |= SignatureStatus.RevokedKey;
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
              sig.Timestamp             = valid.SignatureTime;
              break;
            }
            
            case StatusMessageType.NeedKeyPassphrase:
            {
              SendKeyPassword(cmd, state.PasswordHint, (NeedKeyPassphraseMessage)msg, false);
              break;
            }

            case StatusMessageType.NeedCipherPassphrase:
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
                SecureString password = GetCipherPassword();
                if(password != null) cmd.SendPassword(password, true);
                else cmd.SendLine();
              }
              break;
            }

            case StatusMessageType.GetHidden:
            {
              GetInputMessage m = (GetInputMessage)msg;
              if(!string.Equals(m.PromptId, "passphrase.enter")) goto default; // password entry is handled above
              break;
            }

            default: DefaultStatusMessageHandler(msg, ref state); break;
          }
        }
      };

      cmd.Start();

      if(WriteStreamToProcess(signedData, cmd.Process)) // write the signed and/or encrypted data to STDIN
      {
        // if we're decrypting, read the plaintext from STDOUT
        if(destination != null) IOH.CopyStream(cmd.Process.StandardOutput.BaseStream, destination);
      }

      cmd.WaitForExit();
      if(!cmd.SuccessfulExit) throw new DecryptionFailedException(state.FailureReasons);

      if(sigFilled) signatures.Add(sig); // add the final signature if it's filled out
      // make all the signature objects read only and return them
      foreach(Signature signature in signatures) signature.MakeReadOnly();
      return signatures.ToArray();
    }
  }

  /// <summary>Provides default handling for status messages.</summary>
  void DefaultStatusMessageHandler(StatusMessage msg, ref CommandState state)
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
        OnInvalidPassword(((BadPassphraseMessage)msg).KeyId);
        state.FailureReasons |= FailureReason.BadPassword;
        break;

      case StatusMessageType.NoPublicKey: state.FailureReasons |= FailureReason.MissingPublicKey; break;
      case StatusMessageType.NoSecretKey: state.FailureReasons |= FailureReason.MissingSecretKey; break;

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
        throw new NotImplementedException("GPG requested unknown user input: " + ((GetInputMessage)msg).PromptId);
    }
  }

  /// <summary>Creates a new <see cref="Command"/> object and returns it.</summary>
  /// <param name="args">Command-line arguments to pass to GPG.</param>
  /// <param name="getStatusStream">If true, the status and command streams will be created. If false, they will be
  /// unavailable.
  /// </param>
  /// <param name="closeStdInput">If true, STDIN will be closed immediately after starting the process so that
  /// GPG will not block waiting for input from it.
  /// </param>
  /// <param name="stdOutHandling">Determines how STDOUT will be handled.</param>
  /// <param name="stdErrorHandling">Determines how STDOUT will be handled.</param>
  /// <returns></returns>
  Command Execute(string args, bool getStatusStream, bool closeStdInput,
                  StreamHandling stdOutHandling, StreamHandling stdErrorHandling)
  {
    InheritablePipe statusPipe = null;

    if(getStatusStream) // if the status stream is requested
    {
      statusPipe = new InheritablePipe(); // create a two-way pipe
      string fd = statusPipe.ClientHandle.ToInt64().ToString(CultureInfo.InvariantCulture);
      // and use it for both the status-fd and the command-fd
      args = "--exit-on-status-write-error --status-fd " + fd + " --command-fd " + fd + " " + args;
    }

    return new Command(GetProcessStartInfo(ExecutablePath, args), statusPipe,
                       closeStdInput, stdOutHandling, stdErrorHandling);
  }

  /// <summary>Performs the main work of exporting keys.</summary>
  void ExportCore(string args, Stream destination)
  {
    CommandState state = new CommandState();
    Command cmd = Execute(args, true, true, StreamHandling.Unprocessed, StreamHandling.ProcessText);
    using(cmd)
    {
      cmd.StatusMessageReceived += delegate(StatusMessage msg) { DefaultStatusMessageHandler(msg, ref state); };
      cmd.Start(); // simply start GPG and copy the output to the destination stream
      IOH.CopyStream(cmd.Process.StandardOutput.BaseStream, destination);
      cmd.WaitForExit();
    }

    if(!cmd.SuccessfulExit) throw new ExportFailedException(state.FailureReasons);
  }

  /// <summary>Exports all the keys on the the given keyrings.</summary>
  void ExportKeyrings(Keyring[] keyrings, bool includeDefaultKeyring, Stream destination,
                      ExportOptions exportOptions, OutputOptions outputOptions, bool exportSecretKeys)
  {
    if(destination == null) throw new ArgumentNullException();

    string args = GetKeyringArgs(keyrings, !includeDefaultKeyring, exportSecretKeys) +
                  GetExportArgs(exportOptions, exportSecretKeys) + GetOutputArgs(outputOptions);

    ExportCore(args, destination);
  }

  /// <summary>Exports the given keys.</summary>
  void ExportKeys(PrimaryKey[] keys, Stream destination, ExportOptions exportOptions, OutputOptions outputOptions,
                  bool exportSecretKeys)
  {
    if(keys == null || destination == null) throw new ArgumentNullException();
    if(keys.Length == 0) return;

    string args = GetKeyringArgs(keys, true, exportSecretKeys, true) + GetExportArgs(exportOptions, exportSecretKeys) +
                  GetOutputArgs(outputOptions);

    foreach(PrimaryKey key in keys)
    {
      if(key == null) throw new ArgumentException("A key was null.");
      args += key.Fingerprint + " ";
    }

    ExportCore(args, destination);
  }

  /// <summary>Finds the keys identified by the given fingerprints.</summary>
  PrimaryKey[] FindKeys(string[] fingerprints, KeySignatures signatures, Keyring[] keyrings,
                        bool includeDefaultKeyring, bool secretkeys)
  {
    if(fingerprints == null) throw new ArgumentNullException();
    if(fingerprints.Length == 0) return new PrimaryKey[0];

    // create search arguments containing all the fingerprints
    string searchArgs = null;
    foreach(string fingerprint in fingerprints)
    {
      if(string.IsNullOrEmpty(fingerprint)) throw new ArgumentException("A fingerprint was null or empty.");
      searchArgs += fingerprint + " ";
    }

    // add each key found to a dictionary
    Dictionary<string, PrimaryKey> keyDict = new Dictionary<string, PrimaryKey>();
    foreach(PrimaryKey key in GetKeys(signatures, keyrings, includeDefaultKeyring, secretkeys, searchArgs))
    {
      keyDict[key.Fingerprint] = key;
    }

    // then create the return array and return the keys found
    PrimaryKey[] keys = new PrimaryKey[fingerprints.Length];
    for(int i=0; i<keys.Length; i++) keyDict.TryGetValue(fingerprints[i].ToUpperInvariant(), out keys[i]);
    return keys;
  }

  /// <summary>Does the work of retrieving and searching for keys.</summary>
  PrimaryKey[] GetKeys(KeySignatures signatures, Keyring[] keyrings, bool includeDefaultKeyring, bool secretKeys,
                       string searchArgs)
  {
    // gpg seems to require --no-sig-cache in order to return fingerprints for signatures. that's unfortunate because
    // --no-sig-cache slows things down a fair bit.
    string args;
    if(secretKeys) args = "--list-secret-keys ";
    else if(signatures == KeySignatures.Retrieve) args = "--list-sigs --no-sig-cache ";
    else if(signatures == KeySignatures.Verify) args = "--check-sigs --no-sig-cache ";
    else args = "--list-keys ";

    // produce machine-readable output
    args += "--with-fingerprint --with-fingerprint --with-colons --fixed-list-mode ";

    // although GPG has a "show-keyring" option, it doesn't work with --with-colons, so we need to query each keyring
    // individually, so we can tell which keyring a key came from. this may cause problems with signature verification
    // if a key on one ring signs a key on another ring...
    List<PrimaryKey> keys = new List<PrimaryKey>();
    if(includeDefaultKeyring) GetKeys(keys, args, null, secretKeys, searchArgs);
    if(keyrings != null)
    {
      foreach(Keyring keyring in keyrings)
      {
        if(keyring == null) throw new ArgumentException("A keyring was null.");
        string file = secretKeys ? keyring.SecretFile : keyring.PublicFile;
        if(file == null) throw new ArgumentException("Empty keyring secret filename."); // only secret files can be
        GetKeys(keys, args, keyring, secretKeys, searchArgs);                           // null or empty
      }
    }

    return keys.ToArray();
  }

  /// <summary>Does the work of retrieving and searching for keys on a single keyring.</summary>
  void GetKeys(List<PrimaryKey> keys, string args, Keyring keyring, bool secretKeys, string searchArgs)
  {
    args += GetKeyringArgs(keyring, true, secretKeys, true);

    // if we're searching, but GPG finds no keys, it will give an error. (it doesn't give an error if it found at least
    // one item searched for.) we'll keep track of this case and ignore the error if we happen to be searching.
    bool searchFoundNothing = false;

    Command cmd = Execute(args + searchArgs, false, true, StreamHandling.Unprocessed, StreamHandling.ProcessText);
    using(cmd)
    {
      cmd.StandardErrorLine += delegate(string line)
      {
        if(line.IndexOf(" public key not found", StringComparison.Ordinal) != -1)
        {
          if(searchArgs != null) searchFoundNothing = true; // if we're searching, this error can be ignored.
        }
      };

      cmd.Start();

      List<Subkey> subkeys = new List<Subkey>(); // holds the subkeys in the current primary key
      List<UserId> userIds = new List<UserId>(); // holds user ids in the current primary key
      List<KeySignature> sigs = new List<KeySignature>(); // holds the signatures on the last key or user id

      PrimaryKey currentPrimary = null;
      Subkey currentSubkey = null;
      UserId currentUserId = null;

      while(true)
      {
        string line = cmd.Process.StandardOutput.ReadLine();
        if(line == null) break;

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
              sig.Fingerprint = fields[12].ToUpperInvariant();
            }
            sig.MakeReadOnly();
            sigs.Add(sig);
            break;

          case "uid": // user id
            FinishUserId(userIds, sigs, currentPrimary, currentSubkey, ref currentUserId);
            currentUserId = new UserId();
            if(!string.IsNullOrEmpty(fields[1])) currentUserId.CalculatedTrust = ParseTrustLevel(fields[1][0]);
            if(!string.IsNullOrEmpty(fields[5])) currentUserId.CreationTime    = ParseTimestamp(fields[5]);
            if(!string.IsNullOrEmpty(fields[9])) currentUserId.Name            = CUnescape(fields[9]);
            break;

          case "pub": case "sec": // public and secret primary keys
            FinishPrimaryKey(keys, subkeys, userIds, sigs, ref currentPrimary, ref currentSubkey, ref currentUserId);
            currentPrimary = new PrimaryKey();
            currentPrimary.Keyring = keyring;
            currentPrimary.Secret  = secretKeys;
            ReadKeyData(currentPrimary, fields);
            currentPrimary.Secret = fields[0][0] == 's'; // it's secret if the field was "sec"
            break;

          case "sub": case "ssb": // public and secret subkeys
            FinishSubkey(subkeys, sigs, currentPrimary, ref currentSubkey, currentUserId);
            currentSubkey = new Subkey();
            currentSubkey.Secret = secretKeys;
            ReadKeyData(currentSubkey, fields);
            currentSubkey.Secret = fields[0][1] == 's'; // it's secret if the field was "ssb"
            break;

          case "fpr": // key fingerprint
            if(currentSubkey != null) currentSubkey.Fingerprint = fields[9].ToUpperInvariant();
            else if(currentPrimary != null) currentPrimary.Fingerprint = fields[9].ToUpperInvariant();
            break;

          case "crt": case "crs": // X.509 certificates (we just treat them as an end to the current key)
            FinishPrimaryKey(keys, subkeys, userIds, sigs, ref currentPrimary, ref currentSubkey, ref currentUserId);
            break;
        }
      }

      FinishPrimaryKey(keys, subkeys, userIds, sigs, ref currentPrimary, ref currentSubkey, ref currentUserId);
      cmd.WaitForExit();
    }

    // normally we'd call CheckExitCode to throw an exception if GPG failed, but if we were searching and the search
    // came up empty, don't do that because it'll throw an unwanted exception.
    if(!searchFoundNothing) cmd.CheckExitCode();
  }

  /// <summary>Gets a key password from the user and sends it to the command stream.</summary>
  void SendKeyPassword(Command command, string passwordHint, NeedKeyPassphraseMessage msg, bool passwordRequired)
  {
    string userIdHint = passwordHint + " [0x" + msg.KeyId;
    if(!string.Equals(msg.KeyId, msg.PrimaryKeyId, StringComparison.Ordinal))
    {
      userIdHint += " on primary key 0x" + msg.PrimaryKeyId;
    }
    userIdHint += "]";

    SecureString password = GetKeyPassword(msg.KeyId, userIdHint);
    if(password == null)
    {
      if(passwordRequired) throw new OperationCanceledException("No password was given.");
      else command.SendLine();
    }
    else command.SendPassword(password, true);
  }

  /// <summary>Performs the work of verifying either a detached or embedded signature.</summary>
  Signature[] VerifyCore(string signatureFile, Stream signedData, VerificationOptions options)
  {
    string args = GetVerificationArgs(options, false);
    // --verify takes either one or two arguments. we want the signed data to be sent on STDIN
    args += "--verify " + (signatureFile == null ? "-" : EscapeArg(signatureFile) + " -");
    Command cmd = Execute(args, true, false, StreamHandling.DumpBinary, StreamHandling.ProcessText);
    return DecryptVerifyCore(cmd, signedData, null, null);
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
  static void DefaultStandardErrorHandler(string line, ref CommandState state)
  {
    if(line.IndexOf(" file write error", StringComparison.Ordinal) != -1 ||
       line.IndexOf(" file rename error", StringComparison.Ordinal) != -1)
    {
      state.FailureReasons |= FailureReason.KeyringLocked;
    }
    else if(line.IndexOf(" already in secret keyring", StringComparison.Ordinal) != -1)
    {
      state.FailureReasons |= FailureReason.SecretKeyAlreadyExists;
    }
  }

  /// <summary>Executes the given GPG executable with the given arguments.</summary>
  static Process Execute(string exePath, string args)
  {
    return Process.Start(GetProcessStartInfo(exePath, args));
  }

  /// <summary>Exits a process by closing STDIN, STDOUT, and STDERR, and waiting for it to exit. If it doesn't exit
  /// within a short period, it will be killed. Returns the process' exit code.
  /// </summary>
  static int Exit(Process process)
  {
    process.StandardInput.Close();
    process.StandardOutput.Close();
    process.StandardError.Close();
    if(!process.WaitForExit(500)) process.Kill();
    return process.ExitCode;
  }

  /// <summary>Escapes a command-line argument or throws an exception if it cannot be escaped.</summary>
  static string EscapeArg(string arg)
  {
    // TODO: does this handle everything? unfortunately, ProcessStartInfo.Arguments is very poorly designed
    if(arg.IndexOf(' ') != -1) // if the argument contains spaces, we need to quote it.
    {
      if(arg.IndexOf('"') == -1) return "\"" + arg + "\""; // if it doesn't contain a double-quote, use those
      else if(arg.IndexOf('\'') == -1) return "'" + arg + "'"; // otherwise, try single quotes
    }
    else if(arg.IndexOf('"') != -1)
    {
      throw new NotImplementedException();
    }
    else if(ContainsControlCharacters(arg))
    {
      throw new ArgumentException("Argument '"+arg+"' contains illegal control characters.");
    }
    else return arg;

    throw new ArgumentException("Argument could not be escaped: "+arg);
  }

  /// <summary>A helper for reading key listings, that finishes the current primary key.</summary>
  static void FinishPrimaryKey(List<PrimaryKey> keys, List<Subkey> subkeys, List<UserId> userIds,
                               List<KeySignature> sigs, ref PrimaryKey currentPrimary, ref Subkey currentSubkey,
                               ref UserId currentUserId)
  {
    // finishing a primary key finishes all signatures, subkeys, and user IDs on it
    FinishSignatures(sigs, currentPrimary, currentSubkey, currentUserId);
    FinishSubkey(subkeys, sigs, currentPrimary, ref currentSubkey, currentUserId);
    FinishUserId(userIds, sigs, currentPrimary, currentSubkey, ref currentUserId);

    if(currentPrimary != null)
    {
      currentPrimary.Subkeys = new ReadOnlyListWrapper<Subkey>(subkeys.ToArray());
      currentPrimary.UserIds = new ReadOnlyListWrapper<UserId>(userIds.ToArray());
      if(currentPrimary.Signatures == null) currentPrimary.Signatures = NoSignatures;

      currentPrimary.MakeReadOnly();
      keys.Add(currentPrimary);
      currentPrimary = null;
    }

    subkeys.Clear();
    userIds.Clear();
  }

  /// <summary>A helper for reading key listings, that finishes the current key signatures.</summary>
  static void FinishSignatures(List<KeySignature> sigs, PrimaryKey currentPrimary, Subkey currentSubkey,
                               UserId currentUserId)
  {
    ReadOnlyListWrapper<KeySignature> list = new ReadOnlyListWrapper<KeySignature>(sigs.ToArray());

    // add the signatures to the most recent object in the key listing
    if(currentUserId != null) currentUserId.Signatures = list;
    else if(currentSubkey != null) currentSubkey.Signatures = list;
    else if(currentPrimary != null) currentPrimary.Signatures = list;

    sigs.Clear();
  }

  /// <summary>A helper for reading key listings, that finishes the current subkey.</summary>
  static void FinishSubkey(List<Subkey> subkeys, List<KeySignature> sigs,
                           PrimaryKey currentPrimary, ref Subkey currentSubkey, UserId currentUserId)
  {
    FinishSignatures(sigs, currentPrimary, currentSubkey, currentUserId);

    if(currentSubkey != null && currentPrimary != null)
    {
      currentSubkey.PrimaryKey = currentPrimary;
      if(currentSubkey.Signatures == null) currentSubkey.Signatures = NoSignatures;

      currentSubkey.MakeReadOnly();
      subkeys.Add(currentSubkey);
      currentSubkey = null;
    }
  }

  /// <summary>A helper for reading key listings, that finishes the current user ID.</summary>
  static void FinishUserId(List<UserId> userIds, List<KeySignature> sigs,
                           PrimaryKey currentPrimary, Subkey currentSubkey, ref UserId currentUserId)
  {
    FinishSignatures(sigs, currentPrimary, currentSubkey, currentUserId);

    if(currentUserId != null && currentPrimary != null)
    {
      currentUserId.Key     = currentPrimary;
      currentUserId.Primary = userIds.Count == 0; // the primary user ID is the first one listed
      if(currentUserId.Signatures == null) currentUserId.Signatures = NoSignatures;

      currentUserId.MakeReadOnly();
      userIds.Add(currentUserId);
      currentUserId = null;
    }
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

  /// <summary>Creates GPG arguments to represent the given <see cref="ExportOptions"/>.</summary>
  static string GetExportArgs(ExportOptions options, bool exportSecretKeys)
  {
    string args = null;

    if(options != ExportOptions.Default)
    {
      args += "--export-options ";
      if((options & ExportOptions.CleanKeys) != 0) args += "export-clean ";
      if((options & ExportOptions.ExcludeAttributes) != 0) args += "no-export-attributes ";
      if((options & ExportOptions.ExportLocalSignatures) != 0) args += "export-local-sigs ";
      if((options & ExportOptions.ExportSensitiveRevokerInfo) != 0) args += "export-sensitive-revkeys ";
      if((options & ExportOptions.MinimizeKeys) != 0) args += "export-minimize ";
      if((options & ExportOptions.ResetSubkeyPassword) != 0) args += "export-reset-subkey-passwd ";
    }

    if(exportSecretKeys)
    {
      args += (options & ExportOptions.ClobberMasterSecretKey) != 0 ?
        "--export-secret-subkeys " : "--export-secret-keys ";
    }
    else args += "--export "; // exporting public keys

    return args;
  }

  /// <summary>Creates GPG arguments to represent the given keyring.</summary>
  static string GetKeyringArgs(Keyring keyring, bool publicKeyrings, bool secretKeyrings, bool overrideDefaultKeyring)
  {
    string args = null;
    if(keyring != null)
    {
      if(overrideDefaultKeyring) args += "--no-default-keyring ";

      if(publicKeyrings) args += "--keyring " + EscapeArg(NormalizeKeyringFile(keyring.PublicFile)) + " ";

      if(secretKeyrings && keyring.SecretFile != null)
      {
        args += "--secret-keyring " + EscapeArg(NormalizeKeyringFile(keyring.SecretFile)) + " ";
      }
    }
    return args;
  }

  /// <summary>Creates GPG arguments to represent the given keyrings.</summary>
  static string GetKeyringArgs(IEnumerable<Keyring> keyrings, bool ignoreDefaultKeyring, bool wantSecretKeyrings)
  {
    string args = null;

    if(ignoreDefaultKeyring) args += "--no-default-keyring ";

    foreach(Keyring keyring in keyrings)
    {
      if(keyring != null)
      {
        args += "--keyring " + EscapeArg(NormalizeKeyringFile(keyring.PublicFile)) + " ";
        if(wantSecretKeyrings && keyring.SecretFile != null)
        {
          args += "--secret-keyring " + EscapeArg(NormalizeKeyringFile(keyring.SecretFile)) + " ";
        }
      }
    }

    return args;
  }

  /// <summary>Returns keyring arguments for all of the given keys.</summary>
  static string GetKeyringArgs(IEnumerable<Key> keys, bool publicKeyrings, bool secretKeyrings,
                               bool overrideDefaultKeyring)
  {
    string args = null;

    if(keys != null)
    {
      // keep track of which public and secret keyring files have been seen so we don't add them twice
      Dictionary<string, object> publicFiles = new Dictionary<string, object>(StringComparer.Ordinal);
      Dictionary<string, object> secretFiles = new Dictionary<string, object>(StringComparer.Ordinal);

      foreach(Key key in keys)
      {
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

          if(publicKeyrings && !publicFiles.ContainsKey(publicFile))
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
        args += "--keyserver "+EscapeArg(options.KeyServer.AbsoluteUri)+" --keyserver-options auto-key-retrieve ";
        if(!options.AutoFetchKeys) args += "--auto-key-locate keyserver ";
      }

      if(options.AssumeBinaryInput) args += "--no-armor ";
    }
    return args;
  }

  /// <summary>Creates and returns a new <see cref="ProcessStartInfo"/> for the given GPG executable and arguments.</summary>
  static ProcessStartInfo GetProcessStartInfo(string exePath, string args)
  {
    ProcessStartInfo psi = new ProcessStartInfo();
    psi.Arguments              = "--no-tty --no-options --display-charset utf-8 " + args;
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

  /// <summary>A helper for reading key listings, that reads the data for a primary key or subkey.</summary>
  static void ReadKeyData(Key key, string[] data)
  {
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
    if(!string.IsNullOrEmpty(data[8]) && key is PrimaryKey) ((PrimaryKey)key).OwnerTrust = ParseTrustLevel(data[8][0]);

    if(!string.IsNullOrEmpty(data[11]))
    {
      KeyCapability totalCapabilities = 0;
      foreach(char c in data[11])
      {
        switch(c)
        {
          case 'e': key.Capabilities |= KeyCapability.Encrypt; break;
          case 's': key.Capabilities |= KeyCapability.Sign; break;
          case 'c': key.Capabilities |= KeyCapability.Certify; break;
          case 'a': key.Capabilities |= KeyCapability.Authenticate; break;
          case 'E': totalCapabilities |= KeyCapability.Encrypt; break;
          case 'S': totalCapabilities |= KeyCapability.Sign; break;
          case 'C': totalCapabilities |= KeyCapability.Certify; break;
          case 'A': totalCapabilities |= KeyCapability.Authenticate; break;
          case 'D': if(key is PrimaryKey) ((PrimaryKey)key).Disabled = true; break;
        }
      }

      if(key is PrimaryKey) ((PrimaryKey)key).TotalCapabilities = totalCapabilities;
    }
  }

  /// <summary>Normalizes a keyring filename to something that is acceptable to GPG.</summary>
  static string NormalizeKeyringFile(string filename)
  {
    // GPG treats relative keyring and trustdb paths as being relative to the user's home directory, so we'll get the
    // full path. and it detects relative paths by searching for only one directory separator char (backslash on
    // windows), so we'll normalize those as well
    return Path.GetFullPath(filename).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
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
      default: return TrustLevel.Unknown;
    }
  }

  /// <summary>Trims a string, or returns null if the string is null.</summary>
  static string Trim(string str)
  {
    return str == null ? null : str.Trim();
  }

  /// <summary>Writes all data from the stream to the standard input of the process,
  /// and then closes the standard input.
  /// </summary>
  /// <returns>Returns true if all data was written to the stream before the process terminated.</returns>
  static bool WriteStreamToProcess(Stream data, Process process)
  {
    byte[] buffer = new byte[4096];
    bool allDataWritten = false;

    while(!process.HasExited)
    {
      int read = data.Read(buffer, 0, buffer.Length);
      if(read == 0)
      {
        allDataWritten = true;
        break;
      }

      try { process.StandardInput.BaseStream.Write(buffer, 0, read); }
      catch(ObjectDisposedException) { break; }
    }

    process.StandardInput.Close();
    return allDataWritten;
  }

  /// <summary>Clears the given buffer.</summary>
  static void ZeroBuffer<T>(T[] buffer)
  {
    if(buffer != null) Array.Clear(buffer, 0, buffer.Length);
  }

  string[] ciphers, hashes, keyTypes, compressions;
  string exePath;

  static readonly ReadOnlyListWrapper<KeySignature> NoSignatures =
    new ReadOnlyListWrapper<KeySignature>(new KeySignature[0]);
  static readonly Regex versionLineRe = new Regex(@"^(\w+):\s*(.+)", RegexOptions.Singleline);
  static readonly Regex commaSepRe = new Regex(@",\s*", RegexOptions.Singleline);
  static readonly Regex cEscapeRe = new Regex(@"\\x[0-9a-f]{2}",
    RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);
}
#endregion

} // namespace AdamMil.Security.PGP