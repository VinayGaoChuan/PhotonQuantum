using System;
using System.Collections.Generic;
using Photon.Deterministic.Protocol;
using Quantum;

namespace Photon.Deterministic
{
	internal class DeterministicNetwork : IDeterministicNetwork, IDeterministicRpcProvider, IDeterministicDeltaCompressedInput
	{
		private struct NetworkEvent
		{
			public int OpCode;

			public byte[] Data;

			public int DataLength;

			public object EventObject;
		}

		private struct NetworkSend
		{
			public int Length;

			public int SendCount;
		}

		private int[] _sharedInput = new int[32768];

		private byte[] _sharedInputAsByteArray = new byte[131072];

		private int[] _sharedLocalInput = new int[64];

		private int[] _currentLocalInput = new int[64];

		private LinkedList<Command> _predictedCommands = new LinkedList<Command>();

		private int _localTick;

		private RawInputCache _rawInputCache = new RawInputCache();

		private ICommunicator _communicator;

		private int[] _serverActorNr = new int[1];

		private double _lastSeenServerTime;

		private int _lastInputPolledTick;

		private int _lastPredictedCommandTick;

		private List<DeterministicTickInput> _sendBuffer = new List<DeterministicTickInput>();

		private LinkedList<DeterministicTickInput[]> _staggerBuffer = new LinkedList<DeterministicTickInput[]>();

		private Queue<Message> _protocolMessageSendQueue = new Queue<Message>();

		private Queue<Message> _simulationMessageSendQueue = new Queue<Message>();

		private NetworkQueue<DeterministicTickInput> _inputReceiveQueue = new NetworkQueue<DeterministicTickInput>();

		private DeterministicSession _session;

		private Serializer _serializer;

		private Dispatcher<Message> _dispatcher;

		private BitStream _writeStream = new BitStream(new byte[102400]);

		private BitStream _readStream = new BitStream(new byte[0]);

		private const int INPUT_WRITE_STREAM_SIZE_BYTES = 16384;

		private BitStream _inputWriteStream = new BitStream(new byte[16384]);

		private BitStream _inputEncodeStream = new BitStream(new byte[16384]);

		private byte[] _inputWriteStreamTemp = new byte[16384];

		private RingBuffer<NetworkSend> _inputWriteOffsets = new RingBuffer<NetworkSend>(1024, overwrite: true);

		private DeterministicTickInput _inputWrite = new DeterministicTickInput();

		private List<DeterministicTickInput> _decodeResult;

		private DeterministicTickInputDecoder _decoder;

		private IDeltaCompressor _compressor = new DeltaCompressorDefault();

		private NetworkQueue<NetworkEvent> _eventQueue = new NetworkQueue<NetworkEvent>();

		public int RoundTripTime => _communicator.RoundTripTime;

		public int ActorNumber => _communicator.ActorNumber;

		public NetworkQueue<DeterministicTickInput> InputReceiveQueue => _inputReceiveQueue;

		public DeterministicNetwork(DeterministicSession session, ICommunicator communicator)
		{
			_session = session;
			_dispatcher = _session.CreateNetworkMessageDispatcher();
			_communicator = communicator;
			_decodeResult = new List<DeterministicTickInput>(256);
			_serializer = new Serializer();
			_serializer.ProtocolVersion = DeterministicProtocolVersionAttribute.Get(typeof(DeterministicNetwork).Assembly).Version;
			if (_communicator != null)
			{
				_communicator.AddEventListener(OnEventDataReceived);
			}
		}

		public void SendLocalRtt(int rtt)
		{
			_simulationMessageSendQueue.Enqueue(new RttUpdate
			{
				Rtt = rtt
			});
		}

		public void SendLocalChecksum(int tick, ulong hash)
		{
			_protocolMessageSendQueue.Enqueue(new TickChecksum
			{
				Tick = tick,
				Checksum = hash
			});
		}

		private void SendProtocolMessages()
		{
			while (_protocolMessageSendQueue.Count > 0)
			{
				if (_serializer.PackMessages(_writeStream, _protocolMessageSendQueue))
				{
					_communicator.RaiseEvent(100, _writeStream.Data, _writeStream.BytesRequired, reliable: true, _serverActorNr);
				}
			}
			while (_simulationMessageSendQueue.Count > 0)
			{
				if (_serializer.PackMessages(_writeStream, _simulationMessageSendQueue))
				{
					_communicator.RaiseEvent(101, _writeStream.Data, _writeStream.BytesRequired, reliable: false, _serverActorNr);
				}
			}
		}

