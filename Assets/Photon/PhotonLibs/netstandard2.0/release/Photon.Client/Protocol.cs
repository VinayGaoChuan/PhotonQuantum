using System;
using System.Collections.Generic;

namespace Photon.Client;

public abstract class Protocol
{
	public enum DeserializationFlags
	{
		None,
		AllowPooledByteArray,
		WrapIncomingStructs
	}

	/// <summary>Pool for ByteArraySlices.</summary>
	public readonly ByteArraySlicePool ByteArraySlicePool = new ByteArraySlicePool();

	internal static readonly Dictionary<Type, CustomType> TypeDict = new Dictionary<Type, CustomType>();

	internal static readonly Dictionary<byte, CustomType> CodeDict = new Dictionary<byte, CustomType>();

	public abstract string ProtocolType { get; }

	public abstract byte[] VersionBytes { get; }

	public abstract void Serialize(StreamBuffer dout, object serObject, bool setType);

	public abstract void SerializeShort(StreamBuffer dout, short serObject, bool setType);

	public abstract void SerializeString(StreamBuffer dout, string serObject, bool setType);

	public abstract void SerializeEventData(StreamBuffer stream, EventData serObject, bool setType);

	public abstract void SerializeOperationRequest(StreamBuffer stream, byte operationCode, ParameterDictionary parameters, bool setType);

	public abstract void SerializeOperationResponse(StreamBuffer stream, OperationResponse serObject, bool setType);

	public abstract object Deserialize(StreamBuffer din, byte type, DeserializationFlags flags = DeserializationFlags.None);

	public abstract short DeserializeShort(StreamBuffer din);

	public abstract byte DeserializeByte(StreamBuffer din);

	public abstract EventData DeserializeEventData(StreamBuffer din, EventData target = null, DeserializationFlags flags = DeserializationFlags.None);

	public abstract OperationRequest DeserializeOperationRequest(StreamBuffer din, DeserializationFlags flags = DeserializationFlags.None);

	public abstract OperationResponse DeserializeOperationResponse(StreamBuffer stream, DeserializationFlags flags = DeserializationFlags.None);

	public abstract DisconnectMessage DeserializeDisconnectMessage(StreamBuffer stream);

	/// <summary>
	/// Serialize creates a byte-array from the given object and returns it.
	/// </summary>
	/// <param name="obj">The object to serialize</param>
	/// <returns>The serialized byte-array</returns>
	public byte[] Serialize(object obj)
	{
		StreamBuffer ms = new StreamBuffer(64);
		Serialize(ms, obj, setType: true);
		return ms.ToArray();
	}

	/// <summary>
	/// Deserialize returns an object reassembled from the given StreamBuffer.
	/// </summary>
	/// <param name="stream">The buffer to be Deserialized</param>
	/// <returns>The Deserialized object</returns>
	public object Deserialize(StreamBuffer stream)
	{
		return Deserialize(stream, stream.ReadByte());
	}

	/// <summary>
	/// Deserialize returns an object reassembled from the given byte-array.
	/// </summary>
	/// <param name="serializedData">The byte-array to be Deserialized</param>
	/// <returns>The Deserialized object</returns>
	public object Deserialize(byte[] serializedData)
	{
		StreamBuffer stream = new StreamBuffer(serializedData);
		return Deserialize(stream, stream.ReadByte());
	}

	public object DeserializeMessage(StreamBuffer stream)
	{
		return Deserialize(stream, stream.ReadByte());
	}

	internal void SerializeMessage(StreamBuffer ms, object msg)
	{
		Serialize(ms, msg, setType: true);
	}

	public static bool TryRegisterType(Type type, byte typeCode, SerializeStreamMethod serializeFunction, DeserializeStreamMethod deserializeFunction)
	{
		if (CodeDict.ContainsKey(typeCode) || type == null || TypeDict.ContainsKey(type))
		{
			return false;
		}
		if (serializeFunction == null || deserializeFunction == null)
		{
			return false;
		}
		CustomType customType = new CustomType(type, typeCode, serializeFunction, deserializeFunction);
		CodeDict.Add(typeCode, customType);
		TypeDict.Add(type, customType);
		return true;
	}
}
