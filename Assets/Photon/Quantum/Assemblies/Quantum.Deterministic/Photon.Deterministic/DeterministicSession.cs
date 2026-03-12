using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Photon.Deterministic.Protocol;
using Quantum;

namespace Photon.Deterministic
{
	/// <summary>
	/// Represents a Quantum runtime match, holding the references to Game, its Frames (game state) and Simulator, and implements (together with Simulator) the predict/rollback logic, time control and input management.
	/// Single use, so whenever a Session is shutdown (or disconnected), it can't be reused anymore and a new one must be instantiated.
	/// </summary>
	public class DeterministicSession
	{
		private DeterministicPlayerMap _localPlayerMap;

		private List<PlayerRef> _allPlayers;

		private byte[] _runtimeConfig;

		private byte[] _runtimeConfigLocal;

		private DeterministicSessionConfig _sessionConfig;

		private DeterministicSessionConfig _sessionConfigLocal;

		private IDisposable _frameContext;

		internal IDeterministicNetwork _network;

		private IDeterministicRpcProvider _rpcProvider;

		private IDeterministicInputProvider _inputProvider;

		private IDeterministicReplayProvider _replayProvider;

		private IDeterministicDeltaCompressedInput _deltaCompressionInput;

		private int _simulatorInputOffset;

		private int _simulatorInputOffsetPrevious;

		private DeterministicSimulator _simulator;

		private DeterministicTickChecksumError _checksumError;

		private DeterministicCommandSerializer _commandSerializer;

		private DeterministicGameMode _mode;

		private DeterministicSessionState _state;

		private DeltaClock _deltaClock;

		private Stopwatch _syncClockTimer;

		private Stopwatch _statsUpdateTimer;

		private Stopwatch _rttSyncTimer;

		private bool _lockstep;

		private bool _disableInterpolatableStates;

		private bool _reconnect;

		private double _serverTimeReceived;

		private double _serverTimeAdjusted;

		private double _lastRttSync;

		private double _timeScale = 1.0;

		private List<double> _clientRttAvg = new List<double>();

		private List<double> _serverTimeAvgDiff = new List<double>();

		private int _maxClientRtt;

		internal ITimeProvider _timeProvider;

		private DeterministicStats _stats;

		private IDeterministicGame _game;

		private DeterministicPlatformInfo _platformInfo;

		private DeterministicTickInput.Pool _inputPool = new DeterministicTickInput.Pool();

		private Dictionary<int, ChecksumErrorFrameDump> _checksumErrorFrameDumps = new Dictionary<int, ChecksumErrorFrameDump>();

		private int _maxVerifiedTicksPerUpdate = int.MaxValue;

		private double _spectatingOffsetSec;

		internal List<FrameSnapshot> _snapshots = new List<FrameSnapshot>();

		internal byte[] _frameData;

		internal int _initialTick;

		/// <summary>
		/// Limit the maximum number of verified ticks computed per update. Default is int.MaxValue.
		/// </summary>
		public int MaxVerifiedTicksPerUpdate
		{
			get
			{
				return _maxVerifiedTicksPerUpdate;
			}
			set
			{
				_maxVerifiedTicksPerUpdate = value;
			}
		}

		/// <summary>
		/// Specify the offset in sec that the simulation will run behind in Spectator mode. Value must be negative or 0. Default is -1 sec. 
		/// </summary>
		public double SpectatingOffsetSec
		{
			get
			{
				return _spectatingOffsetSec;
			}
			set
			{
				_spectatingOffsetSec = Math.Min(value, 0.0);
			}
		}

		/// <summary>
		/// Serialized local copy of the game/session custom RuntimeConfig instance (received from the photon server). Since this has been received from server, it's the same in all instances.
		/// </summary>
		public byte[] RuntimeConfig => _runtimeConfig;

		/// <summary>
		/// Copy of local runtime stats (stores the last Update values for simulation time, number of frames simulated, etc).
		/// Used by the QuantumStatsUI to show these in runtime on Unity.
		/// </summary>
		public DeterministicStats Stats => _stats;

		/// <summary>
		/// The matching game instance, which holds the Frames (game state) and other accessory logic entry points for a predict/rollback match.
		/// </summary>
		public IDeterministicGame Game => _game;

		/// <summary>
		/// Quick accessor to the latest locally predicted copy of the game state.
		/// </summary>
		public DeterministicFrame FramePredicted => _simulator?.FramePredicted;

		/// <summary>
		/// Quick accessor to the second latest locally predicted copy of the game state. Used for accurate visual interpolation of transforms (or any other data) between this and the Predicted frame.
		/// </summary>
		public DeterministicFrame FramePredictedPrevious => _simulator?.FramePredictedPrevious;

		/// <summary>
		/// Quick accessor to the forward-only verified data (simulated with confirmed inputs from server in online games). Can be used as source of truth, as this does not include predicted data.
		/// </summary>
		public DeterministicFrame FrameVerified => _simulator?.FrameVerified;

		/// <summary>
		/// Quick accessor to the latest simulated copy of the Frame that was last-Predicted during the previous main session update. Used to calculate transform view error for smoothed correction.
		/// </summary>
		public DeterministicFrame PreviousUpdateFramePredicted => _simulator?.FramePreviousUpdatePredicted;

		/// <summary>
		/// Local copy of the server-provided main set of settings, controlling update rate, input adjustment and other important control values. Since this has been received from server, it's the same set of values in all instances.
		/// </summary>
		public DeterministicSessionConfig SessionConfig => _sessionConfig;

		/// <summary>
		/// Remaining accumulated time after all frames forward (both verified and predicted) have been simulated. Usually less than a full delta-time (defined by update rate), and used to compute the interpolation alpha.
		/// Can be above delta-time if the current session is temporarily lagging behind and can not simulate forward the full prediction (depending on settings).
		/// </summary>
		public double AccumulatedTime
		{
			get
			{
				if (_simulator == null)
				{
					return 0.0;
				}
				return _simulator.Accumulated;
			}
		}

