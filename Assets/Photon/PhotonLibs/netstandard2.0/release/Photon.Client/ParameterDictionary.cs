using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Photon.Client.StructWrapping;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Photon.Client;

[DebuggerDisplay("Parameter count: {Count}")]
public class ParameterDictionary : IEnumerable<KeyValuePair<byte, object>>, IEnumerable
{
	public readonly NonAllocDictionary<byte, object> paramDict;

	public readonly StructWrapperPools wrapperPools = new StructWrapperPools();

	public object this[byte key]
	{
		get
		{
			object obj = paramDict[key];
			if (!(obj is StructWrapper<object> objwrapper))
			{
				return obj;
			}
			return objwrapper;
		}
		set
		{
			paramDict[key] = value;
		}
	}

	public int Count => paramDict.Count;

	public ParameterDictionary()
	{
		paramDict = new NonAllocDictionary<byte, object>();
	}

	public ParameterDictionary(int capacity)
	{
		paramDict = new NonAllocDictionary<byte, object>((uint)capacity);
	}

	public static implicit operator NonAllocDictionary<byte, object>(ParameterDictionary value)
	{
		return value.paramDict;
	}

	/// <inheritdoc />
	IEnumerator<KeyValuePair<byte, object>> IEnumerable<KeyValuePair<byte, object>>.GetEnumerator()
	{
		return ((IEnumerable<KeyValuePair<byte, object>>)paramDict).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<KeyValuePair<byte, object>>)paramDict).GetEnumerator();
	}

	/// <inheritdoc />
	public NonAllocDictionary<byte, object>.PairIterator GetEnumerator()
	{
		return paramDict.GetEnumerator();
	}

	public void Clear()
	{
		wrapperPools.Clear();
		paramDict.Clear();
	}

	public void Add(byte code, string value)
	{
		if (paramDict.ContainsKey(code))
		{
			Debug.LogWarning((object)(code + " already exists as key in ParameterDictionary"));
		}
		paramDict[code] = value;
	}

	public void Add(byte code, PhotonHashtable value)
	{
		if (paramDict.ContainsKey(code))
		{
			Debug.LogWarning((object)(code + " already exists as key in ParameterDictionary"));
		}
		paramDict[code] = value;
	}

	public void Add(byte code, byte value)
	{
		if (paramDict.ContainsKey(code))
		{
			Debug.LogError((object)(code + " already exists as key in ParameterDictionary"));
		}
		StructWrapper<byte> wrapper = StructWrapperPools.mappedByteWrappers[value];
		paramDict[code] = wrapper;
	}

	public void Add(byte code, bool value)
	{
		if (paramDict.ContainsKey(code))
		{
			Debug.LogError((object)(code + " already exists as key in ParameterDictionary"));
		}
		StructWrapper<bool> wrapper = StructWrapperPools.mappedBoolWrappers[value ? 1u : 0u];
		paramDict[code] = wrapper;
	}

	public void Add(byte code, short value)
	{
		if (paramDict.ContainsKey(code))
		{
			Debug.LogWarning((object)(code + " already exists as key in ParameterDictionary"));
		}
		paramDict[code] = value;
	}

	public void Add(byte code, int value)
	{
		if (paramDict.ContainsKey(code))
		{
			Debug.LogWarning((object)(code + " already exists as key in ParameterDictionary"));
		}
		paramDict[code] = value;
	}

	public void Add(byte code, long value)
	{
		if (paramDict.ContainsKey(code))
		{
			Debug.LogWarning((object)(code + " already exists as key in ParameterDictionary"));
		}
		paramDict[code] = value;
	}

	public void Add(byte code, object value)
	{
		if (paramDict.ContainsKey(code))
		{
			Debug.LogWarning((object)(code + " already exists as key in ParameterDictionary"));
		}
		paramDict[code] = value;
	}

	public T Unwrap<T>(byte key)
	{
		return paramDict[key].Unwrap<T>();
	}

	public T Get<T>(byte key)
	{
		return paramDict[key].Get<T>();
	}

	public bool ContainsKey(byte key)
	{
		return paramDict.ContainsKey(key);
	}

	/// <summary>
	/// Will get the object using the key. If the key is invalid, will return null.
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public object TryGetObject(byte key)
	{
		if (paramDict.TryGetValue(key, out var obj))
		{
			return obj;
		}
		return null;
	}

	public bool TryGetValue(byte key, out object value)
	{
		return paramDict.TryGetValue(key, out value);
	}

	public bool TryGetValue<T>(byte key, out T value) where T : struct
	{
		object obj;
		bool success = paramDict.TryGetValue(key, out obj);
		if (!success)
		{
			value = default(T);
			return false;
		}
		if (obj is StructWrapper<T> wrapper)
		{
			value = wrapper.value;
		}
		else if (obj is StructWrapper<object> objwrapper)
		{
			value = (T)objwrapper.value;
		}
		else
		{
			value = (T)obj;
		}
		return success;
	}

	/// <summary>Dictionary content as string.</summary>
	/// <param name="includeTypes">If true, type-info is also included.</param>
	/// <returns>Full content of dictionary as string.</returns>
	public string ToStringFull(bool includeTypes = true)
	{
		if (includeTypes)
		{
			return $"(ParameterDictionary){SupportClass.DictionaryToString(paramDict, includeTypes)}";
		}
		return SupportClass.DictionaryToString(paramDict, includeTypes);
	}
}
