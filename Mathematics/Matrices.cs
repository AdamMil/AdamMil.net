/*
AdamMil.Mathematics is a library that provides some useful mathematics classes
for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2013 Adam Milazzo

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
using System.Collections.Generic;
using System.Text;
using AdamMil.Mathematics.Geometry;
using AdamMil.Mathematics.LinearAlgebra;
using AdamMil.Utilities;

#warning Document matrices!

namespace AdamMil.Mathematics
{
  #region Matrix3
  #pragma warning disable 1591
  [Serializable]
  public sealed class Matrix3 : ICloneable, IEquatable<Matrix3>
  {
    public Matrix3() { }

    public unsafe Matrix3(double[] data)
    {
      if(data == null) throw new ArgumentNullException();
      if(data.Length != Length) throw new ArgumentException("Expected an array of 9 elements.");
      fixed(double* src=data, dest=&M00)
      {
        Unsafe.Copy(src, dest, Length*sizeof(double));
      }
    }

    public unsafe Matrix3(Matrix3 matrix)
    {
      if(matrix == null) throw new ArgumentNullException();
      fixed(double* src=&matrix.M00, dest=&M00)
      {
        Unsafe.Copy(src, dest, Length*sizeof(double));
      }
    }

    public unsafe Matrix3(Matrix matrix)
    {
      if(matrix == null) throw new ArgumentNullException();
      if(matrix.Width != Width || matrix.Height != Height) throw new ArgumentException("The matrix is the wrong size.");
      fixed(double* src=matrix.Array, dest=&M00)
      {
        Unsafe.Copy(src, dest, Length*sizeof(double));
      }
    }

    public const int Width=3, Height=3, Length=Width*Height;

    public unsafe double this[int index]
    {
      get
      {
        if((uint)index >= (uint)Length) throw new ArgumentOutOfRangeException();
        fixed(double* data=&M00) return data[index];
      }
      set
      {
        if((uint)index >= (uint)Length) throw new ArgumentOutOfRangeException();
        fixed(double* data=&M00) data[index]=value;
      }
    }

    public unsafe double this[int row, int column]
    {
      get
      {
        if((uint)row >= (uint)Height || (uint)column >= (uint)Width) throw new ArgumentOutOfRangeException();
        fixed(double* data=&M00) return data[row*Height+column];
      }
      set
      {
        if((uint)row >= (uint)Height || (uint)column >= (uint)Width) throw new ArgumentOutOfRangeException();
        fixed(double* data=&M00) data[row*Height+column]=value;
      }
    }

    public Matrix3 Clone()
    {
      return new Matrix3(this);
    }

    public override bool Equals(object obj)
    {
      return Equals(this, obj as Matrix);
    }

    public bool Equals(Matrix3 other)
    {
      return Equals(this, other);
    }

    public bool Equals(Matrix3 other, double tolerance)
    {
      return Equals(this, other, tolerance);
    }

    public double GetDeterminant()
    {
      return M00*M11*M22 - M00*M12*M21 + M01*M12*M20 - M01*M10*M22 + M02*M10*M21 - M02*M11*M20;
    }

    /// <include file="documentation.xml" path="//Common/GetHashCode/node()"/>
    public unsafe override int GetHashCode()
    {
      int hash = 0;
      fixed(double* dp=&M00)
      {
        for(int i=0; i<Length; i++)
        {
          // +0 and -0 compare equally, so they mustn't lead to different hash codes
          if(dp[i] != 0) hash ^= *(int*)&dp[i] ^ *((int*)&dp[i]+1) ^ (1<<i);
        }
      }
      return hash;
    }

    public void Invert()
    {
      double X = M11*M22 - M12*M21, Y = M12*M20 - M10*M22, Z = M10*M21 - M11*M20;
      double invDeterminant = 1 / (M00*X + M01*Y + M02*Z);

      double m11 = M00*M22 - M02*M20, m22 = M00*M11 - M01*M10, m12 = M02*M10 - M00*M12, m21 = M01*M20 - M00*M21;
      M00 = X * invDeterminant;
      M10 = Y * invDeterminant;
      M20 = Z * invDeterminant;

      X = M01*M12 - M02*M11;
      M01 = (M02*M21 - M01*M22) * invDeterminant;
      M02 = X * invDeterminant;
      M11 = m11 * invDeterminant;
      M12 = m12 * invDeterminant;
      M21 = m21 * invDeterminant;
      M22 = m22 * invDeterminant;
    }

    public void Scale(double x, double y, double z) { M00 *= x; M11 *= y; M22 *= z; }

    public unsafe double[] ToArray()
    {
      double[] ret = new double[Length];
      fixed(double* src=&M00, dest=ret)
      {
        for(int i=0; i<Length; i++) dest[i]=src[i];
      }
      return ret;
    }

    public unsafe Matrix ToMatrix()
    {
      fixed(double* data=&M00) return new Matrix(data, Height, Width);
    }

    public Vector3 Transform(Vector3 v)
    {
      return new Vector3(M00*v.X + M01*v.Y + M02*v.Z,
                        M10*v.X + M11*v.Y + M12*v.Z,
                        M20*v.X + M21*v.Y + M22*v.Z);
    }

    public void Transform(IList<Vector3> vectors)
    {
      for(int i=0; i<vectors.Count; i++) vectors[i] = Transform(vectors[i]);
    }

    public void Transpose()
    {
      Utility.Swap(ref M01, ref M10);
      Utility.Swap(ref M02, ref M20);
      Utility.Swap(ref M12, ref M21);
    }

    public static unsafe void Add(Matrix3 a, Matrix3 b, Matrix3 dest)
    {
      if(a == null || b == null || dest == null) throw new ArgumentNullException();
      fixed(double* ap=&a.M00, bp=&b.M00, dp=&dest.M00)
      {
        for(int i=0; i<Length; i++) dp[i] = ap[i]+bp[i];
      }
    }

    public static unsafe void Subtract(Matrix3 a, Matrix3 b, Matrix3 dest)
    {
      if(a == null || b == null || dest == null) throw new ArgumentNullException();
      fixed(double* ap=&a.M00, bp=&b.M00, dp=&dest.M00)
      {
        for(int i=0; i<Length; i++) dp[i] = ap[i]-bp[i];
      }
    }

    public static unsafe void Multiply(Matrix3 a, Matrix3 b, Matrix3 dest)
    {
      if(a == null || b == null || dest == null) throw new ArgumentNullException();

      fixed(double* ap=&a.M00, bp=&b.M00, dp=&dest.M00)
      {
        dp[0] = ap[0]*bp[0] + ap[1]*bp[3] + ap[2]*bp[6];
        dp[1] = ap[0]*bp[1] + ap[1]*bp[4] + ap[2]*bp[7];
        dp[2] = ap[0]*bp[2] + ap[1]*bp[5] + ap[2]*bp[8];
        dp[3] = ap[3]*bp[0] + ap[4]*bp[3] + ap[5]*bp[6];
        dp[4] = ap[3]*bp[1] + ap[4]*bp[4] + ap[5]*bp[7];
        dp[5] = ap[3]*bp[2] + ap[4]*bp[5] + ap[5]*bp[8];
        dp[6] = ap[6]*bp[0] + ap[7]*bp[3] + ap[8]*bp[6];
        dp[7] = ap[6]*bp[1] + ap[7]*bp[4] + ap[8]*bp[7];
        dp[8] = ap[6]*bp[2] + ap[7]*bp[5] + ap[8]*bp[8];
      }
    }

    public static unsafe bool Equals(Matrix3 a, Matrix3 b)
    {
      if(a == null) return b == null;
      else if(b == null) return false;

      fixed(double* ap=&a.M00, bp=&b.M00)
      {
        for(int i=0; i<Length; i++)
        {
          if(ap[i]!=bp[i]) return false;
        }
      }
      return true;
    }

    public static unsafe bool Equals(Matrix3 a, Matrix3 b, double tolerance)
    {
      if(a == null) return b == null;
      else if(b == null) return false;

      fixed(double* ap=&a.M00, bp=&b.M00)
      {
        for(int i=0; i<Length; i++)
        {
          if(Math.Abs(ap[i]-bp[i]) > tolerance) return false;
        }
      }
      return true;
    }

    public static Matrix3 Identity()
    {
      Matrix3 identity = new Matrix3();
      identity.M00 = 1;
      identity.M11 = 1;
      identity.M22 = 1;
      return identity;
    }

    public static Matrix3 Invert(Matrix3 matrix)
    {
      if(matrix == null) throw new ArgumentNullException();
      matrix = matrix.Clone();
      matrix.Invert();
      return matrix;
    }

    public static Matrix3 Rotation(double x, double y, double z)
    {
      double a=Math.Cos(x), b=Math.Sin(x), c=Math.Cos(y), d=Math.Sin(y), e=Math.Cos(z), f=Math.Sin(z), ad=a*d, bd=b*d;
      Matrix3 ret = new Matrix3();
      ret.M00 = c*e;
      ret.M01 = -(c*f);
      ret.M02 = d;
      ret.M10 = bd*e+a*f;
      ret.M11 = -(bd*f)+a*e;
      ret.M12 = -(b*c);
      ret.M20 = -(ad*e)+b*f;
      ret.M21 = ad*f+b*e;
      ret.M22 = a*c;
      return ret;
    }

    public static Matrix3 Rotation(double angle, Vector3 axis)
    {
      double cos=Math.Cos(angle), sin=Math.Sin(angle);
      Vector3 axisc1m = axis * (1-cos);
      Matrix3 ret = new Matrix3();
      ret.M00 =          cos  + axis.X*axisc1m.X;
      ret.M10 =   axis.Z*sin  + axis.Y*axisc1m.X;
      ret.M20 = -(axis.Y*sin) + axis.Z*axisc1m.X;
      ret.M01 = -(axis.Z*sin) + axis.X*axisc1m.Y;
      ret.M11 =          cos  + axis.Y*axisc1m.Y;
      ret.M21 =   axis.X*sin  + axis.Z*axisc1m.Y;
      ret.M02 =   axis.Y*sin  + axis.X*axisc1m.Z;
      ret.M12 = -(axis.X*sin) + axis.Y*axisc1m.Z;
      ret.M22 =          cos  + axis.Z*axisc1m.Z;
      return ret;
    }

    public static Matrix3 Rotation(Vector3 start, Vector3 end)
    {
      Vector3 cross = start.CrossProduct(end);
      // if the vectors are colinear, rotate one by 90 degrees and use that.
      if(cross.X==0 && cross.Y==0 && cross.Z==0)
      {
        return start.Equals(end, 0.001) ? Identity() : Rotation(Math.PI, new Vector3(-start.Y, start.X, start.Z));
      }
      else
      {
        return Rotation(Math.Acos(start.DotProduct(end)), cross);
      }
    }

    public static Matrix3 RotationX(double angle)
    {
      double sin=Math.Sin(angle), cos=Math.Cos(angle);
      Matrix3 ret = new Matrix3();
      ret.M00 = 1;
      ret.M11 = cos;
      ret.M12 = -sin;
      ret.M21 = sin;
      ret.M22 = cos;
      return ret;
    }

    public static Matrix3 RotationY(double angle)
    {
      double sin=Math.Sin(angle), cos=Math.Cos(angle);
      Matrix3 ret = new Matrix3();
      ret.M00 = cos;
      ret.M02 = sin;
      ret.M11 = 1;
      ret.M20 = -sin;
      ret.M22 = cos;
      return ret;
    }

    public static Matrix3 RotationZ(double angle)
    {
      double sin=Math.Sin(angle), cos=Math.Cos(angle);
      Matrix3 ret = new Matrix3();
      ret.M00 = cos;
      ret.M01 = -sin;
      ret.M10 = sin;
      ret.M11 = cos;
      ret.M22 = 1;
      return ret;
    }

    public static Matrix3 Scaling(double x, double y, double z)
    {
      Matrix3 ret = new Matrix3();
      ret.M00 = x;
      ret.M11 = y;
      ret.M22 = z;
      return ret;
    }

    public static Matrix3 Shearing(double xy, double xz, double yx, double yz, double zx, double zy)
    {
      Matrix3 ret = new Matrix3();
      ret.M00 = 1;
      ret.M01 = yx;
      ret.M02 = zx;
      ret.M10 = xy;
      ret.M11 = 1;
      ret.M12 = zy;
      ret.M20 = xz;
      ret.M21 = yz;
      ret.M22 = 1;
      return ret;
    }

    public static Matrix3 Transpose(Matrix3 matrix)
    {
      if(matrix == null) throw new ArgumentNullException();
      matrix = matrix.Clone();
      matrix.Transpose();
      return matrix;
    }

    public static Matrix3 operator+(Matrix3 a, Matrix3 b)
    {
      Matrix3 ret = new Matrix3();
      Add(a, b, ret);
      return ret;
    }

    public static Matrix3 operator-(Matrix3 a, Matrix3 b)
    {
      Matrix3 ret = new Matrix3();
      Subtract(a, b, ret);
      return ret;
    }

    public static Matrix3 operator*(Matrix3 a, Matrix3 b)
    {
      Matrix3 ret = new Matrix3();
      Multiply(a, b, ret);
      return ret;
    }

    public static Vector3 operator*(Matrix3 a, Vector3 b)
    {
      return a.Transform(b);
    }

    #pragma warning disable 1591
    public double M00, M01, M02,
                  M10, M11, M12,
                  M20, M21, M22;
    #pragma warning restore 1591

    #region ICloneable Members
    object ICloneable.Clone()
    {
      return Clone();
    }
    #endregion
  }
  #endregion

  #region Matrix4
  #pragma warning disable 1591
  [Serializable]
  public sealed class Matrix4 : ICloneable, IEquatable<Matrix4>
  {
    public Matrix4() { }

    public unsafe Matrix4(double[] data)
    {
      if(data == null) throw new ArgumentNullException();
      if(data.Length != Length) throw new ArgumentException("Expected an array of 16 elements.");
      fixed(double* src=data, dest=&M00) Unsafe.Copy(src, dest, Length*sizeof(double));
    }

    public unsafe Matrix4(Matrix4 matrix)
    {
      if(matrix == null) throw new ArgumentNullException();
      fixed(double* src=&matrix.M00, dest=&M00) Unsafe.Copy(src, dest, Length*sizeof(double));
    }

    public unsafe Matrix4(Matrix matrix)
    {
      if(matrix == null) throw new ArgumentNullException();
      if(matrix.Width != Width || matrix.Height != Height) throw new ArgumentException("The matrix is the wrong size.");
      fixed(double* src=matrix.Array, dest=&M00) Unsafe.Copy(src, dest, Length*sizeof(double));
    }

    public const int Width=4, Height=4, Length=Width*Height;

    public unsafe double this[int index]
    {
      get
      {
        if((uint)index >= (uint)Length) throw new ArgumentOutOfRangeException();
        fixed(double* data=&M00) return data[index];
      }
      set
      {
        if((uint)index >= (uint)Length) throw new ArgumentOutOfRangeException();
        fixed(double* data=&M00) data[index]=value;
      }
    }

    public unsafe double this[int row, int column]
    {
      get
      {
        if((uint)row >= (uint)Height || (uint)column >= (uint)Width) throw new ArgumentOutOfRangeException();
        fixed(double* data=&M00) return data[row*Height+column];
      }
      set
      {
        if((uint)row >= (uint)Height || (uint)column >= (uint)Width) throw new ArgumentOutOfRangeException();
        fixed(double* data=&M00) data[row*Height+column]=value;
      }
    }

    public Matrix4 Clone()
    {
      return new Matrix4(this);
    }

    public override bool Equals(object obj)
    {
      return Equals(this, obj as Matrix4);
    }

    public bool Equals(Matrix4 other)
    {
      return Equals(this, other);
    }

    public bool Equals(Matrix4 other, double tolerance)
    {
      return Equals(this, other, tolerance);
    }

    public double GetDeterminant()
    {
      double A = M22*M33 - M23*M32, B = M21*M33 - M23*M31, C = M21*M32 - M22*M31;
      double D = M20*M33 - M23*M30, E = M20*M32 - M22*M30, F = M20*M31 - M21*M30;
      return M00*(M11*A - M12*B + M13*C) - M01*(M10*A - M12*D + M13*E) + M02*(M10*B - M11*D + M13*F) - M03*(M10*C - M11*E + M12*F);
    }

    /// <include file="documentation.xml" path="//Common/GetHashCode/node()"/>
    public unsafe override int GetHashCode()
    {
      int hash = 0;
      fixed(double* dp=&M00)
      {
        for(int i=0; i<Length; i++)
        {
          // +0 and -0 compare equally, so they mustn't lead to different hash codes
          if(dp[i] != 0) hash ^= *(int*)&dp[i] ^ *((int*)&dp[i]+1) ^ (1<<i);
        }
      }
      return hash;
    }

    public void Invert()
    {
      double A = M22*M33 - M23*M32, B = M21*M33 - M23*M31, C = M21*M32 - M22*M31;
      double D = M20*M33 - M23*M30, E = M20*M32 - M22*M30, F = M20*M31 - M21*M30;
      double W = M11*A - M12*B + M13*C, X = M12*D - M10*A - M13*E, Y = M10*B - M11*D + M13*F, Z = M11*E - M10*C - M12*F;

      double m01 = M02*B - M01*A - M03*C, m11 = M00*A - M02*D + M03*E, m21 = M01*D - M00*B - M03*F, m31 = M00*C - M01*E + M02*F;
      A = (M12*M33 - M13*M32);
      B = (M11*M33 - M13*M31);
      C = (M11*M32 - M12*M31);
      D = (M10*M33 - M13*M30);
      E = (M10*M32 - M12*M30);
      F = (M10*M31 - M11*M30);

      double m02 = M01*A - M02*B + M03*C, m12 = M02*D - M00*A - M03*E, m22 = M00*B - M01*D + M03*F, m32 = M01*E - M00*C - M02*F;
      A = (M12*M23 - M13*M22);
      B = (M11*M23 - M13*M21);
      C = (M11*M22 - M12*M21);
      D = (M10*M23 - M13*M20);
      E = (M10*M22 - M12*M20);
      F = (M10*M21 - M11*M20);

      double invDeterminant = 1 / (M00*W + M01*X + M02*Y + M03*Z);
      M10 = X * invDeterminant;
      M20 = Y * invDeterminant;
      M30 = Z * invDeterminant;
      M11 = m11 * invDeterminant;
      M21 = m21 * invDeterminant;
      M31 = m31 * invDeterminant;
      M12 = m12 * invDeterminant;
      M22 = m22 * invDeterminant;
      M32 = m32 * invDeterminant;

      M13 = (M00*A - M02*D + M03*E) * invDeterminant;
      M23 = (M01*D - M00*B - M03*F) * invDeterminant;
      M33 = (M00*C - M01*E + M02*F) * invDeterminant;
      M03 = (M02*B - M01*A - M03*C) * invDeterminant;

      M00 = W * invDeterminant;
      M01 = m01 * invDeterminant;
      M02 = m02 * invDeterminant;
    }

    public void Scale(double x, double y, double z)
    {
      M00 *= x;
      M11 *= y;
      M22 *= z;
    }

    public unsafe Matrix ToMatrix()
    {
      fixed(double* data=&M00) return new Matrix(data, Height, Width);
    }

    public Vector3 Transform(Vector3 v)
    {
      return new Vector3(M00*v.X + M01*v.Y + M02*v.Z + M03,
                        M10*v.X + M11*v.Y + M12*v.Z + M13,
                        M20*v.X + M21*v.Y + M22*v.Z + M23);
    }

    public void Transform(IList<Vector3> vectors)
    {
      for(int i=0; i<vectors.Count; i++) vectors[i] = Transform(vectors[i]);
    }

    public void Translate(double x, double y, double z)
    {
      M03 += x;
      M13 += y;
      M23 += z;
    }

    public void Transpose()
    {
      Utility.Swap(ref M01, ref M10);
      Utility.Swap(ref M02, ref M20);
      Utility.Swap(ref M03, ref M30);
      Utility.Swap(ref M12, ref M21);
      Utility.Swap(ref M13, ref M31);
      Utility.Swap(ref M23, ref M32);
    }

    public unsafe double[] ToArray()
    {
      double[] ret = new double[Length];
      fixed(double* src=&M00, dest=ret) Unsafe.Copy(src, dest, Length*sizeof(double));
      return ret;
    }

    public static unsafe void Add(Matrix4 a, Matrix4 b, Matrix4 dest)
    {
      if(a == null || b == null || dest == null) throw new ArgumentNullException();

      fixed(double* ap=&a.M00, bp=&b.M00, dp=&dest.M00)
      {
        for(int i=0; i<Length; i++) dp[i] = ap[i]+bp[i];
      }
    }

    public static unsafe void Subtract(Matrix4 a, Matrix4 b, Matrix4 dest)
    {
      if(a == null || b == null || dest == null) throw new ArgumentNullException();

      fixed(double* ap=&a.M00, bp=&b.M00, dp=&dest.M00)
      {
        for(int i=0; i<Length; i++) dp[i] = ap[i]-bp[i];
      }
    }

    public static unsafe void Multiply(Matrix4 a, Matrix4 b, Matrix4 dest)
    {
      if(a == null || b == null || dest == null) throw new ArgumentNullException();

      fixed(double* ap=&a.M00, bp=&b.M00, dp=&dest.M00)
      {
        dp[0]  = ap[0]*bp[0]  + ap[1]*bp[4]  + ap[2]*bp[8]   + ap[3]*bp[12];
        dp[1]  = ap[0]*bp[1]  + ap[1]*bp[5]  + ap[2]*bp[9]   + ap[3]*bp[13];
        dp[2]  = ap[0]*bp[2]  + ap[1]*bp[6]  + ap[2]*bp[10]  + ap[3]*bp[14];
        dp[3]  = ap[0]*bp[3]  + ap[1]*bp[7]  + ap[2]*bp[11]  + ap[3]*bp[15];
        dp[4]  = ap[4]*bp[0]  + ap[5]*bp[4]  + ap[6]*bp[8]   + ap[7]*bp[12];
        dp[5]  = ap[4]*bp[1]  + ap[5]*bp[5]  + ap[6]*bp[9]   + ap[7]*bp[13];
        dp[6]  = ap[4]*bp[2]  + ap[5]*bp[6]  + ap[6]*bp[10]  + ap[7]*bp[14];
        dp[7]  = ap[4]*bp[3]  + ap[5]*bp[7]  + ap[6]*bp[11]  + ap[7]*bp[15];
        dp[8]  = ap[8]*bp[0]  + ap[9]*bp[4]  + ap[10]*bp[8]  + ap[11]*bp[12];
        dp[9]  = ap[8]*bp[1]  + ap[9]*bp[5]  + ap[10]*bp[9]  + ap[11]*bp[13];
        dp[10] = ap[8]*bp[2]  + ap[9]*bp[6]  + ap[10]*bp[10] + ap[11]*bp[14];
        dp[11] = ap[8]*bp[3]  + ap[9]*bp[7]  + ap[10]*bp[11] + ap[11]*bp[15];
        dp[12] = ap[12]*bp[0] + ap[13]*bp[4] + ap[14]*bp[8]  + ap[15]*bp[12];
        dp[13] = ap[12]*bp[1] + ap[13]*bp[5] + ap[14]*bp[9]  + ap[15]*bp[13];
        dp[14] = ap[12]*bp[2] + ap[13]*bp[6] + ap[14]*bp[10] + ap[15]*bp[14];
        dp[15] = ap[12]*bp[3] + ap[13]*bp[7] + ap[14]*bp[11] + ap[15]*bp[15];
      }
    }

    public static unsafe bool Equals(Matrix4 a, Matrix4 b)
    {
      if(a == null) return b == null;
      else if(b == null) return false;

      fixed(double* ap=&a.M00, bp=&b.M00)
      {
        for(int i=0; i<Length; i++)
        {
          if(ap[i] != bp[i]) return false;
        }
      }
      return true;
    }

    public static unsafe bool Equals(Matrix4 a, Matrix4 b, double tolerance)
    {
      if(a == null) return b == null;
      else if(b == null) return false;

      fixed(double* ap=&a.M00, bp=&b.M00)
      {
        for(int i=0; i<Length; i++)
        {
          if(Math.Abs(ap[i]-bp[i]) > tolerance) return false;
        }
      }
      return true;
    }

    public static Matrix4 Identity()
    {
      Matrix4 matrix = new Matrix4();
      matrix.M00 = 1;
      matrix.M11 = 1;
      matrix.M22 = 1;
      matrix.M33 = 1;
      return matrix;
    }

    public static Matrix4 Invert(Matrix4 matrix)
    {
      if(matrix == null) throw new ArgumentNullException();
      matrix = matrix.Clone();
      matrix.Invert();
      return matrix;
    }

    public static Matrix4 Rotation(double x, double y, double z)
    {
      double a=Math.Cos(x), b=Math.Sin(x), c=Math.Cos(y), d=Math.Sin(y), e=Math.Cos(z), f=Math.Sin(z), ad=a*d, bd=b*d;
      Matrix4 ret = new Matrix4();
      ret.M00 = c*e;
      ret.M01 = -(c*f);
      ret.M02 = d;
      ret.M10 = bd*e+a*f;
      ret.M11 = -(bd*f)+a*e;
      ret.M12 = -(b*c);
      ret.M20 = -(ad*e)+b*f;
      ret.M21 = ad*f+b*e;
      ret.M22 = a*c;
      ret.M33 = 1;
      return ret;
    }

    public static Matrix4 Rotation(double angle, Vector3 axis)
    {
      double cos=Math.Cos(angle), sin=Math.Sin(angle);
      Vector3 axisc1m = axis * (1-cos);
      Matrix4 ret = new Matrix4();
      ret.M00 =          cos  + axis.X*axisc1m.X;
      ret.M10 =   axis.Z*sin  + axis.Y*axisc1m.X;
      ret.M20 = -(axis.Y*sin) + axis.Z*axisc1m.X;
      ret.M01 = -(axis.Z*sin) + axis.X*axisc1m.Y;
      ret.M11 =          cos  + axis.Y*axisc1m.Y;
      ret.M21 =   axis.X*sin  + axis.Z*axisc1m.Y;
      ret.M02 =   axis.Y*sin  + axis.X*axisc1m.Z;
      ret.M12 = -(axis.X*sin) + axis.Y*axisc1m.Z;
      ret.M22 =          cos  + axis.Z*axisc1m.Z;
      ret.M33 = 1;
      return ret;
    }

    public static Matrix4 Rotation(Vector3 start, Vector3 end)
    {
      Vector3 cross = start.CrossProduct(end);
      // if the vectors are colinear, rotate one by 90 degrees and use that.
      if(cross.X==0 && cross.Y==0 && cross.Z==0)
      {
        return start.Equals(end, 0.001) ? Identity() : Rotation(Math.PI, new Vector3(-start.Y, start.X, start.Z));
      }
      else
      {
        return Rotation(Math.Acos(start.DotProduct(end)), cross);
      }
    }

    public static Matrix4 RotationX(double angle)
    {
      double sin=Math.Sin(angle), cos=Math.Cos(angle);
      Matrix4 ret = new Matrix4();
      ret.M00 = 1;
      ret.M11 = cos;
      ret.M12 = -sin;
      ret.M21 = sin;
      ret.M22 = cos;
      ret.M33 = 1;
      return ret;
    }

    public static Matrix4 RotationY(double angle)
    {
      double sin=Math.Sin(angle), cos=Math.Cos(angle);
      Matrix4 ret = new Matrix4();
      ret.M00 = cos;
      ret.M02 = sin;
      ret.M11 = 1;
      ret.M20 = -sin;
      ret.M22 = cos;
      ret.M33 = 1;
      return ret;
    }

    public static Matrix4 RotationZ(double angle)
    {
      double sin=Math.Sin(angle), cos=Math.Cos(angle);
      Matrix4 ret = new Matrix4();
      ret.M00 = cos;
      ret.M01 = -sin;
      ret.M10 = sin;
      ret.M11 = cos;
      ret.M22 = 1;
      ret.M33 = 1;
      return ret;
    }

    public static Matrix4 Scaling(double x, double y, double z)
    {
      Matrix4 ret = new Matrix4();
      ret.M00 = x;
      ret.M11 = y;
      ret.M22 = z;
      ret.M33 = 1;
      return ret;
    }

    public static Matrix4 Shearing(double xy, double xz, double yx, double yz, double zx, double zy)
    {
      Matrix4 ret = Identity();
      ret.M01 = yx;
      ret.M02 = zx;
      ret.M10 = xy;
      ret.M12 = zy;
      ret.M20 = xz;
      ret.M21 = yz;
      return ret;
    }

    public static Matrix4 Translation(double x, double y, double z)
    {
      Matrix4 ret = Identity();
      ret.M03 = x;
      ret.M13 = y;
      ret.M23 = z;
      return ret;
    }

    public static Matrix4 Transpose(Matrix4 matrix)
    {
      if(matrix == null) throw new ArgumentNullException();
      matrix = matrix.Clone();
      matrix.Transpose();
      return matrix;
    }

    public static Matrix4 operator+(Matrix4 a, Matrix4 b)
    {
      Matrix4 ret = new Matrix4();
      Add(a, b, ret);
      return ret;
    }

    public static Matrix4 operator-(Matrix4 a, Matrix4 b)
    {
      Matrix4 ret = new Matrix4();
      Subtract(a, b, ret);
      return ret;
    }

    public static Matrix4 operator*(Matrix4 a, Matrix4 b)
    {
      Matrix4 ret = new Matrix4();
      Multiply(a, b, ret);
      return ret;
    }

    public static Vector3 operator*(Matrix4 a, Vector3 b)
    {
      return a.Transform(b);
    }

    #pragma warning disable 1591
    public double M00, M01, M02, M03,
                  M10, M11, M12, M13,
                  M20, M21, M22, M23,
                  M30, M31, M32, M33;
    #pragma warning restore 1591

    #region ICloneable Members
    object ICloneable.Clone()
    {
      return Clone();
    }
    #endregion
  }
  #endregion

  #region Matrix
  #pragma warning disable 1591
  [Serializable]
  public sealed class Matrix : ICloneable, IEquatable<Matrix>
  {
    public Matrix(int height, int width)
    {
      Resize(height, width);
    }

    public Matrix(double[,] data)
    {
      if(data == null) throw new ArgumentNullException();
      Height = data.GetLength(0);
      Width  = data.GetLength(1);
      this.data = (double[,])data.Clone();
    }

    public unsafe Matrix(double[] data, int width)
    {
      if(data == null) throw new ArgumentNullException();
      if(width <= 0) throw new ArgumentOutOfRangeException();
      if(data.Length % width != 0) throw new ArgumentException("The data length is not a multiple of the width.");
      Width  = width;
      Height = data.Length / width;
      this.data = new double[Height, Width];

      fixed(double* psrc=data, pdest=this.data)
      {
        Unsafe.Copy(psrc, pdest, data.Length*sizeof(double));
      }
    }

    [CLSCompliant(false)]
    public unsafe Matrix(double* data, int height, int width)
    {
      if(width < 0 || height < 0) throw new ArgumentOutOfRangeException();
      Width     = width;
      Height    = height;
      this.data = new double[height, width];
      fixed(double* dest=this.data) Unsafe.Copy(data, dest, this.data.Length*sizeof(double));
    }

    public Matrix(Matrix matrix)
    {
      if(matrix == null) throw new ArgumentNullException();
      Width  = matrix.Width;
      Height = matrix.Height;
      data   = matrix.data.Length == 0 ? matrix.data : (double[,])matrix.data.Clone();
    }

    public double this[int row, int column]
    {
      get { return data[row, column]; }
      set { data[row, column] = value; }
    }

    public double[,] Array
    {
      get { return data; }
    }

    public bool IsSquare
    {
      get { return Width == Height; }
    }

    public int Width { get; private set; }
    public int Height { get; private set; }

    public unsafe void Add(Matrix matrix)
    {
      if(matrix == null) throw new ArgumentNullException();
      AssertSameSize(matrix);
      fixed(double* dest=data, src=matrix.data)
      {
        for(int i=0, length=data.Length; i<length; i++) dest[i] += src[i];
      }
    }

    public void Assign(Matrix matrix)
    {
      if(matrix == null) throw new ArgumentNullException();
      Resize(matrix.Height, matrix.Width);
      System.Array.Copy(matrix.data, this.data, Width*Height);
    }

    public Matrix Clone()
    {
      return new Matrix(this);
    }

    public override bool Equals(object obj)
    {
      return Equals(this, obj as Matrix);
    }

    public bool Equals(Matrix other)
    {
      return Equals(this, other);
    }

    public bool Equals(Matrix other, double tolerance)
    {
      return Equals(this, other, tolerance);
    }

    public unsafe void Divide(double dividend)
    {
      fixed(double* pdata=data)
      {
        for(int i=0, length=data.Length; i<length; i++) pdata[i] /= dividend;
      }
    }

    public Vector GetColumn(int column)
    {
      double[] colData = new double[Height];
      GetColumn(column, colData);
      return new Vector(colData, false);
    }

    public void GetColumn(int column, double[] array)
    {
      GetColumn(column, array, 0);
    }

    public unsafe void GetColumn(int column, double[] array, int index)
    {
      if((uint)column >= (uint)Width) throw new ArgumentOutOfRangeException();
      Utility.ValidateRange(array, index, Height);
      fixed(double* pdata=data)
      {
        for(int i=0; i<Height; column += Width, i++) array[index+i] = pdata[column];
      }
    }

    public double GetDeterminant()
    {
      AssertSquare();
      return new LUDecomposition(this).GetDeterminant();
    }

    public unsafe override int GetHashCode()
    {
      int hash = 0;
      fixed(double* dp=data)
      {
        for(int i=0,length=Width*Height; i<length; i++)
        {
          // +0 and -0 compare equally, so they mustn't lead to different hash codes
          if(dp[i] != 0) hash ^= *(int*)&dp[i] ^ *((int*)&dp[i]+1) ^ (1<<(i&31));
        }
      }
      return hash;
    }

    public double GetLogDeterminant(out bool negative)
    {
      AssertSquare();
      return new LUDecomposition(this).GetLogDeterminant(out negative);
    }

    public unsafe Vector GetRow(int row)
    {
      fixed(double* pdata=data) return new Vector(pdata+row*Width, Width);
    }

    public void GetRow(int row, double[] array)
    {
      GetRow(row, array, 0);
    }

    public unsafe void GetRow(int row, double[] array, int index)
    {
      if((uint)row >= (uint)Height) throw new ArgumentOutOfRangeException();
      Utility.ValidateRange(array, index, Width);
      fixed(double* psrc=data, pdest=array) Unsafe.Copy(psrc+row*Width, pdest+index, Width*sizeof(double));
    }

    public unsafe void Invert()
    {
      // do the inversion in a separate matrix to prevent this matrix from being clobbered if it turns out to not be invertible
      Matrix inverse = Invert(this);
      fixed(double* src=inverse.data, dest=data) Unsafe.Copy(src, dest, data.Length*sizeof(double));
    }

    public unsafe void Multiply(double factor)
    {
      fixed(double* pdata=data)
      {
        for(int i=0, length=data.Length; i<length; i++) pdata[i] *= factor;
      }
    }

    public unsafe void ScaleRow(int row, double factor)
    {
      if((uint)row >= (uint)Height) throw new ArgumentOutOfRangeException();
      fixed(double* pdata=data)
      {
        for(int i=row*Width, end=i+Width; i<end; i++) pdata[i] *= factor;
      }
    }

    public void Resize(int height, int width)
    {
      if(width != Width || height != Height)
      {
        if(width < 0 || height < 0) throw new ArgumentOutOfRangeException();
        Width  = width;
        Height = height;
        data   = new double[height, width];
      }
    }

    public void SetColumn(int column, Vector vector)
    {
      if(vector == null) throw new ArgumentNullException();
      SetColumn(column, vector.Array, 0);
    }

    public unsafe void SetColumn(int destColumn, Matrix srcMatrix, int srcColumn)
    {
      if(srcMatrix == null) throw new ArgumentNullException();
      if((uint)destColumn >= (uint)Width || (uint)srcColumn >= (uint)srcMatrix.Width) throw new ArgumentOutOfRangeException();
      if(Height != srcMatrix.Height) throw new ArgumentException("The matrixes must have the same height.");
      fixed(double* src=srcMatrix.data, dest=data)
      {
        for(int i=0; i<Height; srcColumn += srcMatrix.Width, destColumn += Width, i++) dest[destColumn] = src[srcColumn];
      }
    }

    public void SetColumn(int column, double[] array)
    {
      SetColumn(column, array, 0);
    }

    public unsafe void SetColumn(int column, double[] array, int index)
    {
      if((uint)column >= (uint)Width) throw new ArgumentOutOfRangeException();
      Utility.ValidateRange(array, index, Height);
      fixed(double* pdata=data)
      {
        for(int i=0; i<Height; column += Width, i++) pdata[column] = array[index+i];
      }
    }

    public void SetIdentity()
    {
      AssertSquare();
      System.Array.Clear(data, 0, data.Length);
      for(int i=0; i<Width; i++) data[i, i] = 1;
    }

    public void SetRow(int row, Vector vector)
    {
      if(vector == null) throw new ArgumentNullException();
      SetRow(row, vector.Array, 0);
    }

    public unsafe void SetRow(int destRow, Matrix srcMatrix, int srcRow)
    {
      if(srcMatrix == null) throw new ArgumentNullException();
      if((uint)srcRow >= (uint)srcMatrix.Height) throw new ArgumentOutOfRangeException();
      if(Width != srcMatrix.Width) throw new ArgumentException("The matrixes must have the same width.");
      fixed(double* psrc=srcMatrix.data) SetRow(destRow, psrc + srcRow*srcMatrix.Width);
    }

    public void SetRow(int row, double[] array)
    {
      SetRow(row, array, 0);
    }

    public unsafe void SetRow(int row, double[] array, int index)
    {
      Utility.ValidateRange(array, index, Width);
      fixed(double* psrc=array) SetRow(row, psrc+index);
    }

    [CLSCompliant(false)]
    public unsafe void SetRow(int row, double* data)
    {
      if((uint)row >= (uint)Height) throw new ArgumentOutOfRangeException();
      fixed(double* dest=this.data) Unsafe.Copy(data, dest + row*Width, Width*sizeof(double));
    }

    public unsafe void Subtract(Matrix matrix)
    {
      if(matrix == null) throw new ArgumentNullException();
      AssertSameSize(matrix);
      fixed(double* src=matrix.data, dest=data)
      {
        for(int i=0, length=data.Length; i<length; i++) dest[i] -= src[i];
      }
    }

    public void Swap(int row1, int column1, int row2, int column2)
    {
      Utility.Swap(ref data[row1, column1], ref data[row2, column2]);
    }

    public unsafe void SwapColumns(int column1, int column2)
    {
      if((uint)column1 >= (uint)Width || (uint)column2 >= (uint)Width) throw new ArgumentOutOfRangeException();
      fixed(double* pdata=data)
      {
        for(int i=0; i<Height; column1 += Width, column2 += Width, i++)
        {
          double t = pdata[column1];
          pdata[column1] = pdata[column2];
          pdata[column2] = t;
        }
      }
    }

    public unsafe void SwapRows(int row1, int row2)
    {
      if((uint)row1 >= (uint)Height || (uint)row2 >= (uint)Height) throw new ArgumentOutOfRangeException();

      fixed(double* pdata=data)
      {
        double* a = pdata + row1*Width, b = pdata + row2*Width;
        for(int x=0; x<Width; x++)
        {
          double t = a[x];
          a[x] = b[x];
          b[x] = t;
        }
      }
    }

    /// <summary>Converts the matrix to a string.</summary>
    public override string ToString()
    {
      // first compute all the column widths, so we can space the columns evenly
      int[] columnWidths = new int[Width];
      for(int x=0; x<columnWidths.Length; x++)
      {
        int maxWidth = 0;
        for(int y=0; y<Height; y++) maxWidth = Math.Max(maxWidth, data[y, x].ToString().Length);
        columnWidths[x] = maxWidth;
      }

      // then put it all together
      // TODO: align numbers at the decimal point
      StringBuilder sb = new StringBuilder();
      for(int y=0; y<Height; y++)
      {
        if(sb.Length != 0) sb.Append('\n');
        sb.Append("| ");
        for(int x=0; x<Width; x++) sb.Append(data[y, x].ToString().PadRight(columnWidths[x])).Append(' ');
        sb.Append('|');
      }

      return sb.ToString();
    }

    public unsafe void Transpose()
    {
      // NOTE: there are faster algorithms to transpose both square and non-square matrices that could be implemented
      if(Width == Height)
      {
        for(int y=0; y<Height-1; y++)
        {
          for(int x=y+1; x<Width; x++) Utility.Swap(ref data[y, x], ref data[x, y]);
        }
      }
      else
      {
        double[,] original = data;
        data = new double[Width, Height];
        for(int y=0; y<Height; y++)
        {
          for(int x=0; x<Width; x++) data[x, y] = original[y, x];
        }

        int temp = Width;
        Width  = Height;
        Height = Width;
      }
    }

    public static Matrix operator+(Matrix a, Matrix b)
    {
      return Add(a, b);
    }

    public static Matrix operator-(Matrix a, Matrix b)
    {
      return Subtract(a, b);
    }

    public static Matrix operator*(Matrix a, Matrix b)
    {
      return Multiply(a, b);
    }

    public static Matrix operator*(Matrix a, double b)
    {
      return Multiply(a, b);
    }

    public static Matrix operator*(double a, Matrix b)
    {
      return Multiply(b, a);
    }

    public static Matrix operator/(Matrix a, double b)
    {
      return Divide(a, b);
    }

    public static Matrix Add(Matrix a, Matrix b)
    {
      if(a == null) throw new ArgumentNullException();
      Matrix result = a.Clone();
      result.Add(b);
      return result;
    }

    public static void Assign(ref Matrix dest, Matrix source)
    {
      if(source == null) throw new ArgumentNullException();
      if(dest == null) dest = source.Clone();
      else dest.Assign(source);
    }

    public static unsafe Matrix Augment(Matrix a, Matrix b)
    {
      if(a == null || b == null) throw new ArgumentNullException();
      if(a.Height != b.Height) throw new ArgumentException("The matrices must have the same height.");
      Matrix result = new Matrix(a.Height, a.Width+b.Width);

      fixed(double* pdest=result.Array, pa=a.Array, pb=b.Array)
      {
        for(int dy=0, di=0, ai=0, bi=0; dy<result.Height; dy++)
        {
          Unsafe.Copy(pa+ai, pdest+di, a.Width*sizeof(double));
          di += a.Width;
          ai += a.Width;
          Unsafe.Copy(pb+bi, pdest+di, b.Width*sizeof(double));
          di += b.Width;
          bi += b.Width;
        }
      }
      return result;
    }

    public static Matrix CreateIdentity(int size)
    {
      Matrix matrix = new Matrix(size, size);
      for(int i=0; i<size; i++) matrix[i, i] = 1;
      return matrix;
    }

    public static Matrix Divide(Matrix a, double b)
    {
      if(a == null) throw new ArgumentNullException();
      a = a.Clone();
      a.Divide(b);
      return a;
    }

    public unsafe static bool Equals(Matrix a, Matrix b)
    {
      if(a == null) return b == null;
      else if(b == null || a.Width != b.Width || a.Height != b.Height) return false;

      fixed(double* pa=a.data, pb=b.data)
      {
        for(int i=0, length=a.data.Length; i<length; i++)
        {
          if(pa[i] != pb[i]) return false;
        }
      }
      return true;
    }

    public unsafe static bool Equals(Matrix a, Matrix b, double tolerance)
    {
      if(a == null) return b == null;
      else if(b == null || a.Width != b.Width || a.Height != b.Height) return false;

      fixed(double* pa=a.data, pb=b.data)
      {
        for(int i=0, length=a.data.Length; i<length; i++)
        {
          if(Math.Abs(pa[i] - pb[i]) > tolerance) return false;
        }
      }
      return true;
    }

    public static Matrix Invert(Matrix matrix)
    {
      return GaussJordan.Invert(matrix);
    }

    public static Matrix Multiply(Matrix a, double b)
    {
      if(a == null) throw new ArgumentNullException();
      a = a.Clone();
      a.Multiply(b);
      return a;
    }

    // TODO: add multiplication instance methods that reuse the matrix storage if it's already of the correct size, and use only O(N)
    // additional storage rather than O(MN)

    public static unsafe Matrix Multiply(Matrix a, Matrix b)
    {
      if(a == null || b == null) throw new ArgumentNullException();
      if(a.Width != b.Height) throw new ArgumentException("The width of the left matrix does not match the height of the right matrix.");
      // when multiplying A*B, an element of the result is sum of the products of pairs of elements from the same row in A and the same
      // column in B.
      Matrix result = new Matrix(a.Height, b.Width);
      fixed(double* plhs=a.data, prhs=b.data, pdest=result.data)
      {
        double* lhs=plhs, dest=pdest;
        for(int i=0; i<result.Height; lhs += a.Width, i++)
        {
          double* rhs=prhs;
          for(int j=0; j<result.Width; rhs++, dest++, j++)
          {
            double sum = 0;
            for(int k=0, ri=0; k<a.Width; ri += b.Width, k++) sum += lhs[k] * rhs[ri];
            *dest = sum;
          }
        }
      }
      return result;
    }

    public static unsafe Matrix MultiplyByDiagonal(Matrix a, Matrix bDiagonal)
    {
      if(a == null || bDiagonal == null) throw new ArgumentNullException();
      if(a.Width != bDiagonal.Height)
      {
        throw new ArgumentException("The width of the left matrix does not match the height of the right matrix.");
      }
      // A * B: | a b c |   | g 0 |   | ag bh |
      //        | d e f | * | 0 h | = | dg eh |
      //                    | 0 0 |
      Matrix result = new Matrix(a.Height, bDiagonal.Width);
      fixed(double* plhs=a.data, rhs=bDiagonal.data, pdest=result.data)
      {
        double* lhs=plhs, dest=pdest;
        for(int i=0; i<result.Height; lhs += a.Width, i++)
        {
          for(int j=0,ri=0; j<result.Width; ri += bDiagonal.Width+1, dest++, j++) *dest = lhs[j] * rhs[ri];
        }
      }
      return result;
    }

    public static unsafe Matrix MultiplyByDiagonal(Matrix a, Vector bDiagonal)
    {
      if(a == null || bDiagonal == null) throw new ArgumentNullException();
      if(a.Width != bDiagonal.Size) throw new ArgumentException("The width of the matrix does not match the size of the vector.");
      Matrix result = new Matrix(a.Height, bDiagonal.Size);
      fixed(double* plhs=a.data, rhs=bDiagonal.Array, pdest=result.data)
      {
        double* lhs=plhs, dest=pdest;
        for(int i=0; i<result.Height; lhs += a.Width, i++)
        {
          for(int j=0; j<result.Width; dest++, j++) *dest = lhs[j] * rhs[j];
        }
      }
      return result;
    }

    public static unsafe Matrix MultiplyByTranspose(Matrix a, Matrix bToTranspose)
    {
      if(a == null || bToTranspose == null) throw new ArgumentNullException();
      if(a.Width != bToTranspose.Width) throw new ArgumentException("The widths of the matrices do not match.");
      // A * transpose(B):  | a b c |             | g h i |    | a b c |   | g j |
      //                    | d e f | * transpose(| j k l |) = | d e f | * | h k |
      //                                                                   | i l |
      Matrix result = new Matrix(a.Height, bToTranspose.Height);
      fixed(double* plhs=a.data, prhs=bToTranspose.data, pdest=result.data)
      {
        double* lhs=plhs, dest=pdest;
        for(int i=0; i<result.Height; lhs += a.Width, i++)
        {
          double* rhs=prhs;
          for(int j=0; j<result.Width; rhs += bToTranspose.Width, dest++, j++)
          {
            double sum = 0;
            for(int k=0; k<a.Width; k++) sum += lhs[k] * rhs[k];
            *dest = sum;
          }
        }
      }
      return result;
    }

    public static unsafe Matrix PremultiplyByTranspose(Matrix aToTranspose, Matrix b)
    {
      if(aToTranspose == null || b == null) throw new ArgumentNullException();
      if(aToTranspose.Height != b.Height) throw new ArgumentException("The heights of the matrices do not match.");
      // transpose(A) * B:            | a b c |    | g h i |   | a d |   | g h i |
      //                    transpose(| d e f |) * | j k l | = | b e | * | j k l |
      //                                                       | c f |
      Matrix result = new Matrix(aToTranspose.Width, b.Width);
      fixed(double* plhs=aToTranspose.data, prhs=b.data, pdest=result.data)
      {
        double* lhs=plhs, dest=pdest;
        for(int i=0; i<result.Height; lhs++, i++)
        {
          double* rhs=prhs;
          for(int j=0; j<result.Width; rhs++, dest++, j++)
          {
            double sum = 0;
            for(int k=0, li=0, ri=0; k<aToTranspose.Height; li += aToTranspose.Width, ri += b.Width, k++) sum += lhs[li] * rhs[ri];
            *dest = sum;
          }
        }
      }
      return result;
    }

    public static Matrix Pseudoinvert(Matrix matrix)
    {
      return new SVDecomposition(matrix).GetInverse();
    }

    /// <summary>Resizes the given matrix if it exists, or allocates a new matrix if it is null.</summary>
    public static void Resize(ref Matrix dest, int height, int width)
    {
      if(dest == null) dest = new Matrix(height, width);
      else dest.Resize(height, width);
    }

    public static Matrix Subtract(Matrix a, Matrix b)
    {
      if(a == null) throw new ArgumentNullException();
      Matrix result = a.Clone();
      result.Subtract(b);
      return result;
    }

    public static Matrix Transpose(Matrix matrix)
    {
      if(matrix == null) throw new ArgumentNullException();

      Matrix dest = new Matrix(matrix.Width, matrix.Height);
      for(int y=0; y<matrix.Height; y++)
      {
        for(int x=0; x<matrix.Width; x++) dest[x, y] = matrix[y, x];
      }
      return dest;
    }

    void AssertSameSize(Matrix matrix)
    {
      if(Width != matrix.Width || Height != matrix.Height) throw new ArgumentException("The matrices must be of the same dimensions.");
    }

    void AssertSquare()
    {
      if(!IsSquare) throw new InvalidOperationException("The matrix must be square.");
    }

    double[,] data;

    #region ICloneable Members
    object ICloneable.Clone()
    {
      return Clone();
    }
    #endregion
  }
  #endregion
} // namespace AdamMil.Mathematics