using System;

namespace Buffers
{
	public interface IBlockDevice
	{
		string Name { get; }
		string Description { get; }

		uint PageSize { get; }
		int ReadCount { get; }
		int WriteCount { get; }

		void Read(uint pageid, byte[] result);
		void Write(uint pageid, byte[] data);
	}
}
