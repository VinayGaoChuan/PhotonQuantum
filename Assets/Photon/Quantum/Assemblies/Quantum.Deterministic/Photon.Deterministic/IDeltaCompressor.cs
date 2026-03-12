namespace Photon.Deterministic
{
	public interface IDeltaCompressor
	{
		void Pack(int[] current, int[] shared, int words, BitStream buffer);

		void Unpack(int[] target, int words, BitStream buffer);
	}
}

