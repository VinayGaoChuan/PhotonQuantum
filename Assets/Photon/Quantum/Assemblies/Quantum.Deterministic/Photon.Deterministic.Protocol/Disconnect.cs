namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// Is sent from the server to gracefully disconnect a client.
	/// </summary>
	public class Disconnect : Message
	{
		/// <summary>
		/// Disconnect reason debug string.
		/// </summary>
		public string Reason;

		/// <summary>
		/// The message serialization (writing and reading).
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">The bitstream to write to or read from</param>
		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref Reason);
		}

		/// <summary>
		/// Debug string with message content.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return "[Disconnect Reason=" + Reason + "]";
		}
	}
}

