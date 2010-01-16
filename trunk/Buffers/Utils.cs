using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Buffers
{
	static class Utils
	{
		public static long CalcTotalCost(int read, int write)
		{
			return read * (long)Config.ReadCost + write * (long)Config.WriteCost;
		}
		public static long CalcTotalCost(IBlockDevice dev)
		{
			return CalcTotalCost(dev.ReadCount, dev.WriteCount);
		}

		public static int FindDiff<T>(T[] array, T[] another)
		{
			if (array == null && another == null)
				return -1;
			if (array == null || another == null)
				return -2;
			if (array.Length != another.Length)
				return -3;

			for (int i = 0; i < array.Length; i++)
				if (!array[i].Equals(another[i]))
					return i;

			return -1;
		}
		public static bool ArrayEqual<T>(T[] array, T[] another)
		{
			return (FindDiff(array, another) == -1);
		}

		public static string FormatDescription(params object[] args)
		{
			StringBuilder sb = new StringBuilder();
			
			for (int i = 1; i < args.Length; i+=2)
				sb.AppendFormat("{0}={1};", args[i - 1], args[i]);

			return sb.ToString().TrimEnd(',');
		}
	}
}
