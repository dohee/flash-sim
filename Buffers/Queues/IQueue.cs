using System;
using System.Collections.Generic;

namespace Buffers.Queues
{
	public interface IQueue<T> : IEnumerable<T>
	{
		uint Size { get; }
		uint BasicQueueCount { get; }

		QueueNode<T> Enqueue(T frame);
		T Dequeue();
		T Dequeue(QueueNode<T> node);
		QueueNode<T> AccessFrame(QueueNode<T> node);
	}
}
