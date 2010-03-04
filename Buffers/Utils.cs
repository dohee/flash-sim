using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if! __MonoCS__
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
#endif


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

		public static string FormatDescription(params object[] args)
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


#if! __MonoCS__
	static class UnmanagedFileIO
	{
		public const short FILE_ATTRIBUTE_NORMAL = 0x80;
		public const uint GENERIC_READ = 0x80000000;
		public const uint GENERIC_WRITE = 0x40000000;
		public const uint FILE_SHARE_READ = 0x00000001;
		public const uint FILE_SHARE_WRITE = 0x00000002;
		public const uint FILE_SHARE_DELETE = 0x00000004;
		public const uint CREATE_NEW = 1;
		public const uint CREATE_ALWAYS = 2;
		public const uint OPEN_EXISTING = 3;
		public const uint OPEN_ALWAYS = 4;
		public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
		public const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern SafeFileHandle CreateFile(
			string lpFileName,
			uint dwDesiredAccess,
			uint dwShareMode,
			IntPtr lpSecurityAttributes,
			uint dwCreationDisposition,
			uint dwFlagsAndAttributes,
			IntPtr hTemplateFile);
	}
#endif

}
