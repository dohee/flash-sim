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


		static Config()
		{
			SetConfig(1, 1);

			foreach (MethodInfo method in typeof(Config).GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
			{
				foreach (object objattr in method.GetCustomAttributes(typeof(ManagerFactoryAttribute), false))
				{
					ManagerFactoryAttribute attr = objattr as ManagerFactoryAttribute;
					creators[attr.CmdLineName.ToLower()] = method;
				}
			}
		}
		public static void SetConfig(decimal readcost, decimal writecost)
		{
			ReadCost = readcost;
			WriteCost = writecost;
		}


		public static IBlockDevice CreateDevice(RunModeInfo mode)
		{
			switch (mode.Mode)
			{
				case RunMode.Verify:
					return new MemorySimulatedDevice(1);
				case RunMode.File:
					return new FileSimulatedDevice(PageSize, (string)mode.ExtInfo);
				default:
					return null;
			}
		}

		public static IBufferManager CreateManager(IBlockDevice dev,
			string name, uint npages, string[] args)
		{
			return (IBufferManager)creators[name.ToLower()].Invoke(
				null, new object[] { dev, npages, args });
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
		[ManagerFactory("FLIRSByCat")]
		static IBufferManager CreateFLIRSByCat(IBlockDevice dev, uint npages, string[] args)
		{
			return new FLIRSByCat(dev, npages, Ratio, float.Parse(args[0]));
		}
		[ManagerFactory("FLIRSByLyf")]
		static IBufferManager CreateFLIRSByLyf(IBlockDevice dev, uint npages, string[] args)
		{
			return new FLIRSbyLyf2(dev, npages, double.Parse(args[0]));
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
		[ManagerFactory("OldTn")]
		static IBufferManager CreateOldTn(IBlockDevice dev, uint npages, string[] args)
		{
			if (args.Length == 0)
				return new OldTn(dev, npages, Ratio);
			else
				return new OldTn(dev, npages, Ratio, new TnConfig(
					int.Parse(args[0]) != 0,
					int.Parse(args[1]) != 0,
					int.Parse(args[2]) != 0,
					float.Parse(args[3]),
					float.Parse(args[4]),
					float.Parse(args[5]),
					float.Parse(args[6])
					));
		}
		[ManagerFactory("OldOldTn")]
		static IBufferManager CreateOldOldTn(IBlockDevice dev, uint npages, string[] args)
		{
			if (args.Length == 0)
				return new OldOldTn(dev, npages, Ratio);
			else
				return new OldOldTn(dev, npages, Ratio, new OldOldTnConfig(
					int.Parse(args[0]) != 0,
					int.Parse(args[1]) != 0,
					uint.Parse(args[5]),
					uint.Parse(args[6]),
					int.Parse(args[2]) != 0
					));
		}
		[ManagerFactory("Tn")]
		[ManagerFactory("ACAR")]
		static IBufferManager CreateTn(IBlockDevice dev, uint npages, string[] args)
		{
			if (args.Length == 0)
				return new Tn(dev, npages, Ratio);
			else
				return new Tn(dev, npages, Ratio, new TnConfig(
					int.Parse(args[0]) != 0,
					int.Parse(args[1]) != 0,
					int.Parse(args[2]) != 0,
					float.Parse(args[3]),
					float.Parse(args[4]),
					float.Parse(args[5]),
					float.Parse(args[6])
					));
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
