using System;
using System.Collections.Generic;
using System.Diagnostics;
using Buffers.Memory;
using Buffers.Queues;
using Buffers.Lists;

namespace Buffers.Managers
{
	public sealed class FLIRS : BufferManagerBase
	{
		private Pool pool;
		private MultiList<RWQueryWithIRFlag> rwlist;
		private IDictionary<uint, RWFrame> map = new Dictionary<uint, RWFrame>();

		public FLIRS(IBlockDevice dev, uint npages)
			: base(dev)
		{
			pool = new Pool(npages, this.dev.PageSize, OnPoolFull);
			rwlist = new MultiList<RWQueryWithIRFlag>(3);
			rwlist.SetConcat(0, 1);
		}

		private int RLIRLength { get { return rwlist.GetNodeCount(0); } }
		private int WLIRLength { get { return rwlist.GetNodeCount(0) + rwlist.GetNodeCount(1); } }
		private int HIRLength { get { return rwlist.GetNodeCount(2); } }

		private void MarkAsLIR(MultiListNode<RWQueryWithIRFlag> node)
		{
			RWQueryWithIRFlag query = node.Value;
			query.IsLowIR = true;
			node.Value = query;
		}
		private void MarkAsHIR(MultiListNode<RWQueryWithIRFlag> node)
		{
			RWQueryWithIRFlag query = node.Value;
			query.IsLowIR = false;
			node.Value = query;
		}

		private MultiListNode<RWQueryWithIRFlag> RetopQuery(MultiListNode<RWQueryWithIRFlag> node)
		{
			if (node.ListIndex == 2)
				Debug.Assert(!node.Value.IsLowIR);
			else
				MarkAsLIR(node);

			return rwlist.AddFirst(0, rwlist.Remove(node));
		}


		private void OnPoolFull()
		{
		}

		protected sealed override void DoRead(uint pageid, byte[] result)
		{
			MultiListNode<RWQueryWithIRFlag> rnode = null;
			RWFrame frame = null;

			if (!map.TryGetValue(pageid, out frame))
			{
				frame = new RWFrame(pageid);
				map[pageid] = frame;
			}

			if ((rnode = frame.NodeOfRead) == null)
			{
				rnode = rwlist.AddFirst(0, new RWQueryWithIRFlag(pageid, false, false));
				frame.NodeOfRead = rnode;
			}

			if (!frame.Resident) { }
			//TODO 
		}

		protected sealed override void DoWrite(uint pageid, byte[] data)
		{
		}

		protected override void DoFlush()
		{
		}

		private void WriteIfDirty(IFrame frame)
		{
			if (frame.Dirty)
			{
				dev.Write(frame.Id, pool[frame.DataSlotId]);
				frame.Dirty = false;
			}
		}



		private class RWFrame : FrameWithRWInfo<MultiListNode<RWQueryWithIRFlag>>
		{
			public RWFrame(uint id) : base(id) { }
			public RWFrame(uint id, int slotid) : base(id, slotid) { }
		}

	}
}
