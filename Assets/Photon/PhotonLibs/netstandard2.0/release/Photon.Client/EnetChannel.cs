using System.Collections.Generic;

namespace Photon.Client;

internal class EnetChannel
{
	public class ReceiveTrackingValues
	{
		internal bool ReceivedReliableCommandSincePreviousAck2;

		/// <summary>Stores the sequence numbers to ACK at some point. May include duplicates and is unordered. A bit-stream may be more effective here.</summary>
		internal HashSet<int> receivedReliableSequenceNumbers = new HashSet<int>();

		internal int reliableSequencedNumbersCompletelyReceived;

		internal int reliableSequencedNumbersHighestReceived;
	}

	internal byte ChannelNumber;

	internal NonAllocDictionary<int, NCommand> incomingReliableCommandsList;

	internal NonAllocDictionary<int, NCommand> incomingUnreliableCommandsList;

	internal Queue<NCommand> incomingUnsequencedCommandsList;

	internal NonAllocDictionary<int, NCommand> incomingUnsequencedFragments;

	internal List<NCommand> outgoingReliableCommandsList;

	internal List<NCommand> outgoingUnreliableCommandsList;

	internal int incomingReliableSequenceNumber;

	internal int incomingUnreliableSequenceNumber;

	internal int outgoingReliableSequenceNumber;

	internal int outgoingUnreliableSequenceNumber;

	/// <summary>Number for reliable unsequenced commands (separate from "standard" reliable sequenced). Used to avoid duplicates.</summary>
	internal int outgoingReliableUnsequencedNumber;

	/// <summary>The highest number of reliable unsequenced commands that arrived (and all commands before).</summary>
	private int reliableUnsequencedNumbersCompletelyReceived;

	/// <summary>Any reliable unsequenced number that's been received, which is higher than the current highest in complete sequence (reliableUnsequencedNumbersCompletelyReceived).</summary>
	private HashSet<int> reliableUnsequencedNumbersReceived = new HashSet<int>();

	/// <summary>To store the highest acknowledged sequence number (and get some impression what the server already received and stored).</summary>
	internal int highestReceivedAck;

	/// <summary>Count of reliable commands sent and not yet acknowledged.</summary>
	internal int reliableCommandsInFlight;

	/// <summary>The lowest sequence number sent but not acknowledged yet. This defines the sequence delta "window" for sending subsequent reliable commands.</summary>
	internal int lowestUnacknowledgedSequenceNumber;

	private ReceiveTrackingValues SequencedReceived = new ReceiveTrackingValues();

	private ReceiveTrackingValues UnsequencedReceived = new ReceiveTrackingValues();

	public EnetChannel(byte channelNumber, int commandBufferSize)
	{
		ChannelNumber = channelNumber;
		incomingReliableCommandsList = new NonAllocDictionary<int, NCommand>((uint)commandBufferSize);
		incomingUnreliableCommandsList = new NonAllocDictionary<int, NCommand>((uint)commandBufferSize);
		incomingUnsequencedCommandsList = new Queue<NCommand>();
		incomingUnsequencedFragments = new NonAllocDictionary<int, NCommand>();
		outgoingReliableCommandsList = new List<NCommand>(commandBufferSize);
		outgoingUnreliableCommandsList = new List<NCommand>(commandBufferSize);
	}

	public bool ContainsUnreliableSequenceNumber(int unreliableSequenceNumber)
	{
		return incomingUnreliableCommandsList.ContainsKey(unreliableSequenceNumber);
	}

	public NCommand FetchUnreliableSequenceNumber(int unreliableSequenceNumber)
	{
		return incomingUnreliableCommandsList[unreliableSequenceNumber];
	}

	public bool ContainsReliableSequenceNumber(int reliableSequenceNumber)
	{
		return incomingReliableCommandsList.ContainsKey(reliableSequenceNumber);
	}

	public bool AddSequencedIfNew(NCommand command)
	{
		NonAllocDictionary<int, NCommand> list = (command.IsFlaggedReliable ? incomingReliableCommandsList : incomingUnreliableCommandsList);
		int sequenceNumber = (command.IsFlaggedReliable ? command.reliableSequenceNumber : command.unreliableSequenceNumber);
		if (list.ContainsKey(sequenceNumber))
		{
			return false;
		}
		list.Add(sequenceNumber, command);
		return true;
	}

	public NCommand FetchReliableSequenceNumber(int reliableSequenceNumber)
	{
		return incomingReliableCommandsList[reliableSequenceNumber];
	}

	public bool TryGetFragment(int reliableSequenceNumber, bool isSequenced, out NCommand fragment)
	{
		if (isSequenced)
		{
			return incomingReliableCommandsList.TryGetValue(reliableSequenceNumber, out fragment);
		}
		return incomingUnsequencedFragments.TryGetValue(reliableSequenceNumber, out fragment);
	}

