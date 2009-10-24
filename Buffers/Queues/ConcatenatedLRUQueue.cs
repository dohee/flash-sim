using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Buffers.Queues
{
	public class ConcatenatedLRUQueue<T> : QueueGroup<T>
	{
		public ConcatenatedLRUQueue(IQueue<T> front)
			: this(front, null) { }

		public ConcatenatedLRUQueue(IQueue<T> front, IQueue<T> back)
		{
			if (back == null)
				queues = new IQueue<T>[] { front };
			else
				queues = new IQueue<T>[] { front, back };

			BuildRoutes();
		}

		public uint FrontQueueSize
		{
			get { return queues[0].Size; }
		}
		public uint BackQueueSize
		{
			get
			{
				if (queues.Length == 2)
					return queues[1].Size;
				else
					return 0;
			}
		}


		public override QueueNode<T> Enqueue(T item)
		{
			QueueNode<T> qn = queues[0].Enqueue(item);
			return NATOutwards(0, qn);
		}
		public override T Dequeue()
		{
			if (BackQueueSize == 0)
				return queues[0].Dequeue();
			else
				return queues[1].Dequeue();
		}

		public override QueueNode<T> Access(QueueNode<T> node)
		{
			RoutingNode routing = NATInwards(node);
			QueueNode<T> qn = queues[0].Enqueue(
				queues[(int)routing.QueueIndex].Dequeue(routing.InnerNode));
			return NATOutwards(0, qn);
		}

		public QueueNode<T> BlowOneItem()
		{
			Debug.Assert(queues.Length == 2);
			T victim = queues[0].Dequeue();
			QueueNode<T> qn = queues[1].Enqueue(victim);
			return NATOutwards(1, qn);
		}
	}
}
