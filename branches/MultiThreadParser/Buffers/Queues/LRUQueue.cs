using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Buffers.Queues
{
	public class LRUQueue<T> : FIFOQueue<T>
	{
		public override QueueNode<T> Access(QueueNode<T> node)
		{
			Debug.Assert(node.Index == 0);
			return new QueueNode<T>(AccessFrame(node.ListNode));
		}

		public LinkedListNode<T> AccessFrame(LinkedListNode<T> node)
		{
			T frame = node.Value;
			queue.Remove(node);
			return queue.AddFirst(frame);
		}

	}
}
