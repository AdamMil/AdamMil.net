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
using System.Runtime.InteropServices;

namespace AdamMil.Utilities
{

#region Unsafe
/// <summary>This class provides methods to help when working with unsafe code.</summary>
[CLSCompliant(false)]
[System.Security.SuppressUnmanagedCodeSecurity]
public static class Unsafe
{
  /// <summary>This method checks two blocks of memory for equality.</summary>
  public static unsafe bool AreEqual(void* a, void* b, int byteCount)
  {
    if(byteCount < 0) throw new ArgumentOutOfRangeException();

    if(sizeof(IntPtr) < 8) // if this is not a 64-bit (or greater) architecture, do 32-bit comparisons
    {
      if(byteCount >= 16)
      {
        // align the memory if possible. it may only be possible to align one pointer, but that's better than nothing
        int offset = (int)a & 3;
        if(offset != 0)
        {
          offset = 4 - offset;
          byteCount -= offset;
          do
          {
            if(*(byte*)a != *(byte*)b) goto notEqual;
            a = (byte*)a + 1;
            b = (byte*)b + 1;
          } while(--offset != 0);
          if(byteCount < 16) goto lastBytes;
        }

        // now compare as many 16-byte chunks as possible
        do
        {
          if(*(uint*)a != *(uint*)b || *(uint*)((byte*)a+4) != *(uint*)((byte*)b+4) ||
             *(uint*)((byte*)a+8) != *(uint*)((byte*)b+8) || *(uint*)((byte*)a+12) != *(uint*)((byte*)b+12))
          {
            goto notEqual;
          }
          a = ((byte*)a + 16);
          b = ((byte*)b + 16);
          byteCount -= 16;
        } while(byteCount >= 16);
      }

      lastBytes:
      if((byteCount & 8) != 0) // if there are between 8 and 15 bytes remaining, compare the first 8 of them, leaving at most 7
      {
        if(*(uint*)a != *(uint*)b || *(uint*)((byte*)a+4) != *(uint*)((byte*)b+4)) goto notEqual;
        a = ((byte*)a + 8);
        b = ((byte*)b + 8);
      }
    }
    else // the architecture is at least 64-bit, so do 64-bit comparisons
    {
      if(byteCount >= 32)
      {
        // align the memory if possible. it may only be possible to align one pointer, but that's better than nothing
        int offset = (int)a & 7; // we can cast 'a' to an int even though it's larger because we're only interested in the lower 3 bits
        if(offset != 0)
        {
          offset = 8 - offset;
          byteCount -= offset;
          do
          {
            if(*(byte*)a != *(byte*)b) goto notEqual;
            a = (byte*)a + 1;
            b = (byte*)b + 1;
          } while(--offset != 0);
          if(byteCount < 32) goto lastBytes;
        }

        // now compare as many 32-byte chunks as possible
        do
        {
          if(*(ulong*)a != *(ulong*)b || *(ulong*)((byte*)a+8) != *(ulong*)((byte*)b+8) ||
             *(ulong*)((byte*)a+16) != *(ulong*)((byte*)b+16) || *(ulong*)((byte*)a+24) != *(ulong*)((byte*)b+24))
          {
            goto notEqual;
          }
          a = ((byte*)a + 32);
          b = ((byte*)b + 32);
          byteCount -= 32;
        } while(byteCount >= 32);
      }

      lastBytes:
      if((byteCount & 16) != 0) // if there are between 16 and 31 bytes remaining, compare the first 16 of them, leaving at most 15
      {
        if(*(ulong*)a != *(ulong*)b || *(ulong*)((byte*)a+8) != *(ulong*)((byte*)b+8)) goto notEqual;
        a = ((byte*)a + 16);
        b = ((byte*)b + 16);
      }
      if((byteCount & 8) != 0) // if there are between 8 and 15 bytes remaining, compare the first 8 of them, leaving at most 7
      {
        if(*(ulong*)a != *(ulong*)b) goto notEqual;
        a = ((byte*)a + 8);
        b = ((byte*)b + 8);
      }
    }

    // it would be possible to avoid some branching here and below by comparing uints after masking off the unwanted bits, but i don't want
    // to read past the end of the user's buffer for fear it just happens to lie at the end of a segment
    switch(byteCount & 7)
    {
      case 1:
        if(*(byte*)a != *(byte*)b) goto notEqual;
        break;
      case 2:
        if(*(ushort*)a != *(ushort*)b) goto notEqual;
        break;
      case 3:
        if(*(ushort*)a != *(ushort*)b || *((byte*)a+2) != *((byte*)b+2)) goto notEqual;
        break;
      case 4:
        if(*(uint*)a != *(uint*)b) goto notEqual;
        break;
      case 5:
        if(*(uint*)a != *(uint*)b || *((byte*)a+4) != *((byte*)b+4)) goto notEqual;
        break;
      case 6:
        if(*(uint*)a != *(uint*)b || *(ushort*)((byte*)a+4) != *(ushort*)((byte*)b+4)) goto notEqual;
        break;
      case 7:
        if(*(uint*)a != *(uint*)b || *(ushort*)((byte*)a+4) != *(ushort*)((byte*)b+4) || *((byte*)a+6) != *((byte*)b+6)) goto notEqual;
        break;
    }

    return true;

    notEqual:
    return false;
  }

