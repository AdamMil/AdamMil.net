using System;
using System.IO;
using System.Net.Mail;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace AdamMil.Security.Logging
{

#region LogEntryType
/// <summary>The type of a log entry.</summary>
public enum LogEntryType
{
  /// <summary>An error event indicates a significant problem, such as a loss of functionality or data.</summary>
  Error,
  /// <summary>A warning event indicates a problem that is not immediately significant but that may cause future
  /// complications.
  /// </summary>
  Warning,
  /// <summary>An information event indicates an infrequent but significant successful operation.</summary>
  Info,
  /// <summary>A debug information event describes useful information about the inner workings of the application, to
  /// aid in diagnosing an issue.
  /// </summary>
  DebugInfo,
  /// <summary>A success audit event is a security event that occurs when an audited access attempt is successful.</summary>
  AuditSuccess,
  /// <summary>A failure audit event is a security event that occurs when an audited access attempt fails. For example,
  /// a failed attempt to open a file is a failure audit event.
  /// </summary>
  AuditFailure,
  /// <summary>A suspicious activity event is a security event that occurs when unusual activity which possibly
  /// indicate an attempted attack is detected.
  /// </summary>
  SuspiciousActivity
}
#endregion

#region LogEntry
/// <summary>Represents an entry in a log.</summary>
public class LogEntry
{
  /// <summary>Initializes a new, valid <see cref="LogEntry"/>, stamped with the current time.</summary>
  public LogEntry(LogEntryType type, string message) : this(type, DateTime.UtcNow, message, true) { }

  /// <summary>Initializes a new, valid <see cref="LogEntry"/>.</summary>
  public LogEntry(LogEntryType type, DateTime timestamp, string message) : this(type, timestamp, message, true) { }

  /// <summary>Initializes a new <see cref="LogEntry"/>.</summary>
  public LogEntry(LogEntryType type, DateTime timestamp, string message, bool isValid)
  {
    if(message == null) throw new ArgumentNullException("message");
    this.message   = message;
    this.timestamp = timestamp.ToUniversalTime();
    this.type      = type;
    this.isValid   = isValid;
  }

  /// <summary>Gets whether the log entry is valid (ie, has not been corrupted or tampered with).</summary>
  public bool IsValid
  {
    get { return isValid; }
  }

  /// <summary>Gets the message in the log entry.</summary>
  public string Message
  {
    get { return message; }
  }

  /// <summary>Gets the timestamp of when the log entry was created, in UTC.</summary>
  public DateTime Timestamp
  {
    get { return timestamp; }
  }

  /// <summary>Gets the type of log entry, as a <see cref="LogEntryType"/>.</summary>
  public LogEntryType Type
  {
    get { return type; }
  }

  readonly string message;
  readonly DateTime timestamp;
  readonly LogEntryType type;
  readonly bool isValid;
}
#endregion

#region EntityActivityEntry
/// <summary>Represents an entry in a log that refers to what an entity did and when.</summary>
public abstract class EntityActivityEntry : LogEntry
{
  /// <summary>Initializes a new, valid <see cref="EntityActivityEntry"/>, stamped with the current time. The 'who' and
  /// 'what' parameters are required.
  /// </summary>
  protected EntityActivityEntry(LogEntryType type, string who, string from, string what)
    : this(type, DateTime.UtcNow, who, from, what, true) { }

  /// <summary>Initializes a new, valid <see cref="EntityActivityEntry"/>. The 'who' and 'what' parameters are
  /// required.
  /// </summary>
  protected EntityActivityEntry(LogEntryType type, DateTime timestamp, string who, string from, string what)
    : this(type, timestamp, who, from, what, true) { }

  /// <summary>Initializes a new, valid <see cref="EntityActivityEntry"/>. The 'who' and 'what' parameters are
  /// required.
  /// </summary>
  protected EntityActivityEntry(LogEntryType type, DateTime timestamp, string who, string from, string what,
                                bool isValid)
    : base(type, timestamp, "User " + who + " (from " + from + ") " + what, isValid)
  {
    if(string.IsNullOrEmpty(who) || string.IsNullOrEmpty(what))
    {
      throw new ArgumentException("The who and what arguments must not be empty.");
    }

    this.who  = who;
    this.from = from;
    this.what = what;
  }

  /// <summary>Gets the portion of the message that refers to who performed the action. This may be null if the message
  /// contained no such portion.
  /// </summary>
  public string Who
  {
    get { return Who; }
  }

  /// <summary>Gets the portion of the message that refers to where the actor came from. This may be null if the
  /// message contained no such portion.
  /// </summary>
  public string From
  {
    get { return from; }
  }

  /// <summary>Gets the portion of the message that refers to what the actor did, or attempted to do. This may be null
  /// if the message contained no such portion.
  /// </summary>
  public string What
  {
    get { return what; }
  }

  readonly string who, from, what;
}
#endregion

#region AuditEntry
/// <summary>Represents an audited event, either successful or unsuccessful.</summary>
public class AuditEntry : EntityActivityEntry
{
  /// <summary>Initializes a new, valid <see cref="AuditEntry"/> event, stamped with the current time. The 'who' and
  /// 'what' parameters are required.
  /// </summary>
  public AuditEntry(string who, string from, string what, bool succeeded)
    : this(DateTime.UtcNow, who, from, what, succeeded) { }

  /// <summary>Initializes a new, valid <see cref="AuditEntry"/> event. The 'who' and 'what' parameters are required.</summary>
  public AuditEntry(DateTime timestamp, string who, string from, string what, bool succeeded)
    : base(succeeded ? LogEntryType.AuditSuccess : LogEntryType.AuditFailure, timestamp, who, from, what, true) { }

  /// <summary>Gets whether the actor named in <see cref="EntityActivityEntry.Who"/> succeeded in the attempt to
  /// perform the action described in <see cref="EntityActivityEntry.What"/>.
  /// </summary>
  public bool Success
  {
    get { return Type == LogEntryType.AuditSuccess; }
  }
}
#endregion

#region SuspiciousActivityEntry
/// <summary>Represents suspicious activity that was detected.</summary>
public class SuspiciousActivityEntry : EntityActivityEntry
{
  /// <summary>Initializes a new <see cref="SuspiciousActivityEntry"/>. The 'who' and 'what' parameters are required.</summary>
  public SuspiciousActivityEntry(DateTime timestamp, string who, string from, string what)
    : base(LogEntryType.SuspiciousActivity, timestamp, who, from, what) { }
}
#endregion

#region SecureLog
/// <summary>Represents a base class for secure log files. Secure log files protect against adding, altering, or
/// deleting log entries or log files, and can send email notifications about issues with the log file. For more
/// details, see the individual subclasses of <see cref="SecureLog"/>, such as <see cref="SecureWindowsEventLog"/> and
/// <see cref="SecureFileLog"/>.
/// </summary>
public abstract class SecureLog
{
  /// <summary>Gets or sets the email address to which mails about the log will be sent. If set to null, mails will not
  /// be sent. The default is null.
  /// </summary>
  public MailAddress AdminEmail
  {
    get { return adminEmail; }
    set { adminEmail = value; }
  }

  /// <summary>Gets or sets the email address from which mails about the log appear to be sent. If set to null, mails
  /// will appear to be sent from the <see cref="AdminEmail"/> address. The default is null.
  /// </summary>
  public MailAddress FromEmail
  {
    get { return fromEmail; }
    set { fromEmail = value; }
  }

  /// <summary>Gets or sets the key used to perform HMAC verification of log messages. The HMAC verification prevents
  /// both corruption and deliberate tampering, unless someone can discover the key. The best key is a string that
  /// UTF-8 encodes to exactly 64-bytes. Longer keys incur a performance penalty, and shorter keys are less secure. If
  /// set to null or an empty string, the HMAC verification will be very insecure, able to subverted by anyone with
  /// access to the source code or this compiled assembly.
  /// </summary>
  public SecureString HashKey
  {
    get { return hashKey; }
    set { hashKey = value; }
  }

  /// <summary>Gets or sets the maximum log entry length, in characters. Log entries greater than this length will be
  /// truncated. If set to zero, log entries will never be truncated. The default is 8192 characters.
  /// </summary>
  public int MaximumEntryLength
  {
    get { return maximumEntryLength; }
    set
    {
      if(value < 0) throw new ArgumentOutOfRangeException("MaximumEntryLength", "Must be non-negative.");
      maximumEntryLength = value;
    }
  }

  /// <summary>Gets (or sets, in a subclass) the name of the log. The name is typically fixed when the log is opened.
  /// The default is null, which indicates that the log has no name.
  /// </summary>
  public string Name
  {
    get { return name; }
    protected set { name = value; }
  }

  /// <summary>Provides a simple interface to add a log entry comprised of a type and a message.</summary>
  public void Log(LogEntryType type, string message)
  {
    Log(new LogEntry(type, DateTime.UtcNow, message));
  }

  /// <summary>Adds the given log entry to the log.</summary>
  public void Log(LogEntry entry)
  {
    if(entry == null) throw new ArgumentNullException();

    string message = MaximumEntryLength != 0 && entry.Message.Length > MaximumEntryLength ?
      entry.Message.Substring(0, MaximumEntryLength) : entry.Message;
    byte[] dateBytes = new byte[8], hashBytes = null, msgBytes = Encoding.UTF8.GetBytes(message);
    IO.IOH.WriteLE8(dateBytes, 0, entry.Timestamp.Ticks);

    SecureString hashKey = HashKey; // grab the hash key in case it's changed by another thread
    HMACSHA1 hmac = null;
    if(hashKey == null || hashKey.Length == 0) // if no key was specified, just use the padding bytes as the key
    {
      hmac = new HMACSHA1(keyPadding);
    }
    else
    {
      SecurityUtility.ProcessSecureString(hashKey, delegate(byte[] keyBytes)
      {
        if(keyBytes.Length < 64) // if the key is shorter than 64 bytes, pad it until it becomes 64 bytes
        {
          byte[] key = new byte[64];
          Array.Copy(keyBytes, key, keyBytes.Length);
          Array.Copy(keyPadding, keyBytes.Length, key, keyBytes.Length, 64-keyBytes.Length);
          keyBytes = key;
        }
        hmac = new HMACSHA1(keyBytes);
        SecurityUtility.ZeroBuffer(keyBytes);
      });
    }

    hmac.TransformBlock(dateBytes, 0, dateBytes.Length, null, 0);
    hashBytes = hmac.TransformFinalBlock(msgBytes, 0, msgBytes.Length);
    hmac.Clear(); // clear the internal buffers of the HMAC class, which contain the key

    try { WriteLogEntry(entry.Type, entry.Timestamp, hashBytes, msgBytes); }
    catch(Exception ex)
    {
      if(AdminEmail != null)
      {
        SendEmailToAdmin("Unable to write to log", "Time: " + DateTime.Now.ToString("G") + "\nMessage: " + message +
                         "\nError:\n" + ex.ToString());
      }
      throw;
    }
  }

  /// <summary>Sends an email to the administrator about a problem with the security log.</summary>
  protected void SendEmailToAdmin(string subject, string body)
  {
    // TODO: add throttling / queuing
    // TODO: add TLS support
    if(string.IsNullOrEmpty(subject)) subject = "Security Log Problem";
    if(string.IsNullOrEmpty(body)) throw new ArgumentException("The body must not be empty.");

    // copy the email addresses in case they're changed by another thread
    MailAddress adminEmail = AdminEmail, fromEmail = FromEmail;
    if(adminEmail == null) throw new InvalidOperationException("No admin email address has been set.");

    // prepend the log name to the subject
    if(!string.IsNullOrEmpty(Name)) subject = "[" + Name + "] " + subject;

    MailMessage message = new MailMessage(fromEmail != null ? fromEmail : adminEmail, adminEmail);
    message.Subject = subject;
    message.Body    = body;
    message.SubjectEncoding = Encoding.UTF8;
    message.BodyEncoding    = Encoding.UTF8;
    new SmtpClient().Send(message);
  }

  protected abstract void WriteLogEntry(LogEntryType type, DateTime timestamp, byte[] hashBytes,
                                        byte[] utf8messageBytes);

  SecureString hashKey;
  MailAddress adminEmail, fromEmail;
  string name;
  int maximumEntryLength = 8192;

  /// <summary>Used to pad the key out to 64 bytes.</summary>
  static readonly byte[] keyPadding = new byte[]
  {
    0x5c, 0x1a, 0x55, 0xf1, 0x58, 0x2f, 0x82, 0x52, 0x88, 0x84, 0xd6, 0xe2, 0x1a, 0x1a, 0xf0, 0x95,
    0xcb, 0x94, 0xc8, 0xd3, 0x87, 0xe0, 0x47, 0x0b, 0xc1, 0x40, 0xe6, 0x0f, 0xbd, 0x9c, 0x34, 0xc2,
    0xd2, 0x88, 0x81, 0xec, 0x2f, 0x06, 0x67, 0x0a, 0x89, 0x6d, 0x51, 0xbf, 0x58, 0x06, 0x44, 0x46,
    0x1b, 0x5d, 0x01, 0x1f, 0x66, 0xfa, 0x63, 0xc1, 0xc2, 0xf9, 0x29, 0x6e, 0x36, 0x3e, 0xcc, 0x1a
  };
}
#endregion

#region SecureFileLog
/// <summary>Represents a log to files on disk. The log supports </summary>
/*public class SecureFileLog : SecureLog
{
}*/
#endregion

} // namespace AdamMil.Security.Logging