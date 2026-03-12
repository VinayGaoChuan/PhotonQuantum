using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Photon.Client;

internal class EnetPeer : PeerBase
{
	internal struct GapBlock
	{
		internal byte Offset;

		internal int Block;
	}

	private const int CRC_LENGTH = 4;

	private const int EncryptedDataGramHeaderSize = 7;

	private const int EncryptedHeaderSize = 5;

	/// <summary>Quick Resends are suspended if the sent queue is this size or larger.</summary>
	private const int QUICK_RESEND_QUEUELIMIT = 25;

	internal Pool<NCommand> nCommandPool = new Pool<NCommand>(() => new NCommand(), delegate(NCommand cmd)
	{
		cmd.Reset();
	}, 16);

	/// <summary>One list for all channels keeps sent commands (for re-sending).</summary>
	private List<NCommand> sentReliableCommands = new List<NCommand>();

	private int sendWindowUpdateRequiredBackValue;

	/// <summary>One pool of ACK byte arrays ( 20 bytes each)  for all channels to keep acknowledgements.</summary>
	private StreamBuffer outgoingAcknowledgementsPool;

	internal const int UnsequencedWindowSize = 128;

	internal readonly int[] unsequencedWindow = new int[4];

	internal int outgoingUnsequencedGroupNumber;

	internal int incomingUnsequencedGroupNumber;

	private byte udpCommandCount;

	private byte[] udpBuffer;

	private int udpBufferIndex;

	private byte[] bufferForEncryption;

	private int commandBufferSize = 100;

	internal int challenge;

	/// <summary>Timestamp sent by the server in last udp package (enet header).</summary>
	/// TODO the serverSentTime should possibly never decrease?!
	internal int serverSentTime;

	internal static readonly byte[] udpHeader0xF3 = new byte[2] { 243, 2 };

	private int datagramEncryptedConnectionBackValue;

	private EnetChannel[] channelArray = new EnetChannel[0];

	private const byte ControlChannelNumber = byte.MaxValue;

	/// <summary>Initial PeerId as used in Connect command. If EnableServerTracing is false.</summary>
	protected internal const short PeerIdForConnect = -1;

	/// <summary>Initial PeerId to enable Photon Tracing, as used in Connect command. See: EnableServerTracing.</summary>
	protected internal const short PeerIdForConnectTrace = -2;

	private Queue<int> commandsToRemove = new Queue<int>();

	/// <summary>Queue of received commands. ReceiveIncomingCommands will queue commands.</summary>
	private ConcurrentQueue<NCommand> CommandQueue = new ConcurrentQueue<NCommand>();

	private int fragmentLength;

	private int fragmentLengthDatagramEncrypt;

	private int fragmentLengthMtuValue;

	/// <summary>Used to store temporary values in UpdateSendWindow().</summary>
	private readonly HashSet<byte> channelsToUpdateLowestSent = new HashSet<byte>();

	private int[] lowestSentSequenceNumber;

	private int[] gapBlocks = new int[4];

	private List<NCommand> toRemove = new List<NCommand>(32);

	internal override int QueuedIncomingCommandsCount
	{
		get
		{
			int x = 0;
			lock (channelArray)
			{
				for (int index = 0; index < channelArray.Length; index++)
				{
					EnetChannel c = channelArray[index];
					x += c.incomingReliableCommandsList.Count;
					x += c.incomingUnreliableCommandsList.Count;
				}
				return x;
			}
		}
	}

	internal override int QueuedOutgoingCommandsCount
	{
		get
		{
			int x = 0;
			lock (channelArray)
			{
				for (int index = 0; index < channelArray.Length; index++)
				{
					EnetChannel channel = channelArray[index];
					lock (channel)
					{
						x += channel.outgoingReliableCommandsList.Count;
						x += channel.outgoingUnreliableCommandsList.Count;
					}
				}
				return x;
			}
		}
	}

	/// <summary>When ACKs are executed they set this. Signals changes within sentReliableCommands. Thread safe.</summary>
	private bool SendWindowUpdateRequired
	{
		get
		{
			return Interlocked.CompareExchange(ref sendWindowUpdateRequiredBackValue, 1, 1) == 1;
		}
		set
		{
			if (value)
			{
				Interlocked.CompareExchange(ref sendWindowUpdateRequiredBackValue, 1, 0);
			}
			else
			{
				Interlocked.CompareExchange(ref sendWindowUpdateRequiredBackValue, 0, 1);
			}
		}
	}

	/// <summary>Gets enabled by "request" from server (not by client). Thread safe.</summary>
	private bool DatagramEncryptedConnection
	{
		get
		{
			return Interlocked.CompareExchange(ref datagramEncryptedConnectionBackValue, 1, 1) == 1;
		}
		set
		{
			if (value)
			{
				Interlocked.CompareExchange(ref datagramEncryptedConnectionBackValue, 1, 0);
			}
			else
			{
				Interlocked.CompareExchange(ref datagramEncryptedConnectionBackValue, 0, 1);
			}
		}
	}

	private bool useAck2
	{
		get
		{
			if (photonPeer.UseAck2)
			{
				return base.serverFeatureAck2Available;
			}
			return false;
		}
	}

	internal EnetPeer()
	{
		messageHeader = udpHeader0xF3;
	}

	internal override bool IsTransportEncrypted()
	{
		return DatagramEncryptedConnection;
	}

	internal override void Reset()
	{
		base.Reset();
		if (photonPeer.PayloadEncryptionSecret != null && usedTransportProtocol == ConnectionProtocol.Udp)
		{
			InitEncryption(photonPeer.PayloadEncryptionSecret);
		}
		if (photonPeer.Encryptor != null)
		{
			isEncryptionAvailable = true;
		}
		peerID = (short)(photonPeer.EnableServerTracing ? (-2) : (-1));
		challenge = SupportClass.ThreadSafeRandom.Next();
		if (udpBuffer == null || udpBuffer.Length != base.mtu)
		{
			udpBuffer = new byte[base.mtu];
		}
		NCommand tmp = null;
		while (CommandQueue.TryDequeue(out tmp))
		{
		}
		timeoutInt = 0;
		base.bestRoundtripTimeout = 0;
		outgoingUnsequencedGroupNumber = 0;
		incomingUnsequencedGroupNumber = 0;
		for (int i = 0; i < unsequencedWindow.Length; i++)
		{
			unsequencedWindow[i] = 0;
		}
		lock (channelArray)
		{
			EnetChannel[] channels = channelArray;
			if (channels.Length != base.ChannelCount + 1)
			{
				channels = new EnetChannel[base.ChannelCount + 1];
			}
			for (byte i2 = 0; i2 < base.ChannelCount; i2++)
			{
				channels[i2] = new EnetChannel(i2, commandBufferSize);
			}
			channels[base.ChannelCount] = new EnetChannel(byte.MaxValue, commandBufferSize);
			channelArray = channels;
		}
		lock (sentReliableCommands)
		{
			sentReliableCommands.Clear();
		}
		outgoingAcknowledgementsPool = new StreamBuffer();
	}

