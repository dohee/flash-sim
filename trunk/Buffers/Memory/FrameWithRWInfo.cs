using System;

namespace Buffers.Memory
{
	public class FrameWithRWInfo<NodeType> : Frame
	{
		protected NodeType nodeR, nodeW;

		public FrameWithRWInfo(uint id) : base(id) { }
		public FrameWithRWInfo(uint id, int slotid) : base(id, slotid) { }

		public NodeType NodeOfRead
		{
			get { return nodeR; }
			set { nodeR = value; }
		}
		public NodeType NodeOfWrite
		{
			get { return nodeW; }
			set { nodeW = value; }
		}
	}
}
