/*
AdamMil.Utilities is a library providing generally useful utilities for
.NET development.

http://www.adammil.net/
Copyright (C) 2010-2011 Adam Milazzo

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
[System.Security.SuppressUnmanagedCodeSecurity]
public static class Unsafe
{
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
  /// <param name="count">The number of bytes to copy.</param>
  public static unsafe void Copy(void* src, void* dest, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();

    // tests show that this method is much faster than both RtlMoveMemory (i.e. memcpy) and the cpblk IL opcode for small amounts
    // of memory, and often for large ones. the cpblk opcode seems unreliable in that it is faster sometimes, but much slower
    // other times. i don't understand that. RtlMoveMemory at least has consistent performance, and the code switches to it when
    // it would be advantageous to do so (as measured on my machine)

    // this doesn't check for all overlaps, only for overlaps that would impact the main copy algorithm (i.e. when the source
    // block comes starts before and overlaps the destination block in memory)
    bool overlap = src < (byte*)dest+count && (byte*)src+count > dest;

    if(overlap) // we'll handle overlap by copying backwards from the end
    {
      #if WINDOWS
      if(count > 132) // RtlMoveMemory is measured to be faster than the loop below for overlapping blocks larger than ~132 bytes
      {
        RtlMoveMemory(dest, src, new IntPtr(count));
      }
      else
      #endif
      if(count != 0)
      {
        // TODO: this could be optimized... (if we do so, recalibrate the 132 byte threshold above)
        byte* sp = (byte*)src + count - 1;
        dest = (byte*)dest + count - 1;
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
        if(count >= 16)
        {
          // try to align the writes if possible (if the pointers are misaligned by different amounts, we'll only be able to
          // align one of them, and we'll assume aligned writes are more important than aligned reads)
          int offset = (int)dest & 3;
          if(offset != 0)
          {
            offset = 4-offset; // figure out how many bytes we need to copy to align them
            count -= offset;
            do
            {
              *(byte*)dest = *(byte*)src;
              src  = (byte*)src + 1;
              dest = (byte*)dest + 1;
            } while(--offset != 0);
            if(count < 16) goto lastBytes; // if there's not enough space left to do the unrolled version, don't
          }

          #if WINDOWS
          if(count > 512) // RtlMoveMemory is measured to be faster than the below code after about 512 bytes (although it sucks
          {               // with unaligned pointers, so we use it after alignment)
            RtlMoveMemory(dest, src, new IntPtr(count));
            return;
          }
          #endif

          do // copy as many 16 byte blocks as we can
          {
            *(uint*)dest = *(uint*)src;
            *(uint*)((byte*)dest+4)  = *(uint*)((byte*)src+4);
            *(uint*)((byte*)dest+8)  = *(uint*)((byte*)src+8);
            *(uint*)((byte*)dest+12) = *(uint*)((byte*)src+12);
            src    = (byte*)src + 16;
            dest   = (byte*)dest + 16;
            count -= 16;
          } while(count >= 16);
        }

        lastBytes:
        if(count != 0) // now copy whatever bytes remain
        {
          if((count & 8) != 0)
          {
            *(uint*)dest = *(uint*)src;
            *(uint*)((byte*)dest+4) = *(uint*)((byte*)src+4);
            src  = (byte*)src  + 8;
            dest = (byte*)dest + 8;
          }
          if((count & 4) != 0)
          {
            *(uint*)dest = *(uint*)src;
            src  = (byte*)src  + 4;
            dest = (byte*)dest + 4;
          }
          if((count & 2) != 0)
          {
            *(ushort*)dest = *(ushort*)src;
            src  = (byte*)src  + 2;
            dest = (byte*)dest + 2;
          }
          if((count & 1) != 0)
          {
            *(byte*)dest = *(byte*)src;
          }
        }
      }
      else // the architecture is at least 64-bit, so copy 64-bit words
      {
        if(count >= 32)
        {
          // try to align the writes if possible (if the pointers are misaligned by different amounts, we'll only be able to
          // align one of them, and we'll assume aligned writes are more important than aligned reads)
          int offset = (int)dest & 7;
          if(offset != 0)
          {
            offset = 8-offset; // figure out how many bytes we need to copy to align them
            count -= offset;
            do
            {
              *(byte*)dest = *(byte*)src;
              src  = (byte*)src + 1;
              dest = (byte*)dest + 1;
            } while(--offset != 0);
            if(count < 32) goto lastBytes; // if there's not enough space left to do the unrolled version, don't
          }

          // TODO: this threshold is probably different on a 64-bit machine, so profile this code on one to find the new value
          #if WINDOWS
          if(count > 512) // RtlMoveMemory is measured to be faster than the below code after about 512 bytes (although it sucks
          {               // with unaligned pointers, so we use it after alignment)
            RtlMoveMemory(dest, src, new IntPtr(count));
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
            count -= 32;
          } while(count >= 32);
        }

        lastBytes:
        if(count != 0) // now copy whatever bytes remain
        {
          if((count & 16) != 0)
          {
            *(ulong*)dest = *(ulong*)src;
            *(ulong*)((byte*)dest+8) = *(ulong*)((byte*)src+8);
            src  = (byte*)src  + 16;
            dest = (byte*)dest + 16;
          }
          if((count & 8) != 0)
          {
            *(ulong*)dest = *(ulong*)src;
            src  = (byte*)src  + 8;
            dest = (byte*)dest + 8;
          }
          if((count & 4) != 0)
          {
            *(uint*)dest = *(uint*)src;
            src  = (byte*)src  + 4;
            dest = (byte*)dest + 4;
          }
          if((count & 2) != 0)
          {
            *(ushort*)dest = *(ushort*)src;
            src  = (byte*)src  + 2;
            dest = (byte*)dest + 2;
          }
          if((count & 1) != 0)
          {
            *(byte*)dest = *(byte*)src;
          }
        }
      }
    }
  }

  /// <summary>This method fills a block of memory with a specified byte value.</summary>
  /// <param name="dest">The pointer to the memory region that will be filled.</param>
  /// <param name="value">The byte value with which the memory region should be filled.</param>
  /// <param name="count">The number of bytes to fill.</param>
  public static unsafe void Fill(void* dest, byte value, int count)
  {
    if(sizeof(IntPtr) < 8) // if the architecture is not at least 64-bit...
    {
      int word = (value<<8) | value;
      FillCore(dest, (uint)((word<<16) | word), count);
    }
    else // at least 64-bit architecture
    {
      long word = (value<<8) | value;
      word = (word<<16) | word;
      FillCore(dest, (ulong)((word<<32) | word), count);
    }
  }

  static unsafe void FillCore(void* dest, uint value, int count)
  {
    if(sizeof(IntPtr) < 8) // reduce the amount of code generated if this function won't be called on this architecture
    {
      if(count < 0) throw new ArgumentOutOfRangeException();

      if(count >= 16)
      {
        // align the write pointer
        int offset = (int)dest & 3;
        if(offset != 0)
        {
          offset = 4-offset;
          count -= offset;
          do
          {
            *(byte*)dest = (byte)value;
            dest = (byte*)dest + 1;
          } while(--offset != 0);
          if(count < 16) goto lastBytes; // if there's not enough space left to do the unrolled version, don't
        }

        #if WINDOWS
        if(count > 640)
        {
          if(value == 0)
          {
            RtlZeroMemory(dest, new IntPtr(count)); // RtlZeroMemory is faster after about 640 bytes (if the pointer is aligned)
            return;
          }
          else if(count > 768) // RtlFillMemory is faster after about 768 bytes (if the pointer is aligned)
          {
            RtlFillMemory(dest, new IntPtr(count), (byte)value);
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
          count -= 16;
        } while(count >= 16);
      }

      lastBytes:
      if(count != 0) // now fill whatever bytes are left
      {
        if((count & 8) != 0)
        {
          *(uint*)dest = value;
          *(uint*)((byte*)dest+4) = value;
          dest = (byte*)dest + 8;
        }
        if((count & 4) != 0)
        {
          *(uint*)dest = value;
          dest = (byte*)dest + 4;
        }
        if((count & 2) != 0)
        {
          *(ushort*)dest = (ushort)value;
          dest = (byte*)dest + 2;
        }
        if((count & 1) != 0)
        {
          *(byte*)dest = (byte)value;
        }
      }
    }
  }

  static unsafe void FillCore(void* dest, ulong value, int count)
  {
    if(sizeof(IntPtr) >= 8) // reduce the amount of code generated if this function won't be called on this architecture
    {
      if(count < 0) throw new ArgumentOutOfRangeException();

      if(count >= 32)
      {
        // align the write pointer
        int offset = (int)dest & 7;
        if(offset != 0)
        {
          offset = 8-offset;
          count -= offset;
          do
          {
            *(byte*)dest = (byte)value;
            dest = (byte*)dest + 1;
          } while(--offset != 0);
          if(count < 32) goto lastBytes; // if there's not enough space left to do the unrolled version, don't
        }

        // TODO: these thresholds are probably different on a 64-bit machine, so profile this code on one to find the new values
        #if WINDOWS
        if(count > 640)
        {
          if(value == 0)
          {
            RtlZeroMemory(dest, new IntPtr(count)); // RtlZeroMemory is faster after about 640 bytes (if the pointer is aligned)
            return;
          }
          else if(count > 768) // RtlFillMemory is faster after about 768 bytes (if the pointer is aligned)
          {
            RtlFillMemory(dest, new IntPtr(count), (byte)value);
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
          count -= 32;
        } while(count >= 32);
      }

      lastBytes:
      if(count != 0) // now fill whatever bytes are left
      {
        if((count & 16) != 0)
        {
          *(ulong*)dest            = value;
          *(ulong*)((byte*)dest+8) = value;
          dest = (byte*)dest + 16;
        }
        if((count & 8) != 0)
        {
          *(ulong*)dest = value;
          dest = (byte*)dest + 8;
        }
        if((count & 4) != 0)
        {
          *(uint*)dest = (uint)value;
          dest = (byte*)dest + 4;
        }
        if((count & 2) != 0)
        {
          *(ushort*)dest = (ushort)value;
          dest = (byte*)dest + 2;
        }
        if((count & 1) != 0)
        {
          *(byte*)dest = (byte)value;
        }
      }
    }
  }

  #if WINDOWS
  // we use IntPtr for the length because the API uses size_t, which is 64-bit on 64-bit machines
  [DllImport("ntdll.dll", ExactSpelling=true)]
  unsafe static extern void RtlFillMemory(void* dest, IntPtr length, byte value);
  [DllImport("ntdll.dll", ExactSpelling=true)]
  unsafe static extern void RtlMoveMemory(void* dest, void* src, IntPtr length);
  [DllImport("ntdll.dll", ExactSpelling=true)]
  unsafe static extern void RtlZeroMemory(void* dest, IntPtr length);
  #endif
}

} // namespace AdamMil.Utilities
