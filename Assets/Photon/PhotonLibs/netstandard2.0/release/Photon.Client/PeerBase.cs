using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Photon.Client.Encryption;

namespace Photon.Client;

/// <summary>Base class to implement the transport-protocols on.</summary>
public abstract class PeerBase
{
	internal delegate void MyAction();

	private static class GpBinaryV3Parameters
	{
		public const byte CustomObject = 0;

		public const byte ExtraPlatformParams = 1;
	}

	private int bestRoundtripTimeoutIntern;

	internal PhotonPeer photonPeer;

	/// <summary>Set to a "Protocol implementation" (1.8 by default) which provides de/serialization for supported containers and types.</summary>
	public Protocol SerializationProtocol;

	internal ConnectionProtocol usedTransportProtocol;

	internal PhotonSocket PhotonSocket;

	/// <summary>
	/// This is the (low level) connection state of the peer. It's internal and based on eNet's states.
	/// </summary>
	/// <remarks>Applications can read the "high level" state as PhotonPeer.PeerState, which uses a different enum.</remarks>
	internal ConnectionStateValue peerConnectionState;

	/// <summary>Byte count of last sent operation (set during serialization).</summary>
	internal int ByteCountLastOperation;

	/// <summary>Byte count of last dispatched message (set during dispatch/deserialization).</summary>
	internal int ByteCountCurrentDispatch;

	/// <summary>The command that's currently being dispatched.</summary>
	internal NCommand CommandInCurrentDispatch;

	internal int packetLossByCrc;

	internal int packetLossByChallenge;

	internal int throttledBySendWindow;

	internal readonly Queue<MyAction> ActionQueue = new Queue<MyAction>();

	/// This ID is assigned by the Realtime Server upon connection.
	/// The application does not have to care about this, but it is useful in debugging.
	internal short peerID = -1;

	internal static short peerCount;

	/// <summary>
	/// The serverTimeOffset is serverTimestamp - localTime. Used to approximate the serverTimestamp with help of localTime
	/// </summary>
	internal int serverTimeOffset;

	internal bool serverTimeOffsetIsAvailable;

	internal int roundTripTime;

	internal int roundTripTimeVariance;

	internal int lastRoundTripTime;

	internal int lowestRoundTripTime;

	internal int highestRoundTripTimeVariance;

	/// <summary>Set via Connect(..., customObject) and sent in Init-Request.</summary>
	internal object PhotonToken;

	/// <summary>Sent on connect in an Init Request.</summary>
	internal object CustomInitData;

	/// <summary>Temporary cache of AppId. Used in Connect() to keep the AppId until we send the Init-Request (after the network-level (and Enet) connect).</summary>
	public string AppId;

	internal EventData reusableEventData;

	internal Stopwatch watch = Stopwatch.StartNew();

	internal int timeoutInt;

	internal int timeLastAckReceive;

	internal int longestSendCall;

	/// <summary>Set to timeInt, whenever SendOutgoingCommands actually checks outgoing queues to send them. Must be connected.</summary>
	internal int timeIntCurrentSend;

	internal bool ApplicationIsInitialized;

	internal bool isEncryptionAvailable;

	private ushort serverFeatureFlags;

	protected internal static Pool<StreamBuffer> MessageBufferPool = new Pool<StreamBuffer>(() => new StreamBuffer(PhotonPeer.OutgoingStreamBufferSize), delegate(StreamBuffer buffer)
	{
		buffer.Reset();
	}, 16);

	internal byte[] messageHeader;

	/// <summary>Count of URLs this peer prepared for WS/WSS yet.</summary>
	private volatile int prepareWebSocketUrlCount = -1;

	/// <summary>Used to prepare websocket urls (with appid, etc).</summary>
	private StringBuilder prepareWebSocketUrlSB;

	internal ICryptoProvider CryptoProvider;

	private readonly Random lagRandomizer = new Random();

	internal readonly LinkedList<SimulationItem> NetSimListOutgoing = new LinkedList<SimulationItem>();

	internal readonly LinkedList<SimulationItem> NetSimListIncoming = new LinkedList<SimulationItem>();

	private readonly NetworkSimulationSet networkSimulationSettings = new NetworkSimulationSet();

	internal int bestRoundtripTimeout
	{
		get
		{
			return bestRoundtripTimeoutIntern;
		}
		set
		{
			if (bestRoundtripTimeoutIntern <= 0 || value < bestRoundtripTimeoutIntern)
			{
				bestRoundtripTimeoutIntern = value;
			}
		}
	}

