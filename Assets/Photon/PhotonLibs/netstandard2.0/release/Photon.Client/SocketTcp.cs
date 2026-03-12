using System;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Threading;

namespace Photon.Client;

/// <summary>Encapsulates the network i/o functionality for the realtime library.</summary>
public class SocketTcp : PhotonSocket, IDisposable
{
	private Socket sock;

	private readonly object syncer = new object();

	[Preserve]
	public SocketTcp(PeerBase npeer)
		: base(npeer)
	{
		if (ReportDebugOfLevel(LogLevel.Info))
		{
			base.Listener.DebugReturn(LogLevel.Info, "SocketTcp, .Net, Unity.");
		}
		PollReceive = false;
	}

	~SocketTcp()
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
			catch (Exception arg)
			{
				if (ReportDebugOfLevel(LogLevel.Info))
				{
					EnqueueDebugReturn(LogLevel.Info, $"Exception caught in Dispose(): {arg}");
				}
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
			EnqueueDebugReturn(LogLevel.Info, "SocketTcp.Disconnect()");
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
				catch (Exception arg)
				{
					if (ReportDebugOfLevel(LogLevel.Info))
					{
						EnqueueDebugReturn(LogLevel.Info, $"Exception caught in Disconnect(): {arg}");
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
					SocketException se = ex as SocketException;
					string exceptionInfo = "";
					string socketInfo = "";
					exceptionInfo = ((se != null) ? $"ErrorCode {se.ErrorCode} Message {se.Message}" : ex.ToString());
					if (sock != null)
					{
						socketInfo = $"Local: {sock.LocalEndPoint} Remote: {sock.RemoteEndPoint}.";
					}
					EnqueueDebugReturn(LogLevel.Info, "Caught exception sending to: " + base.ServerAddress + ". " + socketInfo + " Exception: " + exceptionInfo);
				}
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
				sock = new Socket(ipA.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				sock.NoDelay = true;
				sock.ReceiveTimeout = peerBase.DisconnectTimeout;
				sock.SendTimeout = peerBase.DisconnectTimeout;
				sock.Connect(ipA, base.ServerPort);
				if (sock != null && sock.Connected)
				{
					break;
				}
			}
			catch (SecurityException ex)
			{
				if (ReportDebugOfLevel(LogLevel.Error))
				{
					messages = messages + ex?.ToString() + " ";
					EnqueueDebugReturn(LogLevel.Warning, $"SecurityException caught: {ex}");
				}
			}
			catch (SocketException ex2)
			{
				if (ReportDebugOfLevel(LogLevel.Warning))
				{
					messages = messages + ex2?.ToString() + " " + ex2.ErrorCode + "; ";
					EnqueueDebugReturn(LogLevel.Warning, $"SocketException caught: {ex2} ErrorCode: {ex2.ErrorCode}");
				}
			}
			catch (Exception ex3)
			{
				if (ReportDebugOfLevel(LogLevel.Warning))
				{
					messages = messages + ex3?.ToString() + "; ";
					EnqueueDebugReturn(LogLevel.Warning, $"Exception caught: {ex3}");
				}
			}
		}
		if (sock == null || !sock.Connected)
		{
			if (ReportDebugOfLevel(LogLevel.Error))
			{
				EnqueueDebugReturn(LogLevel.Error, $"Failed to connect to server {base.ServerAddress} after testing each known IP ({addresses.Length}). Error(s): {messages}");
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
		StreamBuffer opCollectionStream = new StreamBuffer(base.MTU);
		byte[] headerBuff = new byte[9];
		while (base.State == PhotonSocketState.Connected)
		{
			opCollectionStream.SetLength(0L);
			try
			{
				int bytesRead = 0;
				int bytesReadThisTime = 0;
				while (bytesRead < 9)
				{
					try
					{
						bytesReadThisTime = sock.Receive(headerBuff, bytesRead, 9 - bytesRead, SocketFlags.None);
					}
					catch (SocketException ex)
					{
						if (base.State != PhotonSocketState.Disconnecting && base.State > PhotonSocketState.Disconnected && ex.SocketErrorCode == SocketError.WouldBlock)
						{
							if (ReportDebugOfLevel(LogLevel.Error))
							{
								EnqueueDebugReturn(LogLevel.Error, "ReceiveLoop() got a WouldBlock exception. This is non-fatal. Going to continue.");
							}
							continue;
						}
						throw;
					}
					bytesRead += bytesReadThisTime;
					if (bytesReadThisTime == 0)
					{
						throw new SocketException(10054);
					}
				}
				if (headerBuff[0] == 240)
				{
					HandleReceivedDatagram(headerBuff, headerBuff.Length, willBeReused: true);
					continue;
				}
				int length = (headerBuff[1] << 24) | (headerBuff[2] << 16) | (headerBuff[3] << 8) | headerBuff[4];
				if (ReportDebugOfLevel(LogLevel.Debug))
				{
					EnqueueDebugReturn(LogLevel.Debug, $"TCP < {length}");
				}
				opCollectionStream.SetCapacityMinimum(length - 7);
				opCollectionStream.Write(headerBuff, 7, bytesRead - 7);
				bytesRead = 0;
				length -= 9;
				while (bytesRead < length)
				{
					try
					{
						bytesReadThisTime = sock.Receive(opCollectionStream.GetBuffer(), opCollectionStream.Position, length - bytesRead, SocketFlags.None);
					}
					catch (SocketException ex2)
					{
						if (base.State != PhotonSocketState.Disconnecting && base.State > PhotonSocketState.Disconnected && ex2.SocketErrorCode == SocketError.WouldBlock)
						{
							if (ReportDebugOfLevel(LogLevel.Error))
							{
								EnqueueDebugReturn(LogLevel.Error, "ReceiveLoop() got a WouldBlock exception. This is non-fatal. Going to continue.");
							}
							continue;
						}
						throw;
					}
					opCollectionStream.Position += bytesReadThisTime;
					bytesRead += bytesReadThisTime;
					if (bytesReadThisTime == 0)
					{
						throw new SocketException(10054);
					}
				}
				HandleReceivedDatagram(opCollectionStream.ToArray(), opCollectionStream.Length, willBeReused: false);
				if (ReportDebugOfLevel(LogLevel.Debug))
				{
					EnqueueDebugReturn(LogLevel.Debug, string.Format("TCP < {0}{1}", opCollectionStream.Length, (opCollectionStream.Length == length + 2) ? " OK" : " BAD"));
				}
			}
			catch (SocketException ex3)
			{
				if (base.State != PhotonSocketState.Disconnecting && base.State != PhotonSocketState.Disconnected)
				{
					if (ReportDebugOfLevel(LogLevel.Error))
					{
						EnqueueDebugReturn(LogLevel.Error, $"Receiving failed. SocketException: {ex3.SocketErrorCode}");
					}
					if (ex3.SocketErrorCode == SocketError.ConnectionReset || ex3.SocketErrorCode == SocketError.ConnectionAborted)
					{
						HandleException(StatusCode.DisconnectByServerTimeout);
					}
					else
					{
						HandleException(StatusCode.ExceptionOnReceive);
					}
				}
			}
			catch (Exception arg)
			{
				if (base.State != PhotonSocketState.Disconnecting && base.State != PhotonSocketState.Disconnected)
				{
					if (ReportDebugOfLevel(LogLevel.Error))
					{
						EnqueueDebugReturn(LogLevel.Error, $"Receive issue. State: {base.State}. Server: '{base.ServerAddress}' Exception: {arg}");
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
