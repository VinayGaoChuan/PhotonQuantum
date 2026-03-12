namespace Photon.Deterministic
{
	/// <summary>
	/// An internal interface to encapsulate managing rpc and input.
	/// </summary>
	public interface IDeterministicReplayProvider : IDeterministicRpcProvider, IDeterministicInputProvider
	{
		/// <summary>
		/// Setting the correct local actor number makes <see cref="M:Photon.Deterministic.DeterministicSession.IsLocalPlayer(Quantum.PlayerRef)" /> work in replays similar to normal Quantum sessions.
		/// </summary>
		int LocalActorNumber { get; }
	}
}

