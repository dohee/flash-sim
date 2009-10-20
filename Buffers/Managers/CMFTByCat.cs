using System;
using System.Collections.Generic;
using Buffers.Memory;
using Buffers.Queues;

namespace Buffers.Managers
{
	public class CMFTByCat:FrameBasedManager
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

			//在其中选出权重最低的页面
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

			//释放页面
			WriteIfDirty(minFrame);
			pool.FreeSlot(minFrame.DataSlotId);
			minFrame.DataSlotId = -1;
		}

		protected override QueueNode OnHit(QueueNode node, bool isWrite)
		{
			throw new NotImplementedException();
		}

		protected override QueueNode OnMiss(IFrame allocatedFrame, bool isWrite)
		{
			irrQ.Enqueue(allocatedFrame.Id, isWrite);
			//FIXME 这里没有设置 IRR 等？
			return fifoQ.Enqueue(allocatedFrame);
		}

		protected override void DoFlush()
		{
			base.DoFlush();
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
				int pos = q.IndexOf(new KeyValuePair<uint, bool>(pageid, dirty));

				if (pos == -1)
					return 0;

				q.RemoveAt(pos);
				q.Add(new KeyValuePair<uint, bool>(pageid, dirty));
				return (uint)pos + 1;
			}

		}
	}
}
