namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// This message is sent frequently from the client to the server to synchronize the distributed systems.
	/// </summary>
	public class RttUpdate : Message
	{
		/// <summary>
		/// The rtt in milliseconds.
		/// </summary>
		public int Rtt;

		/// <summary>
		/// The message serialization (writing and reading).
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">The bitstream to write to or read from</param>
		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref Rtt);
		}

		/// <summary>
		/// Debug string with message content.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return string.Format(string.Format("[{0} {1}={2}]", "RttUpdate", "Rtt", Rtt));
		}
	}
}

