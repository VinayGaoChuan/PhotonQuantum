using System;

namespace Photon.Client;

/// <summary> Internal class for "commands" - the package in which operations are sent.</summary>
internal class NCommand : IComparable<NCommand>
{
	internal const byte FeatureFlagsLow = 0;

	internal const byte FV_UNRELIABLE = 0;

	internal const byte FV_RELIABLE = 1;

	internal const byte FV_UNRELIABLE_UNSEQUENCED = 2;

	internal const byte FV_RELIBALE_UNSEQUENCED = 3;

	internal const byte CT_NONE = 0;

	internal const byte CT_ACK = 1;

	internal const byte CT_CONNECT = 2;

	internal const byte CT_VERIFYCONNECT = 3;

	internal const byte CT_DISCONNECT = 4;

	internal const byte CT_PING = 5;

	internal const byte CT_SENDRELIABLE = 6;

	internal const byte CT_SENDUNRELIABLE = 7;

	internal const byte CT_SENDFRAGMENT = 8;

	internal const byte CT_SENDUNSEQUENCED = 11;

	internal const byte CT_EG_SERVERTIME = 12;

	internal const byte CT_EG_SEND_UNRELIABLE_PROCESSED = 13;

	internal const byte CT_EG_SEND_RELIABLE_UNSEQUENCED = 14;

	internal const byte CT_EG_SEND_FRAGMENT_UNSEQUENCED = 15;

	internal const byte CT_EG_ACK_UNSEQUENCED = 16;

	internal const byte CT_EG_ACK_2 = 17;

	internal const byte CT_EG_ACK_2_UNSEQUENCED = 18;

	internal const byte CT_EG_ACK_2_NULL = 19;

	internal const int HEADER_UDP_PACK_LENGTH = 12;

	internal const int CmdSizeMinimum = 12;

	internal const int CmdSizeAck = 20;

	internal const int CmdSizeConnect = 44;

	internal const int CmdSizeVerifyConnect = 44;

	internal const int CmdSizeDisconnect = 12;

	internal const int CmdSizePing = 12;

	internal const int CmdSizeReliableHeader = 12;

	internal const int CmdSizeUnreliableHeader = 16;

	internal const int CmdSizeUnsequensedHeader = 16;

	internal const int CmdSizeFragmentHeader = 32;

	internal const int CmdSizeMaxHeader = 36;

	internal byte commandFlags;

	internal byte commandType;

	internal byte commandChannelID;

	internal int reliableSequenceNumber;

	internal int unreliableSequenceNumber;

	internal int unsequencedGroupNumber;

	internal byte reservedByte = 4;

	internal int startSequenceNumber;

	internal int fragmentCount;

	internal int fragmentNumber;

	internal int totalLength;

	internal int fragmentOffset;

	internal int fragmentsRemaining;

	internal int commandSentTime;

	internal byte commandSentCount;

	internal int roundTripTimeout;

	internal int timeoutTime;

	internal int ackReceivedReliableSequenceNumber;

	internal int ackReceivedSentTime;

	internal int TimeOfReceive;

	internal int Size;

	internal StreamBuffer Payload;

	/// <summary>Size of the Payload, which may be null.</summary>
	protected internal int SizeOfPayload
	{
		get
		{
			if (Payload == null)
			{
				return 0;
			}
			return Payload.Length;
		}
	}

	/// <summary>Checks commandFlags &amp; FV_UNRELIABLE_UNSEQUENCED.</summary>
	protected internal bool IsFlaggedUnsequenced => (commandFlags & 2) > 0;

	/// <summary>Checks commandFlags &amp; FV_RELIABLE &amp;&amp; this.commandType &lt; CT_EG_ACK_2.</summary>
	/// <remarks>ACK2 commands re-purpose the flags byte, so those never signal "is reliable" for ACK2.</remarks>
	protected internal bool IsFlaggedReliable
	{
		get
		{
			if ((commandFlags & 1) > 0)
			{
				return commandType < 17;
			}
			return false;
		}
	}

	/// <summary>
	///  ACKs should never be created as NCommand. use CreateACK to write the serialized ACK right away...
	/// </summary>
	/// <param name="buffer"></param>
	/// <param name="offset"></param>
	/// <param name="commandToAck"></param>
	/// <param name="sentTime"></param>
	/// <returns></returns>
	internal static void CreateAck(byte[] buffer, int offset, NCommand commandToAck, int sentTime)
	{
		buffer[offset++] = (byte)((!commandToAck.IsFlaggedUnsequenced) ? 1 : 16);
		buffer[offset++] = commandToAck.commandChannelID;
		buffer[offset++] = 0;
		buffer[offset++] = 4;
		MessageProtocol.Serialize(20, buffer, ref offset);
		MessageProtocol.Serialize(0, buffer, ref offset);
		MessageProtocol.Serialize(commandToAck.reliableSequenceNumber, buffer, ref offset);
		MessageProtocol.Serialize(sentTime, buffer, ref offset);
	}

