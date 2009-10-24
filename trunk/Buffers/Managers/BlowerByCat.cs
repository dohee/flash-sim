using System;
using System.Collections.Generic;
using System.Diagnostics;
using Buffers.Queues;
using Buffers.Memory;

namespace Buffers.Managers
{
	public sealed class BlowerByCat : FrameBasedManager
	{
		private FIFOQueue<IFrame> fifoQ = new FIFOQueue<IFrame>();
		private MultiConcatLRUQueue<uint> queryQ;


		public BlowerByCat(uint npages)
			: this(null, npages) { }

		public BlowerByCat(IBlockDevice dev, uint npages)
			: base(dev,npages)
		{
			queryQ = new MultiConcatLRUQueue<uint>(new ConcatenatedLRUQueue<uint>[]{
				new ConcatenatedLRUQueue<uint>(
					new FIFOQueue<uint>(), new FIFOQueue<uint>()),
				new ConcatenatedLRUQueue<uint>(
					new FIFOQueue<uint>(), new FIFOQueue<uint>())
			});
		}

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
				BlowResult r1 = TryBlow(false);
				if (r1 == BlowResult.Succeeded)
					break;

				BlowResult r2 = TryBlow(true);
				if (r2 == BlowResult.Succeeded)
					break;

				Debug.Assert(r1 != BlowResult.QueueIsEmpty ||
					r2 != BlowResult.QueueIsEmpty);
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



		private enum BlowResult
		{
			Succeeded,
			ExistsInAnotherQueue,
			QueueIsEmpty,
		}

		private class FrameWithRWQuery : Frame
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
				return isWrite? NodeOfRead : NodeOfWrite;
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
}
