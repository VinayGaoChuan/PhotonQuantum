using System.Threading.Tasks;

namespace Photon.Deterministic
{
	internal class ICommunicatorDummy : ICommunicator
	{
		public int RoundTripTime => 0;

		public int ActorNumber => 0;

		public bool IsConnected => false;

		public void AddEventListener(OnEventReceived onEventReceived)
		{
		}

		public void OnDestroy()
		{
		}

		public Task OnDestroyAsync()
		{
			return Task.CompletedTask;
		}

		public void DisposeEventObject(object obj)
		{
		}

		public void RaiseEvent(byte eventCode, byte[] message, int messageLength, bool reliable, int[] toPlayers)
		{
		}

		public void Service()
		{
		}
	}
}

