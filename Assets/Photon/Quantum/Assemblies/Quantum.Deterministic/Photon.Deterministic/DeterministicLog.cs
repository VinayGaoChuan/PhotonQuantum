using System;

namespace Photon.Deterministic
{
	[Obsolete("Use Quantum.Log")]
	public static class DeterministicLog
	{
		public static void InitForConsole()
		{
		}

		public static void Init(Action<string> info, Action<string> warn, Action<string> error, Action<Exception> exn)
		{
		}

		public static void Reset()
		{
		}
	}
}

