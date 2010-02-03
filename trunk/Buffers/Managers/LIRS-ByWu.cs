using System;
using System.Collections.Generic;
//using System.Linq;
//using System.Text;
using Buffers.Lists;
using Buffers.Memory;


namespace Buffers.Managers
{
    class LIRS_ByWu : BufferManagerBase
    {
        private long lirSize;
        private long hirSize;
        private long lirNum;
        private LinkedList<lirFrame> lirList = new LinkedList<lirFrame>();
        private IDictionary<uint, LinkedListNode<lirFrame>> lirMap = new Dictionary<uint, LinkedListNode<lirFrame>>();
        private LinkedList<lirFrame> hirList = new LinkedList<lirFrame>();
        private IDictionary<uint, LinkedListNode<lirFrame>> hirMap = new Dictionary<uint, LinkedListNode<lirFrame>>();

        public LIRS_ByWu(uint npages, float hirPercent)
            : this(null, npages, hirPercent) { }
        public LIRS_ByWu(IBlockDevice dev, uint npages, float hirPercent)
            : base(dev, npages)
        {
            hirSize = (int)(npages * hirPercent);
            lirSize = npages - hirSize;
            lirNum = 0;
        }


        public override string Description { get { return Utils.FormatDescription("lirPages", lirSize, "hirPages", hirSize); } }

        protected override void OnPoolFull()
        {
            lirFrame frame = hirList.Last.Value;
            hirList.RemoveLast();
            hirMap.Remove(frame.Id);

            WriteIfDirty(frame);
            pool.FreeSlot(frame.DataSlotId);
            //            Console.WriteLine("evict frame:" + frame.ToString());
            frame.DataSlotId = -1;

            LinkedListNode<lirFrame> node;
            if (lirMap.TryGetValue(frame.Id, out node))
            {
                if (node.Value.Resident)
                {
                    throw new Exception("in lirs-bywu, line 50, this frame should be nonresident!");
                }
            }


        }

        protected override void DoAccess(uint pageid, byte[] resultOrData, AccessType type)
        {

            //            Console.WriteLine("\n\nafter\t" + pageid + "\t" + (type == AccessType.Read ? "Read" : "Write"));

            LinkedListNode<lirFrame> node;
            LinkedListNode<lirFrame> hirNode;
            lirFrame frame;
            //in LIR-list
            if (lirMap.TryGetValue(pageid, out node))
            {
                frame = node.Value;
                //LIRS frame
                if (!frame.IsHir)
                {
                    lirList.Remove(node);
                    lirMap.Remove(pageid);

                    PerformAccess(frame, resultOrData, type);

                    lirList.AddFirst(frame);
                    lirMap[pageid] = lirList.First;

                    sprunning();
                }
                //HIRS resident frame
                else if (frame.Resident)
                {
                    if (hirMap.TryGetValue(pageid, out hirNode))
                    {
                        lirList.Remove(node);
                        lirMap.Remove(pageid);
                        hirList.Remove(hirNode);
                        hirMap.Remove(pageid);

                        PerformAccess(frame, resultOrData, type);

                        frame.IsHir = false;
                        lirList.AddFirst(frame);
                        lirMap[pageid] = lirList.First;

                        frame = lirList.Last.Value;
                        if (frame.IsHir)
                        {
                            throw new Exception("in Lirs_ByWu,line 94, this frame should be lirs!");
                        }
                        frame.IsHir = true;
                        lirList.RemoveLast();
                        lirMap.Remove(frame.Id);
                        sprunning();

                        hirList.AddFirst(frame);
                        hirMap[frame.Id] = hirList.First;
                    }
                    else
                    {
                        throw new Exception("in lirs_ByWu, line 106, this frame should be in hirList!");
                    }
                }
                //HIRS nonresident frame
                else
                {
                    if (hirMap.TryGetValue(pageid, out hirNode))
                    {
                        throw new Exception("in lirs_bywu, line 116, this frame should not be in hirList!");
                    }
                    lirList.Remove(node);
                    lirMap.Remove(pageid);

                    PerformAccess(frame, resultOrData, type);

                    frame.IsHir = false;
                    lirList.AddFirst(frame);
                    lirMap[pageid] = lirList.First;

                    frame = lirList.Last.Value;
                    if (frame.IsHir)
                    {
                        throw new Exception("in lirs_bywu, this frame should be LIRS");
                    }
                    frame.IsHir = true;
                    lirList.RemoveLast();
                    lirMap.Remove(frame.Id);
                    sprunning();

                    hirList.AddFirst(frame);
                    hirMap[frame.Id] = hirList.First;
                }
            }
            //HIRS resident and not in LIRS-list
            else if (hirMap.TryGetValue(pageid, out node))
            {
                frame = node.Value;
                hirList.Remove(node);
                hirMap.Remove(pageid);

                PerformAccess(frame, resultOrData, type);

                lirList.AddFirst(frame);
                lirMap[pageid] = lirList.First;
                hirList.AddFirst(frame);
                hirMap[pageid] = hirList.First;
            }
            //frame which appears at the first time
            else
            {
                frame = new lirFrame(pageid);
                if (lirNum < lirSize)
                {
                    frame.IsHir = false;
                    lirNum++;
                }
                else
                {
                    frame.IsHir = true;
                }

                PerformAccess(frame, resultOrData, type);

                lirList.AddFirst(frame);
                lirMap[pageid] = lirList.First;
                if (frame.IsHir)
                {
                    hirList.AddFirst(frame);
                    hirMap[pageid] = hirList.First;
                }
            }
            /*
                        Console.WriteLine("LIRS list is");
                        foreach (var item in lirList)
                        {
                            Console.WriteLine(item.ToString());
                        }
                        Console.WriteLine("HIRS list is");
                        foreach (var item in hirList)
                        {
                            Console.WriteLine(item.ToString());
                        }*/
        }

        protected override void DoFlush()
        {
            foreach (var item in lirList)
            {
                WriteIfDirty(item);
            }
            foreach (var item in hirList)
            {
                WriteIfDirty(item);
            }
        }

        private void sprunning()
        {
            lirFrame frame = lirList.Last.Value;
            //            Console.Write("\n");
            while (frame.IsHir)
            {
                //                Console.WriteLine("spunning:" + frame.ToString());
                lirMap.Remove(frame.Id);
                lirList.RemoveLast();
                frame = lirList.Last.Value;
            }
        }
    }
}
