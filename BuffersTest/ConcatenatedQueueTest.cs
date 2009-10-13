using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Buffers.Queues;
using Buffers;
using Buffers.Memory;

namespace BuffersTest
{
    
    /// <summary>
    ///这是 ConcatenatedQueueTest 的测试类，旨在
    ///包含所有 ConcatenatedQueueTest 单元测试
    ///</summary>
	[TestClass()]
	public class ConcatenatedQueueTest
	{
		private TestContext testContextInstance;

		/// <summary>
		///获取或设置测试上下文，上下文提供
		///有关当前测试运行及其功能的信息。
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}

		#region 附加测试属性
		// 
		//编写测试时，还可使用以下属性:
		//
		//使用 ClassInitialize 在运行类中的第一个测试前先运行代码
		//[ClassInitialize()]
		//public static void MyClassInitialize(TestContext testContext)
		//{
		//}
		//
		//使用 ClassCleanup 在运行完类中的所有测试后再运行代码
		//[ClassCleanup()]
		//public static void MyClassCleanup()
		//{
		//}
		//
		//使用 TestInitialize 在运行每个测试前先运行代码
		//[TestInitialize()]
		//public void MyTestInitialize()
		//{
		//}
		//
		//使用 TestCleanup 在运行完每个测试后运行代码
		//[TestCleanup()]
		//public void MyTestCleanup()
		//{
		//}
		//
		#endregion


		/// <summary>
		/// 构建出一个简单的连接型队列，其中前部包含元素 10，后部为空
		/// </summary>
		/// <returns></returns>
		private static ConcatenatedQueue ConstructSimpleConcatQueue()
		{
			IQueue front = new FIFOQueue();
			IQueue back = new FIFOQueue();
			front.Enqueue(new Frame(10));
			ConcatenatedQueue target = new ConcatenatedQueue(front, back);
			target.CountQueue(null);
			return target;
		}

		/// <summary>
		/// 构建出一个嵌套的连接型队列。
		/// </summary>
		/// <returns></returns>
		private static ConcatenatedQueue ConstructNestedConcatQueue(out IQueue[] FIFOs)
		{
			IQueue[] q = new FIFOQueue[8];
			for (int i = 0; i < q.Length; i++)
			{
				q[i] = new FIFOQueue();
				q[i].Enqueue(new Frame((uint)(i * 10)));
				q[i].Enqueue(new Frame((uint)(i * 10 + 1)));
			}

			IQueue q01 = new ConcatenatedQueue(q[0], q[1]),
				q23 = new ConcatenatedQueue(q[2], q[3]),
				q45 = new ConcatenatedQueue(q[4], q[5]),
				q67 = new ConcatenatedQueue(q[6], q[7]);

			IQueue q0123 = new ConcatenatedQueue(q01, q23),
				q4567 = new ConcatenatedQueue(q45, q67);

			ConcatenatedQueue target = new ConcatenatedQueue(q0123, q4567);
			target.CountQueue(null);

			FIFOs = q;
			return target;
		}


		/// <summary>
		/// BlowOneFrame 的测试
		///</summary>
		[TestMethod()]
		public void BlowOneFrameSimpleTest()
		{
			ConcatenatedQueue target = ConstructSimpleConcatQueue();
			QueueNode actual = target.BlowOneFrame();

			Assert.AreEqual(1u, actual.Index);
			Assert.AreEqual(10u, actual.ListNode.Value.Id);
			Assert.AreEqual(null, actual.ListNode.Previous);
		}

		/// <summary>
		///BlowOneFrame 的测试
		/// </summary>
		[TestMethod()]
		public void BlowOneFrameNestedTest()
		{
			IQueue[] fifos;
			ConcatenatedQueue target = ConstructNestedConcatQueue(out fifos);
			QueueNode actual = target.BlowOneFrame();

			Assert.AreEqual(4u, actual.Index);
			Assert.AreEqual(30u, actual.ListNode.Value.Id);
			Assert.AreEqual(null, actual.ListNode.Previous);
		}

		/// <summary>
		/// AccessFrame 的测试
		///</summary>
		[TestMethod()]
		public void AccessFrameHitFrontTest()
		{
			IQueue[] fifos;
			ConcatenatedQueue target = ConstructNestedConcatQueue(out fifos);

			uint expRouteIndex = 0;
			LinkedListNode<IFrame> expNode = fifos[0].Enqueue(new Frame(999)).ListNode;
			fifos[0].Enqueue(new Frame(2));

			QueueNode actural;
			target.AccessFrame(new QueueNode(expRouteIndex, expNode), out actural);

			Assert.AreEqual(expRouteIndex, actural.Index);
			Assert.AreEqual(expNode, actural.ListNode);
		}

		/// <summary>
		/// AccessFrame 的测试
		/// </summary>
		[TestMethod()]
		public void AccessFrameHitMiddleTest()
		{
			IQueue[] fifos;
			ConcatenatedQueue target = ConstructNestedConcatQueue(out fifos);

			QueueNode qn = new QueueNode(2, fifos[2].Enqueue(new Frame(999)).ListNode);
			fifos[2].Enqueue(new Frame(22));

			QueueNode actural;
			target.AccessFrame(qn, out actural);

			Assert.AreEqual(0u, actural.Index);
			Assert.AreEqual(999u, actural.ListNode.Value.Id);
			Assert.AreEqual(null, actural.ListNode.Previous);
		}
	}
}
