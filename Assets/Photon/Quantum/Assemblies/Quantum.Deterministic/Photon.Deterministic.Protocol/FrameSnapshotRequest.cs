namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// The server request a frame snapshot from the client.
	/// </summary>
	public class FrameSnapshotRequest : Message
	{
		/// <summary>
		/// The tick the snapshot is requested for.
		/// </summary>
		public int ReferenceTick;

		/// <summary>
		/// The message serialization (writing and reading).
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">The bitstream to write to or read from</param>
		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref ReferenceTick);
		}

		/// <summary>
		/// Debug string with message content.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return $"[FrameSnapshotRequest ReferenceTick={ReferenceTick}]";
		}
	}
}

