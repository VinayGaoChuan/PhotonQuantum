using System.Collections.Generic;

namespace Photon.Deterministic
{
	internal class DeterministicFutureInputPool
	{
		private Stack<DeterministicFutureInput> _futureInputPool = new Stack<DeterministicFutureInput>();

		public DeterministicFutureInput AllocInput(int maxPlayers)
		{
			DeterministicFutureInput deterministicFutureInput;
			if (_futureInputPool.Count > 0)
			{
				deterministicFutureInput = _futureInputPool.Pop();
				deterministicFutureInput.Frame = 0;
			}
			else
			{
				deterministicFutureInput = new DeterministicFutureInput();
				deterministicFutureInput.Rpc = new byte[maxPlayers][];
				deterministicFutureInput.Input = new byte[maxPlayers][];
				deterministicFutureInput.InputStatus = new InputStatus[maxPlayers];
				deterministicFutureInput.InputFlags = new DeterministicInputFlags[maxPlayers];
			}
			for (int i = 0; i < maxPlayers; i++)
			{
				deterministicFutureInput.Rpc[i] = null;
				deterministicFutureInput.Input[i] = null;
				deterministicFutureInput.InputStatus[i] = InputStatus.None;
				deterministicFutureInput.InputFlags[i] = (DeterministicInputFlags)0;
			}
			return deterministicFutureInput;
		}

		public void ReleaseInput(DeterministicFutureInput input)
		{
			if (input != null)
			{
				_futureInputPool.Push(input);
			}
		}
	}
}

