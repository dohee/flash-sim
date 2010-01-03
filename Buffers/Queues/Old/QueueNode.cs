using System;
using System.Collections.Generic;

namespace Buffers.Queues
{
	public struct QueueNode<T> : IEquatable<QueueNode<T>>
	{
		public readonly uint Index;
		public readonly LinkedListNode<T> ListNode;

		public QueueNode(LinkedListNode<T> node)
			: this(0, node) { }

		public QueueNode(uint major, LinkedListNode<T> node)
		{
			Index = major;
			ListNode = node;
		}

		#region Equals 函数族
		public bool Equals(QueueNode<T> other)
		{
			return Index == other.Index && ListNode == other.ListNode;
		}
		public override int GetHashCode()
		{
			return Index.GetHashCode() ^ ListNode.GetHashCode();
		}
		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
				return false;
			else
				return Equals((QueueNode<T>)obj);
		}
		public static bool operator ==(QueueNode<T> left, QueueNode<T> right)
		{
			return left.Equals(right);
		}
		public static bool operator !=(QueueNode<T> left, QueueNode<T> right)
		{
			return !left.Equals(right);
		}
		#endregion
	}
}
