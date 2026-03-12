using System;

namespace Photon.Deterministic
{
	/// <summary>
	/// A struct that saves input data for a player duplicating internal input data that is not save to use because of recycling.
	/// </summary>
	public struct DeterministicFrameInputTemp
	{
		/// <summary>
		/// The tick of the frame this input belongs to.
		/// </summary>
		public int Frame;

		/// <summary>
		/// The Player that the input belongs to.
		/// </summary>
		public int Player;

		/// <summary>
		/// The RPC data.
		/// </summary>
		public byte[] Rpc;

		/// <summary>
		/// The input data.
		/// </summary>
		public byte[] Data;

		/// <summary>
		/// The Data length.
		/// </summary>
		public int DataLength;

		/// <summary>
		/// A flag indicating if the data is verified by the server.
		/// </summary>
		public bool IsVerified;

		/// <summary>
		/// The input flags.
		/// </summary>
		public DeterministicInputFlags Flags;

		/// <summary>
		/// Copies the <see cref="F:Photon.Deterministic.DeterministicFrameInputTemp.Data" /> into a new array.
		/// </summary>
		/// <returns></returns>
		public byte[] CloneData()
		{
			if (Data == null)
			{
				return null;
			}
			if (DataLength == 0)
			{
				return null;
			}
			byte[] array = new byte[DataLength];
			Buffer.BlockCopy(Data, 0, array, 0, DataLength);
			return array;
		}

		/// <summary>
		/// Creates a verified input object.
		/// </summary>
		/// <param name="frame">Tick of the frame</param>
		/// <param name="player">The Quantum player this input is for.</param>
		/// <param name="rpc">The rpc data</param>
		/// <param name="data">The input data</param>
		/// <param name="dataLength">The input data length</param>
		/// <param name="flags">The input flags</param>
		/// <returns>An instance of DeterministicFrameInputTemp with the Verified toggle enabled.</returns>
		public static DeterministicFrameInputTemp Verified(int frame, int player, byte[] rpc, byte[] data, int dataLength, DeterministicInputFlags flags)
		{
			return new DeterministicFrameInputTemp(frame, player, rpc, data, dataLength, flags, verified: true);
		}

		/// <summary>
		/// Creates a predicted input object for tick 0 and player 0 without rpc data.
		/// </summary>
		/// <param name="data">The input data</param>
		/// <param name="dataLength">The input length</param>
		/// <param name="flags">The input flags</param>
		/// <returns>An instance of DeterministicFrameInputTemp with the Verified toggle disabled.</returns>
		public static DeterministicFrameInputTemp Predicted(byte[] data, int dataLength, DeterministicInputFlags flags)
		{
			return new DeterministicFrameInputTemp(0, 0, null, data, dataLength, flags, verified: false);
		}

		/// <summary>
		/// Creates a predicted input object for tick 0 without rpc data.
		/// </summary>
		/// <param name="player">The Quantum player</param>
		/// <param name="data">The input data</param>
		/// <param name="dataLength">The input data length</param>
		/// <param name="flags">The input flags</param>
		/// <returns>An instance of DeterministicFrameInputTemp with the Verified toggle disabled.</returns>
		public static DeterministicFrameInputTemp Predicted(int player, byte[] data, int dataLength, DeterministicInputFlags flags)
		{
			return new DeterministicFrameInputTemp(0, player, null, data, dataLength, flags, verified: false);
		}

		/// <summary>
		/// Creates a predicted input object without rpc data.
		/// </summary>
		/// <param name="frame">Tick</param>
		/// <param name="player">The Quantum player</param>
		/// <param name="data">The input data</param>
		/// <param name="dataLength">The input data length</param>
		/// <param name="flags">The input flags</param>
		/// <returns>An instance of DeterministicFrameInputTemp with the Verified toggle disabled.</returns>
		public static DeterministicFrameInputTemp Predicted(int frame, int player, byte[] data, int dataLength, DeterministicInputFlags flags)
		{
			return new DeterministicFrameInputTemp(frame, player, null, data, dataLength, flags, verified: false);
		}

		/// <summary>
		/// Creates a predicted input object.
		/// </summary>
		/// <param name="frame">Tick of the frame</param>
		/// <param name="player">The Quantum player this input is for.</param>
		/// <param name="rpc">The rpc data</param>
		/// <param name="data">The input data</param>
		/// <param name="dataLength">The input data length</param>
		/// <param name="flags">The input flags</param>
		/// <returns>An instance of DeterministicFrameInputTemp with the Verified toggle disabled.</returns>
		public static DeterministicFrameInputTemp Predicted(int frame, int player, byte[] rpc, byte[] data, int dataLength, DeterministicInputFlags flags)
		{
			return new DeterministicFrameInputTemp(frame, player, rpc, data, dataLength, flags, verified: false);
		}

		private DeterministicFrameInputTemp(int frame, int player, byte[] rpc, byte[] data, int dataLength, DeterministicInputFlags flags, bool verified)
		{
			Frame = frame;
			Player = player;
			Rpc = rpc;
			Data = data;
			DataLength = dataLength;
			Flags = flags;
			IsVerified = verified;
		}
	}
}

