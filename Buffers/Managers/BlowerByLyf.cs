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
		List<uint> readQueue = new List<uint>();
		List<uint> writeQueue = new List<uint>();

		uint windowSize;

		//队列长度的限制，模仿Tn而来，代替quota
		int readLimit = 4;
		int writeLimit = 0;
		bool isLimitRead = true;		//观察此值来决定是不是队尾。如果是true，writeLimit的值就无意义了，认为队尾。
		//int quota_ = 0;			//为非负就是在read窗口里，否则就是在write窗口里。

		//data are all stored in map.
		public Dictionary<uint, Frame> map = new Dictionary<uint, Frame>();


		//由limit计算出quota的值
		public int Quota
		{
			get
			{
				if (isLimitRead)
				{
					int readResidentSize = LastIndexOfResident(readQueue) + 1;
					return (readResidentSize - readLimit);
				}
				else
				{
					int writeResidentSize = LastIndexOfResident(writeQueue) + 1;
					return (writeLimit - writeResidentSize);
				}
			}

		}

		//调整limit; 正数是增加readLimit，或者减少writeLimit。最多不能超过队列长度;至少一个队列limit是队列长度
		void changeLimit(int offset)
		{
			int readResidentSize = LastIndexOfResident(readQueue) + 1;
			int writeResidentSize = LastIndexOfResident(writeQueue) + 1;
			int left = 0;	//存储offset剩余量

			if (isLimitRead)
			{
				readLimit += offset;
				if (readLimit < 0)
				{
					readLimit = 0;
				}
				if (readLimit > readResidentSize)//当前resident队列的长度
				{
					left = readLimit - readResidentSize;
					readLimit = readResidentSize;
					isLimitRead = false;
					writeLimit = Math.Max(writeResidentSize - left, 0);
				}
			}
			else
			{
				writeLimit += -offset;
				if (writeLimit < 0)
				{
					writeLimit = 0;
				}
				if (writeLimit > writeResidentSize)
				{
					left = writeLimit - writeResidentSize;
					writeLimit = writeResidentSize;
					isLimitRead = true;
					readLimit = Math.Max(readResidentSize - left, 0);
				}
			}
			Debug.Assert(readLimit >= 0 && writeLimit >= 0);
			System.Console.WriteLine("readLimit" + readLimit + ", writeLimit" + writeLimit +
			    ", readResidentSize" + readResidentSize + ", writeResidentSize" + writeResidentSize);
		}

		public BlowerByLyf(uint npages)
			: this(null, npages)
		{
		}

		public BlowerByLyf(IBlockDevice dev, uint npages)
			: base(dev)
		{
			pool = new Pool(npages, this.dev.PageSize, OnPoolFull);
			windowSize = npages * 1 / 2;
			//readLimit = (int)npages / 3;
			//writeLimit = (int)npages * 2 / 3;
		}

		public override string Name { get { return "Blow by lyf"; } }

		/*		private int Quota
				{
					get { return quota_; }
					set
					{
						quota_ = value;
						int maxQuota = LastIndexOfResident(readQueue);
						int minQuota = LastIndexOfResident(writeQueue);
						//if (quota_ >= maxQuota)
						//	quota_ = maxQuota;
						//if (quota_ <= -minQuota)
						//	quota_ = -minQuota;

						Console.WriteLine(quota_);
					}
				}
		*/

		int LastIndexOfResident(IList<uint> list)
		{
			int index=-1;
			for (int i = list.Count - 1; i >= 0; i--)
			{
				if (map[list[i]].Resident)
				{
					index = i;
					break;
				}
			}

#if DEBUG //判断是不是前面还有nonresident的。
			for (int i = index; i >= 0; i--)
			{
				if (!map[list[i]].Resident)
				{
					Debug.Assert(false);
				}
			}
#endif
			Debug.Assert(index <= pool.NPages);
			return index;
			//return -1;
		}


		bool IsInReadWindow(uint pageid)
		{
			int endResidentIndex = LastIndexOfResident(readQueue) + 1;
			int begin;

			if (isLimitRead)
				begin = readLimit;
			else
				begin = endResidentIndex;

			int index = readQueue.IndexOf(pageid, begin,
				Math.Min((int)windowSize, readQueue.Count - begin));

			//Console.WriteLine(Math.Min((int)windowSize, readQueue.Count - begin));
			return index >= 0;
		}

		bool IsInWriteWindow(uint pageid)
		{
			int endResidentIndex = LastIndexOfResident(writeQueue) + 1;
			int begin;

			if (isLimitRead)
				begin = endResidentIndex;
			else
				begin = writeLimit;

			int index = writeQueue.IndexOf(pageid, begin,
				Math.Min((int)windowSize, writeQueue.Count - begin));
			return index >= 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pageid"></param>
		/// <param name="result"></param>
		protected sealed override void DoRead(uint pageid, byte[] result)
		{
			Frame frame;

			//if not in hash map
			if (!map.TryGetValue(pageid, out frame))
			{
				//add to hash map
				frame = new Frame(pageid, pool.AllocSlot());
				map[pageid] = frame;

				//load the page
				dev.Read(pageid, pool[frame.DataSlotId]);
				pool[frame.DataSlotId].CopyTo(result, 0);

				//add to queue;
				readQueue.Insert(0, pageid);

				//(to be added) if the queue exceed a certain threshold, one frame should be kicked off.
			}
			else//in hash map
			{
				frame = map[pageid];

				//update 
				if (IsInReadWindow(pageid))
				{
					changeLimit(1);
				}


				if (!frame.Resident)     //miss non resident
				{
					//在writeQueue里前移到resident里
					int index = writeQueue.IndexOf(pageid);
					if (index != -1)
					{
						writeQueue.RemoveAt(index);
					}


					frame.DataSlotId = pool.AllocSlot();
					dev.Read(pageid, pool[frame.DataSlotId]);
					pool[frame.DataSlotId].CopyTo(result, 0);

					//移到resident的末尾
					if (index != -1)
					{
						int lastWriteResident = LastIndexOfResident(writeQueue);
						Debug.Assert(index > lastWriteResident);
						writeQueue.Insert(lastWriteResident + 1, pageid);
					}
				}
				readQueue.Remove(pageid);
				readQueue.Insert(0, pageid);

			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pageid"></param>
		/// <param name="data"></param>
		protected sealed override void DoWrite(uint pageid, byte[] data)
		{
			Frame frame;

			//if not in hash map
			if (!map.TryGetValue(pageid, out frame))
			{
				//add to hash map
				frame = new Frame(pageid, pool.AllocSlot());
				map[pageid] = frame;

				//add to wirte queue
				writeQueue.Insert(0, pageid);

				//(to be added) if the queue exceed a certain threshold, one frame should be kicked off.
			}
			else//in hash map
			{
				frame = map[pageid];

				//update
				if (IsInWriteWindow(pageid))
				{
					changeLimit(-3);//TODO
				}

				
				
				


				if (!frame.Resident)     //miss non resident allocate a slot
				{
					//在readQueue里前移到resident里
					int index = readQueue.IndexOf(pageid);
					if (index != -1)
					{
						readQueue.RemoveAt(index);
					}

					frame.DataSlotId = pool.AllocSlot();

					//移到resident的末尾
					if (index != -1)
					{
						int lastReadResident = LastIndexOfResident(readQueue);
						Debug.Assert(index > lastReadResident);
						readQueue.Insert(lastReadResident + 1, pageid);
					}
				}

				writeQueue.Remove(pageid);
				writeQueue.Insert(0, pageid);
			}

			frame.Dirty = true;
			data.CopyTo(pool[frame.DataSlotId], 0);
		}


		static bool IsInQueue(LinkedList<uint> queue, uint targetFrame, uint lastFrame)
		{
			foreach (uint item in queue)
			{
				if (item == targetFrame)
					return true;
				if (item == lastFrame)
					return false;
			}
			return false;//ifnull
		}

		void OnPoolFull()
		{
			int lastReadResident = LastIndexOfResident(readQueue);
			int lastWriteResident = LastIndexOfResident(writeQueue);
			int checkReadIndex = lastReadResident, checkWriteIndex = lastWriteResident;
			uint victim = uint.MaxValue;
			int tmpQuota = Quota;

			while (true)
			{
				if (tmpQuota >= 0)
				{
					tmpQuota--;

					if (checkReadIndex == -1)
						continue;

					uint pageid = readQueue[checkReadIndex];
					checkReadIndex--;

					if (!map[pageid].Resident)
						continue;

					if (writeQueue.IndexOf(pageid, 0, checkWriteIndex + 1) >= 0)
						continue;

					victim = pageid;
					break;
				}
				else
				{
					tmpQuota++;

					if (checkWriteIndex == -1)
						continue;

					uint pageid = writeQueue[checkWriteIndex];
					checkWriteIndex--;

					if (!map[pageid].Resident)
						continue;

					if (readQueue.IndexOf(pageid, 0, checkReadIndex + 1) >= 0)
						continue;

					victim = pageid;
					break;
				}
			}

			//释放页面
			WriteIfDirty(map[victim]);
			pool.FreeSlot(map[victim].DataSlotId);
			map[victim].DataSlotId = -1;

			int index = readQueue.IndexOf(victim);
			if (index != -1 && index <= lastReadResident)
			{
				readQueue.Insert(lastReadResident + 1, victim);
				readQueue.RemoveAt(index);
			}

			index = writeQueue.IndexOf(victim);
			if (index != -1 && index <= lastWriteResident)
			{
				writeQueue.Insert(lastWriteResident + 1, victim);
				writeQueue.RemoveAt(index);
			}

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

		int ResidentCount(IEnumerable<uint> list)
		{
			int sum = 0;

			foreach (uint item in list)
				if (map[item].Resident)
					sum++;

			return sum;
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
