using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Photon.Client;

/// <summary>
/// Replacement for Dictionary&lt;K,V&gt; which does not allocate memory during usage.
/// </summary>
/// <typeparam name="K">Key type.</typeparam>
/// <typeparam name="V">Value type.</typeparam>
[DebuggerDisplay("{_nodes.Length} nodes, used {Count}")]
public class NonAllocDictionary<K, V> : IDictionary<K, V>, ICollection<KeyValuePair<K, V>>, IEnumerable<KeyValuePair<K, V>>, IEnumerable where K : IEquatable<K>
{
	public struct KeyIterator : IEnumerator<K>, IEnumerator, IDisposable
	{
		private int _index;

		private NonAllocDictionary<K, V> _dict;

		object IEnumerator.Current
		{
			get
			{
				if (_index == 0)
				{
					throw new InvalidOperationException();
				}
				return _dict._nodes[_index].Key;
			}
		}

		public K Current
		{
			get
			{
				if (_index == 0)
				{
					return default(K);
				}
				return _dict._nodes[_index].Key;
			}
		}

		public KeyIterator(NonAllocDictionary<K, V> dictionary)
		{
			_index = 0;
			_dict = dictionary;
		}

		public KeyIterator GetEnumerator()
		{
			return this;
		}

		public void Reset()
		{
			_index = 0;
		}

		public bool MoveNext()
		{
			while (++_index < _dict._usedCount)
			{
				if (_dict._nodes[_index].Used)
				{
					return true;
				}
			}
			_index = 0;
			return false;
		}

		public void Dispose()
		{
		}
	}

	public struct ValueIterator : IEnumerator<V>, IEnumerator, IDisposable
	{
		private int _index;

		private NonAllocDictionary<K, V> _dict;

		public V Current
		{
			get
			{
				if (_index == 0)
				{
					return default(V);
				}
				return _dict._nodes[_index].Val;
			}
		}

		object IEnumerator.Current
		{
			get
			{
				if (_index == 0)
				{
					throw new InvalidOperationException();
				}
				return _dict._nodes[_index].Val;
			}
		}

		public ValueIterator(NonAllocDictionary<K, V> dictionary)
		{
			_index = 0;
			_dict = dictionary;
		}

		public ValueIterator GetEnumerator()
		{
			return this;
		}

		public void Reset()
		{
			_index = 0;
		}

		public bool MoveNext()
		{
			while (++_index < _dict._usedCount)
			{
				if (_dict._nodes[_index].Used)
				{
					return true;
				}
			}
			_index = 0;
			return false;
		}

		public void Dispose()
		{
		}
	}

	public struct PairIterator : IEnumerator<KeyValuePair<K, V>>, IEnumerator, IDisposable
	{
		private int _index;

		private NonAllocDictionary<K, V> _dict;

		object IEnumerator.Current
		{
			get
			{
				if (_index == 0)
				{
					throw new InvalidOperationException();
				}
				return Current;
			}
		}

		public KeyValuePair<K, V> Current
		{
			get
			{
				if (_index == 0)
				{
					return default(KeyValuePair<K, V>);
				}
				return new KeyValuePair<K, V>(_dict._nodes[_index].Key, _dict._nodes[_index].Val);
			}
		}

		public PairIterator(NonAllocDictionary<K, V> dictionary)
		{
			_index = 0;
			_dict = dictionary;
		}

		public void Reset()
		{
			_index = 0;
		}

		public bool MoveNext()
		{
			while (++_index < _dict._usedCount)
			{
				if (_dict._nodes[_index].Used)
				{
					return true;
				}
			}
			_index = 0;
			return false;
		}

		public void Dispose()
		{
		}
	}

	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	private struct Node
	{
		public bool Used;

		public K Key;

		public V Val;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Next;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public uint Hash;

		private string DebuggerDisplay
		{
			get
			{
				if (!Used)
				{
					return "not used";
				}
				return $"{Key}: {Val}";
			}
		}
	}

	private static uint[] _primeTableUInt = new uint[30]
	{
		3u, 7u, 17u, 29u, 53u, 97u, 193u, 389u, 769u, 1543u,
		3079u, 6151u, 12289u, 24593u, 49157u, 98317u, 196613u, 393241u, 786433u, 1572869u,
		3145739u, 6291469u, 12582917u, 25165843u, 50331653u, 100663319u, 201326611u, 402653189u, 805306457u, 1610612741u
	};

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private int _freeHead;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private int _freeCount;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private int _usedCount;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private uint _capacity;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private int[] _buckets;

	private Node[] _nodes;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private bool isReadOnly;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private ICollection<K> keys;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private ICollection<V> values;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public KeyIterator Keys => new KeyIterator(this);

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	ICollection<V> IDictionary<K, V>.Values => values;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	ICollection<K> IDictionary<K, V>.Keys => keys;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public ValueIterator Values => new ValueIterator(this);

	public int Count => _usedCount - _freeCount - 1;

	public bool IsReadOnly => isReadOnly;

	public uint Capacity => _capacity;

	public V this[K key]
	{
		get
		{
			int node = FindNode(key);
			if (node != 0)
			{
				return _nodes[node].Val;
			}
			K val = key;
			throw new InvalidOperationException("Key does not exist: " + val);
		}
		set
		{
			int node = FindNode(key);
			if (node == 0)
			{
				Insert(key, value);
				return;
			}
			Assert(_nodes[node].Key.Equals(key));
			_nodes[node].Val = value;
		}
	}

	public NonAllocDictionary(uint capacity = 29u)
	{
		_capacity = (IsPrimeFromList(capacity) ? capacity : GetNextPrime(capacity));
		_usedCount = 1;
		_buckets = new int[_capacity];
		_nodes = new Node[_capacity];
	}

