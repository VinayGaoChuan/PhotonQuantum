using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Photon.Deterministic
{
	public class PersistentMap<K, V> : IEnumerable<KeyValuePair<K, V>>, IEnumerable where K : IEquatable<K>, IComparable<K>
	{
		public struct Enumerator : IEnumerator<KeyValuePair<K, V>>, IEnumerator, IDisposable
		{
			private readonly Stack<PersistentMap<K, V>> _ancestors;

			public KeyValuePair<K, V> Current { get; private set; }

			object IEnumerator.Current => Current;

			public Enumerator(PersistentMap<K, V> node)
			{
				Current = default(KeyValuePair<K, V>);
				if (node.Count == 0)
				{
					_ancestors = null;
					return;
				}
				_ancestors = new Stack<PersistentMap<K, V>>(node._height);
				_ancestors.Push(node);
				while (node.HasLeft)
				{
					node = node.Left;
					_ancestors.Push(node);
				}
			}

			public bool MoveNext()
			{
				if (_ancestors == null || _ancestors.Count == 0)
				{
					return false;
				}
				PersistentMap<K, V> persistentMap = _ancestors.Pop();
				Current = new KeyValuePair<K, V>(persistentMap.Key, persistentMap.Value);
				if (persistentMap.HasRight)
				{
					persistentMap = persistentMap.Right;
					_ancestors.Push(persistentMap);
					while (persistentMap.HasLeft)
					{
						persistentMap = persistentMap.Left;
						_ancestors.Push(persistentMap);
					}
				}
				return true;
			}

			public void Reset()
			{
				throw new NotImplementedException();
			}

			public void Dispose()
			{
			}
		}

		public struct ValueEnumerator : IEnumerator<V>, IEnumerator, IDisposable
		{
			private Enumerator _enumerator;

			public V Current { get; private set; }

			object IEnumerator.Current => Current;

			public ValueEnumerator(PersistentMap<K, V> map)
			{
				Current = default(V);
				_enumerator = new Enumerator(map);
			}

			public bool MoveNext()
			{
				if (_enumerator.MoveNext())
				{
					Current = _enumerator.Current.Value;
					return true;
				}
				return false;
			}

			public void Reset()
			{
				throw new NotImplementedException();
			}

			public void Dispose()
			{
			}
		}

		public readonly struct ValueEnumerable : IEnumerable<V>, IEnumerable
		{
			[CompilerGenerated]
			private readonly PersistentMap<K, V> _003Cmap_003EP;

			public ValueEnumerable(PersistentMap<K, V> map)
			{
				_003Cmap_003EP = map;
			}

			IEnumerator<V> IEnumerable<V>.GetEnumerator()
			{
				return GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public ValueEnumerator GetEnumerator()
			{
				return new ValueEnumerator(_003Cmap_003EP);
			}
		}

		public delegate void ActionVisitor(K key, V value);

		public delegate void ActionVisitor<T>(K key, V value, T context);

		public delegate bool FuncVisitor(K key, V value);

		public delegate bool FuncVisitor<T>(K key, V value, T context);

		private static readonly PersistentMap<K, V> _empty = new PersistentMap<K, V>();

		private readonly K _key;

		private readonly V _value;

		private readonly int _count;

		private readonly int _height;

		private readonly PersistentMap<K, V> _left;

		private readonly PersistentMap<K, V> _right;

		private K Key
		{
			get
			{
				if (_count == 0)
				{
					throw new InvalidOperationException("Map is empty");
				}
				return _key;
			}
		}

		private V Value
		{
			get
			{
				if (_count == 0)
				{
					throw new InvalidOperationException("Map is empty");
				}
				return _value;
			}
		}

		private PersistentMap<K, V> Left
		{
			get
			{
				if (_count == 0)
				{
					throw new InvalidOperationException("Map is empty");
				}
				return _left;
			}
		}

		private PersistentMap<K, V> Right
		{
			get
			{
				if (_count == 0)
				{
					throw new InvalidOperationException("Map is empty");
				}
				return _right;
			}
		}

		private bool IsEmpty => _count == 0;

		private bool HasLeft
		{
			get
			{
				if (_count > 0)
				{
					return !Left.IsEmpty;
				}
				return false;
			}
		}

		private bool HasRight
		{
			get
			{
				if (_count > 0)
				{
					return !Right.IsEmpty;
				}
				return false;
			}
		}

		private int Balance
		{
			get
			{
				if (_count == 0)
				{
					return 0;
				}
				return Left._height - Right._height;
			}
		}

		public int Count => _count;

		public V this[K key] => Find(key);

		public ValueEnumerable Values => new ValueEnumerable(this);

		public PersistentMap()
		{
			_key = default(K);
			_value = default(V);
			_count = 0;
			_height = 0;
			_left = null;
			_right = null;
		}

		private PersistentMap(K key, V value)
			: this(key, value, _empty, _empty)
		{
		}

		private PersistentMap(K key, V value, PersistentMap<K, V> left, PersistentMap<K, V> right)
		{
			_key = key;
			_value = value;
			_left = left;
			_right = right;
			_height = Math.Max(left._height, right._height) + 1;
			_count = left.Count + right.Count + 1;
		}

		public PersistentMap<K, V> Add(K key, V value)
		{
			if (_count == 0)
			{
				return new PersistentMap<K, V>(key, value, _empty, _empty);
			}
			int num = key.CompareTo(Key);
			if (num < 0)
			{
				return Rebalance(new PersistentMap<K, V>(Key, Value, Left.Add(key, value), Right));
			}
			if (num > 0)
			{
				return Rebalance(new PersistentMap<K, V>(Key, Value, Left, Right.Add(key, value)));
			}
			throw new InvalidOperationException("Key already exists");
		}

		public PersistentMap<K, V> Set(K key, V value, out V oldValue)
		{
			if (_count == 0)
			{
				throw new KeyNotFoundException();
			}
			int num = key.CompareTo(_key);
			if (num < 0)
			{
				return Rebalance(new PersistentMap<K, V>(Key, Value, Left.Set(key, value, out oldValue), Right));
			}
			if (num > 0)
			{
				return Rebalance(new PersistentMap<K, V>(Key, Value, Left, Right.Set(key, value, out oldValue)));
			}
			oldValue = Value;
			return new PersistentMap<K, V>(key, value, Left, Right);
		}

		public PersistentMap<K, V> AddOrSet(K key, V value)
		{
			if (_count == 0)
			{
				return new PersistentMap<K, V>(key, value, _empty, _empty);
			}
			int num = key.CompareTo(_key);
			if (num < 0)
			{
				return Rebalance(new PersistentMap<K, V>(Key, Value, Left.AddOrSet(key, value), Right));
			}
			if (num > 0)
			{
				return Rebalance(new PersistentMap<K, V>(Key, Value, Left, Right.AddOrSet(key, value)));
			}
			return new PersistentMap<K, V>(key, value, Left, Right);
		}

		public PersistentMap<K, V> TryRemove(K key, out V removed)
		{
			if (_count == 0)
			{
				removed = default(V);
				return null;
			}
			int num = key.CompareTo(Key);
			if (num < 0)
			{
				PersistentMap<K, V> persistentMap = Left.TryRemove(key, out removed);
				if ((object)persistentMap != null)
				{
					return Rebalance(new PersistentMap<K, V>(Key, Value, persistentMap, Right));
				}
				return null;
			}
			if (num > 0)
			{
				PersistentMap<K, V> persistentMap2 = Right.TryRemove(key, out removed);
				if ((object)persistentMap2 != null)
				{
					return Rebalance(new PersistentMap<K, V>(Key, Value, Left, persistentMap2));
				}
				return null;
			}
			removed = Value;
			if (Right.Count == 0)
			{
				if (Left.Count == 0)
				{
					return _empty;
				}
				return Rebalance(Left);
			}
			if (Left.Count == 0)
			{
				return Rebalance(Right);
			}
			PersistentMap<K, V> persistentMap3 = Right;
			while (persistentMap3.Left.Count > 0)
			{
				persistentMap3 = persistentMap3.Left;
			}
			V removed2;
			PersistentMap<K, V> right = Right.TryRemove(persistentMap3.Key, out removed2);
			return Rebalance(new PersistentMap<K, V>(persistentMap3.Key, persistentMap3.Value, Left, right));
		}

		public V Find(K key)
		{
			PersistentMap<K, V> persistentMap = Search(key);
			if (persistentMap.Count == 0)
			{
				throw new KeyNotFoundException();
			}
			return persistentMap.Value;
		}

		public bool HasKey(K key)
		{
			V value;
			return TryFind(key, out value);
		}

		public bool TryFind(K key, out V value)
		{
			PersistentMap<K, V> persistentMap = Search(key);
			if (persistentMap.Count == 0)
			{
				value = default(V);
				return false;
			}
			value = persistentMap.Value;
			return true;
		}

		[Obsolete("Use GetEnumerator() instead", true)]
		public IEnumerator<KeyValuePair<K, V>> Iterator()
		{
			return GetEnumerator();
		}

		IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
		{
			return GetEnumerator();
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		private PersistentMap<K, V> Search(K key)
		{
			if (_count == 0)
			{
				return this;
			}
			int num = key.CompareTo(Key);
			if (num < 0)
			{
				return Left.Search(key);
			}
			if (num > 0)
			{
				return Right.Search(key);
			}
			return this;
		}

		private static PersistentMap<K, V> RotateLeft(PersistentMap<K, V> map)
		{
			return new PersistentMap<K, V>(map.Right.Key, map.Right.Value, new PersistentMap<K, V>(map.Key, map.Value, map.Left, map.Right.Left), map.Right.Right);
		}

		private static PersistentMap<K, V> RotateRight(PersistentMap<K, V> map)
		{
			return new PersistentMap<K, V>(map.Left.Key, map.Left.Value, map.Left.Left, new PersistentMap<K, V>(map.Key, map.Value, map.Left.Right, map.Right));
		}

		private static PersistentMap<K, V> Rebalance(PersistentMap<K, V> map)
		{
			int balance = map.Balance;
			if (balance < -2)
			{
				throw new Exception();
			}
			if (balance > 2)
			{
				throw new Exception();
			}
			switch (balance)
			{
			case 2:
				if (map.Left.Balance == -1)
				{
					map = new PersistentMap<K, V>(map.Key, map.Value, RotateLeft(map.Left), map.Right);
				}
				return RotateRight(map);
			case -2:
				if (map.Right.Balance == 1)
				{
					map = new PersistentMap<K, V>(map.Key, map.Value, map.Left, RotateRight(map.Right));
				}
				return RotateLeft(map);
			default:
				return map;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public (K, V)[] ToArray()
		{
			if (_count == 0)
			{
				return Array.Empty<(K, V)>();
			}
			(K, V)[] array = new(K, V)[_count];
			int num = 0;
			using Enumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<K, V> current = enumerator.Current;
				array[num++] = (current.Key, current.Value);
			}
			return array;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		[Obsolete("Do not use", true)]
		public static bool operator ==(PersistentMap<K, V> a, PersistentMap<K, V> b)
		{
			throw new NotImplementedException();
		}

		[Obsolete("Do not use", true)]
		public static bool operator !=(PersistentMap<K, V> a, PersistentMap<K, V> b)
		{
			throw new NotImplementedException();
		}

		[Obsolete("Do not use", true)]
		public bool Equals(PersistentMap<K, V> other)
		{
			throw new NotImplementedException();
		}

		public override int GetHashCode()
		{
			return GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Visit(ActionVisitor visitor)
		{
			if (_count != 0)
			{
				VisitInner(visitor);
			}
		}

		private void VisitInner(ActionVisitor visitor)
		{
			if (_left._count > 0)
			{
				_left.VisitInner(visitor);
			}
			visitor(_key, _value);
			if (_right._count > 0)
			{
				_right.VisitInner(visitor);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Visit<T>(ActionVisitor<T> visitor, T context)
		{
			if (_count != 0)
			{
				VisitInner(visitor, context);
			}
		}

		private void VisitInner<T>(ActionVisitor<T> visitor, T context)
		{
			if (_left._count > 0)
			{
				_left.VisitInner(visitor, context);
			}
			visitor(_key, _value, context);
			if (_right._count > 0)
			{
				_right.VisitInner(visitor, context);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Visit(FuncVisitor visitor)
		{
			if (_count == 0)
			{
				return true;
			}
			return VisitInner(visitor);
		}

		private bool VisitInner(FuncVisitor visitor)
		{
			if (_left._count > 0 && !_left.VisitInner(visitor))
			{
				return false;
			}
			if (!visitor(_key, _value))
			{
				return false;
			}
			if (_right._count > 0 && !_right.VisitInner(visitor))
			{
				return false;
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Visit<T>(FuncVisitor<T> visitor, T context)
		{
			if (_count == 0)
			{
				return true;
			}
			return VisitInner(visitor, context);
		}

		private bool VisitInner<T>(FuncVisitor<T> visitor, T context)
		{
			if (_left._count > 0 && !_left.VisitInner(visitor, context))
			{
				return false;
			}
			if (!visitor(_key, _value, context))
			{
				return false;
			}
			if (_right._count > 0 && !_right.VisitInner(visitor, context))
			{
				return false;
			}
			return true;
		}
	}
}

