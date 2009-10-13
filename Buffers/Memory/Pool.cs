using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Buffers.Memory
{
	public class Pool
	{
		public delegate void PoolFullHandler();
		private byte[][] data;
		private bool[] dataEmpty;
		private Stack<int> freeList;
		private PoolFullHandler fullHdlr;

		public Pool(uint npages, uint pagesize, PoolFullHandler handler)
		{
			Debug.Assert(npages > 0);
			Debug.Assert(handler != null);

			data = new byte[npages][];
			dataEmpty = new bool[npages];
			freeList = new Stack<int>((int)npages);
			fullHdlr = handler;

			for (int i = 0; i < data.Length; i++)
			{
				data[i] = new byte[pagesize];
				dataEmpty[i] = true;
				freeList.Push(i);
			}
		}

		public uint NPages { get { return (uint)data.Length; } }
		public uint PageSize { get { return (uint)data[0].Length; } }

		public int AllocSlot()
		{
			if (freeList.Count == 0)
			{
				fullHdlr();
				Debug.Assert(freeList.Count > 0);
			}

			int slotid = freeList.Pop();
			dataEmpty[slotid] = false;
			return slotid;
		}

		public void FreeSlot(int slotid)
		{
			freeList.Push(slotid);
			dataEmpty[slotid] = true;
		}

		public byte[] this[int index]
		{
			get
			{
				Debug.Assert(dataEmpty[index] == false);
				return data[index];
			}
		}
	}
}
