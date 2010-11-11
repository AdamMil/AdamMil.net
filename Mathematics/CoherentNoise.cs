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

namespace AdamMil.Mathematics.Random
{

/// <summary>Implements a coherent noise source, which produces a smoother, more natural noise. Coherent noise is
/// useful for creating procedural textures, generating landscapes and clouds, drawing "natural" shapes that exhibit
/// slight wobbles, and many other things. Specifically, Perlin and Simplex noise in 1, 2, and 3 dimensions are implemented.
/// </summary>
  /// <include file="documentation.xml" path="/Math/CoherentNoise/AboutPerlin/*" />
  /// <include file="documentation.xml" path="/Math/CoherentNoise/AboutSimplex/*" />
  public sealed class CoherentNoise
{
  /// <summary>Initializes a new <see cref="CoherentNoise"/> object using the default seed.</summary>
  public CoherentNoise()
  {
    ResetToDefaultSeed();
  }

  /// <summary>Initializes a new <see cref="CoherentNoise"/> object, seeding it with values from the given random number
  /// generator.
  /// </summary>
  [CLSCompliant(false)]
  public CoherentNoise(RandomNumberGenerator random)
  {
    Reseed(random);
  }

  /// <summary>Generates one-dimensional Perlin noise based on the given position on the x axis. The noise value will
  /// lie in the range [-1,1], and will begin to repeat if <paramref name="x"/> is outside the range [0,256).
  /// </summary>
  /// <include file="documentation.xml" path="/Math/CoherentNoise/AboutPerlin/*" />
  public double Perlin(double x)
  {
    int ix = FastFloor(x);
    x -= ix; // find the x offset within that span

    // finally, interpolate between the gradients and scale the value to be within (-1,1).

    // we divide by 8 because: the interpolation is a + t(b-a). a and b are the gradients, where a=Rx and b=S(x-1), and
    // R and S are random values in (-16,16). t is the fade factor, t = fade(x) = 6x^5 - 15x^4 + 10x^3. the
    // interpolation becomes Rx + (6x^5 - 15x^4 + 10x^3)(S(x-1)-Rx) = Rx + (6x^5 - 15x^4 + 10x^3)(Sx - S - Rx).
    // this equation happens to be maximized when x=1/2, R=16, and S=-16, where equals 8. thus, the range of the
    // interpolation is -8 to 8, so we divide by 8.
    return Interpolate(Fade(x), Gradient(permutation[ix&255], x), Gradient(permutation[(ix+1)&255], x-1)) * (1.0/8);
  }

  /// <summary>Generates two-dimensional Perlin noise based on the given position on the x and y axes. The noise value
  /// will lie in the range [-1,1], and will repeat if <paramref name="x"/> or <paramref name="y"/> is outside the
  /// range [0,256).
  /// </summary>
  /// <include file="documentation.xml" path="/Math/CoherentNoise/AboutPerlin/*" />
  public double Perlin(double x, double y)
  {
    int ix = FastFloor(x), iy = FastFloor(y); // find the unit square that contains the point
    x -= ix; // find x,y offset within that unit square
    y -= iy;

    // compute hash codes for 4 vectors within the unit square.
    // these correspond to p[p[i]+j] where i={x,x+1} and j={y,y+1}
    int a = permutation[ix&255]+iy, b = permutation[(ix+1)&255]+iy;

    double u = Fade(x); // calculate the fade curve for the X coordinate

    // return the blended results from the 4 vectors, scaled to be within [-1,1]. from a combination of empirical
    // analysis and mathematica tricks, the scaling factor below has been determined to be the best IEEE double approximation
    // of the scale factor, erring on the side that makes the range slightly smaller rather than larger, to guarantee
    // that the return value will not be out of bounds
    return Interpolate(Fade(y), Interpolate(u, Gradient(permutation[a&255],     x,   y),
                                               Gradient(permutation[b&255],     x-1, y)),
                                Interpolate(u, Gradient(permutation[(a+1)&255], x,   y-1),
                                               Gradient(permutation[(b+1)&255], x-1, y-1))) * 0.66173858276491038;
  }

