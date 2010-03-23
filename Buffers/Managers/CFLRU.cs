using System;
using System.Collections.Generic;
using Buffers.Lists;
using Buffers.Memory;
using Buffers.Utilities;

namespace Buffers.Managers
{
	public class CFLRU : BufferManagerBase
	{
		private MultiList<IFrame> list = new MultiList<IFrame>(3);
		private IDictionary<uint, MultiListNode<IFrame>> map = new Dictionary<uint, MultiListNode<IFrame>>();
		private readonly float windowSize;
		private readonly int maxNonwindowLength;

		public CFLRU(uint npages, float windowSize)
			: this(null, npages, windowSize) { }
		public CFLRU(IBlockDevice dev, uint npages, float windowSize)
			: base(dev, npages)
		{
			this.windowSize = windowSize;
			this.maxNonwindowLength = (int)(npages * (1 - windowSize));
		}

		public override string Description
		{
			get
			{
				return Utils.FormatDescription("NPages", pool.NPages,
					"WindowSize", windowSize);
			}
		}

		protected override void OnPoolFull()
		{
			IFrame frame;

			if (list.GetNodeCount(1) > 0)
				frame = list.RemoveLast(1);
			else if (list.GetNodeCount(2) > 0)
				frame = list.RemoveLast(2);
			else
				frame = list.RemoveLast(0);

			map.Remove(frame.Id);
			WriteIfDirty(frame);
			pool.FreeSlot(frame.DataSlotId);
		}

		protected override void DoAccess(uint pageid, byte[] resultOrData, AccessType type)
		{
			MultiListNode<IFrame> node;
			IFrame frame;

			if (map.TryGetValue(pageid, out node))
			{
				frame = node.Value;
				list.Remove(node);
			}
			else
			{
				frame = new Frame(pageid);
			}

			PerformAccess(frame, resultOrData, type);
			node = list.AddFirst(0, frame);
            map[pageid] = node;

			MaintainWindow();
		}

		private void MaintainWindow()
		{
			while (list.GetNodeCount(0) > maxNonwindowLength)
			{
				IFrame frame = list.RemoveLast(0);
				MultiListNode<IFrame> node = list.AddFirst(frame.Dirty ? 2 : 1, frame);
				map[frame.Id] = node;
			}
		}

        protected override void DoFlush()
        {
			foreach (var node in list.EnumNodes(0))
				WriteIfDirty(node.Value);

			while (list.GetNodeCount(2) > 0)
			{
				IFrame frame = list.RemoveLast(2);
				WriteIfDirty(frame);
				MultiListNode<IFrame> node = list.AddFirst(1, frame);
				map[frame.Id] = node;
			}
        }
	}
}
