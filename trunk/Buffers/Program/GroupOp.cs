using System;
using System.Collections.Generic;
using Buffers.Devices;
using Buffers.Managers;

namespace Buffers.Program
{
	static class GroupOp
	{
		public static ManagerGroup InitGroup(uint[] npageses, AlgorithmSpec[] algorithms, bool verify)
		{
			if (npageses.Length == 1)
				return InitSubGroup(npageses[0], algorithms, verify);

			ManagerGroup group = new ManagerGroup();

			foreach (uint npages in npageses)
				group.Add(InitSubGroup(npages, algorithms, verify));

			return group;
		}

		private static ManagerGroup InitSubGroup(uint npages, AlgorithmSpec[] algorithms, bool verify)
		{
			string algoname = null;

			try
			{
				ManagerGroup group = new ManagerGroup();
				foreach (AlgorithmSpec algo in algorithms)
				{
					algoname = algo.Name;
					group.Add(Config.CreateManager(algoname, npages, algo.Arguments, verify));
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
				infos.AddRange(GatherStatistics(mgr.AssociatedDevice, level + 1, -1, false));
			else
				for (int i = 0; i < grp.Count; i++)
					infos.AddRange(GatherStatistics(grp[i], level + 1, i, true));

			return infos.ToArray();
		}

		public static void VerifyData(ManagerGroup group)
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

	}
}