using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Photon.Client;

/// <summary>Encapsulates the network i/o functionality for the realtime library.</summary>
public class SocketUdp : PhotonSocket, IDisposable
{
	private Socket sock;

	private readonly object syncer = new object();

	[Preserve]
	public SocketUdp(PeerBase npeer)
		: base(npeer)
	{
		if (ReportDebugOfLevel(LogLevel.Info))
		{
			base.Listener.DebugReturn(LogLevel.Info, "SocketUdp, .Net, Unity.");
		}
		PollReceive = false;
	}

	~SocketUdp()
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
					sock.Close(1);
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
			EnqueueDebugReturn(LogLevel.Info, "SocketUdp.Disconnect()");
		}
		lock (syncer)
		{
			base.State = PhotonSocketState.Disconnecting;
			if (sock != null)
			{
				try
				{
					sock.Close(1);
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
		catch (SocketException ex)
		{
			if (ex.SocketErrorCode == SocketError.WouldBlock)
			{
				return PhotonSocketError.Busy;
			}
			if (base.State == PhotonSocketState.Disconnecting || base.State == PhotonSocketState.Disconnected)
			{
				return PhotonSocketError.Exception;
			}
			base.SocketErrorCode = (int)ex.SocketErrorCode;
			if (ReportDebugOfLevel(LogLevel.Info))
			{
				string socketInfo = "";
				if (sock != null)
				{
					socketInfo = string.Format(" Local: {0} Remote: {1} ({2}, {3})", sock.LocalEndPoint, sock.RemoteEndPoint, sock.Connected ? "connected" : "not connected", sock.IsBound ? "bound" : "not bound");
				}
				EnqueueDebugReturn(LogLevel.Info, string.Format("Cannot send to: {0}. Uptime: {1} ms. {2} {3}\n{4}", base.ServerAddress, peerBase.timeInt, base.AddressResolvedAsIpv6 ? " IPv6" : string.Empty, socketInfo, ex));
			}
			HandleException(StatusCode.SendError);
			return PhotonSocketError.Exception;
		}
		catch (Exception ex2)
		{
			if (base.State == PhotonSocketState.Disconnecting || base.State == PhotonSocketState.Disconnected)
			{
				return PhotonSocketError.Exception;
			}
			if (ReportDebugOfLevel(LogLevel.Info))
			{
				string socketInfo2 = "";
				if (sock != null)
				{
					socketInfo2 = string.Format(" Local: {0} Remote: {1} ({2}, {3})", sock.LocalEndPoint, sock.RemoteEndPoint, sock.Connected ? "connected" : "not connected", sock.IsBound ? "bound" : "not bound");
				}
				EnqueueDebugReturn(LogLevel.Info, string.Format("Cannot send to: {0}. Uptime: {1} ms. {2} {3}\n{4}", base.ServerAddress, peerBase.timeInt, base.AddressResolvedAsIpv6 ? " IPv6" : string.Empty, socketInfo2, ex2));
			}
			if (!sock.Connected)
			{
				EnqueueDebugReturn(LogLevel.Info, "Caught Exception in Send(). Ending connection with StatusCode.SendError.");
				HandleException(StatusCode.SendError);
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
				sock.Blocking = false;
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
					EnqueueDebugReturn(LogLevel.Warning, "SocketException caught: " + ex?.ToString() + " ErrorCode: " + ex.ErrorCode);
				}
			}
			catch (Exception ex2)
			{
				if (ReportDebugOfLevel(LogLevel.Warning))
				{
					messages = messages + ex2?.ToString() + "; ";
					EnqueueDebugReturn(LogLevel.Warning, "Exception caught: " + ex2);
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
			Thread thread = new Thread(ReceiveLoop);
			thread.IsBackground = true;
			thread.Start();
		}
	}

	/// <summary>Endless loop, run in Receive Thread.</summary>
	public void ReceiveLoop()
	{
		byte[] inBuffer = new byte[base.MTU];
		while (base.State == PhotonSocketState.Connected)
		{
			try
			{
				if (sock.Poll(5000, SelectMode.SelectRead))
				{
					int read = sock.Receive(inBuffer);
					HandleReceivedDatagram(inBuffer, read, willBeReused: true);
				}
			}
			catch (SocketException ex)
			{
				if (base.State != PhotonSocketState.Disconnecting && base.State != PhotonSocketState.Disconnected)
				{
					if (ReportDebugOfLevel(LogLevel.Error))
					{
						EnqueueDebugReturn(LogLevel.Error, "Receive issue. State: " + base.State.ToString() + ". Server: '" + base.ServerAddress + "' ErrorCode: " + ex.ErrorCode + " SocketErrorCode: " + ex.SocketErrorCode.ToString() + " Message: " + ex.Message + " " + ex);
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
						EnqueueDebugReturn(LogLevel.Error, "Receive issue. State: " + base.State.ToString() + ". Server: '" + base.ServerAddress + "' Message: " + ex2.Message + " Exception: " + ex2);
					}
					HandleException(StatusCode.ExceptionOnReceive);
				}
			}
		}
		lock (syncer)
		{
			if (base.State != PhotonSocketState.Disconnecting && base.State != PhotonSocketState.Disconnected)
			{
				Disconnect();
			}
		}
	}
}
