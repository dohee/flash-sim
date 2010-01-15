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
		static int processedLineCount = 0;

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

				DateTime old = DateTime.Now;
				DateTime nnew;

				try
				{
					OperateOnTrace(group, reader);
				}
				finally
				{
					nnew = DateTime.Now;
					if (tmr != null) tmr.Dispose();

					WriteCountOnStderr(null);
					Console.Error.WriteLine();
					ColorStack.PushColor(ConsoleColor.Magenta);
					Console.WriteLine(nnew - old);
					ColorStack.PopColor();
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


		private static void OperateOnTrace(ManagerGroup group, TextReader input)
		{
			byte[] data = new byte[group.PageSize];
			RandomDataGenerator generator = new RandomDataGenerator();
			string line;

			while ((line = input.ReadLine()) != null)
			{
				int lineCount = Interlocked.Increment(ref processedLineCount);
#if DEBUG
				if (lineCount > 10000)
					break;
				if (lineCount % 2000 == 0)
					WriteCountOnStderr(null);

				if (lineCount == 60)
					lineCount=60;
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

		private static void WriteCountOnStderr(object obj)
		{
			ColorStack.PushColor(ConsoleColor.Green);
			Console.Error.Write("\rProcessed {0} Lines.", processedLineCount);
			Console.Error.Flush();
			ColorStack.PopColor();
		}



		private class DevStatInfo
		{
			public string Title = null, Name = null, Description = null;
			public int Read = -1, Write = -1, Flush = -1, Level = -1;
			public long Cost = -1;
		}

		private static DevStatInfo[] FindDevice(IBlockDevice dev, int level)
		{
			// TODO 未完成
			// Group 下的 Mgr 可以压缩
			/*
Device
Manager
	Device
Manager
	Manager
		Device
Group
	Device
	Manager/Device
	Manager/Manager
		Device
	Manager/Group
		Device
		Manager/Device
		Manager/Manager
			Device
	Group
		Manager/Device
		Manager/Manager
			Device
			*/

			DevStatInfo info = new DevStatInfo();
			info.Title = "Dev";
			info.Level = level;
			info.Name = dev.Name;
			info.Description = (dev.Description == null ? "" : dev.Description);
			info.Read = dev.ReadCount;
			info.Write = dev.WriteCount;
			info.Flush = 0;
			info.Cost = Utils.CalcTotalCost(dev);

			IBufferManager mgr = dev as IBufferManager;
			
			if (mgr == null)
				return new DevStatInfo[] { info };


			List<DevStatInfo> infos = new List<DevStatInfo>();
			info.Flush = mgr.FlushCount;
			infos.Add(info);

			ManagerGroup grp = mgr as ManagerGroup;

			if (grp == null)
				infos.AddRange(FindDevice(mgr.AssociatedDevice, level + 1));
			else
				foreach (IBufferManager innermgr in grp)
					infos.AddRange(FindDevice(innermgr, level + 1));
			
			return infos.ToArray();
		}

		private static void GenerateOutput(IBlockDevice dev, TextWriter output)
		{
			DevStatInfo[] infos = FindDevice(dev, 0);
			int[] maxlens = { 0, 0, 0, 0, 0 };

			foreach (var info in infos)
			{
				info.Title = new string(' ', info.Level) + info.Title;
				info.Name = new string(' ', info.Level) + info.Name;
				maxlens[0] = Math.Max(maxlens[0], info.Title.Length);
				maxlens[1] = Math.Max(maxlens[1], info.Read.ToString().Length);
				maxlens[2] = Math.Max(maxlens[2], info.Write.ToString().Length);
				maxlens[3] = Math.Max(maxlens[3], info.Flush.ToString().Length);
				maxlens[4] = Math.Max(maxlens[4], info.Cost.ToString().Length);
			}

			string formatDev = string.Format("{{0,{0}}}  ", -maxlens[0]);
			string formatCost = string.Format(
				"Read {{0,{0}}}  Write {{1,{1}}}  Flush {{2,{2}}}  Cost {{3,{3}}}  ",
				maxlens[1], maxlens[2], maxlens[3], maxlens[4]);

			foreach (var info in infos)
			{
				ColorStack.PushColor(ConsoleColor.Yellow);
				output.Write(formatDev, info.Title);
				ColorStack.PopColor();
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
