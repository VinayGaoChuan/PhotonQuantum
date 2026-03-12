namespace Photon.Deterministic
{
	/// <summary>
	/// The mode to find a frame in the <see cref="T:Photon.Deterministic.DeterministicFrameRingBuffer" /> to best approximate the desired frame.
	/// </summary>
	public enum DeterministicFrameSnapshotBufferFindMode
	{
		/// <summary>
		/// Will only return the frame if the frame number is exactly the same otherwise null.
		/// </summary>
		Equal,
		/// <summary>
		/// Will return a frame that is equal to the desired frame or less.
		/// </summary>
		ClosestLessThanOrEqual,
		/// <summary>
		/// Will return the frame that is closest to the desired frame.
		/// </summary>
		Closest
	}
}

