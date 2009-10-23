using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Buffers.Queues
{
	public class FIFOQueue<T> : QueueBase<T>
	{
		protected LinkedList<T> queue = new LinkedList<T>();

		public override sealed uint Size
		{
			get { return (uint)queue.Count; }
		}

		public override IEnumerator<T> GetEnumerator()
		{
			return queue.GetEnumerator();
		}

		public override sealed QueueNode<T> Enqueue(T frame)
		{
			return new QueueNode<T>(queue.AddFirst(frame));
		}

		public override sealed T Dequeue(QueueNode<T> node)
		{
			Debug.Assert(node.Index == 0);
			T f = node.ListNode.Value;
			queue.Remove(node.ListNode);
			return f;
		}

		public override sealed T Dequeue()
		{
			return Dequeue(new QueueNode<T>(queue.Last));
		}

		public override QueueNode<T> AccessFrame(QueueNode<T> node)
		{
			Debug.Assert(node.Index == 0);
			return node;
		}

	}
}
