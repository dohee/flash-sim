using System;
using Buffers.Devices;

namespace Buffers.FTL
{
	public abstract class FTLBase : BlockDeviceBase, IFTL
	{
		// 字段
		protected IErasableDevice dev;

		// 子类要实现的
		// （无）

		// 可供使用的
		// （无）

		// 已实现的
		public FTLBase(IErasableDevice device)
		{
			dev = device;
			PageSize = device.PageSize;
		}

		IBlockDevice IBlockDeviceWithBase.BaseDevice { get { return dev; } }
		public IErasableDevice BaseErasableDevice { get { return dev; } }
	}
}