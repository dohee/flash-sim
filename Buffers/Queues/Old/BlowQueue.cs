using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffers.Memory;

//namespace Buffers.Managers
//{
//    //窗口，命中窗口内的页面要调整窗口位置。存储了窗口的首尾元素
//    class BlowWindow
//    {
//        public Dictionary<uint, QueueNode<IFrame>> map;
//        protected LinkedListNode<uint> startNode;	//MRU方向
//        protected LinkedListNode<uint> endNode;	//是LRU方向
//        int windowsize;		//当前窗口大小
//        int windowSizeLimit;		//窗口大小的限制
//        bool isRead;		//是read窗口？

//        void setInWindow(uint frameID, bool inWindow)
//        {
//            if (isRead)
//            {
//                (BlowFrame)(map[frameID]).inReadWindow = inWindow;
//            }
//            else
//            {
//                (BlowFrame)(map[frameID]).inWriteWindow = inWindow;
//            }
//        }
//        //调整窗口位置，最大队列长度是windowSizeLimit。正的代表向end方向移动。
//        //startNode不会LRU于队列里最后一个residentNode。返回实质移动了多少。
//        public int adjustWindowPosition(int offset)
//        {
//            if (offset < 0)
//            {
//                for (int i = 0; i < -offset; i++)
//                {
//                    if (startNode.Previous != null)
//                    {
//                        startNode = startNode.Previous;
//                        setInWindow(startNode.Value, true);
//                        setInWindow(endNode.Value, false);
//                        endNode = endNode.Previous;
//                    }
//                }
//                return -offset;
//            }
//            if (offset > 0)
//            {
//                for (int i = 0; i < offset; i++)
//                {
//                    if(startNode==null)
//                    if(startNode.Next==null;
//                    (BlowFrame)(map[startNode.Next.Value])
//                    if (startNode.Previous != null)
//                    {
//                        startNode = startNode.Previous;
//                        setInWindow(startNode.Value, true);
//                    }
//                    setInWindow(endNode.Value, false);
//                    endNode = endNode.Previous;
//                }
//            }
//            return 0;
//        }
//    }
//}

////////////////////////////////////////////
//namespace Buffers.Queues
//{
//    class BlowQueue<T> : FIFOQueue<T>
//    {
//        public Dictionary<uint, QueueNode<IFrame>> map;
//        //替换时从此结点向MLU方向扫描。
//        //目前没有引入动态算法只是每次都从最后一个扫描。
//        LinkedListNode<T> blowEnd = null;

//        public BlowWindow blowWindow;

//        //访问某个页面
//        public void acessFrame( BlowFrame blowFrame)
//        {
//        }

//        //返回一个frame id，如果不成功返回0。
//        public int getVictim()
//        {
//            return 0;
//        }
//    }
//}

/////////////////////////////////////////////////////////////////


