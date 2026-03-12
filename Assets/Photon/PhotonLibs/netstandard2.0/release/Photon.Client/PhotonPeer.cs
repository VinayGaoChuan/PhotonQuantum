using System;
using System.Collections.Generic;
using System.Threading;
using Photon.Client.Encryption;

namespace Photon.Client;

/// <summary>
/// Instances of the PhotonPeer class are used to connect to a Photon server and communicate with it.
/// </summary>
/// <remarks>
/// A PhotonPeer instance allows communication with the Photon Server, which in turn distributes messages
/// to other PhotonPeer clients.<para></para>
/// An application can use more than one PhotonPeer instance, which are treated as separate users on the
/// server. Each should have its own listener instance, to separate the operations, callbacks and events.
/// </remarks>
public class PhotonPeer
{
	/// <summary>False if this library build contains C# Socket code. If true, you must set some type as SocketImplementation before connecting.</summary>
	public const bool NoSocket = false;

	/// <summary>True if the library was compiled with DEBUG setting.</summary>
	public const bool DebugBuild = false;

	/// <summary>Version of the Native Encryptor API compiled into this assembly. Defines which PhotonEncryptorPlugin needs to be used.</summary>
	public const int NativeEncryptorApiVersion = 2;

	/// <summary>Target framework this dll was built for.</summary>
	public TargetFrameworks TargetFramework = TargetFrameworks.NetStandard20;

	/// <summary>A simplified identifier for client SDKs. Helps debugging. Only Photon APIs should set this.</summary>
	public byte ClientSdkId = 15;

	private static string clientVersion;

	/// <summary>Global toggle to avoid callbacks from native plugins. Defaults to false, meaning: "callbacks enabled".</summary>
	/// <remarks>Callbacks from native code will fail on some platforms, which is why you can disable them.</remarks>
	public static bool NoNativeCallbacks;

	/// <summary>Can be used to remove/hide the AppId from websocket connect paths.</summary>
	public bool RemoveAppIdFromWebSocketPath;

	/// <summary>Option to enable V3 Init Requests (WIP).</summary>
	internal bool UseInitV3;

	/// <summary>If true and IsAck2Available is also true, sequential acknowledgements will be used between this client and server.</summary>
	/// <remarks>Even if this gets set to true, ACK2 is only used if the connected server also supports this feature.</remarks>
	public bool UseAck2;

	/// <summary>
	/// Enables the client so send the "encrypted" flag on secure connections. Incompatible with Server SDK 4.x.
	/// </summary>
	public bool EnableEncryptedFlag;

	/// <summary>Optional definition of PhotonSocket type per ConnectionProtocol.</summary>
	/// <remarks>
	/// Several platforms have special Socket implementations and slightly different APIs.
	/// Customizing the SocketImplementationConfig helps to accomodate this.
	/// By default, UDP and TCP have socket implementations assigned.
	///
	/// If a native socket plugin is available set the SocketNativeSource class as Type definition here.
	///
	/// You only need to set the SocketImplementationConfig once, after creating a PhotonPeer
	/// and before connecting. If you switch the TransportProtocol, the correct implementation is being used.
	/// </remarks>
	public Dictionary<ConnectionProtocol, Type> SocketImplementationConfig;

	/// <summary>
	/// Sets the level (and amount) of logging for the PhotonPeer.
	/// </summary>
	/// <remarks>
	/// This affects the callbacks to IPhotonPeerListener.DebugReturn.
	/// Default Level: Error.
	/// </remarks>
	public LogLevel LogLevel = LogLevel.Error;

	private bool reuseEventInstance = true;

	private bool useByteArraySlicePoolForEvents;

	private bool wrapIncomingStructs;

	/// <summary>
	/// This debug setting enables a new send-ordering for commands. Defaults to true and commands are sent in the order they are created. Set to false to use Enet ordering.
	/// </summary>
	public bool SendInCreationOrder = true;

	/// <summary>Defines how far ahead the client can go sending commands (on UDP connections), compared to the acknowledged sequence number.</summary>
	/// <remarks>
	/// This avoids spamming the receiver and puts focus on resending commands that are older and are needed on the receiver side to dispatch commands.
	///
	/// When acks arrive, the SendWindow will move forward and newer commands get sent.
	/// This indirectly affects unreliable commands as they are by default sent in creation order.
	/// It queues more on the client side than on the server.
	/// </remarks>
	public int SendWindowSize = 50;

	private byte quickResendAttempts;

	/// <summary>
	/// How often any reliable command can be repeated before it triggers a disconnect (in case there is no acknowledgement). Default: 7.
	/// </summary>
	/// <remarks>
	/// The initial timeout countdown of a command is calculated by the current roundTripTime + 4 * roundTripTimeVariance.
	/// Please note that the timeout span until a command will be resent is not constant, but based on
	/// the roundtrip time at the initial sending, which will be doubled with every failed retry.
	///
	/// DisconnectTimeout and MaxResends are competing settings: either might trigger a disconnect on the
	/// client first, depending on the values and Roundtrip Time.
	/// </remarks>
	public int MaxResends = 7;

	/// <summary>
	/// Caps the initial timing for repeats of reliable commands. In milliseconds. Default: 400ms.
	/// </summary>
	/// <remarks>
	/// Unless acknowledged, reliable commands are repeated initially after: current roundTripTime + 4 * roundTripTimeVariance.
	///
	/// As this value can be very high when there was exceptional lag, InitialResendTimeMax makes sure that commands
	/// get repeated several times before they may trigger a timeout.
	/// </remarks>
	public int InitialResendTimeMax = 400;

	private int disconnectTimeout = 10000;

	private bool crcEnabled;

	/// <summary>
	/// Sets the time between pings being sent automatically. They measure the roundtrip time and keep connections from closing. Default: 1000.
	/// </summary>
	/// <remarks>
	/// For Photon's reliable UDP connections, pings are skipped if any reliable command was sent during the specified PingInterval.
	/// Any reliable command is used to update the RoundTripTime and RoundTripTimeVariance.
	///
	/// When using TCP and WebSockets, the ping is of interest to measure the roundtrip and to keep a connection open, should nothing else
	/// With those two protocols, the ping is used to update the RoundTripTime and RoundTripTimeVariance.
	/// </remarks>
	public int PingInterval = 1000;

	/// <summary>
	/// Gets / sets the number of channels available in UDP connections with Photon.
	/// Photon Channels are only supported for UDP.
	/// The default ChannelCount is 2. Channel IDs start with 0 and 255 is a internal channel.
	/// </summary>
	public byte ChannelCount = 2;

	/// <summary>
	/// Defines the initial size of an internally used StreamBuffer for Tcp.
	/// The StreamBuffer is used to aggregate operation into (less) send calls,
	/// which uses less resources.
	/// </summary>
	/// <remarks>
	/// The size does not limit the buffer and does not affect when outgoing data is actually sent.
	/// </remarks>
	public static int OutgoingStreamBufferSize = 1200;

	private int mtu = 1200;

	/// <summary>Defines if Key Exchange for Encryption is done asynchronously in another thread.</summary>
	public static bool AsyncKeyExchange = false;

