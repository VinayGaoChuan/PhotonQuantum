using System;
using Quantum;

namespace Photon.Deterministic
{
	internal static class DeterministicSimulatorUtils
	{
		public static void CallChecksumError(IDeterministicGame game, DeterministicTickChecksumError error, DeterministicFrame[] frames)
		{
			try
			{
				LogStream logError = InternalLogStreams.LogError;
				if (logError != null)
				{
					logError.Log("Checksum Error Detected By Server, Simulation Halting.");
				}
				game.OnChecksumError(error, frames);
			}
			catch (Exception ex)
			{
				LogStream logException = InternalLogStreams.LogException;
				if (logException != null)
				{
					logException.Log("## Checksum Error Callback Threw Exception ##", ex);
				}
			}
		}

		public static int CalculateFramesThatWillBeSimulated(double acc, double delta)
		{
			int num = 0;
			while (acc >= delta)
			{
				acc -= delta;
				num++;
			}
			return num;
		}

		public static ulong CallChecksum(DeterministicFrame f)
		{
			try
			{
				return f.CalculateChecksum();
			}
			catch (Exception ex)
			{
				LogStream logException = InternalLogStreams.LogException;
				if (logException != null)
				{
					logException.Log("### CalculateChecksum Threw Exception ###", ex);
				}
			}
			return 0uL;
		}

		public static void CallSimulate(DeterministicFrame f, IDeterministicGame game)
		{
			try
			{
				game.OnSimulate(f);
			}
			catch (Exception ex)
			{
				LogStream logException = InternalLogStreams.LogException;
				if (logException != null)
				{
					logException.Log("## Game Threw Exception ##", ex);
				}
			}
		}

		public static void CallSimulateFinished(DeterministicFrame f, IDeterministicGame game)
		{
			try
			{
				game.OnSimulateFinished(f);
			}
			catch (Exception ex)
			{
				LogStream logException = InternalLogStreams.LogException;
				if (logException != null)
				{
					logException.Log("## Game Threw Exception ##", ex);
				}
			}
		}

		public static void CallChecksumComputed(IDeterministicGame game, int frame, ulong checksum)
		{
			try
			{
				game.OnChecksumComputed(frame, checksum);
			}
			catch (Exception ex)
			{
				LogStream logException = InternalLogStreams.LogException;
				if (logException != null)
				{
					logException.Log("## Game Threw Exception ##", ex);
				}
			}
		}

		public static void CalculateAndProcessChecksumIfRequired(DeterministicFrame f, int checksumInterval, IDeterministicNetwork network, Action<int, ulong> callback)
		{
			if (f.IsVerified && checksumInterval > 0 && f.Number % checksumInterval == 0)
			{
				ulong num = CallChecksum(f);
				network.SendLocalChecksum(f.Number, num);
				callback?.Invoke(f.Number, num);
			}
		}

		private static bool IsAllZero(byte[] array)
		{
			if (array == null)
			{
				return true;
			}
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != 0)
				{
					return false;
				}
			}
			return true;
		}

		public static bool InputIsIdentical(byte[] a, byte[] b)
		{
			if (a == null && b == null)
			{
				return true;
			}
			if (a == null)
			{
				return IsAllZero(b);
			}
			if (b == null)
			{
				return IsAllZero(a);
			}
			if (a.Length != b.Length)
			{
				return false;
			}
			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] != b[i])
				{
					return false;
				}
			}
			return true;
		}
	}
}

