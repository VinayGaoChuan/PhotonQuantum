using System;
using Quantum;

namespace Photon.Deterministic
{
	public class DeterministicDeltaCompression
	{
		public const int RawInputBufferLength = 32768;

		private BitStream _deltaCompressedWriter;

		private BitStream _deltaCompressedReader;

		private IDeltaCompressor _compressor;

		private int[] _sharedInputSet;

		private int[] _currentInputSet;

		private ushort _lastWords;

		private DeterministicTickInputEncodeHeader _header;

		private DeterministicTickInput _reusableTickInput = new DeterministicTickInput();

		public int[] CurrentRawInput => _sharedInputSet;

		public DeterministicDeltaCompression()
		{
			_deltaCompressedWriter = new BitStream(32768);
			_deltaCompressedWriter.Writing = true;
			_deltaCompressedReader = new BitStream(98304);
			_deltaCompressedReader.Reading = true;
			_compressor = new DeltaCompressorDefault();
			_sharedInputSet = new int[32768];
			_currentInputSet = new int[32768];
		}

		public byte[] ComputeDelta(byte[] raw, int offset, int length, int tick, int rawInputLength)
		{
			Assert.Always(length % 4 == 0, "Length must be power of 4");
			Assert.Always<int, int, int>(offset + length < raw.Length, "Offset ({0}) + Length ({1}) must be smaller than raw.Length ({2})", offset, length, raw.Length);
			Array.Clear(_currentInputSet, 0, _currentInputSet.Length);
			Buffer.BlockCopy(raw, offset, _currentInputSet, 0, length);
			ushort num = (ushort)(length / 4);
			ushort num2 = Math.Max(num, _lastWords);
			_deltaCompressedWriter.Reset();
			_deltaCompressedWriter.WriteInt(tick);
			_deltaCompressedWriter.WriteUShort(num2);
			_compressor.Pack(_currentInputSet, _sharedInputSet, num2, _deltaCompressedWriter);
			byte[] result = _deltaCompressedWriter.ToArray();
			_lastWords = num;
			Array.Copy(_currentInputSet, _sharedInputSet, _currentInputSet.Length);
			return result;
		}

		private void DebugDelta(byte[] raw, byte[] compressed, int tick)
		{
			LogStream logInfo = InternalLogStreams.LogInfo;
			if (logInfo != null)
			{
				logInfo.Log("raw length: " + raw.Length + ", delta: " + compressed.Length);
			}
			BitStream bitStream = new BitStream(compressed);
			bitStream.Reading = true;
			int num = bitStream.ReadInt();
			ushort words = bitStream.ReadUShort();
			int[] array = new int[_sharedInputSet.Length];
			Array.Copy(_sharedInputSet, array, array.Length);
			_compressor.Unpack(array, words, bitStream);
			bool flag = num != tick;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != _currentInputSet[i])
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				LogStream logInfo2 = InternalLogStreams.LogInfo;
				if (logInfo2 != null)
				{
					logInfo2.Log("Diff found, tick: " + tick);
				}
			}
		}

		public byte[] Decode(byte[] data, int dataOffset, int dataSize, int player, int referenceTick, DeterministicSessionConfig sessionConfig)
		{
			_deltaCompressedReader.SetBuffer(data, dataSize, dataOffset);
			int playerCount = sessionConfig.PlayerCount;
			_header.InputFixedSize = sessionConfig.InputFixedSize;
			while (_deltaCompressedReader.CanRead(32))
			{
				_deltaCompressedReader.RoundToByte();
				int tick = _deltaCompressedReader.ReadInt();
				bool flag = _deltaCompressedReader.ReadBoolean();
				InputSetMask inputSetMask = default(InputSetMask);
				inputSetMask.Serialize(_deltaCompressedReader, playerCount);
				for (int i = 0; i < playerCount; i++)
				{
					if (inputSetMask.Contains(i))
					{
						_reusableTickInput.Tick = tick;
						_reusableTickInput.PlayerIndex = i;
						_reusableTickInput.SerializeForDecoder(null, _deltaCompressedReader, _header);
						if (i == player)
						{
							return _reusableTickInput.DataArray;
						}
					}
				}
			}
			return null;
		}
	}
}

