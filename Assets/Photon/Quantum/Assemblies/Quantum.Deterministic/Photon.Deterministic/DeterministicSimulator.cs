using System;
using System.Collections.Generic;
using Quantum;

namespace Photon.Deterministic
{
	internal class DeterministicSimulator
	{
		private class InputFrame
		{
			public InputFrame Next;

			public int Number;

			public InputSetMask Verified;

			public InputSetMask Predicted;

			public DeterministicFrameInput Input;

			public InputFrame(int playerCount, int inputSize, Native.Allocator allocator)
			{
				Input = new DeterministicFrameInput(playerCount, inputSize, allocator);
			}

			public void DeserializeCommands(DeterministicCommandSerializer serializer)
			{
				Input.DeserializeCommands(serializer);
			}

			public override string ToString()
			{
				return string.Format("{0}: {1}", "Number", Number);
			}
		}

		private DeterministicSession _session;

		private DeterministicFrame _stateVerified;

		private DeterministicFrame _statePredicted;

		private DeterministicFrame _statePredictedPrevious;

		private DeterministicFrame _statePreviousUpdatePredicted;

		private InputSetMask _playerMask;

		private InputSetMask _playerMaskLocal;

		private InputSetMask _playerMaskRemote;

		private InputFrame _inputFrameHead;

		private int _inputFrameCounter;

		private int _predictedFramesLastUpdate;

		private int _minFramesToVerify;

		private bool _isStalling;

		private Stack<InputFrame> _inputFramePool;

		private Dictionary<int, InputFrame> _inputFrameLookup;

		private double _deltaScale;

		private double _timeAccumulated;

		private bool _clockPaused;

		private List<int> _predictPlayers;

		private byte[][] _predictInput;

		private DeterministicInputFlags[] _predictFlags;

		public bool Paused
		{
			get
			{
				return _clockPaused;
			}
			set
			{
				_clockPaused = value;
			}
		}

		public int FramesRemaining => (int)(_timeAccumulated / DeltaTime);

		public int FrameCounter => _inputFrameCounter;

		public int FramesPredicted => _predictedFramesLastUpdate;

		public double Accumulated => _timeAccumulated;

		public double DeltaTime => 1.0 / (double)_session.SessionConfig.UpdateFPS;

		public bool IsStalling
		{
			get
			{
				return _isStalling;
			}
			internal set
			{
				_isStalling = value;
			}
		}

		/// <summary>
		/// Latest locally predicted copy of the game state.
		/// </summary>
		public DeterministicFrame FramePredicted => _statePredicted;

		/// <summary>
		/// Second latest locally predicted copy of the game state. Used for accurate visual interpolation of transforms (or any other data) between this and the Predicted frame.
		/// </summary>
		public DeterministicFrame FramePredictedPrevious => _statePredictedPrevious;

		/// <summary>
		/// Latest simulated copy of the Frame that was last-Predicted during the previous main session update. Used to calculate transform view error for smoothed correction.
		/// </summary>
		public DeterministicFrame FramePreviousUpdatePredicted => _statePreviousUpdatePredicted;

		public DeterministicFrame FrameVerified => _stateVerified;

		public DeterministicFrame GetVerifiedFrame(int tick)
		{
			return _session.Game.GetVerifiedFrame(tick);
		}

		internal byte[] GetExtraErrorFrameDumpData(DeterministicFrame frame)
		{
			return _session.Game.GetExtraErrorFrameDumpData(frame);
		}

		public void GetLocalConfigs(out DeterministicSessionConfig sessionConfig, out byte[] runtimeConfig)
		{
			_session.GetLocalConfigs(out sessionConfig, out runtimeConfig);
		}

		public DeterministicSimulator(DeterministicSession session)
		{
			_deltaScale = 1.0;
			_session = session;
		}

		public bool IsFrameVerified(int frame)
		{
			if (_inputFrameHead != null)
			{
				return frame <= _inputFrameHead.Number;
			}
			return false;
		}

		public void AccumulateTime(double dt)
		{
			_timeAccumulated += dt * _deltaScale;
		}

