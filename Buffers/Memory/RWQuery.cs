using System;

namespace Buffers.Memory
{
	public struct RWQuery : IEquatable<RWQuery>
	{
		public readonly uint PageId;
		public readonly bool IsWrite;

		public RWQuery(uint id, bool isWrite)
		{
			PageId = id;
			IsWrite = isWrite;
		}

		#region Equals 函数族
		public bool Equals(RWQuery other)
		{
			return PageId == other.PageId && IsWrite == other.IsWrite;
		}
		public override int GetHashCode()
		{
			return PageId.GetHashCode() ^ IsWrite.GetHashCode();
		}
		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
				return false;
			else
				return Equals((RWQuery)obj);
		}
		public static bool operator ==(RWQuery left, RWQuery right)
		{
			return left.Equals(right);
		}
		public static bool operator !=(RWQuery left, RWQuery right)
		{
			return !left.Equals(right);
		}
		#endregion
	}

	public struct RWQueryWithIRFlag
	{
		public readonly uint PageId;
		public readonly bool IsWrite;
		public bool IsLowIR;

		public RWQueryWithIRFlag(uint id, bool isWrite)
			: this(id, isWrite, false) { }

		public RWQueryWithIRFlag(uint id, bool isWrite, bool isLowIR)
		{
			PageId = id;
			IsWrite = isWrite;
			IsLowIR = isLowIR;
		}
	}
}
