using System;
using System.Collections.Generic;
using Buffers.Utilities;
using Buffers.Devices;
using System.Diagnostics;

namespace Buffers.FTL
{
	public sealed class SimpleLogPageFTL : FTLBase
	{
		private readonly uint logAreaSize;
		private readonly uint logRelativeOffset;
		private readonly Dictionary<BlockPageId, BlockPageId> forward;
		private readonly Dictionary<BlockPageId, BlockPageId> backward;
		private readonly Dictionary<uint, LogBlockInfo> logInfos;
		private readonly SortedDictionary<LogBlockInfo, object> fullLogInfos;

		public SimpleLogPageFTL(IErasableDevice device, uint nBlockOfLogArea)
			: base(device)
		{
			if (nBlockOfLogArea == 0)
				throw new ArgumentOutOfRangeException("nBlockOfLogArea", "cannot be zero");

			logAreaSize = nBlockOfLogArea;
		}

		public override string Description
		{
			get
			{
				return Utils.FormatDesc("LogArea", logAreaSize);
			}
		}
		public uint NBlockOfLogArea { get { return logAreaSize; } }

		private BlockPageId ToLogBlockArea(BlockPageId relative)
		{
			Debug.Assert(relative.BlockId < logAreaSize);
			return new BlockPageId(relative.BlockId + logRelativeOffset, relative.PageId);
		}
		private BlockPageId ToLogRelative(BlockPageId absolute)
		{
			Debug.Assert(absolute.BlockId >= logRelativeOffset);
			uint relblockid = absolute.BlockId - logRelativeOffset;
			Debug.Assert(relblockid < logAreaSize);
			return new BlockPageId(relblockid, absolute.PageId);
		}

		protected override void DoRead(uint pageid, byte[] result)
		{
			BlockPageId bpid = dev.ToBlockPageId(pageid);
			BlockPageId logbpid;

			if (forward.TryGetValue(bpid, out logbpid))
				dev.Read(dev.ToUniversalPageId(ToLogBlockArea(logbpid)), result);
			else			
				dev.Read(pageid, result);
		}
		protected override void DoWrite(uint pageid, byte[] data)
		{
			dev.Write(pageid, data);
		}


		private class LogBlockInfo : IComparable<LogBlockInfo>
		{
			public readonly uint BlockId;
			public uint NPageUsed;
			public readonly HashSet<uint> AssociatedBlocks;

			public LogBlockInfo(uint blockId)
			{
				BlockId = blockId;
				NPageUsed = 0;
				AssociatedBlocks = new HashSet<uint>();
			}

			public int CompareTo(LogBlockInfo other)
			{
				return AssociatedBlocks.Count - other.AssociatedBlocks.Count;
			}
		}

	}
}