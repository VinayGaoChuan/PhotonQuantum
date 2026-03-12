namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// The client request to start a online simulation session.
	/// </summary>
	public class StartRequest : Message
	{
		/// <summary>
		/// The client id.
		/// </summary>
		public string Id;

		/// <summary>
		/// The Quantum protocol version of the client.
		/// </summary>
		public string ProtocolVersion;

		/// <summary>
		/// The initial tick to start from. Only used when having a local snapshot ready.
		/// </summary>
		public int InitialTick;

		/// <summary>
		/// The clients session config.
		/// </summary>
		public DeterministicSessionConfig SessionConfig;

		/// <summary>
		/// The clients runtime config.
		/// </summary>
		public byte[] RuntimeConfig;

		/// <summary>
		/// Returns <see langword="true" /> if the <see cref="F:Photon.Deterministic.Protocol.StartRequest.InitialTick" /> is different from 0.
		/// </summary>
		public bool HasLocalSnapshot => InitialTick > 0;

		/// <summary>
		/// The message serialization (writing and reading).
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">The bitstream to write to or read from</param>
		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref Id);
			stream.Serialize(ref ProtocolVersion);
			stream.Serialize(ref InitialTick);
			DeterministicSessionConfig.Serialize(serializer, stream, ref SessionConfig);
			stream.Serialize(ref RuntimeConfig);
		}

		/// <summary>
		/// Debug string with message content.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return string.Format(string.Format("[{0} {1}={2} {3}={4} {5}={6} {7}={8} {9}={10}]", "StartRequest", "Id", Id, "ProtocolVersion", ProtocolVersion, "InitialTick", InitialTick, "SessionConfig", SessionConfig, "RuntimeConfig", (RuntimeConfig == null) ? "null" : ((object)RuntimeConfig.Length)));
		}
	}
}