		public void AdjustClock(double dt)
		{
			_timeAccumulated += dt;
		}

		public bool StepFrameNumber(out int frame)
		{
			if (_timeAccumulated >= DeltaTime && !Paused && (!IsStalling || _stateVerified.Number + _minFramesToVerify > _inputFrameCounter))
			{
				_timeAccumulated -= DeltaTime;
				_inputFrameCounter++;
				frame = _inputFrameCounter;
				return true;
			}
			frame = -1;
			return false;
		}

		public bool StepFrameNumberDown(int count = 1)
		{
			if (_inputFrameCounter >= count)
			{
				_timeAccumulated += (double)count * DeltaTime;
				_inputFrameCounter -= count;
				return true;
			}
			return false;
		}

		public DeterministicFrame CreateStateFrame(IDisposable context, int number)
		{
			DeterministicFrame deterministicFrame = _session.Game.CreateFrame(context);
			deterministicFrame.Number = number;
			deterministicFrame.IsVerified = true;
			deterministicFrame.Init(_session.PlayerCount, _session.Game.GetInputInMemorySize(), _session.PlatformInfo.Allocator);
			return deterministicFrame;
		}

		public bool HasInput(int frame)
		{
			return _inputFrameLookup.ContainsKey(frame);
		}

		public void InsertInput(DeterministicFrameInputTemp input)
		{
			if (input.Frame <= _stateVerified.Number)
			{
				return;
			}
			InputFrame inputFrame = GetInputFrame(input.Frame);
			if (input.IsVerified)
			{
				if (inputFrame.Verified.Contains(input.Player))
				{
					return;
				}
				inputFrame.Verified.Add(input.Player);
			}
			else
			{
				if (inputFrame.Predicted.Contains(input.Player) || inputFrame.Verified.Contains(input.Player))
				{
					return;
				}
				inputFrame.Predicted.Add(input.Player);
			}
			inputFrame.Input.Assign(input, _session.Game);
		}

		public void CheckIsStalling(int maxVerifiedTicks)
		{
			int num = ((_stateVerified != null) ? _stateVerified.Number : 0);
			int num2 = _inputFrameCounter + (int)(_timeAccumulated / DeltaTime);
			_minFramesToVerify = 0;
			while (num < num2 && _minFramesToVerify < maxVerifiedTicks)
			{
				InputFrame inputFrame = GetInputFrame(num + 1);
				if (!(inputFrame.Verified.Intersects(_playerMask) == _playerMask))
				{
					break;
				}
				num++;
				_minFramesToVerify++;
			}
			int num3 = ((_stateVerified != null) ? _stateVerified.Number : 0) + _minFramesToVerify;
			if (_session.IsOnline)
			{
				_isStalling = num3 + _session.SessionConfig.RollbackWindow < num2;
			}
			else
			{
				_isStalling = _minFramesToVerify >= maxVerifiedTicks;
			}
		}

		private void CopyPreviousUpdatePredicted(DeterministicFrame fromState = null)
		{
			if (!_session.IsLockstep && _session.IsInterpolatable)
			{
				_statePreviousUpdatePredicted.CopyFrom(fromState ?? _statePredicted);
			}
		}

		public void Simulate(int maxVerifiedTicks)
		{
			_predictedFramesLastUpdate = 0;
			int number = _stateVerified.Number;
			bool flag = SimulateVerified(maxVerifiedTicks);
			if (_session.IsLockstep)
			{
				return;
			}
			int number2 = _statePredicted.Number;
			if (_statePredicted.Number - _stateVerified.Number >= _session.RollbackWindow)
			{
				return;
			}
			if (flag)
			{
				if (number2 == number)
				{
					CopyPreviousUpdatePredicted();
				}
				_statePredicted.CopyFrom(_stateVerified);
			}
			else
			{
				CopyPreviousUpdatePredicted();
			}
			int num = _statePredicted.Number;
			if (IsStalling)
			{
				return;
			}
			while (num < _inputFrameCounter)
			{
				num++;
				if (num - _stateVerified.Number < _session.RollbackWindow)
				{
					Simulate(GetInputFrame(num), _statePredicted, predicted: true);
					_predictedFramesLastUpdate++;
					if (num == number2)
					{
						CopyPreviousUpdatePredicted();
					}
					continue;
				}
				break;
			}
		}

