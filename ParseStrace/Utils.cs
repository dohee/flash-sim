using System;
using System.Linq;
using System.Globalization;

namespace ParseStrace
{
	static class Utils
	{
		private static string progname = null;

		public static string GetProgramName()
		{
			if (progname == null)
			{
				string[] parts = Environment.GetCommandLineArgs()[0].Split('\\', '/');
				progname = parts[parts.Length - 1];
			}

			return progname;
		}

		public static long ParseHexLong(this string str)
		{
			long ret;
			
			if (long.TryParse(str, out ret))
				return ret;

			return long.Parse(str.Substring(2), NumberStyles.HexNumber);
		}
	}


}
