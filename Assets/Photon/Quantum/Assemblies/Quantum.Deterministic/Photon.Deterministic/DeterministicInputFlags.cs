using System;

namespace Photon.Deterministic
{
	/// <summary>
	/// The DeterministicInputFlags are used by Quantum to:
	/// - detect whether a player is present , i.e.connected, to the simulation;
	/// - decide how to predict the next tick's input for a given player; and,
	/// - know whether the input on a verified frame was provided by a client or was replaced by the server.
	/// </summary>
	[Flags]
	public enum DeterministicInputFlags : byte
	{
		/// <summary>
		/// Tells both the server and other clients to copy this input data into the next tick 
		/// (on server when replacing input due to timeout, and on other clients for the local prediction algorithm). 
		/// This can be set by the developer from Unity when injecting player input and should be used on direct-control-like input such as movement. 
		/// It is not meant for command-like input (e.g. buy item).
		/// </summary>
		Repeatable = 1,
		/// <summary>
		/// No client connected for this player index.
		/// </summary>
		PlayerNotPresent = 2,
		/// <summary>
		/// The player index is controlled by a client, but the client did not send the input in
		/// time which resulted in the server repeating or replacing/zeroing out the input.
		/// </summary>
		ReplacedByServer = 4,
		/// <summary>
		/// This input has an additional deterministic command.
		/// </summary>
		Command = 8
	}
}

