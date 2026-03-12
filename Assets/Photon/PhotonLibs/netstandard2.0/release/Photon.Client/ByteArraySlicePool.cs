using System;
using System.Collections.Generic;

namespace Photon.Client;

/// <summary>Tiered pool for ByteArraySlices. Reduces the allocations once enough slices are available.</summary>
public class ByteArraySlicePool
{
	private int minStackIndex = 7;

	internal readonly Stack<ByteArraySlice>[] poolTiers = new Stack<ByteArraySlice>[32];

	private int allocationCounter;

	/// <summary>
	/// Requests for buffers smaller than 2^minStackIndex will use 2^minStackIndex. This value avoids allocations of smaller rarely used buffers.
	/// Set this to a lower value if you will never need sizes larger than byte[2^minStackIndex]
	/// </summary>
	public int MinStackIndex
	{
		get
		{
			return minStackIndex;
		}
		set
		{
			minStackIndex = ((value <= 0) ? 1 : ((value < 31) ? value : 31));
		}
	}

	/// <summary>Count of allocations this pool did.</summary>
	public int AllocationCounter => allocationCounter;

	/// <summary>Creates a new pool.</summary>
	public ByteArraySlicePool()
	{
		lock (poolTiers)
		{
			poolTiers[0] = new Stack<ByteArraySlice>();
		}
	}

	/// <summary>
	/// Get a ByteArraySlice from pool. This overload handles user supplied byte[] and byte count and can be used as a non-boxing alternative to ArraySegment&lt;byte&gt;.
	/// </summary>
	/// <remarks>
	/// This effectively pools the ByteArraySlice instances but not their data.
	/// ByteArraySlice.Release() will return the slice itself to the pool but delete the reference to the buffer supplied here.
	/// </remarks>
	public ByteArraySlice Acquire(byte[] buffer, int offset = 0, int count = 0)
	{
		ByteArraySlice item;
		lock (poolTiers)
		{
			lock (poolTiers[0])
			{
				item = PopOrCreate(poolTiers[0], 0);
			}
		}
		item.Buffer = buffer;
		item.Offset = offset;
		item.Count = count;
		return item;
	}

	/// <summary>
	/// Get byte[] wrapper from pool. This overload accepts a bytecount and will return a wrapper with a byte[] that size or greater.
	/// </summary>
	public ByteArraySlice Acquire(int minByteCount)
	{
		if (minByteCount < 0)
		{
			throw new Exception(typeof(ByteArraySlice).Name + " requires a positive minByteCount.");
		}
		int stackIdx = minStackIndex;
		if (minByteCount > 0)
		{
			for (int bytes = minByteCount - 1; stackIdx < 32 && bytes >> stackIdx != 0; stackIdx++)
			{
			}
		}
		lock (poolTiers)
		{
			Stack<ByteArraySlice> stack = poolTiers[stackIdx];
			if (stack == null)
			{
				stack = new Stack<ByteArraySlice>();
				poolTiers[stackIdx] = stack;
			}
			lock (stack)
			{
				return PopOrCreate(stack, stackIdx);
			}
		}
	}

	/// <summary>Pops a slice from the stack or creates a new slice for that stack.</summary>
	/// <param name="stack">The stack to use. Lock that stack before calling PopOrCreate for thread safety.</param>
	/// <param name="stackIndex"></param>
	/// <returns>A slice.</returns>
	private ByteArraySlice PopOrCreate(Stack<ByteArraySlice> stack, int stackIndex)
	{
		lock (stack)
		{
			if (stack.Count > 0)
			{
				return stack.Pop();
			}
		}
		ByteArraySlice result = new ByteArraySlice(this, stackIndex);
		allocationCounter++;
		return result;
	}

	/// <summary>
	/// Releasing a ByteArraySlice, will put it back into the pool, if it was acquired from one.
	/// </summary>
	/// <param name="slice">The ByteArraySlice to return to the pool.</param>
	/// <param name="stackIndex">The stackIndex for this slice.</param>
	/// <returns>True if this slice was returned to some pool. False if not (null or stackIndex &lt; 0.</returns>
	internal bool Release(ByteArraySlice slice, int stackIndex)
	{
		if (slice == null || stackIndex < 0)
		{
			return false;
		}
		if (stackIndex == 0)
		{
			slice.Buffer = null;
		}
		lock (poolTiers)
		{
			lock (poolTiers[stackIndex])
			{
				poolTiers[stackIndex].Push(slice);
			}
		}
		return true;
	}

	/// <summary>
	/// Clears all pool items with byte array sizes between lower and upper inclusively.
	/// </summary>
	/// <remarks>
	/// Use this if you sent some unusually large RaiseEvents and believe the buffers of that size
	/// will not be needed again, and you would like to free up the buffer memory.
	/// </remarks>
	public void ClearPools(int lower = 0, int upper = int.MaxValue)
	{
		_ = minStackIndex;
		for (int i = 0; i < 32; i++)
		{
			int stackByteCount = 1 << i;
			if (stackByteCount < lower || stackByteCount > upper)
			{
				continue;
			}
			lock (poolTiers)
			{
				if (poolTiers[i] != null)
				{
					lock (poolTiers[i])
					{
						poolTiers[i].Clear();
					}
				}
			}
		}
	}
}
