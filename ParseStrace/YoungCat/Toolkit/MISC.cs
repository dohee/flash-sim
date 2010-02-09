using System;
using System.Collections.Generic;
using System.Linq;

namespace YoungCat.Toolkit
{
	public static class Extention
	{
		/// <summary>
		/// 使用默认的比较器在整个已排序的 SortedList 中搜索元素，
		/// 并返回该元素从零开始的索引。
		/// </summary>
		/// <param name="item">要定位的对象。</param>
		/// <returns>
		/// 如果找到 item，则为已排序的 SortedList 中 item 的从零开始的索引；
		/// 否则为一个负数，该负数是大于 item 的第一个元素的索引的按位求补。
		/// 如果没有更大的元素，则为 Count 的按位求补。
		/// </returns>
		public static int BinarySearch<TKey, TValue>(this SortedList<TKey, TValue> list, TKey item)
		{
			if (list.Count == 0)
				return -1;

			IComparer<TKey> comparer = list.Comparer;
			int index = 0, length = list.Count;

			int num = index;
			int num2 = (index + length) - 1;

			while (num <= num2)
			{
				int num3 = num + ((num2 - num) >> 1);
				int num4 = comparer.Compare(list.Keys[num3], item);

				if (num4 == 0)
					return num3;
				else if (num4 < 0)
					num = num3 + 1;
				else
					num2 = num3 - 1;
			}

			return ~num;
		}

	}
}
