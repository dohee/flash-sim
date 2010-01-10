using System;
using System.Collections.Generic;
using Buffers.Memory;
using Buffers.Queues;

namespace Buffers.Managers
{
	public class LRU : FrameBasedManager
	{
		private LRUQueue<IFrame> queue = new LRUQueue<IFrame>();

		public LRU(uint npages)
			: this(null, npages) { }
		public LRU(IBlockDevice dev, uint npages)
			: base(dev, npages) { }

		public override string Description { get { return "NPages=" + pool.NPages; } }

		protected override void OnPoolFull()
		{
			IFrame frame = queue.Dequeue();
			map.Remove(frame.Id);
			WriteIfDirty(frame);
			pool.FreeSlot(frame.DataSlotId);
		}

		protected override QueueNode<IFrame> OnHit(QueueNode<IFrame> node, AccessType type)
		{
			return queue.Access(node);
		}

		protected override QueueNode<IFrame> OnMiss(IFrame allocatedFrame, AccessType type)
		{
			return queue.Enqueue(allocatedFrame);
		}
	}
}
