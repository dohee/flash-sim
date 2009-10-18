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
			const uint npages = 100;
			ManagerGroup group = new ManagerGroup();
			Math.Max(1, 2);
			group.Add(new LRU(npages));
			group.Add(Wrapper.CreateCFLRU(npages, npages / 2));
			group.Add(Wrapper.CreateCFLRUD(npages));
			group.Add(new Tn(npages, 3, new TnConfig(false, false, 0, 0, false)));
			group.Add(new Tn(npages, 3, new TnConfig(false, true, 0, 0, false)));
			group.Add(new Tn(npages, 3, new TnConfig(true, false, 0, 0, false)));
			group.Add(new Tn(npages, 3, new TnConfig(true, true, 0, 0, false)));
			group.Add(new Tn(npages, 3, new TnConfig(false, false, npages / 4, npages / 2, false)));
			group.Add(new Tn(npages, 3, new TnConfig(false, false, npages / 4, 0, true)));

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

				OperateOnTrace(group, reader);
			}
			finally
			{
				if (reader != null)
					reader.Dispose();
			}

			GenerateOutput(group, Console.Out);
		}

		private static void OperateOnTrace(ManagerGroup group, TextReader input)
		{
			string line;
			byte[] data = new byte[0];
			int count = 0;

			while ((line = input.ReadLine()) != null)
			{
				string[] parts = line.Split(new char[] { ' ', '\t' },
					StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length < 3)
					continue;

				if (++count % 2000 == 0)
					Console.Error.WriteLine(count);
#if DEBUG
				if (count > 10000)
					break;
#endif

				uint pageid = uint.Parse(parts[0]);
				uint length = uint.Parse(parts[1]);
				uint rw = uint.Parse(parts[2]);

				if (rw == 0)
					while (length-- != 0)
						group.Read(pageid++, data);
				else
					while (length-- != 0)
						group.Write(pageid++, data);
			}

			group.Flush();
		}

		private static void GenerateOutput(ManagerGroup group, TextWriter output)
		{
			output.WriteLine("Group  Read {0}  Write {1}  Flush {2}  Cost {3}",
				group.ReadCount, group.WriteCount, group.FlushCount,
				Utils.CalcTotalCost(group));

			int[] maxlens = { 0, 0, 0, 0 };
			for (int i = 0; i < group.Count; i++)
			{
				IBlockDevice dev = group[i].AssociatedDevice;
				maxlens[0] = Math.Max(maxlens[0], i.ToString().Length);
				maxlens[1] = Math.Max(maxlens[0], dev.ReadCount.ToString().Length);
				maxlens[2] = Math.Max(maxlens[0], dev.WriteCount.ToString().Length);
				maxlens[3] = Math.Max(maxlens[0], Utils.CalcTotalCost(dev).ToString().Length);
			}

			string format = string.Format(
				"Dev {{0,{0}}}  Read {{1,{1}}}  Write {{2,{2}}}  Cost {{3,{3}}}  {{4}}",
				maxlens[0], maxlens[1], maxlens[2], maxlens[3]);

			for (int i = 0; i < group.Count; i++)
			{
				IBlockDevice dev = group[i].AssociatedDevice;
				output.WriteLine(format, i, dev.ReadCount, dev.WriteCount,
					Utils.CalcTotalCost(dev), group[i].Description);
			}
		}
	}
}
