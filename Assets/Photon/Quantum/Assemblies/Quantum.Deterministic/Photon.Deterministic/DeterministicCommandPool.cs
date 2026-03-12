using System;
using System.Collections.Generic;

namespace Photon.Deterministic
{
	/// <summary>
	/// Create a deterministic command pool for a specific command type.
	/// Make sure to reset all command data when reusing the objects.
	/// </summary>
	/// <typeparam name="T">Command type</typeparam>
	public sealed class DeterministicCommandPool<T> : IDeterministicCommandFactory, IDeterministicCommandPool where T : DeterministicCommand, new()
	{
		private Stack<T> _pool = new Stack<T>();

		/// <summary>
		/// The type of the command that the pool is managing.
		/// </summary>
		public Type CommandType => typeof(T);

		/// <summary>
		/// Creates a new instance of the command and calls <see cref="M:Photon.Deterministic.DeterministicCommandPool`1.Acquire" />.
		/// </summary>
		/// <returns></returns>
		public DeterministicCommand GetCommandInstance()
		{
			return Acquire();
		}

		/// <summary>
		/// Creates a new command or retrieves one from the pool.
		/// </summary>
		/// <returns>Command object</returns>
		public DeterministicCommand Acquire()
		{
			DeterministicCommand deterministicCommand = ((_pool.Count <= 0) ? new T() : _pool.Pop());
			deterministicCommand.Pool = this;
			deterministicCommand.Pooled = false;
			return deterministicCommand;
		}

		/// <summary>
		/// Release a command and return it to the pool.
		/// </summary>
		/// <param name="cmd">Command instance</param>
		/// <returns><see langword="true" /> if successfully returned to the pool</returns>
		public bool Release(DeterministicCommand cmd)
		{
			if (cmd.Pooled || !(cmd is T))
			{
				return false;
			}
			cmd.Pooled = true;
			_pool.Push((T)cmd);
			return true;
		}
	}
}

