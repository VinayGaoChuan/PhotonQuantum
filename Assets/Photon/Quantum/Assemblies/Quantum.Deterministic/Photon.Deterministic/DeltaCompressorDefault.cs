namespace Photon.Deterministic
{
	public class DeltaCompressorDefault : IDeltaCompressor
	{
		public const int DEFAULT_COMPRESSOR_OFFSET_BLOCK_SIZE = 3;

		public const int DEFAULT_COMPRESSOR_VALUE_BLOCK_SIZE = 6;

		public void Pack(int[] current, int[] shared, int words, BitStream buffer)
		{
			int num = 0;
			for (int i = 0; i < words; i++)
			{
				if (shared[i] != current[i])
				{
					long i2 = (long)current[i] - (long)shared[i];
					int value = i - num;
					buffer.WriteInt32VarLength(value, 3);
					buffer.WriteInt64VarLength(Maths.ZigZagEncode(i2), 6);
					num = i;
				}
			}
		}

		public void Unpack(int[] target, int words, BitStream buffer)
		{
			int num = 0;
			while (buffer.CanRead() && num < words)
			{
				num += buffer.ReadInt32VarLength(3);
				if (!buffer.CanRead())
				{
					break;
				}
				long num2 = Maths.ZigZagDecode(buffer.ReadInt64VarLength(6));
				target[num] = (int)(target[num] + num2);
			}
		}
	}
}

