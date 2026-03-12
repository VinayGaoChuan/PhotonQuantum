namespace Photon.Deterministic
{
	/// <summary>
	/// Input header information.
	/// </summary>
	public struct DeterministicTickInputEncodeHeader
	{
		internal const int PLAYER_COUNT_BITS = 8;

		internal const int INPUT_FIXED_SIZE_BITS = 10;

		/// <summary>
		/// The max ping that the server recorded for this client.
		/// </summary>
		public int MaxPing;

		/// <summary>
		/// The max player count of the simulation.
		/// </summary>
		public int PlayerCount;

		/// <summary>
		/// The input fixed size.
		/// </summary>
		public int InputFixedSize;

		/// <summary>
		/// The server time in seconds.
		/// </summary>
		public double ServerTime;

		/// <summary>
		/// The server time scaling.
		/// </summary>
		public double ServerTimeScale;

		/// <summary>
		/// Legacy serialization method.
		/// </summary>
		/// <param name="stream">Stream</param>
		public void Legacy_Serialize(BitStream stream)
		{
			if (stream.Writing)
			{
				stream.WriteInt(PlayerCount, 8);
				stream.WriteInt(InputFixedSize, 10);
			}
			else
			{
				PlayerCount = stream.ReadInt(8);
				InputFixedSize = stream.ReadInt(10);
			}
		}

		/// <summary>
		/// Serialize the input header to a bitstream.
		/// </summary>
		/// <param name="stream">Stream</param>
		public void Serialize(BitStream stream)
		{
			stream.Serialize(ref MaxPing);
			stream.Serialize(ref ServerTime);
			stream.Serialize(ref ServerTimeScale);
			if (stream.Writing)
			{
				stream.WriteInt(PlayerCount, 8);
				stream.WriteInt(InputFixedSize, 10);
			}
			else
			{
				PlayerCount = stream.ReadInt(8);
				InputFixedSize = stream.ReadInt(10);
			}
		}
	}
}

