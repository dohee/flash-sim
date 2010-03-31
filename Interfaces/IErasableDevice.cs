using System;

namespace Buffers
{
	public interface IErasableDevice : IBlockDevice
	{
		/// <summary> 该设备的块数
		/// </summary>
		uint BlockCount { get; }
		/// <summary> 每个块的页面数
		/// </summary>
		ushort PageCountPerBlock { get; }

		/// <summary> 执行擦除操作
		/// </summary>
		void Erase(uint blockid);
		/// <summary> 擦除操作的次数
		/// </summary>
		int EraseCount { get; }
		/// <summary> 返回某块被擦除的次数
		/// </summary>
		int GetBlockEraseCount(uint blockid);

		BlockPageId ToBlockPageId(uint universalPageId);
		uint ToBlockId(uint universalPageId);
		ushort ToPageIdInBlock(uint universalPageId);
		uint ToUniversalPageId(uint blockid, ushort pageid);
		uint ToUniversalPageId(BlockPageId bpid);
	}

	public struct BlockPageId
	{
		public uint BlockId { get; private set; }
		public ushort PageId { get; private set; }

		public BlockPageId(uint blockid, ushort pageid)
			: this()
		{
			BlockId = blockid;
			PageId = pageid;
		}

		public bool IsInvalid
		{
			get
			{
				return BlockId == uint.MaxValue || PageId == uint.MinValue;
			}
		}
		public static BlockPageId Invalid
		{
			get
			{
				return new BlockPageId(uint.MaxValue, ushort.MaxValue);
			}
		}

		#region Equals 函数族
		public bool Equals(BlockPageId other)
		{
			return BlockId == other.BlockId && PageId == other.PageId;
		}
		public override int GetHashCode()
		{
			return BlockId.GetHashCode() ^ PageId.GetHashCode();
		}
		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
				return false;
			else
				return Equals((BlockPageId)obj);
		}
		public static bool operator ==(BlockPageId left, BlockPageId right)
		{
			return left.Equals(right);
		}
		public static bool operator !=(BlockPageId left, BlockPageId right)
		{
			return !left.Equals(right);
		}
		#endregion
	}

}