	/// <summary>Indicates if sequence numbers should be randomized.</summary>
	internal bool RandomizeSequenceNumbers;

	/// <summary>Initialization array, used to modify the sequence numbers of channels.</summary>
	internal byte[] RandomizedSequenceNumbers;

	private Type payloadEncryptorType;

	/// <summary>PayloadEncryption Secret. Message payloads get encrypted with it individually and on demand.</summary>
	protected internal byte[] PayloadEncryptionSecret;

	private Type encryptorType;

	/// <summary>The datagram encryptor used for the current connection. Applied internally in InitDatagramEncryption.</summary>
	protected internal IPhotonEncryptor Encryptor;

	/// <summary>If set, the TrafficRecorder will be used to capture all UDP traffic.</summary>
	/// <remarks>
	/// If null or not Enabled, the recorder is not being used.
	/// Traffic is only recorded for UDP.
	///
	/// See ITrafficRecorder docs.
	/// </remarks>
	public ITrafficRecorder TrafficRecorder;

	/// <summary>Debug / info value indicating the client worked around a minor problem of WebSockets. See description.</summary>
	/// <remarks>Background:
	/// In some WebSocket implementations (even in browsers), the client logic will miss the first message from a Photon server.
	/// As workaround on WebSockets the PhotonPeer is able to send pings right after connecting. If a ping's response is
	/// received as first message, this sets this value to true and indicates the original problem being present (and worked around).
	/// </remarks>
	public bool PingUsedAsInit;

	/// <summary>Now obsolete and without function. See PhotonPeer.Stats.</summary>
	[Obsolete("Not used anymore.")]
	public bool TrafficStatsEnabled;

	/// <summary>Implements the message-protocol, based on the underlying network protocol (udp, tcp, ws, wss).</summary>
	internal PeerBase peerBase;

	private readonly object sendOutgoingLockObject = new object();

	private readonly object dispatchLockObject = new object();

	private readonly object enqueueLock = new object();

	/// <summary>For the Init-request, we shift the ClientId by one and the last bit signals a "debug" (0) or "release" build (1).</summary>
	protected internal byte ClientSdkIdShifted => (byte)((ClientSdkId << 1) | 1);

	/// <summary>Version of this library as string.</summary>
	public static string Version
	{
		get
		{
			if (string.IsNullOrEmpty(clientVersion))
			{
				clientVersion = $"{Photon.Client.Version.clientVersion[0]}.{Photon.Client.Version.clientVersion[1]}.{Photon.Client.Version.clientVersion[2]}.{Photon.Client.Version.clientVersion[3]}";
			}
			return clientVersion;
		}
	}

	/// <summary>After connecting, this indicates if the server has the ACK2 feature. If the client does not have the feature, this is always false.</summary>
	/// <remarks>Can only be available for UDP connections.</remarks>
	public bool IsAck2Available => false;

	/// <summary>Enables selection of a (Photon-)serialization protocol. Used in Connect methods.</summary>
	/// <remarks>Defaults to SerializationProtocol.GpBinaryV16;</remarks>
	public SerializationProtocol SerializationProtocolType { get; set; }

	/// <summary>
	/// Can be used to read the PhotonSocket implementation at runtime (before connecting).
	/// </summary>
	/// <remarks>
	/// Use the SocketImplementationConfig to define which PhotonSocket is used per ConnectionProtocol.
	/// </remarks>
	public Type SocketImplementation { get; internal set; }

	/// <summary>Provides access to a SocketError code (if available) after a call to OnStatusChanged with a Socket-level exception.</summary>
	/// <remarks>Useful for debugging send / receive error cases.</remarks>
	public int SocketErrorCode
	{
		get
		{
			if (peerBase == null || peerBase.PhotonSocket == null)
			{
				return 0;
			}
			return peerBase.PhotonSocket.SocketErrorCode;
		}
	}

	/// <summary>Sets the level (and amount) of logging for the PhotonPeer.</summary>
	[Obsolete("Use LogLevel instead.")]
	public LogLevel DebugOut
	{
		get
		{
			return LogLevel;
		}
		set
		{
			LogLevel = value;
		}
	}

	/// <summary>
	/// Gets the IPhotonPeerListener of this instance (set in constructor).
	/// Can be used in derived classes for Listener.DebugReturn().
	/// </summary>
	public IPhotonPeerListener Listener { get; protected set; }

	/// <summary>
	/// This is the (low level) state of the connection to the server of a PhotonPeer. Managed internally and read-only.
	/// </summary>
	/// <remarks>
	/// Don't mix this up with the StatusCode provided in IPhotonListener.OnStatusChanged().
	/// Applications should use the StatusCode of OnStatusChanged() to track their state, as
	/// it also covers the higher level initialization between a client and Photon.
	/// </remarks>
	public PeerStateValue PeerState
	{
		get
		{
			if (peerBase.peerConnectionState == ConnectionStateValue.Connected && !peerBase.ApplicationIsInitialized)
			{
				return PeerStateValue.InitializingApplication;
			}
			return (PeerStateValue)peerBase.peerConnectionState;
		}
	}

	/// <summary>
	/// This peer's ID as assigned by the server or 0 if not using UDP. Will be 0xFFFF before the client connects.
	/// </summary>
	/// <remarks>Used for debugging only. This value is not useful in everyday Photon usage.</remarks>
	public string PeerID => peerBase.PeerID;

	/// <summary>
	/// Option to make the PhotonPeer reuse a single EventData instance for all incoming events.
	/// </summary>
	/// <remarks>
	/// This reduces memory garbage.
	/// If enabled, the event provided via OnEvent(EventData photonEvent) is invalid once the callback finished.
	/// That event's content will get modified. Typically, this is not a problem as events are rarely cached.
	///
	/// Changing this value acquires the same lock that DispatchIncomingCommands() uses.
	/// </remarks>
	public bool ReuseEventInstance
	{
		get
		{
			return reuseEventInstance;
		}
		set
		{
			lock (dispatchLockObject)
			{
				reuseEventInstance = value;
				if (!value)
				{
					peerBase.reusableEventData = null;
				}
			}
		}
	}

	/// <summary>
	/// Enables a deserialization optimization for incoming events. Defaults to false.
	/// </summary>
	/// <remarks>
	/// When enabled, byte-arrays in incoming Photon events are deserialized into pooled ByteArraySlice instances (wrappers for byte[]).
	/// This improves the memory footprint for receiving byte-arrays in events.
	///
	/// When used, you have to release the (pooled) ByteArraySlice instances.
	///
	/// Adjust your handling of EventData accordingly:
	///
	/// The ByteArraySlice.Buffer will usually be bigger than the send/received byte-array.
	/// Check the ByteArraySlice.Count and read only the actually received bytes.
	/// The Buffer is reused and not cleared. The Offset will be 0 for incoming events.
	///
	/// Important:
	/// While the peer will acquire the ByteArraySlice and passes it to OnEvent, the game code has to call ByteArraySlice.Release()
	/// when the slice is no longer needed.
	///
	/// Send either byte[], ArraySegment or use the ByteArraySlicePool to acquire ByteArraySlices to send.
	/// </remarks>
	public bool UseByteArraySlicePoolForEvents
	{
		get
		{
			return useByteArraySlicePoolForEvents;
		}
		set
		{
			useByteArraySlicePoolForEvents = value;
		}
	}

