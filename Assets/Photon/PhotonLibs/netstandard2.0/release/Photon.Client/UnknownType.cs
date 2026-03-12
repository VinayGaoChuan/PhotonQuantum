namespace Photon.Client;

/// <summary>
/// Used as container for unknown types the client could not deserialize.
/// </summary>
public class UnknownType
{
	/// <summary>
	/// The type code which was read for this type.
	/// </summary>
	public byte TypeCode;

	/// <summary>
	/// The size/length value that was read for this type.
	/// </summary>
	/// <remarks>May be larger than Data.Length, if the Size exceeded the remaining message content.</remarks>
	public int Size;

	/// <summary>
	/// Container for the data that arrived.
	/// </summary>
	/// <remarks>If the Size exceeded the remaining message length, only the remaining data is read. This may be null, if the size was somehow less than 1.</remarks>
	public byte[] Data;
}
