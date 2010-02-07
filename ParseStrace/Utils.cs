using System;
using System.Globalization;

namespace ParseStrace
{
	static class Utils
	{
		public static long ParseHexLong(string str)
		{
			long ret;
			
			if (long.TryParse(str, out ret))
				return ret;

			return long.Parse(str.Substring(2), NumberStyles.HexNumber);
		}
	}
}
