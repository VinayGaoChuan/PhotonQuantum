using System;

namespace Photon.Deterministic
{
	/// <summary>
	/// The Quantum game mode makes a distinction between multiplayer, local, and replay modes.
	/// </summary>
	public enum DeterministicGameMode
	{
		/// <summary>
		/// Multiplayer mode
		/// </summary>
		Multiplayer,
		/// <summary>
		/// Offline mode, only running a local simulation.
		/// </summary>
		Local,
		/// <summary>
		/// The replay mode is cost efficient that is used when all inputs are already known. 
		/// </summary>
		Replay,
		/// <summary>
		/// Obsolete mode
		/// </summary>
		[Obsolete("Use Multiplayer instead. The online mode always starts spectating until a player is added.")]
		Spectating
	}
}

