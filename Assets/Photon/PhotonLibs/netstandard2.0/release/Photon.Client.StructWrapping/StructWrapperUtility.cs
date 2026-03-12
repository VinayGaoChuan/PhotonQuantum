using System;
using System.Collections.Generic;

namespace Photon.Client.StructWrapping;

public static class StructWrapperUtility
{
	/// <summary>
	/// Replacement for object.GetType() that first checks to see if object is a WrappedStruct.
	/// If so returns the StructWrapper T type, otherwise just returns object.GetType().
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public static Type GetWrappedType(this object obj)
	{
		if (!(obj is StructWrapper wrapper))
		{
			return obj.GetType();
		}
		return wrapper.ttype;
	}

	/// <summary>
	/// Wrap a struct in a pooled StructWrapper.
	/// </summary>
	public static StructWrapper<T> Wrap<T>(this T value, bool persistant)
	{
		StructWrapper<T> wrapper = StructWrapper<T>.staticPool.Acquire(value);
		if (persistant)
		{
			wrapper.DisconnectFromPool();
		}
		return wrapper;
	}

	/// <summary>
	/// Wrap a struct in a pooled StructWrapper. Pulls wrapper from the static pool. Wrapper is returned to pool when Unwrapped.
	/// Slighty faster version of Wrap() that is hard wired to pull from the static pool. Use the persistant bool argument to make a permanent unpooled wrapper.
	/// </summary>
	public static StructWrapper<T> Wrap<T>(this T value)
	{
		return StructWrapper<T>.staticPool.Acquire(value);
	}

	public static StructWrapper<byte> Wrap(this byte value)
	{
		return StructWrapperPools.mappedByteWrappers[value];
	}

	public static StructWrapper<bool> Wrap(this bool value)
	{
		return StructWrapperPools.mappedBoolWrappers[value ? 1u : 0u];
	}

	/// <summary>
	/// Tests if object is either a cast T, or a wrapped T
	/// </summary>
	public static bool IsType<T>(this object obj)
	{
		if (obj is T)
		{
			return true;
		}
		if (obj is StructWrapper<T>)
		{
			return true;
		}
		return false;
	}

	/// <summary>
	/// Remove all wrappers in hashtable from pooling, so they can remain cached and used later.
	/// </summary>
	/// <param name="table"></param>
	public static T DisconnectPooling<T>(this T table) where T : IEnumerable<object>
	{
		foreach (object item in table)
		{
			if (item is StructWrapper wrapper)
			{
				wrapper.DisconnectFromPool();
			}
		}
		return table;
	}

	public static List<object> ReleaseAllWrappers(this List<object> collection)
	{
		foreach (object item in collection)
		{
			if (item is StructWrapper wrapper)
			{
				wrapper.Dispose();
			}
		}
		return collection;
	}

	public static object[] ReleaseAllWrappers(this object[] collection)
	{
		for (int i = 0; i < collection.Length; i++)
		{
			if (collection[i] is StructWrapper wrapper)
			{
				wrapper.Dispose();
			}
		}
		return collection;
	}

	public static PhotonHashtable ReleaseAllWrappers(this PhotonHashtable table)
	{
		foreach (object value in table.Values)
		{
			if (value is StructWrapper wrapper)
			{
				wrapper.Dispose();
			}
		}
		return table;
	}

	/// <summary>
	/// Unwraps any WrapperStructs, boxes their value, releases hashtable entry with the boxed value. Releases the wrappers.
	/// </summary>
	public static void BoxAll(this PhotonHashtable table, bool recursive = false)
	{
		foreach (object obj in table.Values)
		{
			if (recursive && obj is PhotonHashtable nested)
			{
				nested.BoxAll();
			}
			if (obj is StructWrapper wrapper)
			{
				wrapper.Box();
			}
		}
	}

