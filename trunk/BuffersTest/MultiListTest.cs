using System;
using System.Collections.Generic;

using Buffers.Lists;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuffersTest
{
	/// <summary>
	///这是 MultiListTest 的测试类，旨在
	///包含所有 MultiListTest 单元测试
	///</summary>
	[TestClass()]
	public class MultiListTest
	{
		private TestContext testContextInstance;
		public TestContext TestContext { get { return testContextInstance; } set { testContextInstance = value; } }

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

		#region 创建和初始化
		private static MultiList_Accessor<T> Create<T>()
		{
			var list = new MultiList_Accessor<T>(8);
			list.SetConcat(2, 3);
			list.SetConcat(4, 5);
			list.SetConcat(5, 6);
			list.SetConcat(6, 7);
			return list;
		}

		private static long[] FillLongs(MultiList_Accessor<long> list)
		{
			var nodes = new long[list.ListCount];

			for (int i = 0; i < nodes.Length; ++i)
				nodes[i] = list.AddFirst(i, i + 1000000L).Value;

			return nodes;
		}
		#endregion

		#region SetConcat 的测试
		private void SetConcatAndAssert<T>(MultiList_Accessor<T> target,
			int head, int next, int[] expectedNexts, int[] expectedPrevs)
		{
			if (head != -1)
				target.SetConcat(head, next);

			CollectionAssert.AreEqual(expectedNexts, target.nexts);
			CollectionAssert.AreEqual(expectedPrevs, target.prevs);
		}

		[TestMethod()]
		public void SetConcatTest()
		{
			SetConcatTestHelper<GenericParameterHelper>();
		}

		private void SetConcatTestHelper<T>()
		{
			MultiList_Accessor<T> target = Create<T>();

			SetConcatAndAssert<T>(target, -1, -1,
				new int[] { -1, -1, 3, -1, 5, 6, 7, -1 },
				new int[] { -1, -1, -1, 2, -1, 4, 5, 6 });

			SetConcatAndAssert<T>(target, 1, 0,
				new int[] { -1, 0, 3, -1, 5, 6, 7, -1 },
				new int[] { 1, -1, -1, 2, -1, 4, 5, 6 });
			SetConcatAndAssert<T>(target, 1, -1,
				new int[] { -1, -1, 3, -1, 5, 6, 7, -1 },
				new int[] { -1, -1, -1, 2, -1, 4, 5, 6 });
			SetConcatAndAssert<T>(target, 1, -1,
				new int[] { -1, -1, 3, -1, 5, 6, 7, -1 },
				new int[] { -1, -1, -1, 2, -1, 4, 5, 6 });

			SetConcatAndAssert<T>(target, 3, 4,
				new int[] { -1, -1, 3, 4, 5, 6, 7, -1 },
				new int[] { -1, -1, -1, 2, 3, 4, 5, 6 });
			SetConcatAndAssert<T>(target, 3, 4,
				new int[] { -1, -1, 3, 4, 5, 6, 7, -1 },
				new int[] { -1, -1, -1, 2, 3, 4, 5, 6 });
			SetConcatAndAssert<T>(target, 4, -1,
				new int[] { -1, -1, 3, 4, -1, 6, 7, -1 },
				new int[] { -1, -1, -1, 2, 3, -1, 5, 6 });
			SetConcatAndAssert<T>(target, 4, 5,
				new int[] { -1, -1, 3, 4, 5, 6, 7, -1 },
				new int[] { -1, -1, -1, 2, 3, 4, 5, 6 });

			SetConcatAndAssert<T>(target, 5, 3,
				new int[] { -1, -1, -1, 4, 5, 3, 7, -1 },
				new int[] { -1, -1, -1, 5, 3, 4, -1, 6 });
		}
		#endregion

		#region RemoveLast 的测试
		[TestMethod()]
		public void RemoveLastNormalTest()
		{
			MultiList_Accessor<long> target = Create<long>();
			FillLongs(target);

			target.RemoveLast(0);
			CollectionAssert.AreEqual(new long[0], target.lists[0]);

			target.RemoveLast(6);
			target.RemoveLast(6, true);
			target.RemoveLast(6, true);
			CollectionAssert.AreEqual(new long[0], target.lists[6]);
			CollectionAssert.AreEqual(new long[0], target.lists[5]);
			CollectionAssert.AreEqual(new long[0], target.lists[4]);
		}
		[TestMethod()]
		[ExpectedException(typeof(InvalidOperationException))]
		public void RemoveLastExceptionTest1()
		{
			MultiList_Accessor<long> target = Create<long>();
			FillLongs(target);
			target.RemoveLast(0);
			target.RemoveLast(0, false);
		}
		[TestMethod()]
		[ExpectedException(typeof(InvalidOperationException))]
		public void RemoveLastExceptionTest2()
		{
			MultiList_Accessor<long> target = Create<long>();
			FillLongs(target);
			target.RemoveLast(0);
			target.RemoveLast(0, true);
		}
		[TestMethod()]
		[ExpectedException(typeof(InvalidOperationException))]
		public void RemoveLastExceptionTest3()
		{
			MultiList_Accessor<long> target = Create<long>();
			FillLongs(target);
			target.RemoveLast(6);
			target.RemoveLast(6, false);
		}
		[TestMethod()]
		[ExpectedException(typeof(InvalidOperationException))]
		public void RemoveLastExceptionTest4()
		{
			MultiList_Accessor<long> target = Create<long>();
			FillLongs(target);
			target.RemoveLast(6);
			target.RemoveLast(6, true);
			target.RemoveLast(6, true);
			target.RemoveLast(6, true);
			target.RemoveLast(6, true);
		}
		#endregion

		#region GetNextNode 和 GetPreviousNode 的测试
		[TestMethod()]
		public void GetPrevNextNodeTest()
		{
			var target = Create<long>();
			long[] numbers = FillLongs(target);
			MultiListNode<long> node = target.GetFirstNode(6);

			node = target.GetPreviousNode(node);
			Assert.AreEqual(numbers[5], node.Value);
			node = target.GetPreviousNode(node);
			Assert.AreEqual(numbers[4], node.Value);
			node = target.GetNextNode(node);
			Assert.AreEqual(numbers[5], node.Value);
			node = target.GetNextNode(node);
			Assert.AreEqual(numbers[6], node.Value);

			target.RemoveFirst(5);
			node = target.GetPreviousNode(node);
			Assert.AreEqual(numbers[4], node.Value);
			Assert.IsNull(target.GetPreviousNode(node));

			target.RemoveFirst(6);
			node = target.GetNextNode(node);
			Assert.AreEqual(numbers[7], node.Value);
			Assert.IsNull(target.GetNextNode(node));

			target.RemoveFirst(4);
			Assert.IsNull(target.GetPreviousNode(node));
		}
		#endregion
	}
}
