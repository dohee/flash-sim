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
			return new FrameWithRWQuery(pageid, slotid);
		}

		protected override QueueNode<IFrame> OnHit(QueueNode<IFrame> node, bool isWrite)
		{
			bool isRead = !isWrite;
			FrameWithRWQuery f = node.ListNode.Value as FrameWithRWQuery;

			if (f.HasNodeOf(isWrite))
			{
				f.SetNodeOf(isWrite, queryQ.Enqueue(1, queryQ.Dequeue(f.GetNodeOf(isWrite))));
				//TODO enlarge?
			}
			else
			{
				f.SetNodeOf(isWrite, queryQ.Enqueue((isWrite ? 1 : 0), f.Id));
			}

			if (!f.Resident)
			{
				f.DataSlotId = pool.AllocSlot();
				if (!isWrite)
					dev.Read(f.Id, pool[f.DataSlotId]);
			}

			return node;
		}

		protected override QueueNode<IFrame> OnMiss(IFrame allocatedFrame, bool isWrite)
		{
			FrameWithRWQuery f = allocatedFrame as FrameWithRWQuery;
			f.SetNodeOf(isWrite, queryQ.Enqueue((isWrite ? 1 : 0), f.Id));
			return fifoQ.Enqueue(f);
		}

		protected override void OnPoolFull()
		{
			throw new NotImplementedException();
		}

	}

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
		public override string Description { get { return "By=Cat,NPages=" + pool.NPages; } }


		protected override IFrame CreateFrame(uint pageid, int slotid)
		{
			return new FrameWithRWQuery(pageid, slotid);
		}

		protected override QueueNode<IFrame> OnHit(QueueNode<IFrame> node, bool isWrite)
		{
			FrameWithRWQuery f = node.ListNode.Value as FrameWithRWQuery;

			if (f.HasNodeOf(isWrite))
				f.SetNodeOf(isWrite, queryQ.Access(f.GetNodeOf(isWrite)));
			else
				f.SetNodeOf(isWrite, queryQ.Enqueue((isWrite ? 1 : 0), f.Id));

			return node;
		}

		protected override QueueNode<IFrame> OnMiss(IFrame allocatedFrame, bool isWrite)
		{
			FrameWithRWQuery f = allocatedFrame as FrameWithRWQuery;
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
			FrameWithRWQuery f = map[rwnode.ListNode.Value].ListNode.Value as FrameWithRWQuery;
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
				queryQ.Dequeue(f.NodeOfRead);
			if (f.HasNodeOfWrite)
				queryQ.Dequeue(f.NodeOfWrite);

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

	internal class FrameWithRWQuery : Frame
	{
		private QueueNode<uint>? nodeR, nodeW;


		public FrameWithRWQuery(uint id) : base(id) { }
		public FrameWithRWQuery(uint id, int slotid) : base(id, slotid) { }

		public bool HasNodeOfRead { get { return nodeR.HasValue; } }
		public bool HasNodeOfWrite { get { return nodeW.HasValue; } }

		public void ClearNodeOfRead() { nodeR = null; }
		public void ClearNodeOfWrite() { nodeW = null; }

		public QueueNode<uint> NodeOfRead
		{
			get { return nodeR.Value; }
			set { nodeR = value; }
		}
		public QueueNode<uint> NodeOfWrite
		{
			get { return nodeW.Value; }
			set { nodeW = value; }
		}

		public bool HasNodeOf(bool isWrite)
		{
			return isWrite ? HasNodeOfRead : HasNodeOfWrite;
		}
		public QueueNode<uint> GetNodeOf(bool isWrite)
		{
			return isWrite ? NodeOfRead : NodeOfWrite;
		}
		public void SetNodeOf(bool isWrite, QueueNode<uint> value)
		{
			if (isWrite) NodeOfRead = value;
			else NodeOfWrite = value;
		}
		public void ClearNodeOf(bool isWrite)
		{
			if (isWrite) ClearNodeOfRead();
			else ClearNodeOfWrite();
		}
	}
}
