using System;
using System.Text;
using Quantum;

namespace Photon.Deterministic
{
	/// <summary>
	/// Writes information at the bit level to a byte array.
	/// </summary>
	public class BitStream : IBitStream
	{
		/// <summary>
		/// Represents a delegate for serializing an array element.
		/// </summary>
		/// <typeparam name="T">Type of the array element.</typeparam>
		/// <param name="element">Reference to the array element.</param>
		public delegate void ArrayElementSerializer<T>(ref T element);

		private int _ptr;

		private int _maxPtr;

		private int _offsetBytes;

		private int _capacityBytes;

		private byte[] _data;

		private bool _write;

		/// <summary>
		/// Represents the position of the stream pointer in the <see cref="T:Photon.Deterministic.BitStream" />.
		/// </summary>
		public int Position
		{
			get
			{
				int num = _offsetBytes << 3;
				return _ptr - num;
			}
			set
			{
				int num = _offsetBytes << 3;
				_ptr = num + UdpMath.Clamp(value, 0, _maxPtr - num);
			}
		}

		/// <summary>
		/// Gets the number of bytes required to represent the current position of the stream pointer in the <see cref="T:Photon.Deterministic.BitStream" />.
		/// </summary>
		/// <remarks>
		/// This property is calculated based on the current position of the stream pointer in bits and converted to bytes.
		/// </remarks>
		public int BytesRequired => UdpMath.BytesRequired(Position);

		/// <summary>
		/// Gets a value indicating whether the position of the stream pointer in the <see cref="T:Photon.Deterministic.BitStream" /> is aligned on even bytes.
		/// </summary>
		/// <remarks>
		/// The position of the stream pointer is considered to be aligned on even bytes if the remainder of division by 8 is 0.
		/// For example, if the value is 16, it is aligned on even bytes, but if the value is 17, it is not aligned on even bytes.
		/// </remarks>
		public bool IsEvenBytes => _ptr % 8 == 0;

		/// <summary>
		/// Gets the capacity of the BitStream, in bytes.
		/// </summary>
		/// <value>
		/// The capacity of the BitStream, in bytes.
		/// </value>
		public int Capacity => _capacityBytes;

		/// <summary>
		/// Represents the offset in bytes from the beginning of the byte array in the <see cref="T:Photon.Deterministic.BitStream" />.
		/// </summary>
		public int Offset => _offsetBytes;

		/// <summary>
		/// Represents the offset in bits from the start of the <see cref="T:Photon.Deterministic.BitStream" />.
		/// </summary>
		public int OffsetBits => _offsetBytes / 8;

		/// <summary>
		/// Gets a value indicating whether the stream pointer of the <see cref="T:Photon.Deterministic.BitStream" /> has reached the maximum pointer position.
		/// </summary>
		public bool Done => _ptr == _maxPtr;

		/// <summary>
		/// Gets a value indicating whether the stream pointer of the <see cref="T:Photon.Deterministic.BitStream" /> has exceeded the maximum pointer position.
		/// </summary>
		/// <remarks>
		/// The <see cref="P:Photon.Deterministic.BitStream.Overflowing" /> property returns <see langword="true" /> if the stream pointer is greater than the maximum pointer position, indicating that the stream has overflowed.
		/// </remarks>
		public bool Overflowing => _ptr > _maxPtr;

		/// <summary>
		/// Indicates whether a bit stream is done (reached the end) or overflowing.
		/// </summary>
		/// <value><see langword="true" /> if the bit stream is done or overflowing; otherwise, <see langword="false" />.</value>
		public bool DoneOrOverflow
		{
			get
			{
				if (!Done)
				{
					return Overflowing;
				}
				return true;
			}
		}