		private void HandleProtocolEvent(NetworkEvent ev)
		{
			_readStream.SetBuffer(ev.Data, ev.DataLength);
			_readStream.Reading = true;
			Message msg;
			while (_serializer.ReadNext(_readStream, out msg))
			{
				_dispatcher.Dispatch(msg);
			}
		}

		public void Poll()
		{
			if (_communicator != null && !_communicator.IsConnected)
			{
				_session?.Game?.OnPluginDisconnect("Error #42: Communicator not connected");
				_session?.Destroy();
				return;
			}
			DequeueNetworkRecvQueue();
			if (_session.State != DeterministicSessionState.Destroyed)
			{
				SendProtocolMessages();
				_communicator.Service();
			}
		}

		public void ResetInputState(int tick)
		{
			if (_decoder == null)
			{
				_decoder = new DeterministicTickInputDecoder(_serializer, _session, tick);
			}
			else
			{
				_decoder.ResetInputState(tick);
			}
			_localTick = tick;
			_lastInputPolledTick = 0;
			_lastPredictedCommandTick = 0;
		}

		public void ResetInputState(DeterministicFrame frame)
		{
			ResetInputState(frame.Number);
			if (frame.Number != _session.RollbackWindow - 1)
			{
				LogStream logInfo = InternalLogStreams.LogInfo;
				if (logInfo != null)
				{
					logInfo.Log("Snapshot reset to tick " + frame.Number);
				}
				Array.Copy(frame.GetRawInputs(), _sharedInput, _sharedInput.Length);
			}
		}

		public void SendLocalInputs()
		{
			if (_inputWriteStream.BytesRequired <= 0)
			{
				return;
			}
			_communicator.RaiseEvent(102, _inputWriteStream.Data, _inputWriteStream.BytesRequired, reliable: false, _serverActorNr);
			if (_session.SessionConfig.InputRedundancy > 1)
			{
				int num = 0;
				for (int i = 0; i < _inputWriteOffsets.Count; i++)
				{
					NetworkSend value = _inputWriteOffsets[i];
					value.SendCount++;
					_inputWriteOffsets[i] = value;
					if (value.SendCount == _session.SessionConfig.InputRedundancy)
					{
						num++;
					}
				}
				if (num <= 0)
				{
					return;
				}
				int num2 = 0;
				while (num > 0)
				{
					num2 += _inputWriteOffsets.Pop().Length;
					num--;
				}
				if (num2 <= 0)
				{
					return;
				}
				if (num2 == _inputWriteStream.Position)
				{
					_inputWriteStream.Reset();
					return;
				}
				int position = _inputWriteStream.Position;
				int bytesRequired = _inputWriteStream.BytesRequired;
				Array.Copy(_inputWriteStream.Data, 0, _inputWriteStreamTemp, 0, bytesRequired);
				_inputWriteStream.Reset();
				int num3 = position - num2;
				int num4 = num2;
				if (num4 % 8 > 0)
				{
					int num5 = num4 % 8;
					int num6 = 8 - num4 % 8;
					_inputWriteStream.WriteByte((byte)(_inputWriteStreamTemp[num4 >> 3] >> num5), num6);
					num4 += num6;
					num3 -= num6;
				}
				while (num3 > 0)
				{
					int num7 = Math.Min(8, num3);
					_inputWriteStream.WriteByte(_inputWriteStreamTemp[num4 >> 3], num7);
					num4 += num7;
					num3 -= num7;
				}
			}
			else
			{
				_inputWriteStream.Reset();
			}
		}

		public void QueueLocalInput(DeterministicFrameInputTemp input, int playerSlot)
		{
			if (!DeterministicSessionConfigUtils.VerifyFixedSize(_session.SessionConfig, input.Data, input.DataLength))
			{
				LogStream logError = InternalLogStreams.LogError;
				if (logError != null)
				{
					logError.Log($"Fixed size input (size: {_session.SessionConfig.InputFixedSize}) is enabled but input size we got was {input.DataLength}");
				}
				return;
			}
			_inputWrite.PlayerIndex = playerSlot;
			_inputWrite.Flags = input.Flags;
			_inputWrite.Tick = input.Frame;
			_inputWrite.CopyToDataArray(input.Data, input.DataLength);
			_inputWrite.Rpc = input.Rpc;
			int position = _inputWriteStream.Position;
			_inputWriteStream.Writing = true;
			if (_session.SessionConfig.InputDeltaCompression)
			{
				DeltaCompressInput(ref _inputWrite);
			}
			else
			{
				_inputWrite.SimpleSerialize(_serializer, _inputWriteStream, _session.SessionConfig.InputFixedSize, ignoreRpc: true);
			}
			if (_session.SessionConfig.InputRedundancy > 1)
			{
				_inputWriteOffsets.Push(new NetworkSend
				{
					Length = _inputWriteStream.Position - position
				});
			}
		}

