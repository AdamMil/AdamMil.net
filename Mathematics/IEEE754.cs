namespace AdamMil.Mathematics
{
  // TODO: implement a high-precision (e.g. 96- or 128-bit) emulated floating point format for those rare situations when we would benefit
  // from having more than double precision provides. see, for instance, ieeelib

  /// <summary>Provides utilities related to the IEEE754 floating point format.</summary>
  public static class IEEE754
  {
    /// <summary>The number of digits of precision that a double-precision floating point number can provide.</summary>
    public const int DoubleDigits = 15;
    /// <summary>The number of digits of precision that a single-precision floating point number can provide.</summary>
    public const int SingleDigits = 6;
    /// <summary>The smallest double-precision floating point number that, when added to 1.0, produces a result not equal to 1.0.</summary>
    public const double DoublePrecision = 2.2204460492503131e-016;
    /// <summary>The smallest single-precision floating point number that, when added to 1.0f, produces a result not equal to 1.0.</summary>
    public const float SinglePrecision = 1.192092896e-07f;
  }
}