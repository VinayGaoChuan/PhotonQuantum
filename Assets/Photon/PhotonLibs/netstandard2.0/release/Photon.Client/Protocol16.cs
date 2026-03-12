using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Photon.Client.StructWrapping;

namespace Photon.Client;

/// <summary>
/// Exit Games GpBinaryV16 protocol implementation
/// </summary>
public class Protocol16 : Protocol
{
	/// <summary>
	///   The gp type.
	/// </summary>
	public enum GpType : byte
	{
		/// <summary>
		///   Unkown type.
		/// </summary>
		Unknown = 0,
		/// <summary>
		///   An array of objects.
		/// </summary>
		/// <remarks>
		///   This type is new in version 1.5.
		/// </remarks>
		Array = 121,
		/// <summary>
		///   A boolean Value.
		/// </summary>
		Boolean = 111,
		/// <summary>
		///   A byte value.
		/// </summary>
		Byte = 98,
		/// <summary>
		///   An array of bytes.
		/// </summary>
		ByteArray = 120,
		/// <summary>
		///   An array of objects.
		/// </summary>
		ObjectArray = 122,
		/// <summary>
		///   A 16-bit integer value.
		/// </summary>
		Short = 107,
		/// <summary>
		///   A 32-bit floating-point value.
		/// </summary>
		/// <remarks>
		///   This type is new in version 1.5.
		/// </remarks>
		Float = 102,
		/// <summary>
		///   A dictionary
		/// </summary>
		/// <remarks>
		///   This type is new in version 1.6.
		/// </remarks>
		Dictionary = 68,
		/// <summary>
		///   A 64-bit floating-point value.
		/// </summary>
		/// <remarks>
		///   This type is new in version 1.5.
		/// </remarks>
		Double = 100,
		/// <summary>
		///   A PhotonHashtable.
		/// </summary>
		Hashtable = 104,
		/// <summary>
		///   A 32-bit integer value.
		/// </summary>
		Integer = 105,
		/// <summary>
		///   An array of 32-bit integer values.
		/// </summary>
		IntegerArray = 110,
		/// <summary>
		///   A 64-bit integer value.
		/// </summary>
		Long = 108,
		/// <summary>
		///   A string value.
		/// </summary>
		String = 115,
		/// <summary>
		///   An array of string values.
		/// </summary>
		StringArray = 97,
		/// <summary>
		///   A custom type. 0x63
		/// </summary>
		Custom = 99,
		/// <summary>
		///   Null value don't have types.
		/// </summary>
		Null = 42,
		EventData = 101,
		OperationRequest = 113,
		OperationResponse = 112
	}

	private readonly byte[] versionBytes = new byte[2] { 1, 6 };

	private readonly byte[] memShort = new byte[2];

	private readonly long[] memLongBlock = new long[1];

	private readonly byte[] memLongBlockBytes = new byte[8];

	private static readonly float[] memFloatBlock = new float[1];

	private static readonly byte[] memFloatBlockBytes = new byte[4];

	private readonly double[] memDoubleBlock = new double[1];

	private readonly byte[] memDoubleBlockBytes = new byte[8];

	private readonly byte[] memInteger = new byte[4];

	private readonly byte[] memLong = new byte[8];

	private readonly byte[] memFloat = new byte[4];

	private readonly byte[] memDouble = new byte[8];

	public override string ProtocolType => "GpBinaryV16";

	public override byte[] VersionBytes => versionBytes;

	private bool SerializeCustom(StreamBuffer dout, object serObject)
	{
		Type type = ((serObject is StructWrapper wrapper) ? wrapper.ttype : serObject.GetType());
		if (Protocol.TypeDict.TryGetValue(type, out var customType))
		{
			if (customType.SerializeStreamFunction == null)
			{
				throw new NullReferenceException($"Custom Type serialization failed. SerializeStreamFunction is null for Type: {customType.Type} Code: {customType.Code}.");
			}
			dout.WriteByte(99);
			dout.WriteByte(customType.Code);
			int posOfLengthInfo = dout.Position;
			dout.Position += 2;
			short serializedLength = customType.SerializeStreamFunction(dout, serObject);
			long newPos = dout.Position;
			dout.Position = posOfLengthInfo;
			byte code = customType.Code;
			SerializeLengthAsShort(dout, serializedLength, "Custom Type " + code);
			dout.Position += serializedLength;
			if (dout.Position != newPos)
			{
				throw new Exception($"Serialization failed. Stream position corrupted. Should be {newPos} is now: {dout.Position} serializedLength: {serializedLength}");
			}
			return true;
		}
		return false;
	}