	/// <summary>
	/// Incoming struct types are wrapped in a pooled IWrapperStruct, rather than being cast to object. This eliminated allocations and garbage collection from boxing,
	/// however object that are wrapped structs will need to be cast to WrapperStruct&lt;T&gt; and their values extracted with (obj as WrapperStruct&lt;T&gt;).Value.
	/// </summary>
	public bool WrapIncomingStructs
	{
		get
		{
			return wrapIncomingStructs;
		}
		set
		{
			wrapIncomingStructs = value;
		}
	}

	/// <summary>Instance of a ByteArraySlicePool. UseByteArraySlicePoolForEvents defines if this PhotonPeer is using the pool for deserialization of byte[] in Photon events.</summary>
	/// <remarks>
	/// ByteArraySlice is a serializable datatype of the Photon .Net client library.
	/// It helps avoid allocations by being pooled and (optionally) used in incoming Photon events (see: UseByteArraySlicePoolForEvents).
	///
	/// You can also use the pool to acquire ByteArraySlice instances for serialization.
	/// RaiseEvent will auto-release all ByteArraySlice instances passed in.
	/// </remarks>
	public ByteArraySlicePool ByteArraySlicePool => peerBase.SerializationProtocol.ByteArraySlicePool;

	/// <summary>Provides access to the internally used Pool of StreamBuffer instances, which are used to handle messages (which defined this name).</summary>
	/// <remarks>
	/// The MessageBufferPool is a Pool&lt;StreamBuffer&gt;.
	/// It is thread safe (locks) and static.
	/// The pool will reset each StreamBuffer passed to Release(). See the Pool reference docs.
	/// </remarks>
	public static Pool<StreamBuffer> MessageBufferPool => PeerBase.MessageBufferPool;

	/// <summary>Obsolete. Use SendWindowSize instead.</summary>
	[Obsolete("Use SendWindowSize instead.")]
	public int SequenceDeltaLimitSends
	{
		get
		{
			return SendWindowSize;
		}
		set
		{
			SendWindowSize = value;
		}
	}

	/// <summary>
	/// Up to 4 resend attempts for a reliable command can be done in quick succession (after RTT+4*Variance).
	/// </summary>
	/// <remarks>
	/// By default 0. Any later resend attempt will then double the time before the next resend.
	/// Max value = 4;
	/// Make sure to adjust MaxResends to a slightly higher value, as more repeats will get done.
	/// </remarks>
	public byte QuickResendAttempts
	{
		get
		{
			return quickResendAttempts;
		}
		set
		{
			quickResendAttempts = value;
			if (quickResendAttempts > 4)
			{
				quickResendAttempts = 4;
			}
		}
	}

	/// <summary>How often any reliable command can be repeated before it triggers a disconnect (in case there is no acknowledgement). Default: 7.</summary>
	[Obsolete("Use MaxResends instead.")]
	public int SentCountAllowance
	{
		get
		{
			return MaxResends;
		}
		set
		{
			MaxResends = value;
		}
	}

	/// <summary>
	/// Time in milliseconds before any sent reliable command triggers a timeout disconnect, unless acknowledged by the receiver. Default: 10000.
	/// </summary>
	/// <remarks>
	/// DisconnectTimeout is not an exact value for a timeout. The exact timing of the timeout depends on the frequency
	/// of Service() calls and the roundtrip time. Commands sent with long roundtrip-times and variance are checked less
	/// often for re-sending.
	///
	/// DisconnectTimeout and MaxResends are competing settings: either might trigger a disconnect on the
	/// client first, depending on the values and Roundtrip Time.
	///
	/// Default: 10000 ms.
	/// Setting a negative value will apply the default timeout.
	/// </remarks>
	public int DisconnectTimeout
	{
		get
		{
			return disconnectTimeout;
		}
		set
		{
			if (value < 0)
			{
				disconnectTimeout = 10000;
			}
			disconnectTimeout = value;
		}
	}

	/// <summary>
	/// While not connected, this controls if the next connection(s) should use a per-package CRC checksum.
	/// </summary>
	/// <remarks>
	/// While turned on, the client and server will add a CRC checksum to every sent package.
	/// The checksum enables both sides to detect and ignore packages that were corrupted during transfer.
	/// Corrupted packages have the same impact as lost packages: They require a re-send, adding a delay
	/// and could lead to timeouts.
	///
	/// Building the checksum has a low processing overhead but increases integrity of sent and received data.
	/// Packages discarded due to failed CRC checks are counted in PhotonPeer.PacketLossByCrc.
	/// </remarks>
	public bool CrcEnabled
	{
		get
		{
			return crcEnabled;
		}
		set
		{
			if (crcEnabled != value)
			{
				if (peerBase.peerConnectionState != ConnectionStateValue.Disconnected)
				{
					throw new Exception("CrcEnabled can only be set while disconnected.");
				}
				crcEnabled = value;
			}
		}
	}

	/// <summary>Sets the time between pings being sent automatically. They measure the roundtrip time and keep connections from closing. Default: 1000.</summary>
	[Obsolete("Use PingInterval instead.")]
	public int TimePingInterval
	{
		get
		{
			return PingInterval;
		}
		set
		{
			PingInterval = value;
		}
	}

	/// <summary>
	/// The server address which was used in PhotonPeer.Connect() or null (before Connect() was called).
	/// </summary>
	public string ServerAddress => peerBase.ServerAddress;

	/// <summary>Contains the IP address of the previously resolved ServerAddress (or empty, if address wasn't resolved with the internal methods).</summary>
	public string ServerIpAddress
	{
		get
		{
			if (peerBase != null && peerBase.PhotonSocket != null)
			{
				return peerBase.PhotonSocket.ServerIpAddress;
			}
			return string.Empty;
		}
	}

	/// <summary>The protocol this peer is currently connected/connecting with (or 0).</summary>
	public ConnectionProtocol UsedProtocol => peerBase.usedTransportProtocol;

	/// <summary>This is the transport protocol to be used for next connect (see remarks).</summary>
	/// <remarks>The TransportProtocol can be changed anytime but it will not change the
	/// currently active connection. Instead, TransportProtocol will be applied on next Connect.
	/// </remarks>
	public ConnectionProtocol TransportProtocol { get; set; }

	/// <summary>
	/// Gets or sets the network simulation "enabled" setting.
	/// Changing this value also locks this peer's sending and when setting false,
	/// the internally used queues are executed (so setting to false can take some cycles).
	/// </summary>
	public virtual bool IsSimulationEnabled
	{
		get
		{
			return NetworkSimulationSettings.IsSimulationEnabled;
		}
		set
		{
			if (value == NetworkSimulationSettings.IsSimulationEnabled)
			{
				return;
			}
			lock (sendOutgoingLockObject)
			{
				NetworkSimulationSettings.IsSimulationEnabled = value;
			}
		}
	}

