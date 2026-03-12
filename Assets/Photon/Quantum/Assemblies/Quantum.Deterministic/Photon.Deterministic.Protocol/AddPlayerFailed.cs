namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// This message is sent from the server when the <see cref="T:Photon.Deterministic.Protocol.AddPlayer" /> request failed.
	/// </summary>
	public class AddPlayerFailed : Message
	{
		/// <summary>
		/// The player slot that was failed to be reserved.
		/// </summary>
		public int PlayerSlot;

		/// <summary>
		/// The debug message.
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
		/// Debug string of the message content.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return string.Format("[{0} {1}={2} {3}={4}]", "AddPlayerFailed", "PlayerSlot", PlayerSlot, "Message", Message);
		}
	}
}