  /// <summary>Generates three-dimensional Perlin noise based on the given position on the x, y, and z axes. The noise
  /// value will lie in the range [-1,1], and will repeat if <paramref name="x"/>, <paramref name="y"/>, or
  /// <paramref name="z"/> is outside the range [0,256).
  /// </summary>
  /// <include file="documentation.xml" path="/Math/CoherentNoise/AboutPerlin/*" />
  public double Perlin(double x, double y, double z)
  {
    int ix = FastFloor(x), iy = FastFloor(y), iz = FastFloor(z); // find the unit cube that contains the point
    x -= ix; // find x,y,z offset within that unit cube
    y -= iy;
    z -= iz;

    // compute hash codes for 8 vectors within the unit cube. these correspond to p[p[p[i]+j]+k] where
    // i={x,x+1}, j={y,y+1}, and k={z,z+1}
    int a = permutation[ix&255]+iy, aa = permutation[a&255]+iz, ab = permutation[(a+1)&255]+iz;
    int b = permutation[(ix+1)&255]+iy, ba = permutation[b&255]+iz, bb = permutation[(b+1)&255]+iz;

    double u = Fade(x), v = Fade(y); // calculate the fade curves for the x and y coordinates

    // return the blended results from the 8 vectors, scaled to be within [-1,1]. the scaling factor below has been determined
    // to be a close approximation of the correct scaling factor, but it is possible that it will cause a value to be
    // returned out of bounds
    return Interpolate(Fade(z), Interpolate(v, Interpolate(u, Gradient(permutation[aa&255], x,   y,   z),
                                                              Gradient(permutation[ba&255], x-1, y,   z)),
                                               Interpolate(u, Gradient(permutation[ab&255], x,   y-1, z),
                                                              Gradient(permutation[bb&255], x-1, y-1, z))),
                                Interpolate(v, Interpolate(u, Gradient(permutation[(aa+1)&255], x,   y,   z-1),
                                                              Gradient(permutation[(ba+1)&255], x-1, y,   z-1)),
                                               Interpolate(u, Gradient(permutation[(ab+1)&255], x,   y-1, z-1),
                                                              Gradient(permutation[(bb+1)&255], x-1, y-1, z-1))))
      * 0.965692;
  }

  /// <summary>Generates one-dimensional Simplex noise based on the given position on the x axis. The noise value will
  /// lie in the range [-1,1], and will begin to repeat if <paramref name="x"/> is outside the range [0,256).
  /// </summary>
  /// <include file="documentation.xml" path="/Math/CoherentNoise/AboutSimplex/*" />
  public double Simplex(double x)
  {
    const double DistanceCutoff = 1;
    int ix = FastFloor(x);
    x -= ix;

    double t = DistanceCutoff - x*x, sum;
    if(t < 0)
    {
      sum = 0;
    }
    else
    {
      t *= t;
      sum = t * t * Gradient(permutation[ix&255], x);
    }

    x -= 1;
    t = DistanceCutoff - x*x;
    if(t >= 0)
    {
      t *= t;
      sum += t * t * Gradient(permutation[(ix+1)&255], x);
    }

    // we scale by 16/81 because the function expands into a*(1-x*x)^4 + b*(1-(x-1)^2)^4, which has a maximum of 81/16
    // (when x = 1/2), so we multiply by the inverse to scale it down into [-1,1]
    return sum * (16.0/81);
  }