  /// <summary>This method fills a block of memory with zeros.</summary>
  /// <param name="dest">A pointer to the beginning of the block of memory.</param>
  /// <param name="length">The number of bytes to fill with zeros.</param>
  public static unsafe void Clear(void* dest, int length)
  {
    if(sizeof(IntPtr) < 8) FillCore(dest, (uint)0, length);
    else FillCore(dest, (ulong)0, length);
  }

  /// <summary>This method copies a block of memory to another location.</summary>
  /// <param name="src">A pointer to the beginning of the source block of memory.</param>
  /// <param name="dest">The destination where the source data will be copied.</param>
  /// <param name="byteCount">The number of bytes to copy.</param>
  public static unsafe void Copy(void* src, void* dest, int byteCount)
  {
    if(byteCount < 0) throw new ArgumentOutOfRangeException();

    // tests show that this method is much faster than both RtlMoveMemory (i.e. memcpy) and the cpblk IL opcode for small amounts
    // of memory, and often for large ones. the cpblk opcode seems unreliable in that it is faster sometimes, but much slower
    // other times. i don't understand that. RtlMoveMemory at least has consistent performance, and the code switches to it when
    // it would be advantageous to do so (as measured on my machine). the method is faster than Array.Copy only for small amounts of data.
    // the ArrayUtility.SmallCopy() method exists to switch between this method and Array.Copy() based on measured thresholds

    // check if the blocks overlap. this doesn't check for all overlaps, only for overlaps that would impact the main copy algorithm (i.e.
    // when the source block comes starts before and overlaps the destination block in memory)
    if(src < (byte*)dest+byteCount && (byte*)src+byteCount > dest)
    {
      #if WINDOWS
      if(byteCount > 132) // RtlMoveMemory is measured to be faster than the loop below for overlapping blocks larger than ~132 bytes
      {
        UnsafeNativeMethods.RtlMoveMemory(dest, src, new IntPtr(byteCount));
      }
      else
      #endif
      if(byteCount != 0) // we'll handle overlap by copying backwards from the end
      {
        // TODO: this could be optimized... (if we do so, recalibrate the 132 byte threshold above)
        byte* sp = (byte*)src + byteCount - 1;
        dest = (byte*)dest + byteCount - 1;
        do
        {
          *(byte*)dest = *(byte*)sp;
          dest = (byte*)dest - 1;
          sp   = (byte*)sp   - 1;
        } while(sp >= src);
      }
    }
    else // the memory blocks don't overlap in a way that would affect the copy
    {
      if(sizeof(IntPtr) < 8) // if the architecture is not at least 64-bit, then copy 32-bit words
      {
        if(byteCount >= 16)
        {
          // try to align the writes if possible (if the pointers are misaligned by different amounts, we'll only be able to
          // align one of them, and we'll assume aligned writes are more important than aligned reads)
          int offset = (int)dest & 3;
          if(offset != 0)
          {
            offset = 4-offset; // figure out how many bytes we need to copy to align them
            byteCount -= offset;
            do
            {
              *(byte*)dest = *(byte*)src;
              src  = (byte*)src  + 1;
              dest = (byte*)dest + 1;
            } while(--offset != 0);
            if(byteCount < 16) goto lastBytes; // if there's not enough space left to do the unrolled version, don't
          }

          #if WINDOWS
          if(byteCount > 512) // RtlMoveMemory is measured to be faster than the below code after about 512 bytes (although it sucks
          {                   // with unaligned pointers, so we use it after alignment)
            UnsafeNativeMethods.RtlMoveMemory(dest, src, new IntPtr(byteCount));
            return;
          }
          #endif

          do // copy as many 16 byte blocks as we can
          {
            *(uint*)dest = *(uint*)src;
            *(uint*)((byte*)dest+4)  = *(uint*)((byte*)src+4);
            *(uint*)((byte*)dest+8)  = *(uint*)((byte*)src+8);
            *(uint*)((byte*)dest+12) = *(uint*)((byte*)src+12);
            src    = (byte*)src  + 16;
            dest   = (byte*)dest + 16;
            byteCount -= 16;
          } while(byteCount >= 16);
        }

        lastBytes:
        if((byteCount & 8) != 0)
        {
          *(uint*)dest = *(uint*)src;
          *(uint*)((byte*)dest+4) = *(uint*)((byte*)src+4);
          src  = (byte*)src  + 8;
          dest = (byte*)dest + 8;
        }
      }
      else // the architecture is at least 64-bit, so copy 64-bit words
      {
        if(byteCount >= 32)
        {
          // try to align the writes if possible (if the pointers are misaligned by different amounts, we'll only be able to
          // align one of them, and we'll assume aligned writes are more important than aligned reads)
          int offset = (int)dest & 7;
          if(offset != 0)
          {
            offset = 8-offset; // figure out how many bytes we need to copy to align them
            byteCount -= offset;
            do
            {
              *(byte*)dest = *(byte*)src;
              src  = (byte*)src  + 1;
              dest = (byte*)dest + 1;
            } while(--offset != 0);
            if(byteCount < 32) goto lastBytes; // if there's not enough space left to do the unrolled version, don't
          }

          // TODO: this threshold is probably different on a 64-bit machine, so profile this code on one to find the new value
          #if WINDOWS
          if(byteCount > 512) // RtlMoveMemory is measured to be faster than the below code after about 512 bytes (although it sucks
          {               // with unaligned pointers, so we use it after alignment)
            UnsafeNativeMethods.RtlMoveMemory(dest, src, new IntPtr(byteCount));
            return;
          }
          #endif

          do // copy as many 32 byte blocks as we can
          {
            *(ulong*)dest = *(ulong*)src;
            *(ulong*)((byte*)dest+8)  = *(ulong*)((byte*)src+8);
            *(ulong*)((byte*)dest+16) = *(ulong*)((byte*)src+16);
            *(ulong*)((byte*)dest+24) = *(ulong*)((byte*)src+24);
            src    = (byte*)src  + 32;
            dest   = (byte*)dest + 32;
            byteCount -= 32;
          } while(byteCount >= 32);
        }

        lastBytes:
        if((byteCount & 16) != 0)
        {
          *(ulong*)dest = *(ulong*)src;
          *(ulong*)((byte*)dest+8) = *(ulong*)((byte*)src+8);
          src  = (byte*)src  + 16;
          dest = (byte*)dest + 16;
        }
        if((byteCount & 8) != 0)
        {
          *(ulong*)dest = *(ulong*)src;
          src  = (byte*)src  + 8;
          dest = (byte*)dest + 8;
        }
      }

      switch(byteCount & 7)
      {
        case 1: *(byte*)dest = *(byte*)src; break;
        case 2: *(ushort*)dest = *(ushort*)src; break;
        case 3:
          *(ushort*)dest   = *(ushort*)src;
          *((byte*)dest+2) = *((byte*)src+2);
          break;
        case 4: *(uint*)dest = *(uint*)src; break;
        case 5:
          *(uint*)dest     = *(uint*)src;
          *((byte*)dest+4) = *((byte*)src+4);
          break;
        case 6:
          *(uint*)dest = *(uint*)src;
          *(ushort*)((byte*)dest+4) = *(ushort*)((byte*)src+4);
          break;
        case 7:
          *(uint*)dest = *(uint*)src;
          *(ushort*)((byte*)dest+4) = *(ushort*)((byte*)src+4);
          *((byte*)dest+6) = *((byte*)src+6);
          break;
      }
    }
  }

