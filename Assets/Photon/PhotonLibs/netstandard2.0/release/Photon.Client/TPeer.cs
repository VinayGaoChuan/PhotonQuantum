using System;
using System.Collections.Generic;

namespace Photon.Client;

internal class TPeer : PeerBase
{
	/// <summary>TCP "Package" header: 7 bytes</summary>
	internal const int TCP_HEADER_BYTES = 7;

	/// <summary>TCP "Message" header: 2 bytes</summary>
	internal const int MSG_HEADER_BYTES = 2;

	/// <summary>TCP header combined: 9 bytes</summary>
	public const int ALL_HEADER_BYTES = 9;

	private Queue<StreamBuffer> incomingList = new Queue<StreamBuffer>(32);

	internal List<StreamBuffer> outgoingStream;

	/// <summary>Timestamp of last ping activity (updated when sending to avoid sending again immediately).</summary>
	private int lastPingActivity;

	/// <summary>Re-used binary message used in the TCP protocol (WSS uses a Ping Operation due to lack of framing).</summary>
	private readonly byte[] pingRequest = new byte[5] { 240, 0, 0, 0, 0 };

	/// <summary>Re-used ping-Operation parameter-dict used in the WS/WSS protocol (TCP uses pingRequest).</summary>
	private readonly ParameterDictionary pingParamDict = new ParameterDictionary();

	internal static readonly byte[] tcpFramedMessageHead = new byte[9] { 251, 0, 0, 0, 0, 0, 0, 243, 2 };

	internal static readonly byte[] tcpMsgHead = new byte[2] { 243, 2 };

	/// <summary>Defines if the (TCP) socket implementation needs to do "framing".</summary>
	/// <remarks>The WebSocket protocol (e.g.) includes framing, so when that is used, we set DoFraming to false.</remarks>
	protected internal bool DoFraming = true;

	internal override int QueuedIncomingCommandsCount => incomingList.Count;

	internal override int QueuedOutgoingCommandsCount => outgoingStream.Count;

	internal TPeer()
	{
	}

	internal override bool IsTransportEncrypted()
	{
		return usedTransportProtocol == ConnectionProtocol.WebSocketSecure;
	}

	internal override void Reset()
	{
		base.Reset();
		peerID = (short)(SupportClass.ThreadSafeRandom.Next() % 32767);
		if (photonPeer.PayloadEncryptionSecret != null && usedTransportProtocol != ConnectionProtocol.WebSocketSecure)
		{
			InitEncryption(photonPeer.PayloadEncryptionSecret);
		}
		incomingList = new Queue<StreamBuffer>(32);
		base.Stats.LastReceiveTimestamp = base.timeInt;
	}

	internal override bool Connect(string serverAddress, string proxyServerAddress, string appID, object photonToken)
	{
		outgoingStream = new List<StreamBuffer>(8);
		messageHeader = (DoFraming ? tcpFramedMessageHead : tcpMsgHead);
		if (usedTransportProtocol == ConnectionProtocol.WebSocket || usedTransportProtocol == ConnectionProtocol.WebSocketSecure)
		{
			PhotonSocket.ConnectAddress = PrepareWebSocketUrl(serverAddress, appID, photonToken);
		}
		if (PhotonSocket.Connect())
		{
			peerConnectionState = ConnectionStateValue.Connecting;
			lastPingActivity = base.timeInt;
			if (DoFraming || PhotonToken != null)
			{
				byte[] initBytes = WriteInitRequest();
				EnqueueInit(initBytes);
			}
			return true;
		}
		return false;
	}

	private void Disconnect()
	{
		Disconnect(true);
	}

