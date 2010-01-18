using System;
using Buffers.Devices;
using Buffers.Managers;

namespace Buffers
{
	public static class Config
	{
		public static readonly int ReadCost = 66;
		public static readonly int WriteCost = 200;

		private static float Ratio = (float)Config.WriteCost / Config.ReadCost;
		private static uint RoundedRatio = (uint)Math.Round(Ratio);


		public static IBlockDevice CreateDevice(bool verify)
		{
			if (verify)
				return new MemorySimulatedDevice(1);
			else
				return null;
		}

		public static IBufferManager CreateManager(string name, uint npages, bool verify)
		{
			switch (name.ToLower())
			{
				case "trival":
					return new TrivalManager(CreateDevice(verify));
				case "lru":
					return new LRU(CreateDevice(verify), npages);
				case "cflru":
					return Wrapper.CreateCFLRU(CreateDevice(verify), npages, npages / 2);
				case "cmft":
					return new CMFTByCat(CreateDevice(verify), npages);
				case "tn":
					return new Tn(CreateDevice(verify), npages, RoundedRatio, new TnConfig(true, false, npages / 4, npages / 2, false));
				case "flirsbycat":
					return new FLIRSByCat(CreateDevice(verify), npages, Ratio, 0.4f);
				case "flirsbylyf":
					return new FLIRSbyLyf2(CreateDevice(verify), npages);
				default:
					return null;
			}
		}

	}
}
