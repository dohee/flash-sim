using System;
using System.Collections.Generic;
using System.Diagnostics;
using Buffers.Memory;

namespace Buffers.Queues
{
	public class ConcatenatedLRUQueue : QueueGroup
	{
		public ConcatenatedLRUQueue(IQueue front, IQueue back)
		{
			queues = new IQueue[] { front, back };
			BuildRoutes();
		}

		public uint FrontQueueSize { get { return queues[0].Size; } }
		public uint BackQueueSize { get { return queues[1].Size; } }

		public override QueueNode Enqueue(IFrame frame)
		{
			QueueNode qn = queues[0].Enqueue(frame);
			return NATOutwards(0, qn);
		}
		public override IFrame Dequeue()
		{
			return queues[1].Dequeue();
		}

		public override QueueNode AccessFrame(QueueNode node)
		{
			RoutingNode routing = NATInwards(node);
			QueueNode qn = queues[0].Enqueue(
				queues[(int)routing.QueueIndex].Dequeue(routing.InnerNode));
			return NATOutwards(0, qn);
		}

		public QueueNode BlowOneFrame()
		{
			IFrame victim = queues[0].Dequeue();
			QueueNode qn = queues[1].Enqueue(victim);
			return NATOutwards(1, qn);
		}
	}
}
