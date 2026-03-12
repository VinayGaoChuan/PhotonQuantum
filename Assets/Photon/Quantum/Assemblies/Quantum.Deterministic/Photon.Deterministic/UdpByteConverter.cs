using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	[StructLayout(LayoutKind.Explicit)]
	internal struct UdpByteConverter
	{
		[FieldOffset(0)]
		public short Signed16;

		[FieldOffset(0)]
		public ushort Unsigned16;

		[FieldOffset(0)]
		public char Char;

		[FieldOffset(0)]
		public int Signed32;

		[FieldOffset(0)]
		public uint Unsigned32;

		[FieldOffset(0)]
		public long Signed64;

		[FieldOffset(0)]
		public ulong Unsigned64;

		[FieldOffset(0)]
		public float Float32;

		[FieldOffset(0)]
		public double Float64;

		[FieldOffset(0)]
		public byte Byte0;

		[FieldOffset(1)]
		public byte Byte1;

		[FieldOffset(2)]
		public byte Byte2;

		[FieldOffset(3)]
		public byte Byte3;

		[FieldOffset(4)]
		public byte Byte4;

		[FieldOffset(5)]
		public byte Byte5;

		[FieldOffset(6)]
		public byte Byte6;

		[FieldOffset(7)]
		public byte Byte7;

		public static implicit operator UdpByteConverter(short val)
		{
			return new UdpByteConverter
			{
				Signed16 = val
			};
		}

		public static implicit operator UdpByteConverter(ushort val)
		{
			return new UdpByteConverter
			{
				Unsigned16 = val
			};
		}

		public static implicit operator UdpByteConverter(char val)
		{
			return new UdpByteConverter
			{
				Char = val
			};
		}

		public static implicit operator UdpByteConverter(uint val)
		{
			return new UdpByteConverter
			{
				Unsigned32 = val
			};
		}

		public static implicit operator UdpByteConverter(int val)
		{
			return new UdpByteConverter
			{
				Signed32 = val
			};
		}

		public static implicit operator UdpByteConverter(ulong val)
		{
			return new UdpByteConverter
			{
				Unsigned64 = val
			};
		}

		public static implicit operator UdpByteConverter(long val)
		{
			return new UdpByteConverter
			{
				Signed64 = val
			};
		}

		public static implicit operator UdpByteConverter(float val)
		{
			return new UdpByteConverter
			{
				Float32 = val
			};
		}

		public static implicit operator UdpByteConverter(double val)
		{
			return new UdpByteConverter
			{
				Float64 = val
			};
		}
	}
}

