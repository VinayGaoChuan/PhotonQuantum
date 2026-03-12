using System;
using System.Collections.Generic;

namespace Photon.Client;

/// <summary>
/// Simple Queue-based pool for objects. Thread-safe (will lock).
/// </summary>
/// <typeparam name="T">Type of the items to pool.</typeparam>
public class Pool<T> where T : class
{
	private readonly Func<T> createFunction;

	private readonly Queue<T> pool;

	private readonly Action<T> resetFunction;

	public int Count
	{
		get
		{
			lock (pool)
			{
				return pool.Count;
			}
		}
	}

	public Pool(Func<T> createFunction, Action<T> resetFunction, int poolCapacity)
	{
		this.createFunction = createFunction;
		this.resetFunction = resetFunction;
		pool = new Queue<T>();
		CreatePoolItems(poolCapacity);
	}

	public Pool(Func<T> createFunction, int poolCapacity)
		: this(createFunction, (Action<T>)null, poolCapacity)
	{
	}

	private void CreatePoolItems(int numItems)
	{
		for (int i = 0; i < numItems; i++)
		{
			T item = createFunction();
			pool.Enqueue(item);
		}
	}

	public void Release(T item)
	{
		if (item == null)
		{
			throw new ArgumentNullException("Pushing null as item is not allowed.");
		}
		if (resetFunction != null)
		{
			resetFunction(item);
		}
		lock (pool)
		{
			pool.Enqueue(item);
		}
	}

	public T Acquire()
	{
		lock (pool)
		{
			if (pool.Count == 0)
			{
				return createFunction();
			}
			return pool.Dequeue();
		}
	}
}