	/// <summary>
	/// Gets the settings for built-in Network Simulation for this peer instance
	/// while IsSimulationEnabled will enable or disable them.
	/// Once obtained, the settings can be modified by changing the properties.
	/// </summary>
	public NetworkSimulationSet NetworkSimulationSettings => peerBase.NetworkSimulationSettings;

	/// <summary>
	/// The Maximum Transfer Unit (MTU) defines the (network-level) packet-content size that is
	/// guaranteed to arrive at the server in one piece. The Photon Protocol uses this
	/// size to split larger data into packets and for receive-buffers of packets.
	/// </summary>
	/// <remarks>
	/// This value affects the Packet-content. The resulting UDP packages will have additional
	/// headers that also count against the package size (so it's bigger than this limit in the end)
	/// Setting this value while being connected is not allowed and will throw an Exception.
	/// Minimum is 576. Huge values won't speed up connections in most cases!
	/// </remarks>
	public int MaximumTransferUnit
	{
		get
		{
			return mtu;
		}
		set
		{
			if (PeerState != PeerStateValue.Disconnected)
			{
				throw new Exception("MaximumTransferUnit is only settable while disconnected. State: " + PeerState);
			}
			if (value < 576)
			{
				value = 576;
			}
			mtu = value;
		}
	}

	/// <summary>
	/// This property is set internally, when OpExchangeKeysForEncryption successfully finished.
	/// While it's true, encryption can be used for operations.
	/// </summary>
	public bool IsEncryptionAvailable => peerBase.isEncryptionAvailable;

	/// <summary>Setter for the Payload Encryptor type. Used for next connection.</summary>
	/// <remarks>
	/// If null, the PhotonPeer will create a DiffieHellmanCryptoProvider, which is the default.
	/// This is only needed in rare cases, where using native payload encryption makes sense.
	///
	/// Get in touch about this, if you got questions: developer@photonengine.com
	/// </remarks>
	public Type PayloadEncryptorType
	{
		get
		{
			return payloadEncryptorType;
		}
		set
		{
			if (value == null || typeof(ICryptoProvider).IsAssignableFrom(value))
			{
				payloadEncryptorType = value;
			}
			else if ((int)LogLevel >= 1)
			{
				Listener.DebugReturn(LogLevel.Error, "Failed to set PayloadEncryptorType. Must implement ICryptoProvider.");
			}
		}
	}

	/// <summary>Setter for the Datagram Encryptor type. Used at next connect.</summary>
	/// <remarks>
	/// If null, the PhotonPeer will create a default datagram encryptor instance.
	/// </remarks>
	public Type EncryptorType
	{
		get
		{
			return encryptorType;
		}
		set
		{
			if (value == null || typeof(IPhotonEncryptor).IsAssignableFrom(value))
			{
				encryptorType = value;
			}
			else if ((int)LogLevel >= 1)
			{
				Listener.DebugReturn(LogLevel.Error, "Failed to set EncryptorType. Must implement IPhotonEncryptor.");
			}
		}
	}

	/// <summary>
	/// Approximated Environment.TickCount value of server (while connected).
	/// </summary>
	/// <remarks>
	/// UDP: The server's timestamp is automatically fetched after connecting (once). This is done
	/// internally by a command which is acknowledged immediately by the server.
	/// TCP: The server's timestamp fetched with each ping but set only after connecting (once).
	///
	/// The approximation will be off by +/- 10ms in most cases. Per peer/client and connection, the
	/// offset will be constant (unless FetchServerTimestamp() is used). A constant offset should be
	/// better to adjust for. Unfortunately there is no way to find out how much the local value
	/// differs from the original.
	///
	/// The approximation adds RoundtripTime / 2 and uses this.LocalTimeInMilliSeconds to calculate
	/// in-between values (this property returns a new value per tick).
	///
	/// The value sent by Photon equals Environment.TickCount in the logic layer.
	/// </remarks>
	/// <value>
	/// 0 until connected.
	/// While connected, the value is an approximation of the server's current timestamp.
	/// </value>
	public int ServerTimeInMilliseconds
	{
		get
		{
			if (!peerBase.serverTimeOffsetIsAvailable)
			{
				return 0;
			}
			return peerBase.serverTimeOffset + ConnectionTime;
		}
	}

	/// <summary>
	/// Debugging option to tell the Photon Server to log all datagrams.
	/// </summary>
	public bool EnableServerTracing { get; set; }

	/// <summary>The internally used per PhotonPeer time value.</summary>
	/// <remarks>
	/// Returns the integer part of a Stopwatch ElapsedMilliseconds value.
	/// If the PhotonPeer runs continuously the ClientTime will increment from zero to Int32..::.MaxValue
	/// for approximately 24.9 days, then jump to Int32..::.MinValue (a negative number), then increment
	/// back to zero during the next 24.9 days.
	///
	/// It is recommended to use this int only for delta times, to avoid handling the overflow.
	/// </remarks>
	public int ConnectionTime => peerBase.timeInt;

	/// <summary>Time until a reliable command is acknowledged by the server.</summary>
	/// <remarks>
	/// The value measures network latency and for UDP it includes the server's ACK-delay (setting in config).
	/// In TCP, there is no ACK-delay, so the value is slightly lower (if you use default settings for Photon).
	///
	/// RoundTripTime is updated constantly. Every reliable command will contribute a fraction to this value.
	///
	/// This is also the approximate time until a raised event reaches another client or until an operation
	/// result is available.
	/// </remarks>
	[Obsolete("Use Stats.RoundtripTime instead.")]
	public int RoundTripTime => peerBase.roundTripTime;

	/// <summary>
	/// Changes of the roundtriptime as variance value. Gives a hint about how much the time is changing.
	/// </summary>
	[Obsolete("Use Stats.RoundTripTimeVariance instead.")]
	public int RoundTripTimeVariance => peerBase.roundTripTimeVariance;

	/// <summary>The last measured roundtrip time for this connection.</summary>
	[Obsolete("Use Stats.LastRoundTripTime instead.")]
	public int LastRoundTripTime => peerBase.lastRoundTripTime;

	/// <summary>
	/// Gets count of all bytes coming in (including headers, excluding UDP/TCP overhead)
	/// </summary>
	public long BytesIn => Stats.BytesIn;

	/// <summary>
	/// Gets count of all bytes going out (including headers, excluding UDP/TCP overhead)
	/// </summary>
	public long BytesOut => Stats.BytesOut;

	/// <summary>
	/// Gets the size of the dispatched event or operation-result in bytes.
	/// This value is set before OnEvent() or OnOperationResponse() is called (within DispatchIncomingCommands()).
	/// </summary>
	/// <remarks>
	/// Get this value directly in OnEvent() or OnOperationResponse(). Example:
	/// void OnEvent(...) {
	///   int eventSizeInBytes = this.peer.ByteCountCurrentDispatch;
	///   //...
	///
	/// void OnOperationResponse(...) {
	///   int resultSizeInBytes = this.peer.ByteCountCurrentDispatch;
	///   //...
	/// </remarks>
	public int ByteCountCurrentDispatch => peerBase.ByteCountCurrentDispatch;

