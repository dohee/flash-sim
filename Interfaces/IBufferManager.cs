using System;

namespace Buffers
{
	public interface IBufferManager : IBlockDevice
	{
		string Name { get; }
		string Description { get; }

		IBlockDevice AssociatedDevice { get; }
		int FlushCount { get; }

		void Flush();
	}
}
