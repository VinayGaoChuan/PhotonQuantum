using System;

namespace Photon.Deterministic.Protocol
{
	[Obsolete("This protocol message is not uses anymore. See the new start protocol in the Quantum 3 documentation.")]
	public class Join : Message
	{
		public string Id;

		public string ProtocolVersion;

		public int InitialTick;

		public int PlayerCount;

		public int PlayerSlots;

		public bool Rejoin;

		public bool HasLocalSnapshot => InitialTick > 0;

		public override void Serialize(Serializer serializer, BitStream stream)
		{
		}
	}
}

