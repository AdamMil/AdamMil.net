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
using AdamMil.Utilities;
using System.IO;
using BinaryReader = AdamMil.IO.BinaryReader;
using BinaryWriter = AdamMil.IO.BinaryWriter;

namespace AdamMil.Mathematics.Random
{

#region CollectionExtensions
/// <summary>Implements useful extensions for .NET built-in collections.</summary>
public static class CollectionExtensions
{
  /// <summary>Returns a random item from the list.</summary>
  [CLSCompliant(false)]
  public static T SelectRandom<T>(this IList<T> list, RandomNumberGenerator random)
  {
    if(list == null || random == null) throw new ArgumentNullException();
    if(list.Count == 0) throw new ArgumentException("The collection is empty.");
    return list[random.Next(list.Count)];
  }
}
#endregion

#region RandomNumberGenerator
/// <summary>Provides a base class for random number generators.</summary>
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
///   <term>CMWC4096</term>
///   <description>0.48</description>
///   <description>16 kb</description>
///   <description>2^131086</description>
///   <description>Good</description>
///   <description>Huge period. Significant startup time</description>
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
///   <description>Large period. Significant startup time</description>
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
[CLSCompliant(false)]
[Serializable]
public abstract class RandomNumberGenerator
{
  /// <summary>Returns a byte array containing the internal state of the random number generator. This array can later
  /// be passed to <see cref="SetState"/> to restore the generator state.
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
    using(BinaryReader reader = new BinaryReader(stream)) LoadState(reader);
  }

  /// <summary>Loads the generator state from a <see cref="BinaryReader"/>.</summary>
  public void LoadState(BinaryReader reader)
  {
    if(reader == null) throw new ArgumentNullException();
    bitBuffer = reader.ReadUInt32();
    bits      = reader.ReadByte();
    LoadStateCore(reader);
  }

  /// <summary>Saves the generator state to a stream.</summary>
  public void SaveState(Stream stream)
  {
    using(BinaryWriter writer = new BinaryWriter(stream)) SaveState(writer);
  }

  /// <summary>Saves the generator state to a <see cref="BinaryWriter"/>.</summary>
  public void SaveState(BinaryWriter writer)
  {
    if(writer == null) throw new ArgumentNullException();
    writer.Write(bitBuffer);
    writer.Write(bits);
    SaveStateCore(writer);
  }

  /// <summary>Restores the generator state from an array retrieved by calling <see cref="GetState"/>.</summary>
  public void SetState(byte[] state)
  {
    using(MemoryStream ms = new MemoryStream(state)) LoadState(ms);
  }

  /// <summary>Generates and returns a random non-negative integer.</summary>
  /// <remarks>The default implementation right-shifts a value from <see cref="NextUint32"/> by one bit and returns it.</remarks>
  public int Next()
  {
    return (int)(NextUint32() >> 1);
  }

  /// <summary>Generates and returns a random integer less than the given maximum, which must be positive.</summary>
  public int Next(int exclusiveMaximum)
  {
    if(exclusiveMaximum <= 0) throw new ArgumentOutOfRangeException();
    return (int)(NextDouble() * exclusiveMaximum);
  }

  /// <summary>Generates and returns a random integer between the given inclusive minimum and maximum, which may be any integers.</summary>
  public int Next(int minimum, int maximum)
  {
    if(minimum > maximum) throw new ArgumentException("The minimum must be less than or equal to the maximum.");
    return (int)(NextDouble() * ((long)maximum - minimum)) + minimum;
  }

  /// <summary>Generates and returns a random boolean value.</summary>
  /// <remarks>This method uses <see cref="NextUint32"/> to generate batches of 32 bits at a time.</remarks>
  public bool NextBoolean()
  {
    if(bits == 0)
    {
      bitBuffer = NextUint32();
      bits = 32;
    }

    bool result = (bitBuffer & 1) != 0;
    bitBuffer >>= 1;
    bits--;
    return result;
  }

  /// <include file="documentation.xml" path="//Math/RNG/NextUint32/*"/>
  public abstract uint NextUint32();

