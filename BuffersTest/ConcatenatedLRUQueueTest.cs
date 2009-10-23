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
		private static ConcatenatedLRUQueue<IFrame> ConstructSimpleConcatQueue()
		{
			IQueue<IFrame> front = new FIFOQueue<IFrame>();
			IQueue<IFrame> back = new FIFOQueue<IFrame>();
			front.Enqueue(new Frame(10));
			ConcatenatedLRUQueue<IFrame> target = new ConcatenatedLRUQueue<IFrame>(front, back);
			return target;
		}

		/// <summary>
		/// 构建出一个嵌套的连接型队列。
		/// </summary>
		/// <returns></returns>
		private static ConcatenatedLRUQueue<IFrame> ConstructNestedConcatQueue(out IQueue<IFrame>[] FIFOs)
		{
			IQueue<IFrame>[] q = new FIFOQueue<IFrame>[8];
			for (int i = 0; i < q.Length; i++)
			{
				q[i] = new FIFOQueue<IFrame>();
				q[i].Enqueue(new Frame((uint)(i * 10)));
				q[i].Enqueue(new Frame((uint)(i * 10 + 1)));
			}

			IQueue<IFrame> q01 = new ConcatenatedLRUQueue<IFrame>(q[0], q[1]),
				q23 = new ConcatenatedLRUQueue<IFrame>(q[2], q[3]),
				q45 = new ConcatenatedLRUQueue<IFrame>(q[4], q[5]),
				q67 = new ConcatenatedLRUQueue<IFrame>(q[6], q[7]);

			IQueue<IFrame> q0123 = new ConcatenatedLRUQueue<IFrame>(q01, q23),
				q4567 = new ConcatenatedLRUQueue<IFrame>(q45, q67);

			ConcatenatedLRUQueue<IFrame> target = new ConcatenatedLRUQueue<IFrame>(q0123, q4567);

			FIFOs = q;
			return target;
		}


		/// <summary>
		/// BlowOneFrame 的测试
		///</summary>
		[TestMethod()]
		public void BlowOneFrameSimpleTest()
		{
			ConcatenatedLRUQueue<IFrame> target = ConstructSimpleConcatQueue();
			QueueNode<IFrame> actual = target.BlowOneFrame();

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
			IQueue<IFrame>[] fifos;
			ConcatenatedLRUQueue<IFrame> target = ConstructNestedConcatQueue(out fifos);
			QueueNode<IFrame> actual = target.BlowOneFrame();

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
			IQueue<IFrame>[] fifos;
			ConcatenatedLRUQueue<IFrame> target = ConstructNestedConcatQueue(out fifos);

			QueueNode<IFrame> qn = new QueueNode<IFrame>(0, fifos[0].Enqueue(new Frame(999)).ListNode);
			fifos[0].Enqueue(new Frame(2));

			QueueNode<IFrame> actural = target.Access(qn);

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
			IQueue<IFrame>[] fifos;
			ConcatenatedLRUQueue<IFrame> target = ConstructNestedConcatQueue(out fifos);

			QueueNode<IFrame> qn = new QueueNode<IFrame>(2, fifos[2].Enqueue(new Frame(999)).ListNode);
			fifos[2].Enqueue(new Frame(22));

			QueueNode<IFrame> actural = target.Access(qn);

			Assert.AreEqual(0u, actural.Index);
			Assert.AreEqual(999u, actural.ListNode.Value.Id);
			Assert.AreEqual(null, actural.ListNode.Previous);
		}
	}
}
