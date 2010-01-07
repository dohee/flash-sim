//#define ANALISE

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Buffers;
using Buffers.Devices;
using Buffers.Managers;


namespace Buffers
{
	class Program
	{
		private static ManagerGroup InitGroup()
		{
			const uint npages = 4;
			uint ratio = (uint)Math.Round((double)Config.WriteCost / (double)Config.ReadCost);
			ManagerGroup group = new ManagerGroup();

			//group.Add(new LRU(npages));
			//group.Add(Wrapper.CreateCFLRU(npages, npages / 2));
			//group.Add(Wrapper.CreateCFLRUD(npages));
			//group.Add(Wrapper.CreateLRUWSR(npages));
			//group.Add(new Tn(npages, ratio, new TnConfig(false, false, 0, 0, false)));
			//group.Add(new Tn(npages, ratio, new TnConfig(false, true, 0, 0, false)));
			//group.Add(new Tn(npages, ratio, new TnConfig(true, false, 0, 0, false)));
			//group.Add(new Tn(npages, ratio, new TnConfig(true, true, 0, 0, false)));
			//group.Add(new Tn(npages, ratio, new TnConfig(true, false, npages / 4, npages / 2, false)));
            group.Add(new FLIRSbyLyf(npages)); 
			//group.Add(new Tn(npages, ratio, new TnConfig(true, false, npages / 4, 0, true)));
			//group.Add(new CMFTByCat(npages));
			//group.Add(new OldBlowerByCat(npages));
			//group.Add(new BlowerByCat(npages));
			//group.Add(new BlowerByLyf(npages));
			//group.Add(new BlowerByLyf2(npages));

			return group;
		}

		public static void Main(string[] args)
		{
			ManagerGroup group = InitGroup();
			TextReader reader = null;

			try
			{
				if (args.Length >= 1)
					reader = new StreamReader(args[0]);
				else
					reader = Console.In;

				DateTime old = DateTime.Now;
				OperateOnTrace(group, reader);
				TimeSpan ts = DateTime.Now - old;

				PushColor(ConsoleColor.Magenta);
				Console.WriteLine(ts);
				PopColor();
			}
			finally
			{
				if (reader != null)
					reader.Dispose();
			}

			GenerateOutput(group, Console.Out);
		}


		private static Stack<ConsoleColor> clrstack = new Stack<ConsoleColor>();
		private static void PushColor(ConsoleColor newcolor)
		{
			clrstack.Push(Console.ForegroundColor);
			Console.ForegroundColor = newcolor;
		}
		private static void PopColor()
		{
			Console.ForegroundColor = clrstack.Pop();
		}

		private static void OperateOnTrace(ManagerGroup group, TextReader input)
		{
			string line;
			byte[] data = new byte[0];
			int count = 0;

			while ((line = input.ReadLine()) != null)
			{
				string[] parts = line.Split('#');
				line = parts[0];

				parts = line.Split(new char[] { ' ', '\t' },
					StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length < 3)
					continue;

				if (++count % 2000 == 0)
				{
					PushColor(ConsoleColor.Green);
					Console.Error.WriteLine(count);
					PopColor();
				}
#if DEBUG
				if (count > 10000)
					break;
#endif
#if ANALISE
				if (count == 149)
					Console.WriteLine("Pause here");
#endif

				uint pageid = uint.Parse(parts[0]);
				uint length = uint.Parse(parts[1]);
				uint rw = uint.Parse(parts[2]);


                if(pageid==1)
                  System.Console.WriteLine("sd" + (pageid) + "  " + rw);


				if (rw == 0)
					while (length-- != 0)
						group.Read(pageid++, data);
				else
					while (length-- != 0)
						group.Write(pageid++, data);


				//AnalyseAndOutput(group, count);
			}

			group.Flush();

		}

		private static void GenerateOutput(ManagerGroup group, TextWriter output)
		{
			PushColor(ConsoleColor.Yellow);
			output.Write("Group  ");
			PopColor();
			output.WriteLine("Read {0}  Write {1}  Flush {2}  Cost {3}",
				group.ReadCount, group.WriteCount, group.FlushCount,
				Utils.CalcTotalCost(group));

			int[] maxlens = { 0, 0, 0, 0 };
			for (int i = 0; i < group.Count; i++)
			{
				IBlockDevice dev = group[i].AssociatedDevice;
				maxlens[0] = Math.Max(maxlens[0], i.ToString().Length);
				maxlens[1] = Math.Max(maxlens[1], dev.ReadCount.ToString().Length);
				maxlens[2] = Math.Max(maxlens[2], dev.WriteCount.ToString().Length);
				maxlens[3] = Math.Max(maxlens[3], Utils.CalcTotalCost(dev).ToString().Length);
			}

			string formatDev = string.Format("Dev {{0,{0}}}  ", maxlens[0]);
			string formatCost = string.Format(
				"Read {{0,{0}}}  Write {{1,{1}}}  Cost {{2,{2}}}  ",
				maxlens[1], maxlens[2], maxlens[3]);

			for (int i = 0; i < group.Count; i++)
			{
				IBlockDevice dev = group[i].AssociatedDevice;

				PushColor(ConsoleColor.Yellow);
				output.Write(formatDev, i);
				PopColor();
				output.Write(formatCost, dev.ReadCount, dev.WriteCount, Utils.CalcTotalCost(dev));

				PushColor(ConsoleColor.Cyan);
				output.Write(group[i].Name + " ");
				PopColor();

				if (group[i].Description != null)
				{
					PushColor(ConsoleColor.DarkGray);
					output.Write(group[i].Description);
					PopColor();
				}

				output.WriteLine();
			}
		}

/*		private static void AnalyseAndOutput(ManagerGroup group, int count)
		{
#if ANALISE
			if (FindBug(group, count))
				Console.WriteLine(count);
#endif
		}*/

/*#if ANALISE
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
#endif*/

	}
}
