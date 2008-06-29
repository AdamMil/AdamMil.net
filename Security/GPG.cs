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
      case OpenPGPKeyType.DSA: return MasterKeyType.DSA;
      case OpenPGPKeyType.Elgamal: return SubkeyType.Elgamal;
      case OpenPGPKeyType.ElgamalEncryptOnly: return SubkeyType.ElgamalEncryptOnly;
      case OpenPGPKeyType.RSA: return MasterKeyType.RSA;
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
      return new DateTime(1970, 1, 1).AddSeconds(long.Parse(str, CultureInfo.InvariantCulture));
    }
    else // the date is in ISO8601 format. DateTime.Parse() can handle it.
    {
      return DateTime.Parse(str, CultureInfo.InvariantCulture);
    }
  }

  /// <summary>Parses an argument from a GPG status message into a timestamp, or null if the timestamp is zero.</summary>
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
    // TODO: implement symmetric encryption. it's gonna be a bitch combined with signing, because of the 1-password limit
    if(encryptionOptions != null && encryptionOptions.Password != null) throw new NotImplementedException();

    if(sourceData == null || destination == null || encryptionOptions == null && signingOptions == null)
    {
      throw new ArgumentNullException();
    }

    if(encryptionOptions != null &&
       encryptionOptions.Recipients.Count == 0 && encryptionOptions.HiddenRecipients.Count == 0)
    {
      throw new ArgumentException("No recipients were specified.");
    }

    if(signingOptions != null)
    {
      if(signingOptions.Signers.Count == 0) throw new ArgumentException("No signers were specified.");
      if(signingOptions.Signers.Count > 1)
      {
        throw new NotSupportedException("Unfortunately, GPG only supports sending a single password, so only a "+
          "single signing key can be supported.");
      }
    }

    string args = GetOutputArgs(outputOptions);

    if(encryptionOptions != null)
    {
      List<Key> totalRecipients = new List<Key>();
      totalRecipients.AddRange(encryptionOptions.Recipients);
      totalRecipients.AddRange(encryptionOptions.HiddenRecipients);
      args += GetKeyringArgs(totalRecipients, true, false);

      if(!string.IsNullOrEmpty(encryptionOptions.Cipher))
      {
        args += "--cipher-algo " + EscapeArg(encryptionOptions.Cipher) + " ";
      }

      foreach(Key key in encryptionOptions.Recipients) args += "-r " + key.Fingerprint + " ";
      foreach(Key key in encryptionOptions.HiddenRecipients) args += "-R " + key.Fingerprint + " ";

      args += "-e ";
    }

    if(signingOptions != null)
    {
      args += GetKeyringArgs(signingOptions.Signers, true, true);
      if(!string.IsNullOrEmpty(signingOptions.Hash)) args += "--digest-algo "+EscapeArg(signingOptions.Hash)+" ";
      foreach(Key key in signingOptions.Signers) args += "-u " + key.Fingerprint + " ";
      args += signingOptions != null && signingOptions.Detached ? "-b" : "-s";
    }

    Command cmd = Execute(args, true, false, StreamHandling.Unprocessed, StreamHandling.ProcessText,
                          signingOptions == null ? null : signingOptions.Signers[0]);
    using(ManualResetEvent ready = new ManualResetEvent(false))
    using(cmd)
    {
      cmd.StatusMessageReceived += delegate(StatusMessage msg)
      {
        switch(msg.Type)
        {
          case StatusMessageType.BadPassphrase:
            {
              BadPassphraseMessage m = (BadPassphraseMessage)msg;
              OnInvalidPassword(m.KeyId);
              throw new WrongPassphraseException("Wrong password for key "+m.KeyId);
            }

          case StatusMessageType.InvalidRecipient:
            {
              InvalidRecipientMessage m = (InvalidRecipientMessage)msg;
              throw new InvalidRecipientException("Invalid recipient "+m.Recipient+". "+m.ReasonText);
            }

          case StatusMessageType.BeginEncryption:
          case StatusMessageType.BeginSigning:
            ready.Set();
            break;
        }
      };

      cmd.Start();

      // wait until the process has exited or it's time to write the data or the user aborted
      while(!ready.WaitOne(1000, false) && !cmd.Process.HasExited) { }

      if(!cmd.Process.HasExited)
      {
        if(!WriteStreamToProcess(sourceData, cmd.Process)) throw new Exception("GPG aborted early."); // TODO: exception class
        IOH.CopyStream(cmd.Process.StandardOutput.BaseStream, destination);
        cmd.WaitForExit();
      }
    }

    cmd.CheckExitCode();
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/Decrypt/*"/>
  public override Signature[] Decrypt(Stream ciphertext, Stream destination, DecryptionOptions decryptOptions)
  {
    throw new NotImplementedException();
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

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetPublicKeys2/*"/>
  public override PrimaryKey[] GetPublicKeys(KeySignatures signatures, Keyring[] keyrings, bool includeDefaultKeyring)
  {
    return GetKeys(signatures, keyrings, includeDefaultKeyring, false);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GetSecretKeys2/*"/>
  public override PrimaryKey[] GetSecretKeys(Keyring[] keyrings, bool includeDefaultKeyring)
  {
    return GetKeys(KeySignatures.Ignore, keyrings, includeDefaultKeyring, true);
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/DeleteKey/*"/>
  public override void DeleteKey(Key key, KeyDeletion deletion)
  {
    throw new NotImplementedException();
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ExportPublicKeys/*"/>
  public override void ExportPublicKeys(Key[] key, ExportOptions options, Stream destination, OutputFormat format)
  {
    throw new NotImplementedException();
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ExportPublicKeys2/*"/>
  public override void ExportPublicKeys(Keyring[] keyrings, bool includeDefaultKeyring, ExportOptions options, Stream destination, OutputFormat format)
  {
    throw new NotImplementedException();
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ExportSecretKeys/*"/>
  public override void ExportSecretKeys(Key[] key, ExportOptions options, Stream destination, OutputFormat format)
  {
    throw new NotImplementedException();
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ExportSecretKeys2/*"/>
  public override void ExportSecretKeys(Keyring[] keyrings, bool includeDefaultKeyring, ExportOptions options, Stream destination, OutputFormat format)
  {
    throw new NotImplementedException();
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/ImportKeys3/*"/>
  public override void ImportKeys(Stream source, Keyring keyring, ImportOptions options)
  {
    throw new NotImplementedException();
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/GenerateRandomData/*"/>
  public override void GenerateRandomData(Randomness level, byte[] buffer, int index, int count)
  {
    if(buffer == null) throw new ArgumentNullException();
    if(index < 0 || count < 0 || index+count > buffer.Length) throw new ArgumentOutOfRangeException();
    if(count == 0) return;

    string levelArg;
    if(level == Randomness.Weak) levelArg = "0";
    else if(level == Randomness.TooStrong) levelArg = "2";
    else levelArg = "1";

    // "gpg --gen-random QUALITY COUNT" writes random COUNT bytes to standard output. QUALITY is a value from 0 to 2
    // representing the quality of the random number generator to use.
    Command cmd = Execute("--gen-random "+levelArg+" "+count.ToString(CultureInfo.InvariantCulture),
                          false, true, StreamHandling.Unprocessed, StreamHandling.ProcessText, null);
    using(cmd)
    {
      cmd.Start();
      do
      {
        int read = cmd.Process.StandardOutput.BaseStream.Read(buffer, index, count);
        if(read == 0) throw new Exception("GPG aborted early!"); // TODO: use the proper exception class
        index += read;
        count -= read;
      } while(count != 0);
    }

    cmd.CheckExitCode();
  }

  /// <include file="documentation.xml" path="/Security/PGPSystem/Hash/*"/>
  public override byte[] Hash(string hashAlgorithm, Stream data)
  {
    if(data == null) throw new ArgumentNullException();
    
    if(hashAlgorithm == null || hashAlgorithm == HashAlgorithm.Default) hashAlgorithm = HashAlgorithm.SHA1;
    if(hashAlgorithm.Length == 0) throw new ArgumentException("Unspecified hash algorithm.");

    // "gpg --print-md ALGO" hashes data presented on standard input. if the algorithm is not supported, gpg exits
    // immediately with error code 2. otherwise, it consumes all available input, and then prints the hash in a
    // human-readable form, with hex digits nicely formatted into blocks and lines. we'll feed it all the input and
    // then read the output.
    List<byte> hash = new List<byte>();
    Command cmd = Execute("--print-md "+EscapeArg(hashAlgorithm), false, false,
                          StreamHandling.Unprocessed, StreamHandling.ProcessText, null);
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
      else
      {
        cmd.Dispose();
        if(cmd.ExitCode == 2) throw new ArgumentException("Likely unsupported hash algorithm: "+hashAlgorithm);
        else throw new Exception("GPG aborted early!"); // TODO: use the proper exception class
      }
    }

    cmd.CheckExitCode();
    return hash.ToArray();
  }

  /// <summary>Initializes a new <see cref="ExeGPG"/> object with path the path to the GPG executable. It is assumed
  /// that the executable file will not be altered during the lifetime of this object.
  /// </summary>
  public void Initialize(string exePath)
  {
    FileInfo info;
    try { info = new FileInfo(exePath); }
    catch(Exception ex) { throw new ArgumentException("The executable path is not valid.", ex); }

    if(!info.Exists) throw new FileNotFoundException();

    Process process;
    try { process = Execute(exePath, "--version"); }
    catch(Exception ex) { throw new ArgumentException("The file could not be executed.", ex); }

    // read the GPG --version output to determine its configuration
    process.StandardInput.Close(); // the program should not expect any input

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

    Exit(process);
    this.exePath = info.FullName;
  }

  /// <summary>Determines how a process' stream will be handled.</summary>
  protected enum StreamHandling
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
  protected delegate void StatusMessageHandler(StatusMessage message);

  #region Command
  protected class Command : IDisposable
  {
    public Command(ProcessStartInfo psi, InheritablePipe statusPipe, InheritablePipe passwordPipe, byte[] password,
                   bool closeStdInput, StreamHandling stdOut, StreamHandling stdError)
    {
      if(psi == null) throw new ArgumentNullException();
      this.psi           = psi;
      this.statusPipe    = statusPipe;
      this.passwordPipe  = passwordPipe;
      this.password      = password;
      this.closeStdInput = closeStdInput;
      this.outHandling   = stdOut;
      this.errorHandling = stdError;
    }

    ~Command()
    {
      Dispose(true);
    }

    public event StatusMessageHandler StatusMessageReceived;

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

    public bool IsDone
    {
      get { return outDone && errorDone && statusDone && process.HasExited; }
    }

    public Process Process
    {
      get { return process; }
    }

    public void CheckExitCode()
    {
      if(ExitCode != 0 && ExitCode != 1) throw new Exception("GPG returned failure code "+ExitCode.ToString()); // TODO: exception class
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);
      Dispose(false);
    }

    public void Start()
    {
      try { process = Process.Start(psi); }
      catch
      {
        Dispose();
        throw;
      }

      if(closeStdInput) process.StandardInput.Close();

      if(passwordPipe != null)
      {
        passwordStream = new FileStream(new SafeFileHandle(passwordPipe.ServerHandle, false), FileAccess.Write);
        passwordStream.BeginWrite(password, 0, password.Length, delegate(IAsyncResult result)
        {
          Stream stream = passwordStream;
          if(stream != null)
          {
            stream.EndWrite(result);
            stream.Close();
          }

          byte[] array = password;
          if(array != null)
          {
            Array.Clear(array, 0, array.Length);
            password = null;
          }
        }, null);
      }

      if(statusPipe != null)
      {
        statusStream = new FileStream(new SafeFileHandle(statusPipe.ServerHandle, false), FileAccess.Read);
        statusBuffer = new byte[4096];
        OnStatusRead(null);
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
        OnStdOutRead(null);
      }

      if(errorHandling == StreamHandling.Close || errorHandling == StreamHandling.Unprocessed)
      {
        if(errorHandling == StreamHandling.Close) process.StandardError.Close();
        errorDone = true;
      }
      else
      {
        errorBuffer = new byte[4096];
        OnStdErrorRead(null);
      }
    }

    public void WaitForExit()
    {
      Process.WaitForExit();

      bool closedPipe = false;
      while(!IsDone)
      {
        if(!closedPipe && statusPipe != null)
        {
          statusPipe.CloseClient();
          closedPipe = true;
        }

        System.Threading.Thread.Sleep(0);
      }
    }

    protected virtual void OnStdOutLine(string line)
    {
Debugger.Log(0, "", "OUT: "+line+"\n");
    }

    protected virtual void OnStdErrorLine(string line)
    {
Debugger.Log(0, "", "ERR: "+line+"\n");
    }

    protected virtual void OnStatusMessage(StatusMessage message)
    {
      if(StatusMessageReceived != null) StatusMessageReceived(message);
    }

    delegate void LineProcessor(string line);

    void Dispose(bool finalizing)
    {
      if(!disposed)
      {
        if(passwordStream != null)
        {
          passwordStream.Dispose();
          passwordStream = null;
        }

        if(passwordPipe != null)
        {
          passwordPipe.Dispose();
          passwordPipe = null;
        }

        byte[] password = this.password;
        if(password != null)
        {
          Array.Clear(password, 0, password.Length);
          this.password = null;
        }

        if(statusPipe != null)
        {
          statusStream.Dispose();
          statusStream = null;

          statusPipe.CloseServer();
          if(process != null) Exit(process);
          statusPipe.Dispose();
          statusPipe = null;
        }
        else if(process != null) Exit(process);

        statusDone = outDone = errorDone = disposed = true;
      }
    }

    void HandleStream(StreamHandling handling, Stream stream, IAsyncResult result, ref byte[] buffer,
                      ref int bufferBytes, ref bool bufferDone, LineProcessor processor, AsyncCallback callback)
    {
      if(stream == null)
      {
        bufferDone = true;
      }
      else
      {
        if(result != null)
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

        if(!stream.CanRead) // check CanRead as a way to prevent the ObjectDisposedException if possible
        {
          bufferDone = true;
        }
        else
        {
          try { stream.BeginRead(buffer, bufferBytes, buffer.Length - bufferBytes, callback, null); }
          catch(ObjectDisposedException) { bufferDone = true; }
        }
      }
    }

    // 0420: reference to a volatile field will not be treated as volatile. we aren't worried about this because the
    // field is only written to, not read.
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
        string keyword;
        string[] arguments;
        SplitDecodedLine(binaryLine, Decode(binaryLine), out keyword, out arguments);

        if(keyword != null)
        {
          StatusMessage message = ParseStatusMessage(keyword, arguments);
          if(message != null) OnStatusMessage(message);
        }
      }
    }

    Process process;
    InheritablePipe statusPipe, passwordPipe;
    FileStream statusStream, passwordStream;
    byte[] statusBuffer, outBuffer, errorBuffer, password;
    ProcessStartInfo psi;
    int statusBytes, outBytes, errorBytes;
    StreamHandling outHandling, errorHandling;
    volatile bool statusDone, outDone, errorDone;
    bool closeStdInput, disposed;

    static int Decode(byte[] encoded)
    {
      int newLength = encoded.Length, index = -1;

      while(true)
      {
        index = Array.IndexOf(encoded, (byte)'%', index+1);
        if(index == -1) break;

        if(index < encoded.Length-2) // if there's enough space for two hex digits after the percent sign
        {
          byte high = encoded[index+1], low = encoded[index+2];
          encoded[index] = (byte)GetHexValue((char)high, (char)low); // convert the hex value to the new byte value
          index     += 2;
          newLength -= 2;
        }
      }

      return newLength;
    }

    static void DumpBinaryStream(IAsyncResult result, Stream stream, ref bool bufferDone)
    {
      int bytesRead = 0;
      if(stream != null)
      {
        try { bytesRead = stream.EndRead(result); }
        catch(ObjectDisposedException) { }
      }
      if(bytesRead == 0) bufferDone = true;
    }

    static IEnumerable<byte[]> ProcessAsciiStream(IAsyncResult result, Stream stream,
                                                  ref byte[] buffer, ref int bufferBytes, ref bool bufferDone)
    {
      List<byte[]> lines = new List<byte[]>();
      if(result != null)
      {
        int bytesRead = 0;
        try { bytesRead = stream.EndRead(result); }
        catch(ObjectDisposedException) { }

        if(bytesRead == 0) bufferDone = true;
        else
        {
          int index, searchStart = bufferBytes, newBufferStart = 0;
          bufferBytes += bytesRead;

          do
          {
            index = Array.IndexOf(buffer, (byte)'\n', searchStart, bufferBytes-searchStart);
            if(index == -1) break;

            int eolLength = 1;
            if(index != 0 && buffer[index-1] == (byte)'\r')
            {
              index--;
              eolLength++;
            }

            byte[] line = new byte[index-newBufferStart];
            Array.Copy(buffer, newBufferStart, line, 0, line.Length);
            lines.Add(line);

            newBufferStart = searchStart = index+eolLength;
          } while(bufferBytes != searchStart);

          if(newBufferStart != 0)
          {
            bufferBytes -= newBufferStart;
            if(bufferBytes != 0) Array.Copy(buffer, newBufferStart, buffer, 0, bufferBytes);
          }
        }
      }

      if(bufferBytes == buffer.Length)
      {
        byte[] newBuffer = new byte[buffer.Length*2];
        Array.Copy(buffer, newBuffer, bufferBytes);
        buffer = newBuffer;
      }

      return lines;
    }

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

    static StatusMessage ParseStatusMessage(string keyword, string[] arguments)
    {
Debugger.Log(0, "GPG", keyword+" "+string.Join(" ", arguments)+"\n"); // TODO: remove this

      StatusMessage message;
      switch(keyword)
      {
        case "NEWSIG": message = new NewSigMessage(arguments); break;
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

        case "USERID_HINT": message = new UserIdHintMessage(arguments); break;
        case "NEED_PASSPHRASE": message = new NeedKeyPassphraseMessage(arguments); break;
        case "GOOD_PASSPHRASE": message = new GenericMessage(StatusMessageType.GoodPassphrase); break;
        case "MISSING_PASSPHRASE": message = new GenericMessage(StatusMessageType.MissingPassphrase); break;
        case "BAD_PASSPHRASE": message = new BadPassphraseMessage(arguments); break;

        case "BEGIN_SIGNING": message = new GenericMessage(StatusMessageType.BeginSigning); break;
        case "SIG_CREATED": message = new GenericMessage(StatusMessageType.SigCreated); break;
        case "BEGIN_DECRYPTION": message = new GenericMessage(StatusMessageType.BeginDecryption); break;
        case "END_DECRYPTION": message = new GenericMessage(StatusMessageType.EndDecryption); break;
        case "BEGIN_ENCRYPTION": message = new GenericMessage(StatusMessageType.BeginEncryption); break;
        case "END_ENCRYPTION": message = new GenericMessage(StatusMessageType.EndEncryption); break;

        case "INV_RECP": message = new InvalidRecipientMessage(arguments); break;
        case "NO_PUBKEY": message = new MissingPublicKeyMessage(arguments); break;
        case "NO_SECKEY": message = new MissingSecretKeyMessage(arguments); break;
        case "UNEXPECTED": message = new GenericMessage(StatusMessageType.UnexpectedData); break;

        case "TRUST_UNDEFINED": message = new TrustLevelMessage(StatusMessageType.TrustUndefined); break;
        case "TRUST_NEVER": message = new TrustLevelMessage(StatusMessageType.TrustNever); break;
        case "TRUST_MARGINAL": message = new TrustLevelMessage(StatusMessageType.TrustMarginal); break;
        case "TRUST_FULLY": message = new TrustLevelMessage(StatusMessageType.TrustFully); break;
        case "TRUST_ULTIMATE": message = new TrustLevelMessage(StatusMessageType.TrustUltimate); break;

        case "IMPORT_RES": case "PLAINTEXT": case "PLAINTEXT_LENGTH": case "SIG_ID": // ignore these messages
          message = null;
          break;
        
        default: message = null; break; // TODO: remove later, or replace with logging?
      }
      return message;
    }

    static void SplitDecodedLine(byte[] line, int length, out string keyword, out string[] arguments)
    {
      List<string> chunks = new List<string>();

      for(int index=0; ; )
      {
        while(index < length && line[index] == (byte)' ') index++;
        int start = index;
        while(index < length && line[index] != (byte)' ') index++;

        if(start == length) break;

        chunks.Add(Encoding.UTF8.GetString(line, start, index-start));
      }

      if(chunks.Count < 2)
      {
        keyword   = null;
        arguments = null;
      }
      else
      {
        keyword   = chunks[1]; // skip the first chunk, which is just "[GNUPG:]"
        arguments = new string[chunks.Count-2];
        chunks.CopyTo(2, arguments, 0, arguments.Length);
      }
    }

    static readonly Regex statusLineRe = new Regex(@"^\[GNUPG:] (?<keyword>\w+)(?:\s*(?<arguments>.*))?",
                                                   RegexOptions.Singleline | RegexOptions.Compiled);
    static readonly Regex spaceSepRe = new Regex(@"\s+", RegexOptions.Singleline | RegexOptions.Compiled);
  }
  #endregion

  /// <summary>Throws an exception if <see cref="Initialize"/> has not yet been called.</summary>
  protected void AssertInitialized()
  {
    if(ExecutablePath == null) throw new InvalidOperationException("ExecutablePath is not set.");
  }

  /// <summary>Escapes a command-line argument.</summary>
  protected static string EscapeArg(string arg)
  {
    if(arg.IndexOf(' ') != -1) // if the argument contains spaces, we need to quote it.
    {
      if(arg.IndexOf('"') == -1) return "\"" + arg + "\""; // if it doesn't contain a double-quote, use those
      else if(arg.IndexOf('\'') == -1) return "'" + arg + "'"; // otherwise, try single quotes
    }
    else if(arg.IndexOf('"') != -1)
    {
      throw new NotImplementedException();
    }
    else return arg;

    throw new ArgumentException("Argument could not be escaped: "+arg);
  }

  /// <summary>Normalizes a keyring filename to something that is acceptable to GPG.</summary>
  protected static string NormalizeKeyringFile(string filename)
  {
    return Path.GetFullPath(filename).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
  }

  /// <summary>Writes all data from the stream to the standard input of the process,
  /// and then closes the standard input.
  /// </summary>
  /// <returns>Returns true if all data was written to the stream before the process terminated.</returns>
  protected static bool WriteStreamToProcess(Stream data, Process process)
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

  Command Execute(string args, bool getStatusStream, bool closeStdInput,
                  StreamHandling stdOutHandling, StreamHandling stdErrorHandling, Key passwordKey)
  {
    InheritablePipe statusPipe = null, passwordPipe = null;
    byte[] passwordBytes = null;

    if(passwordKey == null)
    {
      args = "--batch --no-tty " + args;
    }
    else
    {
      PrimaryKey primaryKey = passwordKey.GetPrimaryKey();
      string hint = null;

      if(primaryKey.PrimaryUserId != null) hint = primaryKey.PrimaryUserId.ToString();
      hint += (hint == null ? null : " ") + "[0x" + passwordKey.KeyId;
      if(!string.Equals(passwordKey.KeyId, primaryKey.KeyId, StringComparison.Ordinal))
      {
        hint += " on primary key 0x" + primaryKey.KeyId;
      }
      hint += "]";

      SecureString password = GetPassword(passwordKey.KeyId, hint);
      if(password == null) throw new OperationCanceledException();

      IntPtr bstr  = IntPtr.Zero;
      char[] chars = new char[password.Length+1];
      try
      {
        bstr = Marshal.SecureStringToBSTR(password);
        Marshal.Copy(bstr, chars, 0, chars.Length);
        chars[password.Length] = '\n'; // the password must be EOL-terminated for GPG to accept it
        passwordBytes = Encoding.UTF8.GetBytes(chars);
      }
      finally
      {
        password.Dispose();
        if(bstr != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr);
        Array.Clear(chars, 0, chars.Length);
      }

      passwordPipe = new InheritablePipe();
      args = "--passphrase-fd " + passwordPipe.ClientHandle.ToInt64().ToString(CultureInfo.InvariantCulture) + " " +
             args;
    }

    if(getStatusStream)
    {
      statusPipe = new InheritablePipe();
      args = "--exit-on-status-write-error --status-fd " +
             statusPipe.ClientHandle.ToInt64().ToString(CultureInfo.InvariantCulture) + " " + args;
    }

    return new Command(GetProcessStartInfo(ExecutablePath, args), statusPipe, passwordPipe, passwordBytes,
                       closeStdInput, stdOutHandling, stdErrorHandling);
  }

  PrimaryKey[] GetKeys(KeySignatures signatures, Keyring[] keyrings, bool includeDefaultKeyring, bool secretKeys)
  {
    // gpg seems to require --no-sig-cache in order to return fingerprints for signatures
    string args;
    if(secretKeys) args = "--list-secret-keys ";
    else if(signatures == KeySignatures.Retrieve) args = "--list-sigs --no-sig-cache ";
    else if(signatures == KeySignatures.Verify) args = "--check-sigs --no-sig-cache ";
    else args = "--list-keys ";
    args += "--with-fingerprint --with-fingerprint --with-colons --fixed-list-mode ";

    List<PrimaryKey> keys = new List<PrimaryKey>();

    if(includeDefaultKeyring) GetKeys(keys, args, null, secretKeys);

    if(keyrings != null)
    {
      foreach(Keyring keyring in keyrings)
      {
        string file = keyring == null ? null : secretKeys ? keyring.SecretFile : keyring.PublicFile;
        if(string.IsNullOrEmpty(file)) throw new ArgumentException("Empty keyring filename.");
        GetKeys(keys, args, keyring, secretKeys);
      }
    }

    return keys.ToArray();
  }

  void GetKeys(List<PrimaryKey> keys, string args, Keyring keyring, bool secretKeys)
  {
    if(keyring != null)
    {
      string file = NormalizeKeyringFile(secretKeys ? keyring.SecretFile : keyring.PublicFile);
      args += "--no-default-keyring --" + (secretKeys ? "secret-keyring " : "keyring ") + file;
    }

    Command cmd = Execute(args, false, true, StreamHandling.Unprocessed, StreamHandling.ProcessText, null);
    using(cmd)
    {
      cmd.Start();

      List<Subkey> subkeys = new List<Subkey>();
      List<UserId> userIds = new List<UserId>();
      List<KeySignature> sigs = new List<KeySignature>();

      PrimaryKey currentPrimary = null;
      Subkey currentSubkey = null;
      UserId currentUserId = null;

      while(true)
      {
        string line = cmd.Process.StandardOutput.ReadLine();
        if(line == null) break;

        string[] fields = line.Split(':');
        switch(fields[0])
        {
          case "sig": case "rev": // signature or revocation signature
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
            sig.Finish();
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
    }

    cmd.CheckExitCode();
  }

  Signature[] VerifyCore(string signatureFile, Stream signedData, VerificationOptions options)
  {
    string args = null;
    if(options != null)
    {
      args += GetKeyringArgs(options.IgnoreDefaultKeyring, options.AdditionalKeyrings, false);

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
    }

    args += "--verify " + (signatureFile == null ? "-" : EscapeArg(signatureFile) + " -");
    Command cmd = Execute(args, true, false, StreamHandling.DumpBinary, StreamHandling.ProcessText, null);
    using(cmd)
    {
      List<Signature> signatures = new List<Signature>();
      Signature sig = new Signature();
      bool sigFilled = false;

      cmd.StatusMessageReceived += delegate(StatusMessage msg)
      {
        if(msg is TrustLevelMessage)
        {
          sig.TrustLevel = ((TrustLevelMessage)msg).Level;
        }
        else
        {
          if(msg.Type == StatusMessageType.NewSig  || msg.Type == StatusMessageType.BadSig ||
             msg.Type == StatusMessageType.GoodSig || msg.Type == StatusMessageType.ErrorSig)
          {
            if(sigFilled) signatures.Add(sig);
            sig = new Signature();
            sigFilled = false;
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
          }
        }
      };

      cmd.Start();
      if(!WriteStreamToProcess(signedData, cmd.Process)) throw new Exception("GPG aborted early."); // TODO: exception class
      cmd.WaitForExit();
      if(sigFilled) signatures.Add(sig);

      cmd.CheckExitCode();
      return signatures.ToArray();
    }
  }

  static string CUnescape(string str)
  {
    return cEscapeRe.Replace(str, delegate(Match m)
    {
      return new string((char)GetHexValue(m.Value[2], m.Value[3]), 1);
    });
  }

  static Process Execute(string exePath, string args)
  {
    return Process.Start(GetProcessStartInfo(exePath, args));
  }

  static int Exit(Process process)
  {
    process.StandardInput.Close();
    process.StandardOutput.Close();
    process.StandardError.Close();
    if(!process.WaitForExit(500)) process.Kill();
    return process.ExitCode;
  }

  static void FinishPrimaryKey(List<PrimaryKey> keys, List<Subkey> subkeys, List<UserId> userIds,
                               List<KeySignature> sigs, ref PrimaryKey currentPrimary, ref Subkey currentSubkey,
                               ref UserId currentUserId)
  {
    FinishSignatures(sigs, currentPrimary, currentSubkey, currentUserId);
    FinishSubkey(subkeys, sigs, currentPrimary, ref currentSubkey, currentUserId);
    FinishUserId(userIds, sigs, currentPrimary, currentSubkey, ref currentUserId);

    if(currentPrimary != null)
    {
      currentPrimary.Subkeys    = new ReadOnlyListWrapper<Subkey>(subkeys.ToArray());
      currentPrimary.UserIds    = new ReadOnlyListWrapper<UserId>(userIds.ToArray());

      if(currentPrimary.Signatures == null)
      {
        currentPrimary.Signatures = sigs.Count == 0 ? NoSignatures
                                                    : new ReadOnlyListWrapper<KeySignature>(sigs.ToArray());
      }

      currentPrimary.Finish();
      keys.Add(currentPrimary);
      currentPrimary = null;
    }

    subkeys.Clear();
    userIds.Clear();
  }

  static void FinishSignatures(List<KeySignature> sigs, PrimaryKey currentPrimary, Subkey currentSubkey,
                               UserId currentUserId)
  {
    ReadOnlyListWrapper<KeySignature> list = new ReadOnlyListWrapper<KeySignature>(sigs.ToArray());

    if(currentUserId != null) currentUserId.Signatures = list;
    else if(currentSubkey != null) currentSubkey.Signatures = list;
    else if(currentPrimary != null) currentPrimary.Signatures = list;

    sigs.Clear();
  }

  static void FinishSubkey(List<Subkey> subkeys, List<KeySignature> sigs,
                           PrimaryKey currentPrimary, ref Subkey currentSubkey, UserId currentUserId)
  {
    FinishSignatures(sigs, currentPrimary, currentSubkey, currentUserId);
    if(currentSubkey != null && currentPrimary != null)
    {
      currentSubkey.PrimaryKey = currentPrimary;
      
      if(currentSubkey.Signatures == null)
      {
        currentSubkey.Signatures = sigs.Count == 0 ? NoSignatures
                                                   : new ReadOnlyListWrapper<KeySignature>(sigs.ToArray());
      }

      currentSubkey.Finish();
      subkeys.Add(currentSubkey);
      currentSubkey = null;
    }
  }

  static void FinishUserId(List<UserId> userIds, List<KeySignature> sigs,
                           PrimaryKey currentPrimary, Subkey currentSubkey, ref UserId currentUserId)
  {
    FinishSignatures(sigs, currentPrimary, currentSubkey, currentUserId);
    if(currentUserId != null && currentPrimary != null)
    {
      currentUserId.Key     = currentPrimary;
      currentUserId.Primary = userIds.Count == 0; // the primary user ID is the first one listed

      if(currentUserId.Signatures == null)
      {
        currentUserId.Signatures = sigs.Count == 0 ? NoSignatures
                                                   : new ReadOnlyListWrapper<KeySignature>(sigs.ToArray());
      }

      currentUserId.Finish();
      userIds.Add(currentUserId);
      currentUserId = null;
    }
  }

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

  static int GetHexValue(char high, char low)
  {
    return (GetHexValue(high)<<4) + GetHexValue(low);
  }

  static string GetKeyringArgs(IEnumerable<Key> keys, bool publicKeyrings, bool secretKeyrings)
  {
    string args = null;

    if(keys != null)
    {
      Dictionary<string, object> publicFiles = new Dictionary<string, object>(StringComparer.Ordinal);
      Dictionary<string, object> secretFiles = new Dictionary<string, object>(StringComparer.Ordinal);

      string optionName = secretKeyrings ? "--secret-keyring " : "--keyring ";
      foreach(Key key in keys)
      {
        if(key.Keyring != null)
        {
          string publicFile = NormalizeKeyringFile(key.Keyring.PublicFile);
          string secretFile = NormalizeKeyringFile(key.Keyring.SecretFile);

          if(publicKeyrings && string.IsNullOrEmpty(publicFile) ||
             secretKeyrings && string.IsNullOrEmpty(secretFile))
          {
            throw new ArgumentException("Empty keyring filename.");
          }

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
    }

    return args;
  }

  static string GetKeyringArgs(bool ignoreDefaultKeyring, IEnumerable<Keyring> keyrings, bool secretKeyrings)
  {
    string args = null;

    if(ignoreDefaultKeyring) args += "--no-default-keyring ";

    if(keyrings != null)
    {
      string optionName = secretKeyrings ? "--secret-keyring " : "--keyring ";
      foreach(Keyring keyring in keyrings)
      {
        string file = keyring == null ? null : secretKeyrings ? keyring.SecretFile : keyring.PublicFile;
        if(string.IsNullOrEmpty(file)) throw new ArgumentException("Empty keyring filename.");
        args += optionName + EscapeArg(NormalizeKeyringFile(file)) + " ";
      }
    }

    return args;
  }

  static string GetOutputArgs(OutputOptions options)
  {
    string args = null;
    if(options != null)
    {
      if(options.Format == OutputFormat.ASCII) args += "-a ";
      foreach(string comment in options.Comments)
      {
        if(!string.IsNullOrEmpty(comment)) args += "--comment "+EscapeArg(comment)+" ";
      }
    }
    return args;
  }

  static ProcessStartInfo GetProcessStartInfo(string exePath, string args)
  {
    string prefixArgs = "--no-options --display-charset utf-8 ";

    ProcessStartInfo psi = new ProcessStartInfo();
    psi.Arguments              = prefixArgs + args;
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

  static void ReadKeyData(Key key, string[] data)
  {
    if(!string.IsNullOrEmpty(data[1]))
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