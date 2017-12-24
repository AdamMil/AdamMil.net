/*
AdamMil.Mathematics is a library that provides some useful mathematics classes
for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2016 Adam Milazzo

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
using System.IO;
using AdamMil.Utilities;
using BinaryReader=AdamMil.IO.BinaryReader;
using BinaryWriter=AdamMil.IO.BinaryWriter;

// TODO: move these to AdamMil.Utilities? but then we might have a circular references unless we also moved BinaryReader & BinaryWriter...

namespace AdamMil.Mathematics.Random
{

#region CollectionExtensions
/// <summary>Implements useful extensions for .NET built-in collections.</summary>
public static class CollectionExtensions
{
  /// <summary>Returns a random item from the list.</summary>
  public static T SelectRandom<T>(this IList<T> list, RandomNumberGenerator random)
  {
    if(list == null || random == null) throw new ArgumentNullException();
    if(list.Count == 0) throw new ArgumentException("The collection is empty.");
    return list[random.Next(list.Count)];
  }
}
#endregion

#region RandomNumberGenerator
/// <summary>Provides a base class for random number generators. It supports generating uniformly random bytes, bits, integers,
/// and floating-point numbers, as well as normally (Gaussian) and exponentially distributed values.
/// </summary>
/// <remarks>
/// Comparison of included RNGs (speed is a factor relative to XorShift128, so 0.5 means half the speed of XorShift128):
/// <list type="table">
/// <listheader>
///   <term>RNG</term>
///   <description>Speed (relative)</description>
///   <description>Memory usage</description>
///   <description>Period</description>
///   <description>Randomness</description>
///   <description>Usage notes</description>
/// </listheader>
/// <item>
///   <term>AWCKISS</term>
///   <description>0.63</description>
///   <description>Minimal</description>
///   <description>2^121</description>
///   <description>Good</description>
///   <description>Faster than KISS</description>
/// </item>
/// <item>
///   <term>ISAAC</term>
///   <description>0.36</description>
///   <description>2 kb</description>
///   <description>Depends on seed (2^8295 average, 2^40 minimum)</description>
///   <description>Best</description>
///   <description>Best randomness. Significant startup time</description>
/// </item>
/// <item>
///   <term>KISS</term>
///   <description>0.48</description>
///   <description>Minimal</description>
///   <description>2^125</description>
///   <description>Great</description>
///   <description>Good general purpose RNG. Default RNG</description>
/// </item>
/// <item>
///   <term>MWC256</term>
///   <description>0.54</description>
///   <description>1 kb</description>
///   <description>2^8222</description>
///   <description>Good</description>
///   <description>Huge period. Significant startup time</description>
/// </item>
/// <item>
///   <term>XorShift128</term>
///   <description>1.00</description>
///   <description>Minimal</description>
///   <description>2^128</description>
///   <description>Okay</description>
///   <description>Fastest</description>
/// </item>
/// </list>
/// </remarks>
[Serializable]
public abstract class RandomNumberGenerator
{
  /// <summary>Generates and returns the given number of random bytes.</summary>
  public byte[] GenerateBytes(int byteCount)
  {
    if(byteCount < 0) throw new ArgumentOutOfRangeException();
    byte[] bytes = new byte[byteCount];
    GenerateBytes(bytes, 0, byteCount);
    return bytes;
  }

  /// <summary>Fills the given array with random bytes.</summary>
  public void GenerateBytes(byte[] bytes)
  {
    if(bytes == null) throw new ArgumentNullException();
    GenerateBytes(bytes, 0, bytes.Length);
  }

  /// <summary>Fills a region of an array with random bytes.</summary>
  /// <remarks>The default implementation passes the array to <see cref="GenerateBytes(void*,int)"/>.</remarks>
  public virtual unsafe void GenerateBytes(byte[] bytes, int index, int count)
  {
    Utility.ValidateRange(bytes, index, count);
    fixed(byte* pBytes = bytes) GenerateBytes(pBytes+index, count);
  }

  /// <summary>Fills a region of memory with random bytes.</summary>
  /// <param name="pBytes">A pointer to the memory region to fill.</param>
  /// <param name="count">The number of bytes to fill.</param>
  /// <remarks>The default implementation generates 32-bit integers with <see cref="NextUInt32"/> and uses them to fill up to
  /// four bytes at a time within the array.
  /// </remarks>
  [CLSCompliant(false)]
  public virtual unsafe void GenerateBytes(void* pBytes, int count)
  {
    if(count != 0)
    {
      if(pBytes == null) throw new ArgumentNullException();
      if(count < 0) throw new ArgumentOutOfRangeException();

      // first, align the pointer
      byte* p = (byte*)pBytes;
      if(((int)p & 3) != 0) // if it's misaligned...
      {
        uint chunk = NextBits(Math.Min(count, (4-((int)p&3))) * 8);
        do
        {
          *p++ = (byte)chunk;
          chunk >>= 8;
        } while(--count != 0 && ((int)p & 3) != 0);
      }

      int chunks = count / 4; // do as many bytes as we can in 4-byte chunks
      for(; chunks != 0; p += 4, chunks--) *(uint*)p = NextUInt32();

      count &= 3; // get the number of remaining bytes
      if(count != 0)
      {
        uint chunk = NextBits(count*8);
        do
        {
          *p++ = (byte)chunk;
          chunk >>= 8;
        } while(--count != 0);
      }
    }
  }

  /// <summary>Returns a byte array containing the internal state of the random number generator. This array can later be passed
  /// to <see cref="SetState"/> to restore the generator state, assuming it is passed to a generator of the same type.
  /// </summary>
  public byte[] GetState()
  {
    using(MemoryStream ms = new MemoryStream())
    {
      SaveState(ms);
      return ms.ToArray();
    }
  }

  /// <summary>Loads the generator state from a stream.</summary>
  public void LoadState(Stream stream)
  {
    using(BinaryReader reader = new BinaryReader(stream, false)) LoadState(reader);
  }

  /// <summary>Loads the generator state from a <see cref="BinaryReader"/>.</summary>
  public void LoadState(BinaryReader reader)
  {
    if(reader == null) throw new ArgumentNullException();
    bitsInBuffer = reader.ReadByte();
    bitBuffer    = bitsInBuffer == 0 ? 0 : reader.ReadUInt32();
    LoadStateCore(reader);
  }

  /// <summary>Saves the generator state to a stream.</summary>
  public void SaveState(Stream stream)
  {
    using(BinaryWriter writer = new BinaryWriter(stream, false)) SaveState(writer);
  }

  /// <summary>Saves the generator state to a <see cref="BinaryWriter"/>.</summary>
  public void SaveState(BinaryWriter writer)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.Write(bitsInBuffer);
    if(bitsInBuffer != 0) writer.Write(bitBuffer);
    SaveStateCore(writer);
  }

  /// <summary>Restores the generator state from an array retrieved by calling <see cref="GetState"/>.</summary>
  public void SetState(byte[] state)
  {
    using(MemoryStream ms = new MemoryStream(state)) LoadState(ms);
  }

  /// <summary>Generates and returns a random non-negative integer.</summary>
  public int Next()
  {
    return (int)(NextUInt32()>>1); // we could call GetBits(31) but perhaps the overhead of managing the bit buffer would outweigh the
  }                                // benefit of 3% fewer calls to NextUInt32(), since many of the generators are quite fast

  /// <summary>Generates and returns a random integer less than the given maximum, which must be positive.</summary>
  /// <remarks>If <paramref name="exclusiveMaximum"/> is a power of two, it's much more efficient to use <see cref="NextBits"/>, if you
  /// can easily obtain the base-2 logarithm of <paramref name="exclusiveMaximum"/>.
  /// </remarks>
  public int Next(int exclusiveMaximum)
  {
    if(exclusiveMaximum <= 1<<23) // if we need no more than 23 bits of randomness...
    {
      if(exclusiveMaximum <= 0) throw new ArgumentOutOfRangeException();
      return (int)(NextFloat() * exclusiveMaximum); // use NextFloat, which provides 23 bits of randomness and is usually faster
    }
    else
    {
      return (int)(NextDouble() * exclusiveMaximum); // otherwise use NextDouble, which provides 52 bits of randomness
    }
  }

  /// <summary>Generates and returns a random integer between the given inclusive minimum and maximum, which may be any integers.</summary>
  /// <remarks>If <paramref name="maximum"/>-<paramref name="minimum"/> is a power of two, it's much more efficient to use
  /// <see cref="NextBits"/>.
  /// </remarks>
  public int Next(int minimum, int maximum)
  {
    if(minimum > maximum) throw new ArgumentException("The minimum must be less than or equal to the maximum.");
    return (int)(NextDouble() * (uint)(maximum - minimum)) + minimum; // maximum - minimum can be up to 2^32-1, so it fits in a uint
  }

  /// <summary>Generates a number of random bits and returns them in the low order bits of an integer.</summary>
  /// <param name="bits">The number of bits to generate, from 0 to 32.</param>
  /// <remarks>This method can be used to efficiently generate small random numbers in the range of a power of two. For instance, by
  /// passing 3 you get a small random number from 0 to 7 (i.e. 0 to 2^3-1) much more efficiently than calling <see cref="Next(int)"/>.
  /// This method uses <see cref="NextUInt32"/> to generate batches of 32 bits at a time.
  /// </remarks>
  [CLSCompliant(false)]
  public uint NextBits(int bits)
  {
    uint value;
    if((uint)bits <= bitsInBuffer)
    {
      value        = bitBuffer & ((1u<<bits)-1);
      bitBuffer  >>= bits;
      bitsInBuffer = (byte)(bitsInBuffer - bits); // mcs generates slightly better code for this than bitsInBuffer -= (byte)bits
    }
    else if((uint)bits < 32)
    {
      bits -= bitsInBuffer; // get as many bits as we can from the buffer
      value = bitBuffer;
      bitBuffer = NextUInt32(); // then refill the buffer
      value = (value<<bits) | (bitBuffer & ((1u<<bits)-1)); // and grab the remaining bits from it
      bitBuffer  >>= bits;
      bitsInBuffer = (byte)(32 - bits);
    }
    else if(bits == 32)
    {
      value = NextUInt32();
    }
    else // bits < 0 || bits > 32
    {
      throw new ArgumentOutOfRangeException();
    }
    return value;
  }

  /// <summary>Generates and returns a random boolean value.</summary>
  /// <remarks>This method uses <see cref="NextUInt32"/> to generate batches of 32 bits at a time.</remarks>
  public bool NextBoolean()
  {
    if(bitsInBuffer == 0)
    {
      bitBuffer    = NextUInt32();
      bitsInBuffer = 32;
    }

    bool result = (bitBuffer & 1) != 0;
    bitBuffer >>= 1;
    bitsInBuffer--;
    return result;
  }

  /// <summary>Generates and returns a random double greater than or equal to zero and less than one.</summary>
  /// <remarks><note type="inherit">The default implementation uses <see cref="NextUInt32"/> and <see cref="NextBits"/> to generate a
  /// double with the full 52 bits of randomness. If you override this method (perhaps because your random number generator natively
  /// generates floating point values), it is important that this method be capable of returning a sufficient number of different values.
  /// Ideally this method should be able to return all 2^52 different values, but at a minimum it must be able to return 2^32 different
  /// values. The values must also be uniformly distributed.
  /// </note></remarks>
  public virtual unsafe double NextDouble()
  {
    double n;
    *(uint*)&n     = NextUInt32(); // adapted from http://www.cs.ucl.ac.uk/staff/d.jones/GoodPracticeRNG.pdf
    *((uint*)&n+1) = NextBits(20) | (1023u<<20);
    return n - 1;
  }

  /// <summary>Generates and returns a random float greater than or equal to zero and less than one.</summary>
  /// <remarks><note type="inherit">The default implementation uses <see cref="NextBits"/> to generate a float with the full 23 bits of
  /// randomness. If you override this method (perhaps because your random number generator natively generates floating point values), it
  /// must be able to return all 2^23 different values. The values must also be uniformly distributed.
  /// </note></remarks>
  public virtual unsafe float NextFloat()
  {
    uint n = NextBits(23) | (127u<<23); // adapted from http://www.cs.ucl.ac.uk/staff/d.jones/GoodPracticeRNG.pdf
    return *(float*)&n - 1;
  }

  /// <summary>Returns a value from the exponential distribution with rate 1.</summary>
  public double NextExponential()
  {
    while(true) // we'll use Marsaglia's ziggurat algorithm with a table size of 64. a larger table would make this faster,
    {           // but it's pretty fast with 64
      uint n = NextBits(6), i = n&63; // select a layer of the ziggurat
      double x = expTables.xs[i] * NextDouble(); // generate a value from that layer
      if(x < expTables.xs[i+1]) return x; // if the value definitely lies under the curve, we're done. it'll exit here ~92.6% of the time

      if(i == 0) // if we selected the bottom layer of the ziggurat, then we need to select from the infinite tail of the distribution
      {
        return x + NextExponential(); // since the tail has the same shape as the rest, we can just call the method recursively
      }
      else // if we selected a layer of the ziggurat that has a finite width, check to see whether x lies under the curve
      {
        double y = expTables.xs[i-1]; // generate a high-resolution Y value
        y = (expTables.xs[i]-y)*NextDouble() + y;
        if(y < Math.Exp(-x)) return x; // if y < f(x), i.e. if the random y is below the curve at x, then return x
      }
    }
  }

  /// <summary>Returns a value from the exponential distribution with the given rate, which should be positive.</summary>
  public double NextExponential(double rate)
  {
    return NextExponential() / rate;
  }

  /// <summary>Returns a value from the normal distribution with mean 0 and standard deviation 1.</summary>
  public double NextNormal()
  {
    double x;
    bool negate;
    while(true) // we'll use Marsaglia's ziggurat algorithm with a table size of 64. a larger table would make this faster,
    {           // but it's pretty fast with 64
      uint n = NextBits(7), i = n&63; // select a layer of the ziggurat
      negate = (n&64) != 0;
      x = normalTables.xs[i] * NextDouble(); // generate a value from that layer
      if(x < normalTables.xs[i+1]) break; // if the value definitely lies under the curve, we're done. it'll exit here 94.96% of the time

      if(i == 0) // if we selected the bottom layer of the ziggurat, then we need to select from the infinite tail of the distribution
      {
        while(true) // we'll use Marsaglia's recommended method
        {
          x = NextDouble();
          double y = NextDouble();
          x = -Math.Log(x) / normalTables.xs[1]; // x or y will become Infinity if either is 0, but that should be okay
          y = -Math.Log(y);
          if(y+y > x*x) break;
        }
        x += normalTables.xs[1];
        break;
      }
      else // if we selected a layer of the ziggurat that has a finite width, check to see whether x lies under the curve
      {
        double y = normalTables.xs[i-1]; // generate a high-resolution Y value
        y = (normalTables.xs[i]-y)*NextDouble() + y;
        if(y < Math.Exp(x*x * -0.5)) break; // if y < f(x), i.e. if the random y is below the curve at x, then return x
      }
    }

    if(negate) return -x;
    else return x;
  }

  /// <summary>Returns a value from the normal distribution with the given mean and standard deviation. The standard deviation should be
  /// non-negative.
  /// </summary>
  public double NextNormal(double mean, double stdDev)
  {
    return NextNormal()*stdDev + mean;
  }

  #pragma warning disable 3011 // only CLS compliant methods can be abstract
  // we're going to lie about the CLS compliance of this class because it's used everywhere, and is still very useable by non-CLS-compliant
  // languages. it's only that people may not be able to create their own random number generators, which is no problem for most users
  /// <include file="documentation.xml" path="//Math/RNG/NextUint32/node()"/>
  [CLSCompliant(false)]
  public abstract uint NextUInt32();
  #pragma warning restore 3011

  /// <summary>Generates and returns a random 64-bit unsigned integer.</summary>
  /// <remarks><note type="inherit">The default implementation combines two 32-bit numbers returned from <see cref="NextUInt32"/> into a
  /// single 64-bit number. If the generator implementation is natively capable of generating a 64-bit output, you may want to override
  /// this method to make use of that ability.
  /// </note></remarks>
  [CLSCompliant(false)]
  public virtual unsafe ulong NextUInt64()
  {
    ulong n;
    *(uint*)&n     = NextUInt32();
    *((uint*)&n+1) = NextUInt32();
    return n;
  }

  /// <summary>Creates a new <see cref="RandomNumberGenerator"/> of the all-around best general-purpose type in the library.</summary>
  public static RandomNumberGenerator CreateDefault()
  {
    return new KISSRNG();
  }

  /// <summary>Creates a new <see cref="RandomNumberGenerator"/> of the all-around best general-purpose type in the library.</summary>
  [CLSCompliant(false)]
  public static RandomNumberGenerator CreateDefault(params uint[] seed)
  {
    return new KISSRNG(seed);
  }

  /// <summary>Creates a new <see cref="RandomNumberGenerator"/> of the fastest type available in the library.</summary>
  public static RandomNumberGenerator CreateFastest()
  {
    return new XorShift128RNG();
  }

  /// <summary>Creates a new <see cref="RandomNumberGenerator"/> of the fastest type available in the library.</summary>
  [CLSCompliant(false)]
  public static RandomNumberGenerator CreateFastest(params uint[] seed)
  {
    return new XorShift128RNG(seed);
  }

  /// <include file="documentation.xml" path="//Math/RNG/LoadStateCore/node()"/>
  protected abstract void LoadStateCore(BinaryReader reader);

  /// <include file="documentation.xml" path="//Math/RNG/SaveStateCore/node()"/>
  protected abstract void SaveStateCore(BinaryWriter writer);

  /// <summary>Returns a 128-bit seed based on the current time (both real-world time and an internal timer), as an array of four uints.</summary>
  /// <remarks>Calling this method rapidly from a single thread will result in different seeds even if no measurable time has elapsed
  /// between the calls.
  /// </remarks>
  [CLSCompliant(false)]
  protected static uint[] MakeTimeBasedSeed()
  {
    if(timer == null) timer = System.Diagnostics.Stopwatch.StartNew();
    long dateTicks = DateTime.Now.Ticks, timerTicks = timer.ElapsedTicks;
    // add constants to the timer tick values to prevent them from being too small (especially zero) shortly after startup. the timer isn't
    // thread-safe, but it shouldn't crash and we don't really need accurate timing, only rapid change in value. incrementing seedIncrement
    // isn't thread-safe either, but it'll still advance and it's intended for the case of a tight loop on a single thread anyway
    return new uint[]
    {
      (uint)timerTicks+123456789, (uint)(timerTicks>>32)+678912345, (uint)(dateTicks>>32)+seedIncrement++, (uint)dateTicks
    };
  }

  #region ZigguratTables
  /// <summary>Computes a table for Marsaglia's ziggurat algorithm with 64 layers.</summary>
  struct ZigguratTables
  {
    /// <summary>Initializes ziggurat tables with a size of 64.</summary>
    /// <param name="x">The x value of the right edge of the bottom layer.</param>
    /// <param name="area">The area of each layer.</param>
    /// <param name="f">The function that the ziggurat should build tables for.</param>
    /// <param name="invf">The inverse of <paramref name="f"/>.</param>
    public ZigguratTables(FP107 x, FP107 area, Func<FP107, FP107> f, Func<FP107, FP107> invf)
    {
      xs = new double[65];
      ys = new double[64];

      // we compute the tables in high precision using FP107 to avoid rounding error
      FP107 y = f(x); // the top of the base layer and the bottom of the next
      xs[0] = (double)(area/y); // make xs[1]/xs[0] be the width of the rectangular proportion of the bottom layer that's under the curve
      ys[0] = (double)y;
      for(int i=1; i<ys.Length; i++)
      {
        xs[i] = (double)x;
        y += area / x; // add the height of the layer to get the bottom of the next layer
        ys[i] = (double)y;
        x = invf(y); // compute the X coordinate at the right edge of the next layer
      }
    }

    public readonly double[] xs, ys;
  }
  #endregion

  uint bitBuffer;
  byte bitsInBuffer;

  static System.Diagnostics.Stopwatch timer;
  static uint seedIncrement;

  // to compute the initial value for x, assuming a table size of 64, minimize g(x) := abs(f(0) - gr(f(x), 64, x*f(x)*tf(x))) where
  // gr(y, n, area) := n <= 1 ? y : gr(y+area/invf(y), n-1, area) and tf(x) computes the area of the tail > x. for the normal distribution,
  // tf(x) := sqrt(pi/2)*erfc(x/sqrt(2)). for the exponential distribution, tf(x) = f(x). the value for the area is x*f(x)*tf(x)
  static readonly ZigguratTables expTables =
    new ZigguratTables(FP107.FromComponents(6.07882124685676, -2.1720962841354067E-16),
                       FP107.FromComponents(0.016216697728895255, -6.3099203272486121E-19),
                       x => FP107.Exp(-x), y => FP107.Log(1/y));

  static readonly ZigguratTables normalTables =
    new ZigguratTables(FP107.FromComponents(3.2136576271588955, 1.0131948939415194E-17),
                       FP107.FromComponents(0.020024457157351693, 9.173627566703589E-19),
                       x => FP107.Exp(x.Square() * -0.5), y => FP107.Sqrt(FP107.Log(y) * -2));
}
#endregion

#region AWCKISSRNG
/// <summary>Implements a KISS-like generator following a proposal by George Marsaglia for a faster but poorer-quality
/// alternative to the full <see cref="KISSRNG">KISS generator</see>. The multiply-with-carry component of the KISS generator is
/// replaced with an add-with-carry component. The result is a very fast random number generator that generates numbers with a
/// constant time per number and uses very little memory, while maintaining fairly good quality. It has a period of about 2^121.
/// </summary>
[Serializable]
public sealed class AWCKISSRNG : RandomNumberGenerator
{
  // adapted from http://www.cs.ucl.ac.uk/staff/d.jones/GoodPracticeRNG.pdf

  /// <include file="documentation.xml" path="//Math/RNG/SeedSize/node()"/>
  public const int SeedSize = 4;

  /// <summary>Initializes a new <see cref="AWCKISSRNG"/> random number generator with a seed based on the current time.</summary>
  public AWCKISSRNG() : this(MakeTimeBasedSeed()) { }

  /// <summary>Initializes a new <see cref="AWCKISSRNG"/> random number generator with the given seed (from which up to 4
  /// unsigned integers are used). If <paramref name="seed"/> is null or empty, a constant, default seed will be used. Note that
  /// the seed array should not contain zeros, as these may be ignored.
  /// </summary>
  [CLSCompliant(false)]
  public AWCKISSRNG(params uint[] seed)
  {
    X = 123456789;
    Y = 234567891;
    Z = 345678912;
    W = 456789123;

    if(seed != null)
    {
      if(seed.Length != 0) X = seed[0];
      if(seed.Length > 1 && seed[1] != 0) Y = seed[1]; // avoid Y = 0
      if(seed.Length > 2) Z = seed[2];
      if(seed.Length > 3 && (Z != 0 || seed[3] != 0)) W = seed[3]; // avoid Z = W = 0
    }
  }

  /// <include file="documentation.xml" path="//Math/RNG/NextUint32/node()"/>
  [CLSCompliant(false)]
  public override uint NextUInt32()
  {
    X += 1411392427;

    Y ^= Y<<5;
    Y ^= Y>>7;
    Y ^= Y<<22;

    uint t = Z + W + C;
    Z  = W;
    C  = t >> 31; // C = (int)t < 0 ? 1 : 0
    W  = t & 0x7FFFFFFF;

    return X + Y + W;
  }

  /// <include file="documentation.xml" path="//Math/RNG/LoadStateCore/node()"/>
  protected override void LoadStateCore(BinaryReader reader)
  {
    X = reader.ReadUInt32();
    Y = reader.ReadUInt32();
    Z = reader.ReadUInt32();
    W = reader.ReadUInt32();
    C = reader.ReadUInt32();
  }

  /// <include file="documentation.xml" path="//Math/RNG/SaveStateCore/node()"/>
  protected override void SaveStateCore(BinaryWriter writer)
  {
    writer.Write(X);
    writer.Write(Y);
    writer.Write(Z);
    writer.Write(W);
    writer.Write(C);
  }

  uint X, Y, Z, W, C;
}
#endregion

#region ISAACRNG
/// <summary>Implements the ISAAC random number generator. The generator was designed for cryptography and is very high quality,
/// but is also quite fast. However, it generates results in large batches, so the time needed to get a result is very uneven,
/// depending on whether a new batch needs to be generated. It requires about 2 kb of memory.
/// </summary>
/// <remarks><note type="caution">Note that although ISAAC was designed for cryptography, it is only secure if initialized with a secure
/// seed. A secure seed is an array of 256 cryptographically random uint values. The default constructor does not seed the generator
/// securely, since it uses a small seed based on the current time.</note></remarks>
[Serializable]
public sealed class ISAACRNG : RandomNumberGenerator
{
  // adapted from http://burtleburtle.net/bob/rand/isaacafa.html

  /// <include file="documentation.xml" path="//Math/RNG/SeedSize/node()"/>
  public const int SeedSize = 256;

  /// <summary>Initializes a new <see cref="ISAACRNG"/> with a seed based on the current time.</summary>
  public ISAACRNG() : this(MakeTimeBasedSeed()) { }

  /// <summary>Initializes a new <see cref="ISAACRNG"/> with the given seed (from which up to 256 unsigned integers are
  /// used). If <paramref name="seed"/> is null, a constant, default seed will be used.
  /// </summary>
  [CLSCompliant(false)]
  public ISAACRNG(params uint[] seed)
  {
    results = new uint[Size];
    state   = new uint[Size];
    if(seed != null) Array.Copy(seed, results, Math.Min(Size, seed.Length));
    Initialize(seed != null);
  }

  /// <include file="documentation.xml" path="//Math/RNG/NextUint32/node()"/>
  [CLSCompliant(false)]
  public override uint NextUInt32()
  {
    if(resultIndex == Size) Isaac();
    return results[resultIndex++];
  }

  /// <include file="documentation.xml" path="//Math/RNG/LoadStateCore/node()"/>
  protected override void LoadStateCore(BinaryReader reader)
  {
    results     = reader.ReadUInt32s(256);
    state       = reader.ReadUInt32s(256);
    resultIndex = reader.ReadInt32();
    accumulator = reader.ReadUInt32();
    lastResult  = reader.ReadUInt32();
    counter     = reader.ReadUInt32();
  }

  /// <include file="documentation.xml" path="//Math/RNG/SaveStateCore/node()"/>
  protected override void SaveStateCore(BinaryWriter writer)
  {
    writer.Write(results);
    writer.Write(state);
    writer.Write(resultIndex);
    writer.Write(accumulator);
    writer.Write(lastResult);
    writer.Write(counter);
  }

  const int LogSize = 8, Size = 1<<LogSize, Mask = (Size-1)<<2;

  void DoInitializationRound(uint[] array, uint i, ref uint a, ref uint b, ref uint c, ref uint d,
                             ref uint e, ref uint f, ref uint g, ref uint h)
  {
    if(array != null)
    {
      a+=array[i];   b+=array[i+1]; c+=array[i+2]; d+=array[i+3];
      e+=array[i+4]; f+=array[i+5]; g+=array[i+6]; h+=array[i+7];
    }
    Shuffle(ref a, ref b, ref c, ref d, ref e, ref f, ref g, ref h);
    state[i]=a;   state[i+1]=b; state[i+2]=c; state[i+3]=d;
    state[i+4]=e; state[i+5]=f; state[i+6]=g; state[i+7]=h;
  }

  void DoIsaacRound(ref uint iref, ref uint jref)
  {
    uint i=iref, j=jref, x, y;

    x = state[i];
    accumulator ^= accumulator << 13;
    accumulator += state[j++];
    state[i] = y = state[(x&Mask)>>2] + accumulator + lastResult;
    results[i++] = lastResult = state[((y>>LogSize)&Mask)>>2] + x;

    x = state[i];
    accumulator ^= accumulator >> 6;
    accumulator += state[j++];
    state[i] = y = state[(x&Mask)>>2] + accumulator + lastResult;
    results[i++] = lastResult = state[((y>>LogSize)&Mask)>>2] + x;

    x = state[i];
    accumulator ^= accumulator << 2;
    accumulator += state[j++];
    state[i] = y = state[(x&Mask)>>2] + accumulator + lastResult;
    results[i++] = lastResult = state[((y>>LogSize)&Mask)>>2] + x;

    x = state[i];
    accumulator ^= accumulator >> 16;
    accumulator += state[j++];
    state[i] = y = state[(x&Mask)>>2] + accumulator + lastResult;
    results[i++] = lastResult = state[((y>>LogSize)&Mask)>>2] + x;

    iref = i;
    jref = j;
  }

  void Initialize(bool hasSeed)
  {
    uint i, a, b, c, d, e, f, g, h;
    a = b = c = d = e = f = g = h = 0x9e3779b9;

    for(i=0; i<4; i++) Shuffle(ref a, ref b, ref c, ref d, ref e, ref f, ref g, ref h);

    uint[] seedArray = hasSeed ? results : null;
    for(i=0; i<Size; i+=8) DoInitializationRound(seedArray, i, ref a, ref b, ref c, ref d, ref e, ref f, ref g, ref h);

    if(hasSeed)
    {
      for(i=0; i<Size; i+=8) DoInitializationRound(state, i, ref a, ref b, ref c, ref d, ref e, ref f, ref g, ref h);
    }

    Isaac();
  }

  void Isaac()
  {
    lastResult += ++counter;

    uint i, j;
    for(i=0, j=Size/2; i<Size/2;) DoIsaacRound(ref i, ref j);
    for(j=0; j<Size/2;) DoIsaacRound(ref i, ref j);
    resultIndex = 0;
  }

  uint[] results, state;
  int resultIndex;
  uint accumulator, lastResult, counter;

  static void Shuffle(ref uint a, ref uint b, ref uint c, ref uint d, ref uint e, ref uint f, ref uint g, ref uint h)
  {
    a^=b<<11; d+=a; b+=c;
    b^=c>>2;  e+=b; c+=d;
    c^=d<<8;  f+=c; d+=e;
    d^=e>>16; g+=d; e+=f;
    e^=f<<10; h+=e; f+=g;
    f^=g>>4;  a+=f; g+=h;
    g^=h<<8;  b+=g; h+=a;
    h^=a>>9;  c+=h; a+=b;
  }
}
#endregion

#region KISSRNG
/// <summary>Implements the KISS random number generator by George Marsaglia, which is a simple and quite fast random number
/// generator that is nonetheless high quality. It is roughly equal to <see cref="ISAACRNG" /> in speed and randomness (but is
/// not cryptographically secure). More importantly, it generates random numbers using a constant amount of time per number,
/// rather than generating them in batches. It also uses very little memory and has a period greater than 2^125.
/// <see cref="AWCKISSRNG"/> is somewhat faster than KISS, but also somewhat poorer quality.
/// </summary>
[Serializable]
public sealed class KISSRNG : RandomNumberGenerator
{
  // adapted from http://www.cs.ucl.ac.uk/staff/d.jones/GoodPracticeRNG.pdf

  /// <include file="documentation.xml" path="//Math/RNG/SeedSize/node()"/>
  public const int SeedSize = 4;

  /// <summary>Initializes a new <see cref="KISSRNG"/> with a seed based on the current time.</summary>
  public KISSRNG() : this(MakeTimeBasedSeed()) { }

  /// <summary>Initializes a new <see cref="KISSRNG"/> with the given seed (from which up to 4 unsigned integers are
  /// used). If <paramref name="seed"/> is null or empty, a constant, default seed will be used. Note that zeroes in the seed
  /// array may be ignored.
  /// </summary>
  [CLSCompliant(false)]
  public KISSRNG(params uint[] seed)
  {
    X = 123456789;
    Y = 362436000;
    Z = 521288629;
    C = 7654321;

    if(seed != null)
    {
      if(seed.Length != 0) X = seed[0];
      if(seed.Length > 1 && seed[1] != 0) Y = seed[1]; // avoid Y = 0
      if(seed.Length > 2) Z = seed[2];
      if(seed.Length > 3) C = seed[3] % 698769068 + 1; // C should be less than 698769069. add 1 to avoid Z = C = 0
    }
  }

  /// <include file="documentation.xml" path="//Math/RNG/NextUint32/node()"/>
  [CLSCompliant(false)]
  public override uint NextUInt32()
  {
    X  = 69069*X + 12345;
    Y ^= Y<<13;
    Y ^= Y>>17;
    Y ^= Y<<5;

    ulong t = (ulong)698769069*Z + C;
    C = (uint)(t>>32);
    Z = (uint)t;
    return X + Y + Z;
  }

  /// <include file="documentation.xml" path="//Math/RNG/LoadStateCore/node()"/>
  protected override void LoadStateCore(BinaryReader reader)
  {
    X = reader.ReadUInt32();
    Y = reader.ReadUInt32();
    Z = reader.ReadUInt32();
    C = reader.ReadUInt32();
  }

  /// <include file="documentation.xml" path="//Math/RNG/SaveStateCore/node()"/>
  protected override void SaveStateCore(BinaryWriter writer)
  {
    writer.Write(X);
    writer.Write(Y);
    writer.Write(Z);
    writer.Write(C);
  }

  uint X, Y, Z, C;
}
#endregion

#region MWC256RNG
/// <summary>Implements the MWC256 random number generator by George Marsaglia. The generator is quite fast with good randomness,
/// and generates numbers in a constant time per number, but uses just over 1 kb of memory. The primary benefit of this generator
/// is its huge period of 2^8222 -- more than you could ever use.
/// </summary>
[Serializable]
public sealed class MWC256RNG : RandomNumberGenerator
{
  // adapted from http://www.cs.ucl.ac.uk/staff/d.jones/GoodPracticeRNG.pdf

  /// <include file="documentation.xml" path="//Math/RNG/SeedSize/node()"/>
  public const int SeedSize = 256;

  /// <summary>Initializes a new <see cref="MWC256RNG"/> random number generator with a seed based on the current time.</summary>
  public MWC256RNG() : this(MakeTimeBasedSeed()) { }

  /// <summary>Initializes a new <see cref="MWC256RNG"/> random number generator with the given seed (from which up to 256
  /// unsigned integers are used). If <paramref name="seed"/> is null, a constant, default seed will be used. The elements of the
  /// seed array should be random numbers. If at least 256 seed numbers are given, they will be directly used as the generator's
  /// internal state. Otherwise, the seed will be used to initialize another random number generator, which in turn will be used
  /// to initialize the generator's internal state.
  /// </summary>
  [CLSCompliant(false)]
  public MWC256RNG(params uint[] seed)
  {
    Q = new uint[256];
    C = 362436;
    I = 255;

    if(seed != null && seed.Length >= Q.Length)
    {
      Array.Copy(seed, Q, Q.Length);
    }
    else
    {
      // fill the elements with random numbers from the KISSRNG generator (the best overall choice from the other RNGs)
      KISSRNG rng = new KISSRNG(seed);
      for(int i=0; i<Q.Length; i++) Q[i] = rng.NextUInt32();
    }
  }

  /// <include file="documentation.xml" path="//Math/RNG/NextUint32/node()"/>
  [CLSCompliant(false)]
  public override uint NextUInt32()
  {
    I++;
    ulong t = (ulong)809430660*Q[I] + C;
    C = (uint)(t>>32);
    return Q[I] = (uint)t;
  }

  /// <include file="documentation.xml" path="//Math/RNG/LoadStateCore/node()"/>
  protected override void LoadStateCore(BinaryReader reader)
  {
    Q = reader.ReadUInt32s(256);
    C = reader.ReadUInt32();
    I = reader.ReadByte();
  }

  /// <include file="documentation.xml" path="//Math/RNG/SaveStateCore/node()"/>
  protected override void SaveStateCore(BinaryWriter writer)
  {
    writer.Write(Q);
    writer.Write(C);
    writer.Write(I);
  }

  uint[] Q;
  uint C;
  byte I;
}
#endregion

#region XorShift128RNG
/// <summary>Implements the 128-bit xorshift random number generator, by George Marsaglia. This is the fastest random number
/// generator in the library, but the lowest quality (although the quality is still quite respectable). It generates numbers in a
/// constant amount of time, uses a very small amount of memory, and has a period of about 2^128.
/// </summary>
[Serializable]
public sealed class XorShift128RNG : RandomNumberGenerator
{
  // adapted from Marsaglia (July 2003). "Xorshift RNGs". Journal of Statistical Software Vol. 8 (Issue  14)
  // (http://www.jstatsoft.org/v08/i14/paper)

  /// <include file="documentation.xml" path="//Math/RNG/SeedSize/node()"/>
  public const int SeedSize = 4;

  /// <summary>Initializes a new <see cref="XorShift128RNG"/> random number generator with a seed based on the current time.</summary>
  public XorShift128RNG() : this(MakeTimeBasedSeed()) { }

  /// <summary>Initializes a new <see cref="XorShift128RNG"/> random number generator with the given seed (from which up to 4
  /// unsigned integers are used). If <paramref name="seed"/> is null or empty, a constant, default seed will be used. Note that
  /// the seed array should not contain all zeros.
  /// </summary>
  [CLSCompliant(false)]
  public XorShift128RNG(params uint[] seed)
  {
    X = 123456789;
    Y = 362436069;
    Z = 521288629;
    W = 88675123;

    if(seed != null)
    {
      if(seed.Length != 0) X = seed[0];
      if(seed.Length > 1) Y = seed[1];
      if(seed.Length > 2) Z = seed[2];
      if(seed.Length > 3 && (seed[0] != 0 || seed[1] != 0 || seed[2] != 0 || seed[3] != 0)) W = seed[3]; // avoid all zeroes
    }
  }

  /// <include file="documentation.xml" path="//Math/RNG/NextUint32/node()"/>
  [CLSCompliant(false)]
  public override uint NextUInt32()
  {
    uint t = X ^ (X << 11);
    X = Y;
    Y = Z;
    Z = W;
    return W = W ^ (W >> 19) ^ t ^ (t >> 8);
  }

  /// <include file="documentation.xml" path="//Math/RNG/LoadStateCore/node()"/>
  protected override void LoadStateCore(BinaryReader reader)
  {
    X = reader.ReadUInt32();
    Y = reader.ReadUInt32();
    Z = reader.ReadUInt32();
    W = reader.ReadUInt32();
  }

  /// <include file="documentation.xml" path="//Math/RNG/SaveStateCore/node()"/>
  protected override void SaveStateCore(BinaryWriter writer)
  {
    writer.Write(X);
    writer.Write(Y);
    writer.Write(Z);
    writer.Write(W);
  }

  uint X, Y, Z, W;
}
#endregion

} // namespace AdamMil.Mathematics.Random
