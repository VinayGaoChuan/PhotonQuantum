namespace Photon.Client;

/// <summary>Base class for traffic statistics. This contains the values that can be snapshot and compared.</summary>
public class TrafficStatsBase
{
	/// <summary>Count of received bytes (excluding all transport layer headers).</summary>
	public long BytesIn { get; internal set; }

	/// <summary>Count of sent bytes (excluding all transport layer headers).</summary>
	public long BytesOut { get; internal set; }

	/// <summary>Count of received packages / datagrams.</summary>
	public int PackagesIn { get; internal set; }

	/// <summary>Count of sent packages / datagrams.</summary>
	public int PackagesOut { get; internal set; }

	/// <summary>Count of incoming Fragment commands (data split into multiple commands due to being larger than mtu size).</summary>
	public int UdpFragmentsIn { get; internal set; }

	/// <summary>Count of outgoing Fragment commands (data split into multiple commands due to being larger than mtu size).</summary>
	public int UdpFragmentsOut { get; internal set; }

	/// <summary>Count of reliable commands sent so far (total on all channels). Does not include resends.</summary>
	public int UdpReliableCommandsSent { get; internal set; }

	/// <summary>Count of reliable commands which got repeated (due to local repeat-timing before an ACK was received).</summary>
	public int UdpReliableCommandsResent { get; internal set; }

	/// <summary>Count of reliable commands sent but not yet acknowledged by the receiver / server.</summary>
	public int UdpReliableCommandsInFlight { get; internal set; }

	/// <summary>Count of calls of DispatchIncomingCommands.</summary>
	public int DispatchIncomingCommandsCalls { get; internal set; }

	/// <summary>Count of calls of SendOutgoingCommands.</summary>
	public int SendOutgoingCommandsCalls { get; internal set; }

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
	public long RoundtripTime { get; internal set; }

	/// <summary>Changes of the roundtriptime as variance value. Gives a hint about how much the time is changing.</summary>
	public long RoundtripTimeVariance { get; internal set; }

	/// <summary>The last measured roundtrip time for this connection.</summary>
	public long LastRoundtripTime { get; internal set; }

	/// <summary>Default constructor which leaves all values at default.</summary>
	public TrafficStatsBase()
	{
	}

	/// <summary>Creates a new instance, copying the given origin's values.</summary>
	/// <param name="origin">Provides initial values for the new TrafficStatsBase (copy).</param>
	public TrafficStatsBase(TrafficStatsBase origin = null)
	{
		BytesIn = origin.BytesIn;
		BytesOut = origin.BytesOut;
		PackagesIn = origin.PackagesIn;
		PackagesOut = origin.PackagesOut;
		UdpFragmentsIn = origin.UdpFragmentsIn;
		UdpFragmentsOut = origin.UdpFragmentsOut;
		UdpReliableCommandsSent = origin.UdpReliableCommandsSent;
		UdpReliableCommandsResent = origin.UdpReliableCommandsResent;
		UdpReliableCommandsInFlight = origin.UdpReliableCommandsInFlight;
		DispatchIncomingCommandsCalls = origin.DispatchIncomingCommandsCalls;
		SendOutgoingCommandsCalls = origin.SendOutgoingCommandsCalls;
		RoundtripTime = origin.RoundtripTime;
		RoundtripTimeVariance = origin.RoundtripTimeVariance;
		LastRoundtripTime = origin.LastRoundtripTime;
	}
}