	private object DeserializeCustom(StreamBuffer din, byte customTypeCode, DeserializationFlags flags = DeserializationFlags.None)
	{
		short length = DeserializeShort(din);
		if (length < 0)
		{
			throw new InvalidDataException($"DeserializeCustom() read negative length value: {length} before position: {din.Position}");
		}
		if (length <= din.Available && Protocol.CodeDict.TryGetValue(customTypeCode, out var customType))
		{
			if (customType.DeserializeStreamFunction == null)
			{
				throw new NullReferenceException($"Custom Type deserialization failed. DeserializeStreamFunction is null for Type: {customType.Type} Code: {customType.Code}.");
			}
			int pos = din.Position;
			object result = customType.DeserializeStreamFunction(din, length);
			if (din.Position - pos != length)
			{
				din.Position = pos + length;
			}
			return result;
		}
		int boundedSize = ((length <= din.Available) ? length : ((short)din.Available));
		byte[] bytes = new byte[boundedSize];
		din.Read(bytes, 0, boundedSize);
		return bytes;
	}

	private Type GetTypeOfCode(byte typeCode)
	{
		switch (typeCode)
		{
		case 105:
			return typeof(int);
		case 115:
			return typeof(string);
		case 97:
			return typeof(string[]);
		case 120:
			return typeof(byte[]);
		case 110:
			return typeof(int[]);
		case 104:
			return typeof(PhotonHashtable);
		case 68:
			return typeof(IDictionary);
		case 111:
			return typeof(bool);
		case 107:
			return typeof(short);
		case 108:
			return typeof(long);
		case 98:
			return typeof(byte);
		case 102:
			return typeof(float);
		case 100:
			return typeof(double);
		case 121:
			return typeof(Array);
		case 99:
			return typeof(CustomType);
		case 122:
			return typeof(object[]);
		case 101:
			return typeof(EventData);
		case 113:
			return typeof(OperationRequest);
		case 112:
			return typeof(OperationResponse);
		case 0:
		case 42:
			return typeof(object);
		default:
			throw new Exception("GetTypeOfCode failed for typeCode: " + typeCode);
		}
	}

	private GpType GetCodeOfType(Type type)
	{
		switch (Type.GetTypeCode(type))
		{
		case TypeCode.Byte:
			return GpType.Byte;
		case TypeCode.String:
			return GpType.String;
		case TypeCode.Boolean:
			return GpType.Boolean;
		case TypeCode.Int16:
			return GpType.Short;
		case TypeCode.Int32:
			return GpType.Integer;
		case TypeCode.Int64:
			return GpType.Long;
		case TypeCode.Single:
			return GpType.Float;
		case TypeCode.Double:
			return GpType.Double;
		default:
			if (type.IsArray)
			{
				if (type == typeof(byte[]))
				{
					return GpType.ByteArray;
				}
				return GpType.Array;
			}
			if (type == typeof(PhotonHashtable))
			{
				return GpType.Hashtable;
			}
			if (type == typeof(List<object>))
			{
				return GpType.ObjectArray;
			}
			if (type.IsGenericType && typeof(Dictionary<, >) == type.GetGenericTypeDefinition())
			{
				return GpType.Dictionary;
			}
			if (type == typeof(EventData))
			{
				return GpType.EventData;
			}
			if (type == typeof(OperationRequest))
			{
				return GpType.OperationRequest;
			}
			if (type == typeof(OperationResponse))
			{
				return GpType.OperationResponse;
			}
			return GpType.Unknown;
		}
	}

	private Array CreateArrayByType(byte arrayType, short length)
	{
		return Array.CreateInstance(GetTypeOfCode(arrayType), length);
	}

	public void SerializeOperationRequest(StreamBuffer stream, OperationRequest operation, bool setType)
	{
		SerializeOperationRequest(stream, operation.OperationCode, operation.Parameters, setType);
	}

	public override void SerializeOperationRequest(StreamBuffer stream, byte operationCode, ParameterDictionary parameters, bool setType)
	{
		if (setType)
		{
			stream.WriteByte(113);
		}
		stream.WriteByte(operationCode);
		SerializeParameterTable(stream, parameters);
	}

	public override OperationRequest DeserializeOperationRequest(StreamBuffer din, DeserializationFlags flags)
	{
		OperationRequest request = new OperationRequest();
		request.OperationCode = DeserializeByte(din);
		request.Parameters = DeserializeParameterDictionary(din, request.Parameters, flags);
		return request;
	}

	public override void SerializeOperationResponse(StreamBuffer stream, OperationResponse serObject, bool setType)
	{
		if (setType)
		{
			stream.WriteByte(112);
		}
		stream.WriteByte(serObject.OperationCode);
		SerializeShort(stream, serObject.ReturnCode, setType: false);
		if (string.IsNullOrEmpty(serObject.DebugMessage))
		{
			stream.WriteByte(42);
		}
		else
		{
			SerializeString(stream, serObject.DebugMessage, setType: false);
		}
		SerializeParameterTable(stream, serObject.Parameters);
	}

	public override DisconnectMessage DeserializeDisconnectMessage(StreamBuffer stream)
	{
		return new DisconnectMessage
		{
			Code = DeserializeShort(stream),
			DebugMessage = (Deserialize(stream, DeserializeByte(stream)) as string),
			Parameters = DeserializeParameterDictionary(stream)
		};
	}

