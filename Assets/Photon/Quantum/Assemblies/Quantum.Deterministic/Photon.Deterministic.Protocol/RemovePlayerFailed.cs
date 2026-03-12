namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// The server reponds to <see cref="T:Photon.Deterministic.Protocol.RemovePlayer" /> with this message if the player could not be removed.
	/// </summary>
	public class RemovePlayerFailed : Message
	{
		/// <summary>
		/// Player slot failed to remove.
		/// </summary>
		public int PlayerSlot;

		/// <summary>
		/// Debug message.
		/// </summary>
		public string Message;

		/// <summary>
		/// The message serialization (writing and reading).
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">The bitstream to write to or read from</param>
		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref PlayerSlot);
			stream.Serialize(ref Message);
		}

		/// <summary>
		/// Debug string with message content.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return string.Format(string.Format("[{0} {1}={2} {3}={4}]", "RemovePlayerFailed", "PlayerSlot", PlayerSlot, typeof(Message), Message));
		}
	}
}

