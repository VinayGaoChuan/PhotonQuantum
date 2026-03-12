using System;

namespace Photon.Deterministic
{
	/// <summary>
	/// The complete input set of all players for a single tick.
	/// </summary>
	[Serializable]
	public struct DeterministicTickInputSet
	{
		/// <summary>
		/// The input set is valid for this tick.
		/// </summary>
		public int Tick;

		/// <summary>
		/// On <see cref="T:Photon.Deterministic.DeterministicTickInput" /> per player.
		/// </summary>
		public DeterministicTickInput[] Inputs;
	}
}

