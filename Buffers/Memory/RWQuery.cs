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

        public override string ToString()
        {
            return string.Format("Frame{{Id={0},IsWrite={1}}}",
                PageId, IsWrite);
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
}
