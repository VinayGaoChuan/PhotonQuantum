using Photon.Deterministic.Protocol;

namespace Photon.Deterministic
{
	internal interface IDeterministicNetwork
	{
		int RoundTripTime { get; }

		int ActorNumber { get; }

		NetworkQueue<DeterministicTickInput> InputReceiveQueue { get; }

		void Poll();

		void Destroy();

		void SendLocalRtt(int rtt);

		void SendLocalChecksum(int tick, ulong checksum);

		void SendLocalInputs();

		void QueueLocalInput(DeterministicFrameInputTemp input, int playerSlot);

		void ResetInputState(int tick);

		void ResetInputState(DeterministicFrame frame);

		void SendProtocolMessage(Message msg);

		void SendSimulationMessage(Message msg);
	}
}