	/// <summary>See PhotonPeer value.</summary>
	internal TrafficStats Stats => photonPeer.Stats;

	internal IPhotonPeerListener Listener => photonPeer.Listener;

	internal LogLevel LogLevel => photonPeer.LogLevel;

	/// <summary>The server's address, as set by a Connect() call, including any protocol, ports and or path.</summary>
	/// <remarks>If rHTTP is used, this can be set directly.</remarks>
	public string ServerAddress { get; internal set; }

	/// <summary>Optional proxy address defined for the current WS/WSS connection. Ignored by other protocols.</summary>
	public string ProxyServerAddress { get; internal set; }

	internal string rttVarString => $"{roundTripTime}({roundTripTimeVariance})";

	internal int DisconnectTimeout => photonPeer.DisconnectTimeout;

	internal int PingInterval => photonPeer.PingInterval;

	internal byte ChannelCount => photonPeer.ChannelCount;

	internal abstract int QueuedIncomingCommandsCount { get; }

	internal abstract int QueuedOutgoingCommandsCount { get; }

	/// <summary>When using UDP, the server will assign a PeerID (int) to each connection. This is an internal value but useful to know for debugging connections.</summary>
	/// <remarks>There is no PeerID for other transport protocols.</remarks>
	public virtual string PeerID => ((ushort)peerID).ToString();

	internal int timeInt => (int)watch.ElapsedMilliseconds;

	/// <summary>In a UDP connection, the server's VerifyConnect command may indicate certain features are available (or not).</summary>
	public ushort ServerFeatureFlags
	{
		get
		{
			return serverFeatureFlags;
		}
		internal set
		{
			serverFeatureFlags = value;
			serverFeatureAck2Available = (serverFeatureFlags & 1) > 0;
		}
	}

	internal bool serverFeatureAck2Available { get; private set; }

	/// <summary> Maximum Transfer Unit to be used for UDP+TCP</summary>
	internal int mtu => photonPeer.MaximumTransferUnit;

	/// <summary>If PhotonSocket.Connected is true, this value shows if the server's address resolved as IPv6 address.</summary>
	/// <remarks>
	/// You must check the socket's IsConnected state. Otherwise, this value is not initialized.
	/// Sent to server in Init-Request.
	/// </remarks>
	protected internal bool IsIpv6
	{
		get
		{
			if (PhotonSocket != null)
			{
				return PhotonSocket.AddressResolvedAsIpv6;
			}
			return false;
		}
	}

	/// <summary>
	/// Gets the currently used settings for the built-in network simulation.
	/// Please check the description of NetworkSimulationSet for more details.
	/// </summary>
	public NetworkSimulationSet NetworkSimulationSettings => networkSimulationSettings;

	protected PeerBase()
	{
		networkSimulationSettings.peerBase = this;
		peerCount++;
	}

	internal virtual void Reset()
	{
		SerializationProtocol = SerializationProtocolFactory.Create(photonPeer.SerializationProtocolType);
		ByteCountLastOperation = 0;
		ByteCountCurrentDispatch = 0;
		Stats.BytesIn = 0L;
		Stats.BytesOut = 0L;
		packetLossByCrc = 0;
		packetLossByChallenge = 0;
		networkSimulationSettings.LostPackagesIn = 0;
		networkSimulationSettings.LostPackagesOut = 0;
		throttledBySendWindow = 0;
		lock (NetSimListOutgoing)
		{
			NetSimListOutgoing.Clear();
		}
		lock (NetSimListIncoming)
		{
			NetSimListIncoming.Clear();
		}
		lock (ActionQueue)
		{
			ActionQueue.Clear();
		}
		peerConnectionState = ConnectionStateValue.Disconnected;
		watch.Reset();
		watch.Start();
		isEncryptionAvailable = false;
		ServerFeatureFlags = 0;
		ApplicationIsInitialized = false;
		CryptoProvider = null;
		roundTripTime = 200;
		roundTripTimeVariance = 5;
		serverTimeOffsetIsAvailable = false;
		serverTimeOffset = 0;
	}

	internal abstract bool Connect(string serverAddress, string proxyServerAddress, string appID, object photonToken);

	private string GetHttpKeyValueString(Dictionary<string, string> dic)
	{
		StringBuilder sb = new StringBuilder();
		foreach (KeyValuePair<string, string> p in dic)
		{
			sb.Append(p.Key).Append("=").Append(p.Value)
				.Append("&");
		}
		return sb.ToString();
	}