  /// <summary>This method fills a block of memory with a specified byte value.</summary>
  /// <param name="dest">The pointer to the memory region that will be filled.</param>
  /// <param name="value">The byte value with which the memory region should be filled.</param>
  /// <param name="byteCount">The number of bytes to fill.</param>
  public static unsafe void Fill(void* dest, byte value, int byteCount)
  {
    if(sizeof(IntPtr) < 8) // if the architecture is not at least 64-bit...
    {
      int word = (value<<8) | value;
      FillCore(dest, (uint)((word<<16) | word), byteCount);
    }
    else // at least 64-bit architecture
    {
      long word = (value<<8) | value;
      word = (word<<16) | word;
      FillCore(dest, (ulong)((word<<32) | word), byteCount);
    }
  }

  static unsafe void FillCore(void* dest, uint value, int byteCount)
  {
    if(sizeof(IntPtr) < 8) // reduce the amount of code generated if this function won't be called on this architecture
    {
      if(byteCount < 0) throw new ArgumentOutOfRangeException();

      if(byteCount >= 16)
      {
        // align the write pointer
        int offset = (int)dest & 3;
        if(offset != 0)
        {
          offset = 4-offset;
          byteCount -= offset;
          do
          {
            *(byte*)dest = (byte)value;
            dest = (byte*)dest + 1;
          } while(--offset != 0);
          if(byteCount < 16) goto lastBytes; // if there's not enough space left to do the unrolled version, don't
        }

        #if WINDOWS
        if(byteCount > 640)
        {
          if(value == 0)
          {
            UnsafeNativeMethods.RtlZeroMemory(dest, new IntPtr(byteCount)); // RtlZeroMemory is faster after about 640 bytes (if the pointer is aligned)
            return;
          }
          else if(byteCount > 768) // RtlFillMemory is faster after about 768 bytes (if the pointer is aligned)
          {
            UnsafeNativeMethods.RtlFillMemory(dest, new IntPtr(byteCount), (byte)value);
            return;
          }
        }
        #endif

        do // fill as many 16-byte blocks as possible
        {
          *(uint*)dest = value;
          *(uint*)((byte*)dest+4)  = value;
          *(uint*)((byte*)dest+8)  = value;
          *(uint*)((byte*)dest+12) = value;
          dest   = (byte*)dest + 16;
          byteCount -= 16;
        } while(byteCount >= 16);
      }

      lastBytes:
      if((byteCount & 8) != 0)
      {
        *(uint*)dest = value;
        *(uint*)((byte*)dest+4) = value;
        dest = (byte*)dest + 8;
        byteCount &= 7;
      }

      switch(byteCount)
      {
        case 1: *(byte*)dest = (byte)value; break;
        case 2: *(ushort*)dest = (ushort)value; break;
        case 3:
          *(ushort*)dest   = (ushort)value;
          *((byte*)dest+2) = (byte)value;
          break;
        case 4: *(uint*)dest = value; break;
        case 5: 
          *(uint*)dest     = value;
          *((byte*)dest+4) = (byte)value;
          break;
        case 6:
          *(uint*)dest = value;
          *(ushort*)((byte*)dest+4) = (ushort)value;
          break;
        case 7:
          *(uint*)dest = value;
          *(ushort*)((byte*)dest+4) = (ushort)value;
          *((byte*)dest+6) = (byte)value;
          break;
      }
    }
  }

