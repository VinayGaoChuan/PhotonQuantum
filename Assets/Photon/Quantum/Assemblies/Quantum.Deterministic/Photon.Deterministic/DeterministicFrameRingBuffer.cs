using System;

namespace Photon.Deterministic
{
	/// <summary>
	/// The frame ring buffer is a utility collection class that stores a fixed number of latest frames in a ring buffer, usually in a certain time interval.
	/// A use-case for example are instant replays which allow to create a new simulation in the past time of the main simulation.
	/// </summary>
	public class DeterministicFrameRingBuffer : IDisposable
	{
		private readonly DeterministicFrame[] _data;

		private int _tail;

		/// <summary>
		/// Returns the capacity of the ring buffer.
		/// </summary>
		public int Capacity { get; }

		/// <summary>
		/// Returns the current count of frames in the ring buffer.
		/// </summary>
		public int Count { get; private set; }

		/// <summary>
		/// Returns the internal data array.
		/// </summary>
		public DeterministicFrame[] Data => _data;

		private int Head
		{
			get
			{
				if (Count != Capacity)
				{
					return 0;
				}
				return _tail;
			}
		}

		/// <summary>
		/// Access a frame by index. 
		/// The index is relative to the head of the ring buffer, so index 0 is always the older frame stored.
		/// </summary>
		/// <param name="index">Index of the current saves frames.</param>
		/// <returns>Frame object</returns>
		/// <exception cref="T:System.ArgumentOutOfRangeException">Is raised when the desired index value is less than 0 or out of bounds <see cref="P:Photon.Deterministic.DeterministicFrameRingBuffer.Count" /></exception>
		public DeterministicFrame this[int index]
		{
			get
			{
				if (index < 0 || index >= Count)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				int num = (Head + index) % Capacity;
				return _data[num];
			}
		}

		/// <summary>
		/// Create the frame ring buffer with a fixed capacity.
		/// </summary>
		/// <param name="capacity">Ring buffer size</param>
		/// <exception cref="T:System.ArgumentException">Is raised when the capacity is smaller than 1</exception>
		public DeterministicFrameRingBuffer(int capacity)
		{
			if (capacity <= 0)
			{
				throw new ArgumentException("Capacity has to be 1 or greater.", "capacity");
			}
			Capacity = capacity;
			_data = new DeterministicFrame[capacity];
		}

		/// <summary>
		/// Clear all frames in the ring buffer.
		/// </summary>
		public void Clear()
		{
			for (int i = 0; i < _data.Length; i++)
			{
				_data[i]?.Free();
				_data[i] = null;
			}
			_tail = 0;
			Count = 0;
		}

		/// <summary>
		/// Dispose the collection.
		/// </summary>
		void IDisposable.Dispose()
		{
			Clear();
		}

		/// <summary>
		/// Find a frame that matches the desired frame number.
		/// </summary>
		/// <param name="frameNumber">The desired frame number</param>
		/// <param name="findMode">The find mode</param>
		/// <returns>The best frame relative to the desired frame number or null</returns>
		public DeterministicFrame Find(int frameNumber, DeterministicFrameSnapshotBufferFindMode findMode = DeterministicFrameSnapshotBufferFindMode.Equal)
		{
			DeterministicFrame deterministicFrame = null;
			DeterministicFrame[] data = _data;
			foreach (DeterministicFrame deterministicFrame2 in data)
			{
				if (deterministicFrame2 == null)
				{
					continue;
				}
				if (deterministicFrame2.Number == frameNumber)
				{
					return deterministicFrame2;
				}
				switch (findMode)
				{
				case DeterministicFrameSnapshotBufferFindMode.ClosestLessThanOrEqual:
					if (deterministicFrame2.Number < frameNumber && !(deterministicFrame?.Number >= deterministicFrame2.Number))
					{
						deterministicFrame = deterministicFrame2;
					}
					break;
				case DeterministicFrameSnapshotBufferFindMode.Closest:
				{
					if (deterministicFrame == null)
					{
						deterministicFrame = deterministicFrame2;
						break;
					}
					int num = Math.Abs(deterministicFrame.Number - frameNumber);
					int num2 = Math.Abs(deterministicFrame2.Number - frameNumber);
					if (num2 < num)
					{
						deterministicFrame = deterministicFrame2;
					}
					break;
				}
				}
			}
			return deterministicFrame;
		}

