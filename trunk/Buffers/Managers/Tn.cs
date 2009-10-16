using System;
using System.Collections.Generic;
using Buffers.Memory;
using Buffers.Queues;

namespace Buffers.Managers
{
	public class Tn : FrameBasedManager
	{
		private readonly MultiConcatLRUQueue q;
		private readonly uint kickRatio;
		private readonly bool adjustDROnReadDR, enlargeCROnReadDNR;

		private uint crlimit_;
		private readonly uint SRLimit;
		private readonly uint CNRLimit, DNRLimit, SNRLimit;


		public Tn(uint npages, uint HowManyToKickWhenWriteInDR, uint SRLimit)
			: this(null, npages, HowManyToKickWhenWriteInDR, SRLimit) { }

		public Tn(IBlockDevice dev, uint npages, uint HowManyToKickWhenWriteInDR, uint SRLimit)
			: this(dev, npages, HowManyToKickWhenWriteInDR, SRLimit, false, false) { }

		public Tn(uint npages, uint HowManyToKickWhenWriteInDR, uint SRLimit,
			bool AdjustDRWhenReadInDR, bool EnlargeCRWhenReadInDNR)
			: this(null, npages, HowManyToKickWhenWriteInDR, SRLimit,
			AdjustDRWhenReadInDR, EnlargeCRWhenReadInDNR) { }

		public Tn(IBlockDevice dev, uint npages, uint HowManyToKickWhenWriteInDR, uint SRLimit,
			bool AdjustDRWhenReadInDR, bool EnlargeCRWhenReadInDNR)
			: base(dev, npages)
		{
			q = new MultiConcatLRUQueue(new ConcatenatedLRUQueue[] {
				new ConcatenatedLRUQueue(new FIFOQueue(), new FIFOQueue()),
				new ConcatenatedLRUQueue(new FIFOQueue(), new FIFOQueue()),
				new ConcatenatedLRUQueue(new FIFOQueue(), new FIFOQueue())
			});

			kickRatio = HowManyToKickWhenWriteInDR;
			this.SRLimit = SRLimit;
			adjustDROnReadDR = AdjustDRWhenReadInDR;
			enlargeCROnReadDNR = EnlargeCRWhenReadInDNR;

			crlimit_ = CNRLimit = DNRLimit = SNRLimit = npages / 2;
		}


		private uint CRLimit { get { return crlimit_; } }
		private uint DRLimit { get { return pool.NPages - crlimit_ - SRLimit; } }

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
			bool crOverrun = (q.GetFrontSize(0) > CRLimit);
			bool drOverrun = (q.GetFrontSize(1) > DRLimit);
			bool srOverrun = (q.GetFrontSize(2) > SRLimit);

			if (drOverrun)
				qn = q.BlowOneFrame(1);
			else if (srOverrun)
				qn = q.BlowOneFrame(2);
			else
				qn = q.BlowOneFrame(0);

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
				if (adjustDROnReadDR)
					return q.AccessFrame(node);
				else
					return node;
			}
			else if (inDirty && isRead && !resident)
			{
				if (enlargeCROnReadDNR)
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
					EnlargeCRLimit(-(int)kickRatio);
					node.ListNode.Value.DataSlotId = pool.AllocSlot();
				}
				return node;
			}
			else if (inSingle && resident)
			{
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
	}
}