	internal override void Disconnect(bool queueStatusChangeCallback = true)
	{
		if (peerConnectionState != ConnectionStateValue.Disconnected && peerConnectionState != ConnectionStateValue.Disconnecting)
		{
			if ((int)base.LogLevel >= 4)
			{
				base.Listener.DebugReturn(LogLevel.Debug, "TPeer.Disconnect()");
			}
			peerConnectionState = ConnectionStateValue.Disconnecting;
			if (PhotonSocket != null)
			{
				PhotonSocket.Disconnect();
			}
			lock (incomingList)
			{
				incomingList.Clear();
			}
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
	}

	internal override void SimulateTimeoutDisconnect(bool queueStatusChangeCallback = true)
	{
		if (peerConnectionState != ConnectionStateValue.Disconnected && peerConnectionState != ConnectionStateValue.Disconnecting)
		{
			if ((int)base.LogLevel >= 4)
			{
				base.Listener.DebugReturn(LogLevel.Debug, "TPeer.Disconnect()");
			}
			peerConnectionState = ConnectionStateValue.Disconnecting;
			if (PhotonSocket != null)
			{
				PhotonSocket.Disconnect();
			}
			lock (incomingList)
			{
				incomingList.Clear();
			}
			peerConnectionState = ConnectionStateValue.Disconnected;
			if (queueStatusChangeCallback)
			{
				EnqueueStatusCallback(StatusCode.TimeoutDisconnect);
			}
			else
			{
				base.Listener.OnStatusChanged(StatusCode.TimeoutDisconnect);
			}
		}
	}

	internal override void FetchServerTimestamp()
	{
		if (peerConnectionState != ConnectionStateValue.Connected || !ApplicationIsInitialized)
		{
			if ((int)base.LogLevel >= 3)
			{
				base.Listener.DebugReturn(LogLevel.Info, $"FetchServerTimestamp() skipped. Client is not connected. Current ConnectionState: {peerConnectionState}");
			}
		}
		else
		{
			SendPing();
			serverTimeOffsetIsAvailable = false;
		}
	}

	private void EnqueueInit(byte[] initRequestBytes)
	{
		StreamBuffer bout = new StreamBuffer(initRequestBytes.Length + 32);
		byte[] tcpheader = new byte[7] { 251, 0, 0, 0, 0, 0, 1 };
		int offsetForLength = 1;
		MessageProtocol.Serialize(initRequestBytes.Length + tcpheader.Length, tcpheader, ref offsetForLength);
		bout.Write(tcpheader, 0, tcpheader.Length);
		bout.Write(initRequestBytes, 0, initRequestBytes.Length);
		EnqueueMessageAsPayload(DeliveryMode.Reliable, bout, 0);
	}

	/// <summary>
	/// Checks the incoming queue and Dispatches received data if possible. Returns if a Dispatch happened or
	/// not, which shows if more Dispatches might be needed.
	/// </summary>
	internal override bool DispatchIncomingCommands()
	{
		if (peerConnectionState == ConnectionStateValue.Connected && base.timeInt - base.Stats.LastReceiveTimestamp > base.DisconnectTimeout)
		{
			EnqueueStatusCallback(StatusCode.TimeoutDisconnect);
			EnqueueActionForDispatch(Disconnect);
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
				goto IL_0079;
			}
			IL_0079:
			action();
		}
		StreamBuffer payload;
		lock (incomingList)
		{
			if (incomingList.Count <= 0)
			{
				return false;
			}
			payload = incomingList.Dequeue();
		}
		ByteCountCurrentDispatch = payload.Length + 3;
		bool result = DeserializeMessageAndCallback(payload);
		PeerBase.MessageBufferPool.Release(payload);
		return result;
	}

	/// <summary>
	/// gathers commands from all (out)queues until udp-packet is full and sends it!
	/// </summary>
	internal override bool SendOutgoingCommands()
	{
		if (peerConnectionState == ConnectionStateValue.Disconnected)
		{
			return false;
		}
		if (!PhotonSocket.Connected)
		{
			return false;
		}
		base.Stats.LastSendOutgoingTimestamp = base.timeInt;
		timeIntCurrentSend = base.timeInt;
		if (base.timeInt - lastPingActivity > base.PingInterval)
		{
			SendPing();
		}
		lock (outgoingStream)
		{
			int count = 0;
			int sentBytes = 0;
			PhotonSocketError result = PhotonSocketError.Success;
			for (int i = 0; i < outgoingStream.Count; i++)
			{
				StreamBuffer msg = outgoingStream[i];
				result = SendData(msg.GetBuffer(), msg.Length);
				if (result == PhotonSocketError.Busy)
				{
					break;
				}
				sentBytes += msg.Length;
				count++;
				if (result != PhotonSocketError.PendingSend)
				{
					PeerBase.MessageBufferPool.Release(msg);
				}
				if (sentBytes >= base.mtu || result == PhotonSocketError.PendingSend)
				{
					break;
				}
			}
			outgoingStream.RemoveRange(0, count);
			if (result == PhotonSocketError.Busy || result == PhotonSocketError.PendingSend)
			{
				return false;
			}
			return outgoingStream.Count > 0;
		}
	}