		/// <summary>
		/// Add a frame to the collection, the frame object is copied.
		/// If the collection is full the oldest frame will be overwritten.
		/// </summary>
		/// <param name="frame">The frame to save</param>
		/// <param name="game">Game object</param>
		/// <param name="context">Context object</param>
		public void PushBack(DeterministicFrame frame, IDeterministicGame game, IDisposable context)
		{
			DeterministicFrame deterministicFrame = _data[_tail];
			if (deterministicFrame == null)
			{
				deterministicFrame = (_data[_tail] = game.CreateFrame(context));
				deterministicFrame.IsVerified = true;
			}
			deterministicFrame.CopyFrom(frame);
			bool flag = Count == Capacity;
			_tail = Increment(_tail, Capacity);
			if (!flag)
			{
				int count = Count + 1;
				Count = count;
			}
		}

		/// <summary>
		/// Add a frame to the collection, the frame object is copied.
		/// If the collection is full the oldest frame will be overwritten.
		/// </summary>
		/// <param name="frame">The frame to save</param>
		/// <param name="createFrame">A callback to create a copy of the frame</param>
		public void PushBack(DeterministicFrame frame, Func<DeterministicFrame> createFrame)
		{
			DeterministicFrame deterministicFrame = _data[_tail];
			if (deterministicFrame == null)
			{
				deterministicFrame = (_data[_tail] = createFrame());
				deterministicFrame.IsVerified = true;
			}
			deterministicFrame.CopyFrom(frame);
			bool flag = Count == Capacity;
			_tail = Increment(_tail, Capacity);
			if (!flag)
			{
				int count = Count + 1;
				Count = count;
			}
		}

		/// <summary>
		/// Return the frame at the head of the ring buffer.
		/// </summary>
		/// <param name="offset">The offset relative to the head</param>
		/// <returns>Frame object at the position</returns>
		/// <exception cref="T:System.InvalidOperationException">Is raised when the collection is empty</exception>
		/// <exception cref="T:System.IndexOutOfRangeException">Is raised when the offset is invalid or out of bounds</exception>
		public DeterministicFrame PeekBack(int offset = 0)
		{
			if (Count == 0)
			{
				throw new InvalidOperationException("The buffer is empty");
			}
			if (offset < 0 || offset >= Count)
			{
				throw new IndexOutOfRangeException("offset");
			}
			int num = _tail - 1 - offset;
			if (num < 0)
			{
				num += Capacity;
			}
			return _data[num];
		}

		private static int Increment(int i, int capacity, int count = 1)
		{
			return (i + count) % capacity;
		}

		/// <summary>
		///  This method checks if there is a common sampling pattern between two windows of frames by comparing their sampling rates and window sizes.
		///  If a common pattern is found, it returns <see langword="true" /> and provides the common window size and sampling rate through the output parameters. 
		///  Otherwise, it returns <see langword="false" /> and sets the output parameters to 0.
		/// </summary>
		/// <param name="windowA">Time window a in ticks</param>
		/// <param name="samplingA">Sampling interval a</param>
		/// <param name="windowB">Time window b in ticks</param>
		/// <param name="samplingB">Sampling interval b</param>
		/// <param name="commonWindow">Common time window</param>
		/// <param name="commonSampling">Common sample rate</param>
		/// <returns><see langword="true" /> if a common pattern was found</returns>
		public static bool TryGetCommonSamplingPattern(int windowA, int samplingA, int windowB, int samplingB, out int commonWindow, out int commonSampling)
		{
			if (samplingA <= 0)
			{
				commonWindow = (commonSampling = 0);
				return false;
			}
			if (samplingB <= 0)
			{
				commonWindow = (commonSampling = 0);
				return false;
			}
			if (windowA <= 0 || windowB <= 0)
			{
				commonWindow = (commonSampling = 0);
				return false;
			}
			if (samplingA % samplingB == 0 && windowB >= windowA)
			{
				commonWindow = windowB;
				commonSampling = samplingB;
				return true;
			}
			if (samplingB % samplingA == 0 && windowA >= windowB)
			{
				commonWindow = windowA;
				commonSampling = samplingA;
				return true;
			}
			commonWindow = (commonSampling = 0);
			return false;
		}

		/// <summary>
		/// Calculate the required size based for the ring buffer on the time window and sampling rate.
		/// </summary>
		/// <param name="window">Time window in ticks</param>
		/// <param name="samplingRate">Sampling interval</param>
		/// <returns></returns>
		public static int GetSize(int window, int samplingRate)
		{
			if (samplingRate > 0)
			{
				return 1 + window / samplingRate;
			}
			return 0;
		}
	}
}

