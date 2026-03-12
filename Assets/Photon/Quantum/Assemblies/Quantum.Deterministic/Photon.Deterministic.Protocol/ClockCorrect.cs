using System;

namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// Obsolete protocol message.
	/// </summary>
	[Obsolete]
	public class ClockCorrect : Message
	{
		public double ServerSeconds;

		public double ServerTimeScale;

		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref ServerSeconds);
			stream.Serialize(ref ServerTimeScale);
		}

		public override string ToString()
		{
			return string.Format("[{0} {1}={2} {3}={4}]", "ClockCorrect", "ServerSeconds", ServerSeconds, "ServerTimeScale", ServerTimeScale);
		}
	}
}

