using System;
using System.Globalization;

namespace AdamMil.Utilities
{

static class Exceptions
{
	public static ArgumentException InsufficientBufferSpace()
	{
		return new ArgumentException("Insufficient space in the output buffer.");
	}

	public static ArgumentException InsufficientBufferSpace(int elementsNeeded)
	{
		return new ArgumentException("Insufficient space in the output buffer. " +
		                             elementsNeeded.ToString(CultureInfo.InvariantCulture) + " elements needed.");
	}
}

} // namespace AdamMil.Utilities
