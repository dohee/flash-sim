using System;

namespace Buffers
{
	public interface IBlockDeviceWithBase : IBlockDevice
	{
		/// <summary> 底层的设备
		/// </summary>
		IBlockDevice BaseDevice { get; }
	}
}
