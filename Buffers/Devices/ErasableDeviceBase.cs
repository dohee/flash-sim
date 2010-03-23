using System;
using System.Diagnostics;

namespace Buffers.Devices
{
	public abstract class ErasableDeviceBase : BlockDeviceBase, IErasableDevice
	{
		/// <summary> 真实的擦除操作
		/// </summary>
		protected abstract void DoErase(uint blockid);
		/// <summary> 在真实擦除操作执行前执行此动作 </summary>
		/// <remarks> 继承者注意：必须在方法开头处调用 base.BeforeErase </remarks>
		protected virtual void BeforeErase(uint blockid) { }
		/// <summary> 在真实擦除操作执行后执行此动作 </summary>
		/// <remarks> 继承者注意：必须在方法结尾处调用 base.AfterErase </remarks>
		protected virtual void AfterErase(uint blockid) { EraseCount++; }

		protected virtual byte BlockSizeBit { get; set; }
		public virtual ushort BlockSize { get { return (ushort)(1 << BlockSizeBit); } }
		public int EraseCount { get; private set; }

		public void Erase(uint blockid)
		{
			BeforeErase(blockid);
			DoErase(blockid);
			AfterErase(blockid);
		}

		protected BlockPageId ToBlockPageId(uint universalPageId)
		{
			return new BlockPageId(
				universalPageId >> BlockSizeBit,
				(ushort)(universalPageId & (BlockSize - 1)));
		}
		protected uint ToUniversalPageId(uint blockid, ushort pageid)
		{
			Debug.Assert((pageid & (BlockSize - 1)) == pageid);
			return (blockid << BlockSizeBit) | pageid;
		}
		protected uint ToUniversalPageId(BlockPageId bpid)
		{
			return ToUniversalPageId(bpid.BlockId, bpid.PageId);
		}
	}


	public struct BlockPageId
	{
		public uint BlockId;
		public ushort PageId;

		public BlockPageId(uint blockid, ushort pageid)
		{
			BlockId = blockid;
			PageId = pageid;
		}
	}
}