		/// <summary>
		/// Normally zero (0) for most game clients. Can be set to a specific value if a local copy for initial frame snapshot data is provided (normally used occasionally in case of quick rejoinining a room).
		/// </summary>
		public int InitialTick => _initialTick;

		/// <summary>
		/// Serialized local copy for initial frame snapshot data, if provided when starting a new session (normally used occasionally in case of quick rejoinining a room).
		/// </summary>
		public byte[] IntitialFrameData => _frameData;

		/// <summary>
		/// Current time-dilation delta-time scale. Tf time-dilation settings allow it, server will reduce this to slow down games when some clients have very high ping (reducing the number of predictions). 
		/// </summary>
		public double TimeScale => _timeScale;

		[Obsolete("Use LocalPlayers")]
		public List<PlayerRef> LocalPlayerIndices => _localPlayerMap.Players;

		/// <summary>
		/// The collection of PlayerRefs the local client has control of (sends input for).
		/// </summary>
		public List<PlayerRef> LocalPlayers => _localPlayerMap.Players;

		/// <summary>
		/// The collection of local-indexed (0-n) players the local client has control of (sends input for).
		/// </summary>
		public List<int> LocalPlayerSlots => _localPlayerMap.PlayerSlots;

		/// <summary>
		/// Total/max number of PlayerRefs this particular session instance comprises of. This is not the number of connected players, but rather the total number.
		/// </summary>
		public int PlayerCount
		{
			get
			{
				if (_sessionConfig == null)
				{
					return 0;
				}
				return _sessionConfig.PlayerCount;
			}
		}

		/// <summary>
		/// Fixed rate at which the game is simulated (verified frames). Can be set at DeterministicConfig.
		/// </summary>
		public int SimulationRate
		{
			get
			{
				if (_sessionConfig == null)
				{
					return 0;
				}
				return _sessionConfig.UpdateFPS;
			}
		}

		/// <summary>
		/// Max number of frames/ticks the session is allowed to predict. Can be set at DeterministicConfig.
		/// </summary>
		public int RollbackWindow
		{
			get
			{
				if (_sessionConfig == null)
				{
					return 0;
				}
				return _sessionConfig.RollbackWindow;
			}
		}

		/// <summary>
		/// Current value for the dynamically-adjusted input delay (independent for each client). Starts at the initial offset on DeterministicConfig (defaults to 0), and grows with RTT.
		/// </summary>
		public int LocalInputOffset => _simulatorInputOffset;

		/// <summary>
		/// Stats: Number of predicted frames simulated during the last call to Update().
		/// </summary>
		public int PredictedFrames
		{
			get
			{
				if (_simulator == null)
				{
					return 0;
				}
				return _simulator.FramesPredicted;
			}
		}

		/// <summary>
		/// Stats: Precise time in seconds used by the last call to Update(). Includes all session internal input handling, rollbacks, verified and predicted frames simulation.
		/// </summary>
		public double SimulationTimeElapsed
		{
			get
			{
				if (_simulator == null)
				{
					return 0.0;
				}
				return (double)_simulator.FramePredicted.Number * DeltaTimeDouble;
			}
		}

		[Obsolete("Use SimulationTimeElapsed")]
		public double SimulationTimeElasped => SimulationTimeElapsed;

		/// <summary>
		/// Fixed-point delta-time as defined by DeterministicConfig's UpdateFPS (1/UpdateFPS).
		/// </summary>
		public FP DeltaTime
		{
			get
			{
				if (_sessionConfig == null)
				{
					return FP._0;
				}
				return FP._1 / _sessionConfig.UpdateFPS;
			}
		}

		/// <summary>
		/// Double version of delta-time as defined by DeterministicConfig's UpdateFPS (1/UpdateFPS).
		/// </summary>
		public double DeltaTimeDouble
		{
			get
			{
				if (_sessionConfig == null)
				{
					return 0.0;
				}
				return 1.0 / (double)_sessionConfig.UpdateFPS;
			}
		}

		/// <summary>
		/// Possible modes are online/multiplayer, local or replay.
		/// </summary>
		public DeterministicGameMode GameMode => _mode;

		/// <summary>
		/// <see langword="true" /> for replays and local games (single player or split screen/couch-local). <see langword="false" /> for online games.
		/// </summary>
		public bool IsLocal => !IsOnline;

		/// <summary>
		/// <see langword="true" /> for online games. <see langword="false" /> for replays and local games (single player or split screen/couch-local).
		/// </summary>
		public bool IsOnline => GameMode == DeterministicGameMode.Multiplayer;

		/// <summary>
		/// Always <see langword="true" /> once session is started. <see langword="false" /> if at least one player is controlled by the local session.
		/// </summary>
		public bool IsSpectating => _localPlayerMap.Count == 0;

		/// <summary>
		/// <see langword="true" /> if simulation is being clamped at max prediction (rollback window). Happens normally when input confirmations from server are disrupted (network loss or very high ping times).
		/// </summary>
		public bool IsStalling
		{
			get
			{
				if (_simulator == null)
				{
					return false;
				}
				return _simulator.IsStalling;
			}
		}

		/// <summary>
		/// Temporarily <see langword="true" /> only for a session that has just been started (Start message arrived from server) while waiting for a snapshot to arrive.
		/// </summary>
		public bool IsPaused
		{
			get
			{
				if (_simulator == null)
				{
					return false;
				}
				return _simulator.Paused;
			}
		}

		/// <summary>
		/// Legacy mode in which frames are only simulated forward with input delay, and no prediction is ever performed.
		/// In the past this was the default approach for classic RTS games.
		/// </summary>
		public bool IsLockstep => _lockstep;

		/// <summary>
		/// <see langword="true" /> when not using legacy lockstep mode. Means frames will be predicted and rolled back.
		/// </summary>
		public bool IsPredicted => !_lockstep;

		/// <summary>
		/// <see langword="true" /> when GameMode is Replay (input comes from replay provider).
		/// </summary>
		public bool IsReplay => GameMode == DeterministicGameMode.Replay;

		/// <summary>
		/// If the session provides previous states for interpolation.
		/// </summary>
		public bool IsInterpolatable => !_disableInterpolatableStates;

