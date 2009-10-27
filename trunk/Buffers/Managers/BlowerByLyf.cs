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

		bool IsInReadWindow(uint frameId)
		{
			if (lastReadResident == null)
				return false;

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

		bool IsInWriteWindow(uint frameId)
		{
			//找到quota的页面
			if (lastWriteResident == null)
				return false;

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
				blowFrame.ReadNode = readQueue.First;
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
				if (IsInReadWindow(blowFrame.Id))
				{
					quota += -1;
				}

				readQueue.AddFirst(blowFrame.Id);
				if (lastReadResident == blowFrame.ReadNode)
				{
					lastReadResident = lastReadResident.Previous;
				}
				if (blowFrame.ReadNode!=null)
					readQueue.Remove(blowFrame.ReadNode);

				blowFrame.ReadNode = readQueue.First;
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
				blowFrame.WriteNode = writeQueue.First;
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
				if (IsInWriteWindow(blowFrame.Id))
				{
					quota += 3;//TODO
				}

				writeQueue.AddFirst(blowFrame.Id);
				if (lastWriteResident == blowFrame.WriteNode)
				{
					lastWriteResident = lastWriteResident.Previous;
				}
				if (blowFrame.WriteNode!=null)
					writeQueue.Remove(blowFrame.WriteNode);

				blowFrame.WriteNode = writeQueue.First;
			}

			blowFrame.Dirty = true;
			data.CopyTo(pool[blowFrame.DataSlotId], 0);
		}


		static bool IsInQueue(LinkedList<uint> queue, uint targetFrame, uint lastFrame)
		{
			foreach (uint item in queue)
			{
				if(item==targetFrame)
					return true;
				if (item == lastFrame)
					return false;
			}
			return false;//ifnull
		}
		/// <summary>
		/// 
		/// </summary>
		void OnPoolFull()
		{
			LinkedListNode<uint> readFrame = lastReadResident;
			LinkedListNode<uint> writeFrame = lastWriteResident;

			uint victim = uint.MaxValue;
			for (; ; )
			{
				if (quota >= 0)
				{
					quota--;

					if (readFrame != null)
					{
						bool isResident = map[readFrame.Value].Resident;
						bool isInQueue = false;

						if (writeFrame != null)
							isInQueue = IsInQueue(writeQueue, readFrame.Value, writeFrame.Value);

						if (isResident && !isInQueue)
						{
							victim = readFrame.Value;

							if (readFrame == lastReadResident)
								lastReadResident = lastReadResident.Previous;

							break;
						}
						readFrame = readFrame.Previous;
					}
				}
				else
				{
					quota++;

					if (writeFrame != null)
					{
						bool isResident = map[writeFrame.Value].Resident;
						bool isInQueue = false;

						if (readFrame != null)
							isInQueue = IsInQueue(readQueue, writeFrame.Value, readFrame.Value);

						if (isResident && !isInQueue)
						{
							victim = writeFrame.Value;

							if (writeFrame == lastWriteResident)
								lastWriteResident = lastWriteResident.Previous;

							break;
						}
						if (writeFrame.Previous != null)
						{
							writeFrame = writeFrame.Previous;
						}
					}
				}
			}


			//释放页面
			WriteIfDirty(map[victim]);
			pool.FreeSlot(map[victim].DataSlotId);
			map[victim].DataSlotId = -1;


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
