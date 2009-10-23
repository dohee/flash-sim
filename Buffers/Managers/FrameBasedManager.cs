using System;
using System.Collections.Generic;
using Buffers.Memory;
using Buffers.Queues;

namespace Buffers.Managers
{
	public abstract class FrameBasedManager : BufferManagerBase
	{
		protected Pool pool;
		public IDictionary<uint, QueueNode<IFrame>> map = new Dictionary<uint, QueueNode<IFrame>>();

		public FrameBasedManager(IBlockDevice dev, uint npages)
			: base(dev)
		{
			pool = new Pool(npages, this.dev.PageSize, OnPoolFull);
		}

		protected abstract void OnPoolFull();
		protected abstract QueueNode<IFrame> OnHit(QueueNode<IFrame> node, bool isWrite);
		protected abstract QueueNode<IFrame> OnMiss(IFrame allocatedFrame, bool isWrite);

		protected virtual IFrame CreateFrame(uint pageid, int slotid)
		{
			return new Frame(pageid, slotid);
		}


		protected sealed override void DoRead(uint pageid, byte[] result)
		{
			QueueNode<IFrame> node;
			IFrame frame;

			if (map.TryGetValue(pageid, out node))
			{
				node = OnHit(node, false);
				frame = node.ListNode.Value;
			}
			else
			{
				frame = CreateFrame(pageid, pool.AllocSlot());
				dev.Read(pageid, pool[frame.DataSlotId]);
				node = OnMiss(frame, false);
			}

			map[pageid] = node;
			pool[frame.DataSlotId].CopyTo(result, 0);
		}

		protected sealed override void DoWrite(uint pageid, byte[] data)
		{
			QueueNode<IFrame> node;
			IFrame frame;

			if (map.TryGetValue(pageid, out node))
			{
				node = OnHit(node, true);
				frame = node.ListNode.Value;
			}
			else
			{
				frame = CreateFrame(pageid, pool.AllocSlot());
				node = OnMiss(frame, true);
			}

			map[pageid] = node;
			data.CopyTo(pool[frame.DataSlotId], 0);
			frame.Dirty = true;
		}

		protected override void DoFlush()
		{
			foreach (var entry in map)
			{
				WriteIfDirty(entry.Value.ListNode.Value);
				entry.Value.ListNode.Value.Dirty = false;
			}
		}

		protected void WriteIfDirty(IFrame frame)
		{
			if (frame.Dirty)
			{
				dev.Write(frame.Id, pool[frame.DataSlotId]);
				frame.Dirty = false;
			}
		}
	}
}
