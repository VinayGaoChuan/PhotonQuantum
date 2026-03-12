namespace Photon.Client;

/// <summary>
/// Enumeration of situations that change the peers internal status.
/// Used in calls to OnStatusChanged to inform your application of various situations that might happen.
/// </summary>
/// <remarks>
/// Most of these codes are referenced somewhere else in the documentation when they are relevant to methods.
/// </remarks>
public enum StatusCode
{
	/// <summary>the PhotonPeer is connected.<br />See {@link IPhotonPeerListener#OnStatusChanged}*</summary>
	Connect = 1024,
	/// <summary>the PhotonPeer just disconnected.<br />See {@link IPhotonPeerListener#OnStatusChanged}*</summary>
	Disconnect = 1025,
	/// <summary>the PhotonPeer encountered an exception and will disconnect, too.<br />See {@link IPhotonPeerListener#OnStatusChanged}*</summary>
	Exception = 1026,
	/// <summary>Exception while opening the incoming connection to the server. Followed by Disconnect.</summary>
	/// <remarks>The server could be down / not running or the client has no network or a misconfigured DNS.<br />See {@link IPhotonPeerListener#OnStatusChanged}*</remarks>
	ExceptionOnConnect = 1023,
	/// <summary>Used when the server address looked like an IPv4/IPv6 but could not be parsed as such. A connection is not possible in this case.</summary>
	ServerAddressInvalid = 1050,
	/// <summary>Used when the client fails to resolve the server address via DNS lookup. Check client connectivity! Some platforms require capabilities defined for internet access.</summary>
	DnsExceptionOnConnect = 1051,
	/// <summary>Used on platforms that throw a security exception on connect. Unity3d does this, e.g., if a webplayer build could not fetch a policy-file from a remote server.</summary>
	SecurityExceptionOnConnect = 1022,
	/// <summary>Sending command failed. Either not connected, or the requested channel is bigger than the number of initialized channels.</summary>
	SendError = 1030,
	/// <summary>Exception, if a server cannot be connected. Followed by Disconnect.</summary>
	/// <remarks>Most likely, the server is not responding. Ask user to try again later.</remarks>
	ExceptionOnReceive = 1039,
	/// <summary>Disconnection due to a timeout (client did no longer receive ACKs from server). Followed by Disconnect.</summary>
	TimeoutDisconnect = 1040,
	/// <summary>Timeout disconnect by server. The server didn't receive necessary ACKs in time. Followed by Disconnect.</summary>
	DisconnectByServerTimeout = 1041,
	/// <summary>Disconnect by server due to concurrent user limit reached (received a disconnect command).</summary>
	DisconnectByServerUserLimit = 1042,
	/// <summary>(1043) Disconnect by server due to server's logic. Followed by Disconnect.</summary>
	DisconnectByServerLogic = 1043,
	/// <summary>Disconnect by server due to unspecified reason. Followed by Disconnect.</summary>
	DisconnectByServerReasonUnknown = 1044,
	/// <summary>(1048) Value for OnStatusChanged()-call, when the encryption-setup for secure communication finished successfully.</summary>
	EncryptionEstablished = 1048,
	/// <summary>(1049) Value for OnStatusChanged()-call, when the encryption-setup failed for some reason. Check debug logs.</summary>
	EncryptionFailedToEstablish = 1049
}