		/// <summary>
		/// <see langword="true" /> when the final input in the replay stream has been consumed to simulate forward a verified frame.
		/// </summary>
		public bool IsReplayFinished
		{
			get
			{
				if (IsReplay && _simulator.FrameVerified != null)
				{
					return !ReplayProvider.CanSimulate(_simulator.FrameVerified.Number + 1);
				}
				return false;
			}
		}

		/// <summary>
		/// The initial state is idle, then it will be started after confirmation from the server.
		/// </summary>
		public bool HasStarted => State > DeterministicSessionState.Idle;

		/// <summary>
		/// <see langword="true" /> if local session is started and active.
		/// </summary>
		public bool IsRunning => State == DeterministicSessionState.Running;

		/// <summary>
		/// Returns <see langword="true" /> when <see cref="M:Photon.Deterministic.DeterministicSession.Destroy" /> has been called.
		/// </summary>
		public bool IsDestroyed => State == DeterministicSessionState.Destroyed;

		internal DeterministicSessionState State => _state;

		internal DeterministicSimulator Simulator => _simulator;

		/// <summary>
		/// When running replays, input comes from the provider/container.
		/// Replay input streams can com from file, memory, or a network stream (custom).
		/// </summary>
		public IDeterministicReplayProvider ReplayProvider => _replayProvider;

		/// <summary>
		/// Cached info about the local device platform (some OS and hardware available specs).
		/// </summary>
		public DeterministicPlatformInfo PlatformInfo => _platformInfo;

		/// <summary>
		/// Local instance of specialized serializer that can pack/unpack DeterministicCommands to/from byte[]s.
		/// </summary>
		public DeterministicCommandSerializer CommandSerializer => _commandSerializer;

		/// <summary>
		/// Runner is the wrapper for session/game that can be attached to Unity, a .Net console app, or a Quantum Custom Server Plugin. 
		/// </summary>
		public IDisposable Runner { get; set; }

		/// <summary>
		/// Is executed in the end of <see cref="M:Photon.Deterministic.DeterministicSession.Destroy" />. Is used by the server simulation.
		/// </summary>
		public Action<DeterministicSession> OnDestroyed { get; set; }

		private int ClockAdjustmentAccumulationMax => _sessionConfig.TimeCorrectionRate * 4;

		internal int NextFrame
		{
			get
			{
				if (_simulator != null)
				{
					return _simulator.FrameCounter + 1;
				}
				return 0;
			}
		}

		/// <summary>
		/// Accessor to the pool of input objects.
		/// </summary>
		public DeterministicTickInput.Pool InputPool => _inputPool;

		/// <summary>
		/// Create lazily a list of all possible player slots. Used for polling in Replay and Local mode.
		///
		/// Caveat: During DeterministicSession constructor in Local mode PlayerCount is not set, yet.
		/// </summary>
		private List<PlayerRef> AllPlayers
		{
			get
			{
				if (_allPlayers == null)
				{
					_allPlayers = new List<PlayerRef>(PlayerCount);
					for (int i = 0; i < PlayerCount; i++)
					{
						_allPlayers.Add(i);
					}
				}
				return _allPlayers;
			}
		}

		/// <summary>
		/// Tests whereas this PlayerRef is controlled by this local client/session.
		/// <para>Caveat: This method will behave differently in replays, read the replay online documentation for more information.</para>
		/// </summary>
		public bool IsPlayerLocal(PlayerRef player)
		{
			return _localPlayerMap.Has(player);
		}

		/// <summary>
		/// Returns the Photon Actor Number of the connection, if in online mode.
		/// Otherwise, returns 0.
		/// </summary>
		public int GetActorNumber()
		{
			if (_network != null)
			{
				return _network.ActorNumber;
			}
			return 0;
		}

		/// <summary>
		/// Returns <see langword="true" /> and fills in the corresponding global PlayerRef if playerSlot is a valid local slot, for which the session can send input.
		/// <para>Caveat: This method will behave differently in replays, read the replay online documentation for more information.</para>
		/// </summary>
		public bool TryGetLocalPlayer(int playerSlot, out PlayerRef player)
		{
			player = default(PlayerRef);
			if (_localPlayerMap.Has(playerSlot))
			{
				player = _localPlayerMap.PlayerSlotToPlayer[playerSlot];
				return true;
			}
			return false;
		}

		/// <summary>
		/// Checks if this local player slot is currently in use (local slots can be added/removed dynamically).
		/// <para>Caveat: This method will behave differently in replays, read the replay online documentation for more information.</para>
		/// </summary>
		public bool IsPlayerSlotLocal(int playerSlot)
		{
			return _localPlayerMap.Has(playerSlot);
		}

