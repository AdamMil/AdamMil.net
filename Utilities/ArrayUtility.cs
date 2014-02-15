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

// this code has been auto-generated from ArrayUtility.tt

using System;

namespace AdamMil.Utilities
{

/// <summary>Provides utilities for array data types.</summary>
public static partial class ArrayUtility
{
    /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
        public static void SmallCopy(byte[] source, byte[] dest, int length)
    {
      SmallCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
        public static unsafe void SmallCopy(byte[] source, int sourceIndex, byte[] dest, int destIndex, int length)
    {
      if(length > 128)
      {
        Array.Copy(source, sourceIndex, dest, destIndex, length);
      }
      else
      {
        if(source == null || dest == null) throw new ArgumentNullException();
        if((sourceIndex|destIndex|length) < 0 || sourceIndex+length > source.Length || destIndex+length > dest.Length)
        {
          throw new ArgumentOutOfRangeException();
        }
        fixed(byte* psrc=source, pdest=dest)
        {
          Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(byte));
        }
      }
    }
      /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
        public static void SmallCopy(char[] source, char[] dest, int length)
    {
      SmallCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
        public static unsafe void SmallCopy(char[] source, int sourceIndex, char[] dest, int destIndex, int length)
    {
      if(length > 80)
      {
        Array.Copy(source, sourceIndex, dest, destIndex, length);
      }
      else
      {
        if(source == null || dest == null) throw new ArgumentNullException();
        if((sourceIndex|destIndex|length) < 0 || sourceIndex+length > source.Length || destIndex+length > dest.Length)
        {
          throw new ArgumentOutOfRangeException();
        }
        fixed(char* psrc=source, pdest=dest)
        {
          Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(char));
        }
      }
    }
      /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
        public static void SmallCopy(double[] source, double[] dest, int length)
    {
      SmallCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
        public static unsafe void SmallCopy(double[] source, int sourceIndex, double[] dest, int destIndex, int length)
    {
      if(length > 64)
      {
        Array.Copy(source, sourceIndex, dest, destIndex, length);
      }
      else
      {
        if(source == null || dest == null) throw new ArgumentNullException();
        if((sourceIndex|destIndex|length) < 0 || sourceIndex+length > source.Length || destIndex+length > dest.Length)
        {
          throw new ArgumentOutOfRangeException();
        }
        fixed(double* psrc=source, pdest=dest)
        {
          Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(double));
        }
      }
    }
      /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
        public static void SmallCopy(float[] source, float[] dest, int length)
    {
      SmallCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
        public static unsafe void SmallCopy(float[] source, int sourceIndex, float[] dest, int destIndex, int length)
    {
      if(length > 40)
      {
        Array.Copy(source, sourceIndex, dest, destIndex, length);
      }
      else
      {
        if(source == null || dest == null) throw new ArgumentNullException();
        if((sourceIndex|destIndex|length) < 0 || sourceIndex+length > source.Length || destIndex+length > dest.Length)
        {
          throw new ArgumentOutOfRangeException();
        }
        fixed(float* psrc=source, pdest=dest)
        {
          Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(float));
        }
      }
    }
      /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
        public static void SmallCopy(int[] source, int[] dest, int length)
    {
      SmallCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
        public static unsafe void SmallCopy(int[] source, int sourceIndex, int[] dest, int destIndex, int length)
    {
      if(length > 40)
      {
        Array.Copy(source, sourceIndex, dest, destIndex, length);
      }
      else
      {
        if(source == null || dest == null) throw new ArgumentNullException();
        if((sourceIndex|destIndex|length) < 0 || sourceIndex+length > source.Length || destIndex+length > dest.Length)
        {
          throw new ArgumentOutOfRangeException();
        }
        fixed(int* psrc=source, pdest=dest)
        {
          Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(int));
        }
      }
    }
      /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
        public static void SmallCopy(long[] source, long[] dest, int length)
    {
      SmallCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
        public static unsafe void SmallCopy(long[] source, int sourceIndex, long[] dest, int destIndex, int length)
    {
      if(length > 20)
      {
        Array.Copy(source, sourceIndex, dest, destIndex, length);
      }
      else
      {
        if(source == null || dest == null) throw new ArgumentNullException();
        if((sourceIndex|destIndex|length) < 0 || sourceIndex+length > source.Length || destIndex+length > dest.Length)
        {
          throw new ArgumentOutOfRangeException();
        }
        fixed(long* psrc=source, pdest=dest)
        {
          Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(long));
        }
      }
    }
      /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
          [CLSCompliant(false)]
        public static void SmallCopy(sbyte[] source, sbyte[] dest, int length)
    {
      SmallCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
          [CLSCompliant(false)]
        public static unsafe void SmallCopy(sbyte[] source, int sourceIndex, sbyte[] dest, int destIndex, int length)
    {
      if(length > 128)
      {
        Array.Copy(source, sourceIndex, dest, destIndex, length);
      }
      else
      {
        if(source == null || dest == null) throw new ArgumentNullException();
        if((sourceIndex|destIndex|length) < 0 || sourceIndex+length > source.Length || destIndex+length > dest.Length)
        {
          throw new ArgumentOutOfRangeException();
        }
        fixed(sbyte* psrc=source, pdest=dest)
        {
          Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(sbyte));
        }
      }
    }
      /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
        public static void SmallCopy(short[] source, short[] dest, int length)
    {
      SmallCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
        public static unsafe void SmallCopy(short[] source, int sourceIndex, short[] dest, int destIndex, int length)
    {
      if(length > 80)
      {
        Array.Copy(source, sourceIndex, dest, destIndex, length);
      }
      else
      {
        if(source == null || dest == null) throw new ArgumentNullException();
        if((sourceIndex|destIndex|length) < 0 || sourceIndex+length > source.Length || destIndex+length > dest.Length)
        {
          throw new ArgumentOutOfRangeException();
        }
        fixed(short* psrc=source, pdest=dest)
        {
          Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(short));
        }
      }
    }
      /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
          [CLSCompliant(false)]
        public static void SmallCopy(uint[] source, uint[] dest, int length)
    {
      SmallCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
          [CLSCompliant(false)]
        public static unsafe void SmallCopy(uint[] source, int sourceIndex, uint[] dest, int destIndex, int length)
    {
      if(length > 40)
      {
        Array.Copy(source, sourceIndex, dest, destIndex, length);
      }
      else
      {
        if(source == null || dest == null) throw new ArgumentNullException();
        if((sourceIndex|destIndex|length) < 0 || sourceIndex+length > source.Length || destIndex+length > dest.Length)
        {
          throw new ArgumentOutOfRangeException();
        }
        fixed(uint* psrc=source, pdest=dest)
        {
          Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(uint));
        }
      }
    }
      /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
          [CLSCompliant(false)]
        public static void SmallCopy(ulong[] source, ulong[] dest, int length)
    {
      SmallCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
          [CLSCompliant(false)]
        public static unsafe void SmallCopy(ulong[] source, int sourceIndex, ulong[] dest, int destIndex, int length)
    {
      if(length > 20)
      {
        Array.Copy(source, sourceIndex, dest, destIndex, length);
      }
      else
      {
        if(source == null || dest == null) throw new ArgumentNullException();
        if((sourceIndex|destIndex|length) < 0 || sourceIndex+length > source.Length || destIndex+length > dest.Length)
        {
          throw new ArgumentOutOfRangeException();
        }
        fixed(ulong* psrc=source, pdest=dest)
        {
          Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(ulong));
        }
      }
    }
      /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
          [CLSCompliant(false)]
        public static void SmallCopy(ushort[] source, ushort[] dest, int length)
    {
      SmallCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="//Utilities/ArrayUtility/SmallCopy/node()"/>
          [CLSCompliant(false)]
        public static unsafe void SmallCopy(ushort[] source, int sourceIndex, ushort[] dest, int destIndex, int length)
    {
      if(length > 80)
      {
        Array.Copy(source, sourceIndex, dest, destIndex, length);
      }
      else
      {
        if(source == null || dest == null) throw new ArgumentNullException();
        if((sourceIndex|destIndex|length) < 0 || sourceIndex+length > source.Length || destIndex+length > dest.Length)
        {
          throw new ArgumentOutOfRangeException();
        }
        fixed(ushort* psrc=source, pdest=dest)
        {
          Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(ushort));
        }
      }
    }
  }

} // namespace AdamMil.Utilities