namespace Photon.Deterministic
{
	/// <summary>
	/// The interface encapsulates managing the Quantum RPCs.
	/// </summary>
	public interface IDeterministicRpcProvider
	{
		/// <summary>
		/// Gets the RPC data for the given frame and player.
		/// </summary>
		/// <param name="frame">Frame number</param>
		/// <param name="player">Player</param>
		/// <returns>The RPC data and a bool representing if the RPC is a command.</returns>
		QTuple<byte[], bool> GetRpc(int frame, int player);

		/// <summary>
		/// Adds an RPC to the simulation.
		/// </summary>
		/// <param name="playerSlot">The local player slot</param>
		/// <param name="data">The RPC data</param>
		/// <param name="command">Is the RPC a deterministic command</param>
		void AddRpc(int playerSlot, byte[] data, bool command);
	}
}