  /// <summary>Generates and returns a random 64-bit unsigned integer.</summary>
  /// <remarks>The default implementation combines two 32-bit numbers returned from <see cref="NextUint32"/> into a single 64-bit
  /// number. If the generator implementation is natively capable of generating a 64-bit output, you may want to override this
  /// method to make use of that ability.
  /// </remarks>
  public virtual unsafe ulong NextUint64()
  {
    ulong n;
    *(uint*)&n     = NextUint32();
    *((uint*)&n+1) = NextUint32();
    return n;
  }

  /// <summary>Generates and returns a random double greater than or equal to zero and less than one.</summary>
  /// <remarks>It is important that this method be capable of returning a sufficient number of different values. For instance,
  /// <see cref="Next(int,int)"/> assumes that this method returns at least 2^32 possible values, and <see cref="Next(int)"/>
  /// requires at least 2^31 possible values. The default implementation uses <see cref="NextUint64"/> to generate a double with
  /// the full 52 bits of randomness.
  /// </remarks>
  public virtual unsafe double NextDouble()
  {
    // adapted from http://www.cs.ucl.ac.uk/staff/d.jones/GoodPracticeRNG.pdf
    ulong n = (NextUint64() >> 12) | 0x3FF0000000000000;
    return *(double*)&n - 1;
  }

  /// <summary>Generates and returns a random float greater than or equal to zero and less than one.</summary>
  /// <remarks>The implementation uses <see cref="NextUint32"/> to generate a float with 23 bits of randomness.</remarks>
  public unsafe float NextFloat()
  {
    // adapted from http://www.cs.ucl.ac.uk/staff/d.jones/GoodPracticeRNG.pdf
    uint n = (NextUint32() >> 9) | 0x3F800000;
    return *(float*)&n - 1;
  }

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
  /// <remarks>The default implementation generates 32-bit integers with <see cref="NextUint32"/> and uses them to fill up to
  /// four bytes at a time within the array.
  /// </remarks>
  public virtual void GenerateBytes(byte[] bytes, int index, int count)
  {
    Utility.ValidateRange(bytes, index, count);

    int chunks = count/4; // do as many bytes as we can in 4-byte chunks
    for(; chunks != 0; index += 4, chunks--)
    {
      uint chunk = NextUint32();
      bytes[index]   = (byte)chunk;
      bytes[index+1] = (byte)(chunk >> 8);
      bytes[index+2] = (byte)(chunk >> 16);
      bytes[index+3] = (byte)(chunk >> 24);
    }

    count &= 3; // get the number of remaining bytes
    if(count != 0)
    {
      uint chunk = NextUint32();
      do
      {
        bytes[index++] = (byte)chunk;
        chunk >>= 8;
      } while(--count != 0);
    }
  }

  /// <summary>Creates a new <see cref="RandomNumberGenerator"/> of the all-around best general-purpose type in the library.</summary>
  public static RandomNumberGenerator CreateDefault()
  {
    return new KISSRNG();
  }

  /// <summary>Creates a new <see cref="RandomNumberGenerator"/> of the all-around best general-purpose type in the library.</summary>
  public static RandomNumberGenerator CreateDefault(uint[] seed)
  {
    return new KISSRNG(seed);
  }

  /// <summary>Creates a new <see cref="RandomNumberGenerator"/> of the fastest type available in the library.</summary>
  public static RandomNumberGenerator CreateFastest()
  {
    return new XorShift128RNG();
  }

  /// <summary>Creates a new <see cref="RandomNumberGenerator"/> of the fastest type available in the library.</summary>
  public static RandomNumberGenerator CreateFastest(uint[] seed)
  {
    return new XorShift128RNG(seed);
  }

  /// <include file="documentation.xml" path="//Math/RNG/LoadStateCore/*"/>
  protected abstract void LoadStateCore(BinaryReader reader);

  /// <include file="documentation.xml" path="//Math/RNG/SaveStateCore/*"/>
  protected abstract void SaveStateCore(BinaryWriter writer);

  /// <summary>Returns a 64-bit seed based on the current time (both real-world time and an internal timer), as an array of four
  /// uints.
  /// </summary>
  protected static uint[] MakeTimeBasedSeed()
  {
    if(timer == null)
    {
      System.Diagnostics.Stopwatch t = new System.Diagnostics.Stopwatch();
      t.Start();
      timer = t;
    }

    long dateTicks = DateTime.Now.Ticks, timerTicks = timer.ElapsedTicks;
    // add constants to the timer tick values to prevent them from being too small (especially zero) shortly after startup.
    // the timer may not be thread-safe, but it shouldn't crash and we don't really need accuracy, only rapid change in value
    return new uint[]
    {
      (uint)timerTicks+123456789, (uint)(timerTicks>>32)+678912345, (uint)(dateTicks>>32)+seedIncrement++, (uint)dateTicks
    };
  }

