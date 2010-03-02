using System;
using System.Collections.Generic;
using Buffers.Lists;
using Buffers.Memory;

namespace Buffers.Managers
{
    public class CCFLRU : BufferManagerBase
    {
        private LinkedList<IFrame> list = new LinkedList<IFrame>();
        private LinkedList<IFrame> cclist = new LinkedList<IFrame>();
        private IDictionary<uint, LinkedListNode<IFrame>> map = new Dictionary<uint, LinkedListNode<IFrame>>();

        public CCFLRU(uint npages)
            : this(null, npages) { }
        public CCFLRU(IBlockDevice dev, uint npages)
            : base(dev, npages) { }

        private int COLDMAX = 1;

        public override string Description { get { return Utils.FormatDescription("NPages", pool.NPages); } }

        protected override void OnPoolFull()
        {
            FrameWSR frame;
            LinkedListNode<IFrame> node;
            if (cclist.Count > 0)//不空就踢cleanlist的。
            {
                frame = cclist.Last.Value as FrameWSR;
                cclist.RemoveLast();
            }
            else//用LRUWSR的踢法
            {
                while (true)
                {
                    if (list.Count == 0)//全踢到cclist里了。。。原来算法没有考虑。
                    {
                        frame = cclist.Last.Value as FrameWSR;
                        cclist.RemoveLast();
                        break;
                    }
                    frame = list.Last.Value as FrameWSR;
                    list.RemoveLast();
                    if (frame.cold >= COLDMAX)
                        break;
                    else
                    {
                        frame.cold++;
                        if (frame.cold >= COLDMAX && !frame.Dirty)//是cold clean
                            node = cclist.AddFirst(frame);
                        else
                            node = list.AddFirst(frame);
                        map[frame.Id] = node;
                    }
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
                node.List.Remove(node);//不管在哪都先拿出来再放到好的位置
                frame.cold = 0;
            }
            else
            {
                frame = new FrameWSR(pageid);
                frame.cold = COLDMAX;
            }

            PerformAccess(frame, resultOrData, type);
            if (frame.cold >= COLDMAX && !frame.Dirty)//是cold clean
                node = cclist.AddFirst(frame);
            else
                node = list.AddFirst(frame);
            map[pageid] = node;
        }

        protected override void DoFlush()
        {
            foreach (var item in list)
                WriteIfDirty(item);
        }
    }


    //class FrameWSR : Frame
    //{
    //    public int cold = 0;
    //    public FrameWSR(uint pageid)
    //        : base(pageid)
    //    {
    //    }
    //}

}