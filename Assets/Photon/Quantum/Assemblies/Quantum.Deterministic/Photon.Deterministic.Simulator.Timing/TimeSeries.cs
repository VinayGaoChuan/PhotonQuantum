using System;

namespace Photon.Deterministic.Simulator.Timing
{
	internal class TimeSeries
	{
		private double _mean;

		private double _varSum;

		private readonly RingBuffer<double> _samples;

		/// <summary>
		/// The number of available samples.
		/// </summary>
		public int Count => _samples.Count;

		/// <summary>
		/// The maximum number samples that can fit in this series.
		/// </summary>
		public int Capacity => _samples.Capacity;

		/// <summary>
		/// <see langword="true" /> if the series contains the maximum number of samples.
		/// </summary>
		public bool IsFull => _samples.IsFull;

		/// <summary>
		/// The most recent sample.
		/// </summary>
		public double Latest => _samples.Back();

		/// <summary>
		/// The arithmetic mean of the samples in the series.
		/// </summary>
		public double Avg => _mean;

		/// <summary>
		/// The variance of the samples in the series.
		/// </summary>
		public double Var
		{
			get
			{
				if (Count > 1 && _varSum >= 0.0)
				{
					return _varSum / (double)(Count - 1);
				}
				return 0.0;
			}
		}

		/// <summary>
		/// The standard deviation of the samples in the series.
		/// </summary>
		public double Dev
		{
			get
			{
				double var = Var;
				if (!(var >= double.Epsilon))
				{
					return 0.0;
				}
				return Math.Sqrt(var);
			}
		}

		/// <summary>
		/// The smallest value in the series.
		/// </summary>
		public double Min
		{
			get
			{
				double num = 0.0;
				foreach (double sample in _samples)
				{
					num = Math.Min(num, sample);
				}
				return num;
			}
		}

		/// <summary>
		/// The largest value in the series.
		/// </summary>
		public double Max
		{
			get
			{
				double num = 0.0;
				foreach (double sample in _samples)
				{
					num = Math.Max(num, sample);
				}
				return num;
			}
		}

		/// <summary>
		/// Returns the exponentially-weighted moving average of the series (with smoothing factor <c>alpha</c>).
		/// </summary>
		public double Smoothed(double alpha)
		{
			if (Count > 0)
			{
				double num = _samples[0];
				for (int i = 1; i < Count; i++)
				{
					num = (1.0 - alpha) * num + alpha * _samples[i];
				}
				return num;
			}
			return 0.0;
		}

		public TimeSeries(int capacity)
		{
			_mean = 0.0;
			_varSum = 0.0;
			_samples = new RingBuffer<double>(Math.Max(2, capacity));
		}

		/// <summary>
		/// Adds a new sample. If the series is full, the oldest sample will be removed.
		/// </summary>
		/// <param name="value"></param>
		public void Add(double value)
		{
			double mean = _mean;
			if (IsFull)
			{
				double num = _samples.PopFront();
				_samples.PushBack(value);
				double num2 = value - num;
				_mean += num2 / (double)Capacity;
				_varSum += num2 * (value - _mean + (num - mean));
			}
			else
			{
				_samples.PushBack(value);
				double num3 = value - mean;
				_mean += num3 / (double)Count;
				_varSum += num3 * (value - _mean);
			}
		}

		public double QuantileNormal(double p)
		{
			return Avg + InverseCdfNormal(p) * Dev;
		}

		public static double InverseCdfNormal(double p)
		{
			if (p < 0.5)
			{
				return 0.0 - Polynomial(p);
			}
			return Polynomial(1.0 - p);
			static double Polynomial(double x)
			{
				double num = Math.Sqrt(-2.0 * Math.Log(x));
				double num2 = (0.06114673576519699 * num + 1.5615337002120804) * num + 2.6539620026016846;
				double num3 = ((0.009547745327068945 * num + 0.4540555364442335) * num + 1.9048751828364987) * num + 1.0;
				return num - num2 / num3;
			}
		}

		/// <summary>
		/// Removes all samples and resets all values.
		/// </summary>
		public void Clear()
		{
			_mean = 0.0;
			_varSum = 0.0;
			_samples.Clear();
		}
	}
}

