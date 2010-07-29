/*
AdamMil.Security is a .NET library providing OpenPGP-based security.
http://www.adammil.net/
Copyright (C) 2008-2010 Adam Milazzo

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

namespace AdamMil.Security
{

/// <summary>Provides various methods of running code as a specific user. The typical usage of this class is to either call a
/// static <see cref="RunWithImpersonation"/> method, or to instantiate the class with the user's credentials (which will begin
/// impersonating the user), and end the impersonation by disposing the class.
/// </summary>
public sealed class Impersonation : IDisposable
{
	/// <summary>A user token that can be used to run code as the user who started the process.</summary>
	public static readonly IntPtr RevertToSelf = IntPtr.Zero;

	/// <summary>Impersonates the user referenced by the given user token, as returned from operating system API calls.</summary>
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
	/// <param name="allowNetworkAccess">If true, the user will be logged on with network access.
	/// If false, the user will be logged on as a batch process.
	/// </param>
	public Impersonation(string userName, string password, bool allowNetworkAccess)
		: this(GetDomain(userName), GetUserName(userName), password, allowNetworkAccess) { }

	/// <summary>Impersonates the user with the given domain, user name, and password.
	/// The impersonation ends when the class is disposed.
	/// </summary>
	/// <param name="allowNetworkAccess">If true, the user will be logged on with network access.
	/// If false, the user will be logged on as a batch process.
	/// </param>
	public Impersonation(string domain, string userName, string password, bool allowNetworkAccess)
		: this(LogonUser(domain, userName, password, allowNetworkAccess), true) { }

	/// <summary>Impersonates the user with the given user name and password. The user name can be in either DOMAIN\user or
	/// user@domain format. The impersonation ends when the class is disposed.
	/// </summary>
	/// <param name="allowNetworkAccess">If true, the user will be logged on with network access.
	/// If false, the user will be logged on as a batch process.
	/// </param>
	public Impersonation(string userName, SecureString password, bool allowNetworkAccess)
		: this(GetDomain(userName), GetUserName(userName), password, allowNetworkAccess) { }

	/// <summary>Impersonates the user with the given domain, user name, and password.
	/// The impersonation ends when the class is disposed.
	/// </summary>
	/// <param name="allowNetworkAccess">If true, the user will be logged on with network access.
	/// If false, the user will be logged on as a batch process.
	/// </param>
	public Impersonation(string domain, string userName, SecureString password, bool allowNetworkAccess)
		: this(LogonUser(domain, userName, password, allowNetworkAccess), true) { }

	Impersonation(IntPtr userToken, bool ownToken)
	{
		if(ownToken) ownedToken = userToken;
		context = WindowsIdentity.Impersonate(userToken);
	}

	~Impersonation()
	{
		Dispose(true);
	}

	public void Dispose()
	{
		Dispose(false);
		GC.SuppressFinalize(this);
	}

	/// <summary>Runs the given delegate as the user referenced by the given user token, optionally executing it in a new thread.</summary>
	public static void RunWithImpersonation(IntPtr userToken, bool runInANewThread, ThreadStart code)
	{
		if(code == null) throw new ArgumentException();
		Run(userToken, false, code, runInANewThread);
	}

	/// <summary>Runs the given delegate as the user referenced by the given <see cref="WindowsIdentity"/>, optionally executing it
	/// in a new thread.
	/// </summary>
	public static void RunWithImpersonation(WindowsIdentity user, bool runInANewThread, ThreadStart code)
	{
		if(code == null) throw new ArgumentException();
		Run(user, code, runInANewThread);
	}

	/// <summary>Runs the given delegate as the given user, optionally executing it in a new thread. The user name can be in either
	/// DOMAIN\user or user@domain format.
	/// </summary>
	/// <param name="allowNetworkAccess">If true, the user will be logged on with network access.
	/// If false, the user will be logged on as a batch process.
	/// </param>
	public static void RunWithImpersonation(string userName, string password, bool allowNetworkAccess, bool runInANewThread,
	                                        ThreadStart code)
	{
		if(code == null) throw new ArgumentException();
		Run(LogonUser(GetDomain(userName), GetUserName(userName), password, allowNetworkAccess), true, code, runInANewThread);
	}

	/// <summary>Runs the given delegate as the given user, optionally executing it in a new thread. The user name can be in either
	/// DOMAIN\user or user@domain format.
	/// </summary>
	/// <param name="allowNetworkAccess">If true, the user will be logged on with network access.
	/// If false, the user will be logged on as a batch process.
	/// </param>
	public static void RunWithImpersonation(string userName, SecureString password, bool allowNetworkAccess, bool runInANewThread,
	                                        ThreadStart code)
	{
		if(code == null) throw new ArgumentException();
		Run(LogonUser(GetDomain(userName), GetUserName(userName), password, allowNetworkAccess), true, code, runInANewThread);
	}

	/// <summary>Runs the given delegate as the given user, optionally executing it in a new thread.</summary>
	/// <param name="allowNetworkAccess">If true, the user will be logged on with network access.
	/// If false, the user will be logged on as a batch process.
	/// </param>
	public static void RunWithImpersonation(string domain, string userName, string password, bool allowNetworkAccess,
	                                        bool runInANewThread, ThreadStart code)
	{
		if(code == null) throw new ArgumentException();
		Run(LogonUser(domain, userName, password, allowNetworkAccess), true, code, runInANewThread);
	}

	/// <summary>Runs the given delegate as the given user, optionally executing it in a new thread.</summary>
	/// <param name="allowNetworkAccess">If true, the user will be logged on with network access.
	/// If false, the user will be logged on as a batch process.
	/// </param>
	public static void RunWithImpersonation(string domain, string userName, SecureString password, bool allowNetworkAccess,
	                                        bool runInANewThread, ThreadStart code)
	{
		if(code == null) throw new ArgumentException();
		Run(LogonUser(domain, userName, password, allowNetworkAccess), true, code, runInANewThread);
	}

	void Dispose(bool finalizing)
	{
		if(context != null)
		{
			context.Undo();
			context = null;
		}

		if(ownedToken != IntPtr.Zero)
		{
			CloseHandle(ownedToken);
			ownedToken = IntPtr.Zero;
		}
	}

	WindowsImpersonationContext context;
	IntPtr ownedToken;

	delegate Impersonation ImpersonationMaker();

	/// <summary>Retrieves the domain name from a user name in either user@domain or DOMAIN\user format, suitable for passing to
	/// the <see cref="LogonUser"/> method.
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

	/// <summary>Retrieves the user name from a user name in either user@domain or DOMAIN\user format, suitable for passing to
	/// the <see cref="LogonUser"/> method.
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

	/// <summary>Logs on the given user, returning the corresponding user token.</summary>
	static IntPtr LogonUser(string domain, string userName, string password, bool allowNetworkAccess)
	{
		ValidateLogonArguments(domain, userName, password);
		IntPtr handle;
		int logonType = allowNetworkAccess ? LOGON32_LOGON_NETWORK : LOGON32_LOGON_BATCH;
		if(!LogonUser(userName, domain, password, logonType, LOGON32_PROVIDER_DEFAULT, out handle))
		{
			throw new Win32Exception();
		}
		return handle;
	}

	/// <summary>Logs on the given user, returning the corresponding user token.</summary>
	static IntPtr LogonUser(string domain, string userName, SecureString password, bool allowNetworkAccess)
	{
		ValidateLogonArguments(domain, userName, password);
		IntPtr handle;
		int logonType = allowNetworkAccess ? LOGON32_LOGON_NETWORK : LOGON32_LOGON_BATCH;
		if(!LogonUser(userName, domain, password, logonType, LOGON32_PROVIDER_DEFAULT, out handle))
		{
			throw new Win32Exception();
		}
		return handle;
	}

	static void Run(WindowsIdentity identity, ThreadStart code, bool runInANewThread)
	{
		Run(delegate { return new Impersonation(identity); }, code, runInANewThread);
	}

	static void Run(IntPtr token, bool ownToken, ThreadStart code, bool runInANewThread)
	{
		Run(delegate { return new Impersonation(token, ownToken); }, code, runInANewThread);
	}

	static void Run(ImpersonationMaker impersonationMaker, ThreadStart code, bool runInANewThread)
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

	const int LOGON32_LOGON_NETWORK = 3, LOGON32_LOGON_BATCH = 4, LOGON32_PROVIDER_DEFAULT = 0;

	[DllImport("advapi32.dll", SetLastError=true)]
	static extern bool LogonUser(string userName, string domain, string password, int logonType, int logonProvider,
	                             out IntPtr userToken);

	[DllImport("advapi32.dll", SetLastError=true)]
	static extern bool LogonUser(string userName, string domain, SecureString password, int logonType, int logonProvider,
	                             out IntPtr userToken);

	[DllImport("kernel32.dll", ExactSpelling=true)]
	static extern bool CloseHandle(IntPtr handle);
}

} // namespace AdamMil.Security
