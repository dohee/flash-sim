using System;

namespace Buffers
{
	public interface IBlockDevice : IDisposable
	{
		/// <summary> 名称
		/// </summary>
		string Name { get; }
		/// <summary> 详细描述
		/// </summary>
		string Description { get; }

		/// <summary> 每个页面的字节数
		/// </summary>
		uint PageSize { get; }
		/// <summary> 读操作次数
		/// </summary>
		int ReadCount { get; }
		/// <summary> 写操作次数
		/// </summary>
		int WriteCount { get; }

		/// <summary> 执行读操作
		/// </summary>
		void Read(uint pageid, byte[] result);
		/// <summary> 执行写操作
		/// </summary>
		void Write(uint pageid, byte[] data);
	}
}
