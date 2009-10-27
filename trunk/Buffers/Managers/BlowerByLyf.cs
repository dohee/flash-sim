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

		int quota_ = 0;			//为非负就是在read窗口里，否则就是在write窗口里。
	
		//Real data are all stored in map.
		public Dictionary<uint, Frame> map = new Dictionary<uint, Frame>();

		public BlowerByLyf(uint npages)
			: this(null, npages)
		{			
		}

		public BlowerByLyf(IBlockDevice dev, uint npages)
			: base(dev)
		{
			pool = new Pool(npages, this.dev.PageSize, OnPoolFull);
			windowSize = npages *1/ 2;
		}

		public override string Name { get { return "Blow by lyf"; } }

		private int Quota
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


		int LastIndexOfResident(IList<uint> list)
		{
			for (int i = list.Count - 1; i >= 0; i--)
			{
				if (map[list[i]].Resident)
					return i;
			}
			return -1;
		}


		bool IsInReadWindow(uint pageid)
		{
			int endResidentIndex = LastIndexOfResident(readQueue) + 1;
			int begin;
			
			if (Quota < 0)
				begin = endResidentIndex;
			else
				begin = Math.Max(0, endResidentIndex - Quota);

			int index = readQueue.IndexOf(pageid, begin,
				Math.Min((int)windowSize, readQueue.Count - begin));

			//Console.WriteLine(Math.Min((int)windowSize, readQueue.Count - begin));
			return index >= 0;
		}

		bool IsInWriteWindow(uint pageid)
		{
			int endResidentIndex = LastIndexOfResident(writeQueue) + 1;
			int begin;

			if (-Quota < 0)
				begin = endResidentIndex;
			else
				begin = Math.Max(0, endResidentIndex - (-Quota));

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
					Quota += -1;
				}

				
				if (!frame.Resident)     //miss non resident
				{
					frame.DataSlotId = pool.AllocSlot();
					dev.Read(pageid, pool[frame.DataSlotId]);
					pool[frame.DataSlotId].CopyTo(result, 0);
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
				writeQueue.Insert(0,pageid);

				//(to be added) if the queue exceed a certain threshold, one frame should be kicked off.
			}
			else//in hash map
			{
				frame = map[pageid];

				//update
				if (IsInWriteWindow(pageid))
				{
					Quota += 3;//TODO
				}

				if (!frame.Resident)     //miss non resident allocate a slot
				{
					frame.DataSlotId = pool.AllocSlot();
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
				if(item==targetFrame)
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

			Quota = tmpQuota;
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
