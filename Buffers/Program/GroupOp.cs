using System;
using System.Collections.Generic;
using System.IO;
using Buffers.Devices;
using Buffers.Managers;
using Buffers.Utilities;

namespace Buffers.Program
{
	static class GroupOp
	{
		public static ManagerGroup InitGroup(uint[] npageses,
			AlgorithmSpec[] algorithms, RunModeInfo mode)
		{
			if (npageses.Length == 1)
				return InitSubGroup(npageses[0], algorithms, mode);

			ManagerGroup group = new ManagerGroup();

			foreach (uint npages in npageses)
				group.Add(InitSubGroup(npages, algorithms, mode));

			return group;
		}

		private static ManagerGroup InitSubGroup(uint npages,
			AlgorithmSpec[] algorithms, RunModeInfo mode)
		{
			string algoname = null;

			try
			{
				ManagerGroup group = new ManagerGroup();
				foreach (AlgorithmSpec algo in algorithms)
				{
					algoname = algo.Name;
					group.Add(Config.CreateManager(mode, algo, npages));
				}
				return group;
			}
			catch (Exception ex)
			{
				throw new InvalidCmdLineArgumentException(string.Format(
					"Exception occurs when creating {0}. Details: {1}",
					algoname, ex.Message), ex);
			}
		}

		public static DevStatInfo[] GatherStatistics(IBlockDevice dev,
			int level, int index, bool suppress)
		{
			IBufferManager mgr = dev as IBufferManager;
			ManagerGroup grp = mgr as ManagerGroup;

			DevStatInfo info = new DevStatInfo();
			info.Id = new string(' ', level) +
				(mgr == null ? "Dev" : grp == null ? "Mgr" : "Group") +
				(index < 0 ? "" : index.ToString());
			info.Name = new string(' ', level) + dev.Name;
			info.Description = (dev.Description == null ? "" : dev.Description);
            //........lyf.......
            dev = mgr.BaseDevice;

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
                ;//infos.AddRange(GatherStatistics(mgr.AssociatedDevice, level + 1, -1, false));
            else
                for (int i = 0; i < grp.Count; i++)
                    infos.AddRange(GatherStatistics(grp[i], level + 1, i, true));

			return infos.ToArray();
		}

		public static void VerifyData(ManagerGroup group)
		{
			if (group.Count < 2)
				return;

			MemoryStream[] streams = new MemoryStream[group.Count];

			for (int i = 0; i < streams.Length; i++)
				streams[i] = (group[i].BaseDevice as MemorySimulatedDevice).Stream;


			long length0 = streams[0].Length;

			for (int i = 1; i < streams.Length; i++)
			{
				long length = streams[i].Length;

				if (length0 != length)
					throw new DataNotConsistentException(string.Format(
						"Verified data have different length. " +
						"Device 0 has {0} pages, while Device {2} has {1} pages",
						length0, length, i));
			}


			foreach (MemoryStream stream in streams)
				stream.Seek(0, SeekOrigin.Begin);

			int readcount;
			byte[] data0 = new byte[128 * 1024], data = new byte[data0.Length];

			while ((readcount = streams[0].Read(data0, 0, data0.Length)) != 0)
			{
				for (int i = 1; i < streams.Length; i++)
				{
					streams[i].Read(data, 0, data.Length);
					int diffpos = Utils.FindDiff(data0, data, readcount);

					if (diffpos != -1)
						throw new DataNotConsistentException(string.Format(
							"Verified data not consistent at Page {0} between Device 0 and Device {1}",
							streams[0].Position - readcount + diffpos, i));
				}
			}

		}

	}
}