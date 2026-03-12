namespace Photon.Client;

/// <summary>
/// Interface for (UDP) traffic capturing.
/// </summary>
public interface ITrafficRecorder
{
	/// <summary>Indicates if the PhotonPeer should call Record or not.</summary>
	bool Enabled { get; set; }

	/// <summary>Implement to record network traffic. Called by PhotonPeer for each UDP message sent and received.</summary>
	/// <remarks>
	/// The buffer will not contain Ethernet Header, IP, UDP level data. Only the payload received by the client.
	///
	/// It is advised to not use NetworkSimulation when recording traffic.
	/// The recording is done on the timing of actual receive- and send-calls and internal simulation would offset the timing.
	/// </remarks>
	/// <param name="inBuffer">Buffer to be sent or received. Check length value for actual content length.</param>
	/// <param name="length">Length of the network data.</param>
	/// <param name="incoming">Indicates incoming (true) or outgoing (false) traffic.</param>
	/// <param name="peerId">The local peerId for the connection. Defaults to 0xFFFF until assigned by the Server.</param>
	/// <param name="connection">The currently used PhotonSocket of this Peer. Enables you to track the connection endpoint.</param>
	void Record(byte[] inBuffer, int length, bool incoming, short peerId, PhotonSocket connection);
}