	/// <summary>
	/// Writes and "Init Request", which initializes the connection / application used server-side.
	/// </summary>
	/// <remarks>Uses this.ServerAddress, this.AppId, this.PhotonToken and CustomInitData and some more values.</remarks>
	/// <returns>Bytes of the init request.</returns>
	internal byte[] WriteInitRequest()
	{
		if (photonPeer.UseInitV3)
		{
			return WriteInitV3();
		}
		if (PhotonToken == null)
		{
			byte[] initBytes = new byte[41];
			byte[] clientVersion = Version.clientVersion;
			initBytes[0] = 243;
			initBytes[1] = 0;
			initBytes[2] = SerializationProtocol.VersionBytes[0];
			initBytes[3] = SerializationProtocol.VersionBytes[1];
			initBytes[4] = photonPeer.ClientSdkIdShifted;
			initBytes[5] = (byte)((byte)(clientVersion[0] << 4) | clientVersion[1]);
			initBytes[6] = clientVersion[2];
			initBytes[7] = clientVersion[3];
			initBytes[8] = 0;
			if (string.IsNullOrEmpty(AppId))
			{
				AppId = "Realtime";
			}
			for (int i = 0; i < 32; i++)
			{
				initBytes[i + 9] = (byte)((i < AppId.Length) ? ((byte)AppId[i]) : 0);
			}
			if (IsIpv6)
			{
				initBytes[5] |= 128;
			}
			else
			{
				initBytes[5] &= 127;
			}
			return initBytes;
		}
		if (PhotonToken != null)
		{
			byte[] result = null;
			Dictionary<string, string> query = new Dictionary<string, string>();
			query["init"] = null;
			query["app"] = AppId;
			query["clientversion"] = PhotonPeer.Version;
			query["protocol"] = SerializationProtocol.ProtocolType;
			query["sid"] = photonPeer.ClientSdkIdShifted.ToString();
			byte[] customData = null;
			int totalLen = 0;
			if (PhotonToken != null)
			{
				customData = SerializationProtocol.Serialize(PhotonToken);
				totalLen += customData.Length;
			}
			string queryString = GetHttpKeyValueString(query);
			if (IsIpv6)
			{
				queryString += "&IPv6";
			}
			string httpHeader = $"POST /?{queryString} HTTP/1.1\r\nHost: {ServerAddress}\r\nContent-Length: {totalLen}\r\n\r\n";
			result = new byte[httpHeader.Length + totalLen];
			if (customData != null)
			{
				Buffer.BlockCopy(customData, 0, result, httpHeader.Length, customData.Length);
			}
			Buffer.BlockCopy(Encoding.UTF8.GetBytes(httpHeader), 0, result, 0, httpHeader.Length);
			return result;
		}
		return null;
	}

	private byte[] WriteInitV3()
	{
		StreamBuffer stream = new StreamBuffer();
		stream.WriteByte(245);
		InitV3Flags flags = InitV3Flags.NoFlags;
		if (IsIpv6)
		{
			flags |= InitV3Flags.IPv6Flag;
		}
		flags |= InitV3Flags.ReleaseSdkFlag;
		IPhotonEncryptor encryptor = photonPeer.Encryptor;
		if (encryptor != null)
		{
			flags |= InitV3Flags.EncryptionFlag;
		}
		stream.WriteBytes((byte)((int)flags >> 8), (byte)flags);
		switch (SerializationProtocol.VersionBytes[1])
		{
		case 6:
			stream.WriteByte(16);
			break;
		case 8:
			stream.WriteByte(18);
			break;
		default:
			throw new Exception("Unknown protocol version: " + SerializationProtocol.VersionBytes[1]);
		}
		stream.Write(Version.clientVersion, 0, 4);
		stream.WriteByte(photonPeer.ClientSdkIdShifted);
		stream.WriteByte(0);
		if (string.IsNullOrEmpty(AppId))
		{
			AppId = "Master";
		}
		byte[] appIdBytes = Encoding.UTF8.GetBytes(AppId);
		int len = appIdBytes.Length;
		if (len > 255)
		{
			throw new Exception("AppId is too long. Limited by 255 symbols.");
		}
		stream.WriteByte((byte)len);
		stream.Write(appIdBytes, 0, appIdBytes.Length);
		if (PhotonToken is byte[] tokenBytes)
		{
			len = tokenBytes.Length;
			stream.WriteBytes((byte)(len >> 8), (byte)len);
			stream.Write(tokenBytes, 0, len);
		}
		else
		{
			stream.WriteBytes(0, 0);
		}
		Dictionary<byte, object> query = new Dictionary<byte, object>();
		if (CustomInitData != null)
		{
			query.Add(0, CustomInitData);
		}
		if (encryptor != null)
		{
			throw new NotImplementedException("InitV3 with encryption is not implemented yet.");
		}
		SerializationProtocol.Serialize(stream, query, setType: true);
		return stream.ToArray();
	}

