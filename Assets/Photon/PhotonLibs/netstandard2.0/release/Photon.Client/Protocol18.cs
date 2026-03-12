using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Photon.Client.StructWrapping;

namespace Photon.Client;

public class Protocol18 : Protocol
{
	public enum GpType : byte
	{
		/// <summary>Unkown. GpType: 0.</summary>
		Unknown = 0,
		/// <summary>Boolean. GpType: 2. See: BooleanFalse, BooleanTrue.</summary>
		Boolean = 2,
		/// <summary>Byte. GpType: 3.</summary>
		Byte = 3,
		/// <summary>Short. GpType: 4.</summary>
		Short = 4,
		/// <summary>32-bit floating-point value. GpType: 5.</summary>
		Float = 5,
		/// <summary>64-bit floating-point value. GpType: 6.</summary>
		Double = 6,
		/// <summary>String. GpType: 7.</summary>
		String = 7,
		/// <summary>Null value don't have types. GpType: 8.</summary>
		Null = 8,
		/// <summary>CompressedInt. GpType: 9.</summary>
		CompressedInt = 9,
		/// <summary>CompressedLong. GpType: 10.</summary>
		CompressedLong = 10,
		/// <summary>Int1. GpType: 11.</summary>
		Int1 = 11,
		/// <summary>Int1_. GpType: 12.</summary>
		Int1_ = 12,
		/// <summary>Int2. GpType: 13.</summary>
		Int2 = 13,
		/// <summary>Int2_. GpType: 14.</summary>
		Int2_ = 14,
		/// <summary>L1. GpType: 15.</summary>
		L1 = 15,
		/// <summary>L1_. GpType: 16.</summary>
		L1_ = 16,
		/// <summary>L2. GpType: 17.</summary>
		L2 = 17,
		/// <summary>L2_. GpType: 18.</summary>
		L2_ = 18,
		/// <summary>Custom Type. GpType: 19.</summary>
		Custom = 19,
		/// <summary>Custom Type Slim. GpType: 128 (0x80) and up.</summary>
		CustomTypeSlim = 128,
		/// <summary>Dictionary. GpType: 20.</summary>
		Dictionary = 20,
		/// <summary>PhotonHashtable. GpType: 21.</summary>
		Hashtable = 21,
		/// <summary>ObjectArray. GpType: 23.</summary>
		ObjectArray = 23,
		/// <summary>OperationRequest. GpType: 24.</summary>
		OperationRequest = 24,
		/// <summary>OperationResponse. GpType: 25.</summary>
		OperationResponse = 25,
		/// <summary>EventData. GpType: 26.</summary>
		EventData = 26,
		/// <summary>Boolean False. GpType: 27.</summary>
		BooleanFalse = 27,
		/// <summary>Boolean True. GpType: 28.</summary>
		BooleanTrue = 28,
		/// <summary>ShortZero. GpType: 29.</summary>
		ShortZero = 29,
		/// <summary>IntZero. GpType: 30.</summary>
		IntZero = 30,
		/// <summary>LongZero. GpType: 3.</summary>
		LongZero = 31,
		/// <summary>FloatZero. GpType: 32.</summary>
		FloatZero = 32,
		/// <summary>DoubleZero. GpType: 33.</summary>
		DoubleZero = 33,
		/// <summary>ByteZero. GpType: 34.</summary>
		ByteZero = 34,
		/// <summary>Array for nested Arrays. GpType: 64 (0x40). Element count and type follows.</summary>
		Array = 64,
		BooleanArray = 66,
		ByteArray = 67,
		ShortArray = 68,
		DoubleArray = 70,
		FloatArray = 69,
		StringArray = 71,
		HashtableArray = 85,
		DictionaryArray = 84,
		CustomTypeArray = 83,
		CompressedIntArray = 73,
		CompressedLongArray = 74
	}

	private readonly byte[] versionBytes = new byte[2] { 1, 8 };

	private static readonly byte[] boolMasks = new byte[8] { 1, 2, 4, 8, 16, 32, 64, 128 };

	private readonly double[] memDoubleBlock = new double[1];

	private readonly float[] memFloatBlock = new float[1];

	private readonly byte[] memCustomTypeBodyLengthSerialized = new byte[5];

	private readonly byte[] memCompressedUInt32 = new byte[5];

	private byte[] memCompressedUInt64 = new byte[10];

	public override string ProtocolType => "GpBinaryV18";

	public override byte[] VersionBytes => versionBytes;

	public override void Serialize(StreamBuffer dout, object serObject, bool setType)
	{
		Write(dout, serObject, setType);
	}

	public override void SerializeShort(StreamBuffer dout, short serObject, bool setType)
	{
		WriteInt16(dout, serObject, setType);
	}

	public override void SerializeString(StreamBuffer dout, string serObject, bool setType)
	{
		WriteString(dout, serObject, setType);
	}

	public override object Deserialize(StreamBuffer din, byte type, DeserializationFlags flags = DeserializationFlags.None)
	{
		return Read(din, type);
	}

	public override short DeserializeShort(StreamBuffer din)
	{
		return ReadInt16(din);
	}

	public override byte DeserializeByte(StreamBuffer din)
	{
		return ReadByte(din);
	}

	private static Type GetAllowedDictionaryKeyTypes(GpType gpType)
	{
		switch (gpType)
		{
		case GpType.Byte:
		case GpType.ByteZero:
			return typeof(byte);
		case GpType.Short:
		case GpType.ShortZero:
			return typeof(short);
		case GpType.Float:
		case GpType.FloatZero:
			return typeof(float);
		case GpType.Double:
		case GpType.DoubleZero:
			return typeof(double);
		case GpType.String:
			return typeof(string);
		case GpType.CompressedInt:
		case GpType.Int1:
		case GpType.Int1_:
		case GpType.Int2:
		case GpType.Int2_:
		case GpType.IntZero:
			return typeof(int);
		case GpType.CompressedLong:
		case GpType.L1:
		case GpType.L1_:
		case GpType.L2:
		case GpType.L2_:
		case GpType.LongZero:
			return typeof(long);
		default:
			throw new Exception($"{gpType} is not a valid Type as Dictionary key.");
		}
	}

	private static Type GetClrArrayType(GpType gpType)
	{
		switch (gpType)
		{
		case GpType.Boolean:
		case GpType.BooleanFalse:
		case GpType.BooleanTrue:
			return typeof(bool);
		case GpType.Byte:
		case GpType.ByteZero:
			return typeof(byte);
		case GpType.Short:
		case GpType.ShortZero:
			return typeof(short);
		case GpType.Float:
		case GpType.FloatZero:
			return typeof(float);
		case GpType.Double:
		case GpType.DoubleZero:
			return typeof(double);
		case GpType.String:
			return typeof(string);
		case GpType.CompressedInt:
		case GpType.Int1:
		case GpType.Int1_:
		case GpType.Int2:
		case GpType.Int2_:
		case GpType.IntZero:
			return typeof(int);
		case GpType.CompressedLong:
		case GpType.L1:
		case GpType.L1_:
		case GpType.L2:
		case GpType.L2_:
		case GpType.LongZero:
			return typeof(long);
		case GpType.Hashtable:
			return typeof(PhotonHashtable);
		case GpType.OperationRequest:
			return typeof(OperationRequest);
		case GpType.OperationResponse:
			return typeof(OperationResponse);
		case GpType.EventData:
			return typeof(EventData);
		case GpType.BooleanArray:
			return typeof(bool[]);
		case GpType.ByteArray:
			return typeof(byte[]);
		case GpType.ShortArray:
			return typeof(short[]);
		case GpType.DoubleArray:
			return typeof(double[]);
		case GpType.FloatArray:
			return typeof(float[]);
		case GpType.StringArray:
			return typeof(string[]);
		case GpType.HashtableArray:
			return typeof(PhotonHashtable[]);
		case GpType.CompressedIntArray:
			return typeof(int[]);
		case GpType.CompressedLongArray:
			return typeof(long[]);
		default:
			return null;
		}
	}