	/// <summary>
	/// If object is a StructWrapper, the value will be extracted. If not, the object will be cast to T.
	/// Wrapper is returned to the wrapper pool if applicable, so it is not considered safe to Unwrap multiple times, as the wrapper may be recycled.
	/// </summary>
	public static T Unwrap<T>(this object obj)
	{
		if (!(obj is StructWrapper<T> wrapper))
		{
			return (T)obj;
		}
		_ = wrapper.value;
		if ((wrapper.pooling & Pooling.ReleaseOnUnwrap) == Pooling.ReleaseOnUnwrap)
		{
			wrapper.Dispose();
		}
		return wrapper.value;
	}

	/// <summary>
	/// If object is a StructWrapper, the value will be extracted. If not, the object will be cast to T.
	/// Wrapper is will not be returned to its pool until it is Unwrapped, or the pool is cleared.
	/// </summary>
	public static T Get<T>(this object obj)
	{
		if (!(obj is StructWrapper<T> wrapper))
		{
			return (T)obj;
		}
		return wrapper.value;
	}

	/// <summary>
	/// If object is a StructWrapper, the value will be extracted. If not, the object will be cast to T.
	/// Wrapper is returned to the wrapper pool if applicable, so it is not considered safe to Unwrap multiple times, as the wrapper may be recycled.
	/// </summary>
	public static T Unwrap<T>(this PhotonHashtable table, object key)
	{
		return table[key].Unwrap<T>();
	}

	/// <summary>
	/// If object is a StructWrapper, the value will be extracted. If not, the object will be cast to T.
	/// Wrapper is returned to the wrapper pool if applicable, so it is not considered safe to Unwrap multiple times, as the wrapper may be recycled.
	/// </summary>
	public static bool TryUnwrapValue<T>(this PhotonHashtable table, byte key, out T value) where T : new()
	{
		if (!table.TryGetValue(key, out var obj))
		{
			value = default(T);
			return false;
		}
		value = obj.Unwrap<T>();
		return true;
	}

	/// <summary>
	/// If object is a StructWrapper, the value will be extracted. If not, the object will be cast to T.
	/// </summary>
	public static bool TryGetValue<T>(this PhotonHashtable table, byte key, out T value) where T : new()
	{
		if (!table.TryGetValue(key, out var obj))
		{
			value = default(T);
			return false;
		}
		value = obj.Get<T>();
		return true;
	}

	/// <summary>
	/// If object is a StructWrapper, the value will be extracted. If not, the object will be cast to T.
	/// </summary>
	public static bool TryGetValue<T>(this PhotonHashtable table, object key, out T value) where T : new()
	{
		if (!table.TryGetValue(key, out var obj))
		{
			value = default(T);
			return false;
		}
		value = obj.Get<T>();
		return true;
	}

	/// <summary>
	/// If object is a StructWrapper, the value will be extracted. If not, the object will be cast to T.
	/// Wrapper is returned to the wrapper pool if applicable, so it is not considered safe to Unwrap multiple times, as the wrapper may be recycled.
	/// </summary>
	public static bool TryUnwrapValue<T>(this PhotonHashtable table, object key, out T value) where T : new()
	{
		if (!table.TryGetValue(key, out var obj))
		{
			value = default(T);
			return false;
		}
		value = obj.Unwrap<T>();
		return true;
	}

	/// <summary>
	/// If object is a StructWrapper, the value will be extracted. If not, the object will be cast to T.
	/// Wrapper is returned to the wrapper pool if applicable, so it is not considered safe to Unwrap multiple times, as the wrapper may be recycled.
	/// </summary>
	public static T Unwrap<T>(this PhotonHashtable table, byte key)
	{
		return table[key].Unwrap<T>();
	}

	/// <summary>
	/// If object is a StructWrapper, the value will be extracted. If not, the object will be cast to T.
	/// Wrapper is will not be returned to its pool until it is Unwrapped, or the pool is cleared.
	/// </summary>
	public static T Get<T>(this PhotonHashtable table, byte key)
	{
		return table[key].Get<T>();
	}
}