	internal string PrepareWebSocketUrl(string serverAddress, string appId, object photonToken)
	{
		if (prepareWebSocketUrlSB == null)
		{
			prepareWebSocketUrlSB = new StringBuilder(256);
		}
		prepareWebSocketUrlSB.Clear();
		prepareWebSocketUrlCount++;
		prepareWebSocketUrlSB.Append(serverAddress);
		prepareWebSocketUrlSB.AppendFormat("/?libversion={0}", PhotonPeer.Version);
		prepareWebSocketUrlSB.AppendFormat("&sid={0}", photonPeer.ClientSdkIdShifted);
		prepareWebSocketUrlSB.AppendFormat("&peerId={0}_{1}", peerID, prepareWebSocketUrlCount);
		if (!photonPeer.RemoveAppIdFromWebSocketPath && appId != null && appId.Length >= 8)
		{
			prepareWebSocketUrlSB.AppendFormat("&app={0}", appId.Substring(0, 8));
		}
		if (IsIpv6)
		{
			prepareWebSocketUrlSB.Append("&IPv6");
		}
		if (photonToken != null)
		{
			prepareWebSocketUrlSB.Append("&xInit=");
		}
		return prepareWebSocketUrlSB.ToString();
	}

	/// <summary>Now obsolete. Was required to be called by PhotonSocket implementations to signal when they connected (which triggered sending the initial message, init or connect).</summary>
	[Obsolete("This callback is no longer required by PhotonSocket implementations.")]
	public void OnConnect()
	{
	}

	/// <summary>Called when the server's Init Response arrived.</summary>
	internal void OnInitResponse()
	{
		if (peerConnectionState == ConnectionStateValue.Connecting)
		{
			peerConnectionState = ConnectionStateValue.Connected;
		}
		ApplicationIsInitialized = true;
		FetchServerTimestamp();
		Listener.OnStatusChanged(StatusCode.Connect);
	}

	internal abstract void Disconnect(bool queueStatusChangeCallback = true);

	internal abstract void SimulateTimeoutDisconnect(bool queueStatusChangeCallback = true);

	internal abstract void FetchServerTimestamp();

	internal abstract bool IsTransportEncrypted();

	internal abstract bool EnqueuePhotonMessage(StreamBuffer opBytes, SendOptions sendParams);

	/// <summary>Serializes an operation into our binary messages (magic number, msg-type byte and message). Optionally encrypts.</summary>
	/// <remarks>This method is mostly the same in EnetPeer, TPeer and HttpPeerBase. Also, for raw messages, we have another variant.</remarks>
	internal StreamBuffer SerializeOperationToMessage(byte opCode, ParameterDictionary parameters, EgMessageType messageType, bool encrypt)
	{
		bool num = encrypt && !IsTransportEncrypted();
		StreamBuffer serializeMemStream = MessageBufferPool.Acquire();
		serializeMemStream.SetLength(0L);
		if (!num)
		{
			serializeMemStream.Write(messageHeader, 0, messageHeader.Length);
		}
		SerializationProtocol.SerializeOperationRequest(serializeMemStream, opCode, parameters, setType: false);
		if (num)
		{
			byte[] encryptedBytes = CryptoProvider.Encrypt(serializeMemStream.GetBuffer(), 0, serializeMemStream.Length);
			serializeMemStream.SetLength(0L);
			serializeMemStream.Write(messageHeader, 0, messageHeader.Length);
			serializeMemStream.Write(encryptedBytes, 0, encryptedBytes.Length);
		}
		byte[] fullMessageBytes = serializeMemStream.GetBuffer();
		if (messageType != EgMessageType.Operation)
		{
			fullMessageBytes[messageHeader.Length - 1] = (byte)messageType;
		}
		if (num || (encrypt && photonPeer.EnableEncryptedFlag))
		{
			fullMessageBytes[messageHeader.Length - 1] = (byte)(fullMessageBytes[messageHeader.Length - 1] | 0x80);
		}
		return serializeMemStream;
	}