		/// <summary>
		/// Validates a few conditions to assert the session object has coherent settings.
		/// After construction, session is ready to Join/Start a game.
		/// </summary>
		public DeterministicSession(DeterministicSessionArgs args)
		{
			LogStream logInfo = InternalLogStreams.LogInfo;
			if (logInfo != null)
			{
				logInfo.Log($"Platform Info: {args.PlatformInfo}");
			}
			LogStream logInfo2 = InternalLogStreams.LogInfo;
			if (logInfo2 != null)
			{
				logInfo2.Log($"Assembly Info: {Assembly.GetExecutingAssembly().GetName()} (Release)");
			}
			_platformInfo = args.PlatformInfo;
			_mode = args.Mode;
			_game = args.Game;
			_sessionConfigLocal = args.SessionConfig;
			_runtimeConfigLocal = args.RuntimeConfig;
			_localPlayerMap = new DeterministicPlayerMap(args.SessionConfig.PlayerCount);
			if (args.FrameData != null && args.FrameData.Length != 0)
			{
				_initialTick = ((args.InitialTick == 0) ? (args.SessionConfig.RollbackWindow - 1) : args.InitialTick);
			}
			else
			{
				_initialTick = 0;
			}
			_frameData = args.FrameData;
			_commandSerializer = new DeterministicCommandSerializer();
			_disableInterpolatableStates = args.DisableInterpolatableStates;
			_sessionConfigLocal.InputFixedSize = _game.GetInputSerializedFixedSize();
			_state = DeterministicSessionState.Idle;
			_simulatorInputOffset = _sessionConfigLocal.InputDelayMin;
			_simulatorInputOffsetPrevious = _sessionConfigLocal.InputDelayMin;
			_statsUpdateTimer = Stopwatch.StartNew();
			_simulator = new DeterministicSimulator(this);
			switch (args.Mode)
			{
			case DeterministicGameMode.Multiplayer:
				_network = new DeterministicNetwork(this, args.Communicator);
				_rpcProvider = (IDeterministicRpcProvider)_network;
				_inputProvider = new NetworkInputProvider(_game);
				_timeProvider = new DefaultTimeProvider(args.SessionConfig);
				_deltaCompressionInput = (IDeterministicDeltaCompressedInput)_network;
				break;
			case DeterministicGameMode.Local:
				_inputProvider = new LocalInputProvider(_game, this);
				_rpcProvider = (IDeterministicRpcProvider)_inputProvider;
				_deltaCompressionInput = (IDeterministicDeltaCompressedInput)_inputProvider;
				_network = new DeterministicNetworkLocal(this, _simulator, _rpcProvider, _inputProvider);
				break;
			case DeterministicGameMode.Replay:
				Assert.Always(args.Replay != null, "Replay not set.");
				if (!(args.Replay is IDeterministicStreamReplayInputProvider streamInputProvider))
				{
					_replayProvider = args.Replay;
				}
				else
				{
					_deltaCompressionInput = (IDeterministicDeltaCompressedInput)(_replayProvider = new ReplayInputProvider(streamInputProvider, this));
				}
				_inputProvider = _replayProvider;
				_rpcProvider = _replayProvider;
				_network = new DeterministicNetworkLocal(this, _simulator, _rpcProvider, _inputProvider)
				{
					ActorNumber = args.Replay.LocalActorNumber
				};
				break;
			}
			_stats = new DeterministicStats();
			_game.AssignSession(this);
		}

		/// <summary>
		/// Retrieves the local copies of the main configs.
		/// </summary>
		public void GetLocalConfigs(out DeterministicSessionConfig sessionConfig, out byte[] runtimeConfig)
		{
			sessionConfig = _sessionConfigLocal;
			runtimeConfig = _runtimeConfigLocal;
		}

		internal Dispatcher<Message> CreateNetworkMessageDispatcher()
		{
			Dispatcher<Message> dispatcher = new Dispatcher<Message>();
			dispatcher.Bind<SimulationStart>(OnProtocolSimulationStart);
			dispatcher.Bind<Disconnect>(OnProtocolDisconnect);
			dispatcher.Bind<TickChecksumError>(OnProtocolChecksumError);
			dispatcher.Bind<FrameSnapshot>(OnProtocolFrameSnapshot);
			dispatcher.Bind<FrameSnapshotRequest>(OnProtocolFrameSnapshotRequest);
			dispatcher.Bind<TickChecksumErrorFrameDump>(OnTickChecksumErrorFrameDump);
			dispatcher.Bind<RemovePlayerFailed>(OnRemovePlayerFailed);
			dispatcher.Bind<AddPlayerFailed>(OnAddPlayerFailed);
			return dispatcher;
		}

		/// <summary>
		/// <see langword="true" /> if the latest confirmed/verified frame is equals or larger then this.
		/// </summary>
		public bool IsFrameVerified(int frame)
		{
			if (_simulator != null)
			{
				return _simulator.IsFrameVerified(frame);
			}
			return false;
		}

		/// <summary>
		/// <see langword="true" /> if this global PlayerRef is controlled by the local session.
		/// </summary>
		public bool IsLocalPlayer(PlayerRef player)
		{
			return _localPlayerMap.Has(player);
		}

		/// <summary>
		/// Includes command for local prediction, and sends to server (in online games) for confirmation. Fails if this slot is not in use by the local session/client.
		/// Server protects against all forms of spoofing in online games.
		/// </summary>
		public DeterministicCommandSendResult SendCommand(int playerSlot, DeterministicCommand command)
		{
			if (_state == DeterministicSessionState.Running)
			{
				if (IsSpectating)
				{
					LogStream logError = InternalLogStreams.LogError;
					if (logError != null)
					{
						logError.Log("Can't send commands in spectating mode. Add a player to the game first.");
					}
					return DeterministicCommandSendResult.FailedIsSpectating;
				}
				if (_commandSerializer.EncodeCommand(command, out var result))
				{
					_rpcProvider.AddRpc(playerSlot, result, command: true);
					return DeterministicCommandSendResult.Success;
				}
				LogStream logError2 = InternalLogStreams.LogError;
				if (logError2 != null)
				{
					logError2.Log($"Command {command} message is too big");
				}
				return DeterministicCommandSendResult.FailedTooBig;
			}
			LogStream logError3 = InternalLogStreams.LogError;
			if (logError3 != null)
			{
				logError3.Log("Can't send commands when the simulation is not running");
			}
			return DeterministicCommandSendResult.FailedSimulationNotRunning;
		}

		/// <summary>
		/// Previously could be called multiple times to send custom data relative to a player. AddPlayer replaces it, and can only be called once for a specific slot.
		/// </summary>
		[Obsolete("Use AddPlayer()")]
		public void SetPlayerData(int playerSlot, byte[] data)
		{
			AddPlayer(playerSlot, data);
		}

		/// <summary>
		/// Attempts to add a local player slot to have input controlled by the session. Fails is local slot is already used, or if the server/plugin does not have an available PlayerRef.
		/// </summary>
		public void AddPlayer(int playerSlot, byte[] data)
		{
			if (_state == DeterministicSessionState.Running)
			{
				_rpcProvider.AddRpc(playerSlot, data, command: false);
				return;
			}
			LogStream logError = InternalLogStreams.LogError;
			if (logError != null)
			{
				logError.Log("Can't set player data when the simulation is not running");
			}
		}

