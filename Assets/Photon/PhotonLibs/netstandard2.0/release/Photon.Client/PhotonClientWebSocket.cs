using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Photon.Client;

public class PhotonClientWebSocket : PhotonSocket
{
	private ClientWebSocket clientWebSocket;

	private Task sendTask;

	[Preserve]
	public PhotonClientWebSocket(PeerBase peerBase)
		: base(peerBase)
	{
		if (ReportDebugOfLevel(LogLevel.Info))
		{
			EnqueueDebugReturn(LogLevel.Info, "PhotonClientWebSocket");
		}
	}

	public override bool Connect()
	{
		if (!base.Connect())
		{
			return false;
		}
		base.State = PhotonSocketState.Connecting;
		Thread thread = new Thread(AsyncConnectAndReceive);
		thread.IsBackground = true;
		thread.Start();
		return true;
	}

	private void AsyncConnectAndReceive()
	{
		Uri uri = null;
		try
		{
			uri = new Uri(ConnectAddress);
		}
		catch (Exception arg)
		{
			if (ReportDebugOfLevel(LogLevel.Error))
			{
				base.Listener.DebugReturn(LogLevel.Error, $"Failed to create a URI from ConnectAddress ({ConnectAddress}). Exception: {arg}");
			}
		}
		if (uri != null && uri.HostNameType == UriHostNameType.Dns)
		{
			try
			{
				IPAddress[] hostAddresses = Dns.GetHostAddresses(uri.Host);
				for (int i = 0; i < hostAddresses.Length; i++)
				{
					if (hostAddresses[i].AddressFamily == AddressFamily.InterNetworkV6)
					{
						base.AddressResolvedAsIpv6 = true;
						ConnectAddress += "&IPv6";
						break;
					}
				}
			}
			catch (Exception arg2)
			{
				if (ReportDebugOfLevel(LogLevel.Error))
				{
					base.Listener.DebugReturn(LogLevel.Error, $"AsyncConnectAndReceive() failed. Dns.GetHostAddresses({uri.Host}) caught: {arg2}");
				}
			}
		}
		clientWebSocket = new ClientWebSocket();
		clientWebSocket.Options.AddSubProtocol(base.SerializationProtocol);
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(10000);
		Task tsk = clientWebSocket.ConnectAsync(new Uri(ConnectAddress), cancellationTokenSource.Token);
		try
		{
			tsk.Wait();
		}
		catch (Exception arg3)
		{
			if (ReportDebugOfLevel(LogLevel.Error))
			{
				EnqueueDebugReturn(LogLevel.Error, $"AsyncConnectAndReceive() caught exception on {ConnectAddress}: {arg3}");
			}
		}
		if (tsk.IsFaulted)
		{
			EnqueueDebugReturn(LogLevel.Error, "ClientWebSocket IsFaulted: " + tsk.Exception);
		}
		if (clientWebSocket.State != WebSocketState.Open)
		{
			base.SocketErrorCode = (int)(clientWebSocket.CloseStatus.HasValue ? clientWebSocket.CloseStatus.Value : ((WebSocketCloseStatus)0));
			if (ReportDebugOfLevel(LogLevel.Error))
			{
				EnqueueDebugReturn(LogLevel.Error, $"ClientWebSocket is not open. State: {clientWebSocket.State} CloseStatus: {clientWebSocket.CloseStatus} Description: {clientWebSocket.CloseStatusDescription}");
			}
			HandleException(StatusCode.ExceptionOnConnect);
			return;
		}
		base.State = PhotonSocketState.Connected;
		MemoryStream ms = new MemoryStream(base.MTU);
		bool useStream = false;
		ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[base.MTU]);
		while (clientWebSocket.State == WebSocketState.Open)
		{
			Task<WebSocketReceiveResult> readTask = null;
			try
			{
				readTask = clientWebSocket.ReceiveAsync(buffer, CancellationToken.None);
				while (!readTask.IsCompleted)
				{
					readTask.Wait(50);
				}
			}
			catch (Exception)
			{
			}
			if (!readTask.IsCompleted || clientWebSocket.State != WebSocketState.Open)
			{
				continue;
			}
			if (readTask.IsCanceled)
			{
				EnqueueDebugReturn(LogLevel.Error, $"PhotonClientWebSocket readTask.IsCanceled: {readTask.Status} {base.ServerAddress}:{base.ServerPort} {clientWebSocket.CloseStatusDescription}");
				continue;
			}
			if (readTask.Result.Count == 0)
			{
				if (ReportDebugOfLevel(LogLevel.Info))
				{
					EnqueueDebugReturn(LogLevel.Info, $"PhotonClientWebSocket received 0 bytes. this.State: {base.State} clientWebSocket.State: {clientWebSocket.State} readTask.Status: {readTask.Status}");
				}
				continue;
			}
			if (!readTask.Result.EndOfMessage)
			{
				useStream = true;
				ms.Write(buffer.Array, 0, readTask.Result.Count);
				continue;
			}
			int length;
			byte[] msgReceived;
			if (useStream)
			{
				ms.Write(buffer.Array, 0, readTask.Result.Count);
				length = (int)ms.Length;
				msgReceived = ms.GetBuffer();
				ms.SetLength(0L);
				ms.Position = 0L;
				useStream = false;
			}
			else
			{
				length = readTask.Result.Count;
				msgReceived = buffer.Array;
			}
			HandleReceivedDatagram(msgReceived, length, willBeReused: true);
		}
		if (base.State != PhotonSocketState.Disconnecting && base.State != PhotonSocketState.Disconnected)
		{
			if (ReportDebugOfLevel(LogLevel.Info))
			{
				EnqueueDebugReturn(LogLevel.Info, $"PhotonSocket.State is {base.State} but can't receive anymore. ClientWebSocket.State: {clientWebSocket.State}");
			}
			if (clientWebSocket.State == WebSocketState.CloseReceived)
			{
				HandleException(StatusCode.DisconnectByServerLogic);
			}
			if (clientWebSocket.State == WebSocketState.Aborted)
			{
				HandleException(StatusCode.DisconnectByServerReasonUnknown);
			}
		}
		Disconnect();
	}

	public override bool Disconnect()
	{
		if (clientWebSocket != null && clientWebSocket.State == WebSocketState.CloseReceived)
		{
			try
			{
				clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "CloseAsync due to state CloseReceived", CancellationToken.None);
			}
			catch (Exception arg)
			{
				if (ReportDebugOfLevel(LogLevel.Debug))
				{
					EnqueueDebugReturn(LogLevel.Debug, $"Caught exception in clientWebSocket.CloseAsync(): {arg}");
				}
			}
			base.State = PhotonSocketState.Disconnected;
			return true;
		}
		if (clientWebSocket != null && clientWebSocket.State != WebSocketState.Closed && clientWebSocket.State != WebSocketState.CloseSent)
		{
			base.State = PhotonSocketState.Disconnecting;
			try
			{
				clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "ws close", CancellationToken.None);
			}
			catch (Exception arg2)
			{
				if (ReportDebugOfLevel(LogLevel.Debug))
				{
					EnqueueDebugReturn(LogLevel.Debug, $"Caught exception in clientWebSocket.CloseOutputAsync(): {arg2}");
				}
			}
		}
		base.State = PhotonSocketState.Disconnected;
		return true;
	}

	public override PhotonSocketError Send(byte[] data, int length)
	{
		if (clientWebSocket != null && clientWebSocket.State != WebSocketState.Open && base.State != PhotonSocketState.Disconnecting && base.State != PhotonSocketState.Disconnected)
		{
			if (clientWebSocket.State == WebSocketState.CloseReceived)
			{
				HandleException(StatusCode.DisconnectByServerLogic);
				return PhotonSocketError.Exception;
			}
			if (clientWebSocket.State == WebSocketState.Aborted)
			{
				HandleException(StatusCode.DisconnectByServerReasonUnknown);
				return PhotonSocketError.Exception;
			}
		}
		if (clientWebSocket == null)
		{
			if (base.State == PhotonSocketState.Disconnecting || base.State == PhotonSocketState.Disconnected)
			{
				return PhotonSocketError.Skipped;
			}
			if (ReportDebugOfLevel(LogLevel.Error))
			{
				EnqueueDebugReturn(LogLevel.Error, "PhotonClientWebSocket.Send() failed, as this.clientWebSocket is null.");
			}
			return PhotonSocketError.Exception;
		}
		if (sendTask != null && !sendTask.IsCompleted && !sendTask.Wait(5))
		{
			return PhotonSocketError.Busy;
		}
		sendTask = clientWebSocket.SendAsync(new ArraySegment<byte>(data, 0, length), WebSocketMessageType.Binary, endOfMessage: true, CancellationToken.None);
		if (sendTask != null && !sendTask.IsCompleted && !sendTask.Wait(5))
		{
			return PhotonSocketError.PendingSend;
		}
		sendTask = null;
		return PhotonSocketError.Success;
	}

	public override PhotonSocketError Receive(out byte[] data)
	{
		throw new NotImplementedException();
	}
}
