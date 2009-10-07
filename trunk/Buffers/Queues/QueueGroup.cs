using System.Collections.Generic;
using Buffers.Memory;

namespace Buffers.Queues
{
	public abstract class QueueGroup : QueueBase
	{
		protected List<IQueue> queues = new List<IQueue>();
		protected List<Route> routes = new List<Route>();
		private uint curRouteKey, curRouteValue;

		protected sealed override void OnCountQueue()
		{
			for (int i = 0; i < queues.Count; i++)
			{
				curRouteKey = (uint)i;
				curRouteValue = 0;
				queues[i].CountQueue(this.OnInnerCountQueue);
			}
		}

		private void OnInnerCountQueue()
		{
			routes.Add(new Route(curRouteKey, curRouteValue++));
			countQueueCallback();
		}

		public override void AccessFrame(QueueNode node, out QueueNode newNode)
		{
			RoutingNode routing = NATInwards(node);
			QueueNode returnednode;
			queues[(int)routing.Major].AccessFrame(routing.InnerNode, out returnednode);
			newNode = NATOutwards(routing.InnerNode, routing.Via);
		}

		public override IFrame Dequeue(QueueNode node)
		{
			RoutingNode routing = NATInwards(node);
			return queues[(int)routing.Major].Dequeue(routing.InnerNode);
		}

		protected RoutingNode NATInwards(QueueNode outer)
		{
			Route route = routes[(int)outer.Index];
			return new RoutingNode(route.Major, outer.Index, route.Minor, outer.ListNode);
		}
		protected QueueNode NATOutwards(QueueNode inner, uint via)
		{
			uint newmajor = inner.Index - routes[(int)via].Minor + via;
			return new QueueNode(newmajor, inner.ListNode);
		}



		protected struct Route
		{
			public readonly uint Major;
			public readonly uint Minor;

			public Route(uint major, uint minor)
			{
				Major = major;
				Minor = minor;
			}
		}

		protected struct RoutingNode
		{
			public readonly uint Major;
			public readonly uint Via;
			public readonly QueueNode InnerNode;

			public RoutingNode(uint major, uint via, uint innerindex,
				LinkedListNode<IFrame> innernode)
				: this(major, via, new QueueNode(innerindex, innernode)) { }

			public RoutingNode(uint major, uint via, QueueNode inner)
			{
				Major = major;
				Via = via;
				InnerNode = inner;
			}
		}

	}
}
