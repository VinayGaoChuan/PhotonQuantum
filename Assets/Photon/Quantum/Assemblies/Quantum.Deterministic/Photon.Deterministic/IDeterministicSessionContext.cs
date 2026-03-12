namespace Photon.Deterministic
{
	/// <summary>
	/// The interface describes the context of a Quantum session that is running on the server.
	/// The implementation is provided by the game/Unity side and has full access to all custom game code and Quantum.Engine.dll.
	/// </summary>
	public interface IDeterministicSessionContext
	{
		/// <summary>
		/// Returns the command serializer created when the simulation started.
		/// </summary>
		DeterministicCommandSerializer CommandSerializer { get; }

		/// <summary>
		/// Return the resource manager as an object. Cast to ResourceManagerStatic.
		/// </summary>
		object ResourceManager { get; }

		/// <summary>
		/// Init the simulation.
		/// </summary>
		/// <param name="args">Initialization arguments</param>
		void Init(DeterministicSessionContextInitArguments args);

		/// <summary>
		/// Start the simulation.
		/// </summary>
		/// <param name="args">Start arguments</param>
		void Start(DeterministicSessionContextStartArguments args);

		/// <summary>
		/// Shutdown the context.
		/// </summary>
		void Shutdown();
	}
}

