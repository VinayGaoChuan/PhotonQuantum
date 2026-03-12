namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// The simulation stopped message. Is not used yet.
	/// </summary>
	public class SimulationStop : Message
	{
		/// <summary>
		/// Last tick of the simulation.
		/// </summary>
		public int FinalFrame;

		/// <summary>
		/// The message serialization (writing and reading).
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">The bitstream to write to or read from</param>
		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref FinalFrame);
		}

		/// <summary>
		/// Debug string with message content.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return string.Format(string.Format("[{0} {1}={2}]", "SimulationStop", "FinalFrame", FinalFrame));
		}
	}
}

