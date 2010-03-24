using Buffers.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuffersTest
{
    /// <summary>
    ///这是 SparseArrayTest 的测试类，旨在
    ///包含所有 SparseArrayTest 单元测试
    ///</summary>
	[DeploymentItem("Buffers.exe")]
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

			t.SetBlock(a, 20, (uint)(10 * lowlength - 10), 11);
			t.SetBlock(a, lowlength - 50, (uint)(13 * lowlength - 50), 150);

			return t;
		}
		private SparseArray_Accessor<GenericParameterHelper> CreateGeneric()
		{
			var t = new SparseArray_Accessor<GenericParameterHelper>();
			int lowlength = SparseArray_Accessor<GenericParameterHelper>.LowPartLength;

			t[5, 5] = null;
			t[7, 7] = new GenericParameterHelper(7);

			var a = new GenericParameterHelper[lowlength + 100];
			for (int i = 30; i < lowlength; i++)
				a[i] = new GenericParameterHelper(i);

			t.SetBlock(a, 20, (uint)(10 * lowlength - 10), 11);
			t.SetBlock(a, lowlength - 50, (uint)(13 * lowlength - 50), 150);

			return t;
		}
		#endregion

		[TestMethod()]
		public void ItemTest()
		{
			var t = CreateInt();

			uint lowlength = (uint)SparseArray_Accessor<int>.LowPartLength;
			Assert.AreEqual(0, t[0, 0]);
			Assert.AreEqual(0, t[0]);
			Assert.AreEqual(0, t[5, 5]);
			Assert.AreEqual(0, t[5 * lowlength + 5]);
			Assert.AreEqual(7, t[7, 7]);
			Assert.AreEqual(7, t[7 * lowlength + 7]);

			ItemTestHelper(t);
			ItemTestHelper(CreateGeneric());
		}
		public void ItemTestHelper<T>(SparseArray_Accessor<T> t)
		{
			Assert.IsNull(t.array[1]);
			Assert.IsNull(t.array[5]);
			Assert.IsNotNull(t.array[7]);
		}

		[TestMethod()]
		public void SetBlockTest()
		{
			var t = CreateInt();

			uint lowlength = (uint)SparseArray_Accessor<int>.LowPartLength;
			Assert.AreEqual(0, t[9, (int)(lowlength - 1)]);
			Assert.AreEqual(0, t[9 * lowlength + lowlength - 1]);
			Assert.AreEqual(30, t[10, 0]);
			Assert.AreEqual(30, t[10 * lowlength + 0]);

			SetBlockTestHelper(t);
			SetBlockTestHelper(CreateGeneric());
		}
		public void SetBlockTestHelper<T>(SparseArray_Accessor<T> t)
		{
			Assert.IsNull(t.array[9]);
			Assert.IsNotNull(t.array[10]);
			Assert.IsNotNull(t.array[12]);
			Assert.IsNull(t.array[13]);
		}

		[TestMethod()]
		public void BoundsTest()
		{
			BoundsTestHelper<GenericParameterHelper>();
		}
		public void BoundsTestHelper<T>()
		{
		}


	}
}
