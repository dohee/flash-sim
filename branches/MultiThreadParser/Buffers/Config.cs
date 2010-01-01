using System;
using System.Collections.Generic;
using System.Text;

namespace Buffers
{
	public static class Config
	{
		public static readonly int ReadCost = 66;
		public static readonly int WriteCost = 200;
	}

	public static class Utils
	{
		public static int CalcTotalCost(int read, int write)
		{
			return read * Config.ReadCost + write * Config.WriteCost;
		}
		public static int CalcTotalCost(IBlockDevice dev)
		{
			return CalcTotalCost(dev.ReadCount, dev.WriteCount);
		}

	}
}
