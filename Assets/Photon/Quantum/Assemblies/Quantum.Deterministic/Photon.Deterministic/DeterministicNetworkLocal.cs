using System.Collections.Generic;
using Photon.Deterministic.Protocol;

namespace Photon.Deterministic
{
	internal class DeterministicNetworkLocal : IDeterministicNetwork
	{
		private RttUpdate _rttUpdate;

		private DeterministicSession _session;

		private Dispatcher<Message> _outDispatcher;

		private Dispatcher<Message> _inDispatcher;

		private NetworkQueue<DeterministicTickInput> _inputReceiveQueue;

		private Queue<Message> _inQueue;

		private Queue<Message> _outQueue;

		private LocalInputProvider _localInputProvider;

		public int RoundTripTime => 0;

		public int ActorNumber { get; set; }

		public NetworkQueue<DeterministicTickInput> InputReceiveQueue => _inputReceiveQueue;

		public DeterministicNetworkLocal(DeterministicSession session, DeterministicSimulator simulator, IDeterministicRpcProvider rpcProvider, IDeterministicInputProvider inputProvider = null)
		{
			_rttUpdate = new RttUpdate();
			_session = session;
			_inputReceiveQueue = new NetworkQueue<DeterministicTickInput>();
			_localInputProvider = inputProvider as LocalInputProvider;
			_outDispatcher = _session.CreateNetworkMessageDispatcher();
			_outQueue = new Queue<Message>();
			_inQueue = new Queue<Message>();
			_inDispatcher = new Dispatcher<Message>();
			_inDispatcher.Bind<StartRequest>(OnStartRequest);
			_inDispatcher.Bind<RemovePlayer>(OnRemovePlayer);
			_inDispatcher.Bind<GameResult>(OnGameResult);
		}

		public void Poll()
		{
			_session.OnProtocolRttUpdate(_rttUpdate.Rtt);
			while (_outQueue.Count > 0)
			{
				_outDispatcher.DispatchNext(_outQueue);
			}
			while (_inQueue.Count > 0)
			{
				_inDispatcher.DispatchNext(_inQueue);
			}
		}

		public void ResetInputState(int tick)
		{
		}

		public void ResetInputState(DeterministicFrame frame)
		{
		}

		public void SendProtocolMessage(Message msg)
		{
			_inQueue.Enqueue(msg);
		}

		public void SendSimulationMessage(Message msg)
		{
			_inQueue.Enqueue(msg);
		}

		public void Destroy()
		{
		}

		public void SendLocalInputs()
		{
		}

		public void QueueLocalInput(DeterministicFrameInputTemp input, int playerSlot)
		{
		}

		public void SendLocalRtt(int rtt)
		{
		}

		public void SendLocalChecksum(int tick, ulong checksum)
		{
		}

		private void OnStartRequest(StartRequest startRequest)
		{
			_session.GetLocalConfigs(out startRequest.SessionConfig, out startRequest.RuntimeConfig);
			_outQueue.Enqueue(new SimulationStart
			{
				Reconnect = true,
				ServerTime = 0.0,
				RuntimeConfig = startRequest.RuntimeConfig,
				SessionConfig = startRequest.SessionConfig
			});
			Poll();
		}

		private void OnRemovePlayer(RemovePlayer msg)
		{
			_localInputProvider?.OnRemoveLocalPlayer(msg.PlayerSlot);
		}

		private void OnGameResult(GameResult msg)
		{
		}
	}
}