	internal static void CreateAck2(byte[] buffer, int offset, byte channelId, int completeSequence, int gapBlock, byte gapBlockOffset, int sentTime, bool isSequenced)
	{
		buffer[offset++] = (byte)(isSequenced ? 17 : 18);
		buffer[offset++] = channelId;
		buffer[offset++] = gapBlockOffset;
		buffer[offset++] = 4;
		MessageProtocol.Serialize(20, buffer, ref offset);
		MessageProtocol.Serialize(gapBlock, buffer, ref offset);
		MessageProtocol.Serialize(completeSequence, buffer, ref offset);
		MessageProtocol.Serialize(sentTime, buffer, ref offset);
	}

	/// <summary>this variant does only create outgoing commands and increments . incoming ones are created from a DataInputStream</summary>
	internal void Initialize(EnetPeer peer, byte commandType, StreamBuffer payload, byte channel)
	{
		this.commandType = commandType;
		commandFlags = 1;
		commandChannelID = channel;
		Payload = payload;
		Size = 12;
		switch (this.commandType)
		{
		case 2:
		{
			Size = 44;
			byte[] payloadBytes = new byte[32];
			payloadBytes[0] = 0;
			payloadBytes[1] = 0;
			int mtuOffset = 2;
			MessageProtocol.Serialize((short)peer.mtu, payloadBytes, ref mtuOffset);
			payloadBytes[4] = 0;
			payloadBytes[5] = 0;
			payloadBytes[6] = 128;
			payloadBytes[7] = 0;
			payloadBytes[11] = peer.ChannelCount;
			payloadBytes[15] = 0;
			payloadBytes[19] = 0;
			payloadBytes[22] = 19;
			payloadBytes[23] = 136;
			payloadBytes[27] = 2;
			payloadBytes[31] = 2;
			Payload = new StreamBuffer(payloadBytes);
			break;
		}
		case 4:
			Size = 12;
			if (peer.peerConnectionState != ConnectionStateValue.Connected)
			{
				commandFlags = 2;
				reservedByte = (byte)((peer.peerConnectionState == ConnectionStateValue.Zombie) ? 2 : 4);
			}
			break;
		case 6:
			Size = 12 + payload.Length;
			break;
		case 14:
			Size = 12 + payload.Length;
			commandFlags = 3;
			break;
		case 7:
			Size = 16 + payload.Length;
			commandFlags = 0;
			break;
		case 11:
			Size = 16 + payload.Length;
			commandFlags = 2;
			break;
		case 8:
			Size = 32 + payload.Length;
			break;
		case 15:
			Size = 32 + payload.Length;
			commandFlags = 3;
			break;
		case 3:
		case 5:
		case 9:
		case 10:
		case 12:
		case 13:
			break;
		}
	}

	internal void Initialize(EnetPeer peer, byte[] inBuff, ref int readingOffset, int timeOfReceive)
	{
		commandType = inBuff[readingOffset++];
		commandChannelID = inBuff[readingOffset++];
		commandFlags = inBuff[readingOffset++];
		reservedByte = inBuff[readingOffset++];
		MessageProtocol.Deserialize(out Size, inBuff, ref readingOffset);
		MessageProtocol.Deserialize(out reliableSequenceNumber, inBuff, ref readingOffset);
		int payloadBytes = 0;
		TimeOfReceive = timeOfReceive;
		switch (commandType)
		{
		case 1:
		case 16:
		case 17:
		case 18:
			MessageProtocol.Deserialize(out ackReceivedReliableSequenceNumber, inBuff, ref readingOffset);
			MessageProtocol.Deserialize(out ackReceivedSentTime, inBuff, ref readingOffset);
			break;
		case 6:
		case 14:
			payloadBytes = Size - 12;
			break;
		case 7:
			MessageProtocol.Deserialize(out unreliableSequenceNumber, inBuff, ref readingOffset);
			payloadBytes = Size - 16;
			break;
		case 11:
			MessageProtocol.Deserialize(out unsequencedGroupNumber, inBuff, ref readingOffset);
			payloadBytes = Size - 16;
			break;
		case 8:
		case 15:
			MessageProtocol.Deserialize(out startSequenceNumber, inBuff, ref readingOffset);
			MessageProtocol.Deserialize(out fragmentCount, inBuff, ref readingOffset);
			MessageProtocol.Deserialize(out fragmentNumber, inBuff, ref readingOffset);
			MessageProtocol.Deserialize(out totalLength, inBuff, ref readingOffset);
			MessageProtocol.Deserialize(out fragmentOffset, inBuff, ref readingOffset);
			payloadBytes = Size - 32;
			fragmentsRemaining = fragmentCount;
			break;
		case 3:
		{
			MessageProtocol.Deserialize(out short outgoingPeerID, inBuff, ref readingOffset);
			readingOffset += 2;
			MessageProtocol.Deserialize(out short featureFlagsSigned, inBuff, ref readingOffset);
			readingOffset += 26;
			if (peer.peerID == -1 || peer.peerID == -2)
			{
				peer.peerID = outgoingPeerID;
			}
			peer.ServerFeatureFlags = (ushort)featureFlagsSigned;
			break;
		}
		default:
			readingOffset += Size - 12;
			break;
		}
		if (payloadBytes != 0)
		{
			StreamBuffer sb = PeerBase.MessageBufferPool.Acquire();
			sb.Write(inBuff, readingOffset, payloadBytes);
			Payload = sb;
			Payload.Position = 0;
			readingOffset += payloadBytes;
		}
	}

