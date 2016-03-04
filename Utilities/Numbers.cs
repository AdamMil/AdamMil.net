using System;

namespace AdamMil.Utilities
{

/// <summary>Provides some useful extensions for primitive numeric types.</summary>
public static class NumberExtensions
{
  /// <summary>Determines whether the given value is a number (i.e. not NaN or infinity).</summary>
  public static unsafe bool IsNumber(this double v)
  {
    if(BitConverter.IsLittleEndian) return (*((uint*)&v+1) & (uint)int.MaxValue) < 0x7ff00000;
    else return (*(uint*)&v & (uint)int.MaxValue) < 0x7ff00000;
  }

  /// <summary>Determines whether the given value is a number (i.e. not NaN or infinity).</summary>
  public static unsafe bool IsNumber(this float v)
  {
    return (*(uint*)&v & (uint)int.MaxValue) < 0x7ff00000;
  }
}

} // namespace AdamMil.Utilities