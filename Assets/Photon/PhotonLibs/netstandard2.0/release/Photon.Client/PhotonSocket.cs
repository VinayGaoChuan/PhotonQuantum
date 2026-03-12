using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Photon.Client;

public abstract class PhotonSocket
{
	protected internal PeerBase peerBase;

	/// <summary>The protocol for this socket, defined in constructor.</summary>
	protected readonly ConnectionProtocol Protocol;

	public bool PollReceive;

	/// <summary>Address, as defined via a Connect() call. Including protocol, port and or path.</summary>
	/// <remarks>This is set in the constructor and in Connect() again. Typically the address does not change after the PhotonSocket is instantiated.</remarks>
	public string ConnectAddress;

	protected IPhotonPeerListener Listener => peerBase.Listener;

	protected internal int MTU => peerBase.mtu;

	public PhotonSocketState State { get; protected set; }

	/// <summary>Socket implementations should store the SocketErrorCode here, if anything happens.</summary>
	public int SocketErrorCode { get; protected set; }

	public bool Connected => State == PhotonSocketState.Connected;

	/// <summary>Contains only the server's hostname (stripped protocol, port and or path). Set in PhotonSocket.Connect().</summary>
	public string ServerAddress { get; protected set; }

	public string ProxyServerAddress { get; protected set; }

	/// <summary>Contains the IP address of the previously resolved ServerAddress (or empty, if GetIpAddress wasn't used).</summary>
	public string ServerIpAddress { get; protected set; }

	/// <summary>Contains only the server's port address (as string).  Set in IphotonSocket.Connect().</summary>
	public int ServerPort { get; protected set; }

	/// <summary>Where available, this exposes if the server's address was resolved into an IPv6 address or not.</summary>
	public bool AddressResolvedAsIpv6 { get; protected internal set; }

	public string UrlProtocol { get; protected set; }

	public string UrlPath { get; protected set; }

	/// <summary>
	/// Provides the protocol string, of the current PhotonPeer.SerializationProtocolType to be used for WebSocket connections.
	/// </summary>
	/// <remarks>
	/// Any WebSocket wrapper could access this to get the desired binary protocol for the connection.
	/// Some WebSocket implementations use a static value of the same name and need to be updated.
	///
	/// The value is not cached and each call will create the needed string on the fly.
	/// </remarks>
	protected internal string SerializationProtocol
	{
		get
		{
			if (peerBase == null || peerBase.photonPeer == null)
			{
				return "GpBinaryV18";
			}
			return Enum.GetName(typeof(SerializationProtocol), peerBase.photonPeer.SerializationProtocolType);
		}
	}

	public PhotonSocket(PeerBase peerBase)
	{
		if (peerBase == null)
		{
			throw new Exception("Can't init without peer");
		}
		Protocol = peerBase.usedTransportProtocol;
		this.peerBase = peerBase;
		ConnectAddress = this.peerBase.ServerAddress;
	}

	public virtual bool Connect()
	{
		if (State != PhotonSocketState.Disconnected)
		{
			if ((int)peerBase.LogLevel >= 1)
			{
				peerBase.Listener.DebugReturn(LogLevel.Error, $"Connect() failed: connection in State: {State}");
			}
			return false;
		}
		if (peerBase == null || Protocol != peerBase.usedTransportProtocol)
		{
			return false;
		}
		if (!TryParseAddress(peerBase.ServerAddress, out var host, out var hostPort, out var urlProtocol, out var urlPath))
		{
			if ((int)peerBase.LogLevel >= 1)
			{
				peerBase.Listener.DebugReturn(LogLevel.Error, "Failed parsing address: " + peerBase.ServerAddress);
			}
			return false;
		}
		ServerIpAddress = string.Empty;
		ServerAddress = host;
		ServerPort = hostPort;
		UrlProtocol = urlProtocol;
		UrlPath = urlPath;
		if ((int)peerBase.LogLevel >= 4)
		{
			Listener.DebugReturn(LogLevel.Debug, $"PhotonSocket.Connect() {ServerAddress}:{ServerPort} this.Protocol: {Protocol}");
		}
		return true;
	}

	public abstract bool Disconnect();

	public abstract PhotonSocketError Send(byte[] data, int length);

	public abstract PhotonSocketError Receive(out byte[] data);

	public void HandleReceivedDatagram(byte[] inBuffer, int length, bool willBeReused)
	{
		peerBase.ReceiveIncomingCommands(inBuffer, length);
	}

	public bool ReportDebugOfLevel(LogLevel levelOfMessage)
	{
		return (int)peerBase.LogLevel >= (int)levelOfMessage;
	}

	public void EnqueueDebugReturn(LogLevel logLevel, string message)
	{
		peerBase.EnqueueDebugReturn(logLevel, message);
	}

	protected internal void HandleException(StatusCode statusCode)
	{
		State = PhotonSocketState.Disconnecting;
		peerBase.EnqueueStatusCallback(statusCode);
		peerBase.EnqueueActionForDispatch(delegate
		{
			peerBase.Disconnect();
		});
	}

