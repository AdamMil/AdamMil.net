using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace AdamMil.Security
{

/// <summary>A class containing various helpful functions dealing with security.</summary>
public static class SecurityUtility
{
  /// <summary>Performs the given action on the characters within a <see cref="SecureString"/>.</summary>
  public static void ProcessSecureString(SecureString secureString, Action<char[]> processor)
  {
    if(secureString == null || processor == null) throw new ArgumentNullException();

    // treat the string as securely as we can by ensuring that it doesn't stick around in memory longer than necessary
    IntPtr bstr  = IntPtr.Zero;
    char[] chars = new char[secureString.Length];
    try
    {
      bstr = Marshal.SecureStringToBSTR(secureString);
      Marshal.Copy(bstr, chars, 0, chars.Length);
      processor(chars);
    }
    finally
    {
      if(bstr != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr);
      ZeroBuffer(chars);
    }
  }

  /// <summary>Performs the given action on the UTF-8 encoded bytes of the <see cref="SecureString"/>.</summary>
  public static void ProcessSecureString(SecureString secureString, Action<byte[]> processor)
  {
    ProcessSecureString(secureString, Encoding.UTF8, processor);
  }

  /// <summary>Performs the given action on the encoded bytes of the <see cref="SecureString"/>.</summary>
  public static void ProcessSecureString(SecureString secureString, Encoding encoding, Action<byte[]> processor)
  {
    if(encoding == null || processor == null) throw new ArgumentNullException();

    ProcessSecureString(secureString, delegate(char[] chars)
    {
      byte[] bytes = null;
      try
      {
        bytes = encoding.GetBytes(chars);
        processor(bytes);
      }
      finally { ZeroBuffer(bytes); }
    });
  }

  /// <summary>Clears the given buffer, if it is not null.</summary>
  public static void ZeroBuffer<T>(T[] buffer)
  {
    if(buffer != null) Array.Clear(buffer, 0, buffer.Length);
  }
}

} // namespace AdamMil.Security