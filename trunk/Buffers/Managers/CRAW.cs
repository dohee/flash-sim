using System;
using System.Collections.Generic;
using System.Diagnostics;
using Buffers;
using Buffers.Memory;
using Buffers.Lists;
using Buffers.Utilities;

namespace Buffers.Managers
{
	public sealed class CRAW : BufferManagerBase
	{
		private readonly float ratioOfWriteRead;
		private readonly float rplus, wplus;
		private float rrlimit_;
		private readonly MultiList<uint> rwlist;
		private readonly IDictionary<uint, RWFrame> map = new Dictionary<uint, RWFrame>();

		public CRAW(uint npages, float WRRatio)
			: this(null, npages,WRRatio) { }
		public CRAW(IBlockDevice dev, uint npages, float WRRatio)
			: base(dev, npages)
		{
			rwlist = new MultiList<uint>(4);
			rwlist.SetConcat(0, 1);
			rwlist.SetConcat(2, 3);

			ratioOfWriteRead = WRRatio;
			rrlimit_ = 1 / (1 + WRRatio);

			if (WRRatio > 1)
			{
				rplus = 0.1f;
				wplus = 0.1f * WRRatio;
			}
			else
			{
				rplus = 0.1f / WRRatio;
				wplus = 0.1f;
			}
		}

		public override string Description
		{
			get
			{
				return Utils.FormatDesc("NPages", pool.NPages,
					"WRRatio", ratioOfWriteRead.ToString("0.##"));
			}
		}

		private uint ReadRealLimit { get { return (uint)rrlimit_; } }
		private uint WriteRealLimit { get { return pool.NPages - ReadRealLimit; } }


		protected override void DoAccess(uint pageid, byte[] resultOrData, AccessType type)
		{
			RWFrame frame;
			if (!map.TryGetValue(pageid, out frame))
			{
				frame = new RWFrame(pageid);
				frame.ReadBit = false;
				frame.WriteBit = false;
				map[pageid] = frame;
			}

			int realArea = (type == AccessType.Read ? 0 : 2);
			int ghostArea = (type == AccessType.Read ? 1 : 3);

			if (frame.Resident)
			{
				PerformAccess(frame, resultOrData, type);
				frame.SetBitOf(type, true);
			}
			else
			{
				if (frame.GetNodeOf(type) != null)
				{
					rwlist.Remove(frame.GetNodeOf(type));

					if (type == AccessType.Read)
						rrlimit_ = Math.Min(rrlimit_ + rplus, pool.NPages);
					else
						rrlimit_ = Math.Max(rrlimit_ - wplus, 0);	
				}

				PerformAccess(frame, resultOrData, type);
				frame.SetNodeOf(type, rwlist.AddFirst(realArea, pageid));
				frame.SetBitOf(type, false);
				AdjustGhostSize(type == AccessType.Write);
			}
		}

		protected override void OnPoolFull()
		{
			float r = rwlist.GetNodeCount(0) / rrlimit_;
			float w = rwlist.GetNodeCount(2) / (pool.NPages - rrlimit_);

			if (r > w)
				ShrinkGhost(false);
			else
				ShrinkGhost(true);
		}

		protected override void DoFlush()
		{
			foreach (IFrame frame in map.Values)
				WriteIfDirty(frame);
		}


		private void GetSN(bool writeList,
			out int thisReal, out int thisGhost, out int anotherReal, out int anotherGhost,
			out AccessType thisType, out AccessType anotherType)
		{
			if (!writeList)
			{
				thisReal = 0; thisGhost = 1; anotherReal = 2; anotherGhost = 3;
				thisType = AccessType.Read; anotherType = AccessType.Write;
			}
			else
			{
				thisReal = 2; thisGhost = 3; anotherReal = 0; anotherGhost = 1;
				thisType = AccessType.Write; anotherType = AccessType.Read;
			}
		}

		private void AdjustGhostSize(bool writeList)
		{
			int thisReal, thisGhost, anotherReal, anotherGhost;
			AccessType thisType, anotherType;
			GetSN(writeList, out thisReal, out thisGhost, out anotherReal, out anotherGhost,
				out thisType, out anotherType);

			while (rwlist.GetNodeCountSum(thisReal, thisGhost) > pool.NPages &&
				rwlist.GetNodeCount(thisGhost) > 0)
			{
				uint pageid = rwlist.RemoveLast(thisGhost);
				RWFrame frame = map[pageid];
				frame.SetNodeOf(thisType, null);

				if (frame.NodeOfRead == null && frame.NodeOfWrite == null)
					map.Remove(pageid);
			}
		}

		private void ShrinkGhost(bool writeList)
		{
			int thisReal, thisGhost, anotherReal, anotherGhost;
			AccessType thisType, anotherType;
			GetSN(writeList, out thisReal, out thisGhost, out anotherReal, out anotherGhost,
				out thisType, out anotherType);

			while (true)
			{
				uint pageid = rwlist.RemoveLast(thisReal);
				RWFrame frame = map[pageid];

				if (frame.GetBitOf(thisType))
				{
					frame.SetNodeOf(thisType, rwlist.AddFirst(thisReal, pageid));
					frame.SetBitOf(thisType, false);
				}
				else
				{
					frame.SetNodeOf(thisType, rwlist.AddFirst(thisGhost, pageid));

					if (frame.GetNodeOf(anotherType) == null ||
						frame.GetNodeOf(anotherType).ListIndex != anotherReal)
					{
						WriteIfDirty(frame);
						pool.FreeSlot(frame.DataSlotId);
						frame.DataSlotId = -1;
						return;
					}
				}
			}

		}


		private class RWFrame : FrameWithRWInfo<MultiListNode<uint>>
		{
			public RWFrame(uint id) : base(id) { InitRWFrame(); }
			public RWFrame(uint id, int slotid) : base(id, slotid) { InitRWFrame(); }

			private void InitRWFrame()
			{
				ReadBit = false;
				WriteBit = false;
			}

			public bool ReadBit { get; set; }
			public bool WriteBit { get; set; }

			public bool GetBitOf(AccessType type)
			{
				return type == AccessType.Read ? ReadBit : WriteBit;
			}
			public void SetBitOf(AccessType type, bool value)
			{
				if (type == AccessType.Read)
					ReadBit = value;
				else
					WriteBit = value;
			}
		}
	}
}