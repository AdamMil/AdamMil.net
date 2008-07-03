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
      IOH.WriteBE4(dbFile, (int)((DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds));
    }
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
  public override PrimaryKey[] FindPublicKeys(string[] fingerprints, Keyring[] keyrings, bool includeDefaultKeyring,
                                              ListOptions options)
  {
    return FindKeys(fingerprints, keyrings, includeDefaultKeyring, options, false);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/FindSecretKeys/*"/>
  public override PrimaryKey[] FindSecretKeys(string[] fingerprints, Keyring[] keyrings, bool includeDefaultKeyring,
                                              ListOptions options)
  {
    return FindKeys(fingerprints, keyrings, includeDefaultKeyring, options, true);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetPublicKeys2/*"/>
  public override PrimaryKey[] GetPublicKeys(Keyring[] keyrings, bool includeDefaultKeyring, ListOptions options)
  {
    return GetKeys(keyrings, includeDefaultKeyring, options, false, null);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetSecretKeys2/*"/>
  public override PrimaryKey[] GetSecretKeys(Keyring[] keyrings, bool includeDefaultKeyring)
  {
    return GetKeys(keyrings, includeDefaultKeyring, ListOptions.Default, true, null);
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
      if(string.Equals(options.SubkeyType, SubkeyType.RSA, StringComparison.OrdinalIgnoreCase))
      {
        throw new KeyCreationFailedException(FailureReason.UnsupportedAlgorithm, "Please specify an encryption-only "+
                                             "or signing-only RSA key. Generic RSA subkeys are not allowed by GPG.");
      }
      else
      {
        throw new KeyCreationFailedException(FailureReason.UnsupportedAlgorithm,
                                             "Subkey type "+options.SubkeyType+" is not supported.");
      }
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

    int expirationDays = GetExpirationDays(options.Expiration);

    // the options look good, so lets make the key
    string keyFingerprint = null;
    CommandState state = new CommandState();

    string args = GetKeyringArgs(options.Keyring, true, true, true);

    // if we're using DSA keys greater than 1024 bits, we need to enable DSA2 support
    if(primaryIsDSA && options.KeyLength > 1024 || subIsDSA && options.SubkeyLength > 1024) args += "--enable-dsa2 ";

    Command cmd = Execute(args + "--batch --gen-key", true, false,
                          StreamHandling.ProcessText, StreamHandling.ProcessText);
    using(cmd)
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

    if(!cmd.SuccessfulExit || keyFingerprint == null) throw new KeyCreationFailedException(state.FailureReasons);

    // return the new PrimaryKey
    return FindPublicKey(keyFingerprint, options.Keyring, ListOptions.Default);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/DeleteKeys/*"/>
  public override void DeleteKeys(PrimaryKey[] keys, KeyDeletion deletion)
  {
    if(keys == null) throw new ArgumentNullException();

    string args = GetKeyringArgs(keys, true, deletion == KeyDeletion.PublicAndSecret, true);
    args += (deletion == KeyDeletion.Secret ? "--delete-secret-key " : "--delete-secret-and-public-key ");

    foreach(Key key in keys) args += key.Fingerprint + " "; // add the fingerprints of the keys to delete

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
    DumpBinary,
    /// <summary>The stream has both output and GPG status lines.</summary>
    Mixed
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
  sealed class Command : System.Runtime.ConstrainedExecution.CriticalFinalizerObject, IDisposable
  {
    public Command(ProcessStartInfo psi, InheritablePipe commandPipe,
                   bool closeStdInput, StreamHandling stdOut, StreamHandling stdError)
    {
      if(psi == null) throw new ArgumentNullException();
      this.psi           = psi;
      this.commandPipe   = commandPipe;
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

    /// <summary>Parses a string containing a status line into a status message.</summary>
    public StatusMessage ParseStatusMessage(string line)
    {
      string[] chunks = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      if(chunks.Length >= 2 && string.Equals(chunks[0], "[GNUPG:]", StringComparison.Ordinal))
      {
        string[] arguments = new string[chunks.Length-2];
        Array.Copy(chunks, 2, arguments, 0, arguments.Length);
        return ParseStatusMessage(chunks[1], arguments);
      }
      else return null;
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
Debugger.Log(0, "GPG", ">> "+line+"\n");
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
        IntPtr bstr  = IntPtr.Zero;
        char[] chars = new char[password.Length+1];
        byte[] bytes = null;
        try
        {
          if(commandStream == null) throw new InvalidOperationException("The command stream is not open.");
          bstr = Marshal.SecureStringToBSTR(password);
          Marshal.Copy(bstr, chars, 0, chars.Length);
          chars[password.Length] = '\n'; // the password must be EOL-terminated for GPG to accept it
          bytes = Encoding.UTF8.GetBytes(chars);
          commandStream.Write(bytes, 0, bytes.Length);
          commandStream.Flush();
        }
        finally
        {
          if(ownsPassword) password.Dispose();
          if(bstr != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr);
          ZeroBuffer(chars);
          ZeroBuffer(bytes);
        }
      }
    }

    /// <summary>Starts executing the command.</summary>
    public void Start()
    {
      if(process != null) throw new InvalidOperationException("The process has already been started.");

      process = Process.Start(psi);

      if(closeStdInput) process.StandardInput.Close();

      // if we have a command pipe, set up a stream to read-write it
      if(commandPipe != null)
      {
        commandStream = new FileStream(new SafeFileHandle(commandPipe.ServerHandle, false), FileAccess.ReadWrite);
        if(outHandling != StreamHandling.Mixed) // if the status messages aren't mixed into the STDOUT, then they'll
        {                                       // be available on the command pipe, so start reading them
          statusBuffer = new byte[4096];
          OnStatusRead(null); // start reading on a background thread
        }
        else statusDone = true;
      }
      else statusDone = true;

      if(outHandling == StreamHandling.Close || outHandling == StreamHandling.Unprocessed ||
         outHandling == StreamHandling.Mixed)
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

    void Dispose(bool finalizing)
    {
      if(!disposed)
      {
        if(commandPipe != null) // if we have a command pipe, we want to close our end of it to tell GPG to exit ASAP.
        {                       // this gives GPG a chance to exit more gracefully than if we just terminated it.
          if(commandStream != null)
          {
            commandStream.Dispose();
            commandStream = null;
          }

          commandPipe.CloseServer(); // close the server side of the pipe
          if(process != null) Exit(process); // then exit the process
          commandPipe.Dispose(); // and destroy the pipe
          commandPipe = null;
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
            ProcessStatusStream(result, stream, handling, ref buffer, ref bufferBytes, ref bufferDone);
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
      HandleStream(StreamHandling.ProcessStatus, commandStream, result, ref statusBuffer, ref statusBytes,
                   ref statusDone, null, OnStatusRead);
    }
    #pragma warning restore 420

    void ProcessStatusStream(IAsyncResult result, Stream stream, StreamHandling handling,
                             ref byte[] buffer, ref int bufferBytes, ref bool bufferDone)
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
    InheritablePipe commandPipe;
    FileStream commandStream;
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
          char high = (char)encoded[index+1], low = (char)encoded[index+2];
          if(IsHexDigit(high) && IsHexDigit(low))
          {
            encoded[index] = (byte)GetHexValue(high, low); // convert the hex value to the new byte value
            index     += 2; // skip over two of the three digits. the third will be skipped on the next iteration
            newLength -= 2;
          }
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

        case "ATTRIBUTE": message = new AttributeMessage(arguments); break;

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
Debugger.Log(0, "GPG", Encoding.ASCII.GetString(line, 0, length)+"\n"); // TODO: remove this
      List<string> chunks = new List<string>();
      type      = null;
      arguments = null;

      // the chunks are whitespace-separated
      for(int index=0; ; )
      {
        while(index < length && line[index] == (byte)' ') index++; // find the next non-whitespace character
        int start = index;
        while(index < length && line[index] != (byte)' ') index++; // find the next whitespace character after that

        if(start == length) break; // if we're at the end of the line, we're done

        chunks.Add(Encoding.UTF8.GetString(line, start, index-start)); // grab the text between the two

        // if this isn't a status line, don't waste time splitting the rest of it
        if(chunks.Count == 1 && !string.Equals(chunks[0], "[GNUPG:]", StringComparison.Ordinal)) break;
      }

      if(chunks.Count >= 2) // if there are enough chunks to make up a status line
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
                SecureString password = GetPlainPassword();
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

  void DoEdit(PrimaryKey key, string extraArgs, params EditCommand[] initialCommands)
  {
    if(key == null) throw new ArgumentNullException();
    if(string.IsNullOrEmpty(key.Fingerprint)) throw new ArgumentException("The key to edit has no fingerprint.");

    EditKey originalKey = null, editKey = null;
    Queue<EditCommand> commands = new Queue<EditCommand>(initialCommands);
    CommandState state = new CommandState();

    Command cmd = ExecuteForEdit(key, extraArgs);
    using(cmd)
    {
      cmd.StandardErrorLine += delegate(string line)
      {
        DefaultStandardErrorHandler(line, ref state);
      };

      cmd.Start();

      while(true)
      {
        string line = cmd.Process.StandardOutput.ReadLine();
        Debugger.Log(0, "GPG", line+"\n");
        gotLine:
        if(line == null) break;

        if(line.Equals("[GNUPG:] GOT_IT", StringComparison.Ordinal)) continue; // skip acknowledgements of input
        else if(line.StartsWith("pub:", StringComparison.Ordinal)) // a key listing is beginning
        {
          EditKey newKey = new EditKey();
          EditSubkey currentSubkey = null;
          int subkeyIndex = 1, uidIndex = 1;

          do
          {
            string[] fields = line.Split(':');

            switch(fields[0])
            {
              case "sub":
                if(currentSubkey != null) newKey.Subkeys.Add(currentSubkey);
                currentSubkey = new EditSubkey();
                currentSubkey.Index = subkeyIndex++;
                break;

              case "fpr":
                if(currentSubkey != null) currentSubkey.Fingerprint = fields[9].ToUpperInvariant();
                break;

              case "uid":
              case "uat":
                {
                  EditUserId uid = new EditUserId();
                  uid.Index       = uidIndex++;
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

                  newKey.UserIds.Add(uid);
                  break;
                }
            }

            line = cmd.Process.StandardOutput.ReadLine();
            Debugger.Log(0, "GPG", line+"\n");
          } while(!string.IsNullOrEmpty(line) && line[0] != '['); // break out if the line is empty or a status line

          if(currentSubkey != null) newKey.Subkeys.Add(currentSubkey);

          editKey = newKey;

          if(originalKey == null) originalKey = newKey;
          goto gotLine;
        }
        else if(line.StartsWith("[GNUPG:] ", StringComparison.Ordinal)) // a status message was received
        {
          StatusMessage msg = cmd.ParseStatusMessage(line);
          if(msg != null)
          {
            switch(msg.Type)
            {
              case StatusMessageType.GetLine:
              case StatusMessageType.GetHidden:
              case StatusMessageType.GetBool:
              {
                string promptId = ((GetInputMessage)msg).PromptId;
                while(true)
                {
                  if(commands.Count == 0) commands.Enqueue(new QuitEditCommand(true));

                  EditCommandResult result = commands.Peek().Process(commands, originalKey, editKey, cmd, promptId);
                  if(result == EditCommandResult.Continue)
                  {
                    break;
                  }
                  else if(result == EditCommandResult.Done)
                  {
                    commands.Dequeue();
                    break;
                  }
                  else if(result == EditCommandResult.Next)
                  {
                    commands.Dequeue();
                  }
                }
                break;
              }

              case StatusMessageType.NeedCipherPassphrase:
                while(true)
                {
                  if(commands.Count == 0) goto default;

                  EditCommandResult result = commands.Peek().SendCipherPassword(originalKey, editKey, cmd);
                  if(result == EditCommandResult.Continue)
                  {
                    break;
                  }
                  else if(result == EditCommandResult.Done)
                  {
                    commands.Dequeue();
                    break;
                  }
                  else if(result == EditCommandResult.Next)
                  {
                    commands.Dequeue();
                  }
                }
                break;

              case StatusMessageType.NeedKeyPassphrase:
                if(!SendKeyPassword(cmd, state.PasswordHint, (NeedKeyPassphraseMessage)msg, false))
                {
                  throw new OperationCanceledException();
                }
                break;

              default: DefaultStatusMessageHandler(msg, ref state); break;
            }
          }
        }
      }

      cmd.WaitForExit();
    }

    cmd.CheckExitCode();
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
    InheritablePipe commandPipe = null;
    if(getStatusStream) // if the status stream is requested
    {
      commandPipe = new InheritablePipe(); // create a two-way pipe
      string fd = commandPipe.ClientHandle.ToInt64().ToString(CultureInfo.InvariantCulture);
      // and use it for both the status-fd and the command-fd
      args = "--exit-on-status-write-error --status-fd " + fd + " --command-fd " + fd + " " + args;
    }
    return new Command(GetProcessStartInfo(ExecutablePath, args, false), commandPipe,
                       closeStdInput, stdOutHandling, stdErrorHandling);
  }

  Command ExecuteForEdit(PrimaryKey key, string extraArgs)
  {
    // we'll use the pipe for the command-fd, but we'll pipe the status messages to STDOUT
    InheritablePipe commandPipe = new InheritablePipe(); // create a two-way pipe
    string fd = commandPipe.ClientHandle.ToInt64().ToString(CultureInfo.InvariantCulture);
    string args = GetKeyringArgs(key.Keyring, true, true, true) + "--with-colons --fixed-list-mode "+
                  "--exit-on-status-write-error --status-fd 1 --command-fd " + fd + " " + extraArgs + " --edit-key " +
                  key.Fingerprint;
    return new Command(GetProcessStartInfo(ExecutablePath, args, true), commandPipe,
                       true, StreamHandling.Mixed, StreamHandling.ProcessText);
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
  PrimaryKey[] FindKeys(string[] fingerprints, Keyring[] keyrings, bool includeDefaultKeyring, ListOptions options,
                        bool secretkeys)
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
    foreach(PrimaryKey key in GetKeys(keyrings, includeDefaultKeyring, options, secretkeys, searchArgs))
    {
      keyDict[key.Fingerprint] = key;
    }

    // then create the return array and return the keys found
    PrimaryKey[] keys = new PrimaryKey[fingerprints.Length];
    for(int i=0; i<keys.Length; i++) keyDict.TryGetValue(fingerprints[i].ToUpperInvariant(), out keys[i]);
    return keys;
  }

  /// <summary>Does the work of retrieving and searching for keys.</summary>
  PrimaryKey[] GetKeys(Keyring[] keyrings, bool includeDefaultKeyring, ListOptions options, bool secretKeys,
                       string searchArgs)
  {
    ListOptions signatures = options & ListOptions.SignatureMask;
    // gpg seems to require --no-sig-cache in order to return fingerprints for signatures. that's unfortunate because
    // --no-sig-cache slows things down a fair bit.
    string args;
    if(secretKeys) args = "--list-secret-keys "; // TODO: add --no-auto-check-trustdb to this
    else if(signatures == ListOptions.RetrieveSignatures) args = "--list-sigs --no-sig-cache ";
    else if(signatures == ListOptions.VerifySignatures) args = "--check-sigs --no-sig-cache ";
    else args = "--list-keys "; // TODO: add --no-auto-check-trustdb to this

    // produce machine-readable output
    args += "--with-fingerprint --with-fingerprint --with-colons --fixed-list-mode ";

    // although GPG has a "show-keyring" option, it doesn't work with --with-colons, so we need to query each keyring
    // individually, so we can tell which keyring a key came from. this may cause problems with signature verification
    // if a key on one ring signs a key on another ring...
    List<PrimaryKey> keys = new List<PrimaryKey>();
    if(includeDefaultKeyring) GetKeys(keys, options, args, null, secretKeys, searchArgs);
    if(keyrings != null)
    {
      foreach(Keyring keyring in keyrings)
      {
        if(keyring == null) throw new ArgumentException("A keyring was null.");
        string file = secretKeys ? keyring.SecretFile : keyring.PublicFile;
        if(file == null) throw new ArgumentException("Empty keyring secret filename."); // only secret files can be
        GetKeys(keys, options, args, keyring, secretKeys, searchArgs);                  // null or empty
      }
    }

    return keys.ToArray();
  }

  /// <summary>Does the work of retrieving and searching for keys on a single keyring.</summary>
  void GetKeys(List<PrimaryKey> keys, ListOptions options, string args, Keyring keyring,
               bool secretKeys, string searchArgs)
  {
    args += GetKeyringArgs(keyring, true, secretKeys, true);

    // if we're searching, but GPG finds no keys, it will give an error. (it doesn't give an error if it found at least
    // one item searched for.) we'll keep track of this case and ignore the error if we happen to be searching.
    bool searchFoundNothing = false, retrieveAttributes = (options & ListOptions.RetrieveAttributes) != 0;

    InheritablePipe attrPipe;
    FileStream attrStream;
    AutoResetEvent attrReadEvent, attrWriteEvent;
    OpenPGPAttributeType attrType = 0;
    int attrLength = 0;
    bool attrPrimary = false;
    if(retrieveAttributes)
    {
      attrPipe       = new InheritablePipe();
      attrStream     = new FileStream(new SafeFileHandle(attrPipe.ServerHandle, false), FileAccess.Read);
      attrReadEvent  = new AutoResetEvent(false);
      attrWriteEvent = new AutoResetEvent(true);
      args += "--attribute-fd " + attrPipe.ClientHandle.ToInt64().ToString(CultureInfo.InvariantCulture) + " ";
    }
    else
    {
      attrPipe      = null;
      attrStream    = null;
      attrReadEvent = attrWriteEvent = null;
    }

    Command cmd = Execute(args + searchArgs, retrieveAttributes,
                          true, StreamHandling.Unprocessed, StreamHandling.ProcessText);
    try
    {
      cmd.StandardErrorLine += delegate(string line)
      {
        if(line.IndexOf(" public key not found", StringComparison.Ordinal) != -1)
        {
          if(searchArgs != null) searchFoundNothing = true; // if we're searching, this error can be ignored.
        }
      };

      if(retrieveAttributes)
      {
        cmd.StatusMessageReceived += delegate(StatusMessage msg)
        {
          if(msg.Type == StatusMessageType.Attribute)
          {
            attrWriteEvent.WaitOne(); // wait until the main thread is ready for more attribute data
            AttributeMessage m = (AttributeMessage)msg;
            attrType    = m.AttributeType;
            attrLength  = m.Length;
            attrPrimary = m.IsPrimary;
            attrReadEvent.Set(); // let the mait thread know that the data is available to be read
          }
        };
      }

      cmd.Start();

      List<Subkey> subkeys = new List<Subkey>(); // holds the subkeys in the current primary key
      List<KeySignature> sigs = new List<KeySignature>(); // holds the signatures on the last key or user id
      List<UserAttribute> attributes = new List<UserAttribute>(); // holds user attributes on the key
      List<string> revokers = new List<string>(); // holds designated revokers on the key

      PrimaryKey currentPrimary = null;
      Subkey currentSubkey = null;
      UserAttribute currentAttribute = null;

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
          {
            FinishAttribute(attributes, sigs, currentPrimary, currentSubkey, ref currentAttribute);
            UserId userId = new UserId();
            if(!string.IsNullOrEmpty(fields[1])) userId.CalculatedTrust = ParseTrustLevel(fields[1][0]);
            if(!string.IsNullOrEmpty(fields[5])) userId.CreationTime    = ParseTimestamp(fields[5]);
            if(!string.IsNullOrEmpty(fields[7])) userId.Id              = fields[7].ToUpperInvariant();
            if(!string.IsNullOrEmpty(fields[9])) userId.Name            = CUnescape(fields[9]);
            currentAttribute = userId;
            break;
          }

          case "pub": case "sec": // public and secret primary keys
            FinishPrimaryKey(keys, subkeys, attributes, sigs, revokers,
                             ref currentPrimary, ref currentSubkey, ref currentAttribute);
            currentPrimary = new PrimaryKey();
            currentPrimary.Keyring = keyring;
            currentPrimary.Secret  = secretKeys;
            ReadKeyData(currentPrimary, fields);
            currentPrimary.Secret = fields[0][0] == 's'; // it's secret if the field was "sec"
            break;

          case "sub": case "ssb": // public and secret subkeys
            FinishSubkey(subkeys, sigs, currentPrimary, ref currentSubkey, currentAttribute);
            currentSubkey = new Subkey();
            currentSubkey.Secret = secretKeys;
            ReadKeyData(currentSubkey, fields);
            currentSubkey.Secret = fields[0][1] == 's'; // it's secret if the field was "ssb"
            break;

          case "fpr": // key fingerprint
            if(currentSubkey != null) currentSubkey.Fingerprint = fields[9].ToUpperInvariant();
            else if(currentPrimary != null) currentPrimary.Fingerprint = fields[9].ToUpperInvariant();
            break;

          case "uat": // user attribute
          {
            FinishAttribute(attributes, sigs, currentPrimary, currentSubkey, ref currentAttribute);

            if(retrieveAttributes)
            {
              attrReadEvent.WaitOne(); // wait until attribute data has been provided

              // read the data
              byte[] data = new byte[attrLength];
              int index = 0;
              while(index < data.Length)
              {
                int read = attrStream.Read(data, index, data.Length - index);
                if(read == 0) break;
                index += read;
              }

              if(index == data.Length)
              {
                currentAttribute = UserAttribute.Create(attrType, data);
                currentAttribute.Primary = attrPrimary;
                if(!string.IsNullOrEmpty(fields[1])) currentAttribute.CalculatedTrust = ParseTrustLevel(fields[1][0]);
                if(!string.IsNullOrEmpty(fields[5])) currentAttribute.CreationTime    = ParseTimestamp(fields[5]);
                if(!string.IsNullOrEmpty(fields[7])) currentAttribute.Id              = fields[7].ToUpperInvariant();
              }

              attrWriteEvent.Set(); // we're done with this line and are ready to receive more attribute data
            }
            break;
          }

          case "rvk": // a designated revoker
            revokers.Add(fields[9].ToUpperInvariant());
            break;

          case "crt": case "crs": // X.509 certificates (we just treat them as an end to the current key)
            FinishPrimaryKey(keys, subkeys, attributes, sigs, revokers,
                             ref currentPrimary, ref currentSubkey, ref currentAttribute);
            break;
        }
      }

      FinishPrimaryKey(keys, subkeys, attributes, sigs, revokers,
                       ref currentPrimary, ref currentSubkey, ref currentAttribute);
      cmd.WaitForExit();
    }
    finally
    {
      cmd.Dispose();
      if(retrieveAttributes)
      {
        attrStream.Dispose();
        attrPipe.Dispose();
        attrReadEvent.Close();
        attrWriteEvent.Close();
      }
    }

    // normally we'd call CheckExitCode to throw an exception if GPG failed, but if we were searching and the search
    // came up empty, don't do that because it'll throw an unwanted exception.
    if(!searchFoundNothing) cmd.CheckExitCode();
  }

  /// <summary>Gets a key password from the user and sends it to the command stream.</summary>
  bool SendKeyPassword(Command command, string passwordHint, NeedKeyPassphraseMessage msg, bool passwordRequired)
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
    else if(line.Equals("Need the secret key to do this.", StringComparison.Ordinal))
    {
      state.FailureReasons |= FailureReason.MissingSecretKey;
    }
  }

  /// <summary>Executes the given GPG executable with the given arguments.</summary>
  static Process Execute(string exePath, string args)
  {
    return Process.Start(GetProcessStartInfo(exePath, args, true));
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
  static void FinishPrimaryKey(List<PrimaryKey> keys, List<Subkey> subkeys, List<UserAttribute> attributes,
                               List<KeySignature> sigs, List<string> revokers,ref PrimaryKey currentPrimary,
                               ref Subkey currentSubkey, ref UserAttribute currentAttribute)
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

      currentPrimary.MakeReadOnly();
      keys.Add(currentPrimary);
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
    if(currentAttribute != null) currentAttribute.Signatures = list;
    else if(currentSubkey != null) currentSubkey.Signatures = list;
    else if(currentPrimary != null) currentPrimary.Signatures = list;

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
      currentAttribute.Key  = currentPrimary;

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

      if(keyring.TrustDbFile != null)
      {
        args += "--trustdb-name " + EscapeArg(NormalizeKeyringFile(keyring.TrustDbFile)) + " ";
      }
    }
    
    return args;
  }

  /// <summary>Creates GPG arguments to represent the given keyrings.</summary>
  static string GetKeyringArgs(IEnumerable<Keyring> keyrings, bool ignoreDefaultKeyring, bool wantSecretKeyrings)
  {
    string args = null, trustDb = null;
    bool trustDbSet = false;

    if(ignoreDefaultKeyring) args += "--no-default-keyring ";

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

    if(trustDb != null) args += "--trustdb-name " + EscapeArg(trustDb) + " ";

    return args;
  }

  /// <summary>Returns keyring arguments for all of the given keys.</summary>
  static string GetKeyringArgs(IEnumerable<Key> keys, bool publicKeyrings, bool secretKeyrings,
                               bool overrideDefaultKeyring)
  {
    string args = null, trustDb = null;
    bool trustDbSet = false;

    if(keys != null)
    {
      // keep track of which public and secret keyring files have been seen so we don't add them twice
      Dictionary<string, object> publicFiles = new Dictionary<string, object>(StringComparer.Ordinal);
      Dictionary<string, object> secretFiles = new Dictionary<string, object>(StringComparer.Ordinal);

      foreach(Key key in keys)
      {
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
      // if we're using a non-default trust database, reference it
      if(trustDb != null) args += "--trustdb-name " + EscapeArg(trustDb) + " ";
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
  static ProcessStartInfo GetProcessStartInfo(string exePath, string args, bool allowTTY)
  {
    ProcessStartInfo psi = new ProcessStartInfo();
    psi.Arguments              = (allowTTY ? null : "--no-tty ") + "--no-options --display-charset utf-8 " + args;
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

  static bool IsHexDigit(char c)
  {
    if(c >= '0' && c <= '9') return true;
    else
    {
      c = char.ToLowerInvariant(c);
      return c >= 'a' && c <= 'f';
    }
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

  static readonly ReadOnlyListWrapper<UserAttribute> NoAttributes =
    new ReadOnlyListWrapper<UserAttribute>(new UserAttribute[0]);
  static readonly ReadOnlyListWrapper<string> NoRevokers = new ReadOnlyListWrapper<string>(new string[0]);
  static readonly ReadOnlyListWrapper<KeySignature> NoSignatures =
    new ReadOnlyListWrapper<KeySignature>(new KeySignature[0]);
  static readonly Regex versionLineRe = new Regex(@"^(\w+):\s*(.+)", RegexOptions.Singleline);
  static readonly Regex commaSepRe = new Regex(@",\s*", RegexOptions.Singleline);
  static readonly Regex cEscapeRe = new Regex(@"\\x[0-9a-f]{2}",
    RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

  enum EditCommandResult
  {
    Done, Continue, Next
  }

  sealed class EditSubkey
  {
    public string SelectId
    {
      get { return Index.ToString(CultureInfo.InvariantCulture); }
    }

    public string Fingerprint;
    public int Index;
  }

  sealed class EditUserId
  {
    public string SelectId
    {
      get { return Index.ToString(CultureInfo.InvariantCulture); }
    }

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
    public int Index;
    public bool IsAttribute, Primary, Selected;
  }

  sealed class EditKey
  {
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

    public readonly List<EditUserId> UserIds = new List<EditUserId>();
    public readonly List<EditSubkey> Subkeys = new List<EditSubkey>();
  }

  abstract class EditCommand
  {
    public virtual EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                             Command cmd, string promptId)
    {
      if(string.Equals(promptId, "passphrase.enter", StringComparison.Ordinal))
      {
        return EditCommandResult.Continue;
      }
      else throw new NotImplementedException("Unhandled prompt: " + promptId);
    }

    public virtual EditCommandResult SendCipherPassword(EditKey originalKey, EditKey key, Command cmd)
    {
      throw new NotImplementedException("Unhandled cipher passphrase.");
    }

    protected static PGPException UnexpectedError(string problem)
    {
      return new PGPException("Key edit problem: "+problem);
    }
  }

  abstract class AddUidBase : EditCommand
  {
    public AddUidBase(UserPreferences preferences, bool addAttribute)
    {
      this.preferences  = preferences;
      this.addAttribute = addAttribute;
    }

    protected void AddPreferenceCommands(Queue<EditCommand> commands, EditKey originalKey)
    {
      if(preferences != null)
      {
        commands.Enqueue(new SelectLastUid());

        if(preferences.Primary) commands.Enqueue(new SetPrimary());

        if(preferences.Keyserver != null)
        {
          commands.Enqueue(new RawCommand("keyserver " + preferences.Keyserver.AbsoluteUri));
        }

        if(preferences.PreferredCiphers.Count != 0 || preferences.PreferredCompressions.Count != 0 ||
             preferences.PreferredHashes.Count != 0)
        {
          commands.Enqueue(new SetAlgoPrefs(preferences));
        }

        if(!preferences.Primary)
        {
          EditUserId id = addAttribute ? originalKey.PrimaryAttribute : originalKey.PrimaryUserId;
          if(id != null)
          {
            commands.Enqueue(new SelectUid(id));
            commands.Enqueue(new SetPrimary());
          }
        }
      }
    }

    readonly UserPreferences preferences;
    readonly bool addAttribute;
  }

  sealed class AddPhotoCommand : AddUidBase
  {
    public AddPhotoCommand(string filename, UserPreferences preferences) : base(preferences, true)
    {
      if(string.IsNullOrEmpty(filename)) throw new ArgumentException();
      this.filename = filename;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key, 
                                              Command cmd, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          cmd.SendLine("addphoto " + filename);
          sentCommand = true;
          return EditCommandResult.Continue;
        }
        else
        {
          AddPreferenceCommands(commands, originalKey);
          return EditCommandResult.Next;
        }
      }
      else if(string.Equals(promptId, "photoid.jpeg.size", StringComparison.Ordinal))
      {
        cmd.SendLine("Y");
        return EditCommandResult.Continue;
      }
      else return base.Process(commands, originalKey, key, cmd, promptId);
    }

    readonly string filename;
    bool sentCommand;
  }

  sealed class AddRevoker : EditCommand
  {
    public AddRevoker(string fingerprint)
    {
      this.fingerprint = fingerprint;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key, Command cmd, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          cmd.SendLine("addrevoker");
          sentCommand = true;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "keyedit.add_revoker", StringComparison.Ordinal))
      {
        if(!sentFingerprint)
        {
          cmd.SendLine(fingerprint);
          sentFingerprint = true;
        }
        else throw UnexpectedError("Adding the designated revoker failed.");
      }
      else if(string.Equals(promptId, "keyedit.add_revoker.okay", StringComparison.Ordinal))
      {
        cmd.SendLine("Y");
      }
      else return base.Process(commands, originalKey, key, cmd, promptId);

      return EditCommandResult.Continue;
    }

    readonly string fingerprint;
    bool sentCommand, sentFingerprint;
  }

  sealed class AddSubkeyCommand : EditCommand
  {
    public AddSubkeyCommand(string type, int length, DateTime? expiration)
    {
      // GPG only supports specific RSA subkey types, like RSA-E and RSA-S
      if(string.Equals(type, SubkeyType.RSA, StringComparison.OrdinalIgnoreCase))
      {
        throw new KeyCreationFailedException(FailureReason.UnsupportedAlgorithm, "Please specify an encryption-only "+
                                             "or signing-only RSA key. Generic RSA keys not allowed here.");
      }

      this.type           = type;
      this.length         = length;
      this.expiration     = expiration;
      this.expirationDays = GetExpirationDays(expiration);

      this.isDSA  = string.Equals(type, SubkeyType.DSA, StringComparison.OrdinalIgnoreCase);
      this.isELG  = type == null || string.Equals(type, SubkeyType.ElGamal, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(type, SubkeyType.ElGamalEncryptOnly, StringComparison.OrdinalIgnoreCase);
      this.isRSAE = string.Equals(type, SubkeyType.RSAEncryptOnly, StringComparison.OrdinalIgnoreCase);
      this.isRSAS = string.Equals(type, SubkeyType.RSASignOnly, StringComparison.OrdinalIgnoreCase);

      if(!isDSA && !isELG && !isRSAE && !isRSAS)
      {
        throw new KeyCreationFailedException(FailureReason.UnsupportedAlgorithm, "Unsupported subkey type: " + type);
      }

      int maxLength = isDSA ? 3072 : 4096;
      if(length < 0 || length > maxLength)
      {
        throw new KeyCreationFailedException(FailureReason.None, "Key length " +
                                             length.ToString(CultureInfo.InvariantCulture) + " is not supported.");
      }
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              Command cmd, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          cmd.SendLine("addkey");
          sentCommand = true;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "keygen.algo", StringComparison.Ordinal))
      {
        if(!sentAlgo)
        {
          if(isDSA) cmd.SendLine("2");
          else if(isELG) cmd.SendLine("4");
          else if(isRSAS) cmd.SendLine("5");
          else cmd.SendLine("6");
          sentAlgo = true;
        }
        else
        {
          throw new KeyCreationFailedException(FailureReason.UnsupportedAlgorithm, "Unsupported subkey type: " + type);
        }
      }
      else if(string.Equals(promptId, "keygen.size", StringComparison.Ordinal))
      {
        if(!sentLength)
        {
          cmd.SendLine(length.ToString(CultureInfo.InvariantCulture));
          sentLength = true;
        }
        else
        {
          throw new KeyCreationFailedException(FailureReason.None, "Key length " +
                                               length.ToString(CultureInfo.InvariantCulture) + " is not supported.");
        }
      }
      else if(string.Equals(promptId, "keygen.valid", StringComparison.Ordinal))
      {
        if(!sentExpiration)
        {
          cmd.SendLine(expirationDays.ToString(CultureInfo.InvariantCulture));
          sentExpiration = true;
        }
        else
        {
          throw new KeyCreationFailedException(FailureReason.None, "Expiration date " + Convert.ToString(expiration) +
                                               " is not supported.");
        }
      }
      else return base.Process(commands, originalKey, key, cmd, promptId);

      return EditCommandResult.Continue;
    }

    readonly string type;
    readonly DateTime? expiration;
    readonly int length, expirationDays;
    readonly bool isDSA, isELG, isRSAE, isRSAS;
    bool sentCommand, sentAlgo, sentLength, sentExpiration;
  }

  sealed class AddUid : AddUidBase
  {
    public AddUid(string realName, string email, string comment, UserPreferences preferences)
      : base(preferences, false)
    {
      this.realName    = realName;
      this.email       = email;
      this.comment     = comment;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key, 
                                              Command cmd, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!startedUid)
        {
          cmd.SendLine("adduid");
          startedUid = true;
        }
        else throw UnexpectedError("Adding a new user ID seemed to fail.");
      }
      else if(string.Equals(promptId, "keygen.name", StringComparison.Ordinal)) cmd.SendLine(realName);
      else if(string.Equals(promptId, "keygen.email", StringComparison.Ordinal)) cmd.SendLine(email);
      else if(string.Equals(promptId, "keygen.comment", StringComparison.Ordinal))
      {
        cmd.SendLine(comment);
        AddPreferenceCommands(commands, originalKey);
        return EditCommandResult.Done;
      }
      else return base.Process(commands, originalKey, key, cmd, promptId);

      return EditCommandResult.Continue;
    }

    readonly string realName, email, comment;
    bool startedUid;
  }

  sealed class ChangeExpirationCommand : EditCommand
  {
    public ChangeExpirationCommand(DateTime? expiration)
    {
      this.expiration     = expiration;
      this.expirationDays = GetExpirationDays(expiration);
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              Command cmd, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          cmd.SendLine("expire");
          sentCommand = true;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "keygen.valid", StringComparison.Ordinal))
      {
        if(!sentExpiration)
        {
          cmd.SendLine(expirationDays.ToString(CultureInfo.InvariantCulture));
          sentExpiration = true;
        }
        else throw UnexpectedError("Changing expiration date to " + Convert.ToString(expiration) + " failed.");
      }
      else return base.Process(commands, originalKey, key, cmd, promptId);

      return EditCommandResult.Continue;
    }

    readonly DateTime? expiration;
    readonly int expirationDays;
    bool sentCommand, sentExpiration;
  }

  sealed class ChangePasswordCommand : EditCommand
  {
    public ChangePasswordCommand(SecureString password)
    {
      this.password = password;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              Command cmd, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          cmd.SendLine("passwd");
          sentCommand = true;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "change_passwd.empty.okay", StringComparison.Ordinal))
      {
        cmd.SendLine("Y");
      }
      else return base.Process(commands, originalKey, key, cmd, promptId);

      return EditCommandResult.Continue;
    }

    public override EditCommandResult SendCipherPassword(EditKey originalKey, EditKey key, Command cmd)
    {
      cmd.SendPassword(password, false);
      return EditCommandResult.Continue;
    }

    readonly SecureString password;
    bool sentCommand;
  }

  sealed class GetPrefs : EditCommand
  {
    public GetPrefs(UserPreferences preferences)
    {
      if(preferences == null) throw new ArgumentNullException();
      preferences.PreferredCiphers.Clear();
      preferences.PreferredCompressions.Clear();
      preferences.PreferredHashes.Clear();
      this.preferences = preferences;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key, 
                                              Command cmd, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        EditUserId selectedId = key.SelectedUserId;
        if(selectedId == null) throw UnexpectedError("No user ID is selected.");

        if(!sentCommand)
        {
          cmd.SendLine("showpref");

          foreach(string pref in selectedId.Prefs.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
          {
            int id = int.Parse(pref.Substring(1));
            if(pref[0] == 'S') preferences.PreferredCiphers.Add((OpenPGPCipher)id);
            else if(pref[0] == 'H') preferences.PreferredHashes.Add((OpenPGPHashAlgorithm)id);
            else if(pref[0] == 'Z') preferences.PreferredCompressions.Add((OpenPGPCompression)id);
          }
          preferences.Primary = selectedId.Primary;

          sentCommand = true;
          return EditCommandResult.Continue;
        }
        else return EditCommandResult.Next;
      }
      else return base.Process(commands, originalKey, key, cmd, promptId);
    }

    // TODO: GPG writes this on STDERR like a bastard, so we currently can't get at it...
    /*public override EditCommandResult Process(string line)
    {
      line = line.Trim();
      if(line.StartsWith("Preferred keyserver: ", StringComparison.Ordinal))
      {
        preferences.Keyserver = new Uri(line.Substring(21));
        return EditCommandResult.Done;
      }
      else return EditCommandResult.Continue;
    }*/

    readonly UserPreferences preferences;
    bool sentCommand;
  }

  sealed class QuitEditCommand : EditCommand
  {
    public QuitEditCommand(bool save)
    {
      this.save = save;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key, 
                                              Command cmd, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        cmd.SendLine(save ? "save" : "quit");
      }
      else if(string.Equals(promptId, "keyedit.save.okay", StringComparison.Ordinal))
      {
        cmd.SendLine(save ? "Y" : "N");
      }
      else return base.Process(commands, originalKey, key, cmd, promptId);

      return EditCommandResult.Continue;
    }

    readonly bool save;
  }

  sealed class RawCommand : EditCommand
  {
    public RawCommand(string command)
    {
      this.command = command;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key, 
                                              Command cmd, string promptId)
    {
 	    cmd.SendLine(command);
      return EditCommandResult.Done;
    }

    readonly string command;
  }

  sealed class SelectLastUid : EditCommand
  {
    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key, 
                                              Command cmd, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
 	      for(int i=0; i<key.UserIds.Count-1; i++)
        {
          if(key.UserIds[i].Selected)
          {
            cmd.SendLine("uid -");
            return EditCommandResult.Continue;
          }
        }

        if(!key.UserIds[key.UserIds.Count-1].Selected)
        {
          cmd.SendLine("uid " + key.UserIds.Count.ToString(CultureInfo.InvariantCulture));
          return EditCommandResult.Done;
        }

        return EditCommandResult.Next;
      }
      else return base.Process(commands, originalKey, key, cmd, promptId);
    }
  }

  sealed class SelectSubkey : EditCommand
  {
    public SelectSubkey(string fingerprint)
    {
      if(string.IsNullOrEmpty(fingerprint)) throw new ArgumentException("Fingprint was null or empty.");
      this.fingerprint = fingerprint;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key,
                                              Command cmd, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!clearedSelection)
        {
          cmd.SendLine("key -");
          clearedSelection = true;
          return EditCommandResult.Continue;
        }
        else
        {
          int index;
          for(index=0; index < key.Subkeys.Count; index++)
          {
            if(string.Equals(fingerprint, key.Subkeys[index].Fingerprint, StringComparison.Ordinal)) break;
          }

          if(index == key.Subkeys.Count) throw UnexpectedError("No subkey found with fingerprint " + fingerprint);

          cmd.SendLine("key " + (index+1).ToString(CultureInfo.InvariantCulture));
          return EditCommandResult.Done;
        }
      }
      else return base.Process(commands, originalKey, key, cmd, promptId);
    }

    readonly string fingerprint;
    bool clearedSelection;
  }

  sealed class SelectUid : EditCommand
  {
    public SelectUid(EditUserId id)
    {
      if(id == null) throw new ArgumentNullException();
      this.id = id;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key, 
                                              Command cmd, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        int index;
        for(index=0; index < key.UserIds.Count; index++)
        {
          if(id.Matches(key.UserIds[index]))
          {
            for(int i=index+1; i < key.UserIds.Count; i++)
            {
              if(id.Matches(key.UserIds[i])) throw UnexpectedError("Multiple user IDs matched " + id.ToString());
            }
            break;
          }
        }

        if(index == key.UserIds.Count) throw UnexpectedError("No user ID matched " + id.ToString());

        for(int i=0; i < key.UserIds.Count; i++)
        {
          if(i != index && key.UserIds[i].Selected)
          {
            cmd.SendLine("uid -");
            return EditCommandResult.Continue;
          }
        }

        if(!key.UserIds[index].Selected)
        {
          cmd.SendLine("uid " + (index+1).ToString(CultureInfo.InvariantCulture));
          return EditCommandResult.Done;
        }

        return EditCommandResult.Next;
      }
      else return base.Process(commands, originalKey, key, cmd, promptId);
    }

    readonly EditUserId id;
  }

  sealed class SetAlgoPrefs : EditCommand
  {
    public SetAlgoPrefs(UserPreferences preferences)
    {
      StringBuilder prefString = new StringBuilder();

      foreach(OpenPGPCipher cipher in preferences.PreferredCiphers)
      {
        prefString.Append(" S").Append(((int)cipher).ToString(CultureInfo.InvariantCulture));
      }
      foreach(OpenPGPHashAlgorithm hash in preferences.PreferredHashes)
      {
        prefString.Append(" H").Append(((int)hash).ToString(CultureInfo.InvariantCulture));
      }
      foreach(OpenPGPCompression compression in preferences.PreferredCompressions)
      {
        prefString.Append(" Z").Append(((int)compression).ToString(CultureInfo.InvariantCulture));
      }

      this.prefString = prefString.ToString();
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key, 
                                              Command cmd, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentPrefs)
        {
          cmd.SendLine("setpref " + prefString);
          sentPrefs = true;
          return EditCommandResult.Continue;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "keyedit.setpref.okay", StringComparison.Ordinal))
      {
        cmd.SendLine("Y");
        return EditCommandResult.Done;
      }
      else return base.Process(commands, originalKey, key, cmd, promptId);
    }

    string prefString;
    bool sentPrefs;
  }

  sealed class SetPrimary : EditCommand
  {
    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key, 
                                              Command cmd, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(key.SelectedUserId == null) throw UnexpectedError("Can't set primary uid because no uid is selected.");
        if(!key.SelectedUserId.Primary)
        {
          cmd.SendLine("primary");
          return EditCommandResult.Done;
        }
        else return EditCommandResult.Next;
      }
      else return base.Process(commands, originalKey, key, cmd, promptId);
    }
  }

  sealed class SetTrust : EditCommand
  {
    public SetTrust(TrustLevel level)
    {
      this.level = level;
    }

    public override EditCommandResult Process(Queue<EditCommand> commands, EditKey originalKey, EditKey key, 
                                              Command cmd, string promptId)
    {
      if(string.Equals(promptId, "keyedit.prompt", StringComparison.Ordinal))
      {
        if(!sentCommand)
        {
          cmd.SendLine("trust");
          sentCommand = true;
        }
        else return EditCommandResult.Next;
      }
      else if(string.Equals(promptId, "edit_ownertrust.value", StringComparison.Ordinal))
      {
        switch(level)
        {
          case TrustLevel.Never: cmd.SendLine("2"); break;
          case TrustLevel.Marginal: cmd.SendLine("3"); break;
          case TrustLevel.Full: cmd.SendLine("4"); break;
          case TrustLevel.Ultimate: cmd.SendLine("5"); break;
          default: cmd.SendLine("1"); break;
        }
      }
      else if(string.Equals(promptId, "edit_ownertrust.set_ultimate.okay", StringComparison.Ordinal))
      {
        cmd.SendLine("Y");
      }
      else return base.Process(commands, originalKey, key, cmd, promptId);

      return EditCommandResult.Continue;
    }

    readonly TrustLevel level;
    bool sentCommand;
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/AddDesignatedRevoker/*" />
  public override void AddDesignatedRevoker(PrimaryKey key, PrimaryKey revokerKey)
  {
    if(revokerKey == null) throw new ArgumentNullException();

    if(key.Keyring == null && revokerKey.Keyring != null ||
       key.Keyring != null && !key.Keyring.Equals(revokerKey.Keyring))
    {
      throw new NotSupportedException("Adding a revoker from a different keyring is not supported.");
    }

    if(string.IsNullOrEmpty(revokerKey.Fingerprint))
    {
      throw new ArgumentException("The revoker key has no fingerprint.");
    }

    DoEdit(key, null, new AddRevoker(revokerKey.Fingerprint));
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/AddPhoto4/*" />
  public override void AddPhoto(PrimaryKey key, Stream image, OpenPGPImageType imageFormat,
                                UserPreferences preferences)
  {
    if(key == null || image == null) throw new ArgumentNullException();
    if(imageFormat != OpenPGPImageType.Jpeg)
    {
      throw new NotImplementedException("Only JPEG photos are currently supported.");
    }

    string filename = Path.GetTempFileName();
    try
    {
      using(FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Write)) IOH.CopyStream(image, file);
      DoEdit(key, null, new AddPhotoCommand(filename, preferences)); 
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

    if(ContainsControlCharacters(realName + email + comment))
    {
      throw new ArgumentException("Name, email, or comment contains control characters. Remove them.");
    }

    DoEdit(key, "--allow-freeform-uid", new AddUid(realName, email, comment, preferences));
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/AddSubkey/*" />
  public override void AddSubkey(PrimaryKey key, string keyType, int keyLength, DateTime? expiration)
  {
    DoEdit(key, keyLength == 0 ? null : "--enable-dsa2", new AddSubkeyCommand(keyType, keyLength, expiration));
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ChangeExpiration/*" />
  public override void ChangeExpiration(Key key, DateTime? expiration)
  {
    if(key == null) throw new ArgumentNullException();

    Subkey subkey = key as Subkey;
    if(subkey == null)
    {
      DoEdit(key.GetPrimaryKey(), null, new ChangeExpirationCommand(expiration));
    }
    else
    {
      DoEdit(key.GetPrimaryKey(), null, new SelectSubkey(subkey.Fingerprint), new ChangeExpirationCommand(expiration));
    }
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ChangePassword/*" />
  public override void ChangePassword(PrimaryKey key, SecureString password)
  {
    DoEdit(key, null, new ChangePasswordCommand(password));
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/CleanKeys/*" />
  public override void CleanKeys(PrimaryKey[] keys)
  {
    foreach(PrimaryKey key in keys)
    {
      if(key == null) throw new ArgumentNullException("A key was null.");
      DoEdit(key, null, new RawCommand("clean"));
    }
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/MinimizeKeys/*" />
  public override void MinimizeKeys(PrimaryKey[] keys)
  {
    foreach(PrimaryKey key in keys)
    {
      if(key == null) throw new ArgumentNullException("A key was null.");
      DoEdit(key, null, new RawCommand("minimize"));
    }
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/DisableKeys/*" />
  public override void DisableKeys(PrimaryKey[] keys)
  {
    foreach(PrimaryKey key in keys)
    {
      if(key == null) throw new ArgumentNullException("A key was null.");
      DoEdit(key, null, new RawCommand("disable"));
    }
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/EnableKeys/*" />
  public override void EnableKeys(PrimaryKey[] keys)
  {
    foreach(PrimaryKey key in keys)
    {
      if(key == null) throw new ArgumentNullException("A key was null.");
      DoEdit(key, null, new RawCommand("enable"));
    }
  }

  public override void DeleteAttributes(UserAttribute[] attributes)
  {
    throw new NotImplementedException();
  }

  public override void DeleteSignatures(KeySignature[] signatures)
  {
    throw new NotImplementedException();
  }

  public override void DeleteSubkeys(Subkey[] subkeys)
  {
    throw new NotImplementedException();
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetPreferences/*" />
  public override UserPreferences GetPreferences(UserAttribute user)
  {
    if(user == null) throw new ArgumentNullException();
    if(user.Key == null) throw new ArgumentException("The user attribute must be associated with a key.");

    // TODO: FIXME: currently, this cannot retrieve the user's preferred keyring (because GPG writes it to STDERR
    // rather than STDOUT...)

    UserPreferences preferences = new UserPreferences();
    DoEdit(user.Key, null, new RawCommand("uid " + user.Id), new GetPrefs(preferences));
    return preferences;
  }

  public override void RevokeAttributes(UserAttribute[] attributes)
  {
    throw new NotImplementedException();
  }

  public override void RevokeKeys(PrimaryKey[] keys)
  {
    throw new NotImplementedException();
  }

  public override void RevokeSignatures(KeySignature[] signatures)
  {
    throw new NotImplementedException();
  }

  public override void RevokeSubkeys(Subkey[] subkeys)
  {
    throw new NotImplementedException();
  }

  public override void SetPreferences(UserAttribute user, UserPreferences preferences)
  {
    throw new NotImplementedException();
  }

  public override void SetTrustLevel(PrimaryKey key, TrustLevel trust)
  {
    DoEdit(key, null, new SetTrust(trust));
  }

  public override void SignKey(PrimaryKey keyToSign, PrimaryKey signingKey, KeySigningOptions options)
  {
    throw new NotImplementedException();
  }

  public override void SignKey(UserId userId, PrimaryKey signingKey, KeySigningOptions options)
  {
    throw new NotImplementedException();
  }
}
#endregion

} // namespace AdamMil.Security.PGP