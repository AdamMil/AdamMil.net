/*
AdamMil.Collections is a library that provides useful collection classes for
the .NET framework.

http://www.adammil.net/
Copyright (C) 2010-2013 Adam Milazzo

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

namespace AdamMil.Collections
{

#region SafeNativeMethods
[System.Security.SuppressUnmanagedCodeSecurity]
static class SafeNativeMethods
{
  [DllImport("kernel32.dll", ExactSpelling=true)]
  public static extern long GetTickCount64(); // NOTE: this only exists in Windows Vista and later

  public static readonly bool IsWindowsVistaOrLater =
    Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 6;
}
#endregion

} // namespace AdamMil.Collections
