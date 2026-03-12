using System.Diagnostics;

namespace Photon.Client;

/// <summary>
/// A simulation item is an action that can be queued to simulate network lag.
/// </summary>
internal class SimulationItem
{
	/// <summary>With this, the actual delay can be measured, compared to the intended lag.</summary>
	internal readonly Stopwatch stopw;

	/// <summary>Timestamp after which this item must be executed.</summary>
	public int TimeToExecute;

	/// <summary>Action to execute when the lag-time passed.</summary>
	public byte[] DelayedData;

	public int Delay { get; internal set; }

	/// <summary>Starts a new Stopwatch</summary>
	public SimulationItem()
	{
		stopw = new Stopwatch();
		stopw.Start();
	}
}
