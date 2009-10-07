using System.Collections.Generic;
using System.Diagnostics;
using Buffers.Memory;

namespace Buffers.Queues
{
	public sealed class LRUQueue : FIFOQueue
	{
		public override void AccessFrame(QueueNode node, out QueueNode newNode)
		{
			Debug.Assert(node.Index == 0);
			newNode = new QueueNode(AccessFrame(node.ListNode));
		}

		public LinkedListNode<IFrame> AccessFrame(LinkedListNode<IFrame> node)
		{
			IFrame frame = node.Value;
			queue.Remove(node);
			return queue.AddFirst(frame);
		}

	}
}
