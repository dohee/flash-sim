using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Buffers.Lists
{
	public class MultiListNode<T>
	{
		public MultiList<T> MultiList { get; private set; }
		internal int ListIndex { get; private set; }
		internal LinkedListNode<T> IntraNode { get; private set; }

		internal MultiListNode(MultiList<T> host, int listIndex, LinkedListNode<T> intraNode)
		{
			MultiList = host;
			ListIndex = listIndex;
			IntraNode = intraNode;
		}

		public MultiListNode<T> Previous { get { return MultiList.GetNodePrevious(this); } }
		public MultiListNode<T> Next { get { return MultiList.GetNodeNext(this); } }
		public T Value { get { return IntraNode.Value; } }
	}


	public class MultiList<T>
	{
		/// <summary>
		/// ListIndex -> List
		/// </summary>
		protected LinkedList<T>[] lists = null;

		/// <summary>
		/// ListIndex -> NextListIndex
		/// </summary>
		protected int[] nexts = null;

		/// <summary>
		/// ListIndex -> PrevListIndex
		/// </summary>
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

		/// <summary>
		/// 设置一个连接。
		/// </summary>
		/// <param name="index">位于连接头部的链表。</param>
		/// <param name="next">位于连接尾部的链表。为 -1 表明删除此连接。</param>
		public void SetConcat(int head, int next)
		{
			Debug.Assert(head >= 0 && head < lists.Length);
			Debug.Assert(next >= -1 && next < lists.Length);

			BreakConcat(head);

			if (next == -1)
				return;

			nexts[head] = next;
			prevs[next] = head;
		}

		/// <summary>
		/// 打破第 index 个链表尾部的连接。
		/// </summary>
		private void BreakConcat(int head)
		{
			Debug.Assert(head >= 0 && head < lists.Length);
			int next = nexts[head];
			nexts[head] = -1;

			if (next != -1)
				prevs[next] = -1;
		}


		/// <summary>
		/// 获取第 index 个链表中实际包含的节点数。 
		/// </summary>
		public int GetListCount(int index)
		{
			return lists[index].Count;
		}

		/// <summary>
		/// 获取 node 的上一个节点。
		/// </summary>
		public MultiListNode<T> GetNodePrevious(MultiListNode<T> node)
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

		/// <summary>
		/// 获取 node 的下一个节点。
		/// </summary>
		public MultiListNode<T> GetNodeNext(MultiListNode<T> node)
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


		/// <summary>
		/// 在第 index 个链表的开头处添加新的节点或值。 
		/// </summary>
		public void AddFirst(int index, T value)
		{
			lists[index].AddFirst(value);
		}
		/// <summary>
		/// 在第 index 个链表的结尾处添加新的节点或值。 
		/// </summary>
		public void AddLast(int index, T value)
		{
			lists[index].AddLast(value);
		}

		/// <summary>
		/// 移除并返回位于第 index 个链表开始处的对象。
		/// </summary>
		public T RemoveFirst(int index)
		{
			T item = lists[index].First.Value;
			lists[index].RemoveFirst();
			return item;
		}
		/// <summary>
		/// 移除并返回位于第 index 个链表结尾处的对象。
		/// </summary>
		public T RemoveLast(int index)
		{
			T item = lists[index].Last.Value;
			lists[index].RemoveLast();
			return item;
		}
		/// <summary>
		/// 移除并返回位于第 index 个链表结尾处（或最靠近该链表结尾）的对象。
		/// </summary>
		public T RemoveLast(int index, bool cascade)
		{
			if (!cascade)
				RemoveLast(index);

			LinkedListNode<T> toRemove = lists[index].Last;

			while (toRemove == null)
			{
				index = prevs[index];

				if (index == -1)
					throw new InvalidOperationException(
						"第 " + index + " 个链表和从该链表往前的链表都为空。");

				toRemove = lists[index].Last;
			}

			T item = toRemove.Value;
			toRemove.List.Remove(toRemove);
			return item;
		}
		/// <summary>
		/// 从 MultiLinkedList 中移除指定的节点。 
		/// </summary>
		public void Remove(MultiListNode<T> node)
		{
			Debug.Assert(object.ReferenceEquals(this, node.MultiList));
			lists[node.ListIndex].Remove(node.IntraNode);
		}

		/// <summary>
		/// 将第 from 个链表的最后一个节点（或最靠近该链表的节点）
		/// 移动到它所连接的下游链表。
		/// </summary>
		public T Blow(int from)
		{
			Debug.Assert(nexts[from] != -1);
			T item = RemoveLast(from, true);
			AddFirst(nexts[from], item);
			return item;
		}
	}
}
