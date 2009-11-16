using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Buffers.Queues
{
	public interface IConcatHandler<T>
	{
		MultiList<T> MultiList { get; }
	}

	public class MultiListNode<T>
	{
		public MultiList<T> MultiList { get { return null; } }
		public MultiListNode<T> Previous { get { return null; } }
		public MultiListNode<T> Next { get { return null; } }
		public T Value { get{return default(T); }}
	}


	public class MultiList<T>
	{
		/// <summary>
		/// QueueIndex -> IQueue
		/// </summary>
		protected LinkedList<T>[] queues = null;

		/// <summary>
		/// QueueIndex -> NextQueueIndex
		/// </summary>
		protected int[] nexts = null;

		/// <summary>
		/// QueueIndex -> PrevQueueIndex
		/// </summary>
		protected int[] prevs = null;


		public MultiList(IEnumerable<LinkedList<T>> queues)
		{
			this.queues = queues.ToArray<LinkedList<T>>();
			this.nexts = new int[this.queues.Length];
			this.prevs = new int[this.queues.Length];

			for (int i = 0; i < nexts.Length; i++)
			{
				nexts[i] = -1;
				prevs[i] = -1;
			}
		}

		/// <summary>
		/// 设置一个连接。
		/// </summary>
		/// <param name="index">位于连接头部的队列。</param>
		/// <param name="next">位于连接尾部的队列。为 -1 表明删除此连接。</param>
		/// <returns></returns>
		public IConcatHandler<T> SetConcat(int index, int next)
		{
			Debug.Assert(index > 0 && index < queues.Length);
			Debug.Assert(next > -1 && next < queues.Length);

			BreakConcat(index);

			if (next == -1)
				return null;

			nexts[index] = next;
			prevs[next] = index;
			return new ConcatHandler(this, index);
		}

		/// <summary>
		/// 打破第 index 个队列与跟第 index 个队列尾部相连的队列的连接。
		/// </summary>
		private void BreakConcat(int index)
		{
			int next = nexts[index];
			nexts[index] = -1;

			if (next != -1)
				prevs[next] = -1;
		}

		/// <summary>
		/// 移除并返回位于第 index 个队列开始处的对象。
		/// </summary>
		public T Dequeue(int index)
		{
			T item = queues[index].Last.Value;
			queues[index].RemoveLast();
			return item;
		}




		private class ConcatHandler : IConcatHandler<T>
		{
			public readonly MultiList<T> ThisObject;
			public readonly int Index;

			public ConcatHandler(MultiList<T> obj, int index)
			{
				ThisObject = obj;
				Index = index;
			}

			public MultiList<T> MultiList
			{
				get { return ThisObject; }
			}
		}
	}
}
