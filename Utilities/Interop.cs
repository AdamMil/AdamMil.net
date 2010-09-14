/*
AdamMil.Utilities is a library providing generally useful utilities for
.NET development.

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
using System.Runtime.InteropServices;

namespace AdamMil.Utilities
{

/// <summary>This class provides methods to help when working with unsafe code.</summary>
public static class Unsafe
{
	// we use IntPtr for the length because the length uses size_t, which is 64-bit on 64-bit machines
	[DllImport("ntdll.dll", ExactSpelling=true)]
	unsafe static extern void RtlFillMemory(void* dest, IntPtr length, byte value);
	[DllImport("ntdll.dll", ExactSpelling=true)]
	unsafe static extern void RtlMoveMemory(void* dest, void* src, IntPtr length);
	[DllImport("ntdll.dll", ExactSpelling=true)]
	unsafe static extern void RtlZeroMemory(void* dest, IntPtr length);

	/// <summary>This method fills a block of memory with zeros.</summary>
	/// <param name="dest">A pointer to the beginning of the block of memory.</param>
	/// <param name="length">The number of bytes to fill with zeros.</param>
	public static unsafe void Clear(void* dest, int length)
	{
		if(length < 0) throw new ArgumentOutOfRangeException();
		else if(length != 0) RtlZeroMemory(dest, new IntPtr(length));
	}

	/// <summary>This method copies a block of memory to another location.</summary>
	/// <param name="src">A pointer to the beginning of the source block of memory.</param>
	/// <param name="dest">The destination into which the source data will be copied.</param>
	/// <param name="length">The number of bytes to copy.</param>
	public static unsafe void Copy(void* src, void* dest, int length)
	{
		if(length < 0) throw new ArgumentOutOfRangeException("length", length, "must not be negative");
		else if(length != 0) RtlMoveMemory(dest, src, new IntPtr(length));
	}

	/// <summary>This method fills a block of memory with a specified byte value.</summary>
	/// <param name="dest">The pointer to the memory region that will be filled.</param>
	/// <param name="value">The byte value with which the memory region will be filled.</param>
	/// <param name="length">The number of bytes to fill.</param>
	public static unsafe void Fill(void* dest, byte value, int length)
	{
		if(length < 0) throw new ArgumentOutOfRangeException("length", length, "must not be negative");
		else if(length != 0) RtlFillMemory(dest, new IntPtr(length), value);
	}
}

} // namespace AdamMil.Utilities