  static unsafe void FillCore(void* dest, ulong value, int byteCount)
  {
    if(sizeof(IntPtr) >= 8) // reduce the amount of code generated if this function won't be called on this architecture
    {
      if(byteCount < 0) throw new ArgumentOutOfRangeException();

      if(byteCount >= 32)
      {
        // align the write pointer
        int offset = (int)dest & 7;
        if(offset != 0)
        {
          offset = 8-offset;
          byteCount -= offset;
          do
          {
            *(byte*)dest = (byte)value;
            dest = (byte*)dest + 1;
          } while(--offset != 0);
          if(byteCount < 32) goto lastBytes; // if there's not enough space left to do the unrolled version, don't
        }

        // TODO: these thresholds are probably different on a 64-bit machine, so profile this code on one to find the new values
        #if WINDOWS
        if(byteCount > 640)
        {
          if(value == 0)
          {
            UnsafeNativeMethods.RtlZeroMemory(dest, new IntPtr(byteCount)); // RtlZeroMemory is faster after about 640 bytes (if aligned)
            return;
          }
          else if(byteCount > 768) // RtlFillMemory is faster after about 768 bytes (if the pointer is aligned)
          {
            UnsafeNativeMethods.RtlFillMemory(dest, new IntPtr(byteCount), (byte)value);
            return;
          }
        }
        #endif

        do // fill as many 32-byte blocks as possible
        {
          *(ulong*)dest = value;
          *(ulong*)((byte*)dest+8)  = value;
          *(ulong*)((byte*)dest+16) = value;
          *(ulong*)((byte*)dest+24) = value;
          dest   = (byte*)dest + 32;
          byteCount -= 32;
        } while(byteCount >= 32);
      }

      lastBytes:
      if((byteCount & 16) != 0)
      {
        *(ulong*)dest            = value;
        *(ulong*)((byte*)dest+8) = value;
        dest = (byte*)dest + 16;
      }
      if((byteCount & 8) != 0)
      {
        *(ulong*)dest = value;
        dest = (byte*)dest + 8;
      }

      switch(byteCount)
      {
        case 1: *(byte*)dest = (byte)value; break;
        case 2: *(ushort*)dest = (ushort)value; break;
        case 3:
          *(ushort*)dest   = (ushort)value;
          *((byte*)dest+2) = (byte)value;
          break;
        case 4: *(uint*)dest = (uint)value; break;
        case 5:
          *(uint*)dest     = (uint)value;
          *((byte*)dest+4) = (byte)value;
          break;
        case 6:
          *(uint*)dest = (uint)value;
          *(ushort*)((byte*)dest+4) = (ushort)value;
          break;
        case 7:
          *(uint*)dest = (uint)value;
          *(ushort*)((byte*)dest+4) = (ushort)value;
          *((byte*)dest+6) = (byte)value;
          break;
      }
    }
  }
}
#endregion

#region SafeNativeMethods
[System.Security.SuppressUnmanagedCodeSecurity]
static class SafeNativeMethods
{
  [DllImport("kernel32.dll", ExactSpelling=true)]
  public static extern long GetTickCount64(); // NOTE: this only exists in Windows Vista and later

  [DllImport("kernel32.dll", ExactSpelling=true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static extern bool SwitchToThread();

  public static readonly bool IsWindowsVistaOrLater =
    Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 6;
}
#endregion

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

  // we use IntPtr for the length because the API uses size_t, which is 64-bit on 64-bit machines
  [DllImport("ntdll.dll", ExactSpelling=true)]
  public unsafe static extern void RtlFillMemory(void* dest, IntPtr length, byte value);

  [DllImport("ntdll.dll", ExactSpelling=true)]
  public unsafe static extern void RtlMoveMemory(void* dest, void* src, IntPtr length);

  [DllImport("ntdll.dll", ExactSpelling=true)]
  public unsafe static extern void RtlZeroMemory(void* dest, IntPtr length);
}
#endregion

} // namespace AdamMil.Utilities
