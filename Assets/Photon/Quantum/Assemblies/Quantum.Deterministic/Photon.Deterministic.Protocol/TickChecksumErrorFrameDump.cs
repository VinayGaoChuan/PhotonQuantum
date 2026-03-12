using System;
using System.Collections.Generic;
using System.Linq;

namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// The message is sent from the client to the server to distribute the game state to compare on other clients.
	/// </summary>
	public class TickChecksumErrorFrameDump : Message
	{
		/// <summary>
		/// The tick of the frame dump.
		/// </summary>
		public int Frame;

		/// <summary>
		/// The block number of the encoded snapshot data.
		/// </summary>
		public int Block;

		/// <summary>
		/// The total chunk count.
		/// </summary>
		public int BlockCount;

		/// <summary>
		/// The actor id of the origin.
		/// </summary>
		public int ActorId;

		/// <summary>
		/// The encoded snapshot data.
		/// </summary>
		public byte[] Data;

		/// <summary>
		/// The message serialization (writing and reading).
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">The bitstream to write to or read from</param>
		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref Frame);
			stream.Serialize(ref Block);
			stream.Serialize(ref BlockCount);
			stream.Serialize(ref ActorId);
			stream.Serialize(ref Data);
		}

		/// <summary>
		/// Debug string with message content.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return string.Format("[{0} {1}={2} {3}={4} {5}={6} {7}={8} {9}={10}]", "TickChecksumErrorFrameDump", "Frame", Frame, "Block", Block, "BlockCount", BlockCount, "ActorId", ActorId, "Data", (Data == null) ? "null" : ((object)Data.Length));
		}

		/// <summary>
		/// Encode a snapshot into multiple <see cref="T:Photon.Deterministic.Protocol.TickChecksumErrorFrameDump" />messages.
		/// </summary>
		/// <param name="tick">The tick of the snapshot.</param>
		/// <param name="dump">The snapshot</param>
		/// <returns>The resulting message fragments to be send</returns>
		public static TickChecksumErrorFrameDump[] Encode(int tick, byte[] dump)
		{
			byte[] array = ByteUtils.GZipCompressBytes(dump);
			int num = array.Length / 10240 + Math.Min(1, array.Length % 10240);
			TickChecksumErrorFrameDump[] array2 = new TickChecksumErrorFrameDump[num];
			for (int i = 0; i < num; i++)
			{
				array2[i] = new TickChecksumErrorFrameDump
				{
					Frame = tick,
					Block = i,
					BlockCount = num,
					Data = CopyBlock(array, i, 10240)
				};
			}
			return array2;
		}

		/// <summary>
		/// Decode multiple <see cref="T:Photon.Deterministic.Protocol.TickChecksumErrorFrameDump" /> messages into a single snapshot.
		/// </summary>
		/// <param name="blocks">The messages</param>
		/// <returns><see langword="true" /> and the assembled snapshot or <see langword="false" /></returns>
		/// <exception cref="T:System.InvalidOperationException">Is raised when the input messages are incomplete</exception>
		public static QTuple<bool, byte[]> Decode(IEnumerable<TickChecksumErrorFrameDump> blocks)
		{
			if (blocks.Count() == 0)
			{
				return default(QTuple<bool, byte[]>);
			}
			TickChecksumErrorFrameDump tickChecksumErrorFrameDump = blocks.FirstOrDefault();
			if (tickChecksumErrorFrameDump.BlockCount != blocks.Count())
			{
				return default(QTuple<bool, byte[]>);
			}
			IEnumerable<IGrouping<int, TickChecksumErrorFrameDump>> source = from x in blocks
				group x by x.Block;
			if (source.Count() != blocks.Count())
			{
				throw new InvalidOperationException("Duplicate blocks found");
			}
			int num = blocks.Sum((TickChecksumErrorFrameDump x) => x.Data.Length);
			byte[] array = new byte[num];
			int num2 = 0;
			foreach (TickChecksumErrorFrameDump item in blocks.OrderBy((TickChecksumErrorFrameDump x) => x.Block))
			{
				Array.Copy(item.Data, 0, array, num2, item.Data.Length);
				num2 += item.Data.Length;
			}
			return QTuple.Create(item0: true, ByteUtils.GZipDecompressBytes(array));
		}

		private static byte[] CopyBlock(byte[] data, int block, int blockSize)
		{
			int num = block * blockSize;
			if (num + blockSize > data.Length)
			{
				blockSize = data.Length % blockSize;
			}
			byte[] array = new byte[blockSize];
			Array.Copy(data, num, array, 0, blockSize);
			return array;
		}
	}
}

