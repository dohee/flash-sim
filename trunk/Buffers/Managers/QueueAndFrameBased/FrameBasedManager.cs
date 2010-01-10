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
		protected abstract QueueNode<IFrame> OnHit(QueueNode<IFrame> node, AccessType type);
		protected abstract QueueNode<IFrame> OnMiss(IFrame allocatedFrame, AccessType type);

		protected virtual IFrame CreateFrame(uint pageid, int slotid)
		{
			return new Frame(pageid, slotid);
		}

		protected sealed override void DoAccess(uint pageid, byte[] resultOrData, AccessType type)
		{
			QueueNode<IFrame> node;
			IFrame frame;

			if (map.TryGetValue(pageid, out node))
			{
				node = OnHit(node, type);
				frame = node.ListNode.Value;
			}
			else
			{
				frame = CreateFrame(pageid, pool.AllocSlot());

				if (type == AccessType.Read)
					dev.Read(pageid, pool[frame.DataSlotId]);

				node = OnMiss(frame, type);
			}

			map[pageid] = node;

			if (type == AccessType.Read)
			{
				pool[frame.DataSlotId].CopyTo(resultOrData, 0);
			}
			else
			{
				resultOrData.CopyTo(pool[frame.DataSlotId], 0);
				frame.Dirty = true;
			}
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
