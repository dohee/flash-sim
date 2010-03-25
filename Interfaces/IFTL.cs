using System;

namespace Buffers
{
	public interface IFTL : IBlockDeviceWithBase
	{
		IErasableDevice BaseErasableDevice { get; }
	}
}