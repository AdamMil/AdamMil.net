/*
AdamMil.Mathematics is a library that provides some useful mathematics classes
for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2010 Adam Milazzo

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
using AdamMil.Mathematics.Geometry.ThreeD;
using AdamMil.Utilities;

#warning Document matrices!

namespace AdamMil.Mathematics.Matrices
{

#pragma warning disable 1591
#region Matrix3
[Serializable]
public sealed class Matrix3
{
  public Matrix3() { M00=M11=M22=1; }

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
    if(matrix == null) throw new ArgumentNullException();
    fixed(double* src=&matrix.M00)
    fixed(double* dest=&M00)
    {
      Unsafe.Copy(src, dest, Length*sizeof(double));
    }
  }

  internal Matrix3(bool dummy) { }

  public const int Width=3, Height=3, Length=Width*Height;

  public unsafe double this[int index]
  { get
    { if(index<0 || index>=Length) throw new ArgumentOutOfRangeException();
      fixed(double* data=&M00) return data[index];
    }
    set
    { if(index<0 || index>=Length) throw new ArgumentOutOfRangeException();
      fixed(double* data=&M00) data[index]=value;
    }
  }

  public unsafe double this[int i, int j]
  { get
    {
      if(i<0 || i>=Height || j<0 || j>=Width) throw new ArgumentOutOfRangeException();
      fixed(double* data=&M00) return data[i*Height+j];
    }
    set
    {
      if(i<0 || i>=Height || j<0 || j>=Width) throw new ArgumentOutOfRangeException();
      fixed(double* data=&M00) data[i*Height+j]=value;
    }
  }

  public unsafe Matrix3 Transpose
  { get
    { Matrix3 ret = new Matrix3(false);
      fixed(double* src=&M00) fixed(double* dest=&ret.M00)
      { dest[0]=src[0]; dest[1]=src[3]; dest[2]=src[6];
        dest[3]=src[1]; dest[4]=src[4]; dest[5]=src[7];
        dest[6]=src[2]; dest[7]=src[5]; dest[8]=src[8];
      }
      return ret;
    }
  }

  public override bool Equals(object obj)
  {
    Matrix3 other = obj as Matrix3;
    return other==null ? false : this == other;
  }

  public bool Equals(Matrix3 other)
  {
    return this == other;
  }

  public unsafe bool Equals(Matrix3 other, double epsilon)
  {
    fixed(double* ap=&M00) fixed(double* bp=&other.M00)
      for(int i=0; i<Length; i++) if(Math.Abs(ap[i]-bp[i])>epsilon) return false;
    return true;
  }

  /// <include file="documentation.xml" path="//Common/GetHashCode/*"/>
  public unsafe override int GetHashCode()
  { int hash = 0;
    fixed(double* dp=&M00) { int* p=(int*)dp; for(int i=0; i<Length*2; i++) hash ^= p[i]; }
    return hash;
  }

  public Vector Multiply(Vector v)
  { return new Vector(M00*v.X + M01*v.Y + M02*v.Z,
                      M10*v.X + M11*v.Y + M12*v.Z,
                      M20*v.X + M21*v.Y + M22*v.Z);
  }

  public void Multiply(IList<Vector> vectors)
  { for(int i=0; i<vectors.Count; i++) vectors[i] = Multiply(vectors[i]);
  }

  public void Scale(double x, double y, double z) { M00*=x; M11*=y; M22*=z; }

  public unsafe double[] ToArray()
  { double[] ret = new double[Length];
    fixed(double* src=&M00) fixed(double* dest=ret) for(int i=0; i<Length; i++) dest[i]=src[i];
    return ret;
  }

  public static unsafe void Add(Matrix3 a, Matrix3 b, Matrix3 dest)
  { fixed(double* ap=&a.M00) fixed(double* bp=&b.M00) fixed(double* dp=&dest.M00)
      for(int i=0; i<Length; i++) dp[i] = ap[i]+bp[i];
  }

  public static unsafe void Subtract(Matrix3 a, Matrix3 b, Matrix3 dest)
  { fixed(double* ap=&a.M00) fixed(double* bp=&b.M00) fixed(double* dp=&dest.M00)
      for(int i=0; i<Length; i++) dp[i] = ap[i]-bp[i];
  }

  public static unsafe void Multiply(Matrix3 a, Matrix3 b, Matrix3 dest)
  { fixed(double* ap=&a.M00) fixed(double* bp=&b.M00) fixed(double* dp=&dest.M00)
    { dp[0] = ap[0]*bp[0] + ap[1]*bp[3] + ap[2]*bp[6];
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

  public static Matrix3 Rotation(double x, double y, double z)
  { double a=Math.Cos(x), b=Math.Sin(x), c=Math.Cos(y), d=Math.Sin(y), e=Math.Cos(z), f=Math.Sin(z), ad=a*d, bd=b*d;
    Matrix3 ret = new Matrix3(false);
    ret.M00=c*e;         ret.M01=-(c*f);      ret.M02=d;
    ret.M10=bd*e+a*f;    ret.M11=-(bd*f)+a*e; ret.M12=-(b*c);
    ret.M20=-(ad*e)+b*f; ret.M21=ad*f+b*e;    ret.M22=a*c;
    return ret;
  }

  public static Matrix3 Rotation(double angle, Vector axis)
  { double cos=Math.Cos(angle), sin=Math.Sin(angle);
    Vector axisc1m = axis * (1-cos);
    Matrix3 ret = new Matrix3(false);
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

  public static Matrix3 Rotation(Vector start, Vector end)
  { Vector cross = start.CrossProduct(end);
    // if the vectors are colinear, rotate one by 90 degrees and use that.
    if(cross.X==0 && cross.Y==0 && cross.Z==0)
      return start.Equals(end, 0.001) ? new Matrix3() : Rotation(Math.PI, new Vector(-start.Y, start.X, start.Z));
    return Rotation(Math.Acos(start.DotProduct(end)), cross);
  }

  public static Matrix3 RotationX(double angle)
  { double sin=Math.Sin(angle), cos=Math.Cos(angle);
    Matrix3 ret = new Matrix3(false);
    ret.M00=1; ret.M11=cos; ret.M12=-sin; ret.M21=sin; ret.M22=cos;
    return ret;
  }

  public static Matrix3 RotationY(double angle)
  { double sin=Math.Sin(angle), cos=Math.Cos(angle);
    Matrix3 ret = new Matrix3(false);
    ret.M00=cos; ret.M02=sin; ret.M11=1; ret.M20=-sin; ret.M22=cos;
    return ret;
  }

  public static Matrix3 RotationZ(double angle)
  { double sin=Math.Sin(angle), cos=Math.Cos(angle);
    Matrix3 ret = new Matrix3(false);
    ret.M00=cos; ret.M01=-sin; ret.M10=sin; ret.M11=cos; ret.M22=1;
    return ret;
  }

  public static Matrix3 Scaling(double x, double y, double z)
  { Matrix3 ret = new Matrix3(false);
    ret.M00=x; ret.M11=y; ret.M22=z;
    return ret;
  }

  public static Matrix3 Shearing(double xy, double xz, double yx, double yz, double zx, double zy)
  { Matrix3 ret = new Matrix3();
    ret.M01=yx; ret.M02=zx; ret.M10=xy; ret.M12=zy; ret.M20=xz; ret.M21=yz;
    return ret;
  }

  public static unsafe bool operator==(Matrix3 a, Matrix3 b)
  {
    fixed(double* ap=&a.M00) fixed(double* bp=&b.M00) for(int i=0; i<Length; i++) if(ap[i]!=bp[i]) return false;
    return true;
  }

  public static bool operator!=(Matrix3 a, Matrix3 b)
  {
    return !(a == b);
  }

  public static Matrix3 operator+(Matrix3 a, Matrix3 b)
  { Matrix3 ret = new Matrix3(false);
    Add(a, b, ret);
    return ret;
  }

  public static Matrix3 operator-(Matrix3 a, Matrix3 b)
  { Matrix3 ret = new Matrix3(false);
    Subtract(a, b, ret);
    return ret;
  }

  public static Matrix3 operator*(Matrix3 a, Matrix3 b)
  { Matrix3 ret = new Matrix3(false);
    Multiply(a, b, ret);
    return ret;
  }

  #pragma warning disable 1591
  public double M00, M01, M02,
                M10, M11, M12,
                M20, M21, M22;
  #pragma warning restore 1591
}
#endregion

#pragma warning disable 1591
#region Matrix4
[Serializable]
public sealed class Matrix4
{
  public Matrix4() { M00=M11=M22=M33=1; }

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
    if(matrix == null) throw new ArgumentNullException();
    fixed(double* src=&matrix.M00)
    fixed(double* dest=&M00)
    {
      Unsafe.Copy(src, dest, Length*sizeof(double));
    }
  }

  internal Matrix4(bool dummy) { }

  public const int Width=4, Height=4, Length=Width*Height;

  public unsafe double this[int index]
  { get
    {
      if(index<0 || index>=Length) throw new ArgumentOutOfRangeException();
      fixed(double* data=&M00) return data[index];
    }
    set
    {
      if(index<0 || index>=Length) throw new ArgumentOutOfRangeException();
      fixed(double* data=&M00) data[index]=value;
    }
  }

  public unsafe double this[int i, int j]
  { get
    {
      if(i<0 || i>=Height || j<0 || j>=Width) throw new ArgumentOutOfRangeException();
      fixed(double* data=&M00) return data[i*Height+j];
    }
    set
    {
      if(i<0 || i>=Height || j<0 || j>=Width) throw new ArgumentOutOfRangeException();
      fixed(double* data=&M00) data[i*Height+j]=value;
    }
  }

  public unsafe Matrix4 Transpose
  { get
    { Matrix4 ret = new Matrix4(false);
      fixed(double* src=&M00) fixed(double* dest=&ret.M00)
      { dest[0] =src[0];  dest[1] =src[4];  dest[2] =src[8];  dest[3] =dest[12];
        dest[4] =src[1];  dest[5] =src[5];  dest[6] =src[9];  dest[7] =dest[13];
        dest[8] =src[2];  dest[9] =src[6];  dest[10]=src[10]; dest[11]=dest[14];
        dest[12]=src[3];  dest[13]=src[7];  dest[14]=src[11]; dest[15]=dest[15];
      }
      return ret;
    }
  }

  public override bool Equals(object obj)
  {
    Matrix4 other = obj as Matrix4;
    return other==null ? false : this == other;
  }

  public bool Equals(Matrix4 other)
  {
    return this == other;
  }

  public unsafe bool Equals(Matrix4 other, double epsilon)
  {
    fixed(double* ap=&M00) fixed(double* bp=&other.M00)
      for(int i=0; i<Length; i++) if(Math.Abs(ap[i]-bp[i])>epsilon) return false;
    return true;
  }

  /// <include file="documentation.xml" path="//Common/GetHashCode/*"/>
  public unsafe override int GetHashCode()
  {
    int hash = 0;
    fixed(double* dp=&M00) { int* p=(int*)dp; for(int i=0; i<Length*2; i++) hash ^= p[i]; }
    return hash;
  }

  public Vector Multiply(Vector v)
  { return new Vector(M00*v.X + M01*v.Y + M02*v.Z + M03,
                      M10*v.X + M11*v.Y + M12*v.Z + M13,
                      M20*v.X + M21*v.Y + M22*v.Z + M23);
  }

  public void Multiply(IList<Vector> vectors)
  { for(int i=0; i<vectors.Count; i++) vectors[i] = Multiply(vectors[i]);
  }

  public void Scale(double x, double y, double z) { M00*=x; M11*=y; M22*=z; }
  public void Translate(double x, double y, double z) { M03+=x; M13+=y; M23+=z; }

  public unsafe double[] ToArray()
  { double[] ret = new double[Length];
    fixed(double* src=&M00) fixed(double* dest=ret) for(int i=0; i<Length; i++) dest[i]=src[i];
    return ret;
  }

  public static unsafe void Add(Matrix4 a, Matrix4 b, Matrix4 dest)
  { fixed(double* ap=&a.M00) fixed(double* bp=&b.M00) fixed(double* dp=&dest.M00)
      for(int i=0; i<Length; i++) dp[i] = ap[i]+bp[i];
  }

  public static unsafe void Subtract(Matrix4 a, Matrix4 b, Matrix4 dest)
  { fixed(double* ap=&a.M00) fixed(double* bp=&b.M00) fixed(double* dp=&dest.M00)
      for(int i=0; i<Length; i++) dp[i] = ap[i]-bp[i];
  }

  public static unsafe void Multiply(Matrix4 a, Matrix4 b, Matrix4 dest)
  { fixed(double* ap=&a.M00) fixed(double* bp=&b.M00) fixed(double* dp=&dest.M00)
    { dp[0]  = ap[0]*bp[0]  + ap[1]*bp[4]  + ap[2]*bp[8]   + ap[3]*bp[12];
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

  public static Matrix4 Rotation(double x, double y, double z)
  { double a=Math.Cos(x), b=Math.Sin(x), c=Math.Cos(y), d=Math.Sin(y), e=Math.Cos(z), f=Math.Sin(z), ad=a*d, bd=b*d;
    Matrix4 ret = new Matrix4(false);
    ret.M00=c*e;         ret.M01=-(c*f);      ret.M02=d;
    ret.M10=bd*e+a*f;    ret.M11=-(bd*f)+a*e; ret.M12=-(b*c);
    ret.M20=-(ad*e)+b*f; ret.M21=ad*f+b*e;    ret.M22=a*c;
    ret.M33=1;
    return ret;
  }

  public static Matrix4 Rotation(double angle, Vector axis)
  { double cos=Math.Cos(angle), sin=Math.Sin(angle);
    Vector axisc1m = axis * (1-cos);
    Matrix4 ret = new Matrix4(false);
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

  public static Matrix4 Rotation(Vector start, Vector end)
  { Vector cross = start.CrossProduct(end);
    // if the vectors are colinear, rotate one by 90 degrees and use that.
    if(cross.X==0 && cross.Y==0 && cross.Z==0)
      return start.Equals(end, 0.001) ? new Matrix4() : Rotation(Math.PI, new Vector(-start.Y, start.X, start.Z));
    return Rotation(Math.Acos(start.DotProduct(end)), cross);
  }

  public static Matrix4 RotationX(double angle)
  { double sin=Math.Sin(angle), cos=Math.Cos(angle);
    Matrix4 ret = new Matrix4(false);
    ret.M00=1; ret.M11=cos; ret.M12=-sin; ret.M21=sin; ret.M22=cos; ret.M33=1;
    return ret;
  }

  public static Matrix4 RotationY(double angle)
  { double sin=Math.Sin(angle), cos=Math.Cos(angle);
    Matrix4 ret = new Matrix4(false);
    ret.M00=cos; ret.M02=sin; ret.M11=1; ret.M20=-sin; ret.M22=cos; ret.M33=1;
    return ret;
  }

  public static Matrix4 RotationZ(double angle)
  { double sin=Math.Sin(angle), cos=Math.Cos(angle);
    Matrix4 ret = new Matrix4(false);
    ret.M00=cos; ret.M01=-sin; ret.M10=sin; ret.M11=cos; ret.M22=1; ret.M33=1;
    return ret;
  }

  public static Matrix4 Scaling(double x, double y, double z)
  { Matrix4 ret = new Matrix4(false);
    ret.M00=x; ret.M11=y; ret.M22=z; ret.M33=1;
    return ret;
  }

  public static Matrix4 Shearing(double xy, double xz, double yx, double yz, double zx, double zy)
  { Matrix4 ret = new Matrix4();
    ret.M01=yx; ret.M02=zx; ret.M10=xy; ret.M12=zy; ret.M20=xz; ret.M21=yz;
    return ret;
  }

  public static Matrix4 Translation(double x, double y, double z)
  { Matrix4 ret = new Matrix4();
    ret.M03=x; ret.M13=y; ret.M23=z;
    return ret;
  }

  public static Matrix4 operator+(Matrix4 a, Matrix4 b)
  { Matrix4 ret = new Matrix4(false);
    Add(a, b, ret);
    return ret;
  }

  public static Matrix4 operator-(Matrix4 a, Matrix4 b)
  { Matrix4 ret = new Matrix4(false);
    Subtract(a, b, ret);
    return ret;
  }

  public static unsafe bool operator==(Matrix4 a, Matrix4 b)
  {
    fixed(double* ap=&a.M00) fixed(double* bp=&b.M00) for(int i=0; i<Length; i++) if(ap[i]!=bp[i]) return false;
    return true;
  }

  public static bool operator!=(Matrix4 a, Matrix4 b)
  {
    return !(a == b);
  }

  public static Matrix4 operator*(Matrix4 a, Matrix4 b)
  { Matrix4 ret = new Matrix4(false);
    Multiply(a, b, ret);
    return ret;
  }

  #pragma warning disable 1591
  public double M00, M01, M02, M03,
                M10, M11, M12, M13,
                M20, M21, M22, M23,
                M30, M31, M32, M33;
  #pragma warning restore 1591
}
#endregion

} // namespace AdamMil.Mathematics.Matrices