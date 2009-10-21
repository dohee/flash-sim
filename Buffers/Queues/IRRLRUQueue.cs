using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffers.Memory;

//my first c# programe.  Lv Yanfei
//LRU class with IRR
//这里使用frame,使用了frameid和dirty的属性，dirty＝0，1来表示读写。
//将读和写操作都保存到一个队列里

namespace Buffers.Queues
{
    public class IRRLRUQueue :LRUQueue
    {
        //return IRR, IRR>=1(this value can be increased?), if IRR is <=0, this page is not resident.
        public uint accessIRR(uint id, bool dirty)
        {
            uint irr = 0;        //统计IRR，如果在队首，IRR就是1.

            Frame frameout=null;
            foreach (Frame frame in queue)
            {
                irr++;
                if (frame.Id == id && frame.Dirty == dirty)
                {
                    frameout = frame;
                    break;
                }
            }
            
            if (frameout!=null)//找到了
            {
                queue.Remove(frameout);
                queue.AddFirst(frameout);
                return irr;
            }
            else
            {
                return 0;
            }
        }
    }
}
