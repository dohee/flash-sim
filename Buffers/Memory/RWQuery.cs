using System;
using Buffers;

namespace Buffers.Memory
{
	public struct RWQuery : IEquatable<RWQuery>
	{
		public uint PageId { get; private set; }
		public AccessType Type { get; private set; }

		public RWQuery(uint id, AccessType type) : this() { PageId = id; Type = type; }
		public RWQuery(uint id, bool isWrite) : this(id, isWrite ? AccessType.Write : AccessType.Read) { }

        public override string ToString()
        {
            return string.Format("Frame{{Id={0},Type={1}}}",
                PageId, Type);
        }

		#region Equals 函数族
		public bool Equals(RWQuery other)
		{
			return PageId == other.PageId && Type == other.Type;
		}
		public override int GetHashCode()
		{
			return PageId.GetHashCode() ^ Type.GetHashCode();
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
