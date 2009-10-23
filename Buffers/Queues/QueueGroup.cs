using System;
using System.Collections.Generic;
using System.Diagnostics;
using Buffers.Memory;

namespace Buffers.Queues
{
	public abstract class QueueGroup<T> : QueueBase<T>
	{
		/// <summary>
		/// QueueIndex -> IQueue
		/// </summary>
		protected IQueue<T>[] queues = null;
		/// <summary>
		/// QueueIndex -> RouteIndex
		/// </summary>
		protected uint[] offsets = null;
		/// <summary>
		/// RouteIndex -> (QueueIndex, InnerRootIndex)
		/// </summary>
		protected Route[] routes = null;


		protected void BuildRoutes()
		{
			List<uint> offsets = new List<uint>();
			List<Route> routes = new List<Route>();

			for (int i = 0; i < queues.Length; i++)
			{
				offsets.Add((uint)routes.Count);
				for (int j = 0; j < queues[i].BasicQueueCount; j++)
					routes.Add(new Route((uint)i, (uint)j));
			}

			this.offsets = offsets.ToArray();
			this.routes = routes.ToArray();
		}

		protected RoutingNode NATInwards(QueueNode<T> outerNode)
		{
			Route route = routes[(int)outerNode.Index];
			return new RoutingNode(route.QueueIndex, route.InnerRouteIndex, outerNode.ListNode);
		}
		protected QueueNode<T> NATOutwards(uint queueIndex, QueueNode<T> innerNode)
		{
			uint routeIndex = innerNode.Index + offsets[(int)queueIndex];
			return new QueueNode<T>(routeIndex, innerNode.ListNode);
		}


		public override IEnumerator<T> GetEnumerator()
		{
			foreach (var queue in queues)
				foreach (var t in queue)
					yield return t;
		}

		public override uint Size
		{
			get
			{
				uint sum = 0;
				foreach (var queue in queues)
					sum += queue.Size;
				return sum;
			}
		}

		public override uint BasicQueueCount
		{
			get
			{
				uint sum = 0;
				foreach (var queue in queues)
					sum += queue.BasicQueueCount;
				return sum;
			}
		}


		public override QueueNode<T> Access(QueueNode<T> node)
		{
			RoutingNode routing = NATInwards(node);
			QueueNode<T> qn = queues[(int)routing.QueueIndex].Access(routing.InnerNode);
			return NATOutwards(routing.QueueIndex, qn);
		}

		public override T Dequeue(QueueNode<T> node)
		{
			RoutingNode routing = NATInwards(node);
			return queues[(int)routing.QueueIndex].Dequeue(routing.InnerNode);
		}

		public override uint GetRoute(QueueNode<T> node)
		{
			return NATInwards(node).QueueIndex;
		}

		public override IList<uint> GetRoutePath(QueueNode<T> node)
		{
			RoutingNode routing = NATInwards(node);
			var list = queues[(int)routing.QueueIndex].GetRoutePath(routing.InnerNode);
			list.Add(routing.QueueIndex);
			return list;
		}




		protected struct Route : IEquatable<Route>
		{
			public readonly uint QueueIndex;
			public readonly uint InnerRouteIndex;

			public Route(uint queueIndex, uint innerRouteIndex)
			{
				QueueIndex = queueIndex;
				InnerRouteIndex = innerRouteIndex;
			}

			#region Equals 函数族
			public bool Equals(Route other)
			{
				return QueueIndex == other.QueueIndex &&
					InnerRouteIndex == other.InnerRouteIndex;
			}
			public override int GetHashCode()
			{
				return QueueIndex.GetHashCode() ^ InnerRouteIndex.GetHashCode();
			}
			public override bool Equals(object obj)
			{
				if (obj == null || GetType() != obj.GetType())
					return false;
				else
					return Equals((Route)obj);
			}
			public static bool operator ==(Route left, Route right)
			{
				return left.Equals(right);
			}
			public static bool operator !=(Route left, Route right)
			{
				return !left.Equals(right);
			}
			#endregion

		}

		protected struct RoutingNode : IEquatable<RoutingNode>
		{
			public readonly uint QueueIndex;
			public readonly QueueNode<T> InnerNode;

			public RoutingNode(uint major, uint innerindex, LinkedListNode<T> innernode)
				: this(major, new QueueNode<T>(innerindex, innernode)) { }

			public RoutingNode(uint major, QueueNode<T> inner)
			{
				QueueIndex = major;
				InnerNode = inner;
			}

			#region Equals 函数族
			public bool Equals(RoutingNode other)
			{
				return QueueIndex == other.QueueIndex && InnerNode == other.InnerNode;
			}
			public override int GetHashCode()
			{
				return QueueIndex.GetHashCode() ^ InnerNode.GetHashCode();
			}
			public override bool Equals(object obj)
			{
				if (obj == null || GetType() != obj.GetType())
					return false;
				else
					return Equals((RoutingNode)obj);
			}
			public static bool operator ==(RoutingNode left, RoutingNode right)
			{
				return left.Equals(right);
			}
			public static bool operator !=(RoutingNode left, RoutingNode right)
			{
				return !left.Equals(right);
			}
			#endregion
		}

	}
}
