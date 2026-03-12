using System.Diagnostics;

namespace Photon.Client;

/// <summary>Statistics about the current connection (if any).</summary>
public class TrafficStats : TrafficStatsBase
{
	internal readonly Stopwatch connectionStopwatch;

	/// <summary>When the last call to SendOutgoingCommands happened in milliseconds since Connect().</summary>
	public int LastSendOutgoingTimestamp;

	/// <summary>The last ConnectionTime value, when some ACKs were sent out by this client.</summary>
	/// <remarks>Only applicable to UDP connections.</remarks>
	public int LastSendAckTimestamp;

	public int LastSendOutgoingDeltaTime => (int)(connectionStopwatch.ElapsedMilliseconds - LastSendOutgoingTimestamp);

	/// <summary>How long ago some ACKs were sent out by this client.</summary>
	public int LastSendAckDeltaTime => (int)(connectionStopwatch.ElapsedMilliseconds - LastSendAckTimestamp);

	/// <summary>
	/// Timestamp of the last time anything (!) was received from the server (including low level Ping, ACKs, events and operation-returns).
	/// </summary>
	/// <remarks>
	/// This is not the time when something was dispatched. If you enable NetworkSimulation, this value is affected as well.
	/// </remarks>
	public int LastReceiveTimestamp { get; internal set; }

	/// <summary>How long ago this client received anything (based on LastReceiveTimestamp).</summary>
	public int LastReceiveDeltaTime => (int)(connectionStopwatch.ElapsedMilliseconds - LastReceiveTimestamp);

	/// <summary>When the last call to DispatchIncomingCommands happened in milliseconds since Connect().</summary>
	public int LastDispatchTimestamp { get; internal set; }

	/// <summary>How long ago the last call to DispatchIncomingCommands happened (based on LastDispatchTimestamp).</summary>
	public int LastDispatchDeltaTime => (int)(connectionStopwatch.ElapsedMilliseconds - LastDispatchTimestamp);

	/// <summary>
	/// Gets longest time between subsequent calls to DispatchIncomingCommands in milliseconds.
	/// Note: This is not a crucial timing for the networking. Long gaps just add "local lag" to events that are available already.
	/// </summary>
	public int LongestDeltaBetweenDispatchCalls { get; internal set; }

	/// <summary>How long the last callback from DispatchIncomingCommands took in Milliseconds.</summary>
	public int LastDispatchDuration { get; internal set; }

	/// <summary>
	/// Gets longest time between subsequent calls to SendOutgoingCommands in milliseconds.
	/// Note: This is a crucial value for network stability. Without calling SendOutgoingCommands,
	/// nothing will be sent to the server, who might time out this client.
	/// </summary>
	public int LongestDeltaBetweenSendOutgoingCalls { get; internal set; }

	/// <summary>Creates a new TrafficStats instance, using the given Stopwatch.</summary>
	/// <param name="connectionTimeSw">Can't be null.</param>
	public TrafficStats(Stopwatch connectionTimeSw)
	{
		connectionStopwatch = connectionTimeSw;
	}

	/// <summary>Creates a new snapshot of the current stats.</summary>
	/// <returns>New snapshot.</returns>
	public TrafficStatsSnapshot ToSnapshot()
	{
		long timestamp = ((connectionStopwatch == null) ? 0 : connectionStopwatch.ElapsedMilliseconds);
		return new TrafficStatsSnapshot(this, timestamp);
	}

	/// <summary>Creates a new stats delta instance between current stats and the reference (current - reference).</summary>
	/// <returns>New TrafficStatsDelta (current - reference).</returns>
	public TrafficStatsDelta ToDelta(TrafficStatsSnapshot reference)
	{
		return new TrafficStatsDelta(reference, this);
	}

	internal void DispatchIncomingCommandsCalled(int timestamp)
	{
		if (LastDispatchTimestamp != 0)
		{
			int delta = timestamp - LastDispatchTimestamp;
			if (delta > LongestDeltaBetweenDispatchCalls)
			{
				LongestDeltaBetweenDispatchCalls = delta;
			}
		}
		base.DispatchIncomingCommandsCalls++;
		LastDispatchTimestamp = timestamp;
	}

	internal void SendOutgoingCommandsCalled(int timestamp)
	{
		if (LastSendOutgoingTimestamp != 0)
		{
			int delta = timestamp - LastSendOutgoingTimestamp;
			if (delta > LongestDeltaBetweenSendOutgoingCalls)
			{
				LongestDeltaBetweenSendOutgoingCalls = delta;
			}
		}
		base.SendOutgoingCommandsCalls++;
		LastSendOutgoingTimestamp = timestamp;
	}

	/// <summary>
	/// Resets the values that can be maxed out, like LongestDeltaBetweenDispatching. See remarks.
	/// </summary>
	/// <remarks>
	/// Set to 0: LongestDeltaBetweenDispatching, LongestDeltaBetweenSending, LongestEventCallback, LongestEventCallbackCode, LongestOpResponseCallback, LongestOpResponseCallbackOpCode.
	/// Also resets internal values: timeOfLastDispatchCall and timeOfLastSendCall (so intervals are tracked correctly).
	/// </remarks>
	public void ResetMaximumCounters()
	{
		LongestDeltaBetweenDispatchCalls = 0;
		LongestDeltaBetweenSendOutgoingCalls = 0;
		LastDispatchTimestamp = 0;
		LastSendOutgoingTimestamp = 0;
	}

	/// <summary>Provides a string representation of the current values.</summary>
	/// <returns>ToString(true).</returns>
	public override string ToString()
	{
		return ToString(extended: true);
	}

	/// <summary>Provides a string representation of the current stats values.</summary>
	/// <returns>String (e.g. for logging).</returns>
	public string ToString(bool extended)
	{
		string brief = ((base.UdpFragmentsIn <= 0 && base.UdpFragmentsOut <= 0) ? $"In: {base.BytesIn} bytes, {base.PackagesIn} packages.\nOut: {base.BytesOut} bytes, {base.PackagesOut} packages." : $"In: {base.BytesIn} bytes, {base.PackagesIn} packages, {base.UdpFragmentsIn} fragments.\nOut: {base.BytesOut} bytes, {base.PackagesOut} packages, {base.UdpFragmentsOut} fragments.");
		if (!extended)
		{
			return brief;
		}
		return brief + "\n" + $"Max time between Send: {LongestDeltaBetweenSendOutgoingCalls}ms, " + $"Dispatch: {LongestDeltaBetweenDispatchCalls}ms.  " + $"Send calls: {base.SendOutgoingCommandsCalls}.  " + $"Dispatch calls: {base.DispatchIncomingCommandsCalls}.";
	}
}