	public override OperationResponse DeserializeOperationResponse(StreamBuffer stream, DeserializationFlags flags = DeserializationFlags.None)
	{
		return new OperationResponse
		{
			OperationCode = DeserializeByte(stream),
			ReturnCode = DeserializeShort(stream),
			DebugMessage = (Deserialize(stream, DeserializeByte(stream)) as string),
			Parameters = DeserializeParameterDictionary(stream)
		};
	}

	public override void SerializeEventData(StreamBuffer stream, EventData serObject, bool setType)
	{
		if (setType)
		{
			stream.WriteByte(101);
		}
		stream.WriteByte(serObject.Code);
		SerializeParameterTable(stream, serObject.Parameters);
	}

	public override EventData DeserializeEventData(StreamBuffer din, EventData target = null, DeserializationFlags flags = DeserializationFlags.None)
	{
		EventData result;
		if (target != null)
		{
			target.Reset();
			result = target;
		}
		else
		{
			result = new EventData();
		}
		result.Code = DeserializeByte(din);
		DeserializeParameterDictionary(din, result.Parameters);
		return result;
	}

	[Obsolete("Use ParameterDictionary instead of Dictionary<byte, object>.")]
	private void SerializeParameterTable(StreamBuffer stream, Dictionary<byte, object> parameters)
	{
		if (parameters == null || parameters.Count == 0)
		{
			SerializeShort(stream, 0, setType: false);
			return;
		}
		SerializeLengthAsShort(stream, parameters.Count, "ParameterTable");
		foreach (KeyValuePair<byte, object> pair in parameters)
		{
			stream.WriteByte(pair.Key);
			Serialize(stream, pair.Value, setType: true);
		}
	}

	private void SerializeParameterTable(StreamBuffer stream, ParameterDictionary parameters)
	{
		if (parameters == null || parameters.Count == 0)
		{
			SerializeShort(stream, 0, setType: false);
			return;
		}
		SerializeLengthAsShort(stream, parameters.Count, "Array");
		foreach (KeyValuePair<byte, object> pair in parameters)
		{
			stream.WriteByte(pair.Key);
			Serialize(stream, pair.Value, setType: true);
		}
	}

	private Dictionary<byte, object> DeserializeParameterTable(StreamBuffer stream, Dictionary<byte, object> target = null)
	{
		short numRetVals = DeserializeShort(stream);
		Dictionary<byte, object> retVals = ((target != null) ? target : new Dictionary<byte, object>(numRetVals));
		for (int i = 0; i < numRetVals; i++)
		{
			byte keyByteCode = stream.ReadByte();
			object valueObject = Deserialize(stream, stream.ReadByte());
			retVals[keyByteCode] = valueObject;
		}
		return retVals;
	}

	private ParameterDictionary DeserializeParameterDictionary(StreamBuffer stream, ParameterDictionary target = null, DeserializationFlags flags = DeserializationFlags.None)
	{
		short numRetVals = DeserializeShort(stream);
		ParameterDictionary retVals = ((target != null) ? target : new ParameterDictionary(numRetVals));
		for (int i = 0; i < numRetVals; i++)
		{
			byte keyByteCode = stream.ReadByte();
			object valueObject = Deserialize(stream, stream.ReadByte(), flags);
			retVals.Add(keyByteCode, valueObject);
		}
		return retVals;
	}

	/// <summary>
	/// Calls the correct serialization method for the passed object.
	/// </summary>
	public override void Serialize(StreamBuffer dout, object serObject, bool setType)
	{
		if (serObject == null)
		{
			if (setType)
			{
				dout.WriteByte(42);
			}
			return;
		}
		Type type = ((serObject is StructWrapper wrapper) ? wrapper.ttype : serObject.GetType());
		switch (GetCodeOfType(type))
		{
		case GpType.Byte:
			SerializeByte(dout, serObject.Get<byte>(), setType);
			return;
		case GpType.String:
			SerializeString(dout, (string)serObject, setType);
			return;
		case GpType.Boolean:
			SerializeBoolean(dout, serObject.Get<bool>(), setType);
			return;
		case GpType.Short:
			SerializeShort(dout, serObject.Get<short>(), setType);
			return;
		case GpType.Integer:
			SerializeInteger(dout, serObject.Get<int>(), setType);
			return;
		case GpType.Long:
			SerializeLong(dout, serObject.Get<long>(), setType);
			return;
		case GpType.Float:
			SerializeFloat(dout, serObject.Get<float>(), setType);
			return;
		case GpType.Double:
			SerializeDouble(dout, serObject.Get<double>(), setType);
			return;
		case GpType.Hashtable:
			SerializeHashTable(dout, (PhotonHashtable)serObject, setType);
			return;
		case GpType.ByteArray:
			SerializeByteArray(dout, (byte[])serObject, setType);
			return;
		case GpType.ObjectArray:
			SerializeObjectArray(dout, (IList)serObject, setType);
			return;
		case GpType.Array:
			if (serObject is int[])
			{
				SerializeIntArrayOptimized(dout, (int[])serObject, setType);
			}
			else if (type.GetElementType() == typeof(object))
			{
				SerializeObjectArray(dout, serObject as object[], setType);
			}
			else
			{
				SerializeArray(dout, (Array)serObject, setType);
			}
			return;
		case GpType.Dictionary:
			SerializeDictionary(dout, (IDictionary)serObject, setType);
			return;
		case GpType.EventData:
			SerializeEventData(dout, (EventData)serObject, setType);
			return;
		case GpType.OperationResponse:
			SerializeOperationResponse(dout, (OperationResponse)serObject, setType);
			return;
		case GpType.OperationRequest:
			SerializeOperationRequest(dout, (OperationRequest)serObject, setType);
			return;
		}
		if (serObject is ArraySegment<byte> seg)
		{
			SerializeByteArraySegment(dout, seg.Array, seg.Offset, seg.Count, setType);
		}
		else if (!SerializeCustom(dout, serObject))
		{
			if (serObject is StructWrapper)
			{
				throw new Exception("cannot serialize(): StructWrapper<" + (serObject as StructWrapper).ttype.Name + ">");
			}
			throw new Exception("cannot serialize(): " + type);
		}
	}

