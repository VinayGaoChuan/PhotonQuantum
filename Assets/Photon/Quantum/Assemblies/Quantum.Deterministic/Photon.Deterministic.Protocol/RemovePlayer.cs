namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// The client requests to remove a player from the simulation.
	/// </summary>
	public class RemovePlayer : Message
	{
		/// <summary>
		/// Local player or -1 = All
		/// </summary>
		public int PlayerSlot;

		/// <summary>
		/// The message serialization (writing and reading).
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">The bitstream to write to or read from</param>
		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref PlayerSlot);
		}

		/// <summary>
		/// Debug string with message content.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return string.Format(string.Format("[{0} {1}={2}]", "RemovePlayer", "PlayerSlot", PlayerSlot));
		}
	}
}

