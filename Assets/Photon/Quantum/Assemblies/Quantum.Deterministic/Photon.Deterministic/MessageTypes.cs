namespace Photon.Deterministic
{
	/// <summary>
	/// The Quantum message types.
	/// </summary>
	public static class MessageTypes
	{
		/// <summary>
		/// Protocol message.
		/// </summary>
		public const byte PROTOCOL = 100;

		/// <summary>
		/// Simulation message.
		/// </summary>
		public const byte SIMULATION = 101;

		/// <summary>
		/// Input message.
		/// </summary>
		public const byte INPUT = 102;

		/// <summary>
		/// Input delta message.
		/// </summary>
		public const byte INPUT_DELTA = 103;
	}
}

