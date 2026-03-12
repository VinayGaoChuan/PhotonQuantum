namespace Photon.Deterministic
{
	/// <summary>
	/// The interface encapsulates managing the access to delta compressed input.
	/// </summary>
	public interface IDeterministicDeltaCompressedInput
	{
		/// <summary>
		/// Raw input as int array is saved on the frame.
		/// </summary>
		void GetRawInput(DeterministicFrame frame, ref int[] data);

		/// <summary>
		/// Reset input state for late-joins and instant replays.
		/// </summary>
		void ResetInputState(DeterministicFrame frame);

		/// <summary>
		/// Timing required for the local input provider to cache the raw input and call OnInputSetConfirmed callback.
		/// </summary>
		void OnInputPollingDone(int frame, int playerCount);
	}
}

