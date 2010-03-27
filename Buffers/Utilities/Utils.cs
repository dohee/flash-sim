using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Buffers.Utilities
{
	static class Utils
	{
		private static readonly uint[] LogTwo = new uint[32];
		private static readonly Stack<ConsoleColor> clrstack = new Stack<ConsoleColor>();

		static Utils()
		{
			string[] parts = Environment.GetCommandLineArgs()[0].Split('\\', '/');
			ProgramName = parts[parts.Length - 1];

			for (int i = 0; i < LogTwo.Length; i++)
				LogTwo[i] = (uint)1 << i;
		}

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
			Console.Error.WriteLine("{0}: {1}", ProgramName, message);
			PopColor();
		}
		public static void EmitErrMsg(string format, params object[] obj)
		{
			PushColor(ConsoleColor.White);
			Console.Error.Write(ProgramName + ": ");
			Console.Error.WriteLine(format, obj);
			PopColor();
		}

		public static sbyte ExpToLogTwo(uint number)
		{
			return (sbyte)Array.BinarySearch<uint>(LogTwo, number);
		}

		public static int FindDiff<T>(T[] array, T[] another)
		{
			if (array.Length != another.Length)
				return -3;

			return FindDiff(array, another, array.Length);
		}
		public static int FindDiff<T>(T[] array, T[] another, int length)
		{
			if (array == null && another == null)
				return -1;
			if (array == null || another == null)
				return -2;

			for (int i = 0; i < length; i++)
				if (!array[i].Equals(another[i]))
					return i;

			return -1;
		}

		public static string FormatDesc(params object[] args)
		{
			StringBuilder sb = new StringBuilder();
			
			for (int i = 1; i < args.Length; i+=2)
				sb.AppendFormat("{0}={1},", args[i - 1], args[i]);

			return sb.ToString().TrimEnd(',');
		}

		public static string FormatSpan(TimeSpan ts)
		{
			return string.Format("{0:0}:{1:00}:{2:00}.{3:000}",
				(int)ts.TotalHours, ts.Minutes, ts.Seconds, ts.Milliseconds);
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

		public static string ProgramName
		{
			get;
			private set;
		}


	}

}