	private GpType GetCodeOfType(Type type)
	{
		if (type == null)
		{
			return GpType.Null;
		}
		if (type == typeof(StructWrapper<>))
		{
			return GpType.Unknown;
		}
		if (type.IsPrimitive || type.IsEnum)
		{
			TypeCode typeCode = Type.GetTypeCode(type);
			return GetCodeOfTypeCode(typeCode);
		}
		if (type == typeof(string))
		{
			return GpType.String;
		}
		if (type.IsArray)
		{
			Type elementType = type.GetElementType();
			if (elementType == null)
			{
				throw new InvalidDataException($"Arrays of type {type} are not supported");
			}
			if (elementType.IsPrimitive)
			{
				switch (Type.GetTypeCode(elementType))
				{
				case TypeCode.Byte:
					return GpType.ByteArray;
				case TypeCode.Int16:
					return GpType.ShortArray;
				case TypeCode.Int32:
					return GpType.CompressedIntArray;
				case TypeCode.Int64:
					return GpType.CompressedLongArray;
				case TypeCode.Boolean:
					return GpType.BooleanArray;
				case TypeCode.Single:
					return GpType.FloatArray;
				case TypeCode.Double:
					return GpType.DoubleArray;
				}
			}
			if (elementType.IsArray)
			{
				return GpType.Array;
			}
			if (elementType == typeof(string))
			{
				return GpType.StringArray;
			}
			if (elementType == typeof(object) || elementType == typeof(StructWrapper))
			{
				return GpType.ObjectArray;
			}
			if (elementType == typeof(PhotonHashtable))
			{
				return GpType.HashtableArray;
			}
			if (elementType.IsGenericType && typeof(Dictionary<, >) == elementType.GetGenericTypeDefinition())
			{
				return GpType.DictionaryArray;
			}
			return GpType.CustomTypeArray;
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

	private GpType GetCodeOfTypeCode(TypeCode type)
	{
		return type switch
		{
			TypeCode.Byte => GpType.Byte, 
			TypeCode.String => GpType.String, 
			TypeCode.Boolean => GpType.Boolean, 
			TypeCode.Int16 => GpType.Short, 
			TypeCode.Int32 => GpType.CompressedInt, 
			TypeCode.Int64 => GpType.CompressedLong, 
			TypeCode.Single => GpType.Float, 
			TypeCode.Double => GpType.Double, 
			_ => GpType.Unknown, 
		};
	}

	private object Read(StreamBuffer stream, DeserializationFlags flags, ParameterDictionary parameters)
	{
		return Read(stream, ReadByte(stream), flags, parameters);
	}

	private object Read(StreamBuffer stream, byte gpType, DeserializationFlags flags = DeserializationFlags.None, ParameterDictionary parameters = null)
	{
		int unflagged = ((gpType >= 128) ? (gpType - 128) : gpType);
		unflagged = ((unflagged >= 64) ? (unflagged - 64) : unflagged);
		bool wrapStructs = (flags & DeserializationFlags.WrapIncomingStructs) == DeserializationFlags.WrapIncomingStructs;
		if (gpType >= 128 && gpType <= 228)
		{
			return ReadCustomType(stream, gpType);
		}
		switch ((GpType)gpType)
		{
		case GpType.Boolean:
		{
			bool val5 = ReadBoolean(stream);
			if (!wrapStructs)
			{
				return val5;
			}
			return parameters.wrapperPools.Acquire(val5);
		}
		case GpType.BooleanTrue:
		{
			bool val18 = true;
			if (!wrapStructs)
			{
				return val18;
			}
			return parameters.wrapperPools.Acquire(val18);
		}
		case GpType.BooleanFalse:
		{
			bool val8 = false;
			if (!wrapStructs)
			{
				return val8;
			}
			return parameters.wrapperPools.Acquire(val8);
		}
		case GpType.Byte:
		{
			byte val14 = ReadByte(stream);
			if (!wrapStructs)
			{
				return val14;
			}
			return parameters.wrapperPools.Acquire(val14);
		}
		case GpType.ByteZero:
		{
			byte val22 = 0;
			if (!wrapStructs)
			{
				return val22;
			}
			return parameters.wrapperPools.Acquire(val22);
		}
		case GpType.Short:
		{
			short val12 = ReadInt16(stream);
			if (!wrapStructs)
			{
				return val12;
			}
			return parameters.wrapperPools.Acquire(val12);
		}
		case GpType.ShortZero:
		{
			short val23 = 0;
			if (!wrapStructs)
			{
				return val23;
			}
			return parameters.wrapperPools.Acquire(val23);
		}
		case GpType.Float:
		{
			float val17 = ReadSingle(stream);
			if (!wrapStructs)
			{
				return val17;
			}
			return parameters.wrapperPools.Acquire(val17);
		}
		case GpType.FloatZero:
		{
			float val9 = 0f;
			if (!wrapStructs)
			{
				return val9;
			}
			return parameters.wrapperPools.Acquire(val9);
		}
		case GpType.Double:
		{
			double val3 = ReadDouble(stream);
			if (!wrapStructs)
			{
				return val3;
			}
			return parameters.wrapperPools.Acquire(val3);
		}
		case GpType.DoubleZero:
		{
			double val20 = 0.0;
			if (!wrapStructs)
			{
				return val20;
			}
			return parameters.wrapperPools.Acquire(val20);
		}
		case GpType.String:
			return ReadString(stream);
		case GpType.Int1:
		{
			int val15 = ReadInt1(stream, signNegative: false);
			if (!wrapStructs)
			{
				return val15;
			}
			return parameters.wrapperPools.Acquire(val15);
		}
		case GpType.Int2:
		{
			int val11 = ReadInt2(stream, signNegative: false);
			if (!wrapStructs)
			{
				return val11;
			}
			return parameters.wrapperPools.Acquire(val11);
		}
		case GpType.Int1_:
		{
			int val6 = ReadInt1(stream, signNegative: true);
			if (!wrapStructs)
			{
				return val6;
			}
			return parameters.wrapperPools.Acquire(val6);
		}
		case GpType.Int2_:
		{
			int val2 = ReadInt2(stream, signNegative: true);
			if (!wrapStructs)
			{
				return val2;
			}
			return parameters.wrapperPools.Acquire(val2);
		}
		case GpType.CompressedInt:
		{
			int val21 = ReadCompressedInt32(stream);
			if (!wrapStructs)
			{
				return val21;
			}
			return parameters.wrapperPools.Acquire(val21);
		}
		case GpType.IntZero:
		{
			int val19 = 0;
			if (!wrapStructs)
			{
				return val19;
			}
			return parameters.wrapperPools.Acquire(val19);
		}
		case GpType.L1:
		{
			long val16 = ReadInt1(stream, signNegative: false);
			if (!wrapStructs)
			{
				return val16;
			}
			return parameters.wrapperPools.Acquire(val16);
		}
		case GpType.L2:
		{
			long val13 = ReadInt2(stream, signNegative: false);
			if (!wrapStructs)
			{
				return val13;
			}
			return parameters.wrapperPools.Acquire(val13);
		}
		case GpType.L1_:
		{
			long val10 = ReadInt1(stream, signNegative: true);
			if (!wrapStructs)
			{
				return val10;
			}
			return parameters.wrapperPools.Acquire(val10);
		}
		case GpType.L2_:
		{
			long val7 = ReadInt2(stream, signNegative: true);
			if (!wrapStructs)
			{
				return val7;
			}
			return parameters.wrapperPools.Acquire(val7);
		}
		case GpType.CompressedLong:
		{
			long val4 = ReadCompressedInt64(stream);
			if (!wrapStructs)
			{
				return val4;
			}
			return parameters.wrapperPools.Acquire(val4);
		}
		case GpType.LongZero:
		{
			long val = 0L;
			if (!wrapStructs)
			{
				return val;
			}
			return parameters.wrapperPools.Acquire(val);
		}
		case GpType.Hashtable:
			return ReadHashtable(stream, flags, parameters);
		case GpType.Dictionary:
			return ReadDictionary(stream, flags, parameters);
		case GpType.Custom:
			return ReadCustomType(stream, 0);
		case GpType.OperationRequest:
			return DeserializeOperationRequest(stream);
		case GpType.OperationResponse:
			return DeserializeOperationResponse(stream, flags);
		case GpType.EventData:
			return DeserializeEventData(stream);
		case GpType.ObjectArray:
			return ReadObjectArray(stream, flags, parameters);
		case GpType.BooleanArray:
			return ReadBooleanArray(stream);
		case GpType.ByteArray:
			return ReadByteArray(stream);
		case GpType.ShortArray:
			return ReadInt16Array(stream);
		case GpType.DoubleArray:
			return ReadDoubleArray(stream);
		case GpType.FloatArray:
			return ReadSingleArray(stream);
		case GpType.StringArray:
			return ReadStringArray(stream);
		case GpType.HashtableArray:
			return ReadHashtableArray(stream, flags, parameters);
		case GpType.DictionaryArray:
			return ReadDictionaryArray(stream, flags, parameters);
		case GpType.CustomTypeArray:
			return ReadCustomTypeArray(stream);
		case GpType.CompressedIntArray:
			return ReadCompressedInt32Array(stream);
		case GpType.CompressedLongArray:
			return ReadCompressedInt64Array(stream);
		case GpType.Array:
			return ReadArrayInArray(stream, flags, parameters);
		case GpType.Null:
			return null;
		default:
			throw new InvalidDataException(string.Format("GpTypeCode not found: {0}(0x{0:X}). Is not a CustomType either. Pos: {1} Available: {2}", gpType, stream.Position, stream.Available));
		}
	}

	internal bool ReadBoolean(StreamBuffer stream)
	{
		return stream.ReadByte() > 0;
	}

	internal byte ReadByte(StreamBuffer stream)
	{
		return stream.ReadByte();
	}

	internal short ReadInt16(StreamBuffer stream)
	{
		int pos;
		byte[] b = stream.GetBufferAndAdvance(2, out pos);
		return (short)(b[pos++] | (b[pos] << 8));
	}

	internal ushort ReadUShort(StreamBuffer stream)
	{
		int pos;
		byte[] b = stream.GetBufferAndAdvance(2, out pos);
		return (ushort)(b[pos++] | (b[pos] << 8));
	}

	internal int ReadInt32(StreamBuffer stream)
	{
		int pos;
		byte[] b = stream.GetBufferAndAdvance(4, out pos);
		return (b[pos++] << 24) | (b[pos++] << 16) | (b[pos++] << 8) | b[pos];
	}

	internal long ReadInt64(StreamBuffer stream)
	{
		int pos;
		byte[] b = stream.GetBufferAndAdvance(4, out pos);
		return (long)(((ulong)b[pos++] << 56) | ((ulong)b[pos++] << 48) | ((ulong)b[pos++] << 40) | ((ulong)b[pos++] << 32) | ((ulong)b[pos++] << 24) | ((ulong)b[pos++] << 16) | ((ulong)b[pos++] << 8) | b[pos]);
	}

	internal float ReadSingle(StreamBuffer stream)
	{
		int pos;
		return BitConverter.ToSingle(stream.GetBufferAndAdvance(4, out pos), pos);
	}

	internal double ReadDouble(StreamBuffer stream)
	{
		int pos;
		return BitConverter.ToDouble(stream.GetBufferAndAdvance(8, out pos), pos);
	}

	internal ByteArraySlice ReadNonAllocByteArray(StreamBuffer stream)
	{
		uint bytecount = ReadCompressedUInt32(stream);
		ByteArraySlice wrapper = ByteArraySlicePool.Acquire((int)bytecount);
		stream.Read(wrapper.Buffer, 0, (int)bytecount);
		wrapper.Count = (int)bytecount;
		return wrapper;
	}

	internal byte[] ReadByteArray(StreamBuffer stream)
	{
		uint length = ReadCompressedUInt32(stream);
		byte[] value = new byte[length];
		stream.Read(value, 0, (int)length);
		return value;
	}

	public object ReadCustomType(StreamBuffer stream, byte gpType = 0)
	{
		byte typeCode = 0;
		typeCode = ((gpType != 0) ? ((byte)(gpType - 128)) : stream.ReadByte());
		int size = (int)ReadCompressedUInt32(stream);
		if (size < 0)
		{
			throw new InvalidDataException($"ReadCustomType read negative size value: {size} before position: {stream.Position}");
		}
		bool sizeBytesAvailable = size <= stream.Available;
		if (!sizeBytesAvailable || size > 32767 || !Protocol.CodeDict.TryGetValue(typeCode, out var customType))
		{
			UnknownType unknownCustomType = new UnknownType
			{
				TypeCode = typeCode,
				Size = size
			};
			int boundedSize = (sizeBytesAvailable ? size : stream.Available);
			if (boundedSize > 0)
			{
				byte[] bytes = new byte[boundedSize];
				stream.Read(bytes, 0, boundedSize);
				unknownCustomType.Data = bytes;
			}
			return unknownCustomType;
		}
		if (customType.DeserializeStreamFunction == null)
		{
			throw new NullReferenceException($"Custom Type deserialization failed. DeserializeStreamFunction is null for Type: {customType.Type} Code: {customType.Code}.");
		}
		int pos = stream.Position;
		object result = customType.DeserializeStreamFunction(stream, (short)size);
		if (stream.Position - pos != size)
		{
			stream.Position = pos + size;
		}
		return result;
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
		result.Code = ReadByte(din);
		short numRetVals = ReadByte(din);
		bool allowPooledByteArray = (flags & DeserializationFlags.AllowPooledByteArray) == DeserializationFlags.AllowPooledByteArray;
		for (uint i = 0u; i < numRetVals; i++)
		{
			byte keyByteCode = din.ReadByte();
			byte valueType = din.ReadByte();
			if (keyByteCode == result.SenderKey)
			{
				switch ((GpType)valueType)
				{
				case GpType.Int1:
					result.Sender = ReadInt1(din, signNegative: false);
					break;
				case GpType.Int2:
					result.Sender = ReadInt2(din, signNegative: false);
					break;
				case GpType.Int1_:
					result.Sender = ReadInt1(din, signNegative: true);
					break;
				case GpType.Int2_:
					result.Sender = ReadInt2(din, signNegative: true);
					break;
				case GpType.CompressedInt:
					result.Sender = ReadCompressedInt32(din);
					break;
				case GpType.IntZero:
					result.Sender = 0;
					break;
				default:
				{
					object valueObject = Read(din, valueType, flags, result.Parameters);
					break;
				}
				}
			}
			else
			{
				object valueObject = ((!allowPooledByteArray) ? Read(din, valueType, flags, result.Parameters) : ((valueType != 67) ? Read(din, valueType, flags, result.Parameters) : ReadNonAllocByteArray(din)));
				result.Parameters.Add(keyByteCode, valueObject);
			}
		}
		return result;
	}

	private ParameterDictionary ReadParameterDictionary(StreamBuffer stream, ParameterDictionary target = null, DeserializationFlags flags = DeserializationFlags.None)
	{
		short numRetVals = ReadByte(stream);
		ParameterDictionary retVals = ((target != null) ? target : new ParameterDictionary(numRetVals));
		bool allowPooledByteArray = (flags & DeserializationFlags.AllowPooledByteArray) == DeserializationFlags.AllowPooledByteArray;
		for (uint i = 0u; i < numRetVals; i++)
		{
			byte keyByteCode = stream.ReadByte();
			byte valueType = stream.ReadByte();
			object valueObject = ((!allowPooledByteArray || valueType != 67) ? Read(stream, valueType, flags, retVals) : ReadNonAllocByteArray(stream));
			retVals.Add(keyByteCode, valueObject);
		}
		return retVals;
	}

	public PhotonHashtable ReadHashtable(StreamBuffer stream, DeserializationFlags flags, ParameterDictionary parameters)
	{
		int size = (int)ReadCompressedUInt32(stream);
		PhotonHashtable value = new PhotonHashtable(size);
		for (uint i = 0u; i < size; i++)
		{
			object serKey = Read(stream, flags, parameters);
			object serValue = Read(stream, flags, parameters);
			if (serKey != null)
			{
				if (!(serKey is StructWrapper<byte> bytewrapper))
				{
					value[serKey] = serValue;
				}
				else
				{
					value[bytewrapper.Unwrap<byte>()] = serValue;
				}
			}
		}
		return value;
	}

	public int[] ReadIntArray(StreamBuffer stream)
	{
		int size = ReadInt32(stream);
		int[] array = new int[size];
		for (uint i = 0u; i < size; i++)
		{
			array[i] = ReadInt32(stream);
		}
		return array;
	}

	public override OperationRequest DeserializeOperationRequest(StreamBuffer din, DeserializationFlags flags = DeserializationFlags.None)
	{
		OperationRequest request = new OperationRequest();
		request.OperationCode = ReadByte(din);
		request.Parameters = ReadParameterDictionary(din, request.Parameters, flags);
		return request;
	}

	public override OperationResponse DeserializeOperationResponse(StreamBuffer stream, DeserializationFlags flags = DeserializationFlags.None)
	{
		OperationResponse response = new OperationResponse();
		response.OperationCode = ReadByte(stream);
		response.ReturnCode = ReadInt16(stream);
		response.DebugMessage = Read(stream, ReadByte(stream), flags, response.Parameters) as string;
		response.Parameters = ReadParameterDictionary(stream, response.Parameters, flags);
		return response;
	}

	public override DisconnectMessage DeserializeDisconnectMessage(StreamBuffer stream)
	{
		DisconnectMessage response = new DisconnectMessage();
		response.Code = ReadInt16(stream);
		response.DebugMessage = Read(stream, ReadByte(stream)) as string;
		response.Parameters = ReadParameterDictionary(stream, response.Parameters);
		return response;
	}

	internal string ReadString(StreamBuffer stream)
	{
		int length = (int)ReadCompressedUInt32(stream);
		if (length == 0)
		{
			return string.Empty;
		}
		int offset = 0;
		byte[] bytes = stream.GetBufferAndAdvance(length, out offset);
		return Encoding.UTF8.GetString(bytes, offset, length);
	}

	private object ReadCustomTypeArray(StreamBuffer stream)
	{
		uint arraySize = ReadCompressedUInt32(stream);
		byte typeCode = stream.ReadByte();
		if (!Protocol.CodeDict.TryGetValue(typeCode, out var customType))
		{
			int pos = stream.Position;
			for (uint i = 0u; i < arraySize; i++)
			{
				int size = (int)ReadCompressedUInt32(stream);
				int availableBytes = stream.Available;
				int boundedSize = ((size > availableBytes) ? availableBytes : size);
				stream.Position += boundedSize;
			}
			return new UnknownType[1]
			{
				new UnknownType
				{
					TypeCode = typeCode,
					Size = stream.Position - pos
				}
			};
		}
		Array array = Array.CreateInstance(customType.Type, (int)arraySize);
		for (uint i2 = 0u; i2 < arraySize; i2++)
		{
			int size2 = (int)ReadCompressedUInt32(stream);
			if (size2 < 0)
			{
				throw new InvalidDataException($"ReadCustomTypeArray read negative size value: {size2} before position: {stream.Position}");
			}
			if (size2 > stream.Available || size2 > 32767)
			{
				stream.Position = stream.Length;
				throw new InvalidDataException($"ReadCustomTypeArray read size value: {size2} larger than short.MaxValue or available data: {stream.Available}");
			}
			if (customType.DeserializeStreamFunction == null)
			{
				throw new NullReferenceException($"Custom Type array deserialization failed. DeserializeStreamFunction is null for Type: {customType.Type} Code: {customType.Code}.");
			}
			int pos2 = stream.Position;
			object item = customType.DeserializeStreamFunction(stream, (short)size2);
			if (stream.Position - pos2 != size2)
			{
				stream.Position = pos2 + size2;
			}
			if (item != null && customType.Type.IsAssignableFrom(item.GetType()))
			{
				array.SetValue(item, i2);
			}
		}
		return array;
	}

	private Type ReadDictionaryType(StreamBuffer stream, out GpType keyReadType, out GpType valueReadType)
	{
		keyReadType = (GpType)stream.ReadByte();
		GpType valueType = (valueReadType = (GpType)stream.ReadByte());
		Type keyClrType = ((keyReadType != GpType.Unknown) ? GetAllowedDictionaryKeyTypes(keyReadType) : typeof(object));
		Type valueClrType;
		switch (valueType)
		{
		case GpType.Unknown:
			valueClrType = typeof(object);
			break;
		case GpType.Dictionary:
			valueClrType = ReadDictionaryType(stream);
			break;
		case GpType.Array:
			valueClrType = GetDictArrayType(stream);
			valueReadType = GpType.Unknown;
			break;
		case GpType.ObjectArray:
			valueClrType = typeof(object[]);
			break;
		case GpType.HashtableArray:
			valueClrType = typeof(PhotonHashtable[]);
			break;
		default:
			valueClrType = GetClrArrayType(valueType);
			break;
		}
		return typeof(Dictionary<, >).MakeGenericType(keyClrType, valueClrType);
	}

	private Type ReadDictionaryType(StreamBuffer stream)
	{
		GpType keyType = (GpType)stream.ReadByte();
		GpType valueType = (GpType)stream.ReadByte();
		Type keyClrType = ((keyType != GpType.Unknown) ? GetAllowedDictionaryKeyTypes(keyType) : typeof(object));
		Type valueClrType = valueType switch
		{
			GpType.Unknown => typeof(object), 
			GpType.Dictionary => ReadDictionaryType(stream), 
			GpType.Array => GetDictArrayType(stream), 
			_ => GetClrArrayType(valueType), 
		};
		return typeof(Dictionary<, >).MakeGenericType(keyClrType, valueClrType);
	}

	private Type GetDictArrayType(StreamBuffer stream)
	{
		GpType gpElementType = (GpType)stream.ReadByte();
		int count = 0;
		while (gpElementType == GpType.Array)
		{
			count++;
			gpElementType = (GpType)stream.ReadByte();
		}
		Type type = GetClrArrayType(gpElementType).MakeArrayType();
		for (uint i = 0u; i < count; i++)
		{
			type = type.MakeArrayType();
		}
		return type;
	}

	private IDictionary ReadDictionary(StreamBuffer stream, DeserializationFlags flags, ParameterDictionary parameters)
	{
		GpType keyReadType;
		GpType valueReadType;
		Type dictType = ReadDictionaryType(stream, out keyReadType, out valueReadType);
		if (dictType == null)
		{
			return null;
		}
		if (!(Activator.CreateInstance(dictType) is IDictionary dict))
		{
			return null;
		}
		ReadDictionaryElements(stream, keyReadType, valueReadType, dict, flags, parameters);
		return dict;
	}

	private bool ReadDictionaryElements(StreamBuffer stream, GpType keyReadType, GpType valueReadType, IDictionary dictionary, DeserializationFlags flags, ParameterDictionary parameters)
	{
		uint count = ReadCompressedUInt32(stream);
		for (uint i = 0u; i < count; i++)
		{
			object key = ((keyReadType == GpType.Unknown) ? Read(stream, flags, parameters) : Read(stream, (byte)keyReadType));
			object value = ((valueReadType == GpType.Unknown) ? Read(stream, flags, parameters) : Read(stream, (byte)valueReadType));
			if (key != null)
			{
				dictionary.Add(key, value);
			}
		}
		return true;
	}

	private object[] ReadObjectArray(StreamBuffer stream, DeserializationFlags flags, ParameterDictionary parameters)
	{
		uint length = ReadCompressedUInt32(stream);
		object[] result = new object[length];
		for (uint i = 0u; i < length; i++)
		{
			object val = Read(stream, flags, parameters);
			result[i] = val;
		}
		return result;
	}

	private StructWrapper[] ReadWrapperArray(StreamBuffer stream, DeserializationFlags flags, ParameterDictionary parameters)
	{
		uint length = ReadCompressedUInt32(stream);
		StructWrapper[] result = new StructWrapper[length];
		for (uint i = 0u; i < length; i++)
		{
			object val = Read(stream, flags, parameters);
			result[i] = val as StructWrapper;
			_ = result[i];
		}
		return result;
	}

	private bool[] ReadBooleanArray(StreamBuffer stream)
	{
		uint count = ReadCompressedUInt32(stream);
		bool[] result = new bool[count];
		int fullBytes = (int)count / 8;
		int i = 0;
		while (fullBytes > 0)
		{
			byte temp = stream.ReadByte();
			result[i++] = (temp & 1) == 1;
			result[i++] = (temp & 2) == 2;
			result[i++] = (temp & 4) == 4;
			result[i++] = (temp & 8) == 8;
			result[i++] = (temp & 0x10) == 16;
			result[i++] = (temp & 0x20) == 32;
			result[i++] = (temp & 0x40) == 64;
			result[i++] = (temp & 0x80) == 128;
			fullBytes--;
		}
		if (i < count)
		{
			byte temp2 = stream.ReadByte();
			int shift = 0;
			while (i < count)
			{
				result[i++] = (temp2 & boolMasks[shift]) == boolMasks[shift];
				shift++;
			}
		}
		return result;
	}

	internal short[] ReadInt16Array(StreamBuffer stream)
	{
		short[] value = new short[ReadCompressedUInt32(stream)];
		for (uint i = 0u; i < value.Length; i++)
		{
			value[i] = ReadInt16(stream);
		}
		return value;
	}

	private float[] ReadSingleArray(StreamBuffer stream)
	{
		uint num = ReadCompressedUInt32(stream);
		int countBytes = (int)(num * 4);
		float[] result = new float[num];
		Buffer.BlockCopy(stream.GetBufferAndAdvance(countBytes, out var srcPos), srcPos, result, 0, countBytes);
		return result;
	}

	private double[] ReadDoubleArray(StreamBuffer stream)
	{
		uint num = ReadCompressedUInt32(stream);
		int countBytes = (int)(num * 8);
		double[] result = new double[num];
		Buffer.BlockCopy(stream.GetBufferAndAdvance(countBytes, out var srcPos), srcPos, result, 0, countBytes);
		return result;
	}

	internal string[] ReadStringArray(StreamBuffer stream)
	{
		string[] value = new string[ReadCompressedUInt32(stream)];
		for (uint i = 0u; i < value.Length; i++)
		{
			value[i] = ReadString(stream);
		}
		return value;
	}

	private PhotonHashtable[] ReadHashtableArray(StreamBuffer stream, DeserializationFlags flags, ParameterDictionary parameters)
	{
		uint count = ReadCompressedUInt32(stream);
		PhotonHashtable[] result = new PhotonHashtable[count];
		for (uint i = 0u; i < count; i++)
		{
			result[i] = ReadHashtable(stream, flags, parameters);
		}
		return result;
	}

	private IDictionary[] ReadDictionaryArray(StreamBuffer stream, DeserializationFlags flags, ParameterDictionary parameters)
	{
		GpType keyReadType;
		GpType valueReadType;
		Type dictType = ReadDictionaryType(stream, out keyReadType, out valueReadType);
		uint count = ReadCompressedUInt32(stream);
		IDictionary[] result = (IDictionary[])Array.CreateInstance(dictType, (int)count);
		for (uint i = 0u; i < count; i++)
		{
			result[i] = (IDictionary)Activator.CreateInstance(dictType);
			ReadDictionaryElements(stream, keyReadType, valueReadType, result[i], flags, parameters);
		}
		return result;
	}

	private Array ReadArrayInArray(StreamBuffer stream, DeserializationFlags flags, ParameterDictionary parameters)
	{
		uint count = ReadCompressedUInt32(stream);
		Array result = null;
		Type elementType = null;
		for (uint i = 0u; i < count; i++)
		{
			if (Read(stream, flags, parameters) is Array innerArray)
			{
				if (result == null)
				{
					elementType = innerArray.GetType();
					result = Array.CreateInstance(elementType, (int)count);
				}
				if (elementType.IsAssignableFrom(innerArray.GetType()))
				{
					result.SetValue(innerArray, i);
				}
			}
		}
		return result;
	}

	internal int ReadInt1(StreamBuffer stream, bool signNegative)
	{
		if (signNegative)
		{
			return -stream.ReadByte();
		}
		return stream.ReadByte();
	}

	internal int ReadInt2(StreamBuffer stream, bool signNegative)
	{
		if (signNegative)
		{
			return -ReadUShort(stream);
		}
		return ReadUShort(stream);
	}

	internal int ReadCompressedInt32(StreamBuffer stream)
	{
		uint uValue = ReadCompressedUInt32(stream);
		return DecodeZigZag32(uValue);
	}

	private uint ReadCompressedUInt32(StreamBuffer stream)
	{
		uint value = 0u;
		int shift = 0;
		byte[] data = stream.GetBuffer();
		int offset = stream.Position;
		while (shift != 35)
		{
			if (offset >= stream.Length)
			{
				stream.Position = stream.Length;
				throw new EndOfStreamException("Failed to read full uint. offset: " + offset + " stream.Length: " + stream.Length + " data.Length: " + data.Length + " stream.Available: " + stream.Available);
			}
			byte b = data[offset];
			offset++;
			value |= (uint)((b & 0x7F) << shift);
			shift += 7;
			if ((b & 0x80) == 0)
			{
				break;
			}
		}
		stream.Position = offset;
		return value;
	}

	internal long ReadCompressedInt64(StreamBuffer stream)
	{
		ulong uValue = ReadCompressedUInt64(stream);
		return DecodeZigZag64(uValue);
	}

	private ulong ReadCompressedUInt64(StreamBuffer stream)
	{
		ulong value = 0uL;
		int shift = 0;
		byte[] data = stream.GetBuffer();
		int offset = stream.Position;
		while (shift != 70)
		{
			if (offset >= data.Length)
			{
				throw new EndOfStreamException("Failed to read full ulong.");
			}
			byte b = data[offset];
			offset++;
			value |= (ulong)((long)(b & 0x7F) << shift);
			shift += 7;
			if ((b & 0x80) == 0)
			{
				break;
			}
		}
		stream.Position = offset;
		return value;
	}

	internal int[] ReadCompressedInt32Array(StreamBuffer stream)
	{
		int[] value = new int[ReadCompressedUInt32(stream)];
		for (uint i = 0u; i < value.Length; i++)
		{
			value[i] = ReadCompressedInt32(stream);
		}
		return value;
	}

	internal long[] ReadCompressedInt64Array(StreamBuffer stream)
	{
		long[] value = new long[ReadCompressedUInt32(stream)];
		for (uint i = 0u; i < value.Length; i++)
		{
			value[i] = ReadCompressedInt64(stream);
		}
		return value;
	}

	private int DecodeZigZag32(uint value)
	{
		return (int)((value >> 1) ^ (0L - (long)(value & 1)));
	}

	private long DecodeZigZag64(ulong value)
	{
		return (long)((value >> 1) ^ (0L - (value & 1)));
	}

	internal void Write(StreamBuffer stream, object value, bool writeType)
	{
		if (value == null)
		{
			Write(stream, value, GpType.Null, writeType);
		}
		else
		{
			Write(stream, value, GetCodeOfType(value.GetType()), writeType);
		}
	}

	private void Write(StreamBuffer stream, object value, GpType gpType, bool writeType)
	{
		switch (gpType)
		{
		case GpType.Unknown:
			if (value is ByteArraySlice)
			{
				ByteArraySlice slice = (ByteArraySlice)value;
				WriteByteArraySlice(stream, slice, writeType);
				break;
			}
			if (value is ArraySegment<byte> seg)
			{
				WriteArraySegmentByte(stream, seg, writeType);
				break;
			}
			if (value is StructWrapper wrapper)
			{
				switch (wrapper.wrappedType)
				{
				case WrappedType.Bool:
					WriteBoolean(stream, value.Get<bool>(), writeType);
					break;
				case WrappedType.Byte:
					WriteByte(stream, value.Get<byte>(), writeType);
					break;
				case WrappedType.Int16:
					WriteInt16(stream, value.Get<short>(), writeType);
					break;
				case WrappedType.Int32:
					WriteCompressedInt32(stream, value.Get<int>(), writeType);
					break;
				case WrappedType.Int64:
					WriteCompressedInt64(stream, value.Get<long>(), writeType);
					break;
				case WrappedType.Single:
					WriteSingle(stream, value.Get<float>(), writeType);
					break;
				case WrappedType.Double:
					WriteDouble(stream, value.Get<double>(), writeType);
					break;
				default:
					WriteCustomType(stream, value, writeType);
					break;
				}
				break;
			}
			goto case GpType.Custom;
		case GpType.Custom:
			WriteCustomType(stream, value, writeType);
			break;
		case GpType.CustomTypeArray:
			WriteCustomTypeArray(stream, value, writeType);
			break;
		case GpType.Array:
			WriteArrayInArray(stream, value, writeType);
			break;
		case GpType.CompressedInt:
			WriteCompressedInt32(stream, (int)value, writeType);
			break;
		case GpType.CompressedLong:
			WriteCompressedInt64(stream, (long)value, writeType);
			break;
		case GpType.Dictionary:
			WriteDictionary(stream, (IDictionary)value, writeType);
			break;
		case GpType.Byte:
			WriteByte(stream, (byte)value, writeType);
			break;
		case GpType.Double:
			WriteDouble(stream, (double)value, writeType);
			break;
		case GpType.EventData:
			SerializeEventData(stream, (EventData)value, writeType);
			break;
		case GpType.Float:
			WriteSingle(stream, (float)value, writeType);
			break;
		case GpType.Hashtable:
			WriteHashtable(stream, (PhotonHashtable)value, writeType);
			break;
		case GpType.Short:
			WriteInt16(stream, (short)value, writeType);
			break;
		case GpType.CompressedIntArray:
			WriteInt32ArrayCompressed(stream, (int[])value, writeType);
			break;
		case GpType.CompressedLongArray:
			WriteInt64ArrayCompressed(stream, (long[])value, writeType);
			break;
		case GpType.Boolean:
			WriteBoolean(stream, (bool)value, writeType);
			break;
		case GpType.OperationResponse:
			SerializeOperationResponse(stream, (OperationResponse)value, writeType);
			break;
		case GpType.OperationRequest:
			SerializeOperationRequest(stream, (OperationRequest)value, writeType);
			break;
		case GpType.String:
			WriteString(stream, (string)value, writeType);
			break;
		case GpType.ByteArray:
			WriteByteArray(stream, (byte[])value, writeType);
			break;
		case GpType.ObjectArray:
			WriteObjectArray(stream, (IList)value, writeType);
			break;
		case GpType.DictionaryArray:
			WriteDictionaryArray(stream, (IDictionary[])value, writeType);
			break;
		case GpType.DoubleArray:
			WriteDoubleArray(stream, (double[])value, writeType);
			break;
		case GpType.FloatArray:
			WriteSingleArray(stream, (float[])value, writeType);
			break;
		case GpType.HashtableArray:
			WriteHashtableArray(stream, value, writeType);
			break;
		case GpType.ShortArray:
			WriteInt16Array(stream, (short[])value, writeType);
			break;
		case GpType.BooleanArray:
			WriteBoolArray(stream, (bool[])value, writeType);
			break;
		case GpType.StringArray:
			WriteStringArray(stream, value, writeType);
			break;
		case GpType.Null:
			if (writeType)
			{
				stream.WriteByte(8);
			}
			break;
		}
	}

	public override void SerializeEventData(StreamBuffer stream, EventData serObject, bool setType)
	{
		if (setType)
		{
			stream.WriteByte(26);
		}
		stream.WriteByte(serObject.Code);
		WriteParameterTable(stream, serObject.Parameters);
	}

	private void WriteParameterTable(StreamBuffer stream, Dictionary<byte, object> parameters)
	{
		if (parameters == null || parameters.Count == 0)
		{
			WriteByte(stream, 0, writeType: false);
			return;
		}
		WriteByte(stream, (byte)parameters.Count, writeType: false);
		foreach (KeyValuePair<byte, object> pair in parameters)
		{
			stream.WriteByte(pair.Key);
			Write(stream, pair.Value, writeType: true);
		}
	}

	private void WriteParameterTable(StreamBuffer stream, ParameterDictionary parameters)
	{
		if (parameters == null || parameters.Count == 0)
		{
			WriteByte(stream, 0, writeType: false);
			return;
		}
		WriteByte(stream, (byte)parameters.Count, writeType: false);
		foreach (KeyValuePair<byte, object> pair in parameters)
		{
			stream.WriteByte(pair.Key);
			Write(stream, pair.Value, writeType: true);
		}
	}

	private void SerializeOperationRequest(StreamBuffer stream, OperationRequest operation, bool setType)
	{
		SerializeOperationRequest(stream, operation.OperationCode, operation.Parameters, setType);
	}

	public override void SerializeOperationRequest(StreamBuffer stream, byte operationCode, ParameterDictionary parameters, bool setType)
	{
		if (setType)
		{
			stream.WriteByte(24);
		}
		stream.WriteByte(operationCode);
		WriteParameterTable(stream, parameters);
	}

	public override void SerializeOperationResponse(StreamBuffer stream, OperationResponse serObject, bool setType)
	{
		if (setType)
		{
			stream.WriteByte(25);
		}
		stream.WriteByte(serObject.OperationCode);
		WriteInt16(stream, serObject.ReturnCode, writeType: false);
		if (string.IsNullOrEmpty(serObject.DebugMessage))
		{
			stream.WriteByte(8);
		}
		else
		{
			stream.WriteByte(7);
			WriteString(stream, serObject.DebugMessage, writeType: false);
		}
		WriteParameterTable(stream, serObject.Parameters);
	}

	internal void WriteByte(StreamBuffer stream, byte value, bool writeType)
	{
		if (writeType)
		{
			if (value == 0)
			{
				stream.WriteByte(34);
				return;
			}
			stream.WriteByte(3);
		}
		stream.WriteByte(value);
	}

	internal void WriteBoolean(StreamBuffer stream, bool value, bool writeType)
	{
		if (writeType)
		{
			if (value)
			{
				stream.WriteByte(28);
			}
			else
			{
				stream.WriteByte(27);
			}
		}
		else
		{
			stream.WriteByte(value ? ((byte)1) : ((byte)0));
		}
	}

	internal void WriteUShort(StreamBuffer stream, ushort value)
	{
		stream.WriteBytes((byte)value, (byte)(value >> 8));
	}

	internal void WriteInt16(StreamBuffer stream, short value, bool writeType)
	{
		if (writeType)
		{
			if (value == 0)
			{
				stream.WriteByte(29);
				return;
			}
			stream.WriteByte(4);
		}
		stream.WriteBytes((byte)value, (byte)(value >> 8));
	}

	internal void WriteDouble(StreamBuffer stream, double value, bool writeType)
	{
		if (writeType)
		{
			stream.WriteByte(6);
		}
		int offset;
		byte[] b = stream.GetBufferAndAdvance(8, out offset);
		lock (memDoubleBlock)
		{
			memDoubleBlock[0] = value;
			Buffer.BlockCopy(memDoubleBlock, 0, b, offset, 8);
		}
	}

	internal void WriteSingle(StreamBuffer stream, float value, bool writeType)
	{
		if (writeType)
		{
			stream.WriteByte(5);
		}
		int offset;
		byte[] b = stream.GetBufferAndAdvance(4, out offset);
		lock (memFloatBlock)
		{
			memFloatBlock[0] = value;
			Buffer.BlockCopy(memFloatBlock, 0, b, offset, 4);
		}
	}

	internal void WriteString(StreamBuffer stream, string value, bool writeType)
	{
		if (writeType)
		{
			stream.WriteByte(7);
		}
		int count = Encoding.UTF8.GetByteCount(value);
		if (count > 32767)
		{
			throw new NotSupportedException("Strings that exceed a UTF8-encoded byte-length of 32767 (short.MaxValue) are not supported. Yours is: " + count);
		}
		WriteIntLength(stream, count);
		int offset = 0;
		byte[] streamBuffer = stream.GetBufferAndAdvance(count, out offset);
		Encoding.UTF8.GetBytes(value, 0, value.Length, streamBuffer, offset);
	}

	private void WriteHashtable(StreamBuffer stream, object value, bool writeType)
	{
		PhotonHashtable hashTable = (PhotonHashtable)value;
		if (writeType)
		{
			stream.WriteByte(21);
		}
		WriteIntLength(stream, hashTable.Count);
		foreach (DictionaryEntry entry in hashTable)
		{
			Write(stream, entry.Key, writeType: true);
			Write(stream, entry.Value, writeType: true);
		}
	}

	internal void WriteByteArray(StreamBuffer stream, byte[] value, bool writeType)
	{
		if (writeType)
		{
			stream.WriteByte(67);
		}
		WriteIntLength(stream, value.Length);
		stream.Write(value, 0, value.Length);
	}

	private void WriteArraySegmentByte(StreamBuffer stream, ArraySegment<byte> seg, bool writeType)
	{
		if (writeType)
		{
			stream.WriteByte(67);
		}
		int cnt = seg.Count;
		WriteIntLength(stream, cnt);
		if (cnt > 0)
		{
			stream.Write(seg.Array, seg.Offset, cnt);
		}
	}

	private void WriteByteArraySlice(StreamBuffer stream, ByteArraySlice buffer, bool writeType)
	{
		if (writeType)
		{
			stream.WriteByte(67);
		}
		int cnt = buffer.Count;
		WriteIntLength(stream, cnt);
		stream.Write(buffer.Buffer, buffer.Offset, cnt);
		buffer.Release();
	}

	internal void WriteInt32ArrayCompressed(StreamBuffer stream, int[] value, bool writeType)
	{
		if (writeType)
		{
			stream.WriteByte(73);
		}
		WriteIntLength(stream, value.Length);
		for (int i = 0; i < value.Length; i++)
		{
			WriteCompressedInt32(stream, value[i], writeType: false);
		}
	}

	private void WriteInt64ArrayCompressed(StreamBuffer stream, long[] values, bool setType)
	{
		if (setType)
		{
			stream.WriteByte(74);
		}
		WriteIntLength(stream, values.Length);
		for (int i = 0; i < values.Length; i++)
		{
			WriteCompressedInt64(stream, values[i], writeType: false);
		}
	}

	internal void WriteBoolArray(StreamBuffer stream, bool[] value, bool writeType)
	{
		if (writeType)
		{
			stream.WriteByte(66);
		}
		WriteIntLength(stream, value.Length);
		int fullByteCount = value.Length >> 3;
		byte[] buffer = new byte[fullByteCount + 1];
		int pos = 0;
		int i = 0;
		while (fullByteCount > 0)
		{
			byte temp = 0;
			if (value[i++])
			{
				temp |= 1;
			}
			if (value[i++])
			{
				temp |= 2;
			}
			if (value[i++])
			{
				temp |= 4;
			}
			if (value[i++])
			{
				temp |= 8;
			}
			if (value[i++])
			{
				temp |= 0x10;
			}
			if (value[i++])
			{
				temp |= 0x20;
			}
			if (value[i++])
			{
				temp |= 0x40;
			}
			if (value[i++])
			{
				temp |= 0x80;
			}
			buffer[pos] = temp;
			fullByteCount--;
			pos++;
		}
		if (i < value.Length)
		{
			byte temp2 = 0;
			int shift = 0;
			for (; i < value.Length; i++)
			{
				if (value[i])
				{
					temp2 |= (byte)(1 << shift);
				}
				shift++;
			}
			buffer[pos] = temp2;
			pos++;
		}
		stream.Write(buffer, 0, pos);
	}

	internal void WriteInt16Array(StreamBuffer stream, short[] value, bool writeType)
	{
		if (writeType)
		{
			stream.WriteByte(68);
		}
		WriteIntLength(stream, value.Length);
		for (int i = 0; i < value.Length; i++)
		{
			WriteInt16(stream, value[i], writeType: false);
		}
	}

	internal void WriteSingleArray(StreamBuffer stream, float[] values, bool setType)
	{
		if (setType)
		{
			stream.WriteByte(69);
		}
		WriteIntLength(stream, values.Length);
		int countBytes = values.Length * 4;
		int dstPos;
		byte[] buffer = stream.GetBufferAndAdvance(countBytes, out dstPos);
		Buffer.BlockCopy(values, 0, buffer, dstPos, countBytes);
	}

	internal void WriteDoubleArray(StreamBuffer stream, double[] values, bool setType)
	{
		if (setType)
		{
			stream.WriteByte(70);
		}
		WriteIntLength(stream, values.Length);
		int countBytes = values.Length * 8;
		int dstPos;
		byte[] buffer = stream.GetBufferAndAdvance(countBytes, out dstPos);
		Buffer.BlockCopy(values, 0, buffer, dstPos, countBytes);
	}

	internal void WriteStringArray(StreamBuffer stream, object value0, bool writeType)
	{
		string[] value1 = (string[])value0;
		if (writeType)
		{
			stream.WriteByte(71);
		}
		WriteIntLength(stream, value1.Length);
		for (int i = 0; i < value1.Length; i++)
		{
			if (value1[i] == null)
			{
				throw new InvalidDataException("Unexpected - cannot serialize string array with null element " + i);
			}
			WriteString(stream, value1[i], writeType: false);
		}
	}

	private void WriteObjectArray(StreamBuffer stream, object array, bool writeType)
	{
		WriteObjectArray(stream, (IList)array, writeType);
	}

	private void WriteObjectArray(StreamBuffer stream, IList array, bool writeType)
	{
		if (writeType)
		{
			stream.WriteByte(23);
		}
		WriteIntLength(stream, array.Count);
		for (int index = 0; index < array.Count; index++)
		{
			object element = array[index];
			Write(stream, element, writeType: true);
		}
	}

	private void WriteArrayInArray(StreamBuffer stream, object value, bool writeType)
	{
		object[] array = (object[])value;
		stream.WriteByte(64);
		WriteIntLength(stream, array.Length);
		object[] array2 = array;
		foreach (object item in array2)
		{
			Write(stream, item, writeType: true);
		}
	}

	private void WriteCustomTypeBody(CustomType customType, StreamBuffer stream, object value)
	{
		if (customType.SerializeStreamFunction == null)
		{
			throw new NullReferenceException($"Custom Type serialization failed. SerializeStreamFunction is null for Type: {customType.Type} Code: {customType.Code}.");
		}
		int posOfLengthInfo = stream.Position;
		stream.Position++;
		uint returnedLength = (uint)customType.SerializeStreamFunction(stream, value);
		int serializedLength = stream.Position - posOfLengthInfo - 1;
		_ = serializedLength;
		_ = returnedLength;
		int serializedLengthSize = WriteCompressedUInt32(memCustomTypeBodyLengthSerialized, (uint)serializedLength);
		if (serializedLengthSize == 1)
		{
			stream.GetBuffer()[posOfLengthInfo] = memCustomTypeBodyLengthSerialized[0];
			return;
		}
		for (int i = 0; i < serializedLengthSize - 1; i++)
		{
			stream.WriteByte(0);
		}
		Buffer.BlockCopy(stream.GetBuffer(), posOfLengthInfo + 1, stream.GetBuffer(), posOfLengthInfo + serializedLengthSize, serializedLength);
		Buffer.BlockCopy(memCustomTypeBodyLengthSerialized, 0, stream.GetBuffer(), posOfLengthInfo, serializedLengthSize);
		stream.Position = posOfLengthInfo + serializedLengthSize + serializedLength;
	}

	private void WriteCustomType(StreamBuffer stream, object value, bool writeType)
	{
		Type typeCode = ((!(value is StructWrapper wrapper)) ? value.GetType() : wrapper.ttype);
		if (Protocol.TypeDict.TryGetValue(typeCode, out var customType))
		{
			if (writeType)
			{
				if (customType.Code < 100)
				{
					stream.WriteByte((byte)(128 + customType.Code));
				}
				else
				{
					stream.WriteByte(19);
					stream.WriteByte(customType.Code);
				}
			}
			else
			{
				stream.WriteByte(customType.Code);
			}
			WriteCustomTypeBody(customType, stream, value);
			return;
		}
		throw new Exception("Write failed. Custom type not found: " + typeCode);
	}

	private void WriteCustomTypeArray(StreamBuffer stream, object value, bool writeType)
	{
		IList list = (IList)value;
		Type elementType = value.GetType().GetElementType();
		if (Protocol.TypeDict.TryGetValue(elementType, out var customType))
		{
			if (writeType)
			{
				stream.WriteByte(83);
			}
			WriteIntLength(stream, list.Count);
			stream.WriteByte(customType.Code);
			{
				foreach (object element in list)
				{
					WriteCustomTypeBody(customType, stream, element);
				}
				return;
			}
		}
		throw new Exception("Write failed. Custom type of element not found: " + elementType);
	}

	private bool WriteArrayHeader(StreamBuffer stream, Type type)
	{
		Type elementType = type.GetElementType();
		while (elementType.IsArray)
		{
			stream.WriteByte(64);
			elementType = elementType.GetElementType();
		}
		GpType protocolType = GetCodeOfType(elementType);
		if (protocolType == GpType.Unknown)
		{
			return false;
		}
		stream.WriteByte((byte)(protocolType | GpType.CustomTypeSlim));
		return true;
	}

	private void WriteDictionaryElements(StreamBuffer stream, IDictionary dictionary, GpType keyWriteType, GpType valueWriteType)
	{
		WriteIntLength(stream, dictionary.Count);
		foreach (DictionaryEntry entry in dictionary)
		{
			Write(stream, entry.Key, keyWriteType == GpType.Unknown);
			Write(stream, entry.Value, valueWriteType == GpType.Unknown);
		}
	}

	private void WriteDictionary(StreamBuffer stream, object dict, bool setType)
	{
		if (setType)
		{
			stream.WriteByte(20);
		}
		WriteDictionaryHeader(stream, dict.GetType(), out var keyWriteType, out var valueWriteType);
		IDictionary d = (IDictionary)dict;
		WriteDictionaryElements(stream, d, keyWriteType, valueWriteType);
	}

	private void WriteDictionaryHeader(StreamBuffer stream, Type type, out GpType keyWriteType, out GpType valueWriteType)
	{
		Type[] types = type.GetGenericArguments();
		if (types[0] == typeof(object))
		{
			stream.WriteByte(0);
			keyWriteType = GpType.Unknown;
		}
		else
		{
			if (!types[0].IsPrimitive && types[0] != typeof(string))
			{
				throw new InvalidDataException("Unexpected - cannot serialize Dictionary with key type: " + types[0]);
			}
			keyWriteType = GetCodeOfType(types[0]);
			if (keyWriteType == GpType.Unknown)
			{
				throw new InvalidDataException("Unexpected - cannot serialize Dictionary with key type: " + types[0]);
			}
			stream.WriteByte((byte)keyWriteType);
		}
		if (types[1] == typeof(object))
		{
			stream.WriteByte(0);
			valueWriteType = GpType.Unknown;
			return;
		}
		if (types[1].IsArray)
		{
			if (WriteArrayType(stream, types[1], out valueWriteType))
			{
				return;
			}
			throw new InvalidDataException("Unexpected - cannot serialize Dictionary with value type: " + types[1]);
		}
		valueWriteType = GetCodeOfType(types[1]);
		if (valueWriteType == GpType.Unknown)
		{
			throw new InvalidDataException("Unexpected - cannot serialize Dictionary with value type: " + types[1]);
		}
		if (valueWriteType == GpType.Array)
		{
			if (!WriteArrayHeader(stream, types[1]))
			{
				throw new InvalidDataException("Unexpected - cannot serialize Dictionary with value type: " + types[1]);
			}
		}
		else if (valueWriteType == GpType.Dictionary)
		{
			stream.WriteByte((byte)valueWriteType);
			WriteDictionaryHeader(stream, types[1], out var _, out var _);
		}
		else
		{
			stream.WriteByte((byte)valueWriteType);
		}
	}

	private bool WriteArrayType(StreamBuffer stream, Type type, out GpType writeType)
	{
		Type elementType = type.GetElementType();
		if (elementType == null)
		{
			throw new InvalidDataException("Unexpected - cannot serialize array with type: " + type);
		}
		if (elementType.IsArray)
		{
			while (elementType != null && elementType.IsArray)
			{
				stream.WriteByte(64);
				elementType = elementType.GetElementType();
			}
			byte gpType = (byte)(GetCodeOfType(elementType) | GpType.Array);
			stream.WriteByte(gpType);
			writeType = GpType.Array;
			return true;
		}
		if (elementType.IsPrimitive)
		{
			byte gpType2 = (byte)(GetCodeOfType(elementType) | GpType.Array);
			if (gpType2 == 226)
			{
				gpType2 = 67;
			}
			stream.WriteByte(gpType2);
			if (Enum.IsDefined(typeof(GpType), gpType2))
			{
				writeType = (GpType)gpType2;
				return true;
			}
			writeType = GpType.Unknown;
			return false;
		}
		if (elementType == typeof(string))
		{
			stream.WriteByte(71);
			writeType = GpType.StringArray;
			return true;
		}
		if (elementType == typeof(object))
		{
			stream.WriteByte(23);
			writeType = GpType.ObjectArray;
			return true;
		}
		if (elementType == typeof(PhotonHashtable))
		{
			stream.WriteByte(85);
			writeType = GpType.HashtableArray;
			return true;
		}
		writeType = GpType.Unknown;
		return false;
	}

	private void WriteHashtableArray(StreamBuffer stream, object value, bool writeType)
	{
		PhotonHashtable[] array = (PhotonHashtable[])value;
		if (writeType)
		{
			stream.WriteByte(85);
		}
		WriteIntLength(stream, array.Length);
		PhotonHashtable[] array2 = array;
		foreach (PhotonHashtable element in array2)
		{
			WriteHashtable(stream, element, writeType: false);
		}
	}

	private void WriteDictionaryArray(StreamBuffer stream, IDictionary[] dictArray, bool writeType)
	{
		stream.WriteByte(84);
		WriteDictionaryHeader(stream, dictArray.GetType().GetElementType(), out var keyWriteType, out var valueWriteType);
		WriteIntLength(stream, dictArray.Length);
		foreach (IDictionary item in dictArray)
		{
			WriteDictionaryElements(stream, item, keyWriteType, valueWriteType);
		}
	}

	private void WriteIntLength(StreamBuffer stream, int value)
	{
		WriteCompressedUInt32(stream, (uint)value);
	}

	private void WriteVarInt32(StreamBuffer stream, int value, bool writeType)
	{
		WriteCompressedInt32(stream, value, writeType);
	}

	/// <summary>
	/// Writes integers as compressed. Either directly as zigzag-encoded or (when a type is written for this value) it can use an optimized sub-type.
	/// </summary>
	private void WriteCompressedInt32(StreamBuffer stream, int value, bool writeType)
	{
		if (writeType)
		{
			if (value == 0)
			{
				stream.WriteByte(30);
				return;
			}
			if (value > 0)
			{
				if (value <= 255)
				{
					stream.WriteByte(11);
					stream.WriteByte((byte)value);
					return;
				}
				if (value <= 65535)
				{
					stream.WriteByte(13);
					WriteUShort(stream, (ushort)value);
					return;
				}
			}
			else if (value >= -65535)
			{
				if (value >= -255)
				{
					stream.WriteByte(12);
					stream.WriteByte((byte)(-value));
					return;
				}
				if (value >= -65535)
				{
					stream.WriteByte(14);
					WriteUShort(stream, (ushort)(-value));
					return;
				}
			}
		}
		if (writeType)
		{
			stream.WriteByte(9);
		}
		uint zigVal = EncodeZigZag32(value);
		WriteCompressedUInt32(stream, zigVal);
	}

	private void WriteCompressedInt64(StreamBuffer stream, long value, bool writeType)
	{
		if (writeType)
		{
			if (value == 0L)
			{
				stream.WriteByte(31);
				return;
			}
			if (value > 0)
			{
				if (value <= 255)
				{
					stream.WriteByte(15);
					stream.WriteByte((byte)value);
					return;
				}
				if (value <= 65535)
				{
					stream.WriteByte(17);
					WriteUShort(stream, (ushort)value);
					return;
				}
			}
			else if (value >= -65535)
			{
				if (value >= -255)
				{
					stream.WriteByte(16);
					stream.WriteByte((byte)(-value));
					return;
				}
				if (value >= -65535)
				{
					stream.WriteByte(18);
					WriteUShort(stream, (ushort)(-value));
					return;
				}
			}
		}
		if (writeType)
		{
			stream.WriteByte(10);
		}
		ulong uValue = EncodeZigZag64(value);
		WriteCompressedUInt64(stream, uValue);
	}

	private void WriteCompressedUInt32(StreamBuffer stream, uint value)
	{
		lock (memCompressedUInt32)
		{
			stream.Write(memCompressedUInt32, 0, WriteCompressedUInt32(memCompressedUInt32, value));
		}
	}

	private int WriteCompressedUInt32(byte[] buffer, uint value)
	{
		int count = 0;
		buffer[count] = (byte)(value & 0x7F);
		for (value >>= 7; value != 0; value >>= 7)
		{
			buffer[count] |= 128;
			buffer[++count] = (byte)(value & 0x7F);
		}
		return count + 1;
	}

	private void WriteCompressedUInt64(StreamBuffer stream, ulong value)
	{
		int count = 0;
		lock (memCompressedUInt64)
		{
			memCompressedUInt64[count] = (byte)(value & 0x7F);
			for (value >>= 7; value != 0; value >>= 7)
			{
				memCompressedUInt64[count] |= 128;
				memCompressedUInt64[++count] = (byte)(value & 0x7F);
			}
			count++;
			stream.Write(memCompressedUInt64, 0, count);
		}
	}

	private uint EncodeZigZag32(int value)
	{
		return (uint)((value << 1) ^ (value >> 31));
	}

	private ulong EncodeZigZag64(long value)
	{
		return (ulong)((value << 1) ^ (value >> 63));
	}
}
