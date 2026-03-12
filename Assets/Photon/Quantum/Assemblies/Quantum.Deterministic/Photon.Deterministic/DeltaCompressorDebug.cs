namespace Photon.Deterministic
{
	public class DeltaCompressorDebug : IDeltaCompressor
	{
		public void Pack(int[] current, int[] shared, int words, BitStream buffer)
		{
			for (int i = 0; i < words; i++)
			{
				buffer.WriteInt(i);
				buffer.WriteInt(current[i]);
			}
		}

		public void Unpack(int[] target, int words, BitStream buffer)
		{
			while (buffer.CanRead())
			{
				target[buffer.ReadInt()] = buffer.ReadInt();
			}
		}
	}
}