	/// <summary>Returns the debug string of the event or operation-response currently being dispatched or string. Empty if none.</summary>
	/// <remarks>In a release build of the lib, this will always be empty.</remarks>
	public string CommandInfoCurrentDispatch
	{
		get
		{
			if (peerBase.CommandInCurrentDispatch == null)
			{
				return string.Empty;
			}
			return peerBase.CommandInCurrentDispatch.ToString();
		}
	}

	/// <summary>
	/// Gets the size of the last serialized operation call in bytes.
	/// The value includes all headers for this single operation but excludes those of UDP, Enet Package Headers and TCP.
	/// </summary>
	/// <remarks>
	/// Get this value immediately after calling an operation.
	/// Example:
	///
	/// this.client.OpJoinRoom("myroom");
	/// int opJoinByteCount = this.client.ByteCountLastOperation;
	/// </remarks>
	public int ByteCountLastOperation => peerBase.ByteCountLastOperation;

	/// <summary>
	/// Count of packages dropped due to failed CRC checks for this connection.
	/// </summary>
	/// <see cref="P:Photon.Client.PhotonPeer.CrcEnabled" />
	public int PacketLossByCrc => peerBase.packetLossByCrc;

	/// <summary>
	/// Count of packages dropped due to wrong challenge for this connection.
	/// </summary>
	public int PacketLossByChallenge => peerBase.packetLossByChallenge;

	/// <summary>
	/// Count of commands that got repeated (due to local repeat-timing before an ACK was received).
	/// </summary>
	[Obsolete("Use Stats.UdpReliableCommandsResent instead.")]
	public int ResentReliableCommands => Stats.UdpReliableCommandsResent;

	/// <summary>The last ConnectionTime value, when some ACKs were sent out by this client.</summary>
	[Obsolete("Use Stats.LastSendAckTimestamp instead.")]
	public int LastSendAckTime => Stats.LastSendAckTimestamp;

	/// <summary>Milliseconds since the last SendAcksOnly call.</summary>
	public int LastSendAckDeltaTime => peerBase.timeInt - Stats.LastSendAckTimestamp;

	/// <summary>The last ConnectionTime value, when SendOutgoingCommands actually checked outgoing queues to send them. Must be connected.</summary>
	[Obsolete("Use Stats.LastSendOutgoingTimestamp instead.")]
	public int LastSendOutgoingTime => Stats.LastSendOutgoingTimestamp;

	/// <summary>Milliseconds since the last SendOutgoing call.</summary>
	public int LastSendOutgoingDeltaTime => peerBase.timeInt - Stats.LastSendOutgoingTimestamp;

	/// <summary>Milliseconds since the last SendOutgoing call.</summary>
	[Obsolete("Use Stats.LastReceiveTimestamp instead.")]
	public int TimestampOfLastSocketReceive => Stats.LastReceiveTimestamp;

	/// <summary>Milliseconds since the last received message or datagram (anything received by the socket).</summary>
	public int LastReceiveDeltaTime => peerBase.timeInt - Stats.LastReceiveTimestamp;

	/// <summary>Measures the maximum milliseconds spent in PhotonSocket.Send().</summary>
	public int LongestSendCall
	{
		get
		{
			return peerBase.longestSendCall;
		}
		set
		{
			peerBase.longestSendCall = value;
		}
	}

	/// <summary>Count of unreliable commands being discarded in case this client already dispatched a command that was newer (higher sequence number).</summary>
	public int CountDiscarded { get; set; }

	/// <summary>Set per dispatch in DispatchIncomingCommands to: commandUnreliableSequenceNumber - channel.incomingUnreliableSequenceNumber. Indicates how big the (sequence)gap is, compared to the last dispatched unreliable command.</summary>
	public int DeltaUnreliableNumber { get; set; }

	/// <summary>
	/// Count of all currently received but not-yet-Dispatched reliable commands
	/// (events and operation results) from all channels.
	/// </summary>
	public int QueuedIncomingCommands => peerBase.QueuedIncomingCommandsCount;

	/// <summary>
	/// Count of all commands currently queued as outgoing, including all channels and reliable, unreliable.
	/// </summary>
	public int QueuedOutgoingCommands => peerBase.QueuedOutgoingCommandsCount;

	/// <summary>Count of reliable commands "in flight". They are sent but not yet acknowledged by the receiving peer / server.</summary>
	[Obsolete("Use Stats.UdpReliableCommandsInFlight.")]
	public int ReliableCommandsInFlight => Stats.UdpReliableCommandsInFlight;

	/// <summary>Count of reliable commands "in flight". They are sent but not yet acknowledged by the receiving peer / server.</summary>
	[Obsolete("Use Stats.UdpReliableCommandsInFlight instead. Check reference doc to make sure this is what you want to check.")]
	public int SentReliableCommandsCount => Stats.UdpReliableCommandsInFlight;

	/// <summary>Provides stats of incoming and outgoing traffic for the current connection. Replaced with new instance when connecting.</summary>
	public TrafficStats Stats { get; internal set; }

	/// <summary>
	/// Returns a string of the most interesting connection statistics.
	/// When you have issues on the client side, these might contain hints about the issue's cause.
	/// </summary>
	/// <param name="all">If true, Incoming and Outgoing low-level stats are included in the string.</param>
	/// <returns>Stats as string.</returns>
	public string VitalStatsToString(bool all = true)
	{
		float duration = (float)peerBase.timeInt / 1000f;
		long bytesTotal = BytesIn + BytesOut;
		int bytesPerSec = ((!(duration <= 0f)) ? ((int)((float)bytesTotal / duration)) : 0);
		string essentials = $"Stats duration: {duration:F2} sec. rtt(var): {Stats.RoundtripTime}({Stats.RoundtripTimeVariance})ms. Bytes: {bytesTotal}. Average: {bytesPerSec:N0} bytes/sec. bestRoundtripTimeout: {peerBase.bestRoundtripTimeout} throttledBySendWindow: {peerBase.throttledBySendWindow}.";
		if (!all)
		{
			return essentials;
		}
		return $"{essentials}\n{Stats}";
	}

	/// <summary>Creates a new PhotonPeer with specified transport protocol (without a IPhotonPeerListener).</summary>
	/// <remarks>Make sure to set the Listener, before using the peer.</remarks>
	public PhotonPeer(ConnectionProtocol protocolType)
	{
		TransportProtocol = protocolType;
		SocketImplementationConfig = new Dictionary<ConnectionProtocol, Type>(5);
		SocketImplementationConfig[ConnectionProtocol.Udp] = typeof(SocketUdp);
		SocketImplementationConfig[ConnectionProtocol.Tcp] = typeof(SocketTcp);
		SocketImplementationConfig[ConnectionProtocol.WebSocket] = typeof(PhotonClientWebSocket);
		SocketImplementationConfig[ConnectionProtocol.WebSocketSecure] = typeof(PhotonClientWebSocket);
		CreatePeerBase();
		Stats = new TrafficStats(peerBase.watch);
	}

