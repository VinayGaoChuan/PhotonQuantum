using System;
using System.Collections.Generic;
using Photon.Deterministic.Protocol;

namespace Photon.Deterministic
{
	/// <summary>
	/// The internal input structure for one player and one tick.
	/// </summary>
	/// <summary>
	/// Represents input at a specific tick.
	/// </summary>
	[Serializable]
	public class DeterministicTickInput
	{
		/// <summary>
		/// The input objects are pooled.
		/// </summary>
		public class Pool
		{
			private Stack<DeterministicTickInput> _pool = new Stack<DeterministicTickInput>();

			/// <summary>
			/// Create a new input object from the pool.
			/// </summary>
			/// <returns>DeterministicTickInput instance</returns>
			public DeterministicTickInput Acquire()
			{
				lock (_pool)
				{
					if (_pool.Count > 0)
					{
						DeterministicTickInput deterministicTickInput = _pool.Pop();
						deterministicTickInput.Pooled = false;
						return deterministicTickInput;
					}
				}
				return new DeterministicTickInput();
			}

			/// <summary>
			/// Return the input object to the pool.
			/// Will reset the object before.
			/// </summary>
			/// <param name="input">Input object to be pooled</param>
			public void Release(DeterministicTickInput input)
			{
				input.Tick = 0;
				input.PlayerIndex = 0;
				input.DataLength = 0;
				input.Rpc = null;
				input.Sent = false;
				input.Next = null;
				if (input.DataArray != null)
				{
					Array.Clear(input.DataArray, 0, input.DataArray.Length);
				}
				input.Pooled = true;
				lock (_pool)
				{
					_pool.Push(input);
				}
			}
		}

		/// <summary>
		/// The corrupted exception is thrown when error during the deserialization are detected.
		/// </summary>
		public class CorruptedException : Exception
		{
			/// <summary>
			/// Create a new corrupted exception.
			/// </summary>
			public CorruptedException()
			{
			}

			/// <summary>
			/// Create a new corrupted exception with a message.
			/// </summary>
			/// <param name="message">Debug message</param>
			public CorruptedException(string message)
				: base(message)
			{
			}
		}

		private struct DecodeBitSet
		{
			private unsafe fixed uint _bits[4];

			public unsafe static void Set(DecodeBitSet* set, int bit)
			{
				set->_bits[bit / 32] |= (uint)(1 << bit % 32);
			}

			public unsafe static void ClearAll(DecodeBitSet* set)
			{
				*set->_bits = 0u;
				set->_bits[1] = 0u;
				set->_bits[2] = 0u;
				set->_bits[3] = 0u;
			}

			public unsafe static bool IsSet(DecodeBitSet* set, int bit)
			{
				return (set->_bits[bit / 32] & (uint)(1 << bit % 32)) != 0;
			}
		}

		/// <summary>
		/// Has this input been sent to the client.
		/// </summary>
		[NonSerialized]
		public bool Sent;

		[NonSerialized]
		internal bool Pooled;

		[NonSerialized]
		internal DeterministicTickInput Next;

		/// <summary>
		/// The tick of this input.
		/// </summary>
		public int Tick;

		/// <summary>
		/// This represents the player index when sent from the server.
		/// Is the player slot on input upstream.
		/// </summary>
		public int PlayerIndex;

		/// <summary>
		/// Rpc data.
		/// </summary>
		public byte[] Rpc;

		/// <summary>
		/// Input data.
		/// </summary>
		public byte[] DataArray;

		/// <summary>
		/// Input data length.
		/// </summary>
		public int DataLength;

		/// <summary>
		/// Not serialized (for delta decompression only)
		/// </summary>
		public int ReferenceTick;

		/// <summary>
		/// The input flags assigned by the server.
		/// </summary>
		public DeterministicInputFlags Flags;

		/// <summary>
		/// Copy the array into the <see cref="F:Photon.Deterministic.DeterministicTickInput.DataArray" />.
		/// </summary>
		/// <param name="array">Source input data array</param>
		/// <param name="length">Source input data array size</param>
		public void CopyToDataArray(byte[] array, int length)
		{
			if (DataArray == null || DataArray.Length < length)
			{
				DataArray = new byte[length];
			}
			DataLength = length;
			Buffer.BlockCopy(array, 0, DataArray, 0, DataLength);
		}

