using System;
using System.Collections;
using System.Collections.Generic;

namespace Photon.Deterministic.Simulator.Timing
{
	/// <summary>
	/// <para>
	/// A circular buffer.
	/// </para>
	/// <para>
	/// Normally, you push to the back and pop from the front.
	/// When it's full, <c>PushBack</c> will remove from the front and <c>PushFront</c> will remove from the back.
	/// </para>
	/// </summary>
	internal class RingBuffer<T> : IEnumerable<T>, IEnumerable where T : struct
	{
		private readonly T[] _buffer;

		private int _front;

		private int _count;

		/// <summary>
		/// The number of items in the buffer.
		/// </summary>
		public int Count => _count;

		/// <summary>
		/// The maximum number of items that can be in the buffer.
		/// </summary>
		public int Capacity => _buffer.Length;

		/// <summary>
		/// <see langword="true" /> if the buffer contains no items.
		/// </summary>
		public bool IsEmpty => Count == 0;

		/// <summary>
		/// <see langword="true" /> if the buffer contains the maximum number of items.
		/// </summary>
		public bool IsFull => Count == Capacity;

		/// <summary>
		/// <para>Indexed access to items in the buffer.</para>
		/// <para>Indexes follow insertion order, i.e. <c>this[0]</c> returns the front item and <c>this[Count - 1]</c> returns the rear item.</para>
		/// </summary>
		/// <exception cref="T:System.IndexOutOfRangeException"></exception>
		public T this[int index]
		{
			get
			{
				if (IsEmpty)
				{
					throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer is empty.");
				}
				if (index >= _count)
				{
					throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer has {_count} items.");
				}
				int num = InternalIndex(index);
				return _buffer[num];
			}
			set
			{
				if (IsEmpty)
				{
					throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer is empty.");
				}
				if (index >= _count)
				{
					throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer has {_count} items.");
				}
				int num = InternalIndex(index);
				_buffer[num] = value;
			}
		}

		/// <summary>
		/// Returns a new <see cref="T:Photon.Deterministic.Simulator.Timing.RingBuffer`1" /> instance.
		/// </summary>
		public RingBuffer(int capacity)
			: this(capacity, new T[0])
		{
		}

		/// <summary>
		/// Returns a new <see cref="T:Photon.Deterministic.Simulator.Timing.RingBuffer`1" /> instance.
		/// </summary>
		public RingBuffer(int capacity, T[] items)
		{
			if (capacity < 1)
			{
				throw new ArgumentException("Buffer cannot have negative or zero capacity.", "capacity");
			}
			if (items == null)
			{
				throw new ArgumentNullException("items");
			}
			if (items.Length > capacity)
			{
				throw new ArgumentException("Number of items exceeds buffer capacity.", "items");
			}
			_buffer = new T[capacity];
			Array.Copy(items, _buffer, items.Length);
			_front = 0;
			_count = items.Length;
		}

		/// <summary>
		/// The front item in the buffer.
		/// </summary>
		public T Front()
		{
			ThrowIfEmpty();
			return _buffer[_front];
		}

		/// <summary>
		/// The back item in the buffer.
		/// </summary>
		public T Back()
		{
			ThrowIfEmpty();
			int num = BackIndex();
			return _buffer[((num == 0) ? Capacity : num) - 1];
		}

		/// <summary>
		/// Inserts an item at the back of the buffer.
		/// </summary>
		public void PushBack(T item)
		{
			int num = BackIndex();
			if (IsFull)
			{
				_buffer[num] = item;
				_front = Increment(num);
			}
			else
			{
				_buffer[num] = item;
				_count++;
			}
		}

		/// <summary>
		/// Inserts an item at the front of the buffer.
		/// </summary>
		public void PushFront(T item)
		{
			_front = Decrement(_front);
			if (IsFull)
			{
				_buffer[_front] = item;
				return;
			}
			_buffer[_front] = item;
			_count++;
		}

		/// <summary>
		/// Removes and returns the item at the back of the buffer.
		/// </summary>
		public T PopBack()
		{
			ThrowIfEmpty("Cannot take items from an empty buffer.");
			int num = Decrement(BackIndex());
			T result = _buffer[num];
			_buffer[num] = default(T);
			_count--;
			return result;
		}

		/// <summary>
		/// Removes and returns the item at the front of the buffer.
		/// </summary>
		public T PopFront()
		{
			ThrowIfEmpty("Cannot take items from an empty buffer.");
			T result = _buffer[_front];
			_buffer[_front] = default(T);
			_front = Increment(_front);
			_count--;
			return result;
		}

		/// <summary>
		/// Removes all items from the buffer.
		/// </summary>
		public void Clear()
		{
			_front = 0;
			_count = 0;
			Array.Clear(_buffer, 0, _buffer.Length);
		}

		/// <summary>
		/// Returns an <c>ArraySegment</c> pair, where both segments and the items within them follow insertion order.
		/// Does not copy.
		/// </summary>
		public IList<ArraySegment<T>> ToArraySegments()
		{
			return new ArraySegment<T>[2]
			{
				SpanOne(),
				SpanTwo()
			};
		}

		/// <summary>
		/// Returns a new array with the buffer's items in insertion order.
		/// </summary>
		public T[] ToArray()
		{
			T[] array = new T[Count];
			int num = 0;
			IList<ArraySegment<T>> list = ToArraySegments();
			foreach (ArraySegment<T> item in list)
			{
				Array.Copy(item.Array, item.Offset, array, num, item.Count);
				num += item.Count;
			}
			return array;
		}

		/// <summary>
		/// Returns an enumerator that can iterate the buffer.
		/// </summary>
		public IEnumerator<T> GetEnumerator()
		{
			IList<ArraySegment<T>> list = ToArraySegments();
			foreach (ArraySegment<T> segment in list)
			{
				for (int i = 0; i < segment.Count; i++)
				{
					yield return segment.Array[segment.Offset + i];
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private int FrontIndex()
		{
			return _front;
		}

		private int BackIndex()
		{
			return (_front + _count) % Capacity;
		}

		/// <summary>
		/// Converts <c>index</c> into the corresponding index in <c>_buffer</c>.
		/// </summary>
		private int InternalIndex(int index)
		{
			return _front + ((index < Capacity - _front) ? index : (index - Capacity));
		}

		/// <summary>
		/// Increments the provided index variable, wrapping around if necessary.
		/// </summary>
		private int Increment(int index)
		{
			if (index == Capacity - 1)
			{
				return 0;
			}
			return index + 1;
		}

		/// <summary>
		/// Decrements the provided index variable, wrapping around if necessary.
		/// </summary>
		private int Decrement(int index)
		{
			if (index == 0)
			{
				return Capacity - 1;
			}
			return index - 1;
		}

		private void ThrowIfEmpty(string message = "Cannot access an empty buffer.")
		{
			if (IsEmpty)
			{
				throw new InvalidOperationException(message);
			}
		}

		private ArraySegment<T> SpanOne()
		{
			int num = BackIndex();
			if (IsEmpty)
			{
				return new ArraySegment<T>(new T[0]);
			}
			if (_front < num)
			{
				return new ArraySegment<T>(_buffer, _front, num - _front);
			}
			return new ArraySegment<T>(_buffer, _front, _buffer.Length - _front);
		}

		private ArraySegment<T> SpanTwo()
		{
			int num = BackIndex();
			if (IsEmpty)
			{
				return new ArraySegment<T>(new T[0]);
			}
			if (_front < num)
			{
				return new ArraySegment<T>(_buffer, num, 0);
			}
			return new ArraySegment<T>(_buffer, 0, num);
		}
	}
}