  uint bitBuffer;
  byte bits;

  static System.Diagnostics.Stopwatch timer;
  static uint seedIncrement;
}
#endregion

#region AWCKISSRNG
/// <summary>Implements a KISS-like generator following a proposal by George Marsaglia for a faster but poorer-quality
/// alternative to the full <see cref="KISSRNG">KISS generator</see>. The multiply-with-carry component of the KISS generator is
/// replaced with an add-with-carry component. The result is a very fast random number generator that generates numbers with a
/// constant time per number and uses very little memory, while maintaining fairly good quality. It has a period of about 2^121.
/// </summary>
[CLSCompliant(false)]
[Serializable]
public sealed class AWCKISSRNG : RandomNumberGenerator
{
  // adapted from http://www.cs.ucl.ac.uk/staff/d.jones/GoodPracticeRNG.pdf

  /// <include file="documentation.xml" path="//Math/RNG/SeedSize/*"/>
  public const int SeedSize = 4;

  /// <summary>Initializes a new <see cref="AWCKISSRNG"/> random number generator with a seed based on the current time.</summary>
  public AWCKISSRNG() : this(MakeTimeBasedSeed()) { }

  /// <summary>Initializes a new <see cref="AWCKISSRNG"/> random number generator with the given seed (from which up to 4
  /// unsigned integers are used). If <paramref name="seed"/> is null or empty, a constant, default seed will be used. Note that
  /// the seed array should not contain zeros, as these may be ignored.
  /// </summary>
  public AWCKISSRNG(uint[] seed)
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

  /// <include file="documentation.xml" path="//Math/RNG/NextUint32/*"/>
  public override uint NextUint32()
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

  /// <include file="documentation.xml" path="//Math/RNG/LoadStateCore/*"/>
  protected override void LoadStateCore(BinaryReader reader)
  {
    X = reader.ReadUInt32();
    Y = reader.ReadUInt32();
    Z = reader.ReadUInt32();
    W = reader.ReadUInt32();
    C = reader.ReadUInt32();
  }

  /// <include file="documentation.xml" path="//Math/RNG/SaveStateCore/*"/>
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

#region CMWC4096RNG
/// <summary>Implements the CMWC4096 random number generator by George Marsaglia. The generator is quite fast with good
/// randomness, and generates numbers in a constant time per number, but uses about 16 kb of memory. The primary benefit of
/// this generator is its huge period of 2^131086.
/// </summary>
[CLSCompliant(false)]
[Serializable]
public sealed class CMWC4096RNG : RandomNumberGenerator
{
  // adapted from http://groups.google.com/group/comp.soft-sys.math.mathematica/msg/95a94c3b2aa5f077

  /// <include file="documentation.xml" path="//Math/RNG/SeedSize/*"/>
  public const int SeedSize = 4096;

  /// <summary>Initializes a new <see cref="CMWC4096RNG"/> random number generator with a seed based on the current time.</summary>
  public CMWC4096RNG() : this(MakeTimeBasedSeed()) { }

  /// <summary>Initializes a new <see cref="CMWC4096RNG"/> random number generator with the given seed (from which up to 4096
  /// unsigned integers are used). If <paramref name="seed"/> is null, a constant, default seed will be used. The elements of the
  /// seed array should be random numbers. If at least 4096 seed numbers are given, they will be directly used as the generator's
  /// internal state. Otherwise, the seed will be used to initialize another random number generator, which in turn will be used
  /// to initialize the generator's internal state.
  /// </summary>
  public CMWC4096RNG(uint[] seed)
  {
    Q = new uint[4096];
    C = 362436;
    I = 4095;

    if(seed != null && seed.Length >= Q.Length)
    {
      Array.Copy(seed, Q, Q.Length);
    }
    else
    {
      // fill the elements with random numbers from the XorShiftRNG generator (the fastest choice of the other RNGs)
      XorShift128RNG rng = new XorShift128RNG(seed);
      for(int i=0; i<Q.Length; i++) Q[i] = rng.NextUint32();
    }
  }

