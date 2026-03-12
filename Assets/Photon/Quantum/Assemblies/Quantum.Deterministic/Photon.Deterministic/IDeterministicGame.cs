using System;
using Quantum;

namespace Photon.Deterministic
{
	/// <summary>
	/// The deterministic game interface is the main hub that holds information about the simulation on the Quantum.Deterministic level.
	/// </summary>
	public interface IDeterministicGame
	{
		/// <summary>
		/// Get the deterministic session.
		/// </summary>
		DeterministicSession Session { get; }

		/// <summary>
		/// Returns the serialized input size.
		/// </summary>
		/// <returns>Serialized input size</returns>
		int GetInputSerializedFixedSize();

		/// <summary>
		/// Return the in memory input size.
		/// </summary>
		/// <returns>Input object size</returns>
		int GetInputInMemorySize();

		/// <summary>
		/// Asks the game to de-serialize input into the buffers, because it's game specific it cannot be done from here.
		/// </summary>
		/// <param name="player">The player the input is for</param>
		/// <param name="data">The input data</param>
		/// <param name="buffer">The destination buffer</param>
		/// <param name="verified">Is the input verified</param>
		unsafe void DeserializeInputInto(int player, byte[] data, byte* buffer, bool verified);

		/// <summary>
		/// Polls the game for local input.
		/// </summary>
		/// <param name="frame">The frame that the input is for</param>
		/// <param name="playerSlot">The player slot that is polling</param>
		/// <returns>The polled input</returns>
		DeterministicFrameInputTemp OnLocalInput(int frame, int playerSlot);

		/// <summary>
		/// Asks the game to serialize the input, because it's game specific it cannot be done from here.
		/// </summary>
		/// <param name="encoded"></param>
		/// <param name="dst"></param>
		unsafe void OnSerializedInput(byte* encoded, Array dst);

		/// <summary>
		/// Creates a frame context in the beginning of the simulation.
		/// </summary>
		/// <returns>Frame context.</returns>
		IDisposable CreateFrameContext();

		/// <summary>
		/// Creates a new frame object using the context.
		/// </summary>
		/// <param name="context">Frame context</param>
		/// <returns>Frame object</returns>
		DeterministicFrame CreateFrame(IDisposable context);

		/// <summary>
		/// Creates a new frame object using the context and external frame data.
		/// </summary>
		/// <param name="context">Frame context</param>
		/// <param name="data"></param>
		/// <returns></returns>
		DeterministicFrame CreateFrame(IDisposable context, byte[] data);

		/// <summary>
		/// Try to get the verified frame for a given tick from the snapshot buffer.
		/// </summary>
		/// <param name="tick">Requested tick</param>
		/// <returns>The frame object or <see langword="null" /></returns>
		DeterministicFrame GetVerifiedFrame(int tick);

		/// <summary>
		/// Creates information send to the server when detecting a checksum error.
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <returns>Serialized frame dump context</returns>
		byte[] GetExtraErrorFrameDumpData(DeterministicFrame frame);

		/// <summary>
		/// The callback is called when the simulation is destroyed.
		/// </summary>
		void OnDestroy();

		/// <summary>
		/// The <see cref="T:Photon.Deterministic.DeterministicSession" /> creates this reference during its initialization.
		/// </summary>
		/// <param name="session">Deterministic session that this game uses</param>
		void AssignSession(DeterministicSession session);

		/// <summary>
		/// The callback is called when the actual simulation starts after the online protocol start sequence was successful.
		/// </summary>
		/// <param name="state">Intitial deterministic frame</param>
		void OnGameStart(DeterministicFrame state);

		/// <summary>
		/// The callback is called when the game is starting from a snapshot after the snapshot has been received.
		/// </summary>
		void OnGameResync();

		/// <summary>
		/// Not implemented.
		/// </summary>
		void OnGameEnded();

		/// <summary>
		/// The callback is called when any simulation step was executed.
		/// </summary>
		/// <param name="state">Frame that was simulated</param>
		void OnSimulate(DeterministicFrame state);

