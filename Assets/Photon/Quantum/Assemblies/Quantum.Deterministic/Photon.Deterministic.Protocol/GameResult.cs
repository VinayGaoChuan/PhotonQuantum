namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// The client sends this message to report the game result.
	/// </summary>
	public class GameResult : Message
	{
		/// <summary>
		/// The maximum size of the game result object.
		/// </summary>
		public const int MaxSize = 20480;

		/// <summary>
		/// Game result data 
		/// </summary>
		public byte[] Data;

		/// <summary>
		/// The message serialization (writing and reading).
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">The bitstream to write to or read from</param>
		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref Data);
		}
	}
}

