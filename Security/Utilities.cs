using System;

namespace AdamMil.Security
{

/// <summary>A class containing various helpful methods for dealing with security.</summary>
public static class SecurityUtility
{
  /// <summary>Clears the given buffer, if it is not null.</summary>
  public static void ZeroBuffer<T>(T[] buffer)
  {
    if(buffer != null) Array.Clear(buffer, 0, buffer.Length);
  }
}

} // namespace AdamMil.Security