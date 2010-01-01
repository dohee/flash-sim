using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Buffers.Memory
{
	public class Pool
	{
		public delegate void PoolFullHandler();
		private byte[][] data;
		private int[] freeLink;
		private int freeLinkHead;
		private PoolFullHandler fullHdlr;

		public Pool(uint npages, uint pagesize, PoolFullHandler handler)
		{
			Debug.Assert(npages > 0);
			Debug.Assert(handler != null);

			data = new byte[npages][];
			freeLink = new int[npages];
			fullHdlr = handler;

			for (int i = 0; i < data.Length; i++)
			{
				data[i] = new byte[pagesize];
				freeLink[i] = i + 1;
			}

			freeLink[freeLink.Length - 1] = -1;
			freeLinkHead = 0;
		}

		public uint NPages { get { return (uint)data.Length; } }
		public uint PageSize { get { return (uint)data[0].Length; } }

		public int AllocSlot()
		{
			if (freeLinkHead == -1)
			{
				fullHdlr();
				if (freeLinkHead == -1)
					throw new InvalidOperationException("the PoolFullHandler does not free any slot.");
			}

			int slotid = freeLinkHead;
			freeLinkHead = freeLink[slotid];
			freeLink[slotid] = -2;
			return slotid;
		}

		public void FreeSlot(int slotid)
		{
			Debug.Assert(freeLink[slotid] == -2);
			freeLink[slotid] = freeLinkHead;
			freeLinkHead = slotid;
		}

		public byte[] this[int index]
		{
			get
			{
				Debug.Assert(freeLink[index] == -2);
				return data[index];
			}
		}
	}
}
