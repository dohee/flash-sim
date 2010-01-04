using System;
using System.Collections.Generic;
using System.Diagnostics;
using Buffers.Queues;
using Buffers.Memory;

namespace Buffers.Managers
{
	public sealed class BlowerByCat : FrameBasedManager
	{
		/// <summary>
		/// 用于存储页面信息及页面内容的队列，与 map 的内容对应。
		/// </summary>
		private FIFOQueue<IFrame> fifoQ = new FIFOQueue<IFrame>();

		/// <summary>
		/// 存放页面访问请求的队列。共 4 个队列，
		/// 0/1 号分别存放近期的读/写请求，可吹；
		/// 2/3 号分别存放以往的读/写请求，不可吹。
		/// </summary>
		private MultiConcatLRUQueue<uint> queryQ;

		/// <summary>
		/// 吹读队列的限额。>0 可吹读队列，否则吹写队列。
		/// </summary>
		private int blowReadQuota = 1;


		public BlowerByCat(uint npages)
			: this(null, npages) { }

		public BlowerByCat(IBlockDevice dev, uint npages)
			: base(dev, npages)
		{
			queryQ = new MultiConcatLRUQueue<uint>(new ConcatenatedLRUQueue<uint>[]{
				new ConcatenatedLRUQueue<uint>(
					new FIFOQueue<uint>(), new FIFOQueue<uint>()),
				new ConcatenatedLRUQueue<uint>(
					new FIFOQueue<uint>(), new FIFOQueue<uint>()),
				new ConcatenatedLRUQueue<uint>(
					new FIFOQueue<uint>()),
				new ConcatenatedLRUQueue<uint>(
					new FIFOQueue<uint>())
			});
		}

		public override string Name { get { return "Blower"; } }
		public override string Description { get { return "By=Cat,NPages=" + pool.NPages; } }


		protected override IFrame CreateFrame(uint pageid, int slotid)
		{
			return new FrameWithRWQueryForBlowerByCat(pageid, slotid);
		}

		protected override QueueNode<IFrame> OnHit(QueueNode<IFrame> node, bool isWrite)
		{
			bool isRead = !isWrite;
			FrameWithRWQueryForBlowerByCat f =
				node.ListNode.Value as FrameWithRWQueryForBlowerByCat;

			if (f.HasNodeOf(isWrite))
			{
				QueueNode<uint> rwnode = f.GetNodeOf(isWrite);
				uint queueIndex = queryQ.GetRoute(rwnode);

				queryQ.Dequeue(rwnode);
	
				/*if (queueIndex == 2U && isRead)
					blowReadQuota -= 1;
				else if (queueIndex == 3U && isWrite)
					blowReadQuota += 3;*/
			}

			if (!f.Resident)
			{
				f.DataSlotId = pool.AllocSlot();
				if (!isWrite)
					dev.Read(f.Id, pool[f.DataSlotId]);
			}

			f.SetNodeOf(isWrite, queryQ.Enqueue((isWrite ? 1 : 0), f.Id));
			return node;
		}

		protected override QueueNode<IFrame> OnMiss(IFrame allocatedFrame, bool isWrite)
		{
			FrameWithRWQueryForBlowerByCat f =
				allocatedFrame as FrameWithRWQueryForBlowerByCat;
			f.SetNodeOf(isWrite, queryQ.Enqueue((isWrite ? 1 : 0), f.Id));
			return fifoQ.Enqueue(f);
		}

		protected override void OnPoolFull()
		{
			while (true)
			{
				BlowResult r;

				if (blowReadQuota > 0)
				{
					r = TryBlow(false);
					--blowReadQuota;
				}
				else
				{
					r = TryBlow(true);
					blowReadQuota+=3;
				}

				if (r == BlowResult.Succeeded)
					break;
			}
		}