		/// <summary>
		/// Attempts to remove a local player slot from the session. Fails is local slot is not currently in use.
		/// </summary>
		public void RemovePlayer(int playerSlot)
		{
			if (_state == DeterministicSessionState.Running)
			{
				_network.SendProtocolMessage(new RemovePlayer
				{
					PlayerSlot = playerSlot
				});
				return;
			}
			LogStream logError = InternalLogStreams.LogError;
			if (logError != null)
			{
				logError.Log("Can't remove a player when the simulation is not running");
			}
		}

		/// <summary>
		/// Supports sending one game result during one session per client.
		/// Send this based on timing and simulation data of a verified frame to make results be comparable. 
		/// Data can be any format but the size must be below <see cref="F:Photon.Deterministic.Protocol.GameResult.MaxSize" />.
		/// Zip compress Json for example to reduce the size.
		/// </summary>
		/// <param name="data">Serialized game result</param>
		public void SendGameResult(byte[] data)
		{
			if (data == null || _network == null)
			{
				return;
			}
			if (_state != DeterministicSessionState.Running)
			{
				LogStream logError = InternalLogStreams.LogError;
				if (logError != null)
				{
					logError.Log("Can't send game result when the simulation is not running");
				}
				return;
			}
			if (data.Length > 20480)
			{
				LogStream logWarn = InternalLogStreams.LogWarn;
				if (logWarn != null)
				{
					logWarn.Log($"GameResult size {data.Length} exceeds limit {20480}");
				}
			}
			_network.SendProtocolMessage(new GameResult
			{
				Data = data
			});
		}

		/// <summary>
		/// Sends the Join protocol message to server/plugin. If successful, a start protocol message (and optionally a snapshot) will be received and the session will start to be updated.
		/// Sessions are always joined as spectators (starting on Quantum 3.0), and adding local players that can have input polled for is a dynamic operation done after session start.
		/// </summary>
		public void Join(string id)
		{
			_network.SendProtocolMessage(new StartRequest
			{
				Id = id,
				InitialTick = _initialTick,
				SessionConfig = _sessionConfigLocal,
				RuntimeConfig = _runtimeConfigLocal,
				ProtocolVersion = DeterministicProtocolVersionAttribute.Get(typeof(DeterministicSession).Assembly).Version
			});
		}

		/// <summary>
		/// Legacy method to manually add time to the internal Simulator. Not used anymore by the standard SDK, but available to legacy projects.
		/// </summary>
		public void ApplyTimeOffset(double time)
		{
			_simulator.AccumulateTime(time);
		}

		/// <summary>
		/// Terminates the game/session, Disposing all buffers and objects.
		/// </summary>
		public void Destroy()
		{
			try
			{
				if (_game != null)
				{
					_game.OnDestroy();
					_game = null;
				}
			}
			catch (Exception ex)
			{
				LogStream logException = InternalLogStreams.LogException;
				if (logException != null)
				{
					logException.Log(ex);
				}
			}
			try
			{
				if (_simulator != null)
				{
					_simulator.Destroy();
					_simulator = null;
				}
			}
			catch (Exception ex2)
			{
				LogStream logException2 = InternalLogStreams.LogException;
				if (logException2 != null)
				{
					logException2.Log(ex2);
				}
			}
			try
			{
				if (_network != null)
				{
					_network.Destroy();
					_network = null;
				}
			}
			catch (Exception ex3)
			{
				LogStream logException3 = InternalLogStreams.LogException;
				if (logException3 != null)
				{
					logException3.Log(ex3);
				}
			}
			try
			{
				if (_frameContext != null)
				{
					_frameContext.Dispose();
					_frameContext = null;
				}
			}
			catch (Exception ex4)
			{
				LogStream logException4 = InternalLogStreams.LogException;
				if (logException4 != null)
				{
					logException4.Log(ex4);
				}
			}
			try
			{
				if (Runner != null)
				{
					Runner.Dispose();
					Runner = null;
				}
			}
			catch (Exception ex5)
			{
				LogStream logException5 = InternalLogStreams.LogException;
				if (logException5 != null)
				{
					logException5.Log(ex5);
				}
			}
			if (OnDestroyed != null)
			{
				try
				{
					OnDestroyed(this);
					OnDestroyed = null;
				}
				catch (Exception ex6)
				{
					LogStream logException6 = InternalLogStreams.LogException;
					if (logException6 != null)
					{
						logException6.Log(ex6);
					}
				}
			}
			_state = DeterministicSessionState.Destroyed;
		}

		/// <summary>
		/// Called to move the simulation forward (poll local input, decode confirmed, simulate verified and predicted frames). When passing a delta time, the internal Stopwatch is ignored.
		/// </summary>
		public void Update(double? deltaTime = null)
		{
			UpdateSimulation(deltaTime);
		}

		private double FramesAsSeconds(int frames)
		{
			return (double)frames * DeltaTimeDouble;
		}

		private double MillisecondsAsSeconds(double milliseconds)
		{
			return milliseconds / 1000.0;
		}

		private static double SumList(List<double> list)
		{
			double num = 0.0;
			for (int i = 0; i < list.Count; i++)
			{
				num += list[i];
			}
			return num;
		}

		private void PerformClockAdjustment()
		{
			if (!(_serverTimeReceived > _serverTimeAdjusted) || !IsOnline)
			{
				return;
			}
			_serverTimeAdjusted = _serverTimeReceived;
			_clientRttAvg.Add(_network.RoundTripTime);
			if (_clientRttAvg.Count > ClockAdjustmentAccumulationMax)
			{
				_clientRttAvg.RemoveAt(0);
			}
			double num = FramesAsSeconds(_simulator.FrameCounter) + _simulator.Accumulated;
			double num2 = _serverTimeAdjusted + MillisecondsAsSeconds(SumList(_clientRttAvg) / (double)_clientRttAvg.Count) * 0.5;
			double item = num2 - num;
			_serverTimeAvgDiff.Add(item);
			if (_serverTimeAvgDiff.Count > ClockAdjustmentAccumulationMax)
			{
				_serverTimeAvgDiff.RemoveAt(0);
			}
			if (_serverTimeAvgDiff.Count == ClockAdjustmentAccumulationMax)
			{
				double num3 = SumList(_serverTimeAvgDiff) / (double)_serverTimeAvgDiff.Count;
				double num4 = Math.Abs(num3);
				if (num4 >= (double)_sessionConfig.MinTimeCorrectionFrames * 0.017)
				{
					_simulator.AdjustClock(num3 / (double)_serverTimeAvgDiff.Count);
				}
			}
		}

