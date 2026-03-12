using System;

namespace Photon.Deterministic.Protocol
{
	[Obsolete("Using StartRequest message instead")]
	public class RuntimeConfig : Message
	{
		public bool Requested;

		public byte[] Config;

		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref Requested);
			stream.Serialize(ref Config);
		}

		public override string ToString()
		{
			return string.Format(string.Format("[{0} {1}={2} {3}={4}]", "RuntimeConfig", "Requested", Requested, "Config", (Config == null) ? "null" : ((object)Config.Length)));
		}
	}
}

