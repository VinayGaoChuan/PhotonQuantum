using System;

namespace Photon.Client;

/// <summary>Snapshot of connection statistics at a specific time. Useful to calculate changes over time (e.g. to get BytesIn for each second).</summary>
public class TrafficStatsSnapshot : TrafficStatsBase
{
	public long SnapshotTimestamp;

	[Obsolete("Use SnapshotTimestamp (without incorrect uppercase Stamp).")]
	public long SnapshotTimeStamp
	{
		get
		{
			return SnapshotTimestamp;
		}
		set
		{
			SnapshotTimestamp = value;
		}
	}

	public TrafficStatsSnapshot(TrafficStats ts, long timestamp)
		: base(ts)
	{
		SnapshotTimestamp = timestamp;
	}
}