		/// <summary>
		/// Represents the property indicating whether writing is enabled in the BitStream.
		/// </summary>
		/// <value>
		/// <see langword="true" /> if writing is enabled; otherwise, <see langword="false" />.
		/// </value>
		public bool Writing
		{
			get
			{
				return _write;
			}
			set
			{
				_write = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the stream is in reading mode.
		/// </summary>
		/// <value><see langword="true" /> if the stream is in reading mode; otherwise, <see langword="false" />.</value>
		public bool Reading
		{
			get
			{
				return !_write;
			}
			set
			{
				_write = !value;
			}
		}

		/// <summary>
		/// Represents the data buffer used by the <see cref="T:Photon.Deterministic.BitStream" />.
		/// </summary>
		public byte[] Data => _data;

		/// <summary>
		/// Constructs a new instance of the <see cref="T:Photon.Deterministic.BitStream" /> class with a default buffer size of 0.
		/// </summary>
		public BitStream()
			: this(new byte[0])
		{
		}

		/// <summary>
		/// Constructs a new instance of the <see cref="T:Photon.Deterministic.BitStream" /> class with the specified buffer size.
		/// </summary>
		/// <param name="size">The size of the internal buffer.</param>
		public BitStream(int size)
			: this(new byte[size])
		{
		}

		/// <summary>
		/// Constructs a new instance of the <see cref="T:Photon.Deterministic.BitStream" /> class with the specified buffer.
		/// </summary>
		/// <param name="arr">The source data buffer.</param>
		public BitStream(byte[] arr)
			: this(arr, arr.Length, 0)
		{
		}

		/// <summary>
		/// Constructs a new instance of the <see cref="T:Photon.Deterministic.BitStream" /> class with the specified buffer and size.
		/// </summary>
		/// <param name="arr">The source data buffer.</param>
		/// <param name="size">The size to use.</param>
		public BitStream(byte[] arr, int size)
			: this(arr, size, 0)
		{
		}

		/// <summary>
		/// Constructs a new instance of the <see cref="T:Photon.Deterministic.BitStream" /> class with the specified buffer, size, and offset.
		/// </summary>
		/// <param name="arr">The source data buffer.</param>
		/// <param name="size">The size to use.</param>
		/// <param name="offset">The offset to use.</param>
		public BitStream(byte[] arr, int size, int offset)
		{
			_data = arr;
			_ptr = offset << 3;
			_maxPtr = offset + size << 3;
			_offsetBytes = offset;
			_capacityBytes = size;
		}

		/// <summary>
		/// Sets the buffer of the BitStream with the specified byte array.
		/// </summary>
		/// <param name="arr">The byte array to set as the buffer.</param>
		public void SetBuffer(byte[] arr)
		{
			SetBuffer(arr, arr.Length, 0);
		}

		/// <summary>
		/// Sets the buffer and size of the BitStream instance.
		/// </summary>
		/// <param name="arr">The byte array buffer.</param>
		/// <param name="size">The size of the buffer.</param>
		public void SetBuffer(byte[] arr, int size)
		{
			SetBuffer(arr, size, 0);
		}

		/// <summary>
		/// Sets the buffer of the BitStream instance.
		/// </summary>
		/// <param name="arr">The byte array to set as the buffer for the BitStream.</param>
		/// <param name="size">The size of the buffer in bytes.</param>
		/// <param name="offset">The offset in bytes from the start of the buffer.</param>
		public void SetBuffer(byte[] arr, int size, int offset)
		{
			_data = arr;
			_ptr = offset << 3;
			_maxPtr = offset + size << 3;
			_offsetBytes = offset;
			_capacityBytes = size;
		}

		/// <summary>
		/// Rounds the current position in the stream to the nearest byte boundary and returns the number of bytes written up to that point.
		/// </summary>
		/// <returns>The number of bytes written up to the current position in the stream.</returns>
		public int RoundToByte()
		{
			int num = _ptr % 8;
			if (num > 0)
			{
				int num2 = 8 - num;
				if (_write)
				{
					WriteByte(0, num2);
				}
				else
				{
					_ptr += num2;
				}
			}
			return _ptr / 8;
		}

		/// <summary>
		/// Pads the stream with 0s to make it 4-byte exact.
		/// </summary>
		public void RoundTo4Bytes()
		{
			int i = Position / 8 % 4;
			if (i != 0)
			{
				for (; i < 4; i++)
				{
					WriteByte(0);
				}
			}
		}

		/// <summary>
		/// Determines whether the BitStream can write one bit.
		/// </summary>
		/// <returns><see langword="true" /> if the BitStream can write a bit, otherwise <see langword="false" />.</returns>
		public bool CanWrite()
		{
			return CanWrite(1);
		}

		/// <summary>
		/// Determines if there is at least one bit available to read.
		/// </summary>
		/// <returns>
		/// <see langword="true" /> if there is one bit available to read; otherwise, <see langword="false" />.
		/// </returns>
		public bool CanRead()
		{
			return CanRead(1);
		}

		/// <summary>
		/// Determines if the given number of bits can be written to the BitStream without exceeding the buffer size.
		/// </summary>
		/// <param name="bits">The number of bits to be written.</param>
		/// <returns>
		/// <see langword="true" /> if the given number of bits can be written to the BitStream without exceeding the buffer size;
		/// otherwise, <see langword="false" />.
		/// </returns>
		public bool CanWrite(int bits)
		{
			return _ptr + bits <= _maxPtr;
		}

		/// <summary>
		/// Determines whether the BitStream can read the specified number of bits.
		/// </summary>
		/// <param name="bits">The number of bits to check if can be read.</param>
		/// <returns><see langword="true" /> if the BitStream can read the specified number of bits, <see langword="false" /> otherwise.</returns>
		public bool CanRead(int bits)
		{
			return _ptr + bits <= _maxPtr;
		}

		/// <summary>
		/// Copies data from the input array to the internal buffer.
		/// </summary>
		/// <param name="array">The input array to copy data from.</param>
		public void CopyFromArray(byte[] array)
		{
			Array.Copy(array, 0, _data, _offsetBytes, array.Length);
			_ptr = _offsetBytes << 3;
			_maxPtr = _offsetBytes + array.Length << 3;
		}

		/// <summary>
		/// Copies a specified number of bytes from a buffer into the internal buffer of the BitStream.
		/// </summary>
		/// <param name="buffer">A pointer to the buffer containing the bytes to be copied.</param>
		/// <param name="length">The number of bytes to copy.</param>
		public unsafe void CopyFromBuffer(byte* buffer, int length)
		{
			fixed (byte* src = &_data[_offsetBytes])
			{
				Native.Utils.Copy(buffer, src, length);
			}
			_ptr = _offsetBytes << 3;
			_maxPtr = _offsetBytes + length << 3;
		}

		/// <summary>
		/// Block copy the bitstream buffer into an array.
		/// </summary>
		public void BlockCopyToArray(Array dst, int dstOffset)
		{
			Buffer.BlockCopy(_data, Offset, dst, dstOffset, BytesRequired);
		}

		/// <summary>
		/// Resets the BitStream to its initial state.
		/// </summary>
		public void Reset()
		{
			Reset(Capacity);
		}

		/// <summary>
		/// Resets the BitStream to its initial state, clearing the data and setting the pointer and maximum pointer values.
		/// </summary>
		public void Reset(int byteSize)
		{
			Array.Clear(_data, _offsetBytes, Capacity);
			_ptr = _offsetBytes << 3;
			_maxPtr = _offsetBytes + byteSize << 3;
		}

		/// <summary>
		/// Resets the BitStream to a specified byte size.
		/// </summary>
		/// <param name="byteSize">The size, in bytes, to reset the BitStream to.</param>
		public void ResetFast(int byteSize)
		{
			_ptr = _offsetBytes << 3;
			_maxPtr = _offsetBytes + byteSize << 3;
		}

		/// <summary>
		/// Copies the data from the BitStream into a new byte array.
		/// </summary>
		/// <returns>A new byte array containing the copied data.</returns>
		public byte[] ToArray()
		{
			byte[] array = new byte[BytesRequired];
			Buffer.BlockCopy(_data, _offsetBytes, array, 0, array.Length);
			return array;
		}

		/// <summary>
		/// Writes a boolean value to the bit stream.
		/// </summary>
		/// <param name="value">The boolean value to write.</param>
		/// <returns>The boolean value that was written to the bit stream.</returns>
		public bool WriteBool(bool value)
		{
			InternalWriteByte(value ? ((byte)1) : ((byte)0), 1);
			return value;
		}

		/// <summary>
		/// Writes a boolean value to the <see cref="T:Photon.Deterministic.BitStream" />.
		/// </summary>
		/// <param name="value">The boolean value to write.</param>
		/// <returns><see langword="true" /> if the value was written successfully; otherwise, <see langword="false" />.</returns>
		public bool WriteBoolean(bool value)
		{
			InternalWriteByte(value ? ((byte)1) : ((byte)0), 1);
			return value;
		}

		/// <summary>
		/// Reads a boolean value from the current position in the <see cref="T:Photon.Deterministic.BitStream" />.
		/// </summary>
		/// <returns>A boolean value that indicates whether the read value is <see langword="true" /> (1) or <see langword="false" /> (0).</returns>
		public bool ReadBool()
		{
			return InternalReadByte(1) == 1;
		}

		/// <summary>
		/// Reads a boolean value from the BitStream.
		/// </summary>
		/// <returns>Returns the boolean value read from the BitStream.</returns>
		public bool ReadBoolean()
		{
			return InternalReadByte(1) == 1;
		}

		/// <summary>
		/// Writes a byte value to the BitStream with a specified number of bits.
		/// </summary>
		/// <param name="value">The byte value to write.</param>
		/// <param name="bits">The number of bits to write.</param>
		public void WriteByte(byte value, int bits)
		{
			InternalWriteByte(value, bits);
		}

		/// <summary>
		/// Reads a byte from the BitStream with the specified number of bits.
		/// </summary>
		/// <param name="bits">The number of bits to read the byte.</param>
		/// <returns>The byte read from the BitStream.</returns>
		public byte ReadByte(int bits)
		{
			return InternalReadByte(bits);
		}

		/// <summary>
		/// Writes a byte value to the BitStream using 8 bits.
		/// </summary>
		/// <param name="value">The byte value to be written.</param>
		public void WriteByte(byte value)
		{
			WriteByte(value, 8);
		}

		/// <summary>
		/// Reads a byte from the bit stream using the default number of bits (8 bits).
		/// </summary>
		/// <returns>The byte read from the bit stream.</returns>
		public byte ReadByte()
		{
			return ReadByte(8);
		}

		/// <summary>
		/// Reads a signed byte value from the bit stream.
		/// </summary>
		/// <returns>
		/// A signed byte value read from the bit stream.
		/// </returns>
		public sbyte ReadSByte()
		{
			return (sbyte)ReadByte();
		}

		/// <summary>
		/// Writes a signed byte value to the bit stream.
		/// </summary>
		/// <param name="value">The signed byte value to write.</param>
		public void WriteSByte(sbyte value)
		{
			WriteByte((byte)value);
		}

		/// <summary>
		/// Writes an unsigned short value to the BitStream.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <param name="bits">The number of bits to write.</param>
		public void WriteUShort(ushort value, int bits)
		{
			if (bits <= 8)
			{
				InternalWriteByte((byte)(value & 0xFF), bits);
				return;
			}
			InternalWriteByte((byte)(value & 0xFF), 8);
			InternalWriteByte((byte)(value >> 8), bits - 8);
		}

		/// <summary>
		/// Reads an unsigned short value from the BitStream.
		/// </summary>
		/// <param name="bits">The number of bits to read, must be less than or equal to 16.</param>
		/// <returns>The unsigned short value read from the BitStream.</returns>
		public ushort ReadUShort(int bits)
		{
			if (bits <= 8)
			{
				return InternalReadByte(bits);
			}
			return (ushort)(InternalReadByte(8) | (InternalReadByte(bits - 8) << 8));
		}

		/// <summary>
		/// Writes a ushort value to the BitStream.
		/// </summary>
		/// <param name="value">The ushort value to write.</param>
		public void WriteUShort(ushort value)
		{
			WriteUShort(value, 16);
		}

		/// <summary>
		/// Reads an unsigned short (16 bits) from the BitStream.
		/// </summary>
		/// <returns>The value read from the BitStream.</returns>
		public ushort ReadUShort()
		{
			return ReadUShort(16);
		}

		/// <summary>
		/// Writes a signed 16-bit integer value to the BitStream with specified number of bits.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <param name="bits">The number of bits to write. Must be between 1 and 16, inclusive.</param>
		public void WriteShort(short value, int bits)
		{
			WriteUShort((ushort)value, bits);
		}

		/// <summary>
		/// Reads a short value from the BitStream.
		/// </summary>
		/// <param name="bits">The number of bits to read.</param>
		/// <returns>The short value read from the BitStream.</returns>
		public short ReadShort(int bits)
		{
			return (short)ReadUShort(bits);
		}

		/// <summary>
		/// Writes a short value to the BitStream using the specified number of bits.
		/// </summary>
		/// <param name="value">The short value to write.</param>
		/// <remarks>
		/// The specified number of bits determines the size of the value that can be written.
		/// </remarks>
		public void WriteShort(short value)
		{
			WriteShort(value, 16);
		}

		/// <summary>
		/// Reads a 16-bit integer from the bit stream.
		/// </summary>
		/// <returns>The 16-bit integer read from the bit stream.</returns>
		public short ReadShort()
		{
			return ReadShort(16);
		}

		/// <summary>
		/// Writes a character to the BitStream by converting it to a 16-bit unsigned integer.
		/// </summary>
		/// <param name="value">The character to write.</param>
		public void WriteChar(char value)
		{
			WriteUShort(value, 16);
		}

		/// <summary>
		/// Reads a character from the BitStream.
		/// </summary>
		/// <returns>
		/// The character read from the BitStream.
		/// </returns>
		public char ReadChar()
		{
			return (char)ReadUShort(16);
		}

		/// <summary>
		/// Writes a signed 64-bit integer using variable-length encoding to the bit stream.
		/// </summary>
		/// <param name="value">The signed 64-bit integer to write.</param>
		/// <param name="blockSize">The block size for encoding the integer.</param>
		public void WriteInt64VarLength(long value, int blockSize)
		{
			WriteUInt64VarLength((ulong)value, blockSize);
		}

		/// <summary>
		/// Writes a variable length int32 value to the BitStream.
		/// </summary>
		/// <param name="value">The int32 value to be written.</param>
		/// <param name="blockSize">The block size to use.</param>
		public void WriteInt32VarLength(int value, int blockSize)
		{
			WriteUInt32VarLength((uint)value, blockSize);
		}

		/// <summary>
		/// Reads a variable-length 64-bit signed integer from the <see cref="T:Photon.Deterministic.BitStream" /> with the specified block size.
		/// </summary>
		/// <param name="blockSize">The block size for reading the variable-length integer.</param>
		/// <returns>The read 64-bit signed integer.</returns>
		public long ReadInt64VarLength(int blockSize)
		{
			return (long)ReadUInt64VarLength(blockSize);
		}

		/// <summary>
		/// Reads a variable-length 32-bit integer from the buffer.
		/// </summary>
		/// <param name="blockSize">The block size used for encoding the integer.</param>
		/// <returns>The value of the read integer.</returns>
		public int ReadInt32VarLength(int blockSize)
		{
			return (int)ReadUInt32VarLength(blockSize);
		}

		/// <summary>
		/// Reads a variable length unsigned 32-bit integer from the BitStream.
		/// </summary>
		/// <param name="blockSize">The size of each block of bits to read.</param>
		/// <returns>The value of the variable length unsigned 32-bit integer.</returns>
		public uint ReadUInt32VarLength(int blockSize)
		{
			blockSize = Maths.Clamp(blockSize, 2, 16);
			int num = 1;
			while (!ReadBoolean() && !DoneOrOverflow)
			{
				num++;
			}
			if (DoneOrOverflow)
			{
				return 0u;
			}
			return ReadUInt(num * blockSize);
		}

		/// <summary>
		/// Reads an unsigned 64-bit integer with variable length from the BitStream.
		/// </summary>
		/// <param name="blockSize">The number of bits in each block. Must be between 2 and 16 (inclusive).</param>
		/// <returns>The read value as an unsigned 64-bit integer.</returns>
		public ulong ReadUInt64VarLength(int blockSize)
		{
			blockSize = Maths.Clamp(blockSize, 2, 16);
			int num = 1;
			while (!ReadBoolean() && !DoneOrOverflow)
			{
				num++;
			}
			if (DoneOrOverflow)
			{
				return 0uL;
			}
			return ReadULong(num * blockSize);
		}

		/// <summary>
		/// Writes a variable-length unsigned 32-bit integer to the BitStream.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <param name="blockSize">The number of bits per block. Must be between 2 and 16 (inclusive).</param>
		public void WriteUInt32VarLength(uint value, int blockSize)
		{
			blockSize = Maths.Clamp(blockSize, 2, 16);
			int num = (Maths.BitScanReverse(value) + blockSize) / blockSize;
			WriteUInt((uint)(1 << num - 1), num);
			WriteUInt(value, num * blockSize);
		}

		/// <summary>
		/// Writes a UInt64 variable-length value to the BitStream using the specified blockSize.
		/// </summary>
		/// <param name="value">The UInt64 value to write.</param>
		/// <param name="blockSize">The size of each block in bits.</param>
		public void WriteUInt64VarLength(ulong value, int blockSize)
		{
			blockSize = Maths.Clamp(blockSize, 2, 16);
			int num = (Maths.BitScanReverse(value) + blockSize) / blockSize;
			WriteUInt((uint)(1 << num - 1), num);
			WriteULong(value, num * blockSize);
		}

		/// <summary>
		/// Writes an unsigned integer value to the BitStream with the specified number of bits.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <param name="bits">The number of bits to write.</param>
		public void WriteUInt(uint value, int bits)
		{
			byte value2 = (byte)value;
			byte value3 = (byte)(value >> 8);
			byte value4 = (byte)(value >> 16);
			byte value5 = (byte)(value >> 24);
			switch ((bits + 7) / 8)
			{
			case 1:
				InternalWriteByte(value2, bits);
				break;
			case 2:
				InternalWriteByte(value2, 8);
				InternalWriteByte(value3, bits - 8);
				break;
			case 3:
				InternalWriteByte(value2, 8);
				InternalWriteByte(value3, 8);
				InternalWriteByte(value4, bits - 16);
				break;
			case 4:
				InternalWriteByte(value2, 8);
				InternalWriteByte(value3, 8);
				InternalWriteByte(value4, 8);
				InternalWriteByte(value5, bits - 24);
				break;
			}
		}

		/// <summary>
		/// Reads an unsigned integer from the <see cref="T:Photon.Deterministic.BitStream" /> with the specified number of bits.
		/// </summary>
		/// <param name="bits">The number of bits to read.</param>
		/// <returns>The unsigned integer read from the <see cref="T:Photon.Deterministic.BitStream" />.</returns>
		public uint ReadUInt(int bits)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			switch ((bits + 7) / 8)
			{
			case 1:
				num = InternalReadByte(bits);
				break;
			case 2:
				num = InternalReadByte(8);
				num2 = InternalReadByte(bits - 8);
				break;
			case 3:
				num = InternalReadByte(8);
				num2 = InternalReadByte(8);
				num3 = InternalReadByte(bits - 16);
				break;
			case 4:
				num = InternalReadByte(8);
				num2 = InternalReadByte(8);
				num3 = InternalReadByte(8);
				num4 = InternalReadByte(bits - 24);
				break;
			}
			return (uint)(num | (num2 << 8) | (num3 << 16) | (num4 << 24));
		}

		/// <summary>
		/// Writes an unsigned integer value of 32 bits to the bit stream.
		/// </summary>
		/// <param name="value">The unsigned integer value to write.</param>
		public void WriteUInt(uint value)
		{
			WriteUInt(value, 32);
		}

		/// <summary>
		/// Reads an unsigned 32-bit integer from the BitStream.
		/// </summary>
		/// <returns>The unsigned 32-bit integer read from the BitStream.</returns>
		public uint ReadUInt()
		{
			return ReadUInt(32);
		}

		/// <summary>
		/// Writes an integer value to the BitStream with specified number of bits and a shift value.
		/// </summary>
		/// <param name="value">The integer value to be written.</param>
		/// <param name="bits">The number of bits to use to represent the integer value.</param>
		/// <param name="shift">The shift value to apply to the integer value before writing.</param>
		public void WriteInt_Shifted(int value, int bits, int shift)
		{
			WriteInt(value, 32);
		}

		/// <summary>
		/// Reads an integer value shifted by the specified amount of bits from the BitStream.
		/// </summary>
		/// <param name="bits">The number of bits to read from the BitStream.</param>
		/// <param name="shift">The amount of bits to shift the read value by.</param>
		/// <returns>The integer value read from the BitStream, shifted by the specified amount of bits.</returns>
		public int ReadInt_Shifted(int bits, int shift)
		{
			return ReadInt(32);
		}

		/// <summary>
		/// Writes an integer value to the bitstream using the specified number of bits.
		/// </summary>
		/// <param name="value">The integer value to be written to the bitstream.</param>
		/// <param name="bits">The number of bits to use for writing the integer value.</param>
		public void WriteInt(int value, int bits)
		{
			WriteUInt((uint)value, bits);
		}

		/// <summary>
		/// Reads an integer value from the bit stream.
		/// </summary>
		/// <param name="bits">The number of bits to read.</param>
		/// <returns>The integer value read from the bit stream.</returns>
		public int ReadInt(int bits)
		{
			return (int)ReadUInt(bits);
		}

		/// <summary>
		/// Writes an integer value to the BitStream using a specified number of bits.
		/// </summary>
		/// <param name="value">The integer value to write</param>
		public void WriteInt(int value)
		{
			WriteInt(value, 32);
		}

		/// <summary>
		/// Reads an integer value from the bit stream.
		/// </summary>
		/// <returns>The integer value read from the bit stream.</returns>
		public int ReadInt()
		{
			return ReadInt(32);
		}

		/// <summary>
		/// Writes an unsigned long value to the bit stream.
		/// If the number of bits is less than or equal to 32, it writes the lower 32 bits of the value.
		/// Otherwise, it writes the lower 32 bits first, followed by the remaining bits.
		/// </summary>
		/// <param name="value">The value to be written</param>
		/// <param name="bits">The number of bits to write</param>
		public void WriteULong(ulong value, int bits)
		{
			if (bits <= 32)
			{
				WriteUInt((uint)(value & 0xFFFFFFFFu), bits);
				return;
			}
			WriteUInt((uint)value, 32);
			WriteUInt((uint)(value >> 32), bits - 32);
		}

		/// <summary>
		/// Reads an unsigned 64-bit integer (ulong) from the BitStream with the specified number of bits.
		/// If the specified number of bits is less than or equal to 32, it reads an unsigned 32-bit integer (uint) instead.
		/// </summary>
		/// <param name="bits">The number of bits to read.</param>
		/// <returns>The value read from the BitStream.</returns>
		public ulong ReadULong(int bits)
		{
			if (bits <= 32)
			{
				return ReadUInt(bits);
			}
			ulong num = ReadUInt(32);
			ulong num2 = ReadUInt(bits - 32);
			return num | (num2 << 32);
		}

		/// <summary>
		/// Writes an unsigned long value to the bit stream using a specified number of bits.
		/// </summary>
		/// <param name="value">The unsigned long value to write.</param>
		public void WriteULong(ulong value)
		{
			WriteULong(value, 64);
		}

		/// <summary>
		/// Reads an unsigned 64-bit integer from the BitStream.
		/// </summary>
		/// <returns>The unsigned 64-bit integer read from the BitStream.</returns>
		public ulong ReadULong()
		{
			return ReadULong(64);
		}

		/// <summary>
		/// Writes a long value to the BitStream with the specified number of bits.
		/// </summary>
		/// <param name="value">The long value to write.</param>
		/// <param name="bits">The number of bits to use for encoding the value.</param>
		public void WriteLong(long value, int bits)
		{
			WriteULong((ulong)value, bits);
		}

		/// <summary>
		/// Reads a long value from the BitStream with the specified number of bits.
		/// </summary>
		/// <param name="bits">The number of bits to read.</param>
		/// <returns>The long value read from the BitStream.</returns>
		public long ReadLong(int bits)
		{
			return (long)ReadULong(bits);
		}

		/// <summary>
		/// Writes a long value to the BitStream.
		/// </summary>
		/// <param name="value">The long value to write.</param>
		public void WriteLong(long value)
		{
			WriteLong(value, 64);
		}

		/// <summary>
		/// Reads a long integer from the BitStream.
		/// </summary>
		/// <returns>The long integer value.</returns>
		public long ReadLong()
		{
			return ReadLong(64);
		}

		/// <summary>
		/// Writes a single-precision floating-point value to the <see cref="T:Photon.Deterministic.BitStream" />.
		/// </summary>
		/// <param name="value">The value to be written.</param>
		public void WriteFloat(float value)
		{
			UdpByteConverter udpByteConverter = value;
			InternalWriteByte(udpByteConverter.Byte0, 8);
			InternalWriteByte(udpByteConverter.Byte1, 8);
			InternalWriteByte(udpByteConverter.Byte2, 8);
			InternalWriteByte(udpByteConverter.Byte3, 8);
		}

		/// <summary>
		/// Reads a single-precision floating-point number from the BitStream.
		/// </summary>
		/// <returns>The single-precision floating-point number read from the BitStream.</returns>
		public float ReadFloat()
		{
			UdpByteConverter udpByteConverter = new UdpByteConverter
			{
				Byte0 = InternalReadByte(8),
				Byte1 = InternalReadByte(8),
				Byte2 = InternalReadByte(8),
				Byte3 = InternalReadByte(8)
			};
			return udpByteConverter.Float32;
		}

		/// <summary>
		/// Writes a double value to the bit stream.
		/// </summary>
		/// <param name="value">The double value to write.</param>
		public void WriteDouble(double value)
		{
			UdpByteConverter udpByteConverter = value;
			InternalWriteByte(udpByteConverter.Byte0, 8);
			InternalWriteByte(udpByteConverter.Byte1, 8);
			InternalWriteByte(udpByteConverter.Byte2, 8);
			InternalWriteByte(udpByteConverter.Byte3, 8);
			InternalWriteByte(udpByteConverter.Byte4, 8);
			InternalWriteByte(udpByteConverter.Byte5, 8);
			InternalWriteByte(udpByteConverter.Byte6, 8);
			InternalWriteByte(udpByteConverter.Byte7, 8);
		}

		/// <summary>
		/// Reads a double value from the bit stream.
		/// </summary>
		/// <returns>The double value read from the bit stream.</returns>
		public double ReadDouble()
		{
			UdpByteConverter udpByteConverter = new UdpByteConverter
			{
				Byte0 = InternalReadByte(8),
				Byte1 = InternalReadByte(8),
				Byte2 = InternalReadByte(8),
				Byte3 = InternalReadByte(8),
				Byte4 = InternalReadByte(8),
				Byte5 = InternalReadByte(8),
				Byte6 = InternalReadByte(8),
				Byte7 = InternalReadByte(8)
			};
			return udpByteConverter.Float64;
		}

		/// <summary>
		/// Writes a byte array to the BitStream.
		/// </summary>
		/// <param name="from">The byte array to write</param>
		public void WriteByteArray(byte[] from)
		{
			WriteByteArray(from, 0, from.Length);
		}

		/// <summary>
		/// Writes a byte array to the BitStream starting from a specified offset and writing a specified number of bytes.
		/// </summary>
		/// <param name="from">The byte array to write from.</param>
		/// <param name="count">The number of bytes to write.</param>
		public void WriteByteArray(byte[] from, int count)
		{
			WriteByteArray(from, 0, count);
		}

		/// <summary>
		/// Writes a byte array to the BitStream.
		/// </summary>
		/// <param name="from">The byte array to write.</param>
		/// <param name="offset">The starting index of the byte array to write.</param>
		/// <param name="count">The number of bytes to write from the byte array.</param>
		public void WriteByteArray(byte[] from, int offset, int count)
		{
			int num = _ptr >> 3;
			int num2 = _ptr % 8;
			int num3 = 8 - num2;
			if (num2 == 0)
			{
				Buffer.BlockCopy(from, offset, _data, num, count);
			}
			else
			{
				for (int i = 0; i < count; i++)
				{
					byte b = from[offset + i];
					_data[num] &= (byte)(255 >> num3);
					_data[num] |= (byte)(b << num2);
					num++;
					_data[num] &= (byte)(255 << num2);
					_data[num] |= (byte)(b >> num3);
				}
			}
			_ptr += count * 8;
		}

		/// <summary>
		/// Reads a byte array from the <see cref="T:Photon.Deterministic.BitStream" /> with the specified size.
		/// </summary>
		/// <param name="size">The size of the byte array to read.</param>
		/// <returns>The byte array read from the <see cref="T:Photon.Deterministic.BitStream" />.</returns>
		public byte[] ReadByteArray(int size)
		{
			byte[] array = new byte[size];
			ReadByteArray(array);
			return array;
		}

		/// <summary>
		/// Reads a byte array from the <see cref="T:Photon.Deterministic.BitStream" /> object.
		/// </summary>
		/// <param name="to">The byte array to read the data into.</param>
		public void ReadByteArray(byte[] to)
		{
			ReadByteArray(to, 0, to.Length);
		}

		/// <summary>
		/// Reads a byte array from the BitStream, starting from the current position.
		/// </summary>
		/// <param name="to">The array to read into.</param>
		/// <param name="count">The number of bytes to read.</param>
		public void ReadByteArray(byte[] to, int count)
		{
			ReadByteArray(to, 0, count);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="to"></param>
		/// <param name="offset"></param>
		/// <param name="count"></param>
		public void ReadByteArray(byte[] to, int offset, int count)
		{
			int num = _ptr >> 3;
			int num2 = _ptr % 8;
			if (num2 == 0)
			{
				Buffer.BlockCopy(_data, num, to, offset, count);
			}
			else
			{
				int num3 = 8 - num2;
				for (int i = 0; i < count; i++)
				{
					int num4 = _data[num] >> num2;
					num++;
					int num5 = _data[num] & (255 >> num3);
					to[offset + i] = (byte)(num4 | (num5 << num3));
				}
			}
			_ptr += count * 8;
		}

		/// <summary>
		/// Writes a byte array to the BitStream with a length prefix.
		/// </summary>
		/// <param name="array">The byte array to write.</param>
		public void WriteByteArrayLengthPrefixed(byte[] array)
		{
			WriteByteArrayLengthPrefixed(array, (array != null) ? array.Length : 0);
		}

		/// <summary>
		/// Writes a byte array to the bit stream in a length-prefixed format.
		/// </summary>
		/// <param name="array">The byte array to write.</param>
		/// <param name="maxLength">The maximum length of the byte array to write.</param>
		public void WriteByteArrayLengthPrefixed(byte[] array, int maxLength)
		{
			if (!WriteBool(array != null))
			{
				return;
			}
			int num = Math.Min(array.Length, maxLength);
			if (num < array.Length)
			{
				LogStream logWarn = InternalLogStreams.LogWarn;
				if (logWarn != null)
				{
					logWarn.Log($"Only sending {num}/{array.Length} bytes from byte array");
				}
			}
			WriteUShort((ushort)num);
			WriteByteArray(array, 0, num);
		}

		/// <summary>
		/// Reads a byte array from the bit stream that is length prefixed.
		/// </summary>
		/// <returns>The byte array read from the bit stream. Returns <see langword="null" /> if the length prefix is <see langword="false" />.</returns>
		public byte[] ReadByteArrayLengthPrefixed()
		{
			if (ReadBool())
			{
				byte[] array = new byte[ReadUShort()];
				ReadByteArray(array, 0, array.Length);
				return array;
			}
			return null;
		}

		/// <summary>
		/// Writes a string value to the BitStream using the specified encoding.
		/// </summary>
		/// <param name="value">The string value to write.</param>
		/// <param name="encoding">The encoding to use.</param>
		public void WriteString(string value, Encoding encoding)
		{
			if (!WriteBool(value == null))
			{
				byte[] bytes = encoding.GetBytes(value);
				WriteUShort((ushort)bytes.Length);
				WriteByteArray(bytes);
			}
		}

		/// <summary>
		/// Writes a string to the BitStream using the specified encoding.
		/// </summary>
		/// <param name="value">The string to write to the BitStream.</param>
		public void WriteString(string value)
		{
			WriteString(value, Encoding.UTF8);
		}

		/// <summary>
		/// Reads a string from the bit stream using the specified encoding.
		/// </summary>
		/// <param name="encoding">The encoding used to decode the string.</param>
		/// <returns>The string value.</returns>
		public string ReadString(Encoding encoding)
		{
			if (ReadBool())
			{
				return null;
			}
			int num = ReadUShort();
			if (num == 0)
			{
				return "";
			}
			byte[] array = new byte[num];
			ReadByteArray(array);
			return encoding.GetString(array, 0, array.Length);
		}

		/// <summary>
		/// Reads a string from the BitStream using the specified encoding.
		/// </summary>
		/// <returns>A string value read from the BitStream.</returns>
		public string ReadString()
		{
			return ReadString(Encoding.UTF8);
		}

		/// <summary>
		/// Writes a string to the BitStream using GZip compression.
		/// </summary>
		/// <param name="value">The string value to write.</param>
		/// <param name="encoding">The encoding to use when compressing the string.</param>
		public void WriteStringGZip(string value, Encoding encoding)
		{
			if (!WriteBool(value == null))
			{
				byte[] array = ByteUtils.GZipCompressString(value, encoding);
				WriteUShort((ushort)array.Length);
				WriteByteArray(array);
			}
		}

		/// <summary>
		/// Reads a compressed string from the BitStream using GZip decompression.
		/// </summary>
		/// <param name="encoding">The encoding to use for the string.</param>
		/// <returns>The decompressed string.</returns>
		public string ReadStringGZip(Encoding encoding)
		{
			if (ReadBool())
			{
				return null;
			}
			ushort num = ReadUShort();
			if (num == 0)
			{
				return "";
			}
			byte[] array = new byte[num];
			ReadByteArray(array);
			return ByteUtils.GZipDecompressString(array, encoding);
		}

		/// <summary>
		/// Writes a Guid to the BitStream.
		/// </summary>
		/// <param name="guid">The Guid to write.</param>
		public void WriteGuid(Guid guid)
		{
			WriteByteArray(guid.ToByteArray());
		}

		/// <summary>
		/// Reads a Guid from the BitStream by reading 16 bytes and constructing a Guid object.
		/// </summary>
		/// <returns>
		/// The Guid read from the BitStream.
		/// </returns>
		public Guid ReadGuid()
		{
			byte[] array = new byte[16];
			ReadByteArray(array);
			return new Guid(array);
		}

		/// <summary>
		/// Writes a byte value with the specified number of bits to the internal buffer at the current position.
		/// </summary>
		/// <param name="value">The byte to write.</param>
		/// <param name="bits">The number of bits to use for writing the byte.</param>
		private void InternalWriteByte(byte value, int bits)
		{
			WriteByteAt(_data, _ptr, bits, value);
			_ptr += bits;
		}

		/// <summary>
		/// Writes the given fixed-point value to the bit stream.
		/// </summary>
		/// <param name="fp">The fixed-point value to write.</param>
		public void WriteFP(FP fp)
		{
			WriteLong(fp.RawValue);
		}

		/// <summary>
		/// The BitStream class is used for reading and writing data in a binary format.
		/// </summary>
		/// <remarks>
		/// This class provides methods for reading and writing various types of data, including integers, floats, and custom types such as FP (fixed-point), FPVector2, and FPVector3.
		/// </remarks>
		public FP ReadFP()
		{
			return FP.FromRaw(ReadLong());
		}

		/// <summary>
		/// Writes a nullable fixed-point value to the BitStream.
		/// </summary>
		/// <param name="fp">The nullable fixed-point value to write.</param>
		public void WriteNullableFP(FP fp)
		{
			WriteLong(fp.RawValue);
		}

		/// <summary>
		/// Writes an instance of FPVector2 to the BitStream.
		/// </summary>
		/// <param name="v">The FPVector2 instance to write.</param>
		public void WriteFPVector2(FPVector2 v)
		{
			WriteFP(v.X);
			WriteFP(v.Y);
		}

		/// <summary>
		/// Reads an FPVector2 from the current BitStream.
		/// </summary>
		/// <returns>The read FPVector2.</returns>
		public FPVector2 ReadFPVector2()
		{
			return new FPVector2(ReadFP(), ReadFP());
		}

		/// <summary>
		/// Writes an FPVector3 to the BitStream.
		/// </summary>
		/// <param name="v">The FPVector3 value to write.</param>
		public void WriteFPVector3(FPVector3 v)
		{
			WriteFP(v.X);
			WriteFP(v.Y);
			WriteFP(v.Z);
		}

		/// <summary>
		/// Reads a FPVector3 from the BitStream.
		/// </summary>
		/// <returns>The read FPVector3.</returns>
		public FPVector3 ReadFPVector3()
		{
			return new FPVector3(ReadFP(), ReadFP(), ReadFP());
		}

		/// <summary>
		/// Writes a <see cref="T:Photon.Deterministic.FPQuaternion" /> value to the <see cref="T:Photon.Deterministic.BitStream" />.
		/// </summary>
		/// <param name="v">The <see cref="T:Photon.Deterministic.FPQuaternion" /> value to write.</param>
		public void WriteFPQuaternion(FPQuaternion v)
		{
			WriteFP(v.X);
			WriteFP(v.Y);
			WriteFP(v.Z);
			WriteFP(v.W);
		}

		/// <summary>
		/// Reads and returns an instance of FPQuaternion from the current BitStream.
		/// </summary>
		/// <returns>The read FPQuaternion.</returns>
		public FPQuaternion ReadFPQuaternion()
		{
			return new FPQuaternion(ReadFP(), ReadFP(), ReadFP(), ReadFP());
		}

		/// <summary>
		/// Writes a FPMatrix2x2 to the BitStream.
		/// </summary>
		/// <param name="v">The FPMatrix2x2 to write.</param>
		public void WriteFPMatrix2x2(FPMatrix2x2 v)
		{
			WriteFP(v.M00);
			WriteFP(v.M10);
			WriteFP(v.M01);
			WriteFP(v.M11);
		}

		/// <summary>
		/// Reads a 2x2 matrix of fixed-point numbers from the BitStream.
		/// </summary>
		/// <returns>The 2x2 matrix of fixed-point numbers read from the BitStream.</returns>
		public FPMatrix2x2 ReadFPMatrix2x2()
		{
			FPMatrix2x2 result = default(FPMatrix2x2);
			result.M00 = ReadFP();
			result.M10 = ReadFP();
			result.M01 = ReadFP();
			result.M11 = ReadFP();
			return result;
		}

		/// <summary>
		/// Writes a 3x3 matrix of fixed-point numbers to the current instance of the BitStream.
		/// </summary>
		/// <param name="v">The FPMatrix3x3 to write.</param>
		public void WriteFPMatrix3x3(FPMatrix3x3 v)
		{
			WriteFP(v.M00);
			WriteFP(v.M10);
			WriteFP(v.M20);
			WriteFP(v.M01);
			WriteFP(v.M11);
			WriteFP(v.M21);
			WriteFP(v.M02);
			WriteFP(v.M12);
			WriteFP(v.M22);
		}

		/// Reads a 3x3 matrix of fixed-point numbers from the BitStream. <returns>The read 3x3 matrix of fixed-point numbers.</returns>
		/// /
		public FPMatrix3x3 ReadFPMatrix3x3()
		{
			FPMatrix3x3 result = default(FPMatrix3x3);
			result.M00 = ReadFP();
			result.M10 = ReadFP();
			result.M20 = ReadFP();
			result.M01 = ReadFP();
			result.M11 = ReadFP();
			result.M21 = ReadFP();
			result.M02 = ReadFP();
			result.M12 = ReadFP();
			result.M22 = ReadFP();
			return result;
		}

		/// <summary>
		/// Writes a 4x4 matrix of Fixed Point (FP) values to the bit stream.
		/// </summary>
		/// <param name="v">The 4x4 matrix of FP values to be written.</param>
		public void WriteFPMatrix4x4(FPMatrix4x4 v)
		{
			WriteFP(v.M00);
			WriteFP(v.M10);
			WriteFP(v.M20);
			WriteFP(v.M30);
			WriteFP(v.M01);
			WriteFP(v.M11);
			WriteFP(v.M21);
			WriteFP(v.M31);
			WriteFP(v.M02);
			WriteFP(v.M12);
			WriteFP(v.M22);
			WriteFP(v.M32);
			WriteFP(v.M03);
			WriteFP(v.M13);
			WriteFP(v.M23);
			WriteFP(v.M33);
		}

		/// <summary>
		/// Reads a 4x4 matrix of Fixed Point numbers from the BitStream.
		/// </summary>
		/// <returns>The read <see cref="T:Photon.Deterministic.FPMatrix4x4" /> value.</returns>
		public FPMatrix4x4 ReadFPMatrix4x4()
		{
			FPMatrix4x4 result = default(FPMatrix4x4);
			result.M00 = ReadFP();
			result.M10 = ReadFP();
			result.M20 = ReadFP();
			result.M30 = ReadFP();
			result.M01 = ReadFP();
			result.M11 = ReadFP();
			result.M21 = ReadFP();
			result.M31 = ReadFP();
			result.M02 = ReadFP();
			result.M12 = ReadFP();
			result.M22 = ReadFP();
			result.M32 = ReadFP();
			result.M03 = ReadFP();
			result.M13 = ReadFP();
			result.M23 = ReadFP();
			result.M33 = ReadFP();
			return result;
		}

		/// <summary>
		/// Writes an instance of <see cref="T:Photon.Deterministic.FPBounds2" /> to the current <see cref="T:Photon.Deterministic.BitStream" /> object.
		/// </summary>
		/// <param name="v">The <see cref="T:Photon.Deterministic.FPBounds2" /> instance to write.</param>
		public void WriteFPBounds2(FPBounds2 v)
		{
			WriteFPVector2(v.Center);
			WriteFPVector2(v.Extents);
		}

		/// <summary>
		/// Reads an instance of <see cref="T:Photon.Deterministic.FPBounds2" /> from the <see cref="T:Photon.Deterministic.BitStream" />.
		/// </summary>
		/// <returns>The read <see cref="T:Photon.Deterministic.FPBounds2" />.</returns>
		/// <remarks>
		/// The method reads two instances of <see cref="T:Photon.Deterministic.FPVector2" /> from the
		/// <see cref="T:Photon.Deterministic.BitStream" /> and assigns them to the corresponding fields of <see cref="T:Photon.Deterministic.FPBounds2" />.
		/// </remarks>
		public FPBounds2 ReadFPBounds2()
		{
			FPBounds2 result = default(FPBounds2);
			result.Center = ReadFPVector2();
			result.Extents = ReadFPVector2();
			return result;
		}

		/// <summary>
		/// Writes the FPBounds3 structure to the BitStream.
		/// </summary>
		/// <param name="v">The FPBounds3 structure to be written.</param>
		public void WriteFPBounds3(FPBounds3 v)
		{
			WriteFPVector3(v.Center);
			WriteFPVector3(v.Extents);
		}

		/// <summary>
		/// Reads a <see cref="T:Photon.Deterministic.FPBounds3" /> structure from the <see cref="T:Photon.Deterministic.BitStream" />.
		/// </summary>
		/// <returns>The <see cref="T:Photon.Deterministic.FPBounds3" /> structure read from the <see cref="T:Photon.Deterministic.BitStream" />.</returns>
		public FPBounds3 ReadFPBounds3()
		{
			FPBounds3 result = default(FPBounds3);
			result.Center = ReadFPVector3();
			result.Extents = ReadFPVector3();
			return result;
		}

		/// <summary>
		/// Writes a byte value at the specified position in a byte array, considering the number of bits to be written for the value.
		/// </summary>
		/// <param name="data">The byte array in which the value will be written.</param>
		/// <param name="ptr">The position in the byte array to write the value.</param>
		/// <param name="bits">The number of bits to be written for the value.</param>
		/// <param name="value">The byte value to be written.</param>
		public static void WriteByteAt(byte[] data, int ptr, int bits, byte value)
		{
			if (bits > 0)
			{
				value = (byte)(value & (255 >> 8 - bits));
				int num = ptr >> 3;
				int num2 = ptr & 7;
				int num3 = 8 - num2;
				int num4 = num3 - bits;
				if (num4 >= 0)
				{
					int num5 = (255 >> num3) | (255 << 8 - num4);
					data[num] = (byte)((data[num] & num5) | (value << num2));
				}
				else
				{
					data[num] = (byte)((data[num] & (255 >> num3)) | (value << num2));
					data[num + 1] = (byte)((data[num + 1] & (255 << bits - num3)) | (value >> num3));
				}
			}
		}

		/// <summary>
		/// Reads a byte from the internal buffer with the specified number of bits.
		/// </summary>
		/// <param name="bits">The number of bits to read.</param>
		/// <returns>The byte value read from the internal buffer.</returns>
		private byte InternalReadByte(int bits)
		{
			if (bits <= 0)
			{
				return 0;
			}
			int num = _ptr >> 3;
			int num2 = _ptr % 8;
			byte result;
			if (num2 == 0 && bits == 8)
			{
				result = _data[num];
			}
			else
			{
				int num3 = _data[num] >> num2;
				int num4 = bits - (8 - num2);
				if (num4 < 1)
				{
					result = (byte)(num3 & (255 >> 8 - bits));
				}
				else
				{
					int num5 = _data[num + 1] & (255 >> 8 - num4);
					result = (byte)(num3 | (num5 << bits - num4));
				}
			}
			_ptr += bits;
			return result;
		}

		/// <summary>
		/// Serializes or deserializes a condition value.
		/// </summary>
		/// <param name="condition">The condition value to serialize or deserialize.</param>
		/// <returns>The serialized or deserialized condition value.</returns>
		public bool Condition(bool condition)
		{
			if (_write)
			{
				WriteBool(condition);
			}
			else
			{
				condition = ReadBool();
			}
			return condition;
		}

		/// <summary>
		/// Serializes or deserializes a string value.
		/// </summary>
		/// <param name="value">The string value to serialize or deserialize.</param>
		public void Serialize(ref string value)
		{
			if (_write)
			{
				WriteString(value, Encoding.UTF8);
			}
			else
			{
				value = ReadString(Encoding.UTF8);
			}
		}

		/// <summary>
		/// Serializes or deserializes a boolean value.
		/// </summary>
		/// <param name="value">The boolean value to serialize or deserialize.</param>
		public void Serialize(ref bool value)
		{
			if (_write)
			{
				WriteBool(value);
			}
			else
			{
				value = ReadBool();
			}
		}

		/// <summary>
		/// Serializes or deserializes a string value.
		/// </summary>
		/// <param name="value">The string value to serialize or deserialize.</param>
		public void Serialize(ref float value)
		{
			if (_write)
			{
				WriteFloat(value);
			}
			else
			{
				value = ReadFloat();
			}
		}

		/// <summary>
		/// Serializes or deserializes a <see cref="T:System.Double" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Double" /> value to serialize or deserialize.</param>
		public void Serialize(ref double value)
		{
			if (_write)
			{
				WriteDouble(value);
			}
			else
			{
				value = ReadDouble();
			}
		}

		/// <summary>
		/// Serializes or deserializes a 64-bit signed integer value.
		/// </summary>
		/// <param name="value">The 64-bit signed integer value to serialize or deserialize.</param>
		public void Serialize(ref long value)
		{
			if (_write)
			{
				WriteLong(value);
			}
			else
			{
				value = ReadLong();
			}
		}

		/// <summary>
		/// Serializes or deserializes a UInt64 value.
		/// </summary>
		/// <param name="value">The UInt64 value to serialize or deserialize.</param>
		public void Serialize(ref ulong value)
		{
			if (_write)
			{
				WriteULong(value);
			}
			else
			{
				value = ReadULong();
			}
		}

		/// <summary>
		/// Serializes or deserializes a ushort (UInt16) value.
		/// </summary>
		/// <param name="value">The value to serialize or deserialize.</param>
		public void Serialize(ref ushort value)
		{
			if (_write)
			{
				WriteUShort(value);
			}
			else
			{
				value = ReadUShort();
			}
		}

		/// <summary>
		/// Serializes or deserializes an FP value.
		/// </summary>
		/// <param name="value">The FP value to serialize or deserialize.</param>
		public void Serialize(ref FP value)
		{
			if (_write)
			{
				WriteFP(value);
			}
			else
			{
				value = ReadFP();
			}
		}

		/// <summary>
		/// Serializes or deserializes a <see cref="T:Photon.Deterministic.FPVector2" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:Photon.Deterministic.FPVector2" /> value to serialize or deserialize.</param>
		public void Serialize(ref FPVector2 value)
		{
			if (_write)
			{
				WriteFPVector2(value);
			}
			else
			{
				value = ReadFPVector2();
			}
		}

		/// <summary>
		/// Serializes or deserializes a FPVector3 value.
		/// </summary>
		/// <param name="value">The FPVector3 value to serialize or deserialize.</param>
		public void Serialize(ref FPVector3 value)
		{
			if (_write)
			{
				WriteFPVector3(value);
			}
			else
			{
				value = ReadFPVector3();
			}
		}

		/// <summary>
		/// Serializes or deserializes an FPQuaternion value.
		/// </summary>
		/// <param name="value">The FPQuaternion value to serialize or deserialize.</param>
		public void Serialize(ref FPQuaternion value)
		{
			if (_write)
			{
				WriteFPQuaternion(value);
			}
			else
			{
				value = ReadFPQuaternion();
			}
		}

		/// <summary>
		/// Serializes or deserializes a Byte value.
		/// </summary>
		/// <param name="value">The value to serialize or deserialize.</param>
		public void Serialize(ref byte value)
		{
			if (_write)
			{
				WriteByte(value);
			}
			else
			{
				value = ReadByte();
			}
		}

		/// <summary>
		/// Serializes or deserializes a string value.
		/// </summary>
		/// <param name="value">The string value to serialize or deserialize.</param>
		public void Serialize(ref uint value)
		{
			Serialize(ref value, 32);
		}

		/// <summary>
		/// Serializes or deserializes a given uint value.
		/// </summary>
		/// <param name="value">The value to serialize or deserialize.</param>
		/// <param name="bits">The number of bits to use for serialization or deserialization.</param>
		public void Serialize(ref uint value, int bits)
		{
			if (_write)
			{
				WriteUInt(value, bits);
			}
			else
			{
				value = ReadUInt(bits);
			}
		}

		/// <summary>
		/// Serializes or deserializes a ulong value with a specified number of bits.
		/// </summary>
		/// <param name="value">The ulong value to serialize or deserialize.</param>
		/// <param name="bits">The number of bits to use for serialization or deserialization.</param>
		public void Serialize(ref ulong value, int bits)
		{
			if (_write)
			{
				WriteULong(value, bits);
			}
			else
			{
				value = ReadULong(bits);
			}
		}

		/// <summary>
		/// Serializes or deserializes a value of type int.
		/// </summary>
		/// <param name="value">The value of type int to serialize or deserialize.</param>
		public void Serialize(ref int value)
		{
			Serialize(ref value, 32);
		}

		/// <summary>
		/// Serializes or deserializes a value of type int with the specified number of bits.
		/// </summary>
		/// <param name="value">The int value to serialize or deserialize.</param>
		/// <param name="bits">The number of bits used to represent the int value.</param>
		public void Serialize(ref int value, int bits)
		{
			if (_write)
			{
				WriteInt(value, bits);
			}
			else
			{
				value = ReadInt(bits);
			}
		}

		/// <summary>
		/// Serializes or deserializes an array of Int32 values.
		/// </summary>
		/// <param name="value">The array of Int32 values to serialize or deserialize.</param>
		public void Serialize(ref int[] value)
		{
			if (_write)
			{
				if (WriteBool(value != null))
				{
					WriteUShort((ushort)value.Length);
					for (int i = 0; i < value.Length; i++)
					{
						WriteInt(value[i]);
					}
				}
			}
			else if (ReadBool())
			{
				value = new int[ReadUShort()];
				for (int j = 0; j < value.Length; j++)
				{
					value[j] = ReadInt();
				}
			}
			else
			{
				value = null;
			}
		}

		/// <summary>
		/// Serializes or deserializes a byte array.
		/// </summary>
		/// <param name="value">The byte array to serialize or deserialize.</param>
		public void Serialize(ref byte[] value)
		{
			if (_write)
			{
				WriteByteArrayLengthPrefixed(value, value?.Length ?? 0);
			}
			else
			{
				value = ReadByteArrayLengthPrefixed();
			}
		}

		/// <summary>
		/// Serializes the given BitStream as a byte array.
		/// </summary>
		/// <param name="otherStream">The BitStream to be serialized.</param>
		/// <exception cref="T:System.NotImplementedException">Thrown if the otherStream's Data array is <see langword="null" />.</exception>
		public void SerializeAsByteArray(BitStream otherStream)
		{
			if (_write)
			{
				if (!WriteBool(otherStream.Data != null))
				{
					throw new NotImplementedException();
				}
				int num = Math.Min(otherStream.Data.Length, otherStream.BytesRequired);
				WriteUShort((ushort)num);
				WriteByteArray(otherStream.Data, 0, num);
			}
		}

		/// <summary>
		/// Serializes or deserializes an array of bytes with a variable length.
		/// </summary>
		/// <param name="array">The array to serialize or deserialize.</param>
		/// <param name="length">The length of the array.</param>
		public void Serialize(ref byte[] array, ref int length)
		{
			if (_write)
			{
				if (WriteBool(array != null))
				{
					WriteUShort((ushort)length);
					WriteByteArray(array, 0, length);
				}
			}
			else if (ReadBool())
			{
				length = ReadUShort();
				if (array == null || array.Length < length)
				{
					array = new byte[length];
				}
				ReadByteArray(array, 0, length);
			}
		}

		/// <summary>
		/// Serializes or deserializes a fixed-sized byte array.
		/// </summary>
		/// <param name="value">The byte array to serialize or deserialize.</param>
		/// <param name="fixedSize">The fixed size of the byte array.</param>
		public void Serialize(ref byte[] value, int fixedSize)
		{
			if (_write)
			{
				if (WriteBoolean(value != null && value.Length != 0))
				{
					WriteByteArray(value, fixedSize);
				}
			}
			else if (ReadBoolean())
			{
				value = ReadByteArray(fixedSize);
			}
			else
			{
				value = null;
			}
		}

		/// <summary>
		/// Serializes or deserializes a byte array with a fixed size.
		/// </summary>
		/// <param name="array">The byte array to serialize or deserialize.</param>
		/// <param name="length">The length of the byte array.</param>
		/// <param name="fixedSize">The fixed size of the byte array.</param>
		public void Serialize(ref byte[] array, ref int length, int fixedSize)
		{
			length = fixedSize;
			if (_write)
			{
				if (WriteBoolean(array != null && array.Length != 0))
				{
					WriteByteArray(array, fixedSize);
				}
			}
			else if (ReadBoolean())
			{
				if (array == null || array.Length < fixedSize)
				{
					array = new byte[fixedSize];
				}
				ReadByteArray(array, fixedSize);
			}
			else
			{
				array = null;
			}
		}

		/// <summary>
		/// Serializes or deserializes the length of a generic array.
		/// </summary>
		/// <typeparam name="T">The type of the elements in the array.</typeparam>
		/// <param name="array">The array to serialize or deserialize.</param>
		/// <param name="maxLength">The max size of the input array as a safety mechanism to limit the buffers allocated on deserializing commands for example.</param>
		/// <returns>The length of the array secured by maxLength.</returns>
		public int SerializeArrayLength<T>(ref T[] array, int maxLength = int.MaxValue)
		{
			if (_write)
			{
				int num = Math.Min((array != null) ? array.Length : 0, maxLength);
				WriteInt(num);
				return num;
			}
			int num2 = Math.Min(ReadInt(), maxLength);
			array = new T[num2];
			return num2;
		}

		/// <summary>
		/// Serializes or deserializes an array using a custom serializer.
		/// </summary>
		/// <typeparam name="T">The type of elements in the array.</typeparam>
		/// <param name="array">The array to serialize or deserialize.</param>
		/// <param name="serializer">The custom serializer for the array elements.</param>
		/// <param name="maxLength">The max size of the input array as a safety mechanism to limit the buffers allocated on deserializing commands for example.</param>
		/// <remarks>
		/// If write mode is enabled, the method serializes the length of the array, followed by each element of the array using the provided serializer.
		/// If write mode is disabled, the method deserializes the length of the array, creates a new array of the appropriate length,
		/// and populates the new array by deserializing each array element using the provided serializer.
		/// </remarks>
		public void SerializeArray<T>(ref T[] array, ArrayElementSerializer<T> serializer, int maxLength = int.MaxValue)
		{
			int num = SerializeArrayLength(ref array, maxLength);
			if (array != null)
			{
				for (int i = 0; i < num; i++)
				{
					serializer(ref array[i]);
				}
			}
		}

		/// <summary>
		/// Serializes or deserializes a byte value.
		/// </summary>
		/// <param name="v">The byte value to serialize or deserialize.</param>
		public unsafe void Serialize(byte* v)
		{
			if (_write)
			{
				WriteByte(*v);
			}
			else
			{
				*v = ReadByte();
			}
		}

		/// <summary>
		/// Serializes or deserializes a value.
		/// </summary>
		/// <param name="v">The value to serialize or deserialize.</param>
		public unsafe void Serialize(sbyte* v)
		{
			if (_write)
			{
				WriteSByte(*v);
			}
			else
			{
				*v = ReadSByte();
			}
		}

		/// <summary>
		/// Serializes or deserializes a short value.
		/// </summary>
		/// <param name="v">Pointer to the short value to serialize or deserialize.</param>
		public unsafe void Serialize(short* v)
		{
			if (_write)
			{
				WriteShort(*v);
			}
			else
			{
				*v = ReadShort();
			}
		}

		/// <summary>
		/// Serializes or deserializes a ushort value.
		/// </summary>
		/// <param name="v">The ushort value to serialize or deserialize.</param>
		public unsafe void Serialize(ushort* v)
		{
			if (_write)
			{
				WriteUShort(*v);
			}
			else
			{
				*v = ReadUShort();
			}
		}

		/// <summary>
		/// Serializes or deserializes a value of type int*.
		/// </summary>
		/// <param name="v">The pointer to the int value to serialize or deserialize.</param>
		public unsafe void Serialize(int* v)
		{
			if (_write)
			{
				WriteInt(*v);
			}
			else
			{
				*v = ReadInt();
			}
		}

		/// <summary>
		/// Serializes or deserializes a pointer to an unsigned integer.
		/// </summary>
		/// <param name="v">The pointer to an unsigned integer to serialize or deserialize.</param>
		public unsafe void Serialize(uint* v)
		{
			if (_write)
			{
				WriteUInt(*v);
			}
			else
			{
				*v = ReadUInt();
			}
		}

		/// <summary>
		/// Serializes or deserializes a long value.
		/// </summary>
		/// <param name="v">A pointer to the long value to serialize or deserialize.</param>
		public unsafe void Serialize(long* v)
		{
			if (_write)
			{
				WriteLong(*v);
			}
			else
			{
				*v = ReadLong();
			}
		}

		/// <summary>
		/// Serializes or deserializes the value pointed to by a ulong pointer.
		/// </summary>
		/// <param name="v">The ulong pointer to the value to serialize or deserialize.</param>
		public unsafe void Serialize(ulong* v)
		{
			if (_write)
			{
				WriteULong(*v);
			}
			else
			{
				*v = ReadULong();
			}
		}

		/// <summary>
		/// Serializes or deserializes a string value.
		/// </summary>
		/// <param name="v">The string value to serialize or deserialize.</param>
		/// <param name="bits">The amount of bits to serialize or deserialize.</param>
		public unsafe void Serialize(uint* v, int bits)
		{
			if (_write)
			{
				WriteUInt(*v, bits);
			}
			else
			{
				*v = ReadUInt(bits);
			}
		}

		/// <summary>
		/// Serializes or deserializes a value.
		/// </summary>
		/// <param name="v">The value to serialize or deserialize.</param>
		/// <param name="bits">The number of bits to use for serialization.</param>
		public unsafe void Serialize(int* v, int bits)
		{
			if (_write)
			{
				WriteInt(*v, bits);
			}
			else
			{
				*v = ReadInt(bits);
			}
		}

		/// <summary>
		/// Serializes or deserializes a buffer of bytes.
		/// </summary>
		/// <param name="buffer">A pointer to the buffer of bytes to serialize or deserialize.</param>
		/// <param name="length">The length of the buffer.</param>
		/// <remarks>
		/// If in write mode, the method writes each byte from the buffer to the stream.
		/// If in read mode, the method reads each byte from the stream and stores it in the buffer.
		/// </remarks>
		public unsafe void SerializeBuffer(byte* buffer, int length)
		{
			if (_write)
			{
				for (int i = 0; i < length; i++)
				{
					WriteByte(buffer[i]);
				}
			}
			else
			{
				for (int j = 0; j < length; j++)
				{
					buffer[j] = ReadByte();
				}
			}
		}

		/// <summary>
		/// Serializes or deserializes a buffer of signed bytes.
		/// </summary>
		/// <param name="buffer">The buffer of signed bytes to serialize or deserialize.</param>
		/// <param name="length">The length of the buffer.</param>
		public unsafe void SerializeBuffer(sbyte* buffer, int length)
		{
			if (_write)
			{
				for (int i = 0; i < length; i++)
				{
					WriteSByte(buffer[i]);
				}
			}
			else
			{
				for (int j = 0; j < length; j++)
				{
					buffer[j] = ReadSByte();
				}
			}
		}

		/// <summary>
		/// Serializes or deserializes a buffer of type `short`.
		/// </summary>
		/// <param name="buffer">The buffer to serialize or deserialize.</param>
		/// <param name="length">The length of the buffer.</param>
		/// <remarks>
		/// If `_write` is <see langword="true" />, the method writes each element of the buffer to the bit stream.
		/// If `_write` is <see langword="false" />, the method reads each element of the buffer from the bit stream.
		/// </remarks>
		public unsafe void SerializeBuffer(short* buffer, int length)
		{
			if (_write)
			{
				for (int i = 0; i < length; i++)
				{
					WriteShort(buffer[i]);
				}
			}
			else
			{
				for (int j = 0; j < length; j++)
				{
					buffer[j] = ReadShort();
				}
			}
		}

		/// <summary>
		/// Serializes or deserializes a buffer of unsigned shorts.
		/// </summary>
		/// <param name="buffer">The buffer of unsigned shorts to serialize or deserialize.</param>
		/// <param name="length">The length of the buffer.</param>
		public unsafe void SerializeBuffer(ushort* buffer, int length)
		{
			if (_write)
			{
				for (int i = 0; i < length; i++)
				{
					WriteUShort(buffer[i]);
				}
			}
			else
			{
				for (int j = 0; j < length; j++)
				{
					buffer[j] = ReadUShort();
				}
			}
		}

		/// <summary>
		/// Serializes or deserializes a buffer of integers.
		/// </summary>
		/// <param name="buffer">The buffer of integers to serialize or deserialize.</param>
		/// <param name="length">The length of the buffer.</param>
		public unsafe void SerializeBuffer(int* buffer, int length)
		{
			if (_write)
			{
				for (int i = 0; i < length; i++)
				{
					WriteInt(buffer[i]);
				}
			}
			else
			{
				for (int j = 0; j < length; j++)
				{
					buffer[j] = ReadInt();
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="length"></param>
		public unsafe void SerializeBuffer(uint* buffer, int length)
		{
			if (_write)
			{
				for (int i = 0; i < length; i++)
				{
					WriteUInt(buffer[i]);
				}
			}
			else
			{
				for (int j = 0; j < length; j++)
				{
					buffer[j] = ReadUInt();
				}
			}
		}

		/// <summary>
		/// Serializes or deserializes a buffer of long values.
		/// </summary>
		/// <param name="buffer">The buffer of long values to serialize or deserialize.</param>
		/// <param name="length">The length of the buffer.</param>
		public unsafe void SerializeBuffer(long* buffer, int length)
		{
			if (_write)
			{
				for (int i = 0; i < length; i++)
				{
					WriteLong(buffer[i]);
				}
			}
			else
			{
				for (int j = 0; j < length; j++)
				{
					buffer[j] = ReadLong();
				}
			}
		}

		/// <summary>
		/// Serializes or deserializes a buffer of ulong values.
		/// </summary>
		/// <param name="buffer">A pointer to the buffer to serialize or deserialize.</param>
		/// <param name="length">The length of the buffer.</param>
		public unsafe void SerializeBuffer(ulong* buffer, int length)
		{
			if (_write)
			{
				for (int i = 0; i < length; i++)
				{
					WriteULong(buffer[i]);
				}
			}
			else
			{
				for (int j = 0; j < length; j++)
				{
					buffer[j] = ReadULong();
				}
			}
		}
	}
}