  /// <include file="documentation.xml" path="//Math/RNG/NextUint32/*"/>
  public override uint NextUint32()
  {
    I = (I+1) & 4095;
    ulong t = (ulong)18782*Q[I] + C;
    C = (uint)(t>>32);
    uint x = (uint)t + C;
    if(x < C)
    {
      x++;
      C++;
    }
    return Q[I] = 0XFFFFFFFE - x;
  }

  /// <include file="documentation.xml" path="//Math/RNG/LoadStateCore/*"/>
  protected override void LoadStateCore(BinaryReader reader)
  {
    Q = reader.ReadUInt32s(4096);
    C = reader.ReadUInt32();
    I = reader.ReadInt32();
  }

  /// <include file="documentation.xml" path="//Math/RNG/SaveStateCore/*"/>
  protected override void SaveStateCore(BinaryWriter writer)
  {
    writer.Write(Q);
    writer.Write(C);
    writer.Write(I);
  }

  uint[] Q;
  uint C;
  int I;
}
#endregion

#region ISAACRNG
/// <summary>Implements the ISAAC random number generator. The generator was designed for cryptography and is very high quality,
/// but is also quite fast. However, it generates results in large batches, so the time needed to get a result is very uneven,
/// depending on whether a new batch needs to be generated. It requires about 2 kb of memory. Note that although ISAAC was
/// designed for cryptography, it is only secure if initialized with a secure seed. A secure seed is an array of 256 random uint
/// values, perhaps obtained by encrypting an unpredictable value with a strong block cipher. The default constructor does not
/// seed the generator securely, since it uses a small seed based on the current time.
/// </summary>
[CLSCompliant(false)]
[Serializable]
public sealed class ISAACRNG : RandomNumberGenerator
{
  // adapted from http://burtleburtle.net/bob/rand/isaacafa.html

  /// <include file="documentation.xml" path="//Math/RNG/SeedSize/*"/>
  public const int SeedSize = 256;

  /// <summary>Initializes a new ISAAC random number generator with a seed based on the current time.</summary>
  public ISAACRNG() : this(MakeTimeBasedSeed()) { }

  /// <summary>Initializes a new ISAAC random number generator with the given seed (from which up to 256 unsigned integers are
  /// used). If <paramref name="seed"/> is null, a constant, default seed will be used.
  /// </summary>
  public ISAACRNG(uint[] seed)
  {
    results = new uint[Size];
    state   = new uint[Size];
    if(seed != null) Array.Copy(seed, results, Math.Min(Size, seed.Length));
    Initialize(seed != null);
  }

  /// <include file="documentation.xml" path="//Math/RNG/NextUint32/*"/>
  public override uint NextUint32()
  {
    if(resultIndex == Size) Isaac();
    return results[resultIndex++];
  }

  /// <include file="documentation.xml" path="//Math/RNG/LoadStateCore/*"/>
  protected override void LoadStateCore(BinaryReader reader)
  {
    results     = reader.ReadUInt32s(256);
    state       = reader.ReadUInt32s(256);
    resultIndex = reader.ReadInt32();
    accumulator = reader.ReadUInt32();
    lastResult  = reader.ReadUInt32();
    counter     = reader.ReadUInt32();
  }

  /// <include file="documentation.xml" path="//Math/RNG/SaveStateCore/*"/>
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
[CLSCompliant(false)]
[Serializable]
public sealed class KISSRNG : RandomNumberGenerator
{
  // adapted from http://www.cs.ucl.ac.uk/staff/d.jones/GoodPracticeRNG.pdf

  /// <include file="documentation.xml" path="//Math/RNG/SeedSize/*"/>
  public const int SeedSize = 4;

  /// <summary>Initializes a new KISS random number generator with a seed based on the current time.</summary>
  public KISSRNG() : this(MakeTimeBasedSeed()) { }

  /// <summary>Initializes a new KISS random number generator with the given seed (from which up to 4 unsigned integers are
  /// used). If <paramref name="seed"/> is null or empty, a constant, default seed will be used. Note that zeroes in the seed
  /// array may be ignored.
  /// </summary>
  public KISSRNG(uint[] seed)
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