	internal void ApplyRandomizedSequenceNumbers()
	{
		lock (channelArray)
		{
			for (int i = 0; i < channelArray.Length; i++)
			{
				EnetChannel obj = channelArray[i];
				int modifier = photonPeer.RandomizedSequenceNumbers[i % photonPeer.RandomizedSequenceNumbers.Length];
				obj.incomingReliableSequenceNumber += modifier;
				obj.outgoingReliableSequenceNumber += modifier;
				obj.highestReceivedAck += modifier;
				obj.outgoingReliableUnsequencedNumber += modifier;
			}
		}
	}

	private EnetChannel GetChannel(byte channelNumber)
	{
		if (channelNumber != byte.MaxValue)
		{
			return channelArray[channelNumber];
		}
		return channelArray[channelArray.Length - 1];
	}

	internal override bool Connect(string ipport, string proxyServerAddress, string appID, object photonToken)
	{
		if (PhotonSocket.Connect())
		{
			peerConnectionState = ConnectionStateValue.Connecting;
			NCommand cmd = nCommandPool.Acquire();
			cmd.Initialize(this, 2, null, byte.MaxValue);
			QueueOutgoingReliableCommand(cmd);
			return true;
		}
		return false;
	}

	internal override void Disconnect(bool queueStatusChangeCallback = true)
	{
		if (peerConnectionState == ConnectionStateValue.Disconnected || peerConnectionState == ConnectionStateValue.Disconnecting)
		{
			return;
		}
		if (sentReliableCommands != null)
		{
			lock (sentReliableCommands)
			{
				sentReliableCommands.Clear();
			}
		}
		lock (channelArray)
		{
			EnetChannel[] array = channelArray;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].clearAll();
			}
		}
		bool oldSettings = base.NetworkSimulationSettings.IsSimulationEnabled;
		base.NetworkSimulationSettings.IsSimulationEnabled = false;
		NCommand disconnectCommand = nCommandPool.Acquire();
		disconnectCommand.Initialize(this, 4, null, byte.MaxValue);
		peerConnectionState = ConnectionStateValue.Disconnecting;
		QueueOutgoingReliableCommand(disconnectCommand);
		SendOutgoingCommands();
		base.NetworkSimulationSettings.IsSimulationEnabled = oldSettings;
		if (PhotonSocket != null)
		{
			PhotonSocket.Disconnect();
		}
		DatagramEncryptedConnection = false;
		peerConnectionState = ConnectionStateValue.Disconnected;
		if (queueStatusChangeCallback)
		{
			EnqueueStatusCallback(StatusCode.Disconnect);
		}
		else
		{
			base.Listener.OnStatusChanged(StatusCode.Disconnect);
		}
	}

	internal override void SimulateTimeoutDisconnect(bool queueStatusChangeCallback = true)
	{
		if (peerConnectionState == ConnectionStateValue.Disconnected || peerConnectionState == ConnectionStateValue.Disconnecting)
		{
			return;
		}
		if (sentReliableCommands != null)
		{
			lock (sentReliableCommands)
			{
				sentReliableCommands.Clear();
			}
		}
		lock (channelArray)
		{
			EnetChannel[] array = channelArray;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].clearAll();
			}
		}
		peerConnectionState = ConnectionStateValue.Disconnecting;
		if (PhotonSocket != null)
		{
			PhotonSocket.Disconnect();
		}
		DatagramEncryptedConnection = false;
		peerConnectionState = ConnectionStateValue.Disconnected;
		if (queueStatusChangeCallback)
		{
			EnqueueStatusCallback(StatusCode.TimeoutDisconnect);
			EnqueueStatusCallback(StatusCode.Disconnect);
		}
		else
		{
			base.Listener.OnStatusChanged(StatusCode.TimeoutDisconnect);
			base.Listener.OnStatusChanged(StatusCode.Disconnect);
		}
	}

	internal override void FetchServerTimestamp()
	{
		if (peerConnectionState != ConnectionStateValue.Connected || !ApplicationIsInitialized)
		{
			if ((int)base.LogLevel >= 3)
			{
				EnqueueDebugReturn(LogLevel.Info, $"FetchServerTimestamp() was skipped, as the client is not connected. Current ConnectionState: {peerConnectionState}");
			}
		}
		else
		{
			CreateAndEnqueueCommand(12, null, byte.MaxValue);
		}
	}

	/// <summary>Handles the incoming commands which are not executed yet in the CommandQueue.</summary>
	/// <remarks>
	/// Must be called in DispatchIncomingCommands and SendOutgoingCommands as that queue contains ACKs (which prevent re-sends).
	/// ACKs may set SendWindowUpdateRequired.
	/// </remarks>
	private void DispatchCommandQueue()
	{
		NCommand tmp = null;
		while (CommandQueue.TryDequeue(out tmp))
		{
			ExecuteCommand(tmp);
		}
	}

	/// <summary>
	/// Checks the incoming queue and Dispatches received data if possible.
	/// </summary>
	/// <returns>If a Dispatch happened or not, which shows if more Dispatches might be needed.</returns>
	internal override bool DispatchIncomingCommands()
	{
		DispatchCommandQueue();
		if (SendWindowUpdateRequired)
		{
			base.Stats.UdpReliableCommandsInFlight = sentReliableCommands.Count;
		}
		while (true)
		{
			MyAction action;
			lock (ActionQueue)
			{
				if (ActionQueue.Count <= 0)
				{
					break;
				}
				action = ActionQueue.Dequeue();
				goto IL_005d;
			}
			IL_005d:
			action();
		}
		NCommand command = null;
		lock (channelArray)
		{
			for (int index = 0; index < channelArray.Length; index++)
			{
				EnetChannel channel = channelArray[index];
				if (channel.incomingUnsequencedCommandsList.Count > 0)
				{
					command = channel.incomingUnsequencedCommandsList.Dequeue();
					break;
				}
				if (channel.incomingUnreliableCommandsList.Count > 0)
				{
					int lowestAvailableUnreliableCommandNumber = int.MaxValue;
					foreach (int sequenceNumber in channel.incomingUnreliableCommandsList.Keys)
					{
						NCommand cmd = channel.incomingUnreliableCommandsList[sequenceNumber];
						if (sequenceNumber < channel.incomingUnreliableSequenceNumber || cmd.reliableSequenceNumber < channel.incomingReliableSequenceNumber)
						{
							photonPeer.CountDiscarded++;
							commandsToRemove.Enqueue(sequenceNumber);
						}
						else if (sequenceNumber < lowestAvailableUnreliableCommandNumber && cmd.reliableSequenceNumber <= channel.incomingReliableSequenceNumber)
						{
							lowestAvailableUnreliableCommandNumber = sequenceNumber;
						}
					}
					NonAllocDictionary<int, NCommand> incomingUnreliableCommandsList = channel.incomingUnreliableCommandsList;
					while (commandsToRemove.Count > 0)
					{
						int keyToRemove = commandsToRemove.Dequeue();
						NCommand item = incomingUnreliableCommandsList[keyToRemove];
						incomingUnreliableCommandsList.Remove(keyToRemove);
						item.FreePayload();
						nCommandPool.Release(item);
					}
					if (lowestAvailableUnreliableCommandNumber < int.MaxValue)
					{
						photonPeer.DeltaUnreliableNumber = lowestAvailableUnreliableCommandNumber - channel.incomingUnreliableSequenceNumber;
						command = channel.incomingUnreliableCommandsList[lowestAvailableUnreliableCommandNumber];
					}
					if (command != null)
					{
						channel.incomingUnreliableCommandsList.Remove(command.unreliableSequenceNumber);
						channel.incomingUnreliableSequenceNumber = command.unreliableSequenceNumber;
						break;
					}
				}
				if (command != null || channel.incomingReliableCommandsList.Count <= 0)
				{
					continue;
				}
				lock (channel)
				{
					channel.incomingReliableCommandsList.TryGetValue(channel.incomingReliableSequenceNumber + 1, out command);
					if (command == null)
					{
						continue;
					}
					if (command.commandType != 8)
					{
						channel.incomingReliableSequenceNumber = command.reliableSequenceNumber;
						channel.incomingReliableCommandsList.Remove(command.reliableSequenceNumber);
					}
					else if (command.fragmentsRemaining > 0)
					{
						command = null;
					}
					else
					{
						channel.incomingReliableSequenceNumber = command.reliableSequenceNumber + command.fragmentCount - 1;
						channel.incomingReliableCommandsList.Remove(command.reliableSequenceNumber);
					}
					break;
				}
			}
		}
		if (command != null && command.Payload != null)
		{
			ByteCountCurrentDispatch = command.Size;
			DeserializeMessageAndCallback(command.Payload);
			command.FreePayload();
			nCommandPool.Release(command);
			return true;
		}
		return false;
	}

	/// <summary>Gets the target size for fragments.</summary>
	/// <remarks>
	/// Caches the result for a specific MTU value.
	/// Fragment length is different, when datagram encryption is used (so this caches two values in fact).
	/// </remarks>
	/// <returns></returns>
	private int GetFragmentLength()
	{
		if (fragmentLength == 0 || base.mtu != fragmentLengthMtuValue)
		{
			fragmentLengthMtuValue = base.mtu;
			fragmentLength = base.mtu - 12 - 36;
			fragmentLengthDatagramEncrypt = ((photonPeer.Encryptor != null) ? photonPeer.Encryptor.CalculateFragmentLength() : 0);
		}
		if (!DatagramEncryptedConnection)
		{
			return fragmentLength;
		}
		return fragmentLengthDatagramEncrypt;
	}

	private int CalculatePacketSize(int inSize)
	{
		if (DatagramEncryptedConnection)
		{
			return photonPeer.Encryptor.CalculateEncryptedSize(inSize + 7);
		}
		return inSize;
	}

	private int CalculateInitialOffset()
	{
		if (DatagramEncryptedConnection)
		{
			return 5;
		}
		int result = 12;
		if (photonPeer.CrcEnabled)
		{
			result += 4;
		}
		return result;
	}

	/// <summary></summary>
	/// <returns></returns>
	internal override bool SendAcksOnly()
	{
		return SendOutgoingCommands(sendAcksOnly: true);
	}

	internal override bool SendOutgoingCommands()
	{
		return SendOutgoingCommands(sendAcksOnly: false);
	}

	/// <summary>Fills one datagram to send outgoing commands (acks, ping, commands sent by game logic).</summary>
	/// <param name="sendAcksOnly">Skips sending "new" commands and does not time out, when true. Default: false.</param>
	/// <returns>True if there are commands not sent within the current datagram.</returns>
	internal bool SendOutgoingCommands(bool sendAcksOnly)
	{
		if (peerConnectionState == ConnectionStateValue.Disconnected)
		{
			return false;
		}
		if (PhotonSocket == null || !PhotonSocket.Connected)
		{
			return false;
		}
		int remainingCommands = 0;
		udpBufferIndex = CalculateInitialOffset();
		udpCommandCount = 0;
		timeIntCurrentSend = base.timeInt;
		lock (outgoingAcknowledgementsPool)
		{
			if (outgoingAcknowledgementsPool.Length > 0 || useAck2)
			{
				remainingCommands = SerializeAckToBuffer();
				base.Stats.LastSendAckTimestamp = timeIntCurrentSend;
			}
		}
		DispatchCommandQueue();
		if (timeIntCurrentSend > timeoutInt && sentReliableCommands.Count > 0)
		{
			int timeoutIntNext = timeIntCurrentSend + 50;
			lock (sentReliableCommands)
			{
				int didNotFitCount = 0;
				for (int index = 0; index < sentReliableCommands.Count; index++)
				{
					NCommand command = sentReliableCommands[index];
					int commandRoundTripTimeout = command.commandSentTime + command.roundTripTimeout;
					if (timeIntCurrentSend > commandRoundTripTimeout)
					{
						if (!sendAcksOnly && (command.commandSentCount > photonPeer.MaxResends || timeIntCurrentSend > command.timeoutTime))
						{
							if ((int)base.LogLevel >= 3)
							{
								base.Listener.DebugReturn(LogLevel.Info, $"Timeout-disconnect! Command: {command.ToString(full: true)} now: {timeIntCurrentSend} challenge: {Convert.ToString(challenge, 16)}");
							}
							peerConnectionState = ConnectionStateValue.Zombie;
							EnqueueStatusCallback(StatusCode.TimeoutDisconnect);
							Disconnect();
							nCommandPool.Release(command);
							return false;
						}
						_ = command.commandSentTime;
						_ = command.roundTripTimeout;
						if (SerializeCommandToBuffer(command, commandIsInSentQueue: true))
						{
							if ((int)base.LogLevel >= 4)
							{
								base.Listener.DebugReturn(LogLevel.Debug, $"Resending: {command.ToString(full: true)}.  repeat after: {command.roundTripTimeout} rtt/var: {roundTripTime}/{roundTripTimeVariance} last recv: {base.timeInt - photonPeer.Stats.LastReceiveTimestamp}  now: {timeIntCurrentSend}");
							}
							base.Stats.UdpReliableCommandsResent++;
						}
						else
						{
							didNotFitCount++;
							timeoutIntNext = timeoutInt;
							if (base.mtu - udpBufferIndex < 80)
							{
								break;
							}
						}
					}
					else if (commandRoundTripTimeout < timeoutIntNext)
					{
						timeoutIntNext = commandRoundTripTimeout;
					}
				}
				remainingCommands += didNotFitCount;
				timeoutInt = timeoutIntNext;
			}
		}
		if (!sendAcksOnly)
		{
			if (peerConnectionState == ConnectionStateValue.Connected && base.PingInterval > 0 && sentReliableCommands.Count == 0 && timeIntCurrentSend - timeLastAckReceive > base.PingInterval && CalculatePacketSize(udpBufferIndex + 12) <= base.mtu)
			{
				NCommand pingNCommand = nCommandPool.Acquire();
				pingNCommand.Initialize(this, 5, null, byte.MaxValue);
				QueueOutgoingReliableCommand(pingNCommand);
			}
			base.Stats.LastSendOutgoingTimestamp = base.timeInt;
			if (SendWindowUpdateRequired)
			{
				UpdateSendWindow();
			}
			lock (channelArray)
			{
				for (int i = 0; i < channelArray.Length; i++)
				{
					EnetChannel channel = channelArray[i];
					lock (channel)
					{
						int channelSequenceLimit = channel.lowestUnacknowledgedSequenceNumber + photonPeer.SendWindowSize;
						remainingCommands += SerializeToBuffer(channel.outgoingReliableCommandsList, channelSequenceLimit);
						remainingCommands += SerializeToBuffer(channel.outgoingUnreliableCommandsList, channelSequenceLimit);
					}
				}
			}
		}
		if (udpCommandCount <= 0)
		{
			return false;
		}
		SendData(udpBuffer, udpBufferIndex);
		base.Stats.UdpReliableCommandsInFlight = sentReliableCommands.Count;
		return remainingCommands > 0;
	}

	private void UpdateSendWindow()
	{
		SendWindowUpdateRequired = false;
		if (photonPeer.SendWindowSize <= 0)
		{
			return;
		}
		if (sentReliableCommands.Count == 0)
		{
			lock (channelArray)
			{
				for (int c = 0; c < channelArray.Length; c++)
				{
					EnetChannel obj = channelArray[c];
					obj.reliableCommandsInFlight = 0;
					obj.lowestUnacknowledgedSequenceNumber = obj.highestReceivedAck + 1;
				}
				return;
			}
		}
		channelsToUpdateLowestSent.Clear();
		lock (channelArray)
		{
			for (int i = 0; i < channelArray.Length; i++)
			{
				EnetChannel chan = channelArray[i];
				if (chan.ChannelNumber != byte.MaxValue && chan.reliableCommandsInFlight > 0)
				{
					channelsToUpdateLowestSent.Add(chan.ChannelNumber);
				}
			}
		}
		if (lowestSentSequenceNumber == null || lowestSentSequenceNumber.Length != channelArray.Length)
		{
			lowestSentSequenceNumber = new int[channelArray.Length];
		}
		else
		{
			for (int j = 0; j < lowestSentSequenceNumber.Length; j++)
			{
				lowestSentSequenceNumber[j] = 0;
			}
		}
		lock (sentReliableCommands)
		{
			for (int sentIndex = 0; sentIndex < sentReliableCommands.Count; sentIndex++)
			{
				NCommand command = sentReliableCommands[sentIndex];
				if (command.IsFlaggedUnsequenced || command.commandChannelID == byte.MaxValue)
				{
					continue;
				}
				int channelIndex = command.commandChannelID;
				if (channelsToUpdateLowestSent.Contains(command.commandChannelID))
				{
					if (lowestSentSequenceNumber[channelIndex] == 0)
					{
						lowestSentSequenceNumber[channelIndex] = command.reliableSequenceNumber;
					}
					channelsToUpdateLowestSent.Remove(command.commandChannelID);
					if (channelsToUpdateLowestSent.Count == 0)
					{
						break;
					}
				}
			}
		}
		lock (channelArray)
		{
			for (int index = 0; index < channelArray.Length; index++)
			{
				EnetChannel chan2 = channelArray[index];
				chan2.lowestUnacknowledgedSequenceNumber = ((lowestSentSequenceNumber[index] > 0) ? lowestSentSequenceNumber[index] : (chan2.highestReceivedAck + 1));
			}
		}
	}

	/// <summary>
	/// Checks connected state and channel before operation is serialized and enqueued for sending.
	/// </summary>
	/// <returns>if operation could be enqueued</returns>
	internal override bool EnqueuePhotonMessage(StreamBuffer opBytes, SendOptions sendParams)
	{
		byte commandType = 7;
		if (sendParams.DeliveryMode == DeliveryMode.UnreliableUnsequenced)
		{
			commandType = 11;
		}
		else if (sendParams.DeliveryMode == DeliveryMode.ReliableUnsequenced)
		{
			commandType = 14;
		}
		else if (sendParams.DeliveryMode == DeliveryMode.Reliable)
		{
			commandType = 6;
		}
		return CreateAndEnqueueCommand(commandType, opBytes, sendParams.Channel);
	}

	/// <summary>reliable-udp-level function to send some byte[] to the server via un/reliable command</summary>
	/// <remarks>only called when a custom operation should be send</remarks>
	/// <returns>the invocation ID for this operation (the payload)</returns>
	internal bool CreateAndEnqueueCommand(byte commandType, StreamBuffer payload, byte channelNumber)
	{
		EnetChannel channel = GetChannel(channelNumber);
		ByteCountLastOperation = 0;
		int currentFragmentSize = GetFragmentLength();
		if (currentFragmentSize == 0)
		{
			currentFragmentSize = 1000;
			EnqueueDebugReturn(LogLevel.Warning, "Value of currentFragmentSize should not be 0. Corrected to 1000.");
		}
		if (payload == null || payload.Length <= currentFragmentSize)
		{
			NCommand command = nCommandPool.Acquire();
			command.Initialize(this, commandType, payload, channel.ChannelNumber);
			if (command.IsFlaggedReliable)
			{
				QueueOutgoingReliableCommand(command);
			}
			else
			{
				QueueOutgoingUnreliableCommand(command);
			}
			ByteCountLastOperation = command.Size;
		}
		else
		{
			bool unsequenced = commandType == 14 || commandType == 11;
			int fragmentCount = (payload.Length + currentFragmentSize - 1) / currentFragmentSize;
			int startSequenceNumber = (unsequenced ? channel.outgoingReliableUnsequencedNumber : channel.outgoingReliableSequenceNumber) + 1;
			byte[] tempFragment = payload.GetBuffer();
			int fragmentNumber = 0;
			for (int fragmentOffset = 0; fragmentOffset < payload.Length; fragmentOffset += currentFragmentSize)
			{
				if (payload.Length - fragmentOffset < currentFragmentSize)
				{
					currentFragmentSize = payload.Length - fragmentOffset;
				}
				StreamBuffer fragmentSb = PeerBase.MessageBufferPool.Acquire();
				fragmentSb.Write(tempFragment, fragmentOffset, currentFragmentSize);
				NCommand command2 = nCommandPool.Acquire();
				command2.Initialize(this, (byte)(unsequenced ? 15 : 8), fragmentSb, channel.ChannelNumber);
				command2.fragmentNumber = fragmentNumber;
				command2.startSequenceNumber = startSequenceNumber;
				command2.fragmentCount = fragmentCount;
				command2.totalLength = payload.Length;
				command2.fragmentOffset = fragmentOffset;
				QueueOutgoingReliableCommand(command2);
				ByteCountLastOperation += command2.Size;
				base.Stats.UdpFragmentsOut++;
				fragmentNumber++;
			}
			PeerBase.MessageBufferPool.Release(payload);
		}
		return true;
	}

	internal int SerializeAckToBuffer()
	{
		if (useAck2)
		{
			if (peerConnectionState != ConnectionStateValue.Connected)
			{
				return 0;
			}
			lock (channelArray)
			{
				for (int index = 0; index < channelArray.Length; index++)
				{
					EnetChannel channel = channelArray[index];
					int completeSequenceNumber = 0;
					bool isSequenced = true;
					if (channel.GetGapBlock(out completeSequenceNumber, gapBlocks, isSequenced))
					{
						for (int i = 0; i < gapBlocks.Length; i++)
						{
							int block = gapBlocks[i];
							if (block != 0 || i <= 0)
							{
								NCommand.CreateAck2(udpBuffer, udpBufferIndex, channel.ChannelNumber, completeSequenceNumber, block, (byte)i, serverSentTime, isSequenced);
								udpBufferIndex += 20;
								udpCommandCount++;
							}
						}
					}
					isSequenced = false;
					if (!channel.GetGapBlock(out completeSequenceNumber, gapBlocks, isSequenced))
					{
						continue;
					}
					for (int j = 0; j < gapBlocks.Length; j++)
					{
						int block2 = gapBlocks[j];
						if (block2 != 0 || j <= 0)
						{
							NCommand.CreateAck2(udpBuffer, udpBufferIndex, channel.ChannelNumber, completeSequenceNumber, block2, (byte)j, serverSentTime, isSequenced);
							udpBufferIndex += 20;
							udpCommandCount++;
						}
					}
				}
			}
			return 0;
		}
		outgoingAcknowledgementsPool.Seek(0L, SeekOrigin.Begin);
		while (outgoingAcknowledgementsPool.Position + 20 <= outgoingAcknowledgementsPool.Length && CalculatePacketSize(udpBufferIndex + 20) <= base.mtu)
		{
			Buffer.BlockCopy(outgoingAcknowledgementsPool.GetBufferAndAdvance(20, out var offset), offset, udpBuffer, udpBufferIndex, 20);
			udpBufferIndex += 20;
			udpCommandCount++;
		}
		outgoingAcknowledgementsPool.Compact();
		outgoingAcknowledgementsPool.Position = outgoingAcknowledgementsPool.Length;
		return outgoingAcknowledgementsPool.Length / 20;
	}

	internal int SerializeToBuffer(List<NCommand> commandList, int channelSequenceLimit)
	{
		if (commandList.Count == 0)
		{
			return 0;
		}
		int i = 0;
		int skippedByWindow = 0;
		while (i < commandList.Count)
		{
			NCommand command = commandList[i];
			if (command.IsFlaggedReliable && !command.IsFlaggedUnsequenced && photonPeer.SendWindowSize > 0 && command.commandChannelID != byte.MaxValue && command.reliableSequenceNumber >= channelSequenceLimit && command.startSequenceNumber >= channelSequenceLimit)
			{
				i++;
				skippedByWindow++;
				continue;
			}
			if (!SerializeCommandToBuffer(command))
			{
				break;
			}
			commandList.RemoveAt(i);
		}
		throttledBySendWindow += skippedByWindow;
		return commandList.Count - skippedByWindow;
	}

	private bool SerializeCommandToBuffer(NCommand command, bool commandIsInSentQueue = false)
	{
		if (command == null)
		{
			return true;
		}
		if (CalculatePacketSize(udpBufferIndex + command.Size) > base.mtu)
		{
			return false;
		}
		command.SerializeHeader(udpBuffer, ref udpBufferIndex);
		if (command.SizeOfPayload > 0)
		{
			Buffer.BlockCopy(command.Serialize(), 0, udpBuffer, udpBufferIndex, command.SizeOfPayload);
			udpBufferIndex += command.SizeOfPayload;
		}
		udpCommandCount++;
		if (command.IsFlaggedReliable)
		{
			QueueSentCommand(command, commandIsInSentQueue);
		}
		else
		{
			command.FreePayload();
			nCommandPool.Release(command);
		}
		return true;
	}

	internal void SendData(byte[] data, int length)
	{
		try
		{
			if (DatagramEncryptedConnection)
			{
				SendDataEncrypted(data, length);
				return;
			}
			int offset = 0;
			MessageProtocol.Serialize(peerID, data, ref offset);
			data[2] = (byte)(photonPeer.CrcEnabled ? 204 : 0);
			data[3] = udpCommandCount;
			offset = 4;
			MessageProtocol.Serialize(timeIntCurrentSend, data, ref offset);
			MessageProtocol.Serialize(challenge, data, ref offset);
			if (photonPeer.CrcEnabled)
			{
				MessageProtocol.Serialize(0, data, ref offset);
				uint value = SupportClass.CalculateCrc(data, length);
				offset -= 4;
				MessageProtocol.Serialize((int)value, data, ref offset);
			}
			base.Stats.BytesOut = base.Stats.BytesOut + length;
			base.Stats.PackagesOut++;
			SendToSocket(data, length);
		}
		catch (Exception ex)
		{
			if ((int)base.LogLevel >= 1)
			{
				base.Listener.DebugReturn(LogLevel.Error, "SendData() caught exception: " + ex.ToString());
			}
			SupportClass.WriteStackTrace(ex);
		}
	}

	private void SendToSocket(byte[] data, int length)
	{
		ITrafficRecorder rec = photonPeer.TrafficRecorder;
		if (rec != null && rec.Enabled)
		{
			rec.Record(data, length, incoming: false, peerID, PhotonSocket);
		}
		PhotonSocket.Send(data, length);
	}

	private void SendDataEncrypted(byte[] data, int length)
	{
		if (bufferForEncryption == null || bufferForEncryption.Length != base.mtu)
		{
			bufferForEncryption = new byte[base.mtu];
		}
		byte[] outData = bufferForEncryption;
		int outOffset = 0;
		MessageProtocol.Serialize(peerID, outData, ref outOffset);
		outData[2] = 1;
		outOffset++;
		MessageProtocol.Serialize(challenge, outData, ref outOffset);
		data[0] = udpCommandCount;
		int clearTextOffset = 1;
		MessageProtocol.Serialize(timeIntCurrentSend, data, ref clearTextOffset);
		int outSize = outData.Length - outOffset;
		photonPeer.Encryptor.Encrypt2(data, length, outData, outData, outOffset, ref outSize);
		SendToSocket(outData, outSize + outOffset);
	}

	internal void QueueSentCommand(NCommand command, bool commandIsAlreadyInSentQueue = false)
	{
		command.commandSentTime = timeIntCurrentSend;
		if (command.roundTripTimeout == 0)
		{
			command.roundTripTimeout = Math.Min(roundTripTime + 4 * roundTripTimeVariance, photonPeer.InitialResendTimeMax);
			base.bestRoundtripTimeout = command.roundTripTimeout;
			command.timeoutTime = timeIntCurrentSend + base.DisconnectTimeout;
			base.Stats.UdpReliableCommandsSent++;
		}
		else if (command.commandSentCount > photonPeer.QuickResendAttempts || sentReliableCommands.Count >= 25)
		{
			command.roundTripTimeout *= 2;
		}
		command.commandSentCount++;
		int resendTime = command.commandSentTime + command.roundTripTimeout;
		if (resendTime < timeoutInt)
		{
			timeoutInt = resendTime;
		}
		if (!commandIsAlreadyInSentQueue)
		{
			GetChannel(command.commandChannelID).reliableCommandsInFlight++;
			lock (sentReliableCommands)
			{
				sentReliableCommands.Add(command);
			}
		}
	}

	internal void QueueOutgoingReliableCommand(NCommand command)
	{
		EnetChannel channel = GetChannel(command.commandChannelID);
		lock (channel)
		{
			if (command.reliableSequenceNumber == 0)
			{
				if (command.IsFlaggedUnsequenced)
				{
					command.reliableSequenceNumber = ++channel.outgoingReliableUnsequencedNumber;
				}
				else
				{
					command.reliableSequenceNumber = ++channel.outgoingReliableSequenceNumber;
				}
			}
			channel.outgoingReliableCommandsList.Add(command);
		}
	}

	internal void QueueOutgoingUnreliableCommand(NCommand command)
	{
		EnetChannel channel = GetChannel(command.commandChannelID);
		lock (channel)
		{
			if (command.IsFlaggedUnsequenced)
			{
				command.reliableSequenceNumber = 0;
				command.unsequencedGroupNumber = ++outgoingUnsequencedGroupNumber;
			}
			else
			{
				command.reliableSequenceNumber = channel.outgoingReliableSequenceNumber;
				command.unreliableSequenceNumber = ++channel.outgoingUnreliableSequenceNumber;
			}
			if (!photonPeer.SendInCreationOrder)
			{
				channel.outgoingUnreliableCommandsList.Add(command);
			}
			else
			{
				channel.outgoingReliableCommandsList.Add(command);
			}
		}
	}

	internal void QueueOutgoingAcknowledgement(NCommand readCommand, int sendTime)
	{
		if (useAck2)
		{
			lock (channelArray)
			{
				EnetChannel channel = GetChannel(readCommand.commandChannelID);
				if (channel != null)
				{
					lock (channel)
					{
						channel.Received(readCommand);
						return;
					}
				}
				return;
			}
		}
		lock (outgoingAcknowledgementsPool)
		{
			NCommand.CreateAck(outgoingAcknowledgementsPool.GetBufferAndAdvance(20, out var offset), offset, readCommand, sendTime);
		}
	}

	/// <summary>reads incoming udp-packages to create and queue incoming commands*</summary>
	internal override void ReceiveIncomingCommands(byte[] inBuff, int inDataLength)
	{
		int val = base.timeInt;
		photonPeer.Stats.LastReceiveTimestamp = val;
		base.Stats.BytesIn += inDataLength;
		base.Stats.PackagesIn++;
		if (peerConnectionState == ConnectionStateValue.Disconnected)
		{
			return;
		}
		try
		{
			int readingOffset = 0;
			MessageProtocol.Deserialize(out short _, inBuff, ref readingOffset);
			byte flags = inBuff[readingOffset++];
			int inChallenge;
			byte commandCount;
			if (flags == 1)
			{
				if (photonPeer.Encryptor == null)
				{
					return;
				}
				MessageProtocol.Deserialize(out inChallenge, inBuff, ref readingOffset);
				if (inChallenge != challenge)
				{
					packetLossByChallenge++;
					return;
				}
				inBuff = photonPeer.Encryptor.Decrypt2(inBuff, readingOffset, inDataLength - readingOffset, inBuff, out var _);
				if (!DatagramEncryptedConnection)
				{
					DatagramEncryptedConnection = true;
					fragmentLength = 0;
				}
				readingOffset = 0;
				commandCount = inBuff[readingOffset++];
				MessageProtocol.Deserialize(out serverSentTime, inBuff, ref readingOffset);
			}
			else
			{
				if (DatagramEncryptedConnection)
				{
					if ((int)base.LogLevel >= 2)
					{
						EnqueueDebugReturn(LogLevel.Warning, "Ignored received package. Connection requires Datagram Encryption but received unencrypted datagram.");
					}
					return;
				}
				commandCount = inBuff[readingOffset++];
				MessageProtocol.Deserialize(out serverSentTime, inBuff, ref readingOffset);
				MessageProtocol.Deserialize(out inChallenge, inBuff, ref readingOffset);
				if (inChallenge != challenge)
				{
					packetLossByChallenge++;
					if (peerConnectionState != ConnectionStateValue.Disconnected && (int)base.LogLevel >= 4)
					{
						EnqueueDebugReturn(LogLevel.Debug, $"Ignored received package. Wrong challenge. Received: {inChallenge} local: {challenge}");
					}
					return;
				}
				if (flags == 204)
				{
					MessageProtocol.Deserialize(out int crc, inBuff, ref readingOffset);
					readingOffset -= 4;
					MessageProtocol.Serialize(0, inBuff, ref readingOffset);
					uint localCrc = SupportClass.CalculateCrc(inBuff, inDataLength);
					if (crc != (int)localCrc)
					{
						packetLossByCrc++;
						if (peerConnectionState != ConnectionStateValue.Disconnected && (int)base.LogLevel >= 4)
						{
							EnqueueDebugReturn(LogLevel.Debug, $"Ignored received package. Wrong CRC. Incoming:  {(uint)crc:X} local: {localCrc:X}");
						}
						return;
					}
				}
			}
			if (commandCount <= 0)
			{
				if ((int)base.LogLevel >= 4)
				{
					EnqueueDebugReturn(LogLevel.Debug, $"Ignored received package. No commands in package: {commandCount}.");
				}
				return;
			}
			for (int i = 0; i < commandCount; i++)
			{
				NCommand readCommand = nCommandPool.Acquire();
				readCommand.Initialize(this, inBuff, ref readingOffset, val);
				CommandQueue.Enqueue(readCommand);
				if (readCommand.IsFlaggedReliable)
				{
					QueueOutgoingAcknowledgement(readCommand, serverSentTime);
				}
			}
		}
		catch (Exception ex)
		{
			if ((int)base.LogLevel >= 1)
			{
				EnqueueDebugReturn(LogLevel.Error, $"ReceiveIncomingCommands caught exception: {ex}");
			}
			SupportClass.WriteStackTrace(ex);
		}
	}

	internal void ExecuteCommand(NCommand command)
	{
		switch (command.commandType)
		{
		case 2:
		case 5:
			nCommandPool.Release(command);
			break;
		case 4:
		{
			StatusCode reason = StatusCode.DisconnectByServerReasonUnknown;
			if (command.reservedByte == 1)
			{
				reason = StatusCode.DisconnectByServerLogic;
			}
			else if (command.reservedByte == 2)
			{
				reason = StatusCode.DisconnectByServerTimeout;
			}
			else if (command.reservedByte == 3)
			{
				reason = StatusCode.DisconnectByServerUserLimit;
			}
			if ((int)base.LogLevel >= 4)
			{
				base.Listener.DebugReturn(LogLevel.Debug, $"Disconnect received. Server: {base.ServerAddress} PeerId: {(ushort)peerID} rtt(var): {base.rttVarString} Reason byte: {command.reservedByte} peerConnectionState: {peerConnectionState}");
			}
			if (peerConnectionState != ConnectionStateValue.Disconnected && peerConnectionState != ConnectionStateValue.Disconnecting)
			{
				EnqueueStatusCallback(reason);
				Disconnect();
			}
			nCommandPool.Release(command);
			break;
		}
		case 1:
		case 16:
		{
			timeLastAckReceive = command.TimeOfReceive;
			SendWindowUpdateRequired = true;
			lastRoundTripTime = command.TimeOfReceive - command.ackReceivedSentTime;
			if (lastRoundTripTime < 0 || lastRoundTripTime > 10000)
			{
				if ((int)base.LogLevel >= 3)
				{
					EnqueueDebugReturn(LogLevel.Info, $"Measured lastRoundtripTime is suspicious: {lastRoundTripTime} for command: {command}");
				}
				lastRoundTripTime = roundTripTime * 4;
			}
			NCommand removedCommand = RemoveSentReliableCommand(command.ackReceivedReliableSequenceNumber, command.commandChannelID, command.commandType == 16);
			nCommandPool.Release(command);
			if (removedCommand == null)
			{
				break;
			}
			removedCommand.FreePayload();
			EnetChannel chan = GetChannel(removedCommand.commandChannelID);
			lock (chan)
			{
				if (removedCommand.reliableSequenceNumber > chan.highestReceivedAck)
				{
					chan.highestReceivedAck = removedCommand.reliableSequenceNumber;
				}
				chan.reliableCommandsInFlight--;
			}
			if (removedCommand.commandType == 12)
			{
				if (lastRoundTripTime <= roundTripTime)
				{
					serverTimeOffset = serverSentTime + (lastRoundTripTime >> 1) - base.timeInt;
					serverTimeOffsetIsAvailable = true;
				}
				else
				{
					FetchServerTimestamp();
				}
			}
			else
			{
				UpdateRoundTripTimeAndVariance(lastRoundTripTime);
				if (removedCommand.commandType == 4 && peerConnectionState == ConnectionStateValue.Disconnecting)
				{
					if ((int)base.LogLevel >= 4)
					{
						EnqueueDebugReturn(LogLevel.Debug, "Server ACKd this client's Disconnect command.");
					}
					EnqueueActionForDispatch(delegate
					{
						PhotonSocket.Disconnect();
					});
				}
				else if (removedCommand.commandType == 2 && lastRoundTripTime >= 0)
				{
					if (lastRoundTripTime <= 15)
					{
						roundTripTime = 15;
						roundTripTimeVariance = 5;
					}
					else
					{
						roundTripTime = lastRoundTripTime;
						base.bestRoundtripTimeout = (int)((float)roundTripTime * 1.5f);
					}
				}
			}
			nCommandPool.Release(removedCommand);
			break;
		}
		case 17:
		case 18:
		{
			timeLastAckReceive = command.TimeOfReceive;
			SendWindowUpdateRequired = true;
			lastRoundTripTime = command.TimeOfReceive - command.ackReceivedSentTime;
			if (lastRoundTripTime < 0 || lastRoundTripTime > 10000)
			{
				if ((int)base.LogLevel >= 3)
				{
					EnqueueDebugReturn(LogLevel.Info, $"Measured lastRoundtripTime is suspicious: {lastRoundTripTime} for command: {command}");
				}
				lastRoundTripTime = roundTripTime * 4;
			}
			UpdateRoundTripTimeAndVariance(lastRoundTripTime);
			int ackdCompleteUpTo = command.ackReceivedReliableSequenceNumber;
			uint ackdBeyondSequenceBits = (uint)command.reliableSequenceNumber;
			byte ackdSequenceBitsOffset = command.commandFlags;
			byte ackdChannelId = command.commandChannelID;
			bool ackIsUnsequenced = command.commandType == 18;
			EnetChannel ackdChannel = GetChannel(command.commandChannelID);
			_ = ackdChannel.highestReceivedAck;
			int bitsAckLowSequenceNumber = ackdCompleteUpTo + 1 + ackdSequenceBitsOffset * 32;
			int highestBitAck = bitsAckLowSequenceNumber;
			lock (sentReliableCommands)
			{
				toRemove.Clear();
				foreach (NCommand sentCmd in sentReliableCommands)
				{
					if (sentCmd.commandChannelID != ackdChannelId || sentCmd.IsFlaggedUnsequenced != ackIsUnsequenced)
					{
						continue;
					}
					if (sentCmd.reliableSequenceNumber <= ackdCompleteUpTo)
					{
						toRemove.Add(sentCmd);
						continue;
					}
					int seqDelta = sentCmd.reliableSequenceNumber - bitsAckLowSequenceNumber;
					if (seqDelta < 0 || seqDelta >= 32)
					{
						continue;
					}
					if (((ackdBeyondSequenceBits >> seqDelta) & 1) == 1)
					{
						toRemove.Add(sentCmd);
					}
					else if (sentCmd.reliableSequenceNumber < highestBitAck)
					{
						if (sentCmd.commandSentCount <= 3)
						{
							sentCmd.roundTripTimeout = base.bestRoundtripTimeout;
						}
						timeoutInt = 0;
					}
				}
				foreach (NCommand removeCmd in toRemove)
				{
					sentReliableCommands.Remove(removeCmd);
					if (removeCmd.commandType != 2 && removeCmd.commandType != 4)
					{
						_ = removeCmd.commandType;
						_ = 12;
					}
					removeCmd.FreePayload();
					nCommandPool.Release(removeCmd);
				}
				if (ackdCompleteUpTo > ackdChannel.highestReceivedAck)
				{
					ackdChannel.highestReceivedAck = ackdCompleteUpTo;
				}
				break;
			}
		}
		case 6:
		case 7:
		case 11:
		case 14:
			if (peerConnectionState != ConnectionStateValue.Connected || !QueueIncomingCommand(command))
			{
				nCommandPool.Release(command);
			}
			break;
		case 8:
		case 15:
		{
			if (peerConnectionState != ConnectionStateValue.Connected)
			{
				nCommandPool.Release(command);
				break;
			}
			if (command.fragmentNumber > command.fragmentCount || command.fragmentOffset >= command.totalLength || command.fragmentOffset + command.Payload.Length > command.totalLength)
			{
				if ((int)base.LogLevel >= 1)
				{
					base.Listener.DebugReturn(LogLevel.Error, $"Received fragment has bad size: {command}");
				}
				nCommandPool.Release(command);
				break;
			}
			bool isSequencedFragment = command.commandType == 8;
			EnetChannel channel = GetChannel(command.commandChannelID);
			NCommand startCommand = null;
			lock (channel)
			{
				bool foundStartCommand = channel.TryGetFragment(command.startSequenceNumber, isSequencedFragment, out startCommand);
				if (foundStartCommand && startCommand.fragmentsRemaining <= 0)
				{
					nCommandPool.Release(command);
					break;
				}
				if (!QueueIncomingCommand(command))
				{
					nCommandPool.Release(command);
					break;
				}
				base.Stats.UdpFragmentsIn++;
				if (command.reliableSequenceNumber != command.startSequenceNumber)
				{
					if (foundStartCommand)
					{
						startCommand.fragmentsRemaining--;
					}
				}
				else
				{
					startCommand = command;
					startCommand.fragmentsRemaining--;
					NCommand fragment = null;
					int fragmentSequenceNumber = command.startSequenceNumber + 1;
					while (startCommand.fragmentsRemaining > 0 && fragmentSequenceNumber < startCommand.startSequenceNumber + startCommand.fragmentCount)
					{
						if (channel.TryGetFragment(fragmentSequenceNumber++, isSequencedFragment, out fragment))
						{
							startCommand.fragmentsRemaining--;
						}
					}
				}
				if (startCommand == null || startCommand.fragmentsRemaining > 0)
				{
					break;
				}
				StreamBuffer completeSB = PeerBase.MessageBufferPool.Acquire();
				completeSB.Position = 0;
				completeSB.SetCapacityMinimum(startCommand.totalLength);
				byte[] completePayload = completeSB.GetBuffer();
				for (int number = startCommand.startSequenceNumber; number < startCommand.startSequenceNumber + startCommand.fragmentCount; number++)
				{
					if (channel.TryGetFragment(number, isSequencedFragment, out var fragment2))
					{
						Buffer.BlockCopy(fragment2.Payload.GetBuffer(), 0, completePayload, fragment2.fragmentOffset, fragment2.Payload.Length);
						fragment2.FreePayload();
						channel.RemoveFragment(fragment2.reliableSequenceNumber, isSequencedFragment);
						if (fragment2.fragmentNumber > 0)
						{
							nCommandPool.Release(fragment2);
						}
						continue;
					}
					throw new Exception("startCommand.fragmentsRemaining was 0 but not all fragments were found to be combined!");
				}
				completeSB.SetLength(startCommand.totalLength);
				startCommand.FreePayload();
				startCommand.Payload = completeSB;
				startCommand.Size = 12 * startCommand.fragmentCount + startCommand.totalLength;
				if (isSequencedFragment)
				{
					channel.incomingReliableCommandsList.Add(startCommand.startSequenceNumber, startCommand);
				}
				else
				{
					channel.incomingUnsequencedCommandsList.Enqueue(startCommand);
				}
				break;
			}
		}
		case 3:
			if (peerConnectionState == ConnectionStateValue.Connecting)
			{
				byte[] initBytes = WriteInitRequest();
				CreateAndEnqueueCommand(6, new StreamBuffer(initBytes), 0);
				if (photonPeer.RandomizeSequenceNumbers)
				{
					ApplyRandomizedSequenceNumbers();
				}
				peerConnectionState = ConnectionStateValue.Connected;
			}
			nCommandPool.Release(command);
			break;
		case 9:
		case 10:
		case 12:
		case 13:
		case 19:
			break;
		}
	}

	/// <summary>Queues incoming commands in the correct order as either unreliable, reliable or unsequenced.</summary>
	/// <returns>If queued or not.</returns>
	internal bool QueueIncomingCommand(NCommand command)
	{
		EnetChannel channel = GetChannel(command.commandChannelID);
		if (channel == null)
		{
			if ((int)base.LogLevel >= 1)
			{
				base.Listener.DebugReturn(LogLevel.Error, $"Received command for non-existing channel: {command.commandChannelID}");
			}
			return false;
		}
		if (command.IsFlaggedUnsequenced)
		{
			if (command.IsFlaggedReliable)
			{
				lock (channel)
				{
					return channel.QueueIncomingReliableUnsequenced(command);
				}
			}
			int unsequencedGroup = command.unsequencedGroupNumber;
			int index = command.unsequencedGroupNumber % 128;
			if (unsequencedGroup >= incomingUnsequencedGroupNumber + 128)
			{
				incomingUnsequencedGroupNumber = unsequencedGroup - index;
				for (int i = 0; i < unsequencedWindow.Length; i++)
				{
					unsequencedWindow[i] = 0;
				}
			}
			else if (unsequencedGroup < incomingUnsequencedGroupNumber || (unsequencedWindow[index / 32] & (1 << index % 32)) != 0)
			{
				return false;
			}
			unsequencedWindow[index / 32] |= 1 << index % 32;
			channel.incomingUnsequencedCommandsList.Enqueue(command);
			return true;
		}
		if (command.IsFlaggedReliable)
		{
			if (command.reliableSequenceNumber <= channel.incomingReliableSequenceNumber)
			{
				if ((int)base.LogLevel >= 4)
				{
					base.Listener.DebugReturn(LogLevel.Debug, $"Command {command} outdated. Sequence number is less than dispatched incomingReliableSequenceNumber: {channel.incomingReliableSequenceNumber}");
				}
				return false;
			}
			bool added = false;
			lock (channel)
			{
				added = channel.AddSequencedIfNew(command);
			}
			if (!added)
			{
				if ((int)base.LogLevel >= 4)
				{
					base.Listener.DebugReturn(LogLevel.Debug, $"Command was received before! Old/New: {channel.FetchReliableSequenceNumber(command.reliableSequenceNumber)}/{command} inReliableSeq#: {channel.incomingReliableSequenceNumber}");
				}
				return false;
			}
		}
		else
		{
			if (command.reliableSequenceNumber < channel.incomingReliableSequenceNumber)
			{
				photonPeer.CountDiscarded++;
				if ((int)base.LogLevel >= 4)
				{
					base.Listener.DebugReturn(LogLevel.Debug, "Incoming reliable-seq# < Dispatched-rel-seq#. not saved.");
				}
				return false;
			}
			if (command.unreliableSequenceNumber <= channel.incomingUnreliableSequenceNumber)
			{
				photonPeer.CountDiscarded++;
				if ((int)base.LogLevel >= 4)
				{
					base.Listener.DebugReturn(LogLevel.Debug, "Incoming unreliable-seq# < Dispatched-unrel-seq#. not saved.");
				}
				return false;
			}
			bool added2 = false;
			lock (channel)
			{
				added2 = channel.AddSequencedIfNew(command);
			}
			if (!added2)
			{
				if ((int)base.LogLevel >= 4)
				{
					base.Listener.DebugReturn(LogLevel.Debug, $"Command was received before! Old/New: {channel.FetchReliableSequenceNumber(command.reliableSequenceNumber)}/{command} inReliableSeq#: {channel.incomingReliableSequenceNumber}");
				}
				return false;
			}
		}
		return true;
	}

	/// <summary>removes commands which are acknowledged</summary>
	internal NCommand RemoveSentReliableCommand(int ackReceivedReliableSequenceNumber, int ackReceivedChannel, bool isUnsequenced)
	{
		NCommand found = null;
		lock (sentReliableCommands)
		{
			foreach (NCommand cmd in sentReliableCommands)
			{
				if (cmd != null && cmd.reliableSequenceNumber == ackReceivedReliableSequenceNumber && cmd.commandChannelID == ackReceivedChannel && cmd.IsFlaggedUnsequenced == isUnsequenced)
				{
					found = cmd;
					break;
				}
			}
			if (found != null)
			{
				sentReliableCommands.Remove(found);
			}
			else if ((int)base.LogLevel >= 4 && peerConnectionState != ConnectionStateValue.Connected && peerConnectionState != ConnectionStateValue.Disconnecting)
			{
				EnqueueDebugReturn(LogLevel.Debug, $"No sent command for ACK (Ch: {ackReceivedReliableSequenceNumber} Sq#: {ackReceivedChannel}). PeerState: {peerConnectionState}.");
			}
		}
		return found;
	}

	internal string CommandListToString(NCommand[] list)
	{
		if ((int)base.LogLevel < 4)
		{
			return string.Empty;
		}
		StringBuilder tmp = new StringBuilder();
		for (int i = 0; i < list.Length; i++)
		{
			tmp.Append(i + "=");
			tmp.Append(list[i]);
			tmp.Append(" # ");
		}
		return tmp.ToString();
	}
}
