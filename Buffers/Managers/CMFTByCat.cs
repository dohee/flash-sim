using System;
using System.Collections.Generic;
using Buffers.Memory;
using Buffers.Queues;

namespace Buffers.Managers
{
	public class CMFTByCat : FrameBasedManager
	{
		private FIFOQueue<IFrame> fifoQ = new FIFOQueue<IFrame>();
		private IRRQueue irrQ = new IRRQueue();

		public CMFTByCat(uint npages)
			: this(null, npages) { }
		public CMFTByCat(IBlockDevice dev, uint npages)
			: base(dev, npages) { }

		public override string Name { get { return "CMFT"; } }


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
				IRRFrame f = map[pair.PageId].ListNode.Value as IRRFrame;

				if (!f.Resident)
					continue;

				residentList.Add(f);

				if (pair.IsWrite)
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

		protected override QueueNode<IFrame> OnHit(QueueNode<IFrame> node, bool isWrite)
		{
			IRRFrame irrf = node.ListNode.Value as IRRFrame;
			if (!irrf.Resident)
			{
				irrf.DataSlotId = pool.AllocSlot();
				if (!isWrite)
					dev.Read(irrf.Id, pool[irrf.DataSlotId]);
			}

			uint irr = irrQ.AccessIRR(irrf.Id, isWrite);

			if (isWrite)
				irrf.WriteIRR = irr;
			else
				irrf.ReadIRR = irr;

			if (irr == 0)
				irrQ.Enqueue(irrf.Id, isWrite);

			return node;
		}

		protected override QueueNode<IFrame> OnMiss(IFrame allocatedFrame, bool isWrite)
		{
			irrQ.Enqueue(allocatedFrame.Id, isWrite);
			return fifoQ.Enqueue(allocatedFrame);
		}

		protected override void DoFlush()
		{
			//FIXME do flush
			base.DoFlush();
		}


		private class IRRQueue
		{
			List<RWQuery> q = new List<RWQuery>();

			public int Count { get { return q.Count; } }
			public RWQuery this[int index] { get { return q[index]; } }

			public void Enqueue(uint pageid, bool dirty)
			{
				q.Add(new RWQuery(pageid, dirty));
			}
			public void Dequeue(out uint pageid, out bool dirty)
			{
				var pair = q[0];
				pageid = pair.PageId;
				dirty = pair.IsWrite;
				q.RemoveAt(0);
			}
			public uint AccessIRR(uint pageid, bool dirty)
			{
				int pos = q.LastIndexOf(new RWQuery(pageid, dirty));

				if (pos == -1)
					return 0;

				q.RemoveAt(pos);
				q.Add(new RWQuery(pageid, dirty));
				return (uint)(q.Count - pos);
				//return (uint)(pos + 1);
			}

		}
	}
}
