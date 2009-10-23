using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Buffers.Queues
{
	public abstract class QueueBase<T> : IQueue<T>
	{
		public abstract uint Size { get; }
		public abstract IEnumerator<T> GetEnumerator();

		public abstract QueueNode<T> Enqueue(T frame);
		public abstract T Dequeue();
		public abstract T Dequeue(QueueNode<T> node);
		public abstract QueueNode<T> AccessFrame(QueueNode<T> node);

		public virtual uint BasicQueueCount { get { return 1; } }
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}
