using System;

namespace Buffers
{
	public interface IErasableDevice : IBlockDevice
	{
		/// <summary> 每个块的页面数
		/// </summary>
		ushort BlockSize { get; }
		/// <summary> 擦除操作的次数
		/// </summary>
		int EraseCount { get; }

		/// <summary> 执行擦除操作
		/// </summary>
		void Erase(uint blockid);
	}
}
