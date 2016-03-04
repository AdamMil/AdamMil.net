/*
AdamMil.Utilities is a library providing generally useful utilities for
.NET development.

http://www.adammil.net/
Copyright (C) 2010-2016 Adam Milazzo

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
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Threading;

namespace AdamMil.Utilities
{

#region LoginType
/// <summary>Determines how an impersonated user is logged onto the system.</summary>
public enum LoginType
{
  /// <summary>The user will be logged on as a batch process. This is intended for use when processes may execute on behalf of the
  /// user without their intervention. This creates a primary logon token that allows network access and the creation of new
  /// processes, but requires permission to log onto the machine as a batch process.
  /// </summary>
  Batch,
  /// <summary>The user will be logged on interactively. This is the slowest logon type, and requires permission to log onto the
  /// machine interactively. It grants access to "interactive" resources, such as cmd.exe, which might otherwise be restricted.
  /// This is only intended to be used when the user will be using the machine interactively.
  /// </summary>
  Interactive,
  /// <summary>The user will be logged on as over a network. This is the fastest logon type, but does not create a primary logon
  /// token for the user, does not grant the ability to run new processes as the user, and may not allow the user to access
  /// network resources on other machines. However, it can succeed even if the user has not been granted the right to log onto
  /// the machine.
  /// </summary>
  Network,
  /// <summary>The user will be logged on as a system service. This provides a high degree of access and is faster than
  /// <see cref="Interactive"/>, but requires permission to log on as a service.
  /// </summary>
  Service,
  /// <summary>The same as <see cref="Network"/>.</summary>
  Default=Network,
}
#endregion

#region Impersonation
/// <summary>Provides various methods of running code as a specific user. The typical usage of this class is to either call a
/// static <see cref="RunWithImpersonation(string,string,LoginType,bool,Action)"/> method, or to instantiate the class with the user's
/// credentials (which will begin impersonating the user), and end the impersonation by disposing the class.
/// </summary>
[SuppressUnmanagedCodeSecurity]
public sealed class Impersonation : IDisposable
{
  /// <summary>A logon token that can be used to run code as the user who started the process.</summary>
  public static readonly IntPtr RevertToSelf = IntPtr.Zero;

  /// <summary>Impersonates the user referenced by the given logon token, as returned from operating system API calls.</summary>
  public Impersonation(IntPtr userToken) : this(userToken, false) { }

  /// <summary>Impersonates the user referenced by the given <see cref="WindowsIdentity"/>.</summary>
  public Impersonation(WindowsIdentity user)
  {
    if(user == null) throw new ArgumentNullException();
    context = user.Impersonate();
  }

  /// <summary>Impersonates the user with the given user name and password. The user name can be in either DOMAIN\user or
  /// user@domain format. The impersonation ends when the class is disposed.
  /// </summary>
  public Impersonation(string userName, string password, LoginType loginType)
    : this(GetDomain(userName), GetUserName(userName), password, loginType) { }

  /// <summary>Impersonates the user with the given domain, user name, and password.
  /// The impersonation ends when the class is disposed.
  /// </summary>
  public Impersonation(string domain, string userName, string password, LoginType loginType)
    : this(LogOnUser(domain, userName, password, loginType), true) { }

  /// <summary>Impersonates the user with the given user name and password. The user name can be in either DOMAIN\user or
  /// user@domain format. The impersonation ends when the class is disposed.
  /// </summary>
  public Impersonation(string userName, SecureString password, LoginType loginType)
    : this(GetDomain(userName), GetUserName(userName), password, loginType) { }

  /// <summary>Impersonates the user with the given domain, user name, and password.
  /// The impersonation ends when the class is disposed.
  /// </summary>
  public Impersonation(string domain, string userName, SecureString password, LoginType loginType)
    : this(LogOnUser(domain, userName, password, loginType), true) { }

  Impersonation(IntPtr logonToken, bool ownToken)
  {
    if(ownToken) ownedToken = logonToken;
    context = WindowsIdentity.Impersonate(logonToken);
  }

  /// <summary>Releases all unmanaged resources used by this object.</summary>
  ~Impersonation()
  {
    Dispose(false);
  }

  /// <summary>Releases all unmanaged resources used by this object.</summary>
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  /// <summary>Logs off the user represented by the given logon token.</summary>
  public static void LogOffUser(IntPtr logonToken)
  {
    UnsafeNativeMethods.CloseHandle(logonToken);
  }

  /// <summary>Logs on the given user, returning the corresponding logon token.</summary>
  public static IntPtr LogOnUser(string userName, string password, LoginType loginType)
  {
    return LogOnUser(GetDomain(userName), GetUserName(userName), password, loginType);
  }

  /// <summary>Logs on the given user, returning the corresponding logon token.</summary>
  public static IntPtr LogOnUser(string domain, string userName, string password, LoginType loginType)
  {
    ValidateLogonArguments(domain, userName, password);
    IntPtr handle;
    int logonType = GetNTLogonType(loginType);
    if(!UnsafeNativeMethods.LogonUser(userName, domain, password, logonType, LOGON32_PROVIDER_DEFAULT, out handle))
    {
      throw new Win32Exception();
    }
    return handle;
  }

  /// <summary>Logs on the given user, returning the corresponding logon token.</summary>
  public static IntPtr LogOnUser(string userName, SecureString password, LoginType loginType)
  {
    return LogOnUser(GetDomain(userName), GetUserName(userName), password, loginType);
  }

  /// <summary>Logs on the given user, returning the corresponding logon token.</summary>
  public static IntPtr LogOnUser(string domain, string userName, SecureString password, LoginType loginType)
  {
    ValidateLogonArguments(domain, userName, password);
    IntPtr handle, passwordStr = IntPtr.Zero;
    int logonType = GetNTLogonType(loginType);
    try
    {
      passwordStr = Marshal.SecureStringToGlobalAllocUnicode(password);
      if(!UnsafeNativeMethods.LogonUser(userName, domain, passwordStr, logonType, LOGON32_PROVIDER_DEFAULT, out handle))
      {
        throw new Win32Exception();
      }
    }
    finally
    {
      if(passwordStr != IntPtr.Zero) Marshal.ZeroFreeGlobalAllocUnicode(passwordStr);
    }
    return handle;
  }

  /// <summary>Runs the given delegate as the user referenced by the given logon token, optionally executing it in a new thread.</summary>
  public static void RunWithImpersonation(IntPtr userToken, bool runInANewThread, Action code)
  {
    if(code == null) throw new ArgumentException();
    Run(userToken, false, code, runInANewThread);
  }

  /// <summary>Runs the given delegate as the user referenced by the given <see cref="WindowsIdentity"/>, optionally executing it
  /// in a new thread.
  /// </summary>
  public static void RunWithImpersonation(WindowsIdentity user, bool runInANewThread, Action code)
  {
    if(code == null) throw new ArgumentException();
    Run(user, code, runInANewThread);
  }

  /// <summary>Runs the given delegate as the given user, optionally executing it in a new thread. The user name can be in either
  /// DOMAIN\user or user@domain format.
  /// </summary>
  public static void RunWithImpersonation(string userName, string password, LoginType loginType, bool runInANewThread,
                                          Action code)
  {
    if(code == null) throw new ArgumentException();
    Run(LogOnUser(GetDomain(userName), GetUserName(userName), password, loginType), true, code, runInANewThread);
  }

  /// <summary>Runs the given delegate as the given user, optionally executing it in a new thread. The user name can be in either
  /// DOMAIN\user or user@domain format.
  /// </summary>
  public static void RunWithImpersonation(string userName, SecureString password, LoginType loginType, bool runInANewThread,
                                          Action code)
  {
    if(code == null) throw new ArgumentException();
    Run(LogOnUser(GetDomain(userName), GetUserName(userName), password, loginType), true, code, runInANewThread);
  }

  /// <summary>Runs the given delegate as the given user, optionally executing it in a new thread.</summary>
  public static void RunWithImpersonation(string domain, string userName, string password, LoginType loginType,
                                          bool runInANewThread, Action code)
  {
    if(code == null) throw new ArgumentException();
    Run(LogOnUser(domain, userName, password, loginType), true, code, runInANewThread);
  }

  /// <summary>Runs the given delegate as the given user, optionally executing it in a new thread.</summary>
  public static void RunWithImpersonation(string domain, string userName, SecureString password, LoginType loginType,
                                          bool runInANewThread, Action code)
  {
    if(code == null) throw new ArgumentException();
    Run(LogOnUser(domain, userName, password, loginType), true, code, runInANewThread);
  }

  void Dispose(bool manualDispose)
  {
    if(context != null)
    {
      context.Undo();
      context = null;
    }

    if(ownedToken != IntPtr.Zero)
    {
      UnsafeNativeMethods.CloseHandle(ownedToken);
      ownedToken = IntPtr.Zero;
    }
  }

  WindowsImpersonationContext context;
  IntPtr ownedToken;

  /// <summary>Retrieves the domain name from a user name in either user@domain or DOMAIN\user format, suitable for passing to
  /// the <see cref="LogOnUser(string,string,string,LoginType)"/> method.
  /// </summary>
  static string GetDomain(string userName)
  {
    if(userName == null) throw new ArgumentNullException();

    // if the name appears to be in UPN (name@domain) format, we must pass NULL for the domain
    if(IsUPN(userName)) return null;

    int slash = userName.IndexOf('\\');
    if(slash != -1) return userName.Substring(0, slash); // if the name appears to be in DOMAIN\user format, grab the domain

    throw new ArgumentException("Unable to determine the domain from the user name " + userName);
  }

  /// <summary>Converts a <see cref="LoginType"/> value into a value suitable for passing to <see cref="LogOnUser(string,string,LoginType)"/>.</summary>
  static int GetNTLogonType(LoginType type)
  {
    switch(type)
    {
      case LoginType.Batch: return LOGON32_LOGON_BATCH;
      case LoginType.Interactive: return LOGON32_LOGON_INTERACTIVE;
      case LoginType.Network: return LOGON32_LOGON_NETWORK;
      case LoginType.Service: return LOGON32_LOGON_SERVICE;
      default: throw new ArgumentException("Invalid login type: " + type.ToString());
    }
  }

  /// <summary>Retrieves the user name from a user name in either user@domain or DOMAIN\user format, suitable for passing to
  /// the <see cref="LogOnUser(string,string,LoginType)"/> method.
  /// </summary>
  static string GetUserName(string userName)
  {
    if(userName == null) throw new ArgumentNullException();

    int slash = userName.IndexOf('\\');
    if(slash != -1) return userName.Substring(slash+1); // if the name appears to be in DOMAIN\user format, grab the user name

    return userName;
  }

  /// <summary>Determines whether the given user name is in UPN (user@domain) format.</summary>
  static bool IsUPN(string userName)
  {
    return userName.IndexOf('@') != -1;
  }

  static void Run(WindowsIdentity identity, Action code, bool runInANewThread)
  {
    Run(delegate { return new Impersonation(identity); }, code, runInANewThread);
  }

  static void Run(IntPtr token, bool ownToken, Action code, bool runInANewThread)
  {
    Run(delegate { return new Impersonation(token, ownToken); }, code, runInANewThread);
  }

  static void Run(Func<Impersonation> impersonationMaker, Action code, bool runInANewThread)
  {
    if(!runInANewThread)
    {
      // this try/catch block is required for security, to prevent an exception filter from executing with elevated privileges
      try { using(impersonationMaker()) code(); }
      catch { throw; }
    }
    else
    {
      Exception exception = null;

      Thread thread = new Thread((ThreadStart)delegate
      {
        try { using(impersonationMaker()) code(); } // this try/catch block exists to propagate the exception to the calling thread
        catch(Exception ex) { exception = ex; }
      });
      thread.Start();
      thread.Join();

      if(exception != null) throw exception;
    }
  }

  static void ValidateLogonArguments(string domain, string userName, object password)
  {
    if(userName == null || password == null) throw new ArgumentNullException();

    if(string.IsNullOrEmpty(domain)) domain = null;
    if(string.IsNullOrEmpty(userName)) throw new ArgumentException("The user name was empty.");

    if(domain == null && !IsUPN(userName))
    {
      throw new ArgumentException("If no domain is specified, the user name must be given in UPN format.");
    }
    else if(domain != null && IsUPN(userName))
    {
      throw new ArgumentException("If a domain is specified, the user name must not be given in UPN format.");
    }
  }

  const int LOGON32_LOGON_INTERACTIVE = 2, LOGON32_LOGON_NETWORK = 3, LOGON32_LOGON_BATCH = 4, LOGON32_LOGON_SERVICE = 5;
  const int LOGON32_PROVIDER_DEFAULT = 0;
}
#endregion

} // namespace AdamMil.Utilities
