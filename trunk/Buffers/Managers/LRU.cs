using System;
using System.Collections.Generic;
using Buffers.Memory;
using Buffers.Queues;

namespace Buffers.Managers
{
	public class LRU : FrameBasedManager
	{
		private LRUQueue queue = new LRUQueue();

		public LRU(IBlockDevice dev, uint npages)
			: base(dev, npages) { }

		protected override void OnPoolFull()
		{
			IFrame frame = queue.Dequeue();
			map.Remove(frame.Id);
			WriteIfDirty(frame);
			frame.Resident = false;
		}

		protected override QueueNode OnHit(QueueNode node, bool isWrite)
		{
			return queue.Enqueue(queue.Dequeue(node));
		}

		protected override QueueNode OnMiss(IFrame allocatedFrame, bool isWrite)
		{
			return queue.Enqueue(allocatedFrame);
		}
	}
}
