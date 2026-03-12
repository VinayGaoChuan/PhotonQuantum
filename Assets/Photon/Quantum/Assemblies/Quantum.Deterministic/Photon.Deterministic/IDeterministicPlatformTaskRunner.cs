using System;

namespace Photon.Deterministic
{
	/// <summary>
	/// The interface for the task runner used in the Quantum task system.
	/// </summary>
	public interface IDeterministicPlatformTaskRunner : IDisposable
	{
		/// <summary>
		/// Schedules actions to be executed by the task runner.
		/// </summary>
		/// <param name="delegates">Array of actions</param>
		void Schedule(Action[] delegates);

		/// <summary>
		/// Wait for the task runner to complete all scheduled actions.
		/// </summary>
		void WaitForComplete();

		/// <summary>
		/// Poll the task runner for completion.
		/// </summary>
		/// <returns></returns>
		bool PollForComplete();
	}
}

