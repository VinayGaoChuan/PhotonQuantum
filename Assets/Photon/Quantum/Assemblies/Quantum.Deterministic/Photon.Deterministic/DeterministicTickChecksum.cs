using System;

namespace Photon.Deterministic
{
	/// <summary>
	/// An object that contains a tick and a checksum.
	/// </summary>
	[Serializable]
	public class DeterministicTickChecksum
	{
		/// <summary>
		/// The tick the checksum was recorded.
		/// </summary>
		public int Tick;

		/// <summary>
		/// The checksum.
		/// </summary>
		public ulong Checksum;
	}
}