	/// <summary> Returns the UDP Payload starting with Magic Number for binary protocol </summary>
	internal StreamBuffer SerializeMessageToMessage(object message, bool encrypt)
	{
		bool num = encrypt && !IsTransportEncrypted();
		StreamBuffer serializeMemStream = MessageBufferPool.Acquire();
		serializeMemStream.SetLength(0L);
		if (!num)
		{
			serializeMemStream.Write(messageHeader, 0, messageHeader.Length);
		}
		bool isRawMessage = message is byte[];
		if (isRawMessage)
		{
			byte[] data = message as byte[];
			serializeMemStream.Write(data, 0, data.Length);
		}
		else
		{
			SerializationProtocol.SerializeMessage(serializeMemStream, message);
		}
		if (num)
		{
			byte[] encryptedBytes = CryptoProvider.Encrypt(serializeMemStream.GetBuffer(), 0, serializeMemStream.Length);
			serializeMemStream.SetLength(0L);
			serializeMemStream.Write(messageHeader, 0, messageHeader.Length);
			serializeMemStream.Write(encryptedBytes, 0, encryptedBytes.Length);
		}
		byte[] fullMessageBytes = serializeMemStream.GetBuffer();
		fullMessageBytes[messageHeader.Length - 1] = (byte)(isRawMessage ? 9 : 8);
		if (num || (encrypt && photonPeer.EnableEncryptedFlag))
		{
			fullMessageBytes[messageHeader.Length - 1] = (byte)(fullMessageBytes[messageHeader.Length - 1] | 0x80);
		}
		return serializeMemStream;
	}

	/// <summary>
	/// Checks outgoing queues for commands to send and puts them on their way.
	/// This creates one package per go in UDP.
	/// </summary>
	/// <returns>If commands are not sent, cause they didn't fit into the package that's sent.</returns>
	internal abstract bool SendOutgoingCommands();

	internal virtual bool SendAcksOnly()
	{
		return false;
	}

	internal abstract void ReceiveIncomingCommands(byte[] inBuff, int dataLength);

	/// <summary>
	/// Checks the incoming queue and Dispatches received data if possible.
	/// </summary>
	/// <returns>If a Dispatch happened or not, which shows if more Dispatches might be needed.</returns>
	internal abstract bool DispatchIncomingCommands();

