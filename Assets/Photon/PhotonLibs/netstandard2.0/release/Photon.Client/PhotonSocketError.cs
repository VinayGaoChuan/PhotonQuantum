namespace Photon.Client;

public enum PhotonSocketError
{
	Success,
	Skipped,
	NoData,
	Exception,
	/// <summary>Data wasn't sent yet.</summary>
	Busy,
	/// <summary>Data is being sent async (still in use).</summary>
	PendingSend
}
