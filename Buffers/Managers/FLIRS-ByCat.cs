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

		protected sealed override void DoAccess(uint pageid, byte[] dataOrResult, AccessType type)
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

			if (!frame.Resident)
			{
				frame.DataSlotId = pool.AllocSlot();
				if (type == AccessType.Read)
					dev.Read(pageid, pool[frame.DataSlotId]);
			}

			if (type == AccessType.Read)
			{
				pool[frame.DataSlotId].CopyTo(dataOrResult, 0);
			}
			else
			{
				dataOrResult.CopyTo(pool[frame.DataSlotId], 0);
				frame.Dirty = true;
			}

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

			if (query.Type== AccessType.Read)
			{
				frame.NodeOfRead = null;
				frame.ReadLowIR = false;
			}
			else
			{
				frame.NodeOfWrite = null;
				frame.WriteLowIR = false;
			}

			if (frame.NodeOfHIRPage == null)
				TryAssignHIRPageNode(frame);

			if (frame.NodeOfHIRPage == null && frame.NodeOfRead == null && frame.NodeOfWrite == null)
			{
				WriteIfDirty(frame);
				map.Remove(frame.Id);
			}
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
				NodeOfRead = null;
				NodeOfWrite = null;
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
