namespace Photon.Deterministic
{
	/// <summary>
	/// Deterministic simulation statistic.
	/// </summary>
	public class DeterministicStats
	{
		/// <summary>
		/// The round trip time to the server in milliseconds.
		/// </summary>
		public int Ping { get; internal set; }

		/// <summary>
		/// The last predicted frame that was simulated.
		/// </summary>
		public int Frame { get; internal set; }

		/// <summary>
		/// The current input offset.
		/// </summary>
		public int Offset { get; internal set; }

		/// <summary>
		/// The current number of predicted frames.
		/// </summary>
		public int Predicted { get; internal set; }

		/// <summary>
		/// Not used anymore.
		/// </summary>
		public int ResimulatedFrames { get; internal set; }

		/// <summary>
		/// The total time of the last update.
		/// </summary>
		public double UpdateTime { get; internal set; }
	}
}