		private BlowResult TryBlow(bool isWrite)
		{
			int queueIndex = isWrite ? 1 : 0;
			int otherQueueIndex = isWrite ? 0 : 1;
			int nrQueueIndex = isWrite ? 3 : 2;
			int otherNRQueueIndex = isWrite ? 2 : 3;

			// 空队列判断
			if (queryQ.GetFrontSize(queueIndex) == 0)
				return BlowResult.QueueIsEmpty;

			// 实施吹风
			QueueNode<uint> rwnode = queryQ.BlowOneItem(queueIndex);
			FrameWithRWQueryForBlowerByCat f =
				map[rwnode.ListNode.Value].ListNode.Value as FrameWithRWQueryForBlowerByCat;
			f.SetNodeOf(isWrite, rwnode);

			// 找出该项在另一条队列的位置
			IList<uint> otherRoute = null;
			if (f.HasNodeOf(!isWrite))
				otherRoute = queryQ.GetRoutePath(f.GetNodeOf(!isWrite));

			// 不释放存在于另一条队列的头队列的项
			if (otherRoute != null &&
				otherRoute[otherRoute.Count - 1] == otherQueueIndex &&
				otherRoute[otherRoute.Count - 2] == 0)
				return BlowResult.ExistsInAnotherQueue;
			
			// 实施释放
			WriteIfDirty(f);
			pool.FreeSlot(f.DataSlotId);
			f.DataSlotId = -1;

			// 重设被释放的项在队列中的位置
			f.SetNodeOf(isWrite, queryQ.Enqueue(nrQueueIndex,
				queryQ.Dequeue(f.GetNodeOf(isWrite))));

			if (otherRoute != null &&
				otherRoute[otherRoute.Count - 1] == otherQueueIndex &&
				otherRoute[otherRoute.Count - 2] == 1)
			{
				f.SetNodeOf(!isWrite, queryQ.Enqueue(otherNRQueueIndex,
					queryQ.Dequeue(f.GetNodeOf(!isWrite))));
			}

			// 限制 NR 队列的长度
			while (queryQ.GetFrontSize(2) > pool.NPages / 2)
			{
				uint id = queryQ.Dequeue(2);
				FrameWithRWQueryForBlowerByCat dying =
					map[id].ListNode.Value as FrameWithRWQueryForBlowerByCat;
				dying.ClearNodeOfRead();

				if (!dying.HasNodeOfWrite)
					map.Remove(id);
			}
			while (queryQ.GetFrontSize(3) > pool.NPages / 2)
			{
				uint id = queryQ.Dequeue(3);
				FrameWithRWQueryForBlowerByCat dying =
					map[id].ListNode.Value as FrameWithRWQueryForBlowerByCat;
				dying.ClearNodeOfWrite();

				if (!dying.HasNodeOfRead)
					map.Remove(id);
			}

			// 成功
			return BlowResult.Succeeded;
		}

		protected override void DoFlush()
		{
			// TODO do flush
			base.DoFlush();
		}
	}


	[Obsolete()]
	public sealed class OldBlowerByCat : FrameBasedManager
	{
		private FIFOQueue<IFrame> fifoQ = new FIFOQueue<IFrame>();
		private MultiConcatLRUQueue<uint> queryQ;
		private bool blowWrite = false;


		public OldBlowerByCat(uint npages)
			: this(null, npages) { }

		public OldBlowerByCat(IBlockDevice dev, uint npages)
			: base(dev, npages)
		{
			queryQ = new MultiConcatLRUQueue<uint>(new ConcatenatedLRUQueue<uint>[]{
				new ConcatenatedLRUQueue<uint>(
					new FIFOQueue<uint>(), new FIFOQueue<uint>()),
				new ConcatenatedLRUQueue<uint>(
					new FIFOQueue<uint>(), new FIFOQueue<uint>())
			});
		}

		public override string Name { get { return "OldBlower"; } }
		public override string Description { get { return "NPages=" + pool.NPages; } }


		protected override IFrame CreateFrame(uint pageid, int slotid)
		{
			return new FrameWithRWQueryForBlowerByCat(pageid, slotid);
		}

