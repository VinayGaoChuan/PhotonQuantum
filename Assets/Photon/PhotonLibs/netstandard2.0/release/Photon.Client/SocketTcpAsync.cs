using System;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Threading;

namespace Photon.Client;

/// <summary>Internal class to encapsulate the network i/o functionality for the realtime libary.</summary>
public class SocketTcpAsync : PhotonSocket, IDisposable
{
	private class ReceiveContext
	{
		public Socket workSocket;

		public int ReceivedHeaderBytes;

		public byte[] HeaderBuffer;

		public int ExpectedMessageBytes;

		public int ReceivedMessageBytes;

		public byte[] MessageBuffer;

		public bool ReadingHeader => ExpectedMessageBytes == 0;

		public bool ReadingMessage => ExpectedMessageBytes != 0;

		public byte[] CurrentBuffer
		{
			get
			{
				if (!ReadingHeader)
				{
					return MessageBuffer;
				}
				return HeaderBuffer;
			}
		}

		public int CurrentOffset
		{
			get
			{
				if (!ReadingHeader)
				{
					return ReceivedMessageBytes;
				}
				return ReceivedHeaderBytes;
			}
		}

		public int CurrentExpected
		{
			get
			{
				if (!ReadingHeader)
				{
					return ExpectedMessageBytes;
				}
				return 9;
			}
		}

		public ReceiveContext(Socket socket, byte[] headerBuffer, byte[] messageBuffer)
		{
			HeaderBuffer = headerBuffer;
			MessageBuffer = messageBuffer;
			workSocket = socket;
		}

		public void Reset()
		{
			ReceivedHeaderBytes = 0;
			ExpectedMessageBytes = 0;
			ReceivedMessageBytes = 0;
		}
	}

	private Socket sock;

	private readonly object syncer = new object();

	[Preserve]
	public SocketTcpAsync(PeerBase npeer)
		: base(npeer)
	{
		if (ReportDebugOfLevel(LogLevel.Info))
		{
			base.Listener.DebugReturn(LogLevel.Info, "SocketTcpAsync, .Net, Unity.");
		}
		PollReceive = false;
	}

	~SocketTcpAsync()
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
			EnqueueDebugReturn(LogLevel.Info, "SocketTcpAsync.Disconnect()");
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
					EnqueueDebugReturn(LogLevel.Info, string.Format("Cannot send to: {0} ({4}). Uptime: {1} ms. {2} {3}", base.ServerAddress, peerBase.timeInt, base.AddressResolvedAsIpv6 ? " IPv6" : string.Empty, socketInfo, ex));
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
					EnqueueDebugReturn(LogLevel.Warning, "SecurityException catched: " + ex);
				}
			}
			catch (SocketException ex2)
			{
				if (ReportDebugOfLevel(LogLevel.Warning))
				{
					messages = messages + ex2?.ToString() + " " + ex2.ErrorCode + "; ";
					EnqueueDebugReturn(LogLevel.Warning, "SocketException catched: " + ex2?.ToString() + " ErrorCode: " + ex2.ErrorCode);
				}
			}
			catch (Exception ex3)
			{
				if (ReportDebugOfLevel(LogLevel.Warning))
				{
					messages = messages + ex3?.ToString() + "; ";
					EnqueueDebugReturn(LogLevel.Warning, "Exception catched: " + ex3);
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
			ReceiveAsync();
		}
	}

	private void ReceiveAsync(ReceiveContext context = null)
	{
		if (context == null)
		{
			context = new ReceiveContext(sock, new byte[9], new byte[base.MTU]);
		}
		try
		{
			sock.BeginReceive(context.CurrentBuffer, context.CurrentOffset, context.CurrentExpected - context.CurrentOffset, SocketFlags.None, ReceiveAsync, context);
		}
		catch (Exception ex)
		{
			if (base.State != PhotonSocketState.Disconnecting && base.State != PhotonSocketState.Disconnected)
			{
				if (ReportDebugOfLevel(LogLevel.Error))
				{
					EnqueueDebugReturn(LogLevel.Error, "SocketTcpAsync.ReceiveAsync Exception. State: " + base.State.ToString() + ". Server: '" + base.ServerAddress + "' Exception: " + ex);
				}
				HandleException(StatusCode.ExceptionOnReceive);
			}
		}
	}

	private void ReceiveAsync(IAsyncResult ar)
	{
		if (base.State == PhotonSocketState.Disconnecting || base.State == PhotonSocketState.Disconnected)
		{
			return;
		}
		int readBytes = 0;
		try
		{
			readBytes = sock.EndReceive(ar);
			if (readBytes == 0)
			{
				throw new SocketException(10054);
			}
		}
		catch (SocketException ex)
		{
			if (base.State != PhotonSocketState.Disconnecting && base.State != PhotonSocketState.Disconnected)
			{
				if (ReportDebugOfLevel(LogLevel.Error))
				{
					EnqueueDebugReturn(LogLevel.Error, "SocketTcpAsync.EndReceive SocketException. State: " + base.State.ToString() + ". Server: '" + base.ServerAddress + "' ErrorCode: " + ex.ErrorCode + " SocketErrorCode: " + ex.SocketErrorCode.ToString() + " Message: " + ex.Message + " " + ex);
				}
				HandleException(StatusCode.ExceptionOnReceive);
				return;
			}
		}
		catch (Exception ex2)
		{
			if (base.State != PhotonSocketState.Disconnecting && base.State != PhotonSocketState.Disconnected)
			{
				if (ReportDebugOfLevel(LogLevel.Error))
				{
					EnqueueDebugReturn(LogLevel.Error, "SocketTcpAsync.EndReceive Exception. State: " + base.State.ToString() + ". Server: '" + base.ServerAddress + "' Exception: " + ex2);
				}
				HandleException(StatusCode.ExceptionOnReceive);
				return;
			}
		}
		ReceiveContext context = (ReceiveContext)ar.AsyncState;
		if (readBytes + context.CurrentOffset != context.CurrentExpected)
		{
			if (context.ReadingHeader)
			{
				context.ReceivedHeaderBytes += readBytes;
			}
			else
			{
				context.ReceivedMessageBytes += readBytes;
			}
			ReceiveAsync(context);
		}
		else if (context.ReadingHeader)
		{
			byte[] headerBuff = context.HeaderBuffer;
			if (headerBuff[0] == 240)
			{
				HandleReceivedDatagram(headerBuff, headerBuff.Length, willBeReused: true);
				context.Reset();
				ReceiveAsync(context);
				return;
			}
			int length = (headerBuff[1] << 24) | (headerBuff[2] << 16) | (headerBuff[3] << 8) | headerBuff[4];
			context.ExpectedMessageBytes = length - 7;
			if (context.ExpectedMessageBytes > context.MessageBuffer.Length)
			{
				context.MessageBuffer = new byte[context.ExpectedMessageBytes];
			}
			context.MessageBuffer[0] = headerBuff[7];
			context.MessageBuffer[1] = headerBuff[8];
			context.ReceivedMessageBytes = 2;
			ReceiveAsync(context);
		}
		else
		{
			HandleReceivedDatagram(context.MessageBuffer, context.ExpectedMessageBytes, willBeReused: true);
			context.Reset();
			ReceiveAsync(context);
		}
	}
}
