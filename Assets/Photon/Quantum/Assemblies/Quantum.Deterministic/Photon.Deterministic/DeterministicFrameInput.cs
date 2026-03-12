namespace Photon.Deterministic
{
	internal class DeterministicFrameInput
	{
		private int _playerCount;

		private int _inputSize;

		public byte[][] Rpcs;

		public DeterministicCommand[] Cmds;

		public DeterministicInputFlags[] Flags;

		public unsafe byte* Input;

		public int PlayerCount => _playerCount;

		public int InputSize => _inputSize;

		public unsafe void Clear()
		{
			Native.Utils.Clear(Input, _playerCount * _inputSize);
			for (int i = 0; i < Rpcs.Length; i++)
			{
				Rpcs[i] = null;
			}
			for (int j = 0; j < Flags.Length; j++)
			{
				Flags[j] = DeterministicInputFlags.PlayerNotPresent;
			}
			for (int k = 0; k < Cmds.Length; k++)
			{
				Cmds[k]?.Dispose();
				Cmds[k] = null;
			}
		}

		public unsafe byte* GetPlayerInput(int player)
		{
			return Input + player * _inputSize;
		}

		public unsafe DeterministicFrameInput(int playerCount, int inputSize, Native.Allocator allocator)
		{
			_playerCount = playerCount;
			_inputSize = inputSize;
			Rpcs = new byte[playerCount][];
			Cmds = new DeterministicCommand[playerCount];
			Flags = new DeterministicInputFlags[playerCount];
			for (int i = 0; i < Flags.Length; i++)
			{
				Flags[i] = DeterministicInputFlags.PlayerNotPresent;
			}
			Input = (byte*)allocator.AllocAndClear(playerCount * inputSize);
		}

		public void DeserializeCommands(DeterministicCommandSerializer serializer)
		{
			for (int i = 0; i < Rpcs.Length; i++)
			{
				if ((Flags[i] & DeterministicInputFlags.Command) == DeterministicInputFlags.Command && Rpcs[i] != null && (Cmds[i] == null || Cmds[i].Source != Rpcs[i]) && serializer.TryDecodeCommand<DeterministicCommand>(Rpcs[i], out var result))
				{
					Cmds[i] = result;
					Cmds[i].Source = Rpcs[i];
				}
			}
		}

		public unsafe void Assign(DeterministicFrameInputTemp input, IDeterministicGame sessionGame)
		{
			if (input.IsVerified)
			{
				Cmds[input.Player]?.Dispose();
				Cmds[input.Player] = null;
			}
			Rpcs[input.Player] = input.Rpc;
			Flags[input.Player] = input.Flags;
			sessionGame.DeserializeInputInto(input.Player, input.Data, GetPlayerInput(input.Player), input.IsVerified);
		}

		public unsafe void CopyFrom(DeterministicFrameInput from)
		{
			for (int i = 0; i < PlayerCount; i++)
			{
				Cmds[i] = from.Cmds[i];
				Rpcs[i] = from.Rpcs[i];
				Flags[i] = from.Flags[i];
			}
			Native.Utils.Copy(Input, from.Input, _playerCount * _inputSize);
		}

		public unsafe void CopyForPrediction(DeterministicFrameInput from, int player)
		{
			Flags[player] |= from.Flags[player] & ~DeterministicInputFlags.Command;
			Native.Utils.Copy(GetPlayerInput(player), from.GetPlayerInput(player), _inputSize);
		}

		public unsafe void Free(Native.Allocator allocator)
		{
			Rpcs = null;
			Flags = null;
			Cmds = null;
			if (Input != null)
			{
				allocator.Free(Input);
				Input = null;
			}
		}
	}
}

