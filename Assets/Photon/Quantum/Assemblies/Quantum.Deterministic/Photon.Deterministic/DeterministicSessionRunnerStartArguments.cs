namespace Photon.Deterministic
{
	/// <summary>
	/// The arguments to start a <see cref="T:Photon.Deterministic.IDeterministicSessionRunner" />.
	/// </summary>
	public struct DeterministicSessionRunnerStartArguments
	{
		/// <summary>
		/// The input provider used to feed the simulation with player input.
		/// </summary>
		public IDeterministicReplayProvider InputProvider;

		/// <summary>
		/// The Quantum session config.
		/// </summary>
		public DeterministicSessionConfig SessionConfig;

		/// <summary>
		/// The custom RuntimeConfig used.
		/// </summary>
		public byte[] RuntimeConfig;

		/// <summary>
		/// Resource manager from <see cref="T:Photon.Deterministic.IDeterministicSessionContext" />
		/// </summary>
		public object ResourceManager;
	}
}

