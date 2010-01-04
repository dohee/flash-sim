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
		private readonly float ratioOfWriteRead;
		private readonly uint nHIRPages;
		private readonly Pool pool;
		private readonly MultiList<RWQuery> rwlist;
		private readonly LinkedList<uint> hirPages = new LinkedList<uint>();
		private readonly IDictionary<uint, RWFrame> map = new Dictionary<uint, RWFrame>();

		public FLIRS(IBlockDevice dev, uint npages, float WRRatio, float HIRRatio)
			: base(dev)
		{
			ratioOfWriteRead = WRRatio;
			nHIRPages = (uint)(HIRRatio * npages);
			pool = new Pool(npages, this.dev.PageSize, OnPoolFull);
			rwlist = new MultiList<RWQuery>(2);
			rwlist.SetConcat(0, 1);
		}

		private int RLIRLength { get { return rwlist.GetNodeCount(0); } }
		private int WLIRLength { get { return rwlist.GetNodeCount(0) + rwlist.GetNodeCount(1); } }
		private int HIRQueueLength { get { return rwlist.GetNodeCount(2); } }




		private void OnPoolFull()
		{

		}

		protected sealed override void DoRead(uint pageid, byte[] result)
		{
			RWFrame frame = null;
			bool isLowIRAfter = false;

			if (!map.TryGetValue(pageid, out frame))
			{
				frame = new RWFrame(pageid);
				map[pageid] = frame;
			}

			if (frame.NodeOfRead != null)
			{
				rwlist.Remove(frame.NodeOfRead);
				frame.NodeOfRead = null;
				isLowIRAfter = true;
			}

			if (frame.NodeOfHIRPage != null)
			{
				hirPages.Remove(frame.NodeOfHIRPage);
				frame.NodeOfHIRPage = null;
			}

			if (!frame.Resident)
			{
				frame.DataSlotId = pool.AllocSlot();
				dev.Read(pageid, pool[frame.DataSlotId]);
				pool[frame.DataSlotId].CopyTo(result, 0);
			}


			frame.ReadLowIR = isLowIRAfter;
			frame.NodeOfRead = rwlist.AddFirst(0, new RWQuery(pageid, false));
			AssignHIRPageNode(frame);

			MaintainHIRs();
		}

		private void AssignHIRPageNode(RWFrame frame)
		{
			Debug.Assert(frame.NodeOfHIRPage == null);
			bool assign = false;

			if (!frame.Dirty)
			{
				if (!frame.ReadLowIR)
					assign = true;
			}
			else
			{
				if (!frame.ReadLowIR && !frame.WriteLowIR)
					assign = true;
			}

			if (assign)
				frame.NodeOfHIRPage = hirPages.AddFirst(frame.Id);
		}

		private void MaintainHIRs()
		{
			while (hirPages.Count < nHIRPages)
			{
				if (RLIRLength * ratioOfWriteRead > WLIRLength)
					ShrinkRLIRArea();
				else
					ShrinkWLIRArea();
			}

		}

		private void ShrinkRLIRArea()
		{
			MultiListNode<RWQuery> node = rwlist.Blow(0);
			if (!node.Value.IsWrite)
			{
				RWFrame frame = map[node.Value.PageId];
				if (frame.ReadLowIR)
				{
					frame.ReadLowIR = false;
					AssignHIRPageNode(frame);
				}
			}
		}

		private void ShrinkWLIRArea()
		{
			RWQuery query = rwlist.RemoveLast(1);
			RWFrame frame = map[query.PageId];

			if (query.IsWrite)
			{
				frame.NodeOfWrite = null;
				frame.WriteLowIR = false;
			}
			else
			{
				frame.NodeOfRead = null;
				frame.ReadLowIR = false;
			}

			if (frame.NodeOfHIRPage == null)
				AssignHIRPageNode(frame);

			if (frame.NodeOfHIRPage == null && frame.NodeOfRead == null && frame.NodeOfWrite == null)
			{
				WriteIfDirty(frame);
				map.Remove(frame.Id);
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
				NodeOfHIRPage = null;
			}

			public bool ReadLowIR { get; set; }
			public bool WriteLowIR { get; set; }
			public LinkedListNode<uint> NodeOfHIRPage { get; set; }
		}

	}
}
