using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Photon.Deterministic
{
	/// <summary>
	/// Utility class for working with bytes.
	/// </summary>
	public static class ByteUtils
	{
		/// <summary>
		/// Convert a pointer to a byte array.
		/// </summary>
		/// <param name="ptr">The pointer to convert.</param>
		/// <param name="length">The length of the byte array.</param>
		/// <returns>The byte array.</returns>
		public unsafe static byte[] ToByteArray(byte* ptr, int length)
		{
			byte[] array = new byte[length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = ptr[i];
			}
			return array;
		}

		/// <summary>
		/// Merges multiple byte blocks into a single byte array.
		/// </summary>
		/// <param name="blocks">The byte blocks to merge.</param>
		/// <returns>The merged byte array.</returns>
		public static byte[] MergeByteBlocks(params byte[][] blocks)
		{
			int num = blocks.Select((byte[] x) => x.Length).Sum();
			byte[] array = new byte[num];
			int num2 = 0;
			for (int num3 = 0; num3 < blocks.Length; num3++)
			{
				Array.Copy(blocks[num3], 0, array, num2, blocks[num3].Length);
				num2 += blocks[num3].Length;
			}
			return array;
		}

		/// <summary>
		/// Adds a value block to a byte array.
		/// </summary>
		/// <param name="value">The value to add to the byte array.</param>
		/// <param name="buffer">The byte array to add the value block to.</param>
		/// <param name="offset">The offset in the byte array to start adding the value block.</param>
		/// <returns>The new offset in the byte array after adding the value block.</returns>
		public static int AddValueBlock(int value, byte[] buffer, int offset)
		{
			offset += WriteBytes(4, buffer, offset);
			offset += WriteBytes(value, buffer, offset);
			return offset;
		}

		/// <summary>
		/// Adds a value block to the specified byte array at the given offset.
		/// </summary>
		/// <param name="value">The value to add to the byte array.</param>
		/// <param name="buffer">The byte array.</param>
		/// <param name="offset">The offset at which to add the value block.</param>
		/// <returns>The updated offset after adding the value block.</returns>
		public static int AddValueBlock(long value, byte[] buffer, int offset)
		{
			offset += WriteBytes(8, buffer, offset);
			offset += WriteBytes(value, buffer, offset);
			return offset;
		}

		/// <summary>
		/// Adds a value block to the specified byte array at the given offset.
		/// </summary>
		/// <param name="value">The value to add to the byte array.</param>
		/// <param name="buffer">The byte array.</param>
		/// <param name="offset">The offset at which to add the value block.</param>
		/// <returns>The updated offset after adding the value block.</returns>
		public static int AddValueBlock(ulong value, byte[] buffer, int offset)
		{
			return AddValueBlock((long)value, buffer, offset);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="block"></param>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <returns></returns>
		public static int AddByteBlock(byte[] block, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes(block.Length), 0, buffer, offset, 4);
			offset += 4;
			Array.Copy(block, 0, buffer, offset, block.Length);
			offset += block.Length;
			return offset;
		}

		/// <summary>
		/// Begins a byte block header in the given buffer at the specified offset.
		/// </summary>
		/// <param name="buffer">The byte array buffer.</param>
		/// <param name="offset">The offset at which to begin the byte block header.</param>
		/// <param name="blockStart">The starting index of the byte block.</param>
		/// <returns>The new offset after the byte block header is initialized.</returns>
		public static int BeginByteBlockHeader(byte[] buffer, int offset, out int blockStart)
		{
			blockStart = offset;
			return offset += 4;
		}

		/// <summary>
		/// Ends the byte block header by updating the block size in the byte array.
		/// </summary>
		/// <param name="buffer">The byte array containing the block header.</param>
		/// <param name="blockStart">The start index of the block header.</param>
		/// <param name="bytesWritten">The number of bytes written in the block.</param>
		/// <returns>The updated index after updating the block size in the byte array.</returns>
		public static int EndByteBlockHeader(byte[] buffer, int blockStart, int bytesWritten)
		{
			return blockStart + WriteBytes(bytesWritten, buffer, blockStart) + bytesWritten;
		}

		/// <summary>
		/// Takes multiple byte blocks and packs them into a single byte array.
		/// </summary>
		/// <param name="blocks">The byte blocks to pack.</param>
		/// <returns>The packed byte array.</returns>
		public static byte[] PackByteBlocks(params byte[][] blocks)
		{
			int num = blocks.Select((byte[] x) => x.Length).Sum() + blocks.Length * 4;
			byte[] array = new byte[num];
			int num2 = 0;
			for (int num3 = 0; num3 < blocks.Length; num3++)
			{
				Array.Copy(BitConverter.GetBytes(blocks[num3].Length), 0, array, num2, 4);
				num2 += 4;
				Array.Copy(blocks[num3], 0, array, num2, blocks[num3].Length);
				num2 += blocks[num3].Length;
			}
			return array;
		}

		/// <summary>
		/// Read byte blocks from a byte array.
		/// </summary>
		/// <param name="data">The byte array containing the byte blocks.</param>
		/// <returns>An enumerable collection of byte arrays, each representing a byte block.</returns>
		public static IEnumerable<byte[]> ReadByteBlocks(byte[] data)
		{
			int dataOffset = 0;
			while (dataOffset < data.Length)
			{
				byte[] array = new byte[BitConverter.ToInt32(data, dataOffset)];
				dataOffset += 4;
				Array.Copy(data, dataOffset, array, 0, array.Length);
				dataOffset += array.Length;
				yield return array;
			}
		}

		/// <summary>
		/// Print the bits of an array starting at the specified offset and for the given length.
		/// </summary>
		/// <param name="array">The array to print the bits from.</param>
		/// <param name="offset">The offset within the array to start printing from.</param>
		/// <param name="length">The length of the bits to print.</param>
		/// <returns>A string representation of the bits.</returns>
		public static string PrintBits(Array array, int offset, int length)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < length; i++)
			{
				byte b = Buffer.GetByte(array, offset + i);
				for (int j = 0; j < 8; j++)
				{
					stringBuilder.Append(((b & (1 << j)) == 0) ? "0" : "1");
				}
				if (i + 1 != length)
				{
					stringBuilder.Append(" ");
				}
			}
			return stringBuilder.ToString();
		}

		/// <summary>
		/// Copies data from the source stream to the destination stream.
		/// </summary>
		/// <param name="source">The source stream to copy from.</param>
		/// <param name="destination">The destination stream to copy to.</param>
		private static void CopyTo(Stream source, Stream destination)
		{
			byte[] array = new byte[4096];
			int num = 0;
			while ((num = source.Read(array, 0, array.Length)) != 0)
			{
				destination.Write(array, 0, num);
			}
		}

		/// <summary>
		/// Base64 encodes a string using the specified encoding.
		/// </summary>
		/// <param name="data">The string to be encoded.</param>
		/// <param name="encoding">The encoding to be used.</param>
		/// <returns>The encoded string.</returns>
		public static string Base64EncodeString(string data, Encoding encoding)
		{
			return Base64Encode(encoding.GetBytes(data));
		}

		/// <summary>
		/// Decodes a Base64-encoded string to its original string representation using the specified encoding.
		/// </summary>
		/// <param name="data">The Base64-encoded string to decode.</param>
		/// <param name="encoding">The encoding to use for the decoded string.</param>
		/// <returns>The decoded string.</returns>
		public static string Base64DecodeString(string data, Encoding encoding)
		{
			return encoding.GetString(Base64Decode(data));
		}

		/// <summary>
		/// Encodes a byte array to a Base64 string.
		/// </summary>
		/// <param name="data">The byte array to encode.</param>
		/// <returns>The Base64 encoded string.</returns>
		public static string Base64Encode(byte[] data)
		{
			return Convert.ToBase64String(data);
		}

		/// <summary>
		/// Decodes a Base64 encoded string into a byte array.
		/// </summary>
		/// <param name="data">The Base64 encoded string to decode.</param>
		/// <returns>The decoded byte array.</returns>
		public static byte[] Base64Decode(string data)
		{
			return Convert.FromBase64String(data);
		}

		/// <summary>
		/// Compresses a byte array using GZip.
		/// </summary>
		/// <param name="data">The data to compress.</param>
		/// <returns></returns>
		public static byte[] GZipCompressBytes(byte[] data)
		{
			using MemoryStream memoryStream = new MemoryStream();
			GZipCompressBytes(data, 0, data.Length, memoryStream);
			return memoryStream.ToArray();
		}

		/// <summary>
		/// Compresses a byte array using GZip.
		/// </summary>
		/// <param name="data">The data to compress.</param>
		/// <param name="offset">The offset in the data array to start compressing from.</param>
		/// <param name="size">The size of the data to compress.</param>
		/// <param name="output">The stream to write the data to.</param>
		public static void GZipCompressBytes(byte[] data, int offset, int size, Stream output)
		{
			using MemoryStream input = new MemoryStream(data, offset, size);
			GZipCompressBytes(input, output);
		}

		/// <summary>
		/// Compresses a byte array using GZip.
		/// </summary>
		/// <param name="input">The stream to read the data from.</param>
		/// <param name="output">The stream to write the compressed data to.</param>
		public static void GZipCompressBytes(Stream input, Stream output)
		{
			using GZipStream destination = CreateGZipCompressStream(output);
			CopyTo(input, destination);
		}

		/// <summary>
		/// Decompresses a byte array using GZip.
		/// </summary>
		/// <param name="output">The stream to write the data to.</param>
		/// <returns>The compressed stream.</returns>
		public static GZipStream CreateGZipCompressStream(Stream output)
		{
			return new GZipStream(output, CompressionMode.Compress, leaveOpen: true);
		}

		/// <summary>
		/// Decompresses a byte array using GZip.
		/// </summary>
		/// <param name="output">The stream to write the data to.</param>
		/// <returns>The decompressed stream.</returns>
		public static GZipStream CreateGZipDecompressStream(Stream output)
		{
			return new GZipStream(output, CompressionMode.Decompress, leaveOpen: true);
		}

		/// <summary>
		/// Decompresses a byte array using GZip.
		/// </summary>
		/// <param name="data">The data to decompress</param>
		/// <returns>The decompressed data.</returns>
		public static byte[] GZipDecompressBytes(byte[] data)
		{
			using MemoryStream stream = new MemoryStream(data);
			using MemoryStream memoryStream = new MemoryStream();
			using (GZipStream source = new GZipStream(stream, CompressionMode.Decompress))
			{
				CopyTo(source, memoryStream);
			}
			return memoryStream.ToArray();
		}

		/// <summary>
		/// Compresses a byte array using GZip.
		/// </summary>
		/// <param name="data">The string data to decompress.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <returns>The compressed string.</returns>
		public static byte[] GZipCompressString(string data, Encoding encoding)
		{
			using MemoryStream source = new MemoryStream(encoding.GetBytes(data));
			using MemoryStream memoryStream = new MemoryStream();
			using (GZipStream destination = new GZipStream(memoryStream, CompressionMode.Compress))
			{
				CopyTo(source, destination);
			}
			return memoryStream.ToArray();
		}

		/// <summary>
		/// Decompresses a byte array using GZip.
		/// </summary>
		/// <param name="data">The string data to decompress.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <returns>The decompressed string.</returns>
		public static string GZipDecompressString(byte[] data, Encoding encoding)
		{
			using MemoryStream stream = new MemoryStream(data);
			using MemoryStream memoryStream = new MemoryStream();
			using (GZipStream source = new GZipStream(stream, CompressionMode.Decompress))
			{
				CopyTo(source, memoryStream);
			}
			return encoding.GetString(memoryStream.ToArray());
		}

		/// <summary>
		/// Decompresses a byte array using GZip.
		/// </summary>
		/// <param name="data">The string data to decompress.</param>
		/// <param name="offset">The offset in the data array to start decompressing from.</param>
		/// <param name="size">The size of the data to decompress.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <returns>The decompressed string.</returns>
		public static string GZipDecompressString(byte[] data, int offset, int size, Encoding encoding)
		{
			using MemoryStream stream = new MemoryStream(data, offset, size);
			using MemoryStream memoryStream = new MemoryStream();
			using (GZipStream source = new GZipStream(stream, CompressionMode.Decompress))
			{
				CopyTo(source, memoryStream);
			}
			return encoding.GetString(memoryStream.ToArray());
		}

		/// <summary>
		/// Writes a long value to a byte array at the specified offset.
		/// </summary>
		/// <param name="value">The long value to write.</param>
		/// <param name="array">The byte array to write the value to.</param>
		/// <param name="offset">The offset in the byte array to write the value to.</param>
		/// <returns>The length written in bytes.</returns>
		public unsafe static int WriteBytes(long value, byte[] array, int offset)
		{
			fixed (byte* ptr = array)
			{
				byte* ptr2 = ptr + offset;
				*(long*)ptr2 = value;
				return 8;
			}
		}

		/// <summary>
		/// Writes an unsigned long value to a byte array at the specified offset.
		/// </summary>
		/// <param name="value">The unsigned long value to write.</param>
		/// <param name="array">The byte array to write the value to.</param>
		/// <param name="offset">The offset in the byte array to write the value to.</param>
		public static int WriteBytes(ulong value, byte[] array, int offset)
		{
			return WriteBytes((long)value, array, offset);
		}

		/// <summary>
		/// Writes an integer value to a byte array at the specified offset.
		/// </summary>
		/// <param name="value">The integer value to write.</param>
		/// <param name="array">The byte array to write the value to.</param>
		/// <param name="offset">The offset in the byte array to write the value to.</param>
		public unsafe static int WriteBytes(int value, byte[] array, int offset)
		{
			fixed (byte* ptr = array)
			{
				byte* ptr2 = ptr + offset;
				*(int*)ptr2 = value;
				return 4;
			}
		}
	}
}