		private double GetDeltaTime(double? deltaTime)
		{
			double num = 0.0;
			num = ((!deltaTime.HasValue) ? _deltaClock.GetDelta() : deltaTime.Value);
			return num * _timeScale;
		}

		/// <summary>
		/// Resets the session to the specified frame for replay purposes.
		/// </summary>
		/// <param name="frame">The frame to reset the session to.</param>
		public void ResetReplay(DeterministicFrame frame)
		{
			Reset(frame, resetInputFrame: true);
		}

		/// <summary>
		/// Resets the session to the provided frame data and frame number.
		/// </summary>
		/// <param name="frameData">The frame data to reset the session to.</param>
		/// <param name="frameNumber">The frame number associated with the frame data.</param>
		public void ResetReplay(byte[] frameData, int frameNumber)
		{
			Reset(frameData, frameNumber, resetInputFrame: true);
		}

		/// <summary>
		/// Resynchronizes the session to the specified frame.
		/// </summary>
		/// <param name="frame">The frame to reset to.</param>
		public void Resync(DeterministicFrame frame)
		{
			Reset(frame, resetInputFrame: false);
		}

		/// <summary>
		/// Resynchronizes the session based on the provided frame data and frame number.
		/// </summary>
		/// <param name="frameData">The frame data to use for resynchronization.</param>
		/// <param name="frameNumber">The frame number to resynchronize with.</param>
		public void Resync(byte[] frameData, int frameNumber)
		{
			Reset(frameData, frameNumber, resetInputFrame: false);
		}

		private void Reset(byte[] frameData, int frameNumber, bool resetInputFrame)
		{
			_simulator.Reset(frameData, frameNumber, resetInputFrame);
			OnReset();
		}

		private void Reset(DeterministicFrame frame, bool resetInputFrame)
		{
			_simulator.Reset(frame, resetInputFrame);
			OnReset();
		}

		[Conditional("DEBUG")]
		private void CheckFrameIsNotSmallerThanRollbackWindow(int frameNumber)
		{
			_ = _sessionConfig.RollbackWindow - 1;
		}

		private void OnReset()
		{
			_simulator.Paused = false;
			int number = _simulator.FrameVerified.Number;
			_network?.ResetInputState(_simulator.FrameVerified);
			if (_deltaCompressionInput != null && _network != _deltaCompressionInput)
			{
				_deltaCompressionInput?.ResetInputState(_simulator.FrameVerified);
			}
			_game.OnGameResync();
			if (GameMode == DeterministicGameMode.Local && SessionConfig.InputDelayMin > 0)
			{
				InitializeInputDelayMinForDeltaCompression(number);
			}
		}

		internal void InitializeInputDelayMinForDeltaCompression(int startFrame)
		{
			for (int i = 1; i <= SessionConfig.InputDelayMin; i++)
			{
				for (int j = 0; j < PlayerCount; j++)
				{
					Game.OnInputConfirmed(DeterministicFrameInputTemp.Verified(startFrame + i, j, null, null, 0, (DeterministicInputFlags)0));
				}
				_deltaCompressionInput?.OnInputPollingDone(startFrame + i, PlayerCount);
			}
		}

		private void PushReceivedInputsIntoSimulator()
		{
			DeterministicTickInput ev;
			while (_network.InputReceiveQueue.TryPop(out ev))
			{
				DeterministicFrameInputTemp input = DeterministicFrameInputTemp.Verified(ev.Tick, ev.PlayerIndex, ev.Rpc, ev.DataArray, ev.DataLength, ev.Flags);
				_simulator.InsertInput(input);
				try
				{
					_game.OnInputConfirmed(input);
				}
				catch (Exception ex)
				{
					LogStream logException = InternalLogStreams.LogException;
					if (logException != null)
					{
						logException.Log(ex);
					}
				}
				_inputPool.Release(ev);
			}
		}

		private void UpdateSimulation(double? deltaTime)
		{
			if (_simulator != null)
			{
				_simulator.IsStalling = false;
			}
			if (_state != DeterministicSessionState.Destroyed)
			{
				_network?.Poll();
			}
			if (_state == DeterministicSessionState.Running && _checksumError == null && !_simulator.Paused)
			{
				PushReceivedInputsIntoSimulator();
				double deltaTime2 = GetDeltaTime(deltaTime);
				if (_timeProvider != null)
				{
					_timeProvider.Update(deltaTime2);
					Time time = _timeProvider.GetTime();
				}
				UpdateFrameOffset();
				_simulator.AccumulateTime(deltaTime2);
				PerformClockAdjustment();
				_simulator.CheckIsStalling(MaxVerifiedTicksPerUpdate);
				UpdateSimulationInner();
				UpdateStats();
				CallOnUpdateDone();
			}
		}

