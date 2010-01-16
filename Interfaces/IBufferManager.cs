using System;

namespace Buffers
{
	public interface IBufferManager : IBlockDevice
	{
		IBlockDevice AssociatedDevice { get; }
		int FlushCount { get; }

		void Flush();
		void CascadeFlush();
	}
}
