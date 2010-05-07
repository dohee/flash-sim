using System;
using System.Collections.Generic;
using Buffers.Devices;
using Buffers.Managers;
using System.Reflection;
using Buffers.Program;

namespace Buffers
{
	static class Config
	{
		public static readonly uint PageSize = 8192;
		public static decimal ReadCost { get; private set; }
		public static decimal WriteCost { get; private set; }
		public static float Ratio { get { return (float)(WriteCost / ReadCost); } }
		public static uint RoundedRatio { get { return (uint)Ratio; } }


		private delegate IBufferManager ManagerCreator(uint npages, string[] args);
		private static readonly IDictionary<string, MethodInfo> creators = new Dictionary<string, MethodInfo>();
		private static readonly IDictionary<string, string> normalNames = new Dictionary<string, string>();


		static Config()
		{
			SetConfig(1, 1);

			foreach (MethodInfo method in typeof(Config).GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
			{
				string normalName = null;
				foreach (object objattr in method.GetCustomAttributes(typeof(ManagerFactoryAttribute), false))
				{
					ManagerFactoryAttribute attr = objattr as ManagerFactoryAttribute;
					creators[attr.CmdLineName.ToLower()] = method;

					if (normalName == null)
						normalName = attr.CmdLineName;

					normalNames[attr.CmdLineName.ToLower()] = normalName;
				}
			}
		}

		public static void SetConfig(decimal readcost, decimal writecost)
		{
			ReadCost = readcost;
			WriteCost = writecost;
		}


		public static IBlockDevice CreateDevice(RunModeInfo mode, string algostring)
		{
			switch (mode.Mode)
			{
				case RunMode.Verify:
					return new MemorySimulatedDevice(1);

				case RunMode.File:
					return new FileSimulatedDevice(PageSize,
						(string)((object[])mode.ExtInfo)[0],
						(bool)((object[])mode.ExtInfo)[1]);

				case RunMode.Trace:
					return new TraceLogDevice(
						(string)mode.ExtInfo + "." + algostring + ".trace");

				default:
					return new NullBlockDevice();
			}
		}

		public static IBufferManager CreateManager(RunModeInfo mode,
			AlgorithmSpec algo, uint npages)
		{
			string lowerName = algo.Name.ToLower();

			string algoString = normalNames[lowerName] + algo.ArgumentString;
			IBlockDevice dev = CreateDevice(mode, algoString);

			return (IBufferManager)creators[lowerName].Invoke(
				null, new object[] { dev, npages, algo.Arguments });
		}

           
#pragma warning disable 0169 // "Never used"
#pragma warning disable 0618 // "Obsolete"
		// 注意：按字母表顺序排列
        [ManagerFactory("CCFLRU")]
		static IBufferManager CreateCCFLRU(IBlockDevice dev, uint npages, string[] args)
        {
            return new CCFLRU(dev, npages);
        } 
		[ManagerFactory("CFLRU")]
		static IBufferManager CreateCFLRU(IBlockDevice dev, uint npages, string[] args)
		{
			return new CFLRU(dev, npages, float.Parse(args[0]));
		}
		[ManagerFactory("CMFT")]
		static IBufferManager CreateCMFT(IBlockDevice dev, uint npages, string[] args)
		{
			return new CMFTByCat(dev, npages);
		}
		[ManagerFactory("CRAW")]
		static IBufferManager CreateCRAW(IBlockDevice dev, uint npages, string[] args)
		{
			return new CRAW(dev, npages, Ratio);
		}
		[ManagerFactory("FLRU")]
        [ManagerFactory("FLRUByLyf")]
		static IBufferManager CreateFLRUByLyf(IBlockDevice dev, uint npages, string[] args)
        {
            return new FLRU(dev, npages);
        }
        [ManagerFactory("LIRS")]
		static IBufferManager CreateLIRS(IBlockDevice dev, uint npages, string[] args)
        {
            return new LIRS_ByWu(dev, npages, float.Parse(args[0]));
        }
		[ManagerFactory("LRU")]
		static IBufferManager CreateLRU(IBlockDevice dev, uint npages, string[] args)
		{
			return new LRU(dev, npages);
		}
		[ManagerFactory("LRUWSR")]
		static IBufferManager CreateLRUWSR(IBlockDevice dev, uint npages, string[] args)
		{
			return new LRUWSR(dev, npages);
		}
		[ManagerFactory("Trival")]
		static IBufferManager CreateTrival(IBlockDevice dev, uint npages, string[] args)
		{
			return new TrivalManager(dev);
		}
#pragma warning restore 0618
#pragma warning restore 0169

	}
}
