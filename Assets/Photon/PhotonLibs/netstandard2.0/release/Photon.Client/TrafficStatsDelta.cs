using System.Text;

namespace Photon.Client;

/// <summary>Delta values for TrafficStats, compared to some TrafficStatsSnapshot.</summary>
public class TrafficStatsDelta : TrafficStatsBase
{
	/// <summary>Timespan / delta time between the two snapshots.</summary>
	public long DeltaTime;

	internal new long RoundtripTime { get; set; }

	internal new long RoundtripTimeVariance { get; set; }

	internal new long LastRoundtripTime { get; set; }

	/// <summary>RoundtripTime of the reference (earlier) snapshot.</summary>
	public long ReferenceRoundtripTime { get; set; }

	/// <summary>RoundtripTimeVariance of the reference (earlier) snapshot.</summary>
	public long ReferenceRoundtripTimeVariance { get; set; }

	/// <summary>RoundtripTime of the later snapshot.</summary>
	public long LaterRoundtripTime { get; set; }

	/// <summary>RoundtripTimeVariance of the later snapshot.</summary>
	public long LaterRoundtripTimeVariance { get; set; }

	internal TrafficStatsDelta(TrafficStatsBase reference, TrafficStatsBase now)
	{
		base.BytesIn = now.BytesIn - reference.BytesIn;
		base.BytesOut = now.BytesOut - reference.BytesOut;
		base.PackagesIn = now.PackagesIn - reference.PackagesIn;
		base.PackagesOut = now.PackagesOut - reference.PackagesOut;
		base.UdpFragmentsIn = now.UdpFragmentsIn - reference.UdpFragmentsIn;
		base.UdpFragmentsOut = now.UdpFragmentsOut - reference.UdpFragmentsOut;
		base.UdpReliableCommandsSent = now.UdpReliableCommandsSent - reference.UdpReliableCommandsSent;
		base.UdpReliableCommandsResent = now.UdpReliableCommandsResent - reference.UdpReliableCommandsResent;
		base.UdpReliableCommandsInFlight = now.UdpReliableCommandsInFlight - reference.UdpReliableCommandsInFlight;
		base.DispatchIncomingCommandsCalls = now.DispatchIncomingCommandsCalls - reference.DispatchIncomingCommandsCalls;
		base.SendOutgoingCommandsCalls = now.SendOutgoingCommandsCalls - reference.SendOutgoingCommandsCalls;
		ReferenceRoundtripTime = reference.RoundtripTime;
		ReferenceRoundtripTimeVariance = reference.RoundtripTimeVariance;
		LaterRoundtripTime = now.RoundtripTime;
		LaterRoundtripTimeVariance = now.RoundtripTimeVariance;
	}

	/// <summary>Creates a TrafficStatsDelta instance for a given reference (earlier) snapshot and a later one (now).</summary>
	/// <param name="reference">Earlier snapshot to compare the newer one with.</param>
	/// <param name="now">Newer snapshot marking the end of the timespan to compare.</param>
	public TrafficStatsDelta(TrafficStatsSnapshot reference, TrafficStatsSnapshot now)
		: this((TrafficStatsBase)reference, (TrafficStatsBase)now)
	{
		DeltaTime = now.SnapshotTimestamp - reference.SnapshotTimestamp;
	}

	/// <summary>Creates a TrafficStatsDelta instance for a given reference (earlier) snapshot and newer TrafficStats (now).</summary>
	/// <param name="reference">Earlier snapshot to compare the newer one with.</param>
	/// <param name="now">Newer (likely current) stats, marking the end of the timespan to compare.</param>
	public TrafficStatsDelta(TrafficStatsSnapshot reference, TrafficStats now)
		: this((TrafficStatsBase)reference, (TrafficStatsBase)now)
	{
		DeltaTime = now.connectionStopwatch.ElapsedMilliseconds - reference.SnapshotTimestamp;
	}

	/// <summary>Provides a string representation of the delta values.</summary>
	/// <returns>ToString(true, true, true).</returns>
	public override string ToString()
	{
		return ToString(udpValues: true, rttValues: true, callValues: true);
	}

	/// <summary>Provides a string representation of the delta values.</summary>
	/// <param name="udpValues">Includes UDP related values (if any). Reliable commands, resends, in flight, fragments in and out.</param>
	/// <param name="rttValues">Includes Roundtrip Time and variance (from reference in the past and current values).</param>
	/// <param name="callValues">Includes how often DispatchIncomingCommands and SendOutgoingCommands have been called in the timespan.</param>
	/// <returns>String (e.g. for logging).</returns>
	public string ToString(bool udpValues = true, bool rttValues = false, bool callValues = false)
	{
		float elapsedSeconds = (float)DeltaTime / 1000f;
		StringBuilder sb = new StringBuilder();
		sb.AppendLine($"Delta elapsed: {elapsedSeconds:F3} sec.  Out: {base.BytesOut:N0} bytes -> {base.BytesOut / DeltaTime:N0} kB/sec.  In: {base.BytesIn:N0} bytes -> {base.BytesIn / DeltaTime:N0} kB/sec.\nPackages Out: {base.PackagesOut} In: {base.PackagesIn}");
		if (udpValues)
		{
			if (base.UdpReliableCommandsSent > 0)
			{
				sb.AppendLine($"Reliable commands out: {base.UdpReliableCommandsSent}  resent: {base.UdpReliableCommandsResent}  in flight: {base.UdpReliableCommandsInFlight}.");
			}
			if (base.UdpFragmentsIn > 0 || base.UdpFragmentsOut > 0)
			{
				sb.AppendLine($"Fragments out: {base.UdpFragmentsOut}  in: {base.UdpFragmentsIn}.");
			}
		}
		if (rttValues)
		{
			sb.AppendLine($"RTT/Variance from: {ReferenceRoundtripTime}/{LaterRoundtripTimeVariance}  to: {LaterRoundtripTime}/{LaterRoundtripTimeVariance}");
		}
		if (callValues && (base.DispatchIncomingCommandsCalls > 0 || base.SendOutgoingCommandsCalls > 0))
		{
			sb.AppendLine($"DispatchIncomingCommands calls: {base.DispatchIncomingCommandsCalls}  SendOutgoingCommands calls: {base.SendOutgoingCommandsCalls}.");
		}
		return sb.ToString();
	}
}
