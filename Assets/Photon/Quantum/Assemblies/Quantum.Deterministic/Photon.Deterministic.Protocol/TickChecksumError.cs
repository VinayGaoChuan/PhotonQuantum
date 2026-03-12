namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// The message is sent from the server to the client when a checksum error is detected.
	/// </summary>
	public class TickChecksumError : Message
	{
		/// <summary>
		/// The error details.
		/// </summary>
		public DeterministicTickChecksumError Error;

		/// <summary>
		/// The message serialization (writing and reading).
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">The bitstream to write to or read from</param>
		public override void Serialize(Serializer serializer, BitStream stream)
		{
			if (stream.Reading)
			{
				Error = new DeterministicTickChecksumError();
				Error.Checksums = new DeterministicChecksumResult[stream.ReadInt()];
			}
			else
			{
				stream.WriteInt(Error.Checksums.Length);
			}
			for (int i = 0; i < Error.Checksums.Length; i++)
			{
				stream.Serialize(ref Error.Checksums[i].Client);
				stream.Serialize(ref Error.Checksums[i].Checksum);
			}
			stream.Serialize(ref Error.Tick);
		}

		/// <summary>
		/// Debug string with message content.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return string.Format("[{0} {1}={2}]", "TickChecksumError", "Error", Error);
		}
	}
}