		public void OnGameStart()
		{
			if (_session.IsPredicted)
			{
				_statePredicted.CopyFrom(_stateVerified);
			}
			if (_session.IsInterpolatable)
			{
				_statePredictedPrevious.CopyFrom(_stateVerified);
			}
			CopyPreviousUpdatePredicted();
			if (_session.GameMode == DeterministicGameMode.Local && _session.SessionConfig.InputDelayMin > 0)
			{
				_session.InitializeInputDelayMinForDeltaCompression(_stateVerified.Number);
			}
		}

		public void Initialize(IDisposable context)
		{
			_inputFrameLookup = new Dictionary<int, InputFrame>();
			_inputFramePool = new Stack<InputFrame>();
			int num = _session.SessionConfig.RollbackWindow - 1;
			_stateVerified = CreateStateFrame(context, num);
			_statePredicted = (_session.IsPredicted ? CreateStateFrame(context, num) : _stateVerified);
			_statePredictedPrevious = (_session.IsInterpolatable ? CreateStateFrame(context, num) : _stateVerified);
			_statePreviousUpdatePredicted = ((_session.IsPredicted && _session.IsInterpolatable) ? CreateStateFrame(context, num) : _stateVerified);
			for (int i = 0; i < _session.PlayerCount; i++)
			{
				_playerMask.Add(i);
			}
			UpdatePlayerMasks(_session.LocalPlayers);
			_predictInput = new byte[_session.PlayerCount][];
			_predictFlags = new DeterministicInputFlags[_session.PlayerCount];
			_predictPlayers = new List<int>();
			ResetInputFrameHead(num, resetInputFrame: true);
		}

		public void UpdatePlayerMasks(List<PlayerRef> players)
		{
			_playerMaskLocal = new InputSetMask();
			foreach (PlayerRef player in players)
			{
				_playerMaskLocal.Add(player);
			}
			_playerMaskRemote = _playerMaskLocal.Inverse().Intersects(_playerMask);
		}

		public void Reset(DeterministicFrame frame, bool resetInputFrame)
		{
			ResetStateFrame(_stateVerified, frame);
			if (!_stateVerified.TryResetPlayerMapping(_session.SessionConfig) && _session._network != null && _session._network.ActorNumber != 0)
			{
				_stateVerified.RestorePlayerMapping(_session);
			}
			ResetFrames(resetInputFrame);
		}

		public void Reset(byte[] frameData, int? frameNumber, bool resetInputFrame)
		{
			_stateVerified.Deserialize(frameData);
			if (frameNumber.HasValue)
			{
				_stateVerified.Number = frameNumber.Value;
			}
			if (!_stateVerified.TryResetPlayerMapping(_session.SessionConfig) && _session._network != null && _session._network.ActorNumber != 0)
			{
				_stateVerified.RestorePlayerMapping(_session);
			}
			ResetFrames(resetInputFrame);
		}

		private void ResetFrames(bool resetInputFrame)
		{
			if (_session.IsPredicted)
			{
				ResetStateFrame(_statePredicted, _stateVerified);
			}
			if (_session.IsInterpolatable)
			{
				ResetStateFrame(_statePredictedPrevious, _stateVerified);
			}
			ResetInputFrameHead(_stateVerified.Number, resetInputFrame);
			CopyPreviousUpdatePredicted();
		}

		private void Simulate(InputFrame f, DeterministicFrame state, bool predicted)
		{
			if (f.Number == _inputFrameCounter && _session.IsInterpolatable)
			{
				_statePredictedPrevious.CopyFrom(state);
			}
			if (predicted)
			{
				PredictInput(f);
			}
			f.DeserializeCommands(_session.CommandSerializer);
			ApplyInputToStateFrame(f, state, predicted);
			if (!predicted)
			{
				_session.OnDecodeDeltaCompressedInput(_stateVerified);
			}
			DeterministicSimulatorUtils.CallSimulate(state, _session.Game);
			DeterministicSimulatorUtils.CallSimulateFinished(state, _session.Game);
		}

