using System;

namespace Photon.Deterministic.Protocol
{
	[Obsolete("Using StartRequest message instead")]
	public class GameConfigs : Message
	{
		public bool RequestedSessionConfig;

		public bool RequestedRuntimeConfig;

		public DeterministicSessionConfig SessionConfig;

		public byte[] RuntimeConfig;

		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref RequestedSessionConfig);
			stream.Serialize(ref RequestedRuntimeConfig);
			DeterministicSessionConfig.Serialize(serializer, stream, ref SessionConfig);
			stream.Serialize(ref RuntimeConfig);
		}

		public override string ToString()
		{
			return string.Format(string.Format("[{0} {1}={2} {3}={4} {5}={6} {7}={8}]", "GameConfigs", "RequestedSessionConfig", RequestedSessionConfig, "RequestedSessionConfig", RequestedSessionConfig, "SessionConfig", SessionConfig, "RuntimeConfig", (RuntimeConfig == null) ? "null" : ((object)RuntimeConfig.Length)));
		}
	}
}

