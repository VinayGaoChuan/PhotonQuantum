using System.Threading;

namespace Photon.Client;

/// <summary>
/// A set of network simulation settings, enabled (and disabled) by PhotonPeer.IsSimulationEnabled.
/// </summary>
/// <remarks>
/// For performance reasons, the lag and jitter settings can't be produced exactly.
/// In some cases, the resulting lag will be up to 20ms bigger than the lag settings.
/// Even if all settings are 0, simulation will be used. Set PhotonPeer.IsSimulationEnabled
/// to false to disable it if no longer needed.
///
/// All lag, jitter and loss is additional to the current, real network conditions.
/// If the network is slow in reality, this will add even more lag.
/// The jitter values will affect the lag positive and negative, so the lag settings
/// describe the medium lag even with jitter. The jitter influence is: [-jitter..+jitter].
/// Packets "lost" due to OutgoingLossPercentage count for BytesOut and LostPackagesOut.
/// Packets "lost" due to IncomingLossPercentage count for BytesIn and LostPackagesIn.
/// </remarks>
public class NetworkSimulationSet
{
	/// <summary>internal</summary>
	private bool isSimulationEnabled;

	/// <summary>internal</summary>
	private int outgoingLag = 100;

	/// <summary>internal</summary>
	private int outgoingJitter;

	/// <summary>internal</summary>
	private int outgoingLossPercentage = 1;

	/// <summary>internal</summary>
	private int incomingLag = 100;

	/// <summary>internal</summary>
	private int incomingJitter;

	/// <summary>internal</summary>
	private int incomingLossPercentage = 1;

	internal PeerBase peerBase;

	private Thread netSimThread;

	protected internal readonly ManualResetEvent NetSimManualResetEvent = new ManualResetEvent(initialState: false);

	/// <summary>This setting overrides all other settings and turns simulation on/off. Default: false.</summary>
	protected internal bool IsSimulationEnabled
	{
		get
		{
			return isSimulationEnabled;
		}
		set
		{
			lock (NetSimManualResetEvent)
			{
				if (value == isSimulationEnabled)
				{
					return;
				}
				if (!value)
				{
					lock (peerBase.NetSimListIncoming)
					{
						foreach (SimulationItem item in peerBase.NetSimListIncoming)
						{
							if (peerBase.PhotonSocket != null && peerBase.PhotonSocket.Connected)
							{
								peerBase.ReceiveIncomingCommands(item.DelayedData, item.DelayedData.Length);
							}
						}
						peerBase.NetSimListIncoming.Clear();
					}
					lock (peerBase.NetSimListOutgoing)
					{
						foreach (SimulationItem item2 in peerBase.NetSimListOutgoing)
						{
							if (peerBase.PhotonSocket != null && peerBase.PhotonSocket.Connected)
							{
								peerBase.PhotonSocket.Send(item2.DelayedData, item2.DelayedData.Length);
							}
						}
						peerBase.NetSimListOutgoing.Clear();
					}
				}
				isSimulationEnabled = value;
				if (isSimulationEnabled)
				{
					if (netSimThread == null)
					{
						netSimThread = new Thread(peerBase.NetworkSimRun);
						netSimThread.IsBackground = true;
						netSimThread.Name = "netSim";
						netSimThread.Start();
					}
					NetSimManualResetEvent.Set();
				}
				else
				{
					NetSimManualResetEvent.Reset();
				}
			}
		}
	}

	/// <summary>Outgoing packages delay in ms. Default: 100.</summary>
	public int OutgoingLag
	{
		get
		{
			return outgoingLag;
		}
		set
		{
			outgoingLag = value;
		}
	}

	/// <summary>Randomizes OutgoingLag by [-OutgoingJitter..+OutgoingJitter]. Default: 0.</summary>
	public int OutgoingJitter
	{
		get
		{
			return outgoingJitter;
		}
		set
		{
			outgoingJitter = value;
		}
	}

	/// <summary>Percentage of outgoing packets that should be lost. Between 0..100. Default: 1. TCP ignores this setting.</summary>
	public int OutgoingLossPercentage
	{
		get
		{
			return outgoingLossPercentage;
		}
		set
		{
			outgoingLossPercentage = value;
		}
	}

	/// <summary>Incoming packages delay in ms. Default: 100.</summary>
	public int IncomingLag
	{
		get
		{
			return incomingLag;
		}
		set
		{
			incomingLag = value;
		}
	}

	/// <summary>Randomizes IncomingLag by [-IncomingJitter..+IncomingJitter]. Default: 0.</summary>
	public int IncomingJitter
	{
		get
		{
			return incomingJitter;
		}
		set
		{
			incomingJitter = value;
		}
	}

	/// <summary>Percentage of incoming packets that should be lost. Between 0..100. Default: 1. TCP ignores this setting.</summary>
	public int IncomingLossPercentage
	{
		get
		{
			return incomingLossPercentage;
		}
		set
		{
			incomingLossPercentage = value;
		}
	}

	/// <summary>Counts how many outgoing packages actually got lost. TCP connections ignore loss and this stays 0.</summary>
	public int LostPackagesOut { get; internal set; }

	/// <summary>Counts how many incoming packages actually got lost. TCP connections ignore loss and this stays 0.</summary>
	public int LostPackagesIn { get; internal set; }

	/// <summary>Provides an overview of the current values in form of a string.</summary>
	/// <returns>String summary.</returns>
	public override string ToString()
	{
		return string.Format("NetworkSimulationSet {6}.  Lag in={0} out={1}. Jitter in={2} out={3}. Loss in={4} out={5}.", incomingLag, outgoingLag, incomingJitter, outgoingJitter, incomingLossPercentage, outgoingLossPercentage, IsSimulationEnabled);
	}
}
