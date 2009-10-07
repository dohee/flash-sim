using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Buffers.Memory
{
	public interface IFrame
	{
		Pool AssociatedPool { get; }
		uint Id { get; set; }
		bool Dirty { get; set; }
		bool Resident { get; set; }
		byte[] Data { get; }
	}

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

		public IFrame AllocFrame()
		{
			return new Frame(this, AllocSlot());
		}

		private int AllocSlot()
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

		private void FreeSlot(int slotid)
		{
			freeList.Push(slotid);
			dataEmpty[slotid] = true;
		}

		private byte[] GetData(int index)
		{
			Debug.Assert(dataEmpty[index] == false);
			return data[index];
		}
		

		private class Frame : IFrame, IDisposable
		{
			private Pool pool;
			private uint id;
			private int dataindex;
			private bool dirty = false;
			private bool disposed = false;

			public Pool AssociatedPool { get { return pool; } }
			public uint Id { get { return id; } set { id = value; } }
			public bool Dirty { get { return dirty; } set { dirty = value; } }


			public Frame(Pool pool)
				: this(pool, -1) { }

			public Frame(Pool pool, int dataindex)
			{
				this.pool = pool;
				this.dataindex = dataindex;
			}

			~Frame()
			{
				Dispose(false);
			}
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
			private void Dispose(bool disposing)
			{
				if (disposed)
					return;

				Debug.Assert(this.Resident == false);
				disposed = true;
			}

			public byte[] Data
			{
				get
				{
					if (dataindex < 0)
						return null;
					else
						return pool.GetData(dataindex);
				}
			}

			public bool Resident
			{
				get { return dataindex >= 0; }
				set
				{
					if (this.Resident == value)
						return;

					if (value)
					{
						dataindex = pool.AllocSlot();
					}
					else
					{
						pool.FreeSlot(dataindex);
						dataindex = -1;
					}
				}
			}

		}
	}
}
