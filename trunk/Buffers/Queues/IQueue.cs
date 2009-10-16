using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Buffers.Memory;

namespace Buffers.Queues
{
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

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			if (this.GetType() != obj.GetType())
				return false;

			return this == (QueueNode)obj;
		}

		public static bool operator ==(QueueNode left, QueueNode right)
		{
			return left.Index == right.Index && left.ListNode == right.ListNode;
		}

		public static bool operator !=(QueueNode left, QueueNode right)
		{
			return !(left == right);
		}

		public override int GetHashCode()
		{
			return Index.GetHashCode() ^ ListNode.GetHashCode();
		}
	}


	public interface IQueue : IEnumerable<IFrame>
	{
		uint Size { get; }
		uint BasicQueueCount { get; }

		QueueNode Enqueue(IFrame frame);
		IFrame Dequeue();
		IFrame Dequeue(QueueNode node);
		QueueNode AccessFrame(QueueNode node);
	}
}
