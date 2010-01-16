//#define ANALISE

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Buffers;
using Buffers.Devices;
using Buffers.Managers;


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

			try
			{
				if (args.Length >= 1)
					reader = new StreamReader(args[0], Encoding.Default, true, 32 * 1024 * 1024);
				else
					reader = Console.In;

				group = (Config.RunVerify ?
					Config.InitVerifyGroup() : Config.InitGroup());

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


				if (Config.RunVerify)
				{
					VerifyData(group);
					Console.WriteLine("Data verification succeeded.");
				}
			}
			catch (FileNotFoundException)
			{
				EmitErrMsg("File {0} not found", args[0]);
			}
			catch (DataNotConsistentException ex)
			{
				EmitErrMsg(ex.Message);
			}
			catch (Exception ex)
			{
				EmitErrMsg(ex.ToString());
			}
			finally
			{
				if (group != null)
					GenerateOutput(group, Console.Out);
				if (reader != null)
					reader.Dispose();
			}
		}

		private static void EmitErrMsg(string message)
		{
			ColorStack.PushColor(ConsoleColor.Red);
			Console.Error.WriteLine("{0}: {1}", Environment.GetCommandLineArgs()[0], message);
			ColorStack.PopColor();
		}
		private static void EmitErrMsg(string format, params object[] obj)
		{
			ColorStack.PushColor(ConsoleColor.White);
			Console.Error.Write(Environment.GetCommandLineArgs()[0]);
			Console.Error.WriteLine(": " + format, obj);
			ColorStack.PopColor();
		}
		private static string FormatSpan(TimeSpan ts)
		{
			return string.Format("{0:0}:{1:00}:{2:00}",
				(int)ts.TotalHours, ts.Minutes, ts.Seconds);
		}

		private static void WriteCountOnStderr(object obj)
		{
			long lineCount = Interlocked.Read(ref processedLineCount);
			TimeSpan span = DateTime.Now - oldTime;

			ColorStack.PushColor(ConsoleColor.Green);
			Console.Error.Write("\rProcessed " + lineCount);

			if (totalLineCount != 0)
				Console.Error.Write("/{0} ({1:P})", totalLineCount, (float)processedLineCount / totalLineCount);

			Console.Error.Write(", Time " + FormatSpan(span));

			if (totalLineCount != 0)
			{
				string remainstr;

				if (lineCount != 0)
				{
					TimeSpan total = new TimeSpan((long)(span.Ticks * ((float)totalLineCount / lineCount)));
					TimeSpan remain = total - span;
					if (remain.Ticks < 0)
						remain = new TimeSpan(0);

					remainstr = string.Format("{0:00}'{1:00}\"", (int)remain.TotalMinutes, remain.Seconds);
				}
				else
				{
					remainstr = "[N/A]";
				}

				Console.Error.Write(" ({0})", remainstr);
			}

			Console.Error.Flush();
			ColorStack.PopColor();
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
				if (lineCount > 10000)
					break;
				if (lineCount % 2000 == 0)
					WriteCountOnStderr(null);

				if (lineCount == 60)
					lineCount = 60;
#endif

				string[] parts = line.Split('#');
				line = parts[0];

				parts = line.Split(new char[] { ' ', '\t' },
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

			group.Flush();
		}


		private class DevStatInfo
		{
			public string Id = null, Name = null, Description = null;
			public int Read = -1, Write = -1, Flush = -1;
			public long Cost = -1;
			public bool Suppress;
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

			string formatId = string.Format("{{0,{0}}}  ", -maxlens[0]);
			string formatCost = string.Format(
				"Read {{0,{0}}}  Write {{1,{1}}}  Flush {{2,{2}}}  Cost {{3,{3}}}  ",
				maxlens[1], maxlens[2], maxlens[3], maxlens[4]);
			string emptyCost = new string(' ',
				string.Format(formatCost, 0, 0, 0, 0).Length);

			foreach (var info in infos)
			{
				ColorStack.PushColor(ConsoleColor.Yellow);
				output.Write(formatId, info.Id);
				ColorStack.PopColor();

				if (info.Suppress)
					output.Write(emptyCost);
				else
					output.Write(formatCost, info.Read, info.Write, info.Flush, info.Cost);

				ColorStack.PushColor(ConsoleColor.Cyan);
				output.Write(info.Name + " ");
				ColorStack.PopColor();

				ColorStack.PushColor(ConsoleColor.DarkGray);
				output.Write(info.Description);
				ColorStack.PopColor();

				output.WriteLine();
			}
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
			var lyfMap = ((CMFT)group[1]).map;
			var catMap = ((CMFTByCat)group[2]).map;

			if (lyfMap.Count != catMap.Count)
				return true;

			foreach (var lyfItem in lyfMap)
			{
				Buffers.Memory.IRRFrame lyfFrame = lyfItem.Value;
				Buffers.Queues.QueueNode node;

				if (catMap.TryGetValue(lyfItem.Key, out node))
				{
					Buffers.Memory.IRRFrame catFrame = (Buffers.Memory.IRRFrame)node.ListNode.Value;

					if (lyfFrame.DataSlotId != catFrame.DataSlotId ||
						lyfFrame.Dirty != catFrame.Dirty ||
						lyfFrame.ReadIRR != catFrame.ReadIRR ||
						lyfFrame.ReadRecency != catFrame.ReadRecency ||
						lyfFrame.WriteIRR != catFrame.WriteIRR ||
						lyfFrame.WriteRecency != catFrame.WriteRecency)
						return true;
				}
				else
				{
					return true;
				}
			}

			return false;
		}
#endif

	}
}
