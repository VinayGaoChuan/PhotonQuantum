using System;
using System.Collections.Generic;

namespace Photon.Deterministic.Protocol
{
	/// <summary>
	/// The Quantum online protocol serializer.
	/// </summary>
	public class Serializer
	{
		private Dictionary<Type, byte> _typeToId = new Dictionary<Type, byte>();

		private Dictionary<byte, Message> _idToType = new Dictionary<byte, Message>();

		/// <summary>
		/// The protocol version set internally.
		/// </summary>
		public string ProtocolVersion;

		/// <summary>
		/// Serializer constructor, registers all messages.
		/// </summary>
		public Serializer()
		{
			RegisterPrototype(1, new Join());
			RegisterPrototype(5, new SimulationStart());
			RegisterPrototype(6, new SimulationStop());
			RegisterPrototype(8, new TickChecksum());
			RegisterPrototype(9, new TickChecksumError());
			RegisterPrototype(10, new RttUpdate());
			RegisterPrototype(11, new AddPlayer());
			RegisterPrototype(12, new Disconnect());
			RegisterPrototype(13, new FrameSnapshot());
			RegisterPrototype(14, new Command());
			RegisterPrototype(15, new TickChecksumErrorFrameDump());
			RegisterPrototype(17, new FrameSnapshotRequest());
			RegisterPrototype(18, new RemovePlayer());
			RegisterPrototype(19, new RemovePlayerFailed());
			RegisterPrototype(20, new AddPlayerFailed());
			RegisterPrototype(21, new StartRequest());
			RegisterPrototype(22, new GameResult());
		}

		private void RegisterPrototype(byte id, Message message)
		{
			_idToType.Add(id, message);
			_typeToId.Add(message.GetType(), id);
		}

		internal bool PackNext(BitStream s, Message msg)
		{
			int position = s.Position;
			s.WriteByte(_typeToId[msg.GetType()]);
			msg.Serialize(this, s);
			if (s.Overflowing)
			{
				s.Position = position;
				return false;
			}
			return true;
		}

		/// <summary>
		/// Dispatching of messages.
		/// </summary>
		/// <param name="s">Bitstream to read from.</param>
		/// <param name="msg">Messages being read from the stream</param>
		/// <returns><see langword="true" /> if all messages have been dispatched</returns>
		public bool ReadNext(BitStream s, out Message msg)
		{
			try
			{
				if (!s.CanRead(8))
				{
					msg = null;
					return false;
				}
				byte key = s.ReadByte();
				msg = _idToType[key].Clone();
				msg.Serialize(this, s);
				return true;
			}
			catch
			{
				msg = null;
				return false;
			}
		}

		/// <summary>
		/// Pack messages into a bitstream.
		/// </summary>
		/// <param name="stream">Bitstream to write messages to.</param>
		/// <param name="queue">Queue of messages to process</param>
		/// <returns><see langword="true" /> when queue is empty</returns>
		/// <exception cref="T:System.InvalidOperationException">Is raised when the batch message size limit was exceeded</exception>
		public bool PackMessages(BitStream stream, Queue<Message> queue)
		{
			stream.Reset(51200);
			stream.Writing = true;
			if (queue.Count == 0)
			{
				return false;
			}
			while (queue.Count > 0)
			{
				if (PackNext(stream, queue.Peek()))
				{
					queue.Dequeue();
					continue;
				}
				if (stream.Position != 0)
				{
					break;
				}
				throw new InvalidOperationException("Tried to write a message larger than " + 51200 + " bytes");
			}
			return stream.Position > 0;
		}
	}
}

