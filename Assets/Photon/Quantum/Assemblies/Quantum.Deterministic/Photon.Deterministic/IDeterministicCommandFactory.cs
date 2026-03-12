using System;

namespace Photon.Deterministic
{
	/// <summary>
	/// The command factor interface.
	/// </summary>
	public interface IDeterministicCommandFactory
	{
		/// <summary>
		/// The type of commands to create.
		/// </summary>
		Type CommandType { get; }

		/// <summary>
		/// Returns a new instance of the command, may be pooled.
		/// </summary>
		/// <returns>Deterministic command of type <see cref="P:Photon.Deterministic.IDeterministicCommandFactory.CommandType" /></returns>
		DeterministicCommand GetCommandInstance();
	}
}

