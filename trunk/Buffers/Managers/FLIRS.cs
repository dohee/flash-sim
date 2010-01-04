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
		private readonly float ratio;
		private readonly Pool pool;
		private readonly MultiList<RWQuery> rwlist;
		private readonly IDictionary<uint, RWFrame> map = new Dictionary<uint, RWFrame>();

		public FLIRS(IBlockDevice dev, uint npages, float ratio)
			: base(dev)
		{
			this.ratio = ratio;
			pool = new Pool(npages, this.dev.PageSize, OnPoolFull);
			rwlist = new MultiList<RWQuery>(3);
			rwlist.SetConcat(0, 1);
		}

		private int RLIRLength { get { return rwlist.GetNodeCount(0); } }
		private int WLIRLength { get { return rwlist.GetNodeCount(0) + rwlist.GetNodeCount(1); } }
		private int HIRQueueLength { get { return rwlist.GetNodeCount(2); } }




		private void OnPoolFull()
		{


			Tidy();

		}

		protected sealed override void DoRead(uint pageid, byte[] result)
		{
			RWFrame frame = null;

			if (!map.TryGetValue(pageid, out frame))
			{
				frame = new RWFrame(pageid);
				map[pageid] = frame;
			}

			bool isLowIRAfter = (frame.NodeOfRead != null);

			if (frame.NodeOfRead != null)
				rwlist.Remove(frame.NodeOfRead);
			if (frame.NodeOfReadInHIRQueue != null)
				rwlist.Remove(frame.NodeOfReadInHIRQueue);

			if (!frame.Resident)
			{
				frame.DataSlotId = pool.AllocSlot();
				dev.Read(pageid, pool[frame.DataSlotId]);
				pool[frame.DataSlotId].CopyTo(result, 0);
			}


			RWQuery query = new RWQuery(pageid, false);
			frame.ReadLowIR = isLowIRAfter;
			frame.NodeOfRead = rwlist.AddFirst(0, query);

			if (!isLowIRAfter)
				frame.NodeOfReadInHIRQueue = rwlist.AddFirst(2, query);

			Tidy();
		}

		private void Tidy()
		{
			while (true)
			{
				MultiListNode<RWQuery> node = rwlist.GetLastNode(1);
				if (node == null)
					break;

				RWFrame frame = map[node.Value.PageId];
				bool isLowIR = node.Value.IsWrite ? frame.WriteLowIR : frame.ReadLowIR;

				if (isLowIR)
					break;

				rwlist.Remove(node);
			}

			while (RLIRLength != 0 && (float)WLIRLength / RLIRLength < ratio)
			{
				MultiListNode<RWQuery> node = rwlist.Blow(0);

				if (node.Value.IsWrite)
					continue;

				RWFrame frame = map[node.Value.PageId];
				if (!frame.ReadLowIR)
					continue;

				frame.ReadLowIR = false;
				rwlist.AddFirst(2, node.Value);
			}
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



		private class RWFrame : FrameWithRWInfo<MultiListNode<RWQuery>>
		{
			public RWFrame(uint id) : base(id) { Init(); }
			public RWFrame(uint id, int slotid) : base(id, slotid) { Init(); }

			private void Init()
			{
				ReadLowIR = false;
				WriteLowIR = false;
				NodeOfRead = null;
				NodeOfWrite = null;
				NodeOfReadInHIRQueue = null;
				NodeOfWriteInHIRQueue = null;
			}

			public bool ReadLowIR { get; set; }
			public bool WriteLowIR { get; set; }

			public MultiListNode<RWQuery> NodeOfReadInHIRQueue { get; set; }
			public MultiListNode<RWQuery> NodeOfWriteInHIRQueue { get; set; }
		}

	}
}
