namespace Photon.Client;

/// <summary>
/// These are the options that can be used as underlying transport protocol.
/// </summary>
public enum ConnectionProtocol : byte
{
	/// <summary>Use UDP to connect to Photon, which allows you to send operations reliable or unreliable on demand.</summary>
	Udp = 0,
	/// <summary>Use TCP to connect to Photon.</summary>
	Tcp = 1,
	/// <summary>A TCP-based protocol commonly supported by browsers.For WebGL games mostly. Note: No WebSocket PhotonSocket implementation is in this Assembly.</summary>
	/// <remarks>This protocol is only available in Unity exports to WebGL.</remarks>
	WebSocket = 4,
	/// <summary>A TCP-based, encrypted protocol commonly supported by browsers. For WebGL games mostly. Note: No WebSocket PhotonSocket implementation is in this Assembly.</summary>
	/// <remarks>This protocol is only available in Unity exports to WebGL.</remarks>
	WebSocketSecure = 5
}
