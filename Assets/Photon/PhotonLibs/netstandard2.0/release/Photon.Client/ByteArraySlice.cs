using System;

namespace Photon.Client;

/// <summary>A slice of memory that should be pooled and reused. Wraps a byte-array.</summary>
/// <remarks>
/// This is a serializable datatype for the .Net clients. It will serialize and transfer as byte[].
/// If PhotonPeer.UseByteArraySlicePoolForEvents is enabled, byte-arrays in (incoming) events will be deserialized as
/// ByteArraySlice.
///
/// Adjust your OnEvent code accordingly.
/// </remarks>
public class ByteArraySlice : IDisposable
{
	/// <summary>The buffer for the slice.</summary>
	public byte[] Buffer;

	/// <summary>The position where the content starts in Buffer.</summary>
	public int Offset;

	/// <summary>The length of the data in the Buffer.</summary>
	public int Count;

	private readonly ByteArraySlicePool returnPool;

	private readonly int stackIndex;

	/// <summary>
	/// Internal constructor - these instances will be part of the pooling system.
	/// </summary>
	/// <param name="returnPool">The pool to return to.</param>
	/// <param name="stackIndex">The index to return to (in the related returnPool).</param>
	internal ByteArraySlice(ByteArraySlicePool returnPool, int stackIndex)
	{
		Buffer = ((stackIndex == 0) ? null : new byte[1 << stackIndex]);
		this.returnPool = returnPool;
		this.stackIndex = stackIndex;
	}

	/// <summary>
	/// Create a new ByteArraySlice. The buffer supplied will be used. Usage is similar to ArraySegment.
	/// </summary>
	/// <remarks>Not part of pooling.</remarks>
	public ByteArraySlice(byte[] buffer, int offset = 0, int count = 0)
	{
		Buffer = buffer;
		Count = count;
		Offset = offset;
		returnPool = null;
		stackIndex = -1;
	}

	/// <summary>
	/// Creates a ByteArraySlice, which is not pooled. It has no Buffer.
	/// </summary>
	/// <remarks>Not part of pooling.</remarks>
	public ByteArraySlice()
	{
		returnPool = null;
		stackIndex = -1;
	}

	/// <summary>Makes this class IDisposable. Calls Release().</summary>
	public void Dispose()
	{
		Release();
	}

	/// <summary>
	/// If this item was fetched from a ByteArraySlicePool, this will return it.
	/// </summary>
	/// <returns>
	/// True if this was a pooled item and it successfully was returned.
	/// If it does not belong to a pool nothing will happen, and false will be returned.
	/// </returns>
	public bool Release()
	{
		if (stackIndex < 0)
		{
			return false;
		}
		Count = 0;
		Offset = 0;
		return returnPool.Release(this, stackIndex);
	}

	/// <summary>Resets Count and Offset to 0 each.</summary>
	public void Reset()
	{
		Count = 0;
		Offset = 0;
	}
}
