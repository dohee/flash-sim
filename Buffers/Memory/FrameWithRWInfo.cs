using System;
using Buffers;

namespace Buffers.Memory
{
	public class FrameWithRWInfo<NodeType> : Frame
	{
        public FrameWithRWInfo(uint id) : base(id) { InitFrameWithRWInfo(); }
        public FrameWithRWInfo(uint id, int slotid) : base(id, slotid) { InitFrameWithRWInfo(); }

		public NodeType NodeOfRead { get; set; }
		public NodeType NodeOfWrite { get; set; }

        private void InitFrameWithRWInfo()
        {
            NodeOfRead = default(NodeType);
            NodeOfWrite = default(NodeType);
        }

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