		/// <summary>
		/// Internal method to serialize the input data.
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">Bitstream</param>
		/// <param name="header">Header to write</param>
		public void SerializeForDecoder(Serializer serializer, BitStream stream, DeterministicTickInputEncodeHeader header)
		{
			if (stream.Writing)
			{
				if (stream.WriteBoolean(Flags == (DeterministicInputFlags.Repeatable | DeterministicInputFlags.PlayerNotPresent) && DataLength == 0 && Rpc == null))
				{
					return;
				}
			}
			else if (stream.ReadBoolean())
			{
				Flags = DeterministicInputFlags.Repeatable | DeterministicInputFlags.PlayerNotPresent;
				DataLength = 0;
				Rpc = null;
				return;
			}
			stream.Serialize(ref DataArray, ref DataLength, header.InputFixedSize);
			if (stream.Reading)
			{
				Flags = (DeterministicInputFlags)stream.ReadByte(4);
			}
			else
			{
				stream.WriteByte((byte)Flags, 4);
			}
		}

		/// <summary>
		/// Read and write rpc data from the input stream.
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">Bitstream</param>
		/// <param name="header">Header</param>
		public void SerializeForDecoderRPC(Serializer serializer, BitStream stream, DeterministicTickInputEncodeHeader header)
		{
			if (Flags != (DeterministicInputFlags.Repeatable | DeterministicInputFlags.PlayerNotPresent) || DataLength != 0 || Rpc != null)
			{
				stream.Serialize(ref Rpc);
			}
		}

		/// <summary>
		/// Internal method to serialize the input data.
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">Bitstream</param>
		/// <param name="size">Input size</param>
		/// <param name="ignoreRpc">Ignore rpc</param>
		/// <param name="includeHeader">Include a header</param>
		/// <exception cref="T:Photon.Deterministic.DeterministicTickInput.CorruptedException"></exception>
		public void SimpleSerialize(Serializer serializer, BitStream stream, int size, bool ignoreRpc = false, bool includeHeader = true)
		{
			if (includeHeader)
			{
				stream.Serialize(ref PlayerIndex, 8);
				stream.Serialize(ref Tick);
			}
			if (stream.Writing)
			{
				stream.WriteByteArray(DataArray);
			}
			else
			{
				DataLength = size;
				DataArray = stream.ReadByteArray(DataLength);
			}
			if (!ignoreRpc)
			{
				stream.Serialize(ref Rpc);
			}
			else if (stream.Writing)
			{
				stream.WriteBool(value: false);
			}
			else if (stream.ReadBool())
			{
				throw new CorruptedException("Found invalid rpc");
			}
			if (includeHeader)
			{
				if (stream.Reading)
				{
					Flags = (DeterministicInputFlags)stream.ReadByte();
				}
				else
				{
					stream.WriteByte((byte)Flags);
				}
			}
		}

		/// <summary>
		/// Internal method to deserialized multiple inputs from a bitstream.
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">Bitstream</param>
		/// <param name="result">Resulting list of inputs</param>
		/// <param name="pool">Input object pool to use</param>
		/// <param name="expectedDataLength">Expected input data length to validate, 0 to disable</param>
		/// <param name="ignoreRpc">Ignore the rpc data</param>
		/// <exception cref="T:Photon.Deterministic.DeterministicTickInput.CorruptedException">Is raised when the input length mismatches</exception>
		public static void SimpleDeserializeMultiple(Serializer serializer, BitStream stream, List<DeterministicTickInput> result, Stack<DeterministicTickInput> pool, int expectedDataLength, bool ignoreRpc = false)
		{
			while (stream.CanRead(32))
			{
				DeterministicTickInput deterministicTickInput = ((pool == null || pool.Count <= 0) ? new DeterministicTickInput() : pool.Pop());
				deterministicTickInput.SimpleSerialize(serializer, stream, expectedDataLength, ignoreRpc);
				if (expectedDataLength > 0 && deterministicTickInput.DataLength != expectedDataLength)
				{
					for (int i = 0; i < result.Count; i++)
					{
						pool.Push(result[i]);
					}
					result.Clear();
					pool.Push(deterministicTickInput);
					throw new CorruptedException($"Unexpected data length: expected {expectedDataLength} actual {deterministicTickInput.DataLength}");
				}
				result.Add(deterministicTickInput);
			}
		}

