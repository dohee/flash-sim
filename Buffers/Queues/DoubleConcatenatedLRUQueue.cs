using System;
using System.Collections.Generic;

namespace Buffers.Queues
{
	public class DoubleConcatenatedLRUQueue : QueueGroup
	{
		private ConcatenatedLRUQueue q1, q2;

		public DoubleConcatenatedLRUQueue(ConcatenatedLRUQueue q1, ConcatenatedLRUQueue q2)
		{
			this.q1 = q1;
			this.q2 = q2;
			queues.Add(q1);
			queues.Add(q2);
			BuildRoutes();
		}

		public uint Q1Size { get { return q1.Size; } }
		public uint Q2Size { get { return q2.Size; } }
		public uint Q1FrontSize { get { return q1.FrontQueueSize; } }
		public uint Q2FrontSize { get { return q2.FrontQueueSize; } }
		public uint Q1BackSize { get { return q1.BackQueueSize; } }
		public uint Q2BackSize { get { return q2.BackQueueSize; } }


		public override QueueNode Enqueue(IFrame frame)
		{
			throw new NotSupportedException("Enqueue into which?");
		}
		public override IFrame Dequeue()
		{
			throw new NotSupportedException("Dequeue from which?");
		}

		public QueueNode EnqueueQ1(IFrame frame)
		{
			QueueNode qn = q1.Enqueue(frame);
			return NATOutwards(0, qn);
		}
		public QueueNode EnqueueQ2(IFrame frame)
		{
			QueueNode qn = q2.Enqueue(frame);
			return NATOutwards(1, qn);
		}

		public IFrame DequeueQ1()
		{
			return q1.Dequeue();
		}
		public IFrame DequeueQ2()
		{
			return q2.Dequeue();
		}

		public QueueNode BlowQ1()
		{
			QueueNode qn = q1.BlowOneFrame();
			return NATOutwards(0, qn);
		}
		public QueueNode BlowQ2()
		{
			QueueNode qn = q2.BlowOneFrame();
			return NATOutwards(1, qn);
		}

		public bool IsInQ1(QueueNode node)
		{
			return NATInwards(node).QueueIndex == 0;
		}
		public bool IsInQ2(QueueNode node)
		{
			return NATInwards(node).QueueIndex == 1;
		}
	}
}
