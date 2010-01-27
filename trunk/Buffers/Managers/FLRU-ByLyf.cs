//this applies idea of FLIRS on FLRU to eliminate the effect from lirs.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Buffers.Memory;
using Buffers.Queues;
using Buffers.Lists;

namespace Buffers.Managers
{
    public sealed class FLRU : BufferManagerBase
    {
        //private readonly float ratio;
        private readonly FLIRSQueue readList;
        private readonly FLIRSQueue writeList;
        private readonly IDictionary<uint, RWFrame> map = new Dictionary<uint, RWFrame>();
        private readonly uint cacheSize;

        public FLRU(uint npages)
            : this(null, npages) { }

        public FLRU(IBlockDevice dev, uint npages)
            : base(dev, npages)
        {
            //this.ratio = ratio;
            cacheSize = npages;
            readList = new FLIRSQueue(false);
            writeList = new FLIRSQueue(true);
        }


        protected override void OnPoolFull()
        {
            RWFrame frame = Tidy();

            WriteIfDirty(frame);
            pool.FreeSlot(frame.DataSlotId);
            frame.DataSlotId = -1;
        }

        protected sealed override void DoRead(uint pageid, byte[] result)
        {
            RWFrame frame = null;
            //////////////////////////////////by cat
            if (!map.TryGetValue(pageid, out frame))
            {
                frame = new RWFrame(pageid);
                map[pageid] = frame;
            }


            if (!frame.Resident)
            {
                frame.DataSlotId = pool.AllocSlot();
                dev.Read(pageid, pool[frame.DataSlotId]);
                pool[frame.DataSlotId].CopyTo(result, 0);
            }
            pool[frame.DataSlotId].CopyTo(result, 0);
            //////////////////////////////////////////////////////

            readList.updateFrame(frame, false);
            writeList.updateFrame(frame, false);
        }


        protected sealed override void DoWrite(uint pageid, byte[] data)
        {
            RWFrame frame = null;

            if (!map.TryGetValue(pageid, out frame))
            {
                frame = new RWFrame(pageid);
                map[pageid] = frame;
            }
            if (!frame.Resident)
            {
                frame.DataSlotId = pool.AllocSlot();
            }

            data.CopyTo(pool[frame.DataSlotId], 0);
            frame.Dirty = true;

            //////////////////////////////////////////////////////

            readList.updateFrame(frame, true);
            writeList.updateFrame(frame, true);
        }

        private RWFrame Tidy()
        {
            RWFrame frame;
            while (true)
            {
                if (writeList.Count() * Config.ReadCost > readList.Count() * Config.WriteCost)
                //if (writeList.Count() * 66 > readList.Count() * 200)
                {
                    frame = writeList.removeLast(map);
                }
                else
                {
                    frame = readList.removeLast(map);
                }
                if (frame == null)
                {
                    continue;
                }

                if (frame.ShouldBeVictim() && frame.Resident)
                {
                    break;
                }
            }
            return frame;
        }

        protected override void DoFlush()
        {
            foreach (var entry in map)
            {
                WriteIfDirty(entry.Value);
                entry.Value.Dirty = false;
            }
        }



        public class RWFrame : Frame
        {
            public RWFrame(uint id) : base(id) { Init(); }
            public RWFrame(uint id, int slotid) : base(id, slotid) { Init(); }

            private void Init()
            {
                NodeOfReadInWriteQueue = null;
                NodeOfWriteInReadQueue = null;
                NodeOfReadInReadQueue = null;
                NodeOfWriteInWriteQueue = null;
            }

            public override string ToString()
            {
                return string.Format("Frame{{Id={0},Dirty={1},SlotId={2},InRead={3},InWrite={4}}}",
                    Id, Dirty, DataSlotId, NodeOfReadInReadQueue == null, NodeOfWriteInWriteQueue == null);
            }

