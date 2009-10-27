using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffers.Queues;
using Buffers.Memory;
using System.Diagnostics;

//Blower. Determine victim by two separate queues. each queue stores read and write operation separately. 
namespace Buffers.Managers
{
	class BlowerByLyf : BufferManagerBase
	{
		//the first version eliminate single queue.
		//LRUQueue single;        //Store the pages that was only referenced once. limited by a threshold. From 2Q
		protected Pool pool;

		//
		//An auxiliary data structure, only page id is used. store read and write operation separately.
		LinkedList<uint> readQueue = new LinkedList<uint>();
		LinkedList<uint> writeQueue = new LinkedList<uint>();

		LinkedListNode<uint> lastReadResident;
		LinkedListNode<uint> lastWriteResident;

		uint windowSize;

		int quota = 0;			//为非负就是在read窗口里，否则就是在write窗口里。
		

		//Real data are all stored in map.
		public Dictionary<uint, BlowFrame> map = new Dictionary<uint, BlowFrame>();

		public BlowerByLyf(uint npages)
			: this(null, npages)
		{			
		}

		public BlowerByLyf(IBlockDevice dev, uint npages)
			: base(dev)
		{
			pool = new Pool(npages, this.dev.PageSize, OnPoolFull);
			windowSize = npages / 2;
		}

		public override string Name { get { return "Blow by lyf"; } }

		bool inReadWindow(uint frameId)
		{
			//找到quota的页面
			LinkedListNode<uint> frame = lastReadResident;
			for (int i = 0; i < quota; i++)
			{
				if (frame.Previous != null)
				{
					frame = frame.Previous;
				}
			}

			//看看是不是在窗口里
			for (int i = 0; i < windowSize; i++)
			{
				if (frame.Value == frameId)
				{
					return true;
				}
				if (frame.Next != null)
				{
					frame = frame.Next;
				}
			}
			return false;

		}

		bool inWriteWindow(uint frameId)
		{
			//找到quota的页面
			LinkedListNode<uint> frame = lastWriteResident;
			for (int i = 0; i < -quota; i++)
			{
				if (frame.Previous != null)
				{
					frame = frame.Previous;
				}
			}

			//看看是不是在窗口里
			for (int i = 0; i < windowSize; i++)
			{
				if (frame.Value == frameId)
				{
					return true;
				}
				if (frame.Next != null)
				{
					frame = frame.Next;
				}
			}
			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pageid"></param>
		/// <param name="result"></param>
		protected sealed override void DoRead(uint pageid, byte[] result)
		{
			BlowFrame blowFrame;

			//if not in hash map
			if (!map.TryGetValue(pageid, out blowFrame))
			{
				//add to hash map
				blowFrame = new BlowFrame(pageid, pool.AllocSlot());
				map[pageid] = blowFrame;

				//load the page
				dev.Read(pageid, pool[blowFrame.DataSlotId]);
				pool[blowFrame.DataSlotId].CopyTo(result, 0);

				//add to queue;
				readQueue.AddFirst(pageid);
				blowFrame.readNode = readQueue.First;
				if (lastReadResident == null)
				{
					lastReadResident = readQueue.First;
				}

				//(to be added) if the queue exceed a certain threshold, one frame should be kicked off.
			}
			else//in hash map
			{
				blowFrame = map[pageid];

				if (!blowFrame.Resident)     //miss non resident
				{
					blowFrame.DataSlotId = pool.AllocSlot();
					dev.Read(pageid, pool[blowFrame.DataSlotId]);
					pool[blowFrame.DataSlotId].CopyTo(result, 0);
				}

				//update 
				Debug.Assert(blowFrame.readNode != null);
				if (inReadWindow(blowFrame.Id))
				{
					quota += -1;
				}

				readQueue.AddFirst(blowFrame.Id);
				if (lastReadResident.Value == blowFrame.readNode.Value)
				{
					Debug.Assert(lastReadResident == blowFrame.readNode);
					lastReadResident = lastReadResident.Previous;
				}
				readQueue.Remove(blowFrame.readNode);
				blowFrame.readNode = readQueue.First;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pageid"></param>
		/// <param name="data"></param>
		protected sealed override void DoWrite(uint pageid, byte[] data)
		{
			BlowFrame blowFrame;

			//if not in hash map
			if (!map.TryGetValue(pageid, out blowFrame))
			{
				//add to hash map
				blowFrame = new BlowFrame(pageid, pool.AllocSlot());
				map[pageid] = blowFrame;

				//add to wirte queue
				writeQueue.AddFirst(pageid);
				blowFrame.writeNode = writeQueue.First;
				if (lastWriteResident == null)
				{
					lastWriteResident = writeQueue.First;
				}

				//(to be added) if the queue exceed a certain threshold, one frame should be kicked off.
			}
			else//in hash map
			{
				blowFrame = map[pageid];

				if (!blowFrame.Resident)     //miss non resident allocate a slot
				{
					blowFrame.DataSlotId = pool.AllocSlot();
				}

				//update
				Debug.Assert(blowFrame.writeNode != null);
				if (inWriteWindow(blowFrame.Id))
				{
					quota += 3;//TODO
				}

				writeQueue.AddFirst(blowFrame.Id);
				if (lastWriteResident.Value == blowFrame.Id)
				{
					lastWriteResident = lastWriteResident.Previous;
				}
				writeQueue.Remove(blowFrame.writeNode);
				blowFrame.writeNode = writeQueue.First;
			}

			blowFrame.Dirty = true;
			data.CopyTo(pool[blowFrame.DataSlotId], 0);
		}

		/// <summary>
		/// 
		/// </summary>
		void OnPoolFull()
		{
			LinkedListNode<uint> readFrame = lastReadResident;
			LinkedListNode<uint> writeFrame = lastWriteResident;

			for (; ; )
			{
				
			}

			//释放页面
			//WriteIfDirty(minFrame);
			//pool.FreeSlot(minFrame.DataSlotId);
			//map[minFrame.Id].DataSlotId = -1;
		}


		protected override void DoFlush()
		{
			foreach (var entry in map)
			{
				if (entry.Value.Resident)
				{
					WriteIfDirty(entry.Value);
				}
			}
		}

		protected void WriteIfDirty(IFrame frame)
		{
			if (frame.Dirty)
			{
				dev.Write(frame.Id, pool[frame.DataSlotId]);
				frame.Dirty = false;
			}
		}
	}
}
