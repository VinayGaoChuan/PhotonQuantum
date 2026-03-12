namespace Photon.Deterministic
{
	internal static class DeterministicSessionConfigUtils
	{
		public static bool VerifyFixedSize(DeterministicSessionConfig config, byte[] data, int dataLength)
		{
			if (data != null && data.Length != 0)
			{
				return dataLength == config.InputFixedSize;
			}
			return true;
		}
	}
}

