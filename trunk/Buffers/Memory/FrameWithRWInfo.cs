using System;

namespace Buffers.Memory
{
	public class FrameWithRWInfo<NodeType> : Frame
	{
		public FrameWithRWInfo(uint id) : base(id) { }
		public FrameWithRWInfo(uint id, int slotid) : base(id, slotid) { }

		public NodeType NodeOfRead { get; set; }
		public NodeType NodeOfWrite { get; set; }
	}
}
