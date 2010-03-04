using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Gnu.Getopt;

namespace Buffers.Program
{
	static class CommandLine
	{
		public static void ShowUsage()
		{
			ShowUsage(Console.Error);
		}
		public static void ShowHelp()
		{
			ShowHelp(Console.Error);
		}

		public static void ShowUsage(TextWriter writer)
		{
			writer.WriteLine(
@"Usage: {0} [-r <ReadCost>] [-w <WriteCost>] [-c]/[-t <TraceLogPrefix>]
         -a <Algorithm>[,<Algorithm2>[,...]] -p <NPages>[,<NPages2>[,...]]
         [<TraceFile>]
       {0} [-r <ReadCost>] [-w <WriteCost>] -f/-F <OperatingFile>
         -a <Algorithm> -p <NPages>
         [<TraceFile>]",
				Utils.GetProgramName());
		}

		public static void ShowHelp(TextWriter writer)
		{
			writer.WriteLine(
@"例：
{0} -h
		显示此帮助
{0} -r 25 -w 200 -a LRU,Tn -p 150,300 trace80-20.txt
		你懂的
{0} -a LRU,Tn -p 150,300 trace80-20.txt
		同上，使用默认的 80/200 代价
{0} -a LRU,Tn trace80-20.txt
		同上，使用默认的 NPages = 1024  ((a))
{0} -a LRU,Tn
		同上，使用标准输入作为 trace
{0} -a 'LRU,CFLRU(0.5)'
		同上，用于 Linux。带括号的不加单引号会导致 bash 出错
{0} trace80-20.txt
		同(a)，算法是默认的 TrivalManager
{0}
		没参数，其实是同上，使用标准输入作为 trace
{0} -a LRU,Tn -c trace80-20.txt
		同(a)，带数据校验。PageID 不能太大，否则内存不足
{0} -a LRU,Tn -t path/trace/prefix trace80-20.txt
		同(a)，记录Cache后的trace到一组文件里
{0} -a Tn -f path/to/file trace80-20.txt
		同(a)，操作文件。-f 和 -F 因为需要操作文件看执行时间，
		所以都只允许只指定一个 -a 和一个 -p。-f 要求 PageID 小
		于文件大小，否则抛错。所以适合于手动创建大文件，然后运
		行本程序的情况
{0} -a Tn -F /dev/sda trace80-20.txt
		同(a)，操作 /dev/sda 设备。-F 不要求 PageID 小于文件大
		小。所以适合针对设备文件进行操作
{0} -a Tn -F \\.\PHYSICALDRIVE0 trace80-20.txt
		同上，在 Windows 下操作第一块物理磁盘。注意：一定要谨
		慎，否则会刷坏数据！",
				Utils.GetProgramName());
		}

		public static void ParseArguments(string[] args, out string filename,
			out decimal readCost, out decimal writeCost, out uint[] npageses,
			out AlgorithmSpec[] algorithms, out RunModeInfo mode)
		{
			// Init
			readCost = 80;
			writeCost = 200;
			npageses = new uint[] { 1024 };

			bool verify = false, tracelog = false;
			bool fileop = false, fileopWithoutCheckSize = false;
			string opfilename = null, tracelogfile = null;
			Regex regexAlgo = new Regex(@"(\w+)(?:\(([^)]+)\))?");
			List<AlgorithmSpec> algos = new List<AlgorithmSpec>();

			// Parse
			Getopt g = new Getopt(Utils.GetProgramName(), args, ":a:cf:F:hp:r:t:w:");
			g.Opterr = false;
			int c;

			while ((c = g.getopt()) != -1)
			{
				switch (c)
				{
					case 'a':
						foreach (Match m in regexAlgo.Matches(g.Optarg))
						{
							string name = m.Groups[1].Value;
							string[] arg = m.Groups[2].Value.Split(',');

							if (m.Groups[2].Success)
								algos.Add(new AlgorithmSpec(name, arg));
							else
								algos.Add(new AlgorithmSpec(name));
						}
						break;

					case 'c':
						verify = true;
						break;

					case 'f':
						fileop = true;
						fileopWithoutCheckSize = true;
						opfilename = g.Optarg;
						break;

					case 'F':
						fileop = true;
						fileopWithoutCheckSize = false;
						opfilename = g.Optarg;
						break;

					case 'h':
						throw new CmdLineHelpException();

					case 'p':
						string[] strs = g.Optarg.Split(',');
						npageses = new uint[strs.Length];

						for (int i = 0; i < strs.Length; i++)
						{
							if (!uint.TryParse(strs[i], out npageses[i]) || npageses[i] == 0)
								throw new InvalidCmdLineArgumentException(
									"Positive integer(s) are expected after -p");
						}

						break;

					case 'r':
						if (!decimal.TryParse(g.Optarg, out readCost) || readCost <= 0)
							throw new InvalidCmdLineArgumentException(
								"A positive integer is expected after -r");
						break;

					case 't':
						tracelog = true;
						tracelogfile = g.Optarg;
						break;

					case 'w':
						if (!decimal.TryParse(g.Optarg, out writeCost) || writeCost <= 0)
							throw new InvalidCmdLineArgumentException(
								"A positive integer is expected after -w");
						break;

					case ':':
						throw new InvalidCmdLineArgumentException(
							"Uncomplete argument: -" + (char)g.Optopt);
					case '?':
						throw new InvalidCmdLineArgumentException(
							"Invalid argument: -" + (char)g.Optopt);
					default:
						break;
				}
			}


			// Filename
			if (args.Length > g.Optind)
				filename = args[g.Optind];
			else
				filename = null;

			// Algorithm
			if (algos.Count == 0)
				algorithms = new AlgorithmSpec[] { new AlgorithmSpec("Trival") };
			else
				algorithms = algos.ToArray();

			// Run Mode
			if (fileop && (algorithms.Length > 1 || npageses.Length > 1))
				throw new InvalidCmdLineArgumentException(
					"In -f mode, only ONE algorithm and only ONE npages is allowed.");

			if (verify && fileop)
				throw new InvalidCmdLineArgumentException(
					"Cannot specify both -c and -f/-F");
			else if (verify && tracelog)
				throw new InvalidCmdLineArgumentException(
					"Cannot specify both -c and -s");
			else if (fileop && tracelog)
				throw new InvalidCmdLineArgumentException(
					"Cannot specify both -s and -f/-F");
			else if (verify)
				mode = new RunModeInfo(RunMode.Verify, null);
			else if (fileop)
				mode = new RunModeInfo(RunMode.File, new object[] {
					opfilename, fileopWithoutCheckSize });
			else if (tracelog)
				mode = new RunModeInfo(RunMode.Trace, tracelogfile);
			else
				mode = new RunModeInfo(RunMode.Normal, null);
		}
	}
}