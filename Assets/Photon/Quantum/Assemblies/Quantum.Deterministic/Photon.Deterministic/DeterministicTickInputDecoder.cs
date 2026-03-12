using System;
using System.Collections.Generic;
using System.Diagnostics;
using Photon.Deterministic.Protocol;

namespace Photon.Deterministic
{
	/// <summary>
	/// The decoder helps to deserialize the input from the server.
	/// </summary>
	public class DeterministicTickInputDecoder
	{
		private InputSetMask _playerMask;

		private int _playerCount;

		private int _finishedTick;

		private int _completedHighestTick;

		private Serializer _serializer;

		private Stopwatch _timer;

		private DeterministicSession _session;

		/// <summary>
		/// The finished tick, everything before is 100% done and cleared.
		/// </summary>
		public int FinishedTick => _finishedTick;

		/// <summary>
		/// Creates a input decoder.
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="session">Deterministic session</param>
		/// <param name="finishedTick">Finished tick</param>
		public DeterministicTickInputDecoder(Serializer serializer, DeterministicSession session, int finishedTick)
		{
			_session = session;
			_serializer = serializer;
			_finishedTick = finishedTick;
			_completedHighestTick = finishedTick;
			_timer = Stopwatch.StartNew();
		}

		/// <summary>
		/// Sets the finished tick.
		/// </summary>
		/// <param name="tick">Finished tick</param>
		public void ResetInputState(int tick)
		{
			_finishedTick = tick;
			_completedHighestTick = tick;
		}

		/// <summary>
		/// Decodes input from the stream.
		/// </summary>
		/// <param name="stream">Bitstream</param>
		/// <param name="result">Resulting input objects</param>
		/// <returns>Decoded input header</returns>
		public DeterministicTickInputEncodeHeader Decode(BitStream stream, List<DeterministicTickInput> result)
		{
			DeterministicTickInputEncodeHeader deterministicTickInputEncodeHeader = default(DeterministicTickInputEncodeHeader);
			deterministicTickInputEncodeHeader.Serialize(stream);
			if (_playerMask == default(InputSetMask))
			{
				_playerCount = deterministicTickInputEncodeHeader.PlayerCount;
				for (int i = 0; i < _playerCount; i++)
				{
					_playerMask.Add(i);
				}
			}
			while (stream.CanRead(32))
			{
				stream.RoundToByte();
				int num = stream.ReadInt();
				bool flag = stream.ReadBoolean();
				InputSetMask inputSetMask = default(InputSetMask);
				inputSetMask.Serialize(stream, deterministicTickInputEncodeHeader.PlayerCount);
				if (flag)
				{
					_completedHighestTick = Math.Max(_completedHighestTick, num);
				}
				for (int j = 0; j < deterministicTickInputEncodeHeader.PlayerCount; j++)
				{
					InputSetMask inputSetMask2 = new InputSetMask(j);
					if (inputSetMask.Intersects(inputSetMask2) == inputSetMask2)
					{
						DeterministicTickInput deterministicTickInput = _session.InputPool.Acquire();
						deterministicTickInput.Tick = num;
						deterministicTickInput.PlayerIndex = j;
						deterministicTickInput.SerializeForDecoder(_serializer, stream, deterministicTickInputEncodeHeader);
						result.Add(deterministicTickInput);
					}
				}
				int num2 = 0;
				for (int k = 0; k < deterministicTickInputEncodeHeader.PlayerCount; k++)
				{
					InputSetMask inputSetMask3 = new InputSetMask(k);
					if (inputSetMask.Intersects(inputSetMask3) == inputSetMask3)
					{
						DeterministicTickInput deterministicTickInput2 = result[num2++];
						deterministicTickInput2.SerializeForDecoderRPC(_serializer, stream, deterministicTickInputEncodeHeader);
					}
				}
			}
			return deterministicTickInputEncodeHeader;
		}

		private double GetTime()
		{
			return _timer.Elapsed.TotalSeconds;
		}
	}
}

