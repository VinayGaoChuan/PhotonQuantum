namespace Photon.Deterministic
{
	/// <summary>
	/// The interface encapsulates managing the Quantum input.
	/// </summary>
	public interface IDeterministicInputProvider
	{
		/// <summary>
		/// Is all input for this frame available.
		/// </summary>
		/// <param name="frame">Frame number</param>
		/// <returns><see langword="true" /> if all input is available and the simulation can progress</returns>
		bool CanSimulate(int frame);

		/// <summary>
		/// Get the input for the given frame and player.
		/// </summary>
		/// <param name="frame">Frame number</param>
		/// <param name="playerSlot">Local player slot</param>
		/// <returns>The input struct for that player that can be inserted into the simulation</returns>
		DeterministicFrameInputTemp GetInput(int frame, int playerSlot);
	}
}

