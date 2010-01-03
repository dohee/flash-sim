using System;
using System.Collections.Generic;

namespace Buffers.Queues
{
	public interface IQueue<T> : IEnumerable<T>
	{
		uint Size { get; }
		uint BasicQueueCount { get; }

		QueueNode<T> Enqueue(T item);
		T Dequeue();
		T Dequeue(QueueNode<T> node);
		QueueNode<T> Access(QueueNode<T> node);

		uint GetRoute(QueueNode<T> node);
		IList<uint> GetRoutePath(QueueNode<T> node);
	}
}
