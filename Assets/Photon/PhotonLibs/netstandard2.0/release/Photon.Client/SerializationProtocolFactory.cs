namespace Photon.Client;

internal static class SerializationProtocolFactory
{
	internal static Protocol Create(SerializationProtocol serializationProtocol)
	{
		if (serializationProtocol == SerializationProtocol.GpBinaryV18)
		{
			return new Protocol18();
		}
		return new Protocol16();
	}
}
