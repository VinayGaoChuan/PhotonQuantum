using System;
using System.Collections.Generic;
using Quantum;

namespace Photon.Deterministic
{
	/// <summary>
	/// The DeterministicCommandSerializer is used to serialize and de-serialize DeterministicCommands.
	/// </summary>
	public class DeterministicCommandSerializer
	{
		private Dictionary<Type, ushort> _typeToId = new Dictionary<Type, ushort>();

		private Dictionary<ushort, IDeterministicCommandFactory> _idToFactory = new Dictionary<ushort, IDeterministicCommandFactory>();

		/// <summary>
		/// The reusable read stream.
		/// Use <see cref="M:Photon.Deterministic.BitStream.SetBuffer(System.Byte[])" /> before using.
		/// </summary>
		public BitStream CommandSerializerStreamRead;

		/// <summary>
		/// The re-useable write stream.
		/// Use <see cref="M:Photon.Deterministic.BitStream.Reset(System.Int32)" /> before using.
		/// </summary>
		public BitStream CommandSerializerStreamWrite;

		/// <summary>
		/// Constructor.
		/// </summary>
		public DeterministicCommandSerializer()
		{
			CommandSerializerStreamRead = new BitStream(8192);
			CommandSerializerStreamRead.Reading = true;
			CommandSerializerStreamWrite = new BitStream(8192);
			CommandSerializerStreamWrite.Writing = true;
		}

		/// <summary>
		/// Register one command factory.
		/// </summary>
		/// <param name="factory">Factory instance</param>
		public void RegisterFactory(IDeterministicCommandFactory factory)
		{
			if (factory != null && typeof(DeterministicCommand).IsAssignableFrom(factory.CommandType) && !_typeToId.ContainsKey(factory.CommandType))
			{
				ushort num = (ushort)(_idToFactory.Count + 1);
				_idToFactory.Add(num, factory);
				_typeToId.Add(factory.CommandType, num);
			}
		}

		/// <summary>
		/// Register a list of command factories.
		/// </summary>
		/// <param name="factories">Factory params</param>
		public void RegisterFactories(params IDeterministicCommandFactory[] factories)
		{
			foreach (IDeterministicCommandFactory factory in factories)
			{
				RegisterFactory(factory);
			}
		}

		/// <summary>
		/// Decodes command message byte array into a specific command type.
		/// </summary>
		/// <typeparam name="T">Command type to cast to</typeparam>
		/// <param name="data">Serialized command data.</param>
		/// <param name="result">Resulting command instance.</param>
		/// <returns>Returns <see langword="true" /> when successfully decoded and casted.</returns>
		public bool TryDecodeCommand<T>(byte[] data, out T result) where T : DeterministicCommand
		{
			result = null;
			CommandSerializerStreamRead.SetBuffer(data);
			CommandSerializerStreamRead.Reading = true;
			if (ReadNext(CommandSerializerStreamRead, out var cmd))
			{
				result = cmd as T;
				return result != null;
			}
			return false;
		}

		/// <summary>
		/// Encodes the command into a byte array.
		/// Creates a new byte array and returns it. 
		/// Reusing byte arrays is not recommended because of the uncertain resulting size.
		/// </summary>
		/// <param name="command">Command instance</param>
		/// <param name="result">Serialized command to be send to the clients for example.</param>
		/// <returns>Returns <see langword="true" /> when successfully encoded.</returns>
		public bool EncodeCommand(DeterministicCommand command, out byte[] result)
		{
			result = null;
			CommandSerializerStreamWrite.Reset(CommandSerializerStreamWrite.Capacity);
			CommandSerializerStreamWrite.Writing = true;
			if (PackNext(CommandSerializerStreamWrite, command))
			{
				result = CommandSerializerStreamWrite.ToArray();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Packing a command into a bitstream.
		/// </summary>
		/// <param name="s">Stream</param>
		/// <param name="cmd">Command object</param>
		/// <returns><see langword="true" /> if successfully written the command into the stream</returns>
		public bool PackNext(BitStream s, DeterministicCommand cmd)
		{
			int position = s.Position;
			if (!_typeToId.TryGetValue(cmd.GetType(), out var value))
			{
				Assert.AlwaysFail($"Command {cmd.GetType()} not registered at the command factory");
			}
			s.WriteUShort(value);
			cmd.Serialize(s, this);
			if (s.Overflowing)
			{
				s.Position = position;
				return false;
			}
			return true;
		}

		/// <summary>
		/// Read commands from the bitstream.
		/// </summary>
		/// <param name="s">Stream</param>
		/// <param name="cmd">Command that was read from the stream.</param>
		/// <returns><see langword="true" /> when a command has successfully been read form the stream.</returns>
		public bool ReadNext(BitStream s, out DeterministicCommand cmd)
		{
			try
			{
				if (!s.CanRead(16))
				{
					cmd = null;
					return false;
				}
				ushort key = s.ReadUShort();
				cmd = _idToFactory[key].GetCommandInstance();
				cmd.Serialize(s, this);
				return true;
			}
			catch
			{
				cmd = null;
				return false;
			}
		}
	}
}

