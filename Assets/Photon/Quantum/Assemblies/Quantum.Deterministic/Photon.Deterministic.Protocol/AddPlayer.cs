namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// This messages is send from the client to reserve a player slot.
	/// </summary>
	public class AddPlayer : Message
	{
		/// <summary>
		/// The player slot (0 when only one local player).
		/// </summary>
		public int PlayerSlot;

		/// <summary>
		/// Serialized runtime player data.
		/// </summary>
		public byte[] Data;

		/// <summary>
		/// The message serialization (writing and reading).
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">The bitstream to write to or read from</param>
		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref PlayerSlot);
			stream.Serialize(ref Data);
		}

		/// <summary>
		/// Debug string with message content.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return string.Format("[{0} {1}={2} {3}={4}]", "AddPlayer", "PlayerSlot", PlayerSlot, "Data", (Data == null) ? "null" : ((object)Data.Length));
		}
	}
}

