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

// NOTE: this code has been auto-generated from ArrayUtility.tt

using System;

namespace AdamMil.Utilities
{

/// <summary>Provides utilities for array data types.</summary>
public static partial class ArrayUtility
{
    /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
        public static void FastCopy(this byte[] source, byte[] dest, int length)    {
      FastCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
        public static unsafe void FastCopy(this byte[] source, int sourceIndex, byte[] dest, int destIndex, int length)    {
      if((sourceIndex|destIndex|length) < 0) throw new ArgumentOutOfRangeException();
			if(length <= 10)
			{
				for(int i=0; i<length; i++) dest[destIndex+i] = source[sourceIndex+i];
			}
			else
			{
					      if(source == null || dest == null) throw new ArgumentNullException();
				if(sourceIndex+length > source.Length || destIndex+length > dest.Length) throw new ArgumentOutOfRangeException();
				fixed(byte* psrc=source, pdest=dest)
				{
					Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(byte));
				}
							}
    }
      /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
        public static void FastCopy(this char[] source, char[] dest, int length)    {
      FastCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
        public static unsafe void FastCopy(this char[] source, int sourceIndex, char[] dest, int destIndex, int length)    {
      if((sourceIndex|destIndex|length) < 0) throw new ArgumentOutOfRangeException();
			if(length <= 15)
			{
				for(int i=0; i<length; i++) dest[destIndex+i] = source[sourceIndex+i];
			}
			else
			{
					      if(source == null || dest == null) throw new ArgumentNullException();
				if(sourceIndex+length > source.Length || destIndex+length > dest.Length) throw new ArgumentOutOfRangeException();
				fixed(char* psrc=source, pdest=dest)
				{
					Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(char));
				}
							}
    }
      /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
        public static void FastCopy(this double[] source, double[] dest, int length)    {
      FastCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
        public static unsafe void FastCopy(this double[] source, int sourceIndex, double[] dest, int destIndex, int length)    {
      if((sourceIndex|destIndex|length) < 0) throw new ArgumentOutOfRangeException();
			if(length <= 20)
			{
				for(int i=0; i<length; i++) dest[destIndex+i] = source[sourceIndex+i];
			}
			else
			{
					      if(source == null || dest == null) throw new ArgumentNullException();
				if(sourceIndex+length > source.Length || destIndex+length > dest.Length) throw new ArgumentOutOfRangeException();
				fixed(double* psrc=source, pdest=dest)
				{
					Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(double));
				}
							}
    }
      /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
        public static void FastCopy(this float[] source, float[] dest, int length)    {
      FastCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
        public static unsafe void FastCopy(this float[] source, int sourceIndex, float[] dest, int destIndex, int length)    {
      if((sourceIndex|destIndex|length) < 0) throw new ArgumentOutOfRangeException();
			if(length <= 20)
			{
				for(int i=0; i<length; i++) dest[destIndex+i] = source[sourceIndex+i];
			}
			else
			{
					      if(source == null || dest == null) throw new ArgumentNullException();
				if(sourceIndex+length > source.Length || destIndex+length > dest.Length) throw new ArgumentOutOfRangeException();
				fixed(float* psrc=source, pdest=dest)
				{
					Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(float));
				}
							}
    }
      /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
        public static void FastCopy(this int[] source, int[] dest, int length)    {
      FastCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
        public static unsafe void FastCopy(this int[] source, int sourceIndex, int[] dest, int destIndex, int length)    {
      if((sourceIndex|destIndex|length) < 0) throw new ArgumentOutOfRangeException();
			if(length <= 15)
			{
				for(int i=0; i<length; i++) dest[destIndex+i] = source[sourceIndex+i];
			}
			else
			{
					      if(source == null || dest == null) throw new ArgumentNullException();
				if(sourceIndex+length > source.Length || destIndex+length > dest.Length) throw new ArgumentOutOfRangeException();
				fixed(int* psrc=source, pdest=dest)
				{
					Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(int));
				}
							}
    }
      /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
        public static void FastCopy(this long[] source, long[] dest, int length)    {
      FastCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
        public static unsafe void FastCopy(this long[] source, int sourceIndex, long[] dest, int destIndex, int length)    {
      if((sourceIndex|destIndex|length) < 0) throw new ArgumentOutOfRangeException();
			if(length <= 20)
			{
				for(int i=0; i<length; i++) dest[destIndex+i] = source[sourceIndex+i];
			}
			else
			{
					      if(source == null || dest == null) throw new ArgumentNullException();
				if(sourceIndex+length > source.Length || destIndex+length > dest.Length) throw new ArgumentOutOfRangeException();
				fixed(long* psrc=source, pdest=dest)
				{
					Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(long));
				}
							}
    }
      /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
          [CLSCompliant(false)]
        public static void FastCopy(this sbyte[] source, sbyte[] dest, int length)    {
      FastCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
          [CLSCompliant(false)]
        public static unsafe void FastCopy(this sbyte[] source, int sourceIndex, sbyte[] dest, int destIndex, int length)    {
      if((sourceIndex|destIndex|length) < 0) throw new ArgumentOutOfRangeException();
			if(length <= 10)
			{
				for(int i=0; i<length; i++) dest[destIndex+i] = source[sourceIndex+i];
			}
			else
			{
					      if(source == null || dest == null) throw new ArgumentNullException();
				if(sourceIndex+length > source.Length || destIndex+length > dest.Length) throw new ArgumentOutOfRangeException();
				fixed(sbyte* psrc=source, pdest=dest)
				{
					Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(sbyte));
				}
							}
    }
      /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
        public static void FastCopy(this short[] source, short[] dest, int length)    {
      FastCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
        public static unsafe void FastCopy(this short[] source, int sourceIndex, short[] dest, int destIndex, int length)    {
      if((sourceIndex|destIndex|length) < 0) throw new ArgumentOutOfRangeException();
			if(length <= 14)
			{
				for(int i=0; i<length; i++) dest[destIndex+i] = source[sourceIndex+i];
			}
			else
			{
					      if(source == null || dest == null) throw new ArgumentNullException();
				if(sourceIndex+length > source.Length || destIndex+length > dest.Length) throw new ArgumentOutOfRangeException();
				fixed(short* psrc=source, pdest=dest)
				{
					Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(short));
				}
							}
    }
      /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
          [CLSCompliant(false)]
        public static void FastCopy(this uint[] source, uint[] dest, int length)    {
      FastCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
          [CLSCompliant(false)]
        public static unsafe void FastCopy(this uint[] source, int sourceIndex, uint[] dest, int destIndex, int length)    {
      if((sourceIndex|destIndex|length) < 0) throw new ArgumentOutOfRangeException();
			if(length <= 17)
			{
				for(int i=0; i<length; i++) dest[destIndex+i] = source[sourceIndex+i];
			}
			else
			{
					      if(source == null || dest == null) throw new ArgumentNullException();
				if(sourceIndex+length > source.Length || destIndex+length > dest.Length) throw new ArgumentOutOfRangeException();
				fixed(uint* psrc=source, pdest=dest)
				{
					Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(uint));
				}
							}
    }
      /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
          [CLSCompliant(false)]
        public static void FastCopy(this ulong[] source, ulong[] dest, int length)    {
      FastCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
          [CLSCompliant(false)]
        public static unsafe void FastCopy(this ulong[] source, int sourceIndex, ulong[] dest, int destIndex, int length)    {
      if((sourceIndex|destIndex|length) < 0) throw new ArgumentOutOfRangeException();
			if(length <= 22)
			{
				for(int i=0; i<length; i++) dest[destIndex+i] = source[sourceIndex+i];
			}
			else
			{
					      if(source == null || dest == null) throw new ArgumentNullException();
				if(sourceIndex+length > source.Length || destIndex+length > dest.Length) throw new ArgumentOutOfRangeException();
				fixed(ulong* psrc=source, pdest=dest)
				{
					Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(ulong));
				}
							}
    }
      /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
          [CLSCompliant(false)]
        public static void FastCopy(this ushort[] source, ushort[] dest, int length)    {
      FastCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
          [CLSCompliant(false)]
        public static unsafe void FastCopy(this ushort[] source, int sourceIndex, ushort[] dest, int destIndex, int length)    {
      if((sourceIndex|destIndex|length) < 0) throw new ArgumentOutOfRangeException();
			if(length <= 15)
			{
				for(int i=0; i<length; i++) dest[destIndex+i] = source[sourceIndex+i];
			}
			else
			{
					      if(source == null || dest == null) throw new ArgumentNullException();
				if(sourceIndex+length > source.Length || destIndex+length > dest.Length) throw new ArgumentOutOfRangeException();
				fixed(ushort* psrc=source, pdest=dest)
				{
					Unsafe.Copy(psrc+sourceIndex, pdest+destIndex, length*sizeof(ushort));
				}
							}
    }
      /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
        public static void FastCopy<T>(this T[] source, T[] dest, int length) where T : class    {
      FastCopy(source, 0, dest, 0, length);
    }

    /// <include file="documentation.xml" path="/Utilities/ArrayUtility/FastCopy/*"/>
        public static unsafe void FastCopy<T>(this T[] source, int sourceIndex, T[] dest, int destIndex, int length) where T : class    {
      if((sourceIndex|destIndex|length) < 0) throw new ArgumentOutOfRangeException();
			if(length <= 30)
			{
				for(int i=0; i<length; i++) dest[destIndex+i] = source[sourceIndex+i];
			}
			else
			{
								Array.Copy(source, sourceIndex, dest, destIndex, length);
							}
    }
  }

} // namespace AdamMil.Utilities