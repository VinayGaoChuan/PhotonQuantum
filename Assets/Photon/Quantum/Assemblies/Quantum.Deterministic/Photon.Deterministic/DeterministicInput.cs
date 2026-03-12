using System;

namespace Photon.Deterministic
{
	/// <summary>
	/// Obsolete type.
	/// </summary>
	[Obsolete]
	public struct DeterministicInput
	{
		/// <summary>
		/// Data
		/// </summary>
		public byte[] Data;

		/// <summary>
		/// Flags
		/// </summary>
		public DeterministicInputFlags Flags;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">Data</param>
		public DeterministicInput(byte[] data)
		{
			Data = data;
			Flags = (DeterministicInputFlags)0;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">Data</param>
		/// <param name="flags">Flags</param>
		public DeterministicInput(byte[] data, DeterministicInputFlags flags)
		{
			Data = data;
			Flags = flags;
		}
	}
}