	/// <summary>
	/// Creates a new PhotonPeer instance to communicate with Photon and selects the transport protocol. We recommend UDP.
	/// </summary>
	/// <param name="listener">a IPhotonPeerListener implementation</param>
	/// <param name="protocolType">Protocol to use to connect to Photon.</param>
	public PhotonPeer(IPhotonPeerListener listener, ConnectionProtocol protocolType)
		: this(protocolType)
	{
		Listener = listener;
	}

	/// <summary>Obsolete. Use new overload with updated parameter order.</summary>
	[Obsolete("Use new overload with updated parameter order.")]
	public virtual bool Connect(string serverAddress, string proxyServerAddress, string appId, object photonToken, object customInitData = null)
	{
		return Connect(serverAddress, appId, photonToken, customInitData, proxyServerAddress);
	}

	/// <summary>
	/// Starts connecting to the given Photon server. Non-blocking.
	/// </summary>
	/// <remarks>
	/// Connecting to the Photon server is done asynchronous.
	/// Unless an error happens right away (and this returns false), wait for the call of IPhotonPeerListener.OnStatusChanged.
	/// </remarks>
	/// <param name="serverAddress">
	///     Address of a Photon server as IP:port or hostname. WebSocket connections must contain a scheme (ws:// or wss://).
	/// </param>
	/// <param name="appId">
	///     The ID of the app to use. Typically this is a guid (for the Photon Cloud). Max 32 characters.
	/// </param>
	/// <param name="photonToken">
	///     Optional Photon token data to be used by server during peer creation.
	///     If used for authentication, the server is able to reject a client without creating a peer.
	///     Must be of type string or byte[] (as provided by server).
	/// </param>
	/// <param name="customInitData">Custom data to send to the server in the Init request. Might be used to identify a client / user.</param>
	/// <param name="proxyServerAddress">
	///     Optional address of a proxy server. Only used by WebSocket connections. Set null to use none.
	/// </param>
	/// <returns>
	/// True if a connection attempt will be made. False if some error could be detected early-on.
	/// </returns>
	public virtual bool Connect(string serverAddress, string appId, object photonToken, object customInitData = null, string proxyServerAddress = null)
	{
		lock (dispatchLockObject)
		{
			lock (sendOutgoingLockObject)
			{
				if (peerBase != null && peerBase.peerConnectionState != ConnectionStateValue.Disconnected)
				{
					if ((int)LogLevel >= 2)
					{
						Listener.DebugReturn(LogLevel.Warning, $"Connect() failed. Peer is not Disconnected. peerConnectionState: {peerBase.peerConnectionState}.");
					}
					return false;
				}
				if (photonToken == null)
				{
					Encryptor = null;
					RandomizedSequenceNumbers = null;
					RandomizeSequenceNumbers = false;
				}
				CreatePeerBase();
				peerBase.Reset();
				Stats = new TrafficStats(peerBase.watch);
				PingUsedAsInit = false;
				peerBase.ServerAddress = serverAddress;
				peerBase.ProxyServerAddress = proxyServerAddress;
				peerBase.AppId = appId;
				peerBase.PhotonToken = photonToken;
				peerBase.CustomInitData = customInitData;
				Type socketType = null;
				if (!SocketImplementationConfig.TryGetValue(TransportProtocol, out socketType))
				{
					peerBase.EnqueueDebugReturn(LogLevel.Error, $"Connect() failed. SocketImplementationConfig is not set for protocol {TransportProtocol}: {SupportClass.DictionaryToString(SocketImplementationConfig, includeTypes: false)}");
					return false;
				}
				SocketImplementation = socketType;
				try
				{
					peerBase.PhotonSocket = (PhotonSocket)Activator.CreateInstance(SocketImplementation, peerBase);
				}
				catch (Exception arg)
				{
					if ((int)LogLevel >= 1)
					{
						Listener.DebugReturn(LogLevel.Error, $"Connect() failed to create a PhotonSocket instance for {TransportProtocol}. SocketImplementationConfig: {SupportClass.DictionaryToString(SocketImplementationConfig, includeTypes: false)} Exception: {arg}");
					}
					return false;
				}
				return peerBase.Connect(serverAddress, proxyServerAddress, appId, photonToken);
			}
		}
	}

	private void CreatePeerBase()
	{
		ConnectionProtocol transportProtocol = TransportProtocol;
		if (transportProtocol == ConnectionProtocol.Tcp || transportProtocol - 4 <= ConnectionProtocol.Tcp)
		{
			TPeer existingPeer = peerBase as TPeer;
			if (existingPeer == null)
			{
				existingPeer = (TPeer)(peerBase = new TPeer());
			}
			existingPeer.DoFraming = TransportProtocol == ConnectionProtocol.Tcp;
		}
		else if (!(peerBase is EnetPeer))
		{
			peerBase = new EnetPeer();
		}
		peerBase.photonPeer = this;
		peerBase.usedTransportProtocol = TransportProtocol;
	}

	/// <summary>
	/// This method initiates a mutual disconnect between this client and the server.
	/// </summary>
	/// <remarks>
	/// Calling this method does not immediately close a connection. Disconnect lets the server
	/// know that this client is no longer listening. For the server, this is a much faster way
	/// to detect that the client is gone, but it requires the client to send a few final messages.
	///
	/// On completion, OnStatusChanged is called with the StatusCode.Disconnect.
	///
	/// Any resulting callbacks are queued and need to be dispatched (call Peer.DispatchIncomingCommands()).
	/// This avoids issues when Disconnect() in Unity when Disconnect gets called off main-thread.
	///
	/// If the client is disconnected already or the connection thread is stopped, then there is no callback.
	///
	/// The default server logic will leave any joined game and trigger the respective event.
	/// </remarks>
	public virtual void Disconnect()
	{
		lock (dispatchLockObject)
		{
			lock (sendOutgoingLockObject)
			{
				peerBase.Disconnect();
			}
		}
	}

	/// <summary>Debug dll only: Simulates a local timeout disconnect without delay. Useful for debugging.</summary>
	/// <remarks>
	/// To simulate specific disconnect and reconnect scenarios, this method can be used to simulate
	/// a client side timeout-disconnect immediately - as if acknowledgements were missing for a while already.
	///
	/// The PhotonPeer will not signal a disconnect to the server.
	/// For the duration of the server's timeout time, this peer will be considered connected.
	///
	/// Continue to call DispatchIncomingCommands() to get the associated callbacks - more than one state change is possible.
	///
	/// On a release build of the PhotonClient dll, this method will not do anything!
	/// </remarks>
	public virtual void SimulateTimeoutDisconnect()
	{
	}

	/// <summary>
	/// This will fetch the server's timestamp and update the approximation for property ServerTimeInMilliseconds.
	/// </summary>
	/// <remarks>
	/// The server time approximation will NOT become more accurate by repeated calls. Accuracy currently depends
	/// on a single roundtrip which is done as fast as possible.
	///
	/// The command used for this is immediately acknowledged by the server. This makes sure the roundtrip time is
	/// low and the timestamp + rountriptime / 2 is close to the original value.
	/// </remarks>
	public virtual void FetchServerTimestamp()
	{
		peerBase.FetchServerTimestamp();
	}