	internal virtual bool DeserializeMessageAndCallback(StreamBuffer stream)
	{
		if (stream.Length < 2)
		{
			if ((int)LogLevel >= 4)
			{
				Listener.DebugReturn(LogLevel.Debug, $"Discarding message: Less than 2 bytes. Length: {stream.Length}");
			}
			return false;
		}
		byte magicByte = stream.ReadByte();
		if (magicByte != 243 && magicByte != 253)
		{
			if ((int)LogLevel >= 4)
			{
				Listener.DebugReturn(LogLevel.Debug, $"Discarding message: Unknown magic byte: {magicByte}");
			}
			return false;
		}
		byte num = stream.ReadByte();
		byte msgType = (byte)(num & 0x7F);
		bool isEncrypted = (num & 0x80) > 0;
		if (msgType != 1)
		{
			try
			{
				if (isEncrypted)
				{
					stream = new StreamBuffer(CryptoProvider.Decrypt(stream.GetBuffer(), 2, stream.Length - 2));
				}
				else
				{
					stream.Seek(2L, SeekOrigin.Begin);
				}
			}
			catch (Exception ex)
			{
				if ((int)LogLevel >= 1)
				{
					Listener.DebugReturn(LogLevel.Error, $"Decryption caught exception handling msgType: {msgType} exception: {ex}");
				}
				SupportClass.WriteStackTrace(ex);
				return false;
			}
		}
		Protocol.DeserializationFlags flags = (Protocol.DeserializationFlags)((photonPeer.UseByteArraySlicePoolForEvents ? 1 : 0) | (photonPeer.WrapIncomingStructs ? 2 : 0));
		int timeBeforeCallback = 0;
		switch (msgType)
		{
		case 3:
		{
			OperationResponse opRes = null;
			try
			{
				opRes = SerializationProtocol.DeserializeOperationResponse(stream, flags);
			}
			catch (Exception arg4)
			{
				if ((int)LogLevel >= 1)
				{
					EnqueueDebugReturn(LogLevel.Error, $"Deserialization caught exception for Operation Response: {arg4}");
				}
				return false;
			}
			timeBeforeCallback = timeInt;
			Listener.OnOperationResponse(opRes);
			Stats.LastDispatchDuration = timeInt - timeBeforeCallback;
			break;
		}
		case 4:
		{
			EventData ev = null;
			try
			{
				ev = SerializationProtocol.DeserializeEventData(stream, reusableEventData, flags);
			}
			catch (Exception arg)
			{
				if ((int)LogLevel >= 1)
				{
					EnqueueDebugReturn(LogLevel.Error, $"Deserialization caught exception for Event: {arg}");
				}
				return false;
			}
			timeBeforeCallback = timeInt;
			Listener.OnEvent(ev);
			Stats.LastDispatchDuration = timeInt - timeBeforeCallback;
			if (photonPeer.ReuseEventInstance)
			{
				reusableEventData = ev;
			}
			break;
		}
		case 5:
			try
			{
				DisconnectMessage disconnectMessage = SerializationProtocol.DeserializeDisconnectMessage(stream);
				Listener.OnDisconnectMessage(disconnectMessage);
			}
			catch (Exception arg3)
			{
				if ((int)LogLevel >= 1)
				{
					EnqueueDebugReturn(LogLevel.Error, $"Deserialization caught exception for Disconnect Message: {arg3}");
				}
				return false;
			}
			break;
		case 1:
			OnInitResponse();
			break;
		case 7:
		{
			OperationResponse opRes;
			try
			{
				opRes = SerializationProtocol.DeserializeOperationResponse(stream);
			}
			catch (Exception arg2)
			{
				if ((int)LogLevel >= 1)
				{
					EnqueueDebugReturn(LogLevel.Error, $"Deserialization caught exception for Internal Operation Response: {arg2}");
				}
				return false;
			}
			timeBeforeCallback = timeInt;
			if (opRes.OperationCode == PhotonCodes.InitEncryption)
			{
				DeriveSharedKey(opRes);
			}
			else if (opRes.OperationCode == PhotonCodes.Ping)
			{
				if (peerConnectionState == ConnectionStateValue.Connecting && (usedTransportProtocol == ConnectionProtocol.WebSocket || usedTransportProtocol == ConnectionProtocol.WebSocketSecure))
				{
					photonPeer.PingUsedAsInit = true;
					OnInitResponse();
				}
				if (this is TPeer peer)
				{
					peer.ReadPingResult(opRes);
				}
			}
			else if ((int)LogLevel >= 1)
			{
				EnqueueDebugReturn(LogLevel.Error, "Deserialization failed for unknown Internal Operation Response Code: " + opRes.ToStringFull());
			}
			Stats.LastDispatchDuration = timeInt - timeBeforeCallback;
			break;
		}
		case 8:
		{
			object message = SerializationProtocol.DeserializeMessage(stream);
			timeBeforeCallback = timeInt;
			Listener.OnMessage(isRawMessage: false, message);
			Stats.LastDispatchDuration = timeInt - timeBeforeCallback;
			break;
		}
		case 9:
			timeBeforeCallback = timeInt;
			Listener.OnMessage(isRawMessage: true, stream);
			Stats.LastDispatchDuration = timeInt - timeBeforeCallback;
			break;
		default:
			if ((int)LogLevel >= 1)
			{
				EnqueueDebugReturn(LogLevel.Error, $"Deserialization failed for unexpected msgType: {msgType}");
			}
			break;
		}
		return true;
	}

	internal void UpdateRoundTripTimeAndVariance(int lastRoundtripTime)
	{
		if (lastRoundtripTime >= 0)
		{
			roundTripTimeVariance -= roundTripTimeVariance / 4;
			if (lastRoundtripTime >= roundTripTime)
			{
				roundTripTime += (lastRoundtripTime - roundTripTime) / 8;
				roundTripTimeVariance += (lastRoundtripTime - roundTripTime) / 4;
			}
			else
			{
				roundTripTime += (lastRoundtripTime - roundTripTime) / 8;
				roundTripTimeVariance -= (lastRoundtripTime - roundTripTime) / 4;
			}
			if (roundTripTime < lowestRoundTripTime)
			{
				lowestRoundTripTime = roundTripTime;
			}
			if (roundTripTimeVariance > highestRoundTripTimeVariance)
			{
				highestRoundTripTimeVariance = roundTripTimeVariance;
			}
			Stats.RoundtripTime = roundTripTime;
			Stats.RoundtripTimeVariance = roundTripTimeVariance;
			Stats.LastRoundtripTime = lastRoundTripTime;
		}
	}

