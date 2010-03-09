//#define ANALISE

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Buffers.Managers;
using Buffers.Memory;


namespace Buffers.Program
{
	class Program
	{
		static long processedLineCount = 0, totalLineCount = 0;
		static DateTime oldTime;


		public static int Main(string[] args)
		{
			TextReader reader = null;
			ManagerGroup group = null;
			string filename = null;

			try
			{
				decimal readCost, writeCost;
				uint[] npageses;
				AlgorithmSpec[] algorithms;
				RunModeInfo runmode;

				CommandLine.ParseArguments(args, out filename, out readCost, out writeCost,
					out npageses, out algorithms, out runmode);

				Config.SetConfig(readCost, writeCost);
				reader = InitReader(filename);
				group = GroupOp.InitGroup(npageses, algorithms, runmode);

#if DEBUG
				Timer tmr = null;
#else
				Timer tmr = new Timer(
					delegate(object obj) { WriteCount(Console.Error, true); },
					null, 0, 500);
#endif

				oldTime = DateTime.Now;

				try
				{
					OperateOnTrace(group, reader, (runmode.Mode == RunMode.Verify));
				}
				finally
				{
					if (tmr != null) tmr.Dispose();
					WriteCount(Console.Error, true);
					Console.Error.Write('\r');
					Console.Error.Flush();

					WriteCount(Console.Out, false);
					Console.Out.WriteLine();
				}


				if (runmode.Mode == RunMode.Verify)
				{
					GroupOp.VerifyData(group);
					Console.WriteLine("Data verification succeeded.");
				}

				return 0;
			}
#if! DEBUG
			catch (CmdLineHelpException)
			{
				CommandLine.ShowUsage(Console.Out);
				Console.Out.WriteLine();
				CommandLine.ShowHelp(Console.Out);
				return 0;
			}
			catch (InvalidCmdLineArgumentException ex)
			{
				Utils.EmitErrMsg(ex.Message);
				CommandLine.ShowUsage();
				return 1;
			}
			catch (FileNotFoundException)
			{
				Utils.EmitErrMsg("File {0} not found", filename);
				return 2;
			}
			catch (DataNotConsistentException ex)
			{
				Utils.EmitErrMsg(ex.Message);
				return 3;
			}
			catch (Exception ex)
			{
				Utils.EmitErrMsg(ex.ToString());
				return 9;
			}
#endif
			finally
			{
				if (group != null)
				{
					GenerateOutput(group, Console.Out);
					group.Dispose();
				}
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
				return new StreamReader(new FileStream(
					filename, FileMode.Open, FileAccess.Read, FileShare.Read,
					1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan),
					Encoding.Default, true, 1024 * 1024);
			}
		}

		private static void WriteCount(TextWriter writer, bool carrage)
		{
			long lineCount = Interlocked.Read(ref processedLineCount);
			TimeSpan span = DateTime.Now - oldTime;
			StringBuilder sb = new StringBuilder(100);

			if (carrage)
				sb.Append('\r');

			sb.Append("Line " + lineCount);

			if (totalLineCount != 0)
				sb.AppendFormat("/{0}", totalLineCount, (float)lineCount / totalLineCount);

			sb.Append(". Time " + Utils.FormatSpan(span));

			if (totalLineCount != 0)
			{
				string totalstr;

				if (lineCount != 0)
					totalstr = Utils.FormatSpan(new TimeSpan(
						(long)(span.Ticks * ((float)totalLineCount / lineCount))));
				else
					totalstr = "[N/A]";

				sb.AppendFormat("/{0} ({1:P})", totalstr, (float)lineCount / totalLineCount);
			}

			sb.AppendFormat("    ");
			string str = sb.ToString();

			Utils.PushColor(ConsoleColor.Green);
			writer.Write(str);
			writer.Flush();
			Utils.PopColor();
		}


		private static void OperateOnTrace(ManagerGroup group, TextReader input, bool generateData)
		{
			char[] separators1 = { '#', ';', '/' };
			char[] separators2 = { ' ', '\t', ',' };
			GroupAccessor accessor = new GroupAccessor(group, generateData);
			TraceParser parser = null;
			string line;

			while ((line = input.ReadLine()) != null)
			{
				long lineCount = Interlocked.Increment(ref processedLineCount);
#if DEBUG
				if (lineCount >= 200000)
					break;
				if (lineCount % 5000 == 0)
					WriteCount(Console.Error, true);

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

				if (parser == null)
					parser = TraceParser.CreateParser(parts);

				uint pageid, length;
				AccessType type;
				parser.ParseLine(parts, out pageid, out length, out type);

				for (uint i = 0; i < length; i++)
					accessor.Access(new RWQuery(pageid + i, type));
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

				//if (info.Suppress)
					//output.Write(emptyCost);
				//else
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
