namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// This message contains the client game state checksum for a specific tick.
	/// </summary>
	public class TickChecksum : Message
	{
		/// <summary>
		/// The tick the checksum was recorded.
		/// </summary>
		public int Tick;

		/// <summary>
		/// The checksum.
		/// </summary>
		public ulong Checksum;

		/// <summary>
		/// The message serialization (writing and reading).
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">The bitstream to write to or read from</param>
		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref Tick);
			stream.Serialize(ref Checksum);
		}

		/// <summary>
		/// Debug string with message content.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return string.Format("[{0} {1}={2} {3}={4}]", "TickChecksum", "Tick", Tick, "Checksum", Checksum);
		}
	}
}

