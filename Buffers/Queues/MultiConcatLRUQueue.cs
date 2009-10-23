using System;
using System.Collections.Generic;
using Buffers.Memory;

namespace Buffers.Queues
{
	public class MultiConcatLRUQueue<T> : QueueGroup<T>
	{
		private ConcatenatedLRUQueue<T>[] q;

		public MultiConcatLRUQueue(ICollection<ConcatenatedLRUQueue<T>> queues)
		{
			q = new ConcatenatedLRUQueue<T>[queues.Count];

			int i = 0;
			foreach (var queue in queues)
				q[i++] = queue;

			this.queues = q;
			BuildRoutes();
		}

		public uint GetSize(int queueIndex) { return q[queueIndex].Size; }
		public uint GetFrontSize(int queueIndex) { return q[queueIndex].FrontQueueSize; }
		public uint GetBackSize(int queueIndex) { return q[queueIndex].BackQueueSize; }


		public override QueueNode<T> Enqueue(T item)
		{
			throw new NotSupportedException("Enqueue into which?");
		}
		public override T Dequeue()
		{
			throw new NotSupportedException("Dequeue from which?");
		}

		public QueueNode<T> Enqueue(int queueIndex, T item)
		{
			QueueNode<T> qn = q[queueIndex].Enqueue(item);
			return NATOutwards((uint)queueIndex, qn);
		}
		public T Dequeue(int queueIndex)
		{
			return q[queueIndex].Dequeue();
		}
		public QueueNode<T> BlowOneFrame(int queueIndex)
		{
			QueueNode<T> qn = q[queueIndex].BlowOneFrame();
			return NATOutwards((uint)queueIndex, qn);
		}
	}
}
