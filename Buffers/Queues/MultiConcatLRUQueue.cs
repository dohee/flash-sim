using System;
using System.Collections.Generic;

namespace Buffers.Queues
{
	public class MultiConcatLRUQueue : QueueGroup
	{
		private ConcatenatedLRUQueue[] q;

		public MultiConcatLRUQueue(ICollection<ConcatenatedLRUQueue> queues)
		{
			q = new ConcatenatedLRUQueue[queues.Count];

			int i = 0;
			foreach (var queue in queues)
				q[i++] = queue;

			this.queues = q;
			BuildRoutes();
		}

		public uint GetSize(int queueIndex) { return q[queueIndex].Size; }
		public uint GetFrontSize(int queueIndex) { return q[queueIndex].FrontQueueSize; }
		public uint GetBackSize(int queueIndex) { return q[queueIndex].BackQueueSize; }


		public override QueueNode Enqueue(IFrame frame)
		{
			throw new NotSupportedException("Enqueue into which?");
		}
		public override IFrame Dequeue()
		{
			throw new NotSupportedException("Dequeue from which?");
		}

		public QueueNode Enqueue(int queueIndex, IFrame frame)
		{
			QueueNode qn = q[queueIndex].Enqueue(frame);
			return NATOutwards((uint)queueIndex, qn);
		}
		public IFrame Dequeue(int queueIndex)
		{
			return q[queueIndex].Dequeue();
		}
		public QueueNode BlowOneFrame(int queueIndex)
		{
			QueueNode qn = q[queueIndex].BlowOneFrame();
			return NATOutwards((uint)queueIndex, qn);
		}

		public bool IsInQueue(QueueNode node, int queueIndex)
		{
			return NATInwards(node).QueueIndex == (uint)queueIndex;
		}
	}
}
