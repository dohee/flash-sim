using System;
using System.Collections.Generic;
using Buffers.Memory;
using Buffers.Queues;

namespace Buffers.Managers
{
	public class CMFTByCat : FrameBasedManager
	{
		private FIFOQueue fifoQ = new FIFOQueue();
		private IRRQueue irrQ = new IRRQueue();

		public CMFTByCat(uint npages)
			: this(null, npages) { }
		public CMFTByCat(IBlockDevice dev, uint npages)
			: base(dev, npages) { }


		protected override IFrame CreateFrame(uint pageid, int slotid)
		{
			return new IRRFrame(pageid, slotid);
		}

		protected override void OnPoolFull()
		{
			var residentList = new List<IRRFrame>();

			for (int i = 0; i < irrQ.Count; i++)
			{
				var pair = irrQ[i];
				IRRFrame f = map[pair.Key].ListNode.Value as IRRFrame;

				if (!f.Resident)
					continue;

				residentList.Add(f);

				if (pair.Value)
					f.WriteRecency = (uint)(irrQ.Count - i);
				else
					f.ReadRecency = (uint)(irrQ.Count - i);
			}

			double minPower = Double.MaxValue;
			IRRFrame minFrame = null;

			foreach (var f in residentList)
			{
				double power = f.GetPower();
				if (power < minPower)
				{
					minFrame = f;
					minPower = power;
				}
			}

			WriteIfDirty(minFrame);
			pool.FreeSlot(minFrame.DataSlotId);
			minFrame.DataSlotId = -1;
		}

		protected override QueueNode OnHit(QueueNode node, bool isWrite)
		{
			IRRFrame irrf = node.ListNode.Value as IRRFrame;
			uint irr = irrQ.AccessIRR(irrf.Id, isWrite);

			if (isWrite)
				irrf.WriteIRR = irr;
			else
				irrf.ReadIRR = irr;

			if (irr == 0)
				irrQ.Enqueue(irrf.Id, isWrite);

			if (!irrf.Resident)
			{
				irrf.DataSlotId = pool.AllocSlot();
				if (!isWrite)
					dev.Read(irrf.Id, pool[irrf.DataSlotId]);
			}

			return node;
		}

		protected override QueueNode OnMiss(IFrame allocatedFrame, bool isWrite)
		{
			irrQ.Enqueue(allocatedFrame.Id, isWrite);
			//FIXME 这里没有设置 IRR 等？
			return fifoQ.Enqueue(allocatedFrame);
		}

		protected override void DoFlush()
		{
			//FIXME do flush
			base.DoFlush();
		}



		private struct SkeletalFrame : IEquatable<SkeletalFrame>
		{
			public readonly uint Id;
			public readonly bool Dirty;

			public SkeletalFrame(uint id, bool dirty)
			{
				Id = id;
				Dirty = dirty;
			}

			public bool Equals(SkeletalFrame other)
			{
				return Id == other.Id && Dirty == other.Dirty;
			}
			public override bool Equals(object obj)
			{
				if (obj == null || this.GetType() != obj.GetType())
					return false;

				return Equals((SkeletalFrame)obj);
			}
			public override int GetHashCode()
			{
				return Id.GetHashCode() ^ Dirty.GetHashCode();
			}
			//public static 
			//XXXXX
		}

		private class IRRQueue
		{
			List<KeyValuePair<uint, bool>> q = new List<KeyValuePair<uint, bool>>();

			public int Count { get { return q.Count; } }
			public KeyValuePair<uint, bool> this[int index] { get { return q[index]; } }

			public void Enqueue(uint pageid, bool dirty)
			{
				q.Add(new KeyValuePair<uint, bool>(pageid, dirty));
			}
			public void Dequeue(out uint pageid, out bool dirty)
			{
				var pair = q[0];
				pageid = pair.Key;
				dirty = pair.Value;
				q.RemoveAt(0);
			}
			public uint AccessIRR(uint pageid, bool dirty)
			{
				int pos = q.LastIndexOf(new KeyValuePair<uint, bool>(pageid, dirty));

				if (pos == -1)
					return 0;

				q.RemoveAt(pos);
				q.Add(new KeyValuePair<uint, bool>(pageid, dirty));
				return (uint)pos + 1;
			}

		}
	}
}
