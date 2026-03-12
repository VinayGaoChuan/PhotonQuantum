using System;
using System.Collections.Generic;
using Photon.Deterministic.Protocol;
using Quantum;

namespace Photon.Deterministic
{
	internal class ReplayInputProvider : IDeterministicReplayProvider, IDeterministicRpcProvider, IDeterministicInputProvider, IDeterministicDeltaCompressedInput
	{
		private IDeterministicStreamReplayInputProvider _streamInputProvider;

		private DeterministicSession _session;

		private DeterministicTickInputDecoder _decoder;

		private Serializer _serializer = new Serializer();

		private List<DeterministicTickInput> _decodeResult = new List<DeterministicTickInput>();

		private BitStream _stream = new BitStream();

		private IDeltaCompressor _compressor = new DeltaCompressorDefault();

		private byte[] _buffer;

		private int[] _sharedInput = new int[32768];

		private int _sharedInputFrame = -1;

		private RawInputCache _rawInputCache = new RawInputCache();

		public int LocalActorNumber { get; set; }

		public ReplayInputProvider(IDeterministicStreamReplayInputProvider streamInputProvider, DeterministicSession session)
		{
			_streamInputProvider = streamInputProvider;
			_session = session;
		}

		public bool CanSimulate(int frame)
		{
			return frame <= _streamInputProvider.MaxFrame;
		}

		public void ResetInputState(DeterministicFrame frame)
		{
			Array.Copy(frame.GetRawInputs(), _sharedInput, _sharedInput.Length);
			_sharedInputFrame = -1;
			_streamInputProvider.Reset();
			_rawInputCache.Clear();
		}

		public void AddRpc(int playerSlot, byte[] data, bool command)
		{
		}

		public QTuple<byte[], bool> GetRpc(int frame, int player)
		{
			AdvanceCurrentFrame(frame);
			Assert.Always<int, int>(player < _decodeResult.Count, "Rpc for player {0} not found, player count = {1}", player, _decodeResult.Count);
			Assert.Always<int, int>(frame == _decodeResult[0].Tick, "Unexpected Rpc for frame {0} (expected={1})", frame, _decodeResult[0].Tick);
			DeterministicTickInput deterministicTickInput = _decodeResult[player];
			return QTuple.Create(deterministicTickInput.Rpc, (deterministicTickInput.Flags & DeterministicInputFlags.Command) == DeterministicInputFlags.Command);
		}

		public DeterministicFrameInputTemp GetInput(int frame, int player)
		{
			AdvanceCurrentFrame(frame);
			Assert.Always<int, List<DeterministicTickInput>>(player < _decodeResult.Count, "Input for player {0} not found, player count = {1}", player, _decodeResult);
			Assert.Always<int, int>(frame == _decodeResult[0].Tick, "Unexpected Input for frame {0} (expected={1})", frame, _decodeResult[0].Tick);
			DeterministicTickInput deterministicTickInput = _decodeResult[player];
			return DeterministicFrameInputTemp.Verified(deterministicTickInput.Tick, deterministicTickInput.PlayerIndex, deterministicTickInput.Rpc, deterministicTickInput.DataArray, deterministicTickInput.DataLength, deterministicTickInput.Flags);
		}

		/// <summary>
		/// Resize temp byte buffer
		/// </summary>
		private static void EnsureTempBufferSize(ref byte[] buf, int size)
		{
			if (buf == null)
			{
				buf = new byte[size];
			}
			else if (buf.Length < size)
			{
				Array.Resize(ref buf, size);
			}
		}

		/// <summary>
		/// Advance the current frame to the required tick while reading from the DC input stream. Only moves forward.
		/// Writes the results into _decodeResult.
		/// </summary>
		private void AdvanceCurrentFrame(int frame)
		{
			if (_decoder == null)
			{
				int finishedTick = ((_session.InitialTick != 0) ? _session.InitialTick : (_session.SessionConfig.RollbackWindow - 1));
				_decoder = new DeterministicTickInputDecoder(_serializer, _session, finishedTick);
			}
			int num = _sharedInputFrame;
			int num2 = 0;
			while (num < frame)
			{
				int num3 = _streamInputProvider.BeginReadFrame(frame);
				EnsureTempBufferSize(ref _buffer, num3);
				_streamInputProvider.CompleteReadFrame(frame, num3, ref _buffer);
				_stream.SetBuffer(_buffer, num3, 0);
				num = _stream.ReadInt();
				if (num == frame)
				{
					num2 = _stream.ReadUShort();
					_compressor.Unpack(_sharedInput, num2, _stream);
				}
			}
			if (num != _sharedInputFrame)
			{
				_rawInputCache.Enqueue(frame, _sharedInput);
				_sharedInputFrame = num;
				EnsureTempBufferSize(ref _buffer, _sharedInput.Length * 4);
				Buffer.BlockCopy(_sharedInput, 0, _buffer, 0, num2 * 8);
				_stream.SetBuffer(_buffer, num2 * 8);
				_stream.Reading = true;
				_decodeResult.Clear();
				DeterministicTickInputEncodeHeader deterministicTickInputEncodeHeader = _decoder.Decode(_stream, _decodeResult);
			}
			Assert.Always<int, int>(_sharedInputFrame == frame, "Input not found for frame {0}. Current frame is {1}.", frame, _sharedInputFrame);
		}

		/// <summary>
		/// Returns the int input set for a specific frame. This is used by the simulator and it will ask for frames in the past (before _sharedInputFrame).
		/// </summary>
		public void GetRawInput(DeterministicFrame frame, ref int[] data)
		{
			_rawInputCache.Dequeue(frame.Number, ref data);
		}

		public void OnInputPollingDone(int frame, int playerCount)
		{
		}
	}
}

