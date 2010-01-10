using System;
using Buffers;

namespace Buffers.Memory
{
	public class FrameWithRWInfo<NodeType> : Frame
	{
		public FrameWithRWInfo(uint id) : base(id) { }
		public FrameWithRWInfo(uint id, int slotid) : base(id, slotid) { }

		public NodeType NodeOfRead { get; set; }
		public NodeType NodeOfWrite { get; set; }

		public NodeType GetNodeOf(AccessType type)
		{
			return type == AccessType.Read ? NodeOfRead : NodeOfWrite;
		}
		public void SetNodeOf(AccessType type, NodeType value)
		{
			if (type == AccessType.Read)
				NodeOfRead = value;
			else
				NodeOfWrite = value;
		}
	}
}
