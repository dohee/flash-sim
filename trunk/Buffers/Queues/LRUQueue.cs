using System.Collections.Generic;
using System.Diagnostics;
using Buffers.Memory;

namespace Buffers.Queues
{
	public class LRUQueue : FIFOQueue
	{
		public override QueueNode AccessFrame(QueueNode node)
		{
			Debug.Assert(node.Index == 0);
			return new QueueNode(AccessFrame(node.ListNode));
		}

		public LinkedListNode<IFrame> AccessFrame(LinkedListNode<IFrame> node)
		{
			IFrame frame = node.Value;
			queue.Remove(node);
			return queue.AddFirst(frame);
		}

	}
}
