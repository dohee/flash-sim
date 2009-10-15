using System;
using System.Collections.Generic;
using Buffers.Memory;
using Buffers.Queues;

namespace Buffers.Managers
{
	public class LRU : FrameBasedManager
	{
		private LRUQueue queue = new LRUQueue();

		public LRU(uint npages)
			: this(null, npages) { }
		public LRU(IBlockDevice dev, uint npages)
			: base(dev, npages) { }

		protected override void OnPoolFull()
		{
			IFrame frame = queue.Dequeue();
			map.Remove(frame.Id);
			WriteIfDirty(frame);
			pool.FreeSlot(frame.DataSlotId);
		}

		protected override QueueNode OnHit(QueueNode node, bool isWrite)
		{
			return queue.AccessFrame(node);
		}

		protected override QueueNode OnMiss(IFrame allocatedFrame, bool isWrite)
		{
			return queue.Enqueue(allocatedFrame);
		}
	}
}
