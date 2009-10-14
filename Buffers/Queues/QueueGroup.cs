using System.Collections.Generic;
using System.Diagnostics;
using Buffers.Memory;

namespace Buffers.Queues
{
	public abstract class QueueGroup : QueueBase
	{
		/// <summary>
		/// QueueIndex -> IQueue
		/// </summary>
		protected List<IQueue> queues = new List<IQueue>();
		/// <summary>
		/// QueueIndex -> RouteIndex
		/// </summary>
		protected List<uint> offsets = new List<uint>();
		/// <summary>
		/// RouteIndex -> (QueueIndex, InnerRootIndex)
		/// </summary>
		protected List<Route> routes = new List<Route>();

		private uint curRouteKey, curRouteValue;


		protected sealed override void DoCountQueue()
		{
			for (int i = 0; i < queues.Count; i++)
			{
				offsets.Add((uint)routes.Count);
				curRouteKey = (uint)i;
				curRouteValue = 0;
				queues[i].CountQueue(this.OnInnerCountQueue);
			}
		}

		private void OnInnerCountQueue()
		{
			routes.Add(new Route(curRouteKey, curRouteValue++));
			if (countQueueCallback != null)
				countQueueCallback();
		}

		public override QueueNode AccessFrame(QueueNode node)
		{
			RoutingNode routing = NATInwards(node);
			QueueNode returnednode = queues[(int)routing.QueueIndex].AccessFrame(routing.InnerNode);
			return NATOutwards(routing.QueueIndex, returnednode);
		}

		public override IFrame Dequeue(QueueNode node)
		{
			RoutingNode routing = NATInwards(node);
			return queues[(int)routing.QueueIndex].Dequeue(routing.InnerNode);
		}

		public override IEnumerator<IFrame> GetEnumerator()
		{
			foreach (var queue in queues)
				foreach (var frame in queue)
					yield return frame;
		}


		protected RoutingNode NATInwards(QueueNode outerNode)
		{
			Route route = routes[(int)outerNode.Index];
			return new RoutingNode(route.QueueIndex, route.InnerRouteIndex, outerNode.ListNode);
		}
		protected QueueNode NATOutwards(uint queueIndex, QueueNode innerNode)
		{
			uint routeIndex = innerNode.Index + offsets[(int)queueIndex];
			return new QueueNode(routeIndex, innerNode.ListNode);
		}



		protected struct Route
		{
			public readonly uint QueueIndex;
			public readonly uint InnerRouteIndex;

			public Route(uint queueIndex, uint innerRouteIndex)
			{
				QueueIndex = queueIndex;
				InnerRouteIndex = innerRouteIndex;
			}
		}

		protected struct RoutingNode
		{
			public readonly uint QueueIndex;
			public readonly QueueNode InnerNode;

			public RoutingNode(uint major, uint innerindex, LinkedListNode<IFrame> innernode)
				: this(major, new QueueNode(innerindex, innernode)) { }

			public RoutingNode(uint major, QueueNode inner)
			{
				QueueIndex = major;
				InnerNode = inner;
			}
		}

	}
}
