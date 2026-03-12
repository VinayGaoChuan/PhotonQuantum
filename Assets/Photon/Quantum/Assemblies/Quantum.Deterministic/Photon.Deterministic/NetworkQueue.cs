using System.Collections.Generic;

namespace Photon.Deterministic
{
	internal class NetworkQueue<T>
	{
		private Queue<T> _eventsQueue = new Queue<T>(1024);

		public void Push(T ev)
		{
			lock (_eventsQueue)
			{
				_eventsQueue.Enqueue(ev);
			}
		}

		public bool TryPop(out T ev)
		{
			lock (_eventsQueue)
			{
				if (_eventsQueue.Count > 0)
				{
					ev = _eventsQueue.Dequeue();
					return true;
				}
			}
			ev = default(T);
			return false;
		}
	}
}

