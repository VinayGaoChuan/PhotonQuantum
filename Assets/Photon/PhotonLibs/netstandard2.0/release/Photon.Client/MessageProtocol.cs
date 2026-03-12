using System;

namespace Photon.Client;

/// <summary>
/// Provides tools for the Exit Games Protocol
/// </summary>
public class MessageProtocol
{
	private static readonly float[] memFloatBlock = new float[1];

	private static readonly byte[] memDeserialize = new byte[4];

	/// <summary>
	/// Serializes a short typed value into a byte-array (target) starting at the also given targetOffset.
	/// The altered offset is known to the caller, because it is given via a referenced parameter.
	/// </summary>
	/// <param name="value">The short value to be serialized</param>
	/// <param name="target">The byte-array to serialize the short to</param>
	/// <param name="targetOffset">The offset in the byte-array</param>
	public static void Serialize(short value, byte[] target, ref int targetOffset)
	{
		target[targetOffset++] = (byte)(value >> 8);
		target[targetOffset++] = (byte)value;
	}

	/// <summary>
	/// Deserialize fills the given short typed value with the given byte-array (source) starting at the also given offset.
	/// The result is placed in a variable (value). There is no need to return a value because the parameter value is given by reference.
	/// The altered offset is this way also known to the caller.
	/// </summary>
	/// <param name="value">The short value to deserialized into</param>
	/// <param name="source">The byte-array to deserialize from</param>
	/// <param name="offset">The offset in the byte-array</param>
	public static void Deserialize(out short value, byte[] source, ref int offset)
	{
		value = (short)((source[offset++] << 8) | source[offset++]);
	}

	/// <summary>
	/// Serializes an int typed value into a byte-array (target) starting at the also given targetOffset.
	/// The altered offset is known to the caller, because it is given via a referenced parameter.
	/// </summary>
	/// <param name="value">The int value to be serialized</param>
	/// <param name="target">The byte-array to serialize the short to</param>
	/// <param name="targetOffset">The offset in the byte-array</param>
	public static void Serialize(int value, byte[] target, ref int targetOffset)
	{
		target[targetOffset++] = (byte)(value >> 24);
		target[targetOffset++] = (byte)(value >> 16);
		target[targetOffset++] = (byte)(value >> 8);
		target[targetOffset++] = (byte)value;
	}

	/// <summary>
	/// Deserialize fills the given int typed value with the given byte-array (source) starting at the also given offset.
	/// The result is placed in a variable (value). There is no need to return a value because the parameter value is given by reference.
	/// The altered offset is this way also known to the caller.
	/// </summary>
	/// <param name="value">The int value to deserialize into</param>
	/// <param name="source">The byte-array to deserialize from</param>
	/// <param name="offset">The offset in the byte-array</param>
	public static void Deserialize(out int value, byte[] source, ref int offset)
	{
		value = (source[offset++] << 24) | (source[offset++] << 16) | (source[offset++] << 8) | source[offset++];
	}

	/// <summary>
	/// Serializes an float typed value into a byte-array (target) starting at the also given targetOffset.
	/// The altered offset is known to the caller, because it is given via a referenced parameter.
	/// </summary>
	/// <param name="value">The float value to be serialized</param>
	/// <param name="target">The byte-array to serialize the short to</param>
	/// <param name="targetOffset">The offset in the byte-array</param>
	public static void Serialize(float value, byte[] target, ref int targetOffset)
	{
		lock (memFloatBlock)
		{
			memFloatBlock[0] = value;
			Buffer.BlockCopy(memFloatBlock, 0, target, targetOffset, 4);
		}
		if (BitConverter.IsLittleEndian)
		{
			byte temp0 = target[targetOffset];
			byte temp1 = target[targetOffset + 1];
			target[targetOffset] = target[targetOffset + 3];
			target[targetOffset + 1] = target[targetOffset + 2];
			target[targetOffset + 2] = temp1;
			target[targetOffset + 3] = temp0;
		}
		targetOffset += 4;
	}

	/// <summary>
	/// Deserialize fills the given float typed value with the given byte-array (source) starting at the also given offset.
	/// The result is placed in a variable (value). There is no need to return a value because the parameter value is given by reference.
	/// The altered offset is this way also known to the caller.
	/// </summary>
	/// <param name="value">The float value to deserialize</param>
	/// <param name="source">The byte-array to deserialize from</param>
	/// <param name="offset">The offset in the byte-array</param>
	public static void Deserialize(out float value, byte[] source, ref int offset)
	{
		if (BitConverter.IsLittleEndian)
		{
			lock (memDeserialize)
			{
				byte[] data = memDeserialize;
				data[3] = source[offset++];
				data[2] = source[offset++];
				data[1] = source[offset++];
				data[0] = source[offset++];
				value = BitConverter.ToSingle(data, 0);
				return;
			}
		}
		value = BitConverter.ToSingle(source, offset);
		offset += 4;
	}
}