  /// <summary>Generates two-dimensional Simplex noise based on the given position on the x and y axes. The noise value
  /// will lie in the range [-1,1], and will repeat if <paramref name="x"/> or <paramref name="y"/> is outside the
  /// range [0,256).
  /// </summary>
  /// <include file="documentation.xml" path="/Math/CoherentNoise/AboutSimplex/*" />
  public double Simplex(double x, double y)
  {
    const double SkewFactor = 0.36602540378443864676;   // (sqrt(3) - 1) / 2
    const double UnskewFactor = 0.21132486540518711775; // (3 - sqrt(3)) / 6
    const double DistanceCutoff = 0.5;

    // skew the input coordinates to determine which cell of two simplices we are in
    double t = (x + y) * SkewFactor;
    int ix = FastFloor(x + t), iy = FastFloor(y + t);

    // unskew the cell origin back to simplex space and calculate the offsets from the cell origin
    t = (ix + iy) * UnskewFactor;
    x -= ix - t;
    y -= iy - t;

    // in the 2D case, simplices are equilateral triangles. we can determine which one we're in by comparing x and y
    int ixo, iyo; // offsets to the second corner of the simplex
    if(x > y) // we're in the lower triangle, with corner order (0,0), (1,0), (1,1)
    {
      ixo = 1;
      iyo = 0;
    }
    else // we're in the upper triangle, with corner order (0,0), (0,1), (1,1)
    {
      ixo = 0;
      iyo = 1;
    }

    double sum; // the sum of the contributions from the simplices

    // add in the first corner
    t = DistanceCutoff - x*x - y*y;
    if(t < 0)
    {
      sum = 0;
    }
    else
    {
      t  *= t;
      sum = t * t * Gradient(permutation[(ix + permutation[iy&255]) & 255], x, y);
    }

    // add the second corner
    double tx = x - ixo + UnskewFactor, ty = y - iyo + UnskewFactor;
    t = DistanceCutoff - tx*tx - ty*ty;
    if(t >= 0)
    {
      t   *= t;
      sum += t * t * Gradient(permutation[(ix + ixo + permutation[(iy + iyo) & 255]) & 255], tx, ty);
    }

    // add the third corner
    tx = x + (UnskewFactor*2-1);
    ty = y + (UnskewFactor*2-1);
    t = DistanceCutoff - tx*tx - ty*ty;
    if(t >= 0)
    {
      t   *= t;
      sum += t * t * Gradient(permutation[(ix + 1 + permutation[(iy + 1) & 255]) & 255], tx, ty);
    }

    return sum * 45.230614138179; // inexact, unscientific scaling factor
  }