		/// <summary>
		/// The callback is called after any simulation step was executed and after the <see cref="M:Photon.Deterministic.IDeterministicGame.OnSimulate(Photon.Deterministic.DeterministicFrame)" /> callback.
		/// </summary>
		/// <param name="state">Frame that was simulated</param>
		void OnSimulateFinished(DeterministicFrame state);

		/// <summary>
		/// The callback is called when when the session completed its <see cref="M:Photon.Deterministic.DeterministicSession.Update(System.Nullable{System.Double})" /> loop.
		/// </summary>
		void OnUpdateDone();

		/// <summary>
		/// The callback is called when a checksum error was detected.
		/// </summary>
		/// <param name="error">Checksum error information</param>
		/// <param name="frames">Contains the verified frame that was failed to validate</param>
		void OnChecksumError(DeterministicTickChecksumError error, DeterministicFrame[] frames);

		/// <summary>
		/// The callback is called when the clients receives a frame dump of another client from the server.
		/// </summary>
		/// <param name="actorId">The Photon actor id that the dump belongs to</param>
		/// <param name="frameNumber">The frame number of the dump</param>
		/// <param name="sessionConfig">The session config</param>
		/// <param name="runtimeConfig">The runtime config</param>
		/// <param name="frameData">The frame data</param>
		/// <param name="extraData">Extra dump meta information</param>
		void OnChecksumErrorFrameDump(int actorId, int frameNumber, DeterministicSessionConfig sessionConfig, byte[] runtimeConfig, byte[] frameData, byte[] extraData);

		/// <summary>
		/// The callback is called when an input object was confirmed by the server.
		/// </summary>
		/// <param name="input">Input object</param>
		void OnInputConfirmed(DeterministicFrameInputTemp input);

		/// <summary>
		/// The callback is called when an input set (all clients) was confirmed by the server.
		/// </summary>
		/// <param name="tick">Tick</param>
		/// <param name="length">Length of input object array</param>
		/// <param name="data">Input objects</param>
		void OnInputSetConfirmed(int tick, int length, byte[] data);

		/// <summary>
		/// The callback is called when the local checksum was computed.
		/// </summary>
		/// <param name="frame">The frame the checksum belongs to</param>
		/// <param name="checksum">The checksum that will be send to the server</param>
		void OnChecksumComputed(int frame, ulong checksum);

		/// <summary>
		/// The callback is called before the session computes multiple simulation steps (frames).
		/// </summary>
		void OnSimulationBegin();

		/// <summary>
		/// The callback is called when multiple simulation steps (frames) were executed.
		/// </summary>
		void OnSimulationEnd();

		/// <summary>
		/// The callback is called when the server plugin disconnected the client.
		/// </summary>
		/// <param name="reason">Debug string</param>
		void OnPluginDisconnect(string reason);

		/// <summary>
		/// The callback is called when the server confirmed the addition of a (local) player.
		/// </summary>
		/// <param name="frame">The frame the player has been added</param>
		/// <param name="playerSlot">The player slot that was used to assign the player</param>
		/// <param name="player">The player</param>
		void OnLocalPlayerAddConfirmed(DeterministicFrame frame, int playerSlot, PlayerRef player);

		/// <summary>
		/// The callback is called when the server confirmed the removal of a (local) player.
		/// </summary>
		/// <param name="frame">The frame when the request was confirmed</param>
		/// <param name="playerSlot">The player slot of the removed player</param>
		/// <param name="player">The player that was removed</param>
		void OnLocalPlayerRemoveConfirmed(DeterministicFrame frame, int playerSlot, PlayerRef player);

		/// <summary>
		/// The callback is called when the server failed to process the add player request.
		/// </summary>
		/// <param name="playerSlot">The player slot that was requested</param>
		/// <param name="message">Debug message</param>
		void OnLocalPlayerAddFailed(int playerSlot, string message);

		/// <summary>
		/// The callback is called when the server failed to process the remove player request.
		/// </summary>
		/// <param name="playerSlot">The player slot that was tried to remove</param>
		/// <param name="message">Debug message</param>
		void OnLocalPlayerRemoveFailed(int playerSlot, string message);
	}
}

