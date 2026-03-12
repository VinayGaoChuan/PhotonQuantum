using System;
using System.Collections.Generic;
using Photon.Deterministic.Protocol;
using Quantum;

namespace Photon.Deterministic
{
	internal class LocalInputProvider : IDeterministicInputProvider, IDeterministicRpcProvider, IDeterministicDeltaCompressedInput
	{
		private IDeterministicGame _game;

		private Dictionary<int, Queue<QTuple<byte[], bool>>> _rpcQueues;

		private InputSetMask _localPlayerMask;

		private Serializer _serializer = new Serializer();

		private BitStream _writeStream = new BitStream(new byte[49152]);

		private DeterministicTickInput[] _input;

		private DeterministicDeltaCompression _deltaCompression;

		private DeterministicSession _session;

		private RawInputCache _rawInputCache = new RawInputCache();

		private DeterministicTickInput[] Input
		{
			get
			{
				if (_input == null)
				{
					_input = new DeterministicTickInput[_session.PlayerCount];
					for (int i = 0; i < _session.PlayerCount; i++)
					{
						_input[i] = new DeterministicTickInput();
					}
				}
				return _input;
			}
		}

		public LocalInputProvider(IDeterministicGame game, DeterministicSession session)
		{
			_game = game;
			_rpcQueues = new Dictionary<int, Queue<QTuple<byte[], bool>>>();
			_deltaCompression = new DeterministicDeltaCompression();
			_session = session;
		}

		public bool CanSimulate(int frame)
		{
			return true;
		}

		public void ResetInputState(DeterministicFrame frame)
		{
			Array.Copy(frame.GetRawInputs(), _deltaCompression.CurrentRawInput, _deltaCompression.CurrentRawInput.Length);
		}

		/// <summary>
		/// This method does a bit more than just provide the raw input to the frame.
		/// It will use the timing to call OnInputSetConfirmed() to be able to record a DC replay in local game mode.
		/// </summary>
		public void GetRawInput(DeterministicFrame frame, ref int[] data)
		{
			_rawInputCache.Dequeue(frame.Number, ref data);
		}

		private byte[] ComputeDelta(int frame, int playerCount, DeterministicSession session, DeterministicSessionConfig sessionConfig)
		{
			_writeStream.ResetFast(_writeStream.Capacity);
			_writeStream.Writing = true;
			DeterministicTickInputEncodeHeader header = default(DeterministicTickInputEncodeHeader);
			header.MaxPing = 0;
			header.PlayerCount = playerCount;
			header.InputFixedSize = sessionConfig.InputFixedSize;
			header.Serialize(_writeStream);
			_writeStream.RoundToByte();
			_writeStream.WriteInt(frame);
			_writeStream.WriteBool(value: true);
			_writeStream.WriteULong(ulong.MaxValue, playerCount);
			for (int i = 0; i < Input.Length; i++)
			{
				DeterministicTickInput deterministicTickInput = Input[i];
				deterministicTickInput.SerializeForDecoder(_serializer, _writeStream, header);
			}
			int rawInputLength = _writeStream.Position / 8;
			for (int j = 0; j < Input.Length; j++)
			{
				DeterministicTickInput deterministicTickInput2 = Input[j];
				deterministicTickInput2.SerializeForDecoderRPC(_serializer, _writeStream, header);
			}
			_writeStream.RoundToByte();
			_writeStream.RoundTo4Bytes();
			return _deltaCompression.ComputeDelta(_writeStream.Data, 0, _writeStream.Position / 8, frame, rawInputLength);
		}

		internal void OnRemoveLocalPlayer(int playerSlot)
		{
			if (!_localPlayerMask.Contains(playerSlot))
			{
				LogStream logWarn = InternalLogStreams.LogWarn;
				if (logWarn != null)
				{
					logWarn.Log($"Player does not have player slot '{playerSlot}'");
				}
			}
			else
			{
				_localPlayerMask.Remove(playerSlot);
			}
		}

		/// <summary>
		/// This timing is required, because we need to cache the input for this frame to be used after the simulation (GetRawInput())
		/// Also, the OnInputSetConfirmed callback needs to be called to be able to recorded DC input replays from a local game.
		/// </summary>
		public void OnInputPollingDone(int frame, int playerCount)
		{
			if (_session.SessionConfig.InputDeltaCompression)
			{
				byte[] array = ComputeDelta(frame, playerCount, _session, _session.SessionConfig);
				_session.OnInputSetConfirmed(frame, array.Length, array);
				_rawInputCache.Enqueue(frame, _deltaCompression.CurrentRawInput);
			}
		}

		/// <summary>
		/// Input is polled from all local players, we'll save the polled input internally as well to be able to OnInputSetConfirmed (delta compressed replays).
		/// </summary>
		public DeterministicFrameInputTemp GetInput(int frame, int playerSlot)
		{
			DeterministicFrameInputTemp inputInternal = GetInputInternal(frame, playerSlot);
			Input[playerSlot].Tick = inputInternal.Frame;
			Input[playerSlot].PlayerIndex = inputInternal.Player;
			Input[playerSlot].DataArray = inputInternal.CloneData();
			Input[playerSlot].DataLength = inputInternal.DataLength;
			Input[playerSlot].Flags |= inputInternal.Flags;
			return inputInternal;
		}

		private DeterministicFrameInputTemp GetInputInternal(int frame, int playerSlot)
		{
			if (!_localPlayerMask.Contains(playerSlot))
			{
				return new DeterministicFrameInputTemp
				{
					Frame = frame,
					Player = playerSlot,
					IsVerified = true,
					Flags = DeterministicInputFlags.PlayerNotPresent
				};
			}
			DeterministicFrameInputTemp result = _game.OnLocalInput(frame, playerSlot);
			result.IsVerified = true;
			return result;
		}

		public QTuple<byte[], bool> GetRpc(int frame, int playerSlot)
		{
			Queue<QTuple<byte[], bool>> rpcQueue = GetRpcQueue(playerSlot);
			Input[playerSlot].Rpc = null;
			Input[playerSlot].Flags = (DeterministicInputFlags)0;
			if (rpcQueue.Count > 0)
			{
				QTuple<byte[], bool> result = rpcQueue.Dequeue();
				Input[playerSlot].Rpc = result.Item0;
				if (result.Item0 != null && result.Item1)
				{
					Input[playerSlot].Flags = DeterministicInputFlags.Command;
				}
				return result;
			}
			return default(QTuple<byte[], bool>);
		}

		public void AddRpc(int playerSlot, byte[] data, bool command)
		{
			if (!command)
			{
				if (_localPlayerMask.Contains(playerSlot))
				{
					LogStream logWarn = InternalLogStreams.LogWarn;
					if (logWarn != null)
					{
						logWarn.Log($"Player already has player slot '{playerSlot}'");
					}
					return;
				}
				_localPlayerMask.Add(playerSlot);
				data = ByteUtils.MergeByteBlocks(BitConverter.GetBytes(0), BitConverter.GetBytes(playerSlot), data);
			}
			GetRpcQueue(playerSlot).Enqueue(QTuple.Create(data, command));
		}

		private Queue<QTuple<byte[], bool>> GetRpcQueue(int playerSlot)
		{
			if (!_rpcQueues.TryGetValue(playerSlot, out var value))
			{
				_rpcQueues.Add(playerSlot, value = new Queue<QTuple<byte[], bool>>());
			}
			return value;
		}
	}
}

