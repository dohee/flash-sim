using System;
using System.Collections.Generic;
using Buffers.Memory;
using Buffers.Queues;
using Buffers.Utilities;

namespace Buffers.Managers
{

#pragma warning disable 0618
	[Obsolete("请使用 Tn")]
	public sealed class OldTn : FrameBasedManager
#pragma warning restore 0618
	{
		private readonly MultiConcatLRUQueue<IFrame> q;

		private readonly TnConfig conf;
		private readonly float kickn;
		private float crlimit_;
		private readonly uint CNRLimit, DNRLimit, SRLimit, SNRLimit;


		public OldTn(uint npages, float kickN)
			: this(null, npages, kickN) { }

		public OldTn(IBlockDevice dev, uint npages, float kickN)
			: this(dev, npages, kickN, new TnConfig()) { }

		public OldTn(uint npages, float kickN, TnConfig conf)
			: this(null, npages, kickN, conf) { }

		public OldTn(IBlockDevice dev, uint npages, float kickN, TnConfig conf)
			: base(dev, npages)
		{
			q = new MultiConcatLRUQueue<IFrame>(new ConcatenatedLRUQueue<IFrame>[] {
				new ConcatenatedLRUQueue<IFrame>(
					new FIFOQueue<IFrame>(), new FIFOQueue<IFrame>()),
				new ConcatenatedLRUQueue<IFrame>(
					new FIFOQueue<IFrame>(), new FIFOQueue<IFrame>()),
				new ConcatenatedLRUQueue<IFrame>(
					new FIFOQueue<IFrame>(), new FIFOQueue<IFrame>())
			});

			if (conf.CNRLimitRatio == 0.0)
				conf.CNRLimitRatio = 0.5f;
			if (conf.DNRLimitRatio == 0.0)
				conf.DNRLimitRatio = 0.5f;

			CNRLimit = (uint)(npages * conf.CNRLimitRatio);
			DNRLimit = (uint)(npages * conf.DNRLimitRatio);
			SRLimit = (uint)(npages * conf.SRLimitRatio);
			SNRLimit = (uint)(npages * conf.SNRLimitRatio);
			crlimit_ = (float)(npages / 2.0);

			this.conf = conf;
			this.kickn = kickN;
		}

		public override string Description
		{
			get
			{
				return Utils.FormatDescription("NPages", pool.NPages,
					"KickN", kickn.ToString("0.##"),
					"AdjustDR", conf.AdjustDRWhenReadInDR ? 1 : 0,
					"EnlargeCR", conf.EnlargeCRWhenReadInDNR ? 1 : 0,
					"KickOffSR", conf.PickOffSRWhenHitInSR ? 1 : 0,
					"CNR", conf.CNRLimitRatio.ToString("0.##"),
					"DNR", conf.DNRLimitRatio.ToString("0.##"),
					"SR", conf.SRLimitRatio.ToString("0.##"),
					"SNR", conf.SNRLimitRatio.ToString("0.##")
					);
			}
		}


		private uint CRLimit { get { return (uint)crlimit_; } }
		private uint DRLimit { get { return pool.NPages - (uint)crlimit_ - SRLimit; } }

		private void EnlargeCRLimit(float relativeAmount)
		{
			float cr = crlimit_ + relativeAmount;
			cr = Math.Max(cr, 0);
			cr = Math.Min(cr, pool.NPages - SRLimit);
			crlimit_ = cr;
			//Console.WriteLine("CurrentCRLimit: " + cr);
		}


		protected override QueueNode<IFrame> OnHit(QueueNode<IFrame> node, AccessType type)
		{
			bool isRead = (type == AccessType.Read);
			bool isWrite = (type == AccessType.Write);
			bool resident = node.ListNode.Value.Resident;
			uint inwhichqueue = q.GetRoute(node);
			bool inClean = (inwhichqueue == 0);
			bool inDirty = (inwhichqueue == 1);
			bool inSingle = (inwhichqueue == 2);

			if (inClean && isRead)
			{
				node = q.Access(node);
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
					return q.Access(node);
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
				node = q.Access(node);
				if (!resident)
				{
					EnlargeCRLimit(-kickn);
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

		protected override QueueNode<IFrame> OnMiss(IFrame allocatedFrame, AccessType type)
		{
			if (SRLimit == 0)
			{
				if (type == AccessType.Read)
					return q.Enqueue(0, allocatedFrame);
				else
					return q.Enqueue(1, allocatedFrame);
			}
			else
			{
				return q.Enqueue(2, allocatedFrame);
			}
		}

		protected override void OnPoolFull()
		{
			QueueNode<IFrame> qn;
			if (q.GetFrontSize(0) > CRLimit) qn = q.BlowOneItem(0);
			else if (q.GetFrontSize(1) > DRLimit) qn = q.BlowOneItem(1);
			else if (q.GetFrontSize(2) > SRLimit) qn = q.BlowOneItem(2);
			else if (q.GetFrontSize(0) != 0) qn = q.BlowOneItem(0);
			else if (q.GetFrontSize(1) != 0) qn = q.BlowOneItem(1);
			else qn = q.BlowOneItem(2);

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

		protected override void DoFlush()
		{
			var drpages = new List<uint>();

			foreach (var entry in map)
			{
				IFrame f = entry.Value.ListNode.Value;
				if (!f.Dirty)
					continue;

				dev.Write(f.Id, pool[f.DataSlotId]);
				f.Dirty = false;

				if (q.GetRoute(entry.Value) == 1)
					drpages.Add(entry.Key);
			}

			foreach (var pageid in drpages)
				map[pageid] = q.Enqueue(0, q.Dequeue(map[pageid]));
		}
	}
}