	public void Reset()
	{
		commandFlags = 0;
		commandType = 0;
		commandChannelID = 0;
		reliableSequenceNumber = 0;
		unreliableSequenceNumber = 0;
		unsequencedGroupNumber = 0;
		reservedByte = 4;
		startSequenceNumber = 0;
		fragmentCount = 0;
		fragmentNumber = 0;
		totalLength = 0;
		fragmentOffset = 0;
		fragmentsRemaining = 0;
		commandSentTime = 0;
		commandSentCount = 0;
		roundTripTimeout = 0;
		timeoutTime = 0;
		ackReceivedReliableSequenceNumber = 0;
		ackReceivedSentTime = 0;
		Size = 0;
	}

	internal void SerializeHeader(byte[] buffer, ref int bufferIndex)
	{
		buffer[bufferIndex++] = commandType;
		buffer[bufferIndex++] = commandChannelID;
		buffer[bufferIndex++] = commandFlags;
		buffer[bufferIndex++] = reservedByte;
		MessageProtocol.Serialize(Size, buffer, ref bufferIndex);
		MessageProtocol.Serialize(reliableSequenceNumber, buffer, ref bufferIndex);
		if (commandType == 7)
		{
			MessageProtocol.Serialize(unreliableSequenceNumber, buffer, ref bufferIndex);
		}
		else if (commandType == 11)
		{
			MessageProtocol.Serialize(unsequencedGroupNumber, buffer, ref bufferIndex);
		}
		else if (commandType == 8 || commandType == 15)
		{
			MessageProtocol.Serialize(startSequenceNumber, buffer, ref bufferIndex);
			MessageProtocol.Serialize(fragmentCount, buffer, ref bufferIndex);
			MessageProtocol.Serialize(fragmentNumber, buffer, ref bufferIndex);
			MessageProtocol.Serialize(totalLength, buffer, ref bufferIndex);
			MessageProtocol.Serialize(fragmentOffset, buffer, ref bufferIndex);
		}
	}

	internal byte[] Serialize()
	{
		return Payload.GetBuffer();
	}

	public void FreePayload()
	{
		if (Payload != null)
		{
			PeerBase.MessageBufferPool.Release(Payload);
		}
		Payload = null;
	}

	public int CompareTo(NCommand other)
	{
		if (other == null)
		{
			return 1;
		}
		int reliableDiff = reliableSequenceNumber - other.reliableSequenceNumber;
		if (IsFlaggedReliable || reliableDiff != 0)
		{
			return reliableDiff;
		}
		return unreliableSequenceNumber - other.unreliableSequenceNumber;
	}

	public override string ToString()
	{
		return ToString();
	}

	public string ToString(bool full = false)
	{
		string sequencing = (IsFlaggedUnsequenced ? "u" : "");
		if (unreliableSequenceNumber == 0)
		{
			if (full)
			{
				return $"{sequencing}{reliableSequenceNumber}/{commandChannelID}x{commandSentCount} (CMD {commandType} sent {commandSentTime} timeout {timeoutTime})";
			}
			return $"{sequencing}{reliableSequenceNumber}/{commandChannelID}";
		}
		if (full)
		{
			return $"{sequencing}{reliableSequenceNumber}.{unreliableSequenceNumber}/{commandChannelID}x{commandSentCount} (CMD {commandType} sent {commandSentTime} timeout {timeoutTime})";
		}
		return $"{sequencing}{reliableSequenceNumber}.{unreliableSequenceNumber}/{commandChannelID}";
	}
}
