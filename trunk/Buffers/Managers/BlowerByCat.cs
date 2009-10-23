using System;
using System.Collections.Generic;
using Buffers.Queues;
using Buffers.Memory;

namespace Buffers.Managers
{
	public sealed class BlowerByCat : FrameBasedManager
	{
		private FIFOQueue<IFrame> fifoQ = new FIFOQueue<IFrame>();
		private MultiConcatLRUQueue<RWQuery> queryQ;


		public BlowerByCat(uint npages)
			: this(null, npages) { }

		public BlowerByCat(IBlockDevice dev, uint npages)
			: base(dev,npages)
		{
			queryQ = new MultiConcatLRUQueue<RWQuery>(new ConcatenatedLRUQueue<RWQuery>[]{
				new ConcatenatedLRUQueue<RWQuery>(
					new FIFOQueue<RWQuery>(), new FIFOQueue<RWQuery>()),
				new ConcatenatedLRUQueue<RWQuery>(
					new FIFOQueue<RWQuery>(), new FIFOQueue<RWQuery>())
			});
		}

		protected override IFrame CreateFrame(uint pageid, int slotid)
		{
			return new FrameWithRWQuery(pageid, slotid);
		}

		protected override QueueNode<IFrame> OnHit(QueueNode<IFrame> node, bool isWrite)
		{
			uint a=node.Index;
			throw new NotImplementedException();
		}

		protected override QueueNode<IFrame> OnMiss(IFrame allocatedFrame, bool isWrite)
		{
			throw new NotImplementedException();
		}

		protected override void OnPoolFull()
		{
			throw new NotImplementedException();
		}

		protected override void DoFlush()
		{
			// FIXME do flush
			base.DoFlush();
		}



		private class FrameWithRWQuery : Frame
		{
			private QueueNode<RWQuery>? nodeR, nodeW;


			public FrameWithRWQuery(uint id) : base(id) { }
			public FrameWithRWQuery(uint id, int slotid) : base(id, slotid) { }

			public bool HasNodeOfRead { get { return nodeR.HasValue; } }
			public bool HasNodeOfWrite { get { return nodeW.HasValue; } }

			public void ClearNodeOfRead() { nodeR = null; }
			public void ClearNodeOfWrite() { nodeW = null; }

			public QueueNode<RWQuery> NodeOfRead
			{
				get { return nodeR.Value; }
				set { nodeR = value; }
			}
			public QueueNode<RWQuery> NodeOfWrite
			{
				get { return nodeW.Value; }
				set { nodeW = value; }
			}
		}
	}
}