	private void SerializeByte(StreamBuffer dout, byte serObject, bool setType)
	{
		if (setType)
		{
			dout.WriteByte(98);
		}
		dout.WriteByte(serObject);
	}

	private void SerializeBoolean(StreamBuffer dout, bool serObject, bool setType)
	{
		if (setType)
		{
			dout.WriteByte(111);
		}
		dout.WriteByte(serObject ? ((byte)1) : ((byte)0));
	}

	public override void SerializeShort(StreamBuffer dout, short serObject, bool setType)
	{
		if (setType)
		{
			dout.WriteByte(107);
		}
		lock (memShort)
		{
			byte[] temp = memShort;
			temp[0] = (byte)(serObject >> 8);
			temp[1] = (byte)serObject;
			dout.Write(temp, 0, 2);
		}
	}

	public void SerializeLengthAsShort(StreamBuffer dout, int serObject, string type)
	{
		if (serObject > 32767 || serObject < 0)
		{
			throw new NotSupportedException($"Exceeding 32767 (short.MaxValue) entries are not supported. Failed writing {type}. Length: {serObject}");
		}
		lock (memShort)
		{
			byte[] temp = memShort;
			temp[0] = (byte)(serObject >> 8);
			temp[1] = (byte)serObject;
			dout.Write(temp, 0, 2);
		}
	}

	private void SerializeInteger(StreamBuffer dout, int serObject, bool setType)
	{
		if (setType)
		{
			dout.WriteByte(105);
		}
		lock (memInteger)
		{
			byte[] buff = memInteger;
			buff[0] = (byte)(serObject >> 24);
			buff[1] = (byte)(serObject >> 16);
			buff[2] = (byte)(serObject >> 8);
			buff[3] = (byte)serObject;
			dout.Write(buff, 0, 4);
		}
	}

	private void SerializeLong(StreamBuffer dout, long serObject, bool setType)
	{
		if (setType)
		{
			dout.WriteByte(108);
		}
		lock (memLongBlock)
		{
			memLongBlock[0] = serObject;
			Buffer.BlockCopy(memLongBlock, 0, memLongBlockBytes, 0, 8);
			byte[] data = memLongBlockBytes;
			if (BitConverter.IsLittleEndian)
			{
				byte temp0 = data[0];
				byte temp1 = data[1];
				byte temp2 = data[2];
				byte temp3 = data[3];
				data[0] = data[7];
				data[1] = data[6];
				data[2] = data[5];
				data[3] = data[4];
				data[4] = temp3;
				data[5] = temp2;
				data[6] = temp1;
				data[7] = temp0;
			}
			dout.Write(data, 0, 8);
		}
	}

	private void SerializeFloat(StreamBuffer dout, float serObject, bool setType)
	{
		if (setType)
		{
			dout.WriteByte(102);
		}
		lock (memFloatBlockBytes)
		{
			memFloatBlock[0] = serObject;
			Buffer.BlockCopy(memFloatBlock, 0, memFloatBlockBytes, 0, 4);
			if (BitConverter.IsLittleEndian)
			{
				byte temp0 = memFloatBlockBytes[0];
				byte temp1 = memFloatBlockBytes[1];
				memFloatBlockBytes[0] = memFloatBlockBytes[3];
				memFloatBlockBytes[1] = memFloatBlockBytes[2];
				memFloatBlockBytes[2] = temp1;
				memFloatBlockBytes[3] = temp0;
			}
			dout.Write(memFloatBlockBytes, 0, 4);
		}
	}

	private void SerializeDouble(StreamBuffer dout, double serObject, bool setType)
	{
		if (setType)
		{
			dout.WriteByte(100);
		}
		lock (memDoubleBlockBytes)
		{
			memDoubleBlock[0] = serObject;
			Buffer.BlockCopy(memDoubleBlock, 0, memDoubleBlockBytes, 0, 8);
			byte[] data = memDoubleBlockBytes;
			if (BitConverter.IsLittleEndian)
			{
				byte temp0 = data[0];
				byte temp1 = data[1];
				byte temp2 = data[2];
				byte temp3 = data[3];
				data[0] = data[7];
				data[1] = data[6];
				data[2] = data[5];
				data[3] = data[4];
				data[4] = temp3;
				data[5] = temp2;
				data[6] = temp1;
				data[7] = temp0;
			}
			dout.Write(data, 0, 8);
		}
	}