	/// <summary>
	/// Internally uses an operation to exchange encryption keys with the server.
	/// </summary>
	/// <returns>If the op could be sent.</returns>
	internal bool ExchangeKeysForEncryption(object lockObject)
	{
		if (lockObject == null)
		{
			throw new NotSupportedException("Parameter lockObject must be non-Null.");
		}
		isEncryptionAvailable = false;
		if (CryptoProvider != null)
		{
			CryptoProvider.Dispose();
			CryptoProvider = null;
		}
		if (photonPeer.PayloadEncryptorType != null)
		{
			try
			{
				CryptoProvider = (ICryptoProvider)Activator.CreateInstance(photonPeer.PayloadEncryptorType);
				if (CryptoProvider == null)
				{
					Listener.DebugReturn(LogLevel.Warning, $"Payload encryptor creation by type failed, Activator.CreateInstance() returned null for: {photonPeer.PayloadEncryptorType}");
				}
			}
			catch (Exception arg)
			{
				Listener.DebugReturn(LogLevel.Warning, $"Payload encryptor creation by type failed. Caught: {arg}");
			}
		}
		if (CryptoProvider == null)
		{
			CryptoProvider = new DiffieHellmanCryptoProvider();
		}
		ParameterDictionary parameters = new ParameterDictionary(1);
		parameters[PhotonCodes.ClientKey] = CryptoProvider.PublicKey;
		lock (lockObject)
		{
			SendOptions sendParams = new SendOptions
			{
				Channel = 0,
				Encrypt = false,
				Reliability = true
			};
			StreamBuffer serializedOp = SerializeOperationToMessage(PhotonCodes.InitEncryption, parameters, EgMessageType.InternalOperationRequest, sendParams.Encrypt);
			return EnqueuePhotonMessage(serializedOp, sendParams);
		}
	}

	internal void DeriveSharedKey(OperationResponse operationResponse)
	{
		if (operationResponse.ReturnCode != 0)
		{
			EnqueueDebugReturn(LogLevel.Error, "Establishing encryption keys failed. ReturnCode != OK: " + operationResponse.ToStringFull());
			EnqueueStatusCallback(StatusCode.EncryptionFailedToEstablish);
			return;
		}
		byte[] serverPublicKey = (byte[])operationResponse.Parameters[PhotonCodes.ServerKey];
		if (serverPublicKey == null || serverPublicKey.Length == 0)
		{
			EnqueueDebugReturn(LogLevel.Error, "Establishing encryption keys failed. Server public key is null or empty: " + operationResponse.ToStringFull());
			EnqueueStatusCallback(StatusCode.EncryptionFailedToEstablish);
		}
		else
		{
			CryptoProvider.DeriveSharedKey(serverPublicKey);
			isEncryptionAvailable = true;
			EnqueueStatusCallback(StatusCode.EncryptionEstablished);
		}
	}

	internal virtual void InitEncryption(byte[] secret)
	{
		if (photonPeer.PayloadEncryptorType != null)
		{
			try
			{
				CryptoProvider = (ICryptoProvider)Activator.CreateInstance(photonPeer.PayloadEncryptorType, secret);
				if (CryptoProvider == null)
				{
					if ((int)LogLevel >= 2)
					{
						Listener.DebugReturn(LogLevel.Warning, $"Payload encryptor creation by type failed, Activator.CreateInstance() returned null for: {photonPeer.PayloadEncryptorType}");
					}
				}
				else
				{
					isEncryptionAvailable = true;
				}
			}
			catch (Exception arg)
			{
				if ((int)LogLevel >= 2)
				{
					Listener.DebugReturn(LogLevel.Warning, $"Payload encryptor creation by type failed: {arg}");
				}
			}
		}
		if (CryptoProvider == null)
		{
			CryptoProvider = new DiffieHellmanCryptoProvider(secret);
			isEncryptionAvailable = true;
		}
	}

	internal void EnqueueActionForDispatch(MyAction action)
	{
		lock (ActionQueue)
		{
			ActionQueue.Enqueue(action);
		}
	}

	internal void EnqueueDebugReturn(LogLevel level, string debugReturn)
	{
		lock (ActionQueue)
		{
			ActionQueue.Enqueue(delegate
			{
				Listener.DebugReturn(level, debugReturn);
			});
		}
	}

	internal void EnqueueStatusCallback(StatusCode statusValue)
	{
		lock (ActionQueue)
		{
			ActionQueue.Enqueue(delegate
			{
				Listener.OnStatusChanged(statusValue);
			});
		}
	}

