using System;

namespace Photon.Deterministic
{
	/// <summary>
	/// The interface describes the runner of a Quantum session that is executed on the server.
	/// The implementation is provided by the game/Unity side and has full access to all custom game code and Quantum.Engine.dll.
	/// </summary>
	public interface IDeterministicSessionRunner
	{
		/// <summary>
		/// The event dispatcher that the simulation started with. The type is only known outside of Quantum.Deterministic.
		/// </summary>
		object EventDispatcher { get; }

		/// <summary>
		/// The callback dispatcher that the simulation started with. The type is only known outside of Quantum.Deterministic.
		/// </summary>
		object CallbackDispatcher { get; }

		/// <summary>
		/// The callback is called when the simulation emits the GameResult event.
		/// It is automatically picked up by the server and triggers a webhook.
		/// </summary>
		Action<byte[]> OnGameResult { get; set; }

		/// <summary>
		/// Returns the session.
		/// </summary>
		DeterministicSession Session { get; }

		/// <summary>
		/// Shutdown the simulation.
		/// </summary>
		void Shutdown();

		/// <summary>
		/// Start the simulation.
		/// </summary>
		/// <param name="args">Start arguments</param>
		void Start(DeterministicSessionRunnerStartArguments args);

		/// <summary>
		/// Update the simulation.
		/// </summary>
		/// <param name="gameTime">The current game time in seconds</param>
		void Service(double gameTime);

		/// <summary>
		/// The simulation can return a snapshot for late-joining clients.
		/// </summary>
		/// <param name="tick">The tick of the snapshot</param>
		/// <param name="data">The snapshot data</param>
		/// <returns><see langword="true" /> if a snapshot was successfully created</returns>
		bool TryCreateSnapshot(ref int tick, ref byte[] data);
	}
}

