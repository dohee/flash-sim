using System.Collections.Generic;
using System.Diagnostics;
using Buffers.Memory;

namespace Buffers.Queues
{
	public class FIFOQueue : QueueBase
	{
		protected LinkedList<IFrame> queue = new LinkedList<IFrame>();

		protected override sealed void DoCountQueue()
		{
			if (countQueueCallback != null)
				countQueueCallback();
		}

		public override sealed uint Size
		{
			get { return (uint)queue.Count; }
		}

		public override IEnumerator<IFrame> GetEnumerator()
		{
			return queue.GetEnumerator();
		}

		public override sealed QueueNode Enqueue(IFrame frame)
		{
			return new QueueNode(queue.AddFirst(frame));
		}

		public override sealed IFrame Dequeue(QueueNode node)
		{
			Debug.Assert(node.Index == 0);
			IFrame f = node.ListNode.Value;
			queue.Remove(node.ListNode);
			return f;
		}

		public override sealed IFrame Dequeue()
		{
			return Dequeue(new QueueNode(queue.Last));
		}

		public override void AccessFrame(QueueNode node, out QueueNode newNode)
		{
			Debug.Assert(node.Index == 0);
			newNode = node;
		}

	}
}
