/*
AdamMil.Mathematics is a library that provides some useful mathematics classes
for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2011 Adam Milazzo

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
using AdamMil.Mathematics.LinearEquations;
using AdamMil.Utilities;

#warning Document matrices!

namespace AdamMil.Mathematics
{
  #pragma warning disable 1591
  #region Matrix3
  [Serializable]
  public sealed class Matrix3 : ICloneable, IEquatable<Matrix3>
  {
    public Matrix3() { }

    public unsafe Matrix3(double[] data)
    {
      if(data == null) throw new ArgumentNullException();
      if(data.Length != Length) throw new ArgumentException("Expected an array of 9 elements.");
      fixed(double* src=data)
      fixed(double* dest=&M00)
      {
        Unsafe.Copy(src, dest, Length*sizeof(double));
      }
    }

    public unsafe Matrix3(Matrix3 matrix)
    {
      if((object)matrix == null) throw new ArgumentNullException();
      fixed(double* src=&matrix.M00)
      fixed(double* dest=&M00)
      {
        Unsafe.Copy(src, dest, Length*sizeof(double));
      }
    }

    public unsafe Matrix3(Matrix matrix)
    {
      if((object)matrix == null) throw new ArgumentNullException();
      if(matrix.Width != Width || matrix.Height != Height) throw new ArgumentException("The matrix is the wrong size.");
      fixed(double* src=matrix.Data)
      fixed(double* dest=&M00)
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

    /// <include file="documentation.xml" path="//Common/GetHashCode/*"/>
    public unsafe override int GetHashCode()
    {
      int hash = 0;
      fixed(double* dp=&M00) { int* p=(int*)dp; for(int i=0; i<Length*2; i++) hash ^= p[i]; }
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
      fixed(double* src=&M00)
      fixed(double* dest=ret)
      {
        for(int i=0; i<Length; i++) dest[i]=src[i];
      }
      return ret;
    }

    public unsafe Matrix ToMatrix()
    {
      fixed(double* data=&M00) return new Matrix(data, Width, Height);
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
      if((object)a == null || (object)b == null || (object)dest == null) throw new ArgumentNullException();

      fixed(double* ap=&a.M00)
      fixed(double* bp=&b.M00)
      fixed(double* dp=&dest.M00)
      {
        for(int i=0; i<Length; i++) dp[i] = ap[i]+bp[i];
      }
    }

    public static unsafe void Subtract(Matrix3 a, Matrix3 b, Matrix3 dest)
    {
      if((object)a == null || (object)b == null || (object)dest == null) throw new ArgumentNullException();

      fixed(double* ap=&a.M00)
      fixed(double* bp=&b.M00)
      fixed(double* dp=&dest.M00)
      {
        for(int i=0; i<Length; i++) dp[i] = ap[i]-bp[i];
      }
    }

    public static unsafe void Multiply(Matrix3 a, Matrix3 b, Matrix3 dest)
    {
      if((object)a == null || (object)b == null || (object)dest == null) throw new ArgumentNullException();

      fixed(double* ap=&a.M00)
      fixed(double* bp=&b.M00)
      fixed(double* dp=&dest.M00)
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
      if((object)a == null) return (object)b == null;
      else if((object)b == null) return false;

      fixed(double* ap=&a.M00)
      fixed(double* bp=&b.M00)
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
      if((object)a == null) return (object)b == null;
      else if((object)b == null) return false;

      fixed(double* ap=&a.M00)
      fixed(double* bp=&b.M00)
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
      if((object)matrix == null) throw new ArgumentNullException();
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
      if((object)matrix == null) throw new ArgumentNullException();
      matrix = matrix.Clone();
      matrix.Transpose();
      return matrix;
    }

    public static bool operator==(Matrix3 a, Matrix3 b)
    {
      return Equals(a, b);
    }

    public static bool operator!=(Matrix3 a, Matrix3 b)
    {
      return !Equals(a, b);
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

  #pragma warning disable 1591
  #region Matrix4
  [Serializable]
  public sealed class Matrix4 : ICloneable, IEquatable<Matrix4>
  {
    public Matrix4() { }

    public unsafe Matrix4(double[] data)
    {
      if(data == null) throw new ArgumentNullException();
      if(data.Length != Length) throw new ArgumentException("Expected an array of 16 elements.");
      fixed(double* src=data)
      fixed(double* dest=&M00)
      {
        Unsafe.Copy(src, dest, Length*sizeof(double));
      }
    }

    public unsafe Matrix4(Matrix4 matrix)
    {
      if((object)matrix == null) throw new ArgumentNullException();
      fixed(double* src=&matrix.M00)
      fixed(double* dest=&M00)
      {
        Unsafe.Copy(src, dest, Length*sizeof(double));
      }
    }

    public unsafe Matrix4(Matrix matrix)
    {
      if((object)matrix == null) throw new ArgumentNullException();
      if(matrix.Width != Width || matrix.Height != Height) throw new ArgumentException("The matrix is the wrong size.");
      fixed(double* src=matrix.Data)
      fixed(double* dest=&M00)
      {
        Unsafe.Copy(src, dest, Length*sizeof(double));
      }
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

    /// <include file="documentation.xml" path="//Common/GetHashCode/*"/>
    public unsafe override int GetHashCode()
    {
      int hash = 0;
      fixed(double* dp=&M00) { int* p=(int*)dp; for(int i=0; i<Length*2; i++) hash ^= p[i]; }
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
      fixed(double* data=&M00) return new Matrix(data, Width, Height);
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
      fixed(double* src=&M00)
      fixed(double* dest=ret)
      {
        Unsafe.Copy(src, dest, Length*sizeof(double));
      }
      return ret;
    }

    public static unsafe void Add(Matrix4 a, Matrix4 b, Matrix4 dest)
    {
      if((object)a == null || (object)b == null || (object)dest == null) throw new ArgumentNullException();

      fixed(double* ap=&a.M00)
      fixed(double* bp=&b.M00)
      fixed(double* dp=&dest.M00)
      {
        for(int i=0; i<Length; i++) dp[i] = ap[i]+bp[i];
      }
    }

    public static unsafe void Subtract(Matrix4 a, Matrix4 b, Matrix4 dest)
    {
      if((object)a == null || (object)b == null || (object)dest == null) throw new ArgumentNullException();

      fixed(double* ap=&a.M00)
      fixed(double* bp=&b.M00)
      fixed(double* dp=&dest.M00)
      {
        for(int i=0; i<Length; i++) dp[i] = ap[i]-bp[i];
      }
    }

    public static unsafe void Multiply(Matrix4 a, Matrix4 b, Matrix4 dest)
    {
      if((object)a == null || (object)b == null || (object)dest == null) throw new ArgumentNullException();

      fixed(double* ap=&a.M00)
      fixed(double* bp=&b.M00)
      fixed(double* dp=&dest.M00)
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
      if((object)a == null) return (object)b == null;
      else if((object)b == null) return false;

      fixed(double* ap=&a.M00)
      fixed(double* bp=&b.M00)
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
      if((object)a == null) return (object)b == null;
      else if((object)b == null) return false;

      fixed(double* ap=&a.M00)
      fixed(double* bp=&b.M00)
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
      if((object)matrix == null) throw new ArgumentNullException();
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
      if((object)matrix == null) throw new ArgumentNullException();
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

    public static bool operator==(Matrix4 a, Matrix4 b)
    {
      return Equals(a, b);
    }

    public static bool operator!=(Matrix4 a, Matrix4 b)
    {
      return !Equals(a, b);
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

  #pragma warning disable 1591
  #region Matrix
  [Serializable]
  public sealed class Matrix : ICloneable, IEquatable<Matrix>
  {
    public Matrix(int width, int height)
    {
      Resize(width, height);
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
      fixed(double* source=data)
      fixed(double* dest=this.data)
      {
        Unsafe.Copy(source, dest, data.Length*sizeof(double));
      }
    }

    [CLSCompliant(false)]
    public unsafe Matrix(double* data, int width, int height)
    {
      if(width < 0 || height < 0) throw new ArgumentOutOfRangeException();
      Width     = width;
      Height    = height;
      this.data = new double[height, width];
      fixed(double* dest=this.data) Unsafe.Copy(data, dest, this.data.Length*sizeof(double));
    }

    public Matrix(Matrix matrix)
    {
      if((object)matrix == null) throw new ArgumentNullException();
      Width  = matrix.Width;
      Height = matrix.Height;
      data   = matrix.data.Length == 0 ? matrix.data : (double[,])matrix.data.Clone();
    }

    public double this[int row, int column]
    {
      get { return data[row, column]; }
      set { data[row, column] = value; }
    }

    public double[,] Data
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
      if((object)matrix == null) throw new ArgumentNullException();
      AssertSameSize(matrix);
      fixed(double* dest=data)
      fixed(double* src=matrix.data)
      {
        for(int i=0, length=data.Length; i<length; i++) dest[i] += src[i];
      }
    }

    public unsafe void Subtract(Matrix matrix)
    {
      if((object)matrix == null) throw new ArgumentNullException();
      AssertSameSize(matrix);
      fixed(double* dest=data)
      fixed(double* src=matrix.data)
      {
        for(int i=0, length=data.Length; i<length; i++) dest[i] -= src[i];
      }
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

    public unsafe void Divide(double factor)
    {
      fixed(double* pdata=data)
      {
        for(int i=0, length=data.Length; i<length; i++) pdata[i] /= factor;
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
      fixed(double* pdata=data)
      {
        int* p=(int*)pdata;
        for(int i=0, length=data.Length*2; i<length; i++) hash ^= p[i];
      }
      return hash;
    }

    public double GetLogDeterminant(out bool negative)
    {
      AssertSquare();
      return new LUDecomposition(this).GetLogDeterminant(out negative);
    }

    public unsafe void Invert()
    {
      // do the inversion in a separate matrix to prevent this matrix from being clobbered if it turns out to not be invertible
      Matrix inverse = Invert(this);
      fixed(double* src=inverse.data)
      fixed(double* dest=data)
      {
        Unsafe.Copy(src, dest, data.Length*sizeof(double));
      }
    }

    public unsafe void Multiply(double factor)
    {
      fixed(double* pdata=data)
      {
        for(int i=0, length=data.Length; i<length; i++) pdata[i] *= factor;
      }
    }

    public void Resize(int width, int height)
    {
      if(width != Width || height != Height)
      {
        if(width < 0 || height < 0) throw new ArgumentOutOfRangeException();
        Width  = width;
        Height = height;
        data   = new double[height, width];
      }
    }

    public void SetIdentity()
    {
      AssertSquare();
      Array.Clear(data, 0, data.Length);
      for(int i=0; i<Width; i++) data[i, i] = 1;
    }

    public void Swap(int row1, int column1, int row2, int column2)
    {
      Utility.Swap(ref data[row1, column1], ref data[row2, column2]);
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

    public static bool operator==(Matrix a, Matrix b)
    {
      return Equals(a, b);
    }

    public static bool operator!=(Matrix a, Matrix b)
    {
      return !Equals(a, b);
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
      if((object)a == null) throw new ArgumentNullException();
      Matrix result = a.Clone();
      result.Add(b);
      return result;
    }

    public static Matrix Subtract(Matrix a, Matrix b)
    {
      if((object)a == null) throw new ArgumentNullException();
      Matrix result = a.Clone();
      result.Subtract(b);
      return result;
    }

    public static Matrix Divide(Matrix a, double b)
    {
      if((object)a == null) throw new ArgumentNullException();
      a = a.Clone();
      a.Divide(b);
      return a;
    }

    public unsafe static bool Equals(Matrix a, Matrix b)
    {
      if((object)a == null) return (object)b == null;
      else if((object)b == null || a.Width != b.Width || a.Height != b.Height) return false;

      fixed(double* pa=a.data)
      fixed(double* pb=b.data)
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
      if((object)a == null) return (object)b == null;
      else if((object)b == null || a.Width != b.Width || a.Height != b.Height) return false;

      fixed(double* pa=a.data)
      fixed(double* pb=b.data)
      {
        for(int i=0, length=a.data.Length; i<length; i++)
        {
          if(Math.Abs(pa[i] - pb[i]) > tolerance) return false;
        }
      }
      return true;
    }

    public static Matrix Multiply(Matrix a, double b)
    {
      if((object)a == null) throw new ArgumentNullException();
      a = a.Clone();
      a.Multiply(b);
      return a;
    }

    public static Matrix Multiply(Matrix a, Matrix b)
    {
      if((object)a == null || (object)b == null) throw new ArgumentNullException();
      // when multiplying A*B, an element of the result is sum of the products of pairs of elements from the same row in A and the same
      // column in B.
      Matrix result = new Matrix(b.Width, a.Height);
      for(int dy=0; dy<result.Height; dy++)
      {
        for(int dx=0; dx<result.Width; dx++)
        {
          double sum = 0;
          for(int i=0; i<a.Width; i++) sum += a[dy, i] * b[i, dx];
          result[dy, dx] = sum;
        }
      }
      return result;
    }

    public static unsafe Matrix Augment(Matrix a, Matrix b)
    {
      if((object)a == null || (object)b == null) throw new ArgumentNullException();
      if(a.Height != b.Height) throw new ArgumentException("The matrices must have the same height.");
      Matrix result = new Matrix(a.Width+b.Width, a.Height);

      fixed(double* pdest=result.Data)
      fixed(double* pa=a.Data)
      fixed(double* pb=b.Data)
      for(int dy=0,di=0,ai=0,bi=0; dy<result.Height; dy++)
      {
        Unsafe.Copy(pa+ai, pdest+di, a.Width*sizeof(double));
        di += a.Width;
        ai += a.Width;
        Unsafe.Copy(pb+bi, pdest+di, b.Width*sizeof(double));
        di += b.Width;
        bi += b.Width;
      }
      return result;
    }

    public static Matrix CreateIdentity(int size)
    {
      Matrix matrix = new Matrix(size, size);
      for(int i=0; i<size; i++) matrix[i, i] = 1;
      return matrix;
    }

    public static Matrix Invert(Matrix matrix)
    {
      return GaussJordan.Invert(matrix);
    }

    public static Matrix Transpose(Matrix matrix)
    {
      if((object)matrix == null) throw new ArgumentNullException();

      Matrix dest = new Matrix(matrix.Height, matrix.Width);
      for(int y=0; y<matrix.Height; y++)
      {
        for(int x=0; x<matrix.Width; x++) dest[x, y] = matrix[y, x];
      }
      return matrix;
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