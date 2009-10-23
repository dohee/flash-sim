using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Buffers.Queues
{
	public class ConcatenatedLRUQueue<T> : QueueGroup<T>
	{
		public ConcatenatedLRUQueue(IQueue<T> front, IQueue<T> back)
		{
			queues = new IQueue<T>[] { front, back };
			BuildRoutes();
		}

		public uint FrontQueueSize { get { return queues[0].Size; } }
		public uint BackQueueSize { get { return queues[1].Size; } }

		public override QueueNode<T> Enqueue(T item)
		{
			QueueNode<T> qn = queues[0].Enqueue(item);
			return NATOutwards(0, qn);
		}
		public override T Dequeue()
		{
			return queues[1].Dequeue();
		}

		public override QueueNode<T> Access(QueueNode<T> node)
		{
			RoutingNode routing = NATInwards(node);
			QueueNode<T> qn = queues[0].Enqueue(
				queues[(int)routing.QueueIndex].Dequeue(routing.InnerNode));
			return NATOutwards(0, qn);
		}

		public QueueNode<T> BlowOneFrame()
		{
			T victim = queues[0].Dequeue();
			QueueNode<T> qn = queues[1].Enqueue(victim);
			return NATOutwards(1, qn);
		}
	}
}
