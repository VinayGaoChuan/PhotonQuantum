using System;

namespace Photon.Deterministic
{
	/// <summary>
	/// The data checksum is used to verify the integrity of the simulation.
	/// </summary>
	[Serializable]
	public struct DeterministicChecksumResult
	{
		/// <summary>
		/// The Photon Actor Id of the client that performed the checksum.
		/// </summary>
		public byte Client;

		/// <summary>
		/// The checksum result.
		/// </summary>
		public ulong Checksum;
	}
}

