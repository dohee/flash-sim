using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffers.Queues;
using Buffers.Memory;

//CMFT. Determine the victim page by weighted IRR.
namespace Buffers.Managers
{
	class CMFT : BufferManagerBase
	{
		//the first version eliminate single queue.
		//LRUQueue single;        //Store the pages that was only referenced once. limited by a threshold. From 2Q
		protected Pool pool;

		//IRR queue is for getting IRR. Storing all the readed/writen queue that may be also in single, non-resident page will be stored for a while.
		//Only an auxiliary data structure, page id and dirty is needed. Read and write are stored in one IRRQueue
		IRRLRUQueue IRRQueue = new IRRLRUQueue();


		//Real data are all stored in map.
		protected Dictionary<uint, IRRFrame> map = new Dictionary<uint, IRRFrame>();

		public CMFT(uint npages)
			: this(null, npages)
		{
			pool = new Pool(npages, this.dev.PageSize, OnPoolFull);
		}

		public CMFT(IBlockDevice dev, uint npages)
			: base(dev)
		{
			pool = new Pool(npages, this.dev.PageSize, OnPoolFull);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="pageid"></param>
		/// <param name="result"></param>
		protected sealed override void DoRead(uint pageid, byte[] result)
		{
			IRRFrame irrframe;

			//if not in hash map
			if (!map.TryGetValue(pageid, out irrframe))
			{
				//add to hash map
				irrframe = new IRRFrame(pageid, pool.AllocSlot());
				map[pageid] = irrframe;

				//load the page
				dev.Read(pageid, pool[irrframe.DataSlotId]);
				pool[irrframe.DataSlotId].CopyTo(result, 0);

				//add to IRR queue
				Frame frame = new Frame(pageid);
				frame.Dirty = false;
				IRRQueue.Enqueue(frame);

				//(to be added) if the queue exceed a certain threshold, one frame should be kicked off.
			}
			else//in hash map
			{
				irrframe = map[pageid];

				if (!irrframe.Resident)     //miss non resident
				{
					irrframe.DataSlotId = pool.AllocSlot();
					dev.Read(pageid, pool[irrframe.DataSlotId]);
					pool[irrframe.DataSlotId].CopyTo(result, 0);
				}

				//update IRR
				uint irr = IRRQueue.accessIRR(irrframe);
				irrframe.ReadIRR = irr;	//possiblely 0, no effect
				if (irr == 0)
				{
					Frame frame = new Frame(pageid);
					frame.Dirty = false;
					IRRQueue.Enqueue(frame);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pageid"></param>
		/// <param name="data"></param>
		protected sealed override void DoWrite(uint pageid, byte[] data)
		{
			IRRFrame irrframe;

			//if not in hash map
			if (!map.TryGetValue(pageid, out irrframe))
			{
				//add to hash map
				irrframe = new IRRFrame(pageid, pool.AllocSlot());
				map[pageid] = irrframe;

				//add to IRR queue
				Frame frame = new Frame(pageid);
				frame.Dirty = true;
				IRRQueue.Enqueue(frame);

				//(to be added) if the queue exceed a certain threshold, one frame should be kicked off.
			}
			else//in hash map
			{
				irrframe = map[pageid];

				if (!irrframe.Resident)     //miss non resident allocate a slot
				{
					irrframe.DataSlotId = pool.AllocSlot();
				}

				//update IRR
				uint irr = IRRQueue.accessIRR(irrframe);
				irrframe.WriteIRR = irr;     //0 doesn't matter.
				if (irr == 0)
				{
					Frame frame = new Frame(pageid);
					frame.Dirty = true;
					IRRQueue.Enqueue(frame);
				}
			}

			data.CopyTo(pool[irrframe.DataSlotId], 0);
			irrframe.Dirty = true;
		}

		/// <summary>
		/// 
		/// </summary>
		void OnPoolFull()
		{
			Dictionary<uint, IRRFrame> residentMap = new Dictionary<uint, IRRFrame>();      //存储在内存的页面，供选择替换页面
			uint j = 0;
			//对resident的页面求出每个的recency
			foreach (Frame frame in IRRQueue)
			{
				j++;
				if (map[frame.Id].Resident)
				{
					if (frame.Dirty)
					{ 
						map[frame.Id].WriteRecency = j;
					}
					else
					{
						map[frame.Id].ReadRecency = j;
					}
					residentMap[frame.Id] = map[frame.Id];
				}
			}

			//在其中选出权重最低的页面
			double minPower = Double.MaxValue;
			IRRFrame minFrame = null;
			foreach (KeyValuePair<uint, IRRFrame> i in residentMap)
			{
				IRRFrame irrFrame = i.Value;
				double power = irrFrame.GetPower();
				if (power < minPower)
				{
					minFrame = irrFrame;
					minPower = power;
				}
			}

			//释放页面
			WriteIfDirty(minFrame);
			pool.FreeSlot(minFrame.DataSlotId);
			map[minFrame.Id].DataSlotId = -1;
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
