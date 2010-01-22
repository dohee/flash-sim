﻿using System;
using System.Collections.Generic;
using Buffers.Devices;
using Buffers.Managers;

namespace Buffers
{
	public static class Config
	{
		public static readonly int ReadCost = 66;
		public static readonly int WriteCost = 200;

		public static readonly string[] ManagerNames = {
			"Trival", "CFLRU", "CMFT", "FLIRSByCat", "FLIRSByLyf", "LRU", "Tn" };
		private static readonly ManagerCreator[] ManagerCreators = {
			CreateTrival, CreateCFLRU, CreateCMFT, CreateFLIRSByCat, CreateFLIRSByLyf,
			CreateLRU, CreateTn };



		private static readonly float Ratio = (float)Config.WriteCost / Config.ReadCost;
		private static readonly uint RoundedRatio = (uint)Math.Round(Ratio);

		private delegate IBufferManager ManagerCreator(uint npages, string[] args, bool verify);
		private static readonly IDictionary<string, ManagerCreator> creators = new Dictionary<string, ManagerCreator>();


		static Config()
		{
			for (int i = 0; i < ManagerNames.Length; i++)
				creators[ManagerNames[i].ToLower()] = ManagerCreators[i];
		}


		public static IBlockDevice CreateDevice(bool verify)
		{
			return verify ? new MemorySimulatedDevice(1) : null;
		}
		public static IBufferManager CreateManager(string name, uint npages, string[] args, bool verify)
		{
			return creators[name](npages, args, verify);
		}



	
		private static IBufferManager CreateTrival(uint npages, string[] args, bool verify)
		{
			return new TrivalManager(CreateDevice(verify));
		}
		private static IBufferManager CreateCFLRU(uint npages, string[] args, bool verify)
		{
			return new CFLRU(CreateDevice(verify), npages, float.Parse(args[0]));
		}
		private static IBufferManager CreateCMFT(uint npages, string[] args, bool verify)
		{
			return new CMFTByCat(CreateDevice(verify), npages);
		}
		private static IBufferManager CreateLRU(uint npages, string[] args, bool verify)
		{
			return new LRU(CreateDevice(verify), npages);
		}
		private static IBufferManager CreateFLIRSByCat(uint npages, string[] args, bool verify)
		{
			return new FLIRSByCat(CreateDevice(verify), npages, Ratio, float.Parse(args[0]));
		}
		private static IBufferManager CreateFLIRSByLyf(uint npages, string[] args, bool verify)
		{
			return new FLIRSbyLyf2(CreateDevice(verify), npages);
		}
		private static IBufferManager CreateTn(uint npages, string[] args, bool verify)
		{
			if (args.Length == 0)
				return new Tn(CreateDevice(verify), npages, RoundedRatio);
			else
				return new Tn(CreateDevice(verify), npages, RoundedRatio, new TnConfig(
					int.Parse(args[0]) != 0,
					int.Parse(args[1]) != 0,
					uint.Parse(args[2]),
					uint.Parse(args[3]),
					int.Parse(args[4]) != 0));
		}
	}
}