	/// <summary>Sends a ping in intervals to keep connection alive (server will timeout connection if nothing is sent).</summary>
	/// <returns>Always false in this case (local queues are ignored. true would be: "call again to send remaining data").</returns>
	internal override bool SendAcksOnly()
	{
		if (PhotonSocket == null || !PhotonSocket.Connected)
		{
			return false;
		}
		if (peerConnectionState == ConnectionStateValue.Connected && base.timeInt - lastPingActivity > base.PingInterval)
		{
			SendPing(sendImmediately: true);
		}
		return false;
	}

	internal override bool EnqueuePhotonMessage(StreamBuffer opBytes, SendOptions sendParams)
	{
		return EnqueueMessageAsPayload(sendParams.DeliveryMode, opBytes, sendParams.Channel);
	}

	/// <summary>enqueues serialized operations to be sent as tcp stream / package</summary>
	internal bool EnqueueMessageAsPayload(DeliveryMode deliveryMode, StreamBuffer opMessage, byte channelId)
	{
		if (opMessage == null)
		{
			return false;
		}
		if (DoFraming)
		{
			byte[] msgBytes = opMessage.GetBuffer();
			int offsetForLength = 1;
			MessageProtocol.Serialize(opMessage.Length, msgBytes, ref offsetForLength);
			msgBytes[5] = channelId;
			switch (deliveryMode)
			{
			case DeliveryMode.Unreliable:
				msgBytes[6] = 0;
				break;
			case DeliveryMode.Reliable:
				msgBytes[6] = 1;
				break;
			case DeliveryMode.UnreliableUnsequenced:
				msgBytes[6] = 2;
				break;
			case DeliveryMode.ReliableUnsequenced:
				msgBytes[6] = 3;
				break;
			default:
				throw new ArgumentOutOfRangeException("DeliveryMode", deliveryMode, null);
			}
		}
		lock (outgoingStream)
		{
			outgoingStream.Add(opMessage);
		}
		ByteCountLastOperation = opMessage.Length;
		return true;
	}

	/// <summary>Queues a ping (operation or "message") and updates this.lastPingActivity to avoid another ping for a while.</summary>
	/// <param name="sendImmediately">Set to true to immediately send a Ping (not queueing it first). This is useful for SendAcksOnly() to keep the connection up.</param>
	internal void SendPing(bool sendImmediately = false)
	{
		int currentTimestamp = (lastPingActivity = base.timeInt);
		StreamBuffer pingMessage;
		if (!DoFraming)
		{
			lock (pingParamDict)
			{
				pingParamDict[1] = currentTimestamp;
				pingMessage = SerializeOperationToMessage(PhotonCodes.Ping, pingParamDict, EgMessageType.InternalOperationRequest, encrypt: false);
			}
		}
		else
		{
			int offset = 1;
			MessageProtocol.Serialize(currentTimestamp, pingRequest, ref offset);
			pingMessage = PeerBase.MessageBufferPool.Acquire();
			pingMessage.Write(pingRequest, 0, pingRequest.Length);
		}
		if (!sendImmediately)
		{
			EnqueuePhotonMessage(pingMessage, SendOptions.SendReliable);
		}
		else if (SendData(pingMessage.GetBuffer(), pingMessage.Length) == PhotonSocketError.Success)
		{
			PeerBase.MessageBufferPool.Release(pingMessage);
		}
	}