	public void RemoveFragment(int reliableSequenceNumber, bool isSequenced)
	{
		if (isSequenced)
		{
			incomingReliableCommandsList.Remove(reliableSequenceNumber);
		}
		else
		{
			incomingUnsequencedFragments.Remove(reliableSequenceNumber);
		}
	}

	public void clearAll()
	{
		lock (this)
		{
			SequencedReceived = new ReceiveTrackingValues();
			UnsequencedReceived = new ReceiveTrackingValues();
			incomingReliableCommandsList.Clear();
			incomingUnreliableCommandsList.Clear();
			incomingUnsequencedCommandsList.Clear();
			incomingUnsequencedFragments.Clear();
			outgoingReliableCommandsList.Clear();
			outgoingUnreliableCommandsList.Clear();
		}
	}

	/// <summary>Checks and queues incoming reliable unsequenced commands ("send" or "fragment"), if they haven't been received yet.</summary>
	/// <param name="command">The command to check and queue.</param>
	/// <returns>True if the command is new and got queued (or could be executed/dispatched).</returns>
	public bool QueueIncomingReliableUnsequenced(NCommand command)
	{
		if (command.reliableSequenceNumber <= reliableUnsequencedNumbersCompletelyReceived)
		{
			return false;
		}
		if (reliableUnsequencedNumbersReceived.Contains(command.reliableSequenceNumber))
		{
			return false;
		}
		if (command.reliableSequenceNumber == reliableUnsequencedNumbersCompletelyReceived + 1)
		{
			reliableUnsequencedNumbersCompletelyReceived++;
			while (reliableUnsequencedNumbersReceived.Contains(reliableUnsequencedNumbersCompletelyReceived + 1))
			{
				reliableUnsequencedNumbersCompletelyReceived++;
				reliableUnsequencedNumbersReceived.Remove(reliableUnsequencedNumbersCompletelyReceived);
			}
		}
		else
		{
			reliableUnsequencedNumbersReceived.Add(command.reliableSequenceNumber);
		}
		if (command.commandType == 15)
		{
			incomingUnsequencedFragments.Add(command.reliableSequenceNumber, command);
		}
		else
		{
			incomingUnsequencedCommandsList.Enqueue(command);
		}
		return true;
	}

	public void Received(NCommand inCommand)
	{
		int sequenceNumber = inCommand.reliableSequenceNumber;
		ReceiveTrackingValues receiveTracking = ((!inCommand.IsFlaggedUnsequenced) ? SequencedReceived : UnsequencedReceived);
		lock (receiveTracking)
		{
			receiveTracking.ReceivedReliableCommandSincePreviousAck2 = true;
			if (sequenceNumber > receiveTracking.reliableSequencedNumbersHighestReceived)
			{
				receiveTracking.reliableSequencedNumbersHighestReceived = sequenceNumber;
			}
			if (sequenceNumber == receiveTracking.reliableSequencedNumbersCompletelyReceived + 1)
			{
				receiveTracking.reliableSequencedNumbersCompletelyReceived++;
				while (receiveTracking.receivedReliableSequenceNumbers.Contains(receiveTracking.reliableSequencedNumbersCompletelyReceived + 1))
				{
					receiveTracking.reliableSequencedNumbersCompletelyReceived++;
					receiveTracking.receivedReliableSequenceNumbers.Remove(receiveTracking.reliableSequencedNumbersCompletelyReceived);
				}
			}
			else if (sequenceNumber > receiveTracking.reliableSequencedNumbersCompletelyReceived)
			{
				receiveTracking.receivedReliableSequenceNumbers.Add(sequenceNumber);
			}
		}
	}

	public bool GetGapBlock(out int completeSequenceNumber, int[] blocks, bool isSequenced = true)
	{
		ReceiveTrackingValues receiveTracking = (isSequenced ? SequencedReceived : UnsequencedReceived);
		lock (receiveTracking)
		{
			completeSequenceNumber = receiveTracking.reliableSequencedNumbersCompletelyReceived;
			bool changedCompleteSequence = receiveTracking.ReceivedReliableCommandSincePreviousAck2;
			receiveTracking.ReceivedReliableCommandSincePreviousAck2 = false;
			if (!changedCompleteSequence)
			{
				return false;
			}
			if (blocks == null)
			{
				blocks = new int[4];
			}
			int startOfGap = completeSequenceNumber + 1;
			int flagsWritten = 0;
			for (int b = 0; b < blocks.Length; b++)
			{
				blocks[b] = 0;
				int bitflags = 0;
				int blockSequenceStart = startOfGap + 32 * b;
				for (int i = 0; i < 32; i++)
				{
					int item = blockSequenceStart + i;
					if (receiveTracking.receivedReliableSequenceNumbers.Contains(item))
					{
						bitflags |= 1 << i;
						flagsWritten++;
						if (flagsWritten >= receiveTracking.receivedReliableSequenceNumbers.Count || item > receiveTracking.reliableSequencedNumbersHighestReceived)
						{
							break;
						}
					}
				}
				blocks[b] = bitflags;
			}
			return changedCompleteSequence;
		}
	}
}
