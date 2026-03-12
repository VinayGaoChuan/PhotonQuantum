using System.Diagnostics;

namespace Photon.Deterministic
{
	public class DeltaClock
	{
		private Stopwatch _clock;

		private double _clockPrevious;

		public DeltaClock()
		{
			_clock = new Stopwatch();
		}

		public void Start()
		{
			_clock.Start();
		}

		public double GetDelta()
		{
			double totalSeconds = _clock.Elapsed.TotalSeconds;
			double num = totalSeconds - _clockPrevious;
			if (num < 0.0)
			{
				num = 0.0;
			}
			_clockPrevious = totalSeconds;
			return num;
		}
	}
}

