using System;
using System.Runtime.CompilerServices;
using Quantum;

namespace Photon.Deterministic
{
	/// <summary>
	/// The base class for the simulation frame. Also referred as rollback-able game state.
	/// </summary>
	public abstract class DeterministicFrame
	{
		/// <summary>
		/// This flag controls if the heap data is dumped when calling <see cref="M:Photon.Deterministic.DeterministicFrame.DumpFrame(System.Int32)" />.
		/// </summary>
		public const int DumpFlag_NoHeap = 1;

		internal DeterministicFrameInput Input;

		internal bool Verified;

		/// <summary>
		/// The raw inputs for the delta compressed input.
		/// </summary>
		public int[] RawInputs;

		/// <summary>
		/// The frame(/tick) number.
		/// </summary>
		public int Number;

		/// <summary>
		/// The frame is verified (all input has been validated by the server).
		/// </summary>
		public bool IsVerified
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return Verified;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				Verified = value;
			}
		}

		/// <summary>
		/// The frame is predicted (input may be incorrect and rolled-back by the server).
		/// </summary>
		public bool IsPredicted => !Verified;

		/// <summary>
		/// The raw input size.
		/// </summary>
		public int RawInputSize => Input.InputSize;

		/// <summary>
		/// Set the raw inputs for the delta compression.
		/// </summary>
		/// <param name="inputs">Inputs data</param>
		/// <param name="length">Input data length</param>
		public void SetRawInputs(int[] inputs, int length)
		{
			Array.Copy(inputs, RawInputs, length);
		}

		/// <summary>
		/// Get the raw inputs for delta compression.
		/// </summary>
		/// <returns>Raw inputs</returns>
		public int[] GetRawInputs()
		{
			return RawInputs;
		}

		public DeterministicFrame(int playerCount, int inputSize, Native.Allocator allocator)
		{
			Input = new DeterministicFrameInput(playerCount, inputSize, allocator);
			RawInputs = new int[32768];
		}

		/// <summary>
		/// Initialize the frame.
		/// </summary>
		/// <param name="playerCount">Simulation player count</param>
		/// <param name="inputSize">Input fixed size</param>
		/// <param name="allocator">Native allocator</param>
		/// <param name="resetRawInputs">Restarting a replay requires the raw input to be not reset</param>
		public void Init(int playerCount, int inputSize, Native.Allocator allocator, bool resetRawInputs = true)
		{
			Assert.Always(Input != null, "Expected Input object to be not null");
			Input.Clear();
			Assert.Always(RawInputs != null, "Expected RawInput buffer to be not null");
			if (resetRawInputs)
			{
				Array.Clear(RawInputs, 0, RawInputs.Length);
			}
		}

		internal void CopyInputFrom(DeterministicFrameInput frameInput)
		{
			Input.CopyFrom(frameInput);
		}

		internal DeterministicInputFlags GetPlayerInputFlagsInternal(int player)
		{
			if (player >= 0 && player < Input.PlayerCount)
			{
				return Input.Flags[player];
			}
			return (DeterministicInputFlags)0;
		}

		public void SerializeInputFlags(IBitStream stream)
		{
			Assert.Always(Input != null, "Expected Input object to be not null");
			if (stream.Writing)
			{
				stream.WriteInt(Input.PlayerCount);
				stream.WriteInt(Input.InputSize);
			}
			else
			{
				int num = stream.ReadInt();
				int num2 = stream.ReadInt();
				Assert.Always<int, int>(num == Input.PlayerCount, "Input.PlayerCount mismatch: expected {0}, got {1}", Input.PlayerCount, num);
				Assert.Always<int, int>(num2 == Input.InputSize, "Input.InputSize mismatch: expected {0}, got {1}", Input.InputSize, num2);
			}
			for (int i = 0; i < Input.PlayerCount; i++)
			{
				byte value = (byte)Input.Flags[i];
				stream.Serialize(ref value);
				Input.Flags[i] = (DeterministicInputFlags)value;
			}
		}

		/// <summary>
		/// Get the raw RPC data for the player.
		/// </summary>
		/// <param name="player">Player</param>
		/// <returns>Raw RPC data</returns>
		public byte[] GetRawRpc(PlayerRef player)
		{
			if ((int)player >= 0 && (int)player < Input.PlayerCount)
			{
				return Input.Rpcs[(int)player];
			}
			return null;
		}

		/// <summary>
		/// Get the raw input for a player. Result can be casted to simulation specific Input pointer.
		/// </summary>
		/// <param name="player">Player</param>
		/// <returns>Pointer to input memory block</returns>
		public unsafe byte* GetRawInput(PlayerRef player)
		{
			if ((int)player >= 0 && (int)player < Input.PlayerCount)
			{
				return Input.GetPlayerInput(player);
			}
			return null;
		}

		/// <summary>
		/// Get the player input flags <see cref="T:Photon.Deterministic.DeterministicInputFlags" />.
		/// </summary>
		/// <param name="player">Player</param>
		/// <returns>Input flags.</returns>
		public DeterministicInputFlags GetPlayerInputFlags(PlayerRef player)
		{
			return GetPlayerInputFlagsInternal(player) & ~DeterministicInputFlags.Repeatable;
		}

		/// <summary>
		/// Get the player command for this frame.
		/// </summary>
		/// <param name="player">Player</param>
		/// <returns>A command if one is available.</returns>
		public DeterministicCommand GetPlayerCommand(PlayerRef player)
		{
			if ((int)player >= 0 && (int)player < Input.PlayerCount)
			{
				return Input.Cmds[(int)player];
			}
			return null;
		}

		/// <summary>
		/// Get player command and cast to specific type.
		/// </summary>
		/// <typeparam name="T">Command type</typeparam>
		/// <param name="player">Player</param>
		/// <returns>Available command of concrete type or null if there is no command or that command is of a different type.</returns>
		public T GetPlayerCommand<T>(PlayerRef player) where T : DeterministicCommand
		{
			if ((int)player >= 0 && (int)player < Input.PlayerCount)
			{
				return Input.Cmds[(int)player] as T;
			}
			return null;
		}

		/// <summary>
		/// Get player command and cast to specific type.
		/// </summary>
		/// <typeparam name="T">Command type</typeparam>
		/// <param name="player">Player</param>
		/// <param name="command">Available command of concrete type or null if there is no command or that command is of a different type.</param>
		/// <returns><see langword="true" /> if a command of this type was found.</returns>
		public bool TryGetPlayerCommand<T>(PlayerRef player, out T command) where T : DeterministicCommand
		{
			command = GetPlayerCommand<T>(player);
			return command != null;
		}

		/// <summary>
		/// Dumps the frame with the specified dump flags.
		/// </summary>
		/// <param name="dumpFlags">The dump flags.</param>
		/// <returns>The dumped frame as a string.</returns>
		public abstract string DumpFrame(int dumpFlags = 0);

		/// <summary>
		/// Calculates the checksum of the frame.
		/// </summary>
		/// <returns>The calculated checksum.</returns>
		public abstract ulong CalculateChecksum();

		/// <summary>
		/// Frees the resources used by the frame.
		/// </summary>
		public abstract void Free();

		protected void Free(Native.Allocator allocator)
		{
			if (Input != null)
			{
				Input.Free(allocator);
				Input = null;
			}
		}

		/// <summary>
		/// Save all frame data.
		/// </summary>
		/// <param name="mode">The serialization mode.</param>
		/// <returns>The serialized frame.</returns>
		public abstract byte[] Serialize(DeterministicFrameSerializeMode mode);

		/// <summary>
		/// Load all frame data from the serialized frame.
		/// </summary>
		/// <param name="data">Serialized frame.</param>
		public abstract void Deserialize(byte[] data);

		/// <summary>
		/// Copy the internal frame data. Is called from <see cref="M:Photon.Deterministic.DeterministicFrame.CopyFrom(Photon.Deterministic.DeterministicFrame)" />.
		/// </summary>
		/// <param name="frame">Frame to copy</param>
		protected abstract void Copy(DeterministicFrame frame);

		/// <summary>
		/// Copies the data from the specified frame to this frame.
		/// </summary>
		/// <param name="frame">The frame to copy from.</param>
		public void CopyFrom(DeterministicFrame frame)
		{
			Copy(frame);
			Number = frame.Number;
		}

		/// <summary>
		/// Handles the event when a player is added to the session.
		/// </summary>
		/// <param name="frame">The frame.</param>
		/// <param name="session">The session.</param>
		/// <param name="playerSlot">The player slot.</param>
		/// <param name="actorNumber">The actor number.</param>
		/// <param name="player">The player reference.</param>
		/// <param name="invokeCallback">Invoke game callback.</param>
		public static void OnPlayerAdded(DeterministicFrame frame, DeterministicSession session, int playerSlot, int actorNumber, PlayerRef player, bool invokeCallback)
		{
			session.OnPlayerAdded(frame, playerSlot, actorNumber, player, invokeCallback);
		}

		/// <summary>
		/// Handles the event when a player is removed from the session.
		/// </summary>
		/// <param name="frame">The frame.</param>
		/// <param name="session">The session.</param>
		/// <param name="player">The player reference.</param>
		public static void OnPlayerRemoved(DeterministicFrame frame, DeterministicSession session, PlayerRef player)
		{
			session.OnPlayerRemoved(frame, player);
		}

		/// <summary>
		/// This method will check if the frame number equals RollbackWindow - 1, which indicates
		/// that a snapshots has been promoted online.
		/// The online simulation will not have any players, so local payers have to be reset.
		/// </summary>
		/// <returns>True, when the local snapshot to online promotion was detected.</returns>
		internal bool TryResetPlayerMapping(DeterministicSessionConfig sessionConfig)
		{
			if (Number == sessionConfig.RollbackWindow - 1)
			{
				ResetPlayerMapping();
				return true;
			}
			return false;
		}

		protected abstract void ResetPlayerMapping();

		public abstract void RestorePlayerMapping(DeterministicSession session);
	}
}

