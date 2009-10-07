using System;
using System.Collections.Generic;
using Buffers.Memory;
using Buffers.Queues;

namespace Buffers.Managers
{
	public abstract class FrameBasedManager : BufferManagerBase
	{
		protected Pool pool;
		protected Dictionary<uint, QueueNode> map = new Dictionary<uint, QueueNode>();

		public FrameBasedManager(IBlockDevice dev, uint npages)
			: base(dev)
		{
			pool = new Pool(npages, dev.PageSize, OnPoolFull);
		}

		protected abstract void OnPoolFull();
		protected abstract QueueNode OnHit(QueueNode node, bool isWrite);
		protected abstract QueueNode OnMiss(IFrame allocatedFrame, bool isWrite);
		

		protected sealed override void DoRead(uint pageid, byte[] result)
		{
			QueueNode node;
			IFrame frame;

			if (map.TryGetValue(pageid, out node))
			{
				node = OnHit(node, false);
				frame = node.ListNode.Value;
			}
			else
			{
				frame = pool.AllocFrame();
				frame.Id = pageid;
				dev.Read(pageid, frame.Data);
				node = OnMiss(frame, false);
			}

			map[pageid] = node;
			frame.Data.CopyTo(result, 0);
		}

		protected sealed override void DoWrite(uint pageid, byte[] data)
		{
			QueueNode node;
			IFrame frame;

			if (map.TryGetValue(pageid, out node))
			{
				node = OnHit(node, true);
				frame = node.ListNode.Value;
			}
			else
			{
				frame = pool.AllocFrame();
				frame.Id = pageid;
				node = OnMiss(frame, true);
			}

			map[pageid] = node;
			data.CopyTo(frame.Data, 0);
			frame.Dirty = true;
		}

		protected override void DoFlush()
		{
			foreach (var entry in map)
				WriteIfDirty(entry.Value.ListNode.Value);
		}

		protected void WriteIfDirty(IFrame frame)
		{
			if (frame.Dirty)
			{
				dev.Write(frame.Id, frame.Data);
				frame.Dirty = false;
			}
		}
	}
}
