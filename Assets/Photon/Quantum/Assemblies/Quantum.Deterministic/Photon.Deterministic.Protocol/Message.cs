namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// The base class for Quantum network messages.
	/// </summary>
	public abstract class Message
	{
		internal Message Clone()
		{
			return (Message)MemberwiseClone();
		}

		/// <summary>
		/// The message serialization (writing and reading).
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">The bitstream to write to or read from</param>
		public abstract void Serialize(Serializer serializer, BitStream stream);
	}
}

