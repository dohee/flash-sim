using System;
using System.Collections.Generic;
using Buffers.Lists;
using Buffers.Memory;
using Buffers.Utilities;

namespace Buffers.Managers
{
	public struct TnConfig
	{
		public bool AdjustDRWhenReadInDR;
		public float CNRLimitRatio;
		public float DNRLimitRatio;
		public bool EnlargeCRWhenReadInDNR;
		public bool PickOffSRWhenHitInSR;
		public float SRLimitRatio;
		public float SNRLimitRatio;

		public TnConfig(
			bool AdjustDRWhenReadInDR, bool EnlargeCRWhenReadInDNR,
			bool PickOffSRWhenHitInSR, float CNRLimitRatio, float DNRLimitRatio,
			float SRLimitRatio, float SNRLimitRatio)
		{
			this.AdjustDRWhenReadInDR = AdjustDRWhenReadInDR;
			this.CNRLimitRatio = CNRLimitRatio;
			this.DNRLimitRatio = DNRLimitRatio;
			this.EnlargeCRWhenReadInDNR = EnlargeCRWhenReadInDNR;
			this.PickOffSRWhenHitInSR = PickOffSRWhenHitInSR;
			this.SRLimitRatio = SRLimitRatio;
			this.SNRLimitRatio = SNRLimitRatio;
		}
	}

	
	public sealed class Tn : BufferManagerBase
	{
		private readonly MultiList<IFrame> q = new MultiList<IFrame>(6);
		private readonly Dictionary<uint, MultiListNode<IFrame>> map = new Dictionary<uint, MultiListNode<IFrame>>();

		private readonly TnConfig conf;
		private readonly float WRRatio, rplus, wplus;
		private float crlimit_;
		private readonly uint CNRLimit, DNRLimit, SRLimit, SNRLimit;


		public Tn(uint npages, float WRRatio)
			: this(null, npages, WRRatio) { }

		public Tn(IBlockDevice dev, uint npages, float WRRatio)
			: this(dev, npages, WRRatio, new TnConfig()) { }

		public Tn(uint npages, float WRRatio, TnConfig conf)
			: this(null, npages, WRRatio, conf) { }

		public Tn(IBlockDevice dev, uint npages, float WRRatio, TnConfig conf)
			: base(dev, npages)
		{
			q.SetConcat(0, 1);
			q.SetConcat(2, 3);
			q.SetConcat(4, 5);

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
			this.WRRatio = WRRatio;

			if (WRRatio > 1)
			{
				rplus = 1;
				wplus = WRRatio;
			}
			else
			{
				rplus = 1 / WRRatio;
				wplus = 1;
			}
		}

		public override string Description
		{
			get
			{
				return Utils.FormatDescription("NPages", pool.NPages,
					"KickN", WRRatio.ToString("0.##"),
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
		}


		protected override void DoAccess(uint pageid, byte[] resultOrData, AccessType type)
		{
			MultiListNode<IFrame> node;
			IFrame f;

			if (!map.TryGetValue(pageid, out node))
			{
				f = new Frame(pageid);
				PerformAccess(f, resultOrData, type);

				map[pageid] = q.AddFirst(
					SRLimit != 0 ? 4 : type == AccessType.Read ? 0 : 2, f);

				return;
			}

			f = node.Value;
			int inwhichqueue = node.ListIndex;
			bool inCR = (inwhichqueue == 0);
			bool inCNR = (inwhichqueue == 1);
			bool inDR = (inwhichqueue == 2);
			bool inDNR = (inwhichqueue == 3);
			bool inSR = (inwhichqueue == 4);
			bool inSNR = (inwhichqueue == 5);
			bool inClean = inCR || inCNR;
			bool inDirty = inDR || inDNR;
			bool inSingle = inSR || inSNR;

			if (inClean && type == AccessType.Read)
			{
				node = q.AddFirst(0, q.Remove(node));
				if (!f.Resident)
					EnlargeCRLimit(rplus);
				PerformAccess(f, resultOrData, type);
			}
			else if (inClean && type == AccessType.Write)
			{
				q.Remove(node);
				PerformAccess(f, resultOrData, type);
				node = q.AddFirst(2, f);
			}
			else if (inDR && type == AccessType.Read)
			{
				if (conf.AdjustDRWhenReadInDR)
					node = q.AddFirst(2, q.Remove(node));
				PerformAccess(f, resultOrData, type);
			}
			else if (inDNR && type == AccessType.Read)
			{
				if (conf.EnlargeCRWhenReadInDNR)
					EnlargeCRLimit(rplus);
				q.Remove(node);
				PerformAccess(f, resultOrData, type);
				node = q.AddFirst(0, f);
			}
			else if (inDirty && type == AccessType.Write)
			{
				q.Remove(node);
				if (!f.Resident)
					EnlargeCRLimit(-wplus);
				PerformAccess(f, resultOrData, type);
				node = q.AddFirst(2, f);
			}
			else if (inSR)
			{
				if (conf.PickOffSRWhenHitInSR)
					node = q.AddFirst((type == AccessType.Read ? 0 : 2), q.Remove(node));
				PerformAccess(f, resultOrData, type);
			}
			else if (inSNR)
			{
				q.Remove(node);
				PerformAccess(f, resultOrData, type);
				node = q.AddFirst(type == AccessType.Read ? 0 : 2, f);
			}
			else
			{
				throw new Exception("Should not come here.");
			}

			map[pageid] = node;
		}

		protected override void OnPoolFull()
		{
			MultiListNode<IFrame> node;
			if (q.GetNodeCount(0) > CRLimit) node = q.Blow(0);
			else if (q.GetNodeCount(2) > DRLimit) node = q.Blow(2);
			else if (q.GetNodeCount(4) > SRLimit) node = q.Blow(4);
			else if (q.GetNodeCount(0) != 0) node = q.Blow(0);
			else if (q.GetNodeCount(2) != 0) node = q.Blow(2);
			else node = q.Blow(4);

			IFrame f = node.Value;
			WriteIfDirty(f);
			pool.FreeSlot(f.DataSlotId);
			f.DataSlotId = -1;
			map[f.Id] = node;

			if (q.GetNodeCount(1) > CNRLimit)
				map.Remove(q.RemoveLast(1).Id);
			if (q.GetNodeCount(3) > DNRLimit)
				map.Remove(q.RemoveLast(3).Id);
			if (q.GetNodeCount(5) > SNRLimit)
				map.Remove(q.RemoveLast(5).Id);
		}

		protected override void DoFlush()
		{
			var drpages = new List<uint>();

			foreach (MultiListNode<IFrame> node in map.Values)
			{
				IFrame f = node.Value;
				if (!f.Dirty)
					continue;

				dev.Write(f.Id, pool[f.DataSlotId]);
				f.Dirty = false;

				if (node.ListIndex == 2 || node.ListIndex == 3)
					drpages.Add(f.Id);
			}

			foreach (uint pageid in drpages)
				map[pageid] = q.AddFirst(0, q.Remove(map[pageid]));
		}
	}
}
