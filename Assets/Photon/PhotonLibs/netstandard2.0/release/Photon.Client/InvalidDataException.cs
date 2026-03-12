using System;

namespace Photon.Client;

/// <summary>Exception type for de/serialization issues. Used in Protocol 1.8.</summary>
public class InvalidDataException : Exception
{
	/// <summary>Constructor for the exception.</summary>
	public InvalidDataException(string message)
		: base(message)
	{
	}
}