		private void UpdateSimulationInner()
		{
			try
			{
				_statsUpdateTimer.Restart();
				_game.OnSimulationBegin();
				int frameCounter = _simulator.FrameCounter;
				int num = _simulatorInputOffset - _simulatorInputOffsetPrevious;
				int frame;
				while (_simulator.StepFrameNumber(out frame))
				{
					if (_inputProvider.CanSimulate(frame))
					{
						if (GameMode != DeterministicGameMode.Replay)
						{
							if (num < 0)
							{
								num++;
								continue;
							}
							if (num > 0)
							{
								_simulator.StepFrameNumberDown();
								frame -= num;
								num--;
							}
							frame += _simulatorInputOffset;
						}
						if (IsOnline && _simulator.FramesRemaining - _simulatorInputOffset > _sessionConfig.InputHardTolerance)
						{
							continue;
						}
						List<PlayerRef> list = ((GameMode == DeterministicGameMode.Multiplayer) ? _localPlayerMap.Players : AllPlayers);
						foreach (PlayerRef item3 in list)
						{
							PlayerRef playerRef = item3;
							if (GameMode == DeterministicGameMode.Multiplayer)
							{
								playerRef = _localPlayerMap.PlayerToPlayerSlot[item3];
							}
							_rpcProvider.GetRpc(frame, item3).Deconstruct(out var item, out var item2);
							byte[] array = item;
							bool flag = item2;
							DeterministicFrameInputTemp input = _inputProvider.GetInput(frame, playerRef);
							input.Player = item3;
							if (input.Data == null)
							{
								input.Data = new byte[0];
								input.DataLength = 0;
							}
							if (input.DataLength > 255)
							{
								LogStream logError = InternalLogStreams.LogError;
								if (logError != null)
								{
									logError.Log($"Input byte size of {input.DataLength} is too large, max size is 255.");
								}
								input.Data = new byte[0];
								input.DataLength = 0;
							}
							if (array != null)
							{
								input.Rpc = array;
								if (flag)
								{
									input.Flags |= DeterministicInputFlags.Command;
								}
							}
							_simulator.InsertInput(input);
							if (_network != null)
							{
								_network.QueueLocalInput(input, playerRef);
								_network.SendLocalInputs();
							}
							if (GameMode == DeterministicGameMode.Local)
							{
								_game.OnInputConfirmed(input);
							}
						}
						_deltaCompressionInput?.OnInputPollingDone(frame, PlayerCount);
						continue;
					}
					_simulator.StepFrameNumberDown();
					break;
				}
				if (_simulator.FrameCounter > frameCounter)
				{
					_simulatorInputOffsetPrevious = _simulatorInputOffset;
				}
				_simulator.Simulate(MaxVerifiedTicksPerUpdate);
			}
			finally
			{
				_game.OnSimulationEnd();
				_statsUpdateTimer.Stop();
			}
		}

		private void CallOnUpdateDone()
		{
			try
			{
				_game.OnUpdateDone();
			}
			catch (Exception ex)
			{
				LogStream logException = InternalLogStreams.LogException;
				if (logException != null)
				{
					logException.Log(ex);
				}
			}
		}

		private void UpdateStats()
		{
			_stats.Ping = _network.RoundTripTime;
			_stats.Frame = _simulator.FramePredicted.Number;
			_stats.Offset = _simulatorInputOffset;
			_stats.Predicted = _simulator.FramesPredicted;
			_stats.UpdateTime = _statsUpdateTimer.Elapsed.TotalSeconds;
		}

		private int CalculateInputOffset()
		{
			double num = Math.Max(0.0, (double)(_network.RoundTripTime - _sessionConfigLocal.InputDelayPingStart) * 0.5);
			return _sessionConfigLocal.InputDelayMin + (int)Math.Ceiling(num / (1000.0 / (double)_sessionConfig.UpdateFPS) * _timeScale);
		}

		private void UpdateFrameOffset()
		{
			double totalSeconds = _rttSyncTimer.Elapsed.TotalSeconds;
			if (totalSeconds > _lastRttSync + 0.25)
			{
				_lastRttSync = totalSeconds;
				_network.SendLocalRtt(_network.RoundTripTime);
				if (_timeProvider != null)
				{
					_timeProvider.OnRttUpdated((double)_network.RoundTripTime / 1000.0);
				}
			}
			_simulatorInputOffset = Math.Min(CalculateInputOffset(), _sessionConfigLocal.InputDelayMax);
		}

		private void ChecksumError(DeterministicTickChecksumError error)
		{
			_checksumError = error;
			_simulator.ChecksumError(error);
		}

		internal void OnPlayerAdded(DeterministicFrame frame, int playerSlot, int actorNumber, PlayerRef player, bool invokeCallback)
		{
			if (actorNumber != _network.ActorNumber || !_localPlayerMap.Add(playerSlot, player))
			{
				return;
			}
			if (invokeCallback)
			{
				_game.OnLocalPlayerAddConfirmed(frame, playerSlot, player);
			}
			else
			{
				LogStream logInfo = InternalLogStreams.LogInfo;
				if (logInfo != null)
				{
					logInfo.Log($"Restored local player {player} slot {playerSlot} actor {actorNumber}");
				}
			}
			_simulator?.UpdatePlayerMasks(LocalPlayers);
		}

		internal void OnPlayerRemoved(DeterministicFrame frame, PlayerRef player)
		{
			if (_localPlayerMap.Has(player))
			{
				_game.OnLocalPlayerRemoveConfirmed(frame, _localPlayerMap.SearchSlot(player), player);
				_localPlayerMap.Remove(player);
				_simulator?.UpdatePlayerMasks(LocalPlayers);
			}
		}

		internal void OnDecodeDeltaCompressedInput(DeterministicFrame frame)
		{
			if (SessionConfig.InputDeltaCompression)
			{
				_deltaCompressionInput?.GetRawInput(frame, ref frame.RawInputs);
			}
		}

		internal void OnSendLocalChecksum(int tick, ulong checksum)
		{
			_network.SendLocalChecksum(tick, checksum);
		}

		internal void OnProtocolRttUpdate(int Rtt)
		{
			_maxClientRtt = Rtt;
		}

