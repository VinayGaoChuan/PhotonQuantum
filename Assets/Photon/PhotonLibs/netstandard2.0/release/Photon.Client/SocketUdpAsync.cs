using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Photon.Client;

/// <summary>Internal class to encapsulate the network i/o functionality for the realtime libary.</summary>
public class SocketUdpAsync : PhotonSocket, IDisposable
{
	private Socket sock;

	private readonly object syncer = new object();

	[Preserve]
	public SocketUdpAsync(PeerBase npeer)
		: base(npeer)
	{
		if (ReportDebugOfLevel(LogLevel.Info))
		{
			base.Listener.DebugReturn(LogLevel.Info, "SocketUdpAsync, .Net, Unity.");
		}
		PollReceive = false;
	}

	~SocketUdpAsync()
	{
		Dispose();
	}

	public void Dispose()
	{
		base.State = PhotonSocketState.Disconnecting;
		if (sock != null)
		{
			try
			{
				if (sock.Connected)
				{
					sock.Close();
				}
			}
			catch (Exception ex)
			{
				EnqueueDebugReturn(LogLevel.Info, "Exception in Dispose(): " + ex);
			}
		}
		sock = null;
		base.State = PhotonSocketState.Disconnected;
	}

	public override bool Connect()
	{
		lock (syncer)
		{
			if (!base.Connect())
			{
				return false;
			}
			base.State = PhotonSocketState.Connecting;
		}
		Thread thread = new Thread(DnsAndConnect);
		thread.IsBackground = true;
		thread.Start();
		return true;
	}

	public override bool Disconnect()
	{
		if (ReportDebugOfLevel(LogLevel.Info))
		{
			EnqueueDebugReturn(LogLevel.Info, "SocketUdpAsync.Disconnect()");
		}
		lock (syncer)
		{
			base.State = PhotonSocketState.Disconnecting;
			if (sock != null)
			{
				try
				{
					sock.Close();
				}
				catch (Exception ex)
				{
					if (ReportDebugOfLevel(LogLevel.Info))
					{
						EnqueueDebugReturn(LogLevel.Info, "Exception in Disconnect(): " + ex);
					}
				}
			}
			base.State = PhotonSocketState.Disconnected;
		}
		return true;
	}

	/// <summary>used by PhotonPeer*</summary>
	public override PhotonSocketError Send(byte[] data, int length)
	{
		try
		{
			if (sock == null || !sock.Connected)
			{
				return PhotonSocketError.Skipped;
			}
			sock.Send(data, 0, length, SocketFlags.None);
		}
		catch (Exception ex)
		{
			if (base.State != PhotonSocketState.Disconnecting && base.State != PhotonSocketState.Disconnected)
			{
				if (ReportDebugOfLevel(LogLevel.Info))
				{
					string socketInfo = "";
					if (sock != null)
					{
						socketInfo = string.Format(" Local: {0} Remote: {1} ({2}, {3})", sock.LocalEndPoint, sock.RemoteEndPoint, sock.Connected ? "connected" : "not connected", sock.IsBound ? "bound" : "not bound");
					}
					EnqueueDebugReturn(LogLevel.Info, string.Format("Cannot send to: {0}. Uptime: {1} ms. {2} {3}\n{4}", base.ServerAddress, peerBase.timeInt, base.AddressResolvedAsIpv6 ? " IPv6" : string.Empty, socketInfo, ex));
				}
				if (!sock.Connected)
				{
					EnqueueDebugReturn(LogLevel.Info, "Socket got closed by the local system. Disconnecting from within Send with StatusCode.Disconnect.");
					HandleException(StatusCode.SendError);
				}
			}
			return PhotonSocketError.Exception;
		}
		return PhotonSocketError.Success;
	}

	public override PhotonSocketError Receive(out byte[] data)
	{
		data = null;
		return PhotonSocketError.NoData;
	}

