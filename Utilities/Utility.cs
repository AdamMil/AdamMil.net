/*
AdamMil.Utilities is a library providing generally useful utilities for
.NET devplopment.

http://www.adammil.net/
Copyright (C) 2010 Adam Milazzo

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

namespace AdamMil.Utilities
{

/// <summary>Provides very general utilities to aid .NET development.</summary>
public static class Utility
{
	/// <summary>Disposes the given object if it's not null.</summary>
	public static void Dispose(IDisposable obj)
	{
		if(obj != null) obj.Dispose();
	}

	/// <summary>Disposes the given object if it's disposable and not null.</summary>
	public static void Dispose(object obj)
	{
		IDisposable disposable = obj as IDisposable;
		if(disposable != null) disposable.Dispose();
	}

	/// <summary>Disposes the given object if it's not null, and sets it to null.</summary>
	public static void Dispose<T>(ref T obj) where T : IDisposable
	{
		if(obj != null)
		{
			obj.Dispose();
			obj = default(T);
		}
	}
}

} // namespace AdamMil.Utilities
