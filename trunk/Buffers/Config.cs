using System;
using Buffers.Devices;
using Buffers.Managers;

namespace Buffers
{
	public static class Config
	{
		public static readonly int ReadCost = 66;
		public static readonly int WriteCost = 200;
		public static readonly bool RunVerify = false;
		public static readonly uint NPages = 5000;

		private static float Ratio = (float)Config.WriteCost / Config.ReadCost;
		private static uint RoundedRatio = (uint)Math.Round(Ratio);


		public static ManagerGroup InitGroup()
		{
			ManagerGroup group = new ManagerGroup();

			group.Add(new LRU(NPages));
			group.Add(new LRU(new LRU(NPages), NPages));
			//group.Add(Wrapper.CreateCFLRU(NPages, NPages / 2));
			//group.Add(Wrapper.CreateCFLRUD(NPages));
			//group.Add(Wrapper.CreateLRUWSR(NPages));
			//group.Add(new Tn(NPages, RoundedRatio, new TnConfig(false, false, 0, 0, false)));
			//group.Add(new Tn(NPages, RoundedRatio, new TnConfig(false, true, 0, 0, false)));
			//group.Add(new Tn(NPages, RoundedRatio, new TnConfig(true, false, 0, 0, false)));
			//group.Add(new Tn(NPages, RoundedRatio, new TnConfig(true, true, 0, 0, false)));
			group.Add(new Tn(NPages, RoundedRatio, new TnConfig(true, false, NPages / 4, NPages / 2, false)));
			group.Add(new FLIRSbyLyf2(NPages));
			group.Add(new FLIRSByCat(NPages, Ratio, 0.1f));
			//group.Add(new Tn(NPages, RoundedRatio, new TnConfig(true, false, NPages / 4, 0, true)));
			//group.Add(new CMFTByCat(NPages));
			//group.Add(new OldBlowerByCat(NPages));
			//group.Add(new BlowerByCat(NPages));
			//group.Add(new BlowerByLyf(NPages));
			//group.Add(new BlowerByLyf2(NPages));

			return group;
		}

		public static ManagerGroup InitVerifyGroup()
		{
			ManagerGroup group = new ManagerGroup(true);

			group.Add(new TrivalManager(new MemorySimulatedDevice(1)));
			group.Add(new LRU(new MemorySimulatedDevice(1), NPages));	//Passed
			//group.Add(new Tn(new MemorySimulatedDevice(1), NPages, RoundedRatio));	//Passed
			//group.Add(new CMFTByCat(new MemorySimulatedDevice(1), NPages));	//Passed
			//group.Add(Wrapper.CreateCFLRUD(new MemorySimulatedDevice(1), NPages));	//Passed
			group.Add(new FLIRSByCat(new MemorySimulatedDevice(1),NPages, Ratio, 0.1f));	//Passed
			group.Add(new FLIRSbyLyf2(new MemorySimulatedDevice(1), NPages));	//Failed

			return group;
		}
	}
}
