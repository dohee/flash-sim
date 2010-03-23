using Buffers.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuffersTest
{
    /// <summary>
    ///这是 SparseArrayTest 的测试类，旨在
    ///包含所有 SparseArrayTest 单元测试
    ///</summary>
	[TestClass()]
	public class SparseArrayTest
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

		#region 创建和初始化
		private SparseArray_Accessor<int> CreateInt()
		{
			var t = new SparseArray_Accessor<int>();
			int lowlength = SparseArray_Accessor<int>.LowPartLength;

			t[5, 5] = 0;
			t[7, 7] = 7;

			int[] a = new int[lowlength + 100];
			for (int i = 30; i < lowlength; i++)
				a[i] = i;

			t.SetBlock(a, 0, (uint)(10 * lowlength - 10), 11);
			t.SetBlock(a, lowlength - 50, (uint)(13 * lowlength - 50), 150);

			return t;
		}
		private SparseArray_Accessor<GenericParameterHelper> CreateGeneric()
		{
			var t = new SparseArray_Accessor<GenericParameterHelper>();
			int lowlength = SparseArray_Accessor<GenericParameterHelper>.LowPartLength;

			t[5, 5] = new GenericParameterHelper(0);
			t[7, 7] = new GenericParameterHelper(7);

			var a = new GenericParameterHelper[lowlength + 100];
			for (int i = 30; i < lowlength; i++)
				a[i] = new GenericParameterHelper(i);

			t.SetBlock(a, 0, (uint)(10 * lowlength - 10), 11);
			t.SetBlock(a, lowlength - 50, (uint)(13 * lowlength - 50), 150);

			return t;
		}
		#endregion

		/// <summary>
		///Item 的测试
		///</summary>
		public void ItemTestHelper(SparseArray_Accessor<int> t)
		{
			Assert.AreEqual(0, t[0]);
			Assert.AreEqual(0, t[0, 0]);
			Assert.AreEqual(0, t[5 * t.LowerBound + 5]);
			Assert.AreEqual(0, t[5, 5]);
			Assert.AreEqual(7, t[7 * t.LowerBound + 7]);
			Assert.AreEqual(7, t[7, 7]);
			Assert.AreEqual(0, t[10 * t.LowerBound + t.LowerBound - 1]);
			Assert.AreEqual(0, t[10, (int)(t.LowerBound - 1)]);
			Assert.AreEqual(30, t[11 * t.LowerBound + 20]);
			Assert.AreEqual(30, t[11, 20]);
		}

		[TestMethod()]
		public void ItemTest()
		{
			ItemTestHelper(CreateInt());
		}

		/// <summary>
		///SetBlock 的测试
		///</summary>
		public void SetBlockTestHelper<T>()
		{
			SparseArray_Accessor<T> target = new SparseArray_Accessor<T>(); // TODO: 初始化为适当的值
			T[] buffer = null; // TODO: 初始化为适当的值
			int offset = 0; // TODO: 初始化为适当的值
			uint index = 0; // TODO: 初始化为适当的值
			int count = 0; // TODO: 初始化为适当的值
			target.SetBlock(buffer, offset, index, count);
			Assert.Inconclusive("无法验证不返回值的方法。");
		}

		[TestMethod()]
		public void SetBlockTest()
		{
			SetBlockTestHelper<GenericParameterHelper>();
		}

		/// <summary>
		///GetBlock 的测试
		///</summary>
		public void GetBlockTestHelper<T>()
		{
			SparseArray_Accessor<T> target = new SparseArray_Accessor<T>(); // TODO: 初始化为适当的值
			T[] buffer = null; // TODO: 初始化为适当的值
			int offset = 0; // TODO: 初始化为适当的值
			uint index = 0; // TODO: 初始化为适当的值
			int count = 0; // TODO: 初始化为适当的值
			target.GetBlock(buffer, offset, index, count);
			Assert.Inconclusive("无法验证不返回值的方法。");
		}

		[TestMethod()]
		public void GetBlockTest()
		{
			GetBlockTestHelper<GenericParameterHelper>();
		}

		/// <summary>
		///LowerBound 的测试
		///</summary>
		public void LowerBoundTestHelper<T>()
		{
			SparseArrayBase_Accessor target = new SparseArray_Accessor<T>(); // TODO: 初始化为适当的值
			uint expected = 0; // TODO: 初始化为适当的值
			uint actual;
			target.LowerBound = expected;
			actual = target.LowerBound;
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("验证此测试方法的正确性。");
		}

		[TestMethod()]
		[DeploymentItem("Buffers.exe")]
		public void LowerBoundTest()
		{
			LowerBoundTestHelper<GenericParameterHelper>();
		}

		/// <summary>
		///UpperBound 的测试
		///</summary>
		public void UpperBoundTestHelper<T>()
		{
			SparseArrayBase_Accessor target = new SparseArray_Accessor<T>(); // TODO: 初始化为适当的值
			uint expected = 0; // TODO: 初始化为适当的值
			uint actual;
			target.UpperBound = expected;
			actual = target.UpperBound;
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("验证此测试方法的正确性。");
		}

		[TestMethod()]
		[DeploymentItem("Buffers.exe")]
		public void UpperBoundTest()
		{
			UpperBoundTestHelper<GenericParameterHelper>();
		}
	}
}
