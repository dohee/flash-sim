using System;

namespace Buffers
{
	public interface IBufferManager : IBlockDeviceWithBase
	{
		/// <summary> 刷操作次数
		/// </summary>
		int FlushCount { get; }

		/// <summary> 执行刷操作
		/// </summary>
		void Flush();
		/// <summary> 在多层 Manager 中，执行连续刷操作
		/// </summary>
		void CascadeFlush();
	}
}
