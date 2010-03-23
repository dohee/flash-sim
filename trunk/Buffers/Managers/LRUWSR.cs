using System;
using System.Collections.Generic;
using Buffers.Lists;
using Buffers.Memory;
using Buffers.Utilities;


namespace Buffers.Managers
{
    public class LRUWSR : BufferManagerBase
    {
        private LinkedList<IFrame> list = new LinkedList<IFrame>();
        private IDictionary<uint, LinkedListNode<IFrame>> map = new Dictionary<uint, LinkedListNode<IFrame>>();

        public LRUWSR(uint npages)
            : this(null, npages) { }
        public LRUWSR(IBlockDevice dev, uint npages)
            : base(dev, npages) { }

        private int COLDMAX = 1;

        public override string Description { get { return Utils.FormatDescription("NPages", pool.NPages); } }

        protected override void OnPoolFull()
        {
            FrameWSR frame;
            LinkedListNode<IFrame> node;

            while (true)
            {
                frame = list.Last.Value as FrameWSR;
                list.RemoveLast();
                if (!frame.Dirty || frame.cold>=COLDMAX)
                    break;
                else
                {
                    frame.cold++;
                    node = list.AddFirst(frame);
                    map[frame.Id] = node;
                }
            }
            map.Remove(frame.Id);
            WriteIfDirty(frame);
            pool.FreeSlot(frame.DataSlotId);
        }

        protected override void DoAccess(uint pageid, byte[] resultOrData, AccessType type)
        {
            LinkedListNode<IFrame> node;
            FrameWSR frame;

            if (map.TryGetValue(pageid, out node))
            {
                frame = node.Value as FrameWSR;
                list.Remove(node);
                frame.cold = 0;
            }
            else
            {
                frame = new FrameWSR(pageid);
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


    class FrameWSR : Frame
    {
        public int cold = 0;
        public FrameWSR(uint pageid)
            :base(pageid)
        {
        }
    }

}
