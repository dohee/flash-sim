using System;
using System.Collections.Generic;
using Buffers.Memory;
using Buffers.Queues;

namespace Buffers.Managers
{
	public struct TnConfig
	{
		public bool AdjustDRWhenReadInDR;
		public bool EnlargeCRWhenReadInDNR;
		public uint SRLimit;
		public uint SNRLimit;
		public bool PickOffSRWhenHitInSR;

		public TnConfig(bool AdjustDRWhenReadInDR, bool EnlargeCRWhenReadInDNR,
			uint SRLimit, uint SNRLimit, bool PickOffSRWhenHitInSR)
		{
			this.AdjustDRWhenReadInDR = AdjustDRWhenReadInDR;
			this.EnlargeCRWhenReadInDNR = EnlargeCRWhenReadInDNR;
			this.SRLimit = SRLimit;
			this.SNRLimit = SNRLimit;
			this.PickOffSRWhenHitInSR = PickOffSRWhenHitInSR;
		}
	}


	public class Tn : FrameBasedManager
	{
		private readonly MultiConcatLRUQueue q;

		private readonly TnConfig conf;
		private readonly uint kickn;
		private uint crlimit_;
		private readonly uint CNRLimit, DNRLimit;


		public Tn(uint npages, uint kickN)
			: this(null, npages, kickN) { }

		public Tn(IBlockDevice dev, uint npages, uint kickN)
			: this(dev, npages, kickN, new TnConfig()) { }

		public Tn(uint npages, uint kickN, TnConfig conf)
			: this(null, npages, kickN, conf) { }

		public Tn(IBlockDevice dev, uint npages, uint kickN, TnConfig conf)
			: base(dev, npages)
		{
			q = new MultiConcatLRUQueue(new ConcatenatedLRUQueue[] {
				new ConcatenatedLRUQueue(new FIFOQueue(), new FIFOQueue()),
				new ConcatenatedLRUQueue(new FIFOQueue(), new FIFOQueue()),
				new ConcatenatedLRUQueue(new FIFOQueue(), new FIFOQueue())
			});

			this.conf = conf;
			this.kickn = kickN;

			crlimit_ = CNRLimit = DNRLimit = npages / 2;
		}

		public override string Description
		{
			get
			{
				return string.Format(
					"Tn (NPages={0},KickN={1},AdjustDR={2},EnlargeCR={3},SRLimit={4},SNRLimit={5},KickOffSR={6})",
					pool.NPages, kickn,
					conf.AdjustDRWhenReadInDR ? 1 : 0,
					conf.EnlargeCRWhenReadInDNR ? 1 : 0,
					conf.SRLimit, conf.SNRLimit,
					conf.PickOffSRWhenHitInSR ? 1 : 0);
			}
		}


		private uint CRLimit { get { return crlimit_; } }
		private uint DRLimit { get { return pool.NPages - crlimit_ - SRLimit; } }
		private uint SRLimit { get { return conf.SRLimit; } }
		private uint SNRLimit { get { return conf.SNRLimit; } }

		private void EnlargeCRLimit(int relativeAmount)
		{
			int cr = (int)crlimit_ + relativeAmount;
			cr = Math.Max(cr, 0);
			cr = Math.Min(cr, (int)(pool.NPages - SRLimit));
			crlimit_ = (uint)cr;
			//Console.WriteLine("CurrentCRLimit: " + cr);
		}


		protected override void OnPoolFull()
		{
			QueueNode qn;
			if (q.GetFrontSize(0) > CRLimit) qn = q.BlowOneFrame(0);
			else if (q.GetFrontSize(1) > DRLimit) qn = q.BlowOneFrame(1);
			else if (q.GetFrontSize(2) > SRLimit) qn = q.BlowOneFrame(2);
			else if (q.GetFrontSize(0) != 0) qn = q.BlowOneFrame(0);
			else if (q.GetFrontSize(1) != 0) qn = q.BlowOneFrame(1);
			else qn = q.BlowOneFrame(2);

			IFrame f = qn.ListNode.Value;
			WriteIfDirty(f);
			pool.FreeSlot(f.DataSlotId);
			f.DataSlotId = -1;
			map[f.Id] = qn;

			if (q.GetBackSize(0) > CNRLimit)
				map.Remove(q.Dequeue(0).Id);
			if (q.GetBackSize(1) > DNRLimit)
				map.Remove(q.Dequeue(1).Id);
			if (q.GetBackSize(2) > SNRLimit)
				map.Remove(q.Dequeue(2).Id);			
		}

		protected override QueueNode OnHit(QueueNode node, bool isWrite)
		{
			bool isRead = !isWrite;
			bool resident = node.ListNode.Value.Resident;
			int inwhichqueue = q.InWhichQueue(node);
			bool inClean = (inwhichqueue == 0);
			bool inDirty = (inwhichqueue == 1);
			bool inSingle = (inwhichqueue == 2);

			if (inClean && isRead)
			{
				node = q.AccessFrame(node);
				if (!resident)
				{
					EnlargeCRLimit(1);
					IFrame f = node.ListNode.Value;
					f.DataSlotId = pool.AllocSlot();
					dev.Read(f.Id, pool[f.DataSlotId]);
				}
				return node;
			}
			else if (inClean && isWrite)
			{
				IFrame f = q.Dequeue(node);
				if (!resident)
					f.DataSlotId = pool.AllocSlot();
				return q.Enqueue(1, f);
			}
			else if (inDirty && isRead && resident)
			{
                //Random rand=new Random();
				if (conf.AdjustDRWhenReadInDR)
					return q.AccessFrame(node);
				else
					return node;
			}
			else if (inDirty && isRead && !resident)
			{
				if (conf.EnlargeCRWhenReadInDNR)
					EnlargeCRLimit(1);
				IFrame f = q.Dequeue(node);
				f.DataSlotId = pool.AllocSlot();
				dev.Read(f.Id, pool[f.DataSlotId]);
				return q.Enqueue(0, f);
			}
			else if (inDirty && isWrite)
			{
				node = q.AccessFrame(node);
				if (!resident)
				{
					EnlargeCRLimit(-(int)kickn);
					node.ListNode.Value.DataSlotId = pool.AllocSlot();
				}
				return node;
			}
			else if (inSingle && resident)
			{
				if (conf.PickOffSRWhenHitInSR)
					return q.Enqueue((isRead ? 0 : 1), q.Dequeue(node));
				else
					return node;
			}
			else if (inSingle && !resident)
			{
				IFrame f = q.Dequeue(node);
				f.DataSlotId = pool.AllocSlot();
				if (isRead)
					dev.Read(f.Id, pool[f.DataSlotId]);
				return q.Enqueue((isRead ? 0 : 1), f);
			}
			else
			{
				throw new Exception("Should not come here.");
			}
		}

		protected override QueueNode OnMiss(IFrame allocatedFrame, bool isWrite)
		{
			if (SRLimit == 0)
			{
				if (isWrite)
					return q.Enqueue(1, allocatedFrame);
				else
					return q.Enqueue(0, allocatedFrame);
			}
			else
			{
				return q.Enqueue(2, allocatedFrame);
			}
		}

		protected override void DoFlush()
		{
			foreach (var mapitem in map)
			{
				IFrame f = mapitem.Value.ListNode.Value;
				if (!f.Dirty)
					continue;

				dev.Write(f.Id, pool[f.DataSlotId]);
				f.Dirty = false;

				//if (q.InWhichQueue(mapitem.Value) == 1)
				//	map[f.Id] = q.Enqueue(0, q.Dequeue(mapitem.Value));
			}
		}
	}
}