		private void PredictInput(InputFrame f)
		{
			InputSetMask inputSetMask = f.Verified.Inverse().Intersects(_playerMaskRemote);
			if (inputSetMask == default(InputSetMask))
			{
				return;
			}
			Array.Clear(_predictInput, 0, _predictInput.Length);
			Array.Clear(_predictFlags, 0, _predictFlags.Length);
			_predictPlayers.Clear();
			for (int i = 0; i < _session.PlayerCount; i++)
			{
				if (inputSetMask.Contains(i))
				{
					_predictPlayers.Add(i);
				}
			}
			InputFrame inputFrame = _inputFrameHead;
			while (inputFrame != null && inputFrame != f)
			{
				int num = f.Number - inputFrame.Number;
				if (num <= _session.SessionConfig.InputRepeatMaxDistance && inputFrame.Verified.Intersects(inputSetMask) != default(InputSetMask))
				{
					for (int j = 0; j < _predictPlayers.Count; j++)
					{
						int num2 = _predictPlayers[j];
						if (inputFrame.Verified.Contains(num2) && (inputFrame.Input.Flags[num2] & DeterministicInputFlags.Repeatable) == DeterministicInputFlags.Repeatable)
						{
							f.Input.CopyForPrediction(inputFrame.Input, num2);
							f.Predicted.Add(num2);
						}
					}
				}
				inputFrame = inputFrame.Next;
			}
		}

		private void ResetInputFrameHead(int frame, bool resetInputFrame = false)
		{
			DestroyAllInputFrames();
			_inputFrameHead = AcquireInputFrame(frame);
			_inputFrameHead.Verified = _playerMask;
			_inputFrameHead.Predicted = _playerMask;
			_inputFrameLookup.Add(frame, _inputFrameHead);
			if (!resetInputFrame)
			{
				return;
			}
			_inputFrameCounter = frame;
			if (_session.GameMode == DeterministicGameMode.Local)
			{
				for (int i = 1; i <= _session.SessionConfig.InputDelayMin; i++)
				{
					InputFrame inputFrame = GetInputFrame(frame + i);
					inputFrame.Verified = _playerMask;
					inputFrame.Predicted = _playerMask;
				}
			}
		}

		private bool SimulateVerified(int maxTicks)
		{
			bool flag = false;
			int num = maxTicks;
			while (_inputFrameHead.Next != null && num > 0)
			{
				num--;
				InputFrame next = _inputFrameHead.Next;
				if (next.Number != _stateVerified.Number + 1 || !(next.Verified.Intersects(_playerMask) == _playerMask) || next.Number > _inputFrameCounter)
				{
					break;
				}
				flag = true;
				_minFramesToVerify--;
				InputFrame inputFrameHead = _inputFrameHead;
				inputFrameHead.Next = null;
				_inputFrameHead = next;
				ReleaseInputFrame(inputFrameHead);
				Simulate(_inputFrameHead, _stateVerified, predicted: false);
				if (_inputFrameHead.Number == _statePredicted.Number)
				{
					CopyPreviousUpdatePredicted(_stateVerified);
				}
				int checksumInterval = _session.SessionConfig.ChecksumInterval;
				if (checksumInterval > 0 && _inputFrameHead.Number % checksumInterval == 0)
				{
					ulong checksum = DeterministicSimulatorUtils.CallChecksum(_stateVerified);
					_session.OnSendLocalChecksum(_inputFrameHead.Number, checksum);
					DeterministicSimulatorUtils.CallChecksumComputed(_session.Game, _inputFrameHead.Number, checksum);
				}
			}
			if (flag && _session._timeProvider != null)
			{
				_session._timeProvider.OnVerifiedFrameReceived(_stateVerified.Number);
			}
			return flag;
		}

