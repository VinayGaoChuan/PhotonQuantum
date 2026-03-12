namespace Photon.Deterministic
{
	public interface IBitStream
	{
		int BytesRequired { get; }

		byte[] Data { get; }

		bool Writing { get; set; }

		bool Reading { get; set; }

		void Serialize(ref string value);

		void Serialize(ref bool value);

		void Serialize(ref float value);

		void Serialize(ref double value);

		void Serialize(ref long value);

		void Serialize(ref ulong value);

		void Serialize(ref FP value);

		void Serialize(ref FPVector2 value);

		void Serialize(ref FPVector3 value);

		void Serialize(ref FPQuaternion value);

		void Serialize(ref byte value);

		void Serialize(ref uint value);

		void Serialize(ref uint value, int bits);

		void Serialize(ref ulong value, int bits);

		void Serialize(ref int value);

		void Serialize(ref int value, int bits);

		void Serialize(ref int[] value);

		void Serialize(ref byte[] value);

		void Serialize(ref byte[] array, ref int length);

		void Serialize(ref byte[] value, int fixedSize);

		void Serialize(ref byte[] array, ref int length, int fixedSize);

		int SerializeArrayLength<T>(ref T[] array, int maxLength = int.MaxValue);

		void SerializeArray<T>(ref T[] array, BitStream.ArrayElementSerializer<T> serializer, int maxLength = int.MaxValue);

		unsafe void Serialize(byte* v);

		unsafe void Serialize(sbyte* v);

		unsafe void Serialize(short* v);

		unsafe void Serialize(ushort* v);

		unsafe void Serialize(int* v);

		unsafe void Serialize(uint* v);

		unsafe void Serialize(long* v);

		unsafe void Serialize(ulong* v);

		unsafe void Serialize(int* v, int bits);

		unsafe void Serialize(uint* v, int bits);

		unsafe void SerializeBuffer(byte* buffer, int length);

		unsafe void SerializeBuffer(sbyte* buffer, int length);

		unsafe void SerializeBuffer(short* buffer, int length);

		unsafe void SerializeBuffer(ushort* buffer, int length);

		unsafe void SerializeBuffer(int* buffer, int length);

		unsafe void SerializeBuffer(uint* buffer, int length);

		unsafe void SerializeBuffer(long* buffer, int length);

		unsafe void SerializeBuffer(ulong* buffer, int length);

		bool Condition(bool condition);

		bool ReadBool();

		bool ReadBoolean();

		bool WriteBool(bool b);

		bool WriteBoolean(bool b);

		int ReadInt();

		void WriteInt(int v);

		void WriteUInt(uint v);

		uint ReadUInt();

		FP ReadFP();

		void WriteFP(FP fp);

		FPVector2 ReadFPVector2();

		void WriteFPVector2(FPVector2 fpVector2);

		FPVector3 ReadFPVector3();

		void WriteFPVector3(FPVector3 fpVector3);

		long ReadLong();

		void WriteLong(long v);

		ulong ReadULong();

		void WriteULong(ulong v);

		byte ReadByte();

		void WriteByte(byte p0);

		void CopyFromArray(byte[] data);

		unsafe void CopyFromBuffer(byte* data, int length);

		byte[] ToArray();

		void Reset();
	}
}

