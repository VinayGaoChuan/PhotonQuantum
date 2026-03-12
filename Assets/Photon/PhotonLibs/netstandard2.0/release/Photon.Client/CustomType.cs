using System;

namespace Photon.Client;

internal class CustomType
{
	public readonly byte Code;

	public readonly Type Type;

	public readonly SerializeStreamMethod SerializeStreamFunction;

	public readonly DeserializeStreamMethod DeserializeStreamFunction;

	public CustomType(Type type, byte code, SerializeStreamMethod serializeFunction, DeserializeStreamMethod deserializeFunction)
	{
		Type = type;
		Code = code;
		SerializeStreamFunction = serializeFunction;
		DeserializeStreamFunction = deserializeFunction;
	}
}