	internal void SendNetworkSimulated(byte[] dataToSend)
	{
		if (!NetworkSimulationSettings.IsSimulationEnabled)
		{
			throw new NotImplementedException("SendNetworkSimulated was called, despite NetworkSimulationSettings.IsSimulationEnabled == false.");
		}
		if (usedTransportProtocol == ConnectionProtocol.Udp && NetworkSimulationSettings.OutgoingLossPercentage > 0 && lagRandomizer.Next(101) < NetworkSimulationSettings.OutgoingLossPercentage)
		{
			networkSimulationSettings.LostPackagesOut++;
			return;
		}
		int jitter = ((networkSimulationSettings.OutgoingJitter > 0) ? (lagRandomizer.Next(networkSimulationSettings.OutgoingJitter * 2) - networkSimulationSettings.OutgoingJitter) : 0);
		int delay = networkSimulationSettings.OutgoingLag + jitter;
		int timeToExecute = timeInt + delay;
		SimulationItem simItem = new SimulationItem
		{
			DelayedData = dataToSend,
			TimeToExecute = timeToExecute,
			Delay = delay
		};
		lock (NetSimListOutgoing)
		{
			if (NetSimListOutgoing.Count == 0 || usedTransportProtocol == ConnectionProtocol.Tcp)
			{
				NetSimListOutgoing.AddLast(simItem);
				return;
			}
			LinkedListNode<SimulationItem> node = NetSimListOutgoing.First;
			while (node != null && node.Value.TimeToExecute < timeToExecute)
			{
				node = node.Next;
			}
			if (node == null)
			{
				NetSimListOutgoing.AddLast(simItem);
			}
			else
			{
				NetSimListOutgoing.AddBefore(node, simItem);
			}
		}
	}

	internal void ReceiveNetworkSimulated(byte[] dataReceived)
	{
		if (!networkSimulationSettings.IsSimulationEnabled)
		{
			throw new NotImplementedException("ReceiveNetworkSimulated was called, despite NetworkSimulationSettings.IsSimulationEnabled == false.");
		}
		if (usedTransportProtocol == ConnectionProtocol.Udp && networkSimulationSettings.IncomingLossPercentage > 0 && lagRandomizer.Next(101) < networkSimulationSettings.IncomingLossPercentage)
		{
			networkSimulationSettings.LostPackagesIn++;
			return;
		}
		int jitter = ((networkSimulationSettings.IncomingJitter > 0) ? (lagRandomizer.Next(networkSimulationSettings.IncomingJitter * 2) - networkSimulationSettings.IncomingJitter) : 0);
		int delay = networkSimulationSettings.IncomingLag + jitter;
		int timeToExecute = timeInt + delay;
		SimulationItem simItem = new SimulationItem
		{
			DelayedData = dataReceived,
			TimeToExecute = timeToExecute,
			Delay = delay
		};
		lock (NetSimListIncoming)
		{
			if (NetSimListIncoming.Count == 0 || usedTransportProtocol == ConnectionProtocol.Tcp)
			{
				NetSimListIncoming.AddLast(simItem);
				return;
			}
			LinkedListNode<SimulationItem> node = NetSimListIncoming.First;
			while (node != null && node.Value.TimeToExecute < timeToExecute)
			{
				node = node.Next;
			}
			if (node == null)
			{
				NetSimListIncoming.AddLast(simItem);
			}
			else
			{
				NetSimListIncoming.AddBefore(node, simItem);
			}
		}
	}

	/// <summary>
	/// Core of the Network Simulation, which is available in Debug builds.
	/// Called by a timer in intervals.
	/// </summary>
	protected internal void NetworkSimRun()
	{
		while (true)
		{
			bool enabled = false;
			lock (networkSimulationSettings.NetSimManualResetEvent)
			{
				enabled = networkSimulationSettings.IsSimulationEnabled;
			}
			if (!enabled)
			{
				networkSimulationSettings.NetSimManualResetEvent.WaitOne();
				continue;
			}
			lock (NetSimListIncoming)
			{
				SimulationItem item = null;
				while (NetSimListIncoming.First != null)
				{
					item = NetSimListIncoming.First.Value;
					if (item.stopw.ElapsedMilliseconds < item.Delay)
					{
						break;
					}
					ReceiveIncomingCommands(item.DelayedData, item.DelayedData.Length);
					NetSimListIncoming.RemoveFirst();
				}
			}
			lock (NetSimListOutgoing)
			{
				SimulationItem item2 = null;
				while (NetSimListOutgoing.First != null)
				{
					item2 = NetSimListOutgoing.First.Value;
					if (item2.stopw.ElapsedMilliseconds < item2.Delay)
					{
						break;
					}
					if (PhotonSocket != null && PhotonSocket.Connected)
					{
						PhotonSocket.Send(item2.DelayedData, item2.DelayedData.Length);
					}
					NetSimListOutgoing.RemoveFirst();
				}
			}
			Thread.Sleep(0);
		}
	}
}
