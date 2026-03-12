using System;
using System.Collections.Generic;
using Quantum;

namespace Photon.Deterministic
{
	internal class RawInputCache
	{
		public struct Entry
		{
			public int Frame;

			public int Length;

			public int[] Buffer;
		}

		private RingBuffer<Entry> _cache;

		private Stack<int[]> _buffers;

		public RawInputCache(int capacity = 256)
		{
			_cache = new RingBuffer<Entry>(capacity, overwrite: false);
			_buffers = new Stack<int[]>(capacity);
		}

		public void Clear()
		{
			_cache.Clear();
			_buffers.Clear();
		}

		public void Dequeue(int frame, ref int[] data)
		{
			Assert.Always<int>(!_cache.IsEmpty, "RawInputCache unexpected frame {0} request. Cache is empty.", frame);
			Entry entry = _cache.Pop();
			Assert.Always<int, int>(entry.Frame == frame, "RawInputCache unexpected frame request {0} (expected {1})", frame, entry.Frame);
			Array.Copy(entry.Buffer, data, entry.Buffer.Length);
			_buffers.Push(entry.Buffer);
		}

		public void Enqueue(int frame, int[] data)
		{
			int[] array = null;
			if (_buffers.Count > 0)
			{
				array = _buffers.Pop();
				if (array.Length < data.Length)
				{
					Array.Resize(ref array, data.Length);
				}
			}
			else
			{
				array = new int[data.Length];
			}
			Array.Copy(data, array, data.Length);
			if (_cache.IsFull)
			{
				_cache.Grow();
			}
			_cache.Push(new Entry
			{
				Frame = frame,
				Length = data.Length,
				Buffer = array
			});
		}
	}
}

