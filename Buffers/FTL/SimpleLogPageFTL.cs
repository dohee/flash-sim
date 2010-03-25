using System;

namespace Buffers.FTL
{
	public sealed class SimpleLogPageFTL : FTLBase
	{
		public SimpleLogPageFTL(IErasableDevice device)
			: base(device) { }

		protected override void DoRead(uint pageid, byte[] result)
		{
			dev.Read(pageid, result);
		}
		protected override void DoWrite(uint pageid, byte[] data)
		{
			dev.Write(pageid, data);
		}
		
	}
}