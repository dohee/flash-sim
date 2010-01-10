using System;
using System.Collections.Generic;
using System.Diagnostics;
using Buffers;
using Buffers.Memory;
using Buffers.Queues;
using Buffers.Lists;

namespace Buffers.Managers
{
	public sealed class FLIRS : BufferManagerBase
	{
		private readonly float ratioOfWriteRead;
		private readonly uint nHIRPages;
		private readonly MultiList<RWQuery> rwlist;
		private readonly LinkedList<uint> hirPages = new LinkedList<uint>();
		private readonly IDictionary<uint, RWFrame> map = new Dictionary<uint, RWFrame>();

		public FLIRS(IBlockDevice dev, uint npages, float WRRatio, float HIRRatio)
			: base(dev)
		{
			ratioOfWriteRead = WRRatio;
			nHIRPages = (uint)(HIRRatio * npages);
			rwlist = new MultiList<RWQuery>(2);
			rwlist.SetConcat(0, 1);
		}

		private int RLIRLength { get { return rwlist.GetNodeCount(0); } }
		private int WLIRLength { get { return rwlist.GetNodeCount(0) + rwlist.GetNodeCount(1); } }
		private int HIRQueueLength { get { return rwlist.GetNodeCount(2); } }




		protected override void OnPoolFull()
		{

		}

		protected sealed override void DoAccess(uint pageid, byte[] resultOrData, AccessType type)
		{
			RWFrame frame = null;
			bool isLowIRAfter = false;

			if (!map.TryGetValue(pageid, out frame))
			{
				frame = new RWFrame(pageid);
				map[pageid] = frame;
			}

			if (frame.GetNodeOf(type) != null)
			{
				rwlist.Remove(frame.GetNodeOf(type));
				frame.SetNodeOf(type, null);
				isLowIRAfter = true;
			}

			if (frame.NodeOfHIRPage != null)
			{
				hirPages.Remove(frame.NodeOfHIRPage);
				frame.NodeOfHIRPage = null;
			}


			PerformAccess(frame, resultOrData, type);

			frame.SetLowIROf(type, isLowIRAfter);
			frame.SetNodeOf(type, rwlist.AddFirst(0, new RWQuery(pageid, type)));
			TryAssignHIRPageNode(frame);

			MaintainHIRs();
		}

		private void TryAssignHIRPageNode(RWFrame frame)
		{
			Debug.Assert(frame.NodeOfHIRPage == null);

			if ((!frame.Dirty && !frame.ReadLowIR) ||
				(frame.Dirty && !frame.ReadLowIR && !frame.WriteLowIR))
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
			if (node.Value.Type == AccessType.Read)
			{
				RWFrame frame = map[node.Value.PageId];
				if (frame.ReadLowIR)
				{
					frame.ReadLowIR = false;
					TryAssignHIRPageNode(frame);
				}
			}
		}

		private void ShrinkWLIRArea()
		{
			RWQuery query = rwlist.RemoveLast(1);
			RWFrame frame = map[query.PageId];

			frame.SetNodeOf(query.Type, null);
            frame.SetLowIROf(query.Type, false);

			if (frame.NodeOfHIRPage == null)
				TryAssignHIRPageNode(frame);

			if (frame.NodeOfHIRPage == null && frame.NodeOfRead == null && frame.NodeOfWrite == null)
			{
				WriteIfDirty(frame);
				if (frame.Resident)
					pool.FreeSlot(frame.DataSlotId);
				map.Remove(frame.Id);
			}
		}


		protected override void DoFlush()
		{
		}



		private class RWFrame : FrameWithRWInfo<MultiListNode<RWQuery>>
		{
            public RWFrame(uint id) : base(id) { InitRWFrame(); }
            public RWFrame(uint id, int slotid) : base(id, slotid) { InitRWFrame(); }

			private void InitRWFrame()
			{
				NodeOfHIRPage = null;
				ReadLowIR = false;
				WriteLowIR = false;
			}

			public LinkedListNode<uint> NodeOfHIRPage { get; set; }
			public bool ReadLowIR { get; set; }
			public bool WriteLowIR { get; set; }

			public bool GetLowIROf(AccessType type)
			{
				return type == AccessType.Read ? ReadLowIR : WriteLowIR;
			}
			public void SetLowIROf(AccessType type, bool value)
			{
				if (type == AccessType.Read)
					ReadLowIR = value;
				else
					WriteLowIR = value;
			}
		}

	}
}