	/// <summary>
	/// This method creates a public key for this client and exchanges it with the server.
	/// </summary>
	/// <remarks>
	/// Encryption is not instantly available but calls OnStatusChanged when it finishes.
	/// Check for StatusCode EncryptionEstablished and EncryptionFailedToEstablish.
	///
	/// Calling this method sets IsEncryptionAvailable to false.
	/// This method must be called before the "encrypt" parameter of OpCustom can be used.
	/// </remarks>
	/// <returns>If operation could be enqueued for sending</returns>
	public bool EstablishEncryption()
	{
		if (AsyncKeyExchange)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
				peerBase.ExchangeKeysForEncryption(sendOutgoingLockObject);
			});
			return true;
		}
		return peerBase.ExchangeKeysForEncryption(sendOutgoingLockObject);
	}

	/// <summary>
	/// Initializes Datagram Encryption. Optionally, the EncryptorType is being used, if set.
	/// </summary>
	/// <param name="encryptionSecret">Secret used to cipher udp packets.</param>
	/// <param name="hmacSecret">Secret used for authentication of udp packets.</param>
	/// <param name="randomizedSequenceNumbers">Sets if enet Sequence Numbers will be randomized or not. Preferably should be true.</param>
	/// <param name="chainingModeGCM">Sets if the chaining mode should be CBC (false, default) or GCM (true). GCM mode is only available with a native encryption plugin.</param>
	[Obsolete("Use InitDatagramEncryption(byte[] encryptionSecret, byte[] hmacSecret).")]
	public bool InitDatagramEncryption(byte[] encryptionSecret, byte[] hmacSecret, bool randomizedSequenceNumbers, bool chainingModeGCM)
	{
		if (!randomizedSequenceNumbers || !chainingModeGCM)
		{
			if ((int)LogLevel >= 1)
			{
				Listener.DebugReturn(LogLevel.Error, "InitDatagramEncryption now requires randomizedSequenceNumbers and chainingModeGCM being true.");
			}
			return false;
		}
		return InitDatagramEncryption(encryptionSecret, hmacSecret);
	}

	/// <summary>
	/// Sets up the Datagram Encryption secrets for this connection. These should be exchanged on a WSS connection for safety.
	/// </summary>
	/// <param name="encryptionSecret">Secret encryption key. Must be non null.</param>
	/// <param name="hmacSecret">Hmac init. May be null.</param>
	/// <returns>True if Datagram Encryption could be setup</returns>
	/// <exception cref="T:System.NullReferenceException">If the EncryptorType is not set properly or failed initialization.</exception>
	public bool InitDatagramEncryption(byte[] encryptionSecret, byte[] hmacSecret)
	{
		if (encryptionSecret == null)
		{
			if ((int)LogLevel >= 1)
			{
				Listener.DebugReturn(LogLevel.Error, "InitDatagramEncryption requires non null encryptionSecret.");
			}
			return false;
		}
		if (EncryptorType != null)
		{
			try
			{
				Encryptor = (IPhotonEncryptor)Activator.CreateInstance(EncryptorType);
				if (Encryptor == null)
				{
					Listener.DebugReturn(LogLevel.Warning, "Datagram encryptor creation by type failed, Activator.CreateInstance() returned null");
				}
			}
			catch (Exception arg)
			{
				if ((int)LogLevel >= 2)
				{
					Listener.DebugReturn(LogLevel.Warning, $"Datagram encryptor creation by type failed. Caught: {arg}");
				}
			}
		}
		if (Encryptor == null)
		{
			throw new NullReferenceException("Can not init datagram encryption. No suitable encryptor found or provided.");
		}
		if ((int)LogLevel >= 3)
		{
			Listener.DebugReturn(LogLevel.Info, $"Datagram encryptor of type {Encryptor.GetType()} created. Api version: {2}");
		}
		Encryptor.Init(encryptionSecret, hmacSecret, null, chainingModeGCM: true, mtu);
		RandomizedSequenceNumbers = encryptionSecret;
		RandomizeSequenceNumbers = true;
		return true;
	}

	/// <summary>
	/// Photon's Payload Encryption secret may be set by a response from the server.
	/// </summary>
	/// <param name="secret">The secret in form of a byte[].</param>
	public void InitPayloadEncryption(byte[] secret)
	{
		PayloadEncryptionSecret = secret;
	}

	/// <summary>
	/// This method excutes DispatchIncomingCommands and SendOutgoingCommands in your application Thread-context.
	/// </summary>
	/// <remarks>
	/// The Photon client libraries are designed to fit easily into a game or application. The application
	/// is in control of the context (thread) in which incoming events and responses are executed and has
	/// full control of the creation of UDP/TCP packages.
	///
	/// Sending packages and dispatching received messages are two separate tasks. Service combines them
	/// into one method at the cost of control. It calls DispatchIncomingCommands and SendOutgoingCommands.
	///
	/// Call this method regularly (2..20 times a second).
	///
	/// This will Dispatch ANY remaining buffered responses and events AND will send queued outgoing commands.
	/// Fewer calls might be more effective if a device cannot send many packets per second, as multiple
	/// operations might be combined into one package.
	/// </remarks>
	/// <example>
	/// You could replace Service by:
	///
	///     while (DispatchIncomingCommands()); //Dispatch until everything is Dispatched...
	///     SendOutgoingCommands(); //Send a UDP/TCP package with outgoing messages
	/// </example>
	/// <seealso cref="M:Photon.Client.PhotonPeer.DispatchIncomingCommands" />
	/// <seealso cref="M:Photon.Client.PhotonPeer.SendOutgoingCommands" />
	public virtual void Service()
	{
		while (DispatchIncomingCommands())
		{
		}
		while (SendOutgoingCommands())
		{
		}
	}

	/// <summary>
	/// Creates and sends a UDP/TCP package with outgoing commands (operations and acknowledgements). Also called by Service().
	/// </summary>
	/// <remarks>
	/// As the Photon library does not create any UDP/TCP packages by itself. Instead, the application
	/// fully controls how many packages are sent and when. A tradeoff, an application will
	/// lose connection, if it is no longer calling SendOutgoingCommands or Service.
	///
	/// If multiple operations and ACKs are waiting to be sent, they will be aggregated into one
	/// package. The package fills in this order:
	///   ACKs for received commands
	///   A "Ping" - only if no reliable data was sent for a while
	///   Starting with the lowest Channel-Nr:
	///     Reliable Commands in channel
	///     Unreliable Commands in channel
	///
	/// This gives a higher priority to lower channels.
	///
	/// A longer interval between sends will lower the overhead per sent operation but
	/// increase the internal delay (which adds "lag").
	///
	/// Call this 2..20 times per second (depending on your target platform).
	/// </remarks>
	/// <returns>If commands are not yet sent. Udp limits its package size, Tcp does not.</returns>
	public virtual bool SendOutgoingCommands()
	{
		Stats.SendOutgoingCommandsCalled(peerBase.timeInt);
		lock (sendOutgoingLockObject)
		{
			return peerBase.SendOutgoingCommands();
		}
	}

	/// <summary>
	/// Sends only acks and commands that need a re-send.
	/// </summary>
	/// <returns>If there is more to send. If true, you can consider calling it right again.</returns>
	public virtual bool SendAcksOnly()
	{
		lock (sendOutgoingLockObject)
		{
			return peerBase.SendAcksOnly();
		}
	}

	/// <summary>
	/// Dispatching received messages (commands), causes callbacks for events, responses and state changes within a IPhotonPeerListener.
	/// </summary>
	/// <remarks>
	/// DispatchIncomingCommands only executes a single received
	/// command per call. If a command was dispatched, the return value is true and the method
	/// should be called again.
	///
	/// This method is called by Service() until currently available commands are dispatched.
	/// In general, this method should be called until it returns false. In a few cases, it might
	/// make sense to pause dispatching (if a certain state is reached and the app needs to load
	/// data, before it should handle new events).
	///
	/// The callbacks to the peer's IPhotonPeerListener are executed in the same thread that is
	/// calling DispatchIncomingCommands. This makes things easier in a game loop: Event execution
	/// won't clash with painting objects or the game logic.
	/// </remarks>
	public virtual bool DispatchIncomingCommands()
	{
		Stats.DispatchIncomingCommandsCalled(peerBase.timeInt);
		lock (dispatchLockObject)
		{
			peerBase.ByteCountCurrentDispatch = 0;
			return peerBase.DispatchIncomingCommands();
		}
	}

	/// <summary>
	/// Prepares your operation (code and parameters) to be sent to the Photon Server with specified SendOptions.
	/// </summary>
	/// <remarks>
	/// This method serializes and queued the operation right away while the actual sending happens later.
	/// To be able to aggregate operations/messages, the Photon client sends packages only when you call SendOutgoingCommands().
	///
	/// The sendOptions specify how the operation gets sent exactly.
	/// Keep in mind that some transport protocols don't support unreliable or unsequenced transport.
	/// In that case, the sendOptions might be ignored.
	///
	/// The operationCode must be known by the server's logic or won't be processed.
	/// In almost all cases, sending an operation will result in a OperationResponse (see: IPhotonPeerListener.OnOperationResponse).
	/// </remarks>
	/// <param name="operationCode">Operations are handled by their byte\-typed code. The codes are defined in the Realtime API.</param>
	/// <param name="operationParameters">Containing parameters as key\-value pair. The key is byte\-typed, while the value is any serializable datatype.</param>
	/// <param name="sendOptions">Wraps up DeliveryMode (reliability), Encryption and Channel values for sending.</param>
	/// <returns>If operation could be queued for sending.</returns>
	public virtual bool SendOperation(byte operationCode, ParameterDictionary operationParameters, SendOptions sendOptions)
	{
		if (sendOptions.Encrypt && !IsEncryptionAvailable && peerBase.usedTransportProtocol != ConnectionProtocol.WebSocketSecure)
		{
			throw new ArgumentException("Can't use encryption yet. Exchange keys first.");
		}
		if (peerBase.peerConnectionState != ConnectionStateValue.Connected)
		{
			if ((int)LogLevel >= 1)
			{
				Listener.DebugReturn(LogLevel.Error, $"SendOperation failed. Not connected. Failed operation: {operationCode} PeerState: {peerBase.peerConnectionState}");
			}
			Listener.OnStatusChanged(StatusCode.SendError);
			return false;
		}
		if (sendOptions.Channel >= ChannelCount)
		{
			if ((int)LogLevel >= 1)
			{
				Listener.DebugReturn(LogLevel.Error, $"SendOperation failed. Channel unavailable: ({sendOptions.Channel} >= channelCount {ChannelCount}). Failed operation: {operationCode}");
			}
			Listener.OnStatusChanged(StatusCode.SendError);
			return false;
		}
		lock (enqueueLock)
		{
			StreamBuffer serializedOp = peerBase.SerializeOperationToMessage(operationCode, operationParameters, EgMessageType.Operation, sendOptions.Encrypt);
			return peerBase.EnqueuePhotonMessage(serializedOp, sendOptions);
		}
	}

	/// <summary>Sends a custom "Message" to the server, which must be handled by a Plugin or custom Server.</summary>
	/// <param name="message">When passing a byte[], the client will send a Raw Message (message type 9). Else it is a message with serialization (type code 8).</param>
	/// <param name="sendOptions">Defines how to send the operation. Reliability, encryption and channel.</param>
	/// <returns>True if the message got queued for sending.</returns>
	/// <exception cref="T:System.ArgumentException">If the message failed serialization or queueing.</exception>
	public virtual bool SendMessage(object message, SendOptions sendOptions)
	{
		if (sendOptions.Encrypt && !IsEncryptionAvailable && peerBase.usedTransportProtocol != ConnectionProtocol.WebSocketSecure)
		{
			throw new ArgumentException("Can't use encryption yet. Exchange keys first.");
		}
		if (peerBase.peerConnectionState != ConnectionStateValue.Connected)
		{
			if ((int)LogLevel >= 1)
			{
				Listener.DebugReturn(LogLevel.Error, $"SendMessage failed. Not connected. PeerState: {peerBase.peerConnectionState}");
			}
			Listener.OnStatusChanged(StatusCode.SendError);
			return false;
		}
		if (sendOptions.Channel >= ChannelCount)
		{
			if ((int)LogLevel >= 1)
			{
				Listener.DebugReturn(LogLevel.Error, $"SendMessage failed. Channel unavailable: ({sendOptions.Channel} >= channelCount {ChannelCount}). Failed message: {message}");
			}
			Listener.OnStatusChanged(StatusCode.SendError);
			return false;
		}
		lock (enqueueLock)
		{
			StreamBuffer serializedOp = peerBase.SerializeMessageToMessage(message, sendOptions.Encrypt);
			return peerBase.EnqueuePhotonMessage(serializedOp, sendOptions);
		}
	}

	/// <summary>
	/// Registers new types/classes for de/serialization and the fitting methods to call for this type.
	/// </summary>
	/// <remarks>
	/// After registering a Type, it can be used in events and operations and will be serialized like built-in types.
	///
	/// Serialization and deserialization are complementary: Feed the product of serializeMethod to
	/// the deserializeMethod to get a comparable instance of the object.
	/// </remarks>
	/// <param name="customType">Type (class) to register. Must be non-null.</param>
	/// <param name="code">A byte-code used as shortcut during transfer of this Type.</param>
	/// <param name="serializeMethod">Method delegate to create a byte[] from a customType instance. Must be non-null.</param>
	/// <param name="deserializeMethod">Method delegate to create instances of customType's from byte[]. Must be non-null.</param>
	/// <returns>If the Type was registered successfully. Returns false if the type could not be registered.</returns>
	public static bool RegisterType(Type customType, byte code, SerializeStreamMethod serializeMethod, DeserializeStreamMethod deserializeMethod)
	{
		return Protocol.TryRegisterType(customType, code, serializeMethod, deserializeMethod);
	}
}