	internal PhotonSocketError SendData(byte[] data, int length)
	{
		PhotonSocketError result = PhotonSocketError.Success;
		try
		{
			int time = base.timeInt;
			result = PhotonSocket.Send(data, length);
			int delta = base.timeInt - time;
			if (delta > longestSendCall)
			{
				longestSendCall = delta;
			}
			if (result == PhotonSocketError.Success)
			{
				base.Stats.BytesOut += length;
				base.Stats.PackagesOut++;
			}
		}
		catch (Exception arg)
		{
			if ((int)base.LogLevel >= 1)
			{
				base.Listener.DebugReturn(LogLevel.Error, $"Caught exception in TPeer.SendData(): {arg}");
			}
		}
		return result;
	}

	/// <summary>reads incoming tcp-packages to create and queue incoming commands*</summary>
	/// <remarks>
	/// TCP Sockets have 9 bytes headers (except for ping messages, which are 9 bytes total).
	/// WebSockets have 0 bytes header (so dataLength == actual received payload bytes).
	/// See Confluence, "RTS Protocol".
	/// </remarks>
	internal override void ReceiveIncomingCommands(byte[] inbuff, int dataLength)
	{
		if (inbuff == null)
		{
			if ((int)base.LogLevel >= 1)
			{
				EnqueueDebugReturn(LogLevel.Error, "checkAndQueueIncomingCommands() inBuff: null");
			}
			return;
		}
		base.Stats.LastReceiveTimestamp = base.timeInt;
		base.Stats.BytesIn += dataLength;
		base.Stats.PackagesIn++;
		if (inbuff[0] == 243)
		{
			if (DoFraming)
			{
				base.Stats.BytesIn += 7L;
			}
			byte num = (byte)(inbuff[1] & 0x7F);
			byte opCode = inbuff[2];
			if (num != 7 || opCode != PhotonCodes.Ping)
			{
				StreamBuffer sb = PeerBase.MessageBufferPool.Acquire();
				sb.Write(inbuff, 0, dataLength);
				sb.Position = 0;
				lock (incomingList)
				{
					incomingList.Enqueue(sb);
					return;
				}
			}
			DeserializeMessageAndCallback(new StreamBuffer(inbuff));
		}
		else if (inbuff[0] == 240)
		{
			ReadPingResult(inbuff);
		}
		else if ((int)base.LogLevel >= 1 && dataLength > 0)
		{
			EnqueueDebugReturn(LogLevel.Error, $"ReceiveIncomingCommands MagicNumber should be 0xF0 or 0xF3. Is: {inbuff[0]} dataLength: {dataLength}");
		}
	}

	private void ReadPingResult(byte[] inbuff)
	{
		int serverSentTime = 0;
		int clientSentTime = 0;
		int offset = 1;
		MessageProtocol.Deserialize(out serverSentTime, inbuff, ref offset);
		MessageProtocol.Deserialize(out clientSentTime, inbuff, ref offset);
		lastRoundTripTime = base.timeInt - clientSentTime;
		if (!serverTimeOffsetIsAvailable)
		{
			roundTripTime = lastRoundTripTime;
		}
		UpdateRoundTripTimeAndVariance(lastRoundTripTime);
		if (!serverTimeOffsetIsAvailable)
		{
			serverTimeOffset = serverSentTime + (lastRoundTripTime >> 1) - base.timeInt;
			serverTimeOffsetIsAvailable = true;
		}
	}

	protected internal void ReadPingResult(OperationResponse operationResponse)
	{
		int serverSentTime = (int)operationResponse.Parameters[2];
		int clientSentTime = (int)operationResponse.Parameters[1];
		lastRoundTripTime = base.timeInt - clientSentTime;
		if (!serverTimeOffsetIsAvailable)
		{
			roundTripTime = lastRoundTripTime;
		}
		UpdateRoundTripTimeAndVariance(lastRoundTripTime);
		if (!serverTimeOffsetIsAvailable)
		{
			serverTimeOffset = serverSentTime + (lastRoundTripTime >> 1) - base.timeInt;
			serverTimeOffsetIsAvailable = true;
		}
	}
}
