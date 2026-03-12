using System;

namespace Photon.Deterministic.Protocol
{
	[Obsolete("Using StartRequest message instead")]
	public class SessionConfig : Message
	{
		public bool Requested;

		public DeterministicSessionConfig Config;

		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref Requested);
			DeterministicSessionConfig.Serialize(serializer, stream, ref Config);
		}

		public override string ToString()
		{
			return $"[SessionConfig Requested={Requested} Config={Config}]";
		}
	}
}

