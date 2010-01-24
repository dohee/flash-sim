//#define ANALISE

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Buffers;
using Buffers.Devices;
using Buffers.Managers;
using Gnu.Getopt;
using Buffers.Memory;


namespace Buffers
{
	class Program
	{
		static long processedLineCount = 0, totalLineCount = 0;
		static DateTime oldTime;


		public static void Main(string[] args)
		{
			TextReader reader = null;
			ManagerGroup group = null;
			string filename = null;

			try
			{
				int readCost, writeCost;
				uint[] npageses;
				AlgorithmSpec[] algorithms;
				bool verify;
				
				ParseArguments(args, out filename, out readCost, out writeCost,
					out npageses, out algorithms, out verify);

				Config.SetConfig(readCost, writeCost);
				reader = InitReader(filename);
				group = InitSuperGroup(npageses, algorithms, verify);

#if DEBUG
				Timer tmr = null;
#else
				Timer tmr = new Timer(WriteCountOnStderr, null, 0, 500);
#endif

				oldTime = DateTime.Now;

				try
				{
					OperateOnTrace(group, reader);
				}
				finally
				{
					if (tmr != null) tmr.Dispose();
					WriteCountOnStderr(null);
					Console.Error.WriteLine();
				}


				if (verify)
				{
					VerifyData(group);
					Console.WriteLine("Data verification succeeded.");
				}
			}
			catch (InvalidCmdLineArgumentException ex)
			{
				Utils.EmitErrMsg(ex.Message);
				ShowUsage();
			}
			catch (FileNotFoundException)
			{
				Utils.EmitErrMsg("File {0} not found", filename);
			}
			catch (DataNotConsistentException ex)
			{
				Utils.EmitErrMsg(ex.Message);
			}
			catch (Exception ex)
			{
				Utils.EmitErrMsg(ex.ToString());
			}
			finally
			{
				if (group != null)
					GenerateOutput(group, Console.Out);
				if (reader != null)
					reader.Dispose();
			}
		}


		private static void ShowUsage()
		{
			Console.Error.WriteLine(
@"Usage: {0} [-c] [-r <ReadCost>] [-w <WriteCost>]
       -a <Algorithm>[,<Algorithm2>[,...]]
       -p <NPages>[,<NPages2>[,...]]
       [<Filename>]",
				Utils.GetProgramName());
		}

		private static void ParseArguments(string[] args, out string filename,
			out int readCost,out int writeCost, out uint[] npageses,
			out AlgorithmSpec[] algorithms, out bool verify)
		{
			readCost = 80;
			writeCost = 200;
			npageses = new uint[] { 1024 };
			verify = false;

			Regex regexAlgo = new Regex(@"(\w+)(?:\(([^)]+)\))?");
			List<AlgorithmSpec> algos = new List<AlgorithmSpec>();

			Getopt g = new Getopt(Utils.GetProgramName(), args, ":a:cp:r:w:");
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

					case 'p':
						string[] strs = g.Optarg.Split(',');
						npageses = new uint[strs.Length];

						for (int i = 0; i < strs.Length; i++)
						{
							if (!uint.TryParse(strs[i], out npageses[i]) || npageses[i] == 0)
								throw new InvalidCmdLineArgumentException("Positive integer(s) are expected after -p");
						}

						break;

					case 'r':
						if (!int.TryParse(g.Optarg, out readCost) || readCost <=0)
							throw new InvalidCmdLineArgumentException("A positive integer is expected after -r");
						break;

					case 'w':
						if (!int.TryParse(g.Optarg, out writeCost) || writeCost<=0)
							throw new InvalidCmdLineArgumentException("A positive integer is expected after -w");
						break;

					case ':':
						throw new InvalidCmdLineArgumentException("Uncomplete argument: -" + (char)g.Optopt);
					case '?':
						throw new InvalidCmdLineArgumentException("Invalid argument: -" + (char)g.Optopt);
					default:
						break;
				}
			}


			if (algos.Count == 0)
				algorithms = new AlgorithmSpec[] { new AlgorithmSpec("Trival") };
			else
				algorithms = algos.ToArray();

