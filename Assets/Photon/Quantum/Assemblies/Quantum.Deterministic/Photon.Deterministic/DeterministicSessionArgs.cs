namespace Photon.Deterministic
{
	/// <summary>
	/// Represents the starting arguments for a deterministic constructor.
	/// </summary>
	public struct DeterministicSessionArgs
	{
		/// <summary>
		/// Gets or sets the game mode.
		/// </summary>
		public DeterministicGameMode Mode;

		/// <summary>
		/// Gets or sets the session config.
		/// </summary>
		public DeterministicSessionConfig SessionConfig;

		/// <summary>
		/// Gets or sets the deterministic game interface.
		/// </summary>
		public IDeterministicGame Game;

		/// <summary>
		/// Gets or sets the communicator interface.
		/// </summary>
		public ICommunicator Communicator;

		/// <summary>
		/// Gets or sets the input provider.
		/// </summary>
		public IDeterministicReplayProvider Replay;

		/// <summary>
		/// Gets or sets the platform information.
		/// </summary>
		public DeterministicPlatformInfo PlatformInfo;

		/// <summary>
		/// Gets or sets the initial tick.
		/// </summary>
		public int InitialTick;

		/// <summary>
		/// Gets or sets a value indicating whether to disable interpolated states.
		/// </summary>
		public bool DisableInterpolatableStates;

		/// <summary>
		/// Gets or sets the frame data.
		/// </summary>
		public byte[] FrameData;

		/// <summary>
		/// Gets or sets the runtime configuration.
		/// </summary>
		public byte[] RuntimeConfig;
	}
}

