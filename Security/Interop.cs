/*
AdamMil.Security is a .NET library providing OpenPGP-based security.
http://www.adammil.net/
Copyright (C) 2008-2013 Adam Milazzo

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
using System.Runtime.InteropServices;

namespace AdamMil.Security
{

#region UnsafeNativeMethods
[System.Security.SuppressUnmanagedCodeSecurity]
static class UnsafeNativeMethods
{
  [DllImport("kernel32.dll", ExactSpelling=true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static extern bool CloseHandle(IntPtr handle);

  [DllImport("advapi32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static extern bool LogonUser(string userName, string domain, string password, int logonType, int logonProvider,
                                      out IntPtr logonToken);

  [DllImport("advapi32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static extern bool LogonUser(string userName, string domain, IntPtr password, int logonType, int logonProvider,
                                      out IntPtr logonToken);
}
#endregion

} // namespace AdamMil.Security
