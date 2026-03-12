namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// The server sends the start signal.
	/// </summary>
	public class SimulationStart : Message
	{
		/// <summary>
		/// Unused.
		/// </summary>
		public bool Reconnect;

		/// <summary>
		/// The server time in seconds.
		/// </summary>
		public double ServerTime;

		/// <summary>
		/// The client is flagged for waiting for a snapshot.
		/// </summary>
		public bool WaitingForSnapshot;

		/// <summary>
		/// The runtime config to use.
		/// </summary>
		public byte[] RuntimeConfig;

		/// <summary>
		/// The session config to use.
		/// </summary>
		public DeterministicSessionConfig SessionConfig;

		/// <summary>
		/// The message serialization (writing and reading).
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">The bitstream to write to or read from</param>
		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref Reconnect);
			stream.Serialize(ref ServerTime);
			stream.Serialize(ref WaitingForSnapshot);
			stream.Serialize(ref RuntimeConfig);
			DeterministicSessionConfig.Serialize(serializer, stream, ref SessionConfig);
		}

		/// <summary>
		/// Debug string with message content.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return string.Format(string.Format("[{0} {1}={2} {3}={4} {5}={6} {7}={8}]", "SimulationStart", "WaitingForSnapshot", WaitingForSnapshot, "ServerTime", ServerTime, "SessionConfig", SessionConfig, "RuntimeConfig", (RuntimeConfig == null) ? "null" : ((object)RuntimeConfig.Length)));
		}
	}
}

