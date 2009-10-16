using System;
using System.Collections.Generic;
using Buffers.Memory;
using Buffers.Queues;

namespace Buffers.Managers
{
	public class Tn : FrameBasedManager
	{
		private readonly MultiConcatLRUQueue am;
		private readonly uint kickRatio;
		private readonly bool adjustDROnReadDR, enlargeCROnReadDNR;
		private uint crlimit_;
		private readonly uint CNRLimit, DNRLimit;


		public Tn(uint npages, uint HowManyToKickWhenWriteInDR)
			: this(null, npages, HowManyToKickWhenWriteInDR) { }

		public Tn(uint npages, uint HowManyToKickWhenWriteInDR,
			bool AdjustDRWhenReadInDR, bool EnlargeCRWhenReadInDNR)
			: this(null, npages, HowManyToKickWhenWriteInDR,
			AdjustDRWhenReadInDR, EnlargeCRWhenReadInDNR) { }

		public Tn(IBlockDevice dev, uint npages, uint HowManyToKickWhenWriteInDR)
			: this(dev, npages, HowManyToKickWhenWriteInDR, false, false) { }

		public Tn(IBlockDevice dev, uint npages, uint HowManyToKickWhenWriteInDR,
			bool AdjustDRWhenReadInDR, bool EnlargeCRWhenReadInDNR)
			: base(dev, npages)
		{
			am = new MultiConcatLRUQueue(new ConcatenatedLRUQueue[] {
				new ConcatenatedLRUQueue(new FIFOQueue(), new FIFOQueue()),
				new ConcatenatedLRUQueue(new FIFOQueue(), new FIFOQueue())
			});

			crlimit_ = CNRLimit = DNRLimit = npages / 2;

			kickRatio = HowManyToKickWhenWriteInDR;
			adjustDROnReadDR = AdjustDRWhenReadInDR;
			enlargeCROnReadDNR = EnlargeCRWhenReadInDNR;
		}


		private uint CRLimit { get { return crlimit_; } }
		private uint DRLimit { get { return pool.NPages - crlimit_; } }

		private void EnlargeCRLimit(int relativeAmount)
		{
			int cr = (int)crlimit_ + relativeAmount;
			cr = Math.Max(cr, 0);
			cr = Math.Min(cr, (int)pool.NPages);
			crlimit_ = (uint)cr;
			//Console.WriteLine("CurrentCRLimit: " + cr);
		}


		protected override void OnPoolFull()
		{
			bool crOverrun = (am.GetFrontSize(0) > CRLimit);
			bool blowCR = crOverrun || (am.GetFrontSize(1) == 0);

			QueueNode qn = blowCR ? am.BlowOneFrame(0) : am.BlowOneFrame(1);
			WriteIfDirty(qn.ListNode.Value);
			pool.FreeSlot(qn.ListNode.Value.DataSlotId);
			qn.ListNode.Value.DataSlotId = -1;
			map[qn.ListNode.Value.Id] = qn;

			if (am.GetBackSize(0) > CNRLimit)
			{
				IFrame f = am.Dequeue(0);
				map.Remove(f.Id);
			}

			if (am.GetBackSize(1) > DNRLimit)
			{
				IFrame f = am.Dequeue(1);
				map.Remove(f.Id);
			}
		}

		protected override QueueNode OnHit(QueueNode node, bool isWrite)
		{
			bool isRead = !isWrite;
			bool resident = node.ListNode.Value.Resident;
			bool inDirty = (resident ? node.ListNode.Value.Dirty : am.IsInQueue(node, 1));
			bool inClean = !inDirty;

			if (inClean && isRead)
			{
				node = am.AccessFrame(node);
				if (!resident)
				{
					EnlargeCRLimit(1);
					node.ListNode.Value.DataSlotId = pool.AllocSlot();
				}
				return node;
			}
			else if (inClean && isWrite)
			{
				IFrame f = am.Dequeue(node);
				if (!resident)
					f.DataSlotId = pool.AllocSlot();
				return am.Enqueue(1, f);
			}
			else if (inDirty && isRead && resident)
			{
				if (adjustDROnReadDR)
					return am.AccessFrame(node);
				else
					return node;
			}
			else if (inDirty && isRead && !resident)
			{
				if (enlargeCROnReadDNR)
					EnlargeCRLimit(1);
				IFrame f = am.Dequeue(node);
				f.DataSlotId = pool.AllocSlot();
				return am.Enqueue(0, f);
			}
			else if (inDirty && isWrite)
			{
				node = am.AccessFrame(node);
				if (!resident)
				{
					EnlargeCRLimit(-(int)kickRatio);
					node.ListNode.Value.DataSlotId = pool.AllocSlot();
				}
				return node;
			}
			else
			{
				throw new Exception("Should not come here.");
			}
		}

		protected override QueueNode OnMiss(IFrame allocatedFrame, bool isWrite)
		{
			if (isWrite)
				return am.Enqueue(1, allocatedFrame);
			else
				return am.Enqueue(0, allocatedFrame);
		}
	}
}