		private void ApplyInputToStateFrame(InputFrame inputFrame, DeterministicFrame state, bool predicted)
		{
			state.Number = inputFrame.Number;
			state.IsVerified = !predicted && inputFrame.Verified.Intersects(_playerMask) == _playerMask;
			state.CopyInputFrom(inputFrame.Input);
		}

		private void ResetStateFrame(DeterministicFrame target, DeterministicFrame source)
		{
			target.CopyFrom(source);
			target.IsVerified = true;
			target.Init(_session.PlayerCount, _session.Game.GetInputInMemorySize(), _session.PlatformInfo.Allocator, resetRawInputs: false);
		}

		private bool InputFrameListContains(InputFrame f)
		{
			for (InputFrame inputFrame = _inputFrameHead; inputFrame != null; inputFrame = inputFrame.Next)
			{
				if (inputFrame == f)
				{
					return true;
				}
			}
			return false;
		}

		private InputFrame GetInputFrame(int frame)
		{
			if (_inputFrameLookup.TryGetValue(frame, out var value))
			{
				return value;
			}
			value = AcquireInputFrame(frame);
			_inputFrameLookup.Add(frame, value);
			if (_inputFrameHead.Number > value.Number)
			{
				value.Next = _inputFrameHead;
				_inputFrameHead = value;
				return value;
			}
			for (InputFrame inputFrame = _inputFrameHead; inputFrame != null; inputFrame = inputFrame.Next)
			{
				if (inputFrame.Next == null)
				{
					inputFrame.Next = value;
					return value;
				}
				if (inputFrame.Next.Number > value.Number)
				{
					value.Next = inputFrame.Next;
					inputFrame.Next = value;
					return value;
				}
			}
			return null;
		}

		private InputFrame AcquireInputFrame(int frame)
		{
			InputFrame inputFrame = ((_inputFramePool.Count > 0) ? _inputFramePool.Pop() : new InputFrame(_session.PlayerCount, _session.Game.GetInputInMemorySize(), _session.PlatformInfo.Allocator));
			inputFrame.Number = frame;
			return inputFrame;
		}

		private void ReleaseInputFrame(InputFrame f)
		{
			_inputFrameLookup.Remove(f.Number);
			f.Next = null;
			f.Number = 0;
			f.Verified = default(InputSetMask);
			f.Predicted = default(InputSetMask);
			f.Input.Clear();
			_inputFramePool.Push(f);
		}

		private void DestroyAllInputFrames()
		{
			if (_inputFramePool != null)
			{
				while (_inputFramePool.Count > 0)
				{
					_inputFramePool.Pop().Input.Free(_session.PlatformInfo.Allocator);
				}
				_inputFramePool.Clear();
			}
			for (InputFrame inputFrame = _inputFrameHead; inputFrame != null; inputFrame = inputFrame.Next)
			{
				inputFrame.Input.Free(_session.PlatformInfo.Allocator);
			}
			_inputFrameHead = null;
			if (_inputFrameLookup == null)
			{
				return;
			}
			foreach (KeyValuePair<int, InputFrame> item in _inputFrameLookup)
			{
				item.Value.Input.Free(_session.PlatformInfo.Allocator);
			}
			_inputFrameLookup.Clear();
		}

		public void Destroy()
		{
			DestroyAllInputFrames();
			if (_stateVerified != null)
			{
				_stateVerified.Free();
			}
			if (_statePredicted != null && _stateVerified != _statePredicted)
			{
				_statePredicted.Free();
			}
			if (_statePredictedPrevious != null && _stateVerified != _statePredictedPrevious)
			{
				_statePredictedPrevious.Free();
			}
			if (_statePreviousUpdatePredicted != null && _stateVerified != _statePreviousUpdatePredicted)
			{
				_statePreviousUpdatePredicted.Free();
			}
		}

		public void ChecksumError(DeterministicTickChecksumError error)
		{
			DeterministicSimulatorUtils.CallChecksumError(_session.Game, error, new DeterministicFrame[1] { _stateVerified });
		}
	}
}