		private unsafe void DeltaCompressInput(ref DeterministicTickInput input)
		{
			if (_session.TryGetLocalPlayer(input.PlayerIndex, out var player))
			{
				DeterministicFrame frameVerified = _session.FrameVerified;
				int value = frameVerified.Number;
				_inputWriteStream.Writing = true;
				_inputWriteStream.Serialize(ref input.PlayerIndex, 8);
				_inputWriteStream.Serialize(ref input.Tick);
				_inputWriteStream.Serialize(ref value);
				_inputWriteStream.WriteByte((byte)input.Flags);
				_inputEncodeStream.Reset();
				_inputEncodeStream.Writing = true;
				input.SimpleSerialize(_serializer, _inputEncodeStream, _session.SessionConfig.InputFixedSize, ignoreRpc: true, includeHeader: false);
				Array.Clear(_currentLocalInput, 0, _currentLocalInput.Length);
				_inputEncodeStream.BlockCopyToArray(_currentLocalInput, 0);
				byte* rawInput = frameVerified.GetRawInput(player);
				Array.Clear(_sharedLocalInput, 0, _sharedLocalInput.Length);
				_session.Game.OnSerializedInput(rawInput, _sharedLocalInput);
				_inputEncodeStream.Reset();
				_compressor.Pack(_currentLocalInput, _sharedLocalInput, _currentLocalInput.Length, _inputEncodeStream);
				_inputWriteStream.SerializeAsByteArray(_inputEncodeStream);
			}
		}

		private void OnEventDataReceived(byte eventCode, byte[] data, int dataLength, object eventObject)
		{
			if (eventCode == 102 || eventCode == 100 || eventCode == 101 || eventCode == 103)
			{
				_eventQueue.Push(new NetworkEvent
				{
					OpCode = eventCode,
					Data = data,
					DataLength = dataLength,
					EventObject = eventObject
				});
			}
		}

		private void DequeueNetworkRecvQueue()
		{
			NetworkEvent ev;
			while (_eventQueue.TryPop(out ev))
			{
				switch (ev.OpCode)
				{
				case 100:
				case 101:
					try
					{
						HandleProtocolEvent(ev);
					}
					catch (Exception ex2)
					{
						LogStream logException2 = InternalLogStreams.LogException;
						if (logException2 != null)
						{
							logException2.Log(ex2);
						}
						_session?.Game?.OnPluginDisconnect("Error #40: Caught exception receiving protocol messages");
						_session?.Destroy();
						return;
					}
					break;
				case 102:
					HandleInputEvent(ev);
					break;
				case 103:
					try
					{
						HandleInputDeltaEvent(ev);
					}
					catch (Exception ex)
					{
						LogStream logException = InternalLogStreams.LogException;
						if (logException != null)
						{
							logException.Log(ex);
						}
						_session?.Game?.OnPluginDisconnect("Error #41: Input cache full");
						_session?.Destroy();
						return;
					}
					break;
				}
				if (ev.EventObject != null)
				{
					_communicator?.DisposeEventObject(ev.EventObject);
				}
			}
		}

		private void HandleInputDeltaEvent(NetworkEvent ev)
		{
			_readStream.SetBuffer(ev.Data, ev.DataLength);
			int num = _readStream.ReadInt();
			ushort num2 = _readStream.ReadUShort();
			if (!_session.IsPaused && _session.IsRunning && _localTick + 1 <= num && _localTick + 1 >= num)
			{
				_session.OnInputSetConfirmed(num, ev.DataLength, ev.Data);
				_compressor.Unpack(_sharedInput, num2, _readStream);
				_rawInputCache.Enqueue(num, _sharedInput);
				_localTick = num;
				Buffer.BlockCopy(_sharedInput, 0, _sharedInputAsByteArray, 0, _sharedInputAsByteArray.Length);
				_readStream.SetBuffer(_sharedInputAsByteArray, num2 * 4);
				_readStream.Reading = true;
				HandleInputEvent();
			}
		}