		internal void OnProtocolSimulationStart(SimulationStart msg)
		{
			_state = DeterministicSessionState.Running;
			if (msg.WaitingForSnapshot)
			{
				LogStream logInfo = InternalLogStreams.LogInfo;
				if (logInfo != null)
				{
					logInfo.Log("Waiting for snapshot. Clock paused.");
				}
				_simulator.Paused = true;
			}
			_reconnect = msg.Reconnect;
			_runtimeConfig = msg.RuntimeConfig;
			_sessionConfig = msg.SessionConfig;
			_deltaClock = new DeltaClock();
			_deltaClock.Start();
			_rttSyncTimer = Stopwatch.StartNew();
			_stats = new DeterministicStats();
			_statsUpdateTimer = Stopwatch.StartNew();
			_lockstep = _sessionConfigLocal.LockstepSimulation || _mode == DeterministicGameMode.Replay;
			if (IsOnline)
			{
				if (_timeProvider != null)
				{
					_timeProvider.Reset(InitialTick, (double)_network.RoundTripTime / 1000.0, msg.ServerTime, 1.0);
				}
				_syncClockTimer = Stopwatch.StartNew();
				double num = (double)_network.RoundTripTime / 1000.0 * 0.5;
				int num2 = ((InitialTick == 0) ? _sessionConfig.RollbackWindow : InitialTick);
				num += msg.ServerTime - (double)num2 * DeltaTimeDouble;
				num += (IsSpectating ? _spectatingOffsetSec : 0.0);
				_simulator.AccumulateTime(num);
			}
			_frameContext = _game.CreateFrameContext();
			_network?.ResetInputState(_sessionConfig.RollbackWindow - 1);
			_simulator.Initialize(_frameContext);
			try
			{
				_game.OnGameStart(_simulator.FrameVerified);
				_simulator.OnGameStart();
			}
			catch (Exception ex)
			{
				LogStream logException = InternalLogStreams.LogException;
				if (logException != null)
				{
					logException.Log(ex);
				}
			}
			if (_initialTick != 0 && _frameData != null)
			{
				LogStream logInfo2 = InternalLogStreams.LogInfo;
				if (logInfo2 != null)
				{
					logInfo2.Log($"Resetting to tick {_initialTick}");
				}
				Reset(_frameData, _initialTick, resetInputFrame: true);
			}
		}

		internal void OnProtocolDisconnect(Disconnect msg)
		{
			_game.OnPluginDisconnect(msg.Reason);
			Destroy();
		}

		internal void OnProtocolChecksumError(TickChecksumError msg)
		{
			_checksumError = msg.Error;
			_simulator.ChecksumError(_checksumError);
			DeterministicFrame verifiedFrame = _simulator.GetVerifiedFrame(_checksumError.Tick);
			if (verifiedFrame != null)
			{
				byte[] array = DeterministicSessionConfig.ToByteArray(SessionConfig);
				byte[] runtimeConfig = RuntimeConfig;
				byte[] array2 = verifiedFrame.Serialize(DeterministicFrameSerializeMode.Serialize);
				byte[] extraErrorFrameDumpData = _simulator.GetExtraErrorFrameDumpData(verifiedFrame);
				byte[] dump = ByteUtils.PackByteBlocks(array, runtimeConfig, array2, extraErrorFrameDumpData);
				TickChecksumErrorFrameDump[] array3 = TickChecksumErrorFrameDump.Encode(verifiedFrame.Number, dump);
				foreach (TickChecksumErrorFrameDump msg2 in array3)
				{
					_network.SendProtocolMessage(msg2);
				}
			}
		}

		internal void OnProtocolFrameSnapshotRequest(FrameSnapshotRequest msg)
		{
			if (_state == DeterministicSessionState.Running)
			{
				SendSnapshot(msg.ReferenceTick);
			}
		}

		internal void OnProtocolFrameSnapshot(FrameSnapshot msg)
		{
			_snapshots.Add(msg);
			if (msg.Last)
			{
				FrameSnapshot[] snapshots = _snapshots.ToArray();
				_snapshots.Clear();
				FrameSnapshot.Decode(snapshots, ref _frameData, ref _initialTick);
				if (_state == DeterministicSessionState.Running)
				{
					Resync(_frameData, _initialTick);
				}
			}
		}

		private void SendSnapshot(int referenceTick)
		{
			FrameSnapshot[] array = FrameSnapshot.Encode(_simulator.FrameVerified.Number, _simulator.FrameVerified.Serialize(DeterministicFrameSerializeMode.Serialize));
			FrameSnapshot[] array2 = array;
			foreach (FrameSnapshot msg in array2)
			{
				_network.SendProtocolMessage(msg);
			}
		}

		internal void OnTickChecksumErrorFrameDump(TickChecksumErrorFrameDump msg)
		{
			if (!_checksumErrorFrameDumps.TryGetValue(msg.ActorId, out var value))
			{
				_checksumErrorFrameDumps.Add(msg.ActorId, value = new ChecksumErrorFrameDump());
			}
			if (value.Frame != msg.Frame)
			{
				value.Frame = msg.Frame;
				value.Blocks = new List<TickChecksumErrorFrameDump>();
			}
			value.Blocks.Add(msg);
			try
			{
				QTuple<bool, byte[]> qTuple = TickChecksumErrorFrameDump.Decode(value.Blocks);
				if (qTuple.Item0)
				{
					byte[][] array = ByteUtils.ReadByteBlocks(qTuple.Item1).ToArray();
					DeterministicSessionConfig sessionConfig = DeterministicSessionConfig.FromByteArray(array[0]);
					_game.OnChecksumErrorFrameDump(msg.ActorId, msg.Frame, sessionConfig, array[1], array[2], array[3]);
				}
			}
			catch (Exception ex)
			{
				LogStream logException = InternalLogStreams.LogException;
				if (logException != null)
				{
					logException.Log(ex);
				}
			}
		}

		private void OnAddPlayerFailed(AddPlayerFailed msg)
		{
			_game.OnLocalPlayerAddFailed(msg.PlayerSlot, msg.Message);
		}

		private void OnRemovePlayerFailed(RemovePlayerFailed msg)
		{
			_game.OnLocalPlayerRemoveFailed(msg.PlayerSlot, msg.Message);
		}

		internal void OnProtocolClockCorrect(double ServerSeconds, double ServerTimeScale)
		{
			if (_state != DeterministicSessionState.Running)
			{
				return;
			}
			if (_timeProvider != null)
			{
				_timeProvider.OnClockSyncMessageReceived(ServerSeconds, ServerTimeScale);
			}
			_timeScale = ServerTimeScale;
			if (_reconnect || (_syncClockTimer != null && _syncClockTimer.Elapsed.TotalSeconds > 1.0))
			{
				if (_syncClockTimer != null)
				{
					_syncClockTimer.Stop();
				}
				double num = (IsSpectating ? _spectatingOffsetSec : 0.0);
				_serverTimeReceived = Math.Max(_serverTimeReceived, ServerSeconds) + num;
			}
		}

		internal void OnInputSetConfirmed(int tick, int length, byte[] data)
		{
			Game.OnInputSetConfirmed(tick, length, data);
		}
	}
}