	/// <summary>
	/// Separates the given address into host (name or IP), port, scheme and path. This is more about splitting the parts, than detecting invalid cases.
	/// </summary>
	/// <remarks>
	/// The out scheme may be empty for IP addresses (UDP and TCP). For these protocols, the url should include a port.
	/// IPv6 addresses <b>must use brackets</b> to separate address from port.
	///
	/// Examples:
	///     ns.exitgames.com:5058
	///     http://[2001:db8:1f70::999:de8:7648:6e8]:100/
	///     [2001:db8:1f70::999:de8:7648:6e8]:100
	/// See:
	///     http://serverfault.com/questions/205793/how-can-one-distinguish-the-host-and-the-port-in-an-ipv6-url
	/// </remarks>
	protected internal bool TryParseAddress(string url, out string host, out ushort port, out string scheme, out string absolutePath)
	{
		host = string.Empty;
		port = 0;
		scheme = string.Empty;
		absolutePath = string.Empty;
		if (string.IsNullOrEmpty(url))
		{
			return false;
		}
		bool containsScheme = url.Contains("://");
		Uri uri;
		bool num = Uri.TryCreate(containsScheme ? url : ("net.tcp://" + url), UriKind.Absolute, out uri);
		if (num)
		{
			host = uri.Host;
			port = (ushort)((containsScheme || url.Contains($":{uri.Port}")) ? ((ushort)uri.Port) : 0);
			scheme = (containsScheme ? uri.Scheme : string.Empty);
			absolutePath = ("/".Equals(uri.AbsolutePath) ? string.Empty : uri.AbsolutePath);
		}
		return num;
	}

	private bool IpAddressTryParse(string strIP, out IPAddress address)
	{
		address = null;
		if (string.IsNullOrEmpty(strIP))
		{
			return false;
		}
		string[] arrOctets = strIP.Split(new char[1] { '.' });
		if (arrOctets.Length != 4)
		{
			return false;
		}
		byte[] addressBytes = new byte[4];
		for (int i = 0; i < arrOctets.Length; i++)
		{
			string s = arrOctets[i];
			byte obyte = 0;
			if (!byte.TryParse(s, out obyte))
			{
				return false;
			}
			addressBytes[i] = obyte;
		}
		if (addressBytes[0] == 0)
		{
			return false;
		}
		address = new IPAddress(addressBytes);
		return true;
	}

	/// <summary>Wraps a DNS call to provide an array of addresses, sorted to have the IPv6 ones first.</summary>
	/// <remarks>
	/// This skips a DNS lookup, if the hostname is an IPv4 address. Then only this address is used as is.
	/// The DNS lookup may take a while, so it is recommended to do this in a thread. Also, it may fail entirely.
	/// </remarks>
	/// <returns>
	/// IPAddress array for hostname, sorted to put any IPv6 addresses first.<br />
	/// If the DNS lookup fails, HandleException(StatusCode.ExceptionOnConnect) gets called and null returned.
	/// Then the socket should not attempt to connect.
	/// </returns>
	protected internal IPAddress[] GetIpAddresses(string hostname)
	{
		IPAddress ipa = null;
		if (IPAddress.TryParse(hostname, out ipa))
		{
			if (ipa.AddressFamily == AddressFamily.InterNetworkV6 || IpAddressTryParse(hostname, out ipa))
			{
				return new IPAddress[1] { ipa };
			}
			HandleException(StatusCode.ServerAddressInvalid);
			return null;
		}
		IPAddress[] addresses;
		try
		{
			addresses = Dns.GetHostAddresses(ServerAddress);
		}
		catch (Exception arg)
		{
			try
			{
				addresses = Dns.GetHostByName(ServerAddress).AddressList;
			}
			catch (Exception arg2)
			{
				if (ReportDebugOfLevel(LogLevel.Warning))
				{
					EnqueueDebugReturn(LogLevel.Warning, $"GetHostAddresses and GetHostEntry() failed for: {ServerAddress}. Caught and handled exceptions:\n{arg}\n{arg2}");
				}
				HandleException(StatusCode.DnsExceptionOnConnect);
				return null;
			}
		}
		Array.Sort(addresses, AddressSortComparer);
		if (ReportDebugOfLevel(LogLevel.Info))
		{
			string[] ips = addresses.Select((IPAddress x) => $"{x} ({x.AddressFamily}({(int)x.AddressFamily}))").ToArray();
			string ipList = string.Join(", ", ips);
			if (ReportDebugOfLevel(LogLevel.Info))
			{
				EnqueueDebugReturn(LogLevel.Info, $"{ServerAddress} resolved to {ips.Length} address(es): {ipList}");
			}
		}
		return addresses;
	}

	private int AddressSortComparer(IPAddress x, IPAddress y)
	{
		if (x.AddressFamily == y.AddressFamily)
		{
			return 0;
		}
		if (x.AddressFamily != AddressFamily.InterNetworkV6)
		{
			return 1;
		}
		return -1;
	}
}
