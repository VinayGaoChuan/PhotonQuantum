using System.Collections.Generic;

namespace Photon.Client;

/// <summary>
/// This is a substitute for the Hashtable class which avoids most memory allocations during usage. Based on Dictionary&lt;object,object&gt;.
/// </summary>
/// <remarks>
/// Please be aware that this class might act differently than the System.Hashtable equivalent.
/// As far as Photon is concerned, the substitution is sufficiently precise.
/// </remarks>
public class PhotonHashtable : Dictionary<object, object>
{
	private static readonly object[] boxedByte;

	public new object this[object key]
	{
		get
		{
			object ret = null;
			TryGetValue(key, out ret);
			return ret;
		}
		set
		{
			base[key] = value;
		}
	}

	public object this[byte key]
	{
		get
		{
			object ret = null;
			TryGetValue(boxedByte[key], out ret);
			return ret;
		}
		set
		{
			base[boxedByte[key]] = value;
		}
	}

	public object this[int key]
	{
		get
		{
			object ret = null;
			TryGetValue(key, out ret);
			return ret;
		}
		set
		{
			base[key] = value;
		}
	}

	public static object GetBoxedByte(byte value)
	{
		return boxedByte[value];
	}

	static PhotonHashtable()
	{
		int cnt = 256;
		boxedByte = new object[cnt];
		for (int i = 0; i < cnt; i++)
		{
			boxedByte[i] = (byte)i;
		}
	}

	public PhotonHashtable()
		: base(7)
	{
	}

	public PhotonHashtable(int x)
		: base(x)
	{
	}

	public void Add(byte k, object v)
	{
		Add(boxedByte[k], v);
	}

	public void Add(int k, object v)
	{
		Add((object)k, v);
	}

	public void Remove(byte k)
	{
		Remove(boxedByte[k]);
	}

	public void Remove(int k)
	{
		Remove((object)k);
	}

	/// <summary>
	/// Translates the byte key into the pre-boxed byte before doing the lookup.
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public bool ContainsKey(byte key)
	{
		return ContainsKey(boxedByte[key]);
	}

	public new DictionaryEntryEnumerator GetEnumerator()
	{
		return new DictionaryEntryEnumerator(base.GetEnumerator());
	}

	public override string ToString()
	{
		List<string> temp = new List<string>();
		foreach (object key in base.Keys)
		{
			if (key == null || this[key] == null)
			{
				temp.Add(key?.ToString() + "=" + this[key]);
				continue;
			}
			temp.Add("(" + key.GetType()?.ToString() + ")" + key?.ToString() + "=(" + this[key].GetType()?.ToString() + ")" + this[key]);
		}
		return string.Join(", ", temp.ToArray());
	}

	/// <summary>
	/// Creates a shallow copy of the PhotonHashtable.
	/// </summary>
	/// <remarks>
	/// A shallow copy of a collection copies only the elements of the collection, whether they are
	/// reference types or value types, but it does not copy the objects that the references refer
	/// to. The references in the new collection point to the same objects that the references in
	/// the original collection point to.
	/// </remarks>
	/// <returns>Shallow copy of the PhotonHashtable.</returns>
	public object Clone()
	{
		return new Dictionary<object, object>(this);
	}
}
