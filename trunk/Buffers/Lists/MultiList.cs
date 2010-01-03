using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Buffers.Lists
{
	public class MultiListNode<T>
	{
		public MultiList<T> MultiList { get; private set; }
		public int ListIndex { get; private set; }
		internal LinkedListNode<T> IntraNode { get; private set; }

		internal MultiListNode(MultiList<T> host, int listIndex, LinkedListNode<T> intraNode)
		{
			MultiList = host;
			ListIndex = listIndex;
			IntraNode = intraNode;
		}

		public MultiListNode<T> Previous { get { return MultiList.GetPreviousNode(this); } }
		public MultiListNode<T> Next { get { return MultiList.GetNextNode(this); } }
		public T Value { get { return IntraNode.Value; } set { IntraNode.Value = value; } }
	}


	public class MultiList<T>
	{
		/// <summary>ListIndex -> List</summary>
		protected LinkedList<T>[] lists = null;

		/// <summary>ListIndex -> NextListIndex</summary>
		protected int[] nexts = null;

		/// <summary>ListIndex -> PrevListIndex</summary>
		protected int[] prevs = null;


		public MultiList(int listCount)
		{
			this.lists = new LinkedList<T>[listCount];
			this.nexts = new int[listCount];
			this.prevs = new int[listCount];

			for (int i = 0; i < nexts.Length; i++)
			{
				lists[i] = new LinkedList<T>();
				nexts[i] = -1;
				prevs[i] = -1;
			}
		}

		public MultiList(IEnumerable<LinkedList<T>> lists)
		{
			this.lists = lists.ToArray<LinkedList<T>>();
			this.nexts = new int[this.lists.Length];
			this.prevs = new int[this.lists.Length];

			for (int i = 0; i < nexts.Length; i++)
			{
				nexts[i] = -1;
				prevs[i] = -1;
			}
		}


		/// <summary>设置一个连接。
		/// </summary>
		/// <param name="index">位于连接头部的链表。</param>
		/// <param name="next">位于连接尾部的链表。为 -1 表明删除此连接。</param>
		/// <remarks>不要使这些链表形成环。否则其他算法有可能陷入死循环。</remarks>
		public void SetConcat(int head, int next)
		{
			Debug.Assert(head >= 0 && head < lists.Length);
			Debug.Assert(next >= -1 && next < lists.Length);

			BreakConcat(head);

			if (next == -1)
				return;

			if (prevs[next] != -1)
				BreakConcat(prevs[next]);

			nexts[head] = next;
			prevs[next] = head;
		}

		/// <summary>打破第 index 个链表尾部的连接。
		/// </summary>
		private void BreakConcat(int head)
		{
			Debug.Assert(head >= 0 && head < lists.Length);
			int next = nexts[head];
			nexts[head] = -1;

			if (next != -1)
				prevs[next] = -1;
		}


		/// <summary>获取链表的个数。
		/// </summary>
		public int ListCount { get { return lists.Length; } }
		/// <summary>获取第 listIndex 个链表中实际包含的节点数。
		/// </summary>
		public int GetNodeCount(int listIndex)
		{
			return lists[listIndex].Count;
		}


		/// <summary>获取 node 的上一个节点。
		/// </summary>
		public MultiListNode<T> GetPreviousNode(MultiListNode<T> node)
		{
			Debug.Assert(object.ReferenceEquals(this, node.MultiList));

			int index = node.ListIndex;
			LinkedListNode<T> prevNode = node.IntraNode.Previous;

			while (prevNode == null)
			{
				index = prevs[index];
				if (index == -1)
					return null;
				prevNode = lists[index].Last;
			}

			return new MultiListNode<T>(this, index, prevNode);
		}

		/// <summary>获取 node 的下一个节点。
		/// </summary>
		public MultiListNode<T> GetNextNode(MultiListNode<T> node)
		{
			Debug.Assert(object.ReferenceEquals(this, node.MultiList));

			int index = node.ListIndex;
			LinkedListNode<T> nextNode = node.IntraNode.Next;

			while (nextNode == null)
			{
				index = nexts[index];
				if (index == -1)
					return null;
				nextNode = lists[index].First;
			}

			return new MultiListNode<T>(this, index, nextNode);
		}


		/// <summary>获取第 listIndex 个链表的开头处的节点。
		/// </summary>
		public MultiListNode<T> GetFirstNode(int listIndex)
		{
			LinkedListNode<T> intra = lists[listIndex].First;
			return (intra == null ? null : new MultiListNode<T>(this, listIndex, intra));
		}
		/// <summary>获取第 listIndex 个链表的结尾处的节点。
		/// </summary>
		public MultiListNode<T> GetLastNode(int listIndex)
		{
			LinkedListNode<T> intra = lists[listIndex].Last;
			return (intra == null ? null : new MultiListNode<T>(this, listIndex, intra));
		}

		/// <summary>在第 listIndex 个链表的开头处添加新的节点或值。
		/// </summary>
		public MultiListNode<T> AddFirst(int listIndex, T value)
		{
			LinkedListNode<T> intra = lists[listIndex].AddFirst(value);
			return new MultiListNode<T>(this, listIndex, intra);
		}
		/// <summary>在第 listIndex 个链表的结尾处添加新的节点或值。 
		/// </summary>
		public MultiListNode<T> AddLast(int listIndex, T value)
		{
			LinkedListNode<T> intra = lists[listIndex].AddLast(value);
			return new MultiListNode<T>(this, listIndex, intra);
		}

		/// <summary>移除并返回位于第 listIndex 个链表开始处的对象。
		/// </summary>
		public T RemoveFirst(int listIndex)
		{
			T item = lists[listIndex].First.Value;
			lists[listIndex].RemoveFirst();
			return item;
		}
		/// <summary>移除并返回位于第 listIndex 个链表结尾处的对象。
		/// </summary>
		public T RemoveLast(int listIndex)
		{
			if (lists[listIndex].Count == 0)
				throw new InvalidOperationException("第 " + listIndex + " 个链表为空。");

			T item = lists[listIndex].Last.Value;
			lists[listIndex].RemoveLast();
			return item;
		}
		/// <summary>移除并返回位于第 listIndex 个链表结尾处（或最靠近该链表结尾）的对象。
		/// </summary>
		public T RemoveLast(int listIndex, bool cascade)
		{
			if (!cascade)
				RemoveLast(listIndex);

			LinkedListNode<T> toRemove = lists[listIndex].Last;

			while (toRemove == null)
			{
				listIndex = prevs[listIndex];

				if (listIndex == -1)
					throw new InvalidOperationException(
						"第 " + listIndex + " 个链表和从该链表往前的链表都为空。");

				toRemove = lists[listIndex].Last;
			}

			T item = toRemove.Value;
			toRemove.List.Remove(toRemove);
			return item;
		}
		/// <summary>从 MultiLinkedList 中移除指定的节点。 
		/// </summary>
		public T Remove(MultiListNode<T> node)
		{
			Debug.Assert(object.ReferenceEquals(this, node.MultiList));
			T item = node.Value;
			lists[node.ListIndex].Remove(node.IntraNode);
			return item;
		}

		/// <summary>将第 from 个链表的最后一个节点（或最靠近该链表的节点）移动到它所连接的下游链表。
		/// </summary>
		public MultiListNode<T> Blow(int from)
		{
			Debug.Assert(nexts[from] != -1);
			T item = RemoveLast(from, true);
			return AddFirst(nexts[from], item);
		}
	}
}