		private void HandleInputEvent(NetworkEvent ev)
		{
			_readStream.SetBuffer(ev.Data, ev.DataLength);
			_readStream.Reading = true;
			HandleInputEvent();
		}

		private void HandleInputEvent()
		{
			Assert.Always(_readStream.Reading, "Read stream must be in Reading mode");
			Assert.Always(_readStream.Data.Length != 0, "Read stream is empty");
			if (_decoder == null)
			{
				if (_session.SessionConfig == null)
				{
					return;
				}
				if (_session.InitialTick == 0)
				{
					_decoder = new DeterministicTickInputDecoder(_serializer, _session, _session.SessionConfig.RollbackWindow - 1);
				}
				else
				{
					_decoder = new DeterministicTickInputDecoder(_serializer, _session, _session.InitialTick);
				}
			}
			DeterministicTickInputEncodeHeader deterministicTickInputEncodeHeader = _decoder.Decode(_readStream, _decodeResult);
			if (deterministicTickInputEncodeHeader.ServerTime > _lastSeenServerTime)
			{
				_lastSeenServerTime = deterministicTickInputEncodeHeader.ServerTime;
				_session.OnProtocolClockCorrect(deterministicTickInputEncodeHeader.ServerTime, deterministicTickInputEncodeHeader.ServerTimeScale);
				_session.OnProtocolRttUpdate(deterministicTickInputEncodeHeader.MaxPing);
			}
			for (int i = 0; i < _decodeResult.Count; i++)
			{
				DeterministicTickInput deterministicTickInput = _decodeResult[i];
				if ((deterministicTickInput.Flags & DeterministicInputFlags.PlayerNotPresent) == DeterministicInputFlags.PlayerNotPresent)
				{
					deterministicTickInput.Flags |= DeterministicInputFlags.Repeatable;
				}
				_inputReceiveQueue.Push(deterministicTickInput);
			}
			_decodeResult.Clear();
		}

		public void Destroy()
		{
			_communicator?.OnDestroy();
			_communicator = null;
			_rawInputCache.Clear();
			_predictedCommands.Clear();
		}

		public void SendProtocolMessage(Message msg)
		{
			_protocolMessageSendQueue.Enqueue(msg);
		}

		public void SendSimulationMessage(Message msg)
		{
			_simulationMessageSendQueue.Enqueue(msg);
		}

		public QTuple<byte[], bool> GetRpc(int frame, int player)
		{
			_lastInputPolledTick = frame;
			byte[] array = null;
			LinkedListNode<Command> linkedListNode = _predictedCommands.First;
			while (linkedListNode != null)
			{
				LinkedListNode<Command> next = linkedListNode.Next;
				int predictedTick = linkedListNode.Value.PredictedTick;
				if (predictedTick < frame)
				{
					_predictedCommands.Remove(linkedListNode);
				}
				else if (predictedTick == frame)
				{
					if (_session.TryGetLocalPlayer(linkedListNode.Value.PlayerSlot, out var player2) && player == player2)
					{
						array = linkedListNode.Value.Data;
						break;
					}
				}
				else if (predictedTick > frame)
				{
					break;
				}
				linkedListNode = next;
			}
			if (array != null)
			{
				return new QTuple<byte[], bool>(array, item1: true);
			}
			return default(QTuple<byte[], bool>);
		}

		public void AddRpc(int playerSlot, byte[] data, bool command)
		{
			if (command)
			{
				int predictedTick = 0;
				if (_session?.FramePredicted != null)
				{
					predictedTick = (_lastPredictedCommandTick = Math.Max(Math.Max(_lastPredictedCommandTick, _lastInputPolledTick), _session.FramePredicted.Number) + 1);
				}
				Command command2 = new Command
				{
					PlayerSlot = playerSlot,
					PredictedTick = predictedTick,
					Data = data
				};
				_predictedCommands.AddLast(command2);
				SendProtocolMessage(command2);
			}
			else
			{
				SendProtocolMessage(new AddPlayer
				{
					PlayerSlot = playerSlot,
					Data = data
				});
			}
		}

		public void GetRawInput(DeterministicFrame frame, ref int[] data)
		{
			_rawInputCache.Dequeue(frame.Number, ref data);
		}

		public void OnInputPollingDone(int frame, int playerCount)
		{
		}
	}
}

