using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Buffers.Memory;

namespace Buffers.Queues
{
	public delegate void CountQueue();

	public struct QueueNode
	{
		public readonly uint Index;
		public readonly LinkedListNode<IFrame> ListNode;

		public QueueNode(LinkedListNode<IFrame> node)
			: this(0, node) { }

		public QueueNode(uint major, LinkedListNode<IFrame> node)
		{
			Index = major;
			ListNode = node;
		}
	}

	public interface IQueue : IEnumerable<IFrame>
	{
		void CountQueue(CountQueue callback);

		uint Size { get; }

		QueueNode Enqueue(IFrame frame);
		IFrame Dequeue();
		IFrame Dequeue(QueueNode node);
		void AccessFrame(QueueNode node, out QueueNode newNode);
	}


	public abstract class QueueBase : IQueue
	{
		private bool inited = false;
		protected CountQueue countQueueCallback;


		public abstract uint Size { get; }
		public abstract IEnumerator<IFrame> GetEnumerator();

		public abstract QueueNode Enqueue(IFrame frame);
		public abstract IFrame Dequeue();
		public abstract IFrame Dequeue(QueueNode node);
		public abstract void AccessFrame(QueueNode node, out QueueNode newNode);

		protected abstract void DoCountQueue();

		public void CountQueue()
		{
			CountQueue(null);
		}
		public void CountQueue(CountQueue callback)
		{
			if (inited)
				throw new InvalidOperationException("The queue is counted already");

			inited = true;
			countQueueCallback = callback;
			DoCountQueue();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
