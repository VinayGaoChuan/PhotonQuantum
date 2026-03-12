using System.Collections.Generic;
using Photon.Deterministic.Protocol;

namespace Photon.Deterministic
{
	/// <summary>
	/// A class that accumulates frame dumps from other clients during a checksum desync.
	/// </summary>
	public class ChecksumErrorFrameDump
	{
		/// <summary>
		/// Gets or sets the frame number.
		/// </summary>
		public int Frame;

		/// <summary>
		/// Gets or sets the list of tick checksum error frame dumps.
		/// </summary>
		public List<TickChecksumErrorFrameDump> Blocks = new List<TickChecksumErrorFrameDump>();
	}
}

