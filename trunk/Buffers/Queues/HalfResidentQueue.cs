using System;
using System.Collections.Generic;
using System.Diagnostics;
using Buffers.Memory;

namespace Buffers.Queues
{
	public class HalfResidentQueue : QueueGroup
	{
		public HalfResidentQueue()
		{
			queues.Add(new LRUQueue());
			queues.Add(new FIFOQueue());
		}

		public override uint Size
		{
			get { return queues[0].Size + queues[1].Size; }
		}
		public uint ResidentSize
		{
			get { return queues[0].Size; }
		}

		public override void AccessFrame(QueueNode node, out QueueNode newNode)
		{
			RoutingNode routing = NATInwards(node);
			QueueNode returnednode;

			if (routing.QueueIndex == 0)
			{
				queues[0].AccessFrame(routing.InnerNode, out returnednode);
			}
			else
			{
				IFrame frame = queues[1].Dequeue(routing.InnerNode);
				Debug.Assert(frame.Resident == false);
				frame.Resident = true;
				returnednode = queues[0].Enqueue(frame);
			}

			newNode = NATOutwards(0, returnednode);
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

		public void FreeOneSlot(out uint id, out QueueNode newposition)
		{
			IFrame victim = queues[0].Dequeue();
			Debug.Assert(victim.Resident == true);
			victim.Resident = false;
			QueueNode qn = queues[1].Enqueue(victim);

			id = victim.Id;
			newposition = NATOutwards(1, qn);
		}
	}
}
