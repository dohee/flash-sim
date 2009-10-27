using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Buffers.Queues
{
	public interface IConcatHandler<T>
	{
		MultiQueue<T> Queue{get;}
	}

	public class MultiQueue<T>
	{
		/// <summary>
		/// QueueIndex -> IQueue
		/// </summary>
		protected Queue<T>[] queues = null;

		/// <summary>
		/// QueueIndex -> NextQueueIndex
		/// </summary>
		protected int[] nexts = null;

		/// <summary>
		/// QueueIndex -> PrevQueueIndex
		/// </summary>
		protected int[] prevs = null;


		public MultiQueue(IEnumerable<Queue<T>> queues)
		{
			this.queues = queues.ToArray<Queue<T>>();
			this.nexts = new int[this.queues.Length];
			this.prevs = new int[this.queues.Length];

			for (int i = 0; i < nexts.Length; i++)
			{
				nexts[i] = -1;
				prevs[i] = -1;
			}
		}

		public IConcatHandler<T> SetConcat(int index, int next)
		{
			Debug.Assert(next > -1 && next < queues.Length);

			if (nexts[index] != -1)
			{
				
			}

			nexts[index] = next;
			prevs[next] = index;
			return new ConcatHandler(this, index);
		}

		/// <summary>
		/// 移除并返回位于第 index 个队列开始处的对象。
		/// </summary>
		public T Dequeue(int index)
		{
			return queues[index].Dequeue();
		}




		private class ConcatHandler : IConcatHandler<T>
		{
			public readonly MultiQueue<T> ThisObject;
			public readonly int QueueIndex;

			public ConcatHandler(MultiQueue<T> obj, int index)
			{
				ThisObject = obj;
				QueueIndex = index;
			}

			public MultiQueue<T> Queue
			{
				get { return ThisObject; }
			}
		}
	}
}
