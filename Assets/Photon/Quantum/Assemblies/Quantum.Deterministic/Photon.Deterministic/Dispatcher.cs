using System;
using System.Collections.Generic;
using Quantum;

namespace Photon.Deterministic
{
	public class Dispatcher<T>
	{
		private readonly Dictionary<Type, Action<T>> _callbacks = new Dictionary<Type, Action<T>>();

		public void Bind<K>(Action<K> callback) where K : T
		{
			_callbacks.Add(typeof(K), delegate(T obj)
			{
				callback((K)(object)obj);
			});
		}

		public void DispatchNext(Queue<T> queue)
		{
			if (queue.Count > 0)
			{
				Dispatch(queue.Dequeue());
			}
		}

		public void Dispatch(T obj)
		{
			try
			{
				if (_callbacks.TryGetValue(obj.GetType(), out var value))
				{
					value(obj);
					return;
				}
				LogStream logError = InternalLogStreams.LogError;
				if (logError != null)
				{
					logError.Log($"Could not dispatch callback for {obj.GetType()}");
				}
			}
			catch (Exception ex)
			{
				LogStream logException = InternalLogStreams.LogException;
				if (logException != null)
				{
					logException.Log(ex);
				}
			}
		}
	}
}

