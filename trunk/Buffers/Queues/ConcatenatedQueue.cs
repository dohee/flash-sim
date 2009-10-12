using System;
using System.Collections.Generic;
using System.Diagnostics;
using Buffers.Memory;

namespace Buffers.Queues
{
	public class ConcatenatedQueue : QueueGroup
	{
		public ConcatenatedQueue(IQueue front, IQueue back)
		{
			queues.Add(front);
			queues.Add(back);
		}

		public IQueue FrontQueue { get { return queues[0]; } }
		public IQueue BackQueue { get { return queues[1]; } }

		public override uint Size
		{
			get { return queues[0].Size + queues[1].Size; }
		}

		public override QueueNode Enqueue(IFrame frame)
		{
			QueueNode qn = queues[0].Enqueue(frame);
			return NATOutwards(0, qn);
		}
		public override IFrame Dequeue()
		{
			return queues[1].Dequeue();
		}

		public override void AccessFrame(QueueNode node, out QueueNode newNode)
		{
			RoutingNode routing = NATInwards(node);
			QueueNode returnednode;

			if (routing.QueueIndex == 0)
				queues[0].AccessFrame(routing.InnerNode, out returnednode);
			else
				returnednode = queues[0].Enqueue(queues[1].Dequeue(routing.InnerNode));

			newNode = NATOutwards(0, returnednode);
		}

		public QueueNode BlowOneFrame()
		{
			IFrame victim = queues[0].Dequeue();
			QueueNode qn = queues[1].Enqueue(victim);
			return NATOutwards(1, qn);
		}
	}
}
