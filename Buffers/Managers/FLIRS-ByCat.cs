using System;
using System.Collections.Generic;
using System.Diagnostics;
using Buffers;
using Buffers.Memory;
using Buffers.Queues;
using Buffers.Lists;

namespace Buffers.Managers
{
	public sealed class FLIRSByCat : BufferManagerBase
	{
		private readonly float ratioOfWriteRead;
		private readonly float ratioOfHIRPage;
		private readonly uint nHIRPagesLimit;
		private readonly MultiList<RWQuery> rwlist;
		private readonly LinkedList<uint> hirPages = new LinkedList<uint>();
		private readonly IDictionary<uint, RWFrame> map = new Dictionary<uint, RWFrame>();

		public FLIRSByCat(uint npages, float WRRatio, float HIRRatio)
			: this(null, npages, WRRatio, HIRRatio) { }

		public FLIRSByCat(IBlockDevice dev, uint npages, float WRRatio, float HIRRatio)
			: base(dev, npages)
		{
			ratioOfWriteRead = WRRatio;
			ratioOfHIRPage = HIRRatio;
			nHIRPagesLimit = (uint)(HIRRatio * npages);
			rwlist = new MultiList<RWQuery>(2);
			rwlist.SetConcat(0, 1);
		}

		public override string Name { get { return "FLIRS"; } }

		public override string Description
		{
			get
			{
				return Utils.FormatDescription("By", "Cat", "NPages", pool.NPages,
					"WRRatio", ratioOfWriteRead.ToString("0.##"),
					"HIRRatio", ratioOfHIRPage.ToString("0.##"));
			}
		}

		private int RLIRLength { get { return rwlist.GetNodeCount(0); } }
		private int WLIRLength { get { return rwlist.GetNodeCount(0) + rwlist.GetNodeCount(1); } }


		protected override void OnPoolFull()
		{
			//while (true)
			{
				Debug.Assert(hirPages.Count > 0);

				uint pageid = hirPages.Last.Value;
				hirPages.RemoveLast();

				RWFrame frame = map[pageid];
				frame.NodeOfHIRPage = null;
				//要变回去就把这个注释去掉
				/*map.Remove(pageid);

				if (frame.NodeOfRead != null)
					rwlist.Remove(frame.NodeOfRead);
				if (frame.NodeOfWrite != null)
					rwlist.Remove(frame.NodeOfWrite);*/

				//if (frame.Resident)
				{
					WriteIfDirty(frame);
					pool.FreeSlot(frame.DataSlotId);
					frame.DataSlotId = -1;
					//break;
				}
			}
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

			//if ((!frame.Dirty && !frame.ReadLowIR) ||
			//(frame.Dirty && !frame.ReadLowIR && !frame.WriteLowIR))
			////////////////////////////////////////////////
			if (!frame.ReadLowIR && !frame.WriteLowIR && frame.Resident)
				////////////////////////////////////////////////
				frame.NodeOfHIRPage = hirPages.AddFirst(frame.Id);
		}

		private void MaintainHIRs()
		{
			while (pool.Full && hirPages.Count < nHIRPagesLimit)
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
			AccessType type = node.Value.Type;
			RWFrame frame = map[node.Value.PageId];
			frame.SetNodeOf(type, node);

			if (type == AccessType.Read && frame.ReadLowIR)
			{
				frame.ReadLowIR = false;
				TryAssignHIRPageNode(frame);
			}
		}

		private void ShrinkWLIRArea()
		{
			RWQuery query = rwlist.RemoveLast(1, true);
			RWFrame frame = map[query.PageId];

			frame.SetNodeOf(query.Type, null);
			//////////////////////
			if (query.Type == AccessType.Write && frame.WriteLowIR)
				///////////////////////
				frame.SetLowIROf(query.Type, false);

			if (frame.NodeOfHIRPage == null)
				TryAssignHIRPageNode(frame);

			if (frame.NodeOfHIRPage == null && frame.NodeOfRead == null && frame.NodeOfWrite == null)
			{
				//Never Come HERE
				WriteIfDirty(frame);
				if (frame.Resident)
					pool.FreeSlot(frame.DataSlotId);
				map.Remove(frame.Id);
			}
		}


		protected override void DoFlush()
		{
			foreach (IFrame frame in map.Values)
				WriteIfDirty(frame);
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
