using System;
using System.Collections;
using System.Collections.Generic;
using Quantum;

namespace Photon.Deterministic
{
	internal class RingBuffer<T> : IEnumerable<T>, IEnumerable
	{
		public struct Iterator
		{
			private int _count;

			private int _index;

			private readonly int _version;

			private readonly RingBuffer<T> _buffer;

			public Iterator(RingBuffer<T> buffer)
			{
				_buffer = buffer;
				_version = buffer._version;
				_index = 0;
				_count = buffer._count;
			}

			public bool Next(out T item)
			{
				if (_version != _buffer._version)
				{
					throw new InvalidOperationException("RingBuffer has been modified");
				}
				if (_index < _count)
				{
					item = _buffer[_index];
					_index++;
					return true;
				}
				item = default(T);
				return false;
			}
		}

		private int _head;

		private int _tail;

		private int _count;

		private int _version;

		private T[] _array;

		private readonly bool _overwrite;

		public int Count => _count;

		public int Capacity => _array.Length;

		public bool IsFull => _count == _array.Length;

		public bool IsEmpty => _count == 0;

		public T this[int index]
		{
			get
			{
				if (index < 0 || index >= _count)
				{
					throw new IndexOutOfRangeException();
				}
				return _array[(_tail + index) % _array.Length];
			}
			set
			{
				if (index < 0 || index >= _count)
				{
					throw new IndexOutOfRangeException();
				}
				_array[(_tail + index) % _array.Length] = value;
			}
		}

		public Iterator GetIterator()
		{
			return new Iterator(this);
		}

		public RingBuffer(int size, bool overwrite)
		{
			_array = new T[size];
			_overwrite = overwrite;
		}

		public void Push(T item)
		{
			if (IsFull)
			{
				if (!_overwrite)
				{
					throw new InvalidOperationException();
				}
				Pop();
			}
			_version++;
			_array[_head] = item;
			_head = (_head + 1) % _array.Length;
			_count++;
		}

		public T Pop()
		{
			if (IsEmpty)
			{
				throw new InvalidOperationException();
			}
			_version++;
			T result = _array[_tail];
			_array[_tail] = default(T);
			_tail = (_tail + 1) % _array.Length;
			_count--;
			return result;
		}

		public void Clear()
		{
			_head = 0;
			_tail = 0;
			_count = 0;
			_version++;
			Array.Clear(_array, 0, _array.Length);
		}

		public void Grow(int growMultiplier = 2)
		{
			Assert.Always(growMultiplier > 0, "growMultiplier must be greater than 0");
			T[] array = new T[_array.Length * growMultiplier];
			if (_head > _tail)
			{
				Array.Copy(_array, array, _array.Length);
			}
			else
			{
				Array.Copy(_array, _tail, array, 0, _array.Length - _tail);
				Array.Copy(_array, 0, array, _array.Length - _tail, _tail);
				_head += _array.Length - _tail;
				_tail = 0;
			}
			_array = array;
			_version++;
		}

		public IEnumerator<T> GetEnumerator()
		{
			int i = 0;
			while (i < _count)
			{
				yield return this[i];
				int num = i + 1;
				i = num;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}

