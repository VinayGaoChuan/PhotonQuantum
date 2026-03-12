namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// Creates a Quantum command.
	/// </summary>
	public class Command : Message
	{
		/// <summary>
		/// Index of the local player slot this command is for.
		/// If this is a sever command it will represent the player.
		/// </summary>
		public int PlayerSlot;

		/// <summary>
		/// The tick candidate to match the local command prediction.
		/// The actual command can happen later.
		/// </summary>
		public int PredictedTick;

		/// <summary>
		/// Command data.
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
			stream.Serialize(ref PredictedTick);
			stream.Serialize(ref Data);
		}

		/// <summary>
		/// Debug string with message content.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return string.Format("[{0} {1}={2} {3}={4} {5}={6}]", "Command", "PlayerSlot", PlayerSlot, "PredictedTick", PredictedTick, "Data", (Data == null) ? "null" : ((object)Data.Length));
		}
	}
}

