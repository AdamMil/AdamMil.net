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
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("AdamMil.net Utilities")]
[assembly: AssemblyDescription("A set of generally useful utilities for .NET development.")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyProduct("AdamMil.net")]
[assembly: AssemblyCopyright("Copyright © Adam Milazzo 2010-2016")]

[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
