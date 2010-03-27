using System;
using System.Diagnostics;
using Buffers.Utilities;

namespace Buffers.Devices
{
	public abstract class ErasableDeviceBase : BlockDeviceBase, IErasableDevice
	{
		// 字段
		protected readonly byte blockSizeBit;
		protected readonly ushort blockSize;

		// 子类要实现的
		/// <summary> 真实的擦除操作
		/// </summary>
		protected abstract void DoErase(uint blockid);
		/// <summary> 在真实擦除操作执行前执行此动作 </summary>
		/// <remarks> 继承者注意：必须在方法开头处调用 base.BeforeErase </remarks>
		protected virtual void BeforeErase(uint blockid) { }
		/// <summary> 在真实擦除操作执行后执行此动作 </summary>
		/// <remarks> 继承者注意：必须在方法结尾处调用 base.AfterErase </remarks>
		protected virtual void AfterErase(uint blockid) { EraseCount++; }

		// 可供使用的
		protected BlockPageId ToBlockPageId(uint universalPageId)
		{
			return new BlockPageId(
				universalPageId >> blockSizeBit,
				(ushort)(universalPageId & (BlockSize - 1)));
		}
		protected uint ToBlockId(uint universalPageId)
		{
			return universalPageId >> blockSizeBit;
		}
		protected ushort ToPageIdInBlock(uint universalPageId)
		{
			return (ushort)(universalPageId & (BlockSize - 1));
		}
		protected uint ToUniversalPageId(uint blockid, ushort pageid)
		{
			Debug.Assert((pageid & (BlockSize - 1)) == pageid);
			return (blockid << blockSizeBit) | pageid;
		}
		protected uint ToUniversalPageId(BlockPageId bpid)
		{
			return ToUniversalPageId(bpid.BlockId, bpid.PageId);
		}

		// 已实现的
		public ErasableDeviceBase(ushort blockSize)
		{
			sbyte bit = Utils.ExpToLogTwo(blockSize);

			if (bit < 0)
				throw new ArgumentOutOfRangeException("blockSize",
					blockSize + " is not a power of 2");

			this.blockSize = blockSize;
			this.blockSizeBit = (byte)bit;
		}
		public override string Description
		{
			get
			{
				return Utils.FormatDesc("BlockSize", blockSize);
			}
		}
		public int EraseCount { get; private set; }
		public void Erase(uint blockid)
		{
			BeforeErase(blockid);
			DoErase(blockid);
			AfterErase(blockid);
		}
		public ushort BlockSize { get { return blockSize; } }
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