  /// <summary>Generates three-dimensional Perlin noise based on the given position on the x, y, and z axes. The noise
  /// value will lie in the range [-1,1], and will repeat if <paramref name="x"/>, <paramref name="y"/>, or
  /// <paramref name="z"/> is outside the range [0,256).
  /// </summary>
  /// <include file="documentation.xml" path="/Math/CoherentNoise/AboutSimplex/*" />
  public double Simplex(double x, double y, double z)
  {
    const double SkewFactor   = 1.0/3; // (sqrt(4) - 1) / 2
    const double UnskewFactor = 1.0/6; // (4 - sqrt(4)) / 12
    const double DistanceCutoff = 0.6;

    // skew the input coordinates to determine which cell of six simplices we are in
    double t = (x + y + z) * SkewFactor;
    int ix = FastFloor(x + t), iy = FastFloor(y + t), iz = FastFloor(z + t);

    // unskew the cell origin back to simplex space and calculate the offsets from the cell origin
    t = (ix + iy + iz) * UnskewFactor;
    x -= ix - t;
    y -= iy - t;
    z -= iz - t;

    // determine which of the six simplices we're in
    int xo1, yo1, zo1, xo2, yo2, zo2; // offsets to the second and third corners of the simplex
    if(x < y)
    {
      if(y < z) // ZYX order
      {
        zo1 = yo2 = zo2 = 1;
        xo1 = yo1 = xo2 = 0;
      }
      else if(x < z) // YZX order
      {
        yo1 = yo2 = zo2 = 1;
        xo1 = zo1 = xo2 = 0;
      }
      else // YXZ order
      {
        yo1 = xo2 = yo2 = 1;
        xo1 = zo1 = zo2 = 0;
      }
    }
    else
    {
      if(y < z)
      {
        if(x < z) // ZXY order
        {
          zo1 = xo2 = zo2 = 1;
          xo1 = yo1 = yo2 = 0;
        }
        else // XZY order
        {
          xo1 = xo2 = zo2 = 1;
          yo1 = zo1 = yo2 = 0;
        }
      }
      else // XYZ order
      {
        xo1 = xo2 = yo2 = 1;
        yo1 = zo1 = zo2 = 0;
      }
    }

    double sum; // the sum of the contributions from the simplices

    // add in the first corner
    t = DistanceCutoff - x*x - y*y - z*z;
    if(t < 0)
    {
      sum = 0;
    }
    else
    {
      t *= t;
      sum = t * t * Gradient(permutation[(ix + permutation[(iy + permutation[iz&255]) & 255]) & 255], x, y, z);
    }

    // add the second corner
    double tx = x - xo1 + UnskewFactor, ty = y - yo1 + UnskewFactor, tz = z - zo1 + UnskewFactor;
    t = DistanceCutoff - tx*tx - ty*ty - tz*tz;
    if(t >= 0)
    {
      t   *= t;
      sum += t * t * Gradient(permutation[(ix + xo1 +
                                permutation[(iy + yo1 + permutation[(iz + zo1) & 255]) & 255]) & 255], tx, ty, tz);
    }

    // add the third corner
    tx = x - xo2 + 2*UnskewFactor;
    ty = y - yo2 + 2*UnskewFactor;
    tz = z - zo2 + 2*UnskewFactor;
    t = DistanceCutoff - tx*tx - ty*ty - tz*tz;
    if(t >= 0)
    {
      t   *= t;
      sum += t * t * Gradient(permutation[(ix + xo2 +
                                permutation[(iy + yo2 + permutation[(iz + zo2) & 255]) & 255]) & 255], tx, ty, tz);
    }

    // add the fourth corner
    tx = x + (3*UnskewFactor-1);
    ty = y + (3*UnskewFactor-1);
    tz = z + (3*UnskewFactor-1);
    t = DistanceCutoff - tx*tx - ty*ty - tz*tz;
    if(t >= 0)
    {
      t   *= t;
      sum += t * t * Gradient(permutation[(ix + 1 +
                                permutation[(iy + 1 + permutation[(iz + 1) & 255]) & 255]) & 255], tx, ty, tz);
    }

    return sum * 32.69599590511519; // inexact, unscientific scaling factor
  }

  /// <summary>Resets this <see cref="CoherentNoise"/> class to the default random seed.</summary>
  public void ResetToDefaultSeed()
  {
    permutation = defaultPermutation;
  }

  /// <summary>Randomizes the noise generated by this <see cref="CoherentNoise"/> class.</summary>
  public void Reseed()
  {
    Reseed(RandomNumberGenerator.CreateFastest());
  }

  /// <summary>Randomizes the noise generated by this <see cref="CoherentNoise"/> class based on the current state of
  /// the given random number generator.
  /// </summary>
  [CLSCompliant(false)]
  public void Reseed(RandomNumberGenerator rand)
  {
    if(permutation == null || permutation == defaultPermutation) permutation = new byte[256];
    for(int i=0; i<permutation.Length; i++) permutation[i] = (byte)i;
    Combinatorics.Permutations.RandomlyPermute(permutation, rand);
  }

  byte[] permutation = new byte[256];

  /// <summary>Applies a smoothing curve 6t^5-15t^4+10t^3 to the given value, which should be from 0 to 1.</summary>
  static double Fade(double t)
  {
    return t * t * t * (t * (t * 6 - 15) + 10);
  }