		/// <summary>
		/// Internal method to deserialized multiple inputs from a bitstream.
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">Bitstream</param>
		/// <param name="result">Resulting list of inputs</param>
		/// <param name="pool">Input object pool to use</param>
		/// <param name="expectedDataLength">Expected input data length to validate, 0 to disable</param>
		/// <param name="ignoreRpc">Ignore the rpc data</param>
		/// <param name="delta">Is input delta compressed</param>
		/// <exception cref="T:Photon.Deterministic.DeterministicTickInput.CorruptedException">Is raised when the input length mismatches</exception>
		public static void DeserializeMultiple(Serializer serializer, BitStream stream, List<DeterministicTickInput> result, Stack<DeterministicTickInput> pool, int expectedDataLength, bool ignoreRpc = false, bool delta = false)
		{
			while (stream.CanRead(32))
			{
				DeterministicTickInput deterministicTickInput = ((pool == null || pool.Count <= 0) ? new DeterministicTickInput() : pool.Pop());
				if (delta)
				{
					deterministicTickInput.PlayerIndex = stream.ReadInt(8);
					deterministicTickInput.Tick = stream.ReadInt();
					deterministicTickInput.ReferenceTick = stream.ReadInt();
					deterministicTickInput.Flags = (DeterministicInputFlags)stream.ReadByte();
					stream.Serialize(ref deterministicTickInput.DataArray, ref deterministicTickInput.DataLength);
				}
				else
				{
					deterministicTickInput.SimpleSerialize(serializer, stream, expectedDataLength, ignoreRpc);
				}
				if (!delta && expectedDataLength > 0 && deterministicTickInput.DataLength != expectedDataLength)
				{
					for (int i = 0; i < result.Count; i++)
					{
						pool.Push(result[i]);
					}
					result.Clear();
					pool.Push(deterministicTickInput);
					throw new CorruptedException($"Unexpected data length: expected {expectedDataLength} actual {deterministicTickInput.DataLength}");
				}
				result.Add(deterministicTickInput);
			}
		}

		/// <summary>
		/// Clone the object.
		/// </summary>
		/// <returns>A new cloned instance.</returns>
		public DeterministicTickInput Clone()
		{
			DeterministicTickInput deterministicTickInput = new DeterministicTickInput
			{
				Sent = Sent,
				Tick = Tick,
				PlayerIndex = PlayerIndex,
				DataLength = DataLength,
				Flags = Flags
			};
			if (Rpc != null)
			{
				deterministicTickInput.Rpc = new byte[Rpc.Length];
				Array.Copy(Rpc, deterministicTickInput.Rpc, Rpc.Length);
			}
			if (DataArray != null)
			{
				deterministicTickInput.DataArray = new byte[DataArray.Length];
				Array.Copy(DataArray, deterministicTickInput.DataArray, DataArray.Length);
			}
			return deterministicTickInput;
		}

		/// <summary>
		/// Override ToString method to debug output readable class members.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return $"[Input Tick={Tick} PlayerIndex={PlayerIndex}]";
		}

		/// <summary>
		/// Legacy serialization.
		/// </summary>
		public void Legacy_Serialize_Packed(Serializer serializer, BitStream stream, DeterministicTickInputEncodeHeader header)
		{
			stream.Serialize(ref DataArray, header.InputFixedSize);
			stream.Serialize(ref Rpc);
			if (stream.Reading)
			{
				Flags = (DeterministicInputFlags)stream.ReadByte(4);
			}
			else
			{
				stream.WriteByte((byte)Flags, 4);
			}
		}

		/// <summary>
		/// Legacy serialization.
		/// </summary>
		public void Legacy_Serialize_Simple(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref PlayerIndex, 8);
			stream.Serialize(ref Tick);
			stream.Serialize(ref DataArray);
			stream.Serialize(ref Rpc);
			if (stream.Reading)
			{
				Flags = (DeterministicInputFlags)stream.ReadByte();
			}
			else
			{
				stream.WriteByte((byte)Flags);
			}
		}

		/// <summary>
		/// Legacy serialization.
		/// </summary>
		public static void Legacy_DecodeMultiple_Simple(Serializer serializer, BitStream stream, List<DeterministicTickInput> result)
		{
			while (stream.CanRead(32))
			{
				DeterministicTickInput deterministicTickInput = new DeterministicTickInput();
				deterministicTickInput.Legacy_Serialize_Simple(serializer, stream);
				result.Add(deterministicTickInput);
			}
		}

		/// <summary>
		/// Legacy serialization.
		/// </summary>
		public unsafe static void Legacy_DecodeMultiple_Packed(Serializer serializer, BitStream stream, List<DeterministicTickInput> result)
		{
			DeterministicTickInputEncodeHeader header = default(DeterministicTickInputEncodeHeader);
			header.Legacy_Serialize(stream);
			DecodeBitSet decodeBitSet = default(DecodeBitSet);
			DecodeBitSet* set = &decodeBitSet;
			while (stream.CanRead(32))
			{
				int tick = stream.ReadInt();
				DecodeBitSet.ClearAll(set);
				for (int i = 0; i < header.PlayerCount; i++)
				{
					if (stream.ReadBoolean())
					{
						DecodeBitSet.Set(set, i);
					}
				}
				for (int j = 0; j < header.PlayerCount; j++)
				{
					if (DecodeBitSet.IsSet(set, j))
					{
						DeterministicTickInput deterministicTickInput = new DeterministicTickInput();
						deterministicTickInput.Tick = tick;
						deterministicTickInput.PlayerIndex = j;
						deterministicTickInput.Legacy_Serialize_Packed(serializer, stream, header);
						result.Add(deterministicTickInput);
					}
				}
			}
		}
	}
}

