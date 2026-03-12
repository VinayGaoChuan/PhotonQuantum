using System;
using System.IO;

namespace Photon.Client;

public class StreamBuffer
{
	private const int DefaultInitialSize = 0;

	private int pos;

	private int len;

	private byte[] buf;

	public bool CanRead => true;

	public bool CanSeek => true;

	public bool CanWrite => true;

	public int Length => len;

	public int Position
	{
		get
		{
			return pos;
		}
		set
		{
			pos = value;
			if (len < pos)
			{
				len = pos;
				CheckSize(len);
			}
		}
	}

	/// <summary>
	/// Remaining bytes in this StreamBuffer. Returns 0 if len - pos is less than 0.
	/// </summary>
	public int Available
	{
		get
		{
			int available = len - pos;
			if (available >= 0)
			{
				return available;
			}
			return 0;
		}
	}

	public StreamBuffer(int size = 0)
	{
		buf = new byte[size];
	}

	public StreamBuffer(byte[] buf)
	{
		this.buf = buf;
		len = buf.Length;
	}

	/// <summary>
	/// Allocates a new byte[] that is the exact used length. Use GetBuffer for nonalloc operations.
	/// </summary>
	public byte[] ToArray()
	{
		byte[] res = new byte[len];
		Buffer.BlockCopy(buf, 0, res, 0, len);
		return res;
	}

	/// <summary>
	/// Allocates a new byte[] that is the exact used length. Use GetBuffer for nonalloc operations.
	/// </summary>
	public byte[] ToArrayFromPos()
	{
		int count = len - pos;
		if (count <= 0)
		{
			return new byte[0];
		}
		byte[] res = new byte[count];
		Buffer.BlockCopy(buf, pos, res, 0, count);
		return res;
	}

	/// <summary>
	/// Returns a new ArraySegment for the StreamBuffer starting at Position.
	/// </summary>
	public ArraySegment<byte> ToArraySegmentFromPos()
	{
		int count = len - pos;
		return new ArraySegment<byte>(buf, pos, count);
	}

	/// <summary>
	/// The bytes between Position and Length are copied to the beginning of the buffer. Length decreased by Position. Position set to 0.
	/// </summary>
	public void Compact()
	{
		long remainings = Length - Position;
		if (remainings > 0)
		{
			Buffer.BlockCopy(buf, Position, buf, 0, (int)remainings);
		}
		Position = 0;
		SetLength(remainings);
	}

	public byte[] GetBuffer()
	{
		return buf;
	}

	/// <summary>
	/// Brings StreamBuffer to the state as after writing of 'length' bytes. Returned buffer and offset can be used to actually fill "written" segment with data.
	/// </summary>
	public byte[] GetBufferAndAdvance(int length, out int offset)
	{
		offset = Position;
		Position += length;
		return buf;
	}

	public void Flush()
	{
	}

	public void Reset()
	{
		pos = 0;
		len = 0;
	}

	public long Seek(long offset, SeekOrigin origin)
	{
		int newPos = 0;
		newPos = origin switch
		{
			SeekOrigin.Begin => (int)offset, 
			SeekOrigin.Current => pos + (int)offset, 
			SeekOrigin.End => len + (int)offset, 
			_ => throw new ArgumentException("Invalid seek origin"), 
		};
		if (newPos < 0)
		{
			throw new ArgumentException("Seek before begin");
		}
		if (newPos > len)
		{
			throw new ArgumentException("Seek after end");
		}
		pos = newPos;
		return pos;
	}

	/// <summary>
	/// Sets stream length. If current position is greater than specified value, it's set to the value.
	/// </summary>
	/// <remarks>
	/// SetLength(0) resets the stream to initial state but preserves underlying byte[] buffer.
	/// </remarks>
	public void SetLength(long value)
	{
		len = (int)value;
		CheckSize(len);
		if (pos > len)
		{
			pos = len;
		}
	}

	/// <summary>
	/// Guarantees that the buffer is at least neededSize bytes.
	/// </summary>
	public void SetCapacityMinimum(int neededSize)
	{
		CheckSize(neededSize);
	}

	public int Read(byte[] buffer, int dstOffset, int count)
	{
		int available = len - pos;
		if (available <= 0)
		{
			return 0;
		}
		if (count > available)
		{
			count = available;
		}
		Buffer.BlockCopy(buf, pos, buffer, dstOffset, count);
		pos += count;
		return count;
	}

	public void Write(byte[] buffer, int srcOffset, int count)
	{
		int newPos = pos + count;
		CheckSize(newPos);
		if (newPos > len)
		{
			len = newPos;
		}
		Buffer.BlockCopy(buffer, srcOffset, buf, pos, count);
		pos = newPos;
	}

	public byte ReadByte()
	{
		if (pos >= len)
		{
			throw new EndOfStreamException("SteamBuffer.ReadByte() failed. pos:" + pos + " len:" + len);
		}
		return buf[pos++];
	}

	public void WriteByte(byte value)
	{
		if (pos >= len)
		{
			len = pos + 1;
			CheckSize(len);
		}
		buf[pos++] = value;
	}

	public void WriteBytes(byte v0, byte v1)
	{
		int len1 = pos + 2;
		if (len < len1)
		{
			len = len1;
			CheckSize(len);
		}
		buf[pos++] = v0;
		buf[pos++] = v1;
	}

	public void WriteBytes(byte v0, byte v1, byte v2)
	{
		int len1 = pos + 3;
		if (len < len1)
		{
			len = len1;
			CheckSize(len);
		}
		buf[pos++] = v0;
		buf[pos++] = v1;
		buf[pos++] = v2;
	}

	public void WriteBytes(byte v0, byte v1, byte v2, byte v3)
	{
		int len1 = pos + 4;
		if (len < len1)
		{
			len = len1;
			CheckSize(len);
		}
		buf[pos++] = v0;
		buf[pos++] = v1;
		buf[pos++] = v2;
		buf[pos++] = v3;
	}

	public void WriteBytes(byte v0, byte v1, byte v2, byte v3, byte v4, byte v5, byte v6, byte v7)
	{
		int len1 = pos + 8;
		if (len < len1)
		{
			len = len1;
			CheckSize(len);
		}
		buf[pos++] = v0;
		buf[pos++] = v1;
		buf[pos++] = v2;
		buf[pos++] = v3;
		buf[pos++] = v4;
		buf[pos++] = v5;
		buf[pos++] = v6;
		buf[pos++] = v7;
	}

	private bool CheckSize(int size)
	{
		if (size <= buf.Length)
		{
			return false;
		}
		int s = buf.Length;
		if (s == 0)
		{
			s = 1;
		}
		while (size > s)
		{
			s *= 2;
		}
		byte[] newBuf = new byte[s];
		Buffer.BlockCopy(buf, 0, newBuf, 0, buf.Length);
		buf = newBuf;
		return true;
	}
}
