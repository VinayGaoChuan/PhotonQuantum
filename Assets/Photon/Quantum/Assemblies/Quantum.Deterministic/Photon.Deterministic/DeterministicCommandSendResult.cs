namespace Photon.Deterministic
{
	/// <summary>
	/// The result of sending a deterministic command.
	/// </summary>
	public enum DeterministicCommandSendResult
	{
		/// <summary>
		/// Success
		/// </summary>
		Success,
		/// <summary>
		/// Can't send commands when running in spectating mode.
		/// </summary>
		FailedIsSpectating,
		/// <summary>
		/// Command message is too big
		/// </summary>
		FailedTooBig,
		/// <summary>
		/// Simulation is not running
		/// </summary>
		FailedSimulationNotRunning
	}
}

