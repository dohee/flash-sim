using System;
using System.Collections;
using System.Collections.Generic;

namespace Buffers.Queues
{
	public abstract class QueueBase<T> : IQueue<T>
	{
		public abstract uint Size { get; }
		public abstract IEnumerator<T> GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public abstract QueueNode<T> Enqueue(T item);
		public abstract T Dequeue();
		public abstract T Dequeue(QueueNode<T> node);
		public abstract QueueNode<T> Access(QueueNode<T> node);

		public virtual uint BasicQueueCount { get { return 1; } }
		public virtual uint GetRoute(QueueNode<T> node) { return 0; }

		public virtual IList<uint> GetRoutePath(QueueNode<T> node)
		{
			var list = new List<uint>();
			list.Add(0);
			return list;
		}
	}
}
