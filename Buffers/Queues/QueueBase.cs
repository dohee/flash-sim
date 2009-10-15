using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Buffers.Memory;

namespace Buffers.Queues
{
	public abstract class QueueBase : IQueue
	{
		public abstract uint Size { get; }
		public abstract IEnumerator<IFrame> GetEnumerator();

		public abstract QueueNode Enqueue(IFrame frame);
		public abstract IFrame Dequeue();
		public abstract IFrame Dequeue(QueueNode node);
		public abstract QueueNode AccessFrame(QueueNode node);

		public virtual uint BasicQueueCount { get { return 1; } }
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}
