using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Buffers
{
	static class Utils
	{
		private static readonly Stack<ConsoleColor> clrstack = new Stack<ConsoleColor>();
		private static string progname = null;


		public static bool ArrayEqual<T>(T[] array, T[] another)
		{
			return (FindDiff(array, another) == -1);
		}

		public static decimal CalcTotalCost(int read, int write)
		{
			return read * Config.ReadCost + write * Config.WriteCost;
		}
		public static decimal CalcTotalCost(IBlockDevice dev)
		{
			return CalcTotalCost(dev.ReadCount, dev.WriteCount);
		}

		public static void EmitErrMsg(string message)
		{
			PushColor(ConsoleColor.Red);
			Console.Error.WriteLine("{0}: {1}", GetProgramName(), message);
			PopColor();
		}
		public static void EmitErrMsg(string format, params object[] obj)
		{
			PushColor(ConsoleColor.White);
			Console.Error.Write(GetProgramName() + ": ");
			Console.Error.WriteLine(format, obj);
			PopColor();
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

		public static string FormatDescription(params object[] args)
		{
			StringBuilder sb = new StringBuilder();
			
			for (int i = 1; i < args.Length; i+=2)
				sb.AppendFormat("{0}={1},", args[i - 1], args[i]);

			return sb.ToString().TrimEnd(',');
		}

		public static string FormatSpan(TimeSpan ts)
		{
			return string.Format("{0:0}:{1:00}:{2:00}",
				(int)ts.TotalHours, ts.Minutes, ts.Seconds);
		}

		public static string GetProgramName()
		{
			if (progname == null)
			{
				string[] parts = Environment.GetCommandLineArgs()[0].Split('\\', '/');
				progname = parts[parts.Length - 1];
			}

			return progname;
		}

		public static void PopColor()
		{
			Console.ForegroundColor = clrstack.Pop();
		}

		public static void PushColor(ConsoleColor newcolor)
		{
			clrstack.Push(Console.ForegroundColor);
			Console.ForegroundColor = newcolor;
		}

	}
}
