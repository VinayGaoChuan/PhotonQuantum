using System;
using System.Collections.Generic;

namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// The client sends a snapshot of the game state to the server.
	/// The message is send multiple times if the snapshot is too large.
	/// </summary>
	public class FrameSnapshot : Message
	{
		/// <summary>
		/// Maximum chunk size.
		/// </summary>
		public const int MaxChunkSize = 49152;

		/// <summary>
		/// The tick of the snapshot.
		/// </summary>
		public int Tick;

		/// <summary>
		/// The total size of the snapshot.
		/// </summary>
		public int TotalSize;

		/// <summary>
		/// If this message is the last chunk of the snapshot.
		/// </summary>
		public bool Last;

		/// <summary>
		/// The snapshot data chunk.
		/// </summary>
		public byte[] Data;

		/// <summary>
		/// The message serialization (writing and reading).
		/// </summary>
		/// <param name="serializer">Quantum protocol serializer</param>
		/// <param name="stream">The bitstream to write to or read from</param>
		public override void Serialize(Serializer serializer, BitStream stream)
		{
			stream.Serialize(ref Tick);
			stream.Serialize(ref TotalSize);
			stream.Serialize(ref Last);
			stream.Serialize(ref Data);
		}

		/// <summary>
		/// Debug string with message content.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return string.Format(string.Format("[{0} {1}={2} {3}={4} {5}={6} {7}={8}]", "FrameSnapshot", "Tick", Tick, "TotalSize", TotalSize, "Last", Last, "Data", (Data == null) ? "null" : ((object)Data.Length)));
		}

		/// <summary>
		/// Encode a snapshot into multiple <see cref="T:Photon.Deterministic.Protocol.FrameSnapshot" /> messages.
		/// </summary>
		/// <param name="tick">The tick of the snapshot.</param>
		/// <param name="data">The snapshot data</param>
		/// <returns>The messages to send.</returns>
		public static FrameSnapshot[] Encode(int tick, byte[] data)
		{
			List<FrameSnapshot> list = new List<FrameSnapshot>();
			int num = 0;
			while (num < data.Length)
			{
				int num2 = data.Length - num;
				bool flag = num2 <= 49152;
				int num3 = (flag ? num2 : 49152);
				byte[] array = new byte[num3];
				Array.Copy(data, num, array, 0, num3);
				num += num3;
				FrameSnapshot item = new FrameSnapshot
				{
					Tick = tick,
					TotalSize = data.Length,
					Data = array,
					Last = flag
				};
				list.Add(item);
			}
			return list.ToArray();
		}

		/// <summary>
		/// Decode multiple messages into a snapshot.
		/// </summary>
		/// <param name="snapshots">The snapshot messages</param>
		/// <param name="data">The resulting snapshot data</param>
		/// <param name="tick">The tick of the snapshot</param>
		public static void Decode(FrameSnapshot[] snapshots, ref byte[] data, ref int tick)
		{
			data = new byte[snapshots[0].TotalSize];
			tick = snapshots[0].Tick;
			int num = 0;
			foreach (FrameSnapshot frameSnapshot in snapshots)
			{
				Array.Copy(frameSnapshot.Data, 0, data, num, frameSnapshot.Data.Length);
				num += frameSnapshot.Data.Length;
			}
		}
	}
}

