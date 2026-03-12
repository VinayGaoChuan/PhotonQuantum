using System;

namespace Photon.Deterministic
{
	/// <summary>
	/// Represents the state of a deterministic session.
	/// </summary>
	public enum DeterministicSessionState
	{
		/// <summary>
		/// Represents the idle state of a deterministic session.
		/// </summary>
		Idle,
		/// <summary>
		/// The session is joined.
		/// </summary>
		[Obsolete]
		Joined,
		/// <summary>
		/// The session is running.
		/// </summary>
		Running,
		/// <summary>
		/// The session is shut down.
		/// </summary>
		[Obsolete]
		Shutdown,
		/// <summary>
		/// The session is destroyed.
		/// </summary>
		Destroyed
	}
}