  /// <summary>Performs a floor calculation faster than Math.Floor().</summary>
  static int FastFloor(double n)
  {
    int i = (int)n;
    return n < 0 && n != i ? i-1 : i; // if is a negative integer, then returning i-1 would be wrong
  }

  /// <summary>Linearly interpolates between 'a' and 'b', based on 't', which should be from 0 to 1.</summary>
  static double Interpolate(double t, double a, double b) { return a + t * (b - a); }

  /// <summary>Returns the dot product of (x) with a pseudorandom vector of integer length from ±1 to ±16.</summary>
  static double Gradient(int hash, double x)
  {
    int u = (hash & 15) + 1;
    if((hash & 16) != 0) u = -u;
    return u * x;
  }

  /// <summary>Returns the dot product of (x,y) with a pseudorandom vector of length sqrt(5).</summary>
  static double Gradient(int hash, double x, double y)
  {
    // generate the dot product of (x,y) with one of (±1,±2) or (±2,±1). we can do this by swapping x and y
    // randomly, and taking the resulting dot product with (±1,±2).

    if((hash & 1) == 0)
    {
      double t = x;
      x = y;
      y = t;
    }

    return ((hash & 2) == 0 ? -x : x) + ((hash & 4) == 0 ? -2.0 : 2.0)*y;
  }

  static double Gradient(int hash, double x, double y, double z)
  {
    // convert low 4 bits of hash code into gradient value which is equal to one of the following:
    // x+y, -x+y, x-y, -x-y, x+z, -x+z, x-z, -x-z, y+z, -y+z, y-z, -y-z
    int h = hash & 15;
    double u = h < 8 ? x : y, v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
    return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
  }

  /// <summary>A permutation that I've found produces a nearly full range between -1 and 1 for 1, 2, and 3 dimensional
  /// Perlin noise. (TODO: But I'm not sure if that's what we want to optimize for...)
  /// </summary>
  static readonly byte[] defaultPermutation = new byte[256]
  {
    85, 180, 163, 216, 249, 170, 101, 245, 206, 142, 78, 232, 68, 183, 49, 131, 84, 124, 14, 212, 0, 20, 9, 92, 17,
    5, 30, 150, 164, 4, 96, 64, 61, 162, 83, 118, 211, 193, 148, 63, 205, 38, 73, 115, 160, 188, 172, 24, 58, 168,
    201, 203, 221, 195, 31, 66, 94, 123, 139, 127, 185, 225, 15, 223, 111, 89, 213, 190, 158, 19, 197, 176, 7, 76,
    82, 3, 99, 108, 36, 69, 16, 194, 166, 86, 47, 179, 140, 159, 71, 116, 121, 167, 141, 35, 171, 243, 93, 147, 113,
    28, 241, 247, 239, 146, 102, 130, 226, 228, 204, 149, 72, 98, 53, 8, 87, 254, 74, 42, 200, 214, 230, 91, 48, 165,
    26, 154, 44, 107, 70, 144, 210, 11, 23, 174, 237, 129, 106, 126, 152, 103, 227, 198, 120, 56, 25, 117, 242, 217,
    238, 80, 209, 235, 136, 81, 215, 10, 41, 119, 34, 224, 246, 33, 187, 79, 222, 175, 250, 189, 156, 67, 169, 184,
    90, 97, 251, 109, 52, 137, 143, 105, 54, 12, 252, 161, 114, 244, 192, 233, 134, 208, 207, 202, 39, 88, 77, 104,
    27, 155, 13, 40, 55, 151, 6, 234, 173, 32, 50, 75, 229, 145, 255, 253, 132, 138, 181, 57, 218, 199, 43, 248, 220,
    22, 112, 196, 21, 125, 177, 219, 18, 51, 62, 59, 231, 178, 45, 29, 133, 95, 182, 100, 46, 135, 110, 157, 191,
    240, 37, 122, 1, 2, 186, 236, 128, 60, 65, 153
  };
}

} // namespace AdamMil.Mathematics.Random