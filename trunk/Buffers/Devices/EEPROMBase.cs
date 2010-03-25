using System;
using Buffers.Utilities;

namespace Buffers.Devices
{
	public abstract class EEPROMBase : ErasableDeviceBase
	{
		// 字段
		private SparseArray<ushort> writePos = new SparseArray<ushort>();

		// 子类要实现的
		// （无）

		// 可供使用的
		// （无）

		// 已实现的
		public EEPROMBase(ushort blockSize)
			: base(blockSize) { }

		protected override void BeforeWrite(uint pageid)
		{
			base.BeforeWrite(pageid);

			BlockPageId bpid = ToBlockPageId(pageid);
			ushort shouldWrite = writePos[bpid.BlockId];

			if (shouldWrite != bpid.PageId)
				throw new ArgumentOutOfRangeException("pageid", string.Format(
					"Should write Page {2} in Block {0}, but request to write Page {1}",
					bpid.BlockId, bpid.PageId, shouldWrite));
			if (shouldWrite >= BlockSize)
				throw new ArgumentOutOfRangeException("pageid", string.Format(
					"Block {0} is full with {1} pages written already",
					bpid.BlockId, BlockSize));
		}
		protected override void AfterWrite(uint pageid)
		{
			writePos[ToBlockId(pageid)]++;
			base.AfterWrite(pageid);
		}
		protected override void AfterErase(uint blockid)
		{
			writePos[blockid] = 0;
			base.AfterErase(blockid);
		}
	}
}