		protected override QueueNode<IFrame> OnHit(QueueNode<IFrame> node, bool isWrite)
		{
			FrameWithRWQueryForBlowerByCat f =
				node.ListNode.Value as FrameWithRWQueryForBlowerByCat;

			if (f.HasNodeOf(isWrite))
				f.SetNodeOf(isWrite, queryQ.Access(f.GetNodeOf(isWrite)));
			else
				f.SetNodeOf(isWrite, queryQ.Enqueue((isWrite ? 1 : 0), f.Id));

			return node;
		}

		protected override QueueNode<IFrame> OnMiss(IFrame allocatedFrame, bool isWrite)
		{
			FrameWithRWQueryForBlowerByCat f =
				allocatedFrame as FrameWithRWQueryForBlowerByCat;
			f.SetNodeOf(isWrite, queryQ.Enqueue((isWrite ? 1 : 0), f.Id));
			return fifoQ.Enqueue(f);
		}

		protected override void OnPoolFull()
		{
			while (true)
			{
				BlowResult r = TryBlow(blowWrite);
				blowWrite = !blowWrite;
				if (r == BlowResult.Succeeded)
					break;
			}
		}

		private BlowResult TryBlow(bool isWrite)
		{
			int queueIndex = isWrite ? 1 : 0;
			int otherQueueIndex = isWrite ? 0 : 1;

			if (queryQ.GetFrontSize(queueIndex) == 0)
				return BlowResult.QueueIsEmpty;

			QueueNode<uint> rwnode = queryQ.BlowOneItem(queueIndex);
			FrameWithRWQueryForBlowerByCat f =
				map[rwnode.ListNode.Value].ListNode.Value as FrameWithRWQueryForBlowerByCat;
			f.SetNodeOf(isWrite, rwnode);

			if (f.HasNodeOf(!isWrite))
			{
				var route = queryQ.GetRoutePath(f.GetNodeOf(!isWrite));
				Debug.Assert(route[route.Count - 1] == otherQueueIndex);
				if (route[route.Count - 2] == 0)
					return BlowResult.ExistsInAnotherQueue;
			}

			WriteIfDirty(f);
			pool.FreeSlot(f.DataSlotId);
			f.DataSlotId = -1;
			map.Remove(f.Id);

			if (f.HasNodeOfRead)
				queryQ.Dequeue(f.NodeOfRead.Value);
			if (f.HasNodeOfWrite)
				queryQ.Dequeue(f.NodeOfWrite.Value);

			return BlowResult.Succeeded;
		}

		protected override void DoFlush()
		{
			// TODO do flush
			base.DoFlush();
		}
	}


	internal enum BlowResult
	{
		Succeeded,
		ExistsInAnotherQueue,
		QueueIsEmpty,
	}

	internal sealed class FrameWithRWQueryForBlowerByCat
		: Memory.FrameWithRWInfo<QueueNode<uint>?>
	{
		public FrameWithRWQueryForBlowerByCat(uint id) : base(id) { }
		public FrameWithRWQueryForBlowerByCat(uint id, int slotid) : base(id, slotid) { }

		public bool HasNodeOfRead { get { return NodeOfRead.HasValue; } }
		public bool HasNodeOfWrite { get { return NodeOfWrite.HasValue; } }

        public void ClearNodeOfRead() { NodeOfRead = null; }
        public void ClearNodeOfWrite() { NodeOfWrite = null; }

		public bool HasNodeOf(bool isWrite)
		{
			return !isWrite ? HasNodeOfRead : HasNodeOfWrite;
		}
		public QueueNode<uint> GetNodeOf(bool isWrite)
		{
			return !isWrite ? NodeOfRead.Value : NodeOfWrite.Value;
		}
		public void SetNodeOf(bool isWrite, QueueNode<uint> value)
		{
			if (!isWrite) NodeOfRead = value;
			else NodeOfWrite = value;
		}
		public void ClearNodeOf(bool isWrite)
		{
			if (!isWrite) ClearNodeOfRead();
			else ClearNodeOfWrite();
		}
	}
}
