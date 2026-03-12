namespace Photon.Deterministic
{
	/// <summary>
	/// The interface of the serializer object used to serialize and deserialize components.
	/// </summary>
	public interface IDeterministicFrameSerializer
	{
		/// <summary>
		/// Returns <see langword="true" /> if the serializer is writing.
		/// </summary>
		bool Writing { get; }

		/// <summary>
		/// Returns <see langword="true" /> if the serializer is reading.
		/// </summary>
		bool Reading { get; }

		/// <summary>
		/// The stream used by the serializer.
		/// </summary>
		IBitStream Stream { get; }
	}
}