	internal void DnsAndConnect()
	{
		IPAddress[] addresses = GetIpAddresses(base.ServerAddress);
		if (addresses == null)
		{
			return;
		}
		string messages = string.Empty;
		IPAddress[] array = addresses;
		foreach (IPAddress ipA in array)
		{
			try
			{
				sock = new Socket(ipA.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
				sock.Connect(ipA, base.ServerPort);
				if (sock != null && sock.Connected)
				{
					break;
				}
			}
			catch (SocketException ex)
			{
				if (ReportDebugOfLevel(LogLevel.Warning))
				{
					messages = messages + ex?.ToString() + " " + ex.ErrorCode + "; ";
					EnqueueDebugReturn(LogLevel.Warning, "SocketException catched: " + ex?.ToString() + " ErrorCode: " + ex.ErrorCode);
				}
			}
			catch (Exception ex2)
			{
				if (ReportDebugOfLevel(LogLevel.Warning))
				{
					messages = messages + ex2?.ToString() + "; ";
					EnqueueDebugReturn(LogLevel.Warning, "Exception catched: " + ex2);
				}
			}
		}
		if (sock == null || !sock.Connected)
		{
			if (ReportDebugOfLevel(LogLevel.Error))
			{
				EnqueueDebugReturn(LogLevel.Error, "Failed to connect to server after testing each known IP. Error(s): " + messages);
			}
			HandleException(StatusCode.ExceptionOnConnect);
		}
		else
		{
			base.AddressResolvedAsIpv6 = sock.AddressFamily == AddressFamily.InterNetworkV6;
			base.ServerIpAddress = sock.RemoteEndPoint.ToString();
			base.State = PhotonSocketState.Connected;
			StartReceive();
		}
	}

	public void StartReceive()
	{
		byte[] inBuffer = new byte[base.MTU];
		try
		{
			sock.BeginReceive(inBuffer, 0, inBuffer.Length, SocketFlags.None, OnReceive, inBuffer);
		}
		catch (Exception ex)
		{
			if (base.State != PhotonSocketState.Disconnecting && base.State != PhotonSocketState.Disconnected)
			{
				if (ReportDebugOfLevel(LogLevel.Error))
				{
					EnqueueDebugReturn(LogLevel.Error, "Receive issue. State: " + base.State.ToString() + ". Server: '" + base.ServerAddress + "' Exception: " + ex);
				}
				HandleException(StatusCode.ExceptionOnReceive);
			}
		}
	}

	private void OnReceive(IAsyncResult ar)
	{
		if (base.State == PhotonSocketState.Disconnecting || base.State == PhotonSocketState.Disconnected)
		{
			return;
		}
		int readBytes = 0;
		try
		{
			readBytes = sock.EndReceive(ar);
		}
		catch (SocketException ex)
		{
			if (base.State != PhotonSocketState.Disconnecting && base.State != PhotonSocketState.Disconnected)
			{
				if (ReportDebugOfLevel(LogLevel.Error))
				{
					EnqueueDebugReturn(LogLevel.Error, "SocketException in EndReceive. State: " + base.State.ToString() + ". Server: '" + base.ServerAddress + "' ErrorCode: " + ex.ErrorCode + " SocketErrorCode: " + ex.SocketErrorCode.ToString() + " Message: " + ex.Message + " " + ex);
				}
				HandleException(StatusCode.ExceptionOnReceive);
			}
		}
		catch (Exception ex2)
		{
			if (base.State != PhotonSocketState.Disconnecting && base.State != PhotonSocketState.Disconnected)
			{
				if (ReportDebugOfLevel(LogLevel.Error))
				{
					EnqueueDebugReturn(LogLevel.Error, "Exception in EndReceive. State: " + base.State.ToString() + ". Server: '" + base.ServerAddress + "' Exception: " + ex2);
				}
				HandleException(StatusCode.ExceptionOnReceive);
			}
		}
		if (base.State == PhotonSocketState.Disconnecting || base.State == PhotonSocketState.Disconnected)
		{
			return;
		}
		byte[] inBuffer = (byte[])ar.AsyncState;
		HandleReceivedDatagram(inBuffer, readBytes, willBeReused: true);
		try
		{
			sock.BeginReceive(inBuffer, 0, inBuffer.Length, SocketFlags.None, OnReceive, inBuffer);
		}
		catch (SocketException ex3)
		{
			if (base.State != PhotonSocketState.Disconnecting && base.State != PhotonSocketState.Disconnected)
			{
				if (ReportDebugOfLevel(LogLevel.Error))
				{
					EnqueueDebugReturn(LogLevel.Error, "SocketException in BeginReceive. State: " + base.State.ToString() + ". Server: '" + base.ServerAddress + "' ErrorCode: " + ex3.ErrorCode + " SocketErrorCode: " + ex3.SocketErrorCode.ToString() + " Message: " + ex3.Message + " " + ex3);
				}
				HandleException(StatusCode.ExceptionOnReceive);
			}
		}
		catch (Exception ex4)
		{
			if (base.State != PhotonSocketState.Disconnecting && base.State != PhotonSocketState.Disconnected)
			{
				if (ReportDebugOfLevel(LogLevel.Error))
				{
					EnqueueDebugReturn(LogLevel.Error, "Exception in BeginReceive. State: " + base.State.ToString() + ". Server: '" + base.ServerAddress + "' Exception: " + ex4);
				}
				HandleException(StatusCode.ExceptionOnReceive);
			}
		}
	}
}
