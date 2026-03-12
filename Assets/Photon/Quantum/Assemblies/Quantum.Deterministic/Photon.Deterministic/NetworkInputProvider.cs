namespace Photon.Deterministic
{
	internal class NetworkInputProvider : IDeterministicInputProvider
	{
		private IDeterministicGame _game;

		public NetworkInputProvider(IDeterministicGame game)
		{
			_game = game;
		}

		public bool CanSimulate(int frame)
		{
			return true;
		}

		public DeterministicFrameInputTemp GetInput(int frame, int playerSlot)
		{
			return _game.OnLocalInput(frame, playerSlot);
		}
	}
}

