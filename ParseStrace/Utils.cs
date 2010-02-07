using System;
using System.Linq;
using System.Globalization;

namespace ParseStrace
{
	static class Utils
	{


		public static long ParseHexLong(this string str)
		{
			long ret;
			
			if (long.TryParse(str, out ret))
				return ret;

			return long.Parse(str.Substring(2), NumberStyles.HexNumber);
		}
	}

	static class EnumExtentions
	{
		public static string AccessTypeToString(this AccessType access)
		{
			string str = ((access & AccessType.FileRoutine) != 0) ? "File" : "Mmap";
			str += ((access & AccessType.Read) != 0) ? " Read" : " Write";
			return str;
		}

		public static string FDTypeToString(this FDType fdtype)
		{
			switch (fdtype)
			{
				case FDType.Unknown: return "Unkn";
				case FDType.File: return "File";
				case FDType.Terminal: return "Term";
				case FDType.Pipe: return "Pipe";
				case FDType.Socket: return "Sock";
				default: return null;
			}
		}
	}
}
