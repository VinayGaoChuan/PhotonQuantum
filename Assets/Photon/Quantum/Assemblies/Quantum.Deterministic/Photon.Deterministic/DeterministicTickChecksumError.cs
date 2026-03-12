using System;

namespace Photon.Deterministic
{
	/// <summary>
	/// A collection of checksums when an error was recognized.
	/// </summary>
	[Serializable]
	public class DeterministicTickChecksumError
	{
		/// <summary>
		/// The tick the error was detected.
		/// </summary>
		public int Tick;

		/// <summary>
		/// The checksums details for all clients.
		/// </summary>
		public DeterministicChecksumResult[] Checksums;
	}
}

