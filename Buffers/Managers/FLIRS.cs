using System;
using System.Collections.Generic;
using Buffers.Memory;
using Buffers.Queues;
using Buffers.Lists;

namespace Buffers.Managers
{
	public sealed class FLIRS : BufferManagerBase
	{
		private Pool pool;
		private MultiList<RWQuery> rwlist;
		private IDictionary<uint, RWFrame> map = new Dictionary<uint, RWFrame>();

		public FLIRS(IBlockDevice dev, uint npages)
			: base(dev)
		{
			pool = new Pool(npages, this.dev.PageSize, OnPoolFull);
		}

		private void OnPoolFull() { }


		protected sealed override void DoRead(uint pageid, byte[] result)
		{
		}

		protected sealed override void DoWrite(uint pageid, byte[] data)
		{
		}

		protected override void DoFlush()
		{
		}

		private void WriteIfDirty(IFrame frame)
		{
			if (frame.Dirty)
			{
				dev.Write(frame.Id, pool[frame.DataSlotId]);
				frame.Dirty = false;
			}
		}



		private class RWFrame : FrameWithRWInfo<MultiListNode<RWQuery>>
		{
			public RWFrame(uint id) : base(id) { }
			public RWFrame(uint id, int slotid) : base(id, slotid) { }
		}

		private struct RWQuery
		{
			public readonly uint PageId;
			public readonly bool IsWrite;
			public bool IsLowIR;

			public RWQuery(uint id, bool isWrite)
			{
				PageId = id;
				IsWrite = isWrite;
				IsLowIR = false;
			}
		}
	}
}
