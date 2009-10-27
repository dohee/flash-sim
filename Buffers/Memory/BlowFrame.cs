using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Buffers.Memory
{
	//为blowbylyf设计的frame

	class BlowFrame :Frame
	{
		public LinkedListNode<uint> readNode=null;		//存储在read队列里的结点，如果是null表明不在read队列里
		public LinkedListNode<uint> writeNode=null;	//存储在write队列里的结点，如果是null表明不在write队列里

		////是否在window里，如果在window里就要移动窗口
		//public bool inReadWindow = false;		
		//public bool inWriteWindow = false;

		////是否在热点数据里，如果在另一个队列的热点数据里就不能替换出
		//public bool isReadHot = false;
		//public bool isWriteHot = false;

		public BlowFrame(uint id) : base(id) { }
		public BlowFrame(uint id, int slotid) : base(id, slotid) { }
	}
}