			if (args.Length > g.Optind)
				filename = args[g.Optind];
			else
				filename = null;
		}


		private static TextReader InitReader(string filename)
		{
			if (string.IsNullOrEmpty(filename))
			{
				Console.WriteLine("Reading trace from stdin...");
				return Console.In;
			}
			else
			{
				Console.WriteLine("Reading trace from file '{0}'...", filename);
				return new StreamReader(filename, Encoding.Default, true, 32 * 1024 * 1024);
			}
		}

		private static ManagerGroup InitSuperGroup(uint[] npageses, AlgorithmSpec[] algorithms, bool verify)
		{
			if (npageses.Length == 1)
				return InitGroup(npageses[0], algorithms, verify);

			ManagerGroup group = new ManagerGroup();

			foreach (uint npages in npageses)
				group.Add(InitGroup(npages, algorithms, verify));

			return group;
		}

		private static ManagerGroup InitGroup(uint npages, AlgorithmSpec[] algorithms, bool verify)
		{
			ManagerGroup group = new ManagerGroup();

			foreach (AlgorithmSpec algo in algorithms)
			{
				try
				{
					group.Add(Config.CreateManager(algo.Name, npages, algo.Arguments, verify));
				}
				catch (Exception ex)
				{
					throw new InvalidCmdLineArgumentException(
						"Exception occurs when creating " + algo.Name +
						". Details: " + ex.Message,
						ex);
				}
			}

			return group;
		}


		private static void WriteCountOnStderr(object obj)
		{
			long lineCount = Interlocked.Read(ref processedLineCount);
			TimeSpan span = DateTime.Now - oldTime;

			Utils.PushColor(ConsoleColor.Green);
			Console.Error.Write("\rProcessed " + lineCount);

			if (totalLineCount != 0)
				Console.Error.Write("/{0} ({1:P})", totalLineCount, (float)processedLineCount / totalLineCount);

			Console.Error.Write(". Elapsed " + Utils.FormatSpan(span));

			if (totalLineCount != 0)
			{
				string remainstr;

				if (lineCount != 0)
				{
					TimeSpan total = new TimeSpan((long)(span.Ticks * ((float)totalLineCount / lineCount)));
					TimeSpan remain = total - span;
					if (remain.Ticks < 0)
						remain = new TimeSpan(0);

					remainstr = Utils.FormatSpan(remain);
				}
				else
				{
					remainstr = "[N/A]";
				}

				Console.Error.Write(", remaining {0}", remainstr);
			}

			Console.Error.Write("          ");
			Console.Error.Flush();
			Utils.PopColor();
		}


		private static void OperateOnTrace(ManagerGroup group, TextReader input)
		{
			byte[] data = new byte[group.PageSize];
			RandomDataGenerator generator = new RandomDataGenerator();
			string line;

			while ((line = input.ReadLine()) != null)
			{
				long lineCount = Interlocked.Increment(ref processedLineCount);
#if DEBUG
				if (lineCount >= 10000)
					break;
				if (lineCount % 1000 == 0)
					WriteCountOnStderr(null);

				if (lineCount == 5608)
					lineCount = 5608;
#endif

				if (line.StartsWith("# "))
				{
					string nlines = Regex.Match(line, @"^# Lines: (\d+)").Groups[1].Value;

					if (!string.IsNullOrEmpty(nlines))
						totalLineCount = long.Parse(nlines);

					continue;
				}

				line = line.Split('#')[0];

				string[] parts = line.Split(new char[] { ' ', '\t' },
					StringSplitOptions.RemoveEmptyEntries);

				if (parts.Length < 3)
					continue;

				uint pageid = uint.Parse(parts[0]);
				uint length = uint.Parse(parts[1]);
				uint rw = uint.Parse(parts[2]);

				if (rw == 0)
				{
					while (length-- != 0)
						group.Read(pageid++, data);
				}
				else
				{
					while (length-- != 0)
					{
						generator.Generate(data);
						group.Write(pageid++, data);
					}
				}
			}

			group.CascadeFlush();
		}

		private static void GenerateOutput(IBlockDevice dev, TextWriter output)
		{
			DevStatInfo[] infos = FindDevice(dev, 0, -1, false);
			int[] maxlens = { 0, 0, 0, 0, 0 };

			foreach (var info in infos)
			{
				maxlens[0] = Math.Max(maxlens[0], info.Id.Length);
				maxlens[1] = Math.Max(maxlens[1], info.Read.ToString().Length);
				maxlens[2] = Math.Max(maxlens[2], info.Write.ToString().Length);
				maxlens[3] = Math.Max(maxlens[3], info.Flush.ToString().Length);
				maxlens[4] = Math.Max(maxlens[4], info.Cost.ToString().Length);
			}

			string formatId = string.Format("{{0,{0}}} ", -maxlens[0]);
			string formatCost = string.Format(
				"R:{{0,{0}}}  W:{{1,{1}}}  F:{{2,{2}}}  C:{{3,{3}}} ",
				maxlens[1], maxlens[2], maxlens[3], maxlens[4]);
			string emptyCost = new string(' ',
				string.Format(formatCost, 0, 0, 0, 0).Length);

			foreach (var info in infos)
			{
				Utils.PushColor(ConsoleColor.Yellow);
				output.Write(formatId, info.Id);
				Utils.PopColor();

				if (info.Suppress)
					output.Write(emptyCost);
				else
					output.Write(formatCost, info.Read, info.Write, info.Flush, info.Cost);

				Utils.PushColor(ConsoleColor.Cyan);
				output.Write(info.Name + " ");
				Utils.PopColor();

				Utils.PushColor(ConsoleColor.DarkGray);
				output.Write(info.Description);
				Utils.PopColor();

				output.WriteLine();
			}
		}


		private static DevStatInfo[] FindDevice(IBlockDevice dev, int level, int index, bool suppress)
		{
			IBufferManager mgr = dev as IBufferManager;
			ManagerGroup grp = mgr as ManagerGroup;

			DevStatInfo info = new DevStatInfo();
			info.Id = new string(' ', level) +
				(mgr == null ? "Dev" : grp == null ? "Mgr" : "Group") +
				(index < 0 ? "" : index.ToString());
			info.Name = new string(' ', level) + dev.Name;
			info.Description = (dev.Description == null ? "" : dev.Description);
			info.Read = dev.ReadCount;
			info.Write = dev.WriteCount;
			info.Flush = 0;
			info.Cost = Utils.CalcTotalCost(dev);
			info.Suppress = suppress;

			if (mgr == null)
				return new DevStatInfo[] { info };

			List<DevStatInfo> infos = new List<DevStatInfo>();
			info.Flush = mgr.FlushCount;
			infos.Add(info);

			if (grp == null)
				infos.AddRange(FindDevice(mgr.AssociatedDevice, level + 1, -1, false));
			else
				for (int i = 0; i < grp.Count; i++)
					infos.AddRange(FindDevice(grp[i], level + 1, i, true));

			return infos.ToArray();
		}


		private static void VerifyData(ManagerGroup group)
		{
			byte[] correct = (group[0].AssociatedDevice as MemorySimulatedDevice).ToArray();

			for (int i = 1; i < group.Count; i++)
			{
				byte[] current = (group[i].AssociatedDevice as MemorySimulatedDevice).ToArray();
				int diffpos = Utils.FindDiff(correct, current);

				if (diffpos != -1)
					throw new DataNotConsistentException(string.Format(
						"Verified data not consistent at Page {0} between Device 0 and Device {1}",
						diffpos, i));
			}
		}



		private static void AnalyseAndOutput(ManagerGroup group, int count)
		{
#if ANALISE
			if (FindBug(group, count))
				Console.WriteLine(count);
#endif
		}

#if ANALISE
		private static bool FindBug(ManagerGroup group, int count)
		{
			return false;
		}

		private static int CompareFrame(IFrame f1, IFrame f2)
		{
			int r;
			if ((r = f1.Id.CompareTo(f2.Id)) != 0)
				return r;
			if ((r = f1.Dirty.CompareTo(f2.Dirty)) != 0)
				return r;
			return 0;
		}
#endif





		private class DevStatInfo
		{
			public string Id, Name, Description;
			public int Read, Write, Flush;
			public long Cost;
			public bool Suppress;
		}


		private struct AlgorithmSpec
		{
			public string Name;
			public string[] Arguments;

			public AlgorithmSpec(string name)
				: this(name, new string[0]) { }

			public AlgorithmSpec(string name, string[] arguments)
			{
				Name = name;
				Arguments = arguments;
			}
		}
	}

}