	public override void SerializeString(StreamBuffer stream, string value, bool setType)
	{
		if (setType)
		{
			stream.WriteByte(115);
		}
		int count = Encoding.UTF8.GetByteCount(value);
		if (count > 32767)
		{
			throw new NotSupportedException("Strings that exceed a UTF8-encoded byte-length of 32767 (short.MaxValue) are not supported. Yours is: " + count);
		}
		SerializeLengthAsShort(stream, count, "String");
		int offset = 0;
		byte[] streamBuffer = stream.GetBufferAndAdvance(count, out offset);
		Encoding.UTF8.GetBytes(value, 0, value.Length, streamBuffer, offset);
	}

	private void SerializeArray(StreamBuffer dout, Array serObject, bool setType)
	{
		if (setType)
		{
			dout.WriteByte(121);
		}
		SerializeLengthAsShort(dout, serObject.Length, "Array");
		Type elementType = serObject.GetType().GetElementType();
		GpType contentTypeCode = GetCodeOfType(elementType);
		if (contentTypeCode != GpType.Unknown)
		{
			dout.WriteByte((byte)contentTypeCode);
			if (contentTypeCode == GpType.Dictionary)
			{
				SerializeDictionaryHeader(dout, serObject, out var setKeyType, out var setValueType);
				for (int index = 0; index < serObject.Length; index++)
				{
					object element = serObject.GetValue(index);
					SerializeDictionaryElements(dout, element, setKeyType, setValueType);
				}
			}
			else
			{
				for (int i = 0; i < serObject.Length; i++)
				{
					object o = serObject.GetValue(i);
					Serialize(dout, o, setType: false);
				}
			}
			return;
		}
		if (Protocol.TypeDict.TryGetValue(elementType, out var customType))
		{
			dout.WriteByte(99);
			dout.WriteByte(customType.Code);
			for (int j = 0; j < serObject.Length; j++)
			{
				object obj = serObject.GetValue(j);
				if (customType.SerializeStreamFunction == null)
				{
					throw new NullReferenceException($"Custom Type array serialization failed. SerializeStreamFunction is null for Type: {customType.Type} Code: {customType.Code}.");
				}
				int posOfLengthInfo = dout.Position;
				dout.Position += 2;
				short serializedLength = customType.SerializeStreamFunction(dout, obj);
				long newPos = dout.Position;
				dout.Position = posOfLengthInfo;
				short serObject2 = serializedLength;
				byte code = customType.Code;
				SerializeLengthAsShort(dout, serObject2, "Custom Type " + code);
				dout.Position += serializedLength;
				if (dout.Position != newPos)
				{
					throw new Exception("Serialization failed. Stream position corrupted. Should be " + newPos + " is now: " + dout.Position + " serializedLength: " + serializedLength);
				}
			}
			return;
		}
		throw new NotSupportedException("cannot serialize array of type " + elementType);
	}

	private void SerializeByteArray(StreamBuffer dout, byte[] serObject, bool setType)
	{
		if (setType)
		{
			dout.WriteByte(120);
		}
		SerializeInteger(dout, serObject.Length, setType: false);
		dout.Write(serObject, 0, serObject.Length);
	}

	private void SerializeByteArraySegment(StreamBuffer dout, byte[] serObject, int offset, int count, bool setType)
	{
		if (setType)
		{
			dout.WriteByte(120);
		}
		SerializeInteger(dout, count, setType: false);
		dout.Write(serObject, offset, count);
	}

	private void SerializeIntArrayOptimized(StreamBuffer inWriter, int[] serObject, bool setType)
	{
		if (setType)
		{
			inWriter.WriteByte(121);
		}
		SerializeLengthAsShort(inWriter, serObject.Length, "int[]");
		inWriter.WriteByte(105);
		byte[] temp = new byte[serObject.Length * 4];
		int x = 0;
		for (int i = 0; i < serObject.Length; i++)
		{
			temp[x++] = (byte)(serObject[i] >> 24);
			temp[x++] = (byte)(serObject[i] >> 16);
			temp[x++] = (byte)(serObject[i] >> 8);
			temp[x++] = (byte)serObject[i];
		}
		inWriter.Write(temp, 0, temp.Length);
	}

	private void SerializeStringArray(StreamBuffer dout, string[] serObject, bool setType)
	{
		if (setType)
		{
			dout.WriteByte(97);
		}
		SerializeLengthAsShort(dout, serObject.Length, "string[]");
		for (int i = 0; i < serObject.Length; i++)
		{
			SerializeString(dout, serObject[i], setType: false);
		}
	}

