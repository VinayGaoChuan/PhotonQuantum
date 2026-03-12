using System;

namespace Photon.Deterministic
{
	/// <summary>
	/// Command pool interface.
	/// </summary>
	public interface IDeterministicCommandPool
	{
		/// <summary>
		/// The type of commands pooled.
		/// </summary>
		Type CommandType { get; }

		/// <summary>
		/// Get from pool or create a new instance of the <see cref="P:Photon.Deterministic.IDeterministicCommandPool.CommandType" />.
		/// </summary>
		/// <returns></returns>
		DeterministicCommand Acquire();

		/// <summary>
		/// Return a command to the pool.
		/// </summary>
		/// <param name="cmd">Command to return to the pool.</param>
		/// <returns><see langword="true" /> if command has been returned to the pool</returns>
		bool Release(DeterministicCommand cmd);
	}
}