            public bool ShouldBeVictim()
            {
                return (!Dirty && NodeOfReadInReadQueue==null) ||
                    (Dirty && NodeOfReadInReadQueue==null && NodeOfWriteInWriteQueue==null);
            }

            public LinkedListNode<RWQuery> NodeOfReadInReadQueue { get; set; }
            public LinkedListNode<RWQuery> NodeOfWriteInWriteQueue { get; set; }
            public LinkedListNode<RWQuery> NodeOfReadInWriteQueue { get; set; }
            public LinkedListNode<RWQuery> NodeOfWriteInReadQueue { get; set; }
        }


        private class FLIRSQueue
        {
            LinkedList<RWQuery> queue = new LinkedList<RWQuery>();      //queue for RWQuery
            bool isWriteQueue;       //this is a write queue?
            //IDictionary<uint, RWFrame> map;

            public FLIRSQueue(bool iisWriteQueue)
            {
                isWriteQueue = iisWriteQueue;
                //map = imap;
            }

            public LinkedListNode<RWQuery> updateFrame(RWFrame frame, bool isWrite)
            {
                if (getNode(frame, isWriteQueue, isWrite) == null)
                {
                    return Enqueue(frame, isWrite);
                }
                else
                {
                    return AccessFrame(frame, isWrite);
                }
            }

            public LinkedListNode<RWQuery> AccessFrame(RWFrame frame, bool isWrite)
            {
                LinkedListNode<RWQuery> node = getNode(frame, isWriteQueue, isWrite);

                queue.Remove(node);
                LinkedListNode<RWQuery> newNode = queue.AddFirst(new RWQuery(frame.Id, isWrite));
                updateFrameNode(frame, isWriteQueue, node.Value.Type == AccessType.Write, newNode);

                return newNode;
            }


            public LinkedListNode<RWQuery> Enqueue(RWFrame frame, bool isWrite)
            {
                LinkedListNode<RWQuery> newNode = queue.AddFirst(new RWQuery(frame.Id, isWrite));
                updateFrameNode(frame, isWriteQueue, isWrite, newNode);
                return newNode;
            }

            public int Count()
            {
                return queue.Count;
            }

            //return the lastNode(is LIR) and prune HIR node to insure a new LIR node is the currentNode.
            public RWFrame removeLast(IDictionary<uint, RWFrame> map)
            {
                //update the info of lastNode.
                LinkedListNode<RWQuery> queryNode = queue.Last;

                if (queryNode != null)
                {
                    RWQuery query = queryNode.Value;
                    RWFrame frame = map[query.PageId];
                    
                    updateFrameNode(frame, isWriteQueue, queryNode.Value.Type == AccessType.Write, null);
                    queue.Remove(queryNode);
                    return frame;
                }
                return null;
            }

            private LinkedListNode<RWQuery> getNode(RWFrame frame, bool isWriteQueue, bool isWriteOperation)
            {
                if (isWriteQueue)
                {
                    if (isWriteOperation)
                    {
                        return frame.NodeOfWriteInWriteQueue;
                    }
                    else
                    {
                        return frame.NodeOfReadInWriteQueue;
                    }
                }
                else
                {
                    if (isWriteOperation)
                    {
                        return frame.NodeOfWriteInReadQueue;
                    }
                    else
                    {
                        return frame.NodeOfReadInReadQueue;
                    }
                }
            }

            private void updateFrameNode(RWFrame frame, bool isWriteQueue, bool isWriteOperation, LinkedListNode<RWQuery> node)
            {
                if (isWriteQueue)
                {
                    if (isWriteOperation)
                    {
                        frame.NodeOfWriteInWriteQueue = node;
                    }
                    else
                    {
                        frame.NodeOfReadInWriteQueue = node;
                    }
                }
                else
                {
                    if (isWriteOperation)
                    {
                        frame.NodeOfWriteInReadQueue = node;
                    }
                    else
                    {
                        frame.NodeOfReadInReadQueue = node;
                    }
                }
            }
        }
    }
}
