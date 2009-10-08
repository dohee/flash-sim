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
			const uint npages = 500;
			ManagerGroup group = new ManagerGroup();

			group.Add(new TrivalManager());
			group.Add(new LRU(npages));

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
				if (count > 6000)
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
			output.WriteLine("Group\tRead\t{0}\tWrite\t{1}\tFlush\t{2}\tCost\t{3}",
				group.ReadCount, group.WriteCount, group.FlushCount,
				Utils.CalcTotalCost(group));

			for (int i = 0; i < group.Count; i++)
			{
				IBlockDevice dev = group[i].AssociatedDevice;

				output.WriteLine("Dev {3}\tRead\t{0}\tWrite\t{1}\tCost\t{2}",
					dev.ReadCount, dev.WriteCount,
					Utils.CalcTotalCost(dev), i);
			}
		}
	}
}
