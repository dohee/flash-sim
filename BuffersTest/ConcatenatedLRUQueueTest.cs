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
	public class ConcatenatedLRUQueueTest
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
		private static ConcatenatedLRUQueue ConstructSimpleConcatQueue()
		{
			IQueue front = new FIFOQueue();
			IQueue back = new FIFOQueue();
			front.Enqueue(new Frame(10));
			ConcatenatedLRUQueue target = new ConcatenatedLRUQueue(front, back);
			return target;
		}

		/// <summary>
		/// 构建出一个嵌套的连接型队列。
		/// </summary>
		/// <returns></returns>
		private static ConcatenatedLRUQueue ConstructNestedConcatQueue(out IQueue[] FIFOs)
		{
			IQueue[] q = new FIFOQueue[8];
			for (int i = 0; i < q.Length; i++)
			{
				q[i] = new FIFOQueue();
				q[i].Enqueue(new Frame((uint)(i * 10)));
				q[i].Enqueue(new Frame((uint)(i * 10 + 1)));
			}

			IQueue q01 = new ConcatenatedLRUQueue(q[0], q[1]),
				q23 = new ConcatenatedLRUQueue(q[2], q[3]),
				q45 = new ConcatenatedLRUQueue(q[4], q[5]),
				q67 = new ConcatenatedLRUQueue(q[6], q[7]);

			IQueue q0123 = new ConcatenatedLRUQueue(q01, q23),
				q4567 = new ConcatenatedLRUQueue(q45, q67);

			ConcatenatedLRUQueue target = new ConcatenatedLRUQueue(q0123, q4567);

			FIFOs = q;
			return target;
		}


		/// <summary>
		/// BlowOneFrame 的测试
		///</summary>
		[TestMethod()]
		public void BlowOneFrameSimpleTest()
		{
			ConcatenatedLRUQueue target = ConstructSimpleConcatQueue();
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
			ConcatenatedLRUQueue target = ConstructNestedConcatQueue(out fifos);
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
			ConcatenatedLRUQueue target = ConstructNestedConcatQueue(out fifos);

			QueueNode qn = new QueueNode(0, fifos[0].Enqueue(new Frame(999)).ListNode);
			fifos[0].Enqueue(new Frame(2));

			QueueNode actural = target.AccessFrame(qn);

			Assert.AreEqual(0u, actural.Index);
			Assert.AreEqual(999u, actural.ListNode.Value.Id);
			Assert.AreEqual(null, actural.ListNode.Previous);
		}

		/// <summary>
		/// AccessFrame 的测试
		/// </summary>
		[TestMethod()]
		public void AccessFrameHitMiddleTest()
		{
			IQueue[] fifos;
			ConcatenatedLRUQueue target = ConstructNestedConcatQueue(out fifos);

			QueueNode qn = new QueueNode(2, fifos[2].Enqueue(new Frame(999)).ListNode);
			fifos[2].Enqueue(new Frame(22));

			QueueNode actural = target.AccessFrame(qn);

			Assert.AreEqual(0u, actural.Index);
			Assert.AreEqual(999u, actural.ListNode.Value.Id);
			Assert.AreEqual(null, actural.ListNode.Previous);
		}
	}
}
