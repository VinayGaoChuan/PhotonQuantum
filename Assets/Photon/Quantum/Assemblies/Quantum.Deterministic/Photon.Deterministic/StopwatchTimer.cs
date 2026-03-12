using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// Represents a high-resolution timer.
	/// </summary>
	public struct StopwatchTimer
	{
		private long _start;

		private long _elapsed;

		private byte _running;

		private static double _ticksFreq;

		/// <summary>
		/// Gets the elapsed time in ticks.
		/// </summary>
		public readonly long ElapsedInTicks
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				if (_running != 1)
				{
					return _elapsed;
				}
				return _elapsed + GetDelta();
			}
		}

		public readonly long ElapsedInDateTimeTicks
		{
			get
			{
				long delta = GetDelta();
				if (Stopwatch.IsHighResolution)
				{
					double num = delta;
					num *= _ticksFreq;
					return (long)num;
				}
				return delta;
			}
		}

		/// <summary>
		/// Gets the elapsed time in milliseconds.
		/// </summary>
		public readonly double ElapsedInMilliseconds
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return ElapsedInSeconds * 1000.0;
			}
		}

		/// <summary>
		/// Gets the elapsed time in seconds.
		/// </summary>
		public readonly double ElapsedInSeconds
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (double)ElapsedInTicks / (double)Stopwatch.Frequency;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the timer is running.
		/// </summary>
		public readonly bool IsRunning
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return _running == 1;
			}
		}

		[DllImport("kernel32.dll")]
		private static extern bool QueryPerformanceFrequency(out long value);

		static StopwatchTimer()
		{
			if (QueryPerformanceFrequency(out var value))
			{
				_ticksFreq = 10000000.0 / (double)value;
			}
		}

		/// <summary>
		/// Creates and starts a new timer.
		/// </summary>
		/// <returns>A new instance of the <see cref="T:Photon.Deterministic.StopwatchTimer" /> struct.</returns>
		public static StopwatchTimer StartNew()
		{
			StopwatchTimer result = default(StopwatchTimer);
			result.Start();
			return result;
		}

		/// <summary>
		/// Starts the timer if it is not already running.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Start()
		{
			if (_running == 0)
			{
				_start = Stopwatch.GetTimestamp();
				_running = 1;
			}
		}

		/// <summary>
		/// Stops the timer if it is running and updates the elapsed time.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Stop()
		{
			long delta = GetDelta();
			if (_running == 1)
			{
				_elapsed += delta;
				_running = 0;
				if (_elapsed < 0)
				{
					_elapsed = 0L;
				}
			}
		}

		/// <summary>
		/// Resets the timer to its initial state.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reset()
		{
			_elapsed = 0L;
			_running = 0;
			_start = 0L;
		}

		/// <summary>
		/// Restarts the timer, setting the elapsed time to zero and starting it.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Restart()
		{
			_elapsed = 0L;
			_running = 1;
			_start = Stopwatch.GetTimestamp();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private readonly long GetDelta()
		{
			return Stopwatch.GetTimestamp() - _start;
		}
	}
}

