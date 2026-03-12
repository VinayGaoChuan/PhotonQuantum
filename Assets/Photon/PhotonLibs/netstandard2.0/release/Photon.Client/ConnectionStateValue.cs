namespace Photon.Client;

/// <summary>
/// This is the replacement for the const values used in eNet like: PS_DISCONNECTED, PS_CONNECTED, etc.
/// </summary>
public enum ConnectionStateValue : byte
{
	/// <summary>No connection is available. Use connect.</summary>
	Disconnected = 0,
	/// <summary>Establishing a connection already. The app should wait for a status callback.</summary>
	Connecting = 1,
	/// <summary>
	/// The low level connection with Photon is established. On connect, the library will automatically
	/// send an Init package to select the application it connects to (see also PhotonPeer.Connect()).
	/// When the Init is done, IPhotonPeerListener.OnStatusChanged() is called with connect.
	/// </summary>
	/// <remarks>Please note that calling operations is only possible after the OnStatusChanged() with StatusCode.Connect.</remarks>
	Connected = 3,
	/// <summary>Connection going to be ended. Wait for status callback.</summary>
	Disconnecting = 4,
	/// <summary>Acknowledging a disconnect from Photon. Wait for status callback.</summary>
	AcknowledgingDisconnect = 5,
	/// <summary>Connection not properly disconnected.</summary>
	Zombie = 6
}