	public bool ContainsKey(K key)
	{
		return FindNode(key) != 0;
	}

	public bool Contains(KeyValuePair<K, V> item)
	{
		int i = FindNode(item.Key);
		if (i >= 0 && EqualityComparer<V>.Default.Equals(_nodes[i].Val, item.Value))
		{
			return true;
		}
		return false;
	}

	public bool TryGetValue(K key, out V val)
	{
		int node = FindNode(key);
		if (node != 0)
		{
			val = _nodes[node].Val;
			return true;
		}
		val = default(V);
		return false;
	}

	public void Set(K key, V val)
	{
		int node = FindNode(key);
		if (node == 0)
		{
			Insert(key, val);
			return;
		}
		Assert(_nodes[node].Key.Equals(key));
		_nodes[node].Val = val;
	}

	public void Add(K key, V val)
	{
		if (FindNode(key) == 0)
		{
			Insert(key, val);
			return;
		}
		K val2 = key;
		throw new InvalidOperationException("Duplicate key " + val2);
	}

	public void Add(KeyValuePair<K, V> item)
	{
		if (FindNode(item.Key) == 0)
		{
			Insert(item.Key, item.Value);
			return;
		}
		throw new InvalidOperationException("Duplicate key " + item.Key);
	}

	public bool Remove(K key)
	{
		uint hash = (uint)key.GetHashCode();
		int node = _buckets[hash % _capacity];
		int nodePrev = 0;
		while (node != 0)
		{
			if (_nodes[node].Hash == hash)
			{
				ref K key2 = ref _nodes[node].Key;
				K other = key;
				if (key2.Equals(other))
				{
					if (nodePrev == 0)
					{
						_buckets[hash % _capacity] = _nodes[node].Next;
					}
					else
					{
						_nodes[nodePrev].Next = _nodes[node].Next;
					}
					_nodes[node].Used = false;
					_nodes[node].Next = _freeHead;
					_nodes[node].Val = default(V);
					_freeHead = node;
					_freeCount++;
					return true;
				}
			}
			nodePrev = node;
			node = _nodes[node].Next;
		}
		return false;
	}

	public bool Remove(KeyValuePair<K, V> item)
	{
		if (Contains(item))
		{
			return Remove(item.Key);
		}
		return false;
	}

	IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
	{
		return new PairIterator(this);
	}

	public PairIterator GetEnumerator()
	{
		return new PairIterator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new PairIterator(this);
	}

	private int FindNode(K key)
	{
		uint hash = (uint)key.GetHashCode();
		for (int node = _buckets[hash % _capacity]; node != 0; node = _nodes[node].Next)
		{
			if (_nodes[node].Hash == hash)
			{
				ref K key2 = ref _nodes[node].Key;
				K other = key;
				if (key2.Equals(other))
				{
					return node;
				}
			}
		}
		return 0;
	}

	private void Insert(K key, V val)
	{
		int node = 0;
		if (_freeCount > 0)
		{
			node = _freeHead;
			_freeHead = _nodes[node].Next;
			_freeCount--;
		}
		else
		{
			if (_usedCount == _capacity)
			{
				Expand();
			}
			node = _usedCount++;
		}
		uint hash = (uint)key.GetHashCode();
		uint bucket = hash % _capacity;
		_nodes[node].Used = true;
		_nodes[node].Hash = hash;
		_nodes[node].Next = _buckets[bucket];
		_nodes[node].Key = key;
		_nodes[node].Val = val;
		_buckets[bucket] = node;
	}

	private void Expand()
	{
		Assert(_buckets.Length == _usedCount);
		uint newCapacity = GetNextPrime(_capacity);
		Assert(newCapacity > _capacity);
		int[] newBuckets = new int[newCapacity];
		Node[] newNodes = new Node[newCapacity];
		Array.Copy(_nodes, 0, newNodes, 0, _nodes.Length);
		for (int i = 1; i < _nodes.Length; i++)
		{
			Assert(newNodes[i].Used);
			uint bucket = newNodes[i].Hash % newCapacity;
			newNodes[i].Next = newBuckets[bucket];
			newBuckets[bucket] = i;
		}
		_nodes = newNodes;
		_buckets = newBuckets;
		_capacity = newCapacity;
	}

	public void Clear()
	{
		if (_usedCount > 1)
		{
			Array.Clear(_nodes, 0, _nodes.Length);
			Array.Clear(_buckets, 0, _buckets.Length);
			_freeHead = 0;
			_freeCount = 0;
			_usedCount = 1;
		}
	}

	void ICollection<KeyValuePair<K, V>>.CopyTo(KeyValuePair<K, V>[] array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (index < 0 || index > array.Length)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (array.Length - index < Count)
		{
			throw new ArgumentException("Array plus offset are too small to fit all items in.");
		}
		for (int i = 1; i < _nodes.Length; i++)
		{
			if (_nodes[i].Used)
			{
				array[index++] = new KeyValuePair<K, V>(_nodes[i].Key, _nodes[i].Val);
			}
		}
	}

	private static bool IsPrimeFromList(uint value)
	{
		for (int i = 0; i < _primeTableUInt.Length; i++)
		{
			if (_primeTableUInt[i] == value)
			{
				return true;
			}
		}
		return false;
	}

	private static uint GetNextPrime(uint value)
	{
		for (int i = 0; i < _primeTableUInt.Length; i++)
		{
			if (_primeTableUInt[i] > value)
			{
				return _primeTableUInt[i];
			}
		}
		throw new InvalidOperationException("NonAllocDictionary can't get larger than" + _primeTableUInt[_primeTableUInt.Length - 1]);
	}

	private static void Assert(bool condition)
	{
		if (!condition)
		{
			throw new InvalidOperationException("Assert Failed");
		}
	}
}
