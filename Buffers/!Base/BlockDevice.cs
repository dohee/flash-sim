using System;

namespace Buffers
{
	public interface IBlockDevice
	{
		uint PageSize { get; }
		int ReadCount { get; }
		int WriteCount { get; }

		void Read(uint pageid, byte[] result);
		void Write(uint pageid, byte[] data);
	}
}

namespace Buffers.Devices
{
	public sealed class TrivalBlockDevice : Buffers.IBlockDevice
	{
		private int read = 0, write = 0;
		private static byte[] emptyData = new byte[0];

		public uint PageSize { get { return 0; } }
		public int ReadCount { get { return read; } }
		public int WriteCount { get { return write; } }

		public void Read(uint pageid, byte[] result)
		{
			read++;
		}

		public void Write(uint pageid, byte[] data)
		{
			write++;
		}
	}
}