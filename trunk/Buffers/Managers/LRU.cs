using System;
using System.Collections.Generic;
using Buffers.Lists;
using Buffers.Memory;
using Buffers.Utilities;

namespace Buffers.Managers
{
	public class LRU : BufferManagerBase
	{
		private LinkedList<IFrame> list = new LinkedList<IFrame>();
		private IDictionary<uint, LinkedListNode<IFrame>> map = new Dictionary<uint, LinkedListNode<IFrame>>();

		public LRU(uint npages)
			: this(null, npages) { }
		public LRU(IBlockDevice dev, uint npages)
			: base(dev, npages) { }

		public override string Description { get { return Utils.FormatDesc("NPages", pool.NPages); } }

		protected override void OnPoolFull()
		{
			IFrame frame = list.Last.Value;
			list.RemoveLast();
			map.Remove(frame.Id);
			WriteIfDirty(frame);
			pool.FreeSlot(frame.DataSlotId);
		}

		protected override void DoAccess(uint pageid, byte[] resultOrData, AccessType type)
		{
			LinkedListNode<IFrame> node;
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
			node = list.AddFirst(frame);
            map[pageid] = node;
		}

        protected override void DoFlush()
        {
            foreach (var item in list)
                WriteIfDirty(item);
        }
	}
}
