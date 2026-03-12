namespace Photon.Deterministic
{
	/// <summary>
	/// The interface to implement streaming input for a Quantum replay session.
	/// </summary>
	public interface IDeterministicStreamReplayInputProvider : IDeterministicReplayProvider, IDeterministicRpcProvider, IDeterministicInputProvider
	{
		/// <summary>
		/// The maximum frame number that the input provider can provide at a given moment.
		/// </summary>
		int MaxFrame { get; }

		/// <summary>
		/// Request input for a certain frame.
		/// </summary>
		/// <param name="frame">The frame for the requested input</param>
		/// <returns>The size of the input to be read</returns>
		int BeginReadFrame(int frame);

		/// <summary>
		/// Read the input data for the frame.
		/// </summary>
		/// <param name="frame">The frame to read the input for</param>
		/// <param name="length">The input length requested in <see cref="M:Photon.Deterministic.IDeterministicStreamReplayInputProvider.BeginReadFrame(System.Int32)" /></param>
		/// <param name="data">The array to copy input data to</param>
		void CompleteReadFrame(int frame, int length, ref byte[] data);

		/// <summary>
		/// Resets the input provider when restarting a replay for example. The stream should be reset to the beginning.
		/// </summary>
		void Reset();
	}
}

