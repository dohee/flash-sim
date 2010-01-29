//#define ANALISE

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Buffers.Managers;


namespace Buffers.Program
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

				CommandLine.ParseArguments(args, out filename, out readCost, out writeCost,
					out npageses, out algorithms, out verify);

				Config.SetConfig(readCost, writeCost);
				reader = InitReader(filename);
				group = GroupOp.InitGroup(npageses, algorithms, verify);

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
					GroupOp.VerifyData(group);
					Console.WriteLine("Data verification succeeded.");
				}
			}
			catch (InvalidCmdLineArgumentException ex)
			{
				Utils.EmitErrMsg(ex.Message);
				CommandLine.ShowUsage();
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
			char[] separators1 = { '#', ';', '/' };
			char[] separators2 = { ' ', '\t' };
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

				string[] parts = line.Split(separators1, 2);
				line = parts[0].Trim(separators2);

				if (string.IsNullOrEmpty(line))
				{
					string nlines = Regex.Match(parts[1], @"Lines: (\d+)").Groups[1].Value;

					if (!string.IsNullOrEmpty(nlines))
						totalLineCount = long.Parse(nlines);

					continue;
				}

				parts = line.Split(separators2, StringSplitOptions.RemoveEmptyEntries);

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
			DevStatInfo[] infos = GroupOp.GatherStatistics(dev, 0, -1, false);
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

	}
}