  /// <include file="documentation.xml" path="//Math/RNG/NextUint32/*"/>
  public override uint NextUint32()
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

  /// <include file="documentation.xml" path="//Math/RNG/LoadStateCore/*"/>
  protected override void LoadStateCore(BinaryReader reader)
  {
    X = reader.ReadUInt32();
    Y = reader.ReadUInt32();
    Z = reader.ReadUInt32();
    C = reader.ReadUInt32();
  }

  /// <include file="documentation.xml" path="//Math/RNG/SaveStateCore/*"/>
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
/// is its large period of 2^8222.
/// </summary>
[CLSCompliant(false)]
[Serializable]
public sealed class MWC256RNG : RandomNumberGenerator
{
  // adapted from http://www.cs.ucl.ac.uk/staff/d.jones/GoodPracticeRNG.pdf

  /// <include file="documentation.xml" path="//Math/RNG/SeedSize/*"/>
  public const int SeedSize = 256;

  /// <summary>Initializes a new <see cref="MWC256RNG"/> random number generator with a seed based on the current time.</summary>
  public MWC256RNG() : this(MakeTimeBasedSeed()) { }

  /// <summary>Initializes a new <see cref="MWC256RNG"/> random number generator with the given seed (from which up to 256
  /// unsigned integers are used). If <paramref name="seed"/> is null, a constant, default seed will be used. The elements of the
  /// seed array should be random numbers. If at least 256 seed numbers are given, they will be directly used as the generator's
  /// internal state. Otherwise, the seed will be used to initialize another random number generator, which in turn will be used
  /// to initialize the generator's internal state.
  /// </summary>
  public MWC256RNG(uint[] seed)
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
      // fill the elements with random numbers from the XorShiftRNG generator (the fastest choice of the other RNGs)
      XorShift128RNG rng = new XorShift128RNG(seed);
      for(int i=0; i<Q.Length; i++) Q[i] = rng.NextUint32();
    }
  }

  /// <include file="documentation.xml" path="//Math/RNG/NextUint32/*"/>
  public override uint NextUint32()
  {
    I++;
    ulong t = (ulong)809430660*Q[I] + C;
    C = (uint)(t>>32);
    return Q[I] = (uint)t;
  }

  /// <include file="documentation.xml" path="//Math/RNG/LoadStateCore/*"/>
  protected override void LoadStateCore(BinaryReader reader)
  {
    Q = reader.ReadUInt32s(256);
    C = reader.ReadUInt32();
    I = reader.ReadByte();
  }

  /// <include file="documentation.xml" path="//Math/RNG/SaveStateCore/*"/>
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
[CLSCompliant(false)]
[Serializable]
public sealed class XorShift128RNG : RandomNumberGenerator
{
  // adapted from Marsaglia (July 2003). "Xorshift RNGs". Journal of Statistical Software Vol. 8 (Issue  14)
  // (http://www.jstatsoft.org/v08/i14/paper)

  /// <include file="documentation.xml" path="//Math/RNG/SeedSize/*"/>
  public const int SeedSize = 4;

  /// <summary>Initializes a new <see cref="AWCKISSRNG"/> random number generator with a seed based on the current time.</summary>
  public XorShift128RNG() : this(MakeTimeBasedSeed()) { }

  /// <summary>Initializes a new <see cref="AWCKISSRNG"/> random number generator with the given seed (from which up to 4
  /// unsigned integers are used). If <paramref name="seed"/> is null or empty, a constant, default seed will be used. Note that
  /// the seed array should not contain all zeros.
  /// </summary>
  public XorShift128RNG(uint[] seed)
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

  /// <include file="documentation.xml" path="//Math/RNG/NextUint32/*"/>
  public override uint NextUint32()
  {
    uint t = X ^ (X << 11);
    X = Y;
    Y = Z;
    Z = W;
    return W = W ^ (W >> 19) ^ t ^ (t >> 8);
  }

  /// <include file="documentation.xml" path="//Math/RNG/LoadStateCore/*"/>
  protected override void LoadStateCore(BinaryReader reader)
  {
    X = reader.ReadUInt32();
    Y = reader.ReadUInt32();
    Z = reader.ReadUInt32();
    W = reader.ReadUInt32();
  }

  /// <include file="documentation.xml" path="//Math/RNG/SaveStateCore/*"/>
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