	private void SerializeObjectArray(StreamBuffer dout, IList objects, bool setType)
	{
		if (setType)
		{
			dout.WriteByte(122);
		}
		SerializeLengthAsShort(dout, objects.Count, "object[]");
		for (int index = 0; index < objects.Count; index++)
		{
			object obj = objects[index];
			Serialize(dout, obj, setType: true);
		}
	}

	private void SerializeHashTable(StreamBuffer dout, PhotonHashtable serObject, bool setType)
	{
		if (setType)
		{
			dout.WriteByte(104);
		}
		SerializeLengthAsShort(dout, serObject.Count, "PhotonHashtable");
		foreach (object entry in serObject.Keys)
		{
			Serialize(dout, entry, setType: true);
			Serialize(dout, serObject[entry], setType: true);
		}
	}

	private void SerializeDictionary(StreamBuffer dout, IDictionary serObject, bool setType)
	{
		if (setType)
		{
			dout.WriteByte(68);
		}
		SerializeDictionaryHeader(dout, serObject, out var setKeyType, out var setValueType);
		SerializeDictionaryElements(dout, serObject, setKeyType, setValueType);
	}

	private void SerializeDictionaryHeader(StreamBuffer writer, Type dictType)
	{
		SerializeDictionaryHeader(writer, dictType, out var _, out var _);
	}

	private void SerializeDictionaryHeader(StreamBuffer writer, object dict, out bool setKeyType, out bool setValueType)
	{
		Type[] types = dict.GetType().GetGenericArguments();
		setKeyType = types[0] == typeof(object);
		setValueType = types[1] == typeof(object);
		if (setKeyType)
		{
			writer.WriteByte(0);
		}
		else
		{
			GpType keyType = GetCodeOfType(types[0]);
			if (keyType == GpType.Unknown || keyType == GpType.Dictionary)
			{
				throw new Exception("Unexpected - cannot serialize Dictionary with key type: " + types[0]);
			}
			writer.WriteByte((byte)keyType);
		}
		if (setValueType)
		{
			writer.WriteByte(0);
			return;
		}
		GpType valueType = GetCodeOfType(types[1]);
		if (valueType == GpType.Unknown)
		{
			throw new Exception("Unexpected - cannot serialize Dictionary with value type: " + types[1]);
		}
		writer.WriteByte((byte)valueType);
		if (valueType == GpType.Dictionary)
		{
			SerializeDictionaryHeader(writer, types[1]);
		}
	}

	private void SerializeDictionaryElements(StreamBuffer writer, object dict, bool setKeyType, bool setValueType)
	{
		IDictionary d = (IDictionary)dict;
		SerializeLengthAsShort(writer, d.Count, "Dictionary elements");
		foreach (DictionaryEntry entry in d)
		{
			if (!setValueType && entry.Value == null)
			{
				throw new Exception("Can't serialize null in Dictionary with specific value-type.");
			}
			if (!setKeyType && entry.Key == null)
			{
				throw new Exception("Can't serialize null in Dictionary with specific key-type.");
			}
			Serialize(writer, entry.Key, setKeyType);
			Serialize(writer, entry.Value, setValueType);
		}
	}

	public override object Deserialize(StreamBuffer din, byte type, DeserializationFlags flags = DeserializationFlags.None)
	{
		switch (type)
		{
		case 105:
			return DeserializeInteger(din);
		case 115:
			return DeserializeString(din);
		case 97:
			return DeserializeStringArray(din);
		case 120:
			return DeserializeByteArray(din);
		case 110:
			return DeserializeIntArray(din);
		case 104:
			return DeserializeHashTable(din);
		case 68:
			return DeserializeDictionary(din);
		case 111:
			return DeserializeBoolean(din);
		case 107:
			return DeserializeShort(din);
		case 108:
			return DeserializeLong(din);
		case 98:
			return DeserializeByte(din);
		case 102:
			return DeserializeFloat(din);
		case 100:
			return DeserializeDouble(din);
		case 121:
			return DeserializeArray(din);
		case 99:
		{
			byte typeCode = din.ReadByte();
			return DeserializeCustom(din, typeCode);
		}
		case 122:
			return DeserializeObjectArray(din);
		case 101:
			return DeserializeEventData(din);
		case 113:
			return DeserializeOperationRequest(din, flags);
		case 112:
			return DeserializeOperationResponse(din, flags);
		case 0:
		case 42:
			return null;
		default:
			throw new Exception("Deserialize(): " + type + " pos: " + din.Position + " bytes: " + din.Length + ". " + SupportClass.ByteArrayToString(din.GetBuffer()));
		}
	}

	public override byte DeserializeByte(StreamBuffer din)
	{
		return din.ReadByte();
	}

	private bool DeserializeBoolean(StreamBuffer din)
	{
		return din.ReadByte() != 0;
	}

	public override short DeserializeShort(StreamBuffer din)
	{
		lock (memShort)
		{
			byte[] data = memShort;
			din.Read(data, 0, 2);
			return (short)((data[0] << 8) | data[1]);
		}
	}

