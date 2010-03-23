using System;

namespace Buffers
{
	public interface IBufferManager : IBlockDevice
	{
		/// <summary> 底层的设备
		/// </summary>
		IBlockDevice AssociatedDevice { get; }

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
