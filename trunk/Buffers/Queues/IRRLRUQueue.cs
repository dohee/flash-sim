using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffers.Memory;

//my first c# programe.  Lv Yanfei
//LRU class with IRR
//这里使用frame只使用了frameid的属性。
namespace Buffers.Queues
{
    public class IRRLRUQueue :LRUQueue
    {
        //return IRR, IRR>=1(this value can be increased?), if IRR is <=0, this page is not resident.
        public int accessIRR(Frame iFrame)
        {
            int irr = 0;        //统计IRR，如果在队首，IRR就是1.

            Frame frameout=null;
            foreach (Frame frame in queue)
            {
                irr++;
                if (frame.Id == iFrame.Id)
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
