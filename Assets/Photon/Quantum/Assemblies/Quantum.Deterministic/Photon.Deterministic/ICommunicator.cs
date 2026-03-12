using System.Threading.Tasks;

namespace Photon.Deterministic
{
	/// <summary>
	/// This interface bridges the Quantum networking layer with the Photon Realtime network libraries.
	/// </summary>
	public interface ICommunicator
	{
		/// <summary>
		/// Returns the round trip time in milliseconds.
		/// </summary>
		int RoundTripTime { get; }

		/// <summary>
		/// The Photon Actor number that was assigned to the client.
		/// </summary>
		int ActorNumber { get; }

		/// <summary>
		/// A simple indication if the client is connected to the server.
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// Service updates the network layer and has to be called frequently to process incoming and outgoing messages.
		/// </summary>
		void Service();

		/// <summary>
		/// Is called when the Quantum shuts down and the communicator should clean up.
		/// </summary>
		void OnDestroy();

		/// <summary>
		/// Is called the Quantum shuts down asynchronously and the communicator should clean up.
		/// </summary>
		/// <returns></returns>
		Task OnDestroyAsync();

		/// <summary>
		/// Recycles the event object.
		/// </summary>
		/// <param name="obj">dataContainer of <see cref="T:Photon.Deterministic.OnEventReceived" /></param>
		void DisposeEventObject(object obj);

		/// <summary>
		/// Sends a Quantum message to the server.
		/// </summary>
		/// <param name="eventCode">Photon event code</param>
		/// <param name="message">The message as byte array</param>
		/// <param name="messageLength">The message length</param>
		/// <param name="reliable"><see langword="true" /> if the message should be send reliable</param>
		/// <param name="toPlayers">The recipients of the message, usually [0] for the server.</param>
		void RaiseEvent(byte eventCode, byte[] message, int messageLength, bool reliable, int[] toPlayers);

		/// <summary>
		/// Quantum network layers require this event listener to be added to receive messages.
		/// </summary>
		/// <param name="onEventReceived">The incoming message callback to be called</param>
		void AddEventListener(OnEventReceived onEventReceived);
	}
}