	/// <summary>
	/// DeserializeInteger returns an Integer typed value from the given stream.
	/// </summary>
	private int DeserializeInteger(StreamBuffer din)
	{
		lock (memInteger)
		{
			byte[] data = memInteger;
			din.Read(data, 0, 4);
			return (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
		}
	}

	private long DeserializeLong(StreamBuffer din)
	{
		lock (memLong)
		{
			byte[] data = memLong;
			din.Read(data, 0, 8);
			if (BitConverter.IsLittleEndian)
			{
				return (long)(((ulong)data[0] << 56) | ((ulong)data[1] << 48) | ((ulong)data[2] << 40) | ((ulong)data[3] << 32) | ((ulong)data[4] << 24) | ((ulong)data[5] << 16) | ((ulong)data[6] << 8) | data[7]);
			}
			return BitConverter.ToInt64(data, 0);
		}
	}

	private float DeserializeFloat(StreamBuffer din)
	{
		lock (memFloat)
		{
			byte[] data = memFloat;
			din.Read(data, 0, 4);
			if (BitConverter.IsLittleEndian)
			{
				byte temp0 = data[0];
				byte temp1 = data[1];
				data[0] = data[3];
				data[1] = data[2];
				data[2] = temp1;
				data[3] = temp0;
			}
			return BitConverter.ToSingle(data, 0);
		}
	}

	private double DeserializeDouble(StreamBuffer din)
	{
		lock (memDouble)
		{
			byte[] data = memDouble;
			din.Read(data, 0, 8);
			if (BitConverter.IsLittleEndian)
			{
				byte temp0 = data[0];
				byte temp1 = data[1];
				byte temp2 = data[2];
				byte temp3 = data[3];
				data[0] = data[7];
				data[1] = data[6];
				data[2] = data[5];
				data[3] = data[4];
				data[4] = temp3;
				data[5] = temp2;
				data[6] = temp1;
				data[7] = temp0;
			}
			return BitConverter.ToDouble(data, 0);
		}
	}

	private string DeserializeString(StreamBuffer din)
	{
		short length = DeserializeShort(din);
		if (length == 0)
		{
			return string.Empty;
		}
		if (length < 0)
		{
			throw new NotSupportedException("Received string type with unsupported length: " + length);
		}
		int offset = 0;
		byte[] bytes = din.GetBufferAndAdvance(length, out offset);
		return Encoding.UTF8.GetString(bytes, offset, length);
	}

	private Array DeserializeArray(StreamBuffer din)
	{
		short arrayLength = DeserializeShort(din);
		byte valuesType = din.ReadByte();
		Array resultArray = null;
		switch (valuesType)
		{
		case 121:
		{
			Array innerArray = DeserializeArray(din);
			resultArray = Array.CreateInstance(innerArray.GetType(), arrayLength);
			resultArray.SetValue(innerArray, 0);
			for (short i2 = 1; i2 < arrayLength; i2++)
			{
				innerArray = DeserializeArray(din);
				resultArray.SetValue(innerArray, i2);
			}
			break;
		}
		case 120:
		{
			resultArray = Array.CreateInstance(typeof(byte[]), arrayLength);
			for (short i3 = 0; i3 < arrayLength; i3++)
			{
				Array innerArray2 = DeserializeByteArray(din);
				resultArray.SetValue(innerArray2, i3);
			}
			break;
		}
		case 98:
			resultArray = DeserializeByteArray(din, arrayLength);
			break;
		case 105:
			resultArray = DeserializeIntArray(din, arrayLength);
			break;
		case 99:
		{
			byte customTypeCode = din.ReadByte();
			if (Protocol.CodeDict.TryGetValue(customTypeCode, out var customType))
			{
				resultArray = Array.CreateInstance(customType.Type, arrayLength);
				for (int j = 0; j < arrayLength; j++)
				{
					short objLength = DeserializeShort(din);
					if (objLength < 0)
					{
						throw new InvalidDataException($"DeserializeArray read negative objLength value: {objLength} before position: {din.Position}");
					}
					if (customType.DeserializeStreamFunction == null)
					{
						throw new NullReferenceException($"Custom Type array deserialization failed. DeserializeStreamFunction is null for Type: {customType.Type} Code: {customType.Code}.");
					}
					int pos = din.Position;
					object result2 = customType.DeserializeStreamFunction(din, objLength);
					if (din.Position - pos != objLength)
					{
						din.Position = pos + objLength;
					}
					resultArray.SetValue(result2, j);
				}
				break;
			}
			throw new Exception($"Cannot find deserializer for custom type: {customTypeCode}");
		}
		case 68:
		{
			Array result = null;
			DeserializeDictionaryArray(din, arrayLength, out result);
			return result;
		}
		default:
		{
			resultArray = CreateArrayByType(valuesType, arrayLength);
			for (short i = 0; i < arrayLength; i++)
			{
				resultArray.SetValue(Deserialize(din, valuesType), i);
			}
			break;
		}
		}
		return resultArray;
	}

	private byte[] DeserializeByteArray(StreamBuffer din, int size = -1)
	{
		if (size == -1)
		{
			size = DeserializeInteger(din);
		}
		byte[] retVal = new byte[size];
		din.Read(retVal, 0, size);
		return retVal;
	}

	private int[] DeserializeIntArray(StreamBuffer din, int size = -1)
	{
		if (size == -1)
		{
			size = DeserializeInteger(din);
		}
		int[] retVal = new int[size];
		for (int i = 0; i < size; i++)
		{
			retVal[i] = DeserializeInteger(din);
		}
		return retVal;
	}

	private string[] DeserializeStringArray(StreamBuffer din)
	{
		int size = DeserializeShort(din);
		string[] val = new string[size];
		for (int i = 0; i < size; i++)
		{
			val[i] = DeserializeString(din);
		}
		return val;
	}

	private object[] DeserializeObjectArray(StreamBuffer din)
	{
		short arrayLength = DeserializeShort(din);
		object[] resultArray = new object[arrayLength];
		for (int i = 0; i < arrayLength; i++)
		{
			byte typeCode = din.ReadByte();
			resultArray[i] = Deserialize(din, typeCode);
		}
		return resultArray;
	}

	private PhotonHashtable DeserializeHashTable(StreamBuffer din)
	{
		int size = DeserializeShort(din);
		PhotonHashtable value = new PhotonHashtable(size);
		for (int i = 0; i < size; i++)
		{
			object serKey = Deserialize(din, din.ReadByte());
			object serValue = Deserialize(din, din.ReadByte());
			if (serKey != null)
			{
				value[serKey] = serValue;
			}
		}
		return value;
	}

	private IDictionary DeserializeDictionary(StreamBuffer din)
	{
		byte keyType = din.ReadByte();
		byte valType = din.ReadByte();
		if (keyType == 68 || keyType == 121)
		{
			throw new NotSupportedException("Client serialization protocol 1.6 does not support nesting Dictionary or Arrays into Dictionary keys.");
		}
		if (valType == 68 || valType == 121)
		{
			throw new NotSupportedException("Client serialization protocol 1.6 does not support nesting Dictionary or Arrays into Dictionary values.");
		}
		int size = DeserializeShort(din);
		bool readKeyType = keyType == 0 || keyType == 42;
		bool readValType = valType == 0 || valType == 42;
		Type k = GetTypeOfCode(keyType);
		Type v = GetTypeOfCode(valType);
		IDictionary value = Activator.CreateInstance(typeof(Dictionary<, >).MakeGenericType(k, v)) as IDictionary;
		for (int i = 0; i < size; i++)
		{
			object serKey = Deserialize(din, readKeyType ? din.ReadByte() : keyType);
			object serValue = Deserialize(din, readValType ? din.ReadByte() : valType);
			if (serKey != null)
			{
				value.Add(serKey, serValue);
			}
		}
		return value;
	}

	private bool DeserializeDictionaryArray(StreamBuffer din, short size, out Array arrayResult)
	{
		byte keyTypeCode;
		byte valTypeCode;
		Type dictType = DeserializeDictionaryType(din, out keyTypeCode, out valTypeCode);
		arrayResult = Array.CreateInstance(dictType, size);
		for (short i = 0; i < size; i++)
		{
			if (!(Activator.CreateInstance(dictType) is IDictionary dict))
			{
				return false;
			}
			short dictSize = DeserializeShort(din);
			for (int j = 0; j < dictSize; j++)
			{
				object key;
				if (keyTypeCode != 0)
				{
					key = Deserialize(din, keyTypeCode);
				}
				else
				{
					byte type = din.ReadByte();
					key = Deserialize(din, type);
				}
				object value;
				if (valTypeCode != 0)
				{
					value = Deserialize(din, valTypeCode);
				}
				else
				{
					byte type2 = din.ReadByte();
					value = Deserialize(din, type2);
				}
				if (key != null)
				{
					dict.Add(key, value);
				}
			}
			arrayResult.SetValue(dict, i);
		}
		return true;
	}

	private Type DeserializeDictionaryType(StreamBuffer reader, out byte keyTypeCode, out byte valTypeCode)
	{
		keyTypeCode = reader.ReadByte();
		valTypeCode = reader.ReadByte();
		GpType keyType = (GpType)keyTypeCode;
		GpType valueType = (GpType)valTypeCode;
		Type keyClrType;
		switch (keyType)
		{
		case GpType.Unknown:
			keyClrType = typeof(object);
			break;
		case GpType.Dictionary:
		case GpType.Array:
			throw new NotSupportedException("Client serialization protocol 1.6 does not support nesting Dictionary or Arrays into Dictionary keys.");
		default:
			keyClrType = GetTypeOfCode(keyTypeCode);
			break;
		}
		Type valueClrType;
		switch (valueType)
		{
		case GpType.Unknown:
			valueClrType = typeof(object);
			break;
		case GpType.Dictionary:
		case GpType.Array:
			throw new NotSupportedException("Client serialization protocol 1.6 does not support nesting Dictionary or Arrays into Dictionary values.");
		default:
			valueClrType = GetTypeOfCode(valTypeCode);
			break;
		}
		return typeof(Dictionary<, >).MakeGenericType(keyClrType, valueClrType);
	}
}
